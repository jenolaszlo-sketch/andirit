"""Replay the final documentation guard against exact Q1 Git blobs; no worktree."""
from pathlib import Path
import subprocess, sys

repo, stage = map(Path, sys.argv[1:3])
baseline = "38d4e513951c07578953fda7738d8741d2109004"
root = stage / "mapping-before"
for name in ("mapping-contract.md", "g-confirm-package.md", "g-mint-package.md"):
    rel = "docs/planning/adr-ci1-01-ci1a-s2-2/" + name
    target = root / rel
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(subprocess.check_output(["git", "show", baseline + ":" + rel], cwd=repo))
result = subprocess.run([sys.executable, "-X", "utf8", str(stage / "mapping_docs_regression.py"), str(root)], capture_output=True)
(stage / "mapping-docs-before.log").write_bytes(result.stdout + result.stderr)
print(result.stdout.decode())
assert result.returncode == 1 and b"RESULT: 1/12 passed" in result.stdout
