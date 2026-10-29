# EC-6D representation decision proposal (ADR-1, EC6D-PLAN-R2)

**Owner-01 update:** Option A, query-time joins with computed witnesses,
is accepted under the scope/restrictions of the
[owner record §E](../../decisions/ec6d-owner-01-contract-acceptance.md#e-owner-architecture-decisions),
bound to corrected contract commit `cc65338f50e585d78c3a1fa8e06d02c184a10451`.
Option B is not authorized. The proposal and blank decision template below
are retained as historical rationale; the completed decision is the owner
record. No production implementation is authorized by planning acceptance.

**Status: proposal for owner decision. Not approved.** No implementation may
be inferred while this decision is unresolved. It authorizes nothing.

## 1. Decision required

How EC-6D represents symbol binding: **(A) query-time joins** over persisted
source-grounded occurrence/attribution/context facts, or **(B)
extraction-time binding edges** persisted during extraction and maintained
as the source graph evolves. R1 recommended Option A; Q2 asked whether that
recommendation remains sound after the lexical-shadowing and
physical-cardinality findings.

## 2. Context after R2

- Fact A (import occurrences) is per-file on the importing side; fact C
  (heritage context) is per-file on the consumer side; fact B (attribution)
  and export entries live on the target/exporting side; fact E (export
  references) is per-file on barrel sides.
- The lexical-binding rule (symbol contract §3) is a deterministic function
  of those facts: gates → candidate uniqueness → resolution → entry →
  attribution status → physical declaration identity.
- Witness identity is a versioned computed derivation
  (`binding1.local-v1`) over ordered stored fact ids and policy.

## 3. Option comparison

| Dimension | A — query-time joins (recommended) | B — extraction-time binding edges |
| --- | --- | --- |
| Semantic correctness | Same rule evaluated over stored facts; ambiguity states (`ambiguous-export`, shadowing, duplicates) remain authoritative classifications | Requires replicating the same rule at extraction; ambiguity must be encoded as edge presence or absence, risking false absence (fail-open) or phantom presence on stale data |
| Lexical-scope handling | Gates read fact C (single-file, importer side); no scope knowledge needed beyond the stored context | Same data required; edge emission additionally couples consumer-file extraction to target-file facts |
| Evidence traceability | Witness recomputed from fact ids; every step independently verifiable; no invented ids | Edges could carry witnesses, but the edge itself duplicates derivable state and can diverge from its inputs |
| Durable identity | Facts are stable and additive; witness identity deterministic across reopen | Binding edges need their own identity, ownership and version-bump discipline; persisted spans/joins age with any participant edit |
| Incremental updates | Single-file unit replacement invalidates only local facts; a target/barrel edit invalidates nothing on the consumer side (queries re-derive) | A binding edge is a **cross-file fact**: any participant (consumer, target, barrel) edit requires invalidation across units — dependency ownership beyond the accepted lifecycle |
| Failure and ambiguity behavior | Fail closed at query time with reasons; partial/failed indexing cannot create phantom bindings | Stale edges can survive failed or partial re-indexing; retraction rules needed for merges, deleted targets, changed attribution |
| Query boundedness | Charged candidate scans, joins, attribution lookups and witness steps are budgeted and observable (query contract §5) | Traversal over fewer edges, but edge maintenance and retraction costs move to indexing; ambiguity handling still query-side |
| Migration complexity | Additive facts A/C/E/B-status; repeat the accepted p8→p9 pattern; both stores qualified as usual | Additional cross-unit edge family and lifecycle; heavier rehearsal surface |
| Compatibility with p9 facts | Fully additive; existing facts untouched (A19) | Additive edges plus risk of existing placeholder consumers misreading derived edges |

## 4. Post-Q2 re-assessment

The R1 recommendation **remains sound**, and the Q2 findings strengthen it:
the correction requires *more* context (scope, type parameters, physical
cardinality, attribution status) whose authority is the stored single-file
record set; a persisted binding edge would either duplicate that authority
(divergence risk) or move cross-file invalidation into the indexing
lifecycle the accepted contracts do not model. A deterministic false answer
is still incorrect; Option A keeps refusals visible as reasons instead of
missing edges.

## 5. Recommended decision

**Adopt Option A — query-time joins**, with:

1. facts A, B (status records), C and E persisted additively (§ symbol
   contract §1–2);
2. the witness identity computed at query time (`binding1.local-v1`,
   canonical hash over ordered fact ids + policy) rather than persisted as
   an edge; if persistence is ever needed, it must carry explicit
   dependency ownership and be re-derived on change;
3. the correctness obligations (scope gates, physical cardinality,
   resolver recomputation/join, ambiguity propagation, source-version
   binding, bounded enumeration) fixed as contract text — none deferred to
   implementation;
4. the representation *spelling* (edge kinds, property keys, record
   containers) finalized at owner acceptance within these constraints.

## 6. Decision gate

- Choosing Option B changes the fact set, lifecycle, invalidation
  requirements and acceptance rows (cross-unit ownership, retraction
  tests): it is **correctness-relevant** and requires a re-review of the
  amended contracts before implementation authorization.
- Choosing Option A within the constraints above is the planned path; the
  decision still requires explicit owner approval — silence is not
  approval.
- No implementation, extraction or schema activation may start under either
  option until the owner decision is recorded and the remaining
  prerequisites in `integration-readiness.md` are met.

## 7. Decision record (to be completed by the owner)

| Field | Value |
| --- | --- |
| Decision | (A query-time joins / B extraction-time edges) |
| Chosen witness identity handling | (computed / persisted with dependency ownership) |
| Owner / date | |
| Consequential amendments required | |
| Re-review outcome (if B) | |
