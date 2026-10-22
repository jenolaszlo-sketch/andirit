// REVIEW-ONLY: final probe batch for the independent EC-3 review (EC3-GLM-Q1).

using Xunit;
using Xunit.Abstractions;

namespace Penghou.Hetu.TreeSitter.Tests;

public class EcmaGlmReviewFinalProbeTests(ITestOutputHelper output)
{
    private static readonly TreeSitterTypeScriptPlugin Ts = new();

    // CE-18: abstract method signatures must emit no facts (documented).
    [Fact]
    public async Task CE18_AbstractMethodSignatures_EmitNoMemberFacts()
    {
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            Ts,
            ("src/abstract.ts", """
                abstract class AbstractBase {
                    abstract perform(): void;
                    concrete(): number {
                        return 1;
                    }
                }
                """));

        foreach (var node in extraction.Nodes)
            output.WriteLine($"NODE kind={node.Kind} qname={node.QualifiedName ?? node.Name}");
        Assert.DoesNotContain(extraction.Nodes, node =>
            node.Kind == CodeNodeKinds.Callable &&
            node.QualifiedName == "AbstractBase.perform");
        Assert.Single(extraction.Nodes, node =>
            node.Kind == CodeNodeKinds.Callable &&
            node.QualifiedName == "AbstractBase.concrete");
    }

    // CE-19: overload signature + implementation converge to one callable with
    // two declarations (documented).
    [Fact]
    public async Task CE19_Overloads_ConvergeToOneCallable_TwoDeclarations()
    {
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            Ts,
            ("src/overloads.ts", """
                class Overloaded {
                    invoke(value: string): string;
                    invoke(value: number): number;
                    invoke(value: string | number): string | number {
                        return value;
                    }
                }
                """));

        var invoke = Assert.Single(extraction.Nodes, node =>
            node.Kind == CodeNodeKinds.Callable &&
            node.QualifiedName == "Overloaded.invoke");
        var declarations = extraction.Declarations
            .Where(declaration => declaration.SymbolId == invoke.SymbolId).ToArray();
        Assert.Equal(3, declarations.Length);
        var lines = declarations.Select(declaration => declaration.Location.StartLine)
            .OrderBy(line => line).ToArray();
        Assert.Equal([2, 3, 4], lines);
    }

    // CE-20: getter/setter pair converge to one accessor symbol.
    [Fact]
    public async Task CE20_AccessorPair_ConvergesToOneSymbol()
    {
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            Ts,
            ("src/accessors.ts", """
                class Bound {
                    get value(): number {
                        return this.backing;
                    }
                    set value(next: number) {
                        this.backing = next;
                    }
                    backing = 0;
                }
                """));

        var accessors = extraction.Nodes.Where(node =>
            node.Kind == CodeNodeKinds.Callable &&
            node.QualifiedName == "Bound.value").ToArray();
        Assert.Single(accessors);
        var declarations = extraction.Declarations
            .Where(declaration => declaration.SymbolId == accessors[0].SymbolId).ToArray();
        Assert.Equal(2, declarations.Length);
        Assert.Single(extraction.Nodes, node =>
            node.Kind == CodeNodeKinds.Property && node.QualifiedName == "Bound.backing");
    }

    // CE-21: `declare` field modifier and optional marker are recorded.
    [Fact]
    public async Task CE21_Declare_And_Optional_Modifiers_Recording()
    {
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            Ts,
            ("src/ambient.ts", """
                class Ambient {
                    declare ambient: string;
                    maybe?: number;
                    protected protectedValue = 1;
                }
                """));

        var ambient = Assert.Single(extraction.Nodes, node => node.QualifiedName == "Ambient.ambient");
        Assert.Equal(CodeNodeKinds.Property, ambient.Kind);
        Assert.Contains("declare",
            Assert.IsType<CodeTextProperty>(ambient.Properties[CodePropertyKeys.Modifiers]).Value);

        var maybe = Assert.Single(extraction.Nodes, node => node.QualifiedName == "Ambient.maybe");
        Assert.Contains("optional",
            Assert.IsType<CodeTextProperty>(maybe.Properties[CodePropertyKeys.Modifiers]).Value);

        var protectedValue = Assert.Single(extraction.Nodes, node =>
            node.QualifiedName == "Ambient.protectedValue");
        Assert.Equal("protected",
            Assert.IsType<CodeTextProperty>(protectedValue.Properties[CodePropertyKeys.Access]).Value);
    }

    // CE-22: deeply nested valid syntax stays bounded and does not crash.
    [Fact]
    public async Task CE22_DeepNesting_RemainsBounded()
    {
        const int depth = 200;
        var nested = string.Concat(Enumerable.Repeat("if (x) { ", depth)) +
            "class Deep {}" +
            string.Concat(Enumerable.Repeat(" }", depth));
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            Ts, ("src/deep.ts", nested));

        // The class sits inside nested blocks; the walk must survive and emit
        // the class (possibly with a partial warning if the parser recovers).
        var deep = extraction.Nodes.Where(node => node.QualifiedName is "Deep" or null &&
            node.Name == "Deep" && node.SymbolId is not null).ToArray();
        Assert.Single(deep);
        Assert.Equal(CodeNodeKinds.Type, deep[0].Kind);
    }
}
