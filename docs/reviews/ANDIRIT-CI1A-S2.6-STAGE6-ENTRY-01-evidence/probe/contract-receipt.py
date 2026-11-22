#!/usr/bin/env python3
"""Contract-reconciliation receipt for ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.

Extracts the governing requirement text that defines the S6.1 acceptance
boundary from the baseline commit, so every citation in the traceability matrix
is bound to an exact file and line range in Git. Read-only.

Usage:
    python contract-receipt.py <repo> <baseline-commit> <output-file>
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

EXCERPTS = [
    (
        "ADR-CI1-01 owner decision — D.4 index maintenance, publication and cancellation",
        "docs/decisions/adr-ci1-01-owner-decision.md",
        259,
        299,
    ),
    (
        "Owner decision §G.2 — Q2-F09 / Q2-F12 selection-significant qualifications",
        "docs/decisions/adr-ci1-01-owner-decision.md",
        495,
        505,
    ),
    (
        "S-01 obligation reconciliation — S-02 full native D4 qualification",
        "docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md",
        259,
        274,
    ),
    (
        "CI1A implementation specification — S-02 requirement (line 194-197) and I-04 (line 283)",
        "docs/planning/adr-ci1-01-ci1a-impl-01/specification.md",
        190,
        200,
    ),
    (
        "CI1A acceptance matrix — D4 invariants 1-10 (line 8-31) and matrix rows 4-6 (line 33-38)",
        "docs/planning/adr-ci1-01-ci1a-impl-01/acceptance-matrix.md",
        8,
        38,
    ),
    (
        "S2.5 qualification matrix — M9.5 cancellation row (line 90) and M12.1-M12.3 rows (line 121-123)",
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        90,
        90,
    ),
    (
        "S2.5 qualification matrix — M12.1-M12.3 full native matrix rows",
        "docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md",
        121,
        123,
    ),
    (
        "CI1A S2 obligation register — I-04 (line 216), Q2-F09 (line 227), Q2-F12 (line 230)",
        "docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md",
        216,
        216,
    ),
    (
        "Residual-risk register — deferred obligations DF-1..DF-8 (line 39-50)",
        "docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-m54-owner-01-evidence/residual-risk-register.md",
        39,
        50,
    ),
    (
        "Stage 6 entry proposal — deferred scope, work-package order and qualification gates",
        "docs/decisions/adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/stage6-entry-proposal.md",
        9,
        47,
    ),
    (
        "S2.5 entry assessment — native versus compatibility environment rule",
        "docs/reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md",
        124,
        126,
    ),
]


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__)
        return 2

    repo, commit, output = sys.argv[1], sys.argv[2], sys.argv[3]
    lines: list[str] = []
    lines.append("Contract-reconciliation receipt — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01")
    lines.append(f"baseline commit: {commit}")
    lines.append("All excerpts below are byte-exact Git blobs at the baseline commit.")
    lines.append("")

    for label, path, start, end in EXCERPTS:
        blob = subprocess.run(
            ["git", "-C", repo, "cat-file", "blob", f"{commit}:{path}"], capture_output=True
        ).stdout.decode("utf-8", "replace")
        content = blob.splitlines()
        lines.append("=" * 100)
        lines.append(f"EXCERPT: {label}")
        lines.append(f"SOURCE: {path} lines {start}-{end}")
        lines.append("-" * 100)
        for number in range(start, min(end, len(content)) + 1):
            lines.append(f"{number}: {content[number - 1]}")
        lines.append("")

    Path(output).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(lines)} lines)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
