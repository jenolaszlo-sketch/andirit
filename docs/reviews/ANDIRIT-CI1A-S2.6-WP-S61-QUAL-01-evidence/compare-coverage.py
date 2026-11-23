"""Compare the producer's recorded coverage figures against the committed artifacts."""
import subprocess, xml.etree.ElementTree as ET

REPO = r"D:\projects\andirit"
EVID = "3eb1339304ff843336f6165575904350df56d041"

def blob(path):
    r = subprocess.run(["git", "-C", REPO, "cat-file", "blob", f"{EVID}:{path}"], capture_output=True)
    return r.stdout if r.returncode == 0 else None

PKG = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence"

print("== producer's committed LOCAL (Windows) coverage artifacts ==")
for name in ("coverage", "coverage-csharp", "coverage-latticedb", "coverage-treesitter"):
    data = blob(f"{PKG}/coverage/{name}-coverage.cobertura.xml")
    if data is None:
        print(f"  {name}: ABSENT")
        continue
    root = ET.fromstring(data)
    print(f"  {name:20s} line-rate={float(root.get('line-rate'))*100:6.2f}  branch-rate={float(root.get('branch-rate'))*100:6.2f}")

print()
print("== reviewer's freshly derived HOSTED (ubuntu) coverage artifacts ==")
from pathlib import Path
art = Path(r"D:\m54b-stage6\qual-s61\hosted\coverage")
for name in ("coverage", "coverage-csharp", "coverage-latticedb", "coverage-treesitter"):
    p = art / name / "coverage.cobertura.xml"
    if not p.exists():
        print(f"  {name}: ABSENT")
        continue
    root = ET.parse(p).getroot()
    print(f"  {name:20s} line-rate={float(root.get('line-rate'))*100:6.2f}  branch-rate={float(root.get('branch-rate'))*100:6.2f}")

print()
print("== producer's committed hosted coverage artifacts (in evidence/hosted) ==")
for name in ("coverage", "coverage-csharp", "coverage-latticedb", "coverage-treesitter"):
    data = blob(f"{PKG}/hosted/coverage-results-ubuntu-latest/{name}-coverage.cobertura.xml")
    if data is None:
        print(f"  {name}: ABSENT from the committed package")
        continue
    root = ET.fromstring(data)
    print(f"  {name:20s} line-rate={float(root.get('line-rate'))*100:6.2f}  branch-rate={float(root.get('branch-rate'))*100:6.2f}")
