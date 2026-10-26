# EC-6C.2 GLM-Q1 Evidence — Authentic p8→p9 LatticeDb Migration

**Qualification:** EC-6C.2 independent qualification GLM-Q1
**Date:** 2026-10-26
**Harnesses:** `harness/seed-p8/` (runs against a detached checkout of
`8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`, the pre-publication EC-6C.1
implementation) and `harness/reindex-p9/` (runs against the qualification
worktree at the implementation HEAD). Raw output:
`migration-report.json`.

## Method

1. A disposable detached worktree at `8c1d5e3` (tree
   `8a3cc154486bf9796b532ee4db96714fa63763b2`) was created from the canonical
   repository; no branch was created or modified. Its sources built the seed
   harness, so the p8 seed was produced by the genuine pre-publication
   implementation (profile p8, no publisher).
2. The seed harness wrote the recorded migration fixture (TypeScript:
   `src/main.ts` + `src/dep.ts`; JavaScript: `src/main.js` + `src/dep.js`,
   LF line endings, identical text to
   `TreeSitterEcmaExportShapeTests.PublishedGraphAddsExportsWithoutChangingPreChangeP8Facts`)
   into a temp workspace and indexed it with the real
   `CodeIndexingService` + `LatticeCodeGraphStore` (two repositories:
   `repo:q1-migration-ts`, `repo:q1-migration-js`). The store was closed.
3. The p9 harness reopened the same store in the qualification worktree,
   captured the complete persisted p8 fact set, reindexed the same sources
   with the p9 plugins, captured the complete p9 fact set, compared them,
   closed, reopened, and queried.

## Results (both fixtures)

| Measurement | p8 (observed) | p9 (observed) | Recorded claim | Match |
| --- | ---: | ---: | ---: | --- |
| Nodes | 7 | 12 | 7 → 12 | yes |
| Physical declarations | 4 | 4 | 4 → 4 | yes |
| Edges | 12 | 17 | 12 → 17 | yes |
| Added export entries | — | 5 | 5 | yes |
| Added `exports` edges | — | 5 | 5 | yes |

Observed p9 export entries:

- TypeScript: `Remote`, `current`, `remote`, `run`, `value`
- JavaScript: `current`, `default`, `item`, `remote`, `value`

## Existing-fact comparison

The p8 fact set was serialized with a deterministic projection (facts ordered
by ID, properties sorted, complete evidence included; format recorded in the
harness source). Filtering the p9 graph to non-`export` nodes and
non-`exports` edges produced a byte-identical serialization to the complete
p8 snapshot:

- TypeScript: `TypeScriptExistingFactsIdentical = true`
- JavaScript: `JavaScriptExistingFactsIdentical = true`

Existing identities, relationships, and evidence were therefore preserved as
complete facts, not only counts. Existing non-export edges retain provider
version `0.2.0-preview.6+v0.23.2.abi14.p8` (TypeScript) /
`0.2.0-preview.6+v0.25.0.abi15.p8` (JavaScript) and the resolver edges keep
`0.2.0-preview.6+resolver2.relative-v1` unchanged. New export edges use
`0.2.0-preview.6+exports1.local-v1`.

Planner metadata progressed: unit-origin plugin versions changed from
`...p8` to `...p9` for both languages, which is the invalidation control
recorded by the handoff.

## Close/reopen query behavior

After the p9 reindex the store was closed and reopened:

- Exact-name query `current` (TypeScript repository) returned exactly the
  `export` entry `typescript:export:70d1ce79…` (no declaration with that name
  exists in the fixture).
- File-neighborhood query on `typescript:file:<sha256("src/main.ts")>` filtered
  to `exports` returned the 3 expected occurrence edges for `main.ts`.

## Recorded snapshot hash reconciliation

The handoff records an existing-fact snapshot SHA-256 of
`ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f` produced by
tooling that was not committed ("a temporary migration report"). Three natural
serializations of the same filtered fact set were hashed in an attempt to
reconcile the recorded value: a projected JSON snapshot, a re-encoded
System.Text.Json object tree, and a compact fact stream — for both the
TypeScript and JavaScript fixtures. None matched the recorded value. The
qualification's own snapshot hashes are recorded in `migration-report.json`
(`TypeScriptExistingSnapshotSha256` = `7f0e2bc1…`,
`JavaScriptExistingSnapshotSha256` = `7b8dd5fe…`).

**Assessment:** the substantive claim (existing filtered facts byte-identical
across the p8→p9 transition, with the recorded counts and additions) is
independently demonstrated. The specific recorded hash is not reproducible
without the uncommitted serialization format and is treated as an unverifiable
numeric claim rather than a defect.

## Provenance

- Seed implementation commit: `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`
  (tree `8a3cc154486bf9796b532ee4db96714fa63763b2`), detached worktree
  `C:\Users\Laszlos\source\repos\Penghou.Hetu.q1-p8seed`, removed after the
  seed was captured.
- Reindex implementation: qualification HEAD under review.
- The store, workspace, and report were produced under
  `%TEMP%\opencode\ec6c2-q1-migration\`; the report JSON is committed here.
