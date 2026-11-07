# ANDIRIT-CI1A-S2.4-WP-S24-1-IMPL-01 — Internal exact-target occurrence continuation and coverage breakdown (implementation)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-1-IMPL-01`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Gate:** `WP-S24-1 IMPLEMENTATION AUTHORIZED WITH QUALIFICATIONS`.
**Disposition:** **`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`**.

> Implementation, focused tests and implementation evidence only. This is a
> producer report; the next step is a **separate independent-review commission**
> (`S24-READER-Q1`). No independent qualification, owner acceptance, WP-S24-2,
> WP-S24-3, S2.5, Stage 6, or public Find Usages is performed or authorized.

---

## 1. Preflight

| Check | Result |
| --- | --- |
| Starting HEAD | `9726c3aeb101bd039ca3d8fe921a9910e1878575` (expected) |
| Starting tree | `5546c747ccec6638de174cc3b60b31931ae66beb` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`environment.log`) |

The committed S2.4 owner decision was read as normative; no conflict was found between the prompt and the accepted contract.

## 2. Implementation summary

Additive internal occurrence-reader continuation and coverage, on the real Windows-x64 native store, with no public Find Usages API and no persisted state.

**Production (2 files, +204 / −37):**
- `src/Andirit/CodeOccurrenceReader.cs`: added `CodeOccurrenceContinuationState` (`NotRequested`/`More`/`Complete`/`Invalid`); `CodeOccurrenceUnitCoverage` (per-unit scope provenance); `CodeOccurrenceQuery.DefaultPageSize = 100`, `AbsoluteMaxPageSize = 1000`, and `ContinuationToken`; `CodeOccurrenceQueryResult.NextContinuationToken`, `Continuation`, `CoverageBreakdown` (+ `UnitCoverage`); tightened `IsComplete` so an intermediate/rejected page is never complete.
- `src/Andirit/InMemoryCodeGraphStore.cs`: token decode → trusted-state binding → strictly-after cursor seek → bounded page + next token; per-request publication/authority revalidation is unchanged (the `LatticeCodeGraphStore` gate already runs on every call and needed no change); scope-level per-unit `CoverageBreakdown`.
- `src/Andirit/CodeOccurrenceContinuation.cs` (new): the opaque, versioned, stateless token codec (§3).

**Tests (1 file, +15 tests):** `tests/Andirit.CSharp.Tests/S24ReaderContinuationTests.cs`.
**Public-type snapshot:** `tests/Andirit.Tests/RuntimePublicApiContractTests.cs` updated for the two additive public types.

## 3. Contract compliance matrix

| Requirement | Implementation | Status |
| --- | --- | --- |
| **D1** continuation identity | `CodeOccurrenceContinuation` envelope `v/repo/mode/target/pubRun/pubId/dir/afterPath/afterStartLine/afterStartColumn/afterEndLine/afterEndColumn/afterId/size/sum`; base64url; strict field-set + checksum + range checks; stateless, non-persisted; binding to repo/target/mode/dir/size/publication checked against trusted state | **Met** |
| **D2** publication-change | Token bound to `(run, id)`; on mismatch → `StalePublication` + `Continuation = Invalid`, empty, not complete; no silent restart/blend; recovery rebuild keeps the same publication | **Met** |
| **D3** deterministic pagination | Total order path(ordinal), startLine, startColumn, endLine, endColumn, id; strictly-after cursor; unique `id` ⇒ strict total order; ties by `id`; overloads = distinct targets; cross-unit contributors unioned to one hit | **Met** |
| **D4** bounded execution | Returned ≤ `min(size, AbsoluteMaxPageSize)`; `Examined` = target page candidates (unchanged meaning); work O(target page); token chars ≤ 1024 / envelope ≤ 768 bytes; finite depth | **Met** |
| **D5** authority/availability | Authority gate re-runs on every request; availability / coverage / staleness / incomplete analysis / invalid continuation kept distinct; partial coverage continues under `IncompleteAnalysis` | **Met** |
| **D6** coverage breakdown | `CoverageBreakdown` = ordered per-unit `CodeOccurrenceUnitCoverage` from accepted `_occurrenceCoverage`; aggregate `Coverage` retained (worst-wins); scope-level, not page-level; subset conveyed via `TotalMatches`/`Truncated`/token | **Met** |
| **D7** cross-target scope | Deferred; `mode` reserved; no cross-target implementation; WP-S24-1 independent of it | **Met (deferred as decided)** |
| **D8** boundary | Internal reader/query/result only; no public Find Usages; no persisted token/session; no dual-write/migration; no S2.5/Stage 6 | **Met** |

## 4. Test evidence

Focused tests (`focused-new-tests.log`, 15/15 passed) cover the required categories:

| # | Category | Test |
| --- | --- | --- |
| 1 | Single-page completion | `SinglePage_CompletesWithNoNextToken` |
| 2 | Multi-page traversal, no duplicates/omissions | `MultiPage_TraversesEveryOccurrenceExactlyOnce` |
| 3 | Equal-position ties | `EqualPosition_TiesAreOrderedByIdWithoutDuplicates` |
| 4 | Overload identity | `Overloads_HaveIndependentContinuationPages` |
| 5 | Cross-unit contributor dedup | `CrossUnitContributors_AreDeduplicatedAcrossPages` |
| 6/7 | Exact boundary / completed set | `MultiPage_*` |
| 8 | Stale publication between pages | `StalePublicationBetweenPages_IsRefusedNotBlended` |
| 9 | Recovery between pages | `RecoveryBetweenPages_PreservesPublicationAndContinuation` |
| 10/11/12 | Malformed/altered/oversized/wrong-binding tokens | `MalformedAndMismatchedTokens_AreRefusedAsInvalid`, `TokenBoundToAnotherRepository_IsRefused` |
| 13 | Per-page authority revalidation | `PerPageAuthority_IsRevalidatedOnEveryContinuation` |
| 14 | Candidate/work bounds | `SmallPage_DoesNotHideAnUnboundedScan` |
| 15/16 | Coverage consistency; partial/incomplete analysis | `PartialCoverage_RemainsIncompleteAcrossPages` |
| 17 | Unavailable/refused contributor | `GraphOnlyRepository_IsUnavailableWithEmptyBreakdown` |
| 18 | Legacy reader compatibility | `LegacyQuery_WithoutToken_ReturnsAll` |
| 19 | D3 noninterference | `Pagination_DoesNotChangeGraphOrPersistedRows` |
| 20 | No persisted token/session | `Pagination_DoesNotChangeGraphOrPersistedRows` (row-count invariance) |

Evidence class: **constructed native facts over the accepted staging/native path** (not real-project verified authority, except test 13 which uses the accepted `S23NativeAuthorityFixture` verified compilations). These are producer implementation tests and are **not** independent qualification.

## 5. Bounded execution analysis

- Returned occurrences per request ≤ `min(requested size, AbsoluteMaxPageSize = 1000)`.
- `Examined` is preserved as the number of target-page candidate entries read; continuation re-derives from the target’s bounded candidate set and does not scan unrelated targets. A small page therefore cannot hide an unbounded scan.
- Work per request is `O(target page)` (sort/seek), bounded by the exact-target candidate set.
- Token decode is bounded (≤ 1024 chars, ≤ 768 decoded bytes, strict field set).
- Continuation depth is finite: pages terminate when no entry sorts strictly after the cursor.
- **Cross-target fan-out** is not implemented (deferred); no unbounded fan-out exists.

## 6. Publication/authority refusal analysis

- Every request (initial or continuation) passes the `LatticeCodeGraphStore` authority gate; the inner reader additionally revalidates the token’s publication binding.
- Publication change / recovery across page boundaries → `StalePublication` + `Continuation = Invalid`, empty, `IsComplete = false`; never a blend and never a silent restart.
- Malformed/altered/oversized/wrong-binding tokens → `Continuation = Invalid` with the current availability and an explicit `RefusalReason`; no partial page, not complete.
- Missing current authority → `AnalysisUnavailable` even with a valid token (test 13).
- Recovery rebuild (`RestoreOccurrences`) repopulates authority and coverage so a same-publication continuation still serves correctly.

## 7. Coverage semantics

- `CoverageBreakdown` lists per-unit `(pluginId, unitId, state, analysisPerformed, counters, scopeId, universeId)` for the repository/query scope, ordered by plugin then unit.
- The aggregate `Coverage` is retained (worst-wins) and drives `Availability` and `IsComplete`.
- The breakdown describes the **scope**, not the page; a subset page is conveyed by `TotalMatches`/`Truncated`/`NextContinuationToken`, and a partial scope keeps `IncompleteAnalysis` on every page.
- Only accepted persisted coverage data is surfaced; no membership proof or canonicalization claim is added (S-01 remains deferred).

## 8. Backward compatibility

- Existing constructors/call sites compile unchanged; no-token queries keep their behavior (all results; `TotalMatches`/`Examined`/`Truncated` unchanged).
- **Deliberate contract-conformant change:** `IsComplete` now also requires the final-page/complete continuation state, so an intermediate (`More`) or rejected (`Invalid`) page is not complete. Existing tests that assert `IsComplete` on complete single-page/complete-empty results are unaffected.
- Two additive public types (`CodeOccurrenceContinuationState`, `CodeOccurrenceUnitCoverage`) were registered in the intentional public-type snapshot; the reader remains an internal contract (MD-03), not a public Find Usages API.
- Exact target identity, S2.3 publication/recovery semantics, authority validation, availability/refusal contracts, D3 graph noninterference, and the serialized single-writer model are preserved. No schema migration, dual-write, persisted token, or durable session state.

## 9. Known qualifications and limitations

- **Producer evidence only.** These tests are not independent qualification; `S24-READER-Q1` is required.
- **Same-harness independence limitation** persists (producer and reviewer share the harness); an independent re-run may be required.
- **Platform.** Windows x64, serialized single-writer; no MVCC/crash/cross-platform claim.
- **Unkeyed checksum.** `sum` is tamper-evident against accidental corruption, **not** cryptographic authentication (per the accepted decision). A forged cursor can only skip within one publication and cannot fabricate facts.
- **`Examined` meaning.** Unchanged (target-page candidates); continuation re-reads the target candidate set per request (bounded, not optimized).
- **Coverage breakdown** is derived from accepted in-memory/native per-unit coverage; authority-gate refusal paths return no breakdown (correctly).
- **Untested behavior.** Cross-target (deferred); extreme token tamper beyond the length bound (covered by length reject); multi-process and crash behavior (out of scope). Test 13 uses verified authority; the other continuation tests use constructed native facts.
- **Preserved obligations** (not discharged): F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; SQ-04, SQ-05, SQ-06 remainder, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07.

## 10. Independent reviewer handoff

Provide to `S24-READER-Q1`: reviewed production commit/tree; the two production files and the new codec; the 15 focused tests; this report; the evidence manifest (`docs/reviews/CI1A-S2.4-WP-S24-1-IMPL-01-evidence/manifest.json`). The reviewer should independently reproduce the contract (stable multi-page traversal, no duplicates/omissions, ties/overloads/cross-unit identity, stale refusal, recovery between pages, malformed/altered/oversized/wrong-binding tokens, per-page authority, bounds, coverage correctness, availability/refusal separation, deterministic replay, absence of new persistence) with reviewer-owned expected values, and must not reuse producer expected values.

## 11. Final implementation disposition

**`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** All normative contract requirements (D1–D8) are met and supported by implementation evidence: full solution **903 passed / 0 failed** (51 + 157 + 345 + 350); 15 focused tests passing; frozen R2 oracle **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`; Release build 0/0; `dotnet format --verify-no-changes` exit 0; production change limited to the authorized internal reader surfaces.

---

*Producer implementation report. Independent qualification (`S24-READER-Q1`) remains outstanding; no owner acceptance and no successor authorization is implied.*
