namespace Penghou.Hetu.TreeSitter.Tests;

using System.Text;

// Independent EC6B-GLM-Q1 review probes. Review-only: these tests are not part
// of the EC-6B implementation evidence and must not be treated as production
// tests. They challenge the accepted relative-v1 contract adversarially.

public sealed class ReviewEc6bGlmQ1Probes
{
    // ------------------------------------------------------------------
    // Q1-01: Candidate precedence is declared policy, not enumeration order.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe01_CandidatePrecedenceIsOrderIndependent()
    {
        const string importer = """
            import { a } from "./mod";
            import { b } from "./both";
            """;
        var forward = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/mod.tsx", "export const a = 2;"),
            ("src/mod.ts", "export const a = 1;"),
            ("src/both/index.ts", "export const b = 'index';"),
            ("src/both.ts", "export const b = 'file';"));
        var reversed = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/both.ts", "export const b = 'file';"),
            ("src/both/index.ts", "export const b = 'index';"),
            ("src/mod.ts", "export const a = 1;"),
            ("src/mod.tsx", "export const a = 2;"),
            ("src/page.ts", importer));

        var first = ModuleEdges(forward);
        var second = ModuleEdges(reversed);
        Assert.Equal(
            first.Select(Describe).OrderBy(value => value, StringComparer.Ordinal),
            second.Select(Describe).OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(2, first.Length);

        // `.ts` precedes `.tsx` in the ordered TypeScript suffix list.
        var modEdge = Assert.Single(first, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./mod");
        Assert.Equal(FileNodeId("typescript", "src/mod.ts"), modEdge.TargetId);
        Assert.Equal("extension-substitution", Text(modEdge, CodePropertyKeys.ResolutionRule));

        // File-before-index: `both.ts` wins over `both/index.ts`, and the
        // matched rule is extension-substitution (the file, not the index).
        var bothEdge = Assert.Single(first, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./both");
        Assert.Equal(FileNodeId("typescript", "src/both.ts"), bothEdge.TargetId);
        Assert.Equal("extension-substitution", Text(bothEdge, CodePropertyKeys.ResolutionRule));
    }

    // ------------------------------------------------------------------
    // Q1-02: Root escape, case mismatch, non-ASCII ordinal matching, and
    // inventory containment of every resolved target.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe02_RootEscapeCaseAndUnicodeBoundaries()
    {
        const string importer = """
            import './case';
            import './café';
            import '../../root-file';
            import '../../../escape';
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/deep/page.ts", importer),
            ("src/deep/Case.ts", "export const upper = true;"),
            ("src/deep/café.ts", "export const cafe = true;"),
            ("root-file.ts", "export const root = true;"));

        var edges = ModuleEdges(extracted);
        Assert.Equal(2, edges.Length);
        Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./café");
        Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "../../root-file");

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(1, coverage.UnresolvedTargets);
        Assert.Equal(1, coverage.UnsupportedTargets);

        var fileNodes = extracted.Nodes.Select(node => node.Id).ToHashSet();
        Assert.All(edges, edge =>
        {
            Assert.Contains(edge.SourceId, fileNodes);
            Assert.Contains(edge.TargetId, fileNodes);
            Assert.Equal(CodeNodeKinds.File, extracted.Nodes.Single(node => node.Id == edge.TargetId).Kind);
        });
    }

    // ------------------------------------------------------------------
    // Q1-03: Two identical specifiers on one line remain two distinct
    // occurrence-anchored identities.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe03_SameLineIdenticalSpecifiersStayDistinct()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", "import './a'; import './a';"),
            ("src/a.ts", "export const a = 1;"));

        var edges = ModuleEdges(extracted);
        Assert.Equal(2, edges.Length);
        Assert.Equal(2, edges.Select(edge => edge.Id.Value).Distinct(StringComparer.Ordinal).Count());
        var ordered = edges.OrderBy(edge => edge.Evidence.Location!.StartColumn).ToArray();
        Assert.Equal(1, ordered[0].Evidence.Location!.StartLine);
        Assert.Equal(1, ordered[1].Evidence.Location!.StartLine);
        Assert.True(ordered[0].Evidence.Location!.StartColumn < ordered[1].Evidence.Location!.StartColumn);
        foreach (var edge in edges)
            AssertIdentity(edge, "typescript");
    }

    // ------------------------------------------------------------------
    // Q1-04: Coordinate convention check. The accepted contract (§7.1.1)
    // claims one-based byte columns; the pinned TreeSitter.DotNet binding
    // parses strings as UTF-16LE, so observed columns are UTF-16 code units
    // (equal to .NET char offsets for BMP content). This probe pins the
    // ACTUAL behavior as executed evidence for review finding EC6B-GLM-F01.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe04_ColumnsAreUtf16CodeUnitsNotBytes()
    {
        const string line = "const s = \"中文 café\"; import './a';";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", line),
            ("src/a.ts", "export const a = 1;"));

        var edge = Assert.Single(ModuleEdges(extracted));
        var charIndex = line.IndexOf("import", StringComparison.Ordinal);
        var byteIndex = Encoding.UTF8.GetByteCount(line[..charIndex]);
        Assert.NotEqual(charIndex, byteIndex);
        var location = edge.Evidence.Location!;
        Assert.Equal(1, location.StartLine);
        // The decisive assertion: the reported column is the UTF-16 code-unit
        // column, NOT the UTF-8 byte column the accepted contract claims.
        Assert.Equal(charIndex + 1, location.StartColumn);
        Assert.NotEqual(byteIndex + 1, location.StartColumn);
        AssertIdentity(edge, "typescript");
    }

    // ------------------------------------------------------------------
    // Q1-05: A newly added target appears in the same publication without
    // any importer edit.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe05_TargetAdditionPublishesEdgeWithoutImporterEdit(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-add-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-add-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var queries = new CodeGraphQueryService(store);

        var before = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:before"));
        Assert.Equal(CodeIndexRunStatus.Completed, before.Diagnostics.Status);
        Assert.Empty(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));
        Assert.Equal(1, Assert.Single(before.Diagnostics.Plugins).UnresolvedRelationships);

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");
        var after = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:after"));
        Assert.Equal(CodeIndexRunStatus.Completed, after.Diagnostics.Status);

        var edge = Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));
        Assert.Equal("relative-v1", Text(edge, CodePropertyKeys.ResolutionPolicy));
        Assert.Equal("extension-substitution", Text(edge, CodePropertyKeys.ResolutionRule));
        Assert.NotNull(await store.GetNodeAsync(repositoryId, edge.TargetId));
        Assert.Equal(0, Assert.Single(after.Diagnostics.Plugins).UnresolvedRelationships);
        var unit = Assert.Single(
            await store.GetPublishedUnitsAsync(repositoryId),
            value => value.Origin.IndexUnitId.Value == "typescript:modules");
        Assert.Single(unit.Edges, value => value.Id == edge.Id);
    }

    // ------------------------------------------------------------------
    // Q1-06: Deleting the target removes stale relationships and leaves no
    // dangling endpoint while the structural imports observation survives.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe06_TargetDeletionRemovesStaleEdges(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-del-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-del-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var queries = new CodeGraphQueryService(store);
        var first = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:first"));
        var original = Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));

        File.Delete(Path.Combine(source.FullName, "target.ts"));
        var second = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:second"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

        Assert.Empty(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));
        Assert.Null(await store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/target.ts")));
        Assert.Single(await ReviewStore.Edges(queries, repositoryId, "typescript", "src/page.ts", CodeEdgeKinds.Imports));
        Assert.Equal(1, Assert.Single(second.Diagnostics.Plugins).UnresolvedRelationships);
        Assert.NotEqual(first.Publication.IndexRunId, second.Publication.IndexRunId);
    }

    // ------------------------------------------------------------------
    // Q1-07: Removing the last import publishes an empty resolution unit
    // that clears all previous resolver edges.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe07_EmptyResolutionClearsPublishedEdges(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-empty-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-empty-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var queries = new CodeGraphQueryService(store);
        await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:first"));
        Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "export const page = true;\n");
        var second = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:second"));
        Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

        var unit = Assert.Single(
            await store.GetPublishedUnitsAsync(repositoryId),
            value => value.Origin.IndexUnitId.Value == "typescript:modules");
        Assert.Empty(unit.Edges);
        Assert.DoesNotContain(
            (await store.GetPublishedUnitsAsync(repositoryId)).SelectMany(value => value.Edges),
            edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
    }

    // ------------------------------------------------------------------
    // Q1-08: Cancellation between staged files and publication retains the
    // previous publication, including its resolution unit contents.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe08_CancellationRetainsPreviousResolutionUnit(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-cancel-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-cancel-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var indexing = ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin());
        var queries = new CodeGraphQueryService(store);
        var initial = await indexing.IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:first"));
        var original = Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts"));
        var originalUnitEdges = (await store.GetPublishedUnitsAsync(repositoryId))
            .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
            .Edges.Select(edge => edge.Id.Value).ToArray();
        Assert.Single(originalUnitEdges);

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "// changed\nimport './target';\n");
        using var cancellation = new CancellationTokenSource();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin(new CancelOnFirstFileResolver(cancellation)))
                .IndexAsync(
                    new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                    new CodeIndexRunId("run:cancelled"),
                    cancellationToken: cancellation.Token));

        var cancelled = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:cancelled"));
        Assert.Equal(CodeIndexRunStatus.Cancelled, cancelled!.Status);
        Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
        Assert.Equal(original.Id, Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/page.ts")).Id);
        Assert.Equal(
            originalUnitEdges,
            (await store.GetPublishedUnitsAsync(repositoryId))
                .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
                .Edges.Select(edge => edge.Id.Value).ToArray());
    }

    // ------------------------------------------------------------------
    // Q1-09: A module unit that exceeds its ingestion limit after earlier
    // module batches were written fails the run and publishes nothing new.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe09_MidUnitModuleFailureRetainsPreviousPublication(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-fail-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        for (var index = 0; index < 5; index++)
        {
            await File.WriteAllTextAsync(
                Path.Combine(source.FullName, $"importer{index}.ts"),
                $"import './target{index}';\n");
            await File.WriteAllTextAsync(
                Path.Combine(source.FullName, $"target{index}.ts"),
                $"export const target{index} = true;\n");
        }

        var repositoryId = new CodeRepositoryId($"repo:q1-fail-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        var queries = new CodeGraphQueryService(store);
        var initial = await ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:first"));
        Assert.Equal(CodeIndexRunStatus.Completed, initial.Diagnostics.Status);
        var originalEdges = (await store.GetPublishedUnitsAsync(repositoryId))
            .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
            .Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(5, originalEdges.Length);

        await File.WriteAllTextAsync(Path.Combine(source.FullName, "importer0.ts"), "// changed\nimport './target0';\n");
        var limits = new CodeGraphBatchLimits(
            maxNodes: 2_000,
            maxDeclarations: 2_000,
            maxEdges: 2,
            maxBatchesPerIndexUnit: 1_024,
            maxFactsPerIndexUnit: 4);
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
                .IndexAsync(
                    new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                    new CodeIndexRunId("run:limited"),
                    new CodeIndexingOptions(batchLimits: limits)));

        var failed = await store.GetIndexRunAsync(repositoryId, new CodeIndexRunId("run:limited"));
        Assert.Equal(CodeIndexRunStatus.Failed, failed!.Status);
        Assert.Equal(initial.Publication.IndexRunId, (await store.GetLatestIndexStateAsync(repositoryId))!.IndexRunId);
        Assert.Equal(
            originalEdges,
            (await store.GetPublishedUnitsAsync(repositoryId))
                .Single(value => value.Origin.IndexUnitId.Value == "typescript:modules")
                .Edges.Select(edge => edge.Id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        Assert.Equal(5, (await ReviewStore.ModuleEdges(queries, repositoryId, "src/importer0.ts")).Count == 1 ? 5 : 5);
        Assert.Single(await ReviewStore.ModuleEdges(queries, repositoryId, "src/importer0.ts"));
    }

    // ------------------------------------------------------------------
    // Q1-10: Source and inventory enumeration order cannot change the
    // published resolution facts.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe10_EnumerationOrderCannotChangeFacts()
    {
        var sources = new (string Path, string Content)[]
        {
            ("src/zeta.ts", "import './alpha';\nimport './nested/beta';\n"),
            ("src/alpha.ts", "import './zeta';\n"),
            ("src/nested/beta.ts", "export const beta = 1;\n"),
            ("src/alpha-c.ts", "export const c = 1;\n")
        };
        var forward = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), sources);
        var backward = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), sources.Reverse().ToArray());

        var first = ModuleEdges(forward).Select(Describe).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        var second = ModuleEdges(backward).Select(Describe).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(3, first.Length);
        Assert.Equal(first, second);

        // Direct resolver: same inventory in reverse order, references shuffled.
        var importer = new TreeSitterModuleFile(
            "src/zeta.ts",
            FileNodeId("typescript", "src/zeta.ts"),
            [
                new("./nested/beta", "import", false, new CodeLocation("src/zeta.ts", 2, 8, 2, 25)),
                new("./alpha", "import", false, new CodeLocation("src/zeta.ts", 1, 8, 2, 22))
            ]);
        var inventoryForward = new List<TreeSitterModuleFile>
        {
            importer,
            new("src/alpha.ts", FileNodeId("typescript", "src/alpha.ts"), []),
            new("src/nested/beta.ts", FileNodeId("typescript", "src/nested/beta.ts"), [])
        };
        var inventoryBackward = inventoryForward.AsEnumerable().Reverse().ToList();
        var resolver = new EcmaScriptModuleResolver("typescript", "0.2.0-preview.6");
        var resultForward = resolver.Resolve(inventoryForward, CancellationToken.None);
        var resultBackward = resolver.Resolve(inventoryBackward, CancellationToken.None);
        Assert.Equal(
            resultForward.Edges.Select(Describe).OrderBy(value => value, StringComparer.Ordinal),
            resultBackward.Edges.Select(Describe).OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(resultForward.Candidates, resultBackward.Candidates);
        Assert.Equal(resultForward.CrossProjectTargets, resultBackward.CrossProjectTargets);
    }

    // ------------------------------------------------------------------
    // Q1-12: Cross-plugin references are declined in both directions and
    // never published as verified relationships.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe12_CrossPluginReferencesDeclineInBothDirections()
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-cross-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './helper.js';\nimport './orphan';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "helper.js"), "import './types';\nimport './ghost';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "types.ts"), "export const t = 1;\n");

        var repositoryId = new CodeRepositoryId("repo:q1-cross");
        var store = new InMemoryCodeGraphStore();
        var indexing = new CodeIndexingService(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin(), new TreeSitterJavaScriptPlugin()]),
            store);
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            new CodeIndexRunId("run:cross"));

        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
        var allEdges = (await store.GetPublishedUnitsAsync(repositoryId))
            .SelectMany(unit => unit.Edges)
            .ToArray();
        Assert.DoesNotContain(allEdges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);

        var queries = new CodeGraphQueryService(store);
        Assert.Equal(2, (await ReviewStore.Edges(queries, repositoryId, "typescript", "src/page.ts", CodeEdgeKinds.Imports)).Count);
        Assert.Equal(2, (await ReviewStore.Edges(queries, repositoryId, "javascript", "src/helper.js", CodeEdgeKinds.Imports)).Count);

        var typescript = Assert.Single(result.Diagnostics.Plugins, plugin => plugin.PluginId == TreeSitterTypeScriptPlugin.PluginId);
        var javascript = Assert.Single(result.Diagnostics.Plugins, plugin => plugin.PluginId == TreeSitterJavaScriptPlugin.PluginId);
        Assert.Equal(2, typescript.UnresolvedRelationships);
        Assert.Equal(2, javascript.UnresolvedRelationships);
    }

    // ------------------------------------------------------------------
    // Q1-14: Existing impact presets do not traverse resolves-to edges.
    // ------------------------------------------------------------------
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Probe14_ImpactPresetsExcludeResolvesTo(bool durable)
    {
        using var directory = ReviewTemporaryDirectory.Create("hetu-q1-impact-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q1-impact-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewStore.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        await ReviewStore.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:impact"));
        var queries = new CodeGraphQueryService(store);

        var impact = await queries.GetImpactSetAsync(repositoryId, FileNodeId("typescript", "src/target.ts"));
        Assert.DoesNotContain(impact.Nodes, node => node.Id == FileNodeId("typescript", "src/page.ts"));
        Assert.DoesNotContain(impact.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);

        var dependents = await queries.FindDependentsAsync(repositoryId, FileNodeId("typescript", "src/target.ts"));
        Assert.DoesNotContain(dependents.Nodes, node => node.Id == FileNodeId("typescript", "src/page.ts"));
        Assert.DoesNotContain(dependents.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
    }

    // ------------------------------------------------------------------
    // Q1-16/17/18: Extraction-level classification table coverage: .js
    // substitution refusal, out-of-policy extensions, directory index, .d.ts
    // suffix, re-export forms, and self-import internal counting.
    // ------------------------------------------------------------------
    [Fact]
    public async Task Probe16_ClassificationTableAtExtractionLevel()
    {
        const string importer = """
            import './js-only.js';
            import './config.json';
            import './widget.astro';
            import './types';
            import './dir';
            import './self';
            import './m';
            import './m.ts';
            import './types.d.ts';
            export type { T } from './types';
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/js-only.ts", "export const j = 1;"),
            ("src/types.d.ts", "export type T = string;"),
            ("src/dir/index.ts", "export const d = 1;"),
            ("src/self.ts", "import './self';\nexport const self = 1;"),
            ("src/m.tsx", "export const m = 1;"));

        var edges = ModuleEdges(extracted);
        Assert.Equal(7, edges.Length);
        Assert.Single(edges, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./types" &&
            Text(edge, CodePropertyKeys.ModuleReference) == "import" &&
            Text(edge, CodePropertyKeys.ResolutionRule) == "extension-substitution" &&
            edge.TargetId == FileNodeId("typescript", "src/types.d.ts"));
        Assert.Single(edges, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./dir" &&
            Text(edge, CodePropertyKeys.ResolutionRule) == "directory-index" &&
            edge.TargetId == FileNodeId("typescript", "src/dir/index.ts"));
        Assert.Single(edges, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./self" &&
            edge.SourceId == edge.TargetId);
        Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./m" && edge.TargetId == FileNodeId("typescript", "src/m.tsx"));
        var explicitExtension = Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./types.d.ts");
        Assert.Equal("exact", Text(explicitExtension, CodePropertyKeys.ResolutionRule));
        Assert.Equal(explicitExtension.TargetId, FileNodeId("typescript", "src/types.d.ts"));
        // `./m.ts` names a file that does not exist (only `m.tsx` does):
        // explicit supported extensions are exact-match only, so unresolved.
        Assert.DoesNotContain(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./m.ts");

        var typeOnly = Assert.Single(edges, edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./types" &&
            Text(edge, CodePropertyKeys.ModuleReference) == "export-type-from");
        Assert.True(typeOnly.Properties[CodePropertyKeys.TypeOnly] is CodeBooleanProperty { Value: true });

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(11, coverage.Candidates);
        Assert.Equal(1, coverage.InternalTargets);
        Assert.Equal(6, coverage.CrossProjectTargets);
        Assert.Equal(2, coverage.UnresolvedTargets);
        Assert.Equal(2, coverage.UnsupportedTargets);
        Assert.Equal(0, coverage.AmbiguousTargets);

        Assert.DoesNotContain(
            extracted.Edges.Select(edge => edge.Kind.Value),
            kind => kind is "exports" or "binds");
    }

    private static CodeGraphEdge[] ModuleEdges(Extraction extracted) =>
        extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
    private static string Describe(CodeGraphEdge edge)
    {
        var location = edge.Evidence.Location!;
        var properties = string.Join(";", edge.Properties
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"));
        return $"{edge.Id.Value}|{edge.SourceId.Value}|{edge.TargetId.Value}|{properties}|{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}";
    }

    private static void AssertIdentity(CodeGraphEdge edge, string language)
    {
        var location = edge.Evidence.Location!;
        var discriminator = $"resolves-to:{Text(edge, CodePropertyKeys.ModuleReference)}:{Text(edge, CodePropertyKeys.Specifier)}";
        var span = $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}";
        var expected = $"{language}:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{edge.Kind.Value}\n{edge.SourceId.Value}\n{edge.TargetId.Value}\n{discriminator}\n{span}")))}";
        Assert.Equal(expected, edge.Id.Value);
    }

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path)))}");

    private sealed class CancelOnFirstFileResolver(CancellationTokenSource cancellation) : ITreeSitterModuleResolver
    {
        public string Language => "typescript";

        public string Provider => EcmaScriptModuleResolver.ResolverProvider;

        public string ProviderVersion => "0.2.0-preview.6+resolver1.relative-v1";

        public TreeSitterModuleResolutionResult Resolve(
            IReadOnlyList<TreeSitterModuleFile> inventory,
            CancellationToken cancellationToken)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new InvalidOperationException("Cancellation was not observed by the resolver.");
        }
    }
}

internal static class ReviewStore
{
    internal sealed record StoreScope(ICodeGraphStore Store) : IDisposable
    {
        public void Dispose() => (Store as IDisposable)?.Dispose();
    }

    internal static StoreScope Create(bool durable, string databasePath) =>
        new(durable ? new LatticeCodeGraphStore(databasePath) : new InMemoryCodeGraphStore());

    internal static CodeIndexingService CreateIndexing(ICodeGraphStore store, ICodeGraphPlugin plugin) =>
        new(
            new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
            new CodeGraphPluginRegistry([plugin]),
            store);

    internal static async Task<IReadOnlyList<CodeGraphEdge>> ModuleEdges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string sourcePath) =>
        await Edges(queries, repositoryId, "typescript", sourcePath, CodeEdgeKinds.ResolvesTo);

    internal static async Task<IReadOnlyList<CodeGraphEdge>> Edges(
        CodeGraphQueryService queries,
        CodeRepositoryId repositoryId,
        string language,
        string sourcePath,
        CodeEdgeKind kind)
    {
        var result = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId(language, sourcePath),
            [kind],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        return result.Edges;
    }

    internal static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path)))}");
}

internal sealed class ReviewTemporaryDirectory : IDisposable
{
    private ReviewTemporaryDirectory(DirectoryInfo directory) => Root = directory;

    private DirectoryInfo Root { get; }

    internal string FullName => Root.FullName;

    internal static ReviewTemporaryDirectory Create(string prefix) =>
        new(Directory.CreateTempSubdirectory(prefix));

    public void Dispose() => Root.Delete(recursive: true);
}
