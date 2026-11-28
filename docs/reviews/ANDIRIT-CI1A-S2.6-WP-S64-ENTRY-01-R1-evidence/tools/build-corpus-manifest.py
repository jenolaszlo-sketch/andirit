"""Freeze the D3 corpus and expected-ledger inventory with exact byte hashes."""

import hashlib
import json
import sys
from pathlib import Path

REPO = Path(sys.argv[1]).resolve()
OUT = Path(sys.argv[2]).resolve()
EVIDENCE = "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence"
ROOTS = {
    "c1": "docs/planning/adr-ci1-01-ci1a-s2-0/corpus/repo",
    "c1-variants": "docs/planning/adr-ci1-01-ci1a-s2-0/corpus/variants",
    "c2": "docs/reviews/CI1A-S2.5-WP-S25-4-QUAL-01-evidence/fixture",
    "c3": f"{EVIDENCE}/corpus/c3-linked",
    "c4": f"{EVIDENCE}/corpus/c4-contributors",
}
EXPECTED = {
    "c1-graph": f"{EVIDENCE}/expected/c1-graph.json",
    "c1-source-changed-graph": f"{EVIDENCE}/expected/c1-source-changed-graph.json",
    "c1-no-solo-graph": f"{EVIDENCE}/expected/c1-no-solo-graph.json",
    "c1-occurrence-oracle": f"{EVIDENCE}/expected/c1-occurrence-oracle.json",
    "c1-variants": f"{EVIDENCE}/expected/c1-variants.json",
    "c1-symbols-core-graph": f"{EVIDENCE}/expected/c1-symbols-core-graph.json",
    "c1-symbols-leaf-graph": f"{EVIDENCE}/expected/c1-symbols-leaf-graph.json",
    "c1-reference-missing-graph": f"{EVIDENCE}/expected/c1-reference-missing-graph.json",
    "c2-graph": f"{EVIDENCE}/expected/c2-graph.json",
    "c2-continuity": f"{EVIDENCE}/expected/c2-continuity.json",
    "c2-no-caller-graph": f"{EVIDENCE}/expected/c2-no-caller-graph.json",
    "c3-graph": f"{EVIDENCE}/expected/c3-graph.json",
    "c3-no-alib-graph": f"{EVIDENCE}/expected/c3-no-alib-graph.json",
    "c4-graph": f"{EVIDENCE}/expected/c4-graph.json",
}


def entry(path):
    data = path.read_bytes().replace(b"\r\n", b"\n")
    return {
        "path": path.relative_to(REPO).as_posix(),
        "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(),
    }


def files(root):
    base = REPO / root
    return sorted(
        (entry(path) for path in base.rglob("*")
         if path.is_file() and "/bin/" not in path.as_posix()
         and "/obj/" not in path.as_posix()),
        key=lambda item: item["path"])


corpus = {name: files(root) for name, root in ROOTS.items()}
manifest = {
    "schema": 1,
    "corpus": "ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1",
    "roots": ROOTS,
    "corpusFiles": corpus,
    "expected": {name: entry(REPO / path) for name, path in EXPECTED.items()},
    "variants": {
        "c1-source-changed": {
            "replacement": "docs/planning/adr-ci1-01-ci1a-s2-0/corpus/variants/source-changed/App/Sites.cs",
            "target": "c1/App/Sites.cs",
            "expected": "c1-source-changed-graph",
            "occurrence": "S01 usage identity changes; target identity stable"
        },
        "c1-no-solo": {
            "deletion": "c1/Solo",
            "expected": "c1-no-solo-graph",
            "occurrence": "complete-empty unit removed"
        },
        "c2-no-caller": {
            "deletion": "c2/Caller",
            "expected": "c2-no-caller-graph",
            "occurrence": "caller unit retirement; dependency survives"
        },
        "c3-no-alib": {
            "deletion": "c3/src/ALib",
            "expected": "c3-no-alib-graph",
            "occurrence": "one linked-file contributor removed; BLib context survives"
        },
        "c1-symbols-core": {
            "define": "CORE_FEATURE",
            "project": "c1/Core/Core.csproj",
            "expected": "occurrence oracle variant symbols-core-changed"
        },
        "c1-symbols-leaf": {
            "define": "LEAF_TRACE",
            "project": "c1/Leaf/Leaf.csproj",
            "expected": "occurrence oracle variant symbols-leaf-changed"
        },
        "c1-reference-missing": {
            "removeReference": "c1/App/App.csproj -> c1/Core/Core.csproj",
            "expected": "occurrence oracle RM01 unresolved"
        }
    },
    "c4OccurrenceStatus": {
        "graph": "frozen in c4-graph",
        "target": "QualifierDependency.Api.Mark",
        "contributors": ["Caller", "Caller2"],
        "outstanding": "exact usage4/target occurrence ledger for the two-unit union"
    },
    "h1": {
        "provenance": "docs/reviews/CI1A-S2.5-WP-S25-1-BIND-01-evidence/fixtures/PROVENANCE.md",
        "policy": "Use manifest-bound graph-only and pre-REM DB/WAL only as disposable copies; never mutate originals; retain prior codec exclusions."
    },
    "c4SnippetPosition": "C4 is an added caller unit derived from the W/App shape; C1/C2 already provide nonempty call/reference witnesses."
}
OUT.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
print(json.dumps({"files": sum(len(value) for value in corpus.values()),
                  "expected": len(manifest["expected"])}, indent=2))
