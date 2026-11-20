#!/usr/bin/env python3
"""Independent export verifier: compare a plain export against the exact Git blobs of a pinned revision.

Usage: python verify-exports.py <repo> <revision> <export-dir> [<export-dir> ...]

For each file recorded in `git ls-tree -r <revision>`, compute the Git blob
hash (SHA-1 over "blob <size>\0" + bytes) of the exported bytes and compare.
Reports the total entries, mismatches, missing files and unmanifested extras.
Exit code 0 only on an exact match.
"""
import hashlib
import subprocess
import sys
from pathlib import Path


def git_blobs(repo: str, revision: str) -> dict[str, str]:
    out = subprocess.run(
        ["git", "-C", repo, "ls-tree", "-r", "--full-name", revision],
        check=True, capture_output=True, text=True,
    ).stdout
    blobs: dict[str, str] = {}
    for line in out.splitlines():
        meta, path = line.split("\t", 1)
        mode, otype, sha = meta.split()
        if otype != "blob":
            continue
        blobs[path.replace("\\", "/")] = sha
    return blobs


def blob_hash(data: bytes) -> str:
    h = hashlib.sha1()
    h.update(b"blob %d\0" % len(data))
    h.update(data)
    return h.hexdigest()


def main() -> int:
    repo, revision = sys.argv[1], sys.argv[2]
    export_dirs = sys.argv[3:]
    blobs = git_blobs(repo, revision)
    failures: list[str] = []
    for export in export_dirs:
        root = Path(export)
        present = {p.relative_to(root).as_posix(): p for p in root.rglob("*") if p.is_file()}
        missing = sorted(set(blobs) - set(present))
        extra = sorted(set(present) - set(blobs))
        mismatched: list[str] = []
        for rel, sha in sorted(blobs.items()):
            if rel in missing:
                continue
            data = present[rel].read_bytes()
            if blob_hash(data) != sha:
                mismatched.append(rel)
        print(f"[{export}] git entries={len(blobs)} exported={len(present)} "
              f"missing={len(missing)} extra={len(extra)} mismatched={len(mismatched)}")
        for name in missing[:10]:
            print(f"  MISSING {name}")
        for name in extra[:10]:
            print(f"  EXTRA   {name}")
        for name in mismatched[:10]:
            print(f"  HASH    {name}")
        if missing or extra or mismatched:
            failures.append(export)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())