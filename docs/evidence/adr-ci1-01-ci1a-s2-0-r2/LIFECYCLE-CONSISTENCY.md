# Lifecycle consistency (DEL01)

## Correction scope

The lifecycle projection (`AppFacts` → `RunLifecycle`) now builds its facts
through the same `CanonicalTarget` pathway as the main identity view. Source
ownership, source-set deletion semantics, and the DEL01 structure are
unchanged from R1; only the target-description inputs were corrected, exactly
as Q2-F01 requires. The `lifecycle` section is a compared frozen section of
`oracle-expected-r2.json`.

## Snapshot A / Snapshot B (qualified run, `oracle-r2-run-1.json`)

- Snapshot A (`App/Sites.cs` + `App/Bulk.cs`): **20** source-owned facts from
  `App/Sites.cs`, **32** from `App/Bulk.cs` — the accepted counts are preserved.
- Snapshot B (authoritative manifest excludes `App/Sites.cs`): **32**
  `App/Bulk.cs` facts retained with byte-identical canonical tuples; **0**
  `App/Sites.cs` facts; the reduced App compilation is complete and
  deterministic across repeated builds.
- Corrected canonical values appear in both snapshots: S01
  `d1ae71fc…` and INV-01 `699a94f4…` now carry the same identity in the
  lifecycle projection as in the main identity view; reduced-extension facts
  (S06/`561e2287…`, INV-31/`da3a32d1…`) use the unreduced defining
  declarations.

## Executed checks (all pass)

| Check | Asserts |
|---|---|
| `lifecycle.DEL-N1` | authoritative manifest excludes Sites.cs; reduced compilation complete; removed source-owned facts absent |
| `lifecycle.DEL-N2` | manifest still declaring Sites.cs while acquisition omits it is incomplete |
| `lifecycle.DEL-N3` | injecting prior Sites.cs contributions into the reduced snapshot fails the projection comparison |
| `lifecycle.DEL-counts` | Sites=20, Bulk=32 (A); Bulk=32, no Sites.cs (B) |
| `lifecycle.preserved` | surviving `App/Bulk.cs` facts keep their corrected canonical usage4 identities |
| `lifecycle.identity` | source-set deletion recomputes App compctx1/compctx2; surviving identities derived independently |
| `lifecycle.deterministic` | repeated reduced-scope compilation reproduces contexts and facts |
| `crossView.snapshotB-retained` / `-removed` | the 32 Bulk facts are stable in B; all 20 Sites facts are absent from B |
| `crossView.ID-3-source-ownership` | facts keyed by owning source unit, not target identity |

## External mutation controls

`mutations/deleted-contribution-reinjected/` reinjects the 20 deleted
`App/Sites.cs` contributions into the reduced projection: exit 1 with
`lifecycle.DEL-N1`, `lifecycle.DEL-counts`, `lifecycle.deterministic`, and
`expected.lifecycle` failing — the stale-contribution rejection is enforced
against the frozen expected bytes, not merely self-compared.
