namespace Penghou.Hetu.TreeSitter.Tests;

using System.Text;

// Independent EC6B-GLM-Q2 closure probes for F01-F04 and graph compatibility.
public sealed class ReviewEc6bGlmQ2Probes
{
    // Q2-A/Q2-B: escaped specifiers never produce resolved edges.
    [Fact]
    public async Task ProbeQ2B_EscapedSpecifiersNeverResolve()
    {
        const string importer = """
            import './a\b';
            import './a\\b';
            import './caf\u00e9';
            import './quote\'q';
            import './tab\tx';
            import './a.ts';
            import './plain';
            export { x } from './a\b';
            const required = require('./a\b');
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/a.ts", "export const a = 1;"),
            ("src/caf.ts", "export const caf = 1;"),
            ("src/plain.ts", "export const plain = 1;"));

        var edges = ModuleEdges(extracted);
        Assert.Equal(2, edges.Length);
        Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./a.ts");
        Assert.Single(edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./plain");
        Assert.All(edges, edge => Assert.DoesNotContain(
            "\\", Text(edge, CodePropertyKeys.Specifier), StringComparison.Ordinal));
        Assert.DoesNotContain(edges, edge => edge.TargetId == FileNodeId("typescript", "src/caf.ts"));

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(9, coverage.Candidates);
        Assert.Equal(7, coverage.UnsupportedTargets);
        Assert.Equal(2, coverage.EdgesEmitted);
        Assert.Equal(0, coverage.UnresolvedTargets);

        // Structural imports observations remain present and Structural.
        Assert.All(
            extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Imports),
            edge => Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    // Q2-B publication check: escaped-only repository publishes no resolver edge.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2B_EscapedOnlyRepositoryPublishesNoResolvedEdge(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2-escape-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './a\\b';\nimport './a\\\\b';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "a.ts"), "export const a = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:q2-escape-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var indexing = ReviewQ2Store.CreateIndexing(storeScope.Store, new TreeSitterTypeScriptPlugin());
        var result = await indexing.IndexAsync(
            new CodeRepositoryDescriptor(repositoryId, directory.FullName),
            new CodeIndexRunId("run:q2-escape"));
        Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

        var units = await storeScope.Store.GetPublishedUnitsAsync(repositoryId);
        Assert.DoesNotContain(units.SelectMany(unit => unit.Edges), edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.NotNull(await storeScope.Store.GetNodeAsync(repositoryId, FileNodeId("typescript", "src/a.ts")));
        Assert.Equal(0, Assert.Single(result.Diagnostics.Plugins).UnresolvedRelationships);
    }

    // Q2-A/Q2-C: grammar-aware type-only classification.
    [Theory]
    [InlineData("import { type } from './m';", "import", false)]
    [InlineData("import { type as t } from './m';", "import", false)]
    [InlineData("import type from './m';", "import", false)]
    [InlineData("export { type as u } from './m';", "export-from", false)]
    [InlineData("import { type T } from './m';", "import", true)]
    [InlineData("import { type T as U } from './m';", "import", true)]
    [InlineData("import type { T } from './m';", "import-type", true)]
    [InlineData("import type D from './m';", "import-type", true)]
    [InlineData("import type * as ns from './m';", "import-type", true)]
    [InlineData("export { type T } from './m';", "export-from", true)]
    [InlineData("export { type T as U } from './m';", "export-from", true)]
    [InlineData("export type { T } from './m';", "export-type-from", true)]
    [InlineData("export type * from './m';", "export-type-from", true)]
    [InlineData("export type * as ns from './m';", "export-type-from", true)]
    [InlineData("export { type \"x\" } from './m';", "export-from", true)]
    [InlineData("export { type \"x\" as y } from './m';", "export-from", true)]
    [InlineData("import { type T, V } from './m';", "import", false)]
    [InlineData("export { type T, V } from './m';", "export-from", false)]
    public async Task ProbeQ2C_TypeOnlyMatrix(string source, string moduleReference, bool typeOnly)
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/m.ts", "export type T = string;\nexport const V = 1;\nexport const type = 1;"));

        var edge = Assert.Single(ModuleEdges(extracted));
        Assert.Equal(moduleReference, Text(edge, CodePropertyKeys.ModuleReference));
        var hasTypeOnly = edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
            value is CodeBooleanProperty { Value: true };
        Assert.Equal(typeOnly, hasTypeOnly);
    }

    [Fact]
    public async Task ProbeQ2C_JavaScriptTypeBindingsStayOrdinary()
    {
        const string source = "import { type } from './m';\nimport { type as t } from './m';\nexport { type as u } from './m';";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/page.js", source),
            ("src/m.js", "export const type = 1;"));

        var edges = ModuleEdges(extracted);
        Assert.Equal(3, edges.Length);
        Assert.All(edges, edge => Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly)));
    }

    // Q2-C: pathological grammar forms stay conservative (no false type-only).
    [Theory]
    [InlineData("export { type as } from './m';")]
    [InlineData("export { type as as } from './m';")]
    [InlineData("import { type as as as } from './m';")]
    public async Task ProbeQ2C_PathologicalGrammarFormsAreConservative(string source)
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/m.ts", "export const type = 1;"));

        var edge = Assert.Single(ModuleEdges(extracted));
        Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
    }

    // Q2-A/Q2-D: UTF-16 coordinates survive extraction, identity, publication
    // and durable reopen; no byte conversion exists.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2A_Utf16ColumnsSurvivePublication(bool durable)
    {
        const string line = "const s = \"\u4E2D\u6587 caf\u00E9 \U0001F600\"; import './a';";
        var start = line.IndexOf("import", StringComparison.Ordinal);
        var utf8Start = Encoding.UTF8.GetByteCount(line[..start]);
        Assert.NotEqual(start, utf8Start);

        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2-columns-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "use.ts"), line + "\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "a.ts"), "export const a = 1;\n");

        var repositoryId = new CodeRepositoryId($"repo:q2-columns-{(durable ? "lattice" : "memory")}");
        var databasePath = Path.Combine(directory.FullName, "graph.db");
        using (var storeScope = ReviewQ2Store.Create(durable, databasePath))
        {
            var indexing = ReviewQ2Store.CreateIndexing(storeScope.Store, new TreeSitterTypeScriptPlugin());
            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                new CodeIndexRunId("run:q2-columns"));
            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

            var edge = Assert.Single(
                (await storeScope.Store.GetPublishedUnitsAsync(repositoryId)).SelectMany(unit => unit.Edges),
                candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
            var location = edge.Evidence.Location!;
            Assert.Equal(start + 1, location.StartColumn);
            Assert.NotEqual(utf8Start + 1, location.StartColumn);
            Assert.Equal(start + "import './a';".Length + 1, location.EndColumn);
            AssertIdentity(edge);
        }

        if (durable)
        {
            using var reopened = new LatticeCodeGraphStore(databasePath);
            var edge = Assert.Single(
                (await reopened.GetPublishedUnitsAsync(repositoryId)).SelectMany(unit => unit.Edges),
                candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
            Assert.Equal(start + 1, edge.Evidence.Location!.StartColumn);
        }
    }

    // Q2-D: structural imports and impact presets remain compatible.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProbeQ2D_CompatibilityOfExistingQueries(bool durable)
    {
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2-compat-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "page.ts"), "import './target';\n");
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "target.ts"), "export const target = true;\n");

        var repositoryId = new CodeRepositoryId($"repo:q2-compat-{(durable ? "lattice" : "memory")}");
        using var storeScope = ReviewQ2Store.Create(durable, Path.Combine(directory.FullName, "graph.db"));
        var store = storeScope.Store;
        await ReviewQ2Store.CreateIndexing(store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:q2-compat"));
        var queries = new CodeGraphQueryService(store);

        var imports = await queries.GetNeighborhoodAsync(
            repositoryId,
            FileNodeId("typescript", "src/page.ts"),
            [CodeEdgeKinds.Imports],
            new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 100));
        var importsEdge = Assert.Single(imports.Edges);
        Assert.Equal(EvidenceQuality.Structural, importsEdge.Evidence.Quality);

        var impact = await queries.GetImpactSetAsync(repositoryId, FileNodeId("typescript", "src/target.ts"));
        Assert.DoesNotContain(impact.Nodes, node => node.Id == FileNodeId("typescript", "src/page.ts"));
        Assert.DoesNotContain(impact.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);

        var dependents = await queries.FindDependentsAsync(repositoryId, FileNodeId("typescript", "src/target.ts"));
        Assert.DoesNotContain(dependents.Nodes, node => node.Id == FileNodeId("typescript", "src/page.ts"));

        var units = await store.GetPublishedUnitsAsync(repositoryId);
        Assert.DoesNotContain(units.SelectMany(unit => unit.Edges), edge => edge.Kind.Value is "exports" or "binds");
        Assert.DoesNotContain(units.SelectMany(unit => unit.Nodes), node => node.Kind.Value == "module");
    }

    // Q2-D: determinism including reversed enumeration and LF/CRLF.
    [Fact]
    public async Task ProbeQ2D_DeterministicFacts()
    {
        const string importer = "import './a\\b';\nimport { type } from './m';\nimport './plain';\n";
        var sources = new (string Path, string Content)[]
        {
            ("src/page.ts", importer),
            ("src/a.ts", "export const a = 1;"),
            ("src/m.ts", "export const type = 1;"),
            ("src/plain.ts", "export const plain = 1;")
        };
        var forward = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), sources);
        var reversed = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(), sources.Reverse().ToArray());
        var crlf = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer.Replace("\n", "\r\n", StringComparison.Ordinal)),
            ("src/a.ts", "export const a = 1;"),
            ("src/m.ts", "export const type = 1;"),
            ("src/plain.ts", "export const plain = 1;"));

        var first = ModuleEdges(forward).Select(Describe).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Assert.Equal(2, first.Length);
        Assert.Equal(
            first,
            ModuleEdges(reversed).Select(Describe).OrderBy(value => value, StringComparer.Ordinal));
        Assert.Equal(
            first,
            ModuleEdges(crlf).Select(Describe).OrderBy(value => value, StringComparer.Ordinal));
    }

    // Q2-A/F04: nested require in export= is clarified and pinned.
    [Fact]
    public async Task ProbeQ2A_NestedRequireBehavior()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/legacy.ts", "export = require('./item');\nimport item = require('./item');\nexport = item;"),
            ("src/item.ts", "export const item = true;"));

        var edges = ModuleEdges(extracted);
        var edge = Assert.Single(edges);
        Assert.Equal("require", Text(edge, CodePropertyKeys.ModuleReference));
        Assert.Equal("./item", Text(edge, CodePropertyKeys.Specifier));
        Assert.Equal(FileNodeId("typescript", "src/item.ts"), edge.TargetId);
        Assert.DoesNotContain(extracted.Edges, candidate => candidate.Kind.Value is "exports" or "binds");

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(4, coverage.Candidates);
        Assert.Equal(3, coverage.UnsupportedTargets);
        Assert.Equal(1, coverage.EdgesEmitted);
    }

    // Q2-A: the consumer position API uses the same UTF-16 coordinate system
    // as published evidence locations.
    [Fact]
    public async Task ProbeQ2A_EnclosingSymbolUsesUtf16Coordinates()
    {
        const string line = "const s = \"\u5341\u4E2A\u4E2D\u6587\u5B57\u7B26\u6D4B\u8BD5\"; const x = 1;";
        using var directory = ReviewQ2TemporaryDirectory.Create("hetu-q2-consumer-");
        var source = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
        await File.WriteAllTextAsync(Path.Combine(source.FullName, "use.ts"), line + "\n");

        var repositoryId = new CodeRepositoryId("repo:q2-consumer");
        using var storeScope = ReviewQ2Store.Create(false, "");
        await ReviewQ2Store.CreateIndexing(storeScope.Store, new TreeSitterTypeScriptPlugin())
            .IndexAsync(new CodeRepositoryDescriptor(repositoryId, directory.FullName), new CodeIndexRunId("run:q2-consumer"));
        var queries = new CodeGraphQueryService(storeScope.Store);

        var start = line.IndexOf("x = 1", StringComparison.Ordinal);
        var utf16Column = start + 1;
        var utf8Column = Encoding.UTF8.GetByteCount(line[..start]) + 1;
        Assert.True(utf8Column > utf16Column + 8);

        var native = await queries.FindEnclosingSymbolAsync(repositoryId, "src/use.ts", 1, utf16Column);
        Assert.NotNull(native);
        Assert.Equal("x", native!.Symbol.Name);

        var bytePosition = await queries.FindEnclosingSymbolAsync(repositoryId, "src/use.ts", 1, utf8Column);
        Assert.Null(bytePosition);
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

    private static void AssertIdentity(CodeGraphEdge edge)
    {
        var location = edge.Evidence.Location!;
        var discriminator = $"resolves-to:{Text(edge, CodePropertyKeys.ModuleReference)}:{Text(edge, CodePropertyKeys.Specifier)}";
        var span = $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}";
        var expected = $"typescript:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(
                $"{edge.Kind.Value}\n{edge.SourceId.Value}\n{edge.TargetId.Value}\n{discriminator}\n{span}")))}";
        Assert.Equal(expected, edge.Id.Value);
    }

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Convert.ToHexStringLower(
            System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(path)))}");
}

internal static class ReviewQ2Store
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
}

internal sealed class ReviewQ2TemporaryDirectory : IDisposable
{
    private ReviewQ2TemporaryDirectory(DirectoryInfo directory) => Root = directory;

    private DirectoryInfo Root { get; }

    internal string FullName => Root.FullName;

    internal static ReviewQ2TemporaryDirectory Create(string prefix) =>
        new(Directory.CreateTempSubdirectory(prefix));

    public void Dispose() => Root.Delete(recursive: true);
}
