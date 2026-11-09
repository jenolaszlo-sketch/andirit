# ANDIRIT-CI1A-S2.4-CLOSURE-ASSESS-01 — S2.4 closure-readiness and optional WP-S24-3 scope assessment

**Commission:** `ANDIRIT-CI1A-S2.4-CLOSURE-ASSESS-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Role:** independent closure-readiness assessment; documentation only.
**Disposition:** **`CLOSURE QUALIFICATION READY`**.

> This assessment authorizes nothing. It performs no qualification, no test
> execution, no owner acceptance, and no WP-S24-3 authorization. The proposed
> `S24-FINAL-Q1` commission in §11 is a draft for separate owner authorization only.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `e93ee04492ce94511a05be7754a2f26845914a0a` / `f01bbe7946f22a56f68583f269e1dc7c7189c342` — both expected |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |
| S2.4 decisions | entry (`9726c3a`), WP-S24-1 owner (`c0ec72e`), WP-S24-2 owner (`e93ee04`) present |
| Evidence manifests | QUAL-PREP 21/0; SURFACE-Q1 28/0; READER-Q1 27/0; READER-Q2 41/0; R1 19/0 — **0 mismatches** |

No unexpected state, inconsistent authority, or evidence-integrity failure was found.

## 2. Authoritative S2.4 requirements matrix

| # | Requirement | Authoritative source | Mandatory for S2.4? | Package | Qualification evidence | Remaining limitation | Extra integration evidence? |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Exact-target continuation | entry decision §3–§5; boundary §8 (S2.4); IF-04 | Yes | WP-S24-1 | Q1/R1/Q2 (18 tests + reviewer probes); SURFACE-Q1 | — | No (accepted) |
| 2 | Deterministic pagination | entry decision §5 (D3); boundary §8 "bounded ordering and explicit limits" | Yes | WP-S24-1 | Q2 oracle; SURFACE-Q1 corpus/generated-real oracles | 10000/page-1 omitted (S3-obs-01) | Representative integrated cases (§7) |
| 3 | Publication-bound token behavior | entry decision §3/§4 (D1/D2) | Yes | WP-S24-1 | Q1/Q2 token matrices; SURFACE-Q1 publication probe | — | No |
| 4 | Publication-change refusal | entry decision §4 (D2) | Yes | WP-S24-1 | Q2 precedence; SURFACE-Q1 stale/no-blend | — | Real-corpus replacement combo (§7) |
| 5 | Per-request authority validation | entry decision §7 (D5); S2.3 accept §5 | Yes | WP-S24-1 (+S2.3) | Q2 authority probe; SURFACE-Q1 corpus | Real-authority union restriction (F-01) | Authority + recovery + partial combo (§7) |
| 6 | Scope-level coverage breakdown | entry decision §6 (D6) | Yes | WP-S24-1 | Q2/SURFACE-Q1 coverage probes | Synthetic cross-unit ≠ real-authority union | Partial scope on real facts (§7) |
| 7 | Availability/completeness separation | entry decision §7 (D5); IF-03/REM-01 | Yes | WP-S24-1 | Q2/SURFACE-Q1; IF-05 matrix | — | No |
| 8 | IF-05 exception/status mapping | register IF-05/I-03; boundary §8 "honest coverage and refusal states" | Yes | WP-S24-2 | producer matrix + SURFACE-Q1 13/13 | Q2-P1-01 closed; S1-obs-01 recorded | Authority-bearing mismatch combo (§7) |
| 9 | IF-02 realistic-size ordering/limits | register IF-02; reconciliation IF-02 | Yes | WP-S24-2 | SURFACE-Q1 corpus + generated-real oracles | S3-obs-01 omission | Representative integrated cases (§7) |
| 10 | SQ-07/MD-02 characterization | register SQ-07/MD-02; acceptance-matrix §11 | Yes (evidence) | WP-S24-2 | SURFACE-Q1 measurement; producer harness | No thresholds; S4-obs-01 variance | No |
| 11 | Cross-target behavior | IF-04 "optional cross-target mode"; G-ACCEPT §18 "partial/cross-target reader surface"; entry decision §8 (D7) | **Optional** (deferred) | WP-S24-3 (optional) | — | Deferred by owner decision | See §4/§5 |
| 12 | Internal vs public API boundary | entry decision §9 (D8); boundary §10/§11; MD-03; ADR-CI1-02 | Yes (exclusion) | WP-S24-1/2 | SURFACE-Q1 S9; snapshots | — | No |

A requirement is marked fulfilled only where an **independent** qualification (or an accepted owner decision on independently qualified evidence) exists.

## 3. WP-S24-1/2 acceptance and evidence inventory

- **WP-S24-1** (`de92eeb`; R1 `39cb0ef`; Q1 `04ab118` REMEDIATION REQUIRED; Q2 `f37efd7` SUPPORTED WITH QUALIFICATIONS; owner `c0ec72e` ACCEPTED WITH QUALIFICATIONS): internal exact-target continuation, canonical/stateless tokens, publication binding, per-request authority, strictly-after ordering, malformed-token refusal, coverage breakdown, bounded execution (target-local), legacy compatibility, D3 noninterference.
- **WP-S24-2** (`b7207f3`; SURFACE-Q1 `495f54f` SUPPORTED WITH QUALIFICATIONS; owner `e93ee04` ACCEPTED WITH QUALIFICATIONS): IF-05 mapping 13/13 (incl. Q2-P1-01 closed), IF-02 authentic + generated-real ordering, SQ-07/MD-02 characterization, coverage/provenance, publication/recovery.
- **Regression floors:** full solution **907/0/0**; focused S24Surface 3/0; WP-S24-1 subset 70/0; frozen R2 oracle **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`; Release 0/0; format exit 0.

## 4. Cross-target obligation analysis

**Three distinct concepts (not to be conflated):**

1. **Exact-target queries with occurrences contributed by multiple storage units** — supported, qualified, and required (WP-S24-1 contributor union; SURFACE-Q1 coverage). This is *not* cross-target.
2. **Queries enumerating occurrences for multiple target identities** (a.k.a. cross-target mode) — described as an **optional** mode in IF-04 and deferred by the S2.4 entry owner decision §8 (D7) to a separately authorized WP-S24-3; tokens reserve `mode` for forward compatibility; not implemented.
3. **Public Find Usages search across a codebase** — excluded from S2.4/Stage 2 (boundary §10/§11; MD-03); requires the separate ADR-CI1-02 owner decision.

**Normative references examined:** boundary §8 (S2.4 gate: pagination, coverage, ordering/limits, honest refusal; **no cross-target requirement**); boundary §10 (internal reader may be extended; public excluded); boundary §11 (exclusions); Stage-2 acceptance matrix rows 7–8 (Incomplete/complete-empty; **no cross-target row or gate**; §16 gate summary contains no cross-target gate); S1 reconciliation IF-04 ("pagination token, **optional cross-target mode**, coverage reporting"); G-ACCEPT §18 ("publication-change handling; partial/cross-target reader surface"); register IF-04 ("**applicable** cross-target request"); S2.4 entry decision §8 (D7: cross-target **deferred to optional WP-S24-3**, forward-compat constraints fixed); S2.3 acceptance/register (SQ-03 etc.); ROADMAP CI-1 and ADR-CI1-02 (public Find Usages is a separate decision).

**Reading.** The only source that could be read as requiring a "cross-target reader surface" is G-ACCEPT §18; IF-04 qualifies it as *optional* and the register as *applicable*, and the later, more specific S2.4 entry owner decision §8 explicitly **adopted the deferral** and fixed forward-compatibility constraints. The S2.4 gate and the acceptance matrix never require multi-target querying. There is therefore **no unresolved normative conflict**: the controlling owner decision defers WP-S24-3.

## 5. WP-S24-3 necessity determination

**WP-S24-3 is not a prerequisite for S2.4 closure or S2.4 owner acceptance.** Multi-target querying is optional under IF-04, absent from the boundary §8 S2.4 gate and the acceptance matrix, excluded from WP-S24-1's accepted scope by the owner decision that deferred it, and distinct from both multi-unit exact-target contributor aggregation (already qualified) and public Find Usages (a separate ADR-CI1-02 decision). WP-S24-3 **remains optional and unauthorized, and is not decided unnecessary**: the final S2.4 acceptance should record the deferral explicitly (forward-compatibility constraints preserved: versioned tokens, `mode` reserved, per-target ordering unchanged, no unbounded fan-out if later authorized).

## 6. Integrated qualification coverage assessment (A3)

| Interaction | Tested together? | Evidence |
| --- | --- | --- |
| Continuation over real published occurrence data | Yes — real plugin + verified authority + pagination | WP-S24-1 `I01/I02`; S23-PUB; SURFACE-Q1 corpus |
| Publication authority during pagination | Yes (per-page authority; refusal without authority) | WP-S24-1 `I09`; Q2 authority recheck; SURFACE-Q1 corpus |
| Recovery between continuation requests | Yes (real-plugin publication) | WP-S24-1 `I02`; R1/Q2; SURFACE-Q1 publication (legacy facts) |
| Source replacement + stale continuation refusal | Yes (plugin-path replacement; superseded token refused) | WP-S24-1 `I03_I07`; SURFACE-Q1 publication (legacy facts) |
| Scope-level coverage under incomplete analysis | Yes (constructed partial + pagination) | WP-S24-1 `L13`/`PartialCoverage_*`; SURFACE-Q1 coverage |
| Contributor provenance (incl. cross-unit dedup) | Yes (constructed union; real distinct units) | WP-S24-1 `L04*`; SURFACE-Q1 coverage |
| IF-05 mapping across open and query | Yes (native, legacy + tamper) | WP-S24-2 test; SURFACE-Q1 mapping |
| Real-source ordering and limits | Yes (frozen corpus; generated-real) | SURFACE-Q1 corpus/genreal |
| Boundary conditions + malformed token refusal | Yes | WP-S24-1/Q2; SURFACE-Q1 |
| D3 noninterference | Yes | WP-S24-1 `L16`; Q2/SURFACE-Q1 noninterference |
| Resource characterization + disclosed scaling limitation | Yes (characterization only) | SURFACE-Q1 measurement; S3-obs-01 |

**Isolation gaps (material cross-package interactions not yet jointly exercised):** (a) real-corpus pagination under **incomplete (partial) coverage**; (b) **recovery-between-pages on a real-authority (plugin) publication** combined with **stale continuation after the frozen `source-changed` replacement variant**; (c) the **open-time page/publication mismatch on an authority-bearing publication** (S1 was exercised on the legacy path); (d) a **realistic-size generated-real traversal with per-page authority revalidation and coverage breakdown together**. Predecessor qualifications are not equated with integrated qualification; these four combos are the bounded residual integration surface.

## 7. Remaining integration qualification matrix (A4 draft)

Proposed `S24-FINAL-Q1` (bounded; reuse verified evidence, fresh only for the four combos):

| # | Integration case | Fresh/reuse | Evidence required |
| --- | --- | --- | --- |
| F1 | Real-corpus (frozen) pagination under a **partial** contributing unit; worst-wins `IncompleteAnalysis` on every page; scope-level breakdown stable | Fresh (bounded) | Reviewer probe + manifest |
| F2 | Real-authority (plugin) publication: **recovery between pages** and **stale continuation** after the frozen `source-changed` variant; no blend/restart | Fresh | Reviewer probe (fresh processes) |
| F3 | **Open-time page/publication mismatch on an authority-bearing publication** → `CodeOccurrenceIndexException` (extends S1 to the authority path) | Fresh | Reviewer probe |
| F4 | Generated-real cardinality 1000: traversal with **per-page authority revalidation + coverage breakdown**; oracle-exact ordering | Fresh | Reviewer probe |
| F5 | Regression floors: full solution green; frozen R2 oracle byte-identical; format/build; manifests; D3 noninterference | Fresh re-run | Logs + manifests |

**Required:** independent reviewer ≠ implementer; fresh OS processes; expected values from accepted contracts/Roslyn facts; self-excluding evidence manifest; historical evidence byte-exact. **Blocking:** any ordering/omission, authority/refusal, coverage, or integrity violation. **Disposition:** `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED`. **Then:** a separate `S2.4-ACCEPT-OWNER` decision.

## 8. Binding qualifications and deferred obligations (A5)

**Binding (preserved):** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer independence limitation; Windows-x64 and serialized single-writer; managed fault-injection durability limits; real-authority union restriction; **F-OBS-Q1-01** (target-local `O(n log n)` per page); **F-OBS-Q1-03** (unkeyed token checksum); **Q2-OBS-01**; **S1-obs-01**, **S3-obs-01**, **S4-obs-01**.

**Deferred beyond S2.4 (not discharged; do not block S2.4 acceptance):** SQ-04 authentic historical fixtures; SQ-05 expanded Linux native qualification; SQ-06 remainder (independent-review/owner step + Stage-2 placement); S-01 integrated proof; D4.5; full S-02; D3-full; residual IF-02/IF-05/SQ-07 work. **Blocking S2.4 acceptance:** only the `S24-FINAL-Q1` integration cases of §7 (none currently demonstrated as violated).

## 9. Architecture and performance boundary assessment (A6)

- **Target-local bound satisfied:** SURFACE-Q1 measurements confirm `Examined` = target cardinality (100/1000/10000) and per-page work bounded by the target candidate set; no repository-global scan; no unbounded materialization. F-OBS-Q1-01 remains an observation, **not** a contractual violation; the realistic-size evidence does not identify any violated requirement.
- **No new threshold** is introduced; no additional performance qualification is required beyond the §7 F4 combination.
- **Optimization remains deferred** and is not a prerequisite.
- **Boundaries preserved:** existing token format; no persisted continuation sessions; no storage migration; no index redesign; no dual-write; no new public API; no public Find Usages authorization.

## 10. Closure disposition and rationale (A7)

**`CLOSURE QUALIFICATION READY`.**

All mandatory S2.4 capability requirements (items 1–10 and 12 of §2) are met by owner-accepted packages on independently qualified evidence; the optional cross-target mode (item 11) is authoritatively deferred by the S2.4 entry owner decision §8, and the S2.4 boundary gate/acceptance matrix do not require multi-target querying. No material prerequisite is missing, no evidence-integrity problem exists (all five manifests 0 mismatches), and no unresolved blocking defect is recorded. Four bounded cross-package integration combinations (§7 F1–F4) remain for the final integrated qualification; they are integration completeness items, not demonstrated violations.

## 11. Proposed next commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.4-S24-FINAL-Q1` — S2.4 final integration qualification (reviewer ≠ implementer), executing §7 F1–F5, producing an independent report and a self-excluding evidence manifest, ending in `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED`. **Then** a separate `S2.4-ACCEPT-OWNER` decision that records the accepted S2.4 scope, the preserved qualifications, the explicit WP-S24-3 deferral (optional/unauthorized), and the deferred S2.5/Stage-2 obligations.

**Not authorized:** this commission; `S24-FINAL-Q1` execution; S2.4 owner acceptance; WP-S24-3 implementation/authorization; S2.5; Stage 6; public Find Usages; optimization; any production or test change.

---

*Additive assessment only. No production code, test, corpus/oracle, historical evidence, prior decision, or planning contract was modified.*
