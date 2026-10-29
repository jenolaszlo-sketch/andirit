using Andirit;
using System.Reflection;

namespace Andirit.Tests;

public sealed class PublicApiContractTests
{
    [Fact]
    public void Abstractions_PublicTypeSnapshot_IsIntentional()
    {
        var actual = typeof(CodeGraphNode).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expected = new[]
        {
            "Andirit.CodeBooleanProperty",
            "Andirit.CodeDeclarationId",
            "Andirit.CodeEdgeId",
            "Andirit.CodeEdgeKind",
            "Andirit.CodeEdgeKinds",
            "Andirit.CodeEvidence",
            "Andirit.EvidenceQuality",
            "Andirit.EvidenceQualityExtensions",
            "Andirit.CodeFactOrigin",
            "Andirit.CodeGraphBatch",
            "Andirit.CodeGraphBatchLimits",
            "Andirit.CodeGraphBatchRejectedException",
            "Andirit.CodeGraphCapabilities",
            "Andirit.CodeGraphDeclaration",
            "Andirit.CodeGraphEdge",
            "Andirit.CodeGraphExtractionResult",
            "Andirit.CodeGraphNode",
            "Andirit.CodeGraphPluginContext",
            "Andirit.CodeGraphSource",
            "Andirit.CodeGraphSourceChange",
            "Andirit.CodeGraphSourceChangeKind",
            "Andirit.CodePluginFailureException",
            "Andirit.CodePluginFailureKind",
            "Andirit.CodeGraphValidationError",
            "Andirit.CodeGraphValidationErrorKind",
            "Andirit.CodeIndexRunId",
            "Andirit.CodeIndexUnitCoverage",
            "Andirit.CodeIndexUnitId",
            "Andirit.CodeIntegerProperty",
            "Andirit.CodeLocation",
            "Andirit.CodeNodeId",
            "Andirit.CodeNodeKind",
            "Andirit.CodeNodeKinds",
            "Andirit.CodeNumberProperty",
            "Andirit.CodePluginId",
            "Andirit.CodePropertyKeys",
            "Andirit.CodePropertyValue",
            "Andirit.CodeRelationshipCoverage",
            "Andirit.CodeRelationshipCoverageState",
            "Andirit.CodeRepositoryId",
            "Andirit.CodeSymbolId",
            "Andirit.CodeTextListProperty",
            "Andirit.CodeTextProperty",
            "Andirit.ICodeGraphExtractionSession",
            "Andirit.ICodeGraphPlugin",
            "Andirit.ICodeGraphSink"
        }.Order(StringComparer.Ordinal).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Abstractions_DoNotReferenceParserDatabaseOrAiFrameworks()
    {
        var references = typeof(CodeGraphNode).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .ToArray();
        var forbiddenPrefixes = new[]
        {
            "Microsoft.CodeAnalysis",
            "Antlr",
            "Lattice",
            "Kuzu",
            "Microsoft.Extensions.AI"
        };

        Assert.DoesNotContain(
            references,
            reference => forbiddenPrefixes.Any(prefix =>
                reference.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void PluginContract_DoesNotExposeParserSpecificTypes()
    {
        var members = typeof(ICodeGraphPlugin)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Select(member => member.ToString() ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(
            members,
            member => member.Contains("Roslyn", StringComparison.OrdinalIgnoreCase) ||
                member.Contains("SyntaxTree", StringComparison.OrdinalIgnoreCase));
    }
}
