"""Read the independently fetched hosted records and challenge the frozen verifier."""
from collections import Counter
from copy import deepcopy
import hashlib
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
from uuid import uuid4
from zipfile import ZipFile
import xml.etree.ElementTree as ET

root = Path(sys.argv[1])
verifier = sys.argv[2]
run_id = 45691273312
subject = "a96c19a844f8ea1719508198b47df23c4e203571"
download = root / str(run_id) / "download"
platforms = {"ubuntu-latest": ("Linux", "X64"), "windows-latest": ("Windows", "X64"), "macos-latest": ("macOS", "ARM64")}
projects = {"core": ("Andirit.Tests", "coverage", 345, 55), "csharp": ("Andirit.CSharp.Tests", "coverage-csharp", 372, 75),
            "treesitter": ("Andirit.TreeSitter.Tests", "coverage-treesitter", 157, 60),
            "latticedb": ("Andirit.LatticeDb.Tests", "coverage-latticedb", 131, 70)}


def load(file):
    return json.loads(file.read_text(encoding="utf-8-sig"))


def digest(file):
    return hashlib.sha256(file.read_bytes()).hexdigest()


run = root / str(run_id)
metadata = load(run / "run.json")
assert (metadata["id"], metadata["head_sha"], metadata["head_branch"], metadata["event"],
        metadata["status"], metadata["conclusion"], metadata["path"], metadata["run_attempt"]) == (
        run_id, subject, "main", "push", "completed", "success", ".github/workflows/ci.yml", 1)
jobs = load(run / "jobs.json")["jobs"]
assert len(jobs) == 4
assert {job["name"] for job in jobs} == {"pack", *(f"test ({key})" for key in platforms)}
for job in jobs:
    assert job["head_sha"] == subject and job["conclusion"] == "success"
    print("job", job["id"], job["name"], job["conclusion"], job["runner_name"], job["runner_group_name"], job["labels"])
    if job["name"].startswith("test ("):
        steps = {step["name"]: step["conclusion"] for step in job["steps"]}
        for step in ("Test and coverage (LatticeDb)", "Bind test receipts to checkout and runner",
                     "Verify S6.3 named-case evidence", "Upload test execution evidence", "Upload coverage"):
            assert steps[step] == "success"
ubuntu = next(job for job in jobs if job["name"] == "test (ubuntu-latest)")
ubuntu_log = (run / ("job-" + str(ubuntu["id"]) + ".log")).read_text(encoding="utf-8-sig")
assert all(item in ubuntu_log for item in ("Hosted Compute Agent", "26.04.1", "ubuntu-26.04", "20261116.202.1",
                                           "/usr/bin/bash", "/home/runner/work/andirit/andirit", subject))

artifacts = load(run / "artifacts.json")["artifacts"]
assert {artifact["name"] for artifact in artifacts} == {f"{kind}-results-{platform}"
                                                         for kind in ("coverage", "test") for platform in platforms}
for artifact in artifacts:
    assert artifact["workflow_run"]["id"] == run_id and artifact["workflow_run"]["head_sha"] == subject
    print("artifact", artifact["name"], artifact["id"], artifact["digest"])
native = next(artifact for artifact in artifacts if artifact["name"] == "test-results-ubuntu-latest")
raw = run / (native["name"] + ".zip")
assert native["digest"] == "sha256:" + digest(raw)
with ZipFile(raw) as archive:
    members = {row for row in archive.namelist() if not row.endswith("/")}
    assert members == {"subject.json", *(key + "/tests.trx" for key in projects)}
    for member in members:
        assert archive.read(member) == (download / native["name"] / member).read_bytes()


def observations(cases, os_name):
    d05 = cases["ConcurrentMultiProcessAccess_FileLockingAndConsistency"]
    d07 = cases["AbruptProcessTermination_DurableStateConsistent"]
    p12 = next(output for name, output in cases.items() if name.startswith("P12_"))
    p15 = next(output for name, output in cases.items() if name.startswith("P15_"))
    assert re.search(r"P12 before=run:p12:1/[a-f0-9]{64} readerQueued=true after=run:p12:2/[a-f0-9]{64}", p12)
    worker_digests = set()
    group = {}
    for label, value in (("D-05", d05), ("D-07", d07), ("P-15", p15)):
        ready = re.findall(r"READY\|(\d+)\|", value)
        finished = re.findall(r"worker pid=(\d+) exit=0 sha256=([a-f0-9]{64})", value)
        all_pids = ready + [pid for pid, _ in finished]
        assert len(ready) == 1 and len(all_pids) == len(set(all_pids)) == {"D-05": 5, "D-07": 3, "P-15": 2}[label]
        worker_digests.update(sha for _, sha in finished)
        group[label] = all_pids
    reads = re.findall(r"(READ|CANCELLED)\|(\d+)\|run:durability-pub\|([a-f0-9]{64})", d05)
    assert [kind for kind, _, _ in reads] == ["READ", "READ", "CANCELLED", "READ"]
    assert len({key for _, _, key in reads}) == 1 and [pid for _, pid, _ in reads] == group["D-05"][1:]
    assert "PUBLISHED|run:durability-pub|" in d05
    killed = re.search(r"READY\|\d+\|repo:d07-held\|run:d07-held\|unit:abrupt KILLED exit=(-?\d+)", d07)
    assert killed and int(killed[1]) != 0
    transitions = re.findall(r"READ\|(\d+)\|(run:d07-[a-z-]+)\|([a-f0-9]{64})", d07)
    assert [row[1] for row in transitions] == ["run:d07-baseline", "run:d07-after-recovery"]
    assert [row[0] for row in transitions] == group["D-07"][1:] and len({row[2] for row in transitions}) == 2
    assert "PUBLISHED|run:p15-pub|" in p15 and "repo:p15-hold|run:p15-hold|unit:p15-hold" in p15
    assert len(worker_digests) == 1
    print(os_name, "worker observations", group, "D-07 kill exit", killed[1], "copied worker hash", worker_digests.pop())


for platform, (os_name, architecture) in platforms.items():
    folder = download / ("test-results-" + platform)
    receipt = load(folder / "subject.json")
    assert (receipt["headSha"], receipt["runId"], receipt["runAttempt"], receipt["runnerOS"],
            receipt["runnerArchitecture"]) == (subject, str(run_id), "1", os_name, architecture)
    if platform == "ubuntu-latest":
        assert "linux-x64" in receipt["dotnetInfo"]
    print(platform, receipt["sdk"], receipt["runnerArchitecture"])
    for project, (assembly, coverage_dir, count, threshold) in projects.items():
        relative = f"{project}/tests.trx"
        trx = folder / relative
        assert digest(trx) == receipt["trxSha256"][relative]
        tree = ET.parse(trx)
        definitions = {}
        for unit in tree.findall(".//{*}UnitTest"):
            method = unit.find("{*}TestMethod").attrib
            assert unit.attrib["id"] not in definitions
            assert method["codeBase"].lower().endswith("/" + assembly.lower() + ".dll") or method["codeBase"].lower().endswith("\\" + assembly.lower() + ".dll")
            definitions[unit.attrib["id"]] = (method["className"], method["name"])
        results = tree.findall(".//{*}UnitTestResult")
        counters = tree.find(".//{*}Counters").attrib
        assert len(results) == len({result.attrib["executionId"] for result in results}) == count
        assert all(int(counters[key]) == count for key in ("total", "executed", "passed"))
        assert all(not int(counters[key]) for key in ("failed", "error", "notExecuted"))
        assert Counter(result.attrib["outcome"] for result in results) == {"Passed": count}
        assert all(result.attrib["testId"] in definitions for result in results)
        coverage = download / ("coverage-results-" + platform) / coverage_dir / "coverage.cobertura.xml"
        data = ET.parse(coverage).getroot().attrib
        assert all(float(data[key]) * 100 >= threshold for key in ("line-rate", "branch-rate"))
        print(assembly, count, "lines", data["lines-covered"], "/", data["lines-valid"], "branches",
              data["branches-covered"], "/", data["branches-valid"], "TRX SHA", digest(trx), "coverage SHA", digest(coverage))
        if project == "latticedb":
            cases, named = {}, set()
            for result in results:
                clazz, method = definitions[result.attrib["testId"]]
                if clazz == "Andirit.LatticeDb.Tests.NativePublicationCorrectnessTests":
                    case = re.match(r"P(\d\d)_", method)[1]
                    assert case not in named and result.attrib["testName"] == f"{clazz}.{method}"
                    named.add(case)
                    print(f"P-{case} {clazz}.{method} {result.attrib['outcome']} {result.attrib['duration']}")
                if clazz.endswith((".NativeDurabilityTests", ".NativePublicationCorrectnessTests")):
                    cases[method] = result.findtext(".//{*}StdOut") or ""
            assert named == {f"{index:02}" for index in range(1, 17)}
            assert "D07_ExitBeforeReadyFailsPromptly" in cases
            observations(cases, os_name)

for key, sha, conclusion in ((45639342986, "d3c9359d1abd8c89a7214e37f074802e5d2fa238", "success"),
                             (45513525415, "ed216e87a2222e974ff0e477d1b04d87f151acc4", "failure")):
    previous = load(root / str(key) / "run.json")
    assert previous["head_sha"] == sha and previous["conclusion"] == conclusion
    print("historical", key, sha, conclusion)

source = download / "test-results-ubuntu-latest"
for kind in ("original", "missing-case", "duplicate-case", "skipped", "failed", "removed-worker-output",
             "wrong-digest", "missing-artifact", "malformed", "wrong-subject", "wrong-run"):
    with tempfile.TemporaryDirectory(prefix="q2-hosted-control-") as scratch:
        folder = Path(scratch) / "results"
        shutil.copytree(source, folder)
        manifest = folder / "subject.json"
        original = load(manifest)
        path = folder / "latticedb/tests.trx"
        if kind in {"missing-case", "duplicate-case", "skipped", "failed", "removed-worker-output", "malformed"}:
            tree = ET.parse(path)
            results = tree.find(".//{*}Results")
            p = next(node for node in results if ".P15_" in node.attrib["testName"])
            totals = tree.find(".//{*}Counters")
            if kind in {"duplicate-case", "missing-case"}:
                if kind == "duplicate-case":
                    clone = deepcopy(p)
                    clone.attrib["executionId"] = str(uuid4())
                    results.append(clone)
                    delta = 1
                else:
                    results.remove(p)
                    delta = -1
                for counter in ("total", "executed", "passed"):
                    totals.attrib[counter] = str(int(totals.attrib[counter]) + delta)
            elif kind in {"skipped", "failed"}:
                p.attrib["outcome"] = "NotExecuted" if kind == "skipped" else "Failed"
            elif kind == "removed-worker-output":
                p.find(".//{*}StdOut").text = "missing ready and publish observations"
            tree.write(path, encoding="utf-8", xml_declaration=True)
            if kind == "malformed":
                path.write_bytes(b"<broken")
            original["trxSha256"]["latticedb/tests.trx"] = digest(path)
            manifest.write_text(json.dumps(original), encoding="utf-8")
        elif kind == "wrong-digest":
            path.write_bytes(path.read_bytes() + b"\n")
        elif kind == "missing-artifact":
            path.unlink()
        result = subprocess.run(["pwsh", "-NoProfile", "-File", verifier, "-Results", str(folder), "-ExpectedSha",
                                 "1" * 40 if kind == "wrong-subject" else subject, "-ExpectedRunId",
                                 "1" if kind == "wrong-run" else str(run_id), "-ExpectedRunAttempt", "1",
                                 "-ExpectedRunnerOS", "Linux", "-ExpectedRunnerArch", "X64"], capture_output=True)
        assert result.returncode == (0 if kind == "original" else 1), (kind, result.stdout, result.stderr)
        if kind in {"missing-case", "duplicate-case", "skipped", "failed", "removed-worker-output"}:
            assert b"P-15" in result.stderr, (kind, result.stderr)
        print("verifier", kind, "exit", result.returncode, result.stderr.decode(errors="replace").splitlines()[-1:] if result.stderr else "")
