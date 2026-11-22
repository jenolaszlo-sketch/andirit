# ADR-CI1-01-CI1A-S2-6-WP-S61-IMPL-01 — S6.1 native cancellation path: implementation record

**Commission:** `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01`.
**Project:** Andirit. **Recorded:** 2026-11-22. **Canonical branch:** `main`.
**Owner authorization:** the owner accepted the Stage 6 entry assessment
(`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01`, disposition **READY WITH QUALIFICATIONS**) as the basis for
bounded S6.1 implementation and authorized change set **C-1…C-4**.

> **Status: RECORDED — IMPLEMENTATION COMPLETE, AWAITING INDEPENDENT QUALIFICATION.**
>
> This decision records the *implementation* of the S6.1 native cancellation path. It grants **no
> S6.1 owner acceptance** and **no independent qualification**: the independent qualification is a
> separate commission, `ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01`.

This is an additive record. It changes no production behaviour, no dependency, no public API
signature, no CI policy, no test threshold and no coverage exclusion; it changes no prior decision,
historical review artifact or sealed evidence package. The nine prior sealed packages and the Stage 6
entry package are **not modified** — they are re-verified byte-exact in §1.

## 1. Authorization, baseline and preflight

| Object | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01` |
| Baseline (Stage 6 entry revision) | `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157` (tree `82ce2b873aa17c69fe5da63deb5910fd520c8acd`) |
| Remote `main` at preflight | `b5a81f81fade9807f706b4586329efe416cf1558` |
| Qualified implementation | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` — strict ancestor; `src/` unchanged since it |
| Published tag | `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` — unchanged |
| Release version | `0.2.0-preview.7` — unchanged; nothing published by this commission |
| Sealed evidence packages | **10** lineage packages, **139** entries, **0** mismatches; all **46** repository packages re-verified, with one pre-existing historical gap recorded in §6 |
| CI at preflight | run `44993021436` — `completed` / `success`, `headSha = b5a81f8…` |

Re-derivable from Git objects by `evidence/preflight-receipt.py` (`evidence/preflight.txt`).

## 2. Contract-consistency gate

**Result: PASS — no binding requirement mandates interruption of active native work.**

Method (deterministic, re-runnable, fail closed — `evidence/contract-gate.py`,
`evidence/contract-gate.txt`):

1. the governing excerpts were extracted (D4 invariant 5, the D4 qualification clause, S-02,
   M9.5, M12.2, I-04, Q2-F09);
2. every line of the whole tracked Markdown corpus (518 documents) combining a
   cancellation/interruption term, a native/transaction term **and** a modal verb was enumerated —
   **33 candidate lines**, a superset of every conceivable mandate of native interruption;
3. every candidate was adjudicated against an explicit table: **33/33 adjudicated, 0 unclassified**.

Adjudication summary:

| Category | Count | Meaning |
| --- | --- | --- |
| `A-QUALIFICATION-OBLIGATION` | 13 | requires **native evidence** for the cancellation path (D4 invariant 5 non-exposure, the D4 qualification clause, S-02, M9.5, M12.2) — not an interruption mechanism |
| `B-EXPLICIT-NEGATION` | 6 | forbids claiming interruption, forbids representing a managed timeout as native cancellation, forbids negating a durable outcome |
| `C-RECORDED-LIMITATION` | 9 | records the non-interruptible native window and the conditional upstream dependency change |
| `D-PROCESS-LEVEL-INTERRUPTION` | 2 | "interruption" denotes an analysis-process / store-lifetime interruption (a durability and reopen obligation) |
| `E-OUT-OF-SCOPE-TRACK` | 3 | EC-6C track prose outside the CI-1A S6.1 boundary |

**Conclusion.** The existing wording of D4.5, S-02, M9.5, M12.2 and I-04/Q2-F09 *permits*
qualification through **atomicity**, **non-exposure of partial state** and **truthful outcomes** on
the pinned dependency, without any interruption capability. **No requirement was silently
weakened**: the non-interruptible native window is published in the API contract (C-1, C-2) and is
carried forward as qualification **Q-S61-1**. The bounded owner contract-resolution route was
therefore **not** triggered.

## 3. Implemented change set

| # | Change | Exact files | Kind | Verified by |
| --- | --- | --- | --- | --- |
| **C-1** | Publish the native cancellation contract on the production store: the two honoured checkpoints, the non-cancellable native window, the three truthful outcomes, the obligation to await completion, and the D4 invariant-5 consequence (no partial facts, atomic completion). | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (class-level `<remarks>`, +21 lines) | documentation only | `evidence/api-surface/api-diff.txt` — public API surface byte-identical |
| **C-2** | Publish the same contract once at the interface boundary so that implementers of `ICodeGraphStore` inherit the same semantics. | `src/Andirit/ICodeGraphStore.cs` (`ICodeGraphIndexStore` `<remarks>`, +21 lines) | documentation only | same |
| **C-3** | Add the dedicated native cancel-path regression suite (the M9.5 / M12.2 instrument): 13 tests covering Q-01…Q-12 plus the post-commit-cancellation regression guard. | `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` (new) | test addition | `evidence/case-receipts/` — 13/13 PASS |
| **C-4** | Record this implementation decision, the qualification outcome and the qualifications Q-S61-1…Q-S61-8. | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md` (this document) | documentation | — |

**Production delta:** `git diff <baseline> -- src tests .github` = **2 documentation files, +42 lines**
and **1 new test file**. No behavioural source line, no public signature, no dependency reference,
no workflow line and no threshold changed. The exact diff is `evidence/production-delta.diff`.

**Dependencies:** unchanged — `LatticeDbSharp 0.2.0` (managed assembly SHA-256
`d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46`).

**Public API:** unchanged. The exported types and every public member signature of
`Andirit.Abstractions`, `Andirit` and `Andirit.LatticeDb` were dumped by reflection from the
baseline build and from the implementation build and compared: **2 629 lines, 0 differences**
(`evidence/api-surface/api-diff.txt`).

**Compiled program:** unchanged. The three production assemblies were rebuilt from the baseline and
from the implementation revision and compared type by type, member by member, with attributes,
signature blobs and **IL bytes**: **7 649 metadata+IL lines across three assemblies, identical
SHA-256, 0 differences** (`evidence/dll-comparison.txt`). The module MVID and the PE debug
directory differ because a deterministic build derives them from the source hash; the whole-file
assembly hashes differ for that reason alone. **The change is provably documentation-only.**

**Package contents:** `Andirit.LatticeDb.0.2.0-preview.7.nupkg` was rebuilt from the baseline and
from the implementation revision and compared entry by entry: **6 entries, 5 identical, 1
different** — only `lib/net10.0/Andirit.LatticeDb.dll`, and only in the PE debug directory
(`evidence/package-comparison.txt`). **Erratum to the entry commission's implementation proposal**
(C-4 §4.7): that document states the XML documentation would be "embedded in the assemblies". It is
not — these projects do not enable `GenerateDocumentationFile`, so no XML documentation file is
produced and the package content is entirely unaffected apart from the deterministic debug
directory. The cancellation contract is published in the **source-level API documentation**.

## 4. The published cancellation contract (the substance of C-1/C-2)

1. A cancellation request is honoured **while the operation waits for the managed write gate** and
   **immediately before the native write transaction opens**.
2. It is **not** honoured inside the native transaction: the pinned dependency exposes no
   cancellation or interruption entry point, so a cancellation arriving during active native
   execution is not observed, the transaction commits atomically and the operation **completes**.
3. A cancelled operation never exposes partial facts or index entries, and a durable mutation is
   never reported as cancelled.
4. The truthful outcomes are exactly three: **cancelled before native invocation** (an
   `OperationCanceledException`, no native write occurred), **completed**, or **commit outcome
   unknown** (`CodeGraphCommitOutcomeUnknownException`, after which the instance refuses
   current-state claims until the durable state is corroborated by reopening).
5. A managed timeout, an abandoned task or a disposed caller is **not** a native cancellation and
   must never be reported as one: **callers must await the operation** to learn its outcome.

## 5. Qualification result (Q-01…Q-12)

Executed on **Windows x64** against the production store over real LatticeDB files, with durable
state asserted by reading the native tables directly (not only through the managed projection).
Per-case receipts: `evidence/case-receipts/Q-01.log` … `Q-12.log` and `REG.log`;
summary: `evidence/case-receipts/case-summary.json`.

| Case | Test | Result |
| --- | --- | --- |
| Q-01 | `CancellationBeforeNativeInvocation_LeavesTheDurableStateUnchanged` | PASS |
| Q-02 | `CancellationRacingTheNativeWindow_NeverExposesPartialState` | PASS |
| Q-03 | `CancellationInsideTheOpenNativeTransaction_IsNotObservedAndCommitsAtomically` | PASS |
| Q-04 | `CancellationRacingWithSuccessfulCompletion_NeverReportsCancellationAfterACommit` | PASS |
| Q-05 | `CancellationAfterTheIrreversibleCommit_DoesNotReportCancellation` | PASS |
| Q-06 | `ReopenAfterACancelledStaging_PreservesTheAcceptedPublication` | PASS |
| Q-07 | `ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite` | PASS |
| Q-08 | `DisposalReleasesTheNativeHandle` | PASS |
| Q-09 | `SuccessfulNonCancelledControl_PublishesAndReopens` | PASS |
| Q-10 | `AcceptedStoreSemanticsRemainIntactAcrossACancelledStaging` | PASS |
| Q-11 | `UnknownCommitOutcome_IsReportedAndQuarantinedAndReopenCorroborates` | PASS |
| Q-12 | `PinnedNativeDependency_ExposesNoCancellationEntryPoint` | PASS |
| regression | `Regression_CancellationAfterASuccessfulCommit_MustNotSurfaceAsCancelled` | PASS |

**Constructed versus genuine.** Q-02 and Q-04 are the **genuine** arbitrary-arrival cases (a
concurrent canceller, no seam control); their assertions are invariants that hold for every arrival
time — the staged unit is entirely absent or entirely present, never partial. Q-03 and Q-05 are
**constructed** (the cancellation is placed deterministically inside the open transaction and after
the commit through the existing internal fault-injector seam); the native writes, the transaction
and the commit in those cases are genuine. No arbitrary-timing claim rests on a constructed case.

## 6. Regression, coverage and packaging results

| Gate | Requirement | Result |
| --- | --- | --- |
| Dedicated suite | Q-01…Q-12 + regression | 13/13 PASS, 0 skipped |
| LatticeDb regression suite | unchanged expectations | **88 passed**, 0 failed, 0 skipped (75 historical + 13 new) |
| Core suite (`Andirit.Tests`) | unchanged | **345 passed**, 0 failed, 0 skipped |
| CSharp suite | unchanged | **372 passed**, 0 failed, 0 skipped |
| TreeSitter suite | unchanged | **157 passed**, 0 failed, 0 skipped |
| Whole solution | all projects | **962 passed**, 0 failed, 0 skipped |
| Coverage `Andirit` + `Andirit.Abstractions` | ≥ 55 line/branch | **82.42 % line / 70.89 % branch** (total) — PASS |
| Coverage `Andirit.CSharp` | ≥ 75 line/branch | **95.15 % line / 88 % branch** — PASS |
| Coverage `Andirit.TreeSitter` | ≥ 60 line/branch | **89.85 % line / 75.45 % branch** — PASS |
| Coverage `Andirit.LatticeDb` | ≥ 70 line/branch | **92.87 % line / 78.96 % branch** — PASS |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0, no change — PASS |
| Release build | `dotnet build Andirit.slnx -c Release` | succeeded, 0 warnings, 0 errors |
| Package build | `dotnet pack Andirit.slnx -c Release` | 6 `.nupkg` + 6 `.snupkg` at `0.2.0-preview.7` — nothing published |

**Preserved historical evidence — one pre-existing gap (not introduced by this commission).**
The ten lineage packages are byte-exact (139 entries, 0 mismatches). A sweep of **all 46** repository
packages found **4** manifest entries that are absent from the Git tree: the four coverage XML files
listed by `docs/reviews/CI1A-S2.5-WP-S25-2-G2-EXEC-evidence/manifest.json`. The same 4 entries are
absent at the baseline `b5a81f8…` and at the Stage 6 entry revision `2d1e8ec…`; the package was
created in a single commit `df57a2a`. This is a **preserved historical observation** about a
package outside the S6.1 lineage, recorded rather than silently repaired: no historical evidence is
modified by this commission.

## 7. Hosted execution

The commission requires **genuine hosted Ubuntu x64** execution and forbids any workflow change.
The existing unmodified `.github/workflows/ci.yml` runs `tests/Andirit.LatticeDb.Tests` on the
`ubuntu-latest` matrix leg without a filter, so the new tests execute there without skips. The
hosted result is recorded in `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01.md` §6 and in
`evidence/hosted/`, derived from the raw CI artifacts of the pushed revision.

## 8. Qualifications carried forward

Q-S61-1…Q-S61-8 from the Stage 6 entry assessment remain in force unchanged: Q-S61-1 (non-interruptible
native window), Q-S61-2 (the native window must be awaited), Q-S61-3 (D4.5 discharged by atomicity, not
interruption), Q-S61-4 (`osx-arm64` unqualified — no native runtime evidence), Q-S61-5 (genuine hosted
Linux required for the Linux row), Q-S61-6 (platform-invariant contract, platform-bounded evidence),
Q-S61-7 (scope limited to qualification of existing behaviour), Q-S61-8 (F-02 boundary untouched).
Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4 and DF-1…DF-8 remain in force.

## 9. What this decision does **not** do

- It does **not** grant S6.1 owner acceptance.
- It does **not** discharge M12.1, M12.3, the complete S-02 / D4.5 matrices, DF-4 (crash, power-loss,
  multi-process) or DF-6 (full platform certification).
- It does **not** implement S6.2–S6.5.
- It does **not** claim native interruption, macOS native runtime qualification, or any public API
  change.
