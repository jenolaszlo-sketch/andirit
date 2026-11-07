# S2.3 dependency-ordered work-package plan

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
No package authorizes its successor. Each requires its own commission, independent
qualification, and owner decision.

**Ordering constraint (binding):** `NEW-01` must pass its gate **before** any S2.3
native publication or recovery reliance. The durability gate must pass **before**
native publication is declared accepted. This follows the accepted rule *"Before
S2.3 and any production recovery reliance."*

---

## WP-S23-1 — NEW-01 targeted recovery remediation  *(first authorized)*

| Field | Content |
| --- | --- |
| Requirement / authority | Obligation register `NEW-01`; S2.1 boundary §4; S2.2 entry §3.7; S2.2 `G-ACCEPT` §12 |
| Existing surface | `src/Andirit/DurableCommandLog.cs`, `src/Andirit/InMemoryCodeGraphStore.cs`, `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` |
| Allowed changes | Restore occurrence projection/coverage/availability/publication binding on the `ReplayAsync`-based recovery paths; focused regression tests. **No** schema change; **no** merge/identity change; **no** quarantine-policy change |
| Positive tests | Recovery restores graph **and** occurrence projection; recovered availability equals fresh reopen; coverage/complete-empty preserved |
| Negative tests | Deliberate cross-unit conflict still refused after recovery; unknown-outcome quarantine still distinct; no false current authority |
| Acceptance | Defect reproduced before fix; permanent regressions; producer evidence; independent qualification; owner decision |
| Independent gate | `NEW-01-Q1` (separate reviewer) |
| Preserved | D3/D4/D5; frozen formulas; `CodeOccurrenceCoverage.Merge` unchanged |
| Stop | Any required schema-format change or quarantine-policy change → return to owner |
| Dependencies | None (self-contained) |

## WP-S23-2 — Defining-`compctx2` native carrier and reopen enforcement  *(durability gate)*

| Field | Content |
| --- | --- |
| Requirement / authority | Option A §5.1(5)/§5.2(8); mapping §4; S2.2 entry §3.5; S2.2 `G-ACCEPT` §11 |
| Existing surface | `CodeOccurrenceFact`/`CodeOccurrenceCoverage` (consume only), `CodeGraphBatch`, `CodeIndexUnitReplacement`, `LatticeCodeGraphStore` occurrence tables |
| Allowed changes | **Additive** persisted carrier for the recorded defining `compctx2` + universe membership; reopen-time validation; stale refusal. Additive JSON only (D5) |
| Positive tests | Matching current context usable after reopen; recorded association persisted |
| Negative tests | Changed defining `compctx2` (incl. transitive-only) refuses stale facts after reopen even with stable `asmctx3`; missing/corrupt/unsupported provenance refuses; complete-empty vs unavailable preserved |
| Acceptance | Test 8b (document 3) with real facts; independent qualification; owner decision |
| Independent gate | `S23-DUR-Q1` |
| Preserved | `compctx2` never enters `asmctx3`/`usage4`; frozen formulas; D3/D4/D5 |
| Stop / escalation | Cannot persist/enforce after reopen → **Option B** escalation (Option A §5.1(5)) |
| Dependencies | **WP-S23-1** (NEW-01) must have passed |

## WP-S23-3 — Real-fact publication integration

| Field | Content |
| --- | --- |
| Requirement / authority | Stage 2 boundary §3.8; handoff §3; S2.3 scope inventory R15 |
| Existing surface | S2.2 `VerifiedCSharpOccurrenceExtractor`; `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync`; `CSharpCodeGraphPlugin*` |
| Allowed changes | Wire accepted S2.2 facts into the existing publication path; emit `CodeGraphBatch`/`CodeIndexUnitReplacement`; **no** new publication path; **no** formula change |
| Positive tests | Real Roslyn facts persisted through the accepted chain; cross-project targets correct |
| Negative tests | Unsupported/ambiguous/stale refuse; no trusted fact from structural ids; no occurrence as graph node/edge |
| Acceptance | Producer evidence; independent qualification; owner decision |
| Independent gate | `S23-PUB-Q1` |
| Preserved | S2.2 refusal/coverage semantics; WP6 merge semantics; WP7 stale boundary |
| Stop | Any need for a new storage path or public API → return to owner |
| Dependencies | WP-S23-1, WP-S23-2 |

## WP-S23-4 — Native lifecycle qualification with real facts

| Field | Content |
| --- | --- |
| Requirement / authority | Stage 2 boundary §8 gate; document 5 matrix L1–L20; D3/D4/D5 |
| Existing surface | Stage 1 native tests (adapt); `LatticeCodeGraphStore` |
| Allowed changes | Tests + evidence only (no production change expected) |
| Positive/negative tests | L1–L20 (document 5) |
| Acceptance | Real-source replacement/deletion/reindex; contributor behavior; graph/occurrence consistency; availability/coverage; native reopen; D3/D4; native schedule test |
| Independent gate | `S23-LIFE-Q1` |
| Preserved | Historical schema refusal; no MVCC claim; serialized model |
| Stop | D3/D4/D5 weakening → return to owner |
| Dependencies | WP-S23-3 |

## WP-S23-5 — S2.3 owner acceptance

| Field | Content |
| --- | --- |
| Requirement / authority | S2.1 boundary §7; Stage 2 boundary §8 |
| Existing surface | — |
| Allowed changes | Documentation/decision only |
| Acceptance | Separate final integration qualification + explicit owner decision |
| Independent gate | Combined independent reviews + owner |
| Dependencies | WP-S23-1 … WP-S23-4 |

---

## Immediate authorization

Only **WP-S23-1 (NEW-01 remediation)** is authorized by the entry decision.
WP-S23-2/3/4/5 are **not** authorized; each requires a separate commission after
its predecessor's gate passes.

## Authorization status (additive; current state)

This note is additive status only; it changes no package definition, scope,
dependency, or gate above. Package definitions remain the controlling record.

| Package | Status |
| --- | --- |
| WP-S23-1 (NEW-01 remediation) | accepted by owner (`adr-ci1-01-ci1a-s2-3-new-01-owner-01.md`) |
| WP-S23-2 (S23-DUR durability gate) | accepted with qualifications by owner (`adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md`) |
| WP-S23-3 / WP-S23-3A (real-fact publication integration; first bounded package) | **accepted with qualifications by owner (`adr-ci1-01-ci1a-s2-3-wp-s23-3a-owner-01.md`)**, covering `e2b833a`, the R1 remediation `e0cef1c`, and the R2 remediation `a4d8501`, after S23-PUB-Q1 and S23-PUB-Q2 |
| WP-S23-3B | not defined by this plan; no such package exists in the committed plan. Further WP-S23-3 packages, if any, require their own commission |
| WP-S23-4 (native lifecycle qualification with real facts) | **accepted with qualifications by owner (`adr-ci1-01-ci1a-s2-3-wp-s23-4-owner-01.md`)** — tests/evidence package `0dca67b` and S23-LIFE-Q1 report/evidence `e4b64eb` (additive `b32cb32`, `5f1ae63`) at reviewed source `f0d960a`; S23-LIFE-Q1 recorded `SUPPORTED WITH QUALIFICATIONS`; no production change. Entry decision: `adr-ci1-01-ci1a-s2-3-wp-s23-4-entry-owner-01.md` |
| WP-S23-5 (S2.3 owner acceptance) | **closed — `S2.3 ACCEPTED WITH QUALIFICATIONS` by owner (`adr-ci1-01-ci1a-s2-3-accept-owner-01.md`)**. Entry assessment `9a8d523`; final integration qualification tests `e2ae83f` and report/evidence `e1ccaf6` (`S23-WP-S23-5-QUAL-01`, `SUPPORTED WITH QUALIFICATIONS`); no production change. All qualifications and the S2.5/Stage-2 obligations remain binding; no successor is authorized |

**S2.3 stage disposition (additive).** `S2.3 ACCEPTED WITH QUALIFICATIONS` at entry HEAD `e1ccaf6` (`adr-ci1-01-ci1a-s2-3-accept-owner-01.md`). S2.4, S2.5, Stage 6, and public Find Usages remain unauthorized; each requires its own commission. The open S2.5/Stage-2 obligations (SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02) are recorded there and are not discharged by this acceptance.

## Excluded from all packages

Native schema **format** migration; dual-write; legacy readers/adapters; public
Find Usages; background/distributed indexing; automatic repair framework;
incremental page maintenance/native-node-ID optimization; MVCC/snapshot isolation;
crash/power-loss/multi-process recovery; reader continuation (S2.4); integrated
Stage-2 proof (S2.5); package publication.
