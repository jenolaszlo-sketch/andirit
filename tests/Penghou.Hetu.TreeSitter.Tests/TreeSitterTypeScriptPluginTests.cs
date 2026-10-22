using System.Reflection;
using System.Text.Json;

namespace Penghou.Hetu.TreeSitter.Tests;

public sealed class TreeSitterTypeScriptPluginTests
{
    private const string TypeScriptSource = """
        import { readFile } from "node:fs";
        import * as path from "node:path";

        export const VERSION = "1.0";

        export interface Shape {
          area(): number;
        }

        export type Id = string | number;

        export enum Color {
          Red,
          Green,
        }

        export abstract class Widget implements Shape {
          private count: number = 0;

          constructor(public name: string) {}

          area(): number {
            return this.count;
          }

          static create(name: string): Widget {
            return new Widget(name);
          }
        }

        export function makeWidget(name: string): Widget {
          const helper = (x: number) => x + 1;
          helper(1);
          return Widget.create(name);
        }

        const render = (w: Widget) => w.area();

        export namespace Geometry {
          export function distance(): number {
            return 0;
          }
        }

        readFile("x", () => {});
        path.join("a", "b");
        """;

    private const string TsxSource = """
        export function App() {
          return <div onClick={() => handleClick()}>{render()}</div>;
        }
        """;

    private const string HeritageSource = """
        class Base {}
        interface I1 {}
        interface I2 {}
        class Child extends Base implements I1, I2 {}
        interface I3 extends I1 {}
        """;

    private const string BrokenSource = """
        export class Broken {
          run() {
            return 1;
          }
        """;

    private const string InterfaceSource = """
        export interface Shape {
          area(): number;
          readonly id: string;
          timeout?: number;
          readonly cache?: string;
        }
        """;

    private const string InterfaceOverloadSource = """
        interface Parser {
          parse(input: string): string;
          parse(input: number): number;
        }
        """;

    private const string DuplicateMemberSource = """
        interface Reader {
          read(): string;
        }
        interface Writer {
          read(): string;
        }
        """;

    private const string UnsupportedMemberSource = """
        interface Unsupported {
          [key: string]: unknown;
          (value: string): number;
          new (): Unsupported;
          [Symbol.iterator](): Iterator<string>;
          "literal-name": number;
          0: number;
        }
        """;

    private const string BrokenInterfaceSource = """
        export interface Broken {
          ok(): number;
          bad(
        }
        """;

    [Fact]
    public async Task ExtractAsync_ModelsClassesInterfacesAndContainment()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", TypeScriptSource));

        var widget = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Widget");
        Assert.Equal("Widget", widget.Name);
        Assert.NotNull(widget.SymbolId);

        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Shape");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Id");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Color");
        var geometry = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Namespace && node.QualifiedName == "Geometry");

        var area = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Widget.area");
        var makeWidget = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "makeWidget");
        var distance = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Geometry.distance");

        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == widget.Id &&
                edge.TargetId == area.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == geometry.Id &&
                edge.TargetId == distance.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Declares &&
                edge.TargetId == makeWidget.Id);

        Assert.Equal(1, extracted.Result.SourcesExamined);
        Assert.Equal(0, extracted.Result.UnresolvedRelationships);
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_TreatsArrowAndFunctionExpressionsAsCallables()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", TypeScriptSource));

        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "render");
    }

    [Fact]
    public async Task ExtractAsync_EmitsImportsAndStructuralInvocationsOnly()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", TypeScriptSource));

        var imports = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Imports).ToArray();
        Assert.Contains(imports, edge => TargetName(extracted, edge) == "node:fs");
        Assert.Contains(imports, edge => TargetName(extracted, edge) == "node:path");

        var invocations = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.InvokesName).ToArray();
        Assert.NotEmpty(invocations);
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "helper");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "Widget.create");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "w.area");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "Widget");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "readFile");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "path.join");

        // Structural parsing never claims resolved calls or references.
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
        Assert.All(extracted.Edges, edge =>
            Assert.Equal("penghou.hetu.tree-sitter.typescript", edge.Evidence.Provider));
    }

    [Fact]
    public async Task ExtractAsync_EmitsStructuralHeritage()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/heritage.ts", HeritageSource));

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();
        var implements = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Implements).ToArray();

        Assert.Contains(inherits, edge => SourceName(extracted, edge) == "Child" && TargetName(extracted, edge) == "Base");
        Assert.Contains(inherits, edge => SourceName(extracted, edge) == "I3" && TargetName(extracted, edge) == "I1");
        Assert.Contains(implements, edge => SourceName(extracted, edge) == "Child" && TargetName(extracted, edge) == "I1");
        Assert.Contains(implements, edge => SourceName(extracted, edge) == "Child" && TargetName(extracted, edge) == "I2");

        Assert.All(inherits.Concat(implements), edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));

        Assert.Contains(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.Inherits.Value &&
                coverage.State == CodeRelationshipCoverageState.Produced);
        Assert.Contains(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.Implements.Value &&
                coverage.State == CodeRelationshipCoverageState.Produced);
    }

    [Fact]
    public async Task ExtractAsync_ModelsInterfaceMembersWithContainmentAndLocations()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/shape.ts", InterfaceSource));

        var shape = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Shape");
        var area = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Shape.area");
        var id = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.id");
        var timeout = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.timeout");
        var cache = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.cache");

        foreach (var member in new[] { area, id, timeout, cache })
        {
            Assert.NotNull(member.SymbolId);
            Assert.Contains(
                extracted.Edges,
                edge => edge.Kind == CodeEdgeKinds.Contains &&
                    edge.SourceId == shape.Id &&
                    edge.TargetId == member.Id);
            Assert.Contains(
                extracted.Edges,
                edge => edge.Kind == CodeEdgeKinds.Declares &&
                    edge.TargetId == member.Id);
        }

        // The member declaration covers the written signature with an exact
        // source location; the containment edge keeps the precise name span.
        var areaDeclaration = Assert.Single(
            extracted.Declarations,
            declaration => declaration.SymbolId == area.SymbolId);
        Assert.Equal("src/shape.ts", areaDeclaration.Location.Path);
        Assert.Equal(2, areaDeclaration.Location.StartLine);
        Assert.Equal(3, areaDeclaration.Location.StartColumn);

        var areaEdge = Assert.Single(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == shape.Id &&
                edge.TargetId == area.Id);
        Assert.Equal(2, areaEdge.Evidence.Location!.StartLine);
        Assert.Equal(3, areaEdge.Evidence.Location.StartColumn);
        Assert.Equal(7, areaEdge.Evidence.Location.EndColumn);

        // Signatures are declarations, never invocations.
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.InvokesName);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_RecordsOptionalAndReadonlyInterfaceModifiers()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/shape.ts", InterfaceSource));

        var area = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Shape.area");
        var id = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.id");
        var timeout = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.timeout");
        var cache = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.cache");

        Assert.Null(Modifiers(area));
        Assert.Equal("readonly", Modifiers(id));
        Assert.Equal("optional", Modifiers(timeout));
        Assert.Equal("optional readonly", Modifiers(cache));
    }

    [Fact]
    public async Task ExtractAsync_KeepsEveryInterfaceMethodOverloadSignature()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/parser.ts", InterfaceOverloadSource));

        // Name-derived identity converges the overloads into one callable;
        // every physical signature keeps its own declaration and location.
        var parse = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Parser.parse");
        var declarations = extracted.Declarations
            .Where(declaration => declaration.SymbolId == parse.SymbolId)
            .OrderBy(declaration => declaration.Location.StartLine)
            .ToArray();

        Assert.Equal(2, declarations.Length);
        Assert.Equal([2, 3], declarations.Select(declaration => declaration.Location.StartLine));
        Assert.All(declarations, declaration =>
            Assert.Equal(3, declaration.Location.StartColumn));
        Assert.NotEqual(declarations[0].Location, declarations[1].Location);

        var contains = extracted.Edges
            .Where(edge => edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == parse.Id)
            .ToArray();
        Assert.Equal(2, contains.Length);
        Assert.Equal(
            [2, 3],
            contains.Select(edge => edge.Evidence.Location!.StartLine).Order());
    }

    [Fact]
    public async Task ExtractAsync_KeepsIdenticallyNamedMembersOnDistinctInterfaces()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/reader.ts", DuplicateMemberSource));

        var readerRead = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Reader.read");
        var writerRead = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Writer.read");

        Assert.NotEqual(readerRead.Id, writerRead.Id);
        Assert.NotEqual(readerRead.SymbolId, writerRead.SymbolId);
    }

    [Fact]
    public async Task ExtractAsync_KeepsSameNameInterfacesInSeparateModulesDistinct()
    {
        const string module = """
            export interface Shape {
              area(): number;
            }
            """;
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", module),
            ("src/b.ts", module));

        // Interface identity binds the source path: unrelated modules that
        // both declare `Shape` must not silently merge.
        var shapes = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Shape")
            .ToArray();
        var areas = extracted.Nodes
            .Where(node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Shape.area")
            .ToArray();

        Assert.Equal(2, shapes.Length);
        Assert.Equal(2, shapes.Select(shape => shape.Id).Distinct().Count());
        Assert.Equal(2, areas.Length);
        Assert.Equal(2, areas.Select(area => area.Id).Distinct().Count());
        Assert.Equal(
            2,
            extracted.Declarations.Count(declaration =>
                shapes.Any(shape => shape.SymbolId == declaration.SymbolId)));
        Assert.Equal(
            2,
            extracted.Declarations.Count(declaration =>
                areas.Any(area => area.SymbolId == declaration.SymbolId)));
    }

    [Fact]
    public async Task ExtractAsync_SkipsComputedAndUnsupportedInterfaceMemberNames()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/unsupported.ts", UnsupportedMemberSource));

        var unsupported = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Unsupported");

        // Computed, string, numeric, index, call, and construct members are
        // unsupported: they emit no nodes, declarations, or containment.
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.QualifiedName is not null &&
            node.QualifiedName.StartsWith("Unsupported.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains && edge.SourceId == unsupported.Id);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.InvokesName);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidInterfaceFactsFromPartialFiles()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken-interface.ts", BrokenInterfaceSource));

        Assert.Contains("typescript.parse-error", extracted.Result.WarningCodes);

        // The interface and its valid signature survive recovery; the
        // partially recovered signature emits no fragment facts.
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Interface && node.QualifiedName == "Broken");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Broken.ok");
        Assert.DoesNotContain(
            extracted.Nodes,
            node => node.QualifiedName == "Broken.bad");

        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_RepeatsInterfaceFactsDeterministically()
    {
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/shape.ts", InterfaceSource),
            ("src/parser.ts", InterfaceOverloadSource),
            ("src/broken-interface.ts", BrokenInterfaceSource));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken-interface.ts", BrokenInterfaceSource),
            ("src/parser.ts", InterfaceOverloadSource),
            ("src/shape.ts", InterfaceSource));

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Declarations), JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task ExtractAsync_TsxTreatsJsxAsStructureNotInvocation()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/app.tsx", TsxSource));

        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "App");

        var invocations = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.InvokesName).ToArray();
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "handleClick");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "render");
        Assert.DoesNotContain(invocations, edge => InvokedName(extracted, edge) == "div");
        Assert.DoesNotContain(extracted.Result.WarningCodes, code => code == "typescript.parse-error");
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidFactsFromPartialFiles()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken.ts", BrokenSource));

        var broken = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Broken");
        var declaration = Assert.Single(
            extracted.Declarations,
            value => value.SymbolId == broken.SymbolId);
        Assert.Equal("src/broken.ts", declaration.Location.Path);

        Assert.Contains("typescript.parse-error", extracted.Result.WarningCodes);
        var declares = Assert.Single(
            extracted.Result.RelationshipCoverage,
            coverage => coverage.RelationshipKind == CodeEdgeKinds.Declares.Value);
        Assert.Equal(CodeRelationshipCoverageState.Partial, declares.State);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_RepeatsNormalizedFactsDeterministically()
    {
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", TypeScriptSource),
            ("src/broken.ts", BrokenSource));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/broken.ts", BrokenSource),
            ("src/widget.ts", TypeScriptSource));

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Declarations), JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task ExtractAsync_EmittedBatchesPassHetuValidation()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/widget.ts", TypeScriptSource),
            ("src/app.tsx", TsxSource),
            ("src/broken.ts", BrokenSource));
        var validator = new CodeGraphBatchValidator();

        Assert.All(extracted.Batches, batch =>
            Assert.Empty(validator.Validate(batch, new CodeGraphBatchLimits())));
    }

    [Fact]
    public void ExtractAsync_ReportsCapabilitiesHonestly()
    {
        var plugin = new TreeSitterTypeScriptPlugin();

        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Syntax));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Declarations));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Imports));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Diagnostics));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.References));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Calls));

        Assert.True(plugin.CanHandle("src/app.ts"));
        Assert.True(plugin.CanHandle("src/app.tsx"));
        Assert.True(plugin.CanHandle("src/app.MTS"));
        Assert.False(plugin.CanHandle("src/app.js"));
        Assert.False(plugin.CanHandle("lib/accounts.ex"));
        Assert.False(plugin.CanHandle("README.md"));
    }

    [Fact]
    public void Plugin_ExposesPinnedGrammarIdentity()
    {
        var plugin = new TreeSitterTypeScriptPlugin();

        Assert.Equal("typescript", plugin.Language);
        Assert.Equal("tree-sitter-typescript", plugin.Grammar.GrammarId);
        Assert.Equal("v0.23.2", plugin.Grammar.GrammarVersion);
        Assert.Equal("75b3874edb2dc714fb1fd77a32013d0f8699989f", plugin.Grammar.GrammarCommit);
        Assert.Equal(14, plugin.Grammar.AbiVersion);
        Assert.Equal("tree-sitter-tsx", plugin.TsxGrammar.GrammarId);
        Assert.Equal(14, plugin.TsxGrammar.AbiVersion);

        var packageVersion = typeof(TreeSitterTypeScriptPlugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+', 2)[0];
        Assert.Equal($"{packageVersion}+v0.23.2.abi14.p2", plugin.Version);
    }

    [Fact]
    public async Task IndexAsync_SelectsTypeScriptPluginThroughProviderMapAndAnswersQueries()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-typescript-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "widget.ts"), TypeScriptSource);

            var registry = new CodeGraphPluginRegistry(
                [
                    new TreeSitterTypeScriptPlugin(),
                    new TreeSitterJavaScriptPlugin()
                ]);
            var repositoryId = new CodeRepositoryId("repo:typescript");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                registry,
                store);

            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                new CodeIndexRunId("run:typescript"));

            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var widgetId = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Widget"))
                .Candidates.Single().Id;

            // Structural containment reaches the members; resolved callers stay
            // empty because structural evidence never poses as resolved.
            var view = (await queries.OpenLatestPublicationAsync(repositoryId))!;
            var neighborhood = await view.GetNeighborhoodAsync(
                widgetId,
                options: new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Widget.area");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_RemovesStaleInterfaceMembersOnReindex()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-interface-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            var path = Path.Combine(src.FullName, "shape.ts");
            await File.WriteAllTextAsync(path, """
                export interface Shape {
                  area(): number;
                  perimeter(): number;
                }
                """);
            var repositoryId = new CodeRepositoryId("repo:ts-interface");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
                store);
            var descriptor = new CodeRepositoryDescriptor(repositoryId, directory.FullName);

            var first = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:first"));
            Assert.Equal(CodeIndexRunStatus.Completed, first.Diagnostics.Status);
            var queries = new CodeGraphQueryService(store);
            var shapeId = (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape"))
                .Candidates.Single().Id;
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape.perimeter")).Candidates);
            Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape.area")).Candidates);

            await File.WriteAllTextAsync(path, """
                export interface Shape {
                  area(): number;
                }
                """);
            var second = await indexing.IndexAsync(descriptor, new CodeIndexRunId("run:second"));
            Assert.Equal(CodeIndexRunStatus.Completed, second.Diagnostics.Status);

            // The removed member leaves no node, declaration, or edge behind.
            Assert.Empty(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape.perimeter")).Candidates);
            var area = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape.area")).Candidates);
            var declarations = await queries.FindDeclarationsAsync(repositoryId, area.SymbolId!);
            Assert.Single(declarations);
            Assert.Equal("src/shape.ts", declarations[0].Location.Path);

            var view = (await queries.OpenLatestPublicationAsync(repositoryId))!;
            var neighborhood = await view.GetNeighborhoodAsync(
                shapeId,
                options: new CodeGraphQueryOptions(maxDepth: 2, maxNodes: 100, maxEdges: 200));
            Assert.DoesNotContain(
                neighborhood.Result.Nodes,
                node => node.QualifiedName == "Shape.perimeter");
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.QualifiedName == "Shape.area");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task IndexAsync_AnswersExactInterfaceDeclarationAndContainmentQueries()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-interface-query-");
        try
        {
            var src = Directory.CreateDirectory(Path.Combine(directory.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(src.FullName, "shape.ts"), InterfaceSource);
            var repositoryId = new CodeRepositoryId("repo:ts-interface-query");
            var store = new InMemoryCodeGraphStore();
            var indexing = new CodeIndexingService(
                new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]),
                new CodeGraphPluginRegistry([new TreeSitterTypeScriptPlugin()]),
                store);
            var result = await indexing.IndexAsync(
                new CodeRepositoryDescriptor(repositoryId, directory.FullName),
                new CodeIndexRunId("run:ts-interface-query"));
            Assert.Equal(CodeIndexRunStatus.Completed, result.Diagnostics.Status);

            var queries = new CodeGraphQueryService(store);
            var shape = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape")).Candidates);
            var area = Assert.Single(
                (await queries.FindSymbolsByQualifiedNameAsync(repositoryId, "Shape.area")).Candidates);
            Assert.Equal(CodeNodeKinds.Interface, shape.Kind);
            Assert.Equal(CodeNodeKinds.Callable, area.Kind);

            // Exact physical declarations for the file: the interface plus
            // its four members.
            var declarations = await queries.GetDeclarationsInFileAsync(repositoryId, "src/shape.ts");
            Assert.Equal(5, declarations.Count);
            var areaDeclaration = Assert.Single(
                declarations,
                declaration => declaration.SymbolId == area.SymbolId);
            Assert.Equal("src/shape.ts", areaDeclaration.Location.Path);
            Assert.Equal(2, areaDeclaration.Location.StartLine);
            Assert.Equal(3, areaDeclaration.Location.StartColumn);

            // Containment connects the interface to every member.
            var view = (await queries.OpenLatestPublicationAsync(repositoryId))!;
            var neighborhood = await view.GetNeighborhoodAsync(
                shape.Id,
                options: new CodeGraphQueryOptions(maxDepth: 1, maxNodes: 100, maxEdges: 200));
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Shape.area");
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.id");
            Assert.Contains(
                neighborhood.Result.Nodes,
                node => node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Shape.cache");
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task Plan_InvalidatesPreviousExtractionProfileRevisions()
    {
        var directory = Directory.CreateTempSubdirectory("hetu-ts-plan-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "shape.ts"), InterfaceSource);
            var plugin = new TreeSitterTypeScriptPlugin();
            var providers = new CodeRepositoryProviderRegistry([new FileSystemCodeRepositoryProvider()]);
            var repository = new CodeRepositoryDescriptor(
                new CodeRepositoryId("repo:ts-plan"),
                directory.FullName);
            var planner = new CodeIndexPlanner(new CodeGraphPluginRegistry([plugin]));
            await using var source = await providers.OpenAsync(repository);

            var initial = await planner.CreatePlanAsync(source);
            var item = Assert.Single(initial.Items);
            Assert.Equal(CodeIndexPlanStatus.New, item.Status);
            Assert.EndsWith(".p2", item.Manifest.PluginVersion, StringComparison.Ordinal);

            // A previous publication from the older extraction-profile
            // revision must re-extract even though plugin identity and content
            // are unchanged: emitted interface facts changed with the profile.
            var previous = new CodeSourceManifest(
                item.Manifest.PluginId,
                item.Manifest.PluginVersion.Replace(".p2", ".p1", StringComparison.Ordinal),
                item.Manifest.SourcePath,
                item.Manifest.SourceHash);
            var replanned = await planner.CreatePlanAsync(source, [previous]);

            var changed = Assert.Single(replanned.Items);
            Assert.Equal(CodeIndexPlanStatus.Changed, changed.Status);
            Assert.Equal(item.Manifest.SourceHash, changed.Manifest.SourceHash);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static string? Modifiers(CodeGraphNode node) =>
        node.Properties.TryGetValue(CodePropertyKeys.Modifiers, out var value)
            ? ((CodeTextProperty)value).Value
            : null;

    private static string InvokedName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).Name;

    private static string SourceName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.SourceId).Name;

    private static string? TargetName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName;
}
