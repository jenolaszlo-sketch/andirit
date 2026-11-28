"""Seal the independent qualification evidence."""

import argparse
import hashlib
import json
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-QUAL-01"
REVIEW = f"docs/reviews/{COMMISSION}.md"
EVIDENCE = f"docs/reviews/{COMMISSION}-evidence"
MANIFEST = f"{EVIDENCE}/manifest.json"


def git(*args):
    return subprocess.check_output(["git", "-C", str(ROOT), *args])


def text(*args):
    return git(*args).decode().strip()


def blob(commit, path):
    return git("cat-file", "blob", f"{commit}:{path}")


def member(commit, path):
    data = blob(commit, path)
    return {"path": path, "git_blob": text("rev-parse", f"{commit}:{path}"),
            "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def changed(a, b):
    return set(text("diff-tree", "--no-commit-id", "--no-renames",
                    "--name-only", "-r", a, b).splitlines())


def paths(commit):
    listed = text("ls-tree", "-r", "--name-only", commit, "--", EVIDENCE).splitlines()
    return sorted({REVIEW, *(path for path in listed if path != MANIFEST)})


def build(payload):
    parents = text("show", "-s", "--format=%P", payload).split()
    if len(parents) != 1:
        raise ValueError("payload must have one parent")
    return {
        "schema": 1,
        "commission": COMMISSION,
        "payload": {"commit": payload, "tree": text("rev-parse", f"{payload}^{{tree}}")},
        "producerSeal": "870b600acf1490337af9f483f8894a5e8166a90e",
        "entries": [member(payload, path) for path in paths(payload)],
    }


def verify(revision):
    seal = text("rev-parse", revision + "^{commit}")
    raw = blob(seal, MANIFEST)
    saved = json.loads(raw)
    payload = saved["payload"]["commit"]
    if changed(payload, seal) != {MANIFEST}:
        raise ValueError("seal commit must add only manifest.json")
    if saved != build(payload):
        raise ValueError("manifest differs from payload")
    if saved["entries"] != [member(seal, e["path"]) for e in saved["entries"]]:
        raise ValueError("evidence differs from seal")
    print(f"Verified {len(saved['entries'])} qualification evidence files")
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
