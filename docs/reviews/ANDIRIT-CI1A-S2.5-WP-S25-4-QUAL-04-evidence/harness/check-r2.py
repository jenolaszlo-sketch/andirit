#!/usr/bin/env python3
"""QUAL-04 baseline-defect checker.

Asserts that the pinned R2 export (25bc6e3b) reproduces the historical
complete-scope membership defect with exactly the reviewer-derived values
registered in expected.json's "negative" section, and that the same fixtures
answer correctly on the pinned R3 export. Both sides are observed through the
same reviewer harness, so the only variable is the production revision.

Usage: python check-r2.py <r2-receipts> <r3-receipts> <expected.json>
Exit code 0 only when every registered baseline expectation holds.
"""
import json
import sys
from pathlib import Path

FAIL = []
PASS = []


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def observe(receipts, tag, case, phase="verify"):
    data = load(Path(receipts) / f"{tag}-{case}-{phase}.json")
    query = data["query"]
    coverage = query.get("coverage") or {}
    rows = (data.get("native") or {}).get("units") or []
    return {
        "availability": query["availability"],
        "complete": query["isComplete"],
        "declared": coverage.get("declaredUnitCount"),
        "verified": coverage.get("verifiedUnitCount"),
        "facts": coverage.get("invocationFacts"),
        "hits": len(query["occurrences"]),
        "rows": len(rows),
        "rowCov": sorted((r["coverage"]["declared"], r["coverage"]["verified"]) for r in rows),
        "env": sorted({(r.get("authoritySourceUnit") or "")[:0] + str(r.get("authorityCoverage"))
                       for r in rows}) if rows else [],
    }


def check(case, name, actual, expected):
    if actual == expected:
        PASS.append(f"{case}: {name} = {json.dumps(actual)}")
    else:
        FAIL.append(f"{case}: {name} expected {json.dumps(expected)}, observed {json.dumps(actual)}")


def main():
    r2_dir, r3_dir, expected_path = sys.argv[1], sys.argv[2], sys.argv[3]
    expected = load(expected_path)["negative"]

    # N1: the historical defect (complete rows carry full-universe membership).
    for case, values in expected["N1"]["expectedDetection"].items():
        for phase in ("publish", "verify"):
            seen = observe(r2_dir, "r2", case, phase)
            check(f"r2/{case}/{phase}", "declared", seen["declared"], values["declared"])
            check(f"r2/{case}/{phase}", "verified", seen["verified"], values["verified"])
        # The same fixture on R3 must answer the true complete-scope membership:
        # an N-unit complete scope reports exactly N declared / N verified.
        r3_expected = {"M2": 2, "M4": 2, "M3": 3}[case]
        fixed = observe(r3_dir, "r3", case, "verify")
        check(f"r3/{case}/verify", "declared", fixed["declared"], r3_expected)
        check(f"r3/{case}/verify", "verified", fixed["verified"], r3_expected)

    # The one complete single-unit case is not inflated at either revision.
    for tag, d in (("r2", r2_dir), ("r3", r3_dir)):
        seen = observe(d, tag, "M1", "verify")
        check(f"{tag}/M1/verify", "declared", seen["declared"], 1)
        check(f"{tag}/M1/verify", "verified", seen["verified"], 1)

    # Partial analysis membership is identical at both revisions (preserved).
    for case, declared, verified in (("M6", 3, 2), ("M7", 4, 2), ("M8", 4, 3)):
        for tag, d in (("r2", r2_dir), ("r3", r3_dir)):
            seen = observe(d, tag, case, "verify")
            check(f"{tag}/{case}/verify", "declared", seen["declared"], declared)
            check(f"{tag}/{case}/verify", "verified", seen["verified"], verified)
            check(f"{tag}/{case}/verify", "complete", seen["complete"], False)

    # Complete-empty and multi-hit adversarial scopes.
    for tag, d, m4, a1, a3, a4 in (("r2", r2_dir, 4, 4, 9, 9), ("r3", r3_dir, 2, 2, 3, 3)):
        for case, value in (("M4", m4), ("M5", m4), ("A1", a1), ("A3", a3), ("A4", a4)):
            seen = observe(d, tag, case, "verify")
            check(f"{tag}/{case}/verify", "declared", seen["declared"], value)
            check(f"{tag}/{case}/verify", "verified", seen["verified"], value)

    # Genuine R2-produced store reopened by the same R2 binaries (X1b).
    seen = observe(r2_dir, "r2", "X1", "verify")
    check("r2/X1/verify", "declared", seen["declared"], 9)
    check("r2/X1/verify", "verified", seen["verified"], 9)
    check("r2/X1/verify", "rowCov", seen["rowCov"], [(3, 3), (3, 3), (3, 3)])

    # The 50-site preservation corpus: membership was inflated at R2.
    for tag, d, value in (("r2", r2_dir, 4), ("r3", r3_dir, 2)):
        seen = observe(d, tag, "F254-01", "verify")
        check(f"{tag}/F254-01/verify", "declared", seen["declared"], value)
        check(f"{tag}/F254-01/verify", "verified", seen["verified"], value)
        check(f"{tag}/F254-01/verify", "hits", seen["hits"], 50)

    # Lifecycle membership sequence on one durable store.
    for tag, d, sequence in (("r2", r2_dir, [4, 3, 4, 4]), ("r3", r3_dir, [2, 3, 2, 2])):
        observed = []
        for generation in ("first", "partial", "restored", "repeat"):
            seen = observe(d, tag, "lifecycle", f"verify-{generation}")
            observed.append(seen["declared"])
        check(f"{tag}/lifecycle", "declaredSequence", observed, sequence)

    print(f"passed assertions: {len(PASS)}")
    print(f"failed assertions: {len(FAIL)}")
    for failure in FAIL:
        print("FAIL " + failure)
    return 1 if FAIL else 0


if __name__ == "__main__":
    sys.exit(main())