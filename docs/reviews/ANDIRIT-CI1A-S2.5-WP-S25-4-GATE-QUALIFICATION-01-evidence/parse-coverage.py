#!/usr/bin/env python3
"""Independent coverage derivation (GQ-03) for ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01.

Derives line and branch numerator/denominator directly from Cobertura artifacts:
  * the coverlet root attributes (the binding gate summary values), and
  * an independent re-derivation from <line> condition-coverage sums,
    cross-checked against the R3 baseline artifact (QUAL-04 gates package) so the
    constant coverlet granularity offset is demonstrated rather than assumed.

Usage: python parse-coverage.py <r3-cobertura.xml> <remediation-cobertura.xml> [out.json]
"""
import json
import re
import sys
import xml.etree.ElementTree as ET

COND = re.compile(r"\((\d+)\s*/\s*(\d+)\)")


def derive(path: str) -> dict:
    root = ET.parse(path).getroot()
    lines_cov = lines_val = cond_cov = cond_val = branch_lines = 0
    per_class: dict[str, list[int]] = {}
    for line in root.iter("line"):
        lines_val += 1
        if int(line.get("hits", "0")) > 0:
            lines_cov += 1
        if line.get("branch") == "True":
            branch_lines += 1
            m = COND.search(line.get("condition-coverage") or "")
            if m:
                cond_cov += int(m.group(1))
                cond_val += int(m.group(2))
    for cls in root.iter("class"):
        n = m_ = 0
        for line in cls.iter("line"):
            if line.get("branch") == "True":
                mm = COND.search(line.get("condition-coverage") or "")
                if mm:
                    n += int(mm.group(1))
                    m_ += int(mm.group(2))
        per_class[cls.get("name")] = [n, m_]
    return {
        "root_attributes": {k: root.get(k) for k in (
            "lines-covered", "lines-valid", "branches-covered", "branches-valid",
            "line-rate", "branch-rate", "timestamp")},
        "line_elements": {"covered": lines_cov, "valid": lines_val},
        "condition_coverage": {"covered": cond_cov, "valid": cond_val},
        "condition_half": {"covered": cond_cov // 2, "valid": cond_val // 2},
        "branch_lines": branch_lines,
        "branch_rate_root": (
            int(root.get("branches-covered")) / int(root.get("branches-valid"))
            if root.get("branches-covered") and root.get("branches-valid") else None),
        "per_class": per_class,
    }


def main() -> int:
    r3 = derive(sys.argv[1])
    new = derive(sys.argv[2])
    delta = {
        name: new["per_class"][name][0] - r3["per_class"].get(name, [0, 0])[0]
        for name in new["per_class"]
    }
    result = {
        "r3_baseline": r3,
        "remediation": new,
        "covered_branch_delta_total": (
            int(new["root_attributes"]["branches-covered"])
            - int(r3["root_attributes"]["branches-covered"])),
        "valid_branch_delta_total": (
            int(new["root_attributes"]["branches-valid"])
            - int(r3["root_attributes"]["branches-valid"])),
        "class_set_identical": set(r3["per_class"]) == set(new["per_class"]),
        "condition_half_offset_r3": (
            int(r3["root_attributes"]["branches-valid"]) - r3["condition_half"]["valid"]),
        "condition_half_offset_new": (
            int(new["root_attributes"]["branches-valid"]) - new["condition_half"]["valid"]),
        "largest_covered_condition_deltas": sorted(
            ({"class": k, "condition_covered_delta": v} for k, v in delta.items()),
            key=lambda d: -d["condition_covered_delta"])[:5],
    }
    text = json.dumps(result, indent=2)
    if len(sys.argv) > 3:
        with open(sys.argv[3], "w", encoding="utf-8") as fh:
            fh.write(text)
    print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())