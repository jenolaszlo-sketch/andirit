#!/usr/bin/env python3
"""Independent reconciliation of producer evidence against raw artifacts.

Reads the producer evidence package (GATE-REMEDIATION-01) without modifying it,
independently derives coverage numerator/denominator from the Cobertura XML and
test counts from the TRX, and records byte/length identity for the manifest.
"""
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(r"C:\Windows\Temp\opencode\gate-remediation\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01")
pkg = ROOT / "evidence"

out = {}

# --- artifact identity (exact bytes, SHA-256, lengths) ------------------------
for name in sorted(p.name for p in pkg.rglob("*") if p.is_file()):
    data = (pkg / name).read_bytes()
    out.setdefault("artifacts", {})[name] = {
        "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
    }
report = (ROOT / "ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01.md").read_bytes()
out["report"] = {"bytes": len(report), "sha256": hashlib.sha256(report).hexdigest()}

# --- independent coverage derivation -----------------------------------------
xml = pkg / "coverage-latticedb.cobertura.xml"
root = ET.parse(xml).getroot()
declared = {
    "lines-covered": root.get("lines-covered"),
    "lines-valid": root.get("lines-valid"),
    "branches-covered": root.get("branches-covered"),
    "branches-valid": root.get("branches-valid"),
    "line-rate": root.get("line-rate"),
    "branch-rate": root.get("branch-rate"),
}
derived = {"classes": 0, "methods": 0, "branch_true_lines": 0, "line_hits_elements": 0}
pkg_lines_valid = pkg_lines_covered = 0
pkg_branches_valid = pkg_branches_covered = 0
per_package = {}
for package in root.iter("package"):
    pname = package.get("name")
    p_lines_covered = p_lines_valid = 0
    p_branch_covered = p_branch_valid = 0
    for cls in package.iter("class"):
        derived["classes"] += 1
        p_lines_covered += int(cls.get("lines-covered", "0"))
        p_lines_valid += int(cls.get("lines-valid", "0"))
        for method in cls.iter("method"):
            derived["methods"] += 1
            p_branch_covered += int(method.get("branch-rate") is not None)
        for line in cls.iter("line"):
            derived["line_hits_elements"] += 1
            if line.get("branch") == "True":
                derived["branch_true_lines"] += 1
                p_branch_valid += 1
                if int(line.get("hits", "0")) > 0:
                    # condition-coverage "x/y" numerator > 0 -> branch covered
                    pass
    per_package[pname] = {
        "class-lines-covered": p_lines_covered,
        "class-lines-valid": p_lines_valid,
        "class-branchable-lines": p_branch_valid,
    }

# Cobertura semantic: <line branch="True" condition-coverage="n/d" hits="h">
# branch is covered iff any branch was taken: condition-coverage n/d with n>0.
branch_covered = 0
branch_valid = 0
lines_covered = 0
lines_valid = 0
import re
cond_re = re.compile(r"\((\d+)\s*/\s*(\d+)\)")
for line in root.iter("line"):
    lines_valid += 1
    if int(line.get("hits", "0")) > 0:
        lines_covered += 1
    if line.get("branch") == "True":
        cc = line.get("condition-coverage")
        if cc is None:
            continue
        m = cond_re.search(cc)
        if m is None:
            continue
        taken, total = int(m.group(1)), int(m.group(2))
        branch_valid += total
        branch_covered += taken
derived["line_elements"] = {"covered": lines_covered, "valid": lines_valid}
derived["condition_coverage"] = {"covered": branch_covered, "valid": branch_valid}

out["coverage"] = {
    "declared_root_attributes": declared,
    "derived": derived,
    "per_package": per_package,
    "branch_rate_from_condition_coverage": round(branch_covered / branch_valid, 6) if branch_valid else None,
    "line_rate_from_elements": round(lines_covered / lines_valid, 6) if lines_valid else None,
}

# --- producer TRX test counts ------------------------------------------------
trx = pkg / "latticedb-testresults.trx"
troot = ET.parse(trx).getroot()
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamMSTest/2010"}
counters = troot.find(".//t:ResultSummary/t:Counters", ns)
counts = dict(counters.attrib) if counters is not None else {}
outcomes = {}
for ut in troot.iter("{http://microsoft.com/schemas/VisualStudio/TeamMSTest/2010}UnitTestResult"):
    outcomes[ut.get("outcome")] = outcomes.get(ut.get("outcome"), 0) + 1
out["producer_trx"] = {
    "counters": counts,
    "outcomes": outcomes,
    "total_results": sum(outcomes.values()),
}

print(json.dumps(out, indent=2))