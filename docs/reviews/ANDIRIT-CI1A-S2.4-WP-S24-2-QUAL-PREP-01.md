# ANDIRIT-CI1A-S2.4-WP-S24-2-QUAL-PREP-01 — WP-S24-2 qualification preparation (producer-side)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-2-QUAL-PREP-01` (`WP-S24-2 QUALIFICATION PREPARATION AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Starting HEAD / tree:** `a811ace2e5983a5a5860ad068131873780d321a2` / `e5314fdad518fd23f95cb66a5cebac1972a76482`.
**Disposition:** **`QUALIFICATION PREPARATION COMPLETE — READY FOR INDEPENDENT REVIEW`**.

> Producer-side qualification preparation: tests, generated-real fixtures, probes,
> documentation and additive evidence only. **No production code was modified.** This
> is **not** independent `S24-SURFACE-Q1` qualification and **not** WP-S24-2 owner
> acceptance.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `a811ace` / `e5314fda…` — both expected |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`P05-environment.json`) |
| WP-S24-1 status | accepted with qualifications (`c0ec72e`) |

## 2. IF-05 exception/status mapping matrix (P1)

Observed on the real native store by `tests/Andirit.LatticeDb.Tests/S24SurfaceRefusalMappingTests.cs::If05_MappingMatrix_MatchesAcceptedContract` (table-driven, each case a genuine published/tampered database; **passing**):

**Open-time failures (exception, never converted to a query status):**

| Trigger | Detection | Observed |
| --- | --- | --- |
| Occurrence schema **version** mismatch | store open | `CodeOccurrenceSchemaException` |
| Occurrence schema **namespace** mismatch | store open | `CodeOccurrenceSchemaException` |
| Occurrence units/pages **without meta** | store open | `CodeOccurrenceSchemaException` |
| **Malformed occurrence unit payload** (non-canonical/incompatible) | store open | `CodeOccurrenceSchemaException` (`ActualVersion 0`) |
| **Malformed graph unit payload** (`HetuUnit`) | store open | `JsonException` |
| **Missing graph edge endpoint** after complete reconstruction | store open | `CodeGraphBatchRejectedException` |

**Query-time statuses:**

| Trigger | Detection | Observed |
| --- | --- | --- |
| No successful publication | query | `AnalysisUnavailable` |
| Graph-only publication | query | `AnalysisUnavailable` (no coverage) |
| Partial declared scope | query | `IncompleteAnalysis`, `IsComplete = false` |
| Complete declared scope, zero facts | query | `Available`, `IsComplete = True` |
| Stale/foreign expected publication handle | query | `StalePublication` (`Examined = 0`) |
| **Malformed continuation token** | query | `Continuation = Invalid`, 0 occurrences, availability **unchanged** (D5 separation) |

**Reused mapping (already covered by accepted tests; not re-implemented here):**
- Authority-bearing: missing current authority → `AnalysisUnavailable`; stale/foreign authority → `StalePublication`; corrupt/substituted authority → `CorruptedIndex`; unsupported authority version → `UnsupportedSchema`; orphaned occurrence unit → `CorruptedIndex`; authority-requiring graph unit without association → `CorruptedIndex` (WP-S23-2 R1, S23-PUB-Q1/Q2, S23-LIFE-Q1, S24-READER-Q1/Q2).
- Page/publication mismatch on open (`CodeOccurrenceIndexException`): production path present (`RestoreOccurrences`); **not freshly exercised by this commission** — recorded as a reviewer-checkable item (S1).

**Contract statement:** an open-time exception is an open-time exception; this mapping does **not** claim a universal catch-and-map policy, and it does not claim every mismatch is detected at open.

## 3. Authentic frozen-corpus results (P2)

`tests/Andirit.CSharp.Tests/S24SurfaceAuthenticCorpusTests.cs` (2 tests, passing) publishes the **compiling** corpus projects (App, Core, Leaf, Solo; Negatives/External excluded because they intentionally do not compile and would make the verified universe incomplete) through the **real C# plugin**, then paginates real targets:

- **Overload target** `Corpus.Core.Methods.Add(int,int)`: independent expected spans derived from the App syntax trees (identifier-token spans of invocations binding to the exact overload); pagination at page sizes **1/2/3/7/100** reproduced the accepted-tuple order exactly, with no duplicates/omissions, `TotalMatches`/`Examined` = real count, correct `More`/`Complete` continuation state, and `Truncated`/`IsComplete` for the whole-scope page. Exact target binding and `SourceUnitId = App` unit asserted per hit.
- **Cross-project target** `Corpus.Leaf.Helper.Compute`: two authentic callers (Core.LeafUser and App.Sites) serve 2 occurrences, all bound to the Leaf target; calling unit owns each occurrence and the defining unit is never a source unit.

Frozen corpus and oracle bytes were **not** modified.

## 4. Generated-real workload inventory (P3)

The probe (`docs/reviews/CI1A-S2.4-WP-S24-2-QUAL-PREP-01-tests`) generates genuine C# source compiled and ingested through the real plugin: a `Core` project (`Core.Api.Ping`), one `Consumer` project, and `Consumer{NNNN}.cs` files each with 100 invocation statements on distinct lines.

| Workload | Files (incl. 2 projects) | Target cardinality | Fixture class |
| --- | --- | --- | --- |
| G-100 | 4 | 100 | generated-real |
| G-1000 | 13 | 1000 | generated-real |
| G-10000 | 103 | 10000 | generated-real |

Parameters: fixed generation (deterministic, no randomness), all calls to one shared `Core.Api.Ping` target; `compileMs`/`publishMs` recorded in `P01`–`P03`. **Generated-real workloads are labelled and are not authentic historical workloads**; constructed occurrence facts were not used for scaling.

## 5. Independent expected-order oracle methodology

- **Authentic corpus:** expected occurrence spans are read from the **live Roslyn syntax trees** (invocation expressions whose `SymbolInfo.Symbol` is the exact method symbol; identifier-token spans), then ordered by the accepted tuple (`path` ordinal, start line/column, end line/column). Production sorting/mapping code is never consulted.
- **Generated-real:** expected spans are the generation parameters themselves (file path, statement line, computed identifier column), ordered by the same tuple.
- Expected **targets** are recomputed from the frozen R2 authority (`CompilationProvenance.Describe`/`ProvenanceHashing`) and cross-checked against `CodeOccurrenceIdentity.ComputeTargetId`.

## 6. Realistic-size pagination results (P4)

All combinations below report `matchesOracle = true` (complete concatenation equals the independent oracle) with `distinct == cardinality` (`P01`–`P03`, `probe-matrix.txt`):

| Workload | Page sizes executed | Pages (largest page size) | Result |
| --- | --- | --- | --- |
| G-100 | 1, 10, 100, 1000 | 1 (100) | PASS |
| G-1000 | 1, 10, 100, 1000 | 1 (1000) | PASS |
| G-10000 | 10, 100, 1000 | 10 (1000) | PASS |

**Omitted combination (recorded, justified):** G-10000 with page size **1** (≈10,000 continuation requests × per-page target-local `O(n log n)` sort) was omitted as disproportionately expensive; correctness at cardinality 10,000 is covered by page sizes 10/100/1000, and page-size 1 is covered at 100 and 1000. **Boundary micro-cases** (equal-position ties, exact page boundaries, page-size limits) are covered by the accepted WP-S24-1 tests and the Q2 reviewer oracle (reused).

## 7. SQ-07 / MD-02 measurement methodology and results (P5)

Harness: `S24SurfaceProbe measure` with **2 warmup + 7 recorded repetitions**, Release, cardinality 1000 (`P04-measure-1000.json`; environment in `P05` and the report header). No threshold is claimed.

| Measure | Observed (median; ms) | Notes |
| --- | --- | --- |
| Publication (real plugin, 1000 facts) | ~655.7 | single run per repetition; includes plugin extraction + ingestion |
| Reopen | ~255.5 | fresh `LatticeCodeGraphStore` open |
| Query, page size 10 / 100 / 1000 | ~80.7 / ~82.0 / ~95.7 | target-local per-request sort (`Examined` = 1000); consistent with F-OBS-Q1-01 |

Distinguished: publication vs reopen vs query; first vs repeated page (the harness fetches a warm page then measures a repeated page). Allocated bytes are recorded per scaling combination (`P01`–`P03`). Peak memory/CPU were not captured in this pass (feasibility); the environment record includes processors/GC mode. **No constant-time, index-backed, or globally bounded retrieval is claimed.**

## 8. Coverage/lifecycle evidence (P6)

- **Complete publication + provenance + complete-empty:** authentic corpus tests assert `Available` + `IsComplete` on a complete universe; complete-empty is asserted in the IF-05 matrix (`Available:IsComplete=True` on a zero-fact `Produced` unit).
- **Partial coverage → page-level independence:** reused — WP-S24-1 `PartialCoverage_RemainsIncompleteAcrossPages` (worst-wins `IncompleteAnalysis` on every page; scope-level breakdown stable).
- **Replacement/reindex, stale continuation, durable reopen, recovery between pages:** reused — WP-S24-1 (`I03_I07_*`, `P07`), R1/Q2 probes, and accepted WP-S23-2/3A suites.
- **Real-authority union restriction preserved:** F-01 retained; synthetic union evidence is not presented as unrestricted real-authority proof. No false-complete/false-available result was observed.

## 9. Regression receipts (P7)

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |
| New S24Surface tests | `--filter FullyQualifiedName~S24Surface` | **3 passed** (2 CSharp + 1 LatticeDb) | `focused-s24surface-tests.log` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **907 passed / 0 failed / 0 skipped** (52 + 157 + 345 + 353) | `full-solution-tests.log` |
| Frozen R2 oracle | `SemanticCorpusOracle` | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |
| Probe matrix | `run-probes.ps1` | 5 phases, 0 failed | `probe-matrix.txt` |

Baseline before this package was 904 passed; post-change 907 = 904 + 3 new tests. Evidence manifest: **21 entries, 0 mismatches** (`manifest.json`, produced by the S24-2-local seal script; no historical evidence was touched).

## 10. Findings and limitations

- **No production defect found.** All observed conditions match the accepted contract; the IF-05 mapping required recording the *actual* behavior (e.g., a malformed **occurrence** payload → `CodeOccurrenceSchemaException`, whereas a malformed **graph** payload → `JsonException`).
- **Q2-P1-01 (open-time page/publication mismatch):** the `CodeOccurrenceIndexException` path is present but was not freshly exercised; handed to `S24-SURFACE-Q1` as a reviewer-checkable item.
- **Omitted combination:** G-10000/page size 1 (cost), as recorded in §6.
- **Measurement limitations:** allocations captured for scaling; peak memory/CPU not captured; measurements are characterization only (no threshold); F-OBS-Q1-01 (target-local `O(n log n)`) preserved and **not** treated as a violation.
- **P6** is largely **reused** accepted evidence; only complete-publication/complete-empty/authentic-corpus coverage was freshly exercised.
- **Generated-real vs authentic:** generated workloads are labelled; frozen corpus/oracle bytes unchanged.
- **No new schema, index, ordering, token-format, cross-target, or public-API change** was introduced.

## 11. Qualification readiness

**`QUALIFICATION PREPARATION COMPLETE — READY FOR INDEPENDENT REVIEW`.** The IF-05 matrix, IF-02 authentic/generated-real pagination evidence, SQ-07/MD-02 characterization, and the regression/oracle floors are in place with a self-excluding manifest; no blocking finding was discovered. Independent `S24-SURFACE-Q1` execution is required; this preparation is **not** independent qualification.

## 12. S24-SURFACE-Q1 handoff matrix (P8)

| # | Check | Producer-executed here | Reviewer-required |
| --- | --- | --- | --- |
| S1 | IF-05 mapping exhaustiveness | 12-case table-driven matrix + reused authority mappings | Re-derive mapping independently; exercise open-time page/publication mismatch (`CodeOccurrenceIndexException`) |
| S2 | IF-02 real-size ordering | Authentic corpus (2 tests) + generated-real 100/1000/10000 vs independent oracle | Independent reviewer oracle + fresh-process re-execution |
| S3 | Boundary cases | Reuse WP-S24-1/Q2 (ties, boundaries); authentic overload identity | Independent boundary probes |
| S4 | SQ-07/MD-02 characterization | 2+7-repetition harness (`P04`) | Independent re-measurement and reproduction |
| S5 | Coverage/provenance | Authentic corpus + reuse (WP-S24-1 partial pages) | Independent scope-vs-page checks |
| S6 | Publication/recovery | Reuse (WP-S24-1/R1/Q2, WP-S23-2/3A) | Independent fresh-process probes |
| S7 | WP-S24-1 regression | Full solution 907/0/0 + focused 3/0 | Independent re-run |
| S8 | Evidence integrity | 21-entry manifest, 0 mismatches; historical evidence untouched | Verify manifest/hashes |
| S9 | Authorized boundary | No production change; no schema/index/ordering/token/cross-target/API change | Confirm from Git |

**Blocking vs non-blocking:** S1/S2/S3/S5/S6/S7/S8/S9 findings are blocking; S4 magnitude is non-blocking characterization unless a normative bound is violated.

---

*Producer-side preparation. No production code, frozen corpus/oracle, historical evidence, prior decision, or planning contract was modified. Independent `S24-SURFACE-Q1` qualification and WP-S24-2 owner acceptance remain outstanding.*
