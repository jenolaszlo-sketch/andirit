"""Verify the frozen S2.0 R1 package from a checkout or clean archive.

Archive mode requires --trusted-manifest-sha256 (or ANDIRIT_R1_MANIFEST_SHA256).
Obtain that value from the source-bound Q2 handoff; this is the archive trust anchor.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import sys


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def git(root: Path, *args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=root, stderr=subprocess.DEVNULL, text=True).strip()


parser = argparse.ArgumentParser()
parser.add_argument("--manifest", type=Path)
parser.add_argument("--trusted-manifest-sha256", default=os.environ.get("ANDIRIT_R1_MANIFEST_SHA256"))
args = parser.parse_args()
here = Path(__file__).resolve().parent
manifest_path = (args.manifest or here / "r1-source-evidence-manifest.json").resolve()
manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
root = next((p for p in manifest_path.parents if (p / "Andirit.slnx").is_file()), None)
if root is None:
    raise SystemExit("FAIL: cannot locate extracted source root (Andirit.slnx missing)")

try:
    git_root = Path(git(root, "rev-parse", "--show-toplevel")).resolve()
    git_mode = True
except (subprocess.CalledProcessError, FileNotFoundError):
    git_root = None
    git_mode = False

manifest_bytes = manifest_path.read_bytes()
if not git_mode:
    if not args.trusted_manifest_sha256:
        raise SystemExit("FAIL: archive mode requires the independently supplied manifest SHA-256 trust anchor")
    if sha256(manifest_bytes) != args.trusted_manifest_sha256.lower():
        raise SystemExit("FAIL: manifest differs from the independently supplied trust anchor")

expected_groups = ("PlanningDocuments", "CorpusFixtures", "OracleSources", "EvidenceFiles")
seen = set()
total = 0
for group in expected_groups:
    items = manifest.get(group)
    if not isinstance(items, list) or not items:
        raise SystemExit(f"FAIL: missing or empty manifest group {group}")
    for item in items:
        rel = Path(item["Path"])
        if rel.is_absolute() or ".." in rel.parts:
            raise SystemExit(f"FAIL: unsafe manifest path {item['Path']}")
        key = rel.as_posix()
        if key in seen:
            raise SystemExit(f"FAIL: duplicate manifest path {key}")
        seen.add(key)
        path = root / rel
        if not path.is_file():
            raise SystemExit(f"FAIL: missing frozen file {key}")
        data = path.read_bytes()
        if len(data) != item["Bytes"]:
            raise SystemExit(f"FAIL: size drift {key}")
        if sha256(data) != item["Sha256"]:
            raise SystemExit(f"FAIL: hash drift {key}")
        total += 1

binding = manifest["SourceBinding"]
if git_mode:
    branch = git(git_root, "rev-parse", "--abbrev-ref", "HEAD")
    if branch != binding["Branch"]:
        raise SystemExit(f"FAIL: branch {branch!r} != frozen {binding['Branch']!r}")
    base_tree = git(git_root, "rev-parse", binding["StartingCommit"] + "^{tree}")
    if base_tree != binding["StartingTree"]:
        raise SystemExit("FAIL: pinned starting commit tree differs from source binding")
    subprocess.check_call(["git", "merge-base", "--is-ancestor", binding["StartingCommit"], "HEAD"], cwd=git_root)
    print(f"PASS: {total} files verified; Git branch and pinned ancestry verified.")
else:
    print(f"PASS: {total} files verified against trusted manifest SHA-256 {sha256(manifest_bytes)}.")
    print("INFO: live Git branch, HEAD, and ancestry verification unavailable in archive mode.")
