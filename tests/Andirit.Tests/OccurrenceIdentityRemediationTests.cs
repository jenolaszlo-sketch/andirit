using System.Security.Cryptography;
using System.Text;
using Andirit;

namespace Andirit.Tests;

/// <summary>
/// REM-01 identity regressions: the production helpers must reproduce the
/// successor R2 usage5 tuple, not a weaker local hash.
/// </summary>
public sealed class OccurrenceIdentityRemediationTests
{
    private static readonly CodeRepositoryId Repository = new("experiment:andirit-31c33a9");
    private static readonly CodePluginId Plugin = new("experiment.csharp.occurrences");
    private static readonly CodeOccurrenceTargetId FrozenTarget = new(
        "asmctx4:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d");

    [Fact]
    public void Usage5_MatchesTheFrozenR2Vector()
    {
        var id = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository,
            Plugin,
            "caller-unit",
            "Caller.cs",
            "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
            new CodeLocation("Caller.cs", 1, 72, 1, 77),
            "M:N.C.M",
            FrozenTarget,
            CodeOccurrenceCategory.Invocation);

        Assert.Equal(
            "4b0b01b1f20add7a59e9ca8ac04a220f932dfc0deacc0042a02efdfa3596dc48",
            id.Value);
    }

    [Fact]
    public void Usage5_DistinguishesDefiningCompilationContexts()
    {
        var otherTarget = new CodeOccurrenceTargetId(
            "asmctx4:sha256:fb3e0561272eb342a549036ecf0095c2ec2a6d5d7bc159fc943dee11fe669c26");
        var span = new CodeLocation("Caller.cs", 1, 72, 1, 77);
        var first = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "caller-unit", "Caller.cs",
            "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
            span, "M:N.C.M", FrozenTarget, CodeOccurrenceCategory.Invocation);
        var second = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "caller-unit", "Caller.cs",
            "sha256:2f7803daa5910bf2f5fb5927b2e9f999ebd1b7074340096f620df37278df484a",
            span, "M:N.C.M", otherTarget, CodeOccurrenceCategory.Invocation);

        Assert.NotEqual(first.Value, second.Value);
        Assert.Equal(
            "c8ef6e665aafadb72d1d0f47d4de52967fd050d74af9e86f4ff8bdad095453cb",
            second.Value);
    }

    [Fact]
    public void Usage5_IncludesSemanticUnitAndSourceHash()
    {
        var span = new CodeLocation("Caller.cs", 1, 72, 1, 77);
        var baseline = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "unit:a", "Caller.cs", "sha256:source-a",
            span, "M:N.C.M", FrozenTarget, CodeOccurrenceCategory.Invocation);
        var otherUnit = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "unit:b", "Caller.cs", "sha256:source-a",
            span, "M:N.C.M", FrozenTarget, CodeOccurrenceCategory.Invocation);
        var otherSource = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "unit:a", "Caller.cs", "sha256:source-b",
            span, "M:N.C.M", FrozenTarget, CodeOccurrenceCategory.Invocation);

        Assert.NotEqual(baseline.Value, otherUnit.Value);
        Assert.NotEqual(baseline.Value, otherSource.Value);
    }

    [Fact]
    public void Target_UsesTheSuccessorAsmctx4ComponentsAndCanonicalFormat()
    {
        var context = "compctx4:sha256:" + new string('a', 64);
        var first = CodeOccurrenceIdentity.ComputeTargetId(
            context, "N.C|0.0.0.0||", "N.C.dll", "identifier-a", "declaration-a", "shape-a");
        var second = CodeOccurrenceIdentity.ComputeTargetId(
            context, "N.C|0.0.0.0||", "N.C.dll", "identifier-a", "declaration-a", "shape-b");

        Assert.True(CodeOccurrenceIdentity.IsCanonicalTarget(first.Value));
        Assert.StartsWith("asmctx4:sha256:", first.Value, StringComparison.Ordinal);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx:not-canonical", "meta", "module", "id", "decl", "shape"));
    }

    [Fact]
    public void Target_RejectsUnavailableAndFabricatedIdentities()
    {
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceTargetId("asmctx4:unavailable"));
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceTargetId("asmctx4:sha256:abc"));
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceId(new string('A', 64)));
    }

    [Fact]
    public void Fact_RejectsANonCanonicalOccurrenceIdentity()
    {
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceId("not-an-identity"));
    }

    [Fact]
    public void Fact_KeepsIdentityStableAndPayloadSensitiveToNonIdentityContent()
    {
        var contributor = new CodeOccurrenceContributor(Plugin, "caller-unit");
        var span = new CodeLocation("Caller.cs", 1, 72, 1, 77);
        var first = CodeOccurrenceFact.Create(
            Repository, FrozenTarget, "M:N.C.M", "caller-unit", contributor,
            CodeOccurrenceCategory.Invocation, span, "sha256:source", "content:a");
        var second = CodeOccurrenceFact.Create(
            Repository, FrozenTarget, "M:N.C.M", "caller-unit", contributor,
            CodeOccurrenceCategory.Invocation, span, "sha256:source", "content:b");

        Assert.Equal(first.Id, second.Id);
        Assert.NotEqual(first.PayloadHash, second.PayloadHash);
    }

    [Fact]
    public void Fact_RejectsAnIdentityThatDoesNotMatchTheTuple()
    {
        var contributor = new CodeOccurrenceContributor(Plugin, "caller-unit");
        var span = new CodeLocation("Caller.cs", 1, 72, 1, 77);
        var wrong = new CodeOccurrenceId(new string('d', 64));
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceFact(
            wrong,
            FrozenTarget,
            "M:N.C.M",
            Repository,
            CodeOccurrenceCategory.Invocation,
            "caller-unit",
            contributor,
            span,
            "sha256:source",
            "content:a"));
    }

    [Fact]
    public void Coverage_DistinguishesNoAnalysisIncompleteAndCompleteEmpty()
    {
        var none = new CodeOccurrenceCoverage();
        var incomplete = new CodeOccurrenceCoverage(
            0, 0, CodeRelationshipCoverageState.Partial);
        var completeEmpty = new CodeOccurrenceCoverage(
            0, 0, CodeRelationshipCoverageState.Produced);

        Assert.False(none.AnalysisPerformed);
        Assert.False(incomplete.IsComplete);
        Assert.True(incomplete.AnalysisPerformed);
        Assert.True(completeEmpty.IsComplete);
        Assert.True(completeEmpty.AnalysisPerformed);
        Assert.Equal(0, completeEmpty.TotalFacts);
    }
}

