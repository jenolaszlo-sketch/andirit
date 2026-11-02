from pathlib import Path
import hashlib, json
root=next(p for p in Path(__file__).resolve().parents if (p/"Andirit.slnx").exists())
ev=root/"docs/evidence/adr-ci1-01-ci1a-s2-0-r1"
planning=root/"docs/planning/adr-ci1-01-ci1a-s2-0"
corpus=planning/"corpus"
oracle=planning/"oracle"
def entry(path):
    data=path.read_bytes()
    return {"Path":path.relative_to(root).as_posix(),"Sha256":hashlib.sha256(data).hexdigest(),"Bytes":len(data)}
def collect(directory, exclude=()):
    return [entry(p) for p in sorted(directory.rglob("*")) if p.is_file() and not any(p.name==x for x in exclude) and "bin" not in p.parts and "obj" not in p.parts]
manifest={"Package":"ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-R1","SourceBinding":{"StartingCommit":"82f101c24e21828a41a30d4342ba73db9376e24e","StartingTree":"75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0","StartingOwnerBaseline":"b7fea0d219409b279b78317e32bdc3c8b5ae61ee","StartingOwnerTree":"b4a764637498ed8e8bddac28fb0b0b3716b7ffee","Branch":"main","ProductionExtractionChanged":False},"HashScheme":"sha256 of exact working-tree bytes; forward-slash repo-relative paths; R1 manifest self-excluded","PlanningDocuments":[entry(f) for f in sorted(planning.rglob("*")) if f.is_file() and not ({"corpus","oracle"} & set(f.relative_to(planning).parts)) and "bin" not in f.parts and "obj" not in f.parts],"CorpusFixtures":collect(corpus),"OracleSources":collect(oracle),"EvidenceFiles":collect(ev,exclude=("r1-source-evidence-manifest.json",))}
(ev/"r1-source-evidence-manifest.json").write_bytes((json.dumps(manifest,indent=2)+"\n").encode("utf-8"))
print("manifest written", sum(len(manifest[k]) for k in ("PlanningDocuments","CorpusFixtures","OracleSources","EvidenceFiles")), "files")
