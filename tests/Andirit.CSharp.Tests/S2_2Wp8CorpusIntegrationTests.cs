using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Andirit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Frozen = AdrCi1Exp.Shared;

namespace Andirit.CSharp.Tests;

/// <summary>
/// S2.2 WP8: integration/comparison harness over the accepted frozen S2.0/R2
/// corpus. It runs the real production chain (verified sources and references,
/// verified compilations, compctx1/compctx2, defining resolution, asmctx3,
/// usage4, extraction, coverage) over the corpus and compares the applicable
/// frozen expectations. Semantics and relations are compared; exact frozen
/// compctx1/asmctx3/usage4 values are fixture-profile values and are explicitly
/// NOT compared cross-profile (WP7-OWNER).
/// </summary>
public sealed class S2_2Wp8CorpusIntegrationTests
{
    private const string Repo = "corpus:wp8-production";
    private static readonly CodeRepositoryId Repository = new(Repo);
    private static readonly CodePluginId Plugin = new("corpus.wp8.production");
    private static readonly Lazy<string> Root = new(FindRepositoryRoot);
    private static readonly Lazy<JsonDocument> Expected = new(() =>
        JsonDocument.Parse(File.ReadAllText(ExpectedPath)));

    internal static string CorpusRoot => Path.Combine(Root.Value,
        "docs", "planning", "adr-ci1-01-ci1a-s2-0", "corpus");
    internal static string ExpectedPath => Path.Combine(Root.Value,
        "docs", "planning", "adr-ci1-01-ci1a-s2-0", "oracle", "expected", "oracle-expected-r2.json");

    private static readonly Func<IMethodSymbol, string> IdDigest = m => "wp8-id:" + m.Name;
    private static readonly Func<IMethodSymbol, string> DeclDigest = m => "wp8-decl:" + m.ContainingType.Name + "." + m.Name;
    private static readonly Func<IMethodSymbol, string> ShapeDigest = m => "wp8-method|" + m.Parameters.Length;
    private static readonly Func<IMethodSymbol, string> TargetDoc =
        m => m.GetDocumentationCommentId() ?? m.Name;

    private static readonly string[] UniverseAssemblies = ["Corpus.Core", "Corpus.Leaf"];

    // ================================================= corpus authentication

    [Fact]
    public void Wp8_CorpusAuthentication_ManifestAndMinima()
    {
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var root = expected.RootElement;
        var manifest = root.GetProperty("fixtureManifest");
        Assert.Equal(11, manifest.EnumerateObject().Count());

        foreach (var entry in manifest.EnumerateObject())
        {
            var path = Path.Combine(CorpusRoot, "repo", entry.Name.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(path), "missing corpus file: " + entry.Name);
            var actual = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            Assert.Equal(entry.Value.GetString(), actual);
        }

        // §6.6 minima as declared by the accepted frozen corpus.
        var counts = root.GetProperty("counts");
        var minima = new (string Key, int Minimum)[]
        {
            ("csharp-files", 10), ("invocations", 50), ("method-references", 20),
            ("overloaded-methods", 5), ("namespaces", 3), ("projects", 2),
            ("cross-project-invocations", 5),
        };
        foreach (var (key, minimum) in minima)
            Assert.True(counts.GetProperty(key).GetInt32() >= minimum, key + " below §6.6 minimum");

        // Independently verify file/project/namespace counts from the corpus.
        var repo = Path.Combine(CorpusRoot, "repo");
        Assert.Equal(11, Directory.GetFiles(repo, "*.cs", SearchOption.AllDirectories).Length);
        Assert.Equal(5, Directory.GetFiles(repo, "*.csproj", SearchOption.AllDirectories).Length);
        var namespaces = Directory.GetFiles(repo, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadLines(path))
            .Where(line => line.TrimStart().StartsWith("namespace ", StringComparison.Ordinal))
            .Select(line => line.Trim()["namespace ".Length..].TrimEnd(';', ' ', '{'))
            .Distinct(StringComparer.Ordinal)
            .Count();
        Assert.True(namespaces >= 3, $"namespaces={namespaces}");
    }

    // ================================================= baseline semantics

    [Fact]
    public async Task Wp8_Baseline_Invocations_MatchFrozenSemantics()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var extraction = Extract(corpus.Universe, corpus.App);

        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var invocationEntries = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(entry => entry.GetProperty("Category").GetString() == "invocation")
            .ToArray();

        foreach (var entry in invocationEntries)
        {
            var file = entry.GetProperty("File").GetString()!;
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            var fact = FactAt(extraction, file, line, column);
            Assert.True(fact is not null, "missing fact for " + entry.GetProperty("Id").GetString());
            var symbolKey = entry.GetProperty("symbolKey").GetString()!;
            var docId = symbolKey[(symbolKey.IndexOf('!') + 1)..];
            // Frozen expectations use the definition documentation id; the
            // production fact records the constructed symbol's id.
            var derivedEntry = DeriveSupportedSpans(corpus.App)
                .Single(d => d.Path == file && d.Line == line && d.Column == column);
            Assert.Equal(docId, derivedEntry.DefinitionDocId);
            Assert.Equal(derivedEntry.ConstructedDocId, fact!.TargetDocumentId);
        }

        // Independent enumeration of every supported invocation in the corpus
        // (including unmarked sites): unique supported symbol bound to an
        // in-universe source assembly. The production fact set must match exactly.
        var derived = DeriveSupportedSpans(corpus.App);
        var actual = extraction.Facts
            .Select(f => (f.Span.Path, f.Span.StartLine, f.Span.StartColumn, f.TargetDocumentId))
            .ToHashSet();
        var derivedSet = derived
            .Select(d => (d.Path, d.Line, d.Column, d.ConstructedDocId))
            .ToHashSet();
        Console.WriteLine("WP8 app facts=" + extraction.Facts.Length + " derived=" + derivedSet.Count +
            " frozenInvocationEntries=" + invocationEntries.Length);
        Assert.Equal(derivedSet.Count, actual.Count);
        Assert.True(derivedSet.SetEquals(actual),
            "fact set differs; missing=" + string.Join(",", derivedSet.Except(actual)) +
            " extra=" + string.Join(",", actual.Except(derivedSet)));
        Assert.Equal(0, extraction.Coverage.Ambiguous);
        Assert.Equal(0, extraction.Coverage.Unresolved);
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
    }

    [Fact]
    public async Task Wp8_Baseline_UnsupportedAndDeferred_NeverEmitFacts()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var appExtraction = Extract(corpus.Universe, corpus.App);
        var negativesExtraction = Extract(corpus.Universe, corpus.Negatives);

        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var entries = expected.RootElement.GetProperty("semantics").EnumerateArray().ToArray();

        // Deferred/unsupported categories are outside the bounded invocation
        // scanner and must never appear as trusted facts.
        foreach (var entry in entries)
        {
            var category = entry.GetProperty("Category").GetString()!;
            var file = entry.GetProperty("File").GetString()!;
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            if (category == "resolved-external-to-requesting-project-missing-defining-provenance")
                continue; // shares the S01 span; exercised by the reference-missing variant
            if (line == 0 && column == 0)
                continue; // zero-span metadata row (event handler qualification)
            var extraction = file.StartsWith("Negatives/", StringComparison.Ordinal)
                ? negativesExtraction : appExtraction;
            if (category is "invocation" or "ambiguous" or "genuine-ambiguity" or "unresolved"
                or "dynamic" or "invalid-invocation" or "inactive")
                continue;
            Assert.True(FactAt(extraction, file, line, column) is null,
                $"unsupported category '{category}' produced a fact at {file}:{line}:{column}");
        }

        // The two invocation-shaped unsupported kinds (delegate invocation and
        // local-function invocation) are refused and counted, not emitted.
        Assert.True(appExtraction.Coverage.Unsupported >= 2);
        Assert.True(appExtraction.Coverage.UnsupportedCategories >= 2);
    }

    [Fact]
    public async Task Wp8_Negatives_RefusalsAndCoverage()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var extraction = Extract(corpus.Universe, corpus.Negatives);

        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var negatives = expected.RootElement.GetProperty("semantics").EnumerateArray()
            .Where(entry => entry.GetProperty("File").GetString()!.StartsWith("Negatives/", StringComparison.Ordinal))
            .ToArray();

        foreach (var entry in negatives)
        {
            var span = entry.GetProperty("span");
            var line = span.GetProperty("startLine").GetInt32();
            var column = span.GetProperty("startColumn").GetInt32();
            if (line == 0 && column == 0) continue;
            var fact = FactAt(extraction, entry.GetProperty("File").GetString()!, line, column);
            if (entry.GetProperty("Category").GetString() == "invalid-invocation")
            {
                // Recorded observation: the frozen oracle classifies CS0176 as an
                // invalid invocation; the accepted WP5 gate is binding-based.
                Console.WriteLine("WP8-CLS02-observed-fact=" + (fact is not null));
                continue;
            }
            Assert.True(fact is null,
                "negative entry '" + entry.GetProperty("Id").GetString() + "' produced a fact");
        }

        Assert.True(extraction.Coverage.Ambiguous >= 1);
        Assert.True(extraction.Coverage.Unresolved >= 2);
        Assert.True(extraction.Coverage.ConditionalInactive >= 1);
        Assert.Equal(0, extraction.Coverage.External); // F4: construction-stage refusal
        Assert.Equal(CodeRelationshipCoverageState.Produced, extraction.Coverage.State);
        Console.WriteLine("WP8 negatives coverage: ambiguous=" + extraction.Coverage.Ambiguous +
            " unresolved=" + extraction.Coverage.Unresolved +
            " inactive=" + extraction.Coverage.ConditionalInactive +
            " unsupported=" + extraction.Coverage.Unsupported +
            " facts=" + extraction.Facts.Length);
    }

    // ================================================= identity/provenance

    [Fact]
    public async Task Wp8_Identity_ProductionProfile_IndependentReconstruction()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var appExtraction = Extract(corpus.Universe, corpus.App);
        var s01 = FactAt(appExtraction, "App/Sites.cs", 15, 30);
        Assert.NotNull(s01);

        // Independent expected asmctx3: frozen R2 Describe over the production
        // inputs, then the frozen target tuple.
        var core = corpus.Core;
        var direct = Target(core.Compilation, "Corpus.Core.Methods", "Direct", 1);
        var expectedAsmctx3 = ExpectedAsmctx3(core, direct);
        Assert.Equal(expectedAsmctx3, s01!.TargetId.Value);

        // Independent expected usage4 from the frozen occurrence formula.
        var expectedUsage4 = ExpectedUsage4(corpus.App, s01.Span.Path,
            s01.Span.StartLine, s01.Span.StartColumn, s01.Span.EndLine, s01.Span.EndColumn,
            s01.TargetDocumentId);
        Assert.Equal(expectedUsage4, s01.Id.Value);
        Console.WriteLine("WP8 s01 asmctx3=" + s01.TargetId.Value);
        Console.WriteLine("WP8 s01 usage4=" + s01.Id.Value);

        // The frozen exact identity values are fixture-profile values; the
        // production profile (actual captured paths) must not be compared to them.
        using var expected = JsonDocument.Parse(File.ReadAllText(ExpectedPath));
        var frozenCore = expected.RootElement.GetProperty("identities")
            .GetProperty("compctx1").GetProperty("Core/Core.csproj").GetString();
        Assert.NotEqual(frozenCore, FrozenCompctx1(core));
        Assert.Contains("file|", core.Descriptor.References.First(r => r.StartsWith("file|", StringComparison.Ordinal)));
        Console.WriteLine("WP8 production-profile compctx1(Core)=" + FrozenCompctx1(core));
        Console.WriteLine("WP8 frozen-fixture-profile compctx1(Core)=" + frozenCore);
    }

    // ================================================= variants

    [Fact]
    public async Task Wp8_Variants_Relations()
    {
        var baseline = await CorpusAsync(CorpusVariant.Baseline);
        var again = await CorpusAsync(CorpusVariant.Baseline);
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(again.Core));
        Assert.Equal(baseline.Core.Descriptor.DescriptorDigest, again.Core.Descriptor.DescriptorDigest);

        var sourceChanged = await CorpusAsync(CorpusVariant.SourceChanged);
        Assert.NotEqual(
            baseline.App.Descriptor.Sources.Single(s => s.Path == "App/Sites.cs").ContentHash,
            sourceChanged.App.Descriptor.Sources.Single(s => s.Path == "App/Sites.cs").ContentHash);
        Assert.NotEqual(FrozenCompctx1(baseline.App), FrozenCompctx1(sourceChanged.App));
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(sourceChanged.Core));
        var baselineTarget = ExpectedAsmctx3(baseline.Core,
            Target(baseline.Core.Compilation, "Corpus.Core.Methods", "Direct", 1));
        var variantTarget = ExpectedAsmctx3(sourceChanged.Core,
            Target(sourceChanged.Core.Compilation, "Corpus.Core.Methods", "Direct", 1));
        Assert.Equal(baselineTarget, variantTarget);

        // symbols-leaf-changed: Leaf's identity changes; Core/App R2 tree material
        // (raw source text) is unchanged, so their compctx1 is stable while the
        // compctx2 chain changes.
        var symbolsLeaf = await CorpusAsync(CorpusVariant.SymbolsLeafChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.Leaf), FrozenCompctx1(symbolsLeaf.Leaf));
        Assert.Equal(FrozenCompctx1(baseline.Core), FrozenCompctx1(symbolsLeaf.Core));
        Assert.Equal(FrozenCompctx1(baseline.App), FrozenCompctx1(symbolsLeaf.App));
        Assert.NotEqual(baseline.Leaf.Descriptor.DescriptorDigest, symbolsLeaf.Leaf.Descriptor.DescriptorDigest);
        Assert.NotEqual(baseline.Core.Descriptor.DescriptorDigest, symbolsLeaf.Core.Descriptor.DescriptorDigest);
        Assert.NotEqual(baseline.App.Descriptor.DescriptorDigest, symbolsLeaf.App.Descriptor.DescriptorDigest);
        Assert.Equal(baselineTarget, ExpectedAsmctx3(symbolsLeaf.Core,
            Target(symbolsLeaf.Core.Compilation, "Corpus.Core.Methods", "Direct", 1)));

        // symbols-core-changed: Core's own identity and the conditional target change.
        var symbolsCore = await CorpusAsync(CorpusVariant.SymbolsCoreChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.Core), FrozenCompctx1(symbolsCore.Core));
        Assert.NotEqual(
            ExpectedAsmctx3(baseline.Core, Target(baseline.Core.Compilation, "Corpus.Core.ConditionalApi", "Value", 0)),
            ExpectedAsmctx3(symbolsCore.Core, Target(symbolsCore.Core.Compilation, "Corpus.Core.ConditionalApi", "Value", 0)));
        Assert.Equal(
            baseline.App.Descriptor.Sources.Single(s => s.Path == "App/Sites.cs").ContentHash,
            symbolsCore.App.Descriptor.Sources.Single(s => s.Path == "App/Sites.cs").ContentHash);

        // options-changed: material option changes all identities; restore reproduces.
        var options = await CorpusAsync(CorpusVariant.OptionsChanged);
        Assert.NotEqual(FrozenCompctx1(baseline.App), FrozenCompctx1(options.App));
        Assert.NotEqual(baseline.App.Descriptor.DescriptorDigest, options.App.Descriptor.DescriptorDigest);
        var restored = await CorpusAsync(CorpusVariant.Baseline);
        Assert.Equal(FrozenCompctx1(baseline.App), FrozenCompctx1(restored.App));
        Assert.Equal(baseline.App.Descriptor.DescriptorDigest, restored.App.Descriptor.DescriptorDigest);

        // reference-missing (RM01): S01 becomes unresolved and emits no fact.
        var missing = await CorpusAsync(CorpusVariant.ReferenceMissing);
        var missingExtraction = Extract(missing.Universe, missing.App);
        Assert.True(FactAt(missingExtraction, "App/Sites.cs", 15, 30) is null);
        Assert.True(missingExtraction.Coverage.Unresolved >= 1);
    }

    // ================================================= coverage

    [Fact]
    public async Task Wp8_Coverage_StatesMergingAndCompleteness()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var appExtraction = Extract(corpus.Universe, corpus.App);
        Assert.Equal(CodeRelationshipCoverageState.Produced, appExtraction.Coverage.State);
        Assert.True(appExtraction.Coverage.IsComplete);

        var solo = Extract(corpus.Universe, corpus.Solo);
        Assert.Empty(solo.Facts);
        Assert.Equal(CodeRelationshipCoverageState.Produced, solo.Coverage.State);
        Assert.True(solo.Coverage.IsComplete); // verified complete-empty

        var scopeA = "analysisscope1:sha256:" + new string('a', 64);
        var scopeB = "analysisscope1:sha256:" + new string('b', 64);
        var universe1 = "compilation-universe1:sha256:" + new string('c', 64);
        var universe2 = "compilation-universe1:sha256:" + new string('d', 64);
        var producedA = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeA, universe1, 1, 1);
        var producedB = new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeB, universe1, 1, 1);
        var mixed = producedA.Merge(producedB);
        Assert.Equal(CodeRelationshipCoverageState.Partial, mixed.State);
        Assert.Null(mixed.ScopeId);
        Assert.Throws<ArgumentException>(() => producedA.Merge(
            new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeA, universe2, 1, 1)));
        Assert.Equal(CodeRelationshipCoverageState.Produced,
            producedA.Merge(new CodeOccurrenceCoverage(1, 0, CodeRelationshipCoverageState.Produced, scopeA, universe1, 1, 1)).State);
    }

    // ================================================= adversarial integration

    [Fact]
    public async Task Wp8_Adversarial_StaleSubstitutedAndScopeMismatch()
    {
        var baseline = await CorpusAsync(CorpusVariant.Baseline);
        var symbolsLeaf = await CorpusAsync(CorpusVariant.SymbolsLeafChanged);
        var coreTarget = Target(symbolsLeaf.Core.Compilation, "Corpus.Core.Methods", "Direct", 1);

        // Stale recorded compctx2 (baseline) against the changed context.
        var stale = symbolsLeaf.Universe.MintTarget(coreTarget, baseline.Core.Descriptor.DescriptorDigest,
            IdDigest(coreTarget), DeclDigest(coreTarget), ShapeDigest(coreTarget));
        Assert.Equal(VerifiedTargetMintStatus.StaleContext, stale.Status);
        Assert.Null(stale.TargetId);

        // Substituted live instance (metadata-equivalent, different object).
        var substituted = Target(baseline.Core.Compilation, "Corpus.Core.Methods", "Direct", 1);
        var substitutedMint = symbolsLeaf.Universe.MintTarget(substituted, symbolsLeaf.Core.Descriptor.DescriptorDigest,
            IdDigest(substituted), DeclDigest(substituted), ShapeDigest(substituted));
        Assert.False(substitutedMint.IsMinted);

        // Declared scope claiming a source that was not compiled is refused.
        var scope = VerifiedCSharpScope.Declare(Repo,
        [
            new("App/App.csproj", "App/App.csproj", ["App/Bulk.cs", "App/Sites.cs"]),
            new("Core/Core.csproj", "Core/Core.csproj",
                ["Core/Extensions.cs", "Core/LeafUser.cs", "Core/Methods.cs", "Core/Types.cs"]),
            new("Leaf/Leaf.csproj", "Leaf/Leaf.csproj", ["Leaf/Helper.cs"]),
            new("Solo/Solo.csproj", "Solo/Solo.csproj", ["Solo/Declarations.cs"]),
            new("Negatives/Negatives.csproj", "Negatives/Negatives.csproj",
                ["Negatives/External.cs", "Negatives/NegativeSites.cs"]),
        ]);
        var sourceDeleted = await CorpusAsync(CorpusVariant.SourceDeleted);
        Assert.Throws<InvalidDataException>(() =>
            VerifiedCSharpUniverse.Create(Repo, scope,
                [sourceDeleted.Leaf, sourceDeleted.Core, sourceDeleted.App, sourceDeleted.Solo, sourceDeleted.Negatives]));
    }

    // ================================================= determinism

    [Fact]
    public async Task Wp8_Determinism_CanonicalDigest()
    {
        var canonical = await CanonicalOutputAsync();
        var second = await CanonicalOutputAsync();
        Assert.Equal(canonical, second);
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        Console.WriteLine("WP8-DIGEST=" + hash);
        Console.WriteLine("WP8-CANONICAL-BYTES=" + Encoding.UTF8.GetByteCount(canonical));
    }

    // ---------------------------------------------------------------- harness

    internal sealed record CorpusBuildResult(
        VerifiedCSharpCompilation Leaf,
        VerifiedCSharpCompilation Core,
        VerifiedCSharpCompilation App,
        VerifiedCSharpCompilation Solo,
        VerifiedCSharpCompilation Negatives,
        VerifiedCSharpUniverse Universe);

    internal sealed record CorpusVariant(
        string Name,
        string AppSitesPhysical,
        string CoreSymbols,
        string LeafSymbols,
        bool IncludeCoreReference,
        bool IncludeLeafReference,
        bool IncludeAppSites,
        bool CheckOverflow)
    {
        public static readonly CorpusVariant Baseline = new("baseline", "repo/App/Sites.cs", "", "", true, true, true, false);
        public static readonly CorpusVariant SourceChanged = new("source-changed", "variants/source-changed/App/Sites.cs", "", "", true, true, true, false);
        public static readonly CorpusVariant SymbolsLeafChanged = new("symbols-leaf-changed", "repo/App/Sites.cs", "", "LEAF_TRACE", true, true, true, false);
        public static readonly CorpusVariant SymbolsCoreChanged = new("symbols-core-changed", "repo/App/Sites.cs", "CORE_FEATURE", "", true, true, true, false);
        public static readonly CorpusVariant OptionsChanged = new("options-changed", "repo/App/Sites.cs", "", "", true, true, true, true);
        public static readonly CorpusVariant ReferenceMissing = new("reference-missing", "repo/App/Sites.cs", "", "", false, true, true, false);
        public static readonly CorpusVariant SourceDeleted = new("source-deleted", "repo/App/Sites.cs", "", "", true, true, false, false);
    }

    private static readonly Lazy<VerifiedMetadataReference> Corelib =
        new(() => VerifiedMetadataReference.Capture(typeof(object).Assembly.Location));
    private static readonly Lazy<VerifiedMetadataReference> Runtime =
        new(() => VerifiedMetadataReference.Capture(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")));
    private static readonly Lazy<VerifiedMetadataReference> MicrosoftCSharp =
        new(() => VerifiedMetadataReference.Capture(Path.Combine(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!, "Microsoft.CSharp.dll")));
    private static readonly Lazy<VerifiedMetadataReference> ExternalImage = new(CaptureExternalImage);

    private static VerifiedMetadataReference[] Framework() =>
        [Runtime.Value, Corelib.Value, MicrosoftCSharp.Value];

    private static VerifiedMetadataReference CaptureExternalImage()
    {
        var source = Path.Combine(CorpusRoot, "repo", "External", "ExternalLib.cs");
        var tree = CSharpSyntaxTree.ParseText(
            File.ReadAllText(source),
            new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose, SourceCodeKind.Regular),
            "External/ExternalLib.cs");
        var compilation = CSharpCompilation.Create("Corpus.External", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, deterministic: true, concurrentBuild: false));
        using var image = new MemoryStream();
        var emit = compilation.Emit(image);
        if (!emit.Success) throw new InvalidOperationException("Corpus.External emit failed");
        var directory = Path.Combine(Path.GetTempPath(), "andirit-wp8");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "Corpus.External.dll");
        File.WriteAllBytes(path, image.ToArray());
        return VerifiedMetadataReference.Capture(path);
    }

    private static async Task<CorpusBuildResult> CorpusAsync(CorpusVariant variant)
    {
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            nullableContextOptions: NullableContextOptions.Disable,
            checkOverflow: variant.CheckOverflow, deterministic: true, concurrentBuild: false);
        var framework = Framework();

        var leaf = await BuildUnitAsync("Leaf/Leaf.csproj", "Corpus.Leaf",
            [("Leaf/Helper.cs", "repo/Leaf/Helper.cs")],
            variant.LeafSymbols, framework, [], options);
        var core = await BuildUnitAsync("Core/Core.csproj", "Corpus.Core",
            [
                ("Core/Types.cs", "repo/Core/Types.cs"),
                ("Core/Methods.cs", "repo/Core/Methods.cs"),
                ("Core/Extensions.cs", "repo/Core/Extensions.cs"),
                ("Core/LeafUser.cs", "repo/Core/LeafUser.cs"),
            ],
            variant.CoreSymbols, framework, [leaf], options);
        var appSources = new List<(string, string)>();
        if (variant.IncludeAppSites)
            appSources.Add(("App/Sites.cs", variant.AppSitesPhysical));
        appSources.Add(("App/Bulk.cs", "repo/App/Bulk.cs"));
        var appDependencies = new List<VerifiedCSharpCompilation>();
        if (variant.IncludeCoreReference) appDependencies.Add(core);
        if (variant.IncludeLeafReference) appDependencies.Add(leaf);
        var appMetadata = framework.ToList();
        var app = await BuildUnitAsync("App/App.csproj", "Corpus.App",
            appSources.ToArray(), "", appMetadata, appDependencies, options);
        var solo = await BuildUnitAsync("Solo/Solo.csproj", "Corpus.Solo",
            [("Solo/Declarations.cs", "repo/Solo/Declarations.cs")], "", framework, [], options);
        var negatives = await BuildUnitAsync("Negatives/Negatives.csproj", "Corpus.Negatives",
            [
                ("Negatives/NegativeSites.cs", "repo/Negatives/NegativeSites.cs"),
                ("Negatives/External.cs", "repo/Negatives/External.cs"),
            ],
            "", [.. framework, ExternalImage.Value], [core], options);

        var units = new[]
        {
            new CSharpScopeUnit(leaf.Descriptor.ProjectUnitId, leaf.Descriptor.ProjectUnitId,
                leaf.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()),
            new CSharpScopeUnit(core.Descriptor.ProjectUnitId, core.Descriptor.ProjectUnitId,
                core.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()),
            new CSharpScopeUnit(app.Descriptor.ProjectUnitId, app.Descriptor.ProjectUnitId,
                app.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()),
            new CSharpScopeUnit(solo.Descriptor.ProjectUnitId, solo.Descriptor.ProjectUnitId,
                solo.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()),
            new CSharpScopeUnit(negatives.Descriptor.ProjectUnitId, negatives.Descriptor.ProjectUnitId,
                negatives.Descriptor.Sources.Select(s => s.Path).ToImmutableArray()),
        };
        var universe = VerifiedCSharpUniverse.Create(Repo, VerifiedCSharpScope.Declare(Repo, units),
            [leaf, core, app, solo, negatives]);
        return new(leaf, core, app, solo, negatives, universe);
    }

    private static async Task<VerifiedCSharpCompilation> BuildUnitAsync(
        string unitId, string assembly, (string Logical, string Physical)[] sources,
        string symbols, IReadOnlyList<VerifiedMetadataReference> metadata,
        IReadOnlyList<VerifiedCSharpCompilation> dependencies, CSharpCompilationOptions options)
    {
        var verifiedSources = new List<VerifiedCSharpSource>();
        var trees = new List<SyntaxTree>();
        var parse = new CSharpParseOptions(LanguageVersion.CSharp14, DocumentationMode.Diagnose,
            SourceCodeKind.Regular,
            string.IsNullOrEmpty(symbols) ? [] : symbols.Split(';'));
        foreach (var (logical, physical) in sources.OrderBy(s => s.Logical, StringComparer.Ordinal))
        {
            var bytes = File.ReadAllBytes(Path.Combine(CorpusRoot, physical));
            var path = Path.Combine(CorpusRoot, physical);
            var source = await VerifiedCSharpSource.ReadAsync(
                new CodeGraphSource(logical, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)),
                    _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false))),
                CancellationToken.None);
            verifiedSources.Add(source);
            trees.Add(CSharpSyntaxTree.ParseText(source.Text, parse, logical));
        }
        var references = new List<MetadataReference>();
        references.AddRange(metadata.Select(m => m.Reference));
        references.AddRange(dependencies.Select(d => d.Compilation.ToMetadataReference()));
        var compilation = CSharpCompilation.Create(assembly, trees, references, options);
        return VerifiedCSharpCompilation.Create(Repo, unitId, compilation,
            verifiedSources, metadata, dependencies, CancellationToken.None);
    }

    private static VerifiedOccurrenceExtraction Extract(
        VerifiedCSharpUniverse universe, VerifiedCSharpCompilation unit) =>
        VerifiedCSharpOccurrenceExtractor.Extract(universe,
            new VerifiedOccurrenceExtractionRequest(Repository, Plugin, unit,
                IdDigest, DeclDigest, ShapeDigest, TargetDoc));

    private static CodeOccurrenceFact? FactAt(
        VerifiedOccurrenceExtraction extraction, string file, int line, int column) =>
        extraction.Facts.SingleOrDefault(f => f.Span.Path == file &&
            f.Span.StartLine == line && f.Span.StartColumn == column);

    private static List<(string Path, int Line, int Column, string ConstructedDocId, string DefinitionDocId)> DeriveSupportedSpans(
        VerifiedCSharpCompilation unit)
    {
        var results = new List<(string, int, int, string, string)>();
        foreach (var tree in unit.Compilation.SyntaxTrees)
        {
            var model = unit.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var info = model.GetSymbolInfo(invocation);
                if (info.Symbol is not IMethodSymbol method) continue;
                if (method.MethodKind is not (MethodKind.Ordinary
                    or MethodKind.ReducedExtension or MethodKind.ExplicitInterfaceImplementation)) continue;
                if (!UniverseAssemblies.Contains(method.ContainingAssembly.Name, StringComparer.Ordinal)) continue;
                var token = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax member => member.Name.Identifier,
                    SimpleNameSyntax simple => simple.Identifier,
                    _ => invocation.Expression.GetLastToken(),
                };
                var line = tree.GetLineSpan(token.Span);
                results.Add((tree.FilePath,
                    line.StartLinePosition.Line + 1, line.StartLinePosition.Character + 1,
                    method.GetDocumentationCommentId() ?? method.Name,
                    (method.ReducedFrom ?? method.OriginalDefinition).GetDocumentationCommentId()
                        ?? (method.ReducedFrom ?? method.OriginalDefinition).Name));
            }
        }
        return results;
    }

    private static IMethodSymbol Target(CSharpCompilation compilation, string typeName, string method, int arity)
    {
        var type = Assert.IsAssignableFrom<INamedTypeSymbol>(compilation.GetTypeByMetadataName(typeName));
        return Assert.IsAssignableFrom<IMethodSymbol>(type.GetMembers(method)
            .Single(m => m is IMethodSymbol ms && ms.Parameters.Length == arity));
    }

    private static string FrozenCompctx1(VerifiedCSharpCompilation unit) =>
        Frozen.CompilationProvenance.Describe(Repo, unit.Descriptor.ProjectUnitId, unit.Compilation,
            unit.Sources.Select(s => (s.Path, Encoding.UTF8.GetBytes(s.Text.ToString()))).ToArray())
        .DescriptorDigest;

    private static string ExpectedAsmctx3(VerifiedCSharpCompilation unit, IMethodSymbol method) =>
        CodeOccurrenceIdentity.ComputeTargetId(FrozenCompctx1(unit), Metadata(unit), Module(unit),
            IdDigest(method), DeclDigest(method), ShapeDigest(method)).Value;

    private static string ExpectedUsage4(VerifiedCSharpCompilation calling, string path,
        int startLine, int startColumn, int endLine, int endColumn, string targetDocumentId)
    {
        var sourceHash = calling.Sources.Single(s => s.Path == path).ContentHash;
        var parts = new List<string>
        {
            Repo, Plugin.Value, CodeOccurrenceIdentity.SemanticsVersion,
            calling.Descriptor.ProjectUnitId, path, sourceHash,
            startLine.ToString(), startColumn.ToString(), endLine.ToString(), endColumn.ToString(),
            targetDocumentId, ExpectedAsmctx3Core(calling, targetDocumentId), CodeOccurrenceIdentity.InvocationCategory,
        };
        return LengthPrefixedHash("usage4", parts);
    }

    private static string ExpectedAsmctx3Core(VerifiedCSharpCompilation calling, string targetDocumentId)
    {
        // The S01 site targets Corpus.Core.Methods.Direct; resolve its defining unit.
        var core = calling.Dependencies.Single(d => d.Descriptor.ProjectUnitId == "Core/Core.csproj");
        var method = core.Compilation.GetTypeByMetadataName("Corpus.Core.Methods")!
            .GetMembers("Direct").OfType<IMethodSymbol>().Single(m => m.Parameters.Length == 1);
        return ExpectedAsmctx3(core, method);
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

    private static async Task<string> CanonicalOutputAsync()
    {
        var corpus = await CorpusAsync(CorpusVariant.Baseline);
        var app = Extract(corpus.Universe, corpus.App);
        var negatives = Extract(corpus.Universe, corpus.Negatives);
        var solo = Extract(corpus.Universe, corpus.Solo);
        var lines = new List<string>
        {
            "corpus=CI1A-S2.0-CORPUS-R2",
            "profile=production-capture",
            "leaf.compctx1=" + FrozenCompctx1(corpus.Leaf),
            "leaf.compctx2=" + corpus.Leaf.Descriptor.DescriptorDigest,
            "core.compctx1=" + FrozenCompctx1(corpus.Core),
            "core.compctx2=" + corpus.Core.Descriptor.DescriptorDigest,
            "app.compctx1=" + FrozenCompctx1(corpus.App),
            "app.compctx2=" + corpus.App.Descriptor.DescriptorDigest,
            "app.coverage=" + Coverage(app.Coverage),
            "neg.coverage=" + Coverage(negatives.Coverage),
            "solo.coverage=" + Coverage(solo.Coverage),
        };
        lines.AddRange(app.Facts
            .Select(f => $"fact|{f.Span.Path}|{f.Span.StartLine}|{f.Span.StartColumn}|{f.TargetDocumentId}|{f.TargetId.Value}|{f.Id.Value}")
            .Order(StringComparer.Ordinal));
        return string.Join("\n", lines);
    }

    private static string Coverage(CodeOccurrenceCoverage coverage) =>
        $"state={coverage.State};facts={coverage.InvocationFacts};unsupported={coverage.Unsupported}" +
        $";unsupportedCategories={coverage.UnsupportedCategories};ambiguous={coverage.Ambiguous}" +
        $";unresolved={coverage.Unresolved};external={coverage.External}" +
        $";inactive={coverage.ConditionalInactive};declared={coverage.DeclaredUnitCount};verified={coverage.VerifiedUnitCount}";

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

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Andirit.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new InvalidOperationException("Repository root not found.");
    }
}
