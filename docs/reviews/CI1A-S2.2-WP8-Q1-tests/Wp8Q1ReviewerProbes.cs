using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit.Abstractions;
using Frozen = AdrCi1Exp.Shared;

namespace IndependentWp8Qualification;

/// <summary>
/// Independent WP8-Q1 probes. Expected values come from the frozen R2
/// authority compiled into this reviewer assembly; actual values come from the
/// real S2.2 production APIs. The frozen expected JSON is never regenerated.
/// </summary>
public sealed class Wp8Q1ReviewerProbes(ITestOutputHelper output)
{
    private const string Repo = "corpus:wp8-production";
    private static readonly CodeRepositoryId Repository = new(Repo);
    private static readonly CodePluginId Plugin = new("corpus.wp8.production");
    private static readonly Lazy<string> Root = new(FindRoot);
    private static readonly Lazy<JsonDocument> Expected = new(() =>
        JsonDocument.Parse(File.ReadAllText(ExpectedPath)));

    private static string CorpusRoot => Path.Combine(Root.Value,
        "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus");
    private static string ExpectedPath => Path.Combine(Root.Value,
        "docs", "planning", "adr-ci1-01-ci1a-s2-0", "oracle", "expected", "oracle-expected-r2.json");

    private static readonly Func<IMethodSymbol, string> IdDigest = m => "wp8-id:" + m.Name;
    private static readonly Func<IMethodSymbol, string> DeclDigest = m => "wp8-decl:" + m.ContainingType.Name + "." + m.Name;
    private static readonly Func<IMethodSymbol, string> ShapeDigest = m => "wp8-method|" + m.Parameters.Length;
    private static readonly Func<IMethodSymbol, string> TargetDoc = m => m.GetDocumentationCommentId() ?? m.Name;

    // ================================================= Q1-A corpus authentication

    [Fact]
    public async Task Q1_CorpusAuthentication()
    {
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        Assert.Equal("274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(ExpectedPath))));

        var manifest = expected.RootElement.GetProperty("fixtureManifest");
        Assert.Equal(11, manifest.EnumerateObject().Count());
        foreach (var entry in manifest.EnumerateObject())
        {
            var bytes = File.ReadAllBytes(Path.Combine(CorpusRoot, "repo",
                entry.Name.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Equal(entry.Value.GetString(),
                "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }

        var repoRoot = Path.Combine(CorpusRoot, "repo");
        Assert.Equal(11, Directory.GetFiles(repoRoot, "*.cs", SearchOption.AllDirectories).Length);
        Assert.Equal(5, Directory.GetFiles(repoRoot, "*.csproj", SearchOption.AllDirectories).Length);

        // §6.6 quantities recomputed with the accepted oracle definitions.
        var corpus = await CorpusAsync(Variant.Baseline);
        var appInvocations = corpus.App.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();
        var methodGroups = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Count(e => e.GetProperty("Category").GetString() == "method-group");
        var addOverloads = corpus.Core.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!
            .GetMembers("Add").OfType<IMethodSymbol>()
            .Select(m => m.GetDocumentationCommentId()).Distinct(StringComparer.Ordinal).Count();
        var namespaces = corpus.Leaf.Compilation.SyntaxTrees
            .Concat(corpus.Core.Compilation.SyntaxTrees)
            .Concat(corpus.App.Compilation.SyntaxTrees)
            .Concat(corpus.Solo.Compilation.SyntaxTrees)
            .Concat(corpus.Negatives.Compilation.SyntaxTrees)
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            .Select(n => n.Name.ToString()).Distinct(StringComparer.Ordinal).Count();
        var crossProject = 0;
        foreach (var tree in corpus.App.Compilation.SyntaxTrees)
        {
            var model = corpus.App.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var assembly = model.GetSymbolInfo(invocation).Symbol?.ContainingAssembly?.Name;
                if (assembly is not null && assembly != "Corpus.App") crossProject++;
            }
        }

        var counts = expected.RootElement.GetProperty("counts");
        Assert.Equal(11, counts.GetProperty("csharp-files").GetInt32());
        Assert.Equal(53, counts.GetProperty("invocations").GetInt32());
        Assert.Equal(21, counts.GetProperty("method-references").GetInt32());
        Assert.Equal(5, counts.GetProperty("overloaded-methods").GetInt32());
        Assert.Equal(5, counts.GetProperty("namespaces").GetInt32());
        Assert.Equal(5, counts.GetProperty("projects").GetInt32());
        Assert.Equal(52, counts.GetProperty("cross-project-invocations").GetInt32());

        Assert.Equal(53, appInvocations);
        Assert.Equal(21, methodGroups);
        Assert.Equal(5, addOverloads);
        Assert.Equal(5, namespaces);
        Assert.Equal(52, crossProject);
        output.WriteLine($"Q1 counts: files=11 invocations={appInvocations} methodGroups={methodGroups} addOverloads={addOverloads} namespaces={namespaces} crossProject={crossProject}");
    }

    // ================================================= Q1-B oracle independence

    [Fact]
    public async Task Q1_OracleIndependence_MutationBreaksFrozenComparison()
    {
        var baseline = await CorpusAsync(Variant.Baseline);
        var baselineS01 = FactAt(Extract(baseline.Universe, baseline.App), "App/Sites.cs", 15, 30)!;
        Assert.Equal("M:Corpus.Core.Methods.Direct(System.Int32)", baselineS01.TargetDocumentId);

        // Test-only mutation of a copied source (frozen corpus untouched): change
        // Direct's parameter type. The frozen expectation stays unchanged; the
        // production observation must change, so a frozen comparison must fail.
        var mutatedCore = Path.Combine(Path.GetTempPath(), "andirit-wp8q1",
            "Methods-" + Guid.NewGuid().ToString("N") + ".cs");
        Directory.CreateDirectory(Path.GetDirectoryName(mutatedCore)!);
        try
        {
            var text = File.ReadAllText(Path.Combine(CorpusRoot, "repo", "Core", "Methods.cs"))
                .Replace("public int Direct(int value) => value;", "public long Direct(long value) => value;");
            File.WriteAllText(mutatedCore, text);
            var mutated = await CorpusAsync(Variant.Baseline,
                new Dictionary<string, string> { ["Core/Methods.cs"] = mutatedCore });
            var mutatedS01 = FactAt(Extract(mutated.Universe, mutated.App), "App/Sites.cs", 15, 30)!;
            Assert.Equal("M:Corpus.Core.Methods.Direct(System.Int64)", mutatedS01.TargetDocumentId);
            Assert.NotEqual(baselineS01.TargetId.Value, mutatedS01.TargetId.Value);
            Assert.NotEqual(FrozenCompctx1(baseline.Core), FrozenCompctx1(mutated.Core));
            output.WriteLine("Q1 independence mutation: baseline doc=" + baselineS01.TargetDocumentId +
                " mutated doc=" + mutatedS01.TargetDocumentId + " (frozen expectation unchanged)");
        }
        finally
        {
            if (File.Exists(mutatedCore)) File.Delete(mutatedCore);
        }
    }

    // ================================================= Q1-C 50/51/53 reconciliation

    [Fact]
    public async Task Q1_OccurrenceReconciliation_53_50_51()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var extraction = Extract(corpus.Universe, corpus.App);

        var all = corpus.App.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();
        var derived = DeriveSupported(corpus.App);
        var unsupported = DeriveUnsupported(corpus.App);
        Assert.Equal(53, all);
        Assert.Equal(51, derived.Count);
        Assert.Equal(2, unsupported.Count);
        Assert.Equal(all, derived.Count + unsupported.Count);

        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var frozenInvocations = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(e => e.GetProperty("Category").GetString() == "invocation").ToArray();
        Assert.Equal(50, frozenInvocations.Length);

        var derivedBySpan = derived.ToDictionary(d => (d.Path, d.Line, d.Column));
        foreach (var entry in frozenInvocations)
        {
            var file = entry.GetProperty("File").GetString()!;
            var span = entry.GetProperty("span");
            var key = (file, span.GetProperty("startLine").GetInt32(), span.GetProperty("startColumn").GetInt32());
            Assert.True(derivedBySpan.ContainsKey(key), "frozen invocation not in derived set: " + entry.GetProperty("Id"));
            var symbolKey = entry.GetProperty("symbolKey").GetString()!;
            Assert.Equal(symbolKey[(symbolKey.IndexOf('!') + 1)..], derivedBySpan[key].DefinitionDocId);
        }

        var extra = derived.Where(d => !frozenInvocations.Any(e =>
        {
            var span = e.GetProperty("span");
            return e.GetProperty("File").GetString() == d.Path &&
                   span.GetProperty("startLine").GetInt32() == d.Line &&
                   span.GetProperty("startColumn").GetInt32() == d.Column;
        })).ToArray();
        Assert.Single(extra);
        Assert.Equal("App/Sites.cs", extra[0].Path);
        Assert.Equal("M:Corpus.Core.PartialType.Right", extra[0].DefinitionDocId);

        var actual = extraction.Facts
            .Select(f => (f.Span.Path, f.Span.StartLine, f.Span.StartColumn, f.TargetDocumentId))
            .ToHashSet();
        var derivedSet = derived.Select(d => (d.Path, d.Line, d.Column, d.ConstructedDocId)).ToHashSet();
        Assert.Equal(derivedSet.Count, actual.Count);
        Assert.True(derivedSet.SetEquals(actual), "supported fact sets differ");
        output.WriteLine("Q1 reconciliation: invocations=53 supported=51 unsupported=2 frozen=50 extra=PartialType.Right");
    }

    // ================================================= Q1-D frozen semantics

    [Fact]
    public async Task Q1_FrozenSemanticComparison()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var app = Extract(corpus.Universe, corpus.App);
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var entries = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(e => e.GetProperty("Category").GetString() == "invocation").ToArray();
        var derived = DeriveSupported(corpus.App).ToDictionary(d => (d.Path, d.Line, d.Column));
        foreach (var entry in entries)
        {
            var file = entry.GetProperty("File").GetString()!;
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            var fact = FactAt(app, file, line, column);
            Assert.True(fact is not null, "missing fact: " + entry.GetProperty("Id"));
            Assert.Equal(derived[(file, line, column)].ConstructedDocId, fact!.TargetDocumentId);
        }
        Assert.Equal(0, app.Coverage.Ambiguous);
        Assert.Equal(0, app.Coverage.Unresolved);
        output.WriteLine("Q1 frozen semantics: 50/50 matched; ambiguous=0 unresolved=0");
    }

    // ================================================= Q1-E identity anchors

    [Fact]
    public async Task Q1_IdentityAnchors()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var s01 = FactAt(Extract(corpus.Universe, corpus.App), "App/Sites.cs", 15, 30)!;
        Assert.StartsWith("asmctx3:sha256:ece8e803", s01.TargetId.Value);
        Assert.StartsWith("1027f062", s01.Id.Value);

        var core = corpus.Core;
        var direct = Target(core.Compilation, "Corpus.Core.Methods", "Direct", 1);
        Assert.Equal(s01.TargetId.Value, ExpectedAsmctx3(core, direct));
        Assert.Equal(s01.Id.Value, ExpectedUsage4(corpus.App, s01));
        Assert.StartsWith("compctx1:sha256:afcb56d0", FrozenCompctx1(core));

        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var frozenCore = expected.RootElement.GetProperty("identities")
            .GetProperty("compctx1").GetProperty("Core/Core.csproj").GetString();
        Assert.StartsWith("compctx1:sha256:942a8e58", frozenCore);
        Assert.NotEqual(frozenCore, FrozenCompctx1(core));
        output.WriteLine("Q1 anchors: s01 asmctx3=" + s01.TargetId.Value + " usage4=" + s01.Id.Value +
            " core compctx1=" + FrozenCompctx1(core) + " (frozen " + frozenCore + ")");
    }

    // ================================================= Q1-F profile controls

    [Fact]
    public async Task Q1_ProfileControls()
    {
        var baseline = await CorpusAsync(Variant.Baseline);
        var again = await CorpusAsync(Variant.Baseline);
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(again.Core));
        Assert.Equal(baseline.Core.Descriptor.DescriptorDigest, again.Core.Descriptor.DescriptorDigest);

        var sourceChanged = await CorpusAsync(Variant.SourceChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.App), FrozenCompctx1(sourceChanged.App));
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(sourceChanged.Core));

        var symbolsLeaf = await CorpusAsync(Variant.SymbolsLeafChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.Leaf), FrozenCompctx1(symbolsLeaf.Leaf));
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(symbolsLeaf.Core));
        Assert.Equal(FrozenCompctx1(baseline.App), FrozenCompctx1(symbolsLeaf.App));
        Assert.NotEqual(baseline.Core.Descriptor.DescriptorDigest, symbolsLeaf.Core.Descriptor.DescriptorDigest);
        Assert.NotEqual(baseline.App.Descriptor.DescriptorDigest, symbolsLeaf.App.Descriptor.DescriptorDigest);

        var options = await CorpusAsync(Variant.OptionsChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.App), FrozenCompctx1(options.App));
        Assert.NotEqual(FrozenCompctx1(baseline.Core), FrozenCompctx1(options.Core));

        var otherPath = typeof(object).Assembly.Location;
        var copyPath = Path.Combine(Path.GetTempPath(), "andirit-wp8q1", "ref-" + Guid.NewGuid().ToString("N") + ".dll");
        Directory.CreateDirectory(Path.GetDirectoryName(copyPath)!);
        try
        {
            File.Copy(otherPath, copyPath, overwrite: true);
            var original = VerifiedMetadataReference.Capture(otherPath);
            var copy = VerifiedMetadataReference.Capture(copyPath);
            Assert.NotEqual(original.Identity, copy.Identity);
            Assert.StartsWith("file|" + Path.GetFullPath(otherPath).Replace('\\', '/') + "|", original.Identity);
        }
        finally
        {
            if (File.Exists(copyPath)) File.Delete(copyPath);
        }

        var missing = await CorpusAsync(Variant.ReferenceMissing);
        Assert.True(FactAt(Extract(missing.Universe, missing.App), "App/Sites.cs", 15, 30) is null);
        output.WriteLine("Q1 profile controls: equivalence, source/symbols/options changes, path identity, missing reference all as accepted");
    }

    // ================================================= Q1-G negatives

    [Fact]
    public async Task Q1_Negatives_RefusalMatrix()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var negatives = Extract(corpus.Universe, corpus.Negatives);
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var entries = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(e => e.GetProperty("File").GetString()!.StartsWith("Negatives/", StringComparison.Ordinal))
            .ToArray();

        foreach (var entry in entries)
        {
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            if (line == 0 && column == 0) continue;
            Assert.True(FactAt(negatives, entry.GetProperty("File").GetString()!, line, column) is null,
                "negative emitted a fact: " + entry.GetProperty("Id"));
        }
        Assert.Equal(1, negatives.Coverage.Ambiguous);
        Assert.Equal(4, negatives.Coverage.Unresolved);
        Assert.Equal(1, negatives.Coverage.ConditionalInactive);
        Assert.Equal(0, negatives.Coverage.External);
        Assert.Empty(negatives.Facts);

        // Stale / missing / fabricated recorded context; substituted instance.
        var coreTarget = Target(corpus.Core.Compilation, "Corpus.Core.Methods", "Direct", 1);
        Assert.Equal(VerifiedTargetMintStatus.StaleContext,
            corpus.Universe.MintTarget(coreTarget, "compctx2:sha256:" + new string('a', 64),
                IdDigest(coreTarget), DeclDigest(coreTarget), ShapeDigest(coreTarget)).Status);
        Assert.Equal(VerifiedTargetMintStatus.MissingRecordedContext,
            corpus.Universe.MintTarget(coreTarget, null,
                IdDigest(coreTarget), DeclDigest(coreTarget), ShapeDigest(coreTarget)).Status);
        var substitutedCorpus = await CorpusAsync(Variant.Baseline);
        var substituted = Target(substitutedCorpus.Core.Compilation, "Corpus.Core.Methods", "Direct", 1);
        Assert.False(corpus.Universe.MintTarget(substituted, corpus.Core.Descriptor.DescriptorDigest,
            IdDigest(substituted), DeclDigest(substituted), ShapeDigest(substituted)).IsMinted);

        // Incomplete universe and incompatible/mixed coverage.
        var scope = VerifiedCSharpScope.Declare(Repo,
        [
            new("Leaf/Leaf.csproj", "Leaf/Leaf.csproj", ["Leaf/Helper.cs"]),
            new("Core/Core.csproj", "Core/Core.csproj",
                ["Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs", "Core/Types.cs"]),
            new("App/App.csproj", "App/App.csproj", ["App/Bulk.cs", "App/Sites.cs"]),
            new("Negatives/Negatives.csproj", "Negatives/Negatives.csproj",
                ["Negatives/External.cs", "Negatives/NegativeSites.cs"]),
            new("Missing/Missing.csproj", "Missing/Missing.csproj", ["Missing/Missing.cs"]),
        ]);
        var incomplete = VerifiedCSharpUniverse.Create(Repo, scope,
            [corpus.Leaf, corpus.Core, corpus.App, corpus.Negatives]);
        Assert.Equal(VerifiedTargetMintStatus.Incomplete,
            incomplete.MintTarget(coreTarget, corpus.Core.Descriptor.DescriptorDigest,
                IdDigest(coreTarget), DeclDigest(coreTarget), ShapeDigest(coreTarget)).Status);

        var scopeA = "analysisscope1:sha256:" + new string('a', 64);
        var scopeB = "analysisscope1:sha256:" + new string('b', 64);
        var u1 = "compilation-universe1:sha256:" + new string('c', 64);
        var u2 = "compilation-universe1:sha256:" + new string('d', 64);
        var a = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeA, u1, 1, 1);
        var b = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeB, u1, 1, 1);
        var mixed = a.Merge(b);
        Assert.Equal(CodeRelationshipCoverageState.Partial, mixed.State);
        Assert.Null(mixed.ScopeId);
        Assert.Throws<ArgumentException>(() => a.Merge(
            new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeA, u2, 1, 1)));
        output.WriteLine("Q1 negatives: ambiguous=1 unresolved=4 inactive=1 external=0 facts=0; stale/missing/fabricated/substituted/incomplete/mixed all refused");
    }

    // ================================================= Q1-H coverage

    [Fact]
    public async Task Q1_CoverageCompleteness()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var app = Extract(corpus.Universe, corpus.App);
        Assert.Equal(CodeRelationshipCoverageState.Produced, app.Coverage.State);
        Assert.True(app.Coverage.IsComplete);
        Assert.Equal(51, app.Facts.Length);

        var negatives = Extract(corpus.Universe, corpus.Negatives);
        Assert.Equal(CodeRelationshipCoverageState.Produced, negatives.Coverage.State);
        Assert.True(negatives.Coverage.IsComplete);
        Assert.Empty(negatives.Facts);
        Assert.True(negatives.Coverage.Ambiguous + negatives.Coverage.Unresolved + negatives.Coverage.ConditionalInactive > 0);

        var solo = Extract(corpus.Universe, corpus.Solo);
        Assert.Equal(CodeRelationshipCoverageState.Produced, solo.Coverage.State);
        Assert.True(solo.Coverage.IsComplete);
        Assert.Empty(solo.Facts);
        output.WriteLine("Q1 coverage: app Produced 51 facts; negatives Produced 0 facts with refusals; solo complete-empty");
    }

    // ================================================= Q1-I unsupported forms

    [Fact]
    public async Task Q1_UnsupportedDeferred_NoFacts()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var app = Extract(corpus.Universe, corpus.App);
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var deferred = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(e => e.GetProperty("Category").GetString() is not ("invocation" or "ambiguous"
                or "genuine-ambiguity" or "unresolved" or "dynamic" or "invalid-invocation" or "inactive"
                or "resolved-external-to-requesting-project-missing-defining-provenance"))
            .ToArray();
        foreach (var entry in deferred)
        {
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            if (line == 0 && column == 0) continue;
            Assert.True(FactAt(app, entry.GetProperty("File").GetString()!, line, column) is null,
                "deferred category emitted a fact: " + entry.GetProperty("Category"));
        }
        Assert.True(app.Coverage.Unsupported >= 2);
        Assert.Equal(21, expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Count(e => e.GetProperty("Category").GetString() == "method-group"));
        output.WriteLine("Q1 unsupported/deferred: no facts for method groups/ctor/property/field/event/delegate/local/type; unsupported counter=" + app.Coverage.Unsupported);
    }

    // ================================================= Q1-K determinism

    [Fact]
    public async Task Q1_Determinism_CanonicalDigest()
    {
        var canonical = await CanonicalAsync();
        var second = await CanonicalAsync();
        Assert.Equal(canonical, second);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        output.WriteLine("WP8Q1-DIGEST=" + hash + "|BYTES=" + Encoding.UTF8.GetByteCount(canonical));
    }

    // ---------------------------------------------------------------- harness

    private sealed record CorpusBuild(
        VerifiedCSharpCompilation Leaf, VerifiedCSharpCompilation Core,
        VerifiedCSharpCompilation App, VerifiedCSharpCompilation Solo,
        VerifiedCSharpCompilation Negatives, VerifiedCSharpUniverse Universe);

    private sealed record Variant(
        string Name, string AppSites, string CoreSymbols, string LeafSymbols,
        bool IncludeCore, bool IncludeLeaf, bool IncludeSites, bool CheckOverflow)
    {
        public static readonly Variant Baseline = new("baseline", "repo/App/Sites.cs", "", "", true, true, true, false);
        public static readonly Variant SourceChanged = new("source-changed", "variants/source-changed/App/Sites.cs", "", "", true, true, true, false);
        public static readonly Variant SymbolsLeafChanged = new("symbols-leaf-changed", "repo/App/Sites.cs", "", "LEAF_TRACE", true, true, true, false);
        public static readonly Variant SymbolsCoreChanged = new("symbols-core-changed", "repo/App/Sites.cs", "CORE_FEATURE", "", true, true, true, false);
        public static readonly Variant OptionsChanged = new("options-changed", "repo/App/Sites.cs", "", "", true, true, true, true);
        public static readonly Variant ReferenceMissing = new("reference-missing", "repo/App/Sites.cs", "", "", false, true, true, false);
    }

    private static readonly Lazy<VerifiedMetadataReference> Corelib =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location));
    private static readonly Lazy<VerifiedMetadataReference> Runtime =
        new(() => VerifiedMetadataReference.Capture(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")));
    private static readonly Lazy<VerifiedMetadataReference> MicrosoftCSharp =
        new(() => VerifiedMetadataReference.Capture(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "Microsoft.CSharp.dll")));
    private static readonly Lazy<VerifiedMetadataReference> External = new(CaptureExternal);

    private static VerifiedMetadataReference CaptureExternal()
    {
        var tree = CSharpSyntaxTree.ParseText(
            File.ReadAllText(Path.Combine(CorpusRoot, "repo", "External", "ExternalLib.cs")),
            new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose, SourceCodeKind.Regular),
            "External/ExternalLib.cs");
        var compilation = CSharpCompilation.Create("Corpus.External", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false));
        using var image = new MemoryStream();
        var emit = compilation.Emit(image);
        if (!emit.Success) throw new InvalidOperationException("external emit failed");
        var directory = Path.Combine(Path.GetTempPath(), "andirit-wp8q1");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Corpus.External.dll");
        File.WriteAllBytes(path, image.ToArray());
        return VerifiedMetadataReference.Capture(path);
    }

    private static async Task<CorpusBuild> CorpusAsync(Variant variant, Dictionary<string, string>? overrides = null)
    {
        overrides ??= [];
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            nullableContextOptions: NullableContextOptions.Disable,
            checkOverflow: variant.CheckOverflow, deterministic: true, concurrentBuild: false);
        var framework = new[] { Runtime.Value, Corelib.Value, MicrosoftCSharp.Value };

        var leaf = await BuildUnit("Leaf/Leaf.csproj", "Corpus.Leaf",
            [("Leaf/Helper.cs", "repo/Leaf/Helper.cs")], variant.LeafSymbols, framework, [], options, overrides);
        var core = await BuildUnit("Core/Core.csproj", "Corpus.Core",
            [("Core/Types.cs", "repo/Core/Types.cs"), ("Core/Methods.cs", "repo/Core/Methods.cs"),
             ("Core/Extensions.cs", "repo/Core/Extensions.cs"), ("Core/LeafUser.cs", "repo/Core/LeafUser.cs")],
            variant.CoreSymbols, framework, [leaf], options, overrides);
        var appSources = new List<(string, string)>();
        if (variant.IncludeSites) appSources.Add(("App/Sites.cs", variant.AppSites));
        appSources.Add(("App/Bulk.cs", "repo/App/Bulk.cs"));
        var deps = new List<VerifiedCSharpCompilation>();
        if (variant.IncludeCore) deps.Add(core);
        if (variant.IncludeLeaf) deps.Add(leaf);
        var app = await BuildUnit("App/App.csproj", "Corpus.App", appSources.ToArray(), "",
            framework, deps, options, overrides);
        var solo = await BuildUnit("Solo/Solo.csproj", "Corpus.Solo",
            [("Solo/Declarations.cs", "repo/Solo/Declarations.cs")], "", framework, [], options, overrides);
        var negMeta = new List<VerifiedMetadataReference>(framework) { External.Value };
        var negatives = await BuildUnit("Negatives/Negatives.csproj", "Corpus.Negatives",
            [("Negatives/NegativeSites.cs", "repo/Negatives/NegativeSites.cs"),
             ("Negatives/External.cs", "repo/Negatives/External.cs")],
            "", negMeta, [core], options, overrides);

        var units = new[]
        {
            ScopeUnit(leaf), ScopeUnit(core), ScopeUnit(app), ScopeUnit(solo), ScopeUnit(negatives),
        };
        var universe = VerifiedCSharpUniverse.Create(Repo, VerifiedCSharpScope.Declare(Repo, units),
            [leaf, core, app, solo, negatives]);
        return new(leaf, core, app, solo, negatives, universe);
    }

    private static CSharpScopeUnit ScopeUnit(VerifiedCSharpCompilation unit) =>
        new(unit.Descriptor.ProjectUnitId, unit.Descriptor.ProjectUnitId,
            unit.Descriptor.Sources.Select(s => s.Path).ToImmutableArray());

    private static async Task<VerifiedCSharpCompilation> BuildUnit(
        string unitId, string assembly, (string Logical, string Physical)[] sources,
        string symbols, IReadOnlyList<VerifiedMetadataReference> metadata,
        IReadOnlyList<VerifiedCSharpCompilation> dependencies,
        CSharpCompilationOptions options, Dictionary<string, string> overrides)
    {
        var verified = new List<VerifiedCSharpSource>();
        var trees = new List<SyntaxTree>();
        var parse = new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose,
            SourceCodeKind.Regular, string.IsNullOrEmpty(symbols) ? [] : symbols.Split(';'));
        foreach (var (logical, physical) in sources.OrderBy(s => s.Logical, StringComparer.Ordinal))
        {
            var file = overrides.TryGetValue(logical, out var alternate) ? alternate
                : Path.Combine(CorpusRoot, physical);
            var bytes = File.ReadAllBytes(file);
            var source = await VerifiedCSharpSource.ReadAsync(
                new CodeGraphSource(logical, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
                CancellationToken.None);
            verified.Add(source);
            trees.Add(CSharpSyntaxTree.ParseText(source.Text, parse, logical));
        }
        var references = new List<MetadataReference>();
        references.AddRange(metadata.Select(m => m.Reference));
        references.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, trees, references, options);
        return VerifiedCSharpCompilation.Create(Repo, unitId, compilation, verified, metadata,
            dependencies, CancellationToken.None);
    }

    private static VerifiedOccurrenceExtraction Extract(VerifiedCSharpUniverse universe,
        VerifiedCSharpCompilation unit) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe,
            new VerifiedOccurrenceExtractionRequest(Repository, Plugin, unit,
                IdDigest, DeclDigest, ShapeDigest, TargetDoc));

    private static CodeOccurrenceFact? FactAt(VerifiedOccurrenceExtraction extraction,
        string file, int line, int column) =>
        extraction.Facts.SingleOrDefault(f => f.Span.Path == file &&
            f.Span.StartLine == line && f.Span.StartColumn == column);

    private static List<(string Path, int Line, int Column, string ConstructedDocId, string DefinitionDocId)>
        DeriveSupported(VerifiedCSharpCompilation unit)
    {
        var results = new List<(string, int, int, string, string)>();
        foreach (var tree in unit.Compilation.SyntaxTrees)
        {
            var model = unit.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method) continue;
                if (method.MethodKind is not (MethodKind.Ordinary
                    or MethodKind.ReducedExtension or MethodKind.ExplicitInterfaceImplementation)) continue;
                if (method.ContainingAssembly.Name is not ("Corpus.Core" or "Corpus.Leaf")) continue;
                var token = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name.Identifier,
                    SimpleNameSyntax simple => simple.Identifier,
                    _ => invocation.Expression.GetLastToken(),
                };
                var line = tree.GetLineSpan(token.Span);
                results.Add((tree.FilePath, line.StartLinePosition.Line + 1,
                    line.StartLinePosition.Character + 1,
                    method.GetDocumentationCommentId() ?? method.Name,
                    (method.ReducedFrom ?? method.OriginalDefinition).GetDocumentationCommentId()
                        ?? (method.ReducedFrom ?? method.OriginalDefinition).Name));
            }
        }
        return results;
    }

    private static List<(string Path, int Line, int Column)> DeriveUnsupported(VerifiedCSharpCompilation unit)
    {
        var results = new List<(string, int, int)>();
        foreach (var tree in unit.Compilation.SyntaxTrees)
        {
            var model = unit.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
                    method.MethodKind is MethodKind.Ordinary or MethodKind.ReducedExtension
                        or MethodKind.ExplicitInterfaceImplementation)
                    continue;
                var token = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name.Identifier,
                    SimpleNameSyntax simple => simple.Identifier,
                    _ => invocation.Expression.GetLastToken(),
                };
                var line = tree.GetLineSpan(token.Span);
                results.Add((tree.FilePath, line.StartLinePosition.Line + 1,
                    line.StartLinePosition.Character + 1));
            }
        }
        return results;
    }

    private static string FrozenCompctx1(VerifiedCSharpCompilation unit) =>
        Frozen.CompilationProvenance.Describe(Repo, unit.Descriptor.ProjectUnitId, unit.Compilation,
            unit.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray())
        .DescriptorDigest;

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation unit, IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeTargetId(FrozenCompctx1(unit), Metadata(unit), Module(unit),
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).Value;

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling, CodeOccurrenceFact fact)
    {
        var core = calling.Dependencies.Single(d => d.Descriptor.ProjectUnitId == "Core/Core.csproj");
        var method = core.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!
            .GetMembers("Direct").OfType<IMethodSymbol>().Single(m => m.Parameters.Length == 1);
        var parts = new List<string>
        {
            Repo, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            calling.Descriptor.ProjectUnitId, fact.Span.Path,
            calling.Sources.Single(s => s.Path == fact.Span.Path).ContentHash,
            fact.Span.StartLine.ToString(), fact.Span.StartColumn.ToString(),
            fact.Span.EndLine.ToString(), fact.Span.EndColumn.ToString(),
            fact.TargetDocumentId, ExpectedAsmctx3(core, method),
            CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method, int arity)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method)
            .Single(m => m is IMethodSymbol ms && ms.Parameters.Length == arity));
    }

    private static string Metadata(VerifiedCSharpCompilation unit)
    {
        var identity = unit.Compilation.Assembly.Identity;
        var token = identity.PublicKeyToken.IsDefaultOrEmpty ? string.Empty
            : Convert.ToHexStringLower(identity.PublicKeyToken.AsSpan());
        return string.Join("|", identity.Name, identity.Version.ToString(),
            identity.CultureName ?? string.Empty, token);
    }

    private static string Module(VerifiedCSharpCompilation unit) =>
        unit.Compilation.Assembly.Modules.First().Name;

    private static async Task<string> CanonicalAsync()
    {
        var corpus = await CorpusAsync(Variant.Baseline);
        var app = Extract(corpus.Universe, corpus.App);
        var negatives = Extract(corpus.Universe, corpus.Negatives);
        var lines = new List<string>
        {
            "corpus=CI1A-S2.0-CORPUS-R2",
            "profile=production-capture",
            "leaf.compctx1=" + FrozenCompctx1(corpus.Leaf),
            "core.compctx1=" + FrozenCompctx1(corpus.Core),
            "core.compctx2=" + corpus.Core.Descriptor.DescriptorDigest,
            "app.compctx1=" + FrozenCompctx1(corpus.App),
            "app.compctx2=" + corpus.App.Descriptor.DescriptorDigest,
            "app.coverage=" + Coverage(app.Coverage),
            "neg.coverage=" + Coverage(negatives.Coverage),
        };
        lines.AddRange(app.Facts
            .Select(f => $"fact|{f.Span.Path}|{f.Span.StartLine}|{f.Span.StartColumn}|{f.TargetDocumentId}|{f.TargetId.Value}|{f.Id.Value}")
            .Order(StringComparer.Ordinal));
        return string.Join("\n", lines);
    }

    private static string Coverage(CodeOccurrenceCoverage c) =>
        $"state={c.State};facts={c.InvocationFacts};unsupported={c.Unsupported}" +
        $";ambiguous={c.Ambiguous};unresolved={c.Unresolved};external={c.External}" +
        $";inactive={c.ConditionalInactive};declared={c.DeclaredUnitCount};verified={c.VerifiedUnitCount}";

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

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Andirit.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("repository root not found");
    }
}
