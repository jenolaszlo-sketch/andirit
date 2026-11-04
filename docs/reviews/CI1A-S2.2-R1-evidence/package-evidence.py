"""Assemble additive evidence only; never touch original Q1 or frozen packages."""
from pathlib import Path
import hashlib, json, shutil, sys

stage, package = map(Path, sys.argv[1:3])
package.mkdir(parents=True, exist_ok=True)
files = ["authentication.json", "authentication.log", "environment-dotnet.txt", "integrity.json", "integrity.log",
    "q1-before.log", "q1-after.log", "regression-before.log", "regression-before-final.log",
    "regression-after.log", "regression-after-final.log", "platform-debug.log", "solution-after.log",
    "r2-verification.log", "oracle-r2.json", "oracle-r2.log", "mapping_docs_regression.py",
    "pre-remediation.txt", "pre-remediation-final.txt", "mapping-docs-before.log", "replay-doc-baseline.py",
    "mapping-docs-after.log", "collect-r1.py", "results.json", "results-summary.log",
    "q1-probe-matrix.md", "verify-integrity.py", "package-evidence.py", "verify-evidence.py"]
for name in files: shutil.copyfile(stage / name, package / name)
for name in ("q1-before", "q1-after", "regression-before", "regression-after", "solution-after", "inputs", "mapping-before"):
    shutil.copytree(stage / name, package / name, dirs_exist_ok=True)
for name in ("PreserveInputs.csproj", "Program.cs"):
    target = package / "inputs-tool" / name
    target.parent.mkdir(exist_ok=True)
    shutil.copyfile(stage / "inputs-tool" / name, target)
(package / ".gitattributes").write_text("* -text -whitespace\n", encoding="utf-8")
(package / ".gitignore").write_text("!*.log\n", encoding="utf-8")
items = []
for path in sorted(package.rglob("*")):
    if not path.is_file() or path.name == "manifest.json": continue
    data = path.read_bytes()
    items.append({"path": path.relative_to(package).as_posix(), "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})
(package / "manifest.json").write_text(json.dumps({"scheme": "SHA-256 exact bytes; self-excluded; report/source/docs bound separately by Git", "files": items}, indent=2) + "\n", encoding="utf-8")
print(f"Packaged {len(items)} exact-byte evidence files")
