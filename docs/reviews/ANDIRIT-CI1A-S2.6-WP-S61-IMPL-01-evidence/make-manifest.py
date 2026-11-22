#!/usr/bin/env python3
"""Self-excluding SHA-256 manifest for the WP-S61-IMPL-01 evidence package.

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
        "commission": "ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01",
        "scheme": "SHA-256 exact bytes and lengths; ordinal relative paths; manifest self-excluded; report bound by Git",
        "scope": "S6.1 native cancellation path: implementation of C-1..C-4, the Git-object-derived preflight receipt, the contract-consistency gate receipt, the Windows x64 qualification matrix Q-01..Q-12 with the post-commit regression guard, the whole-solution regression and coverage receipts, the public API surface diff, the production delta and the genuine hosted Ubuntu x64 CI evidence",
        "reviewedSubject": "ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 (S6.1 native cancellation path: implementation and native qualification)",
        "baselineCommit": "2d1e8ec88e397d0c36b7f258eb1a1c0840e56157",
        "baselineTree": "82ce2b873aa17c69fe5da63deb5910fd520c8acd",
        "qualifiedImplementation": "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6",
        "releaseTag": "v0.2.0-preview.7",
        "releaseTagObject": "6659647527a6e0eb822a3d7a05b8283c75fd6a3e",
        "releaseTagTarget": "6ed05964d2fcc758648b654a01ec706647e52183",
        "releaseVersion": "0.2.0-preview.7",
        "nativeDependency": "LatticeDbSharp 0.2.0",
        "nativeDependencyAssemblySha256": "d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46",
        "qualificationCases": 12,
        "regressionGuards": 1,
        "files": files,
        "fileCount": len(files),
        "totalBytes": sum(entry["bytes"] for entry in files),
    }

    (root / MANIFEST).write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(
        f"manifest: {len(files)} files, {manifest['totalBytes']} bytes; "
        f"HEAD {git(repo, 'rev-parse', 'HEAD')}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
