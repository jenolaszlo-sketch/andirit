using System.Security.Cryptography;
using System.Text;

namespace Penghou.Hetu.TreeSitter.Tests;

public sealed class TreeSitterModuleResolutionTests
{
    [Fact]
    public async Task ExtractAsync_EmitsPolicyScopedModuleEdgesAfterFileUnits()
    {
        const string importer = """
            import { projects } from "./projects";
            import type { Person } from "../types";
            import { type Person } from "../types";
            export { projects } from "./projects";
            export type { Person } from "../types";
            export * from "./folder";
            export * as tools from "./folder2";
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/projects.ts", "export const projects = [];"),
            ("types.d.ts", "export interface Person {}"),
            ("src/folder/index.ts", "export const item = 1;"),
            ("src/folder2/index.ts", "export const tool = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(7, edges.Length);
        Assert.All(edges, edge => Assert.Equal(EvidenceQuality.Resolved, edge.Evidence.Quality));
        Assert.All(edges, edge => Assert.Equal(
            "penghou.hetu.tree-sitter.ecmascript-module-resolver",
            edge.Evidence.Provider));
        Assert.All(edges, edge => Assert.Equal("src/page.ts", edge.Evidence.Location!.Path));
        Assert.All(edges, edge =>
        {
            Assert.Equal(CodeNodeKinds.File, extracted.Nodes.Single(node => node.Id == edge.SourceId).Kind);
            Assert.Equal(CodeNodeKinds.File, extracted.Nodes.Single(node => node.Id == edge.TargetId).Kind);
            Assert.Equal("relative-v1", Text(edge, CodePropertyKeys.ResolutionPolicy));
        });

        AssertEdge(edges, "./projects", "import", "extension-substitution", typeOnly: false, line: 1);
        AssertEdge(edges, "../types", "import-type", "extension-substitution", typeOnly: true, line: 2);
        AssertEdge(edges, "../types", "import", "extension-substitution", typeOnly: true, line: 3);
        AssertEdge(edges, "./projects", "export-from", "extension-substitution", typeOnly: false, line: 4);
        AssertEdge(edges, "../types", "export-type-from", "extension-substitution", typeOnly: true, line: 5);
        AssertEdge(edges, "./folder", "export-star", "directory-index", typeOnly: false, line: 6);
        AssertEdge(edges, "./folder2", "export-namespace-from", "directory-index", typeOnly: false, line: 7);

        var packageVersion = new TreeSitterTypeScriptPlugin().Version.Split('+', 2)[0];
        Assert.All(edges, edge => Assert.Equal(
            $"{packageVersion}+resolver2.relative-v1",
            edge.Evidence.ProviderVersion));

        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind.Value is "exports" or "binds");
        var structuralImports = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Imports).ToArray();
        Assert.Equal(7, structuralImports.Length);
        Assert.All(structuralImports, edge => Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));

        var moduleCoverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(CodeRelationshipCoverageState.Produced, moduleCoverage.State);
        Assert.Equal(7, moduleCoverage.Candidates);
        Assert.Equal(7, moduleCoverage.EdgesEmitted);
        Assert.Equal(0, moduleCoverage.InternalTargets);
        Assert.Equal(7, moduleCoverage.CrossProjectTargets);
        Assert.Equal(7, moduleCoverage.InternalTargets + moduleCoverage.CrossProjectTargets);

        var moduleBatches = extracted.Batches
            .Where(batch => batch.Origin.IndexUnitId.Value == "typescript:modules")
            .ToArray();
        Assert.NotEmpty(moduleBatches);
        Assert.All(moduleBatches, batch =>
        {
            Assert.Empty(batch.Nodes);
            Assert.Empty(batch.Declarations);
        });
        Assert.True(moduleBatches[^1].CompletesIndexUnit);
        Assert.Equal(7, moduleBatches.SelectMany(batch => batch.Edges).Count());
        var firstModuleBatchIndex = Array.FindIndex(
            extracted.Batches.ToArray(),
            batch => batch.Origin.IndexUnitId.Value == "typescript:modules");
        Assert.All(
            extracted.Batches.Take(firstModuleBatchIndex),
            batch => Assert.NotEqual("typescript:modules", batch.Origin.IndexUnitId.Value));
    }

    [Fact]
    public void Resolver_RelativeV1UsesOrderedSuffixesAndClassifiesWithoutFilesystemFallback()
    {
        var importer = File("typescript", "src/page.ts");
        var inventory = new List<TreeSitterModuleFile>
        {
            importer,
            File("typescript", "src/mod.ts"),
            File("typescript", "src/mod.tsx"),
            File("typescript", "src/dir.ts"),
            File("typescript", "src/mod/index.ts"),
            File("typescript", "src/dir/index.ts"),
            File("typescript", "src/same.ts"),
            File("typescript", "types.d.ts"),
            File("typescript", "src/Case.ts"),
            File("typescript", "src/component.ts")
        };
        var specifiers = new string?[]
        {
            "./mod", "./dir", "./same.ts", "../types", "././same", "./Case",
            "./case", "./component.js", "./missing", "./config.json", "./widget.astro",
            "../../outside", "./", "./a/", "./a//b", @".\a", @"./a\b",
            "./x?raw", "./x#fragment", "/abs/x", "zod", "node:fs",
            "https://example.test/a", "@/alias", "#internal", ".", "..", "./page.ts",
            new string('x', 257)
        };
        importer = importer with
        {
            References = specifiers.Select((specifier, index) => Reference(specifier, index + 1)).ToArray()
        };
        inventory[0] = importer;

        var result = new EcmaScriptModuleResolver("typescript", "0.2.0-preview.6")
            .Resolve(inventory, CancellationToken.None);

        Assert.Equal(specifiers.Length, result.Candidates);
        Assert.Equal(7, result.Edges.Count);
        Assert.Equal(6, result.CrossProjectTargets);
        Assert.Equal(1, result.InternalTargets);
        Assert.Equal(3, result.UnresolvedTargets);
        Assert.Equal(7, result.ExternalTargets);
        Assert.Equal(12, result.UnsupportedTargets);
        Assert.Equal(0, result.AmbiguousTargets);
        Assert.Equal("src/mod.ts", TargetPath(result.Edges, "./mod", inventory));
        Assert.Equal("src/dir.ts", TargetPath(result.Edges, "./dir", inventory));
        Assert.Equal("src/same.ts", TargetPath(result.Edges, "./same.ts", inventory));
        Assert.Equal("types.d.ts", TargetPath(result.Edges, "../types", inventory));
        Assert.Equal("src/same.ts", TargetPath(result.Edges, "././same", inventory));
        Assert.Equal("src/page.ts", TargetPath(result.Edges, "./page.ts", inventory));
        Assert.DoesNotContain(result.Edges, edge => Text(edge, CodePropertyKeys.Specifier).Length > 256);
    }

    [Fact]
    public void Resolver_RelativeV1UsesOrderedJavaScriptSuffixesForFilesAndIndexes()
    {
        var importer = File("javascript", "src/page.js") with
        {
            References = [Reference("./module", 1), Reference("./folder", 2)]
        };
        var inventory = new[]
        {
            importer,
            File("javascript", "src/module.js"),
            File("javascript", "src/module.jsx"),
            File("javascript", "src/module.mjs"),
            File("javascript", "src/module.cjs"),
            File("javascript", "src/folder/index.js"),
            File("javascript", "src/folder/index.jsx"),
            File("javascript", "src/folder/index.mjs"),
            File("javascript", "src/folder/index.cjs")
        };

        var result = new EcmaScriptModuleResolver("javascript", "0.2.0-preview.6")
            .Resolve(inventory, CancellationToken.None);

        Assert.Equal(2, result.Edges.Count);
        Assert.Equal("src/module.js", TargetPath(result.Edges, "./module", inventory));
        Assert.Equal("src/folder/index.js", TargetPath(result.Edges, "./folder", inventory));
        Assert.Equal("extension-substitution", Text(
            Assert.Single(result.Edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./module"),
            CodePropertyKeys.ResolutionRule));
        Assert.Equal("directory-index", Text(
            Assert.Single(result.Edges, edge => Text(edge, CodePropertyKeys.Specifier) == "./folder"),
            CodePropertyKeys.ResolutionRule));
    }

    [Fact]
    public async Task ExtractAsync_CapturesLiteralRequireAndRejectsDynamicAndTemplateForms()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/page.js", "const exact = require('./lib.mjs');\nconst dynamic = import('./lazy.js');\nconst template = require(`./template`);"),
            ("src/lib.mjs", "export const exact = true;"),
            ("src/lazy.js", "export const lazy = true;"),
            ("src/template.js", "export const template = true;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        var edge = Assert.Single(edges);
        Assert.Equal("./lib.mjs", Text(edge, CodePropertyKeys.Specifier));
        Assert.Equal("require", Text(edge, CodePropertyKeys.ModuleReference));
        Assert.Equal("exact", Text(edge, CodePropertyKeys.ResolutionRule));
        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(3, coverage.Candidates);
        Assert.Equal(2, coverage.UnsupportedTargets);
    }

    [Fact]
    public async Task ExtractAsync_IdentityPreservesOccurrencesAndIsLineEndingStable()
    {
        const string source = "import { first } from './shared';\nimport { second } from './shared';\nimport { third } from './shared.ts';";
        var targets = new (string Path, string Content)[]
        {
            ("src/use.ts", source),
            ("src/shared.ts", "export const first = 1;")
        };
        var first = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), targets);
        var repeat = await TreeSitterTestSupport.ExtractAsync(new TreeSitterTypeScriptPlugin(), targets.Reverse().ToArray());
        var crlf = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", source.Replace("\n", "\r\n", StringComparison.Ordinal)),
            ("src/shared.ts", "export const first = 1;"));

        var edges = first.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(3, edges.Length);
        Assert.Equal(
            JsonEdgeIdentities(edges),
            JsonEdgeIdentities(repeat.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)));
        Assert.Equal(
            JsonEdgeIdentities(edges),
            JsonEdgeIdentities(crlf.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo)));
        Assert.All(edges, AssertIdentity);
        Assert.Equal(2, edges.Count(edge => Text(edge, CodePropertyKeys.Specifier) == "./shared"));
        Assert.Equal(3, edges.Select(edge => edge.Id.Value).Distinct(StringComparer.Ordinal).Count());

        var oneOccurrence = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", "import { first } from './shared';"),
            ("src/shared.ts", "export const first = 1;"));
        var oneEdge = Assert.Single(oneOccurrence.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(edges.Single(edge =>
            Text(edge, CodePropertyKeys.Specifier) == "./shared" &&
            edge.Evidence.Location!.StartLine == 1).Id, oneEdge.Id);

        var shifted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", "// shifted\n" + source),
            ("src/shared.ts", "export const first = 1;"));
        var shiftedEdges = shifted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(3, shiftedEdges.Length);
        Assert.All(edges, previous => Assert.DoesNotContain(
            shiftedEdges,
            current => current.Id == previous.Id));
    }

    [Fact]
    public async Task ExtractAsync_IdentifiesSharedTargetsAcrossImporterFiles()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "import './shared';"),
            ("src/b.ts", "import './shared';"),
            ("src/shared.ts", "export const shared = true;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(2, edges.Length);
        Assert.Equal(2, edges.Select(edge => edge.SourceId).Distinct().Count());
        Assert.Single(edges.Select(edge => edge.TargetId).Distinct());
    }

    [Fact]
    public void Resolver_RejectsIdentityCollisionWithConflictingProperties()
    {
        var importer = File("typescript", "src/page.ts") with
        {
            References =
            [
                Reference("./target", 1),
                Reference("./target", 1) with { TypeOnly = true }
            ]
        };
        var inventory = new[] { importer, File("typescript", "src/target.ts") };
        var resolver = new EcmaScriptModuleResolver("typescript", "0.2.0-preview.6");

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(inventory, CancellationToken.None));
    }

    [Fact]
    public async Task ExtractAsync_UsesOnlyTheOwningPluginInventory()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", "import './runtime.js';"),
            ("src/runtime.ts", "export const runtime = true;"));

        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(1, coverage.UnresolvedTargets);
        Assert.Equal(0, coverage.EdgesEmitted);
        Assert.Equal(1, extracted.Result.UnresolvedRelationships);
    }

    [Fact]
    public async Task ExtractAsync_MarksResolverCoveragePartialForParseAndDecodeErrors()
    {
        const string sourceText = "import './target';\nexport const = ;";
        const string targetText = "export const target = true;";
        var invalidUtf8 = new byte[] { 0xC3, 0x28 };
        var sources = new[]
        {
            new CodeGraphSource(
                "src/page.ts",
                $"sha256:{TreeSitterTestSupport.Hash(sourceText)}",
                _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(sourceText)))),
            new CodeGraphSource(
                "src/target.ts",
                $"sha256:{TreeSitterTestSupport.Hash(targetText)}",
                _ => new ValueTask<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(targetText)))),
            new CodeGraphSource(
                "src/broken.ts",
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(invalidUtf8))}",
                _ => new ValueTask<Stream>(new MemoryStream(invalidUtf8)))
        };
        var context = new CodeGraphPluginContext(
            new CodeRepositoryId("repo:partial-resolution"),
            "memory://partial-resolution",
            new CodeIndexRunId("run:partial-resolution"),
            sources);
        var sink = new RecordingSink(new CodeGraphBatchLimits());
        await using var session = await new TreeSitterTypeScriptPlugin().CreateSessionAsync(context);

        var result = await session.ExtractAsync(sink);

        var coverage = Assert.Single(
            result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(CodeRelationshipCoverageState.Partial, coverage.State);
        Assert.Equal(1, coverage.Candidates);
        Assert.Equal(1, coverage.EdgesEmitted);
        Assert.Contains("typescript.parse-error", result.WarningCodes);
        Assert.Contains("typescript.undecodable-source", result.WarningCodes);
        Assert.DoesNotContain(
            sink.Batches.SelectMany(batch => batch.Nodes),
            node => node.Id == FileNodeId("typescript", "src/broken.ts"));
    }

    [Fact]
    public async Task ExtractAsync_RejectsImportEqualsAndExportEqualsResolution()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", "import item = require('./item');\nimport spaced = require ('./item');\nexport = item;"),
            ("src/item.ts", "export const item = true;"));

        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.ResolvesTo);
        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(3, coverage.Candidates);
        Assert.Equal(3, coverage.UnsupportedTargets);
    }

    // EC6B-GLM-Q1 probe 18: rejected path forms never resolve at the capture
    // level; single-dot segments normalize deterministically.
    [Fact]
    public async Task ExtractAsync_RejectedPathFormsNeverResolveAtCaptureLevel()
    {
        const string importer = """
            import './a\b';
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

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal("./a/./b", Text(edge, CodePropertyKeys.Specifier));
        Assert.Equal("src/a/b.ts", extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName);

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(8, coverage.Candidates);
        Assert.Equal(7, coverage.UnsupportedTargets);
        Assert.Equal(0, coverage.UnresolvedTargets);
        Assert.Equal(0, coverage.ExternalTargets);
    }

    // EC6B-GLM-F02: escaped specifiers are rejected, never truncated into a
    // resolves-to edge.
    [Fact]
    public async Task ExtractAsync_EscapedSpecifiersAreUnsupportedAndNeverResolve()
    {
        const string importer = """
            import './a\b';
            import './a\\b';
            import './caf\u00e9';
            import './quote\'name';
            import './tab\tname';
            import './plain';
            const required = require('./a\b');
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", importer),
            ("src/a.ts", "export const a = 1;"),
            ("src/caf.ts", "export const caf = 1;"),
            ("src/plain.ts", "export const plain = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        var edge = Assert.Single(edges);
        Assert.Equal("./plain", Text(edge, CodePropertyKeys.Specifier));
        Assert.Equal(FileNodeId("typescript", "src/plain.ts"), edge.TargetId);

        // Truncated prefixes (`./a`, `./caf`, ...) must never be targets.
        Assert.DoesNotContain(edges, candidate => candidate.TargetId == FileNodeId("typescript", "src/a.ts"));
        Assert.DoesNotContain(edges, candidate => candidate.TargetId == FileNodeId("typescript", "src/caf.ts"));

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(7, coverage.Candidates);
        Assert.Equal(6, coverage.UnsupportedTargets);
        Assert.Equal(1, coverage.EdgesEmitted);
        Assert.Equal(0, coverage.UnresolvedTargets);
    }

    // EC6B-GLM-F03: a binding literally named `type` is not type-only syntax.
    [Theory]
    [InlineData("import { type } from './m';", "import")]
    [InlineData("import { type as t } from './m';", "import")]
    [InlineData("import type from './m';", "import")]
    [InlineData("export { type as u } from './m';", "export-from")]
    public async Task ExtractAsync_BindingNamedTypeIsNotTypeOnly(string source, string moduleReference)
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/m.ts", "export const type = 1;"));

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(moduleReference, Text(edge, CodePropertyKeys.ModuleReference));
        Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
    }

    // EC6B-GLM-F03: genuine explicit type-only syntax is preserved.
    [Theory]
    [InlineData("import type { T } from './m';", "import-type")]
    [InlineData("import { type T } from './m';", "import")]
    [InlineData("import { type T as U } from './m';", "import")]
    [InlineData("export type { T } from './m';", "export-type-from")]
    [InlineData("export { type T } from './m';", "export-from")]
    [InlineData("export { type T as U } from './m';", "export-from")]
    [InlineData("export type * from './m';", "export-type-from")]
    [InlineData("export type * as ns from './m';", "export-type-from")]
    public async Task ExtractAsync_ExplicitTypeOnlySyntaxStaysTypeOnly(string source, string moduleReference)
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/m.ts", "export type T = string;"));

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(moduleReference, Text(edge, CodePropertyKeys.ModuleReference));
        Assert.True(edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value) &&
            value is CodeBooleanProperty { Value: true });
    }

    [Theory]
    [InlineData("import { type T, V } from './m';", "import")]
    [InlineData("export { type T, V } from './m';", "export-from")]
    public async Task ExtractAsync_MixedTypeAndValueBindingsAreNotWhollyTypeOnly(
        string source,
        string moduleReference)
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/page.ts", source),
            ("src/m.ts", "export type T = string;\nexport const V = 1;"));

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal(moduleReference, Text(edge, CodePropertyKeys.ModuleReference));
        Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
    }

    // EC6B-GLM-F03: JavaScript has no inline type modifier; `type` is an
    // ordinary binding name there.
    [Fact]
    public async Task ExtractAsync_JavaScriptBindingNamedTypeIsNotTypeOnly()
    {
        const string source = "import { type } from './m';\nimport { type as t } from './m';\nexport { type as u } from './m';";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/page.js", source),
            ("src/m.js", "export const type = 1;"));

        var edges = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.ResolvesTo).ToArray();
        Assert.Equal(3, edges.Length);
        Assert.All(edges, edge =>
        {
            Assert.Contains(Text(edge, CodePropertyKeys.ModuleReference), new[] { "import", "export-from" });
            Assert.False(edge.Properties.ContainsKey(CodePropertyKeys.TypeOnly));
        });
    }

    // EC6B-GLM-F04: the literal require inside an unsupported `export =`
    // form is a separate supported site; the outer form stays unsupported.
    [Fact]
    public async Task ExtractAsync_LiteralRequireNestedInExportEqualsIsCaptured()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/legacy.ts", "export = require('./item');"),
            ("src/item.ts", "export const item = true;"));

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        Assert.Equal("require", Text(edge, CodePropertyKeys.ModuleReference));
        Assert.Equal("./item", Text(edge, CodePropertyKeys.Specifier));
        Assert.Equal(FileNodeId("typescript", "src/item.ts"), edge.TargetId);
        Assert.DoesNotContain(extracted.Edges, candidate => candidate.Kind.Value is "exports" or "binds");

        var coverage = Assert.Single(
            extracted.Result.RelationshipCoverage,
            item => item.RelationshipKind == CodeEdgeKinds.ResolvesTo.Value);
        Assert.Equal(2, coverage.Candidates);
        Assert.Equal(1, coverage.UnsupportedTargets);
        Assert.Equal(1, coverage.EdgesEmitted);
    }

    // EC6B-GLM-F01: published columns are one-based UTF-16 code units.
    [Fact]
    public async Task ExtractAsync_ReportsUtf16CodeUnitColumns()
    {
        const string line = "const s = \"\u4E2D\u6587 caf\u00E9 \U0001F600\"; import './a';";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/use.ts", line),
            ("src/a.ts", "export const a = 1;"));

        var edge = Assert.Single(
            extracted.Edges,
            candidate => candidate.Kind == CodeEdgeKinds.ResolvesTo);
        var start = line.IndexOf("import", StringComparison.Ordinal);
        var utf8Start = Encoding.UTF8.GetByteCount(line[..start]);
        var location = edge.Evidence.Location!;
        Assert.Equal(1, location.StartLine);
        Assert.Equal(start + 1, location.StartColumn);
        Assert.NotEqual(utf8Start + 1, location.StartColumn);
        Assert.Equal(start + "import './a';".Length + 1, location.EndColumn);
        AssertIdentity(edge);
    }

    private static TreeSitterModuleFile File(string language, string path) =>
        new(path, FileNodeId(language, path), []);

    private static TreeSitterModuleReference Reference(string? specifier, int line) =>
        new(specifier, "import", false, new CodeLocation("src/page.ts", line, 1, line, 10));

    private static CodeNodeId FileNodeId(string language, string path) =>
        new($"{language}:file:{Hash(path)}");

    private static string TargetPath(
        IReadOnlyList<CodeGraphEdge> edges,
        string specifier,
        IReadOnlyList<TreeSitterModuleFile> inventory)
    {
        var edge = Assert.Single(edges, candidate => Text(candidate, CodePropertyKeys.Specifier) == specifier);
        return inventory.Single(file => file.FileNodeId == edge.TargetId).Path;
    }

    private static void AssertEdge(
        IReadOnlyList<CodeGraphEdge> edges,
        string specifier,
        string moduleReference,
        string rule,
        bool typeOnly,
        int line)
    {
        var edge = Assert.Single(edges, candidate =>
            Text(candidate, CodePropertyKeys.Specifier) == specifier &&
            Text(candidate, CodePropertyKeys.ModuleReference) == moduleReference &&
            candidate.Evidence.Location?.StartLine == line);
        Assert.Equal(rule, Text(edge, CodePropertyKeys.ResolutionRule));
        Assert.Equal(typeOnly, edge.Properties.TryGetValue(CodePropertyKeys.TypeOnly, out var value)
            && value is CodeBooleanProperty { Value: true });
    }

    private static void AssertIdentity(CodeGraphEdge edge)
    {
        var location = edge.Evidence.Location!;
        var discriminator = $"resolves-to:{Text(edge, CodePropertyKeys.ModuleReference)}:{Text(edge, CodePropertyKeys.Specifier)}";
        var span = $"{location.StartLine}:{location.StartColumn}:{location.EndLine}:{location.EndColumn}";
        var expected = $"typescript:{Hash($"{edge.Kind.Value}\n{edge.SourceId.Value}\n{edge.TargetId.Value}\n{discriminator}\n{span}")}";
        Assert.Equal(expected, edge.Id.Value);
    }

    private static string JsonEdgeIdentities(IEnumerable<CodeGraphEdge> edges) =>
        string.Join("\n", edges.OrderBy(edge => edge.Id.Value, StringComparer.Ordinal)
            .Select(edge => $"{edge.Id.Value}|{edge.Properties[CodePropertyKeys.Specifier]}|{edge.Evidence.Location}"));

    private static string Text(CodeGraphEdge edge, string key) =>
        Assert.IsType<CodeTextProperty>(edge.Properties[key]).Value;

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
