"""Package additive Q2 outputs only. Never writes an older evidence package."""
from pathlib import Path
import hashlib, json, shutil, sys
stage,repo=map(Path,sys.argv[1:3])
target=repo/"docs/reviews/CI1A-S2.2-Q2-evidence"
target.mkdir(parents=True,exist_ok=True)
excluded={"FoundationRequalificationProbes.cs","CI1A-S2.2-Q2-foundation-requalification.md"}
for p in stage.iterdir():
    if p.name in excluded: continue
    if p.is_dir():
        assert p.name in {"solution","q1","remediation","foundation","identity","q2-first","q2-final","q2-complete","q2-repeat","q2-inputs","inputs"},p
        shutil.copytree(p,target/p.name,dirs_exist_ok=True)
    else: shutil.copy2(p,target/p.name)
(target/".gitattributes").write_text("# Preserve raw evidence bytes and Windows line endings.\n* -text whitespace=cr-at-eol\n# Raw tool/test outputs retain their original whitespace.\n*.log -whitespace\n*.trx -whitespace\n*.bin binary\n",encoding="utf-8")
(target/".gitignore").write_text("!**\n",encoding="utf-8")
def item(p,root):
    raw=p.read_bytes()
    return dict(path=p.relative_to(root).as_posix(),bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest())
sources=[repo/"docs/reviews/CI1A-S2.2-Q2-foundation-requalification.md",
    repo/"docs/reviews/CI1A-S2.2-Q2-tests/ReviewerTests.csproj",
    repo/"docs/reviews/CI1A-S2.2-Q2-tests/FoundationRequalificationProbes.cs"]
manifest=dict(candidate="b4eff01bb736f1725543fc8ba928ff313d65c4d6",selfExcluded=True,
    reviewerSources=[item(p,repo) for p in sources],
    files=[item(p,target) for p in sorted(target.rglob("*")) if p.is_file() and p.name!="manifest.json"])
(target/"manifest.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf-8")
print(f"Packaged {len(manifest['files'])} Q2 evidence files and bound {len(sources)} reviewer source/report files")
