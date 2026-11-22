# ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01

**Date**: 2026-11-22
**Gate**: Stage 6 work package **S6.1** — native cancellation path: implementation and native
qualification.
**Mode**: bounded implementation of C-1…C-4, execution of the approved qualification matrix
Q-01…Q-12, and evidence sealing. No production behaviour change, no dependency change, no public
API change, no CI change, no publication, no tag change.

**Formal disposition**: **PENDING — hosted execution in progress**

> S6.1 owner acceptance is **not** granted by this commission. The independent qualification is a
> separate commission, `ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01`.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01` |
| Owner authorization | C-1…C-4 from `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` (disposition **READY WITH QUALIFICATIONS**) |
| Baseline | `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157` (Stage 6 entry revision) |
| Qualified implementation | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` — strict ancestor; `src/` unchanged since it |
| Native dependency | `LatticeDbSharp 0.2.0` — pinned, **not** upgraded |
| Production behaviour change | **not authorized** — none made |
| Push authorization | **granted**: one fast-forward push of the verified implementation revision and the preceding entry commit, after all local gates pass |

Deliverables produced by this commission:

| Deliverable | Document |
| --- | --- |
| Commission record (this document) | `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md` |
| C-1…C-4 implementation traceability matrix | `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-traceability-matrix.md` |
| Implementation decision record (C-4) | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md` |
| Sealed evidence package | `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/` |

## 2. Preflight (all conditions met; no stop condition)

Verified against primary sources; re-derivable from Git objects by
`evidence/preflight-receipt.py` (`evidence/preflight.txt`).

| Check | Result |
| --- | --- |
| Local `main` at preflight | `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157` (tree `82ce2b873aa17c69fe5da63deb5910fd520c8acd`) |
| Remote `main` at preflight | `b5a81f81fade9807f706b4586329efe416cf1558` |
| Working tree | clean at preflight (verified before any commission artifact was written) |
| Ancestry | `2d1e8ec…` and `eafd632…` are strict ancestors of the implementation revision |
| Qualified implementation | `eafd632…` unchanged; `src/` unchanged since it |
| Published tag | `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` — unchanged |
| Release version | `0.2.0-preview.7` — unchanged; nothing published |
| Sealed evidence packages | **10** lineage packages, **139** entries, **0** mismatches; all **46** repository packages swept — one pre-existing historical gap recorded in §7 |
| CI at preflight | run `44993021436` — `completed` / `success`, `headSha = b5a81f8…` |

## 3. Contract-consistency gate — **PASS**

Full receipt: `evidence/contract-gate.txt`; generator: `evidence/contract-gate.py`.

Method: the governing excerpts were extracted (D4 invariant 5, the D4 qualification clause, S-02,
M9.5, M12.2, I-04, Q2-F09); then **every** line of the tracked Markdown corpus (518 documents)
combining a cancellation/interruption term, a native/transaction term **and** a modal verb was
enumerated (**33 candidates** — a superset of every conceivable mandate of native interruption) and
adjudicated against an explicit, auditable table (**33/33 adjudicated, 0 unclassified**; the gate
fails closed on any new wording).

| Adjudication category | Count | Meaning |
| --- | --- | --- |
| `A-QUALIFICATION-OBLIGATION` | 13 | requires **native evidence** for the cancellation path — not an interruption mechanism |
| `B-EXPLICIT-NEGATION` | 6 | forbids claiming interruption, forbids representing a managed timeout as native cancellation |
| `C-RECORDED-LIMITATION` | 9 | records the non-interruptible native window and the conditional upstream dependency change |
| `D-PROCESS-LEVEL-INTERRUPTION` | 2 | "interruption" denotes an analysis-process / store-lifetime interruption |
| `E-OUT-OF-SCOPE-TRACK` | 3 | EC-6C track prose outside the CI-1A S6.1 boundary |

**Finding.** No binding requirement explicitly mandates interruption of active native work. The
existing wording of D4.5, S-02, M9.5, M12.2 and I-04/Q2-F09 **permits** qualification through
atomicity, non-exposure of partial state and truthful outcomes on the pinned dependency. The
bounded owner contract-resolution route was **not** triggered.

**No requirement was silently weakened.** The non-interruptible native window is published in the
API contract (C-1, C-2) and carried forward as qualification Q-S61-1.

## 4. Implemented change set (C-1…C-4)

| # | Change | Exact files | Kind |
| --- | --- | --- | --- |
| **C-1** | Publish the native cancellation contract on the production store (two honoured checkpoints; non-cancellable native window; three truthful outcomes; the obligation to await completion; the D4 invariant-5 consequence) | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class `<remarks>`, +21 lines) | documentation only |
| **C-2** | Publish the same contract once at the interface boundary | `src/Andirit/ICodeGraphStore.cs` (`ICodeGraphIndexStore` `<remarks>`, +21 lines) | documentation only |
| **C-3** | Add the dedicated native cancel-path regression suite (13 tests: Q-01…Q-12 plus the post-commit-cancellation regression guard) | `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` (new) | test addition |
| **C-4** | Record the implementation decision, the qualification outcome and Q-S61-1…Q-S61-8 | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md` (new) | documentation |

**Production delta**: `git diff <baseline> -- src tests .github` = **2 documentation files, +42 lines**
and **1 new test file**. No behavioural source line, no public signature, no dependency reference,
no workflow line, no threshold, no coverage exclusion (`evidence/production-delta.diff`).

**Compiled-program equivalence.** The three production assemblies were rebuilt from the baseline
(`2d1e8ec…`) and from the implementation revision and compared at the level of the compiled program
(`evidence/dll-comparison.txt`, `evidence/tools/dllcompare/`): every type, field, method, property
and event with its attributes, signature blob and **IL bytes** — **7 649 metadata+IL lines across
three assemblies, identical SHA-256, 0 differences**. The module MVID and the PE debug directory
are excluded because a deterministic build derives them from the source hash; the whole-file
assembly hashes differ for that reason alone. **The change is provably documentation-only.**

**Public API**: unchanged. The exported types and every public member signature of
`Andirit.Abstractions`, `Andirit` and `Andirit.LatticeDb` were dumped by reflection from the
baseline build and from the implementation build and compared: **2 629 lines, 0 differences**
(`evidence/api-surface/api-diff.txt`).

**Package contents**: `Andirit.LatticeDb.0.2.0-preview.7.nupkg` was rebuilt from the baseline and
from the implementation revision and compared entry by entry (`evidence/package-comparison.txt`):
**6 entries, 5 identical, 1 different** — only `lib/net10.0/Andirit.LatticeDb.dll`, and only in the
PE debug directory, exactly as the semantic comparison above predicts. The `.nuspec`, the README,
the content types and the package metadata are byte-identical. **No XML documentation file is
emitted** by these projects (`GenerateDocumentationFile` is not enabled), so the cancellation
contract is published in the **source-level API documentation** and not as a package artifact —
this corrects the entry commission's implementation-proposal wording, which said the XML
documentation would be "embedded in the assemblies"; it is not, and the package content is
therefore entirely unaffected apart from the deterministic debug directory.

## 5. Qualification matrix execution — Windows x64

Executed against the production `LatticeCodeGraphStore` over real LatticeDB files. Durable state is
asserted by reading the native tables directly (`HetuStage`, `HetuUnit`) in addition to the public
API, so no case can pass on the managed projection alone. Per-case receipts:
`evidence/case-receipts/Q-01.log` … `Q-12.log`, `REG.log`, `case-summary.json`.

| Case | Test | Class | Result |
| --- | --- | --- | --- |
| Q-01 | `CancellationBeforeNativeInvocation_LeavesTheDurableStateUnchanged` | constructed | **PASS** |
| Q-02 | `CancellationRacingTheNativeWindow_NeverExposesPartialState` | **genuine** | **PASS** |
| Q-03 | `CancellationInsideTheOpenNativeTransaction_IsNotObservedAndCommitsAtomically` | constructed (deterministic, inside the open transaction) | **PASS** |
| Q-04 | `CancellationRacingWithSuccessfulCompletion_NeverReportsCancellationAfterACommit` | **genuine** | **PASS** |
| Q-05 | `CancellationAfterTheIrreversibleCommit_DoesNotReportCancellation` | constructed | **PASS** |
| Q-06 | `ReopenAfterACancelledStaging_PreservesTheAcceptedPublication` | constructed | **PASS** |
| Q-07 | `ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite` | constructed | **PASS** |
| Q-08 | `DisposalReleasesTheNativeHandle` | constructed | **PASS** |
| Q-09 | `SuccessfulNonCancelledControl_PublishesAndReopens` | constructed control | **PASS** |
| Q-10 | `AcceptedStoreSemanticsRemainIntactAcrossACancelledStaging` | constructed control | **PASS** |
| Q-11 | `UnknownCommitOutcome_IsReportedAndQuarantinedAndReopenCorroborates` | constructed | **PASS** |
| Q-12 | `PinnedNativeDependency_ExposesNoCancellationEntryPoint` | static | **PASS** |
| regression | `Regression_CancellationAfterASuccessfulCommit_MustNotSurfaceAsCancelled` | constructed | **PASS** |

**Constructed versus genuine.** Q-02 and Q-04 are the genuine arbitrary-arrival cases (a concurrent
canceller, no seam control); their assertions are invariants that hold for every arrival time — the
staged unit is entirely absent or entirely present, never partial. Q-03 and Q-05 are constructed
(the cancellation is placed deterministically inside the open transaction and after the commit
through the existing internal fault-injector seam); the native writes, the transaction and the
commit in those cases are genuine. No arbitrary-timing claim rests on a constructed case.

## 6. Hosted execution — genuine Ubuntu x64

**PENDING.** The existing unmodified `.github/workflows/ci.yml` runs
`tests/Andirit.LatticeDb.Tests` on the `ubuntu-latest` matrix leg without a filter, so the new
tests execute there without skips. No workflow, policy or threshold was modified. The hosted result
and the raw CI artifacts are recorded here and in `evidence/hosted/` after the authorized
fast-forward push.

## 7. Regression, coverage and packaging results (local, Windows x64)

| Gate | Requirement | Result |
| --- | --- | --- |
| Dedicated suite | Q-01…Q-12 + regression | 13/13 PASS, 0 skipped |
| LatticeDb regression suite | unchanged expectations | **88 passed**, 0 failed, 0 skipped (75 historical + 13 new) |
| Core suite (`Andirit.Tests`) | unchanged | **345 passed**, 0 failed, 0 skipped |
| CSharp suite | unchanged | **372 passed**, 0 failed, 0 skipped |
| TreeSitter suite | unchanged | **157 passed**, 0 failed, 0 skipped |
| Whole solution | all projects | **962 passed**, 0 failed, 0 skipped |
| Coverage `Andirit` + `Andirit.Abstractions` | ≥ 55 line/branch | **82.42 % line / 70.89 % branch** — PASS |
| Coverage `Andirit.CSharp` | ≥ 75 line/branch | **95.15 % line / 88 % branch** — PASS |
| Coverage `Andirit.TreeSitter` | ≥ 60 line/branch | **89.85 % line / 75.45 % branch** — PASS |
| Coverage `Andirit.LatticeDb` | ≥ 70 line/branch | **92.87 % line / 78.96 % branch** — PASS |
| Exact-byte formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0, no change — PASS |
| Release build | `dotnet build Andirit.slnx -c Release` | succeeded, 0 warnings, 0 errors |
| Package build | `dotnet pack Andirit.slnx -c Release` | 6 `.nupkg` + 6 `.snupkg` at `0.2.0-preview.7` — nothing published |

**Preserved historical evidence — one pre-existing gap (not introduced by this commission).** The
ten lineage packages are byte-exact (139 entries, 0 mismatches). A sweep of **all 46** repository
packages found **4** manifest entries absent from the Git tree: the four coverage XML files listed
by `docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence/manifest.json`. The same 4 entries are absent
at the baseline `b5a81f8…` and at the Stage 6 entry revision `2d1e8ec…`; the package was created in
a single commit `df57a2a`. This is a **preserved historical observation** about a package outside the
S6.1 lineage, recorded rather than silently repaired: no historical evidence is modified by this
commission.

## 8. Evidence package

`docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01-evidence/` — sealed with a self-excluding SHA-256
manifest over the exact bytes and lengths of every package file, with ordinal relative paths.

| Artifact | Content |
| --- | --- |
| `preflight.txt` | Git-object-derived preflight receipt (re-generated by `preflight-receipt.py`) |
| `contract-gate.txt` | contract-consistency gate receipt (re-generated by `contract-gate.py`) |
| `environment-windows.txt` | Windows x64 execution environment and pinned native dependency identity |
| `environment-ubuntu.txt` | genuine hosted Ubuntu x64 environment identity |
| `case-receipts/` | the Q-01…Q-12 and regression receipts plus the machine-readable case summary |
| `suite-receipts/` | the four suite logs, the whole-solution log, the format, build and pack logs |
| `coverage/` | the four Cobertura coverage reports |
| `api-surface/` | the baseline and implementation public API dumps and their diff |
| `hosted/` | the raw CI artifacts and the independently derived hosted results |
| `production-delta.diff` | the exact `src`/`tests`/`.github` delta against the baseline |
| `manifest.json` | the self-excluding SHA-256 manifest |

## 9. Qualifications carried forward

Q-S61-1…Q-S61-8 remain in force unchanged: Q-S61-1 (non-interruptible native window), Q-S61-2 (the
native window must be awaited), Q-S61-3 (D4.5 discharged by atomicity, not interruption), Q-S61-4
(`osx-arm64` unqualified — no native runtime evidence), Q-S61-5 (genuine hosted Linux required for
the Linux row), Q-S61-6 (platform-invariant contract, platform-bounded evidence), Q-S61-7 (scope
limited to qualification of existing behaviour), Q-S61-8 (F-02 boundary untouched). Q-L1…Q-L8,
MR-1…MR-4, GH-1…GH-4 and DF-1…DF-8 remain in force.

## 10. Protected scope — verification

| Protected item | Status |
| --- | --- |
| Production behaviour | unchanged — 2 documentation files only in `src/` |
| Simulated native interruption | none |
| `LatticeDbSharp` | unchanged at `0.2.0` |
| Public API signatures | unchanged — 0 differences over 2 629 member lines |
| CI policies / thresholds / coverage exclusions | unchanged — `.github/` diff empty; thresholds 55 / 75 / 60 / 70 unchanged and met |
| Managed-fault-only limitation | not discharged — DF-4 stays open |
| macOS native runtime qualification | not claimed — Q-S61-4 preserved |
| S6.2–S6.5 | not implemented |
| Packages / release tags | nothing published; `v0.2.0-preview.7` unchanged |
| Stage-2 qualifications | all preserved |

## 11. Final disposition

**PENDING** — set after the hosted Ubuntu x64 execution (§6) is complete and sealed.
