# Independent qualification plan — ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01

**Date**: 2026-11-24
**Status**: plan only; execution belongs to the implementation commission
(`ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01`, see the implementation-proposal document).
**Baseline for the plan**: `92bf73a61794fa3bccad53d23dd7e65c51807cd5`; the executed plan must be
bound to the commit that carries the S6.2 change set.

---

## 1. Rules of the plan

1. **Production storage only.** Every case runs against the production
   `Andirit.LatticeDb.LatticeCodeGraphStore` over a real LatticeDB file. No in-process substitute,
   no mock, no simulation of the native engine.
2. **Native observation.** Durable-state assertions are made by reading the native tables directly
   (`HetuStage`, `HetuUnit`, `HetuRun`, `HetuIndexState`) with `LatticeDbSharp`, in addition to the
   public API, so a case cannot pass on the managed projection alone.
3. **Constructed and genuine evidence are labelled separately** (§3).
4. **Platform honesty.** `win-x64` evidence is produced locally; `linux-x64` evidence requires a
   genuine hosted Linux run (Q-S62-6); macOS is not a native-evidence platform for the LatticeDB
   store (Q-S62-5/Q-S62-7).
5. **No evidence substitution.** A Windows result is never presented as Linux evidence; a
   deterministic interleaving probe is diagnostic only and is never presented as native atomicity
   evidence (CI1A specification, line 194–197).
6. **Independence.** The reviewer is not the producer; the reviewer re-derives the expectations from
   the contract, not from the producer's test code (owner decision §D.4; entry proposal §3).

---

## 2. Case matrix

| ID | Case | Expected outcome | Observation mechanism | Platform | Evidence format | Acceptance criterion |
| --- | --- | --- | --- | --- | --- | --- |
| **D-01** | Crash before invocation | No native transaction opens; `HetuStage` = 0, `HetuUnit` = 0; no publication; reopened store healthy | pre-cancelled `CancellationTokenSource`; native table read after disposal; reopen + health | win-x64 + genuine linux-x64 | test receipt (test name, exit, assertion list), native table counts, reopen log | The crashed worker leaves no native trace; the durable state is byte-for-byte unchanged, verified natively and after reopen |
| **D-02** | Crash during active native work | The native transaction is either rolled back or committed atomically; the native commit is complete; the accepted publication is unchanged; no partial state | crash issued from a concurrent thread while a large native batch write is in flight (genuine, untimed arrival); native table read; reopen | win-x64 + genuine linux-x64 | test receipt, concurrent-crash trace (crash timestamp vs write completion), native table counts | The native state is either pre-crash or fully committed — **never** presented as a native crash |
| **D-03** | Crash immediately after commit | The commit is durable; the write is visible; the store is usable and reopenable | fault-injector seam (`"after-commit"`) crashes after `txn.Commit()` returned; native table read; reopen | win-x64 + genuine linux-x64 | test receipt, seam-trigger record, native table counts, reopen log | The write is durable and visible after reopen; no outcome negation |
| **D-04** | Unknown commit outcome (power loss during commit) | `CodeGraphCommitOutcomeUnknownException`; instance quarantined and refuses current-state claims; durable state corroborable by reopen | fault-injector seam throwing at commit boundary | win-x64 + genuine linux-x64 | test receipt, refusal message, native table counts, reopen corroboration | The accepted unknown-outcome semantics are unchanged and are used as the vocabulary for the non-cancellable native window |
| **D-05** | Multiple processes concurrent access | The operation completes successfully; **no** writer starvation; the durable state is consistent | multiple independent worker processes attempt simultaneous `Open`/`Write`/`Commit` | win-x64 + genuine linux-x64 | test receipt, file-lock evidence, native table counts | Only one writer holds the database at a time; the durable state is consistent after all complete |
| **D-06** | Reopen and durable-state consistency | After a crashed (pre-native) staging **and** after a completed-but-crashed staging: publication identity, snapshot identity, consistency flag and published-unit count are preserved; the store is healthy | close and reopen the store; compare the publication tuple and the published-unit set; native table read | win-x64 + genuine linux-x64 | before/after comparison receipt, native table counts, health output | The reopened store exposes exactly the same accepted publication as before the crashed operation |
| **D-07** | Abrupt process termination (`kill -9` / `TerminateProcess`) | The process is killed mid-transaction; durable state is consistent; reopen healthy | OS-level process termination during a native write | win-x64 + genuine linux-x64 | test receipt, termination mechanism, native table counts, reopen log | Durable state consistent; no partial writes |
| **D-08** | Resource cleanup | After disposal the native handle is released: the database file can be deleted (Windows fails while a handle is open) and the store can be reopened | delete the database file after disposal; reopen a copy | win-x64 (the file-deletion assertion is Windows-specific); linux-x64 asserts clean reopen and no leaked files | file-deletion receipt, reopen log, process exit code | The file is deletable after disposal and no native handle or temporary artifact remains |
| **D-09** | Successful non-crashed controls | The ordinary path is unaffected: staging, completion, publication, node resolution and reopen all behave as accepted | the full non-crashed flow, with the same native assertions as D-01/D-06 | win-x64 + genuine linux-x64 | control receipt, native table counts, publication tuple | The non-crashed control passes on every platform and is byte-comparable with the accepted Stage-2 expectations |
| **D-10** | Preservation of the historical S6.1 qualifications | The complete existing suites keep passing with unchanged counts, coverage thresholds and CI gates | the unmodified `Andirit.LatticeDb.Tests`, `Andirit.Tests`, `Andirit.CSharp.Tests`, `Andirit.TreeSitter.Tests` suites, plus the whole solution matrix | win-x64 + genuine linux-x64 + macos-latest (managed suites only) | suite receipts (per-project counts), coverage reports, CI run receipts | Every historical suite is green with unchanged expectations; no existing test is modified, skipped or relaxed; coverage thresholds (55 / 75 / 60 / 70) are unchanged and met |

---

## 3. Constructed controls versus genuinely exercised native behaviour

| Case | Class | Explanation |
| --- | --- | --- |
| D-01 | **constructed** (the crash is placed deliberately before the call) | the *native work* is genuine — no native transaction is opened at all — but the timing is controlled |
| D-02 | **constructed** (deterministic, inside the open transaction) | the native writes, the transaction and the commit are genuine |
| D-03 | **constructed** (the crash is placed deterministically after the commit) | the commit is genuine; the timing is controlled |
| D-04 | **constructed** (the crash is placed deterministically at the commit boundary) | the native commit is genuine; the timing is controlled |
| D-05 | **genuine** | the concurrent access arrives at an arbitrary moment during independent processes; no seam controls the timing |
| D-06, D-08, D-09 | **constructed but native** | the schedule is controlled; every native operation, transaction and commit is real |
| D-07 | **genuine** | the OS-level termination (`kill -9` / `TerminateProcess`) arrives at an arbitrary moment |
| D-10 | **genuine** | the unmodified production suites run end to end |

**Rule for the report.** A constructed case may establish *what happens at a specific point of the
native timeline*; it may not be presented as an arbitrary-timing guarantee. The arbitrary-timing
claim rests on D-05 and D-07 only. Conversely, a genuine case may not be used to claim a specific
internal placement.

---

## 4. Platform requirements

| Case | win-x64 | linux-x64 | macos (managed only) |
| --- | --- | --- | --- |
| D-01 … D-09 | required | **required (genuine hosted Linux)** | not applicable — the LatticeDB native store has no supported macOS runtime in the pinned dependency (Q-S62-5) |
| D-10 | required | required | required (the existing CI matrix covers the managed suites on all three platforms) |
| File-deletion assertion in D-08 | required (Windows-specific semantics) | adapted (clean reopen + no leaked files) | not applicable |

A Linux result produced in WSL2, Docker-on-Windows or any other Windows-hosted compatibility
environment **must not** be recorded as Linux evidence. A macOS run of the managed suites is **not**
macOS evidence for the native store.

---

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

---

## 6. Acceptance and stop conditions

**Acceptance** requires all of: D-01…D-10 PASS or QUAL with a recorded reason; the genuine
`linux-x64` row executed on a genuine hosted Linux runner bound to the sealed commit; D-10 green
with unchanged suites and thresholds; the native API/ABI census unchanged; the production delta
limited to the approved documentation and test additions; and an independent reviewer who re-derived
the expectations from the contract.

**Immediate stop conditions** (owner review required, no further work):

1. The pinned dependency turns out to expose a crash-recovery mechanism (native API census changes) —
   the design must then be re-derived, because a crash-recovery capability would change the S6.2
   boundary.
2. Any case shows a *partial* native application (a partially applied staging, a split publication,
   a durable write reported as crashed/rolled-back).
3. A crash is observed **after** a successful commit (a false crash outcome).
4. A staged or published fact becomes visible while a crash is pending (a D4 invariant-4
   exposure).
5. Disposal interrupts an in-flight native transaction, or the native handle is not released.
6. The historical suites change count, expectation, coverage threshold or CI gate.
7. Any non-Linux result is proposed as Linux evidence, or a compatibility environment is proposed
   as native Linux execution.

---

## 7. Independence

The qualification must be executed by a session other than the implementation session, or reviewed
by an independent reviewer against the sealed evidence, mirroring the S2.x pattern (entry proposal
§3): *"Per-package independent qualification (reviewer session ≠ producer session; receipt-based
independence where hosted runs are used) and owner acceptance."* The reviewer re-derives the
expected outcomes from the contract excerpts (`contract-reconciliation.txt`) and from the production
source, not from the producer's test code.