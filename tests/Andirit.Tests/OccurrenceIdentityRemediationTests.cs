using System.Security.Cryptography;
using System.Text;
using Andirit;

namespace Andirit.Tests;

/// <summary>
/// REM-01 identity regressions: the production helpers must reproduce the
/// accepted R2 usage4 tuple, not a weaker local hash.
/// </summary>
public sealed class OccurrenceIdentityRemediationTests
{
    private static readonly CodeRepositoryId Repository = new("experiment:andirit-31c33a9");
    private static readonly CodePluginId Plugin = new("experiment.csharp.occurrences");
    private static readonly CodeOccurrenceTargetId FrozenTarget = new(
        "asmctx3:sha256:632e7953365d1dbadbb55414b7cbcb991c9bc2653ec6cafb6f6d9d95a9c2032d");

    [Fact]
    public void Usage4_MatchesTheFrozenR2Vector()
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
            "bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af",
            id.Value);
    }

    [Fact]
    public void Usage4_DistinguishesDefiningCompilationContexts()
    {
        var otherTarget = new CodeOccurrenceTargetId(
            "asmctx3:sha256:fb3e0561272eb342a549036ecf0095c2ec2a6d5d7bc159fc943dee11fe669c26");
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
            "b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca",
            second.Value);
    }

    [Fact]
    public void Usage4_IncludesSemanticUnitAndSourceHash()
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
    public void Target_UsesTheAcceptedAsmctx3ComponentsAndCanonicalFormat()
    {
        var context = "compctx1:sha256:" + new string('a', 64);
        var first = CodeOccurrenceIdentity.ComputeTargetId(
            context, "N.C|0.0.0.0||", "N.C.dll", "identifier-a", "declaration-a", "shape-a");
        var second = CodeOccurrenceIdentity.ComputeTargetId(
            context, "N.C|0.0.0.0||", "N.C.dll", "identifier-a", "declaration-a", "shape-b");

        Assert.True(CodeOccurrenceIdentity.IsCanonicalTarget(first.Value));
        Assert.StartsWith("asmctx3:sha256:", first.Value, StringComparison.Ordinal);
        Assert.NotEqual(first.Value, second.Value);
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx:not-canonical", "meta", "module", "id", "decl", "shape"));
    }

    [Fact]
    public void Target_RejectsUnavailableAndFabricatedIdentities()
    {
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceTargetId("asmctx3:unavailable"));
        Assert.Throws<ArgumentException>(() => new CodeOccurrenceTargetId("asmctx3:sha256:abc"));
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
