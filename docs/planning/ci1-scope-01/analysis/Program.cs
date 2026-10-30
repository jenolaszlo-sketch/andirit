using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args[0]);
var packet = Path.Combine(root, "docs/planning/ci1-scope-01");
var mode = args.Length > 1 ? args[1] : "fixture";
var data = new SortedDictionary<string, string>(StringComparer.Ordinal);
if (mode == "fixture")
{
    foreach (var path in Directory.GetFiles(Path.Combine(packet, "fixtures"), "*.cs"))
        data.Add("src/" + Path.GetFileName(path), File.ReadAllText(path));
}
else
{
    // An explicit tracked-source subset; never enumerate build output or use an edited tree as a Git snapshot.
    foreach (var project in (args.Length > 2 ? args[2].Split(',') : new[] { "Andirit.Abstractions", "Andirit", "Andirit.CSharp" }))
    {
        var folder = Path.Combine(root, "src", project);
        foreach (var path in Directory.GetFiles(folder, "*", SearchOption.TopDirectoryOnly)
                     .Where(p => p.EndsWith(".cs", StringComparison.Ordinal) || p.EndsWith(".csproj", StringComparison.Ordinal)))
            data.Add(Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path));
    }
}
// Separate source/Roslyn walk: never reconstruct occurrences from graph edges.
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
    .Select(p => MetadataReference.CreateFromFile(p)).ToList<MetadataReference>();
var oracle = new List<object>();
var allSites = new List<Site>();
foreach (var group in mode == "fixture"
             ? new[] { ("@loose/csharp", data.Where(p => p.Key.EndsWith(".cs")).ToArray()) }
             : new[] { "Andirit.Abstractions", "Andirit", "Andirit.CSharp" }.Select(p =>
                 ("src/" + p + "/" + p + ".csproj", data.Where(v => v.Key.StartsWith("src/" + p + "/", StringComparison.Ordinal) && v.Key.EndsWith(".cs")).ToArray())))
{
        var trees = group.Item2.Select(p => CSharpSyntaxTree.ParseText(p.Value, path: p.Key)).ToArray();
    var compilationTrees = mode == "fixture" ? trees : trees.Append(CSharpSyntaxTree.ParseText(
        "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;",
        path: "@oracle/sdk-implicit-usings.cs")).ToArray();
    var compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(group.Item1), compilationTrees, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false));
    foreach (var tree in trees)
    {
        var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);
        foreach (var syntax in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var info = model.GetSymbolInfo(syntax);
            var method = info.Symbol as IMethodSymbol;
            var target = method is null ? null : (method.ReducedFrom ?? method).OriginalDefinition.GetDocumentationCommentId();
            var owner = syntax.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
            var ownerSymbol = owner is null ? null : model.GetDeclaredSymbol(owner);
            allSites.Add(new(group.Item1, target, ownerSymbol?.GetDocumentationCommentId(),
                Loc(tree.FilePath, syntax), info.CandidateSymbols.Length, info.CandidateReason.ToString()));
        }
    }
    references.Add(compilation.ToMetadataReference());
}
var repository = new CodeRepositoryId("repo:ci1-" + mode);
var run = new CodeIndexRunId("run:ci1-baseline");
var plugin = new CSharpCodeGraphPlugin();
var store = new InMemoryCodeGraphStore();
var started = DateTimeOffset.UtcNow;
await store.UpsertRepositoryAsync(new(repository));
await store.StoreIndexRunAsync(new(repository, run, started, plugins: [plugin.Id]));
var sources = data.Select(p => new CodeGraphSource(p.Key, "sha256:" + Hash(Encoding.UTF8.GetBytes(p.Value)),
    _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(p.Value))))).ToArray();
var context = new CodeGraphPluginContext(repository, "memory://ci1-pinned-inputs", run, sources);
var clock = Stopwatch.StartNew();
var recording = new RecordingSink();
await using (var session = await plugin.CreateSessionAsync(context))
{
    recording.Result = await session.ExtractAsync(recording);
}
clock.Stop();
var extractionMs = clock.Elapsed.TotalMilliseconds;
clock.Restart();
await using (var sink = new CodeGraphIngestionSink(store))
    foreach (var batch in recording.Batches) await sink.WriteBatchAsync(batch);
await store.CompleteIndexRunAsync(new(repository, run, started, CodeIndexRunStatus.Completed, DateTimeOffset.UtcNow, [plugin.Id]),
    new(repository, run, sources.Select(s => new CodeSourceManifest(plugin.Id, plugin.Version, s.Path, s.ContentHash)).ToArray(),
        snapshotIdentity: null, isConsistentSnapshot: false));
clock.Stop();
var publicationMs = clock.Elapsed.TotalMilliseconds;
var nodes = recording.Batches.SelectMany(b => b.Nodes).DistinctBy(n => n.Id.Value).ToArray();
var declarations = recording.Batches.SelectMany(b => b.Declarations).DistinctBy(n => n.Id.Value).ToArray();
var edges = recording.Batches.SelectMany(b => b.Edges).DistinctBy(e => e.Id.Value).ToArray();
var nodeMap = nodes.ToDictionary(n => n.Id.Value);
var queries = new CodeGraphQueryService(store);
var view = (await queries.OpenLatestPublicationAsync(repository))!;
var selections = mode == "fixture"
    ? new[] { ("Logger.Write(string)", "M:Logger.Write(System.String)"), ("Target", "T:Target") }
    : new[]
    {
        ("Andirit.CodeGraphQueryService.EnsurePublication(Andirit.CodeGraphPublication, Andirit.CodeGraphPublication?)", "M:Andirit.CodeGraphQueryService.EnsurePublication(Andirit.CodeGraphPublication,Andirit.CodeGraphPublication)"),
        ("Andirit.CodeGraphQueryService.FindSymbolsByQualifiedNameAsync(Andirit.CodeRepositoryId, string, System.Threading.CancellationToken)", "M:Andirit.CodeGraphQueryService.FindSymbolsByQualifiedNameAsync(Andirit.CodeRepositoryId,System.String,System.Threading.CancellationToken)"),
        ("Andirit.CodeGraphQueryService.FindDeclarationsWithProvenanceAsync(Andirit.CodeRepositoryId, Andirit.CodeSymbolId, System.Threading.CancellationToken)", "M:Andirit.CodeGraphQueryService.FindDeclarationsWithProvenanceAsync(Andirit.CodeRepositoryId,Andirit.CodeSymbolId,System.Threading.CancellationToken)"),
        ("Andirit.CodeGraphBatchLimits.Positive(int, string)", "M:Andirit.CodeGraphBatchLimits.Positive(System.Int32,System.String)"),
        ("Andirit.ICodeGraphReader.GetLatestPublicationAsync(Andirit.CodeRepositoryId, System.Threading.CancellationToken)", "M:Andirit.ICodeGraphReader.GetLatestPublicationAsync(Andirit.CodeRepositoryId,System.Threading.CancellationToken)")
    };
var answers = new List<object>();
foreach (var (name, docId) in selections)
{
    var stepTimes = new List<double>();
    clock.Restart();
        // Planning-only identity discovery from recorded declarations; consumer need not fetch all nodes.
    var prefix = name.Contains('(') ? name[..name.IndexOf('(')] + "(" : name;
    var discovered = nodes.Where(n => n.QualifiedName == name || (name.Contains('(') && n.QualifiedName?.StartsWith(prefix, StringComparison.Ordinal) == true)).ToArray();
    var selectedName = discovered.Length == 1 ? discovered[0].QualifiedName! : name;
    var lookup = await view.FindSymbolAsync(selectedName);
    stepTimes.Add(clock.Elapsed.TotalMilliseconds);
    var target = lookup.Result.SingleOrDefault;
    if (target is null)
    {
        answers.Add(new { Requested = name, Error = "Not uniquely selected", Candidates = lookup.Result.Candidates.Select(n => n.QualifiedName),
            NearNames = nodes.Where(n => n.QualifiedName?.StartsWith(prefix, StringComparison.Ordinal) == true).Select(n => n.QualifiedName) });
        continue;
    }
    clock.Restart(); var decl = await view.FindDeclarationsAsync(target.SymbolId!); stepTimes.Add(clock.Elapsed.TotalMilliseconds);
    clock.Restart(); var calls = await view.FindCallersAsync(target.Id, new(maxDepth: 1, maxNodes: 1000, maxEdges: 5000)); stepTimes.Add(clock.Elapsed.TotalMilliseconds);
    clock.Restart(); var refs = await view.FindReferencesAsync(target.Id, new(maxDepth: 1, maxNodes: 1000, maxEdges: 5000)); stepTimes.Add(clock.Elapsed.TotalMilliseconds);
    var serialized = new[] { JsonSerializer.Serialize(lookup), JsonSerializer.Serialize(decl), JsonSerializer.Serialize(calls), JsonSerializer.Serialize(refs) };
    var joined = new { Requested = name, SelectedQualifiedName = selectedName, NodeId = target.Id.Value, SymbolId = target.SymbolId!.Value,
        Declarations = decl.Result.Select(d => d.Location),
        Callers = calls.Result.Edges.Select(e => Edge(e)), References = refs.Result.Edges.Select(e => Edge(e)),
        CallsTruncated = calls.Result.Truncated, ReferencesTruncated = refs.Result.Truncated,
        OracleInvocations = allSites.Where(s => s.TargetDocumentationId == docId).ToArray(),
        LogicalApiCalls = 4, QueryElapsedMs = stepTimes, SerializedEnvelopeUtf8Bytes = serialized.Sum(s => Encoding.UTF8.GetByteCount(s)) };
    answers.Add(joined);
}
var report = new
{
    BaselineCommit = "cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2", Mode = mode,
    Environment = new { Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription, ProcessorCount = Environment.ProcessorCount },
    HarnessBoundary = "Public extraction session, recording sink, validated public ingestion, atomic public completion; not full indexing-service timing. Independent Roslyn uses the same project subset and host references plus explicitly declared standard SDK implicit global usings for real mode; plugin inputs remain raw. No MSBuild or restore. Oracle is stronger than plugin context and is not a completeness proof.",
    InputFiles = data.Select(p => new { Path = p.Key, Sha256 = Hash(File.ReadAllBytes(Path.Combine(mode == "fixture" ? Path.Combine(packet, "fixtures") : root,
        mode == "fixture" ? Path.GetFileName(p.Key) : p.Key))), Utf8TextBytes = Encoding.UTF8.GetByteCount(p.Value) }),
    SourceBytesUtf8 = data.Sum(p => Encoding.UTF8.GetByteCount(p.Value)), ExtractionMs = extractionMs, PublicationMs = publicationMs,
    PluginId = plugin.Id.Value, PluginVersion = plugin.Version, view.Publication,
    Nodes = nodes.Length, Declarations = declarations.Length, Edges = edges.Length,
    RecordedFactJsonUtf8Bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(recording.Batches)),
    Coverage = recording.Result!.RelationshipCoverage, UnitCoverage = recording.Result.IndexUnitCoverage, recording.Result.WarningCodes,
    Answers = answers, RawFixtureInvocations = mode == "fixture" ? allSites : null,
    FixtureEdges = mode == "fixture" ? edges.Where(e => e.Kind == CodeEdgeKinds.Calls || e.Kind == CodeEdgeKinds.References).Select(Edge) : null,
    RepeatedCallerGroups = allSites.Where(s => s.TargetDocumentationId is not null).GroupBy(s => (s.Project, s.OwnerDocumentationId, s.TargetDocumentationId))
        .Where(g => g.Count() >= 3).OrderByDescending(g => g.Count()).Take(8).Select(g => new { g.Key.Project, g.Key.OwnerDocumentationId, g.Key.TargetDocumentationId, Count = g.Count(), Sites = g.ToArray() })
};
File.WriteAllText(Path.Combine(packet, "evidence", mode + "-baseline.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new { Mode = mode, Inputs = data.Count, Nodes = nodes.Length, Edges = edges.Length, ExtractionMs = extractionMs, PublicationMs = publicationMs,
    SelectionCount = answers.Count }));
return 0;

object Edge(CodeGraphEdge e) => new { Id = e.Id.Value, Kind = e.Kind.Value, SourceId = e.SourceId.Value, SourceName = nodeMap[e.SourceId.Value].QualifiedName,
    TargetId = e.TargetId.Value, TargetName = nodeMap[e.TargetId.Value].QualifiedName, e.Evidence };
static CodeLocation Loc(string path, SyntaxNode syntax)
{
    var s = syntax.GetLocation().GetLineSpan().Span;
    return new(path, s.Start.Line + 1, s.Start.Character + 1, s.End.Line + 1, s.End.Character + 1);
}
static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
sealed record Site(string Project, string? TargetDocumentationId, string? OwnerDocumentationId, CodeLocation Location, int CandidateCount, string CandidateReason);
sealed class RecordingSink : ICodeGraphSink
{
    public CodeGraphBatchLimits Limits { get; } = new();
    public List<CodeGraphBatch> Batches { get; } = [];
    public CodeGraphExtractionResult? Result { get; set; }
    public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken cancellationToken = default)
    { Batches.Add(batch); return ValueTask.CompletedTask; }
}
