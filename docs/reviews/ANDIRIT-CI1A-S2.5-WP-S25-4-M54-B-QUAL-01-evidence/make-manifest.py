#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the M54-B-QUAL-01 evidence package.

Qual-04/R3/GATE-QUALIFICATION-01 scheme: "SHA-256 exact bytes and lengths; ordinal
relative paths; manifest self-excluded; report bound by Git". Run after all other
package files are final.
"""
import hashlib
import sys
from pathlib import Path

ROOT = Path(sys.argv[1])
MANIFEST = "manifest.json"

entries = []
for path in ROOT.rglob("*"):
    if not path.is_file():
        continue
    rel = path.relative_to(ROOT).as_posix()
    if rel == MANIFEST:
        continue
    entries.append(rel)
# Ordinal relative paths (posix string order), the established convention.
files = []
for rel in sorted(entries):
    data = (ROOT / rel).read_bytes()
    files.append({
        "path": rel,
        "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
    })

manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "reviewer qualification evidence package; report bound by Git at ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01.md",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01 (M5.4 under the amended contract)",
    "reviewedCommit": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    "reviewedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "startingCommit": "7d1797fd5d0aa721946f0c5ef5f25bdab98aac2e",
    "startingTree": "f54e06cbf707cc3021cbdb3e432c483146f2a7c2",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}

(ROOT / MANIFEST).write_text(
    __import__("json").dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes -> {ROOT / MANIFEST}")