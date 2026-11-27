"""Seal the S64-MIRROR-REMED-01 evidence and bind the implementation blobs."""

import argparse
import hashlib
import json
import subprocess

COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01"
REVIEW = f"docs/reviews/{COMMISSION}.md"
EVIDENCE = f"docs/reviews/{COMMISSION}-evidence"
BASE = "dda4358bc2588368efc6ab6a7bd774857d998026"
MANIFEST = f"{EVIDENCE}/manifest.json"
INPUTS = f"{EVIDENCE}/reviewed-inputs.json"
SEAL_ADDITIONS = {INPUTS, f"{EVIDENCE}/hosted-ci.md"}
IMPLEMENTATION_ROOTS = ("src/", "tests/", ".github/", "Directory.Build.props",
                        "Directory.Build.targets", "Andirit.slnx")


def git(*args):
    return subprocess.check_output(["git", *args])


def rev_parse(revision):
    return git("rev-parse", "--verify", revision).decode().strip()


def describe(revision):
    commit = rev_parse(revision + "^{commit}")
    return {"commit": commit, "tree": rev_parse(commit + "^{tree}")}


def member(commit, path, blob_key="git_blob"):
    blob = rev_parse(f"{commit}:{path}")
    data = git("cat-file", "blob", blob)
    return {"path": path, blob_key: blob, "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest()}


def changed(base, commit):
    return set(git("diff-tree", "--no-commit-id", "--no-renames", "--name-only",
                   "-r", base, commit).decode().splitlines())


def implementation_paths(commit):
    return sorted(path for path in changed(BASE, commit)
                  if path.startswith(IMPLEMENTATION_ROOTS))


def evidence_paths(commit):
    listed = git("ls-tree", "-r", "--name-only", commit, "--", EVIDENCE).decode().splitlines()
    return sorted({REVIEW, *(path for path in listed if path != MANIFEST)})


def build_inputs(implementation):
    parents = git("show", "-s", "--format=%P", implementation).decode().split()
    if parents != [BASE]:
        raise ValueError("The implementation commit is not a direct child of the entry seal")
    expected = set(implementation_paths(implementation))
    if not expected:
        raise ValueError("The implementation commit changes no source or test files")
    return {
        "schema": 1,
        "commission": COMMISSION,
        "baseline": describe(BASE),
        "implementation": describe(implementation),
        "entries": [member(implementation, path) for path in sorted(expected)],
    }


def build_manifest(payload):
    parents = git("show", "-s", "--format=%P", payload).decode().split()
    if len(parents) != 1:
        raise ValueError("The payload commit must have exactly one parent")
    implementation = rev_parse(parents[0] + "^{commit}")
    additions = changed(implementation, payload)
    if additions != SEAL_ADDITIONS:
        raise ValueError(f"Payload additions are {sorted(additions)}, expected {sorted(SEAL_ADDITIONS)}")
    paths = evidence_paths(payload)
    if MANIFEST in paths:
        raise ValueError("The manifest must not seal itself")
    raw_inputs = git("cat-file", "blob", f"{payload}:{INPUTS}")
    inputs = json.loads(raw_inputs)
    expected_inputs = build_inputs(implementation)
    if inputs != expected_inputs:
        raise ValueError("reviewed-inputs.json differs from the implementation blobs")
    return {
        "schema": 1,
        "commission": COMMISSION,
        "reviewed_revision": describe(BASE),
        "implementation": describe(implementation),
        "payload": describe(payload),
        "seal_additions": sorted(SEAL_ADDITIONS),
        "entries": [member(payload, path) for path in paths],
    }


def verify(revision):
    seal = describe(revision)["commit"]
    raw = git("cat-file", "blob", f"{seal}:{MANIFEST}")
    saved = json.loads(raw)
    payload = saved["payload"]["commit"]
    if changed(payload, seal) != {MANIFEST}:
        raise ValueError("The seal commit must add only manifest.json")
    if saved != build_manifest(payload):
        raise ValueError("Seal differs from the exact payload")
    implementation = saved["implementation"]["commit"]
    if changed(implementation, payload) != SEAL_ADDITIONS:
        raise ValueError("Payload differs from the sealed additions")
    if saved["entries"] != [member(seal, entry["path"]) for entry in saved["entries"]]:
        raise ValueError("Evidence differs from the sealed payload")
    inputs = json.loads(git("cat-file", "blob", f"{implementation}:{INPUTS}"))
    expected = build_inputs(implementation)
    if inputs != expected or inputs["entries"] != [
            member(implementation, entry["path"]) for entry in inputs["entries"]]:
        raise ValueError("Reviewed input inventory differs from the implementation")
    print(f"Verified {len(saved['entries'])} sealed evidence files and "
          f"{len(inputs['entries'])} implementation inputs")
    print(f"Implementation {implementation} tree {saved['implementation']['tree']}")
    print(f"Payload {payload} tree {saved['payload']['tree']}")
    print(f"Seal {seal} SHA-256 {hashlib.sha256(raw).hexdigest()}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("inputs", "create", "verify"))
    parser.add_argument("revision")
    args = parser.parse_args()
    if args.mode == "inputs":
        print(json.dumps(build_inputs(rev_parse(args.revision + "^{commit}")), indent=2))
    elif args.mode == "create":
        print(json.dumps(build_manifest(rev_parse(args.revision + "^{commit}")), indent=2))
    else:
        verify(args.revision)
