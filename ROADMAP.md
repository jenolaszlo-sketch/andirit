# Penghou.Hetu Roadmap

## Future resource-provider integration — 2026-10-02

Reference: [resource-abstractions architecture](../Penghou/docs/resource-abstractions-architecture.md).
After RA-5, assess an optional IO-backed implementation of the existing
repository-source contract. Keep code graph, repository and snapshot semantics
in Hetu; the immediate correction does not migrate this project.

- [ ] Map explicit workspace/view/invocation identities and bounded reads/pages.
  Preserve incomplete and authorized-view coverage: excluded/unscanned resources
  must not become evidence of deletion.
- [ ] **VFS-2/8/9, deferred consumers:** accept proven snapshot, filtered or Git
  views without claiming that a live Local scan is a coherent snapshot.
- [ ] Qualify differences in stream/buffer APIs, entry/file limits, path equality
  and version identity before replacing any current repository provider.

Handoffs cite the canonical RA/VFS gates and the concrete provider guarantees.

## Objective

Build a local-first, language-neutral code knowledge graph that gives Solo and
other .NET consumers deterministic repository comprehension without coupling
the graph model to Roslyn, LatticeDB, or any model provider.

The central invariant is:

> Hetu defines what code means in the graph. Plugins define how those facts are
> discovered.

## Architectural laws

- Extraction is repository-aware and session-oriented. Plugins choose their
  atomic index units rather than inheriting file ownership from the planner.
- Semantic symbols and physical declarations remain separate.
- Repository, snapshot, index-run, plugin, plugin-version, and index-unit
  identities remain distinct.
- Every fact has an owner. Non-trivial relationships carry honest evidence.
- Successful graph publication is atomic, idempotent, and consistent with the
  published source state. When nothing changed since the latest publication,
  indexing reuses it instead of minting a run: the returned publication
  carries the previous run identity, not the requested one. Consumers must
  key their own per-attempt records by their own run identity and use
  `IndexIdentity` (content-addressed) to detect identical source states.
- Persistence and language extraction remain replaceable behind
  provider-neutral contracts.
- Queries are bounded, deterministic, provenance-aware, and explicit about
  ambiguity and truncation.
- Hetu returns attributable facts and candidates. Solo owns interpretation,
  ranking, and context composition.

## Decisions

Recorded choices with the conditions that would trigger revisiting them:

- **Single target framework net10.0.** Sibling packages multi-target
  net8.0-net10.0; revisit when the first external consumer on an LTS
  framework needs Hetu, or at general availability.
- **Lightweight csproj parsing without MSBuild evaluation.** Conditions,
  imports, custom targets, and solution configurations are not interpreted.
  Revisit only as an opt-in provider concern when a real repository fails to
  index correctly because of it.
- **Synthetic `@loose/csharp` project** for sources outside any discovered
  project. Revisit if loose sources prove ambiguous in practice.
- **C# semantic resolution references the host runtime**
  (`TRUSTED_PLATFORM_ASSEMBLIES`), so non-host target frameworks resolve
  against a different BCL. Revisit if unresolved-relationship counts prove
  misleading on cross-target repositories; until then this limitation must be
  documented wherever extraction semantics are.

## Current baseline

Milestones 1 through 7 established the working foundation:

- portable graph identities, vocabulary, properties, evidence, locations,
  extraction sessions, batches, and validation;
- deterministic repository discovery, source hashing, incremental planning,
  bounded indexing, diagnostics, and plugin registration;
- an in-memory graph store and a durable LatticeDB provider sharing a store
  conformance suite;
- a Roslyn C# plugin with provider-neutral SDK-style project discovery,
  declarations, symbols, containment, partial types, overload-safe identities,
  and project dependencies;
- exact symbol and declaration lookup plus bounded provider-neutral traversal
  and impact-query operations;
- run-scoped staging with atomic graph/state publication, failure cleanup, and
  durable restart semantics shared by memory and LatticeDb providers;
- publication-bound query envelopes with node, declaration, and edge
  contributors plus explicit traversal truncation diagnostics;
- deterministic publication receipts and source-state identities, fail-on-change
  publication query views, bounded batch symbol/impact queries, declarations by
  file, and project public-surface queries;
- a cohesive host facade with host-default indexing bounds, caller-supplied
  store ownership, and provider-neutral content-free readiness checks;
- separate indexing and reader contracts, cached in-memory materialization,
  adjacency indexes, and incremental LatticeDb mutation handling;
- native-provider CI, restart and corruption coverage, and recorded persistence
  benchmarks.
- LatticeDb replay distinguishes the latest completed publication from older
  completed run history. Reopening after multiple successful index runs restores
  every immutable run manifest while only the latest run restores the atomic
  repository index state; historical completion is never routed through the
  non-success `StoreIndexRunAsync` path.

This section is intentionally a summary rather than a historical delivery log.
The tests, public API snapshots, README, and Git history are authoritative for
already completed work.

## Marang Gate 0.5 / Batch 4 handoff

The current `0.2.0-preview.5` baseline is usable as a bounded, provider-neutral
graph source for Marang. This is an integration boundary and audit record; it
does not make Marang workflow semantics part of Hetu.

### Usable now in Hetu

- Stable repository, publication, and source-state identities, including the
  deterministic `CodeIndexIdentity`; symbol, node, and physical declaration
  identities are separate and stable (`CodeIndexIdentity.cs`,
  `CodeIdentities.cs`, `CodeIndexIdentityTests.cs`).
- Bounded exact lookup, declarations-in-file, public-surface, neighborhood,
  dependency/dependent, caller/callee, implementation, and incoming impact
  traversals. `CodeGraphQueryEnvelope<T>` binds results to the publication,
  applied query, and contributor provenance (`CodeGraphQueryService.cs`,
  `CodeGraphPublicationQuery.cs`).
- Contributor provenance through plugin/version/index-unit/source ownership,
  explicit ambiguity and traversal truncation, and source drift detection at
  the planning/extraction boundary (`CodeGraphFacts.cs`,
  `CodeIndexingLifecycle.cs`).
- Run-scoped staging, atomic publication, bounded plugin concurrency, and
  durable LatticeDB restart/recovery with corruption and interrupted-transaction
  coverage (`InMemoryCodeGraphStore.cs`, `LatticeCodeGraphStore.cs`, and their
  lifecycle/provider tests).

### Reusable upstream follow-ups

These should remain provider-neutral Hetu work, ordered as Gate 0.5 risks:

1. **P1 — Immutable historical graph snapshots or export references
   (landed on `feature/latticedb-provider`).** A publication-bound view fails
   when the latest publication moves, but Hetu retains only the latest graph.
   `CodePublicationSnapshot` now captures one publication as bounded,
   schema-versioned, SHA-256 integrity-checked units that re-import through
   the normal staging path, so a later query reproduces the original result.
   Serialized transport (index-in-CI, query-locally) remains Tier C
   follow-up work.
2. **P1 — Repository/workspace revisions and freshness semantics (freshness
   landed on `feature/latticedb-provider`).** `CodeIndexingService.CheckFreshnessAsync`
   compares live sources with the latest published state and reports
   unknown/fresh/stale/source-conflict with per-status counts, bound to the
   compared publication, without staging or publishing anything. Working
   revisions atop a pinned publication remain workspace-experiment work (see
   the [workspace experiment](docs/workspaces-design.md)).
3. **P1 — Affected-test query (landed on `feature/latticedb-provider`).**
   The C# plugin marks exact allowlisted test-framework methods with a
   `test-method` property (never inferred), and `GetAffectedTestsAsync`
   derives per-seed bounded test sets from incoming calls/references on both
   the service and publication-bound query surfaces.
4. **P1 — Same-repository concurrent publication ordering (landed on
   `feature/latticedb-provider`).** Mutations stay serialized, and completion
   is now optimistic: a run records the publication it planned against at
   registration, and completing after another publication lands fails
   explicitly instead of silently winning. Retries use a new run id against
   the latest publication. History replay restores without ordering checks
   and re-anchors resumed runs, so reopen never manufactures conflicts.

Explicit path/shortest-path queries and changed-symbol convenience methods can
remain later follow-ups; Marang can compose current traversals in the interim.

### Marang-owned adapter boundary

Workflow/task/node ownership mapping and `SupervisorContextPackage` shaping,
ranking, redaction, and final context budgets remain Marang responsibilities.
The adapter should use Hetu's publication, typed fact identities, locations,
evidence, query descriptor, and contributor provenance as its references rather
than adding workflow IDs to the Hetu graph model.

### Security and resource notes

Hetu bounds source enumeration/materialization, ingestion batches, and each
query, but there is no global retained-graph or LatticeDb replay cap. Marang must
apply a total context budget and treat large or highly connected results as
truncated. Query envelopes may expose repository-relative paths, symbol names,
properties, and documentation summaries; adapter output must apply its own
redaction and size policy. Source blobs are not persisted by the graph store.

## Hongxian optional code-memory boundary

Status: **design gate recorded; implementation deferred while Fuwen is the
active priority**

Hongxian sessions must remain fully usable without Hetu. The current source
dependency audit confirms that neither `Penghou.Hongxian` nor
`Penghou.Hongxian.Sqlite` has a Hetu, parser, graph-store, or Roslyn runtime
dependency. Its public-API analyzer is build-only with `PrivateAssets=all`;
future packed-consumer tests must prove that it does not flow transitively.

The responsibility split is fixed:

- Hongxian owns session continuity, immutable evidence, and opaque correlation.
- Hetu owns normalized code facts, language capabilities, publication identity,
  freshness, and code-graph queries.
- An optional integration adapter may correlate one immutable session resource
  revision with one exact Hetu publication. It must not copy graph nodes/edges
  into Hongxian or add session/workflow identities to Hetu's graph model.

Before Hetu multi-repository federation, structural publication deltas, Marang
MCP exposure, parser evaluation, or delegated-work synchronization through
Hetu:

- [ ] Prove an isolated Hongxian consumer has no transitive Hetu, Roslyn, ANTLR,
  or LatticeDB dependencies.
- [ ] Decide the optional integration package's repository ownership without
  changing dependency direction. `Penghou.Hongxian.Hetu` may depend on both
  sides; Hongxian core/storage and Hetu core must not depend on it.
- [ ] Define exact revision/publication correlation, idempotent synchronization,
  freshness, concurrent publication ordering, superseding revisions, late
  completion, and forward reconciliation after partial failure.
- [ ] Keep zero adapters valid. Capability absence is discovered explicitly;
  it is not represented by a mandatory no-op provider or an adapter-generated
  `NotConfigured` result.
- [ ] Preserve parser-private implementations, normalized facts, replaceable
  graph persistence, domain-specific bounded queries, deterministic language
  registration, and explicit installed-language/provider/store capabilities.
- [ ] Add an enabled-path acceptance test covering R1 -> H1, R2 -> H2, exact
  publication evidence, synchronization failure without session rollback, and
  removal of the optional adapter without loss of generic session behavior.

The integration host owns resource mapping, credentials, filesystem/network
authority, bounds, and redaction. Opaque session metadata is correlation data,
not authority to open a repository or activate a provider. No generic graph,
universal parser, dynamic plugin loader, MEF composition, or MCP surface belongs
in this boundary milestone.

## Post-boundary proving sequence — shared workspaces and structural catch-up

Status: **planned after Fuwen is usable and the Hongxian optional-capability
boundary is proven**

The product hypothesis is that Hetu can reduce the supervisor's catch-up cost
after delegated coding work. Validate that hypothesis in Marang before growing
Hetu into a broad federation platform.

### Gate A — reproducible repository publications

The current store retains historical run manifests but restores only the latest
graph. A workspace manifest containing old publication IDs is therefore not a
reproducible structural snapshot by itself.

- [ ] Complete the existing immutable historical graph snapshot/export-reference
  work before claiming reproducible workspace publications or deltas.
- [ ] Bind each retained graph state to its repository identity, exact source
  state, plugin/version set, fact vocabulary/schema, and integrity metadata.
- [ ] Define retention failure explicitly: a workspace publication whose member
  graph is unavailable is identifiable but not queryable and must never fall
  through to the repository's latest graph.

### Gate B — immutable multi-repository composition

- [ ] Add stable workspace identity and a canonical immutable publication
  manifest containing an ordered mapping from repository identity to exact
  repository publication. Do not flatten repositories into one anonymous graph.
- [ ] Make creation idempotent and content-addressed, reject duplicate repository
  identities and mismatched source state, and preserve predecessor/lineage
  without using wall-clock order as authority.
- [ ] Keep composition separate from federation. The first workspace publication
  may provide deterministic membership and per-repository query routing without
  claiming cross-repository symbol resolution.
- [ ] Add explicit branch/candidate lineage. Parallel workers produce separate
  candidate publications; an integration or promotion step creates a new
  publication. Do not pretend independently edited branches automatically form
  one later workspace state.

### Gate C — bounded structural change

- [ ] Define a raw `GraphChangeSet` over two available immutable publications:
  added/removed/stably identified changed facts and edges, with contributor and
  evidence changes preserved. Define “changed” precisely rather than comparing
  display names or mutable locations.
- [ ] Keep factual graph delta separate from derived impact analysis. Impacted
  nodes/repositories require their own query descriptor, bounds, truncation,
  evidence, and algorithm/version identity.
- [ ] Bound or page every delta dimension and provide deterministic summaries so
  a large rename or regenerated project cannot exhaust MCP or model context.
- [ ] Distinguish unavailable history, incompatible schema/plugin semantics,
  ambiguous identity, and source drift from a valid empty change set.

### Gate D — Marang/Codex evaluation

- [ ] Expose only a minimal, authorization-scoped, bounded code-query and
  structural-catch-up surface through Marang. Hetu does not acquire an MCP host.
- [ ] Compare paired tasks pinned to the same source/publication state, recording
  source reads, searches, relevant relationships found/missed, false facts,
  elapsed time, and observable token use. Treat token measurements as noisy.
- [ ] Use independently reviewable expected facts or labeled fixtures for parser
  precision/recall. Codex may discover discrepancies but is not the correctness
  oracle; confirmed defects become regression tests.
- [ ] Stop or narrow the feature if structural catch-up does not materially
  reduce source inspection or if false relationships increase review risk.

Cross-repository resolution follows composition only when a concrete query needs
it. Any relationship across repositories requires evidence such as an explicit
project/package/workspace mapping and exact version provenance; a package
version must not silently resolve to an unrelated working-tree checkout.

## Milestone 7.5 — useful semantic relationships

Status: **complete in `0.2.0-preview.5`**. Implementation was tracked in
[docs/milestone-7.5-execution-plan.md](docs/milestone-7.5-execution-plan.md);
all five phases landed with regression coverage.

The query surface exists, and the C# plugin now emits the relationships that
make comprehension and impact analysis useful, using uniquely resolved Roslyn
symbols:

- inheritance and interface implementation, classified without transitive
  closure (`Inherits` for interface extension and class extension,
  `Implements` for class/struct interface adoption);
- semantic calls (ordinary, static, extension, generic, interface-dispatched,
  constructors, implicit creation, `this(...)`/`base(...)`) and references
  with smallest-owner attribution, local-function owners, and attribute
  targets;
- import relationships preserving alias, `static`, `global`, and
  file/namespace/project scope metadata;
- return and accepted-parameter relationships remain deliberately
  `NotProduced`: the cross-language meaning is not precise enough to publish
  without guessing;
- stable diagnostics for unresolved, ambiguous, external, unsupported, and
  deliberately omitted targets — externals are counted, never emitted.

Do not guess targets. Syntax-only observations must not be labeled semantic.
Full MSBuild evaluation remains an optional future provider concern rather than
a requirement of the lightweight C# plugin.

Index-coverage metadata lets a consumer distinguish “no relationships exist”
(`Produced`, zero edges) from “deliberately not produced” (`NotProduced`),
“could not be determined” (`Unavailable`), and “partially resolved”
(`Partial`). Run-wide entries carry candidate/internal/cross-project/external/
ambiguous/unsupported breakdowns, and every project index unit reports its own
per-kind entries.

Tier-A feature candidates below intentionally rode along with this extraction
pass: they decorate symbols the plugin already emits.

Exit criteria:

- cross-file and cross-project callers, callees, references, inheritance, and
  implementations are queryable — done, with internal/cross-project split;
- partial types, overloads, generics, extension methods, and interface dispatch
  have explicit regression coverage — done;
- unresolved or ambiguous targets never create guessed edges — done, with
  `AmbiguousTargets`/`ExternalTargets` counts to prove it;
- repeat and incremental extraction remain deterministic — done, including
  coverage determinism and stale-relationship removal on rebind;
- coverage/capability metadata accurately describes the published index —
  done (Phase 4 contract with per-unit entries);
- the store conformance suite gains relationship-kind checks alongside plugin
  tests — done (`traversal-semantic-kind-filter` on both providers);
- indexing a repository of Roslyn-solution size completes within the configured
  per-source and total byte budgets — bounded chunked batch delivery is proven,
  and an extraction-throughput baseline now exists
  (`ExtractionBenchmarks` in `BENCHMARKS.md`, 100 and 1,000 in-memory files);
  a CI byte-budget assertion still remains open.

## Feature candidates

Candidates are promoted into a numbered milestone when scheduled. None of them
changes the architectural laws. Effort: S (days), M (weeks), L (longer).

### Tier A — ride along with Milestone 7.5

- **Documentation-comment extraction (S, landed in `0.2.0-preview.5`)** —
  `<summary>` and `<remarks>` text on symbol nodes as bounded properties
  (`doc-summary`, `doc-remarks`, 512 chars each).
- **Modifier/attribute properties (S, landed in `0.2.0-preview.5`)** —
  `static/virtual/abstract/sealed`, access level, `[Obsolete]`, exact
  test-framework allowlist (`test-method`), and exact ASP.NET Core
  route/verb allowlist (`http-endpoint` plus bounded `route-template`).
  Unlocks public-surface, obsolete-member, test-filtering, and endpoint
  queries without turning arbitrary attribute payloads into an unbounded
  property channel.
- **Literal values for enums/constants (S, landed)** — constant field values
  as bounded `constant-value` properties. Scope clarification from the
  [language audit](docs/language-feature-matrix.md): the current declaration
  selector does not emit enum-member nodes; this historical item must not be
  read as verified enum-member/value coverage.
- **Package-reference nodes (M, landed)** — `PackageReference` items become bounded
  syntax-evidence external dependency nodes with version and unexpanded
  condition metadata; do not claim evaluated MSBuild semantics.
- **Solution-file scoping (M, landed in `0.2.0-preview.5`)** — `.sln` and
  `.slnx` define the canonical project set instead of directory-walk
  inference. Configurations, solution folders, and nested-project sections
  are not evaluated; their presence is reported as
  `csharp.solution.has-configurations`, `has-solution-folders`, and
  `has-nested-projects` diagnostics. Project-reference edges remain the
  primary build-order evidence.

### Tier B — query surface

- **Name-pattern candidate search (M, landed)** — bounded prefix/pattern
  candidates with explicit truncation; Solo ranks results.

### Milestone 8A — dogfood query ergonomics (complete)

Status: **complete on `main` (unreleased)**. All five query additions landed
and are exercised by provider-neutral tests and the C#/Elixir dogfood
(`tests/Penghou.Hetu.Tests/CodeGraphDiscoveryTests.cs`,
`tests/Penghou.Hetu.TreeSitter.Tests/QueryDogfoodTests.cs`).

Composable, store-neutral query additions proven against C# and Elixir
graphs before TypeScript/JavaScript strains the extraction seam:

- bounded name/prefix candidate search (already landed as Tier B above);
- deterministic symbol discovery with language, kind, container, and
  project scoping plus explicit truncation;
- source-location to enclosing-symbol lookup bridging diffs into graph
  identities;
- symbols and declarations by file or project;
- C# and Elixir dogfood plus recorded evidence on the traversal-policy
  versus answer-filter question (see the evidence-quality ADR).

Bounded source excerpts remain deferred: hash validation and snapshot
mismatch detection pull harder on repository semantics than the query
additions above.

### Tier C — strategic

- **Publication snapshot export/import (M, landed on
  `feature/latticedb-provider`)** — store-agnostic, bounded,
  schema-versioned serialized publications with integrity hashes and explicit
  compatibility rules; enables index-in-CI, query-locally workflows and makes
  LatticeDb optional for read-only consumers. `ToJson`/`FromJson` transport
  re-verifies schema, consistency, bounds, and hash; corrupt payloads fail
  explicitly.
- **Test-to-production mapping (M, landed)** — the C# plugin marks projects
  referencing an exact test-framework package allowlist with a `test-project`
  property and derives one bounded `exercised-by` edge from each resolved
  production target to the allowlisted test method that calls it, so impact
  sets include the tests to run. Only resolved calls map; externals and
  ambiguous callees stay unemitted.
- **Entry-point/route heuristics (M)** — `Main`, controller actions,
  minimal-API endpoints as heuristic-evidence nodes; must respect the
  honest-evidence law.
- **TODO/FIXME comment nodes (S)** — linked to their containing symbol for
  graph-addressable cleanup tracking.

### Explicitly not now

Textual query language, SCIP/LSIF export, and embeddings or vector search remain
out of scope. Multi-repository composition and narrowly evidenced federation
follow only the post-boundary proving sequence above; do not fold them into the
semantic-relationship milestone.

## Milestone 8 — dogfood with Solo

Milestone 7.5 is complete, so this milestone is unblocked. It will
be designed and implemented together with Solo rather than completed
speculatively.

Index Hetu and the other Penghou repositories, then integrate read-only code
comprehension into Solo for:

- resolving relevant declarations for a planning task;
- selecting bounded callers, callees, implementations, and dependencies;
- estimating an impact neighborhood before editing;
- recording the exact graph publication and query used for context selection;
- optionally persisting selected graph observations or references in Cangjie.

Memory/experience boundary:

- Hetu remains authoritative only for code facts at an exact publication and
  declared extraction-evidence level. It does not store participant beliefs,
  workflow experience, model reputation, or procedural knowledge.
- Cangjie may relate attributed diary entries to stable Hetu publication/node
  references; Hongxian may record which publication/query influenced an
  execution. Neither copies or silently upgrades a code observation into a
  stronger fact.
- Marang/Guyabano compose bounded code context with working memory and prior
  experience. Hetu supplies query results, evidence, bounds, and freshness, not
  planning or checkpoint decisions.

During dogfooding, evaluate these usability additions from real workflows:

- deterministic symbol discovery scoped by language, node kind, project, or
  containing symbol;
- canonical symbol-key lookup and source-location-to-enclosing-symbol lookup;
- declarations and symbols by file or project;
- bounded, hash-validated source excerpts with snapshot mismatch detection;
- a cohesive host facade for indexing and querying, with typed C# and LatticeDb
  registration helpers.

### Transactional workspace experiment

Evaluate Hetu as a deterministic model of code Solo is actively constructing,
not only as an index of the last committed repository. Keep this experimental
until the read-only integration proves which semantics are genuinely useful.

Start with the smallest vertical slice:

```text
published repository graph
        -> begin workspace pinned to one publication and validated source view
        -> replace one existing C# source in memory
        -> refresh the affected project graph
        -> run bounded provenance-aware queries
        -> discard the workspace
```

The workspace view must be implemented as a repository overlay. Added and
modified sources shadow the base repository, deleted sources disappear, and
unchanged sources continue to come from the pinned base snapshot. Language
plugins consume the resulting `ICodeRepositorySource` view without learning
whether content came from disk, a VFS, or a workspace edit. Do not create
temporary project files or introduce filesystem assumptions.

Pinning a graph publication is insufficient when its repository provider is a
live filesystem. Beginning and refreshing a workspace must also validate the
base source manifests and hashes, or open a provider-defined immutable source
snapshot. A mismatch fails explicitly rather than combining facts from one
publication with later source bytes.

If the first slice proves useful, extend it in the staged order documented in
[docs/workspaces-design.md](docs/workspaces-design.md), which also holds the
revision record, graph-diff semantics, and the workspace/source-persistence
separation. Summary of that design: revisions are immutable and append-only
with a movable logical head; rollback moves the head rather than reversing
graph mutations; CodeGraphDiff is a first-class derived result between
publications or revisions; source blobs stay out of `ICodeGraphStore` behind a
future `ICodeWorkspaceStore` whose recovered workloads revalidate their pinned
base publication.

`BasePublicationId` refers to Hetu's existing successfully published
`CodeIndexRunId`; it does not introduce a parallel repository-publication
identity. A workspace revision uses its own typed `WorkspaceRevisionId`, which
also identifies the atomically refreshed working graph for that revision.

The working graph must remain separate from the repository's last successful
published graph. Refresh should atomically publish a workspace revision or
leave its prior working graph intact. Partially edited or temporarily invalid
code is acceptable: preserve valid structural facts, report compiler
diagnostics and unresolved relationships, and never promote guessed semantic
edges.

Important boundaries:

- beginning or refreshing a workspace never mutates the published repository
  graph;
- every operation is bounded and cancellation-aware;
- diagnostics remain source-content-free by default;
- disposal, expiry, and abandoned-workspace cleanup are host-triggerable;
- graph diff and rollback operate on immutable revisions;
- Git integration and applying edits to the real repository remain host/Solo
  responsibilities;
- incremental Roslyn compilation reuse is an optimization behind an optional
  capability, not a requirement imposed on every language plugin.

Workspace experiment exit criteria:

- a modified source can be queried semantically without changing disk or the
  published graph;
- discard restores the exact published view with no cleanup ambiguity;
- add/delete and incomplete-code behavior are deterministic and tested;
- workspace query envelopes identify both the base publication and workspace
  revision;
- measurements justify whether checkpoints, persistence, graph diff, and
  incremental plugin sessions should become supported product contracts.

Avoid fuzzy symbol selection in Hetu core. Return explicit candidates and let
Solo rank them.

Before leaving this milestone, review the vocabulary and plugin contract
against real C# and at least two structurally different non-C# languages
before generator work. Elixir plus TypeScript/JavaScript satisfy this if the
extraction-profile seam survives; no specific further language is required
without a consumer use case.

## Milestone 8B — Language Feature Catalog and Phased Language Expansion

Status: **design documented; extraction improvements and new-language plugins
remain future work**. The initial catalog is an audit, not a runtime capability
release. Milestone 8A remains complete; the Solo dogfood and Marang integration
gates retain their priority and are not expanded or marked complete here.

Phase 0 ECMAScript milestone status (2026-10-23): EC-3D closed **VERIFIED WITH
QUALIFICATIONS** (heritage target identity correction independently verified),
EC-4 closed **USEFUL WITH QUALIFICATIONS** (real-repository qualification with
source-grounded verification), and EC-5 closed **VERIFIED WITH QUALIFICATIONS**
(file-level variable declarations independently qualified by GLM; 21/21 website
declarations, 11/11 directly exported, 18/18 adversarial probes, authentic
p5→p6 migration, unchanged pre-existing invocation facts). EC-6B relative
module resolution implementation is complete and **ready for independent
review**; it has not been independently qualified. The authentic website
p6→p7 migration and full solution suite are recorded in
`docs/evidence/ec6b-relative-module-resolution-handoff.md`. Export/import
binding and Astro (`.astro`) support remain future work. Closure records,
preserved review bindings, and accepted limitations are in
`docs/evidence/ec5-closure.md` and the
[language roadmap](docs/language-support-roadmap.md#ecmascript-milestone-status-updated-2026-10-25).

Objective: establish a maintainable, evidence-based feature inventory before
adding the next language. The approved strategy is in the
[language support roadmap](docs/language-support-roadmap.md); the permanent
[feature catalog and audited matrix](docs/language-feature-matrix.md) records
applicability, graph mappings, actual extraction, evidence and verification gaps.

Phase 0 establishes that foundation across C#, TypeScript, JavaScript and Elixir.
Prioritize TS/JS declaration/member/module coverage and honest capability
reporting, preserve C# semantic extraction and its documented limitations, and
maintain Elixir structural support. Useful mappings and feature-level verification
expectations must be recorded before another language is added.

Expansion order, subject to a recorded consumer-driven reprioritization:

1. Phase 1: Python, then Java; useful structural extraction first, semantic
   enrichment as separate feature-specific milestones.
2. Phase 2: Go, then Rust; explicit embedding/receiver/trait/implementation-block
   semantics and honest implicit-interface and macro boundaries.
3. Phase 3: Kotlin, including extensions, properties, companions, top-level
   declarations and sealed types, reusing suitable Java concepts.

The nine-language target includes the four existing languages. The five planned
languages have no supported plugin yet; there are no dates or release promises.
Deferred candidates, separate SQL/shell consideration, and parked Erlang are
recorded in the language roadmap, not promoted to commitments here.

Capability and evidence requirements:

- Preserve `CodeGraphCapabilities`, extensible kinds/properties, and
  `CodeRelationshipCoverage`/`CodeRelationshipCoverageState`; feature inventory
  complements broad capabilities and actual published coverage.
- Keep applicability, representability, extraction and evidence independent.
  `Resolved`, `Structural`, `Derived` and `Inferred` are classifications, not a
  confidence ranking; no syntax/name match can create a resolved call.
- Define future catalog/version/plugin/grammar/dialect/effective-capability
  binding to an exact publication. Upgrading a plugin cannot upgrade old graphs'
  capability claims. Persistence design and implementation remain future work.

Documentation deliverables: the two linked design documents, README support
summary, this milestone, and targeted consistency/cross-reference corrections.

Exit criteria for the foundation and subsequent separately implemented support:

- The catalog has stable IDs, useful graph mappings, explicit unresolved choices,
  applicability for planned languages and an audited four-language baseline.
- Phase 0 gaps have scoped consumer questions, prioritized changes and fixture
  expectations; parser recognition and planned support are never called verified.
- Every subsequently advertised feature has deterministic positive/negative
  fixtures for nodes, declarations, edges, properties, evidence, partial and
  ambiguous cases, repeat extraction and stale-fact removal. Automated feature
  conformance is future work, not delivered by these documents.
- Actual plugin capabilities, emitted facts and publication coverage remain
  attributable and honest, including absent/partial/unavailable results.
- Expansion preserves the architectural laws and broader Solo/Marang priorities;
  vocabulary changes are justified by useful repository questions.

Explicit non-goals of this design task: parser/grammar additions, new extractors
or semantic providers, public APIs or a feature-ID enum, vocabulary migrations,
storage schemas, catalog loading/persistence, runtime capability changes,
extractor refactoring, or a new test framework. Generated plugins remain reserved
future work, not a prerequisite or a newly delivered capability.

## Later opportunities

### Deterministic architectural analysis

Build primitives proven useful during Solo integration:

- highly connected symbols and bounded dependency neighborhoods;
- cross-project coupling and test relationships;
- component candidates and change-impact summaries.

Component, subsystem, and feature labels do not belong in core until their
semantics and evidence requirements are proven.

Workspace graph comparison is a likely consumer of these primitives, but
architectural scoring remains derived analysis rather than workspace mutation
logic. Candidate comparisons must report their evidence and bounds rather than
claiming one implementation is universally better.

### Generated language plugins

Reserve `Penghou.Hetu.Generator` for deterministic plugin generation from
grammars and declarative mappings. AI may help author those inputs, but runtime
AI parsing and arbitrary generated runtime code remain out of scope.

TypeScript/JavaScript landed as the second structural provider in the
`Penghou.Hetu.TreeSitter` package (Phase 3B), with a concrete consumer in the
Astro/TypeScript `website` repository. EC-5 records named file-level
TypeScript/JavaScript variables as `field` declarations with direct written
`export` metadata; it does not model export bindings, local variables, ambient
`declare` declarations, or resolved types, and it reuses the generic `field`
node kind with ownership and declaration metadata distinguishing module
variables from class members. The exact revision and real-repository evidence
are recorded in `docs/evidence/ec5-variable-declarations-handoff.md`; the
independent qualification and closure disposition are recorded in
`docs/evidence/ec5-closure.md`.
`Penghou.Hetu.Generator` remains reserved for later. The approved expansion
sequence is recorded in
[Milestone 8B](#milestone-8b--language-feature-catalog-and-phased-language-expansion)
and the [language roadmap](docs/language-support-roadmap.md). It does not require
generator work or select parser technology. New plugins must validate the shared
vocabulary rather than copy C# semantics mechanically.

## Engineering health

Package validation and per-package public-API contract tests are part of the
current build. Coverage reporting with enforced thresholds and benchmark CI
integration remain engineering follow-ups before a stable first release.

## Explicit non-goals for the first release

- a universal compiler or giant universal AST;
- exhaustive data-flow analysis;
- embeddings or vector search;
- runtime AI parsing, architectural labeling, or plugin generation;
- automatic plugin or NuGet downloading;
- distributed graph persistence;
- provider query languages in ordinary consumer APIs.

## First-release acceptance criteria

1. Public abstractions contain no Roslyn, LatticeDB, parser, or AI dependency.
2. Stable symbols and physical declarations remain separate.
3. Every graph fact has extraction ownership and honest evidence.
4. A successful index publication atomically updates graph facts, run status,
   source state, and query-visible provenance.
5. Failed and cancelled runs cannot alter the last successful graph snapshot.
6. Memory and LatticeDb pass the same conformance suite.
7. C# extraction provides useful semantic calls, references, inheritance, and
   implementation relationships without guessed targets.
8. Queries are deterministic, bounded, attributable, and explicit about
   ambiguity, index coverage, and truncation.
9. Repeat and incremental indexing do not create duplicate graph entities.
10. Solo can assemble explainable, snapshot-validated code context without
    depending on Roslyn or LatticeDB types.
11. A future deterministic non-C# plugin can implement the public extraction
    contract without changing Hetu core.
12. Documentation quick-start samples compile against the public packages.

## Durable store schema evolution

`LatticeCodeGraphStore.CurrentSchemaVersion` is validated on open and a
mismatch is rejected rather than migrated. There are no in-place upgrades in
preview: a schema change ships with a schema-versioned export/import path
(store-agnostic, bounded, integrity-hashed, per Tier C publication snapshot
export/import) so hosts re-index or import instead of running mixed-version
binaries against one file. The health check reports the on-disk version to
make the mismatch actionable before any write is attempted.

## V2 — Evidence-driven workflow evolution (deferred)

Status: **future work after V1; not a current release gate**. Added 2026-09-26.
V2.1/V2.2/V2.3 name cross-project delivery stages, not package or IR versions.
Existing near-term priorities and completed work retain their current status.

Architecture and shared acceptance gates: [reviewed V2 specification](../Penghou.Guihua/docs/evidence-driven-workflow-evolution-v2.md).
Cross-repository links assume sibling checkouts.

### V2.1/V2.2 — Revision-bound context and impact evidence

- [ ] Supply exact repository/publication, query/version, source-revision and
  extraction-strength references when a host evaluates a repair or candidate.
  Preserve bounded traversal, truncation, stale/missing publication and
  unsupported-capability diagnostics.
- [ ] Allow the host to compare candidate publications and retain which code
  observations informed evaluation or a transition preview, without publishing
  an unselected candidate as the accepted workspace.
- [ ] Make incomplete structural coverage explicit. Hetu impact neighborhoods
  may inform application policy but never replace Fuwen/Zhinu data, control,
  validation or external-effect dependency analysis.

Gate: a candidate/repair explanation is reproducible at its publication, while
missing or truncated graph coverage cannot assert that unrelated workflow work
is safe to reuse.

### V2.3 and later — Context drift inputs

- [ ] Expose publication changes as evidence for host-controlled reconsideration
  of a contextual preference. Code similarity alone must not establish
  equivalent tasks, causal improvement or authority to reuse an artifact.

This extends existing publication/query seams only when the consumer needs
them. Hetu does not store workflow reputation, choose winners or learn topology.
