# EC6-GLM-AR1 — Independent architecture review of EC-6A cross-file resolution

**Project:** Penghou.Hetu (future Andirit)
**Review ID:** EC6-GLM-AR1
**Reviewer:** GLM (independent of the design author)
**Design author:** DeepSeek
**Review type:** Independent adversarial architecture and contract review
**Review date:** 2026-10-24
**Environment:** Windows 11 Home Single Language 10.0.26200 x64, .NET SDK 10.0.401, git 2.55.0.windows.3, `dotnet test` on the pinned grammars shipped in the repository.

---

## A. Git binding

| Item | Identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Penghou.Hetu` |
| Qualified implementation baseline | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018`), canonical `main`, verified unchanged during the review |
| Design branch | `design/ec6a-cross-file-resolution` |
| Design commits | `99da4d433b72b135b70138597532c4a93853dc86` (architecture) → `b46334d3efd1ce9a2da81533675146aa1a3ca170` (adversarial cases) → `d87ab9225d653b9440160e78c816cc947da6230e` (roadmap) → `aa4031cd14b6ed8b7089c9471648a8d9d677f782` (decision register, tip) |
| Design tip tree | `fa5efdef1b3694b12f40de0c4d6b81447fcd5857` |
| Design content | exactly 4 files under `docs/design/`, 2,094 inserted lines, no `src/` or `tests/` changes (verified `git diff --name-status 558f8ac..aa4031c`) |
| Review worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6-ar1` |
| Review branch | `review/ec6-glm-ar1`, created from exactly `aa4031c` |
| Ancestry | `558f8ac` → `99da4d4` → `b46334d` → `d87ab92` → `aa4031c`, verified linear |

Isolation: no pull, rebase, reset, push, or merge was performed on any branch;
canonical `main` and the independent review evidence branches were not
touched; the design documents were not modified. Review-only additions are
this report, the decision reconciliation, and one marked probe file.

## B. Existing-contract verification (AR1-A)

Every load-bearing claim of the design was re-verified against source before
being accepted. Results below distinguish **verified** (source read in this
review), **executed** (disposable probe), and **assumption** (design claim
with no current runtime counterpart).

### B1. Verified runtime capabilities the design may rely on

1. **The session sees the plugin's complete current source set**, including
   unchanged files, each with path, content hash, and a repeatable reader
   (`src/Penghou.Hetu/CodeIndexingLifecycle.cs:816-836`; context contract
   `src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs:5-89`). It does
   **not** see other plugins' sources — the design's per-plugin scope (D-18)
   is the only scope the runtime offers.
2. **A plugin executes iff at least one of its plan items is not
   `Unchanged`; unchanged means same plugin version and same source hash**
   (`CodeIndexingLifecycle.cs:359-363`; `src/Penghou.Hetu/CodeIndexPlanner.cs:204-208`).
   When it executes it re-extracts **all** of its sources
   (`src/Penghou.Hetu.TreeSitter/TreeSitterExtractionSession.cs:74-161`).
3. **Staging is ordered and order-sensitive.** Each unit is validated against
   the prospective graph (previous publication − staged deletions + staged
   replacements + the unit itself) when it is staged
   (`src/Penghou.Hetu/InMemoryCodeGraphStore.cs:287-299, 661-671`); an edge
   whose endpoint is absent is rejected with `graph.edge.endpoint-missing`
   (`InMemoryCodeGraphStore.cs:913-929`). **Executed:** probe P1.
4. **Deletions are prospectively validated and rejected while any surviving
   unit still references the target** (`InMemoryCodeGraphStore.cs:358-390`;
   existing test `StageIndexUnitDeletionAsync_RejectsDanglingReferences`).
   **Executed:** probe P3 (both orders).
5. **Unit replacement is whole-unit** (`CodeGraphStoreModels.cs:142-168`);
   replacing the resolution unit removes exactly its previous facts and
   nothing else. **Executed:** probe P2 (including empty replacement).
6. **Staged facts are invisible until atomic completion**; failed and
   cancelled runs retain the previous publication
   (`src/Penghou.Hetu/ICodeGraphStore.cs:27-35`; conformance checks
   `staged-replacement-invisible`, `failed-run-discards-staged-graph`,
   `cancelled-run-discards-staged-graph`, `failed-replacement-atomic` in
   `src/Penghou.Hetu.Testing/CodeGraphStoreConformanceSuite.cs`). **Executed:**
   probe P6 (store level).
7. **Lifecycle ordering is plugin-execution → obsolete-unit deletions →
   hygiene deletions → atomic completion**
   (`CodeIndexingLifecycle.cs:440-677, 681-696, 716-731, 732`). This is the
   exact order the design's deletion scenario (F2) requires.
8. **Unchanged reindex is a no-op returning the previous publication**
   (`CodeIndexingLifecycle.cs:364-385`; test
   `CodeIndexingServiceTests` asserts the previous run id is returned).
9. **LatticeDb delegates staging/validation to the same inner semantics and
   commits in one native transaction** (`src/Penghou.Hetu.LatticeDb/LatticeCodeGraphStore.cs:167-171, 452, 503-517`); no store schema change is
   needed for new edge kinds/properties (kinds are opaque validated strings;
   snapshot schema v2 unchanged).
10. **Edge identity excludes properties; edge equality includes them**
    (`src/Penghou.Hetu.Abstractions/CodeGraphFacts.cs:79-93`;
    `src/Penghou.Hetu/GraphFactEquality.cs:25-31`). Two contributions of one
    edge id with different properties conflict (`graph.edge.conflict`);
    logically distinct relationships between the same endpoints coexist when
    identity (discriminator + span) differs. **Executed:** probe P4.
11. **Identity scopes match the design's matrix** (verified in
    `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs`): only
    `class` (line 127) and `interface` (line 147) are `fileScoped`; `type`
    (156), `enum` (159), `namespace` (164), `callable` (176) are name-based
    (`identity = fileScoped ? path\nqualified : qualified`, line 638).
    File-level variables are path-scoped (`"variable\n" + path + "\n" +
    name`, lines 334-336). **Executed:** probe P5.
12. **Traversal presets are closed lists** that exclude `imports` and would
    exclude any new kind until explicitly added
    (`src/Penghou.Hetu/CodeGraphTraversalPresets.cs:10-23`).
13. **Public API contract tests snapshot exported types, not members**
    (`tests/Penghou.Hetu.Tests/PublicApiContractTests.cs:11-66`), so additive
    `CodeEdgeKinds`/`CodePropertyKeys` constants do not change the public-API
    snapshots.
14. **Coverage arithmetic is strict**: `EdgesEmitted ==
    InternalTargets + CrossProjectTargets`, non-produced states report no
    counts, produced kinds appear exactly once
    (`src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs:193-220, 308-318`).

### B2. Design assumptions with no current runtime counterpart

These are **proposals**, correctly marked as such by the design; the review
confirms none of them exists today and all of them are implementable within
the verified capabilities above:

- Module-shape capture and a resolution pass inside the Tree-sitter session
  (new internal code in the TreeSitter package).
- The `{language}:modules` resolution unit (new plugin-owned unit).
- `resolves-to`/`exports`/`binds` kinds and their properties (additive
  vocabulary).
- Resolver provider identity/version (properties of `CodeEvidence`, no
  contract change).
- Nothing in the design requires a core-engine change; the review found no
  hidden core dependency.

### B3. What cannot be determined without implementation

- Actual resolution cost at repository scale (analysis in §I; benchmarks
  deliberately not invented).
- Whether EC-6C/D's export-table semantics can be made deterministic enough
  in every corner (deferred by design; see §K).

## C. Architecture assessment (Review Area A)

**Selected approach.** Resolution as a pass inside the Tree-sitter extraction
session, after all per-file units, emitting edges only into one stable
`{language}:modules` unit owned by the same plugin.

**Answers to the ten required checks:**

1. *Complete visibility of the relevant source set?* Yes for the plugin's own
   language universe (`B1.1`); that is exactly the design's declared scope.
2. *Full repository or plugin subset?* Plugin subset only. The design says
   so; the review confirms no runtime mechanism offers more without core
   changes.
3. *Missing vs other-plugin files distinguishable?* Not distinguishable from
   inside the plugin, and it does not need to be: both are "not in this
   resolver's inventory" → unresolved. The design documents this honestly
   (D-18); the consequence is measured recall loss, never a false edge.
4. *All declarations available when resolution runs?* Yes — the resolution
   pass runs after the per-file loop that produced them
   (`TreeSitterExtractionSession.cs:74-161` ordering is sequential and
   `await`ed).
5. *Can the resolution unit reference other units' nodes?* Yes, when staged
   after them in the same run (`B1.3`, probe P1). The C# plugin's
   dependency-ordered cross-project units are the in-repo precedent.
6. *Is unit ordering guaranteed?* Within one session, yes, by the
   session's sequential emission; the lifecycle stages obsolete deletions
   only after plugin execution (`B1.7`), which the deletion scenario
   requires (probe P3). **Qualification F02:** the design must state this
   ordering as an explicit implementation contract and pin it with lifecycle
   fixtures, because the runtime enforces it only by rejection, not by
   scheduling.
7. *Subset of files changes?* The plugin executes; the resolution unit is
   fully rebuilt. Cost is O(sources + imports), correctness is unconditional.
8. *Target changes, importer unchanged?* Target's hash changes → plugin
   executes → full re-resolution. Verified by the planner classification
   (`B1.2`).
9. *Independent replacement without stale edges?* Whole-unit replacement
   guarantees it (probe P2); the resolution unit must be re-emitted (possibly
   empty) whenever the session runs — the design specifies this and the
   empty-completion pattern exists (`TreeSitterExtractionSession.cs:96-107`).
10. *Compatible with durable publication?* Yes (`B1.6`, `B1.9`); no store or
    snapshot schema change required.

**Plugin-local pass vs separate post-extraction phase.** The comparison was
performed seriously rather than accepting simplicity. The core-phase
alternative would add ordering and visibility guarantees the current engine
lacks as *contracts* (today they exist as implementation facts), at the cost
of new core contracts, a new ownership model, and service changes. The
plugin-local approach achieves the same consistency model inside one
verified session boundary, fails loudly on any ordering violation (probes
P1/P3), and keeps the resolver core liftable. **The review accepts the
plugin-local approach for EC-6B–EC-6F**, with two qualifications: (a) F02's
ordering contract must be explicit; (b) the Tree-sitter coupling is bounded
by the internal seam — a future core phase can adopt the resolver core
without identity changes. Future languages reuse the seam or implement their
own; neither is precluded.

## D. Evidence policy assessment (Review Area B, D-03/O-01)

**B1 — module resolution.** The design's `relative-v1` resolution is a
unique-candidate match under an explicitly selected, versioned policy over an
inventory the provider fully possesses. It is *not* consistent-with-tsconfig
and *not* runtime/bundler equivalence; the design never claims those and
refuses config-dependent cases (unresolved). The minimum evidence necessary
for a `Resolved` module relationship is exactly what the design specifies:
unique ordered candidate, no name matching, policy+rule+specifier recorded as
properties, resolver provider identity and version, and absence-is-not-a-
negative semantics.

**B2 — export relationships.** The review agrees with the design's own
warning: a written `export { value as result }` establishes a binding only
through module-scope resolution of `value`. Two sub-cases differ materially:

- *Direct forms* (`export const value = 1`): the design captures the declared
  node id during the walk — direct syntax association, no lookup. If
  implemented as specified, this is the strongest possible structural-to-
  resolved association.
- *Local lists and re-exports*: binding requires unique module-scope name
  resolution. This is *defensible* as Resolved only with a documented scope
  model (module-scope declarations are the only candidates, shadowing of
  module scope by locals does not apply to export lists, ambiguity refuses).
  Name-based symbol *identity* convergence (§E) further limits what the
  target node can attest.

**B3 — import bindings.** The design correctly separates the five facts
(module target, exported name, local alias, export binding, originating
declaration) into distinct edges and properties; module resolution success
never implies binding success (separate kinds, separate counts).

**Required decision (D-03/O-01) — review recommendation:**

1. **Accept `Resolved` for `resolves-to` in EC-6B**, with a mandatory
   contract sentence in the consumer documentation and the identity doc:
   > "`Resolved` on a `resolves-to` edge means: the named resolver verified,
   > under the declared policy and the indexed source inventory of this
   > publication, that exactly one repository-local file matches the written
   > specifier. It does not claim TypeScript-configuration, bundler, or
   > runtime equivalence."
   Rationale: `Derived` is defined for facts computed *from other evidence*
   (the `exercised-by` precedent) and "must never present itself as
   resolved"; module resolution is provider analysis over source + inventory
   + policy, not evidence aggregation. Choosing `Derived` would understate
   the fact and would make it impossible to distinguish path arithmetic from
   the later, genuinely semantic export/binding resolution.
2. **Settle `exports`/`binds` evidence classification at the EC-6C/EC-6D
   gates**, not now, under these binding preconditions: direct-form exports
   (captured declaration reference) may be `Resolved`; local-list, re-export,
   star, and import bindings may be `Resolved` only if the milestone's design
   documents the module-scope model and shadowing analysis that justifies
   the claim; any binding established by name matching alone is forbidden in
   either quality. Name-based symbol identity convergence (finding F01) must
   be resolved or attributed before `binds` ships.
3. Fixtures must assert: resolver edges never carry `Structural`; no
   pre-existing fact changes quality or identity.

**Compatibility implications.** Accepting `Resolved` for `resolves-to` is
additive: no existing edge changes quality; the `calls`-requires-`Resolved`
validator rule is unaffected; evidence filters behave identically. If the
owner instead selects `Derived`, EC-6B still proceeds with the same
structure — only the quality constant and documentation change — so this
decision does not alter the architecture's shape.

## E. Identity and relationship assessment (Review Area C + D)

**Multiplicity (probe P4, executed).** The existing graph contract supports
multiple logically distinct relationships between the same endpoints:
distinct edge ids (discriminator + span) coexist, both are returned by
traversal with their own properties, and deleting one occurrence is achieved
by unit replacement. Identity collision with different properties is a hard
conflict (`graph.edge.conflict`) — the run fails loudly; nothing is silently
dropped. **Therefore the property-based edge model is sufficient**; no
binding-node abstraction is required. **Qualification F09:** because
identity excludes properties, the design's discriminator specification
(written name + form) must be normative for every resolver kind, with
fixtures for alias multiplicity and collision rejection. For EC-6B the
requirement is smaller (statement spans already disambiguate repeated
imports) but the normative statement belongs in the EC-6B contract text.

**Name-based convergence (probe P5, executed).** The design matrix is
confirmed from source and execution:

| Symbol kind | Identity scope | Bindable safely (EC-6D)? | Limitation |
| --- | --- | --- | --- |
| File-level variable | path + name (`EcmaScriptExtractionProfile.cs:334-336`) | Yes | Dual projection is a *second* node (ambiguous under unique-match rule; D-05) |
| Class | path + qualified name (127) | Yes | — |
| Interface | path + qualified name (147) | Yes | — |
| Function (top-level) | name only (176) | **No — converged identity** | Same node for every module declaring that name (probe P5) |
| Type alias | name only (156) | **No — converged identity** | Same |
| Enum | name only (159) | **No — converged identity** | Same |
| Namespace | name only (164) | **No — converged identity** | Same |
| Class/interface members | owner-derived | Yes (via owner) | — |

Probe P5 executed the real extractor: two files declaring `export function
run(){}` converge to **one** node id; two files declaring `export const run`
stay **distinct**; a function-valued variable yields **two** same-named
nodes (field + callable).

**Consequence and recommendation (finding F01).** For EC-6D, a `binds` edge
targeting a converged function/type/enum/namespace node cannot, by node
identity alone, attribute which module's declaration the importer reached;
`exports` edges from several modules to one node compound this. This is the
EC-3D failure shape (unattributable relationships caused by name-based
convergence) reintroduced at the binding level. EC-6D must choose one of:

1. carry an explicit declaring-module property (e.g. `exported-from`) on
   every `exports`/`binds` edge so attribution is recoverable per edge; or
2. restrict binding eligibility to path-scoped kinds and refuse
   name-based kinds with counted unsupported targets; or
3. perform the identity migration (path-scoping those kinds) as its own
   milestone before binding ships.

The review recommends option 1 for EC-6D (truthful, additive, no recall
loss) with option 3 recorded as the durable fix. **This does not affect
EC-6B**, which emits no export or binding facts.

## F. Publication and invalidation assessment (Review Area F)

| Case | Mechanism | Status |
| --- | --- | --- |
| F1 importer changed | hash change → plugin executes → resolution unit rebuilt | verified (planner `CodeIndexPlanner.cs:204-208`) |
| F2 target deleted | plugin executes (Deleted item); file unit reported obsolete; resolution unit rebuilt without it; deletion staged after plugin execution | **executed** probe P3 + lifecycle ordering `CodeIndexingLifecycle.cs:681-696`; wrong order rejected loudly |
| F3 target added | New item → plugin executes → resolves | verified + store analog probe P1 |
| F4 export changed | content hash change → same as F1 | verified |
| F5 policy/version change, sources identical | plugin version participates in the manifest → every path `Changed` | verified (`CodeIndexPlanner.cs:204-208`; test `Plan_InvalidatesPreviousExtractionProfileRevisions`, `TreeSitterTypeScriptPluginTests.cs:778`) |
| F6 resolution fails after file units staged | run fails; staged footprint deleted; prior publication retained | verified (`CodeIndexingLifecycle.cs:536-556`; conformance `failed-run-discards-staged-graph`) |
| F7 cancellation | run terminal non-completed → staged discarded; prior retained | verified (conformance `cancelled-run-discards-staged-graph`; sink `CompleteAsync` rejects pending units). Qualification F05: the resolver loop must honor the token |
| F8 process interruption | Lattice: durable log replay, staged rows discarded on non-completion, one-transaction commit; In-memory: process state is the publication boundary | verified (`LatticeCodeGraphStore.cs:299-349, 441-449, 503-517`) |
| F9 empty resolution | empty completing batch replaces the unit; previous edges removed | **executed** probe P2; pattern precedent `TreeSitterExtractionSession.cs:96-107` |
| F10 mixed revisions | publication is one atomic swap; readers see one consistent graph; unchanged runs return the previous publication | **executed** probe P6 + `CodeIndexingLifecycle.cs:364-385, 732` |

**Atomic publication vs ordered staging.** The distinction is real and
verified: staging is ordered and order-validated (probes P1/P3), visibility
is granted only at atomic completion (probe P6). The design's claims are
achievable **without runtime changes**; the only mandatory additions are
documentation and fixtures (F02) plus the deletion-ordering fixture in
EC-6E.

**Single resolution unit sufficiency.** Adequate for EC-6B–E: a poisoned
resolution unit fails the run loudly, exactly like any other unit rejection
(no partial publication exists in this engine). If per-file failure
isolation for resolution is ever wanted, it is an EC-6G-scale change; not
required by any identified scenario. The all-files-deleted interaction
(hygiene deletes the empty staged resolution unit because the plugin holds
no paths; `InMemoryCodeGraphStore.cs:389-390` removes the staged
replacement in favor of the deletion) was analyzed and is correct; fixture
in EC-6E (F10-note in §K).

## G. Cycles and export closure (Review Area G)

Worked example from the review brief (`a *-exports b; b *-exports c and
*-exports a; c exports value`): with the design's memoized visited-set
algorithm, `Table(a)` → star from `b` → `Table(b)` → star from `c` (value)
and star from `a` (in-progress → cut); `value` is exposed through both a and
b exactly once. Termination is guaranteed; the result is an
**under-approximation** (cyclic contributions are dropped, never guessed).

Assessment of the alternatives: strongly-connected-component decomposition
and bounded fixed-point evaluation would recover some cyclic exports but add
substantial algorithmic and semantic surface (fixpoint semantics for
conflicting stars inside cycles are genuinely ambiguous in TypeScript
itself). **Recommendation for EC-6D:** keep the visited-set cut with
memoization, document the under-approximation as a qualified limitation,
require order-independence and cycle fixtures (the brief's example must
become a fixture), and defer fixed-point evaluation until a measured
consumer need exists. Direct-export shadowing of stars, star conflicts, and
default exclusion are already specified by the design and are correct.

Type/value namespace separation (brief item): `export type T` and
`export const T` in one module are distinct graph nodes; an import of `T`
would be ambiguous under the unique-match rule. Conservative refusal is
acceptable and must be documented (folded into F07's EC-6D clarifications).

## H. Query semantics (Review Area H)

- `resolves-to` (EC-6B) means a **verified module dependency under the
  declared policy**; `imports` remains the written-syntax observation. The
  two are distinguishable by kind and by evidence quality; consumer guidance
  must say so explicitly (D-15/F03-adjacent docs requirement).
- Exact-name lookup already returns declaration-less candidates beside
  declarations; the new edges change no node, so lookup results are
  unchanged (probe P5 nodes are unchanged by EC-6B).
- Export aliases (EC-6C) are retrievable as independent edges with distinct
  identities (probe P4); no query change needed.
- Unresolved/ambiguous relationships are **not** exposed as facts; they are
  extraction-result coverage counts, which are not queryable per publication
  (finding F06 — a platform-level gap the design correctly does not try to
  paper over).
- Impact presets exclude the new kinds automatically (closed lists,
  `CodeGraphTraversalPresets.cs:10-23`); **the review endorses D-15: no
  preset changes in EC-6B–F**, and module facts must not join `ImpactSet`
  without a separately justified contract decision.
- Transitive traversal cannot be mistaken for verified impact as long as
  presets stay closed; consumers who traverse `resolves-to` transitively get
  verified module *dependencies*, not impact.
- Policy-dependence: edges carry policy/rule properties and a
  policy-bearing provider version; two publications under different policies
  are comparable through provenance, and a version change re-extracts
  everything (F5), so a graph never mixes policies.

**Recommended explicit EC-6B query behavior:** unchanged API; document the
traversal patterns (outgoing/incoming `resolves-to` via
`TraverseWithProvenanceAsync`/`GetNeighborhoodAsync`), the
`Resolved`-filter usage, and the "absence is not evidence of absence" rule.

## I. Performance and resource boundaries (Review Area I)

- Complexity per executing run: resolution is O(S·(1 + ī) + Σ export names +
  star closure) with memoized tables; star closure is bounded by reachable
  export names, worst case O(S·N) with S files and N distinct exported
  names; cycle cut bounds depth by the number of modules.
- Memory: the session already materializes all plugin sources under the
  existing byte budgets (`CodeIndexingLifecycle.cs:1105-1166`); the resolver
  adds per-file shapes and name→node indexes of the same order as the
  already-materialized facts.
- Rebuild after a single-file edit: full re-resolution (correctness-first);
  acceptable at the scale of the qualified repositories; EC-6F must measure.
- LatticeDb cost: one unit replacement per run — the same cost class as any
  file unit rewrite.
- Upper bounds: unit caps (1,024 batches × 5,000 edges; 1,000,000 facts)
  bound the resolution unit; exceeding them fails the run loudly (sink
  poison), which is the correct failure mode.
- Cancellation: must be honored between modules (F05 requirement).
- Degradation (D-17): the proposed guidance limit with deterministic
  degradation order (module edges → direct exports → bindings) plus warning
  code and coverage counts is acceptable; it must be fixture-proven in
  EC-6E and the exact numbers need owner acceptance (O-07). No benchmark
  numbers are invented here.

## G2. Independent adversarial cases (AR1-01 … AR1-15)

| Case | Invariant challenged | Result | Evidence | Blocks EC-6B? |
| --- | --- | --- | --- | --- |
| AR1-01 two aliases, same endpoints | multiplicity | Correct: both edges coexist with distinct identities/properties; collision is a loud conflict | **executed** P4 | No (F09 makes the discriminator contract normative) |
| AR1-02 two imports, same source/target, different occurrences | multiplicity | Correct: distinct spans → distinct ids; repeated extraction reproduces ids | executed (P4 mechanism) + design §7.1 | No |
| AR1-03 name-based identities converge incorrectly | EC-3D lesson | Confirmed real (functions/types/enums/namespaces converge; variables/classes/interfaces do not); must be attributed or refused at EC-6D | **executed** P5 + source (127/147/156/159/164/176/638) | No (no bindings in EC-6B) → finding F01 |
| AR1-04 target deleted, importer unchanged | stale edges | Correct: no stale edge can survive; ordering mandatory and enforced by rejection | **executed** P3 | No |
| AR1-05 previously missing file added | rebuild | Correct: New → plugin executes → resolution rebuilt | verified planner + probe P1 analog | No |
| AR1-06 resolver config changes without source edits | invalidation | Correct: version change invalidates all plugin paths | verified (`CodeIndexPlanner.cs:204-208`, test at `TreeSitterTypeScriptPluginTests.cs:778`) | No |
| AR1-07 cancellation after file-unit staging | atomicity | Correct: staged discarded, prior retained; resolver must honor token mid-pass | verified conformance + code; F05 requirement | No |
| AR1-08 empty resolution replaces prior edges | cleanup | Correct: empty completion removes previous edges | **executed** P2 | No |
| AR1-09 same specifier, two policies | provenance | Correct: policy/rule properties + version-stamped provider; no mixed-policy publication | design + F5 verification + probe P6 | No |
| AR1-10 cross-plugin TS/JS dependency | scope honesty | Correct-as-limited: unresolved-by-scope, never fabricated; recall cost measured in EC-6F | design analysis (D-18) | No |
| AR1-11 re-export cycle with reachable declaration | termination + completeness | Correct: visited-set exposes the reachable declaration (worked example), terminates, under-approximates | design analysis (§G of this report) | No (EC-6D) |
| AR1-12 conflicting star exports | no guessing | Correct: no edge for the conflicting name; ambiguity counted; order-independence fixture required | design analysis | No (EC-6D) |
| AR1-13 type-only and value exports share a written name | namespace separation | Conservative refusal (ambiguous) acceptable; must be documented | design analysis | No (EC-6D) |
| AR1-14 path-case collision on case-insensitive filesystem | determinism | Correct: ordinal comparison; mismatch → unresolved; no fs access | design analysis (policy) | No (pin in F04) |
| AR1-15 resolution budget exhausted | degradation honesty | Correct-with-qualification: loud failure at hard caps; deterministic degradation at the guidance limit with coverage + warning | design analysis (D-17) | No (fixture in EC-6E; numbers to owner) |

## H2. Findings

### EC6-GLM-AR1-F01 — Name-based convergence makes binding targets unattributable
- **Disposition / severity:** DESIGN AMBIGUITY — **P2**
- **References:** design architecture §7.3, D-05, O-02, R-02; adversarial AC-01b;
  runtime `EcmaScriptExtractionProfile.cs:127, 147, 156, 159, 164, 176, 638`; probe P5.
- **Violated invariant:** relationships must be attributable without display-name inference (EC-3D lesson).
- **Counterexample:** a.ts and b.ts both `export function run(){}`; importer binds `run` — the shared target node cannot say which module's function was imported.
- **Expected / proposed:** attribution recoverable per edge (declaring-module property), or eligibility restriction, or identity migration (§E).
- **Consumer consequence:** without correction, "who imports a.ts's run" cannot be answered exactly.
- **Minimal correction:** require `exported-from` (declaring module path) on every `exports`/`binds` edge in EC-6C/D; record option 3 as the durable fix.
- **Stage:** EC-6C/D. **Blocks EC-6B:** No.

### EC6-GLM-AR1-F02 — Staging-order contract is implicit
- **Disposition / severity:** DESIGN AMBIGUITY — **P3**
- **References:** design architecture §9.1; runtime `InMemoryCodeGraphStore.cs:287-299, 358-390, 913-929`; `CodeIndexingLifecycle.cs:681-696`; probes P1/P3.
- **Invariant:** resolution facts may only be staged after the units they reference; deletions after replacement.
- **Counterexample:** reversed staging is rejected (`MissingEndpoint`) — safe, loud — but the design must not rely on rejection as its specification.
- **Correction:** state the ordering as a normative implementation contract of the resolver emission and add lifecycle fixtures (importer-changed, target-deleted, wrong-order-rejected) to EC-6B acceptance.
- **Stage:** EC-6B (docs + fixtures). **Blocks EC-6B:** No, but required in its acceptance criteria.

### EC6-GLM-AR1-F03 — Publication claims verified
- **Disposition:** NO ISSUE FOUND (informational).
- Cross-unit referencing, whole-unit replacement, empty-clear, no-op reuse, staged-invisibility, atomic completion, failed/cancelled retention, and Lattice delegation were verified in source and by probes P1–P3, P6 and the conformance suite. The design's publication model needs no runtime change.

### EC6-GLM-AR1-F04 — Policy normalization underspecified
- **Disposition / severity:** DESIGN AMBIGUITY — **P3**
- **References:** design architecture §6.2; adversarial AC-17/AC-18/AC-23.
- Gaps: trailing-slash specifiers, bare `.`/`..`, backslash separators, `?query`/`#fragment` suffixes, property presence conventions (`type-only` absent vs false), and behavior for specifiers beyond the 256-char bound are not pinned.
- **Correction:** one normative normalization + classification table (resolved/unresolved/external/unsupported per input class) added to the policy section before EC-6B fixtures are written.
- **Stage:** EC-6B. **Blocks EC-6B:** No, but must precede its fixtures.

### EC6-GLM-AR1-F05 — Cancellation and determinism under cancellation unspecified
- **Disposition / severity:** DESIGN AMBIGUITY — **P3**
- The resolver loop must honor `CancellationToken` between modules; cancellation at any point must leave the run failed with the prior publication retained (no partial resolution unit). One paragraph + one fixture.
- **Stage:** EC-6B. **Blocks EC-6B:** No.

### EC6-GLM-AR1-F06 — Resolution coverage is not queryable per publication
- **Disposition / severity:** QUALIFIED LIMITATION — **P3**
- Coverage exists only in extraction results (`CodeGraphExtraction.cs:267-350`); consumers cannot ask "did the resolver run for this publication?" except via plugin-version provenance. Consistent with the platform's recorded coverage-persistence gap; consumer guidance must state it. Deferred; not a defect of this design.
- **Stage:** post-EC-6F (platform work).

### EC6-GLM-AR1-F07 — Star/re-export type-only propagation unspecified
- **Disposition / severity:** DESIGN AMBIGUITY — **P3**
- Whether star-derived `exports` edges preserve the source export's `type-only` flag, and how namespace re-exports interact with type-only syntax, is unspecified (O-04). Must be specified in the EC-6D design with fixtures; conservative refusal is acceptable.
- **Stage:** EC-6D. **Blocks EC-6B:** No.

### EC6-GLM-AR1-F08 — No existing contract changes
- **Disposition:** NO ISSUE FOUND.
- Additive kinds/properties do not alter public-API type snapshots
  (`PublicApiContractTests.cs:11-66`), presets (`CodeGraphTraversalPresets.cs:10-23`),
  store/snapshot schemas, or any existing identity. Existing `imports`
  observations remain untouched.

### EC6-GLM-AR1-F09 — Edge-identity/discriminator contract must be normative
- **Disposition / severity:** DESIGN AMBIGUITY — **P2**
- **References:** `CodeGraphFacts.cs:79-93` (identity excludes properties), `GraphFactEquality.cs:25-31` (equality includes them), probe P4 (coexistence and `graph.edge.conflict`).
- The design's discriminator scheme is load-bearing but stated informally. **Correction:** make per-kind discriminator + span identity a normative contract (including "same id + different properties fails the run"), with fixtures; include the EC-6B-relevant statement (`resolves-to` uniqueness per statement span) in the EC-6B contract text.
- **Stage:** EC-6B (statement) and EC-6C/D (full contract + fixtures). **Blocks EC-6B:** No.

### EC6-GLM-AR1-F10 — All-files-deleted interaction should be fixture-pinned
- **Disposition / severity:** QUALIFIED LIMITATION — **P3**
- When every file of a plugin is deleted, the plugin still executes, the (empty) resolution unit is staged, and cross-run hygiene then deletes it because the owner holds no paths (`CodeIndexingLifecycle.cs:716-731`; `InMemoryCodeGraphStore.cs:389-390` removes the staged replacement in favor of the deletion). Outcome is correct; EC-6E must fixture it.
- **Stage:** EC-6E. **Blocks EC-6B:** No.

### EC6-GLM-AR1-F11 — Per-plugin scope is the only safe scope today
- **Disposition:** NO ISSUE FOUND (D-18 upheld).
- Concurrent plugin execution (`MaxConcurrentPlugins`) provides no cross-plugin ordering or visibility; the design's per-language scope is the maximal safe scope without core work. Recall cost must be measured in EC-6F.

### EC6-GLM-AR1-F12 — Export/binding evidence classification deferred with preconditions
- **Disposition / severity:** QUALIFIED LIMITATION — **P3**
- Consistent with §D: `exports`/`binds` evidence is settled at the EC-6C/D gates under the stated preconditions (direct-capture may be Resolved; scope-resolution requires documented justification; name-match-only binding forbidden). Not a defect; a scheduled decision.

**Summary:** no P0/P1 findings. Two P2 ambiguities (F01, F09) are EC-6C/D-stage
with small EC-6B documentation consequences; the P3 items are bounded
clarifications.

## I2. Decision reconciliation

See [ec6-glm-ar1-decision-reconciliation.md](ec6-glm-ar1-decision-reconciliation.md)
for the owner-facing table. Notable: the review brief's §15 labels for D-07
("resolution-unit ownership") and D-14 ("publication assumptions") do not
match the design register (D-12/D-13 own those topics; D-07 is binding rules
and D-14 is coverage counting). The reconciliation table assesses the actual
register entries and covers the brief's intent under both IDs.

## J. EC-6B readiness

**Primary question: can EC-6B implement conservative relative module
resolution without weakening Hetu's existing correctness guarantees?**

**Answer: yes, with the qualifications below.** The bounded scope
(relative-only resolution, `resolves-to` only, no exports/bindings, no
re-export following, no preset changes, no package resolution) is compatible
with every verified runtime guarantee and requires no core change.

**Required before EC-6B implementation begins (all small, bounded):**

1. **Owner acceptance of the evidence classification for `resolves-to`**
   (§D recommendation: `Resolved` with the mandated policy-specific contract
   sentence; `Derived` is a workable fallback requiring no structural
   change).
2. **F04:** normative normalization/classification table added to the policy
   section.
3. **F05:** cancellation/determinism requirement added.
4. **F02 + F09:** ordering contract and per-kind identity/discriminator
   statements added to the EC-6B contract text, with the lifecycle fixtures
   (importer-changed, target-deleted, wrong-order-rejected, empty-rebuild,
   unchanged no-op) folded into the EC-6B acceptance criteria.

**EC-6B acceptance criteria (as proposed by the design, endorsed with the
additions above):** the design's ten criteria are achievable; criterion 6
(invalidation) and 7 (failure atomicity) are additionally proven by the new
fixtures; criterion 3 (identity/multiplicity) is satisfied for EC-6B's
statement-scoped edges and made normative by F09's text.

**Explicitly out of EC-6B (do not expand):** export tables, bindings,
re-export/star semantics, tsconfig, CJS, cross-plugin resolution, preset
changes, query conveniences.

## K. Later-stage issues (explicitly deferred)

| Issue | Stage | Gate |
| --- | --- | --- |
| F01 attribution of converged identities (`exported-from` or migration) | EC-6C/D design | owner |
| F09 full discriminator contract + alias/collision fixtures | EC-6C/D | review |
| F07 star type-only propagation; namespace/type-only interactions; O-04 | EC-6D design | review |
| Cycle-closure completeness (visited-set vs fixed-point); order-independence fixtures | EC-6D design | review |
| D-06 export of imported bindings; D-11 anonymous defaults; D-08 `.js`→`.ts`; D-09 tsconfig; D-10 CJS surfaces | post-EC-6F | owner |
| F06 coverage persistence; query conveniences; preset/impact decisions (D-15) | platform work | owner |
| F10 all-files-deleted fixture; degradation-limit fixture and numbers (O-07) | EC-6E | review + owner |
| Cross-plugin resolution (D-18 evolution) | post-EC-6F, core work | owner |

## L. Final verdict

**READY WITH QUALIFICATIONS.**

The EC-6A architecture is compatible with the verified runtime contracts;
its publication and invalidation claims were confirmed by source inspection
and executed probes; no P0/P1 findings exist. EC-6B can proceed once the
owner accepts the `resolves-to` evidence classification and the small set of
specified corrections (F02, F04, F05, F09-statements) is folded into the
EC-6B contract text and acceptance criteria. The P2 ambiguities (F01, F09)
gated at EC-6C/D do not affect EC-6B's bounded scope. No authorization to
implement is granted or implied.

### Executed evidence (this environment)

| Command | Result |
| --- | --- |
| `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --filter FullyQualifiedName~ReviewEc6GlmAr1Probes` | 6/6 passed (probes P1–P6) |
| `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj` (full project incl. probes) | 96/96 passed |
| `git diff --name-status 558f8ac aa4031c` | exactly 4 design docs, no src/tests |
| `git log --oneline design/ec6a-cross-file-resolution` | 4 design commits, linear from `558f8ac` |

Source-inspection-only conclusions are marked as such in §B and §F;
remaining unverified assumptions are listed in §B3 and §K.
