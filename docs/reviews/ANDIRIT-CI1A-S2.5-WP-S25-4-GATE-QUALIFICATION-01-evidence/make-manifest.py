#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the reviewer evidence package.

Mirrors the QUAL-04 manifest scheme:
"SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded;
report bound by Git". Run after all other package files are final.
"""
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(sys.argv[1])
MANIFEST = "manifest.json"

files = []
entries = []
for path in ROOT.rglob("*"):
    if not path.is_file():
        continue
    rel = path.relative_to(ROOT).as_posix()
    if rel == MANIFEST:
        continue
    entries.append(rel)
# Ordinal relative paths (posix string order), the QUAL-04 manifest convention.
for rel in sorted(entries):
    data = (ROOT / rel).read_bytes()
    files.append({
        "path": rel,
        "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
    })

manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "reviewer qualification evidence package only; producer evidence is preserved separately in ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01",
    "reviewedCommit": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    "reviewedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "startingCommit": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    "startingTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}

out = ROOT / MANIFEST
out.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes -> {out}")