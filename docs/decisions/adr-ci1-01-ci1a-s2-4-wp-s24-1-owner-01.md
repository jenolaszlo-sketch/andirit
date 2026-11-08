# ADR-CI1-01-CI1A-S2.4-WP-S24-1-OWNER-01 — Qualified owner acceptance of WP-S24-1 (internal occurrence continuation + coverage)

**Commission:** `ANDIRIT-CI1A-S2.4-WP-S24-1-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Owner decision:** **`WP-S24-1 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`WP-S24-1` (internal exact-target occurrence continuation and scope-level coverage breakdown) ACCEPTED within the qualified scope below, after the `S24-READER-Q1`/R1/Q2 qualification chain.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> This decision accepts the bounded WP-S24-1 implementation (`de92eeb`), the R1
> remediation of the blocking Q1 finding (`39cb0ef`), and the independent Q2
> requalification (`f37efd7`, `SUPPORTED WITH QUALIFICATIONS`). It authorizes no
> successor work: WP-S24-2, WP-S24-3, S2.4 as a whole, S2.5, Stage 6, public Find
> Usages, and pagination optimization remain unauthorized.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Verified preflight and Git ancestry

| Object | Value |
| --- | --- |
| Entry HEAD | `f37efd7cf2e9051cfbbe36a35957b8d63dd4f5b3` |
| Entry tree | `2ddb35210245a0fd7c32d7829638c18d96f684ce` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

The reviewed chain is consecutive and exact (verified from Git objects):

| Stage | Commit | Tree |
| --- | --- | --- |
| S2.4 entry/contract owner decision | `9726c3aeb101bd039ca3d8fe921a9910e1878575` | `5546c747ccec6638de174cc3b60b31931ae66beb` |
| WP-S24-1 implementation | `de92eeb95463b42914b483534c55a0fa2b0e828b` | `bb1b4157360abaeda66e72cd4811fc9898d19f25` |
| S24-READER-Q1 independent qualification (`REMEDIATION REQUIRED`) | `04ab118f53d905bad6d8b25acc6fafc6889e2447` | `960dfc9c2dad08254c46018f664dbf6916086d8b` |
| WP-S24-1-R1 remediation | `39cb0ef18ee787c5c75410bbfa8b58f2f2305bd4` | `60c750c19f80a7b35471de2930e1c4632b798f72` |
| S24-READER-Q2 independent requalification (`SUPPORTED WITH QUALIFICATIONS`) | `f37efd7cf2e9051cfbbe36a35957b8d63dd4f5b3` | `2ddb35210245a0fd7c32d7829638c18d96f684ce` |

## 2. Qualification-chain reconstruction

- **Authorization.** WP-S24-1 was authorized by `adr-ci1-01-ci1a-s2-4-entry-owner-01.md` and the [work-package plan](../planning/adr-ci1-01-ci1a-s2-4/work-package-plan.md) (internal reader/query/result contract only; no public Find Usages; no persistence/migration; no cross-target).
- **Implementation** (`de92eeb`): internal `CodeOccurrenceContinuationState`, `CodeOccurrenceUnitCoverage`, query `ContinuationToken`, result `NextContinuationToken`/`Continuation`/`CoverageBreakdown`, tightened `IsComplete`, the `CodeOccurrenceContinuation` token codec, and the reader continuation/coverage path — plus 15 focused tests.
- **Q1** (`04ab118`): independent reviewer probe found the **blocking** F-Q1-01 (duplicate-key token propagated an unhandled `ArgumentException` instead of `Invalid`) and the non-blocking F-OBS-Q1-01/Q1-02/Q1-03; disposition `REMEDIATION REQUIRED`.
- **R1** (`39cb0ef`): authorized, bounded remediation — a targeted malformed-token exception boundary, a canonical base64url round-trip check (D1-required), a post-publication-binding cursor well-formedness refusal, and one focused regression test. No other production behavior changed.
- **Q2** (`f37efd7`): independent requalification with fresh reviewer-owned probes; **`SUPPORTED WITH QUALIFICATIONS`**; F-Q1-01 and F-OBS-Q1-02 resolved; no new blocking finding.

## 3. Accepted capability boundary

Within the qualified scope, the owner accepts:

1. **Internal exact-target continuation** on `ICodeOccurrenceReader`/`CodeOccurrenceQuery`/`CodeOccurrenceQueryResult` (no public Find Usages API).
2. **Stateless, versioned, canonical, non-persisted tokens** bound to repository, exact target, mode, publication `(run, id)`, ordering direction and page size; no server-side session, no new storage row/label, no schema change.
3. **Publication-bound continuation**: a superseded/retired publication refuses (`StalePublication`); no silent restart, no cross-publication blend.
4. **Per-request authority revalidation**: the accepted authority gate runs on every continuation request; missing authority refuses (`AnalysisUnavailable`).
5. **Strictly-after deterministic pagination**: the accepted total order (`path`, start line/column, end line/column, occurrence id) with no duplicates and no omissions within a stable publication.
6. **Correct refusal of malformed continuation**: duplicate keys, missing fields, wrong types, unexpected fields, nested/array values, malformed JSON, unsupported versions, invalid checksums, oversized input, invalid and noncanonical base64url, and out-of-range/invalid cursors all return `Continuation = Invalid` without propagating parser/decoder exceptions.
7. **Stale-publication handling** and the normative precedence between `Invalid`, `StalePublication` and `AnalysisUnavailable`.
8. **Scope-level coverage breakdown**: per-unit contributions (complete/incomplete/unavailable/refused) with worst-wins aggregation, stable across pages, distinct from availability and not inferred from the current page.
9. **Explicit availability, continuation and completeness semantics**: `IsComplete` is true only on the final page of a complete-scope query; intermediate/invalid pages are never complete.
10. **Page-size and target-local execution bounds**: `DefaultPageSize = 100`, `AbsoluteMaxPageSize = 1000`; `Examined` remains the honest target-page candidate count; no repository-global scan.
11. **Legacy reader compatibility** (no-token queries unchanged) and **D3 graph noninterference** (no graph or persisted-row changes from pagination).

No cross-target behavior, public Find Usages API, persistence, migration, or dual-write was introduced.

## 4. Evidence and manifest verification

The owner re-verified the committed artifacts rather than relying on report summaries:

| Evidence | Result |
| --- | --- |
| Q2 evidence manifest | **41 entries, 0 mismatches** |
| R1 evidence manifest | 19 entries, 0 mismatches |
| Q1 evidence manifest | Git blob `29f6ca6b70b70b5fc96e196e380c965ced386099` at **both** `04ab118` and HEAD — byte-identical; Q1 evidence changed files: **0** |
| Q2 probe matrix | **14 phases, 0 failed** |
| Independent malformed-token cases | **40 cases, 0 failures** (all `Invalid`, no exception) |
| Q1 reviewer token matrix re-executed fresh | **0 failures** |
| Pagination oracle / authority / coverage / publication probes | pass (Q2 fresh + Q1-reuse re-executed) |
| Full solution | **904 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 351) |
| Frozen R2 oracle | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Release build / formatting | 0 warnings / 0 errors; `dotnet format --verify-no-changes` exit 0 |
| Production/test change since R1 | **0** |

**Provenance distinction.** Fresh Q2 independent evidence (`Q2-P0*`, `Q2-reuse-*` re-execution in fresh processes) is distinguished from reused Q1 reviewer tooling/evidence and from producer implementation tests (`S24ReaderContinuationTests`, 16 tests). Overlapping executions are regression floors, not independent assurance.

## 5. Finding dispositions

| Finding | Disposition |
| --- | --- |
| **F-Q1-01** — duplicate-key token propagated an exception instead of `Invalid` | **RESOLVED** (R1; independently confirmed by Q2's 40-case adversarial matrix and the re-executed Q1 token matrix) |
| **F-OBS-Q1-02** — noncanonical base64url accepted | **RESOLVED** (R1 canonical round-trip check; Q2 canonical variants refused) |
| **F-OBS-Q1-01** — per-page full-target sorting `O(n log n)` | **PRESERVED — non-blocking performance observation**; target-local, within the accepted D4 boundary; `Examined` retains its accepted meaning; not a D4 violation and **D4 is not reopened** |
| **F-OBS-Q1-03** — unkeyed checksum | **PRESERVED — unkeyed-checksum limitation**; corruption detection only, **not** cryptographic authentication; a checksum-consistent forged cursor can skip within one publication and cannot bypass trusted-state authority validation |
| **Q2-OBS-01** — canonical alphabet sub-cases not applicable for ASCII envelope tokens | **PRESERVED as documented qualification context** (test-construction note; equivalent noncanonical variants exercised) |

## 6. Binding qualifications (retained, not resolved)

Carried forward without reinterpretation: **F-01** (restricted real-authority cross-unit union), **F-02** (managed fault injection ≠ crash/power-loss durability), **F-03** (authentic SQ-04 historical fixtures unavailable), **Q1-Q1**, **Q1-Q2**, **S-01** (caller-supplied component digests), **Q2-Q01** (concurrent interrupted-run stale baseline), **Q2-Q02** (failure-message text), **O-Q2-01** (present-owner marker hardening candidate), **Q-I01** (fixture minima vs invocation-fact scope), **Q-I08** (D3 reuse). Additionally preserved: the **same-harness reviewer independence limitation**, the **Windows-x64 execution boundary**, the **serialized single-writer model**, the **managed fault-injection durability limitation**, the **real-authority union restriction**, the **unauthenticated continuation tokens** (F-OBS-Q1-03), and the **target-local sorting observation** (F-OBS-Q1-01).

No constant-time, index-backed, or cryptographic-continuation guarantee is accepted; the observed implementation performs target-local sorting with `O(n log n)` work per page.

## 7. Deferred S2.5 / Stage-2 obligations (not discharged)

**SQ-04** authentic historical fixtures; **SQ-05** expanded Linux native qualification; **SQ-06** remainder (independent-review/owner step + Stage-2 placement); **S-01** integrated proof; **D4.5** cancelled staging; **full S-02**; **D3-full** real-project corpus; **IF-02/IF-05 residuals**; **SQ-07** cost measurement. WP-S24-1 acceptance discharges none of these; cross-target remains deferred.

## 8. Formal owner decision

**`WP-S24-1 ACCEPTED WITH QUALIFICATIONS`.**

The bounded, tests-and-internal-reader-only WP-S24-1 functionality is accepted on the independently qualified evidence above, restricted to the internal exact-target continuation and scope-level coverage breakdown, subject to every qualification in §6 and every deferred obligation in §7. No material contradiction was found between the committed evidence and the Q2 handoff; the blocking F-Q1-01 and the non-blocking F-OBS-Q1-02 are resolved; no production or test change occurred after R1.

## 9. Successor authorization status

- **WP-S24-1 is closed** within its accepted scope only.
- **WP-S24-2** (refusal mapping, real-size mapping, cost measurement) remains **planned but unauthorized**; a separate WP-S24-2 entry/authorization decision is required.
- **WP-S24-3** (bounded cross-target continuation) remains **optional and unauthorized**.
- **S2.4 as a whole is not accepted**; S2.5 and Stage 6 remain **unauthorized**.
- **Public Find Usages remains unauthorized** (ADR-CI1-02 is a separate owner decision).
- **Pagination optimization is not authorized.**
- No new public API, persisted format, migration, or dual-write is authorized.

## 10. Repository discipline

Only this decision document and the additive WP-S24-1 status note to the S2.4 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, prior decision, or S2.3 contract is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.
