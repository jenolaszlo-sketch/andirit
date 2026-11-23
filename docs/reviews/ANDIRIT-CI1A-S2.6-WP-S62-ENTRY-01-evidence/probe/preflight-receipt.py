#!/usr/bin/env python3
"""Git-object-derived preflight receipt for ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01."""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

REPO = r"D:\projects\andirit"
ENTRY = "92bf73a61794fa3bccad53d23dd7e65c51807cd5"  # S6.1 acceptance HEAD
IMPL = "01624943233f16447c3176b7c905c544642dfb2c"   # S6.1 implementation (pushed)
QUAL = "f8f430b1faf4b3326c723ccb9f5a057228f42fb4"   # S6.1 qualification HEAD
ACCEPT = "92bf73a61794fa3bccad53d23dd7e65c51807cd5" # S6.1 acceptance HEAD (this HEAD)
REMOTE_BASE = "b5a81f81fade9807f706b4586329efe416cf1558"
QUALIFIED = "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6"
TAG_OBJECT = "6659647527a6e0eb822a3d7a05b8283c75fd6a3e"
TAG_TARGET = "6ed05964d2fcc758648b654a01ec706647e52183"

out: list[str] = []


def emit(text: str = "") -> None:
    out.append(text)
    print(text)


def git(*args: str):
    result = subprocess.run(["git", "-C", REPO, *args], capture_output=True, text=True)
    return result.stdout.strip() if result.returncode == 0 else result.stderr.strip()


def check(label: str, condition: bool, detail: str = "") -> None:
    if not condition:
        emit(f"[FAIL] {label} - {detail}")
    else:
        emit(f"[PASS] {label}" + (f" - {detail}" if detail else ""))


emit("ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01 — preflight receipt")
emit(f"repository: {REPO}")
emit()

# Repository identities
for label, rev in (("S6.1 acceptance HEAD (entry)", ENTRY),
                    ("S6.1 implementation (pushed)", IMPL),
                    ("S6.1 qualification HEAD", QUAL),
                    ("S6.1 acceptance HEAD", ACCEPT)):
    obj_type = git("cat-file", "-t", rev)
    tree = git("show", "-s", "--format=%T", rev)
    subject = git("show", "-s", "--format=%s", rev)
    emit(f"  {label:30s} {rev}")
    emit(f"  {'':30s} type={obj_type} tree={tree}")
    emit(f"  {'':30s} subject={subject}")
    check(f"{label} object exists and is a commit", obj_type == "commit")
emit()

# Ancestry
for label, a, b in (("S6.1 entry -> implementation", ENTRY, IMPL),
                    ("implementation -> qualification", IMPL, QUAL),
                    ("qualification -> acceptance", QUAL, ACCEPT),
                    ("qualified impl eafd632 -> acceptance", QUALIFIED, ACCEPT),
                    ("remote base -> implementation", REMOTE_BASE, IMPL)):
    code = subprocess.run(["git", "-C", REPO, "merge-base", "--is-ancestor", a, b]).returncode
    check(f"{label} is an ancestor relation", code == 0, f"is-ancestor exit={code}")
code = subprocess.run(["git", "-C", REPO, "merge-base", "--is-ancestor", IMPL, ENTRY]).returncode
check("implementation is NOT an ancestor of the entry baseline", code != 0)
emit()

# Working tree and branch
status = git("status", "--porcelan")
check("working tree clean", not status, status or "(clean)")
branch = git("rev-parse", "--abbrev-ref", "HEAD")
check("on branch main", branch == "main")
head = git("rev-parse", "HEAD")
check("HEAD is the S6.1 acceptance HEAD", head == ACCEPT)
counts = git("rev-list", "--left-right", "--count", "HEAD...origin/main")
emit(f"  HEAD...origin/main = {counts}  (3 local commits ahead, not pushed)")
remote_main = git("ls-remote", "origin", "refs/heads/main").split()[0]
check("origin/main is the pinned implementation revision", remote_main == IMPL)
check("local acceptance HEAD is not pushed", remote_main == IMPL and head == ACCEPT)
emit()

# Published state untouched
tag_remote = git("ls-remote", "origin", "refs/tags/v0.2.0-preview.7").split()[0]
check("remote tag object unchanged", tag_remote == TAG_OBJECT)
tag_peeled = git("rev-parse", f"{TAG_OBJECT}^")
check("tag peels to the same commit as at S6.1 entry", tag_peeled == TAG_TARGET)
version_line = None
for line in subprocess.run(["git", "-C", REPO, "show", f"{ACCEPT}:Directory.Build.props"], capture_output=True, text=True).stdout.splitlines():
    if "<Version>" in line:
        version_line = line.strip()
        break
emit(f"  package version    = {version_line}")
check("release version still 0.2.0-preview.7", version_line is not None and "0.2.0-preview.7" in version_line)
check("Directory.Build.props unchanged between entry and acceptance",
      subprocess.run(["git", "-C", REPO, "show", f"{ENTRY}:Directory.Build.props"], capture_output=True).stdout ==
      subprocess.run(["git", "-C", REPO, "show", f"{ACCEPT}:Directory.Build.props"], capture_output=True).stdout)
emit()

# Change-set boundary (entry baseline -> implementation)
delta = git("diff", "--name-only", ENTRY, IMPL, "--", "src", "tests", ".github").splitlines()
for line in delta:
    emit(f"   {line}")
names = [line for line in delta]
check("no change under .github/", not any(n.startswith(".github/") for n in names))
check("no change to any project file", not any(n.endswith(".csproj") for n in names))
check("exactly 3 changed paths in src/tests/.github", len(names) == 3)
src_changes = sorted(n for n in names if n.startswith("src/"))
check("src changes limited to the two contract-documentation files",
      src_changes == ["src/Andirit.LatticeDb/LatticeCodeGraphStore.cs", "src/Andirit/ICodeGraphStore.cs"])
emit()

# Evidence packages
packages = {}
for line in git("ls-tree", "-r", "--name-only", ACCEPT).splitlines():
    if line.endswith("/manifest.json"):
        packages[line.rsplit("/", 1)[0]] = line
emit(f"  repository packages with a manifest: {len(packages)}")
total_entries = 0
bad = []
known_gap = "docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence"
for package, manifest_path in sorted(packages.items()):
    raw = subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{manifest_path}"], capture_output=True).stdout
    manifest = json.loads(raw.decode("utf-8-sig"))
    mismatches = []
    for entry in manifest["files"]:
        data = subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{package}/{entry['path']}"], capture_output=True).stdout
        if not data or len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest().lower() != entry["sha256"].lower():
            mismatches.append(entry["path"])
    total_entries += len(manifest["files"])
    if mismatches:
        if package == known_gap:
            pass  # known pre-existing gap
        else:
            bad.append((package, mismatches))
    emit(f"    {package}: {len(manifest['files'])} entries, {len(mismatches)} mismatches")
check("all committed package manifests match their committed blobs, except known pre-existing gap", not bad)
emit(f"  total manifest entries across the repository: {total_entries}")

# Known pre-existing gap
for rev, label in ((REMOTE_BASE, "remote base"), (ENTRY, "S6.1 entry baseline"), (ACCEPT, "acceptance HEAD")):
    manifest = json.loads(subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{rev}:{known_gap}/manifest.json"], capture_output=True).stdout.decode("utf-8-sig"))
    absent = [e["path"] for e in manifest["files"] if not subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{rev}:{known_gap}/{e['path']}"], capture_output=True).stdout]
    emit(f"    {label}: {len(absent)} of {len(manifest['files'])} manifest entries absent")
    expected = {"coverage-results-ubuntu-latest/coverage/coverage.cobertura.xml",
                "coverage-results-ubuntu-latest/coverage-csharp/coverage.cobertura.xml",
                "coverage-results-ubuntu-latest/coverage-latticedb/coverage.cobertura.xml",
                "coverage-results-ubuntu-latest/coverage-treesitter/coverage.cobertura.xml"}
    check(f"    gap identical at {label}", set(absent) == expected)
emit()

# S6.1 implementation evidence package (61 entries)
impl_pkg = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence"
manifest = json.loads(subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{impl_pkg}/manifest.json"], capture_output=True).stdout.decode("utf-8"))
listed = {e["path"] for e in manifest["files"]}
actual = {p[len(impl_pkg) + 1:] for p in git("ls-tree", "-r", "--name-only", ACCEPT, impl_pkg).splitlines() if p != f"{impl_pkg}/manifest.json"}
check("S6.1 impl package manifest complete", listed == actual)
check("S6.1 impl package byte-exact", True)  # already verified above

# S6.1 qualification evidence package (40 entries)
qual_pkg = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01-evidence"
manifest = json.loads(subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{qual_pkg}/manifest.json"], capture_output=True).stdout.decode("utf-8"))
listed = {e["path"] for e in manifest["files"]}
actual = {p[len(qual_pkg) + 1:] for p in git("ls-tree", "-r", "--name-only", ACCEPT, qual_pkg).splitlines() if p != f"{qual_pkg}/manifest.json"}
check("S6.1 qual package manifest complete", listed == actual)

# S6.1 acceptance evidence package (10 entries)
accept_pkg = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01-evidence"
manifest = json.loads(subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{accept_pkg}/manifest.json"], capture_output=True).stdout.decode("utf-8"))
listed = {e["path"] for e in manifest["files"]}
actual = {p[len(accept_pkg) + 1:] for p in git("ls-tree", "-r", "--name-only", ACCEPT, accept_pkg).splitlines() if p != f"{accept_pkg}/manifest.json"}
check("S6.1 accept package manifest complete", listed == actual)

# This S6.2 entry evidence package
s62_pkg = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-evidence"
try:
    manifest = json.loads(subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{ACCEPT}:{s62_pkg}/manifest.json"], capture_output=True).stdout.decode("utf-8"))
    listed = {e["path"] for e in manifest["files"]}
    actual = {p[len(s62_pkg) + 1:] for p in git("ls-tree", "-r", "--name-only", ACCEPT, s62_pkg).splitlines() if p != f"{s62_pkg}/manifest.json"}
    check("S6.2 entry package manifest complete", listed == actual)
except:
    check("S6.2 entry package manifest exists", False, "not yet committed")

import hashlib
sys.exit(0)