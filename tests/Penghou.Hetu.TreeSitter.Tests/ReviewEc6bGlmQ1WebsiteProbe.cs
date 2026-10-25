namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q1 review probe: real-repository qualification against
// an isolated snapshot of jenolaszlo-sketch/website@f9cca177. Inert unless
// EC6B_WEBSITE_SNAPSHOT points at the snapshot directory.
public sealed class ReviewEc6bGlmQ1WebsiteProbe(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task Probe15_WebsiteResolvesExactlyTheIndependentlyEstablishedReferences()
    {
        var snapshot = Environment.GetEnvironmentVariable("EC6B_WEBSITE_SNAPSHOT");
        if (string.IsNullOrWhiteSpace(snapshot) || !Directory.Exists(snapshot))
            return;

        var repositoryId = new CodeRepositoryId("repo:q1-website");
        var databasePath = Path.Combine(snapshot, "..", "q1-website-graph.db");
        using var store = new LatticeCodeGraphStore(databasePath);
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, snapshot),
            new CodeIndexRunId("run:q1-website"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

        var units = await store.GetPublishedUnitsAsync(repositoryId);
        var nodes = units.SelectMany(unit => unit.Nodes).ToArray();
        var declarations = units.SelectMany(unit => unit.Declarations).ToArray();
        var edges = units.SelectMany(unit => unit.Edges).ToArray();

        var nodesById = nodes.GroupBy(node => node.Id.Value).ToArray();
        // Placeholder nodes (invoked names) are multi-origin: the same id is
        // contributed by several file units and merges into one node.
        output.WriteLine($"nodes={nodesById.Length} declarations={declarations.Length} edges={edges.Length}");
        foreach (var group in edges.GroupBy(edge => edge.Kind.Value).OrderBy(group => group.Key, StringComparer.Ordinal))
            output.WriteLine($"kind {group.Key}={group.Count()}");

        // EC-5 qualified baseline must be preserved.
        Assert.Equal(128, nodesById.Length);
        Assert.Equal(83, declarations.Length);
        Assert.Equal(277, edges.Count(edge => edge.Kind != CodeEdgeKinds.ResolvesTo));
        Assert.Equal(9, edges.Count(edge => edge.Kind == CodeEdgeKinds.Imports));
        Assert.Equal(1, edges.Count(edge => edge.Kind == CodeEdgeKinds.Inherits));
        Assert.Equal(101, edges.Count(edge => edge.Kind == CodeEdgeKinds.InvokesName));

        // Independent ground truth: exactly three supported relative references.
        var expected = new (string Source, string Target, string Specifier)[]
        {
            ("src/lib/schema.ts", "src/lib/site.ts", "./site"),
            ("src/lib/schema.ts", "src/lib/paths.ts", "./paths"),
            ("src/pages/robots.txt.ts", "src/lib/paths.ts", "../lib/paths")
        };
        var moduleEdges = edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(3, moduleEdges.Length);
        foreach (var moduleEdge in moduleEdges)
        {
            output.WriteLine(
                $"resolves-to {PathOf(nodes, moduleEdge.SourceId)} -> {PathOf(nodes, moduleEdge.TargetId)} " +
                $"specifier={Text(moduleEdge, CodePropertyKeys.Specifier)} " +
                $"rule={Text(moduleEdge, CodePropertyKeys.ResolutionRule)} " +
                $"location={moduleEdge.Evidence.Location}");
            var match = Assert.Single(expected, value =>
                PathOf(nodes, moduleEdge.SourceId) == value.Source &&
                PathOf(nodes, moduleEdge.TargetId) == value.Target &&
                Text(moduleEdge, CodePropertyKeys.Specifier) == value.Specifier);
            Assert.Equal("extension-substitution", Text(moduleEdge, CodePropertyKeys.ResolutionRule));
            Assert.Equal("relative-v1", Text(moduleEdge, CodePropertyKeys.ResolutionPolicy));
            Assert.Equal("import", Text(moduleEdge, CodePropertyKeys.ModuleReference));
            Assert.Equal(EvidenceQuality.Resolved, moduleEdge.Evidence.Quality);
            Assert.Equal("penghou.hetu.tree-sitter.ecmascript-module-resolver", moduleEdge.Evidence.Provider);
            Assert.EndsWith("+resolver1.relative-v1", moduleEdge.Evidence.ProviderVersion, StringComparison.Ordinal);
        }

        // Unsupported/external references never fabricate an edge.
        Assert.All(moduleEdges, edge => Assert.Equal(CodeNodeKinds.File,
            nodes.Single(node => node.Id == edge.SourceId).Kind));
        Assert.All(moduleEdges, edge => Assert.Equal(CodeNodeKinds.File,
            nodes.Single(node => node.Id == edge.TargetId).Kind));

        // Consumer query: which indexed source file does the reference resolve to?
        var queries = new CodeGraphQueryService(store);
        var schemaNode = nodes.Single(node => node.QualifiedName == "src/lib/schema.ts").Id;
        var neighborhood = await queries.GetNeighborhoodAsync(
            repositoryId,
            schemaNode,
            [CodeEdgeKinds.ResolvesTo],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        Assert.Equal(2, neighborhood.Edges.Count);
        Assert.All(neighborhood.Edges, edge => Assert.Equal(schemaNode, edge.SourceId));

        // The import-binding question must remain unsupported in EC-6B.
        Assert.DoesNotContain(edges, edge => edge.Kind.Value is "binds" or "exports");

        // Record a compact, normalized snapshot for cross-run comparison.
        var evidencePath = Environment.GetEnvironmentVariable("EC6B_EVIDENCE_PATH");
        if (!string.IsNullOrWhiteSpace(evidencePath))
        {
            Directory.CreateDirectory(evidencePath);
            await File.WriteAllLinesAsync(
                Path.Combine(evidencePath, "website-p7-snapshot.txt"),
                Snapshot(units));
        }
    }

    private static IReadOnlyList<string> Snapshot(IReadOnlyList<CodeIndexUnitReplacement> units) =>
        units
            .SelectMany(unit => unit.Nodes.Select(node =>
                    $"NODE {node.Id.Value} {node.Kind.Value} {node.QualifiedName}"))
            .Concat(units.SelectMany(unit => unit.Declarations.Select(declaration =>
                $"DECL {declaration.Id.Value} {declaration.SymbolNodeId.Value}")))
            .Concat(units.SelectMany(unit => unit.Edges.Select(edge =>
                $"EDGE {edge.Id.Value} {edge.Kind.Value} {edge.SourceId.Value} {edge.TargetId.Value}")))
            .OrderBy(line => line, StringComparer.Ordinal)
            .ToArray();

    private static string PathOf(IReadOnlyList<CodeGraphNode> nodes, CodeNodeId id) =>
        nodes.Single(node => node.Id == id).QualifiedName!;

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;
}
