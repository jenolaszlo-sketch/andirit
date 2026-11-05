using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP5-R1: producer regressions for the WP5-Q1 F1 blocking finding. An
/// invocation that Roslyn cannot bind to a unique method must never emit a
/// trusted `usage4`: multiple equally-applicable candidates are an `Ambiguous`
/// refusal, any other unresolved binding stays `Unresolved`, and candidate
/// order can never influence trusted emission. Expected `usage4`/`asmctx3`
/// values are reconstructed from the accepted formulas; they never read the
/// extractor under test.
/// </summary>
public sealed class S2_2Wp5R1AmbiguityTests
{
    private const string RepositoryId = "fixture:wp5";
    private static readonly CodeRepositoryId Repository = new(RepositoryId);
    private static readonly CodePluginId Plugin = new("fixture.wp5.plugin");
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

    private const string AmbiguousOverloads =
        "namespace N; public sealed class M {\n" +
        "  public int Amb(int a, long b) => a + (int)b;\n" +
        "  public int Amb(long a, int b) => (int)a + b;\n" +
        "}\n" +
        "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }";

    private const string AmbiguousOverloadsSwapped =
        "namespace N; public sealed class M {\n" +
        "  public int Amb(long a, int b) => (int)a + b;\n" +
        "  public int Amb(int a, long b) => a + (int)b;\n" +
        "}\n" +
        "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }";

    private const string ResolvableOverloads =
        "namespace N; public static class H {\n" +
        "  public static int Add(int a, int b) => a + b;\n" +
        "  public static int Add(int a, int b, int c) => a + b + c;\n" +
        "}\n" +
        "public static class Site { public static int Run() { return H.Add(1, 2); } }";

    private const string UnresolvedSources =
        "namespace N; public static class H { public static int Only(int x) => x; }\n" +
        "public static class Site {\n" +
        "  public static int MissingTarget() { return Missing.Type.Invoke(); }\n" +
        "  public static int FailedResolution() { return H.Only(\"x\"); }\n" +
        "}";

    private const string MixedSource =
        "namespace N; public sealed class M {\n" +
        "  public int Amb(int a, long b) => a + (int)b;\n" +
        "  public int Amb(long a, int b) => (int)a + b;\n" +
        "}\n" +
        "public static class H { public static int Add(int a, int b) => a + b; }\n" +
        "public static class Site {\n" +
        "  public static int Good() { return H.Add(1, 2); }\n" +
        "  public static int Bad() { var m = new M(); return m.Amb(1, 1); }\n" +
        "}";

    // ------------------------------------------------------------ R1-T1

    [Fact]
    public async Task R1_T1_AmbiguousOverload_EmitsNoTrustedFact()
    {
        var unit = await SourceUnit("S/S.csproj", "S", AmbiguousOverloads);

        // Independent premise: Roslyn binds no unique symbol and reports two candidates.
        var binding = Binding(unit, "Amb");
        Assert.Null(binding.Symbol);
        Assert.Equal(2, binding.CandidateCount);

        var extraction = Extract(Universe(unit), unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(0, extraction.Coverage.InvocationFacts);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    // ------------------------------------------------------------ R1-T2

    [Fact]
    public async Task R1_T2_UnresolvedBindings_AreNeverConvertedToFacts()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnresolvedSources);

        // Zero candidates: the receiver type itself is missing.
        var missing = Binding(unit, "Invoke");
        Assert.Null(missing.Symbol);
        Assert.Equal(0, missing.CandidateCount);
        // One non-applicable candidate: overload resolution fails against a unique method.
        var failed = Binding(unit, "Only");
        Assert.Null(failed.Symbol);
        Assert.Equal(1, failed.CandidateCount);

        var extraction = Extract(Universe(unit), unit);
        Assert.Empty(extraction.Facts);
        Assert.Equal(2, extraction.Coverage.Unresolved);
        Assert.Equal(0, extraction.Coverage.Ambiguous);
    }

    // ------------------------------------------------------------ R1-T3

    [Fact]
    public async Task R1_T3_UniquelyResolvedOverload_StillMintsAuthorizedUsage()
    {
        var unit = await SourceUnit("S/S.csproj", "S", ResolvableOverloads);

        var binding = Binding(unit, "Add");
        Assert.NotNull(binding.Symbol);
        Assert.Equal(0, binding.CandidateCount);
        Assert.Equal(2, binding.Symbol!.Parameters.Length);

        var extraction = Extract(Universe(unit), unit);
        var fact = Assert.Single(extraction.Facts);
        var method = Target(unit.Compilation, "N.H", "Add", parameterCount: 2);
        Assert.Equal(TargetDoc(method), fact.TargetDocumentId);
        Assert.Equal(ExpectedAsmctx3(unit, method), fact.TargetId.Value);
        Assert.Equal(ExpectedUsage4(unit, unit, "Add", method), fact.Id.Value);
        Assert.Equal(0, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
    }

    // ------------------------------------------------------------ R1-T4

    [Fact]
    public async Task R1_T4_CrossProjectInvocation_KeepsDefiningCompilationAuthority()
    {
        var libSource = "namespace N; public static class Lib { public static int Get() => 1; }";
        var appSource = "namespace N; public static class App { public static int Run() => Lib.Get(); }";
        var lib = await SourceUnit("Lib/Lib.csproj", "Lib", libSource);
        var app = await SourceUnit("App/App.csproj", "App", appSource, lib);

        var extraction = Extract(Universe(lib, app), app);
        var fact = Assert.Single(extraction.Facts);
        var method = Target(lib.Compilation, "N.Lib", "Get", parameterCount: 0);
        Assert.Equal(ExpectedAsmctx3(lib, method), fact.TargetId.Value);
        Assert.Equal(ExpectedUsage4(app, lib, "Get", method), fact.Id.Value);
    }

    // ------------------------------------------------------------ R1-T5

    [Fact]
    public async Task R1_T5_MixedSource_RefusesOnlyTheAmbiguousInvocation()
    {
        var unit = await SourceUnit("S/S.csproj", "S", MixedSource);

        var extraction = Extract(Universe(unit), unit);
        var fact = Assert.Single(extraction.Facts);
        var method = Target(unit.Compilation, "N.H", "Add", parameterCount: 2);
        // The valid neighbor keeps its exact authorized identity.
        Assert.Equal(ExpectedUsage4(unit, unit, "Add", method), fact.Id.Value);
        Assert.Equal(1, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    // ------------------------------------------------------------ R1-T6

    [Fact]
    public async Task R1_T6_CandidateOrder_CannotAffectTrustedEmission()
    {
        var first = await SourceUnit("S/S.csproj", "S", AmbiguousOverloads);
        var second = await SourceUnit("S/S.csproj", "S", AmbiguousOverloadsSwapped);

        // The premise: the same call site yields the same two candidates in
        // swapped declaration order, so the "first" candidate differs.
        var firstBinding = Binding(first, "Amb");
        var secondBinding = Binding(second, "Amb");
        Assert.Null(firstBinding.Symbol);
        Assert.Null(secondBinding.Symbol);
        Assert.Equal(2, firstBinding.CandidateCount);
        Assert.Equal(2, secondBinding.CandidateCount);
        Assert.NotEqual(
            firstBinding.Candidates[0].ToDisplayString(),
            secondBinding.Candidates[0].ToDisplayString());

        var firstExtraction = Extract(Universe(first), first);
        var secondExtraction = Extract(Universe(second), second);
        Assert.Empty(firstExtraction.Facts);
        Assert.Empty(secondExtraction.Facts);
        Assert.Equal(1, firstExtraction.Coverage.Ambiguous);
        Assert.Equal(1, secondExtraction.Coverage.Ambiguous);
        Assert.Equal(0, firstExtraction.Coverage.Unresolved);
        Assert.Equal(0, secondExtraction.Coverage.Unresolved);
        Assert.Equal(firstExtraction.Coverage.InvocationFacts, secondExtraction.Coverage.InvocationFacts);
        Assert.Equal(firstExtraction.Coverage.Unsupported, secondExtraction.Coverage.Unsupported);
        Assert.Equal(firstExtraction.Coverage.ConditionalInactive, secondExtraction.Coverage.ConditionalInactive);
    }

    // ------------------------------------------------------------------- helpers

    private static (IMethodSymbol? Symbol, int CandidateCount, ImmutableArray<ISymbol> Candidates) Binding(
        VerifiedCSharpCompilation unit, string methodName)
    {
        var tree = unit.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        var info = unit.Compilation.GetSemanticModel(tree).GetSymbolInfo(invocation);
        return (info.Symbol as IMethodSymbol, info.CandidateSymbols.Length, info.CandidateSymbols);
    }

    private static VerifiedOccurrenceExtraction Extract(
        VerifiedCSharpUniverse universe, VerifiedCSharpCompilation unit) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe, new VerifiedOccurrenceExtractionRequest(
            Repository, Plugin, unit, IdDigest, DeclDigest, ShapeDigest, TargetDoc));

    private static async Task<VerifiedCSharpCompilation> SourceUnit(
        string unit, string assembly, string text, params VerifiedCSharpCompilation[] dependencies)
        => await BuildUnit(unit, assembly, text, [], dependencies);

    private static async Task<VerifiedCSharpCompilation> BuildUnit(
        string unit, string assembly, string text, string[] symbols,
        VerifiedCSharpCompilation[] dependencies)
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
        references.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, [tree], references, Options);
        return VerifiedCSharpCompilation.Create(RepositoryId, unit, compilation, [source], [Platform.Value],
            dependencies, CancellationToken.None);
    }

    private static VerifiedCSharpUniverse Universe(params VerifiedCSharpCompilation[] entries)
    {
        var units = entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
            e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray())).ToArray();
        return VerifiedCSharpUniverse.Create(RepositoryId, VerifiedCSharpScope.Declare(RepositoryId, units), entries);
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method, int parameterCount)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method)
            .Single(m => m is IMethodSymbol ms && ms.Parameters.Length == parameterCount));
    }

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation definingUnit, IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeTargetId(
            ExpectedCompctx1(definingUnit), Metadata(definingUnit), Module(definingUnit),
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).Value;

    private static string ExpectedUsage4(
        VerifiedCSharpCompilation callingUnit, VerifiedCSharpCompilation definingUnit,
        string methodName, IMethodSymbol method)
    {
        var tree = callingUnit.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        var token = invocation.Expression is MemberAccessExpressionSyntax ma
            ? ma.Name.Identifier
            : invocation.Expression.GetLastToken();
        var line = tree.GetLineSpan(token.Span);
        var path = tree.FilePath.Replace('\\', '/');
        var sourceHash = callingUnit.Sources.Single(s => s.Path == path).ContentHash;
        var parts = new List<string>
        {
            RepositoryId, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            callingUnit.Descriptor.ProjectUnitId, path, sourceHash,
            (line.StartLinePosition.Line + 1).ToString(),
            (line.StartLinePosition.Character + 1).ToString(),
            (line.EndLinePosition.Line + 1).ToString(),
            (line.EndLinePosition.Character + 1).ToString(),
            TargetDoc(method), ExpectedAsmctx3(definingUnit, method), CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static string ExpectedCompctx1(VerifiedCSharpCompilation unit)
    {
        var d = unit.Descriptor;
        var parts = new List<string>
        {
            "compctx1", d.RepositoryId, d.ProjectUnitId, d.AssemblyName,
            d.MetadataIdentity, d.ModuleName, d.OptionsDigest,
        };
        parts.AddRange(d.Sources.Select(s => s.CanonicalForm));
        parts.AddRange(d.References.Order(StringComparer.Ordinal));
        return "compctx1:sha256:" + LengthPrefixedHash("compctx1-context", parts);
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
