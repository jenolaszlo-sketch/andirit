using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

public sealed class TreeSitterModuleResolutionLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexAsync_RebuildsResolutionUnitOnTargetAndImporterChanges(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-lifecycle-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var importerPath = Path.Combine(sourceDirectory.FullName, "page.ts");
        var targetPath = Path.Combine(sourceDirectory.FullName, "target.ts");
        await File.WriteAllTextAsync(importerPath, "import './target';\n");
        await File.WriteAllTextAsync(targetPath, "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6b-lifecycle-{(durable ? "lattice" : "memory")}");
        var databasePath = Path.Combine(directory.FullName, "graph.db");
        ICodeGraphStore store = CreateStore(durable, databasePath);
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var queries = new CodeGraphQueryService(store);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            var firstEdge = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));
            Assert.Equal("src/page.ts", firstEdge.Evidence.Location!.Path);
            Assert.Equal("src/target.ts", await TargetPath(queries, repositoryId, firstEdge.TargetId));

            File.Delete(targetPath);
            var deletedTarget = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:delete-target"));
            Assert.Equal(CodeIndexRunStatus.Completed, deletedTarget.Diagnostics.Status);
            Assert.DoesNotContain(
                await ModuleEdges(queries, repositoryId, "src/page.ts"),
                edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
            Assert.Null(await store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/target.ts")));
            Assert.Single(await NeighborhoodEdges(queries, repositoryId, "src/page.ts", CodeEdgeKinds.Imports));
            Assert.Equal(1, Assert.Single(
                deletedTarget.Diagnostics.Plugins).UnresolvedRelationships);

            await File.WriteAllTextAsync(targetPath, "export const target = true;\n");
            var addedTarget = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:add-target"));
            Assert.Equal(CodeIndexRunStatus.Completed, addedTarget.Diagnostics.Status);
            var addedEdge = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));
            Assert.Equal("src/target.ts", await TargetPath(queries, repositoryId, addedEdge.TargetId));

            var otherTargetPath = Path.Combine(sourceDirectory.FullName, "other.ts");
            await File.WriteAllTextAsync(otherTargetPath, "export const other = true;\n");
            await File.WriteAllTextAsync(importerPath, "import './other';\n");
            var changedSpecifier = await indexing.IndexAsync(
                descriptor,
                new CodeIndexRunId("run:change-module-specifier"));
            Assert.Equal(CodeIndexRunStatus.Completed, changedSpecifier.Diagnostics.Status);
            var changedEdge = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));
            Assert.Equal("src/other.ts", await TargetPath(queries, repositoryId, changedEdge.TargetId));
            Assert.NotEqual(addedEdge.Id, changedEdge.Id);

            await File.WriteAllTextAsync(importerPath, "export const page = true;\n");
            var emptied = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:empty-modules"));
            Assert.Equal(CodeIndexRunStatus.Completed, emptied.Diagnostics.Status);
            Assert.DoesNotContain(
                await ModuleEdges(queries, repositoryId, "src/page.ts"),
                edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
            var emptyModuleUnit = Assert.Single(
                await store.GetPublishedUnitsAsync(repositoryId),
                unit => unit.Origin.IndexUnitId.Value == "typescript:modules");
            Assert.Empty(emptyModuleUnit.Edges);

            File.Delete(importerPath);
            var deletedImporter = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:delete-importer"));
            Assert.Equal(CodeIndexRunStatus.Completed, deletedImporter.Diagnostics.Status);
            Assert.DoesNotContain(
                await ModuleEdges(queries, repositoryId, "src/page.ts"),
                edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
            Assert.Null(await store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/page.ts")));

            var unchanged = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:unchanged"));
            Assert.Equal(CodeIndexRunStatus.Completed, unchanged.Diagnostics.Status);
            Assert.Equal(0, unchanged.Diagnostics.PluginsExecuted);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexAsync_RemovesOneOfTwoSameTargetOccurrences(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-occurrence-removal-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var importerPath = Path.Combine(sourceDirectory.FullName, "page.ts");
        await File.WriteAllTextAsync(
            importerPath,
            "import { first } from './shared';\nimport { second } from './shared';\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "shared.ts"), "export const shared = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6b-occurrence-removal-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var queries = new CodeGraphQueryService(store);
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:occurrences-first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);

            var initialEdges = (await ModuleEdges(queries, repositoryId, "src/page.ts"))
                .OrderBy(edge => edge.Evidence.Location!.StartLine)
                .ToArray();
            Assert.Equal(2, initialEdges.Length);
            Assert.Equal(2, initialEdges.Select(edge => edge.Id).Distinct().Count());
            var survivingId = initialEdges[0].Id;
            var removedId = initialEdges[1].Id;

            await File.WriteAllTextAsync(importerPath, "import { first } from './shared';\n");
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:occurrences-second"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

            var remaining = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));
            Assert.Equal(survivingId, remaining.Id);
            Assert.DoesNotContain(removedId, (await store.GetPublishedUnitsAsync(repositoryId))
                .SelectMany(unit => unit.Edges)
                .Select(edge => edge.Id));
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexAsync_ResolverFailureRetainsPreviousPublication(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-failure-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6b-failure-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var initial = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            var queries = new CodeGraphQueryService(store);
            var original = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));

            await File.WriteAllTextAsync(
                Path.Combine(sourceDirectory.FullName, "page.ts"),
                "// shift the occurrence\nimport './target';\n");
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new ThrowingModuleResolver()))
                    .IndexAsync(descriptor, new CodeIndexRunId("run:resolver-failure")));

            var failed = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:resolver-failure"));
            Assert.Equal(CodeIndexRunStatus.Failed, failed!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(original.Id, Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts")).Id);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexAsync_CancellationBeforeResolutionPublicationRetainsPreviousGraph(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-cancel-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6b-cancel-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var initial = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            var queries = new CodeGraphQueryService(store);
            var original = Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts"));

            await File.WriteAllTextAsync(
                Path.Combine(sourceDirectory.FullName, "page.ts"),
                "// shift the occurrence\nimport './target';\n");
            using var cancellation = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new CancellingModuleResolver(cancellation)))
                    .IndexAsync(
                        descriptor,
                        new CodeIndexRunId("run:cancelled"),
                        cancellationToken: cancellation.Token));

            var cancelled = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:cancelled"));
            Assert.Equal(CodeIndexRunStatus.Cancelled, cancelled!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(original.Id, Assert.Single(await ModuleEdges(queries, repositoryId, "src/page.ts")).Id);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task LatticeDb_ReopensPublishedModuleRelationships()
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-reopen-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId("repo:ec6b-lattice-reopen");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var databasePath = Path.Combine(directory.FullName, "graph.db");
        using (var store = new LatticeCodeGraphStore(databasePath))
        {
            var indexed = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:lattice-persist"));
            Assert.Equal(CodeIndexRunStatus.Completed, indexed.Diagnostics.Status);
        }

        using var reopened = new LatticeCodeGraphStore(databasePath);
        var edge = Assert.Single(await ModuleEdges(
            new CodeGraphQueryService(reopened), repositoryId, "src/page.ts"));
        Assert.Equal("./target", Assert.IsType<CodeTextProperty>(edge.Properties[CodePropertyKeys.Specifier]).Value);
        Assert.Equal(EvidenceQuality.Resolved, edge.Evidence.Quality);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Store_RejectsResolutionUnitBeforeEndpointFileUnits(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-order-");
        var plugin = new TreeSitterTypeScriptPlugin();
        var repositoryId = new CodeRepositoryId($"repo:ec6b-order-{(durable ? "lattice" : "memory")}");
        var runId = new CodeIndexRunId("run:wrong-order");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            await store.UpsertRepositoryAsync(new CodeRepositoryManifest(repositoryId));
            await store.StoreIndexRunAsync(new(
                repositoryId,
                runId,
                DateTimeOffset.UtcNow,
                plugins: [plugin.Id]));
            var source = FileNodeId("typescript", "src/page.ts");
            var target = FileNodeId("typescript", "src/target.ts");
            var origin = new CodeFactOrigin(
                repositoryId,
                plugin.Id,
                plugin.Version,
                runId,
                new CodeIndexUnitId("typescript:modules"));
            var edge = new CodeGraphEdge(
                new CodeEdgeId("typescript:wrong-order"),
                source,
                target,
                CodeEdgeKinds.ResolvesTo,
                new CodeEvidence(
                    EvidenceQuality.Resolved,
                    EcmaScriptModuleResolver.ResolverProvider,
                    "0.2.0-preview.6+resolver2.relative-v1"));

            var exception = await Assert.ThrowsAsync<CodeGraphBatchRejectedException>(async () =>
                await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(origin, edges: [edge])));
            Assert.Contains(exception.Errors, error => error.Code == "graph.edge.endpoint-missing");
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task IndexAsync_DoesNotResolveAcrossTypeScriptAndJavaScriptPluginInventories()
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6b-cross-plugin-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "page.ts"), "import './runtime';\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "runtime.js"), "export const runtime = true;\n");

        var repositoryId = new CodeRepositoryId("repo:ec6b-cross-plugin");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            new CodeIndexRunId("run:cross-plugin"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        var queries = new CodeGraphQueryService(store);
        Assert.DoesNotContain(
            await ModuleEdges(queries, repositoryId, "src/page.ts"),
            edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Single(await NeighborhoodEdges(queries, repositoryId, "src/page.ts", CodeEdgeKinds.Imports));
        Assert.Equal(1, Assert.Single(
            result.Diagnostics.Plugins,
            item => item.PluginId == TreeSitterTypeScriptPlugin.PluginId).UnresolvedRelationships);
    }

    private static ICodeGraphStore CreateStore(bool durable, string databasePath) =>
        durable ? new LatticeCodeGraphStore(databasePath) : new InMemoryCodeGraphStore();

    private static CodeIndexingService CreateIndexing(ICodeGraphStore store, ICodeGraphPlugin plugin) =>
        new(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([plugin]),
            store);

    private static async Task<IReadOnlyList<CodeGraphEdge>> ModuleEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string sourcePath) =>
        await NeighborhoodEdges(queries, repositoryId, sourcePath, CodeEdgeKinds.ResolvesTo);

    private static async Task<IReadOnlyList<CodeGraphEdge>> NeighborhoodEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string sourcePath,
        CodeEdgeKind kind)
    {
        var result = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId("typescript", sourcePath),
            [kind],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        return result.Edges;
    }

    private static async Task<string?> TargetPath(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        CodeNodeId nodeId)
    {
        var neighborhood = await queries.GetNeighborhoodAsync(
            repositoryId,
            nodeId,
            options: new CodeGraphQueryOptions(maxDepth: 0, maxNodes: 10, maxEdges: 10));
        return neighborhood.Nodes.SingleOrDefault(node => node.Id == nodeId)?.QualifiedName;
    }

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Hash(path)}");

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(DirectoryInfo directory) => Root = directory;

        internal DirectoryInfo Root { get; }
        internal string FullName => Root.FullName;

        internal static TemporaryDirectory Create(string prefix) =>
            new(Directory.CreateTempSubdirectory(prefix));

        public void Dispose() => Root.Delete(recursive: true);
    }

    private sealed class ThrowingModuleResolver : ITreeSitterModuleResolver
    {
        public string Language => "typescript";
        public string Provider => EcmaScriptModuleResolver.ResolverProvider;
        public string ProviderVersion => "0.2.0-preview.6+resolver2.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected module-resolution failure.");
    }

    private sealed class CancellingModuleResolver(CancellationTokenSource cancellation) : ITreeSitterModuleResolver
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
