"""Bind the S6.3 acceptance package and state changes to immutable Git blobs."""
import argparse
import hashlib
import json
import subprocess

KEY = "ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01"
REVIEW = f"docs/reviews/{KEY}"
EVIDENCE = f"{REVIEW}-evidence"
MANIFEST = f"{EVIDENCE}/manifest.json"
SOURCE = {
    "tested_r2": "a96c19a844f8ea1719508198b47df23c4e203571",
    "producer_evidence_erratum": "ab2bd168ab2436ffb2a07de8466b33cecd79720b",
    "prepublication_main_and_replay": "f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60",
    "independent_payload": "0f520cf61adaf1a7336fd4a360b20f6d270029be",
    "independent_seal": "329e8f775ab9fba10f61510c7f80a312525d13ff",
}
HISTORICAL_Q1 = {
    "commit": "a69c13ce447839330793be2e4cbc7a1dd732d1dd",
    "tree": "a57df9a83cb692acef2c449b0c7eba657887f6d0",
    "archive": f"{EVIDENCE}/historical-q1.bundle",
}
PATHS = {
    f"{REVIEW}.md", f"{REVIEW}-P03.md", "README.md", "ROADMAP.md",
    "docs/agent-code-intelligence-roadmap.md",
    "docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md",
    *(f"{EVIDENCE}/{name}" for name in (
        ".gitattributes", "README.md", "prepublication.txt", "seal.py", "historical-q1.bundle")),
}


def git(*args):
    return subprocess.check_output(["git", *args])


def describe(revision):
    commit = git("rev-parse", revision + "^{commit}").decode().strip()
    tree = git("rev-parse", commit + "^{tree}").decode().strip()
    return {"commit": commit, "tree": tree}


def parent(commit):
    return git("show", "-s", "--format=%P", commit).decode().strip()


def member(commit, path):
    blob = git("rev-parse", f"{commit}:{path}").decode().strip()
    data = git("cat-file", "blob", blob)
    return {"path": path, "git_blob": blob, "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest()}


def changes(base, end):
    return set(git("diff", "--no-renames", "--name-only", "-z", base, end).decode().strip("\0").split("\0"))


def manifest(payload):
    baseline = SOURCE["independent_seal"]
    commit = payload["commit"]
    if parent(commit) != baseline or changes(baseline, commit) != PATHS:
        raise ValueError("Acceptance payload must directly extend the independent seal with the exact approved paths")
    return {
        "schema": 1,
        "commission": KEY,
        "revisions": {label: describe(revision) for label, revision in SOURCE.items()},
        "historical_q1": HISTORICAL_Q1,
        "payload": payload,
        "entries": [member(commit, path) for path in sorted(PATHS)],
    }


def verify(revision):
    seal = describe(revision)
    commit = seal["commit"]
    data = git("cat-file", "blob", f"{commit}:{MANIFEST}")
    saved = json.loads(data.decode("utf-8", errors="strict"))
    payload = describe(saved["payload"]["commit"])
    if parent(commit) != payload["commit"] or changes(payload["commit"], commit) != {MANIFEST}:
        raise ValueError("The seal must directly follow the payload and add only the manifest")
    if manifest(payload) != saved or [member(commit, path) for path in sorted(PATHS)] != saved["entries"]:
        raise ValueError("The manifest does not match the complete payload and sealed blob bytes")
    print(f"Verified {len(PATHS)} acceptance payload files from committed objects")
    print(f"Acceptance payload {payload['commit']} tree {payload['tree']}")
    print(f"Acceptance seal {commit} tree {seal['tree']}")
    print(f"Manifest SHA-256 {hashlib.sha256(data).hexdigest()}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("create", "verify"))
    parser.add_argument("revision")
    args = parser.parse_args()
    if args.mode == "create":
        print(json.dumps(manifest(describe(args.revision)), indent=2))
    else:
        verify(args.revision)
