namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q1 review probes: raw-specifier capture classification,
// CRLF identity stability, and physically present but unindexed targets.
public sealed class ReviewEc6bGlmQ1CaptureProbe
{
    [Fact]
    public async Task Probe18_RejectedPathFormsNeverResolveAtCaptureLevel()
    {
        // Escape-sequence handling is pinned separately in Probe21.
        const string importer = """
            import './x?raw';
            import './y#frag';
            import './a//b';
            import '/abs/x';
            import './a/';
            import './';
            import './a/./b';
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/a/b.ts", "export const b = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        var edge = Assert.Single(edges);
        Assert.Equal("./a/./b", Assert.IsType<CodeTextProperty>(
            edge.Properties[CodePropertyKeys.Specifier]).Value);
        Assert.Equal(
            "src/a/b.ts",
            extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName);

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(7, coverage.Candidates);
        Assert.Equal(6, coverage.UnsupportedTargets);
        Assert.Equal(0, coverage.UnresolvedTargets);
        Assert.Equal(0, coverage.ExternalTargets);
        Assert.Equal(0, coverage.AmbiguousTargets);
    }

    [Fact]
    public async Task Probe19_CrlfSourceKeepsIdenticalResolverIdentities()
    {
        const string lf = "import './a';\nconst s = \"café 中文\";\nexport const p = 1;";
        var lfExtracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", lf),
            ("src/a.ts", "export const a = 1;"));
        var crlfExtracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", lf.Replace("\n", "\r\n", StringComparison.Ordinal)),
            ("src/a.ts", "export const a = 1;"));

        var lfEdge = Assert.Single(lfExtracted.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        var crlfEdge = Assert.Single(crlfExtracted.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(lfEdge.Id, crlfEdge.Id);
        Assert.Equal(lfEdge.Evidence.Location, crlfEdge.Evidence.Location);
    }

    [Fact]
    public async Task Probe20_PresentButUnindexedFilesStayOutsideTheInventory()
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-unindexed-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(
            Path.Combine(source.FullName, "page.ts"),
            "import './config.json';\nimport './helper.astro';\nimport './present';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "config.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "helper.astro"), "<h1>hi</h1>");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "present.tsx"), "export const p = 1;");

        var repositoryId = new CodeRepositoryId("repo:q1-unindexed");
        using var storeScope = ReviewStore.Create(false, "");
        var store = storeScope.Store;
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            new CodeIndexRunId("run:q1-unindexed"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

        var units = await store.GetPublishedUnitsAsync(repositoryId);
        var edges = units.SelectMany(unit => unit.Edges).ToArray();
        var moduleEdge = Assert.Single(edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal("./present", Assert.IsType<CodeTextProperty>(
            moduleEdge.Properties[CodePropertyKeys.Specifier]).Value);
        Assert.Equal("src/present.tsx",
            units.SelectMany(unit => unit.Nodes).Single(node => node.Id == moduleEdge.TargetId).QualifiedName);

        var plugin = Assert.Single(result.Diagnostics.Plugins);
        Assert.Equal(0, plugin.UnresolvedRelationships);
        Assert.Null(await store.GetNodeAsync(repositoryId, ReviewStore.FileNodeId("typescript", "src/config.json")));
        Assert.Null(await store.GetNodeAsync(repositoryId, ReviewStore.FileNodeId("typescript", "src/helper.astro")));
    }

    [Fact]
    public async Task Probe21_EscapedSpecifiersAreTruncatedAndProduceFalseResolvedEdges()
    {
        // EC6B-GLM-F02 reproduction. Accepted contract (architecture §6.2,
        // §6.2.2): the exact written specifier text is preserved and any
        // backslash in the raw text is Unsupported with no edge. Observed:
        // capture keeps only the first string_fragment, so the written text
        // after an escape sequence is dropped AND the backslash rejection is
        // bypassed. Both statements below emit false Resolved edges.
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", "import './a\\b';\nimport './caf\\u00e9';"),
            ("src/a.ts", "export const a = 1;"),
            ("src/caf.ts", "export const caf = 1;"),
            ("src/caf\u00e9.ts", "export const cafe = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(2, edges.Length);
        var byTarget = edges.ToDictionary(
            edge => extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName!,
            edge => Assert.IsType<CodeTextProperty>(edge.Properties[CodePropertyKeys.Specifier]).Value,
            StringComparer.Ordinal);
        Assert.Equal("./a", byTarget["src/a.ts"]);
        Assert.Equal("./caf", byTarget["src/caf.ts"]);

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(2, coverage.EdgesEmitted);
        Assert.Equal(0, coverage.UnsupportedTargets);
    }

    [Fact]
    public async Task Probe22_BindingNamedTypeIsNotExplicitTypeOnlySyntax()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", "import { type } from './m';\nimport { type as t } from './m';\nexport { type as u } from './m';"),
            ("src/m.ts", "export const type = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        foreach (var edge in edges)
        {
            var moduleReference = Assert.IsType<CodeTextProperty>(
                edge.Properties[CodePropertyKeys.ModuleReference]).Value;
            var typeOnly = edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
                value is CodeBooleanProperty { Value: true };
            Console.WriteLine($"binding-type line={edge.Evidence.Location!.StartLine} ref={moduleReference} typeOnly={typeOnly}");
        }
        Assert.Equal(3, edges.Length);
    }
}
