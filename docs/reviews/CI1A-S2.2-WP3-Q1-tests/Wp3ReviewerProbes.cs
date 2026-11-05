using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp3Qualification;

// Independent WP3 mapper/resolver probes. Expected R2 compctx1 values are computed by the
// accepted frozen R2 authority (AdrCi1Exp.Shared.CompilationProvenance.Describe), never by the
// production mapper under test.
public sealed class Wp3ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "review:wp3-q1";
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private static readonly Dictionary<string, byte[]> Bytes = new(StringComparer.Ordinal);

    // ---------------------------------------------------------------- mapper probes

    [Fact]
    public async Task Mapper_Standalone_EqualsFrozenR2Authority()
    {
        var (source, text) = await Src("src/A.cs", "public sealed class A { public int Value; }");
        var compilation = Compile("A", [Tree(source)], [Platform.Value.Reference], Options);
        var verified = Create("src/A.csproj", compilation, [source], [Platform.Value], []);

        var mapped = verified.MapR2FaithfulCompctx1();
        var r2 = Frozen.CompilationProvenance.Describe(Repo, "src/A.csproj", compilation,
            [("src/A.cs", text)]);

        output.WriteLine("mapped=" + mapped.DescriptorDigest);
        output.WriteLine("frozenR2=" + r2.DescriptorDigest);
        Assert.Equal(r2.DescriptorDigest, mapped.DescriptorDigest);
        Assert.Equal("compctx1", mapped.ContractVersion);
    }

    [Fact]
    public async Task Mapper_DependencyBearing_EqualsFrozenR2Authority()
    {
        var (libSource, libText) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libCompilation = Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options);
        var lib = Create("Lib/Lib.csproj", libCompilation, [libSource], [Platform.Value], []);

        var (appSource, appText) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var appCompilation = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libCompilation.ToMetadataReference()], Options);
        var app = Create("App/App.csproj", appCompilation, [appSource], [Platform.Value], [lib]);

        var mapped = app.MapR2FaithfulCompctx1();
        var r2 = Frozen.CompilationProvenance.Describe(Repo, "App/App.csproj", appCompilation,
            [("App/App.cs", appText)]);

        output.WriteLine("mapped=" + mapped.DescriptorDigest);
        output.WriteLine("frozenR2=" + r2.DescriptorDigest);
        foreach (var reference in mapped.References) output.WriteLine("mapped.ref=" + reference);
        Assert.Equal(r2.DescriptorDigest, mapped.DescriptorDigest);
        Assert.DoesNotContain(mapped.References, r => r.StartsWith("compilation-context|", StringComparison.Ordinal));
        Assert.Contains(mapped.References, r => r.StartsWith("compilation|Lib|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Mapper_TwoDistinctDependencies_EqualsFrozenR2Authority()
    {
        var (libOneSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var (libTwoSource, _) = await Src("Lib2/Lib2.cs", "namespace N; public static class Other { public static int Read() => 2; }");
        var libOne = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libOneSource)], [Platform.Value.Reference], Options), [libOneSource], [Platform.Value], []);
        var libTwo = Create("Lib2/Lib2.csproj",
            Compile("Other", [Tree(libTwoSource)], [Platform.Value.Reference], Options), [libTwoSource], [Platform.Value], []);

        var (appSource, appText) = await Src("App/App.cs", "public static class App { public static int Run() => 1; }");
        var appCompilation = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libOne.Compilation.ToMetadataReference(), libTwo.Compilation.ToMetadataReference()], Options);
        var app = Create("App/App.csproj", appCompilation, [appSource], [Platform.Value], [libOne, libTwo]);

        var mapped = app.MapR2FaithfulCompctx1();
        var r2 = Frozen.CompilationProvenance.Describe(Repo, "App/App.csproj", appCompilation,
            [("App/App.cs", appText)]);

        Assert.Equal(r2.DescriptorDigest, mapped.DescriptorDigest);
        Assert.Contains(mapped.References, r => r.StartsWith("compilation|Lib|", StringComparison.Ordinal));
        Assert.Contains(mapped.References, r => r.StartsWith("compilation|Other|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Mapper_IdenticalLegacyItems_NoDedup_EqualsFrozenR2Authority()
    {
        // Two distinct project units with identical R2-visible material must project two identical
        // legacy items; R2 does not deduplicate the projected reference list.
        var (source, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libOne = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(source)], [Platform.Value.Reference], Options), [source], [Platform.Value], []);
        var libTwo = Create("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(source)], [Platform.Value.Reference], Options), [source], [Platform.Value], []);

        var (appSource, appText) = await Src("App/App.cs", "public static class App { public static int Run() => 1; }");
        var appCompilation = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libOne.Compilation.ToMetadataReference(), libTwo.Compilation.ToMetadataReference()], Options);
        var app = Create("App/App.csproj", appCompilation, [appSource], [Platform.Value], [libOne, libTwo]);

        var mapped = app.MapR2FaithfulCompctx1();
        var r2 = Frozen.CompilationProvenance.Describe(Repo, "App/App.csproj", appCompilation,
            [("App/App.cs", appText)]);

        var mappedProjects = mapped.References.Where(r => r.StartsWith("compilation|", StringComparison.Ordinal)).ToArray();
        output.WriteLine("mapped project items: " + mappedProjects.Length);
        Assert.Equal(2, mappedProjects.Length);
        Assert.Equal(mappedProjects[0], mappedProjects[1]);
        Assert.Equal(r2.DescriptorDigest, mapped.DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_DeterministicAcrossFreshConstructedInstances()
    {
        var digests = new List<string>();
        for (var i = 0; i < 3; i++)
        {
            var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
            var lib = Create("Lib/Lib.csproj",
                Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
            var (appSource, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
            var app = Create("App/App.csproj",
                Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options),
                [appSource], [Platform.Value], [lib]);
            digests.Add(app.MapR2FaithfulCompctx1().DescriptorDigest);
            digests.Add(app.MapR2FaithfulCompctx1().DescriptorDigest);
        }

        Assert.Single(digests.Distinct(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Mapper_PreservesActualPeIdentity_AndIsolatesCompctx2()
    {
        var (source, _) = await Src("src/A.cs", "public sealed class A {}");
        var verified = Create("src/A.csproj",
            Compile("A", [Tree(source)], [Platform.Value.Reference], Options), [source], [Platform.Value], []);

        var mapped = verified.MapR2FaithfulCompctx1();

        Assert.Contains(Platform.Value.Identity, mapped.References);
        Assert.DoesNotContain(mapped.References, r => r.Contains("runtime/System.", StringComparison.Ordinal));
        Assert.DoesNotContain("compctx2", mapped.ComputeDigest());
        Assert.StartsWith("compctx1:sha256:", mapped.DescriptorDigest);
        Assert.StartsWith("compctx2:sha256:", verified.Descriptor.DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_DefiningSourceChange_ChangesMappedIdentity_TransitiveOnlyDoesNot()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (one, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var (two, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get() + 1; }");
        var appOne = Create("App/App.csproj",
            Compile("App", [Tree(one)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options), [one], [Platform.Value], [lib]);
        var appTwo = Create("App/App.csproj",
            Compile("App", [Tree(two)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options), [two], [Platform.Value], [lib]);
        Assert.NotEqual(appOne.MapR2FaithfulCompctx1().DescriptorDigest, appTwo.MapR2FaithfulCompctx1().DescriptorDigest);

        // Transitive-only: dependency's own options change, but its R2-visible material does not.
        var leafOne = Create("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var leafTwo = Create("Leaf/Leaf.csproj",
            Compile("Leaf", [Tree(libSource)], [Platform.Value.Reference],
                Options.WithNullableContextOptions(NullableContextOptions.Enable)), [libSource], [Platform.Value], []);
        var (mid, _) = await Src("Mid/Mid.cs", "namespace N; public static class Mid { public static int Read() => Leaf.Get(); }");
        var midOne = Create("Mid/Mid.csproj",
            Compile("Mid", [Tree(mid)], [Platform.Value.Reference, leafOne.Compilation.ToMetadataReference()], Options), [mid], [Platform.Value], [leafOne]);
        var midTwo = Create("Mid/Mid.csproj",
            Compile("Mid", [Tree(mid)], [Platform.Value.Reference, leafTwo.Compilation.ToMetadataReference()], Options), [mid], [Platform.Value], [leafTwo]);
        Assert.NotEqual(leafOne.Descriptor.DescriptorDigest, leafTwo.Descriptor.DescriptorDigest);
        Assert.Equal(midOne.MapR2FaithfulCompctx1().DescriptorDigest, midTwo.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_LegacyTwinSeparatedFromS22Mapper()
    {
        var (standaloneSource, _) = await Src("src/A.cs", "public sealed class A {}");
        var standalone = Create("src/A.csproj",
            Compile("A", [Tree(standaloneSource)], [Platform.Value.Reference], Options), [standaloneSource], [Platform.Value], []);
        Assert.NotNull(standalone.LegacyDescriptor);
        Assert.Equal(standalone.LegacyDescriptor!.DescriptorDigest, standalone.MapR2FaithfulCompctx1().DescriptorDigest);

        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", "public static class App { public static int Run() => 1; }");
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [lib]);
        Assert.Null(app.LegacyDescriptor);
        Assert.StartsWith("compctx1:sha256:", app.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    [Fact]
    public async Task Mapper_DifferentReferenceInventory_DoesNotCollapse()
    {
        var (source, _) = await Src("src/A.cs", "public sealed class A {}");
        var other = VerifiedMetadataReference.Capture(
            typeof(VerifiedCSharpCompilation).Assembly.Location, CancellationToken.None);
        var one = Create("src/A.csproj",
            Compile("A", [Tree(source)], [Platform.Value.Reference], Options), [source], [Platform.Value], []);
        var two = Create("src/A.csproj",
            Compile("A", [Tree(source)], [Platform.Value.Reference, other.Reference], Options),
            [source], [Platform.Value, other], []);
        Assert.NotEqual(one.MapR2FaithfulCompctx1().DescriptorDigest, two.MapR2FaithfulCompctx1().DescriptorDigest);
    }

    // -------------------------------------------------------------- resolver probes

    [Fact]
    public async Task Resolver_Complete_ExactLiveInstance_ReturnsMappedCompctx1()
    {
        var (universe, target, caller, lib) = await TwoProjectUniverse();
        var result = universe.ResolveDefining(target, lib.Descriptor.DescriptorDigest, caller);
        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        Assert.Same(lib, result.Defining);
        Assert.Equal(lib.MapR2FaithfulCompctx1().DescriptorDigest, result.Compctx1!.DescriptorDigest);
    }

    [Fact]
    public async Task Resolver_BindsCrossProjectViaSharedAssemblySymbol()
    {
        // Documents which branch binds the ordinary cross-project positive: a symbol taken
        // from the caller's semantic model is the referenced compilation's own symbol.
        var (universe, target, caller, lib) = await TwoProjectUniverse();
        output.WriteLine("sameAssemblyInstance=" +
            ReferenceEquals(target.ContainingAssembly, lib.Compilation.Assembly));
        Assert.Same(lib.Compilation.Assembly, target.ContainingAssembly);
        Assert.Equal(VerifiedDefiningStatus.Complete, universe.ResolveDefining(target, null, caller).Status);
    }

    [Fact]
    public async Task Resolver_RefusesStaleAndFabricatedRecordedContext()
    {
        var (universe, target, caller, lib) = await TwoProjectUniverse();
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            universe.ResolveDefining(target, "compctx2:sha256:" + new string('0', 64), caller).Status);
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            universe.ResolveDefining(target, "compctx2:sha256:" + new string('a', 64), caller).Status);
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            universe.ResolveDefining(target, "not-a-context", caller).Status);
        Assert.Equal(VerifiedDefiningStatus.Complete,
            universe.ResolveDefining(target, lib.Descriptor.DescriptorDigest, caller).Status);
    }

    [Fact]
    public async Task Resolver_RefusesSubstitutedLiveInstanceWithSameDigest()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var libB = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        Assert.Equal(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);

        var (appSource, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, libA.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [libA]);
        var caller = Compile("App", [Tree(appSource)], [Platform.Value.Reference, libB.Compilation.ToMetadataReference()], Options);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [libA, app]);

        var result = universe.ResolveDefining(Target(caller, "N.Lib", "Get"), null, caller);
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation, result.Status);
    }

    [Fact]
    public async Task Resolver_RefusesForeignIdentityEqualTargetInstance()
    {
        // The target is a symbol from libB; the universe holds libA and the caller references
        // libA. Both compilations share an identical metadata identity but are distinct live
        // instances. Documents whether metadata similarity alone can bind a foreign instance.
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var libB = Create("LibB/LibB.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);

        var (appSource, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, libA.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [libA]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [libA, app]);

        var foreignTarget = Target(libB.Compilation, "N.Lib", "Get");
        var result = universe.ResolveDefining(foreignTarget, libA.Descriptor.DescriptorDigest, app.Compilation);
        output.WriteLine("foreign-instance status=" + result.Status);
        output.WriteLine("bound to libA=" + (result.Defining is not null &&
            ReferenceEquals(result.Defining, libA)));
        // Documented observation, not an endorsement: the accepted frozen R2 authority
        // (AdrCi1Exp.Shared.CompilationUniverse.Verify/Binds) binds this case the same way
        // (its direct branch is identity equality, which is weaker than the production
        // ReferenceEquals direct branch; both share the metadata-identity cross branch).
        // Pinned as a regression guard and reported as a QUALIFICATION for G-MINT.
        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        Assert.True(ReferenceEquals(result.Defining, libA));

        // Corroboration: the accepted frozen R2 authority reaches the same Complete verdict.
        var r2Scope = Frozen.AnalysisScope.Declare(Repo,
        [
            new Frozen.AnalysisScopeUnit("Lib/Lib.csproj", "Lib/Lib.csproj"),
            new Frozen.AnalysisScopeUnit("App/App.csproj", "App/App.csproj"),
        ]);
        var r2Universe = Frozen.CompilationUniverse.Create(Repo, r2Scope,
            new Frozen.UniverseEntryInput("Lib/Lib.csproj", libA.Compilation),
            new Frozen.UniverseEntryInput("App/App.csproj", app.Compilation));
        var r2Result = r2Universe.Verify(foreignTarget, app.Compilation);
        output.WriteLine("frozen-r2 foreign-instance status=" + r2Result.Status);
        Assert.Equal(Frozen.UniverseContextStatus.Complete, r2Result.Status);
    }

    [Fact]
    public async Task Resolver_RefusesAmbiguousTwoLiveBindings()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var libB = Create("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)],
                [Platform.Value.Reference, libA.Compilation.ToMetadataReference(), libB.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [libA, libB]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Lib2/Lib2.csproj", "Lib2/Lib2.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [libA, libB, app]);

        Assert.Equal(VerifiedDefiningStatus.Ambiguous,
            universe.ResolveDefining(Target(app.Compilation, "N.Lib", "Get"), null, app.Compilation).Status);
        // Precedence: ambiguity is decided before the recorded-context comparison.
        Assert.Equal(VerifiedDefiningStatus.Ambiguous,
            universe.ResolveDefining(Target(app.Compilation, "N.Lib", "Get"),
                "compctx2:sha256:" + new string('0', 64), app.Compilation).Status);
    }

    [Fact]
    public async Task Resolver_RefusesIncompleteUniverse_BeforeBinding()
    {
        var (universeBase, target, caller, lib) = await TwoProjectUniverse();
        // Rebuild with a declared-but-unresolved unit, and with a refusal recorded.
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var incomplete = VerifiedCSharpUniverse.Create(Repo, scope,
            universeBase.Entries, ["UnverifiedSource:src/X.cs"]);
        Assert.False(incomplete.IsComplete);
        // A target that would be missing is still classified Incomplete (precedence).
        var unrelated = Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options);
        Assert.Equal(VerifiedDefiningStatus.Incomplete,
            incomplete.ResolveDefining(Target(unrelated, "U.U", "M"), null, unrelated).Status);
        Assert.Equal(VerifiedDefiningStatus.Incomplete,
            incomplete.ResolveDefining(target, lib.Descriptor.DescriptorDigest, caller).Status);
    }

    [Fact]
    public async Task Resolver_RefusesMissingDefiningCompilation()
    {
        var (universe, _, _, _) = await TwoProjectUniverse();
        var unrelated = Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options);
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            universe.ResolveDefining(Target(unrelated, "U.U", "M"), null, unrelated).Status);
    }

    [Fact]
    public async Task Resolver_RefusesExternalUnitAtConstruction()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Other/Other.csproj", "Other/Other.csproj", ["Other/Other.cs"])]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create(Repo, scope, [lib]));
    }

    [Fact]
    public async Task Resolver_DoesNotMintAsmctxOrPersistAssociation()
    {
        var (universe, target, caller, lib) = await TwoProjectUniverse();
        var result = universe.ResolveDefining(target, lib.Descriptor.DescriptorDigest, caller);
        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        // The resolution exposes only a mapped compctx1; no asmctx3/usage4 is produced, and the
        // result carries no persistence state.
        Assert.NotNull(result.Compctx1);
        Assert.StartsWith("compctx1:sha256:", result.Compctx1!.DescriptorDigest);
        Assert.DoesNotContain("asmctx3", result.Compctx1.ComputeDigest());
        Assert.DoesNotContain("usage4", result.Compctx1.ComputeDigest());
        Assert.Null(typeof(VerifiedDefiningResolution).GetProperty("Persisted"));
        Assert.Null(typeof(VerifiedDefiningResolution).GetProperty("Association"));
    }

    // ------------------------------------------------------------------- helpers

    private async Task<(VerifiedCSharpUniverse Universe, IMethodSymbol Target, CSharpCompilation Caller, VerifiedCSharpCompilation Lib)>
        TwoProjectUniverse()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", "namespace N; public static class Lib { public static int Get() => 1; }");
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", "namespace N; public static class App { public static int Run() => Lib.Get(); }");
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [lib]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [lib, app]);
        return (universe, Target(app.Compilation, "N.Lib", "Get"), app.Compilation, lib);
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    private static async Task<(VerifiedCSharpSource Source, byte[] Bytes)> Src(string path, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        Bytes[path] = bytes;
        var source = await VerifiedCSharpSource.ReadAsync(
            new CodeGraphSource(path, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
            CancellationToken.None);
        return (source, bytes);
    }

    private static SyntaxTree Tree(VerifiedCSharpSource source) =>
        CSharpSyntaxTree.ParseText(source.Text,
            new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, SourceCodeKind.Regular),
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
        VerifiedCSharpCompilation.Create(Repo, projectUnit, compilation, sources, metadata, dependencies,
            CancellationToken.None);
}
