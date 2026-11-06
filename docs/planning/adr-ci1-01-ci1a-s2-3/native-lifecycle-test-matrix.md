# Native publication / lifecycle test matrix

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Authority:** Stage 2 boundary §8/§3; stage2-handoff §5; D4 invariants; S1A.

Each obligation must use **real S2.2 facts** (real source bytes, real Roslyn
compilations, real cross-project binding) and independently established expected
values. "Existing" = reusable Stage 1 test to adapt; "New" = required S2.3 test.

| # | Obligation | Requirement / authority | Expected | Existing → S2.3 |
| --- | --- | --- | --- | --- |
| L1 | Initial publication | Unit/page/meta persisted; graph+occurrence same transaction (S1A §3.1/§3.2) | Facts durably queryable | N1 → real |
| L2 | Same-unit replacement | Obsolete facts retired; other units preserved; pages rebuilt (S1A §3.3; D4.8) | Old fact gone, new visible | N3 → real source edit |
| L3 | Source deletion | Deleted unit's facts removed; remaining preserved; pages rebuilt | Deletion reflected | N4 → real |
| L4 | Contributor union | Identical semantic facts union contributors; different semantic units stay distinct (REM-01 §4.3) | 1 fact 2 contributors; distinct units distinct | N4 → real cross-unit |
| L5 | Contributor conflict refusal | Same identity, different payload rejected at staging; prior state intact (S1A §3.3; D4.4) | Rejection; prior publication retained | N4 → real |
| L6 | Valid complete-empty | `Produced` zero-fact unit persists; reopen restores (REM-01 §6; D4.7) | `Available` zero + complete | `CompleteEmptyAnalysisSurvivesReplacementAndReopen` → real |
| L7 | Native reopen | Projection reconstructed; byte-equivalent ordered results (S1A §3.5; D4.7) | Identical results | N5 → real |
| L8 | Stale publication refusal | Stale handle → `StalePublication` (D4.1/D4.2) | No hits; explicit status | N7 → real |
| L9 | Transactional graph/occurrence consistency | Failed completion preserves prior publication; graph+occurrence atomic (S1A §3.2; D4.4) | Prior intact; no partial | N6 → real |
| L10 | Recovery & repeatability | Known pre-commit failure recoverable; reopen durable; no blind retry (SQ-02; NEW-01) | Recovery projection = durable | NEW-01 package |
| L11 | Real source modification & re-index | Replacement after actual source change; identity/coverage correct | New identities; stale invalidated | New |
| L12 | Old-or-new complete visibility under serialized writes | Readers see a complete publication (old or new), never a partial blend (D4.10; SQ-03) | No partial visibility | New (native schedule test) |
| L13 | Partial/unavailable coverage | `Produced`/`Partial`/unavailable distinguished and persisted (REM-01 §6) | Correct availability mapping | `IncompleteCoverageCannotMasqueradeAsCompleteEmpty`, `NoAnalysisWithZeroFactsRemainsUnavailable` → real |
| L14 | Deletion invalidates coverage | Deleting the last analyzed unit → `AnalysisUnavailable` | Unavailable | `DeletingACompleteUnitInvalidatesCoverage` → real |
| L15 | Schema/namespace refusal | Unsupported schema/namespace and units-without-meta refused on open (D5; S1A §3.6) | Explicit refusal | `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`, `Store_RejectsOccurrenceUnitsWithoutMeta` (unchanged) |
| L16 | D3 graph noninterference | Graph nodes/edges/counts/traversal unchanged across occurrence lifecycle | Identical graph snapshots | N8 → real |
| L17 | Native reopen stale-defining-`compctx2` refusal | **Test 8b** (document 3) | Stale refused; current usable | New (WP-DUR) |
| L18 | Real cross-project target identity | `asmctx3` from dependency's verified defining compilation; persisted; reopened | Correct target | New |
| L19 | Recorded-context missing/corrupt refusal | Missing/corrupted persisted provenance refuses conservatively | Explicit refusal | New (WP-DUR) |
| L20 | Unknown-outcome quarantine | Post-commit/ack-loss quarantines; reopen reconciles (SQ-02) | Quarantine; reopen durable | `AfterCommitAcknowledgementLoss…` (unchanged) |

---

## Applicable D4 invariants (real facts)

| Invariant | S2.3 obligation |
| --- | --- |
| 1 accepted generation only | L1/L8 |
| 2 stale/unverifiable index refuses explicitly | L8/L13 |
| 3 no unbounded scan on stale | L2 (`Examined` = page size) |
| 4 failed staging preserves prior publication | L9/L10 |
| 5 cancelled staging exposes no partial | Stage 6 (not counted) |
| 6 superseded cannot replace newer | L11 (supersession) |
| 7 reopen semantically equivalent | L7/L17 |
| 8 contributor/deletion consistent | L3/L4/L14 |
| 9 deterministic order + limit | L2 |
| 10 serialized concurrency | L12 |

---

## Platform

Native S2.3 qualification is **Windows x64** unless a Linux run is separately
performed. The Stage-2 boundary requires an **expanded Linux native suite as an
S2.5/Stage-2 acceptance obligation**, not full cross-platform certification (which
remains Stage 6). **Earlier hosted CI runs do not constitute fresh S2.3 platform
qualification.**

## Historical fixtures (SQ-04)

Available authentic prior-writer formats are Stage-2 acceptance evidence; formats
without an authentic writer are an explicit limitation, not replaced by
fresh-store tampering. S2.3 must not fabricate historical fixtures.
