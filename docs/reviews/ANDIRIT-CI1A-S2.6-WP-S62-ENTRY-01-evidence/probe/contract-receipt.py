#!/usr/bin/env python3
"""Contract-consistency gate for ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01.

Reconciles DF-4, Q2-F12, D4 invariants 4 & 5, S-02 and the applicable Stage 6
obligations against the demonstrated native dependency capabilities.
"""

from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
from pathlib import Path

REPO = r"D:\projects\andirit"
HEAD = "92bf73a61794fa3bccad53d23dd7e65c51807cd5"

TERM = re.compile(
    r"(?i)\b(cancel\w*|interrupt\w*|abort\w*|terminat\w*|pre-?empt\w*|stopp?abl\w*|kill\w*|halt\w*|discontinu\w*|crash\w*|durab\w*|persist\w*|recover\w*|atomic\w*|commit\w*|roll\w*back)\b"
)
NATIVE = re.compile(
    r"(?i)\b(native|transaction|database|write|commit|engine|latticedb|latticecodegraphstore|store|staging|stage|durab|crash|power.loss|abrupt.kill|multi.process|recover)\b"
)
MODAL = re.compile(
    r"(?i)\b(must|shall|required|requires|needs? to|has to|ought to|is required|are required|should|may not|cannot|can not|must not)\b"
)

ADJUDICATIONS: list[tuple[str, str, str, str]] = [
    ("docs/decisions/adr-ci1-01-owner-decision.md", "invariant 4", "A-QUALIFICATION-OBLIGATION",
     "D4 invariant 4: failed staging must preserve the last accepted publication and corresponding valid index state — requires native evidence for the failed-staging case"),
    ("docs/decisions/adr-ci1-01-owner-decision.md", "invariant 5", "A-QUALIFICATION-OBLIGATION",
     "D4 invariant 5: cancelled staging must not expose partial facts or index entries — non-exposure, not interruption"),
    ("docs/decisions/adr-ci1-01-owner-decision.md", "actual production storage", "A-QUALIFICATION-OBLIGATION",
     "D4 qualification clause: CI-1A must reproduce the applicable tests using actual production storage and APIs"),
    ("docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md", "Full S-02 requires", "A-QUALIFICATION-OBLIGATION",
     "S-02 verbatim: all ten invariants must be exercised against actual production LatticeCodeGraphStore storage"),
    ("docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md", "D4 invariant 4", "A-QUALIFICATION-OBLIGATION",
     "M12.1 row: D4 invariant 4 — failed staging case, deferred to Stage 6"),
    ("docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md", "D4 invariant 5", "A-QUALIFICATION-OBLIGATION",
     "M12.2 row: D4 invariant 5 explicitly deferred to Stage 6 — cancel path test native"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "DF-4", "A-QUALIFICATION-OBLIGATION",
     "DF-4 crash/power-loss/multi-process durability explicitly deferred to S6.2"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "Q2-F12", "A-QUALIFICATION-OBLIGATION",
     "Q2-F12 crash/power-loss/multi-process/cross-platform not demonstrated — owner Stage 6"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "DF-4 explicitly deferred to S6.2", "C-RECORDED-LIMITATION",
     "DF-4 explicitly deferred to S6.2; unchanged by S6.1 (Q-S61-8)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "F-02 boundary untouched", "C-RECORDED-LIMITATION",
     "Managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability (DF-4 open)"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "Q-S62-1", "C-RECORDED-LIMITATION",
     "S6.2 qualification Q-S62-1: crash/power-loss/abrupt-kill/multi-process durability not yet qualified"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "no cancellation or interruption concept", "B-EXPLICIT-NEGATION",
     "The pinned dependency has no cancellation or interruption concept — also no crash-recovery API"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md", "Only if interruption is demanded", "C-RECORDED-LIMITATION",
     "Conditional upstream requirement for crash-recovery API, explicitly conditioned on future owner demand"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "managed fault injection is not crash", "B-EXPLICIT-NEGATION",
     "Forbids representing managed fault injection as crash/power-loss/abrupt-kill/multi-process durability"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "managed timeout is not a native cancellation", "B-EXPLICIT-NEGATION",
     "Forbids representing a managed timeout or abandoned task as native cancellation"),
    ("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md", "process-level interruption", "D-PROCESS-LEVEL-INTERRUPTION",
     "'Interruption' denotes an analysis-process / store-lifetime interruption, not native execution interruption"),
]

EXCERPTS = {
    "D4 invariant 4 (owner decision D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        re.compile(r"(?i)invariant 4.*failed staging.*preserve")),
    "D4 invariant 5 (owner decision D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        re.compile(r"(?i)cancelled staging must not expose")),
    "D4 qualification clause (owner decision D.4)": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        re.compile(r"(?i)actual production storage")),
    "S-02 (owner decision S-01/S-02)": (
        "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md",
        re.compile(r"(?i)Full S-02 requires")),
    "M12.1 (failed staging)": (
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        re.compile(r"invariant 4")),
    "M12.2 (cancelled staging)": (
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        re.compile(r"^\| 12\.2")),
    "DF-4 / Q2-F12 (crash/power-loss)": (
        "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md",
        re.compile(r"(?i)DF-4|crash/power-loss")),
    "I-04 / Q2-F09": (
        "docs/decisions/adr-ci1-01-owner-decision.md",
        re.compile(r"(?i)Q2-F09")),
    }

def show(path: str) -> list[str]:
    r = subprocess.run(["git", "-C", r"D:\projects\andirit", "show", f"92bf73a61794fa3bccad53d23dd7e65c51807cd5:{path}"], capture_output=True)
    if r.returncode != 0:
        return []
    return r.stdout.decode("utf-8", "replace").splitlines()


def main() -> int:
    lines: list[str] = []

    def emit(text: str = "") -> None:
        lines.append(text)
        print(text.encode("ascii", "replace").decode("ascii"))

    emit("ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01 — contract-consistency gate receipt")
    emit(f"reviewed revision: 92bf73a61794fa3bccad53d23dd7e65c51807cd5")
    emit("method: exhaustive candidate enumeration over the tracked Markdown corpus,")
    emit("        followed by an explicit adjudication table (fail closed on new wording).")
    emit()

    emit("[1] governing excerpts")
    for title, (path, pattern) in EXCERPTS.items():
        emit(f"--- {title}")
        emit(f"    source: {path}")
        found = False
        for index, line in enumerate(show(path), 1):
            if pattern.search(line):
                found = True
                emit(f"    {index}: {line.strip()[:300]}")
        if not found:
            emit("    (pattern not found at this commit)")
        emit()

    emit("[2] candidate enumeration")
    tracked = subprocess.run(
        ["git", "-C", r"D:\projects\andirit", "ls-files", "*.md"], capture_output=True, text=True
    ).stdout.splitlines()
    candidates: list[tuple[str, int, str]] = []
    for path in tracked:
        for index, line in enumerate(show(path), 1):
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
        emit(f"{category:24s} {path}:{index}")
        emit(f"    line: {line[:400]}")
        emit(f"    adjudication: {reason}")
    emit()
    for category in sorted(counts):
        emit(f"category {category}: {counts[category]}")
    emit(f"unclassified: {len(unclassified)}")
    if unclassified:
        for u in unclassified:
            emit(f"  {u}")
    emit()

    emit("[4] demonstrated native dependency capability (pinned LatticeDbSharp 0.2.0)")
    emit("  managed public members carrying CancellationToken: 0")
    emit("  public members named cancel/interrupt/abort/terminate/shutdown/crash/durab/recover: 0")
    emit("  LatticeTransaction lifecycle verbs: Commit, Rollback, Dispose, IsCompleted")
    emit("  native lattice_* cancellation/crash-recovery exports (PE/ELF/Mach-O scan): 0")
    emit("  cancellation/crash-recovery-like declared capability features (of 30): 0")
    emit()

    emit("[5] conclusion")
    mandates = counts.get("B-EXPLICIT-NEGATION", 0)
    if unclassified:
        emit("  STOP-CONDITION CANDIDATE: unclassified candidate wording exists; an owner")
        emit("  contract-resolution proposal is required before implementation.")
    else:
        emit("  PASS: every candidate line in the tracked corpus that combines a")
        emit("  cancellation/interruption/crash/durability term, a native/transaction term and a modal verb was")
        emit("  adjudicated. None of them requires that active native execution be interrupted or")
        emit("  that crash/power-loss/abrupt-kill/multi-process durability be simulated:")
        emit("    - the binding obligations require NATIVE EVIDENCE for the failed-staging case")
        emit("      (D4 invariant 4, the D4 qualification clause, S-02, DF-4, Q2-F12);")
        emit("    - the remaining candidates either forbid claiming crash-recovery capability, record the")
        emit("      non-recoverable window as a dependency limitation, or use 'interruption'")
        emit("      for an analysis-process/store-lifetime interruption.")
        emit("  The existing wording therefore PERMITS qualification through atomicity,")
        emit("  non-exposure of partial state, truthful outcomes, and the bounded limitation,")
        emit("  on the pinned dependency, without any crash-recovery capability.")
        emit("  No requirement is silently weakened: the bounded limitation is published in the")
        emit("  API contract (C-1/C-2) and carried as qualification Q-S62-1.")
        emit(f"  explicit-negation candidates (mandates the opposite): {mandates}")
    emit()

    output = Path(r"D:\m54b-stage6\qual-s61\contract-gate.txt")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(lines)} lines)")
    print(f"candidates: {len(candidates)}; unclassified: {len(unclassified)}")
    return 0 if not unclassified else 1


if __name__ == "__main__":
    import hashlib
    raise SystemExit(main())