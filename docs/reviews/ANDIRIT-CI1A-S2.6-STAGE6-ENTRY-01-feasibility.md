# Native cancellation feasibility assessment — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Baseline**: `b5a81f81fade9807f706b4586329efe416cf1558` (production `src/` unchanged since the
qualified implementation `eafd632…`)
**Subject**: `Andirit.LatticeDb.LatticeCodeGraphStore` and its pinned native dependency
`LatticeDbSharp 0.2.0`
**Method**: source inspection of the production implementation, static inspection of the pinned
dependency (managed API surface, native export tables, declared capability set), and isolated
reproducible probes against the production store and the real native database file.

---

## 1. Summary of the finding

| Question | Answer |
| --- | --- |
| Which operations cross the managed/native boundary? | Every store operation. Writes: one `BeginWriteTransaction` + LatticeDB node/edge writes + one `Commit` per command. Reads: `BeginReadTransaction` + prepared query + `Commit` (read transaction). Open/reopen: `LatticeDatabase.Open` + schema initialization + a full native replay of the durable command log. |
| Which operations expose an actual cancellation or interruption mechanism? | Only two managed checkpoints: the write-gate wait (`await _writeGate.WaitAsync(ct)`) and a single `ThrowIfCancellationRequested()` immediately before `BeginWriteTransaction`. No operation exposes native cancellation. |
| Can cancellation interrupt active native work? | **No.** A cancellation requested while the native write transaction is open is not observed; the transaction commits atomically and the operation returns success. |
| Is native cancellation cooperative or unavailable? | **Unavailable.** `LatticeDbSharp 0.2.0` has no cancellation or interruption concept in its managed API, its native export tables, or its declared capability set. |
| Transaction / write / publication / durability implications | One native transaction per command gives all-or-nothing commit; the command log advances only after the native write returns; a pre-native cancellation leaves the previous publication and index state intact; a cancellation during the native window yields a completed operation, never a partial one. |
| Resource lifetime / disposal / concurrency implications | `Dispose()` takes the same single-writer gate and therefore *waits* for an in-flight native write rather than aborting it; disposal releases the native handle (the database file is deletable afterwards); concurrent writers are serialized by the gate. |
| Platform-specific differences | The cancellation surface is identical on all three shipped native assets (no interruption entry point anywhere). Runtime evidence exists only for `win-x64` and `linux-x64`; `osx-arm64` is a candidate RID in the pinned dependency. |
| Required upstream dependency changes | Only if interruption is demanded: a new LatticeDB transaction-interrupt entry point, a new qualified `LatticeDbSharp` release, and a separately authorized dependency upgrade. Not authorized here; not simulated. |

## 2. The managed/native boundary

### 2.1 Write path (the only path that matters for D4 invariant 5)

`LatticeCodeGraphStore.MutateAsync` (lines 336–418) is the single write funnel:

1. `ThrowIfDisposed()`.
2. `await _writeGate.WaitAsync(cancellationToken)` — **cancellation checkpoint #1 (managed wait)**.
3. `ThrowIfDisposed()`, `ThrowIfUncertain()` (the unknown-outcome quarantine).
4. Optional baseline read for a running index run.
5. `Apply(_commands, command)` — the in-memory projection is computed first so ordinary validation
   failures happen before any native write.
6. `ApplyCommandAsync(_inner, command, cancellationToken)` — the projection is advanced.
7. `TranslateNative(() => Persist(command, baseline, cancellationToken), "persist mutation")` —
   the native write (§2.2).
8. Only after the native write returns: `_commands = next`, the baseline cache is updated, and for a
   completed run the publication authority and native mirror are swapped in.

`Persist` (lines 883–982) is the native section:

```
cancellationToken.ThrowIfCancellationRequested();   // line 888 — cancellation checkpoint #2
using var txn = _database.BeginWriteTransaction();   // line 889 — the native window opens
try
{
    switch (command.Kind) { … }                     // native node/edge writes
    _faultInjector?.Invoke("before-commit");        // line 950 — internal seam (tests only)
    txn.Commit();                                   // line 954 — the irreversible commit
    _faultInjector?.Invoke("after-commit");         // line 955 — internal seam (tests only)
}
```

**There is exactly one cancellation checkpoint inside the write path, and it is before the native
transaction opens.** No cancellation token is passed to LatticeDB, and LatticeDB exposes no way to
receive one.

### 2.2 Read path

`ReadAsync` (lines 420–435) takes the same write gate (so reads serialize behind writes, as
accepted) and delegates to the in-memory projection; the native read helpers
(`ReadPairsCore`, `ReadTableCore`, `CountCore`) open a native read transaction and commit it, with
no cancellation checkpoint. A long native read is therefore not cancellable either.

### 2.3 Open / reopen path

The constructor (lines 90–135) opens the native database, initializes the schema, reads every
durable table, replays the command log into the projection (`ReplayAsync(..., cancellationToken)`),
restores occurrences and mirrors, and disposes the native database if any of that fails. It has
cancellation checkpoints around the schema/replay work (lines 105, 107, 124) and one inside the
occurrence restore loop (line 589) — all *between* native calls, never inside one. The native
`Open` and the native table reads themselves are not cancellable.

### 2.4 Public surface

15 public asynchronous members accept a `CancellationToken` (`UpsertRepositoryAsync`,
`GetRepositoryAsync`, `StoreIndexRunAsync`, `GetIndexRunAsync`, `CompleteIndexRunAsync`,
`GetLatestIndexStateAsync`, `GetLatestPublicationAsync`, `GetPublishedUnitsAsync`,
`StageIndexUnitAsync`, `StageIndexUnitDeletionAsync`, `GetNodeAsync`, `FindSymbolAsync`,
`FindNodesByQualifiedNameAsync`, `GetDeclarationsAsync`, `TraverseAsync`) plus `OpenAsync`. The
token is honoured at the two write-path checkpoints and at the open/reopen checkpoints; everywhere
else it is accepted but not observed.

## 3. Cancellation mechanisms in the pinned dependency

### 3.1 Managed API (probe `api-surface`)

`LatticeDbSharp 0.2.0` — assembly `LatticeDbSharp, Version=0.2.0.0`, informational version
`0.2.0+40bc81ce8806602d09017037ed3e13c39d342109`, SHA-256
`d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46` (102 400 bytes).

| Measurement | Value |
| --- | --- |
| Public types | 23 |
| Public members | 270 |
| Members whose name suggests cancel / interrupt / abort / terminate / shutdown | **0** |
| Members carrying a `CancellationToken` | **0** |
| `LatticeTransaction` members | `AddLabel`, `BatchInsertNodes`, `Commit`, `CreateEdge`, `CreateNode`, `DeleteEdge`, `DeleteNode`, `Dispose`, `FindEdgesByTypeProperty`, `FindNodesByLabelProperty`, `FtsSearch`, `FtsSearchFuzzy`, `GetAllNodes`, `GetEdgeProperty`, `GetIncomingEdges`, `GetLabels`, `GetNodesByLabel`, `GetOutgoingEdges`, `GetProperty`, `IsCompleted`, `Mode`, `NodeExists`, `RemoveEdgeProperty`, `RemoveLabel`, `Rollback`, `SetEdgeProperty`, `SetProperty`, `SetVector`, `TryGetEdgeProperty`, `TryGetProperty`, `VectorSearch` |

The transaction surface offers exactly three lifecycle verbs — `Commit`, `Rollback`, `Dispose` — and
no way to interrupt an in-flight transaction.

### 3.2 Native ABI (probe `native-abi-scan`, read-only static scan)

| Native asset | Format | Symbols inspected | `lattice_*` cancellation exports |
| --- | --- | --- | --- |
| `runtimes/win-x64/native/lattice.dll` (2 580 992 B, sha256 `3dec7386…`) | PE | 105 exports | **0** |
| `runtimes/linux-x64/native/liblattice.so` (14 374 704 B, sha256 `dc5b4534…`) | ELF | 191 dynamic symbols | **0** |
| `runtimes/osx-arm64/native/liblattice.dylib` (2 173 424 B, sha256 `8d99e822…`) | Mach-O | identifier scan | **0** |

The transaction/lifecycle exports present in both parseable binaries are
`lattice_begin`, `lattice_commit`, `lattice_rollback`, `lattice_close`, `lattice_open`,
`lattice_open_v2`, `lattice_open_v3`, `lattice_open_v4`. The few cancellation-pattern matches in
the ELF/Mach-O images are C-runtime and Swift-concurrency symbols (`abort`, `kill`,
`pthread_kill`, `shutdown`, `NOCANCEL`, `batchCancel`, `groupCancel`, `checkCancel`, `recancel`) —
they are runtime symbols, not LatticeDB transaction-control entry points.

### 3.3 Declared capability set (`build/LatticeDbSharp/capabilities.json`)

30 declared features; **0** cancellation-like features. The only lifecycle features are
`database.open`, `database.close`, `transaction.readWrite` (`lattice_begin`), `database.serialize`,
`database.deserializeCopy`. `database.close` is documented as having *"consuming and non-consuming
error cases"* — i.e. the close path is not a cancellation mechanism either.

### 3.4 Pinned upstream

`https://github.com/jeffhajewski/latticedb` @ `v0.15.0`, commit
`9800159e22e200f2b6888c7c6be1810adb695506`, built with Zig 0.16.0, status
`linux-x64-and-win-x64-patched-build-abi-lifecycle-durability-verified`. Supported RIDs:
`linux-x64`, `win-x64`. Candidate RIDs: `linux-arm64`, `osx-x64`, `osx-arm64`.

## 4. The three distinct cancellation classes — empirically verified

Each class was probed in isolation against the production store and the real native database file.
The probe harness is `evidence/probe/Program.cs`; receipts are in `evidence/`.

### 4.1 Class 1 — cancellation before native invocation (`pre-invocation`)

A token cancelled before the call is observed at the write-gate wait. Result: the operation throws
`OperationCanceledException`; **no native transaction is opened**; the native `HetuStage` and
`HetuUnit` tables are empty; no publication exists; the reopened store is healthy.

*Truthful claim*: cancellation before native invocation prevents the native work entirely.

### 4.2 Class 2 — cancellation of a managed wait (`managed-wait`)

While one write is blocked inside the native transaction (fault-injector seam), a second operation
with a token cancelled during the wait throws `TaskCanceledException`; the in-flight native write is
unaffected and completes; the native `HetuStage` table contains exactly one row — the
non-cancelled write.

*Truthful claim*: a waiting operation can be cancelled; the running native operation is not
disturbed.

### 4.3 Class 3 — cancellation of active native execution (`native-window`)

The token is cancelled **inside the open native write transaction**, after every native write and
before the commit (fault-injector seam, `before-commit`). Result: the cancellation is **not
observed** — the operation completes successfully and the transaction commits; the native
`HetuStage` table contains the staged row; no publication was created; the reopened store is
healthy. The same probe with the cancellation placed after the commit (`post-commit`) yields the
same outcome.

*Truthful claim*: the native window is not cancellable; a cancellation arriving there produces a
**completed** operation, never a cancelled one and never a partially applied one.

### 4.4 The genuinely-arbitrary variant

The `native-window` probe places the cancellation deterministically. The genuinely-arbitrary case
(cancellation at an unknown instant during a long native batch write) is covered in the proposed
qualification by cancelling from a concurrent thread while a large batch write is in flight; the
expected outcome is identical (the operation completes), because no checkpoint exists inside the
native window. This variant is a *qualification* item, not an entry-commission claim.

### 4.5 Unknown outcome (`commit-unknown`)

A post-commit fault produces `CodeGraphCommitOutcomeUnknownException`; the instance is quarantined
and refuses current-state claims (*"A native publication commit outcome is unknown. Refusing
current-state claims and mutations until the durable publication is corroborated by reopening the
store."*); the write had in fact landed (`HetuStage` rows = 1). This is the accepted, truthful
pattern for an unobservable native outcome and is the vocabulary S6.1 reuses.

## 5. Transaction, write, publication and durability implications

| Aspect | Finding |
| --- | --- |
| Atomicity | One native write transaction per command (`BeginWriteTransaction` → writes → `Commit`). The commit is all-or-nothing, so a cancellation in the native window cannot produce a partially applied mutation. |
| Command log | `_commands` advances only after the native write returns (`MutateAsync` line 392). A pre-native cancellation or a known native failure rebuilds the projection from the unchanged durable log (`RebuildInnerFromDurableStateAsync`). |
| Publication | Publication happens in the `complete` command inside the same native transaction (`PublishStagedRun` + `IndexState` + occurrence authority + optional mirror rebuild). A cancelled staging is therefore not published at all; a staged write that completes during a cancellation remains staged until `CompleteIndexRunAsync` publishes it. |
| Durability | The commit is the durability point. After it, the outcome must not be negated by a cancellation; the store already refuses to guess (unknown-outcome quarantine). A cancellation *before* the commit leaves the previous publication and index state intact. |
| Index state | `CodeRepositoryIndexState` is written in the same transaction as the publication, so the accepted index state is never split from the accepted publication. |
| Failure vs cancellation | A known native failure rolls back and rebuilds the projection; a cancellation before the native window does the same by never entering it. Both preserve the accepted publication. |

## 6. Resource lifetime, disposal and concurrency

| Aspect | Finding (probe evidence) |
| --- | --- |
| Write serialization | A single `SemaphoreSlim(1,1)` gate serializes every mutation and read against the store. |
| Disposal during an in-flight native write | `Dispose()` acquires the same gate, so it **waits** for the native write to finish; it never aborts native work mid-transaction (`dispose-race`: disposal was still blocked after 750 ms, the write completed, the durable state was intact and the reopened store was healthy). |
| Handle release | After disposal the native handle is released: the database file can be deleted on Windows, which fails while a handle is open (`cleanup`). |
| Concurrent cancellation + disposal | The cancellable unit is the *waiting* operation; the in-flight native operation completes; disposal follows. There is no torn state and no leaked handle. |
| Process crash | Out of scope — managed fault injection is not crash/power-loss durability (DF-4 open). The native dependency's own hard-kill recovery is an upstream property, not an Andirit claim. |

## 7. Platform-specific differences

| Platform | Native asset | Cancellation entry points | Native runtime evidence available to Andirit |
| --- | --- | --- | --- |
| `win-x64` | `lattice.dll` (PE) | none | **yes** — the probe receipts in this package are Windows x64 |
| `linux-x64` | `liblattice.so` (ELF) | none | **yes**, but it requires a genuine hosted Linux run (Q-S61-5) |
| `osx-arm64` | `liblattice.dylib` (Mach-O) | none | **no** — candidate RID in the pinned dependency; content evidence only (Q-S61-4) |
| `linux-arm64`, `osx-x64` | not shipped | n/a | no |

Because the absence of an interruption entry point is a property of the LatticeDB API itself and not
of the platform binding, the **contract** is platform-invariant; only the **runtime evidence** is
platform-bounded.

## 8. Upstream dependency requirements

| Requirement | Status |
| --- | --- |
| A managed cancellation API on `LatticeTransaction` / `LatticeDatabase` | absent |
| A native transaction-interrupt entry point (e.g. `lattice_interrupt` / `lattice_txn_cancel`) | absent |
| A capability declaration for cancellation | absent |
| A qualified `LatticeDbSharp` release exposing them | does not exist |

If the owner ever requires interruption of active native work, the dependency chain is: LatticeDB
(native interrupt support) → `LatticeDbSharp` (managed binding + capability declaration) →
`Andirit.LatticeDb` (checkpoint and truthful-outcome handling) → Andirit qualification. Each step
is a separate owner authorization; none is authorized by this commission, and the current commission
does not simulate any of them.

## 9. Bounded alternatives (proposed, not implemented here)

1. **Bounded managed cancellation (selected for S6.1).** Specify and qualify the cancellation
   contract exactly as it exists: honoured before the native window and while waiting for the gate;
   not honoured inside the native window; three truthful outcomes; the operation must be awaited.
   Cost: documentation + a dedicated native cancel-path test suite. No dependency change.
2. **Native-window observability without new production seams.** Use the existing internal
   fault-injector seam for deterministic checkpoint placement and concurrent cancellation for the
   arbitrary-timing case. Cost: test-only.
3. **Outcome-clarity for abandoned operations (deferred).** If a caller abandons a task, the store
   cannot report the outcome; the existing unknown-outcome vocabulary and the reopen-corroboration
   path remain the only truthful mechanism. Extending this to "cancelled" would require the upstream
   dependency change of §8.
4. **Not proposed.** A dependency upgrade, a new native seam in production code, a new public API
   for cancellation, or any behaviour that would let a managed timeout be reported as a native
   cancellation.

## 10. Residual risks

| Risk | Mitigation / status |
| --- | --- |
| A caller assumes a cancelled token cancels native work | contract documentation (S6.1 change 1) + the qualification suite + Q-S61-1/Q-S61-2 |
| A future regression adds a cancellation checkpoint *after* the commit, turning a durable write into a reported cancellation | the qualification suite must assert that no `OperationCanceledException` escapes after a successful commit; a regression test is part of the proposed change set |
| A future dependency bump silently changes the native API surface | the api-surface and native-abi-scan probes are re-runnable and are part of the sealed evidence; the qualification must re-run them against the pinned dependency |
| The native window is long enough that an abandoned task looks like a hang | no interruption is possible without the upstream change; the contract states that the operation must be awaited, and the unknown-outcome path covers a failed commit |
| Linux/macOS evidence asymmetry | Q-S61-4…Q-S61-6; genuine hosted Linux required for the Linux row |
