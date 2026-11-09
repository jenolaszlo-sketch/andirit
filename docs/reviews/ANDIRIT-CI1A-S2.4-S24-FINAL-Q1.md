# ANDIRIT-CI1A-S2.4-S24-FINAL-Q1 — Final independent integration qualification of S2.4

**Commission:** `ANDIRIT-CI1A-S2.4-S24-FINAL-Q1` (`S24-FINAL-Q1 AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Reviewed source:** `9e414c86abf73a61e2f07ef56ca6c0bb0ce81f07` (tree `eb3afc358841daf619133d90564258e012aa0f66`).
**Disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> Reviewer-owned probes (fresh OS processes) executed the closure assessment's
> F1–F5 matrix. All pass; no blocking finding. No production change; no S2.4 owner
> acceptance recorded.

---

## 1. Preflight and Git ancestry

| Check | Result |
| --- | --- |
| HEAD / tree | `9e414c86…` / `eb3afc35…` — both expected |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |
| Chain | WP-S24-1 owner `c0ec72e`; WP-S24-2 owner `e93ee04`; closure assessment `9e414c8` |

## 2. F1–F5 qualification matrix

| # | Case | Evidence | Result |
| --- | --- | --- | --- |
| F1 | Real-corpus pagination with constructed partial coverage | `Q1-F1.json` | **PASS** |
| F2 | Real-authority recovery / source-change / stale continuation | `Q1-F2.json` | **PASS** |
| F3 | Authority-bearing page/publication mismatch | `Q1-F3.json` | **PASS** |
| F4 | Generated-real 1000 integrated traversal | `Q1-F4.json` | **PASS** |
| F5 | Regression, oracle, evidence, boundary | logs + manifest | **PASS** |

## 3. Fresh independent integration results

- **F1** — the frozen R2 corpus's authentic App unit was extracted with the accepted extractor (8 real facts on the 2-arg `Add` target); a **constructed `Partial` coverage state** (clearly separated from the authentic facts) was published. Pagination at page sizes 1/2/100 returned `IncompleteAnalysis` on **every** page with `IsComplete = false`, stable scope-level per-unit provenance (`App:Partial`), `TotalMatches`/`Examined` = 8, exact target and `SourceUnitId = App`, and the concatenated order matched the accepted-tuple oracle with **no duplicates/omissions**. Hybrid case explicitly **not** claimed as unrestricted authentic real-authority union.
- **F2** — real plugin publication (`run:a`), first page + token; durable reopen continued in `run:a`; a managed before-commit recovery kept the publication; the frozen `variants/source-changed/App/Sites.cs` was published as `run:b`; the original token was **refused** (`StalePublication`, 0 hits); explicit restart served `run:b`. No mixed-publication page, no silent restart.
- **F3** — on an **authority-bearing** plugin publication, tampering the occurrence page's `PublicationIndexRunId` caused **open-time `CodeOccurrenceIndexException`** (extends the IF-05 mapping to the authority path; no false `Available`, no corruption recovery).
- **F4** — generated-real 1000 genuine Roslyn facts through plugin → sink → store → reader: page sizes 1/10/100/1000 all matched the source-derived oracle; per-page authority revalidation, coverage breakdown present, `TotalMatches = Examined = 1000`, page-size bounds and terminal state correct; **missing authority → `AnalysisUnavailable`**; **changed publication → old token refused** (`StalePublication`/`CorruptedIndex`).

## 4. Evidence classification

Fresh reviewer evidence (F1–F4 probes, gates); authentic frozen-source evidence (R2 corpus + `source-changed` variant; bytes unchanged); generated-real Roslyn evidence (F4, reviewer generator); constructed boundary controls (F1 partial coverage, F3 tamper, F4 negative controls); inherited independent evidence (WP-S24-1 Q1/R1/Q2, WP-S24-2 SURFACE-Q1); producer evidence (WP-S24-2 prep, reproduced floors only). Static: none required beyond prior mapping analysis.

## 5. Publication, recovery, authority findings

F2/F4 confirm publication run/identity continuity, authority and carrier preservation, recovery consistency, stale refusal and refusal precedence. No mixed-publication page, no stale available occurrence, no incorrect complete-empty.

## 6. Coverage, continuation, refusal findings

F1 (partial scope across pages), F3 (open-time mismatch), F4 (bounded pages, terminal state, negative controls) confirm scope-level coverage, continuation correctness, and the IF-05 separation of open-time exceptions from query-time availability/continuation. No false-complete/false-available.

## 7. Regression and oracle receipts (F5)

| Gate | Result |
| --- | --- |
| Release build | 0 warnings / 0 errors |
| Focused S24 (CSharp 18, LatticeDb 2) | 20 passed / 0 failed |
| Full solution | **907 passed / 0 failed / 0 skipped** (52 + 157 + 345 + 353) |
| Frozen R2 oracle | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Formatting | exit 0 |

## 8. Evidence manifest verification

Producer/reviewer manifests re-verified: QUAL-PREP 21/0; SURFACE-Q1 28/0; READER-Q1 27/0; READER-Q2 41/0; R1 19/0. New `CI1A-S2.4-S24-FINAL-Q1-evidence/manifest.json`: **17 entries, 0 mismatches**, Q1-local generation; historical evidence untouched.

## 9. Combined independent-review synthesis

Fresh integration evidence (this commission) covers the four residual interactions; inherited independent evidence (WP-S24-1 Q2, WP-S24-2 SURFACE-Q1) covers the established reader/mapping/size/measurement surface. Combined, the bounded S2.4 contract is supported: exact-target continuation, publication-bound pagination, per-request authority, deterministic ordering/refusal, scope-level coverage, contributor provenance, recovery/replacement, IF-05 mapping, IF-02 ordering/limits, SQ-07/MD-02 characterization, D3 noninterference. Overlapping counts are not aggregated as separate assurance.

## 10. Findings

No blocking finding. Observations preserved: F-OBS-Q1-01, F-OBS-Q1-03, Q2-OBS-01, S1-obs-01, S3-obs-01, S4-obs-01. **Q2-P1-01 remains closed** (now also on the authority path).

## 11. Binding qualifications and deferred obligations

F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness independence limitation; Windows-x64 / serialized single-writer; managed fault-injection durability limits; real-authority union restriction; F-OBS-Q1-01 (target-local `O(n log n)`); F-OBS-Q1-03 (unkeyed checksum); Q2-OBS-01; S1/S3/S4 observations. Deferred (not discharged): SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02, D3-full, residual IF-02/IF-05/SQ-07.

## 12. WP-S24-3 deferral confirmation

Exact-target multi-unit contributor aggregation is within the accepted scope; multi-target enumeration remains **deferred** (optional, unauthorized); public Find Usages remains **excluded**; WP-S24-3 **unauthorized**. No new authoritative evidence reopens this.

## 13. Final qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.** F1–F5 pass with no blocking findings; the inherited qualifications remain binding.

## 14. Recommendation

Proceed to a separate **`S2.4-ACCEPT-OWNER`** decision recording the accepted S2.4 scope, preserved qualifications, the explicit WP-S24-3 deferral, and the deferred S2.5/Stage-2 obligations. Not authorized by this commission.

---

*Reviewer qualification report. No production code, corpus/oracle, historical evidence, or prior decision was modified.*
