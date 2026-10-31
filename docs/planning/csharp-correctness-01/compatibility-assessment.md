# Compatibility assessment — ANDIRIT-CSharp-CORRECTNESS-01

## Public APIs, persistence and query contracts

| Contract | Status |
| --- | --- |
| `ICodeGraphPlugin` / `ICodeGraphExtractionSession` / `ICodeGraphSink` surface | **Unchanged.** `CSharpPublicApiContractTests` passes; no public type, method or property added/removed. |
| Public FindUsages / occurrence APIs | None added (not authorized, not needed). |
| Graph schema (node kinds, edge kinds, property keys, identities) | **Unchanged.** All `CodeNodeId`/`CodeSymbolId`/`CodeDeclarationId`/`CodeEdgeId` construction rules are byte-identical. |
| Publication lifecycle / index-unit replacement | Unchanged (`Session.ExtractAsync` write path untouched apart from property materialization before the same write). |
| Caller/callee/reference/impact query paths | Full `Andirit.Tests` (312), `LatticeDb.Tests` (27), `TreeSitter.Tests` (157) pass unchanged. |

## Identity stability (proven, not asserted)

The `cross` fixture (genuine cross-project call + inheritance + implements +
import, no defects exercised) produces **byte-identical** extraction JSON
before and after the fix:

| Artifact | Baseline `5c89ac0` | Remediated |
| --- | --- | --- |
| `evidence/comparison/cross.baseline.json` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` | `bf8681cae584e32cca3b932525aaab6959478a3dd42edd73d3f2b17b69b0ab72` (identical) |

In the `mixed` fixture, every edge that survives in both runs keeps its exact
`CodeEdgeId` (script-verified: *surviving-edge IDs stable: True*). No global
edge-ID rewrite occurred.

## Intentionally changed semantic results

### 1. Removed — false resolved edges (P-DEF-01)

`mixed` fixture: 45 edges → 42. All three removed edges are the defect:

| Removed edge | Source | Target |
| --- | --- | --- |
| `calls` | `App.App.Run()` (`src/Z/App.cs`) | `System.Console.WriteLine(string)` — actually the unrelated `A/Shadow.cs` declaration (`csharp:symbol:8102d2ac…`) |
| `references` | `App.App.Run()` | `System.Console` (shadow type node `csharp:symbol:3f3042d1…`) |
| `references` | `App.App.Run()` | shadow method node `csharp:symbol:8102d2ac…` |

Coverage delta (`calls`): `edgesEmitted 2→1`, `crossProjectTargets 2→1`,
`externalTargets 0→1`. These are accuracy corrections, not regressions.

### 2. Restored — previously suppressed valid edges (P-DEF-01 strictness flip side)

`ambiguous` fixture (same documentation ID in an unrelated project + genuine
dependency): 19 → 22 edges. Baseline refused the provable relationship
(`calls: e=0, unresolved=1, ambiguous=1`); the remediated resolver proves
dependency correspondence and emits:

| Added edge | Source | Target |
| --- | --- | --- |
| `calls` | `App.Program.Run()` | `Example.Helper.Go()` in `src/Lib` (`csharp:symbol:e186634b…`) |
| `references` | `App.Program.Run()` | `Example.Helper.Go()` (`src/Lib`) |
| `references` | `App.Program.Run()` | `Example.Helper` type (`src/Lib`) |

Coverage delta (`calls`): `edgesEmitted 0→1`, `crossProjectTargets 0→1`,
`external 0`, `ambiguous 1→0`.

### 3. Retargeted test expectation

`ExtractAsync_AmbiguousTargetsAreCountedNeverGuessed` →
`ExtractAsync_UnrelatedDuplicateDoesNotMakeAnExactDependencyAmbiguous`
(fixture unchanged; assertion now expects exact resolution). See
[`regression-matrix.md`](regression-matrix.md#retargeted-pre-existing-test).

### 4. Property payload changes (P-DEF-02)

For non-partial symbols the emitted properties are identical (documentation is
materialized with the same single value as before — the merge of a one-element
set is the value itself). For multi-part declarations of one logical symbol:

- `doc-summary`/`doc-remarks` on the logical node become the deterministic
  ordinal-sorted distinct merge (bounded 2048). Previously the input either
  crashed or (undocumented/identical cases) emitted the identical value, so
  only genuinely multi-valued documented partials change — and only from
  "extraction fails" to "deterministic bounded merge".
- A previously impossible output class (documented partial type) now extracts
  with one logical node and N physical declarations.

## Explicitly preserved incorrect-behaviour boundary

- No incorrect relationship is preserved for historical cardinality: the
  three false edges are removed, not reclassified or retained.
- No valid relationship was weakened: `cross` byte-identity plus rows 11–12 of
  the regression matrix demonstrate cross-project calls, references, imports,
  inheritance and implements all still resolve.
- Remaining unsupported/ambiguous contexts are unchanged in kind and now
  counted honestly (`ExternalTargets`, `AmbiguousTargets`, `UnresolvedTargets`,
  `UnsupportedTargets`).

## Not done (out of scope, unchanged)

CI-1A implementation; occurrence-level storage; query API changes; graph
schema migrations; NuGet evaluation; TS/JS changes; release/packaging.
