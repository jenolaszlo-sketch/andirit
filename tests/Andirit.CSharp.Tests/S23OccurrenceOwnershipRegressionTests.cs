using Andirit;
using F = Andirit.CSharp.Tests.S23NativeAuthorityFixture;

namespace Andirit.CSharp.Tests;

// WP-S23-2-R1 regression coverage for F-01: an authority-bearing occurrence unit
// must correspond to the actual accepted published graph owner set in both
// directions, and that correspondence must be reconstructed at a durable reopen.
// These tests use the real LatticeDB file boundary; the orphan case removes the
// durable HetuUnit graph owner while leaving every occurrence row intact, which
// is the exact independent Q1 counterexample.
public sealed class S23OccurrenceOwnershipRegressionTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(), "andirit-s23-r1-" + Guid.NewGuid().ToString("N") + ".ltdb");

    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    [Fact]
    public async Task GraphOwnerRemoved_OrphanedOccurrence_RefusesAsCorruptedIndex_AcrossReopen()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.Empty);

        // Positive control: matching verified current authority is Available.
        using (var control = new LatticeCodeGraphStore(_path))
        {
            var available = await control.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, available.Availability);
            Assert.Single(available.Occurrences);
            Assert.True(available.IsComplete);
        }

        // Remove only the published graph owner; every occurrence row is intact.
        F.RemoveGraphUnit(_path, a.App.Descriptor.ProjectUnitId);
        var tampered = F.Snapshot(_path);

        // The retirement must survive a fresh durable reopen and refuse the
        // orphaned occurrence instead of serving it as a complete current result.
        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var published = await reopened.GetPublishedUnitsAsync(F.Repo);
            Assert.DoesNotContain(published, unit =>
                unit.Origin.IndexUnitId.Value == a.App.Descriptor.ProjectUnitId);
            var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.CorruptedIndex, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Equal(0, result.Examined);
            Assert.False(result.IsComplete);
            Assert.NotNull(result.RefusalReason);
        }

        Assert.Equal(tampered, F.Snapshot(_path));
    }

    [Fact]
    public async Task PublishedAuthorityUnitWithoutOccurrenceAssociation_Refuses()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.Empty);

        // Remove the occurrence association but keep the authority-requiring graph
        // unit. The manifest is kept internally consistent so the dedicated
        // missing-mandatory-association path is exercised rather than the
        // accounting check.
        F.RemoveOccurrenceUnit(_path, a.App.Descriptor.ProjectUnitId);
        F.MutateMetaRow(_path, meta => { meta["UnitCount"] = 1; meta["FactCount"] = 0; });
        var tampered = F.Snapshot(_path);

        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var published = await reopened.GetPublishedUnitsAsync(F.Repo);
            Assert.Contains(published, unit =>
                unit.Origin.IndexUnitId.Value == a.App.Descriptor.ProjectUnitId &&
                unit.RequiresOccurrenceAuthority);
            var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.CorruptedIndex, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Equal(0, result.Examined);
            Assert.False(result.IsComplete);
            Assert.NotNull(result.RefusalReason);
        }

        Assert.Equal(tampered, F.Snapshot(_path));
    }

    [Fact]
    public async Task StagedDeletionOfOneUnit_LeavesUnaffectedUnitsQueryable_AcrossReopen()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
            var before = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, before.Availability);
            Assert.Equal(2, before.Occurrences.Count);

            // Accepted retirement: the graph owner and its occurrence
            // contribution retire together and no other unit is affected.
            await F.Start(store, "run:delete");
            await store.StageIndexUnitDeletionAsync(
                F.Repo, new("run:delete"), F.Plugin, new(a.App.Descriptor.ProjectUnitId));
            await F.Complete(store, "run:delete");
        }

        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var published = await reopened.GetPublishedUnitsAsync(F.Repo);
            Assert.DoesNotContain(published, unit =>
                unit.Origin.IndexUnitId.Value == a.App.Descriptor.ProjectUnitId);
            Assert.Contains(published, unit =>
                unit.Origin.IndexUnitId.Value == a.App2.Descriptor.ProjectUnitId);
            var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.Equal(a.App2.Descriptor.ProjectUnitId, result.Occurrences[0].SourceUnitId);
            Assert.True(result.IsComplete);
        }
    }

    [Fact]
    public async Task RetainedOwnerBesideGraphOnlyUnit_IsNotMistakenForOrphan()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.Empty);

        // A later run publishes only a graph-only unit (no occurrence
        // association and no authority marker). Retained authority-bearing units
        // must remain queryable and must not be flagged as orphans.
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:graph");
            await store.StageIndexUnitAsync(new(F.Origin("run:graph", "Extra/Extra.csproj")));
            await F.Complete(store, "run:graph");
        }

        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var published = await reopened.GetPublishedUnitsAsync(F.Repo);
            Assert.Contains(published, unit =>
                unit.Origin.IndexUnitId.Value == "Extra/Extra.csproj" && !unit.RequiresOccurrenceAuthority);
            var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Single(result.Occurrences);
            Assert.True(result.IsComplete);
        }
    }

    [Fact]
    public async Task CompleteEmptyPublication_RemainsCompleteZero_NotRefused()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:empty", a.Empty);

        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
        Assert.Empty(result.Occurrences);
        Assert.Equal(0, result.Examined);
        Assert.True(result.IsComplete);

        // A genuinely complete empty result stays distinct from unavailable
        // coverage: omitting current authority still refuses.
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
            (await reopened.FindOccurrencesAsync(new(F.Repo, a.LibTarget))).Availability);
    }

    [Fact]
    public async Task Replacement_ReportsNewValidState_AndRefusesOldContext_AcrossReopen()
    {
        var a = await F.Build();
        var b = await F.Build("source");
        Assert.Equal(a.LibTarget, b.LibTarget);

        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.Empty);
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await b.Publish(store, "run:b", b.App, b.Empty);
            var current = await store.FindOccurrencesAsync(b.Query(b.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
            Assert.Single(current.Occurrences);
            Assert.Equal(CodeOccurrenceAvailability.StalePublication,
                (await store.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
        }

        using var reopened = new LatticeCodeGraphStore(_path);
        Assert.Equal(CodeOccurrenceAvailability.Available,
            (await reopened.FindOccurrencesAsync(b.Query(b.LibTarget))).Availability);
        Assert.Equal(CodeOccurrenceAvailability.StalePublication,
            (await reopened.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
    }
}
