# ADR-CI1-01-CI1A-S2.4-ACCEPT-OWNER-01 — Qualified S2.4 owner acceptance

**Commission:** `ANDIRIT-CI1A-S2.4-ACCEPT-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Owner decision:** **`S2.4 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`S2.4-ACCEPT-OWNER` — the mandatory S2.4 internal reader/continuation/coverage and qualification scope ACCEPTED within the qualified boundary below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> S2.4 is accepted on the independently qualified WP-S24-1 and WP-S24-2 packages
> and the `S24-FINAL-Q1` integration qualification, under the preserved
> qualifications and the explicit WP-S24-3 deferral. This is **not** S2.5,
> Stage 2, Stage 6, or public Find Usages acceptance, and it authorizes no
> successor implementation.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Verified preflight and Git ancestry

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `8d2a762e3c3fc7b572e9c237866e4a0282cb7d32` / `dde14ce6d23c2edfe2b34fe612bb39b4d52154d2` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Chain (verified from Git objects): S2.3 accept `3e3209f` → S2.4 entry `9726c3a` → WP-S24-1 impl `de92eeb` → Q1 `04ab118` → R1 `39cb0ef` → Q2 `f37efd7` → WP-S24-1 owner `c0ec72e` → WP-S24-2 entry `a811ace` → prep `b7207f3` → SURFACE-Q1 `495f54f` → WP-S24-2 owner `e93ee04` → closure assess `9e414c8` → FINAL-Q1 `8d2a762`.

## 2. Prerequisite acceptance matrix

| Prerequisite | Decision | Status |
| --- | --- | --- |
| S2.3 | `adr-ci1-01-ci1a-s2-3-accept-owner-01.md` | **ACCEPTED WITH QUALIFICATIONS** |
| S2.4 entry | `adr-ci1-01-ci1a-s2-4-entry-owner-01.md` | **ENTRY AUTHORIZED WITH QUALIFICATIONS** |
| WP-S24-1 | `adr-ci1-01-ci1a-s2-4-wp-s24-1-owner-01.md` (`c0ec72e`) | **ACCEPTED WITH QUALIFICATIONS** |
| WP-S24-2 | `adr-ci1-01-ci1a-s2-4-wp-s24-2-owner-01.md` (`e93ee04`) | **ACCEPTED WITH QUALIFICATIONS** |
| Closure assessment | `ANDIRIT-CI1A-S2.4-CLOSURE-ASSESS-01.md` (`9e414c8`) | `CLOSURE QUALIFICATION READY` |
| S24-FINAL-Q1 | `ANDIRIT-CI1A-S2.4-S24-FINAL-Q1.md` (`8d2a762`) | `SUPPORTED WITH QUALIFICATIONS` |

## 3. F1–F5 final qualification verification

Owner re-check of committed receipts (not summaries):

- **F1.** Frozen R2 authentic facts with a **disclosed constructed partial-coverage state**: every page `IncompleteAnalysis`, `IsComplete = false`, per-unit provenance `App:Partial` stable, all 8 target occurrences represented, deterministic order, no duplicate/omission/false-complete. No unrestricted authentic cross-unit authority union claimed.
- **F2.** `run:a` real publication → durable reopen continuation → managed recovery preserving `run:a` → frozen `source-changed` variant as `run:b` → old token `StalePublication` (0 hits) → explicit restart serving `run:b`. No silent restart or mixed publication. **Managed recovery ≠ crash/power-loss durability.**
- **F3.** Authority-bearing publication with tampered occurrence-page `PublicationIndexRunId` → **open-time `CodeOccurrenceIndexException`**; no false `Available`; correct detection boundary. **Q2-P1-01 closed on the authority path.** Open-time exceptions remain distinct from query-time statuses.
- **F4.** Generated-real 1000 (Roslyn → plugin → sink → durable store → reader): oracle-exact at page sizes 1/10/100/1000; per-page authority revalidation; missing authority → `AnalysisUnavailable`; coverage breakdown; strictly-after continuation; bounds/terminal correct; stale refusal after publication change. Generated-real evidence is distinct from the frozen corpus.
- **F5.** Focused S24 **20/0**; full solution **907/0/0** (52 + 157 + 345 + 353); oracle **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`; Release 0/0; format exit 0; historical manifests 21/0, 28/0, 27/0, 41/0, 19/0; FINAL-Q1 manifest **17/0**; historical evidence unmodified.

## 4. Accepted S2.4 capability boundary

Within the qualified scope the owner accepts: internal exact-target occurrence continuation; stateless, versioned, canonical, non-persisted tokens; publication-bound pagination; per-request authority revalidation; deterministic total ordering and strictly-after continuation; malformed-token and stale-publication refusal; scope-level coverage breakdown and contributor provenance; availability/coverage/continuation/completeness separation; IF-05 exception/status mapping within demonstrated boundaries; IF-02 realistic-size ordering/limit evidence; SQ-07/MD-02 reproducible resource characterization; recovery and replacement integration; D3 graph noninterference; legacy reader compatibility — on **Windows x64**, serialized single-writer, serialized store.

**Not accepted:** constant-time pagination, unrestricted real-authority union, universal exception mapping, cryptographic token authentication, crash/power-loss durability, cross-target/multi-target enumeration, public Find Usages.

## 5. Evidence integrity and provenance

All six evidence manifests verified with 0 mismatches; historical evidence untouched (`git diff` clean across Q1/R1/Q2/SURFACE-Q1/QUAL-PREP directories); frozen corpus/oracle bytes unchanged; 0 production/test change during S24-FINAL-Q1. Fresh reviewer evidence (F1–F4, gates), authentic frozen-source evidence (corpus + variant), generated-real Roslyn evidence (F4), constructed boundary controls (F1 partial, F3 tamper, F4 negatives), and inherited independent evidence (Q1/R1/Q2, SURFACE-Q1) are distinguished; overlapping counts are not aggregated as independent assurance.

## 6. Binding qualification register

**Resolved/closed:** **F-Q1-01 RESOLVED**, **F-OBS-Q1-02 RESOLVED**, **Q2-P1-01 CLOSED** (including the authority path).

**Preserved (binding):** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer independence limitation; Windows-x64 and serialized single-writer; managed fault-injection durability limits; restricted real-authority union evidence; **F-OBS-Q1-01** (target-local `O(n log n)` per-page sorting); **F-OBS-Q1-03** (unkeyed checksum, not authentication); Q2-OBS-01; S1-obs-01, S3-obs-01, S4-obs-01. Resolved findings are not resurrected; preserved qualifications are not discharged.

## 7. Deferred Stage-2 obligations (non-blocking for bounded S2.4)

**SQ-04** authentic historical fixtures; **SQ-05** expanded Linux native qualification; **SQ-06** remaining independent/owner obligations; **S-01** integrated proof; **D4.5**; **full S-02**; **D3-full**; residual **IF-02/IF-05/SQ-07** obligations. Each is classified non-blocking for the bounded S2.4 acceptance under the authoritative decisions; no broader Stage-2 acceptance is implied.

## 8. Explicit WP-S24-3 deferral

WP-S24-3 (bounded cross-target continuation) is **optional** under the authoritative S2.4 acceptance contract (entry decision §8/IF-04; Stage-2 boundary §8 gate contains no cross-target requirement; acceptance matrix has no cross-target gate). Exact-target aggregation of contributors across storage units remains supported within its accepted qualifications; **multi-target enumeration is not included** in accepted S2.4 functionality; **public Find Usages is excluded** (ADR-CI1-02). WP-S24-3 remains **deferred and unauthorized**; the deferral is **not** a permanent rejection or a declaration that the capability is unnecessary. The accepted scope determination is not reopened without new authoritative evidence.

## 9. Formal owner disposition

**`S2.4 ACCEPTED WITH QUALIFICATIONS`.** Acceptance is limited to the mandatory S2.4 reader, continuation, coverage and qualification scope, subject to §4, §6, §7 and §8. It does not represent completion of S2.5 or the entire Stage-2 roadmap.

## 10. Successor authorization status

- **WP-S24-1: closed. WP-S24-2: closed. S2.4: closed within the qualified scope above.**
- **WP-S24-3: optional, deferred, unauthorized.**
- **S2.5: not authorized** — a separate S2.5 entry assessment is recommended following this acceptance.
- **Stage 6: not authorized. Public Find Usages: not authorized. Pagination/index optimization: not authorized.**
- No new public API, persisted format, migration, or dual-write is authorized.

## 11. Repository discipline

Only this decision document and the additive S2.4 status note to the S2.4 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, prior decision, or S2.3/S2.4 contract is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.
