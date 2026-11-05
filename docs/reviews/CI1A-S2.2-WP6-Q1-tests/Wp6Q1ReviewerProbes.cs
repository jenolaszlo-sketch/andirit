using Andirit;
using Andirit.LatticeDb;
using System.Text;
using Xunit.Abstractions;

namespace IndependentWp6Qualification;

/// <summary>
/// Independent WP6-Q1 qualification probes for the NEW-04 coverage-merge
/// correction. Expected values are derived from the accepted contracts
/// (obligation register NEW-04; S2.2 entry-acceptance falsification probes;
/// coverage-and-availability state conjunction), never from the production
/// merge. Integration probes drive the real store, sink, and restore paths.
/// </summary>
public sealed class Wp6Q1ReviewerProbes(ITestOutputHelper output)
{
    private static readonly CodePluginId Plugin = new("plugin:wp6q1");
    private static readonly DateTimeOffset Started = new(2026, 11, 5, 0, 0, 0, TimeSpan.Zero);

    private static readonly string ScopeS1 = "analysisscope1:sha256:" + Cs("s1");
    private static readonly string ScopeS2 = "analysisscope1:sha256:" + Cs("s2");
    private static readonly string ScopeS3 = "analysisscope1:sha256:" + Cs("s3");
    private static readonly string UniverseU1 = "compilation-universe1:sha256:" + Cs("u1");
    private static readonly string UniverseU2 = "compilation-universe1:sha256:" + Cs("u2");

    private static string Cs(string tag) =>
        Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(tag)));

    private static CodeOccurrenceCoverage C(
        int invocationFacts,
        CodeRelationshipCoverageState state,
        string? scopeId,
        string? universeId,
        int declared,
        int verified,
        int external = 0,
        int ambiguous = 0,
        int unresolved = 0,
        int unsupported = 0,
        int conditionalInactive = 0,
        int unsupportedCategories = 0,
        IReadOnlyDictionary<string, int>? unavailableReasons = null) =>
        new(invocationFacts, unsupportedCategories, state, scopeId, universeId,
            declared, verified, external, ambiguous, unresolved, unsupported,
            conditionalInactive, unavailableReasons);

    // ================================================= Q1-A scope/universe

    [Fact]
    public void Q1A1_SameScopeSameUniverse_ProducesComplete()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 2, 2)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.True(merged.IsComplete);
        Assert.Equal(ScopeS1, merged.ScopeId);
        Assert.Equal(UniverseU1, merged.UniverseId);
        Assert.Equal(3, merged.DeclaredUnitCount);
        Assert.Equal(3, merged.VerifiedUnitCount);
    }

    [Fact]
    public void Q1A2_DifferentScopesSameUniverse_NeverCompleteNorSingleScope()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
        Assert.Null(merged.ScopeId);
        // G-Q1A2 (reviewer finding): the committed implementation also discards a
        // MATCHING universe attribution when scopes are mixed. The authority is
        // silent on the universe under mixed scope; the behavior is conservative
        // (no false claim, no arbitrary first-selection) but loses true universe
        // information. Recorded for the owner; not a mandatory-rule violation.
        Assert.Null(merged.UniverseId);
        output.WriteLine("Q1A2: mixed-scope merge discards the matching universe attribution (G-Q1A2 qualification).");
        // Symmetric.
        var reverse = C(1, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 1, 1)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, reverse.State);
        Assert.Null(reverse.ScopeId);
        Assert.Null(reverse.UniverseId);
    }

    [Fact]
    public void Q1A3_SameScopeDifferentUniverses_RefusedBothOrders()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU2, 1, 1);
        Assert.Throws<ArgumentException>(() => a.Merge(b));
        Assert.Throws<ArgumentException>(() => b.Merge(a));
    }

    [Fact]
    public void Q1A4_DifferentScopesAndUniverses_Refused()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU2, 1, 1);
        Assert.Throws<ArgumentException>(() => a.Merge(b));
    }

    [Fact]
    public void Q1A5_NullScope_WithOneAttributed_KeepsSoleAttribution()
    {
        var merged = C(0, CodeRelationshipCoverageState.NotProduced, null, null, 1, 0)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1));
        Assert.Equal(ScopeS1, merged.ScopeId);
        Assert.Equal(UniverseU1, merged.UniverseId);
        // Completeness still degrades: the unattributed contribution did not run.
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
    }

    [Fact]
    public void Q1A6_NullScopeIntermediate_ThenAttributed_NeverRegainsComplete()
    {
        // A mixed-scope intermediate (null scope, Partial) combined with another
        // complete record must not regain a complete claim.
        var mixed = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, mixed.State);
        Assert.Null(mixed.ScopeId);

        var merged = mixed.Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS3, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
        Assert.Equal(3, merged.DeclaredUnitCount);
        Assert.Equal(3, merged.VerifiedUnitCount);
    }

    [Fact]
    public void Q1A7_NullUniverse_WithAttributed_SoleAttributionBoundary()
    {
        // A complete record that carries no universe attribution folds into the
        // sole attributed universe under the documented legacy boundary. Both
        // inputs claim complete; the conjunction is complete. Production
        // producers (extractor, stores) never create Produced-with-null-universe
        // records; this boundary is recorded as a reviewer qualification.
        var unattributed = C(1, CodeRelationshipCoverageState.Produced, null, null, 1, 1);
        var merged = unattributed.Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.Equal(UniverseU1, merged.UniverseId);
        output.WriteLine("Q1A7: Produced(null-universe) + Produced(U1) -> Produced(U1); sole-attribution boundary, recorded as qualification.");
    }

    [Fact]
    public void Q1A9_ThreeWayMixedScopes_EveryOrder_StaysPartial()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 1, 1);
        var c = C(1, CodeRelationshipCoverageState.Produced, ScopeS3, UniverseU1, 1, 1);

        var leftAssoc = a.Merge(b).Merge(c);
        var rightAssoc = a.Merge(b.Merge(c));
        var balanced = b.Merge(a.Merge(c));

        foreach (var merged in new[] { leftAssoc, rightAssoc, balanced })
        {
            Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
            Assert.False(merged.IsComplete);
            Assert.Equal(3, merged.DeclaredUnitCount);
            Assert.Equal(3, merged.VerifiedUnitCount);
            Assert.Equal(3, merged.InvocationFacts);
        }
        // The state is order-independent; the (informational, Partial) attribution
        // label follows the last attributed merge and is not a completeness claim.
        output.WriteLine($"Q1A9 attributions: left={leftAssoc.ScopeId is null}, right={rightAssoc.ScopeId is null}, balanced={balanced.ScopeId is null}");
    }

    // ================================================= Q1-B counter aggregation

    [Fact]
    public void Q1B_AllCountersSum_WithDistinctValues()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 3, 3,
            external: 7, ambiguous: 6, unresolved: 5, unsupported: 4, conditionalInactive: 3,
            unsupportedCategories: 2);
        var b = C(2, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1,
            external: 1, ambiguous: 2, unresolved: 3, unsupported: 4, conditionalInactive: 1,
            unsupportedCategories: 1);

        var merged = a.Merge(b);

        Assert.Equal(3, merged.InvocationFacts);
        Assert.Equal(8, merged.External);
        Assert.Equal(8, merged.Ambiguous);
        Assert.Equal(8, merged.Unresolved);
        Assert.Equal(8, merged.Unsupported);
        Assert.Equal(4, merged.ConditionalInactive);
        Assert.Equal(3, merged.UnsupportedCategories);
        Assert.Equal(4, merged.DeclaredUnitCount);
        Assert.Equal(4, merged.VerifiedUnitCount);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
    }

    [Fact]
    public void Q1B_EmptyContribution_IsNotAnalyzedAndDegradesCompleteness()
    {
        var empty = new CodeOccurrenceCoverage();
        Assert.Equal(CodeRelationshipCoverageState.NotProduced, empty.State);
        Assert.Equal(0, empty.DeclaredUnitCount);

        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1)
            .Merge(empty);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.Equal(1, merged.DeclaredUnitCount);
        Assert.Equal(1, merged.VerifiedUnitCount);
        Assert.Equal(1, merged.InvocationFacts);
    }

    [Fact]
    public void Q1B_OverflowBoundary_FailsClosed()
    {
        // The contract does not define overflow arithmetic; the observed behavior
        // is fail-closed rejection (negative sums violate the constructor).
        var huge = C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1,
            int.MaxValue, int.MaxValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => huge.Merge(huge));
    }

    [Fact]
    public void Q1B_UnavailableReasons_MergeByKey()
    {
        var a = C(0, CodeRelationshipCoverageState.Unavailable, ScopeS1, UniverseU1, 1, 0,
            unavailableReasons: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["missing-source"] = 4, ["no-analysis"] = 1,
            });
        var b = C(0, CodeRelationshipCoverageState.Unavailable, ScopeS1, UniverseU1, 1, 0,
            unavailableReasons: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["missing-source"] = 2,
            });

        var merged = a.Merge(b);
        Assert.Equal(6, merged.UnavailableReasons["missing-source"]);
        Assert.Equal(1, merged.UnavailableReasons["no-analysis"]);
        Assert.Equal(CodeRelationshipCoverageState.Unavailable, merged.State);
    }

    // ================================================= Q1-C declared=0 invariants

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(1, 0, true)]
    [InlineData(1, 1, true)]
    [InlineData(0, 1, false)]
    [InlineData(1, 2, false)]
    public void Q1C_ConstructorInvariant(int declared, int verified, bool valid)
    {
        if (valid)
        {
            var coverage = C(0, CodeRelationshipCoverageState.Partial, ScopeS1, UniverseU1, declared, verified);
            Assert.Equal(declared, coverage.DeclaredUnitCount);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => C(
                0, CodeRelationshipCoverageState.Partial, ScopeS1, UniverseU1, declared, verified));
        }
    }

    [Fact]
    public void Q1C_MixedZeroAndNonZero_Sums()
    {
        var merged = C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 0, 0)
            .Merge(C(0, CodeRelationshipCoverageState.Partial, ScopeS1, UniverseU1, 2, 1));
        Assert.Equal(2, merged.DeclaredUnitCount);
        Assert.Equal(1, merged.VerifiedUnitCount);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
    }

    [Fact]
    public void Q1C_EmptyCompleteAndEmptyPartial_Combine()
    {
        var completeEmpty = C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 0, 0);
        var emptyPartial = C(0, CodeRelationshipCoverageState.Partial, ScopeS1, UniverseU1, 0, 0);
        var merged = completeEmpty.Merge(emptyPartial);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
    }

    // ================================================= Q1-D P06 false-Produced

    [Fact]
    public void Q1D_TwoProducedDifferentScopes_IsTheP06Class()
    {
        var merged = C(2, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 2, 2)
            .Merge(C(2, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 2, 2));
        Assert.NotEqual(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.False(merged.IsComplete);
        Assert.Null(merged.ScopeId);
        Assert.Equal(4, merged.DeclaredUnitCount);
        Assert.Equal(4, merged.VerifiedUnitCount);
    }

    [Fact]
    public void Q1D_PartialPlusComplete_StaysPartial_EvenWhenNumericallyComplete()
    {
        // 2/2 Partial + 1/1 Produced sums to 3/3: numerically complete, but the
        // partial contribution's incompleteness is preserved by the state rule.
        var merged = C(0, CodeRelationshipCoverageState.Partial, ScopeS1, UniverseU1, 2, 2)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
        Assert.Equal(3, merged.DeclaredUnitCount);
        Assert.Equal(3, merged.VerifiedUnitCount);
    }

    [Fact]
    public void Q1D_CompletePlusRefusedContributions_KeepsRefusalsAndCompleteness()
    {
        // Refusal categories are recorded observations, not incompleteness
        // signals: a complete analysis that refused occurrences remains complete
        // with its refusal evidence intact (accepted WP5 semantics).
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1,
                external: 1, ambiguous: 1, unresolved: 1, unsupported: 1, conditionalInactive: 1)
            .Merge(C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1,
                external: 0, ambiguous: 2, unresolved: 0, unsupported: 2, conditionalInactive: 0));
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.True(merged.IsComplete);
        Assert.Equal(1, merged.External);
        Assert.Equal(3, merged.Ambiguous);
        Assert.Equal(1, merged.Unresolved);
        Assert.Equal(3, merged.Unsupported);
        Assert.Equal(1, merged.ConditionalInactive);
    }

    [Fact]
    public void Q1D_DifferentScopesMatchingCounts_StillNotComplete()
    {
        var merged = C(3, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 3, 3)
            .Merge(C(3, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 3, 3));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.Null(merged.ScopeId);
        Assert.Equal(6, merged.DeclaredUnitCount);
    }

    // ================================================= Q1-E store integration

    [Fact]
    public async Task Q1E_InMemoryStore_SameScopeUnits_ProduceCompleteQueryCoverage()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp6q1-complete");
        var target = Target(repo, "T1");

        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", ScopeS1, UniverseU1,
                declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)]),
            Unit(repo, "run:1", "unit:b", ScopeS1, UniverseU1,
                declared: 2, verified: 2, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:b", target, "src/B.cs", 1, 1)]));

        var query = await store.FindOccurrencesAsync(new(repo, target));
        Assert.Equal(CodeOccurrenceAvailability.Available, query.Availability);
        Assert.NotNull(query.Coverage);
        Assert.Equal(CodeRelationshipCoverageState.Produced, query.Coverage!.State);
        Assert.True(query.Coverage.IsComplete);
        Assert.Equal(3, query.Coverage.DeclaredUnitCount);
        Assert.Equal(3, query.Coverage.VerifiedUnitCount);
        Assert.Equal(2, query.Coverage.InvocationFacts);
        Assert.Equal(ScopeS1, query.Coverage.ScopeId);
        Assert.Equal(UniverseU1, query.Coverage.UniverseId);
        Assert.True(query.IsComplete);
    }

    [Fact]
    public async Task Q1E_InMemoryStore_MixedScopeUnits_CannotClaimComplete()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp6q1-mixed");
        var target = Target(repo, "T1");

        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", ScopeS1, UniverseU1,
                declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)]),
            Unit(repo, "run:1", "unit:b", ScopeS2, UniverseU1,
                declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:b", target, "src/B.cs", 1, 1)]));

        var query = await store.FindOccurrencesAsync(new(repo, target));
        Assert.NotNull(query.Coverage);
        Assert.Equal(CodeRelationshipCoverageState.Partial, query.Coverage!.State);
        Assert.False(query.Coverage.IsComplete);
        Assert.Null(query.Coverage.ScopeId);
        Assert.Equal(2, query.Coverage.DeclaredUnitCount);
        Assert.Equal(2, query.Coverage.VerifiedUnitCount);
        // The P06 class through the real aggregation path: mixed scopes never
        // yield complete, consumer-visible coverage.
        Assert.False(query.IsComplete);
        Assert.NotEqual(CodeOccurrenceAvailability.Available, query.Availability);
    }

    [Fact]
    public async Task Q1E_InMemoryStore_LegacyUnattributedUnit_DoesNotManufactureComplete()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp6q1-legacy");
        var target = Target(repo, "T1");

        // Legacy contribution: analysis ran, no completeness evidence, no identity.
        var legacy = new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId("run:1"), new CodeIndexUnitId("unit:legacy")),
            nodes: [Node("node:legacy")],
            occurrences: [Fact(repo, "unit:legacy", target, "src/L.cs", 1, 1)],
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Partial));
        var attributed = Unit(repo, "run:1", "unit:a", ScopeS1, UniverseU1,
            declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
            [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)]);

        await PublishAsync(store, repo, "run:1", legacy, attributed);

        var query = await store.FindOccurrencesAsync(new(repo, target));
        Assert.NotNull(query.Coverage);
        Assert.Equal(CodeRelationshipCoverageState.Partial, query.Coverage!.State);
        Assert.False(query.Coverage.IsComplete);
        Assert.False(query.IsComplete);
        // Legacy facts are retained; completeness is not manufactured.
        Assert.NotEmpty(query.Occurrences);
    }

    [Fact]
    public async Task Q1E_InMemoryStore_DifferingUniversesAcrossUnits_StillAggregatesByRepository()
    {
        // Units of one repository publication may carry different universe
        // identities; the repository aggregation must refuse to merge them and
        // the store must not crash or fabricate completeness.
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp6q1-uni");
        var target = Target(repo, "T1");

        await PublishAsync(store, repo, "run:1",
            Unit(repo, "run:1", "unit:a", ScopeS1, UniverseU1,
                declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)]),
            Unit(repo, "run:1", "unit:b", ScopeS1, UniverseU2,
                declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                [Fact(repo, "unit:b", target, "src/B.cs", 1, 1)]));

        // The repository aggregation refuses incompatible universes (the mandated
        // NEW-04 refusal) and surfaces it fail-closed at the query boundary: no
        // coverage is fabricated and no completeness is claimed. Recorded as a
        // qualification: multi-universe repositories reject occurrence queries.
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindOccurrencesAsync(new(repo, target)));
        output.WriteLine("Q1E differing-universe units: query fails closed with the mandated merge refusal (no false coverage).");
    }

    [Fact]
    public async Task Q1E_Sink_AccumulatesBatchCoverage_BeforeStoreCommit()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp6q1-sink");
        var target = Target(repo, "T1");
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:1"), Started, plugins: [Plugin]));

        await using (var sink = new CodeGraphIngestionSink(store))
        {
            // Same-origin batches accumulate: complete + complete stays complete
            // with summed unit counts.
            await sink.WriteBatchAsync(new CodeGraphBatch(
                Origin(repo, "run:1", "unit:a"),
                nodes: [Node("node:a")],
                occurrences: [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)],
                occurrenceCoverage: C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1)));
            await sink.WriteBatchAsync(new CodeGraphBatch(
                Origin(repo, "run:1", "unit:a"),
                completesIndexUnit: true,
                occurrenceCoverage: C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 2, 2)));
            await sink.CompleteAsync();
        }

        await store.CompleteIndexRunAsync(
            new(repo, new CodeIndexRunId("run:1"), Started, CodeIndexRunStatus.Completed,
                Started.AddSeconds(1), [Plugin]),
            new(repo, new CodeIndexRunId("run:1"), []));

        var query = await store.FindOccurrencesAsync(new(repo, target));
        Assert.NotNull(query.Coverage);
        Assert.Equal(CodeRelationshipCoverageState.Produced, query.Coverage!.State);
        Assert.Equal(3, query.Coverage.DeclaredUnitCount);
        Assert.Equal(3, query.Coverage.VerifiedUnitCount);
        Assert.True(query.Coverage.IsComplete);

        // Mixed-scope accumulation for another origin degrades to Partial.
        await store.StoreIndexRunAsync(new(repo, new CodeIndexRunId("run:2"), Started.AddSeconds(5), plugins: [Plugin]));
        await using (var sink = new CodeGraphIngestionSink(store))
        {
            await sink.WriteBatchAsync(new CodeGraphBatch(
                Origin(repo, "run:2", "unit:m"),
                nodes: [Node("node:m")],
                occurrenceCoverage: C(0, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1)));
            await sink.WriteBatchAsync(new CodeGraphBatch(
                Origin(repo, "run:2", "unit:m"),
                completesIndexUnit: true,
                occurrenceCoverage: C(0, CodeRelationshipCoverageState.Produced, ScopeS2, UniverseU1, 1, 1)));
            await sink.CompleteAsync();
        }

        await store.CompleteIndexRunAsync(
            new(repo, new CodeIndexRunId("run:2"), Started.AddSeconds(5), CodeIndexRunStatus.Completed,
                Started.AddSeconds(6), [Plugin]),
            new(repo, new CodeIndexRunId("run:2"), []));

        var mixed = await store.FindOccurrencesAsync(new(repo, target));
        Assert.NotNull(mixed.Coverage);
        Assert.Equal(CodeRelationshipCoverageState.Partial, mixed.Coverage!.State);
        Assert.False(mixed.Coverage.IsComplete);
    }

    [Fact]
    public async Task Q1E_LatticeRestore_RoundTripsCorrectedMerge()
    {
        var path = TemporaryDatabasePath();
        var repo = new CodeRepositoryId("repo:wp6q1-lattice");
        var target = Target(repo, "T1");
        try
        {
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(store, repo, "run:1",
                    Unit(repo, "run:1", "unit:a", ScopeS1, UniverseU1,
                        declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                        [Fact(repo, "unit:a", target, "src/A.cs", 1, 1)]),
                    Unit(repo, "run:1", "unit:b", ScopeS1, UniverseU1,
                        declared: 2, verified: 2, state: CodeRelationshipCoverageState.Produced,
                        [Fact(repo, "unit:b", target, "src/B.cs", 1, 1)]));
            }

            // Reopen: the restore path must re-derive coverage through the
            // corrected merge and must not resurrect a false complete claim.
            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var query = await reopened.FindOccurrencesAsync(new(repo, target));
                Assert.NotNull(query.Coverage);
                Assert.Equal(CodeRelationshipCoverageState.Produced, query.Coverage!.State);
                Assert.True(query.Coverage.IsComplete);
                Assert.Equal(3, query.Coverage.DeclaredUnitCount);
                Assert.Equal(3, query.Coverage.VerifiedUnitCount);
            }

            // Mixed-scope publication round-trips as Partial.
            using (var store = new LatticeCodeGraphStore(path))
            {
                await PublishAsync(store, repo, "run:2",
                    Unit(repo, "run:2", "unit:c", ScopeS2, UniverseU1,
                        declared: 1, verified: 1, state: CodeRelationshipCoverageState.Produced,
                        [Fact(repo, "unit:c", target, "src/C.cs", 1, 1)]));
            }

            using (var reopened = new LatticeCodeGraphStore(path))
            {
                var query = await reopened.FindOccurrencesAsync(new(repo, target));
                Assert.NotNull(query.Coverage);
                Assert.Equal(CodeRelationshipCoverageState.Partial, query.Coverage!.State);
                Assert.False(query.Coverage.IsComplete);
                Assert.False(query.IsComplete);
                output.WriteLine($"Q1E lattice mixed restore: state={query.Coverage.State} scope={(query.Coverage.ScopeId is null ? "null" : "set")}");
            }
        }
        finally
        {
            TryDelete(path);
        }
    }

    // ================================================= Q1-H determinism

    [Fact]
    public void Q1H_MergeDeterminism_CanonicalDigest()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 2, 2,
                external: 1, ambiguous: 2, unresolved: 3, unsupported: 4, conditionalInactive: 5,
                unsupportedCategories: 6)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeS1, UniverseU1, 1, 1,
                external: 1, ambiguous: 1, unresolved: 1, unsupported: 1, conditionalInactive: 1,
                unsupportedCategories: 1));

        var digest =
            $"WP6Q1-DIGEST|state={merged.State}|scope={(merged.ScopeId == ScopeS1 ? "S1" : merged.ScopeId)}" +
            $"|universe={(merged.UniverseId == UniverseU1 ? "U1" : merged.UniverseId)}" +
            $"|declared={merged.DeclaredUnitCount}|verified={merged.VerifiedUnitCount}" +
            $"|facts={merged.InvocationFacts}|ext={merged.External}|amb={merged.Ambiguous}" +
            $"|unres={merged.Unresolved}|unsup={merged.Unsupported}|cond={merged.ConditionalInactive}" +
            $"|cats={merged.UnsupportedCategories}|complete={merged.IsComplete}";
        output.WriteLine(digest);
        Assert.Equal(
            "WP6Q1-DIGEST|state=Produced|scope=S1|universe=U1|declared=3|verified=3|facts=2|ext=2|amb=3|unres=4|unsup=5|cond=6|cats=7|complete=True",
            digest);
    }

    // ---------------------------------------------------------------- helpers

    private static CodeFactOrigin Origin(CodeRepositoryId repo, string run, string unit) =>
        new(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unit));

    private static CodeOccurrenceFact Fact(
        CodeRepositoryId repo, string unit, CodeOccurrenceTargetId target,
        string path, int line, int column) =>
        CodeOccurrenceFact.Create(
            repo, target, target.Value, unit,
            new CodeOccurrenceContributor(Plugin, unit),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation(path, line, column, line, column + 4),
            "sha256:source", "content:invocation");

    private static CodeOccurrenceTargetId Target(CodeRepositoryId repo, string name) =>
        CodeOccurrenceIdentity.ComputeTargetId(
            "compctx1:sha256:" + Cs(repo.Value + name),
            $"{repo.Value}|1.0.0||", "module.dll",
            "qid:" + name, "qdecl:T." + name, "qmethod|0");

    private static CodeGraphNode Node(string id) =>
        new(new CodeNodeId(id), CodeNodeKinds.Callable, id, id, new CodeSymbolId($"symbol:{id}"));

    private static CodeIndexUnitReplacement Unit(
        CodeRepositoryId repo, string run, string unit, string scope, string universe,
        int declared, int verified, CodeRelationshipCoverageState state,
        IReadOnlyList<CodeOccurrenceFact> occurrences) =>
        new(
            Origin(repo, run, unit),
            nodes: occurrences.Select((_, index) => Node($"node:{unit}:{index}")).ToArray(),
            occurrences: occurrences,
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: C(occurrences.Count, state, scope, universe, declared, verified));

    private static async Task PublishAsync(
        InMemoryCodeGraphStore store, CodeRepositoryId repo, string run,
        params CodeIndexUnitReplacement[] units)
        => await PublishCoreAsync(store, repo, run, units);

    private static async Task PublishAsync(
        LatticeCodeGraphStore store, CodeRepositoryId repo, string run,
        params CodeIndexUnitReplacement[] units)
        => await PublishCoreAsync(store, repo, run, units);

    private static async Task PublishCoreAsync(
        ICodeGraphIndexStore store, CodeRepositoryId repo, string run,
        IReadOnlyList<CodeIndexUnitReplacement> units)
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
        Path.Combine(Path.GetTempPath(), $"wp6q1-{Guid.NewGuid():N}.db");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
        }
    }
}
