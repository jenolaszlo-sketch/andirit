# ADR-CI1-01-CI1A-S2.4-WP-S24-2-OWNER-01 — Qualified owner acceptance of WP-S24-2 (IF-05 mapping, IF-02 sizes, SQ-07/MD-02 measurement)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-2-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Owner decision:** **`WP-S24-2 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`WP-S24-2` (bounded evidence completion for IF-05 refusal mapping, IF-02 realistic-size ordering/limits, SQ-07/MD-02 resource characterization, and supporting F10/MD-03 evidence) ACCEPTED within the qualified scope below, after the `S24-SURFACE-Q1` independent qualification.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> This decision accepts the bounded WP-S24-2 evidence package — entry assessment
> (`a811ace`), producer qualification preparation (`b7207f3`), and independent
> `S24-SURFACE-Q1` qualification (`495f54f`, `SUPPORTED WITH QUALIFICATIONS`). It
> authorizes no successor work: WP-S24-3, S2.4 overall acceptance, S2.5, Stage 6,
> public Find Usages, and pagination optimization remain unauthorized.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Verified preflight and Git ancestry

| Object | Value |
| --- | --- |
| Entry HEAD | `495f54f06806ff681ceb23c34f383b98b2874fff` |
| Entry tree | `c4b0f1e71bcce225aa575a80e1d9990adf191d45` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Consecutive chain (verified from Git objects):

| Stage | Commit | Tree |
| --- | --- | --- |
| WP-S24-2 entry assessment | `a811ace2e5983a5a5860ad068131873780d321a2` | `e5314fdad518fd23f95cb66a5cebac1972a76482` |
| Producer qualification preparation | `b7207f3995f7abf47451b3bb76c996872b3c024c` | `90805016600491cf61fe43a588ee5da4b41866fb` |
| Independent `S24-SURFACE-Q1` qualification | `495f54f06806ff681ceb23c34f383b98b2874fff` | `c4b0f1e71bcce225aa575a80e1d9990adf191d45` |

## 2. Qualification-chain reconstruction

1. **Entry** (`a811ace`): `ENTRY READY WITH QUALIFICATIONS` for WP-S24-2 as evidence completion only, with no schema/index/ordering/token/cross-target/public-API change.
2. **Bounded authorization**: `WP-S24-2 QUALIFICATION PREPARATION AUTHORIZED WITH QUALIFICATIONS` (tests, generated-real fixtures, probes, measurements, documentation, additive evidence only).
3. **Producer preparation** (`b7207f3`): IF-05 table-driven mapping test, authentic-corpus tests, generated-real scaling probe (100/1000/10000), SQ-07/MD-02 measurement harness, coverage/publication probes, evidence package (21 entries).
4. **Independent qualification** (`495f54f`): reviewer-owned probes in fresh processes (mapping 13/13, corpus oracle, generated-real oracle, independent measurement, coverage, publication), regression floors, reviewer evidence package (28 entries).
5. **Final qualification disposition**: `SUPPORTED WITH QUALIFICATIONS`.
6. **No unresolved blocking finding**; `Q2-P1-01` closed.

## 3. S1–S9 evidence verification

The owner re-verified the committed artifacts (not report summaries).

| # | Check | Committed evidence | Result |
| --- | --- | --- | --- |
| S1 | IF-05 mapping | `Q1-P01-mapping.json` | **13/13 normative** |
| S2 | Authentic ordering | `Q1-P02-corpus.json` | page sizes 1/2/3/100 match the Roslyn oracle |
| S3 | Generated-real | `Q1-P03`, `Q1-P04` | 100/1000 (1/10/100/1000) and 10000 (100/1000) oracle-exact |
| S4 | SQ-07/MD-02 | `Q1-P05-measure-1000.json` | 2 warmup + 7 reps; publication ≈435.6 ms, reopen ≈199.0 ms, queries ≈58.8/67.6/58.2 ms |
| S5 | Coverage/provenance | `Q1-P06-coverage.json` | worst-wins partial, stable per-unit provenance, no false-complete |
| S6 | Publication/recovery | `Q1-P07-publication.json` | reopen / recovery-in-run / stale refusal, no blend |
| S7 | WP-S24-1 regression | `wp241-regression.log` (70/0), `full-solution-tests.log` (907/0/0) | pass |
| S8 | Integrity/regression | producer manifest **21/0**; reviewer manifest **28/0**; oracle **185/185**; build 0/0; format exit 0 | pass |
| S9 | Boundary | no `src`/`tests` change since `b7207f3` | pass |

**S1 detail.** Open-time: schema version/namespace and malformed occurrence payload → `CodeOccurrenceSchemaException`; malformed graph payload → `JsonException`; missing endpoint → `CodeGraphBatchRejectedException`; **page/publication mismatch → `CodeOccurrenceIndexException`** (Q2-P1-01 closed). Query-time: `AnalysisUnavailable`/`IncompleteAnalysis`/`Available`/`StalePublication`, and malformed continuation → availability `Available` with `Continuation = Invalid` (dimensions distinct, no universal open-time conversion). **S2 detail:** exact-target identity, overload separation, source positions, and contributor ownership confirmed; frozen corpus/oracle bytes unchanged. **S3 detail:** workloads are genuine Roslyn source through plugin → sink → store → reader; generated-real evidence is distinct from the frozen authentic corpus; S3-obs-01 (10000/page-size-1 omitted) preserved. **S4 detail:** separate publication/reopen/query measurement; characterization only, no threshold; timing variance (S4-obs-01) preserved.

## 4. Accepted evidence/capability boundary

WP-S24-2 is accepted as completing the bounded **evidence** package for: IF-05 refusal and exception mapping; IF-02 realistic-size ordering and limits; SQ-07/MD-02 resource characterization; and supporting F10/MD-03 evidence within the assessed scope. This is **evidence qualification, not new production capability**. The owner does **not** accept unrestricted realistic-scale performance, cross-target querying, universal exception mapping, or platform-wide qualification.

## 5. Finding dispositions

| Finding | Disposition |
| --- | --- |
| **Q2-P1-01** (open-time page/publication mismatch untested) | **CLOSED** — `CodeOccurrenceIndexException` independently observed |
| **S1-obs-01** (distinct malformed occurrence vs graph paths) | **PRESERVED** (recorded mapping) |
| **S3-obs-01** (10000/page-size-1 omitted) | **PRESERVED** (explicit qualification) |
| **S4-obs-01** (timing variance vs producer run) | **PRESERVED** (characterization, non-gating) |
| **F-OBS-Q1-01** (target-local per-page `O(n log n)`) | **PRESERVED** (observation; not a D4 violation) |
| **F-OBS-Q1-03** (unauthenticated continuation checksum) | **PRESERVED** |

No new finding is required by the committed evidence; no non-gating characterization observation is treated as a contract violation.

## 6. Binding qualifications (retained, not resolved)

All S2.3 and WP-S24-1 qualifications remain binding: **F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08**; the **same-harness reviewer independence limitation**; **Windows-x64** and the **serialized single-writer** model; **managed fault-injection durability limits**; the **real-authority union restriction**; the **unauthenticated token checksum** (F-OBS-Q1-03); the **target-local `O(n log n)` per-page sorting observation** (F-OBS-Q1-01); and **Q2-OBS-01**. No broader performance, durability, platform or authority-union support is accepted. Optimization is not authorized.

## 7. Deferred S2.5 / Stage-2 obligations (not discharged)

**SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02, D3-full**, and any residual IF-02/IF-05/SQ-07 work not explicitly supported by accepted evidence remain open. WP-S24-2 acceptance discharges none of them.

## 8. Formal owner decision

**`WP-S24-2 ACCEPTED WITH QUALIFICATIONS`.**

The bounded WP-S24-2 evidence package is accepted on the independently qualified evidence above, restricted to the IF-05 mapping, IF-02 realistic-size ordering/limits, SQ-07/MD-02 characterization, and supporting F10/MD-03 evidence within the assessed scope, subject to every qualification in §6 and every deferred obligation in §7.

## 9. Successor authorization status

- **WP-S24-1** is accepted with qualifications; **WP-S24-2 is closed** within its accepted scope only.
- **WP-S24-3** (bounded cross-target continuation, optional per the S2.4 entry decision §8) remains **unauthorized**; it is **not** decided unnecessary here.
- **S2.4 overall acceptance is not yet recorded.** A separate **S2.4 closure / optional-WP-S24-3 decision assessment** is recommended; it must consult the Stage-2 boundary §8 and the Stage-2 acceptance matrix (S2.4 gate: real-project end-to-end query assertions with coverage breakdown; cross-target optional) before deciding whether WP-S24-3 is required.
- **S2.5 and Stage 6 remain unauthorized**; **public Find Usages remains unauthorized**; **pagination optimization is not authorized**.
- No new public API, persisted format, migration, or dual-write is authorized.

## 10. Repository discipline

Only this decision document and the additive WP-S24-2 status note to the S2.4 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, prior decision, or S2.3 contract is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.
