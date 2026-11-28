using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP7: Option A §5.2 tests 2 (transitive-only change), 6 (declared
/// comparison profiles) and 8 (stale publication/context refusal, in-memory
/// portion). Expected identities are reconstructed from the accepted frozen R2
/// formulas and the mapping contract; the production mapper is not used as its
/// own oracle. The tests establish the accepted distinction between stable
/// identity and current authority.
/// </summary>
public sealed class S2_2Wp7IdentityStabilityTests
{
    private const string RepositoryId = "fixture:wp7";
    private static readonly CodeRepositoryId Repository = new(RepositoryId);
    private static readonly CodePluginId Plugin = new("fixture.wp7.plugin");
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

    private const string LeafV1 = "namespace L; public static class Leaf { public static int V() => 1; }";
    private const string LeafV2 = "namespace L; public static class Leaf { public static int V() => 2; }";
    private const string MidSource = "namespace M; public static class Mid { public static int V() => L.Leaf.V(); }";
    private const string LibSource = "namespace B; public static class Lib { public static int V() => M.Mid.V(); }";
    private const string AppSource = "namespace A; public static class App { public static int Run() => B.Lib.V(); }";

    // =================================================== Option A §5.2 test 2

    [Fact]
    public async Task Wp7_Test2_TransitiveOnlyChange_StableIdentityChangedContextStaleRefused()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2);

        // Defining R2 fields of Lib are unchanged: the transitive-only change did
        // not alter Lib's own sources, options or the accepted project-reference
        // projection of its direct dependency (Mid).
        Assert.Equal(libA.Descriptor.AssemblyName, libB.Descriptor.AssemblyName);
        Assert.Equal(libA.Descriptor.MetadataIdentity, libB.Descriptor.MetadataIdentity);
        Assert.Equal(libA.Descriptor.ModuleName, libB.Descriptor.ModuleName);
        Assert.Equal(libA.Descriptor.OptionsDigest, libB.Descriptor.OptionsDigest);
        Assert.Equal(
            libA.Descriptor.Sources.Select(s => s.CanonicalForm),
            libB.Descriptor.Sources.Select(s => s.CanonicalForm));
        Assert.Equal(ExpectedDefiningContext(libA), ExpectedDefiningContext(libB));

        // compctx3 changes at every level: Leaf itself, Mid (direct dep of Leaf),
        // Lib and the caller App.
        Assert.NotEqual(leafA.Descriptor.DescriptorDigest, leafB.Descriptor.DescriptorDigest);
        Assert.NotEqual(midA.Descriptor.DescriptorDigest, midB.Descriptor.DescriptorDigest);
        Assert.NotEqual(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.NotEqual(appA.Descriptor.DescriptorDigest, appB.Descriptor.DescriptorDigest);

        // The changing Lib item is the verified project-reference context; the
        // mapped legacy item (and therefore the identity) is unchanged.
        Assert.Contains(libA.Descriptor.References, r => r.StartsWith("compilation-context|", StringComparison.Ordinal));
        Assert.NotEqual(
            string.Join("\n", libA.Descriptor.References),
            string.Join("\n", libB.Descriptor.References));

        // Authority: graph A evidence is stale under graph B; graph B mints; graph A
        // is reproducible under graph A with the unchanged identity.
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var universeA = Universe(leafA, midA, libA, appA);
        var universeB = Universe(leafB, midB, libB, appB);

        var mintA = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        Assert.Equal(VerifiedTargetMintStatus.Minted, mintA.Status);
        Assert.Equal(ExpectedAsmctx4(libA, targetA), mintA.TargetId!.Value);

        var stale = universeB.MintTarget(targetB, libA.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
        Assert.Null(stale.TargetId);

        var fresh = universeB.MintTarget(targetB, libB.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.Minted, fresh.Status);
        Assert.Equal(ExpectedAsmctx4(libB, targetB), fresh.TargetId!.Value);

        // Identity is stable even though the recorded context changed.
        Assert.Equal(mintA.TargetId!.Value, fresh.TargetId!.Value);
        Assert.NotEqual(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);

        var mintARepeat = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        Assert.Equal(mintA.TargetId!.Value, mintARepeat.TargetId!.Value);

        // usage5: refused under the stale recorded context, accepted under the
        // current context, with the same target identity and a caller-scoped value.
        var span = new CodeLocation("Lib/Lib.cs", 1, 1, 1, 12);
        var sourceHash = libB.Sources.Single(s => s.Path == span.Path).ContentHash;
        var staleUsage = universeB.MintUsage(new VerifiedUsageInput(
            Repository, Plugin, targetB, libA.Descriptor.DescriptorDigest,
            libB.Descriptor.ProjectUnitId, span, sourceHash, "sha256:content",
            TargetDoc(targetB), IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB),
            libB.Compilation));
        Assert.False(staleUsage.IsMinted);
        Assert.Null(staleUsage.Fact);

        var freshUsage = universeB.MintUsage(new VerifiedUsageInput(
            Repository, Plugin, targetB, libB.Descriptor.DescriptorDigest,
            libB.Descriptor.ProjectUnitId, span, sourceHash, "sha256:content",
            TargetDoc(targetB), IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB),
            libB.Compilation));
        Assert.True(freshUsage.IsMinted);
        Assert.Equal(ExpectedUsage5(libB, libB, targetB, span), freshUsage.Fact!.Id.Value);
        Assert.Equal(fresh.TargetId!.Value, freshUsage.Fact.TargetId.Value);
    }

    [Fact]
    public async Task Wp7_Test2_CrossProjectAssociation_StaleRecordedContextRefused()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2);
        var symbolA = BoundInvocationSymbol(appA, "V");
        var symbolB = BoundInvocationSymbol(appB, "V");

        var universeB = Universe(leafB, midB, libB, appB);
        var stale = universeB.MintTarget(symbolB, libA.Descriptor.DescriptorDigest,
            IdDigest(symbolB), DeclDigest(symbolB), ShapeDigest(symbolB), appB.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
        Assert.Null(stale.TargetId);

        var fresh = universeB.MintTarget(symbolB, libB.Descriptor.DescriptorDigest,
            IdDigest(symbolB), DeclDigest(symbolB), ShapeDigest(symbolB), appB.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.Minted, fresh.Status);
        Assert.Equal(ExpectedAsmctx4(libB, symbolB), fresh.TargetId!.Value);

        var universeA = Universe(leafA, midA, libA, appA);
        var replayed = universeA.MintTarget(symbolA, libA.Descriptor.DescriptorDigest,
            IdDigest(symbolA), DeclDigest(symbolA), ShapeDigest(symbolA), appA.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.Minted, replayed.Status);
        Assert.Equal(fresh.TargetId!.Value, replayed.TargetId!.Value);
    }

    [Fact]
    public async Task Wp7_Test2_DirectDependencyChange_ChangesTheDependentsIdentity()
    {
        // Negative control for the transitive-only rule: when Mid's own accepted
        // project-reference preimage changes (Leaf's trees), Mid's identity changes.
        // The stability demonstrated above is specific to the transitive dimension.
        var (_, midA, _, _) = await ChainAsync(LeafV1);
        var (_, midB, _, _) = await ChainAsync(LeafV2);
        Assert.NotEqual(ExpectedDefiningContext(midA), ExpectedDefiningContext(midB));
        Assert.NotEqual(
            ExpectedAsmctx4(midA, Target(midA.Compilation, "M.Mid", "V")),
            ExpectedAsmctx4(midB, Target(midB.Compilation, "M.Mid", "V")));
        Assert.NotEqual(midA.Descriptor.DescriptorDigest, midB.Descriptor.DescriptorDigest);
    }

    // =================================================== Option A §5.2 test 6

    [Fact]
    public async Task Wp7_Test6_EquivalentCaptures_ProductionProfile_EqualIdentityAndTarget()
    {
        var path = typeof(object).Assembly.Location;
        var first = VerifiedMetadataReference.Capture(path);
        var second = VerifiedMetadataReference.Capture(path);

        // Independent derivation of the successor content-bound capture identity:
        // refctx1|<assembly identity>|sha256:<lowercase hash of the captured bytes>.
        var expectedIdentity = SuccessorIdentityOracle.DescribeReferenceIdentity(path);
        Assert.Equal(expectedIdentity, first.Identity);
        Assert.Equal(first.Identity, second.Identity);
        Assert.DoesNotContain(Path.GetFullPath(path).Replace('\\', '/'), first.Identity);

        var a = await BuildWithReferenceAsync("S/S.csproj", "S", Source, first);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", Source, second);

        // Independently constructed equivalent artifacts map to the same successor
        // defining context and the same asmctx4.
        Assert.Equal(a.Descriptor.DescriptorDigest, b.Descriptor.DescriptorDigest);
        Assert.Equal(ExpectedDefiningContext(a), ExpectedDefiningContext(b));
        var method = Target(a.Compilation, "S.X", "V");
        Assert.Equal(ExpectedAsmctx4(a, method), ExpectedAsmctx4(b, method));
        var mint = Universe(a).MintTarget(method, a.Descriptor.DescriptorDigest,
            IdDigest(method), DeclDigest(method), ShapeDigest(method));
        Assert.Equal(ExpectedAsmctx4(a, method), mint.TargetId!.Value);
    }

    [Fact]
    public async Task Wp7_Test6_DifferentPath_SameContentIsEquivalent()
    {
        // The successor reference identity is content-bound: byte-identical
        // references installed at different absolute paths are the same semantic
        // input and cannot churn the defining context or target identity.
        var sourcePath = typeof(object).Assembly.Location;
        var copyPath = Path.Combine(Path.GetTempPath(), "wp7-ref-" + Guid.NewGuid().ToString("N") + ".dll");
        try
        {
            File.Copy(sourcePath, copyPath, overwrite: true);
            var original = VerifiedMetadataReference.Capture(sourcePath);
            var copy = VerifiedMetadataReference.Capture(copyPath);
            Assert.Equal(original.Identity, copy.Identity);
            Assert.StartsWith("refctx1|", original.Identity, StringComparison.Ordinal);
            Assert.DoesNotContain(Path.GetFullPath(sourcePath).Replace('\\', '/'), original.Identity);
            Assert.DoesNotContain(Path.GetFullPath(copyPath).Replace('\\', '/'), copy.Identity);

            var a = await BuildWithReferenceAsync("S/S.csproj", "S", Source, original);
            var b = await BuildWithReferenceAsync("S/S.csproj", "S", Source, copy);
            Assert.Equal(a.Descriptor.DescriptorDigest, b.Descriptor.DescriptorDigest);
            Assert.Equal(ExpectedDefiningContext(a), ExpectedDefiningContext(b));
            Assert.Equal(
                ExpectedAsmctx4(a, Target(a.Compilation, "S.X", "V")),
                ExpectedAsmctx4(b, Target(b.Compilation, "S.X", "V")));
        }
        finally
        {
            if (File.Exists(copyPath)) File.Delete(copyPath);
        }
    }

    [Fact]
    public async Task Wp7_Test6_DifferentReferenceBytes_NotEquivalent()
    {
        var platformPath = typeof(object).Assembly.Location;
        var otherPath = typeof(Andirit.CodeRepositoryId).Assembly.Location;
        var platform = VerifiedMetadataReference.Capture(platformPath);
        var other = VerifiedMetadataReference.Capture(otherPath);
        Assert.NotEqual(platform.Identity, other.Identity);

        var a = await BuildWithReferenceAsync("S/S.csproj", "S", Source, platform);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", Source, other);
        Assert.NotEqual(ExpectedDefiningContext(a), ExpectedDefiningContext(b));
        Assert.NotEqual(
            ExpectedAsmctx4(a, Target(a.Compilation, "S.X", "V")),
            ExpectedAsmctx4(b, Target(b.Compilation, "S.X", "V")));
    }

    [Fact]
    public async Task Wp7_Test6_CompilationOptionChange_ChangesIdentity()
    {
        var a = await BuildWithReferenceAsync("S/S.csproj", "S", Source, Platform.Value, Options);
        var debug = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Debug,
            nullableContextOptions: NullableContextOptions.Disable,
            deterministic: true,
            concurrentBuild: false);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", Source, Platform.Value, debug);
        Assert.NotEqual(a.Descriptor.OptionsDigest, b.Descriptor.OptionsDigest);
        Assert.NotEqual(ExpectedDefiningContext(a), ExpectedDefiningContext(b));
        Assert.NotEqual(
            ExpectedAsmctx4(a, Target(a.Compilation, "S.X", "V")),
            ExpectedAsmctx4(b, Target(b.Compilation, "S.X", "V")));
    }

    [Fact]
    public void Wp7_Test6_CrossProfileEqualityIsNotClaimed()
    {
        // The successor production capture profile is content-bound and path-free.
        // The historical frozen corpus fixture profile uses path/fixture labels;
        // its anchors apply only under that complete predecessor profile, and no
        // cross-profile or cross-generation equality is claimed.
        var production = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location).Identity;
        var fixtureLabel = "refctx1|runtime/System.Runtime.dll|" + new string('0', 64);

        Assert.StartsWith("refctx1|", production, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime/", production);
        Assert.DoesNotContain(Path.GetFullPath(typeof(object).Assembly.Location).Replace('\\', '/'), production);
        Assert.NotEqual(fixtureLabel, production);
        // The production identity contains the captured byte hash of the actual file.
        Assert.Contains(
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(object).Assembly.Location))),
            production);
    }

    // =================================================== Option A §5.2 test 8

    [Fact]
    public async Task Wp7_Test8_StalePublicationRefused_CurrentPublicationAccepted()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp7-publication");
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var target = Target(libA.Compilation, "B.Lib", "V");
        var universe = Universe(leafA, midA, libA, appA);
        var mint = universe.MintTarget(target, libA.Descriptor.DescriptorDigest,
            IdDigest(target), DeclDigest(target), ShapeDigest(target));
        Assert.Equal(VerifiedTargetMintStatus.Minted, mint.Status);

        var firstPublication = await PublishAsync(store, repo, "run:1", mint.TargetId!, line: 1);
        var current = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, mint.TargetId!, expectedPublication: firstPublication));
        Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
        Assert.True(current.IsComplete);
        Assert.Single(current.Occurrences);

        var secondPublication = await PublishAsync(store, repo, "run:2", mint.TargetId!, line: 2);
        Assert.NotEqual(firstPublication, secondPublication);

        var stale = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, mint.TargetId!, expectedPublication: firstPublication));
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
        Assert.Empty(stale.Occurrences);

        var accepted = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, mint.TargetId!, expectedPublication: secondPublication));
        Assert.Equal(CodeOccurrenceAvailability.Available, accepted.Availability);
        Assert.Equal(2, accepted.Occurrences[0].Span.StartLine);
    }

    [Fact]
    public async Task Wp7_Test8_StaleRecordedContext_NoMintAndNoTrustedUsage()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2);
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var universeA = Universe(leafA, midA, libA, appA);
        var universeB = Universe(leafB, midB, libB, appB);

        var mintA = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        Assert.True(mintA.IsMinted);

        // A previously minted structural identity is not current authority: reusing
        // graph A's recorded context against graph B refuses minting.
        var replay = universeB.MintTarget(targetB, libA.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, replay.Status);
        Assert.Null(replay.TargetId);
        Assert.NotEqual(mintA.TargetId!.Value, replay.TargetId?.Value);

        // Fabricated canonical-shaped recorded context is refused as stale.
        var fabricated = universeB.MintTarget(targetB, "compctx3:sha256:" + new string('a', 64),
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.False(fabricated.IsMinted);
        Assert.Null(fabricated.TargetId);

        // Missing recorded context cannot mint (WP4 G3 precondition).
        var missing = universeB.MintTarget(targetB, null,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext, missing.Status);
        Assert.Null(missing.TargetId);
    }

    [Fact]
    public async Task Wp7_Test8_SubstitutedAndIncompleteUniverses_Refused()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var (leafA2, midA2, libA2, appA2) = await ChainAsync(LeafV1);
        // Metadata-equivalent instances: identical descriptor digests.
        Assert.Equal(libA.Descriptor.DescriptorDigest, libA2.Descriptor.DescriptorDigest);

        var universeA = Universe(leafA, midA, libA, appA);
        var substituted = Target(libA2.Compilation, "B.Lib", "V");
        var substitutedMint = universeA.MintTarget(substituted, libA.Descriptor.DescriptorDigest,
            IdDigest(substituted), DeclDigest(substituted), ShapeDigest(substituted));
        Assert.False(substitutedMint.IsMinted);
        Assert.Null(substitutedMint.TargetId);

        // The exact instance in its own universe still mints the same identity.
        var own = Universe(leafA2, midA2, libA2, appA2).MintTarget(substituted, libA2.Descriptor.DescriptorDigest,
            IdDigest(substituted), DeclDigest(substituted), ShapeDigest(substituted));
        Assert.True(own.IsMinted);

        // Incomplete declared scope refuses before any minting. The entry chain is
        // complete; the declared scope contains one uncovered unit.
        var scope = VerifiedCSharpScope.Declare(RepositoryId,
            [new("Leaf/Leaf.csproj", "Leaf/Leaf.csproj", ["Leaf/Leaf.cs"]),
             new("Mid/Mid.csproj", "Mid/Mid.csproj", ["Mid/Mid.cs"]),
             new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var incompleteUniverse = VerifiedCSharpUniverse.Create(RepositoryId, scope, [leafA, midA, libA]);
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var incomplete = incompleteUniverse.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        Assert.Equal(VerifiedTargetMintStatus.Incomplete, incomplete.Status);
        Assert.Null(incomplete.TargetId);
    }

    // ============================================================ determinism

    [Fact]
    public async Task Wp7_Determinism_FreshEquivalentGraphs_CanonicalDigest()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV1);
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var mintA = Universe(leafA, midA, libA, appA).MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        var mintB = Universe(leafB, midB, libB, appB).MintTarget(targetB, libB.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));

        Assert.Equal(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.Equal(mintA.TargetId!.Value, mintB.TargetId!.Value);
        Assert.Equal(ExpectedAsmctx4(libA, targetA), mintA.TargetId.Value);
        Console.WriteLine("WP7-DIGEST|asmctx4=" + mintA.TargetId.Value +
            "|compctx3=" + libA.Descriptor.DescriptorDigest);
    }

    // ---------------------------------------------------------------- helpers

    private static string Source =>
        "namespace S; public static class X { public static int V() => 1; }";

    private static async Task<(VerifiedCSharpCompilation Leaf, VerifiedCSharpCompilation Mid,
        VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App)> ChainAsync(string leafText)
    {
        var leaf = await BuildUnitAsync("Leaf/Leaf.csproj", "Leaf", leafText, [], []);
        var mid = await BuildUnitAsync("Mid/Mid.csproj", "Mid", MidSource, [], [leaf]);
        var lib = await BuildUnitAsync("Lib/Lib.csproj", "Lib", LibSource, [], [mid]);
        var app = await BuildUnitAsync("App/App.csproj", "App", AppSource, [], [lib]);
        return (leaf, mid, lib, app);
    }

    private static async Task<VerifiedCSharpCompilation> BuildWithReferenceAsync(
        string unit, string assembly, string text, VerifiedMetadataReference reference,
        CSharpCompilationOptions? options = null)
        => await BuildUnitAsync(unit, assembly, text, [], [], reference, options ?? Options);

    private static async Task<VerifiedCSharpCompilation> BuildUnitAsync(
        string unit, string assembly, string text, string[] symbols,
        VerifiedCSharpCompilation[] dependencies,
        VerifiedMetadataReference? metadata = null, CSharpCompilationOptions? options = null)
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
        var captured = metadata ?? Platform.Value;
        var references = new List<MetadataReference> { captured.Reference };
        references.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, [tree], references, options ?? Options);
        return VerifiedCSharpCompilation.Create(RepositoryId, unit, compilation, [source], [captured],
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

    private static IMethodSymbol BoundInvocationSymbol(VerifiedCSharpCompilation unit, string methodName)
    {
        var tree = unit.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        return Assert.IsAssignableFrom<IMethodSymbol>(
            unit.Compilation.GetSemanticModel(tree).GetSymbolInfo(invocation).Symbol);
    }

    /// <summary>Independent successor defining-context value.</summary>
    private static string ExpectedDefiningContext(VerifiedCSharpCompilation unit) =>
        SuccessorIdentityOracle.DescribeDefiningContext(RepositoryId, unit);

    private static string ExpectedAsmctx4(VerifiedCSharpCompilation definingUnit, IMethodSymbol method) =>
        SuccessorIdentityOracle.TargetId(RepositoryId, definingUnit, method, IdDigest, DeclDigest, ShapeDigest);

    private static string ExpectedUsage5(VerifiedCSharpCompilation callingUnit,
        VerifiedCSharpCompilation definingUnit, IMethodSymbol method, CodeLocation span)
    {
        var sourceHash = callingUnit.Sources.Single(s => s.Path == span.Path).ContentHash;
        return SuccessorIdentityOracle.UsageId(RepositoryId, Plugin.Value,
            callingUnit.Descriptor.ProjectUnitId, span.Path, sourceHash, span,
            TargetDoc(method), ExpectedAsmctx4(definingUnit, method));
    }

    private static async Task<CodeGraphPublication> PublishAsync(
        InMemoryCodeGraphStore store, CodeRepositoryId repo, string run,
        CodeOccurrenceTargetId target, int line)
    {
        var unitId = "unit:target";
        var fact = CodeOccurrenceFact.Create(
            repo, target, target.Value, unitId,
            new CodeOccurrenceContributor(Plugin, unitId),
            CodeOccurrenceCategory.Invocation,
            new CodeLocation("src/A.cs", line, 1, line, 8),
            "sha256:source", "content:invocation");
        var unit = new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", new CodeIndexRunId(run), new CodeIndexUnitId(unitId)),
            occurrences: [fact],
            occurrenceAnalysisIncluded: true,
            occurrenceCoverage: new CodeOccurrenceCoverage(
                1, 0, CodeRelationshipCoverageState.Produced,
                "analysisscope1:sha256:" + new string('a', 64),
                "compilation-universe1:sha256:" + new string('b', 64),
                declaredUnitCount: 1, verifiedUnitCount: 1));
        var started = new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
        var runId = new CodeIndexRunId(run);
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(unit);
        await store.CompleteIndexRunAsync(
            new(repo, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
        return (await store.GetLatestPublicationAsync(repo))!;
    }

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}



