"""Independent parse of locally executed TRX receipts."""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
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
NEW = {
    "Andirit.LatticeDb.Tests.NativeGraphMirrorRemediationTests": 20,
    "Andirit.LatticeDb.Tests.NativeGraphMirrorOwnershipTests": 8,
    "Andirit.CSharp.Tests.NativeGraphMirrorLinkedFixtureTests": 1,
}


def parse(path):
    root = ET.parse(path).getroot()
    defs = {}
    for unit in root.findall("t:TestDefinitions/t:UnitTest", NS):
        method = unit.find("t:TestMethod", NS)
        defs[unit.get("id")] = method.get("className") + "." + method.get("name")
    results = {defs[r.get("testId")]: r
               for r in root.findall("t:Results/t:UnitTestResult", NS)}
    counters = root.find("t:ResultSummary/t:Counters", NS)
    return counters, results


def main(base):
    base = Path(base)
    report = {"projects": {}, "new": {}, "mandatory": {}}
    all_results = {}
    for project in ("core", "csharp", "treesitter", "latticedb"):
        counters, results = parse(base / project / "tests.trx")
        total = int(counters.get("total"))
        passed = int(counters.get("passed"))
        failed = int(counters.get("failed"))
        skipped = int(counters.get("notExecuted"))
        assert failed == 0 and skipped == 0 and total == passed, project
        report["projects"][project] = {"total": total, "passed": passed,
                                       "failed": failed, "notExecuted": skipped}
        all_results.update(results)
        for name, result in results.items():
            for klass, expected in NEW.items():
                if name.startswith(klass + "."):
                    slot = report["new"].setdefault(
                        klass, {"expected": expected, "passed": 0, "names": []})
                    assert result.get("outcome") == "Passed", name
                    slot["passed"] += 1
                    slot["names"].append(name.rsplit(".", 1)[1])
    for klass, expected in NEW.items():
        slot = report["new"].get(klass)
        assert slot and slot["passed"] == expected, klass
    for case, name in MANDATORY.items():
        result = all_results.get(name)
        assert result is not None and result.get("outcome") == "Passed", case
        report["mandatory"][case] = "Passed"
    report["total"] = sum(p["total"] for p in report["projects"].values())
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main(sys.argv[1])
