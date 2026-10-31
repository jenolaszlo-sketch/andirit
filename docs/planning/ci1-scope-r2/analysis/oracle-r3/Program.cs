// ============================================================================
// CI-1-SCOPE-R3 corrected planning-only semantic oracle.
//
// Typed semantic assertion model (ci1-scope-r3/semantic-assertions/1):
//   * the declared outcome label is a serialized description of typed facts;
//   * every fact (binding outcome, exact target identity, eligibility,
//     invocation category, static-dispatch characteristic, cross-project
//     dependency context, after-replacement context, conditional-compilation
//     activity, lexical owner, repository provenance, lifecycle qualification)
//     is derived independently from fixture source + Roslyn and compared
//     against the claimed assertion;
//   * a display label alone can never satisfy a check.
//
// Pure Roslyn. No Andirit product reference. No CI-1A output exists or is read.
// Failure modes: structured diagnostics, never a crash; unreadable input is
// never silently treated as success.
// ============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CI1ScopeOracleR3;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var root = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var packet = Path.Combine(root, "docs/planning/ci1-scope-01");
var fixtures = Path.Combine(packet, "fixtures");
const string baselineCommit = "5c89ac0b3d0e87c0112880cf985826ded42b21b2";

var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase)
    .Select(p => MetadataReference.CreateFromFile(p)).ToArray<MetadataReference>();

var sourceInputs = new SortedDictionary<string, object>(StringComparer.Ordinal);
(string Path, string Text) ReadFixture(string rel)
{
    var full = Path.Combine(fixtures, rel);
    var bytes = File.ReadAllBytes(full);
    var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    using var ms = new MemoryStream(bytes);
    using var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
    var text = sr.ReadToEnd();
    sourceInputs.TryAdd("fixtures/" + rel.Replace('\\', '/'), new
    {
        path = "fixtures/" + rel.Replace('\\', '/'),
        sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)),
        bytes = bytes.Length,
        encoding = hasBom ? "utf-8-sig" : "utf-8",
        lineEndings = text.Contains("\r\n") ? "crlf" : "lf",
    });
    return (rel, text);
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

// Reference edges between source-built compilations (recorded construction
// bookkeeping; the cross-project facet requires one of these edges).
var referenceEdges = new Dictionary<string, string[]>(StringComparer.Ordinal)
{
    ["acceptance"] = Array.Empty<string>(), ["enabled"] = Array.Empty<string>(),
    ["repeated"] = Array.Empty<string>(), ["deleted"] = Array.Empty<string>(),
    ["shifted"] = Array.Empty<string>(), ["lib"] = Array.Empty<string>(),
    ["app"] = new[] { "lib" },
};
// Replacement-snapshot contexts: post-edit artifacts whose pre-image exists in
// the fixture set (mechanically paired, label-independent).
var replacementCompilations = new[] { "deleted", "shifted" };

var env = new OracleEnvironment(
    new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal)
    {
        ["acceptance"] = main, ["enabled"] = enabled, ["repeated"] = repeated,
        ["deleted"] = deleted, ["shifted"] = shifted, ["lib"] = lib, ["app"] = app,
    },
    referenceEdges, replacementCompilations, ReadFixture, fixtures, contexts);

// ---- frozen-table consistency guard: the oracle's own inputs must be coherent
if (Frozen.ExpectedCorpusKeys.Length != 32 || Frozen.ExpectedOwners.Count != 32
    || Frozen.ExpectedCorpusKeys.Any(k => !Frozen.ExpectedOwners.ContainsKey($"{k.Case}|{k.Path}|{k.Span[0]},{k.Span[1]}")))
{
    Console.WriteLine(JsonSerializer.Serialize(new { error = "frozen oracle tables are internally inconsistent" }));
    return 3;
}

var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(fixtures, "expected-sites.json")))!;
var baseline = R3Audit.Audit(manifest, env);

// ============================================================================
// Mutation qualification — three tiers
// ============================================================================
var mutations = new List<object>();
var structuralCodes = new HashSet<string> { "COORDINATE_LEXEME_MISMATCH", "DUPLICATE_OCCURRENCE", "SYNTAX_MISSING", "SYNTAX_CATEGORY_UNKNOWN" };

void Mutate(string tier, string id, string description, string? original, string? mutated, string? expectedReason,
    Action<JsonNode> change, Action<Dictionary<string, string>>? ownersChange = null)
{
    var m = manifest.DeepClone();
    change(m);
    Dictionary<string, string>? owners = null;
    if (ownersChange is not null)
    {
        owners = new Dictionary<string, string>(Frozen.ExpectedOwners, StringComparer.Ordinal);
        ownersChange(owners);
    }
    AuditResult result;
    try { result = R3Audit.Audit(m, env, owners); }
    catch (Exception ex)
    {
        mutations.Add(new { tier, id, description, originalValue = original, mutatedValue = mutated, expectedRejectionReason = expectedReason, crashed = true, crash = ex.GetType().Name + ": " + ex.Message, detected = false, rejectedForIntendedReason = false, acceptedAsControl = false, reasonCodes = Array.Empty<string>(), failingCases = Array.Empty<object>(), globalErrors = Array.Empty<object>() });
        return;
    }
    var failing = result.Sites.Where(a => a.Errors.Count > 0).ToList();
    var siteCodes = failing.SelectMany(a => a.Errors).Select(e => e.Code);
    var codes = siteCodes.Concat(result.GlobalErrors.Select(g => g.Code)).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToArray();
    var detected = result.AnyFailure;
    var unrelatedOnly = detected && expectedReason is not null && !codes.Contains(expectedReason) && codes.Any(c => structuralCodes.Contains(c));
    mutations.Add(new
    {
        tier,
        id,
        description,
        originalValue = original,
        mutatedValue = mutated,
        expectedRejectionReason = expectedReason,
        crashed = false,
        crash = (string?)null,
        detected,
        failingRows = failing.Count,
        primaryReasonCode = failing.SelectMany(a => a.Errors).Select(e => e.Code).FirstOrDefault() ?? result.GlobalErrors.FirstOrDefault()?.Code,
        reasonCodes = codes,
        rejectedForIntendedReason = detected && (expectedReason is null ? false : codes.Contains(expectedReason)),
        acceptedAsControl = expectedReason is null && !detected,
        unrelatedValidationOnly = unrelatedOnly,
        failingCases = failing.Select(a => new { a.Case, a.Path, codes = a.Errors.Select(e => e.Code).ToArray() }).ToArray(),
        globalErrors = result.GlobalErrors.Select(g => new { g.Code, g.Detail }).ToArray(),
    });
}

static JsonNode SiteByCase(JsonNode m, string cs) => m["Sites"]!.AsArray().First(s => (string?)s!["Case"] == cs)!;
static void SetOutcome(JsonNode m, string cs, string v) => SiteByCase(m, cs)["ExpectedOutcome"] = v;
static void SetOutcomeByValue(JsonNode m, string current, string v) => m["Sites"]!.AsArray().First(s => (string?)s!["ExpectedOutcome"] == current)!["ExpectedOutcome"] = v;

// ---- Tier A: the Q2 named regressions and the full R2 required set (unchanged).
Mutate("A", "M01-external-to-confirmed", "C10 external platform call relabelled confirmed", "external-no-confirmed-local-target", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => SetOutcomeByValue(m, "external-no-confirmed-local-target", "confirmed"));
Mutate("A", "M02-confirmed-to-external", "C01 confirmed local call relabelled external", "confirmed", "external-no-confirmed-local-target", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "external-no-confirmed-local-target");
Mutate("A", "M03-confirmed-to-deferred", "C01 confirmed local call relabelled deferred", "confirmed", "deferred-method-group", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "deferred-method-group");
Mutate("A", "M04-invalid-outcome", "C01 recognised outcome replaced with an unrecognised string", "confirmed", "invalid-q2-outcome", "OUTCOME_UNRECOGNIZED",
    m => m["Sites"]![0]!["ExpectedOutcome"] = "invalid-q2-outcome");
Mutate("A", "M05-ambiguous-to-confirmed", "C11 ambiguous Pick(null) relabelled confirmed", "ambiguous-no-confirmation", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => SetOutcome(m, "C11", "confirmed"));
Mutate("A", "M06-unresolved-to-confirmed", "C11 unresolved Missing() relabelled confirmed", "unresolved-no-confirmation", "confirmed", "OUTCOME_CLASS_MISMATCH",
    m => m["Sites"]!.AsArray().Last(s => (string?)s!["Case"] == "C11")!["ExpectedOutcome"] = "confirmed");
Mutate("A", "M07-wrong-overload", "C02 Hit(1) declared target changed to the string overload", "CI1Corpus.Overloads.Hit(int)", "CI1Corpus.Overloads.Hit(string)", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![3]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Overloads.Hit(string)");
Mutate("A", "M08-confirmed-to-wrong-assembly", "C03 cross-project target changed to a different-assembly external target", "CI1Cross.Library.Work() in Lib/Lib.csproj", "System.Console.WriteLine(string)", "TARGET_IDENTITY_MISMATCH",
    m => SiteByCase(m, "C03")["ExpectedTargetSourceSignature"] = "System.Console.WriteLine(string)");
Mutate("A", "M09-confirmed-to-unrelated-indexed-method", "C01 declared target changed to an unrelated indexed local method", "Logger.Write(string)", "CI1Corpus.Target.Work()", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![0]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Target.Work()");
Mutate("A", "M10-correct-target-incorrect-span", "C01 start column corrupted", "[5,9,5,30]", "[5,10,5,30]", "COORDINATE_LEXEME_MISMATCH",
    m => m["Sites"]![0]!["Span"]![1] = 10);
Mutate("A", "M11-duplicate-entry", "C01 first site duplicated", "32 unique rows", "33 rows with duplicate occurrence", "DUPLICATE_OCCURRENCE",
    m => m["Sites"]!.AsArray().Add(m["Sites"]![0]!.DeepClone()));
Mutate("A", "P01-correct-outcome-accepted", "positive control: unchanged manifest, no corruption", "(unchanged)", "(unchanged)", null, _ => { });

// ---- Tier B: complete Q3 adversarial set replayed against the corrected model.
Mutate("B", "OUT-01-confirmed-to-cross-project", "C01 'confirmed' -> 'confirmed-cross-project'", "confirmed", "confirmed-cross-project", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C01", "confirmed-cross-project"));
Mutate("B", "OUT-02-confirmed-to-interface-static", "C01 'confirmed' -> 'confirmed-interface-static-target'", "confirmed", "confirmed-interface-static-target", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C01", "confirmed-interface-static-target"));
Mutate("B", "OUT-03-confirmed-to-after-replacement", "C01 'confirmed' -> 'confirmed-after-replacement'", "confirmed", "confirmed-after-replacement", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C01", "confirmed-after-replacement"));
Mutate("B", "OUT-04-interface-to-plain-confirmed", "C06 'confirmed-interface-static-target' -> 'confirmed'", "confirmed-interface-static-target", "confirmed", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C06", "confirmed"));
Mutate("B", "OUT-05-cross-project-to-plain", "C03 'confirmed-cross-project' -> 'confirmed'", "confirmed-cross-project", "confirmed", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C03", "confirmed"));
Mutate("B", "OUT-06-after-replacement-to-plain", "C14 'confirmed-after-replacement' -> 'confirmed'", "confirmed-after-replacement", "confirmed", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C14", "confirmed"));
Mutate("B", "OUT-07-external-to-delegate", "C10 'external-no-confirmed-local-target' -> 'external-delegate-invoke-not-Target.Work'", "external-no-confirmed-local-target", "external-delegate-invoke-not-Target.Work", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C10", "external-delegate-invoke-not-Target.Work"));
Mutate("B", "OUT-08-delegate-to-external", "C08 delegate-invoke row 'external-delegate-invoke-not-Target.Work' -> 'external-no-confirmed-local-target' (R3 correction: Q3's SiteByCase('C08') selected the method-group row; the delegate row is selected by outcome value here)", "external-delegate-invoke-not-Target.Work", "external-no-confirmed-local-target", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcomeByValue(m, "external-delegate-invoke-not-Target.Work", "external-no-confirmed-local-target"));
Mutate("B", "OUT-09-c20-to-plain-confirmed", "C20 'inactive-default; confirmed only with CI1_EXTRA' -> 'confirmed' (drops the conditional assertion)", "inactive-default; confirmed only with CI1_EXTRA", "confirmed", "CONDITIONAL_ACTIVITY_MISMATCH",
    m => SetOutcomeByValue(m, "inactive-default; confirmed only with CI1_EXTRA", "confirmed"));
Mutate("B", "OUT-10-plain-to-context-dependent", "C01 'confirmed' -> 'inactive-default; confirmed only with CI1_EXTRA' (fabricates conditional activity)", "confirmed", "inactive-default; confirmed only with CI1_EXTRA", "CONDITIONAL_ACTIVITY_MISMATCH",
    m => SetOutcome(m, "C01", "inactive-default; confirmed only with CI1_EXTRA"));

Mutate("B", "VOCAB-01-capitalized", "C01 'confirmed' -> 'Confirmed'", "confirmed", "Confirmed", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "Confirmed"));
Mutate("B", "VOCAB-02-leading-space", "C01 -> ' confirmed'", "confirmed", " confirmed", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", " confirmed"));
Mutate("B", "VOCAB-03-trailing-space", "C01 -> 'confirmed '", "confirmed", "confirmed ", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "confirmed "));
Mutate("B", "VOCAB-04-prefix-suffix", "C01 -> 'confirmedx'", "confirmed", "confirmedx", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "confirmedx"));
Mutate("B", "VOCAB-05-prefix-bogus", "C01 -> 'confirmed-bogus'", "confirmed", "confirmed-bogus", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "confirmed-bogus"));
Mutate("B", "VOCAB-06-deferred-prefix", "C01 -> 'deferred-method'", "confirmed", "deferred-method", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "deferred-method"));
Mutate("B", "VOCAB-07-bare-external", "C01 -> 'external'", "confirmed", "external", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "external"));
Mutate("B", "VOCAB-08-bare-unsupported", "C01 -> 'unsupported'", "confirmed", "unsupported", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "unsupported"));
Mutate("B", "VOCAB-09-empty", "C01 -> ''", "confirmed", "", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", ""));
Mutate("B", "VOCAB-10-uppercase", "C01 -> 'CONFIRMED'", "confirmed", "CONFIRMED", "OUTCOME_UNRECOGNIZED", m => SetOutcome(m, "C01", "CONFIRMED"));

Mutate("B", "TARGET-01-path-swap-cross-assembly", "C01 row0 re-pathed to the AfterDelete replacement fixture (identical lexeme/span, different source-built assembly, same doc ID)", "Repeated.cs", "acceptance/Repeated.AfterDelete.cs.txt", "CORPUS_ROW_UNEXPECTED",
    m => m["Sites"]![0]!["FixturePath"] = "acceptance/Repeated.AfterDelete.cs.txt");
Mutate("B", "TARGET-02-repository-path-tamper", "C01 RepositoryPath -> 'src/WRONG/PATH.cs'", "src/Repeated.cs", "src/WRONG/PATH.cs", "REPOSITORY_PATH_MISMATCH",
    m => m["Sites"]![0]!["RepositoryPath"] = "src/WRONG/PATH.cs");
Mutate("B", "TARGET-03-basis-tamper", "C01 Basis -> 'tampered by Q3'", Frozen.BasisPlain, "tampered by Q3", "BASIS_UNRECOGNIZED",
    m => m["Sites"]![0]!["Basis"] = "tampered by Q3");
Mutate("B", "TARGET-04-type-token-to-method", "C09 typeof(Target) declared target 'CI1Corpus.Target' -> 'CI1Corpus.Target.Work()'", "CI1Corpus.Target", "CI1Corpus.Target.Work()", "TARGET_IDENTITY_MISMATCH",
    m => SiteByCase(m, "C09")["ExpectedTargetSourceSignature"] = "CI1Corpus.Target.Work()");
Mutate("B", "TARGET-05-external-target-swap", "C10 declared target -> 'System.Action.Invoke()'", "System.Console.WriteLine(string)", "System.Action.Invoke()", "TARGET_IDENTITY_MISMATCH",
    m => SiteByCase(m, "C10")["ExpectedTargetSourceSignature"] = "System.Action.Invoke()");
Mutate("B", "TARGET-06-ambiguous-to-specific", "C11 Pick(null) declared target -> 'CI1Corpus.Overloads.Hit(int)'", "unknown-string-or-Exception-overload", "CI1Corpus.Overloads.Hit(int)", "TARGET_IDENTITY_MISMATCH",
    m => SiteByCase(m, "C11")["ExpectedTargetSourceSignature"] = "CI1Corpus.Overloads.Hit(int)");
Mutate("B", "TARGET-07-case-field-unvalidated", "C01 Case -> 'CXX' (the case label is part of the frozen occurrence identity)", "C01", "CXX", "CORPUS_ROW_UNEXPECTED",
    m => m["Sites"]![0]!["Case"] = "CXX");

Mutate("B", "CORPUS-01-remove-row", "remove C01 row0 (32 -> 31 rows)", "32 rows", "31 rows", "CORPUS_ROW_MISSING",
    m => m["Sites"]!.AsArray().RemoveAt(0));
Mutate("B", "CORPUS-02-remove-conditional-row", "remove the C20 row", "32 rows", "31 rows", "CORPUS_ROW_MISSING",
    m => { var a = m["Sites"]!.AsArray(); a.RemoveAt(a.Select((s, i) => (s, i)).First(t => (string?)t.s!["Case"] == "C20").i); });
Mutate("B", "CORPUS-03-remove-all-replacement-rows", "remove every C14/C15 row", "32 rows", "27 rows", "CORPUS_ROW_MISSING",
    m => { var a = m["Sites"]!.AsArray(); for (var i = a.Count - 1; i >= 0; i--) { var c = (string?)a[i]!["Case"]; if (c == "C14" || c == "C15") a.RemoveAt(i); } });
Mutate("B", "CORPUS-04-add-valid-new-row", "append a new internally consistent occurrence (Logger.Write sub-span, deferred-method-group)", "32 rows", "33 rows", "CORPUS_ROW_UNEXPECTED",
    m => { var a = m["Sites"]!.AsArray(); var row = (JsonObject)a[0]!.DeepClone(); row["Case"] = "C90"; row["Span"] = new JsonArray(5, 9, 5, 21); row["Lexeme"] = "Logger.Write"; row["ExpectedOutcome"] = "deferred-method-group"; a.Add(row); });

Mutate("B", "MAL-01-short-span", "Span has 2 elements", "[5,9,5,30]", "[5,9]", "COORDINATE_LEXEME_MISMATCH", m => m["Sites"]![0]!["Span"] = new JsonArray(5, 9));
Mutate("B", "MAL-02-span-not-array", "Span is a string", "[5,9,5,30]", "\"5,9,5,30\"", "MANIFEST_STRUCTURE_INVALID", m => m["Sites"]![0]!["Span"] = "5,9,5,30");
Mutate("B", "MAL-03-span-not-int", "Span contains non-integers", "[5,9,5,30]", "[\"a\",\"b\",\"c\",\"d\"]", "MANIFEST_STRUCTURE_INVALID", m => m["Sites"]![0]!["Span"] = new JsonArray("a", "b", "c", "d"));
Mutate("B", "MAL-04-missing-outcome", "ExpectedOutcome key removed", "present", "removed", "MANIFEST_STRUCTURE_INVALID", m => ((JsonObject)m["Sites"]![0]!).Remove("ExpectedOutcome"));
Mutate("B", "MAL-05-missing-lexeme", "Lexeme key removed", "present", "removed", "MANIFEST_STRUCTURE_INVALID", m => ((JsonObject)m["Sites"]![0]!).Remove("Lexeme"));
Mutate("B", "MAL-06-line-out-of-range", "Span references line 999", "line 5", "line 999", "COORDINATE_LEXEME_MISMATCH",
    m => { m["Sites"]![0]!["Span"]![0] = 999; m["Sites"]![0]!["Span"]![2] = 999; });
Mutate("B", "MAL-07-negative-line", "Span references line -1", "line 5", "line -1", "COORDINATE_LEXEME_MISMATCH",
    m => { m["Sites"]![0]!["Span"]![0] = -1; m["Sites"]![0]!["Span"]![2] = -1; });

// ---- Tier C: R3 regressions for the corrected typed model.
string Slug(string s) => new string(s.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray());

var siteOutcomes = ((JsonArray)manifest["Sites"]!).Select(s => (string?)s!["ExpectedOutcome"]).ToArray();
for (var i = 0; i < siteOutcomes.Length; i++)
{
    var current = siteOutcomes[i]!;
    if (!Frozen.ConfirmedFamily.Contains(current)) continue;
    foreach (var label in Frozen.ConfirmedFamily.Where(l => l != current))
    {
        var expected = current.Contains("CI1_EXTRA") || label.Contains("CI1_EXTRA")
            ? "CONDITIONAL_ACTIVITY_MISMATCH" : "OUTCOME_PROVENANCE_MISMATCH";
        var idx = i;
        Mutate("C", $"R01-sweep-{idx}-{Slug(label)}", $"row {idx} ('{current}') relabelled '{label}'", current, label, expected,
            m => m["Sites"]![idx]!["ExpectedOutcome"] = label);
    }
}

Mutate("C", "R02a-external-swap-delegate-to-plain", "C10 external platform call relabelled external-delegate-invoke (dup of OUT-07, tier-C regression slot)", "external-no-confirmed-local-target", "external-delegate-invoke-not-Target.Work", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcome(m, "C10", "external-delegate-invoke-not-Target.Work"));
Mutate("C", "R02b-external-swap-plain-to-delegate", "C08 delegate invoke relabelled external-no-confirmed-local-target (dup of corrected OUT-08)", "external-delegate-invoke-not-Target.Work", "external-no-confirmed-local-target", "OUTCOME_PROVENANCE_MISMATCH",
    m => SetOutcomeByValue(m, "external-delegate-invoke-not-Target.Work", "external-no-confirmed-local-target"));

Mutate("C", "R03a-conditional-bypass-cross-project", "C20 relabelled 'confirmed-cross-project' (drops conditional assertion AND fabricates cross-project)", "inactive-default; confirmed only with CI1_EXTRA", "confirmed-cross-project", "CONDITIONAL_ACTIVITY_MISMATCH",
    m => SetOutcomeByValue(m, "inactive-default; confirmed only with CI1_EXTRA", "confirmed-cross-project"));
Mutate("C", "R03b-conditional-fabricated-c02", "C02 Hit(1) relabelled to the conditional label", "confirmed", "inactive-default; confirmed only with CI1_EXTRA", "CONDITIONAL_ACTIVITY_MISMATCH",
    m => m["Sites"]![3]!["ExpectedOutcome"] = "inactive-default; confirmed only with CI1_EXTRA");
Mutate("C", "R03c-conditional-fabricated-c09", "C09 Target.Work() relabelled to the conditional label", "confirmed", "inactive-default; confirmed only with CI1_EXTRA", "CONDITIONAL_ACTIVITY_MISMATCH",
    m => m["Sites"]![18]!["ExpectedOutcome"] = "inactive-default; confirmed only with CI1_EXTRA");

Mutate("C", "R05a-same-class-wrong-definition", "C12 Partial.Work() declared target changed to Target.Work() (same class, wrong definition)", "CI1Corpus.Partial.Work()", "CI1Corpus.Target.Work()", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![22]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Target.Work()");
Mutate("C", "R05b-same-class-wrong-definition", "C09 Target.Work() declared target changed to Partial.Work() (same class, wrong definition)", "CI1Corpus.Target.Work()", "CI1Corpus.Partial.Work()", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![18]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Partial.Work()");

Mutate("C", "R06-wrong-overload-reverse", "C02 Hit(\"x\") declared target changed to the int overload", "CI1Corpus.Overloads.Hit(string)", "CI1Corpus.Overloads.Hit(int)", "TARGET_IDENTITY_MISMATCH",
    m => m["Sites"]![4]!["ExpectedTargetSourceSignature"] = "CI1Corpus.Overloads.Hit(int)");

Mutate("C", "R07a-wrong-owner-swap", "owner-channel probe: C06/C07 expected lexical owners swapped (manifest unchanged; oracle-owned owner channel perturbed)", "Dispatch.InterfaceCall / Dispatch.VirtualCall", "Dispatch.VirtualCall / Dispatch.InterfaceCall", "OWNER_MISMATCH",
    _ => { }, owners => { owners["C06|acceptance/Cases.cs|36,55"] = "M:CI1Corpus.Dispatch.VirtualCall"; owners["C07|acceptance/Cases.cs|37,52"] = "M:CI1Corpus.Dispatch.InterfaceCall"; });
Mutate("C", "R07b-wrong-owner-unrelated", "owner-channel probe: C01 row0 expected owner replaced by an unrelated method (manifest unchanged)", "M:Worker.Execute", "M:CI1Corpus.Target.Work", "OWNER_MISMATCH",
    _ => { }, owners => owners["C01|Repeated.cs|5,9"] = "M:CI1Corpus.Target.Work");

Mutate("C", "R08-wrong-repository-path-c03", "C03 RepositoryPath -> 'src/Cases.cs'", "App/App.cs", "src/Cases.cs", "REPOSITORY_PATH_MISMATCH",
    m => SiteByCase(m, "C03")["RepositoryPath"] = "src/Cases.cs");

Mutate("C", "R09-unsupported-basis-claim", "C02 Hit(1) Basis extended with an extractor-verified claim (unsupported provenance assertion)", Frozen.BasisPlain, Frozen.BasisPlain + " ; verified by extractor output", "BASIS_UNRECOGNIZED",
    m => m["Sites"]![3]!["Basis"] = Frozen.BasisPlain + " ; verified by extractor output");

Mutate("C", "R10-missing-cross-project-row", "remove the C03 cross-project row", "32 rows", "31 rows", "CORPUS_ROW_MISSING",
    m => { var a = m["Sites"]!.AsArray(); a.RemoveAt(a.Select((s, i) => (s, i)).First(t => (string?)t.s!["Case"] == "C03").i); });

Mutate("C", "R11a-extra-duplicate-clone", "append a clone of row0 with Case 'C91' (same path/span)", "32 rows", "33 rows", "CORPUS_ROW_UNEXPECTED",
    m => { var a = m["Sites"]!.AsArray(); var row = (JsonObject)a[0]!.DeepClone(); row["Case"] = "C91"; a.Add(row); });
Mutate("C", "R11b-extra-new-occurrence", "append an internally consistent occurrence outside the frozen universe (Logger sub-span)", "32 rows", "33 rows", "CORPUS_ROW_UNEXPECTED",
    m => { var a = m["Sites"]!.AsArray(); var row = (JsonObject)a[0]!.DeepClone(); row["Case"] = "C92"; row["Span"] = new JsonArray(5, 9, 5, 15); row["Lexeme"] = "Logger"; a.Add(row); });

Mutate("C", "R12a-malformed-sites-not-array", "top-level 'Sites' replaced by a string", "array(32)", "\"not-an-array\"", "MANIFEST_STRUCTURE_INVALID",
    m => m["Sites"] = "not-an-array");
Mutate("C", "R12b-malformed-row-not-object", "Sites[0] replaced by a string element", "object", "\"junk\"", "MANIFEST_STRUCTURE_INVALID",
    m => m["Sites"]!.AsArray()[0] = JsonValue.Create("junk"));
Mutate("C", "R12c-malformed-missing-fixturepath", "Sites[0] FixturePath key removed", "present", "removed", "MANIFEST_STRUCTURE_INVALID",
    m => ((JsonObject)m["Sites"]![0]!).Remove("FixturePath"));

Mutate("C", "R13a-unverifiable-unknown-project", "C01 declared target qualified with an unknown project", "Logger.Write(string)", "Logger.Write(string) in Nowhere/Nowhere.csproj", "TARGET_UNRECOGNIZED",
    m => m["Sites"]![0]!["ExpectedTargetSourceSignature"] = "Logger.Write(string) in Nowhere/Nowhere.csproj");
Mutate("C", "R13b-unverifiable-unqualified-cross-project", "C03 declared target loses its project qualifier", "CI1Cross.Library.Work() in Lib/Lib.csproj", "CI1Cross.Library.Work()", "TARGET_UNRECOGNIZED",
    m => SiteByCase(m, "C03")["ExpectedTargetSourceSignature"] = "CI1Cross.Library.Work()");
Mutate("C", "R13c-unverifiable-unknown-target", "C09 Target.Work() declared target replaced by a nonexistent definition", "CI1Corpus.Target.Work()", "CI1Corpus.DoesNotExist.Work()", "TARGET_UNRECOGNIZED",
    m => m["Sites"]![18]!["ExpectedTargetSourceSignature"] = "CI1Corpus.DoesNotExist.Work()");

Mutate("C", "R14-unchanged-manifest-accepted", "positive control (tier C): unchanged manifest, typed assertions all agree", "(unchanged)", "(unchanged)", null, _ => { });

// ============================================================================
// Adversarial scenarios
// ============================================================================

// ---- Q3-F02: two source-built assemblies exposing the same documentation ID.
CSharpCompilation BuildScenarioComp(string name, (string Path, string Text)[] sources, MetadataReference[]? extra = null)
    => CSharpCompilation.Create(name,
        sources.Select(s => CSharpSyntaxTree.ParseText(s.Text, new CSharpParseOptions(LanguageVersion.Latest), s.Path)),
        references.Concat(extra ?? []), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: false));

var abDecl = "namespace N { public static class C { public static void M() { } } }";
var compA = BuildScenarioComp("A-src", new[] { ("A.cs", abDecl) });
var compB = BuildScenarioComp("B-src", new[] { ("B.cs", abDecl) });
var abCaller = BuildScenarioComp("caller-probe", new[] { ("Caller.cs", "namespace N { public static class CallerP { public static void Run() { C.M(); } } }") }, extra: [compB.ToMetadataReference()]);
var abEnv = new OracleEnvironment(
    new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal) { ["A-src"] = compA, ["B-src"] = compB, ["caller-probe"] = abCaller },
    new Dictionary<string, string[]>(StringComparer.Ordinal) { ["caller-probe"] = new[] { "B-src" } },
    Array.Empty<string>(), _ => throw new InvalidOperationException("no fixture reads in the A/B scenario"), "", new List<object>());
var abTree = abCaller.SyntaxTrees.Single();
var abInvocation = abTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var abBound = (IMethodSymbol)abCaller.GetSemanticModel(abTree).GetSymbolInfo(abInvocation).Symbol!;
var abDoc = Sem.Doc(abBound);
var abDeclsA = Sem.ResolveDeclarationsByDoc(compA, abDoc!);
var abDeclsB = Sem.ResolveDeclarationsByDoc(compB, abDoc!);
var abDescBound = Sem.DescribeDefinition(abBound, abEnv)!;
var abDescA = Sem.DescribeDefinition(abDeclsA.Single(), abEnv)!;
var abDescB = Sem.DescribeDefinition(abDeclsB.Single(), abEnv)!;
var abR2Eligible = Sem.EligibleLocal(abBound, new[] { compA, compB, abCaller });
var abRejectsWrongAssembly = abDescBound.Compilation != abDescA.Compilation && abDescBound.Compilation == "B-src";
var abAcceptsCorrectAssembly = Sem.DescriptorEquals(abDescBound, abDescB);
var abRequirementSatisfied = abDoc == "M:N.C.M" && abR2Eligible && abRejectsWrongAssembly && abAcceptsCorrectAssembly;
var abScenario = new
{
    name = "Q3-F02 two-source-built-assembly exact-identity probe (A/B)",
    note = "A-src and B-src both declare N.C.M (doc M:N.C.M); the caller references B-src. R2's eligibility (source-built set membership + doc ID) cannot distinguish; the R3 exact-identity rule compares defining compilation, assembly identity and declaration location.",
    documentationId = abDoc,
    boundContainingAssembly = abBound.ContainingAssembly!.Identity.ToString(),
    r2EligibleLocalOfBoundTarget = abR2Eligible,
    r2WouldDeriveClass = abR2Eligible ? "confirmed" : "external",
    r3BoundDefinition = abDescBound,
    r3CheckAgainstExpectedA = new { expectedDefinition = abDescA, definingProjectMatches = false, result = "TARGET_DEFINING_PROJECT_MISMATCH (rejected; bounded unverified, never guessed)" },
    r3CheckAgainstExpectedB = new { expectedDefinition = abDescB, definingProjectMatches = true, result = "accepted (the only expectation the binding actually satisfies)" },
    requirementSatisfied = abRequirementSatisfied,
};

// ---- F01 collision defence, preserved and strengthened by the metadata rule.
var r2FixtureRoot = Path.Combine(root, "docs/planning/ci1-scope-r2/analysis/fixtures");
var collisionFixtureFiles = new[] { "f01-collision/A/Shadow.cs", "f01-collision/A/Shadow.csproj", "f01-collision/Z/App.cs", "f01-collision/Z/App.csproj" }
    .Select(rel => { var b = File.ReadAllBytes(Path.Combine(r2FixtureRoot, rel)); return new { path = rel, sha256 = Convert.ToHexStringLower(SHA256.HashData(b)), bytes = b.Length }; }).ToArray();
var collisionShadow = BuildScenarioComp("Shadow", new[] { ("Shadow.cs", File.ReadAllText(Path.Combine(r2FixtureRoot, "f01-collision/A/Shadow.cs"))) });
var collisionApp = BuildScenarioComp("App", new[] { ("App.cs", File.ReadAllText(Path.Combine(r2FixtureRoot, "f01-collision/Z/App.cs"))) });
var collisionEnv = new OracleEnvironment(
    new Dictionary<string, CSharpCompilation>(StringComparer.Ordinal) { ["Shadow"] = collisionShadow, ["App"] = collisionApp },
    new Dictionary<string, string[]>(StringComparer.Ordinal), Array.Empty<string>(),
    _ => throw new InvalidOperationException("no fixture reads in the collision scenario"), "", new List<object>());
var collisionTree = collisionApp.SyntaxTrees.Single();
var collisionInvocation = collisionTree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().Single();
var collisionBound = (IMethodSymbol)collisionApp.GetSemanticModel(collisionTree).GetSymbolInfo(collisionInvocation).Symbol!;
var collisionShadowDecl = (IMethodSymbol)collisionShadow.GetSemanticModel(collisionShadow.SyntaxTrees.Single())
    .GetDeclaredSymbol(collisionShadow.SyntaxTrees.Single().GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single())!;
var collisionDoc = Sem.Doc(collisionBound);
var collisionBoundDesc = Sem.DescribeDefinition(collisionBound, collisionEnv)!;
var collisionShadowDesc = Sem.DescribeDefinition(collisionShadowDecl, collisionEnv)!;
var collisionR3MetadataRuleAcceptsBound = collisionBoundDesc.Compilation == "metadata";
var collisionR3MetadataRuleRejectsSourceShadow = collisionShadowDesc.Compilation != "metadata";
var collisionScenario = new
{
    name = "F01 external/local documentation-ID collision",
    documentationId = collisionDoc,
    indexedLocalDeclaration = new { assembly = collisionShadowDecl.ContainingAssembly!.Identity.ToString(), definition = collisionShadowDesc },
    boundTarget = new { assembly = collisionBound.ContainingAssembly!.Identity.ToString(), definition = collisionBoundDesc },
    r3MetadataRuleAcceptsPlatformBoundCall = collisionR3MetadataRuleAcceptsBound,
    r3MetadataRuleRejectsSourceBuiltShadow = collisionR3MetadataRuleRejectsSourceShadow,
    requirementSatisfied = collisionR3MetadataRuleAcceptsBound && collisionR3MetadataRuleRejectsSourceShadow,
};

// ---- Owner-channel distinction on the corpus (six rows share one doc ID).
// The channel distinguishes lexical methods: every distinct lexical method
// containing one of these occurrences has its own owner identity (two
// occurrences inside the same method legitimately share that method's owner).
var ownerRows = baseline.Sites.Where(s => s.BoundTargetDoc == "M:CI1Corpus.Target.Work").ToArray();
var ownerMethods = ownerRows.GroupBy(s => s.DerivedOwner).ToList();
var ownerScenario = new
{
    name = "lexical-owner channel distinguishes same-doc-ID occurrences (corpus-native S3)",
    occurrences = ownerRows.Select(s => new { s.Case, s.Path, s.Span, s.BoundTargetDoc, derivedOwner = s.DerivedOwner, expectedOwner = s.ExpectedOwner }).ToArray(),
    distinctLexicalMethods = ownerMethods.Count,
    distinctOwners = ownerRows.Select(s => s.DerivedOwner).Distinct().Count(),
    ownerChannelDistinguishes = ownerMethods.Count >= 3
        && ownerMethods.Count == ownerRows.Select(s => s.DerivedOwner).Distinct().Count()
        && ownerRows.All(s => s.DerivedOwner == s.ExpectedOwner),
    enforcedBy = "OWNER_MISMATCH (tier C R07a/R07b)",
};

// ============================================================================
// Summaries
// ============================================================================
static bool Rejected(object m)
{
    var t = m.GetType();
    return (bool)t.GetProperty("rejectedForIntendedReason")!.GetValue(m)!;
}
static bool Detected(object m) => (bool)m.GetType().GetProperty("detected")!.GetValue(m)!;
static bool Crashed(object m) => (bool)m.GetType().GetProperty("crashed")!.GetValue(m)!;
static string Id(object m) => (string)m.GetType().GetProperty("id")!.GetValue(m)!;
static string Tier(object m) => (string)m.GetType().GetProperty("tier")!.GetValue(m)!;

var q2Named = new[] { "M01-external-to-confirmed", "M02-confirmed-to-external", "M03-confirmed-to-deferred", "M04-invalid-outcome" };
var tierA = mutations.Where(m => Tier(m) == "A").ToList();
var tierB = mutations.Where(m => Tier(m) == "B").ToList();
var tierC = mutations.Where(m => Tier(m) == "C").ToList();
var q2NamedRegressionsRejected = tierA.Where(m => q2Named.Contains(Id(m))).All(Rejected);
var tierAAllRejected = tierA.Where(m => Id(m) != "P01-correct-outcome-accepted").All(Rejected);
var positiveControlAccepted = tierA.Where(m => Id(m) == "P01-correct-outcome-accepted").All(m => !Detected(m));
var tierBAllRejected = tierB.All(Rejected) && !tierB.Any(Crashed);
var tierCAllRejected = tierC.Where(m => Id(m) != "R14-unchanged-manifest-accepted").All(Rejected) && !tierC.Any(Crashed);
var tierCControlAccepted = tierC.Where(m => Id(m) == "R14-unchanged-manifest-accepted").All(m => !Detected(m));
var noCrashes = !mutations.Any(Crashed);
var allRequiredMutationsRejected = tierAAllRejected && tierBAllRejected && tierCAllRejected;
var baselineClean = baseline.Sites.Count == 32 && baseline.Sites.All(s => s.Errors.Count == 0) && baseline.GlobalErrors.Count == 0;
var universeComplete = baseline.GlobalErrors.Count == 0
    && baseline.Sites.All(s => !s.Errors.Any(e => e.Code.StartsWith("CORPUS_", StringComparison.Ordinal)));
var ownerChecksEnforced = baseline.Sites.All(s => s.ExpectedOwner is not null && s.DerivedOwner == s.ExpectedOwner);
var repositoryPathsVerified = baseline.Sites.All(s => s.RepositoryPathVerdict == "verified");
var basisRecognized = baseline.Sites.All(s => s.BasisVerdict.StartsWith("recognized", StringComparison.Ordinal));
var lifecycleRows = baseline.Sites.Where(s => s.Qualifications.Any(q => q.Code == "LIFECYCLE_NOT_QUALIFIED")).ToList();

var failedRows = baseline.Sites.Count(s => s.Errors.Count > 0);
var rejectedCount = mutations.Count(Rejected);
var overallPass = baselineClean && universeComplete && ownerChecksEnforced && repositoryPathsVerified && basisRecognized
    && q2NamedRegressionsRejected && allRequiredMutationsRejected && positiveControlAccepted && tierCControlAccepted
    && noCrashes && abRequirementSatisfied && collisionScenario.requirementSatisfied
    && ownerScenario.ownerChannelDistinguishes && lifecycleRows.Count == 5;

var rowSemantics = baseline.Sites.Select(a => new
{
    a.Case, a.Path, a.Span, a.Lexeme,
    declaredTarget = a.DeclaredTarget,
    declaredOutcome = a.DeclaredOutcome,
    derivedClass = a.DerivedClass.ToString(),
    declaredClass = a.DeclaredClass.ToString(),
    derivedDispatch = a.DerivedDispatch.ToString(),
    derivedCrossProject = a.DerivedCrossProject,
    derivedAfterReplacement = a.DerivedAfterReplacement,
    conditional = a.Conditional,
    a.BoundTargetDoc,
    a.BoundContainingAssembly,
    a.BoundEligibleLocal,
    a.ExpectedDeclaringProject,
    boundDefinition = a.BoundDefinition,
    expectedDefinition = a.ExpectedDefinition,
    a.SymbolEqualityAgrees,
    derivedOwner = a.DerivedOwner,
    expectedOwner = a.ExpectedOwner,
    repositoryPathVerdict = a.RepositoryPathVerdict,
    basisVerdict = a.BasisVerdict,
    errors = a.Errors.Select(e => e.Code).ToArray(),
    qualifications = a.Qualifications.Select(q => q.Code).ToArray(),
}).ToArray();

var result = new
{
    harness = "CI-1-SCOPE-R3 typed semantic assertion oracle",
    schemaVersion = "ci1-scope-r3/semantic-assertions/1",
    independence = "Pure Roslyn compiler oracle; no Andirit plugin reference; no CI-1A output exists or is read; frozen tables are independent oracle inputs checked against the manifest, never the reverse",
    baselineCommit,
    compiler = new
    {
        roslynCSharp = typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(),
        roslynCommon = typeof(Compilation).Assembly.GetName().Version?.ToString(),
        languageVersion = LanguageVersion.Latest.ToString(),
        trustedPlatformAssemblies = references.Length,
    },
    sourceInputs = sourceInputs.Values,
    projectContext = contexts,
    referenceEdges,
    replacementContexts = replacementCompilations,
    targetVocabulary = Frozen.TargetTable.Select(kv => new { signature = kv.Key, doc = kv.Value.DocId, kind = kv.Value.Kind.ToString(), eligibleLocal = kv.Value.EligibleLocal, declaringProject = kv.Value.DeclaringProject }).ToArray(),
    outcomeVocabulary = Frozen.OutcomeVocabulary.Select(kv => new
    {
        declared = kv.Key,
        @class = kv.Value.Class.ToString(),
        subtype = kv.Value.Subtype,
        dispatchClaim = kv.Value.Dispatch.ToString(),
        crossProjectClaim = kv.Value.CrossProject,
        afterReplacementClaim = kv.Value.AfterReplacement,
        conditionalClaim = kv.Value.Conditional.ToString(),
        lifecycleClaim = kv.Value.LifecycleAfterReplacement ? "after-replacement (not mechanically qualifiable; recorded as human-reviewed)" : "none",
    }).ToArray(),
    expectedCorpus = new { count = Frozen.ExpectedCorpusKeys.Length, keys = Frozen.ExpectedCorpusKeys.Select(k => Sem.RowKey(k.Case, k.Path, k.Span)).ToArray() },
    expectedOwners = Frozen.ExpectedOwners,
    authorizedRepositoryPaths = Frozen.AuthorizedRepositoryPaths,
    recognizedBasisForms = new[] { Frozen.BasisPlain, Frozen.BasisConditional },
    baseline = new
    {
        sites = baseline.Sites.Count,
        failures = failedRows,
        globalStructureErrors = baseline.GlobalErrors.Count,
        globalErrors = baseline.GlobalErrors.Select(g => new { g.Code, g.Detail }).ToArray(),
        classDistribution = new Dictionary<string, int>
        {
            ["confirmed"] = baseline.Sites.Count(a => a.DerivedClass == OutcomeClass.Confirmed),
            ["external"] = baseline.Sites.Count(a => a.DerivedClass == OutcomeClass.External),
            ["ambiguous"] = baseline.Sites.Count(a => a.DerivedClass == OutcomeClass.Ambiguous),
            ["unresolved"] = baseline.Sites.Count(a => a.DerivedClass == OutcomeClass.Unresolved),
            ["unsupported"] = baseline.Sites.Count(a => a.DerivedClass == OutcomeClass.Unsupported),
        },
        lifecycleQualifications = lifecycleRows.Select(a => new { a.Case, a.Path, code = "LIFECYCLE_NOT_QUALIFIED" }).ToArray(),
    },
    rowSemantics,
    siteEvidence = baseline.Sites.Select(a => (object)new
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
        boundDefinition = a.BoundDefinition,
        expectedDeclaringProject = a.ExpectedDeclaringProject,
        expectedDefinition = a.ExpectedDefinition,
        symbolEqualityAgrees = a.SymbolEqualityAgrees,
        derivedDispatch = a.DerivedDispatch.ToString(),
        derivedCrossProject = a.DerivedCrossProject,
        derivedAfterReplacement = a.DerivedAfterReplacement,
        conditional = a.Conditional,
        derivedClass = a.DerivedClass.ToString(),
        declaredClass = a.DeclaredClass.ToString(),
        derivedSubtype = a.DerivedSubtype,
        declaredSubtype = a.DeclaredSubtype,
        derivedOwner = a.DerivedOwner,
        expectedOwner = a.ExpectedOwner,
        repositoryPath = a.RepositoryPath,
        repositoryPathVerdict = a.RepositoryPathVerdict,
        basisVerdict = a.BasisVerdict,
        errors = a.Errors.Select(e => new { e.Code, e.Detail }).ToArray(),
        qualifications = a.Qualifications.Select(q => new { q.Code, q.Detail }).ToArray(),
    }).ToArray(),
    mutations,
    mutationTierSummaries = new
    {
        tierA = new { total = tierA.Count, rejected = tierA.Count(Rejected), q2NamedRegressionsRejected, allRejected = tierAAllRejected, positiveControlAccepted },
        tierB = new { total = tierB.Count, rejected = tierB.Count(Rejected), allRejected = tierBAllRejected, note = "complete Q3 adversarial set replayed; OUT-08 targets the delegate row (Q3's selector picked the method-group row)" },
        tierC = new { total = tierC.Count, rejected = tierC.Count(Rejected), allRejected = tierCAllRejected, positiveControlAccepted = tierCControlAccepted },
    },
    adversarialScenarios = new object[] { abScenario, collisionScenario, ownerScenario },
    collisionFixtureFiles,
    summary = new
    {
        baselineClean,
        universeComplete,
        ownerChecksEnforced,
        repositoryPathsVerified,
        basisRecognized,
        q2NamedRegressionsRejected,
        allRequiredMutationsRejected,
        positiveControlAccepted,
        tierCControlAccepted,
        noCrashes,
        noUnrelatedOnlyRejection = !mutations.Any(m => (bool)m.GetType().GetProperty("unrelatedValidationOnly")!.GetValue(m)!),
        abIdentityRequirementSatisfied = abRequirementSatisfied,
        collisionRequirementSatisfied = (bool)collisionScenario.requirementSatisfied,
        ownerChannelDistinguishes = (bool)ownerScenario.ownerChannelDistinguishes,
        lifecycleQualificationCount = lifecycleRows.Count,
        all32RowsRetainTypedSemantics = baselineClean && baseline.Sites.All(a => a.Qualifications.Count >= 1),
        candidateResultComparison = "unqualified — no CI-1A implementation exists; nothing here qualifies candidate-result comparison",
    },
};

File.WriteAllText(output, JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }) + "\n");
Console.WriteLine(JsonSerializer.Serialize(new
{
    Sites = baseline.Sites.Count,
    Failures = failedRows,
    GlobalStructureErrors = baseline.GlobalErrors.Count,
    Mutations = mutations.Count,
    Rejected = rejectedCount,
    AcceptedControls = mutations.Count(m => (bool)m.GetType().GetProperty("acceptedAsControl")!.GetValue(m)!),
    PositiveControlAccepted = positiveControlAccepted && tierCControlAccepted,
    Q2NamedRejected = q2NamedRegressionsRejected,
    Collision = (bool)collisionScenario.requirementSatisfied,
    ABIdentityRejected = abRequirementSatisfied,
    LifecycleQualifications = lifecycleRows.Count,
    Pass = overallPass,
}));
return overallPass ? 0 : baseline.GlobalErrors.Any(g => g.Code == "MANIFEST_STRUCTURE_INVALID") && baseline.Sites.Count == 0 ? 3 : 1;
