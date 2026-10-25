using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

// Independent EC6B-GLM-Q2 publication probes: replacement, deletion, empty
// completion, cancellation, resource-limit failure, and endpoint ordering.
public sealed class ReviewEc6bGlmQ2LifecycleProbe
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2G_TargetAdditionAndDeletion(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2g-lifecycle-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");

        var repositoryId = new CodeRepositoryId($"repo:q2g-lifecycle-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var queries = new CodeGraphQueryService(store);
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);

        var unresolved = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q2g-missing"));
        Assert.Equal(CodeIndexRunStatus.Completed, unresolved.Diagnostics.Status);
        Assert.Empty(await ModuleEdges(queries, repositoryId));
        Assert.Equal(1, Assert.Single(unresolved.Diagnostics.Plugins).UnresolvedRelationships);

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");
        var added = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q2g-added"));
        Assert.Equal(CodeIndexRunStatus.Completed, added.Diagnostics.Status);
        var edge = Assert.Single(await ModuleEdges(queries, repositoryId));
        Assert.Equal("extension-substitution", Text(edge, CodePropertyKeys.ResolutionRule));
        Assert.Equal(0, Assert.Single(added.Diagnostics.Plugins).UnresolvedRelationships);

        File.Delete(Path.Combine(source.FullName, "target.ts"));
        var deleted = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q2g-deleted"));
        Assert.Equal(CodeIndexRunStatus.Completed, deleted.Diagnostics.Status);
        Assert.Empty(await ModuleEdges(queries, repositoryId));
        Assert.Null(await store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/target.ts")));
        var imports = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId("typescript", "src/page.ts"),
            [CodeEdgeKinds.Imports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        Assert.Single(imports.Edges);
        Assert.Equal(1, Assert.Single(deleted.Diagnostics.Plugins).UnresolvedRelationships);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2G_EmptyResolutionClearsUnit(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2g-empty-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q2g-empty-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q2g-first"));

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "export const page = true;\n");
        var emptied = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:q2g-empty"));
        Assert.Equal(CodeIndexRunStatus.Completed, emptied.Diagnostics.Status);
        var unit = Assert.Single(
            await store.GetPublishedUnitsAsync(repositoryId),
            value => value.Origin.IndexUnitId.Value == "typescript:modules");
        Assert.Empty(unit.Edges);
        Assert.DoesNotContain(
            (await store.GetPublishedUnitsAsync(repositoryId)).SelectMany(value => value.Edges),
            edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2G_CancellationRetainsPreviousPublication(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2g-cancel-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q2g-cancel-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var initial = await ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(descriptor, new CodeIndexRunId("run:q2g-first"));
        var queries = new CodeGraphQueryService(store);
        var original = Assert.Single(await ModuleEdges(queries, repositoryId));

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "// shifted\nimport './target';\n");
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin(new CancelResolver(cancellation)))
                .IndexAsync(descriptor, new CodeIndexRunId("run:q2g-cancelled"), cancellationToken: cancellation.Token));

        var cancelled = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:q2g-cancelled"));
        Assert.Equal(CodeIndexRunStatus.Cancelled, cancelled!.Status);
        Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
        Assert.Equal(original.Id, Assert.Single(await ModuleEdges(queries, repositoryId)).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2G_MidUnitLimitFailurePublishesNothingPartial(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2g-limit-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        for (var index = 0; index < 5; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(source.FullName, $"importer{index}.ts"), $"import './target{index}';\n");
            await File.WriteAllTextAsync(
                Path.Combine(source.FullName, $"target{index}.ts"), $"export const target{index} = true;\n");
        }

        var repositoryId = new CodeRepositoryId($"repo:q2g-limit-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var initial = await ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(descriptor, new CodeIndexRunId("run:q2g-first"));
        Assert.Equal(CodeIndexRunStatus.Completed, initial.Diagnostics.Status);
        var originalIds = (await store.GetPublishedUnitsAsync(repositoryId))
            .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
            .Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(5, originalIds.Length);

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "importer0.ts"), "// changed\nimport './target0';\n");
        var limits = new CodeGraphBatchLimits(
            maxNodes: 2_000,
            maxDeclarations: 2_000,
            maxEdges: 2,
            maxBatchesPerIndexUnit: 1_024,
            maxFactsPerIndexUnit: 4);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(
                    descriptor,
                    new CodeIndexRunId("run:q2g-limited"),
                    new CodeIndexingOptions(batchLimits: limits)));

        var failed = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:q2g-limited"));
        Assert.Equal(CodeIndexRunStatus.Failed, failed!.Status);
        Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
        Assert.Equal(
            originalIds,
            (await store.GetPublishedUnitsAsync(repositoryId))
                .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
                .Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2G_ResolutionUnitBeforeEndpointsIsRejected(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2g-order-");
        var repositoryId = new CodeRepositoryId($"repo:q2g-order-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var plugin = new TreeSitterTypeScriptPlugin();
        var runId = new CodeIndexRunId("run:q2g-order");
        await store.UpsertRepositoryAsync(new CodeRepositoryManifest(repositoryId));
        await store.StoreIndexRunAsync(new(repositoryId, runId, DateTimeOffset.UtcNow, plugins: [plugin.Id]));
        var origin = new CodeFactOrigin(
            repositoryId,
            plugin.Id,
            plugin.Version,
            runId,
            new CodeIndexUnitId("typescript:modules"));
        var edge = new CodeGraphEdge(
            new CodeEdgeId("typescript:q2g-order"),
            FileNodeId("typescript", "src/page.ts"),
            FileNodeId("typescript", "src/target.ts"),
            CodeEdgeKinds.ResolvesTo,
            new CodeEvidence(
                EvidenceQuality.Resolved,
                EcmaScriptModuleResolver.ResolverProvider,
                "0.2.0-preview.6+resolver2.relative-v1"));

        var exception = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
            await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(origin, edges: [edge])));
        Assert.Contains(exception.Errors, error => error.Code == "graph.edge.endpoint-missing");
    }

    private static async Task<IReadOnlyList<CodeGraphEdge>> ModuleEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId)
    {
        var result = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId("typescript", "src/page.ts"),
            [CodeEdgeKinds.ResolvesTo],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        return result.Edges;
    }

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path)))}");

    private sealed class CancelResolver(CancellationTokenSource cancellation) : ITreeSitterModuleResolver
    {
        public string Language => "typescript";

        public string Provider => EcmaScriptModuleResolver.ResolverProvider;

        public string ProviderVersion => "0.2.0-preview.6+resolver2.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not observed.");
        }
    }
}
