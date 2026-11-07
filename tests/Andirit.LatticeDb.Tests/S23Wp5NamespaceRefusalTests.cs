using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// WP-S23-5 I-04 / SQ-06: isolated occurrence-namespace refusal. The store's
/// open path validates both the occurrence schema version and the occurrence
/// namespace; the accepted Stage-1 regression suite exercises only the version
/// branch (<see cref="OccurrenceStage1NativeTests.Store_RejectsUnsupportedOccurrenceSchemaOnOpen"/>
/// tampers the version to 99) and the missing-manifest branch
/// (<c>Store_RejectsOccurrenceUnitsWithoutMeta</c>). This additive native
/// regression tampers only the persisted <c>Namespace</c> while keeping the
/// schema version valid, and proves the open is refused with the appropriate
/// schema exception. No production code is changed.
/// </summary>
public sealed class S23Wp5NamespaceRefusalTests
{
    private static readonly CodePluginId Plugin = new("plugin:wp5-namespace");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Store_RejectsUnsupportedOccurrenceNamespaceOnOpen()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:wp5:namespace");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    "run:namespace",
                    Unit(repo, "run:namespace", "unit:namespace", [Node("node:namespace")],
                        [Fact(repo, "unit:namespace", target, "src/A.cs", 1, 1)]));
            }

            // Positive control: the untouched store opens and serves.
            using (var control = new LatticeCodeGraphStore(path))
            {
                var result = await control.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
                Assert.Single(result.Occurrences);
            }

            // Isolated namespace tamper: the schema version stays valid, only
            // the persisted occurrence namespace is changed.
            TamperOccurrenceNamespace(path, repo.Value, "wrong-occurrence-facts/9");
            AssertValidSchemaVersion(path, repo.Value);

            var exception = Assert.Throws<CodeOccurrenceSchemaException>(
                () => new LatticeCodeGraphStore(path));
            Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, exception.ExpectedVersion);
            // Distinguishing property: a namespace mismatch keeps a *valid*
            // version, unlike the version-mismatch control (ActualVersion == 99).
            Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, exception.ActualVersion);
            Assert.Contains("namespace", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // The persisted version is unchanged by the namespace tamper; this guards
    // the "isolated" property of the test above.
    private static void AssertValidSchemaVersion(string path, string repositoryId)
    {
        var row = MetaPayload(path, repositoryId);
        Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, row["SchemaVersion"]!.GetValue<int>());
    }

    private static void TamperOccurrenceNamespace(string path, string repositoryId, string replacementNamespace)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginWriteTransaction();
        var row = MetaPayload(database, txn, repositoryId);
        row["Namespace"] = replacementNamespace;
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(row.ToJsonString()));
        using (var query = database.Prepare(
            "MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k SET s.payload = $p"))
        {
            query.Bind("k", LatticeValue.From(repositoryId));
            query.Bind("p", LatticeValue.From(payload));
            using var result = query.Execute(txn);
            result.ReadAll();
        }
        txn.Commit();
    }

    private static JsonObject MetaPayload(string path, string repositoryId)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginReadTransaction();
        var row = MetaPayload(database, txn, repositoryId);
        txn.Commit();
        return row;
    }

    private static JsonObject MetaPayload(LatticeDatabase database, LatticeTransaction txn, string repositoryId)
    {
        using var query = database.Prepare(
            "MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k RETURN s.payload");
        query.Bind("k", LatticeValue.From(repositoryId));
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        Assert.Single(rows);
        return JsonNode.Parse(Convert.FromBase64String(rows[0].GetValue(0).AsString()))!.AsObject();
    }

    // ---- minimal native publication helpers (mirrors the accepted Stage-1 suite) ----

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
    {
        var context = "compctx1:sha256:" + Hex(repo.Value + "|" + name);
        return CodeOccurrenceIdentity.ComputeTargetId(
            context,
            "meta:" + name,
            "module:" + name,
            CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
            CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
            CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
    }

    private static string Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target, string path, int line, int column) =>
        CodeOccurrenceFact.Create(
            repo,
            target,
            target.Value,
            unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source",
            "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes, IReadOnlyList<CodeOccurrenceFact> occurrences) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            nodes,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences.Count > 0,
            occurrenceCoverage: new CodeOccurrenceCoverage(
                occurrences.Count, 0, CodeRelationshipCoverageState.Produced));

    private static async Task PublishAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run, params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-wp5-ns-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            try
            {
                if (File.Exists(candidate))
                    File.Delete(candidate);
            }
            catch
            {
            }
        }
    }
}
