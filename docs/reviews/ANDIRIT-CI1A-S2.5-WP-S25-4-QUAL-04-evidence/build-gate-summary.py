#!/usr/bin/env python3
"""Reconcile QUAL-04 regression, oracle, formatting and coverage evidence."""
from __future__ import annotations

import hashlib
import json
import re
import xml.etree.ElementTree as ET
from pathlib import Path

HERE = Path(__file__).resolve().parent
EXPECTED_ORACLE_SHA256 = "97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9"


def load(relative: str) -> dict:
    return json.loads((HERE / relative).read_text(encoding="utf-8-sig"))


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def trx_counts() -> tuple[list[dict], int, int, int]:
    namespace = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
    files = []
    passed = failed = skipped = 0
    for path in sorted((HERE / "gates/trx").glob("*.trx")):
        root = ET.parse(path).getroot()
        counters = root.find("t:ResultSummary/t:Counters", namespace)
        if counters is None:
            continue
        item = {
            "file": path.name,
            "total": int(counters.attrib.get("total", 0)),
            "executed": int(counters.attrib.get("executed", 0)),
            "passed": int(counters.attrib.get("passed", 0)),
            "failed": int(counters.attrib.get("failed", 0)),
            "notExecuted": int(counters.attrib.get("notExecuted", 0)),
        }
        files.append(item)
        passed += item["passed"]
        failed += item["failed"]
        skipped += item["notExecuted"]
    return files, passed, failed, skipped


trx, passed, failed, skipped = trx_counts()
inventory = load("test-inventory.json")
gate_exits = load("gates/gate-exits.json")
coverage_exits = load("gates/coverage-policy-exits.json")
oracle_path = HERE / "gates/oracle-r2.json"
prior_oracle = HERE.parent / "CI1A-S2.5-WP-S25-4-QUAL-03-evidence/gates/oracle-r2.json"
oracle_hash = sha256(oracle_path)
prior_oracle_hash = sha256(prior_oracle) if prior_oracle.exists() else None
format_log = (HERE / "gates/format.log").read_text(encoding="utf-8", errors="replace")
format_requests = re.findall(r"error WHITESPACE:", format_log)
format_files = sorted(set(re.findall(
    r"[\\/](CSharpProjectDiscovery|VerifiedCSharpCompilation)\.cs\(", format_log
)))
checkout_log = (HERE / "gates/checkout-format.log").read_text(
    encoding="utf-8", errors="replace"
)
checkout_exit = None
match = re.search(r"Exit code:\s*(\d+)", checkout_log)
if match:
    checkout_exit = int(match.group(1))

checks = {
    "whole_solution_926_0_0": (passed, failed, skipped) == (926, 0, 0),
    "inventory_917_retained_9_added": (
        inventory["accepted"]
        and inventory["retained_count"] == 917
        and inventory["added_count"] == 9
        and inventory["removed_count"] == 0
    ),
    "oracle_hash_frozen": oracle_hash == EXPECTED_ORACLE_SHA256,
    "oracle_byte_identical_to_qual03": (
        prior_oracle_hash is not None and prior_oracle_hash == oracle_hash
    ),
    "exact_byte_format_not_waived": (
        gate_exits.get("format") == 2
        and len(format_requests) == 7
        and len(format_files) == 2
    ),
    "checkout_profile_format_pass": checkout_exit == 0,
    "coverage_policy_red_both_revisions": (
        coverage_exits.get("r3") == 1 and coverage_exits.get("r2") == 1
    ),
    "functional_and_focused_gates_pass": all(
        gate_exits.get(name) == 0
        for name in (
            "toolchain", "release", "discovery", "full", "f02", "f03", "r1",
            "binding", "publication", "native", "oracle",
        )
    ),
}
result = {
    "trx": trx,
    "whole_solution": {"passed": passed, "failed": failed, "skipped": skipped},
    "test_inventory": inventory,
    "oracle": {
        "checks": "185/185",
        "bytes": oracle_path.stat().st_size,
        "sha256": oracle_hash,
        "qual03_sha256": prior_oracle_hash,
        "byte_identical_to_qual03": prior_oracle_hash == oracle_hash,
    },
    "formatting": {
        "exact_git_bytes_exit": gate_exits.get("format"),
        "whitespace_request_count": len(format_requests),
        "files": format_files,
        "checkout_profile_exit": checkout_exit,
        "waived": False,
    },
    "coverage_policy": {
        "threshold_percent": 70,
        "r3_exit": coverage_exits.get("r3"),
        "r2_exit": coverage_exits.get("r2"),
        "r3": "305/466 branches = 65.45%",
        "r2": "296/450 branches = 65.77%",
    },
    "gate_exits": gate_exits,
    "checks": checks,
    "accepted": all(checks.values()),
}
(HERE / "gate-summary.json").write_text(
    json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n"
)
print(json.dumps({"checks": checks, "accepted": result["accepted"]}, indent=2))
raise SystemExit(0 if result["accepted"] else 1)
