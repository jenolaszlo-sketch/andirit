using System.Security.Cryptography;
using System.Text;
using LatticeDbSharp;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// ADR-CI1-01-CI1A-IMPL-01-S1 native vertical slice. These are the eight
/// mandatory native tests plus the D3 graph-noninterference corpus, executed
/// against the real <see cref="LatticeCodeGraphStore"/> and LatticeDB.
/// </summary>
public sealed class OccurrenceStage1NativeTests
{
    private static readonly CodePluginId Plugin = new("plugin:occurrence");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    // N1 — initial publication becomes durably queryable.
    [Fact]
    public async Task N1_InitialPublicationPersistsNativeFactsPagesAndMeta()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n1");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:n1"),
                    Unit(repo, "run:n1", "unit:n1",
                        [Node("node:n1")],
                        [
                            Fact(repo, "unit:n1", target, "src/A.cs", 10, 5),
                            Fact(repo, "unit:n1", target, "src/A.cs", 20, 5),
                            Fact(repo, "unit:n1", target, "src/B.cs", 5, 3)
                        ]));
                var result = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
                Assert.Equal(3, result.Occurrences.Count);
                Assert.Equal(3, result.Examined);
                Assert.False(result.Truncated);
                Assert.NotNull(result.Publication);
            }

            // Inspect the actual durable occurrence tables after close.
            Assert.Equal(1, CountLabel(path, "AndiritOccurrenceUnit"));
            Assert.Equal(1, CountLabel(path, "AndiritOccurrencePage"));
            Assert.Equal(1, CountLabel(path, "AndiritOccurrenceMeta"));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N2 — bounded, deterministically ordered target page.
    [Fact]
    public async Task N2_BoundedQueryIsDeterministicAndDoesNotScanUnrelatedTargets()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n2");
        var target = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            var facts = new List<CodeOccurrenceFact>
            {
                Fact(repo, "unit:n2", target, "src/C.cs", 3, 1),
                Fact(repo, "unit:n2", target, "src/A.cs", 9, 2),
                Fact(repo, "unit:n2", target, "src/A.cs", 2, 1),
                Fact(repo, "unit:n2", target, "src/B.cs", 1, 1),
                Fact(repo, "unit:n2", target, "src/A.cs", 2, 8)
            };
            for (var index = 0; index < 40; index++)
            {
                facts.Add(Fact(
                    repo,
                    "unit:n2",
                    Target(repo, $"other:{index}"),
                    $"src/Other{index}.cs",
                    1,
                    1));
            }

            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n2"),
                Unit(repo, "run:n2", "unit:n2", [Node("node:n2")], facts));

            var bounded = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(repo, target, maxResults: 2));
            Assert.Equal(CodeOccurrenceAvailability.Available, bounded.Availability);
            Assert.Equal(2, bounded.Occurrences.Count);
            Assert.Equal(5, bounded.TotalMatches);
            Assert.Equal(5, bounded.Examined); // only the target page, not the 40 unrelated facts
            Assert.True(bounded.Truncated);
            Assert.Equal("src/A.cs", bounded.Occurrences[0].Span.Path);
            Assert.Equal(2, bounded.Occurrences[0].Span.StartLine);
            Assert.Equal(1, bounded.Occurrences[0].Span.StartColumn);

            var full = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(repo, target, maxResults: 100));
            Assert.Equal(5, full.Occurrences.Count);
            Assert.Equal(
                ["src/A.cs:2:1", "src/A.cs:2:8", "src/A.cs:9:2", "src/B.cs:1:1", "src/C.cs:3:1"],
                full.Occurrences
                    .Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}")
                    .ToArray());
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N3 — unit replacement retires its obsolete facts and updates pages.
    [Fact]
    public async Task N3_ReplacementRetiresObsoleteFactsAndPreservesOtherUnits()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n3");
        var t1 = Target(repo, "T1");
        var t2 = Target(repo, "T2");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n3:1"),
                Unit(repo, "run:n3:1", "unit:n3:a", [Node("node:n3:a")],
                    [Fact(repo, "unit:n3:a", t1, "src/A.cs", 1, 1),
                     Fact(repo, "unit:n3:a", t2, "src/A.cs", 2, 1)]),
                Unit(repo, "run:n3:1", "unit:n3:b", [Node("node:n3:b")],
                    [Fact(repo, "unit:n3:b", t1, "src/B.cs", 1, 1)]));

            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n3:2"),
                Unit(repo, "run:n3:2", "unit:n3:a", [Node("node:n3:a")],
                    [Fact(repo, "unit:n3:a", t1, "src/A.cs", 5, 1)]));

            var first = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
            Assert.Equal(["src/A.cs:5:1", "src/B.cs:1:1"],
                first.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}").ToArray());
            var retired = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t2));
            Assert.Empty(retired.Occurrences);
            Assert.Equal(CodeOccurrenceAvailability.Available, retired.Availability);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N4 — contributor union, scoped deletion and conflict rejection.
    [Fact]
    public async Task N4_ContributorUnionAndScopedDeletionPreserveRemainingContributors()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n4");
        var t1 = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n4:1"),
                Unit(repo, "run:n4:1", "unit:n4:a", [Node("node:n4:a")],
                    [Fact(repo, "unit:n4:a", t1, "src/Shared.cs", 1, 1, "n4:semantic")]),
                Unit(repo, "run:n4:1", "unit:n4:b", [Node("node:n4:b")],
                    [Fact(repo, "unit:n4:b", t1, "src/Shared.cs", 1, 1, "n4:semantic")]),
                Unit(repo, "run:n4:1", "unit:n4:c", [Node("node:n4:c")],
                    [Fact(repo, "unit:n4:c", t1, "src/C.cs", 1, 1)]));

            var union = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
            Assert.Equal(2, union.Occurrences.Count);
            var shared = union.Occurrences.Single(hit => hit.Span.Path == "src/Shared.cs");
            Assert.Equal(2, shared.Contributors.Count);

            await PublishDeletionAsync(store, repo, "run:n4:2", "unit:n4:a");
            var afterFirstDeletion = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
            Assert.Equal(2, afterFirstDeletion.Occurrences.Count);
            shared = afterFirstDeletion.Occurrences.Single(hit => hit.Span.Path == "src/Shared.cs");
            Assert.Single(shared.Contributors);
            Assert.Equal("unit:n4:b", shared.Contributors[0].IndexUnitId);

            await PublishDeletionAsync(store, repo, "run:n4:3", "unit:n4:b");
            var afterSecondDeletion = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
            Assert.Single(afterSecondDeletion.Occurrences);
            Assert.Equal("src/C.cs", afterSecondDeletion.Occurrences[0].Span.Path);

            // A conflicting immutable payload for the same identity is rejected.
            await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:n4:4"), Started.AddSeconds(10), plugins: [Plugin]));
            // Same identity as the surviving unit:n4:c fact, incompatible payload.
            var conflicting = Fact(repo, "unit:n4:d", t1, "src/C.cs", 1, 1, "unit:n4:c");
            var differentPayload = new CodeOccurrenceFact(
                conflicting.Id,
                conflicting.TargetId,
                conflicting.TargetDocumentId,
                conflicting.RepositoryId,
                conflicting.Category,
                conflicting.SourceUnitId,
                new CodeOccurrenceContributor(Plugin, "unit:n4:d"),
                conflicting.Span,
                conflicting.SourceHash,
                contentHash: "content:different",
                conflicting.DeclarationSpan,
                conflicting.SemanticsVersion);
            await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await store.StageIndexUnitAsync(
                    new CodeIndexUnitReplacement(
                        new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run:n4:4"), new CodeIndexUnitId("unit:n4:d")),
                        [Node("node:n4:d")],
                        occurrences: [differentPayload],
                        occurrenceAnalysisIncluded: true)));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N5 — accepted native state survives close/reopen with identical ordering.
    [Fact]
    public async Task N5_ReopenPreservesOccurrenceAndIndexState()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n5");
        var target = Target(repo, "T1");
        try
        {
            CodeOccurrenceQueryResult before;
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:n5"),
                    Unit(repo, "run:n5", "unit:n5",
                        [Node("node:n5")],
                        [
                            Fact(repo, "unit:n5", target, "src/B.cs", 2, 1),
                            Fact(repo, "unit:n5", target, "src/A.cs", 3, 1),
                            Fact(repo, "unit:n5", target, "src/A.cs", 1, 1)
                        ]));
                before = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
            Assert.Equal(
                before.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}"),
                after.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}"));
            Assert.Equal(before.TotalMatches, after.TotalMatches);
            Assert.Equal(before.Examined, after.Examined);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N6 — failed publication preserves the prior accepted publication.
    [Fact]
    public async Task N6_FailedCompletionPreservesPriorOccurrencePublication()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n6");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            using (var store = new LatticeCodeGraphStore(path, null, point =>
            {
                if (fail && point == "before-commit")
                    throw new InjectedPersistenceException();
            }))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:n6:1"),
                    Unit(repo, "run:n6:1", "unit:n6", [Node("node:n6")],
                        [Fact(repo, "unit:n6", target, "src/Prior.cs", 1, 1)]));

                await store.StoreIndexRunAsync(new(
                    repo,
                    new CodeIndexRunId("run:n6:2"),
                    Started.AddSeconds(5),
                    plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:n6:2", "unit:n6", [Node("node:n6")],
                        [Fact(repo, "unit:n6", target, "src/New.cs", 1, 1)]));
                // Staged contributions are not visible before completion (D4 #5).
                var staged = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Single(staged.Occurrences);
                Assert.Equal("src/Prior.cs", staged.Occurrences[0].Span.Path);
                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:n6:2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:n6:2"), [])));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
            Assert.Single(after.Occurrences);
            Assert.Equal("src/Prior.cs", after.Occurrences[0].Span.Path);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N7 — stale publication and graph-only stores refuse explicitly.
    [Fact]
    public async Task N7_StalePublicationAndGraphOnlyStoresRefuseExplicitly()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:n7");
        var target = Target(repo, "T1");
        try
        {
            using var store = new LatticeCodeGraphStore(path);
            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n7:1"),
                Unit(repo, "run:n7:1", "unit:n7", [Node("node:n7")],
                    [Fact(repo, "unit:n7", target, "src/A.cs", 1, 1)]));
            var firstPublication = await store.GetLatestPublicationAsync(repo);
            Assert.NotNull(firstPublication);

            await PublishAsync(
                store,
                repo,
                new CodeIndexRunId("run:n7:2"),
                Unit(repo, "run:n7:2", "unit:n7", [Node("node:n7")],
                    [Fact(repo, "unit:n7", target, "src/B.cs", 1, 1)]));

            var stale = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(repo, target, firstPublication));
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);

            var current = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
            Assert.Equal("src/B.cs", current.Occurrences[0].Span.Path);

            // A graph-only repository/publication reports analysis unavailable.
            var graphOnly = new CodeRepositoryId("repo:n7:graph-only");
            await PublishAsync(
                store,
                graphOnly,
                new CodeIndexRunId("run:n7:graph"),
                Unit(graphOnly, "run:n7:graph", "unit:n7:graph", [Node("node:n7:graph")], []));
            var unavailable = await store.FindOccurrencesAsync(
                new CodeOccurrenceQuery(graphOnly, target));
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, unavailable.Availability);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // N8 — D3 legacy graph noninterference across the required events.
    [Fact]
    public async Task N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:d3");
        var target = Target(repo, "T1");
        var fail = false;
        string baseline;
        try
        {
            using (var store = new LatticeCodeGraphStore(path, null, point =>
            {
                if (fail && point == "before-commit")
                    throw new InjectedPersistenceException();
            }))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:d3:1"),
                    GraphUnit(repo, "run:d3:1", "unit:d3:a", "A"),
                    GraphUnit(repo, "run:d3:1", "unit:d3:b", "B"));
                baseline = await ObserveGraphAsync(store, repo);

                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:d3:3"),
                    GraphUnit(repo, "run:d3:3", "unit:d3:a", "A",
                        Fact(repo, "unit:d3:a", target, "src/A.cs", 10, 1)),
                    Unit(repo, "run:d3:3", "unit:d3:occ", [],
                        [Fact(repo, "unit:d3:occ", target, "src/Occ.cs", 1, 1)]));
                Assert.Equal(baseline, await ObserveGraphAsync(store, repo));

                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:d3:4"),
                    Unit(repo, "run:d3:4", "unit:d3:occ", [],
                        [Fact(repo, "unit:d3:occ", target, "src/Occ.cs", 2, 1)]));
                Assert.Equal(baseline, await ObserveGraphAsync(store, repo));

                await PublishDeletionAsync(store, repo, "run:d3:5", "unit:d3:occ");
                Assert.Equal(baseline, await ObserveGraphAsync(store, repo));

                await store.StoreIndexRunAsync(new(
                    repo,
                    new CodeIndexRunId("run:d3:6"),
                    Started.AddSeconds(20),
                    plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    GraphUnit(repo, "run:d3:6", "unit:d3:a", "A",
                        Fact(repo, "unit:d3:a", target, "src/A.cs", 99, 1)));
                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:d3:6"), Started.AddSeconds(20),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(21), [Plugin]),
                        new(repo, new CodeIndexRunId("run:d3:6"), [])));
                Assert.Equal(baseline, await ObserveGraphAsync(store, repo));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            Assert.Equal(baseline, await ObserveGraphAsync(reopened, repo));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // SQ-03 / D5 — unsupported occurrence schema is refused explicitly.
    [Fact]
    public async Task Store_RejectsUnsupportedOccurrenceSchemaOnOpen()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:schema");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:schema"),
                    Unit(repo, "run:schema", "unit:schema", [Node("node:schema")],
                        [Fact(repo, "unit:schema", target, "src/A.cs", 1, 1)]));
            }

            TamperOccurrenceMetaVersion(path, repo.Value, 99);

            var exception = Assert.Throws<CodeOccurrenceSchemaException>(
                () => new LatticeCodeGraphStore(path));
            Assert.Equal(99, exception.ActualVersion);
            Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, exception.ExpectedVersion);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // D5 — occurrence records without a schema manifest are malformed, not empty.
    [Fact]
    public async Task Store_RejectsOccurrenceUnitsWithoutMeta()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:schema-missing");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:schema-missing"),
                    Unit(repo, "run:schema-missing", "unit:schema-missing", [Node("node:schema-missing")],
                        [Fact(repo, "unit:schema-missing", target, "src/A.cs", 1, 1)]));
            }

            DeleteLabelRow(path, "AndiritOccurrenceMeta", repo.Value);

            Assert.Throws<CodeOccurrenceSchemaException>(
                () => new LatticeCodeGraphStore(path));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ---- helpers -----------------------------------------------------------

    private static void TamperOccurrenceMetaVersion(string path, string repositoryId, int version)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginWriteTransaction();
        var meta = new PersistedOccurrenceMeta(
            version,
            LatticeCodeGraphStore.OccurrenceNamespace,
            repositoryId,
            "run:tampered",
            "identity:tampered",
            null,
            false,
            1,
            1,
            1,
            true);
        var payload = Convert.ToBase64String(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(meta));
        using var query = database.Prepare(
            "MATCH (s:AndiritOccurrenceMeta) WHERE s.key = $k SET s.payload = $p");
        query.Bind("k", LatticeValue.From(repositoryId));
        query.Bind("p", LatticeValue.From(payload));
        using var result = query.Execute(txn);
        result.ReadAll();
        txn.Commit();
    }

    private static void DeleteLabelRow(string path, string label, string key)
    {
        using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginWriteTransaction();
        using var query = database.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s");
        query.Bind("k", LatticeValue.From(key));
        using var result = query.Execute(txn);
        result.ReadAll();
        txn.Commit();
    }

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
        CodeRepositoryId repo,
        string unitId,
        CodeOccurrenceTargetId target,
        string path,
        int line,
        int column,
        string? semanticUnitId = null) =>
        CodeOccurrenceFact.Create(
            repo,
            target,
            target.Value,
            semanticUnitId ?? unitId,
            new CodeOccurrenceContributor(Plugin, unitId),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            sourceHash: "sha256:source",
            contentHash: "content:invocation");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id,
            new CodeSymbolId($"symbol:{id}"));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo,
        string run,
        string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeOccurrenceFact> occurrences) =>
        new(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            nodes,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences.Count > 0,
            occurrenceCoverage: occurrences.Count > 0
                ? new CodeOccurrenceCoverage(
                    occurrences.Count,
                    0,
                    CodeRelationshipCoverageState.Produced)
                : null);

    private static CodeIndexUnitReplacement GraphUnit(
        CodeRepositoryId repo,
        string run,
        string unit,
        string tag,
        params CodeOccurrenceFact[] occurrences)
    {
        var owner = $"node:d3:{tag}:owner";
        var callee = $"node:d3:{tag}:callee";
        var caller = $"node:d3:{tag}:caller";
        var nodes = new[]
        {
            Node(owner),
            Node(callee),
            Node(caller)
        };
        var edges = new[]
        {
            new CodeGraphEdge(
                new CodeEdgeId($"edge:d3:{tag}:calls"),
                new CodeNodeId(caller),
                new CodeNodeId(callee),
                CodeEdgeKinds.Calls,
                new CodeEvidence(EvidenceQuality.Resolved, "plugin:occurrence", "1.0.0")),
            new CodeGraphEdge(
                new CodeEdgeId($"edge:d3:{tag}:references"),
                new CodeNodeId(caller),
                new CodeNodeId(owner),
                CodeEdgeKinds.References,
                new CodeEvidence(EvidenceQuality.Resolved, "plugin:occurrence", "1.0.0"))
        };
        return new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            nodes,
            edges: edges,
            occurrences: occurrences,
            occurrenceAnalysisIncluded: occurrences.Length > 0,
            occurrenceCoverage: occurrences.Length > 0
                ? new CodeOccurrenceCoverage(
                    occurrences.Length,
                    0,
                    CodeRelationshipCoverageState.Produced)
                : null);
    }

    private static async Task PublishAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repo,
        CodeIndexRunId run,
        params CodeIndexUnitReplacement[] units)
    {
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, run, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, run, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new(repo, run, []));
    }

    private static async Task PublishDeletionAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repo,
        string run,
        string unit)
    {
        var runId = new CodeIndexRunId(run);
        await store.StoreIndexRunAsync(new(repo, runId, Started.AddSeconds(30), plugins: [Plugin]));
        await store.StageIndexUnitDeletionAsync(repo, runId, Plugin, new CodeIndexUnitId(unit));
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started.AddSeconds(30), CodeIndexRunStatus.Completed, Started.AddSeconds(31), [Plugin]),
            new(repo, runId, []));
    }

    private static async Task<string> ObserveGraphAsync(
        LatticeCodeGraphStore store,
        CodeRepositoryId repo)
    {
        var units = await store.GetPublishedUnitsAsync(repo);
        var nodes = units.SelectMany(unit => unit.Nodes)
            .Select(node => node.Id.Value).Order(StringComparer.Ordinal).ToArray();
        var edges = units.SelectMany(unit => unit.Edges)
            .Select(edge => edge.Id.Value).Order(StringComparer.Ordinal).ToArray();
        var callers = await store.TraverseAsync(repo, new(
            new CodeNodeId("node:d3:A:callee"),
            CodeGraphDirection.Incoming,
            [CodeEdgeKinds.Calls]));
        var references = await store.TraverseAsync(repo, new(
            new CodeNodeId("node:d3:A:owner"),
            CodeGraphDirection.Incoming,
            [CodeEdgeKinds.References]));
        var node = await store.GetNodeAsync(repo, new CodeNodeId("node:d3:A:caller"));
        return string.Join(
            "|",
            $"nodes=[{string.Join(",", nodes)}]",
            $"edges=[{string.Join(",", edges)}]",
            $"callers=[{string.Join(",", callers.Nodes.Select(item => item.Id.Value))}]",
            $"references=[{string.Join(",", references.Nodes.Select(item => item.Id.Value))}]",
            $"caller={node?.QualifiedName}");
    }

    private static int CountLabel(string path, string label)
    {
        using var database = LatticeDatabase.Open(
            path,
            new LatticeDatabaseOptions { Create = false });
        using var txn = database.BeginReadTransaction();
        using var query = database.Prepare($"MATCH (s:{label}) RETURN count(s)");
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows.Count == 0 ? 0 : (int)rows[0].GetValue(0).AsInt64();
    }

    private static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-occ-{Guid.NewGuid():N}.ltdb");

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

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class InjectedPersistenceException : Exception;
}
