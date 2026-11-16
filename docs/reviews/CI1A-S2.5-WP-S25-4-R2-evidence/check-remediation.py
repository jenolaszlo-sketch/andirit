import base64
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
owner = hashlib.sha256(b"Caller/Caller.csproj").hexdigest()

def read(path):
    return json.loads((root / path).read_bytes())

def rows(receipt):
    return [json.loads(base64.b64decode(row["payload"]))
            for row in receipt["native"]["AndiritOccurrenceUnit"]]

def status(result):
    return {0: "Available", 1: "AnalysisUnavailable", 5: "IncompleteAnalysis"}[result["Availability"]]

results = []
for source, prefix in (("R1", "baseline"), ("R2", "probes/corrected")):
    for case in ("ctor", "property", "reference", "declared", "incomplete"):
        pair = []
        for phase in ("publish", "reopen"):
            path = f"{prefix}/control-{case}-{phase}.json"
            record = read(path)
            native = rows(record)
            result = record["result"]
            summary = dict(receipt=path, status=status(result), complete=result["IsComplete"],
                           pid=record["processId"])
            if case in {"ctor", "property", "reference"}:
                expected = int(source == "R2")
                caller = next(unit for unit in native if unit["IndexUnitId"].endswith(owner))
                counts = [stage["UnsupportedCategories"] for stage in
                          (record["initialCoverage"], result["Coverage"], caller["Coverage"])]
                unsupported = [stage["Unsupported"] for stage in
                               (record["initialCoverage"], result["Coverage"], caller["Coverage"])]
                assert counts == unsupported == [expected] * 3
                assert len(caller["Facts"]) == 50 and result["IsComplete"]
                assert not any(fact["Span"]["Path"] == "Caller/Review.cs" for fact in caller["Facts"])
                summary.update(expectedUnsupported=1, unsupportedCounts=counts, positiveFacts=50,
                               unsupportedOccurrences=0)
            elif source == "R2":
                assert result["Availability"] == 5 and not result["IsComplete"] and not result["Occurrences"]
                assert result["Coverage"]["DeclaredUnitCount"] == 3 and result["Coverage"]["VerifiedUnitCount"] == 2
                assert record["authorityRefusal"] == "IncompleteCompilationUniverse"
                assert len(native) == 2
                assert sum(unit["Coverage"]["DeclaredUnitCount"] for unit in native) == 3
                assert all(unit["Coverage"]["State"] == 1 and unit["Coverage"]["VerifiedUnitCount"] == 1 and
                           unit["Authority"] is None and not unit["RequiresAuthority"] and not unit["Facts"]
                           for unit in native)
                summary.update(declared=3, verified=2, nativeUnitCount=2, occurrences=0)
            else:
                assert result["Availability"] == 1 and result["Coverage"] is None and not native
                summary.update(nativeUnitCount=0, occurrences=0)
            pair.append(summary)
        assert pair[0]["pid"] != pair[1]["pid"]
        results.extend(pair)

m54 = {}
for profile in ("transitive", "direct"):
    for phase in ("publish", "reopen"):
        path = f"probes/corrected/m54-{profile}-{phase}.json"
        record = read(path)
        assert not record["failures"], path
        unresolved, facts = (2, 1) if profile == "transitive" else (0, 3)
        assert record["containedAncestorReference"] == (profile == "direct")
        assert [item["resolution"] for item in record["binding"]] == (
            [None, None, "Complete"] if profile == "transitive" else ["Complete"] * 3)
        assert record["exactMissingDefiningCompilationSites"] == 0
        for coverage in (record["initialCoverage"], record["query"]["Coverage"], record["decodedCallerUnit"]["Coverage"]):
            assert (coverage["Unresolved"], coverage["External"], coverage["InvocationFacts"]) == (unresolved, 0, facts)
        assert record["query"]["IsComplete"]
        m54[f"{profile}/{phase}"] = dict(receipt=path, pid=record["processId"],
            attemptedSites=record["binding"], unresolved=unresolved, totalOccurrences=facts,
            exactMissingDefiningSites=0, exactContinuity=False)
    assert m54[f"{profile}/publish"]["pid"] != m54[f"{profile}/reopen"]["pid"]

for receipt in read("historical/sq04-integrity.json"):
    assert receipt["exit"] == 0
    for item in receipt["files"]:
        assert item["masterBefore"] == item["masterAfter"] == item["copyBefore"]

(root / "remediation-results.json").write_text(json.dumps(
    dict(coverageAndAvailability=results, missingDefiningContinuity=m54), indent=2) + "\n", encoding="utf-8")
print("R1 reproduction, R2 native remediation, exact M5.4 gap, and historic copy receipts verified")

