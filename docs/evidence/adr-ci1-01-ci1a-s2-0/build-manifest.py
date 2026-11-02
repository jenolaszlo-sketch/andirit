"""Rebuild the self-excluded SHA-256 manifest for the S2.0 corpus package.

Run from anywhere:  python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/build-manifest.py
The manifest hashes working-tree bytes of every corpus, oracle and evidence
file, excluding build output (bin/obj) and the manifest itself. It is the
evidence-integrity authority for the frozen S2.0 package.
"""
from pathlib import Path
import hashlib
import json
import subprocess

here = Path(__file__).resolve().parent
repo = next(p for p in here.parents if (p / "Andirit.slnx").exists())
areas = {
    "CorpusFixtures": repo / "docs/planning/adr-ci1-01-ci1a-s2-0/corpus",
    "OracleSources": repo / "docs/planning/adr-ci1-01-ci1a-s2-0/oracle",
    "PlanningDocuments": repo / "docs/planning/adr-ci1-01-ci1a-s2-0",
    "EvidenceFiles": here,
}
skip_dirs = {"bin", "obj", "__pycache__"}
excluded = {"source-evidence-manifest.json"}


def files_under(base: Path):
    for path in sorted(base.rglob("*")):
        if not path.is_file():
            continue
        if any(part in skip_dirs for part in path.parts):
            continue
        if path.name in excluded:
            continue
        yield path


def git(*args):
    return subprocess.check_output(["git", *args], cwd=repo).decode().strip()


def entry(path: Path):
    data = path.read_bytes()
    return {
        "Path": path.relative_to(repo).as_posix(),
        "Sha256": hashlib.sha256(data).hexdigest(),
        "Bytes": len(data),
    }


def collect(base: Path, already: set):
    result = []
    for path in files_under(base):
        rel = path.relative_to(repo).as_posix()
        if rel in already:
            continue
        already.add(rel)
        result.append(entry(path))
    return result


already: set = set()
manifest = {
    "Package": "ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01",
    "SourceBinding": {
        # Canonical commission baseline (fixed; not the evolving HEAD).
        "StartingCommit": "b7fea0d219409b279b78317e32bdc3c8b5ae61ee",
        "StartingTree": "b4a764637498ed8e8bddac28fb0b0b3716b7ffee",
        # Implementation commit present when this manifest was built.
        "EvidenceBaseCommit": git("rev-parse", "HEAD"),
        "EvidenceBaseTree": git("rev-parse", "HEAD^{tree}"),
        "Branch": git("rev-parse", "--abbrev-ref", "HEAD"),
        "ProductionExtractionChanged": False,
    },
    "HashScheme": "sha256 of exact working-tree bytes; forward-slash repo-relative paths; manifest self-excluded",
}
# Corpus and oracle subsets are collected first so the planning-area glob only
# contributes the remaining documents.
manifest["CorpusFixtures"] = collect(areas["CorpusFixtures"], already)
manifest["OracleSources"] = collect(areas["OracleSources"], already)
manifest["PlanningDocuments"] = collect(areas["PlanningDocuments"], already)
manifest["EvidenceFiles"] = collect(areas["EvidenceFiles"], already)

(here / "source-evidence-manifest.json").write_text(
    json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
count = sum(len(manifest[k]) for k in
            ("PlanningDocuments", "CorpusFixtures", "OracleSources", "EvidenceFiles"))
print(f"manifest written: {count} files (manifest self-excluded)")
