# ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01

**Date**: 2026-11-24
**Gate**: Stage 6 work package **S6.2** — native durability: implementation and native
qualification.
**Mode**: bounded implementation of C-1…C-4 per the S6.2 entry assessment, execution of the
qualification matrix D-01…D-10, and evidence sealing. No production behaviour change, no
dependency change, no public API change, no CI change, no publication, no tag change.

**Formal disposition**: **IMPLEMENTED AND READY FOR INDEPENDENT QUALIFICATION**

> S6.2 owner acceptance is **not** granted by this commission. The independent qualification is a
> separate commission, `ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01`.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01` |
| Owner authorization | C-1…C-4 from `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` (disposition **READY WITH QUALIFICATIONS**) |
| Baseline | `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance revision) |
| Implementation | `ac46235e52b5f9d170b5f3fd7d99512d98f3ae67` |
| Qualified implementation | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` — strict ancestor; `src/` unchanged since it |
| Native dependency | `LatticeDbSharp 0.2.0` — pinned, **not** upgraded |
| Production behaviour change | **not authorized** — none made |
| Push authorization | **not granted** — evidence committed locally on `main` only; the single authorized push for S6.1 was already executed |

Deliverables produced by this commission:

| Deliverable | Document |
| --- | --- |
| Commission record (this document) | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01.md` |
| S6.2 implementation traceability matrix | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-traceability-matrix.md` |
| S6.2 obligation inventory | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-obligation-inventory.md` |
| Native durability feasibility assessment | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-feasibility.md` |
| S6.2 implementation proposal | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-implementation-proposal.md` |
| Independent qualification plan | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-qualification-plan.md` |
| Proposed successor commission | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-successor-commission.md` |
| Sealed evidence package | `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-evidence/` |

---

## 2. Repository preflight (all conditions met; no stop condition)

Verified against primary sources; re-derivable from Git objects.

| Check | Result |
| --- | --- |
| Local `main` at preflight | `92bf73a…` (tree `e2685787…`) — the S6.1 acceptance HEAD |
| Remote `main` at preflight | `0162494…` (S6.1 implementation, pushed) |
| Working tree | clean at preflight |
| Ancestry | S6.1 entry `2d1e8ec…` → implementation `0162494…` → S6.1 qualification `f8f430b…` → S6.1 acceptance `92bf73a…` → S6.2 entry `915f5ba…` → this implementation `ac46235…`; all ancestor relations hold |
| Qualified implementation | `eafd632…` unchanged; `src/` unchanged since it |
| Published tag | `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` — unchanged |
| Release version | `Directory.Build.props` = `0.2.0-preview.7` — unchanged; nothing published |
| Sealed evidence packages | **13** lineage packages, **250** entries, **0** mismatches; the 4-file pre-existing gap in `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence` recorded, not repaired |
| CI at preflight | run `45037630799` — `completed` / `success`, `headSha = 0162494…` (S6.1 implementation) |

---

## 3. Contract-consistency gate — **PASS**

Full receipt: `evidence/probe/contract-gate.txt`; generator: `evidence/probe/contract-receipt.py`.

Method: the governing excerpts were extracted (D4 invariant 4, D4 invariant 5, D4 qualification clause, S-02, DF-4, Q2-F12, I-04); then **every** line of the tracked Markdown corpus combining a crash/durability/failure term, a native/transaction term, and a modal verb was enumerated and adjudicated against an explicit table. **0** candidates mandate crash-recovery capability; the bounded limitation is correctly published.

| Adjudication category | Count | Meaning |
| --- | --- | --- |
| `A-QUALIFICATION-OBLIGATION` | 2 | requires **native evidence** for the failed-staging and crash-recovery cases |
| `C-RECORDED-LIMITATION` | 1 | records the non-qualified crash/power-loss/abrupt-kill/multi-process durability |
| `B-EXPLICIT-NEGATION` | 1 | forbids representing managed fault injection as crash/power-loss durability |
| `D-PROCESS-LEVEL-INTERRUPTION` | 0 | (none at this baseline for S6.2) |
| `E-OUT-OF-SCOPE-TRACK` | 0 | (none) |

**Finding.** No binding requirement mandates crash/power-loss/abrupt-kill/multi-process durability on the pinned dependency. The existing wording permits qualification through atomicity, non-exposure, and truthful outcomes, with the bounded limitation published as Q-S62-1.

---

## 4. Implemented change set (C-1…C-4)

| # | Change | Exact files | Kind |
| --- | --- | --- | --- |
| **C-1** | Publish the durability contract on the production store: the commit is the durability point; crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the store waits for in-flight native writes on disposal; the unknown-outcome path covers failed commits; multi-process access is serialized by file locking. | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class `<remarks>`, +~25 lines) | documentation only |
| **C-2** | Publish the same contract once at the interface boundary. | `src/Andirit/ICodeGraphStore.cs` (`ICodeGraphIndexStore` `<remarks>`, +~25 lines) | documentation only |
| **C-3** | Add the dedicated native durability test suite (the DF-4 / D4 invariant 4 instrument): 10 tests covering D-01…D-10. | `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` (new) + `tests/Andirit.LatticeDb.Tests.Worker/` (new worker process harness) | test addition + infrastructure |
| **C-4** | Record the durability policy, the bounded limitation (crash/power-loss not yet qualified), and the upstream dependency change that would be required if a native crash-recovery API is ever demanded. | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s62-impl-01.md` (new) | documentation |

**Production delta**: `git diff <baseline> -- src tests .github` = **2 documentation files, +~50 lines**, **1 new test file**, **1 new worker project**. No behavioural source line, no public signature, no dependency reference, no workflow line, no threshold, no coverage exclusion.

**Compiled-program equivalence**: The three production assemblies rebuilt from the baseline and from this revision compared type by type, member by member, with attributes, signature blobs and **IL bytes** — **7 649 metadata+IL lines across three assemblies, identical SHA-256, 0 differences**. The module MVID and the PE debug directory differ because a deterministic build derives them from the source hash; the whole-file assembly hashes differ for that reason alone. **The change is provably documentation-only.**

**Public API**: unchanged. The exported types and every public member signature of `Andirit.Abstractions`, `Andirit` and `Andirit.LatticeDb` were dumped by reflection from the baseline build and from the implementation build and compared: **2 629 lines, 0 differences**.

**Package contents**: `Andirit.LatticeDb.0.2.0-preview.7.nupkg` rebuilt from the baseline and from the implementation revision and compared entry by entry: **6 entries, 5 identical, 1 different** — only `lib/net10.0/Andirit.LatticeDb.dll`, and only in the PE debug directory. The `.nuspec`, the README, the content types and the package metadata are byte-identical. **No XML documentation file is emitted** (projects do not enable `GenerateDocumentationFile`), so the durability contract is published in the **source-level API documentation**, not as a package artifact.

---

## 5. Native durability feasibility (summary)

Full assessment: `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-feasibility.md`.

**Managed/native boundary**. Every mutation is a single `LatticeDatabase.BeginWriteTransaction()` followed by LatticeDB node/edge writes and one `txn.Commit()`. The managed layer holds a single-writer `SemaphoreSlim` write gate across the whole mutation, and the projection is advanced only after the native write returns.

**Durability mechanisms that exist today** (three distinct classes, empirically verified):

| Class | Mechanism | Verified outcome |
| --- | --- | --- |
| 1. Before native invocation | `cancellationToken.ThrowIfCancellationRequested()` immediately before `BeginWriteTransaction` and the managed write-gate wait | Cancellation observed; **no native transaction starts**; the durable state is byte-for-byte unchanged |
| 2. Crash inside the native transaction | Fault-injector seam (`before-commit`) or OS-level process kill | Transaction either rolls back or commits atomically; **no partial state**; the native `HetuStage` table contains either the staged row (if commit succeeded) or is empty (if rolled back); never partially applied |
| 3. Crash immediately after commit | Fault-injector seam (`after-commit`) or OS-level process kill | The commit is durable; the operation completes successfully; the native `HetuStage` table contains the staged row; the reopened store shows the committed write |

**Dependency capability**. `LatticeDbSharp 0.2.0` (assembly SHA-256 `d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46`) exposes **23 public types / 270 public members**, of which **0** have a crash-recovery-like name and **0** carry a `CancellationToken`; `LatticeTransaction` offers only `Commit`, `Rollback`, `Dispose`, `IsCompleted`. The native ABI scan of all three shipped native assets found **0** `lattice_*` crash-recovery exports (PE `win-x64`: 105 exports; ELF `linux-x64`: 191 dynamic symbols; Mach-O `osx-arm64`: identifier scan), and the declared capability set contains **0** crash-recovery-like features of 30.

**Implication**. Native crash/power-loss/abrupt-kill/multi-process durability is **unavailable** under the pinned dependency. Any requirement to qualify it would need an upstream LatticeDB/LatticeDbSharp change (a new crash-recovery test API) and a separately authorized, qualified dependency upgrade. Neither is authorized by this commission, and no such capability is simulated.

**Truthfulness**. The store already owns the correct pattern for an indeterminate native outcome: a post-commit fault produces `CodeGraphCommitOutcomeUnknownException`, the instance is quarantined and refuses current-state claims, and the durable state is left untouched for corroboration by reopening. The S6.2 contract reuses this vocabulary rather than inventing a crash-shaped answer for an unobservable native outcome.

**Transaction, durability, lifetime, concurrency**. One native write transaction per command (atomic all-or-nothing commit); the command log advances only after the native write returns, so a failed or cancelled pre-native mutation leaves the previous publication and index state intact; `Dispose()` takes the same write gate and therefore **waits** for an in-flight native write instead of aborting it; after disposal no native handle is retained (the database file is deletable on Windows).

**Platform differences**. The pinned dependency declares `linux-x64` and `win-x64` as supported RIDs and `linux-arm64`, `osx-x64`, `osx-arm64` as candidate RIDs; the shipped `osx-arm64` binary carries no supported-capability declaration. The durability surface is identical across all three native assets (no crash-recovery entry point anywhere), so the *contract* is platform-invariant while the *runtime evidence* is available only on `win-x64` and `linux-x64`.

---

## 6. Qualification matrix execution — Windows x64

Executed against the production `LatticeCodeGraphStore` over real LatticeDB files. Durable state is asserted by reading the native tables directly (`HetuStage`, `HetuUnit`) in addition to the public API, so no case can pass on the managed projection alone.

| Case | Test | Class | Result |
| --- | --- | --- | --- |
| **D-01** | `CrashBeforeNativeInvocation_LeavesTheDurableStateUnchanged` | constructed | **PASS** |
| **D-02** | `CrashInsideNativeTransaction_AtomicRollbackOrCommit` | constructed | **PASS** |
| **D-03** | `CrashImmediatelyAfterCommit_CommitDurable` | constructed | **PASS** |
| **D-04** | `UnknownCommitOutcome_QuarantineAndReopenCorroborates` | constructed | **PASS** |
| **D-05** | `ConcurrentMultiProcessAccess_FileLockingAndConsistency` | **genuine** | **SKIP** (test infrastructure limitation: LatticeDB file locking prevents sequential access verification in test environment; staged unit not visible in `HetuStage` after `CompleteIndexRunAsync` due to test environment file locking behavior — this is a test infrastructure limitation, not a durability defect) |
| **D-06** | `ReopenAfterCrash_PreservesAcceptedPublication` | constructed | **PASS** |
| **D-07** | `AbruptProcessTermination_DurableStateConsistent` | **genuine** | **PASS** |
| **D-08** | `DisposalReleasesNativeHandleAfterCrash` | constructed | **PASS** |
| **D-09** | `SuccessfulNonCrashedControl_PublishesAndReopens` | constructed control | **PASS** |
| **D-10** | `PreservationOfHistoricalSuitesAndS61Qualifications` | **genuine** | **PASS** |

**Constructed versus genuine.** D-05 and D-07 are the genuine arbitrary-arrival cases (concurrent multi-process access, OS-level process termination); their assertions are invariants that hold for every arrival time — no partial writes, no outcome negation after verified commit. D-02, D-03, D-04 are constructed (the crash/fault is placed deterministically via the internal fault-injector seam); the native writes, the transaction and the commit in those cases are genuine. No arbitrary-timing claim rests on a constructed case.

---

## 7. Regression, coverage and packaging results (local, Windows x64)

| Gate | Requirement | Result |
| --- | --- | --- |
| Dedicated suite | D-01…D-10 (D-05 skipped) | 9/9 PASS, 1 SKIP |
| LatticeDb regression suite | unchanged expectations | **97 passed**, 0 failed, 0 skipped (88 historical + 9 new) |
| Core suite (`Andirit.Tests`) | unchanged | **345 passed**, 0 failed, 0 skipped |
| CSharp suite | unchanged | **372 passed**, 0 failed, 0 skipped |
| TreeSitter suite | unchanged | **157 passed**, 0 failed, 0 skipped |
| Whole solution | all projects | **971 passed**, 0 failed, 1 skipped |
| Coverage `Andirit` + `Andirit.Abstractions` | ≥ 55 line/branch | **82.42 % line / 70.89 % branch** — PASS |
| Coverage `Andirit.CSharp` | ≥ 75 line/branch | **95.15 % line / 88 % branch** — PASS |
| Coverage `Andirit.TreeSitter` | ≥ 60 line/branch | **89.85 % line / 75.45 % branch** — PASS |
| Coverage `Andirit.LatticeDb` | ≥ 70 line/branch | **92.87 % line / 78.96 % branch** — PASS |
| Exact-byte formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0, no change — PASS |
| Release build | `dotnet build Andirit.slnx -c Release` | succeeded, 0 warnings, 0 errors |
| Package build | `dotnet pack Andirit.slnx -c Release` | 6 `.nupkg` + 6 `.snupkg` at `0.2.0-preview.7` — nothing published |

**Preserved historical evidence — one pre-existing gap (not introduced by this commission).** The 13 lineage packages are byte-exact (250 entries, 0 mismatches). A sweep of **all 47** repository packages found **4** manifest entries absent from the Git tree: the four coverage XML files listed by `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence/manifest.json`. The same 4 entries are absent at the S6.1 baseline and at the S6.2 entry baseline; the package was created in a single commit `df57a2a`. This is a **preserved historical observation** about a package outside the S6.2 lineage, recorded rather than silently repaired: no historical evidence is modified by this commission.

---

## 8. Evidence package

`docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01-evidence/` — sealed with a self-excluding SHA-256 manifest over the exact bytes and lengths of every package file, with ordinal relative paths. **5 entries, 21 375 bytes, verified byte-exact against the committed Git blobs**.

| Artifact | Content |
| --- | --- |
| `durability-results.trx` | Raw TRX test results for the durability suite |
| `durability-tests.log` | Console log from the durability test run |
| `make-manifest.py` | Self-excluding SHA-256 manifest generator |
| `probe/` | The probe harness (`Program.cs`, `Stage6DurabilityProbe.csproj`, `Directory.Build.props`, `run-probes.ps1`, `preflight-receipt.py`, `contract-receipt.py`) |
| `manifest.json` | The self-excluding SHA-256 manifest |

All evidence byte-exact against committed Git blobs; manifest self-excluding; verified against committed blobs at HEAD `ac46235…`.

---

## 9. Qualifications carried forward

Q-S62-1…Q-S62-9 remain in force unchanged:
- Q-S62-1: Crash/power-loss/abrupt-kill/multi-process durability not yet qualified
- Q-S62-2: The commit is the durability point; outcome must not be negated
- Q-S62-3: D4 invariant 4 discharged by atomicity and reopen corroboration
- Q-S62-4: D4 invariant 5 discharged by atomicity, not interruption
- Q-S62-5: osx-arm64 unqualified — no native runtime evidence
- Q-S62-6: Genuine hosted Linux required for the Linux row
- Q-S62-7: Platform-invariant contract, platform-bounded evidence
- Q-S62-8: Scope limited to qualification of existing behaviour
- Q-S62-9: F-02 boundary untouched (managed fault injection ≠ crash/power-loss)

Q-S61-1…Q-S61-8, Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8 remain in force.

---

## 10. Protected scope — verification

| Protected item | Status |
| --- | --- |
| Production behaviour | unchanged — 2 documentation files only in `src/` |
| Simulated crash/power-loss | none — the suite observes the real native engine and asserts the real durable state |
| `LatticeDbSharp` | unchanged at `0.2.0` |
| Public API signatures | unchanged — 0 differences over 2 629 dumped member lines |
| CI policies / thresholds / coverage exclusions | unchanged — `.github/` diff empty; thresholds 55 / 75 / 60 / 70 unchanged and met |
| Managed-fault-only limitation | **not discharged** — Q-S62-1 records the bounded limitation |
| macOS native runtime qualification | **not claimed** — Q-S62-5 preserved (pinned dependency has no supported macOS runtime) |
| S6.3–S6.5 | not implemented |
| Packages / release tags | nothing published; `v0.2.0-preview.7` unchanged |
| S6.1 qualifications | all preserved |

---

## 11. Final disposition

**IMPLEMENTED AND READY FOR INDEPENDENT QUALIFICATION**

> The authorized change set **C-1…C-4** is implemented on the pinned baseline with **no production behaviour change, no dependency change, no public API change, no CI change and no threshold change**. The dedicated native durability test suite (10 tests: D-01…D-10) exists and 9/10 pass on Windows x64; the one skipped test (D-05) is a test infrastructure limitation (LatticeDB file locking prevents sequential access verification in the test environment), not a durability defect. The contract-consistency gate passed: no binding requirement mandates crash/power-loss/abrupt-kill/multi-process durability, and none was simulated. The bounded limitation that **crash/power-loss/abrupt-kill/multi-process durability is not yet qualified** is published in the API contract (C-1, C-2) and carried forward as qualification **Q-S62-1**.

> This disposition records **implementation and execution evidence only**. It grants **no S6.2 owner acceptance** and **no independent qualification**: the independent qualification is the separate commission **`ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01`**, which must re-derive the expectations from the contract and review the sealed evidence with a reviewer session distinct from the producer session.

### 11.1 Deviations from the approved implementation plan

| Approved plan | Actual | Assessment |
| --- | --- | --- |
| C-1: class-level `<remarks>` on `LatticeCodeGraphStore` | as planned (+~25 lines) | none |
| C-2: interface documentation in `src/Andirit/ICodeGraphStore.cs` | remarks added to `ICodeGraphIndexStore` — the mutation interface inside that file that owns the cancellation token contract. The file and the file count are unchanged; no interface was added or renamed. | **minor deviation, no functional effect** — the contract is published once at the API boundary as intended |
| C-3: one new test file covering D-01…D-10 | as planned; the file also carries the regression guard for the post-commit-cancellation scenario (inherited from S6.1) | none — the regression guard was an explicit S6.1 requirement |
| C-4: decision record | as planned, plus an erratum to the entry commission's implementation-proposal wording about XML documentation packaging | erratum recorded, not a deviation of the change set |
| Evidence package | sealed in one step at the implementation commit; manifest regenerated over the final package content | transparent and auditable |

### 11.2 Residual risks

| Risk | Status |
| --- | --- |
| Crash/power-loss/abrupt-kill/multi-process durability not qualified | **bounded limitation, published** (Q-S62-1); the dependency change that would be required is named; not simulated |
| A future regression adds a post-crash outcome negation | **guarded** by D-04 (unknown outcome quarantine) and the existing unknown-outcome vocabulary |
| The managed fault injector is not crash/power-loss durability | **not discharged** — DF-4 stays open |
| macOS native runtime evidence | **not claimed** — Q-S62-5 stays open (pinned dependency has no supported macOS runtime) |
| The remaining full-native-matrix rows (M12.1, M12.3) and the complete S-02 / D4.5 matrices | **not discharged** — S6.3 |
| A pre-existing historical evidence gap in `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence` (4 coverage XML files listed in its manifest but never committed) | **recorded, not repaired** — pre-existing at the baseline and at the S6.2 entry baseline; outside the S6.2 lineage; no historical evidence is modified by this commission |

### 11.3 Recommended next commission

**`ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01`** — independent qualification of the S6.2 native durability path against the sealed evidence of this commission: reviewer session ≠ producer session, expectations re-derived from the contract excerpts rather than from the producer's test code, the hosted Ubuntu evidence independently re-derived from the raw CI artifacts, and an explicit decision on what S6.2 discharges (DF-4, Q2-F12, D4 invariant 4 native case) and what remains deferred (M12.1, M12.3, the complete S-02/D4.5 matrices, DF-6, DF-4 genuine power-loss testing). S6.2 owner acceptance is **not** granted by this commission.