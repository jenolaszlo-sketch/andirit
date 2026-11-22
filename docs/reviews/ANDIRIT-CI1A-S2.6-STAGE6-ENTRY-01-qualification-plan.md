# Independent qualification plan — ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Status**: plan only; execution belongs to the successor commission
(`ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01`, see the successor-commission document).
**Baseline for the plan**: `b5a81f81fade9807f706b4586329efe416cf1558`; the executed plan must be
bound to the commit that carries the S6.1 change set.

---

## 1. Rules of the plan

1. **Production storage only.** Every case runs against the production
   `Andirit.LatticeDb.LatticeCodeGraphStore` over a real LatticeDB file. No in-process substitute,
   no mock, no simulation of the native engine.
2. **Native observation.** Durable-state assertions are made by reading the native tables directly
   (`HetuStage`, `HetuUnit`, `HetuRun`, `HetuIndexState`) with `LatticeDbSharp`, in addition to the
   public API, so a case cannot pass on a managed projection alone.
3. **Constructed and genuine evidence are labelled separately** (§3).
4. **Platform honesty.** `win-x64` evidence is produced locally; `linux-x64` evidence requires a
   genuine hosted Linux run (Q-S61-5); macOS is not a native-evidence platform for the LatticeDB
   store (Q-S61-4/Q-S61-6).
5. **No evidence substitution.** A Windows result is never presented as Linux evidence; a
   deterministic interleaving probe is diagnostic only and is never presented as native atomicity
   evidence (CI1A specification, line 194–197).
6. **Independence.** The reviewer is not the producer; the reviewer re-derives the expectations from
   the contract, not from the producer's test code (owner decision §D.4; entry proposal §3).

## 2. Case matrix

| ID | Case | Expected outcome | Observation mechanism | Platform | Evidence format | Acceptance criterion |
| --- | --- | --- | --- | --- | --- | --- |
| **Q-01** | Cancellation before invocation | `OperationCanceledException`; **no native transaction opens**; `HetuStage` = 0, `HetuUnit` = 0; no publication; reopened store healthy | pre-cancelled `CancellationTokenSource`; native table read after disposal; reopen + health | win-x64 + genuine linux-x64 | test receipt (test name, exit, assertion list), native table counts, reopen log | The cancelled call throws `OperationCanceledException` **and** the native durable state is unchanged, verified natively and after reopen |
| **Q-02** | Cancellation during active native work | The cancellation is **not observed**: the operation **completes successfully**; the native write is durable; the accepted publication is unchanged; no partial state | cancellation issued from a concurrent thread while a large native batch write is in flight (genuine, untimed arrival); native table read; reopen | win-x64 + genuine linux-x64 | test receipt, concurrent-cancel trace (cancel timestamp vs write completion), native table counts | The operation returns success (never `OperationCanceledException`), the native commit is complete, and the durable state contains the whole staged unit — **never** presented as a native cancellation |
| **Q-03** | Cancellation before the commit boundary | Deterministic: cancellation placed **inside the open native transaction, after the native writes and before the commit** → **not observed**; the commit proceeds; the operation completes; the state is atomic | internal fault-injector seam (`"before-commit"`), reached through `InternalsVisibleTo("Andirit.LatticeDb.Tests")`; native table read; reopen | win-x64 + genuine linux-x64 | test receipt, seam-trigger record, native table counts, reopen log | The cancellation request is proven to have occurred inside the native window, yet the operation completes and the durable state is complete and consistent — no partial application |
| **Q-04** | Cancellation racing with successful completion | The operation completes successfully; **no** cancellation is reported; the durable state is complete | token cancelled concurrently during a large native batch write (repeated N times to exercise the race) | win-x64 + genuine linux-x64 | test receipt, repetition count, native table counts | Across all repetitions the outcome is *completed*; no iteration reports a cancellation after a successful commit |
| **Q-05** | Cancellation after an irreversible commit | The operation completes successfully; the write is durable; the store is usable and reopenable | internal fault-injector seam (`"after-commit"`) cancels the token after `txn.Commit()` returned; native table read; reopen | win-x64 + genuine linux-x64 | test receipt, seam-trigger record, native table counts, reopen log | The operation reports success (not cancellation) and the committed write is durable and visible after reopen |
| **Q-06** | Reopen and durable-state consistency | After a cancelled (pre-native) staging **and** after a completed-but-cancelled-request staging: publication identity, snapshot identity, consistency flag and published-unit count are preserved; the store is healthy | close and reopen the store; compare the publication tuple and the published-unit set; native table read | win-x64 + genuine linux-x64 | before/after comparison receipt, native table counts, health output | The reopened store exposes exactly the same accepted publication as before the cancelled operation, and the pre-existing publication is never replaced by a cancelled operation |
| **Q-07** | Concurrent cancellation and disposal | Disposal **waits** for the in-flight native write; the write completes; the durable state is intact; the store reopens healthy | dispose concurrently with a native write held inside the transaction; observe that disposal does not complete first; native table read; reopen | win-x64 + genuine linux-x64 | timing receipt (write entered / dispose requested / write completed / dispose completed), native table counts, reopen log | Disposal never interrupts the native transaction, the write completes, and the durable state afterwards is intact and reopenable |
| **Q-08** | Resource cleanup | After disposal the native handle is released: the database file can be deleted (Windows fails while a handle is open) and the store can be reopened | delete the database file after disposal; reopen a copy | win-x64 (the file-deletion assertion is Windows-specific); linux-x64 asserts clean reopen and no leaked files | file-deletion receipt, reopen log, process exit code | The file is deletable after disposal and no native handle or temporary artifact remains |
| **Q-09** | Successful non-cancelled controls | The ordinary path is unaffected: staging, completion, publication, node resolution and reopen all behave as accepted | the full non-cancelled flow, with the same native assertions as Q-01/Q-06 | win-x64 + genuine linux-x64 | control receipt, native table counts, publication tuple | The non-cancelled control passes on every platform and is byte-comparable with the accepted Stage-2 expectations |
| **Q-10** | Preservation of the historical Stage-2 regressions | The complete existing suites keep passing with unchanged counts, coverage thresholds and CI gates | the unmodified `Andirit.LatticeDb.Tests`, `Andirit.Tests`, `Andirit.CSharp.Tests`, `Andirit.TreeSitter.Tests` suites, plus the whole solution matrix | win-x64 + genuine linux-x64 + macos-latest (managed suites only) | suite receipts (per-project counts), coverage reports, CI run receipts | Every historical suite is green with unchanged expectations; no existing test is modified, skipped or relaxed; coverage thresholds (55 / 75 / 60 / 70) are unchanged and met |
| **Q-11** | Unknown outcome (the truthful template) | A failed commit is reported as `CodeGraphCommitOutcomeUnknownException`; the instance is quarantined and refuses current-state claims; the durable state is corroborable by reopen | internal fault-injector seam throwing after the commit | win-x64 + genuine linux-x64 | test receipt, refusal message, native table counts, reopen corroboration | The accepted unknown-outcome semantics are unchanged by S6.1 and are used as the vocabulary for the non-cancellable native window |
| **Q-12** | Dependency-surface re-verification | The pinned dependency still exposes no cancellation/interruption entry point | the api-surface and native-abi-scan probes, re-run against the pinned dependency of the sealed commit | any (static) | probe receipts (managed member census, export tables, capability set) | The census is unchanged (0 cancellation-like public members, 0 `CancellationToken` members, 0 `lattice_*` cancellation exports, 0 cancellation-like capability features); any change is an immediate stop condition |

## 3. Constructed controls versus genuinely exercised native behaviour

| Case | Class | Explanation |
| --- | --- | --- |
| Q-01 | **constructed** (the cancellation is placed deliberately before the call) | the *native work* is genuine — no native transaction is opened at all — but the timing is controlled |
| Q-02 | **genuine** | the cancellation arrives at an arbitrary moment during an in-flight native batch write; no seam controls the timing |
| Q-03 | **constructed** | the cancellation is placed deterministically inside the open native transaction via the internal seam; the native writes, the transaction and the commit are genuine |
| Q-04 | **genuine** | the cancellation races the operation without seam control; repeated to exercise the race window |
| Q-05 | **constructed** | the cancellation is placed deterministically after `txn.Commit()` returned; the commit is genuine |
| Q-06, Q-07, Q-08, Q-09, Q-11 | **constructed but native** | the schedule is controlled; every native operation, transaction and commit is real |
| Q-10 | **genuine** | the unmodified production suites run end to end |
| Q-12 | **static** | no native execution; a read-only API and ABI census |

**Rule for the report.** A constructed case may establish *what happens at a specific point of the
native timeline*; it may not be presented as an arbitrary-timing guarantee. The arbitrary-timing
claim rests on Q-02/Q-04 only. Conversely, a genuine case may not be used to claim a specific
internal placement.

## 4. Platform requirements

| Case | win-x64 | linux-x64 | macos (managed only) |
| --- | --- | --- | --- |
| Q-01 … Q-09, Q-11 | required | **required (genuine hosted Linux)** | not applicable — the LatticeDB native store has no supported macOS runtime in the pinned dependency (Q-S61-4) |
| Q-10 | required | required | required (the existing CI matrix covers the managed suites on all three platforms) |
| Q-12 | optional (static) | optional (static) | optional (static) |
| File-deletion assertion in Q-08 | required (Windows-specific semantics) | adapted (clean reopen + no leaked files) | not applicable |

A Linux result produced in WSL2, Docker-on-Windows or any other Windows-hosted compatibility
environment **must not** be recorded as Linux evidence. A macOS run of the managed suites is **not**
macOS evidence for the native store.

## 5. Evidence package format

Per the repository evidence convention (self-excluding SHA-256 manifest; `.gitattributes` with
`* -text` / `**/* -text`; `.gitignore` with `!*` / `!**/`):

| Artifact | Content |
| --- | --- |
| `qualification-plan.md` | this plan, as executed |
| `matrix.md` | one row per case with the expected outcome, the observed outcome and PASS/QUAL/FAIL |
| `case-receipts/` | the raw per-case receipts (test output, native table counts, timing traces, reopen logs) |
| `platform-receipts/` | the Linux and Windows run receipts (run identity, commit binding, SDK/OS identity) |
| `native-api-census.txt` | the re-run api-surface and native-abi-scan receipts |
| `production-delta.txt` | the exact diff of `src/`, `tests/` and `.github/` against the baseline |
| `manifest.json` | the self-excluding SHA-256 manifest binding the package to the sealed commit |

## 6. Acceptance and stop conditions

**Acceptance** requires all of: Q-01…Q-12 PASS or QUAL with a recorded reason; the genuine
`linux-x64` row executed on a genuine hosted Linux runner bound to the sealed commit; Q-10 green
with unchanged suites and thresholds; Q-12 unchanged; the production delta limited to the approved
documentation and test additions; and an independent reviewer who re-derived the expectations from
the contract.

**Immediate stop conditions** (owner review required, no further work):

1. The pinned dependency turns out to expose a cancellation/interruption mechanism (Q-12 changes) —
   the design must then be re-derived, because an interruption capability would change the S6.1
   boundary.
2. Any case shows a *partial* native application (a partially applied staging, a split publication,
   a durable write reported as cancelled).
3. A cancellation is observed **after** a successful commit (a false cancellation).
4. A staged or published fact becomes visible while a cancellation is pending (a D4 invariant-5
   exposure).
5. Disposal interrupts an in-flight native transaction, or the native handle is not released.
6. The historical suites change count, expectation, coverage threshold or CI gate.
7. Any non-Linux result is proposed as Linux evidence, or a compatibility environment is proposed
   as native Linux execution.

## 7. Independence

The qualification must be executed by a session other than the implementation session, or reviewed
by an independent reviewer against the sealed evidence, mirroring the S2.x pattern (entry proposal
§3): *"Per-package independent qualification (reviewer session ≠ producer session; receipt-based
independence where hosted runs are used) and owner acceptance."* The reviewer re-derives the
expected outcomes from the contract excerpts (`contract-reconciliation.txt`) and from the production
source, not from the producer's test assertions.
