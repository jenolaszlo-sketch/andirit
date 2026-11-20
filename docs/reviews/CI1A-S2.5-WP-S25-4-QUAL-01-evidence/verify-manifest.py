import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent
manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
expected = {entry["path"]: entry for entry in manifest["files"]}
observed = {path.relative_to(root).as_posix(): path for path in root.rglob("*")
            if path.is_file() and path != root / "manifest.json"}
assert set(expected) == set(observed), ("inventory", set(expected) ^ set(observed))
for name, path in sorted(observed.items()):
    payload = path.read_bytes()
    assert len(payload) == expected[name]["bytes"], ("bytes", name)
    assert hashlib.sha256(payload).hexdigest() == expected[name]["sha256"], ("sha256", name)
print(f"{len(observed)} entries verified")
