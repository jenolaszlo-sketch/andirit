"""Independent Q2 audit. Reads authenticated expectations; never regenerates them."""
from pathlib import Path
import hashlib, json, subprocess, sys

repo, stage = map(Path, sys.argv[1:3])
def git(*args): return subprocess.check_output(["git", *args], cwd=repo).decode().strip()
def sha(data): return hashlib.sha256(data).hexdigest()
pins = {
    "38d4e513951c07578953fda7738d8741d2109004": "ffca6b84dd8c2c1d9eebcbcd0544d5615dcb7e2a",
    "d5644d814b83170f553584529c05f5f8c289ba25": "bcbf65117c9a1f892b987e4e83ed25eab99ee705",
    "b4eff01bb736f1725543fc8ba928ff313d65c4d6": "ca448ef7da813a45130ed3a138850a27457de735",
}
q1, implementation, head = pins
assert git("rev-parse", "HEAD") == head
assert git("branch", "--show-current") == "main"
assert not git("diff", "HEAD")
for commit, tree in pins.items():
    assert git("rev-parse", commit+"^{tree}") == tree
    subprocess.check_call(["git", "merge-base", "--is-ancestor", commit, head], cwd=repo)
assert git("rev-parse", implementation+"^") == q1
assert git("rev-parse", head+"^") == implementation
production = {"src/Andirit.CSharp/MetadataReferenceValidation.cs", "src/Andirit.CSharp/VerifiedCSharpCompilation.cs"}
assert set(git("diff", "--name-only", q1, head, "--", "src").splitlines()) == production
assert git("diff", "--name-status", q1, implementation).splitlines() == [
    "A\tsrc/Andirit.CSharp/MetadataReferenceValidation.cs",
    "M\tsrc/Andirit.CSharp/VerifiedCSharpCompilation.cs",
    "A\ttests/Andirit.CSharp.Tests/S2_2MetadataRemediationTests.cs"]
protected = ["docs/decisions", "docs/planning/adr-ci1-01-ci1a-s2-0", "docs/evidence",
    "docs/reviews/CI1A-S2.2-Q1-tests", "docs/reviews/CI1A-S2.2-Q1-evidence",
    "docs/reviews/CI1A-S2.2-Q1-foundation-qualification.md",
    "src/Andirit.Abstractions", "src/Andirit/CodeIndexIdentity.cs",
    "src/Andirit.CSharp/VerifiedCSharpSource.cs", "src/Andirit.CSharp/VerifiedCSharpAnalysis.cs"]
assert not git("diff", q1, head, "--", *protected)
# All pre-existing producer tests are unchanged; the only test delta is additive R1.
assert git("diff", "--name-status", q1, head, "--", "tests") == "A\ttests/Andirit.CSharp.Tests/S2_2MetadataRemediationTests.cs"
for line in git("ls-tree", "-r", head, "--", "src", "tests", "docs/decisions", "docs/evidence", "docs/planning",
                "docs/reviews/CI1A-S2.2-Q1-tests", "docs/reviews/CI1A-S2.2-Q1-evidence",
                "docs/reviews/CI1A-S2.2-R1-evidence", "docs/reviews/CI1A-S2.2-R1-foundation-remediation.md").splitlines():
    meta, path = line.split("\t",1)
    assert git("hash-object", "--", path) == meta.split()[2], path

manifest = repo / "docs/evidence/adr-ci1-01-ci1a-s2-0-r2/r2-source-evidence-manifest.json"
anchor="bce7de2a98602e60c7631757b30ebd05b7b388d65fed69e50a43ae0cec848b13"
assert sha(manifest.read_bytes()) == anchor
data=json.loads(manifest.read_text())
groups={}
for group in ("PlanningDocuments","CorpusFixtures","OracleSources","EvidenceFiles"):
    groups[group]=len(data[group])
    for item in data[group]:
        raw=(repo/item["Path"]).read_bytes()
        assert len(raw)==item["Bytes"] and sha(raw)==item["Sha256"], item["Path"]
counts={}
for label in ("Q1","R1"):
    root=repo/f"docs/reviews/CI1A-S2.2-{label}-evidence"
    m=json.loads((root/"manifest.json").read_text())
    for item in m["files"]:
        raw=(root/item["path"]).read_bytes()
        assert len(raw)==item["bytes"] and sha(raw)==item["sha256"], item["path"]
    counts[label]=len(m["files"])
oracle_sha=sha((stage/"oracle-r2.json").read_bytes())
assert oracle_sha=="97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9"
inherited=json.loads((stage/"inherited-authentication.json").read_text())
assert inherited["q3Files"]==338
receipt=dict(pins=pins, head=head, branch="main", statusAtAudit=git("status","--short"),
    worktrees=git("worktree","list","--porcelain"), stashes=git("stash","list"), originMain=git("rev-parse","origin/main"),
    implementationDiff=git("diff","--name-status",q1,implementation), evidenceDiff=git("diff","--name-status",implementation,head),
    protectedPathsUnchanged=protected, r2Anchor=anchor, r2Groups=groups, reviewerManifests=counts,
    oracleOutputSha256=oracle_sha, inheritedAuthentication="inherited-authentication.json",
    productionPathsChanged=sorted(production))
(stage/"authentication.json").write_text(json.dumps(receipt,indent=2)+"\n",encoding="utf-8")
print(json.dumps(receipt,indent=2))
