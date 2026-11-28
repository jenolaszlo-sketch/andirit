using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AdrCi1Exp.Shared;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Independent C4 two-unit occurrence oracle. It uses the frozen R2 provenance
// authority and the public identity contract; it never calls the production
// extractor.
if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("usage: D3OccurrenceOracle <c4-root> <output.json> [baseline|without-caller|without-caller2|caller2-changed]");
    return 2;
}

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var variant = args.Length == 3 ? args[2] : "baseline";
if (variant is not ("baseline" or "without-caller" or "without-caller2" or "caller2-changed"))
    throw new ArgumentException("unknown variant " + variant);

const string RepositoryId = "review:s64:d3-c4";
const string PluginId = "penghou.hetu.csharp";
const string PluginVersion = "0.2.0-preview.7";
var repository = new CodeRepositoryId(RepositoryId);
var plugin = new CodePluginId(PluginId);
var metadata = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .Order(StringComparer.OrdinalIgnoreCase)
    .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
    .ToArray();
var parse = new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose);
var options = new CSharpCompilationOptions(
    OutputKind.DynamicallyLinkedLibrary,
    optimizationLevel: OptimizationLevel.Release,
    nullableContextOptions: NullableContextOptions.Disable,
    deterministic: true,
    concurrentBuild: false);

var dependencySources = new[] { "Dependency/Api.cs" };
var callerSources = new[] { "Caller/Caller.csproj" }
    .SelectMany(_ => Directory.EnumerateFiles(Path.Combine(root, "Caller"), "*.cs")
        .Select(path => Relative(path)).Order(StringComparer.Ordinal))
    .ToArray();
var caller2Sources = new[] { "Caller2/Caller2.csproj" }
    .SelectMany(_ => Directory.EnumerateFiles(Path.Combine(root, "Caller2"), "*.cs")
        .Select(path => Relative(path)).Order(StringComparer.Ordinal))
    .ToArray();

var dependency = Build("Dependency", dependencySources, [], metadata);
var caller = Build("Caller", callerSources, [dependency], metadata);
var caller2 = variant == "caller2-changed"
    ? Build("Caller2", caller2Sources, [dependency], metadata, addComment: true)
    : Build("Caller2", caller2Sources, [dependency], metadata);

var dependencyUnit = UnitId("Dependency/Dependency.csproj");
var callerUnit = UnitId("Caller/Caller.csproj");
var caller2Unit = UnitId("Caller2/Caller2.csproj");
var descriptor = CompilationProvenance.Describe(
    RepositoryId, dependencyUnit, dependency,
    dependencySources.Select(path => (path, File.ReadAllBytes(Path.Combine(root, path)))).ToArray());
var method = (IMethodSymbol)dependency.GetTypeByMetadataName("QualifierDependency.Api")!
    .GetMembers("Mark").Single();
var documentId = method.GetDocumentationCommentId() ?? "M:QualifierDependency.Api.Mark";
var target = CodeOccurrenceIdentity.ComputeTargetId(
    descriptor.DescriptorDigest,
    descriptor.MetadataIdentity,
    descriptor.ModuleName,
    CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]),
    CodeOccurrenceIdentity.ComputeDeclarationDigest([documentId]),
    CodeOccurrenceIdentity.ComputeShapeDigest("method|0|"));

var occurrences = new List<Occurrence>();
var units = new List<(string Unit, CSharpCompilation Compilation, string[] Sources)>
{
    (callerUnit, caller, callerSources),
    (caller2Unit, caller2, caller2Sources)
};
foreach (var (unit, compilation, sources) in units)
{
    if (variant == "without-caller" && unit == callerUnit) continue;
    if (variant == "without-caller2" && unit == caller2Unit) continue;
    foreach (var path in sources)
    {
        var tree = compilation.SyntaxTrees.Single(candidate =>
            Relative(candidate.FilePath) == path);
        var semantic = compilation.GetSemanticModel(tree);
        var bytes = File.ReadAllBytes(Path.Combine(root, path));
        var hash = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
        foreach (var invocation in tree.GetRoot().DescendantNodes()
                     .OfType<InvocationExpressionSyntax>())
        {
            if (semantic.GetSymbolInfo(invocation).Symbol is not IMethodSymbol bound ||
                bound.Name != "Mark" ||
                bound.ContainingType.ToDisplayString() != "QualifierDependency.Api")
                continue;
            var token = invocation.Expression.GetLastToken();
            var span = token.GetLocation().GetLineSpan().Span;
            var location = new CodeLocation(
                path,
                span.Start.Line + 1,
                span.Start.Character + 1,
                span.End.Line + 1,
                span.End.Character + 1);
            var id = CodeOccurrenceIdentity.ComputeOccurrenceId(
                repository, plugin, unit, path, hash, location,
                documentId, target, CodeOccurrenceCategory.Invocation);
            occurrences.Add(new Occurrence(
                id.Value, unit, path, hash, location.StartLine, location.StartColumn,
                location.EndLine, location.EndColumn, documentId, target.Value,
                CodeOccurrenceIdentity.InvocationCategory));
        }
    }
}

var result = new
{
    variant,
    repository = RepositoryId,
    plugin = PluginId,
    pluginVersion = PluginVersion,
    target = new
    {
        value = target.Value,
        document = documentId,
        context = descriptor.DescriptorDigest,
        metadata = descriptor.MetadataIdentity,
        module = descriptor.ModuleName,
        contributors = occurrences.Select(item => item.Unit)
            .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
    },
    occurrences = occurrences.OrderBy(item => item.Unit, StringComparer.Ordinal)
        .ThenBy(item => item.Path, StringComparer.Ordinal)
        .ThenBy(item => item.StartLine)
        .ThenBy(item => item.StartColumn).ToArray(),
    byUnit = occurrences.GroupBy(item => item.Unit, StringComparer.Ordinal)
        .OrderBy(group => group.Key, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count()),
    cardinality = occurrences.Count,
    targetContributorCount = occurrences.Select(item => item.Unit)
        .Distinct(StringComparer.Ordinal).Count()
};
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(json);
await File.WriteAllTextAsync(output, json);
return 0;

CSharpCompilation Build(
    string assembly, string[] sources, CSharpCompilation[] references,
    MetadataReference[] metadataReferences, bool addComment = false)
{
    var trees = sources.Select(path =>
    {
        var text = File.ReadAllText(Path.Combine(root, path));
        if (addComment && path == "Caller2/Work.cs")
            text = "// occurrence-variant\n" + text;
        return CSharpSyntaxTree.ParseText(text, parse, Path.Combine(root, path));
    }).ToList();
    var refs = new List<MetadataReference>(metadataReferences);
    refs.AddRange(references.Select(reference => reference.ToMetadataReference()));
    return CSharpCompilation.Create(assembly, trees, refs, options);
}

string Relative(string path) =>
    Path.GetRelativePath(root, path).Replace('\\', '/');

static string UnitId(string projectPath) =>
    "csharp:project:" + Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(projectPath)));

internal sealed record Occurrence(
    string Id, string Unit, string Path, string SourceHash,
    int StartLine, int StartColumn, int EndLine, int EndColumn,
    string TargetDocument, string TargetId, string Category);
