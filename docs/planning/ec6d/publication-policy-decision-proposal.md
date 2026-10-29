# EC-6D publication policy decision proposal (ADR-2, EC6D-PLAN-R2)

**Owner-01 update:** Design 2, qualified results with all mandatory
safeguards, is accepted under the
[owner record §E](../../decisions/ec6d-owner-01-contract-acceptance.md#e-owner-architecture-decisions),
bound to corrected contract commit `cc65338f50e585d78c3a1fa8e06d02c184a10451`.
The checked consistent-only authority path remains required. The proposal
and blank template below remain historical rationale; the completed decision
is the owner record. No production implementation is authorized.

**Status: proposal for owner decision. Not approved.** No implementation may
be inferred while this decision is unresolved. It authorizes nothing.

## 1. Decision required

How confirmed impact results are presented when the publication's source
provider declares that it does **not** guarantee a single immutable source
snapshot (the only shipped filesystem provider always declares
`IsConsistentSnapshot = false`, with `SnapshotIdentity = null`).

Designs: **(2) qualified results with structurally mandatory
qualification** (R1/R2 recommendation) or **(1) consistent-only
fail-closed** results.

## 2. Proposed policy (design 2, precise)

1. **Identity**: every result carries `repositoryId`, `indexRunId`,
   `indexIdentity`, `snapshotIdentity`, `isConsistentSnapshot` and the
   derived `qualification` (publication-consistency-contract §3).
2. **Derivation (mechanical)**: `false`/any → `qualified`; `true` with a
   non-null, non-blank identity → `consistent`; `true` with a null identity
   → `qualified` (an inconsistent provider declaration is never promoted to
   snapshot authority).
3. **Meaning of qualified**: all participating facts were produced in the
   index run from per-file hash-verified reads; the provider does not
   guarantee one snapshot; results may mix per-file states read at
   different times and may not reflect edits made after the run.
4. **Confirmed semantics**: confirmations are asserted relative to the
   publication (all witnesses are stored facts of that publication); they
   are never asserted as snapshot-consistent statements under `qualified`.
5. **Integrity failures fail closed**: contradictory endpoints, mismatched
   unit versions, superseded units referenced by a chain, missing witness
   facts, identity conflicts, publication movement (including
   same-content/new-run), invalid input, cancellation, query/store failure
   → error, never partial classified output. Ordinary missing attribution
   remains a relationship-level unknown.
6. **Consumption safeguards**: qualification is a required discriminated
   field; official display/serialization/export preserve it; a checked
   consistent-only authority path refuses qualified results for automation;
   navigation surfaces label qualified results; tests pin
   serialization round-trips and the authority path (acceptance A17).
7. **Failure retention**: a failed or cancelled re-index publishes no
   partial binding facts and preserves the last successful publication;
   bound queries keep answering from it with its recorded identity and
   qualification (never a freshness claim). Later live edits need not move
   or upgrade it.

## 3. Interaction table

| Relationship state | Publication `qualified` | Publication `consistent` |
| --- | --- | --- |
| Confirmed (complete witness) | Returned with `qualified`; usable for navigation/triage; never as an authoritative snapshot statement | Returned with `consistent`; may gate automated changes |
| Possible consumer | Module-dependency label; qualification attaches to the whole result | Same, unqualified label retained |
| Unknown (reason) | Relationship-level unknown; qualification independent | Same |
| Integrity failure (broken chain, unit mismatch, movement) | Error — never classified output, regardless of qualification | Error |
| Cancellation / input error | Error — no partial output | Error |

Neither dimension absorbs the other: `unknown` says nothing about snapshot
consistency, and `qualified` says nothing about whether a specific witness
is valid.

## 4. Alternative (design 1) and tradeoffs

**Consistent-only fail-closed:** refuse confirmed results unless
`qualification = consistent`.
- Tradeoff: with the only shipped provider every publication is
  inconsistent, so EC-6D would return no confirmed results in production
  and could not be exercised end-to-end; the vertical slice's success
  criterion would be unreachable without a new snapshot-consistent provider
  (a provider-strategy / EC-6E-class concern beyond this milestone).
- It also does not remove the need for the truth table, identity block or
  integrity rules; it only converts `qualified` outputs into errors.
- Conservative merit: maximal caution where results feed automation; this
  is partially retained via the checked consistent-only authority path.

## 5. Recommendation

**Adopt design 2 (qualified results)** with the safeguards in §2, and
retain the credible-fail-closed alternative as a recorded option. If the
owner prefers design 1, confirmed results must be replaced by an explicit
`consistent-publication-required` error state, and the acceptance rows
(A07/A17) must be re-aimed accordingly — a correctness-relevant change
requiring re-review.

## 6. Decision gate

- The derivation rule, integrity-failure list and retention semantics are
  fixed regardless of the choice; the choice between returning qualified
  results and erroring on inconsistent publications changes result
  semantics and is **correctness-relevant**.
- No implementation, extraction or query authorization may be inferred
  while this decision is unresolved or if the owner selects design 1
  without the consequential amendments being reviewed.

## 7. Decision record (to be completed by the owner)

| Field | Value |
| --- | --- |
| Decision | (design 2 qualified / design 1 consistent-only) |
| Authority-path requirement retained | (yes/no) |
| Consequential amendments required | |
| Owner / date | |
