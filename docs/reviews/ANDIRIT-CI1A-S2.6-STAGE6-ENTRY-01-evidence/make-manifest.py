#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the STAGE6-ENTRY-01 evidence package.

Scheme (QUAL-04/R3 lineage): "SHA-256 exact bytes and lengths; ordinal relative
paths; manifest self-excluded; report bound by Git".

Usage:
    python make-manifest.py <evidence-root> [<repo>]
"""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

MANIFEST = "manifest.json"


def git(repo: str, *args: str) -> str:
    result = subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True)
    return result.stdout.strip()


def main() -> int:
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    root = Path(sys.argv[1]).resolve()
    repo = sys.argv[2] if len(sys.argv) > 2 else "D:\\projects\\andirit"

    entries = []
    for path in root.rglob("*"):
        if not path.is_file():
            continue
        relative = path.relative_to(root).as_posix()
        if relative == MANIFEST:
            continue
        entries.append(relative)

    files = []
    for relative in sorted(entries):
        data = (root / relative).read_bytes()
        files.append({
            "path": relative,
            "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest(),
        })

    manifest = {
        "commission": "ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01",
        "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
        "scope": "Stage 6 entry, work package S6.1: repository preflight, contract reconciliation excerpts, native cancellation feasibility probes (managed/native API census, native ABI scan, ten behavioural scenarios), and the raw receipts behind the feasibility assessment, implementation proposal and qualification plan",
        "reviewedSubject": "ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01 (Stage 6 entry for S6.1: native cancellation path)",
        "baselineCommit": "b5a81f81fade9807f706b4586329efe416cf1558",
        "baselineTree": "e3c28384abe1870eb5b44dbbc693b69f371542af",
        "qualifiedImplementation": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
        "releaseTag": "v0.2.0-preview.7",
        "releaseTagObject": "6659647527a6e0eb822a3d7a05b8283c75fd6a3e",
        "releaseTagTarget": "6ed05964d2fcc758648b654a01ec706647e52183",
        "releaseVersion": "0.2.0-preview.7",
        "nativeDependency": "LatticeDbSharp 0.2.0",
        "nativeDependencyAssemblySha256": "d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46",
        "probeScenarios": 10,
        "probeChecks": 41,
        "disposition": "READY WITH QUALIFICATIONS",
        "qualifications": [
            "Q-S61-1", "Q-S61-2", "Q-S61-3", "Q-S61-4",
            "Q-S61-5", "Q-S61-6", "Q-S61-7", "Q-S61-8",
        ],
        "files": files,
        "fileCount": len(files),
        "totalBytes": sum(entry["bytes"] for entry in files),
    }

    (root / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(
        f"manifest: {len(files)} files, {manifest['totalBytes']} bytes; "
        f"baseline {git(repo, 'rev-parse', 'HEAD')}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
