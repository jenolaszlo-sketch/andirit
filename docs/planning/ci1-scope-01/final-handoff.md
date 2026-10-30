# Final handoff

**CI-1-SCOPE-01 READY WITH QUALIFICATIONS.**
This is an implementation specification proposal, not implementation approval.
Independent CI-1-SCOPE-Q1 and an explicit owner decision are the next gate.

## Git/source binding

Repository jenolaszlo-sketch/andirit, canonical D:/projects/Andirit, branch main.
Starting HEAD/main/origin/main and remotely advertised main:
cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2.
Starting tree: af9202904f8dbf0f696c21b6893b5184fa3b5423.
Initial clean status, one canonical worktree, no linked worktrees or concurrent
tracked changes found. Source-binding evidence verifies all 47 real inputs
against the starting Git blobs. [Git record](evidence/git-binding.txt).
Planning-content commit: 69d5152cb3ac1f9699e82981e11c122f261ada16;
tree: fa134d401bd2952484bc25336000084ee503854c.
Delivery content and synchronization status are recorded in
[delivery binding](evidence/delivery-binding.txt); the final containing commit
is necessarily identified externally by Git/the chat handoff, not by its own
self-referential content hash.

## Findings and recommended design

Andirit already provides project-scoped logical symbols, physical declarations,
resolved aggregate calls/references, direct type heritage, provenance-aware
bounded queries, atomic publication, freshness checks and two store providers.
It does not retain every ordinary usage occurrence, classify all usage categories,
prove runtime dispatch, or prove callers require edits.

Controlled three-call fixture: three independently bound invocations, one calls
edge retaining the first span, one overlapping method-reference edge.
Calls EdgesEmitted=3 counts attempts, not stored edges. A three-type-reference
fixture similarly retains one reference. Positive in real source has eight
invocations but one calls edge. Query-only wrapping cannot recover lost spans.

Recommend CI-1A: exact local ordinary method definition selection and distinct
supported InvocationExpression sites with static/generic/extension/interface/
virtual facets, lexical owner and declarations, source/hash/span, per-site
provenance, publication/coverage, hard processing/output limits and direct
caller grouping. Constructors, local-function targets, method groups, read/write,
override mapping, indirect impact and expanded tests remain deferred.
No runtime receiver closure or required-edit claim.

ADR recommendations: separate source-owned occurrences plus legacy aggregate
edges; versioned answer-oriented FindUsagesV1 alongside existing queries;
separate future GetChangeImpactV1; static targets with conservative ambiguity;
normal project-unit atomic lifecycle and approved historical codec/reindex route.
A new versioned occurrence edge kind remains a viable smaller storage alternative.
The owner must decide before production APIs or schema changes.

## Acceptance and evidence

Proposed C01-C20 matrix plus source-selected expression/signature manifest:
repeat sites, overloads, cross-project, generic/extension, static interface/base/
virtual semantics, deferred groups/constructors/type references, external and
ambiguous negatives, partial types, deterministic retries, deletion/span shift,
bounds/publication movement, two-store/reopen and conditional/generated context.
Expected outputs are derived from source reasoning, not current edge output.
Independent review of the manifest is pending.

Existing regressions: 47 C# tests, 20 selected core/query/snapshot/memory tests,
six selected LatticeDB/conformance/reopen/filter tests passed.
No new production test or full-solution qualification is claimed.
Planning checks verify links/JSON/source spans, baseline report assertions and
the authorized diff boundary.

Real core sample: 47 inputs, 382,944 UTF-8 text bytes; 1,981 nodes,
1,976 declarations, 7,935 edges. Extraction 3,045.3709 ms and ingestion/completion
112.8454 ms in one observation. Twenty public queries plus one shared open,
five source searches and five reads; 78,895 source-text bytes, 4,546 search bytes
and 91,486 serialized query bytes. No measured token savings or candidate timing.
See [performance plan](performance-evaluation-plan.md) for accounting limits.

## Qualifications and risks

- Adding raw C# plugin sources to the sample fails in existing AddSymbol
  diagnostic formatting (InvalidCastException); the underlying inconsistent
  symbol trigger and full self-indexing are not established or repaired.
- Raw plugin context omits generated SDK implicit usings and external package/
  exact target-framework context. The separate real-source oracle explicitly adds
  standard implicit usings. Its differences are not a same-context recall score.
- The successful subset still reports 1,894 unresolved call attempts and 4,474
  unresolved reference attempts. Global counters cannot locate selected-target
  unknowns. V1 needs attributable persisted category/unit coverage.
- The oracle shares Roslyn and the packet author; it is not independent
  qualification. Candidate storage bytes, replay, total task time, tokens and
  performance break-even remain unknown.
- Source-hash occurrence IDs deliberately churn after edits; linked project
  contexts remain distinct. Historical read/reindex/migration policy needs approval.

These qualifications are visible review inputs, not grounds to claim the
future implementation works. No accepted EC-6D artifact is modified or reopened.

## Publication permission

The direct ordinary push to origin/main was rejected before execution by
automatic approval review, citing shared-main collaboration risk and insufficient
publication authorization. No push or workaround was executed. The complete
packet remains committed locally; explicit approval is required to retry the push.
This execution-permission qualification does not authorize production work.

## Owner decisions and next gate

Decide CI-1A scope, C versus versioned occurrence-edge representation, identity
tuple/edit stability, coverage and non-error binding gate, query limits/statuses,
historical schema/snapshot compatibility, and acceptable measured overhead.
Authorize CI-1-SCOPE-Q1 independently; accept/remediate the scope based on its
findings. CI-1B, compilation-context remediation and full self-indexing fixes
require their own bounded authorization if needed.
Release, package publication and production implementation remain unauthorized.

## Exact change inventory

[change-inventory.txt](evidence/change-inventory.txt) enumerates every changed/
added tracked delivery path. Only docs/planning/ci1-scope-01/** and the requested
status-language edits to docs/agent-code-intelligence-roadmap.md are permitted.
The packet contains the eleven requested documents, read-only analysis harness/
checks, planning fixture examples and measurement/regression/Git evidence.
Ignored bin/obj products are not delivery artifacts. No src, existing tests,
schema, package version, workflow or historical evidence change is authorized.
