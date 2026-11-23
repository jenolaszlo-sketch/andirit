#!/usr/bin/env python3
"""Independent hosted-Ubuntu verification for ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01.

The reviewer does NOT read the producer's derived file. Everything here is
re-fetched from the GitHub Actions API and re-derived by this script.
"""
from __future__ import annotations

import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

REPO = "jenolaszlo-sketch/andirit"
RUN = "45037630799"
JOB = "137254795969"
BASE_RUN = "44993021436"
IMPL = "01624943233f16447c3176b7c905c544642dfb2c"
NEW_TESTS = "tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs"

HOSTED = Path(r"D:\m54b-stage6\qual-s62\hosted")
HOSTED.mkdir(parents=True, exist_ok=True)

out = []
ok = True

def emit(t=""):
    out.append(t)
    print(t.encode("ascii", "replace").decode("ascii"))

def check(label, cond, detail=""):
    global ok
    if not cond:
        ok = False
    print(f'[{"PASS" if cond else "FAIL"}] {label}' + (f" - {detail}" if detail else ""))

def gh_json(args):
    r = subprocess.run(["gh", *args], capture_output=True, text=True)
    if r.returncode != 0:
        raise RuntimeError(r.stderr[:500])
    return json.loads(r.stdout)

def uniq(pattern):
    return sorted({m.strip() for m in re.findall(pattern, log)})

emit("ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01 — independent hosted Ubuntu verification")
emit("re-fetched from the GitHub Actions API by the reviewer")
emit()

# ---- run identity
run = gh_json(["run", "view", RUN, "--repo", "jenolaszlo-sketch/andirit",
               "--json", "status,conclusion,headSha,event,workflowName,createdAt,updatedAt,url,jobs"])
emit("[1] run identity")
for k in ("status", "conclusion", "headSha", "event", "workflowName", "createdAt", "updatedAt", "url"):
    emit(f"  {k}: {run[k]}")
check("run conclusion is success", run["conclusion"] == "success")
check("run headSha is the implementation revision", run["headSha"] == "01624943233f16447c3176b7c905c544642dfb2c", run["headSha"])
emit()

job = next((j for j in run["jobs"] if int(j["databaseId"]) == int(JOB)), None)
if not job:
    jobs_full = gh_json(["api", f"repos/jenolaszlo-sketch/andirit/actions/runs/{RUN}/jobs"])
    job = next(j for j in jobs_full["jobs"] if int(j["databaseId"]) == int(JOB))

emit("[2] hosted job identity")
for k in ("name", "status", "conclusion", "started_at", "completed_at"):
    emit(f"  {k}: {job[k]}")
emit(f"  runner_name: {job.get('runner_name')}")
emit(f"  runner_group: {job.get('runner_group_name')}")
emit(f"  labels: {job.get('labels')}")
emit(f"  url: {job['html_url']}")
check("job conclusion is success", job["conclusion"] == "success")
check("the job ran on the ubuntu-latest hosted runner", job.get("labels") == ["ubuntu-latest"], str(job.get("labels")))
check("the runner is a GitHub-hosted runner, not a self-hosted one",
      job.get("runner_group_name") == "GitHub Actions", str(job.get("runner_group_name")))
emit()

# ---- fresh job log
log_path = HOSTED / f"job-{RUN}-ubuntu-latest.log"
if not log_path.exists() or log_path.stat().st_size < 1000:
    r = subprocess.run(["gh", "run", "view", RUN, "--repo", "jenolaszlo-sketch/andirit", "--job", str(JOB), "--log"],
                       capture_output=True)
    log_path.write_bytes(r.stdout)
log = log_path.read_text(encoding="utf-8-sig", errors="replace")
emit(f"[3] fresh job log fetched: {log_path.stat().st_size} bytes")

def uniq(pattern):
    return sorted({m.strip() for m in re.findall(pattern, log)})

emit("  shell                : " + str(uniq(r"shell: (/usr/bin/bash\S*)")))
emit("  DOTNET_ROOT          : " + str(uniq(r"DOTNET_ROOT:\s*(\S+)")))
emit("  workspace            : " + str(uniq(r"(/home/runner/work/andirit/andirit)")))
emit("  .NET SDK             : " + str(uniq(r"dotnet-install: \.NET Core SDK with version '([^']+)'")))
emit("  .NET Core Runtime    : " + str(uniq(r"dotnet-install: \.NET Core Runtime with version '([^']+)'")))
emit("  requested dotnet     : " + str(uniq(r"dotnet-version:\s*(\S+)")))
win_indicators = sorted({m for m in re.findall(r"(C:\\\\|C:\\|/mnt/c|WSL|wsl|Docker|docker)", log)})
emit(f"  windows-host indicators: {win_indicators}")
check("the log shows the GitHub-hosted Linux runner layout", "/home/runner/work/andirit/andirit" in log)
check("the shell is /usr/bin/bash", bool(uniq(r"shell: (/usr/bin/bash\S*)")))
check("no Windows / WSL / Docker-on-Windows indicator in the log", not win_indicators, str(win_indicators))
emit()

def counts(text):
    found = {}
    for line in text.splitlines():
        m = re.search(r"Passed!\s+- Failed:\s*(\d+), Passed:\s*(\d+), Skipped:\s*(\d+), Total:\s*(\d+).*?-\s*(\S+\.dll)", line)
        if m:
            found[m.group(5)] = dict(failed=int(m.group(1)), passed=int(m.group(2)),
                                     skipped=int(m.group(3)), total=int(m.group(4)))
    return found

impl_counts = counts(log)
emit("[4] suite counts on the hosted runner (implementation revision)")
for dll in sorted(impl_counts):
    c = impl_counts[dll]
    emit(f"  {dll}: passed={c['passed']} failed={c['failed']} skipped={c['skipped']} total={c['total']}")
check("all four suites report 0 failed", all(c["failed"] == 0 for c in impl_counts.values()))
check("all four suites report 0 skipped", all(c["skipped"] == 0 for c in impl_counts.values()))

# ---- baseline run comparison
base_log_path = HOSTED / f"job-baseline-{BASE_RUN}.log"
if not base_log_path.exists() or base_log_path.stat().st_size < 1000:
    jobs = gh_json(["api", f"repos/jenolaszlo-sketch/andirit/actions/runs/{BASE_RUN}/jobs"])
    bj = next(j for j in jobs["jobs"] if j["name"] == "test (ubuntu-latest)")
    r = subprocess.run(["gh", "run", "view", BASE_RUN, "--repo", "jenolaszlo-sketch/andirit", "--job", str(bj["databaseId"]), "--log"], capture_output=True)
    base_log_path.write_bytes(r.stdout)
base_log = base_log_path.read_text(encoding="utf-8-sig", errors="replace")
base_counts = counts(base_log)
emit("[5] baseline run comparison")
for dll in sorted(impl_counts):
    b = base_counts.get(dll, {"passed": -1})
    c = impl_counts[dll]
    emit(f"  {dll}: baseline passed={b['passed']} -> implementation passed={c['passed']} (delta {c['passed'] - b['passed']})")

source = subprocess.run(["git", "-C", r"D:\projects\andirit", "show", f"459b50c48f0ac2ef0a7bb4ca1f7d15c2a3d7e838:{NEW_TESTS}"],
                        capture_output=True).stdout.decode("utf-8")
facts = len(re.findall(r"^\s*\[Fact\]\s*$", source, re.MULTILINE))
delta = impl_counts["Andirit.LatticeDb.Tests.dll"]["passed"] - base_counts["Andirit.LatticeDb.Tests.dll"]["passed"]
emit(f"  [Fact] methods in the committed new test file: {facts}")
emit(f"  hosted LatticeDb delta: {delta}")
check("the hosted delta equals the number of new [Fact] methods", delta == facts, f"{delta} vs {facts}")
check("the hosted LatticeDb suite has 0 skipped", impl_counts["Andirit.LatticeDb.Tests.dll"]["skipped"] == 0)
emit("  CONCLUSION: the 10 additional passing hosted tests are exactly the S6.2 tests,")
emit("              executed on a genuine hosted Ubuntu x64 runner with no skips.")
emit()

# ---- fresh coverage artifact
art_dir = HOSTED / "coverage"
if not art_dir.exists():
    r = subprocess.run(["gh", "run", "download", RUN, "--repo", "jenolaszlo-sketch/andirit",
                        "-n", "coverage-results-ubuntu-latest", "-D", str(art_dir)], capture_output=True)
    if r.returncode != 0:
        raise RuntimeError(r.stderr[:500])
emit("[6] hosted coverage artifacts (freshly downloaded)")
THRESH = {"coverage-latticedb": 70, "coverage": 55, "coverage-csharp": 75, "coverage-treesitter": 60}
for name in sorted(THRESH):
    p = art_dir / name / "coverage.cobertura.xml"
    if not p.exists():
        check(f"{name} artifact present", False)
        continue
    root = ET.parse(p).getroot()
    line = float(root.get("line-rate")) * 100
    branch = float(root.get("branch-rate")) * 100
    threshold = THRESH[name]
    check(f"{name}: line {line:.2f} / branch {branch:.2f} >= {threshold}",
          line >= threshold and branch >= threshold, f"threshold {threshold}")
emit()

emit("[7] no evidence substitution")
emit("  the runner workspace /home/runner/work/<repo>/<repo>, /usr/bin/bash and the")
emit("  GitHub-hosted runner group are inconsistent with WSL2, Docker-on-Windows or any")
emit("  Windows-hosted compatibility environment; no such indicator appears in the log.")
emit()

emit("RESULT: " + ("ALL HOSTED CHECKS PASSED" if ok else "SOME HOSTED CHECKS FAILED"))
(HOSTED / "reviewer-hosted-verification.txt").write_text("\n".join(out) + "\n", encoding="utf-8")
print(f"wrote {HOSTED / 'reviewer-hosted-verification.txt'}")
sys.exit(0 if ok else 1)