using Penghou.Andirit;
using System.Reflection;

namespace Penghou.Andirit.Tests;

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
            "Penghou.Andirit.CodeBooleanProperty",
            "Penghou.Andirit.CodeDeclarationId",
            "Penghou.Andirit.CodeEdgeId",
            "Penghou.Andirit.CodeEdgeKind",
            "Penghou.Andirit.CodeEdgeKinds",
            "Penghou.Andirit.CodeEvidence",
            "Penghou.Andirit.EvidenceQuality",
            "Penghou.Andirit.EvidenceQualityExtensions",
            "Penghou.Andirit.CodeFactOrigin",
            "Penghou.Andirit.CodeGraphBatch",
            "Penghou.Andirit.CodeGraphBatchLimits",
            "Penghou.Andirit.CodeGraphBatchRejectedException",
            "Penghou.Andirit.CodeGraphCapabilities",
            "Penghou.Andirit.CodeGraphDeclaration",
            "Penghou.Andirit.CodeGraphEdge",
            "Penghou.Andirit.CodeGraphExtractionResult",
            "Penghou.Andirit.CodeGraphNode",
            "Penghou.Andirit.CodeGraphPluginContext",
            "Penghou.Andirit.CodeGraphSource",
            "Penghou.Andirit.CodeGraphSourceChange",
            "Penghou.Andirit.CodeGraphSourceChangeKind",
            "Penghou.Andirit.CodePluginFailureException",
            "Penghou.Andirit.CodePluginFailureKind",
            "Penghou.Andirit.CodeGraphValidationError",
            "Penghou.Andirit.CodeGraphValidationErrorKind",
            "Penghou.Andirit.CodeIndexRunId",
            "Penghou.Andirit.CodeIndexUnitCoverage",
            "Penghou.Andirit.CodeIndexUnitId",
            "Penghou.Andirit.CodeIntegerProperty",
            "Penghou.Andirit.CodeLocation",
            "Penghou.Andirit.CodeNodeId",
            "Penghou.Andirit.CodeNodeKind",
            "Penghou.Andirit.CodeNodeKinds",
            "Penghou.Andirit.CodeNumberProperty",
            "Penghou.Andirit.CodePluginId",
            "Penghou.Andirit.CodePropertyKeys",
            "Penghou.Andirit.CodePropertyValue",
            "Penghou.Andirit.CodeRelationshipCoverage",
            "Penghou.Andirit.CodeRelationshipCoverageState",
            "Penghou.Andirit.CodeRepositoryId",
            "Penghou.Andirit.CodeSymbolId",
            "Penghou.Andirit.CodeTextListProperty",
            "Penghou.Andirit.CodeTextProperty",
            "Penghou.Andirit.ICodeGraphExtractionSession",
            "Penghou.Andirit.ICodeGraphPlugin",
            "Penghou.Andirit.ICodeGraphSink"
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
