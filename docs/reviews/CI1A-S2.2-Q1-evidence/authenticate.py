"""Reviewer-only identity/evidence audit; never writes to an accepted package."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys

repo = Path(sys.argv[1])
out = Path(sys.argv[2])
workspace = Path(r"C:\Users\Laszlos\source\repos\Solo")

def digest(data):
    return hashlib.sha256(data).hexdigest()

def git(*args):
    return subprocess.check_output(["git", *args], cwd=repo).decode().strip()

pins = {
    "febc28d92548fd7323294077c37444048741a8c8": "e1fa31ec23ef213629dd613e92073641bf2e6861",
    "0c989e2990717b3dc0df629f449885d1d6408622": "ef7457008bdb74e1b2de3d7e96d3dc0ffef9dad8",
    "5cae7f09992f4be86964e4d7af1c893f07d67b47": "2a7468991f8a3ded8b2372cfbef7bc5461a3b2f3",
    "7f8d035d7fa6bd5da49f71ee86ff54d7350318c5": "29150eb6ef2b0fe65942f3c8930037f8c0120174",
    "b7fea0d219409b279b78317e32bdc3c8b5ae61ee": "b4a764637498ed8e8bddac28fb0b0b3716b7ffee",
    "c02d5eae157653ba7f123b98aceccb01a0b542a0": "a53a076dbb482062973f70fd11bf4261cc5368f3",
    "9fc2115622c93fa54f5f7882b117185a017703bb": "547af9c3c7d37132b93944cf8b3dbf284872450f",
    "82f101c24e21828a41a30d4342ba73db9376e24e": "75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0",
    "a1cf47083747bae90293ec074b35fe8078bd0d6e": "ebba0daf2977315a5c09551df0de698afeb6696f",
}
for commit, tree in pins.items():
    assert git("rev-parse", commit + "^{tree}") == tree, commit
    subprocess.check_call(["git", "merge-base", "--is-ancestor", commit, "HEAD"], cwd=repo)
assert git("rev-parse", "--abbrev-ref", "HEAD") == "main"
assert git("rev-parse", "5cae7f0^") == "0c989e2990717b3dc0df629f449885d1d6408622"
assert git("rev-parse", "0c989e2^") == "febc28d92548fd7323294077c37444048741a8c8"
protected = ["docs/planning/adr-ci1-01-ci1a-s2-0", "docs/evidence/adr-ci1-01-ci1a-s2-0",
             "docs/evidence/adr-ci1-01-ci1a-s2-0-r1", "docs/evidence/adr-ci1-01-ci1a-s2-0-r2",
             "docs/evidence/adr-ci1-01-ci1a-s2-1-raw", "docs/reviews"]
assert not git("diff", "febc28d", "5cae7f0", "--", *protected)
allowed = {"src/Andirit.CSharp/VerifiedCSharpSource.cs", "src/Andirit.CSharp/VerifiedCSharpCompilation.cs"}
production_diff = git("diff", "--name-only", "febc28d", "5cae7f0", "--", "src").splitlines()
assert set(production_diff) == allowed

r2 = repo / "docs/evidence/adr-ci1-01-ci1a-s2-0-r2/r2-source-evidence-manifest.json"
assert digest(r2.read_bytes()) == "bce7de2a98602e60c7631757b30ebd05b7b388d65fed69e50a43ae0cec848b13"
r2_manifest = json.loads(r2.read_text())
counts = {}
for group in ("PlanningDocuments", "CorpusFixtures", "OracleSources", "EvidenceFiles"):
    counts[group] = len(r2_manifest[group])
    for item in r2_manifest[group]:
        data = (repo / item["Path"]).read_bytes()
        assert len(data) == item["Bytes"] and digest(data) == item["Sha256"], item["Path"]

q3root = workspace / "adr-ci1-01-ci1a-s2-0-corpus-01-q3"
q3path = q3root / "reviewer-manifest.json"
assert digest(q3path.read_bytes()) == "9efc098528340178d6a239a9c0137f8f40b8c62ace2b1d5546561c1b56ccf52a"
q3 = json.loads(q3path.read_text())
for item in q3["Files"]:
    data = (q3root / item["Path"]).read_bytes()
    assert len(data) == item["Bytes"] and digest(data) == item["Sha256"], item["Path"]

s21path = repo / "docs/evidence/adr-ci1-01-ci1a-s2-1-raw/source-evidence-manifest.json"
s21 = json.loads(s21path.read_text())
for group in ("SourceFiles", "EvidenceFiles"):
    for item in s21[group]:
        blob = subprocess.check_output(["git", "cat-file", "blob", item["GitBlob"]], cwd=repo)
        assert digest(blob) == item["Sha256CanonicalGitBytes"], item["Path"]
        if group == "SourceFiles":
            assert git("rev-parse", s21["Implementation"]["Commit"] + ":" + item["Path"]) == item["GitBlob"]
        else:
            assert git("hash-object", item["Path"]) == item["GitBlob"], item["Path"]

s21root = workspace / "adr-ci1-01-ci1a-impl-01-s2-1-q1"
historical = {}
for name in ("REVIEWER-MANIFEST.sha256", "REVIEWER-MANIFEST-Q1.sha256"):
    matched, preserved, drift = [], [], []
    for line in (s21root / "evidence" / name).read_text().splitlines():
        if not line or line.startswith("#"):
            continue
        expected, rel = line.split("  ", 1)
        current = s21root / rel
        old = s21root / "evidence/prior-review-preserved" / Path(rel).name
        if current.is_file() and digest(current.read_bytes()) == expected:
            matched.append(rel)
        elif old.is_file() and digest(old.read_bytes()) == expected:
            preserved.append(rel)
        else:
            drift.append(rel)
    historical[name] = dict(in_place=len(matched), preserved=len(preserved), drift=drift)
assert len(historical["REVIEWER-MANIFEST.sha256"]["drift"]) == 14
assert not historical["REVIEWER-MANIFEST-Q1.sha256"]["drift"]

result = dict(pins=pins, head=git("rev-parse", "HEAD"), branch="main",
              worktrees=git("worktree", "list", "--porcelain"), status=git("status", "--short"),
              originMain=git("rev-parse", "origin/main"),
              boundedDiff=git("diff", "--name-status", "febc28d", "5cae7f0"),
              productionDiff=production_diff, frozenR2=counts, q3Files=len(q3["Files"]),
              s21ProducerGroups={g: len(s21[g]) for g in ("SourceFiles", "EvidenceFiles")},
              s21Historical=historical)
out.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, indent=2))
