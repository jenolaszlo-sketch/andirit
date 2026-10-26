# EC6C-D0: Export representation implementation roadmap

## Current EC6C-A2 roadmap: Structural Export Surface

**Status:** Replaces the D0 five-stage order for Track A. EC-6C.1 is **accepted with qualifications** (independently verified by GLM-Q1) and EC-6C.2 is **accepted with qualifications** (independently qualified by GLM-Q1 with no demonstrated production defect); the owner closure is [`../decisions/ec6c-owner-acceptance.md`](../decisions/ec6c-owner-acceptance.md). No integration, release, EC-6D, or Track B work is authorized. The original roadmap below remains historical D0 planning; old stage numbers are mapped explicitly and must not be reused with a new meaning without this crosswalk.

### Track A stages

| Current stage | Scope and exit criteria | D0 roadmap crosswalk |
| --- | --- | --- |
| EC-6C.1 — Export shape inventory | Implement only the accepted allowlist in the architecture addendum. Capture canonical public names, local names when written, exact name token spans, export form, explicit type-only state and syntax-only namespace projection. Reject error/missing subtrees and unsupported forms. No declaration lookup, no graph publication requirement in this stage, no export coverage system. Exit with the A01–A29 input/shape fixtures represented in extraction tests or a separately reviewed inventory artifact. | Reuses D0 EC-6C.1, narrowed to positive syntax capture. |
| EC-6C.2 — Structural export publication | Emit per-module/name export nodes and per-written-occurrence exports edges in each file unit. Apply the exact identity, provider/evidence and collision rules in the EC6C-A1 acceptance record and [EC6C-A2 closure addendum](../decisions/ec6c-a29-closure.md). p9 in both plugins; no export-space aggregate; no exported-as; no resolver changes. Exit with extraction, query, replacement, both-store, failure/cancellation and deterministic identity tests. | Reuses D0 EC-6C.2 but removes attribution, export completeness counters and all re-export entries. |
| EC-6C.3 — Track A qualification | Execute the complete matrix, exact-name compatibility, p8-to-p9 durable migration, both stores, durable reopen, source replacement/deletion, failure/cancellation, LF/CRLF and Unicode. Perform pinned website smoke qualification with independently prepared expected names for the nine indexed files; record only the allowlisted positive observations and do not claim complete exports. Preserve commands/results and an independent bounded review. | Takes the relevant D0 EC-6C.5 qualification work. D0 EC-6C.5's migration and qualification intent remains; its declaration-attribution and complete-surface requirements are removed from Track A. |

Current dispositions: the pre-qualification EC-6C.1 acceptance
([`../decisions/ec6c1-owner-acceptance.md`](../decisions/ec6c1-owner-acceptance.md))
is superseded by the final closure
([`../decisions/ec6c-owner-acceptance.md`](../decisions/ec6c-owner-acceptance.md)):
EC-6C.1 **accepted with qualifications**, EC-6C.2 **accepted with
qualifications**, EC-6C closed for the accepted scope. The EC-6C.2 implementation handoff, both-store
lifecycle tests, Unicode round-trip, authentic p8→p9 migration, and the
pinned website smoke are preserved in
[`../evidence/ec6c-2-structural-export-publication-handoff.md`](../evidence/ec6c-2-structural-export-publication-handoff.md).
The independent GLM-Q1 review artifacts (`docs/reviews/ec6c2-glm-q1-*.md`,
`docs/evidence/ec6c2-glm-q1/**`) are preserved unchanged on branch
`qualification/ec6c-2-glm-q1` at `54ca715`, bound by the owner decision. The
remaining gates are integration, release, Track B, and EC-6D; none is
authorized. The committed migration reproduction tooling and fresh
reproduction evidence are in
[`../evidence/ec6c-owner-acceptance/migration-reproduction/`](../evidence/ec6c-owner-acceptance/migration-reproduction/README.md).

### Track B separation

D0 EC-6C.3 (local declaration attribution) and EC-6C.4 (name-converged attribution) are not Track A implementation stages. Move them to a separate, not-yet-numbered Track B design/remediation workstream. It must first specify physical occurrence identity, conservative lexical scope, merged declarations, namespace matching and evidence claims. No Track B implementation begins under this roadmap.

EC-6D remains later design work for import/export binding, remote names, star closure, namespace re-exports, conflicts, cycles and cross-file ownership/dependency tracking.

### Qualification assertions

- No production identity or evidence claim changes other than additive export facts.
- Existing file/declaration IDs, structural edges and unchanged resolves-to facts remain byte-identical across authentic p8-to-p9 migration.
- JavaScript and TypeScript profile-bearing plugin versions both advance to p9; resolver remains resolver2.relative-v1.
- Exact QualifiedName lookup retains all prior candidates and adds a declaration-less Kind=export candidate when the export spelling matches. Existing declaration navigation remains SymbolId-based.
- A completed empty file unit clears old export facts; no exported facts or separate completeness marker appear for unsupported syntax.
- A collision or cancellation before unit completion cannot publish a partial replacement.
- Bounded traversals report Truncated and consumers must not treat truncated observations as all observations.

### Track A implementation-readiness gate

Readiness disposition: Track A implementation was authorized after EC-6C.1 owner acceptance and is locally complete. The EC-6C.2 handoff records both-store and durable qualification, authentic p8→p9 migration, and a real-repository smoke. Independent qualification is now complete (EC-6C.1 verified, EC-6C.2 qualified by GLM-Q1) and EC-6C is closed with qualifications by the owner decision; integration and release remain unauthorized and require a separate decision.

| Required prerequisite | EC6C-A2 disposition |
| --- | --- |
| Exported-name validation and 256-scalar boundary | Specified in the architecture and normative A29 fixture table; strict malformed-surrogate rejection is local to the future extractor. |
| Export-entry identity | Exact module-path/name formula, UTF-8 encoding, ordinal case, no normalization, and fixed hash ID specified. |
| Occurrence identity and collisions | Full accepted tuple and fail-before-insert conflict behavior specified. |
| Type/value projection | Per-occurrence type/value/both/unknown semantics specified. |
| Source spans and coordinates | Exact name-token spans and one-based UTF-16 columns, end-exclusive, specified. |
| Syntax allowlist and re-export exclusions | Supported and excluded forms specified; escaped-name support remains unchanged. |
| Positive-only absence semantics | Empty, omitted, unsupported, and truncated results specified without completeness claims. |
| Exact-name query compatibility | Existing all-node ordinal QualifiedName lookup and declaration-navigation boundary specified. |
| Per-file publication | Entry/occurrence ownership and atomic completion specified. |
| Deletion and replacement | Completed empty replacement and file deletion behavior specified. |
| Failure and cancellation | Incomplete replacement preserves the prior completed publication. |
| Historical p8-to-p9 migration | Both ECMAScript plugins advance to p9; existing identities and resolver2.relative-v1 remain unchanged. |
| Both graph stores | Locally exercised for extraction, persistence, reopen, replacement, deletion, failure, and cancellation; independent review remains a separate gate. |
| Independent review | Existing AR1 design evidence and EC-6C.1 local report are not independent implementation verification; EC-6C.1 and EC-6C.2 require explicit independent findings and dispositions. |

A29 closes the accepted name-bound decision. EC-6C.2 advances TypeScript and JavaScript profile versions to p9, uses `exports1.local-v1` for new export edges, and leaves `resolver2.relative-v1` unchanged. This roadmap status records local implementation evidence only; Track B remains DESIGN REMEDIATION REQUIRED.

EC6C-A2 closes A29. The accepted bound is 256 Unicode scalar values with strict malformed-UTF-16 rejection, no normalization, and no truncation. The UTF-16 code-unit alternative is rejected for name length; source coordinates remain UTF-16. No Track A owner decision remains open. Implementation authorization remains separate and is not granted by this roadmap.

---

> Historical EC6C-D0 content follows. Preserve it for rationale and traceability; the EC6C-A1 contract above controls wherever the text conflicts.

**Status:** proposed decomposition for EC-6C, derived from
[ec6c-export-representation-architecture.md](ec6c-export-representation-architecture.md).
No implementation is authorized by this document. The EC-6C owner gate and an
independent design review precede code; each implementation stage needs its
own review gate.
**Baseline:** `038295f` (owner-accepted EC-6B tree).
**Recommendation:** per-file export units, profile revision **p9**, export
resolver **`exports1.local-v1`**, no identity migration.

## 1. Dependencies and sequencing

| Prerequisite | Status |
| --- | --- |
| EC-6B accepted with qualifications (`EC6B-OA1` closure) | Satisfied |
| EC-6C owner acceptance of the architecture, vocabulary and evidence contract | Required before EC-6C.1 implementation |
| Independent design review of EC6C-D0 | Next step |
| EC-6B source not merged into `main` | EC-6C work must not depend on an EC-6B merge; it can be implemented on a dedicated branch from the accepted EC-6B tree or after a separate integration decision |
| Profile p8 baseline and existing identity contracts | Stable |

Ordering rule: vocabulary and identity first; surface facts before attribution;
path-scoped attribution before name-converged attribution; deferred/unsupported
classification before qualification. Publication and versioning are validated
incrementally, with the final p9 migration at the end.

## 2. EC-6C.1 — Export syntax inventory and shape capture

**Goal.** Recognize the supported module-level export forms and preserve the
written exported names, occurrences, forms, local names, type-only syntax,
default forms and re-export specifiers in a deterministic in-memory shape. No
graph facts and no declaration-binding claims.

**Scope.**
- Add internal export-shape capture to the shared ECMAScript profile:
  direct declarations (variables/classes/interfaces/type aliases/enums/
  namespaces/functions), export lists, aliases, default named/identifier/
  expression/anonymous forms, inline/statement type-only forms, named/
  namespace re-exports, `export *`, and the explicit unsupported forms (CJS,
  `export =`, ambient, destructuring, overlong/escape/empty names).
- Reuse the EC-6B-R1 grammar-aware `type`/`as` classification; no text
  heuristics.
- Record the AST occurrence span for each written name and the declaration AST
  span for direct forms; record import local bindings for the module-scope
  boundary.
- Define the closed classification enum: supported / structural-surface /
  deferred / unsupported / partial.

**Required contract.**
- A written exported name is captured only from module-level export statements
  (`isFileLevel`), never from namespace/function-nested exports.
- Type-only syntax is the EC-6B-R1 rule; `export type T = …` is a direct type
  alias, not a type-only list.
- Default named declarations use the `declaration` field; anonymous/expression
  defaults use the `value` field.
- No truncation or decoding of names/specifiers; bounded text only.
- Every captured shape has a deterministic source order and a classification.

**New graph facts.** None (capture only).

**Prerequisites.** EC-6C owner acceptance of the design contract.

**Acceptance tests.**
- Extraction-shape tests for every row of architecture §6.
- AST-form regression tests for type alias vs type-only list, default
  declaration vs expression, ambient wrapper, destructuring pattern, overload
  signature, quoted names, and `type` identifiers.
- Negative assertions: no `export` nodes/edges, no coverage changes, no
  declaration-binding claims in this stage.
- Determinism with reversed source enumeration; LF/CRLF stability of shapes.
- `dotnet format` and `git diff --check`.

**Compatibility.** The profile emits no new graph facts yet; no plugin version
change is strictly required until EC-6C.2 emits facts. If the capture ships in
an intermediate build, keep it internal and unreferenced; the first shipping
stage bumps p9.

**Failure cases.** Malformed export syntax; missing names; unsupported forms;
overlong/escape-bearing names; nested exports. All classify, never throw except
for invariant violations (duplicate shape capture).

**Independent review gate.** A focused design-conformance review of the
classification matrix and shape capture (can be folded into the EC-6C.2
review if no graph facts ship).

## 3. EC-6C.2 — Export entry representation, identity, and publication

**Goal.** Introduce the graph vocabulary and publish the export surface
(entries + occurrence edges) without declaration attribution.

**Scope.**
- Add `CodeNodeKinds.Export`; `CodeEdgeKinds.Exports`; property keys
  `export-kind`, `export-via`, `local-name`, `export-space`, `via-module`
  (and reuse `type-only`). Add the constants to `CodePropertyKeys`.
- Emit one `export` node per (module path, exported name) in the owning file's
  unit, with `Name`/`QualifiedName` = exported name, no `SymbolId`, property
  `export-space`.
- Emit one `exports` edge per written occurrence with the identity and span
  from architecture §5.3; evidence Structural, extractor provider, plugin
  version p9.
- Compute `export-space` from the captured forms and (when no attribution
  exists) syntax facts: type-only → type; direct declaration kind → its space;
  expression → value/both; deferred/unresolved → unknown.
- Add the kinds to the profile's shared relationship-kind lists, the session's
  coverage list, and the plugin produced-kind sets. Add per-kind coverage:
  candidates = written export occurrences; emitted = occurrence edges;
  internal = emitted (same file unit); cross = 0; unsupported = recognized
  forms with no entry (star re-export, CJS, `export =`, ambient/destructuring
  per policy, unsupported names); unresolved/ambiguous/external = 0 for the
  surface kind unless the owner adopts a deferred counter.
- Emit in the profile's post-walk export pass (or an equivalent file-local pass
  before the unit completes); ensure entries/edges are in the same file unit
  as their endpoints.
- Bump `EcmaScriptExtractionProfile.ProfileRevision` 8 → 9 and both plugin
  versions; the module resolver stays `resolver2.relative-v1`.

**Required contract.**
- Entry identity: `{language}:export:{sha256(modulePath\nexportedName)}`;
  occurrence identity: architecture §5.3.
- No attribution edges in this stage; no `Resolved`/`Derived` export facts.
- `export-space` values and occurrence properties as architecture §5.1/§6.
- The surface edge is Structural; provider is the extraction plugin.
- Duplicate names aggregate to one entry with multiple occurrence edges; no
  arbitrary pick; duplicate surface counting follows the owner decision.
- Re-export/imported-binding entries follow the owner decision D-EC6C-09; the
  implementation supports the chosen policy behind a single classification
  flag.

**New graph facts.** `export` nodes; `exports` edges; `export-space`/occurrence
properties; export coverage rows.

**Prerequisites.** EC-6C.1; owner acceptance of the vocabulary and identity
(D-EC6C-01/02/03/06/07/09/10/11/12).

**Acceptance tests.**
- Identity tests for every supported form: entry ids, occurrence ids,
  properties, spans (UTF-16), duplicate names, aliases, Unicode/quoted names.
- Exact-name lookup returns entries as candidates, never a single pick;
  outgoing `exports` from the file returns the surface.
- No `exported-as` edges yet; no `Resolved` export claims.
- Publication on both stores: add/remove/rename/empty replacement, file
  deletion, failed run, cancellation, durable reopen; no dangling endpoints.
- p8→p9 version invalidation test; existing facts unchanged (see EC-6C.5).
- Coverage arithmetic holds; no pre-existing fact changes; no preset changes.
- Formatter and whitespace checks.

**Compatibility.** New node/edge/property kinds are additive; no schema
change; existing identities unchanged; `resolves-to` unchanged. The profile
bump invalidates all sources; the module unit re-emits (identities stable).

**Failure cases.** Identity collisions fail loudly; unsupported forms emit no
entry and are counted; a failed post-walk pass fails the file unit; partial
parsing keeps valid siblings; cancellation discards staged facts.

**Independent review gate.** Bounded review of the representation, identity,
publication and query behavior before attribution is added.

## 4. EC-6C.3 — Eligible local-declaration attribution

**Goal.** Connect exported names to declarations only where eligibility is
established for path-scoped, occurrence-specific declaration kinds.

**Scope.**
- Add `CodeEdgeKinds.ExportedAs` and the attribution property keys
  `attribution-basis`, `declaration-kind`.
- Build the per-file module-scope table during the walk: file-level
  declarations, import local bindings, projections (callable projection of a
  function-valued variable), and exact declaration AST/name spans.
- Direct attribution: match the captured declaration AST span to the emitted
  declaration record; target the declaration node; evidence `Resolved` only for
  path-scoped kinds (variable/class/interface), `Derived` for name-converged
  kinds (deferred to EC-6C.4 if staged separately).
- Local-list/default-identifier attribution: unique eligible module-scope
  declaration; exclude import bindings (`local-import`, deferred); exclude the
  callable projection; multiple declarations → no attribution (ambiguous);
  zero → no attribution (unresolved).
- Type/space compatibility: explicit type-only requires type-space; default
  expression identifiers require value-space; mismatches emit no attribution
  and are counted.
- Emit attribution edges with the declaration occurrence span and the export
  resolver provider (`exports1.local-v1`).
- Update per-kind coverage for attribution (candidates, emitted, unresolved,
  ambiguous, unsupported, external, and the deferred policy).

**Required contract.**
- Only occurrence-specific declarations get `Resolved`; name equality alone is
  never sufficient; direct span matching and the documented module-scope model
  are the only bases.
- `exported-as` direction is declaration → entry; evidence location is the
  declaration occurrence; `attribution-basis` and `declaration-kind` are
  present.
- No attribution edge for unresolved, ambiguous, imported-binding, expression/
  anonymous default, ambient or destructured cases.
- The callable projection is not an eligible declaration; the field is.

**New graph facts.** `exported-as` edges; attribution evidence/provenance;
attribution coverage.

**Prerequisites.** EC-6C.2; owner acceptance of evidence levels
(D-EC6C-04/05), the D-05 refinement for dual projections, and the module-scope
model (D-EC6C-05).

**Acceptance tests.**
- Direct and local fixtures for variables/classes/interfaces; aliases;
  multi-declarators; function-valued variables; type-only identifiers;
  `type`-named bindings; default identifier; type/space mismatches.
- Negative: unresolved, dual projection, import binding, anonymous/expression
  default, ambient/destructuring — no attribution.
- Evidence assertions: Resolved only for path-scoped occurrences; provider/
  version; no confidence.
- Publication/replacement/deletion for attribution edges on both stores.
- Coverage assertions per outcome.

**Compatibility.** Additive; no change to existing kinds/identities; p9
already introduced. `exported-as` is not in presets/references.

**Failure cases.** Ambiguous lookup; space mismatch; missing declaration;
duplicate declaration records; projection matching; collisions fail loudly.

**Independent review gate.** Attribution eligibility and evidence review before
name-converged attribution.

## 5. EC-6C.4 — Name-converged attribution and deferred/unsupported classification

**Goal.** Complete the attribution model for name-converged declarations and
finalize the honest classification of deferred/unsupported forms.

**Scope.**
- Emit `Derived` attribution for function/type/enum/namespace with the
  declaration occurrence and declaring-module evidence; no `Resolved`.
- Handle overloads (implementation attribution only; signature occurrences are
  surface-only), declaration merging (multiple attribution edges), duplicate
  names (multiple occurrences/attributions; no arbitrary pick), and type/value
  union spaces.
- Finalize re-export/imported-binding entries per owner decision D-EC6C-09;
  implement the selected policy: structural entries with no attribution
  (recommended) or suppression with coverage.
- Finalize destructuring/ambient/CJS/`export *` classification and coverage;
  add consumer documentation boundaries.
- Ensure coverage fields have exact EC-6C semantics; if the owner accepts an
  additive deferred counter, add it and test; otherwise document the chosen
  mapping precisely.
- Consumer documentation: the export model, the Resolved vs Derived meaning,
  absence semantics, aliases/defaults/type spaces, deferred forms, and the
  unchanged EC-6B `resolves-to` sentence.

**Required contract.**
- `Derived` never presents as `Resolved`; consumers filtering `Resolved` must
  not receive name-converged attributions.
- Multiple attributions are permitted (merging/duplicates); no one-to-one
  fabrication.
- Deferred/unsupported forms are explicit and counted; no fabricated
  declaration or remote binding.
- Re-export entries, if present, are Structural surface facts with no
  `exported-as`; the `resolves-to` fact remains the module dependency.

**New graph facts.** `Derived` attribution edges; final classification/coverage;
consumer docs.

**Prerequisites.** EC-6C.3; owner acceptance of D-EC6C-05/06/08/09/12.

**Acceptance tests.**
- C03/C13/C14/C24/C32/C33 fixtures; two-module convergence; merging; overloads;
  duplicate names; type/value unions.
- Evidence assertions: Derived only for name-converged; no Resolved; declaring
  module/occurrence present; no confidence.
- Deferred/unsupported negative fixtures (C21/C27/C28/C29/C34/C35).
- Consumer query scenarios from architecture §10.
- Coverage per outcome; exact-name/traversal behavior.

**Compatibility.** Additive; no preset/query changes; `FindReferencesAsync`
unchanged (the design does not use `references`).

**Failure cases.** Overload-only exports; merge ambiguity; duplicate-name
conflicts; re-export suppression vs entry policy; type/value union errors.

**Independent review gate.** F01/D-05/evidence review; the reviewer must
challenge the Derived claim and the module-scope model.

## 6. EC-6C.5 — Qualification, migration, and independent review

**Goal.** Qualify the implemented export representation against the accepted
contract and preserve evidence.

**Scope.**
- Full solution suite; focused export tests; formatter; `git diff --check`.
- Both stores, durable close/reopen, add/delete/rename/empty, failure and
  cancellation, endpoint ordering, unchanged no-op publication.
- Determinism: reversed enumeration, LF/CRLF, supplementary Unicode, repeated
  extraction.
- **Authentic p8→p9 migration.** Build a durable LatticeDb publication with the
  real p8 extractor from the owner-accepted EC-6B source (`038295f`), then
  reindex unchanged sources with the EC-6C p9 extractor. Assert:
  - every existing node, declaration, structural edge and `resolves-to` edge
    identity is preserved byte-for-byte;
  - the only additions are export entries/edges/attributions and their
    coverage;
  - both plugins execute and both end at p9;
  - no mixed revisions;
  - durable reopen preserves the new facts;
  - deletion/removal scenarios remove the correct export facts.
- **Real-repository smoke.** Index the pinned website snapshot
  (`jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`) and
  independently establish the expected export names/declarations for the nine
  indexed sources before comparing. Do not derive expected counts from the
  implementation. Record supported/unattributed/deferred forms.
- Adversarial matrix C01–C35 executed or explicitly reasoned; any unexecuted
  case recorded as an evidence limitation.
- Preserve review evidence on a separate branch; do not merge.

**Required contract.** All architecture §5–§12 invariants; no P0/P1/P2
defects; the qualifications in §13 remain documented.

**New graph facts.** None beyond EC-6C.4.

**Prerequisites.** EC-6C.1–6C.4; owner acceptance of the final contract.

**Acceptance gates.**
- Full suite and focused tests pass; formatter and whitespace clean.
- p8→p9 migration preservation proven; website ground truth established and
  matched within the documented scope.
- No `exports`/`binds` import binding, no cross-plugin remote binding, no
  identity migration, no preset change.
- Independent EC-6C review completed and preserved; owner closure decision.

**Failure cases.** Migration mismatch; website ground-truth difference; stale
export facts; mixed profile revisions; durable reopen loss; any false
attribution.

**Independent review gate.** Full EC-6C adversarial qualification review
(separate assignment), followed by owner acceptance.

## 7. Fixture inventory

| Fixture family | Cases | Stage |
| --- | --- | --- |
| Syntax/classification matrix | C04–C16, C26–C29, C31, C34, C35 | 6C.1/6C.2 |
| Identity/multiplicity | C01, C02, C09, C15, C17, C23, C24, C32, C33 | 6C.2/6C.4 |
| Path-scoped attribution | C04, C05, C10, C11, C12, C30 | 6C.3 |
| Name-converged attribution | C03, C13, C14, C24 | 6C.4 |
| Deferred/unsupported | C06, C07, C08, C21, C27, C28, C29, C34, C35 | 6C.2/6C.4 |
| Lifecycle/publication | C17–C20, C25 | 6C.2/6C.3/6C.5 |
| Determinism/coordinates | C19, C23 | 6C.2/6C.5 |
| Historical migration | all preserved p8 facts | 6C.5 |
| Real repository | pinned website exports | 6C.5 |

## 8. Test strategy

- **Extraction tests** (Tree-sitter test project): shapes, nodes, declarations,
  edges, ids, properties, evidence, coverage; no store dependence.
- **Store/lifecycle tests** (both `InMemoryCodeGraphStore` and
  `LatticeCodeGraphStore`): staging, replacement, deletion, empty completion,
  failure, cancellation, endpoint ordering, durable reopen.
- **Query tests**: exact-name candidates, outgoing/incoming traversals,
  evidence filters, absence semantics, presets unchanged.
- **Migration tests**: authentic p8 seed at `038295f`, durable DB, p9 reindex,
  normalized snapshot diff.
- **Website smoke**: independent ground truth for the nine supported files;
  no count-only assertions.
- **Negative assertions**: no `binds`/`exports` import binding, no `references`
  edges, no new preset membership, no declaration identity changes, no
  cross-plugin remote binding.
- **Regression guard**: every pre-existing node/declaration/structural/
  `resolves-to` id is byte-identical across p8→p9 for unchanged sources.

No benchmark or performance number is proposed; EC-6E owns measurement.

## 9. Historical migration requirements

- Use the real owner-accepted p8 extractor (not a simulation) at
  `038295f`.
- Seed a durable LatticeDb publication from a fixture repository with the
  export matrix and from the pinned website snapshot.
- Reindex unchanged sources with the p9 extractor.
- Preserve a normalized snapshot (nodes, declarations, edges, ids) before and
  after; the diff must contain only the new export facts and coverage.
- Explicitly verify `resolves-to` edge ids and structural identities are
  unchanged.
- Test a source edit that removes one export, an alias rename, and a target
  declaration deletion; verify stale export facts are removed and no
  attribution dangles.
- Reopen the database after migration and verify the export facts persist.

If the authentic p8 seed cannot be executed, record the limitation and do not
substitute an invented result.

## 10. Independent review points

1. **Design review (EC6C-GLM-DR1 or equivalent):** challenge the representation,
   F01/D-05/D-06 reconciliation, evidence levels, identity, coverage and
   ECM-6D compatibility.
2. **Bounded implementation review after 6C.2:** surface representation,
   identity, publication, queries; no attribution yet.
3. **Attribution review after 6C.4:** eligibility, Resolved/Derived, module
   scope, merging/duplicates.
4. **Final EC-6C qualification review after 6C.5:** full adversarial,
   migration, website, evidence; owner closure.

No review point authorizes merging into `main`; integration remains separately
owner-gated.

## 11. Minimal first implementation slice

The smallest truthful slice is **EC-6C.1 + EC-6C.2**: export entries and
occurrence edges for the supported direct/local/default/expression forms, with
`export-space`, Structural evidence, per-file publication, coverage, queries,
and the p9 version bump — **without any declaration attribution**. It delivers
the export surface, preserves all existing facts, and avoids the F01/D-05
attribution questions until the model is reviewed. Attribution (6C.3/6C.4) is
then additive and independently reviewable.

If the owner prefers attribution in the first shipping slice, include 6C.3
(path-scoped only), keeping name-converged attribution (6C.4) separate.

## 12. Deferred EC-6D work

- Import-to-export binding and edge vocabulary (`binds` or equivalent).
- Named/star/namespace re-export closure, conflicts, cycles.
- Binding to re-export entries and remote declaration origins.
- Type/value matching for imports against export spaces and declaration kinds.
- Cross-file dependency tracking or a plugin-owned export unit if required.
- Promotion rules for `Derived` associations in import binding (conservative
  default: not Resolved).
- Extended queries/conveniences for imports/exports.

None of these is authorized or implemented by EC-6C.

## 13. Open owner decisions before implementation

The decision register lists the required approvals. The minimum set is:
representation and vocabulary (D-EC6C-01/02/03), evidence levels and provider
(D-EC6C-04/10), D-05/F01 reconciliation (D-EC6C-05), namespace/default policy
(D-EC6C-06/07), ownership/publication (D-EC6C-08), re-export/imported-binding
surface entries (D-EC6C-09), coverage semantics (D-EC6C-12), and profile p9 /
no identity migration (D-EC6C-11). If any is rejected, the affected stage is
rescoped or withheld before implementation.
