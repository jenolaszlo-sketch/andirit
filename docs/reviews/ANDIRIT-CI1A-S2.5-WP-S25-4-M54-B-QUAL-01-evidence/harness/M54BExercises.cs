// M54-B-QUAL-01 independent reviewer harness — pre-registered exercises.
// Exercises the REAL production refusal path (`VerifiedCSharpUniverse.ResolveDefining`
// / `MintTarget` / `MintUsage`), the REAL extractor accounting
// (`VerifiedCSharpOccurrenceExtractor.Extract`) and the REAL native store chain
// (publish -> close -> distinct-process reopen -> query). Expectations are derived
// from the amended M5.4 contract (`adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6),
// never from implementation output. Constructed controls are labeled as such.
using System.Text.Json;
using Andirit;
using LatticeDbSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace M54B;

public sealed class M54BExercises
{
    // ---------------------------------------------------------------- fixtures

    private static readonly VerifiedMetadataReference Platform = UniverseFactory.CapturePlatform();
    private static readonly DateTimeOffset Started = new(2026, 11, 21, 0, 0, 0, TimeSpan.Zero);
    private static readonly CodePluginId Plugin = new("plugin:m54b");

    private static VerifiedCSharpCompilation LibEntry() => UniverseFactory.CreateVerified(
        "Lib/Lib.csproj",
        UniverseFactory.Compile("Lib",
            [UniverseFactory.Tree("Lib/Lib.cs", "namespace N { public static class Lib { public static int Get() => 7; } }")],
            [Platform.Reference]),
        [UniverseFactory.ReadTextSource("Lib/Lib.cs", "namespace N { public static class Lib { public static int Get() => 7; } }")],
        [Platform], []);

    // App calls Lib.Get (in-universe), Console.WriteLine (PE-external),
    // Console.Out (unsupported category) and Missing.Method (error recovery).
    private const string AppSource =
        "using System; namespace N { public static class App { public static int Run() { Missing.Method(); Console.WriteLine(\"x\"); var c = Console.Out; return Lib.Get(); } } }";

    private static (VerifiedCSharpCompilation App, CSharpCompilation Compilation, VerifiedCSharpCompilation Lib)
        AppOverLib()
    {
        var lib = LibEntry();
        var console = UniverseFactory.CaptureConsole();
        var source = UniverseFactory.ReadTextSource("App/App.cs", AppSource);
        var compilation = UniverseFactory.Compile("App",
            [UniverseFactory.Tree(source)],
            [Platform.Reference, console.Reference, lib.Compilation.ToMetadataReference()]);
        var app = UniverseFactory.CreateVerified("App/App.csproj", compilation, [source],
            [Platform, console], [lib]);
        return (app, compilation, lib);
    }

    private static VerifiedCSharpUniverse UniverseOf(params VerifiedCSharpCompilation[] entries)
    {
        var scopeUnits = entries.Select(e => (
            e.Descriptor.ProjectUnitId,
            e.Descriptor.ProjectUnitId + ".csproj",
            e.Sources.Select(s => s.Path).ToArray())).ToArray();
        var scope = UniverseFactory.DeclareScope(scopeUnits);
        return UniverseFactory.CreateUniverse(scope, entries);
    }

    private static IMethodSymbol LibGet(CSharpCompilation caller) =>
        caller.Method("N.Lib", "Get");

    private static string Recorded(VerifiedCSharpCompilation entry) =>
        entry.Descriptor.DescriptorDigest;

    // ---------------------------------------------------------------- receipts

    private static void Emit(string name, object payload) => Receipt.Emit(name, payload);

    private static string Describe(M54BHarnessStatus status) => status.ToString();

    private enum M54BHarnessStatus
    {
        Complete,
        Incomplete,
        MissingDefiningCompilation,
        UnsupportedExternalEvidence,
        Ambiguous,
        StaleContext,
        MissingRecordedContext,
        Minted,
    }

    private static M54BHarnessStatus Map(VerifiedDefiningStatus status) => status switch
    {
        VerifiedDefiningStatus.Complete => M54BHarnessStatus.Complete,
        VerifiedDefiningStatus.Incomplete => M54BHarnessStatus.Incomplete,
        VerifiedDefiningStatus.MissingDefiningCompilation => M54BHarnessStatus.MissingDefiningCompilation,
        VerifiedDefiningStatus.UnsupportedExternalEvidence => M54BHarnessStatus.UnsupportedExternalEvidence,
        VerifiedDefiningStatus.Ambiguous => M54BHarnessStatus.Ambiguous,
        VerifiedDefiningStatus.StaleContext => M54BHarnessStatus.StaleContext,
        _ => throw new InvalidOperationException("unmapped defining status " + status),
    };

    private static M54BHarnessStatus Map(VerifiedTargetMintStatus status) => status switch
    {
        VerifiedTargetMintStatus.Minted => M54BHarnessStatus.Minted,
        VerifiedTargetMintStatus.Incomplete => M54BHarnessStatus.Incomplete,
        VerifiedTargetMintStatus.MissingRecordedContext => M54BHarnessStatus.MissingRecordedContext,
        VerifiedTargetMintStatus.MissingDefiningCompilation => M54BHarnessStatus.MissingDefiningCompilation,
        VerifiedTargetMintStatus.UnsupportedExternalEvidence => M54BHarnessStatus.UnsupportedExternalEvidence,
        VerifiedTargetMintStatus.Ambiguous => M54BHarnessStatus.Ambiguous,
        VerifiedTargetMintStatus.StaleContext => M54BHarnessStatus.StaleContext,
        _ => throw new InvalidOperationException("unmapped mint status " + status),
    };

    // ---------------------------------------------------------------- E1: isolated refusal

    [Fact]
    public void E1a_IsolatedMissingDefiningCompilation_RefusesWithoutMint()
    {
        var app = UniverseFactory.CreateVerified(
            "App/App.csproj",
            UniverseFactory.Compile("App",
                [UniverseFactory.Tree("App/App.cs", "namespace N { public static class App { public static int Run() => 42; } }")],
                [Platform.Reference]),
            [UniverseFactory.ReadTextSource("App/App.cs", "namespace N { public static class App { public static int Run() => 42; } }")],
            [Platform], []);
        var universe = UniverseFactory.CreateUniverse(
            UniverseFactory.DeclareScope(("App/App.csproj", "App/App.csproj", ["App/App.cs"])), [app]);

        // Constructed control: the target's defining compilation is deliberately
        // absent from the declared universe.
        var outside = UniverseFactory.Compile("Outside",
            [UniverseFactory.Tree("Outside/Outside.cs", "namespace N { public static class Outside { public static int Get() => 1; } }")],
            [Platform.Reference]);
        var target = outside.Method("N.Outside", "Get");

        var resolved = universe.ResolveDefining(target, null, null);
        Assert.Equal(M54BHarnessStatus.MissingDefiningCompilation, Map(resolved.Status));
        Assert.Contains("defining assembly", resolved.Detail, StringComparison.Ordinal);

        var mint = universe.MintTarget(target, "compctx2:sha256:" + new string('a', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), null);
        Assert.False(mint.IsMinted);
        Assert.Equal(M54BHarnessStatus.MissingDefiningCompilation, Map(mint.Status));

        var usage = universe.MintUsage(new VerifiedUsageInput(
            new CodeRepositoryId(UniverseFactory.RepositoryId), Plugin, target,
            "compctx2:sha256:" + new string('a', 64), "App/App.csproj",
            new CodeLocation("App/App.cs", 1, 20, 1, 30), "sha256:source", "sha256:content",
            UniverseFactory.TargetDocument(target),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape")));
        Assert.False(usage.IsMinted);
        Assert.Null(usage.Fact);

        // Natural construction guards: RequireCompilation refuses a non-member and
        // an incomplete universe.
        var missing = Assert.Throws<InvalidDataException>(() => universe.RequireCompilation(outside));
        Assert.Contains("MissingDefiningCompilation", missing.Message, StringComparison.Ordinal);

        Emit("e1a", new
        {
            classification = "constructed control (deliberately absent defining compilation)",
            resolvedStatus = Describe(Map(resolved.Status)),
            resolvedDetail = resolved.Detail,
            mintStatus = Describe(Map(mint.Status)),
            minted = mint.IsMinted,
            usageMinted = usage.IsMinted,
            usageFactEmitted = usage.Fact is not null,
            requireCompilationRefusal = missing.Message,
        });
    }

    [Fact]
    public void E1b_UniverseAndCompilationConstructionGuards_BlockMissingDefiningCompilation()
    {
        var (app, _, _) = AppOverLib();

        // Natural guard 1 (compilation creation): an unmatched project reference
        // never becomes a verified compilation. The reference counts balance, so
        // the refusal is the project-reference membership guard itself.
        var lib = LibEntry();
        var other = UniverseFactory.CreateVerified("Other/Other.csproj",
            UniverseFactory.Compile("Other",
                [UniverseFactory.Tree("Other/Other.cs", "namespace N { public static class Other { public static int Get() => 1; } }")],
                [Platform.Reference]),
            [UniverseFactory.ReadTextSource("Other/Other.cs", "namespace N { public static class Other { public static int Get() => 1; } }")],
            [Platform], []);
        var appSource = UniverseFactory.ReadTextSource("App/App.cs", "namespace N { public static class App { public static int Run() => Other.Get(); } }");
        var appCompilation = UniverseFactory.Compile("App",
            [UniverseFactory.Tree(appSource)],
            [Platform.Reference, other.Compilation.ToMetadataReference()]);
        var unmatched = Assert.Throws<InvalidDataException>(() =>
            UniverseFactory.CreateVerified("App/App.csproj", appCompilation, [appSource], [Platform], [lib]));
        Assert.Contains("MissingDefiningCompilation", unmatched.Message, StringComparison.Ordinal);

        // Natural guard 2 (universe creation): a universe member may not depend on
        // a compilation that is not itself a universe member.
        var scope = UniverseFactory.DeclareScope(
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]));
        var excludedDependency = Assert.Throws<InvalidDataException>(() =>
            UniverseFactory.CreateUniverse(scope, [app]));
        Assert.Contains("MissingDefiningCompilation", excludedDependency.Message, StringComparison.Ordinal);

        Emit("e1b", new
        {
            classification = "natural production guards (constructed inputs)",
            unmatchedProjectReferenceRefusal = unmatched.Message,
            nonMemberDependencyRefusal = excludedDependency.Message,
            consequence = "a complete admissible universe cannot carry a member depending on an unverified compilation",
        });
    }

    [Fact]
    public void E1c_IncompleteUniverse_DistinctFromMissingDefining()
    {
        var appEntry = UniverseFactory.CreateVerified(
            "App/App.csproj",
            UniverseFactory.Compile("App",
                [UniverseFactory.Tree("App/App.cs", "namespace N { public static class App { public static int Run() => 42; } }")],
                [Platform.Reference]),
            [UniverseFactory.ReadTextSource("App/App.cs", "namespace N { public static class App { public static int Run() => 42; } }")],
            [Platform], []);
        var scope = UniverseFactory.DeclareScope(
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]));

        // Missing scope unit -> Incomplete (the declared-universe variant), not
        // the missing-defining reason.
        var incomplete = UniverseFactory.CreateUniverse(scope, [appEntry]);
        var target = appEntry.Compilation.Method("N.App", "Run");
        var resolved = incomplete.ResolveDefining(target, null, null);
        Assert.Equal(M54BHarnessStatus.Incomplete, Map(resolved.Status));
        Assert.DoesNotContain("MissingDefiningCompilation", resolved.Detail, StringComparison.Ordinal);

        // Empty universe entries -> Incomplete with the missing-unit detail.
        var empty = UniverseFactory.CreateUniverse(
            UniverseFactory.DeclareScope(("App/App.csproj", "App/App.csproj", ["App/App.cs"])), []);
        var emptyTarget = UniverseFactory.Compile("App",
            [UniverseFactory.Tree("App/App.cs", "namespace N { public static class App { public static int Run() => 42; } }")],
            [Platform.Reference]).Method("N.App", "Run");
        var emptyResolved = empty.ResolveDefining(emptyTarget, null, null);
        Assert.Equal(M54BHarnessStatus.Incomplete, Map(emptyResolved.Status));
        Assert.Contains("declared project units", emptyResolved.Detail, StringComparison.Ordinal);

        // The mint gate refuses the same way.
        var mint = incomplete.MintTarget(target, "compctx2:sha256:" + new string('c', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), null);
        Assert.False(mint.IsMinted);
        Assert.Equal(M54BHarnessStatus.Incomplete, Map(mint.Status));
        var guard = Assert.Throws<InvalidDataException>(() => incomplete.RequireCompilation(appEntry.Compilation));
        Assert.Contains("IncompleteCompilationUniverse", guard.Message, StringComparison.Ordinal);

        Emit("e1c", new
        {
            classification = "constructed controls (incomplete / empty declared universes)",
            missingScopeUnitStatus = Describe(Map(resolved.Status)),
            missingScopeUnitDetail = resolved.Detail,
            emptyUniverseStatus = Describe(Map(emptyResolved.Status)),
            emptyUniverseDetail = emptyResolved.Detail,
            mintStatus = Describe(Map(mint.Status)),
            requireCompilationRefusal = guard.Message,
        });
    }

    // ---------------------------------------------------------------- E2: mint gate screen

    [Fact]
    public void E2_MintGate_RefusesEveryNonCompleteStatus()
    {
        var (app, appCompilation, lib) = AppOverLib();
        var universe = UniverseOf(lib, app);
        var target = LibGet(appCompilation);

        // (a) No recorded defining context: never mints, even though the resolution
        //     itself would be Complete.
        var noContext = universe.MintTarget(target, null,
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), appCompilation);
        Assert.False(noContext.IsMinted);
        Assert.Equal(M54BHarnessStatus.MissingRecordedContext, Map(noContext.Status));
        var blankContext = universe.MintTarget(target, "   ",
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), appCompilation);
        Assert.False(blankContext.IsMinted);
        Assert.Equal(M54BHarnessStatus.MissingRecordedContext, Map(blankContext.Status));

        // (b) Stale recorded context.
        var stale = universe.MintTarget(target, "compctx2:sha256:" + new string('b', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), appCompilation);
        Assert.False(stale.IsMinted);
        Assert.Equal(M54BHarnessStatus.StaleContext, Map(stale.Status));

        // (c) Missing defining compilation (deliberately absent defining assembly).
        var outside = UniverseFactory.Compile("Outside",
            [UniverseFactory.Tree("Outside/Outside.cs", "namespace N { public static class Outside { public static int Get() => 1; } }")],
            [Platform.Reference]);
        var missing = universe.MintTarget(outside.Method("N.Outside", "Get"),
            "compctx2:sha256:" + new string('a', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), null);
        Assert.False(missing.IsMinted);
        Assert.Equal(M54BHarnessStatus.MissingDefiningCompilation, Map(missing.Status));

        // (d) Incomplete universe (standalone member so the universe can carry a
// // missing scope unit without tripping the dependency-membership guard).
        var standalone = SharedFixture.StandaloneApp();
        var incompleteScope = UniverseFactory.DeclareScope(
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]));
        var incomplete = UniverseFactory.CreateUniverse(incompleteScope, [standalone]);
        var standaloneTarget = standalone.Compilation.Method("N.App", "Run");
        var incompleteMint = incomplete.MintTarget(standaloneTarget,
            "compctx2:sha256:" + new string('c', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), standalone.Compilation);
        Assert.False(incompleteMint.IsMinted);
        Assert.Equal(M54BHarnessStatus.Incomplete, Map(incompleteMint.Status));

        // (e) Runtime-forwarded target: PE-only evidence -> unsupported channel.
        var uriReference = VerifiedMetadataReference.Capture(typeof(Uri).Assembly.Location, CancellationToken.None);
        var fwdSource = UniverseFactory.ReadTextSource("AppFwd/AppFwd.cs",
            "using System; namespace N { public static class AppFwd { public static string Run() => new Uri(\"x\").ToString(); } } }");
        var fwdCompilation = UniverseFactory.Compile("AppFwd",
            [UniverseFactory.Tree(fwdSource)], [Platform.Reference, uriReference.Reference]);
        var fwdEntry = UniverseFactory.CreateVerified("AppFwd/AppFwd.csproj", fwdCompilation, [fwdSource],
            [Platform, uriReference], []);
        var fwdUniverse = UniverseFactory.CreateUniverse(
            UniverseFactory.DeclareScope(("AppFwd/AppFwd.csproj", "AppFwd/AppFwd.csproj", ["AppFwd/AppFwd.cs"])), [fwdEntry]);
        var forwarded = fwdCompilation.Method("System.Uri", "ToString");
        var external = fwdUniverse.MintTarget(forwarded, "compctx2:sha256:" + new string('d', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), fwdCompilation);
        Assert.False(external.IsMinted);
        Assert.Equal(M54BHarnessStatus.UnsupportedExternalEvidence, Map(external.Status));
        var externalResolution = fwdUniverse.ResolveDefining(forwarded, null, fwdCompilation);
        Assert.Equal(M54BHarnessStatus.UnsupportedExternalEvidence, Map(externalResolution.Status));
        Assert.Contains("PE metadata", externalResolution.Detail, StringComparison.Ordinal);

        // (f) Ambiguous: two identity-equal instances of the same source in one
        //     universe (constructed ambiguity).
        var libSource = UniverseFactory.ReadTextSource("Lib/Lib.cs", "namespace N { public static class Lib { public static int Get() => 7; } }");
        var l1 = UniverseFactory.CreateVerified("Lib1/Lib.csproj",
            UniverseFactory.Compile("Lib", [UniverseFactory.Tree(libSource)], [Platform.Reference]), [libSource], [Platform], []);
        var l2 = UniverseFactory.CreateVerified("Lib2/Lib.csproj",
            UniverseFactory.Compile("Lib", [UniverseFactory.Tree(libSource)], [Platform.Reference]), [libSource], [Platform], []);
        var ambSource = UniverseFactory.ReadTextSource("Amb/Amb.cs",
            "namespace N { public static class Amb { public static int Run() => Lib.Get(); } }");
        var ambCompilation = UniverseFactory.Compile("Amb",
            [UniverseFactory.Tree(ambSource)],
            [Platform.Reference, l1.Compilation.ToMetadataReference(), l2.Compilation.ToMetadataReference()]);
        var ambEntry = UniverseFactory.CreateVerified("Amb/Amb.csproj", ambCompilation, [ambSource], [Platform], [l1, l2]);
        var ambUniverse = AmbiguousUniverse(l1, l2, ambEntry);
        var ambiguous = ambUniverse.ResolveDefining(LibGet(ambCompilation), null, ambCompilation);
        Assert.Equal(M54BHarnessStatus.Ambiguous, Map(ambiguous.Status));
        var ambiguousMint = ambUniverse.MintTarget(LibGet(ambCompilation), "compctx2:sha256:" + new string('e', 64),
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), ambCompilation);
        Assert.False(ambiguousMint.IsMinted);
        Assert.Equal(M54BHarnessStatus.Ambiguous, Map(ambiguousMint.Status));

        Emit("e2", new
        {
            classification = "constructed controls + natural guards",
            missingRecordedContext = new[] { Describe(Map(noContext.Status)), Describe(Map(blankContext.Status)) },
            staleContext = Describe(Map(stale.Status)),
            missingDefiningCompilation = Describe(Map(missing.Status)),
            incomplete = Describe(Map(incompleteMint.Status)),
            unsupportedExternalEvidence = Describe(Map(external.Status)),
            unsupportedDetail = externalResolution.Detail,
            ambiguous = Describe(Map(ambiguous.Status)),
            ambiguousDetail = ambiguous.Detail,
            mintedAny = new[]
            {
                noContext.IsMinted, blankContext.IsMinted, stale.IsMinted, missing.IsMinted,
                incompleteMint.IsMinted, external.IsMinted, ambiguousMint.IsMinted,
            }.Any(minted => minted),
        });
    }

    private static VerifiedCSharpUniverse AmbiguousUniverse(
        VerifiedCSharpCompilation l1, VerifiedCSharpCompilation l2, VerifiedCSharpCompilation amb)
    {
        var scope = UniverseFactory.DeclareScope(
            ("Lib1/Lib.csproj", "Lib1/Lib.csproj", ["Lib/Lib.cs"]),
            ("Lib2/Lib.csproj", "Lib2/Lib.csproj", ["Lib/Lib.cs"]),
            ("Amb/Amb.csproj", "Amb/Amb.csproj", ["Amb/Amb.cs"]));
        return UniverseFactory.CreateUniverse(scope, [l1, l2, amb]);
    }

    [Fact]
    public void E2p_PositiveControl_CompleteMint_MatchesIndependentlyComputedTargetId()
    {
        var (app, appCompilation, lib) = AppOverLib();
        var universe = UniverseOf(lib, app);
        var target = LibGet(appCompilation);

        var recorded = Recorded(lib);
        var mint = universe.MintTarget(target, recorded,
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"), appCompilation);
        Assert.True(mint.IsMinted);
        Assert.Equal(M54BHarnessStatus.Minted, Map(mint.Status));

        // Independent computation of the expected asmctx3 target id from the mapped
        // R2-faithful compctx1 and the caller-supplied component digests.
        var compctx1 = lib.MapR2FaithfulCompctx1();
        var expected = CodeOccurrenceIdentity.ComputeTargetId(
            compctx1.DescriptorDigest, compctx1.MetadataIdentity, compctx1.ModuleName,
            UniverseFactory.Digest("id"), UniverseFactory.Digest("decl"), UniverseFactory.Digest("shape"));
        Assert.Equal(expected, mint.TargetId);

        Emit("e2p", new
        {
            classification = "naturally reproduced positive control",
            mintStatus = Describe(Map(mint.Status)),
            targetId = mint.TargetId!.Value,
            independentlyComputedTargetId = expected.Value,
            targetIdMatches = expected == mint.TargetId,
        });
    }

    // ---------------------------------------------------------------- E3: integrated accounting

    [Fact]
    public void E3a_Extraction_UnresolvedNeverBecomesFact()
    {
        var (app, appCompilation, lib) = AppOverLib();
        var universe = UniverseOf(lib, app);
        var appSource = app.Sources[0];

        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(universe, new VerifiedOccurrenceExtractionRequest(
            new CodeRepositoryId(UniverseFactory.RepositoryId), Plugin, app,
            _ => UniverseFactory.Digest("id"),
            _ => UniverseFactory.Digest("decl"),
            _ => UniverseFactory.Digest("shape"),
            UniverseFactory.TargetDocument));

        Assert.Equal(1, extraction.Facts.Length); // only the in-universe call mints
        Assert.Equal(1, extraction.Coverage.External); // Console.WriteLine -> PE evidence
        Assert.Equal(1, extraction.Coverage.Unresolved); // Missing.Method -> error recovery
        Assert.Equal(1, extraction.Coverage.UnsupportedCategories); // Console.Out -> property
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Assert.NotNull(extraction.Authority); // complete universe records authority
        Assert.Single(extraction.Authority!.Facts);

        Emit("e3a", new
        {
            classification = "naturally reproduced (constructed source, admissible universe)",
            facts = extraction.Facts.Length,
            external = extraction.Coverage.External,
            unresolved = extraction.Coverage.Unresolved,
            unsupportedCategories = extraction.Coverage.UnsupportedCategories,
            coverageState = extraction.Coverage.State.ToString(),
            authorityRecorded = extraction.Authority is not null,
            authorityFacts = extraction.Authority?.Facts.Count,
        });
    }

    [Fact]
    public void A4_IncompleteUniverse_NeverRecordsAuthority()
    {
        var app = SharedFixture.StandaloneApp();
        var scope = UniverseFactory.DeclareScope(
            ("App/App.csproj", "App/App.csproj", ["App/App.cs"]),
            ("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]));
        var incomplete = UniverseFactory.CreateUniverse(scope, [app]);

        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(incomplete, new VerifiedOccurrenceExtractionRequest(
            new CodeRepositoryId(UniverseFactory.RepositoryId), Plugin, app,
            _ => UniverseFactory.Digest("id"),
            _ => UniverseFactory.Digest("decl"),
            _ => UniverseFactory.Digest("shape"),
            UniverseFactory.TargetDocument));

        Assert.Empty(extraction.Facts);
        Assert.Null(extraction.Authority); // never records an authority envelope
        Assert.Equal(CodeRelationshipCoverageState.Partial, extraction.Coverage.State);

        Emit("a4", new
        {
            classification = "natural production guard (incomplete universe)",
            facts = extraction.Facts.Length,
            authorityRecorded = extraction.Authority is not null,
            coverageState = extraction.Coverage.State.ToString(),
        });
    }
}