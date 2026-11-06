// ANDIRIT-CI1A-S2.3-NEW-01-Q1 — independent reviewer probes.
//
// Subject: NEW-01 recovery remediation (commit c75c41f) — both known-failure
// recovery branches in LatticeCodeGraphStore.MutateAsync must rebuild the FULL
// projection (graph + baselines + occurrences) via RebuildInnerFromDurableStateAsync().
//
// These probes are reviewer-owned, independently implemented assertions. They
// never modify production code. Where they decode durable state they parse the
// base64 payload columns with their own JsonDocument readers (independent of the
// production deserializer) and recompute identity/coverage expectations from the
// reviewer's own fact objects. Identity types serialize as {"Value": ...}; enums
// as numbers; PascalCase property names (CodeJsonDefaults = plain STJ).
//
// Recovery branches are driven mechanically through the internal fault-injecting
// constructor: "before-commit" throws inside Persist's commit scope (known
// failure -> rollback, original exception rethrown after a recovery rebuild) and
// "after-commit" throws after txn.Commit() (unknown outcome -> quarantine). The
// reviewer's injected exception therefore surfaces unchanged, so the exact
// identity of the rethrown original failure is provable.
//
// Authority for expected behavior:
//   docs/planning/adr-ci1-01-ci1a-s2-3/new-01-recovery-remediation.md
//   docs/planning/adr-ci1-01-ci1a-s2-rem-01/coverage-and-availability.md (§3/§5)
//   docs/planning/adr-ci1-01-ci1a-s2-rem-01/publication-uncertainty.md

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Reviewer;

/// <summary>
/// Reviewer qualification probes for the NEW-01 recovery remediation. One
/// database per probe; every probe disposes its store and deletes the database.
/// </summary>
public sealed class New01Q1ReviewerProbes
{
    // P01 — Branch-B flagship. The recovered in-place projection must equal the
    // durable occurrence oracle field-by-field; staged contributions stay
    // invisible; the failed completion writes nothing durable.
    [Fact]
    public async Task P01_CompletePreCommitFailure_RebuildMatchesDurableOccurrenceOracle()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repoBase = new CodeRepositoryId("repo:q1:base");
            var repoStale = new CodeRepositoryId("repo:q1:stale");
            var t1 = Q1.Target(repoBase, "T1");
            var t2 = Q1.Target(repoBase, "T2");
            var t3 = Q1.Target(repoBase, "T3");
            var tStale = Q1.Target(repoStale, "S1");
            var fact1 = Q1.Fact(repoBase, "unit:p1u1", t1, "src/A.cs", 5, 1);
            var fact2 = Q1.Fact(repoBase, "unit:p1u1", t1, "src/A.cs", 9, 1);
            var fact3 = Q1.Fact(repoBase, "unit:p1u2", t2, "src/B.cs", 2, 3);
            var stagedFact = Q1.Fact(repoBase, "unit:p2u3", t3, "src/C.cs", 1, 1);
            var staleFact0 = Q1.Fact(repoStale, "unit:s0", tStale, "src/S.cs", 1, 1);
            var staleFact1 = Q1.Fact(repoStale, "unit:s1", tStale, "src/S.cs", 4, 1);
            var expectedP1 = new CodeRepositoryIndexState(repoBase, new CodeIndexRunId("run:p1"), []);

            CodeGraphPublication staleExpected;
            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repoBase, "run:p1",
                    Q1.Unit(repoBase, "run:p1", "unit:p1u1", [Q1.Node("node:p1u1")], [fact1, fact2], CodeRelationshipCoverageState.Produced),
                    Q1.Unit(repoBase, "run:p1", "unit:p1u2", [Q1.Node("node:p1u2")], [fact3], CodeRelationshipCoverageState.Produced));
                await Q1.PublishAsync(setup, repoStale, "run:s0",
                    Q1.Unit(repoStale, "run:s0", "unit:s0", [Q1.Node("node:s0")], [staleFact0], CodeRelationshipCoverageState.Produced));
                staleExpected = (await setup.GetLatestPublicationAsync(repoStale))!;
                await Q1.PublishAsync(setup, repoStale, "run:s1",
                    Q1.Unit(repoStale, "run:s1", "unit:s1", [Q1.Node("node:s1")], [staleFact1], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repoBase, new CodeIndexRunId("run:p2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            // Segment rows are excluded: staging is itself a successful durable
            // mutation, while a failed publication must leave these untouched.
            var beforeLabels = Q1.Subset(path, Q1.ProtectedLabels());

            string sigT1;
            string sigT2;
            string sigT3;
            string sigStale;
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                // The staged contribution is (re)staged in the same lifetime as the
                // publication attempt, as production does: occurrence facts are not
                // part of the durable segment record (D3) and are only persisted by
                // the completing transaction.
                await store.StageIndexUnitAsync(
                    Q1.Unit(repoBase, "run:p2", "unit:p2u3", [Q1.Node("node:p2u3")], [stagedFact], CodeRelationshipCoverageState.Produced));

                sigT1 = Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t1)));
                sigT2 = Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t2)));
                sigT3 = Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t3)));
                sigStale = Q1.Render(await store.FindOccurrencesAsync(new(repoStale, tStale, expectedPublication: staleExpected)));

                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repoBase, new CodeIndexRunId("run:p2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(5), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repoBase, new CodeIndexRunId("run:p2"), [])));
                arm = false;

                Assert.Equal(sigT1, Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t1))));
                Assert.Equal(sigT2, Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t2))));
                Assert.Equal(sigT3, Q1.Render(await store.FindOccurrencesAsync(new(repoBase, t3))));
                Assert.Equal(sigStale, Q1.Render(await store.FindOccurrencesAsync(new(repoStale, tStale, expectedPublication: staleExpected))));
                Assert.Equal(CodeOccurrenceAvailability.Available, (await store.FindOccurrencesAsync(new(repoBase, t1))).Availability);
                Assert.Empty((await store.FindOccurrencesAsync(new(repoBase, t3))).Occurrences);
            }

            Assert.Equal(beforeLabels, Q1.Subset(path, Q1.ProtectedLabels()));

            var unitRows = Q1.Rows(path, Q1.OccurrenceUnitLabel).ToDictionary(pair => pair.Key, pair => pair.Payload);
            Assert.Equal(4, unitRows.Count);
            using (var u1 = Q1.Decode(unitRows[Q1.UnitRowKey(repoBase, Q1.Plugin.Value, "unit:p1u1")]))
            {
                Assert.Equal(repoBase.Value, u1.RootElement.Str("RepositoryId"));
                Assert.Equal("unit:p1u1", u1.RootElement.Str("IndexUnitId"));
                Assert.True(u1.RootElement.Bool("AnalysisIncluded"));
                var facts = u1.RootElement.Arr("Facts").EnumerateArray().ToList();
                Assert.Equal(2, facts.Count);
                DurableAssert.FactMatches(facts[0], fact1);
                DurableAssert.FactMatches(facts[1], fact2);
                DurableAssert.CoverageMatches(u1.RootElement.Obj("Coverage"),
                    new CodeOccurrenceCoverage(2, 0, CodeRelationshipCoverageState.Produced, Q1.Scope, Q1.Universe, 1, 1));
            }

            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single(pair => pair.Key == repoBase.Value).Payload))
            {
                Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, meta.RootElement.Int("SchemaVersion"));
                Assert.Equal(LatticeCodeGraphStore.OccurrenceNamespace, meta.RootElement.Str("Namespace"));
                Assert.Equal("run:p1", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(expectedP1.IndexIdentity.Value, meta.RootElement.Str("IndexIdentity"));
                Assert.Null(meta.RootElement.OptStr("SnapshotIdentity"));
                Assert.Equal(3, meta.RootElement.Int("FactCount"));
                Assert.Equal(2, meta.RootElement.Int("UnitCount"));
                Assert.Equal(2, meta.RootElement.Int("PageCount"));
                Assert.True(meta.RootElement.Bool("CompleteCoverage"));
                Assert.Equal(Q1.Scope, meta.RootElement.Str("ScopeId"));
            }

            using (var page1 = Q1.Decode(Q1.Rows(path, Q1.OccurrencePageLabel).Single(pair => pair.Key == Q1.PageRowKey(repoBase, t1)).Payload))
            {
                Assert.Equal([fact1.Id.Value, fact2.Id.Value],
                    page1.RootElement.Arr("OccurrenceIds").EnumerateArray().Select(v => v.GetString()!).ToArray());
            }

            var baselineRows = Q1.Rows(path, Q1.BaselineLabel);
            Assert.Single(baselineRows);
            Assert.Equal(Q1.BaselineRowKey(repoBase, "run:p2"), baselineRows[0].Key);
            Assert.Equal("run:p1", baselineRows[0].Payload);

            using (var stage = Q1.Decode(Q1.Rows(path, Q1.StageLabel).Single().Payload))
            {
                Assert.Equal("stage-replace", stage.RootElement.Str("Kind"));
                Assert.Equal("unit:p2u3", stage.RootElement.Obj("Replacement").Obj("Origin").IdValue("IndexUnitId"));
                Assert.False(stage.RootElement.GetProperty("Replacement").TryGetProperty("Occurrences", out _));
            }

            using (var graphUnit = Q1.Decode(Q1.Rows(path, Q1.UnitLabel)
                .Single(pair => pair.Key == Q1.UnitRowKey(repoBase, Q1.Plugin.Value, "unit:p1u1")).Payload))
            {
                Assert.False(graphUnit.RootElement.TryGetProperty("Occurrences", out _));
                Assert.False(graphUnit.RootElement.TryGetProperty("OccurrenceAnalysisIncluded", out _));
            }

            using (var runP2 = Q1.Decode(Q1.Rows(path, Q1.RunLabel)
                .Single(pair => pair.Key == Q1.RunRowKey(repoBase, "run:p2")).Payload))
            {
                Assert.Equal((int)CodeIndexRunStatus.Running, runP2.RootElement.Int("Status"));
            }

            using (var state = Q1.Decode(Q1.Rows(path, Q1.IndexStateLabel).Single(pair => pair.Key == repoBase.Value).Payload))
            {
                Assert.Equal("run:p1", state.RootElement.IdValue("IndexRunId"));
                Assert.Equal(expectedP1.IndexIdentity.Value, state.RootElement.Obj("IndexIdentity").Str("Value"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(sigT1, Q1.Render(await reopened.FindOccurrencesAsync(new(repoBase, t1))));
                Assert.Equal(sigT2, Q1.Render(await reopened.FindOccurrencesAsync(new(repoBase, t2))));
                Assert.Equal(sigT3, Q1.Render(await reopened.FindOccurrencesAsync(new(repoBase, t3))));
                var durableStale = await reopened.FindOccurrencesAsync(new(repoStale, tStale, expectedPublication: staleExpected));
                Assert.Equal(sigStale, Q1.Render(durableStale));
                Assert.Equal(CodeOccurrenceAvailability.StalePublication, durableStale.Availability);
                Assert.Equal("run:s1", durableStale.Publication!.IndexRunId.Value);
                Assert.Equal(2, (await reopened.FindOccurrencesAsync(new(repoBase, t1))).Occurrences.Count);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P02 — Branch-A conflict. The rejected staged conflict must not change
    // durable bytes; after the recovery, valid contributions still publish and
    // the conflict refusal remains effective.
    [Fact]
    public async Task P02_ConflictRecoveryKeepsDurableBytesAndValidation()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:conflict");
            var t1 = Q1.Target(repo, "T1");
            var committed = Q1.Fact(repo, "unit:c1", t1, "src/K.cs", 4, 1);
            var valid = Q1.Fact(repo, "unit:c2", t1, "src/L.cs", 9, 1);
            var conflicting = CodeOccurrenceFact.Create(repo, t1, t1.Value, "unit:c1",
                new CodeOccurrenceContributor(Q1.Plugin, "unit:c1"), CodeOccurrenceCategory.Invocation,
                committed.Span, committed.SourceHash, "content:conflicting");
            Assert.Equal(committed.Id, conflicting.Id);
            Assert.NotEqual(committed.PayloadHash, conflicting.PayloadHash);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:c1",
                    Q1.Unit(repo, "run:c1", "unit:c1", [Q1.Node("node:c1")], [committed], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            using (var store = new LatticeCodeGraphStore(path))
            {
                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q2", "unit:x", [Q1.Node("node:x")], [conflicting], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection.Errors, error => error.Code == "occurrence.payload.conflict");
                Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));

            using (var store = new LatticeCodeGraphStore(path))
            {
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:c2", [Q1.Node("node:c2")], [valid], CodeRelationshipCoverageState.Produced));
                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                Assert.Equal(["src/K.cs", "src/L.cs"],
                    (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());

                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q3"), Q1.Started, plugins: [Q1.Plugin]));
                var rejection2 = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q3", "unit:x2", [Q1.Node("node:x2")], [conflicting], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection2.Errors, error => error.Code == "occurrence.payload.conflict");
                Assert.Equal(2, (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Count);
            }

            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q2", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(2, meta.RootElement.Int("FactCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(2, (await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences.Count);
                Assert.Equal("run:q2",
                    (await reopened.FindOccurrencesAsync(new(repo, t1))).Publication!.IndexRunId.Value);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P03 — Branch-A graph-invariant trigger. A staged edge to a missing
    // endpoint is rejected; the recovery leaves traversals and durable rows
    // unchanged, and a repaired replacement still publishes.
    [Fact]
    public async Task P03_EdgeEndpointMismatchRecoveryPreservesGraph()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:graph");
            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:r1",
                    Q1.GraphUnit(repo, "run:r1", "unit:r1",
                        [Q1.Node("node:r1a"), Q1.Node("node:r1b")],
                        [Q1.Edge("edge:r1ab", "node:r1a", "node:r1b", CodeEdgeKinds.Calls)],
                        [], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:r2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            using (var store = new LatticeCodeGraphStore(path))
            {
                var before = Q1.TravSig(await store.TraverseAsync(repo, new(new CodeNodeId("node:r1a"), maxDepth: 3)));
                Assert.Equal("n[node:r1a,node:r1b]e[edge:r1ab]d1o0", before);
                var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.GraphUnit(repo, "run:r2", "unit:r2", [],
                            [Q1.Edge("edge:r2ghost", "node:r1b", "node:r2ghost", CodeEdgeKinds.References)],
                            [], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection.Errors, error => error.Code == "graph.edge.endpoint-missing");
                Assert.Equal(before, Q1.TravSig(await store.TraverseAsync(repo, new(new CodeNodeId("node:r1a"), maxDepth: 3))));
                Assert.Single(await store.GetPublishedUnitsAsync(repo));
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));

            using (var retry = new LatticeCodeGraphStore(path))
            {
                await retry.StageIndexUnitAsync(
                    Q1.GraphUnit(repo, "run:r2", "unit:r2v",
                        [Q1.Node("node:r2v")],
                        [Q1.Edge("edge:r2vbd", "node:r1b", "node:r2v", CodeEdgeKinds.References)],
                        [], CodeRelationshipCoverageState.Produced));
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:r2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:r2"), []));
                Assert.Equal(
                    "n[node:r1a,node:r1b,node:r2v]e[edge:r1ab,edge:r2vbd]d2o0",
                    Q1.TravSig(await retry.TraverseAsync(repo, new(new CodeNodeId("node:r1a"), maxDepth: 3))));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(2, (await reopened.GetPublishedUnitsAsync(repo)).Count);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P04 — Branch-B on a run command. The rolled-back run is absent from the
    // recovered projection; its ordering baseline is restored on retry and a
    // same-Owner replacement retires the obsolete facts.
    [Fact]
    public async Task P04_PreCommitFailureOnRunCommand_BaselineRestoredAndRetirementApplied()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:retriedrun");
            var t1 = Q1.Target(repo, "T1");
            var factA = Q1.Fact(repo, "unit:p1", t1, "src/PriorA.cs", 4, 1);
            var factB = Q1.Fact(repo, "unit:p1", t1, "src/PriorB.cs", 9, 1);
            var factC = Q1.Fact(repo, "unit:p1", t1, "src/NewC.cs", 2, 2);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:p1", [Q1.Node("node:p1")], [factA, factB], CodeRelationshipCoverageState.Produced));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.StoreIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin])));
                arm = false;
                Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
                Assert.Equal(2, (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Count);
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));
            Assert.Empty(Q1.Rows(path, Q1.BaselineLabel));

            using (var runOnly = new LatticeCodeGraphStore(path))
            {
                await runOnly.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var baselineRows = Q1.Rows(path, Q1.BaselineLabel);
            Assert.Single(baselineRows);
            Assert.Equal(Q1.BaselineRowKey(repo, "run:q2"), baselineRows[0].Key);
            Assert.Equal("run:p1", baselineRows[0].Payload);

            using (var retry = new LatticeCodeGraphStore(path))
            {
                await retry.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:p1", [Q1.Node("node:p1")], [factC], CodeRelationshipCoverageState.Produced));
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                var after = await retry.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
                Assert.Equal(["src/NewC.cs"], after.Occurrences.Select(hit => hit.Span.Path).ToArray());
                Assert.Equal(
                    Q1.CoverSig(new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, Q1.Scope, Q1.Universe, 1, 1)),
                    Q1.CoverSig(after.Coverage!));
            }

            Assert.Empty(Q1.Rows(path, Q1.BaselineLabel));
            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q2", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(1, meta.RootElement.Int("FactCount"));
                Assert.Equal(1, meta.RootElement.Int("UnitCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(["src/NewC.cs"],
                    (await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P05 — Branch-B on stage-replace. The failing stage leaves the previously
    // staged unit's durable row unchanged and its contribution invisible until
    // publication; a later valid stage publishes normally.
    [Fact]
    public async Task P05_PreCommitFailureOnStageCommand_StageRowsUnchangedAndRetryPublishes()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:stagedfailed");
            var t1 = Q1.Target(repo, "T1");
            var factF = Q1.Fact(repo, "unit:p1u", t1, "src/F.cs", 4, 1);
            var factG = Q1.Fact(repo, "unit:q2g", t1, "src/G.cs", 9, 9);
            var factH = Q1.Fact(repo, "unit:q3t", t1, "src/H.cs", 3, 3);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:p1u", [Q1.Node("node:p1u")], [factF], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            using (var stageSetup = new LatticeCodeGraphStore(path))
            {
                await stageSetup.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:q2g", [Q1.Node("node:q2g")], [factG], CodeRelationshipCoverageState.Produced));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                Assert.Equal("src/F.cs",
                    (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Single().Span.Path);
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q2", "unit:q3t", [Q1.Node("node:q3t")], [factH], CodeRelationshipCoverageState.Produced)));
                arm = false;
                var recovered = await store.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal("src/F.cs", recovered.Occurrences.Single().Span.Path);
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));
            var stageRows = Q1.Rows(path, Q1.StageLabel);
            Assert.Single(stageRows);
            using (var stageRow = Q1.Decode(stageRows[0].Payload))
            {
                Assert.Equal("stage-replace", stageRow.RootElement.Str("Kind"));
                Assert.Equal("unit:q2g", stageRow.RootElement.Obj("Replacement").Obj("Origin").IdValue("IndexUnitId"));
            }

            using (var retry = new LatticeCodeGraphStore(path))
            {
                // Both contributions are re-staged in the publishing lifetime: the
                // segment record deliberately excludes occurrence facts (D3).
                await retry.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:q2g", [Q1.Node("node:q2g")], [factG], CodeRelationshipCoverageState.Produced));
                await retry.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:q3t", [Q1.Node("node:q3t")], [factH], CodeRelationshipCoverageState.Produced));
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                var after = await retry.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(["src/F.cs", "src/G.cs", "src/H.cs"],
                    after.Occurrences.Select(hit => hit.Span.Path).ToArray());
            }

            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q2", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(3, meta.RootElement.Int("FactCount"));
                Assert.Equal(3, meta.RootElement.Int("UnitCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(3, (await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences.Count);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P06 — Branch-B on complete with a staged deletion. The retry applies the
    // accepted availability mapping: the retired target answers Available with
    // zero hits (complete coverage, absent page), not a fabricated analysis.
    // NOTE: the reviewer's draft expected AnalysisUnavailable; the accepted
    // REM-01 §3 mapping answers Available+zero under complete coverage.
    [Fact]
    public async Task P06_PreCommitFailureOnComplete_StagedDeletionThenAvailabilityMapping()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:deletion");
            var t1 = Q1.Target(repo, "T1");
            var t2 = Q1.Target(repo, "T2");
            var factA = Q1.Fact(repo, "unit:p1u1", t1, "src/A1.cs", 1, 1);
            var factB = Q1.Fact(repo, "unit:p1u2", t2, "src/B1.cs", 2, 2);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:p1u1", [Q1.Node("node:p1u1")], [factA], CodeRelationshipCoverageState.Produced),
                    Q1.Unit(repo, "run:p1", "unit:p1u2", [Q1.Node("node:p1u2")], [factB], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
                await setup.StageIndexUnitDeletionAsync(
                    repo, new CodeIndexRunId("run:q2"), Q1.Plugin, new CodeIndexUnitId("unit:p1u2"));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                var sigT1 = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                var sigT2 = Q1.Render(await store.FindOccurrencesAsync(new(repo, t2)));
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;
                Assert.Equal(sigT1, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
                Assert.Equal(sigT2, Q1.Render(await store.FindOccurrencesAsync(new(repo, t2))));
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));

            using (var retry = new LatticeCodeGraphStore(path))
            {
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                var afterT1 = await retry.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(CodeOccurrenceAvailability.Available, afterT1.Availability);
                Assert.Single(afterT1.Occurrences);
                var afterT2 = await retry.FindOccurrencesAsync(new(repo, t2));
                Assert.Equal(CodeOccurrenceAvailability.Available, afterT2.Availability);
                Assert.Empty(afterT2.Occurrences);
                Assert.True(afterT2.IsComplete);
            }

            Assert.Single(Q1.Rows(path, Q1.OccurrenceUnitLabel));
            Assert.DoesNotContain(Q1.Rows(path, Q1.OccurrencePageLabel), pair => pair.Key == Q1.PageRowKey(repo, t2));
            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q2", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(1, meta.RootElement.Int("FactCount"));
                Assert.Equal(1, meta.RootElement.Int("UnitCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Empty((await reopened.FindOccurrencesAsync(new(repo, t2))).Occurrences);
                Assert.Single((await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P07 — Contributor union. Identical facts contributed by two staged units
    // union into one indexed entry after the retried publication; the Branch-B
    // failure must not apply the staged contributions early.
    [Fact]
    public async Task P07_PostRecoveryContributorUnionSortedAndDurableCounts()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:union");
            var t1 = Q1.Target(repo, "T1");
            var baseFact = Q1.Fact(repo, "unit:u0", t1, "src/Base.cs", 5, 1);
            var union1 = Q1.FactShared(repo, "shared:src", t1,
                new CodeOccurrenceContributor(Q1.Plugin, "unit:cu1"), "src/Union.cs", 7, 7);
            var union2 = Q1.FactShared(repo, "shared:src", t1,
                new CodeOccurrenceContributor(Q1.Plugin, "unit:cu2"), "src/Union.cs", 7, 7);
            Assert.Equal(union1.Id, union2.Id);
            Assert.Equal(union1.PayloadHash, union2.PayloadHash);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:u0", [Q1.Node("node:u0")], [baseFact], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                // Both contributions are staged in the publishing lifetime.
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:cu1", [Q1.Node("node:cu1")], [union1], CodeRelationshipCoverageState.Produced));
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:cu2", [Q1.Node("node:cu2")], [union2], CodeRelationshipCoverageState.Produced));

                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                Assert.Single((await store.FindOccurrencesAsync(new(repo, t1))).Occurrences);
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;
                Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));

                // The retry publishes both contributions; the union is sorted.
                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                var after = await store.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(2, after.Occurrences.Count);
                var unionHit = after.Occurrences.Single(hit => hit.Span.Path == "src/Union.cs");
                Assert.Equal(["unit:cu1", "unit:cu2"],
                    unionHit.Contributors.Select(contributor => contributor.IndexUnitId).ToArray());
                Assert.Single(after.Occurrences.Single(hit => hit.Span.Path == "src/Base.cs").Contributors);
            }

            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal(3, meta.RootElement.Int("FactCount"));
                Assert.Equal(3, meta.RootElement.Int("UnitCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            var pageRow = Q1.Rows(path, Q1.OccurrencePageLabel).Single();
            using (var page = Q1.Decode(pageRow.Payload))
            {
                var ids = page.RootElement.Arr("OccurrenceIds").EnumerateArray().Select(v => v.GetString()!).ToArray();
                Assert.Equal(2, ids.Distinct().Count());
                Assert.Contains(union1.Id.Value, ids);
            }

            Assert.Equal(3, Q1.Rows(path, Q1.OccurrenceUnitLabel).Count);
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var durable = await reopened.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(2, durable.Occurrences.Count);
                Assert.Equal(["unit:cu1", "unit:cu2"],
                    durable.Occurrences.Single(hit => hit.Span.Path == "src/Union.cs")
                        .Contributors.Select(contributor => contributor.IndexUnitId).ToArray());
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P08 — Two consecutive Branch-B failures on the same retried completion:
    // reader and durable bytes stay stable, and one retry publishes once.
    [Fact]
    public async Task P08_RepeatedPreCommitFailures_PublicationStabilizes()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:repeats");
            var t1 = Q1.Target(repo, "T1");
            var factA = Q1.Fact(repo, "unit:p1u", t1, "src/P1.cs", 4, 1);
            var factG = Q1.Fact(repo, "unit:q2v", t1, "src/G1.cs", 9, 9);

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:p1u", [Q1.Node("node:p1u")], [factA], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:q2v", [Q1.Node("node:q2v")], [factG], CodeRelationshipCoverageState.Produced));
                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    arm = true;
                    await Assert.ThrowsAsync<ReviewerPersistenceException>(
                        async () => await store.CompleteIndexRunAsync(
                            new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                            new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                    arm = false;
                    Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
                }

                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(6), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                var after = await store.FindOccurrencesAsync(new(repo, t1));
                Assert.Equal(["src/G1.cs", "src/P1.cs"], after.Occurrences.Select(hit => hit.Span.Path).ToArray());
                Assert.Equal("run:q2", after.Publication!.IndexRunId.Value);
            }

            Assert.Empty(Q1.Rows(path, Q1.BaselineLabel));
            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal(2, meta.RootElement.Int("FactCount"));
                Assert.Equal(2, meta.RootElement.Int("UnitCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(2, (await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences.Count);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P09 — Availability matrix across four repositories in one recovered store:
    // complete-empty Available, Partial IncompleteAnalysis, graph-only
    // AnalysisUnavailable, and consolidated refusal counters preserved.
    [Fact]
    public async Task P09_RecoveryAvailabilityMatrix_FourRepositoriesWithinOneStore()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repoA = new CodeRepositoryId("repo:q1:ma");
            var repoB = new CodeRepositoryId("repo:q1:mb");
            var repoC = new CodeRepositoryId("repo:q1:mc");
            var repoD = new CodeRepositoryId("repo:q1:md");
            var targetA = Q1.Target(repoA, "TA");
            var targetB = Q1.Target(repoB, "TB");
            var targetC = Q1.Target(repoC, "TC");
            var targetD = Q1.Target(repoD, "TD");
            var factB = Q1.Fact(repoB, "unit:pb1", targetB, "src/B1.cs", 3, 3);
            var factD1 = Q1.Fact(repoD, "unit:pd1", targetD, "src/D1.cs", 2, 2);
            var factD2 = Q1.Fact(repoD, "unit:pd1", targetD, "src/D2.cs", 6, 6);
            var counterCoverage = new CodeOccurrenceCoverage(
                2, 1, CodeRelationshipCoverageState.Partial, Q1.Scope, Q1.Universe, 2, 2,
                2, 1, 2, 1, 2,
                new Dictionary<string, int>(StringComparer.Ordinal) { ["review:reason:x"] = 2 });

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repoA, "run:e1",
                    Q1.Unit(repoA, "run:e1", "unit:ae1", [Q1.Node("node:ae1")], [], CodeRelationshipCoverageState.Produced));
                await Q1.PublishAsync(setup, repoB, "run:pb1",
                    Q1.Unit(repoB, "run:pb1", "unit:pb1", [Q1.Node("node:pb1")], [factB], CodeRelationshipCoverageState.Partial));
                await Q1.PublishAsync(setup, repoC, "run:go1", Q1.GraphOnlyUnit(repoC, "run:go1", "unit:go1"));
                await Q1.PublishAsync(setup, repoD, "run:pd1",
                    Q1.Unit(repoD, "run:pd1", "unit:pd1", [Q1.Node("node:pd1")], [factD1, factD2],
                        CodeRelationshipCoverageState.Partial, counterCoverage));
                foreach (var repo in new[] { repoA, repoB, repoC, repoD })
                {
                    var run = "run:q2" + repo.Value[^2..];
                    await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId(run), Q1.Started, plugins: [Q1.Plugin]));
                }
            }

            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                foreach (var repo in new[] { repoA, repoB, repoC, repoD })
                {
                    await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q2" + repo.Value[^2..], "unit:m2" + repo.Value[^2..],
                            [Q1.Node("node:m2" + repo.Value[^2..])], [], CodeRelationshipCoverageState.Produced));
                }

                var sigA = Q1.Render(await store.FindOccurrencesAsync(new(repoA, targetA)));
                var sigB = Q1.Render(await store.FindOccurrencesAsync(new(repoB, targetB)));
                var sigC = Q1.Render(await store.FindOccurrencesAsync(new(repoC, targetC)));
                var sigD = Q1.Render(await store.FindOccurrencesAsync(new(repoD, targetD)));

                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repoD, new CodeIndexRunId("run:q2md"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repoD, new CodeIndexRunId("run:q2md"), [])));
                arm = false;

                var afterA = await store.FindOccurrencesAsync(new(repoA, targetA));
                Assert.Equal(CodeOccurrenceAvailability.Available, afterA.Availability);
                Assert.Empty(afterA.Occurrences);
                Assert.True(afterA.IsComplete);
                Assert.Equal(
                    Q1.CoverSig(new CodeOccurrenceCoverage(0, 0, CodeRelationshipCoverageState.Produced, Q1.Scope, Q1.Universe, 1, 1)),
                    Q1.CoverSig(afterA.Coverage!));

                var afterB = await store.FindOccurrencesAsync(new(repoB, targetB));
                Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, afterB.Availability);
                Assert.Single(afterB.Occurrences);
                Assert.Equal(CodeRelationshipCoverageState.Partial, afterB.Coverage!.State);

                var afterC = await store.FindOccurrencesAsync(new(repoC, targetC));
                Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, afterC.Availability);
                Assert.Null(afterC.Coverage);
                Assert.Equal("run:go1", afterC.Publication!.IndexRunId.Value);

                var afterD = await store.FindOccurrencesAsync(new(repoD, targetD));
                Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, afterD.Availability);
                Assert.Equal(2, afterD.Occurrences.Count);
                Assert.Equal(Q1.CoverSig(counterCoverage), Q1.CoverSig(afterD.Coverage!));

                Assert.Equal(sigA, Q1.Render(afterA));
                Assert.Equal(sigB, Q1.Render(afterB));
                Assert.Equal(sigC, Q1.Render(afterC));
                Assert.Equal(sigD, Q1.Render(afterD));

                // Retry the failed publication in the staging lifetime.
                await store.CompleteIndexRunAsync(
                    new(repoD, new CodeIndexRunId("run:q2md"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repoD, new CodeIndexRunId("run:q2md"), []));
            }

            Assert.Single(Q1.Rows(path, Q1.OccurrenceMetaLabel), pair => pair.Key == repoA.Value);
            Assert.DoesNotContain(Q1.Rows(path, Q1.OccurrenceUnitLabel),
                pair => pair.Key.StartsWith(repoC.Value + "\n", StringComparison.Ordinal));
            Assert.DoesNotContain(Q1.Rows(path, Q1.OccurrenceMetaLabel), pair => pair.Key == repoC.Value);
            using (var counters = Q1.Decode(Q1.Rows(path, Q1.OccurrenceUnitLabel)
                .Single(pair => pair.Key == Q1.UnitRowKey(repoD, Q1.Plugin.Value, "unit:pd1")).Payload))
            {
                DurableAssert.CoverageMatches(counters.RootElement.Obj("Coverage"), counterCoverage);
            }
            using (var metaD = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single(pair => pair.Key == repoD.Value).Payload))
            {
                Assert.Equal("run:q2md", metaD.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(2, metaD.RootElement.Int("FactCount"));
                Assert.Equal(2, metaD.RootElement.Int("UnitCount"));
                Assert.Equal(1, metaD.RootElement.Int("PageCount"));
                Assert.Equal((int)CodeRelationshipCoverageState.Partial, metaD.RootElement.Int("CoverageState"));
                Assert.False(metaD.RootElement.Bool("CompleteCoverage"));
            }
            using (var metaA = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single(pair => pair.Key == repoA.Value).Payload))
            {
                Assert.Equal(0, metaA.RootElement.Int("FactCount"));
                Assert.True(metaA.RootElement.Bool("CompleteCoverage"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var durableA = await reopened.FindOccurrencesAsync(new(repoA, targetA));
                Assert.Equal(CodeOccurrenceAvailability.Available, durableA.Availability);
                Assert.True(durableA.IsComplete);
                var durableB = await reopened.FindOccurrencesAsync(new(repoB, targetB));
                Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, durableB.Availability);
                Assert.Single(durableB.Occurrences);
                Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
                    (await reopened.FindOccurrencesAsync(new(repoC, targetC))).Availability);
                var expectedDurableCoverage = counterCoverage.Merge(
                    new CodeOccurrenceCoverage(0, 0, CodeRelationshipCoverageState.Produced, Q1.Scope, Q1.Universe, 1, 1));
                Assert.Equal(Q1.CoverSig(expectedDurableCoverage),
                    Q1.CoverSig((await reopened.FindOccurrencesAsync(new(repoD, targetD))).Coverage!));
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P10 — Chained recoveries: Branch A, Branch B, a second Branch A and a
    // success, each separated by a durable-byte check and ended with reopen parity.
    [Fact]
    public async Task P10_ChainedRecoveryBranches_ConflictFailureSuccessReopenParity()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:chain");
            var t1 = Q1.Target(repo, "T1");
            var fact1 = Q1.Fact(repo, "unit:n1", t1, "src/C1.cs", 4, 1);
            var fact2 = Q1.Fact(repo, "unit:n2", t1, "src/C2.cs", 9, 1);
            var fact3 = Q1.Fact(repo, "unit:n3", t1, "src/C3.cs", 12, 1);
            var conflicting = CodeOccurrenceFact.Create(repo, t1, t1.Value, "unit:n1",
                new CodeOccurrenceContributor(Q1.Plugin, "unit:n1"), CodeOccurrenceCategory.Invocation,
                fact1.Span, fact1.SourceHash, "content:conflicting");

            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:n1", [Q1.Node("node:n1")], [fact1], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var beforeSnapshot1 = Q1.Snapshot(path);
            using (var store = new LatticeCodeGraphStore(path))
            {
                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q2", "unit:x1", [Q1.Node("node:x1")], [conflicting], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection.Errors, error => error.Code == "occurrence.payload.conflict");
                Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
            }

            Assert.Equal(beforeSnapshot1, Q1.Snapshot(path));

            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:n2", [Q1.Node("node:n2")], [fact2], CodeRelationshipCoverageState.Produced));
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;
                Assert.Equal(["src/C1.cs"],
                    (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());

                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                Assert.Equal(["src/C1.cs", "src/C2.cs"],
                    (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());

                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q3"), Q1.Started, plugins: [Q1.Plugin]));
                var rejection2 = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.Unit(repo, "run:q3", "unit:x2", [Q1.Node("node:x2")], [conflicting], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection2.Errors, error => error.Code == "occurrence.payload.conflict");
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q3", "unit:n3", [Q1.Node("node:n3")], [fact3], CodeRelationshipCoverageState.Produced));
                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q3"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(5), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q3"), []));
                Assert.Equal(["src/C1.cs", "src/C2.cs", "src/C3.cs"],
                    (await store.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());
            }

            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q3", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(3, meta.RootElement.Int("FactCount"));
                Assert.Equal(1, meta.RootElement.Int("PageCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal(["src/C1.cs", "src/C2.cs", "src/C3.cs"],
                    (await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences.Select(hit => hit.Span.Path).ToArray());
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P11 — Native mirror under Branch-B. Mirror and projection both stay on the
    // pre-failure publication (no staged F or C->E), the retry updates both, and
    // a non-native reopen agrees with the updated mirror.
    [Fact]
    public async Task P11_NativeMirrorRecovery_TraversalUnchangedThenRetryUpdatesBothViews()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:native");
            var u1 = Q1.GraphUnit(repo, "run:p1", "unit:m1",
                [Q1.Node("node:m:a"), Q1.Node("node:m:b"), Q1.Node("node:m:c")],
                [
                    Q1.Edge("edge:m:ab", "node:m:a", "node:m:b", CodeEdgeKinds.Calls),
                    Q1.Edge("edge:m:bc", "node:m:b", "node:m:c", CodeEdgeKinds.Calls)
                ],
                [], CodeRelationshipCoverageState.Produced);
            var u2 = Q1.GraphUnit(repo, "run:p1", "unit:m2",
                [Q1.Node("node:m:d"), Q1.Node("node:m:e")],
                [
                    Q1.Edge("edge:m:cd", "node:m:c", "node:m:d", CodeEdgeKinds.References),
                    Q1.Edge("edge:m:de", "node:m:d", "node:m:e", CodeEdgeKinds.Calls)
                ],
                [], CodeRelationshipCoverageState.Produced);
            var u3 = Q1.GraphUnit(repo, "run:q2", "unit:m3",
                [Q1.Node("node:m:f")],
                [
                    Q1.Edge("edge:m:fa", "node:m:f", "node:m:a", CodeEdgeKinds.Calls),
                    Q1.Edge("edge:m:ce", "node:m:c", "node:m:e", CodeEdgeKinds.Calls)
                ],
                [], CodeRelationshipCoverageState.Produced);

            using (var setup = Q1.OpenNativeGuarded(path, _ => false))
            {
                await Q1.PublishAsync(setup, repo, "run:p1", u1, u2);
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
                await setup.StageIndexUnitAsync(u3);
            }

            var beforeNative = Q1.NativeSnapshot(path);
            var arm = false;
            string outgoingBefore;
            string incomingBefore;
            string filteredBefore;
            using (var store = Q1.OpenNativeGuarded(path, point => arm && point == "before-commit"))
            {
                outgoingBefore = Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Outgoing, maxDepth: 4)));
                incomingBefore = Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Incoming, maxDepth: 4)));
                filteredBefore = Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Outgoing,
                        evidenceQualities: [EvidenceQuality.Resolved], maxDepth: 4)));
                Assert.Equal(
                    "n[node:m:a,node:m:b,node:m:c,node:m:d,node:m:e]e[edge:m:ab,edge:m:bc,edge:m:cd,edge:m:de]d4o0",
                    outgoingBefore);
                Assert.Equal("n[node:m:a]e[]d0o0", incomingBefore);

                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;

                Assert.Equal(outgoingBefore, Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Outgoing, maxDepth: 4))));
                Assert.Equal(incomingBefore, Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Incoming, maxDepth: 4))));
                Assert.Equal(filteredBefore, Q1.TravSig(await store.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Outgoing,
                        evidenceQualities: [EvidenceQuality.Resolved], maxDepth: 4))));
            }

            Assert.Equal(beforeNative, Q1.NativeSnapshot(path));

            using (var retry = new LatticeCodeGraphStore(path, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                Assert.Equal("n[node:m:a,node:m:f]e[edge:m:fa]d1o0",
                    Q1.TravSig(await retry.TraverseAsync(repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Incoming, maxDepth: 4))));
                var outgoingFromC = Q1.TravSig(await retry.TraverseAsync(
                    repo, new(new CodeNodeId("node:m:c"), CodeGraphDirection.Outgoing, maxDepth: 2)));
                Assert.Contains("edge:m:ce", outgoingFromC);
            }

            var manifests = Q1.MirrorManifests(path);
            Assert.Single(manifests);
            Assert.Contains("node:m:f", manifests[0].NodeIds);
            Assert.Contains("edge:m:ce", manifests[0].EdgeIds);
            Assert.Contains("edge:m:fa", manifests[0].EdgeIds);
            Assert.Contains("node:m:f", string.Join(",", Q1.GraphNodePayloads(path)));

            using (var projection = new LatticeCodeGraphStore(path))
            {
                Assert.Equal("n[node:m:a,node:m:f]e[edge:m:fa]d1o0",
                    Q1.TravSig(await projection.TraverseAsync(
                        repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Incoming, maxDepth: 4))));
            }

            using (var reopened = new LatticeCodeGraphStore(path, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                Assert.Equal("n[node:m:a,node:m:f]e[edge:m:fa]d1o0",
                    Q1.TravSig(await reopened.TraverseAsync(
                        repo, new(new CodeNodeId("node:m:a"), CodeGraphDirection.Incoming, maxDepth: 4))));
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P12 — Native mirror with (a) a Branch-A edge rejection and (b) a staged
    // deletion that fails at complete; both must conserve mirror and projection,
    // and the retried deletion must remove the unit from both.
    [Fact]
    public async Task P12_NativeEdgeRejectionAndStagedDeletionRecovery_MirrorsConserved()
    {
        var path1 = Q1.TemporaryDatabasePath();
        var path2 = Q1.TemporaryDatabasePath();
        try
        {
            // (a) Branch-A edge rejection, native store.
            var repo1 = new CodeRepositoryId("repo:q1:nativedge");
            using (var setup = Q1.OpenNativeGuarded(path1, _ => false))
            {
                await Q1.PublishAsync(setup, repo1, "run:r1",
                    Q1.GraphUnit(repo1, "run:r1", "unit:r1",
                        [Q1.Node("node:r1a"), Q1.Node("node:r1b")],
                        [Q1.Edge("edge:r1ab", "node:r1a", "node:r1b", CodeEdgeKinds.Calls)],
                        [], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo1, new CodeIndexRunId("run:r2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var beforeNative1 = Q1.NativeSnapshot(path1);
            using (var store = Q1.OpenNativeGuarded(path1, _ => false))
            {
                var before = Q1.TravSig(await store.TraverseAsync(repo1, new(new CodeNodeId("node:r1a"), maxDepth: 3)));
                var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(
                    async () => await store.StageIndexUnitAsync(
                        Q1.GraphUnit(repo1, "run:r2", "unit:bad", [],
                            [Q1.Edge("edge:bad", "node:r1b", "node:r1missing", CodeEdgeKinds.Calls)],
                            [], CodeRelationshipCoverageState.Produced)));
                Assert.Contains(rejection.Errors, error => error.Code == "graph.edge.endpoint-missing");
                Assert.Equal(before, Q1.TravSig(await store.TraverseAsync(repo1, new(new CodeNodeId("node:r1a"), maxDepth: 3))));
            }

            Assert.Equal(beforeNative1, Q1.NativeSnapshot(path1));
            using (var retry = new LatticeCodeGraphStore(path1, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                await retry.StageIndexUnitAsync(
                    Q1.GraphUnit(repo1, "run:r2", "unit:r2v",
                        [Q1.Node("node:r2v")],
                        [Q1.Edge("edge:r2vbd", "node:r1b", "node:r2v", CodeEdgeKinds.References)],
                        [], CodeRelationshipCoverageState.Produced));
                await retry.CompleteIndexRunAsync(
                    new(repo1, new CodeIndexRunId("run:r2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo1, new CodeIndexRunId("run:r2"), []));
            }

            using (var reopened = new LatticeCodeGraphStore(path1, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                Assert.Equal("n[node:r1a,node:r1b,node:r2v]e[edge:r1ab,edge:r2vbd]d2o0",
                    Q1.TravSig(await reopened.TraverseAsync(repo1, new(new CodeNodeId("node:r1a"), maxDepth: 3))));
            }

            // (b) Staged deletion failing at complete, native store.
            var repo2 = new CodeRepositoryId("repo:q1:nativedel");
            var t1 = Q1.Target(repo2, "NT1");
            var t2 = Q1.Target(repo2, "NT2");
            var factA = Q1.Fact(repo2, "unit:n1u", t1, "src/NA.cs", 1, 1);
            var factB = Q1.Fact(repo2, "unit:n2u", t2, "src/NB.cs", 2, 2);
            using (var setup = Q1.OpenNativeGuarded(path2, _ => false))
            {
                await Q1.PublishAsync(setup, repo2, "run:p1",
                    Q1.GraphUnit(repo2, "run:p1", "unit:n1u",
                        [Q1.Node("node:n1a"), Q1.Node("node:n1b")],
                        [Q1.Edge("edge:n1ab", "node:n1a", "node:n1b", CodeEdgeKinds.Calls)],
                        [factA], CodeRelationshipCoverageState.Produced),
                    Q1.GraphUnit(repo2, "run:p1", "unit:n2u",
                        [Q1.Node("node:n2c"), Q1.Node("node:n2d")],
                        [Q1.Edge("edge:n2bc", "node:n1b", "node:n2c", CodeEdgeKinds.References)],
                        [factB], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo2, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
                await setup.StageIndexUnitDeletionAsync(
                    repo2, new CodeIndexRunId("run:q2"), Q1.Plugin, new CodeIndexUnitId("unit:n2u"));
            }

            var beforeNative2 = Q1.NativeSnapshot(path2);
            var arm = false;
            using (var store = Q1.OpenNativeGuarded(path2, point => arm && point == "before-commit"))
            {
                var traversal = Q1.TravSig(await store.TraverseAsync(repo2, new(new CodeNodeId("node:n1a"), maxDepth: 3)));
                var sigT2 = Q1.Render(await store.FindOccurrencesAsync(new(repo2, t2)));
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo2, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo2, new CodeIndexRunId("run:q2"), [])));
                arm = false;
                Assert.Equal(traversal, Q1.TravSig(await store.TraverseAsync(repo2, new(new CodeNodeId("node:n1a"), maxDepth: 3))));
                Assert.Equal(sigT2, Q1.Render(await store.FindOccurrencesAsync(new(repo2, t2))));
            }

            Assert.Equal(beforeNative2, Q1.NativeSnapshot(path2));
            using (var retry = new LatticeCodeGraphStore(path2, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                await retry.CompleteIndexRunAsync(
                    new(repo2, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo2, new CodeIndexRunId("run:q2"), []));
                Assert.Equal("n[node:n1a,node:n1b]e[edge:n1ab]d1o0",
                    Q1.TravSig(await retry.TraverseAsync(repo2, new(new CodeNodeId("node:n1a"), maxDepth: 3))));
                Assert.Equal(CodeOccurrenceAvailability.Available,
                    (await retry.FindOccurrencesAsync(new(repo2, t2))).Availability);
                Assert.Empty((await retry.FindOccurrencesAsync(new(repo2, t2))).Occurrences);
            }

            var manifest = Q1.MirrorManifests(path2).Single();
            Assert.Equal(["node:n1a", "node:n1b"], manifest.NodeIds);
            Assert.Equal(["edge:n1ab"], manifest.EdgeIds);
            Assert.Equal(2, Q1.GraphNodePayloads(path2).Count);

            using (var reopened = new LatticeCodeGraphStore(path2, new LatticeDbStoreOptions { UseNativeTraversal = true }))
            {
                Assert.Empty((await reopened.FindOccurrencesAsync(new(repo2, t2))).Occurrences);
                Assert.Single((await reopened.FindOccurrencesAsync(new(repo2, t1))).Occurrences);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path1);
            Q1.DeleteDatabase(path2);
        }
    }

    // P13 — Tampered durable occurrence records fail closed on open, and the
    // failed open must not mutate the tampered rows.
    [Fact]
    public async Task P13_TamperedDurableOccurrenceRows_FailClosedOpenWithRowsUnchanged()
    {
        var path = Q1.TemporaryDatabasePath();
        var good = path + ".good";
        try
        {
            var repo = new CodeRepositoryId("repo:q1:tamper");
            var t1 = Q1.Target(repo, "T1");
            var fact = Q1.Fact(repo, "unit:tm", t1, "src/TM.cs", 1, 1);
            using (var store = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(store, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:tm", [Q1.Node("node:tm")], [fact], CodeRelationshipCoverageState.Produced));
            }

            File.Copy(path, good, overwrite: true);
            using (var healthy = new LatticeCodeGraphStore(path))
            {
                Assert.Single((await healthy.FindOccurrencesAsync(new(repo, t1))).Occurrences);
            }

            var unitKey = Q1.UnitRowKey(repo, Q1.Plugin.Value, "unit:tm");
            Q1.SetRow(path, Q1.OccurrenceUnitLabel, unitKey, "not-base64!!");
            var tamperedUnits = Q1.Rows(path, Q1.OccurrenceUnitLabel);
            var payloadRejection = Assert.Throws<CodeOccurrenceSchemaException>(() => new LatticeCodeGraphStore(path));
            Assert.Equal(0, payloadRejection.ActualVersion);
            Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, payloadRejection.ExpectedVersion);
            Assert.Equal(tamperedUnits, Q1.Rows(path, Q1.OccurrenceUnitLabel));

            File.Copy(good, path, overwrite: true);
            var metaKey = Q1.MetaRowKey(repo);
            var metaRow = Q1.Rows(path, Q1.OccurrenceMetaLabel).Single(pair => pair.Key == metaKey);
            string tamperedJson;
            using (var meta = Q1.Decode(metaRow.Payload))
            {
                tamperedJson = Q1.RewriteJsonProperty(meta.RootElement.GetRawText(), "Namespace", "andirit-occurrence-facts/2");
            }

            Q1.SetRow(path, Q1.OccurrenceMetaLabel, metaKey, Convert.ToBase64String(Encoding.UTF8.GetBytes(tamperedJson)));
            var tamperedMeta = Q1.Rows(path, Q1.OccurrenceMetaLabel);
            var namespaceRejection = Assert.Throws<CodeOccurrenceSchemaException>(() => new LatticeCodeGraphStore(path));
            Assert.Equal(1, namespaceRejection.ActualVersion);
            Assert.Equal(LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion, namespaceRejection.ExpectedVersion);
            Assert.Equal(tamperedMeta, Q1.Rows(path, Q1.OccurrenceMetaLabel));

            File.Copy(good, path, overwrite: true);
            using (var restored = new LatticeCodeGraphStore(path))
            {
                Assert.Single((await restored.FindOccurrencesAsync(new(repo, t1))).Occurrences);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
            Q1.DeleteDatabase(good);
        }
    }

    // P14 — Baseline and index-state projection after a Branch-B complete
    // failure on an empty staged run.
    [Fact]
    public async Task P14_BaselineAndIndexStateProjection_AfterCompleteFailure()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:indexstate");
            var t1 = Q1.Target(repo, "T1");
            var fact = Q1.Fact(repo, "unit:s1", t1, "src/S.cs", 2, 2);
            using (var setup = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(setup, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:s1", [Q1.Node("node:s1")], [fact], CodeRelationshipCoverageState.Produced));
                await setup.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
            }

            var beforeSnapshot = Q1.Snapshot(path);
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "before-commit"))
            {
                var baseline = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
                arm = true;
                await Assert.ThrowsAsync<ReviewerPersistenceException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;
                Assert.Equal(baseline, Q1.Render(await store.FindOccurrencesAsync(new(repo, t1))));
                Assert.Equal("run:p1", (await store.GetLatestIndexStateAsync(repo))!.IndexRunId.Value);
            }

            Assert.Equal(beforeSnapshot, Q1.Snapshot(path));
            var baselineRows = Q1.Rows(path, Q1.BaselineLabel);
            Assert.Single(baselineRows);
            Assert.Equal(Q1.BaselineRowKey(repo, "run:q2"), baselineRows[0].Key);
            Assert.Equal("run:p1", baselineRows[0].Payload);

            using (var retry = new LatticeCodeGraphStore(path))
            {
                await retry.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(4), [Q1.Plugin]),
                    new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), []));
                Assert.Equal("run:q2", (await retry.GetLatestIndexStateAsync(repo))!.IndexRunId.Value);
            }

            Assert.Empty(Q1.Rows(path, Q1.BaselineLabel));
            using (var meta = Q1.Decode(Q1.Rows(path, Q1.OccurrenceMetaLabel).Single().Payload))
            {
                Assert.Equal("run:q2", meta.RootElement.Str("PublicationIndexRunId"));
                Assert.Equal(1, meta.RootElement.Int("FactCount"));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                Assert.Equal("run:q2", (await reopened.GetLatestIndexStateAsync(repo))!.IndexRunId.Value);
                Assert.Equal("run:q2", (await reopened.GetLatestPublicationAsync(repo))!.IndexRunId.Value);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P15 — Known-vs-unknown contrast: an after-commit fault quarantines the
    // instance (no replay, no reads, no mutations), while reopen reconciles.
    [Fact]
    public async Task P15_AfterCommitFailure_QuarantinesReadsAndMutationsReopenReconciles()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:unknown");
            var t1 = Q1.Target(repo, "T1");
            var t2 = Q1.Target(repo, "T2");
            var factA = Q1.Fact(repo, "unit:u1", t1, "src/U1.cs", 1, 1);
            var factB = Q1.Fact(repo, "unit:u2", t2, "src/U2.cs", 2, 2);
            var arm = false;
            using (var store = Q1.OpenGuarded(path, point => arm && point == "after-commit"))
            {
                await Q1.PublishAsync(store, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:u1", [Q1.Node("node:u1")], [factA], CodeRelationshipCoverageState.Produced));
                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q2"), Q1.Started, plugins: [Q1.Plugin]));
                await store.StageIndexUnitAsync(
                    Q1.Unit(repo, "run:q2", "unit:u2", [Q1.Node("node:u2")], [factB], CodeRelationshipCoverageState.Produced));

                arm = true;
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                    async () => await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q2"), Q1.Started, CodeIndexRunStatus.Completed, Q1.Started.AddSeconds(3), [Q1.Plugin]),
                        new CodeRepositoryIndexState(repo, new CodeIndexRunId("run:q2"), [])));
                arm = false;

                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                    async () => await store.FindOccurrencesAsync(new(repo, t1)));
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                    async () => await store.GetPublishedUnitsAsync(repo));
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                    async () => await store.TraverseAsync(repo, new(new CodeNodeId("node:u1"))));
                await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                    async () => await store.StoreIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:q3"), Q1.Started, plugins: [Q1.Plugin])));

                var health = await store.CheckHealthAsync();
                Assert.Equal(CodeGraphStoreHealthStatus.Healthy, health.Status);
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var durableT2 = await reopened.FindOccurrencesAsync(new(repo, t2));
                Assert.Single(durableT2.Occurrences);
                Assert.Equal("run:q2", durableT2.Publication!.IndexRunId.Value);
                await reopened.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:q3"), Q1.Started, plugins: [Q1.Plugin]));
                Assert.Single((await reopened.FindOccurrencesAsync(new(repo, t1))).Occurrences);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // P16 — Schema constants and metadata tamper fail-closed contract.
    [Fact]
    public async Task P16_SchemaConstantsAndMetadataTamper_FailClosedExceptionContract()
    {
        var path = Q1.TemporaryDatabasePath();
        var good = path + ".good";
        try
        {
            Assert.Equal(2, LatticeCodeGraphStore.CurrentSchemaVersion);
            Assert.Equal(1, LatticeCodeGraphStore.CurrentOccurrenceSchemaVersion);
            Assert.Equal("andirit-occurrence-facts/1", LatticeCodeGraphStore.OccurrenceNamespace);

            var repo = new CodeRepositoryId("repo:q1:schema");
            var t1 = Q1.Target(repo, "T1");
            var fact = Q1.Fact(repo, "unit:sc", t1, "src/SC.cs", 1, 1);
            using (var store = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(store, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:sc", [Q1.Node("node:sc")], [fact], CodeRelationshipCoverageState.Produced));
            }

            File.Copy(path, good, overwrite: true);
            Assert.Equal(2, Q1.SchemaVersion(path));
            Q1.SetSchemaVersion(path, 3);
            Assert.Equal(3, Q1.SchemaVersion(path));

            var rejected = Assert.Throws<LatticeCodeGraphSchemaException>(() => new LatticeCodeGraphStore(path));
            Assert.Equal(3, rejected.ActualVersion);
            Assert.Equal(2, rejected.ExpectedVersion);

            File.Copy(good, path, overwrite: true);
            using (var restored = new LatticeCodeGraphStore(path))
            {
                var reopened = await restored.FindOccurrencesAsync(new(repo, t1));
                Assert.Single(reopened.Occurrences);
                Assert.Equal(CodeOccurrenceAvailability.Available, reopened.Availability);
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
            Q1.DeleteDatabase(good);
        }
    }

    // P17 — No-fault control: three successive opens agree on reader results and
    // on the closed durable byte image.
    [Fact]
    public async Task P17_NoFaultControl_ThreeSuccessiveOpensParity()
    {
        var path = Q1.TemporaryDatabasePath();
        try
        {
            var repo = new CodeRepositoryId("repo:q1:control");
            var t1 = Q1.Target(repo, "T1");
            var fact1 = Q1.Fact(repo, "unit:c1", t1, "src/C1.cs", 1, 1);
            var fact2 = Q1.Fact(repo, "unit:c1", t1, "src/C2.cs", 2, 2);
            string first;
            using (var store = new LatticeCodeGraphStore(path))
            {
                await Q1.PublishAsync(store, repo, "run:p1",
                    Q1.Unit(repo, "run:p1", "unit:c1", [Q1.Node("node:c1")], [fact1, fact2], CodeRelationshipCoverageState.Produced));
                first = Q1.Render(await store.FindOccurrencesAsync(new(repo, t1)));
            }

            var firstSnapshot = Q1.Snapshot(path);
            for (var attempt = 0; attempt < 3; attempt++)
            {
                using (var reopened = new LatticeCodeGraphStore(path))
                {
                    Assert.Equal(first, Q1.Render(await reopened.FindOccurrencesAsync(new(repo, t1))));
                }

                Assert.Equal(firstSnapshot, Q1.Snapshot(path));
            }
        }
        finally
        {
            Q1.DeleteDatabase(path);
        }
    }

    // >>> PROBE SECTION ANCHOR <<<
}

/// <summary>
/// Probe helpers: durable row keys/labels, native row access and byte-level
/// tampering, reviewer-owned JSON accessors, and canonical fact/unit builders.
/// Row keys mirror DurableCommandLog's protocol identities (repo "\n" run etc.);
/// they are duplicated here deliberately so the probes do not depend on the
/// production implementation's internals for their own decoding.
/// </summary>
internal static class Q1
{
    internal const string
        RepositoryLabel = "HetuRepository",
        RunLabel = "HetuRun",
        IndexStateLabel = "HetuIndexState",
        UnitLabel = "HetuUnit",
        StageLabel = "HetuStage",
        BaselineLabel = "HetuBaseline",
        OccurrenceUnitLabel = "AndiritOccurrenceUnit",
        OccurrencePageLabel = "AndiritOccurrencePage",
        OccurrenceMetaLabel = "AndiritOccurrenceMeta",
        MirrorLabel = "HetuMirror",
        GraphNodeLabel = "HetuGraphNode";

    internal static readonly CodePluginId Plugin = new("plugin:q1reviewer");
    internal static readonly DateTimeOffset Started = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    internal static readonly string Scope = "analysisscope1:sha256:" + Hex("q1-reviewer|scope");
    internal static readonly string Universe = "compilation-universe1:sha256:" + Hex("q1-reviewer|universe");

    internal static string[] CoreLabels() =>
    [
        RepositoryLabel, RunLabel, IndexStateLabel, UnitLabel, StageLabel, BaselineLabel,
        OccurrenceUnitLabel, OccurrenceMetaLabel, OccurrencePageLabel
    ];

    internal static CodeFactOrigin Origin(CodeRepositoryId repo, string run, string unit) =>
        new(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit));

    internal static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name)
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

    internal static string Hex(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    internal static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target,
        string path, int line, int column) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source", "content:invocation");

    internal static CodeOccurrenceFact FactShared(
        CodeRepositoryId repo, string sourceUnit, CodeOccurrenceTargetId target,
        CodeOccurrenceContributor contributor, string path, int line, int column) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, sourceUnit, contributor,
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source", "content:union");

    internal static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeOccurrenceFact> facts,
        CodeRelationshipCoverageState state,
        CodeOccurrenceCoverage? explicitCoverage = null) =>
        new(
            Origin(repo, run, unit),
            nodes,
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: explicitCoverage ?? new CodeOccurrenceCoverage(
                facts.Count, 0, state,
                scopeId: Scope, universeId: Universe,
                declaredUnitCount: 1, verifiedUnitCount: 1));

    internal static CodeIndexUnitReplacement GraphOnlyUnit(
        CodeRepositoryId repo, string run, string unit) =>
        new(Origin(repo, run, unit), [Node("node:" + unit)]);

    internal static CodeIndexUnitReplacement GraphUnit(
        CodeRepositoryId repo, string run, string unit,
        IReadOnlyList<CodeGraphNode> nodes,
        IReadOnlyList<CodeGraphEdge> edges,
        IReadOnlyList<CodeOccurrenceFact> facts,
        CodeRelationshipCoverageState state,
        CodeOccurrenceCoverage? explicitCoverage = null) =>
        new(
            Origin(repo, run, unit),
            nodes,
            edges: edges,
            occurrences: facts,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: explicitCoverage ?? new CodeOccurrenceCoverage(
                facts.Count, 0, state, Scope, Universe, 1, 1));

    internal static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId("symbol:" + id));

    internal static CodeGraphEdge Edge(string id, string source, string target, CodeEdgeKind kind) =>
        new(new CodeEdgeId(id), new CodeNodeId(source), new CodeNodeId(target), kind,
            new CodeEvidence(EvidenceQuality.Resolved, Plugin.Value, "1.0.0"));

    /// <summary>Opens a store whose mechanical fault point throws the reviewer's
    /// own exception so the rethrow identity of each recovery branch is provable.</summary>
    internal static LatticeCodeGraphStore OpenGuarded(string path, Predicate<string> throwAt) =>
        new(path, null, point =>
        {
            if (throwAt(point))
                throw new ReviewerPersistenceException();
        });

    internal static LatticeCodeGraphStore OpenNativeGuarded(string path, Predicate<string> throwAt) =>
        new(path, new LatticeDbStoreOptions { UseNativeTraversal = true }, point =>
        {
            if (throwAt(point))
                throw new ReviewerPersistenceException();
        });

    internal static string RewriteJsonProperty(string rawJson, string propertyName, string value)
    {
        using var document = JsonDocument.Parse(rawJson);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.NameEquals(propertyName))
                    writer.WriteString(propertyName, value);
                else
                    property.WriteTo(writer);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static async Task PublishAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
        params CodeIndexUnitReplacement[] units)
    {
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new CodeRepositoryManifest(repo));
        await store.StoreIndexRunAsync(new(repo, runId, Started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [Plugin]),
            new CodeRepositoryIndexState(repo, runId, []));
    }

    internal static string TemporaryDatabasePath() =>
        Path.Combine(Path.GetTempPath(), $"andirit-q1reviewer-{Guid.NewGuid():N}.ltdb");

    internal static void DeleteDatabase(string path)
    {
        foreach (var candidate in new[] { path, path + "-wal", path + "-journal", path + ".wal" })
        {
            if (File.Exists(candidate))
                File.Delete(candidate);
        }
    }

    internal static List<(string Key, string Payload)> Rows(string path, string label)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        using var query = db.Prepare($"MATCH (s:{label}) RETURN s.key, s.payload");
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows
            .Select(row => (row.GetValue(0).AsString(), row.GetValue(1).AsString()))
            .OrderBy(pair => pair.Item1, StringComparer.Ordinal)
            .ToList();
    }

    internal static List<string> GraphNodePayloads(string path)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        using var query = db.Prepare($"MATCH (s:{GraphNodeLabel}) RETURN s.hetu_json");
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows
            .Select(row => row.GetValue(0).AsString())
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>Decodes the HetuMirror manifest rows. The payload is a
    /// JSON-encoded string that itself contains a manifest object whose Nodes/
    /// Edges maps key Hetu identities by native ids.</summary>
    internal static List<(string RepositoryId, List<string> NodeIds, List<string> EdgeIds)>
        MirrorManifests(string path)
    {
        var manifests = new List<(string, List<string>, List<string>)>();
        foreach (var (key, payload) in Rows(path, MirrorLabel))
        {
            using var wrapper = JsonDocument.Parse(Convert.FromBase64String(payload));
            using var manifest = JsonDocument.Parse(wrapper.RootElement.GetString()!);
            var nodes = manifest.RootElement.GetProperty("Nodes").EnumerateObject()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal).ToList();
            var edges = manifest.RootElement.GetProperty("Edges").EnumerateObject()
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal).ToList();
            manifests.Add((key, nodes, edges));
        }

        return manifests;
    }

    internal static void SetRow(string path, string label, string key, string payload)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var delete = db.Prepare($"MATCH (s:{label}) WHERE s.key = $k DELETE s"))
        {
            delete.Bind("k", LatticeValue.From(key));
            using var deleted = delete.Execute(txn);
            deleted.ReadAll();
        }

        using (var create = db.Prepare($"CREATE (:{label} {{key: $k, payload: $p}})"))
        {
            create.Bind("k", LatticeValue.From(key));
            create.Bind("p", LatticeValue.From(payload));
            using var created = create.Execute(txn);
            created.ReadAll();
        }

        txn.Commit();
    }

    internal static void SetSchemaVersion(string path, long version)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginWriteTransaction();
        using (var delete = db.Prepare("MATCH (s:HetuMetadata) DELETE s"))
        {
            using var deleted = delete.Execute(txn);
            deleted.ReadAll();
        }

        using (var create = db.Prepare("CREATE (:HetuMetadata {key: $k, schemaVersion: $v})"))
        {
            create.Bind("k", LatticeValue.From("schema"));
            create.Bind("v", LatticeValue.From(version));
            using var created = create.Execute(txn);
            created.ReadAll();
        }

        txn.Commit();
    }

    internal static long SchemaVersion(string path)
    {
        using var db = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = true });
        using var txn = db.BeginReadTransaction();
        using var query = db.Prepare("MATCH (s:HetuMetadata) WHERE s.key = $k RETURN s.schemaVersion");
        query.Bind("k", LatticeValue.From("schema"));
        using var result = query.Execute(txn);
        var rows = result.ReadAll();
        txn.Commit();
        return rows.Count == 0 ? 0 : rows[0].GetValue(0).AsInt64();
    }

    internal static List<string> Snapshot(string path)
    {
        var snapshot = new List<string>();
        foreach (var label in CoreLabels())
        {
            foreach (var (key, payload) in Rows(path, label))
                snapshot.Add($"{label}|{key}|{payload}");
        }

        return snapshot;
    }

    /// <summary>Labels a definitively failed mutation must never modify. Segment
    /// rows (HetuStage) are excluded because staging a contribution is itself a
    /// durable, successful mutation; the failed publication must leave these
    /// protected labels byte-identical.</summary>
    internal static string[] ProtectedLabels() =>
    [
        RepositoryLabel, RunLabel, IndexStateLabel, UnitLabel, BaselineLabel,
        OccurrenceUnitLabel, OccurrenceMetaLabel, OccurrencePageLabel
    ];

    internal static List<string> Subset(string path, string[] labels)
    {
        var subset = new List<string>();
        foreach (var label in labels)
        {
            foreach (var (key, payload) in Rows(path, label))
                subset.Add($"{label}|{key}|{payload}");
        }

        return subset;
    }

    internal static List<string> NativeSnapshot(string path)
    {
        var snapshot = Snapshot(path);
        snapshot.AddRange(GraphNodePayloads(path).Select(payload => $"{GraphNodeLabel}|{payload}"));
        foreach (var (repository, nodes, edges) in MirrorManifests(path))
        {
            snapshot.Add(
                $"{MirrorLabel}|{repository}|{string.Join(";", nodes)}|{string.Join(";", edges)}");
        }

        return snapshot;
    }

    internal static JsonDocument Decode(string payload) =>
        JsonDocument.Parse(Convert.FromBase64String(payload));

    internal static string UnitRowKey(CodeRepositoryId repo, string plugin, string unit) =>
        $"{repo.Value}\n{plugin}\n{unit}";

    internal static string MetaRowKey(CodeRepositoryId repo) => repo.Value;

    internal static string PageRowKey(CodeRepositoryId repo, CodeOccurrenceTargetId target) =>
        $"{repo.Value}\n{target.Value}";

    internal static string RunRowKey(CodeRepositoryId repo, string run) =>
        $"{repo.Value}\n{run}";

    internal static string StageRowKey(CodeRepositoryId repo, string run, string plugin, string unit) =>
        $"{repo.Value}\n{run}\n{plugin}\n{unit}";

    internal static string BaselineRowKey(CodeRepositoryId repo, string run) =>
        $"{repo.Value}\n{run}";

    /// <summary>Deterministic comparable projection of an occurrence reader
    /// result, including every coverage counter, so recover-in-place vs. fresh
    /// reopen equality is assertable in one line.</summary>
    internal static string Render(CodeOccurrenceQueryResult result)
    {
        var coverage = result.Coverage is null
            ? "coverage:null"
            : "coverage:" + CoverSig(result.Coverage);
        return string.Join("|",
            result.Availability,
            result.Publication?.IndexRunId.Value ?? "pub:null",
            string.Join(";", result.Occurrences.Select(hit =>
                hit.Id.Value + ":" +
                hit.TargetId.Value + ":" +
                hit.Category + ":" +
                hit.Span.Path + ":" + hit.Span.StartLine + ":" + hit.Span.StartColumn + ":" +
                hit.Span.EndLine + ":" + hit.Span.EndColumn + ":" + hit.SourceUnitId + ":" +
                string.Join("+", hit.Contributors.Select(contributor =>
                    contributor.PluginId.Value + "@" + contributor.IndexUnitId)) + ":" +
                hit.SemanticsVersion)),
            "total:" + result.TotalMatches,
            "examined:" + result.Examined,
            "refusal:" + (result.RefusalReason ?? "-"),
            coverage,
            "complete:" + result.IsComplete);
    }

    internal static string CoverSig(CodeOccurrenceCoverage coverage) =>
        string.Join(",",
            coverage.State,
            coverage.InvocationFacts,
            coverage.UnsupportedCategories,
            coverage.DeclaredUnitCount,
            coverage.VerifiedUnitCount,
            coverage.External,
            coverage.Ambiguous,
            coverage.Unresolved,
            coverage.Unsupported,
            coverage.ConditionalInactive,
            string.Join(";", coverage.UnavailableReasons
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => pair.Key + "=" + pair.Value)),
            coverage.ScopeId ?? "-",
            coverage.UniverseId ?? "-");

    internal static string TravSig(CodeGraphTraversalResult result) =>
        "n[" + string.Join(",", result.Nodes.Select(node => node.Id.Value)) + "]" +
        "e[" + string.Join(",", result.Edges.Select(edge => edge.Id.Value)) + "]" +
        "d" + result.DepthReached + "o" + result.OmittedFrontierCount;
}

/// <summary>Reviewer-owned durable shape assertions shared by probes: decodes a
/// persisted occurrence fact and asserts every field against the reviewer's own
/// in-memory fact object (recomputed, independent of production deserialization).</summary>
internal static class DurableAssert
{
    internal static void FactMatches(
        JsonElement decoded, CodeOccurrenceFact expected)
    {
        Assert.Equal(expected.Id.Value, decoded.IdValue("Id"));
        Assert.Equal(expected.TargetId.Value, decoded.IdValue("TargetId"));
        Assert.Equal(expected.TargetDocumentId, decoded.Str("TargetDocumentId"));
        Assert.Equal(expected.RepositoryId.Value, decoded.IdValue("RepositoryId"));
        Assert.Equal((int)expected.Category, decoded.Int("Category"));
        Assert.Equal(expected.SourceUnitId, decoded.Str("SourceUnitId"));
        Assert.Equal(expected.Contributor.PluginId.Value, decoded.Obj("Contributor").IdValue("PluginId"));
        Assert.Equal(expected.Contributor.IndexUnitId, decoded.Obj("Contributor").Str("IndexUnitId"));
        Assert.Equal(expected.Span.Path, decoded.Obj("Span").Str("Path"));
        Assert.Equal(expected.Span.StartLine, decoded.Obj("Span").Int("StartLine"));
        Assert.Equal(expected.Span.StartColumn, decoded.Obj("Span").Int("StartColumn"));
        Assert.Equal(expected.Span.EndLine, decoded.Obj("Span").Int("EndLine"));
        Assert.Equal(expected.Span.EndColumn, decoded.Obj("Span").Int("EndColumn"));
        Assert.Equal(expected.SourceHash, decoded.Str("SourceHash"));
        Assert.Equal(expected.ContentHash, decoded.Str("ContentHash"));
        Assert.Equal(expected.PluginVersion, decoded.Str("PluginVersion"));
        Assert.Equal(expected.SemanticsVersion, decoded.Int("SemanticsVersion"));
        Assert.Equal(expected.PayloadHash, decoded.Str("PayloadHash"));
    }

    internal static void CoverageMatches(JsonElement decoded, CodeOccurrenceCoverage expected)
    {
        Assert.Equal((int)expected.State, decoded.Int("State"));
        Assert.Equal(expected.InvocationFacts, decoded.Int("InvocationFacts"));
        Assert.Equal(expected.UnsupportedCategories, decoded.Int("UnsupportedCategories"));
        Assert.Equal(expected.DeclaredUnitCount, decoded.Int("DeclaredUnitCount"));
        Assert.Equal(expected.VerifiedUnitCount, decoded.Int("VerifiedUnitCount"));
        Assert.Equal(expected.External, decoded.Int("External"));
        Assert.Equal(expected.Ambiguous, decoded.Int("Ambiguous"));
        Assert.Equal(expected.Unresolved, decoded.Int("Unresolved"));
        Assert.Equal(expected.Unsupported, decoded.Int("Unsupported"));
        Assert.Equal(expected.ConditionalInactive, decoded.Int("ConditionalInactive"));
        var reasons = decoded.GetProperty("UnavailableReasons").EnumerateObject()
            .Select(property => (property.Name, property.Value.GetInt32()))
            .OrderBy(pair => pair.Name, StringComparer.Ordinal).ToList();
        var expectedReasons = expected.UnavailableReasons
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => (pair.Key, pair.Value)).ToList();
        Assert.Equal(expectedReasons, reasons);
        Assert.Equal(expected.ScopeId, decoded.OptStr("ScopeId"));
        Assert.Equal(expected.UniverseId, decoded.OptStr("UniverseId"));
    }
}

/// <summary>Independent JSON accessors over the reviewer-owned decoder.</summary>
internal static class J
{
    internal static JsonElement Obj(this JsonElement element, string name) =>
        element.GetProperty(name);

    internal static string Str(this JsonElement element, string name) =>
        element.GetProperty(name).GetString()!;

    internal static string? OptStr(this JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    internal static int Int(this JsonElement element, string name) =>
        element.GetProperty(name).GetInt32();

    internal static bool Bool(this JsonElement element, string name) =>
        element.GetProperty(name).GetBoolean();

    internal static JsonElement Arr(this JsonElement element, string name) =>
        element.GetProperty(name);

    internal static string IdValue(this JsonElement element, string name) =>
        element.GetProperty(name).GetProperty("Value").GetString()!;
}

/// <summary>The reviewer's own injected persistence failure. Deliberately NOT a
/// LatticeException so TranslateNative cannot rewrite its identity.</summary>
internal sealed class ReviewerPersistenceException : Exception
{
    internal ReviewerPersistenceException()
        : base("reviewer injected persistence failure")
    {
    }
}
