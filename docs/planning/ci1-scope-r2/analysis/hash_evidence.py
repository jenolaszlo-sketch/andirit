"""Record SHA-256 hashes of the R2 planning checker sources and evidence.

Writes evidence/evidence-hashes.json. Planning evidence only; never modifies
the hashed files.
"""
import hashlib
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
R2 = os.path.abspath(os.path.join(HERE, ".."))

FILES = [
    "analysis/before/Before.csproj",
    "analysis/before/Program.cs",
    "analysis/oracle/OracleR2.csproj",
    "analysis/oracle/Program.cs",
    "analysis/compare.py",
    "analysis/hash_evidence.py",
    "analysis/fixtures/f01-collision/README.md",
    "analysis/fixtures/f01-collision/A/Shadow.cs",
    "analysis/fixtures/f01-collision/A/Shadow.csproj",
    "analysis/fixtures/f01-collision/Z/App.cs",
    "analysis/fixtures/f01-collision/Z/App.csproj",
    "evidence/before/before-run.json",
    "evidence/before/before-run.txt",
    "evidence/after/after-run.json",
    "evidence/after/after-run.txt",
    "evidence/mutation-comparison.json",
    "evidence/git-bindings.json",
]

entries = []
for rel in FILES:
    path = os.path.join(R2, rel)
    data = open(path, "rb").read()
    entries.append({"path": rel.replace("\\", "/"), "sha256": hashlib.sha256(data).hexdigest(), "bytes": len(data)})

out = os.path.join(R2, "evidence", "evidence-hashes.json")
json.dump({"algorithm": "SHA-256", "files": entries}, open(out, "w", encoding="utf-8"), indent=2)
print("wrote", out, "with", len(entries), "entries")
