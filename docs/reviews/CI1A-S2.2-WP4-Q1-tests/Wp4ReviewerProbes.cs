using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp4Qualification;

// Independent WP4 qualification probes. Expected `asmctx3` values are derived from the
// accepted frozen R2 authority (`AdrCi1Exp.Shared.CompilationProvenance.Describe`) plus an
// independently written structural `asmctx3` tuple; they are never derived from the
// production mint (`MintTarget`) or from the production mapper.
public sealed class Wp4ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "review:wp4-q1";
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private const string LibSource = "namespace N; public static class Lib { public static int Get() => 1; }";
    private const string AppSource = "namespace N; public static class App { public static int Run() => Lib.Get(); }";
    private const string MidSource = "namespace N; public static class Mid { public static int Read() => 1; }";

    // ==================================================== formula / positive controls

    [Fact]
    public async Task Wp4_Standalone_MintEqualsIndependentR2Tuple()
    {
        var (lib, bytes) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var mint = Universe(lib).MintTarget(Target(lib.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id-a", "decl-a", "method|0");
        Assert.True(mint.IsMinted);
        Assert.Equal(ExpectedTarget(lib, bytes, "id-a", "decl-a", "method|0"), mint.TargetId!.Value);
        // Compctx1 is the frozen R2 authority value, and metadata/module match the defining compilation.
        Assert.Equal(FrozenCompctx1(lib, bytes), mint.Compctx1!.DescriptorDigest);
        Assert.Equal(ManualMetadata(lib.Compilation.Assembly.Identity), mint.Compctx1.MetadataIdentity);
        Assert.Equal(lib.Compilation.Assembly.Modules.First().Name, mint.Compctx1.ModuleName);
    }

    [Fact]
    public async Task Wp4_DependencyBearingDefining_MintEqualsIndependentR2Tuple()
    {
        // The defining compilation (Mid) is dependency-bearing; its R2-faithful compctx1
        // substitutes the project-reference item. Minting must reproduce the frozen tuple.
        var (leaf, leafBytes) = await Standalone("Leaf/Leaf.csproj", "Leaf", "namespace N; public static class Leaf { public static int V() => 1; }");
        var (mid, midBytes) = await DependsOn("Mid/Mid.csproj", "Mid", MidSource, leaf);
        var mint = Universe(leaf, mid).MintTarget(Target(mid.Compilation, "N.Mid", "Read"),
            mid.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(mint.IsMinted);
        Assert.Equal(ExpectedTarget(mid, midBytes, "id", "decl", "shape"), mint.TargetId!.Value);
        Assert.Equal(FrozenCompctx1(mid, midBytes), mint.Compctx1!.DescriptorDigest);
        Assert.DoesNotContain(mint.Compctx1.References, r => r.StartsWith("compilation-context|", StringComparison.Ordinal));
        Assert.Contains(mint.Compctx1.References, r => r.StartsWith("compilation|Leaf|", StringComparison.Ordinal));
        _ = leafBytes;
    }

    [Fact]
    public async Task Wp4_MultiDependencyDefining_MintEqualsIndependentR2Tuple()
    {
        var (one, _) = await Standalone("L1/L1.csproj", "L1", "namespace A; public static class L1 { public static int V() => 1; }");
        var (two, _) = await Standalone("L2/L2.csproj", "L2", "namespace B; public static class L2 { public static int V() => 2; }");
        var (mid, midBytes) = await DependsOn("Mid/Mid.csproj", "Mid", MidSource, one, two);
        var mint = Universe(one, two, mid).MintTarget(Target(mid.Compilation, "N.Mid", "Read"),
            mid.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(mint.IsMinted);
        Assert.Equal(ExpectedTarget(mid, midBytes, "id", "decl", "shape"), mint.TargetId!.Value);
        Assert.Contains(mint.Compctx1!.References, r => r.StartsWith("compilation|L1|", StringComparison.Ordinal));
        Assert.Contains(mint.Compctx1.References, r => r.StartsWith("compilation|L2|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Wp4_CrossProject_MintEqualsIndependentR2Tuple()
    {
        var (lib, libBytes) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var (app, _) = await DependsOn("App/App.csproj", "App", AppSource, lib);
        var mint = Universe(lib, app).MintTarget(Target(app.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(mint.IsMinted);
        Assert.Equal(ExpectedTarget(lib, libBytes, "id", "decl", "shape"), mint.TargetId!.Value);
    }

    [Fact]
    public async Task Wp4_PeIdentitiesPreserved_Compctx2Isolated_NoNormalization()
    {
        var (lib, bytes) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var mint = Universe(lib).MintTarget(Target(lib.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.Contains(Platform.Value.Identity, mint.Compctx1!.References);
        Assert.DoesNotContain("compctx2", mint.TargetId!.Value);
        Assert.DoesNotContain("compctx2", mint.Compctx1.ComputeDigest());
        Assert.StartsWith("asmctx3:sha256:", mint.TargetId.Value, StringComparison.Ordinal);
        // Different reference inventory must not collapse to the same context.
        var (two, _) = await WithExtraReference("Lib/Lib.csproj", "Lib", LibSource);
        var other = Universe(two).MintTarget(Target(two.Compilation, "N.Lib", "Get"),
            two.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.NotEqual(mint.TargetId.Value, other.TargetId!.Value);
        _ = bytes;
    }

    // ============================================= mandatory recorded context (G3)

    [Fact]
    public async Task Wp4_RecordedContext_Matrix()
    {
        var (lib, app) = await LibAndApp();
        var universe = Universe(lib, app);
        var target = Target(app.Compilation, "N.Lib", "Get");

        // 1 null, 2 empty, 3 whitespace/malformed -> MissingRecordedContext, no artifact.
        foreach (var recorded in new string?[] { null, "", "   " })
            AssertNoArtifact(universe.MintTarget(target, recorded, "id", "decl", "shape"),
                VerifiedTargetMintStatus.MissingRecordedContext);
        AssertNoArtifact(universe.MintTarget(target, "not-a-context", "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);

        // 4 unrelated / 5 fabricated / 6 stale -> StaleContext, no artifact.
        var (unrelated, _) = await Standalone("U/U.csproj", "U", "namespace U; public static class U { public static int M() => 1; }");
        AssertNoArtifact(universe.MintTarget(target, unrelated.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);
        AssertNoArtifact(universe.MintTarget(target, "compctx2:sha256:" + new string('a', 64), "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);
        AssertNoArtifact(universe.MintTarget(target, "compctx2:sha256:" + new string('1', 64), "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);

        // 8 incorrect context with otherwise valid live compilation -> StaleContext.
        AssertNoArtifact(universe.MintTarget(target, app.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);

        // 7 correct context paired with a substituted live compilation -> MissingDefiningCompilation.
        var (substitute, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        AssertNoArtifact(universe.MintTarget(Target(substitute.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.MissingDefiningCompilation);

        // 9 correct context + correct defining compilation -> Minted.
        Assert.True(universe.MintTarget(target, lib.Descriptor.DescriptorDigest, "id", "decl", "shape").IsMinted);
    }

    [Fact]
    public async Task Wp4_Case10_ResolveDefiningNullIsComplete_ButMintRefuses()
    {
        var (lib, app) = await LibAndApp();
        var universe = Universe(lib, app);
        var target = Target(app.Compilation, "N.Lib", "Get");

        Assert.Equal(VerifiedDefiningStatus.Complete, universe.ResolveDefining(target, null, null).Status);
        var mint = universe.MintTarget(target, null, "id", "decl", "shape");
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext, mint.Status);
        AssertNoArtifact(mint, VerifiedTargetMintStatus.MissingRecordedContext);
    }

    [Fact]
    public async Task Wp4_MalformedComponentDigest_RefusesWithoutArtifact()
    {
        var (lib, app) = await LibAndApp();
        var universe = Universe(lib, app);
        var target = Target(app.Compilation, "N.Lib", "Get");
        // The frozen constructor throws for a non-canonical component; no artifact is produced.
        Assert.Throws<ArgumentException>(() => universe.MintTarget(target, lib.Descriptor.DescriptorDigest, "", "decl", "shape"));
    }

    // ============================================================ G1 identity boundaries

    [Fact]
    public async Task Wp4_G1_IdentityBoundaries()
    {
        var (lib, app) = await LibAndApp();
        var universe = Universe(lib, app);

        // Valid direct and valid cross-project binding.
        Assert.True(universe.MintTarget(Target(lib.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape").IsMinted);
        Assert.True(universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape").IsMinted);

        // Foreign metadata-equal instance: accepted bounded binding; artifact uses the member context.
        var (foreign, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var foreignResult = universe.MintTarget(Target(foreign.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id", "decl", "shape", caller: app.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.Minted, foreignResult.Status);
        Assert.Equal(ExpectedTarget(lib, [("Lib/Lib.cs", await SourceBytes(lib))], "id", "decl", "shape"),
            foreignResult.TargetId!.Value);

        // Substituted same-digest instance without a live caller reference -> Missing.
        AssertNoArtifact(universe.MintTarget(Target(foreign.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.MissingDefiningCompilation);

        // Identity-unequal target -> Missing.
        var (other, _) = await Standalone("O/O.csproj", "O", "namespace O; public static class O { public static int M() => 1; }");
        AssertNoArtifact(universe.MintTarget(Target(other.Compilation, "O.O", "M"), null, "id", "decl", "shape"),
            VerifiedTargetMintStatus.MissingRecordedContext);
        AssertNoArtifact(universe.MintTarget(Target(other.Compilation, "O.O", "M"), other.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.MissingDefiningCompilation);

        // Valid metadata identity with an invalid recorded context -> Stale.
        AssertNoArtifact(universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"), app.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.StaleContext);
    }

    [Fact]
    public async Task Wp4_G1_AmbiguousDefining_Refuses()
    {
        var (a, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var (b, _) = await Standalone("Lib2/Lib2.csproj", "Lib", LibSource);
        var (app, _) = await DependsOn("App/App.csproj", "App", AppSource, a, b);
        var universe = Universe(a, b, app);
        AssertNoArtifact(universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"),
            a.Descriptor.DescriptorDigest, "id", "decl", "shape", caller: app.Compilation),
            VerifiedTargetMintStatus.Ambiguous);
    }

    // ============================================================ G2 fail-closed

    [Fact]
    public async Task Wp4_G2_OverlappingFailures_NeverMint()
    {
        var (lib, app) = await LibAndApp();
        var universe = Universe(lib, app);
        var stale = "compctx2:sha256:" + new string('0', 64);

        var incomplete = await Incomplete();
        var cases = new (string Name, VerifiedTargetMintStatus Status)[]
        {
            ("incomplete+null", incomplete.Universe.MintTarget(incomplete.Target, null, "id", "decl", "shape").Status),
            ("incomplete+stale", incomplete.Universe.MintTarget(incomplete.Target, stale, "id", "decl", "shape").Status),
            ("complete+stale", universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"), stale, "id", "decl", "shape").Status),
            ("complete+missing", universe.MintTarget(Target(
                Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options), "U.U", "M"),
                lib.Descriptor.DescriptorDigest, "id", "decl", "shape").Status),
        };
        foreach (var (name, status) in cases)
        {
            output.WriteLine($"WP4Q1 {name} => {status}");
            Assert.NotEqual(VerifiedTargetMintStatus.Minted, status);
        }

        // Mixed: correct context + substituted live compilation (missing) still refuses.
        var (foreign, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        AssertNoArtifact(universe.MintTarget(Target(foreign.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape"),
            VerifiedTargetMintStatus.MissingDefiningCompilation);
    }

    // ============================================================ Option A §5.2 forms

    [Fact]
    public async Task Wp4_OptionAS52_RuntimeForms()
    {
        // Form 1 (conditional defining API change -> target changes).
        const string text = "namespace N; public static class Lib {\n#if FEATURE\n public static int Get() => 1;\n#else\n public static int Get() => 1;\n#endif\n}";
        var (off, _) = await Standalone("Lib/Lib.csproj", "Lib", text, []);
        var (on, _) = await Standalone("Lib/Lib.csproj", "Lib", text, ["FEATURE"]);
        var offMint = Universe(off).MintTarget(Target(off.Compilation, "N.Lib", "Get"), off.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var onMint = Universe(on).MintTarget(Target(on.Compilation, "N.Lib", "Get"), on.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.True(offMint.IsMinted && onMint.IsMinted);
        Assert.NotEqual(offMint.TargetId!.Value, onMint.TargetId!.Value);

        // Form 2 (transitive-only change -> target stable while defining compctx2 changes).
        var (leaf1, _) = await Standalone("Leaf/Leaf.csproj", "Leaf", "namespace N; public static class Leaf { public static int V() => 1; }");
        var (leaf2, _) = await Standalone("Leaf/Leaf.csproj", "Leaf", "namespace N; public static class Leaf { public static int V() => 1; }", [],
            Options.WithNullableContextOptions(NullableContextOptions.Enable));
        var (mid1, _) = await DependsOn("Mid/Mid.csproj", "Mid", MidSource, leaf1);
        var (mid2, _) = await DependsOn("Mid/Mid.csproj", "Mid", MidSource, leaf2);
        var m1 = Universe(leaf1, mid1).MintTarget(Target(mid1.Compilation, "N.Mid", "Read"), mid1.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var m2 = Universe(leaf2, mid2).MintTarget(Target(mid2.Compilation, "N.Mid", "Read"), mid2.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.NotEqual(mid1.Descriptor.DescriptorDigest, mid2.Descriptor.DescriptorDigest);
        Assert.Equal(m1.TargetId!.Value, m2.TargetId!.Value);

        // Form 4 (restore -> original identity) is exercised by the deterministic separate-process probe.
        // Form 5 (distinct defining compilations -> distinct targets).
        var (c, _) = await Standalone("C/C.csproj", "C", "namespace N; public static class Lib { public static int Get() => 1; }");
        var cMint = Universe(c).MintTarget(Target(c.Compilation, "N.Lib", "Get"), c.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var (solo, _) = await Standalone("S/S.csproj", "Lib", LibSource);
        var sMint = Universe(solo).MintTarget(Target(solo.Compilation, "N.Lib", "Get"), solo.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.NotEqual(cMint.TargetId!.Value, sMint.TargetId!.Value);

        // Form 6 (equivalent inputs -> equal identity) with the same unit as `solo`.
        var (eq, _) = await Standalone("S/S.csproj", "Lib", LibSource);
        var eqMint = Universe(eq).MintTarget(Target(eq.Compilation, "N.Lib", "Get"), eq.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.Equal(sMint.TargetId!.Value, eqMint.TargetId!.Value);

        // Form 10 (compctx2 cannot enter asmctx3 / no unverified mint).
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx2:sha256:" + new string('0', 64), "meta", "module", "id", "decl", "shape"));
    }

    // ============================================================ determinism

    [Fact]
    public async Task Wp4_SeparateProcessDigest()
    {
        var (lib, libBytes) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var (app, _) = await DependsOn("App/App.csproj", "App", AppSource, lib);
        var universe = Universe(lib, app);
        var mint = universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.True(mint.IsMinted);
        output.WriteLine("WP4Q1-ASMCTX3=" + mint.TargetId!.Value);
        output.WriteLine("WP4Q1-COMPCTX1=" + mint.Compctx1!.DescriptorDigest);
        output.WriteLine("WP4Q1-EXPECTED=" + ExpectedTarget(lib, libBytes, "id", "decl", "shape"));
    }

    // ------------------------------------------------------------------- helpers

    private static void AssertNoArtifact(VerifiedTargetMint mint, VerifiedTargetMintStatus expected)
    {
        Assert.Equal(expected, mint.Status);
        Assert.False(mint.IsMinted);
        Assert.Null(mint.TargetId);
        Assert.Null(mint.Compctx1);
    }

    private sealed record IncompleteFixture(VerifiedCSharpUniverse Universe, IMethodSymbol Target);

    private async Task<IncompleteFixture> Incomplete()
    {
        var (lib, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        return new IncompleteFixture(VerifiedCSharpUniverse.Create(Repo, scope, [lib]), Target(lib.Compilation, "N.Lib", "Get"));
    }

    private async Task<(VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App)> LibAndApp()
    {
        var (lib, _) = await Standalone("Lib/Lib.csproj", "Lib", LibSource);
        var (app, _) = await DependsOn("App/App.csproj", "App", AppSource, lib);
        return (lib, app);
    }

    private static async Task<byte[]> SourceBytes(VerifiedCSharpCompilation compilation) =>
        await Task.FromResult(Encoding.UTF8.GetBytes(
            compilation.Compilation.SyntaxTrees.Single().GetText().ToString()));

    private async Task<(VerifiedCSharpCompilation Compilation, (string Path, byte[] Bytes)[] Bytes)> Standalone(
        string unit, string assembly, string text, string[]? symbols = null, CSharpCompilationOptions? options = null)
    {
        var path = unit[..^".csproj".Length] + ".cs";
        var source = await ReadSource(path, text, symbols);
        var compilation = Compile(assembly, [Tree(source, symbols)], [Platform.Value.Reference], options ?? Options);
        var verified = Create(unit, compilation, [source], [Platform.Value], []);
        return (verified, [(path, Bytes(text))]);
    }

    private async Task<(VerifiedCSharpCompilation Compilation, (string Path, byte[] Bytes)[] Bytes)> WithExtraReference(
        string unit, string assembly, string text)
    {
        var path = unit[..^".csproj".Length] + ".cs";
        var source = await ReadSource(path, text, null);
        var extra = VerifiedMetadataReference.Capture(typeof(VerifiedCSharpCompilation).Assembly.Location, CancellationToken.None);
        var compilation = Compile(assembly, [Tree(source, null)], [Platform.Value.Reference, extra.Reference], Options);
        var verified = Create(unit, compilation, [source], [Platform.Value, extra], []);
        return (verified, [(path, Bytes(text))]);
    }

    private async Task<(VerifiedCSharpCompilation Compilation, (string Path, byte[] Bytes)[] Bytes)> DependsOn(
        string unit, string assembly, string text, params VerifiedCSharpCompilation[] dependencies)
    {
        var path = unit[..^".csproj".Length] + ".cs";
        var source = await ReadSource(path, text, null);
        var refs = new List<MetadataReference> { Platform.Value.Reference };
        refs.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = Compile(assembly, [Tree(source, null)], refs, Options);
        var verified = Create(unit, compilation, [source], [Platform.Value], dependencies);
        return (verified, [(path, Bytes(text))]);
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

    // Independent expected `asmctx3`: frozen-authority compctx1 + an independently
    // written structural tuple (manual metadata/module), never the production mint.
    private static string ExpectedTarget(VerifiedCSharpCompilation defining, (string Path, byte[] Bytes)[] bytes,
        string identifierDigest, string declarationDigest, string shapeDigest)
    {
        var compctx1 = FrozenCompctx1(defining, bytes);
        var metadata = ManualMetadata(defining.Compilation.Assembly.Identity);
        var module = defining.Compilation.Assembly.Modules.First().Name;
        return "asmctx3:sha256:" + LengthPrefixedHash("asmctx3-context",
            ["compctx1", compctx1, metadata, module, identifierDigest, declarationDigest, shapeDigest]);
    }

    private static string FrozenCompctx1(VerifiedCSharpCompilation defining, (string Path, byte[] Bytes)[] bytes) =>
        Frozen.CompilationProvenance.Describe(Repo, defining.Descriptor.ProjectUnitId, defining.Compilation, bytes)
            .DescriptorDigest;

    private static string ManualMetadata(AssemblyIdentity identity)
    {
        var token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return string.Join("|", identity.Name, identity.Version.ToString(), identity.CultureName ?? string.Empty, token);
    }

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

    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    private static async Task<VerifiedCSharpSource> ReadSource(string path, string text, string[]? symbols) =>
        await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Hex(Bytes(text)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Bytes(text), writable: false))),
            CancellationToken.None);

    private static SyntaxTree Tree(VerifiedCSharpSource source, string[]? symbols) =>
        CSharpSyntaxTree.ParseText(source.Text,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular,
                symbols ?? Array.Empty<string>()),
            source.Path);

    private static SyntaxTree Tree(string path, string text) =>
        CSharpSyntaxTree.ParseText(text,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular), path);

    private static CSharpCompilation Compile(string assembly, IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references, CSharpCompilationOptions options) =>
        CSharpCompilation.Create(assembly, trees, references, options);

    private static VerifiedCSharpCompilation Create(string unit, CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources, IReadOnlyCollection<VerifiedMetadataReference> metadata,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(Repo, unit, compilation, sources, metadata, dependencies, CancellationToken.None);

    private static string Hex(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
