using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp5Qualification;

// Independent WP5 qualification probes. Expected `usage4`/`asmctx3` values are derived
// from the accepted frozen R2 authority plus an independently written structural tuple;
// they are never derived from the production extractor or `MintUsage`.
public sealed class Wp5ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "review:wp5-q1";
    private static readonly CodeRepositoryId Repository = new(Repo);
    private static readonly CodePluginId Plugin = new("review.wp5.plugin");
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private static readonly Func<IMethodSymbol, string> IdDigest = m => "id:" + m.Name;
    private static readonly Func<IMethodSymbol, string> DeclDigest = m => "decl:" + m.ContainingType.Name + "." + m.Name;
    private static readonly Func<IMethodSymbol, string> ShapeDigest = m => "method|" + m.Parameters.Length;
    private static readonly Func<IMethodSymbol, string> TargetDoc = m => m.GetDocumentationCommentId() ?? m.Name;

    // ============================================================ trusted emission

    [Fact]
    public async Task Wp5_Ambig_Invocation_MustNotEmitTrustedFact()
    {
        // Two equally-applicable overloads: the invocation is ambiguous and must not
        // become a trusted usage4 (coverage Ambiguous/Unresolved, no fact).
        const string source = @"
namespace N;
public sealed class M {
  public int Amb(int a, long b) => a + (int)b;
  public int Amb(long a, int b) => (int)a + b;
}
public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        output.WriteLine($"WP5Q1 ambig facts={extraction.Facts.Length} ambiguous={extraction.Coverage.Ambiguous} unresolved={extraction.Coverage.Unresolved}");
        Assert.Empty(extraction.Facts);
    }

    [Fact]
    public async Task Wp5_UnresolvedSymbol_NoFact()
    {
        const string source = "public static class Site { public static int Run() { return Missing.Type.Invoke(); } }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.True(extraction.Coverage.Unresolved >= 1);
    }

    [Fact]
    public async Task Wp5_DirectInvocation_EmitsExactAcceptedTuple()
    {
        const string source = "namespace N; public static class Site { public static int Run() { return Helper.Get(); } }\npublic static class Helper { public static int Get() => 1; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Single(extraction.Facts);
        var method = Target(unit.Compilation, "N.Helper", "Get");
        var expectedAsmctx3 = ExpectedAsmctx3(unit, method);
        Assert.Equal(expectedAsmctx3, extraction.Facts[0].TargetId.Value);
        Assert.Equal(ExpectedUsage4(unit, unit, "Get", method), extraction.Facts[0].Id.Value);
        Assert.DoesNotContain("compctx2", extraction.Facts[0].Id.Value);
    }

    [Fact]
    public async Task Wp5_MultipleInvocations_NoCollisionOrDedup()
    {
        const string source =
            "namespace N; public static class Site {\n" +
            "  public static int A() => Helper.Get();\n" +
            "  public static int B() => Helper.Get();\n" +
            "}\npublic static class Helper { public static int Get() => 1; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Equal(2, extraction.Facts.Length);
        Assert.Equal(2, extraction.Facts.Select(f => f.Id.Value).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(extraction.Facts.Select(f => f.TargetId.Value).Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Wp5_CrossProject_MultipleDependencies_ExactTargets()
    {
        var l1 = await Build("L1/L1.csproj", "L1", "namespace A; public static class L1 { public static int One() => 1; }");
        var l2 = await Build("L2/L2.csproj", "L2", "namespace B; public static class L2 { public static int Two() => 2; }");
        var app = await BuildWithDeps("App/App.csproj", "App",
            "namespace N; public static class App { public static int Run() => A.L1.One() + B.L2.Two(); }", l1, l2);
        var universe = Universe(l1, l2, app);
        var extraction = Extract(universe, app);
        Assert.Equal(2, extraction.Facts.Length);
        var one = Target(l1.Compilation, "A.L1", "One");
        var two = Target(l2.Compilation, "B.L2", "Two");
        var targets = extraction.Facts.Select(f => f.TargetId.Value).ToHashSet(StringComparer.Ordinal);
        Assert.Contains(ExpectedAsmctx3(l1, one), targets);
        Assert.Contains(ExpectedAsmctx3(l2, two), targets);
    }

    // ============================================================ authority consumption

    [Fact]
    public async Task Wp5_StructuralTargetId_NotInjectable()
    {
        var props = typeof(VerifiedUsageInput).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("TargetId", props);
        var unit = await Build("S/S.csproj", "S", "namespace N; public static class H { public static int G() => 1; }");
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.H", "G");
        // Null/stale/fabricated recorded context -> no fact.
        Assert.Null(Mint(unit, method, universe, null).Fact);
        Assert.Null(Mint(unit, method, universe, "").Fact);
        Assert.Null(Mint(unit, method, universe, "compctx2:sha256:" + new string('a', 64)).Fact);
        Assert.Null(Mint(unit, method, universe, "not-a-context").Fact);
        Assert.NotNull(Mint(unit, method, universe, unit.Descriptor.DescriptorDigest).Fact);
    }

    [Fact]
    public async Task Wp5_Mint_BypassPathInventoriesNoDirectEmission()
    {
        // A structurally canonical occurrence id is constructible but carries no authority.
        var structural = CodeOccurrenceIdentity.ComputeOccurrenceId(
            Repository, Plugin, "unit", "S/S.cs", "sha256:x",
            new CodeLocation("S/S.cs", 1, 1, 1, 2), "M:N.H.G",
            new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('a', 64)),
            CodeOccurrenceCategory.Invocation);
        Assert.True(CodeOccurrenceIdentity.IsCanonicalOccurrence(structural.Value));
        var unit = await Build("S/S.csproj", "S", "namespace N; public static class H { public static int G() => 1; }");
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.H", "G");
        // The only trusted path (MintUsage) refuses a stale context regardless of any structural id.
        Assert.False(Mint(unit, method, universe, "compctx2:sha256:" + new string('0', 64)).IsMinted);
    }

    [Fact]
    public async Task Wp5_SubstitutedInstance_NoFact()
    {
        var a = await Build("H/H.csproj", "H", "namespace N; public static class H { public static int G() => 1; }");
        var b = await Build("H/H.csproj", "H", "namespace N; public static class H { public static int G() => 1; }");
        Assert.Equal(a.Descriptor.DescriptorDigest, b.Descriptor.DescriptorDigest);
        var universe = Universe(a);
        var method = Target(b.Compilation, "N.H", "G");
        Assert.Null(Mint(a, method, universe, a.Descriptor.DescriptorDigest).Fact);
    }

    // ============================================================ extraction correctness

    [Fact]
    public async Task Wp5_Extraction_Matrix()
    {
        const string source = @"
using System;
namespace N;
public static class Ext { public static int Twice(this int x) => x * 2; }
public sealed class C { public int P { get; set; } public C() {} }
public static class Site {
  public static int Stat(int x) => H.Get(x);
  public static int Over(int x) => H.Add(1) + H.Add(1, 2);
  public static int Gen(int x) => H.Identity<int>(x);
  public static int Chain(int x) => x.Twice().ToString().Length;
  public static int Nest() => H.Get(H.Add(1));
  public static int Cond(bool b) => b ? H.Get(1) : H.Get(2);
  public static async System.Threading.Tasks.Task<int> Async() { await System.Threading.Tasks.Task.Yield(); return H.Get(1); }
  public static int Lambda() { Func<int> f = () => H.Get(3); return f(); }
  public static int Local() { int L(int x) => H.Get(x); return L(1); }
}
public static class H {
  public static int Get(int x) => x;
  public static int Add(int a, int b = 5) => a + b;
  public static T Identity<T>(T v) => v;
}";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        output.WriteLine($"WP5Q1 matrix facts={extraction.Facts.Length} unsupported={extraction.Coverage.Unsupported} unresolved={extraction.Coverage.Unresolved}");
        // Supported invocations (Stat, Over x2, Gen, Chain x2 via Twice/ToString, Nest x2, Cond x2, Async, Lambda-internal H.Get, Local-internal H.Get) plus the unsupported local `f()` delegate.
        Assert.True(extraction.Facts.Length >= 10, "supported invocations should be extracted");
        // The delegate invocation f() and local function L(1) are unsupported.
        Assert.True(extraction.Coverage.Unsupported >= 2);
    }

    [Fact]
    public async Task Wp5_InvocationLikeTextInActiveStrings_NotExtracted()
    {
        const string source =
            "namespace N; public static class Site { public static string Run() => \"H.G()\"; }\n" +
            "public static class H { public static int G() => 1; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
    }

    // ============================================================ F09 matrix

    [Fact]
    public async Task Wp5_F09_Matrix()
    {
        // (a) disabled real invocation -> ConditionalInactive, no fact.
        var disabled = await BuildWithSymbols("S/S.csproj", "S",
            "namespace N; public static class Site {\n#if DEBUG\n public static int Run() => H.Get(1);\n#endif\n}\npublic static class H { public static int Get(int x) => x; }", []);
        var a = Extract(disabled);
        Assert.Empty(a.Facts);
        Assert.True(a.Coverage.ConditionalInactive >= 1);

        // (b) enabled -> fact, zero ConditionalInactive.
        var enabled = await BuildWithSymbols("S/S.csproj", "S",
            "namespace N; public static class Site {\n#if DEBUG\n public static int Run() => H.Get(1);\n#endif\n}\npublic static class H { public static int Get(int x) => x; }", ["DEBUG"]);
        var b = Extract(enabled);
        Assert.Single(b.Facts);
        Assert.Equal(0, b.Coverage.ConditionalInactive);

        // (c) #if/#else: only the active branch is a fact; the inactive branch is coverage.
        const string elseSrc =
            "namespace N; public static class Site {\n#if DEBUG\n public static int Run() => H.Get(1);\n#else\n public static int Run() => H.Get(2);\n#endif\n}\npublic static class H { public static int Get(int x) => x; }";
        var c = Extract(await BuildWithSymbols("S/S.csproj", "S", elseSrc, []));
        Assert.Single(c.Facts);
        Assert.True(c.Coverage.ConditionalInactive >= 1);

        // (d) comment/string/real inside disabled text: coverage is a bounded heuristic.
        const string noise =
            "namespace N; public static class Site {\n#if DEBUG\n // H.Get(9);\n string s = \"H.Get(8)\";\n public static int Run() => H.Get(1);\n#endif\n}\npublic static class H { public static int Get(int x) => x; }";
        var d = Extract(await BuildWithSymbols("S/S.csproj", "S", noise, []));
        Assert.Empty(d.Facts);
        output.WriteLine($"WP5Q1 F09 noise ConditionalInactive={d.Coverage.ConditionalInactive} (ground-truth disabled invocations=1)");
        Assert.True(d.Coverage.ConditionalInactive >= 1);

        // (e) generic/qualified invocation-like text in disabled text.
        const string generic =
            "namespace N; public static class Site {\n#if DEBUG\n public static int Run() => H.Id<int>(1);\n#endif\n}\npublic static class H { public static T Id<T>(T v) => v; }";
        var e = Extract(await BuildWithSymbols("S/S.csproj", "S", generic, []));
        output.WriteLine($"WP5Q1 F09 generic ConditionalInactive={e.Coverage.ConditionalInactive} (ground-truth disabled invocations=1)");
    }

    // ============================================================ coverage accounting

    [Fact]
    public async Task Wp5_Coverage_MixedCorpus_DisjointAndNoLoss()
    {
        const string source =
            "namespace N; public static class Site {\n" +
            "  public static int Good() => H.Get(1);\n" +            // confirmed
            "  public static int Over(int x) => H.Add(1) + H.Add(1, 2);\n" + // confirmed x2
            "  public static void Ext() { Missing.Type.Invoke(); }\n" + // unresolved
            "  public static int Local() { int L(int x) => x; return L(1); }\n" + // unsupported (local function invocation)
            "}\npublic static class H { public static int Get(int x) => x; public static int Add(int a, int b = 5) => a + b; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        output.WriteLine($"WP5Q1 mixed facts={extraction.Facts.Length} unsupported={extraction.Coverage.Unsupported} unresolved={extraction.Coverage.Unresolved} produced={extraction.Coverage.State}");
        Assert.Equal(3, extraction.Facts.Length);
        Assert.True(extraction.Coverage.Unresolved >= 1);
        Assert.True(extraction.Coverage.Unsupported >= 1);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    [Fact]
    public async Task Wp5_ZeroOccurrence_CompleteEmpty()
    {
        var unit = await Build("S/S.csproj", "S", "namespace N; public static class Site { public static int Value => 1; }");
        var extraction = Extract(unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Assert.True(extraction.Coverage.IsComplete);
    }

    [Fact]
    public async Task Wp5_IncompleteUniverse_IsPartial()
    {
        var unit = await Build("S/S.csproj", "S", "namespace N; public static class Site { public static int Run() => 1; }");
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("S/S.csproj", "S/S.csproj", ["S/S.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [unit]);
        var extraction = Extract(universe, unit);
        Assert.Equal(CodeRelationshipCoverageState.Partial, extraction.Coverage.State);
        Assert.False(extraction.Coverage.IsComplete);
    }

    // ============================================================ targetDocumentId

    [Fact]
    public async Task Wp5_TargetDocumentId_OverloadsDistinct()
    {
        const string source =
            "namespace N; public static class Site { public static int A() => H.M(1); public static string B() => H.M(\"x\"); }\n" +
            "public static class H { public static int M(int x) => x; public static string M(string x) => x; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Equal(2, extraction.Facts.Length);
        var docs = extraction.Facts.Select(f => f.TargetDocumentId).ToArray();
        output.WriteLine("WP5Q1 targetDocuments=" + string.Join(",", docs));
        Assert.Equal(2, docs.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, extraction.Facts.Select(f => f.Id.Value).Distinct(StringComparer.Ordinal).Count());
    }

    // ============================================================ determinism

    [Fact]
    public async Task Wp5_SeparateProcessDigest()
    {
        const string source = "namespace N; public static class Site { public static int Run() { return Helper.Get(); } }\npublic static class Helper { public static int Get() => 1; }";
        var unit = await Build("S/S.csproj", "S", source);
        var extraction = Extract(unit);
        Assert.Single(extraction.Facts);
        output.WriteLine("WP5Q1-USAGE4=" + extraction.Facts[0].Id.Value);
        output.WriteLine("WP5Q1-ASMCTX3=" + extraction.Facts[0].TargetId.Value);
        output.WriteLine("WP5Q1-FACTS=" + extraction.Facts.Length);
        output.WriteLine("WP5Q1-EXPECTED=" + ExpectedUsage4(unit, unit, "Get", Target(unit.Compilation, "N.Helper", "Get")));
    }

    // ------------------------------------------------------------------- helpers

    private VerifiedUsageMint Mint(VerifiedCSharpCompilation unit, IMethodSymbol method,
        VerifiedCSharpUniverse universe, string? recorded) =>
        universe.MintUsage(new VerifiedUsageInput(Repository, Plugin, method, recorded,
            unit.Descriptor.ProjectUnitId, new CodeLocation("S/S.cs", 1, 1, 1, 2),
            "sha256:src", "sha256:content", TargetDoc(method), IdDigest(method), DeclDigest(method),
            ShapeDigest(method), unit.Compilation));

    private VerifiedOccurrenceExtraction Extract(VerifiedCSharpCompilation unit) => Extract(Universe(unit), unit);

    private static VerifiedOccurrenceExtraction Extract(VerifiedCSharpUniverse universe, VerifiedCSharpCompilation unit) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe, new VerifiedOccurrenceExtractionRequest(
            Repository, Plugin, unit, IdDigest, DeclDigest, ShapeDigest, TargetDoc));

    private async Task<VerifiedCSharpCompilation> Build(string unit, string assembly, string text) =>
        await BuildUnit(unit, assembly, text, [], []);

    private async Task<VerifiedCSharpCompilation> BuildWithSymbols(string unit, string assembly, string text, string[] symbols) =>
        await BuildUnit(unit, assembly, text, symbols, []);

    private async Task<VerifiedCSharpCompilation> BuildWithDeps(string unit, string assembly, string text, params VerifiedCSharpCompilation[] deps) =>
        await BuildUnit(unit, assembly, text, [], deps);

    private static async Task<VerifiedCSharpCompilation> BuildUnit(string unit, string assembly, string text,
        string[] symbols, VerifiedCSharpCompilation[] deps)
    {
        var path = unit[..^".csproj".Length] + ".cs";
        var bytes = Encoding.UTF8.GetBytes(text);
        var source = await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Sha256(bytes),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None);
        var parse = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular, symbols);
        var tree = CSharpSyntaxTree.ParseText(source.Text, parse, path);
        var references = new List<MetadataReference> { Platform.Value.Reference };
        references.AddRange(deps.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, [tree], references, Options);
        return VerifiedCSharpCompilation.Create(Repo, unit, compilation, [source], [Platform.Value], deps, CancellationToken.None);
    }

    private static VerifiedCSharpUniverse Universe(params VerifiedCSharpCompilation[] entries)
    {
        var units = entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
            e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray())).ToArray();
        return VerifiedCSharpUniverse.Create(Repo, VerifiedCSharpScope.Declare(Repo, units), entries);
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation defining, IMethodSymbol method) =>
        "asmctx3:sha256:" + LengthPrefixedHash("asmctx3-context",
            ["compctx1", FrozenCompctx1(defining), Metadata(defining), Module(defining),
             IdDigest(method), DeclDigest(method), ShapeDigest(method)]);

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling, VerifiedCSharpCompilation defining,
        string methodName, IMethodSymbol method)
    {
        var tree = calling.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        var token = invocation.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax ma
            ? ma.Name.Identifier : invocation.Expression.GetLastToken();
        var line = tree.GetLineSpan(token.Span);
        var path = tree.FilePath.Replace('\\', '/');
        var sourceHash = calling.Sources.Single(s => s.Path == path).ContentHash;
        var parts = new List<string>
        {
            Repo, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            calling.Descriptor.ProjectUnitId, path, sourceHash,
            (line.StartLinePosition.Line + 1).ToString(), (line.StartLinePosition.Character + 1).ToString(),
            (line.EndLinePosition.Line + 1).ToString(), (line.EndLinePosition.Character + 1).ToString(),
            TargetDoc(method), ExpectedAsmctx3(defining, method), CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static string FrozenCompctx1(VerifiedCSharpCompilation defining)
    {
        var bytes = defining.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray();
        return Frozen.CompilationProvenance.Describe(Repo, defining.Descriptor.ProjectUnitId, defining.Compilation, bytes)
            .DescriptorDigest;
    }

    private static string Metadata(VerifiedCSharpCompilation unit)
    {
        var id = unit.Compilation.Assembly.Identity;
        var token = id.PublicKeyToken.IsDefaultOrEmpty ? string.Empty : Convert.ToHexStringLower(id.PublicKeyToken.AsSpan());
        return string.Join("|", id.Name, id.Version.ToString(), id.CultureName ?? string.Empty, token);
    }

    private static string Module(VerifiedCSharpCompilation unit) => unit.Compilation.Assembly.Modules.First().Name;

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
