// REVIEW-ONLY probe: dump facts for nested-scope classes.

using Xunit;
using Xunit.Abstractions;

namespace Penghou.Hetu.TreeSitter.Tests;

public class EcmaGlmReviewDumpTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Dump_Nested_Class_Facts()
    {
        var extraction = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/nested.ts", "function outer() {\n    class Widget { render() {} }\n}\n"));

        foreach (var node in extraction.Nodes)
            output.WriteLine($"NODE {node.Id.Value[..40]} kind={node.Kind} qname={node.QualifiedName ?? node.Name} symbol={node.SymbolId?.Value ?? "-"}");
        foreach (var declaration in extraction.Declarations)
            output.WriteLine($"DECL {declaration.SymbolId.Value[..40]} node={declaration.SymbolNodeId.Value[..40]} @L{declaration.Location.StartLine}");
        foreach (var edge in extraction.Edges)
            output.WriteLine($"EDGE {edge.Kind} {edge.SourceId.Value[..40]} -> {edge.TargetId.Value[..40]}");
    }
}
