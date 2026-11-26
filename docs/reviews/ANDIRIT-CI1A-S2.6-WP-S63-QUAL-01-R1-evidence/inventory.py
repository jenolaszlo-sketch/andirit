"""Independently inventory local test results and retained native assets."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
from zipfile import ZipFile
import xml.etree.ElementTree as ET

staging, tested_repo, package = map(Path, sys.argv[1:4])
root = staging / "local-R2"
tags = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
baseline_id = "d3c9359d1abd8c89a7214e37f074802e5d2fa238"
tested_id = "a96c19a844f8ea1719508198b47df23c4e203571"
publication_path = "tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs"
definition = re.compile(r"\[Fact\]\s+public (?:async Task|void) (P\d\d_\w+)\(")
suite_paths = {
    "core": "Andirit.Tests",
    "csharp": "Andirit.CSharp.Tests",
    "treesitter": "Andirit.TreeSitter.Tests",
    "latticedb": "Andirit.LatticeDb.Tests",
}


def git(*args):
    return subprocess.check_output(["git", "-C", str(tested_repo), *args]).decode()


def tests(file, outcome, count):
    document = ET.parse(file).getroot()
    results = document.findall(f"./{tags}Results/{tags}UnitTestResult")
    definitions = {}
    for item in document.findall(f"./{tags}TestDefinitions/{tags}UnitTest"):
        method = item.find(tags + "TestMethod")
        definitions[item.attrib["id"]] = (method.attrib["className"] + "." + method.attrib["name"],
                                         item.find(tags + "Execution").attrib["id"])
    assert len(results) == count and Counter(row.attrib["outcome"] for row in results) == {outcome: count}
    assert len({row.attrib["executionId"] for row in results}) == count
    for row in results:
        assert definitions[row.attrib["testId"]][1] == row.attrib["executionId"]
    return [(definitions[row.attrib["testId"]][0], "".join(row.itertext())) for row in results]


frozen_names = definition.findall(git("show", f"{baseline_id}:{publication_path}"))
r2_names = definition.findall(git("show", f"{tested_id}:{publication_path}"))
assert frozen_names == r2_names and [name[:3] for name in r2_names] == [f"P{i:02}" for i in range(1, 17)]
dispatched = tests(root / "publication/tests.trx", "Passed", 16)
expected = {"Andirit.LatticeDb.Tests.NativePublicationCorrectnessTests." + name for name in r2_names}
assert {name for name, _ in dispatched} == expected
for name in sorted(expected):
    print("local named case", name)

for label, count, threshold in zip(suite_paths, (345, 372, 157, 131), (55, 75, 60, 70), strict=True):
    tests(root / label / "tests.trx", "Passed", count)
    coverage_file, = (root / label / "coverage").glob("*.xml")
    coverage = ET.parse(coverage_file).getroot().attrib
    assert min(float(coverage["line-rate"]), float(coverage["branch-rate"])) * 100 >= threshold
    print(label, suite_paths[label], count, "passed, 0 failed, 0 skipped", "line/branch",
          coverage["line-rate"], coverage["branch-rate"], "threshold", threshold)

for directory, count in (("deadline", 9), ("recovery-controls", 7), ("copied-worker", 3)):
    rows = tests(root / directory / "tests.trx", "Passed", count)
    print(directory, count, "passed", [name for name, _ in rows])
for label, marker in (("early-worker", "exited before READY (exit 19)"),
                      ("missing-worker", "Worker assembly was not built")):
    [(name, detail)] = tests(root / label / "tests.trx", "Failed", 1)
    assert name.endswith(".AbruptProcessTermination_DurableStateConsistent") and marker in detail
    print(label, "expected failure", name, marker)

for repetition in range(1, 21):
    label = f"d07-{repetition:02}"
    [(_, detail)] = tests(root / label / "tests.trx", "Passed", 1)
    ready, = re.findall(r"READY\|(\d+)\|repo:d07-held\|run:d07-held\|unit:abrupt KILLED exit=(-?\d+)", detail)
    observations = re.findall(r"READ\|(\d+)\|(run:d07-[\w-]+)\|([0-9a-f]{64})", detail)
    (baseline_pid, baseline_run, baseline_index), (later_pid, later_run, later_index) = observations
    assert len({ready[0], baseline_pid, later_pid}) == 3 and ready[1] != "0"
    assert (baseline_run, later_run) == ("run:d07-baseline", "run:d07-after-recovery")
    assert baseline_index != later_index
    print(label, "passed", "baseline, ready/killed and later PIDs", baseline_pid, ready[0], later_pid, "kill exit", ready[1])

original = (staging / "local-R1/q1-replay.log").read_text(encoding="utf-8")
corrected = (root / "q1-replay.log").read_text(encoding="utf-8")
assert original.count("\nlate_success repetition=") == 5 and original.count("\nPASS ") == 6
assert corrected.count("\ntimeout repetition=") == 5 and corrected.count("\nPASS ") == 5
assert corrected.count("\nFAIL ") == 1 and "Late success was not reproduced" in corrected
prepared = (root / "prepared.log").read_text(encoding="utf-8")
assert len(re.findall(r"^[A-I] [123] PASS", prepared, re.MULTILINE)) == 27
for root_dir in (root, staging / "local-R1"):
    assert all(value["matches"] for value in json.loads((root_dir / "results.json").read_text()).values())
print("Original reproducers, 27 prepared repetitions and all command outcomes verified")

package_file, = package.glob("latticedbsharp.0.2.0.nupkg")
for file in sorted(package.rglob("*")):
    if file.suffix.lower() not in {".dll", ".so", ".dylib", ".nupkg"}:
        continue
    content = file.read_bytes()
    member = file.relative_to(package).as_posix()
    print("LatticeDbSharp 0.2.0", member, len(content), hashlib.sha256(content).hexdigest())
    if file != package_file:
        with ZipFile(package_file) as zip_file:
            assert zip_file.read(member) == content

production_delta = git("diff", "--name-only", baseline_id, tested_id, "--", "src", "Directory.Build.props",
                       "Directory.Build.targets", "docs", "Andirit.slnx", ".github/workflows/publish.yml")
assert not any(not row.startswith("docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL") for row in production_delta.splitlines())
print("Production, build definitions, release workflow and controlling authority preserved")
