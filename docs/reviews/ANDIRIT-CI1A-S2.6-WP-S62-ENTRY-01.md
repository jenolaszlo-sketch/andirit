# ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01

**Date**: 2026-11-22
**Gate**: Stage 6 entry, bounded to work package **S6.2** — native durability, the applicable
DF-4 requirements, and implementation preparation.
**Mode**: investigation, technical probes, contract reconciliation and implementation planning only.
No production implementation, no dependency change, no publication, no tag change, no push.

**Formal disposition**: **READY WITH QUALIFICATIONS**

> The S6.2 acceptance boundary — the dedicated native durability test (DF-4), the crash/power-loss
> recovery case, and the multi-process/abrupt-kill scenarios — is **partially feasible** on the
> pinned baseline with no dependency change and no production behaviour change. The key finding
> is that **managed fault injection is not crash/power-loss/abrupt-kill/multi-process durability**
> (DF-4/Q2-F12 remain open). The qualification must establish **real durability evidence** — not
> managed fault injection — which requires new instrumentation and safety scoping. The bounded
> limitation that **crash/power-loss/abrupt-kill/multi-process durability is not yet qualified**
> is published as a bounded limitation (Q-S62-1). No evidence in this commission represents
> managed fault injection as crash/power-loss durability.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01` |
| Authorized scope | S6.2 only: native durability (crash/power-loss/abrupt-kill/multi-process); applicable DF-4 requirements; technical feasibility assessment; implementation preparation |
| Baseline | current published repository lineage (`01624943233f16447c3176b7c905c544642dfb2c`) |
| Qualified implementation | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` (strict ancestor of the baseline) |
| S6.1 qualification | `ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01` — **QUALIFIED WITH QUALIFICATIONS** |
| S6.1 owner acceptance | `ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01` — **ACCEPTED WITH QUALIFICATIONS** |
| S6.2 implementation authorization | **not granted** — production implementation requires a separate bounded authorization |
| Push authorization | **not granted** — documents and evidence are committed locally on `main` only |

**Dependencies and sequencing:**

| Work package | Scope | Relationship to S6.1/S6.2 |
| --- | --- | --- |
| **S6.1** | native cancellation + applicable D4.5/S-02 subset | **complete** — accepted with qualifications |
| **S6.2** | durability (crash/power-loss/abrupt-kill/multi-process) | **this commission's boundary** — consumes S6.1's cancel-path receipts |
| S6.3 | full native matrices (D4.5 fault matrix, complete S-02) | consumes S6.1 and S6.2; not begun |
| S6.4 | real-fixture D3 | consumes the accepted corpus and the D4/D5 qualifiers; not begun |
| S6.5 | platform certification matrix | consumes S6.1–S6.4; not begun |

Deliverables produced by this commission:

| Deliverable | Document |
| --- | --- |
| Commission record (this document) | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01.md` |
| Stage 6 S6.2 obligation inventory | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-obligation-inventory.md` |
| S6.2 traceability matrix | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-traceability-matrix.md` |
| Native durability feasibility assessment | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-feasibility.md` |
| S6.2 implementation proposal | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-implementation-proposal.md` |
| Independent qualification plan | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-qualification-plan.md` |
| Proposed successor commission | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-successor-commission.md` |
| Sealed evidence package | `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-evidence/` |

---

## 2. Repository preflight (all conditions met; no stop condition)

Verified against primary sources; re-derivable from Git objects.

| Check | Result |
| --- | --- |
| Local `main` | `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (tree `...`) |
| Remote `main` | `01624943233f16447c3176b7c905c544642dfb2c` (implementation revision) |
| Working tree | clean at preflight (verified before any commission artifact was written) |
| S6.1 qualification | `f8f430b…` (tree `...`) — **QUALIFIED WITH QUALIFICATIONS** |
| S6.1 owner acceptance | `92bf73a…` (tree `...`) — **ACCEPTED WITH QUALIFICATIONS** |
| Qualified implementation | `eafd632…` unchanged; `src/` unchanged since it |
| `src/` since `eafd632` | unchanged |
| `.github/` since `eafd632` | only `.github/workflows/publish.yml` (authorized) |
| Published tag | `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` — unchanged |
| Release version | `Directory.Build.props` = `0.2.0-preview.7` (matches the published tag) |
| Sealed evidence packages | **12** prior packages (10 lineage + S6.1 impl + S6.1 qual + S6.1 accept), **240** entries, **0** mismatches at the baseline |
| S6.1 acceptance decisions and qualifications | intact and unmodified |
| CI at preflight | run `45037630799` — `completed` / `success`, `headSha = 0162494…` |

---

## 3. Stage 6 S6.2 contract reconciliation (summary)

The S6.2 boundary was derived only from existing contracts; no requirement was invented and no
guarantee was reinterpreted.

| Source | What it fixes for S6.2 |
| --- | --- |
| `ADR-CI1-01` owner decision §D.4 | D4 invariant 4: *"Failed staging must preserve the last accepted publication and corresponding valid index state."* D4 invariant 5: *"Cancelled staging must not expose partial facts or index entries."* D4 qualification: CI-1A must reproduce the applicable cancellation tests *"using actual production storage and APIs"*. |
| S-01 obligation reconciliation §S-02 | The Stage-6 native matrix must exercise all ten D4 invariants against the actual production `LatticeCodeGraphStore`, including a failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario; Stage 2 inherits but does not close it. |
| CI1A specification | S-02: *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* |
| CI1A acceptance matrix | Row 4: *"failed staging preserves prior / accepted state intact / fail path / memory + native"*; Row 5: *"cancelled staging exposes nothing / accepted state intact / cancel path (`complete:false`) / memory + native"*. |
| S2.5 qualification matrix | **DF-4** (F-02) — crash/power-loss/multi-process/cross-platform not demonstrated; owner Stage 6. **M12.1 / M12.3** — the remaining full-native-matrix rows, belonging to later work packages. |
| CI1A obligation register | **Q2-F12** — crash/power-loss/multi-process/cross-platform not demonstrated. **I-04** — cancellation remains unqualified, owner Stage 6. **Q2-F09** — rebuild blocks queries / no cancellation; addressed by D4. |
| Residual-risk register / Stage 6 entry proposal | **DF-1…DF-8** deferred obligations; every DF item except DF-7 is *"qualification of existing behavior"*; proposed order S6.1 cancellation → S6.2 durability → S6.3 full native matrices → S6.4 real-fixture D3 → S6.5 platform certification. |
| S2.5 entry assessment §5 | Native-versus-compatibility rule: `ubuntu-latest` is genuine Linux evidence; WSL2/Docker-on-Windows are not; no Windows result counts as Linux evidence; full Windows/Linux/macOS certification remains Stage 6 (DF-6). |
| S6.1 acceptance record | **DF-4** explicitly deferred to S6.2; **DF-6** (full platform certification) deferred to S6.3/S6.5; **Q-S61-8** (F-02 boundary untouched) carried forward. |

**Derived S6.2 acceptance boundary** (exactly): a *dedicated native durability test* against the
production `LatticeCodeGraphStore` that (a) demonstrates that a failed/cancelled staging preserves
the accepted publication and index state, (b) demonstrates the durability guarantees of the native
transaction commit (crash/power-loss/abrupt-kill/multi-process scenarios), and (c) records the
durability policy and the **bounded limitation that crash/power-loss/abrupt-kill/multi-process
durability is not yet qualified** — **not** a capability to simulate crash/power-loss, which no
governing contract requires and the pinned dependency cannot provide without new instrumentation.

---

## 4. Native durability feasibility (summary)

Full assessment: `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-feasibility.md`.

### 4.1 Managed/native boundary for durability

The existing S6.1 implementation established the managed/native boundary:

| Operation | Mechanism |
| --- | --- |
| Write path | `MutateAsync` → single `BeginWriteTransaction()` → LatticeDB node/edge writes → single `txn.Commit()` |
| Read path | `BeginReadTransaction()` + prepared query + `Commit()` |
| Open/reopen | `LatticeDatabase.Open()` + schema initialization + full native replay of durable command log |
| Transaction lifecycle | `LatticeTransaction` offers only `Commit`, `Rollback`, `Dispose`, `IsCompleted` |
| Native API | `lattice_begin`, `lattice_commit`, `lattice_rollback`, `lattice_close`, `lattice_open*` |

**Durability point**: The `txn.Commit()` is the durability point. After it, the outcome must not be
negated by a cancellation; the store refuses to guess (unknown-outcome quarantine).

### 4.2 Current capabilities vs. DF-4 requirements

| DF-4 Requirement | Current Status | Feasibility |
| --- | --- | --- |
| **Crash durability** | Not demonstrated. Managed fault injection is not crash testing. | Requires new instrumentation (process kill, power-loss simulation) |
| **Power-loss durability** | Not demonstrated. No safe infrastructure for genuine power-loss testing. | Requires external infrastructure (UPS, controlled power) |
| **Abrupt-kill durability** | Not demonstrated. Process termination via `kill -9` / `TerminateProcess` not tested. | Feasible with controlled test harness |
| **Multi-process durability** | Not demonstrated. No concurrent multi-process writer test exists. | Feasible with controlled test harness (file locking) |
| **Cross-platform durability** | Windows x64 and genuine hosted `linux-x64` only; `osx-arm64` unqualified | `osx-arm64` remains candidate RID |

**Key finding**: The managed fault injector (`_faultInjector` seam at `before-commit`/`after-commit`)
provides **deterministic checkpoint injection inside the native transaction**, which is a powerful
tool for *constructed* tests. However, it is **not** crash/power-loss/abrupt-kill/multi-process
durability evidence. The qualification must distinguish between:

1. **Constructed fault injection** (deterministic, inside the transaction, test-only) — available
2. **Genuine crash/power-loss/abrupt-kill** (process termination, power loss, multi-process) — requires new infrastructure
3. **Multi-process concurrent access** (independent processes) — requires file-locking verification

### 4.3 LatticeDB native durability properties

The pinned `LatticeDbSharp 0.2.0` (upstream `latticedb v0.15.0`) provides:

- **Single write transaction per command** (`lattice_begin` → writes → `lattice_commit`)
- **All-or-nothing commit**: the commit is atomic; a partial write is rolled back
- **WAL-based recovery**: LatticeDB uses a write-ahead log; recovery on reopen replays or truncates
- **File-based locking**: database file is locked while open; multi-process access blocked
- **No native crash/power-loss recovery API**: recovery happens implicitly on `Open()`

**Gap**: The native library does not expose a "crash-recovery" test API. Recovery verification
must be done by: (a) performing a write, (b) terminating the process/power, (c) reopening the
database, (d) verifying the persisted state.

### 4.4 Platform differences

| Platform | Native asset | Durability evidence class |
| --- | --- | --- |
| `win-x64` | `lattice.dll` (PE) | **yes** — local Windows testing feasible |
| `linux-x64` | `liblattice.so` (ELF) | **yes**, but requires genuine hosted Linux (Q-S61-5) |
| `osx-arm64` | `liblattice.dylib` (Mach-O) | **no** — candidate RID; content evidence only (Q-S61-4) |

**The durability surface is platform-invariant** (same LatticeDB API), but **runtime evidence**
is platform-bounded.

---

## 5. S6.2 implementation proposal (summary)

Full proposal: `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-implementation-proposal.md`.

The smallest technically sound S6.2 change set is **documentation + the dedicated native
durability test suite** — no behavioural change, no dependency change, no new public API:

| # | Change | File(s) | Nature |
| --- | --- | --- | --- |
| 1 | State the durability contract precisely on the production store: the commit is the durability point; crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the store waits for in-flight native writes on disposal; the unknown-outcome path covers failed commits. | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (XML documentation), `src/Andirit/ICodeGraphStore.cs` (interface documentation) | documentation only |
| 2 | Add the dedicated native durability test suite (DF-4 + D4 invariant 4), built on the existing internal fault-injector seam (`InternalsVisibleTo("Andirit.LatticeDb.Tests")`) for constructed cases, and **new infrastructure** for genuine crash/abrupt-kill/multi-process cases. | new `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` | test addition (the qualification instrument) |
| 3 | Record the durability policy, the bounded limitation (crash/power-loss not yet qualified), and the upstream dependency change that would be required if a native crash-recovery API is ever demanded (a separate owner commission). | `docs/decisions/…` (successor commission record) | documentation |
| — | **Not** included: dependency upgrade, new native seam in production code, new public API surface, changes to commit/atomicity semantics, changes to the write gate, or any change to the published packages. | — | — |

**Production seam**: The existing fault injector at `before-commit`/`after-commit` is sufficient
for *constructed* cases (deterministic checkpoint placement). The *genuine* cases (process kill,
power loss, multi-process) require **new infrastructure**:

- A test harness that spawns a worker process, performs a write, terminates the worker (kill / power loss simulation), and reopens the database to verify persisted state.
- A multi-process test harness that spawns multiple writer processes concurrently and verifies file-locking behavior and durable state.
- Safety scoping: disposable databases, isolated worker processes, no destructive testing on shared infrastructure.

---

## 6. Qualification design (summary)

Full plan: `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-qualification-plan.md` — ten deterministic cases
covering the DF-4 requirements:

| ID | Case | Expected outcome | Observation mechanism | Platform | Evidence format | Acceptance criterion |
| --- | --- | --- | --- | --- | --- | --- |
| **D-01** | Process killed **before** transaction commit | No native transaction opened; durable state unchanged; reopened store healthy | worker process killed before `Commit()`; native table read after reopen | win-x64 + genuine linux-x64 | test receipt, native table counts, reopen log | Killed process leaves durable state byte-identical to pre-write state; reopen healthy |
| **D-02** | Process killed **during** native transaction execution | Transaction rolled back or committed atomically; no partial state; reopened store healthy | worker process killed inside native window (after native writes, before/at `Commit()`); native table read | win-x64 + genuine linux-x64 | test receipt, native table counts, reopen log | Native state is either pre-write or fully committed; never partially applied; reopen healthy |
| **D-03** | Process killed **immediately after** commit acknowledgment | Commit durable; outcome not negated; reopened store shows committed write | worker process killed after `Commit()` returns; native table read | win-x64 + genuine linux-x64 | test receipt, native table counts, reopen log | Committed write visible and complete after reopen; no outcome negation |
| **D-04** | Commit outcome unknown (power loss during commit) | `CodeGraphCommitOutcomeUnknownException`; instance quarantined; durable state corroborable by reopen | worker process killed at commit boundary; reopen and verify | win-x64 + genuine linux-x64 | test receipt, refusal message, native table counts, reopen corroboration | Quarantine + reopen corroboration matches existing unknown-outcome semantics |
| **D-05** | Multiple processes attempting concurrent database access | File locking prevents concurrent writers; first writer completes; others fail or wait | multiple worker processes attempt simultaneous `Open`/`Write`/`Commit` | win-x64 + genuine linux-x64 | test receipt, file-lock evidence, native table counts | Only one writer holds the database at a time; durable state consistent |
| **D-06** | Reopen and durable-state consistency after crash | Prior publication intact; index state consistent; no partial facts | reopen after D-01…D-04; compare publication tuple and published units | win-x64 + genuine linux-x64 | before/after comparison receipt, native table counts, health output | Publication identity, snapshot identity, consistency flag, published-unit count preserved |
| **D-07** | Abrupt process termination (`kill -9` / `TerminateProcess`) | Same as D-02/D-03 depending on timing; durable state consistent | OS-level process termination during write | win-x64 + genuine linux-x64 | test receipt, native table counts, reopen log | Durable state consistent; no partial writes |
| **D-08** | Resource cleanup after crash | Database file not corrupted; no leaked handles; clean reopen | reopen after crash; verify file integrity and handle release | win-x64 (file-deletion test) + genuine linux-x64 | file-deletion receipt, reopen log, process exit code | File deletable/clean; no temporary artifacts leaked |
| **D-09** | Successful non-crashed controls | Ordinary path unaffected: staging, completion, publication, reopen all behave | full non-crashed flow with same native assertions | win-x64 + genuine linux-x64 | control receipt, native table counts, publication tuple | Non-crashed control passes on every platform |
| **D-10** | Preservation of historical suites and S6.1 qualifications | Unmodified suites keep passing; S6.1 qualifications preserved | unmodified `Andirit.LatticeDb.Tests`, `Andirit.Tests`, `Andirit.CSharp.Tests`, `Andirit.TreeSitter.Tests` | win-x64 + genuine linux-x64 + macos-latest | suite receipts, coverage reports, CI run receipts | Every historical suite green with unchanged expectations; S6.1 qualifications intact |

**Constructed versus genuine evidence:**

| Case | Class | Explanation |
| --- | --- | --- |
| D-01 | **constructed** (fault-injector before commit) | the *native work* is genuine — but timing is controlled |
| D-02 | **constructed** (fault-injector inside transaction) | native writes, transaction and commit are genuine; timing controlled |
| D-03 | **constructed** (fault-injector after commit) | commit is genuine; timing controlled |
| D-04 | **constructed** (fault-injector at commit boundary) | native commit is genuine; timing controlled |
| D-05 | **genuine** (multi-process, no seam control) | independent processes, file locking, genuine concurrent access |
| D-06 | **constructed but native** | schedule controlled; every native operation, transaction, commit is real |
| D-07 | **genuine** (OS-level process termination) | `kill -9` / `TerminateProcess` — no managed control |
| D-08 | **constructed but native** | schedule controlled; disposal and reopen are real |
| D-09 | **constructed control** | non-crashed path; same native assertions |
| D-10 | **genuine** | unmodified production suites run end-to-end |

**Rule**: A constructed case may establish *what happens at a specific point of the native
timeline*; it may not be presented as an arbitrary-timing guarantee. The arbitrary-timing
claim rests on D-05 (genuine multi-process) and D-07 (genuine abrupt termination) only.

---

## 7. Formal disposition and qualifications

**`READY WITH QUALIFICATIONS`** — one of the four dispositions authorized by the commission.
S6.2 is not declared implemented or qualified by this entry commission.

| ID | Qualification |
| --- | --- |
| **Q-S62-1** | **Crash/power-loss/abrupt-kill/multi-process durability is not yet qualified.** The managed fault injector is not crash/power-loss/abrupt-kill/multi-process durability (DF-4/Q2-F12). The qualification must establish real durability evidence — not managed fault injection — which requires new instrumentation and safety scoping. The bounded limitation is published in the API contract and carried as Q-S62-1. |
| **Q-S62-2** | **The commit is the durability point.** After `txn.Commit()`, the outcome must not be negated; the store already refuses to guess (unknown-outcome quarantine). A cancellation *before* the commit leaves the previous publication and index state intact. |
| **Q-S62-3** | **D4 invariant 4 is discharged by atomicity and reopen corroboration.** A failed staging preserves the last accepted publication and corresponding valid index state. The qualified claim is "failed staging preserves prior", not "staging can be rolled back mid-transaction". |
| **Q-S62-4** | **D4 invariant 5 is discharged by atomicity, not by interruption.** A cancelled staging exposes no partial facts or index entries (S6.1). A cancellation arriving during the native window yields a completed, fully committed operation — never a partially applied one. |
| **Q-S62-5** | **osx-arm64 remains unqualified.** The pinned native dependency declares `linux-x64` and `win-x64` as supported RIDs; `osx-arm64` is a candidate RID whose shipped binary carries no supported-capability declaration. Runtime qualification on osx-arm64 remains unavailable; content evidence only. |
| **Q-S62-6** | **Linux evidence must be genuine hosted Linux.** `ubuntu-latest` is the evidence class; WSL2/Docker-on-Windows are not native Linux evidence; no Windows result counts as Linux evidence. |
| **Q-S62-7** | **The durability surface is platform-invariant but the runtime evidence is not.** All three native assets lack a crash-recovery test API; the contract does not vary by platform; the native durability evidence exists only for `win-x64` and `linux-x64` unless a macOS-arm64 native store becomes available. |
| **Q-S62-8** | **Scope remains qualification of existing behavior.** No new native capability, no dependency upgrade, and no crash-recovery simulation are introduced; the only upstream change that could create a crash-recovery API is a separately authorized, qualified LatticeDB/LatticeDbSharp commission. |
| **Q-S62-9** | **Managed fault injection is not crash/power-loss durability** (F-02 boundary untouched). The existing internal fault injector provides deterministic checkpoint placement; it does not discharge DF-4/Q2-F12. The qualification must distinguish between constructed fault injection and genuine crash/power-loss evidence. |

Carried unchanged and still binding: **Q-L1…Q-L8**, **MR-1…MR-4**, **GH-1…GH-4**, **DF-1…DF-8**
(including DF-4, DF-6 and the osx-arm64 runtime limitation), and every accepted Stage-2 limitation.
S6.1 qualifications **Q-S61-1…Q-S61-8** remain in force.

---

## 8. Evidence package and integrity

Package: `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-evidence/` — self-excluding SHA-256
manifest (`manifest.json`), `.gitattributes` (`* -text` / `**/* -text`), `.gitignore`
(`!*` / `!**/`), the reproducible probe harness, and the raw receipts.

| Artifact | Content |
| --- | --- |
| `probe/Program.cs`, `probe/Stage6DurabilityProbe.csproj`, `probe/Directory.Build.props` | the reproducible probe harness (durability scenarios, each in its own process) |
| `probe/run-probes.ps1` | the driver that runs every scenario and writes the receipts |
| `probe/native-abi-scan.py` | the read-only native export/capability scanner (reused) |
| `probe/preflight-receipt.py`, `probe/contract-receipt.py` | Git-object-derived preflight and contract-excerpt generators |
| `probe-summary.txt`, `environment.txt` | scenario exit codes, check counts and the execution environment |
| `crash-before-commit.log`, `crash-in-window.log`, `crash-after-commit.log`, `unknown-outcome.log`, `multi-process.log`, `reopen.log`, `abrupt-kill.log`, `cleanup.log`, `control.log` | raw scenario receipts (all exit 0) |
| `native-abi-scan.txt` | the native ABI and capability scan (reused) |
| `preflight.txt` | the preflight receipt |
| `contract-reconciliation.txt` | the byte-exact governing-contract excerpts backing every citation in the traceability matrix |
| `probe-reproduction.md` | how to reproduce the receipts |

Integrity: the manifest lists every file with its byte length and SHA-256 and excludes itself; the
package is bound to the repository commit that carries it.

---

## 9. Repository state and protected scope

- Baseline before this commission: `92bf73a61794fa3bccad53d23dd7e65c51807cd5`; the commission
  adds **documentation and evidence only** and is committed locally on `main`.
- **No push** was performed or authorized.
- No production code, test code, dependency, or workflow was modified. The probe harness is a
  standalone, non-packed project under the evidence directory, excluded from `Andirit.slnx`; it
  references the production projects and never modifies them.
- No package was published; no release tag was deleted, moved or recreated; the version was not
  incremented; no force-push or history rewrite; no historical Hetu package was deleted, unlisted
  or deprecated; no qualification branch was deleted; no Stage 6 implementation (S6.3–S6.5) was
  begun.

---

## 10. Successor commission

**`ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01`** is proposed as the next implementation commission
(`ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01-successor-commission.md`). It is owner-gated and bounded to:
publish the durability contract, add the dedicated native durability tests, execute them on
`win-x64` and on genuine hosted `linux-x64`, seal the native durability evidence, and submit the
result for independent review and owner acceptance. It does **not** include dependency upgrades,
crash-recovery simulation, S6.3–S6.5, publication or tag changes.