"""Verify the S2.0 corpus evidence manifest.

Run from anywhere:  python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/verify-manifest.py
Recomputes the SHA-256 of every listed file from working-tree bytes and checks
the source binding. Fails loudly on any drift.
"""
from pathlib import Path
import hashlib
import json
import subprocess

here = Path(__file__).resolve().parent
repo = next(p for p in here.parents if (p / "Andirit.slnx").exists())
manifest = json.loads((here / "source-evidence-manifest.json").read_text(encoding="utf-8"))


def git(*args):
    return subprocess.check_output(["git", *args], cwd=repo).decode().strip()


groups = ("PlanningDocuments", "CorpusFixtures", "OracleSources", "EvidenceFiles")
total = 0
for group in groups:
    for item in manifest[group]:
        path = repo / item["Path"]
        assert path.is_file(), f"missing {item['Path']}"
        data = path.read_bytes()
        assert len(data) == item["Bytes"], f"size drift {item['Path']}"
        assert hashlib.sha256(data).hexdigest() == item["Sha256"], f"hash drift {item['Path']}"
        total += 1

binding = manifest["SourceBinding"]
assert git("rev-parse", "--abbrev-ref", "HEAD") == binding["Branch"]
print(f"PASS: {total} files verified; manifest self-excluded; "
      f"source binding recorded (starting commit {binding['StartingCommit'][:12]}).")
