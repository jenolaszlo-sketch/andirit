# ADR-CI1-01-CI1A-S2.3-ACCEPT-OWNER-01 — Qualified S2.3 owner acceptance

**Commission:** `ANDIRIT-CI1A-S2.3-ACCEPT-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Owner decision:** **`S2.3 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`S2.3-ACCEPT-OWNER` — the bounded S2.3 native publication integration (WP-S23-1 … WP-S23-5) ACCEPTED within the qualified scope below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> S2.3 is accepted as a bounded, stage-level result on Windows x64 under the
> accepted serialized single-writer model. All predecessor qualifications and the
> WP-S23-5 qualifications are retained; the S2.5/Stage-2 obligations remain open.
> This decision authorizes **no** successor package, no public Find Usages, and no
> S2.4/S2.5/Stage 6 work.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Verified entry and exact baselines

| Object | Value |
| --- | --- |
| Entry HEAD | `e1ccaf65c44f8e20109f985ee700be82d181abec` |
| Entry tree | `7a8eca0c02d67488498475331ac5880f16a68e67` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

The owner re-verified the entry HEAD/tree and confirmed the S2.3 chain is present and consecutive. No reset/clean/restore/rebase/prune/worktree/push was performed.

## 2. Prerequisite owner-acceptance matrix

Each package was independently qualified and separately owner-accepted with qualifications; **no package authorizes its successor.**

| Package | Owner decision (commit / tree) | Disposition | Independent gate |
| --- | --- | --- | --- |
| WP-S23-1 (`NEW-01`) | `b576acb004747c89d257d8e61721c17babe6c5a6` / `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` | **ACCEPTED WITH QUALIFICATIONS** (Q1-Q1, Q1-Q2) | `NEW-01-Q1` |
| WP-S23-2 (`S23-DUR`) | `80f61774bf7f8205ec1f8fb27d0ac1c69aee4bfe` / `ddacf7d23aa3a9b9140ccd077e7483a9e098777f` | **ACCEPTED WITH QUALIFICATIONS** (Q1-Q1, Q1-Q2, S-01, O-Q2-01) | `S23-DUR-Q1`, Q2 |
| WP-S23-3A (`S23-PUB`) | `4a775c88de49d0f65be04c79b942b99cb35bfe0f` / `ab236e70d43c0c5d82122904547c5a92ffb05252` | **ACCEPTED WITH QUALIFICATIONS** (Q2-Q01, Q2-Q02) | `S23-PUB-Q1`, Q2 |
| WP-S23-4 (`S23-LIFE`) | entry `f0d960af6ec1ffec145a6416d74ef06384b1bafd` / `a72f05c69ba722b3600b3d40c59c7adc11c6561b`; owner `078011b1c4f5a0d82c9a477b646dde965f3049b9` / `b91cf9249f6c61c5771c58dedef23388c1a8d1c4` | **ACCEPTED WITH QUALIFICATIONS** (F-01, F-02, F-03 + carried) | `S23-LIFE-Q1` |
| **WP-S23-5** | entry `9a8d5236b45d8e7d674126d5e94abaec26b0c80e` / `87b0d907be4bd4313de2b781ea59065d525e89e6`; tests `e2ae83f990347abc372b6e177d74e3ae84911b21` / `22321fd0bbae1436f828cbc6dc4c72edd11c4cc7`; report+evidence `e1ccaf65c44f8e20109f985ee700be82d181abec` / `7a8eca0c02d67488498475331ac5880f16a68e67` | Entry `ENTRY READY WITH QUALIFICATIONS`; qualification `SUPPORTED WITH QUALIFICATIONS` | `S23-WP-S23-5-QUAL-01` |

WP-S23-5 was executed strictly within its tests-and-evidence-only authorization: 5 additive qualification tests, a reviewer probe harness, and evidence; **0 production lines changed** since the entry HEAD.

## 3. WP-S23-5 integrated qualification findings

The final integration qualification ([QUAL-01](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01.md)) executed the full I-01 … I-11 matrix and reported **`SUPPORTED WITH QUALIFICATIONS`**. Owner re-check of the committed receipts (`full-solution-tests.log`, `focused-s23-regression.log`, `oracle.log`, `probe-matrix.txt`, `P01`–`P08`) confirms:

| Integrated claim | Fresh / reused | Verdict |
| --- | --- | --- |
| Genuine C# project extraction and occurrence publication through the accepted `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync` chain | Fresh (I-01) | Supported |
| Verified compilation identity and authority binding (real cross-project `asmctx3`, real defining `compctx2`, authority envelopes) | Fresh (I-01, I-02) | Supported |
| Durable publication and fresh-process reopen | Fresh (I-01, I-03) | Supported |
| Recovery across the WP-S23-1 rebuild × WP-S23-2 carrier × WP-S23-3A authority boundaries | Fresh (I-02) | Supported |
| Replacement, retirement and complete-empty behavior | Fresh (I-03/I-07) | Supported |
| Stale-publication refusal (no hits, `Examined == 0`) | Fresh (I-03/I-07, I-09) | Supported |
| Availability and coverage correctness (no false-available, no false-empty) | Fresh (I-09) | Supported |
| Schema-**version** corruption refusal (existing) and occurrence-**namespace** corruption refusal (SQ-06) | Fresh (I-04) | Supported |
| D3 graph noninterference; occurrence identities never appear as graph nodes/edges | Reuse (L16, N8) + fresh light assertion | Supported (Q-I08) |
| Windows-x64 serialized single-writer execution | Fresh (all) | Supported within boundary |

**SQ-06 / entry finding RQ2 — closed for the S2.3 surface.** The isolated occurrence-**namespace**-mismatch rejection now has a dedicated native regression (`tests/Andirit.LatticeDb.Tests/S23Wp5NamespaceRefusalTests.cs`) plus fresh-process probes `P05`/`P06`: an isolated `meta.Namespace` tamper with a **valid** `SchemaVersion` opens with `CodeOccurrenceSchemaException` (`ExpectedVersion == ActualVersion == CurrentOccurrenceSchemaVersion`; message “…namespace is unsupported.”), distinct from the pre-existing version-mismatch control (`ActualVersion == 99`). Production already enforced this; no production defect exists.

**Cross-package interactions.** Recovery, completed-publication and staged-run reconstruction, authority/carrier propagation, ownership correspondence, replacement/deletion and stale handling together expose **no defect, no stale-available leak, and no partial-visibility leak**. No remediation is required.

## 4. Evidence provenance and integrity verification

- QUAL-01 evidence manifest `docs/reviews/CI1A-S2.3-WP-S23-5-QUAL-01-evidence/manifest.json`: **32 entries; 0 missing; 0 byte/hash mismatches** (self-excluding SHA-256 over exact bytes), re-verified by the owner.
- Frozen R2 oracle: **185/185**, output **146,851 bytes**, SHA-256 **`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`** (byte-identical to the accepted value).
- Regression floor: whole solution **888 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 335); focused S23/NEW-01/Occurrence/WP8 **194 passed / 0 failed**; new qualification tests **5 passed**; Release build 0/0; `dotnet format --verify-no-changes` exit 0.
- Provenance classes are recorded distinctly (fresh native, previously accepted independent, same-harness reviewer, synthetic/constructed, managed fault-injection, authentic production-path, unsupported). The owner decision relies on the committed receipts, not on the reported disposition alone; the full suite was **not** re-run solely to record this decision.
- **Same-harness reviewer independence limitation (binding).** All S2.3 review gates (NEW-01-Q1, WP-S23-2-Q1/Q2, S23-PUB-Q1/Q2, S23-LIFE-Q1, WP-S23-5-ENTRY-01/QUAL-01) were produced in the same agent harness as distinct commissioned tasks; there is no separate-session/separate-model receipt. Independence rests on reviewer-owned committed probes, frozen-R2/identity-derived expected values, and producer/reviewer commit separation. A strict independent re-run may be commissioned if required.

## 5. Exact accepted S2.3 capability boundary

Within the qualified scope, the owner accepts integrated S2.3 native publication integration:

1. Real S2.2 occurrence extraction over genuine verified C# compilations, published through the accepted source-unit lifecycle, with native transactional consistency (same transaction as the graph).
2. Verified compilation identity and authority binding: real cross-project `asmctx3` targets, real defining `compctx2`, and internal authority envelopes preserving owner/plugin/run/compilation identity.
3. Durable publication, native reopen with reconstructable projection, and order-independent completed/staged reconstruction.
4. Known-failure recovery whose recovered projection equals a fresh durable reopen, and unknown-outcome quarantine/reopen.
5. Real-source replacement, unit retirement/deletion, contributor union/conflict refusal, and valid complete-empty analysis.
6. Availability/coverage/refusal correctness: `Available`/`IncompleteAnalysis`/`AnalysisUnavailable`; stale/foreign handles refuse; no false-available or false-empty.
7. Schema-version and occurrence-namespace corruption refusal (D5 additive, no migration).
8. D3 legacy-graph noninterference; applicable D4 invariants; the accepted serialized single-writer model.

**Platform and model:** the serialized native store on **Windows x64** with the accepted SDK/runtime, under the accepted serialized single-writer model (no MVCC).

## 6. Binding qualification register (retained, not resolved)

- **F-01** — no unrestricted naturally occurring fully real-authority cross-unit identical-fact union qualification (accepted union evidence is real-fact identity across storage units plus accepted synthetic `N4`).
- **F-02** — managed fault injection does **not** establish crash/power-loss/abrupt-kill/multi-process durability.
- **F-03** — authentic SQ-04 historical prior-writer fixtures remain unavailable and were not fabricated.
- **Q1-Q1** — no cross-process segment-resumption occurrence reconstruction.
- **Q1-Q2** — no stronger recovery-rebuild failure atomicity.
- **S-01** — component digests remain caller-supplied; integrated proof deferred.
- **Q2-Q01** — concurrent interrupted-run stale-baseline remains binding (not superseded by the SQ-03 schedule).
- **Q2-Q02** — the staged-graph failure-message text is not a contract.
- **O-Q2-01** — present-owner marker-clear hardening candidate; remediation not authorized.
- **Q-I01** — the real-project fixture meets the file/project/namespace/invocation/overload minima; Stage-2 §9 *method-reference* and *unsupported/incomplete-analysis* minima remain coverage categories outside the accepted invocation-fact scope.
- **Q-I08** — integration-level D3 is reused (L16, N8) plus a fresh node/edge non-leak assertion; a fresh multi-project-graph-only D3 lifecycle variant was not separately executed (the plugin couples graph and occurrence per project; the controlled occurrence-only unit model remains the D3 instrument).
- **Same-harness reviewer independence limitation** and the **Windows-x64 / serialized single-writer boundary**.

## 7. Deferred S2.5 / Stage-2 obligations (not discharged by S2.3)

Recorded as open, not closed, and separated from S2.3-blocking defects (there are none):

- **SQ-04** authentic historical fixtures (immutable master + per-instance hashes + disposable copies + refusal), as available formats permit.
- **SQ-05** expanded **Linux** native qualification with logs and locking/WAL/mmap notes.
- **SQ-06** remaining obligations beyond the closed namespace-mismatch test gap — the register item's independent-review/owner step and its Stage-2 acceptance placement.
- **S-01** integrated independent proof chain (source bytes → compilation → binding → occurrence identity).
- **D4.5** cancelled-staging non-exposure; full **S-02** ten-invariant native matrix.
- Broader crash/power-loss and any Stage-6 cross-platform certification remain explicitly out of scope.

These are obligations for S2.5/Stage 2 and Stage 6 as applicable; none blocks the bounded S2.3 acceptance.

## 8. Formal owner disposition

**`S2.3 ACCEPTED WITH QUALIFICATIONS`.**

The bounded S2.3 native publication integration is accepted on the evidence-supported scope above, subject to every qualification in §6 and every deferred obligation in §7. No material contradiction was found between the committed evidence and the WP-S23-5 qualification handoff; the SQ-06 test gap is closed; zero production lines changed during qualification. Acceptance does not extend to crash/power-loss recovery, cross-platform correctness, MVCC/snapshot isolation, multi-process writers, authentic SQ-04 fixtures, or a naturally occurring fully real-authority cross-unit union.

## 9. Successor authorization status

This decision authorizes **no** successor work.

- **S2.4** (reader pagination/coverage), **S2.5** (independent Stage 2 qualification), **Stage 6**, and **public Find Usages** remain **unauthorized**; each requires its own commission.
- No new public API, storage contract, schema-format migration, or dual-write is authorized.
- `WP-S23-5` is **closed** by this decision for its bounded scope only.
- Any further work requires a separate owner authorization.

## 10. Repository discipline

Only this decision document and the additive authorization-status note to the S2.3 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, or prior decision is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff, outside this self-containing Git record.
