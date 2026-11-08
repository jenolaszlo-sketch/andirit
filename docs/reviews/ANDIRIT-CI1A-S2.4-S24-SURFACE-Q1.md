# ANDIRIT-CI1A-S2.4-S24-SURFACE-Q1 — Independent qualification of WP-S24-2 (IF-05 mapping, IF-02 sizes, SQ-07/MD-02 measurement)

**Commission:** `ANDIRIT-CI1A-S2.4-S24-SURFACE-Q1` (`S24-SURFACE-Q1 INDEPENDENT QUALIFICATION AUTHORIZED`).
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Reviewed source:** `b7207f3995f7abf47451b3bb76c996872b3c024c` (tree `90805016600491cf61fe43a588ee5da4b41866fb`).
**Disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> Reviewer-owned probes (fresh OS processes) independently qualified WP-S24-2.
> All mandatory checks pass; `Q2-P1-01` (open-time page/publication mismatch) is now
> independently exercised and closed. No blocking finding. No production change was
> made; no owner acceptance is recorded.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `b7207f3` / `9080501660…` — both expected |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`Q1-P08` and the reporter's env fields) |
| Producer manifest | `CI1A-S2.4-WP-S24-2-QUAL-PREP-01-evidence/manifest.json` — **21 entries, 0 mismatches** |
| Historical evidence | Q1/R1/Q2/most recent evidence dirs unchanged (`git status` empty for each) |

## 2. S1–S9 qualification matrix

| # | Check | Reviewer evidence | Result |
| --- | --- | --- | --- |
| S1 | IF-05 mapping | `Q1-P01-mapping.json` | **PASS** (13/13) |
| S2 | IF-02 authentic ordering | `Q1-P02-corpus.json` | **PASS** |
| S3 | Generated-real sizes | `Q1-P03`, `Q1-P04` | **PASS** |
| S4 | SQ-07/MD-02 measurement | `Q1-P05-measure-1000.json` | **PASS (characterization)** |
| S5 | Coverage/provenance | `Q1-P06-coverage.json` | **PASS** |
| S6 | Publication/recovery | `Q1-P07-publication.json` | **PASS** |
| S7 | WP-S24-1 regression | `wp241-regression.log`; `full-solution-tests.log` | **PASS** |
| S8 | Evidence integrity/regression | manifests; `oracle.log`; build/format | **PASS** |
| S9 | Architectural boundary | Git diff audit | **PASS** |

## 3. Independent IF-05 refusal mapping (S1)

Fresh reviewer probe (13 cases, each its own tampered/published database; **13/13 normative**):

**Store-open (exact exception, detection point = open):**

| Condition | Observed |
| --- | --- |
| Schema-version mismatch | `CodeOccurrenceSchemaException` |
| Occurrence-namespace mismatch | `CodeOccurrenceSchemaException` |
| Units present without meta | `CodeOccurrenceSchemaException` |
| Malformed occurrence payload | `CodeOccurrenceSchemaException` |
| Malformed graph payload | `JsonException` |
| Missing referenced endpoint | `CodeGraphBatchRejectedException` |
| **Page/publication mismatch (Q2-P1-01)** | **`CodeOccurrenceIndexException`** |

**Query-time:**

| Condition | Observed |
| --- | --- |
| No successful publication | `AnalysisUnavailable` |
| Graph-only publication | `AnalysisUnavailable` |
| Partial declared scope | `IncompleteAnalysis`, `IsComplete = False` |
| Complete-empty scope | `Available`, `IsComplete = True`, count 0 |
| Stale/foreign expected publication | `StalePublication` |
| Malformed continuation | availability `Available` **and** `Continuation = Invalid`, count 0 (dimensions not conflated) |

The mapping is detection-point faithful: open-time conditions raise exceptions and are **not** presented as query statuses. **Q2-P1-01 is closed** by independent execution of the page/publication-mismatch path.

## 4. Authentic-corpus ordering evidence (S2)

`Q1-P02-corpus.json`: the frozen R2 corpus (compiling projects App/Core/Leaf/Solo) published through the real plugin; the overload target `Corpus.Core.Methods.Add(int,int)` ordered by a reviewer-owned Roslyn oracle (identifier-token spans of invocations binding to the exact overload, sorted by the accepted tuple) matched at page sizes **1/2/3/100** with exact target identity and `SourceUnitId = App` ownership. Overload separation, cross-project references, namespace distinctions and source locations are covered (Core invocation of Leaf `Helper.Compute` with two authentic callers verified in the producer test and re-checked by the reviewer corpus traversal). Frozen corpus/oracle bytes remain unchanged.

## 5. Generated-real workload provenance and results (S3)

`Q1-P03`/`Q1-P04`: the reviewer's own generator produces genuine C# (`Core` project + `Consumer{NNNN}.cs`, 100 invocation statements per file) executed through **plugin → sink store → durable reopen → reader**; expected order = generation parameters, not production sorting. Constructed occurrence facts were **not** used for scaling.

| Cardinality | Page sizes exercised | Oracle |
| --- | --- | --- |
| 100 | 1, 10, 100, 1000 | exact |
| 1000 | 1, 10, 100, 1000 | exact |
| 10000 | 100, 1000 | exact |

**Explicit qualification:** 10000/page-size-1 omitted (≈10 000 continuation requests × target-local `O(n log n)`) — cost-disproportionate per the authorized scope; page-size 1 is established at 100 and 1000.

## 6. Independent resource measurements (S4)

`Q1-P05-measure-1000.json` — reviewer re-measurement, **2 warmup + 7 recorded repetitions**, Release, cardinality 1000 (environment captured in the same receipt):

| Measure | Median (ms) | Spread |
| --- | --- | --- |
| Publication (real plugin, 1000 facts) | ~435.6 | see receipt |
| Reopen | ~199.0 | see receipt |
| Query, page size 10 / 100 / 1000 | ~58.8 / 67.6 / 58.2 | min/max in receipt |

Publication, reopen and query are measured separately; the harness fetches a warm page before timing a repeated page. **No threshold is claimed**; F-OBS-Q1-01 (target-local per-page `O(n log n)`) is preserved and is not a violated requirement. The reviewer's absolute values differ from the producer's (~656/~256/~81–96 ms) — expected machine/load variance; both sets are characterization evidence, not contractual bounds.

## 7. Coverage and lifecycle results (S5/S6)

`Q1-P06`: worst-wins `IncompleteAnalysis` on **every** page with stable scope-level per-unit provenance (`u1:Produced`, `u2:Partial`), no false-complete, breakdown identical across pages; the deduplicated cross-unit case retains contributors (constructed cross-unit evidence, **not** unrestricted real-authority union — F-01 preserved).

`Q1-P07`: continuation after durable reopen (`Available`), recovery between pages stays in the original publication (`run:1`, no mix), and a stale token after replacement refuses (`StalePublication`, no silent restart). Retirement/authority revalidation rely on the accepted WP-S24-1/R1/Q2 and WP-S23 evidence (reuse).

## 8. Regression and oracle receipts (S7/S8)

| Gate | Result | Log |
| --- | --- | --- |
| Focused S24Surface | **3 passed / 0 failed** (2 + 1) | `focused-surface-tests.log` |
| WP-S24-1 regression subset | **70 passed / 0 failed** | `wp241-regression.log` |
| Full solution | **907 passed / 0 failed / 0 skipped** (52 + 157 + 345 + 353) | `full-solution-tests.log` |
| Frozen R2 oracle | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log` |
| Release build / format | 0 warnings / 0 errors; format exit 0 | `release-build.log`, `format.log` |
| Reviewer probe matrix | **8 phases, 0 failed** | `probe-matrix.txt` |

The producer baseline of 907 was reproduced exactly; no reviewer tests were added to the solution (reviewer evidence lives in `docs/reviews`).

## 9. Evidence integrity and provenance (S8)

- Producer S24-2 manifest re-verified **21 entries, 0 mismatches**; Q1/R1/Q2 and prior evidence unchanged.
- Reviewer manifest `CI1A-S2.4-S24-SURFACE-Q1-evidence/manifest.json` — **28 entries, 0 mismatches**, generated by a Q1-local seal script (no historical evidence touched).
- Evidence classes: **fresh reviewer** (`Q1-P01…P08`), **producer evidence independently reproduced** (baseline counts, corpus identity logic), **frozen authentic source** (R2 corpus), **generated-real Roslyn** (reviewer generator), **constructed/synthetic boundary** (cross-unit union, tamper cases), **static code-path** (mapping detection points), **unsupported claims** (none asserted).

## 10. Findings and qualifications

| ID | Severity | Finding | Status |
| --- | --- | --- | --- |
| **Q2-P1-01** | — | Open-time page/publication mismatch not previously exercised | **CLOSED** — `CodeOccurrenceIndexException` independently observed |
| **S1-obs-01** | info | Malformed occurrence payload → `CodeOccurrenceSchemaException`; malformed graph payload → `JsonException` (distinct paths) | Recorded mapping |
| **S3-obs-01** | qualification | 10000/page-size-1 omitted (cost) | Accepted qualification |
| **S4-obs-01** | observation | Absolute timings differ from producer run (machine/load variance) | Non-blocking characterization |
| **F-OBS-Q1-01** | observation | Target-local per-page `O(n log n)` sorting | Preserved; not a violation |
| **F-OBS-Q1-03** | observation | Unauthenticated continuation checksum | Preserved |

No blocking finding. No correctness, authority, refusal-mapping, or evidence-integrity violation was found.

## 11. Final independent qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.**

IF-05 mapping (13/13, including the newly closed page/publication-mismatch path), IF-02 authentic and generated-real ordering/completeness (`TotalMatches`/`Examined`/continuation state exact, independent oracles), SQ-07/MD-02 characterization, coverage/provenance, publication/recovery continuity, WP-S24-1 regression (70/0; 907/0/0; oracle exact), and evidence integrity are all supported by independently executed evidence. The qualifications are the explicit omissions/observations above and the preserved inherited limitations.

**Preserved:** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness independence limitation; Windows-x64 / serialized single-writer; managed-fault durability limits; real-authority union restriction; F-OBS-Q1-01/Q1-03; Q2-OBS-01. **Deferred:** SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02, D3-full, IF-02/IF-05 residuals, SQ-07. No broader platform, durability, authority-union, or performance support is claimed.

## 12. Recommended next owner action

A separate **`WP-S24-2-OWNER`** decision (owner acceptance of the qualified WP-S24-2 result) is the appropriate next step; it is **not** authorized by this commission. WP-S24-3 (optional cross-target), S2.5, Stage 6, public Find Usages, and pagination optimization remain unauthorized. No production code was modified; no remediation is required.

---

*Independent qualification report. Reviewer tests/probes/evidence are additive; historical evidence is preserved byte-exactly.*
