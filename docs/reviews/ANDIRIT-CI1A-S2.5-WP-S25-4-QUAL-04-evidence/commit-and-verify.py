#!/usr/bin/env python3
"""Verify the local QUAL-04 qualification commit and its sealed package.

This verifier is intentionally read-only: it performs no fetch, push, remote
execution, or working-tree mutation. Run it after the local commit is created.
"""
from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
PACKAGE = Path(__file__).resolve().parent
START = "dba6597284c655b7fd7d2645c1ecb9486fe6d693"
START_TREE = "fda08e42428625ea96e0deccf3a1d97eff7a99d1"
REVIEWED = "8b83e0558f4c757272cd60a1c81b8dbc6fd7873f"
REVIEWED_TREE = "0f89a469858abd2126f3be01cd060bc534e930c0"
ORIGIN = "48370711a6247b972dfc83628618924e864be374"
REPORT = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04.md"
PACKAGE_REL = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence"


def git(*args: str, text: bool = True) -> bytes | str:
    result = subprocess.run(
        ["git", "--no-optional-locks", "-C", str(REPO), *args],
        capture_output=True,
        text=text,
    )
    if result.returncode != 0:
        stderr = result.stderr if text else result.stderr.decode("utf-8", "replace")
        raise RuntimeError(f"git {' '.join(args)} failed: {stderr}")
    return result.stdout


def verify_manifest() -> dict[str, object]:
    manifest_path = PACKAGE / "manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    listed = [entry["path"] for entry in manifest["files"]]
    if listed != sorted(listed) or len(listed) != len(set(listed)):
        raise ValueError("manifest paths are not unique ordinal paths")
    actual_paths = sorted(
        path.relative_to(PACKAGE).as_posix()
        for path in PACKAGE.rglob("*")
        if path.is_file() and path != manifest_path
    )
    if actual_paths != listed:
        raise ValueError("manifest does not have complete package coverage")
    for entry in manifest["files"]:
        path = PACKAGE / entry["path"]
        if path.is_symlink():
            raise ValueError(f"symlink in sealed package: {entry['path']}")
        data = path.read_bytes()
        if len(data) != entry["bytes"]:
            raise ValueError(f"byte-length mismatch: {entry['path']}")
        if hashlib.sha256(data).hexdigest() != entry["sha256"]:
            raise ValueError(f"SHA-256 mismatch: {entry['path']}")
    return manifest


def committed_blob(head: str, relative: str) -> bytes:
    return subprocess.run(
        ["git", "--no-optional-locks", "-C", str(REPO), "cat-file", "blob", f"{head}:{relative}"],
        capture_output=True,
        check=True,
    ).stdout


def main() -> int:
    head = str(git("rev-parse", "HEAD")).strip()
    tree = str(git("rev-parse", "HEAD^{tree}")).strip()
    parent = str(git("rev-parse", "HEAD^")).strip()
    if parent != START:
        raise ValueError(f"unexpected parent: {parent}")
    if str(git("rev-parse", f"{START}^{{tree}}")).strip() != START_TREE:
        raise ValueError("starting tree differs")
    if str(git("rev-parse", f"{REVIEWED}^{{tree}}")).strip() != REVIEWED_TREE:
        raise ValueError("reviewed R3 tree differs")
    if subprocess.run(
        ["git", "-C", str(REPO), "merge-base", "--is-ancestor", REVIEWED, head],
        capture_output=True,
    ).returncode != 0:
        raise ValueError("reviewed R3 is not an ancestor of the qualification commit")
    if str(git("branch", "--show-current")).strip() != "main":
        raise ValueError("qualification commit is not on main")
    if str(git("status", "--porcelain=v1")).strip():
        raise ValueError("working tree is not clean")
    if str(git("stash", "list")).strip():
        raise ValueError("stash list is not empty")
    if str(git("worktree", "list", "--porcelain")).count("worktree ") != 1:
        raise ValueError("expected exactly one worktree")
    if str(git("rev-parse", "origin/main")).strip() != ORIGIN:
        raise ValueError("recorded origin/main changed")
    ahead_behind = str(git("rev-list", "--left-right", "--count", "origin/main...HEAD")).split()
    if ahead_behind != ["0", "12"]:
        raise ValueError(f"unexpected origin/main divergence: {ahead_behind}")

    manifest = verify_manifest()
    expected = sorted(
        [REPORT, f"{PACKAGE_REL}/manifest.json"] +
        [f"{PACKAGE_REL}/{entry['path']}" for entry in manifest["files"]]
    )
    changes = sorted(str(git("diff", "--name-status", "--no-renames", START, head)).splitlines())
    if changes != sorted(f"A\t{path}" for path in expected):
        raise ValueError("commit additions differ from report/evidence package")
    outside = str(git(
        "diff", "--name-only", START, head, "--", ".",
        f":(exclude){REPORT}", f":(exclude){PACKAGE_REL}/**",
    )).strip()
    if outside:
        raise ValueError(f"protected paths changed: {outside}")

    for relative in expected:
        committed = committed_blob(head, relative)
        worktree = (REPO / relative).read_bytes()
        if committed != worktree:
            raise ValueError(f"committed/worktree bytes differ: {relative}")

    manifest_bytes = (PACKAGE / "manifest.json").read_bytes()
    receipt = {
        "verified": True,
        "startingHead": START,
        "startingTree": START_TREE,
        "finalHead": head,
        "finalTree": tree,
        "parentVerified": True,
        "reviewedHead": REVIEWED,
        "reviewedTree": REVIEWED_TREE,
        "reviewedRevisionIsAncestor": True,
        "changeCount": len(changes),
        "allChangesAreReportAndEvidenceAdditions": True,
        "protectedPathsUnchanged": True,
        "committedBlobBytesMatchWorktree": True,
        "manifestEntries": len(manifest["files"]),
        "manifestSha256": hashlib.sha256(manifest_bytes).hexdigest(),
        "manifestVerified": True,
        "branch": "main",
        "clean": True,
        "worktrees": 1,
        "stashes": 0,
        "aheadOfRecordedOriginMain": 12,
        "behindRecordedOriginMain": 0,
        "fetchPerformed": False,
        "pushPerformed": False,
        "remoteExecutionPerformed": False,
    }
    print(json.dumps(receipt, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
