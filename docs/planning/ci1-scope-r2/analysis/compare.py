"""Build the CI-1-SCOPE-R2 before/after mutation comparison.

Reads the preserved prior-checker output (evidence/before/before-run.json) and
the corrected-oracle output (evidence/after/after-run.json) and writes
evidence/mutation-comparison.json. Planning evidence only.
"""
import json
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
EVIDENCE = os.path.abspath(os.path.join(HERE, "..", "evidence"))

before = json.load(open(os.path.join(EVIDENCE, "before", "before-run.json"), encoding="utf-8"))
after = json.load(open(os.path.join(EVIDENCE, "after", "after-run.json"), encoding="utf-8"))

before_by_name = {m["Name"]: m for m in before["MutationChecks"]}
after_by_id = {m["id"]: m for m in after["mutations"]}

# Semantic case -> (prior checker mutation name, corrected oracle mutation id)
CASES = [
    ("External -> confirmed", "outcome-external-to-confirmed", "M01-external-to-confirmed"),
    ("Confirmed -> external", "outcome-confirmed-to-external", "M02-confirmed-to-external"),
    ("Confirmed -> deferred", "outcome-confirmed-to-deferred", "M03-confirmed-to-deferred"),
    ("Unrecognised outcome string", "outcome-invalid-enum", "M04-invalid-outcome"),
    ("Ambiguous -> confirmed", "outcome-ambiguous-to-confirmed", "M05-ambiguous-to-confirmed"),
    ("Unresolved -> confirmed", None, "M06-unresolved-to-confirmed"),
    ("Confirmed -> wrong overload", "wrong-overload", "M07-wrong-overload"),
    ("Confirmed -> wrong assembly", None, "M08-confirmed-to-wrong-assembly"),
    ("Confirmed -> unrelated indexed method", "wrong-target", "M09-confirmed-to-unrelated-indexed-method"),
    ("Correct target -> incorrect span", "wrong-coordinate", "M10-correct-target-incorrect-span"),
    ("Correct entry -> duplicate entry", "duplicate-row", "M11-duplicate-entry"),
    ("Correct expected outcome (positive control)", None, "P01-correct-outcome-accepted"),
    ("Wrong start line", "wrong-start-line", None),
    ("Wrong end line", "wrong-end-line", None),
    ("Wrong end column", "wrong-end-column", None),
    ("Semantic target mismatch (original name)", "wrong-target", None),
    ("Outcome consistency (original name)", "wrong-outcome", None),
]

rows = []
for label, bname, aid in CASES:
    b = before_by_name.get(bname) if bname else None
    a = after_by_id.get(aid) if aid else None
    rows.append({
        "semanticCase": label,
        "priorCheckerMutation": bname,
        "priorCheckerRejected": (b["Detected"] if b else None),
        "priorCheckerFailingRows": (b["FailingRows"] if b else None),
        "correctedOracleMutation": aid,
        "correctedOracleRejected": (a["detected"] if a else None),
        "correctedOracleExpectedReason": (a["expectedRejectionReason"] if a else None),
        "correctedOraclePrimaryReason": (a["primaryReasonCode"] if a else None),
        "correctedOracleRejectedForIntendedReason": (a["rejectedForIntendedReason"] if a else None),
        "correctedOracleUnrelatedFailedFirst": (a["unrelatedValidationFailedFirst"] if a else None),
    })

result = {
    "priorChecker": {
        "source": "evidence/before/before-run.json",
        "sites": before["Sites"].__len__(),
        "baselineFailures": before.get("CollisionActualTarget") is not None and sum(1 for s in before["Sites"] if s["Errors"]),
        "mutations": len(before["MutationChecks"]),
        "undetectedMutations": [m["Name"] for m in before["MutationChecks"] if not m["Detected"]],
    },
    "correctedOracle": {
        "source": "evidence/after/after-run.json",
        "sites": after["baseline"]["sites"],
        "baselineFailures": after["baseline"]["failures"],
        "mutations": len(after["mutations"]),
        "summary": after["summary"],
    },
    "comparison": rows,
}

out = os.path.join(EVIDENCE, "mutation-comparison.json")
json.dump(result, open(out, "w", encoding="utf-8"), indent=2)
print("wrote", out)
for r in rows:
    print("%-44s before=%-5s after=%-5s primary=%s" % (
        r["semanticCase"], r["priorCheckerRejected"], r["correctedOracleRejected"], r["correctedOraclePrimaryReason"]))
