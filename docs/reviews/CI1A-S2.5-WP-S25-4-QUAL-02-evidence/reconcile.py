import json
import re
import sys
from collections import Counter
from pathlib import Path

root = Path(__file__).resolve().parent
history = Path(sys.argv[1])
prior = "../CI1A-S2.5-WP-S25-4-QUAL-01-evidence"
b3 = "probes/corrected"
add = "additional"
st = "state"
retry = "state-retry"
confirmation = "confirmation"

def matches(prefix, names):
    return [f"{prefix}/{name}.json" for name in names.split()]

def profiles(prefix, names, phases="publish reopen"):
    return [f"{prefix}/{name}-{phase}.json" for name in names.split() for phase in phases.split()]

def pointers(paths):
    for path in paths:
        assert (root / path).is_file(), path
    return " · ".join(f"[{Path(path).name}]({path})" for path in paths)

def read_table(path, columns):
    return [line.strip("|").strip().split(" | ") for line in path.read_text(encoding="utf-8-sig").splitlines()
            if line.startswith("| ") and not line.startswith(("| ---", "| ID |", "| Obligation |")) and
            len(line.strip("|").strip().split(" | ")) == columns]

def row_text(cells):
    return "| " + " | ".join(cells) + " |\n"

passed = "PASS"
qualified = "PASS WITH QUALIFICATION"
fail = "FAIL"
blocked = "BLOCKED"
fresh = {}
def add_rows(ids, status, note, paths):
    for row in ids.split():
        assert row not in fresh
        fresh[row] = (status, note, paths)

positive = profiles(b3, "b3")
mixed = profiles(b3, "b3-mixed")
b6 = matches(b3, "b6-publish b6-delete-a b6-reopen-a b6-delete-b b6-reopen-b")
b7 = matches(b3, "b7-conflict b7-reopen")
d3 = matches(b3, "d3-1 d3-2 d3-3")
d4 = matches(b3, "d4-schedule")
recovery = profiles(retry, "state-known") + profiles(st, "state-unknown")
supersession = profiles(retry, "state-supersession")
historic = matches(confirmation, "sq04-pre-rem sq04-graph-only-retry sq04-integrity sq04-graph-only-retry-hashes")
provenance = matches(add, "control-provenance-publish")
partial = profiles(retry, "coverage-boundary")
scope = profiles(add, "control-empty control-incomplete")
unsupported = profiles(add, "control-ctor control-property control-reference control-local")

add_rows("M1.1 M1.2", passed, "Exact raw bytes, separate source and nullable-options deltas, and ordered reference preimages verified. False supplied hash and missing declared project reference refused.", provenance)
add_rows("M1.3", passed, "The caller binds the declared dependency by its exact live compilation. Missing source evidence yields no trusted binding.", positive + matches(b3, "b3-missing"))
add_rows("M2.1", passed, "The independent encoder agreed on canonical usage4 and supported discriminators. The canonical fact constructor rejected a forged digest. Constructed storage contributor and payload controls retained the specified boundary.", matches(st, "identity-discriminators") + ["b3-identity-verification.json", "gates/oracle-r2.json"])
add_rows("M2.2", passed, "Separate defining context deltas changed expected and observed targets and occurrences. Positive fixture reconstruction, zero-live-definition boundary, and PE refusal agree.", provenance + positive + partial)
add_rows("M2.3", passed, "One fact retains both ordered contributors and presents distinct semantic-unit and plugin identities.", b6)
add_rows("M2.4", passed, "Identity preserved across a payload-only change. The native sink rejected the conflicting immutable payload and preserved the published state.", b7 + recovery[:2])
add_rows("M3.1", passed, "All 50 independently encoded targets route through the verified dependency. Metadata, missing live binding and distinct multiple-live ambiguity refuse without minting. The mixed native profile preserves caller fact inventory.", positive + mixed + partial + profiles(st, "multiple-definitions"))
add_rows("M3.2 M4.3 M4.4 M5.1 M6.1", passed, "Exact independently annotated source bytes, caller ownership, target, native declaration span and all 50 identities agree at publication and fresh-process reopen.", positive + ["b3-identity-verification.json"])
add_rows("M4.1", fail, "All 50 ordinary invocations and the positive local-function coverage control agree. Constructor and property controls had zero facts and zero UnsupportedCategories, contrary to the required coverage increment.", positive + unsupported)
add_rows("M4.2", fail, "The property-access and method-group profiles each produced no fact and UnsupportedCategories=0 where the matrix requires 1. Bound query and fresh-process reopen preserve the omission.", profiles(add, "control-property control-reference"))
add_rows("M5.2", passed, "The exact verified captured PE maps to UnsupportedExternalEvidence. Counters 1 external and 0 unresolved with zero external occurrences survive native persistence and reopen. Baseline inverse counts reproduced.", positive + ["probes/baseline/b3-publish.json", "probes/baseline/b3-reopen.json", "b3-identity-verification.json"])
add_rows("M5.3", passed, "Two verified live references carry the same assembly identity. The resolver returns Ambiguous. Native coverage is one ambiguous, zero external, zero unresolved, with zero facts after reopen.", profiles(st, "multiple-definitions"))
add_rows("M5.4", blocked, "Exact missing defining classification was observed for an unrelated source symbol with no caller reference. The real mixed fixture persists one unresolved diagnostic. The native partial fixture persists a constructed unresolved count. No continuous authority-bound production receipt connects the exact MissingDefiningCompilation outcome to native persisted unresolved evidence.", partial + mixed + matches(b3, "b3-missing"))
add_rows("M5.5", fail, "A binding object creation with no other unsupported site created zero facts for the constructor, but UnsupportedCategories stayed 0 through extraction and native reopen. The accepted matrix requires 1.", profiles(add, "control-ctor"))
add_rows("M5.6", passed, "The disabled DEBUG branch yields exactly one ConditionalInactive and no confirmed fact for that branch. Counters survive native reopen.", profiles(add, "control-inactive"))
add_rows("M5.7", fail, "Both a missing solution project and an explicitly declared unit with missing source deny live authority and return AnalysisUnavailable through publish/reopen. The approved row requires IncompleteAnalysis. The separately staged partial storage control returns IncompleteAnalysis. Both controls refuse complete-empty availability.", scope + partial + profiles("declared", "control-declared") + profiles("baseline-declared", "control-declared"))
add_rows("M5.8", passed, "An authority-bound complete zero reports Available and complete with zero facts. A matching fixture with a declared absent project refuses availability and completeness throughout reopen.", scope)
add_rows("M6.2", passed, "Five old sites retired and five newly derived sites appeared, with 45 retained sites. The unrelated real project's exact ID remains available during replacement and reopen.", profiles(st, "state-lifecycle", "publish replace reopen"))
add_rows("M6.3", passed, "Real caller deletion removes its native unit and facts. The unrelated project's independently derived fact remains persisted through fresh-process reopen.", profiles(st, "state-lifecycle", "delete deletedreopen"))
add_rows("M6.4 M7.3", qualified, "Contributor union and ordered deletions survived reopen. The accepted F-01 constructed authority boundary carries forward.", b6 + positive)
add_rows("M6.5", qualified, "Explicit rejection and exact previously accepted state preservation confirmed for constructed contributor conflict and for the constructed cross-unit payload variation on a real fact following known recovery.", b7 + recovery[:2])
add_rows("M7.1", qualified, "Production extraction/publication agrees with exact native results. Managed fault checks preserve or quarantine state according to the known or unknown outcome.", positive + recovery)
add_rows("M7.2 M7.5 M9.7", passed, "Authority-bound facts remain tied to publication and retain their exact native IDs and coverage across a new process.", positive)
add_rows("M7.4", passed, "Fresh distinct stale-publication and persisted page-binding refusals preserve the previous behavior.", positive + matches(b3, "d5-page"))
add_rows("M8.1 M8.2 M8.3 M8.4", passed, "Three real-project repeats agree on graph noninterference, native mirror equivalence and graph-only isolation within bounded D3 scope.", d3)
add_rows("M9.1", passed, "Authority-bound current publication and old/new serialization agree, with the stale publication refused.", d4 + positive)
add_rows("M9.2", passed, "Unbound, historical graph-only, and isolated namespace refusals remain distinct.", positive + historic + matches(b3, "d5-namespace"))
add_rows("M9.3", passed, "Bounded target-only Examined reproduced by the paged real fixture and the frozen historical P2 harness replay.", positive + matches(confirmation, "p2-correct-order-integrate p2-correct-order-reopen"))
add_rows("M9.4", passed, "Known pre-commit failure restored old graph and occurrence state before reopen. A separate storage owner carrying the same ID and altered immutable payload was rejected with occurrence.payload.conflict. Unknown after-commit outcome quarantined reads/writes and a distinct process exposed the exact new committed generation.", recovery)
add_rows("M9.6", passed, "A staged real writer based on the former accepted generation was explicitly rejected following a newer successful publication. Exact new facts and attribution survive reopen.", supersession)
add_rows("M9.8", qualified, "Constructed union/conflict and real caller lifecycle passed in the accepted F-01 scope.", b6 + b7 + profiles(st, "state-lifecycle", "replace delete deletedreopen"))
add_rows("M9.9", passed, "Independent full-span ordering, exhaustive bounded traversal and malformed, wrong-target, and superseded-publication continuation refusal agree.", positive + supersession)
add_rows("M9.10", qualified, "Both sides of the explicit serialized writer barrier produced the expected complete publication. The bounded managed scope carries.", d4)
add_rows("M10.1 M10.2", passed, "An authentic graph-only copy retained readable graph data and reported unavailable occurrence analysis. Fresh graph-only isolation was also exercised.", historic + d3[:1])
add_rows("M10.3 M15.1", passed, "Pre-REM refusal and isolated schema and namespace mutations each returned the prescribed schema exception. The namespace control retained its schema version.", historic[:1] + matches(b3, "d5-schema d5-namespace"))
add_rows("M10.4", passed, "Page-publication mismatch refused with CodeOccurrenceIndexException.", matches(b3, "d5-page"))
add_rows("M10.5", passed, "Explicit stale handles refused with zero occurrences.", d4 + supersession)
add_rows("M10.6", qualified, "The protected source delta touches only C# classification. The prior source audit and amended SQ-04 scope remain applicable. Fresh authentic-copy behavior confirmed.", historic)
add_rows("M11.1 M11.2", passed, "Three cold-process, independently initialized real-project samples preserve individual publication and native reopen costs and graph snapshots. No additional latency threshold applied.", d3)
add_rows("M11.3", qualified, "Paged target-local timing and Examined evidence reproduced. Measurements apply to the recorded workload and host.", positive[1:] + d3)
add_rows("M11.4 M14.1", blocked, "Prior genuine Ubuntu x64 52/0/0 remains accepted historically. Corrected-revision producer-to-native Linux regression and focused classification publication/reopen require a new authorized pinned run.", ["integrity-preflight.jsonl", "production-delta.txt"])
add_rows("M14.2", "REUSED QUALIFIED", "Historical platform locking, WAL and mapping documentation remains applicable to its accepted scope. Corrected execution is tracked in M11.4 and M14.1.", ["integrity-preflight.jsonl", "production-delta.txt"])
add_rows("M13.1 M13.3", qualified, "Two authentic fixture sets retained their pinned master bytes. The pre-REM store refused and the prior graph-only generation retained graph readability without occurrence availability. SQ-04 exclusions carry.", historic)
add_rows("M13.2", passed, "All three master files were hashed before and after execution. Only disposable verified copies were opened.", historic)

matrix = read_table(history / "matrix.md", 12)
assert len(matrix) == len({row[0] for row in matrix}) == 64
current = Counter()
mandatory = Counter()
historical = Counter(row[9] for row in matrix)
assert historical == {"PASS": 27, "PASS WITH QUALIFICATION": 10, "FAIL": 2, "BLOCKED": 18,
                      "REUSED QUALIFIED": 3, "DEFERRED": 4}
with (root / "matrix.md").open("w", encoding="utf-8", newline="\n") as out:
    out.write("# WP-S25-4 reconciled qualification matrix\n\n")
    out.write("The approved row requirements and fixtures are reproduced from the preserved QUAL-01 matrix. "
              "Fresh receipts were collected with Windows x64 native LatticeDb and the serialized single-writer model. "
              "The two blocked Linux execution rows retain their earlier genuine Ubuntu receipts within the accepted revision-bound scope.\n\n")
    out.write(row_text(["ID", "Authority", "Fresh requirement", "Fixture", "Requirement", "Independent expected result and required case",
                        "QUAL-01", "Current", "Basis and observed result", "Receipts", "Qualification"]))
    out.write(row_text(["---"] * 11))
    for ident, authority, required, fixture, requirement, oracle, _, _, _, previous, qualification, _ in matrix:
        if previous == "DEFERRED":
            status, note, evidence = "DEFERRED", "Historical Stage 6 deferral retained.", "[Entry scope](../ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01.md)"
        else:
            status, note, paths = fresh.pop(ident)
            evidence = pointers(paths)
        current[status] += 1
        if previous == "BLOCKED":
            mandatory[status] += 1
        out.write(row_text([ident, authority, required, fixture, requirement, oracle, previous, status, note, evidence, qualification]))
assert not fresh and sum(mandatory.values()) == 18

# Evidence classifications are qualification bookkeeping. Owner decisions remain effective.
obligation_changes = {
    "F03": ("BLOCKED", "M5.2, M5.7, M5.8", "External classification corrected. The incomplete-analysis status gap remains.", "Resolve the declared-unit status discrepancy."),
    "F04": ("BLOCKED", "M5.7, M5.8", "Complete-empty safety passed. Exact IncompleteAnalysis for the missing declared unit remains unmet.", "Disposition the status mismatch."),
    "F05": ("QUALIFIED", "M9.4", "Known fault preserves accepted state and explicit conflict refusal. Unknown outcome reconciles in a fresh process.", "Carry managed fault limits."),
    "F06": ("BLOCKED", "matrix.md", "Mandatory category, incomplete-status, exact missing-defining persistence, and Linux evidence remain.", "Commission the remaining correction and independent qualification."),
    "F16": ("BLOCKED", "matrix.md", "All 64 rows accounted for. Corrected-revision evidence blockers remain.", "Resolve the recorded matrix blockers before acceptance."),
    "NEW-01": ("QUALIFIED", "M9.4, state-retry/state-known-publish.json", "Fresh real known-fault recovery and structured post-recovery conflict refusal agree.", "Carry managed fault and Stage 6 limits."),
    "NEW-04": ("BLOCKED", "M5.2, M5.4, M5.7, M5.8", "Corrected external count and complete-empty guard pass. Exact missing-defining persistence and missing-unit status remain open.", "Disposition the precise remaining coverage obligations."),
    "SQ-02": ("QUALIFIED", "M9.4, state/state-unknown-reopen.json", "Fresh known-fault recovery, unknown quarantine and new-process reconciliation agree.", "Carry managed fault scope and F-02."),
    "SQ-04": ("QUALIFIED", "M13.1–M13.3, confirmation/sq04-integrity.json", "Amended historical scope and disposable-copy replay preserved.", "Retain the eight exclusions and reopening obligation."),
    "SQ-05": ("QUALIFIED", "M11.4/M14.1/M14.2, integrity-preflight.jsonl", "Historical qualified run retains its pinned scope. R1 needs new corrected-revision Linux evidence.", "Authorize genuine Ubuntu delta under the successor commission."),
    "SQ-07": ("QUALIFIED", "M11.1–M11.3, probes/corrected/d3-1.json through d3-3.json", "Three publication/reopen samples and separate target-page samples recorded.", "Retain workload and host scope and existing optimization boundary."),
    "IF-03": ("BLOCKED", "M5.2–M5.8", "Four tested refusal categories or coverage routes agree. Unsupported and incomplete status requirements remain unmet, and exact missing-defining persistence remains blocked.", "Commission targeted qualification once feedback and provenance are resolved."),
    "IF-04": ("QUALIFIED", "M9.9, state-retry/state-supersession-publish.json", "Wrong-target and stale-publication tokens explicitly refuse. Cross-target public feature scope stays deferred.", "Preserve the demonstrated target-local continuation and scope."),
    "MD-01": ("DISCHARGED", "manifest.json, verify-integrity.py", "Historical package seals verified before execution, and QUAL-02 sealed additively.", "Carry the evidence commit and digest into the owner record."),
    "S-01": ("QUALIFIED", "b3-identity-verification.json, test-inventory.json, verified additive erratum", "R1 refuses PE-only bindings before mint. Fresh component and composition comparison agrees at the accepted boundary.", "Retain accepted Q-P2-01 through Q-P2-06."),
    "D4.1–D4.10": ("QUALIFIED", "M9.1–M9.10, state and state-retry receipts", "Applicable serialized publication, known recovery, quarantine, reconciliation and supersession passed.", "Carry D4.5 and the full matrix as Stage 6 obligations."),
    "I-01": ("BLOCKED", "matrix.md", "Production category/status findings and exact missing-defining evidence gap remain, along with corrected-revision Linux evidence.", "Commission bounded correction and independent final integration."),
    "B3": ("QUALIFIED", "M1.3/M2.2/M3.1/M3.2/M5.2, b3-identity-verification.json", "Original F-S254-01 independently requalified, 50 identities preserved.", "Record independent qualification and separately disposition the owner-governed finding."),
    "B6": ("QUALIFIED", "M2.3/M6.4/M9.8", "Constructed contributor union and real distinct-owner lifecycle freshly confirmed.", "Preserve F-01 and the contributor fixture provenance."),
    "B7": ("QUALIFIED", "M2.4/M6.5/M9.8/M9.4", "Prior accepted state survives the constructed conflict and the combined real recovery/payload variation.", "Carry the constructed cross-unit conflict limit."),
    "F-G2-01": ("OPEN-S2.5", "confirmation/windows-coverage.log and coverage/coverage.cobertura.xml", "Corrected Windows export measured 296/450 branches = 65.77% at the existing 70% threshold. Historical gated Ubuntu failure remains.", "Authorize bounded test-only coverage remediation and rerun the existing gate."),
    "Required evidence, fixture, runner or independence gap": ("BLOCKED", "matrix.md and integrity-preflight.jsonl", "Session separation and manifest gate passed. Mandatory functional evidence and corrected genuine Ubuntu execution remain incomplete.", "Provide the separately authorized Linux route and complete the unresolved functional review."),
}
obligations = read_table(history / "obligations.md", 8)
assert len(obligations) == len({row[0] for row in obligations}) == 78
counts = Counter()
with (root / "obligations.md").open("w", encoding="utf-8", newline="\n") as out:
    out.write("# WP-S25-4 obligation reconciliation\n\n")
    out.write("The 78 entries follow the [approved obligation inventory](../../planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md) and "
              "the [consolidated register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md). "
              "The historical evidence locators refer to [the QUAL-01 reconciliation](" + prior + "/obligations.md). "
              "The separate findings and Linux delta are tracked below the reconciled entries. "
              "The evidence disposition carries no owner closure authority.\n\n")
    out.write(row_text(["Obligation", "Governing source", "Entry disposition", "QUAL-01", "QUAL-01 evidence", "QUAL-02",
                        "Current evidence", "Effect of R1 and QUAL-02", "Inherited limitation", "Owner action", "Downstream owner"]))
    out.write(row_text(["---"] * 11))
    for ident, authority, entry, old, evidence, limitation, action, downstream in obligations:
        current_value, current_evidence, impact, owner_action = obligation_changes.pop(ident, (
            old, "Prior sealed scope and production-delta.txt",
            "The R1 resolver delta leaves the inherited governing decision and its demonstrated scope applicable.", action))
        counts[current_value] += 1
        out.write(row_text([ident, authority, entry, old, evidence, current_value, current_evidence, impact,
                            limitation, owner_action, downstream]))
    out.write("\n- **F-S254-01** independently requalified on Windows under M3.1 and M5.2. Formal finding disposition remains with the owner.\n")
    out.write("- **F-S254-02** unsupported counters fail M4.1/M4.2/M5.5. **F-S254-03** the declared-unit publication status fails M5.7. M5.4 retains a separate continuity evidence gap.\n")
    out.write("- **Corrected-revision genuine Ubuntu delta** remains blocked pending an authorized pinned Linux run under M11.4/M14.1 and the contemporary SQ-05 scope.\n")
    out.write("- **F-G2-01** remains open. Stage 6 owns D4.5, full D3, full S-02, F-02 durability and full platform certification, as above.\n")
assert not obligation_changes
summary = dict(historical=historical, current=current, previouslyOutstanding=mandatory, obligations=counts)
(root / "reconciliation-counts.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
print(json.dumps(summary))


