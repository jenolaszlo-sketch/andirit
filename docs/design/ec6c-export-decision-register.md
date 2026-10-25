# EC6C-D0: Export representation decision register

**Status:** design decision register for EC6C-D0. It records recommendations,
alternatives, evidence and owner-approval requirements. It does not record owner
approval that has not been given. The previously proposed file-to-declaration
`exports` model was **not** accepted for EC-6C; this register treats it as a
rejected alternative, not a settled contract.
**Baseline:** `038295f` (owner-accepted EC-6B tree).
**Related:** [architecture](ec6c-export-representation-architecture.md),
[adversarial cases](ec6c-export-adversarial-cases.md),
[implementation roadmap](ec6c-export-implementation-roadmap.md).

## 1. Decision status vocabulary

- **ACCEPTED (inherited):** an existing accepted contract; unchanged by this
  design unless explicitly superseded.
- **RECOMMENDED:** the design proposes this; owner approval is required before
  implementation.
- **REJECTED:** evaluated and not recommended, with rationale.
- **DEFERRED:** explicitly outside EC-6C; assigned to a future milestone.
- **OPEN:** a material question this design cannot settle from accepted
  constraints and source inspection.

## 2. Existing accepted contracts (inputs, not re-decided)

| Contract | Status | Relevance |
| --- | --- | --- |
| EC-6A A1: resolution pass inside the Tree-sitter session; no core resolution phase | ACCEPTED | Export facts remain plugin-local; no new core phase |
| EC-6A A2: module/file identity; no new module node kinds | ACCEPTED | Export entries are new nodes, but module identity stays the file node; no module node kind |
| EC-6A A3: `Resolved` for `resolves-to` with the mandated sentence | ACCEPTED | Unchanged; EC-6C attribution has its own bounded meaning |
| EC-6A A4: EC-6B vocabulary only | ACCEPTED | EC-6C vocabulary is new and requires its own acceptance |
| EC-6A A5/A6/A7: rebuild, presets, per-plugin scope | ACCEPTED | Per-file export facts; presets unchanged; plugin-local |
| EC-6A D-05: `exports` may target name-based symbols under the F01 gate | ACCEPTED with gate | This design **supersedes the representation** and satisfies the gate; owner approval required |
| EC-6A D-06: imported-binding exports emit no `exports` edge | ACCEPTED boundary | Reconciled by D-EC6C-09 (surface entry vs declaration binding) |
| EC-6A D-10: CJS binding deferred | ACCEPTED | Unchanged; unsupported in EC-6C |
| EC-6A D-11: anonymous default exports produce no export edge/binding target | ACCEPTED boundary | Reconciled by D-EC6C-07 (a `default` surface entry with no declaration/binding is not a forbidden edge to a declaration) |
| EC-6A F12: direct capture may be Resolved; scope resolution needs a documented module-scope model; name-match-only binding forbidden | ACCEPTED gate | Satisfied by D-EC6C-04/05 |
| EC-6B identity, UTF-16 columns, occurrence anchoring, atomic publication, no identity migration | ACCEPTED | Preserved; no redefinition |
| EC-6B provider/policy `resolves-to` semantics | ACCEPTED | Unchanged |

## 3. Recommendations requiring owner acceptance

### D-EC6C-01 — Export representation model

**Status:** RECOMMENDED (owner approval required).
**Decision.** Represent the export surface with export-entry **nodes** plus two
new edge kinds: `exports` (file → entry, one per written occurrence) and
`exported-as` (declaration node → entry, one per eligible declaration
occurrence). Reject file-to-declaration-only edges (Alternative A) and the
hybrid (Alternative C).
**Alternatives.** A: module file → declaration edges with name/form properties.
C: A for attributable cases + entries for unresolved.
**Evidence.** Source inspection and the disposable probe (§16 of the
architecture) show (a) name-scoped declarations converge across files, so an
edge target cannot identify a physical declaration; (b) expression/anonymous
defaults, unresolved names, imported-binding exports and re-exports have no
local declaration endpoint; (c) edge properties are not identity inputs and are
not server-side queryable; (d) EC-6D needs an export-name endpoint. Alternative
B is the smallest model that preserves the five fact layers.
**Tradeoffs.** One node kind + two edge kinds; more facts; new query paths.
**Risks.** Owner has previously accepted the D-05 direction; this is a
representation change requiring explicit approval.
**Stage.** EC-6C.1/6C.2. **Owner action:** accept/reject the model.

### D-EC6C-02 — Export-name identity

**Status:** RECOMMENDED.
**Decision.** One `export` node per (module path, exported name):
`{language}:export:{sha256(modulePath\nname)}`; `Name`/`QualifiedName` = the
exported name; no `SymbolId`; occurrence-independent; bounded 256 chars;
ordinal; no Unicode normalization. One entry per name means duplicate written
names aggregate to one entry with multiple occurrences.
**Alternatives.** Occurrence-scoped entries (per written site); namespace
split into identity; name as a property.
**Evidence.** The graph's exact-name lookup uses node names; properties are not
queryable; occurrence-independent names are stable under statement moves and
provide a stable EC-6D endpoint. The probe shows no existing export nodes to
migrate.
**Tradeoffs.** Entry metadata (`export-space`) can change across publications
when occurrences change; the entry id remains the name. Occurrences and
declarations are separate identities.
**Risks.** Consumers might expect one node per occurrence; documentation must
be explicit. Duplicate-name surfaces are ambiguous by design.
**Stage.** EC-6C.2. **Owner action:** accept/reject.

### D-EC6C-03 — Source-occurrence identity and coordinates

**Status:** RECOMMENDED.
**Decision.** One `exports` edge per written occurrence; identity inputs are
kind, file source, entry target, discriminator (written export kind/via/local
name/explicit type-only/raw `via-module` specifier when present) and the
written-name span; one-based UTF-16 code-unit columns, end-exclusive; LF/CRLF
stable; collision with differing facts fails loudly.
**Alternatives.** Statement-span edges; property-only occurrences; byte columns
(rejected by EC-6B-R1).
**Evidence.** EC-6B identity conventions and the corrected coordinate contract;
probe AST name spans.
**Tradeoffs.** Moving an occurrence changes its edge id (occurrence-anchored);
the entry id is stable.
**Risks.** Consumers relying on edge ids across edits; documented.
**Stage.** EC-6C.2. **Owner action:** accept/reject.

### D-EC6C-04 — Declaration-attribution eligibility

**Status:** RECOMMENDED.
**Decision.** Attribution requires either (a) direct syntax: the declaration AST
span captured inside the export statement matches exactly one emitted
declaration record, or (b) a documented module-scope lookup: exactly one
eligible file-level declaration for the local name, no import binding. Evidence
is `Resolved` only for occurrence-specific (path-scoped) declaration kinds
(variable, class, interface) and `Derived` for name-converged kinds (function,
type alias, enum, namespace). No attribution for unresolved, ambiguous,
imported-binding, anonymous/expression default, ambient, destructured or
type/space-mismatched cases. The callable projection of a function-valued
variable is not an independent eligible declaration; the field is.
**Alternatives.** Attribute name-converged kinds as Resolved (rejected: no
occurrence-specific target); drop all name-converged attribution (conservative
but loses the association; Derived is the chosen middle); name-match only
(forbidden).
**Evidence.** Probe: convergence matrix; dual projection; declaration records
carry exact spans; EC-6A F01/F12 gates.
**Tradeoffs.** Derived is not a verified declaration binding; some common
exports (functions) have association but not Resolved. Recall is higher than
dropping them and truth is preserved by the evidence.
**Risks.** Consumers reading Derived as Resolved; mitigated by provider/evidence
docs and filters.
**Stage.** EC-6C.3/6C.4. **Owner action:** accept/reject, including the D-05
refinement for dual projections.

### D-EC6C-05 — Evidence levels and provider

**Status:** RECOMMENDED.
**Decision.** `exports` = `Structural` with the extraction plugin provider;
`exported-as` = `Resolved` or `Derived` per D-EC6C-04 with a distinct export
resolver `penghou.hetu.tree-sitter.ecmascript-export-resolver` and version
`{packageVersion}+exports1.local-v1`; deterministic qualities carry no
confidence; the EC-6B `resolves-to` sentence and semantics remain unchanged;
consumer docs state the exact export claims and absence semantics.
**Alternatives.** Extract plugin as attribution provider (blurs parse vs
attribute); no distinct provider (loses provenance); promote all to Resolved
(rejected).
**Evidence.** EC-6B provider precedent; EC-6A F12; architecture §7/§8.
**Tradeoffs.** One more provider identity; version discipline.
**Risks.** Provider version must move with profile revision (D-16); stated.
**Stage.** EC-6C.3/6C.4. **Owner action:** accept/reject.

### D-EC6C-06 — Type/value namespace representation

**Status:** RECOMMENDED.
**Decision.** Export space is **metadata** on the entry
(`export-space`: type/value/both/unknown), not part of the entry identity. The
written type-only modifier is an occurrence property (`type-only`). Attribution
edges carry `declaration-kind`, from which the declaration space is known.
A name with type and value bindings is one entry with space `both`; type-only
and value occurrences are distinguished on their surface edges.
**Alternatives.** Split entries by space; put type-only on the entry; ignore
space (rejected: EC-6D needs it).
**Evidence.** A module exports one name; the spaces are properties of its
bindings; splitting creates duplicate names and exact-name noise. Probe shows
class/both, interface/type, enum/both, namespace/both.
**Tradeoffs.** Entry space is a union that can change when occurrences change.
**Risks.** Consumers must consult occurrence flags and declaration kinds for
fine-grained matching; documented.
**Stage.** EC-6C.2/6C.4. **Owner action:** accept/reject.

### D-EC6C-07 — Default-export representation

**Status:** RECOMMENDED.
**Decision.** `export default` always exposes the exported name `default` as an
entry. Named function/class declarations (`declaration` field) get attribution
per D-EC6C-04; anonymous function/class and expression defaults (`value` field)
get an entry with `export-via=expression`, no attribution and no fabricated
declaration; a bare identifier default uses the unique-local rule and only
value-space declarations. `export type { X as default }` is a type-only list
entry. Space: function/expression = value; class = both.
**Alternatives.** No entry for anonymous/expression defaults (loses the visible
default name); fabricate a declaration (forbidden); D-11's original "no export
edge" strictness (the new entry is not a declaration edge; owner must confirm).
**Evidence.** Probe AST: declaration vs value fields; no declarations for
anonymous/expression defaults.
**Tradeoffs.** `default` is a name-level entry with no declaration for some
forms; consumers must not infer a declaration.
**Risks.** D-11 wording must be reconciled in owner acceptance.
**Stage.** EC-6C.2/6C.3. **Owner action:** accept/reject and reconcile D-11.

### D-EC6C-08 — Source-unit ownership and publication

**Status:** RECOMMENDED.
**Decision.** Export facts live in the owning **per-file unit** (Option A). No
`{language}:exports` unit and no hybrid in EC-6C; no core publication change.
The profile emits entries/occurrences/attributions in the same file unit; the
existing atomic replacement, endpoint validation, failure/cancellation
retention, and deletion behavior apply. EC-6D may add a cross-file unit or
dependency tracking for re-export/star binding.
**Alternatives.** B: stable `{language}:exports` unit (unnecessary for
file-local facts); C: hybrid (duplicated ownership).
**Evidence.** All EC-6C facts depend only on the file; the probe has no
cross-file export facts; the accepted unit/publication model is sufficient.
**Tradeoffs.** EC-6D cross-file closure will need separate design.
**Risks.** A future cross-file requirement might tempt a late ownership change;
it is explicitly deferred.
**Stage.** EC-6C.2. **Owner action:** accept/reject.

### D-EC6C-09 — Deferred/remote surface entries (reconciliation of D-06)

**Status:** RECOMMENDED; owner decision.
**Decision.** EC-6C may emit **structural surface entries** for written names
whose binding is remote or imported: named re-exports (`export { x } from`,
`export { x as y } from`), namespace re-export (`export * as ns from`), and
exports of imported local bindings (`import { x }; export { x }`). These entries
have `export-via=re-export`/`namespace-re-export`/`local-import`, no
`exported-as`, no declaration claim; `via-module` carries the raw written
specifier where available. `export *` has no written name and emits no entry.
The existing `resolves-to` edge remains the module fact. Alternative:
suppress all such entries in EC-6C and record coverage only (strict D-06).
**Alternatives.** Suppress entries; emit declaration edges (rejected: no local
declaration; would be false).
**Evidence.** The written exported name is a real surface fact; the task
requires distinguishing visible names from verified local bindings; D-06
forbade a declaration `exports` edge, which the new model does not create.
**Tradeoffs.** Surface completeness vs minimal scope; consumers might mistake
the entry for a verified binding unless the `export-via` value and absence of
`exported-as` are documented.
**Risks.** D-06 wording conflict; owner must reconcile explicitly.
**Stage.** EC-6C.2/6C.4. **Owner action:** choose structural entries (recommended)
or suppression.

### D-EC6C-10 — Query visibility

**Status:** RECOMMENDED.
**Decision.** Use existing public query APIs: outgoing `exports` from the
module file; incoming/outgoing `exported-as`; exact-name lookup on the entry
name; evidence-filtered traversals. No new query API is required in EC-6C;
property filters (`export-space`, `type-only`, `local-name`) are client-side as
with existing properties. Presets, `FindReferencesAsync`, declaration lookup and
`resolves-to` queries are unchanged. The design deliberately does not use the
existing `references` kind for attribution, so reference-query behavior does
not change. Optional convenience APIs are deferred.
**Alternatives.** New query service methods/presets (premature); using
`references` (changes existing reference behavior); properties-only names (not
queryable).
**Evidence.** Query service/source inspection; presets closed sets; node
name/qualified-name lookup.
**Tradeoffs.** Consumers must write a traversal or read properties; documented.
**Risks.** Convenience gap; deferred.
**Stage.** EC-6C.2. **Owner action:** accept/reject (or request convenience).

### D-EC6C-11 — Profile version, provider version, and migration

**Status:** RECOMMENDED.
**Decision.** Advance the ECMAScript profile p8 → **p9** in both plugins;
export resolver version `{packageVersion}+exports1.local-v1`; module resolver
remains `resolver2.relative-v1`. Existing identities remain byte-identical for
unchanged sources; the p8→p9 migration is validated with an authentic p8 seed.
No identity migration is proposed. Stores/snapshots need no schema change. Any
later change to emitted export facts must bump the profile/resolver revision in
lockstep (D-16 discipline).
**Alternatives.** p8 without a bump (invalidates nothing; stale exports); a
separate export unit version axis (unnecessary); identity migration (not
justified).
**Evidence.** EC-6B invalidation precedent; new facts are additive; file-local
ownership.
**Tradeoffs.** Full re-extraction on first p9 run.
**Risks.** Historical p8 publications remain without export facts (expected).
**Stage.** EC-6C.2/6C.5. **Owner action:** accept/reject.

### D-EC6C-12 — Coverage reporting

**Status:** RECOMMENDED; owner decision.
**Decision.** Add `exports` and `exported-as` to the Tree-sitter coverage list.
For `exports`: candidates = written export occurrences; emitted = occurrence
edges; internal = emitted (all file-local); cross = 0; unsupported = recognized
forms with no entry (star re-export, CJS/`export =`, ambient/destructuring per
policy, unsupported names); unresolved/ambiguous/external = 0. For
`exported-as`: candidates = local-binding occurrences; emitted = attribution
edges; internal = emitted; cross = 0; unresolved = no declaration; ambiguous =
multiple eligible declarations or duplicate-name conflict; unsupported =
ineligible/space mismatch; deferred = imported-binding/re-export attribution,
which is not an attribution candidate — if the owner accepts a new optional
`deferredTargets` counter it is reported there, otherwise it is documented and
excluded from both candidate and unsupported counts. Keep
`CodeGraphExtractionResult.UnresolvedRelationships` unchanged (module resolver
only) to avoid changing EC-6B consumer semantics; document the export coverage
contract.
**Alternatives.** New counter (cleaner, additive API); fold deferred into
unsupported (no API change, less precise); reuse unresolved (misleading).
**Evidence.** Coverage constructor contract; EC-6A §12.2; EC-6B acceptance.
**Tradeoffs.** Clean deferred reporting vs additive public API change.
**Risks.** Coverage semantics confusion; mitigated by documentation/tests.
**Stage.** EC-6C.2/6C.4. **Owner action:** choose the deferred-counter policy.

### D-EC6C-13 — Unattributable, ambiguous, and unsupported behavior

**Status:** RECOMMENDED.
**Decision.** Always emit the surface entry when the exported name is readable
and supported; never fabricate a declaration or remote binding. No
`exported-as` for unresolved, ambiguous, imported-binding, re-export,
anonymous/expression, ambient or destructured cases. Coverage distinguishes
the reason; `export-via` distinguishes deferred forms. Absence of an
attribution edge means "not established under EC-6C", never "no declaration" or
"no export". The consumer documentation states this.
**Alternatives.** Suppress the surface entry when unattributable (loses the
name); emit a declaration placeholder (forbidden); emit a Resolved edge to a
shared node (forbidden).
**Evidence.** Task's five layers; F01/F12; EC-6B absence semantics.
**Tradeoffs.** Surface completeness with a larger unattributed class.
**Risks.** Consumers over-reading entries; documented.
**Stage.** EC-6C.2/6C.4. **Owner action:** accept/reject.

### D-EC6C-14 — EC-6D extensibility

**Status:** RECOMMENDED; informational/owner acknowledgement.
**Decision.** The export entry is the future import-binding endpoint; aliases,
defaults, type-only and namespace forms are representable; re-export/star/
namespace closure and remote declaration origins remain EC-6D; `Derived`
associations are not automatically promoted to Resolved import bindings;
no identity migration is expected. EC-6D may need a cross-file export unit or
dependency tracking.
**Alternatives.** Bind imports directly to declarations now (would conflate
stages and repeat the F01 hazard).
**Evidence.** Architecture §13; EC-6B module resolution remains the file-level
prerequisite.
**Tradeoffs.** EC-6C ships a surface, not import bindings.
**Risks.** EC-6D may discover a need for more metadata; the entry/vocabulary is
designed to be additive.
**Stage.** EC-6C and EC-6D design. **Owner action:** acknowledge the boundary.

## 4. Rejected alternatives (summary)

| Alternative | Why rejected |
| --- | --- |
| A: file → declaration `exports` edges only | No endpoint for expression/anonymous defaults, unresolved/imported/re-export names; exported name has no identity; properties not queryable/identity; F01 convergence remains at the target; EC-6D has no bind endpoint |
| C: hybrid A + entries | Two representations for one name; representation depends on attribution; duplicate queries and lifecycle; no reduction in needed entry model |
| Attribute name-converged kinds as Resolved | The target node is shared; cannot prove a specific physical declaration |
| Drop name-converged attribution entirely | Loses deterministic association that Derived can represent honestly; chosen alternative is Derived |
| Name-only attribution (text match) | Forbidden by EC-6A/F12; not scope-aware |
| Split entries by type/value identity | Duplicates names/exact-name noise; a module's exported name is one surface entity |
| Separate `{language}:exports` unit in EC-6C | Facts are file-local; unnecessary ownership/ordering complexity; deferred to EC-6D if needed |
| Use existing `references` kind | Would change `FindReferencesAsync` consumer behavior; new kind is additive |
| Reuse `exports` file→file self-edges for unattributable names | Conflates module identity with no declaration; no name entity |
| Byte-column coordinates | Rejected by EC-6B-R1; UTF-16 is the accepted convention |
| Identity migration for name-converged kinds | Not demonstrated necessary; Derived + entry identity + occurrence provenance preserves truth; migration is a separate owner decision |

## 5. Open questions / owner decisions checklist

Before EC-6C.1/6C.2 implementation:

- [ ] Accept D-EC6C-01 (entry representation) and D-EC6C-02 (name identity).
- [ ] Accept D-EC6C-03 (occurrence identity/coordinates).
- [ ] Accept D-EC6C-04/05 (eligibility; Resolved/Derived; export resolver
      provider) including the D-05 refinement and F01 reconciliation.
- [ ] Accept D-EC6C-06 (space metadata) and D-EC6C-07 (default forms),
      reconciling D-11.
- [ ] Accept D-EC6C-08 (per-file ownership).
- [ ] Decide D-EC6C-09 (re-export/imported surface entries vs suppression),
      reconciling D-06.
- [ ] Accept D-EC6C-10 (query visibility/no new API).
- [ ] Accept D-EC6C-11 (p9, versions, no identity migration).
- [ ] Decide D-EC6C-12 (coverage/deferred counter).
- [ ] Acknowledge D-EC6C-13 (unattributable behavior) and D-EC6C-14 (EC-6D
      boundary).

If the owner rejects a decision, the affected stage is rescoped; the design
does not silently substitute.

## 6. Deferred EC-6D decisions

- Import-binding vocabulary and direction (`binds` or equivalent).
- Re-export/star/namespace closure tables, conflict and cycle rules.
- Remote declaration-origin resolution and its evidence.
- Whether `Derived` associations may participate in verified import binding.
- Cross-file export unit/dependency tracking, if required.
- Type/value matching for imports and overload/default subtleties.
- Extended consumer queries/conveniences.

## 7. Risks register

| Risk | Mitigation |
| --- | --- |
| `Derived` mistaken for verified binding | Distinct evidence + provider docs + `Resolved` filter; consumer tests |
| Representation change conflicts with accepted D-05 intent | Owner approval + independent design review; F01 gate explicitly satisfied |
| Name-converged attribution remains limited | Derived association + occurrence/module provenance; no identity migration claimed |
| Overloads/merging/duplicates produce multiple edges | Documented; coverage; no one-to-one claim |
| Re-export/imported entries mistaken for bindings | `export-via` values, no `exported-as`, consumer docs; owner decision |
| Coverage fields over/under-report | Exact per-kind mapping; optional deferred counter; tests |
| Per-file ownership insufficient for EC-6D | Explicit EC-6D design gate for cross-file work |
| Properties not queryable | Names/ids carry identity; properties documented as client-side |
| Absence over-interpretation | D-20-style absence statement in consumer docs |
| Profile/version drift | D-16 discipline; p9 + export resolver version in lockstep |
| Quoted/Unicode/escape names | Bounded, no decoding, unsupported escapes; UTF-16 tests |

## 8. Blockers

No architectural blocker to a truthful export representation has been found.
The material blockers to implementation are **owner decisions** D-EC6C-01
through D-EC6C-12 and the independent design review, not the graph model. No
core graph redesign, schema change, or identity migration is proposed.
