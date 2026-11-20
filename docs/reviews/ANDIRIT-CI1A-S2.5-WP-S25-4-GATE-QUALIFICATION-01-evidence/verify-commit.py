#!/usr/bin/env python3
"""Post-commit verification for ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01.

Read-only: manifest vs working tree vs committed blobs; producer evidence byte
identity against the original temp package; path/scope confine vs eafd632.
Modeled on the QUAL-04 commit-and-verify.py method.
"""
from __future__ import annotations

import hashlib
import json
import subprocess
import sys
from pathlib import Path

REPO = Path(r"D:\projects\andirit")
PKG = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence"
PROD = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence"
REPORT = "docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01.md"
PROD_TEMP = Path(r"C:\Windows\Temp\opencode\gate-remediation\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01")
START = "eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6"
ORIGIN = "48370711a6247b972dfc83628618924e864be374"


def git(*args: str) -> bytes:
    result = subprocess.run(["git", "--no-optional-locks", "-C", str(REPO), *args],
                            capture_output=True)
    if result.returncode != 0:
        raise RuntimeError(f"git {' '.join(args)}: {result.stderr.decode('utf-8', 'replace')}")
    return result.stdout


def blob(head: str, rel: str) -> bytes:
    return subprocess.run(["git", "--no-optional-locks", "-C", str(REPO), "cat-file",
                           "blob", f"{head}:{rel}"], capture_output=True, check=True).stdout


def main() -> int:
    head = git("rev-parse", "HEAD").decode().strip()
    tree = git("rev-parse", "HEAD^{tree}").decode().strip()
    checks: dict[str, object] = {}

    manifest = json.loads((REPO / PKG / "manifest.json").read_text(encoding="utf-8"))
    listed = [e["path"] for e in manifest["files"]]
    actual = sorted(p.relative_to(REPO / PKG).as_posix()
                    for p in (REPO / PKG).rglob("*")
                    if p.is_file() and p.name != "manifest.json")
    checks["manifestCompleteCoverage"] = actual == listed

    mismatches = []
    for e in manifest["files"]:
        work = (REPO / PKG / e["path"]).read_bytes()
        com = blob(head, f"{PKG}/{e['path']}")
        if len(work) != e["bytes"] or hashlib.sha256(work).hexdigest() != e["sha256"]:
            mismatches.append(("worktree", e["path"]))
        if work != com:
            mismatches.append(("blob", e["path"]))
    checks["manifestMismatches"] = mismatches

    producer_pairs = [
        (f"{PROD}/coverage-latticedb.cobertura.xml", PROD_TEMP / "evidence" / "coverage-latticedb.cobertura.xml"),
        (f"{PROD}/latticedb-testresults.trx", PROD_TEMP / "evidence" / "latticedb-testresults.trx"),
        (f"{PROD}/MANIFEST.txt", PROD_TEMP / "evidence" / "MANIFEST.txt"),
        (f"{PROD}/PRODUCER-EVIDENCE.md", None),
    ]
    prod = []
    for rel, src in producer_pairs:
        com = blob(head, rel)
        if src is None:
            prod.append((rel, "reviewer-record", len(com)))
        else:
            prod.append((rel, "byte-identical", com == src.read_bytes(), len(com)))
    checks["producerEvidence"] = prod

    report_blob = blob(head, REPORT)
    checks["reportBytes"] = len(report_blob)
    checks["reportSha256"] = hashlib.sha256(report_blob).hexdigest()

    expected = sorted([REPORT, f"{PKG}/manifest.json"] +
                      [f"{PKG}/{e['path']}" for e in manifest["files"]] +
                      [f"{PROD}/{p.name}" for p in (REPO / PROD).rglob("*") if p.is_file()])
    changes = sorted(git("diff", "--name-status", "--no-renames", START, head).decode().splitlines())
    checks["changesOnlyExpectedAdditions"] = changes == sorted(f"A\t{p}" for p in expected)
    checks["changeCount"] = len(changes)
    outside = git("diff", "--name-only", START, head, "--", ".",
                  f":(exclude){REPORT}", f":(exclude){PKG}/**", f":(exclude){PROD}/**").decode().strip()
    checks["protectedPathsChanged"] = bool(outside)
    checks["worktreeClean"] = not git("status", "--porcelain=v1").decode().strip()
    checks["branch"] = git("branch", "--show-current").decode().strip()
    checks["stashes"] = len(git("stash", "list").decode().splitlines())
    checks["worktrees"] = git("worktree", "list", "--porcelain").decode().count("worktree ")
    checks["originMain"] = git("rev-parse", "origin/main").decode().strip()
    checks["divergence"] = git("rev-list", "--left-right", "--count", "origin/main...HEAD").decode().split()
    checks["head"] = head
    checks["tree"] = tree
    checks["parent"] = git("rev-parse", "HEAD^").decode().strip()
    checks["manifestEntries"] = len(manifest["files"])
    checks["manifestSha256"] = hashlib.sha256((REPO / PKG / "manifest.json").read_bytes()).hexdigest()

    print(json.dumps(checks, indent=2))
    ok = (checks["manifestCompleteCoverage"] and not checks["manifestMismatches"]
          and checks["changesOnlyExpectedAdditions"] and not checks["protectedPathsChanged"]
          and checks["worktreeClean"] and checks["branch"] == "main"
          and all(p[1] == "byte-identical" for p in checks["producerEvidence"] if p[1] != "reviewer-record"))
    print("VERIFICATION:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())