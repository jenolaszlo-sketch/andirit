#!/usr/bin/env python3
"""Verify disposable QUAL-04 exports against pinned Git trees.

For an exact-byte export, every tracked file must be present with its exact
Git blob bytes. Extra files are accepted only when they are recognized build
outputs (a `bin`, `obj`, or `TestResults` path segment). A checkout-profile
export is separately classified: every mismatch must be explained solely by
CRLF-to-LF normalization.
"""
from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path


def blob_hash(data: bytes) -> str:
    digest = hashlib.sha1()
    digest.update(f"blob {len(data)}\0".encode("ascii"))
    digest.update(data)
    return digest.hexdigest()


def git_blobs(repo: Path, revision: str) -> dict[str, str]:
    output = subprocess.check_output(
        ["git", "-C", str(repo), "ls-tree", "-r", "--full-name", revision],
        text=True,
    )
    result: dict[str, str] = {}
    for line in output.splitlines():
        metadata, path = line.split("\t", 1)
        mode, object_type, sha = metadata.split()
        if object_type == "blob":
            result[path.replace("\\", "/")] = sha
    return result


def is_build_output(path: str) -> bool:
    return any(segment in {"bin", "obj", "TestResults"} for segment in path.split("/"))


def verify(repo: Path, revision: str, export: Path, profile: str) -> dict[str, object]:
    blobs = git_blobs(repo, revision)
    present = {
        path.relative_to(export).as_posix(): path
        for path in export.rglob("*")
        if path.is_file()
    }
    missing = sorted(set(blobs) - set(present))
    extra = sorted(set(present) - set(blobs))
    mismatched: list[str] = []
    line_ending_only: list[str] = []
    other_mismatched: list[str] = []

    for relative, sha in sorted(blobs.items()):
        if relative in missing:
            continue
        data = present[relative].read_bytes()
        if blob_hash(data) == sha:
            continue
        mismatched.append(relative)
        if profile == "checkout-crlf":
            normalized = data.replace(b"\r\n", b"\n")
            if blob_hash(normalized) == sha:
                line_ending_only.append(relative)
            else:
                other_mismatched.append(relative)
        else:
            other_mismatched.append(relative)

    build_output_extras = [path for path in extra if is_build_output(path)]
    unexpected_extras = [path for path in extra if not is_build_output(path)]
    if profile == "exact":
        accepted = not missing and not other_mismatched and not unexpected_extras
    elif profile == "checkout-crlf":
        accepted = not missing and not other_mismatched and not unexpected_extras
    else:
        raise ValueError(f"unknown profile: {profile}")

    return {
        "profile": profile,
        "revision": revision,
        "export": str(export),
        "tracked_entries": len(blobs),
        "exported_files": len(present),
        "missing_count": len(missing),
        "missing": missing,
        "mismatched_count": len(mismatched),
        "mismatched": mismatched,
        "line_ending_only_count": len(line_ending_only),
        "other_mismatched_count": len(other_mismatched),
        "other_mismatched": other_mismatched,
        "extra_count": len(extra),
        "build_output_extra_count": len(build_output_extras),
        "unexpected_extra_count": len(unexpected_extras),
        "unexpected_extras": unexpected_extras,
        "accepted": accepted,
    }


def main() -> int:
    if len(sys.argv) != 8:
        print(
            "usage: verify-archives.py <repo> <r3-revision> <r3-export> "
            "<r2-revision> <r2-export> <r3-checkout-export> <output-json>",
            file=sys.stderr,
        )
        return 2
    repo = Path(sys.argv[1]).resolve()
    r3_revision, r3_export = sys.argv[2], Path(sys.argv[3])
    r2_revision, r2_export = sys.argv[4], Path(sys.argv[5])
    checkout_export = Path(sys.argv[6])
    output = Path(sys.argv[7])

    result = {
        "method": (
            "git ls-tree pinned revisions; SHA-1 Git blob hash over exported bytes; "
            "checkout profile additionally normalizes CRLF to LF solely to classify "
            "line-ending differences"
        ),
        "exact_exports": [
            verify(repo, r3_revision, r3_export.resolve(), "exact"),
            verify(repo, r2_revision, r2_export.resolve(), "exact"),
        ],
        "checkout_profile": verify(
            repo, r3_revision, checkout_export.resolve(), "checkout-crlf"
        ),
    }
    output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(json.dumps({
        "r3": {
            key: result["exact_exports"][0][key]
            for key in (
                "tracked_entries", "exported_files", "missing_count",
                "mismatched_count", "extra_count", "build_output_extra_count",
                "unexpected_extra_count", "accepted",
            )
        },
        "r2": {
            key: result["exact_exports"][1][key]
            for key in (
                "tracked_entries", "exported_files", "missing_count",
                "mismatched_count", "extra_count", "build_output_extra_count",
                "unexpected_extra_count", "accepted",
            )
        },
    }, indent=2))
    return 0 if all(item["accepted"] for item in result["exact_exports"]) else 1


if __name__ == "__main__":
    sys.exit(main())
