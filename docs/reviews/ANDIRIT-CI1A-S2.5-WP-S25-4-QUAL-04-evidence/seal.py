#!/usr/bin/env python3
"""Seal or verify the QUAL-04 evidence package.

The manifest binds every regular file in this evidence directory except
``manifest.json`` itself. Paths are sorted by ordinal relative POSIX path;
each entry records the exact byte length and SHA-256 digest. The report is
outside the package and is bound by Git.

Usage:
    python seal.py [--verify] [package-root]
"""
from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path


COMMISSION = "ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04"
SCHEME = (
    "SHA-256 exact bytes and lengths; ordinal relative paths; manifest "
    "self-excluded; report bound by Git"
)


def expected_manifest(root: Path) -> dict[str, object]:
    manifest_path = root / "manifest.json"
    files: list[dict[str, object]] = []
    for path in sorted(root.rglob("*"), key=lambda item: item.relative_to(root).as_posix()):
        if not path.is_file() or path == manifest_path:
            continue
        if path.is_symlink():
            raise ValueError(f"evidence symlink is not allowed: {path}")
        data = path.read_bytes()
        files.append({
            "path": path.relative_to(root).as_posix(),
            "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest(),
        })
    return {
        "commission": COMMISSION,
        "scheme": SCHEME,
        "scope": "QUAL-04 evidence directory only; report bound by Git",
        "startingHead": "dba6597284c655b7fd7d2645c1ecb9486fe6d693",
        "startingTree": "fda08e42428625ea96e0deccf3a1d97eff7a99d1",
        "reviewedHead": "8b83e0558f4c757272cd60a1c81b8dbc6fd7873f",
        "reviewedTree": "0f89a469858abd2126f3be01cd060bc534e930c0",
        "negativeBaselineHead": "25bc6e3b4305c7ce088deb36cfb000138dc76648",
        "negativeBaselineTree": "2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3",
        "reviewerSession": "ses_e1257f799ffefw0spAJ9UdV67w",
        "files": files,
    }


def main() -> int:
    verify = "--verify" in sys.argv[1:]
    positional = [value for value in sys.argv[1:] if not value.startswith("--")]
    root = Path(positional[0]).resolve() if positional else Path(__file__).resolve().parent
    if not root.is_dir():
        raise FileNotFoundError(root)

    manifest_path = root / "manifest.json"
    if manifest_path.is_symlink():
        raise ValueError("manifest.json must not be a symlink")
    expected = expected_manifest(root)

    if verify:
        if not manifest_path.is_file():
            raise FileNotFoundError(manifest_path)
        actual = json.loads(manifest_path.read_text(encoding="utf-8"))
        if actual != expected:
            raise ValueError("manifest contents, membership, lengths or digests differ")
        mode = "verified"
    else:
        manifest_path.write_text(
            json.dumps(expected, indent=2) + "\n", encoding="utf-8", newline="\n"
        )
        mode = "sealed"

    manifest_bytes = manifest_path.read_bytes()
    print(json.dumps({
        "mode": mode,
        "entries": len(expected["files"]),
        "manifestBytes": len(manifest_bytes),
        "manifestSha256": hashlib.sha256(manifest_bytes).hexdigest(),
    }, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
