namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Contract tests binding advertised <see cref="CodeGraphCapabilities"/> to
/// facts the Tree-sitter plugins actually emit. A capability flag without
/// matching relationship coverage is a false claim: the ECMAScript profiles
/// emit structural/Derived inheritance, Elixir does not.
/// </summary>
public sealed class TreeSitterCapabilityContractTests
{
    private const string TypeScriptHeritage = """
        class Base {}
        interface I1 {}
        class Child extends Base implements I1 {}
        """;

    private const string JavaScriptHeritage = """
        class Base {}
        class Child extends Base {}
        """;

    private const string ElixirModule = """
        defmodule Example do
          def run, do: :ok
        end
        """;

    [Fact]
    public void TypeScriptCapabilities_AdvertiseInheritanceForBothGrammars()
    {
        var plugin = new TreeSitterTypeScriptPlugin();

        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
        // `.tsx` files are extracted by the same profile, so the TSX grammar
        // descriptor must not advertise a different capability set.
        Assert.True(plugin.TsxGrammar.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
        Assert.True(plugin.Grammar.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
    }

    [Fact]
    public void JavaScriptCapabilities_AdvertiseInheritance()
    {
        var plugin = new TreeSitterJavaScriptPlugin();

        Assert.True(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
    }

    [Fact]
    public void ElixirCapabilities_DoNotAdvertiseInheritanceWithoutOutput()
    {
        var plugin = new TreeSitterElixirPlugin();

        Assert.False(plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance));
    }

    [Fact]
    public async Task InheritsCoverage_MatchesTheAdvertisedInheritanceCapability()
    {
        var cases = new[]
        {
            (Plugin: (ICodeGraphPlugin)new TreeSitterTypeScriptPlugin(), Path: "src/heritage.ts", Source: TypeScriptHeritage),
            (Plugin: (ICodeGraphPlugin)new TreeSitterJavaScriptPlugin(), Path: "src/heritage.js", Source: JavaScriptHeritage),
            (Plugin: (ICodeGraphPlugin)new TreeSitterElixirPlugin(), Path: "lib/example.ex", Source: ElixirModule)
        };

        foreach (var (plugin, path, source) in cases)
        {
            var extracted = await TreeSitterTestSupport.ExtractAsync(plugin, (path, source));
            var inherits = extracted.Result.RelationshipCoverage.Single(coverage =>
                coverage.RelationshipKind == CodeEdgeKinds.Inherits.Value);
            var advertised = plugin.Capabilities.HasFlag(CodeGraphCapabilities.Inheritance);

            Assert.Equal(
                advertised,
                inherits.State == CodeRelationshipCoverageState.Produced);
            if (!advertised)
                Assert.Equal(CodeRelationshipCoverageState.NotProduced, inherits.State);
        }
    }

    [Fact]
    public async Task TypeScriptOnly_ProducesImplementsCoverage()
    {
        var typeScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/heritage.ts", TypeScriptHeritage));
        var javaScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/heritage.js", JavaScriptHeritage));

        Assert.Equal(
            CodeRelationshipCoverageState.Produced,
            typeScript.Result.RelationshipCoverage.Single(coverage =>
                coverage.RelationshipKind == CodeEdgeKinds.Implements.Value).State);
        Assert.Equal(
            CodeRelationshipCoverageState.NotProduced,
            javaScript.Result.RelationshipCoverage.Single(coverage =>
                coverage.RelationshipKind == CodeEdgeKinds.Implements.Value).State);
    }

    [Fact]
    public async Task InheritanceCapablePlugins_NeverPresentHeritageAsResolved()
    {
        var typeScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/heritage.ts", TypeScriptHeritage));
        var javaScript = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            ("src/legacy.js", """
                class Base {}
                Child.prototype = Object.create(Base.prototype);
                """));

        foreach (var extracted in new[] { typeScript, javaScript })
        {
            var heritage = extracted.Edges
                .Where(edge => edge.Kind == CodeEdgeKinds.Inherits ||
                    edge.Kind == CodeEdgeKinds.Implements)
                .ToArray();
            Assert.NotEmpty(heritage);
            Assert.All(heritage, edge => Assert.Contains(
                edge.Evidence.Quality,
                new[] { EvidenceQuality.Structural, EvidenceQuality.Derived }));
        }

        // The JavaScript prototype pattern keeps its Derived distinction.
        Assert.Contains(
            javaScript.Edges,
            edge => edge.Kind == CodeEdgeKinds.Inherits &&
                edge.Evidence.Quality == EvidenceQuality.Derived);
    }
}
