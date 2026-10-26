# FQ1-EV-1 Fresh Reproduction Attempt (EC6C-OWNER-01)

**Date:** 2026-10-26
**Tooling:** `seed-p8/` and `reindex-p9/` in this directory (committed by
EC6C-OWNER-01; derived from the GLM-Q1 harnesses and extended with additional
reconciliation candidates). **Instructions:** `README.md`.
**Environment:** Windows; .NET SDK `10.0.401`; `net10.0`; git.

## Commands executed and exit results

| # | Command | Result |
| --- | --- | --- |
| 1 | `git worktree add --detach <temp>\owner01-p8seed 8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` | exit 0; checkout verified `8c1d5e3` / tree `8a3cc154486bf9796b532ee4db96714fa63763b2` |
| 2 | `dotnet run --project seed-p8 -p:HetuRoot=<temp>\owner01-p8seed -- <temp>\owner01-migration` | exit 0; expected output exact (see below) |
| 3 | `dotnet run --project reindex-p9 -- <temp>\owner01-migration <this dir>\reproduction-report.json` | exit 0; report written |

Seed output (step 2), matching the committed expectations exactly:

```text
ts p8 status: Completed
js p8 status: Completed
typescript plugin version (seed): 0.2.0-preview.6+v0.23.2.abi14.p8
javascript plugin version (seed): 0.2.0-preview.6+v0.25.0.abi15.p8
ts p8 units: 3 nodes=7 decls=4 edges=12
js p8 units: 3 nodes=7 decls=4 edges=12
```

## Actual structural counts (report fields)

| Measurement | p8 actual | p9 actual | Expected | Equality |
| --- | ---: | ---: | ---: | --- |
| TypeScript nodes / declarations / edges | 7 / 4 / 12 | 12 / 4 / 17 | 7/4/12 → 12/4/17 | match |
| JavaScript nodes / declarations / edges | 7 / 4 / 12 | 12 / 4 / 17 | 7/4/12 → 12/4/17 | match |
| TypeScript export entries / `exports` edges | — | 5 / 5 | 5 / 5 | match |
| JavaScript export entries / `exports` edges | — | 5 / 5 | 5 / 5 | match |

Export names: TypeScript `Remote, current, remote, run, value`; JavaScript
`current, default, item, remote, value` (both as expected).

## Actual computed hashes and equality results

| Measurement | Value |
| --- | --- |
| `TypeScriptExistingFactsIdentical` | **true** |
| `JavaScriptExistingFactsIdentical` | **true** |
| Canonical TypeScript existing-fact snapshot SHA-256 (this run) | `7f0e2bc1f51bb96eaee3247e2018bae3a42f2d50cdd86e753fbf3c3c207dc39d` |
| Canonical JavaScript existing-fact snapshot SHA-256 (this run) | `7b8dd5fed89d72bb090d1ecf9aa87fad4ccd5526ed3f04cbd69751849dd9d1a3` |
| `P9TsResolverUnchanged` / `P9JsResolverUnchanged` | true (`0.2.0-preview.6+resolver2.relative-v1`) |
| p9 unit-origin plugin versions | `…v0.23.2.abi14.p9` / `…v0.25.0.abi15.p9` |
| p9 existing-edge evidence versions | `…abi14.p8` and `resolver2.relative-v1` |
| p9 export-edge evidence versions | `0.2.0-preview.6+exports1.local-v1` |
| Reopen exact-name `current` | one `export` candidate `typescript:export:70d1ce79…` |
| Reopen neighborhood (exports filter, `src/main.ts`) | 3 occurrence targets |

Side observation recorded by the reconciliation set: the live-object
serializations of the filtered existing facts are identical between the p8
and p9 stores (`p8-ts-live-default == p9-ts-live-default = ea7e2f82…`;
`p8-js-live-default == p9-js-live-default = 8fe154cb…`), independently
corroborating the byte-identity result.

## Historical hash reconciliation (`ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f`)

30 candidate serializations were computed and compared (16 precomputed +
14 string-based; full list in `reproduction-report.json`
`PrecomputedAttempts` and `HashReconciliation`):

- live-object serializations (framework default and camelCase) of filtered
  facts for p8-ts, p8-js, p9-ts, p9-js;
- `CodePublicationSnapshot.ExportAsync` `ToJson()` and `IntegrityHash` for
  p8-ts, p8-js, p9-ts, p9-js (full publication, no filtering);
- canonical projection, CRLF variant, UTF-8-BOM variant, camelCase variant,
  re-encoded JSON tree, compact stream, each for ts and js;
- combined ts+js projection and stream.

**Divergence:** none of the 30 candidates equals the historical hash. The
recorded value `ff6b88…` remains unreproducible from the committed tooling
and from every natural serialization attempted. It is preserved as a recorded
historical value and is **not** replaced.

## Impact assessment of the unresolved hash

- The reproduction failure affects **only the historical hash value**. The
  underlying migration-integrity demonstration is independently supported by
  this run: the same seed construction, migration, and complete existing-fact
  comparison were executed end-to-end, with byte-identical existing facts for
  both languages, the exact expected counts and additions, unchanged resolver
  facts, p9 origin metadata, and working close/reopen queries.
- The historical tooling that produced `ff6b88…` (and therefore its exact
  serialization, canonicalization, and hashed byte sequence) was never
  committed and cannot be reconstructed from its output; the assignment's
  boundary ("do not alter migration production code to force a match")
  was respected — no production or harness behavior was adjusted to influence
  the comparison.
- The canonical serialization used here is fully specified in `README.md` §5–§7
  and is deterministic for the logical fact set; the LatticeDb database file
  itself is explicitly not hashed (`README.md` §10).

## Cleanup

After this record and `reproduction-report.json` were captured:

- the disposable p8 checkout and the temp workspace were removed
  (`git worktree remove --force`, `git worktree prune`,
  `Remove-Item -Recurse`), and the harness `bin`/`obj` outputs were deleted
  (gitignored; removed to keep the checkout lean). The acceptance worktree
  remains clean.
