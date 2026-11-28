"""Validate the frozen D3 corpus and oracle ledgers against their controls."""

import json
import sys
from pathlib import Path

REPO = Path(sys.argv[1]).resolve()
OUT = Path(sys.argv[2]).resolve()
E = REPO / "docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01-R1-evidence/expected"


def load(name):
    return json.loads((E / name).read_text(encoding="utf-8"))


def ids(graph):
    return {node["Id"] for node in graph["nodes"]}, {edge["Id"] for edge in graph["edges"]}


result = {"checks": [], "pass": True}


def check(name, condition, detail):
    result["checks"].append({"name": name, "pass": bool(condition), "detail": detail})
    if not condition:
        result["pass"] = False


c1 = load("c1-graph.json")
c1_variant = load("c1-source-changed-graph.json")
c1_no_solo = load("c1-no-solo-graph.json")
c2 = load("c2-graph.json")
c2_no_caller = load("c2-no-caller-graph.json")
c3 = load("c3-graph.json")
c3_no_alib = load("c3-no-alib-graph.json")
c4 = load("c4-graph.json")
continuity = load("c2-continuity.json")
occurrence = load("c1-occurrence-oracle.json")
c1_symbols_core = load("c1-symbols-core-graph.json")
c1_symbols_leaf = load("c1-symbols-leaf-graph.json")
c1_reference_missing = load("c1-reference-missing-graph.json")

c1_nodes, c1_edges = ids(c1)
c1v_nodes, c1v_edges = ids(c1_variant)
check("c1.node-count", len(c1_nodes) == 118, len(c1_nodes))
check("c1.edge-count", len(c1_edges) == 350, len(c1_edges))
check("c1.source-changed.node-identity",
      c1_nodes == c1v_nodes,
      f"changed nodes={len(c1_nodes ^ c1v_nodes)}")
check("c1.source-changed.edge-delta",
      len(c1_edges ^ c1v_edges) == 12,
      f"symmetric edge delta={len(c1_edges ^ c1v_edges)} (6 replaced declares)")
check("c1.solo-removal",
      len(c1_nodes - ids(c1_no_solo)[0]) == 11 and
      len(c1_edges - ids(c1_no_solo)[1]) == 20,
      f"nodes={len(c1_nodes - ids(c1_no_solo)[0])} edges={len(c1_edges - ids(c1_no_solo)[1])}")

sc_nodes, sc_edges = ids(c1_symbols_core)
check("c1.symbols-core-delta",
      sc_nodes == c1_nodes and len(sc_edges ^ c1_edges) == 2,
      f"nodes={len(sc_nodes ^ c1_nodes)} edge symmetric delta={len(sc_edges ^ c1_edges)} (one declares replacement)")
sl_nodes, sl_edges = ids(c1_symbols_leaf)
check("c1.symbols-leaf-delta",
      len(sl_nodes - c1_nodes) == 1 and len(sl_edges - c1_edges) == 2 and
      not (c1_nodes - sl_nodes) and not (c1_edges - sl_edges),
      f"nodes=+{len(sl_nodes - c1_nodes)} edges=+{len(sl_edges - c1_edges)}")
rm_nodes, rm_edges = ids(c1_reference_missing)
check("c1.reference-missing-delta",
      rm_nodes == c1_nodes and len(c1_edges - rm_edges) == 79 and
      not (rm_edges - c1_edges),
      f"nodes={len(rm_nodes ^ c1_nodes)} removedEdges={len(c1_edges - rm_edges)}")

c2_nodes, c2_edges = ids(c2)
check("c2.continuity.nodes",
      c2_nodes == set(continuity["nodeIds"]),
      f"expected={continuity['nodeCount']} actual={len(c2_nodes)}")
check("c2.continuity.edges",
      c2_edges == set(continuity["edgeIds"]),
      f"expected={continuity['edgeCount']} actual={len(c2_edges)}")
check("c2.caller-removal",
      len(c2_nodes - ids(c2_no_caller)[0]) == 32 and
      len(c2_edges - ids(c2_no_caller)[1]) == 91,
      f"nodes={len(c2_nodes - ids(c2_no_caller)[0])} edges={len(c2_edges - ids(c2_no_caller)[1])}")

c3_nodes = {node["Id"]: node for node in c3["nodes"]}
c3_edges = c3["edges"]
helpers = [node for node in c3["nodes"]
           if node["Kind"] == "type" and "Example.Helper" in node["QualifiedName"]]
check("c3.linked-contexts",
      len(helpers) == 2 and len({node["Project"] for node in helpers}) == 2,
      [node["Project"] for node in helpers])
calls = [edge for edge in c3_edges if edge["Kind"] == "calls"]
check("c3.app-call", len(calls) == 1, len(calls))
if calls:
    target = c3_nodes[calls[0]["Target"]]
    check("c3.app-call-blib-context",
          target["Project"] == "src/BLib/BLib.csproj",
          target["Project"])
check("c3.alib-contributor-removal",
      len(ids(c3)[0] - ids(c3_no_alib)[0]) == 4 and
      len(ids(c3)[1] - ids(c3_no_alib)[1]) == 6,
      f"nodes={len(ids(c3)[0] - ids(c3_no_alib)[0])} edges={len(ids(c3)[1] - ids(c3_no_alib)[1])}")

c4_nodes = {node["Id"]: node for node in c4["nodes"]}
c4_targets = [node for node in c4["nodes"]
              if node["Kind"] == "callable" and "Api.Mark" in node["QualifiedName"]]
check("c4.single-target", len(c4_targets) == 1, len(c4_targets))
if c4_targets:
    target = c4_targets[0]["Id"]
    calls = [edge for edge in c4["edges"]
             if edge["Kind"] == "calls" and edge["Target"] == target]
    sources = {c4_nodes[edge["Source"]]["Project"] for edge in calls}
    check("c4.two-contributors",
          sources == {"Caller/Caller.csproj", "Caller2/Caller2.csproj"},
          sorted(sources))
    check("c4.eleven-sites", len(calls) == 11, len(calls))

counts = occurrence["counts"]
check("c1.f12.invocations", counts["invocations"] == 53, counts["invocations"])
check("c1.f12.references", counts["method-references"] == 21, counts["method-references"])
check("c1.f12.overloads", counts["overloaded-methods"] == 5, counts["overloaded-methods"])
check("c1.f12.cross-project", counts["cross-project-invocations"] == 52,
      counts["cross-project-invocations"])
check("c1.f12.files", counts["csharp-files"] == 11, counts["csharp-files"])
check("c1.f12.projects", counts["projects"] == 5, counts["projects"])
check("c1.f12.namespaces", counts["namespaces"] == 5, counts["namespaces"])
check("c1.complete-empty", occurrence["coverage"]["completeEmpty"]["occurrences"] == 0,
      occurrence["coverage"]["completeEmpty"])
check("c1.reference-missing",
      any(case["id"] == "RM01" and case["pass"] for case in occurrence["variants"]),
      "RM01 unresolved")

OUT.write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps({"pass": result["pass"], "checks": len(result["checks"])}, indent=2))
raise SystemExit(0 if result["pass"] else 1)
