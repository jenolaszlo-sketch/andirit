# ADR-CI1-01-CI1A-S2.3-WP-S23-3-ENTRY-OWNER-01 — WP-S23-3 scope confirmation and bounded implementation authorization

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3-ENTRY-OWNER-01` (scope confirmation and implementation authorization).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`ENTRY AUTHORIZED WITH QUALIFICATIONS`**.
**Gate:** **WP-S23-3 (`S23-PUB`) entry supportable; first bounded implementation package `WP-S23-3A` authorized.**
**Current authority:** WP-S23-2 accepted with qualifications (F-01 closed; Q1-Q1/Q1-Q2/S-01/O-Q2-01 carried).

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The real-fact publication integration package may begin as the bounded first package
> `WP-S23-3A`. Q1-Q1 must be addressed **within** this package or escalated, not treated
> as already closed. WP-S23-4, WP-S23-5, S2.4/S2.5, Stage 6, public Find Usages, and any
> new public API or storage path remain **NOT authorized**. No successor package is
> authorized by this decision.

This is an additive owner decision and work-package specification. It changes no production code, test, schema, frozen expectation, prior decision, reviewer evidence, or accepted planning contract.

## 1. Verified repository baseline and ancestry

| Object | Value |
| --- | --- |
| HEAD | `80f61774bf7f8205ec1f8fb27d0ac1c69aee4bfe` |
| Tree | `ddacf7d23aa3a9b9140ccd077e7483a9e098777f` |
| Working tree | clean; one worktree (`D:/projects/Andirit`); no stashes |
| Ahead / behind `origin/main` | 31 / 0; nothing pushed |
| Environment | Windows x64; SDK `10.0.401`; runtime `10.0.12` |
| Regression baseline | whole solution **834 / 0 / 0**; frozen R2 oracle **185/185** (`97F6C5E5…`) |

Accepted ancestry, verified from Git objects: `b576acb` (NEW-01 baseline) → `51369b3` (WP-S23-2 implementation) → `d94052d` (WP-S23-2 Q1) → `1bce70e` (WP-S23-2 R1 remediation) → `0bc5091` (R1 handoff) → `0888be9` (WP-S23-2 Q2) → `80f6177` (WP-S23-2 owner acceptance). The predecessor gates `NEW-01` (WP-S23-1) and WP-S23-2 are owner-accepted.

## 2. Prerequisite resolution

| # | Question | Resolution |
| --- | --- | --- |
| 1 | What WP-S23-3 must deliver | Real Roslyn-derived occurrence facts published through the accepted chain `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync`, reusing the S2.2 extractor and the WP-S23-2 authority carrier (Stage 2 §3.8; scope inventory R15). Positive: real facts persisted, cross-project targets correct. Negative: unsupported/ambiguous/stale refuse; no trusted fact from structural ids; no occurrence as graph node/edge. |
| 2 | Is Q1-Q1 a blocking prerequisite or part of the work? | **Part of the authorized work, not a separate blocker.** The WP-S23-2 IMPL-01 §14 and owner acceptance §6 require WP-S23-3 to propagate the internal authority + mandatory marker through the batch/sink handoff and explicitly re-stage/re-verify after a store-lifetime interruption, or to obtain separately authorized segment-durability work. |
| 3 | Does Q1-Q2 impose an entry restriction? | **Non-blocking carried qualification.** WP-S23-3 must preserve the accepted recovery behavior and must not rely on or claim stronger recovery-rebuild atomicity/quarantine than already accepted. |
| 4 | Which S23-DUR guarantees must stay invariant? | All accepted WP-S23-2 guarantees: orphan-owner refusal; mandatory association; F-01 durable-reopen closure; retirement correctness; complete-empty vs unavailable/corrupted distinction; and preserved recovery/stale/conflict/coverage/mirror/D3/D4/D5/frozen identities. The accepted carrier, its version, and the occurrence schema/namespace are unchanged. |
| 5 | Other unsatisfied prerequisite gates? | None blocks entry. S-01 canonicalization is deferred to S2.5 and does not block (entry decision §7). The SQ-03 native schedule test, real-fact N1–N8 adaptation, and historical-fixture (SQ-04) work are WP-S23-4/S2.5 obligations. O-Q2-01 remains an unstarted, separately-authorized hardening candidate. |

**Distinction.** Blocking prerequisites: none outstanding. Deferred qualifications: Q1-Q1 (in-package deliverable), Q1-Q2 (carried, claims-bounded), S-01 (S2.5), O-Q2-01 (separate hardening), SQ-03/SQ-04/N1–N8 (successor packages).

## 3. Authorized first bounded package

**`WP-S23-3A` — real-fact publication wiring, authority propagation, and segment-resumption containment.** Full specification: [`wp-s23-3-publication-integration.md`](../planning/adr-ci1-01-ci1a-s2-3/wp-s23-3-publication-integration.md).

Deliverables: (1) real-fact publication wiring through the existing chain; (2) internal, non-public propagation of the accepted authority envelope + `RequiresOccurrenceAuthority` marker to the staged `CodeIndexUnitReplacement`; (3) Q1-Q1 segment-resumption containment (explicit re-stage/re-verify or fail-closed refusal, with fresh-process evidence); (4) negative controls; (5) producer evidence with real inputs.

**Known design tension (implementation choice).** `Andirit` references `Andirit.Abstractions`; `Andirit.Abstractions` references nothing, so the internal `CodeOccurrenceAuthority` (defined in `Andirit`) cannot be a member of the public `CodeGraphBatch` type in `Andirit.Abstractions`. The authority handoff must therefore use an internal, non-public, non-persisted channel (or an equivalent internal mechanism). Adding a public API or changing the persisted schema/format is a stop condition.

## 4. Required behaviors and acceptance criteria

Executable acceptance criteria are enumerated in the specification §5. In summary, the package must demonstrate with real Roslyn facts and fresh-process evidence that: published facts are `Available` under matching current authority after reopen with correct cross-project targets; direct/transitive-only/options-only defining-context changes refuse after reopen despite stable `asmctx3`; missing/corrupt/unsupported provenance refuses conservatively; complete-empty stays distinct from unavailable and corrupted; Q1-Q1 segment resumption cannot silently serve stale/incomplete authority; NEW-01 recovery, retirement, conflict refusal, quarantine, counters, mirrors, and D3/D4/D5 remain correct; the whole solution stays green; and the frozen R2 oracle remains `185/185` (`97F6C5E5…`) with formatting and Release/Debug builds clean.

Verification obligations: producer evidence plus an independent `S23-PUB-Q1` qualification (reviewer ≠ implementer) and a separate `S23-PUB-OWNER` decision. Frozen formulas, the frozen corpus/oracle, and all historical evidence remain unchanged.

## 5. Explicit acceptance boundary

Limited to the serialized native store on **Windows x64** with the recorded SDK/runtime, the accepted publication lifecycle, the accepted S2.2 extraction/refusal semantics, and the accepted WP-S23-2 carrier. It does not extend to crash consistency, power-loss/abrupt-kill recovery, arbitrary crash points, cross-platform correctness, unrestricted concurrency, or multiple writers.

## 6. Binding qualifications carried forward

- **Q1-Q1 (in-package).** Must be addressed by `WP-S23-3A` (re-stage/re-verify or fail-closed refusal) or escalated; not treated as closed.
- **Q1-Q2 (carried).** No stronger recovery-rebuild atomicity/quarantine claim.
- **S-01 (deferred to S2.5).** Component-digest canonicalization remains caller-supplied.
- **O-Q2-01 (deferred, non-blocking).** The cleared-marker-on-present-owner observation remains a separate, unstarted hardening candidate; this decision does not authorize its remediation.
- **SQ-03 / SQ-04 / real-fact N1–N8.** WP-S23-4 / S2.5 obligations; not closed here.

## 7. Excluded and unqualified claims

Not authorized or claimed: WP-S23-4/WP-S23-5, S2.4/S2.5, Stage 6, public Find Usages, schema-format migration, dual-write, legacy readers/adapters, a new storage/publication path, a new public API, MVCC/snapshot isolation, crash/power-loss/multi-process recovery, background/distributed indexing, automatic repair, O-Q2-01 remediation, broad cross-platform certification, and package publication. No reopening of the WP-S23-2 owner acceptance or of the Q1/R1/Q2 artifacts.

## 8. Subsequent work authorization status

**`WP-S23-3A` (first bounded package) is authorized to begin.** This authorization does **not** extend to any further WP-S23-3 package, to WP-S23-4, or to WP-S23-5; each requires its own commission, independent qualification, and owner decision. This decision begins no implementation and authorizes no successor.

## 9. Repository discipline

Documentation-only: this decision and the work-package specification are the only changes. No production code, test, schema, oracle/corpus, reviewer evidence, or prior decision is modified. Canonical `main`, single worktree, no branch or additional worktree, no push, no package publication. The decision/specification commit and tree identities and final working-tree status are reported in the handoff.
