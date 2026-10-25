# EC-6A: Implementation roadmap for cross-file resolution

**Status:** design artifact for EC-6A, reconciled under EC6-A1. The
independent review EC6-GLM-AR1 returned READY WITH QUALIFICATIONS and the
owner accepted the EC-6B decisions and the F02/F04/F05/F09 corrections
([owner-acceptance record](../decisions/ec6a-owner-acceptance.md)); the review
gate on EC-6B is therefore satisfied. EC-6B was subsequently implemented,
remediated, and accepted with qualifications (EC6B-OA2), so only EC-6C
planning is eligible under a separate assignment. Milestones beyond EC-6B
remain
**proposals**, and **implementation authorization is a separate owner
decision.** This artifact did not start EC-6B; its acceptance is recorded
in [the EC-6B owner decision](../decisions/ec6b-owner-acceptance.md).
**Baseline:** `558f8ac` (EC6-A0).
Companion documents:
[architecture](ec6a-cross-file-resolution-architecture.md),
[adversarial cases](ec6a-adversarial-cases.md),
[decision register](ec6a-decision-register.md).

Every milestone in this roadmap is:

- **separately shippable** (its facts are useful without later milestones),
- **separately reviewable** (its own adversarial probes, its own review
  branch, preserved evidence),
- **economically sized** for one implementation pass by DeepSeek or Luna,
- **gated** by the previous milestone's independent review verdict.

## Milestone overview

| Milestone | Goal | New graph vocabulary | Profile revision | Size | Depends on |
| --- | --- | --- | --- | --- | --- |
| EC-6B | Module-resolution contracts + conservative local module resolution | `resolves-to` + 5 property keys (accepted) | p6 → p8 (p7; R1) | S | EC6-A1 owner acceptance (satisfied) |
| EC-6C | Export surface a module owns by itself | `exports` + 4 property keys | p8 → p9 | S–M | EC-6B |
| EC-6D | Re-export/star closure + import binding | `binds` + 3 property keys | p9 → p10 | M | EC-6C |
| EC-6E | Incremental publication and durability qualification | none | none (unless a defect fix changes facts) | S (verification-heavy) | EC-6D |
| EC-6F | Real-repository qualification | none | none | M | EC-6E |
| *(unauthorized)* EC-6G+ | Policy extensions, query conveniences, identity migration | to be designed | n/a | L | Owner decision |

Profile revisions: EC-6B shipped p7 and the EC-6B-R1 corrective pass advanced
the effective profile to p8 because emitted resolver facts changed. EC-6C
therefore starts from p8 and the EC-6D transition shifts accordingly; exact
revisions are re-confirmed at each design gate.

Common disciplines that apply to every emitting milestone:

1. **Revision discipline.** Any change to emitted facts bumps
   `EcmaScriptExtractionProfile.ProfileRevision` (single counter; documented
   as the emitted-fact revision) because plugin versions fold it and the
   planner invalidates every source on a version change
   (`src/Penghou.Hetu.TreeSitter/TreeSitterTypeScriptPlugin.cs:23-27`,
   `src/Penghou.Hetu/CodeIndexPlanner.cs:204-208`). Resolver provider version
   changes with it (architecture §8).
2. **Coverage discipline.** New kinds join the session coverage list
   (`src/Penghou.Hetu.TreeSitter/TreeSitterExtractionSession.cs:24-34`) and
   obey `EdgesEmitted == InternalTargets + CrossProjectTargets`
   (`src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs:204-209`).
3. **No identity changes.** No existing symbol, declaration, file, or edge
   identity may change; no migration is allowed inside this series.
4. **No new node kinds.** Resolution adds edges only.
5. **Evidence honesty.** Resolver edges are `Resolved` and carry the resolver
   provider id/version; existing Structural facts are untouched. The
   `calls`-requires-`Resolved` validator rule stays the only per-kind rule;
   the new kinds' evidence discipline is enforced by fixtures.
6. **Public API.** Additive `CodeEdgeKinds`/`CodePropertyKeys` constants do
   not change the exported-type snapshots
   (`tests/Penghou.Hetu.Tests/PublicApiContractTests.cs:11-66` lists types
   only). Any new public method is a separate reviewed task, never a
   side-effect of these milestones.
7. **Docs per milestone:** update `docs/language-feature-matrix.md`
   (`module.import`, `module.export`, `relationship.alias` rows and audit
   findings), `docs/consumer-guidance-qualified-name-lookup.md` (new module
   section), `CHANGELOG.md`, and the language roadmap status. No identity-doc
   change is needed unless identities change.
8. **Evidence handoff:** each milestone commits a handoff document under
   `docs/evidence/` and an independent review preserves its probes on a
   `review/...` branch (never merged), mirroring the EC-3D/EC-4/EC-5 pattern.

### Test reconciliation required by every emitting milestone

Adding Resolved edges to extraction output invalidates blanket "all edges are
Structural" / "all edges use the plugin provider" assertions in fixtures that
contain imports or exports. At baseline these are (audited):

| Test | Assertion | Milestone that first affects it |
| --- | --- | --- |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs:212-215` | all edges Structural + provider = plugin | EC-6B (fixture has imports) |
| `TreeSitterTypeScriptPluginTests.cs:305, 468, 529` | same pattern | EC-6B/C |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterJavaScriptPluginTests.cs:117, 183` | same pattern | EC-6B |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaVariableTests.cs:88` | all edges Structural | EC-6C (fixture exports variables) |
| `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaClassMemberTests.cs:300` | all edges Structural | EC-6C if fixture exports |
| Coverage-shape assertions (produced-kind lists, counts) in plugin tests and `TreeSitterCapabilityContractTests` | lists/counts of produced kinds | EC-6B (new coverage kinds) |

The rescope must preserve intent: each affected test should still assert that
**existing** facts keep Structural evidence and the extraction provider, and
should add explicit assertions that resolver edges are exactly the new kinds
with `Resolved` evidence and the resolver provider. Weakening or deleting the
structural claims is not acceptable. `Assert.Equal(0,
Result.UnresolvedRelationships)` assertions stay valid for fixtures whose
imports are external only (external sites are not unresolved; see the
counting table in architecture §12.2), but each fixture must be re-verified
rather than assumed.

---

## EC-6B — Module-resolution contracts and conservative local resolution

**Goal.** Introduce the resolver seam and the `resolves-to` fact for
**relative specifiers only**, with no export table and no bindings. After
EC-6B, "which modules does this file depend on / who depends on this file"
is answerable offline for a single language's files.

**Scope (in).**

- Accepted constants (A4): `CodeEdgeKinds.ResolvesTo`; property keys
  `Specifier`, `ModuleReference`, `ResolutionPolicy`, `ResolutionRule`,
  `TypeOnly`.
- Internal seam `ITreeSitterModuleResolver` in the shared Tree-sitter
  session; `EcmaScriptModuleResolver` implementation (module resolution
  only) in the TreeSitter package; module-shape capture for import sites and
  re-export-from sites (statement forms only) — A1.
- Owner-accepted policy `relative-v1` exactly as architecture §6.2:
  normalization, the input classification table, root-escape refusal, the
  ordered suffix list (including `.d.ts`), directory index, and inventory
  restricted to the plugin's decodable sources (A2/A7, F04).
- Resolution unit `{language}:modules` (edges only, always re-emitted when
  the session runs; empty completion clears it otherwise), owned by the
  existing plugin; full rebuild on any relevant change (A5, F02).
- The normative emission order and staged-vs-published semantics of
  architecture §9.1.1–§9.1.3 (F02), the determinism/cancellation/failure
  contract of §9.4 (F05), and the `resolves-to` identity contract of §7.1.1
  (F09).
- Resolver evidence identity `penghou.hetu.tree-sitter.ecmascript-module-
  resolver` and version `{packageVersion}+resolver{ResolverRevision}.{policy}`
  (A3, architecture §8); profile revision p6 → p7.
- Coverage kind `resolves-to` with the counting table of architecture
  §12.2.
- Consumer documentation of the mandated `Resolved` sentence and the
  absence semantics (D-20), and the handoff.

**Scope (out).** `exports`/`binds` edges; export tables; name resolution of
any kind; `tsconfig`; `.js`→`.ts` substitution; CJS bindings; cross-plugin
resolution; preset or query changes; degradation numbers.

**Deterministic fixtures (positive).**

1. `./a` → `a.ts` (extension substitution, exact property values).
2. `../lib/util` → `lib/util.ts` (normalization).
3. `./dir` → `dir/index.ts` (directory index).
4. `./b.ts` → exact rule.
5. `./types` → `types.d.ts` (declaration suffix in the ordered list; AC-40).
6. `import type { T } from './t'` → `type-only: true`, `module-reference:
   import-type`.
7. `const x = require('./c.mjs')` (JavaScript plugin) → `module-reference:
   require`, no binding.
8. `export { x } from './m'`, `export type { T } from './m'`, and `export *
   from './m'` → module edges with `module-reference` `export-from` /
   `export-type-from` / `export-star`.
9. Two importers, same specifier, different directories → two distinct
   `resolves-to` edges (the EC-4 D3 defect fixed at the module level).
10. Determinism: two extractions of identical input produce identical edge
    ids; repeated indexing is a no-op returning the previous publication
    (AC-51).
11. Self-import (`./self`) → InternalTargets counting; every other resolved
    target → CrossProjectTargets.
12. Two same-target occurrences in one file with distinct specifiers and
    distinct locations → separate edges with the §7.1.1 identity inputs
    (AC-46, AC-47).

**Fixtures (negative).**

1. `./nope` missing → no edge, unresolved count.
2. `../../../outside` escaping root → no edge, unsupported count.
3. `node:fs`, `zod`, `https://…`, `@/alias` → no edge, external count.
4. `./config.json`, `./x.astro` → unsupported count.
5. `./x.js` where only `x.ts` exists → no edge, unresolved count (D-08).
6. `./`, `./a//b`, `./a/` → no edge, unsupported count.
7. `.\a`, `./a\b` → no edge, unsupported count.
8. `./x?raw`, `./x#frag` → no edge, unsupported count.
9. `.` and `..` bare → no edge, external count.
10. `/abs/x` → no edge, unsupported count.
11. A specifier over 256 chars → no edge, unsupported count, no retained
    text.
12. dynamic `import('./' + x)`, template `require` → no edge, unsupported.
13. A `.ts` specifier that names a JavaScript-owned file → unresolved
    (per-plugin inventory), no edge (AC-39).
14. `./Lib/x` where the file is `lib/x.ts` → unresolved (ordinal
    comparison, no filesystem probe).

**Failure cases.** Undecodable source contributes no inventory entry;
parse-error runs mark resolver kinds `Partial`; endpoint validation must
never reject a correct resolution unit because file units are staged first
(AC-41/AC-42); a resolver failure after per-file staging retains the prior
publication (AC-44); cancellation before publication retains the prior
publication and never emits a partial unit (AC-45); identity collisions fail
loudly (AC-53). The full taxonomy is architecture §9.4.3.

**Acceptance criteria.**

- Focused fixtures + full solution suite pass with the rescoped assertions;
  `dotnet format --verify-no-changes`; `git diff --check` clean.
- No existing identity changes (assertion: every pre-existing node,
  declaration, and edge id is byte-identical between p6 and p7 outputs for
  unchanged sources; the diff is exactly the new `resolves-to` edges and
  their coverage).
- No `exports`/`binds` edges are emitted (negative assertion).
- F02: lifecycle fixtures AC-41–AC-45 pass on both stores; staging order is
  asserted by a wrong-order rejection test (resolution unit before file
  units fails with `graph.edge.endpoint-missing`).
- F04: every row of the architecture §6.2.3 classification table has a
  fixture or an explicitly documented exclusion; no contradictory
  unresolved/unsupported outcome remains.
- F05: determinism is asserted by byte-identical repeated extraction and a
  cancellation fixture; the resolver observes the token between
  files/modules and before batch emission.
- F09: identity fixtures AC-46–AC-53 pass; every emitted edge's id is
  recomputed independently from `(kind, source, target, discriminator,
  span)`.
- Evidence: every `resolves-to` edge is `Resolved`, carries the resolver
  provider id/version, and the mandated policy sentence appears in consumer
  docs and the identity doc; no pre-existing fact changes quality.
- Coverage invariants hold; `UnresolvedRelationships` semantics documented
  and tested.
- Docs updated; handoff committed.

**EC-6B acceptance matrix.** Every row is normative; "architecture §" points
to the exact rule, and each row names its fixture family and review gate.

| Capability | Exact input | Expected output | Evidence | Provenance | Identity | Negative cases | Lifecycle | Required test | Review gate |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Relative exact match | `./b.ts`, `../lib/util.ts` in inventory | `resolves-to` with `resolution-rule: exact` | `Resolved` | resolver provider + `relative-v1` + raw specifier | §7.1.1 inputs; span = statement | missing → unresolved | replacement removes stale ids | positive fixture 4 | EC6-A1 acceptance; EC-6B review |
| Extension substitution | extensionless path, ordered suffixes | first matching `<path><suffix>` edge with `extension-substitution` | `Resolved` | same | suffix order is policy, recorded per edge | `./x.js` only `.ts` → unresolved (D-08); no arbitrary fallback | target add/delete AC-41/42 | positives 1, 5; negative 5 | EC-6B review |
| Directory index | extensionless dir, no file match | `<dir>/index<suffix>` edge with `directory-index` | `Resolved` | same | file-before-index precedence | missing index → unresolved; `package.json` never read | target add/delete | positive 3 | EC-6B review |
| Re-export module reference | `export … from './m'`, `export * from './m'`, `export * as ns from './m'` | module edge with matching `module-reference`; no export/binding facts in EC-6B | `Resolved` | same | statement span | unresolved target → no edge | full rebuild triggers | positive 8 | EC-6B review |
| Type-only forms | `import type … from`, `export type … from` | module edge with `type-only: true` | `Resolved` | same | presence-only property (absent otherwise) | value/type mismatch cases stay EC-6D | n/a | positive 6 | EC-6B review |
| Literal `require` | `require('./c.mjs')` in JavaScript plugin | module edge with `module-reference: require`; no binding | `Resolved` | same | call-expression span | dynamic/template require → unsupported | n/a | positive 7; negative 12 | EC-6B review |
| External specifiers | bare, `node:`, URL, alias | no edge; `external-targets` | n/a | n/a | n/a | none needed | n/a | negative 3 | EC-6B review |
| Rejected/unsupported syntax | escape, absolute, `//`, `\`, `?`, `#`, overlong, out-of-policy extension | no edge; `unsupported-targets` | n/a | n/a | no text retained for overlong | all rows of §6.2.3 | n/a | negatives 2, 4, 6–11 | EC-6B review |
| Unresolved targets | policy match absent | no edge; `unresolved-targets` | n/a | n/a | n/a | cross-plugin and case-mismatch variants | unresolved becomes resolved on add (AC-42) | negatives 1, 13, 14; positive by AC-42 | EC-6B review |
| Identity and multiplicity | repeated/aliased imports, shared targets | distinct deterministic edges per written occurrence | `Resolved` | same | §7.1.1 exact inputs and location conventions | collision → loud failure (AC-53) | removal/shift fixtures AC-49/50 | AC-46–AC-53 | EC-6B review |
| Lifecycle and publication | add/delete/empty/fail/cancel sequences | staged facts invisible; published graph exactly per AC-41–AC-45 | `Resolved` where emitted | unit origin and run provenance | replacement removes stale ids | wrong order rejected loudly | AC-41–AC-45 on both stores | lifecycle fixtures | EC-6B review + EC-6E qualification |
| Coverage accounting | every captured site | §12.2 counters; `EdgesEmitted == Internal + CrossProject` | n/a | n/a | n/a | non-produced kinds report nothing | counts recomputed per rebuild | coverage fixture per outcome | EC-6B review |
| Determinism | identical inputs twice | byte-identical ids, properties, order, counts | `Resolved` | same | stable under byte-identical content | LF/CRLF equivalence | no-op reindex returns prior publication | AC-51, AC-52 | EC-6B review |

**Independent review.** Adversarial probes must include: specifier
normalization edge cases, the full classification table, escape refusal,
ordering of substitution versus index, inventory scope, case-only mismatch,
overlong behavior, unit replacement after deletion, empty replacement,
failed-run retention, cancellation before publication, wrong-order rejection,
evidence/provider/version assertions, identity/occurrence multiplicity,
LF/CRLF stability, collision rejection, and a byte-level audit that no
pre-existing fact moved. Verdict recorded on a preserved review branch.

---

## EC-6C — Export surface a module owns by itself

**Goal.** Represent what a module exports **directly**: declarations and
declarators, local lists and aliases, default exports in named/identifier
form, and namespace re-export of a resolved module. No cross-module chain
resolution yet (named re-export/star arrive in EC-6D), so the resolver never
consults another module's table in this milestone — but it does emit the
`resolves-to` edge for re-export statements (already in EC-6B).

**Entry conditions (EC6B-OA1 closure).** Planning may be commissioned under
a separate assignment. Implementation is not authorized by the EC-6B
closure; the EC-6C owner gate must accept the export contract before code.
EC-6B is accepted with qualifications but is not merged, so EC-6C must not
depend on an EC-6B merge.

**Scope (in).**

- Proposed constant `CodeEdgeKinds.Exports`, property keys `ExportedName`,
  `ExportKind`, `ExportVia` (`direct`/`local`/`namespace` at this
  milestone), `ViaModule` (namespace form only).
- Export shape capture for direct declarations (captured node ids), local
  lists, default named declarations, default identifiers, namespace
  re-exports.
- Unique-local-declaration rule and its ambiguity handling (dual projection
  → no edge; architecture §7.3).
- **F01 gate (from EC6-GLM-AR1):** before any `exports` edge targets a
  name-based symbol kind (function, type, enum, namespace), this milestone's
  design must adopt one of GLM's options — carry an explicit declaring
  module property (recommended `exported-from`), restrict eligibility to
  path-scoped kinds, or migrate identities — with fixtures proving
  attribution. Unattributed name-based targets must not ship.
- Coverage kind `exports`; profile revision p8 → p9.

**Scope (out).** Named re-export and star chains (`export-via: re-export` /
`star` and their tables) — EC-6D; import binding — EC-6D; export of an
imported binding — deferred (D-06); anonymous default and `export =` —
unsupported (D-10, D-11).

**Deterministic fixtures (positive).**

1. Each direct declaration kind exported: `const`/`let`/`var` declarators
   (including multi-declarator statements), class, interface, type alias,
   enum, namespace, function.
2. `export { a as b }` → target S(a), `exported-name: b`, `export-via:
   local`.
3. `export default function f` / `export default class C` →
   `export-kind: default`.
4. `export default localName` → unique-local rule.
5. `export * as ns from './m'` → target is M's file node,
   `export-kind: namespace`, plus `resolves-to` (EC-6B).
6. Duplicate same-file declarations (`var shared … export var shared`) → one
   node edge, direct-form captured target, no ambiguity.
7. Export edges targeting name-based symbol kinds (function/type/enum/
   namespace) with the AC-01b caveat documented and asserted.
8. Determinism and lifecycle: adding, removing, and renaming an export
   updates the resolution unit atomically; unrelated edits leave the
   resolution unit's edge ids unchanged.

**Fixtures (negative).**

1. `export { handler }` with dual `field`+`callable` projections → no edge,
   ambiguous count.
2. `export { missing }` (no local declaration) → no edge, unresolved count.
3. `export default () => 1` → no edge, unsupported count.
4. `export = value` → no edge, unsupported count.
5. An export-only file change with no import changes still rebuilds the
   resolution unit and removes stale export edges.

**Acceptance criteria.** As EC-6B, plus: EC-6B facts are byte-identical
except for the new `exports` edges; no `binds` edges; `exports` coverage
counts satisfy the invariant; consumer guidance documents export edges vs
the `export` modifier distinction (written modifier remains Structural).

**Independent review.** Probes must include every export form, the dual
projection ambiguity, name-based identity convergence honesty, default
handling, namespace re-export targeting, lifecycle replacement, and
determinism.

---

## EC-6D — Re-export/star closure and import binding

**Goal.** Complete the module model: named re-export chains, `export *`
closure with shadowing/conflict/cycle rules, and import bindings for named,
default, and namespace imports with explicit type-only semantics.

**Scope (in).**

- Proposed constant `CodeEdgeKinds.Binds`, property keys `ImportKind`,
  `ImportedName`, `LocalName`, `TypeOnly` (already introduced), `Specifier`.
- Export table algorithm with memoization, visited-set cycle cut, star
  conflict detection, local shadowing, default exclusion from stars,
  deterministic convergence (`via-module` selection) — architecture §11.2.
- Binding algorithm and type/value rules (architecture §7.4).
- `export-via` values `re-export` and `star` and their `via-module`
  properties.
- **F07 gate:** specify whether star-derived `exports` edges preserve the
  source export's `type-only` flag and how namespace re-exports interact
  with type-only syntax (O-04), with fixtures; conservative refusal is
  acceptable.
- **F01 gate carries here:** no `binds` edge may target a converged
  name-based node unless EC-6C's attribution decision makes the edge
  attributable; order-independent star closure and the brief's cycle
  example must be fixtures (GLM §G).
- Coverage kinds `binds`; profile revision p9 → p10.

**Scope (out).** CJS bindings; export of imported bindings; dynamic module
references; `tsconfig`; cross-plugin resolution.

**Deterministic fixtures (positive).**

1. `import { a, b as c }` → two bindings with imported/local names.
2. `import d from './m'` → default binding to the default export.
3. `import * as ns from './m'` → binding targeting M's file node.
4. Three-hop re-export chain (AC-12) with per-hop `resolves-to`,
   intermediate `exports` edges, and final-symbol binding.
5. Star closure: per-name edges, default exclusion, local shadowing,
   duplicate convergence from two paths to the same target (single edge).
6. Type-only matrix: `import type` of type-only export; `import type` of
   value export; value import of type-only export (no binding).
7. Multiple files importing the same export → distinct `binds` edges with
   per-file locations; incoming traversal yields all importers.
8. `.d.ts` target: module edge yes, binding no (honest absence).

**Fixtures (negative).**

1. Missing export name in a resolved module → no binding, unresolved count.
2. Conflicting star exports (AC-14) → no export edge for the name, no
   binding, ambiguous counts.
3. Re-export cycle (AC-13) → termination, no fabricated edges.
4. `module.exports` / `exports.x` → unsupported; `require` bindings absent.
5. Dynamic `import()` / `import x = require()` → unsupported.
6. Cross-plugin TS→JS import (AC-39) → unresolved at module level; the
   limitation is asserted, not silent.
7. Value import of type-only export → no binding.
8. Export of imported binding → unsupported at this milestone.

**Failure cases.** Star conflict resolution must be order-independent; the
visited-set cut must be proven by a cycle fixture that would otherwise hang;
binding ambiguity must never prefer one candidate; a renamed export must
remove the old binding in the same publication.

**Acceptance criteria.** As before, plus: EC-6B/6C facts byte-identical
except new `binds` edges and the `export-via: re-export/star` additions;
full adversarial case set (all AC cases relevant to B–D) has fixtures or
documented exclusions; consumer guidance contains the module section with
the "absence is not evidence of absence" statement.

**Independent review.** This is the highest-risk milestone: evidence-
quality rationale (D-03), chain semantics, star conflicts, type-only rules,
and cross-plugin boundaries must be adversarially probed. Preserve probes
and verdict on a review branch.

---

## EC-6E — Cross-file incremental publication and durability qualification

**Goal.** Prove on durable and in-memory stores that resolver facts obey
every existing lifecycle guarantee, and fix any defect the proof reveals.
This milestone is verification-first; it is expected to change no emitted
fact (no revision bump unless a defect fix changes them).

**Scope (in).**

- Multi-step lifecycle qualification against `CodeIndexingService` +
  `InMemoryCodeGraphStore` and `LatticeCodeGraphStore`: add/change/delete/
  rename files; export add/remove; import add/remove; policy/version bump
  (simulated by a stored manifest with the old version, as EC-5 did).
- Failure and interruption: plugin crash mid-session (resolution unit
  never half-published), store rejection, cancellation, superseded run.
- Unchanged reindex: byte-identical resolution unit, previous publication
  returned, `PluginsExecuted = 0`.
- Snapshot export/import round-trip containing resolver edges.
- Lattice close/reopen durability across separate processes.
- Bounds: a synthetic fixture near the degradation limit validates the
  policy of architecture §12.1 (warning code + coverage honesty, no silent
  truncation, no stale edges).
- **F10 fixture:** all files of a plugin deleted — the empty resolution unit
  is displaced by cross-run ownership hygiene and no resolver fact survives
  (architecture §9.1.3).
- Conformance additions only if a store behavior proves unspecified; any
  such addition must itself pass both stores.

**Scope (out).** New facts; policy extensions; query APIs; performance
benchmarks beyond the bounded fixture.

**Acceptance criteria.** Documented evidence report with exact commands and
counts; all lifecycle laws verified; no stale edge after any sequence;
failed runs retain the prior resolution unit; no revision bump unless a fix
demanded one (then restart the evidence chain).

**Independent review.** Verification audit: the reviewer must reproduce at
least the deletion, rename, policy-bump, and failed-run scenarios on both
stores and challenge the degradation policy.

---

## EC-6F — Real-repository qualification

**Goal.** Qualify the complete module model on a real TypeScript/JavaScript
repository, mirroring EC-4.

**Scope (in).**

- Target: the `jenolaszlo-sketch/website` snapshot at a pinned commit
  (reuse or advance from `f9cca1773a32bb517e624cc380e1f6d14e0cc317`), plus
  at least one additional pinned repository with a materially different
  import/export mix if available offline.
- Independently assembled source-ground-truth inventory of imports/exports
  before running the extractor (per file: specifier, form, bindings,
  export name/form) — never derived from emitted edges.
- Precision/recall per fact kind over the explicitly enumerated eligible
  subset; zero tolerance for false bindings (precision target 100% for
  emitted resolver edges, with every miss categorized); recall measured and
  explained (bare specifiers, cross-plugin, `.js`-specifier, dynamic,
  `.astro`).
- p(n−1) → p(n) authentic migration on a durable store with the real prior
  extractor, if a version bump applies.
- Adversarial probes (18+ family, EC-5 style) exercised on the real
  snapshot's shapes.
- Qualification report under `docs/evidence/`; independent review.

**Scope (out).** Fixing coverage gaps beyond the declared policy (they are
recorded, not patched); `.astro`; cross-plugin; tsconfig.

**Acceptance criteria.** Verdict recorded by the independent reviewer;
metrics scoped exactly to the enumerated subset; every emitted binding
independently source-verified (or a sampled verification plan accepted by
the reviewer); no identity change anywhere; documentation states the
qualified boundaries truthfully.

---

## Post-EC-6F candidates (unauthorized; design when requested)

These are recorded so milestones do not accidentally absorb them:

1. **Policy extensions:** `.js`→`.ts` substitution; `tsconfig` `paths`/
   `baseUrl` with explicit opt-in, config identity, and config-hash
   invalidation; alternative `moduleResolution` emulation levels.
2. **CommonJS surfaces:** `module.exports = …`, `exports.x = …`,
   `require` bindings (identity design needed).
3. **Cross-plugin resolution:** a combined resolver scope for TypeScript +
   JavaScript (requires a core-assisted phase or shared plugin scope;
   revisit D-01/D-18).
4. **Query conveniences:** module dependency presets, property filters,
   dedicated module queries (additive public API, separately reviewed).
5. **Coverage persistence:** exposing extraction coverage per publication
   (shared with the feature-catalog future work).
6. **Identity migration:** path-scoping name-based symbols (functions,
   types, enums, namespaces) so AC-01b's convergence caveat disappears — a
   re-extraction milestone of its own.
7. **Export of imported bindings** and other unsupported forms from D-06.

## Sequencing and review gates

```text
EC-6A design review (EC6-GLM-AR1: READY WITH QUALIFICATIONS) ──accept──►
EC6-A1 owner acceptance ──authorize──► EC-6B ──review──► EC-6C ──review──►
EC-6D ──review──► EC-6E ──review──► EC-6F ──review──► closure
```

- No milestone starts before the previous milestone's review verdict is
  recorded on a preserved review branch.
- Each review branch keeps its probes and evidence; it is never merged.
- A failed review returns the milestone to implementation with its findings
  as the new acceptance list; P3-grade documentation/test findings may be
  released through the EC-5 closure pattern (fix + closure record) rather
  than a new milestone.
