using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.LatticeDb;

namespace Andirit.LatticeDb.Tests;

/// <summary>
/// NEW-01 recovery-projection regressions. Following a definitively resolved
/// known-failure recovery, the recovered in-memory graph and occurrence state must
/// correspond to the same authoritative durable publication. Before the
/// remediation these tests fail because the recovery branches rebuilt only the
/// graph projection (see the implementation report for the before-fix evidence).
/// </summary>
public sealed class New01RecoveryProjectionTests
{
    private static readonly CodePluginId Plugin = new("plugin:new01");
    private static readonly DateTimeOffset Started =
        new(2026, 11, 1, 0, 0, 0, TimeSpan.Zero);

    // Mandatory class A: pre-commit failure during a "complete" mutation.
    [Fact]
    public async Task Recovery_AfterPreCommitFailure_MatchesDurableOccurrenceProjection()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:precommit");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            CodeOccurrenceQueryResult recovered;
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (fail && point == "before-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store, repo, "run:p1",
                    Unit(repo, "run:p1", "unit:p1", [Node("node:p1")],
                        [
                            Fact(repo, "unit:p1", target, "src/Prior.cs", 3, 1),
                            Fact(repo, "unit:p1", target, "src/Prior.cs", 7, 1)
                        ],
                        CodeRelationshipCoverageState.Produced));

                var baseline = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, baseline.Availability);
                Assert.Equal(2, baseline.Occurrences.Count);

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:p2"), Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:p2", "unit:p2", [Node("node:p2")],
                        [Fact(repo, "unit:p2", target, "src/New.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));

                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:p2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:p2"), [])));

                // The defect: the recovered in-process projection must still agree
                // with the authoritative durable publication.
                recovered = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            }

            // Fresh reopen of the same durable state is the independent oracle.
            using var reopened = new LatticeCodeGraphStore(path);
            var durable = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));

            Assert.Equal(CodeOccurrenceAvailability.Available, durable.Availability);
            Assert.Equal(2, durable.Occurrences.Count);

            Assert.Equal(durable.Availability, recovered.Availability);
            Assert.Equal(
                durable.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}"),
                recovered.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}"));
            Assert.Equal(durable.Publication, recovered.Publication);
            Assert.NotNull(recovered.Coverage);
            Assert.True(recovered.Coverage!.IsComplete);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // Mandatory class B: in-memory apply failure. A conflicting staged payload is
    // rejected by the inner store, triggering the recovery branch.
    [Fact]
    public async Task Recovery_AfterInMemoryApplyFailure_PreservesCommittedOccurrenceProjection()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:apply");
        var target = Target(repo, "T1");
        try
        {
            CodeOccurrenceQueryResult recovered;
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(
                    store, repo, "run:a1",
                    Unit(repo, "run:a1", "unit:a1", [Node("node:a1")],
                        [Fact(repo, "unit:a1", target, "src/Prior.cs", 4, 1)],
                        CodeRelationshipCoverageState.Produced));

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:a2"), Started.AddSeconds(5), plugins: [Plugin]));

                // Same identity tuple as the committed fact, different immutable
                // payload (contentHash is payload, not identity) -> inner validation
                // rejects the staged replacement before any native write.
                var committed = Fact(repo, "unit:a1", target, "src/Prior.cs", 4, 1);
                var conflicting = CodeOccurrenceFact.Create(
                    repo,
                    target,
                    target.Value,
                    "unit:a1",
                    new CodeOccurrenceContributor(Plugin, "unit:a1"),
                    CodeOccurrenceCategory.Invocation,
                    committed.Span,
                    "sha256:source",
                    "content:conflicting");

                await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                    await store.StageIndexUnitAsync(
                        Unit(repo, "run:a2", "unit:a2", [Node("node:a2")],
                            [conflicting], CodeRelationshipCoverageState.Produced)));

                recovered = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var durable = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));

            Assert.Equal(CodeOccurrenceAvailability.Available, durable.Availability);
            Assert.Single(durable.Occurrences);
            Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
            Assert.Single(recovered.Occurrences);
            Assert.Equal(durable.Publication, recovered.Publication);
            // The rejected conflicting contribution must not have mutated durable state.
            Assert.Single(await reopened.GetPublishedUnitsAsync(repo));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // Complete-empty must survive recovery (never downgraded to unavailable/empty-success).
    [Fact]
    public async Task Recovery_PreservesCompleteEmptyEvidence()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:empty");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            CodeOccurrenceQueryResult recovered;
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (fail && point == "before-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store, repo, "run:e1",
                    CompleteEmptyUnit(repo, "run:e1", "unit:e1"));

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:e2"), Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:e2", "unit:e2", [Node("node:e2")],
                        [Fact(repo, "unit:e2", target, "src/X.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));
                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:e2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:e2"), [])));

                recovered = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var durable = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));

            Assert.Equal(CodeOccurrenceAvailability.Available, durable.Availability);
            Assert.True(durable.IsComplete);
            Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
            Assert.True(recovered.IsComplete);
            Assert.Empty(recovered.Occurrences);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // Partial coverage must not be upgraded to Produced by recovery.
    [Fact]
    public async Task Recovery_PreservesPartialCoverage()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:partial");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            CodeOccurrenceQueryResult recovered;
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (fail && point == "before-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store, repo, "run:q1",
                    Unit(repo, "run:q1", "unit:q1", [Node("node:q1")],
                        [Fact(repo, "unit:q1", target, "src/P.cs", 1, 1)],
                        CodeRelationshipCoverageState.Partial));

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:q2"), Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:q2", "unit:q2", [Node("node:q2")],
                        [Fact(repo, "unit:q2", target, "src/Y.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));
                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:q2"), [])));

                recovered = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var durable = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));

            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, durable.Availability);
            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, recovered.Availability);
            Assert.False(recovered.IsComplete);
            Assert.Single(recovered.Occurrences);
            Assert.Equal(durable.Coverage!.State, recovered.Coverage!.State);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // A conflicting contribution must still be refused after recovery, and the
    // rejection must not mutate durable state.
    [Fact]
    public async Task Recovery_ConflictRefusalStillEffective()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:conflict");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            using var store = new LatticeCodeGraphStore(path, null, point =>
            {
                if (fail && point == "before-commit")
                    throw new InjectedPersistenceException();
            });

            await PublishAsync(
                store, repo, "run:c1",
                Unit(repo, "run:c1", "unit:c1", [Node("node:c1")],
                    [Fact(repo, "unit:c1", target, "src/C.cs", 1, 1)],
                    CodeRelationshipCoverageState.Produced));

            await store.StoreIndexRunAsync(new(
                repo, new CodeIndexRunId("run:c2"), Started.AddSeconds(5), plugins: [Plugin]));
            await store.StageIndexUnitAsync(
                Unit(repo, "run:c2", "unit:c2", [Node("node:c2")],
                    [Fact(repo, "unit:c2", target, "src/D.cs", 1, 1)],
                    CodeRelationshipCoverageState.Produced));
            fail = true;
            await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:c2"), Started.AddSeconds(5),
                        CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                    new(repo, new CodeIndexRunId("run:c2"), [])));
            fail = false;

            // After recovery the committed fact is still validated against later
            // contributions, so a conflicting payload is refused.
            var committed = Fact(repo, "unit:c1", target, "src/C.cs", 1, 1);
            var conflicting = CodeOccurrenceFact.Create(
                repo, target, target.Value, "unit:c1",
                new CodeOccurrenceContributor(Plugin, "unit:c1"),
                CodeOccurrenceCategory.Invocation, committed.Span,
                "sha256:source", "content:conflicting");

            await store.StoreIndexRunAsync(new(
                repo, new CodeIndexRunId("run:c3"), Started.AddSeconds(9), plugins: [Plugin]));
            await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:c3", "unit:c3", [Node("node:c3")],
                        [conflicting], CodeRelationshipCoverageState.Produced)));

            var after = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
            Assert.Single(after.Occurrences);
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // Graph noninterference: recovery must not change the published graph units.
    [Fact]
    public async Task Recovery_PreservesPublishedGraphUnits()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:graph");
        var target = Target(repo, "T1");
        var fail = false;
        try
        {
            IReadOnlyList<CodeIndexUnitReplacement> beforeUnits;
            IReadOnlyList<CodeIndexUnitReplacement> afterUnits;
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (fail && point == "before-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store, repo, "run:g1",
                    Unit(repo, "run:g1", "unit:g1", [Node("node:g1")],
                        [Fact(repo, "unit:g1", target, "src/G.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));
                beforeUnits = await store.GetPublishedUnitsAsync(repo);

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:g2"), Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:g2", "unit:g2", [Node("node:g2")],
                        [Fact(repo, "unit:g2", target, "src/H.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));
                fail = true;
                await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:g2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:g2"), [])));

                afterUnits = await store.GetPublishedUnitsAsync(repo);
            }

            Assert.Equal(
                beforeUnits.SelectMany(unit => unit.Nodes).Select(node => node.Id.Value).OrderBy(id => id),
                afterUnits.SelectMany(unit => unit.Nodes).Select(node => node.Id.Value).OrderBy(id => id));
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // Unknown-outcome non-regression: an ack-loss must still quarantine and must
    // not replay; reopen reconciles durable state.
    [Fact]
    public async Task UnknownOutcome_StillQuarantinesAndReopenSeesDurableState()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:new01:unknown");
        var target = Target(repo, "T1");
        var failAfterCommit = false;
        try
        {
            using (var store = new LatticeCodeGraphStore(path, null, point =>
                   {
                       if (failAfterCommit && point == "after-commit")
                           throw new InjectedPersistenceException();
                   }))
            {
                await PublishAsync(
                    store, repo, "run:u1",
                    Unit(repo, "run:u1", "unit:u1", [Node("node:u1")],
                        [Fact(repo, "unit:u1", target, "src/U.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));

                await store.StoreIndexRunAsync(new(
                    repo, new CodeIndexRunId("run:u2"), Started.AddSeconds(5), plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:u2", "unit:u2", [Node("node:u2")],
                        [Fact(repo, "unit:u2", target, "src/U2.cs", 1, 1)],
                        CodeRelationshipCoverageState.Produced));
                failAfterCommit = true;
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:u2"), Started.AddSeconds(5),
                            CodeIndexRunStatus.Completed, Started.AddSeconds(6), [Plugin]),
                        new(repo, new CodeIndexRunId("run:u2"), [])));

                // Quarantine: no current-state claim (and no replay).
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                    await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)));
            }

            using var reopened = new LatticeCodeGraphStore(path);
            var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
            Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
            // The committed completion is durable (U2.cs), proving reopen reconciles.
            Assert.Contains(after.Occurrences, hit => hit.Span.Path == "src/U2.cs");
        }
        finally
        {
            DeleteDatabase(path);
        }
    }

    // ---- helpers -----------------------------------------------------------

    private static CodeFactOrigin Origin(CodeRepositoryId repo, string run, string unit) =>
        new(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit));

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
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target,
        string path, int line, int column) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source", "content:invocation");

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeOccurrenceFact> facts,
        CodeRelationshipCoverageState state) =>
        new(
            Origin(repo, run, unit),
            nodes,
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(
                facts.Count, 0, state,
                scopeId: "analysisscope1:sha256:" + new string('a', 64),
                universeId: "compilation-universe1:sha256:" + new string('b', 64),
                declaredUnitCount: 1, verifiedUnitCount: 1));

    private static CodeIndexUnitReplacement CompleteEmptyUnit(
        CodeRepositoryId repo, string run, string unit) =>
        Unit(repo, run, unit, [Node("node:" + unit)], [], CodeRelationshipCoverageState.Produced);

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    private static async Task PublishAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
        params CodeIndexUnitReplacement[] units)
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
        Path.Combine(Path.GetTempPath(), $"andirit-new01-{Guid.NewGuid():N}.ltdb");

    private static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            if (File.Exists(candidate))
                File.Delete(candidate);
        }
    }

    private sealed class InjectedPersistenceException : Exception
    {
        public InjectedPersistenceException() : base("injected persistence failure") { }
    }
}
