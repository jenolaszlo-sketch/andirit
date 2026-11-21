#!/usr/bin/env python3
"""M54-B-QUAL-01 refusal-path coverage verification.

Independently verifies, from the generated Cobertura artifact, that the
production refusal branches exercised by this commission were actually
executed (hits > 0), and records the summary counters of the included
assemblies. Produces a JSON receipt.
"""
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

COVERAGE = Path(sys.argv[1] if len(sys.argv) > 1 else r"D:\m54b-qual\coverage\coverage.cobertura.xml")
OUT = Path(sys.argv[2] if len(sys.argv) > 2 else r"D:\m54b-qual\receipts\refusal-path-coverage.json")

ROOT = ET.parse(COVERAGE).getroot()

TARGETS = {
    # In-process refusal paths (coverlet-instrumented; must be hit).
    "src/Andirit.CSharp/VerifiedCSharpAnalysis.cs": [
        (105, "Incomplete classification (incomplete declared universe)"),
        (117, "UnsupportedExternalEvidence classification (PE-only evidence)"),
        (119, "MissingDefiningCompilation classification (no verified defining compilation)"),
        (123, "Ambiguous classification (multiple binding entries)"),
        (134, "StaleContext classification (recorded context mismatch)"),
        (163, "Mint gate: MissingRecordedContext refusal"),
        (167, "Mint gate: non-Complete resolution refuses"),
        (185, "MapMintStatus: MissingDefiningCompilation mapping"),
        (202, "MintUsage: non-minted mint reaches the refusal return"),
        (203, "MintUsage: non-minted status yields no fact"),
    ],
    "src/Andirit.CSharp/VerifiedUsageMint.cs": [
        (60, "PrepareNativeUnit: refuses to publish without a verified authority"),
    ],
    # Distinct-process paths: exercised by the reopen leg, evidenced by the
    # reopen receipts (coverlet does not instrument the child process).
    "CHILD_PROCESS": {
        "src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs": [
            (66, "Native authority gate: AnalysisUnavailable refusal"),
        ],
        "src/Andirit/InMemoryCodeGraphStore.cs": [
            (887, "Availability mapping: IncompleteAnalysis vs Available decision"),
            (890, "Truthful refusal reason for incomplete analysis"),
        ],
    },
}


def line_hits_by_number(root, suffix):
    """Merge the <line> elements of every <class> whose filename ends with
    suffix (a source file may carry several partial-class entries)."""
    merged: dict[int, int] = {}
    found = False
    for c in root.iter("class"):
        filename = (c.get("filename") or "").replace("\\", "/")
        if not filename.endswith(suffix):
            continue
        found = True
        for line in c.iter("line"):
            number = int(line.get("number"))
            hits = int(line.get("hits", "0"))
            merged[number] = max(merged.get(number, 0), hits)
    return merged if found else None


CHILD = TARGETS.pop("CHILD_PROCESS")
report = {"artifact": str(COVERAGE), "targets": {}, "assemblies": {}, "allTargetsHit": True,
          "childProcessPaths": {}}
for suffix, lines in CHILD.items():
    report["childProcessPaths"][suffix] = [
        {"line": n, "description": d,
         "coverage": "not collected (distinct reopen process)",
         "receiptEvidence": "e3b/e3b2/e3c reopen receipts"} for n, d in lines]
for suffix, lines in TARGETS.items():
    by_number = line_hits_by_number(ROOT, suffix)
    if by_number is None:
        report["targets"][suffix] = "class-not-instrumented"
        report["allTargetsHit"] = False
        continue
    entry = {}
    for number, description in lines:
        hits = by_number.get(number)
        entry[f"{number}"] = {"hits": hits, "description": description,
                              "hit": hits is not None and hits > 0}
        if hits is None or hits == 0:
            report["allTargetsHit"] = False
    report["targets"][suffix] = entry

for pkg in ROOT.iter("package"):
    name = pkg.get("name")
    lc = sum(int(c.get("lines-covered", "0")) for c in pkg.iter("class"))
    lv = sum(int(c.get("lines-valid", "0")) for c in pkg.iter("class"))
    bc = int(pkg.get("branches-covered", "0"))
    bv = int(pkg.get("branches-valid", "0"))
    report["assemblies"][name] = {"lines": f"{lc}/{lv}", "branches": f"{bc}/{bv}",
                                  "line-rate": pkg.get("line-rate"),
                                  "branch-rate": pkg.get("branch-rate")}

OUT.parent.mkdir(parents=True, exist_ok=True)
OUT.write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({"allTargetsHit": report["allTargetsHit"],
                  "targets": {k: (v if isinstance(v, str) else
                                  {n: d["hits"] for n, d in v.items()})
                              for k, v in report["targets"].items()},
                  "assemblies": report["assemblies"]}, indent=2))
sys.exit(0 if report["allTargetsHit"] else 1)