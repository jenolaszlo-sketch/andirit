using Andirit;
using Xunit;

namespace Andirit.Tests;

/// <summary>
/// S2.2 WP6: NEW-04 `CodeOccurrenceCoverage.Merge` coverage-authority correction.
/// Expected values are derived from the accepted contract (obligation register
/// NEW-04; S2.2 entry-acceptance owner falsification probes; coverage-and-availability
/// dimension rules), never from the production merge. Combining coverage evidence
/// must never manufacture successful coverage, conceal refusal or incomplete
/// evidence, or attribute a mixed combination to a single scope.
/// </summary>
public sealed class S2_2Wp6CoverageMergeTests
{
    private const string ScopeA = "analysisscope1:sha256:" + ConstA;
    private const string ScopeB = "analysisscope1:sha256:" + ConstB;
    private const string UniverseU1 = "compilation-universe1:sha256:" + ConstC;
    private const string UniverseU2 = "compilation-universe1:sha256:" + ConstD;

    private const string ConstA = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ConstB = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ConstC = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string ConstD = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";

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
        new(
            invocationFacts,
            unsupportedCategories,
            state,
            scopeId,
            universeId,
            declared,
            verified,
            external,
            ambiguous,
            unresolved,
            unsupported,
            conditionalInactive,
            unavailableReasons);

    // ---------------------------------------------- original NEW-04 defects

    [Fact]
    public void OriginalDefect_UnitCountsMustSum_NotMax()
    {
        // Same scope/universe, disjoint unit batches. The register's max-count
        // defect reports max(2,2)=2 declared and max(1,2)=2 verified, hiding one
        // unverified unit; the contract combines disjoint contributions by sum.
        var a = C(0, CodeRelationshipCoverageState.Partial, ScopeA, UniverseU1,
            declared: 2, verified: 1, unresolved: 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1,
            declared: 2, verified: 2);

        var merged = a.Merge(b);

        Assert.Equal(4, merged.DeclaredUnitCount);
        Assert.Equal(3, merged.VerifiedUnitCount);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.Equal(1, merged.InvocationFacts);
        Assert.Equal(1, merged.Unresolved);
        Assert.False(merged.IsComplete);
    }

    [Fact]
    public void OriginalDefect_MixedScopes_NeverClaimProducedOrSingleScope()
    {
        // The P06 defect: two Produced records from different analysis scopes
        // merged into `Produced` attributed to the first scope. The combination
        // is neither complete nor attributable to a single scope.
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1,
            declared: 1, verified: 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeB, UniverseU1,
            declared: 1, verified: 1);

        var merged = a.Merge(b);

        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
        Assert.Null(merged.ScopeId);
        Assert.Equal(2, merged.InvocationFacts);
        Assert.Equal(2, merged.DeclaredUnitCount);
        Assert.Equal(2, merged.VerifiedUnitCount);
        // Symmetric: order does not rescue an attribution.
        var reverse = b.Merge(a);
        Assert.Equal(CodeRelationshipCoverageState.Partial, reverse.State);
        Assert.Null(reverse.ScopeId);
    }

    [Fact]
    public void OriginalDefect_DifferingUniverses_MergeRefused()
    {
        // Verified-unit evidence from different verified compilation universes
        // is not comparable; the merge is refused outright.
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1,
            declared: 1, verified: 1);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU2,
            declared: 1, verified: 1);

        Assert.Throws<ArgumentException>(() => a.Merge(b));
        Assert.Throws<ArgumentException>(() => b.Merge(a));
    }

    [Fact]
    public void OriginalDefect_ZeroDeclaredWithObservedVerified_Rejected()
    {
        // The constructor exempted declared=0 from the verified<=declared
        // invariant, accepting verified evidence without any declared unit.
        Assert.Throws<ArgumentException>(() => C(
            2, CodeRelationshipCoverageState.Produced, null, null,
            declared: 0, verified: 2));
    }

    // ---------------------------------------------- state conjunction rules

    [Fact]
    public void PositiveControl_MatchingScopeCompleteBatches_ProduceComplete()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1,
            declared: 2, verified: 2);
        var b = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1,
            declared: 1, verified: 1);

        var merged = a.Merge(b);

        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.True(merged.IsComplete);
        Assert.Equal(ScopeA, merged.ScopeId);
        Assert.Equal(UniverseU1, merged.UniverseId);
        Assert.Equal(3, merged.DeclaredUnitCount);
        Assert.Equal(3, merged.VerifiedUnitCount);
        Assert.Equal(2, merged.InvocationFacts);
    }

    [Fact]
    public void PartialPlusComplete_IsPartial()
    {
        var merged = C(0, CodeRelationshipCoverageState.Partial, ScopeA, UniverseU1, 2, 1)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.Equal(3, merged.DeclaredUnitCount);
        Assert.Equal(2, merged.VerifiedUnitCount);
    }

    [Fact]
    public void CompletePlusNotAnalyzed_IsPartial()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1)
            .Merge(C(0, CodeRelationshipCoverageState.NotProduced, ScopeA, UniverseU1, 1, 0));
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.False(merged.IsComplete);
    }

    [Fact]
    public void NotProducedPlusNotProduced_IsNotProduced()
    {
        var merged = C(0, CodeRelationshipCoverageState.NotProduced, ScopeA, UniverseU1, 1, 0)
            .Merge(C(0, CodeRelationshipCoverageState.NotProduced, ScopeA, UniverseU1, 1, 0));
        Assert.Equal(CodeRelationshipCoverageState.NotProduced, merged.State);
        Assert.Equal(2, merged.DeclaredUnitCount);
        Assert.Equal(0, merged.VerifiedUnitCount);
    }

    [Fact]
    public void UnavailableDominates()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1)
            .Merge(C(0, CodeRelationshipCoverageState.Unavailable, ScopeA, UniverseU1, 1, 0));
        Assert.Equal(CodeRelationshipCoverageState.Unavailable, merged.State);
        Assert.False(merged.AnalysisPerformed);
    }

    // ---------------------------------------------- refusal/counter integrity

    [Fact]
    public void RefusalCounters_PreserveAndSum()
    {
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1,
            external: 1, ambiguous: 2, unresolved: 3, unsupported: 4, conditionalInactive: 5);
        var b = C(0, CodeRelationshipCoverageState.Partial, ScopeA, UniverseU1, 1, 0,
            external: 1, ambiguous: 1, unresolved: 1, unsupported: 1, conditionalInactive: 1);

        var merged = a.Merge(b);

        Assert.Equal(2, merged.External);
        Assert.Equal(3, merged.Ambiguous);
        Assert.Equal(4, merged.Unresolved);
        Assert.Equal(5, merged.Unsupported);
        Assert.Equal(6, merged.ConditionalInactive);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
    }

    [Fact]
    public void UnsupportedCategories_Sum()
    {
        var merged = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1,
                unsupportedCategories: 2)
            .Merge(C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1,
                unsupportedCategories: 3));
        Assert.Equal(5, merged.UnsupportedCategories);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
    }

    [Fact]
    public void UnavailableReasons_Sum()
    {
        var a = C(0, CodeRelationshipCoverageState.Unavailable, ScopeA, UniverseU1, 1, 0,
            unavailableReasons: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["missing-source"] = 2,
            });
        var b = C(0, CodeRelationshipCoverageState.Partial, ScopeA, UniverseU1, 1, 0,
            unavailableReasons: new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["missing-source"] = 1,
                ["no-analysis"] = 3,
            });

        var merged = a.Merge(b);

        Assert.Equal(3, merged.UnavailableReasons["missing-source"]);
        Assert.Equal(3, merged.UnavailableReasons["no-analysis"]);
    }

    // ---------------------------------------------- declared=0 boundary

    [Fact]
    public void ZeroDeclared_NoObservations_IsValid()
    {
        var empty = C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 0, 0);
        Assert.True(empty.IsComplete);

        var merged = empty.Merge(C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 0, 0));
        Assert.Equal(0, merged.DeclaredUnitCount);
        Assert.Equal(0, merged.VerifiedUnitCount);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
    }

    [Fact]
    public void MixedZeroAndNonZeroDeclared_Sum()
    {
        var merged = C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 0, 0)
            .Merge(C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 2, 2));
        Assert.Equal(2, merged.DeclaredUnitCount);
        Assert.Equal(2, merged.VerifiedUnitCount);
        Assert.Equal(1, merged.InvocationFacts);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
    }

    [Fact]
    public void VerifiedWithinDeclared_RemainsValid()
    {
        var coverage = C(0, CodeRelationshipCoverageState.Partial, ScopeA, UniverseU1, 3, 2);
        Assert.Equal(2, coverage.VerifiedUnitCount);
    }

    // ---------------------------------------------- attribution boundaries

    [Fact]
    public void UnattributedPlusAttributed_KeepsSoleAttribution()
    {
        // Legacy contributions carry no scope/universe identity. The only
        // available attribution is kept; it is not an arbitrary selection
        // between conflicting identities. Completeness still degrades through
        // the state conjunction.
        var legacy = C(0, CodeRelationshipCoverageState.NotProduced, null, null, 1, 0);
        var attributed = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1);

        var merged = legacy.Merge(attributed);

        Assert.Equal(ScopeA, merged.ScopeId);
        Assert.Equal(UniverseU1, merged.UniverseId);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
        Assert.Equal(2, merged.DeclaredUnitCount);
    }

    [Fact]
    public void UnattributedPlusUnattributed_StaysUnattributed()
    {
        var merged = C(0, CodeRelationshipCoverageState.Partial, null, null, 1, 0)
            .Merge(C(0, CodeRelationshipCoverageState.Partial, null, null, 1, 0));
        Assert.Null(merged.ScopeId);
        Assert.Null(merged.UniverseId);
        Assert.Equal(CodeRelationshipCoverageState.Partial, merged.State);
    }

    // ---------------------------------------------- F09 heuristic interaction

    [Fact]
    public void HeuristicConditionalInactive_IsCountedNeverConverted()
    {
        // F09 counts are heuristic observations: merging preserves and sums them,
        // never converts them into facts, and never lets them manufacture
        // completeness (state is driven by the analysis states alone).
        var merged = C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1,
                conditionalInactive: 2)
            .Merge(C(0, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1,
                conditionalInactive: 3));

        Assert.Equal(5, merged.ConditionalInactive);
        Assert.Equal(0, merged.InvocationFacts);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
        Assert.True(merged.IsComplete);
    }

    // ---------------------------------------------- repeated inputs boundary

    [Fact]
    public void RepeatedEquivalentInput_DoublesAccountingNeverCompleteness()
    {
        // Merge is defined for disjoint contributions. Combining a record with
        // itself is the caller's obligation outside that contract; it doubles
        // the accounting but cannot manufacture a completeness claim that the
        // inputs did not make (the state conjunction is unchanged).
        var a = C(1, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1);

        var merged = a.Merge(a);

        Assert.Equal(2, merged.InvocationFacts);
        Assert.Equal(2, merged.DeclaredUnitCount);
        Assert.Equal(2, merged.VerifiedUnitCount);
        Assert.Equal(CodeRelationshipCoverageState.Produced, merged.State);
    }

    // ---------------------------------------------- authority boundary

    [Fact]
    public void Merge_ReturnsCoverageOnly_NoFactAuthority()
    {
        // The merge API returns coverage accounting; it constructs no facts and
        // no target/usage identities. Total facts remain the sum of inputs.
        var a = C(2, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1);
        var b = C(3, CodeRelationshipCoverageState.Produced, ScopeA, UniverseU1, 1, 1);

        var merged = a.Merge(b);

        Assert.Equal(5, merged.TotalFacts);
        Assert.IsType<CodeOccurrenceCoverage>(merged);
    }
}
