#!/usr/bin/env python3
"""Git-object-derived preflight receipt for ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.

Every fact in the receipt is re-derived from Git objects (and the remote
references), so an independent reviewer can reproduce it byte-for-byte.

Usage:
    python preflight-receipt.py <repo> <baseline-commit> <output-file>
"""

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


def git(repo: str, *args: str) -> str:
    result = subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True)
    return result.stdout.strip()


def blob(repo: str, commit: str, path: str) -> bytes:
    return subprocess.run(
        ["git", "-C", repo, "cat-file", "blob", f"{commit}:{path}"], capture_output=True
    ).stdout


def main() -> int:
    if len(sys.argv) < 4:
        print(__doc__)
        return 2

    repo = str(Path(sys.argv[1]).resolve())
    baseline = sys.argv[2]
    output = Path(sys.argv[3])

    lines: list[str] = []

    def emit(text: str = "") -> None:
        lines.append(text)

    head = git(repo, "rev-parse", "HEAD")
    head_tree = git(repo, "rev-parse", "HEAD^{tree}")
    status = git(repo, "status", "--porcelain")

    emit("ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01 - preflight receipt")
    emit("generated-by: preflight-receipt.py (Git-object derived)")
    emit()
    emit("[repository]")
    emit(f"repo: {repo}")
    emit(f"local-head: {head}")
    emit(f"local-head-tree: {head_tree}")
    emit(f"baseline-commit: {baseline}")
    emit(f"baseline-tree: {git(repo, 'rev-parse', baseline + '^{tree}')}")
    emit(f"worktree-clean: {str(not status).lower()}")
    if status:
        emit("worktree-status:")
        lines.extend("  " + row for row in status.splitlines())
    emit(f"branch: {git(repo, 'rev-parse', '--abbrev-ref', 'HEAD')}")
    emit()

    remote = subprocess.run(
        ["git", "-C", repo, "ls-remote", "origin", "refs/heads/main", "refs/tags/v0.2.0-preview.7"],
        capture_output=True,
        text=True,
    ).stdout.splitlines()
    remote_main = next((line.split()[0] for line in remote if line.endswith("refs/heads/main")), "")
    tag_object = next((line.split()[0] for line in remote if line.endswith("refs/tags/v0.2.0-preview.7")), "")
    emit("[remote]")
    emit(f"origin-main: {remote_main}")
    emit(f"tag-object v0.2.0-preview.7: {tag_object}")
    emit(f"tag-peeled-target: {git(repo, 'rev-parse', 'v0.2.0-preview.7^{commit}')}")
    emit(f"ahead-behind HEAD...origin/main: {git(repo, 'rev-list', '--left-right', '--count', 'HEAD...origin/main')}")
    emit()

    emit("[ancestry]")
    for name, ancestor in [
        ("baseline", baseline),
        ("qualified-implementation-eafd632", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6"),
        ("stage6-entry-2d1e8ec", "2d1e8ec88e397d0c36b7f258eb1a1c0840e56157"),
    ]:
        strict = subprocess.run(
            ["git", "-C", repo, "merge-base", "--is-ancestor", ancestor, "HEAD"]
        ).returncode == 0
        emit(f"{name}: strict-ancestor-of-HEAD={str(strict).lower()}")
    emit(f"src-changed-since-eafd632: {git(repo, 'diff', '--name-only', 'eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6', 'HEAD', '--', 'src') or '(none)'}")
    emit()

    emit("[release]")
    version = ""
    for candidate in git(repo, "ls-files", "*.csproj", "*.props").splitlines():
        text = blob(repo, head, candidate).decode("utf-8", "replace")
        for line in text.splitlines():
            if "<Version>" in line:
                version = line.split("<Version>")[1].split("</Version>")[0].strip()
                break
        if version:
            break
    emit(f"version: {version}")
    emit(f"tag: v0.2.0-preview.7 -> {git(repo, 'rev-parse', 'v0.2.0-preview.7^{commit}')}")
    emit()

    emit("[sealed-evidence-packages]")
    manifests = sorted(
        path for path in git(repo, "ls-files").splitlines() if path.endswith("/manifest.json")
    )
    lineage = [
        "docs/reviews/ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.5-RELEASE-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-WORKFLOW-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-EXEC-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.5-PUBLICATION-01-evidence",
        "docs/decisions/adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence",
        "docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-final-owner-01-evidence",
        "docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-m54-owner-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01-evidence",
        "docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence",
    ]
    total_entries = 0
    total_mismatches = 0
    lineage_entries = 0
    lineage_mismatches = 0
    for manifest_path in manifests:
        package = manifest_path.rsplit("/", 1)[0]
        try:
            manifest = json.loads(blob(repo, head, manifest_path).decode("utf-8-sig"))
        except Exception as exception:  # pragma: no cover - defensive
            emit(f"{package}: UNREADABLE ({exception})")
            total_mismatches += 1
            lineage_mismatches += 1
            continue
        entries = manifest.get("files", [])
        mismatches = []
        for entry in entries:
            data = blob(repo, head, f"{package}/{entry['path']}")
            if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest().lower() != entry["sha256"].lower():
                mismatches.append(entry["path"])
        total_entries += len(entries)
        total_mismatches += len(mismatches)
        marker = " [LINEAGE]" if package in lineage else ""
        if package in lineage:
            lineage_entries += len(entries)
            lineage_mismatches += len(mismatches)
        state = "EXACT" if not mismatches else "MISMATCH:" + ",".join(mismatches)
        emit(f"{package}: entries={len(entries)} {state}{marker}")
    emit(f"packages: {len(manifests)}")
    emit(f"entries: {total_entries}")
    emit(f"mismatches: {total_mismatches}")
    emit(f"lineage-packages: {len(lineage)}")
    emit(f"lineage-entries: {lineage_entries}")
    emit(f"lineage-mismatches: {lineage_mismatches}")
    emit()

    emit("[production-delta]")
    delta = git(repo, "diff", "--stat", baseline, "HEAD", "--", "src", "tests", ".github")
    emit(delta or "(no changes)")
    emit()

    output.write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(f"wrote {output} ({len(lines)} lines)")
    print(f"sealed packages: {len(manifests)}, entries: {total_entries}, mismatches: {total_mismatches}")
    return 0 if total_mismatches == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main())
