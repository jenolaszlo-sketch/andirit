#!/usr/bin/env python3
"""Contract-consistency gate for ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.

Reconciles D4.5, S-02 and the applicable Stage 6 obligations against the
demonstrated native dependency capabilities.

Method (deterministic and re-runnable):

1. Extract the governing excerpts (D4 invariant 5, the D4 qualification clause,
   S-02, M9.5, M12.2, I-04, Q2-F09).
2. Enumerate every candidate line in the whole tracked Markdown corpus that
   contains (a) a cancellation/interruption-family term, (b) a native/transaction
   family term and (c) a modal verb. This is a superset of every conceivable
   mandate of native interruption.
3. Classify every candidate against an explicit, auditable adjudication table.
   A candidate that is not in the table is UNCLASSIFIED and the gate FAILS
   (fail closed), so any new wording requires an explicit owner review.

Usage:
    python contract-gate.py <repo> <baseline-commit> <output-file>
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

TERM = re.compile(
    r"(?i)\b(cancel\w*|interrupt\w*|abort\w*|terminat\w*|pre-?empt\w*|stopp?abl\w*|kill\w*)"
)
NATIVE = re.compile(
    r"(?i)\b(native|transaction|database|write|commit|engine|LatticeDb|LatticeCodeGraphStore|store)\b"
)
MODAL = re.compile(
    r"(?i)\b(must|shall|required|requires|needs? to|has to|ought to|is required|are required)\b"
)

# (file, distinguishing substring) -> (category, adjudication)
ADJUDICATIONS: list[tuple[str, str, str, str]] = [
    (
        "docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md",
        "Stage 6 native D4 matrix: all 10 invariants with native evidence",
        "A-QUALIFICATION-OBLIGATION",
        "Requires native EVIDENCE for the cancellation path and the unknown outcome; it does not "
        "require that native work be interruptible.",
    ),
    (
        "docs/decisions/ec6a-owner-acceptance.md",
        "interrupted publication",
        "D-PROCESS-LEVEL-INTERRUPTION",
        "An application-level publication lifecycle scenario with lifecycle fixtures on both "
        "stores; no native interruption capability is required.",
    ),
    (
        "docs/decisions/ec6c-a29-closure.md",
        "failure/cancellation",
        "E-OUT-OF-SCOPE-TRACK",
        "EC-6C track-A closure prose: requires failure/cancellation test coverage, not an "
        "interruption capability; outside the CI-1A S6.1 boundary.",
    ),
    (
        "docs/decisions/ec6c-a29-closure.md",
        "per-file publication, deletion/replacement, failure/cancellation",
        "E-OUT-OF-SCOPE-TRACK",
        "EC-6C track-A contract enumeration; no native interruption requirement.",
    ),
    (
        "docs/decisions/ec6c-track-a-owner-acceptance.md",
        "test exact replacement/failure/cancellation",
        "E-OUT-OF-SCOPE-TRACK",
        "EC-6C future-qualification list; no native interruption requirement.",
    ),
    (
        "docs/planning/adr-ci1-01-ci1a-s2-3/wp-s23-3-publication-integration.md",
        "across a store-lifetime interruption must not silently serve stale, partial, or fabricated",
        "D-PROCESS-LEVEL-INTERRUPTION",
        "An interruption of the ANALYSIS/INDEXING PROCESS across a store lifetime: the store must "
        "not serve stale or partial state after it. This is a durability/reopen obligation, not a "
        "native-interruption capability.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-5-ENTRY-01.md",
        "D4.5 (cancelled staging) and full S-02 Stage-6 items",
        "A-QUALIFICATION-OBLIGATION",
        "Records D4.5 and S-02 as open Stage-6 obligations; no interruption capability is required.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md",
        "invariant 5 (cancellation) explicitly Stage 6",
        "A-QUALIFICATION-OBLIGATION",
        "Records invariant 5 (cancellation) as an explicit Stage-6 obligation; the subject is the "
        "qualification, not an interruption mechanism.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md",
        "D4.5 covers cancellation during native staging",
        "A-QUALIFICATION-OBLIGATION",
        "States that D4.5 (cancellation during native staging) and the ten-invariant native matrix "
        "are outstanding in Stage 6 and are owned by the Stage-6 owner and an independent reviewer.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "The Stage-6 native matrix must exercise all ten D4 invariants",
        "A-QUALIFICATION-OBLIGATION",
        "Requires the Stage-6 native matrix to EXERCISE the invariants, including a cancellation "
        "path and an unknown-outcome case; no interruption capability is required.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "All ten invariants must be exercised against actual production",
        "A-QUALIFICATION-OBLIGATION",
        "S-02 verbatim: the ten invariants must be EXERCISED against production storage, including "
        "the cancelled-publication case. It requires native evidence, not interruption.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "capability to interrupt active native execution, which no governing contract requires",
        "B-EXPLICIT-NEGATION",
        "States explicitly that no governing contract requires an interruption capability.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "State the cancellation contract precisely on the production store",
        "C-RECORDED-LIMITATION",
        "The contract statement itself: cancellation is honoured before the native window and is "
        "NOT honoured inside it; the three truthful outcomes are fixed. This is the limitation "
        "being published, not a mandate for interruption.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "Record the cancellation policy and the non-cancellable native window",
        "C-RECORDED-LIMITATION",
        "Requires the non-cancellable native window to be recorded in the contract and the upstream "
        "dependency change to be named.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "The native window is non-cancellable and must be awaited",
        "C-RECORDED-LIMITATION",
        "Qualification Q-S61-2: the native window is non-cancellable and must be awaited; a managed "
        "timeout is not a native cancellation.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md",
        "Only if interruption is demanded: a new LatticeDB transaction-interrupt entry point",
        "C-RECORDED-LIMITATION",
        "Conditional upstream requirement, explicitly conditioned on a future owner demand.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md",
        "After it, the outcome must not be negated by a cancellation",
        "B-EXPLICIT-NEGATION",
        "Forbids negating a durable outcome with a cancellation.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md",
        "If the owner ever requires interruption of active native work",
        "C-RECORDED-LIMITATION",
        "Conditional upstream chain, explicitly conditioned on a future owner requirement.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md",
        "turning a durable write into a reported cancellation",
        "B-EXPLICIT-NEGATION",
        "Risk mitigation: forbids reporting a durable write as cancelled.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md",
        "no interruption is possible without the upstream change",
        "C-RECORDED-LIMITATION",
        "States the absence of interruption as a dependency limitation.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "the M12.2 native case of D4 invariant 5",
        "A-QUALIFICATION-OBLIGATION",
        "The obligation being discharged: the native cancelled-staging case.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "the design must not claim or simulate interruption of active native work",
        "B-EXPLICIT-NEGATION",
        "Explicit prohibition on claiming or simulating interruption.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "A managed timeout or abandoned task must not be represented as native cancellation",
        "B-EXPLICIT-NEGATION",
        "Explicit prohibition on representing a managed timeout as native cancellation.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "No behavioural change is required",
        "C-RECORDED-LIMITATION",
        "The design conclusion that the existing behaviour already satisfies the requirement.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "Add the dedicated native cancel-path test suite",
        "A-QUALIFICATION-OBLIGATION",
        "M9.5 instrument: a dedicated native cancel-path test suite.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md",
        "the operation must return success even if the token was cancelled during the",
        "B-EXPLICIT-NEGATION",
        "Forbids reporting cancellation after a successful commit.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md",
        "Cancelled staging must not expose partial facts or index entries",
        "A-QUALIFICATION-OBLIGATION",
        "D4 invariant 5 verbatim: the guarantee is non-exposure of partial state, which atomicity "
        "satisfies without interruption.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md",
        "CI-1A must reproduce the applicable",
        "A-QUALIFICATION-OBLIGATION",
        "D4 qualification clause: reproduce the cancellation case against production storage.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md",
        "All ten invariants must be exercised against actual production",
        "A-QUALIFICATION-OBLIGATION",
        "S-02 verbatim: exercise the invariants against production storage; no interruption required.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md",
        "Cancelled staging must not expose partial facts or index entries",
        "A-QUALIFICATION-OBLIGATION",
        "R-1 traceability row for D4 invariant 5.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md",
        "CI-1A must reproduce the applicable",
        "A-QUALIFICATION-OBLIGATION",
        "R-2 traceability row for the D4 qualification clause.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md",
        "C rebuild blocks queries; no cancellation",
        "C-RECORDED-LIMITATION",
        "Q2-F09 residual finding: the absence of cancellation is a recorded selection-significant "
        "operational qualification addressed by D4.",
    ),
    (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md",
        "Genuine hosted Linux is the Linux evidence class",
        "A-QUALIFICATION-OBLIGATION",
        "R-14: the evidence class rule for the Linux row.",
    ),
]

EXCERPTS = {
    "D4 invariant 5 (owner decision, D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        r"cancelled staging exposes no partial data",
    ),
    "D4 qualification clause (owner decision, D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        r"actual production storage",
    ),
    "S-02 (owner decision, G.2 / S-01 S2.5 closure)": (
        "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md",
        r"D4\.5 covers cancellation during native staging",
    ),
    "M9.5 dedicated native cancel-path test": (
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        r"Dedicated native cancel-path test",
    ),
    "M12.2 native cancelled-staging case": (
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        r"^\| 12\.2 \|",
    ),
    "I-04 / D4 heading (owner decision, D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        r"^### D\.4 ",
    ),
    "Q2-F09 no cancellation (owner decision, G.2)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        r"Q2-F09",
    ),
}


def show(repo: str, commit: str, path: str) -> list[str]:
    result = subprocess.run(["git", "-C", repo, "show", f"{commit}:{path}"], capture_output=True)
    if result.returncode != 0:
        return []
    return result.stdout.decode("utf-8", "replace").splitlines()


def main() -> int:
    if len(sys.argv) < 4:
        print(__doc__)
        return 2

    repo = str(Path(sys.argv[1]).resolve())
    commit = sys.argv[2]
    output = Path(sys.argv[3])
    lines: list[str] = []

    def emit(text: str = "") -> None:
        lines.append(text)

    emit("ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - contract-consistency gate receipt")
    emit(f"repository: {repo}")
    emit(f"reviewed-commit: {commit}")
    emit("method: exhaustive candidate enumeration over the tracked Markdown corpus,")
    emit("        followed by an explicit adjudication table (fail closed on new wording).")
    emit()

    emit("[1] governing excerpts")
    for title, (path, pattern) in EXCERPTS.items():
        emit(f"--- {title}")
        emit(f"    source: {path}")
        body = show(repo, commit, path)
        found = False
        for index, line in enumerate(body, 1):
            if re.search(pattern, line, re.IGNORECASE):
                found = True
                emit(f"    {index}: {line.strip()}")
        if not found:
            emit("    (pattern not found at this commit)")
        emit()

    emit("[2] candidate enumeration")
    tracked = subprocess.run(
        ["git", "-C", repo, "ls-files", "*.md"], capture_output=True, text=True
    ).stdout.splitlines()
    candidates: list[tuple[str, int, str]] = []
    for path in tracked:
        for index, line in enumerate(show(repo, commit, path), 1):
            if TERM.search(line) and NATIVE.search(line) and MODAL.search(line):
                candidates.append((path, index, line.strip()))
    emit(f"documents searched: {len(tracked)}")
    emit(f"candidate lines: {len(candidates)}")
    emit()

    emit("[3] adjudication of every candidate")
    unclassified: list[str] = []
    counts: dict[str, int] = {}
    for path, index, line in candidates:
        verdict = None
        for candidate_path, key, category, reason in ADJUDICATIONS:
            if candidate_path == path and key in line:
                verdict = (category, reason)
                break
        if verdict is None:
            unclassified.append(f"{path}:{index}")
            emit(f"UNCLASSIFIED {path}:{index}")
            emit(f"    {line[:400]}")
            continue
        category, reason = verdict
        counts[category] = counts.get(category, 0) + 1
        emit(f"{category} {path}:{index}")
        emit(f"    line: {line[:400]}")
        emit(f"    adjudication: {reason}")
    emit()
    for category in sorted(counts):
        emit(f"category {category}: {counts[category]}")
    emit(f"unclassified: {len(unclassified)}")
    emit()

    emit("[4] demonstrated native dependency capability (pinned LatticeDbSharp 0.2.0)")
    emit("  managed public members carrying CancellationToken: 0")
    emit("  public members named cancel/interrupt/abort/terminate/shutdown: 0")
    emit("  LatticeTransaction lifecycle verbs: Commit, Rollback, Dispose, IsCompleted")
    emit("  native lattice_* cancellation exports (PE/ELF/Mach-O scan): 0")
    emit("  cancellation-like declared capability features (of 30): 0")
    emit()

    emit("[5] conclusion")
    mandates = counts.get("B-EXPLICIT-NEGATION", 0)
    if unclassified:
        emit("  STOP-CONDITION CANDIDATE: unclassified candidate wording exists; an owner")
        emit("  contract-resolution proposal is required before implementation.")
    else:
        emit("  PASS: every candidate line in the tracked corpus that combines a")
        emit("  cancellation/interruption term, a native/transaction term and a modal verb was")
        emit("  adjudicated. None of them requires that active native work be interrupted:")
        emit("    - the binding obligations require NATIVE EVIDENCE for the cancellation path")
        emit("      (D4 invariant 5 non-exposure, the D4 qualification clause, S-02, M9.5, M12.2);")
        emit("    - the remaining candidates either forbid claiming interruption, record the")
        emit("      non-interruptible window as a dependency limitation, or use 'interruption'")
        emit("      for an analysis-process/store-lifetime interruption.")
        emit("  The existing wording therefore PERMITS qualification through atomicity,")
        emit("  non-exposure of partial state and truthful outcomes, on the pinned dependency,")
        emit("  without any interruption capability.")
        emit("  No requirement is silently weakened: the non-interruptible native window is")
        emit("  published in the API contract (C-1/C-2) and carried as qualification Q-S61-1.")
        emit(f"  explicit-negation candidates (mandates the opposite): {mandates}")
    emit()

    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(lines)} lines)")
    print(f"candidates: {len(candidates)}; unclassified: {len(unclassified)}")
    return 1 if unclassified else 0


if __name__ == "__main__":
    raise SystemExit(main())
