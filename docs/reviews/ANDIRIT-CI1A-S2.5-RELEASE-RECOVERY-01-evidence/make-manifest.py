#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the RELEASE-RECOVERY-01 evidence package.

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

tag = git("rev-parse", "v0.2.0-preview.7^{commit}")
manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "Release recovery and public-feed qualification: publication workflow attempt-2 receipts, public-feed availability/content receipts, public-feed consumer restore and smoke logs, release identity",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01 (0.2.0-preview.7 publication recovery and public-feed qualification)",
    "qualifiedImplementation": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    "qualifiedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "releaseTag": "v0.2.0-preview.7",
    "releaseTagObject": "6659647527a6e0eb822a3d7a05b8283c75fd6a3e",
    "releaseTagTarget": tag,
    "publishRunId": "44965409440",
    "publishAttempt": 2,
    "publishAttemptConclusion": "success",
    "packagesPublished": 12,
    "disposition": "0.2.0-preview.7 - PUBLISHED AND PUBLIC-FEED QUALIFIED WITH PRESERVED QUALIFICATIONS",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}
(ROOT / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes; tag target: {tag}")