# ANDIRIT-CI1A-S2.4-WP-S24-2-ENTRY-01 — WP-S24-2 entry assessment (IF-05 mapping, IF-02 realistic sizes, SQ-07/MD-02 measurement)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-2-ENTRY-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Role:** independent entry assessment; documentation only.
**Disposition:** **`ENTRY READY WITH QUALIFICATIONS`**.

> This is an entry assessment. It authorizes **no** implementation, test change,
> qualification execution, optimization, or successor work. The proposed
> `WP-S24-2-QUAL-PREP` commission in §10 is a draft for separate owner
> authorization only.

---

## 1. Verified repository state

| Check | Result |
| --- | --- |
| HEAD | `c0ec72ea7a23919e8919a54cda67b1dbe2452045` (expected) |
| Tree | `57bd0d2a801c18ab9d8a983eedb262a69607126c` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |
| WP-S24-1 status | accepted with qualifications (`adr-ci1-01-ci1a-s2-4-wp-s24-1-owner-01.md`, commit `c0ec72e`) |
| S2.4 plan / entry decision | present; WP-S24-1 closed, WP-S24-2 "planned but unauthorized" |

No unexpected state, authority conflict, or evidence inconsistency was found.

## 2. Authoritative obligation matrix

WP-S24-2 covers the remaining **qualification, verification and documentation** obligations of S2.4. It does not cover a functionality defect: WP-S24-1's accepted behavior has no open blocking finding. Existing evidence is reused where accepted; the gaps below are evidence/verification gaps, not defects.

| Obligation | Authoritative requirement | Existing supporting evidence | Remaining gap | Required test / measurement | Proposed acceptance criterion | Reviewer independence | Exclusions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **IF-05** refusal mapping | S1 reconciliation §IF-05; register IF-05/I-03: *document/test the exact open-exception vs query-status mapping* (graph-only/no-analysis, schema/namespace, stale/unbound page, incomplete coverage); do not promise every mismatch is detected at open | WP-S23-2 Q2 (schema/namespace/stale), S23-LIFE-Q1 (namespace SQ-06), WP-S23-3A Q1/Q2 (page bounds, malformed rows, endpoint), WP-S24-1 Q1/Q2 (invalid continuation) | Mapping is spread across tests; no single exhaustive **operational mapping document** + table-driven test | A reviewer-owned mapping table + tests per case: open-time `CodeOccurrenceSchemaException`/`CodeOccurrenceIndexException`/`CodeGraphBatchRejectedException` vs query-time `Available`/`AnalysisUnavailable`/`IncompleteAnalysis`/`StalePublication`/`CorruptedIndex`/`UnsupportedSchema`/`Invalid` | Every enumerated case has a documented mapping and a failing-asserting test; **no** claim that all mismatch classes are detected at open | `S24-SURFACE-Q1` (reviewer ≠ implementer) | No schema change; no new status values beyond the accepted vocabulary |
| **IF-02** realistic sizes | S1 reconciliation §IF-02; register IF-02: verify real target sizes and site order including end coordinates/ID; planner bounds and pagination | WP-S24-1 Q2 pagination oracle (synthetic native facts, page sizes 1/2/3/7/100); frozen R2 corpus (real, small targets, 53 invocations / 5 projects / 5 overloads) | No real-source target with **large** cardinality; no documented planner-bound/pagination behavior at realistic sizes | Real-source workloads at increasing target cardinality; independent total-order oracle; first/middle/terminal pages; exact boundaries; equal-position ties; overloads; cross-unit contributors | Concatenated pages equal the independent oracle with no duplicates/omissions; end coordinates and ids included in the order; `Examined`/`TotalMatches`/`Truncated`/continuation/`IsComplete` correct | `S24-SURFACE-Q1` | No ordering change; no index; no cross-target; no optimization |
| **SQ-07** resource/cost measurement | Register SQ-07; stage2-acceptance-matrix §11: measure real-project publication/reopen/query costs; query latency bounded via `Examined` | `OccurrenceStage1PerformanceProbe` (Stage-1 synthetic, single-run, explicitly non-threshold); WP-S24-1 Q2 bounded scaling (500/2000/8000, `Examined` = cardinality) | No real-project measurement with stated repetitions/environment; no cold/warm or page-position breakdown | Repeatable harness: target cardinality, repository-wide cardinality, page size/position, `Examined`, elapsed, allocations, peak memory (feasible), CPU (feasible), cold vs warm, first vs repeated page | **Characterization report**, not a pass/fail threshold: reproducible setup, repetitions, environment, distributions/percentiles, and explicit "no contractual bound claimed" | `S24-SURFACE-Q1` | **No optimization**; no incremental maintenance/native-node-ID work; no numeric threshold invented |
| **MD-02** measurement honesty | S1 reconciliation §MD-02: measurements must state repetitions/environment; existing single-run probe is non-benchmark | `OccurrenceStage1PerformanceProbe` | New measurements must not repeat the single-run/Debug pattern | Include repetitions ≥ N, warmup, Release, machine/runtime config, GC mode | Report states repetitions/environment and labels values as observations | `S24-SURFACE-Q1` | No benchmark contract |
| **F10** (gates IF-02/IF-04) | Register F10: continuation across publication changes, ordering/limits, completeness/availability assertions | WP-S24-1 accepted (Q1/R1/Q2) | Only realistic-size + mapping portions remain (continuation semantics accepted) | Covered by IF-02/IF-05 rows | As above | `S24-SURFACE-Q1` | No re-open of accepted WP-S24-1 semantics |
| **MD-03** boundary | Internal reader contract; no public Find Usages | WP-S24-1 accepted | — | Confirm unchanged | No new public API | `S24-SURFACE-Q1` | Public API excluded |

**Defect vs gap.** No functionality defect is asserted. The items are (a) a documentation/test-exhaustiveness gap (IF-05), (b) a realistic-size verification gap (IF-02), and (c) a measurement-evidence gap (SQ-07/MD-02).

## 3. Existing evidence inventory

- **Real-source fixture (authentic):** the frozen R2 corpus `docs/planning/adr-ci1-01-ci1a-s2-0/corpus` — 5 projects, 11 C# files, 5 namespaces, 53 invocations (52 cross-project), 5 overloaded methods, 21 method references, plus a `variants/source-changed/App/Sites.cs` variant. The frozen oracle `oracle-expected-r2.json` (145,620 bytes, SHA-256 `274A7AAE…`) is the independent semantic reference; the observed replay is `185/185` (`97F6C5E5…`).
- **WP-S24-1 evidence:** internal continuation/coverage accepted (Q2 `SUPPORTED WITH QUALIFICATIONS`); the Q2 probe suite (`Q2-P0*`, `Q2-reuse-*`) already provides a reviewer-owned total-order oracle, canonical/refusal matrices, precedence, bounded scaling, and noninterference — reusable as the pagination oracle basis.
- **WP-S23 evidence:** refusal behavior (schema/namespace, page/publication mismatch, malformed rows, endpoint/owner correspondence, orphan refusal), authority/recovery/publication.
- **Stage-1:** `OccurrenceStage1PerformanceProbe` (measurement pattern, explicitly non-threshold).
- **Real-project plugin fixture (WP-S24-1/Q2):** generated-but-genuine C# (real Roslyn) with 2 projects, cross-project invocations — a template for scalable real-source workloads.

## 4. Real-source fixture and oracle plan

**Fixtures.**
1. **Frozen R2 corpus** (authentic real project) for correctness/mapping realism: multi-project, namespaces, overloads, cross-project references, source locations, contributor provenance, and the `source-changed` variant for replacement/stale behavior. It is the primary authentic fixture.
2. **Scalable generated-real projects** (real C# source bytes compiled by Roslyn, not synthetic facts) for target cardinality: a `Core` project defining a shared method (and overloads) plus `N` consumer files/projects invoking it, producing a target with cardinality `N`. These are genuine compilations with real spans/hashes; they are recorded as generated fixtures (not the frozen corpus).
3. **Constructed native facts** remain permitted only for boundary micro-cases (equal-position ties, exact page boundaries) where they are the correct instrument; they must not be presented as real-source proof.

**Independent mapping oracle.** The reviewer must derive expected mapping outcomes from the **accepted contract** (public status vocabulary, D5, IF-03 coverage algebra) and from **independent source facts** (Roslyn symbol/definition data, file bytes), not by calling the production mapping/refusal code. For IF-02 the oracle is the accepted total-order tuple (`path` ordinal, start line/column, end line/column, occurrence id) computed by reviewer-owned code.

## 5. Realistic-size workload plan

| Tier | Workload | Purpose |
| --- | --- | --- |
| Small control | Frozen R2 corpus target (few occurrences) | Correctness/mapping control on authentic source |
| Boundary micro | Constructed facts: equal-position ties, exact page boundaries (n = page size, n = page size ± 1) | Off-by-one/ties |
| Medium | Generated-real target cardinality 10²–10³ (page sizes 1/10/100/1000) | Ordering, limits, `Examined`, terminal state |
| Larger | Generated-real target cardinality 10⁴ (page sizes 10/100/1000) | Realistic-size ordering and continuation depth |
| Overload/cross-unit | Real corpus overloads + generated cross-unit contributions | Identity/union provenance |

For each workload and page size: traverse first/middle/terminal pages; compare the **complete concatenated result** to the independent oracle (no duplicates/omissions); assert page-size bounds, exact boundary pages, `Examined` (target-page candidates), `TotalMatches` (scope total), `Truncated`, `NextContinuationToken`, `Continuation` (`More`/`Complete`), and `IsComplete` (final page only for complete scopes). Valid pagination is not inferred from a single page.

**Limitation.** Target cardinality above ~10⁴ via generated source is bounded by Roslyn compilation time; any ceiling must be recorded, and large-scale claims must not be made from synthetic facts alone.

## 6. SQ-07 / MD-02 measurement methodology

- **Environment disclosure:** OS/arch, .NET SDK/runtime, process architecture, Release configuration, GC mode, machine (CPU class/RAM), and single-writer serialized usage.
- **Warmup + repetitions:** explicit warmup iterations; ≥ 5–10 recorded repetitions per point; report min/median/max (and percentile) rather than a single run; discard warmup from statistics.
- **Dimensions:** target occurrence cardinality; repository-wide occurrence cardinality; page size (1/10/100/1000); page position (first/middle/terminal); cold vs warm store; first page vs repeated page fetch.
- **Metrics:** `Examined`, elapsed time (`Stopwatch`), allocated bytes (`GC.GetAllocatedBytesForCurrentThread` / `GC.GetTotalAllocatedBytes`), peak working set (`Process.PeakWorkingSet64`, feasible), CPU time (`Process.TotalProcessorTime`, feasible).
- **Honesty:** report observed values; **no numeric acceptance threshold** is invented (none exists in the accepted contract). Measurements characterize; they do not gate acceptance unless a normative bound is violated. F-OBS-Q1-01 (target-local per-page sorting `O(n log n)`) remains an observation; measurements must not be used to preauthorize optimization.
- **Tooling:** confined to tests or a reviewer probe (e.g., a `docs/reviews/...-tests` harness); no production instrumentation, no new persisted schema.

## 7. Proposed S24-SURFACE-Q1 matrix

| # | Check | Class | Blocking? |
| --- | --- | --- | --- |
| S1 | IF-05 exhaustive refusal/status mapping table + table-driven tests | correctness/documentation | yes (missing mapping) |
| S2 | IF-02 realistic-size ordering vs independent oracle (all tiers/page sizes) | correctness | yes (ordering/omission) |
| S3 | IF-02 exact boundaries, equal-position ties, overloads, cross-unit contributors | correctness | yes |
| S4 | SQ-07/MD-02 reproducible measurement report (no thresholds) | evidence | no (characterization) |
| S5 | Coverage/provenance correctness at realistic sizes | correctness | yes |
| S6 | Publication/recovery continuity across pages (reuse + fresh) | correctness | yes |
| S7 | Regression against accepted WP-S24-1 behavior (no-token legacy, ordering, bounds, refusal, `IsComplete`) | regression | yes |
| S8 | Evidence integrity (self-excluding manifest; historical evidence byte-exact) | integrity | yes |
| S9 | Boundary confirmation (no cross-target, no public API, no persistence/index/schema, no optimization) | boundary | yes |

**Blocking vs non-blocking.** A wrong mapping, ordering/omission/boundary defect, coverage/provenance error, regression in WP-S24-1 behavior, or evidence-integrity failure is **blocking**. Measurement magnitude (S4) is **non-blocking** unless it demonstrates a normative-bound violation; the `O(n log n)` per-page sort is **not** a violation and must not trigger optimization.

## 8. Risks and dependencies

- **Fixture authenticity:** IF-02 realism depends on genuine Roslyn workloads; generated-real projects must be labelled as generated and not conflated with the frozen corpus.
- **Measurement noise:** JIT/GC/OS scheduling; mitigated by warmup/repetitions and distributions, and by making measurements non-gating.
- **Scope creep:** measurements may tempt optimization; F-OBS-Q1-01/register SQ-07 explicitly exclude optimization without separate authorization.
- **Mapping exhaustiveness:** IF-05 must not overpromise open-time detection; the document must state which mismatches are detected at open vs at query.
- **Feasibility:** no new schema/index/ordering/token/cross-target/public-API is needed; existing architecture and the WP-S24-1 acceptance support all proposed checks.
- **Tooling:** a reviewer measurement probe is needed; it must live in tests/`docs/reviews` and change no production code.

**S2.5/Stage-2 dependencies (not discharged):** SQ-04 authentic historical fixtures, SQ-05 expanded Linux qualification, SQ-06 remainder, S-01 integrated proof, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07, platform/independence boundaries.

## 9. Entry disposition

**`ENTRY READY WITH QUALIFICATIONS`.**

WP-S24-2 rests on the accepted WP-S24-1 reader and existing refusal/measurement infrastructure; the remaining items are bounded verification, documentation and measurement obligations requiring no schema, index, ordering, token, cross-target or public-API change. Entry is qualified because (1) IF-05 mapping exhaustiveness and IF-02 realistic-size ordering must be produced and independently reviewed; (2) SQ-07/MD-02 require a reproducible, repetition-based measurement report with no invented thresholds; (3) real-source vs generated-fixture provenance must be explicit; (4) all qualifications and deferred obligations in §11 remain binding. No condition rises to `ENTRY BLOCKED`.

## 10. Proposed next owner commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.4-WP-S24-2-QUAL-PREP` — bounded WP-S24-2 implementation/qualification-preparation (tests, fixtures, reviewer probes, documentation and evidence only).

- **Scope:** produce the IF-05 mapping document + table-driven tests; the IF-02 realistic-size workloads and independent oracle; the SQ-07/MD-02 measurement harness + report; the `S24-SURFACE-Q1` matrix (§7).
- **Exclusions:** production behavior change, optimization, new schema/index/ordering/token format, cross-target, public API, S2.5/Stage 6.
- **Gates:** independent `S24-SURFACE-Q1` (reviewer ≠ implementer) then a separate `WP-S24-2-OWNER` decision.
- **Not authorized by this assessment:** WP-S24-2 implementation; WP-S24-3; S2.5; Stage 6; public Find Usages; pagination optimization.

## 11. Preserved qualifications and exclusions

All S2.3 and WP-S24-1 qualifications are carried forward: **F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08**; the same-harness reviewer independence limitation; the **Windows-x64** and **serialized single-writer** boundary; managed fault-injection durability limits; the **real-authority union restriction**; **F-OBS-Q1-01** (target-local per-page `O(n log n)` sorting — observation, not a violation); **F-OBS-Q1-03** (unauthenticated checksum); **Q2-OBS-01**. Deferred S2.5/Stage-2 obligations remain: **SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07**. No performance, durability, platform or authority claim is broadened; no numeric threshold is invented.

---

*Additive documentation only. No production code, test, oracle/corpus, planning contract, or prior decision was modified; historical artifacts are preserved unchanged.*
