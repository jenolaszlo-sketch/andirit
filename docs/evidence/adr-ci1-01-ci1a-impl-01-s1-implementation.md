# ADR-CI1-01-CI1A-IMPL-01-S1 — Implementation Evidence

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S1` (Stage 1 native vertical slice only).
**Status:** documentation of the Stage 1 producer implementation. Not independent
qualification. Stage 2 is **not authorized**.

## 1. Source binding

| Item | Value |
|---|---|
| Canonical repository | `D:\projects\Andirit` (`https://github.com/jenolaszlo-sketch/andirit.git`) |
| Branch | `main` |
| Starting HEAD | `fba53ceb28af0113979f512d251e73e18f211c0a` |
| Starting tree | `7bc88a67ce577a11f37d7259d4428cc23782553d` |
| Remote baseline | `origin/main` behind; local was ahead 3 at start; **nothing pushed** |
| Worktrees | 1 canonical; no branch or linked worktree created |
| Toolchain | .NET SDK 10.0.401, `net10.0` |
| Host | Microsoft Windows 10.0.26200, X64 |
| Native provider | `LatticeDbSharp` 0.2.0 |

The starting HEAD and tree matched the commission exactly. No reset/rebase/merge was
performed. Unrelated untracked files were preserved.

## 2. Changed-file inventory

Production:

- `src/Andirit.Abstractions/CodeOccurrence.cs` (new) — occurrence contracts, identity,
  contributor, coverage.
- `src/Andirit.Abstractions/CodeGraphBatch.cs` — additive `Occurrences` list and
  `MaxOccurrences` limit.
- `src/Andirit/CodeOccurrenceReader.cs` (new) — query/result/availability and
  `ICodeOccurrenceReader`.
- `src/Andirit/CodeGraphStoreModels.cs` — additive occurrence list + analysis flag on
  `CodeIndexUnitReplacement` (never serialized into the graph unit record).
- `src/Andirit/CodeGraphBatchValidator.cs` — occurrence ownership/semantics validation.
- `src/Andirit/CodeGraphIngestionSink.cs` — accumulates and forwards controlled
  occurrence records through the existing unit replacement path.
- `src/Andirit/InMemoryCodeGraphStore.cs` — occurrence state, target index,
  contributor union/conflict, bounded reader, publication binding.
- `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` — native occurrence tables, atomic
  publication, restore on open, schema/refusal, reader delegation.

Tests:

- `tests/Andirit.Tests/OccurrenceInMemoryParityTests.cs` (new).
- `tests/Andirit.LatticeDb.Tests/OccurrenceStage1NativeTests.cs` (new).
- `tests/Andirit.LatticeDb.Tests/OccurrenceStage1PerformanceProbe.cs` (new, measurement).
- `tests/Andirit.Tests/PublicApiContractTests.cs`, `RuntimePublicApiContractTests.cs`
  — additive public-type snapshots.

No change to `src/Andirit.CSharp`, `src/Andirit.TreeSitter`, existing graph labels,
`calls`/`references` identities, `FindCallers`/`FindReferences`, or legacy reader
surfaces.

## 3. Design summary

- **Occurrence facts** are separate, source-unit-owned records. They are never graph
  nodes or edges. `CodeIndexUnitReplacement.Occurrences` is additive and
  `[JsonIgnore]`d so the legacy `HetuUnit`/`HetuStage` payloads remain graph-only.
- **Identity** is domain-separated, deterministic, length-prefixed SHA-256
  (`andirit-asmctx3`, `andirit-usage4`, `andirit-occurrence-payload4`). The
  contributing unit is excluded from the occurrence identity and payload hash so
  identical physical occurrences union contributors. No clock, process id, run id or
  enumeration order participates.
- **Ownership**: a fact must belong to the batch's unit; replacement/deletion operate on
  the owning unit. Contributor union is computed across units.
- **Conflict**: identical identity with a different immutable payload is rejected at
  staging/publication (`occurrence.payload.conflict`), never overwritten.
- **Publication**: occurrence unit rows, target pages and meta are written in the same
  LatticeDB write transaction as graph completion (`Persist` → `PersistOccurrences`).
- **Retrieval**: a target-scoped in-memory index is rebuilt from the durable native
  occurrence rows on open and updated on publication. A query reads only the target
  page; `Examined` reports page entries read.

## 4. Controlled records vs confirmed production occurrences

Stage 1 uses **structurally valid controlled occurrence records** supplied with the
unit replacement, per the commission's Stage 1 boundary. It does **not** implement
Roslyn occurrence extraction, and these records are **not** presented as semantically
confirmed production occurrences. Full `compctx1`/`asmctx3`/`usage4` provenance and
S-01 source-byte binding remain Stage 2 obligations.

The native tests use representative synthetic graph units and controlled occurrence
facts through the real production store. The unchanged C# plugin regression suite
(`Andirit.CSharp.Tests`, 69 tests) remains the graph ingestion evidence.

## 5. Verified test results (producer)

| Suite | Result |
|---|---|
| `Andirit.Tests` | **315 passed / 0 failed / 0 skipped** |
| `Andirit.CSharp.Tests` | **69 / 0 / 0** |
| `Andirit.LatticeDb.Tests` | **38 / 0 / 0** (includes 8 mandatory native + 2 schema + 1 performance probe) |
| `Andirit.TreeSitter.Tests` | **157 / 0 / 0** |

Commands:

```
dotnet build Andirit.slnx -c Debug
dotnet test tests/Andirit.Tests/Andirit.Tests.csproj -c Debug
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj -c Debug
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Debug
dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj -c Debug
```

## 6. Known limitations

- Occurrence data is controlled, not Roslyn-extracted (authorized Stage 1 boundary).
- Native tests use synthetic representative graph units, not a real `.csproj` ingestion,
  for the occurrence slice.
- Target pages are recomputed for the repository at each completion (bounded, Stage 1);
  incremental page maintenance is deferred.
- No crash/power-loss or acknowledgment-loss qualification is claimed.
- Historical prior-writer fixture (SQ-06) was not generated; schema refusal is proven on
  fresh stores (see schema evidence). This remains a Stage 1 qualification.
