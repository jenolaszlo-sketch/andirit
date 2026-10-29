# EC-6D consumer-intelligence planning

**Current disposition — EC6D-OWNER-01: ACCEPT WITH QUALIFICATIONS.**
The [owner acceptance record](../../decisions/ec6d-owner-01-contract-acceptance.md)
binds the corrected contracts at `cc65338f50e585d78c3a1fa8e06d02c184a10451`
/ tree `9940965196b35488258501e9513ec1d8025d4007`, closes Q3-F01..F05
and records ADR-1 Option A, ADR-2 qualified results with safeguards and
ADR-3 narrow attribution authority. [Closure/audit](owner-01-findings-closure.md),
[validation](owner-01-validation.md), and the
[HETU-INT-01 handoff](owner-01-integration-handoff.md) are the current entry
points. The R2 status and proposal language below describe their historical
authorship state; the owner record supplies current decision status.
R16 and all inherited qualifications remain open. No implementation,
integration, rename, publication or release is authorized.

**EC6D-PLAN-R2 status: remediation complete; ready for focused Q3
verification after the owner decisions listed below are prepared (not yet
approved).** The recovered EC6D-PLAN-01 packet (commit `91a3ed5`) and the R1
remediation (`0e1b213`) remain the historical record; this branch
(`planning/hetu-ec6d-plan-r2`) applies the targeted repairs required by the
reconciled Q2 review (register Q2-01..Q2-08). It contains no EC-6D
implementation or production source change.

## R2 additions (read first)

1. [Q2 findings disposition](q2-findings-disposition.md): the Q2-01..Q2-08
   matrix with the N01–N09 crosswalk, remediation, acceptance links,
   reason reachability and the cross-contract consistency audit.
2. [Representation decision proposal](representation-decision-proposal.md)
   (ADR-1): query-time joins recommended; correctness-relevant owner
   decision.
3. [Publication policy decision proposal](publication-policy-decision-proposal.md)
   (ADR-2): qualified results with safeguards recommended; fail-closed
   alternative recorded; correctness-relevant owner decision.

## Review in this order

1. [Symbol-binding contract](symbol-binding-contract.md) (R2): facts A, C
   and E; physical-declaration attribution; the lexical-binding rule that
   replaces import-first; computed witness identity.
2. [Consumer query contract](consumer-query-contract.md) (R2):
   entry-driven discovery; closed reason table with reachability;
   structured coverage and the not-evaluated frontier; charged-work
   budgets.
3. [Publication-consistency contract](publication-consistency-contract.md)
   (R2): identity block; consistency truth table; integrity failures;
   checked consumption.
4. [Acceptance contract](acceptance-contract.md) (R2): the A01–A28 matrix
   with the change log and fixture inventory (committed Q2 corpora plus
   [analysis/fixtures/r2](analysis/fixtures/r2/README.md)).
5. [Scope proposal](scope-proposal.md) (R2 revision): the six R2 changes
   and the unchanged milestone boundary.
6. [Integration readiness](integration-readiness.md) (R2 revision):
   implementation/integration/release gates and the narrow-attribution
   authorization requirement.
7. [Risk register](risk-register.md): R19 (lexical shadowing) and R20
   (unevaluated-relationship visibility) added.
8. [Q1 findings disposition](q1-findings-disposition.md) with its R2
   update note; [evidence index](evidence-index.md) with the R2 evidence
   section.
9. Historical packet (unchanged content, corrections marked): [recovery
   and provenance](recovery-and-provenance.md), [current capability
   inventory](current-capability-inventory.md), [consumer use cases](consumer-use-cases.md),
   [consumer baseline](consumer-baseline.md), [gap analysis](gap-analysis.md),
   [integration delta](integration-delta.md).

## Recommended scope

Bounded TypeScript interface impact through direct named relative imports
and module-scope simple-identifier class `implements` clauses: return
uniquely bound explicit implementers (import route or same-file route) with
complete witnesses, direct named-importing modules labeled as possible
consumers, and explicit unknowns for every unevaluated relationship inside
the declared boundary. Re-export closure (one bounded alias-association hop
only), ordinary type-use references, calls, implicit implementations, graph
diff and performance claims remain deferred. Success is pinned at the query
boundary: the committed corpus's four intended direct implementations must
return as confirmed results with full witnesses, negatives must stay
negative, and no false-confirmed-binding rule may remain in the supported
profile.

This is a proposal only. The owner must separately authorize design,
implementation, integration and release; the two decision proposals and
the narrow-attribution authorization are prerequisites, and EC-6C
integration, packages, releases and Track B remain outside this planning
task's authority. This assessment does not establish production readiness.
