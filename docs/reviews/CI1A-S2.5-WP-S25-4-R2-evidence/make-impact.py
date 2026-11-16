from collections import Counter
from pathlib import Path
import sys

old = Path(sys.argv[1])
out = Path(__file__).resolve().parent

def rows(path, columns):
    result = []
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        if line.startswith("| ") and not line.startswith(("| ---", "| ID |", "| Obligation |")):
            parts = line.strip("|").strip().split(" | ")
            assert len(parts) == columns, line
            result.append(parts)
    return result

def row(cells):
    return "| " + " | ".join(cells) + " |\n"

def links(*paths):
    for path in paths:
        assert (out / path).is_file(), path
    return " · ".join(f"[{Path(path).name}]({path})" for path in paths)

p = "probes/corrected/"
scopes = {}
def assign(ids, impact, producer, next_check, limit, *receipts):
    for key in ids.split():
        assert key not in scopes
        scopes[key] = impact, producer + " " + links(*receipts), next_check, limit

assign("M4.1 M4.2 M5.5", "Direct F-S254-02. Bound construction, property and method-group sites now contribute unsupported counters.",
       "Focused exact-count tests and process-separated native fixtures passed.",
       "Fresh independent category, overlap, mixed diagnostic, publication and reopen checks.",
       "Coverage only for these categories.",
       "gates/f02.log", p+"control-ctor-reopen.json", p+"control-property-reopen.json", p+"control-reference-reopen.json")
assign("M5.7", "Direct F-S254-03. Preserve declared missing membership and stage zero-fact partial coverage.",
       "Missing project and missing source each produced IncompleteAnalysis before and after reopen. Actual transition tests preserved stale and unbound-authority refusal.",
       "Fresh real missing-unit and partial/complete/unavailable contrast, including publication transition and reopen.",
       "At least one usable verified source unit and no unrelated provenance refusal.",
       "gates/f03.log", p+"control-incomplete-reopen.json", p+"control-declared-reopen.json")
assign("M5.4", "No production change for this requirement. Producer investigation exercises direct and transitive project references.",
       "Transitive two-site diagnostics persist Unresolved=2 through reopen. Both sites have no method symbol, so the resolver never returned MissingDefiningCompilation for either.",
       "Establish an authority-bound fixture that reaches the exact resolver result, or obtain owner direction on how to disposition the demonstrated boundary.",
       "Exact continuous production evidence remains blocked.",
       p+"m54-transitive-publish.json", p+"m54-transitive-reopen.json", p+"m54-direct-reopen.json")
assign("M1.3 M2.2 M3.1 M3.2 M4.3 M4.4 M6.1", "Complete-universe live binding and identity code retained. Extraction now accounts for additional categories.",
       "All 50 independent B3 derivations, inputs, and ownership match in the R2 profile.",
       "Reuse exact tuple/encoding evidence. Recheck positive identities and acquisition invariants in fresh impacted fixtures.",
       "O-1 host scope and Q-P2 limits.",
       "b3-identity-verification.json", p+"b3-reopen.json")
assign("M5.1 M5.2 M5.3 M5.6", "Unsupported-site branch shares the extraction pass with existing classification counters.",
       "Confirmed, external, distinct multiple-live ambiguity, and inactive coverage observed. R1 tests remain green.",
       "Fresh mixed-counter and R1 confirmation in impacted verification, including zero facts for refusals.",
       "Retain F09 and bounded scanner limits.",
       "gates/r1.log", p+"b3-mixed-reopen.json", p+"multiple-definitions-reopen.json", p+"control-inactive-reopen.json")
assign("M5.8 M9.2 M10.2", "Availability boundary includes real partial analysis under the existing graph/authority rules.",
       "Complete-empty and no-analysis controls preserve exact statuses. Authentic graph-only replay passed.",
       "Recheck complete zero, graph-only, missing authority, and partial-only outcomes on the reviewed tree.",
       "No complete negative from an incomplete publication.",
       p+"control-empty-reopen.json", "gates/f03.log", "historical/sq04-graph-only.json")
assign("M6.2 M6.3 M7.1 M7.2 M7.3 M7.4 M7.5 M9.1 M9.7 M10.5",
       "Published occurrence coverage can have new diagnostic counts or zero-fact partial unit rows. Complete facts retain the same authority envelope.",
       "Bound and partial publication paths, old handle refusal, and native restoration exercised.",
       "Fresh affected transition and persistence checks. Reuse stable node/fact identity and deletion controls where their class permits.",
       "Serialized single writer and existing native schema boundary.",
       "gates/publication.log", "gates/f03.log", p+"b3-reopen.json", p+"control-declared-reopen.json")
assign("M8.1 M8.2 M8.3 M8.4", "Same graph builder and native mirror. New coverage can share the graph unit publication.",
       "Corrected real-project and native mirror comparison passed.",
       "Reuse prior D3 scope with fresh targeted graph noninterference beside affected occurrence publication.",
       "Applicable D3 subset only.",
       p+"d3.json")
assign("M9.4 M9.6 M9.10", "Existing serialization and recovery handle the changed occurrence-unit payload.",
       "Known and unknown managed faults, supersession, and barrier probes passed.",
       "Fresh targeted recovery and ordered publication on affected paths. Retain historical evidence where its scope holds.",
       "Managed faults only. D4.5 remains deferred.",
       p+"state-known-publish.json", p+"state-unknown-reopen.json", p+"state-supersession-publish.json", p+"d4-schedule.json")
assign("M2.3 M2.4 M6.4 M6.5 M9.8", "Contributor, payload and ownership logic retained.",
       "Constructed contributor union/deletion and payload conflict probes passed.",
       "Prior evidence remains reusable within its accepted fixture scope. Confirm affected recovery interaction as needed.",
       "F-01 and constructed controls.",
       p+"b6-reopen-a.json", p+"b7-reopen.json")
assign("M10.1 M10.3 M10.4 M10.6 M13.1 M13.2 M13.3 M15.1", "Codec identities, historical writers, storage schema and native refusal paths retained.",
       "Authentic copies, isolated schema/namespace and separate page-binding refusal rechecked.",
       "Prior amended SQ-04 evidence remains reusable. Recheck distinct refusal outcomes alongside partial coverage as applicable.",
       "Eight legacy codec exclusions and future-discovery reopening obligation.",
       "historical/sq04-integrity.json", p+"d5-namespace.json", p+"d5-schema.json", p+"d5-page.json")
assign("M11.1 M11.2 M11.3", "Additional occurrence syntax checks and partial rows may affect workload costs.",
       "Targeted D3 and native lookup receipts cover the producer workload.",
       "Prior timing remains historical. Take fresh workload-labelled samples for any corrected-revision performance claim.",
       "Host and workload scoped. Existing thresholds and optimization limits apply.",
       p+"d3.json", p+"b3-reopen.json")
assign("M11.4 M14.1", "Managed producer and authority-bound native payload changed since the qualified Ubuntu revision.",
       "Windows native 52-case floor passed.",
       "New pinned genuine Ubuntu run with R1/R2 focused controls, native suite and fresh-process durable reopen.",
       "Corrected-revision Linux evidence remains blocked pending separate owner authorization.",
       "gates/native.log")

matrix = rows(old / "matrix.md", 11)
assert len(matrix) == len({item[0] for item in matrix}) == 64
counts = Counter(item[7] for item in matrix)
assert counts == {"PASS": 42, "PASS WITH QUALIFICATION": 10, "FAIL": 4, "BLOCKED": 3, "REUSED QUALIFIED": 1, "DEFERRED": 4}
with (out / "requalification-impact.md").open("w", encoding="utf-8", newline="\n") as stream:
    stream.write("# R2 requalification impact\n\n")
    stream.write("The original [QUAL-02 matrix](../CI1A-S2.5-WP-S25-4-QUAL-02-evidence/matrix.md) and its 64 results stand. "
                 "The 78-entry [QUAL-02 obligation reconciliation](../CI1A-S2.5-WP-S25-4-QUAL-02-evidence/obligations.md) stands. "
                 "The table records producer checks and the next independent scope.\n\n")
    stream.write(row(["Row", "QUAL-02", "Governing requirement", "R2 impact", "Producer result",
                      "Required independent work or reuse", "Remaining limitation"]))
    stream.write(row(["---"]*7))
    for ident, _, _, _, requirement, _, _, previous, _, _, qualification in matrix:
        if ident in scopes:
            selected = scopes.pop(ident)
        elif previous == "DEFERRED":
            selected = ("Stage 6 scope remains assigned.", "Outside the bounded R2 producer execution.",
                        "Retain Stage 6 deferral.", qualification)
        elif ident == "M14.2":
            selected = ("No change to historical platform documentation.", "Ubuntu receipts remain pinned to their accepted revision.",
                        "Reuse the documented environment behavior within its accepted scope.", qualification)
        else:
            selected = ("No direct change on the governing path.",
                        "Relevant suites passed. " + links("gates/full.log"),
                        "Reuse accepted QUAL-02 result with unchanged prerequisites and limitations.", qualification)
        stream.write(row([ident, previous, requirement, *selected]))
assert not scopes, scopes
obligations = rows(old / "obligations.md", 11)
assert len(obligations) == 78
assert Counter(item[5] for item in obligations) == {
    "DISCHARGED": 11, "QUALIFIED": 49, "BLOCKED": 8, "OPEN-S2.5": 1, "OPEN-STAGE6": 7, "AMENDMENT-SUBJECT": 2,
}
print("Verified 64 carried matrix rows and 78 historical obligation dispositions")

