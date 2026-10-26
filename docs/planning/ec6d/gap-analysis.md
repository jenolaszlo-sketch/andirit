# Consumer gap analysis

The evidence basis is the accepted EC-6C candidate and the TypeScript fixture
at [consumer-baseline.md](consumer-baseline.md). Severity reflects usefulness
to an agent handling the evaluated interface-change question, not a general
product defect rating.

## G1: Extraction gaps — HIGH

**Evidence:** The Tree-sitter TypeScript profile emits no calls or references.
Written implements targets are declaration-less placeholders. The fixture
contains 17 imports but no imported-name binding and 13 exports edges but no
declaration attribution. The hand-reviewed Renderable query returns no
implementations and does not expose either type-use consumer.

**Affected use cases:** UC-1, UC-2, UC-3.

**Practical impact:** An agent can see syntax but cannot trace a changed
interface to its implementations or type users. A spelling-based fallback
returns one same-name false positive and misses the alias implementer.

**Components:** EcmaScriptExtractionProfile, EcmaScriptExportGraphPublisher,
TreeSitterModuleResolution and the declaration/member model.

**Candidate solution:** Extract named TypeScript import occurrences for a
narrow supported grammar; bind direct relative imports to unique direct
exports and declarations; associate explicit heritage placeholders with the
resolved interface declaration. Keep unsupported and ambiguous forms
unbound and observable.

**Dependencies:** EC-6B relative-v1 and EC-6C p9 export entries; a prior
architecture decision on additive symbol-binding facts and their evidence
meaning.

**Regression risks:** Overbinding on name equality, declaration merging,
type/value confusion, duplicate imports, aliases, stale target changes,
profile invalidation and identity drift.

**Recommended priority:** P0 for any milestone claiming interface impact;
exclude calls/references and arbitrary type uses from the first slice.

## G2: Resolution gaps — HIGH

**Evidence:** resolves-to targets a file only. It does not bind a named import
or exported name to a declaration. The baseline has six direct file edges to
the selected module, but this does not associate the imported Renderable name
with its declaration. The barrel's source module resolves, while its three
written re-export names create no exports edges.

**Affected use cases:** UC-1, UC-2, UC-3.

**Practical impact:** The graph cannot distinguish the selected Renderable
from another same-named interface, follow Drawable back to Renderable, or
confirm that an importer actually uses the symbol.

**Components:** TreeSitterModuleResolution, EcmaScriptExtractionProfile,
EcmaScriptExportGraphPublisher, graph identity/evidence contracts.

**Candidate solution:** Bounded same-plugin resolution for direct named
imports and direct named exports, including local aliases. Bind only when
the target module and declaration are unique. Defer re-export closure and
compiler configuration.

**Dependencies:** A defined source/profile boundary and fail-closed behavior
for missing, external, unsupported, duplicate and ambiguous names.

**Regression risks:** Confusing a verified module path with a verified symbol;
claiming compiler or runtime equivalence; crossing plugin inventories;
incorrectly binding an alias or declaration merge.

**Recommended priority:** P0 with G1. Do not expand relative-v1 policy in the
same change unless the chosen test corpus requires it.

## G3: Query gaps — HIGH

**Evidence:** Generic neighborhoods accept explicit edge-kind filters, but
the impact preset excludes imports, exports and resolves-to. FindImplementations
on the selected interface declaration returns zero. A TypeScript Project node
is not emitted, so project-rooted public-surface lookup is not a working
shortcut for this profile.

**Affected use cases:** UC-1, UC-2, UC-3.

**Practical impact:** The facts that do exist are not sufficient to get a
bounded interface-impact answer through one consumer operation. Consumers
must make several low-level calls and still cannot infer the missing binding.

**Components:** CodeGraphQueryService, CodeGraphQueryModels and
CodeGraphQueryServiceTests.

**Candidate solution:** Add a narrowly defined interface-impact query or a
documented typed composition that reports direct explicit implementers,
direct potential importers, ambiguity and unresolved counts. Preserve query
bounds and expose provenance for every candidate.

**Dependencies:** G1/G2 relationship contract; bound-view and truncation
semantics.

**Regression risks:** Quietly changing preset membership, crossing
publication boundaries, omitting evidence on derived paths, or presenting
possible importers as confirmed uses.

**Recommended priority:** P0 after relationship semantics are designed.

## G4: Identity and change-tracking gaps — MEDIUM

**Evidence:** Stable identities, deterministic IndexIdentity, unit
replacement, publication snapshots and integrity checks exist. Only the
latest publication is queryable; no graph diff or historical retention API
exists. The baseline fact digest is deterministic, but that alone does not
classify changes.

**Affected use cases:** UC-4 primarily; all use cases when source revisions
change.

**Practical impact:** An agent cannot ask Hetu directly for added/deleted
relationships between two revisions and cannot assume a replaced publication
remains available.

**Components:** CodeIndexIdentity, CodePublicationSnapshot, indexing
lifecycle and store publication contracts.

**Candidate solution:** Defer graph diff to a separate milestone. Preserve
source revision, provider versions and canonical snapshots when consumers
need external comparison.

**Dependencies:** A later retained-snapshot or two-snapshot diff contract.

**Regression risks:** Misclassifying intentional profile-version identity
changes as source changes; unbounded history or storage growth.

**Recommended priority:** P2 for this scope.

## G5: Provenance and explainability gaps — MEDIUM

**Evidence:** Provenance envelopes include publication/query binding,
provider/version, unit origin, evidence and source locations. The API does
not return excerpts and does not promise a single witness path explaining
every multi-hop result. The baseline has evidence for file and export facts,
but no edge explaining the missing alias-to-declaration step.

**Affected use cases:** UC-1, UC-2, UC-3 and UC-5.

**Practical impact:** A consumer can inspect individual known edges, but
cannot explain a complete intended impact chain. Downstream presentation
could overstate structural or policy-bounded evidence.

**Components:** Query envelopes, traversal results and CodeGraphEvidence
models.

**Candidate solution:** Make evidence level and relationship path mandatory
fields of the proposed impact response. Source excerpt retrieval can remain
with the repository reader if the query returns exact spans.

**Dependencies:** G1/G2/G3; no separate storage requirement for the initial
slice.

**Regression risks:** Treating Resolved file policy as semantic binding;
dropping provenance while aggregating paths; stale source locations.

**Recommended priority:** P1 acceptance requirement, not a separate broad
explanation product.

## G6: Reliability and communication gaps — HIGH

**Evidence:** The fixture demonstrates a same-name false positive and alias
false negative when placeholders are matched by spelling. Missing import
information is an aggregate unresolved count rather than a per-symbol
negative fact. EC-6C exports are explicitly positive-only. The file-system
provider marked the baseline publication not snapshot-consistent.

**Affected use cases:** All five.

**Practical impact:** Absence and truncation can be mistaken for a negative
answer; same-name candidates can be conflated; concurrent source changes
cannot be ruled out by the experiment's source provider.

**Components:** Provider diagnostics/coverage, query envelopes, resolver
policy, and consumer response contract.

**Candidate solution:** Make unresolved/ambiguous/unsupported/not-evaluated
states explicit. Include coverage and snapshot-consistency metadata in the
consumer result. Require unique resolution and fail closed.

**Dependencies:** Query response design and provider diagnostic contract.

**Regression risks:** Overclaiming completeness; unbounded diagnostics;
missing stale-fact replacement after source changes.

**Recommended priority:** P0 for the selected profile.

## G7: Performance and operations — NOT EVALUATED

**Evidence:** This baseline indexes thirteen TypeScript files and makes
bounded in-memory queries. No large-repository throughput, memory, durable
store latency or repeated-index performance was measured.

**Affected use cases:** All five at scale.

**Practical impact:** Unknown. No claim that current or proposed performance
is adequate or inadequate is supported.

**Components:** Tree-sitter inventory/extraction, module-resolution pass,
publication stores and query traversal.

**Candidate solution:** After scope stabilizes, measure indexing time, peak
memory, query latency and durable close/reopen behavior on a representative
repository. Record hardware, source count, cold/warm state and result bounds
before setting targets.

**Dependencies:** A representative repository and reproducible benchmark
environment.

**Regression risks:** Optimizing a narrow synthetic fixture; noisy thresholds
without an operational target.

**Recommended priority:** P2 measurement task; establish a baseline before
promising a threshold.

## Cross-cutting open issue

No agent interviews, usage telemetry or real consumer sessions were provided.
The five questions are the task's representative requirements, and the
fixture is a controlled diagnostic. The scope should be confirmed with an
intended consumer owner before implementation authorization; this limitation
does not alter the observed query gap.
