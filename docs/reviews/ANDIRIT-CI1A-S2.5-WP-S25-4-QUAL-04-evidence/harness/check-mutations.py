#!/usr/bin/env python3
"""QUAL-04 mutation negative-control checker.

Asserts that each bounded mutation of the pinned R3 production code is detected
with exactly the pre-registered detection values from expected.json's
"negative" section. A control that is NOT detected would invalidate the
qualification; a control detected with a different value falsifies the
registered expectation.

Usage: python check-mutations.py <n1-receipts> <n2-receipts> <expected.json>
"""
import json
import sys
from pathlib import Path

FAIL, PASS = [], []


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def observe(receipts, tag, case, phase):
    data = load(Path(receipts) / f"{tag}-{case}-{phase}.json")
    coverage = (data.get("query") or {}).get("coverage") or {}
    rows = (data.get("native") or {}).get("units") or []
    return {
        "declared": coverage.get("declaredUnitCount"),
        "verified": coverage.get("verifiedUnitCount"),
        "hits": len((data.get("query") or {}).get("occurrences") or []),
        "rowCov": sorted((r["coverage"]["declared"], r["coverage"]["verified"]) for r in rows),
    }


def check(name, actual, expected):
    if actual == expected:
        PASS.append(f"{name} = {json.dumps(actual)}")
    else:
        FAIL.append(f"{name} expected {json.dumps(expected)}, observed {json.dumps(actual)}")


def main():
    n1, n2, expected_path = sys.argv[1], sys.argv[2], sys.argv[3]
    r2_baseline = sys.argv[4] if len(sys.argv) > 4 else None
    expected = load(expected_path)["negative"]

    # N1: complete rows carry the full-universe membership again (R2 behavior).
    for case, values in expected["N1"]["expectedDetection"].items():
        for phase in ("publish", "verify"):
            seen = observe(n1, "n1", case, phase)
            check(f"N1/{case}/{phase}/declared", seen["declared"], values["declared"])
            check(f"N1/{case}/{phase}/verified", seen["verified"], values["verified"])
        # The mutation must also be visible in the persisted native rows: the
        # mutated row shape must equal the pinned R2 baseline's row shape.
        if len(sys.argv) > 4:
            baseline = observe(sys.argv[4], "r2", case, "verify")
            check(f"N1/{case}/verify/rowCov", seen["rowCov"], baseline["rowCov"])

    # N2: the restore projection is removed, so a matching-authority
    # historically shaped store aggregates every full-universe row again.
    for phase in ("publish", "verify"):
        seen = observe(n2, "n2", "X2", phase)
        for name, value in expected["N2"]["expectedDetection"]["X2"].items():
            check(f"N2/X2/{phase}/{name}", seen[name], value)

    print(f"passed assertions: {len(PASS)}")
    print(f"failed assertions: {len(FAIL)}")
    for failure in FAIL:
        print("FAIL " + failure)
    return 1 if FAIL else 0


if __name__ == "__main__":
    sys.exit(main())