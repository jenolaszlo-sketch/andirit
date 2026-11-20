import base64
import json
import hashlib
from pathlib import Path
import xml.etree.ElementTree as ET

root = Path(__file__).resolve().parent

def receipt(rel):
    return json.loads((root / rel).read_bytes())

checked = []
for prefix in ("baseline-comparison", "additional"):
    for profile in ("ctor", "property", "reference", "incomplete"):
        for phase in ("publish", "reopen"):
            path = f"{prefix}/control-{profile}-{phase}.json"
            row = receipt(path)
            if profile == "incomplete":
                assert row["scope"]["IsComplete"] is False
                assert row["authorityRefusal"] == "IncompleteCompilationUniverse"
                assert row["result"]["Availability"] == 1
                assert row["result"]["Occurrences"] == [] and not row["result"]["IsComplete"]
                assert row["native"]["AndiritOccurrenceUnit"] == []
                checked.append(dict(receipt=path, availability="AnalysisUnavailable", persistedUnits=0))
                continue
            owner = next(item for item in row["native"]["AndiritOccurrenceUnit"]
                         if item["key"].endswith(hashlib.sha256(b"Caller/Caller.csproj").hexdigest()))
            payload = json.loads(base64.b64decode(owner["payload"]))
            observed = [coverage["UnsupportedCategories"] for coverage in [
                row["initialCoverage"], row["result"]["Coverage"], payload["Coverage"]]]
            assert observed == [0, 0, 0] and row["expected"]["expectedUnsupported"] == 1
            assert all(item["Span"]["Path"] != "Caller/Review.cs" for item in payload["Facts"])
            checked.append(dict(receipt=path, expectedUnsupported=1, observed=observed, controlOccurrences=0))
for prefix in ("declared", "baseline-declared"):
    for phase in ("publish", "reopen"):
        path = f"{prefix}/control-declared-{phase}.json"
        row = receipt(path)
        missing = "csharp:project:" + hashlib.sha256(b"Absent/Absent.csproj").hexdigest()
        assert row["scope"]["MissingScopeUnits"] == [missing]
        assert row["scope"]["Refusals"] == ["MissingDeclaredSource:Absent/Missing.cs"]
        assert row["result"]["Availability"] == 1 and not row["result"]["IsComplete"]
        assert row["result"]["Occurrences"] == [] and row["native"]["AndiritOccurrenceUnit"] == []
        checked.append(dict(receipt=path, missingDeclaredUnit=missing, expectedAvailability="IncompleteAnalysis",
                            availability="AnalysisUnavailable", persistedUnits=0))
report = ET.parse(root / "confirmation/coverage/coverage.cobertura.xml").getroot()
branches = dict(covered=int(report.attrib["branches-covered"]), valid=int(report.attrib["branches-valid"]), threshold=70)
assert branches == dict(covered=296, valid=450, threshold=70)
(root / "findings-receipt-summary.json").write_text(json.dumps(
    dict(inheritedMatrixFailures=checked, windowsCoverage=branches), indent=2) + "\n", encoding="utf-8")


