# EC-6D consumer-intelligence planning

**EC6D-PLAN-01 status: ready for independent scope review.** This branch
contains assessment, documentation, a committed non-production TypeScript
fixture and a read-only analysis harness. It contains no EC-6D implementation
or production source change.

## Review in this order

1. [Integration readiness](integration-readiness.md): exact Git ancestry,
   dependency status, candidate delta, qualifications and required owner
   authorization.
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
   bounded interface-impact slice.
7. [Acceptance contract](acceptance-contract.md): a verifiable proposal for
   owner approval.
8. [Risk register](risk-register.md): qualification, consumer and
   authorization risks.
9. [Evidence index](evidence-index.md): revisions, commands, results,
   artifact paths and preservation boundaries.

## Recommended scope

Bounded TypeScript interface impact through direct named relative imports:
return uniquely bound explicit implementers and direct importing modules
labeled as possible consumers, with source-backed evidence and explicit
unknowns. Re-export closure, ordinary type-use references, calls, implicit
implementations, graph diff and performance claims remain deferred.

This is a proposal only. The owner must separately authorize design and
implementation. EC-6C integration, packages, releases and Track B are also
outside this planning task's authority.
