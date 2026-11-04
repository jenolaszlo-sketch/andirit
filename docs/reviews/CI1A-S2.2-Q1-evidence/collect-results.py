"""Extract reviewer observations from TRX and compare independent process runs."""
import hashlib
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

stage = Path(sys.argv[1])
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}

def read(path):
    tree = ET.parse(path)
    counters = tree.find("t:ResultSummary/t:Counters", ns).attrib
    observations = []
    for node in tree.findall(".//t:UnitTestResult", ns):
        text = node.findtext("t:Output/t:StdOut", "", ns)
        for line in text.splitlines():
            if line.startswith("{"):
                value = json.loads(line)
                value["test"] = node.attrib["testName"]
                observations.append(value)
    return dict(file=path.name, counters=counters, observations=observations)

result = {}
for directory in ("baseline-trx", "decision-baseline-trx", "producer-targeted-trx", "frozen-contract-trx", "reviewer-trx"):
    result[directory] = [read(p) for p in sorted((stage / directory).glob("*.trx"))]
complete = next(v for v in result["reviewer-trx"] if v["file"] == "reviewer-complete.trx")
repeat = next(v for v in result["reviewer-trx"] if v["file"] == "reviewer-repeat.trx")

def stable(observations):
    rows = []
    for value in observations:
        # Captured identity contains a deliberately unique temporary file path.
        clean = {k: v for k, v in value.items() if k != "identity"}
        rows.append(json.dumps(clean, sort_keys=True))
    return sorted(rows)

assert stable(complete["observations"]) == stable(repeat["observations"])
result["separateProcessStableObservations"] = len(complete["observations"])
result["comparisonExclusion"] = "Only metadata identity's random temp filename; byte hashes and all mapped identities compared. TRX run IDs/timestamps/durations are not compared."
result["oracle"] = {"sha256": hashlib.sha256((stage / "oracle-r2.json").read_bytes()).hexdigest(),
                    "summary": (stage / "oracle-r2.log").read_text().strip()}
(stage / "results.json").write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps({k: v for k, v in result.items() if k not in ("baseline-trx", "decision-baseline-trx", "producer-targeted-trx", "frozen-contract-trx", "reviewer-trx")}, indent=2))
for row in complete["observations"]:
    if row["kind"] not in ("encoding", "new02-anchor"):
        print(json.dumps(row))
