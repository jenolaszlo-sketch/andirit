#!/usr/bin/env python3
"""Compare R2 and R3 `dotnet test --list-tests` inventories.

The PowerShell wrappers prepend a UTF-8 header while dotnet writes UTF-16LE
output. The parser starts at the first UTF-16LE `Test` marker and extracts the
four-space-indented fully qualified test names.
"""
from __future__ import annotations

import json
import sys
from pathlib import Path


def names(path: Path) -> list[str]:
    data = path.read_bytes()
    marker = b"T\x00e\x00s\x00t\x00"
    start = data.find(marker)
    if start < 0:
        raise ValueError(f"UTF-16LE dotnet output marker not found in {path}")
    text = data[start:].decode("utf-16le", errors="strict")
    result = []
    for line in text.splitlines():
        value = line.strip()
        if (
            line.startswith("    ")
            and value.startswith(("Andirit.", "Penghou."))
            and "Test run" not in value
        ):
            result.append(value)
    return result


def main() -> int:
    if len(sys.argv) != 4:
        print("usage: compare-test-inventory.py <r2-log> <r3-log> <output-json>")
        return 2
    r2 = names(Path(sys.argv[1]))
    r3 = names(Path(sys.argv[2]))
    r2_set, r3_set = set(r2), set(r3)
    result = {
        "r2_count": len(r2),
        "r3_count": len(r3),
        "r2_unique_count": len(r2_set),
        "r3_unique_count": len(r3_set),
        "retained_count": len(r2_set & r3_set),
        "added_count": len(r3_set - r2_set),
        "removed_count": len(r2_set - r3_set),
        "added": sorted(r3_set - r2_set),
        "removed": sorted(r2_set - r3_set),
        "accepted": (
            len(r2) == len(r2_set) == 917
            and len(r3) == len(r3_set) == 926
            and len(r2_set - r3_set) == 0
        ),
    }
    Path(sys.argv[3]).write_text(
        json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n"
    )
    print(json.dumps(result, indent=2))
    return 0 if result["accepted"] else 1


if __name__ == "__main__":
    sys.exit(main())
