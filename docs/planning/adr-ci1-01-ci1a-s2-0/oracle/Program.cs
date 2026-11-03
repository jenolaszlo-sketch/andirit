using System.Globalization;
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

        // ---- cross-view canonical occurrence consistency --------------------
        var baselineFacts = AppFacts(baseline, baselineIdentity);
        var crossView = RunCrossView(corpusRoot, baseline, baselineIdentity, baselineFacts, checks);

        // ---- E02 defining-provenance authority ------------------------------
        var e02 = RunE02(corpusRoot, baseline, baselineIdentity, checks);

        // ---- coverage -------------------------------------------------------
        var lifecycle = RunLifecycle(corpusRoot, baseline, baselineIdentity, baselineFacts, checks);
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

        report["oracleVersion"] = "CI1A-S2.0-CORPUS-R2";
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
        report["crossView"] = crossView;
        report["e02"] = e02;
        report["coverage"] = coverage;
        report["lifecycle"] = lifecycle;
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

        // S15 event target and handler method are separate Roslyn symbols.
        var s15Tree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var s15Model = app.Compilation.GetSemanticModel(s15Tree);
        var handlerNode = s15Tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
            .Single(n => n.Identifier.ValueText == "OnChanged" && n.Parent is AssignmentExpressionSyntax);
        var handlerInfo = s15Model.GetSymbolInfo(handlerNode);
        var handlerKey = handlerInfo.Symbol is null ? null : SemanticInspector.SymbolKey(handlerInfo.Symbol);
        var eventNode = SemanticInspector.Locate(s15Tree, "@S15", SyntaxKind.SimpleMemberAccessExpression);
        var eventKey = eventNode is null ? null : SemanticInspector.SymbolKey(s15Model.GetSymbolInfo(eventNode).Symbol!);
        var eventHandlerDistinct = eventKey == Core("E:Corpus.Core.Methods.Changed") &&
            handlerKey == "Corpus.App!M:Corpus.App.Sites.OnChanged" && eventKey != handlerKey;
        checks.Add(new Check("semantic.S15-handler", "semantic", eventHandlerDistinct,
            $"event={eventKey}; handler={handlerKey}; distinct={eventKey != handlerKey}"));
        results.Add(new
        {
            Id = "S15-handler",
            File = "App/Sites.cs",
            Category = "event-handler-method",
            Support = "qualification",
            resolution = handlerInfo.Symbol is null ? Resolution.Unresolved.ToString() : Resolution.Resolved.ToString(),
            symbolKey = handlerKey,
            assembly = handlerInfo.Symbol?.ContainingAssembly?.Name,
            span = new { startLine = 0, startColumn = 0, endLine = 0, endColumn = 0 },
            detail = "handler method is a distinct symbol from the subscribed event member",
        });

        // E02 is an actual Roslyn resolution with missing authoritative defining-context provenance.
        var e02Tree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var e02 = SemanticInspector.Inspect(app.Compilation, e02Tree, "@S01", SyntaxKind.InvocationExpression);
        var e02Pass = e02.Resolution == Resolution.Resolved && e02.Assembly == "Corpus.Core";
        checks.Add(new Check("semantic.E02", "semantic", e02Pass,
            $"Roslyn resolves {e02.SymbolKey}; publication refusal is exercised separately by the missing-provenance E02 gate"));
        results.Add(new
        {
            Id = "E02",
            File = "App/Sites.cs",
            Category = "resolved-external-to-requesting-project-missing-defining-provenance",
            Support = "negative",
            resolution = e02.Resolution.ToString(),
            symbolKey = e02.SymbolKey,
            assembly = e02.Assembly,
            span = new { startLine = e02.StartLine, startColumn = e02.StartColumn, endLine = e02.EndLine, endColumn = e02.EndColumn },
            detail = "resolved project target; authoritative defining compctx2 and universe membership intentionally unavailable; confirmed publication refused",
        });

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
            new("S15", AppUnit, "App/Sites.cs", "@S15", "SimpleMemberAccessExpression", Resolution.Resolved, Core("E:Corpus.Core.Methods.Changed"), "event-member", "deferred"),
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
            new("CLS01", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N01", "InvocationExpression", Resolution.Ambiguous, null, "genuine-ambiguity", "negative"),
            new("N02", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N02", "InvocationExpression", Resolution.Unresolved, null, "unresolved", "negative"),
            new("N03", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@N03", "InvocationExpression", Resolution.Dynamic, null, "dynamic", "negative"),
            new("CLS02", "Negatives/Negatives.csproj", "Negatives/NegativeSites.cs", "@CLS02", "InvocationExpression", Resolution.InvalidInvocation, null, "invalid-invocation", "negative"),
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
        var targets = new (string Id, string Unit, string DocId, int ParameterCount)[]
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
            asmctx3[t.Id] = CanonicalTarget.TargetId(identity,
                CanonicalTarget.Fixture(t.Unit, t.DocId, t.ParameterCount));
        }

        checks.Add(new Check("identity.asmctx3.canonical", "identity",
            asmctx3.Values.All(v => v.StartsWith("asmctx3:sha256:", StringComparison.Ordinal) && v.Length == "asmctx3:sha256:".Length + 64),
            "all asmctx3 values canonical"));
        checks.Add(new Check("identity.asmctx3.frozen-vectors", "identity",
            targets.All(t => string.Equals(asmctx3[t.Id], FrozenExpectations.Asmctx3[t.Id], StringComparison.Ordinal)),
            "all six named asmctx3 vectors equal the accepted frozen R2 contract values"));
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
        var directTarget = CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        var add2Target = CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", 2);
        var usage4S01 = CanonicalTarget.OccurrenceId(identity, directTarget, AppUnit, "App/Sites.cs",
            sitesHash, s01.StartLine, s01.StartColumn, s01.EndLine, s01.EndColumn);
        var usage4Inv01 = CanonicalTarget.OccurrenceId(identity, add2Target, AppUnit, "App/Bulk.cs",
            identity.SourceHash(AppUnit, "App/Bulk.cs"),
            inv01.StartLine, inv01.StartColumn, inv01.EndLine, inv01.EndColumn);

        checks.Add(new Check("identity.usage4.canonical", "identity",
            usage4S01.Length == 64 && usage4S01.All(char.IsAsciiHexDigitLower), usage4S01));
        checks.Add(new Check("identity.usage4.frozen-vectors", "identity",
            string.Equals(usage4S01, FrozenExpectations.Usage4["S01"], StringComparison.Ordinal) &&
            string.Equals(usage4Inv01, FrozenExpectations.Usage4["INV-01"], StringComparison.Ordinal),
            "S01/INV-01 usage4 equal the independently qualified frozen main identity vectors"));

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
        var optionsChanged = CorpusBuild.Build(corpusRoot, CorpusVariant.OptionsChanged);
        var optionsChangedIdentity = CorpusIdentity.Compute(optionsChanged);

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

        // OPT01 changes a material Roslyn compilation option with identical source bytes and parse inputs.
        var optionProjectBefore = baseline.Get(AppUnit);
        var optionProjectAfter = optionsChanged.Get(AppUnit);
        var optionDigestBefore = R2Identity.OptionsDigest((CSharpCompilationOptions)optionProjectBefore.Compilation.Options);
        var optionDigestAfter = R2Identity.OptionsDigest((CSharpCompilationOptions)optionProjectAfter.Compilation.Options);
        var optionSourcesSame = optionProjectBefore.SourcePaths.SequenceEqual(optionProjectAfter.SourcePaths, StringComparer.Ordinal) &&
            optionProjectBefore.SourcePaths.All(path => optionProjectBefore.SourceBytes[path].SequenceEqual(optionProjectAfter.SourceBytes[path]));
        var optionParseSame = optionProjectBefore.Compilation.SyntaxTrees.All(tree =>
            ((CSharpParseOptions)tree.Options).Equals((CSharpParseOptions)optionProjectAfter.Compilation.SyntaxTrees.Single(t => t.FilePath == tree.FilePath).Options));
        var optionChanged = ((CSharpCompilationOptions)optionProjectAfter.Compilation.Options).CheckOverflow;
        var optionCtxChanged = optionDigestBefore != optionDigestAfter &&
            baselineIdentity.Compctx1[AppUnit] != optionsChangedIdentity.Compctx1[AppUnit] &&
            baselineIdentity.Compctx2[AppUnit] != optionsChangedIdentity.Compctx2[AppUnit];
        var optionBinding = SemanticInspector.Inspect(optionProjectAfter.Compilation,
            optionProjectAfter.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs"), "@S01", SyntaxKind.InvocationExpression);
        Add("OPT01", optionChanged && optionSourcesSame && optionParseSame && optionCtxChanged &&
            optionBinding.Resolution == Resolution.Resolved && optionBinding.SymbolKey == Core("M:Corpus.Core.Methods.Direct(System.Int32)"),
            $"CheckOverflow={optionChanged}; source bytes same={optionSourcesSame}; parse options same={optionParseSame}; optionsDigest changed={optionDigestBefore != optionDigestAfter}; compctx1/2 App changed={optionCtxChanged}; binding preserved={optionBinding.SymbolKey}");
        var optionTargetChanged = TargetFor(baselineIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1) !=
            TargetFor(optionsChangedIdentity, CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        Add("OPT01-target", optionTargetChanged,
            "defining compilation options participate in compctx1, so asmctx3 changes under the accepted R2 tuple");
        Add("OPT01-restore", baselineIdentity.Compctx1[AppUnit] == secondIdentity.Compctx1[AppUnit] &&
            baselineIdentity.Compctx2[AppUnit] == secondIdentity.Compctx2[AppUnit],
            "restoring baseline compilation options reproduces baseline compctx1/compctx2");

        // E02: Roslyn resolves a project-defined symbol, but an authoritative manifest without its defining context refuses publication.
        var e02Tree = baseline.Get(AppUnit).Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var e02Binding = SemanticInspector.Inspect(baseline.Get(AppUnit).Compilation, e02Tree, "@S01", SyntaxKind.InvocationExpression);
        var noCoreUniverse = ProvenanceGate.Universe(baselineIdentity, LeafUnit, AppUnit, SoloUnit);
        var e02Refused = e02Binding.Resolution == Resolution.Resolved && e02Binding.Assembly == "Corpus.Core" &&
            !ProvenanceGate.Eligible(null, baselineIdentity.Compctx2, noCoreUniverse, CoreUnit);
        var verifiedDefiningContext = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [CoreUnit] = baselineIdentity.Compctx2[CoreUnit],
        };
        var fullUniverse = ProvenanceGate.Universe(baselineIdentity, LeafUnit, CoreUnit, AppUnit, SoloUnit);
        var e02Positive = e02Binding.Resolution == Resolution.Resolved &&
            ProvenanceGate.Eligible(verifiedDefiningContext[CoreUnit], baselineIdentity.Compctx2, fullUniverse, CoreUnit);
        Add("E02", e02Refused, $"Roslyn resolved {e02Binding.SymbolKey}; defining project is Core; authoritative compctx2/universe absent; confirmed target refused");
        Add("E02-positive", e02Positive, "adding the verified defining compctx2 and universe membership changes provenance eligibility");

        // A05 same textual descriptor in different defining compilations -> distinct targets.
        var sameFormsSolo = baselineIdentity.TargetId(SoloUnit, "M:Corpus.Core.Methods.Direct(System.Int32)",
            CoreUnit + "::M:Corpus.Core.Methods.Direct(System.Int32)", "method|1");
        var sameFormsCore = baselineIdentity.TargetId(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)",
            CoreUnit + "::M:Corpus.Core.Methods.Direct(System.Int32)", "method|1");
        Add("A05", sameFormsSolo != sameFormsCore,
            "identical declared site forms in different defining compilations yield distinct asmctx3");

        // Validity gate: a fact confirms only when its recorded compctx2 is the
        // defining context's current verified compctx2 and the universe membership
        // carries that same verified context. Identity equality alone is not validity.
        var universe = ProvenanceGate.Universe(baselineIdentity, LeafUnit, CoreUnit, AppUnit, SoloUnit);

        // A06 missing current compctx2 -> no confirmed occurrence.
        Add("A06", !ProvenanceGate.Eligible(null, baselineIdentity.Compctx2, universe, CoreUnit),
            "a fact without a current defining compctx2 association cannot confirm");

        // A07 missing universe membership -> refuse.
        var universeWithoutCore = ProvenanceGate.Universe(baselineIdentity, LeafUnit, AppUnit);
        Add("A07", !ProvenanceGate.Eligible(baselineIdentity.Compctx2[CoreUnit], baselineIdentity.Compctx2, universeWithoutCore, CoreUnit),
            "defining unit outside the verified universe refuses confirmation");

        // A08 stale dependency provenance -> invalid, re-ingest.
        Add("A08", !ProvenanceGate.Eligible(baselineIdentity.Compctx2[AppUnit], symbolsLeafIdentity.Compctx2, universe, AppUnit),
            "recorded compctx2 differing from the current context is stale and must be refused or re-ingested");

        // A01 positive control on the same gate.
        Add("A01-gate", ProvenanceGate.Eligible(baselineIdentity.Compctx2[AppUnit], baselineIdentity.Compctx2, universe, AppUnit),
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
            ["options-changed"] = new { compctx1 = optionsChangedIdentity.Compctx1, compctx2 = optionsChangedIdentity.Compctx2 },
        };

        return (results, variantIdentities);
    }

    // --------------------------------------------------------------- deletion lifecycle

    private static Dictionary<string, object?> RunLifecycle(string corpusRoot, CorpusBuild baseline,
        CorpusIdentity baselineIdentity, IReadOnlyDictionary<string, LifecycleFact[]> baselineFacts,
        List<Check> checks)
    {
        var reduced = CorpusBuild.Build(corpusRoot, CorpusVariant.SourceDeleted);
        var reducedAgain = CorpusBuild.Build(corpusRoot, CorpusVariant.SourceDeleted);
        var reducedIdentity = CorpusIdentity.Compute(reduced);
        var reducedAgainIdentity = CorpusIdentity.Compute(reducedAgain);
        var before = ProjectUsage4(baselineFacts);
        var afterFacts = AppFacts(reduced, reducedIdentity);
        var after = ProjectUsage4(afterFacts);
        var priorPaths = baseline.Get(AppUnit).SourcePaths.Order(StringComparer.Ordinal).ToArray();
        var currentPaths = reduced.Get(AppUnit).SourcePaths.Order(StringComparer.Ordinal).ToArray();
        var projectUniverse = baseline.Projects.Keys.Order(StringComparer.Ordinal).ToArray();
        var removed = priorPaths.Except(currentPaths, StringComparer.Ordinal).ToArray();
        var expectedAfter = before.Where(pair => currentPaths.Contains(pair.Key, StringComparer.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var preserved = expectedAfter.TryGetValue("App/Bulk.cs", out var expectedBulk) &&
            after.TryGetValue("App/Bulk.cs", out var actualBulk) && expectedBulk.SequenceEqual(actualBulk, StringComparer.Ordinal);
        var deletedAbsent = !after.ContainsKey("App/Sites.cs") && removed.SequenceEqual(["App/Sites.cs"]);
        var complete = currentPaths.SequenceEqual(reduced.Get(AppUnit).SourcePaths, StringComparer.Ordinal) &&
            reduced.Get(AppUnit).Compilation.GetDiagnostics().All(d => d.Severity != DiagnosticSeverity.Error);
        var stale = after.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        stale["App/Sites.cs"] = before["App/Sites.cs"];
        var staleRejected = !ProjectionMatches(expectedAfter, stale);
        var declaredPrior = priorPaths.ToHashSet(StringComparer.Ordinal);
        var acquiredWithMissingRequired = priorPaths.Where(path => path != "App/Sites.cs").ToHashSet(StringComparer.Ordinal);
        var missingRequiredIncomplete = !declaredPrior.SetEquals(acquiredWithMissingRequired);
        var deterministic = reducedIdentity.Compctx1[AppUnit] == reducedAgainIdentity.Compctx1[AppUnit] &&
            reducedIdentity.Compctx2[AppUnit] == reducedAgainIdentity.Compctx2[AppUnit] &&
            ProjectionMatches(after, ProjectUsage4(AppFacts(reducedAgain, reducedAgainIdentity)));
        var sourceSetIdentityChanged = baselineIdentity.Compctx1[AppUnit] != reducedIdentity.Compctx1[AppUnit] &&
            baselineIdentity.Compctx2[AppUnit] != reducedIdentity.Compctx2[AppUnit];
        var sitesBefore = before.GetValueOrDefault("App/Sites.cs", []).Length;
        var bulkBefore = before.GetValueOrDefault("App/Bulk.cs", []).Length;
        var bulkAfter = after.GetValueOrDefault("App/Bulk.cs", []).Length;
        var countsPreserved = sitesBefore == 20 && bulkBefore == 32 && bulkAfter == 32 && !after.ContainsKey("App/Sites.cs");

        checks.Add(new Check("lifecycle.DEL-N1", "lifecycle", deletedAbsent && complete &&
            !after.ContainsKey("App/Sites.cs"), "authoritative manifest excludes Sites.cs; reduced App compilation is complete; removed source-owned facts absent"));
        checks.Add(new Check("lifecycle.DEL-N2", "lifecycle", missingRequiredIncomplete,
            "manifest still declares Sites.cs but acquisition omits it, so the scope is incomplete"));
        checks.Add(new Check("lifecycle.DEL-N3", "lifecycle", staleRejected,
            "injecting prior Sites.cs contributions into the reduced snapshot fails the source-owned projection comparison"));
        checks.Add(new Check("lifecycle.DEL-counts", "lifecycle", countsPreserved,
            $"Snapshot A source-owned facts Sites={sitesBefore} Bulk={bulkBefore}; Snapshot B Bulk={bulkAfter} and no Sites.cs"));
        checks.Add(new Check("lifecycle.preserved", "lifecycle", preserved,
            "App/Bulk.cs source-owned qualifying occurrences remain represented with their corrected canonical usage4 identities"));
        checks.Add(new Check("lifecycle.identity", "lifecycle", sourceSetIdentityChanged,
            "source-set deletion recomputes App compctx1/compctx2 under R2; surviving usage4 identities are derived independently"));
        checks.Add(new Check("lifecycle.deterministic", "lifecycle", deterministic,
            "repeating reduced-scope compilation reproduces canonical contexts and source-owned facts"));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["caseId"] = "DEL01",
            ["snapshotA"] = new { authoritativeSourceUnits = priorPaths, sourceHashes = baseline.Get(AppUnit).SourcePaths.ToDictionary(path => path, path => baselineIdentity.SourceHash(AppUnit, path), StringComparer.Ordinal), sourceOwnedOccurrences = before, unitId = AppUnit, projectUniverse, verifiedProjectUnits = projectUniverse, complete = true, compctx1 = baselineIdentity.Compctx1[AppUnit], compctx2 = baselineIdentity.Compctx2[AppUnit] },
            ["snapshotB"] = new { authoritativeSourceUnits = currentPaths, sourceHashes = reduced.Get(AppUnit).SourcePaths.ToDictionary(path => path, path => reducedIdentity.SourceHash(AppUnit, path), StringComparer.Ordinal), sourceOwnedOccurrences = after, removedSourceUnits = removed, unitId = AppUnit, projectUniverse, verifiedProjectUnits = projectUniverse, complete, compctx1 = reducedIdentity.Compctx1[AppUnit], compctx2 = reducedIdentity.Compctx2[AppUnit] },
            ["removedContributions"] = before.GetValueOrDefault("App/Sites.cs", []),
            ["preservedContributions"] = after.GetValueOrDefault("App/Bulk.cs", []),
            ["negativeControls"] = new { DEL_N1 = deletedAbsent, DEL_N2 = missingRequiredIncomplete, DEL_N3 = staleRejected },
            ["counts"] = new { sitesBefore, bulkBefore, bulkAfter },
            ["deterministic"] = deterministic,
            ["identityContract"] = "usage4 identities are recomputed from R2 inputs; no stability is assumed across changed source identities",
        };
    }

    /// <summary>One source-owned occurrence as the lifecycle view represents it.</summary>
    private sealed record LifecycleFact(
        string Usage4,
        string DefiningUnit,
        string DocumentId,
        string DeclarationForm,
        string Shape,
        string Asmctx3,
        int StartLine,
        int StartColumn,
        int EndLine,
        int EndColumn);

    private static Dictionary<string, string[]> ProjectUsage4(
        IReadOnlyDictionary<string, LifecycleFact[]> facts) =>
        facts.ToDictionary(pair => pair.Key, pair => pair.Value.Select(f => f.Usage4).ToArray(),
            StringComparer.Ordinal);

    /// <summary>
    /// The source lifecycle projection. It now uses the single
    /// <see cref="CanonicalTarget"/> pathway (unreduced defining declaration,
    /// fixture parameter-count shape) instead of Roslyn generic arity and a raw
    /// <c>OriginalDefinition</c>.
    /// </summary>
    private static Dictionary<string, LifecycleFact[]> AppFacts(CorpusBuild build, CorpusIdentity identity)
    {
        var app = build.Get(AppUnit);
        var facts = new SortedDictionary<string, List<LifecycleFact>>(StringComparer.Ordinal);
        foreach (var tree in app.Compilation.SyntaxTrees.OrderBy(t => t.FilePath, StringComparer.Ordinal))
        {
            var values = new List<LifecycleFact>();
            var model = app.Compilation.GetSemanticModel(tree);
            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
                    continue;
                var target = CanonicalTarget.Describe(method, assembly =>
                    build.Projects.Values.FirstOrDefault(project => project.AssemblyName == assembly)?.UnitId);
                if (target is null)
                    continue;
                var token = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax ma => ma.Name.Identifier,
                    SimpleNameSyntax sn => sn.Identifier,
                    _ => invocation.Expression.GetLastToken(),
                };
                var line = tree.GetLineSpan(token.Span);
                var sourceHash = identity.SourceHash(AppUnit, tree.FilePath);
                var startLine = line.StartLinePosition.Line + 1;
                var startColumn = line.StartLinePosition.Character + 1;
                var endLine = line.EndLinePosition.Line + 1;
                var endColumn = line.EndLinePosition.Character + 1;
                var occurrence = CanonicalTarget.OccurrenceId(identity, target.Value, AppUnit, tree.FilePath,
                    sourceHash, startLine, startColumn, endLine, endColumn);
                values.Add(new LifecycleFact(occurrence, target.Value.DefiningUnit, target.Value.DocumentId,
                    target.Value.DeclarationForm, target.Value.Shape,
                    CanonicalTarget.TargetId(identity, target.Value), startLine, startColumn, endLine, endColumn));
            }
            facts[tree.FilePath] = values.OrderBy(value => value.Usage4, StringComparer.Ordinal).ToList();
        }
        return facts.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray(), StringComparer.Ordinal);
    }

    private static bool ProjectionMatches(IReadOnlyDictionary<string, string[]> expected,
        IReadOnlyDictionary<string, string[]> actual) => expected.Count == actual.Count &&
        expected.All(pair => actual.TryGetValue(pair.Key, out var values) && pair.Value.SequenceEqual(values, StringComparer.Ordinal));

    // ------------------------------------------------------- cross-view consistency

    private static readonly (string Id, string File, string Marker, SyntaxKind Kind)[] CrossViewSites =
    [
        ("S01", "App/Sites.cs", "@S01", SyntaxKind.InvocationExpression),
        ("S02", "App/Sites.cs", "@S02", SyntaxKind.InvocationExpression),
        ("S03", "App/Sites.cs", "@S03", SyntaxKind.InvocationExpression),
        ("S06", "App/Sites.cs", "@S06", SyntaxKind.InvocationExpression),
        ("S21", "App/Sites.cs", "@S21", SyntaxKind.InvocationExpression),
        ("S26", "App/Sites.cs", "@S26", SyntaxKind.InvocationExpression),
        ("INV-01", "App/Bulk.cs", "@INV-01", SyntaxKind.InvocationExpression),
        ("INV-11", "App/Bulk.cs", "@INV-11", SyntaxKind.InvocationExpression),
        ("INV-31", "App/Bulk.cs", "@INV-31", SyntaxKind.InvocationExpression),
    ];

    /// <summary>
    /// The main identity view's canonical tuple for a designated site, declared
    /// explicitly from the named R2 vectors (not derived from the lifecycle path).
    /// </summary>
    private static TargetDescription CrossViewFixture(string id) => id switch
    {
        "S01" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1),
        "S02" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Static(System.Int32)", 1),
        "S03" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", 2),
        "S06" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.StringExtensions.Shout(System.String)", 1),
        "S21" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32,System.Int32)", 3),
        "S26" => CanonicalTarget.Fixture(LeafUnit, "M:Corpus.Leaf.Helper.Compute(System.Int32)", 1),
        "INV-01" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Add(System.Int32,System.Int32)", 2),
        "INV-11" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1),
        "INV-31" => CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.StringExtensions.WordCount(System.String)", 1),
        _ => throw new InvalidOperationException("no cross-view fixture tuple for " + id),
    };

    private static string? NamedVectorId(string id) => id switch
    {
        "S01" or "INV-11" => "T.Direct",
        "S02" => "T.Static",
        "S03" or "INV-01" => "T.Add2",
        "S21" => "T.Add3",
        "S26" => "T.LeafCompute",
        _ => null,
    };

    private sealed record CrossViewRow(string Id, string File, string SourceHash, SemanticResult Inspected,
        IMethodSymbol Method, TargetDescription Target, string Asmctx3, string Usage4, LifecycleFact? Fact);

    private static Dictionary<string, object?> RunCrossView(string corpusRoot, CorpusBuild baseline,
        CorpusIdentity identity, IReadOnlyDictionary<string, LifecycleFact[]> baselineFacts, List<Check> checks)
    {
        var app = baseline.Get(AppUnit);
        var rows = new List<object>();
        var byId = new Dictionary<string, CrossViewRow>(StringComparer.Ordinal);
        var allAgree = true;
        var allFrozen = true;

        foreach (var site in CrossViewSites)
        {
            var tree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == site.File);
            var model = app.Compilation.GetSemanticModel(tree);
            var node = SemanticInspector.Locate(tree, site.Marker, site.Kind)
                ?? throw new InvalidOperationException("cross-view marker not found: " + site.Marker);
            var inspected = SemanticInspector.Inspect(app.Compilation, tree, site.Marker, site.Kind);
            var method = model.GetSymbolInfo(node).Symbol as IMethodSymbol
                ?? throw new InvalidOperationException("cross-view site is not a method: " + site.Id);
            var sourceHash = identity.SourceHash(AppUnit, site.File);

            var main = CrossViewFixture(site.Id);
            var mainAsmctx3 = CanonicalTarget.TargetId(identity, main);
            var mainParts = CanonicalTarget.Usage4Parts(identity, main, AppUnit, site.File, sourceHash,
                inspected.StartLine, inspected.StartColumn, inspected.EndLine, inspected.EndColumn);
            var mainUsage = R2Identity.Hash("usage4", mainParts);
            var mainBytes = R2Identity.EncodedTuple("usage4", mainParts);

            var fact = baselineFacts.TryGetValue(site.File, out var facts)
                ? facts.SingleOrDefault(value => value.StartLine == inspected.StartLine && value.StartColumn == inspected.StartColumn)
                : null;
            var lifecycleParts = fact is null
                ? []
                : R2Identity.Usage4Parts(CorpusBuild.RepoId, CorpusBuild.PluginId, AppUnit, site.File,
                    sourceHash, fact.StartLine, fact.StartColumn, fact.EndLine, fact.EndColumn,
                    fact.DocumentId, fact.Asmctx3);
            var lifecycleBytes = fact is null ? [] : R2Identity.EncodedTuple("usage4", lifecycleParts);

            var fields = new (string Name, string? Main, string? Lifecycle)[]
            {
                ("semanticUnit", AppUnit, fact is null ? null : AppUnit),
                ("path", site.File, fact is null ? null : site.File),
                ("sourceHash", sourceHash, fact is null ? null : sourceHash),
                ("startLine", inspected.StartLine.ToString(CultureInfo.InvariantCulture), fact?.StartLine.ToString(CultureInfo.InvariantCulture)),
                ("startColumn", inspected.StartColumn.ToString(CultureInfo.InvariantCulture), fact?.StartColumn.ToString(CultureInfo.InvariantCulture)),
                ("endLine", inspected.EndLine.ToString(CultureInfo.InvariantCulture), fact?.EndLine.ToString(CultureInfo.InvariantCulture)),
                ("endColumn", inspected.EndColumn.ToString(CultureInfo.InvariantCulture), fact?.EndColumn.ToString(CultureInfo.InvariantCulture)),
                ("definingUnit", main.DefiningUnit, fact?.DefiningUnit),
                ("targetDocumentId", main.DocumentId, fact?.DocumentId),
                ("declarationForm", main.DeclarationForm, fact?.DeclarationForm),
                ("shape", main.Shape, fact?.Shape),
                ("asmctx3", mainAsmctx3, fact?.Asmctx3),
                ("usage4", mainUsage, fact?.Usage4),
            };
            var firstDivergence = fields.FirstOrDefault(field =>
                !string.Equals(field.Main, field.Lifecycle, StringComparison.Ordinal));
            var agree = fact is not null && firstDivergence == default &&
                mainBytes.SequenceEqual(lifecycleBytes);
            allAgree &= agree;

            var asmctx3Frozen = NamedVectorId(site.Id) is not { } vectorId ||
                string.Equals(mainAsmctx3, FrozenExpectations.Asmctx3[vectorId], StringComparison.Ordinal);
            var usage4Frozen = !FrozenExpectations.Usage4.TryGetValue(site.Id, out var frozenUsage) ||
                string.Equals(mainUsage, frozenUsage, StringComparison.Ordinal);
            allFrozen &= asmctx3Frozen && usage4Frozen;

            checks.Add(new Check("crossView." + site.Id, "cross-view", agree,
                agree
                    ? $"{main.DeclarationForm} {main.Shape} {mainUsage}"
                    : $"first divergent field: {(firstDivergence.Name ?? "missing-lifecycle-fact")}; main={firstDivergence.Main}; lifecycle={firstDivergence.Lifecycle}"));
            checks.Add(new Check("crossView." + site.Id + ".frozen", "cross-view", asmctx3Frozen && usage4Frozen,
                $"main identity agrees with the accepted frozen anchor (asmctx3={asmctx3Frozen}, usage4={usage4Frozen})"));

            byId[site.Id] = new CrossViewRow(site.Id, site.File, sourceHash, inspected, method, main, mainAsmctx3, mainUsage, fact);
            rows.Add(new
            {
                id = site.Id,
                snapshot = "A",
                sourceUnit = AppUnit,
                file = site.File,
                sourceHash,
                span = new { startLine = inspected.StartLine, startColumn = inspected.StartColumn, endLine = inspected.EndLine, endColumn = inspected.EndColumn },
                category = R2Identity.InvocationCategory,
                resolvedSymbol = inspected.SymbolKey,
                roslynShape = new { genericArity = method.Arity, parameterCount = method.Parameters.Length, reducedFrom = method.ReducedFrom?.GetDocumentationCommentId(), originalDefinition = method.OriginalDefinition.GetDocumentationCommentId() },
                main = new
                {
                    definingUnit = main.DefiningUnit,
                    targetDocumentId = main.DocumentId,
                    declarationForm = main.DeclarationForm,
                    shape = main.Shape,
                    asmctx3 = mainAsmctx3,
                    usage4 = mainUsage,
                    encodedBytesSha256 = R2Identity.Sha256Hex(mainBytes),
                    encodedBytesHex = Convert.ToHexStringLower(mainBytes),
                },
                lifecycle = fact is null ? null : new
                {
                    definingUnit = fact.DefiningUnit,
                    targetDocumentId = fact.DocumentId,
                    declarationForm = fact.DeclarationForm,
                    shape = fact.Shape,
                    asmctx3 = fact.Asmctx3,
                    usage4 = fact.Usage4,
                    encodedBytesSha256 = R2Identity.Sha256Hex(lifecycleBytes),
                    encodedBytesHex = Convert.ToHexStringLower(lifecycleBytes),
                },
                fieldsAgree = agree,
                firstDivergence = agree ? null : (firstDivergence.Name ?? "missing-lifecycle-fact"),
                asmctx3Frozen,
                usage4Frozen,
            });
        }

        // ID-2: two distinct source sites may share a defining target (asmctx3)
        // while retaining distinct occurrence identities (usage4).
        var id2 = string.Equals(byId["S01"].Asmctx3, byId["INV-11"].Asmctx3, StringComparison.Ordinal) &&
            !string.Equals(byId["S01"].Usage4, byId["INV-11"].Usage4, StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-2-target-vs-occurrence", "cross-view", id2,
            "S01 and INV-11 share Methods.Direct asmctx3 while their usage4 identities differ"));

        // ---- Snapshot B: retained Bulk facts and removed Sites facts ----------
        var reduced = CorpusBuild.Build(corpusRoot, CorpusVariant.SourceDeleted);
        var reducedIdentity = CorpusIdentity.Compute(reduced);
        var reducedFacts = AppFacts(reduced, reducedIdentity);
        var bulkStable = baselineFacts.TryGetValue("App/Bulk.cs", out var bulkBefore) &&
            reducedFacts.TryGetValue("App/Bulk.cs", out var bulkAfter) &&
            bulkBefore.Length == 32 && bulkAfter.Length == 32 &&
            bulkBefore.SequenceEqual(bulkAfter);
        var removedSites = baselineFacts.GetValueOrDefault("App/Sites.cs", []);
        var reducedUsage = reducedFacts.Values.SelectMany(value => value).Select(value => value.Usage4).ToHashSet(StringComparer.Ordinal);
        var removedAbsent = !reducedFacts.ContainsKey("App/Sites.cs") &&
            removedSites.Length == 20 && removedSites.All(value => !reducedUsage.Contains(value.Usage4));
        checks.Add(new Check("crossView.snapshotB-retained", "cross-view", bulkStable,
            "the 32 App/Bulk.cs source-owned facts retain identical canonical tuples and usage4 in Snapshot B"));
        checks.Add(new Check("crossView.snapshotB-removed", "cross-view", removedAbsent,
            "all 20 App/Sites.cs source-owned facts are absent from Snapshot B"));

        // ID-3: source ownership is retained by the owning source unit, not by target identity.
        var ownership = baselineFacts.TryGetValue("App/Sites.cs", out var sitesFacts) &&
            baselineFacts.TryGetValue("App/Bulk.cs", out var bulkFacts) &&
            sitesFacts.Length == 20 && bulkFacts.Length == 32 &&
            reducedFacts.TryGetValue("App/Bulk.cs", out var bulkRetained) && bulkRetained.Length == 32;
        checks.Add(new Check("crossView.ID-3-source-ownership", "cross-view", ownership,
            "source-owned facts are keyed by their owning source unit (Sites=20, Bulk=32; retained Bulk=32 in B)"));

        // ---- Negative controls -------------------------------------------------
        var s01 = byId["S01"];
        var s01Span = (StartLine: s01.Inspected.StartLine, StartColumn: s01.Inspected.StartColumn,
            EndLine: s01.Inspected.EndLine, EndColumn: s01.Inspected.EndColumn);

        // ID-N1: change one target-shape field but retain the original expected identity.
        var shapeMutated = s01.Target with { Shape = "method|2" };
        var shapeMutatedUsage = CanonicalTarget.OccurrenceId(identity, shapeMutated, AppUnit, s01.File, s01.SourceHash,
            s01Span.StartLine, s01Span.StartColumn, s01Span.EndLine, s01Span.EndColumn);
        var idN1 = !string.Equals(shapeMutatedUsage, FrozenExpectations.Usage4["S01"], StringComparison.Ordinal) &&
            !string.Equals(CanonicalTarget.TargetId(identity, shapeMutated), FrozenExpectations.Asmctx3["T.Direct"], StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-N1-shape-mutation", "cross-view", idN1,
            $"changing shape {s01.Target.Shape}->method|2 changes asmctx3 and usage4 ({shapeMutatedUsage})"));

        // ID-N2: disable reduced-extension normalization.
        var s06 = byId["S06"];
        var unnormalizedDocId = s06.Method.OriginalDefinition.GetDocumentationCommentId()!;
        var unnormalized = new TargetDescription(CoreUnit, unnormalizedDocId,
            CoreUnit + "::" + unnormalizedDocId, CanonicalTarget.ShapeFor(s06.Method.Arity));
        var unnormalizedUsage = CanonicalTarget.OccurrenceId(identity, unnormalized, AppUnit, s06.File, s06.SourceHash,
            s06.Inspected.StartLine, s06.Inspected.StartColumn, s06.Inspected.EndLine, s06.Inspected.EndColumn);
        var idN2 = !string.Equals(unnormalizedDocId, s06.Target.DocumentId, StringComparison.Ordinal) &&
            !string.Equals(unnormalizedUsage, s06.Usage4, StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-N2-reduced-normalization", "cross-view", idN2,
            $"retaining the reduced shape {unnormalizedDocId} produces {unnormalizedUsage}, not the canonical {s06.Usage4}"));

        // ID-N3: reuse a target identity from an incompatible defining snapshot.
        var coreChanged = CorpusIdentity.Compute(CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsCoreChanged));
        var leafChanged = CorpusIdentity.Compute(CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsLeafChanged));
        var incompatibleTarget = CanonicalTarget.TargetId(coreChanged, s01.Target);
        var incompatibleUsage = CanonicalTarget.OccurrenceId(coreChanged, s01.Target, AppUnit, s01.File, s01.SourceHash,
            s01Span.StartLine, s01Span.StartColumn, s01Span.EndLine, s01Span.EndColumn);
        var idN3 = !string.Equals(incompatibleTarget, s01.Asmctx3, StringComparison.Ordinal) &&
            !string.Equals(incompatibleUsage, s01.Usage4, StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-N3-incompatible-snapshot", "cross-view", idN3,
            "reusing a defining-context identity from a changed Core compilation changes asmctx3 and usage4"));

        // Option A distinction: a transitive-only dependency change leaves the
        // defining compctx1-derived target identity stable.
        var optionAStable = string.Equals(CanonicalTarget.TargetId(leafChanged, s01.Target), s01.Asmctx3, StringComparison.Ordinal) &&
            !string.Equals(leafChanged.Compctx2[CoreUnit], identity.Compctx2[CoreUnit], StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-4-option-a-snapshot", "cross-view", optionAStable,
            "transitive-only dependency change keeps Direct asmctx3 stable while Core compctx2 changes"));

        // ID-N4: reintroduce a deleted-source contribution.
        var reducedProjection = ProjectUsage4(reducedFacts).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var expectedAfter = ProjectUsage4(baselineFacts)
            .Where(pair => pair.Key != "App/Sites.cs")
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var staleInjected = reducedProjection.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        staleInjected["App/Sites.cs"] = ProjectUsage4(baselineFacts)["App/Sites.cs"];
        var idN4 = !ProjectionMatches(expectedAfter, staleInjected) &&
            ProjectUsage4(baselineFacts)["App/Sites.cs"].All(value => !reducedUsage.Contains(value));
        checks.Add(new Check("crossView.ID-N4-deleted-contribution", "cross-view", idN4,
            "reintroducing the 20 deleted App/Sites.cs contributions fails the Snapshot B projection comparison"));

        // ID-N5: substitute a different valid overload with a similar descriptor.
        var s02 = byId["S02"];
        var substitutedUsage = CanonicalTarget.OccurrenceId(identity, s02.Target, AppUnit, s01.File, s01.SourceHash,
            s01Span.StartLine, s01Span.StartColumn, s01Span.EndLine, s01Span.EndColumn);
        var idN5 = !string.Equals(s02.Asmctx3, s01.Asmctx3, StringComparison.Ordinal) &&
            !string.Equals(substitutedUsage, s01.Usage4, StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-N5-overload-substitution", "cross-view", idN5,
            "substituting Methods.Static(int) for Methods.Direct(int) changes asmctx3 and usage4 despite the same shape"));

        // ID-4: an unchanged textual expression in a changed calling source may
        // legitimately change usage4 while the defining target stays stable.
        var sourceChanged = CorpusIdentity.Compute(CorpusBuild.Build(corpusRoot, CorpusVariant.SourceChanged));
        var changedHash = sourceChanged.SourceHash(AppUnit, "App/Sites.cs");
        var changedUsage = CanonicalTarget.OccurrenceId(sourceChanged, s01.Target, AppUnit, "App/Sites.cs", changedHash,
            s01Span.StartLine, s01Span.StartColumn, s01Span.EndLine, s01Span.EndColumn);
        var id4 = !string.Equals(changedHash, s01.SourceHash, StringComparison.Ordinal) &&
            !string.Equals(changedUsage, s01.Usage4, StringComparison.Ordinal) &&
            string.Equals(CanonicalTarget.TargetId(sourceChanged, s01.Target), s01.Asmctx3, StringComparison.Ordinal);
        checks.Add(new Check("crossView.ID-4-calling-source-change", "cross-view", id4,
            "changed calling source changes usage4 while the defining Methods.Direct asmctx3 remains stable"));

        checks.Add(new Check("crossView.all-sites-agree", "cross-view", allAgree,
            "every designated source occurrence agrees across the identity and lifecycle views"));
        checks.Add(new Check("crossView.all-frozen-anchors", "cross-view", allFrozen,
            "every designated source occurrence agrees with its accepted frozen anchor where one exists"));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["caseId"] = "ID-1..ID-5",
            ["sites"] = rows,
            ["counts"] = new { sites = CrossViewSites.Length, agreeing = allAgree ? CrossViewSites.Length : 0, frozenAnchors = allFrozen },
            ["snapshotB"] = new { retainedBulkFacts = 32, removedSitesFacts = removedSites.Length, sitesAbsent = !reducedFacts.ContainsKey("App/Sites.cs"), retainedBulkStable = bulkStable, removedContributionsAbsent = removedAbsent },
            ["negativeControls"] = new { ID_N1 = idN1, ID_N2 = idN2, ID_N3 = idN3, ID_N4 = idN4, ID_N5 = idN5 },
            ["optionA"] = new { transitiveTargetStable = optionAStable },
            ["invariants"] = new
            {
                ID_1 = allAgree,
                ID_2 = id2,
                ID_3 = ownership,
                ID_4 = id4,
                ID_5 = allFrozen,
            },
        };
    }

    // --------------------------------------------------- defining provenance (E02)

    private static Dictionary<string, object?> RunE02(string corpusRoot, CorpusBuild baseline,
        CorpusIdentity baselineIdentity, List<Check> checks)
    {
        var app = baseline.Get(AppUnit);
        var tree = app.Compilation.SyntaxTrees.Single(t => t.FilePath == "App/Sites.cs");
        var binding = SemanticInspector.Inspect(app.Compilation, tree, "@S01", SyntaxKind.InvocationExpression);
        var directTarget = CanonicalTarget.Fixture(CoreUnit, "M:Corpus.Core.Methods.Direct(System.Int32)", 1);
        var targetAsmctx3 = CanonicalTarget.TargetId(baselineIdentity, directTarget);

        // Independently derived current authority: computed from the verified
        // fixture inputs, never read from the candidate evidence being validated.
        var current = baselineIdentity.Compctx2;
        var currentCore = current[CoreUnit];

        checks.Add(new Check("e02.authority.frozen-compctx2", "provenance",
            string.Equals(currentCore, FrozenExpectations.CoreCompctx2, StringComparison.Ordinal),
            $"derived Core compctx2 equals the accepted frozen contract value {currentCore}"));
        checks.Add(new Check("e02.authority.frozen-compctx1", "provenance",
            string.Equals(baselineIdentity.Compctx1[CoreUnit], FrozenExpectations.CoreCompctx1, StringComparison.Ordinal),
            "derived Core compctx1 equals the accepted frozen contract value"));

        var fullUniverse = ProvenanceGate.Universe(baselineIdentity, LeafUnit, CoreUnit, AppUnit, SoloUnit);
        var noCoreUniverse = ProvenanceGate.Universe(baselineIdentity, LeafUnit, AppUnit);
        var coreChanged = CorpusIdentity.Compute(CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsCoreChanged));
        var leafChanged = CorpusIdentity.Compute(CorpusBuild.Build(corpusRoot, CorpusVariant.SymbolsLeafChanged));

        var zero = "compctx2:sha256:" + new string('0', 64);
        var staleCore = coreChanged.Compctx2[CoreUnit];
        var wrongCompilation = baselineIdentity.Compctx2[AppUnit];
        var incompatibleUniverse = ProvenanceGate.Universe(coreChanged, LeafUnit, CoreUnit, AppUnit, SoloUnit);

        static bool Canonical(string value) =>
            value.StartsWith("compctx2:sha256:", StringComparison.Ordinal) &&
            value.Length == "compctx2:sha256:".Length + 64;

        var definitions = new (string Id, string? Supplied,
            IReadOnlyDictionary<string, string> Current,
            IReadOnlyDictionary<string, string> Universe,
            bool ExpectedEligible, string Detail)[]
        {
            ("E02-R2-01-valid", currentCore, current, fullUniverse, true,
                "verified current defining compctx2 with matching universe membership is eligible"),
            ("E02-R2-02-missing", null, current, fullUniverse, false,
                "missing defining-context evidence is refused"),
            ("E02-R2-03-stale", staleCore, current, fullUniverse, false,
                "a previously valid but no longer current compctx2 is refused"),
            ("E02-R2-04-all-zero", zero, current, fullUniverse, false,
                "a canonical all-zero compctx2 is not authority"),
            ("E02-R2-05-wrong-compilation", wrongCompilation, current, fullUniverse, false,
                "a valid canonical context belonging to a different compilation is refused"),
            ("E02-R2-06-missing-universe", currentCore, current, noCoreUniverse, false,
                "the correct compctx2 without Core universe membership is refused"),
            ("E02-R2-07-incompatible-universe", currentCore, current, incompatibleUniverse, false,
                "universe membership carrying an incompatible defining context is refused"),
            ("E02-R2-08-restored", currentCore, current, fullUniverse, true,
                "restoring the verified current context and matching membership removes the provenance refusal"),
        };

        var results = new List<object>();
        foreach (var definition in definitions)
        {
            var eligible = ProvenanceGate.Eligible(definition.Supplied, definition.Current,
                definition.Universe, CoreUnit);
            checks.Add(new Check("e02." + definition.Id, "provenance", eligible == definition.ExpectedEligible,
                $"{definition.Id}: eligible={eligible} expected={definition.ExpectedEligible}; {definition.Detail}"));
            results.Add(new
            {
                id = definition.Id,
                suppliedCompctx2 = definition.Supplied,
                suppliedIsCanonicalShape = definition.Supplied is not null && Canonical(definition.Supplied),
                definingUnit = CoreUnit,
                universeMember = definition.Universe.TryGetValue(CoreUnit, out var member) ? member : null,
                eligible,
                expectedEligible = definition.ExpectedEligible,
                detail = definition.Detail,
            });
        }

        // PROV-1: a supplied digest cannot establish its own authority.
        var prov1 = Canonical(zero) && !ProvenanceGate.Eligible(zero, current, fullUniverse, CoreUnit);
        // PROV-2: changing the candidate does not silently redefine current authority.
        var currentBefore = current[CoreUnit];
        var tampered = zero;
        var prov2 = string.Equals(currentBefore, current[CoreUnit], StringComparison.Ordinal) &&
            !string.Equals(tampered, current[CoreUnit], StringComparison.Ordinal) &&
            !ProvenanceGate.Eligible(tampered, current, fullUniverse, CoreUnit);
        // PROV-3: a changed verified defining compilation invalidates prior evidence.
        var changedUniverse = ProvenanceGate.Universe(coreChanged, LeafUnit, CoreUnit, AppUnit, SoloUnit);
        var prov3 = !string.Equals(coreChanged.Compctx2[CoreUnit], currentCore, StringComparison.Ordinal) &&
            !ProvenanceGate.Eligible(currentCore, coreChanged.Compctx2, changedUniverse, CoreUnit);
        // PROV-4: correct context without membership is insufficient.
        var prov4 = !ProvenanceGate.Eligible(currentCore, current, noCoreUniverse, CoreUnit);
        // PROV-5: membership must carry the same verified context, not a count or copied digest.
        var prov5 = !ProvenanceGate.Eligible(currentCore, current, incompatibleUniverse, CoreUnit);
        // PROV-6: a resolved Roslyn symbol may remain nonpublishable without authority.
        var prov6 = binding.Resolution == Resolution.Resolved &&
            !ProvenanceGate.Eligible(null, current, fullUniverse, CoreUnit);
        // PROV-7: asmctx3 equality does not prove current compctx2 validity.
        var leafTargetSame = string.Equals(CanonicalTarget.TargetId(leafChanged, directTarget), targetAsmctx3, StringComparison.Ordinal);
        var leafUniverse = ProvenanceGate.Universe(leafChanged, LeafUnit, CoreUnit, AppUnit, SoloUnit);
        var leafEvidenceInvalid = !ProvenanceGate.Eligible(currentCore, leafChanged.Compctx2, leafUniverse, CoreUnit);
        var prov7 = leafTargetSame && leafEvidenceInvalid &&
            !string.Equals(leafChanged.Compctx2[CoreUnit], currentCore, StringComparison.Ordinal);
        // PROV-8: both the authority field and the resulting eligibility are frozen
        // and independently anchored (the e02 section is compared to the expected file).
        var prov8 = string.Equals(currentCore, FrozenExpectations.CoreCompctx2, StringComparison.Ordinal) &&
            string.Equals(binding.SymbolKey, "Corpus.Core!M:Corpus.Core.Methods.Direct(System.Int32)", StringComparison.Ordinal) &&
            targetAsmctx3 == FrozenExpectations.Asmctx3["T.Direct"];

        var invariants = new (string Id, bool Pass, string Detail)[]
        {
            ("PROV-1", prov1, "a supplied digest cannot establish its own authority"),
            ("PROV-2", prov2, "changing the supplied candidate does not redefine current authority"),
            ("PROV-3", prov3, "a changed verified defining compilation invalidates incompatible prior evidence"),
            ("PROV-4", prov4, "a correct compctx2 without universe membership is insufficient"),
            ("PROV-5", prov5, "universe membership must carry the same verified defining context"),
            ("PROV-6", prov6, "a resolved Roslyn symbol can remain nonpublishable without provenance"),
            ("PROV-7", prov7, "asmctx3 equality does not prove current compctx2 validity"),
            ("PROV-8", prov8, "expected authority and eligibility are independently anchored and frozen"),
        };
        foreach (var invariant in invariants)
            checks.Add(new Check("e02." + invariant.Id, "provenance", invariant.Pass, invariant.Detail));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["caseId"] = "E02",
            ["resolvedSymbol"] = binding.SymbolKey,
            ["resolution"] = binding.Resolution.ToString(),
            ["definingUnit"] = CoreUnit,
            ["actualDefiningCompctx1"] = baselineIdentity.Compctx1[CoreUnit],
            ["currentAuthorityCompctx2"] = currentCore,
            ["targetAsmctx3"] = targetAsmctx3,
            ["trustBoundary"] = "verified fixture compilation -> independently derived current context -> candidate evidence validation -> eligibility",
            ["cases"] = results,
            ["invariants"] = invariants.Select(invariant => new { id = invariant.Id, pass = invariant.Pass, detail = invariant.Detail }).ToArray(),
        };
    }

    // ------------------------------------------------------------------ coverage

    private static Dictionary<string, object?> RunCoverage(CorpusBuild baseline, CorpusIdentity identity, List<Check> checks)
    {
        // Complete-empty: Solo analyzed completely, zero occurrences.
        var solo = baseline.Get(SoloUnit);
        var soloCount = solo.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()).Count();
        checks.Add(new Check("coverage.complete-empty", "model-assertion", soloCount == 0,
            "MODEL-LEVEL expectation: Solo Roslyn compilation has zero invocation syntax; production coverage authority is not exercised"));

        // Incomplete: a declared unit without a verified context.
        var declared = new[] { LeafUnit, CoreUnit, AppUnit, SoloUnit };
        var verified = new[] { LeafUnit, CoreUnit, AppUnit };
        var missing = declared.Except(verified, StringComparer.Ordinal).ToArray();
        checks.Add(new Check("coverage.incomplete", "model-assertion", missing.Length == 1 && missing[0] == SoloUnit,
            "MODEL-LEVEL expectation: omitting a declared unit maps to IncompleteAnalysis; production coverage types are not exercised"));

        // NEW-04: incompatible coverage scopes must not be merged as authoritative.
        var scopeA = identity.Compctx2[CoreUnit];
        var scopeB = identity.Compctx2[LeafUnit];
        var compatible = string.Equals(scopeA, scopeB, StringComparison.Ordinal);
        checks.Add(new Check("coverage.new-04-scope-compatibility", "coverage", !compatible,
            "distinct scope/universe identities are incompatible and must not be merged as authoritative"));

        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["completeEmpty"] = new { unit = SoloUnit, occurrences = soloCount, state = "Produced", assertionLevel = "model expectation backed by direct Roslyn syntax inspection; not production coverage enforcement" },
            ["incomplete"] = new { declared = declared, verified = verified, missing, state = "IncompleteAnalysis", assertionLevel = "model expectation; not production coverage enforcement" },
            ["new04"] = new { scopeA = "Core", scopeB = "Leaf", compatible, note = "incompatible scopes are not merge authority", assertionLevel = "model expectation; NEW-04 remains open" },
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
        foreach (var section in new[] { "semantics", "identities", "variantIdentities", "variants", "crossView", "e02", "coverage", "counts", "lifecycle" })
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
