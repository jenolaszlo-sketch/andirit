using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Penghou.Hetu;
using Penghou.Hetu.LatticeDb;

// Evidence tooling only. Never point this probe at a live or sole-copy store.
// Bind the same probe separately to the exact p6, p8 and integrated p9 sources.
if (args.Length != 5) throw new ArgumentException("seed|index|inspect|cancel workspace report label projection|native");
var (operation, root, reportPath, label, profile) = (args[0], args[1], args[2], args[3], args[4]);
var database = Path.Combine(root, "graph.db");
if (operation == "seed")
{
    if (Directory.Exists(root)) throw new InvalidOperationException("Seed workspace must not exist.");
    foreach (var language in new[] { "ts", "js" }) Directory.CreateDirectory(Path.Combine(root, language, "src"));
    File.WriteAllText(Path.Combine(root, "ts", "src", "main.ts"), "import { type Remote } from \"./dep\";\nexport const value = 1;\nexport function run() { return value; }\nexport { value as current };\nexport { remote as forwarded } from \"./dep\";\n");
    File.WriteAllText(Path.Combine(root, "ts", "src", "dep.ts"), "export interface Remote {}\nexport const remote = 2;");
    File.WriteAllText(Path.Combine(root, "js", "src", "main.js"), "import { item } from \"./dep\";\nexport const value = 1;\nexport default function run() { return item; }\nexport { value as current };\nexport { remote as forwarded } from \"./dep\";\n");
    File.WriteAllText(Path.Combine(root, "js", "src", "dep.js"), "export const item = 1;\nexport const remote = 2;");
}
else if (!File.Exists(database)) throw new InvalidOperationException("Existing disposable store is required.");
var statuses = new List<object>();
var options = new LatticeDbStoreOptions { UseNativeTraversal = profile == "native" };
if (operation is "seed" or "index" or "cancel")
{
    using var store = new LatticeCodeGraphStore(database, options);
    foreach (var language in new[] { "ts", "js" })
    {
        ICodeGraphPlugin plugin = language == "ts" ? new TreeSitterTypeScriptPlugin() : new TreeSitterJavaScriptPlugin();
        var indexing = new CodeIndexingService(new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]), new CodeGraphPluginRegistry([plugin]), store);
        if (operation == "cancel")
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            try
            {
                var result = await indexing.IndexAsync(new CodeRepositoryDescriptor(new CodeRepositoryId("repo:q1-migration-" + language), Path.Combine(root, language)), new CodeIndexRunId("run:int-" + label + "-" + language), cancellationToken: cancellation.Token);
                statuses.Add(new { language, status = result.Diagnostics.Status.ToString(), result.Diagnostics.PluginsExecuted });
                if (result.Diagnostics.Status == CodeIndexRunStatus.Completed) throw new InvalidOperationException("Cancelled migration silently reported success.");
            }
            catch (OperationCanceledException) { statuses.Add(new { language, status = "OperationCanceledException" }); }
        }
        else
        {
            var result = await indexing.IndexAsync(new CodeRepositoryDescriptor(new CodeRepositoryId("repo:q1-migration-" + language), Path.Combine(root, language)), new CodeIndexRunId("run:int-" + label + "-" + language));
            statuses.Add(new { language, status = result.Diagnostics.Status.ToString(), result.Diagnostics.PluginsExecuted });
            if (result.Diagnostics.Status != CodeIndexRunStatus.Completed) throw new InvalidOperationException("Index operation did not complete.");
        }
    }
}
// A separate open proves durability instead of reading the writer's projection.
var observations = new List<object>();
using (var store = new LatticeCodeGraphStore(database, options))
{
    var health = await store.CheckHealthAsync();
    if (!health.IsHealthy || health.SchemaVersion != 2) throw new InvalidOperationException("Unhealthy durable store.");
    foreach (var language in new[] { "ts", "js" })
    {
        var repository = new CodeRepositoryId("repo:q1-migration-" + language);
        var units = await store.GetPublishedUnitsAsync(repository);
        var nodes = units.SelectMany(u => u.Nodes).Select(n => new Fact(n.Id.Value, n.Kind.Value, Canonical(n))).OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        var edges = units.SelectMany(u => u.Edges).Select(e => new Fact(e.Id.Value, e.Kind.Value, Canonical(e))).OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        var declarations = units.SelectMany(u => u.Declarations).Select(d => new Fact(d.Id.Value, "declaration", Canonical(d))).OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();
        foreach (var group in new[] { nodes, edges, declarations }) if (group.GroupBy(f => f.Id).Any(g => g.Count() != 1)) throw new InvalidOperationException("Duplicate fact identity.");
        var ids = nodes.Select(n => n.Id).ToHashSet(StringComparer.Ordinal);
        if (units.SelectMany(u => u.Edges).Any(e => !ids.Contains(e.SourceId.Value) || !ids.Contains(e.TargetId.Value))) throw new InvalidOperationException("Dangling edge.");
        var queries = new CodeGraphQueryService(store);
        var lookup = await queries.FindSymbolsByQualifiedNameAsync(repository, "value");
        var current = await queries.FindSymbolsByQualifiedNameAsync(repository, "current");
        var lang = language == "ts" ? "typescript" : "javascript";
        var path = "src/main." + language;
        var file = new CodeNodeId(lang + ":file:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path))));
        var neighborhood = await queries.GetNeighborhoodAsync(repository, file);
        observations.Add(new {
            language,
            units = units.Select(u => new { id = u.Origin.IndexUnitId.Value, version = u.Origin.PluginVersion }).OrderBy(u => u.id, StringComparer.Ordinal),
            nodes, declarations, edges,
            lookup = lookup.Candidates.Select(n => n.Id.Value).OrderBy(x => x, StringComparer.Ordinal),
            exportLookup = current.Candidates.Select(n => n.Id.Value).OrderBy(x => x, StringComparer.Ordinal),
            defaultNeighborhoodNodeIds = neighborhood.Nodes.Select(n => n.Id.Value).OrderBy(x => x, StringComparer.Ordinal),
            defaultNeighborhoodEdgeIds = neighborhood.Edges.Select(e => e.Id.Value).OrderBy(x => x, StringComparer.Ordinal)
        });
    }
    File.WriteAllText(reportPath, JsonSerializer.Serialize(new { operation, label, profile, sdk = "10.0.401", provider = typeof(LatticeCodeGraphStore).Assembly.GetName().Version?.ToString(), engine = typeof(LatticeDbSharp.LatticeDatabase).Assembly.GetName().Version?.ToString(), health, statuses, observations }, new JsonSerializerOptions { WriteIndented = true }));
}
Console.WriteLine($"{label}: {profile} {operation}; reopened healthy schema 2; report {reportPath}");

static string Canonical<T>(T value)
{
    using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
    using var stream = new MemoryStream();
    using (var writer = new Utf8JsonWriter(stream)) Write(writer, document.RootElement);
    return Encoding.UTF8.GetString(stream.ToArray());
}
static void Write(Utf8JsonWriter writer, JsonElement element)
{
    if (element.ValueKind == JsonValueKind.Object)
    {
        writer.WriteStartObject();
        foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal)) { writer.WritePropertyName(property.Name); Write(writer, property.Value); }
        writer.WriteEndObject();
    }
    else if (element.ValueKind == JsonValueKind.Array) { writer.WriteStartArray(); foreach (var item in element.EnumerateArray()) Write(writer, item); writer.WriteEndArray(); }
    else element.WriteTo(writer);
}
record Fact(string Id, string Kind, string Json);
