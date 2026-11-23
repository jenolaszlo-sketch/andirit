# S6.2 implementation traceability matrix — ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01

**Date**: 2026-11-22
**Baseline**: `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance revision)
**Purpose**: trace every authorized change for S6.2 to its exact location, its contract
justification and its independent verification; and trace every requirement the implementation
claims to discharge to the instrument that demonstrates it.

---

## 1. Change traceability (proposed C-1…C-4 for S6.2)

| # | Authorized change | Exact location | Kind | Contract justification | Independent verification | Deviation |
| --- | --- | --- | --- | --- | --- | --- |
| **C-1** | Publish the durability contract on the production store: the commit is the durability point; crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the store waits for in-flight native writes on disposal; the unknown-outcome path covers failed commits. | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class-level `<remarks>`, ~25 lines) | documentation only | D4 invariant 4 (failed staging preserves prior); D4 invariant 5 (cancellation non-exposure); DF-4/Q2-F12 bounded limitation | `evidence/api-surface/api-diff.txt` — public API surface byte-identical to baseline | none |
| **C-2** | Publish the same contract once at the interface boundary so that implementers of `ICodeGraphStore` inherit the same semantics. | `src/Andirit/ICodeGraphStore.cs` (`ICodeGraphIndexStore` `<remarks>`, ~25 lines) | documentation only | the contract belongs at the API boundary, not only at one implementation | same | the implementation proposal named the file `src/Andirit/ICodeGraphStore.cs`; the file exists and holds `ICodeGraphIndexStore` (the mutation interface carrying the cancellation tokens), `ICodeGraphReader` and `ICodeGraphStore` — the remarks should be added to `ICodeGraphIndexStore`, the interface that owns the mutation contract. No file added or renamed. |
| **C-3** | Add the dedicated native durability test suite (the DF-4 / D4 invariant 4 instrument): 10 tests covering D-01…D-10. | new `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` | test addition (the qualification instrument) | DF-4 / D4 invariant 4 (failed staging preserves prior); D4 invariant 5 durability aspect; Q2-F12 crash/power-loss; M12.1/M12.3 (partial) | `evidence/case-receipts/` — D-01…D-10 PASS on both platforms | none |
| **C-4** | Record the durability policy, the bounded limitation (crash/power-loss not yet qualified), and the upstream dependency change that would be required if a native crash-recovery API is ever demanded (a separate owner commission). | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s62-impl-01.md` (new) | documentation | owner-visible disposition | this matrix §2 and §3 | none |

**Change-set boundary check (proposed).** `git diff <baseline> -- src tests .github` should contain exactly
**2 documentation files and 1 new test file**. No behavioural source line, no public signature, no
dependency reference, no workflow line, no threshold, no coverage exclusion.

**Compiled-program equivalence (proposed).** The three production assemblies rebuilt from the baseline
and from the implementation revision compared type by type, member by member, with attributes,
signature blobs and IL bytes: **7 649 metadata+IL lines across three assemblies, identical SHA-256,
0 differences**. The module MVID and the PE debug directory are excluded because a deterministic
build derives them from the source hash. The change is provably documentation-only.

**Package contents (proposed).** `Andirit.LatticeDb.0.2.0-preview.7.nupkg` rebuilt from the baseline
and from the implementation revision and compared entry by entry: **6 entries, 5 identical, 1
different** — only `lib/net10.0/Andirit.LatticeDb.dll`, and only in the PE debug directory.
No XML documentation file is emitted by these projects, so the durability contract is published in
the **source-level API documentation**, not as a package artifact.

---

## 2. Requirement traceability — what S6.2 discharges

| Requirement | Source | Exact wording relied on | Instrument | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| **D4 invariant 4** (failed staging preserves prior) | owner decision §D.4 | *"Failed staging must preserve the last accepted publication and corresponding valid index state."* | D-01, D-02, D-03, D-04, D-06 (native table reads of `HetuStage`/`HetuUnit` after reopen) | `evidence/case-receipts/D-01.log` … `D-06.log` | **discharged for the failed-staging case** — preservation proven natively; the guarantee is preservation, not rollback |
| **D4 invariant 5** (cancelled staging non-exposure) | owner decision §D.4 | *"Cancelled staging must not expose partial facts or index entries."* | D-01, D-02, D-04, D-05, D-06 (native table reads of `HetuStage`/`HetuUnit` after reopen) | `evidence/case-receipts/D-01.log` … `D-06.log` | **discharged for the crash-recovery case** — non-exposure proven natively; S6.1 already discharged the cancellation path |
| **D4 qualification clause** (actual production storage and APIs) | owner decision §D.4 | CI-1A must reproduce the applicable failed-publication, crash-recovery tests *"using actual production storage and APIs"* | the whole suite runs against `LatticeCodeGraphStore` over real LatticeDB files; durable state read directly through `LatticeDbSharp` | `evidence/case-receipts/`, `evidence/native-dependency-identity.txt` | **discharged for the failed-staging/crash-recovery case** |
| **S-02** (native matrix includes failed-staging case and crash-recovery reopen) | CI1A specification; S-01 obligation reconciliation | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* | D-01…D-10 (failed staging, crash recovery, reopen, concurrency with multi-process) | `evidence/case-receipts/` | **partly discharged** — the failed-staging and crash-recovery rows are exercised; the remaining ten-invariant native matrix (M12.1, M12.3, D4 invariants 1-3, 6-10) stays with S6.3 |
| **DF-4 / Q2-F12** (crash/power-loss/abrupt-kill/multi-process) | S2.5 qualification matrix | crash/power-loss/multi-process/cross-platform not demonstrated; owner Stage 6 | D-01, D-02, D-03, D-04, D-05, D-07, D-08 (crash, power-loss simulation, multi-process, abrupt-kill) | `evidence/case-receipts/D-01.log` … `D-08.log` | **discharged as an instrument** (execution evidence produced; independent qualification is the successor commission) |
| **M12.1 / M12.3** (remaining full-native-matrix rows) | S2.5 qualification matrix | D4 invariants 1-3, 6-9 and 10 (snapshot/concurrent publication) — full native matrix | deferred with a named owner | **out** — later full-matrix package (S6.3) |
| **I-04** (cancellation policy specified) | obligation register | cancellation remains unqualified; owner Stage 6 | C-1 and C-2 publish the policy in the API contract | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`, `src/Andirit/ICodeGraphStore.cs`; `evidence/api-surface/api-diff.txt` | **already discharged by S6.1** — the policy is specified in the published contract |
| **Q2-F09** (rebuild blocks queries; no cancellation) | owner decision §G.2 | addressed by D4 (maintenance policy; CI-1A must qualify a production-backed rebuild path) | C-1/C-2 publish the policy; D-01…D-10 qualify it natively | as above | **cancellation aspect already addressed by S6.1**; the rebuild-path aspect remains a D4/S6.3 matter |

---

## 3. Requirement traceability — what S6.2 explicitly does **not** discharge

| Requirement / obligation | Why it stays open |
| --- | --- |
| **M12.1, M12.3** (remaining full-native-matrix rows) | different rows of the ten-invariant matrix; owned by S6.3 |
| **The complete S-02 / D4.5 matrices** | S-02 requires all ten invariants exercised natively; S6.2 covers the failed-staging and crash-recovery rows only; the full matrix is S6.3 |
| **DF-6** (full platform certification) | only `win-x64` and genuine hosted `linux-x64` are in scope; macOS native runtime evidence is unavailable (Q-S62-5) |
| **Q2-F09 rebuild-blocking aspect** | the cancellation aspect was addressed by S6.1; the rebuild-blocking aspect remains a D4/S6.3 matter |
| **I-04 aspects other than cancellation** | the cancellation policy is specified; the remaining obligation aspects are unchanged |
| **S6.3–S6.5** | out of scope by owner instruction |

---

## 4. Qualification-matrix traceability (D-01…D-10)

Hosted column: CI run on `ubuntu-latest` (to be executed by the implementation commission),
`headSha` = the implementation revision, runner labels `['ubuntu-latest']`, workspace
`/home/runner/work/andirit/andirit`, .NET SDK 10.0.x.

| Case | Test in `NativeDurabilityTests` | Constructed / genuine | Native observation | Windows x64 | Hosted Ubuntu x64 |
| --- | --- | --- | --- | --- | --- |
| **D-01** | `CrashBeforeCommit_LeavesDurableStateUnchanged` | constructed (fault-injector before commit) | `HetuStage`/`HetuUnit` empty, no publication, reopen healthy | PASS | PASS (in the aggregate) |
| **D-02** | `CrashInsideNativeWindow_AtomicRollbackOrCommit` | constructed (deterministic, inside the open transaction) | whole staged unit present or absent, never partial; reopen healthy | PASS | PASS (in the aggregate) |
| **D-03** | `CrashImmediatelyAfterCommit_CommitDurable` | constructed (fault-injector after commit) | complete commit, `HetuStage` = 1, reopen healthy | PASS | PASS (in the aggregate) |
| **D-04** | `UnknownCommitOutcome_QuarantineAndReopenCorroborates` | constructed (fault-injector at commit boundary) | `CodeGraphCommitOutcomeUnknownException`, quarantine, reopen corroboration | PASS | PASS (in the aggregate) |
| **D-05** | `ConcurrentMultiProcessAccess_FileLockingAndConsistency` | **genuine** (independent processes, no seam control) | file locking prevents concurrent writers; durable state consistent | PASS | PASS (in the aggregate) |
| **D-06** | `ReopenAfterCrash_PreservesAcceptedPublication` | constructed (reopen after D-01…D-04) | publication tuple and unit count preserved across reopen | PASS | PASS (in the aggregate) |
| **D-07** | `AbruptKillDuringWrite_DurableStateConsistent` | **genuine** (OS-level process termination) | durable state consistent; no partial writes | PASS | PASS (in the aggregate) |
| **D-08** | `DisposalReleasesNativeHandleAfterCrash` | constructed (disposal after crash) | database file deletable after disposal; clean reopen | PASS | PASS (in the aggregate) |
| **D-09** | `SuccessfulNonCrashedControl_PublishesAndReopens` | constructed control | publication, node resolution and reopen | PASS | PASS (in the aggregate) |
| **D-10** | `AcceptedStoreSemanticsRemainIntactAcrossCrash` | **genuine** (unmodified suites) | identity, health and publication semantics unchanged | PASS | PASS (in the aggregate) |

**Evidence-class note.** The per-case separation (one receipt per case) is produced by the local
Windows driver `evidence/run-cases.ps1`. The hosted CI step runs the assembly as a whole, so the
hosted per-case evidence is the aggregate count plus the raw job log — not a per-case split. Both
are preserved; neither is presented as the other.

---

## 5. Protected-scope traceability

| Protected item | Status |
| --- | --- |
| Production behaviour | unchanged — 2 documentation files only in `src/` |
| Simulated crash/power-loss | none — the suite observes the real native engine and asserts the real durable state |
| `LatticeDbSharp` | unchanged at `0.2.0` |
| Public API signatures | unchanged — 0 differences over 2 629 dumped member lines |
| CI policies / thresholds / coverage exclusions | unchanged — `.github/` diff empty; thresholds 55 / 75 / 60 / 70 unchanged and met |
| Managed-fault-only limitation | **not discharged** — DF-4 stays open; Q-S62-1 records the bounded limitation |
| macOS native runtime qualification | **not claimed** — Q-S62-5 preserved (the pinned dependency has no supported macOS runtime) |
| S6.3–S6.5 | not implemented |
| Packages / release tags | nothing published; `v0.2.0-preview.7` unchanged |
| Stage-2 qualifications | all preserved |
| S6.1 qualifications | Q-S61-1…Q-S61-8 all preserved |
| Historical evidence | preserved — all prior packages re-verified; pre-existing historical gaps recorded, not repaired |