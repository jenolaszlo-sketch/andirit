// REVIEW-ONLY: adversarial counterexamples for the independent EC-3 review
// (EC3-GLM-Q1). Not part of the implementation's qualification suite. These
// tests probe predicted invariants and must be deleted with the review
// worktree or preserved only as review evidence.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Independent adversarial probes for EC-3. Each test states the suspected
/// invariant violation, the prediction, and the observed assertion.
/// </summary>
public static class EcmaGlmReviewProbeExtensions
{
    internal static CodeGraphNode? Node(this Extraction extraction, string qualifiedName) =>
        extraction.Nodes.SingleOrDefault(node =>
            node.QualifiedName == qualifiedName &&
            node.SymbolId is not null);

    internal static IReadOnlyList<CodeGraphNode> NodesNamed(this Extraction extraction, string qualifiedName) =>
        [.. extraction.Nodes.Where(node => node.QualifiedName == qualifiedName)];

    internal static IReadOnlyList<CodeGraphEdge> EdgesFrom(this Extraction extraction, CodeNodeId source, CodeEdgeKind kind) =>
        [.. extraction.Edges.Where(edge => edge.Kind == kind && edge.SourceId == source)];

    internal static IReadOnlyList<CodeGraphEdge> EdgesTo(this Extraction extraction, CodeNodeId target, CodeEdgeKind kind) =>
        [.. extraction.Edges.Where(edge => edge.Kind == kind && edge.TargetId == target)];
}

public class EcmaGlmReviewAdversarialTests
{
    private static readonly TreeSitterTypeScriptPlugin Ts = new();
    private static readonly TreeSitterJavaScriptPlugin Js = new();

    private static Extraction Tsx(params (string Path, string Content)[] files) =>
        TreeSitterTestSupport.ExtractAsync(Ts, files).GetAwaiter().GetResult();

    private static Extraction Jsx(params (string Path, string Content)[] files) =>
        TreeSitterTestSupport.ExtractAsync(Js, files).GetAwaiter().GetResult();

    // CE-1 (Area B): shadowed heritage target converges with the outer,
    // unrelated top-level class declaration.
    [Fact]
    public void CE1_Heritage_Placeholder_Converges_With_Outer_Base_Under_Shadowing()
    {
        var extraction = Tsx((
            "src/heritage-shadow.ts",
            """
            class Base {}

            function create() {
                class Base {}
                class Derived extends Base {}
                return Derived;
            }
            """));

        var outerBase = extraction.Node("Base");
        var innerBase = extraction.Node("create.Base");
        var derived = extraction.Node("create.Derived");
        Assert.NotNull(outerBase);
        Assert.NotNull(innerBase);
        Assert.NotNull(derived);
        Assert.NotEqual(outerBase!.Id, innerBase!.Id);

        var inherits = extraction.EdgesFrom(derived!.Id, CodeEdgeKinds.Inherits);
        var edge = Assert.Single(inherits);

        // Prediction: the placeholder for the written name "Base" hashes
        // path + "Base" without lexical scope, so the edge converges with the
        // OUTER top-level Base node even though runtime binding resolves to
        // the inner create.Base.
        Assert.Equal(outerBase.Id, edge.TargetId);
        Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
    }

    // CE-2 (Area A): identity separation across files and lexical scopes.
    // Failed-counterexample probe: separation is expected to hold.
    [Fact]
    public void CE2_SameName_Classes_Across_Files_And_Scopes_Stay_Distinct()
    {
        var extraction = Tsx(
            ("src/first.ts", "class Widget { render() {} }\n"),
            ("src/second.ts", "class Widget { render() {} }\n"),
            ("src/nested.ts", """
                function outer() {
                    class Widget { render() {} }
                }
                """));

        var topLevelWidgets = extraction.NodesNamed("Widget");
        var nestedWidget = Assert.Single(extraction.NodesNamed("outer.Widget"));
        var widgets = topLevelWidgets.Append(nestedWidget).ToArray();
        Assert.Equal(3, widgets.Length);
        Assert.Equal(3, widgets.Select(node => node.SymbolId!.Value).Distinct().Count());
        Assert.Equal(3, widgets.Select(node => node.Id.Value).Distinct().Count());

        var renders = extraction.NodesNamed("outer.Widget.render")
            .Concat(extraction.NodesNamed("Widget.render")).ToArray();
        // Two file-scoped classes contribute Widget.render twice; the nested
        // one contributes outer.Widget.render once.
        Assert.Equal(3, renders.Length);
        Assert.Equal(3, renders.Select(node => node.SymbolId!.Value).Distinct().Count());

        // Exact-name lookup ambiguity: 3 Widget candidates, each owned by its
        // own file; containment must not join the owners.
        foreach (var widget in widgets)
        {
            var contains = extraction.EdgesFrom(
                widget.Id, CodeEdgeKinds.Contains).Where(edge =>
                    extraction.Nodes.Any(node => node.Id == edge.TargetId && node.SymbolId is not null));
            Assert.All(contains, edge =>
                Assert.Equal(widget.Id, edge.SourceId));
        }
    }

    // CE-3 (Area A/C): duplicate same-name members in one class converge.
    [Fact]
    public void CE3_Duplicate_Members_In_One_Class_Converge_To_One_Symbol_Two_Declarations()
    {
        var extraction = Tsx((
            "src/duplicate-members.ts",
            """
            class Dup {
                x = 1;
                x = 2;
            }
            """));

        var fields = extraction.NodesNamed("Dup.x")
            .Where(node => node.Kind == CodeNodeKinds.Property).ToArray();
        var field = Assert.Single(fields);
        var symbolDeclarations = extraction.Declarations
            .Where(declaration => declaration.SymbolId == field.SymbolId).ToArray();
        Assert.Equal(2, symbolDeclarations.Length);

        var contains = extraction.EdgesFrom(
            extraction.Nodes.Single(node => node.QualifiedName == "Dup").Id,
            CodeEdgeKinds.Contains);
        Assert.Equal(2, contains.Count(edge => edge.TargetId == field.Id));
    }

    // CE-4 (Area C): unsupported member shapes must not produce field facts.
    [Fact]
    public void CE4_Unsupported_Member_Shapes_Are_Skipped_Conservatively()
    {
        var extraction = Tsx((
            "src/weird-members.ts",
            """
            class Weird {
                ["computed"]() {}
                "stringLiteral" = 1;
                42 = 2;
                #priv = 3;
                accessor auto = 4;
                static { boot(); }
                constructor(private p: string) {}
            }
            """));

        var names = extraction.Nodes.Select(node => node.QualifiedName).ToHashSet();
        Assert.DoesNotContain("Weird.computed", names);
        Assert.DoesNotContain("Weird.stringLiteral", names);
        Assert.DoesNotContain("Weird.42", names);
        Assert.DoesNotContain("Weird.priv", names);
        Assert.DoesNotContain("Weird.auto", names);
        Assert.DoesNotContain("Weird.p", names);

        // The static block's invocation is attributed to the class node.
        var weird = extraction.Nodes.Single(node => node.QualifiedName == "Weird");
        var classInvokes = extraction.EdgesFrom(weird.Id, CodeEdgeKinds.InvokesName);
        Assert.Single(classInvokes, edge =>
            extraction.Nodes.Any(node => node.Id == edge.TargetId && node.Name == "boot"));

        // No field/property node other than none: every property node in the
        // file must belong to the invocation placeholders only.
        var properties = extraction.Nodes.Where(node => node.Kind == CodeNodeKinds.Property).ToArray();
        Assert.Empty(properties);
    }

    // CE-5 (Area D): field initializer invocation ownership.
    [Fact]
    public void CE5_Field_Initializer_Invocations_Are_Owned_By_Field_Nodes()
    {
        var extraction = Tsx((
            "src/initializers.ts",
            """
            class Service {
                client = createClient();

                handler = () => execute();
            }
            """));

        var client = Assert.Single(extraction.NodesNamed("Service.client"));
        var handler = Assert.Single(extraction.NodesNamed("Service.handler"));
        Assert.Equal(CodeNodeKinds.Property, client.Kind);
        Assert.Equal(CodeNodeKinds.Property, handler.Kind);

        var clientInvokes = extraction.EdgesFrom(client.Id, CodeEdgeKinds.InvokesName);
        var edge = Assert.Single(clientInvokes);
        Assert.Equal("createClient", extraction.Nodes.Single(node => node.Id == edge.TargetId).Name);

        var handlerInvokes = extraction.EdgesFrom(handler.Id, CodeEdgeKinds.InvokesName);
        Assert.Single(handlerInvokes, e =>
            extraction.Nodes.Single(n => n.Id == e.TargetId).Name == "execute");

        // The class itself must not own these invocations.
        var service = extraction.Nodes.Single(node => node.QualifiedName == "Service");
        Assert.Empty(extraction.EdgesFrom(service.Id, CodeEdgeKinds.InvokesName));
    }

    // CE-6 (Area B): cross-file heritage stays a declaration-less placeholder.
    [Fact]
    public void CE6_CrossFile_Heritage_Target_Does_Not_Converge()
    {
        var extraction = Tsx(
            ("src/base.ts", "export class Base {}\n"),
            ("src/derived.ts", "class Derived extends Base {}\n"));

        var declaredBase = extraction.Nodes.Single(node =>
            node.SymbolId is not null && node.QualifiedName == "Base");
        var placeholder = Assert.Single(extraction.Nodes, node =>
            node.SymbolId is null && node.Name == "Base");
        Assert.NotEqual(declaredBase.Id, placeholder.Id);

        var derived = extraction.Nodes.Single(node => node.QualifiedName == "Derived");
        var inherits = Assert.Single(extraction.EdgesFrom(derived.Id, CodeEdgeKinds.Inherits));
        Assert.Equal(placeholder.Id, inherits.TargetId);
        Assert.Equal(EvidenceQuality.Structural, inherits.Evidence.Quality);
    }

    // CE-7 (Area A): class-expression members keep the name-based model.
    [Fact]
    public void CE7_Class_Expression_Methods_Converge_With_TopLevel_Function()
    {
        var extraction = Tsx((
            "src/class-expressions.ts",
            """
            const A = class { start() {} };
            const B = class { start() {} };
            function start() {}
            """));

        var starts = extraction.NodesNamed("start").ToArray();
        // Prediction per documented limitation: all three converge to one
        // callable symbol "start".
        var callable = Assert.Single(starts, node =>
            node.Kind == CodeNodeKinds.Callable && node.SymbolId is not null);
        var declarations = extraction.Declarations
            .Where(declaration => declaration.SymbolId == callable.SymbolId).ToArray();
        Assert.Equal(3, declarations.Length);
    }

    // CE-8 (Area B): JavaScript prototype idioms produce Derived evidence.
    [Fact]
    public void CE8_Prototype_Patterns_Emit_Derived_Inheritance()
    {
        var extraction = Jsx((
            "src/prototype.js",
            """
            function Child() {}
            function Parent() {}
            Child.prototype = Object.create(Parent.prototype);
            util.inherits(Other, Another);
            Object.setPrototypeOf(Third.prototype, Fourth.prototype);
            """));

        var derivedEdges = extraction.Edges
            .Where(edge => edge.Kind == CodeEdgeKinds.Inherits)
            .ToArray();
        Assert.Equal(3, derivedEdges.Length);
        Assert.All(derivedEdges, edge => Assert.Equal(EvidenceQuality.Derived, edge.Evidence.Quality));

        var names = derivedEdges.Select(edge => (
            Source: extraction.Nodes.Single(node => node.Id == edge.SourceId).Name,
            Target: extraction.Nodes.Single(node => node.Id == edge.TargetId).Name)).ToHashSet();
        Assert.Contains(("Child", "Parent"), names);
        Assert.Contains(("Other", "Another"), names);
        Assert.Contains(("Third", "Fourth"), names);
    }

    // CE-9 (Area C/D): decorators on fields attribute their invocation to the
    // field scope; parameter properties emit nothing.
    [Fact]
    public void CE9_Decorator_Invocation_Is_Attributed_To_Field_Scope()
    {
        var extraction = Tsx((
            "src/decorators.ts",
            """
            class Decorated {
                @log()
                value = compute();
            }
            """));

        var value = Assert.Single(extraction.NodesNamed("Decorated.value"));
        var invokes = extraction.EdgesFrom(value.Id, CodeEdgeKinds.InvokesName);
        Assert.Equal(2, invokes.Count);
        var targets = invokes.Select(edge =>
            extraction.Nodes.Single(node => node.Id == edge.TargetId).Name).ToHashSet();
        Assert.Contains("log", targets);
        Assert.Contains("compute", targets);
    }

    // CE-10 (Area G): determinism across repeated extraction and reordered input.
    [Fact]
    public void CE10_Repeated_Extraction_Is_Deterministic()
    {
        (string Path, string Content)[] files =
        [
            ("src/first.ts", "class Widget { render() {} static zone = boot(); }\n"),
            ("src/second.ts", "class Widget { render() {} static zone = boot(); }\n")
        ];

        var first = Tsx(files);
        var second = Tsx(files);
        Assert.Equal(
            first.Nodes.Select(node => node.Id.Value),
            second.Nodes.Select(node => node.Id.Value));
        Assert.Equal(
            first.Edges.Select(edge => edge.Id.Value),
            second.Edges.Select(edge => edge.Id.Value));
        Assert.Equal(
            first.Declarations.Select(declaration => declaration.Id.Value),
            second.Declarations.Select(declaration => declaration.Id.Value));

        // Static fields carry the static modifier and static ownership. Two
        // files yield two distinct same-named static fields (path-scoped).
        Assert.Equal(2, first.NodesNamed("Widget.zone").Count());
        var zones = first.NodesNamed("Widget.zone").ToArray();
        Assert.Equal(2, zones.Select(node => node.SymbolId!.Value).Distinct().Count());
        Assert.All(zones, zone =>
        {
            var modifiers = Assert.IsType<CodeTextProperty>(zone.Properties[CodePropertyKeys.Modifiers]).Value;
            Assert.Contains("static", modifiers);
        });
    }

    // CE-11 (Area G): unicode member names, bounded long identifiers.
    [Fact]
    public void CE11_Unicode_And_Overlong_Identifiers()
    {
        var longName = new string('x', 300);
        var extraction = Tsx((
            "src/unicode.ts",
            $$"""
            class Univocal { élément = 1; }
            class Long { {{longName}} = 1; }
            """));

        var element = Assert.Single(extraction.NodesNamed("Univocal.élément"));
        Assert.Equal(CodeNodeKinds.Property, element.Kind);
        Assert.DoesNotContain(extraction.Nodes, node => node.Name == longName);
    }

    // CE-12 (Area F): TSX grammar variant still extracts fields and modifiers.
    [Fact]
    public void CE12_TSX_Variant_Extracts_Fields()
    {
        var extraction = Tsx((
            "src/component.tsx",
            """
            export class Panel {
                private readonly label: string = "panel";
                static count = 0;
                render() {
                    return <div>{this.label}</div>;
                }
            }
            """));

        var label = Assert.Single(extraction.NodesNamed("Panel.label"));
        Assert.Equal(CodeNodeKinds.Property, label.Kind);
        var access = Assert.IsType<CodeTextProperty>(label.Properties[CodePropertyKeys.Access]).Value;
        Assert.Equal("private", access);
        var modifiers = Assert.IsType<CodeTextProperty>(label.Properties[CodePropertyKeys.Modifiers]).Value;
        Assert.Contains("readonly", modifiers);
        Assert.DoesNotContain("static", modifiers);

        var count = Assert.Single(extraction.NodesNamed("Panel.count"));
        Assert.Contains("static", Assert.IsType<CodeTextProperty>(count.Properties[CodePropertyKeys.Modifiers]).Value);
    }
}
