# Q2 adversarial fixture corpus (review-only, non-production)

Purpose: independently probe the EC-6D contract surface against the
**existing** EC-6C p9 extraction at the accepted baseline. These files are
review evidence for `EC6D-SCOPE-Q2`; they are not production code, and no
EC-6D behavior exists to test yet. The run was produced with the committed
planning harness (`docs/planning/ec6d/analysis/`, unmodified) executed in the
Q2 review worktree. Raw output:
`../../runs/adversarial-corpus-observation.json`.

Command (from the review worktree root, with the analysis harness built):

```
dotnet docs/planning/ec6d/analysis/bin/Debug/net10.0/ConsumerBaseline.dll \
  docs/reviews/ec6d-scope-q2-evidence/fixtures/adversarial \
  > docs/reviews/ec6d-scope-q2-evidence/runs/adversarial-corpus-observation.json
```

The harness selects the target `src/contracts/Renderable.ts` / `Renderable`
(hard-coded by the committed harness), so this corpus mirrors that target name
while exercising adversarial structures.

## Probes and observed p9 facts (run: 13 files, 68 node contributions, 63
distinct nodes, 99 edges)

| # | Fixture | Adversarial intent | Observed p9 facts (independently reproduced) | Consequence for the R1 contracts |
| --- | --- | --- | --- | --- |
| 1 | `src/merge/Merged.ts` (`interface Merge` twice, both exported) | Declaration merging, same namespace | One `interface Merge` symbol node; one `export` entry node named `Merge`; **two** `exports` occurrence edges (both `namespace-projection=type`) | Attribution rule sees a single declaration identity; the R1 §2.2.2 "merged interface + class" wording does not describe this shape. The rule text must define the candidate set (distinct declaration node identities). |
| 2 | `src/merge/ClassInterfaceMerge.ts` (`interface Mix` + `class Mix`) | Type/value namespace merging | **Two** declaration nodes named `Mix` (kinds `interface` and `type`); one export entry node; two occurrence edges (`projection=both` and `type`) | Makes the "exactly one declaration" rule ambiguous → attribution must not be emitted (fail-closed). No acceptance row currently tests this. |
| 3 | `src/alias/LocalAlias.ts` (`export { Q as QRename }`) | Aliased local export | Export entry `QRename` with `export-via=local`, `local-name=Q` | The local-name side needed by the attribution rule exists; alias resolution is implementable from p9 facts. |
| 4 | `src/barrel/StarBarrel.ts` (`export * from`) | Star re-export | `resolves-to` edge with `module-reference=export-star`; no export entries | Barrier detection is possible for star barrels **if** the predicate uses `module-reference`; the R1 wording ("outgoing resolves-to edges") is imprecise. |
| 5 | `src/barrel/NamedBarrel.ts` (named re-export) | Named re-export | `resolves-to` edge `module-reference=export-from`; no export entries | Same as #4 for named barrels (matches the committed corpus `src/api/index.ts`). |
| 6 | `src/consumers/MemberConsumer.ts` (`implements NS.Renderable`) | Qualified heritage name | **No heritage fact emitted** (no `implements` edge); import and resolution facts exist | Qualified `implements` names disappear at extraction; the R1 coverage statement does not disclose this, and no acceptance row reports it. Q1 required "reported, not silent". |
| 7 | `src/consumers/TypeParam.ts` (`class TypeParamC<Renderable> implements Renderable`) | Class type-parameter shadowing | Heritage fact emitted (`implements TypeParamC -> heritage-target Renderable`, span `3:48-3:58`) | Semantically invalid TS; shows the extraction never models type-parameter shadowing. Needs an explicit "semantically invalid sources" boundary statement (and/or a guard). |
| 8 | `src/consumers/NsShadow.ts` (`import type { Renderable as Drawable }` + `namespace NS { interface Drawable; class NsC implements Drawable }`) | Legal nested-scope shadowing of an imported alias | Nested `namespace NS`, `interface NS.Drawable`, class `NS.NsC`, and `implements NS.NsC -> heritage-target Drawable` are all emitted; import/resolution facts exist | **Import-first by local name alone would confirm `NsC implements Renderable`** although TypeScript resolves `Drawable` to `NS.Drawable`. False-confirmation path for valid TypeScript; the R1 §3.2a rule needs a scope restriction/guard. |
| 9 | `src/consumers/DupLocal.ts` (two imports both binding `Dup`) | Duplicate local names, invalid TS | Two `imports` reference edges and two `resolves-to` edges with **distinct identity hashes** (`484a7821…` line 2, `bd4b1103…` line 1) | Per-occurrence identity can distinguish duplicates; the binding rule must still fail closed on multiple same-localName occurrences (`duplicate-local-name`). No row tests it. |
| 10 | `src/consumers/SameFile.ts` (`class SameFileC implements LocalT`, local interface) | Same-file binding route | Heritage fact `SameFileC -> LocalT` at span `5:35-5:41`; declaration `LocalT` exists | The `same-file` route is implementable from p9 facts but has **no acceptance row** (positive or negative). |
| 11 | `src/consumers/StarConsumer.ts` (imports through `StarBarrel`) | Direct import through a star barrel | Import resolves to the barrel file only; heritage `StarC -> Renderable` | Barrier unknown can be produced for *heritage-bearing* consumers; see #12 for the corpus case without heritage. |
| 12 | `src/consumers/NamedBarrelConsumer.ts` (imports through `NamedBarrel`) | Named-barrel consumer | Same shape as #11 via `export-from` | Same. |
| 13 | `src/consumers/DupLocal.ts` (import line spans) | Span convention cross-check | Implements spans verified one-based, end-exclusive against source columns | Confirms the acceptance contract's documented span convention. |

## Cross-checks also performed

- `src/contracts/Renderable.ts` target node identity equals the committed
  corpus target identity (`typescript:symbol:b51a8ed9…`), demonstrating the
  identity scheme is content-position based, not corpus-specific.
- All adversarial runs complete with `Status=1` (completed), no warnings, and
  no duplicate-identity failure, including the merging fixtures.
- The `NsShadow` and collision shapes were additionally checked with a
  TypeScript 5.9.3 semantic oracle (Q2-authored; see
  `../../runs/typescript-oracle-q2.json`): the nested-shadow shape is **valid
  TypeScript and binds the namespace-local interface**, and the collision
  shape is invalid (TS2440) while resolving to the imported symbol when
  forced. The `TypeParam` shape is invalid (TS2422) and binds the class type
  parameter.

## Limits

- Probes exercise **p9 extraction only**; no EC-6D fact (import occurrence,
  attribution, binding) exists yet. Binding-level statements above are
  contract-analysis conclusions, not implemented behavior.
- The fixture corpus is intentionally hostile (some files are semantically
  invalid TypeScript); it is not an accuracy benchmark.
