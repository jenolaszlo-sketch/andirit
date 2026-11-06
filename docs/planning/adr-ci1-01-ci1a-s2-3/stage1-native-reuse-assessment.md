# Stage 1 native capability reuse assessment

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Authority:** S1A (§3); REM-01 owner acceptance; REM-01 contract reconciliation;
coverage-and-availability; publication-uncertainty.

---

## 1. Reuse unchanged

These Stage 1 capabilities are accepted and are to be consumed **unchanged** by
S2.3:

| Capability | Surface | Evidence |
| --- | --- | --- |
| Native occurrence labels `AndiritOccurrenceUnit/Page/Meta`, schema `andirit-occurrence-facts/1` | `LatticeCodeGraphStore` | S1A §3.1 |
| Atomic graph+occurrence publication in one transaction | `CompleteIndexRunAsync → Persist → PersistOccurrences` | S1A §3.2; N6 |
| Source-unit ownership (calling unit owns fact; storage contributor separate) | `CodeOccurrenceFact.SourceUnitId` / `Contributor.IndexUnitId` | REM-01 §4.3 |
| Replacement / deletion / contributor union / conflict refusal | Stage 1 native lifecycle | S1A §3.3; N3/N4 |
| Bounded deterministic target-page query (`Examined` = page size; ordering path→line→col→id; `MaxResults`/`Truncated`) | `FindOccurrencesAsync`, `CodeOccurrenceReader` | S1A §3.4; N2 |
| Publication-consistent availability (`StalePublication`, `AnalysisUnavailable`) | reader | S1A §3.4; coverage-and-availability §3 |
| Durable reopen / projection reconstruction | constructor → `RestoreOccurrences` | S1A §3.5; N5 |
| Schema/namespace refusal; units-without-meta refusal; page/publication mismatch refusal | open path | S1A §3.6; D5 |
| Complete-empty / partial / unavailable persistence and distinction | `PersistedOccurrenceUnit.Coverage`, `PersistedOccurrenceMeta` | REM-01 §6 |
| Unknown-outcome quarantine/reopen (post-commit/ack-loss) | `MutateAsync`, `ThrowIfUncertain` | SQ-02; REM-B05 |
| Serialized write/concurrency model | `_writeGate` | SQ-03 |
| D3 noninterference (synthetic corpus) | graph vs occurrence separation | S1A §3.7; N8 |

The identity/serialization helpers (`usage4`/`asmctx3` canonical forms,
`CodeOccurrenceFact` constructor recomputation, payload hash) are frozen and must
not change.

---

## 2. Requires integration (S2.3 work)

| Item | Gap | S2.3 action |
| --- | --- | --- |
| Real Roslyn facts through the publication path | S2.2's `VerifiedCSharpOccurrenceExtractor.Extract` is internal and not wired to a plugin/publication flow | Wire accepted S2.2 facts into `CodeGraphBatch`/sink/`StageIndexUnitAsync` (document 7, WP-PUB) |
| Recorded defining `compctx2` carrier | Not persisted (scope inventory §4) | Additive persisted carrier + reopen validation (document 3, WP-DUR) |
| N1–N8 with **real** facts | Existing N1–N8 use synthetic records | Adapt/extend to real S2.2 facts (document 5) |
| Real-source replacement/deletion/reindex cycle | Existing uses synthetic unit changes | Add real-source-change lifecycle tests (document 5) |
| Real-project D3 noninterference | Stage 1 synthetic | Real-fact D3 observation (document 5) |
| Recovery projection restoration | NEW-01 defect | NEW-01 remediation (document 2) |
| Native schedule test (SQ-03) | Not performed | S2 acceptance obligation |

---

## 3. Explicitly not reused / not changed

- **Do not** replace the storage architecture (D4).
- **Do not** migrate or dual-write experimental schemas (D5).
- **Do not** add a background maintenance service, distributed coordinator, or
  automatic repair framework (D4).
- **Do not** introduce MVCC/snapshot isolation (SQ-03).
- **Do not** reinterpret historical aggregate edges as occurrence facts (D5).

---

## 4. Reusable tests (real-fact adaptation targets)

| Existing test | Real-fact S2.3 adaptation |
| --- | --- |
| `N1_InitialPublicationPersistsNativeFactsPagesAndMeta` | Publish real S2.2 facts; assert unit/page/meta |
| `N2_BoundedQueryIsDeterministicAndDoesNotScanUnrelatedTargets` | Real facts; `Examined` = page size |
| `N3_ReplacementRetiresObsoleteFactsAndPreservesOtherUnits` | Real source edit + reindex |
| `N4_ContributorUnionAndScopedDeletionPreserveRemainingContributors` | Real cross-unit contributors |
| `N5_ReopenPreservesOccurrenceAndIndexState` | Real facts; byte-equivalent ordered results |
| `N6_FailedCompletionPreservesPriorOccurrencePublication` | Real facts; prior publication intact |
| `N7_StalePublicationAndGraphOnlyStoresRefuseExplicitly` | Real facts |
| `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle` | Real facts; graph snapshot equality |
| `Store_RejectsUnsupportedOccurrenceSchemaOnOpen` | Unchanged (D5) |
| `Store_RejectsOccurrenceUnitsWithoutMeta` | Unchanged (D5) |
| `CompleteEmptyAnalysisSurvivesReplacementAndReopen` | Real complete-empty unit |
| `IncompleteCoverageCannotMasqueradeAsCompleteEmpty` | Real partial coverage |
| `NoAnalysisWithZeroFactsRemainsUnavailable` | Real unavailable |
| `DeletingACompleteUnitInvalidatesCoverage` | Real deletion |
| `AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState` | Unchanged (SQ-02) |

Test locations: `tests/Andirit.LatticeDb.Tests/OccurrenceStage1NativeTests.cs`,
`OccurrenceRemediationNativeTests.cs`.

---

## 5. New tests required (not present in Stage 1)

- Native reopen **stale-defining-`compctx2` refusal** (test 8b) — see document 3.
- Real-source **transitive-only** change → stable `asmctx3`, changed `compctx2`,
  stale refusal after reopen.
- Real **cross-project** target identity persisted and reopened.
- **NEW-01** recovery projection restoration (document 2).
- Native **schedule** test for the serialized old-or-new publication visibility
  (SQ-03; no MVCC claim).
- Recorded-`compctx2` **missing/corrupted** refusal after reopen.
- Complete-empty **vs** unavailable distinction after real reopen.
