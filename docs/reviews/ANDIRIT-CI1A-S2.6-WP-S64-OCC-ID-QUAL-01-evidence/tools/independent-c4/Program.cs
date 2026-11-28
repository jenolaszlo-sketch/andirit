using System.Buffers.Binary;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
const string repo = "review:s64:d3-c4";
const string plugin = "penghou.hetu.csharp";
const string semantics = "usage5-b2c-exp-r2-20261128";
var runtime = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
var references = new List<(string Identity, MetadataReference Reference)>();
foreach (var path in Directory.EnumerateFiles(runtime, "*.dll").Order(StringComparer.Ordinal))
{
    using var stream = File.OpenRead(path);
    using var pe = new PEReader(stream);
    if (!pe.HasMetadata || !pe.GetMetadataReader().IsAssembly) continue;
    var reader = pe.GetMetadataReader();
    var def = reader.GetAssemblyDefinition();
    var metadata = string.Join("|", reader.GetString(def.Name), def.Version.ToString(),
        def.Culture.IsNil ? "" : reader.GetString(def.Culture), Convert.ToHexStringLower(PublicKeyToken(reader.GetBlobBytes(def.PublicKey))));
    var digest = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    references.Add(("refctx1|" + metadata + "|sha256:" + digest, MetadataReference.CreateFromFile(path)));
}
if (args.Length > 2 && args[2] == "three")
{
    references = references.Where(r => r.Identity.StartsWith("refctx1|System.Runtime|") || r.Identity.StartsWith("refctx1|System.Private.CoreLib|") || r.Identity.StartsWith("refctx1|Microsoft.CSharp|")).ToList();
}
references = references.OrderBy(r => r.Identity, StringComparer.Ordinal).ToList();
var projects = new Dictionary<string, ProjectInfo>(StringComparer.Ordinal);
foreach (var csproj in Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
{
    var rel = Path.GetRelativePath(root, csproj).Replace('\\', '/');
    var doc = XDocument.Load(csproj);
    var asm = doc.Descendants("AssemblyName").Select(e => e.Value.Trim()).FirstOrDefault() ?? Path.GetFileNameWithoutExtension(csproj);
    var sources = Directory.EnumerateFiles(Path.GetDirectoryName(csproj)!, "*.cs", SearchOption.AllDirectories)
        .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                 && !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
        .Order(StringComparer.Ordinal).ToArray();
    var refs = doc.Descendants("ProjectReference").Select(e => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(csproj)!, e.Attribute("Include")!.Value)))
        .Select(p => Path.GetRelativePath(root, p).Replace('\\', '/')).ToArray();
    projects[rel] = new ProjectInfo(rel, asm, sources, refs);
}
foreach (var project in projects.Values.OrderBy(p => p.Rel, StringComparer.Ordinal)) Build(project, projects, references, root);
var dep = projects["Dependency/Dependency.csproj"];
var depComp = dep.Compilation!;
var sourceText = File.ReadAllText(dep.Sources.Single());
var sourceHash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(dep.Sources.Single())));
var options = OptionsDigest(depComp.Options);
var unit = "csharp:project:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Dependency/Dependency.csproj")));
var depMetadata = Metadata(depComp.Assembly.Identity);
var module = depComp.Assembly.Modules.First().Name;
var compctx3 = Hash("compctx3-context", ["compctx3", repo, unit, "Dependency", depMetadata, module, options,
    "Dependency/Api.cs|" + sourceHash + "|CSharp14|", .. references.Select(r => r.Identity)]);
var compctx4 = Hash("compctx4-context", ["compctx4", repo, unit, "Dependency", depMetadata, module, options,
    "Dependency/Api.cs|" + sourceHash + "|CSharp14|", .. references.Select(r => r.Identity)]);
var mark = (IMethodSymbol)depComp.GetTypeByMetadataName("QualifierDependency.Api")!.GetMembers("Mark").Single();
var idDigest = Hash("asmctx4-identifier", ["Mark"]);
var declDigest = Hash("asmctx4-declarations", ["M:QualifierDependency.Api.Mark"]);
var shapeDigest = Hash("asmctx4-target", ["method|0|"]);
var target = "asmctx4:sha256:" + Hash("asmctx4-context", ["compctx4", "compctx4:sha256:" + compctx4, depMetadata, module, idDigest, declDigest, shapeDigest]);
var facts = new List<Fact>();
foreach (var caller in projects.Values.Where(p => p.Rel is "Caller/Caller.csproj" or "Caller2/Caller2.csproj"))
{
    foreach (var tree in caller.Compilation!.SyntaxTrees)
    {
        var path = tree.FilePath.Replace('\\', '/');
        var source = caller.Sources.Single(s => Path.GetRelativePath(root, s).Replace('\\', '/') == path);
        var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(source)));
        var model = caller.Compilation.GetSemanticModel(tree);
        foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol m) continue;
            if (!ReferenceEquals(m.ContainingAssembly, depComp.Assembly)) continue;
            var token = invocation.Expression is MemberAccessExpressionSyntax ma ? ma.Name.Identifier : invocation.Expression.GetLastToken();
            var line = tree.GetLineSpan(token.Span);
            var span = new[] { (line.StartLinePosition.Line + 1).ToString(), (line.StartLinePosition.Character + 1).ToString(), (line.EndLinePosition.Line + 1).ToString(), (line.EndLinePosition.Character + 1).ToString() };
            var usage = Hash("usage5", [repo, plugin, semantics, caller.Rel == "Caller/Caller.csproj"
                ? "csharp:project:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Caller/Caller.csproj")))
                : "csharp:project:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("Caller2/Caller2.csproj"))),
                path, hash, .. span, "M:QualifierDependency.Api.Mark", target, "method-invocation"]);
            facts.Add(new Fact(usage, "csharp:project:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(caller.Rel))), path, hash, span[0], span[1], span[2], span[3]));
        }
    }
}
var samples = references.Where(r => r.Identity.Contains("|System.Runtime|") || r.Identity.Contains("|System.Private.CoreLib|") || r.Identity.Contains("|Microsoft.CSharp|")).Select(r => r.Identity).ToArray();
var refHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", references.Select(r => r.Identity)))));
var result = new { target, compctx3 = "compctx3:sha256:" + compctx3, compctx4 = "compctx4:sha256:" + compctx4, options, unit, metadata = depMetadata, module, sourceHash, sourceText = "Dependency/Api.cs|" + sourceHash + "|CSharp14|", refHash, samples, referenceCount = references.Count, cardinality = facts.Count, facts = facts.OrderBy(f => f.Id, StringComparer.Ordinal).ToArray() };
await File.WriteAllTextAsync(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new { target, compctx3 = "compctx3:sha256:" + compctx3, compctx4 = "compctx4:sha256:" + compctx4, options, cardinality = facts.Count, referenceCount = references.Count }, new JsonSerializerOptions { WriteIndented = true }));

static void Build(ProjectInfo project, Dictionary<string, ProjectInfo> projects, List<(string Identity, MetadataReference Reference)> refs, string root)
{
    if (project.Compilation is not null) return;
    var trees = project.Sources.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path),
        new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose),
        Path.GetRelativePath(root, path).Replace('\\', '/'))).ToArray();
    var references = refs.Select(r => r.Reference).Cast<MetadataReference>().ToList();
    foreach (var r in project.Refs)
    {
        Build(projects[r], projects, refs, root);
        references.Add(projects[r].Compilation!.ToMetadataReference());
    }
    project.Compilation = CSharpCompilation.Create(project.Assembly, trees, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable,
            deterministic: true, concurrentBuild: false));
}
static string Hash(string domain, IEnumerable<string> parts)
{
    using var h = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    void Add(string s) { var b = Encoding.UTF8.GetBytes(s); var l = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(l, b.Length); h.AppendData(l); h.AppendData(b); }
    Add(domain); foreach (var p in parts) Add(p);
    return Convert.ToHexStringLower(h.GetHashAndReset());
}
static string OptionsDigest(CompilationOptions o) => Hash("compctx3-options",
    [o.OutputKind.ToString(), o.NullableContextOptions.ToString(), (o as CSharpCompilationOptions)?.AllowUnsafe.ToString() ?? "", o.OptimizationLevel.ToString(),
     o.Platform.ToString(), o.CheckOverflow.ToString(), o.Deterministic.ToString(), o.WarningLevel.ToString(),
     o.GeneralDiagnosticOption.ToString(), string.Join(",", o.SpecificDiagnosticOptions.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key + "=" + p.Value))]);
static string Metadata(AssemblyIdentity i) => string.Join("|", i.Name, i.Version.ToString(), i.CultureName ?? "", i.PublicKeyToken.IsDefaultOrEmpty ? "" : Convert.ToHexStringLower(i.PublicKeyToken.AsSpan()));
static byte[] PublicKeyToken(byte[] key) { var h = SHA1.HashData(key); var t = h.AsSpan(h.Length - 8).ToArray(); Array.Reverse(t); return t; }
sealed class ProjectInfo(string rel, string assembly, string[] sources, string[] refs)
{
    public string Rel { get; } = rel; public string Assembly { get; } = assembly; public string[] Sources { get; } = sources;
    public string[] Refs { get; } = refs; public CSharpCompilation? Compilation { get; set; }
}


sealed record Fact(string Id, string Unit, string Path, string SourceHash, string StartLine, string StartColumn, string EndLine, string EndColumn);









