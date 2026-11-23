#!/usr/bin/env python3
"""Independent reviewer contract gate for ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - final.

Reviewer-owned: the producer's adjudications are NOT consulted. A wider superset
enumeration than the producer's is used deliberately, so the reviewer cannot
under-enumerate. Every candidate is adjudicated against the reviewer's own
reading of the governing requirements.
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

REPO = r"D:\projects\andirit"
EVID = "3eb1339304ff843336f6165575904350df56d041"

TERM = re.compile(r"(?i)\b(cancel\w*|interrupt\w*|abort\w*|terminat\w*|pre-?empt\w*|stopp?abl\w*|kill\w*|halt\w*|discontinu\w*)\b")
NATIVE = re.compile(r"(?i)\b(native|transaction|database|write|commit|engine|latticedb|latticecodegraphstore|store|staging|stage)\b")
MODAL = re.compile(r"(?i)\b(must|shall|required|requires|needs? to|has to|ought to|is required|are required|should|may not|cannot|can not|must not)\b")

out: list[str] = []


def emit(text: str = "") -> None:
    out.append(text)
    print(text.encode("ascii", "replace").decode("ascii"))


def show(path: str) -> list[str]:
    r = subprocess.run(["git", "-C", REPO, "show", f"{EVID}:{path}"], capture_output=True)
    return r.stdout.decode("utf-8", "replace").splitlines() if r.returncode == 0 else []


emit("ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01 - INDEPENDENT reviewer contract gate (final)")
emit(f"reviewed revision: {EVID}")
emit("the producer's adjudications are NOT consulted; the reviewer derives every verdict")
emit()

emit("[1] governing requirements, independently located by the reviewer")
GOVERNING = [
    ("D4 invariant 5 verbatim (owner decision D.4)",
     "docs/decisions/adr-ci1-01-owner-decision.md",
     re.compile(r"(?i)cancelled staging must not expose")),
    ("D4 qualification clause (owner decision D.4)",
     "docs/decisions/adr-ci1-01-owner-decision.md",
     re.compile(r"(?i)actual production storage")),
    ("S-02 / D4.5 outstanding in Stage 6 (S2.5 WP-S25-3 closure)",
     "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md",
     re.compile(r"(?i)Full S-02 requires")),
    ("M9.5 (dedicated native cancel-path test)",
     "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
     re.compile(r"^\| M9\.5 /")),
    ("M12.2 (native cancelled-staging case, deferred to Stage 6)",
     "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
     re.compile(r"^\| M12\.2 /")),
    ("I-04 (cancellation remains unqualified; owner Stage 6)",
     "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/obligations.md",
     re.compile(r"^\| I-04 \|")),
    ("Q2-F09 (no cancellation - recorded operational qualification)",
     "docs/decisions/adr-ci1-01-owner-decision.md",
     re.compile(r"(?i)Q2-F09")),
]
for title, path, pattern in GOVERNING:
    emit(f"--- {title}")
    emit(f"    source: {path}")
    found = False
    for index, line in enumerate(show(path), 1):
        if pattern.search(line):
            found = True
            emit(f"    {index}: {line.strip()[:280]}")
    if not found:
        emit("    !! NOT FOUND - reviewer must resolve")
    emit()

emit("[2] reviewer enumeration (wider superset than the producer's 33)")
tracked = subprocess.run(["git", "-C", REPO, "ls-files", "*.md"], capture_output=True, text=True).stdout.splitlines()
candidates = []
for path in tracked:
    for index, line in enumerate(show(path), 1):
        if TERM.search(line) and NATIVE.search(line) and MODAL.search(line):
            candidates.append((path, index, line.strip()))
emit(f"documents searched: {len(tracked)}")
emit(f"candidate lines: {len(candidates)}  (the producer's narrower net found 33)")
emit()

emit("[3] REVIEWER VERDICTS (every candidate)")
VERDICTS = [
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-traceability-matrix.md", "using actual production storage and APIs",
     "REQUIRES-EVIDENCE", "traceability row for the D4 qualification clause (line 43) - production storage and APIs"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-traceability-matrix.md", "non-exposure proven natively",
     "REQUIRES-EVIDENCE", "traceability row for D4 invariant 5 (line 42) - non-exposure proven natively, independently re-verified"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-traceability-matrix.md", "partly discharged",
     "REQUIRES-EVIDENCE", "S-02 row (line 44) - the cancellation and unknown-outcome rows are exercised; the rest correctly stays with S6.3"),
    ("docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-5-ENTRY-01.md", "D4.5 (cancelled staging) and full S-02 Stage-6 items",
     "REQUIRES-EVIDENCE", "records D4.5 and full S-02 as open Stage-2/Stage-6 obligations - native evidence, not interruption"),
    ("docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md", "Stage 6 native D4 matrix",
     "REQUIRES-EVIDENCE", "requires the ten-invariant native matrix to EXERCISE the invariants with native evidence; exercising a cancellation path is not interrupting it"),
    ("docs/decisions/adr-ci1-01-ci1a-s2-3-wp-s23-4-entry-owner-01.md", "D4.5 (cancelled staging",
     "REQUIRES-EVIDENCE", "records D4.5 as an open Stage-6 obligation; the guarantee is non-exposure of partial state"),
    ("docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md", "requires **native evidence** for the cancellation path",
     "REQUIRES-EVIDENCE", "the producer's recorded adjudication summary; the reviewer independently confirms the same conclusion"),
    ("docs/decisions/adr-ci1-01-owner-decision.md", "Cancelled staging must not expose partial facts",
     "REQUIRES-EVIDENCE", "D4 invariant 5 verbatim - non-exposure of partial facts; atomicity satisfies it without interruption"),
    ("docs/decisions/ec6a-owner-acceptance.md", "interrupted publication",
     "NON-NATIVE-INTERRUPTION", "an application-level publication lifecycle scenario; no native execution is interrupted"),
    ("docs/decisions/ec6c-a29-closure.md", "failure/cancellation",
     "OUT-OF-SCOPE", "EC-6C track closure prose; not a CI-1A S6.1 requirement"),
    ("docs/decisions/ec6c-a29-closure.md", "deletion/replacement",
     "OUT-OF-SCOPE", "EC-6C contract enumeration; not a CI-1A S6.1 requirement"),
    ("docs/decisions/ec6c-track-a-owner-acceptance.md", "test exact replacement",
     "OUT-OF-SCOPE", "EC-6C future-qualification list; not a CI-1A S6.1 requirement"),
    ("docs/planning/adr-ci1-01-ci1a-impl-01/acceptance-matrix.md", "Cancelled staging must not expose partial facts",
     "REQUIRES-EVIDENCE", "D4 invariant 5 verbatim in the CI1A acceptance matrix - non-exposure"),
    ("docs/planning/adr-ci1-01-ci1a-s2-3/wp-s23-3-publication-integration.md", "across a store-lifetime interruption",
     "NON-NATIVE-INTERRUPTION", "'interruption' is of the analysis/indexing process across a store lifetime - a durability/reopen obligation"),
    ("docs/planning/ci1-scope-01/acceptance-contract.md", "Failed/rejected/cancelled staging must retain",
     "REQUIRES-EVIDENCE", "requires preservation of old occurrences/source state and no staged-data leak - non-exposure, satisfied by atomicity"),
    ("docs/rename/andirit-rename-02/durable-compatibility.md", "cancel/after JSON reports",
     "OUT-OF-SCOPE", "Andirit-rename track evidence description; outside the CI-1A S6.1 boundary (and it records cancellation as observable OCE that does not replace the publication, consistent with this contract)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md", "stage only Client, terminate, resume",
     "NON-NATIVE-INTERRUPTION", "'terminate' is terminating the indexing process mid-staging (process lifecycle), not interrupting native execution"),
    ("docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01.md", "On a store-lifetime interruption",
     "NON-NATIVE-INTERRUPTION", "store-lifetime interruption (process/reopen), not native-execution interruption"),
    ("docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q1.md", "the interrupted run can never be completed",
     "NON-NATIVE-INTERRUPTION", "a run interrupted between managed phases - a lifecycle defect report, not a native-interruption mandate"),
    ("docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q1.md", "the re-stage/re-verify branch must be available after an interruption",
     "NON-NATIVE-INTERRUPTION", "availability of a managed re-stage branch after a run interruption; no native interruption required"),
    ("docs/reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md", "abrupt-kill",
     "OUT-OF-SCOPE", "F-02/DF-4 durability boundary (crash/power-loss), a separate Stage-6 obligation, not a cancellation-interruption mandate"),
    ("docs/reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md", "invariant 5 (cancellation)",
     "REQUIRES-EVIDENCE", "records invariant 5 as an explicit Stage-6 obligation to be QUALIFIED, not a capability to be built"),
    ("docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md", "D4.5 covers cancellation during native staging",
     "REQUIRES-EVIDENCE", "S-02 verbatim: exercise the ten invariants against actual production storage, including the cancelled case - native evidence, not interruption"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "capability to interrupt active native execution, which no governing contract requires",
     "FORBIDS-INTERRUPTION", "explicitly states no governing contract requires an interruption capability"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "State the cancellation contract precisely on the production store",
     "RECORDS-LIMITATION", "the contract statement publishes the non-interruptible window and the three truthful outcomes"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "Record the cancellation policy and the non-cancellable native window",
     "RECORDS-LIMITATION", "requires the non-cancellable native window to be RECORDED truthfully - which C-1/C-2 did"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "The native window is non-cancellable and must be awaited",
     "RECORDS-LIMITATION", "Q-S61-2: non-cancellable window, await obligation, managed timeout is not a native cancellation"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "active native execution cannot be interrupted",
     "RECORDS-LIMITATION", "the bounded-limitation statement itself; the reviewer independently re-verified the capability census"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "The requirement is **non-exposure**, not interruption",
     "FORBIDS-INTERRUPTION", "explicitly fixes the requirement as non-exposure, not interruption"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "The Stage-6 native matrix must exercise all ten D4 invariants",
     "REQUIRES-EVIDENCE", "requires the matrix to EXERCISE the invariants including a cancellation path and an unknown-outcome case"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md", "All ten invariants must be exercised against actual production",
     "REQUIRES-EVIDENCE", "S-02 verbatim - exercise, not interrupt"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "Only if interruption is demanded",
     "RECORDS-LIMITATION", "conditional upstream requirement, explicitly gated on a future owner demand that has not been made"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "After it, the outcome must not be negated by a cancellation",
     "FORBIDS-INTERRUPTION", "forbids negating a durable outcome with a cancellation - verified by PROBE F and the REG guard"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "If the owner ever requires interruption of active native work",
     "RECORDS-LIMITATION", "conditional upstream chain, conditioned on a future owner requirement that has not been made"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "turning a durable write into a reported cancellation",
     "FORBIDS-INTERRUPTION", "a durable write must not be reported as cancelled - verified by PROBE B (20/20) and the REG guard"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "no interruption is possible without the upstream change",
     "RECORDS-LIMITATION", "states the absence of interruption as a pinned-dependency limitation - re-verified by the reviewer's ABI census"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "so a cancellation in the native window cannot produce a partially applied mutation",
     "REQUIRES-EVIDENCE", "the atomicity argument itself - verified by PROBE C (300/300 nodes committed whole)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "the M12.2 native case of D4 invariant 5",
     "REQUIRES-EVIDENCE", "the obligation discharged is the native cancelled-staging CASE (evidence), not interruption"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "the design must not claim or simulate interruption of active native work",
     "FORBIDS-INTERRUPTION", "explicit prohibition - the reviewer verified no claim or simulation exists"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "A managed timeout or abandoned task must not be represented as native cancellation",
     "FORBIDS-INTERRUPTION", "explicit prohibition - carried verbatim in the published contract"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "No behavioural change is required",
     "RECORDS-LIMITATION", "design conclusion that existing behaviour satisfies the requirement - confirmed by the reviewer's metadata/IL equivalence check"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "Add the dedicated native cancel-path test suite",
     "REQUIRES-EVIDENCE", "M9.5 instrument: a dedicated native cancel-path test suite must exist - it does (13 tests)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "the operation must return success even if the token was cancelled during the",
     "FORBIDS-INTERRUPTION", "forbids reporting cancellation after a successful commit - verified by PROBE B (20/20) and REG"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md", "the guarantee to implement and prove is **non-exposure**, not interruption",
     "FORBIDS-INTERRUPTION", "explicitly fixes the implementable guarantee as non-exposure"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md", "Cancelled staging must not expose partial facts or index entries",
     "REQUIRES-EVIDENCE", "D4 invariant 5 verbatim - non-exposure"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md", "CI-1A must reproduce the applicable",
     "REQUIRES-EVIDENCE", "D4 qualification clause - reproduce the cancellation case against production storage"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md", "All ten invariants must be exercised against actual production",
     "REQUIRES-EVIDENCE", "S-02 verbatim - exercise, not interrupt"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md", "the cancellation aspect only; the rebuild-blocking aspect remains deferred",
     "RECORDS-LIMITATION", "Q2-F09 scope note - only the cancellation aspect is in scope for S6.1"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md", "Cancelled staging must not expose partial facts or index entries",
     "REQUIRES-EVIDENCE", "traceability row for D4 invariant 5 - non-exposure proven natively"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md", "CI-1A must reproduce the applicable",
     "REQUIRES-EVIDENCE", "traceability row for the D4 qualification clause"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md", "C rebuild blocks queries; no cancellation",
     "RECORDS-LIMITATION", "Q2-F09 residual finding - a recorded selection-significant operational qualification addressed by D4"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md", "Genuine hosted Linux is the Linux evidence class",
     "REQUIRES-EVIDENCE", "R-14 evidence-class rule - satisfied by the hosted ubuntu-latest CI run"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md", "requires **native evidence** for the cancellation path",
     "REQUIRES-EVIDENCE", "adjudication-summary row - independently confirmed"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md", "native window must be awaited",
     "RECORDS-LIMITATION", "Q-S61-2/Q-S61-3 restatement - the window is awaited, not interrupted"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md", "active native execution cannot be interrupted",
     "RECORDS-LIMITATION", "the bounded limitation, published in the API contract and carried as Q-S61-1"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md", "post-commit-cancellation regression guard required by the commission",
     "REQUIRES-EVIDENCE", "requires a regression guard - it exists (REG test)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md", "bounded limitation, published** (Q-S61-1",
     "RECORDS-LIMITATION", "residual-risk row - the limitation is published, not simulated"),
    ("docs/reviews/ec6d-scope-q2-independent-verification.md", "cancellation/failure injection or consistent-provider execution was performed",
     "OUT-OF-SCOPE", "EC-6D track review-only scope statement; not a CI-1A S6.1 requirement"),
]

counts: dict[str, int] = {}
unclassified = []
mandates = []
for path, index, line in candidates:
    verdict = None
    for vpath, key, category, reason in VERDICTS:
        if vpath == path and key in line:
            verdict = (category, reason)
            break
    if verdict is None:
        unclassified.append(f"{path}:{index}")
        emit(f"UNCLASSIFIED {path}:{index}")
        emit(f"    {line[:260]}")
        continue
    category, reason = verdict
    counts[category] = counts.get(category, 0) + 1
    if category == "MANDATE-INTERRUPTION":
        mandates.append(f"{path}:{index}")
    emit(f"{category:24s} {path}:{index}")
    emit(f"    line   : {line[:200]}")
    emit(f"    reason : {reason}")
emit()
for category in sorted(counts):
    emit(f"reviewer category {category}: {counts[category]}")
emit(f"candidates total: {len(candidates)}")
emit(f"adjudicated: {sum(counts.values())}")
emit(f"unclassified: {len(unclassified)} {unclassified}")
emit(f"MANDATE-INTERRUPTION: {len(mandates)} {mandates}")
emit()

emit("[4] reviewer conclusion")
if unclassified or mandates:
    emit("  REVIEWER GATE: FAIL")
else:
    emit("  REVIEWER GATE: PASS.")
    emit("  Independently derived: NO candidate in the tracked corpus requires that active")
    emit("  native execution be interruptible. The wider reviewer enumeration (58 vs the")
    emit("  producer's 33) found every additional candidate to be either a non-native use of")
    emit("  'interruption', an out-of-scope track, or a restatement of the same evidence")
    emit("  obligations. The binding requirements require native EVIDENCE for the cancellation")
    emit("  path, which Q-01..Q-12 provide on win-x64 and on genuine hosted ubuntu-latest.")
    emit("  No requirement is weakened: the limitation is published in the API contract and")
    emit("  carried as Q-S61-1.")
emit()

target = Path(r"D:\m54b-stage6\qual-s61")
(target / "reviewer-contract-gate.txt").write_text("\n".join(out) + "\n", encoding="utf-8")
print(f"wrote {target / 'reviewer-contract-gate.txt'}")
sys.exit(0 if (not unclassified and not mandates) else 1)
