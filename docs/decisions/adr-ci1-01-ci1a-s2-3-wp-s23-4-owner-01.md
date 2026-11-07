# ADR-CI1-01-CI1A-S2.3-WP-S23-4-OWNER-01 — Qualified owner acceptance of the WP-S23-4 native lifecycle qualification

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Owner decision:** **`WP-S23-4 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`S23-LIFE` / WP-S23-4 (native lifecycle qualification with real occurrence facts) ACCEPTED within the qualified scope below, on top of the accepted `S23-LIFE-Q1` independent qualification.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> This decision accepts the bounded WP-S23-4 tests-and-evidence package (`0dca67b`)
> and its independent qualification (`S23-LIFE-Q1`, report/evidence `e4b64eb`, with
> additive hash/normalization commits `b32cb32` and `5f1ae63`) at reviewed source
> `f0d960a`. It accepts no production change (none was made), and it does **not**
> authorize WP-S23-5, S2.4/S2.5, Stage 6, or public Find Usages.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Authority, exact baseline, and ancestry

Controlling authority: the [WP-S23-4 entry owner decision](adr-ci1-01-ci1a-s2-3-wp-s23-4-entry-owner-01.md), the [S2.3 work-package plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md) (WP-S23-4/WP-S23-5 rows), the [native publication / lifecycle test matrix](../planning/adr-ci1-01-ci1a-s2-3/native-lifecycle-test-matrix.md) (L1–L20, D4 mapping), the [S2.3 independent-qualification plan](../planning/adr-ci1-01-ci1a-s2-3/independent-qualification-plan.md), [Stage 2 boundary §8](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md), and the accepted owner decisions [NEW-01](adr-ci1-01-ci1a-s2-3-new-01-owner-01.md), [WP-S23-2](adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md), and [WP-S23-3A](adr-ci1-01-ci1a-s2-3-wp-s23-3a-owner-01.md). The independent review considered is [S23-LIFE-Q1](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1.md).

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| WP-S23-4 entry baseline (reviewed source) | `f0d960af6ec1ffec145a6416d74ef06384b1bafd` | `a72f05c69ba722b3600b3d40c59c7adc11c6561b` |
| **WP-S23-4 qualification tests (accepted)** | **`0dca67bf54b5499fce4e70217730ed388e3cbd4a`** | **`690a6ae59709649325605f734555802a962f0ce4`** |
| **S23-LIFE-Q1 report + evidence + probe harness (accepted)** | **`e4b64ebd6892a6ff6892f84840e907303f0de01e`** | **`8d8f134aa7c3f423be78003a4f0ab13f1dc2bc4e`** |
| S23-LIFE-Q1 exact-hash record (additive) | `b32cb323deb2c1961ecfb3e67bb5d0e546282b2a` | `ac59f4e3800ba47e8922c5857f7e5f7ddbfbe9c5` |
| S23-LIFE-Q1 evidence UTF-8 normalization (additive) | `5f1ae633790a4a7cd7a3c7f386e39084ee966e9d` | `97966e37b82dc9eba9190626aed5c81b013d9f29` |

The reviewed chain is consecutive and exact: `f0d960a` → `0dca67b` → `e4b64eb` → `b32cb32` → `5f1ae63`. The owner began on clean canonical `main` at `5f1ae63`, one worktree (`D:/projects/Andirit`), no stashes, nothing pushed. No fetch, branch, additional worktree, reset/restore, rebase, prune, or publication was performed.

## 2. Change scope and evidence integrity

`git diff --name-status f0d960a 5f1ae63` adds exactly: the qualification test file (`tests/Andirit.CSharp.Tests/S23LifecycleQualificationTests.cs`), the reviewer probe harness (`docs/reviews/CI1A-S2.3-S23-LIFE-Q1-tests/`), the evidence directory (`docs/reviews/CI1A-S2.3-S23-LIFE-Q1-evidence/`), and the Q1 report. **No `src/` file changed** (`git diff f0d960a 5f1ae63 -- src` is empty: 0 lines), no existing test in any project changed, and no historical review, evidence, oracle, corpus, or prior decision was modified. This matches the entry decision's authorization (tests and evidence only).

Evidence integrity was re-verified by the owner from the committed artifacts:

| Evidence | Result |
| --- | --- |
| `CI1A-S2.3-S23-LIFE-Q1-evidence/manifest.json` | **32 entries; 0 missing; 0 byte/hash mismatches** (self-excluded, SHA-256 over exact bytes) |
| Manifest oracle entry `oracle-r2.json` | **146,851 bytes**; SHA-256 **`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`** |
| `reviewed-head.txt` | reviewed HEAD `f0d960a`, tree `a72f05c6…` |
| `production-diff.txt` | 0 production lines |
| `probe-matrix.txt` / `P01`–`P08` | 8/8 native probe phases `ok=true`, each a distinct OS process with recorded `processId`, Windows `10.0.26200` **x64**, `.NET 10.0.12` |

The Q1 report and its probes were treated as committed artifacts; the full suite was **not** re-run solely to record acceptance.

## 3. Accepted lifecycle and native guarantees

Within the qualified scope, the owner accepts the following, evidenced by the independent `S23-LIFE-Q1` results (18 new tests, 8 native probe phases) and the owner's re-check of the committed artifacts:

1. **Real-fact publication through the accepted pipeline.** Real S2.2 extraction via the C# plugin / ingestion sink publishes through `StageIndexUnitAsync → CompleteIndexRunAsync` into the real native store; unit/page/meta rows carry the authority envelope (L1, P01/P02).
2. **Correct identities.** Real cross-project `asmctx3` targets are recomputed from the frozen R2 authority and identity primitives and match the persisted facts; the persisted `DefiningProjectUnitId`/`Compctx2` bind each fact to its verified defining compilation (L18, P01/P02).
3. **Durable reopen.** A fresh process reconstructs the same ordered real-fact projection (L7, P02/P05); byte-equivalent ordered results.
4. **Replacement / retirement / complete-empty.** Same-unit replacement after a real source edit retires obsolete facts and exposes the new ones; scoped deletion removes only the deleted unit; a `Produced` zero-fact unit persists and reopens as a complete-empty answer (L2/L3/L6/L11, P05).
5. **Availability, coverage and refusal.** `Available`/`IncompleteAnalysis`/`AnalysisUnavailable` are distinguished and persisted; deleting the last analyzed unit invalidates coverage to `AnalysisUnavailable`; missing current authority is unavailable (never empty-success); a stale/foreign publication handle is `StalePublication` with 0 hits and 0 examined (L8/L13/L14, P03/P04).
6. **D3 noninterference.** Graph nodes/edges/counts/traversal are unchanged across the real-fact occurrence lifecycle and after reopen (L16).
7. **Corruption and conflict.** Real-identity conflicts are rejected at staging with byte-identical prior state; schema/namespace and recorded-context controls remain refused (L5, L15/L19 reuse).
8. **Recovery and unknown outcome (managed).** Managed before-commit failure preserves the prior publication and the recovered projection equals a fresh reopen; managed after-commit ack loss quarantines and a fresh reopen reconciles to the durable publication (L9/L10/L20).
9. **SQ-03 native schedule.** Under the serialized `_writeGate` model a reader observes a complete old-or-new publication, never a partial blend: the deterministic schedule trace is exact (P06), and 4 concurrent readers over 800 samples observed **0** violations (P07).
10. **Surface preservation.** No production source, public API, persisted schema/format, namespace, or Option A identity separation changed.

## 4. Mandatory qualifications

- **F-01 — Cross-unit union evidence shape (binding).** A naturally occurring, fully real-authority identical-fact cross-unit union was **not** demonstrated: real verified provenance binds `SourceUnitId` to the analysis unit and the C# plugin maps one project to exactly one storage/analysis unit, so one publication cannot carry the same authority-validated identical fact under two storage units (`src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs`, `src/Andirit/CodeGraphBatchValidator.cs`). The union algebra is instead evidenced by `L04_*` (a real extracted identity — real target/span/sourceHash/semantic unit — contributed by two storage units ⇒ 1 fact, 2 contributors) combined with the accepted synthetic-contribution union and distinct-semantic-unit evidence (`N4_*`, `L04b_*`). The owner accepts this combination because the accepted L4 requirement is a storage-level identity merge, which `L04_*` exercises on real identity values, and because the unmatched artifact (a naturally occurring two-project identical fact) is architecturally precluded by the accepted design rather than a behavioral gap. **No unrestricted real-authority cross-unit union is accepted.** If a future acceptance requires a naturally occurring two-project identical-fact union, it is a producer/plugin concern outside WP-S23-4's tests-only scope and requires its own authorization.
- **F-02 — Fault injection (binding).** L9/L10 rely on the store's managed `before-commit` injector and L20 on the managed `after-commit` injector. No authentic crash, power-loss, abrupt-kill, arbitrary-crash-point, or multi-process-writer behavior is established. Process restart is not crash consistency.
- **F-03 — Historical SQ-04 fixtures (binding).** Authentic SQ-04 historical prior-writer fixtures remain unavailable and were not fabricated. The S2.5/Stage-2 historical-fixture obligation is preserved, not silently closed. The available schema-refusal controls (L15) and recorded-context refusal controls (L19) are deterministic controls, reused unchanged and applicable to the current (unchanged) production source per the entry decision §4.
- **Carried qualifications preserved (binding, not resolved).** Q1-Q1 (segment resumption; closed fail-closed by WP-S23-3A); Q1-Q2 (recovery-rebuild internal-failure atomicity bound); S-01 (caller-supplied component digests, S2.5); Q2-Q01 (concurrent interrupted-run stale-baseline — the binding concurrent qualification; `P07`/`L12b_*` do **not** supersede it); Q2-Q02 (failure-message text); O-Q2-01 (separate, unstarted hardening candidate). The passing Q1 suite does not reinterpret these.
- **Disclosed same-harness reviewer qualification (binding).** `S23-LIFE-Q1` was executed in the same agent harness as prior commissions, as a distinct commissioned review task, with a reviewer-owned committed probe harness and expected values derived from the frozen R2 authority rather than producer output. The evidence contains no explicit separate-session/separate-model receipt; independence rests on the separate commission/commit boundary and the frozen-authority-derived expected values, **not** on a session- or model-identity claim. If strict session separation is required, the owner may commission an independent re-run; this acceptance rests on the independently re-verified committed artifacts.
- **Durability and concurrency boundary (binding).** Verification is Windows x64 only, with the accepted SDK/runtime and the serialized single-writer store. No claim of arbitrary crash, power-loss, cross-platform, MVCC/snapshot-isolation, general linearizability, or multi-process-writer behavior. The 800-sample schedule demonstrates serialized-write old-or-new completeness for the demonstrated schedule/interleaving only.
- **No public Find Usages.** No new public Find Usages capability is accepted or implied.
- **No successor authorization.** No authorization is granted for WP-S23-5, S2.4, S2.5, Stage 6, or any other work.
- **Claim provenance.** Only results independently established by `S23-LIFE-Q1` (or, where the entry decision approved reuse, the accepted prior suites) are accepted; carried obligations (S-01, SQ-04, the fully real-authority cross-unit union, O-Q2-01) remain unqualified.

## 5. Verification receipts (from committed review artifacts)

| Gate | Evidence | Result |
| --- | --- | --- |
| New lifecycle tests | `…S23-LIFE-Q1-evidence/lifecycle-tests.log` | 18 passed, 0 failed, 0 skipped |
| Full solution | `…S23-LIFE-Q1-evidence/full-solution-tests.log` | **883** passed, 0 failed, 0 skipped (LatticeDb 50, TreeSitter 157, core 345, CSharp 331) |
| Focused S23 / NEW-01 / Occurrence / WP8 | `…S23-LIFE-Q1-evidence/s23-new01-regression.log` | **189** passed, 0 failed, 0 skipped (166 + 23) |
| Frozen R2 oracle | `…S23-LIFE-Q1-evidence/oracle.log` + `oracle-r2.json` | **185/185**, 0 failed; **146,851 bytes**; SHA-256 **`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`** |
| Native SQ-03 schedule | `…S23-LIFE-Q1-evidence/P06-sq03-schedule.json` | exact complete old-or-new trace |
| Native SQ-03 concurrency | `…S23-LIFE-Q1-evidence/P07-sq03-concurrent.json` | 800 samples, **0** violations |
| Release build | `…S23-LIFE-Q1-evidence/release-build.log` | 0 warnings, 0 errors |
| Formatting | `…S23-LIFE-Q1-evidence/format.log` | exit 0 |
| Evidence manifest | `…S23-LIFE-Q1-evidence/manifest.json` | 32 entries verified (0 mismatches) |

The baseline floor was 865 passed before the change; the post-change 883 equals it plus the 18 new lifecycle tests. The owner verified these receipts and the oracle digest from the committed artifacts rather than from abbreviated values.

## 6. Explicit acceptance boundary

Acceptance is limited to the serialized native store on **Windows x64** with the accepted SDK/runtime, the accepted S2.2 extraction/refusal semantics, the accepted WP-S23-2 carrier, the accepted WP-S23-3A publication path, and the L1–L20 / N1–N8-equivalent lifecycle behavior demonstrated by `S23-LIFE-Q1`. It does not extend to crash consistency, power-loss, arbitrary crash points, cross-platform correctness, MVCC/snapshot isolation, general linearizability, or multiple writers, and it accepts no production change.

## 7. Excluded or unqualified claims

This decision does not accept: WP-S23-5 or any successor package; S2.4/S2.5; Stage 6; public Find Usages; unrestricted real-authority cross-unit contributor union (F-01); authentic historical SQ-04 fixtures (F-03); crash/power-loss/multi-process recovery (F-02); cross-process segment-resumption occurrence reconstruction (Q1-Q1); recovery-rebuild failure atomicity (Q1-Q2); digest canonicalization (S-01); concurrent interrupted-run stale-baseline beyond Q2-Q01; failure-message text as a contract (Q2-Q02); O-Q2-01 remediation; any schema-format migration, dual-write, or legacy reader/adapter; MVCC/snapshot isolation; background/distributed indexing or automatic repair; or package publication. No successor package is authorized by this decision.

## 8. Subsequent work authorization status and next planned entry gate

**WP-S23-4 is closed by this decision** (its authorized tests-and-evidence scope only), on top of the accepted WP-S23-1 (NEW-01), WP-S23-2 (S23-DUR), and WP-S23-3A gates.

**Successor assessment — WP-S23-5 (S2.3 owner acceptance), as actually committed** ([work-package-plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md) §WP-S23-5; [independent-qualification-plan](../planning/adr-ci1-01-ci1a-s2-3/independent-qualification-plan.md) §1):

- **Objective:** documentation/decision only — the S2.3 owner acceptance of the combined S2.3 result.
- **Entry prerequisites:** WP-S23-1 … WP-S23-4 accepted (now satisfied) on clean canonical `main`; a separate commission; a separate final integration qualification with **combined independent reviews** plus the owner decision (`S2.3-ACCEPT-OWNER`).
- **Qualifications WP-S23-5 must preserve:** F-01, F-02, F-03; Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01; the durability/concurrency boundary; and the Windows-x64-only scope.
- **Dependencies on SQ-04 / S2.5 / Stage 2:** the S2.3 package closure is bounded by the S2.5/Stage-2 obligations it does not discharge — the **expanded Linux native suite** (S2.5/Stage-2 acceptance evidence) and **authentic historical SQ-04 fixtures** (not fabricated). WP-S23-5 must record these as open Stage-2 obligations and must not treat S2.3 closure as cross-platform certification or SQ-04 closure.
- **Next gate:** a separate `S23-ACCEPT` (combined independent reviews) followed by the `S2.3-ACCEPT-OWNER` decision.
- **Recommendation:** a separate **WP-S23-5 entry assessment** is appropriate and is **not** authorized by this decision; it requires its own owner commission. It must not begin implementation, S2.4/S2.5, Stage 6, or public Find Usages.

- **Next recommended bounded package:** `WP-S23-5` — S2.3 owner acceptance (its own commission required).
- **Not authorized:** WP-S23-5 implementation or entry authorization; S2.4; S2.5; Stage 6; public Find Usages.

## 9. Repository discipline

Only this decision document and the additive authorization-status note to the S2.3 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, or prior decision is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff, outside this self-containing Git record.
