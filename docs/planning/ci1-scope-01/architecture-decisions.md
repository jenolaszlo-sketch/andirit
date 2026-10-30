# Architecture decision proposals

All five ADRs have status **PROPOSED / OWNER DECISION REQUIRED**.
Recommendations do not authorize code, schema changes or release.
Evidence: [inventory](existing-capability-inventory.md),
[reproduction](occurrence-representation-analysis.md),
[consumer cases](consumer-scenarios.md) and raw evidence/.

## ADR-CI1-01: Occurrence storage

Problem: aggregate source-target edge identity loses physical sites; changing
existing calls/reference IDs would change accepted query cardinalities and bounds.

Alternatives: (A) aggregate plus on-demand rebinding, (B) occurrence-aware existing
edges, (B2) new versioned occurrence edge kind plus aggregates, (C) separate
source-owned occurrence facts plus aggregates, (D) typed witness collections.

Evidence: three calls yield one edge, eight Positive calls yield one edge;
one callee token also emits a separate reference edge. Exact spans cannot be
reconstructed from the stored graph.

Advantages: A keeps storage small; B reuses adjacency; B2 preserves old presets;
C gives explicit site/ownership/target-index semantics without graph nodes;
D reduces pair-level overhead while retaining witnesses.

Risks: A repeatedly reads/rebinds exact sources; B silently changes callers;
B2 grows graph edges/properties and needs careful preset exclusion; C expands
batch/store/snapshot codecs; D needs witness merging/paging/conflict rules.
Every occurrence-preserving option is O(U) in evidence, even if edge count is O(P).

Compatibility: preserve current aggregate IDs, old presets/traversal results and
public API behavior. New query never treats aggregate locations as complete sites.

Storage/performance: benchmark C vs B2 on U/P, target reads, allocation, incremental
replacement and replay, not only query speed. No measured winner exists.

Recommend C with B2 as a serious simpler alternative before durable authorization.
Owner decides representation, exact minimal payload, identity tuple, budgets and
whether benchmark evidence warrants C's extra contract surface.

## ADR-CI1-02: Public query shape

Problem: an agent currently needs selection/declarations/callers/references,
provenance/source joins and source reads; graph traversals do not preserve sites.

Alternatives: extend low-level traversal, versioned answer-oriented FindUsagesV1,
or a consumer-only adapter over existing output.

Evidence: four public queries per target and lost sites in baseline; existing
envelopes already supply publication/provenance foundations.

Advantages: traversal is flexible; adapter can ship with no core API expansion;
answer-oriented result directly exposes exact sites, owners and completeness.

Risks: traversal makes consumers reconstruct semantics; adapter cannot recover
discarded evidence and might wrongly imply completeness; answer shape can grow
too large if every engineering question is included.

Compatibility: retain old query service and bound views; add a separately versioned
operation only after approval. Provider-neutral strings/values, Roslyn stays plugin
private. Discovery remains separate; do not fetch all repository symbols.

Storage/performance: query needs indexed target access plus bounded summaries and
joins. Existing low-level limits lack processing-work/byte budgets; new shape
must impose them. No unbounded unit scan hidden behind compact output.

Recommend versioned FindUsagesV1 alongside existing service/bound-view patterns.
Owner decides API placement, status model, normalized JSON/DTO boundary, hard
limits and whether any future continuation is justified. Names remain provisional.

## ADR-CI1-03: Direct and indirect impact

Problem: a verified dependency does not prove breakage or required edits, while
combining reachability with direct usage risks an oversized V1.

Alternatives: one combined usages+impact operation; separate versioned operations;
Find Usages only with direct grouping, defer all indirect work.

Evidence: current GetImpactSet mixes edge kinds, affected-tests filters that set,
and neither encodes a proposed change kind or complete occurrence paths.

Advantages: combined answers more questions in one call; separate contracts
distinguish proofs/budgets; deferral provides immediate location value with less risk.

Risks: combined operation conflates direct and derived claims; separate operations
need shared publication identity and some duplicate transport; deferral leaves
blast-radius reconstruction to existing consumers temporarily.

Compatibility: do not alter GetImpactSet/GetAffectedTests semantics.
CI-1A direct groups derive only from returned occurrence rows, with partial counts.

Storage/performance: future impact depth/path/adjacency/proof work can dominate
fact lookup and must have separate quotas/cycle rules. Do not index transitive
closure for this milestone.

Recommend CI-1A alone now; design separate GetChangeImpactV1 for later narrow
incoming-call paths, candidate tests and explicit change kinds.
Owner decides whether CI-1B is separately worthwhile after CI-1A utility evidence;
no automatic inclusion of references/heritage/runtime dispatch.

## ADR-CI1-04: Dispatch, aliases and ambiguity

Problem: static Roslyn binding, runtime receiver selection and unresolved candidates
are different evidence. Same-name search or type heritage can fabricate calls.

Alternatives: expand interface/virtual targets to implementations; return static
bound definition only; emit unresolved candidates as weak "usages"; return them
as separate unknown coverage.

Evidence: HandleInvocation uses bound IMethodSymbol; TryResolveTarget refuses
global ambiguous canonical keys; candidate-only binding can miss counters.
No method implements/override graph relation exists. Alias references unwrap
IAliasSymbol; qualified display alone is insufficient identity.

Advantages: static binding is reproducible/local; runtime expansion offers
potential leads if distinctly labeled; separate unknown coverage avoids false
confirmations. Alias spelling can remain a future facet without changing target.

Risks: static-target results need explicit runtime limits; runtime expansion lacks
whole-program evidence; weak candidates in the positive list encourage false edits;
unit-level counters cannot pretend to identify selected-target sites.

Compatibility: keep existing target normalization/edge semantics; V1 tightens only
its new confirmation gate and persists honest coverage under approved versioning.
Defer local-function/partial-method identity risks rather than change all IDs.

Storage/performance: static facts linear in sites; dispatch closure would multiply
targets and work. Coverage summaries add bounded per-unit/category data. Unknowns
need scope, reason and counts; no invented source location or external node.

Recommend static bound target only, generic/reduced-definition normalization,
explicit interface/virtual facets, no runtime closure, conservative unknowns.
Owner decides error-type rejection, combined generic-extension cases, supported
lexical owners, ambiguity accounting and exact coverage qualification rules.

## ADR-CI1-05: Publication, lifecycle and historical compatibility

Problem: occurrences move/disappear with edits and must publish atomically with
source hashes; latest-only stores and historical codecs cannot silently invent data.

Alternatives: mutate per-file occurrences separately; project-unit replacement
in normal atomic publication; side cache keyed by publication; on-demand source
reconstruction. Side caches may accelerate reads but cannot become unowned authority.

Evidence: C# owns semantic project units; normal staging replaces whole contributions;
conformance preserves shared contributors/failure state. Lattice and snapshot
currently report schema 2 with historical Hetu labels. Bound views fail on movement.

Advantages: normal project replacement aligns binding dependencies and deletion;
side cache can be rebuilt; per-file updates can be cheap where proven independent.

Risks: separate per-file mutation leaves stale cross-project bindings or partial
publication; project replacement churns site IDs/bytes; cache authority complicates
durable parity; snapshots need exact occurrence/coverage preservation.

Compatibility: old graph queries and authentic historical stores remain readable;
new query reports analysis unavailable until reindex. Do not change historical
labels or reopen accepted EC-6D. New codec/schema policy must explicitly govern
old data and unsupported-reader refusal; no silent migration or downgrade promise.

Storage/performance: project-level O(unit facts) replacement/replay plus target
index updates; failed/cancelled work can cost CPU without publication.
Freshness checks are separate from publication ID. Snapshots are explicit history,
not an implicit old-view query fallback.

Recommend normal project-unit staging of occurrences AND coverage, atomic commit,
deterministic file-hash/span keys, fail-on-change reads, preserved old queries,
and a separately approved additive version/codec compatibility route.
Owner decides exact durable/snapshot versioning, legacy loading/reindex behavior,
snapshot limits, semantic invalidation and identity churn acceptance.

## R1 amendment (CI-1-SCOPE-R1)

**ADR-CI1-01 reopened (S01).** The recommendation "Recommend C with B2 as a
serious simpler alternative" is withdrawn. Both C (separate source-owned
occurrence facts) and B2 (new versioned occurrence edge kind) remain qualified
alternatives; no winner is claimed. See
[`../ci1-scope-r1/storage-alternatives.md`](../ci1-scope-r1/storage-alternatives.md)
for the requirement-by-requirement comparison and the minimum comparative
experiment. The storage choice must not be made irreversible during remediation.

**ADR-CI1-04 amended (F01).** "Keep existing target normalization/edge
semantics; V1 tightens only its new confirmation gate" is insufficient on its
own. The V1 confirmation gate must require an **exact eligible target identity**:
proven correspondence between the bound symbol's defining compilation/assembly
and definition and the indexed declaration in the applicable dependency context.
A canonical documentation ID is a lookup aid only. Refuse/qualify when
correspondence is unproven; never copy current global-key resolution into
positive facts. Add the single-indexed-local-versus-external collision to
mandatory acceptance.

**ADR-CI1-05 amended (F02, S02).** The unidentified wider failure is now named: a
documented `sealed partial` type throws `InvalidCastException` in the
inconsistency diagnostic (unsafe `modifiers` cast) after the declaration-specific
`doc-summary` disagreement activates it. This is an explicit prerequisite-or-
narrowing decision ([P-DEF-02](../ci1-scope-r1/prerequisite-defects.md)), not
CI-1A work. Before codec work, name and hash real prior-writer databases/
snapshots, writer package/commit and consumer executable; demonstrate old-query
preservation and `AnalysisUnavailable`/reindex-required for new queries; decide
old-reader refusal explicitly; never fabricate occurrences from old aggregate
edges. Preserve Hetu labels and accepted EC6D contracts.
