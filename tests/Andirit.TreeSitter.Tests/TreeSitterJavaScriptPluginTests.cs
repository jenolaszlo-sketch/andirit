using System.Reflection;
using System.Text.Json;

namespace Andirit.TreeSitter.Tests;

public sealed class TreeSitterJavaScriptPluginTests
{
    private const string JavaScriptSource = """
        const fs = require("node:fs");
        import { join } from "node:path";

        export class Store {
          constructor() {
            this.items = [];
          }

          add(item) {
            this.items.push(item);
            return this;
          }
        }

        export function main() {
          const store = new Store();
          store.add("a").add("b");
          console.log(fs.readFileSync("x"));
          return store;
        }

        const helper = function (n) {
          return n * 2;
        };

        main();
        helper(3);
        join("a", "b");
        """;

    private const string PrototypeSource = """
        class Base {}
        function Legacy() {}
        class Derived extends Base {}
        Child.prototype = Object.create(Base.prototype);
        Other.prototype = new Base();
        util.inherits(Legacy, Base);
        Object.setPrototypeOf(Proto.prototype, Base.prototype);
        """;

    private const string BrokenSource = """
        export class Broken {
          run() {
            return 1;
          }
        """;

    [Fact]
    public async Task ExtractAsync_ModelsClassesFunctionsAndContainment()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", JavaScriptSource));

        var store = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Store");
        Assert.NotNull(store.SymbolId);

        var constructor = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Store.constructor");
        var add = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "Store.add");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "main");
        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Callable && node.QualifiedName == "helper");

        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == store.Id &&
                edge.TargetId == constructor.Id);
        Assert.Contains(
            extracted.Edges,
            edge => edge.Kind == CodeEdgeKinds.Contains &&
                edge.SourceId == store.Id &&
                edge.TargetId == add.Id);
    }

    [Fact]
    public async Task ExtractAsync_TreatsRequireAndImportAsImportsAndOtherCallsAsInvocations()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", JavaScriptSource));

        var imports = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Imports).ToArray();
        Assert.Contains(imports, edge => TargetName(extracted, edge) == "node:fs");
        Assert.Contains(imports, edge => TargetName(extracted, edge) == "node:path");

        var invocations = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.InvokesName).ToArray();
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "Store");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "store.add");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "console.log");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "fs.readFileSync");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "main");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "helper");
        Assert.Contains(invocations, edge => InvokedName(extracted, edge) == "join");

        // `require` is an import, not a callable invocation.
        Assert.DoesNotContain(invocations, edge => InvokedName(extracted, edge) == "require");
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Calls);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.References);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_DerivesPrototypeInheritanceAsDerivedEvidence()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/legacy.js", PrototypeSource));

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();

        // ES6 extends is a grammar-level fact. Its written base remains a
        // declaration-less candidate even beside a same-name declaration.
        var baseClass = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "Base" &&
                node.SymbolId is not null);
        var derivedClass = Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type &&
                node.QualifiedName == "Derived" &&
                node.SymbolId is not null);
        var structuralEdge = Assert.Single(
            inherits,
            edge => SourceName(extracted, edge) == "Derived" &&
                TargetName(extracted, edge) == "Base" &&
                edge.Evidence.Quality == EvidenceQuality.Structural);
        var baseTarget = Assert.Single(extracted.Nodes, node => node.Id == structuralEdge.TargetId);
        Assert.Equal(derivedClass.Id, structuralEdge.SourceId);
        Assert.NotEqual(baseClass.Id, baseTarget.Id);
        Assert.Null(baseTarget.SymbolId);

        // Prototype idioms are recognized patterns, never presented as
        // structural or resolved facts.
        foreach (var child in new[] { "Child", "Other", "Legacy", "Proto" })
        {
            Assert.Contains(
                inherits,
                edge => SourceName(extracted, edge) == child &&
                    TargetName(extracted, edge) == "Base" &&
                    edge.Evidence.Quality == EvidenceQuality.Derived);
        }

        Assert.All(
            inherits.Where(edge => edge.Evidence.Quality == EvidenceQuality.Derived),
            edge =>
            {
                Assert.Null(extracted.Nodes.Single(node => node.Id == edge.SourceId).SymbolId);
                Assert.Null(extracted.Nodes.Single(node => node.Id == edge.TargetId).SymbolId);
            });
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidFactsFromPartialFiles()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/broken.js", BrokenSource));

        Assert.Single(
            extracted.Nodes,
            node => node.Kind == CodeNodeKinds.Type && node.QualifiedName == "Broken");
        Assert.Contains("javascript.parse-error", extracted.Result.WarningCodes);
        Assert.All(extracted.Edges, edge =>
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
    }

    [Fact]
    public async Task ExtractAsync_RepeatsNormalizedFactsDeterministically()
    {
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", JavaScriptSource),
            ("src/broken.js", BrokenSource),
            ("src/legacy.js", PrototypeSource));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/broken.js", BrokenSource),
            ("src/legacy.js", PrototypeSource),
            ("src/store.js", JavaScriptSource));

        Assert.Equal(JsonSerializer.Serialize(first.Nodes), JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(JsonSerializer.Serialize(first.Declarations), JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(JsonSerializer.Serialize(first.Edges), JsonSerializer.Serialize(second.Edges));
    }

    [Fact]
    public async Task ExtractAsync_EmittedBatchesPassHetuValidation()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/store.js", JavaScriptSource),
            ("src/broken.js", BrokenSource));
        var validator = new CodeGraphBatchValidator();

        Assert.All(extracted.Batches, batch =>
            Assert.Empty(validator.Validate(batch, new CodeGraphBatchLimits())));
    }

    [Fact]
    public void ExtractAsync_ReportsCapabilitiesHonestly()
    {
        var plugin = new TreeSitterJavaScriptPlugin();

        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Syntax));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Declarations));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Imports));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Diagnostics));
        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.References));
        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Calls));

        Assert.True(plugin.CanHandle("src/store.js"));
        Assert.True(plugin.CanHandle("src/store.jsx"));
        Assert.True(plugin.CanHandle("src/store.MJS"));
        Assert.False(plugin.CanHandle("src/app.ts"));
        Assert.False(plugin.CanHandle("src/app.tsx"));
        Assert.False(plugin.CanHandle("README.md"));
    }

    [Fact]
    public void Plugin_ExposesPinnedGrammarIdentity()
    {
        var plugin = new TreeSitterJavaScriptPlugin();

        Assert.Equal("javascript", plugin.Language);
        Assert.Equal("tree-sitter-javascript", plugin.Grammar.GrammarId);
        Assert.Equal("v0.25.0", plugin.Grammar.GrammarVersion);
        Assert.Equal("58404d8cf191d69f2674a8fd507bd5776f46cb11", plugin.Grammar.GrammarCommit);
        Assert.Equal(15, plugin.Grammar.AbiVersion);

        var packageVersion = typeof(TreeSitterJavaScriptPlugin).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion.Split('+', 2)[0];
        Assert.Equal($"{packageVersion}+v0.25.0.abi15.p10", plugin.Version);
    }

    private static string InvokedName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).Name;

    private static string SourceName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.SourceId).Name;

    private static string? TargetName(Extraction extracted, CodeGraphEdge edge) =>
        extracted.Nodes.Single(node => node.Id == edge.TargetId).QualifiedName;
}
