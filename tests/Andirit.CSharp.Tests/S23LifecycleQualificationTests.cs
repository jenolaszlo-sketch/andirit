using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using F = Andirit.CSharp.Tests.S23NativeAuthorityFixture;
using Frozen = AdrCi1Exp.Shared;

namespace Andirit.CSharp.Tests;

// ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1 native lifecycle qualification.
//
// The reviewer-owned real-fact lifecycle tests required by the entry owner
// matrix (L1-L20) that are not already covered by the accepted WP-S23-1/2/3A
// suites. Every case runs against the real Windows-x64 LatticeCodeGraphStore and
// the real S2.2 publication path (verified Roslyn compilations through the C#
// plugin/sink, or the accepted S23NativeAuthorityFixture). Expected values are
// recomputed from the accepted frozen R2 authority and identity primitives,
// never copied from the implementation under test. No production code is
// modified by this file.
public sealed class S23LifecycleQualificationTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-s23-life-" + Guid.NewGuid().ToString("N") + ".ltdb");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    private static readonly CodeRepositoryId PluginRepo = new("repo:s23-life-plugin");
    private static readonly DateTimeOffset Started = new(2026, 11, 7, 9, 0, 0, TimeSpan.Zero);

    // ---- L1: initial publication of real facts, native rows in one transaction ----

    [Fact]
    public async Task L01_InitialPublication_PersistsRealFactUnitPageMetaInOneTransaction()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);

        var rows = F.Rows(_path, "AndiritOccurrenceUnit", "AndiritOccurrencePage", "AndiritOccurrenceMeta");
        var units = rows.Where(p => p.Key.StartsWith("AndiritOccurrenceUnit|", StringComparison.Ordinal)).ToArray();
        var pages = rows.Where(p => p.Key.StartsWith("AndiritOccurrencePage|", StringComparison.Ordinal)).ToArray();
        var meta = Assert.Single(rows, p => p.Key.StartsWith("AndiritOccurrenceMeta|", StringComparison.Ordinal));
        Assert.Equal(3, units.Length); // App, App2, Empty
        Assert.Equal(3, meta.Value["FactCount"]!.GetValue<int>()); // App:2 + App2:1
        Assert.Equal(3, meta.Value["UnitCount"]!.GetValue<int>());
        Assert.Equal("run:a", meta.Value["PublicationIndexRunId"]!.GetValue<string>());
        Assert.True(meta.Value["RequiresAuthority"]!.GetValue<bool>());
        foreach (var unit in units)
        {
            Assert.True(unit.Value["RequiresAuthority"]!.GetValue<bool>());
            Assert.NotNull(unit.Value["Authority"]);
        }
        Assert.NotEmpty(pages);

        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Equal(2, result.Occurrences.Count); // App and App2 both target Lib
        Assert.True(result.IsComplete);
    }

    // ---- L2/L11: same-unit replacement after a real source edit ----

    [Fact]
    public async Task L02_SameUnitReplacement_AfterRealSourceEdit_RetiresObsoleteAndShowsNew()
    {
        var v1Files = SingleApp("public static int B() => 1; public static int C() => B();");
        var v2Files = SingleApp("public static int B() => 1; public static int D() => 2; public static int C() => D();");
        var v1Context = Context("run:1", v1Files);
        var v2Context = Context("run:2", v2Files);
        var (a1, app1) = await PrepareSingle(v1Context);
        var (a2, app2) = await PrepareSingle(v2Context);
        var bV1 = ExpectedTarget(app1, Method(app1, "App.A", "B"));
        var bV2 = ExpectedTarget(app2, Method(app2, "App.A", "B"));
        var dV2 = ExpectedTarget(app2, Method(app2, "App.A", "D"));

        using (var store = new LatticeCodeGraphStore(_path))
        {
            await PublishRealAsync(store, "run:1", v1Context);
            var before = await store.FindOccurrencesAsync(Query(a1, bV1));
            Assert.Equal(CodeOccurrenceAvailability.Available, before.Availability);
            Assert.Single(before.Occurrences);

            await PublishRealAsync(store, "run:2", v2Context);
            var retired = await store.FindOccurrencesAsync(Query(a2, bV2));
            Assert.Equal(CodeOccurrenceAvailability.Available, retired.Availability);
            Assert.Empty(retired.Occurrences);
            Assert.True(retired.IsComplete);
            var added = await store.FindOccurrencesAsync(Query(a2, dV2));
            Assert.Equal(CodeOccurrenceAvailability.Available, added.Availability);
            Assert.Single(added.Occurrences);
            Assert.Equal(app2.Descriptor.ProjectUnitId, added.Occurrences[0].SourceUnitId);
        }

        using var reopened = new LatticeCodeGraphStore(_path);
        var retiredAfter = await reopened.FindOccurrencesAsync(Query(a2, bV2));
        Assert.Empty(retiredAfter.Occurrences);
        Assert.True(retiredAfter.IsComplete);
        Assert.Single((await reopened.FindOccurrencesAsync(Query(a2, dV2))).Occurrences);
    }

    // ---- L3: scoped deletion of a real unit removes its facts only ----

    [Fact]
    public async Task L03_ScopedUnitDeletion_RemovesRealFactsAndPreservesRemaining()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
            var before = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(2, before.Occurrences.Count);

            await F.Start(store, "run:delete");
            await store.StageIndexUnitDeletionAsync(
                F.Repo, new("run:delete"), F.Plugin, new(a.App.Descriptor.ProjectUnitId));
            await F.Complete(store, "run:delete");
        }

        var units = F.Rows(_path, "AndiritOccurrenceUnit");
        Assert.DoesNotContain(units.Values, unit =>
            unit["IndexUnitId"]?.GetValue<string>() == a.App.Descriptor.ProjectUnitId);
        using var reopened = new LatticeCodeGraphStore(_path);
        var after = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
        Assert.Single(after.Occurrences);
        Assert.Equal(a.App2.Descriptor.ProjectUnitId, after.Occurrences[0].SourceUnitId);
        Assert.True(after.IsComplete);
    }

    // ---- L4: identical real-fact identity unions contributors; distinct semantic units stay distinct ----

    [Fact]
    public async Task L04_IdenticalRealFactIdentity_TwoStorageUnits_UnionToOneFactTwoContributors()
    {
        // A real extracted identity (real target/span/sourceHash/semantic unit) is
        // contributed by two distinct storage units. Identity excludes the storage
        // contributor, so the accepted merge must union them to one fact.
        var b = await F.Build();
        var fact = b.Extract(b.App).Facts.Single(f => f.TargetId == b.LibTarget);
        using var store = new LatticeCodeGraphStore(_path);
        await F.Start(store, "run:union");
        await store.StageIndexUnitAsync(ContributedUnit("run:union", "unit:x", Recontributor(fact, "unit:x")));
        await store.StageIndexUnitAsync(ContributedUnit("run:union", "unit:y", Recontributor(fact, "unit:y")));
        await F.Complete(store, "run:union");

        var result = await store.FindOccurrencesAsync(new CodeOccurrenceQuery(F.Repo, b.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        var hit = Assert.Single(result.Occurrences);
        Assert.Equal(2, hit.Contributors.Count);
        Assert.Equal(new[] { "unit:x", "unit:y" },
            hit.Contributors.Select(c => c.IndexUnitId).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task L04b_DistinctSemanticUnits_RemainDistinctFactsForOneTarget()
    {
        // Real cross-project facts: App and App2 both invoke the same Lib method
        // but are distinct semantic (source) units, so they stay distinct facts.
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(2, result.Occurrences.Count);
        Assert.Equal(new[] { a.App.Descriptor.ProjectUnitId, a.App2.Descriptor.ProjectUnitId },
            result.Occurrences.Select(o => o.SourceUnitId).Order(StringComparer.Ordinal).ToArray());
        Assert.All(result.Occurrences, o => Assert.Single(o.Contributors));
    }

    // ---- L5: contributor conflict (same identity, different payload) refused; prior intact ----

    [Fact]
    public async Task L05_ContributorConflict_RealIdentity_IsRefusedAtStagingWithPriorIntact()
    {
        var b = await F.Build();
        var fact = b.Extract(b.App).Facts.Single(f => f.TargetId == b.LibTarget);
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:conflict");
            await store.StageIndexUnitAsync(ContributedUnit("run:conflict", "unit:x", Recontributor(fact, "unit:x")));
            await F.Complete(store, "run:conflict");
        }
        var before = F.Snapshot(_path);

        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:conflict2");
            var conflicting = CodeOccurrenceFact.Create(
                F.Repo, fact.TargetId, fact.TargetDocumentId, fact.SourceUnitId,
                new CodeOccurrenceContributor(F.Plugin, "unit:z"), fact.Category, fact.Span,
                fact.SourceHash, "content:conflicting");
            Assert.Equal(fact.Id, conflicting.Id); // same identity, different immutable payload
            await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await store.StageIndexUnitAsync(ContributedUnit("run:conflict2", "unit:z", conflicting)));
        }

        Assert.Equal(before, F.Snapshot(_path));
        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(new CodeOccurrenceQuery(F.Repo, b.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Single(result.Occurrences);
    }

    // ---- L6: valid complete-empty real analysis persists and reopens ----

    [Fact]
    public async Task L06_CompleteEmpty_RealFacts_PersistsAndReopensAsCompleteZero()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:empty", a.Empty);
            var current = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
            Assert.Empty(current.Occurrences);
            Assert.True(current.IsComplete);
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        var after = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
        Assert.Empty(after.Occurrences);
        Assert.True(after.IsComplete);
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
            (await reopened.FindOccurrencesAsync(new(F.Repo, a.LibTarget))).Availability);
    }

    // ---- L7: native reopen reconstructs the real-fact projection byte-equivalently ----

    [Fact]
    public async Task L07_NativeReopen_RealFacts_OrderedResultsAreIdentical()
    {
        var a = await F.Build();
        CodeOccurrenceQueryResult before;
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
            before = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        var after = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget, before.Publication));
        Assert.Equal(before.Availability, after.Availability);
        Assert.Equal(Key(before), Key(after));
        Assert.Equal(before.TotalMatches, after.TotalMatches);
        Assert.Equal(before.Examined, after.Examined);
    }

    // ---- L8/L17: stale publication refusal with real facts after a transitive-only change ----

    [Fact]
    public async Task L08_StalePublication_RealFact_AfterTransitiveOnlyChange_RefusesAcrossReopen()
    {
        var a = await F.Build();
        var b = await F.Build("source"); // Leaf body changes only; App/Lib contexts differ transitively
        Assert.Equal(a.LibTarget, b.LibTarget); // stable target identity
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);

        using var reopened = new LatticeCodeGraphStore(_path);
        var current = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
        var stale = await reopened.FindOccurrencesAsync(b.Query(b.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
        Assert.Empty(stale.Occurrences);
        Assert.Equal(0, stale.Examined);
        Assert.False(stale.IsComplete);
        Assert.Equal("run:a", stale.Publication!.IndexRunId.Value);
    }

    // ---- L9: before-commit failure with real facts preserves the prior publication ----

    [Fact]
    public async Task L09_BeforeCommitFailure_RealFacts_PreservesPriorPublication()
    {
        var a = await F.Build();
        using (var setup = new LatticeCodeGraphStore(_path))
            await a.Publish(setup, "run:a", a.App, a.Empty);
        // The failed completion preserves occurrence authority and the published
        // graph; the run/stage rows legitimately reflect their own successful
        // staging operations (accepted WP-S23-2/3A semantics).
        var baseline = OccurrenceRows();

        var arm = false;
        using (var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "before-commit") throw new LifeFaultException(); }))
        {
            await F.Start(store, "run:b");
            await store.StageIndexUnitAsync(a.Replacement("run:b", a.App2));
            arm = true;
            await Assert.ThrowsAsync<LifeFaultException>(async () => await F.Complete(store, "run:b"));
            arm = false;

            var recovered = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
            Assert.Single(recovered.Occurrences);
            Assert.Equal("run:a", recovered.Publication!.IndexRunId.Value);
        }
        Assert.Equal(baseline, OccurrenceRows());
        using var reopened = new LatticeCodeGraphStore(_path);
        var after = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
        Assert.Single(after.Occurrences);
    }

    // ---- L10: known-failure recovery projection corresponds to the durable publication ----

    [Fact]
    public async Task L10_RecoveryProjection_RealFacts_MatchesDurableReopen()
    {
        var a = await F.Build();
        var arm = false;
        CodeOccurrenceQueryResult recovered;
        using (var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "before-commit") throw new LifeFaultException(); }))
        {
            await a.Publish(store, "run:a", a.App, a.Empty);
            await F.Start(store, "run:b");
            await store.StageIndexUnitAsync(a.Replacement("run:b", a.App2));
            arm = true;
            await Assert.ThrowsAsync<LifeFaultException>(async () => await F.Complete(store, "run:b"));
            arm = false;
            recovered = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        var durable = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(durable.Availability, recovered.Availability);
        Assert.Equal(Key(durable), Key(recovered));
        Assert.Equal(durable.Publication, recovered.Publication);
        Assert.Equal(durable.Coverage!.State, recovered.Coverage!.State);
    }

    // ---- L12 / SQ-03: old-or-new complete visibility under the serialized write gate ----

    [Fact]
    public async Task L12_Sq03_DeterministicSchedule_NeverExposesAPartialBlend()
    {
        var a = await F.Build();
        var trace = new List<string>();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:1", a.App, a.Empty);
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));

            await F.Start(store, "run:2");
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.App));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.App2));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await store.StageIndexUnitAsync(a.Replacement("run:2", a.Empty));
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
            await F.Complete(store, "run:2");
            trace.Add(Observe(await store.FindOccurrencesAsync(a.Query(a.LibTarget))));
        }

        // Steps: publish r1; run-started; stage App; stage App2; stage Empty; complete r2.
        Assert.Equal(
            new[] { "run:1=1/Available", "run:1=1/Available", "run:1=1/Available",
                    "run:1=1/Available", "run:1=1/Available", "run:2=2/Available" },
            trace.ToArray());
    }

    [Fact]
    public async Task L12b_Sq03_ConcurrentReaders_SeeOnlyCompleteOldOrNewPublications()
    {
        var a = await F.Build();
        using var store = new LatticeCodeGraphStore(_path);
        await a.Publish(store, "run:1", a.App, a.Empty);

        var writers = Task.Run(async () =>
        {
            for (var round = 0; round < 20; round++)
            {
                var expand = "run:n:" + round;
                await F.Start(store, expand);
                await store.StageIndexUnitAsync(a.Replacement(expand, a.App));
                await store.StageIndexUnitAsync(a.Replacement(expand, a.App2));
                await store.StageIndexUnitAsync(a.Replacement(expand, a.Empty));
                await F.Complete(store, expand);

                var shrink = "run:o:" + round;
                await F.Start(store, shrink);
                await store.StageIndexUnitAsync(a.Replacement(shrink, a.App));
                await store.StageIndexUnitAsync(a.Replacement(shrink, a.Empty));
                await store.StageIndexUnitDeletionAsync(F.Repo, new(shrink), F.Plugin, new(a.App2.Descriptor.ProjectUnitId));
                await F.Complete(store, shrink);
            }
        });

        var violations = new List<string>();
        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < 200; i++)
            {
                var result = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
                if (result.Availability != CodeOccurrenceAvailability.Available) continue;
                var run = result.Publication!.IndexRunId.Value;
                var count = result.Occurrences.Count;
                // Only two complete publications exist: App-only (1, "run:o:") and
                // App+App2 (2, "run:n:"/"run:1"). A blend would be rejected here.
                var consistent = count switch
                {
                    1 => run == "run:1" || run.StartsWith("run:o:", StringComparison.Ordinal),
                    2 => run.StartsWith("run:n:", StringComparison.Ordinal),
                    _ => false,
                };
                if (!consistent) lock (violations) violations.Add(Observe(result));
            }
        })).ToArray();

        await Task.WhenAll(readers.Append(writers));
        Assert.Empty(violations);
    }

    // ---- L13: coverage classification of real facts (Produced / Partial / unavailable) ----

    [Fact]
    public async Task L13_CoverageClassification_RealFacts_DistinguishesProducedPartialUnavailable()
    {
        var a = await F.Build();
        var extracted = a.Extract(a.App);
        var partialCoverage = extracted.Coverage.WithState(CodeRelationshipCoverageState.Partial);
        var partial = extracted with { Coverage = partialCoverage, Authority = extracted.Authority! with { Coverage = partialCoverage } };

        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:partial");
            await store.StageIndexUnitAsync(partial.PrepareNativeUnit(
                F.Origin("run:partial", a.App.Descriptor.ProjectUnitId)));
            await F.Complete(store, "run:partial");
            var result = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
            Assert.False(result.IsComplete);
            Assert.Single(result.Occurrences);
        }

        // A graph-only publication with no occurrence analysis stays unavailable.
        var graphOnlyPath = _path + "-graphonly.ltdb";
        try
        {
            var a2 = await F.Build();
            using (var store = new LatticeCodeGraphStore(graphOnlyPath))
            {
                await F.Start(store, "run:graph");
                await store.StageIndexUnitAsync(new(F.Origin("run:graph", a2.Empty.Descriptor.ProjectUnitId)));
                await F.Complete(store, "run:graph");
            }
            using var reopened = new LatticeCodeGraphStore(graphOnlyPath);
            var unavailable = await reopened.FindOccurrencesAsync(a2.Query(a2.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, unavailable.Availability);
            Assert.Null(unavailable.Coverage);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
                if (File.Exists(graphOnlyPath + suffix)) File.Delete(graphOnlyPath + suffix);
        }
    }

    // ---- L14: deleting the last analyzed unit invalidates coverage ----

    [Fact]
    public async Task L14_DeletionInvalidatesCoverage_RealFacts_AnalysisUnavailable()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:a", a.Empty);
            Assert.True((await store.FindOccurrencesAsync(a.Query(a.LibTarget))).IsComplete);
            await F.Start(store, "run:delete");
            await store.StageIndexUnitDeletionAsync(
                F.Repo, new("run:delete"), F.Plugin, new(a.Empty.Descriptor.ProjectUnitId));
            await F.Complete(store, "run:delete");
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
            (await reopened.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
    }

    // ---- L16: D3 graph noninterference across the real-fact occurrence lifecycle ----

    [Fact]
    public async Task L16_D3GraphNoninterference_RealFacts_AcrossOccurrenceLifecycle()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:g0");
            await store.StageIndexUnitAsync(new(F.Origin("run:g0", "Graph/Owner.csproj"),
                [new CodeGraphNode(new("node:owner"), CodeNodeKinds.Callable, "owner", "owner",
                    new CodeSymbolId("symbol:owner"))]));
            await F.Complete(store, "run:g0");
            var baseline = await GraphAsync(store);

            await a.Publish(store, "run:occ", a.App, a.App2, a.Empty);
            Assert.Equal(baseline, await GraphAsync(store));

            await F.Start(store, "run:occ2");
            await store.StageIndexUnitAsync(a.Replacement("run:occ2", a.App2));
            await store.StageIndexUnitAsync(a.Replacement("run:occ2", a.Empty));
            await F.Complete(store, "run:occ2");
            Assert.Equal(baseline, await GraphAsync(store));

            await F.Start(store, "run:del");
            await store.StageIndexUnitDeletionAsync(F.Repo, new("run:del"), F.Plugin, new(a.App.Descriptor.ProjectUnitId));
            await store.StageIndexUnitDeletionAsync(F.Repo, new("run:del"), F.Plugin, new(a.App2.Descriptor.ProjectUnitId));
            await F.Complete(store, "run:del");
            Assert.Equal(baseline, await GraphAsync(store));
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        Assert.Contains("node:owner", await GraphAsync(reopened));
    }

    // ---- L18: real cross-project target identity persisted and reopened ----

    [Fact]
    public async Task L18_CrossProjectTargetIdentity_IsPersistedAndReopened()
    {
        var context = Context("run:x", CrossProject());
        var (analysis, app, lib) = await LiveAsync(context);
        var target = ExpectedTarget(lib, Method(lib, "Lib.Value", "Get"));
        using (var store = new LatticeCodeGraphStore(_path))
            await PublishRealAsync(store, "run:x", context);

        var unit = F.Rows(_path, "AndiritOccurrenceUnit").Values.Single(u =>
            u["Authority"] is { } auth &&
            auth["SourceUnitId"]!.GetValue<string>() == app.Descriptor.ProjectUnitId);
        Assert.True(unit["RequiresAuthority"]!.GetValue<bool>());
        Assert.Equal(lib.Descriptor.ProjectUnitId,
            unit["Authority"]!["Facts"]![0]!["DefiningProjectUnitId"]!.GetValue<string>());

        using var reopened = new LatticeCodeGraphStore(_path);
        var query = VerifiedCSharpOccurrenceAuthority.Bind(new(PluginRepo, target), analysis.Universe, app.Compilation);
        var result = await reopened.FindOccurrencesAsync(query);
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Single(result.Occurrences);
        Assert.Equal(target, result.Occurrences[0].TargetId);
        Assert.Equal(app.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
        Assert.True(result.IsComplete);
    }

    // ---- L20: unknown-outcome (after-commit ack loss) quarantines; reopen reconciles ----

    [Fact]
    public async Task L20_UnknownOutcome_RealFacts_QuarantinesAndReopenReconciles()
    {
        var a = await F.Build();
        var arm = false;
        using (var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "after-commit") throw new LifeFaultException(); }))
        {
            await a.Publish(store, "run:a", a.App, a.Empty);
            await F.Start(store, "run:b");
            await store.StageIndexUnitAsync(a.Replacement("run:b", a.App2));
            arm = true;
            await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(async () => await F.Complete(store, "run:b"));
            await Assert.ThrowsAsync<CodeGraphCommitOutcomeUnknownException>(
                async () => await store.FindOccurrencesAsync(a.Query(a.LibTarget)));
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        var after = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, after.Availability);
        Assert.Equal(2, after.Occurrences.Count); // App + App2 both target Lib
        Assert.True(after.IsComplete);
    }

    // ---- helpers -----------------------------------------------------------

    private static CodeOccurrenceFact Recontributor(CodeOccurrenceFact fact, string unit) =>
        CodeOccurrenceFact.Create(
            F.Repo, fact.TargetId, fact.TargetDocumentId, fact.SourceUnitId,
            new CodeOccurrenceContributor(F.Plugin, unit), fact.Category, fact.Span,
            fact.SourceHash, fact.ContentHash);

    private static CodeIndexUnitReplacement ContributedUnit(string run, string unit, CodeOccurrenceFact fact) =>
        new(
            new CodeFactOrigin(F.Repo, F.Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit)),
            [new CodeGraphNode(new("node:" + unit), CodeNodeKinds.Callable, unit, unit, new CodeSymbolId("symbol:" + unit))],
            occurrences: [fact],
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced));

    private string OccurrenceRows() => JsonSerializer.Serialize(
        F.Rows(_path, "AndiritOccurrenceUnit", "AndiritOccurrencePage", "AndiritOccurrenceMeta"));

    private static string Observe(CodeOccurrenceQueryResult result) =>
        $"{result.Publication?.IndexRunId.Value}={result.Occurrences.Count}/{result.Availability}";

    private static string[] Key(CodeOccurrenceQueryResult result) =>
        result.Occurrences
            .Select(o => $"{o.Id.Value}|{o.SourceUnitId}|{string.Join("+", o.Contributors.Select(c => c.IndexUnitId))}")
            .ToArray();

    private static async Task<string> GraphAsync(LatticeCodeGraphStore store)
    {
        var units = await store.GetPublishedUnitsAsync(F.Repo);
        var nodes = units.SelectMany(u => u.Nodes).Select(n => n.Id.Value).Order(StringComparer.Ordinal);
        var edges = units.SelectMany(u => u.Edges).Select(e => e.Id.Value).Order(StringComparer.Ordinal);
        var traversal = await store.TraverseAsync(F.Repo, new(new("node:owner"), CodeGraphDirection.Incoming, maxDepth: 2));
        return $"nodes=[{string.Join(",", nodes)}]|edges=[{string.Join(",", edges)}]|tr={traversal.Nodes.Count}";
    }

    // ---- local real single-project fixture (plugin-path publication) ----

    private static IReadOnlyDictionary<string, string> SingleApp(string body) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["App/App.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["App/A.cs"] = "namespace App; public static class A { " + body + " }",
        };

    private static CodeOccurrenceQuery Query(VerifiedCSharpAnalysis analysis, CodeOccurrenceTargetId target) =>
        VerifiedCSharpOccurrenceAuthority.Bind(new(PluginRepo, target), analysis.Universe,
            analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!.Compilation);

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App)> PrepareSingle(
        CodeGraphPluginContext context)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis, analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!);
    }

    private static IReadOnlyDictionary<string, string> CrossProject(string libBody = "public static int Get() => 1;") =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Lib/Lib.csproj"] = "<Project Sdk=\"Microsoft.NET.Sdk\" />",
            ["Lib/Value.cs"] = "namespace Lib; public static class Value { " + libBody + " }",
            ["App/App.csproj"] =
                "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"../Lib/Lib.csproj\" /></ItemGroup></Project>",
            ["App/Application.cs"] =
                "namespace App; public static class Application { public static int Run() => Lib.Value.Get(); }",
        };

    private static CodeGraphPluginContext Context(string run, IReadOnlyDictionary<string, string> files) =>
        new(PluginRepo, null, new CodeIndexRunId(run),
            files.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair =>
            {
                var bytes = Encoding.UTF8.GetBytes(pair.Value);
                return new CodeGraphSource(pair.Key,
                    "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
            }).ToArray());

    private static async Task PublishRealAsync(LatticeCodeGraphStore store, string run, CodeGraphPluginContext context)
    {
        var plugin = new CSharpCodeGraphPlugin();
        await store.UpsertRepositoryAsync(new(PluginRepo));
        await store.StoreIndexRunAsync(new(PluginRepo, new(run), Started, plugins: [plugin.Id]));
        await using (var sink = new CodeGraphIngestionSink(store))
        await using (var session = await plugin.CreateSessionAsync(context))
        {
            await session.ExtractAsync(sink);
            await sink.CompleteAsync();
        }
        await store.CompleteIndexRunAsync(
            new(PluginRepo, new(run), Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), [plugin.Id]),
            new(PluginRepo, new(run), []));
    }

    private static async Task<(VerifiedCSharpAnalysis Analysis, VerifiedCSharpCompilation App, VerifiedCSharpCompilation Lib)>
        LiveAsync(CodeGraphPluginContext context)
    {
        var analysis = await CSharpCodeGraphPlugin.PrepareAnalysisAsync(context);
        return (analysis,
            analysis.Projects.Single(p => p.Project.Path == "App/App.csproj").Verified!,
            analysis.Projects.Single(p => p.Project.Path == "Lib/Lib.csproj").Verified!);
    }

    private static IMethodSymbol Method(VerifiedCSharpCompilation unit, string type, string name) =>
        (IMethodSymbol)unit.Compilation.GetTypeByMetadataName(type)!.GetMembers(name).Single();

    private static CodeOccurrenceTargetId ExpectedTarget(VerifiedCSharpCompilation defining, IMethodSymbol method)
    {
        var mapped = defining.MapR2FaithfulCompctx1();
        var identifier = CodeOccurrenceIdentity.ComputeIdentifierDigest([method.Name]);
        var declaration = CodeOccurrenceIdentity.ComputeDeclarationDigest([method.GetDocumentationCommentId() ?? method.Name]);
        var shape = CodeOccurrenceIdentity.ComputeShapeDigest("method|" + method.Arity + "|" +
            string.Join(",", method.Parameters.Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
        var viaIdentity = CodeOccurrenceIdentity.ComputeTargetId(
            mapped.DescriptorDigest, mapped.MetadataIdentity, mapped.ModuleName, identifier, declaration, shape);
        var frozen = Frozen.CompilationProvenance.Describe(PluginRepo.Value, defining.Descriptor.ProjectUnitId,
            defining.Compilation, defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray());
        var viaFrozen = "asmctx3:sha256:" + Frozen.ProvenanceHashing.Hash("asmctx3-context",
            ["compctx1", frozen.DescriptorDigest, frozen.MetadataIdentity, frozen.ModuleName, identifier, declaration, shape]);
        Assert.Equal(viaIdentity.Value, viaFrozen);
        return viaIdentity;
    }

    private sealed class LifeFaultException : Exception;
}
