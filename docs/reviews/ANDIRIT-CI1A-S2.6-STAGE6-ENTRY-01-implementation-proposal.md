# S6.1 implementation proposal — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Status**: proposal only. This document does **not** implement S6.1; production implementation
requires the separate bounded authorization proposed in
`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-successor-commission.md`.
**Baseline**: `b5a81f81fade9807f706b4586329efe416cf1558` (production `src/` unchanged since
`eafd632…`).

---

## 1. Objective and constraints

**Objective.** Discharge the S6.1 cancellation-path obligations — M9.5 (*dedicated native cancel-path
test*), the M12.2 native case of D4 invariant 5 (*cancelled staging must not expose partial facts
or index entries*), and the cancellation part of I-04/Q2-F09 — by **specifying** the cancellation
policy and **qualifying** it natively against production storage, with no new native capability.

**Constraints carried from the governing contracts and this commission.**

| Constraint | Consequence for the design |
| --- | --- |
| The pinned dependency exposes no interruption mechanism (§3 of the feasibility assessment) | the design must not claim or simulate interruption of active native work |
| *"Cancelled staging must not expose partial facts or index entries"* (D4 invariant 5) | the guarantee to implement and prove is **non-exposure**, not interruption |
| *"A managed timeout or abandoned task must not be represented as native cancellation"* (commission §4) | the outcome vocabulary is fixed to three values; no fourth "cancelled" outcome may be synthesized |
| Stage 2 accepted behavior, refusal vocabulary and accounting semantics are preserved | no change to validation, refusal, identity, merge or accounting paths |
| No dependency upgrade is authorized | `LatticeDbSharp` stays at `0.2.0` |
| No production implementation without a separate authorization | this document proposes; the successor commission implements |

## 2. Design decision

**The smallest technically sound S6.1 change set is a documentation change plus a dedicated native
cancel-path test suite.** No behavioural change is required, because the production store already
satisfies the D4 invariant-5 requirement (non-exposure) and already reports outcomes truthfully:

- a cancellation observed before the native window prevents the native transaction entirely;
- a cancellation observed during the native window is not observed by the store, and the native
  transaction commits atomically — so no partial facts are ever exposed, and the caller is told
  *completed*;
- an unobservable native outcome (a failed commit) already uses the accepted
  `CodeGraphCommitOutcomeUnknownException` + quarantine + reopen-corroboration vocabulary.

What S6.1 is missing is not capability but **specification and native evidence**. The entry
commission's probes already demonstrate that the behaviours are correct on `win-x64`; S6.1 turns
those probes into a permanent, deterministic, reviewable test suite and publishes the contract the
suite enforces.

**Rejected alternative — a production cancellation seam.** Adding a native-operation observer hook
(or any other production instrumentation) to place cancellations deterministically *inside* a native
call was considered and rejected: the existing internal fault-injector seam (`"before-commit"` /
`"after-commit"`, already exposed to `Andirit.LatticeDb.Tests` through
`InternalsVisibleTo`) places the cancellation deterministically inside the open native transaction
without any production change, and the genuinely-arbitrary timing case is covered by concurrent
cancellation during a large native batch write.

## 3. Proposed change set

| # | Change | Exact files | Kind | Rationale |
| --- | --- | --- | --- | --- |
| **C-1** | Publish the cancellation contract on the production store: a new `<para>` in the `LatticeCodeGraphStore` class remarks describing (i) the two honoured checkpoints, (ii) the non-cancellable native window, (iii) the three truthful outcomes, (iv) the obligation to await completion, and (v) the D4 invariant-5 consequence (no partial facts, atomic completion). | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class-level `<remarks>`, lines 9–31) | documentation only | I-04 "cancellation policy specified"; prevents the false-cancellation reading |
| **C-2** | Add the same contract to the interface documentation, once, so that implementers of `ICodeGraphStore` inherit the same semantics. | `src/Andirit/ICodeGraphStore.cs` (interface `<remarks>`) | documentation only | keeps the contract at the API boundary, not only at one implementation |
| **C-3** | Add the dedicated native cancel-path test suite (the M9.5 / M12.2 instrument). | new file `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` | test addition | the qualification instrument required by M9.5 and M12.2 |
| **C-4** | Record the qualification outcome, the qualifications Q-S61-1…Q-S61-8 and the upstream dependency note in the successor commission's decision record. | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md` (created by the successor commission) | documentation | owner-visible disposition |
| — | **Not included** | — | — | dependency upgrade; new production seam; new public API; changes to the write gate, commit/atomicity semantics, validation, refusal vocabulary, identity/accounting; changes to `src/Andirit`, `src/Andirit.Abstractions`, `src/Andirit.CSharp`, `src/Andirit.TreeSitter`, `src/Andirit.Testing`; changes to CI workflows, coverage thresholds or the publish workflow; any S6.2–S6.5 work; publication or tag changes |

### 3.1 Exact API, dependency and contract impact

| Surface | Impact |
| --- | --- |
| Public API surface (`ICodeGraphStore`, `ICodeGraphStoreHealthCheck`, `ICodeOccurrenceReader`, `LatticeCodeGraphStore`) | **no signature change** — only XML documentation |
| Public behaviour | **no change** — the existing checkpoints stay exactly where they are (`_writeGate.WaitAsync(ct)` at line 339; `ThrowIfCancellationRequested()` at line 888) |
| New cancellation checkpoints | **none** — adding one after the commit would create a falsely-successful cancellation (see §4.3) and is explicitly rejected |
| Dependencies | unchanged: `LatticeDbSharp 0.2.0`; `Microsoft.CodeAnalysis.CSharp 5.9.0` and `Microsoft.Extensions.DependencyInjection.Abstractions 10.0.9` are untouched; no new package |
| Project files | unchanged: `Andirit.LatticeDb.csproj` untouched; `Andirit.LatticeDb.Tests.csproj` untouched (the SDK-style project globs `*.cs`) |
| Contract artefacts | the cancellation policy becomes part of the published API documentation; no schema, no storage format, no protocol identity changes |
| Coverage | the new tests only add coverage to `Andirit.LatticeDb`; the 70 % line/branch threshold is unaffected |

## 4. Design details

### 4.1 Cancellation propagation across the managed/native boundary

```
caller token
   │
   ├─► MutateAsync: await _writeGate.WaitAsync(ct) ........ checkpoint #1 (managed wait)  → honoured
   │        │
   │        └─► ApplyCommandAsync(_inner, command, ct) ...... managed projection apply
   │
   └─► Persist: ct.ThrowIfCancellationRequested() ........... checkpoint #2 (pre-native)    → honoured
            │
            └─► BeginWriteTransaction → native writes → Commit ................ native window
                                                                                  → NOT honoured
```

The boundary rule: **the token is honoured up to the point where the native transaction opens, and
never inside it.** The native window is bounded by one transaction and one commit, so the window is
short and deterministic — but it is not interruptible, and the contract says so.

### 4.2 Cancellation checkpoints (exact positions, unchanged)

| Checkpoint | Location | Behaviour |
| --- | --- | --- |
| write-gate wait | `LatticeCodeGraphStore.cs` line 339 (`await _writeGate.WaitAsync(cancellationToken)`) | cancels the *waiting* operation only |
| pre-native | `LatticeCodeGraphStore.cs` line 888 (`cancellationToken.ThrowIfCancellationRequested();`) | prevents the native transaction from opening |
| open/reopen | lines 105, 107, 124 and 589 (occurrence restore loop) | cancels the open/replay work between native calls |
| *(absent)* native window | between line 889 (`BeginWriteTransaction`) and line 954 (`txn.Commit()`) | cancellation is not observed; the transaction commits atomically |

### 4.3 Atomicity and commit-boundary behaviour

- **No checkpoint after the commit.** Adding one would produce a durable write reported as
  cancelled — the exact falsity the commission forbids. The proposal therefore *requires* that the
  post-commit region contains no cancellation check, and the qualification suite asserts it: after a
  successful commit the operation must return success even if the token was cancelled during the
  native window (probe: `native-window`, `post-commit`).
- **No checkpoint between the native writes and the commit** either, for the same reason: the writes
  are already in the transaction and the commit is imminent; cancelling there would abort a mutation
  whose native work is complete and whose rollback path would have to undo it — possible, but it
  would still be an *interruption* claim that the dependency cannot support for the general case.
- The single pre-native checkpoint therefore remains the only write-path checkpoint, and the
  transaction remains the atomic unit.

### 4.4 Prevention of partial or falsely successful publication

| Concern | How the design prevents it |
| --- | --- |
| partial facts after a cancelled staging | the native transaction is never opened when the cancellation is observed, and when it *is* opened it commits atomically — there is no state in which part of a staging write is visible |
| falsely successful publication | the `complete` command publishes inside the same native transaction as the index state and the occurrence authority; a failure or a cancellation before the window leaves the previous publication in place, and a failure *at* the commit is reported as an unknown outcome with quarantine |
| falsely *cancelled* publication | no cancellation check exists after the commit; a durable publication is never reported as cancelled |
| publication split from index state | both are written in one transaction (unchanged) |

### 4.5 Deterministic resource cleanup

- `Dispose()` acquires the same single-writer gate, so it never disposes the native database while a
  transaction is open; the in-flight native write completes first (verified: `dispose-race`).
- `Persist` rolls back the native transaction on any known failure (`if (!txn.IsCompleted) txn.Rollback();`)
  and rethrows; the `using` on the transaction disposes it in every path.
- The projection is rebuilt from the unchanged durable command log after a failed or cancelled
  mutation, so the managed state never diverges from the durable state.
- After disposal the native handle is released and the database file is deletable (verified:
  `cleanup`).

### 4.6 Truthful cancellation and refusal outcomes

The outcome vocabulary of a store operation is exactly:

| Outcome | When | Surfaced as |
| --- | --- | --- |
| **Cancelled** | the token was observed at checkpoint #1 or #2 | `OperationCanceledException` / `TaskCanceledException`; no native write occurred |
| **Completed** | the native transaction committed (or the operation was a read) | normal return; **never** reported as cancelled, even if the token was cancelled during the native window |
| **Outcome unknown** | the commit was attempted and its result could not be established | `CodeGraphCommitOutcomeUnknownException`; the instance is quarantined and refuses current-state claims until the durable state is corroborated by reopening |
| **Refused** | validation, schema, freshness or availability rules rejected the operation | the existing refusal vocabulary (`CodeGraphStoreException`, `CodeGraphBatchRejectedException`, `StalePublication`, `AnalysisUnavailable`, …) — unchanged |

An abandoned task or a managed timeout maps to **none** of these; the caller must await the
operation to learn its outcome. This is stated in the published contract (C-1, C-2) and is the
subject of qualification case Q-10.

### 4.7 Compatibility with the existing public API

- No signature, return type, optional-parameter or default-value change.
- No behavioural change for any existing caller: the same operations succeed, fail or refuse
  exactly as before.
- The documentation change is additive; consumers that do not read XML documentation are unaffected.
- The published package identities, versions and contents are unaffected by C-1/C-2 except for the
  XML documentation embedded in the assemblies — which is a documentation-only difference and does
  not change any public member.

### 4.8 Preservation of accepted Stage-2 semantics

Merge, identity, refusal and accounting semantics, the D4 invariants 1–4 and 6–10, the D3/D5 rules,
the occurrence namespace separation, the schema-2 storage format, the historical regression suites,
the coverage thresholds and the CI gates are all untouched. S6.1 adds tests and documentation only.

## 5. Proposed qualification-suite outline (C-3)

`tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` — deterministic cases derived from
the qualification plan (§6 of the entry record and the qualification-plan document):

| Test | Mechanism |
| --- | --- |
| `Cancellation_BeforeInvocation_LeavesDurableStateUnchanged` | pre-cancelled token; assert `OperationCanceledException`, no staged row (direct native table read), no publication, reopen healthy |
| `Cancellation_DuringNativeWindow_CompletesWithoutCancellation` | internal fault-injector seam (`before-commit`); assert the operation succeeds, the staging is durable, no publication |
| `Cancellation_BeforeCommitBoundary_CommitsAtomically` | internal fault-injector seam (`before-commit`) with a large staged unit; assert success and a complete, atomic commit |
| `Cancellation_RacingWithSuccessfulCompletion_ReportsCompleted` | token cancelled concurrently during a large native batch write; assert success, never `OperationCanceledException` |
| `Cancellation_AfterIrreversibleCommit_DoesNotReportCancellation` | internal fault-injector seam (`after-commit`); assert success and durability |
| `Reopen_AfterCancelledStaging_PreservesAcceptedPublication` | reopen; assert publication identity, snapshot identity, consistency flag and published-unit count are preserved |
| `ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite` | dispose while a native write is in flight; assert disposal waits, the write completes, the durable state is intact |
| `Disposal_ReleasesTheNativeHandle` | after disposal the database file can be deleted and the store reopened |
| `SuccessfulNonCancelledControl_PublishesAndReopens` | the non-cancelled control path |
| `NoCancellationExceptionAfterCommit_IsAsserted` | regression guard: no `OperationCanceledException` may escape after a successful commit |
| `UnknownCommitOutcome_QuarantinesTheInstance` | the accepted unknown-outcome semantics as the truthful template |

The suite must run unchanged on `win-x64` and on genuine hosted `linux-x64`; it uses only public
production APIs plus the existing internal test seam.

## 6. Acceptance criteria for the implementation commission

1. C-1…C-4 are applied and reviewed; `src/` differs from the baseline only in documentation.
2. The qualification suite passes on `win-x64` and on a genuine hosted `linux-x64` run bound to the
   sealed commit; the Linux run is independent (reviewer ≠ producer) and receipt-based.
3. The historical Stage-2 regression suites, the coverage thresholds and the CI gates are unchanged
   and green.
4. The native cancel-path evidence is sealed with a self-excluding SHA-256 manifest.
5. An independent review and an owner acceptance record close S6.1 against M9.5, M12.2, D4
   invariant 5 and the cancellation part of I-04/Q2-F09 — and against nothing else.
6. Q-S61-1…Q-S61-8 are carried forward explicitly; no interruption capability is claimed.

## 7. Effort and risk

| Item | Assessment |
| --- | --- |
| Production change | documentation only — low risk, no behavioural surface |
| Test change | one new test file — low risk; uses an existing internal seam and public APIs only |
| Dependency change | none |
| CI / workflow change | none required (the existing three-OS matrix already executes the LatticeDb suite; the genuine-Linux row must be confirmed from the matrix run rather than from a separate workflow) |
| Principal risk | a future regression that adds a post-commit cancellation checkpoint; mitigated by the dedicated regression test in §5 |
| Residual limitation | active native execution remains non-interruptible (Q-S61-1) — a dependency-level limitation that this change set deliberately does not paper over |
