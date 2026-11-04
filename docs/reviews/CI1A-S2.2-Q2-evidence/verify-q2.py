"""Read-only verification of the existing Q2 package, without rewriting expectations."""
from pathlib import Path
import hashlib,json,sys
repo=Path(sys.argv[1])
root=repo/"docs/reviews/CI1A-S2.2-Q2-evidence"
m=json.loads((root/"manifest.json").read_text(encoding="utf-8"))
def verify(items,base):
    seen=set()
    for item in items:
        path=Path(item["path"])
        assert not path.is_absolute() and ".." not in path.parts and path.as_posix() not in seen
        seen.add(path.as_posix())
        raw=(base/path).read_bytes()
        assert len(raw)==item["bytes"] and hashlib.sha256(raw).hexdigest()==item["sha256"],path
    return seen
seen=verify(m["files"],root)
assert seen=={p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file() and p.name!="manifest.json"}
verify(m["reviewerSources"],repo)
print(f"PASS: {len(seen)} Q2 evidence files and {len(m['reviewerSources'])} reviewer source/report files verified")
