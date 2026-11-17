using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var records = new List<object>();
foreach (var scenario in new[] { "signature", "signature-direct", "retarget", "forwarding" })
{
    var files = new Dictionary<string, string>(StringComparer.Ordinal);
    if (scenario != "forwarding")
    {
        AddProject("Root", "namespace Root; public class Hidden {} ");
        AddProject("Bridge", "namespace Bridge; public static class Api { public static int Signature(Root.Hidden? x = null) => 7; public static int Primitive() => 9; }", "Root");
    }
    switch (scenario)
    {
        case "signature":
            AddProject("Caller", "public static class Use { public static int Call() => Bridge.Api.Signature(); }", "Bridge");
            break;
        case "signature-direct":
            AddProject("Caller", "public static class Use { public static int Call() => Bridge.Api.Signature(); }", "Bridge", "Root");
            break;
        case "retarget":
            AddProject("Caller", "public static class Use { public static int Call() => Bridge.Api.Primitive(); }", "Bridge");
            break;
        case "forwarding":
            AddProject("Caller", "public static class Use { public static bool Call() => System.Uri.IsWellFormedUriString(\"path\", System.UriKind.Relative); }");
            break;
    }

    var repo = new CodeRepositoryId("review:r3:m54:" + scenario);
    var context = new CodeGraphPluginContext(repo, null, new CodeIndexRunId("probe"), files.Select(pair =>
    {
        var bytes = Encoding.UTF8.GetBytes(pair.Value);
        return new CodeGraphSource(pair.Key, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
            _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }).ToArray());
    var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
    var caller = analysis.Projects.Single(p => p.Project.Path == "Caller/Caller.csproj");
    var compilation = caller.Compilation;
    var tree = compilation.SyntaxTrees.Single();
    var invocation = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
    var info = compilation.GetSemanticModel(tree).GetSymbolInfo(invocation);
    var method = info.Symbol as IMethodSymbol;
    var owner = method?.ContainingAssembly;
    var reference = owner is null ? null : compilation.GetMetadataReference(owner);
    var facade = compilation.References.SingleOrDefault(r => Path.GetFileName(r.Display) == "System.dll");
    var facadeSymbol = facade is null ? null : compilation.GetAssemblyOrModuleSymbol(facade) as IAssemblySymbol;
    var forwardedUri = scenario == "forwarding" ? facadeSymbol?.ResolveForwardedType("System.Uri") : null;
    var exactCaller = caller.Verified is not null &&
        ReferenceEquals(caller.Verified, analysis.Universe.RequireCompilation(compilation));
    var eligible = method?.MethodKind is MethodKind.Ordinary or MethodKind.ReducedExtension or MethodKind.ExplicitInterfaceImplementation;
    var resolution = eligible && exactCaller ? analysis.Universe.ResolveDefining(method!, null, compilation) : null;
    records.Add(new
    {
        scenario, files, invocation = invocation.ToString(),
        analysis.Universe.IsComplete, analysis.Universe.Refusals,
        declared = analysis.Universe.Scope.Units,
        verified = analysis.Universe.Entries.Select(e => e.Descriptor.ProjectUnitId),
        exactCaller, eligible, symbol = method?.ToDisplayString(), kind = method?.MethodKind.ToString(),
        owner = owner?.Identity.ToString(), ownerReferenceType = reference?.GetType().Name, ownerReferenceDisplay = reference?.Display,
        ownAssembly = ReferenceEquals(compilation.Assembly, owner),
        directSources = caller.AvailableCompilations.Select(pair => new
        {
            path = pair.Key, referenceEqualsOwner = ReferenceEquals(pair.Value.Assembly, owner),
            identityEqualsOwner = pair.Value.Assembly.Identity.Equals(owner?.Identity),
        }),
        capturedReferenceCount = compilation.References.Count(),
        forwardingFacade = scenario == "forwarding" ? facade?.Display : null,
        forwardedOwner = forwardedUri?.ContainingAssembly.Identity.ToString(),
        forwardedTypeMatchesInvocationOwner = forwardedUri is not null && ReferenceEquals(forwardedUri.ContainingAssembly, owner),
        candidateReason = info.CandidateReason.ToString(), candidates = info.CandidateSymbols.Select(c => c.ToDisplayString()),
        diagnostics = compilation.GetDiagnostics().Select(d => d.ToString()),
        resolver = resolution?.Status.ToString(), resolution?.Detail,
    });
    if (resolution?.Status == VerifiedDefiningStatus.MissingDefiningCompilation)
        break;

    void AddProject(string name, string source, params string[] references)
    {
        var items = string.Concat(references.Select(r => $"<ProjectReference Include=\"../{r}/{r}.csproj\" />"));
        files[$"{name}/{name}.csproj"] = $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{items}</ItemGroup></Project>";
        files[$"{name}/Code.cs"] = source;
    }
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    processId = Environment.ProcessId,
    platform = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
    compiler = Describe(typeof(CSharpCompilation).Assembly),
    common = Describe(typeof(Compilation).Assembly),
    records,
}, new JsonSerializerOptions { WriteIndented = true }));

static object Describe(Assembly assembly) => new
{
    name = assembly.FullName,
    version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
    sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
};
