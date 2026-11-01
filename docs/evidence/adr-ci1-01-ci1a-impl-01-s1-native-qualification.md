# ADR-CI1-01-CI1A-IMPL-01-S1 — Native Qualification

**Scope:** the eight mandatory native tests extracted from the independent Q1
`STAGE1-READINESS.md` and `OWNER-HANDOFF.md`, plus the D4/SQ mapping. All tests execute
against the real `LatticeCodeGraphStore` and LatticeDB (`LatticeDbSharp` 0.2.0).

File: `tests/Andirit.LatticeDb.Tests/OccurrenceStage1NativeTests.cs`.
Environment: Windows 10.0.26200 X64, .NET SDK 10.0.401, Debug build, fresh isolated
database per test.

## 1. The eight mandatory tests

| # | Reviewer requirement | Actual test | Positive assertion | Negative assertion | LatticeDB state observed |
|---|---|---|---|---|---|
| 1 | Ingest unit → publish occurrences atomically | `N1_InitialPublicationPersistsNativeFactsPagesAndMeta` | 3 ordered hits, `Available`, publication bound | — | `AndiritOccurrenceUnit/Page/Meta` each 1; queried after close by raw `LatticeDatabase` |
| 2 | Bounded, deterministic target page | `N2_BoundedQueryIsDeterministicAndDoesNotScanUnrelatedTargets` | limit=2 → 2 hits, `TotalMatches=5`, `Examined=5`, deterministic order | 40 unrelated-target facts are not examined | target page only |
| 3 | Replace unit → old facts retired, pages updated | `N3_ReplacementRetiresObsoleteFactsAndPreservesOtherUnits` | new fact visible, other unit preserved | old unit facts and its target page entry retired | occurrence unit rewritten in completion transaction |
| 4 | Delete unit with contributor union | `N4_ContributorUnionAndScopedDeletionPreserveRemainingContributors` | identical fact has 2 contributors; deletion keeps the other | final contributor removal retires the fact; conflicting payload rejected | contributor union/removal across units |
| 5 | Reopen → identical ordered results | `N5_ReopenPreservesOccurrenceAndIndexState` | same hits/order/total/examined after reopen | — | meta/units/pages restored and re-validated on open |
| 6 | Inject `CompleteIndexRunAsync` failure → prior publication intact | `N6_FailedCompletionPreservesPriorOccurrencePublication` | staged facts invisible; prior fact visible | `before-commit` fault throws; after reopen prior fact still visible | rolled-back native transaction; prior tables intact |
| 7 | Stale publication → explicit refusal | `N7_StalePublicationAndGraphOnlyStoresRefuseExplicitly` | current query available | stale expected publication → `StalePublication`; graph-only → `AnalysisUnavailable` | publication binding in meta/index |
| 8 | D3 corpus → zero graph semantic delta | `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle` | graph snapshot identical across all events | any graph delta fails | graph labels untouched by occurrence writes |

Additional native tests:

| Test | Purpose |
|---|---|
| `Store_RejectsUnsupportedOccurrenceSchemaOnOpen` | SQ-03/D5 unsupported format refusal |
| `Store_RejectsOccurrenceUnitsWithoutMeta` | malformed state refusal |
| `OccurrenceStage1PerformanceProbe.Stage1WorkloadMeasurements` | reproducible baseline measurements |

Result: **8/8 mandatory native tests pass**, plus 2 schema tests, 1 probe, 0 failed,
0 skipped (`Andirit.LatticeDb.Tests`: 38/38).

## 2. D4 invariant mapping

| # | Accepted invariant | Stage 1 evidence | Status |
|---|---|---|---|
| 1 | query uses only the accepted published generation | N1, N7 | Covered |
| 2 | stale/unbound/unverifiable index refuses explicitly | N7, N5, schema tests | Covered |
| 3 | stale index must not trigger unbounded scan | N2 (`Examined` = page only) | Covered |
| 4 | failed staging preserves last accepted publication | N6 | Covered |
| 5 | cancelled staging exposes nothing | N6 pre-completion assertion; staged-not-visible | Covered (no explicit cancel token path) |
| 6 | superseded publication cannot replace newer | existing `Store_ResumedRunConflictsAfterNewerPublication` + N7 | Partial (graph ordering path; occurrence inheritance by baseline) |
| 7 | reopen/rebuild yields equivalent index | N5 | Covered |
| 8 | contributor/scoped deletion consistent | N4 | Covered |
| 9 | deterministic order + explicit limit | N2 | Covered |
| 10 | defined concurrency/snapshot contract | `_writeGate` + N7 stale refusal | Partial (no true concurrent publication stress) |

**Stage 1 does not close the full D4 matrix.** Items 6 and 10 remain partial and are
carried to the Stage 6 native matrix.

## 3. SQ-01 — fault injection

The existing `LatticeCodeGraphStore(Action<string>? faultInjector)` seam is used
unchanged. It is internal (test-accessible via `InternalsVisibleTo`) and invoked at
`before-commit` inside the write transaction. No externally callable production
fault-injection facility was added. N6 and N8 use it to fail the commit.

## 4. SQ-02 — publication/query serialization

`_writeGate` is a `SemaphoreSlim(1,1)` held by `MutateAsync` (all writes),
`ReadAsync` (all graph reads), `TraverseAsync`, `FindOccurrencesAsync`, `CheckHealthAsync`
and `Dispose`. A query cannot interleave with a publication; a reader observes either
the prior or the new accepted state. Reads are served from the materialized projection
(and its occurrence index), which is rebuilt from durable native records on open. The
gate does not itself provide crash recovery or multi-process coordination.

## 5. SQ-05 — unknown commit outcome

The store distinguishes:

- known failure **before** commit (`before-commit` fault → no commit attempted);
- a rejected/throwing native commit (transaction rolled back);
- confirmed durable commit (commit returns normally).

On any persistence failure, `MutateAsync` replays the previously accepted command list
and rethrows, so the in-memory projection is restored to the last accepted state.
A commit that succeeds while its acknowledgement is lost is **not** distinguishable in
this slice; no durable-success claim is made for that case. Native crash/power-loss
recovery remains unqualified.

## 6. Publication race

A successful occurrence query is bound to one `CodeGraphPublication`. If the caller
supplies an expected publication that is no longer current, the query returns
`StalePublication` rather than mixing generations. Because publication and query share
`_writeGate`, in-process facts and pages cannot be combined across generations.
