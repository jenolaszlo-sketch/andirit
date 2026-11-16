import argparse
import hashlib
import json
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("--verify", action="store_true")
parser.add_argument("package", type=Path)
args = parser.parse_args()
package = args.package.resolve()
seal_path = package / "manifest.json"
manifest = {
    "commission": "ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02",
    "scheme": "SHA-256 over exact bytes, relative ordinal paths, manifest self-excluded",
    "startingHead": "d48feeaa316f76901b6e37264d6d115f689a7000",
    "startingTree": "8050060b64fa2444282167c2a0f60131795c0f64",
    "reviewedHead": "afa136f4033f35d9a657d8c5f1d5db92d736b679",
    "reviewedTree": "2962442c4c1528193fe79df9ea0ecc31701a463d",
    "files": [],
}
for path in sorted(package.rglob("*"), key=lambda entry: entry.relative_to(package).as_posix()):
    if path.is_file() and path != seal_path:
        assert not path.is_symlink(), path
        content = path.read_bytes()
        manifest["files"].append({
            "path": path.relative_to(package).as_posix(),
            "bytes": len(content),
            "sha256": hashlib.sha256(content).hexdigest(),
        })

if args.verify:
    assert manifest == json.loads(seal_path.read_bytes()), "Manifest inventory, identity or digest mismatch"
else:
    seal_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
print(f"{len(manifest['files'])} entries verified; seal SHA-256 {hashlib.sha256(seal_path.read_bytes()).hexdigest()}")

