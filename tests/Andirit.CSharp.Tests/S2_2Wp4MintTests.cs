using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP4: authority-gated `asmctx3` target minting. Exercises the production
/// minting path (<see cref="VerifiedCSharpUniverse.MintTarget"/>) against accepted
/// authority (Option A §5.1(1); mapping contract §3; G-MINT owner §7) and the frozen
/// R2 `asmctx3` tuple. Expected identities are reconstructed independently from the
/// accepted formula; they never read the implementation under test. No `usage4`,
/// extraction, minting of facts or persistence.
/// </summary>
public sealed class S2_2Wp4MintTests
{
    private const string RepositoryId = "fixture:wp4";
    private static readonly CSharpCompilationOptions LibraryOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private const string LibSource = "namespace N; public static class Lib { public static int Get() => 1; }";
    private const string AppSource = "namespace N; public static class App { public static int Run() => Lib.Get(); }";

    // ------------------------------------------------------------- valid minting

    [Fact]
    public async Task Mint_ValidDirectAssociation_ProducesCanonicalAsmctx3()
    {
        var lib = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var universe = Universe(lib);
        var mint = universe.MintTarget(Target(lib.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id-1", "decl-1", "method|0");

        Assert.Equal(VerifiedTargetMintStatus.Minted, mint.Status);
        Assert.True(mint.IsMinted);
        Assert.StartsWith("asmctx3:sha256:", mint.TargetId!.Value, StringComparison.Ordinal);
        Assert.DoesNotContain("compctx2", mint.TargetId.Value);
        Assert.Equal(
            ExpectedTargetId(ExpectedCompctx1(lib, static _ => null),
                lib.Descriptor.MetadataIdentity, lib.Descriptor.ModuleName, "id-1", "decl-1", "method|0"),
            mint.TargetId.Value);
    }

    [Fact]
    public async Task Mint_ValidCrossProjectAssociation_UsesDefiningCompilationContext()
    {
        var (universe, target, lib) = await TwoProject();
        var mint = universe.MintTarget(target, lib.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(mint.IsMinted);
        Assert.Equal(
            ExpectedTargetId(ExpectedCompctx1(lib, static _ => null),
                lib.Descriptor.MetadataIdentity, lib.Descriptor.ModuleName, "id", "decl", "shape"),
            mint.TargetId!.Value);
    }

    [Fact]
    public async Task Mint_ForeignMetadataEquivalentTarget_MintsDefiningMemberContext()
    {
        // Accepted F1/G1: a foreign, identity-equal target instance binds the universe
        // member the caller live-references. Minting must build the artifact from the
        // defining member's mapped compctx1, not claim exact identity of the foreign
        // instance (which is neither required nor established by accepted authority).
        var (libA, libB, app) = await IdentityEqualInstances();
        var universe = Universe(libA, app);
        var foreign = Target(libB.Compilation, "N.Lib", "Get");

        var mint = universe.MintTarget(foreign, libA.Descriptor.DescriptorDigest, "id", "decl", "shape",
            caller: app.Compilation);

        Assert.Equal(VerifiedTargetMintStatus.Minted, mint.Status);
        Assert.Equal(
            ExpectedTargetId(ExpectedCompctx1(libA, static _ => null),
                libA.Descriptor.MetadataIdentity, libA.Descriptor.ModuleName, "id", "decl", "shape"),
            mint.TargetId!.Value);
    }

    [Fact]
    public async Task Mint_DeterministicAcrossRepeatedAndEquivalentInstances()
    {
        var (universeA, targetA, libA) = await TwoProject();
        var (universeB, targetB, libB) = await TwoProject();

        var a1 = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var a2 = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var b1 = universeB.MintTarget(targetB, libB.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.Equal(a1.TargetId!.Value, a2.TargetId!.Value);
        Assert.Equal(a1.TargetId!.Value, b1.TargetId!.Value);
    }

    // ---------------------------------------------- mandatory recorded compctx2 (G3)

    [Fact]
    public async Task Mint_NullOrEmptyRecordedContext_RefusesWithoutArtifact()
    {
        var (universe, target, _) = await TwoProject();
        foreach (var recorded in new string?[] { null, "", "   " })
        {
            var mint = universe.MintTarget(target, recorded, "id", "decl", "shape");
            Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext, mint.Status);
            Assert.False(mint.IsMinted);
            Assert.Null(mint.TargetId);
            Assert.Null(mint.Compctx1);
        }
    }

    [Fact]
    public async Task Mint_NullRecordedContext_ResolveDefiningIsComplete_ButMintRefuses()
    {
        // Demonstrates the enforcement is load-bearing: the accepted WP3 resolver
        // returns Complete without a recording, while the WP4 mint path refuses it.
        var (universe, target, _) = await TwoProject();
        Assert.Equal(VerifiedDefiningStatus.Complete, universe.ResolveDefining(target, null, null).Status);
        Assert.False(universe.MintTarget(target, null, "id", "decl", "shape").IsMinted);
    }

    [Fact]
    public async Task Mint_StaleOrFabricatedRecordedContext_Refuses()
    {
        var (universe, target, lib, app) = await TwoProjectExpanded();
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, universe.MintTarget(
            target, "compctx2:sha256:" + new string('0', 64), "id", "decl", "shape").Status);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, universe.MintTarget(
            target, "compctx2:sha256:" + new string('a', 64), "id", "decl", "shape").Status);
        // Wrong defining compilation: the caller's own context is not the defining context.
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, universe.MintTarget(
            target, app.Descriptor.DescriptorDigest, "id", "decl", "shape").Status);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, universe.MintTarget(
            target, "not-a-context", "id", "decl", "shape").Status);
        Assert.True(universe.MintTarget(target, lib.Descriptor.DescriptorDigest, "id", "decl", "shape").IsMinted);
    }

    [Fact]
    public async Task Mint_PreviouslyValidButStaleContext_Refuses()
    {
        var before = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var staleDigest = before.Descriptor.DescriptorDigest;
        var after = await StandaloneLib("Lib/Lib.csproj", "Lib",
            "namespace N; public static class Lib { public static int Get() => 2; }");
        var universe = Universe(after);

        Assert.NotEqual(staleDigest, after.Descriptor.DescriptorDigest);
        var mint = universe.MintTarget(Target(after.Compilation, "N.Lib", "Get"), staleDigest, "id", "decl", "shape");
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, mint.Status);
        Assert.Null(mint.TargetId);
    }

    // -------------------------------------------------- association refusals (G1/G2)

    [Fact]
    public async Task Mint_SubstitutedLiveInstanceWithSameDigest_Refuses()
    {
        var before = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var substitute = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        Assert.Equal(before.Descriptor.DescriptorDigest, substitute.Descriptor.DescriptorDigest);
        var universe = Universe(before);

        var mint = universe.MintTarget(Target(substitute.Compilation, "N.Lib", "Get"),
            before.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.Equal(VerifiedTargetMintStatus.MissingDefiningCompilation, mint.Status);
        Assert.Null(mint.TargetId);
    }

    [Fact]
    public async Task Mint_IncompleteUniverse_Refuses()
    {
        var (lib, _) = await TwoProjectParts();
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib]);
        var mint = universe.MintTarget(Target(lib.Compilation, "N.Lib", "Get"),
            lib.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.Equal(VerifiedTargetMintStatus.Incomplete, mint.Status);
    }

    [Fact]
    public async Task Mint_AmbiguousDefiningCompilation_Refuses()
    {
        var libSource = await ReadSourceAsync("Lib/Lib.cs", LibSource);
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], LibraryOptions), [libSource], [Platform.Value], []);
        var libB = Create("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], LibraryOptions), [libSource], [Platform.Value], []);
        var appSource = await ReadSourceAsync("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)],
                [Platform.Value.Reference, libA.Compilation.ToMetadataReference(), libB.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [Platform.Value], [libA, libB]);
        var universe = Universe(libA, libB, app);

        var mint = universe.MintTarget(Target(app.Compilation, "N.Lib", "Get"),
            libA.Descriptor.DescriptorDigest, "id", "decl", "shape", caller: app.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.Ambiguous, mint.Status);
        Assert.Null(mint.TargetId);
    }

    [Fact]
    public async Task Mint_MissingDefiningCompilation_Refuses()
    {
        var (universe, _, _) = await TwoProject();
        var unrelated = Compile("U",
            [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")],
            Array.Empty<MetadataReference>(), LibraryOptions);
        var mint = universe.MintTarget(Target(unrelated, "U.U", "M"),
            "compctx2:sha256:" + new string('0', 64), "id", "decl", "shape");
        Assert.Equal(VerifiedTargetMintStatus.MissingDefiningCompilation, mint.Status);
    }

    [Fact]
    public async Task Mint_ExternalUnitRefusedAtConstruction()
    {
        var lib = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Other/Other.csproj", "Other/Other.csproj", ["Other/Other.cs"])]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib]));
    }

    [Fact]
    public async Task Mint_OverlappingFailures_NeverMint()
    {
        var (universe, target, lib, app) = await TwoProjectExpanded();
        var stale = "compctx2:sha256:" + new string('0', 64);

        // Ambiguous would require a second binding; use incomplete + others here.
        var incomplete = await IncompleteUniverse();
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext,
            incomplete.Universe.MintTarget(incomplete.Target, null, "id", "decl", "shape").Status);
        Assert.Equal(VerifiedTargetMintStatus.Incomplete,
            incomplete.Universe.MintTarget(incomplete.Target, stale, "id", "decl", "shape").Status);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext,
            universe.MintTarget(target, stale, "id", "decl", "shape").Status);
        Assert.Equal(VerifiedTargetMintStatus.MissingDefiningCompilation,
            universe.MintTarget(Target(Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], Array.Empty<MetadataReference>(), LibraryOptions), "U.U", "M"),
                lib.Descriptor.DescriptorDigest, "id", "decl", "shape").Status);
        // Only the fully valid case mints.
        Assert.True(universe.MintTarget(target, lib.Descriptor.DescriptorDigest, "id", "decl", "shape").IsMinted);
    }

    // ------------------------------------------------ Option A §5.2 runtime forms

    [Fact]
    public async Task Mint_ConditionalDefiningApiChange_ChangesAsmctx3()
    {
        // Option A §5.2 tests 1/3: a defining-project conditional API change alters the
        // defining compilation's R2 material, so `asmctx3` changes (WP4 form).
        const string text = "namespace N; public static class Lib {\n#if FEATURE\n public static int Get() => 1;\n#else\n public static int Get() => 1;\n#endif\n}";
        var off = await StandaloneLib("Lib/Lib.csproj", "Lib", text, []);
        var on = await StandaloneLib("Lib/Lib.csproj", "Lib", text, ["FEATURE"]);

        var mOff = Universe(off).MintTarget(Target(off.Compilation, "N.Lib", "Get"),
            off.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var mOn = Universe(on).MintTarget(Target(on.Compilation, "N.Lib", "Get"),
            on.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(mOff.IsMinted && mOn.IsMinted);
        Assert.NotEqual(off.Descriptor.DescriptorDigest, on.Descriptor.DescriptorDigest);
        Assert.NotEqual(mOff.TargetId!.Value, mOn.TargetId!.Value);
    }

    [Fact]
    public async Task Mint_TransitiveOnlyChange_Asmctx3StableWhileCompctx2Changes()
    {
        // Option A §5.2 test 2 (WP4 form): a dependency-context-only change leaves the
        // defining compilation's R2-visible material unchanged, so `asmctx3` is stable
        // while its verified `compctx2` changes.
        var leafSource = await ReadSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 1; }");
        var leaf1 = Create("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leafSource)], [Platform.Value.Reference], LibraryOptions), [leafSource], [Platform.Value], []);
        var leaf2 = Create("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leafSource)], [Platform.Value.Reference],
                LibraryOptions.WithNullableContextOptions(NullableContextOptions.Enable)), [leafSource], [Platform.Value], []);
        var midSource = await ReadSourceAsync("Mid/Mid.cs", "namespace N; public static class Mid { public static int Read() => Leaf.Value(); }");
        var mid1 = Create("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [Platform.Value.Reference, leaf1.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [Platform.Value], [leaf1]);
        var mid2 = Create("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [Platform.Value.Reference, leaf2.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [Platform.Value], [leaf2]);

        var m1 = Universe(leaf1, mid1).MintTarget(Target(mid1.Compilation, "N.Mid", "Read"),
            mid1.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var m2 = Universe(leaf2, mid2).MintTarget(Target(mid2.Compilation, "N.Mid", "Read"),
            mid2.Descriptor.DescriptorDigest, "id", "decl", "shape");

        Assert.True(m1.IsMinted && m2.IsMinted);
        Assert.NotEqual(leaf1.Descriptor.DescriptorDigest, leaf2.Descriptor.DescriptorDigest);
        Assert.NotEqual(mid1.Descriptor.DescriptorDigest, mid2.Descriptor.DescriptorDigest);
        Assert.Equal(m1.TargetId!.Value, m2.TargetId!.Value);
        Assert.Equal(
            ExpectedTargetId(ExpectedCompctx1(mid1,
                    r => r.StartsWith("compilation-context|", StringComparison.Ordinal)
                        ? ExpectedLegacyProjectReference(leaf1) : null),
                mid1.Descriptor.MetadataIdentity, mid1.Descriptor.ModuleName, "id", "decl", "shape"),
            m1.TargetId.Value);
    }

    [Fact]
    public async Task Mint_RestoreInputs_ReproducesOriginalAsmctx3()
    {
        // Option A §5.2 test 4: restoring the original inputs reproduces the identity.
        var first = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var changed = await StandaloneLib("Lib/Lib.csproj", "Lib", "namespace N; public static class Lib { public static int Get() => 9; }");
        var restored = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);

        string Mint(VerifiedCSharpCompilation lib) => Universe(lib)
            .MintTarget(Target(lib.Compilation, "N.Lib", "Get"), lib.Descriptor.DescriptorDigest, "id", "decl", "shape")
            .TargetId!.Value;

        var original = Mint(first);
        Assert.NotEqual(original, Mint(changed));
        Assert.Equal(original, Mint(restored));
    }

    [Fact]
    public async Task Mint_DistinctDefiningCompilations_DistinctAsmctx3()
    {
        // Option A §5.2 test 5: distinct defining compilations yield distinct targets.
        var one = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var two = await StandaloneLib("Lib/Lib.csproj", "Lib", "namespace N; public static class Lib { public static int Get() => 1; public static int Extra() => 2; }");
        var a = Universe(one).MintTarget(Target(one.Compilation, "N.Lib", "Get"), one.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var b = Universe(two).MintTarget(Target(two.Compilation, "N.Lib", "Get"), two.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.NotEqual(a.TargetId!.Value, b.TargetId!.Value);
    }

    [Fact]
    public async Task Mint_EquivalentDefiningCompilations_EqualAsmctx3()
    {
        // Option A §5.2 test 6 (same-inputs profile): equivalent authoritative inputs
        // yield an equal target identity. Cross-reference-profile equality is a
        // separate G-MINT comparison item and is not claimed here.
        var one = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var two = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        Assert.Equal(one.Descriptor.DescriptorDigest, two.Descriptor.DescriptorDigest);
        var a = Universe(one).MintTarget(Target(one.Compilation, "N.Lib", "Get"), one.Descriptor.DescriptorDigest, "id", "decl", "shape");
        var b = Universe(two).MintTarget(Target(two.Compilation, "N.Lib", "Get"), two.Descriptor.DescriptorDigest, "id", "decl", "shape");
        Assert.Equal(a.TargetId!.Value, b.TargetId!.Value);
    }

    [Fact]
    public void Mint_Compctx2CannotEnterAsmctx3()
    {
        // Mapping contract §6 / Option A §5.2 test 10: a `compctx2` context is
        // structurally refused by the frozen `asmctx3` constructor.
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx2:sha256:" + new string('0', 64), "meta", "module", "id", "decl", "shape"));
        Assert.Throws<ArgumentException>(() => CodeOccurrenceIdentity.ComputeTargetId(
            "compctx1:sha256:short", "meta", "module", "id", "decl", "shape"));
    }

    // ------------------------------------------------------------------- helpers

    private sealed record Incomplete(VerifiedCSharpUniverse Universe, IMethodSymbol Target);

    private async Task<Incomplete> IncompleteUniverse()
    {
        var (lib, _) = await TwoProjectParts();
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib]);
        return new Incomplete(universe, Target(lib.Compilation, "N.Lib", "Get"));
    }

    private async Task<(VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App)> TwoProjectParts()
    {
        var lib = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var appSource = await ReadSourceAsync("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [Platform.Value], [lib]);
        return (lib, app);
    }

    private async Task<(VerifiedCSharpUniverse Universe, IMethodSymbol Target, VerifiedCSharpCompilation Lib)> TwoProject()
    {
        var (lib, app) = await TwoProjectParts();
        return (Universe(lib, app), Target(app.Compilation, "N.Lib", "Get"), lib);
    }

    private async Task<(VerifiedCSharpUniverse Universe, IMethodSymbol Target, VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App)>
        TwoProjectExpanded()
    {
        var (lib, app) = await TwoProjectParts();
        return (Universe(lib, app), Target(app.Compilation, "N.Lib", "Get"), lib, app);
    }

    private async Task<(VerifiedCSharpCompilation LibA, VerifiedCSharpCompilation LibB, VerifiedCSharpCompilation App)>
        IdentityEqualInstances()
    {
        var libA = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var libB = await StandaloneLib("Lib/Lib.csproj", "Lib", LibSource);
        var appSource = await ReadSourceAsync("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, libA.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [Platform.Value], [libA]);
        return (libA, libB, app);
    }

    private static async Task<VerifiedCSharpCompilation> StandaloneLib(
        string unit, string assembly, string text, string[]? symbols = null)
    {
        var source = await ReadSourceAsync(Paths(unit), text);
        return Create(unit, Compile(assembly, [Tree(source, symbols)], [Platform.Value.Reference], LibraryOptions),
            [source], [Platform.Value], []);
    }

    private static VerifiedCSharpUniverse Universe(params VerifiedCSharpCompilation[] entries)
    {
        var units = entries.Select(e => new CSharpScopeUnit(e.Descriptor.ProjectUnitId, e.Descriptor.ProjectUnitId,
            e.Descriptor.Sources.Select(s => s.Path).ToImmutableArray())).ToArray();
        return VerifiedCSharpUniverse.Create(RepositoryId, VerifiedCSharpScope.Declare(RepositoryId, units), entries);
    }

    private static string Paths(string unit) =>
        unit.EndsWith(".csproj", StringComparison.Ordinal) ? unit[..^".csproj".Length] + ".cs" : unit + ".cs";

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    // Independent reconstruction of the frozen R2 `asmctx3` tuple.
    private static string ExpectedTargetId(string compctx1, string metadataIdentity, string moduleName,
        string identifierDigest, string declarationDigest, string shapeDigest) =>
        "asmctx3:sha256:" + LengthPrefixedHash("asmctx3-context",
            ["compctx1", compctx1, metadataIdentity, moduleName, identifierDigest, declarationDigest, shapeDigest]);

    // Independent reconstruction of the R2-faithful `compctx1` from descriptor fields.
    private static string ExpectedCompctx1(VerifiedCSharpCompilation compilation, Func<string, string?> mapProject)
    {
        var d = compilation.Descriptor;
        var references = d.References.Select(r => mapProject(r) ?? r).Order(StringComparer.Ordinal).ToArray();
        var parts = new List<string>
        {
            "compctx1", d.RepositoryId, d.ProjectUnitId, d.AssemblyName,
            d.MetadataIdentity, d.ModuleName, d.OptionsDigest,
        };
        parts.AddRange(d.Sources.Select(s => s.CanonicalForm));
        parts.AddRange(references);
        return "compctx1:sha256:" + LengthPrefixedHash("compctx1-context", parts);
    }

    private static string ExpectedLegacyProjectReference(VerifiedCSharpCompilation dependency)
    {
        var compilation = dependency.Compilation;
        var trees = string.Join(",", compilation.SyntaxTrees
            .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
            .Select(tree => tree.FilePath.Replace('\\', '/') + "=" +
                Sha256(Encoding.UTF8.GetBytes(tree.GetText().ToString()))));
        var identity = compilation.Assembly.Identity;
        var token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return "compilation|" + compilation.AssemblyName + "|" +
            string.Join("|", identity.Name, identity.Version.ToString(), identity.CultureName ?? string.Empty, token) +
            "|" + trees;
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

    private static async Task<VerifiedCSharpSource> ReadSourceAsync(string path, string text) =>
        await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Sha256(Encoding.UTF8.GetBytes(text)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(text), writable: false))),
            CancellationToken.None);

    private static SyntaxTree Tree(VerifiedCSharpSource source, string[]? symbols = null) =>
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

    private static VerifiedCSharpCompilation Create(string projectUnit, CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadata,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(RepositoryId, projectUnit, compilation, sources, metadata, dependencies,
            CancellationToken.None);

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
