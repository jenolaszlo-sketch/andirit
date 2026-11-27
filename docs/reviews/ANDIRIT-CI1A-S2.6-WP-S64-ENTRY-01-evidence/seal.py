"""Seal the S6.4 entry assessment and check the exact reviewed input blobs."""

import argparse
import hashlib
import json
import subprocess

COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01"
REVIEW = f"docs/reviews/{COMMISSION}"
EVIDENCE = f"{REVIEW}-evidence"
BASELINE = "99b04c0ebb5dfa43065581bb6489fe2468d9a5aa"
MANIFEST = f"{EVIDENCE}/manifest.json"
PATHS = {
    *(f"{REVIEW}{suffix}.md" for suffix in (
        "", "-traceability-matrix", "-qualification-plan", "-successor-commission")),
    *(f"{EVIDENCE}/{path}" for path in (
        ".gitattributes", "README.md", "baseline-verification.txt", "reviewed-inputs.json",
        "mirror-graph-only.json", "mirror-integrated.json", "mirror-observer/ObserveMirror.csproj",
        "mirror-observer/Program.cs", "seal.py")),
}


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


def verify_delta(base, commit, paths):
    parents = git("show", "-s", "--format=%P", commit).decode().split()
    changes = git("diff-tree", "--no-commit-id", "--no-renames", "--name-status",
                  "-r", base, commit).decode().splitlines()
    if parents != [base] or set(changes) != {f"A\t{path}" for path in paths}:
        raise ValueError(f"Unexpected parents or path changes in {commit}")


def build_manifest(revision):
    payload = describe(revision)
    verify_delta(BASELINE, payload["commit"], PATHS)
    return {
        "schema": 1,
        "commission": COMMISSION,
        "reviewed_revision": describe(BASELINE),
        "payload": payload,
        "entries": [member(payload["commit"], path) for path in sorted(PATHS)],
    }


def verify(revision):
    commit = describe(revision)["commit"]
    raw = git("cat-file", "blob", f"{commit}:{MANIFEST}")
    saved = json.loads(raw)
    payload = saved["payload"]["commit"]
    verify_delta(payload, commit, {MANIFEST})
    if saved != build_manifest(payload):
        raise ValueError("Seal differs from the exact payload")
    if saved["entries"] != [member(commit, path) for path in sorted(PATHS)]:
        raise ValueError("Evidence differs from the sealed payload")
    inventory = json.loads(git("cat-file", "blob", f"{commit}:{EVIDENCE}/reviewed-inputs.json"))
    inputs = inventory["entries"]
    if (inventory["revision"] != BASELINE or not inputs or
            len({entry["path"] for entry in inputs}) != len(inputs) or
            inputs != [member(BASELINE, entry["path"], "blob") for entry in inputs]):
        raise ValueError("Reviewed input inventory differs from baseline Git objects")
    print(f"Verified {len(PATHS)} sealed payload files and {len(inputs)} reviewed inputs")
    print(f"Payload {payload} tree {saved['payload']['tree']}")
    print(f"Seal {commit} SHA-256 {hashlib.sha256(raw).hexdigest()}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("create", "verify"))
    parser.add_argument("revision")
    args = parser.parse_args()
    if args.mode == "create":
        print(json.dumps(build_manifest(args.revision), indent=2))
    else:
        verify(args.revision)
