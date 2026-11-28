using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.CSharp;
using Andirit.CSharp.Tests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Independent successor C4 occurrence oracle. It consumes verified analysis
// inputs but derives every reference identity, defining context, target and
// occurrence identity from the documented successor contract in
// SuccessorIdentityOracle; it never calls the production identity helpers.
if (args.Length != 3)
{
    Console.Error.WriteLine("usage: C4SuccessorOracle <corpus-root> <variant> <output.json>");
    return 2;
}

var corpusRoot = Path.GetFullPath(args[0]);
var variant = args[1];
var outputPath = Path.GetFullPath(args[2]);
const string repository = "review:s64:d3-c4";
const string startedText = "2026-11-28T12:00:00Z";

var files = new Dictionary<string, string>(StringComparer.Ordinal);
foreach (var path in Directory.EnumerateFiles(corpusRoot, "*", SearchOption.AllDirectories)
             .Where(path => path.EndsWith(".cs", StringComparison.Ordinal) ||
                            path.EndsWith(".csproj", StringComparison.Ordinal))
             .OrderBy(path => Relative(path), StringComparer.Ordinal))
{
    var logical = Relative(path);
    var text = File.ReadAllText(path);
    if (variant == "without-caller2" && logical.StartsWith("Caller2/", StringComparison.Ordinal))
        continue;
    if (variant == "without-caller" && logical.StartsWith("Caller/", StringComparison.Ordinal))
        continue;
    if (variant == "caller2-changed" && logical == "Caller2/Work.cs")
        text = "// caller2 variant: same target, changed caller source\n" + text;
    files[logical] = text;
}

var plugin = new CSharpCodeGraphPlugin();
var run = new CodeIndexRunId("run:c4-oracle");
var sources = files.Select(pair =>
{
    var bytes = Encoding.UTF8.GetBytes(pair.Value);
    return new CodeGraphSource(pair.Key,
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
        _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
}).ToArray();
var context = new CodeGraphPluginContext(new CodeRepositoryId(repository), null, run, sources);
var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
if (!analysis.Universe.IsComplete)
    throw new InvalidOperationException("corpus analysis is incomplete: " + string.Join(",", analysis.Universe.Refusals));

var facts = new List<Fact>();
foreach (var caller in analysis.Universe.Entries.OrderBy(entry => entry.Descriptor.ProjectUnitId, StringComparer.Ordinal))
{
    foreach (var tree in caller.Compilation.SyntaxTrees.OrderBy(tree => tree.FilePath, StringComparer.Ordinal))
    {
        var path = tree.FilePath.Replace('\\', '/');
        var source = caller.Sources.Single(item => item.Path == path);
        var model = caller.Compilation.GetSemanticModel(tree);
        foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
                continue;
            if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.ReducedExtension
                or MethodKind.ExplicitInterfaceImplementation))
                continue;
            var defining = Defining(analysis.Universe, caller, method);
            if (defining is null)
                continue;
            var span = SiteSpan(tree, invocation);
            var identifier = SuccessorIdentityOracle.IdentifierDigest(method);
            var declaration = SuccessorIdentityOracle.DeclarationDigest(method);
            var shape = SuccessorIdentityOracle.ShapeDigest(method);
            var target = SuccessorIdentityOracle.TargetId(
                repository, defining, method, _ => identifier, _ => declaration, _ => shape);
            var targetDocument = SuccessorIdentityOracle.TargetDocumentId(method);
            var usage = SuccessorIdentityOracle.UsageId(
                repository, plugin.Id.Value, caller.Descriptor.ProjectUnitId, path,
                source.ContentHash, span, targetDocument, target);
            facts.Add(new Fact(usage, caller.Descriptor.ProjectUnitId, path, source.ContentHash,
                span, targetDocument, target, "Invocation"));
        }
    }
}

var ordered = facts.OrderBy(fact => fact.Id, StringComparer.Ordinal).ToArray();
var targetIds = ordered.Select(fact => fact.TargetId).Distinct(StringComparer.Ordinal).ToArray();
if (targetIds.Length != 1)
    throw new InvalidOperationException("expected one target, observed " + targetIds.Length);
var contributors = ordered.Select(fact => fact.Unit).Distinct(StringComparer.Ordinal)
    .Order(StringComparer.Ordinal).ToArray();
var definingUnit = analysis.Projects.Single(project =>
    project.Project.Path == "Dependency/Dependency.csproj").Verified!;
var markMethod = definingUnit.Compilation.GetTypeByMetadataName("QualifierDependency.Api")!
    .GetMembers("Mark").OfType<IMethodSymbol>().Single();
var mapped = SuccessorIdentityOracle.DescribeDefiningContext(repository, definingUnit);
var ledger = new
{
    variant,
    repository,
    plugin = plugin.Id.Value,
    pluginVersion = "0.2.0-preview.7",
    started = startedText,
    target = new
    {
        value = targetIds[0],
        document = SuccessorIdentityOracle.TargetDocumentId(markMethod),
        context = mapped,
        metadata = SuccessorIdentityOracle.Metadata(definingUnit.Compilation.Assembly.Identity),
        module = definingUnit.Compilation.Assembly.Modules.First().Name,
        contributors,
    },
    cardinality = ordered.Length,
    targetContributorCount = contributors.Length,
    occurrences = ordered.Select(fact => new
    {
        Id = fact.Id,
        Unit = fact.Unit,
        Path = fact.Path,
        SourceHash = fact.SourceHash,
        StartLine = fact.Span.StartLine,
        StartColumn = fact.Span.StartColumn,
        EndLine = fact.Span.EndLine,
        EndColumn = fact.Span.EndColumn,
        TargetDocument = fact.TargetDocument,
        TargetId = fact.TargetId,
        Category = fact.Category,
    }).ToArray(),
};
var json = JsonSerializer.Serialize(ledger, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(outputPath, json + "\n");
Console.WriteLine(json);
return 0;

string Relative(string path) =>
    Path.GetRelativePath(corpusRoot, path).Replace('\\', '/');

static VerifiedCSharpCompilation? Defining(
    VerifiedCSharpUniverse universe, VerifiedCSharpCompilation caller, IMethodSymbol targetMethod)
{
    if (targetMethod.ContainingAssembly is null)
        return null;
    foreach (var entry in universe.Entries)
    {
        if (ReferenceEquals(entry.Compilation.Assembly, targetMethod.ContainingAssembly))
            return entry;
    }
    var metadata = SuccessorIdentityOracle.Metadata(targetMethod.ContainingAssembly.Identity);
    foreach (var reference in caller.Compilation.References.OfType<CompilationReference>())
    {
        if (SuccessorIdentityOracle.Metadata(reference.Compilation.Assembly.Identity) != metadata)
            continue;
        return universe.Entries.SingleOrDefault(entry =>
            ReferenceEquals(entry.Compilation, reference.Compilation));
    }
    return null;
}

static CodeLocation SiteSpan(SyntaxTree tree, InvocationExpressionSyntax invocation)
{
    var token = invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier,
        SimpleNameSyntax simple => simple.Identifier,
        _ => invocation.Expression.GetLastToken(),
    };
    var line = tree.GetLineSpan(token.Span);
    var path = tree.FilePath.Replace('\\', '/');
    return new CodeLocation(path,
        line.StartLinePosition.Line + 1, line.StartLinePosition.Character + 1,
        line.EndLinePosition.Line + 1, line.EndLinePosition.Character + 1);
}

internal sealed record Fact(
    string Id, string Unit, string Path, string SourceHash, CodeLocation Span,
    string TargetDocument, string TargetId, string Category);

