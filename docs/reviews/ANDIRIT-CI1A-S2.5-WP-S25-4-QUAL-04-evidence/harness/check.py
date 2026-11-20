#!/usr/bin/env python3
"""QUAL-04 independent receipt checker.

Compares harness receipts (observed values only) against the pre-registered
reviewer expectations (expected.json). This checker is independent of the
harness: it never re-derives observations, it only asserts.

Usage: python check.py <receipts-dir> [--expect expected.json] [-v]
Exit code 0 only when every registered assertion passes.
"""
import json
import sys
from pathlib import Path

FAILURES = []
PASSES = []


def load(path):
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def check(case, name, actual, expected):
    if actual == expected:
        PASSES.append(f"{case}: {name} = {json.dumps(actual)}")
    else:
        FAILURES.append(f"{case}: {name} expected {json.dumps(expected)}, observed {json.dumps(actual)}")


def q(receipt):
    query = receipt.get("query")
    if query is None:
        raise KeyError("receipt has no query result")
    return query


def normalize(stem):
    # Receipt filenames carry the harness-build tag ("r2-", "r3-", ...) so one
    # receipts directory can hold evidence from several pinned revisions. The
    # pre-registered expectations are tag-relative, so strip a leading tag.
    head, sep, tail = stem.partition("-")
    return tail if sep and head else stem


def cov(receipt):
    query = q(receipt)
    coverage = query.get("coverage")
    return coverage or {}


def verify_membership(case, receipt, expectation, harness_fixture):
    coverage = cov(receipt)
    query = q(receipt)
    check(case, "availability", query["availability"], expectation["availability"])
    check(case, "isComplete", query["isComplete"], expectation["complete"])
    check(case, "declared", coverage.get("declaredUnitCount"), expectation["declared"])
    check(case, "verified", coverage.get("verifiedUnitCount"), expectation["verified"])
    check(case, "invocationFacts", coverage.get("invocationFacts"), expectation["facts"])
    check(case, "occurrences", len(query["occurrences"]), expectation["targetHits"])
    for key in ("unresolved", "external", "ambiguous", "unsupported"):
        if key in expectation:
            # Receipt coverage fields are camelCase (harness DescribeResult).
            field = {"unresolved": "unresolved", "external": "external",
                     "ambiguous": "ambiguous", "unsupported": "unsupported"}[key]
            check(case, key, coverage.get(field), expectation[key])
    if "rowDeclared" in expectation and "rows" in expectation:
        native = receipt["native"]
        units = native["units"]
        check(case, "nativeUnitCount", len(units), expectation["rows"])
        if "rowDeclared" in expectation and expectation.get("perRowFacts") is None:
            check(case, "nativeDeclaredSum", sum(u["coverage"]["declared"] for u in units), expectation["declared"])
            check(case, "nativeVerifiedSum", sum(u["coverage"]["verified"] for u in units), expectation["verified"])
        if "perRowFacts" in expectation:
            check(case, "nativeFactTotal", sum(u["facts"] for u in units), expectation["facts"])
            check(case, "nativeMetaFactCount", native["factCount"], [expectation["facts"]])
    # Complete fixtures must publish with authority envelopes; partial must not.
    complete = expectation["availability"] == "Available"
    for unit in receipt["native"]["units"]:
        if complete:
            if unit["authoritySourceUnit"] is None:
                FAILURES.append(f"{case}: complete unit {unit['indexUnitId']} has no authority envelope")
            elif unit["authoritySourceUnit"] != unit["indexUnitId"]:
                FAILURES.append(f"{case}: unit {unit['indexUnitId']} envelope SourceUnitId={unit['authoritySourceUnit']}")
            else:
                PASSES.append(f"{case}: {unit['indexUnitId']} self-owned envelope")
            if unit["coverage"]["declared"] != 1 or unit["coverage"]["verified"] != 1:
                FAILURES.append(f"{case}: complete unit {unit['indexUnitId']} row membership "
                                f"{unit['coverage']['declared']}/{unit['coverage']['verified']} != 1/1")
            else:
                PASSES.append(f"{case}: {unit['indexUnitId']} disjoint 1/1 row")
        else:
            if unit["authoritySourceUnit"] is not None:
                FAILURES.append(f"{case}: partial unit {unit['indexUnitId']} unexpectedly carries an envelope")
            else:
                PASSES.append(f"{case}: {unit['indexUnitId']} partial row without envelope")
            if unit["coverage"]["facts"] != 0:
                FAILURES.append(f"{case}: partial unit {unit['indexUnitId']} carries facts")
            else:
                PASSES.append(f"{case}: {unit['indexUnitId']} zero-fact partial row")
        if "perRowFacts" in expectation:
            pass  # checked through nativeFactTotal


def verify_adversarial(case, receipt, expectation):
    coverage = cov(receipt)
    query = q(receipt)
    check(case, "availability", query["availability"], expectation["availability"])
    check(case, "isComplete", query["isComplete"], expectation["complete"])
    check(case, "declared", coverage.get("declaredUnitCount"), expectation["declared"])
    check(case, "verified", coverage.get("verifiedUnitCount"), expectation["verified"])
    check(case, "invocationFacts", coverage.get("invocationFacts"), expectation["facts"])
    if "perRowFacts" in expectation:
        hits = receipt.get("hitsBySourceUnit", {})
        check(case, "hitsBySourceUnitCount", sum(hits.values()), expectation["targetHits"])
        native = receipt["native"]
        units = sorted(native["units"], key=lambda u: u["indexUnitId"])
        check(case, "perRowFacts", [u["facts"] for u in units], expectation["perRowFacts"])
    elif "targetHits" in expectation:
        check(case, "occurrences", len(query["occurrences"]), expectation["targetHits"])
    if case == "A2":
        check(case, "distinctFactIds", receipt.get("distinctFactIds"), expectation["facts"])
    if case == "A3":
        check(case, "targetStableAcrossOrdering", receipt.get("targetStable"), True)
    for unit in receipt["native"]["units"]:
        if unit["coverage"]["declared"] != 1 or unit["coverage"]["verified"] != 1:
            FAILURES.append(f"{case}: unit {unit['indexUnitId']} row membership "
                            f"{unit['coverage']['declared']}/{unit['coverage']['verified']} != 1/1")


def verify_stale(case, receipt):
    for name in ("stalePublication", "staleBoundAuthority", "authorityMismatch"):
        if name in receipt:
            observed = receipt[name]
            check(case, name + ".availability", observed["availability"], "StalePublication")
            check(case, name + ".isComplete", observed["isComplete"], False)
            check(case, name + ".occurrences", len(observed["occurrences"]), 0)


def verify_legacy(case, receipt, expectation):
    if "storedUnitCount" in expectation:
        check(case, "storedUnitCount", len(receipt["native"]["units"]), expectation["storedUnitCount"])
        raw = [u["coverage"]["verified"] for u in receipt["native"]["units"]]
        check(case, "rawRowVerified", raw, [expectation["rawRowVerified"]] * expectation["storedUnitCount"])
    query = q(receipt)
    coverage = cov(receipt)
    check(case, "availability", query["availability"], expectation["availability"])
    check(case, "isComplete", query["isComplete"], expectation["complete"])
    if "declared" in expectation:
        check(case, "declared", coverage.get("declaredUnitCount"), expectation["declared"])
    if "verified" in expectation:
        check(case, "verified", coverage.get("verifiedUnitCount"), expectation["verified"])
    if "facts" in expectation:
        check(case, "invocationFacts", coverage.get("invocationFacts", 0) or 0, expectation["facts"])
    if case == "X1c":
        # The refusal must expose no membership and no facts.
        check(case, "coverageAbsent", coverage, {})


def verify_preservation(case, receipt, expectation):
    query = q(receipt)
    coverage = cov(receipt)
    check(case, "availability", query["availability"], expectation["availability"])
    check(case, "isComplete", query["isComplete"], expectation["complete"])
    if "facts" in expectation:
        # A refused availability carries no coverage block at all; that is zero
        # produced invocation facts, not a missing observation.
        check(case, "invocationFacts", coverage.get("invocationFacts", 0) or 0, expectation["facts"])
    if "external" in expectation:
        check(case, "external", coverage.get("external"), expectation["external"])
        check(case, "unresolved", coverage.get("unresolved"), expectation["unresolved"])
        check(case, "ambiguous", coverage.get("ambiguous"), expectation["ambiguous"])
        check(case, "unsupported", coverage.get("unsupported"), expectation["unsupported"])
        check(case, "unsupportedCategories", coverage.get("unsupportedCategories"), expectation["unsupportedCategories"])
        check(case, "externalOccurrenceFacts", receipt.get("externalOccurrenceFacts"), expectation["externalOccurrences"])
    if "declared" in expectation:
        check(case, "declared", coverage.get("declaredUnitCount"), expectation["declared"])
        check(case, "verified", coverage.get("verifiedUnitCount"), expectation["verified"])
    if case == "F254-01":
        check(case, "identityMatch", receipt.get("identityMatch"), True)
        check(case, "expectedIdCount", receipt.get("expectedIdCount"), 50)
        check(case, "nativeFactCount", receipt.get("nativeFactCount"), 50)
        check(case, "callerOwnershipMatch", receipt.get("callerOwnershipMatch"), True)
        check(case, "targetMatch", receipt.get("targetMatch"), True)
        check(case, "declarationSpans", receipt.get("declarationSpans"),
              [receipt.get("expectedDeclarationSpan")])
        check(case, "nativeDeclarationSpans", receipt.get("nativeDeclarationSpans"),
              [receipt.get("expectedDeclarationSpan")])
        caller = receipt.get("callerProjectUnitId")
        dependency = receipt.get("dependencyProjectUnitId")
        for unit in receipt["native"]["units"]:
            expected = 50 if unit["indexUnitId"] == caller else 0 if unit["indexUnitId"] == dependency else None
            if expected is None:
                FAILURES.append(f"{case}: native row {unit['indexUnitId']} belongs to neither project")
            else:
                check(case, "nativeRowFacts:" + unit["indexUnitId"][-12:], unit["facts"], expected)
    if case == "F254-02":
        check(case, "unsupported", coverage.get("unsupported"), expectation["unsupported"])
        check(case, "unsupportedCategories", coverage.get("unsupportedCategories"), expectation["unsupportedCategories"])
        use = receipt.get("useProjectUnitId")
        dep = receipt.get("depProjectUnitId")
        for unit in receipt["native"]["units"]:
            expected = 3 if unit["indexUnitId"] == use else 0 if unit["indexUnitId"] == dep else None
            if expected is None:
                FAILURES.append(f"{case}: native row {unit['indexUnitId']} belongs to neither project")
                continue
            check(case, "unsupportedRow:" + unit["indexUnitId"][-8:], unit["coverage"]["unsupported"], expected)
            check(case, "unsupportedCategoriesRow:" + unit["indexUnitId"][-8:],
                  unit["coverage"]["unsupportedCategories"], expected)
    if case == "F254-03-none":
        check(case, "universeComplete", receipt.get("universeComplete"), False)
        check(case, "verifiedEntries", receipt.get("verifiedEntries"), 0)


def main():
    args = sys.argv[1:]
    verbose = "-v" in args
    if "-v" in args:
        args.remove("-v")
    receipts_dir = Path(args[0])
    expected_path = Path(args[1]) if len(args) > 1 else receipts_dir / "expected.json"
    expected = load(expected_path)
    membership = expected["membership"]
    legacy = expected["legacy"]

    receipts = {}
    raw = {}
    for path in sorted(receipts_dir.glob("*.json")):
        if path.name == "expected.json":
            continue
        data = load(path)
        raw[path.stem] = data
        receipts[normalize(path.stem)] = data

    for case, expectation in membership.items():
        # Lifecycle generations are checked by the dedicated sequence loop below.
        if case.startswith("L"):
            continue
        for suffix in ("publish", "verify"):
            key = f"{case}-{suffix}"
            if key not in receipts:
                FAILURES.append(f"{case}: missing receipt {key}")
                continue
            receipt = receipts[key]
            if case.startswith("F254") or case == "b3":
                verify_preservation(case, receipt, expectation)
            elif case.startswith("L"):
                # lifecycle generations are checked separately below
                continue
            else:
                verify_membership(case, receipt, expectation, receipt.get("fixture"))
                verify_stale(case, receipt)
        if case.startswith("A"):
            verify_stale(case, receipts.get(f"{case}-publish", {}))

    # Lifecycle: the required membership sequence 2/2 -> 3/2 -> 2/2 -> 2/2.
    sequence = [("L1", "first"), ("L2", "partial"), ("L3", "restored"), ("L4", "repeat")]
    for case_key, generation in sequence:
        expectation = membership[case_key]
        for phase in ("publish", "verify"):
            key = f"lifecycle-{phase}-{generation}"
            if key not in receipts:
                FAILURES.append(f"lifecycle: missing receipt {key}")
                continue
            receipt = receipts[key]
            verify_membership(f"lifecycle-{generation}-{phase}", receipt, expectation, None)
            if generation != "first":
                stale = receipt.get("staleFirstHandle")
                if stale is None:
                    FAILURES.append(f"lifecycle-{generation}-{phase}: missing staleFirstHandle")
                else:
                    check(f"lifecycle-{generation}-{phase}", "staleFirstHandle.availability",
                          stale["availability"], "StalePublication")
                    check(f"lifecycle-{generation}-{phase}", "staleFirstHandle.occurrences",
                          len(stale["occurrences"]), 0)

    legacy_keys = {
        "X1b": ["r2-X1-verify", "r2-X1-publish"],
        "X1c": ["r3-X1-verify"],
    }
    for case, expectation in legacy.items():
        keys = [key for key in (f"{case}-verify", f"{case}-publish", case) if key in receipts]
        keys += [key for key in legacy_keys.get(case, []) if key in receipts or key in raw]
        keys = [receipts[k] if k in receipts else raw[k] for k in keys]
        if not keys:
            FAILURES.append(f"{case}: missing receipt")
            continue
        for key in keys:
            verify_legacy(case, key, expectation)

    print(f"passed assertions: {len(PASSES)}")
    print(f"failed assertions: {len(FAILURES)}")
    for failure in FAILURES:
        print("FAIL " + failure)
    if verbose:
        for item in PASSES:
            print("ok   " + item)
    return 1 if FAILURES else 0


if __name__ == "__main__":
    sys.exit(main())