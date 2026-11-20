using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

internal static partial class Probe
{
    private sealed class GraphOnlySink(CodeGraphIngestionSink inner) : ICodeGraphSink
    {
        public CodeGraphBatchLimits Limits => inner.Limits;

        public ValueTask WriteBatchAsync(CodeGraphBatch batch, CancellationToken ct = default) =>
            inner.WriteBatchAsync(new(batch.Origin, batch.Nodes, batch.Declarations, batch.Edges,
                batch.CompletesIndexUnit), ct);
    }

    private static async Task PublishGraphOnly(
        LatticeCodeGraphStore store, CodeRepositoryId repo, Dictionary<string, byte[]> files)
    {
        await Run(store, repo, "graph-only", [CSharp.Id], async () =>
        {
            await using var ingest = new CodeGraphIngestionSink(store);
            await using var session = await CSharp.CreateSessionAsync(Context(repo, "graph-only", files));
            await session.ExtractAsync(new GraphOnlySink(ingest));
            await ingest.CompleteAsync();
        });
    }

    private static async Task<object> GraphSnapshot(LatticeCodeGraphStore store, CodeRepositoryId repo)
    {
        var units = await store.GetPublishedUnitsAsync(repo);
        var nodes = units.SelectMany(unit => unit.Nodes).DistinctBy(node => node.Id)
            .OrderBy(node => node.Id.Value, StringComparer.Ordinal).ToArray();
        var edges = units.SelectMany(unit => unit.Edges).DistinctBy(edge => edge.Id)
            .OrderBy(edge => edge.Id.Value, StringComparer.Ordinal).ToArray();
        var method = nodes.Single(node => node.Name == "Mark");
        var legacy = new CodeGraphQueryService(store);
        var limits = new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 200, maxEdges: 500);
        var callers = await legacy.FindCallersAsync(repo, method.Id, limits);
        var references = await legacy.FindReferencesAsync(repo, method.Id, limits);
        var traversal = await store.TraverseAsync(repo, new(method.Id, CodeGraphDirection.Incoming,
            [CodeEdgeKinds.Calls], maxDepth: 1, maxNodes: 200, maxEdges: 500));
        Check(callers.Edges.Count > 0, "Real fixture's Mark declaration has legacy callers");
        return new
        {
            unitCount = units.Count,
            nodes = nodes.Select(node => new { id = node.Id.Value, node.Name, node.QualifiedName }).ToArray(),
            edges = edges.Select(edge => edge.Id.Value).ToArray(),
            callers = Project(callers),
            references = Project(references),
            traversal = Project(traversal)
        };
    }

    private static object Project(CodeGraphTraversalResult result) => new
    {
        nodes = result.Nodes.Select(point => point.Id.Value).Order(StringComparer.Ordinal).ToArray(),
        edges = result.Edges.Select(edge => edge.Id.Value).Order(StringComparer.Ordinal).ToArray(),
        result.Truncated
    };

    private static async Task GraphCase(string path, string output, string fixtures)
    {
        var repo = new CodeRepositoryId("review:s254:graph");
        var files = Directory.EnumerateFiles(fixtures, "*", SearchOption.AllDirectories)
            .ToDictionary(file => Path.GetRelativePath(fixtures, file).Replace('\\', '/'), File.ReadAllBytes);
        var mirror = new LatticeDbStoreOptions { UseNativeTraversal = true };
        object graphOnly;
        object integrated;
        using (var store = new LatticeCodeGraphStore(path, mirror))
        {
            await PublishGraphOnly(store, repo, files);
            graphOnly = await GraphSnapshot(store, repo);
            var unavailable = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo,
                new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('0', 64))));
            Receipt["graphOnlyOccurrence"] = unavailable;
            Check(unavailable.Availability == CodeOccurrenceAvailability.AnalysisUnavailable && !unavailable.IsComplete,
                "Graph-only store remains readable while occurrence analysis is unavailable");
        }
        Receipt["graphOnlyRows"] = ReadNative(path);

        var clock = Stopwatch.StartNew();
        using (var store = new LatticeCodeGraphStore(path, mirror))
        {
            Receipt["graphOnlyReopenMs"] = clock.Elapsed.TotalMilliseconds;
            clock.Restart();
            await PublishCSharp(store, repo, "integrated", files);
            Receipt["integratedPublicationMs"] = clock.Elapsed.TotalMilliseconds;
            integrated = await GraphSnapshot(store, repo);
        }

        Receipt["graphOnly"] = graphOnly;
        Receipt["integrated"] = integrated;
        Check(JsonSerializer.Serialize(graphOnly) == JsonSerializer.Serialize(integrated),
            "Graph-only and integrated node, edge, caller, reference and mirror snapshots agree exactly");
        Receipt["integratedRows"] = ReadNative(path);

        clock.Restart();
        using (var store = new LatticeCodeGraphStore(path, mirror))
        {
            Receipt["integratedReopenMs"] = clock.Elapsed.TotalMilliseconds;
            Receipt["nativeTraversalAfterReopen"] = await GraphSnapshot(store, repo);
        }

        using (var store = new LatticeCodeGraphStore(path))
            Receipt["managedTraversalAfterReopen"] = await GraphSnapshot(store, repo);

        Check(JsonSerializer.Serialize(graphOnly) == JsonSerializer.Serialize(Receipt["nativeTraversalAfterReopen"]),
            "Native mirror graph is preserved after reopen");
        Check(JsonSerializer.Serialize(graphOnly) == JsonSerializer.Serialize(Receipt["managedTraversalAfterReopen"]),
            "Native mirror and managed traversal agree");
    }

    private static void Tamper(string phase, string path)
    {
        var label = phase == "d5-page" ? "AndiritOccurrencePage" : "AndiritOccurrenceMeta";
        var property = phase switch
        {
            "d5-page" => "PublicationIndexRunId",
            "d5-namespace" => "Namespace",
            "d5-schema" => "SchemaVersion",
            _ => throw new ArgumentException("Unrecognized tamper case")
        };
        using (var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false }))
        using (var txn = db.BeginWriteTransaction())
        {
            string key;
            JsonObject row;
            using (var lookup = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload"))
            using (var result = lookup.Execute(txn))
            {
                var existing = result.ReadAll().Single();
                key = existing.GetValue(0).AsString();
                row = JsonNode.Parse(Convert.FromBase64String(existing.GetValue(1).AsString()))!.AsObject();
            }

            Receipt["before"] = row.DeepClone();
            row[property] = phase == "d5-schema" ? JsonValue.Create(99) : JsonValue.Create("not-the-approved-value");
            Receipt["after"] = row.DeepClone();

            using var command = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p");
            command.Bind("k", LatticeValue.From(key));
            command.Bind("p", LatticeValue.From(Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(row))));
            using var changed = command.Execute(txn);
            changed.ReadAll();
            txn.Commit();
        }

        var expected = phase == "d5-page" ? "CodeOccurrenceIndexException" : "CodeOccurrenceSchemaException";
        Receipt["expectedException"] = expected;
        try
        {
            using var opened = new LatticeCodeGraphStore(path);
            Failures.Add("Tampered native store opened");
        }
        catch (Exception ex)
        {
            Receipt["observedException"] = ex.GetType().Name;
            Receipt["refusalMessage"] = ex.Message;
            Check(ex.GetType().Name == expected, "Distinct expected open-time refusal");
        }
    }
}

