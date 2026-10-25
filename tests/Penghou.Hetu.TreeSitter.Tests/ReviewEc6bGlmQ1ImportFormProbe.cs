namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q1 review probe: import/export form classification,
// especially TypeScript import-equals and dynamic forms that must never
// produce a resolved edge.
public sealed class ReviewEc6bGlmQ1ImportFormProbe
{
    [Fact]
    public async Task Probe17_ImportAndExportFormsFollowTheClassificationTable()
    {
        const string source = """
            import item = require('./item');
            import spaced = require ('./item');
            import type Typed = require('./item');
            export = item;
            import type { T } from './item';
            import { type T2 } from './item';
            const required = require('./item');
            const template = require(`./item`);
            const dynamic = import('./item');
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/item.ts", "export const item = true;\nexport type T = string;\nexport type T2 = string;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(3, edges.Length);
        Assert.DoesNotContain(edges, edge => edge.Evidence.Location!.StartLine is 1 or 2 or 3 or 4);
        Assert.Contains(edges, edge =>
            edge.Evidence.Location!.StartLine == 5 &&
            Text(edge, CodePropertyKeys.ModuleReference) == "import-type" &&
            IsTypeOnly(edge));
        Assert.Contains(edges, edge =>
            edge.Evidence.Location!.StartLine == 6 &&
            Text(edge, CodePropertyKeys.ModuleReference) == "import" &&
            IsTypeOnly(edge));
        Assert.Contains(edges, edge =>
            edge.Evidence.Location!.StartLine == 7 &&
            Text(edge, CodePropertyKeys.ModuleReference) == "require");

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(3, coverage.EdgesEmitted);
        Assert.Equal(6, coverage.UnsupportedTargets);
        Assert.Equal(0, coverage.AmbiguousTargets);
        Assert.Equal(0, coverage.ExternalTargets);
    }

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static bool IsTypeOnly(CodeGraphEdge edge) =>
        edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
        value is CodeBooleanProperty { Value: true };

    [Fact]
    public async Task Probe17b_ExportEqualsWithLiteralRequireDoesNotFabricateAnExportFact()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/legacy.ts", "export = require('./item');"),
            ("src/item.ts", "export const item = true;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        // Observation probe (review finding EC6B-GLM-F02): the export= form is
        // rejected, but a nested literal require is captured as its own site.
        Assert.Equal(1, coverage.UnsupportedTargets);
        Assert.All(edges, edge => Assert.Equal(
            "require", Text(edge, CodePropertyKeys.ModuleReference)));
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind.Value is "exports" or "binds");
    }
}
