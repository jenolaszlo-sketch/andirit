// REVIEW-ONLY: adversarial counterexamples for the independent EC-3D
// remediation review (EC3-GLM-Q2). Not part of the implementation's
// qualification suite. Preserved only as review evidence on the review
// branch; must never be merged into production.

using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Penghou.Hetu.TreeSitter.Tests;

/// <summary>
/// Extraction-level probes for EC3-GLM-Q2. Each probe challenges the EC-3D
/// heritage-candidate remediation against a published identity formula that
/// this review computes independently, instead of trusting the
/// implementation's own fixtures.
/// </summary>
public class EcmaGlmQ2ReviewAdversarialTests
{
    private static string Sha(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string CandidateId(string language, string path, string writtenName) =>
        $"{language}:heritage-target:{Sha($"{path}\n{writtenName}")}";

    private static CodeGraphNode? SingleNodeOrDefault(
        Extraction extraction,
        Func<CodeGraphNode, bool> predicate) =>
        extraction.Nodes.Where(predicate).ToList() is { Count: 1 } matches
            ? matches[0]
            : null;

    // Q2-CE1: the original F01 counterexample. A written `extends Base`
    // inside a shadowing function must not converge with the unrelated
    // top-level `Base` declaration, and the candidate must be a
    // declaration-less node in the dedicated heritage-target namespace with
    // the exact documented identity.
    [Fact]
    public async Task Q2CE1_ShadowedHeritageTarget_IsDeclarationlessDistinctCandidate()
    {
        const string path = "src/heritage-shadow.ts";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, """
                class Base {}

                function create() {
                    class Base {}
                    class Derived extends Base {}
                }
                """));

        var topLevelBase = SingleNodeOrDefault(extracted, node =>
            node.Kind == CodeNodeKinds.Type &&
            node.QualifiedName == "Base" &&
            node.SymbolId is not null);
        var nestedBase = SingleNodeOrDefault(extracted, node =>
            node.Kind == CodeNodeKinds.Type &&
            node.QualifiedName == "create.Base" &&
            node.SymbolId is not null);
        var derived = SingleNodeOrDefault(extracted, node =>
            node.Kind == CodeNodeKinds.Type &&
            node.QualifiedName == "create.Derived" &&
            node.SymbolId is not null);
        Assert.NotNull(topLevelBase);
        Assert.NotNull(nestedBase);
        Assert.NotNull(derived);

        var inherits = Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Inherits);
        var target = Assert.Single(extracted.Nodes, node => node.Id == inherits.TargetId);

        // Declared identities survive; the written target is separate.
        Assert.Equal(derived.Id, inherits.SourceId);
        Assert.NotEqual(topLevelBase.Id, inherits.TargetId);
        Assert.NotEqual(nestedBase.Id, inherits.TargetId);
        Assert.Null(target.SymbolId);
        Assert.Equal(CodeNodeKinds.Type, target.Kind);
        Assert.Equal("Base", target.Name);
        Assert.Equal("Base", target.QualifiedName);
        Assert.Empty(target.Properties);

        // The candidate has no declaration and no declares/contains edges.
        Assert.DoesNotContain(extracted.Declarations, d => d.SymbolNodeId == target.Id);
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Declares && edge.TargetId == target.Id);
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Contains && edge.TargetId == target.Id);

        // The candidate identity matches the documented formula exactly,
        // computed here independently of the implementation.
        Assert.Equal(
            CandidateId("typescript", path, "Base"),
            target.Id.Value);
        Assert.StartsWith("typescript:heritage-target:", target.Id.Value, StringComparison.Ordinal);

        // The relationship keeps its source location and structural evidence.
        Assert.Equal(EvidenceQuality.Structural, inherits.Evidence.Quality);
        Assert.Equal(path, inherits.Evidence.Location?.Path);
        Assert.DoesNotContain(extracted.Edges, edge => edge.Evidence.Quality == EvidenceQuality.Resolved);
    }

    // Q2-CE2 (spec Test A): the same written base name in two files must
    // produce distinct candidates, and neither may bind to a declared class.
    [Fact]
    public async Task Q2CE2_SameWrittenNameInDifferentFiles_YieldsDistinctCandidates()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/a.ts", "class A extends Base {}\n"),
            ("src/b.ts", "class B extends Base {}\n"),
            ("src/base.ts", "class Base {}\n"));

        var declaredBase = SingleNodeOrDefault(extracted, node =>
            node.QualifiedName == "Base" && node.SymbolId is not null);
        Assert.NotNull(declaredBase);

        var candidates = extracted.Nodes
            .Where(node => node.SymbolId is null && node.QualifiedName == "Base")
            .ToArray();
        Assert.Equal(2, candidates.Length);
        Assert.Equal(2, candidates.Select(node => node.Id.Value).Distinct().Count());
        Assert.All(candidates, node => Assert.Null(node.SymbolId));
        Assert.Equal(
            new[]
            {
                CandidateId("typescript", "src/a.ts", "Base"),
                CandidateId("typescript", "src/b.ts", "Base")
            }.OrderBy(value => value, StringComparer.Ordinal),
            candidates.Select(node => node.Id.Value).OrderBy(value => value, StringComparer.Ordinal));

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();
        Assert.Equal(2, inherits.Length);
        Assert.All(inherits, edge => Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality));
        foreach (var edge in inherits)
        {
            // Each written relationship targets only its own file's candidate.
            var expectedPath = edge.Evidence.Location!.Path;
            Assert.Equal(CandidateId("typescript", expectedPath, "Base"), edge.TargetId.Value);
            Assert.NotEqual(declaredBase.Id, edge.TargetId);
            var source = Assert.Single(extracted.Nodes, node => node.Id == edge.SourceId);
            Assert.NotNull(source.SymbolId);
            Assert.True(
                source.Properties[CodePropertyKeys.Language] is CodeTextProperty { Value: "typescript" });
        }

        // The declared top-level Base gains no incoming heritage edge.
        Assert.DoesNotContain(inherits, edge => edge.TargetId == declaredBase.Id);
    }

    // Q2-CE3 (spec Test B): two written `Base` targets in one file (different
    // lexical scopes) share one candidate node by design, while each
    // relationship occurrence keeps its own edge, location, and evidence.
    [Fact]
    public async Task Q2CE3_SameNameInDifferentScopesOneFile_SharesCandidateWithDistinctEdges()
    {
        const string path = "src/shared-candidate.ts";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, """
                class Alpha extends Base {}

                function create() {
                    class Base {}
                    class Beta extends Base {}
                }
                """));

        var candidates = extracted.Nodes
            .Where(node => node.SymbolId is null && node.QualifiedName == "Base")
            .ToArray();
        var candidate = Assert.Single(candidates);
        Assert.Equal(CandidateId("typescript", path, "Base"), candidate.Id.Value);

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();
        Assert.Equal(2, inherits.Length);
        Assert.Equal(2, inherits.Select(edge => edge.Id.Value).Distinct().Count());
        Assert.All(inherits, edge =>
        {
            Assert.Equal(EvidenceQuality.Structural, edge.Evidence.Quality);
            Assert.Equal(candidate.Id, edge.TargetId);
            Assert.Equal(path, edge.Evidence.Location?.Path);
        });

        // The two occurrences are distinguishable through edge evidence:
        // distinct sources and distinct source locations.
        Assert.Equal(2, inherits.Select(edge => edge.SourceId.Value).Distinct().Count());
        Assert.Equal(
            2,
            inherits.Select(edge =>
                    $"{edge.Evidence.Location?.StartLine}:{edge.Evidence.Location?.StartColumn}")
                .Distinct().Count());
        var alpha = SingleNodeOrDefault(extracted, node => node.QualifiedName == "Alpha")!;
        var beta = SingleNodeOrDefault(extracted, node => node.QualifiedName == "create.Beta")!;
        Assert.Contains(inherits, edge => edge.SourceId == alpha.Id);
        Assert.Contains(inherits, edge => edge.SourceId == beta.Id);

        // Alpha's occurrence cannot be confused with the inner declaration.
        var nestedBase = SingleNodeOrDefault(extracted, node =>
            node.QualifiedName == "create.Base" && node.SymbolId is not null)!;
        Assert.NotEqual(nestedBase.Id, candidate.Id);
    }

    // Q2-CE4 (spec Test C): qualified written names produce distinct
    // candidates that preserve the full written member name; unsupported
    // expression targets produce no invented candidates.
    [Fact]
    public async Task Q2CE4_QualifiedHeritageNames_ProduceDistinctWrittenNameCandidates()
    {
        const string path = "src/qualified-heritage.ts";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, """
                class A extends Models.Base {}
                class B extends Other.Base {}
                class C extends Deep.Nested.Inner {}
                class D extends Base {}
                class E extends mixin(Base) {}
                """));

        var expected = new[]
        {
            "Models.Base",
            "Other.Base",
            "Deep.Nested.Inner",
            "Base"
        };
        foreach (var writtenName in expected)
        {
            var candidates = extracted.Nodes
                .Where(node => node.SymbolId is null && node.QualifiedName == writtenName)
                .ToArray();
            var candidate = Assert.Single(candidates);
            Assert.Equal(CandidateId("typescript", path, writtenName), candidate.Id.Value);
            Assert.Equal(writtenName, candidate.Name);
            Assert.Null(candidate.SymbolId);
        }

        Assert.Equal(4, extracted.Nodes.Count(node =>
            node.Id.Value.StartsWith("typescript:heritage-target:", StringComparison.Ordinal)));

        // `extends mixin(Base)` is an unsupported expression target: no
        // heritage edge for E, no fabricated candidate for it.
        var mixinCandidates = extracted.Nodes
            .Where(node => node.QualifiedName is { } name && name.Contains("mixin", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(mixinCandidates);
        var classE = SingleNodeOrDefault(extracted, node => node.QualifiedName == "E")!;
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Inherits && edge.SourceId == classE.Id);
    }

    // Q2-CE5 (spec Test D): written `implements` and interface-level
    // `extends` keep their relationship kinds and stay declaration-less,
    // even beside a declared interface of the same name.
    [Fact]
    public async Task Q2CE5_ImplementsAndInterfaceInheritance_StayDistinctAndDeclarationless()
    {
        const string path = "src/contracts.ts";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, """
                interface Repository {}

                class Service implements Repository {}
                interface Specialized extends Repository {}
                """));

        var declaredRepository = SingleNodeOrDefault(extracted, node =>
            node.Kind == CodeNodeKinds.Interface &&
            node.QualifiedName == "Repository" &&
            node.SymbolId is not null);
        Assert.NotNull(declaredRepository);

        var candidate = Assert.Single(extracted.Nodes, node =>
            node.SymbolId is null && node.QualifiedName == "Repository");
        Assert.Equal(CandidateId("typescript", path, "Repository"), candidate.Id.Value);

        var implements = Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Implements);
        var interfaceInherits = Assert.Single(extracted.Edges, edge =>
            edge.Kind == CodeEdgeKinds.Inherits);
        Assert.Equal(EvidenceQuality.Structural, implements.Evidence.Quality);
        Assert.Equal(EvidenceQuality.Structural, interfaceInherits.Evidence.Quality);

        var service = SingleNodeOrDefault(extracted, node => node.QualifiedName == "Service")!;
        var specialized = SingleNodeOrDefault(extracted, node => node.QualifiedName == "Specialized")!;
        Assert.Equal(service.Id, implements.SourceId);
        Assert.Equal(specialized.Id, interfaceInherits.SourceId);
        Assert.Equal(candidate.Id, implements.TargetId);
        Assert.Equal(candidate.Id, interfaceInherits.TargetId);

        // Neither relationship binds to the declared interface.
        Assert.NotEqual(declaredRepository.Id, implements.TargetId);
        Assert.NotEqual(declaredRepository.Id, interfaceInherits.TargetId);
        Assert.DoesNotContain(extracted.Edges, edge =>
            edge.TargetId == declaredRepository.Id &&
            (edge.Kind == CodeEdgeKinds.Implements || edge.Kind == CodeEdgeKinds.Inherits));
        Assert.DoesNotContain(extracted.Edges, edge => edge.Evidence.Quality == EvidenceQuality.Resolved);
    }

    // Q2-CE6 (spec Test E): recognized prototype idioms keep Derived evidence
    // with declaration-less candidates that never collide with declared
    // classes or functions; dynamic patterns invent nothing.
    [Fact]
    public async Task Q2CE6_PrototypePatterns_KeepDerivedEvidenceWithDeclarationlessCandidates()
    {
        const string path = "src/prototype.js";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterJavaScriptPlugin(),
            (path, """
                function Child() {}
                function Other() {}
                function Third() {}
                class Base {}

                Child.prototype = Object.create(Base.prototype);
                util.inherits(Other, Base);
                Object.setPrototypeOf(Third.prototype, Base.prototype);
                Ghost.prototype.__proto__ = Base.prototype;
                util.inherits(Dyn, registry[name]);
                """));

        var inherits = extracted.Edges.Where(edge => edge.Kind == CodeEdgeKinds.Inherits).ToArray();
        Assert.Equal(3, inherits.Length);
        Assert.All(inherits, edge => Assert.Equal(EvidenceQuality.Derived, edge.Evidence.Quality));

        var expectedPairs = new[]
        {
            ("Child", "Base"),
            ("Other", "Base"),
            ("Third", "Base")
        };
        foreach (var (child, parent) in expectedPairs)
        {
            var childId = new CodeNodeId(CandidateId("javascript", path, child));
            var parentId = new CodeNodeId(CandidateId("javascript", path, parent));
            Assert.Contains(inherits, edge =>
                edge.SourceId == childId &&
                edge.TargetId == parentId &&
                edge.Evidence.Location!.Path == path);
        }

        // Child and parent candidates are declaration-less and disjoint from
        // the declared class and functions.
        foreach (var writtenName in new[] { "Child", "Other", "Third", "Base" })
        {
            var candidate = Assert.Single(extracted.Nodes, node =>
                node.SymbolId is null && node.QualifiedName == writtenName);
            Assert.Equal(CandidateId("javascript", path, writtenName), candidate.Id.Value);
            var declared = SingleNodeOrDefault(extracted, node =>
                node.SymbolId is not null && node.QualifiedName == writtenName);
            Assert.NotNull(declared);
            Assert.NotEqual(declared!.Id, candidate.Id);
        }

        // Dynamic/unsupported patterns invent no candidates or edges.
        Assert.DoesNotContain(extracted.Nodes, node =>
            node.QualifiedName is "Ghost" or "Dyn" or "registry");
        Assert.Equal(4, extracted.Nodes.Count(node =>
            node.Id.Value.StartsWith("javascript:heritage-target:", StringComparison.Ordinal)));
        Assert.DoesNotContain(inherits, edge =>
            edge.SourceId == new CodeNodeId(CandidateId("javascript", path, "Ghost")) ||
            edge.SourceId == new CodeNodeId(CandidateId("javascript", path, "Dyn")));
    }

    // Q2-CE7: declared class, interface, and member identities keep the exact
    // EC-3 formulas in the presence of heritage clauses — EC-3D changed no
    // declared identity.
    [Fact]
    public async Task Q2CE7_DeclaredIdentities_KeepExactEc3FormulasBesideHeritage()
    {
        const string path = "src/declared-identities.ts";
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            (path, """
                interface Contract {
                  commit(): number;
                }

                class Widget extends Contract implements Contract {
                  run(): number {
                    return 1;
                  }
                  count = 0;
                }
                """));

        var classHash = Sha($"class\n{path}\nWidget");
        var widget = Assert.Single(extracted.Nodes, node => node.Id == new CodeNodeId($"typescript:symbol:{classHash}"));
        Assert.NotNull(widget.SymbolId);
        Assert.Equal($"typescript:{classHash}", widget.SymbolId!.Value);

        var interfaceHash = Sha($"interface\n{path}\nContract");
        Assert.Contains(extracted.Nodes, node => node.Id == new CodeNodeId($"typescript:symbol:{interfaceHash}"));

        var methodHash = Sha($"method\n{new CodeNodeId($"typescript:symbol:{classHash}")}\ninstance\nrun");
        Assert.Contains(extracted.Nodes, node => node.Id == new CodeNodeId($"typescript:symbol:{methodHash}"));

        var fieldHash = Sha($"field\n{new CodeNodeId($"typescript:symbol:{classHash}")}\ninstance\ncount");
        Assert.Contains(extracted.Nodes, node => node.Id == new CodeNodeId($"typescript:symbol:{fieldHash}"));

        var interfaceMethodHash = Sha($"interface-method\n{new CodeNodeId($"typescript:symbol:{interfaceHash}")}\ncommit");
        Assert.Contains(extracted.Nodes, node => node.Id == new CodeNodeId($"typescript:symbol:{interfaceMethodHash}"));

        // The written names in heritage clauses do not alter any symbol id.
        Assert.All(extracted.Nodes.Where(node => node.SymbolId is not null), node =>
            Assert.StartsWith("typescript:symbol:", node.Id.Value, StringComparison.Ordinal));
    }

    // Q2-CE8: repeated extraction in different file orders is deterministic
    // for heritage-bearing fixtures — same node, declaration, and edge ids.
    [Fact]
    public async Task Q2CE8_HeritageExtraction_IsDeterministicAcrossOrderings()
    {
        var first = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/child.ts", "class Child extends Base {}\n"),
            ("src/shadow.ts", """
                class Base {}
                function create() {
                  class Base {}
                  class Derived extends Base {}
                }
                """),
            ("src/contracts.ts", """
                interface Repository {}
                class Service implements Repository {}
                """));
        var second = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/contracts.ts", """
                interface Repository {}
                class Service implements Repository {}
                """),
            ("src/shadow.ts", """
                class Base {}
                function create() {
                  class Base {}
                  class Derived extends Base {}
                }
                """),
            ("src/child.ts", "class Child extends Base {}\n"));

        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first.Nodes),
            System.Text.Json.JsonSerializer.Serialize(second.Nodes));
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first.Declarations),
            System.Text.Json.JsonSerializer.Serialize(second.Declarations));
        Assert.Equal(
            System.Text.Json.JsonSerializer.Serialize(first.Edges),
            System.Text.Json.JsonSerializer.Serialize(second.Edges));
    }

    // Q2-CE9: heritage in unsupported or under-modeled forms must fabricate
    // nothing beyond the documented subset.
    [Fact]
    public async Task Q2CE9_UnsupportedHeritageForms_FabricateNothing()
    {
        var extracted = await TreeSitterTestSupport.ExtractAsync(
            new TreeSitterTypeScriptPlugin(),
            ("src/unsupported.ts", """
                class Base<T> {}

                class Generic extends Base<string> {}

                const Mixed = class extends Base {
                    run() {}
                };
                """));

        // The generic class declaration exists with its written name, and the
        // written `extends Base<string>` resolves to the name-subset
        // candidate "Base" (generic arguments are not modeled).
        Assert.Single(extracted.Nodes, node =>
            node.Kind == CodeNodeKinds.Type &&
            node.QualifiedName == "Base" &&
            node.SymbolId is not null);
        var candidate = Assert.Single(extracted.Nodes, node =>
            node.Id.Value.StartsWith("typescript:heritage-target:", StringComparison.Ordinal));
        Assert.Equal("Base", candidate.QualifiedName);
        Assert.Equal(
            CandidateId("typescript", "src/unsupported.ts", "Base"),
            candidate.Id.Value);
        Assert.Single(extracted.Edges, edge => edge.Kind == CodeEdgeKinds.Inherits);

        // A class expression's heritage is inside an unsupported declaration
        // form: no extra candidate or inherits edge is fabricated for it.
        Assert.Single(extracted.Nodes, node =>
            node.QualifiedName == "Generic" && node.SymbolId is not null);
    }
}
