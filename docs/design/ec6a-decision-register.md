# EC-6A: Decision register

**Status:** design artifact for EC-6A, reconciled under EC6-A1. Entries marked
**OWNER ACCEPTED** are decided for the named scope; every other entry remains
a proposal awaiting its gate. None is implemented.
**Baseline:** `558f8ac` (EC6-A0). Acceptance record:
[ec6a-owner-acceptance.md](../decisions/ec6a-owner-acceptance.md).

Status vocabulary:

- **OWNER ACCEPTED** — validated by the independent review and accepted by
  the owner for the named milestone scope; implementable without further
  approval. The scope column is exact: acceptance does not extend to later
  milestones.
- **RECOMMENDED** — the architecture document recommends this; the design
  reviewer should accept or reject it with rationale.
- **OWNER APPROVAL REQUIRED** — touches public vocabulary, evidence meaning,
  consumer-visible semantics, or declared scope; must not be implemented on
  design-review acceptance alone.
- **DEFERRED** — deliberately excluded from EC-6B–F; recorded so later work
  starts from a decision, not an accident.
- **OPEN** — not yet resolved; listed for its gate to close or carry forward.

| ID | Decision | Status | Milestone |
| --- | --- | --- | --- |
| D-01 | Resolver placement | OWNER ACCEPTED (EC-6B) | EC-6B |
| D-02 | Module identity | OWNER ACCEPTED (EC-6B) | EC-6B |
| D-03 | Evidence quality of resolver facts | OWNER ACCEPTED (EC-6B, `resolves-to`); `exports`/`binds` gate | EC-6B / EC-6C/D |
| D-04 | Edge kind vocabulary | OWNER ACCEPTED (EC-6B subset: `resolves-to`); later kinds proposed | EC-6B/C/D |
| D-05 | Name-based symbol targets and dual projections | RECOMMENDED + OWNER APPROVAL REQUIRED (F01 correction mandatory) | EC-6C |
| D-06 | Export of imported bindings | DEFERRED | post-6F |
| D-07 | Import binding rules and type-only semantics | RECOMMENDED | EC-6D |
| D-08 | `.js` → `.ts` extension substitution | DEFERRED | post-6F |
| D-09 | `tsconfig` paths/baseUrl/module modes | DEFERRED | post-6F |
| D-10 | CommonJS surfaces and `require` bindings | OWNER ACCEPTED (EC-6B module edge) / DEFERRED (modeling) | EC-6B / post-6F |
| D-11 | Anonymous default exports | DEFERRED | post-6F |
| D-12 | Resolution unit identity and ownership | OWNER ACCEPTED (EC-6B) | EC-6B |
| D-13 | Invalidation model | OWNER ACCEPTED (EC-6B, qualified in EC-6E) | EC-6B |
| D-14 | Coverage counting | OWNER ACCEPTED (EC-6B; §12.2 normative) | EC-6B |
| D-15 | Query surface changes | OWNER ACCEPTED (no changes; impact stays closed) | EC-6B–F |
| D-16 | Version and invalidation identity | OWNER ACCEPTED (EC-6B) | EC-6B |
| D-17 | Bounds and degradation | OWNER APPROVAL REQUIRED (numbers) | EC-6E |
| D-18 | Cross-plugin TypeScript↔JavaScript resolution | OWNER ACCEPTED (deferred scope; measured in EC-6F) | EC-6F |
| D-19 | Property vocabulary discipline | OWNER ACCEPTED (EC-6B key subset) | EC-6B–D |
| D-20 | Consumer meaning of missing resolver facts | OWNER ACCEPTED (EC-6B–F) | EC-6B–F |

---

## EC6-A1 owner acceptance (2026-10-25)

**Evidence.** Independent review `EC6-GLM-AR1` (`review/ec6-glm-ar1`, tip
`c66ed60`; `docs/reviews/ec6-glm-ar1-architecture-review.md` and
`docs/reviews/ec6-glm-ar1-decision-reconciliation.md`; executed probes
P1–P6) returned READY WITH QUALIFICATIONS with no P0/P1 findings. The owner
accepted the reviewed architecture with four documentation corrections
(F02/F04/F05/F09). Binding record:
[ec6a-owner-acceptance.md](../decisions/ec6a-owner-acceptance.md). This
section records decisions; it does not convert deferred items into resolved
ones.

### Accepted decisions and exact scope

| ID | Accepted scope (exact) |
| --- | --- |
| D-01 | Resolution runs as a pass inside the existing Tree-sitter extraction session, after the relevant per-file extraction units, behind internal `ITreeSitterModuleResolver` with an ECMAScript implementation; relationships are emitted into the stable plugin-owned unit `{language}:modules`. No new core graph-resolution phase. A later parser-independent or cross-plugin layer remains an option, not a requirement |
| D-02 | Module target is the existing file node; no new module node kinds, no changes to existing file identities; EC-6B supports only the conservative repository-local relative subset |
| D-03 | `Resolved` for a successfully established `resolves-to` edge under the declared policy, with the mandated meaning sentence and resolver/policy/version provenance. A matching filename alone never justifies `Resolved`; when policy conditions fail, no edge is emitted. `exports`/`binds` classification remains at its EC-6C/EC-6D gates (F12) |
| D-04 | EC-6B adds only `resolves-to` (importing/re-exporting file → resolved target file). No new public node kinds; no `exports`/`binds` during EC-6B; later vocabulary remains proposed |
| D-10 | Literal `require('./m')` yields a `resolves-to` module edge (`module-reference: require`); CommonJS export/binding modeling remains deferred |
| D-12/D-13 | One stable `{language}:modules` unit per plugin, edges only, always re-emitted when the plugin executes (empty completion included); full rebuild on any relevant indexed-input or resolution-policy change; no dependency-level incremental optimization in EC-6B — correctness and deterministic replacement take precedence |
| D-14 | The §12.2 counting table is normative; resolver kinds join the Tree-sitter coverage list; no new coverage fields |
| D-15 | Existing impact presets stay unchanged; adding `resolves-to` must not silently expand impact interpretation; existing structural `imports` remain available |
| D-16 | A single emitted-fact revision governs resolver-fact changes; provider version `{packageVersion}+resolver{ResolverRevision}.{policy}` changes in lockstep; no second invalidation axis |
| D-18 | EC-6B operates within the source inventory of the owning Tree-sitter plugin; cross-plugin TS/JS resolution is not claimed; missing out-of-inventory targets are never represented as verified source dependencies; limitation documented and measured in EC-6F |
| D-19 | EC-6B property keys `specifier`, `module-reference`, `resolution-policy`, `resolution-rule`, `type-only` accepted; values bounded; further keys proposed |
| D-20 | Absence of a resolver edge means "not verified under the active policy", never "no dependency exists"; the sentence is required in consumer documentation |

### GLM finding dispositions

| Finding | Disposition | Normative location |
| --- | --- | --- |
| F02 staging/replacement ordering | Reconciled: emission order, staged-vs-published facts, lifecycle scenarios | architecture §9.1.1–§9.1.3; AC-41–AC-45 |
| F04 normalization/classification | Reconciled: normative normalization, ordered matching, input classification table | architecture §6.2; roadmap EC-6B fixtures |
| F05 cancellation/determinism/failure | Reconciled: determinism, token handling, failure taxonomy, reused resource limits | architecture §9.4; AC-45, AC-51, AC-52 |
| F09 identity/multiplicity | Reconciled for `resolves-to`; full per-kind contract gated at EC-6C/D | architecture §7.1.1; AC-46–AC-53 |
| F01 name-based attribution | Deferred with a mandatory correction at EC-6C/D (below) | D-05; roadmap F01 gates |
| F03, F08, F11 | No issue found; recorded, no action | GLM review |
| F06 coverage persistence | Deferred to platform work (below) | — |
| F07 star type-only propagation | Deferred to the EC-6D design (below) | O-04 |
| F10 all-files-deleted | Deferred fixture to EC-6E | architecture §9.1.3 |
| F12 export/binding evidence gates | Preserved: classification settled at EC-6C/D under preconditions | architecture §8; D-03 |

**Identifier note (from the GLM reconciliation report).** The review brief's
§15 labels for D-07 ("resolution-unit ownership") and D-14 ("publication
assumptions") do not match this register, where D-12/D-13 own resolution-unit
ownership/invalidation, D-07 owns import binding rules, and D-14 owns
coverage counting. The acceptance record and this register assess the actual
register entries and cover the brief's intent under both IDs.

### Deferred decisions with future gates

| Item | Original source | Reason deferred | Required milestone | Blocking condition | Evidence for acceptance |
| --- | --- | --- | --- | --- | --- |
| F01 attribution of converged name-based identities | GLM F01; D-05; O-02 | EC-6B emits no export/binding facts; attribution needs its own design | EC-6C design; EC-6D for `binds` | Any `exports`/`binds` edge targeting a converged kind | Chosen option (declaring-module property or eligibility restriction) plus fixtures proving attribution |
| Export binding attribution and eligibility | Task-deferred list | Requires F01 | EC-6C/D | Shipping export/bind facts | Same as F01 |
| Distinct export/bind identities (full F09 contract) | GLM F09 | Kinds not in EC-6B | EC-6C/D | Any `exports`/`binds` edge | Per-kind discriminator contract plus alias/collision fixtures |
| F07 star/re-export type-only propagation; O-04 representation | GLM F07 | EC-6D scope | EC-6D design | Star/namespace re-export emission | Fixtures, or a documented conservative refusal |
| Re-export cycle closure completeness | GLM §G | EC-6D scope | EC-6D design | Star/re-export closure shipping | Cycle and order-independence fixtures; under-approximation documented |
| Export of imported bindings; anonymous defaults; CJS binding semantics | D-06, D-10 (modeling), D-11 | Separable identity work; dynamic export objects | post-6F | Milestone authorized | Design plus fixtures |
| `.js`→`.ts` substitution (`relative-v2`) | D-08 | Mode semantics not emulated | post-6F | Policy extension authorized | Policy design, fixtures; recall measured in EC-6F |
| `tsconfig` paths/baseUrl/module modes | D-09 | Distinct trust/bounds/invalidation capability | post-6F | Explicit opt-in design exists | Config identity and config-hash invalidation fixtures |
| D-17 numbers and degradation order; O-07 | Register; GLM | Needs measurement; no invented benchmarks | EC-6E | Large-repository qualification | Fixture near the limit plus owner acceptance of numbers |
| F06 / persisted coverage diagnostics | GLM F06 | Coverage exists only in extraction results | post-6F platform work | Consumer need | Additive API design |
| F10 all-files-deleted lifecycle | GLM F10 | Qualification | EC-6E | EC-6E | Fixture passes on both stores |
| Measured performance limits; full incremental and durability qualification | R-05; EC-6E scope | Measurement | EC-6E/EC-6F | Qualification | Evidence report with commands and counts |
| Cross-plugin resolution (D-18 evolution) | Register | Requires core-assisted ordering/visibility | post-6F | Owner authorization | Design with ordering guarantees; F11 constraint respected |
| Query conveniences, preset/impact changes, optimized dependency tracking | D-15; post-6F candidates | No measured consumer need | post-6F | Explicit decision | Additive API review |
| Identity migration for name-based symbols | D-05 consequence; roadmap post-6F item 6 | Re-extraction milestone of its own | post-6F | F01 option 3 chosen | Migration design and evidence |

### Open-question dispositions under EC6-A1

| ID | Disposition |
| --- | --- |
| O-01 | CLOSED: `Resolved` accepted for `resolves-to` with the mandated wording; `Derived` remains recorded as the fallback for later kinds |
| O-02 | Carried to EC-6C/D: must be settled with F01 before `exports`/`binds` ship |
| O-03 | Carried to EC-6B implementation (documented and tested there) |
| O-04 | Carried to the EC-6D design (F07), conservative refusal acceptable |
| O-05 | Partially closed: `resolves-to` accepted (A4); `exports`/`binds` names remain owner items at their milestones |
| O-06 | CLOSED: distinct resolver provider id accepted as A3 provenance |
| O-07 | Owner item at EC-6E (numbers and preference order) |
| O-08 | Carried to EC-6D implementation |
| O-09 | Carried to EC-6D implementation |

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

**EC6-A1 disposition.** Accepted by the owner for `resolves-to` with the
mandated policy sentence (architecture §8). The `Derived` alternative remains
recorded and would change no structure if later selected for other kinds;
`exports`/`binds` classification stays open under F12.

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

**EC6-A1 disposition.** The owner accepted only the EC-6B subset —
`resolves-to`. The `exports`/`binds` names and semantics remain proposals
gated at EC-6C/EC-6D with F01/F07/F12 conditions.

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

**EC6-A1 disposition.** Not accepted for EC-6B (no export or binding facts).
The F01 correction (declaring-module attribution or eligibility restriction)
is mandatory before any `exports`/`binds` edge targets a converged name-based
kind; the deferred-gates table above records the acceptance evidence.

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

**EC6-A1 disposition.** Accepted (A5): full rebuild of the plugin-owned
`{language}:modules` unit on any relevant indexed-input or policy change;
no dependency-level incremental optimization in EC-6B.

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

**EC6-A1 disposition.** The owner acknowledged the per-plugin scope
limitation (A7): EC-6B operates within the owning plugin's source inventory,
missing out-of-inventory targets are never represented as verified source
dependencies, and the limitation is documented and measured in EC-6F.

## D-19 — Property vocabulary discipline

**Decision (recommended).** New keys are added to `CodePropertyKeys` with
documented value sets; all values bounded; no new consumer-facing property
filters in EC-6B–F; property growth is reviewed like kind growth. Owner
approval required for the vocabulary set in architecture §7.2.

**EC6-A1 disposition.** The owner accepted the EC-6B key subset —
`specifier`, `module-reference`, `resolution-policy`, `resolution-rule`,
`type-only` — as public vocabulary; later keys remain proposals at their
milestones.

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
| R-01 | `Resolved` claim challenged as overstatement for a policy-based resolver | Closed for EC-6B: owner accepted `Resolved` with the mandated policy sentence (A3, §D of the GLM review); the `Derived` fallback stays recorded for later kinds |
| R-02 | Name-based symbol convergence makes export attribution ambiguous for common kinds | F01 correction mandatory at EC-6C/D (declaring-module attribution or eligibility restriction) before `exports`/`binds` ship; AC-01b documented; identity migration tracked |
| R-03 | Per-plugin scope (D-18) depresses real-world recall | EC-6F measures; docs state limits; cross-plugin is a designed next step |
| R-04 | Policy `relative-v1` diverges from TypeScript behavior in edge cases | Ordered policy table + explicit deviation list; each extension gets its own policy level and fixtures |
| R-05 | Full re-resolution cost on very large repositories | Bounds + degradation policy (D-17); EC-6E validates; dependency tracking deferred |
| R-06 | Local export lists lose the dual-projection case (D-05) | Counted ambiguous; direct exports (the common form) unaffected |
| R-07 | Test rescoping hides real regressions | Roadmap mandates intent-preserving rescope; reviewers audit the diff and negative assertions |
| R-08 | Property vocabulary grows without discipline | D-19; bounded values; review per milestone |
| R-09 | Coverage field overloading (D-14) confuses consumers | Architecture §12.2 table is normative; reviewer may escalate to additive fields |
| R-10 | Star/re-export semantics diverge from TypeScript in exotic cases | Conservative conflict/cycle rules; adversarial cases AC-12–AC-16 are fixtures |

## Open questions for the design review

| ID | Question | Status / carried to |
| --- | --- | --- |
| O-01 | Accept `Resolved` (D-03) or select `Derived`? Exact wording for consumer docs? | CLOSED by EC6-A1: `Resolved` accepted for `resolves-to` with the mandated sentence; `Derived` recorded as the fallback for later kinds |
| O-02 | Should `exports` include name-based symbol kinds at all, or exclude them until identity migration? | Carried to EC-6C/D with F01 (no exports in EC-6B) |
| O-03 | Should `UnresolvedRelationships` include all unresolved categories (including mismatch), or only module-not-found? | EC-6B implementation (documented and tested there) |
| O-04 | Should star-derived per-name edges exist, or is one star marker plus consumer traversal preferable? | EC-6D design (F07; conservative refusal acceptable) |
| O-05 | Final kind names (`resolves-to`/`exports`/`binds`) versus alternatives (`import-target`, `imports-symbol`) | Partially closed: `resolves-to` accepted (A4); `exports`/`binds` names at their milestones |
| O-06 | Resolver provider id distinct from plugin id (recommended) or shared with version suffix? | CLOSED by EC6-A1: distinct provider id accepted as A3 provenance |
| O-07 | Degradation order and guidance limit (D-17) — exact numbers and preference order | Owner item at EC-6E |
| O-08 | Should `type-only` be tri-state (explicit type-only / explicit value / unspecified) instead of presence-only? | EC-6D implementation |
| O-09 | Should `via-module` selection under multi-path star convergence be first-ordinal (proposed) or omitted? | EC-6D implementation |

## Owner approval summary

Satisfied for EC-6B by EC6-A1: D-01–D-04 (EC-6B scope), D-10 (module-edge
boundary), D-12–D-16, D-18, D-19 (EC-6B key subset), D-20. Still required at
their gates: D-05 and O-02 with F01 (EC-6C/D), D-07 and O-04/O-08/O-09
(EC-6D), D-17 and O-07 numbers (EC-6E), and any eventual decision to
implement the post-EC-6F candidates.
