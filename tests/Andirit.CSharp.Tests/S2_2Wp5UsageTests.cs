using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP5: authority-gated `usage4` construction, bounded extraction, coverage and
/// the F09 disabled-text scanner. Expected `usage4` values are reconstructed
/// independently from the accepted formulas; they never read the implementation
/// under test. No persistence, no WP6 NEW-04 behavior, no WP5 `Merge` reliance.
/// </summary>
public sealed class S2_2Wp5UsageTests
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

    private const string UnitSource =
        "namespace N; public static class Site { public static int Run() { return Helper.Get(); } }\n" +
        "public static class Helper { public static int Get() => 1; }";

    // -------------------------------------------------------- valid usage4

    [Fact]
    public async Task Usage_ValidDirectInvocation_ProducesCanonicalUsage4()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var extraction = Extract(universe, unit);

        Assert.Single(extraction.Facts);
        var fact = extraction.Facts[0];
        var method = Target(unit.Compilation, "N.Helper", "Get");
        Assert.Equal(ExpectedUsage4(unit, unit, "Get", method), fact.Id.Value);
        Assert.Equal(ExpectedAsmctx3(unit, method), fact.TargetId.Value);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Assert.Equal(1, extraction.Coverage.InvocationFacts);
    }

    [Fact]
    public async Task Usage_CrossProjectInvocation_UsesDefiningCompilationTarget()
    {
        var libSource = "namespace N; public static class Lib { public static int Get() => 1; }";
        var appSource = "namespace N; public static class App { public static int Run() => Lib.Get(); }";
        var lib = await SourceUnit("Lib/Lib.csproj", "Lib", libSource);
        var app = await SourceUnit("App/App.csproj", "App", appSource, lib);
        var universe = Universe(lib, app);

        var extraction = Extract(universe, app);

        Assert.Single(extraction.Facts);
        var fact = extraction.Facts[0];
        var method = Target(lib.Compilation, "N.Lib", "Get");
        // The target is the dependency's minted asmctx3, not a structural shortcut.
        Assert.Equal(ExpectedAsmctx3(lib, method), fact.TargetId.Value);
        Assert.Equal(ExpectedUsage4(app, lib, "Get", method), fact.Id.Value);
    }

    [Fact]
    public async Task Usage_ComponentDigests_AreCallerSupplied()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var a = Extract(universe, unit, identifier: m => "id-a");
        var b = Extract(universe, unit, identifier: m => "id-b");
        Assert.Single(a.Facts);
        Assert.Single(b.Facts);
        Assert.NotEqual(a.Facts[0].Id.Value, b.Facts[0].Id.Value);
    }

    [Fact]
    public async Task Usage_DeterministicAcrossRepeatedExtraction()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var first = Extract(universe, unit);
        var second = Extract(universe, unit);
        Assert.Equal(
            first.Facts.Select(f => f.Id.Value).Order(StringComparer.Ordinal),
            second.Facts.Select(f => f.Id.Value).Order(StringComparer.Ordinal));
        Assert.Equal(first.Coverage.InvocationFacts, second.Coverage.InvocationFacts);
    }

    // ------------------------------------------------- coverage / refusal

    [Fact]
    public async Task Usage_UnsupportedCategories_AreCoverageNotFacts()
    {
        // Property access + object creation + local function are not ordinary invocations.
        const string source =
            "namespace N; public sealed class C { public int P { get; set; } }\n" +
            "public static class Site {\n" +
            "  public static int Run(C c) {\n" +
            "    int local(int x) => x + 1;\n" +
            "    var made = new C();\n" +
            "    _ = c.P;\n" +
            "    return local(1);\n" +
            "  }\n" +
            "}";
        var unit = await SourceUnit("S/S.csproj", "S", source);
        var universe = Universe(unit);
        var extraction = Extract(universe, unit);
        Assert.Empty(extraction.Facts);
        Assert.True(extraction.Coverage.Unsupported >= 1);
        Assert.True(extraction.Coverage.UnsupportedCategories >= 1);
    }

    [Fact]
    public async Task Usage_UnresolvedExternalTarget_IsCoverageNotFact()
    {
        const string source = "public static class Site { public static void Run() { System.Console.WriteLine(1); } }";
        var unit = await SourceUnit("S/S.csproj", "S", source);
        var universe = Universe(unit);
        var extraction = Extract(universe, unit);
        Assert.Empty(extraction.Facts);
        Assert.True(extraction.Coverage.Unresolved >= 1);
    }

    [Fact]
    public async Task Usage_AmbiguousDefining_IsRefused()
    {
        var libSource = "namespace N; public static class Lib { public static int Get() => 1; }";
        var appSource = "namespace N; public static class App { public static int Run() => Lib.Get(); }";
        var libA = await SourceUnit("Lib/Lib.csproj", "Lib", libSource);
        var libB = await SourceUnit("Lib2/Lib2.csproj", "Lib", libSource);
        var app = await DependsOn("App/App.csproj", "App", appSource, libA, libB);
        var universe = Universe(libA, libB, app);

        var extraction = Extract(universe, app);
        Assert.Empty(extraction.Facts);
        Assert.True(extraction.Coverage.Ambiguous >= 1);
    }

    [Fact]
    public async Task Usage_IncompleteUniverse_IsPartial_AndNoCompleteEmpty()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("S/S.csproj", "S/S.csproj", ["S/S.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [unit]);
        var extraction = Extract(universe, unit);
        Assert.Equal(CodeRelationshipCoverageState.Partial, extraction.Coverage.State);
        Assert.False(extraction.Coverage.IsComplete);
    }

    // ------------------------------------------------- F09 disabled text

    [Fact]
    public async Task Usage_F09_ConditionalInactive_IsNotAConfirmedFact()
    {
        const string source =
            "public static class Site {\n" +
            "  public static void Run() {\n" +
            "#if DEBUG\n    Helper.Get();\n#endif\n  }\n" +
            "}\n" +
            "public static class Helper { public static int Get() => 1; }";
        var unit = await SourceWithSymbols("S/S.csproj", "S", source, []);
        var universe = Universe(unit);

        var withoutDebug = Extract(universe, unit);
        Assert.Empty(withoutDebug.Facts);
        Assert.True(withoutDebug.Coverage.ConditionalInactive >= 1);

        var withDebugUnit = await SourceWithSymbols("S/S.csproj", "S", source, ["DEBUG"]);
        var withDebug = Extract(Universe(withDebugUnit), withDebugUnit);
        Assert.Single(withDebug.Facts);
        Assert.Equal(0, withDebug.Coverage.ConditionalInactive);
    }

    // ------------------------------------------------- WP4 authority consumption

    [Fact]
    public async Task Usage_MintUsage_NullRecordedContext_ProducesNoFact()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.Helper", "Get");
        var mint = universe.MintUsage(Input(unit, method, recorded: null));
        Assert.False(mint.IsMinted);
        Assert.Null(mint.Fact);
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext, mint.Status);
    }

    [Fact]
    public async Task Usage_MintUsage_StaleRecordedContext_ProducesNoFact()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.Helper", "Get");
        var mint = universe.MintUsage(Input(unit, method, recorded: "compctx2:sha256:" + new string('0', 64)));
        Assert.False(mint.IsMinted);
        Assert.Null(mint.Fact);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, mint.Status);
    }

    [Fact]
    public async Task Usage_MintUsage_ValidContext_ProducesTrustedFact()
    {
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.Helper", "Get");
        var mint = universe.MintUsage(Input(unit, method, recorded: unit.Descriptor.DescriptorDigest));
        Assert.True(mint.IsMinted);
        Assert.NotNull(mint.Fact);
        Assert.Equal(ExpectedAsmctx3(unit, method), mint.Fact!.TargetId.Value);
    }

    [Fact]
    public async Task Usage_StructuralIdentifierAloneIsNotAuthority()
    {
        // The usage input exposes no caller-supplied target identifier, and a
        // structurally valid target id cannot be injected into the mint path.
        var inputProps = typeof(VerifiedUsageInput).GetProperties().Select(p => p.Name);
        Assert.DoesNotContain("TargetId", inputProps);
        Assert.Contains("RecordedCompctx2", inputProps);

        // A structurally canonical id is constructible but is not authority.
        var structural = CodeOccurrenceIdentity.ComputeTargetId(
            "compctx1:sha256:" + new string('a', 64), "N.H|0.0.0.0||", "N.H.dll", "i", "d", "s");
        Assert.True(CodeOccurrenceIdentity.IsCanonicalTarget(structural.Value));

        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var method = Target(unit.Compilation, "N.Helper", "Get");
        // The mint derives authority from the association, not from any structural id.
        Assert.Equal(VerifiedTargetMintStatus.StaleContext,
            universe.MintUsage(Input(unit, method, recorded: "compctx2:sha256:" + new string('1', 64))).Status);
    }

    [Fact]
    public async Task Usage_Coverage_IsConstructedWithoutMerge()
    {
        // The extraction coverage carries scope/universe evidence directly and is not
        // derived from the deferred NEW-04 Merge.
        var unit = await SourceUnit("S/S.csproj", "S", UnitSource);
        var universe = Universe(unit);
        var extraction = Extract(universe, unit);
        Assert.Equal(universe.Scope.Identity, extraction.Coverage.ScopeId);
        Assert.Equal(universe.Identity, extraction.Coverage.UniverseId);
        Assert.Equal(universe.Scope.Units.Length, extraction.Coverage.DeclaredUnitCount);
        Assert.Equal(universe.Entries.Length, extraction.Coverage.VerifiedUnitCount);
    }

    // ------------------------------------------------------------------- helpers

    private static VerifiedUsageInput Input(VerifiedCSharpCompilation unit, IMethodSymbol method, string? recorded)
    {
        var span = new CodeLocation("S/S.cs", 1, 1, 1, 2);
        return new VerifiedUsageInput(Repository, Plugin, method, recorded,
            unit.Descriptor.ProjectUnitId, span, "sha256:src", "sha256:content",
            TargetDoc(method), IdDigest(method), DeclDigest(method), ShapeDigest(method),
            unit.Compilation);
    }

    private static VerifiedOccurrenceExtraction Extract(
        VerifiedCSharpUniverse universe, VerifiedCSharpCompilation unit,
        Func<IMethodSymbol, string>? identifier = null) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe, new VerifiedOccurrenceExtractionRequest(
            Repository, Plugin, unit,
            identifier ?? IdDigest, DeclDigest, ShapeDigest, TargetDoc));

    private static async Task<VerifiedCSharpCompilation> SourceUnit(
        string unit, string assembly, string text, params VerifiedCSharpCompilation[] dependencies)
        => await BuildUnit(unit, assembly, text, [], dependencies);

    private static async Task<VerifiedCSharpCompilation> SourceWithSymbols(
        string unit, string assembly, string text, string[] symbols)
        => await BuildUnit(unit, assembly, text, symbols, []);

    private static async Task<VerifiedCSharpCompilation> DependsOn(
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

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
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
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        var token = invocation.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax ma
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
