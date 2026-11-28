"""Independent audit of downloaded hosted artifacts for the qualification."""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
MERGE = "c549852450d950412ff7eb01f0916134f2539c2a"
RUN = "46035752304"
ATTEMPT = "1"
FLOORS = {"core": 55.0, "csharp": 75.0, "treesitter": 60.0, "latticedb": 70.0}
NEW = {
    "Andirit.LatticeDb.Tests.NativeGraphMirrorRemediationTests": 20,
    "Andirit.LatticeDb.Tests.NativeGraphMirrorOwnershipTests": 8,
    "Andirit.CSharp.Tests.NativeGraphMirrorLinkedFixtureTests": 1,
}
P = "Andirit.LatticeDb.Tests.NativePublicationCorrectnessTests"
D = "Andirit.LatticeDb.Tests.NativeDurabilityTests"
MANDATORY = {
    "P-01": f"{P}.P01_QueryPinnedToSupersededGeneration_RefusesExplicitly",
    "P-02": f"{P}.P02_OccurrencePageNotBoundToAcceptedPublication_RefusesExplicitlyOnOpen",
    "P-03": f"{P}.P03_StaleIndexDoesNotScanTheWholeStore_AndBoundedQueryExaminesOnlyItsTargetPage",
    "P-04": f"{P}.P04_FailedCompletion_PreservesPriorOccurrencePublicationAndIndex",
    "P-05": f"{P}.P05_OlderGenerationsSuperseded_LatestAcceptedAndDurable",
    "P-06": f"{P}.P06_RunPlannedAgainstSupersededBaseline_CannotCompleteAndRegressGeneration",
    "P-07": f"{P}.P07_OrdinaryReopen_OccurrenceQueryResultsEquivalent",
    "P-08": f"{P}.P08_RebuildFromDurableState_OccurrenceIndexEquivalent",
    "P-09": f"{P}.P09_ContributorUnionAndConflictRejection_VisibilityConsistent",
    "P-10": f"{P}.P10_ScopedDeletion_RemovesOnlyAffectedContextLeavesIndexClean",
    "P-11": f"{P}.P11_BoundedQuery_DeterministicSiteOrderAndExplicitLimit",
    "P-12": f"{P}.P12_ConcurrentReaderDuringPublication_SeesAtomicSnapshot",
    "P-13": f"{P}.P13_RapidSequentialPublications_OnlyLatestAccepted",
    "P-14": f"{P}.P14_UnknownCommitOutcome_QuarantineAndReopenCorroborates",
    "P-15": f"{P}.P15_MultiProcessPublication_FileLockSerializesAndReopenSeesCommittedPublication",
    "P-16": f"{P}.P16_InterleavingIsNotANativeEvidenceSurface",
    "D-05": f"{D}.ConcurrentMultiProcessAccess_FileLockingAndConsistency",
    "D-07": f"{D}.AbruptProcessTermination_DurableStateConsistent",
    "D-07-early-exit": f"{D}.D07_ExitBeforeReadyFailsPromptly",
}
MARKERS = {
    "P-12": ["readerQueued=true"],
    "P-15": ["READY|", "PUBLISHED|run:p15-pub|", "sha256="],
    "D-05": ["READY|", "PUBLISHED|run:durability-pub|", "CANCELLED|", "sha256="],
    "D-07": ["READY|", "KILLED exit=", "PUBLISHED|run:d07-after-recovery|", "sha256="],
}


def fail(message):
    raise SystemExit("NOT VERIFIED: " + message)


def parse_trx(path):
    root = ET.parse(path).getroot()
    defs = {}
    for unit in root.findall("t:TestDefinitions/t:UnitTest", NS):
        method = unit.find("t:TestMethod", NS)
        defs[unit.get("id")] = method.get("className") + "." + method.get("name")
    results = {defs[r.get("testId")]: r
               for r in root.findall("t:Results/t:UnitTestResult", NS)}
    counters = root.find("t:ResultSummary/t:Counters", NS)
    return counters, results


def rates(path):
    root = ET.parse(path).getroot()
    return (round(float(root.get("line-rate")) * 100, 2),
            round(float(root.get("branch-rate")) * 100, 2))


def main(base):
    base = Path(base)
    out = {"merge": MERGE, "run": RUN, "attempt": ATTEMPT, "runners": {}}
    for runner in ("windows-latest", "ubuntu-latest", "macos-latest"):
        tests = base / f"test-results-{runner}"
        subject = json.loads((tests / "subject.json").read_text(encoding="utf-8-sig"))
        if subject["headSha"] != MERGE:
            fail(f"{runner} subject SHA {subject['headSha']}")
        if str(subject["runId"]) != RUN or str(subject["runAttempt"]) != ATTEMPT:
            fail(f"{runner} run identity")
        entry = {"subject": subject, "projects": {}, "new": {}, "mandatory": {},
                 "coverage": {}}
        all_results = {}
        for project in ("core", "csharp", "treesitter", "latticedb"):
            counters, results = parse_trx(tests / project / "tests.trx")
            total, passed = int(counters.get("total")), int(counters.get("passed"))
            failed, skipped = int(counters.get("failed")), int(counters.get("notExecuted"))
            if failed or skipped or total != passed:
                fail(f"{runner} {project} failed={failed} skipped={skipped}")
            entry["projects"][project] = {"total": total, "passed": passed,
                                          "failed": failed, "notExecuted": skipped}
            all_results.update(results)
            for name, result in results.items():
                for klass, expected in NEW.items():
                    if name.startswith(klass + "."):
                        slot = entry["new"].setdefault(
                            klass, {"expected": expected, "passed": 0, "names": []})
                        if result.get("outcome") != "Passed":
                            fail(f"{runner} new test not passed: {name}")
                        slot["passed"] += 1
                        slot["names"].append(name.rsplit(".", 1)[1])
        for klass, expected in NEW.items():
            slot = entry["new"].get(klass)
            if not slot or slot["passed"] != expected:
                fail(f"{runner} {klass} missing or count mismatch")
        for case, name in MANDATORY.items():
            result = all_results.get(name)
            if result is None or result.get("outcome") != "Passed":
                fail(f"{runner} mandatory {case} not passed")
            stdout = result.find("t:Output/t:StdOut", NS)
            text = stdout.text if stdout is not None and stdout.text else ""
            for marker in MARKERS.get(case, []):
                if marker not in text:
                    fail(f"{runner} mandatory {case} missing marker {marker}")
            entry["mandatory"][case] = "Passed"
        cov = base / f"coverage-results-{runner}"
        for project, folder in (("core", "coverage"), ("csharp", "coverage-csharp"),
                                ("treesitter", "coverage-treesitter"),
                                ("latticedb", "coverage-latticedb")):
            line, branch = rates(cov / folder / "coverage.cobertura.xml")
            if line < FLOORS[project] or branch < FLOORS[project]:
                fail(f"{runner} {project} coverage {line}/{branch}")
            entry["coverage"][project] = {"line": line, "branch": branch}
        out["runners"][runner] = entry
    print(json.dumps(out, indent=2))


if __name__ == "__main__":
    main(sys.argv[1])
