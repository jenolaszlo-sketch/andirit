#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the PUBLICATION-WORKFLOW-01 evidence package.

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

release = git("rev-parse", "origin/main")
manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-PUBLICATION-WORKFLOW-01",
    "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
    "scope": "publish.yml remediation qualification: workflow diff, local logic tests, packaging reproduction, inventory reconciliation against the qualified 0.2.0-preview.7 feed; decision bound by Git",
    "reviewedSubject": "ANDIRIT-CI1A-S2.5-PUBLICATION-WORKFLOW-01 (release publication workflow correction)",
    "qualifiedImplementation": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
    "qualifiedTree": "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d",
    "publicationQualificationCommit": "2aaab9ab3babd493b4a84e954ee16c623f558e73",
    "executionCommit": "3c48187ca1ec66eb1c62c1cfb9c1a4134875bb57",
    "releaseCommit": "68a141f67023a9a66a5d2b77c8d93cf3cecb9993",
    "publishedRemoteMain": release,
    "intendedTag": "v0.2.0-preview.7",
    "files": files,
    "fileCount": len(files),
    "totalBytes": sum(f["bytes"] for f in files),
}
(ROOT / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
print(f"manifest: {len(files)} files, {manifest['totalBytes']} bytes; published main: {release}")