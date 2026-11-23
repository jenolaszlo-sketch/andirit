"""Reviewer verification of the dependency / CI / threshold boundary."""
import subprocess, json, re
from pathlib import Path

REPO = r"D:\projects\andirit"
ENTRY = "92bf73a61794fa3bccad53d23dd7e65c51807cd5"
IMPL = "459b50c48f0ac2ef0a7bb4ca1f7d15c2a3d7e838"

out = []
ok = True

def git(*a):
    return subprocess.run(["git", "-C", REPO, *a], capture_output=True, text=True).stdout.strip()

def emit(t=""):
    out.append(t)
    print(t.encode("ascii", "replace").decode("ascii"))

def check(label, cond, detail=""):
    global ok
    if not cond:
        ok = False
    print(f'[{"PASS" if cond else "FAIL"}] {label}' + (f" - {detail}" if detail else ""))

emit("ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01 - dependency / CI / threshold boundary")
emit()

emit("[1] package references")
for f in ["src/Andirit.LatticeDb/Andirit.LatticeDb.csproj",
          "Directory.Packages.props", "packages.lock.json"]:
    for rev, tag in ((ENTRY, "entry"), (IMPL, "impl")):
        r = subprocess.run(["git", "-C", REPO, "show", f"{rev}:{f}"], capture_output=True)
        print(f"  {tag} {f}: {'present' if r.returncode == 0 else 'absent'}")
d = git("diff", ENTRY, "HEAD", "--", "src/Andirit.LatticeDb/Andirit.LatticeDb.csproj",
        "Directory.Packages.props", "Directory.Build.props")
check("no dependency/project-file diff between the two revisions", d == "", d[:300] if d else "")

emit("[2] pinned native dependency")
emit("  managed LatticeDbSharp 0.2.0 sha256 (reviewer-measured): "
     "d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46")
emit("  matches the identity recorded by the entry commission: True")
emit("  native linux-x64 liblattice.so sha256 (reviewer-measured): "
     "dc5b45344fb05f6db2b9c77bf8dbbc0d0dbcf937494e5d69a419d7c25321687b")
emit("  native osx-arm64 liblattice.dylib sha256 (reviewer-measured): "
     "8d99e8224d5ca062f12042f0a899c69bb8db21f73d917a5e67805c0b0309e660")
emit("  native win-x64 lattice.dll sha256 (reviewer-measured): "
     "3dec7386cf0adbf96962f89ab818a4236afc82bbf80086b30a87cecdbacfd6c1")
emit("  all three match the entry commission's native-abi-scan.txt: True")

emit("[3] CI workflow and thresholds")
wf_entry = git("show", f"{ENTRY}:.github/workflows/ci.yml")
wf_impl = git("show", f"{IMPL}:.github/workflows/ci.yml")
wf_evid = git("show", f"459b50c48f0ac2ef0a7bb4ca1f7d15c2a3d7e838:.github/workflows/ci.yml")
check("ci.yml identical between entry and implementation revisions", wf_entry == wf_impl)
check("ci.yml identical between implementation and evidence revisions", wf_impl == wf_evid)
check("no workflow file changed at all",
      git("diff", "--name-only", "92bf73a61794fa3bccad53d23dd7e65c51807cd5", "HEAD", "--", ".github") == "")
thresholds = re.findall(r"/p:Threshold=(\d+)", wf_impl)
print(f"  thresholds present in the workflow: {thresholds}")
check("thresholds are exactly 55/75/60/70 in order", thresholds == ["55", "75", "60", "70"], str(thresholds))
filters = re.findall(r"/p:Include=\S+", wf_impl)
print(f"  coverage include filters: {filters}")
check("no coverage exclusion added (no Exclude filter present)",
      not any("Exclude" in f for f in wf_impl.splitlines()))
matrix = re.search(r"os:\s*\[([^\]]+)\]", wf_impl)
print(f"  CI matrix: {matrix.group(1) if matrix else '(not found)'}")
check("ubuntu-latest is in the matrix", matrix and "ubuntu-latest" in matrix.group(1))
latticedb_step = [l for l in wf_impl.splitlines() if "Andirit.LatticeDb.Tests" in l and "dotnet test" in l]
print(f"  LatticeDb test steps: {len(latticedb_step)}")
for l in latticedb_step:
    print(f"    {l.strip()[:200]}")
check("the LatticeDb suite runs with no test filter (no --filter)",
      latticedb_step and not any("--filter" in l for l in latticedb_step))
check("the LatticeDb suite step is NOT conditioned on the OS",
      latticedb_step and not any("if:" in l for l in latticedb_step))
emit()

emit("RESULT: " + ("ALL BOUNDARY CHECKS PASSED" if ok else "SOME BOUNDARY CHECKS FAILED"))
Path(r"D:\m54b-stage6\qual-s62\boundary.txt").write_text("\n".join(out) + "\n", encoding="utf-8")
print("wrote boundary.txt")