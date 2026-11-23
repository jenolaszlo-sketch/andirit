#!/usr/bin/env python3
"""Git-object-derived preflight receipt for ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01."""

import hashlib
import json
import subprocess
import sys

REPO = r"D:\projects\andirit"
S62_BASE = "92bf73a61794fa3bccad53d23dd7e65c51807cd5"  # S6.2 entry baseline = S6.1 acceptance HEAD
IMPL = "01624943233f16447c3176b7c905c544642dfb2c"
QUAL = "f8f430b1faf4b3326c723ccb9f5a057228f42fb4"
ACCEPT = "92bf73a61794fa3bccad53d23dd7e65c51807cd5"
REMOTE_BASE = "b5a81f81fade9807f706b4586329efe416cf1558"
QUALIFIED = "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6"
TAG_OBJECT = "6659647527a6e0eb822a3d7a05b8283c75fd6a3e"
TAG_TARGET = "6ed05964d2fcc758648b654a01ec706647e52183"

def git(*args):
    r = subprocess.run(["git", "-C", r"D:\projects\andirit", *args], capture_output=True, text=True)
    return r.stdout.strip() if r.returncode == 0 else r.stderr.strip()

def check(label, condition, detail=""):
    if not condition:
        print(f"[FAIL] {label} - {detail}")
    else:
        print(f"[PASS] {label}" + (f" - {detail}" if detail else ""))

def main():
    print("ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01 — preflight receipt")
    print(f"repository: D:\\projects\\andirit")
    print()

    # Repository identities at the S6.2 entry baseline (S6.1 acceptance HEAD)
    for label, rev in (("S6.2 entry baseline (S6.1 acceptance HEAD)", "92bf73a61794fa3bccad53d23dd7e65c51807cd5"),
                        ("S6.1 implementation (pushed)", "01624943233f16447c3176b7c905c544642dfb2c"),
                        ("S6.1 qualification HEAD", "f8f430b1faf4b3326c723ccb9f5a057228f42fb4"),
                        ("S6.1 acceptance HEAD", "92bf73a61794fa3bccad53d23dd7e65c51807cd5")):
        obj_type = subprocess.run(["git", "-C", r"D:\projects\andirit", "cat-file", "-t", rev], capture_output=True, text=True).stdout.strip()
        tree = subprocess.run(["git", "-C", r"D:\projects\andirit", "show", "-s", "--format=%T", rev], capture_output=True, text=True).stdout.strip()
        subject = subprocess.run(["git", "-C", r"D:\projects\andirit", "show", "-s", "--format=%s", rev], capture_output=True, text=True).stdout.strip()
        print(f"  {label:30s} {rev}")
        print(f"  {'':30s} type={obj_type} tree={tree}")
        print(f"  {'':30s} subject={subject}")
        print(f"[PASS] {label} object exists and is a commit" if obj_type == "commit" else f"[FAIL] {label} object exists and is a commit - {obj_type}")
    print()

    # Ancestry: S6.2 entry baseline is a descendant of implementation
    for label, a, b in (("S6.2 entry baseline -> implementation", "92bf73a61794fa3bccad53d23dd7e65c51807cd5", "01624943233f16447c3176b7c905c544642dfb2c"),
                        ("implementation -> qualification", "01624943233f16447c3176b7c905c544642dfb2c", "f8f430b1faf4b3326c723ccb9f5a057228f42fb4"),
                        ("qualification -> acceptance", "f8f430b1faf4b3326c723ccb9f5a057228f42fb4", "92bf73a61794fa3bccad53d23dd7e65c51807cd5"),
                        ("qualified impl eafd632 -> acceptance", "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6", "92bf73a61794fa3bccad53d23dd7e65c51807cd5"),
                        ("remote base -> implementation", "b5a81f81fade9807f706b4586329efe416cf1558", "01624943233f16447c3176b7c905c544642dfb2c")):
        code = subprocess.run(["git", "-C", r"D:\projects\andirit", "merge-base", "--is-ancestor", a, b]).returncode
        print(f"[PASS] {label} is an ancestor relation" if code == 0 else f"[FAIL] {label} is an ancestor relation - is-ancestor exit={code}")
    print()

    # Working tree: only S6.2 entry docs should be modified
    status = subprocess.run(["git", "-C", r"D:\projects\andirit", "status", "--porcelain"], capture_output=True, text=True).stdout
    if not status.strip():
        print("[PASS] working tree clean")
    else:
        # Only S6.2 entry docs should be modified
        dirty = [l for l in status.splitlines() if l.strip()]
        s62_files = [l for l in dirty if "ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01" in l]
        other = [l for l in dirty if "ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01" not in l]
        if not other:
            print(f"[PASS] working tree only has S6.2 entry modifications ({len(s62_files)} files)")
        else:
            print(f"[FAIL] working tree has non-S6.2 changes: {other}")
    print()

    branch = subprocess.run(["git", "-C", "D:\\projects\\andirit", "rev-parse", "--abbrev-ref", "HEAD"], capture_output=True, text=True).stdout.strip()
    if branch == "main":
        print("[PASS] on branch main")
    else:
        print(f"[FAIL] on branch main - {branch}")
    head = subprocess.run(["git", "-C", r"D:\projects\andirit", "rev-parse", "HEAD"], capture_output=True, text=True).stdout.strip()
    # HEAD is the current commit (S6.2 entry commit), not the acceptance baseline
    counts = subprocess.run(["git", "-C", r"D:\projects\andirit", "rev-list", "--left-right", "--count", "HEAD...origin/main"], capture_output=True, text=True).stdout.strip()
    print(f"  HEAD...origin/main = {counts}  (local commits ahead, not pushed)")
    remote_main = subprocess.run(["git", "ls-remote", "origin", "refs/heads/main"], capture_output=True, text=True).stdout.strip().split()[0]
    if remote_main == "01624943233f16447c3176b7c905c544642dfb2c":
        print("[PASS] origin/main is the pinned implementation revision")
    else:
        print(f"[FAIL] origin/main is the pinned implementation revision - {remote_main}")
    if remote_main == "01624943233f16447c3176b7c905c544642dfb2c":
        print("[PASS] local S6.2 entry HEAD is not pushed (origin/main is implementation)")
    else:
        print("[FAIL] local acceptance HEAD is not pushed")
    print()

    # Published state untouched
    tag_remote = subprocess.run(["git", "ls-remote", "origin", "refs/tags/v0.2.0-preview.7"], capture_output=True, text=True).stdout.strip().split()[0]
    print("[PASS] remote tag object unchanged" if subprocess.run(["git", "ls-remote", "origin", "refs/tags/v0.2.0-preview.7"], capture_output=True, text=True).stdout.strip().split()[0] == "6659647527a6e0eb822a3d7a05b8283c75fd6a3e" else "[FAIL] remote tag object unchanged")
    tag_peeled = subprocess.run(["git", "rev-parse", "6659647527a6e0eb822a3d7a05b8283c75fd6a3e^{}"], capture_output=True, text=True).stdout.strip()
    if tag_peeled == "6ed05964d2fcc758648b654a01ec706647e52183":
        print("[PASS] tag peels to the same commit as at S6.1 entry")
    else:
        print(f"[FAIL] tag peels to the same commit as at S6.1 entry - {tag_peeled}")
    version_line = None
    for line in subprocess.run(["git", "-C", r"D:\projects\andirit", "show", "92bf73a61794fa3bccad53d23dd7e65c51807cd5:Directory.Build.props"], capture_output=True, text=True).stdout.splitlines():
        if "<Version>" in line:
            version_line = line.strip()
            break
    print(f"  package version    = {version_line}")
    print("[PASS] release version still 0.2.0-preview.7" if version_line and "0.2.0-preview.7" in version_line else f"[FAIL] release version still 0.2.0-preview.7 - {version_line}")
    print()

    # Change-set boundary (S6.2 entry baseline -> current HEAD) - should be 0 production changes
    delta = subprocess.run(["git", "-C", r"D:\projects\andirit", "diff", "--name-only", "92bf73a61794fa3bccad53d23dd7e65c51807cd5", "HEAD", "--", "src", "tests", ".github"], capture_output=True, text=True).stdout.splitlines()
    for line in delta:
        print(f"   {line}")
    names = [line for line in delta]
    if not any(n.startswith(".github/") for n in delta):
        print("[PASS] no change under .github/")
    else:
        print("[FAIL] no change under .github/")
    if not any(n.endswith(".csproj") for n in delta):
        print("[PASS] no change to any project file")
    else:
        print("[FAIL] no change to any project file")
    if len(delta) == 0:
        print("[PASS] exactly 0 changed paths in src/tests/.github (S6.2 entry adds no production changes)")
    else:
        print(f"[FAIL] exactly 0 changed paths in src/tests/.github - got {len(delta)}")
    print()

    # Evidence packages - only check S6.2 entry package
    s62_pkg = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-evidence"
    try:
        manifest_raw = subprocess.run(["git", "-C", r"D:\projects\andirit", "cat-file", "blob", f"HEAD:{s62_pkg}/manifest.json"], capture_output=True, text=True).stdout
        manifest = json.loads(manifest_raw)
        listed = {e["path"] for e in manifest["files"]}
        actual = set()
        for line in subprocess.run(["git", "-C", r"D:\projects\andirit", "ls-tree", "-r", "--name-only", "HEAD", s62_pkg], capture_output=True, text=True).stdout.splitlines():
            if line != f"{s62_pkg}/manifest.json":
                actual.add(line[len(s62_pkg) + 1:])
        if listed == actual:
            print(f"[PASS] S6.2 entry package manifest complete ({len(listed)} entries)")
        else:
            print(f"[FAIL] S6.2 entry package manifest complete - listed {len(listed)}, committed {len(actual)}")
    except Exception as e:
        print(f"[FAIL] S6.2 entry package manifest - {e}")

    return 0

if __name__ == "__main__":
    import sys
    sys.exit(main())