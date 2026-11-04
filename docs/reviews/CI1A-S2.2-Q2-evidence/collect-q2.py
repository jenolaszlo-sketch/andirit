"""Preserve every executed case; do not treat expected historical failures as green."""
from pathlib import Path
import json, sys, xml.etree.ElementTree as ET
repo, stage=map(Path,sys.argv[1:3])
ns={"t":"http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
def results(path):
    root=ET.parse(path).getroot()
    return [dict(name=r.attrib["testName"],outcome=r.attrib["outcome"],
        message=r.findtext("t:Output/t:ErrorInfo/t:Message",default="",namespaces=ns),
        stdout=r.findtext("t:Output/t:StdOut",default="",namespaces=ns))
        for r in root.findall("t:Results/t:UnitTestResult",ns)]
runs={}
for folder in ("solution","q1","remediation","foundation","identity","q2-first","q2-final","q2-complete","q2-repeat","q2-inputs"):
    cases=[case for p in sorted((stage/folder).glob("*.trx")) for case in results(p)]
    assert cases,folder
    runs[folder]=dict(total=len(cases),passed=sum(c["outcome"]=="Passed" for c in cases),
        failed=sum(c["outcome"]=="Failed" for c in cases),skipped=sum(c["outcome"] not in ("Passed","Failed") for c in cases),cases=cases)
assert (runs["solution"]["passed"],runs["solution"]["failed"])==(692,0)
assert (runs["q1"]["passed"],runs["q1"]["failed"])==(45,3)
before=results(repo/"docs/reviews/CI1A-S2.2-R1-evidence/q1-before/q1-before.trx")
assert len(before)==48 and all(c["outcome"]=="Passed" for c in before)
assert {c["name"] for c in before}=={c["name"] for c in runs["q1"]["cases"]}
matrix=[]
for c in sorted(runs["q1"]["cases"],key=lambda c:c["name"]):
    expected=c["outcome"]=="Failed"
    if expected:
        assert "ObserveMalformedMetadataStillExposesIdentity" in c["name"]
        assert any('"'+s+'"' in c["name"] for s in ("first-type-name","later-type-name","method-signature"))
        assert "UnsupportedMetadataReference" in c["message"]
    matrix.append(dict(name=c["name"],historical="Passed",current=c["outcome"],classification="Historical observation invalidated by remediation" if expected else "Preserved observation"))
for name,n in (("remediation",40),("foundation",7),("identity",9),("q2-complete",40),("q2-repeat",40),("q2-inputs",40)):
    assert (runs[name]["passed"],runs[name]["failed"],runs[name]["skipped"])==(n,0,0)
def observations(run):
    rows=[]
    for c in runs[run]["cases"]:
        for line in c["stdout"].splitlines():
            if line.startswith("{"): rows.append(json.loads(line))
    return rows
stable={"mapping-authority","frozen-profile","frozen-raw-order","actual-production-inventory"}
a=sorted([r for r in observations("q2-complete") if r["kind"] in stable],key=lambda x:json.dumps(x,sort_keys=True))
b=sorted([r for r in observations("q2-repeat") if r["kind"] in stable],key=lambda x:json.dumps(x,sort_keys=True))
assert a==b and len(a)==5
last=[r for r in observations("q2-inputs") if r["kind"] in stable]
assert [r for r in a if r["kind"]!="actual-production-inventory"]==sorted(
    [r for r in last if r["kind"]!="actual-production-inventory"],key=lambda x:json.dumps(x,sort_keys=True))
old_inventory=next(r for r in a if r["kind"]=="actual-production-inventory")
new_inventory=next(r for r in last if r["kind"]=="actual-production-inventory")
assert old_inventory["count"]==new_inventory["count"]
inventory_delta=sorted(set(old_inventory["identities"])^set(new_inventory["identities"]))
assert len(inventory_delta)==2 and all("/CI1A-S2.2-Q2-tests/bin/Debug/net10.0/Andirit.CSharp.Tests.dll|" in x for x in inventory_delta)
inputs=[]
import hashlib
for p in sorted((stage/"inputs").glob("*.bin")):
    digest=hashlib.sha256(p.read_bytes()).hexdigest()
    assert digest==p.stem
    inputs.append(dict(path="inputs/"+p.name,sha256=digest,bytes=p.stat().st_size))
for row in observations("q2-inputs"):
    if row["kind"] in ("refused","captured"):
        assert any(x["sha256"]==row["sha256"] for x in inputs), row
summary={name:{k:v for k,v in run.items() if k!="cases"} for name,run in runs.items()}
out=dict(summary=summary,historicalMatrix=matrix,stableObservationCount=len(a),stableObservations=a,
    observations=observations("q2-inputs"),inputs=inputs,inventoryDeltaAfterReviewerRecorderBuild=inventory_delta,runs=runs)
(stage/"results.json").write_text(json.dumps(out,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
table=["| Unchanged Q1 case | Historical | Q2 execution | Classification |","| --- | --- | --- | --- |"]
for row in matrix:
    table.append("| `"+row["name"].replace("|","&#124;")+"` | Passed | "+row["current"]+" | "+row["classification"]+" |")
(stage/"q1-probe-matrix.md").write_text("\n".join(table)+"\n",encoding="utf-8")
print(json.dumps(dict(summary=summary,stableObservationCount=len(a)),indent=2))
