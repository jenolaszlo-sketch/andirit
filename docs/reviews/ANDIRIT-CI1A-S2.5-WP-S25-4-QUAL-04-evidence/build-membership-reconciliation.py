#!/usr/bin/env python3
"""Build the QUAL-04 R2/R3 membership and legacy-compatibility reconciliation."""
from __future__ import annotations

import json
from pathlib import Path

HERE = Path(__file__).resolve().parent


def load(relative: str) -> dict:
    return json.loads((HERE / relative).read_text(encoding="utf-8"))


def observation(relative: str) -> dict:
    receipt = load(relative)
    query = receipt.get("query") or {}
    coverage = query.get("coverage") or {}
    native = receipt.get("native") or {}
    rows = []
    for unit in native.get("units") or []:
        rows.append({
            "indexUnitId": unit.get("indexUnitId"),
            "rowCoverage": unit.get("coverage"),
            "authoritySourceUnit": unit.get("authoritySourceUnit"),
            "authorityCoverage": unit.get("authorityCoverage"),
            "authorityMembers": unit.get("authorityMembers"),
            "facts": unit.get("facts"),
        })
    return {
        "receipt": relative,
        "case": receipt.get("case"),
        "availability": query.get("availability"),
        "isComplete": query.get("isComplete"),
        "declared": coverage.get("declaredUnitCount"),
        "verified": coverage.get("verifiedUnitCount"),
        "invocationFacts": coverage.get("invocationFacts"),
        "targetHits": len(query.get("occurrences") or []),
        "rows": rows,
        "failures": receipt.get("failures") or [],
    }


def grouped(prefix: str, cases: list[str], directory: str, tag: str) -> dict:
    result = {}
    for case in cases:
        result[case] = {
            "publish": observation(f"{directory}/{tag}-{case}-publish.json"),
            "verify": observation(f"{directory}/{tag}-{case}-verify.json"),
        }
    return result


def lifecycle(directory: str, tag: str) -> dict:
    result = {}
    for generation in ("first", "partial", "restored", "repeat"):
        result[generation] = {
            "publish": observation(
                f"{directory}/{tag}-lifecycle-publish-{generation}.json"
            ),
            "verify": observation(
                f"{directory}/{tag}-lifecycle-verify-{generation}.json"
            ),
        }
    return result


def pair(actual: dict, expected: tuple[object, ...]) -> bool:
    return (
        actual["declared"], actual["verified"], actual["targetHits"]
    ) == expected


membership_cases = ["M1", "M2", "M3", "M4", "M5", "M6", "M7", "M8"]
legacy_cases = ["X2", "X3", "X4a", "X4b"]
r2 = grouped("r2", membership_cases, "receipts-r2", "r2")
r3 = grouped("r3", membership_cases, "receipts-r3", "r3")
r2_legacy = grouped("r2", legacy_cases, "receipts-r2", "r2")
r3_legacy = grouped("r3", legacy_cases, "receipts-r3", "r3")
n1 = grouped("n1", ["M2", "M3", "M4"], "receipts-n1", "n1")
n1a = grouped("n1a", ["M2", "M3", "M4"], "receipts-N1a", "N1a")
n2 = grouped("n2", ["X2"], "receipts-n2", "n2")

r2_lifecycle = lifecycle("receipts-r2", "r2")
r3_lifecycle = lifecycle("receipts-r3", "r3")
r2_x1 = {
    "publish": observation("receipts-r3/r2-X1-publish.json"),
    "same_binary_verify": observation("receipts-r3/r2-X1-verify.json"),
}
r3_x1 = {
    "publish": observation("receipts-r3/r3-X1-publish.json"),
    "cross_revision_verify": observation("receipts-r3/r3-X1-verify.json"),
}

checks = {
    "r2_reproduces_complete_defect": all([
        pair(r2["M2"]["publish"], (4, 4, 1)),
        pair(r2["M2"]["verify"], (4, 4, 1)),
        pair(r2["M4"]["publish"], (4, 4, 0)),
        pair(r2["M4"]["verify"], (4, 4, 0)),
        pair(r2["M3"]["publish"], (9, 9, 1)),
        pair(r2["M3"]["verify"], (9, 9, 1)),
    ]),
    "r3_complete_membership_correct": all([
        pair(r3["M2"]["publish"], (2, 2, 1)),
        pair(r3["M2"]["verify"], (2, 2, 1)),
        pair(r3["M4"]["publish"], (2, 2, 0)),
        pair(r3["M4"]["verify"], (2, 2, 0)),
        pair(r3["M3"]["publish"], (3, 3, 1)),
        pair(r3["M3"]["verify"], (3, 3, 1)),
    ]),
    "r3_partial_membership_preserved": all([
        pair(r3["M6"]["publish"], (3, 2, 0)),
        pair(r3["M7"]["publish"], (4, 2, 0)),
        pair(r3["M8"]["publish"], (4, 3, 0)),
    ]),
    "r3_lifecycle_correct": all([
        pair(r3_lifecycle["first"]["publish"], (2, 2, 1)),
        pair(r3_lifecycle["partial"]["publish"], (3, 2, 0)),
        pair(r3_lifecycle["restored"]["publish"], (2, 2, 1)),
        pair(r3_lifecycle["repeat"]["publish"], (2, 2, 1)),
    ]),
    "matching_authority_legacy_compatible": all([
        pair(r3_legacy["X2"]["publish"], (3, 3, 0)),
        pair(r3_legacy["X2"]["verify"], (3, 3, 0)),
    ]),
    "cross_revision_r2_store_refused": (
        r3_x1["cross_revision_verify"]["availability"] == "StalePublication"
        and r3_x1["cross_revision_verify"]["targetHits"] == 0
    ),
    "same_binary_r2_store_retains_historical_inflation": all([
        pair(r2_x1["publish"], (9, 9, 1)),
        pair(r2_x1["same_binary_verify"], (9, 9, 1)),
    ]),
    "n1_combined_detected": all([
        pair(n1["M2"]["publish"], (4, 4, 1)),
        pair(n1["M3"]["publish"], (9, 9, 1)),
        pair(n1["M4"]["publish"], (4, 4, 0)),
    ]),
    "n1a_supplemental_diagnostic": all([
        pair(n1a["M2"]["publish"], (2, 2, 1)),
        pair(n1a["M3"]["publish"], (3, 3, 1)),
        pair(n1a["M4"]["publish"], (2, 2, 0)),
    ]),
    "n2_detected": pair(n2["X2"]["publish"], (9, 9, 0)),
}

result = {
    "purpose": "independent complete-scope membership accounting and compatibility reconciliation",
    "r2_negative_baseline": {"membership": r2, "legacy": r2_legacy, "lifecycle": r2_lifecycle, "x1": r2_x1},
    "r3_candidate": {"membership": r3, "legacy": r3_legacy, "lifecycle": r3_lifecycle, "x1": r3_x1},
    "mutation_controls": {"n1_combined": n1, "n1a_supplemental": n1a, "n2": n2},
    "checks": checks,
    "accepted": all(checks.values()),
}
(HERE / "membership-reconciliation.json").write_text(
    json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n"
)
print(json.dumps({"checks": checks, "accepted": result["accepted"]}, indent=2))
raise SystemExit(0 if result["accepted"] else 1)
