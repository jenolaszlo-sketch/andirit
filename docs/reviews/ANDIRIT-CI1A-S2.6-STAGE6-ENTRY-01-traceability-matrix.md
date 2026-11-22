# S6.1 traceability matrix — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Baseline**: `b5a81f81fade9807f706b4586329efe416cf1558` (qualified implementation `eafd632…`)
**Purpose**: derive the exact S6.1 acceptance boundary from existing contracts and bind every row
to an observation or a receipt. The matrix distinguishes previously accepted behavior, managed
cancellation guarantees, deferred native cancellation guarantees, applicable native qualification
requirements, and requirements belonging to later Stage 6 work packages.

Citation anchors are byte-exact in `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/contract-reconciliation.txt`.

---

## 1. Matrix

| # | Contract item (source) | Obligation as written | Category | State at baseline | S6.1 disposition | Evidence / observation | Acceptance criterion for S6.1 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| R-1 | D4 invariant 5 — owner decision §D.4 (line 274) | "Cancelled staging must not expose partial facts or index entries." | deferred native qualification requirement | accepted as an invariant; native case deferred (M12.2) | **in** | `pre-invocation.log` (no native transaction started: `HetuStage` = 0, `HetuUnit` = 0, no publication, reopen healthy); `native-window.log` (cancellation inside the native window → the operation completes and commits atomically; the accepted publication is unchanged) | A cancelled staging leaves no staged row, no published unit and no changed publication in the native store, verified by direct native table reads and by reopen, on `win-x64` and genuine hosted `linux-x64` |
| R-2 | D4 qualification clause — owner decision §D.4 (lines 295–299) | "CI-1A must reproduce the applicable … cancellation … tests **using actual production storage and APIs** … record those limits explicitly." | applicable native qualification requirement | Stage-2 applicable subset qualified; cancellation case outstanding | **in** | probe harness references the production `src/Andirit.LatticeDb` project directly; every scenario uses the production `LatticeCodeGraphStore` and the real LatticeDB file | The cancel-path test executes against production storage and the production public API — no in-process or simulation substitute — and the interruption limit is recorded in the contract |
| R-3 | M9.5 — S2.5 qualification matrix (line 90) | "Stage 1 cancellation behavior preserved — Dedicated native cancel-path test"; evidence class `E / no`; owner Stage 6 | applicable native qualification requirement | deferred with a named owner; claiming completion is blocked | **in** (primary deliverable) | dedicated `NativeCancellationPathTests` suite proposed; current probe receipts already exercise the same behaviours against production storage | A dedicated, native, deterministic cancel-path test suite exists in `tests/Andirit.LatticeDb.Tests`, passes on the supported RIDs, and is sealed as evidence |
| R-4 | M12.2 — S2.5 qualification matrix (line 122) | D4 invariant 5, "Cancel path test native" | applicable native qualification requirement | deferred with a named owner | **in** | as R-1 | as R-1, executed as native evidence rather than as a Stage-1 in-process observation |
| R-5 | S-02 — S-01 obligation reconciliation (lines 264–268) | "All 10 D4 invariants exercised against actual production `LatticeCodeGraphStore`"; Stage 6 matrix includes "cancellation path, unknown outcome" | later-package requirement (partly in) | open — Stage 6 obligation | **partly in** — cancellation path and unknown-outcome case; the other eight invariants and the complete matrix stay with the later package | `commit-unknown.log` (unknown commit outcome reported and the instance quarantined — the accepted unknown-outcome semantics) | The cancellation-path and unknown-outcome rows are satisfied natively; no claim is made that S-02 is complete |
| R-6 | S-02 specification — CI1A specification (lines 194–197) | "… including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario. In-process deterministic interleaving is retained as a diagnostic only and is not accepted as native atomicity evidence." | later-package requirement (partly in) | open | **partly in** | `reopen.log` (publication identity, snapshot identity, consistency flag and published-unit count preserved across reopen); `dispose-race.log` (real native concurrency with the write gate) | The cancelled-publication and reopen rows carry native evidence; the interleaving probes are labelled diagnostic and are not used as native atomicity evidence |
| R-7 | I-04 — CI1A obligation register (line 216) | "Simple publication-bound maintenance and serialized policy fixed; **cancellation remains unqualified**." | applicable native qualification requirement | open — Stage 6 | **in** — the cancellation policy is specified as part of S6.1 | the contract proposed in the implementation proposal (three truthful outcomes, one cancellation checkpoint before the native window, one managed-wait checkpoint) | The cancellation policy is published in the production API documentation and the qualification suite exercises each specified outcome |
| R-8 | Q2-F09 — owner decision §G.2 (line 499); register (line 227) | "C rebuild blocks queries; no cancellation"; addressed by D4 (maintenance policy; CI-1A must qualify a production-backed rebuild path) | partly in (cancellation aspect) | open — Stage 6 | **partly in** — the cancellation aspect only | `managed-wait.log` (the serialized write gate is the mechanism that makes a waiting operation cancellable while an in-flight native write continues) | The cancellation aspect is qualified; the rebuild-blocking aspect is explicitly not claimed |
| R-9 | Q2-F12 — owner decision §G.2 (line 502); register (line 230) | "Crash/power-loss/multi-process/cross-platform not demonstrated" | later-package requirement | open — Stage 6 | **out** | — | No claim; DF-4/DF-6 remain open |
| R-10 | F-02 | Managed fault injection ≠ crash/power-loss/abrupt-kill/multi-process durability | later-package requirement | open — Stage 6 | **out** | — | No claim (Q-S61-8) |
| R-11 | DF-5 / DF-8 | DF-5 (Q2-F09 / Q2-F12 / I-04 cancellation paths); DF-8 (M9.5 cancellation path; M12.1–M12.3 full native matrix) | deferred obligations | deferred, owner Stage 6 | **partly in** | as R-3, R-7, R-8 | M9.5 and the cancellation part of I-04/Q2-F09 are discharged by S6.1 only after owner acceptance; M12.1, M12.3, the Q2-F12 part of DF-5 and the full DF-8 matrix remain deferred |
| R-12 | DF-2 / DF-3 | DF-2 (D4.5 full native fault matrix); DF-3 (full S-02 ten-invariant native matrix) | deferred obligations | deferred, owner Stage 6 | **partly in** — the cancellation case only | as R-1, R-4, R-5 | The cancellation case is qualified; the full matrices are not claimed |
| R-13 | DF-1 / DF-4 / DF-6 / DF-7 | D3 real fixtures; crash durability; full platform certification; optional cross-target scope | later-package / unauthorized | deferred | **out** | — | No claim; DF-7 stays unauthorized |
| R-14 | SQ-05 / §14 + S2.5 entry assessment §5 (lines 118–126) | Genuine hosted Linux is the Linux evidence class; WSL2/Docker-on-Windows are not native Linux evidence; no Windows result counts as Linux evidence; full platform certification remains Stage 6 | applicable native qualification requirement | Linux minimum qualified in Stage 2 | **in** for the cancel-path rows | the plan requires a genuine hosted `ubuntu-latest` execution of the cancel-path suite | The Linux row is satisfied only by a genuine hosted Linux run bound to the sealed commit; otherwise it is recorded as not executed |
| R-15 | Platform support — pinned dependency `upstream.json` / `capabilities.json` | supported RIDs `linux-x64`, `win-x64`; candidate RIDs `linux-arm64`, `osx-x64`, `osx-arm64`; `platform.windowsX64` supported | native qualification constraint | osx-arm64 runtime limitation preserved | **in** as a constraint | `native-abi-scan.txt` (candidate-only osx-arm64 asset; no supported capability declaration) | The osx-arm64 row is recorded as content-evidence-only and is never presented as runtime-qualified |
| R-16 | D4 invariants 1–4, 6–10 | Stale index, bounded scan, failed staging, supersession, reopen, contributor changes, bounded queries, concurrent publication | previously accepted behavior / later-package requirement | applicable subset qualified in Stage 2; the full native matrix is deferred (M12.1, M12.3) | **out** — preserved, not re-qualified | — | S6.1 changes nothing here; the regression suites remain the guard |

## 2. Previously accepted behavior that S6.1 must preserve

| Accepted behavior | Where it lives | S6.1 treatment |
| --- | --- | --- |
| Single-writer serialization (`SemaphoreSlim` write gate) across the whole mutation | `LatticeCodeGraphStore.MutateAsync` / `ReadAsync` | unchanged; the managed-wait cancellation row is qualified against it |
| One native write transaction per command; command log advances only after the native write returns | `LatticeCodeGraphStore.Persist` | unchanged; the commit boundary is the reference point for the cancel-path rows |
| Failed staging preserves the last accepted publication and its valid index state | `MutateAsync` catch → `RebuildInnerFromDurableStateAsync` | unchanged; the cancel-path rows assert the same property for a pre-native cancellation |
| Unknown commit outcome ⇒ `CodeGraphCommitOutcomeUnknownException`, instance quarantine, corroboration by reopen | `Persist` commit catch + `ThrowIfUncertain` | reused as the truthful vocabulary for the non-cancellable native window; re-verified by the `commit-unknown` probe |
| Publication-bound freshness; stale/unverifiable index refuses explicitly | D4 invariants 1–3; `CodeGraphPublication` | unchanged; out of S6.1 scope |
| Graph/occurrence separation, refusal vocabulary, identity/accounting semantics | D3/D5, `ICodeGraphStore`, `Andirit.Testing` | unchanged; out of S6.1 scope |

## 3. Managed cancellation guarantees (qualified by S6.1)

| Guarantee | Mechanism | Truthful outcome |
| --- | --- | --- |
| A cancellation observed before the native write transaction prevents the native transaction entirely | `ThrowIfCancellationRequested` immediately before `BeginWriteTransaction` | `OperationCanceledException`; durable state unchanged; no publication change |
| A cancellation observed while waiting for the write gate aborts the waiting operation only | `await _writeGate.WaitAsync(cancellationToken)` | `OperationCanceledException` for the waiter; the in-flight native write is unaffected and completes; only the non-cancelled write becomes durable |
| A cancellation request that arrives during the native window is not observed | no checkpoint inside the native transaction | the operation completes successfully and the transaction commits atomically; the caller is told *completed*, never *cancelled* |

## 4. Deferred native cancellation guarantees (not claimed by S6.1)

| Deferred guarantee | Why deferred | What would be required |
| --- | --- | --- |
| Interruption of an in-flight native write or query | `LatticeDbSharp 0.2.0` has no cancellation/interrupt API (0 members, 0 `lattice_*` exports, 0 capability features) | an upstream LatticeDB transaction-interrupt entry point, a new qualified LatticeDbSharp release, and a separately authorized dependency upgrade |
| Cancellation of an in-flight native *rebuild* / reopen replay | the replay path calls into native reads without checkpoints | same upstream dependency change plus a native-replay cancellation design |
| Cross-process cancellation of another process's native work | the native API exposes file locking only, no cross-process control | upstream native support; out of scope of CI-1A |

## 5. Requirements belonging to later Stage 6 work packages

S6.2 — durability (F-02 crash/power-loss/multi-process) · S6.3 — the complete D4.5 fault matrix
and the complete S-02 ten-invariant native matrix · S6.4 — real-fixture D3 · S6.5 — full platform
certification (distributions, architectures, internal locking/WAL/mmap instrumentation) ·
DF-7 — optional cross-target scope, public Find Usages, optimization (unauthorized).

## 6. Boundary statement

S6.1 covers the **cancellation path only**. It does not qualify, claim or discharge D4 invariants
1–4 and 6–10 natively, does not claim S-02 completeness, does not narrow the F-02 durability
boundary, and does not extend platform certification beyond `win-x64` and genuine hosted
`linux-x64`.
