"""Read-only R1 scope/integrity audit. No expected evidence is rewritten."""
from pathlib import Path
import hashlib, json, subprocess, sys

repo = Path(sys.argv[1])
out = Path(sys.argv[2])
baseline = "38d4e513951c07578953fda7738d8741d2109004"
def git(*args): return subprocess.check_output(["git", *args], cwd=repo).decode().strip()
def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
assert git("branch", "--show-current") == "main"
assert git("rev-parse", baseline + "^{tree}") == "ffca6b84dd8c2c1d9eebcbcd0544d5615dcb7e2a"
subprocess.check_call(["git", "merge-base", "--is-ancestor", baseline, "HEAD"], cwd=repo)
protected = ["docs/reviews/CI1A-S2.2-Q1-foundation-qualification.md", "docs/reviews/CI1A-S2.2-Q1-tests",
    "docs/reviews/CI1A-S2.2-Q1-evidence", "docs/planning/adr-ci1-01-ci1a-s2-0",
    "docs/evidence/adr-ci1-01-ci1a-s2-0", "docs/evidence/adr-ci1-01-ci1a-s2-0-r1",
    "docs/evidence/adr-ci1-01-ci1a-s2-0-r2", "docs/evidence/adr-ci1-01-ci1a-s2-1-raw",
    "src/Andirit.CSharp/VerifiedCSharpSource.cs", "src/Andirit.Abstractions",
    "src/Andirit.CSharp/VerifiedCSharpAnalysis.cs"]
assert not git("diff", baseline, "--", *protected)
q1root = repo / "docs/reviews/CI1A-S2.2-Q1-evidence"
q1 = json.loads((q1root / "manifest.json").read_text())
for item in q1["files"]:
    path = q1root / item["path"]
    assert path.stat().st_size == item["bytes"] and digest(path) == item["sha256"], item["path"]
for path in protected:
    entries = git("ls-tree", "-r", baseline, "--", path).splitlines()
    assert entries, f"No protected baseline path: {path}"
    for entry in entries:
        meta, name = entry.split("\t", 1)
        assert git("hash-object", "--", name) == meta.split()[2], name
oracle = Path(sys.argv[3])
assert digest(oracle) == "97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9"
allowed_sources = {"src/Andirit.CSharp/VerifiedCSharpCompilation.cs", "src/Andirit.CSharp/MetadataReferenceValidation.cs"}
changed_sources = set(git("diff", "--name-only", baseline, "HEAD", "--", "src").splitlines())
assert changed_sources == allowed_sources, changed_sources
receipt = {"baseline": baseline, "baselineTree": git("rev-parse", baseline + "^{tree}"),
    "headAtAudit": git("rev-parse", "HEAD"), "treeAtAudit": git("rev-parse", "HEAD^{tree}"),
    "branch": "main", "protectedPathsUnchanged": protected, "q1ManifestFilesVerified": len(q1["files"]),
    "oracleOutputSha256": digest(oracle), "productionPathsChanged": sorted(changed_sources),
    "statusAtAudit": git("status", "--short"), "worktrees": git("worktree", "list", "--porcelain")}
out.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
print(json.dumps(receipt, indent=2))
