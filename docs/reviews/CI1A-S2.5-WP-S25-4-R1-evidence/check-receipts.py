import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def read(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def digest(fields):
    h = hashlib.sha256()
    for field in fields:
        encoded = str(field).encode()
        h.update(len(encoded).to_bytes(4, "big"))
        h.update(encoded)
    return h.hexdigest()


def verify_binding(path, expected_status, external, unresolved, expected_failures):
    run = read(path)
    observed = run["externalObserved"]
    assert observed["resolution"] == expected_status, path
    assert observed["candidates"] == [] and observed["definingOwner"] is None, path
    assert observed["assembly"].startswith("System.Private.CoreLib,"), path
    assert run["failures"] == expected_failures, path

    result = run["result"]
    unit = run["decodedCallerUnit"]
    body = unit["Facts"]
    for coverage in [result["Coverage"], unit["Coverage"], unit["Authority"]["Coverage"]]:
        assert (coverage["External"], coverage["Unresolved"], coverage["Ambiguous"]) == (external, unresolved, 0), path
        assert coverage["InvocationFacts"] == 50, path
    assert result["IsComplete"] and unit["RequiresAuthority"], path

    fields = run["expectedDefiningCompctx1Fields"]
    compctx = "compctx1:sha256:" + digest([
        "compctx1-context", *(fields[name] for name in (
            "ContractVersion", "RepositoryId", "ProjectUnitId", "AssemblyName",
            "MetadataIdentity", "ModuleName", "OptionsDigest")),
        *(source["CanonicalForm"] for source in fields["Sources"]), *fields["References"],
    ])
    parts = run["expectedTargetParts"]
    assert compctx == fields["DescriptorDigest"] == parts[2], path
    assert parts[3:5] == [fields["MetadataIdentity"], fields["ModuleName"]], path
    target = "asmctx3:sha256:" + digest(parts)
    assert target == run["expectedTarget"], path
    expected = run["expectedFacts"]
    assert len(body) == len(result["Occurrences"]) == len(expected) == len(unit["Authority"]["Facts"]) == 50, path
    assert set(expected) == {hit["Id"]["Value"] for hit in result["Occurrences"]}, path
    assert set(expected) == {hit["Id"]["Value"] for hit in body}, path
    for fact in body:
        identifier = fact["Id"]["Value"]
        site = expected[identifier]
        span = site["span"]
        assert (fact["SourceUnitId"], fact["TargetId"]["Value"], fact["TargetDocumentId"],
                fact["Span"], fact["SourceHash"]) == (
                    site["owner"], target, site["doc"], span, site["sourceHash"]), path
        assert fact["DeclarationSpan"]["Path"] == "Dependency/Api.cs", path
        assert identifier == digest([
            "usage4", "review:s254:binding", "penghou.hetu.csharp", "usage4-b2c-exp-r2-20261101",
            site["owner"], span["Path"], site["sourceHash"], span["StartLine"], span["StartColumn"],
            span["EndLine"], span["EndColumn"], site["doc"], target, "method-invocation",
        ]), path
    assert all(hit["TargetId"]["Value"] == target and hit["SourceUnitId"] == expected[hit["Id"]["Value"]]["owner"]
               for hit in result["Occurrences"]), path
    assert all(authority["DefiningProjectUnitId"] == fields["ProjectUnitId"]
               for authority in unit["Authority"]["Facts"]), path

    return run, {
        "processId": run["processId"],
        "status": expected_status,
        "externalOccurrences": len(body) - len(expected),
        "positiveOccurrences": len(body),
        "external": external,
        "unresolved": unresolved,
        "callerOwner": unit["Authority"]["SourceUnitId"],
        "target": target,
        "definingUnit": fields["ProjectUnitId"],
        "componentDigests": parts[-3:],
        "orderedReferenceCount": len(fields["References"]),
        "positiveIds": sorted(expected),
    }


def compare_profiles(old, new):
    assert old["expectedTargetParts"][-3:] == new["expectedTargetParts"][-3:]
    assert old["expectedDefiningCompctx1Fields"]["Sources"] == new["expectedDefiningCompctx1Fields"]["Sources"]
    old_fields, new_fields = (r["expectedDefiningCompctx1Fields"] for r in (old, new))
    equal = old_fields == new_fields
    def stable_sites(run):
        return sorted((site["span"]["Path"], site["span"]["StartLine"], site["span"]["StartColumn"],
                       site["span"]["EndLine"], site["span"]["EndColumn"], site["sourceHash"],
                       site["owner"], site["doc"]) for site in run["expectedFacts"].values())
    assert stable_sites(old) == stable_sites(new)
    if equal:
        assert old["expectedFacts"] == new["expectedFacts"]
        assert old["expectedTarget"] == new["expectedTarget"]
    else:
        assert {k: v for k, v in old_fields.items() if k not in ("References", "DescriptorDigest")} == {
            k: v for k, v in new_fields.items() if k not in ("References", "DescriptorDigest")}
    old_refs, new_refs = map(set, (old_fields["References"], new_fields["References"]))
    removed, added = sorted(old_refs - new_refs), sorted(new_refs - old_refs)
    assert all("/probe/bin/Release/net10.0/" in ref for ref in removed + added)
    return {"stableSitesAndComponentDigestsMatch": True, "sameHostPreimage": equal,
            "exactPositiveIdsMatch": set(old["expectedFacts"]) == set(new["expectedFacts"]),
            "removedReferenceIdentities": removed, "addedReferenceIdentities": added}


def suite_counts(directory):
    suites = {}
    for path in directory.glob("*.trx"):
        root = ET.parse(path).getroot()
        counters = root.find(".//{*}Counters").attrib
        sources = {Path(element.attrib["storage"]).name.lower()
                   for element in root.findall(".//{*}UnitTest")}
        assert len(sources) == 1, (path, sources)
        name = sources.pop()
        assert name not in suites, name
        suites[name] = {k: int(counters[k]) for k in ("total", "executed", "passed", "failed", "notExecuted")}
    expected = {"andirit.tests.dll": 345, "andirit.csharp.tests.dll": 355,
                "andirit.treesitter.tests.dll": 157, "andirit.latticedb.tests.dll": 52}
    assert set(expected) == set(suites), suites
    for name, count in expected.items():
        assert suites[name] == dict(total=count, executed=count, passed=count, failed=0, notExecuted=0), suites
    return suites


def main():
    scratch = Path(sys.argv[1])
    sealed = Path("docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence")
    failure = ["M5.2: external symbol and persisted coverage refusal classification"]
    results = {}
    runs = {}
    for phase in ("b3-publish", "b3-reopen"):
        for flavor, status, ex, un, checks in (
            ("baseline", "MissingDefiningCompilation", 0, 1, failure),
            ("corrected", "UnsupportedExternalEvidence", 1, 0, []),
        ):
            key = f"{flavor}/{phase}"
            runs[key], results[key] = verify_binding(scratch / f"{key}.json", status, ex, un, checks)
        results[f"baseline-to-corrected/{phase}"] = compare_profiles(runs[f"baseline/{phase}"], runs[f"corrected/{phase}"])
        historical, _ = verify_binding(sealed / f"{phase}.json", "MissingDefiningCompilation", 0, 1, failure)
        results[f"qual01-to-corrected/{phase}"] = compare_profiles(historical, runs[f"corrected/{phase}"])
    for flavor in ("baseline", "corrected"):
        before, after = (runs[f"{flavor}/b3-{phase}"] for phase in ("publish", "reopen"))
        assert before["processId"] != after["processId"]
        assert before["database"] == after["database"]
        assert before["decodedCallerUnit"] == after["decodedCallerUnit"]
        assert compare_profiles(before, after)["exactPositiveIdsMatch"]

    native_phases = ("b3-missing", "b3-ambiguous", "b6-publish", "b7-conflict", "b7-reopen",
                     "b6-delete-a", "b6-reopen-a", "b6-delete-b", "b6-reopen-b", "d3", "d4-schedule",
                     "d5-namespace", "d5-schema", "d5-page")
    receipts = {phase: read(scratch / f"corrected/{phase}.json") for phase in native_phases}
    assert all(not entry["failures"] for entry in receipts.values())
    ambiguity = receipts["b3-ambiguous"]
    assert ambiguity["ambiguousCoverage"]["Ambiguous"] == 1
    assert ambiguity["ambiguousCoverage"]["External"] == 0

    inputs = {}
    binaries = {}
    for flavor in ("baseline", "corrected"):
        for group in ("probe", "fixture"):
            for source in (sealed / group).rglob("*"):
                if source.is_file():
                    relative = source.relative_to(sealed).as_posix()
                    original_hash = sha(source)
                    assert sha(scratch / flavor / relative) == original_hash, relative
                    inputs[relative] = original_hash
        out = scratch / flavor / "probe/bin/Release/net10.0"
        binaries[flavor] = {p.relative_to(out).as_posix(): sha(p) for p in out.rglob("*")
                            if p.is_file() and (p.name.startswith(("Andirit.", "lattice", "LatticeDb"))) and
                            p.suffix.lower() in (".dll", ".so", ".dylib")}
        assert "Andirit.CSharp.dll" in binaries[flavor]
    assert binaries["baseline"]["Andirit.CSharp.dll"] != binaries["corrected"]["Andirit.CSharp.dll"]

    v = scratch / "verification"
    pids = []
    for phase in ("publish", "verify"):
        payload = (v / f"testhost-{phase}.log").read_bytes()
        log = payload.decode("utf-16" if payload.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8-sig")
        pids.append(int(re.search(rf"s254-r1-external phase={phase} pid=(\d+)", log)[1]))
    assert pids[0] != pids[1]
    print(json.dumps({
        "b3": results, "focusedNative": {k: val["processId"] for k, val in receipts.items()},
        "separateTestHosts": dict(zip(("publish", "verify"), pids)),
        "sealedProbeAndFixtureSha256": inputs, "probeBinariesSha256": binaries,
        "regression": suite_counts(v / "trx"),
    }, indent=2))


if __name__ == "__main__":
    main()
