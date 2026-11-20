#!/usr/bin/env python3
"""Build the pinned-revision LatticeDb suite census for the Ubuntu commission.

The enumeration is derived from the qualified GATE-QUALIFICATION-01 whole-solution TRX
(windows execution record of the pinned revision eafd632) - preparation data, not a
re-execution. The Linux execution re-derives its own counts and must agree 75/0/0.
"""
import xml.etree.ElementTree as ET
from pathlib import Path

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
BASE = Path(__file__).resolve().parent
QUAL_TRX = Path(
    r"D:\projects\andirit\docs\reviews\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence"
    r"\gates\trx\Laszlos_LAPTOP-KBD8D8JG_2026-11-20_14_46_07_net10.0.trx")
OUT = BASE / "suite-census.txt"


def main() -> int:
    root = ET.parse(QUAL_TRX).getroot()
    rows = [(ut.get("testName"), ut.get("outcome"))
            for ut in root.iter(NS + "UnitTestResult")]
    counts: dict[str, int] = {}
    for _, outcome in rows:
        counts[outcome] = counts.get(outcome, 0) + 1
    by: dict[str, list[tuple[str, str]]] = {}
    for name, outcome in rows:
        parts = name.split(".")
        by.setdefault(parts[-2] if len(parts) >= 2 else name, []).append((name, outcome))
    lines = [
        "# Pinned LatticeDb suite census for ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01",
        "# Source: GATE-QUALIFICATION-01 whole-solution TRX (pinned revision eafd632, Windows x64,",
        "#         2026-11-20). Preparation enumeration only; Linux execution re-derives the counts.",
        f"# Total test cases: {len(rows)}  outcomes: {counts}",
        "",
    ]
    for cls in sorted(by):
        entries = by[cls]
        lines.append(f"## {cls} ({len(entries)} cases)")
        for name, outcome in sorted(entries):
            lines.append(f"  - {name.rsplit('.', 1)[-1]}  [{outcome}]")
        lines.append("")
    OUT.write_text("\n".join(lines), encoding="utf-8")
    print(f"census: {len(rows)} cases {counts} -> {OUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())