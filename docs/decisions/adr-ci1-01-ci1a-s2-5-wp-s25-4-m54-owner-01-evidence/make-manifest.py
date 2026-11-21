#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the M54-OWNER-01 decision evidence package.

Scheme (QUAL-04/R3/GATE-QUALIFICATION-01): "SHA-256 exact bytes and lengths;
ordinal relative paths; manifest self-excluded; report bound by Git".
"""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT = Path(sys.argv[1])
MANIFEST = "manifest.json"


def git(*args: str) -> str:
    return subprocess.run(["git", "-C", r"D:\projects\andirit", *args],
                          capture_output=True, text=True).stdout.strip()


entries = []
for path in ROOT.rglob("*"):
    if not path.is_file():
        continue
    rel = path.relative_to(ROOT).as_posix()
    if rel == MANIFEST:
        continue
    entries.append(rel)
files = []
for rel in sorted(entries):
    data = (ROOT / rel).read_bytes()
    files.append({"path": rel, "bytes": len(data),
                  "sha256": hashlib.sha256(data).hexdigest()})

qualified = git("rev-parse", "origin/qualification/wp-s25-4-sq05-linux-r2")
manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-WP-S25-4-M54-OWNER-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "owner decision evidence package (closure assessment + residual-risk register); decisions bound by Git",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01 (M5.4 under the amended contract)",
    "qualifiedImplementation": qualified,
    "qualifiedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "m54QualificationCommit": "3e6a0690df55d8e61b71e1f0813732b839141c9f",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}
(ROOT / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes")