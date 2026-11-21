#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the STAGE2-ACCEPTANCE-OWNER-01 evidence package.

Scheme (QUAL-04/R3 lineage): "SHA-256 exact bytes and lengths; ordinal relative
paths; manifest self-excluded; report bound by Git".
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
    "commission": "ANDIRIT-CI1A-S2.5-STAGE2-ACCEPTANCE-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "Stage-2 integrated owner acceptance evidence (acceptance matrix, obligation register, capability summary, Stage 6 entry proposal, publication readiness); decision bound by Git",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-STAGE2-ACCEPTANCE-01 (Stage-2 integrated acceptance)",
    "qualifiedImplementation": qualified,
    "qualifiedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "wpS25FinalClosureCommit": "6250ad153191cb60c779136dd08e0ce2548bbce6",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}
(ROOT / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes")