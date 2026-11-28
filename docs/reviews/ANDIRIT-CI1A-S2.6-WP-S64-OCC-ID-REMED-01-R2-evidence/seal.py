"""Seal the R2 framework/compatibility remediation evidence (self-excluding)."""

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R2"
REVIEW = f"docs/reviews/{COMMISSION}.md"
EVIDENCE = f"docs/reviews/{COMMISSION}-evidence"
EXTRA = ["docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01-R1.md"]
MANIFEST = f"{EVIDENCE}/manifest.json"


def git(*args):
    return subprocess.check_output(["git", "-C", str(ROOT), *args])


def text(*args):
    return git(*args).decode().strip()


def blob(commit, path):
    return git("cat-file", "blob", f"{commit}:{path}")


def member(commit, path):
    data = blob(commit, path)
    return {
        "path": path,
        "git_blob": text("rev-parse", f"{commit}:{path}"),
        "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
    }


def delta_status(a, b):
    return text(
        "diff-tree", "--no-commit-id", "--no-renames", "--name-status", "-r", a, b
    ).splitlines()


def paths(commit):
    listed = text("ls-tree", "-r", "--name-only", commit, "--", EVIDENCE).splitlines()
    return sorted({REVIEW, *EXTRA, *(path for path in listed if path != MANIFEST)})


def build(payload):
    return {
        "schema": 1,
        "commission": COMMISSION,
        "payload": {
            "commit": payload,
            "tree": text("rev-parse", f"{payload}^{{tree}}"),
        },
        "priorSeals": {
            "producerR1": "880a888da2256608a91010026b1c3f39d0de76b6",
            "qualificationR1": "c6c2dcfd5697460318634d34d26eba6b88f6d4e1",
            "investigation": "81102d21b4d6f6ed510b0555bb0d034aa1705c44",
            "reentry": "1776ea8ca12ba54dc85e817484711663480569a2",
            "blockedD3": "d79617727dcdcdbc0ed659c1c69dc0e175787806",
        },
        "entries": [member(payload, path) for path in paths(payload)],
    }


def verify(revision):
    seal = text("rev-parse", revision + "^{commit}")
    raw = blob(seal, MANIFEST)
    saved = json.loads(raw)
    payload = saved["payload"]["commit"]
    if delta_status(payload, seal) != ["A\t" + MANIFEST]:
        raise ValueError("seal commit must add only manifest.json")
    if saved != build(payload):
        raise ValueError("manifest differs from payload")
    if saved["entries"] != [member(seal, e["path"]) for e in saved["entries"]]:
        raise ValueError("evidence differs from seal")
    print(f"Verified {len(saved['entries'])} R2 evidence files")
    print(f"Payload {payload} tree {saved['payload']['tree']}")
    print(f"Seal {seal} SHA-256 {hashlib.sha256(raw).hexdigest()}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=("create", "verify"))
    parser.add_argument("revision")
    args = parser.parse_args()
    if args.mode == "create":
        print(json.dumps(build(text("rev-parse", args.revision + "^{commit}")), indent=2))
    else:
        verify(args.revision)
