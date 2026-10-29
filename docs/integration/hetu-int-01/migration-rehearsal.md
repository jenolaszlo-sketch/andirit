# Fresh durable migration rehearsal

**PASS.** This is a disposable-store integration gate, not a production migration.

Provider: `LatticeDbSharp` 0.2.0 / Hetu LatticeDB provider; schema 2 throughout. Profiles: default authoritative materialized projection and experimental `UseNativeTraversal=true`. No SQLite implementation exists in the accepted source/tests; SQLite is therefore not applicable. No unsupported migration support is inferred.

Source p6: `558f8ac82ac1b5e07da68f4a71a70240eaf38409`; p8: `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`; p9: tested integrated candidate `5548bf4a870733341a1471eb26a9727192dc9f48`, production-equivalent to `2c3090câ€¦`. Initial execution referenced existing checked-out sources; no linked worktree was created.

Fixture source is the accepted FQ1-EV-1 TS/JS migration corpus, embedded byte-for-byte in [MigrationProbe.cs](tools/MigrationProbe.cs). A genuine p6 implementation persisted each seed before it was closed and copied. Original seeds were retained and reopened with p6 after all rehearsals. Each path uses a separate disposable copy outside the canonical checkout; it never operates on a live or sole-copy user database.

| Path | Languages | Traversal profiles | Fresh result |
|---|---|---|---|
| p6â†’p9 direct reindex | TS + JS | Projection + native | Pass |
| p6â†’p8â†’p9 staged reindex | TS + JS | Projection + native | Pass |
| p9 repeated indexing | TS + JS | Projection + native | Completed, zero plugins executed, identical facts |
| Cancelled indexing | TS + JS | Projection + native | Observable `OperationCanceledException`, publication unchanged |
| Reopen and original-seed recovery | TS + JS | Projection + native | Healthy schema 2; exact logical facts retained |

Per-language counts: p6 7 nodes / 4 declarations / 10 edges; p8 7 / 4 / 12; p9 12 / 4 / 17, including five positive export entries and five `exports` edges. Old node/declaration/edge IDs survive. During p6â†’p8 reindex, existing edge producer evidence versions change from `.p6` to `.p8`, as accepted by EC-6B; every other existing edge field is identical. This expected attribution change is recorded explicitly, not described as byte identity. p8â†’p9 existing facts are exactly identical. Direct and staged p9 snapshots agree exactly in both profiles.

The probe checks all existing facts, endpoint existence, duplicate identities, old exact-name candidates, default neighborhood identity preservation, p9 origin versions, new export lookup, health after reopen, repeat behavior and cancellation. The solution's durable resolver-failure and store-failure regressions additionally passed; cancellation evidence alone is not a claim covering every failure mode. Preview schema-version mismatches retain their existing reject/export-import contract; this rehearsal changes extraction profiles within schema 2, not a provider schema format.

Exact executed commands: [migration commands](raw/migration-commands.txt). Full reports: [verification](raw/migration-verification.json), [direct projection](raw/migration-reports/direct-p9-projection.json), [staged projection](raw/migration-reports/staged-p9-projection.json), [direct native](raw/migration-reports/direct-p9-native.json), [staged native](raw/migration-reports/staged-p9-native.json). All stage/reopen/repeat/cancellation observations are in `raw/migration-reports/` and covered by SHA256SUMS.

Replay without linked worktrees (exports exact Git sources into a new disposable directory):

```powershell
pwsh -File docs/integration/hetu-int-01/tools/reproduce-migration.ps1 -Repository <canonical-path> -Workspace <new-disposable-path>
```

Fresh observations were checked by [verify_migration.py](tools/verify_migration.py); no expected hash was changed to obtain a pass. Historical `ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f` remains unreproducible. These new reproducible comparisons establish the integration rehearsal; they do not replace that historical value or remove its qualification. Representative deployment/performance and native Linux/macOS migration qualifications remain open.

The portable replay was also executed successfully from fresh Git archive exports; see [replay transcript](raw/portable-rehearsal.txt) and [replay verification](raw/portable-rehearsal-verification.json). No linked worktree was created.
