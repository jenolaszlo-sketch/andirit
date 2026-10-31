// ============================================================================
// CI-1-SCOPE-R3 oracle — typed semantic assertion contracts and frozen tables.
//
// Planning-only. Pure Roslyn. No Andirit product reference, no CI-1A output.
// These tables are INDEPENDENT ORACLE INPUTS, not manifest data: they are
// derived from the frozen fixture corpus and the CI-1 planning contracts, and
// the manifest is checked against them (never the reverse).
//
// Assertion model version: ci1-scope-r3/semantic-assertions/1
// ============================================================================

using Microsoft.CodeAnalysis.CSharp;

namespace CI1ScopeOracleR3;

public enum OutcomeClass { Confirmed, External, Ambiguous, Unresolved, Unsupported, NotEvaluated, Unrecognized }

public enum OccurrenceCategory { OrdinaryInvocation, Constructor, MethodGroup, TypeOf, NameOf, Unknown }

public enum TargetKind { Method, Constructor, TypeToken, Ambiguous, Unresolved, None }

// The single static-dispatch characteristic of a uniquely bound invocation.
// Mutually exclusive by definition; precedence: InterfaceMember, then
// BaseReceiver, then DelegateInvoke, then VirtualMember, then Plain.
public enum DispatchFacet { NotApplicable, Plain, InterfaceMember, VirtualMember, BaseReceiver, DelegateInvoke }

public enum ConditionalClaim { None, InactiveDefaultConfirmedWithCI1Extra }

// Claimed assertions carried by one recognized declared-outcome label.
// A display label is a serialized description of these facts, never the
// authority from which they are derived.
public sealed record ClaimedOutcome(
    OutcomeClass Class,
    string? Subtype,
    DispatchFacet Dispatch,
    bool CrossProject,
    bool AfterReplacement,
    ConditionalClaim Conditional,
    bool LifecycleAfterReplacement)
{
    public bool Recognized => Class != OutcomeClass.Unrecognized;
}

public sealed record TargetExpectation(string DocId, TargetKind Kind, bool EligibleLocal, string DeclaringProject);

// Exact-definition descriptor: the identity facts of one definition.
// Equality of descriptors = same defining compilation + same assembly +
// same documentation ID + same declaration location.
public sealed record DefDescriptor(
    string Compilation,
    string AssemblyIdentity,
    string DocId,
    string? TreePath,
    int? DeclStart,
    int? DeclEnd);

public sealed record ConditionalActivity(
    bool ActiveInDefault,
    bool ActiveWithCI1Extra,
    bool ActiveInAudited,
    bool ConditionalOnly)
{
    public string Requires => ConditionalOnly ? "CI1_EXTRA" : "none";
}

public sealed record SiteError(string Code, string Detail);

// Structured qualification: an explicit, recorded limitation. Never silently
// dropped, never counted as a rejection.
public sealed record Qualification(string Code, string Detail);

public sealed record GlobalError(string Code, string Detail);

public sealed record SiteAuditR3(
    string Case,
    string Path,
    int[] Span,
    string Lexeme,
    string Compilation,
    string DeclaredTarget,
    string DeclaredOutcome,
    OccurrenceCategory Category,
    string? BoundTargetDoc,
    string? BoundContainingAssembly,
    bool BoundEligibleLocal,
    string[] BoundSourceDeclarations,
    string?[] CandidateDocs,
    string CandidateReason,
    DefDescriptor? BoundDefinition,
    DefDescriptor? ExpectedDefinition,
    string ExpectedDeclaringProject,
    bool SymbolEqualityAgrees,
    DispatchFacet DerivedDispatch,
    bool DerivedCrossProject,
    bool DerivedAfterReplacement,
    ConditionalActivity Conditional,
    string? DerivedOwner,
    string? ExpectedOwner,
    OutcomeClass DerivedClass,
    OutcomeClass DeclaredClass,
    string? DerivedSubtype,
    string? DeclaredSubtype,
    string? RepositoryPath,
    string RepositoryPathVerdict,
    string BasisVerdict,
    List<SiteError> Errors,
    List<Qualification> Qualifications);

public sealed record AuditResult(IReadOnlyList<SiteAuditR3> Sites, IReadOnlyList<GlobalError> GlobalErrors)
{
    public bool AnyFailure => Sites.Any(s => s.Errors.Count > 0) || GlobalErrors.Count > 0;
}

public sealed record OracleEnvironment(
    IReadOnlyDictionary<string, CSharpCompilation> ByName,
    IReadOnlyDictionary<string, string[]> ReferenceEdges,
    IReadOnlyCollection<string> ReplacementCompilations,
    Func<string, (string Path, string Text)> ReadFixture,
    string FixtureRoot,
    List<object> Contexts)
{
    public IEnumerable<CSharpCompilation> SourceCompilations => ByName.Values;
}

// ---------------------------------------------------------------------------
// Frozen tables. All content below is independent oracle input derived from
// the frozen CI-1A fixture corpus at commit 5c89ac0b3d0e87c0112880cf985826ded42b21b2.
// ---------------------------------------------------------------------------

public static class Frozen
{
    // Exact target-identity table. DeclaringProject:
    //   "audited"  — the definition must come from the site's own audited compilation
    //   "lib"      — the definition must come from the named referenced project compilation
    //   "metadata" — the target is a metadata-only external definition; NO source-built
    //                compilation may declare a symbol with this documentation ID
    //   "none"     — no target expected at all (unresolved / ambiguous rows)
    public static readonly Dictionary<string, TargetExpectation> TargetTable = new(StringComparer.Ordinal)
    {
        ["Logger.Write(string)"] = new("M:Logger.Write(System.String)", TargetKind.Method, true, "audited"),
        ["CI1Corpus.Overloads.Hit(int)"] = new("M:CI1Corpus.Overloads.Hit(System.Int32)", TargetKind.Method, true, "audited"),
        ["CI1Corpus.Overloads.Hit(string)"] = new("M:CI1Corpus.Overloads.Hit(System.String)", TargetKind.Method, true, "audited"),
        ["CI1Corpus.GenericExtensions.Echo<T>(T)"] = new("M:CI1Corpus.GenericExtensions.Echo``1(``0)", TargetKind.Method, true, "audited"),
        ["CI1Corpus.GenericExtensions.Mark<T>(this T)"] = new("M:CI1Corpus.GenericExtensions.Mark``1(``0)", TargetKind.Method, true, "audited"),
        ["CI1Corpus.Constructors(int)"] = new("M:CI1Corpus.Constructors.#ctor(System.Int32)", TargetKind.Constructor, true, "audited"),
        ["CI1Corpus.ConstructorBase(int)"] = new("M:CI1Corpus.ConstructorBase.#ctor(System.Int32)", TargetKind.Constructor, true, "audited"),
        ["CI1Corpus.Constructors()"] = new("M:CI1Corpus.Constructors.#ctor", TargetKind.Constructor, true, "audited"),
        ["CI1Corpus.IWorker.Work()"] = new("M:CI1Corpus.IWorker.Work", TargetKind.Method, true, "audited"),
        ["CI1Corpus.Worker.Work()"] = new("M:CI1Corpus.Worker.Work", TargetKind.Method, true, "audited"),
        ["CI1Corpus.Target.Work()"] = new("M:CI1Corpus.Target.Work", TargetKind.Method, true, "audited"),
        ["System.Action.Invoke()"] = new("M:System.Action.Invoke", TargetKind.Method, false, "metadata"),
        ["CI1Corpus.Target"] = new("T:CI1Corpus.Target", TargetKind.TypeToken, true, "audited"),
        ["System.Console.WriteLine(string)"] = new("M:System.Console.WriteLine(System.String)", TargetKind.Method, false, "metadata"),
        ["CI1Corpus.Partial.Work()"] = new("M:CI1Corpus.Partial.Work", TargetKind.Method, true, "audited"),
        ["CI1Cross.Library.Work() in Lib/Lib.csproj"] = new("M:CI1Cross.Library.Work", TargetKind.Method, true, "lib"),
        ["unknown"] = new("", TargetKind.None, false, "none"),
        ["unknown-string-or-Exception-overload"] = new("", TargetKind.Ambiguous, false, "none"),
    };

    // The exact 15-string declared-outcome vocabulary, now carrying the typed
    // assertions each label claims. No new labels are introduced.
    public static readonly Dictionary<string, ClaimedOutcome> OutcomeVocabulary = new(StringComparer.Ordinal)
    {
        ["confirmed"] = new(OutcomeClass.Confirmed, null, DispatchFacet.Plain, false, false, ConditionalClaim.None, false),
        ["confirmed-interface-static-target"] = new(OutcomeClass.Confirmed, null, DispatchFacet.InterfaceMember, false, false, ConditionalClaim.None, false),
        ["confirmed-virtual-static-target"] = new(OutcomeClass.Confirmed, null, DispatchFacet.VirtualMember, false, false, ConditionalClaim.None, false),
        ["confirmed-base-static-target"] = new(OutcomeClass.Confirmed, null, DispatchFacet.BaseReceiver, false, false, ConditionalClaim.None, false),
        ["confirmed-cross-project"] = new(OutcomeClass.Confirmed, null, DispatchFacet.Plain, true, false, ConditionalClaim.None, false),
        ["confirmed-after-replacement"] = new(OutcomeClass.Confirmed, null, DispatchFacet.Plain, false, true, ConditionalClaim.None, true),
        ["inactive-default; confirmed only with CI1_EXTRA"] = new(OutcomeClass.Confirmed, null, DispatchFacet.Plain, false, false, ConditionalClaim.InactiveDefaultConfirmedWithCI1Extra, false),
        ["external-no-confirmed-local-target"] = new(OutcomeClass.External, null, DispatchFacet.Plain, false, false, ConditionalClaim.None, false),
        ["external-delegate-invoke-not-Target.Work"] = new(OutcomeClass.External, null, DispatchFacet.DelegateInvoke, false, false, ConditionalClaim.None, false),
        ["ambiguous-no-confirmation"] = new(OutcomeClass.Ambiguous, null, DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
        ["unresolved-no-confirmation"] = new(OutcomeClass.Unresolved, null, DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
        ["deferred-constructor"] = new(OutcomeClass.Unsupported, "constructor", DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
        ["deferred-method-group"] = new(OutcomeClass.Unsupported, "method-group", DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
        ["deferred-type-reference"] = new(OutcomeClass.Unsupported, "type-reference", DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
        ["deferred-nameof-reference"] = new(OutcomeClass.Unsupported, "nameof-reference", DispatchFacet.NotApplicable, false, false, ConditionalClaim.None, false),
    };

    public static readonly string[] ConfirmedFamily =
    {
        "confirmed",
        "confirmed-interface-static-target",
        "confirmed-virtual-static-target",
        "confirmed-base-static-target",
        "confirmed-cross-project",
        "confirmed-after-replacement",
        "inactive-default; confirmed only with CI1_EXTRA",
    };

    // Expected selection universe for the frozen CI-1A corpus: the 32 reviewed
    // occurrences, keyed by (Case, FixturePath, one-based UTF-16 end-exclusive span)
    // with the declared lexeme. Identity of sites — not semantics.
    public static readonly (string Case, string Path, int[] Span, string Lexeme)[] ExpectedCorpusKeys =
    {
        ("C01", "Repeated.cs", new[] { 5, 9, 5, 30 }, "Logger.Write(\"first\")"),
        ("C01", "Repeated.cs", new[] { 6, 9, 6, 31 }, "Logger.Write(\"second\")"),
        ("C01", "Repeated.cs", new[] { 7, 9, 7, 30 }, "Logger.Write(\"third\")"),
        ("C02", "acceptance/Cases.cs", new[] { 6, 35, 6, 41 }, "Hit(1)"),
        ("C02", "acceptance/Cases.cs", new[] { 6, 43, 6, 51 }, "Hit(\"x\")"),
        ("C04", "acceptance/Cases.cs", new[] { 12, 35, 12, 47 }, "Echo<int>(1)"),
        ("C04", "acceptance/Cases.cs", new[] { 12, 49, 12, 67 }, "\"x\".Mark<string>()"),
        ("C05", "acceptance/Cases.cs", new[] { 20, 29, 20, 36 }, "this(1)"),
        ("C05", "acceptance/Cases.cs", new[] { 21, 38, 21, 49 }, "base(value)"),
        ("C05", "acceptance/Cases.cs", new[] { 22, 52, 22, 57 }, "new()"),
        ("C05", "acceptance/Cases.cs", new[] { 22, 67, 22, 86 }, "new Constructors(1)"),
        ("C06", "acceptance/Cases.cs", new[] { 36, 55, 36, 67 }, "value.Work()"),
        ("C07", "acceptance/Cases.cs", new[] { 37, 52, 37, 64 }, "value.Work()"),
        ("C07", "acceptance/Cases.cs", new[] { 32, 30, 32, 41 }, "base.Work()"),
        ("C08", "acceptance/Cases.cs", new[] { 45, 58, 45, 69 }, "Target.Work"),
        ("C08", "acceptance/Cases.cs", new[] { 45, 71, 45, 79 }, "action()"),
        ("C09", "acceptance/Cases.cs", new[] { 49, 43, 49, 57 }, "typeof(Target)"),
        ("C09", "acceptance/Cases.cs", new[] { 49, 67, 49, 86 }, "nameof(Target.Work)"),
        ("C09", "acceptance/Cases.cs", new[] { 49, 88, 49, 101 }, "Target.Work()"),
        ("C10", "acceptance/Cases.cs", new[] { 53, 35, 53, 70 }, "System.Console.WriteLine(\"outside\")"),
        ("C11", "acceptance/Cases.cs", new[] { 59, 35, 59, 45 }, "Pick(null)"),
        ("C11", "acceptance/Cases.cs", new[] { 59, 47, 59, 56 }, "Missing()"),
        ("C12", "acceptance/Partial.Two.cs", new[] { 4, 28, 4, 34 }, "Work()"),
        ("C19", "acceptance/Duplicate.One.cs", new[] { 4, 35, 4, 48 }, "Target.Work()"),
        ("C19", "acceptance/Duplicate.Two.cs", new[] { 4, 35, 4, 48 }, "Target.Work()"),
        ("C03", "acceptance/CrossProject/App/App.cs", new[] { 4, 35, 4, 49 }, "Library.Work()"),
        ("C14", "acceptance/Repeated.AfterDelete.cs.txt", new[] { 5, 9, 5, 30 }, "Logger.Write(\"first\")"),
        ("C14", "acceptance/Repeated.AfterDelete.cs.txt", new[] { 6, 9, 6, 30 }, "Logger.Write(\"third\")"),
        ("C15", "acceptance/Repeated.AfterShift.cs.txt", new[] { 6, 9, 6, 30 }, "Logger.Write(\"first\")"),
        ("C15", "acceptance/Repeated.AfterShift.cs.txt", new[] { 7, 9, 7, 31 }, "Logger.Write(\"second\")"),
        ("C15", "acceptance/Repeated.AfterShift.cs.txt", new[] { 8, 9, 8, 30 }, "Logger.Write(\"third\")"),
        ("C20", "acceptance/Cases.cs", new[] { 66, 9, 66, 22 }, "Target.Work()"),
    };

    // Expected lexical (syntactic) owner per occurrence, keyed
    // "Case|FixturePath|startLine,startCol". The owner is the nearest
    // enclosing method-like declaration; this is lexical containment, not
    // runtime caller attribution.
    public static readonly Dictionary<string, string> ExpectedOwners = new(StringComparer.Ordinal)
    {
        ["C01|Repeated.cs|5,9"] = "M:Worker.Execute",
        ["C01|Repeated.cs|6,9"] = "M:Worker.Execute",
        ["C01|Repeated.cs|7,9"] = "M:Worker.Execute",
        ["C02|acceptance/Cases.cs|6,35"] = "M:CI1Corpus.Overloads.Caller",
        ["C02|acceptance/Cases.cs|6,43"] = "M:CI1Corpus.Overloads.Caller",
        ["C04|acceptance/Cases.cs|12,35"] = "M:CI1Corpus.GenericExtensions.Caller",
        ["C04|acceptance/Cases.cs|12,49"] = "M:CI1Corpus.GenericExtensions.Caller",
        ["C05|acceptance/Cases.cs|20,29"] = "M:CI1Corpus.Constructors.#ctor",
        ["C05|acceptance/Cases.cs|21,38"] = "M:CI1Corpus.Constructors.#ctor(System.Int32)",
        ["C05|acceptance/Cases.cs|22,52"] = "M:CI1Corpus.Constructors.Caller",
        ["C05|acceptance/Cases.cs|22,67"] = "M:CI1Corpus.Constructors.Caller",
        ["C06|acceptance/Cases.cs|36,55"] = "M:CI1Corpus.Dispatch.InterfaceCall(CI1Corpus.IWorker)",
        ["C07|acceptance/Cases.cs|37,52"] = "M:CI1Corpus.Dispatch.VirtualCall(CI1Corpus.Worker)",
        ["C07|acceptance/Cases.cs|32,30"] = "M:CI1Corpus.OverrideWorker.BaseCall",
        ["C08|acceptance/Cases.cs|45,58"] = "M:CI1Corpus.MethodGroups.Caller",
        ["C08|acceptance/Cases.cs|45,71"] = "M:CI1Corpus.MethodGroups.Caller",
        ["C09|acceptance/Cases.cs|49,43"] = "M:CI1Corpus.References.Caller",
        ["C09|acceptance/Cases.cs|49,67"] = "M:CI1Corpus.References.Caller",
        ["C09|acceptance/Cases.cs|49,88"] = "M:CI1Corpus.References.Caller",
        ["C10|acceptance/Cases.cs|53,35"] = "M:CI1Corpus.External.Caller",
        ["C11|acceptance/Cases.cs|59,35"] = "M:CI1Corpus.Ambiguous.Caller",
        ["C11|acceptance/Cases.cs|59,47"] = "M:CI1Corpus.Ambiguous.Caller",
        ["C12|acceptance/Partial.Two.cs|4,28"] = "M:CI1Corpus.Partial.Caller",
        ["C19|acceptance/Duplicate.One.cs|4,35"] = "M:CI1Corpus.FirstFile.Caller",
        ["C19|acceptance/Duplicate.Two.cs|4,35"] = "M:CI1Corpus.SecondFile.Caller",
        ["C03|acceptance/CrossProject/App/App.cs|4,35"] = "M:CI1Cross.Application.Caller",
        ["C14|acceptance/Repeated.AfterDelete.cs.txt|5,9"] = "M:Worker.Execute",
        ["C14|acceptance/Repeated.AfterDelete.cs.txt|6,9"] = "M:Worker.Execute",
        ["C15|acceptance/Repeated.AfterShift.cs.txt|6,9"] = "M:Worker.Execute",
        ["C15|acceptance/Repeated.AfterShift.cs.txt|7,9"] = "M:Worker.Execute",
        ["C15|acceptance/Repeated.AfterShift.cs.txt|8,9"] = "M:Worker.Execute",
        ["C20|acceptance/Cases.cs|66,9"] = "M:CI1Corpus.Conditional.Caller",
    };

    // Authorized fixture → repository-path mapping for the CI-1A packet.
    // Replacement snapshots (AfterDelete/AfterShift) map to the same repository
    // file they replace.
    public static readonly Dictionary<string, string> AuthorizedRepositoryPaths = new(StringComparer.Ordinal)
    {
        ["Repeated.cs"] = "src/Repeated.cs",
        ["acceptance/Cases.cs"] = "src/Cases.cs",
        ["acceptance/Partial.Two.cs"] = "src/Partial.Two.cs",
        ["acceptance/Duplicate.One.cs"] = "src/Duplicate.One.cs",
        ["acceptance/Duplicate.Two.cs"] = "src/Duplicate.Two.cs",
        ["acceptance/CrossProject/App/App.cs"] = "App/App.cs",
        ["acceptance/Repeated.AfterDelete.cs.txt"] = "src/Repeated.cs",
        ["acceptance/Repeated.AfterShift.cs.txt"] = "src/Repeated.cs",
    };

    // The only basis provenance statements recognized in the frozen corpus.
    // Claims inside them are classified: "coordinates computed from text" is
    // mechanically corroborated by the span check; "hand-selected" and
    // "no extractor output" are human-reviewed process assertions and are NOT
    // mechanically qualified by this oracle.
    public const string BasisPlain =
        "Hand-selected source expression/signature; coordinates computed from text, no extractor output";
    public const string BasisConditional =
        "Hand-selected conditional source expression, no extractor output";
}
