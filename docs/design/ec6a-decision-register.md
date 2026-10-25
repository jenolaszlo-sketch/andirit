# EC-6A: Decision register

**Status:** design artifact for EC-6A. Every entry is a **proposal** awaiting
independent design review and, where marked, owner approval. None is
implemented.
**Baseline:** `558f8ac` (EC6-A0).

Status vocabulary:

- **RECOMMENDED** — the architecture document recommends this; the design
  reviewer should accept or reject it with rationale.
- **OWNER APPROVAL REQUIRED** — touches public vocabulary, evidence meaning,
  consumer-visible semantics, or declared scope; must not be implemented on
  design-review acceptance alone.
- **DEFERRED** — deliberately excluded from EC-6B–F; recorded so later work
  starts from a decision, not an accident.
- **OPEN** — not yet resolved; listed for the design review to close or
  carry forward.

| ID | Decision | Status | Milestone |
| --- | --- | --- | --- |
| D-01 | Resolver placement | RECOMMENDED | EC-6B |
| D-02 | Module identity | RECOMMENDED | EC-6B |
| D-03 | Evidence quality of resolver facts | RECOMMENDED + OWNER APPROVAL REQUIRED | EC-6B |
| D-04 | Edge kind vocabulary | RECOMMENDED + OWNER APPROVAL REQUIRED | EC-6B/C/D |
| D-05 | Name-based symbol targets and dual projections | RECOMMENDED + OWNER APPROVAL REQUIRED | EC-6C |
| D-06 | Export of imported bindings | DEFERRED | post-6F |
| D-07 | Import binding rules and type-only semantics | RECOMMENDED | EC-6D |
| D-08 | `.js` → `.ts` extension substitution | DEFERRED | post-6F |
| D-09 | `tsconfig` paths/baseUrl/module modes | DEFERRED | post-6F |
| D-10 | CommonJS surfaces and `require` bindings | RECOMMENDED (boundary) / DEFERRED (modeling) | EC-6B (edges) / post-6F |
| D-11 | Anonymous default exports | DEFERRED | post-6F |
| D-12 | Resolution unit identity and ownership | RECOMMENDED | EC-6B |
| D-13 | Invalidation model | RECOMMENDED | EC-6B, qualified in EC-6E |
| D-14 | Coverage counting | RECOMMENDED | EC-6B |
| D-15 | Query surface changes | RECOMMENDED (none) + OWNER APPROVAL REQUIRED (impact) | EC-6B–F |
| D-16 | Version and invalidation identity | RECOMMENDED | EC-6B |
| D-17 | Bounds and degradation | RECOMMENDED + OWNER APPROVAL REQUIRED | EC-6E |
| D-18 | Cross-plugin TypeScript↔JavaScript resolution | RECOMMENDED (defer) + OWNER APPROVAL REQUIRED | EC-6F (measure) |
| D-19 | Property vocabulary discipline | RECOMMENDED + OWNER APPROVAL REQUIRED | EC-6B–D |
| D-20 | Consumer meaning of missing resolver facts | RECOMMENDED | EC-6B–F |

---

## D-01 — Resolution placement

**Decision.** Resolution runs as a pass inside the existing Tree-sitter
extraction session, after all per-file units of that session are emitted,
factored behind an internal `ITreeSitterModuleResolver` seam with an
ECMAScript implementation. A core resolution phase and a separate resolver
plugin are deferred.

**Alternatives considered.** (A) extract-time per-file resolution — rejected:
no cross-file visibility and no invalidation path. (B) core phase — deferred:
requires new core contracts, ordering, and ownership before any evidence of
need. (C) separate resolver plugin — deferred: needs new plugin roles and
source-visibility changes.

**Rationale.** Preserves every lifecycle guarantee (ownership, ordering,
replacement, atomicity), requires no core change, fits the existing
session-context model (the session already materializes all its sources),
and keeps the resolver core parser-independent so it can later be lifted into
B or C without identity changes. C# project-unit cross-project resolution is
the in-repo precedent.

**Consequences.** Resolution scope is one plugin's file universe
(see D-18). Future cross-language resolution will require revisiting this
decision.

## D-02 — Module identity

**Decision.** A module is an indexed source file; module identity is the
existing file node id `{language}:file:{sha256(path)}`; module name is the
repository-relative path in `QualifiedName`. No `module` node kind is added.

**Alternatives.** New `module` kind with its own namespace; reusing
`namespace`; a synthetic node per path.

**Rationale.** File nodes already exist, are stable, path-addressed, and
queryable; the feature matrix records "module identity convention undecided"
(`docs/language-feature-matrix.md:89`) and EC-5 closure already rejected an
unnecessary new kind on contract-minimalism grounds. No new node kinds keeps
the change surface to three edge kinds.

**Consequences.** "Module" and "file" are the same concept for the qualified
subset; external modules have no identity by construction. If package-level
boundaries are ever needed (workspaces, `package.json`) that is a new
design, not an extension of this one.

## D-03 — Evidence quality of resolver facts

**Decision (recommended).** Resolver edges carry `Resolved` evidence with a
dedicated resolver provider identity and policy-bearing version string.

**Alternatives.** `Derived` ("Hetu deterministically computed the fact from
other evidence"); a new quality value (rejected: vocabulary freeze).

**Rationale.** `Resolved` is defined as "resolved by the named provider in
its analysis context, not universally exact"
(`src/Penghou.Hetu.Abstractions/CodeEvidence.cs:15-20`); module resolution
and binding are language-defined resolution concepts implemented by a named,
versioned resolver under a declared policy, and the resolver emits only
facts it can verify within that policy. `Derived` is defined for facts
computed from other *evidence* and "must never present itself as resolved";
the resolver's input includes written syntax *and* the source inventory and
declared symbols, which is provider analysis context rather than an
aggregation of graph edges.

**Review-critical.** This is the most consequential claim in the design. The
independent design review must validate it against the ADR and either accept
it or select `Derived`; the choice applies uniformly to `resolves-to`,
`exports`, and `binds`, is asserted by fixtures, and must be reflected in
consumer guidance. Owner approval required because it changes what
`Resolved` means for a structural provider.

## D-04 — Edge kind vocabulary

**Decision (recommended).** Three additive kinds: `resolves-to` (module
resolution), `exports` (export surface), `binds` (import binding).

**Alternatives.** Upgrade/retarget `imports` (rejected: published fact
migration); reuse `depends-on` (rejected: documented project/package scope);
`imports-symbol`/`resolves-name` naming; a single `resolved` kind with
properties.

**Rationale.** `imports` remains the structural spelling observation exactly
as EC-4 recommended; three focused kinds keep presets and evidence policies
precise and let consumers opt in per question. Owner approval required
because kinds are public vocabulary.

**Consequences.** Traversal presets do not change automatically (D-15).

## D-05 — Name-based symbol targets and dual projections

**Decision (recommended).** `exports` edges may target symbols whose
identity is name-based and converges across files (top-level functions,
types, enums, namespaces); the convergence caveat is documented and asserted
(adversarial case AC-01b). For local export lists, a written name matching
two distinct symbol-bearing nodes in one file (the function-valued variable
`field` + `callable` projections) is ambiguous: no edge.

**Alternatives.** Restrict export edges to path-scoped kinds (rejected:
loses most real exports and hides true facts); prefer the `field` projection
heuristically (rejected: preference heuristics contradict "no guessing");
make star/list exports follow the direct-export capture (not possible for
names never written as direct exports).

**Rationale.** The export fact is true; the shared identity is a pre-existing
documented limitation (`docs/ecmascript-symbol-identity.md:223-232`), not
something EC-6 creates. Ambiguity is reported, never guessed.

**Consequences.** Consumers must use `exports`/`binds` edges (not symbol
identity alone) to attribute a module surface. Identity migration is a
separate milestone (roadmap post-EC-6F item 6). Owner approval required
because it accepts a visible truthfulness tradeoff.

## D-06 — Export of imported bindings

**Decision.** `import { x } from './n'; export { x };` emits no `exports`
edge in EC-6C/D; counted unsupported.

**Rationale.** Binding through the import requires import binding (EC-6D)
and a second chain hop with its own ambiguity rules; doing it correctly is a
small, separable design with its own fixtures. **Deferred**, recorded here.

## D-07 — Import binding rules and type-only semantics

**Decision.** Binding only from the resolved module's export table; named →
final symbol; default → the `default` entry; namespace → the module file
node; value import of a `type-only` export emits no binding; type-only
import of a value export binds with `type-only: true`; missing/ambiguous
names emit nothing and are counted.

**Rationale.** Matches the resolver's conservative principle and the
explicitly required "type/value distinction" without pretending to perform
type checking.

**Consequences.** A small class of legal-but-erased imports is represented
by absence plus counts; documented.

## D-08 — `.js` → `.ts` extension substitution

**Decision.** Not resolved by `relative-v1`; deferred to a future policy
level (`relative-v2`) with its own adversarial qualification.

**Rationale.** NodeNext-style substitution can change which file a specifier
resolves to in mixed repositories; folding it into the first policy would
make EC-6B's acceptance depend on mode semantics this design does not
emulate. Recorded so EC-6F can measure the recall cost.

## D-09 — `tsconfig` paths/baseUrl/module modes

**Decision.** Out of policy. A future policy level must be explicit opt-in,
parse only a declared config subset (no `extends` evaluation at first),
embed a config identity in provenance, and invalidate on config change.

**Rationale.** Reading configuration is a distinct capability with its own
trust, bounds, and invalidation story; silently approximating TS modes would
violate the honesty principle. Deferred with requirements recorded.

## D-10 — CommonJS surfaces and `require` bindings

**Decision (recommended boundary).** Literal `require('./m')` produces a
`resolves-to` edge (`module-reference: require`). No CommonJS bindings;
`module.exports`/`exports.x` produce no export facts. All counted
unsupported. Recorded for a future CJS milestone.

**Rationale.** The module dependency is a true path fact; the export object
is dynamic (`module.exports = …` reassignment, getters) and needs its own
identity work. Mixing a half-model into `exports` would be dishonest.

## D-11 — Anonymous default exports

**Decision.** `export default <expression>` (including anonymous functions
and classes) produces no `exports` edge and no binding target; counted
unsupported. Synthetic default identities are deferred.

**Rationale.** No declaration identity exists to target (EC-5 explicitly
skips anonymous defaults); minting one is an identity-policy decision for a
separate milestone.

## D-12 — Resolution unit identity and ownership

**Decision (recommended).** One stable unit per language plugin,
`{language}:modules`, owned by the existing extraction plugin, containing
edges only, always re-emitted when the session runs (empty completion when
nothing resolves), never reported obsolete.

**Alternatives.** Input-hash unit ids (rejected: churn requires obsolete
reporting and strands units on partial reporting failure); per-module units
(rejected: unnecessary before dependency tracking exists); core-owned unit
(rejected in D-01).

**Rationale.** Stable id + full replacement is the existing unit contract
(`src/Penghou.Hetu/CodeGraphStoreModels.cs:142-168`); edges-only content
minimizes validation surface and needs no new node kinds.

## D-13 — Invalidation model

**Decision (recommended).** Invalidation is plugin-execution-driven: any
input change classifies a plan item as not-unchanged, the plugin executes,
and the resolution unit is fully recomputed. No dependency tracking in
EC-6B–E; EC-6E proves correctness for all change classes.

**Alternatives.** Per-module dependency manifests; resolution-aware
planner extensions; partial re-resolution.

**Rationale.** The output is a pure function of inputs the session already
has; full recomputation is always correct and needs no new planner
contracts. Cost is O(sources + imports) per executing run, bounded in §12 of
the architecture. Dependency tracking is an optimization to revisit only if
EC-6F measures a real cost problem.

## D-14 — Coverage counting

**Decision (recommended).** Map resolver outcomes onto the existing
`CodeRelationshipCoverage` fields per the architecture §12.2 table; sum
`UnresolvedTargets` of resolver kinds into
`CodeGraphExtractionResult.UnresolvedRelationships`. No new coverage fields
and no new `CodeRelationshipCoverageState` values.

**Alternatives.** New counter fields (`MissingTargets`, `PolicyGaps`, …) —
rejected for now: an additive API change should be justified by an observed
consumer need. Reusing fields overloads semantics slightly (documented in
the table).

**Review note.** If the design reviewer finds a semantic overflow the table
cannot carry honestly, a minimal additive field proposal is the correct
escalation, not silent reuse.

## D-15 — Query surface changes

**Decision (recommended).** No query API, preset, or operation changes in
EC-6B–F. Consumers use `GetNeighborhoodAsync`/`TraverseWithProvenanceAsync`
with the new kinds and evidence filters. Module facts must **not** join
`ImpactSet`, `Dependencies`, or `Calls` presets.

**Alternatives.** A `ModuleDependencies` preset; module queries
(`GetModuleImportsAsync` …); including `resolves-to` in impact.

**Rationale.** Presets are consumer semantics; silently enlarging impact
results with module edges would change existing query outcomes — forbidden
without an explicit decision. Convenience APIs are additive and should
follow real consumer demand. Owner approval required if a future task wants
module facts in impact.

## D-16 — Version and invalidation identity

**Decision (recommended).** The existing single `ProfileRevision` counter
(renamed conceptually to "emitted-fact revision" in docs) governs all
resolver-fact changes; resolver provider version string is
`{packageVersion}+resolver{ResolverRevision}.{policy}` and changes in
lockstep with any emitted-fact change. No second invalidation axis.

**Rationale.** The planner compares one plugin-version string; two counters
would force either double invalidation logic or subtle mismatch. Coarse
invalidation (re-extract all files of the plugin) already exists for
profile changes and is acceptable in preview.

## D-17 — Bounds and degradation

**Decision (recommended).** Hard platform caps are never exceeded silently;
a guidance limit of 250,000 resolver edges per language run triggers
deterministic degradation (prefer `resolves-to`, then direct `exports`, then
`binds`; star-derived edges last), a `{language}.resolution-limit` warning
code, and coverage counts reflecting what was not emitted. Resolver kinds
report `Partial` when any source parsed with errors or was undecodable.
Owner approval required: this defines behavior on very large repositories.

**Alternatives.** Fail the run at the cap (harshest but simplest); no
guidance limit (rely on platform caps); truncate silently (forbidden by
project principles).

## D-18 — Cross-plugin TypeScript↔JavaScript resolution

**Decision (recommended).** Deferred. Each plugin resolves only within its
own source universe; a `.ts` import of a JavaScript-owned file is
module-not-found for the TypeScript resolver. The limitation is documented,
asserted (`imports` remains), and **measured** in EC-6F. Owner approval
required because it bounds real-world recall.

**Rationale.** Two plugins execute concurrently with no ordering guarantee
and no cross-plugin source visibility; making this work requires the core
phase or shared-scope work of D-01 alternatives, which should be justified
by measured consumer need.

## D-19 — Property vocabulary discipline

**Decision (recommended).** New keys are added to `CodePropertyKeys` with
documented value sets; all values bounded; no new consumer-facing property
filters in EC-6B–F; property growth is reviewed like kind growth. Owner
approval required for the vocabulary set in architecture §7.2.

## D-20 — Consumer meaning of missing resolver facts

**Decision.** The absence of a resolver edge means "not verified under the
active policy", never "no dependency/binding exists". This sentence must
appear in consumer guidance in every milestone that ships a fact kind,
together with the qualified boundaries (bare specifiers, cross-plugin,
`.js`-specifier, dynamic, `.astro`).

**Rationale.** The governing principle and EC-4's consumer-limitation
precedent; absent facts are the dominant failure mode consumers will
misread.

---

## Risks

| ID | Risk | Mitigation |
| --- | --- | --- |
| R-01 | `Resolved` claim challenged as overstatement for a policy-based resolver | D-03 is review-critical; fallback `Derived` is designed-in and uniform; owner decides |
| R-02 | Name-based symbol convergence makes export attribution ambiguous for common kinds | AC-01b documented and asserted; consumers use edges, not symbol identity alone; identity migration tracked |
| R-03 | Per-plugin scope (D-18) depresses real-world recall | EC-6F measures; docs state limits; cross-plugin is a designed next step |
| R-04 | Policy `relative-v1` diverges from TypeScript behavior in edge cases | Ordered policy table + explicit deviation list; each extension gets its own policy level and fixtures |
| R-05 | Full re-resolution cost on very large repositories | Bounds + degradation policy (D-17); EC-6E validates; dependency tracking deferred |
| R-06 | Local export lists lose the dual-projection case (D-05) | Counted ambiguous; direct exports (the common form) unaffected |
| R-07 | Test rescoping hides real regressions | Roadmap mandates intent-preserving rescope; reviewers audit the diff and negative assertions |
| R-08 | Property vocabulary grows without discipline | D-19; bounded values; review per milestone |
| R-09 | Coverage field overloading (D-14) confuses consumers | Architecture §12.2 table is normative; reviewer may escalate to additive fields |
| R-10 | Star/re-export semantics diverge from TypeScript in exotic cases | Conservative conflict/cycle rules; adversarial cases AC-12–AC-16 are fixtures |

## Open questions for the design review

| ID | Question | Carried to |
| --- | --- | --- |
| O-01 | Accept `Resolved` (D-03) or select `Derived`? Exact wording for consumer docs? | design review — blocking |
| O-02 | Should `exports` include name-based symbol kinds at all, or exclude them until identity migration? | design review / owner |
| O-03 | Should `UnresolvedRelationships` include all unresolved categories (including mismatch), or only module-not-found? | EC-6B implementation |
| O-04 | Should star-derived per-name edges exist, or is one star marker plus consumer traversal preferable? | design review (EC-6D scope) |
| O-05 | Final kind names (`resolves-to`/`exports`/`binds`) versus alternatives (`import-target`, `imports-symbol`) | owner (vocabulary) |
| O-06 | Resolver provider id distinct from plugin id (recommended) or shared with version suffix? | design review |
| O-07 | Degradation order and guidance limit (D-17) — exact numbers and preference order | owner / EC-6E |
| O-08 | Should `type-only` be tri-state (explicit type-only / explicit value / unspecified) instead of presence-only? | EC-6D implementation |
| O-09 | Should `via-module` selection under multi-path star convergence be first-ordinal (proposed) or omitted? | EC-6D implementation |

## Owner approval summary

Approval is required for: D-03 (evidence meaning), D-04 and D-19 (public
vocabulary), D-05 and O-02 (representation truthfulness tradeoff), D-15
(query semantics, especially impact), D-17 (large-repository behavior),
D-18 (scope limitation), and any eventual decision to implement the
post-EC-6F candidates.
