using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// ============================================================================
// CI-1-SCOPE-R2 corrected planning-only semantic oracle.
//
// This is a pure-Roslyn compiler oracle. It does NOT reference the Andirit
// plugin and does NOT read any CI-1A implementation output (none exists).
// Expected outcomes are derived independently from the fixture source and the
// recorded compilation/dependency context, then compared against the frozen
// expected-sites manifest. No editable manifest field is compared to another
// editable manifest field.
//
// Error/decision vocabulary and limitations are documented in
// docs/planning/ci1-scope-r2/oracle-contract.md.
// ============================================================================

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var packet = Path.Combine(root, "docs/planning/ci1-scope-01");
var fixtures = Path.Combine(packet, "fixtures");

var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
    .Select(p => MetadataReference.CreateFromFile(p)).ToArray<MetadataReference>();

// ---- Frozen target-identity table: independent oracle input, not manifest data.
var targetTable = new Dictionary<string, TargetExpectation>(StringComparer.Ordinal)
{
    ["Logger.Write(string)"] = new("M:Logger.Write(System.String)", TargetKind.Method, true),
    ["CI1Corpus.Overloads.Hit(int)"] = new("M:CI1Corpus.Overloads.Hit(System.Int32)", TargetKind.Method, true),
    ["CI1Corpus.Overloads.Hit(string)"] = new("M:CI1Corpus.Overloads.Hit(System.String)", TargetKind.Method, true),
    ["CI1Corpus.GenericExtensions.Echo<T>(T)"] = new("M:CI1Corpus.GenericExtensions.Echo``1(``0)", TargetKind.Method, true),
    ["CI1Corpus.GenericExtensions.Mark<T>(this T)"] = new("M:CI1Corpus.GenericExtensions.Mark``1(``0)", TargetKind.Method, true),
    ["CI1Corpus.Constructors(int)"] = new("M:CI1Corpus.Constructors.#ctor(System.Int32)", TargetKind.Constructor, true),
    ["CI1Corpus.ConstructorBase(int)"] = new("M:CI1Corpus.ConstructorBase.#ctor(System.Int32)", TargetKind.Constructor, true),
    ["CI1Corpus.Constructors()"] = new("M:CI1Corpus.Constructors.#ctor", TargetKind.Constructor, true),
    ["CI1Corpus.IWorker.Work()"] = new("M:CI1Corpus.IWorker.Work", TargetKind.Method, true),
    ["CI1Corpus.Worker.Work()"] = new("M:CI1Corpus.Worker.Work", TargetKind.Method, true),
    ["CI1Corpus.Target.Work()"] = new("M:CI1Corpus.Target.Work", TargetKind.Method, true),
    ["System.Action.Invoke()"] = new("M:System.Action.Invoke", TargetKind.Method, false),
    ["CI1Corpus.Target"] = new("T:CI1Corpus.Target", TargetKind.TypeToken, true),
    ["System.Console.WriteLine(string)"] = new("M:System.Console.WriteLine(System.String)", TargetKind.Method, false),
    ["CI1Corpus.Partial.Work()"] = new("M:CI1Corpus.Partial.Work", TargetKind.Method, true),
    ["CI1Cross.Library.Work() in Lib/Lib.csproj"] = new("M:CI1Cross.Library.Work", TargetKind.Method, true),
    ["unknown"] = new("", TargetKind.Unresolved, false),
    ["unknown-string-or-Exception-overload"] = new("", TargetKind.Ambiguous, false),
};

// ---- Exhaustive recognized-outcome vocabulary (exact strings).
var outcomeVocabulary = new Dictionary<string, DeclaredOutcome>(StringComparer.Ordinal)
{
    ["confirmed"] = new(OutcomeClass.Confirmed, null, false),
    ["confirmed-interface-static-target"] = new(OutcomeClass.Confirmed, null, false),
    ["confirmed-virtual-static-target"] = new(OutcomeClass.Confirmed, null, false),
    ["confirmed-base-static-target"] = new(OutcomeClass.Confirmed, null, false),
    ["confirmed-cross-project"] = new(OutcomeClass.Confirmed, null, false),
    ["confirmed-after-replacement"] = new(OutcomeClass.Confirmed, null, false),
    ["inactive-default; confirmed only with CI1_EXTRA"] = new(OutcomeClass.Confirmed, null, true),
    ["external-no-confirmed-local-target"] = new(OutcomeClass.External, null, false),
    ["external-delegate-invoke-not-Target.Work"] = new(OutcomeClass.External, null, false),
    ["ambiguous-no-confirmation"] = new(OutcomeClass.Ambiguous, null, false),
    ["unresolved-no-confirmation"] = new(OutcomeClass.Unresolved, null, false),
    ["deferred-constructor"] = new(OutcomeClass.Unsupported, "constructor", false),
    ["deferred-method-group"] = new(OutcomeClass.Unsupported, "method-group", false),
    ["deferred-type-reference"] = new(OutcomeClass.Unsupported, "type-reference", false),
    ["deferred-nameof-reference"] = new(OutcomeClass.Unsupported, "nameof-reference", false),
};

var sourceInputs = new SortedDictionary<string, object>(StringComparer.Ordinal);
(string Path, string Text) ReadFixture(string rel)
{
    var full = Path.Combine(fixtures, rel);
    var bytes = File.ReadAllBytes(full);
    sourceInputs["fixtures/" + rel.Replace('\\', '/')] = new { path = "fixtures/" + rel.Replace('\\', '/'), sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes = bytes.Length };
    using var ms = new MemoryStream(bytes);
    using var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    return (rel, sr.ReadToEnd());
}

var contexts = new List<object>();
CSharpCompilation Compile(string name, IEnumerable<(string Path, string Text)> inputs, MetadataReference[]? extra = null, string[]? defines = null)
{
    var parse = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: defines);
    var trees = inputs.Select(p => CSharpSyntaxTree.ParseText(p.Text, parse, p.Path)).ToArray();
    var comp = CSharpCompilation.Create(name, trees, references.Concat(extra ?? []), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false));
    contexts.Add(new
    {
        name,
        assemblyIdentity = comp.Assembly.Identity.ToString(),
        languageVersion = parse.LanguageVersion.ToString(),
        preprocessorSymbols = parse.PreprocessorSymbolNames.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
        sourceFiles = trees.Select(t => t.FilePath).ToArray(),
        metadataReferenceCount = comp.References.Count(),
        sourceProjectReferenceCount = (extra ?? []).Length,
    });
    return comp;
}

var mainPaths = new[] { "Cases.cs", "Partial.One.cs", "Partial.Two.cs", "Duplicate.One.cs", "Duplicate.Two.cs" };
var main = Compile("acceptance", mainPaths.Select(p => ReadFixture("acceptance/" + p)));
var enabled = Compile("enabled", mainPaths.Select(p => ReadFixture("acceptance/" + p)), defines: ["CI1_EXTRA"]);
var repeated = Compile("repeated", new[] { ReadFixture("Repeated.cs"), ReadFixture("References.cs") });
var deleted = Compile("deleted", new[] { ReadFixture("acceptance/Repeated.AfterDelete.cs.txt") });
var shifted = Compile("shifted", new[] { ReadFixture("acceptance/Repeated.AfterShift.cs.txt") });
var lib = Compile("lib", new[] { ReadFixture("acceptance/CrossProject/Lib/Lib.cs") });
var app = Compile("app", new[] { ReadFixture("acceptance/CrossProject/App/App.cs") }, extra: [lib.ToMetadataReference()]);
var sourceCompilations = new[] { main, enabled, repeated, deleted, shifted, lib, app };
var sourceIdentities = sourceCompilations.Select(c => c.Assembly.Identity).ToArray();

// ---- Independent target identity / eligibility helpers.
bool EligibleLocal(ISymbol? target, IEnumerable<CSharpCompilation> eligibleSet)
{
    if (target is null) return false;
    var def = Def(target);
    var asm = def.ContainingAssembly;
    if (asm is null) return false;
    var identity = asm.Identity;
    if (!eligibleSet.Any(c => c.Assembly.Identity.Equals(identity))) return false;
    return def.Locations.Any(l => l.IsInSource && l.SourceTree is not null);
}

var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures, "expected-sites.json")))!;
var baseline = AuditR2(manifest);

// ============================================================================
// Mutation qualification
// ============================================================================
var mutations = new List<object>();
void Mutate(string id, string description, string? original, string? mutated, string? expectedReason, Action<JsonNode> change, string? intendedRow = null)
{
    var m = manifest.DeepClone();
    change(m);
    var audits = AuditR2(m);
    var failing = audits.Where(a => a.Errors.Count > 0).ToList();
    var codes = failing.SelectMany(a => a.Errors.Select(e => e.Code)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
    var detected = failing.Count > 0;
    var unrelated = codes.Any(c => c is "COORDINATE_LEXEME_MISMATCH" or "DUPLICATE_OCCURRENCE" or "SYNTAX_MISSING" or "SYNTAX_CATEGORY_UNKNOWN");
    mutations.Add(new
    {
        id,
        description,
        originalValue = original,
        mutatedValue = mutated,
        expectedRejectionReason = expectedReason,
        detected,
        failingRows = failing.Count,
        primaryReasonCode = failing.SelectMany(a => a.Errors).Select(e => e.Code).FirstOrDefault(),
        reasonCodes = codes,
        rejectedForIntendedReason = detected && (expectedReason is null ? false : codes.Contains(expectedReason)),
        unrelatedValidationFailedFirst = unrelated && (expectedReason is "OUTCOME_CLASS_MISMATCH" or "OUTCOME_UNRECOGNIZED" or "OUTCOME_SUBTYPE_MISMATCH" or "TARGET_IDENTITY_MISMATCH" or "TARGET_ELIGIBILITY_MISMATCH" or "TARGET_KIND_MISMATCH" or "TARGET_UNRECOGNIZED"),
        failingCases = failing.Select(a => new { a.Case, a.Path, codes = a.Errors.Select(e => e.Code).ToArray() }).ToArray(),
    });
}

// Mandatory Q2 named regressions (the four F04 failures).
Mutate("M01-external-to-confirmed", "C10 external platform call relabelled confirmed", "external-no-confirmed-local-target", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]!.AsArray().First(s => (string?)s!["Case"] == "C10")!["ExpectedOutcome"] = "confirmed");
Mutate("M02-confirmed-to-external", "C01 confirmed local call relabelled external", "confirmed", "external-no-confirmed-local-target", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "external-no-confirmed-local-target");
Mutate("M03-confirmed-to-deferred", "C01 confirmed local call relabelled deferred", "confirmed", "deferred-method-group", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "deferred-method-group");
Mutate("M04-invalid-outcome", "C01 recognised outcome replaced with an unrecognised string", "confirmed", "invalid-q2-outcome", "OUTCOME_UNRECOGNIZED",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "invalid-q2-outcome");

// Required adversarial outcome cases.
Mutate("M05-ambiguous-to-confirmed", "C11 ambiguous Pick(null) relabelled confirmed", "ambiguous-no-confirmation", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]!.AsArray().First(s => (string?)s!["ExpectedOutcome"] == "ambiguous-no-confirmation")!["ExpectedOutcome"] = "confirmed");
Mutate("M06-unresolved-to-confirmed", "C11 unresolved Missing() relabelled confirmed", "unresolved-no-confirmation", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]!.AsArray().First(s => (string?)s!["ExpectedOutcome"] == "unresolved-no-confirmation")!["ExpectedOutcome"] = "confirmed");

// Target identity / assembly / false-local cases.
Mutate("M07-wrong-overload", "C02 Hit(1) declared target changed to the string overload", "CI1Corpus.Overloads.Hit(int)", "CI1Corpus.Overloads.Hit(string)", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![3]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Overloads.Hit(string)");
Mutate("M08-confirmed-to-wrong-assembly", "C03 cross-project target changed to a different-assembly external target", "CI1Cross.Library.Work() in Lib/Lib.csproj", "System.Console.WriteLine(string)", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]!.AsArray().First(s => (string?)s!["Case"] == "C03")!["ExpectedTargetSourceSignature"] = "System.Console.WriteLine(string)");
Mutate("M09-confirmed-to-unrelated-indexed-method", "C01 declared target changed to an unrelated indexed local method", "Logger.Write(string)", "CI1Corpus.Target.Work()", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![0]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Target.Work()");

// Coordinate and duplicate cases.
Mutate("M10-correct-target-incorrect-span", "C01 start column corrupted", "[5,9,5,30]", "[5,10,5,30]", "COORDINATE_LEXEME_MISMATCH",
    m => m["Sites"]![0]!["Span"]![1] = 10);
Mutate("M11-duplicate-entry", "C01 first site duplicated", "32 unique rows", "33 rows with duplicate occurrence", "DUPLICATE_OCCURRENCE",
    m => m["Sites"]!.AsArray().Add(m["Sites"]![0]!.DeepClone()));

// Positive control: unchanged declared outcome must be accepted (proves the
// corrected checker is not simply rejecting everything).
Mutate("P01-correct-outcome-accepted", "positive control: unchanged manifest, no corruption", "(unchanged)", "(unchanged)", null,
    _ => { });

// ============================================================================
// Adversarial scenario: F01 external/local documentation-ID collision.
// A local source declaration that shares the documentation ID of the platform
// call must not receive that call, even when the local declaration is indexed.
// ============================================================================
var r2FixtureRoot = Path.Combine(root, "docs/planning/ci1-scope-r2/analysis/fixtures");
var collisionFixtureFiles = new[] { "f01-collision/A/Shadow.cs", "f01-collision/A/Shadow.csproj", "f01-collision/Z/App.cs", "f01-collision/Z/App.csproj" }
    .Select(rel => { var b = File.ReadAllBytes(Path.Combine(r2FixtureRoot, rel)); return new { path = rel, sha256 = Convert.ToHexStringLower(SHA256.HashData(b)), bytes = b.Length }; }).ToArray();
(string Path, string Text) ReadR2Fixture(string rel)
{
    using var ms = new MemoryStream(File.ReadAllBytes(Path.Combine(r2FixtureRoot, rel)));
    using var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    return (rel, sr.ReadToEnd());
}
var collisionShadow = Compile("Shadow", new[] { ReadR2Fixture("f01-collision/A/Shadow.cs") });
var collisionApp = Compile("App", new[] { ReadR2Fixture("f01-collision/Z/App.cs") });
var collisionTree = collisionApp.SyntaxTrees.Single();
var collisionInvocation = collisionTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var collisionBound = (IMethodSymbol)collisionApp.GetSemanticModel(collisionTree).GetSymbolInfo(collisionInvocation).Symbol!;
var collisionShadowDecl = (IMethodSymbol)collisionShadow.GetSemanticModel(collisionShadow.SyntaxTrees.Single())
    .GetDeclaredSymbol(collisionShadow.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
var collisionEligibleSet = new[] { collisionShadow, collisionApp };
var collisionScenario = new
{
    name = "F01 external/local documentation-ID collision",
    documentedIdCoincides = Doc(collisionShadowDecl) == Doc(collisionBound),
    documentationId = Doc(collisionBound),
    indexedLocalDeclaration = new { assembly = collisionShadowDecl.ContainingAssembly.Identity.ToString(), sourceLocations = collisionShadowDecl.Locations.Count(l => l.IsInSource) },
    boundTarget = new { assembly = collisionBound.ContainingAssembly.Identity.ToString(), sourceLocations = collisionBound.Locations.Count(l => l.IsInSource) },
    indexedLocalIsEligible = EligibleLocal(collisionShadowDecl, collisionEligibleSet),
    boundTargetIsEligibleLocal = EligibleLocal(collisionBound, collisionEligibleSet),
    derivedClassForBoundCall = EligibleLocal(collisionBound, collisionEligibleSet) ? "confirmed" : "external",
    requirementSatisfied = Doc(collisionShadowDecl) == Doc(collisionBound)
        && EligibleLocal(collisionShadowDecl, collisionEligibleSet)
        && !EligibleLocal(collisionBound, collisionEligibleSet),
};

// ============================================================================
// Summaries
// ============================================================================
static string MutationId(object m) => (string)m.GetType().GetProperty("id")!.GetValue(m)!;
static bool MutationDetected(object m) => (bool)m.GetType().GetProperty("detected")!.GetValue(m)!;
static bool MutationRejected(object m) => (bool)m.GetType().GetProperty("rejectedForIntendedReason")!.GetValue(m)!;
var mandatoryRegressions = new[] { "M01-external-to-confirmed", "M02-confirmed-to-external", "M03-confirmed-to-deferred", "M04-invalid-outcome" };
bool q2NamedRegressionsRejected = mutations.Where(m => mandatoryRegressions.Contains(MutationId(m))).All(MutationRejected);
bool allRequiredMutationsRejected = mutations.Where(m => MutationId(m) != "P01-correct-outcome-accepted").All(MutationRejected);
bool positiveControlAccepted = !MutationDetected(mutations.First(m => MutationId(m) == "P01-correct-outcome-accepted"));
bool noUnrelatedFirstFailure = mutations.Where(m => MutationId(m) != "P01-correct-outcome-accepted")
    .All(m => !(bool)m.GetType().GetProperty("unrelatedValidationFailedFirst")!.GetValue(m)!);

var rowSemantics = baseline.Select(a => new
{
    a.Case,
    a.Path,
    a.Span,
    a.Lexeme,
    declaredTarget = a.DeclaredTarget,
    declaredOutcome = a.DeclaredOutcome,
    derivedClass = a.DerivedClass,
    declaredClass = a.DeclaredClass,
    derivedSubtype = a.DerivedSubtype,
    declaredSubtype = a.DeclaredSubtype,
    a.BoundTargetDoc,
    a.BoundContainingAssembly,
    a.BoundEligibleLocal,
    a.CandidateReason,
    errors = a.Errors.Select(e => e.Code).ToArray(),
}).ToArray();

var result = new
{
    harness = "CI-1-SCOPE-R2 corrected planning-only semantic oracle",
    independence = "Pure Roslyn compiler oracle; no Andirit plugin reference; no CI-1A output exists or is read",
    compiler = new
    {
        roslynCSharp = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(),
        roslynCommon = typeof(Compilation).Assembly.GetName().Version?.ToString(),
        languageVersion = LanguageVersion.Latest.ToString(),
        trustedPlatformAssemblies = references.Length,
    },
    sourceInputs = sourceInputs.Values,
    projectContext = contexts,
    targetVocabulary = targetTable.Select(kv => new { signature = kv.Key, doc = kv.Value.DocId, kind = kv.Value.Kind.ToString(), eligibleLocal = kv.Value.EligibleLocal }).ToArray(),
    outcomeVocabulary = outcomeVocabulary.Select(kv => new { declared = kv.Key, @class = kv.Value.Class.ToString(), subtype = kv.Value.Subtype, contextDependent = kv.Value.ContextDependent }).ToArray(),
    baseline = new { sites = baseline.Count, failures = baseline.Count(a => a.Errors.Count > 0) },
    semanticIdentity = new
    {
        allRowsDerivedMatchesDeclared = baseline.All(a => a.Errors.Count == 0),
        eligible = baseline.Count(a => a.DerivedClass == OutcomeClass.Confirmed),
        external = baseline.Count(a => a.DerivedClass == OutcomeClass.External),
        ambiguous = baseline.Count(a => a.DerivedClass == OutcomeClass.Ambiguous),
        unresolved = baseline.Count(a => a.DerivedClass == OutcomeClass.Unresolved),
        unsupported = baseline.Count(a => a.DerivedClass == OutcomeClass.Unsupported),
    },
    sites = rowSemantics,
    siteEvidence = baseline.Select(a => (object)new
    {
        a.Case, a.Path, a.Span, a.Lexeme,
        compilation = a.Compilation,
        declaredTarget = a.DeclaredTarget,
        declaredOutcome = a.DeclaredOutcome,
        category = a.Category.ToString(),
        boundTargetDoc = a.BoundTargetDoc,
        boundContainingAssembly = a.BoundContainingAssembly,
        boundSourceDeclarations = a.BoundSourceDeclarations,
        boundEligibleLocal = a.BoundEligibleLocal,
        candidateDocs = a.CandidateDocs,
        candidateReason = a.CandidateReason,
        derivedClass = a.DerivedClass.ToString(),
        declaredClass = a.DeclaredClass.ToString(),
        derivedSubtype = a.DerivedSubtype,
        declaredSubtype = a.DeclaredSubtype,
        errors = a.Errors.Select(e => e.Code).ToArray(),
    }).ToArray(),
    mutations,
    adversarialScenarios = new object[] { collisionScenario },
    collisionFixtureFiles,
    summary = new
    {
        q2NamedRegressionsRejected,
        allRequiredMutationsRejected,
        positiveControlAccepted,
        noUnrelatedFirstFailure,
        baselineClean = baseline.All(a => a.Errors.Count == 0),
        all32RowsRetainSemantics = baseline.Count == 32 && baseline.All(a => a.Errors.Count == 0),
        collisionRequirementSatisfied = collisionScenario.requirementSatisfied,
    },
};

File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + "\n");
var failed = baseline.Count(a => a.Errors.Count > 0);
var rejected = mutations.Count(m => !(bool)m.GetType().GetProperty("detected")!.GetValue(m)!);
Console.WriteLine(JsonSerializer.Serialize(new { Sites = baseline.Count, Failures = failed, Mutations = mutations.Count, AcceptedControls = rejected, PositiveControlAccepted = result.summary.positiveControlAccepted, Collision = collisionScenario.requirementSatisfied }));
return failed > 0 ? 1 : 0;

// ============================================================================
// Helpers and audit
// ============================================================================
static string? Doc(ISymbol? s) => s is IMethodSymbol m ? (m.ReducedFrom ?? m).OriginalDefinition.GetDocumentationCommentId() : s?.OriginalDefinition.GetDocumentationCommentId();
static ISymbol Def(ISymbol s) => s.OriginalDefinition;

List<SiteAudit> AuditR2(JsonNode data)
{
    var answer = new List<SiteAudit>();
    var keys = new HashSet<string>(StringComparer.Ordinal);
    foreach (var site in data["Sites"]!.AsArray())
    {
        var p = (string)site!["FixturePath"]!;
        var k = (string)site["Case"]!;
        var lexeme = (string)site["Lexeme"]!;
        var span = site["Span"]!.AsArray().Select(v => (int)v!).ToArray();
        var declaredTarget = (string)site["ExpectedTargetSourceSignature"]!;
        var declaredOutcomeText = (string)site["ExpectedOutcome"]!;
        var errors = new List<SiteError>();

        void Add(string code, string detail) => errors.Add(new SiteError(code, detail));

        var text = File.ReadAllText(Path.Combine(fixtures, p));
        var st = Microsoft.CodeAnalysis.Text.SourceText.From(text);
        int start = 0, end = 0;
        try
        {
            start = st.Lines[span[0] - 1].Start + span[1] - 1;
            end = st.Lines[span[2] - 1].Start + span[3] - 1;
            if (end < start) Add("COORDINATE_LEXEME_MISMATCH", "end before start");
            else if (st.ToString(new Microsoft.CodeAnalysis.Text.TextSpan(start, end - start)) != lexeme) Add("COORDINATE_LEXEME_MISMATCH", "span text differs from declared lexeme");
        }
        catch { Add("COORDINATE_LEXEME_MISMATCH", "invalid coordinate"); }

        if (!keys.Add(p + ":" + string.Join(',', span))) Add("DUPLICATE_OCCURRENCE", "same fixture path and span already audited");

        var c = p == "Repeated.cs" ? repeated : p.Contains("AfterDelete") ? deleted : p.Contains("AfterShift") ? shifted : p.Contains("CrossProject") ? app : k == "C20" ? enabled : main;
        // A corrupted coordinate invalidates the site identity itself; report it
        // alone rather than cascading into derived-semantics noise.
        if (errors.Any(e => e.Code == "COORDINATE_LEXEME_MISMATCH"))
        {
            answer.Add(new SiteAudit(k, p, span, lexeme, c.AssemblyName ?? c.Assembly.Identity.Name, declaredTarget, declaredOutcomeText, OccurrenceCategory.Unknown,
                null, null, Array.Empty<string>(), false, Array.Empty<string?>(), "None", OutcomeClass.Unrecognized, OutcomeClass.Unrecognized, null, null, null, errors));
            continue;
        }

        var tree = c.SyntaxTrees.Single(t => t.FilePath == p);
        var model = c.GetSemanticModel(tree, ignoreAccessibility: false);
        var node = tree.GetRoot().DescendantNodes().Where(n => n.Span.Start == start && n.Span.End == end).OrderBy(n => n is InvocationExpressionSyntax ? 0 : 1).FirstOrDefault();
        node ??= tree.GetRoot().DescendantNodes().OfType<ConstructorInitializerSyntax>().SingleOrDefault(n => n.Span.Contains(start) && n.Span.End == end);
        if (node is null)
        {
            Add("SYNTAX_MISSING", "no syntax node matches the span");
            answer.Add(new SiteAudit(k, p, span, lexeme, c.AssemblyName ?? c.Assembly.Identity.Name, declaredTarget, declaredOutcomeText, OccurrenceCategory.Unknown,
                null, null, Array.Empty<string>(), false, Array.Empty<string?>(), "None", OutcomeClass.Unrecognized, OutcomeClass.Unrecognized, null, null, null, errors));
            continue;
        }

        var category = OccurrenceCategory.Unknown;
        SymbolInfo info = default;
        ISymbol? target = null;
        if (node is TypeOfExpressionSyntax typeOf)
        {
            category = OccurrenceCategory.TypeOf;
            target = model.GetTypeInfo(typeOf.Type).Type;
        }
        else if (node is ConstructorInitializerSyntax)
        {
            category = OccurrenceCategory.Constructor;
            info = model.GetSymbolInfo(node);
            target = info.Symbol;
        }
        else if (node is ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax)
        {
            category = OccurrenceCategory.Constructor;
            info = model.GetSymbolInfo(node);
            target = info.Symbol;
        }
        else if (node is InvocationExpressionSyntax inv && inv.Expression.ToString() == "nameof")
        {
            category = OccurrenceCategory.NameOf;
            info = model.GetSymbolInfo(inv.ArgumentList.Arguments[0].Expression);
            target = info.Symbol ?? model.GetMemberGroup(inv.ArgumentList.Arguments[0].Expression).SingleOrDefault();
        }
        else if (node is InvocationExpressionSyntax)
        {
            category = OccurrenceCategory.OrdinaryInvocation;
            info = model.GetSymbolInfo(node);
            target = info.Symbol;
        }
        else if (node is MemberAccessExpressionSyntax or IdentifierNameSyntax)
        {
            info = model.GetSymbolInfo(node);
            target = info.Symbol;
            category = target is IMethodSymbol ? OccurrenceCategory.MethodGroup : OccurrenceCategory.Unknown;
        }
        else if (node is not null)
        {
            info = model.GetSymbolInfo(node);
            target = info.Symbol;
            category = OccurrenceCategory.Unknown;
            Add("SYNTAX_CATEGORY_UNKNOWN", "unrecognised occurrence category");
        }

        var boundDoc = Doc(target);
        var candidateDocs = info.CandidateSymbols.Select(Doc).ToArray();
        var boundEligible = EligibleLocal(target, sourceCompilations);
        var boundSourceDeclarations = target is null ? Array.Empty<string>()
            : Def(target).Locations.Where(l => l.IsInSource && l.SourceTree is not null).Select(l => l.SourceTree!.FilePath).Distinct().ToArray();

        // ---- Derived outcome class from source/compiler evidence only.
        var derived = OutcomeClass.Unrecognized;
        string? derivedSubtype = null;
        switch (category)
        {
            case OccurrenceCategory.Constructor: derived = OutcomeClass.Unsupported; derivedSubtype = "constructor"; break;
            case OccurrenceCategory.MethodGroup: derived = OutcomeClass.Unsupported; derivedSubtype = "method-group"; break;
            case OccurrenceCategory.TypeOf: derived = OutcomeClass.Unsupported; derivedSubtype = "type-reference"; break;
            case OccurrenceCategory.NameOf: derived = OutcomeClass.Unsupported; derivedSubtype = "nameof-reference"; break;
            case OccurrenceCategory.OrdinaryInvocation:
                if (target is IMethodSymbol) derived = boundEligible ? OutcomeClass.Confirmed : OutcomeClass.External;
                else if (info.CandidateSymbols.Length > 0) derived = OutcomeClass.Ambiguous;
                else derived = OutcomeClass.Unresolved;
                break;
            default:
                if (target is IMethodSymbol) derived = boundEligible ? OutcomeClass.Confirmed : OutcomeClass.External;
                else if (info.CandidateSymbols.Length > 0) derived = OutcomeClass.Ambiguous;
                else derived = OutcomeClass.Unresolved;
                break;
        }

        // ---- Declared outcome mapping (exhaustive recognised vocabulary).
        if (!outcomeVocabulary.TryGetValue(declaredOutcomeText, out var declared))
        {
            declared = new DeclaredOutcome(OutcomeClass.Unrecognized, null, false);
            Add("OUTCOME_UNRECOGNIZED", $"declared outcome '{declaredOutcomeText}' is not in the recognised vocabulary");
        }

        // ---- Target identity verification against the frozen oracle table.
        if (declaredTarget == "unknown")
        {
            if (target is not null || candidateDocs.Length != 0) Add("TARGET_IDENTITY_MISMATCH", "expected no target and no candidates");
        }
        else if (declaredTarget == "unknown-string-or-Exception-overload")
        {
            if (target is not null || candidateDocs.Length != 2
                || !candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.String)")
                || !candidateDocs.Contains("M:CI1Corpus.Ambiguous.Pick(System.Exception)"))
                Add("TARGET_IDENTITY_MISMATCH", "expected exactly the string/Exception overload candidates");
        }
        else if (!targetTable.TryGetValue(declaredTarget, out var exp))
        {
            Add("TARGET_UNRECOGNIZED", $"declared target '{declaredTarget}' is not in the frozen target table");
        }
        else
        {
            if (boundDoc != exp.DocId) Add("TARGET_IDENTITY_MISMATCH", $"bound '{boundDoc}' does not match expected '{exp.DocId}'");
            if (exp.EligibleLocal && !boundEligible) Add("TARGET_ELIGIBILITY_MISMATCH", "expected an eligible indexed local definition but the bound target is not eligible local");
            if (exp.Kind == TargetKind.Constructor && (target is not IMethodSymbol cm || cm.MethodKind != MethodKind.Constructor)) Add("TARGET_KIND_MISMATCH", "expected a constructor binding");
            if (exp.Kind == TargetKind.TypeToken && target is not INamedTypeSymbol) Add("TARGET_KIND_MISMATCH", "expected a type token binding");
        }

        // ---- Outcome class / subtype agreement.
        if (declared.Recognized)
        {
            if (declared.Class != derived) Add("OUTCOME_CLASS_MISMATCH", $"declared class {declared.Class} does not match derived class {derived}");
            else if (declared.Class == OutcomeClass.Unsupported && declared.Subtype != derivedSubtype) Add("OUTCOME_SUBTYPE_MISMATCH", $"declared subtype '{declared.Subtype}' does not match derived subtype '{derivedSubtype}'");
            if (declared.ContextDependent)
            {
                var defaultTree = main.SyntaxTrees.SingleOrDefault(t => t.FilePath == p);
                var defaultActive = defaultTree is not null && defaultTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Any(n => n.Span.Start == start);
                if (defaultActive) Add("DEFAULT_CONTEXT_ACTIVE", "conditional site is active in the default context");
            }
        }

        var ownerNode = node?.Ancestors().FirstOrDefault(n => n is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax);
        answer.Add(new SiteAudit(k, p, span, lexeme, c.AssemblyName ?? c.Assembly.Identity.Name, declaredTarget, declaredOutcomeText, category,
            boundDoc, target?.ContainingAssembly?.Identity.ToString(), boundSourceDeclarations, boundEligible, candidateDocs, info.CandidateReason.ToString(),
            derived, declared.Class, derivedSubtype, declared.Subtype,
            ownerNode is null ? null : Doc(model.GetDeclaredSymbol(ownerNode)), errors));
    }
    return answer;
}

enum OutcomeClass { Confirmed, External, Ambiguous, Unresolved, Unsupported, NotEvaluated, Unrecognized }
enum OccurrenceCategory { OrdinaryInvocation, Constructor, MethodGroup, TypeOf, NameOf, Unknown }
enum TargetKind { Method, Constructor, TypeToken, Ambiguous, Unresolved }
sealed record TargetExpectation(string DocId, TargetKind Kind, bool EligibleLocal);
sealed record DeclaredOutcome(OutcomeClass Class, string? Subtype, bool ContextDependent) { public bool Recognized => Class != OutcomeClass.Unrecognized; }
sealed record SiteError(string Code, string Detail);
sealed record SiteAudit(string Case, string Path, int[] Span, string Lexeme, string Compilation, string DeclaredTarget, string DeclaredOutcome,
    OccurrenceCategory Category, string? BoundTargetDoc, string? BoundContainingAssembly, string[] BoundSourceDeclarations, bool BoundEligibleLocal,
    string?[] CandidateDocs, string CandidateReason, OutcomeClass DerivedClass, OutcomeClass DeclaredClass, string? DerivedSubtype, string? DeclaredSubtype,
    string? Owner, List<SiteError> Errors);
