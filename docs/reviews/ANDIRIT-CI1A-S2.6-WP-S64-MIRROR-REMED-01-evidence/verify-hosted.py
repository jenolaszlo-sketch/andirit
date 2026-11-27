"""Verify downloaded hosted CI receipts for S64-MIRROR-REMED-01.

Reads the test-results-* and coverage-results-* artifact directories, checks the
subject binding, per-project counters, the S6.3 mandatory named cases, the 29
new remediation/ownership tests, and the coverage floors. Exits non-zero on any
mismatch.
"""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
EXPECTED_SHA = "c549852450d950412ff7eb01f0916134f2539c2a"
RUN_ID = "46035752304"
ATTEMPT = "1"
FLOORS = {"core": 55.0, "csharp": 75.0, "treesitter": 60.0, "latticedb": 70.0}
PROJECTS = ("core", "csharp", "treesitter", "latticedb")
NEW_CLASSES = {
    "Andirit.LatticeDb.Tests.NativeGraphMirrorRemediationTests": 20,
    "Andirit.LatticeDb.Tests.NativeGraphMirrorOwnershipTests": 8,
    "Andirit.CSharp.Tests.NativeGraphMirrorLinkedFixtureTests": 1,
}
PUBLICATION = "Andirit.LatticeDb.Tests.NativePublicationCorrectnessTests"
DURABILITY = "Andirit.LatticeDb.Tests.NativeDurabilityTests"
MANDATORY = {
    "P-01": f"{PUBLICATION}.P01_QueryPinnedToSupersededGeneration_RefusesExplicitly",
    "P-02": f"{PUBLICATION}.P02_OccurrencePageNotBoundToAcceptedPublication_RefusesExplicitlyOnOpen",
    "P-03": f"{PUBLICATION}.P03_StaleIndexDoesNotScanTheWholeStore_AndBoundedQueryExaminesOnlyItsTargetPage",
    "P-04": f"{PUBLICATION}.P04_FailedCompletion_PreservesPriorOccurrencePublicationAndIndex",
    "P-05": f"{PUBLICATION}.P05_OlderGenerationsSuperseded_LatestAcceptedAndDurable",
    "P-06": f"{PUBLICATION}.P06_RunPlannedAgainstSupersededBaseline_CannotCompleteAndRegressGeneration",
    "P-07": f"{PUBLICATION}.P07_OrdinaryReopen_OccurrenceQueryResultsEquivalent",
    "P-08": f"{PUBLICATION}.P08_RebuildFromDurableState_OccurrenceIndexEquivalent",
    "P-09": f"{PUBLICATION}.P09_ContributorUnionAndConflictRejection_VisibilityConsistent",
    "P-10": f"{PUBLICATION}.P10_ScopedDeletion_RemovesOnlyAffectedContextLeavesIndexClean",
    "P-11": f"{PUBLICATION}.P11_BoundedQuery_DeterministicSiteOrderAndExplicitLimit",
    "P-12": f"{PUBLICATION}.P12_ConcurrentReaderDuringPublication_SeesAtomicSnapshot",
    "P-13": f"{PUBLICATION}.P13_RapidSequentialPublications_OnlyLatestAccepted",
    "P-14": f"{PUBLICATION}.P14_UnknownCommitOutcome_QuarantineAndReopenCorroborates",
    "P-15": f"{PUBLICATION}.P15_MultiProcessPublication_FileLockSerializesAndReopenSeesCommittedPublication",
    "P-16": f"{PUBLICATION}.P16_InterleavingIsNotANativeEvidenceSurface",
    "D-05": f"{DURABILITY}.ConcurrentMultiProcessAccess_FileLockingAndConsistency",
    "D-07": f"{DURABILITY}.AbruptProcessTermination_DurableStateConsistent",
    "D-07-early-exit": f"{DURABILITY}.D07_ExitBeforeReadyFailsPromptly",
}
MARKERS = {
    "P-12": ["readerQueued=true"],
    "P-15": ["READY|", "PUBLISHED|run:p15-pub|", "sha256="],
    "D-05": ["READY|", "PUBLISHED|run:durability-pub|", "CANCELLED|", "sha256="],
    "D-07": ["READY|", "KILLED exit=", "PUBLISHED|run:d07-after-recovery|", "sha256="],
}


def parse_trx(path):
    root = ET.parse(path).getroot()
    definitions = {}
    for unit in root.findall("t:TestDefinitions/t:UnitTest", NS):
        method = unit.find("t:TestMethod", NS)
        definitions[unit.get("id")] = f"{method.get('className')}.{method.get('name')}"
    results = {}
    for result in root.findall("t:Results/t:UnitTestResult", NS):
        name = definitions[result.get("testId")]
        results[name] = result
    counters = root.find("t:ResultSummary/t:Counters", NS)
    return {
        "total": int(counters.get("total")),
        "passed": int(counters.get("passed")),
        "failed": int(counters.get("failed")),
        "notExecuted": int(counters.get("notExecuted")),
        "results": results,
    }


def check(condition, message):
    if not condition:
        raise SystemExit("FAILED: " + message)


def coverage(path):
    root = ET.parse(path).getroot()
    return {
        "line": round(float(root.get("line-rate")) * 100, 2),
        "branch": round(float(root.get("branch-rate")) * 100, 2),
    }


def main(base):
    base = Path(base)
    report = {"expectedSha": EXPECTED_SHA, "runId": RUN_ID, "attempt": ATTEMPT,
              "runners": {}}
    for runner in ("windows-latest", "ubuntu-latest", "macos-latest"):
        tests = base / f"test-results-{runner}"
        subject = json.loads((tests / "subject.json").read_text(encoding="utf-8-sig"))
        check(subject["headSha"] == EXPECTED_SHA, f"{runner} subject headSha")
        check(str(subject["runId"]) == RUN_ID, f"{runner} run id")
        check(str(subject["runAttempt"]) == ATTEMPT, f"{runner} attempt")
        entry = {"subject": {k: subject[k] for k in
                             ("headSha", "runnerOS", "runnerArchitecture", "runId",
                              "runAttempt", "sdk")},
                 "projects": {}, "newTests": {}, "mandatory": {}}
        for project in PROJECTS:
            parsed = parse_trx(tests / project / "tests.trx")
            check(parsed["failed"] == 0 and parsed["notExecuted"] == 0,
                  f"{runner} {project} has failed/notExecuted tests")
            check(parsed["total"] == parsed["passed"],
                  f"{runner} {project} total != passed")
            entry["projects"][project] = {k: parsed[k] for k in
                                          ("total", "passed", "failed", "notExecuted")}
            for name, result in parsed["results"].items():
                for klass, expected in NEW_CLASSES.items():
                    if name.startswith(klass + "."):
                        entry["newTests"].setdefault(klass, {"expected": expected,
                                                             "passed": 0, "names": []})
                        check(result.get("outcome") == "Passed",
                              f"{runner} new test not passed: {name}")
                        entry["newTests"][klass]["passed"] += 1
                        entry["newTests"][klass]["names"].append(name.split(".")[-1])
            for case, name in MANDATORY.items():
                if name in parsed["results"]:
                    result = parsed["results"][name]
                    check(result.get("outcome") == "Passed",
                          f"{runner} mandatory {case} not passed")
                    stdout = result.find("t:Output/t:StdOut", NS)
                    text = stdout.text if stdout is not None and stdout.text else ""
                    for marker in MARKERS.get(case, []):
                        check(marker in text, f"{runner} mandatory {case} marker {marker}")
                    entry["mandatory"][case] = "Passed"
        for klass, expected in NEW_CLASSES.items():
            actual = entry["newTests"].get(klass)
            check(actual is not None and actual["passed"] == expected,
                  f"{runner} {klass} count {actual and actual['passed']} != {expected}")
        coverage_dir = base / f"coverage-results-{runner}"
        entry["coverage"] = {}
        for project, folder in (("core", "coverage"), ("csharp", "coverage-csharp"),
                                ("treesitter", "coverage-treesitter"),
                                ("latticedb", "coverage-latticedb")):
            rates = coverage(coverage_dir / folder / "coverage.cobertura.xml")
            check(rates["line"] >= FLOORS[project] and rates["branch"] >= FLOORS[project],
                  f"{runner} {project} coverage below floor")
            entry["coverage"][project] = rates
        report["runners"][runner] = entry
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main(sys.argv[1])
