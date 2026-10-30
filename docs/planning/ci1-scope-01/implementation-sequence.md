# Separately authorizable implementation sequence

This sequence does not authorize any production change.

| Gate | Work if explicitly authorized | Reviewable exit |
| --- | --- | --- |
| S0: CI-1-SCOPE-Q1 | Independent source/evidence/contract challenge | Findings and supported/remediation disposition bound to reviewed Git tree |
| S1: owner scope decision | Resolve five ADRs, category/identity/coverage/budget rules and historical policy | Explicit accepted scope; separate implementation authorization |
| I1: contracts and oracle | Freeze minimal provider-neutral occurrence/coverage values, query statuses and canonical identity/serialization; independently review source expectations | Contract diff, manually verified oracle, no approved public API assumed |
| I2: ownership/storage skeleton | Only selected representation: validated staging, target index, atomic replacement/deletion, both providers, durable/snapshot compatibility | Authentic historical open, old-query equality, new-data roundtrip and failure/cancel retention |
| I3: C# occurrences | Plugin-private InvocationExpression extraction, normalization/classification, overlap dedup, owner contexts, disjoint coverage outcomes | C01-C12/C19/C20 positive and negative results without old edge changes |
| I4: answer-oriented query | Exact selection, bound declarations/sites/owners/proofs, coverage, work/byte quotas and deterministic output | C13-C18, no full-repository symbol fetch, honest hard-budget truncation |
| I5: consumer economics | Real Andirit probes, paired baseline/candidate retrieval/index/storage/reopen measurements | Correctness and total-cost evidence, explicit context gaps and no fabricated tokens |
| I6: regression/independent qualification | Existing solution/provider regressions and an independent new-feature reviewer | Reproducible findings; independent false-confirmation/omission/stale-fact challenges |
| I7: owner acceptance | Review evidence, costs, residual limits and independent disposition | Explicit owner accept/reject; release remains separate |

I2 and I3 may be built in small reviewable increments only after the owner
accepts their contracts. Do not implement both storage alternatives speculatively.
No separate worktree is required or authorized by this planning request.

## Scope controls

The authorized first candidate ends at CI-1A: ordinary method invocation sites,
static/generic/extension/interface/virtual facets within accepted context, direct
caller grouping and honest unsupported coverage. Constructors, method groups,
read/write, method override mapping, indirect blast radius/test expansion and
GetChangeImpactV1 require separate scope gates.

A broader compiler-context resolver, local-function identity repair, full
self-indexing failure remediation, external metadata indexing or MSBuild/NuGet
evaluation must not be hidden in I3. If required for practical benefit, report
the concrete dependency and return to the owner with a bounded proposal.

Preserve EC-6D accepted production/history and all old query/persistence behavior.
Tests must reject aggregate-as-occurrence output and inaccurate complete-empty
claims. Document result limits instead of loosening them to get fixtures green.

## Stop/narrow/reject criteria

Stop implementation for unapproved schema migration, changed legacy IDs/cardinality,
lost shared contributors, mixed publication, wrong target confirmations,
unsupported cases silently treated as complete, unbounded processing, or measured
cost that cannot justify the selected consumer benefit.
Fixes require review under the applicable authorization; release/package/MCP/
autonomous edit work is not implied by acceptance of a design or implementation.

Planning completion means a reviewable proposal plus qualifications; it does not
mean S0/S1 or any I gate has already passed.
