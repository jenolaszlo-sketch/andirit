# EC-6D consumer-intelligence planning

**EC6D-PLAN-01 status: recovered exactly (EC6D-PLAN-R1); ready for
independent Q2 review after owner decision on the R1 contracts.** The
original planning branch, commit, tree, and worktree were recovered intact
(see [recovery-and-provenance.md](recovery-and-provenance.md)); this branch
(`planning/hetu-ec6d-plan-r1`) extends the recovered tip with remediations
for the independent Q1 review findings. It contains no EC-6D implementation
or production source change.

## R1 additions (read first)

0. [Recovery and provenance](recovery-and-provenance.md): recovery
   classification, verified Git identities, reproduction of the committed
   baseline, and authenticity boundaries.
0a. [Q1 findings disposition](q1-findings-disposition.md): F-01 through F-08
   with dispositions, evidence and remaining qualifications.

## Review in this order

1. [Integration readiness](integration-readiness.md): exact Git ancestry,
   dependency status, candidate delta, qualifications and required owner
   authorization.
   The complete changed-path inventory is in
   [integration-delta.md](integration-delta.md).
2. [Current capability inventory](current-capability-inventory.md): what
   consumers can use on the owner-accepted candidate, separated from
   canonical main.
3. [Consumer use cases](consumer-use-cases.md): five representative agent
   questions, current support and evidence gates.
4. [Consumer baseline](consumer-baseline.md): the manually reviewed
   TypeScript interface scenario, observed queries, errors and limits.
5. [Gap analysis](gap-analysis.md): G1 through G7 with evidence, impact,
   dependencies and priority.
6. [Scope proposal](scope-proposal.md): three alternatives and the recommended
   bounded interface-impact slice, plus the R1 revision that adds the
   binding-fact prerequisite and defines vertical-slice success.
7. [Symbol-binding contract](symbol-binding-contract.md) (R1): the import
   occurrence, export attribution, and five-step binding contracts.
8. [Consumer query contract](consumer-query-contract.md) (R1): the versioned
   `GetInterfaceImpactV1` result categories, unknowns, ordering and bounds.
9. [Publication-consistency contract](publication-consistency-contract.md)
   (R1): qualified-result policy and fail-closed boundaries.
10. [Acceptance contract](acceptance-contract.md): the verifiable proposal
    for owner approval, with the R1 A01–A20 requirements matrix.
11. [Risk register](risk-register.md): qualification, consumer and
    authorization risks.
12. [Evidence index](evidence-index.md): revisions, commands, results,
    artifact paths and preservation boundaries.

## Recommended scope

Bounded TypeScript interface impact through direct named relative imports:
return uniquely bound explicit implementers and direct importing modules
labeled as possible consumers, with source-backed evidence and explicit
unknowns. Re-export closure, ordinary type-use references, calls, implicit
implementations, graph diff and performance claims remain deferred. R1 adds
the two missing fact capabilities (import occurrences; export attribution)
that make a confirmed binding possible without name-only matching, and pins
milestone success to the query boundary.

This is a proposal only. The owner must separately authorize design and
implementation. EC-6C integration, packages, releases and Track B are also
outside this planning task's authority. This assessment does not establish
production readiness.
