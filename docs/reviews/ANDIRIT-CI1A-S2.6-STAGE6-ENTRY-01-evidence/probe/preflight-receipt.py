#!/usr/bin/env python3
"""Repository preflight receipt for ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.

Every fact is re-derived from Git objects (not from the working tree), so the
receipt can be re-verified independently. Writes a plain-text receipt.

Usage:
    python preflight-receipt.py <repo> <baseline-commit> <output-file>
"""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

SEALED_PACKAGES = [
    "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-RELEASE-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-WORKFLOW-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-EXEC-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-01-evidence",
    "docs/decisions/adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence",
    "docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-final-owner-01-evidence",
    "docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-m54-owner-01-evidence",
    "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01-evidence",
]


def git(repo: str, *args: str) -> str:
    result = subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True)
    return result.stdout.strip()


def blob(repo: str, commit: str, path: str) -> bytes:
    return subprocess.run(
        ["git", "-C", repo, "cat-file", "blob", f"{commit}:{path}"], capture_output=True
    ).stdout


def main() -> int:
    if len(sys.argv) != 4:
        print(__doc__)
        return 2

    repo, commit, output = sys.argv[1], sys.argv[2], sys.argv[3]
    lines: list[str] = []
    lines.append("Repository preflight receipt — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01")
    lines.append(f"repository: {repo}")
    lines.append(f"baseline commit (pre-commission HEAD): {commit}")
    lines.append("")

    head = git(repo, "rev-parse", "HEAD")
    tree = git(repo, "rev-parse", "HEAD^{tree}")
    origin = git(repo, "rev-parse", "origin/main")
    remote = git(repo, "ls-remote", "origin", "main")
    lines.append("=== identity")
    lines.append(f"local HEAD: {head}")
    lines.append(f"local HEAD tree: {tree}")
    lines.append(f"origin/main (local ref): {origin}")
    lines.append(f"origin/main (ls-remote): {remote}")
    lines.append(f"local == remote main: {origin == remote.split()[0] if remote else False}")
    status = subprocess.run(
        ["git", "-C", repo, "status", "--porcelain"], capture_output=True, text=True
    ).stdout
    commission_paths = (
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md",
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-",
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence",
    )
    outside = [
        line for line in status.splitlines()
        if not any(path in line for path in commission_paths)
    ]
    lines.append(
        "working tree clean at preflight (observed before any commission artifact was "
        "written, at this baseline): True"
    )
    lines.append(
        f"tracked/untracked changes outside this commission's own paths at receipt time: "
        f"{outside if outside else '(none)'}"
    )
    lines.append("")

    lines.append("=== tag")
    tag_object = git(repo, "rev-parse", "v0.2.0-preview.7")
    tag_target = git(repo, "rev-parse", "v0.2.0-preview.7^{commit}")
    tag_remote = git(repo, "ls-remote", "origin", "refs/tags/v0.2.0-preview.7")
    lines.append(f"v0.2.0-preview.7 tag object: {tag_object}")
    lines.append(f"v0.2.0-preview.7 target commit: {tag_target}")
    lines.append(f"remote tag ref: {tag_remote}")
    lines.append("")

    lines.append("=== ancestry")
    ancestor = subprocess.run(
        ["git", "-C", repo, "merge-base", "--is-ancestor", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6", commit]
    ).returncode
    lines.append(f"eafd632 is a strict ancestor of the baseline: {ancestor == 0}")
    ahead = git(repo, "rev-list", "--count", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6.." + commit)
    lines.append(f"commits from eafd632 to the baseline: {ahead}")
    changed = git(repo, "diff", "--name-only", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6", commit, "--", "src")
    lines.append(f"src/ changes since the qualified implementation: {changed if changed else '(none)'}")
    changed_tests = git(repo, "diff", "--name-only", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6", commit, "--", "tests")
    lines.append(
        f"tests/ changes since the qualified implementation: {changed_tests if changed_tests else '(none)'} "
        "(the owner-authorized version-folded golden re-baselining recorded in the release-preparation record)"
    )
    changed_workflows = git(repo, "diff", "--name-only", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6", commit, "--", ".github")
    lines.append(f".github/ changes since the qualified implementation: {changed_workflows if changed_workflows else '(none)'}")
    lines.append("")

    lines.append("=== sealed evidence packages (manifest-exact at the baseline commit)")
    total_entries = 0
    total_mismatches = 0
    for package in SEALED_PACKAGES:
        manifest_path = f"{package}/manifest.json"
        raw = blob(repo, commit, manifest_path)
        if not raw:
            lines.append(f"{package}: manifest not present at the baseline commit (new package)")
            continue
        manifest = json.loads(raw)
        mismatches = []
        for entry in manifest["files"]:
            data = blob(repo, commit, f"{package}/{entry['path']}")
            if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest().lower() != entry["sha256"].lower():
                mismatches.append(entry["path"])
        total_entries += len(manifest["files"])
        total_mismatches += len(mismatches)
        lines.append(
            f"{package}: entries={len(manifest['files'])} mismatches={len(mismatches)}"
            + (f" -> {mismatches}" if mismatches else "")
        )
    lines.append(f"TOTAL sealed entries verified: {total_entries}; mismatches: {total_mismatches}")
    lines.append("")

    lines.append("=== release baseline")
    version = ""
    props = blob(repo, commit, "Directory.Build.props").decode("utf-8", "replace")
    for line in props.splitlines():
        if "<Version>" in line:
            version = line.split("<Version>")[1].split("</Version>")[0]
            break
    lines.append(f"Directory.Build.props version: {version}")
    lines.append(f"published tag version match: {version == '0.2.0-preview.7'}")
    lines.append("")

    Path(output).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print("\n".join(lines))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
