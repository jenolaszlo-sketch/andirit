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
    "commission": "ANDIRIT-CI1A-S2.5-WP-S25-4-R2",
    "scheme": "SHA-256 over exact bytes, relative ordinal paths, manifest self-excluded",
    "startingHead": "6d4d45f7aedc85f4b52310cf9a3fa6082f885296",
    "startingTree": "d60f3527090aa0a11a68abb10ce05cd3c6e87ee9",
    "reviewedHead": "25bc6e3b4305c7ce088deb36cfb000138dc76648",
    "reviewedTree": "2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3",
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


