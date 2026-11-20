import hashlib
import json
import pathlib
import re
import sys
import xml.etree.ElementTree as ET
from collections import Counter

root = pathlib.Path(__file__).resolve().parent
fixture = pathlib.Path(sys.argv[1])
qualified = pathlib.Path(sys.argv[2])

def digest(fields):
    hashed = hashlib.sha256()
    for field in fields:
        data = str(field).encode("utf-8")
        hashed.update(len(data).to_bytes(4, "big"))
        hashed.update(data)
    return hashed.hexdigest()

def raw_hash(data):
    return "sha256:" + hashlib.sha256(data).hexdigest()

def load(path):
    return json.loads(path.read_bytes())

def positive(path, revision, mixed=False):
    data = load(path)
    assert not data["failures"], path
    inputs = data["sourceInputs"]
    context = data["expectedDefiningCompctx1Fields"]
    repo = "review:s254:binding"
    plugin = "penghou.hetu.csharp"
    doc = "M:QualifierDependency.Api.Mark"
    dependency = "Dependency/Api.cs"
    source = context["Sources"][0]
    assert source["ContentHash"] == raw_hash(inputs[dependency].encode()) and source["Path"] == dependency
    owner = "csharp:project:" + hashlib.sha256(b"Caller/Caller.csproj").hexdigest()
    defining_unit = "csharp:project:" + hashlib.sha256(b"Dependency/Dependency.csproj").hexdigest()
    assert context["ProjectUnitId"] == defining_unit
    sources = ["|".join(item[key] for key in ("Path", "ContentHash", "LanguageVersion", "PreprocessorSymbols"))
               for item in context["Sources"]]
    refs = context["References"]
    assert refs == sorted(refs)
    ctx = "compctx1:sha256:" + digest([
        "compctx1-context", "compctx1", repo, defining_unit, context["AssemblyName"],
        context["MetadataIdentity"], context["ModuleName"], context["OptionsDigest"], *sources, *refs,
    ])
    assert ctx == context["DescriptorDigest"] == data["observedDefiningCompctx1"]["DescriptorDigest"]
    components = [digest(["asmctx3-identifier", "Mark"]), digest(["asmctx3-declarations", doc]),
                  digest(["asmctx3-target", "method|0|"])]
    target = "asmctx3:sha256:" + digest(["asmctx3-context", "compctx1", ctx, context["MetadataIdentity"],
                                       context["ModuleName"], *components])
    assert target == data["expectedTarget"]
    sites = []
    declaration_lines = inputs[dependency].splitlines()
    declaration_row, declaration_text = next((n, line) for n, line in enumerate(declaration_lines, 1)
                                             if "public static int Mark() => 7;" in line)
    declaration_col = declaration_text.index("public static int Mark()") + 1
    declaration_end = declaration_text.index(";", declaration_col - 1) + 2
    for source_path in sorted(key for key in inputs if re.fullmatch(r"Caller/Fixture\d+\.cs", key)):
        raw = inputs[source_path].encode("utf-8")
        assert raw == (fixture / source_path).read_bytes()
        content = raw_hash(raw)
        for lineno, line in enumerate(inputs[source_path].splitlines(), 1):
            for match in re.finditer(r"\bMark(?=\(\);)", line):
                col = match.start() + 1
                end = match.end() + 1
                occurrence = digest(["usage4", repo, plugin, "usage4-b2c-exp-r2-20261101", owner,
                                     source_path, content, lineno, col, lineno, end, doc, target,
                                     "method-invocation"])
                sites.append(dict(id=occurrence, path=source_path, sourceHash=content, line=lineno,
                                  start=col, end=end, owner=owner, target=target))
    assert len(sites) == 50
    expected = {site["id"] for site in sites}
    result = data["result"]
    stored = data["decodedCallerUnit"]
    assert expected == {fact["Id"]["Value"] for fact in result["Occurrences"]}
    assert expected == {fact["Id"]["Value"] for fact in stored["Facts"]}
    for fact in stored["Facts"]:
        assert fact["SourceUnitId"] == owner and fact["TargetDocumentId"] == doc and fact["TargetId"]["Value"] == target
        decl = fact["DeclarationSpan"]
        if not mixed:
            assert (decl["Path"], decl["StartLine"], decl["StartColumn"], decl["EndLine"], decl["EndColumn"]) == (
                dependency, declaration_row, declaration_col, declaration_row, declaration_end)
    for hit in result["Occurrences"]:
        assert hit["SourceUnitId"] == owner and hit["TargetId"]["Value"] == target and hit["Category"] == 0
    ext = int(revision != "baseline")
    unresolved = (1 - ext) + mixed
    for coverage in (data["initialCoverage"], result["Coverage"], stored["Coverage"]):
        assert (coverage["External"], coverage["Unresolved"], coverage["Ambiguous"],
                coverage["InvocationFacts"]) == (ext, unresolved, int(mixed), 50)
    status = "UnsupportedExternalEvidence" if ext else "MissingDefiningCompilation"
    assert data["externalObserved"]["resolution"] == status
    assert data["bindingBoundary"]["withoutCaller"] == data["bindingBoundary"]["unverifiedCaller"] == "MissingDefiningCompilation"
    assert data["bindingBoundary"]["exactPeReference"]
    return dict(context=ctx, target=target, components=components, references=refs, sites=sites,
                processId=data["processId"], status=status, external=ext, unresolved=unresolved,
                ambiguous=int(mixed), externalOccurrences=0)

profiles = {}
for revision in ("r1", "r2"):
    for phase in ("publish", "reopen"):
        receipt_root = qualified if revision == "r1" else root
        profiles[f"{revision}/{phase}"] = positive(receipt_root / f"probes/corrected/b3-{phase}.json", revision)
for phase in ("publish", "reopen"):
    profiles[f"mixed/{phase}"] = positive(root / f"probes/corrected/b3-mixed-{phase}.json", "corrected", True)
for revision in ("r1", "r2", "mixed"):
    first, second = (profiles[f"{revision}/{phase}"] for phase in ("publish", "reopen"))
    assert first["processId"] != second["processId"] and first["sites"] == second["sites"]
baseline, corrected = (profiles[f"{revision}/publish"] for revision in ("r1", "r2"))
stable = lambda profile: [{k: v for k, v in site.items() if k not in {"id", "target"}} for site in profile["sites"]]
assert stable(baseline) == stable(corrected) and baseline["components"] == corrected["components"]
refdiff = {rev: sorted(set(a["references"]) - set(b["references"])) for rev, a, b in
           (("r1Only", baseline, corrected), ("r2Only", corrected, baseline))}
assert refdiff["r1Only"] and refdiff["r2Only"]
report = {"profiles": profiles, "stableSitePrimitivesMatch": True, "componentDigestsMatch": True,
          "referenceDifferences": refdiff}
(root / "b3-identity-verification.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")

# TRX uses unmangled method names in definitions and argument-qualified invocations in results.
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
discovery = Counter(line.strip() for line in (root / "gates/discovery.log").read_text(encoding="utf-8-sig").splitlines()
                    if line.startswith("    Andirit."))
executed = Counter()
suites = {}
for path in sorted((root / "gates/trx").glob("*.trx")):
    doc = ET.parse(path)
    counters = doc.find("t:ResultSummary/t:Counters", ns).attrib
    assert int(counters["failed"]) == int(counters["notExecuted"]) == int(counters["error"]) == 0
    definitions = {unit.attrib["id"]: unit.find("t:TestMethod", ns).attrib
                   for unit in doc.findall("t:TestDefinitions/t:UnitTest", ns)}
    for result in doc.findall("t:Results/t:UnitTestResult", ns):
        method = definitions[result.attrib["testId"]]
        assert result.attrib["outcome"] == "Passed"
        assert result.attrib["testName"].startswith(method["className"] + "." + method["name"])
        display = re.sub(r"\\u(d[89ab][0-9a-f]{2})\\u(d[c-f][0-9a-f]{2})",
                         lambda pair: chr(0x10000 + (int(pair[1], 16) - 0xD800) * 1024
                                          + int(pair[2], 16) - 0xDC00), result.attrib["testName"], flags=re.I)
        executed[display] += 1
    assembly = pathlib.PureWindowsPath(next(iter(definitions.values()))["codeBase"]).name
    suites[assembly] = int(counters["passed"])
assert len(suites) == 4 and sum(suites.values()) == sum(discovery.values()) == sum(executed.values()) == 917
prior = Counter(line.strip() for line in (qualified / "gates/discovery.log").read_text(encoding="utf-8-sig").splitlines()
                if line.startswith("    Andirit."))
assert sum(prior.values()) == 909 and not (prior - discovery)
new_tests = list((discovery - prior).elements())
assert len(new_tests) == 8 and all("UnsupportedSites_CountOnce" in case or "MissingDeclaredUnit" in case
                                  for case in new_tests)
assert discovery == executed, {"onlyDiscovered": discovery - executed, "onlyExecuted": executed - discovery}
oracle = (root / "gates/oracle-r2.json").read_bytes()
assert (len(oracle), hashlib.sha256(oracle).hexdigest()) == (
    146851, "97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9")
(root / "test-inventory.json").write_text(json.dumps(dict(suites=suites, passed=917, failed=0, skipped=0,
    discoveryExactlyMatchesTrx=True, newCases=new_tests, retainedCases=909, oracleCases=185, oracleBytes=len(oracle)), indent=2) + "\n", encoding="utf-8")
print("50 exact IDs per profile/phase, 917 discovered/executed tests, and frozen oracle bytes verified")



