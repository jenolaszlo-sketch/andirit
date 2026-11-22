# S6.1 implementation traceability matrix — ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01

**Date**: 2026-11-22
**Baseline**: `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157` (Stage 6 entry revision)
**Purpose**: trace every authorized change **C-1…C-4** to its exact location, its contract
justification and its independent verification; and trace every requirement the implementation
claims to discharge to the instrument that demonstrates it.

---

## 1. Change traceability (C-1…C-4)

| # | Authorized change | Exact location | Kind | Contract justification | Independent verification | Deviation |
| --- | --- | --- | --- | --- | --- | --- |
| **C-1** | Publish the native cancellation contract on the production store | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`, class-level `<remarks>`, two new `<para>` elements (21 added lines) | documentation only | I-04 "cancellation policy specified"; D4 invariant 5 consequence; prevents the false-cancellation reading | `evidence/api-surface/api-diff.txt` — public API surface byte-identical to the baseline (2 629 lines, 0 differences) | none |
| **C-2** | Publish the same contract once at the interface boundary | `src/Andirit/ICodeGraphStore.cs`, `ICodeGraphIndexStore` `<remarks>` (21 added lines) | documentation only | the contract belongs at the API boundary, not only at one implementation | same | the implementation proposal named the file `src/Andirit/ICodeGraphStore.cs`; the file exists and holds `ICodeGraphIndexStore` (the mutation interface carrying the cancellation tokens), `ICodeGraphReader` and `ICodeGraphStore` — the remarks were added to `ICodeGraphIndexStore`, the interface that owns the mutation contract. No file was added or renamed. |
| **C-3** | Add the dedicated native cancel-path regression suite | `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` (new; 13 tests) | test addition | M9.5 "Dedicated native cancel-path test"; M12.2 native cancelled-staging case | `evidence/case-receipts/` — 13/13 PASS, exit 0, 0 skipped | none |
| **C-4** | Record the qualification outcome, the qualifications and the upstream dependency note | `docs/decisions/adr-ci1-01-ci1a-s2-6-wp-s61-impl-01.md` (new) | documentation | owner-visible disposition | this matrix §2 and §3 | none |

**Change-set boundary check.** `git diff <baseline> -- src tests .github` contains exactly two
documentation files and one new test file — **no behavioural source line, no public signature, no
dependency reference, no workflow line, no threshold, no coverage exclusion** (exact diff:
`evidence/production-delta.diff`).

**Compiled-program equivalence.** The three production assemblies were rebuilt from the baseline and
from the implementation revision and compared type by type, member by member, with attributes,
signature blobs and IL bytes: **7 649 metadata+IL lines across three assemblies, identical SHA-256,
0 differences** (`evidence/dll-comparison.txt`). The module MVID and the PE debug directory are
excluded because a deterministic build derives them from the source hash. The change is provably
documentation-only.

**Package contents.** `Andirit.LatticeDb.0.2.0-preview.7.nupkg` rebuilt from the baseline and from
the implementation revision: **6 entries, 5 identical, 1 different** — only
`lib/net10.0/Andirit.LatticeDb.dll`, and only in the PE debug directory
(`evidence/package-comparison.txt`). No XML documentation file is emitted by these projects, so the
contract is published in the source-level API documentation, not as a package artifact.

## 2. Requirement traceability — what S6.1 discharges

| Requirement | Source | Exact wording relied on | Instrument | Evidence | Status |
| --- | --- | --- | --- | --- | --- |
| **D4 invariant 5** (cancelled staging exposes no partial data) | owner decision §D.4 | *"Cancelled staging must not expose partial facts or index entries."* | Q-01, Q-02, Q-03, Q-04 (native table reads of `HetuStage` / `HetuUnit` after disposal) | `evidence/case-receipts/Q-01.log` … `Q-04.log` | **discharged for the cancellation path** — non-exposure proven natively; the guarantee is non-exposure, not interruption |
| **D4 qualification clause** (actual production storage and APIs) | owner decision §D.4 | CI-1A must reproduce the applicable cancellation tests *"using actual production storage and APIs"* | the whole suite runs against `LatticeCodeGraphStore` over real LatticeDB files; durable state read directly through `LatticeDbSharp` | `evidence/case-receipts/`, `evidence/native-dependency-identity.txt` | **discharged for the cancellation path** |
| **S-02** (native matrix includes a cancellation path and an unknown-outcome case) | CI1A specification; S-01 obligation reconciliation | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* | Q-01…Q-12 (cancellation path, unknown outcome, reopen, concurrency with disposal) | `evidence/case-receipts/` | **partly discharged** — the cancellation and unknown-outcome rows are exercised; the remaining ten-invariant native matrix (M12.1, M12.3) stays with S6.3 |
| **M9.5** (dedicated native cancel-path test) | S2.5 qualification matrix §9 | *"Stage 1 cancellation behavior preserved — Dedicated native cancel-path test"*; evidence class `E / no`; owner Stage 6 | `NativeCancellationPathTests` — 13 dedicated native cancel-path tests | `evidence/case-receipts/case-summary.json` | **discharged as an instrument** (execution evidence produced; independent qualification is the successor commission) |
| **M12.2** (native cancelled-staging case) | S2.5 qualification matrix §12 | D4 invariant 5 *"Explicitly deferred to Stage 6"*; *"Cancel path test native"* | Q-01, Q-02, Q-03, Q-04 — native `HetuStage` / `HetuUnit` assertions | `evidence/case-receipts/Q-01.log` … `Q-04.log` | **discharged as an instrument** |
| **I-04** (cancellation policy specified) | obligation register | cancellation remains unqualified; owner Stage 6 | C-1 and C-2 publish the policy in the API contract | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`, `src/Andirit/ICodeGraphStore.cs`; `evidence/api-surface/api-diff.txt` | **discharged** — the policy is specified in the published contract |
| **Q2-F09** (rebuild blocks queries; no cancellation) | owner decision §G.2 | addressed by D4 (maintenance policy; CI-1A must qualify a production-backed rebuild path) | C-1/C-2 publish the policy; Q-01…Q-12 qualify it natively | as above | **cancellation aspect addressed**; the rebuild-path aspect remains a D4/S6.3 matter |

## 3. Requirement traceability — what S6.1 explicitly does **not** discharge

| Requirement / obligation | Why it stays open |
| --- | --- |
| **M12.1, M12.3** (remaining full-native-matrix rows) | different rows of the ten-invariant matrix; owned by S6.3 |
| **The complete S-02 / D4.5 matrices** | S6.1 covers the cancellation path only; the full matrix is S6.3 |
| **DF-4** (crash, power-loss, multi-process durability) | not exercised; the managed fault injector is not crash/power-loss durability |
| **DF-6** (full platform certification) | only `win-x64` and genuine hosted `linux-x64` are in scope; macOS native runtime evidence is unavailable (Q-S61-4) |
| **I-04 aspects other than cancellation** | the cancellation policy is specified; the remaining obligation aspects are unchanged |
| **S6.2–S6.5** | out of scope by owner instruction |

## 4. Qualification-matrix traceability (Q-01…Q-12)

Hosted column: CI run `45037630799`, job `test (ubuntu-latest)`, job id `137254795969`,
`headSha = 01624943233f16447c3176b7c905c544642dfb2c` (the implementation revision), runner
labels `['ubuntu-latest']`, workspace `/home/runner/work/andirit/andirit`, .NET SDK 10.0.404. The
hosted step runs the whole `Andirit.LatticeDb.Tests` assembly, which contains all thirteen tests
below; the hosted LatticeDb count moved from **75 → 88** (delta exactly 13 = the 13 `[Fact]`
methods), **0 skipped**.

| Case | Test in `NativeCancellationPathTests` | Constructed / genuine | Native observation | Windows x64 | Hosted Ubuntu x64 |
| --- | --- | --- | --- | --- | --- |
| Q-01 | `CancellationBeforeNativeInvocation_LeavesTheDurableStateUnchanged` | constructed | `HetuStage`/`HetuUnit` empty, no publication, reopen healthy | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-02 | `CancellationRacingTheNativeWindow_NeverExposesPartialState` | **genuine** | whole staged unit present or absent, never partial | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-03 | `CancellationInsideTheOpenNativeTransaction_IsNotObservedAndCommitsAtomically` | constructed (deterministic, inside the open transaction) | complete commit, `HetuStage` = 1, reopen healthy | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-04 | `CancellationRacingWithSuccessfulCompletion_NeverReportsCancellationAfterACommit` | **genuine** | cancelled ⇒ nothing durable; completed ⇒ whole unit durable | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-05 | `CancellationAfterTheIrreversibleCommit_DoesNotReportCancellation` | constructed (after the commit) | durable write, success reported | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-06 | `ReopenAfterACancelledStaging_PreservesTheAcceptedPublication` | constructed | publication tuple and unit count preserved across reopen | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-07 | `ConcurrentCancellationAndDisposal_WaitsForTheInFlightNativeWrite` | constructed | disposal waits; the in-flight write completes; the waiting operation is cancelled | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-08 | `DisposalReleasesTheNativeHandle` | constructed | database file deletable after disposal; clean reopen | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-09 | `SuccessfulNonCancelledControl_PublishesAndReopens` | constructed control | publication, node resolution and reopen | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-10 | `AcceptedStoreSemanticsRemainIntactAcrossACancelledStaging` | constructed control | identity, health and publication semantics unchanged | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-11 | `UnknownCommitOutcome_IsReportedAndQuarantinedAndReopenCorroborates` | constructed | `CodeGraphCommitOutcomeUnknownException`, quarantine, reopen corroboration | PASS | PASS (in the 88 / 0-skipped aggregate) |
| Q-12 | `PinnedNativeDependency_ExposesNoCancellationEntryPoint` | static | managed member census of the pinned assembly | PASS | PASS (in the 88 / 0-skipped aggregate) |
| regression | `Regression_CancellationAfterASuccessfulCommit_MustNotSurfaceAsCancelled` | constructed | durable write reported as success, never as cancelled | PASS | PASS (in the 88 / 0-skipped aggregate) |

**Evidence-class note.** The per-case separation (one receipt per case) is produced by the local
Windows driver `evidence/run-cases.ps1`. The hosted CI step runs the assembly as a whole, so the
hosted per-case evidence is the aggregate count plus the raw job log — not a per-case split. Both
are preserved; neither is presented as the other.

## 5. Protected-scope traceability

| Protected item | Status |
| --- | --- |
| Production behaviour | unchanged — 2 documentation files only in `src/` |
| Simulated native interruption | none — the suite observes the real native engine and asserts the real outcome |
| `LatticeDbSharp` | unchanged at `0.2.0` |
| Public API signatures | unchanged — 0 differences over 2 629 dumped member lines |
| CI policies / thresholds / coverage exclusions | unchanged — `.github/` diff empty; thresholds 55 / 75 / 60 / 70 unchanged and met |
| Managed-fault-only limitation | not discharged — DF-4 stays open and is restated in the decision record |
| macOS native runtime qualification | not claimed — Q-S61-4 preserved |
| S6.2–S6.5 | not implemented |
| Packages / release tags | nothing published; `v0.2.0-preview.7` unchanged |
| Stage-2 qualifications | Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8, Q-S61-1…Q-S61-8 all preserved |
| Historical evidence | preserved — all prior packages re-verified; one pre-existing historical gap recorded, not repaired |
