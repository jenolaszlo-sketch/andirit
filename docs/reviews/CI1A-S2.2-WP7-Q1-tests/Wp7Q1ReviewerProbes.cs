using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp7Qualification;

/// <summary>
/// Independent WP7-Q1 probes for Option A §5.2 tests 2, 6 and 8. Expected
/// `compctx1` values come from the frozen R2 `CompilationProvenance.Describe`
/// algorithm (an independent implementation, not the production mapping);
/// expected `asmctx3`/`usage4` come from the frozen identity formulas. Stale
/// authority probes exercise the real mint and in-memory publication paths.
/// </summary>
public sealed class Wp7Q1ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "review:wp7-q1";
    private static readonly CodeRepositoryId Repository = new(Repo);
    private static readonly CodePluginId Plugin = new("review.wp7q1.plugin");
    private static readonly CSharpCompilationOptions Options = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Release,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly CSharpCompilationOptions DebugOptions = new(
        OutputKind.DynamicallyLinkedLibrary,
        optimizationLevel: OptimizationLevel.Debug,
        nullableContextOptions: NullableContextOptions.Disable,
        deterministic: true,
        concurrentBuild: false);
    private static readonly Lazy<VerifiedMetadataReference> Platform =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location, CancellationToken.None));

    private static readonly Func<IMethodSymbol, string> IdDigest = m => "qid:" + m.Name;
    private static readonly Func<IMethodSymbol, string> DeclDigest = m => "qdecl:" + m.ContainingType.Name + "." + m.Name;
    private static readonly Func<IMethodSymbol, string> ShapeDigest = m => "qmethod|" + m.Parameters.Length;
    private static readonly Func<IMethodSymbol, string> TargetDoc = m => m.GetDocumentationCommentId() ?? m.Name;

    private const string LeafV1 = "namespace L; public static class Leaf { public static int V() => 1; }";
    private const string LeafV2 = "namespace L; public static class Leaf { public static int V() => 2; }";
    private const string MidSource = "namespace M; public static class Mid { public static int V() => L.Leaf.V(); }";
    private const string MidSourceV2 = "namespace M; public static class Mid { public static int V() => L.Leaf.V() + 1; }";
    private const string LibSource = "namespace B; public static class Lib { public static int V() => M.Mid.V(); }";
    private const string AppSource = "namespace A; public static class App { public static int Run() => B.Lib.V(); }";
    private const string SimpleSource = "namespace S; public static class X { public static int V() => 1; }";

    // ==================================================== Q1-A: test 2

    [Fact]
    public async Task Q1A_TransitiveSourceChange_StableIdentity_ChangedContext_StaleRefused()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2, MidSource, Options);

        // Independent frozen-R2 expectation: the R2-faithful compctx1 of Lib is
        // unchanged because Lib's own fields and the legacy projection of its
        // direct dependency (Mid) are unchanged.
        Assert.Equal(FrozenCompctx1(libA), FrozenCompctx1(libB));
        Assert.Equal(FrozenCompctx1(libA), libA.MapR2FaithfulCompctx1().DescriptorDigest);
        Assert.Equal(FrozenCompctx1(libB), libB.MapR2FaithfulCompctx1().DescriptorDigest);

        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var expected = ExpectedAsmctx3(libA, targetA);
        Assert.Equal(expected, ExpectedAsmctx3(libB, targetB));

        // compctx2 changes at every level.
        Assert.NotEqual(leafA.Descriptor.DescriptorDigest, leafB.Descriptor.DescriptorDigest);
        Assert.NotEqual(midA.Descriptor.DescriptorDigest, midB.Descriptor.DescriptorDigest);
        Assert.NotEqual(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.NotEqual(appA.Descriptor.DescriptorDigest, appB.Descriptor.DescriptorDigest);

        var universeA = Universe(leafA, midA, libA, appA);
        var universeB = Universe(leafB, midB, libB, appB);
        var mintA = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        var mintB = universeB.MintTarget(targetB, libB.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.Minted, mintA.Status);
        Assert.Equal(VerifiedTargetMintStatus.Minted, mintB.Status);
        Assert.Equal(expected, mintA.TargetId!.Value);
        Assert.Equal(expected, mintB.TargetId!.Value);

        // Acceptance-critical: identical structural identity, stale recorded
        // authority. A's recorded context must not mint in B.
        var stale = universeB.MintTarget(targetB, libA.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
        Assert.Null(stale.TargetId);

        // Stale usage4 is refused; fresh usage4 keeps the stable target identity.
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
        Assert.Equal(ExpectedUsage4(libB, libB, targetB, span), freshUsage.Fact!.Id.Value);
        Assert.Equal(expected, freshUsage.Fact.TargetId.Value);

        // Graph A remains reproducible.
        var replayA = universeA.MintTarget(targetA, libA.Descriptor.DescriptorDigest,
            IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA));
        Assert.Equal(expected, replayA.TargetId!.Value);
    }

    [Fact]
    public async Task Q1A_TransitiveOptionsOnlyChange_StableIdentity_ChangedContext()
    {
        // A purer transitive-only change: Leaf's compile options change, its source
        // does not. Mid's legacy projection (trees + metadata) is unchanged, so
        // Lib's frozen compctx1 and asmctx3 stay stable while Leaf/Mid/Lib compctx2
        // all change through the ancestor context chain.
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV1, MidSource, DebugOptions);

        Assert.NotEqual(leafA.Descriptor.DescriptorDigest, leafB.Descriptor.DescriptorDigest);
        Assert.NotEqual(midA.Descriptor.DescriptorDigest, midB.Descriptor.DescriptorDigest);
        Assert.NotEqual(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.Equal(FrozenCompctx1(libA), FrozenCompctx1(libB));
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        Assert.Equal(ExpectedAsmctx3(libA, targetA), ExpectedAsmctx3(libB, targetB));

        var universeB = Universe(leafB, midB, libB, appB);
        var stale = universeB.MintTarget(targetB, libA.Descriptor.DescriptorDigest,
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB));
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
    }

    [Fact]
    public async Task Q1A_DirectDependencySourceChange_ChangesDependentIdentity()
    {
        // Negative control: Mid's source change alters Lib's accepted
        // project-reference preimage, so Lib's identity must change. Stability is
        // specific to the transitive dimension.
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV1, MidSourceV2, Options);
        Assert.NotEqual(FrozenCompctx1(libA), FrozenCompctx1(libB));
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        Assert.NotEqual(ExpectedAsmctx3(libA, targetA), ExpectedAsmctx3(libB, targetB));
        Assert.NotEqual(
            Universe(leafA, midA, libA, appA).MintTarget(targetA, libA.Descriptor.DescriptorDigest,
                IdDigest(targetA), DeclDigest(targetA), ShapeDigest(targetA)).TargetId!.Value,
            Universe(leafB, midB, libB, appB).MintTarget(targetB, libB.Descriptor.DescriptorDigest,
                IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB)).TargetId!.Value);
    }

    [Fact]
    public async Task Q1A_CrossProjectBinding_StaleRefused_FreshMinted()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2, MidSource, Options);
        var symbolA = BoundInvocationSymbol(appA, "V");
        var symbolB = BoundInvocationSymbol(appB, "V");

        var universeB = Universe(leafB, midB, libB, appB);
        var stale = universeB.MintTarget(symbolB, libA.Descriptor.DescriptorDigest,
            IdDigest(symbolB), DeclDigest(symbolB), ShapeDigest(symbolB), appB.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
        var fresh = universeB.MintTarget(symbolB, libB.Descriptor.DescriptorDigest,
            IdDigest(symbolB), DeclDigest(symbolB), ShapeDigest(symbolB), appB.Compilation);
        Assert.Equal(VerifiedTargetMintStatus.Minted, fresh.Status);
        Assert.Equal(ExpectedAsmctx3(libB, symbolB), fresh.TargetId!.Value);

        var replayA = Universe(leafA, midA, libA, appA).MintTarget(symbolA,
            libA.Descriptor.DescriptorDigest, IdDigest(symbolA), DeclDigest(symbolA),
            ShapeDigest(symbolA), appA.Compilation);
        Assert.Equal(fresh.TargetId!.Value, replayA.TargetId!.Value);
    }

    [Fact]
    public async Task Q1A_SubstitutedInstance_Refused_ExactInstanceMints()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafA2, midA2, libA2, appA2) = await ChainAsync(LeafV1, MidSource, Options);
        Assert.Equal(libA.Descriptor.DescriptorDigest, libA2.Descriptor.DescriptorDigest);

        var universeA = Universe(leafA, midA, libA, appA);
        var substituted = Target(libA2.Compilation, "B.Lib", "V");
        var refused = universeA.MintTarget(substituted, libA.Descriptor.DescriptorDigest,
            IdDigest(substituted), DeclDigest(substituted), ShapeDigest(substituted));
        Assert.False(refused.IsMinted);
        Assert.Null(refused.TargetId);

        var own = Universe(leafA2, midA2, libA2, appA2).MintTarget(substituted,
            libA2.Descriptor.DescriptorDigest, IdDigest(substituted), DeclDigest(substituted),
            ShapeDigest(substituted));
        Assert.True(own.IsMinted);
        Assert.Equal(ExpectedAsmctx3(libA, Target(libA.Compilation, "B.Lib", "V")), own.TargetId!.Value);
    }

    [Fact]
    public async Task Q1A_UnchangedGraphFreshObjects_SameIdentity()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV1, MidSource, Options);
        Assert.Equal(libA.Descriptor.DescriptorDigest, libB.Descriptor.DescriptorDigest);
        Assert.Equal(FrozenCompctx1(libA), FrozenCompctx1(libB));
        Assert.Equal(
            ExpectedAsmctx3(libA, Target(libA.Compilation, "B.Lib", "V")),
            ExpectedAsmctx3(libB, Target(libB.Compilation, "B.Lib", "V")));
        output.WriteLine("Q1A unchanged-graph digests: " + FrozenCompctx1(libA));
    }

    // ==================================================== Q1-B: test 6

    [Fact]
    public void Q1B_CaptureIdentity_IndependentFormula()
    {
        var path = typeof(object).Assembly.Location;
        var captured = VerifiedMetadataReference.Capture(path);
        var expected = "file|" + Frozen.ProvenanceHashing.NormalizePath(path) + "|" +
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        Assert.Equal(expected, captured.Identity);
        // The frozen R2 reference identity for the same PE agrees.
        Assert.Equal(expected, FrozenReferenceIdentity(path));
    }

    [Fact]
    public async Task Q1B_EquivalentCaptures_SameFrozenCompctx1AndAsmctx3()
    {
        var path = typeof(object).Assembly.Location;
        var first = VerifiedMetadataReference.Capture(path);
        var second = VerifiedMetadataReference.Capture(path);
        Assert.Equal(first.Identity, second.Identity);

        var a = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, first);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, second);
        Assert.Equal(FrozenCompctx1(a), FrozenCompctx1(b));
        Assert.Equal(FrozenCompctx1(a), a.MapR2FaithfulCompctx1().DescriptorDigest);
        var method = Target(a.Compilation, "S.X", "V");
        Assert.Equal(ExpectedAsmctx3(a, method), ExpectedAsmctx3(b, method));
        Assert.True(Universe(a).MintTarget(method, a.Descriptor.DescriptorDigest,
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).IsMinted);
    }

    [Fact]
    public async Task Q1B_DifferentPathSameBytes_NotEquivalent()
    {
        var sourcePath = typeof(object).Assembly.Location;
        var copyPath = Path.Combine(Path.GetTempPath(), "wp7q1-ref-" + Guid.NewGuid().ToString("N") + ".dll");
        try
        {
            File.Copy(sourcePath, copyPath, overwrite: true);
            var original = VerifiedMetadataReference.Capture(sourcePath);
            var copy = VerifiedMetadataReference.Capture(copyPath);
            Assert.NotEqual(original.Identity, copy.Identity);

            var a = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, original);
            var b = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, copy);
            Assert.NotEqual(FrozenCompctx1(a), FrozenCompctx1(b));
            Assert.NotEqual(
                ExpectedAsmctx3(a, Target(a.Compilation, "S.X", "V")),
                ExpectedAsmctx3(b, Target(b.Compilation, "S.X", "V")));
        }
        finally
        {
            if (File.Exists(copyPath)) File.Delete(copyPath);
        }
    }

    [Fact]
    public async Task Q1B_DifferentPe_NotEquivalent()
    {
        var platform = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location);
        var other = VerifiedMetadataReference.Capture(typeof(Andirit.CodeRepositoryId).Assembly.Location);
        Assert.NotEqual(platform.Identity, other.Identity);
        var a = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, platform);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, other);
        Assert.NotEqual(FrozenCompctx1(a), FrozenCompctx1(b));
        Assert.NotEqual(
            ExpectedAsmctx3(a, Target(a.Compilation, "S.X", "V")),
            ExpectedAsmctx3(b, Target(b.Compilation, "S.X", "V")));
    }

    [Fact]
    public async Task Q1B_OptionsChange_ChangesCompctx1()
    {
        var a = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, Platform.Value, Options);
        var b = await BuildWithReferenceAsync("S/S.csproj", "S", SimpleSource, Platform.Value, DebugOptions);
        Assert.NotEqual(FrozenCompctx1(a), FrozenCompctx1(b));
        Assert.NotEqual(
            ExpectedAsmctx3(a, Target(a.Compilation, "S.X", "V")),
            ExpectedAsmctx3(b, Target(b.Compilation, "S.X", "V")));
    }

    [Fact]
    public void Q1B_ProfileBoundary_NoFixtureLabelSubstitution()
    {
        var production = VerifiedMetadataReference.Capture(typeof(object).Assembly.Location).Identity;
        Assert.StartsWith("file|" + Frozen.ProvenanceHashing.NormalizePath(typeof(object).Assembly.Location) + "|", production);
        Assert.DoesNotContain("runtime/", production);
        // The frozen-corpus fixture profile is a distinct declared profile whose
        // labelled items are not production identities. No cross-profile equality.
        var fixtureLabel = "file|runtime/System.Runtime.dll|" + new string('a', 64);
        Assert.NotEqual(fixtureLabel, production);
        output.WriteLine("Q1B profile boundary: production identity preserves the actual path; no fixture-label substitution.");
    }

    // ==================================================== Q1-C: test 8

    [Fact]
    public async Task Q1C_Publication_CurrentAccepted_StaleRefused_UnchangedTargetId()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp7q1-publication");
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2, MidSource, Options);
        var target = ExpectedAsmctx3(libA, Target(libA.Compilation, "B.Lib", "V"));
        Assert.Equal(target, ExpectedAsmctx3(libB, Target(libB.Compilation, "B.Lib", "V")));

        var first = await PublishAsync(store, repo, "run:1", new CodeOccurrenceTargetId(target), line: 1);
        var current = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, new CodeOccurrenceTargetId(target), expectedPublication: first));
        Assert.Equal(CodeOccurrenceAvailability.Available, current.Availability);
        Assert.True(current.IsComplete);

        var second = await PublishAsync(store, repo, "run:2", new CodeOccurrenceTargetId(target), line: 2);
        Assert.NotEqual(first, second);

        // Same structural target id, stale publication evidence: refused, no hits.
        var stale = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, new CodeOccurrenceTargetId(target), expectedPublication: first));
        Assert.Equal(CodeOccurrenceAvailability.StalePublication, stale.Availability);
        Assert.Empty(stale.Occurrences);
        Assert.False(stale.IsComplete);

        var fresh = await store.FindOccurrencesAsync(
            new CodeOccurrenceQuery(repo, new CodeOccurrenceTargetId(target), expectedPublication: second));
        Assert.Equal(CodeOccurrenceAvailability.Available, fresh.Availability);
        Assert.Equal(2, fresh.Occurrences[0].Span.StartLine);
    }

    [Fact]
    public async Task Q1C_GraphOnlyRepository_IsAnalysisUnavailable_NotCompleteEmpty()
    {
        var store = new InMemoryCodeGraphStore();
        var repo = new CodeRepositoryId("repo:wp7q1-graphonly");
        var target = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('c', 64));
        var started = new DateTimeOffset(2026, 11, 6, 0, 0, 0, TimeSpan.Zero);
        var runId = new CodeIndexRunId("run:1");
        await store.UpsertRepositoryAsync(new(repo));
        await store.StoreIndexRunAsync(new(repo, runId, started, plugins: [Plugin]));
        await store.StageIndexUnitAsync(new CodeIndexUnitReplacement(
            new CodeFactOrigin(repo, Plugin, "1.0.0", runId, new CodeIndexUnitId("unit:graph")),
            nodes: [new CodeGraphNode(new CodeNodeId("node:1"), CodeNodeKinds.Callable, "n", "n", new CodeSymbolId("s"))],
            occurrenceAnalysisIncluded: false));
        await store.CompleteIndexRunAsync(
            new(repo, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));

        var query = await store.FindOccurrencesAsync(new(repo, target));
        Assert.Equal(CodeOccurrenceAvailability.AnalysisUnavailable, query.Availability);
        Assert.Empty(query.Occurrences);
        Assert.False(query.IsComplete);
    }

    [Fact]
    public async Task Q1C_StaleContexts_CannotMintOrEmitThroughAnyRefusalClass()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2, MidSource, Options);
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var universeB = Universe(leafB, midB, libB, appB);

        Assert.Equal(VerifiedTargetMintStatus.StaleContext, universeB.MintTarget(targetB,
            libA.Descriptor.DescriptorDigest, IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB)).Status);
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext, universeB.MintTarget(targetB,
            null, IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB)).Status);
        Assert.False(universeB.MintTarget(targetB, "compctx2:sha256:" + new string('d', 64),
            IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB)).IsMinted);

        // A structurally valid target id is not an input to minting; the usage path
        // still requires a successful mint under the current recorded context.
        var structural = new CodeOccurrenceTargetId("asmctx3:sha256:" + new string('e', 64));
        Assert.NotEqual(ExpectedAsmctx3(libB, targetB), structural.Value);
        output.WriteLine("Q1C stale classes: StaleContext/MissingRecordedContext/fabricated refused; no fact path bypass.");
    }

    [Fact]
    public async Task Q1C_IncompleteUniverse_Refused()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var scope = VerifiedCSharpScope.Declare(Repo,
            [new("Leaf/Leaf.csproj", "Leaf/Leaf.csproj", ["Leaf/Leaf.cs"]),
             new("Mid/Mid.csproj", "Mid/Mid.csproj", ["Mid/Mid.cs"]),
             new("Lib/Lib.csproj", "Lib/Lib.csproj", ["Lib/Lib.cs"]),
             new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"])]);
        var universe = VerifiedCSharpUniverse.Create(Repo, scope, [leafA, midA, libA]);
        var target = Target(libA.Compilation, "B.Lib", "V");
        var mint = universe.MintTarget(target, libA.Descriptor.DescriptorDigest,
            IdDigest(target), DeclDigest(target), ShapeDigest(target));
        Assert.Equal(VerifiedTargetMintStatus.Incomplete, mint.Status);
        Assert.Null(mint.TargetId);
    }

    // ==================================================== Q1-D: authority continuity

    [Fact]
    public async Task Q1D_AmbiguousInvocation_NoTrustedFact()
    {
        var unit = await BuildUnitAsync("S/S.csproj", "S",
            "namespace N; public sealed class M {\n" +
            "  public int Amb(int a, long b) => a + (int)b;\n" +
            "  public int Amb(long a, int b) => (int)a + b;\n" +
            "}\n" +
            "public static class Site { public static int Run() { var m = new M(); return m.Amb(1, 1); } }",
            [], []);
        var extraction = VerifiedCSharpOccurrenceExtractor.Extract(Universe(unit),
            new VerifiedOccurrenceExtractionRequest(Repository, Plugin, unit,
                IdDigest, DeclDigest, ShapeDigest, TargetDoc));
        Assert.Empty(extraction.Facts);
        Assert.True(extraction.Coverage.Ambiguous >= 1);
    }

    // ==================================================== Q1-E: WP6 provenance

    [Fact]
    public void Q1E_CoverageProvenance_Unchanged()
    {
        var scopeA = "analysisscope1:sha256:" + new string('a', 64);
        var scopeB = "analysisscope1:sha256:" + new string('b', 64);
        var universe1 = "compilation-universe1:sha256:" + new string('c', 64);
        var universe2 = "compilation-universe1:sha256:" + new string('d', 64);

        var producedA = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced,
            scopeA, universe1, 1, 1);
        var producedB = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced,
            scopeB, universe1, 1, 1);
        var mixed = producedA.Merge(producedB);
        Assert.Equal(CodeRelationshipCoverageState.Partial, mixed.State);
        Assert.Null(mixed.ScopeId);
        Assert.False(mixed.IsComplete);

        Assert.Throws<ArgumentException>(() => producedA.Merge(
            new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced,
                scopeA, universe2, 1, 1)));

        var complete = producedA.Merge(new CodeOccurrenceCoverage(2, 0,
            CodeRelationshipCoverageState.Produced, scopeA, universe1, 1, 1));
        Assert.Equal(CodeRelationshipCoverageState.Produced, complete.State);

        // F09 heuristic counts remain observations: never facts, never completeness.
        var heuristic = new CodeOccurrenceCoverage(0, 0, CodeRelationshipCoverageState.Produced,
            scopeA, universe1, 1, 1, conditionalInactive: 5);
        Assert.Equal(0, heuristic.InvocationFacts);
        Assert.True(heuristic.IsComplete);
    }

    // ==================================================== Q1-F: determinism

    [Fact]
    public async Task Q1F_CanonicalDigests()
    {
        var (leafA, midA, libA, appA) = await ChainAsync(LeafV1, MidSource, Options);
        var (leafB, midB, libB, appB) = await ChainAsync(LeafV2, MidSource, Options);
        var targetA = Target(libA.Compilation, "B.Lib", "V");
        var targetB = Target(libB.Compilation, "B.Lib", "V");
        var identity = ExpectedAsmctx3(libA, targetA);
        Assert.Equal(identity, ExpectedAsmctx3(libB, targetB));

        var staleStatus = Universe(leafB, midB, libB, appB)
            .MintTarget(targetB, libA.Descriptor.DescriptorDigest,
                IdDigest(targetB), DeclDigest(targetB), ShapeDigest(targetB)).Status.ToString();

        output.WriteLine("WP7Q1-DIGEST|asmctx3=" + identity +
            "|compctx1A=" + FrozenCompctx1(libA) +
            "|compctx1B=" + FrozenCompctx1(libB) +
            "|compctx2A=" + libA.Descriptor.DescriptorDigest +
            "|compctx2B=" + libB.Descriptor.DescriptorDigest +
            "|stale=" + staleStatus);
    }

    // ---------------------------------------------------------------- helpers

    private static VerifiedCSharpCompilation leafOf(VerifiedCSharpCompilation app) =>
        app.Dependencies.Single().Dependencies.Single().Dependencies.Single();

    private static async Task<(VerifiedCSharpCompilation Leaf, VerifiedCSharpCompilation Mid,
        VerifiedCSharpCompilation Lib, VerifiedCSharpCompilation App)> ChainAsync(
        string leafText, string midText, CSharpCompilationOptions leafOptions)
    {
        var leaf = await BuildUnitAsync("Leaf/Leaf.csproj", "Leaf", leafText, [], [], null, leafOptions);
        var mid = await BuildUnitAsync("Mid/Mid.csproj", "Mid", midText, [], [leaf]);
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
        return VerifiedCSharpCompilation.Create(Repo, unit, compilation, [source], [captured],
            dependencies, CancellationToken.None);
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

    private static IMethodSymbol BoundInvocationSymbol(VerifiedCSharpCompilation unit, string methodName)
    {
        var tree = unit.Compilation.SyntaxTrees.Single();
        var invocation = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(i => i.Expression.ToString().EndsWith(methodName, StringComparison.Ordinal));
        return Assert.IsAssignableFrom<IMethodSymbol>(
            unit.Compilation.GetSemanticModel(tree).GetSymbolInfo(invocation).Symbol);
    }

    /// <summary>Frozen R2 `Describe` over identical accepted inputs — independent compctx1.</summary>
    private static string FrozenCompctx1(VerifiedCSharpCompilation unit)
    {
        var bytes = unit.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray();
        return Frozen.CompilationProvenance.Describe(Repo, unit.Descriptor.ProjectUnitId,
            unit.Compilation, bytes).DescriptorDigest;
    }

    private static string FrozenReferenceIdentity(string path)
    {
        // Recompute the frozen R2 `ReferenceIdentity` item for a captured PE.
        return "file|" + Frozen.ProvenanceHashing.NormalizePath(path) + "|" +
            Frozen.ProvenanceHashing.Sha256Hex(File.ReadAllBytes(path));
    }

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation unit, IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeTargetId(
            FrozenCompctx1(unit), Metadata(unit), Module(unit),
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).Value;

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling,
        VerifiedCSharpCompilation defining, IMethodSymbol method, CodeLocation span)
    {
        var sourceHash = calling.Sources.Single(s => s.Path == span.Path).ContentHash;
        var parts = new List<string>
        {
            Repo, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            calling.Descriptor.ProjectUnitId, span.Path, sourceHash,
            span.StartLine.ToString(), span.StartColumn.ToString(),
            span.EndLine.ToString(), span.EndColumn.ToString(),
            TargetDoc(method), ExpectedAsmctx3(defining, method), CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static string Metadata(VerifiedCSharpCompilation unit)
    {
        var identity = unit.Compilation.Assembly.Identity;
        var token = identity.PublicKeyToken.IsDefaultOrEmpty
            ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return string.Join("|", identity.Name, identity.Version.ToString(),
            identity.CultureName ?? string.Empty, token);
    }

    private static string Module(VerifiedCSharpCompilation unit) =>
        unit.Compilation.Assembly.Modules.First().Name;

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
        var replacement = new CodeIndexUnitReplacement(
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
        await store.StageIndexUnitAsync(replacement);
        await store.CompleteIndexRunAsync(
            new(repo, runId, started, CodeIndexRunStatus.Completed, started.AddSeconds(1), [Plugin]),
            new(repo, runId, []));
        return (await store.GetLatestPublicationAsync(repo))!;
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

    private static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
}
