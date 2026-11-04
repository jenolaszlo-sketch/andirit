"""Summarize raw TRX without filtering tests or rewriting reviewer semantics."""
from pathlib import Path
import hashlib, json, sys, xml.etree.ElementTree as ET

root = Path(sys.argv[1])
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
def trx(path):
    node = ET.parse(path).getroot()
    return {"file": path.relative_to(root).as_posix(),
        "counters": node.find("t:ResultSummary/t:Counters", ns).attrib,
        "tests": [{"name": t.attrib["testName"], "outcome": t.attrib["outcome"],
            "stdout": t.findtext("t:Output/t:StdOut", default="", namespaces=ns),
            "error": t.findtext("t:Output/t:ErrorInfo/t:Message", default="", namespaces=ns)}
            for t in node.findall("t:Results/t:UnitTestResult", ns)]}
groups = {}
for group in ("q1-before", "q1-after", "regression-before", "regression-after", "solution-after"):
    groups[group] = [trx(p) for p in sorted((root / group).glob("*.trx"))]
before = {t["name"]: t for t in groups["q1-before"][0]["tests"]}
after = {t["name"]: t for t in groups["q1-after"][0]["tests"]}
assert before.keys() == after.keys() and len(before) == 48
assert all(t["outcome"] == "Passed" for t in before.values())
failures = [name for name, t in after.items() if t["outcome"] != "Passed"]
assert len(failures) == 3 and all("ObserveMalformedMetadataStillExposesIdentity" in n for n in failures)
lines = ["| Unchanged Q1 probe/case | Before | After | Interpretation |", "| --- | --- | --- | --- |"]
for name in sorted(before):
    short = name.removeprefix("IndependentFoundationReview.FoundationQualificationProbes.")
    result = after[name]["outcome"]
    lines.append(f"| `{short.replace('|', '&#124;')}` | Passed | {result} | " +
        ("Expected observation mismatch: capture now refuses; producer regression passes." if result != "Passed" else "Preserved.") + " |")
(root / "q1-probe-matrix.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
(root / "results.json").write_text(json.dumps(groups, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
digest = hashlib.sha256((root / "oracle-r2.json").read_bytes()).hexdigest()
print(f"Q1 cases: {len(before)}; after: {len(after)-len(failures)} passed, {len(failures)} expected observation mismatches")
for group, runs in groups.items():
    for run in runs: print(group, run["file"], run["counters"])
print("Oracle output SHA-256:", digest)
