# ADR-CI1-01-CI1A-S2.4-ENTRY-OWNER-01 — S2.4 entry authorization and internal reader continuation contract

**Commission:** `ANDIRIT-CI1A-S2.4-ENTRY-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-08. **Canonical branch:** `main`.
**Owner decision:** **`S2.4 ENTRY AUTHORIZED WITH QUALIFICATIONS`**.
**Gate:** **the S2.4 planning boundary is authorized; it fixes the internal bounded reader/coverage continuation contract and the S2.4 work-package decomposition.** `WP-S24-1` implementation requires its own explicit commission.

> **Status: RECORDED — OWNER-AUTHORIZED (planning boundary only).**
>
> This decision resolves the blocking design questions M-1 … M-7 from the
> [S2.4 entry assessment](../reviews/ANDIRIT-CI1A-S2.4-ENTRY-ASSESS-01.md), fixes the
> normative internal reader continuation contract, and adopts the S2.4 work-package
> decomposition ([work-package plan](../planning/adr-ci1-01-ci1a-s2-4/work-package-plan.md)).
> It implements nothing, changes no production/test/schema, authorizes no public API,
> and authorizes no successor stage.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified entry

| Object | Value |
| --- | --- |
| Entry HEAD | `673453c2956cef77fc2cdd76ccba6b2cba7524b8` |
| Entry tree | `4c44a67e5a99ce1cdbaac274fd8741d9b66abd1f` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Binding prerequisites | S2.3 accepted (entry `e1ccaf6`; decision commit `3e3209f`); S2.4 entry assessment `673453c` |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

## 2. Resolution of M-1 … M-7

| ID | Question | Decision |
| --- | --- | --- |
| **M-1** | Continuation-token semantics | **Stateless, opaque, versioned, non-persisted** token (§3). No server memory, no durable sessions. |
| **M-2** | Publication-change behavior | **Refuse** (`StalePublication`); restart only by an explicit new query (§4). No silent restart. |
| **M-3** | Cross-target mode in S2.4? | **Deferred** to the optional `WP-S24-3`; forward-compat constraints fixed (§8), not implemented now. |
| **M-4** | Coverage-breakdown shape | Defined in §6. Scope-level, not page-level; never inferred beyond accepted authority data. |
| **M-5** | S2.4 work-package decomposition | Adopted in the [work-package plan](../planning/adr-ci1-01-ci1a-s2-4/work-package-plan.md). |
| **M-6** | Token persistence | **Transient only.** No new persisted token/session format; no schema change (§9). |
| **M-7** | Internal-contract boundary | **Preserved.** `Andirit` reader types remain an internal contract; no public Find Usages; ADR-CI1-02 absent (§9). |

## 3. Normative continuation contract (D1)

**Form.** A continuation token is an **opaque, versioned, stateless** string (base64url of a compact, canonical envelope). It is bound to exactly one query identity and one publication. The envelope fields:

| Field | Meaning |
| --- | --- |
| `v` | Contract version (integer; begins at `1`) |
| `repo` | `CodeRepositoryId` |
| `mode` | `target` (v1); reserved for a future `targets` cross-target mode |
| `target` | `CodeOccurrenceTargetId` (exact-target binding) |
| `pub.run` | publication `IndexRunId` |
| `pub.id` | publication `IndexIdentity` |
| `dir` | ordering direction (`asc` only in v1) |
| `after` | the **last emitted** total-order sort key (the full 6-tuple, §5) |
| `size` | requested page size for the continuation |
| `sum` | integrity checksum (first 16 lowercase hex of SHA-256 over the canonical envelope fields) |

**Query/result contract changes (internal reader only).**
- `CodeOccurrenceQuery` gains `string? ContinuationToken = null` (and may reuse `MaxResults` as the page size; `PageSize` alias permitted). `DefaultPageSize = 100`; `AbsoluteMaxPageSize = 1000`.
- `CodeOccurrenceQueryResult` gains `string? NextContinuationToken` (non-null iff more results exist within the same publication) and `CodeOccurrenceContinuationState Continuation` (`NotRequested | More | Complete | Invalid`). `IsComplete` semantics are unchanged and remain tied to complete declared scope — a continuation result is **never** `IsComplete` unless it is the final page of a complete-scope query.

**Target/query binding.** A token is valid only for the same `repo`, `mode`, `target`, and `size`-range; a token presented with a different target/repo/mode is invalid.

**Cursor and ordering.** The reader positions **strictly after** the token’s `after` key in the total order (§5); it never re-emits an already-emitted key. Duplicates/omissions are prevented by the strict total order and the strict-greater predicate within a stable publication.

**Page-size handling.** The returned page size is `min(requested size, remaining)`, hard-capped at `AbsoluteMaxPageSize`. A page smaller than the request is allowed only when the result is exhausted (`Continuation = Complete`, `NextContinuationToken = null`).

**Compatibility and invalid-token behavior.** Unknown `v`, malformed/oversized envelope, checksum mismatch, non-canonical encoding, binding mismatch, or an `after` key that is not a well-formed sort key → **invalid continuation**, refused without a partial page (see §7 separation). Tokens are not accepted across contract versions except where a later version defines a migration.

**Stateless vs memory.** Tokens are **stateless** (no server state); they require **no** persistence and **no** store memory beyond the current open store. Cryptographic tamper-proofing (HMAC) is **not** required at this internal boundary: the reader remains authoritative (a forged cursor can only skip within one publication and cannot fabricate facts), and qualification tests assert round-trip completeness. The `sum` checksum is tamper-*evident* against accidental corruption, not a security MAC. A future public surface (ADR-CI1-02) may require a keyed MAC; that is out of scope.

## 4. Publication-change and authority rules (D2)

A continuation is bound to `(repo, pub.run, pub.id)`. On **every** continuation request the reader re-runs the accepted authority gate and re-validates the current publication:

1. **Publication changed** (current publication differs from the token’s) → `Availability = StalePublication`, empty page, `Continuation = Invalid`, `IsComplete = false`, `NextContinuationToken = null`. **No silent restart; no cross-publication blending.**
2. **Recovery.** After an accepted known-failure recovery, continuation is valid only if the recovered publication identity equals the token’s `(run, id)`; otherwise rule 1 applies. Recovery must not resurrect a superseded continuation.
3. **Explicit restart.** The caller restarts by issuing a **new** query without a token (optionally with `expectedPublication`); the reader never restarts implicitly.
4. **Ownership/authority.** Continuation is subject to the same authority/orphan-owner correspondence as a fresh query; a continuation cannot bypass the authority gate.

## 5. Deterministic pagination (D3)

**Total order (exact-target, v1):** ascending by
`Span.Path (ordinal)`, `Span.StartLine`, `Span.StartColumn`, `Span.EndLine`, `Span.EndColumn`, `Id.Value (ordinal)`.
This is the accepted order (already implemented in `InMemoryCodeGraphStore.FindOccurrencesAsync`).

- **Uniqueness / tie-breaking.** `Id.Value` (the `usage4` occurrence identity) is globally unique, so the order is **strict and total**; equal-position occurrences are ordered by `Id`. Continuation seeks strictly after `(…, Id)`.
- **Equal-position occurrences.** Distinct occurrences at the same span are distinct by `Id`; all are emitted exactly once.
- **Overloads.** Distinct overloads resolve to distinct `TargetId` values (distinct target pages) and/or distinct `Id` values; no overload is merged. Ordering within a target is the total order above.
- **Cross-unit contributors.** An identical semantic fact contributed by multiple storage units is a **single** occurrence (contributors unioned on the hit), so it is emitted once and never duplicated across pages. Distinct semantic units remain distinct `usage4` facts (distinct `Id`).
- **Cross-target (deferred, v2).** A future cross-target order prepends `TargetId.Value` to the tuple; per-target order is unchanged.

## 6. Coverage breakdown (D6)

The result carries a **scope-level** coverage breakdown describing the query/publication scope — **not** the current page — unless a future field explicitly labels a page-level view.

Minimum internal breakdown (additive, derived only from persisted authority data — units, facts, coverage):
- **Aggregate** `CodeOccurrenceCoverage` (existing) with `State`, `ScopeId`, `UniverseId`, `DeclaredUnitCount`, `VerifiedUnitCount`, and the counters (`InvocationFacts`, `UnsupportedCategories`, `External`, `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive`).
- **Per-contributing-unit entries**: `(pluginId, unitId)` → `analysisPerformed`, `state` (`Produced`/`Partial`/`NotProduced`/`Unavailable`), `invocationFacts`, `unsupportedCategories`, and the refusal counters; plus `scopeId`/`universeId`.
- **Complete vs incomplete contributions** distinguished by `state`; aggregate completeness is **worst-wins** (per the accepted `CodeOccurrenceCoverage.Merge`); `IsComplete` iff aggregate `State == Produced`.
- **Unavailable/refused contributions** enumerated with their reasons (no silent omission).
- **Subset page.** When the page holds a subset of the scope’s occurrences, the breakdown still describes the **scope**; the page-subset state is conveyed by `TotalMatches`, `Truncated`, and `NextContinuationToken`, never by downgrading the coverage breakdown.

The breakdown must **not** claim more than the accepted authority data supports (S-01 membership proof and general canonicalization remain deferred); it must not conflate coverage with availability.

## 7. Bounded execution and dimension separation (D4/D5)

**Bounds.**
- Returned occurrences per request ≤ `min(requested, AbsoluteMaxPageSize)`.
- `Examined` = candidate entries read for the **target page** only; a small result page must **never** hide an unbounded scan. Exact-target reads are bounded by the target’s occurrence entries.
- Work per request is bounded by the target candidate set (`O(target entries)` for the seek+page).
- Token size bounded (≤ 512 bytes canonical); oversized tokens are invalid.
- Continuation depth is finite (pages terminate when no entry is strictly greater than the cursor).
- **Cross-target fan-out** (deferred): a future `MaxTargets` plus a global page budget; exceeding the budget is an **explicit bounded refusal** (never a silent truncation). Not implemented in v1.

**Dimension separation (D5).** The result keeps these orthogonal, with no false-complete/false-available:
- **Availability** (`CodeOccurrenceAvailability`) — store/publication state: `Available`/`AnalysisUnavailable`/`UnsupportedSchema`/`StalePublication`/`CorruptedIndex`/`IncompleteAnalysis`.
- **Coverage** — analysis completeness (§6).
- **Publication staleness** — token vs current publication (§4).
- **Incomplete analysis** — coverage `Partial` ⇒ `Availability = IncompleteAnalysis`, `IsComplete = false`.
- **Invalid continuation** — `ContinuationState = Invalid` (malformed/oversized/checksum/binding), kept distinct from publication staleness. A publication mismatch yields `StalePublication`; a structurally invalid token for the **current** publication yields `Continuation = Invalid` with the current availability and an explicit `RefusalReason`.
- **Authority** — revalidated on every request (§4).

**Continuation under partial coverage.** Permitted: `Availability = IncompleteAnalysis`, `IsComplete = false`, further pages still offered within the same publication. A zero-result page is never a complete negative unless the scope is complete.

## 8. Cross-target scope (D7)

**Cross-target is deferred** to an optional, separately authorized `WP-S24-3`. `WP-S24-1` implements **exact-target** continuation + coverage only, and must be implementable and qualifiable **independently** of cross-target work. Forward-compatibility constraints fixed now: tokens remain versioned (`v`), `mode` is reserved, per-target ordering is unchanged, and any future cross-target mode must add a `targets` cursor set and a bounded global budget (never an unbounded fan-out).

## 9. Contract and storage boundary (D8)

Confirmed:
- **Internal reader/query/result contract changes only.** `ICodeOccurrenceReader`, `CodeOccurrenceQuery`, `CodeOccurrenceQueryResult` (and the additive `CodeOccurrenceContinuationState`); no other surface.
- **No public Find Usages API.** `Andirit` reader types remain an internal contract; MD-03 preserved; ADR-CI1-02 is absent and remains a separate owner decision.
- **No new persisted token/session format.** Tokens are transient/opaque; no `AndiritOccurrence*` row or new label; continuation does not write.
- **No dual-write; no storage migration** (D5).
- **No expansion into S2.5 or Stage 6.**

If any of these cannot be maintained, the affected slice **stops** and returns for a blocking owner decision rather than expanding scope.

## 10. Work-package decomposition and gates

Adopted from the [work-package plan](../planning/adr-ci1-01-ci1a-s2-4/work-package-plan.md):

| Package | Scope | Independent gate |
| --- | --- | --- |
| **WP-S24-1** | Exact-target continuation + coverage breakdown; publication-change refusal; determinism; bounded `Examined`; per-page authority | **`S24-READER-Q1`** |
| **WP-S24-2** | IF-05 refusal-mapping documentation/tests; IF-02 real-target size/order/planner-bound verification; SQ-07/MD-02 cost measurement | **`S24-SURFACE-Q1`** |
| **WP-S24-3 (optional)** | Bounded cross-target continuation (separately authorized) | independent gate if authorized |

Each package: internal reader/tests only; no public API; no persistence/schema change; preserves all accepted D3/D4/D5 and S2.3 invariants; requires a self-excluding evidence manifest and zero unauthorized production change.

## 11. Independent qualification requirements

The `S24-READER-Q1` commission must include independent tests over the real Windows-x64 native store and a fresh-process probe harness covering: stable multi-page traversal; no duplicate or omitted occurrences; equal-position ties; overload and cross-unit identity; stale-publication refusal; recovery between pages; malformed and altered tokens (checksum/binding/oversize); per-page authority revalidation; candidate-examination bounds (`Examined`); coverage-breakdown correctness (scope-level, worst-wins); availability/refusal separation; deterministic replay against a stable publication; and **absence of new persistent storage behavior** (no new rows/labels; no writes during continuation). Expected values must be derived from the accepted contracts (ordering tuple, identity primitives, coverage algebra), not from the implementation. Evidence must include raw receipts and a self-excluding SHA-256 manifest; independent review must not reuse the producer's expected values.

## 12. Binding qualifications and deferred obligations

**Preserved without reinterpretation:** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08, the same-harness reviewer independence limitation, and the Windows-x64 / serialized single-writer boundary. **Deferred S2.5/Stage-2 obligations:** SQ-04 (authentic historical fixtures), SQ-05 (expanded Linux native qualification), SQ-06 remainder (independent-review/owner step + Stage-2 placement), S-01 (integrated proof), D4.5 and full S-02 (Stage 6), D3-full (real-project corpus), IF-02/IF-05 residuals, SQ-07 measurement, and the platform/independence boundaries. None is discharged by this decision; S2.4 does not treat qualified evidence as unrestricted platform or durability support.

## 13. Formal entry disposition and next authorization

**`S2.4 ENTRY AUTHORIZED WITH QUALIFICATIONS`.** The S2.4 **planning boundary** is authorized with the normative contract above and the adopted work-package plan. This authorizes **no implementation**.

- **Next authorized status:** a **separate owner commission** of `WP-S24-1` (exact-target internal continuation + coverage breakdown). Until that commission, no production or test change for S2.4 is permitted.
- **Not authorized:** `WP-S24-1` implementation (this decision alone does not authorize it); `WP-S24-2`; `WP-S24-3` cross-target; any public Find Usages; any new public API, persisted format, dual-write, or migration; S2.5; Stage 6.

## 14. Repository discipline

Only this decision document and the additive S2.4 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, prior decision, or S2.3 contract is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.
