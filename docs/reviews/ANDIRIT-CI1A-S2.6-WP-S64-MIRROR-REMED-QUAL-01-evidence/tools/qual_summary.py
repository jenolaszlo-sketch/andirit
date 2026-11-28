"""Build compact summary JSON for the independent qualification evidence."""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def trx(path):
    root = ET.parse(path).getroot()
    defs = {}
    for unit in root.findall("t:TestDefinitions/t:UnitTest", NS):
        method = unit.find("t:TestMethod", NS)
        defs[unit.get("id")] = method.get("className") + "." + method.get("name")
    results = {defs[r.get("testId")]: r.get("outcome")
               for r in root.findall("t:Results/t:UnitTestResult", NS)}
    counters = root.find("t:ResultSummary/t:Counters", NS)
    return {
        "total": int(counters.get("total")),
        "passed": int(counters.get("passed")),
        "failed": int(counters.get("failed")),
        "notExecuted": int(counters.get("notExecuted")),
        "failedNames": sorted(name for name, outcome in results.items()
                              if outcome == "Failed"),
    }


def rate(path):
    root = ET.parse(path).getroot()
    return {"line": round(float(root.get("line-rate")) * 100, 2),
            "branch": round(float(root.get("branch-rate")) * 100, 2)}


def main(base):
    base = Path(base)
    output = {}
    output["baselineProducerTests"] = {
        "latticedb": trx(base / "results" / "baseline-tests" / "base.trx"),
        "csharp": trx(base / "results" / "baseline-tests-csharp" / "base-csharp.trx"),
    }
    accepted = {}
    for project in ("core", "csharp", "latticedb", "treesitter"):
        accepted[project] = trx(base / "accepted-main" / project / "tests.trx")
    output["acceptedMain"] = accepted
    output["acceptedTotal"] = sum(item["total"] for item in accepted.values())
    coverage = {}
    for project, folder in (("core", "coverage-core"), ("csharp", "coverage-csharp"),
                            ("treesitter", "coverage-treesitter"),
                            ("latticedb", "coverage-latticedb")):
        coverage[project] = rate(base / "results" / "regression" / folder / "coverage.cobertura.xml")
    output["coverage"] = coverage
    print(json.dumps(output, indent=2))


if __name__ == "__main__":
    main(sys.argv[1])
