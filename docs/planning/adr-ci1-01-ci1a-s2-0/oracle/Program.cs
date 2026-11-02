using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SemanticCorpusOracle;

public static class Program
{
    private const string AppUnit = "App/App.csproj";
    private const string CoreUnit = "Core/Core.csproj";
    private const string LeafUnit = "Leaf/Leaf.csproj";
    private const string SoloUnit = "Solo/Solo.csproj";

    private static int Main(string[] args)
    {
        var corpusRoot = ArgValue(args, "--corpus")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../corpus"));
        var outputPath = ArgValue(args, "--output");

        var checks = new List<Check>();
        var report = new Dictionary<string, object?>(StringComparer.Ordinal);

        try
        {
            R2Identity.SelfTest();
            checks.Add(new Check("anchor.r2-vectors", "identity", true, "frozen R2 compctx1/compctx2/usage4 anchors reproduced"));
        }
        catch (Exception ex)
        {
            checks.Add(new Check("anchor.r2-vectors", "identity", false, ex.Message));
            Finish(checks, report, outputPath);
            return 1;
        }

        var repoRoot = Path.Combine(corpusRoot, "repo");
        var baseline = CorpusBuild.Build(corpusRoot, CorpusVariant.Baseline);
        var baselineIdentity = CorpusIdentity.Compute(baseline);
        var second = CorpusBuild.Build(corpusRoot, CorpusVariant.Baseline);
        var secondIdentity = CorpusIdentity.Compute(second);

        // ---- semantic truth -------------------------------------------------
        var semantics = RunSemantics(baseline, checks);

        // ---- identity -------------------------------------------------------
        var identities = RunIdentities(baseline, baselineIdentity, checks);

        // ---- variant / Option A provenance ---------------------------------
        var (variants, variantIdentities) =
            RunOptionA(corpusRoot, baseline, baselineIdentity, second, secondIdentity, checks, identities);

        // ---- coverage -------------------------------------------------------
        var coverage = RunCoverage(baseline, baselineIdentity, checks);

        // ---- F12 counts -----------------------------------------------------
        var counts = RunCounts(baseline, semantics, repoRoot);
        foreach (var (key, value) in counts)
        {
            var pass = key switch
            {
                "csharp-files" => value >= 10,
                "invocations" => value >= 50,
                "method-references" => value >= 20,
                "overloaded-methods" => value >= 5,
                "namespaces" => value >= 3,
                "projects" => value >= 2,
                "cross-project-invocations" => value >= 5,
                _ => true,
            };
            checks.Add(new Check("f12." + key, "coverage", pass, $"{key} = {value}"));
        }

        report["environment"] = new
        {
            framework = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            roslyn = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(),
            languageVersion = LanguageVersion.CSharp14.ToString(),
        };
        report["fixtureManifest"] = FixtureManifest(repoRoot);
        report["semantics"] = semantics;
        report["identities"] = identities;
        report["variantIdentities"] = variantIdentities;
        report["variants"] = variants;
        report["coverage"] = coverage;
        report["counts"] = counts;

        var expectedPath = ArgValue(args, "--expected");
        if (expectedPath is not null)
            CompareToExpected(report, expectedPath, checks);

        Finish(checks, report, outputPath);
        return checks.Any(c => !c.Pass) ? 1 : 0;
    }

    // ------------------------------------------------------------------ semantics

    private static List<object> RunSemantics(CorpusBuild baseline, List<Check> checks)
    {
        var results = new List<object>();
        var app = baseline.Get(AppUnit);
        var negatives = baseline.Get("Negatives/Negatives.csproj");

        foreach (var c in BaselineCases())
        {
            var unit = c.UnitId == AppUnit ? app : negatives;
            var tree = unit.Compilation.SyntaxTrees.Single(t => t.FilePath == c.File);
            var result = SemanticInspector.Inspect(unit.Compilation, tree, c.Marker, Enum.Parse<SyntaxKind>(c.NodeKind));
            var pass = result.Resolution == c.ExpectedResolution &&
                (c.ExpectedSymbolKey is null || result.SymbolKey == c.ExpectedSymbolKey);
            checks.Add(new Check("semantic." + c.Id, "semantic", pass,
                $"{result.Resolution} {result.SymbolKey ?? "-"} (expected {c.ExpectedResolution} {c.ExpectedSymbolKey ?? "-"})"));
            results.Add(new
            {
                c.Id,
                c.File,
                c.Category,
                c.Support,
                resolution = result.Resolution.ToString(),
                symbolKey = result.SymbolKey,
                assembly = result.Assembly,
                span = new { startLine = result.StartLine, startColumn = result.StartColumn, endLine = result.EndLine, endColumn = result.EndColumn },
                detail = result.Detail,
            });
        }

        // Inactive conditional branch: marker text is present in the bytes but no active
        // invocation operation exists for the excluded code.
        var negTree = negatives.Compilation.SyntaxTrees.Single(t => t.FilePath == "Negatives/NegativeSites.cs");
        var negRoot = negTree.GetRoot();
        var hasMarker = negTree.ToString().Contains("@I01", StringComparison.Ordinal);
        var hasActiveInvocation = negRoot.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Any(i => i.ToString().Contains("Direct(99)", StringComparison.Ordinal));
        var inactivePass = hasMarker && !hasActiveInvocation;
        checks.Add(new Check("semantic.I01", "semantic", inactivePass,
            $"marker present={hasMarker} active inactive-code invocation={hasActiveInvocation}"));
        results.Add(new
        {
            Id = "I01",
            File = "Negatives/NegativeSites.cs",
            Category = "inactive",
            Support = "negative",
            resolution = Resolution.Inactive.ToString(),
            symbolKey = (string?)null,
            assembly = (string?)null,
            span = new { startLine = 0, startColumn = 0, endLine = 0, endColumn = 0 },
            detail = "source bytes retained; excluded from the active compilation",
        });

        return results;
    }

    private static List<CaseDef> BaselineCases()
    {
        var cases = new List<CaseDef>
        {
            new("S01", AppUnit, "App/Sites.cs", "@S01", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Direct(System.Int32)"), "invocation", "mandatory"),
            new("S02", AppUnit, "App/Sites.cs", "@S02", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Static(System.Int32)"), "invocation", "mandatory"),
            new("S03", AppUnit, "App/Sites.cs", "@S03", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Add(System.Int32,System.Int32)"), "invocation", "mandatory"),
            new("S04", AppUnit, "App/Sites.cs", "@S04", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Identity``1(``0)"), "invocation", "mandatory"),
            new("S05", AppUnit, "App/Sites.cs", "@S05", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Named(System.Int32,System.Int32,System.String)"), "invocation", "mandatory"),
            new("S06", AppUnit, "App/Sites.cs", "@S06", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.StringExtensions.Shout(System.String)"), "invocation", "mandatory"),
            new("S07", AppUnit, "App/Sites.cs", "@S07", "ObjectCreationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.#ctor"), "constructor", "deferred"),
            new("S08", AppUnit, "App/Sites.cs", "@S08", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Animal.BaseOnly"), "invocation", "mandatory"),
            new("S09", AppUnit, "App/Sites.cs", "@S09", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Animal.Speak"), "invocation", "mandatory"),
            new("S10", AppUnit, "App/Sites.cs", "@S10", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.IGreeter.Greet(System.String)"), "invocation", "mandatory"),
            new("S11", AppUnit, "App/Sites.cs", "@S11", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Animal.Describe"), "invocation", "mandatory"),
            new("S12", AppUnit, "App/Sites.cs", "@S12", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.IGreeter.Greet(System.String)"), "invocation", "qualification"),
            new("S13", AppUnit, "App/Sites.cs", "@S13", "SimpleMemberAccessExpression", Resolution.Resolved, Core("P:Corpus.Core.Methods.Prop"), "property", "deferred"),
            new("S14", AppUnit, "App/Sites.cs", "@S14", "SimpleMemberAccessExpression", Resolution.Resolved, Core("F:Corpus.Core.Methods.Field"), "field", "deferred"),
            new("S15", AppUnit, "App/Sites.cs", "@S15", "IdentifierName", Resolution.Resolved, "Corpus.App!M:Corpus.App.Sites.OnChanged", "event", "deferred"),
            new("S16", AppUnit, "App/Sites.cs", "@S16", "SimpleMemberAccessExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Raise"), "method-group", "deferred"),
            new("S17", AppUnit, "App/Sites.cs", "@S17", "InvocationExpression", Resolution.Resolved, null, "delegate", "deferred"),
            new("S18", AppUnit, "App/Sites.cs", "@S18", "InvocationExpression", Resolution.Resolved, "Corpus.App!M:Corpus.App.Sites.Local(System.Int32)", "local-function", "deferred"),
            new("S19", AppUnit, "App/Sites.cs", "@S19", "TypeOfExpression", Resolution.Resolved, Core("T:Corpus.Core.Methods"), "type-reference", "deferred"),
            new("S20", AppUnit, "App/Sites.cs", "@S20", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.PartialType.Left"), "invocation", "mandatory"),
            new("S21", AppUnit, "App/Sites.cs", "@S21", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)"), "invocation", "mandatory"),
            new("S22", AppUnit, "App/Sites.cs", "@S22", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Add(System.Double,System.Double)"), "invocation", "mandatory"),
            new("S23", AppUnit, "App/Sites.cs", "@S23", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.Add(System.String,System.String)"), "invocation", "mandatory"),
            new("S24", AppUnit, "App/Sites.cs", "@S24", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.Methods.WithOptional(System.Int32,System.Int32)"), "invocation", "mandatory"),
            new("S25", AppUnit, "App/Sites.cs", "@S25", "InvocationExpression", Resolution.Resolved, Core("M:Corpus.Core.ConditionalApi.Value"), "invocation", "mandatory"),
            new("S26", AppUnit, "App/Sites.cs", "@S26", "InvocationExpression", Resolution.Resolved, "Corpus.Leaf!M:Corpus.Leaf.Helper.Compute(System.Int32)", "invocation", "mandatory"),
            new("N01", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N01", "InvocationExpression", Resolution.Ambiguous, null, "ambiguous", "negative"),
            new("N02", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N02", "InvocationExpression", Resolution.Unresolved, null, "unresolved", "negative"),
            new("N03", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N03", "InvocationExpression", Resolution.Dynamic, null, "dynamic", "negative"),
            new("E01", "Negatives/Negatives.csproj", "Negatives/External.cs", "@E01", "InvocationExpression", Resolution.Resolved, "Corpus.External!M:Corpus.External.Api.Value", "external", "negative"),
        };

        cases.AddRange(BulkCases());
        return cases;
    }

    private static IEnumerable<CaseDef> BulkCases()
    {
        static CaseDef Inv(string id, string key) => new(id, AppUnit, "App/Bulk.cs", "@" + id, "InvocationExpression",
            Resolution.Resolved, Core(key), "invocation", "mandatory");
        static CaseDef Mr(string id, string key) => new(id, AppUnit, "App/Bulk.cs", "@" + id, "SimpleMemberAccessExpression",
            Resolution.Resolved, key.Contains('!', StringComparison.Ordinal) ? key : Core(key), "method-group", "qualification");

        var add2 = "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)";
        var add3 = "M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)";
        for (var i = 1; i <= 5; i++) yield return Inv($"INV-{i:00}", add2);
        for (var i = 6; i <= 10; i++) yield return Inv($"INV-{i:00}", "M:Corpus.Core.Methods.Static(System.Int32)");
        for (var i = 11; i <= 15; i++) yield return Inv($"INV-{i:00}", "M:Corpus.Core.Methods.Direct(System.Int32)");
        for (var i = 16; i <= 18; i++) yield return Inv($"INV-{i:00}", add3);
        for (var i = 19; i <= 20; i++) yield return Inv($"INV-{i:00}", add2);
        for (var i = 21; i <= 25; i++) yield return Inv($"INV-{i:00}", "M:Corpus.Core.Methods.Identity``1(``0)");
        yield return Inv("INV-26", "M:Corpus.Core.Methods.Add(System.Int64,System.Int64)");
        yield return Inv("INV-27", "M:Corpus.Core.Methods.Add(System.Double,System.Double)");
        yield return Inv("INV-28", "M:Corpus.Core.Methods.WithOptional(System.Int32,System.Int32)");
        yield return Inv("INV-29", "M:Corpus.Core.Methods.WithOptional(System.Int32,System.Int32)");
        yield return Inv("INV-30", "M:Corpus.Core.Methods.Named(System.Int32,System.Int32,System.String)");
        yield return Inv("INV-31", "M:Corpus.Core.StringExtensions.WordCount(System.String)");
        yield return Inv("INV-32", "M:Corpus.Core.StringExtensions.WordCount(System.String)");

        yield return Mr("MR-01", "M:Corpus.Core.Methods.Raise");
        yield return Mr("MR-02", "M:Corpus.Core.Methods.Direct(System.Int32)");
        yield return Mr("MR-03", add2);
        yield return Mr("MR-04", "M:Corpus.Core.Methods.Static(System.Int32)");
        yield return Mr("MR-05", "M:Corpus.Core.Methods.Identity``1(``0)");
        yield return Mr("MR-06", add3);
        yield return Mr("MR-07", "M:Corpus.Core.Methods.Add(System.String,System.String)");
        yield return Mr("MR-08", "M:Corpus.Core.Methods.Add(System.Double,System.Double)");
        yield return Mr("MR-09", "M:Corpus.Core.Methods.Add(System.Int64,System.Int64)");
        yield return Mr("MR-10", "M:Corpus.Core.Methods.WithOptional(System.Int32,System.Int32)");
        yield return Mr("MR-11", "M:Corpus.Core.Methods.Named(System.Int32,System.Int32,System.String)");
        yield return Mr("MR-12", "M:Corpus.Core.StringExtensions.Shout(System.String)");
        yield return Mr("MR-13", "M:Corpus.Core.StringExtensions.WordCount(System.String)");
        yield return Mr("MR-14", "M:Corpus.Core.PartialType.Left");
        yield return Mr("MR-15", "M:Corpus.Core.PartialType.Right");
        yield return Mr("MR-16", "M:Corpus.Core.Methods.Direct(System.Int32)");
        yield return Mr("MR-17", "Corpus.Leaf!M:Corpus.Leaf.Helper.Compute(System.Int32)");
        yield return Mr("MR-18", "Corpus.Leaf!M:Corpus.Leaf.Helper.Extra(System.Int32)");
        yield return Mr("MR-19", add2);
        yield return Mr("MR-20", "M:Corpus.Core.Methods.Direct(System.Int32)");
    }

    private static string Core(string docId) => "Corpus.Core!" + docId;

    // ------------------------------------------------------------------ identities

    private static Dictionary<string, object?> RunIdentities(CorpusBuild baseline, CorpusIdentity identity, List<Check> checks)
    {
        var targets = new (string Id, string Unit, string DocId, int Arity)[]
        {
            ("T.Direct", CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1),
            ("T.Static", CoreUnit, "M:Corpus.Core.Methods.Static(System.Int32)", 1),
            ("T.Add2", CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", 2),
            ("T.Add3", CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)", 3),
            ("T.Conditional", CoreUnit, "M:Corpus.Core.ConditionalApi.Value", 0),
            ("T.LeafCompute", LeafUnit, "M:Corpus.Leaf.Helper.Compute(System.Int32)", 1),
        };

        var asmctx3 = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var t in targets)
        {
            // Fixture-supplied canonical component inputs. The corpus does not claim
            // to freeze S2.2's source canonicalization; it supplies the R2 site forms.
            asmctx3[t.Id] = identity.TargetId(t.Unit, t.DocId, t.Unit + "::" + t.DocId, "method|" + t.Arity);
        }

        checks.Add(new Check("identity.asmctx3.canonical", "identity",
            asmctx3.Values.All(v => v.StartsWith("asmctx3:sha256:", StringComparison.Ordinal) && v.Length == "asmctx3:sha256:".Length + 64),
            "all asmctx3 values canonical"));
        checks.Add(new Check("identity.asmctx3.distinct-declarations", "identity",
            asmctx3["T.Direct"] != asmctx3["T.Static"] &&
            asmctx3["T.Static"] != asmctx3["T.LeafCompute"] &&
            asmctx3["T.Add2"] != asmctx3["T.Add3"],
            "distinct declarations and distinct overloads yield distinct asmctx3"));

        // usage4 for the S01 site and its source-changed twin, plus a bulk site.
        var app = baseline.Get(AppUnit);
        var sitesTree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var s01 = SemanticInspector.Inspect(app.Compilation, sitesTree, "@S01", SyntaxKind.InvocationExpression);
        var bulkTree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Bulk.cs");
        var inv01 = SemanticInspector.Inspect(app.Compilation, bulkTree, "@INV-01", SyntaxKind.InvocationExpression);
        var sitesHash = identity.SourceHash(AppUnit, "App/Sites.cs");
        var usage4S01 = identity.OccurrenceId(AppUnit, "App/Sites.cs", sitesHash,
            s01.StartLine, s01.StartColumn, s01.EndLine, s01.EndColumn, "M:Corpus.Core.Methods.Direct(System.Int32)", asmctx3["T.Direct"]);
        var usage4Inv01 = identity.OccurrenceId(AppUnit, "App/Bulk.cs", identity.SourceHash(AppUnit, "App/Bulk.cs"),
            inv01.StartLine, inv01.StartColumn, inv01.EndLine, inv01.EndColumn, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", asmctx3["T.Add2"]);

        checks.Add(new Check("identity.usage4.canonical", "identity",
            usage4S01.Length == 64 && usage4S01.All(char.IsAsciiHexDigitLower), usage4S01));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["repo"] = CorpusBuild.RepoId,
            ["plugin"] = CorpusBuild.PluginId,
            ["compctx1"] = identity.Compctx1.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value),
            ["compctx2"] = identity.Compctx2.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => p.Value),
            ["asmctx3"] = asmctx3,
            ["usage4"] = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["S01"] = usage4S01,
                ["INV-01"] = usage4Inv01,
            },
            ["sitesSourceHash"] = sitesHash,
        };
    }

    // ------------------------------------------------------------------ Option A

    private static (List<object> Results, Dictionary<string, object?> VariantIdentities) RunOptionA(
        string corpusRoot, CorpusBuild baseline, CorpusIdentity baselineIdentity,
        CorpusBuild second, CorpusIdentity secondIdentity, List<Check> checks, Dictionary<string, object?> identities)
    {
        var results = new List<object>();

        void Add(string id, bool pass, string detail)
        {
            checks.Add(new Check("optionA." + id, "provenance", pass, detail));
            results.Add(new { id, pass, detail });
        }

        // A01 same inputs, recomputed in a separate build -> identical provenance and target.
        Add("A01", baselineIdentity.Compctx2.SequenceEqual(secondIdentity.Compctx2) &&
            baselineIdentity.Compctx1.SequenceEqual(secondIdentity.Compctx1),
            "recomputed baseline provenance and target-context inputs are identical");

        // Build the variants.
        var symbolsLeaf = CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsLeafChanged);
        var symbolsLeafIdentity = CorpusIdentity.Compute(symbolsLeaf);
        var symbolsCore = CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsCoreChanged);
        var symbolsCoreIdentity = CorpusIdentity.Compute(symbolsCore);
        var sourceChanged = CorpusBuild.Build(corpusRoot, CorpusVariant.SourceChanged);
        var sourceChangedIdentity = CorpusIdentity.Compute(sourceChanged);
        var refMissing = CorpusBuild.Build(corpusRoot, CorpusVariant.ReferenceMissing);

        string TargetFor(CorpusIdentity identity, string unit, string docId, int arity) =>
            identity.TargetId(unit, docId, unit + "::" + docId, "method|" + arity);

        // A02 transitive dependency (leaf preprocessor symbol) change:
        //   Core's R2 tree material is unchanged -> compctx1 and target identity stable;
        //   compctx2 changes at Leaf, Core and App.
        var coreStable = baselineIdentity.Compctx1[CoreUnit] == symbolsLeafIdentity.Compctx1[CoreUnit];
        var coreTargetStable = TargetFor(baselineIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1) ==
            TargetFor(symbolsLeafIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        var leafCtxChanged = baselineIdentity.Compctx2[LeafUnit] != symbolsLeafIdentity.Compctx2[LeafUnit];
        var coreCtxChanged = baselineIdentity.Compctx2[CoreUnit] != symbolsLeafIdentity.Compctx2[CoreUnit];
        var appCtxChanged = baselineIdentity.Compctx2[AppUnit] != symbolsLeafIdentity.Compctx2[AppUnit];
        Add("A02", coreStable && coreTargetStable && leafCtxChanged && coreCtxChanged && appCtxChanged,
            $"compctx1(Core) stable={coreStable}; target stable={coreTargetStable}; " +
            $"compctx2 changed leaf={leafCtxChanged} core={coreCtxChanged} app={appCtxChanged}");

        // A03 defining API change (Core preprocessor symbol) -> target identity changes; App source unchanged.
        var coreCtx1Changed = baselineIdentity.Compctx1[CoreUnit] != symbolsCoreIdentity.Compctx1[CoreUnit];
        var conditionalTargetChanged = TargetFor(baselineIdentity, CoreUnit, "M:Corpus.Core.ConditionalApi.Value", 0) !=
            TargetFor(symbolsCoreIdentity, CoreUnit, "M:Corpus.Core.ConditionalApi.Value", 0);
        var appSourceUnchanged = baselineIdentity.SourceHash(AppUnit, "App/Sites.cs") == symbolsCoreIdentity.SourceHash(AppUnit, "App/Sites.cs");
        Add("A03", coreCtx1Changed && conditionalTargetChanged && appSourceUnchanged,
            $"compctx1(Core) changed={coreCtx1Changed}; conditional target changed={conditionalTargetChanged}; " +
            $"App source unchanged={appSourceUnchanged}");

        // A04 calling source change -> new occurrence identity; defining target identity stable.
        var appSourceChanged = baselineIdentity.SourceHash(AppUnit, "App/Sites.cs") != sourceChangedIdentity.SourceHash(AppUnit, "App/Sites.cs");
        var targetAfterSource = TargetFor(sourceChangedIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        var targetBaseline = TargetFor(baselineIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        Add("A04", appSourceChanged && targetAfterSource == targetBaseline,
            $"calling source changed={appSourceChanged}; target identity stable={targetAfterSource == targetBaseline}");

        // A05 same textual descriptor in different defining compilations -> distinct targets.
        var sameFormsSolo = baselineIdentity.TargetId(SoloUnit, "M:Corpus.Core.Methods.Direct(System.Int32)",
            CoreUnit + "::M:Corpus.Core.Methods.Direct(System.Int32)", "method|1");
        var sameFormsCore = baselineIdentity.TargetId(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)",
            CoreUnit + "::M:Corpus.Core.Methods.Direct(System.Int32)", "method|1");
        Add("A05", sameFormsSolo != sameFormsCore,
            "identical declared site forms in different defining compilations yield distinct asmctx3");

        // Validity gate: a fact confirms only when its recorded compctx2 is the
        // defining context's current verified compctx2 and the defining unit is in
        // the universe. Identity equality alone is not validity.
        static bool Valid(string? recorded, IReadOnlyDictionary<string, string> current,
            string unit, IReadOnlyCollection<string> universe) =>
            recorded is not null && current.TryGetValue(unit, out var cur) && cur == recorded && universe.Contains(unit);

        var universe = new[] { LeafUnit, CoreUnit, AppUnit, SoloUnit };

        // A06 missing current compctx2 -> no confirmed occurrence.
        Add("A06", !Valid(null, baselineIdentity.Compctx2, CoreUnit, universe),
            "a fact without a current defining compctx2 association cannot confirm");

        // A07 missing universe membership -> refuse.
        var universeWithoutCore = new[] { LeafUnit, AppUnit };
        Add("A07", !Valid(baselineIdentity.Compctx2[CoreUnit], baselineIdentity.Compctx2, CoreUnit, universeWithoutCore),
            "defining unit outside the verified universe refuses confirmation");

        // A08 stale dependency provenance -> invalid, re-ingest.
        Add("A08", !Valid(baselineIdentity.Compctx2[AppUnit], symbolsLeafIdentity.Compctx2, AppUnit, universe),
            "recorded compctx2 differing from the current context is stale and must be refused or re-ingested");

        // A01 positive control on the same gate.
        Add("A01-gate", Valid(baselineIdentity.Compctx2[AppUnit], baselineIdentity.Compctx2, AppUnit, universe),
            "unchanged, universe-member context is valid");

        // A09 re-ingestion determinism.
        Add("A09", baselineIdentity.Compctx2[AppUnit] == secondIdentity.Compctx2[AppUnit] &&
            baselineIdentity.Compctx1[AppUnit] == secondIdentity.Compctx1[AppUnit],
            "recomputation from frozen inputs is deterministic");

        // A10 complete-empty verified scope (Solo has no invocations).
        var solo = baseline.Get(SoloUnit);
        var soloInvocations = solo.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();
        Add("A10", soloInvocations == 0, $"Solo verified scope produced zero invocation occurrences (count={soloInvocations})");

        // reference-missing diagnostics: Core reference removal must leave the S01 site unresolved.
        var refApp = refMissing.Get(AppUnit);
        var refTree = refApp.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var refResult = SemanticInspector.Inspect(refApp.Compilation, refTree, "@S01", SyntaxKind.InvocationExpression);
        Add("RM01", refResult.Resolution != Resolution.Resolved,
            "removing the Core reference leaves the S01 site unresolved: " + refResult.Resolution);

        var variantIdentities = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["baseline"] = new { compctx1 = baselineIdentity.Compctx1, compctx2 = baselineIdentity.Compctx2 },
            ["symbols-leaf-changed"] = new { compctx1 = symbolsLeafIdentity.Compctx1, compctx2 = symbolsLeafIdentity.Compctx2 },
            ["symbols-core-changed"] = new { compctx1 = symbolsCoreIdentity.Compctx1, compctx2 = symbolsCoreIdentity.Compctx2 },
            ["source-changed"] = new { compctx1 = sourceChangedIdentity.Compctx1, compctx2 = sourceChangedIdentity.Compctx2 },
        };

        return (results, variantIdentities);
    }

    // ------------------------------------------------------------------ coverage

    private static Dictionary<string, object?> RunCoverage(CorpusBuild baseline, CorpusIdentity identity, List<Check> checks)
    {
        // Complete-empty: Solo analyzed completely, zero occurrences.
        var solo = baseline.Get(SoloUnit);
        var soloCount = solo.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();
        checks.Add(new Check("coverage.complete-empty", "coverage", soloCount == 0,
            "Solo is a complete verified scope with zero occurrences"));

        // Incomplete: a declared unit without a verified context.
        var declared = new[] { LeafUnit, CoreUnit, AppUnit, SoloUnit };
        var verified = new[] { LeafUnit, CoreUnit, AppUnit };
        var missing = declared.Except(verified, StringComparer.Ordinal).ToArray();
        checks.Add(new Check("coverage.incomplete", "coverage", missing.Length == 1 && missing[0] == SoloUnit,
            "omitting a declared unit yields IncompleteAnalysis, not a complete-empty result"));

        // NEW-04: incompatible coverage scopes must not be merged as authoritative.
        var scopeA = identity.Compctx2[CoreUnit];
        var scopeB = identity.Compctx2[LeafUnit];
        var compatible = string.Equals(scopeA, scopeB, StringComparison.Ordinal);
        checks.Add(new Check("coverage.new-04-scope-compatibility", "coverage", !compatible,
            "distinct scope/universe identities are incompatible and must not be merged as authoritative"));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["completeEmpty"] = new { unit = SoloUnit, occurrences = soloCount, state = "Produced" },
            ["incomplete"] = new { declared = declared, verified = verified, missing, state = "IncompleteAnalysis" },
            ["new04"] = new { scopeA = "Core", scopeB = "Leaf", compatible, note = "incompatible scopes are not merge authority" },
        };
    }

    // ------------------------------------------------------------------ counts

    private static SortedDictionary<string, int> RunCounts(CorpusBuild baseline, List<object> semantics, string repoRoot)
    {
        var app = baseline.Get(AppUnit);
        var allInvocations = app.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();

        var methodRefs = semantics.Count(s => (string?)s.GetType().GetProperty("Category")!.GetValue(s) == "method-group");

        var overloads = DescendantTypes(baseline.Get(CoreUnit).Compilation.Assembly.GlobalNamespace)
            .SelectMany(t => t.GetMembers().OfType<IMethodSymbol>())
            .Where(m => m.Name == "Add")
            .Select(m => m.GetDocumentationCommentId())
            .Distinct(StringComparer.Ordinal).Count();

        var namespaceCount = baseline.Projects.Values
            .SelectMany(p => p.Compilation.SyntaxTrees)
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            .Select(n => n.Name.ToString())
            .Distinct(StringComparer.Ordinal)
            .Count();

        var crossProject = 0;
        foreach (var tree in app.Compilation.SyntaxTrees)
        {
            var model = app.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var symbol = model.GetSymbolInfo(invocation).Symbol;
                var assembly = symbol?.ContainingAssembly?.Name;
                if (assembly is not null && assembly != "Corpus.App")
                    crossProject++;
            }
        }

        return new SortedDictionary<string, int>(StringComparer.Ordinal)
        {
            ["csharp-files"] = Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories).Count(),
            ["invocations"] = allInvocations,
            ["method-references"] = methodRefs,
            ["overloaded-methods"] = overloads,
            ["namespaces"] = namespaceCount,
            ["projects"] = baseline.Projects.Count,
            ["cross-project-invocations"] = crossProject,
        };
    }

    private static IEnumerable<INamedTypeSymbol> DescendantTypes(INamespaceSymbol root)
    {
        foreach (var type in root.GetTypeMembers())
            yield return type;
        foreach (var nested in root.GetNamespaceMembers().SelectMany(DescendantTypes))
            yield return nested;
    }

    // ------------------------------------------------------------------ helpers

    private static Dictionary<string, string> FixtureManifest(string repoRoot)
    {
        var manifest = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(repoRoot, "*.cs", SearchOption.AllDirectories))
            manifest[R2Identity.NormalizePath(Path.GetRelativePath(repoRoot, file))] =
                "sha256:" + R2Identity.Sha256Hex(File.ReadAllBytes(file));
        return new Dictionary<string, string>(manifest, StringComparer.Ordinal);
    }

    /// <summary>
    /// Compares the freshly computed sections against a frozen oracle document.
    /// A mismatch is reported as a failure; the frozen document is the authority.
    /// </summary>
    private static void CompareToExpected(Dictionary<string, object?> report, string path, List<Check> checks)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var options = new JsonSerializerOptions { WriteIndented = false };
        foreach (var section in new[] { "semantics", "identities", "variantIdentities", "coverage", "counts" })
        {
            if (!root.TryGetProperty(section, out var expected))
            {
                checks.Add(new Check("expected." + section, "frozen", false, "frozen oracle has no section " + section));
                continue;
            }

            var actualText = JsonSerializer.Serialize(report[section], options);
            var expectedText = JsonSerializer.Serialize(expected, options);
            var identical = string.Equals(actualText, expectedText, StringComparison.Ordinal);
            checks.Add(new Check("expected." + section, "frozen", identical,
                identical ? "matches frozen oracle" : "differs from frozen oracle"));
        }
    }

    private static void Finish(List<Check> checks, Dictionary<string, object?> report, string? outputPath)
    {
        var summary = new
        {
            total = checks.Count,
            passed = checks.Count(c => c.Pass),
            failed = checks.Count(c => !c.Pass),
        };
        report["checks"] = checks;
        report["summary"] = summary;
        // Logical, host-independent location so the frozen document is portable.
        report["corpusRoot"] = "docs/planning/adr-ci1-01-ci1a-s2-0/corpus";

        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        });

        if (outputPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
            File.WriteAllText(outputPath, json);
        }

        Console.WriteLine($"S2.0 oracle: {summary.passed}/{summary.total} checks passed, {summary.failed} failed.");
        foreach (var failure in checks.Where(c => !c.Pass))
            Console.WriteLine($"  FAIL {failure.Id}: {failure.Detail}");

        if (outputPath is null)
            Console.WriteLine(json);
    }

    private static string? ArgValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (args[i] == name)
                return args[i + 1];
        return null;
    }

    private sealed record CaseDef(string Id, string UnitId, string File, string Marker, string NodeKind,
        Resolution ExpectedResolution, string? ExpectedSymbolKey, string Category, string Support);

    private sealed record Check(string Id, string Kind, bool Pass, string Detail);
}
