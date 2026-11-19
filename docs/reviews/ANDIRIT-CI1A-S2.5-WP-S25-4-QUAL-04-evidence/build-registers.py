#!/usr/bin/env python3
"""Build the QUAL-04 64-row matrix and 78-entry obligation register.

The QUAL-03 registers are the historical baseline. This script preserves every
row, adds an explicit QUAL-04 classification, applies only the dispositions
recorded by the independent QUAL-04 review, and asserts row/count integrity.
"""
from __future__ import annotations

import json
import re
from collections import Counter
from pathlib import Path

HERE = Path(__file__).resolve().parent
REVIEWS = HERE.parent
QUAL03 = REVIEWS / "CI1A-S2.5-WP-S25-4-QUAL-03-evidence"
MATRIX_SOURCE = QUAL03 / "matrix.md"
OBLIGATIONS_SOURCE = QUAL03 / "obligations.md"


def table(lines: list[str]) -> tuple[list[str], list[list[str]], int]:
    header_at = next(i for i, line in enumerate(lines) if line.startswith("| "))
    header = [cell.strip() for cell in lines[header_at].strip().strip("|").split("|")]
    rows: list[list[str]] = []
    for line in lines[header_at + 2 :]:
        if not line.startswith("| "):
            break
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) != len(header):
            raise ValueError(f"row has {len(cells)} cells, expected {len(header)}: {line}")
        rows.append(cells)
    return header, rows, header_at


def replace_cell(row: list[str], index: int, value: str) -> None:
    row[index] = value


def build_matrix() -> str:
    lines = MATRIX_SOURCE.read_text(encoding="utf-8").splitlines()
    header, rows, header_at = table(lines)
    expected_header = [
        "Row",
        "Governing requirement",
        "QUAL-02",
        "Current production impact",
        "Fresh or reused evidence",
        "QUAL-03",
        "Residual limitations",
        "Owner action",
    ]
    if header != expected_header:
        raise ValueError(f"unexpected QUAL-03 matrix header: {header}")

    for row in rows:
        row_id = row[0]
        row.insert(6, row[5])  # QUAL-04 defaults to the QUAL-03 classification.
        if row_id in {"M5.1", "M5.8"}:
            row[3] = (
                "R3 replaces the repeated whole-universe complete-row contribution with "
                "the source-owning project's disjoint 1/1 contribution while retaining the "
                "full verified-universe authority envelope. The R2 duplicate is absent."
            )
            row[4] = (
                "Fresh QUAL-04 independent controls reproduce R2 at 4/4 and 9/9, then show "
                "R3 at 2/2 and 3/3 for publish and fresh-process reopen. Every complete row "
                "persists disjoint 1/1 with the full-universe envelope; complete-empty remains "
                "Available with zero facts."
            )
            row[6] = "PASS"
            row[7] = (
                "Q03-F-01 is technically qualified on pinned Windows R3. M5.4 continuity, "
                "genuine successor Ubuntu, exact-byte formatting, and F-G2-01 remain separate."
            )
            row[8] = (
                "Accept the bounded technical qualification for M5.1/M5.8; owner acceptance "
                "remains a separate act."
            )
        elif "Q03-F-01" in row[7]:
            row[7] = row[7].replace(
                "unrelated Q03-F-01 complete membership issue assessed at M5.1/M5.8.",
                "Q03-F-01 is independently qualified on pinned Windows R3.",
            )
            row[7] = row[7].replace(
                "complete-path membership discrepancy is separately Q03-F-01, not a missing-unit failure.",
                "Q03-F-01 is independently qualified on pinned Windows R3; this row remains a "
                "bounded missing-unit correction.",
            )

    # Inherited links point into the immutable QUAL-03 evidence package.
    historical_prefix = "../CI1A-S2.5-WP-S25-4-QUAL-03-evidence/"
    for row in rows:
        for index, cell in enumerate(row):
            row[index] = re.sub(
                r"\]\((?!https?:|#|\.\./)([^)#]+)(#[^)]*)?\)",
                lambda match: "](" + historical_prefix + match.group(1) +
                (match.group(2) or "") + ")",
                cell,
            )

    matrix_header = header[:6] + ["QUAL-04"] + header[6:]
    output = [
        "# QUAL-04 complete 64-row reconciliation",
        "",
        "Reviewed production: 8b83e0558f4c757272cd60a1c81b8dbc6fd7873f "
        "(tree 0f89a469858abd2126f3be01cd060bc534e930c0). QUAL-03 is the historical "
        "64-row baseline. QUAL-04 independently requalifies Q03-F-01 on M5.1/M5.8; all "
        "other classifications are carried only after the unchanged premise and R3 regression "
        "were checked. Reused evidence remains independently established QUAL-02/QUAL-03 "
        "evidence, never producer execution. M5.4, successor Ubuntu M11.4/M14.1, "
        "exact-byte formatting, and F-G2-01 remain outstanding.",
        "",
        "| " + " | ".join(matrix_header) + " |",
        "| " + " | ".join("---" for _ in matrix_header) + " |",
    ]
    output.extend("| " + " | ".join(row) + " |" for row in rows)

    counts = Counter(row[6] for row in rows)
    expected_counts = {
        "PASS": 46,
        "PASS WITH QUALIFICATION": 10,
        "FAIL": 0,
        "BLOCKED": 3,
        "REUSED QUALIFIED": 1,
        "DEFERRED": 4,
    }
    if len(rows) != 64 or len({row[0] for row in rows}) != 64:
        raise ValueError("QUAL-04 matrix must contain 64 unique row IDs")
    if counts != Counter(expected_counts):
        raise ValueError(f"QUAL-04 matrix counts differ: {dict(counts)}")
    output.extend(
        [
            "",
            "Counts: " + json.dumps(expected_counts, sort_keys=True) +
            ". Total 64, unique IDs 64.",
            "",
            "## QUAL-04 membership delta",
            "",
            "The pinned R2 baseline is a negative control, not candidate evidence. It reproduces "
            "the pre-existing defect on both publication and fresh-process reopen: two complete "
            "members report 4/4 and three report 9/9. R3 reports 2/2 and 3/3, with each "
            "complete source contribution persisted as a disjoint 1/1 row and the aggregate "
            "authority still bound to the full verified universe. The lifecycle sequence is "
            "2/2 -> 3/2 -> 2/2 -> 2/2. See [the reviewer checks](checks/check-r3.txt), "
            "[R3 receipts](receipts-r3/), [R2 receipts](receipts-r2/), and "
            "[mutation controls](mutations/).",
            "",
            "A matching-authority historical row (one self-owned row per verified unit, each "
            "carrying the full-universe counts) remains readable and projects to disjoint 1/1 "
            "at restore; its aggregate remains N/N. Hand-tampered historical envelopes and row "
            "removal remain visible as an inadmissible-history boundary, not a defect in R3's "
            "legacy projection.",
        ]
    )
    return "\n".join(output) + "\n"


def build_obligations() -> str:
    lines = OBLIGATIONS_SOURCE.read_text(encoding="utf-8").splitlines()
    header, rows, header_at = table(lines)
    expected_header = [
        "Obligation",
        "Governing source",
        "QUAL-02",
        "QUAL-03",
        "Current evidence and impact",
        "Inherited limitation",
        "Owner action",
        "Owner",
    ]
    if header != expected_header:
        raise ValueError(f"unexpected QUAL-03 obligation header: {header}")

    qualified_membership = {
        "F03",
        "F04",
        "NEW-04",
        "IF-03",
    }
    blocked_acceptance = {
        "F06",
        "F16",
        "I-01",
        "Required evidence, fixture, runner or independence gap",
    }
    for row in rows:
        obligation = row[0]
        row.insert(4, row[3])  # QUAL-04 defaults to the QUAL-03 classification.
        if obligation in qualified_membership:
            row[5] = (
                "QUAL-04 independently reproduces the R2 defect (4/4 and 9/9), then qualifies "
                "R3's disjoint complete-row accounting (2/2 and 3/3) through publication and "
                "fresh-process reopen. R1/R2 corrections remain preserved; M5.4 remains separate."
            )
            row[6] = (
                "Retain the bounded technical qualification. Counts remain structural claims, "
                "not extractor-completeness proof; M5.4 continuity remains unresolved."
            )
            row[7] = (
                "Record technical qualification; resolve M5.4 separately. No owner acceptance "
                "is inferred by this review."
            )
            row[4] = "QUALIFIED"
        elif obligation in blocked_acceptance:
            row[5] = (
                "Independence/integrity gates passed and Q03-F-01 is independently qualified "
                "on pinned Windows R3. M5.4, corrected genuine Ubuntu, exact-Git-byte formatter "
                "exit 2 versus checkout-profile exit 0, and coverage/owner prerequisites still "
                "prevent acceptance."
            )
            row[7] = "Resolve named blockers and obtain separate owner acceptance; no qualification bypass."
        elif obligation == "F-G2-01":
            row[5] = (
                "Independent pinned measurements: R2 296/450 = 65.77% and R3 305/466 = 65.45%, "
                "both below the unchanged 70% branch threshold. The policy gate exits 1 on both "
                "revisions; no threshold, filter, or CI change was made."
            )
            row[7] = "Separate bounded test-only coverage commission and successor measurement; repository CI is not fully green."

    obligations_header = header[:4] + ["QUAL-04"] + header[4:]
    output = [
        "# QUAL-04 complete 78-entry obligation reconciliation",
        "",
        "Historical entry and QUAL-02/QUAL-03 classifications remain preserved in their "
        "original records. The six governing classification names are retained. QUAL-04 "
        "adds one explicit column and qualifies only F03, F04, NEW-04, and IF-03 on the "
        "independently demonstrated complete-scope accounting correction. Counts refer to "
        "qualification routing, not new owner closure. Finding overlays follow the table and "
        "do not create duplicate inventory entries.",
        "",
        "| " + " | ".join(obligations_header) + " |",
        "| " + " | ".join("---" for _ in obligations_header) + " |",
    ]
    output.extend("| " + " | ".join(row) + " |" for row in rows)

    counts = Counter(row[4] for row in rows)
    expected_counts = {
        "AMENDMENT-SUBJECT": 2,
        "BLOCKED": 4,
        "DISCHARGED": 11,
        "OPEN-S2.5": 1,
        "OPEN-STAGE6": 7,
        "QUALIFIED": 53,
    }
    if len(rows) != 78 or len({row[0] for row in rows}) != 78:
        raise ValueError("QUAL-04 obligations must contain 78 unique entries")
    if counts != Counter(expected_counts):
        raise ValueError(f"QUAL-04 obligation counts differ: {dict(counts)}")
    output.extend(
        [
            "",
            "Counts: " + json.dumps(expected_counts, sort_keys=True) +
            ". Total 78, unique entries 78.",
            "",
            "Finding overlays: F-S254-01 preservation independently verified, with no formal "
            "closure. F-S254-02 and F-S254-03 remain SUPPORTED WITH QUALIFICATIONS for their "
            "bounded Windows corrections. Q03-F-01 is QUALIFIED at the technical level on "
            "pinned R3 M5.1/M5.8; this does not grant owner acceptance. M5.4 technical "
            "disposition D and governance BLOCKED remain. Corrected genuine-Linux M11.4/M14.1 "
            "remain BLOCKED. F-G2-01 remains OPEN. Exact Git-byte formatting remains unsatisfied "
            "and is not waived. All named Stage 6 routes remain separate.",
        ]
    )
    return "\n".join(output) + "\n"


def main() -> None:
    matrix = build_matrix()
    obligations = build_obligations()
    (HERE / "matrix.md").write_text(matrix, encoding="utf-8", newline="\n")
    (HERE / "obligations.md").write_text(obligations, encoding="utf-8", newline="\n")
    print("QUAL-04 matrix: 64 unique rows")
    print("QUAL-04 obligations: 78 unique entries")


if __name__ == "__main__":
    main()
