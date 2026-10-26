using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Penghou.Hetu;

// GLM-Q1 qualification harness: p9 reindex of the authentic p8 seed and
// complete fact comparison. Writes a JSON report next to the store.
var root = args.Length > 0 ? args[0] : throw new ArgumentException("Usage: reindex <workspace-root> <report.json>");
var databasePath = Path.Combine(root, "graph.db");
var report = new Report();

var tsRepoId = new CodeRepositoryId("repo:q1-migration-ts");
var jsRepoId = new CodeRepositoryId("repo:q1-migration-js");
var tsRoot = Path.Combine(root, "ts");
var jsRoot = Path.Combine(root, "js");

// --- Phase 1: capture the p8 snapshot exactly as persisted. ---
string p8TsPluginVersion;
string p8JsPluginVersion;
using (var store = new LatticeCodeGraphStore(databasePath))
{
    p8TsPluginVersion = PluginVersionOfUnits(await store.GetPublishedUnitsAsync(tsRepoId));
    p8JsPluginVersion = PluginVersionOfUnits(await store.GetPublishedUnitsAsync(jsRepoId));
    (report.P8TsCounts, report.P8TsResolverVersion) = await CountFactsAsync(store, tsRepoId);
    (report.P8JsCounts, report.P8JsResolverVersion) = await CountFactsAsync(store, jsRepoId);
    report.P8TypeScript = await SnapshotAsync(store, tsRepoId);
    report.P8JavaScript = await SnapshotAsync(store, jsRepoId);
    Console.WriteLine($"p8 ts: nodes={report.P8TsCounts.Nodes} decls={report.P8TsCounts.Declarations} edges={report.P8TsCounts.Edges} unitsPluginVersion={p8TsPluginVersion} resolver={report.P8TsResolverVersion}");
    Console.WriteLine($"p8 js: nodes={report.P8JsCounts.Nodes} decls={report.P8JsCounts.Declarations} edges={report.P8JsCounts.Edges} unitsPluginVersion={p8JsPluginVersion} resolver={report.P8JsResolverVersion}");
}
report.P8TsPluginVersion = p8TsPluginVersion;
report.P8JsPluginVersion = p8JsPluginVersion;

// --- Phase 2: reindex the same sources under p9. ---
using (var store = new LatticeCodeGraphStore(databasePath))
{
    var typeScriptIndexing = new CodeIndexingService(
        new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
        store);
    var typeScriptResult = await typeScriptIndexing.IndexAsync(
        new CodeRepositoryDescriptor(tsRepoId, tsRoot), new CodeIndexRunId("run:q1-p9-ts"));
    Console.WriteLine($"ts p9 status: {typeScriptResult.Diagnostics.Status}");
    var javaScriptIndexing = new CodeIndexingService(
        new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
        new CodeGraphPluginRegistry([new TreeSitterJavaScriptPlugin()]),
        store);
    var javaScriptResult = await javaScriptIndexing.IndexAsync(
        new CodeRepositoryDescriptor(jsRepoId, jsRoot), new CodeIndexRunId("run:q1-p9-js"));
    Console.WriteLine($"js p9 status: {javaScriptResult.Diagnostics.Status}");

    report.P9TypeScript = await SnapshotAsync(store, tsRepoId);
    report.P9JavaScript = await SnapshotAsync(store, jsRepoId);
    (report.P9TsCounts, report.P9TsResolverVersion) = await CountFactsAsync(store, tsRepoId);
    (report.P9JsCounts, report.P9JsResolverVersion) = await CountFactsAsync(store, jsRepoId);
    report.P9TsUnitOriginVersions = UnitOriginVersions(report.P9TypeScript);
    report.P9JsUnitOriginVersions = UnitOriginVersions(report.P9JavaScript);
    report.P9TsExportNames = ExportEntryNames(report.P9TypeScript);
    report.P9JsExportNames = ExportEntryNames(report.P9JavaScript);
    report.P9TsExportEntries = snapshotCountKind(report.P9TypeScript, "export");
    report.P9JsExportEntries = snapshotCountKind(report.P9JavaScript, "export");
    report.P9TsExportEdges = snapshotCountKind(report.P9TypeScript, "exports");
    report.P9JsExportEdges = snapshotCountKind(report.P9JavaScript, "exports");
}

// --- Phase 3: comparison of complete fact sets. ---
var tsExistingFromP8 = FilterExisting(report.P8TypeScript);
var tsExistingFromP9 = FilterExisting(report.P9TypeScript);
report.TypeScriptExistingFactsIdentical = Serialization(tsExistingFromP8) == Serialization(tsExistingFromP9);
report.TypeScriptExistingSnapshotSha256 = Sha256(Serialization(tsExistingFromP9));
var jsExistingFromP8 = FilterExisting(report.P8JavaScript);
var jsExistingFromP9 = FilterExisting(report.P9JavaScript);
report.JavaScriptExistingFactsIdentical = Serialization(jsExistingFromP8) == Serialization(jsExistingFromP9);
report.JavaScriptExistingSnapshotSha256 = Sha256(Serialization(jsExistingFromP9));

report.P9TsResolverUnchanged = report.P8TsResolverVersion == report.P9TsResolverVersion;
report.P9JsResolverUnchanged = report.P8JsResolverVersion == report.P9JsResolverVersion;
report.P9TsExistingEvidenceVersions = DistinctExistingEdgeVersions(report.P9TypeScript);
report.P9JsExistingEvidenceVersions = DistinctExistingEdgeVersions(report.P9JavaScript);
report.P9TsExportEvidenceVersions = DistinctExportEdgeVersions(report.P9TypeScript);
report.P9JsExportEvidenceVersions = DistinctExportEdgeVersions(report.P9JavaScript);

// --- Phase 4: close/reopen query behavior on the migrated store. ---
using (var store = new LatticeCodeGraphStore(databasePath))
{
    var queries = new CodeGraphQueryService(store);
    var current = await queries.FindSymbolsByQualifiedNameAsync(tsRepoId, "current");
    report.ReopenCurrentLookup = current.Candidates.Select(node => $"{node.Kind.Value}:{node.Id.Value}").ToArray();
    var neighborhood = await queries.GetNeighborhoodAsync(
        tsRepoId,
        new CodeNodeId(FileId("typescript", "src/main.ts")),
        [CodeEdgeKinds.Exports],
        new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
    report.ReopenNeighborhoodEdgeTargets = neighborhood.Edges
        .Select(edge => edge.TargetId.Value)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();
}

// --- Phase 5: recorded-hash reconciliation attempts over natural serializations. ---
report.HashReconciliation = TryReconcileHash(report);

File.WriteAllText(args[1], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"ts p9: nodes={report.P9TsCounts.Nodes} decls={report.P9TsCounts.Declarations} edges={report.P9TsCounts.Edges} entries={report.P9TsExportEntries} exportEdges={report.P9TsExportEdges}");
Console.WriteLine($"js p9: nodes={report.P9JsCounts.Nodes} decls={report.P9JsCounts.Declarations} edges={report.P9JsCounts.Edges} entries={report.P9JsExportEntries} exportEdges={report.P9JsExportEdges}");
Console.WriteLine($"ts existing-facts byte-identical: {report.TypeScriptExistingFactsIdentical}; q1 snapshot sha256: {report.TypeScriptExistingSnapshotSha256}");
Console.WriteLine($"js existing-facts byte-identical: {report.JavaScriptExistingFactsIdentical}; q1 snapshot sha256: {report.JavaScriptExistingSnapshotSha256}");
Console.WriteLine($"ts resolver unchanged: {report.P9TsResolverUnchanged} ({report.P9TsResolverVersion})");
Console.WriteLine($"js resolver unchanged: {report.P9JsResolverUnchanged} ({report.P9JsResolverVersion})");
Console.WriteLine($"ts p9 unit origin versions: {string.Join(",", report.P9TsUnitOriginVersions)}");
Console.WriteLine($"js p9 unit origin versions: {string.Join(",", report.P9JsUnitOriginVersions)}");
Console.WriteLine($"ts existing evidence versions: {string.Join(",", report.P9TsExistingEvidenceVersions)}");
Console.WriteLine($"ts export evidence versions: {string.Join(",", report.P9TsExportEvidenceVersions)}");
Console.WriteLine($"ts export entries: {string.Join(",", report.P9TsExportNames)}");
Console.WriteLine($"js export entries: {string.Join(",", report.P9JsExportNames)}");
Console.WriteLine($"reopen exact-name 'current': {string.Join(",", report.ReopenCurrentLookup)}");
Console.WriteLine($"reopen neighborhood targets: {report.ReopenNeighborhoodEdgeTargets.Length}");
Console.WriteLine($"hash reconciliation: {string.Join("; ", report.HashReconciliation)}");
return 0;

static string FileId(string language, string path) =>
    $"{language}:file:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path)))}";

static string PluginVersionOfUnits(IReadOnlyList<CodeIndexUnitReplacement> units) =>
    units.Select(unit => unit.Origin.PluginVersion).Distinct().Single();

static async Task<(FactCounts Counts, string ResolverVersion)> CountFactsAsync(ICodeGraphStore store, CodeRepositoryId repositoryId)
{
    var units = await store.GetPublishedUnitsAsync(repositoryId);
    var counts = new FactCounts(
        units.Sum(unit => unit.Nodes.Count),
        units.Sum(unit => unit.Declarations.Count),
        units.Sum(unit => unit.Edges.Count));
    var resolverVersion = units.SelectMany(unit => unit.Edges)
        .Where(edge => edge.Evidence.ProviderVersion?.Contains("resolver2", StringComparison.Ordinal) == true)
        .Select(edge => edge.Evidence.ProviderVersion!)
        .Distinct().Single();
    return (counts, resolverVersion);
}

static async Task<UnitSnapshot> SnapshotAsync(ICodeGraphStore store, CodeRepositoryId repositoryId)
{
    var units = await store.GetPublishedUnitsAsync(repositoryId);
    return new UnitSnapshot(
        units
            .Select(unit => new UnitSnapshotUnit(
                unit.Origin.IndexUnitId.Value,
                unit.Origin.PluginVersion,
                unit.Nodes.Select(SerializeNode).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                unit.Declarations.Select(SerializeDeclaration).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                unit.Edges.Select(SerializeEdge).OrderBy(value => value, StringComparer.Ordinal).ToArray()))
            .OrderBy(unit => unit.UnitId, StringComparer.Ordinal)
            .ToArray());
}

static string[] UnitOriginVersions(UnitSnapshot snapshot) =>
    snapshot.Units.Select(unit => unit.PluginVersion).Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();

static (string kind, string name, string json) ParseNode(string json)
{
    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    return (root.GetProperty("Kind").GetString()!, root.GetProperty("Name").GetString()!, json);
}

static (string kind, string json) ParseEdge(string json)
{
    using var document = JsonDocument.Parse(json);
    return (document.RootElement.GetProperty("Kind").GetString()!, json);
}

static string[] ExportEntryNames(UnitSnapshot snapshot) =>
    snapshot.Units.SelectMany(unit => unit.Nodes)
        .Select(ParseNode)
        .Where(node => node.kind == "export")
        .Select(node => node.name)
        .OrderBy(value => value, StringComparer.Ordinal)
        .ToArray();

static int snapshotCountKind(UnitSnapshot snapshot, string kind) =>
    snapshot.Units.SelectMany(unit => kind == "export" ? unit.Nodes : unit.Edges).Count(value => ParseKind(value) == kind);

static string ParseKind(string json)
{
    using var document = JsonDocument.Parse(json);
    return document.RootElement.GetProperty("Kind").GetString()!;
}

static UnitSnapshotUnit FilterExisting(UnitSnapshot snapshot) =>
    new(
        "(filtered)",
        "(filtered)",
        snapshot.Units.SelectMany(unit => unit.Nodes).Select(ParseNode).Where(node => node.kind != "export").Select(node => node.json).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        snapshot.Units.SelectMany(unit => unit.Declarations).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
        snapshot.Units.SelectMany(unit => unit.Edges).Select(ParseEdge).Where(edge => edge.kind != "exports").Select(edge => edge.json).OrderBy(value => value, StringComparer.Ordinal).ToArray());

static string[] DistinctExistingEdgeVersions(UnitSnapshot snapshot) =>
    snapshot.Units.SelectMany(unit => unit.Edges).Select(ParseEdge).Where(edge => edge.kind != "exports")
        .Select(edge => JsonDocument.Parse(edge.json).RootElement.GetProperty("Evidence").GetProperty("ProviderVersion").GetString())
        .Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray()!;

static string[] DistinctExportEdgeVersions(UnitSnapshot snapshot) =>
    snapshot.Units.SelectMany(unit => unit.Edges).Select(ParseEdge).Where(edge => edge.kind == "exports")
        .Select(edge => JsonDocument.Parse(edge.json).RootElement.GetProperty("Evidence").GetProperty("ProviderVersion").GetString())
        .Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray()!;

static string SerializeNode(CodeGraphNode node) =>
    JsonSerializer.Serialize(new
    {
        Id = node.Id.Value,
        Kind = node.Kind.Value,
        node.Name,
        node.QualifiedName,
        SymbolId = node.SymbolId?.Value,
        Properties = SortedProperties(node.Properties)
    });

static string SerializeDeclaration(CodeGraphDeclaration declaration) =>
    JsonSerializer.Serialize(new
    {
        Id = declaration.Id.Value,
        SymbolNodeId = declaration.SymbolNodeId.Value,
        SymbolId = declaration.SymbolId.Value
    });

static string SerializeEdge(CodeGraphEdge edge) =>
    JsonSerializer.Serialize(new
    {
        Id = edge.Id.Value,
        SourceId = edge.SourceId.Value,
        TargetId = edge.TargetId.Value,
        Kind = edge.Kind.Value,
        Evidence = new
        {
            Quality = edge.Evidence.Quality.ToString(),
            edge.Evidence.Provider,
            edge.Evidence.ProviderVersion,
            Location = edge.Evidence.Location is null ? null : new
            {
                edge.Evidence.Location.Path,
                edge.Evidence.Location.StartLine,
                edge.Evidence.Location.StartColumn,
                edge.Evidence.Location.EndLine,
                edge.Evidence.Location.EndColumn
            },
            edge.Evidence.Confidence
        },
        Properties = SortedProperties(edge.Properties)
    });

static (string Key, string Value)[] SortedProperties(IReadOnlyDictionary<string, CodePropertyValue> properties) =>
    properties.OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => (pair.Key, Value: pair.Value switch
        {
            CodeTextProperty text => "t:" + text.Value,
            CodeBooleanProperty boolean => "b:" + boolean.Value,
            CodeIntegerProperty integer => "i:" + integer.Value.ToString(CultureInfo.InvariantCulture),
            CodeNumberProperty number => "n:" + number.Value.ToString("R", CultureInfo.InvariantCulture),
            CodeTextListProperty list => "l:" + string.Join(",", list.Values),
            _ => "o:" + pair.Value.ToString()
        }))
        .ToArray();

static string Serialization(UnitSnapshotUnit unit) =>
    JsonSerializer.Serialize(new
    {
        unit.Nodes,
        unit.Declarations,
        unit.Edges
    });

static string Sha256(string value) =>
    Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

static string[] TryReconcileHash(Report report)
{
    var expected = "ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f";
    var attempts = new List<string>();
    foreach (var (label, snapshot) in new[] { ("ts", report.P9TypeScript), ("js", report.P9JavaScript) })
    {
        var filtered = FilterExisting(snapshot);
        // (a) projected serialization
        attempts.Add($"{label}-projected={Sha256(Serialization(filtered))}");
        // (b) re-encoded System.Text.Json object tree
        var rawJson = JsonSerializer.Serialize(new
        {
            Nodes = filtered.Nodes.Select(value => JsonDocument.Parse(value).RootElement.Clone()),
            Declarations = filtered.Declarations.Select(value => JsonDocument.Parse(value).RootElement.Clone()),
            Edges = filtered.Edges.Select(value => JsonDocument.Parse(value).RootElement.Clone())
        });
        attempts.Add($"{label}-reencoded={Sha256(rawJson)}");
        // (c) compact fact stream
        var stream = string.Join('\n', filtered.Nodes.Concat(filtered.Declarations).Concat(filtered.Edges));
        attempts.Add($"{label}-stream={Sha256(stream)}");
    }

    var matched = attempts.Where(attempt => attempt.EndsWith("=" + expected, StringComparison.Ordinal)).ToArray();
    return matched.Length > 0
        ? attempts.Append($"MATCHED: {string.Join(",", matched)}").ToArray()
        : attempts.Append("none of the attempted serializations matches the recorded ff6b88... hash").ToArray();
}

sealed record FactCounts(int Nodes, int Declarations, int Edges);

sealed record UnitSnapshotUnit(string UnitId, string PluginVersion, string[] Nodes, string[] Declarations, string[] Edges);

sealed record UnitSnapshot(UnitSnapshotUnit[] Units);

sealed class Report
{
    public UnitSnapshot P8TypeScript { get; set; } = new([]);
    public UnitSnapshot P8JavaScript { get; set; } = new([]);
    public UnitSnapshot P9TypeScript { get; set; } = new([]);
    public UnitSnapshot P9JavaScript { get; set; } = new([]);
    public FactCounts P8TsCounts { get; set; } = new(0, 0, 0);
    public FactCounts P8JsCounts { get; set; } = new(0, 0, 0);
    public FactCounts P9TsCounts { get; set; } = new(0, 0, 0);
    public FactCounts P9JsCounts { get; set; } = new(0, 0, 0);
    public string P8TsPluginVersion { get; set; } = "";
    public string P8JsPluginVersion { get; set; } = "";
    public string P9TsResolverVersion { get; set; } = "";
    public string P9JsResolverVersion { get; set; } = "";
    public string P8TsResolverVersion { get; set; } = "";
    public string P8JsResolverVersion { get; set; } = "";
    public string[] P9TsUnitOriginVersions { get; set; } = [];
    public string[] P9JsUnitOriginVersions { get; set; } = [];
    public string[] P9TsExportNames { get; set; } = [];
    public string[] P9JsExportNames { get; set; } = [];
    public int P9TsExportEntries { get; set; }
    public int P9TsExportEdges { get; set; }
    public int P9JsExportEntries { get; set; }
    public int P9JsExportEdges { get; set; }
    public bool TypeScriptExistingFactsIdentical { get; set; }
    public bool JavaScriptExistingFactsIdentical { get; set; }
    public string TypeScriptExistingSnapshotSha256 { get; set; } = "";
    public string JavaScriptExistingSnapshotSha256 { get; set; } = "";
    public bool P9TsResolverUnchanged { get; set; }
    public bool P9JsResolverUnchanged { get; set; }
    public string[] P9TsExistingEvidenceVersions { get; set; } = [];
    public string[] P9JsExistingEvidenceVersions { get; set; } = [];
    public string[] P9TsExportEvidenceVersions { get; set; } = [];
    public string[] P9JsExportEvidenceVersions { get; set; } = [];
    public string[] ReopenCurrentLookup { get; set; } = [];
    public string[] ReopenNeighborhoodEdgeTargets { get; set; } = [];
    public string[] HashReconciliation { get; set; } = [];
}
