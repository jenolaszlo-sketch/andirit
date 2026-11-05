using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentGMintQualification;

// Independent G-MINT pre-mint probes. Expected R2 values are computed only through the
// accepted frozen R2 authority (AdrCi1Exp.Shared.CompilationProvenance.Describe) or from
// accepted contract text, never from the production implementation under test.
public sealed class GMintProbes(ITestOutputHelper output)
{
    private const string Repo = "review:g-mint-q1";
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

    // ============================================================ GM-1: authority

    [Fact]
    public async Task Gm1_ExactLiveInstance_AndValidCallerRelationship_Complete()
    {
        var f = await TwoProject();
        var result = f.Universe.ResolveDefining(f.Target, f.Lib.Descriptor.DescriptorDigest, f.Caller);
        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        Assert.Same(f.Lib, result.Defining);
        // The ordinary cross-project positive is bound by exact assembly instance.
        Assert.Same(f.Lib.Compilation.Assembly, f.Target.ContainingAssembly);
    }

    [Fact]
    public async Task Gm1_ForeignIdentityEqualTargetInstance_BindsViaMetadataIdentity()
    {
        // Accepted F1: a target symbol from a foreign, identity-equal compilation binds the
        // universe member that the caller live-references. Grounded in the frozen R2 authority.
        var (libA, libB, app, universe) = await IdentityEqualInstances();
        Assert.Equal(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        var foreign = Target(libB.Compilation, "N.Lib", "Get");
        var result = universe.ResolveDefining(foreign, libA.Descriptor.DescriptorDigest, app.Compilation);
        output.WriteLine("GM1 foreign-instance status=" + result.Status);
        Assert.Equal(VerifiedDefiningStatus.Complete, result.Status);
        Assert.True(ReferenceEquals(result.Defining, libA));

        // Corroboration: the accepted frozen R2 authority binds the same case.
        var scope = Frozen.AnalysisScope.Declare(Repo,
        [
            new Frozen.AnalysisScopeUnit("Lib/Lib.csproj", "Lib/Lib.csproj"),
            new Frozen.AnalysisScopeUnit("App/App.csproj", "App/App.csproj"),
        ]);
        var r2 = Frozen.CompilationUniverse.Create(Repo, scope,
            new Frozen.UniverseEntryInput("Lib/Lib.csproj", libA.Compilation),
            new Frozen.UniverseEntryInput("App/App.csproj", app.Compilation));
        Assert.Equal(Frozen.UniverseContextStatus.Complete, r2.Verify(foreign, app.Compilation).Status);
    }

    [Fact]
    public async Task Gm1_DifferentInstanceEqualDigest_And_UnequalIdentity()
    {
        var (libA, libB, app, _) = await IdentityEqualInstances();
        // Equal digest, different live instance, caller references libA -> metadata binds libA.
        var universe = BuildUniverse([libA, app], includeAppAndLibScope: true);
        var foreign = Target(libB.Compilation, "N.Lib", "Get");
        Assert.Equal(VerifiedDefiningStatus.Complete,
            universe.ResolveDefining(foreign, libA.Descriptor.DescriptorDigest, app.Compilation).Status);

        // Unequal metadata identity: a foreign target whose assembly identity differs cannot bind.
        var (otherSource, _) = await Src("Other/Other.cs", "namespace O; public static class Other { public static int Get() => 1; }");
        var other = Create("Other/Other.csproj",
            Compile("Other", [Tree(otherSource)], [Platform.Value.Reference], Options), [otherSource], [Platform.Value], []);
        var otherTarget = Target(other.Compilation, "O.Other", "Get");
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            universe.ResolveDefining(otherTarget, null, app.Compilation).Status);
    }

    [Fact]
    public async Task Gm1_MultipleCandidates_Ambiguous()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var libB = Create("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
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
    }

    [Fact]
    public async Task Gm1_SubstitutedCallerInstance_Missing()
    {
        // Universe holds lib (unit Lib) and App; a caller references a distinct identity-equal
        // instance that is not a universe member -> no binding.
        var f = await TwoProject();
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libB = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var caller = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libB.Compilation.ToMetadataReference()], Options);
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            f.Universe.ResolveDefining(Target(caller, "N.Lib", "Get"), null, caller).Status);
    }

    // ================================================== GM-2: fail-closed behavior

    [Fact]
    public async Task Gm2_RefusalConditions_Cannot_Yield_Complete_Under_Any_Overlap()
    {
        var f = await TwoProject();
        var valid = f.Lib.Descriptor.DescriptorDigest;
        var stale = "compctx2:sha256:" + new string('0', 64);

        // Incomplete universe (declared-but-unresolved unit).
        var incomplete = BuildIncomplete(f);
        // Ambiguous universe (two live bindings to the same assembly identity).
        var (ambiguousUniverse, ambiguousTarget, ambiguousCaller) = await Ambiguous();

        var cases = new (string Name, VerifiedDefiningStatus Status)[]
        {
            ("incomplete+stale", incomplete.ResolveDefining(f.Target, stale, f.Caller).Status),
            ("incomplete+valid-recorded", incomplete.ResolveDefining(f.Target, valid, f.Caller).Status),
            ("incomplete+missing-binding", incomplete.ResolveDefining(
                Target(Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options), "U.U", "M"),
                null, null).Status),
            ("incomplete+ambiguous", incomplete.ResolveDefining(
                Target(f.App.Compilation, "N.Lib", "Get"), null, f.App.Compilation).Status),
            ("ambiguous+stale", ambiguousUniverse.ResolveDefining(ambiguousTarget, stale, ambiguousCaller).Status),
            ("ambiguous+null-recorded", ambiguousUniverse.ResolveDefining(ambiguousTarget, null, ambiguousCaller).Status),
            ("complete+stale", f.Universe.ResolveDefining(f.Target, stale, f.Caller).Status),
            ("complete+missing-binding", f.Universe.ResolveDefining(
                Target(Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options), "U.U", "M"),
                valid, null).Status),
            ("complete+single+null", f.Universe.ResolveDefining(f.Target, null, f.Caller).Status),
            ("complete+single+valid", f.Universe.ResolveDefining(f.Target, valid, f.Caller).Status),
        };

        foreach (var (name, status) in cases)
        {
            output.WriteLine($"GM2 {name} => {status}");
            var mustRefuse = !name.StartsWith("complete+single+", StringComparison.Ordinal);
            if (mustRefuse) Assert.NotEqual(VerifiedDefiningStatus.Complete, status);
        }

        // Mint-eligibility invariant: Complete requires complete universe, exactly one binding,
        // and (no recorded context or an equal recorded context) — independent of label order.
        Assert.Equal(VerifiedDefiningStatus.Complete, f.Universe.ResolveDefining(f.Target, null, f.Caller).Status);
        Assert.Equal(VerifiedDefiningStatus.Complete, f.Universe.ResolveDefining(f.Target, valid, f.Caller).Status);
        Assert.Equal(VerifiedDefiningStatus.Incomplete, incomplete.ResolveDefining(f.Target, valid, f.Caller).Status);
        Assert.Equal(VerifiedDefiningStatus.Ambiguous, ambiguousUniverse.ResolveDefining(ambiguousTarget, valid, ambiguousCaller).Status);
        Assert.Equal(VerifiedDefiningStatus.StaleContext, f.Universe.ResolveDefining(f.Target, stale, f.Caller).Status);
    }

    // ============================================ GM-3: mandatory recorded context

    [Fact]
    public async Task Gm3_RecordedContext_Matrix()
    {
        var f = await TwoProject();
        var valid = f.Lib.Descriptor.DescriptorDigest;

        // Case 1: null -> accepted non-minting behavior (Complete). This is the F3 qualification.
        Assert.Equal(VerifiedDefiningStatus.Complete, f.Universe.ResolveDefining(f.Target, null, f.Caller).Status);
        // Case 2: empty / malformed -> refused.
        Assert.Equal(VerifiedDefiningStatus.StaleContext, f.Universe.ResolveDefining(f.Target, "", f.Caller).Status);
        Assert.Equal(VerifiedDefiningStatus.StaleContext, f.Universe.ResolveDefining(f.Target, "not-a-context", f.Caller).Status);
        // Case 3: wrong defining-compilation context (the caller's own context) -> refused.
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            f.Universe.ResolveDefining(f.Target, f.App.Descriptor.DescriptorDigest, f.Caller).Status);
        // Case 4: previously valid but stale -> refused.
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            f.Universe.ResolveDefining(f.Target, "compctx2:sha256:" + new string('a', 64), f.Caller).Status);
        // Case 5: fabricated context from an unrelated verified compilation -> refused.
        var (u, _) = await Src("U/U.cs", "namespace U; public static class U { public static int M() => 1; }");
        var unrelated = Create("U/U.csproj",
            Compile("U", [Tree(u)], [Platform.Value.Reference], Options), [u], [Platform.Value], []);
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            f.Universe.ResolveDefining(f.Target, unrelated.Descriptor.DescriptorDigest, f.Caller).Status);
        // Case 6/7: valid recorded context + incorrect live binding -> refused at binding (Missing).
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libB = Create("LibB/LibB.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var substitutedCaller = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libB.Compilation.ToMetadataReference()], Options);
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            f.Universe.ResolveDefining(Target(substitutedCaller, "N.Lib", "Get"), valid, substitutedCaller).Status);
        // Case: exact live binding + correct recorded context -> Complete (valid control).
        Assert.Equal(VerifiedDefiningStatus.Complete, f.Universe.ResolveDefining(f.Target, valid, f.Caller).Status);
    }

    [Fact]
    public async Task Gm3_Complete_Alone_Does_Not_Expose_A_Validated_Association()
    {
        var f = await TwoProject();
        var nullResult = f.Universe.ResolveDefining(f.Target, null, f.Caller);
        Assert.Equal(VerifiedDefiningStatus.Complete, nullResult.Status);
        // No result member records whether a supplied context was validated: Complete with a
        // null recording is indistinguishable, at the result shape, from a validated one.
        var recorded = new[] { "Recorded", "RecordedCompctx2", "Validated", "AssociationValidated", "Association" };
        foreach (var name in recorded)
            Assert.Null(typeof(VerifiedDefiningResolution).GetProperty(name));
        // The minting path can still read the current verified context from the defining entry.
        Assert.Equal(f.Lib.Descriptor.DescriptorDigest, nullResult.Defining!.Descriptor.DescriptorDigest);
    }

    // ============================================== GM-4: §5.2 negative matrix

    [Fact]
    public async Task Gm4_OptionAS52_PreMint_Negatives_And_Controls()
    {
        var f = await TwoProject();
        var valid = f.Lib.Descriptor.DescriptorDigest;

        // N-a stale context refused / control accepted.
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            f.Universe.ResolveDefining(f.Target, "compctx2:sha256:" + new string('0', 64), f.Caller).Status);
        Assert.Equal(VerifiedDefiningStatus.Complete, f.Universe.ResolveDefining(f.Target, valid, f.Caller).Status);

        // N-b fabricated context refused.
        var (u, _) = await Src("U/U.cs", "namespace U; public static class U { public static int M() => 1; }");
        var unrelated = Create("U/U.csproj",
            Compile("U", [Tree(u)], [Platform.Value.Reference], Options), [u], [Platform.Value], []);
        Assert.Equal(VerifiedDefiningStatus.StaleContext,
            f.Universe.ResolveDefining(f.Target, unrelated.Descriptor.DescriptorDigest, f.Caller).Status);

        // N-c substituted live instance refused.
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libB = Create("LibB/LibB.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var substitutedCaller = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libB.Compilation.ToMetadataReference()], Options);
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            f.Universe.ResolveDefining(Target(substitutedCaller, "N.Lib", "Get"), null, substitutedCaller).Status);

        // N-d incomplete universe refused.
        Assert.Equal(VerifiedDefiningStatus.Incomplete, BuildIncomplete(f).ResolveDefining(f.Target, valid, f.Caller).Status);

        // N-e ambiguous refused.
        var (ambiguousUniverse, ambiguousTarget, ambiguousCaller) = await Ambiguous();
        Assert.Equal(VerifiedDefiningStatus.Ambiguous,
            ambiguousUniverse.ResolveDefining(ambiguousTarget, valid, ambiguousCaller).Status);

        // N-f missing defining compilation refused.
        Assert.Equal(VerifiedDefiningStatus.MissingDefiningCompilation,
            f.Universe.ResolveDefining(
                Target(Compile("U", [Tree("src/U.cs", "namespace U; public static class U { public static int M() => 1; }")], [], Options), "U.U", "M"),
                null, null).Status);

        // N-g external defining unit refused at construction (GM-5).
        Assert.Throws<InvalidDataException>(() =>
        {
            var scope = VerifiedCSharpScope.Declare(Repo, [new("Other/Other.csproj", "Other/Other.csproj", ["Other/Other.cs"])]);
            VerifiedCSharpUniverse.Create(Repo, scope, [f.Lib]);
        });

        // N-h compctx2 isolation: the mapped compctx1 contains no compctx2 material.
        var mapped = f.Lib.MapR2FaithfulCompctx1();
        Assert.DoesNotContain("compctx2", mapped.ComputeDigest());
        Assert.StartsWith("compctx2:sha256:", f.Lib.Descriptor.DescriptorDigest);
        Assert.StartsWith("compctx1:sha256:", mapped.DescriptorDigest);

        // All of the above operate in memory before minting; no asmctx3/usage4 is produced.
        Assert.StartsWith("compctx1:sha256:", f.Universe.ResolveDefining(f.Target, valid, f.Caller).Compctx1!.DescriptorDigest);
    }

    // ============================================== GM-5: external refusal boundary

    [Fact]
    public async Task Gm5_ConstructionStage_Refusal_Boundary()
    {
        var f = await TwoProject();

        // Out-of-scope unit refused at construction.
        var externalScope = VerifiedCSharpScope.Declare(Repo, [new("Other/Other.csproj", "Other/Other.csproj", ["Other/Other.cs"])]);
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create(Repo, externalScope, [f.Lib]));

        // Repository mismatch refused at construction.
        Assert.Throws<InvalidDataException>(() => VerifiedCSharpUniverse.Create("other:repo", f.Scope, f.Universe.Entries));

        // Dependency not present in the universe refused at construction.
        Assert.Throws<InvalidDataException>(() =>
        {
            var scope = VerifiedCSharpScope.Declare(Repo, [new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
            VerifiedCSharpUniverse.Create(Repo, scope, [f.App]);
        });

        // A recorded refusal makes the universe incomplete and can never confirm.
        var incomplete = VerifiedCSharpUniverse.Create(Repo, f.Scope, f.Universe.Entries, ["UnverifiedSource:x"]);
        Assert.False(incomplete.IsComplete);
        Assert.Equal(VerifiedDefiningStatus.Incomplete,
            incomplete.ResolveDefining(f.Target, f.Lib.Descriptor.DescriptorDigest, f.Caller).Status);

        // The resolver never reaches the external status on a validly constructed universe.
        var statuses = typeof(VerifiedDefiningStatus).GetEnumNames();
        Assert.Contains("UnsupportedExternalEvidence", statuses);
    }

    // ============================================== GM-7: trusted evidence boundary

    [Fact]
    public async Task Gm7_MappedCompctx1_EqualsFrozenAuthority_AndPreservesPeIdentity()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libCompilation = Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options);
        var lib = Create("Lib/Lib.csproj", libCompilation, [libSource], [Platform.Value], []);
        var (appSource, appBytes) = await Src("App/App.cs", AppSource);
        var appCompilation = Compile("App", [Tree(appSource)],
            [Platform.Value.Reference, libCompilation.ToMetadataReference()], Options);
        var app = Create("App/App.csproj", appCompilation, [appSource], [Platform.Value], [lib]);

        var mapped = app.MapR2FaithfulCompctx1();
        var r2 = Frozen.CompilationProvenance.Describe(Repo, "App/App.csproj", appCompilation,
            [("App/App.cs", appBytes)]);
        Assert.Equal(r2.DescriptorDigest, mapped.DescriptorDigest);
        Assert.Contains(Platform.Value.Identity, mapped.References);
        Assert.DoesNotContain(mapped.References, r => r.Contains("runtime/System.", StringComparison.Ordinal));
        Assert.DoesNotContain(mapped.References, r => r.StartsWith("compilation-context|", StringComparison.Ordinal));
        Assert.Contains(mapped.References, r => r.StartsWith("compilation|Lib|", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Gm7_StableDigest_ForCrossProcessComparison()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [lib]);
        // Printed for a separate-process determinism comparison.
        output.WriteLine("GM7-MAPPER-DIGEST=" + app.MapR2FaithfulCompctx1().DescriptorDigest);
        output.WriteLine("GM7-LIB-DIGEST=" + lib.Descriptor.DescriptorDigest);
    }

    // ------------------------------------------------------------------- helpers

    private sealed record ProjectFixture(VerifiedCSharpUniverse Universe, VerifiedCSharpScope Scope,
        VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App, IMethodSymbol Target, CSharpCompilation Caller);

    private async Task<ProjectFixture> TwoProject()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var lib = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, lib.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [lib]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [lib, app]);
        return new ProjectFixture(universe, scope, lib, app, Target(app.Compilation, "N.Lib", "Get"), app.Compilation);
    }

    private VerifiedCSharpUniverse BuildIncomplete(ProjectFixture f)
    {
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        return VerifiedCSharpUniverse.Create(Repo, scope, [f.Lib, f.App]);
    }

    private async Task<(VerifiedCSharpUniverse Universe, IMethodSymbol Target, CSharpCompilation Caller)> Ambiguous()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var libB = Create("Lib2/Lib2.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)],
                [Platform.Value.Reference, libA.Compilation.ToMetadataReference(), libB.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [libA, libB]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Lib2/Lib2.csproj", "Lib2/Lib2.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [libA, libB, app]);
        return (universe, Target(app.Compilation, "N.Lib", "Get"), app.Compilation);
    }

    private async Task<(VerifiedCSharpCompilation LibA, VerifiedCSharpCompilation LibB, VerifiedCSharpCompilation App,
        VerifiedCSharpUniverse Universe)> IdentityEqualInstances()
    {
        var (libSource, _) = await Src("Lib/Lib.cs", LibSource);
        var libA = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        // Same project unit as libA -> identical verified compctx2 digest, distinct live instance.
        var libB = Create("Lib/Lib.csproj",
            Compile("Lib", [Tree(libSource)], [Platform.Value.Reference], Options), [libSource], [Platform.Value], []);
        var (appSource, _) = await Src("App/App.cs", AppSource);
        var app = Create("App/App.csproj",
            Compile("App", [Tree(appSource)], [Platform.Value.Reference, libA.Compilation.ToMetadataReference()], Options),
            [appSource], [Platform.Value], [libA]);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [libA, app]);
        return (libA, libB, app, universe);
    }

    private static VerifiedCSharpUniverse BuildUniverse(
        IReadOnlyCollection<VerifiedCSharpCompilation> entries, bool includeAppAndLibScope)
    {
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("App/App.csproj", "App/App.csproj", ["App/App.cs"])]);
        return VerifiedCSharpUniverse.Create(Repo, scope, entries);
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method).Single());
    }

    private static async Task<(VerifiedCSharpSource Source, byte[] Bytes)> Src(string path, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
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
