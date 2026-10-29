using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Penghou.Andirit.TreeSitter.Tests;

public sealed class TreeSitterEcmaExportPublicationTests
{
    [Fact]
    public async Task PublishesModuleScopedSlotsAndIndependentOccurrenceEdges()
    {
        const string path = "src/main.ts";
        const string source = "interface Model {}\nconst project = {};\n" +
            "export { project as currentProject };\n" +
            "export { project as currentProject };\n" +
            "export type { Model as Shape };\n" +
            "export { type Model as InlineShape };";

        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var exportEdges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        Assert.Equal(3, entries.Length);
        Assert.Equal(4, exportEdges.Length);
        Assert.Equal(3, entries.Select(node => node.Id).Distinct().Count());
        Assert.Equal(4, exportEdges.Select(edge => edge.Id).Distinct().Count());
        Assert.Equal(1, entries.Count(node => node.Name == "currentProject"));
        Assert.Equal(2, exportEdges.Count(edge => EntryName(entries, edge) == "currentProject"));

        foreach (var entry in entries)
        {
            Assert.Equal(entry.Name, entry.QualifiedName);
            Assert.Null(entry.SymbolId);
            Assert.Equal(
                new Dictionary<string, CodePropertyValue>(StringComparer.Ordinal)
                {
                    [CodePropertyKeys.Language] = new CodeTextProperty("typescript")
                },
                entry.Properties);
            Assert.Equal(
                $"typescript:export:{Hash($"{path}\n{entry.Name}")}",
                entry.Id.Value);
        }

        var sourceNodeId = Assert.Single(exportEdges.Select(edge => edge.SourceId).Distinct());
        Assert.Equal(FileNodeId("typescript", path), sourceNodeId);
        Assert.All(exportEdges, edge =>
        {
            var exportedName = EntryName(entries, edge);
            var (localName, typeOnly, projection) = exportedName switch
            {
                "currentProject" => ("project", false, "unknown"),
                "Shape" or "InlineShape" => ("Model", true, "type"),
                _ => throw new InvalidOperationException($"Unexpected export name '{exportedName}'.")
            };
            var location = Assert.IsType<CodeLocation>(edge.Evidence.Location);
            Assert.Equal(path, location.Path);
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(EcmaScriptExportGraphPublisher.Provider, edge.Evidence.Provider);
            Assert.Equal(
                $"{new TreeSitterTypeScriptPlugin().Version.Split('+', 2)[0]}+exports1.local-v1",
                edge.Evidence.ProviderVersion);
            Assert.Equal("named", TextProperty(edge, CodePropertyKeys.ExportKind));
            Assert.Equal("local", TextProperty(edge, CodePropertyKeys.ExportVia));
            Assert.Equal(projection, TextProperty(edge, CodePropertyKeys.NamespaceProjection));
            Assert.Equal(localName, TextProperty(edge, CodePropertyKeys.LocalName));
            Assert.Equal(typeOnly, edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var typeOnlyValue) &&
                Assert.IsType<CodeBooleanProperty>(typeOnlyValue).Value);
            Assert.False(edge.Properties.ContainsKey("construct-span"));
            Assert.Equal(
                ExpectedOccurrenceId(
                    "typescript",
                    edge.SourceId,
                    edge.TargetId,
                    exportedName,
                    "named",
                    "local",
                    localName,
                    typeOnly,
                    projection,
                    location),
                edge.Id.Value);

            var line = source.Split('\n')[location.StartLine - 1];
            var tokenIndex = line.IndexOf(exportedName, StringComparison.Ordinal);
            Assert.Equal(tokenIndex + 1, location.StartColumn);
            Assert.Equal(tokenIndex + exportedName.Length + 1, location.EndColumn);
        });

        var shapeEdge = Assert.Single(exportEdges, edge => EntryName(entries, edge) == "Shape");
        Assert.Equal("type", TextProperty(shapeEdge, CodePropertyKeys.NamespaceProjection));
        Assert.True(BooleanProperty(shapeEdge, CodePropertyKeys.TypeOnly));

        var inlineShapeEdge = Assert.Single(exportEdges, edge => EntryName(entries, edge) == "InlineShape");
        Assert.Equal("type", TextProperty(inlineShapeEdge, CodePropertyKeys.NamespaceProjection));
        Assert.True(BooleanProperty(inlineShapeEdge, CodePropertyKeys.TypeOnly));

        Assert.DoesNotContain(
            extraction.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.Exports.Value);
        Assert.DoesNotContain(extraction.Edges, edge => edge.Kind.Value is "exported-as" or "binds");

        var repeated = exportEdges.Where(edge => EntryName(entries, edge) == "currentProject")
            .OrderBy(edge => edge.Evidence.Location!.StartLine)
            .ToArray();
        Assert.Equal(2, repeated.Length);
        Assert.NotEqual(repeated[0].Id, repeated[1].Id);
        Assert.Equal(repeated[0].TargetId, repeated[1].TargetId);
    }

    [Fact]
    public async Task EntryIdentityIsModuleAndNameScopedWithoutUnicodeNormalization()
    {
        const string firstSource = "export { local as \"é\" };\nexport { local as \"é\" };";
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/one.ts", firstSource));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/two.ts", "export { local as \"é\" };"));

        var firstEntries = first.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var secondEntry = Assert.Single(second.Nodes, node => node.Kind == CodeNodeKinds.Export);

        Assert.Equal(2, firstEntries.Length);
        Assert.NotEqual(firstEntries[0].Name, firstEntries[1].Name);
        Assert.NotEqual(firstEntries[0].Id, firstEntries[1].Id);
        Assert.NotEqual(firstEntries[0].Id, secondEntry.Id);
        Assert.Equal(
            $"typescript:export:{Hash($"src/one.ts\n{firstEntries[0].Name}")}",
            firstEntries[0].Id.Value);
    }

    [Fact]
    public async Task MapsDirectDefaultAndExpressionFormsToAcceptedMetadata()
    {
        const string path = "src/forms.ts";
        const string source = "export const direct = 1;\n" +
            "export default function named() {}\n" +
            "export default 42;";
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, source));
        var entries = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Export).ToArray();
        var edges = extraction.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Exports).ToArray();

        Assert.Equal(2, entries.Length);
        Assert.Equal(3, edges.Length);
        var direct = Assert.Single(edges, edge => EntryName(entries, edge) == "direct");
        Assert.Equal("named", TextProperty(direct, CodePropertyKeys.ExportKind));
        Assert.Equal("direct", TextProperty(direct, CodePropertyKeys.ExportVia));
        Assert.Equal("direct", TextProperty(direct, CodePropertyKeys.LocalName));
        Assert.Equal("value", TextProperty(direct, CodePropertyKeys.NamespaceProjection));
        Assert.False(direct.Properties.ContainsKey(CodePropertyKeys.TypeOnly));

        var defaults = edges.Where(edge => EntryName(entries, edge) == "default")
            .OrderBy(edge => edge.Evidence.Location!.StartLine)
            .ToArray();
        Assert.Equal(2, defaults.Length);
        Assert.All(defaults, edge => Assert.Equal("default", TextProperty(edge, CodePropertyKeys.ExportKind)));
        Assert.Equal("direct", TextProperty(defaults[0], CodePropertyKeys.ExportVia));
        Assert.Equal("named", TextProperty(defaults[0], CodePropertyKeys.LocalName));
        Assert.Equal("expression", TextProperty(defaults[1], CodePropertyKeys.ExportVia));
        Assert.False(defaults[1].Properties.ContainsKey(CodePropertyKeys.LocalName));
        Assert.All(defaults, edge =>
        {
            Assert.Equal("value", TextProperty(edge, CodePropertyKeys.NamespaceProjection));
            Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
            Assert.Equal(
                ExpectedOccurrenceId(
                    "typescript",
                    edge.SourceId,
                    edge.TargetId,
                    "default",
                    "default",
                    TextProperty(edge, CodePropertyKeys.ExportVia),
                    edge.Properties.TryGetValue(CodePropertyKeys.LocalName, out var localName)
                        ? Assert.IsType<CodeTextProperty>(localName).Value
                        : null,
                    typeOnly: false,
                    "value",
                    edge.Evidence.Location!),
                edge.Id.Value);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexAsync_ReplacesAliasesAndRemovesFinalExportFacts(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6c-export-replace-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath,
            "const project = {};\n" +
            "export { project as currentProject };\n" +
            "export { project as currentProject };\n" +
            "export { project as selectedProject };\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6c-export-replace-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var indexing = CreateIndexing(store, new TreeSitterTypeScriptPlugin());
            var queries = new CodeGraphQueryService(store);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:exports-first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            var firstEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            Assert.Equal(3, firstEdges.Count);
            Assert.Equal(2, firstEdges.Select(edge => edge.TargetId).Distinct().Count());
            var firstNames = new Dictionary<CodeEdgeId, string?>();
            foreach (var edge in firstEdges)
                firstNames[edge.Id] = await TargetName(store, repositoryId, edge.TargetId);
            Assert.Equal(2, firstNames.Values.Count(name => name == "currentProject"));

            var originalSelected = Assert.Single(firstEdges, edge => firstNames[edge.Id] == "selectedProject");
            var originalSelectedNameId = originalSelected.TargetId;
            var originalDuplicate = firstEdges
                .Where(edge => firstNames[edge.Id] == "currentProject")
                .OrderBy(edge => edge.Evidence.Location!.StartLine)
                .First();

            var exactAlias = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "currentProject");
            Assert.Equal(CodeNodeKinds.Export, Assert.Single(exactAlias.Candidates).Kind);
            Assert.Null(Assert.Single(exactAlias.Candidates).SymbolId);

            await File.WriteAllTextAsync(filePath,
                "const project = {};\n" +
                "export { project as renamedProject };\n" +
                "export { project as selectedProject };\n");
            var renamed = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:exports-renamed"));
            Assert.Equal(CodeIndexRunStatus.Completed, renamed.Diagnostics.Status);
            var renamedEdges = await ExportEdges(queries, repositoryId, "src/main.ts");
            Assert.Equal(2, renamedEdges.Count);
            Assert.DoesNotContain(renamedEdges, edge => edge.TargetId == originalDuplicate.TargetId);
            Assert.Contains(renamedEdges, edge => edge.TargetId == originalSelectedNameId);
            Assert.Null(await store.GetNodeAsync(repositoryId, originalDuplicate.TargetId));
            Assert.Equal(
                CodeNodeKinds.Export,
                Assert.Single((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "renamedProject")).Candidates).Kind);
            Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "currentProject")).Candidates);

            await File.WriteAllTextAsync(filePath, "const project = {};\n");
            var emptied = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:exports-empty"));
            Assert.Equal(CodeIndexRunStatus.Completed, emptied.Diagnostics.Status);
            Assert.Empty(await ExportEdges(queries, repositoryId, "src/main.ts"));
            Assert.Null(await store.GetNodeAsync(repositoryId, originalSelectedNameId));

            File.Delete(filePath);
            var deleted = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:exports-delete"));
            Assert.Equal(CodeIndexRunStatus.Completed, deleted.Diagnostics.Status);
            Assert.Null(await store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/main.ts")));
            Assert.DoesNotContain(
                (await store.GetPublishedUnitsAsync(repositoryId)).SelectMany(unit => unit.Edges),
                edge => edge.Kind == CodeEdgeKinds.Exports);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Fact]
    public async Task LatticeDb_ReopensAndQueriesMaximumUnicodeExportName()
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6c-export-unicode-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var sourcePath = Path.Combine(sourceDirectory.FullName, "unicode.ts");
        var supplementary = char.ConvertFromUtf32(0x1f600);
        var maximumName = string.Concat(Enumerable.Repeat(supplementary, 256));
        var rejectedName = string.Concat(Enumerable.Repeat(supplementary, 257));
        await File.WriteAllTextAsync(sourcePath,
            $"const local = 1;\nexport {{ local as \"{maximumName}\" }};\n" +
            $"export {{ local as \"{rejectedName}\" }};\n");

        var repositoryId = new CodeRepositoryId("repo:ec6c-export-unicode-reopen");
        var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
        var databasePath = Path.Combine(directory.FullName, "graph.db");
        using (var store = new LatticeCodeGraphStore(databasePath))
        {
            var indexed = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:unicode-index"));
            Assert.Equal(CodeIndexRunStatus.Completed, indexed.Diagnostics.Status);
            Assert.Single(await ExportEdges(new CodeGraphQueryService(store), repositoryId, "src/unicode.ts"));
        }

        using var reopened = new LatticeCodeGraphStore(databasePath);
        var queries = new CodeGraphQueryService(reopened);
        var maximumLookup = await queries.FindSymbolsByQualifiedNameAsync(repositoryId, maximumName);
        var entry = Assert.Single(maximumLookup.Candidates);
        Assert.Equal(CodeNodeKinds.Export, entry.Kind);
        Assert.Equal(maximumName, entry.Name);
        Assert.Equal(maximumName, entry.QualifiedName);
        Assert.Null(entry.SymbolId);
        Assert.Equal(
            $"typescript:export:{Hash($"src/unicode.ts\n{maximumName}")}",
            entry.Id.Value);
        Assert.Empty((await queries.FindSymbolsByQualifiedNameAsync(repositoryId, rejectedName)).Candidates);
        var edge = Assert.Single(await ExportEdges(queries, repositoryId, "src/unicode.ts"));
        Assert.Equal(entry.Id, edge.TargetId);
        Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
    }

    [Fact]
    public void PublisherIsIdempotentAndRejectsSameIdDifferentOccurrenceFacts()
    {
        const string path = "src/collision.ts";
        var result = TreeSitterTestSupport.ExtractSyntax(
            "typescript",
            path,
            "export { local as result };",
            out var hasParseError);
        Assert.False(hasParseError);
        var fileNodeId = new CodeNodeId("typescript:file:shape-test");
        var pluginVersion = TreeSitterTypeScriptPlugin.PluginVersion;
        var nodeCount = result.Nodes.Count;
        var edgeCount = result.Edges.Count;

        EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result);
        Assert.Equal(nodeCount, result.Nodes.Count);
        Assert.Equal(edgeCount, result.Edges.Count);

        var original = Assert.Single(result.Edges.Values, edge => edge.Kind == CodeEdgeKinds.Exports);
        var conflicting = new CodeGraphEdge(
            original.Id,
            original.SourceId,
            original.TargetId,
            original.Kind,
            new CodeEvidence(
                original.Evidence.Quality,
                original.Evidence.Provider,
                original.Evidence.ProviderVersion,
                new CodeLocation(path, 1, 1, 1, 2)),
            original.Properties);
        result.Edges[conflicting.Id.Value] = conflicting;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            EcmaScriptExportGraphPublisher.Publish(path, "typescript", fileNodeId, pluginVersion, result));
        Assert.Contains(original.Id.Value, exception.Message, StringComparison.Ordinal);
        Assert.Contains("Existing fact: edge(", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Candidate fact: edge(", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ProviderVersion=", exception.Message, StringComparison.Ordinal);
        Assert.Equal(nodeCount, result.Nodes.Count);
        Assert.Equal(edgeCount, result.Edges.Count);
        Assert.Same(conflicting, result.Edges[conflicting.Id.Value]);

        var entryCollisionResult = TreeSitterTestSupport.ExtractSyntax(
            "typescript",
            path,
            "export { local as result };",
            out hasParseError);
        Assert.False(hasParseError);
        var entry = Assert.Single(
            entryCollisionResult.Nodes.Values,
            node => node.Kind == CodeNodeKinds.Export);
        entryCollisionResult.Nodes[entry.Id.Value] = new CodeGraphNode(
            entry.Id,
            entry.Kind,
            "different",
            "different",
            properties: entry.Properties);
        var entryException = Assert.Throws<InvalidOperationException>(() =>
            EcmaScriptExportGraphPublisher.Publish(
                path,
                "typescript",
                fileNodeId,
                pluginVersion,
                entryCollisionResult));
        Assert.Contains(entry.Id.Value, entryException.Message, StringComparison.Ordinal);
        Assert.Contains("Existing fact: node(", entryException.Message, StringComparison.Ordinal);
        Assert.Contains("Name=\"different\"", entryException.Message, StringComparison.Ordinal);
        Assert.Contains("Name=\"result\"", entryException.Message, StringComparison.Ordinal);

        var invalidNameResult = new TreeSitterExtractionResult();
        invalidNameResult.ExportObservations.Add(new EcmaScriptExportObservation(
            path,
            "typescript",
            "\ud800",
            null,
            EcmaScriptExportKind.Named,
            EcmaScriptExportForm.LocalAlias,
            false,
            EcmaScriptNamespaceProjection.Unknown,
            new CodeLocation(path, 1, 1, 1, 2),
            new CodeLocation(path, 1, 1, 1, 2)));
        Assert.Throws<InvalidOperationException>(() =>
            EcmaScriptExportGraphPublisher.Publish(
                path,
                "typescript",
                fileNodeId,
                pluginVersion,
                invalidNameResult));
        Assert.Empty(invalidNameResult.Nodes);
        Assert.Empty(invalidNameResult.Edges);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolverFailureRetainsPreviouslyPublishedExportFacts(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6c-export-failure-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "import './dep';\nexport { value as result };\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "dep.ts"), "export const value = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6c-export-failure-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var initial = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:export-success"));
            Assert.Equal(CodeIndexRunStatus.Completed, initial.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var published = Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts"));

            await File.WriteAllTextAsync(filePath, "// shift\nimport './dep';\nexport { value as renamed };\n");
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new ThrowingModuleResolver()))
                    .IndexAsync(descriptor, new CodeIndexRunId("run:export-failure")));

            var failed = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:export-failure"));
            Assert.Equal(CodeIndexRunStatus.Failed, failed!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(published.Id, Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts")).Id);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationRetainsPreviouslyPublishedExportFacts(bool durable)
    {
        using var directory = TemporaryDirectory.Create("hetu-ec6c-export-cancel-");
        var sourceDirectory = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        var filePath = Path.Combine(sourceDirectory.FullName, "main.ts");
        await File.WriteAllTextAsync(filePath, "import './dep';\nexport { value as result };\n");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory.FullName, "dep.ts"), "export const value = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:ec6c-export-cancel-{(durable ? "lattice" : "memory")}");
        var store = CreateStore(durable, Path.Combine(directory.FullName, "graph.db"));
        try
        {
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);
            var initial = await CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(descriptor, new CodeIndexRunId("run:export-cancel-success"));
            Assert.Equal(CodeIndexRunStatus.Completed, initial.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var published = Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts"));

            await File.WriteAllTextAsync(filePath, "// shift\nimport './dep';\nexport { value as renamed };\n");
            using var cancellation = new CancellationTokenSource();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await CreateIndexing(store, new TreeSitterTypeScriptPlugin(new CancellingModuleResolver(cancellation)))
                    .IndexAsync(
                        descriptor,
                        new CodeIndexRunId("run:export-cancelled"),
                        cancellationToken: cancellation.Token));

            var cancelled = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:export-cancelled"));
            Assert.Equal(CodeIndexRunStatus.Cancelled, cancelled!.Status);
            Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
            Assert.Equal(published.Id, Assert.Single(await ExportEdges(queries, repositoryId, "src/main.ts")).Id);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
        }
    }

    private static string EntryName(IReadOnlyList<CodeGraphNode> entries, CodeGraphEdge edge) =>
        entries.Single(node => node.Id == edge.TargetId).Name;

    private static string TextProperty(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static bool BooleanProperty(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeBooleanProperty>(edge.Properties[key]).Value;

    private static string ExpectedOccurrenceId(
        string language,
        CodeNodeId sourceId,
        CodeNodeId targetId,
        string exportedName,
        string exportKind,
        string exportVia,
        string? localName,
        bool typeOnly,
        string projection,
        CodeLocation location)
    {
        var span = string.Create(
            CultureInfo.InvariantCulture,
            $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}");
        var material = string.Join(
            '\n',
            "exports",
            sourceId.Value,
            targetId.Value,
            exportedName,
            exportKind,
            exportVia,
            localName is null ? "0" : "1",
            localName ?? string.Empty,
            typeOnly ? "true" : "false",
            projection,
            span);
        return $"{language}:{Hash(material)}";
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Hash(path)}");

    private static ICodeGraphStore CreateStore(bool durable, string databasePath) =>
        durable ? new LatticeCodeGraphStore(databasePath) : new InMemoryCodeGraphStore();

    private static CodeIndexingService CreateIndexing(ICodeGraphStore store, ICodeGraphPlugin plugin) =>
        new(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([plugin]),
            store);

    private static async Task<IReadOnlyList<CodeGraphEdge>> ExportEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string path)
    {
        var result = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId("typescript", path),
            [CodeEdgeKinds.Exports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        return result.Edges;
    }

    private static async Task<string?> TargetName(
        ICodeGraphReader store,
        CodeRepositoryId repositoryId,
        CodeNodeId nodeId) =>
        (await store.GetNodeAsync(repositoryId, nodeId))?.Name;

    private sealed class TemporaryDirectory : IDisposable
    {
        private TemporaryDirectory(DirectoryInfo directory) => Directory = directory;

        internal DirectoryInfo Directory { get; }
        internal string FullName => Directory.FullName;

        internal static TemporaryDirectory Create(string prefix) =>
            new(System.IO.Directory.CreateTempSubdirectory(prefix));

        public void Dispose() => Directory.Delete(recursive: true);
    }

    private sealed class ThrowingModuleResolver : ITreeSitterModuleResolver
    {
        public string Language => "typescript";
        public string Provider => EcmaScriptModuleResolver.ResolverProvider;
        public string ProviderVersion => "0.2.0-preview.6+resolver2.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Injected module-resolution failure after source extraction.");
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
