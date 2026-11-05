using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp5Requalification;

/// <summary>
/// Independent WP5-Q2 requalification probes. Expected `usage4`/`asmctx3` values are
/// reconstructed from the accepted frozen R2 authority plus independently written
/// structural tuples; the production extractor and `MintUsage` are never used to
/// derive expectations. Every refusal probe first pins the actual Roslyn binding
/// premise (`Symbol`, `CandidateSymbols`, `CandidateReason`) before asserting the
/// extractor outcome.
/// </summary>
public sealed class Wp5Q2ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "review:wp5-q2";
    private static readonly CodeRepositoryId Repository = new(Repo);
    private static readonly CodePluginId Plugin = new("review.wp5q2.plugin");
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private static readonly Func<IMethodSymbol, string> IdDigest = m => "qid:" + m.Name;
    private static readonly Func<IMethodSymbol, string> DeclDigest = m => "qdecl:" + m.ContainingType.Name + "." + m.Name;
    private static readonly Func<IMethodSymbol, string> ShapeDigest = m => "qmethod|" + m.Parameters.Length;
    private static readonly Func<IMethodSymbol, string> TargetDoc = m => m.GetDocumentationCommentId() ?? m.Name;

    // ============================================================ F1-A / F1-B matrix

    [Fact]
    public async Task Q2_F1A_OriginalCounterexample_RefusesTrustedEmission()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }");

        var binding = Binding(unit, "Amb");
        Assert.Null(binding.Symbol);
        Assert.Equal(2, binding.CandidateCount);
        output.WriteLine($"Q2 F1A premise: candidates={binding.CandidateCount} reason={binding.Reason} first={binding.CandidateDocs[0]}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    [Fact]
    public async Task Q2_ThreeCandidates_RefuseTrustedEmission()
    {
        // `Pick(null)` is equally applicable to three unrelated reference parameters.
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class A { } public sealed class B { } public sealed class C { }\n" +
            "public static class M {\n" +
            "  public static int Pick(A a) => 1;\n" +
            "  public static int Pick(B b) => 2;\n" +
            "  public static int Pick(C c) => 3;\n" +
            "}\n" +
            "public static class Site { public static int Run() { return M.Pick(null); } }");

        var binding = Binding(unit, "Pick");
        Assert.Null(binding.Symbol);
        Assert.True(binding.CandidateCount >= 3, $"expected >=3 candidates, got {binding.CandidateCount}");
        output.WriteLine($"Q2 three-candidate premise: candidates={binding.CandidateCount} reason={binding.Reason}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
    }

    [Fact]
    public async Task Q2_CandidateOrder_IsIrrelevantToRefusal()
    {
        var first = await Build("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }");
        var second = await Build("S2/S2.csproj", "S2",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "}\n" +
            "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }");

        var firstBinding = Binding(first, "Amb");
        var secondBinding = Binding(second, "Amb");
        Assert.Null(firstBinding.Symbol);
        Assert.Null(secondBinding.Symbol);
        // The premise: the "first" candidate genuinely differs between the two orders.
        Assert.NotEqual(firstBinding.CandidateDocs[0], secondBinding.CandidateDocs[0]);
        output.WriteLine($"Q2 order premise: first-swap first={firstBinding.CandidateDocs[0]} second-swap first={secondBinding.CandidateDocs[0]}");

        var a = Extract(first);
        var b = Extract(second);
        Assert.Empty(a.Facts);
        Assert.Empty(b.Facts);
        Assert.Equal(1, a.Coverage.Ambiguous);
        Assert.Equal(1, b.Coverage.Ambiguous);
        Assert.Equal(0, a.Coverage.Unresolved);
        Assert.Equal(0, b.Coverage.Unresolved);
    }

    [Fact]
    public async Task Q2_SingleNonApplicableCandidate_UnresolvedRefusal()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int Only(int x) => x; }\n" +
            "public static class Site { public static int Run() { return H.Only(\"x\"); } }");

        var binding = Binding(unit, "Only");
        Assert.Null(binding.Symbol);
        Assert.Equal(1, binding.CandidateCount);
        output.WriteLine($"Q2 single-candidate premise: candidates=1 reason={binding.Reason}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Unresolved);
        Assert.Equal(0, extraction.Coverage.Ambiguous);
    }

    [Fact]
    public async Task Q2_ZeroCandidates_UnresolvedRefusal()
    {
        var unit = await Build("S/S.csproj", "S",
            "public static class Site { public static int Run() { return Missing.Type.Invoke(); } }");

        var binding = Binding(unit, "Invoke");
        Assert.Null(binding.Symbol);
        Assert.Equal(0, binding.CandidateCount);

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Unresolved);
        Assert.Equal(0, extraction.Coverage.Ambiguous);
    }

    [Fact]
    public async Task Q2_DynamicReceiver_UnresolvedRefusal()
    {
        // Oracle-corpus N03 pattern: dynamic dispatch is not a uniquely bound method.
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int Direct(int v) => v; }\n" +
            "public static class Site { public static int Run(N.H d) { return d.Direct(1); } }");
        // Rewrite: dynamic receiver.
        var dynamicUnit = await Build("D/D.csproj", "D",
            "public static class Site {\n" +
            "  public static int Run(dynamic d) { return d.Direct(1); }\n" +
            "}");

        var binding = Binding(dynamicUnit, "Direct");
        Assert.Null(binding.Symbol);
        Assert.Equal(0, binding.CandidateCount);
        output.WriteLine($"Q2 dynamic premise: candidates=0 reason={binding.Reason}");

        var extraction = Extract(dynamicUnit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Unresolved);
    }

    [Fact]
    public async Task Q2_InapplicableCandidatePair_ConservativeRefusal()
    {
        // Two candidates, none applicable: not a genuine ambiguity between live
        // bindings, but the extractor has no unique binding either way.
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H {\n" +
            "  public static int Add(int a, int b) => a + b;\n" +
            "  public static int Add(int a, int b, int c) => a + b + c;\n" +
            "}\n" +
            "public static class Site { public static int Run() { return H.Add(\"a\"); } }");

        var binding = Binding(unit, "Add");
        Assert.Null(binding.Symbol);
        Assert.Equal(2, binding.CandidateCount);
        output.WriteLine($"Q2 inapplicable premise: candidates=2 reason={binding.Reason} -> classified Ambiguous={1}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        // The bounded R1 mapping counts every multi-candidate non-binding as Ambiguous.
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
    }

    [Fact]
    public async Task Q2_InaccessibleMember_UniqueBindingObservation()
    {
        // Accessibility is a semantic diagnostic, not a binding failure; observe the
        // actual Roslyn result and the extractor's treatment of a unique binding.
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class C { private int Hidden() => 1; }\n" +
            "public static class Site { public static int Run(N.C c) { return c.Hidden(); } }");

        var binding = Binding(unit, "Hidden");
        output.WriteLine($"Q2 inaccessible premise: symbolNull={binding.Symbol is null} candidates={binding.CandidateCount} reason={binding.Reason}");

        var extraction = Extract(unit);
        output.WriteLine($"Q2 inaccessible outcome: facts={extraction.Facts.Length} unresolved={extraction.Coverage.Unresolved} ambiguous={extraction.Coverage.Ambiguous}");
        if (binding.Symbol is null)
        {
            Assert.Empty(extraction.Facts);
        }
        else
        {
            // Unique textual binding: the bounded WP5 contract gates on the binding,
            // not on compilation diagnostics; the mint is the accepted authority path.
            Assert.Single(extraction.Facts);
        }
    }

    [Fact]
    public async Task Q2_MixedClassifications_DisjointAccounting()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class H { public static int Add(int a, int b) => a + b; }\n" +
            "public static class Site {\n" +
            "  public static int Good() { return H.Add(1, 2); }\n" +
            "  public static int Bad() { var m = new M(); return m.Amb(1, 1); }\n" +
            "  public static int Lost() { return Missing.Type.Invoke(); }\n" +
            "}");

        var extraction = Extract(unit);
        var fact = Assert.Single(extraction.Facts);
        var method = Target(unit.Compilation, "N.H", "Add", 2);
        Assert.Equal(ExpectedUsage4(unit, unit, "Add", method), fact.Id.Value);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(1, extraction.Coverage.Unresolved);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    [Fact]
    public async Task Q2_MultipleAmbiguousSites_AccumulateWithoutFacts()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site {\n" +
            "  public static int One() { var m = new M(); return m.Amb(1, 1); }\n" +
            "  public static int Two() { var m = new M(); return m.Amb(2, 2); }\n" +
            "}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(2, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
    }

    // ============================================================ F1-C non-regression

    [Fact]
    public async Task Q2_ValidInvocations_KeepExactAcceptedIdentities()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H {\n" +
            "  public static int Stat(int x) => x;\n" +
            "  public int Inst(int x) => x;\n" +
            "  public static int Add(int a, int b) => a + b;\n" +
            "  public static int Add(int a, int b, int c) => a + b + c;\n" +
            "  public static T Id<T>(T v) => v;\n" +
            "}\n" +
            "public static class Site {\n" +
            "  public static int Direct() { return H.Stat(1); }\n" +
            "  public static int Instance() { return new H().Inst(2); }\n" +
            "  public static int Over() { return H.Add(1, 2); }\n" +
            "  public static int Generic() { return H.Id<int>(3); }\n" +
            "}");

        var extraction = Extract(unit);
        Assert.Equal(4, extraction.Facts.Length);
        Assert.Equal(4, extraction.Facts.Select(f => f.Id.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(0, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);

        // Independent identity checks for representative sites.
        var stat = Target(unit.Compilation, "N.H", "Stat", 1);
        var statFact = extraction.Facts.Single(f => f.TargetDocumentId == TargetDoc(stat));
        Assert.Equal(ExpectedAsmctx3(unit, stat), statFact.TargetId.Value);
        Assert.Equal(ExpectedUsage4(unit, unit, "Stat", stat), statFact.Id.Value);

        var add = Target(unit.Compilation, "N.H", "Add", 2);
        var addFact = extraction.Facts.Single(f => f.TargetDocumentId == TargetDoc(add));
        Assert.Equal(ExpectedAsmctx3(unit, add), addFact.TargetId.Value);
        Assert.Equal(ExpectedUsage4(unit, unit, "Add", add), addFact.Id.Value);

        var generic = Target(unit.Compilation, "N.H", "Id", 1);
        var bindingGeneric = Binding(unit, i => i.Expression.ToString().Contains(".Id<")).Symbol;
        Assert.NotNull(bindingGeneric);
        output.WriteLine($"Q2 generic premise: definitionDoc={TargetDoc(generic)} constructedDoc={bindingGeneric!.GetDocumentationCommentId() ?? "<null>"}");
        // The extractor records the constructed symbol's documentation id; expectations
        // are therefore derived from the bound (constructed) symbol independently.
        var genericFact = extraction.Facts.Single(f => f.TargetDocumentId == bindingGeneric.GetDocumentationCommentId());
        Assert.Equal(ExpectedUsage4(unit, unit, i => i.Expression.ToString().Contains(".Id<"), bindingGeneric), genericFact.Id.Value);
    }

    [Fact]
    public async Task Q2_CrossProjectMultipleDependencies_ExactTargets()
    {
        var l1 = await Build("L1/L1.csproj", "L1", "namespace A; public static class L1 { public static int One() => 1; }");
        var l2 = await Build("L2/L2.csproj", "L2", "namespace B; public static class L2 { public static int Two() => 2; }");
        var app = await BuildWithDeps("App/App.csproj", "App",
            "namespace N; public static class App { public static int Run() => A.L1.One() + B.L2.Two(); }", l1, l2);
        var universe = Universe(app, l1, l2);

        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(universe, Request(app));
        Assert.Equal(2, extraction.Facts.Length);
        var one = Target(l1.Compilation, "A.L1", "One", 0);
        var two = Target(l2.Compilation, "B.L2", "Two", 0);
        var targets = extraction.Facts.Select(f => f.TargetId.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(ExpectedAsmctx3(l1, one), targets);
        Assert.Contains(ExpectedAsmctx3(l2, two), targets);
        Assert.Equal(2, extraction.Facts.Select(f => f.Id.Value).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Q2_RepeatedSameTarget_DistinctUsages()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int Get() => 1; }\n" +
            "public static class Site {\n" +
            "  public static int A() { return H.Get(); }\n" +
            "  public static int B() { return H.Get(); }\n" +
            "}");

        var extraction = Extract(unit);
        Assert.Equal(2, extraction.Facts.Length);
        Assert.Equal(2, extraction.Facts.Select(f => f.Id.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(extraction.Facts.Select(f => f.TargetId.Value).Distinct(StringComparer.Ordinal));
    }

    // ============================================================ authority boundary

    [Fact]
    public async Task Q2_AuthorityBoundary_RecordedContextRefusals()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int G() => 1; }");
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.H", "G", 0);

        Assert.Null(Mint(universe, unit, method, null).Fact);
        Assert.Null(Mint(universe, unit, method, "").Fact);
        Assert.Null(Mint(universe, unit, method, "not-a-context").Fact);
        Assert.Null(Mint(universe, unit, method, "compctx2:sha256:" + new string('a', 64)).Fact);
        Assert.Null(Mint(universe, unit, method, "compctx2:sha256:" + new string('0', 64)).Fact);
        Assert.NotNull(Mint(universe, unit, method, unit.Descriptor.DescriptorDigest).Fact);
    }

    [Fact]
    public async Task Q2_SubstitutedDefiningCompilation_NoAuthority()
    {
        var text = "namespace N; public static class H { public static int G() => 1; }";
        var a = await Build("H/H.csproj", "H", text);
        var b = await Build("H/H.csproj", "H", text);
        // Metadata-equivalent targets (accepted G1/W3 qualification): same descriptor digest.
        Assert.Equal(a.Descriptor.DescriptorDigest, b.Descriptor.DescriptorDigest);

        // Authority binds to the exact verified instance in the universe, not the
        // metadata identity: the B symbol cannot mint against A's verified context.
        var universeA = Universe(a);
        var symbolB = Target(b.Compilation, "N.H", "G", 0);
        var mint = Mint(universeA, a, symbolB, a.Descriptor.DescriptorDigest);
        Assert.False(mint.IsMinted);
        Assert.Null(mint.Fact);
    }

    [Fact]
    public async Task Q2_StructuralIdentifiers_CarryNoMintingAuthority()
    {
        // Canonical structural identifiers are publicly constructible (accepted REM-01
        // boundary) but the extraction request surface accepts no precomputed fact or
        // target id, and a fabricated recorded context still refuses.
        var structural = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "u", "S/S.cs", "sha256:x",
            new CodeLocation("S/S.cs", 1, 1, 1, 2), "M:N.H.G",
            new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('a', 64)),
            CodeOccurrenceCategory.Invocation);
        Assert.True(CodeOccurrenceIdentity.IsCanonicalOccurrence(structural.Value));

        var requestType = typeof(VerifiedOccurrenceExtractionRequest);
        var propertyNames = requestType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToArray();
        Assert.DoesNotContain(propertyNames, n => n.Contains("Fact") || n.Contains("TargetId") || n.Contains("Usage4"));

        var unit = await Build("S/S.csproj", "S", "namespace N; public static class H { public static int G() => 1; }");
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.H", "G", 0);
        Assert.Null(Mint(universe, unit, method, "compctx2:sha256:" + new string('f', 64)).Fact);
    }

    // ============================================================ coverage accounting

    [Fact]
    public async Task Q2_ZeroOccurrence_CompleteProduced()
    {
        var unit = await Build("S/S.csproj", "S", "namespace N; public static class Site { public static int Value => 1; }");
        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Assert.True(extraction.Coverage.IsComplete);
        Assert.Equal(0, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
    }

    [Fact]
    public async Task Q2_OnlyRefusedSites_ProducedWithRefusalCounters()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site {\n" +
            "  public static int Bad() { var m = new M(); return m.Amb(1, 1); }\n" +
            "  public static int Lost() { return Missing.Type.Invoke(); }\n" +
            "}");

        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(1, extraction.Coverage.Unresolved);
        // The coverage state tracks extraction completeness of the verified universe,
        // not fact success; the refused occurrences remain visible in their counters.
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Assert.True(extraction.Coverage.IsComplete);
    }

    [Fact]
    public async Task Q2_RepeatedExtraction_StableCountsAndFacts()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int G() => 1; }\n" +
            "public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site {\n" +
            "  public static int Good() { return H.G(); }\n" +
            "  public static int Bad() { var m = new M(); return m.Amb(1, 1); }\n" +
            "}");

        var universe = Universe(unit);
        var first = VerifiedCSharpOccurrenceExtractor.Extract(universe, Request(unit));
        var second = VerifiedCSharpOccurrenceExtractor.Extract(universe, Request(unit));
        Assert.Equal(
            first.Facts.Select(f => f.Id.Value).Order(StringComparer.Ordinal),
            second.Facts.Select(f => f.Id.Value).Order(StringComparer.Ordinal));
        Assert.Equal(first.Coverage.InvocationFacts, second.Coverage.InvocationFacts);
        Assert.Equal(first.Coverage.Ambiguous, second.Coverage.Ambiguous);
        Assert.Equal(first.Coverage.Unresolved, second.Coverage.Unresolved);
        Assert.Equal(1, first.Coverage.Ambiguous);
        _ = Assert.Single(first.Facts);
    }

    [Fact]
    public async Task Q2_SupportedUnsupportedMix_F3AccountingReobserved()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int G(int x) => x; }\n" +
            "public static class Site {\n" +
            "  public static int Good() { return H.G(1); }\n" +
            "  public static int Local() { int L(int x) => x; return L(1); }\n" +
            "  public static int Delegate() { System.Func<int> f = () => 1; return f(); }\n" +
            "}");

        var extraction = Extract(unit);
        Assert.Single(extraction.Facts);
        // F3 (WP5-Q1) reobservation: one unsupported occurrence increments both
        // counters; their documented meanings are not assumed mutually exclusive.
        Assert.Equal(2, extraction.Coverage.Unsupported);
        Assert.Equal(2, extraction.Coverage.UnsupportedCategories);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    // ============================================================ F09 reobservation

    [Fact]
    public async Task Q2_F09_DisabledText_NeverMintsFacts()
    {
        const string conditional =
            "namespace N; public static class H { public static int Get(int x) => x; }\n" +
            "public static class Site {\n" +
            "#if DEBUG\n" +
            "  public static int Run() { return H.Get(1); }\n" +
            "#endif\n" +
            "}";

        var disabled = await BuildWithSymbols("S/S.csproj", "S", conditional, []);
        var disabledExtraction = Extract(disabled);
        Assert.Empty(disabledExtraction.Facts);
        // The bounded heuristic counts invocation-like text in disabled regions,
        // including declaration-shaped text (`Run(`); observed value is recorded.
        output.WriteLine($"Q2 F09 disabled: ConditionalInactive={disabledExtraction.Coverage.ConditionalInactive} (1 real disabled call)");
        Assert.True(disabledExtraction.Coverage.ConditionalInactive >= 1);

        var enabled = await BuildWithSymbols("S/S.csproj", "S", conditional, ["DEBUG"]);
        var enabledExtraction = Extract(enabled);
        Assert.Single(enabledExtraction.Facts);
        Assert.Equal(0, enabledExtraction.Coverage.ConditionalInactive);

        // F2 (WP5-Q1) reobservation: invocation-like text inside disabled comments and
        // strings inflates ConditionalInactive (bounded heuristic), but no disabled
        // text can ever mint a fact.
        const string noise =
            "namespace N; public static class H { public static int Get(int x) => x; }\n" +
            "public static class Site {\n" +
            "#if DEBUG\n" +
            "  // H.Get(9);\n" +
            "  string s = \"H.Get(8)\";\n" +
            "  public static int Run() { return H.Get(1); }\n" +
            "#endif\n" +
            "}";
        var noisy = await BuildWithSymbols("S/S.csproj", "S", noise, []);
        var noisyExtraction = Extract(noisy);
        Assert.Empty(noisyExtraction.Facts);
        output.WriteLine($"Q2 F09 noise: ConditionalInactive={noisyExtraction.Coverage.ConditionalInactive} (ground truth disabled invocations=1, F2 bounded heuristic)");
        Assert.True(noisyExtraction.Coverage.ConditionalInactive >= 1);
    }

    // ============================================================ determinism

    [Fact]
    public async Task Q2_SeparateProcessDeterminism()
    {
        var unit = await Build("S/S.csproj", "S",
            "namespace N; public static class H { public static int Get() => 1; }\n" +
            "public static class Site { public static int Run() { return H.Get(); } }");
        var extraction = Extract(unit);
        var fact = Assert.Single(extraction.Facts);
        var method = Target(unit.Compilation, "N.H", "Get", 0);
        output.WriteLine("WP5Q2-USAGE4=" + fact.Id.Value);
        output.WriteLine("WP5Q2-ASMCTX3=" + fact.TargetId.Value);
        output.WriteLine("WP5Q2-FACTS=" + extraction.Facts.Length);
        output.WriteLine("WP5Q2-EXPECTED=" + ExpectedUsage4(unit, unit, "Get", method));
        Assert.Equal(ExpectedUsage4(unit, unit, "Get", method), fact.Id.Value);
    }

    // ------------------------------------------------------------------- helpers

    private VerifiedUsageMint Mint(VerifiedCSharpUniverse universe, VerifiedCSharpCompilation unit,
        IMethodSymbol method, string? recorded) =>
        universe.MintUsage(new VerifiedUsageInput(Repository, Plugin, method, recorded,
            unit.Descriptor.ProjectUnitId, new CodeLocation("S/S.cs", 1, 1, 1, 2),
            "sha256:src", "sha256:content", TargetDoc(method), IdDigest(method),
            DeclDigest(method), ShapeDigest(method), unit.Compilation));

    private VerifiedOccurrenceExtraction Extract(VerifiedCSharpCompilation unit) =>
        VerifiedCSharpOccurrenceExtractor.Extract(Universe(unit), Request(unit));

    private static VerifiedOccurrenceExtractionRequest Request(VerifiedCSharpCompilation unit) =>
        new(Repository, Plugin, unit, IdDigest, DeclDigest, ShapeDigest, TargetDoc);

    private static async Task<VerifiedCSharpCompilation> Build(string unit, string assembly, string text) =>
        await BuildUnit(unit, assembly, text, [], []);

    private static async Task<VerifiedCSharpCompilation> BuildWithSymbols(
        string unit, string assembly, string text, string[] symbols) =>
        await BuildUnit(unit, assembly, text, symbols, []);

    private static async Task<VerifiedCSharpCompilation> BuildWithDeps(
        string unit, string assembly, string text, params VerifiedCSharpCompilation[] deps) =>
        await BuildUnit(unit, assembly, text, [], deps);

    private static async Task<VerifiedCSharpCompilation> BuildUnit(
        string unit, string assembly, string text, string[] symbols, VerifiedCSharpCompilation[] deps)
    {
        var path = unit[..^".csproj".Length] + ".cs";
        var bytes = Encoding.UTF8.GetBytes(text);
        var source = await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Sha256(bytes),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None);
        var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose,
            SourceCodeKind.Regular, symbols);
        var tree = CSharpSyntaxTree.ParseText(source.Text, parse, path);
        var references = new List<MetadataReference> { Platform.Value.Reference };
        references.AddRange(deps.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, [tree], references, Options);
        return VerifiedCSharpCompilation.Create(Repo, unit, compilation, [source], [Platform.Value],
            deps, CancellationToken.None);
    }

    private static VerifiedCSharpUniverse Universe(params VerifiedCSharpCompilation[] entries)
    {
        var units = entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
            e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray())).ToArray();
        return VerifiedCSharpUniverse.Create(Repo, VerifiedCSharpScope.Declare(Repo, units), entries);
    }

    private static (IMethodSymbol? Symbol, int CandidateCount, string Reason, string[] CandidateDocs) Binding(
        VerifiedCSharpCompilation unit, string methodName) =>
        Binding(unit, i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));

    private static (IMethodSymbol? Symbol, int CandidateCount, string Reason, string[] CandidateDocs) Binding(
        VerifiedCSharpCompilation unit, Func<InvocationExpressionSyntax, bool> match)
    {
        var tree = unit.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(match);
        var info = unit.Compilation.GetSemanticModel(tree).GetSymbolInfo(invocation);
        return (info.Symbol as IMethodSymbol, info.CandidateSymbols.Length,
            info.CandidateReason.ToString(),
            info.CandidateSymbols.Select(s => s.ToDisplayString()).ToArray());
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method, int parameterCount)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method)
            .Single(m => m is IMethodSymbol ms && ms.Parameters.Length == parameterCount));
    }

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation defining, IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeTargetId(
            FrozenCompctx1(defining), Metadata(defining), Module(defining),
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).Value;

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling, VerifiedCSharpCompilation defining,
        string methodName, IMethodSymbol method) =>
        ExpectedUsage4(calling, defining,
            i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal), method);

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling, VerifiedCSharpCompilation defining,
        Func<InvocationExpressionSyntax, bool> match, IMethodSymbol method)
    {
        var tree = calling.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(match);
        var token = invocation.Expression is MemberAccessExpressionSyntax ma
            ? ma.Name.Identifier
            : invocation.Expression.GetLastToken();
        var line = tree.GetLineSpan(token.Span);
        var path = tree.FilePath.Replace('\\', '/');
        var sourceHash = calling.Sources.Single(s => s.Path == path).ContentHash;
        var parts = new List<string>
        {
            Repo, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            calling.Descriptor.ProjectUnitId, path, sourceHash,
            (line.StartLinePosition.Line + 1).ToString(),
            (line.StartLinePosition.Character + 1).ToString(),
            (line.EndLinePosition.Line + 1).ToString(),
            (line.EndLinePosition.Character + 1).ToString(),
            TargetDoc(method), ExpectedAsmctx3(defining, method), CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static string FrozenCompctx1(VerifiedCSharpCompilation unit)
    {
        var bytes = unit.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray();
        return Frozen.CompilationProvenance.Describe(Repo, unit.Descriptor.ProjectUnitId, unit.Compilation, bytes)
            .DescriptorDigest;
    }

    private static string Metadata(VerifiedCSharpCompilation unit)
    {
        var identity = unit.Compilation.Assembly.Identity;
        var token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return string.Join("|", identity.Name, identity.Version.ToString(), identity.CultureName ?? string.Empty, token);
    }

    private static string Module(VerifiedCSharpCompilation unit) =>
        unit.Compilation.Assembly.Modules.First().Name;

    private static string LengthPrefixedHash(string domain, IEnumerable<string> parts)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        void Append(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            Span<byte> length = stackalloc byte[4];
            System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        Append(domain);
        foreach (var part in parts) Append(part);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
