# FQ1-EV-1 Migration Reproduction Instructions

**Purpose:** reproduce the EC-6C.2 authentic p8→p9 LatticeDb migration claim
independently: an authentic p8 store produced by the pre-publication EC-6C.1
implementation, reindexed under p9, with complete existing-fact comparison.

**Scope boundary:** these are non-production evidence harnesses. They never
modify the canonical repository, the preserved qualification worktree, or any
implementation branch. The p8 checkout is a disposable detached worktree.

## 1. Required runtime and tool versions

- Windows (evidence was produced on Windows 11 x64); works on any OS with the
  .NET SDK.
- .NET SDK `10.0.401`; projects target `net10.0`.
- `git` available on `PATH`.
- No network access required beyond the NuGet restore of the referenced
  projects (the pinned Tree-sitter native packages restore from NuGet).

## 2. Source seed and exact Git binding

- Repository: `jenolaszlo-sketch/penghou-hetu`.
- Pre-publication seed implementation (detached checkout):
  `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`
  (tree `8a3cc154486bf9796b532ee4db96714fa63763b2`; "docs: record EC-6C.2
  integration prerequisites" — profile p8, no export publisher).
- p9 side: any checkout containing the accepted EC-6C.2 implementation
  (e.g. the owner-acceptance worktree, base
  `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a`).
- The seed fixture text is embedded verbatim in `seed-p8/Program.cs` and is
  identical to
  `TreeSitterEcmaExportShapeTests.PublishedGraphAddsExportsWithoutChangingPreChangeP8Facts`.

## 3. Seed construction procedure (run once, from the p9 checkout)

```powershell
# 1. Create a disposable detached checkout of the pre-publication implementation.
$seedCheckout = Join-Path $env:TEMP "penghu-fq1ev1-p8seed"
git -C <canonical-or-any-hetu-checkout> worktree add --detach $seedCheckout 8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340

# 2. Run the seed harness against that checkout.
$workspace = Join-Path $env:TEMP "penghu-fq1ev1-migration"
dotnet run --project .\seed-p8 -p:HetuRoot=$seedCheckout -- $workspace
```

The seed harness writes `<workspace>\ts\src\main.ts`, `<workspace>\ts\src\dep.ts`,
`<workspace>\js\src\main.js`, `<workspace>\js\src\dep.js` (LF line endings) and
indexes them into a new LatticeDb store at `<workspace>\graph.db` using
repository IDs `repo:q1-migration-ts` and `repo:q1-migration-js` and run IDs
`run:q1-p8-ts` / `run:q1-p8-js` through the real `CodeIndexingService`.

Expected seed output (must match before continuing):

```text
ts p8 status: Completed
js p8 status: Completed
typescript plugin version (seed): 0.2.0-preview.6+v0.23.2.abi14.p8
javascript plugin version (seed): 0.2.0-preview.6+v0.25.0.abi15.p8
ts p8 units: 3 nodes=7 decls=4 edges=12
js p8 units: 3 nodes=7 decls=4 edges=12
```

## 4. Migration invocation

```powershell
# From the p9 checkout (default HetuRoot resolves to the checkout root):
dotnet run --project .\reindex-p9 -- $workspace <output\report.json>
```

The harness (a) captures the persisted p8 facts, (b) reindexes the same
sources under p9 (run IDs `run:q1-p9-ts` / `run:q1-p9-js`), (c) compares,
(d) closes/reopens and queries, and (e) attempts reconciliation of the
historical recorded hash. It never writes to the source fixtures. The store
file remains in `<workspace>`.

## 5. Snapshot serialization and canonicalization rules

- Facts are read through `ICodeGraphStore.GetPublishedUnitsAsync`; each unit's
  nodes, declarations, and edges are serialized as System.Text.Json objects
  with PascalCase property names and default escaping (no indentation, UTF-8
  text without BOM).
- Every fact list is ordered by the serialized fact string, ordinal
  (`StringComparer.Ordinal`), which is the canonical order.
- Node facts project: `Id`, `Kind`, `Name`, `QualifiedName`, `SymbolId`,
  `Properties`.
- Declaration facts project: `Id`, `SymbolNodeId`, `SymbolId`.
- Edge facts project: `Id`, `SourceId`, `TargetId`, `Kind`, `Evidence`
  (quality name, provider, provider version, location tuple, confidence), and
  `Properties`.
- `Properties` are canonicalized as an array of `(Key, Value)` pairs sorted
  by key ordinal; values carry a type tag (`t:` text, `b:` boolean,
  `i:` integer, `n:` number, `l:` text list, `o:` other) so distinct value
  types cannot collide.
- The existing-fact comparison string is the JSON object
  `{"Nodes":[...],"Declarations":[...],"Edges":[...]}` built from those
  canonical lists (see `Serialization` in `reindex-p9/Program.cs`).

## 6. Encoding and byte-order assumptions

- Strings are .NET UTF-16 text; hashing encodes with `Encoding.UTF8` (no BOM).
- The JSON serializer emits UTF-8 text; .NET's `GetBytes` never prepends a
  BOM. BOM and CRLF variants are attempted only inside the hash
  reconciliation list; they are not part of the canonical format.

## 7. Hash algorithm and exact hashed byte sequence

- Algorithm: SHA-256 (`System.Security.Cryptography.SHA256`), lowercase hex
  output (`Convert.ToHexStringLower`).
- Hashed byte sequence: the UTF-8 bytes of the exact canonical serialization
  string defined in §5. The report field
  `TypeScriptExistingSnapshotSha256` / `JavaScriptExistingSnapshotSha256`
  records this for the p9-filtered existing-fact snapshot.
- The reproduction also records a reconciliation list
  (`PrecomputedAttempts`, `HashReconciliation`) of additional candidate
  serializations for the historical recorded hash, including live-object
  serializations, camelCase, CRLF, BOM, combined TS+JS, and the public
  `CodePublicationSnapshot` export hashes. These candidates are recorded
  attempts, never replacements for the canonical value.

## 8. Expected structural counts

| Measurement | p8 | p9 |
| --- | ---: | ---: |
| Nodes | 7 | 12 |
| Declarations | 4 | 4 |
| Edges | 12 | 17 |
| Export entries | — | 5 |
| `exports` edges | — | 5 |

Expected p9 export entry names — TypeScript: `Remote`, `current`, `remote`,
`run`, `value`; JavaScript: `current`, `default`, `item`, `remote`, `value`.
Expected p9 unit-origin plugin versions end `.p9`; exporter edge provider
version is `0.2.0-preview.6+exports1.local-v1`; existing edge evidence keeps
`.p8`; resolver edges keep `0.2.0-preview.6+resolver2.relative-v1`.

## 9. Existing-fact comparison procedure

1. Serialize the complete p8 fact set (§5).
2. Serialize the complete p9 fact set.
3. Filter the p9 serialization to nodes whose `Kind != "export"` and edges
   whose `Kind != "exports"` (p8 contains no export facts by construction, so
   the p8 side needs no filtering).
4. Require ordinal string equality of the two serializations: the report
   fields `TypeScriptExistingFactsIdentical` and
   `JavaScriptExistingFactsIdentical` must both be `true`.
5. Record the SHA-256 of the p9-filtered serialization as the canonical
   snapshot hash for this run.

## 10. Cleanup and rerun procedure

```powershell
# Remove the disposable p8 checkout and the workspace.
git worktree remove --force $seedCheckout
git worktree prune
Remove-Item -Recurse -Force $workspace
# Optional: remove harness build outputs.
Remove-Item -Recurse -Force .\seed-p8\bin, .\seed-p8\obj, .\reindex-p9\bin, .\reindex-p9\obj
```

Rerun determinism: delete `<workspace>` and repeat §3–§4; all logical facts,
identities, counts, and canonical hashes are deterministic (no timestamps,
GUIDs, or ordering nondeterminism enter the logical snapshot). The LatticeDb
**database file** itself is not byte-stable (command log, file allocation,
native mirror state) and must never be hashed as a proxy for the logical
snapshot; only the canonical logical serialization in §5 is compared. The
workspace lives outside the repository, so nothing in the checkout changes.

## Historical-hash note

The implementation handoff recorded an existing-fact snapshot SHA-256 of
`ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f` produced by
tooling that was not committed. The reproduction records every attempted
serialization in the report. If no attempt matches, the historical hash
remains unreproducible and that limitation is preserved as-is; the
migration-integrity demonstration (step §9 and §8) is unaffected because it
depends only on the canonical serialization defined here, not on the
historical value.
