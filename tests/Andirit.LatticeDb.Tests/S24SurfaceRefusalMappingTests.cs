using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// WP-S24-2 P1 / IF-05: table-driven mapping of reachable store-open and
/// query-time failure conditions to their observable behavior on the real native
/// store. Each case is produced by tampering a genuine published database or by
/// publishing a genuine store state; no production code is modified. The mapping
/// is a contract record, not an invented universal catch-and-map policy: an
/// open-time exception is an open-time exception and is never claimed to be
/// converted to a query status.
/// </summary>
public sealed class S24SurfaceRefusalMappingTests
{
    private static readonly CodePluginId Plugin = new("plugin:s24-map");
    private static readonly DateTimeOffset Started = new(2026, 11, 9, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task If05_MappingMatrix_MatchesAcceptedContract()
    {
        var observed = new List<string>();

        observed.Add(await OpenCase("schema-version", "open:CodeOccurrenceSchemaException", async path =>
        {
            await SeedAsync(path, "repo:s24map:version", "run", 1);
            TamperMeta(path, "repo:s24map:version", json => json["SchemaVersion"] = 99);
        }));
        observed.Add(await OpenCase("schema-namespace", "open:CodeOccurrenceSchemaException", async path =>
        {
            await SeedAsync(path, "repo:s24map:namespace", "run", 1);
            TamperMeta(path, "repo:s24map:namespace", json => json["Namespace"] = "wrong/9");
        }));
        observed.Add(await OpenCase("units-without-meta", "open:CodeOccurrenceSchemaException", async path =>
        {
            await SeedAsync(path, "repo:s24map:nommeta", "run", 1);
            DeleteRow(path, "AndiritOccurrenceMeta", "repo:s24map:nommeta");
        }));
        observed.Add(await OpenCase("malformed-unit-row", "open:CodeOccurrenceSchemaException", async path =>
        {
            await SeedAsync(path, "repo:s24map:malformed", "run", 1);
            var key = UnitKey(path);
            WriteRawRow(path, "AndiritOccurrenceUnit", key, "not-json");
        }));
        observed.Add(await OpenCase("malformed-graph-unit-row", "open:JsonException", async path =>
        {
            await SeedAsync(path, "repo:s24map:malformedgraph", "run", 1);
            var key = RowKey(path, "HetuUnit");
            WriteRawRow(path, "HetuUnit", key, "not-json");
        }));
        observed.Add(await OpenCase("missing-graph-endpoint", "open:CodeGraphBatchRejectedException", async path =>
        {
            await SeedCrossUnitGraphAsync(path, "repo:s24map:endpoint");
            using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
            using var txn = db.BeginWriteTransaction();
            using (var q = db.Prepare("MATCH (s:HetuUnit) RETURN s.key"))
            using (var r = q.Execute(txn))
            {
                var keys = r.ReadAll().Select(row => row.GetValue(0).AsString()).Order(StringComparer.Ordinal).ToArray();
                var libKey = keys.Single(k => k.Contains("\nlib", StringComparison.Ordinal));
                using var d = db.Prepare("MATCH (s:HetuUnit) WHERE s.key = $k DELETE s");
                d.Bind("k", LatticeValue.From(libKey));
                using var dr = d.Execute(txn);
                dr.ReadAll();
            }
            txn.Commit();
        }));

        // Query-time cases (each its own published store state).
        observed.Add(await QueryCase("no-publication", "AnalysisUnavailable", path => Task.CompletedTask));
        observed.Add(await QueryCase("graph-only", "AnalysisUnavailable", path => SeedGraphOnlyAsync(path, "repo:s24map:graphonly")));
        observed.Add(await QueryCase("partial-coverage", "IncompleteAnalysis", path => SeedAsync(path, "repo:s24map:partial", "run", 2, CodeRelationshipCoverageState.Partial)));
        observed.Add(await QueryCase("complete-empty", "Available:IsComplete=true", path => SeedAsync(path, "repo:s24map:empty", "run", 0)));
        observed.Add(await QueryCase("stale-expected-publication", "StalePublication", path => SeedAsync(path, "repo:s24map:stale", "run", 2), staleExpectedPublication: true));
        observed.Add(await QueryCase("malformed-continuation", "Invalid", path => SeedAsync(path, "repo:s24map:token", "run", 2), token: "%%%"));

        var expected = new[]
        {
            "schema-version=open:CodeOccurrenceSchemaException",
            "schema-namespace=open:CodeOccurrenceSchemaException",
            "units-without-meta=open:CodeOccurrenceSchemaException",
            "malformed-unit-row=open:CodeOccurrenceSchemaException",
            "malformed-graph-unit-row=open:JsonException",
            "missing-graph-endpoint=open:CodeGraphBatchRejectedException",
            "no-publication=AnalysisUnavailable",
            "graph-only=AnalysisUnavailable",
            "partial-coverage=IncompleteAnalysis",
            "complete-empty=Available:IsComplete=True",
            "stale-expected-publication=StalePublication",
            "malformed-continuation=Invalid:Occurrences=0",
        };
        Assert.Equal(string.Join(";", expected), string.Join(";", observed));
    }

    // ---- case runners ----

    private static async Task<string> OpenCase(string name, string expected, Func<string, Task> arrange)
    {
        var path = TempPath(name);
        try
        {
            await arrange(path);
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                return name + "=open:no-exception";
            }
            catch (Exception exception)
            {
                return name + "=open:" + exception.GetType().Name;
            }
        }
        finally
        {
            Delete(path);
        }
    }

    private static async Task<string> QueryCase(
        string name, string expected, Func<string, Task> arrange, bool staleExpectedPublication = false, string? token = null)
    {
        var path = TempPath(name);
        try
        {
            await arrange(path);
            var repo = new CodeRepositoryId(RepoFor(name));
            using var store = new LatticeCodeGraphStore(path);
            var target = Target(repo, "T");
            CodeOccurrenceQuery query = staleExpectedPublication
                ? new CodeOccurrenceQuery(repo, target, new CodeGraphPublication(
                    repo, new CodeIndexRunId("run:foreign"), null, true, new CodeIndexIdentity("identity:foreign")))
                : new CodeOccurrenceQuery(repo, target) { ContinuationToken = token };
            try
            {
                var result = await store.FindOccurrencesAsync(query);
                return name switch
                {
                    "complete-empty" => name + "=" + result.Availability + ":IsComplete=" + result.IsComplete,
                    // D5 separation: an invalid continuation is a continuation state,
                    // not an availability state.
                    "malformed-continuation" => name + "=" + result.Continuation + ":Occurrences=" + result.Occurrences.Count,
                    _ => name + "=" + result.Availability,
                };
            }
            catch (Exception exception)
            {
                return name + "=query:" + exception.GetType().Name;
            }
        }
        finally
        {
            Delete(path);
        }
    }

    private static string RepoFor(string name) => "repo:s24map:" + (name switch
    {
        "no-publication" => "none",
        "graph-only" => "graphonly",
        "partial-coverage" => "partial",
        "complete-empty" => "empty",
        "stale-expected-publication" => "stale",
        "malformed-continuation" => "token",
        _ => "unknown",
    });

    // ---- seed / tamper helpers ----

    private static async Task SeedAsync(string path, string repoValue, string run, int facts, CodeRelationshipCoverageState state = CodeRelationshipCoverageState.Produced)
    {
        var repo = new CodeRepositoryId(repoValue);
        var target = Target(repo, "T");
        using var store = new LatticeCodeGraphStore(path);
        var list = Enumerable.Range(1, facts).Select(i => Fact(repo, "u", target, "src/A.cs", i, 1)).ToList();
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("u")),
            [Node("node:u")],
            occurrences: list,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(list.Count, 0, state)));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static async Task SeedGraphOnlyAsync(string path, string repoValue)
    {
        var repo = new CodeRepositoryId(repoValue);
        using var store = new LatticeCodeGraphStore(path);
        var runId = new CodeIndexRunId("run");
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("g")),
            [Node("node:g")]));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static async Task SeedCrossUnitGraphAsync(string path, string repoValue)
    {
        var repo = new CodeRepositoryId(repoValue);
        using var store = new LatticeCodeGraphStore(path);
        var runId = new CodeIndexRunId("run");
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("lib")),
            [new CodeGraphNode(new("node:lib"), CodeNodeKinds.Type, "Lib", "Lib", new CodeSymbolId("symbol:lib"))]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("app")),
            [new CodeGraphNode(new("node:app"), CodeNodeKinds.Type, "App", "App", new CodeSymbolId("symbol:app"))],
            edges: [new CodeGraphEdge(new("edge:app->lib"), new("node:app"), new("node:lib"),
                CodeEdgeKinds.DependsOn, new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"))]));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static void TamperMeta(string path, string repositoryId, Action<JsonObject> mutation)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        JsonObject row;
        using (var q = db.Prepare("MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k RETURN s.payload"))
        {
            q.Bind("k", LatticeValue.From(repositoryId));
            using var r = q.Execute(txn);
            row = JsonNode.Parse(Convert.FromBase64String(r.ReadAll()[0].GetValue(0).AsString()))!.AsObject();
        }
        mutation(row);
        using (var q = db.Prepare("MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k SET s.payload = $p"))
        {
            q.Bind("k", LatticeValue.From(repositoryId));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()))));
            using var r = q.Execute(txn);
            r.ReadAll();
        }
        txn.Commit();
    }

    private static void DeleteRow(string path, string label, string key)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        using var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s");
        q.Bind("k", LatticeValue.From(key));
        using var r = q.Execute(txn);
        r.ReadAll();
        txn.Commit();
    }

    private static void WriteRawRow(string path, string label, string key, string payload)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginWriteTransaction();
        using (var q = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k SET s.payload = $p"))
        {
            q.Bind("k", LatticeValue.From(key));
            q.Bind("p", LatticeValue.From(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))));
            using var r = q.Execute(txn);
            r.ReadAll();
        }
        txn.Commit();
    }

    private static string UnitKey(string path) => RowKey(path, "AndiritOccurrenceUnit");

    private static string RowKey(string path, string label)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = db.BeginReadTransaction();
        using var q = db.Prepare($"MATCH (s:{label}) RETURN s.key");
        using var r = q.Execute(txn);
        var key = r.ReadAll().Single().GetValue(0).AsString();
        txn.Commit();
        return key;
    }

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context, "meta:" + name, "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeOccurrenceFact Fact(CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column) =>
        CodeOccurrenceFact.Create(repo, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit), CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4), "sha256:source", "content:invocation");

    private static string TempPath(string name) =>
        Path.Combine(Path.GetTempPath(), $"andirit-s24map-{name}-{Guid.NewGuid():N}.ltdb");

    private static void Delete(string path)
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(path + suffix)) File.Delete(path + suffix);
    }
}
