"""Independently parse Windows and downloaded hosted R2 artifacts."""

import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

root = Path(sys.argv[1])
subject = sys.argv[2]
run_id = sys.argv[3]
check_expected = {
    "core": (345, 55),
    "csharp": (372, 75),
    "treesitter": (157, 60),
    "latticedb": (131, 70),
}
required_publication = {f"{case:02}" for case in range(1, 17)}
required_native = {
    "ConcurrentMultiProcessAccess_FileLockingAndConsistency",
    "AbruptProcessTermination_DurableStateConsistent",
    "D07_ExitBeforeReadyFailsPromptly",
}
locations = {
    "core": "coverage", "csharp": "coverage-csharp",
    "treesitter": "coverage-treesitter", "latticedb": "coverage-latticedb",
}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def results(folder, cover_folder, os_name, architecture, hosted=False):
    evidence = read_json(folder / "subject.json")
    assert evidence["headSha"] == subject
    assert evidence["runId"] == (run_id if hosted else "local-r2")
    assert evidence["runnerOS"] == os_name and evidence["runnerArchitecture"] == architecture
    print(f"{os_name} {architecture} run={evidence['runId']} attempt={evidence['runAttempt']} sdk={evidence['sdk']} commit={subject}")
    total = 0
    for project, (expected, threshold) in check_expected.items():
        relative = f"{project}/tests.trx"
        path = folder / relative
        assert sha(path) == evidence["trxSha256"][relative]
        document = ET.parse(path)
        tests = document.findall(".//{*}UnitTestResult")
        definitions = {test.attrib["id"]: test.find("{*}TestMethod") for test in document.findall(".//{*}UnitTest")}
        counters = document.find(".//{*}Counters").attrib
        assert len(tests) == int(counters["total"]) == int(counters["passed"]) == expected
        assert all(test.attrib["outcome"] == "Passed" for test in tests)
        assert all(test.attrib["testId"] in definitions for test in tests)
        total += expected
        coverage = cover_folder / (locations[project] if hosted else project) / "coverage.cobertura.xml"
        metrics = ET.parse(coverage).getroot().attrib
        assert all(float(metrics[key]) * 100 >= threshold for key in ("line-rate", "branch-rate"))
        print(f"  {project} {expected} passed, 0 failed, 0 skipped | lines={metrics['lines-covered']}/{metrics['lines-valid']} | "
              f"branches={metrics['branches-covered']}/{metrics['branches-valid']} | trx={sha(path)} | coverage={sha(coverage)}")
        if project == "latticedb":
            cases = {}
            native = set()
            for test in tests:
                method = definitions[test.attrib["testId"]].attrib
                name = method["className"] + "." + method["name"]
                match = re.fullmatch(r"P(\d{2})_.+", method["name"])
                if method["className"].endswith(".NativePublicationCorrectnessTests") and match:
                    case = match.group(1)
                    assert case not in cases, name
                    cases[case] = (name, test)
                if method["className"].endswith(".NativeDurabilityTests") and method["name"] in required_native:
                    native.add(method["name"])
                    print(f"    {name} Passed duration={test.get('duration')}")
            assert set(cases) == required_publication and native == required_native
            for case, (name, test) in sorted(cases.items()):
                print(f"    P-{case} {name} Passed duration={test.get('duration')}")
    print(f"  all suites {total} passed, 0 failed, 0 skipped")


local = root / "local"
results(local / "test-results", local / "coverage", "Windows", "X64")
solution = [ET.parse(path).find(".//{*}Counters").attrib for path in (local / "solution").glob("*.trx")]
assert len(solution) == 4 and sum(int(unit["passed"]) for unit in solution) == 1005
for repeat in range(1, 21):
    testpath = local / f"d07-{repeat:02d}" / "tests.trx"
    results_xml = ET.parse(testpath).findall(".//{*}UnitTestResult")
    assert len(results_xml) == 1 and results_xml[0].attrib["outcome"] == "Passed"
    print(f"local D-07 repeat {repeat:02d} Passed duration={results_xml[0].get('duration')} trx={sha(testpath)}")

hosted = root / "hosted" / run_id
run = read_json(hosted / "run.json")
assert str(run["id"]) == run_id and run["head_sha"] == subject and run["head_branch"] == "main"
assert run["event"] == "push" and run["conclusion"] == "success" and run["path"] == ".github/workflows/ci.yml"
jobs = read_json(hosted / "jobs.json")["jobs"]
assert len(jobs) == 4 and all(job["head_sha"] == subject and job["conclusion"] == "success" for job in jobs)
for job in jobs:
    print(f"Job {job['name']} id={job['id']} group={job['runner_group_name']} runner={job['runner_name']} conclusion={job['conclusion']}")
artifacts = read_json(hosted / "artifacts.json")["artifacts"]
expected_artifacts = {f"{kind}-results-{platform}" for kind in ("test", "coverage")
                      for platform in ("ubuntu-latest", "windows-latest", "macos-latest")}
assert set(item["name"] for item in artifacts) == expected_artifacts
for item in artifacts:
    assert item["workflow_run"]["head_sha"] == subject and str(item["workflow_run"]["id"]) == run_id
    print(f"Artifact {item['name']} id={item['id']} digest={item.get('digest')}")
for platform, os_name, arch in (("ubuntu-latest", "Linux", "X64"), ("windows-latest", "Windows", "X64"),
                                ("macos-latest", "macOS", "ARM64")):
    files = hosted / "download"
    folder = files / ("test-results-" + platform)
    assert read_json(folder / "subject.json")["runAttempt"] == str(run["run_attempt"])
    results(folder, files / ("coverage-results-" + platform), os_name, arch, hosted=True)
