# ADR-CI1-01-CI1A-IMPL-01-S1 — D3 Legacy Graph Noninterference

**Requirement:** occurrence publication must produce zero unauthorized semantic change in
existing graph behavior. A graph count change caused by occurrence facts, or a named query
staying the same while native unfiltered traversal changes, is a blocking defect.

## 1. Corpus

The native D3 test (`N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`) builds two
representative graph units:

- `unit:d3:a`, `unit:d3:b`, each with an owner `callable`, a `callee`, and a `caller`;
- a resolved `calls` edge `caller → callee`;
- a resolved `references` edge `caller → owner`.

This covers method-like symbols, call relationships, reference relationships, and
multiple units. A third unit `unit:d3:occ` carries **occurrence facts only** (no graph
nodes/edges) so its replacement/deletion must not touch the graph.

## 2. Observed snapshot

For each event the test captures an exact string:

```
nodes=[sorted node ids]
edges=[sorted edge ids]
callers=[nodes reachable as incoming calls to node:d3:A:callee]
references=[nodes reachable as incoming references to node:d3:A:owner]
caller=<qualified name of node:d3:A:caller>
```

The snapshot is compared with `Assert.Equal(baseline, current)` — exact identity, order
and cardinality, not a fuzzy count.

## 3. Events compared

| Event | Result |
|---|---|
| Baseline (graph only) | baseline snapshot |
| Initial occurrence publication (`unit:d3:a` + facts, `unit:d3:occ` facts) | identical |
| Occurrence replacement (`unit:d3:occ` facts changed) | identical |
| Occurrence-bearing unit deletion (`unit:d3:occ`) | identical |
| Failed occurrence publication (`before-commit` fault) | identical |
| Close and reopen | identical |

All six observations matched exactly. No occurrence fact appeared as a graph node or
edge; `calls`/`references` identities, traversal results, node/edge enumerations and
counts were unchanged.

## 4. Structural guarantees

- Occurrence data lives in `AndiritOccurrenceUnit/Page/Meta`, never `Hetu*`.
- `CodeIndexUnitReplacement.Occurrences` is excluded from graph-unit serialization.
- No traversal preset, `ICodeGraphReader` method, `FindCallers`/`FindReferences`,
  `NativeGraphMirror`, or graph query signature was changed.
- The graph materialization path does not read occurrence rows.

## 5. Additional regression evidence

- `Andirit.CSharp.Tests`: **69/69** pass unchanged (real Roslyn graph extraction,
  inheritance/implement/calls/cross-project/rebind/references fixtures).
- `Andirit.Tests`: **315/315** (graph semantics, query, ingestion, snapshot).
- `Andirit.TreeSitter.Tests`: **157/157**.
- `Andirit.LatticeDb.Tests`: **38/38** (native graph + occurrence + D3).

## 6. Limits

The D3 corpus is **store-level and representative**, not the full C#-plugin corpus
enumerated in `specification.md` §6.1 (that corpus remains for the broader Stage 6 D3
regression). No statement is made about platforms or schemas outside the tested store.
