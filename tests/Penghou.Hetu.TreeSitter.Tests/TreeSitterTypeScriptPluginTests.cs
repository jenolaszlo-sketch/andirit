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
        Assert.Equal($"{packageVersion}+v0.23.2.abi14.p1", plugin.Version);
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

    private static string InvokedName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).Name;

    private static string SourceName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.SourceId).Name;

    private static string? TargetName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName;
}
