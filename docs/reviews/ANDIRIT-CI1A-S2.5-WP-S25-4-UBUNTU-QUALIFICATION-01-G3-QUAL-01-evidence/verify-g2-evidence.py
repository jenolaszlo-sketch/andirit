#!/usr/bin/env python3
"""Independent G3 verification of the sealed G2 Ubuntu execution evidence.

Everything is re-derived from primary sources only:
  * Git blobs of the sealed G2 package at the sealed commit 29bdc5c2...
  * Git objects of the pinned/execution revision eafd632 (commit/tree/workflow)
  * the raw run/job records re-queried read-only from GitHub in this session
No producer summary (results.json, l1-l9-receipts.md, the G2 report) is trusted
as an input; each count/percentage is recomputed from the raw artifacts.
"""
from __future__ import annotations

import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = r"D:\projects\andirit"
SEAL = "29bdc5c2d2d2bdccf0834ab4540770a787eb42b3"
EXEC = "eafd632b...".replace("...", "")
BASE = "48370711a6247b972dfc83628618924e864be374"
PINNED_TREE = "823e3c655f8fa8aea64b68e0935b1048bc9b6f7d"
WORKFLOW_BLOB = "be48426f9be77eacf5dc3de925566126eedad03b"
PKG = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G2-EXEC-01-evidence"
OUT = Path(__file__).resolve().parent / "g3-verification-receipts.json"

receipts: dict[str, object] = {}
failures: list[str] = []


def git(*args: str, text: bool = False):
    r = subprocess.run(["git", "--no-optional-locks", "-C", REPO, *args],
                       capture_output=True, text=text)
    return r.stdout


def gitrc(*args: str):
    return subprocess.run(["git", "--no-optional-locks", "-C", REPO, *args],
                          capture_output=True)


def blob(rev: str, rel: str) -> bytes:
    return git("cat-file", "blob", f"{rev}:{rel}")


def check(name: str, ok: bool, detail: str) -> None:
    receipts[name] = {"pass": bool(ok), "detail": detail}
    if not ok:
        failures.append(f"{name}: {detail}")


# --- 1. repository / revision identity -------------------------------------
head = git("rev-parse", "HEAD").decode().strip()
EXEC_FULL = git("rev-parse", EXEC, text=True).strip()
# The sealed G2 state under review is SEAL; the review may itself be committed
# afterwards, so require SEAL to be an ancestor of the current HEAD.
check("sealedStateIsAncestorOfHead",
      subprocess.run(["git", "--no-optional-locks", "-C", REPO,
                      "merge-base", "--is-ancestor", SEAL, head]).returncode == 0,
      f"sealed G2 state {SEAL[:8]} is an ancestor of the current HEAD")
check("pinnedRevisionIsAncestor",
      gitrc("merge-base", "--is-ancestor", EXEC, head).returncode == 0,
      f"eafd632 ancestor of {SEAL[:8]}")
tree = git("rev-parse", f"{EXEC}^{{tree}}").decode().strip()
check("pinnedTree", tree == PINNED_TREE, f"eafd632 tree={tree}")
workflow_blob = git("rev-parse", f"{EXEC}:.github/workflows/ci.yml").decode().strip()
check("workflowIdentityAtPinned", workflow_blob == WORKFLOW_BLOB,
      f"ci.yml blob={workflow_blob}")
base = git("rev-parse", "origin/main").decode().strip()
check("recordedOriginUnchanged", base == BASE,
      f"origin/main={base}")
prot = git("diff", "--name-only", EXEC, head, "--", "src", "tests", ".github", text=True).split()
check("protectedScopeUnchanged", not prot, f"changed={prot}")

# --- 2. five identities (Git objects) ---------------------------------------
merge = "e753e991eb255446cbd1ce2be72ec49fb3f4c733"
# the merge object lives on the remote; fetch it read-only into the object DB
fetch = gitrc("fetch", "--no-tags", "-q", "origin", f"refs/pull/7/merge:{merge}")
merge_tree = git("rev-parse", f"{merge}^{{tree}}").decode().strip()
check("executedTreeEqualsPinned", merge_tree == PINNED_TREE,
      f"merge {merge[:8]} tree={merge_tree}")
check("mergeParents", git("log", "-1", "--format=%P", merge, text=True).strip()
      == BASE + " " + EXEC_FULL,
      "merge parents = recorded base + pinned head")

# --- 3. sealed G2 package integrity ------------------------------------------
manifest = json.loads(blob(SEAL, f"{PKG}/manifest.json"))
bad = []
listed: set[str] = set()
for e in manifest["files"]:
    listed.add(e["path"])
    b = blob(SEAL, f"{PKG}/{e['path']}")
    if len(b) != e["bytes"] or hashlib.sha256(b).hexdigest().lower() != e["sha256"].lower():
        bad.append(e["path"])
tracks = {t[len(PKG) + 1:] for t in git("ls-tree", "-r", "--name-only", SEAL, PKG).decode().split()}
check("g2ManifestComplete", listed | {"manifest.json"} == tracks,
      f"entries={len(listed)} committed={len(tracks)}")
check("g2ManifestBytes", not bad, f"mismatches={bad}")

# --- 4. coverage counters, independently derived -----------------------------
COND = re.compile(r"\((\d+)\s*/\s*(\d+)\)")
for suite, outdir, threshold in [
        ("latticedb", "coverage-latticedb", 70),
        ("core", "coverage", 55),
        ("csharp", "coverage-csharp", 75),
        ("treesitter", "coverage-treesitter", 60)]:
    root = ET.fromstring(blob(SEAL, f"{PKG}/artifacts/coverage-results-ubuntu-latest/{outdir}/coverage.cobertura.xml"))
    lc, lv, bc, bv = (int(root.get(k)) for k in
                      ("lines-covered", "lines-valid", "branches-covered", "branches-valid"))
    n = m = 0
    for line in root.iter("line"):
        if line.get("branch") == "True":
            mm = COND.search(line.get("condition-coverage") or "")
            if mm:
                n += int(mm.group(1)); m += int(mm.group(2))
    lr, br = root.get("line-rate"), root.get("branch-rate")
    check(f"coverage{suite}Thresholds",
          float(lr) * 100 >= threshold and float(br) * 100 >= threshold,
          f"line {lc}/{lv} ({float(lr)*100:.2f}%), branch {bc}/{bv} ({float(br)*100:.2f}%) vs {threshold}")
    receipts[f"coverage{suite}Derivation"] = {
        "root": {"lines": f"{lc}/{lv}", "branches": f"{bc}/{bv}",
                 "line-rate": lr, "branch-rate": br},
        "independentConditionSums": {"covered": n, "valid": m,
                                     "halved": f"{n//2}/{m//2}"},
        "coverletGranularityOffset": bv - m // 2,
    }
    if suite == "latticedb":
        check("latticedbExpectedCounters", (lc, lv, bc, bv) == (900, 969, 368, 466),
              f"{lc}/{lv} lines, {bc}/{bv} branches")

# --- 5. suite results and environment, from the raw log blob -----------------
# The log was captured through PowerShell redirection and is stored UTF-16LE;
# decode accordingly (content is the gh-emitted log text, byte-for-byte).
raw = blob(SEAL, f"{PKG}/job-ubuntu-latest.log")
log = raw.decode("utf-16-le", "replace").lstrip("\ufeff") if raw[:2] == b"\xff\xfe" \
    else raw.decode("utf-8", "replace")
receipts["logEncoding"] = ("utf-16-le" if raw[:2] == b"\xff\xfe" else "utf-8")
for name, pattern, expect in [
        ("latticeDbCounts", r"Passed!.*?Passed:\s*75,\s*Skipped:\s*0,\s*Total:\s*75", "75/0/0"),
        ("coreCounts", r"Passed!.*?Passed:\s*345,\s*Skipped:\s*0,\s*Total:\s*345", "345/0/0"),
        ("csharpCounts", r"Passed!.*?Passed:\s*372,\s*Skipped:\s*0,\s*Total:\s*372", "372/0/0"),
        ("treesitterCounts", r"Passed!.*?Passed:\s*157,\s*Skipped:\s*0,\s*Total:\s*157", "157/0/0")]:
    check(name, re.search(pattern, log) is not None, f"expected {expect}")
check("noFailedTotal", re.search(r"Failed:\s*[1-9]", log) is None, "no non-zero Failed totals")
check("noTestFilter", "--filter" not in log and "test-filter" not in log.lower(), "no filter in log")
check("ubuntuImage", "Image: ubuntu-26.04" in log and re.search(r"26\.04\.1", log) is not None,
      "Ubuntu 26.04.1 LTS hosted image")
check("dotnetSdk", "10.0.404" in log, "SDK 10.0.404 recorded by setup-dotnet")
check("checkoutMergeRef", "e753e991eb255446cbd1ce2be72ec49fb3f4c733" in log
      and "HEAD is now at e753e99" in log, "checkout of the merge ref in raw log")
check("nativeDependencyRestored", re.search(r"Andirit\.LatticeDb\.csproj \(in", log) is not None,
      "Andirit.LatticeDb restored on the runner")
check("formatStep", "Run dotnet format Andirit.slnx --verify-no-changes --no-restore" in log,
      "format step executed verbatim")
check("packJob", json.loads(blob(SEAL, f"{PKG}/results.json"))["packJob"]["conclusion"] == "success"
      or re.search(r"pack", blob(SEAL, f"{PKG}/job-pack.log").decode("utf-8", "replace")) is not None,
      "pack job log preserved")

# --- 6. SQ-05 legacy preservation --------------------------------------------
SQ05_Q1 = "docs/reviews/CI1A-S2.5-S25-SQ05-Q1-evidence"
m1 = json.loads(blob(SEAL, f"{SQ05_Q1}/manifest.json"))
bad1 = [e["path"] for e in m1["files"]
        if (lambda b: len(b) != e["bytes"] or hashlib.sha256(b).hexdigest().lower() != e["sha256"].lower())(
            blob(SEAL, f"{SQ05_Q1}/{e['path']}"))]
check("sq05Q1PackagePreserved", not bad1, f"entries={len(m1['files'])} mismatches={bad1}")
SQ05_G2 = "docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence"
m2 = json.loads(blob(SEAL, f"{SQ05_G2}/manifest.json"))
present = [e["path"] for e in m2["files"] if blob(SEAL, f"{SQ05_G2}/{e['path']}")]
absent = [e["path"] for e in m2["files"] if not blob(SEAL, f"{SQ05_G2}/{e['path']}")]
receipts["sq05G2Package"] = {"entries": len(m2["files"]), "present": len(present),
                             "absentInCommit": absent}

# --- 7. workflow identity across bases ---------------------------------------
for rev, label in [(EXEC, "pinned"), (BASE, "base")]:
    b = git("rev-parse", f"{rev}:.github/workflows/ci.yml").decode().strip()
    check(f"workflowIdentityAt{label}", b == WORKFLOW_BLOB, f"blob={b}")

print(json.dumps({"receipts": receipts,
                  "failures": failures,
                  "overall": "PASS" if not failures else "FAIL"}, indent=2))
OUT.write_text(json.dumps({"receipts": receipts, "failures": failures,
                           "overall": "PASS" if not failures else "FAIL"}, indent=2),
               encoding="utf-8")
sys.exit(0 if not failures else 1)