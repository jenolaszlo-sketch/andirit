using Andirit;

namespace Andirit.Tests;

public sealed class ContractValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(" leading")]
    [InlineData("trailing ")]
    [InlineData("line\nbreak")]
    public void Identities_RejectMalformedValues(string value)
    {
        Assert.Throws<ArgumentException>(() => new CodeNodeId(value));
    }

    [Theory]
    [InlineData("/absolute/file.cs")]
    [InlineData("../outside.cs")]
    [InlineData("src/../../outside.cs")]
    public void Location_RejectsPathsOutsideRepository(string path)
    {
        Assert.Throws<ArgumentException>(() =>
            new CodeLocation(path, 1, 1, 1, 2));
    }

    [Fact]
    public void Location_NormalizesDirectorySeparators()
    {
        var location = new CodeLocation(
            "src\\Example.cs",
            1,
            1,
            2,
            1);

        Assert.Equal("src/Example.cs", location.Path);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NumberProperty_RejectsNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CodeNumberProperty(value));
    }

    [Fact]
    public void Node_RejectsNullPropertyValues()
    {
        var properties = new Dictionary<string, CodePropertyValue>
        {
            ["invalid"] = null!
        };

        Assert.Throws<ArgumentException>(() =>
            new CodeGraphNode(
                new CodeNodeId("node:one"),
                CodeNodeKinds.Type,
                "Example",
                properties: properties));
    }

    [Fact]
    public void Evidence_RejectsConfidenceForDeterministicFacts()
    {
        Assert.Throws<ArgumentException>(() =>
            new CodeEvidence(
                EvidenceQuality.Resolved,
                "roslyn",
                confidence: 0.99));
    }

    [Fact]
    public void Evidence_AllowsBoundedConfidenceForInferredFacts()
    {
        var evidence = new CodeEvidence(
            EvidenceQuality.Inferred,
            "hetu-test",
            confidence: 0.75);

        Assert.Equal(0.75, evidence.Confidence);
    }

    [Fact]
    public void Evidence_RejectsConfidenceForDerivedFacts()
    {
        Assert.Throws<ArgumentException>(() =>
            new CodeEvidence(
                EvidenceQuality.Derived,
                "hetu-test",
                confidence: 0.5));
    }

    [Fact]
    public void CallsEdge_WithStructuralEvidence_FailsValidation()
    {
        var edge = new CodeGraphEdge(
            new CodeEdgeId("edge:calls:one"),
            new CodeNodeId("node:caller"),
            new CodeNodeId("node:callee"),
            CodeEdgeKinds.Calls,
            new CodeEvidence(EvidenceQuality.Structural, "tree-sitter"));
        var batch = new CodeGraphBatch(
            new CodeFactOrigin(
                new CodeRepositoryId("repo:test"),
                new CodePluginId("plugin:test"),
                "1.0.0",
                new CodeIndexRunId("run:one"),
                new CodeIndexUnitId("unit:one")),
            edges: [edge]);

        var errors = new CodeGraphBatchValidator().Validate(batch, new CodeGraphBatchLimits());

        Assert.Contains(errors, error => error.Code == "edge.evidence.calls-requires-resolved");
    }

    [Fact]
    public void CallsEdge_WithResolvedEvidence_PassesValidation()
    {
        var edge = new CodeGraphEdge(
            new CodeEdgeId("edge:calls:one"),
            new CodeNodeId("node:caller"),
            new CodeNodeId("node:callee"),
            CodeEdgeKinds.Calls,
            new CodeEvidence(EvidenceQuality.Resolved, "roslyn"));
        var batch = new CodeGraphBatch(
            new CodeFactOrigin(
                new CodeRepositoryId("repo:test"),
                new CodePluginId("plugin:test"),
                "1.0.0",
                new CodeIndexRunId("run:one"),
                new CodeIndexUnitId("unit:one")),
            edges: [edge]);

        var errors = new CodeGraphBatchValidator().Validate(batch, new CodeGraphBatchLimits());

        Assert.DoesNotContain(errors, error => error.Code == "edge.evidence.calls-requires-resolved");
    }

    [Fact]
    public void EvidenceQuality_IsDeterministicOnlyForNonProbabilisticQualities()
    {
        Assert.True(EvidenceQuality.Structural.IsDeterministic());
        Assert.True(EvidenceQuality.Resolved.IsDeterministic());
        Assert.True(EvidenceQuality.Derived.IsDeterministic());
        Assert.False(EvidenceQuality.Inferred.IsDeterministic());
    }

    [Fact]
    public void EvidenceQuality_NumericValuesArePersistenceCompatibilityOnly()
    {
        // Numeric values preserve the previous Syntax/Semantic/Heuristic/
        // AiInferred encoding so persisted payloads keep decoding. They are
        // not strength levels: Derived (2) is not stronger than Resolved (1).
        // Consumers must use IsDeterministic or set membership, never
        // ordinal comparison.
        Assert.Equal(0, (int)EvidenceQuality.Structural);
        Assert.Equal(1, (int)EvidenceQuality.Resolved);
        Assert.Equal(2, (int)EvidenceQuality.Derived);
        Assert.Equal(3, (int)EvidenceQuality.Inferred);
    }

    [Fact]
    public void Origin_RequiresSourcePathAndHashTogether()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateOrigin(sourcePath: "src/Example.cs"));
    }

    [Fact]
    public void BatchLimits_MustBePositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new CodeGraphBatchLimits(maxEdges: 0));
    }

    [Fact]
    public void Kinds_AllowNamespacedConsumerExtensions()
    {
        var customNodeKind = new CodeNodeKind("solo:test-fixture");
        var customEdgeKind = new CodeEdgeKind("solo:covered-by-test");

        Assert.Equal("solo:test-fixture", customNodeKind.Value);
        Assert.Equal("solo:covered-by-test", customEdgeKind.Value);
    }

    private static CodeFactOrigin CreateOrigin(
        string? sourcePath = null,
        string? sourceHash = null) =>
        new(
            new CodeRepositoryId("repo:test"),
            new CodePluginId("plugin:test"),
            "1.0.0",
            new CodeIndexRunId("run:one"),
            new CodeIndexUnitId("unit:one"),
            sourcePath,
            sourceHash);
}
