"""Seal or verify the independent evidence using committed Git object bytes."""
import argparse
import hashlib
import json
from pathlib import PurePosixPath
import subprocess

PACKAGE = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence"
REPORT = PACKAGE.removesuffix("-evidence") + ".md"
MANIFEST = f"{PACKAGE}/manifest.json"
COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1"
REVISIONS = {
    "frozen_r1": "d3c9359d1abd8c89a7214e37f074802e5d2fa238",
    "previous_evidence": "dd484af2d32ff1f5b7951a85f4f3104c4b4b5402",
    "independent_q1": "a69c13ce447839330793be2e4cbc7a1dd732d1dd",
    "tested_r2": "a96c19a844f8ea1719508198b47df23c4e203571",
    "r2_and_erratum_seal": "ab2bd168ab2436ffb2a07de8466b33cecd79720b",
    "published_main_and_clean_replay_seal": "f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60",
}


def git(*args):
    return subprocess.check_output(["git", *args])


def resolve(revision):
    commit = git("rev-parse", "--verify", revision + "^{commit}").decode().strip()
    tree = git("rev-parse", "--verify", commit + "^{tree}").decode().strip()
    return {"commit": commit, "tree": tree}


def snapshot(revision, scope, excluded=()):
    entries = []
    raw = git("ls-tree", "-r", "-z", revision, "--", scope)
    for row in filter(None, raw.split(b"\0")):
        metadata, path = row.decode().split("\t", 1)
        mode, kind, blob = metadata.split(" ")
        if path in excluded:
            continue
        if mode != "100644" or kind != "blob":
            raise ValueError(f"Unexpected Git entry {path}: {metadata}")
        if set(PurePosixPath(path).parts) & {"..", "."}:
            raise ValueError(f"Invalid path {path}")
        data = git("cat-file", "blob", blob)
        entries.append({"path": path, "git_blob": blob, "bytes": len(data),
                        "sha256": hashlib.sha256(data).hexdigest()})
    return entries


def contents(payload):
    parent = git("show", "-s", "--format=%P", payload["commit"]).decode().strip()
    if parent != REVISIONS["published_main_and_clean_replay_seal"]:
        raise ValueError("Payload must be a direct child of the published baseline")
    entries = snapshot(payload["commit"], PACKAGE)
    report = snapshot(payload["commit"], REPORT)
    if not entries or len(report) != 1 or MANIFEST in {entry["path"] for entry in entries}:
        raise ValueError("Payload membership or manifest exclusion failed")
    expected = {entry["path"] for entry in entries + report}
    actual = git("diff", "--name-only", "-z", parent, payload["commit"]).decode().split("\0")
    if set(filter(None, actual)) != expected:
        raise ValueError("Payload commit includes changes outside the new evidence package and report")
    return {
        "schema": 1,
        "commission": COMMISSION,
        "revisions": {label: resolve(commit) for label, commit in REVISIONS.items()},
        "payload": payload,
        "evidence": entries,
        "report": report[0],
    }


def verify(revision):
    sealed = resolve(revision)
    encoded = git("cat-file", "blob", f"{sealed['commit']}:{MANIFEST}")
    recorded = json.loads(encoded.decode("utf-8", errors="strict"))
    payload = resolve(recorded["payload"]["commit"])
    expected = contents(payload)
    if recorded != expected:
        raise ValueError("Manifest differs from committed payload and source objects")
    parent = git("show", "-s", "--format=%P", sealed["commit"]).decode().strip()
    delta = git("diff", "--name-status", parent, sealed["commit"]).decode().strip()
    if parent != payload["commit"] or delta != f"A\t{MANIFEST}":
        raise ValueError("Seal must add only its self-excluding manifest")
    evidence = snapshot(sealed["commit"], PACKAGE, (MANIFEST,))
    report = snapshot(sealed["commit"], REPORT)
    if evidence != recorded["evidence"] or report != [recorded["report"]]:
        raise ValueError("Sealed object bytes, paths or membership differ")
    print(f"Verified {len(evidence)} evidence entries and formal report from committed blobs")
    print("Payload", payload["commit"], payload["tree"])
    print("Seal", sealed["commit"], sealed["tree"])
    print("Manifest SHA-256", hashlib.sha256(encoded).hexdigest())


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("create", "verify"))
    parser.add_argument("revision")
    args = parser.parse_args()
    if args.mode == "create":
        print(json.dumps(contents(resolve(args.revision)), indent=2))
    else:
        verify(args.revision)
