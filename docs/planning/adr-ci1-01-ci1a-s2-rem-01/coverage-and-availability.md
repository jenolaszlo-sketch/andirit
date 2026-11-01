# Coverage and Availability — REM-B03 / REM-B04

**Authority:** owner decision D4/D5; RECON IF-03; Q1B F03/F04/F08/F10.
**Production:** `CodeOccurrenceCoverage`, `CodeOccurrenceAvailability`,
`CodeGraphIngestionSink`, `InMemoryCodeGraphStore`, `LatticeCodeGraphStore`.
**Tests:** `OccurrenceRemediationNativeTests`, `OccurrenceInMemoryParityTests`,
`CodeGraphIngestionSinkTests`.

---

## 1. Three dimensions, not one

| Dimension | Question | Representation |
|---|---|---|
| Semantic resolution | What happened to this candidate/target? | `Confirmed`, `External`, `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive` counters/reasons |
| Analysis coverage | Which declared scope was actually analyzed, and was it complete? | `CodeOccurrenceCoverage.State` + scope/universe + declared/verified counts |
| Publication availability | Can the accepted publication answer this query? | `CodeOccurrenceAvailability` |

`CodeOccurrenceCoverage.State` reuses the existing
`CodeRelationshipCoverageState` vocabulary:

| State | Meaning |
|---|---|
| `Produced` | The declared scope was completely analyzed. |
| `Partial` | Analysis ran, but completeness is unproven (legacy facts, missing unit, unsupported source/construct). |
| `NotProduced` | No occurrence analysis ran. |
| `Unavailable` | Analysis evidence is unavailable. |
| `NotApplicable` | Not used for occurrence analysis. |

A zero-fact contribution with `Produced` is a valid complete-empty analysis.
A zero-fact contribution with `Partial` is **not** a complete negative.

---

## 2. Coverage record

`CodeOccurrenceCoverage` carries:

- `InvocationFacts`, `UnsupportedCategories`;
- `External`, `Ambiguous`, `Unresolved`, `Unsupported`,
  `ConditionalInactive`;
- `UnavailableReasons` (cause -> count);
- `ScopeId` (`analysisscope1`), `UniverseId`
  (`compilation-universe1`);
- `DeclaredUnitCount`, `VerifiedUnitCount`;
- `State`.

`Merge` combines contributions: any unavailable contribution makes the combined
state unavailable; any partial contribution makes it partial; complete + not
analyzed is partial (not complete). Only complete + complete is complete.

---

## 3. Availability mapping

| Condition | Availability | Result |
|---|---|---|
| No successful publication | `AnalysisUnavailable` | no hits; reason |
| Expected publication mismatch | `StalePublication` | no hits |
| Occurrence index bound to another publication | `StalePublication` | no hits |
| No coverage or `NotProduced` | `AnalysisUnavailable` | no hits; reindex reason |
| Coverage `Partial` / `Unavailable` | `IncompleteAnalysis` | known hits may be returned; coverage attached; zero result is not complete |
| Coverage `Produced`, target page absent | `Available` | zero hits; `Coverage.IsComplete` true |
| Coverage `Produced`, target page present | `Available` | hits; coverage attached |

`CodeOccurrenceQueryResult.Coverage` is part of the result. `IsComplete` is true
only for `Available` plus complete coverage.

`Examined` continues to mean the number of target-index entries read for the
query. It is not the unrelated repository fact count and is not necessarily
`MaxResults` when the target page has more entries.

---

## 4. Sink and replacement propagation

`CodeGraphBatch` accepts an optional `OccurrenceCoverage`. The sink merges batch
coverage and sets `OccurrenceAnalysisIncluded` from either facts or a performed
analysis. A completing batch with zero facts and complete coverage therefore
produces a replacement with analysis evidence, not an empty/no-analysis unit.

`CodeIndexUnitReplacement` carries `OccurrenceCoverage`. If a replacement has
analysis facts but no coverage (legacy path), it is normalized to `Partial`; it is
never assumed complete.

---

## 5. Durable representation

`PersistedOccurrenceUnit` gains `Coverage`. `PersistedOccurrenceMeta` gains
`CoverageState`, `ScopeId`, `UniverseId` and `CompleteCoverage`.

The schema version and namespace remain `1` / `andirit-occurrence-facts/1`. The
new fields are additive JSON. Old records without coverage restore as `Partial`
when they contain facts or claim analysis; otherwise they are unavailable.

A complete-empty unit is persisted even with zero facts. Its page set may be
empty; the unit and meta remain. On reopen, the coverage restores and a missing
target page returns `Available`/zero with complete coverage.

Replacement, deletion, failed completion and supersession follow the existing
publication lifecycle. Deleting the last analyzed unit removes coverage and
returns `AnalysisUnavailable`.

---

## 6. Authority

- The extractor/producer supplies semantic resolution and claimed scope.
- The store validates canonical identities and payload, and persists coverage
  with the accepted publication.
- A fact alone does not assert that the scope was complete.
- A caller-supplied boolean is not sufficient; completeness is represented by
  the `Produced` state and scope/universe evidence.
- The reader exposes coverage; it does not infer completeness from an empty
  result.

---

## 7. Required tests

| Case | Test |
|---|---|
| Complete-empty publication, replacement, reopen | `CompleteEmptyAnalysisSurvivesReplacementAndReopen` |
| Incomplete coverage with a known hit | `IncompleteCoverageCannotMasqueradeAsCompleteEmpty` |
| No analysis with zero facts | `NoAnalysisWithZeroFactsRemainsUnavailable` |
| Deletion invalidates coverage | `DeletingACompleteUnitInvalidatesCoverage` |
| Sink propagation | `CompleteEmptyCoverage_PropagatesThroughSinkAndQuery` |
| Complete/incomplete distinction | `Coverage_DistinguishesNoAnalysisIncompleteAndCompleteEmpty` |

`ConditionalInactive` remains a Stage 2 extraction classification. REM-01 does
not implement the Roslyn inactive-site scanner; it provides the vocabulary and
prevents inactive/unavailable states from becoming confirmed facts.
