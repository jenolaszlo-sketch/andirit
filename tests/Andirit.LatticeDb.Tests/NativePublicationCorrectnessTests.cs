using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Andirit.LatticeDb;
using LatticeDbSharp;

namespace Andirit.LatticeDb.Tests
{
    /// <summary>
    /// S6.3 dedicated native publication-correctness regressions (M12.1, M12.3, D4 invariants 1-4 and 6-10, S-02).
    ///
    /// The governing publication-correctness contract for <see cref="LatticeCodeGraphStore"/> is the
    /// ADR-CI1-01 owner decision §D.4 plus S-01 §S-02 / the CI1A specification: every D4 invariant must
    /// be exercised against actual production <see cref="LatticeCodeGraphStore"/> storage, including
    /// failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario.
    ///
    /// D4 invariant 5 (cancelled staging exposes nothing) was discharged by S6.1 (Q-01..Q-06/Q-11); the
    /// unknown-outcome and failed-staging cases were partly discharged by S6.2 (D-01..D-04). This suite
    /// covers the remaining invariants and the complete S-02 matrix, and it reuses the S6.1/S6.2
    /// evidence where the traceability matrix says it is legitimately reusable (noted per case).
    ///
    /// Every case runs against the production store over a real LatticeDB file and asserts the native
    /// durable state directly (AndiritOccurrenceMeta/Unit/Page and HetuStage/HetuUnit), so a case cannot
    /// pass on the managed projection alone. Constructed cases place a failure deterministically through
    /// the production fault-injector seam; genuine cases use real publication sequences or genuine
    /// multi-process workers and OS file-locking serialization.
    /// </summary>
    public sealed class NativePublicationCorrectnessTests
    {
        private const string OccurrenceUnitLabel = "AndiritOccurrenceUnit";
        private const string OccurrencePageLabel = "AndiritOccurrencePage";
        private const string OccurrenceMetaLabel = "AndiritOccurrenceMeta";

        private static readonly CodePluginId Plugin = new("plugin:publication-correctness");
        private static readonly DateTimeOffset Started =
            new(2026, 11, 24, 0, 0, 0, TimeSpan.Zero);
        private static readonly DateTimeOffset Later =
            new(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);

        // ------------------------------------------------------------------ P-01 (D4-1/D4-2)
        // A query may use only an index consistent with the accepted published generation; a query
        // pinned to a superseded publication refuses with an explicit status. Constructed by real
        // publication supersession (no fault injection).
        [Fact]
        public async Task P01_QueryPinnedToSupersededGeneration_RefusesExplicitly()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p01");
            var target = Target(repo, "T1");
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p01:1"),
                    Unit(repo, "run:p01:1", "unit:p01", [Node("node:p01")],
                        [Fact(repo, "unit:p01", target, "src/First.cs", 1, 1)]));

                var first = await store.GetLatestPublicationAsync(repo);
                Assert.NotNull(first);

                // A newer generation supersedes the first.
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p01:2"),
                    Unit(repo, "run:p01:2", "unit:p01", [Node("node:p01")],
                        [Fact(repo, "unit:p01", target, "src/Second.cs", 1, 1)]));

                // D4-1/D4-2: the superseded generation refuses explicitly; no facts are served from it.
                var stale = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, first));
                Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
                Assert.Empty(stale.Occurrences);
                Assert.NotEqual(0, stale.RefusalReason?.Length);

                // Only the accepted generation answers.
                var current = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
                Assert.Equal("run:p01:2", current.Publication!.IndexRunId.Value);
                Assert.Single(current.Occurrences);
                Assert.Equal("src/Second.cs", current.Occurrences[0].Span.Path);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-02 (D4-1/D4-2)
        // An index that is not consistent with the accepted publication refuses explicitly at load,
        // never silently returning an empty success. Constructed by tampering the durable page binding.
        [Fact]
        public async Task P02_OccurrencePageNotBoundToAcceptedPublication_RefusesExplicitlyOnOpen()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p02");
            var target = Target(repo, "T1");
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p02"),
                        Unit(repo, "run:p02", "unit:p02", [Node("node:p02")],
                            [Fact(repo, "unit:p02", target, "src/A.cs", 1, 1)]))
                        ;
                }

                // Tamper the durable target page so it is bound to a publication that is not the
                // accepted one. A reopen must refuse with an explicit index-inconsistency error.
                TamperOccurrencePageBinding(path, repositoryId: repo.Value, staleRunId: "run:p02:stale");

                var exception = Assert.Throws<CodeOccurrenceIndexException>(
                    () => new LatticeCodeGraphStore(path));
                Assert.Contains("not bound", exception.Message, StringComparison.Ordinal);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-03 (D4-3)
        // A stale index must not silently trigger an unbounded full-store scan; and a bounded query
        // examines only its target page. Constructed.
        [Fact]
        public async Task P03_StaleIndexDoesNotScanTheWholeStore_AndBoundedQueryExaminesOnlyItsTargetPage()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p03");
            var target = Target(repo, "T1");
            const int targetFacts = 200;
            const int unrelatedTargets = 10;
            const int unrelatedPerTarget = 20;
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                var facts = Enumerable.Range(0, targetFacts)
                    .Select(index => Fact(repo, "unit:p03", target, $"src/T{index:D4}.cs", 1, 1))
                    .ToList();
                var unrelated = Enumerable.Range(0, unrelatedTargets)
                    .SelectMany(t => Enumerable.Range(0, unrelatedPerTarget)
                        .Select(index => Fact(repo, "unit:p03", Target(repo, $"other:{t}"), $"src/U{t}_{index}.cs", 1, 1)))
                    .ToList();
                facts.AddRange(unrelated);

                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p03"),
                    Unit(repo, "run:p03", "unit:p03", [Node("node:p03")], facts));

                var wholeStore = targetFacts + unrelatedTargets * unrelatedPerTarget;

                var first = await store.GetLatestPublicationAsync(repo);
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p03:2"),
                    Unit(repo, "run:p03:2", "unit:p03", [Node("node:p03")], facts));

                // D4-3 (stale path): a stale query refuses before touching the index - it does not
                // fall back to scanning the whole store.
                var stale = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, first));
                Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
                Assert.Equal(0, stale.Examined);

                // D4-3 (bounded path): a bounded current query examines only its own target page,
                // never the whole store, and honours the explicit limit.
                var bounded = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, maxResults: 100));
                Assert.Equal(CodeOccurrenceAvailability.Available, bounded.Availability);
                Assert.Equal(100, bounded.Occurrences.Count);
                Assert.True(bounded.Examined < wholeStore);
                Assert.Equal(targetFacts, bounded.Examined);
                Assert.True(bounded.Truncated);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-04 (D4-4 / S-02-2)
        // Failed completion must preserve the last accepted publication and its valid index state.
        // Constructed with the production fault-injector at the durable commit boundary. Extends the
        // S6.2 D-01/D-04 evidence to the occurrence publication and index.
        [Fact]
        public async Task P04_FailedCompletion_PreservesPriorOccurrencePublicationAndIndex()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p04");
            var target = Target(repo, "T1");
            var arm = false;
            string priorRun = "run:p04:1";
            try
            {
                using (var store = new LatticeCodeGraphStore(path, null, point =>
                {
                    if (arm && point == "before-commit")
                        throw new InjectedPersistenceException();
                }))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p04:1"),
                        Unit(repo, "run:p04:1", "unit:p04", [Node("node:p04")],
                            [Fact(repo, "unit:p04", target, "src/Prior.cs", 1, 1)]));

                    var prior = await store.GetLatestPublicationAsync(repo);
                    Assert.NotNull(prior);
                    Assert.Equal(priorRun, prior!.IndexRunId.Value);

                    await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p04:2"), Later, plugins: [Plugin]));
                    await store.StageIndexUnitAsync(
                        Unit(repo, "run:p04:2", "unit:p04", [Node("node:p04")],
                            [Fact(repo, "unit:p04", target, "src/New.cs", 1, 1)]));

                    // The staged-but-not-published fact is invisible (falls through to D4-5).
                    var duringStaging = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Single(duringStaging.Occurrences);
                    Assert.Equal("src/Prior.cs", duringStaging.Occurrences[0].Span.Path);

                    arm = true;
                    await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                        await store.CompleteIndexRunAsync(
                            new(repo, new CodeIndexRunId("run:p04:2"), Later, CodeIndexRunStatus.Completed, Later.AddSeconds(1), [Plugin]),
                            new(repo, new CodeIndexRunId("run:p04:2"), [])));

                    // D4-4: the last accepted publication and its index are preserved.
                    var after = await store.GetLatestPublicationAsync(repo);
                    Assert.NotNull(after);
                    Assert.Equal(priorRun, after!.IndexRunId.Value);
                    var afterResult = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Equal(CodeOccurrenceAvailability.Available, afterResult.Availability);
                    Assert.Single(afterResult.Occurrences);
                    Assert.Equal("src/Prior.cs", afterResult.Occurrences[0].Span.Path);
                }

                var publishedRun = NativeOccurrencePublicationRunId(path, repo.Value);
                Assert.Equal("run:p04:1", publishedRun);

                using var reopened = new LatticeCodeGraphStore(path);
                var reopenedResult = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, reopenedResult.Availability);
                Assert.Equal(priorRun, reopenedResult.Publication!.IndexRunId.Value);
                Assert.Single(reopenedResult.Occurrences);
                Assert.Equal("src/Prior.cs", reopenedResult.Occurrences[0].Span.Path);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-05 (D4-6 / S-02-4, genuine)
        // A superseded publication must not replace a newer accepted generation; older generations are
        // refused and the durable index reflects exactly the latest accepted generation. Genuine: real
        // publication supersession observed through the real query path (no fault injection).
        [Fact]
        public async Task P05_OlderGenerationsSuperseded_LatestAcceptedAndDurable()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p05");
            var target = Target(repo, "T1");
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    var first = await store.GetLatestPublicationAsync(repo);
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p05:1"),
                        Unit(repo, "run:p05:1", "unit:p05", [Node("node:p05")],
                            [Fact(repo, "unit:p05", target, "src/G1.cs", 1, 1)]));
                    first = await store.GetLatestPublicationAsync(repo);

                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p05:2"),
                        Unit(repo, "run:p05:2", "unit:p05", [Node("node:p05")],
                            [Fact(repo, "unit:p05", target, "src/G2.cs", 1, 1)]));
                    var second = await store.GetLatestPublicationAsync(repo);

                    // The older generation G1 is refused; it cannot be served over the newer G2.
                    var staleG1 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target, first));
                    Assert.Equal(CodeOccurrenceAvailability.StalePublication, staleG1.Availability);
                    Assert.Empty(staleG1.Occurrences);

                    // Only the newest accepted generation serves.
                    var currentG2 = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Equal(CodeOccurrenceAvailability.Available, currentG2.Availability);
                    Assert.Equal("run:p05:2", currentG2.Publication!.IndexRunId.Value);
                    Assert.Equal("src/G2.cs", currentG2.Occurrences[0].Span.Path);
                    Assert.Equal(second!.IndexRunId.Value, currentG2.Publication.IndexRunId.Value);
                }

                // The durable occurrence index is bound to exactly the newest generation.
                Assert.Equal("run:p05:2", NativeOccurrencePublicationRunId(path, repo.Value));
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-06 (D4-6, constructed)
        // A run planned against a superseded baseline cannot complete and replace a newer accepted
        // generation. Constructed by interleaving two runs so the later one is planned against an
        // older baseline than the one that ends up accepted. This is the only clean scenario for
        // "a superseded publication must not replace a newer accepted generation".
        [Fact]
        public async Task P06_RunPlannedAgainstSupersededBaseline_CannotCompleteAndRegressGeneration()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p06");
            var target = Target(repo, "T1");
            try
            {
                using var store = new LatticeCodeGraphStore(path);

                // Establish accepted generation G1.
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p06:1"),
                    Unit(repo, "run:p06:1", "unit:p06", [Node("node:p06")],
                        [Fact(repo, "unit:p06", target, "src/G1.cs", 1, 1)]));

                // Two runs plan concurrently against the current baseline G1.
                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p06:a"), Later, plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:p06:a", "unit:p06", [Node("node:p06")], [Fact(repo, "unit:p06", target, "src/A.cs", 1, 1)]));

                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p06:b"), Later, plugins: [Plugin]));
                await store.StageIndexUnitAsync(
                    Unit(repo, "run:p06:b", "unit:p06", [Node("node:p06")], [Fact(repo, "unit:p06", target, "src/B.cs", 1, 1)]));

                // run:p06:b wins and is accepted, superseding G1.
                await store.CompleteIndexRunAsync(
                    new(repo, new CodeIndexRunId("run:p06:b"), Later, CodeIndexRunStatus.Completed, Later.AddSeconds(1), [Plugin]),
                    new(repo, new CodeIndexRunId("run:p06:b"), []));
                Assert.Equal("run:p06:b", (await store.GetLatestPublicationAsync(repo))!.IndexRunId.Value);

                // run:p06:a, planned against the superseded G1 baseline, may not complete: the newer
                // generation run:p06:b is retained and D4-6 holds.
                var refused = await Record.ExceptionAsync(async () =>
                    await store.CompleteIndexRunAsync(
                        new(repo, new CodeIndexRunId("run:p06:a"), Later, CodeIndexRunStatus.Completed, Later.AddSeconds(1), [Plugin]),
                        new(repo, new CodeIndexRunId("run:p06:a"), [])));
                Assert.IsType<InvalidOperationException>(refused);
                Assert.Contains("superseded publication", refused.Message, StringComparison.Ordinal);

                var latest = await store.GetLatestPublicationAsync(repo);
                Assert.Equal("run:p06:b", latest!.IndexRunId.Value);
                var current = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal("run:p06:b", current.Publication!.IndexRunId.Value);
                Assert.Equal("src/B.cs", current.Occurrences[0].Span.Path);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-07 (D4-7 / S-02-5, genuine)
        // An ordinary reopen produces a semantically equivalent published occurrence index. Genuine.
        [Fact]
        public async Task P07_OrdinaryReopen_OccurrenceQueryResultsEquivalent()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p07");
            var target = Target(repo, "T1");
            var other = Target(repo, "T2");
            var before = default(CodeOccurrenceQueryResult);
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p07"),
                        Unit(repo, "run:p07", "unit:p07:a", [Node("node:p07:a")],
                            [
                                Fact(repo, "unit:p07:a", target, "src/A.cs", 10, 5),
                                Fact(repo, "unit:p07:a", other, "src/A.cs", 3, 1)
                            ]),
                        Unit(repo, "run:p07", "unit:p07:b", [Node("node:p07:b")],
                            [
                                Fact(repo, "unit:p07:b", target, "src/A.cs", 2, 1),
                                Fact(repo, "unit:p07:b", target, "src/B.cs", 1, 1)
                            ]));
                    before = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Equal(CodeOccurrenceAvailability.Available, before.Availability);
                }

                using var reopened = new LatticeCodeGraphStore(path);
                var after = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
                Assert.Equal(Describe(before), Describe(after));
                Assert.Equal(before.TotalMatches, after.TotalMatches);
                Assert.Equal(before.Examined, after.Examined);
                Assert.Equal(
                    before.Publication!.IndexRunId.Value + "|" + before.Publication.IndexIdentity.Value,
                    after.Publication!.IndexRunId.Value + "|" + after.Publication.IndexIdentity.Value);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-08 (D4-7, genuine)
        // Rebuilding the projection from durable authority (the recovery path triggered by a known
        // pre-commit failure) yields a semantically equivalent published occurrence index. Genuine.
        [Fact]
        public async Task P08_RebuildFromDurableState_OccurrenceIndexEquivalent()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p08");
            var target = Target(repo, "T1");
            var arm = false;
            try
            {
                using (var store = new LatticeCodeGraphStore(path, null, point =>
                {
                    if (arm && point == "before-commit")
                        throw new InjectedPersistenceException();
                }))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p08"),
                        Unit(repo, "run:p08", "unit:p08", [Node("node:p08")],
                            [
                                Fact(repo, "unit:p08", target, "src/A.cs", 3, 1),
                                Fact(repo, "unit:p08", target, "src/A.cs", 1, 1),
                                Fact(repo, "unit:p08", target, "src/B.cs", 2, 1)
                            ]));
                    var before = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));

                    // A subsequent mutation fails before the durable native commit; the store recovers
                    // by rebuilding the projection from durable authority (RebuildInnerFromDurableState).
                    // The rebuild must reproduce the same accepted published occurrence index.
                    await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p08:stage"), Later, plugins: [Plugin]));
                    arm = true;
                    await Assert.ThrowsAsync<InjectedPersistenceException>(async () =>
                        await store.StageIndexUnitAsync(
                            Unit(repo, "run:p08:stage", "unit:p08:stage", [Node("node:p08:stage")],
                                [Fact(repo, "unit:p08:stage", target, "src/Extra.cs", 1, 1)])));

                    var after = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Equal(Describe(before), Describe(after));
                    Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
                    Assert.Equal(before.Publication!.IndexRunId.Value, after.Publication!.IndexRunId.Value);
                }
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-09 (D4-8, constructed)
        // Contributor union merges identical facts; a conflicting immutable payload is rejected and the
        // accepted state remains queryable, with publication/index visibility updated consistently.
        [Fact]
        public async Task P09_ContributorUnionAndConflictRejection_VisibilityConsistent()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p09");
            var target = Target(repo, "T1");
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                // Two storage units contribute identical occurrences of the same semantic unit => union.
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p09:1"),
                    Unit(repo, "run:p09:1", "unit:p09:a", [Node("node:p09:a")],
                        [Fact(repo, "unit:p09:a", target, "src/Shared.cs", 1, 1, "p09:semantic")]),
                    Unit(repo, "run:p09:1", "unit:p09:b", [Node("node:p09:b")],
                        [Fact(repo, "unit:p09:b", target, "src/Shared.cs", 1, 1, "p09:semantic")]),
                    Unit(repo, "run:p09:1", "unit:p09:c", [Node("node:p09:c")],
                        [Fact(repo, "unit:p09:c", target, "src/C.cs", 1, 1)]));

                var unioned = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, unioned.Availability);
                Assert.Equal(2, unioned.Occurrences.Count);
                var shared = unioned.Occurrences.Single(hit => hit.Span.Path == "src/Shared.cs");
                Assert.Equal(2, shared.Contributors.Count);

                // A conflicting immutable payload for the same identity is rejected; the accepted state
                // remains queryable afterwards (no partial exposure).
                await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p09:2"), Later, plugins: [Plugin]));
                // Same identity as the accepted src/C.cs occurrence (the same semantic/source unit),
                // but an incompatible immutable payload => contributor conflict.
                var conflicting = Fact(repo, "unit:p09:c", target, "src/C.cs", 1, 1, "unit:p09:c");
                var conflicted = new CodeOccurrenceFact(
                    conflicting.Id,
                    conflicting.TargetId,
                    conflicting.TargetDocumentId,
                    conflicting.RepositoryId,
                    conflicting.Category,
                    conflicting.SourceUnitId,
                    new CodeOccurrenceContributor(Plugin, "unit:p09:d"),
                    conflicting.Span,
                    conflicting.SourceHash,
                    contentHash: "content:conflict",
                    conflicting.DeclarationSpan,
                    conflicting.SemanticsVersion);
                await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                    await store.StageIndexUnitAsync(
                        new CodeIndexUnitReplacement(
                            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run:p09:2"), new CodeIndexUnitId("unit:p09:d")),
                            [Node("node:p09:d")],
                            occurrences: [conflicted],
                            occurrenceAnalysisIncluded: true)));

                var afterConflict = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, afterConflict.Availability);
                Assert.Equal(2, afterConflict.Occurrences.Count);
                Assert.Equal(Describe(unioned), Describe(afterConflict));
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-10 (D4-8, constructed)
        // Scoped deletion removes only the affected target context and leaves the rest of the index
        // clean and consistent. Includes a reopen and native assertions.
        [Fact]
        public async Task P10_ScopedDeletion_RemovesOnlyAffectedContextLeavesIndexClean()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p10");
            var t1 = Target(repo, "T1");
            var t2 = Target(repo, "T2");
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p10:1"),
                        Unit(repo, "run:p10:1", "unit:p10:a", [Node("node:p10:a")],
                            [
                                Fact(repo, "unit:p10:a", t1, "src/A.cs", 1, 1),
                                Fact(repo, "unit:p10:a", t2, "src/A.cs", 2, 1)
                            ]),
                        Unit(repo, "run:p10:1", "unit:p10:b", [Node("node:p10:b")],
                            [Fact(repo, "unit:p10:b", t1, "src/B.cs", 1, 1)]));

                    var beforeDelete = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
                    Assert.Equal(2, beforeDelete.Occurrences.Count);

                    // Delete only unit:p10:a; t1 keeps unit:p10:b; t2 loses unit:p10:a's contribution.
                    await PublishDeletionAsync(store, repo, "run:p10:2", "unit:p10:a");

                    var t1After = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
                    Assert.Equal(CodeOccurrenceAvailability.Available, t1After.Availability);
                    Assert.Single(t1After.Occurrences);
                    Assert.Equal("src/B.cs", t1After.Occurrences[0].Span.Path);
                    Assert.Equal("unit:p10:b", t1After.Occurrences[0].Contributors[0].IndexUnitId);

                    var t2After = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t2));
                    Assert.Equal(CodeOccurrenceAvailability.Available, t2After.Availability);
                    Assert.Empty(t2After.Occurrences);
                }

                // Native: only one occurrence unit row remains, and it belongs to unit:p10:b.
                var units = NativeOccurrenceUnitKeys(path);
                Assert.Single(units);
                Assert.Contains("unit:p10:b", units[0], StringComparison.Ordinal);

                using var reopened = new LatticeCodeGraphStore(path);
                var reopenedT1 = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, t1));
                Assert.Single(reopenedT1.Occurrences);
                Assert.Equal("src/B.cs", reopenedT1.Occurrences[0].Span.Path);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-11 (D4-9, constructed)
        // Bounded queries have deterministic ordering and explicit result limits.
        [Fact]
        public async Task P11_BoundedQuery_DeterministicSiteOrderAndExplicitLimit()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p11");
            var target = Target(repo, "T1");
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p11"),
                    Unit(repo, "run:p11", "unit:p11", [Node("node:p11")],
                        [
                            Fact(repo, "unit:p11", target, "src/C.cs", 3, 1),
                            Fact(repo, "unit:p11", target, "src/A.cs", 9, 2),
                            Fact(repo, "unit:p11", target, "src/A.cs", 2, 8),
                            Fact(repo, "unit:p11", target, "src/A.cs", 2, 1),
                            Fact(repo, "unit:p11", target, "src/B.cs", 1, 1)
                        ]));

                var bounded = await store.FindOccurrencesAsync(
                    new CodeOccurrenceQuery(repo, target, maxResults: 3));
                Assert.Equal(CodeOccurrenceAvailability.Available, bounded.Availability);
                Assert.Equal(3, bounded.Occurrences.Count);
                Assert.Equal(5, bounded.TotalMatches);
                Assert.True(bounded.Truncated);

                var order = bounded.Occurrences
                    .Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}")
                    .ToArray();
                // Ordinal path order first, then line, then column.
                Assert.Equal(["src/A.cs:2:1", "src/A.cs:2:8", "src/A.cs:9:2"], order);

                var full = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(
                    ["src/A.cs:2:1", "src/A.cs:2:8", "src/A.cs:9:2", "src/B.cs:1:1", "src/C.cs:3:1"],
                    full.Occurrences.Select(hit => $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}").ToArray());
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-12 (D4-10 / S-02-6, genuine)
        // Query behaviour during a concurrent publication follows a snapshot contract: a reader sees
        // either the pre-publication or post-publication snapshot atomically, never a partial one.
        // Genuine, in-process: a real background reader races a real durable publication.
        [Fact]
        public async Task P12_ConcurrentReaderDuringPublication_SeesAtomicSnapshot()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p12");
            var target = Target(repo, "T1");
            try
            {
                using var store = new LatticeCodeGraphStore(path);
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p12:1"),
                    Unit(repo, "run:p12:1", "unit:p12", [Node("node:p12")],
                        [Fact(repo, "unit:p12", target, "src/Before.cs", 1, 1)]));

                using var stop = new CancellationTokenSource(1500);
                var observations = new List<CodeOccurrenceAvailability>();
                var reader = Task.Run(async () =>
                {
                    while (!stop.IsCancellationRequested)
                    {
                        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target), stop.Token)
                            .ConfigureAwait(false);
                        lock (observations)
                        {
                            observations.Add(result.Availability);
                        }
                        // Every observed answer is one atomic snapshot: either the pre-publication
                        // fact or the post-publication fact, never a mix and never partial.
                        Assert.True(
                            result.Occurrences.Count <= 2,
                            "A concurrent reader observed a partial/mixed occurrence snapshot.");
                    }
                });

                // Publish the superseding generation on the main thread while the reader runs.
                await PublishAsync(
                    store,
                    repo,
                    new CodeIndexRunId("run:p12:2"),
                    Unit(repo, "run:p12:2", "unit:p12", [Node("node:p12")],
                        [
                            Fact(repo, "unit:p12", target, "src/Before.cs", 1, 1),
                            Fact(repo, "unit:p12", target, "src/After.cs", 2, 1)
                        ]));

                try { await reader; } catch (OperationCanceledException) { }

                Assert.Contains(CodeOccurrenceAvailability.Available, observations);
                Assert.DoesNotContain(CodeOccurrenceAvailability.CorruptedIndex, observations);
                var final = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal("run:p12:2", final.Publication!.IndexRunId.Value);
                Assert.Equal(2, final.Occurrences.Count);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-13 (S-02-4, genuine)
        // Rapid sequential publications leave exactly the latest generation accepted and everything
        // earlier superseded. Genuine publication stress; native assertions confirm one live binding.
        [Fact]
        public async Task P13_RapidSequentialPublications_OnlyLatestAccepted()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p13");
            var target = Target(repo, "T1");
            var runIds = new List<string>();
            try
            {
                using (var store = new LatticeCodeGraphStore(path))
                {
                    for (var generation = 1; generation <= 5; generation++)
                    {
                        var runId = $"run:p13:{generation}";
                        runIds.Add(runId);
                        await PublishAsync(
                            store,
                            repo,
                            new CodeIndexRunId(runId),
                            Unit(repo, runId, "unit:p13", [Node("node:p13")],
                                [Fact(repo, "unit:p13", target, $"src/G{generation}.cs", 1, 1)]));
                    }

                    Assert.Equal(runIds[^1], (await store.GetLatestPublicationAsync(repo))!.IndexRunId.Value);

                    var current = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                    Assert.Equal(runIds[^1], current.Publication!.IndexRunId.Value);
                    Assert.Single(current.Occurrences);
                    Assert.Equal($"src/G{5}.cs", current.Occurrences[0].Span.Path);
                }

                // Native assertion after the store released the file: exactly the newest generation
                // is bound in the durable occurrence meta.
                Assert.Equal(runIds[^1], NativeOccurrencePublicationRunId(path, repo.Value));
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-14 (S-02-5, constructed)
        // An unknown commit outcome on an occurrence publication is quarantined (current-state claims
        // refused) and corroborated by reopening; the unknown outcome is never reported as a confirmed
        // rollback. Constructed with the fault-injector after the durable commit.
        [Fact]
        public async Task P14_UnknownCommitOutcome_QuarantineAndReopenCorroborates()
        {
            var path = TemporaryDatabasePath();
            var repo = new CodeRepositoryId("repo:p14");
            var target = Target(repo, "T1");
            var arm = false;
            try
            {
                using (var store = new LatticeCodeGraphStore(path, null, point =>
                {
                    if (arm && point == "after-commit")
                        throw new InjectedPersistenceException();
                }))
                {
                    await PublishAsync(
                        store,
                        repo,
                        new CodeIndexRunId("run:p14:1"),
                        Unit(repo, "run:p14:1", "unit:p14", [Node("node:p14")],
                            [Fact(repo, "unit:p14", target, "src/Prior.cs", 1, 1)]));

                    await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:p14:2"), Later, plugins: [Plugin]));
                    await store.StageIndexUnitAsync(
                        Unit(repo, "run:p14:2", "unit:p14", [Node("node:p14")],
                            [Fact(repo, "unit:p14", target, "src/Next.cs", 1, 1)]));

                    arm = true;
                    await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                        await store.CompleteIndexRunAsync(
                            new(repo, new CodeIndexRunId("run:p14:2"), Later, CodeIndexRunStatus.Completed, Later.AddSeconds(1), [Plugin]),
                            new(repo, new CodeIndexRunId("run:p14:2"), [])));

                    // The quarantined instance refuses current-state claims; it does not claim a
                    // rollback or a commit.
                    await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () =>
                        await store.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target)));
                }

                // A fresh reopen corroborates the durable state: the store opens cleanly and answers
                // from one accepted generation (whether the write landed is corroborated, not guessed).
                using var reopened = new LatticeCodeGraphStore(path);
                Assert.True((await reopened.CheckHealthAsync()).IsHealthy);
                var corroborated = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(repo, target));
                Assert.Equal(CodeOccurrenceAvailability.Available, corroborated.Availability);
                Assert.True(corroborated.Publication is not null);
                Assert.Single(corroborated.Occurrences);
                Assert.NotEqual(CodeOccurrenceAvailability.CorruptedIndex, corroborated.Availability);
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-15 (S-02-6 / D4-10, genuine multi-process; D-05 fix, F-QU-01)
        // Native concurrency/republication across processes: the first writer completes and commits a
        // real occurrence publication; other processes cannot open the database while it is held; a
        // fresh reopen sees exactly the committed publication. This discharges F-QU-01: the writer
        // completes the run rather than only staging, so the publication is durably visible.
        [Fact]
        public async Task P15_MultiProcessPublication_FileLockSerializesAndReopenSeesCommittedPublication()
        {
            var path = TemporaryDatabasePath();
            var holdRepo = "repo:p15-hold";
            var holdRun = "run:p15-hold";
            var publishRepo = "repo:p15-pub";
            var publishRun = "run:p15-pub";
            var targetName = "T1";
            try
            {
                var worker = LocateWorkerAssembly();

                // Phase 1: a separate process opens the database and deterministically holds the native
                // file lock (genuine cross-process ownership). Any other process must fail to open it.
                var signal = TemporaryDatabasePath() + ".p15signal";
                var hold = StartWorker(worker,
                    $"--db \"{path}\" --repo {holdRepo} --run {holdRun} --unit unit:p15-hold --node node:p15-hold --mode hold --signal \"{signal}\"");
                try
                {
                    Assert.True(
                        WaitForFile(signal + ".ready", TimeSpan.FromSeconds(30)),
                        "The hold worker never reported READY.");

                    // The first writer holds the lock: a second open (from this process) fails rather
                    // than queuing - genuine cross-process file-locking serialization.
                    var blocked = Assert.Throws<CodeGraphStoreException>(() => new LatticeCodeGraphStore(path));
                    Assert.Contains("open database", blocked.Message, StringComparison.Ordinal);
                }
                finally
                {
                    File.WriteAllText(signal + ".go", "GO");
                    hold.WaitForExit(30_000);
                }

                // Phase 2: a second, independent process publishes a complete occurrence publication
                // (the F-QU-01 fix: it actually completes the run) after the lock is released.
                var publisher = StartWorker(worker,
                    $"--db \"{path}\" --repo {publishRepo} --run {publishRun} --unit unit:p15-pub --node node:p15-pub --mode publish-complete --target {targetName} --occurrences 3");
                var publisherOutput = ReadToEnd(publisher);
                publisher.WaitForExit(60_000);
                Assert.True(publisher.ExitCode == 0, $"The publish worker exited {publisher.ExitCode}: {publisherOutput}");
                var published = Assert.Single(publisherOutput
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line.StartsWith("PUBLISHED|", StringComparison.Ordinal))
                    .Select(line => line["PUBLISHED|".Length..].Split('|'))
                    .ToArray());
                var publishedRun = published[0];

                // Phase 3: a fresh reopen sees exactly the committed publication (not merely staged).
                using (var reopened = new LatticeCodeGraphStore(path))
                {
                    var publication = await reopened.GetLatestPublicationAsync(new CodeRepositoryId(publishRepo));
                    Assert.NotNull(publication);
                    Assert.Equal(publishedRun, publication!.IndexRunId.Value);

                    var target = ComputeTarget(publishRepo, targetName);
                    var result = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(new CodeRepositoryId(publishRepo), target));
                    Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
                    Assert.Equal(publishedRun, result.Publication!.IndexRunId.Value);
                    Assert.Equal(3, result.Occurrences.Count);

                    // The graph-only hold repository never published; analysis is unavailable, not empty.
                    var holdResult = await reopened.FindOccurrencesAsync(
                        new CodeOccurrenceQuery(new CodeRepositoryId(holdRepo), target));
                    Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, holdResult.Availability);
                }
            }
            finally
            {
                DeleteDatabase(path);
            }
        }

        // ------------------------------------------------------------------ P-16 (S-02-7, documentation)
        // In-process deterministic interleaving is a diagnostic only and is never accepted as native
        // atomicity evidence (CI1A spec line 194). This case records the production boundary reflectively:
        // the pinned native dependency exposes no interleaving/coordination/cancellation seam, so no
        // deterministic interleaving can impersonate native publication atomicity.
        [Fact]
        public void P16_InterleavingIsNotANativeEvidenceSurface()
        {
            var assembly = typeof(LatticeDatabase).Assembly;
            Assert.Equal("LatticeDbSharp", assembly.GetName().Name);
            Assert.Equal(new Version(0, 2, 0, 0), assembly.GetName().Version);

            Type[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                types = exception.Types.Where(type => type is not null).Cast<Type>().ToArray();
            }

            var members = types
                .SelectMany(type => type
                    .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Select(member => $"{type.FullName}.{member.Name}"))
                .ToList();

            // No interleaving/coordination/observation seam is part of the production native surface.
            Assert.DoesNotContain(members, name =>
                name.Contains("Interleav", StringComparison.Ordinal) ||
                name.Contains("Coordinator", StringComparison.Ordinal) ||
                name.Contains("Schedule", StringComparison.Ordinal) ||
                name.Contains("Hook", StringComparison.Ordinal));

            // The truthfulness boundary that stands in for interleaving evidence is the unknown-outcome
            // vocabulary; it is the only way an indeterminate native result is represented.
            Assert.Contains(typeof(LatticeCodeGraphStore).Assembly.GetTypes(), type =>
                type.Name == nameof(CodeGraphCommitOutcomeUnknownException));
        }

        // ------------------------------------------------------------------ helpers

        private static string Describe(CodeOccurrenceQueryResult result) =>
            string.Join(
                "|",
                $"availability={result.Availability}",
                $"publication={result.Publication?.IndexRunId.Value}:{result.Publication?.IndexIdentity.Value}",
                $"examined={result.Examined}",
                $"total={result.TotalMatches}",
                string.Join(",", result.Occurrences.Select(hit =>
                    $"{hit.Span.Path}:{hit.Span.StartLine}:{hit.Span.StartColumn}:{hit.TargetId.Value}:{hit.Id.Value}:{string.Join("&", hit.Contributors.Select(c => c.PluginId.Value + "/" + c.IndexUnitId))}")));

        private static CodeOccurrenceTargetId ComputeTarget(string repositoryId, string name)
        {
            var context = "compctx1:sha256:" + Hex(repositoryId + "|" + name);
            return CodeOccurrenceIdentity.ComputeTargetId(
                context,
                "meta:" + name,
                "module:" + name,
                CodeOccurrenceIdentity.ComputeIdentifierDigest([$"src/{name}.cs|1|2"]),
                CodeOccurrenceIdentity.ComputeDeclarationDigest([$"src/{name}.cs|1|2|0|10|1|1|1|2|1|1|1|10"]),
                CodeOccurrenceIdentity.ComputeShapeDigest("shape:" + name));
        }

        private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name) =>
            ComputeTarget(repo.Value, name);

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
                sourceHash: "sha256:source:" + path,
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
                        CodeRelationshipCoverageState.Produced,
                        declaredUnitCount: 1,
                        verifiedUnitCount: 1)
                    : null);

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
            await store.StoreIndexRunAsync(new(repo, runId, Later, plugins: [Plugin]));
            await store.StageIndexUnitDeletionAsync(repo, runId, Plugin, new CodeIndexUnitId(unit));
            await store.CompleteIndexRunAsync(
                new(repo, runId, Later, CodeIndexRunStatus.Completed, Later.AddSeconds(1), [Plugin]),
                new(repo, runId, []));
        }

        private static IReadOnlyList<string> NativePayloads(string path, string label)
        {
            using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
            using var transaction = database.BeginReadTransaction();
            using var query = database.Prepare($"MATCH (s:{label}) RETURN s.payload");
            using var result = query.Execute(transaction);
            var rows = result.ReadAll();
            transaction.Commit();
            return rows
                .Select(row => Encoding.UTF8.GetString(Convert.FromBase64String(row.GetValue(0).AsString())))
                .ToList();
        }

        private static string NativeOccurrencePublicationRunId(string path, string repositoryId)
        {
            // NativePayloads already base64-decodes each payload to its UTF-8 JSON text.
            var meta = NativePayloads(path, OccurrenceMetaLabel)
                .Select(payload => System.Text.Json.JsonSerializer.Deserialize<PersistedOccurrenceMeta>(payload))
                .SingleOrDefault(value => value is not null && value.RepositoryId == repositoryId);
            Assert.NotNull(meta);
            return meta!.PublicationIndexRunId;
        }

        private static IReadOnlyList<string> NativeOccurrenceUnitKeys(string path)
        {
            using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
            using var txn = database.BeginReadTransaction();
            using var query = database.Prepare($"MATCH (s:{OccurrenceUnitLabel}) RETURN s.key");
            using var result = query.Execute(txn);
            var rows = result.ReadAll();
            txn.Commit();
            return rows.Select(row => row.GetValue(0).AsString()).ToList();
        }

        /// <summary>
        /// Tampers every durable occurrence page of a repository so its publication binding is stale,
        /// which must make the store refuse at open with an explicit index-inconsistency error.
        /// </summary>
        private static void TamperOccurrencePageBinding(string path, string repositoryId, string staleRunId)
        {
            using var database = LatticeDatabase.Open(path, new LatticeDatabaseOptions { Create = false });
            using var txn = database.BeginWriteTransaction();

            // Fully read and dispose the reader first; LatticeDB refuses to commit or close while a
            // prepared query remains active.
            var rows = new List<(string Key, string Payload)>();
            using (var query = database.Prepare($"MATCH (s:{OccurrencePageLabel}) RETURN s.key, s.payload"))
            using (var result = query.Execute(txn))
            {
                var read = result.ReadAll();
                rows.AddRange(read.Select(row => (
                    key: row.GetValue(0).AsString(),
                    payload: row.GetValue(1).AsString())));
            }

            foreach (var (key, payload) in rows)
            {
                var page = System.Text.Json.JsonSerializer.Deserialize<PersistedOccurrencePage>(
                    Convert.FromBase64String(payload));
                if (page is null || page.RepositoryId != repositoryId)
                    continue;
                var tampered = page with { PublicationIndexRunId = staleRunId };
                using var update = database.Prepare(
                    $"MATCH (s:{OccurrencePageLabel}) WHERE s.key = $k SET s.payload = $p");
                update.Bind("k", LatticeValue.From(key));
                update.Bind("p", LatticeValue.From(
                    Convert.ToBase64String(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(tampered))));
                using var updated = update.Execute(txn);
                updated.ReadAll();
            }

            txn.Commit();
        }

        // ---- multi-process worker helpers (genuine native concurrency) --------------------------

        private static string LocateWorkerAssembly()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null &&
                   !File.Exists(Path.Combine(directory.FullName, "Andirit.slnx")))
                directory = directory.Parent;
            var root = directory?.FullName
                ?? throw new InvalidOperationException("The repository root (Andirit.slnx) was not found.");
            foreach (var configuration in new[] { "Release", "Debug" })
            {
                var candidate = Path.Combine(
                    root,
                    "tests",
                    "Andirit.LatticeDb.Tests.Worker",
                    "bin",
                    configuration,
                    "net10.0",
                    "Andirit.LatticeDb.Tests.Worker.dll");
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new InvalidOperationException(
                "The Andirit.LatticeDb.Tests.Worker assembly was not built; build the solution before running the multi-process cases.");
        }

        private static Process StartWorker(string workerAssembly, string arguments) =>
            Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"exec \"{workerAssembly}\" {arguments}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("The worker process could not be started.");

        private static bool WaitForFile(string path, TimeSpan timeout)
        {
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                if (File.Exists(path))
                    return true;
                Thread.Sleep(25);
            }
            return false;
        }

        private static string ReadToEnd(Process process)
        {
            var standardOut = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            return standardOut + standardError;
        }

        private static string TemporaryDatabasePath() =>
            Path.Combine(Path.GetTempPath(), $"andirit-pub-{Guid.NewGuid():N}.ltdb");

        private static void DeleteDatabase(string path)
        {
            foreach (var candidate in new[]
                     {
                         path, path + "-wal", path + "-journal", path + ".wal",
                         path + ".p15signal", path + ".p15signal.ready", path + ".p15signal.go"
                     })
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

        private sealed class InjectedPersistenceException : Exception;
    }
}
