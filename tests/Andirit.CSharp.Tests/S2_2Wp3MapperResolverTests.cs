using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP3: production mapper/resolver and pre-mint validation. Exercises the
/// R2-faithful `compctx1` mapping and the pre-mint association resolver against
/// authority derived from the accepted mapping contract and the frozen R2
/// algorithm. No extraction, target/occurrence minting, persistence or reopen.
/// Expected values are reconstructed independently from the accepted formulas and
/// known inputs; they never read the implementation under test.
/// </summary>
public sealed class S2_2Wp3MapperResolverTests
{
    private const string RepositoryId = "fixture:wp3";
    private static readonly CSharpCompilationOptions LibraryOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);

    // ------------------------------------------------------------ WP3-B: mapper

    [Fact]
    public async Task Mapper_StandaloneEqualsLegacyTwinAndIndependentR2Tuple()
    {
        var platform = CapturePlatform();
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A { public int Value; }");
        var standalone = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);

        var mapped = standalone.MapR2FaithfulCompctx1();

        Assert.Equal("compctx1", mapped.ContractVersion);
        Assert.Equal(standalone.LegacyDescriptor!.DescriptorDigest, mapped.DescriptorDigest);
        Assert.Equal(mapped.DescriptorDigest, mapped.Verified().DescriptorDigest);
        Assert.Equal(
            IndependentCompctx1(standalone, static _ => null),
            mapped.DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_DependencyBearingReplacesContextItemWithR2LegacyItem()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [lib]);

        Assert.Null(app.LegacyDescriptor);
        Assert.Contains("compilation-context|" + lib.Descriptor.DescriptorDigest, app.Descriptor.References);

        var mapped = app.MapR2FaithfulCompctx1();

        Assert.Equal("compctx1", mapped.ContractVersion);
        Assert.DoesNotContain(mapped.References, r => r.StartsWith("compilation-context|", StringComparison.Ordinal));
        Assert.Contains(mapped.References, r => r.StartsWith("compilation|Lib|", StringComparison.Ordinal));
        Assert.Equal(IndependentCompctx1(app,
            r => r.StartsWith("compilation-context|", StringComparison.Ordinal)
                ? IndependentLegacyProjectReference(lib) : null),
            mapped.DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_TransitiveLegacyItemEqualsFrozenR2Formula()
    {
        // The mapped project-reference item must equal the frozen R2 formula
        // `compilation|<assembly>|<metadata>|<trees>` (sorted ordinal by original
        // FilePath, tree-text SHA-256, comma-joined) reconstructed independently.
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [lib]);

        var legacy = app.MapR2FaithfulCompctx1().References
            .Single(r => r.StartsWith("compilation|", StringComparison.Ordinal));
        Assert.Equal(IndependentLegacyProjectReference(lib), legacy);
    }

    [Fact]
    public async Task Mapper_IsDeterministicAcrossRepeatedAndEquivalentInstances()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");

        var libA = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appA = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, libA.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [libA]);

        var libB = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appB = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, libB.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [libB]);

        Assert.Equal(appA.MapR2FaithfulCompctx1().DescriptorDigest, appA.MapR2FaithfulCompctx1().DescriptorDigest);
        Assert.Equal(appA.MapR2FaithfulCompctx1().DescriptorDigest, appB.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_TransitiveOnlyDependencyContextChangeDoesNotChurnIdentity()
    {
        // Option A no-churn: the direct dependency's options change (its verified
        // compctx2 changes) while its R2-visible legacy fields (assembly, metadata,
        // trees) are unchanged, so the mapped defining compctx1 is stable even
        // though the defining compilation's own compctx2 changes.
        var platform = CapturePlatform();
        var leafSource = await ReadTextSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 1; }");
        var leaf1 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leafSource)], [platform.Reference], LibraryOptions), [leafSource], [platform], []);
        var leaf2 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leafSource)], [platform.Reference],
                LibraryOptions.WithNullableContextOptions(NullableContextOptions.Enable)), [leafSource], [platform], []);

        var midSource = await ReadTextSourceAsync("Mid/Mid.cs", "namespace N; public static class Mid { public static int Read() => Leaf.Value(); }");
        var mid1 = CreateVerified("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [platform.Reference, leaf1.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [platform], [leaf1]);
        var mid2 = CreateVerified("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [platform.Reference, leaf2.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [platform], [leaf2]);

        Assert.NotEqual(leaf1.Descriptor.DescriptorDigest, leaf2.Descriptor.DescriptorDigest);
        Assert.NotEqual(mid1.Descriptor.DescriptorDigest, mid2.Descriptor.DescriptorDigest);
        Assert.Equal(mid1.MapR2FaithfulCompctx1().DescriptorDigest, mid2.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_DefiningSourceChangeChangesMappedIdentity()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var one = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var two = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get() + 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appOne = CreateVerified("App/App.csproj",
            Compile("App", [Tree(one)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions), [one], [platform], [lib]);
        var appTwo = CreateVerified("App/App.csproj",
            Compile("App", [Tree(two)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions), [two], [platform], [lib]);

        Assert.NotEqual(appOne.MapR2FaithfulCompctx1().DescriptorDigest, appTwo.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_TransitiveDependencySourceChangePropagatesToCaller()
    {
        var platform = CapturePlatform();
        var leaf1 = await ReadTextSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 1; }");
        var leaf2 = await ReadTextSourceAsync("Leaf/Leaf.cs", "namespace N; public static class Leaf { public static int Value() => 2; }");
        var verifiedLeaf1 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leaf1)], [platform.Reference], LibraryOptions), [leaf1], [platform], []);
        var verifiedLeaf2 = CreateVerified("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(leaf2)], [platform.Reference], LibraryOptions), [leaf2], [platform], []);
        var midSource = await ReadTextSourceAsync("Mid/Mid.cs", "namespace N; public static class Mid { public static int Read() => Leaf.Value(); }");
        var mid1 = CreateVerified("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [platform.Reference, verifiedLeaf1.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [platform], [verifiedLeaf1]);
        var mid2 = CreateVerified("Mid/Mid.csproj",
            Compile("Mid", [Tree(midSource)], [platform.Reference, verifiedLeaf2.Compilation.ToMetadataReference()], LibraryOptions),
            [midSource], [platform], [verifiedLeaf2]);

        Assert.NotEqual(mid1.MapR2FaithfulCompctx1().DescriptorDigest, mid2.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_PreservesActualPeIdentityAndDoesNotSubstituteFixtureLabels()
    {
        // C2: the declared comparison profiles must not be conflated. Production
        // keeps the actual captured PE identities (`file|<actual path>|<sha256>`);
        // it never rewrites paths to the frozen `runtime/...` fixture labels.
        var platform = CapturePlatform();
        var source = await ReadTextSourceAsync("src/A.cs", "public sealed class A {}");
        var verified = CreateVerified("src/A.csproj",
            Compile("A", [Tree(source)], [platform.Reference], LibraryOptions), [source], [platform], []);

        var mapped = verified.MapR2FaithfulCompctx1();

        Assert.Contains(platform.Identity, mapped.References);
        Assert.Contains(mapped.References, r => r.StartsWith("file|", StringComparison.Ordinal));
        Assert.DoesNotContain(mapped.References, r => r.Contains("runtime/System.", StringComparison.Ordinal));
        Assert.Equal(verified.Descriptor.References.Order(StringComparer.Ordinal).ToArray(),
            mapped.References.ToArray());
    }

    [Fact]
    public async Task Mapper_PreservesCompctx2Isolation()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [lib]);

        var mapped = app.MapR2FaithfulCompctx1();

        Assert.DoesNotContain("compctx2", mapped.ComputeDigest());
        Assert.DoesNotContain(mapped.References, r => r.Contains("compctx2", StringComparison.Ordinal));
        Assert.StartsWith("compctx1:sha256:", mapped.DescriptorDigest);
        Assert.StartsWith("compctx2:sha256:", app.Descriptor.DescriptorDigest);
        Assert.NotEqual(mapped.DescriptorDigest, app.Descriptor.DescriptorDigest);
    }

    // ---------------------------------------------------- WP3-D: pre-mint resolver

    [Fact]
    public async Task Resolver_CompletesForVerifiedDefiningContextAndMapsCompctx1()
    {
        var (universe, target, caller) = BuildTwoProjectUniverse();
        var defining = universe.Entries.Single(e => e.Descriptor.ProjectUnitId == "Lib/Lib.csproj");

        var result = universe.ResolveDefining(target, defining.Descriptor.DescriptorDigest, caller);

        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        Assert.True(result.IsComplete);
        Assert.Same(defining, result.Defining);
        Assert.Equal(defining.MapR2FaithfulCompctx1().DescriptorDigest, result.Compctx1!.DescriptorDigest);
    }

    [Fact]
    public async Task Resolver_RefusesStaleOrFabricatedRecordedContext()
    {
        var (universe, target, caller) = BuildTwoProjectUniverse();

        var stale = universe.ResolveDefining(target, "compctx2:sha256:" + new string('0', 64), caller);
        var shapeOnly = universe.ResolveDefining(target, "compctx2:sha256:" + new string('a', 64), caller);

        Assert.Equal(VerifiedDefiningStatus.StaleContext, stale.Status);
        Assert.Null(stale.Compctx1);
        Assert.Equal(VerifiedDefiningStatus.StaleContext, shapeOnly.Status);
    }

    [Fact]
    public async Task Resolver_RefusesMissingDefiningCompilation()
    {
        var (universe, _, _) = BuildTwoProjectUniverse();
        var unrelated = Compile("Unrelated", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], Array.Empty<MetadataReference>(), LibraryOptions);

        var result = universe.ResolveDefining(TargetSymbol(unrelated, "U.U", "M"), null, unrelated);

        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation, result.Status);
    }

    [Fact]
    public async Task Resolver_RefusesAmbiguousBinding()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libA = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var libB = CreateVerified("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        // The caller holds two live CompilationReferences to two distinct verified
        // compilations that both present the same defining assembly identity.
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)],
                [platform.Reference, libA.Compilation.ToMetadataReference(), libB.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [libA, libB]);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Lib2/Lib2.csproj", "Lib2/Lib2.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [libA, libB, app]);

        var result = universe.ResolveDefining(TargetSymbol(app, "N.Lib", "Get"), null, app.Compilation);

        Assert.Equal(VerifiedDefiningStatus.Ambiguous, result.Status);
    }

    [Fact]
    public async Task Resolver_RefusesIncompleteUniverse()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [lib]);
        // Scope declares a unit that has no verified context, so the universe is incomplete.
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib, app]);

        var result = universe.ResolveDefining(TargetSymbol(app, "N.Lib", "Get"), null, app.Compilation);

        Assert.Equal(VerifiedDefiningStatus.Incomplete, result.Status);
        Assert.False(universe.IsComplete);
    }

    [Fact]
    public async Task Resolver_RefusesExternalDefiningUnit()
    {
        // The defining unit is verified and live but not declared in the scope.
        // The universe rejects scope-membership construction, so this negative is
        // asserted at construction, matching the accepted "scope is the authority
        // boundary" invariant.
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Other/Other.csproj", "Other/Other.csproj", ["Other/Other.cs"])]);

        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib]));
    }

    [Fact]
    public async Task Resolver_RefusesSubstitutedLiveInstanceWithSameDigest()
    {
        var platform = CapturePlatform();
        var libSource = await ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libA = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var libB = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        Assert.Equal(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.NotSame(libA.Compilation, libB.Compilation);

        // The universe's complete verified context references the exact libA
        // instance; a separate caller compilation references the substituted libB
        // instance (identical digest, different live object).
        var appSource = await ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, libA.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [libA]);
        var caller = Compile("App", [Tree(appSource)], [platform.Reference, libB.Compilation.ToMetadataReference()], LibraryOptions);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [libA, app]);

        var result = universe.ResolveDefining(TargetSymbol(caller, "N.Lib", "Get"), null, caller);

        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation, result.Status);
    }

    // ------------------------------------------------------------------- helpers

    private (VerifiedCSharpUniverse Universe, IMethodSymbol Target, CSharpCompilation Caller) BuildTwoProjectUniverse()
    {
        var platform = CapturePlatform();
        var libSource = ReadTextSourceAsync("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }").GetAwaiter().GetResult();
        var lib = CreateVerified("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [platform.Reference], LibraryOptions), [libSource], [platform], []);
        var appSource = ReadTextSourceAsync("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }").GetAwaiter().GetResult();
        var app = CreateVerified("App/App.csproj",
            Compile("App", [Tree(appSource)], [platform.Reference, lib.Compilation.ToMetadataReference()], LibraryOptions),
            [appSource], [platform], [lib]);
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(RepositoryId, scope, [lib, app]);
        return (universe, TargetSymbol(app, "N.Lib", "Get"), app.Compilation);
    }

    private static IMethodSymbol TargetSymbol(VerifiedCSharpCompilation compilation, string typeName, string method) =>
        TargetSymbol(compilation.Compilation, typeName, method);

    private static IMethodSymbol TargetSymbol(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    private static string IndependentCompctx1(
        VerifiedCSharpCompilation compilation, Func<string, string?> mapProjectReference)
    {
        var d = compilation.Descriptor;
        var references = d.References
            .Select(r => mapProjectReference(r) ?? r)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var parts = new List<string>
        {
            "compctx1", d.RepositoryId, d.ProjectUnitId, d.AssemblyName,
            d.MetadataIdentity, d.ModuleName, d.OptionsDigest,
        };
        parts.AddRange(d.Sources.Select(s => s.CanonicalForm));
        parts.AddRange(references);
        return "compctx1:sha256:" + LengthPrefixedHash("compctx1-context", parts);
    }

    private static string IndependentLegacyProjectReference(VerifiedCSharpCompilation dependency)
    {
        var compilation = dependency.Compilation;
        var trees = string.Join(",", compilation.SyntaxTrees
            .OrderBy(tree => tree.FilePath, StringComparer.Ordinal)
            .Select(tree => tree.FilePath.Replace('\\', '/') + "=" + Sha256(Encoding.UTF8.GetBytes(tree.GetText().ToString()))));
        return "compilation|" + compilation.AssemblyName + "|" + Metadata(compilation.Assembly.Identity) + "|" + trees;
    }

    private static string Metadata(AssemblyIdentity identity)
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

    private static async Task<VerifiedCSharpSource> ReadTextSourceAsync(string path, string text) =>
        await ReadSourceAsync(path, Encoding.UTF8.GetBytes(text));

    private static async Task<VerifiedCSharpSource> ReadSourceAsync(string path, byte[] bytes) =>
        await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Sha256(bytes),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None);

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static CSharpParseOptions ParseOptions(params string[] symbols) =>
        new(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular, symbols);

    private static SyntaxTree Tree(VerifiedCSharpSource source, CSharpParseOptions? options = null) =>
        CSharpSyntaxTree.ParseText(source.Text, options ?? ParseOptions(), source.Path);

    private static SyntaxTree Tree(string path, string text) =>
        CSharpSyntaxTree.ParseText(text, ParseOptions(), path);

    private static CSharpCompilation Compile(
        string assembly,
        IEnumerable<SyntaxTree> trees,
        IEnumerable<MetadataReference> references,
        CSharpCompilationOptions options) =>
        CSharpCompilation.Create(assembly, trees, references, options);

    private static VerifiedCSharpCompilation CreateVerified(
        string projectUnit,
        CSharpCompilation compilation,
        IReadOnlyCollection<VerifiedCSharpSource> sources,
        IReadOnlyCollection<VerifiedMetadataReference> metadataReferences,
        IReadOnlyCollection<VerifiedCSharpCompilation> dependencies) =>
        VerifiedCSharpCompilation.Create(
            RepositoryId, projectUnit, compilation, sources, metadataReferences, dependencies,
            CancellationToken.None);

    private static VerifiedMetadataReference CapturePlatform() =>
        VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None);
}
