from pathlib import Path
import hashlib, json
root=next(p for p in Path(__file__).resolve().parents if (p/"Andirit.slnx").exists())
ev=root/"docs/evidence/adr-ci1-01-ci1a-s2-0-r2"
planning=root/"docs/planning/adr-ci1-01-ci1a-s2-0"
corpus=planning/"corpus"
oracle=planning/"oracle"
def entry(path):
    data=path.read_bytes()
    return {"Path":path.relative_to(root).as_posix(),"Sha256":hashlib.sha256(data).hexdigest(),"Bytes":len(data)}
def collect(directory, exclude=()):
    return [entry(p) for p in sorted(directory.rglob("*")) if p.is_file() and not any(p.name==x for x in exclude) and "bin" not in p.parts and "obj" not in p.parts]
manifest={"Package":"ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-R2","SourceBinding":{"StartingCommit":"9fc2115622c93fa54f5f7882b117185a017703bb","StartingTree":"547af9c3c7d37132b93944cf8b3dbf284872450f","PriorPinCommit":"82f101c24e21828a41a30d4342ba73db9376e24e","PriorPinTree":"75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0","Branch":"main","ProductionExtractionChanged":False},"HashScheme":"sha256 of exact working-tree bytes; forward-slash repo-relative paths; R2 manifest self-excluded","PlanningDocuments":[entry(f) for f in sorted(planning.rglob("*")) if f.is_file() and not ({"corpus","oracle"} & set(f.relative_to(planning).parts)) and "bin" not in f.parts and "obj" not in f.parts],"CorpusFixtures":collect(corpus),"OracleSources":collect(oracle),"EvidenceFiles":collect(ev,exclude=("r2-source-evidence-manifest.json",))}
(ev/"r2-source-evidence-manifest.json").write_bytes((json.dumps(manifest,indent=2)+"\n").encode("utf-8"))
print("manifest written", sum(len(manifest[k]) for k in ("PlanningDocuments","CorpusFixtures","OracleSources","EvidenceFiles")), "files")
