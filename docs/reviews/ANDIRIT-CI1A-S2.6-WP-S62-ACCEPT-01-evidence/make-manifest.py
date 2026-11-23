#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the S6.2 acceptance evidence package."""
from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: python make-manifest.py <package-dir> <repo-root>")
        return 2
    package = Path(sys.argv[1]).resolve()
    repo = Path(sys.argv[2]).resolve()

    files = sorted(
        path for path in package.rglob("*")
        if path.is_file() and path.name != "manifest.json"
    )

    entries = []
    total = 0
    for path in files:
        data = path.read_bytes()
        entries.append({
            "path": path.relative_to(package).as_posix(),
            "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest().lower(),
        })
        total += len(data)

    head = subprocess.run(
        ["git", "-C", str(repo), "rev-parse", "HEAD"], capture_output=True, text=True
    ).stdout.strip()

    manifest = {
        "package": package.name,
        "boundToHeadAtGeneration": head,
        "entries": len(entries),
        "totalBytes": total,
        "algorithm": "sha256",
        "selfExcluding": True,
        "files": entries,
    }
    (package / "manifest.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=True) + "\n", encoding="utf-8"
    )
    print(f"manifest: {len(entries)} files, {total} bytes; HEAD {head}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())