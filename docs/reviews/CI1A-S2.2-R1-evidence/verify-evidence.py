"""Verify the existing R1 manifest without regenerating expected values."""
from pathlib import Path
import hashlib, json, sys

root = Path(sys.argv[1]) if len(sys.argv) > 1 else Path(__file__).resolve().parent
manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
seen = set()
for item in manifest["files"]:
    rel = Path(item["path"])
    assert not rel.is_absolute() and ".." not in rel.parts and rel.as_posix() not in seen
    seen.add(rel.as_posix())
    data = (root / rel).read_bytes()
    assert len(data) == item["bytes"] and hashlib.sha256(data).hexdigest() == item["sha256"], str(rel)
assert seen == {p.relative_to(root).as_posix() for p in root.rglob("*") if p.is_file() and p.name != "manifest.json"}
print(f"PASS: {len(seen)} R1 evidence files verified")
