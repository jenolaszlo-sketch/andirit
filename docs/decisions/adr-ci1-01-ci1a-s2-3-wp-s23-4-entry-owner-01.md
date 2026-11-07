# ADR-CI1-01-CI1A-S2.3-WP-S23-4-ENTRY-OWNER-01 — WP-S23-4 entry authorization (native lifecycle qualification with real facts)

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-4-ENTRY-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Owner decision:** **`WP-S23-4 ENTRY AUTHORIZED WITH QUALIFICATIONS`**.
**Gate:** **`S23-LIFE` / WP-S23-4 (native lifecycle qualification with real facts) is authorized to enter, bounded to tests and evidence only, under the qualification matrix and constraints below.**
**Independent gate:** **`S23-LIFE-Q1`** (separate reviewer ≠ implementer), followed by a separate `S23-LIFE-OWNER` decision.

> **Status: RECORDED — OWNER-AUTHORIZED (entry only).**
>
> This decision authorizes the *entry* of WP-S23-4 as a bounded, tests-and-evidence-only
> qualification package. It does **not** execute qualification, accept any native
> lifecycle result, authorize a production change, or authorize WP-S23-5, S2.4,
> S2.5, or Stage 6. It records the exact qualification matrix, native execution
> requirements, known limitations, and the `S23-LIFE-Q1` commission boundary.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Authority, exact baseline, and ancestry

Controlling authority: the [S2.3 work-package plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md) (WP-S23-4 row and dependency order), the [native publication / lifecycle test matrix](../planning/adr-ci1-01-ci1a-s2-3/native-lifecycle-test-matrix.md) (L1–L20, D4 mapping, platform, SQ-04), the [S2.3 independent-qualification plan](../planning/adr-ci1-01-ci1a-s2-3/independent-qualification-plan.md), [Stage 2 boundary §8](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md), the [S2.3 scope inventory](../planning/adr-ci1-01-ci1a-s2-3/scope-and-authority-inventory.md) (D3/D4/D5, SQ-02/SQ-03), and the accepted owner decisions [NEW-01](adr-ci1-01-ci1a-s2-3-new-01-owner-01.md), [WP-S23-2](adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md), and [WP-S23-3A](adr-ci1-01-ci1a-s2-3-wp-s23-3a-owner-01.md).

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| WP-S23-1 (NEW-01) accepted baseline | `b576acb004747c89d257d8e61721c17babe6c5a6` | `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` |
| WP-S23-2 (S23-DUR) accepted baseline | `80f61774bf7f8205ec1f8fb27d0ac1c69aee4bfe` | — |
| WP-S23-3A publication integration | `e2b833a35e74d7fa8b1bf9eabe506af0357902b3` | `a196cb2ce21163c9b45c2abd03f11fd7d4754382` |
| R1 completed-publication reconstruction | `e0cef1c94f68f2a24a529e1182c0de5862e383c8` | `f6284492bdc08e786e78ddac4b47e429bacff4be` |
| R2 staged-run reconstruction | `a4d85011b8b753d4b058ed3a2a02fe2ebc73294c` | `cfbf67eb4d38999c3b95e0186655915461ff732b` |
| S23-PUB-Q2 independent requalification | `36657172197370603aafe1adf3d8e31a29b0f03c` | `3998cea5f41d98cb845bbd44af465707e1b322a2` |
| **Entry baseline (this decision's HEAD)** | **`4a775c88de49d0f65be04c79b942b99cb35bfe0f`** | **`ab236e70d43c0c5d82122904547c5a92ffb05252`** |

The owner began on clean canonical `main` at `4a775c8`, one worktree (`D:/projects/Andirit`), no stashes, nothing pushed. No fetch, branch, additional worktree, reset/restore, rebase, prune, or publication was performed.

## 2. Prerequisites verified

| # | Prerequisite | Status |
| --- | --- | --- |
| 1 | WP-S23-1 (NEW-01 recovery) owner-accepted | satisfied (`adr-ci1-01-ci1a-s2-3-new-01-owner-01.md`) |
| 2 | WP-S23-2 (S23-DUR carrier/reopen) owner-accepted with qualifications | satisfied (`adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md`) |
| 3 | WP-S23-3A (real-fact publication + R1/R2 durability) owner-accepted with qualifications | satisfied (`adr-ci1-01-ci1a-s2-3-wp-s23-3a-owner-01.md`) |
| 4 | Real-fact publication path exists through the accepted chain | satisfied (`e2b833a`; independently qualified by S23-PUB-Q1/Q2) |
| 5 | Native store + occurrence tables + reader exist (Stage 1) | satisfied (`LatticeCodeGraphStore`, `AndiritOccurrenceUnit/Page/Meta`, `ICodeOccurrenceReader`) |
| 6 | Independent qualification gate defined (`S23-LIFE-Q1`) | satisfied (independent-qualification plan §1) |
| 7 | Whole solution green, frozen oracle `185/185` unchanged | satisfied per the accepted S23-PUB-Q2 evidence at `3665717` (865 / 0 / 0; oracle `146,851` bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`); the WP-S23-3A owner decision added documentation only |
| 8 | No unresolved design boundary blocks entry | satisfied — the plan defines scope, allowed changes, and gate; no Option-B-class escalation is required |

The next explicitly planned bounded package is WP-S23-4; there is no authorized WP-S23-3B.

## 3. Authorized and excluded scope

**Authorized (tests and evidence only; no production change):**

1. Add bounded lifecycle tests and fixtures that adapt the Stage 1 native tests (N1–N8) to **real S2.2 facts** and cover the L1–L20 matrix.
2. Execute genuine **native** qualification scenarios against real `LatticeCodeGraphStore` databases (Windows x64).
3. Produce independent source and runtime evidence with complete raw receipts.
4. Verify lifecycle outcomes (publication/replacement/deletion/reopen/schedule/coverage/refusal) and refusal behavior.
5. Reuse accepted WP-S23-1/2/3A evidence (including S23-PUB-Q2) where it already satisfies a case; add only the evidence genuinely missing.
6. Implement the **SQ-03 native schedule test** over the accepted serialized `_writeGate` model (no MVCC claim).

**Excluded / not authorized:**

- Any production change; any change that weakens D3/D4/D5; any frozen-identity or schema-format change.
- A new public API, a persisted-format change, or any occurrence-authority contract change.
- Public Find Usages; WP-S23-5; S2.4; S2.5; Stage 6; crash/power-loss/multi-process recovery; cross-platform certification (Linux suite remains an S2.5 obligation).
- Fabricating historical fixtures (SQ-04) or treating a managed simulation as a native result.

If the qualification finds a **production defect**, the reviewer records it and **stops** rather than repairing it in this package.

## 4. Qualification matrix (implementation-independent)

Cases and expected outcomes are taken verbatim from the accepted plan (`native-lifecycle-test-matrix.md` L1–L20, D4 mapping) and the accepted test vocabulary. "Existing evidence to reuse" is the accepted, already-qualified surface; "Required new `S23-LIFE-Q1` evidence" is what WP-S23-4 must add. "Class" distinguishes a genuine **native** execution from a deterministic control that does not require a native lifecycle run.

| Case | Requirement / expected | Existing evidence to reuse | Required new S23-LIFE-Q1 evidence | Class |
| --- | --- | --- | --- | --- |
| L1 Initial publication | Unit/page/meta persisted; graph+occurrence same transaction | `OccurrenceStage1NativeTests.N1_*`; `S23PublicationIntegrationTests.RealCrossProjectFact_*` | Real-fact native unit/page/meta inspection in one transaction | native |
| L2 Same-unit replacement | Obsolete facts retired; other units preserved; pages rebuilt; `Examined` = page size | `N3_ReplacementRetiresObsoleteFactsAndPreservesOtherUnits` | Replacement after a **real source edit** with real identities | native |
| L3 Source deletion | Deleted unit's facts removed; remaining preserved; pages rebuilt | `N4_ContributorUnionAndScopedDeletionPreserveRemainingContributors` | Deletion of a **real** source unit/file | native |
| L4 Contributor union | Identical facts union contributors; distinct semantic units stay distinct | `N4_*` | Real **cross-unit** union → 1 fact, 2 contributors | native |
| L5 Contributor conflict refusal | Same identity, different payload rejected at staging; prior state intact | `N4_*` conflict leg; R2 `ConflictingStagedIdentity_IsRefused` | Real-fact conflict rejection with prior publication retained | native |
| L6 Valid complete-empty | `Produced` zero-fact unit persists; reopen restores | `OccurrenceRemediationNativeTests.CompleteEmptyAnalysisSurvivesReplacementAndReopen`; `S23NativeAuthorityTests.CompleteEmpty_*` | Real-fact complete-empty persist + reopen | native |
| L7 Native reopen | Projection reconstructed; byte-equivalent ordered results | `N5_ReopenPreservesOccurrenceAndIndexState`; R1/R2 reopen suites | Real-fact reopen byte-equivalence | native |
| L8 Stale publication refusal | Stale handle → `StalePublication`; no hits | `N7_*`; `S23PublicationIntegrationTests.TransitiveDependencyChange_*`; S23-PUB-Q2 P15 | Real-fact stale handle → explicit status, no hits | native |
| L9 Transactional graph/occurrence consistency | Failed completion preserves prior publication | `N6_FailedCompletionPreservesPriorOccurrencePublication` | Real-fact before-commit fault; prior intact, no partial | native (managed fault) |
| L10 Recovery & repeatability | Known pre-commit failure recoverable; reopen durable; no blind retry | `New01RecoveryProjectionTests` (accepted) | Recovery projection correspondence for the real-fact path | native |
| L11 Real source modification & re-index | Replacement after actual source change; identity/coverage correct; stale invalidated | — | New: real source change → new identities, supersession correct | native |
| L12 Old-or-new complete visibility (SQ-03) | Readers see a complete publication (old or new), never a partial blend | `_writeGate` serialized model (accepted) | New native schedule test under the serialized model | native |
| L13 Partial/unavailable coverage | `Produced`/`Partial`/unavailable distinguished and persisted | `IncompleteCoverageCannotMasqueradeAsCompleteEmpty`; `NoAnalysisWithZeroFactsRemainsUnavailable` | Real-fact coverage classification | native |
| L14 Deletion invalidates coverage | Deleting the last analyzed unit → `AnalysisUnavailable` | `DeletingACompleteUnitInvalidatesCoverage` | Real-fact deletion → unavailable | native |
| L15 Schema/namespace refusal | Unsupported schema/namespace and units-without-meta refused on open | `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`; `Store_RejectsOccurrenceUnitsWithoutMeta` (unchanged; D5) | Confirm unchanged; no new fabrication | deterministic control |
| L16 D3 graph noninterference | Graph nodes/edges/counts/traversal unchanged across occurrence lifecycle | `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`; S2.2 D3 evidence | Real-fact graph snapshots identical across the lifecycle | native |
| L17 Reopen stale-defining-`compctx2` refusal | Test 8b: stale refused; current usable | `S23NativeAuthorityTests.Test8b_TransitiveOnlyChange_*` (WP-S23-2) | Reuse; confirm real-fact reopen path | native |
| L18 Real cross-project target identity | `asmctx3` from the dependency's verified defining compilation; persisted; reopened | S2.2 WP8 corpus; S23-PUB-Q2 P02/P13 | Persisted real target inspection after reopen | native |
| L19 Recorded-context missing/corrupt refusal | Missing/corrupted persisted provenance refuses conservatively | `S23NativeAuthorityTests.MalformedOrSubstitutedAuthority_*`; R2 `MalformedStagedRecord_IsRefused` | Reuse; real-fact variant where not covered | native |
| L20 Unknown-outcome quarantine | Post-commit/ack-loss quarantines; reopen reconciles | `AfterCommitAcknowledgementLoss…`; `New01RecoveryProjectionTests.UnknownOutcome_*` | Reuse; real-fact reopen reconciliation | native (managed fault) |

**N1–N8-equivalent native requirements** (Stage 2 boundary §8): N1↔L1, N2↔L2 (ordering/limits), N3↔L2, N4↔L3/L4/L5, N5↔L7, N6↔L9, N7↔L8, N8↔L16 — each must be demonstrated with **real S2.2 facts**, not synthetic records.

**SQ-03 native schedule coverage:** a native test must demonstrate that under the serialized `_writeGate` model a reader observes a **complete old-or-new** occurrence publication and truthful attribution, with **no partial blend** and **no MVCC/snapshot-isolation claim**.

**SQ-04 historical fixtures:** authentic prior-writer formats only; formats without an authentic writer remain an explicit limitation and must **not** be fabricated. SQ-04 closure is primarily an S2.5/Stage-2 obligation; WP-S23-4 must not manufacture historical fixtures.

**Relevant D4 invariants** (from the accepted mapping): 1 accepted generation only (L1/L8); 2 stale/unverifiable refuses explicitly (L8/L13); 3 no unbounded scan on stale (L2); 4 failed staging preserves prior (L9/L10); 6 superseded cannot replace newer (L11); 7 reopen equivalent (L7/L17); 8 contributor/deletion consistent (L3/L4/L14); 9 deterministic order + limit (L2); 10 serialized concurrency (L12). D4.5 (cancelled staging exposes no partial) remains Stage 6 and is not counted.

## 5. Native execution requirements

1. **Genuine native runs.** Each "native" case must execute against a real `LatticeCodeGraphStore` database file on **Windows x64** with the recorded SDK/runtime; a managed simulation, a skipped test, or source inspection alone does not establish a native result.
2. **Real inputs.** Real source bytes + hashes, actual Roslyn compilations, metadata/project references, and live defining instances; expected values derived from accepted authority / frozen R2, never from the implementation under test.
3. **Raw receipts.** Commands, process identities, database provenance, before/after native row inspection, TRX/logs, environment, and a self-excluding manifest.
4. **Fault injection.** `before-commit` tests are managed fault injections; they qualify ordinary known-failure recovery only and do **not** establish crash/power-loss consistency. `after-commit`/ack-loss tests qualify the accepted unknown-outcome quarantine, not crash recovery.
5. **Frozen integrity.** Frozen R2 corpus and `oracle-expected-r2.json` unchanged; oracle replay `185/185` with output SHA-256 `97F6C5E5…39C9`; frozen identity formulas unchanged; prior S2.2/S2.3 evidence preserved, not rewritten.
6. **Regression floor.** Whole solution green, `dotnet format Andirit.slnx --verify-no-changes` exit 0, Release build 0 warnings / 0 errors, at the qualification HEAD.
7. **Independence.** The `S23-LIFE-Q1` reviewer must not be the implementer; reviewer probe projects are committed; expected values are authority-derived.

## 6. Known limitations and constraints

- **Platform.** Native qualification is Windows x64 only; the expanded Linux native suite remains an S2.5/Stage-2 obligation, not part of WP-S23-4. Earlier hosted CI does not constitute fresh S2.3 platform qualification.
- **Fault scope.** No crash-consistency, power-loss, abrupt-kill, arbitrary-crash-point, or multi-process-writer guarantee; fault capabilities are limited to the store's managed fault injector.
- **Carried qualifications.** Q1-Q1 (segment resumption) is closed fail-closed by WP-S23-3A; Q1-Q2 (recovery-rebuild internal-failure atomicity bound) and S-01 (caller-supplied component digests, S2.5) remain binding; O-Q2-01 remains a separate hardening candidate; WP-S23-3A qualifications Q2-Q01 (concurrent interrupted-run stale-baseline) and Q2-Q02 (failure-message text) remain binding and must be preserved, not "fixed", by this package.
- **SQ-04.** No authentic historical fixtures may be fabricated; unavailable formats are recorded as explicit limitations.
- **No production repair.** If a genuine production defect is found, the reviewer records it and stops; no remediation occurs inside WP-S23-4.

## 7. S23-LIFE-Q1 commission boundary

The subsequent `S23-LIFE-Q1` commission may:

1. Add bounded lifecycle tests and fixtures (tests only; no production change).
2. Execute the genuine native scenarios in the §4 matrix.
3. Reuse accepted WP-S23-1/2/3A and S23-PUB-Q2 evidence where a case is already satisfied, and state exactly what remains new.
4. Produce independent source and runtime evidence with complete raw receipts.
5. Return exactly one of: `S23-LIFE-Q1 SUPPORTED WITH QUALIFICATIONS`, `REMEDIATION REQUIRED`, `DESIGN DECISION REQUIRED`, `INCONCLUSIVE`, or `BLOCKED BY ENVIRONMENT`, with findings identifying exact source, governing contract, minimal reproduction, expected vs actual, consequence, and the smallest remediation boundary.
6. Stop and report a production defect rather than repairing it.

It must **not**: change production or accepted test oracles; weaken D3/D4/D5; fabricate historical fixtures; claim native results from simulations; or expand into WP-S23-5/S2.4/S2.5/Stage 6.

## 8. Successor entry criteria

- **Next gate after this entry:** `S23-LIFE-Q1` independent qualification of the §4 matrix (reviewer ≠ implementer).
- **Then:** a separate `S23-LIFE-OWNER` decision accepting or rejecting the qualified lifecycle results.
- **Only then:** WP-S23-5 (S2.3 owner acceptance), which itself requires its own commission and combined independent reviews.
- WP-S23-5, S2.4, S2.5, Stage 6, and public Find Usages remain **unauthorized**; each requires a separate owner commission.

## 9. Repository discipline

Only this decision document (and an additive status note to the S2.3 work-package plan's authorization-status section) is added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, or prior decision is modified. No branch, additional worktree, push, or package publication is performed. This decision executes no qualification and accepts no lifecycle result.
