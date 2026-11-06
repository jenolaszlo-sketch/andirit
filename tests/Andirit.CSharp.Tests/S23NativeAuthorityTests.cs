using System.Text.Json;
using System.Text.Json.Nodes;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using F = Andirit.CSharp.Tests.S23NativeAuthorityFixture;

namespace Andirit.CSharp.Tests;

public sealed class S23NativeAuthorityTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "andirit-s23-" + Guid.NewGuid().ToString("N") + ".ltdb");
    public void Dispose()
    {
        foreach (var suffix in new[] { "", "-wal", "-journal", ".wal" })
            if (File.Exists(_path + suffix)) File.Delete(_path + suffix);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("options")]
    public async Task Test8b_TransitiveOnlyChange_StableIdentityRefusesNativeReopenedAuthority(string variant)
    {
        var a = await F.Build();
        var b = await F.Build(variant);
        Assert.Equal(a.LibTarget, b.LibTarget);
        Assert.NotEqual(a.Lib.Descriptor.DescriptorDigest, b.Lib.Descriptor.DescriptorDigest);
        Assert.All(a.Entries.Take(5).Zip(b.Entries.Take(5)), pair =>
            Assert.NotEqual(pair.First.Descriptor.DescriptorDigest, pair.Second.Descriptor.DescriptorDigest));
        var target = Assert.IsAssignableFrom<IMethodSymbol>(a.Lib.Compilation.GetTypeByMetadataName("B.Lib")!.GetMembers("V").Single());
        Assert.Equal(F.ExpectedTarget(a.Lib, target), a.LibTarget.Value);
        Assert.All(a.Extract(a.App).Facts, fact => Assert.Equal(F.ExpectedUsage(fact), fact.Id.Value));
        using (var store = new LatticeCodeGraphStore(_path))
            await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
        var recorded = F.Snapshot(_path);
        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var good = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, good.Availability);
            Assert.Equal(2, good.Occurrences.Count);
            Assert.True(good.IsComplete);
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
                (await reopened.FindOccurrencesAsync(new(F.Repo, a.LibTarget))).Availability);
            var stale = await reopened.FindOccurrencesAsync(b.Query(b.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
            Assert.Empty(stale.Occurrences);
            Assert.Equal(0, stale.Examined);
            Assert.False(stale.IsComplete);
            Assert.Equal("run:a", stale.Publication!.IndexRunId.Value);
        }
        Assert.Equal(recorded, F.Snapshot(_path));
        using (var fresh = new LatticeCodeGraphStore(_path))
        {
            await b.Publish(fresh, "run:b", b.App, b.App2, b.Empty);
            var current = await fresh.FindOccurrencesAsync(b.Query(b.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
            Assert.Equal(2, current.Occurrences.Count);
        }
        using var bReopen = new LatticeCodeGraphStore(_path);
        Assert.Equal(CodeOccurrenceAvailability.Available, (await bReopen.FindOccurrencesAsync(b.Query(b.LibTarget))).Availability);
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, (await bReopen.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
    }

    [Fact]
    public async Task CrossProjectMultiContext_DurableReceiptsBindEachFactToItsDefiningCompilation()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path)) await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
        var rows = F.Rows(_path);
        var app = rows.Single(p => p.Key.StartsWith("AndiritOccurrenceUnit|", StringComparison.Ordinal) &&
            p.Value["IndexUnitId"]!.GetValue<string>() == "App/App.csproj").Value;
        var authority = app["Authority"]!.AsObject();
        Assert.Equal("App/App.csproj", authority["SourceUnitId"]!.GetValue<string>());
        var receipts = authority["Facts"]!.AsArray();
        Assert.Equal(2, receipts.Count);
        Assert.Equal(new[] { "Lib/Lib.csproj", "Mid/Mid.csproj" }, receipts.Select(r =>
            r!["DefiningProjectUnitId"]!.GetValue<string>()).Order(StringComparer.Ordinal).ToArray());
        foreach (var receipt in receipts)
        {
            var defining = a.Entries.Single(e => e.Descriptor.ProjectUnitId == receipt!["DefiningProjectUnitId"]!.GetValue<string>());
            Assert.Equal(defining.Descriptor.DescriptorDigest, receipt!["Compctx2"]!.GetValue<string>());
            Assert.NotEqual(a.App.Descriptor.DescriptorDigest, receipt["Compctx2"]!.GetValue<string>());
        }
        Assert.Equal("run:a", app["AuthorityPublicationIndexRunId"]!.GetValue<string>());
        foreach (var unit in rows.Where(p => p.Key.StartsWith("HetuUnit|", StringComparison.Ordinal)).Select(p => p.Value))
        {
            Assert.False(unit.ContainsKey("Occurrences"));
            Assert.False(unit.ContainsKey("OccurrenceAuthority"));
            Assert.True(unit["RequiresOccurrenceAuthority"]!.GetValue<bool>());
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        foreach (var fact in a.Extract(a.App).Facts)
        {
            var result = await reopened.FindOccurrencesAsync(a.Query(fact.TargetId));
            Assert.Equal(CodeOccurrenceAvailability.Available, result.Availability);
            Assert.Contains(result.Occurrences, hit => hit.Id == fact.Id);
        }
    }

    [Theory]
    [InlineData("missing-envelope", CodeOccurrenceAvailability.AnalysisUnavailable)]
    [InlineData("missing-context", CodeOccurrenceAvailability.AnalysisUnavailable)]
    [InlineData("corrupt-context", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("unsupported-version", CodeOccurrenceAvailability.UnsupportedSchema)]
    [InlineData("corrupt-version", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("malformed-envelope", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("wrong-defining-unit", CodeOccurrenceAvailability.StalePublication)]
    [InlineData("caller-as-defining", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("formatted-unverified-context", CodeOccurrenceAvailability.StalePublication)]
    [InlineData("different-membership", CodeOccurrenceAvailability.StalePublication)]
    [InlineData("duplicate-membership", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("missing-members", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("wrong-target", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("wrong-payload", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("missing-receipts", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("duplicate-receipts", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("wrong-owner", CodeOccurrenceAvailability.CorruptedIndex)]
    [InlineData("wrong-publication", CodeOccurrenceAvailability.StalePublication)]
    [InlineData("coverage-substitution", CodeOccurrenceAvailability.CorruptedIndex)]
    public async Task MalformedOrSubstitutedAuthority_RefusesWithoutChangingDurableRows(string mutation, CodeOccurrenceAvailability expected)
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path)) await a.Publish(store, "run:a", a.App, a.App2, a.Empty);
        F.MutateUnit(_path, row =>
        {
            var authority = row["Authority"]!.AsObject();
            var fact = authority["Facts"]![0]!.AsObject();
            switch (mutation)
            {
                case "missing-envelope": row.Remove("Authority"); break;
                case "missing-context": fact.Remove("Compctx2"); break;
                case "corrupt-context": fact["Compctx2"] = "not-a-context"; break;
                case "unsupported-version": authority["Version"] = 2; break;
                case "corrupt-version": authority["Version"] = "one"; break;
                case "malformed-envelope": row["Authority"] = "not-an-object"; break;
                case "wrong-defining-unit": fact["DefiningProjectUnitId"] = "App/App.csproj"; break;
                case "caller-as-defining":
                    fact["DefiningProjectUnitId"] = "App/App.csproj";
                    fact["Compctx2"] = a.App.Descriptor.DescriptorDigest; break;
                case "formatted-unverified-context": fact["Compctx2"] = "compctx2:sha256:" + new string('a', 64); break;
                case "different-membership": authority["Members"]![0]!["Compctx2"] = "compctx2:sha256:" + new string('b', 64); break;
                case "duplicate-membership": authority["Members"]!.AsArray().Add(authority["Members"]![0]!.DeepClone()); break;
                case "missing-members": authority.Remove("Members"); break;
                case "wrong-target": fact["TargetId"] = "asmctx3:sha256:" + new string('a', 64); break;
                case "wrong-payload": fact["PayloadHash"] = new string('a', 64); break;
                case "missing-receipts": authority["Facts"] = new JsonArray(); break;
                case "duplicate-receipts": authority["Facts"]![1] = fact.DeepClone(); break;
                case "wrong-owner": authority["IndexUnitId"] = "foreign-unit"; break;
                case "wrong-publication": row["AuthorityPublicationIndexRunId"] = "run:foreign"; break;
                case "coverage-substitution": authority["Coverage"]!["InvocationFacts"] = 0; break;
            }
        });
        var tampered = F.Snapshot(_path);
        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(expected, result.Availability);
            Assert.Empty(result.Occurrences);
            Assert.Equal(0, result.Examined);
            Assert.False(result.IsComplete);
            Assert.NotNull(result.RefusalReason);
        }
        Assert.Equal(tampered, F.Snapshot(_path));
    }

    [Fact]
    public async Task CompleteEmpty_RequiresCurrentVerifiedScope_AndRemainsDistinctFromGraphOnly()
    {
        var a = await F.Build();
        var b = await F.Build("source");
        using (var store = new LatticeCodeGraphStore(_path)) await a.Publish(store, "run:empty", a.Empty);
        using (var reopened = new LatticeCodeGraphStore(_path))
        {
            var empty = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.True(empty.IsComplete);
            Assert.Empty(empty.Occurrences);
            Assert.Equal(CodeOccurrenceAvailability.StalePublication, (await reopened.FindOccurrencesAsync(b.Query(b.LibTarget))).Availability);
            Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, (await reopened.FindOccurrencesAsync(new(F.Repo, a.LibTarget))).Availability);
            await F.Start(reopened, "run:graph-only");
            await reopened.StageIndexUnitAsync(new(F.Origin("run:graph-only", a.Empty.Descriptor.ProjectUnitId)));
            await F.Complete(reopened, "run:graph-only");
        }
        using var graphOnly = new LatticeCodeGraphStore(_path);
        var unavailable = await graphOnly.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, unavailable.Availability);
        Assert.Null(unavailable.Coverage);
    }

    [Fact]
    public async Task PartialCoverage_RemainsIncompleteAfterVerifiedReopen()
    {
        var a = await F.Build();
        var extracted = a.Extract(a.App);
        var coverage = extracted.Coverage.WithState(CodeRelationshipCoverageState.Partial);
        var partial = extracted with { Coverage = coverage, Authority = extracted.Authority! with { Coverage = coverage } };
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:partial");
            await store.StageIndexUnitAsync(partial.PrepareNativeUnit(F.Origin("run:partial", a.App.Descriptor.ProjectUnitId)));
            await F.Complete(store, "run:partial");
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.IncompleteAnalysis, result.Availability);
        Assert.Single(result.Occurrences);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public async Task ReplacementDeletion_RetiresReceipts_AndVerifiedRetiredTargetIsAvailableZero()
    {
        var a = await F.Build();
        var retired = await F.Build(retire: true);
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await a.Publish(store, "run:a", a.App, a.Empty);
            var old = (await store.GetLatestPublicationAsync(F.Repo))!;
            await retired.Publish(store, "run:retired", retired.App, retired.Empty);
            var empty = await store.FindOccurrencesAsync(retired.Query(a.LibTarget));
            Assert.True(empty.IsComplete);
            Assert.Empty(empty.Occurrences);
            Assert.Equal(CodeOccurrenceAvailability.StalePublication,
                (await store.FindOccurrencesAsync(retired.Query(a.LibTarget, old))).Availability);
            await F.Start(store, "run:delete");
            await store.StageIndexUnitDeletionAsync(F.Repo, new("run:delete"), F.Plugin, new(a.App.Descriptor.ProjectUnitId));
            await F.Complete(store, "run:delete");
        }
        var units = F.Rows(_path, "AndiritOccurrenceUnit");
        Assert.Single(units);
        Assert.Empty(units.Single().Value["Authority"]!["Facts"]!.AsArray());
        Assert.Equal("run:delete", units.Single().Value["AuthorityPublicationIndexRunId"]!.GetValue<string>());
        using var reopened = new LatticeCodeGraphStore(_path);
        var result = await reopened.FindOccurrencesAsync(retired.Query(a.LibTarget));
        Assert.True(result.IsComplete);
        Assert.Empty(result.Occurrences);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KnownFailure_RecoversPublishedAuthority_WithOriginalExceptionAndAtomicRetry(bool corruptCarrier)
    {
        var a = await F.Build();
        var b = await F.Build("source");
        using (var setup = new LatticeCodeGraphStore(_path)) await a.Publish(setup, "run:a", a.App, a.Empty);
        if (corruptCarrier) F.MutateUnit(_path, row => row["Authority"]!["Version"] = 900);
        var before = F.Snapshot(_path);
        var arm = false;
        using (var store = new LatticeCodeGraphStore(_path, null, point =>
            { if (arm && point == "before-commit") throw new S23FaultException(); }))
        {
            await F.Start(store, "run:b");
            await store.StageIndexUnitAsync(b.Replacement("run:b", b.App));
            await store.StageIndexUnitAsync(b.Replacement("run:b", b.Empty));
            arm = true;
            await Assert.ThrowsAsync<S23FaultException>(async () => await F.Complete(store, "run:b"));
            arm = false;
            var recovered = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(corruptCarrier ? CodeOccurrenceAvailability.UnsupportedSchema : CodeOccurrenceAvailability.Available, recovered.Availability);
            if (!corruptCarrier) Assert.Single(recovered.Occurrences);
        }
        // A failed completion preserves occurrence authority and graph publication;
        // run/stage rows reflect their separate successful staging operations.
        var oldUnits = JsonNode.Parse(before)!.AsObject().Where(p => p.Key.StartsWith("AndiritOccurrence", StringComparison.Ordinal));
        var newUnits = F.Rows(_path).Where(p => p.Key.StartsWith("AndiritOccurrence", StringComparison.Ordinal));
        Assert.Equal(JsonSerializer.Serialize(oldUnits.ToDictionary(p => p.Key, p => p.Value)), JsonSerializer.Serialize(newUnits.ToDictionary(p => p.Key, p => p.Value)));
        using var retry = new LatticeCodeGraphStore(_path);
        // Explicit re-staging is required by Q1-Q1 after this store lifetime change.
        await retry.StageIndexUnitAsync(b.Replacement("run:b", b.App));
        await retry.StageIndexUnitAsync(b.Replacement("run:b", b.Empty));
        await F.Complete(retry, "run:b");
        Assert.Equal(CodeOccurrenceAvailability.Available, (await retry.FindOccurrencesAsync(b.Query(b.LibTarget))).Availability);
    }

    [Fact]
    public async Task SegmentResumption_MarkerSurvives_ButMissingFactsAndCarrierNeverBecomeVerifiedEmpty()
    {
        var a = await F.Build();
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:resume");
            await store.StageIndexUnitAsync(a.Replacement("run:resume", a.App));
        }
        var segment = F.Rows(_path, "HetuStage").Single().Value["Replacement"]!.AsObject();
        Assert.True(segment["RequiresOccurrenceAuthority"]!.GetValue<bool>());
        Assert.False(segment.ContainsKey("Occurrences"));
        Assert.False(segment.ContainsKey("OccurrenceAuthority"));
        using (var resumed = new LatticeCodeGraphStore(_path)) await F.Complete(resumed, "run:resume");
        using var reopened = new LatticeCodeGraphStore(_path);
        Assert.NotEmpty(await reopened.GetPublishedUnitsAsync(F.Repo));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, (await reopened.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, (await reopened.FindOccurrencesAsync(new(F.Repo, a.LibTarget))).Availability);
    }

    [Fact]
    public async Task SubstitutedLiveInstanceAndIncompleteUniverse_CannotSupplyNativeAuthority()
    {
        var a = await F.Build();
        var other = await F.Build();
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpOccurrenceAuthority.Bind(new(F.Repo, a.LibTarget), a.Universe, other.App.Compilation));
        var substituted = a.Extract(other.App);
        Assert.Null(substituted.Authority);
        Assert.Throws<InvalidDataException>(() => substituted.PrepareNativeUnit(F.Origin("run:bad", other.App.Descriptor.ProjectUnitId)));
        var incomplete = VerifiedCSharpUniverse.Create(F.Repo.Value, a.Universe.Scope, a.Entries, ["refused"]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpOccurrenceAuthority.Bind(new(F.Repo, a.LibTarget), incomplete, a.App.Compilation));
    }

    [Fact]
    public async Task ApplyFailure_RestoresPublishedAuthority_WithoutNewReconstructionFailure()
    {
        var a = await F.Build();
        using var store = new LatticeCodeGraphStore(_path);
        await a.Publish(store, "run:a", a.App, a.Empty);
        await F.Start(store, "run:bad");
        var edge = new CodeGraphEdge(new("edge:missing"), new("node:absent1"), new("node:absent2"),
            CodeEdgeKinds.Calls, new(EvidenceQuality.Resolved, F.Plugin.Value, "1.0.0"));
        await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
            await store.StageIndexUnitAsync(new(F.Origin("run:bad", "unit:bad"), edges: [edge])));
        var recovered = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
        Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
        Assert.Single(recovered.Occurrences);
    }

    [Fact]
    public async Task LegacyRawEvidence_IsNeverPromotedByTheVerifiedNativeGate()
    {
        var a = await F.Build();
        var extraction = a.Extract(a.App);
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:legacy");
            await store.StageIndexUnitAsync(new(F.Origin("run:legacy", a.App.Descriptor.ProjectUnitId),
                occurrences: extraction.Facts, occurrenceAnalysisIncluded: true, occurrenceCoverage: extraction.Coverage));
            await F.Complete(store, "run:legacy");
        }
        using var reopened = new LatticeCodeGraphStore(_path);
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable,
            (await reopened.FindOccurrencesAsync(a.Query(a.LibTarget))).Availability);
    }

    [Fact]
    public async Task MixedVerifiedContexts_CannotOverwriteOrSelectOneContextForAStableTarget()
    {
        var a = await F.Build();
        var b = await F.Build("source");
        Assert.Equal(a.LibTarget, b.LibTarget);
        using (var setup = new LatticeCodeGraphStore(_path)) await a.Publish(setup, "run:a", a.App, a.Empty);
        var before = JsonSerializer.Serialize(F.Rows(_path, "AndiritOccurrenceUnit", "AndiritOccurrencePage", "AndiritOccurrenceMeta"));
        using (var store = new LatticeCodeGraphStore(_path))
        {
            await F.Start(store, "run:mixed");
            await store.StageIndexUnitAsync(b.Replacement("run:mixed", b.App2));
            // The accepted coverage algebra rejects differing verified universes;
            // no first-target selection or implicit context overwrite is allowed.
            await Assert.ThrowsAsync<ArgumentException>(async () => await F.Complete(store, "run:mixed"));
            var recovered = await store.FindOccurrencesAsync(a.Query(a.LibTarget));
            Assert.Equal(CodeOccurrenceAvailability.Available, recovered.Availability);
            Assert.Single(recovered.Occurrences);
            Assert.Equal("run:a", recovered.Publication!.IndexRunId.Value);
        }
        Assert.Equal(before, JsonSerializer.Serialize(F.Rows(_path, "AndiritOccurrenceUnit", "AndiritOccurrencePage", "AndiritOccurrenceMeta")));
    }

    private sealed class S23FaultException : Exception;
}
