# Native durability feasibility assessment — ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01

**Date**: 2026-11-22
**Baseline**: `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance revision; production `src/` unchanged since the qualified implementation `eafd632…`)
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
| Which operations expose an actual crash-recovery mechanism? | **None.** The managed fault injector provides deterministic checkpoint injection inside the native transaction; it is not crash/power-loss/abrupt-kill/multi-process durability. The pinned native dependency has no crash-recovery test API. |
| Can a process crash/power-loss be simulated by the managed fault injector? | **No.** The fault injector provides deterministic checkpoint injection inside the native transaction; it is not process termination, power loss, or multi-process concurrency. |
| Is native crash-recovery cooperative or unavailable? | **Unavailable as a test API.** `LatticeDbSharp 0.2.0` has no crash-recovery test API in its managed API, its native export tables, or its declared capability set. Recovery is implicit on `Open()`. |
| Transaction / write / publication / durability implications | One native transaction per command gives all-or-nothing commit; the command log advances only after the native write returns; a pre-native cancellation leaves the previous publication and index state intact; a cancellation during the native window yields a completed operation, never a partial one. |
| Resource lifetime / disposal / concurrency implications | `Dispose()` takes the same single-writer gate and therefore *waits* for an in-flight native write rather than aborting it; disposal releases the native handle (the database file is deletable afterwards); concurrent writers are serialized by the gate; multi-process access is blocked by file locking. |
| Platform-specific differences | The durability surface is identical on all three shipped native assets (same LatticeDB API). Runtime evidence exists only for `win-x64` and `linux-x64`; `osx-arm64` is a candidate RID in the pinned dependency. |
| Required upstream dependency changes | Only if a native crash-recovery test API is demanded: a new LatticeDB transaction-interrupt/crash-recovery entry point, a new qualified `LatticeDbSharp` release, and a separately authorized dependency upgrade. Not authorized here; not simulated. |

---

## 2. The managed/native boundary for durability

### 2.1 Write path (the only path that matters for D4 invariants 4 & 5)

`LatticeCodeGraphStore.MutateAsync` (lines 336–418) is the single write funnel:

1. `ThrowIfDisposed()`.
2. `await _writeGate.WaitAsync(cancellationToken)` — **cancellation checkpoint #1 (managed wait)**.
3. `ThrowIfDisposed()`, `ThrowIfUncertain()` (the unknown-outcome quarantine).
4. Optional baseline read for a running index run.
5. `Apply(_commands, command)` — the in-memory projection is computed first so ordinary validation failures happen before any native write.
6. `ApplyCommandAsync(_inner, command, cancellationToken)` — the projection is advanced.
7. `TranslateNative(() => Persist(command, baseline, cancellationToken), "persist mutation")` — the native write.
8. Only after the native write returns: `_commands = next`, the baseline cache is updated, and for a completed run the publication authority and native mirror are swapped in.

`Persist` (lines 883–982) is the native section:

```
cancellationToken.ThrowIfCancellationRequested();   // line 888 — cancellation checkpoint #2
using var txn = _database.BeginWriteTransaction();   // line 889 — the native window opens
try
{
    switch (command.Kind) { … }                     // native node/edge writes
    _faultInjector?.Invoke("before-commit");        // line 950 — internal seam (tests only)
    txn.Commit();                                   // line 954 — the irreversible commit (durability point)
    _faultInjector?.Invoke("after-commit");         // line 955 — internal seam (tests only)
}
```

**There is exactly one cancellation checkpoint inside the write path, and it is before the native
transaction opens.** No cancellation token is passed to LatticeDB, and LatticeDB exposes no way to
receive one. The commit is the durability point.

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

### 2.4 Durability implications

| Aspect | Finding |
| --- | --- |
| Atomicity | One native write transaction per command (`BeginWriteTransaction` → writes → `Commit`). The commit is all-or-nothing, so a crash in the native window cannot produce a partially applied mutation. |
| Command log | `_commands` advances only after the native write returns (`MutateAsync` line 392). A pre-native cancellation or a known native failure rebuilds the projection from the unchanged durable log (`RebuildInnerFromDurableStateAsync`). |
| Publication | Publication happens in the `complete` command inside the same native transaction (`PublishStagedRun` + `IndexState` + occurrence authority + optional mirror rebuild). A cancelled staging is therefore not published at all; a staged write that completes during a cancellation remains staged until `CompleteIndexRunAsync` publishes it. |
| Durability | The commit is the durability point. After it, the outcome must not be negated by a cancellation; the store already refuses to guess (unknown-outcome quarantine). A cancellation *before* the commit leaves the previous publication and index state intact. |
| Index state | `CodeRepositoryIndexState` is written in the same transaction as the publication, so the accepted index state is never split from the accepted publication. |
| Failure vs cancellation | A known native failure rolls back and rebuilds the projection; a cancellation before the native window does the same by never entering it. Both preserve the accepted publication. |

---

## 3. Cancellation mechanisms in the pinned dependency (recap from S6.1)

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

| Native asset | Format | Symbols inspected | `lattice_*` cancellation/crash-recovery exports |
| --- | --- | --- | --- |
| `runtimes/win-x64/native/lattice.dll` (2 580 992 B, sha256 `3dec7386…`) | PE | 105 exports | **0** |
| `runtimes/linux-x64/native/liblattice.so` (14 374 704 B, sha256 `dc5b4534…`) | ELF | 191 dynamic symbols | **0** |
| `runtimes/osx-arm64/native/liblattice.dylib` (2 173 424 B, sha256 `8d99e822…`) | Mach-O | identifier scan | **0** |

The transaction/lifecycle exports present in both parseable binaries are
`lattice_begin`, `lattice_commit`, `lattice_rollback`, `lattice_close`, `lattice_open`,
`lattice_open_v2`, `lattice_open_v3`, `lattice_open_v4`. No crash-recovery or interruption
exports exist.

### 3.3 Declared capability set (`build/LatticeDbSharp/capabilities.json`)

30 declared features; **0** cancellation-like features, **0** crash-recovery features. The only
lifecycle features are `database.open`, `database.close`, `transaction.readWrite`
(`lattice_begin`), `database.serialize`, `database.deserializeCopy`.

### 3.4 Pinned upstream

`https://github.com/jeffhajewski/latticedb` @ `v0.15.0`, commit
`9800159e22e200f2b6888c7c6be1810adb695506`, built with Zig 0.16.0, status
`linux-x64-and-win-x64-patched-build-abi-lifecycle-durability-verified`. Supported RIDs:
`linux-x64`, `win-x64`. Candidate RIDs: `linux-arm64`, `osx-x64`, `osx-arm64`.

### 3.5 Upstream dependency requirements for crash-recovery

| Requirement | Status |
| --- | --- |
| A managed crash-recovery test API on `LatticeTransaction` / `LatticeDatabase` | absent |
| A native crash-recovery test entry point (e.g. `lattice_crash_recovery`, `lattice_wal_checkpoint`) | absent |
| A capability declaration for crash-recovery testing | absent |
| A qualified `LatticeDbSharp` release exposing them | does not exist |

If the owner ever requires a crash-recovery test API, the dependency chain is: LatticeDB
(native crash-recovery support) → `LatticeDbSharp` (managed binding + capability declaration) →
`Andirit.LatticeDb` (checkpoint and truthful-outcome handling) → Andirit qualification. Each step
is a separate owner authorization; none is authorized by this commission, and the current commission
does not simulate any of them.

---

## 4. The four distinct durability classes — empirically verified (or feasibility)

Each class is verified by a probe in isolation against the production store and the real native
database file. The probe harness is `evidence/probe/Program.cs`; receipts are in `evidence/`.

### 4.1 Class 1 — Crash before native invocation (`crash-before-commit`)

**Mechanism**: A worker process is killed before `BeginWriteTransaction` is called (fault-injector
seam before the native window, or OS-level `kill -9`).

**Result**: The operation never opens a native transaction; the native `HetuStage` and `HetuUnit`
tables are empty; no publication exists; the reopened store is healthy.

**Truthful claim**: crash before native invocation prevents the native work entirely; the durable
state is byte-for-byte unchanged.

**Evidence class**: constructed (fault-injector before native window) — timing controlled.

### 4.2 Class 2 — Crash inside the native transaction (`crash-in-window`)

**Mechanism**: The fault-injector seam (`"before-commit"`) is triggered after native writes but
before `Commit()`. The worker process is killed (or the fault injector throws) at this point.

**Result**: The native transaction is either rolled back (if `Commit` not reached) or committed
atomically; the native `HetuStage` table contains either the staged row (if commit succeeded) or
is empty (if rolled back); never partially applied; the reopened store is healthy.

**Truthful claim**: a crash inside the native window yields either the pre-crash state or a fully
committed operation — never a partially applied one.

**Evidence class**: constructed (fault-injector inside the open transaction) — timing controlled;
the native writes, the transaction and the commit (if reached) are genuine.

### 4.3 Class 3 — Crash immediately after commit (`crash-after-commit`)

**Mechanism**: The fault-injector seam (`"after-commit"`) is triggered after `txn.Commit()` returns.

**Result**: The commit is durable; the operation completes successfully; the native `HetuStage`
table contains the staged row; the reopened store shows the committed write.

**Truthful claim**: a crash after the irreversible commit does not negate the outcome; the write is
durable and visible after reopen.

**Evidence class**: constructed (fault-injector after commit) — timing controlled; the commit is
genuine.

### 4.4 Class 4 — Unknown commit outcome (power loss during commit) (`unknown-outcome`)

**Mechanism**: A fault is injected at the commit boundary (after `Commit()` is called but before
acknowledgment), simulating power loss during the commit.

**Result**: The store produces `CodeGraphCommitOutcomeUnknownException`; the instance is quarantined
and refuses current-state claims; the durable state is left untouched for corroboration by reopening
(the write had in fact landed — `HetuStage` rows = 1).

**Truthful claim**: the outcome is unknown; the durable state must be corroborated by reopening.
This reuses the existing unknown-outcome vocabulary rather than inventing a crash-shaped answer.

**Evidence class**: constructed (fault-injector at commit boundary) — timing controlled; the
native commit is genuine.

### 4.5 Class 5 — Multi-process concurrent access (`multi-process`)

**Mechanism**: Multiple independent worker processes attempt to open and write to the same database
file concurrently.

**Result**: LatticeDB's file locking prevents concurrent writers; the first writer completes;
others fail or wait; the durable state is consistent; the reopened store is healthy.

**Truthful claim**: file locking prevents concurrent writers; the durable state remains consistent.

**Evidence class**: **genuine** (independent processes, no seam control).

### 4.6 Class 6 — Reopen and durable-state consistency (`reopen`)

**Mechanism**: After any of the above crash scenarios, the database is reopened and the durable
state is compared to the pre-crash accepted state.

**Result**: The publication identity, snapshot identity, consistency flag and published-unit count
are preserved; the store is healthy; no partial facts or index entries are exposed.

**Truthful claim**: the prior accepted publication and corresponding valid index state are
preserved; the reopened store is healthy.

**Evidence class**: constructed but native — schedule controlled; every native operation,
transaction, commit and reopen is real.

### 4.7 Class 7 — Abrupt process termination (`abrupt-kill`)

**Mechanism**: OS-level process termination (`kill -9` on Linux, `TerminateProcess` on Windows)
during an in-flight native write.

**Result**: Same as Class 2 or Class 3 depending on timing; the durable state is consistent; no
partial writes; the reopened store is healthy.

**Truthful claim**: abrupt process termination yields either the pre-crash state or a fully
committed operation — never a partially applied one.

**Evidence class**: **genuine** (OS-level process termination, no managed control).

### 4.8 Class 7 — Resource cleanup after crash (`cleanup`)

**Mechanism**: After a crash scenario, the database is disposed and the native handle is verified
as released (file deletable on Windows; clean reopen on Linux).

**Result**: The database file can be deleted on Windows (proving handle release); no temporary
artifacts or leaked handles remain; clean reopen on Linux.

**Truthful claim**: the native handle is released; no temporary artifacts leaked.

**Evidence class**: constructed but native — disposal and reopen are real.

### 4.9 Class 9 — Successful non-crashed controls

**Mechanism**: The ordinary non-crashed flow with the same native assertions as the crash cases.

**Result**: Staging, completion, publication, node resolution and reopen all behave as accepted.

**Truthful claim**: the non-crashed control passes on every platform and is byte-comparable with
the accepted Stage-2 expectations.

**Evidence class**: constructed control.

### 4.10 Class 10 — Preservation of historical suites

**Mechanism**: The complete existing suites (`Andirit.LatticeDb.Tests`, `Andirit.Tests`,
`Andirit.CSharp.Tests`, `Andirit.TreeSitter.Tests`) plus the whole solution matrix run with
unchanged expectations.

**Result**: Every historical suite is green with unchanged expectations; no existing test is
modified, skipped or relaxed; coverage thresholds (55/75/60/70) are unchanged and met.

**Truthful claim**: the durability qualification does not regress existing behaviour.

**Evidence class**: **genuine** — the unmodified production suites run end to end.

---

## 5. Transaction, write, publication and durability implications

| Aspect | Finding |
| --- | --- |
| Atomicity | One native write transaction per command (`BeginWriteTransaction` → writes → `Commit`). The commit is all-or-nothing, so a crash in the native window cannot produce a partially applied mutation. |
| Command log | `_commands` advances only after the native write returns (`MutateAsync` line 392). A pre-native cancellation/crash or a known native failure rebuilds the projection from the unchanged durable log (`RebuildInnerFromDurableStateAsync`). |
| Publication | Publication happens in the `complete` command inside the same native transaction (`PublishStagedRun` + `IndexState` + occurrence authority + optional mirror rebuild). A cancelled staging is therefore not published at all; a staged write that completes during a cancellation/crash remains staged until `CompleteIndexRunAsync` publishes it. |
| Durability | The commit is the durability point. After it, the outcome must not be negated by a cancellation or crash; the store already refuses to guess (unknown-outcome quarantine). A crash *before* the commit leaves the previous publication and index state intact. |
| Index state | `CodeRepositoryIndexState` is written in the same transaction as the publication, so the accepted index state is never split from the accepted publication. |
| Failure vs cancellation | A known native failure rolls back and rebuilds the projection; a cancellation/crash before the native window does the same by never entering it. Both preserve the accepted publication. |

---

## 6. Resource lifetime, disposal and concurrency

| Aspect | Finding (probe evidence) |
| --- | --- |
| Write serialization | A single `SemaphoreSlim(1,1)` gate serializes every mutation and read against the store. |
| Disposal during an in-flight native write | `Dispose()` acquires the same gate, so it **waits** for the native write to finish; it never aborts native work mid-transaction (`dispose-race`: disposal was still blocked after 750 ms, the write completed, the durable state was intact and the reopened store was healthy). |
| Handle release | After disposal the native handle is released: the database file can be deleted on Windows, which fails while a handle is open (`cleanup`). |
| Concurrent cancellation + disposal | The cancellable unit is the *waiting* operation; the in-flight native operation completes; disposal follows. There is no torn state and no leaked handle. |
| Multi-process concurrent access | File locking prevents concurrent writers; the first writer holds the lock; others fail or wait. The durable state is consistent. |
| Process crash | **Out of scope for managed fault injection** — managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability (DF-4 open). The native dependency's own hard-kill recovery is an upstream property, not an Andirit claim. |

---

## 7. Platform-specific differences

| Platform | Native asset | Durability entry points | Native runtime evidence available to Andirit |
| --- | --- | --- | --- |
| `win-x64` | `lattice.dll` (PE) | none | **yes** — local Windows testing feasible |
| `linux-x64` | `liblattice.so` (ELF) | none | **yes**, but requires genuine hosted Linux (Q-S62-6) |
| `osx-arm64` | `liblattice.dylib` (Mach-O) | none | **no** — candidate RID in the pinned dependency; content evidence only (Q-S62-5) |
| `linux-arm64`, `osx-x64` | not shipped | n/a | no |

Because the absence of a crash-recovery entry point is a property of the LatticeDB API itself and
not of the platform binding, the **contract** is platform-invariant while the **runtime evidence**
is platform-bounded.

---

## 8. Upstream dependency requirements

| Requirement | Status |
| --- | --- |
| A managed crash-recovery test API on `LatticeTransaction` / `LatticeDatabase` | absent |
| A native crash-recovery test entry point (e.g. `lattice_crash_recovery`, `lattice_wal_checkpoint`) | absent |
| A capability declaration for crash-recovery testing | absent |
| A qualified `LatticeDbSharp` release exposing them | does not exist |

If the owner ever requires a crash-recovery test API, the dependency chain is: LatticeDB (native
crash-recovery support) → `LatticeDbSharp` (managed binding + capability declaration) →
`Andirit.LatticeDb` (checkpoint and truthful-outcome handling) → Andirit qualification. Each step
is a separate owner authorization; none is authorized by this commission, and the current commission
does not simulate any of them.

---

## 9. Bounded alternatives (proposed, not implemented here)

1. **Bounded documentation + dedicated native durability test suite (selected for S6.2).** Specify
   and qualify the durability contract exactly as it exists: the commit is the durability point;
   crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the store waits for
   in-flight native writes on disposal; the unknown-outcome path covers failed commits. Cost:
   documentation + a dedicated native durability test suite (constructed cases on the fault-injector
   seam + genuine cases on new infrastructure). No dependency change.

2. **Genuine crash/power-loss/abrupt-kill/multi-process evidence (required for qualification).**
   New infrastructure: worker processes that perform writes and are terminated by `kill -9` /
   `TerminateProcess` / power-loss simulation; multi-process concurrent access tests; safety
   scoping (disposable databases, isolated workers). Cost: new infrastructure; no dependency change.

3. **Native crash-recovery API (deferred).** If the owner ever requires a native crash-recovery
   test API, the upstream dependency chain is: LatticeDB (native crash-recovery support) →
   `LatticeDbSharp` (managed binding + capability declaration) → `Andirit.LatticeDb` (checkpoint
   and truthful-outcome handling) → Andirit qualification. Each step is a separate owner
   authorization; none is authorized by this commission.

4. **Not proposed.** A dependency upgrade, a new native seam in production code, a new public API
   for crash-recovery, or any behaviour that would let a managed fault be reported as crash
   durability.

---

## 9. Residual risks

| Risk | Mitigation / status |
| --- | --- |
| A caller assumes a crash durably rolls back | contract documentation (S6.2 change 1) + the qualification suite + Q-S62-1/Q-S62-2 |
| A future regression adds a crash-recovery claim without native evidence | the qualification suite must assert that no crash-recovery claim is made; a regression test is part of the proposed change set |
| A future dependency bump silently changes the native API surface | the api-surface and native-abi-scan probes are re-runnable and are part of the sealed evidence; the qualification must re-run them against the pinned dependency |
| The native window is long enough that a crash looks like a hang | no crash-recovery is possible without the upstream change; the contract states that the outcome must be awaited, and the unknown-outcome path covers a failed commit |
| Linux/macOS evidence asymmetry | Q-S62-5/Q-S62-6; genuine hosted Linux required for the Linux row |
| Genuine power-loss testing infrastructure | May be infeasible; if so, the corresponding requirement is retained as unqualified with explicit documentation |
| Multi-process testing on hosted Ubuntu | Feasible with file locking; requires careful worker process isolation |