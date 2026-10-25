namespace Penghou.Hetu.TreeSitter.Tests;

// REVIEW-ONLY (EC6-GLM-AR1): disposable probes executed to support the
// independent architecture review. They exercise existing runtime contracts
// only; no production code is modified. Remove with the review branch.
public sealed class ReviewEc6GlmAr1Probes
{
    private static readonly CodeRepositoryId Repo = new("repo:ar1");
    private static readonly CodePluginId Plugin = new("plugin:ar1");

    // P1 (F2/F3, architecture §9.1): a resolution unit staged after the file
    // units of the same run can target their nodes; staged before them the
    // staging itself is rejected. Staging order is honored by the store.
    [Fact]
    public async Task Probe_CrossUnitEdgeStagingIsOrderSensitive()
    {
        var file = Node("file:a.ts");
        var target = Node("file:b.ts");
        var edge = Edge("edge:resolves", file.Id, target.Id);

        var ordered = await SeededStoreAsync([
            Unit("typescript:file:a", [file], []),
            Unit("typescript:file:b", [target], []),
            Unit("typescript:modules", [], [edge]),
        ]);
        Assert.NotNull(await ordered.GetNodeAsync(Repo, file.Id));
        Assert.Equal(1, await CountEdgesFrom(ordered, file.Id));

        var reversed = await EmptyStoreAsync();
        var (run, _) = await BeginRunAsync(reversed);
        var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(() =>
            reversed.StageIndexUnitAsync(WithRun(
                Unit("typescript:modules", [], [edge]), run)).AsTask());
        Assert.Contains(
            rejection.Errors,
            error => error.Kind == CodeGraphValidationErrorKind.MissingEndpoint);
    }

    // P2 (F9, AR1-08): replacing the resolution unit with an empty
    // replacement removes its previously published edges while other units
    // survive.
    [Fact]
    public async Task Probe_ResolutionUnitReplacementRemovesPriorEdges()
    {
        var file = Node("file:a.ts");
        var target = Node("file:b.ts");
        var store = await SeededStoreAsync([
            Unit("typescript:file:a", [file], []),
            Unit("typescript:file:b", [target], []),
            Unit("typescript:modules", [], [Edge("edge:r1", file.Id, target.Id)]),
        ]);
        Assert.Equal(1, await CountEdgesFrom(store, file.Id));

        await RerunAsync(store, [Unit("typescript:modules", [], [])]);
        Assert.Equal(0, await CountEdgesFrom(store, file.Id));
        Assert.NotNull(await store.GetNodeAsync(Repo, target.Id));
    }

    // P3 (F2): deleting a target file unit succeeds only after the
    // referencing resolution unit has been replaced in the same run;
    // deleting it first is rejected. This pins the ordering requirement the
    // lifecycle already implements (plugin execution, then deletions).
    [Fact]
    public async Task Probe_TargetDeletionRequiresPriorReferenceRemoval()
    {
        var file = Node("file:a.ts");
        var target = Node("file:b.ts");

        var correctOrder = await SeededStoreAsync([
            Unit("typescript:file:a", [file], []),
            Unit("typescript:file:b", [target], []),
            Unit("typescript:modules", [], [Edge("edge:r1", file.Id, target.Id)]),
        ]);
        await RerunAsync(
            correctOrder,
            [Unit("typescript:modules", [], [])],
            deleteUnits: ["typescript:file:b"]);
        Assert.Null(await correctOrder.GetNodeAsync(Repo, target.Id));
        Assert.Equal(0, await CountEdgesFrom(correctOrder, file.Id));

        var wrongOrder = await SeededStoreAsync([
            Unit("typescript:file:a", [file], []),
            Unit("typescript:file:b", [target], []),
            Unit("typescript:modules", [], [Edge("edge:r1", file.Id, target.Id)]),
        ]);
        var (run, _) = await BeginRunAsync(wrongOrder);
        var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(() =>
            wrongOrder.StageIndexUnitDeletionAsync(Repo, run, Plugin, new("typescript:file:b")).AsTask());
        Assert.Contains(
            rejection.Errors,
            error => error.Kind == CodeGraphValidationErrorKind.MissingEndpoint);
    }

    // P4 (AR1-01/02): logically distinct relationships between the same
    // endpoints coexist when identity differs (discriminator + span); the
    // same identity contributed by two different units with different
    // properties is a conflict.
    [Fact]
    public async Task Probe_EdgeMultiplicityRequiresDistinctIdentity()
    {
        var source = Node("file:a.ts");
        var target = Node("file:b.ts");
        var store = await SeededStoreAsync([
            Unit("typescript:file:a", [source], []),
            Unit("typescript:file:b", [target], []),
            Unit("typescript:modules", [],
            [
                Edge("edge:alias-1", source.Id, target.Id, NameProperty("first")),
                Edge("edge:alias-2", source.Id, target.Id, NameProperty("second")),
            ]),
        ]);

        var traversal = await store.TraverseAsync(Repo, new CodeGraphTraversalQuery(
            source.Id, CodeGraphDirection.Outgoing, maxNodes: 10, maxEdges: 10));
        Assert.Equal(2, traversal.Edges.Count);
        Assert.Contains(traversal.Edges, edge =>
            edge.Properties["exported-name"] is CodeTextProperty { Value: "first" });
        Assert.Contains(traversal.Edges, edge =>
            edge.Properties["exported-name"] is CodeTextProperty { Value: "second" });

        var (run, _) = await BeginRunAsync(store);
        var rejection = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(() =>
            store.StageIndexUnitAsync(WithRun(
                Unit("typescript:modules-2", [],
                    [Edge("edge:alias-1", source.Id, target.Id, NameProperty("changed"))]),
                run)).AsTask());
        Assert.Contains(
            rejection.Errors,
            error => error.Code == "graph.edge.conflict");
    }

    // P5 (AR1-03, Review Area D): the extractor's real identity behavior.
    // Name-based symbol kinds converge across files; path-scoped kinds and
    // dual projections do not.
    [Fact]
    public async Task Probe_ExtractorIdentityScopesMatchTheDesignMatrix()
    {
        var functions = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "export function run() {}"),
            ("src/b.ts", "export function run() {}"));
        var functionNodes = SymbolNodes(functions, CodeNodeKinds.Callable, "run");
        Assert.Equal(2, functionNodes.Length);
        Assert.Single(functionNodes.Select(node => node.Id).Distinct());

        var variables = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "export const run = 1;"),
            ("src/b.ts", "export const run = 2;"));
        var variableNodes = SymbolNodes(variables, CodeNodeKinds.Field, "run");
        Assert.Equal(2, variableNodes.Length);
        Assert.Equal(2, variableNodes.Select(node => node.Id).Distinct().Count());

        var projection = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "const handler = () => 1;"));
        var handlerNodes = SymbolNodes(projection, null, "handler");
        Assert.Equal(2, handlerNodes.Length);
        Assert.Equal(2, handlerNodes.Select(node => node.Id).Distinct().Count());
        Assert.Contains(handlerNodes, node => node.Kind == CodeNodeKinds.Field);
        Assert.Contains(handlerNodes, node => node.Kind == CodeNodeKinds.Callable);
    }

    // P6 (F6/F8/F10): staged facts are invisible until the run completes
    // atomically; the previous publication remains readable throughout.
    [Fact]
    public async Task Probe_StagedFactsAreInvisibleUntilAtomicCompletion()
    {
        var file = Node("file:a.ts");
        var target = Node("file:b.ts");
        var store = await SeededStoreAsync([
            Unit("typescript:file:a", [file], []),
            Unit("typescript:file:b", [target], []),
        ]);

        var (run, started) = await BeginRunAsync(store);
        await store.StageIndexUnitAsync(WithRun(
            Unit("typescript:modules", [], [Edge("edge:r1", file.Id, target.Id)]), run));
        Assert.Equal(0, await CountEdgesFrom(store, file.Id));

        await store.CompleteIndexRunAsync(
            new(Repo, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(Repo, run, []));
        Assert.Equal(1, await CountEdgesFrom(store, file.Id));
    }

    private static CodeGraphNode[] SymbolNodes(
        Extraction extraction,
        CodeNodeKind? kind,
        string name) =>
        extraction.Nodes
            .Where(node => node.Name == name &&
                node.SymbolId is not null &&
                (kind is null || node.Kind == kind))
            .ToArray();

    private static IReadOnlyDictionary<string, CodePropertyValue> NameProperty(string value) =>
        new Dictionary<string, CodePropertyValue> { ["exported-name"] = new CodeTextProperty(value) };

    private static CodeGraphNode Node(string name) =>
        new(new($"node:{name}"), CodeNodeKinds.File, name, name, new($"symbol:{name}"));

    private static CodeGraphEdge Edge(
        string id,
        CodeNodeId source,
        CodeNodeId target,
        IReadOnlyDictionary<string, CodePropertyValue>? properties = null) =>
        new(new(id), source, target, CodeEdgeKinds.References,
            new CodeEvidence(EvidenceQuality.Resolved, "review:ec6-glm-ar1"), properties);

    private static CodeIndexUnitReplacement Unit(
        string unitId,
        CodeGraphNode[] nodes,
        CodeGraphEdge[] edges) =>
        new(new CodeFactOrigin(Repo, Plugin, "1.0.0", new("run:seed"), new(unitId)), nodes, edges: edges);

    private static async Task<InMemoryCodeGraphStore> EmptyStoreAsync()
    {
        var store = new InMemoryCodeGraphStore();
        await store.UpsertRepositoryAsync(new(Repo));
        return store;
    }

    private static async Task<InMemoryCodeGraphStore> SeededStoreAsync(
        CodeIndexUnitReplacement[] units)
    {
        var store = await EmptyStoreAsync();
        var started = DateTimeOffset.UtcNow;
        await store.StoreIndexRunAsync(new(Repo, new("run:seed"), started, plugins: [Plugin]));
        foreach (var unit in units)
            await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(Repo, new("run:seed"), started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(Repo, new("run:seed"), []));
        return store;
    }

    private static async Task<(CodeIndexRunId Run, DateTimeOffset Started)> BeginRunAsync(
        InMemoryCodeGraphStore store)
    {
        var run = new CodeIndexRunId($"run:{Guid.NewGuid():N}");
        var started = DateTimeOffset.UtcNow;
        await store.StoreIndexRunAsync(new(Repo, run, started, plugins: [Plugin]));
        return (run, started);
    }

    private static async Task RerunAsync(
        InMemoryCodeGraphStore store,
        CodeIndexUnitReplacement[] replacements,
        string[]? deleteUnits = null)
    {
        var (run, started) = await BeginRunAsync(store);
        foreach (var replacement in replacements)
            await store.StageIndexUnitAsync(WithRun(replacement, run));
        foreach (var unit in deleteUnits ?? [])
            await store.StageIndexUnitDeletionAsync(Repo, run, Plugin, new(unit));
        await store.CompleteIndexRunAsync(
            new(Repo, run, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(Repo, run, []));
    }

    private static CodeIndexUnitReplacement WithRun(CodeIndexUnitReplacement replacement, CodeIndexRunId run)
    {
        var origin = replacement.Origin;
        return new(
            new CodeFactOrigin(origin.RepositoryId, origin.PluginId, origin.PluginVersion, run, origin.IndexUnitId),
            replacement.Nodes,
            replacement.Declarations,
            replacement.Edges);
    }

    private static async Task<int> CountEdgesFrom(InMemoryCodeGraphStore store, CodeNodeId source)
    {
        var traversal = await store.TraverseAsync(Repo, new CodeGraphTraversalQuery(
            source, CodeGraphDirection.Outgoing, maxNodes: 50, maxEdges: 50));
        return traversal.Edges.Count;
    }
}

