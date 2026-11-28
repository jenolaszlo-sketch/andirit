"""Independent seal verification for S64-MIRROR-REMED-01.

Does not import or execute any producer script.
"""

import hashlib
import json
import subprocess
import sys

REPO = sys.argv[1]
COMMISSION = "ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01"
EVIDENCE = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01-evidence"
REVIEW = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-MIRROR-REMED-01.md"
MANIFEST = EVIDENCE + "/manifest.json"
ENTRY = "dda4358bc2588368efc6ab6a7bd774857d998026"
IMPL = "ec984bfe63e5fbe13047cee8920e00b634ab17b0"
PAYLOAD = "b167fed057dfa2b2b1e4da1fde26826ff5f01ec9"
SEAL = "870b600acf1490337af9f483f8894a5e8166a90e"
SEAL_SHA = "b473dd41742510d5676c1d8c033244afbc2c769dc52bf3941fa776769a6704f2"
IMPL_PREFIXES = ("src/", "tests/", ".github/", "Directory.Build.props",
                 "Directory.Build.targets", "Andirit.slnx")


def git(*args):
    return subprocess.check_output(["git", "-C", REPO, *args])


def text(*args):
    return git(*args).decode().strip()


def blob(commit, path):
    return git("cat-file", "blob", f"{commit}:{path}")


def member(commit, path):
    data = blob(commit, path)
    return {"path": path, "git_blob": text("rev-parse", f"{commit}:{path}"),
            "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()}


def tree(commit):
    return text("rev-parse", f"{commit}^{{tree}}")


def changed(a, b):
    return set(text("diff-tree", "--no-commit-id", "--no-renames",
                    "--name-only", "-r", a, b).splitlines())


report = {"repository": REPO, "checks": {}, "entries": 0, "inputs": 0}
for name, commit in (("entry", ENTRY), ("implementation", IMPL),
                     ("payload", PAYLOAD), ("seal", SEAL)):
    report["checks"][name + "Tree"] = tree(commit)
assert tree(IMPL) == "8ec0eb7a5e647a171f54fa8969dadbb6c65da179"
assert tree(PAYLOAD) == "bb9a0386fdfc4f4a3e9c744b1611c154f2ebca40"
assert text("show", "-s", "--format=%P", SEAL).split() == [PAYLOAD]
assert text("show", "-s", "--format=%P", PAYLOAD).split() == [IMPL]
assert changed(PAYLOAD, SEAL) == {MANIFEST}

raw = blob(SEAL, MANIFEST)
assert hashlib.sha256(raw).hexdigest() == SEAL_SHA
manifest = json.loads(raw)
assert manifest["schema"] == 1
assert manifest["commission"] == COMMISSION
assert manifest["reviewed_revision"]["commit"] == ENTRY
assert manifest["implementation"]["commit"] == IMPL
assert manifest["payload"]["commit"] == PAYLOAD
assert manifest["payload_additions"] == sorted(changed(IMPL, PAYLOAD))
assert set(manifest["payload_additions"]) >= {
    EVIDENCE + "/hosted-ci.md", EVIDENCE + "/reviewed-inputs.json"}
assert not any(path.startswith(IMPL_PREFIXES)
               for path in manifest["payload_additions"])

listed = text("ls-tree", "-r", "--name-only", SEAL, "--", EVIDENCE).splitlines()
expected_paths = sorted({REVIEW, *(p for p in listed if p != MANIFEST)})
assert [entry["path"] for entry in manifest["entries"]] == expected_paths
for entry in manifest["entries"]:
    assert entry == member(SEAL, entry["path"]), entry["path"]
report["entries"] = len(manifest["entries"])

inputs = json.loads(blob(PAYLOAD, EVIDENCE + "/reviewed-inputs.json"))
assert inputs["baseline"]["commit"] == ENTRY
assert inputs["implementation"]["commit"] == IMPL
expected_inputs = sorted(path for path in changed(ENTRY, IMPL)
                         if path.startswith(IMPL_PREFIXES))
assert [entry["path"] for entry in inputs["entries"]] == expected_inputs
for entry in inputs["entries"]:
    assert entry == member(IMPL, entry["path"]), entry["path"]
report["inputs"] = len(inputs["entries"])

store_dir = EVIDENCE + "/affected-stores/"
sums = {}
for line in blob(SEAL, store_dir + "SHA256SUMS.txt").decode().splitlines():
    digest, name = line.split("  ", 1)
    sums[name] = digest
for name, digest in sums.items():
    assert hashlib.sha256(blob(SEAL, store_dir + name)).hexdigest() == digest
report["affectedStores"] = sorted(sums)
report["sealSha256"] = SEAL_SHA
print(json.dumps(report, indent=2))
