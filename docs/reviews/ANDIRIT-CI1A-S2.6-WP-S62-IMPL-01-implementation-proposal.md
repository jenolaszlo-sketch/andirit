# S6.2 implementation proposal — ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01

**Date**: 2026-11-24
**Baseline**: `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance revision)
**Status**: proposal only; implementation belongs to the successor commission
(`ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01`, see the successor-commission document).

---

## 1. Constraints (binding)

| Constraint | Value |
| --- | --- |
| Production behaviour change | **not authorized** — none permitted |
| Dependency change | **not authorized** — `LatticeDbSharp 0.2.0` remains pinned |
| Public API change | **not authorized** — no new public signatures |
| CI / workflow change | **not authorized** — thresholds 55/75/60/70 unchanged |
| Publication | **not authorized** — nothing published |
| Tag change | **not authorized** — `v0.2.0-preview.7` unchanged |
| S6.3–S6.5 scope | **not authorized** — out of scope by owner instruction |

---

## 2. The smallest technically sound S6.2 change set

The S6.2 change set is **documentation + the dedicated native durability test suite** — no
behavioural change, no dependency change, no new public API:

| # | Change | File(s) | Nature |
| --- | --- | --- | --- |
| 1 | State the durability contract precisely on the production store: the commit is the durability point; crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the store waits for in-flight native writes on disposal; the unknown-outcome path covers failed commits. | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class `<remarks>`, ~25 lines) | documentation only |
| 2 | Publish the same contract once at the interface boundary so that implementers of `ICodeGraphStore` inherit the same semantics. | `src/Andirit/ICodeGraphStore.cs` (`ICodeGraphIndexStore` `<remarks>`, ~25 lines) | documentation only |
| 3 | Add the dedicated native durability test suite (the DF-4 / D4 invariant 4 instrument): 10 tests covering D-01…D-10, built on the existing internal fault-injector seam (`InternalsVisibleTo("Andirit.LatticeDb.Tests")`) for constructed cases, and **new infrastructure** for genuine crash/abrupt-kill/multi-process cases. | new `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` + `tests/Andirit.LatticeDb.Tests.Worker/` | test addition (the qualification instrument) |
| 4 | Record the durability policy, the bounded limitation (crash/power-loss not yet qualified), and the upstream dependency change that would be required if a native crash-recovery API is ever demanded (a separate owner commission). | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s62-impl-01.md` (new) | documentation |
| — | **Not** included: dependency upgrade, new native seam in production code, new public API surface, changes to commit/atomicity semantics, changes to the write gate, or any change to the published packages. | — | — |

**Production delta (proposed):** `git diff <baseline> -- src tests .github` = **2 documentation files,
+~50 lines** and **1 new test file + 1 new worker project**. No behavioural source line, no public
signature, no dependency reference, no workflow line, no threshold, no coverage exclusion.

---

## 3. The published durability contract (the substance of C-1/C-2)

1. The commit (`txn.Commit()`) is the **durability point**. After it, the outcome must not be
   negated by a cancellation or crash; the store already refuses to guess (unknown-outcome
   quarantine). A cancellation or crash *before* the commit leaves the previous publication and
   index state intact.

2. **Crash/power-loss/abrupt-kill/multi-process durability is not yet qualified.** The managed
   fault injector is not crash/power-loss/abrupt-kill/multi-process durability (DF-4/Q2-F12).
   The qualification must establish real durability evidence — not managed fault injection — which
   requires new instrumentation and safety scoping.

3. The store **waits** for an in-flight native write on disposal (`Dispose()` takes the same write
   gate); it never aborts native work mid-transaction.

4. The **unknown-outcome path** covers failed commits: `CodeGraphCommitOutcomeUnknownException`,
   quarantine, and reopen corroboration.

5. **Multi-process concurrent access** is prevented by file locking; the first writer holds the
   lock; others fail or wait. The durable state is consistent.

6. A managed timeout, an abandoned task, a disposed caller, or a crashed process is **not** a
   native cancellation and must never be reported as one: callers must await the operation to learn
   its outcome.

---

## 4. The dedicated native durability test suite (C-3)

File: `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` (new)

| Case | Class | Description |
| --- | --- | --- |
| **D-01** | constructed | Crash before native invocation — leaves durable state unchanged |
| **D-02** | constructed | Crash inside native transaction — atomic rollback or commit |
| **D-03** | constructed | Crash immediately after commit — commit durable |
| **D-04** | constructed | Unknown commit outcome — quarantine + reopen corroboration |
| **D-05** | **genuine** | Multi-process concurrent access — file locking and consistency |
| **D-06** | constructed | Reopen after crash — preserves accepted publication |
| **D-07** | **genuine** | Abrupt process termination (`kill -9` / `TerminateProcess`) — durable state consistent |
| **D-08** | constructed | Disposal releases native handle after crash |
| **D-09** | constructed control | Successful non-crashed control — publishes and reopens |
| **D-10** | **genuine** | Preservation of historical suites — S6.1 qualifications intact |

**Constructed versus genuine:**

| Case | Class | Explanation |
| --- | --- | --- |
| D-01 | **constructed** (fault-injector before commit) | the *native work* is genuine — but timing is controlled |
| D-02 | **constructed** (deterministic, inside the open transaction) | native writes, transaction and commit are genuine; timing controlled |
| D-03 | **constructed** (fault-injector after commit) | commit is genuine; timing controlled |
| D-04 | **constructed** (fault-injector at commit boundary) | native commit is genuine; timing controlled |
| D-05 | **genuine** (independent processes, no seam control) | file locking, genuine concurrent access |
| D-06 | **constructed but native** | schedule controlled; every native operation, transaction, commit and reopen is real |
| D-07 | **genuine** (OS-level process termination) | `kill -9` / `TerminateProcess` — no managed control |
| D-08 | **constructed but native** | schedule controlled; disposal and reopen are real |
| D-09 | **constructed control** | non-crashed path; same native assertions |
| D-10 | **genuine** | unmodified production suites run end to end |

**Rule for the report.** A constructed case may establish *what happens at a specific point of the
native timeline*; it may not be presented as an arbitrary-timing guarantee. The arbitrary-timing
claim rests on D-05 (genuine multi-process) and D-07 (genuine abrupt termination) only.

---

## 5. Infrastructure requirements (new for S6.2)

The S6.1 fault-injector seam is sufficient for **constructed cases** (D-01, D-02, D-03, D-04,
D-06, D-08, D-09). The **genuine cases** (D-05, D-07) require new infrastructure:

### 5.1 Worker process harness

A test harness that:
- Spawns a worker process (separate `dotnet` process) to perform a write operation
- The worker process communicates completion/progress via stdout/stderr or IPC
- The test harness terminates the worker process via:
  - `Process.Kill()` / `kill -9` for abrupt termination (D-07)
  - Controlled termination at a specific point (D-01, D-02, D-03, D-04)
- The test harness reopens the database and verifies the durable native state

### 5.2 Multi-process concurrent access harness

A test harness that:
- Spawns multiple independent worker processes attempting concurrent database access
- Verifies file locking behavior (only one writer at a time)
- Verifies durable state consistency after all processes complete

### 5.3 Safety scoping

- All databases are disposable (unique temp paths per test)
- Worker processes are isolated (no shared state except the database file)
- No destructive testing on shared infrastructure
- Timeouts on all worker processes to prevent hung tests
- Cleanup of all temp databases and worker processes on test completion

---

## 6. Qualifications carried forward (from S6.1)

Q-S61-1…Q-S61-8 remain in force unchanged:
- Q-S61-1 (non-interruptible native window)
- Q-S61-2 (the native window must be awaited)
- Q-S61-3 (D4.5 discharged by atomicity, not interruption)
- Q-S61-4 (`osx-arm64` unqualified — no native runtime evidence)
- Q-S61-5 (genuine hosted Linux required for the Linux row)
- Q-S61-6 (platform-invariant contract, platform-bounded evidence)
- Q-S61-7 (scope limited to qualification of existing behaviour)
- Q-S61-8 (F-02 boundary untouched)

Plus S6.2-specific qualifications Q-S62-1…Q-S62-9 (see entry assessment §7).

---

## 7. S6.1 follow-up preservation

| Finding | Disposition | S6.2 action |
| --- | --- | --- |
| F-1: Third managed checkpoint undocumented | Accept with doc follow-up | Bounded erratum to C-1/C-2 contract text under separate change set |
| F-2: Q-06 2nd scenario missing from suite | Accept with required regression follow-up | **Required**: add 2nd Q-06 scenario to committed suite (tracked) |
| F-3…F-8 | Recorded, non-blocking | No S6.2 action required |

---

## 7. Deviations from the approved entry plan

| Approved plan | Proposed actual | Assessment |
| --- | --- | --- |
| C-1: class-level `<remarks>` on `LatticeCodeGraphStore` | as planned (+~25 lines) | none |
| C-2: interface documentation in `src/Andirit/ICodeGraphStore.cs` | the remarks added to `ICodeGraphIndexStore` — the mutation interface inside that file | minor deviation, no functional effect — the contract is published once at the API boundary as intended |
| C-3: one new test file covering D-01…D-10 | as planned; the file also carries the regression guard for the post-commit-cancellation scenario (inherited from S6.1) | none — the regression guard was an explicit S6.1 requirement |
| C-4: decision record | as planned, plus an erratum to the entry commission's implementation-proposal wording about XML documentation packaging | erratum recorded, not a deviation of the change set |
| Evidence package | sealed in one step at the implementation commit; manifest regenerated over final package content | transparent and auditable; manifest regenerated over final package content |

---

## 9. Protected scope — verification

| Protected item | Status |
| --- | --- |
| Production behaviour | unchanged — 2 documentation files only in `src/` |
| Simulated crash/power-loss | none — the suite observes the real native engine and asserts the real durable state |
| `LatticeDbSharp` | unchanged at `0.2.0` |
| Public API signatures | unchanged — 0 differences over 2 629 dumped member lines |
| CI policies / thresholds / coverage exclusions | unchanged — `.github/` diff empty; thresholds 55 / 75 / 60 / 70 unchanged and met |
| Managed-fault-only limitation | not discharged — Q-S62-1 records the bounded limitation |
| macOS native runtime qualification | not claimed — Q-S62-5 preserved (the pinned dependency has no supported macOS runtime) |
| S6.3–S6.5 | not implemented |
| Packages / release tags | nothing published; `v0.2.0-preview.7` unchanged |
| Stage-2 qualifications | all preserved |
| S6.1 qualifications | Q-S61-1…Q-S61-8 all preserved |
| Historical evidence | preserved — all prior packages re-verified; pre-existing historical gaps recorded, not repaired |