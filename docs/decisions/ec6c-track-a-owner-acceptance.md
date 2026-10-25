# EC6C-A1/A2 — Track A owner acceptance and A29 closure

**Project:** Penghou.Hetu (future Andirit)
**Milestone:** EC6C-A1/A2
**A1 disposition:** EC6C-A1 ACCEPTED WITH OPEN DECISIONS (historical at A1 completion)
**Current status:** EC6C-A2 ACCEPTED — TRACK A IMPLEMENTATION READY
**Scope:** Documentation and owner-decision reconciliation only
**Production implementation:** NOT AUTHORIZED
**Next implementation target:** EC-6C.1 Structural Export Surface, after separate implementation authorization; EC6C-A2 does not start it

## 1. Git and evidence binding

| Evidence | Exact binding |
| --- | --- |
| Repository | jenolaszlo-sketch/penghou-hetu |
| Canonical checkout / main baseline | 558f8ac82ac1b5e07da68f4a71a70240eaf38409 (tree 08a01a1b7d5d3109b991bcd05d57272fd3f5f018); main unchanged |
| Accepted EC-6B baseline | 038295f26ce76a8747e923f9e0ef971e773c166e (tree 8779e00d57bd0b40a3ff89d16ca771e79cc950bf) |
| EC6C-D0 design tip | d12fe1882c814a0863f872e3913ddff921d31bee (tree fed4ff117b28899ccbb9032d0fded2084dc59523) |
| Independent EC6C-GLM-AR1 review | 49a01393375a345cefe0ca30b1f58fee1fe24e04 (tree 827f83f38221548c168e272700410a9da6cb525f; parent d12fe1882c814a0863f872e3913ddff921d31bee) |
| Reviewer | GPT-6 in Codex runtime; exact serving build unavailable. EC6C-GLM-AR1 is the supplied review ID, not a claim that GLM performed it. |
| Acceptance branch and parent | decisions/ec6c-track-a-owner-acceptance, created at d12fe1882c814a0863f872e3913ddff921d31bee |
| Acceptance worktree | C:/Users/Laszlos/source/repos/Penghou.Hetu.ec6c-track-a-owner-acceptance |

The acceptance worktree was created directly from the requested D0 tip. The independent review is read from its immutable Git commit and is not merged into this worktree. The design, canonical main, EC-6B owner record, and review branch are not modified by this record. No pull, merge, rebase, reset or push is part of EC6C-A1.

Sources read: all four EC6C-D0 design files; the immutable review report and decision reconciliation at the review commit; EC-6B and EC-6A owner records; graph identity and module-resolution guidance; graph fact/evidence contracts; ECMAScript extraction, session, query, planner, indexing lifecycle, in-memory and LatticeDb store code.

No probes, tests, export implementation, or p8-to-p9 migration were executed in EC6C-A1. Existing Q1/Q2 evidence remains unchanged and is not represented as A1 execution.

## 2. EC6C-D0 disposition

The owner accepts a bounded structural Track A direction, not the entire EC6C-D0 proposal.

Accepted direction:

- Export-entry nodes identify a module’s written exported-name slots.
- One Structural exports relationship represents each accepted written occurrence.
- Name-slot identity is independent of declarations and occurrences.
- Type/value observations are occurrence-level and preserve unknown.
- Track A is positive-only and does not claim effective export completeness.
- Named, star and namespace re-exports are excluded.
- Per-file ownership, profile p9 in both plugins, and the existing atomic publication model are the intended implementation contract.
- No declaration attribution ships in Track A.

The original alternative comparison and design reasoning remain available in the D0 documents. Conflicting D0 proposals are superseded by the A1 addenda at the beginning of each design file. D0’s “recommended” decisions were proposals, not owner approvals.

## 3. Independent review findings and A1 disposition

| Review finding | A1 disposition |
| --- | --- |
| F01 — counters did not provide queryable per-file completeness | Accept positive-only semantics. Add no completeness marker or export counter. |
| F02 — one entry can span type/value namespace slots | Entry is a name slot; put type/value/both/unknown on each occurrence, never as an entry-level union. |
| F03 — same-file physical declarations can share a path-scoped node | No declaration relationship in Track A. |
| F04 — Derived does not make a converged node occurrence-specific | No declaration relationship in Track A; defer all Derived attribution. |
| F05 — current extraction has no module-scope binding table | Defer all local lookup and binding to Track B design. |
| F06 — named re-export omitted its remote source name | Exclude named, star and namespace re-exports entirely in A1. |
| F07 — D0 overstated prior EC-6A status | Correct the history below. A1 source verification found that the immutable AR1 finding’s explanation itself says D-05 was accepted under the F01 gate; the EC-6A owner record actually lists D-05 name-based targets as explicitly not accepted. The review commit remains unchanged. F07’s overall warning about D0 overstating prior approval is preserved; this record uses the authoritative EC-6A text. |
| F08 — exact-name lookup gains candidates | Accept additive declaration-less candidates. The existing query has no kind filter and accepted consumer guidance already says results need not be declarations. Preserve old IDs and ordering among all matches; consumers distinguish by Kind/SymbolId. |
| F09 — TryAdd can suppress a conflicting edge before validation | Require full-fact collision comparison before insertion; any differing fact with the same ID fails extraction and is not published. |

### Historical EC-6A decision labels

The authoritative EC-6A owner record, §3 and §7, explicitly lists D-05 name-based export targets, D-06 imported-binding exports, and D-11 anonymous default declaration/binding edges as not accepted at that milestone. The D-10 CommonJS binding deferral remains an accepted scope boundary. F01/O-02 remained a gate for converged export/bind targets. D0’s claim that D-05 was previously accepted with a gate, and that D-06/D-11 were already accepted boundaries reconciled by D0, is corrected in the current decision register. A1 makes new bounded Track A decisions; it does not rewrite EC-6A history.

## 4. Owner-accepted Track A direction

Track A answers only:

**Which exported names were directly observed in the supported syntax of this module?**

It can report observed names and occurrences. It does not answer which physical declaration a name binds to.

Accepted graph shape:

~~~text
file --exports--> export entry
~~~

An export entry is a module-scoped name slot, not a declaration or verified binding. Track A emits one Structural exports edge for every supported written occurrence. It emits no exported-as edge and no declaration-binding relationship under any evidence quality.

## 5. Corrected structural export contract

Add node kind export and edge kind exports. For each supported module/name pair:

- Entry Id uses E(modulePath, exportedName) in §6.
- Entry Name and QualifiedName equal the canonical exported name.
- Entry SymbolId is null.
- Entry carries the existing language property and no namespace union property.
- Edge source is the owning file node; target is the entry.
- Edge Evidence.Quality is Structural.
- Edge location is the exact written exported-name token.
- Edge properties are export-kind (named/default), export-via (direct/local/expression), local-name when syntactically present, type-only=true only for explicit syntax, and namespace-projection (type/value/both/unknown).
- No remote via-module, declaration node, export provider resolver, or completeness property exists in A1.

The syntax allowlist and per-case expected graph facts are in the [A01–A29 acceptance matrix](../design/ec6c-export-adversarial-cases.md#current-ec6c-a2-structural-acceptance-matrix).

## 6. Name-slot identity and type/value semantics

### Entry identity

~~~text
{language}:export:{lowercase-hex(SHA-256(UTF-8(modulePath + LF + exportedName)))}
~~~

language is exactly javascript or typescript. modulePath is the normalized repository-relative POSIX file path used by the owning file node. UTF-8 has no BOM; SHA-256 renders lowercase hexadecimal. No Unicode normalization is applied; name matching is ordinal and case-sensitive. The identity does not include a declaration, binding, occurrence, source span or inferred runtime fact.

For a simple quoted exported name without escapes, exportedName is its string value without quotes. Identifiers retain their exact spelling. The default name is default. Escaped, empty, all-whitespace, control-containing, undecodable and over-bound names are unsupported and never truncated.

A29 closes the exported-name bound at 256 Unicode scalar values. A Unicode scalar excludes U+D800–U+DFFF; a valid surrogate pair counts once and each combining mark counts separately. Strict validation must report malformed UTF-16 and reject unpaired surrogates before identity construction. Do not count with .NET string.Length, normalize, replace, or truncate. The 256 UTF-16-code-unit alternative is rejected. UTF-16 remains the source-coordinate unit. See the [A29 closure addendum](ec6c-a29-closure.md) and normative matrix fixtures.

### Per-occurrence namespace projection

The entry has no type/value aggregate. Each Structural occurrence has exactly one namespace-projection value:

| Supported syntax evidence | Projection |
| --- | --- |
| Direct variable or function declaration; JavaScript value export | value |
| Direct interface or type alias | type |
| Direct class or enum | both |
| Explicit type-only local export list | type, with type-only=true |
| Normal local export list, including an imported local alias | unknown |
| Default expression or function declaration | value |
| Direct default class declaration | both |
| Namespace/ambiguous syntax | unknown or excluded if its exact public name cannot be established |

type-only describes a written modifier. It is not a synonym for projection. Both is an observation from that occurrence’s syntax; it does not mean the entry has one binding or that another occurrence is equivalent. A future EC-6D binding must select an occurrence projection and establish compatibility for the import use. Unknown is not binding evidence.

When type and value declarations share Item, the graph has one Item entry and two occurrence edges with independent projections. When several written sites contribute the same projection, each site remains separately visible.

## 7. Occurrence identity and collision handling

The accepted identity fields are edge kind, source file node ID, target entry ID, exported name, export kind, export form, local-name-presence and value, explicit type-only state, occurrence projection, and the exact source span. Use the existing newline-separated UTF-8 SHA-256 edge identity convention; all accepted names and paths are control-free, making the encoding unambiguous. Exact serialized formula and span conventions are in architecture §5.

The span is the written public-name token: declaration name for direct forms, alias/public name for a local list, and default keyword for defaults. Positions are one-based; columns are UTF-16 code units; end is exclusive.

The emitter constructs the complete fact before insertion. If the ID is unused, insert. If the ID exists and every fact field, evidence field, property and span matches, treat it as the same source occurrence produced twice and no-op. If any value differs, raise a fatal identity-collision error before source-unit completion. Do not choose a winner by enumeration order. Existing transaction behavior retains the previous published file unit.

Entry identity stays stable when an occurrence moves or duplicate occurrence is added/removed. The occurrence ID changes when its span, form, local name, type-only flag or projection changes. Alias rename changes the entry and occurrence IDs. CRLF/LF-only change preserves positions and IDs. Repeated identical extraction reproduces the same facts.

## 8. Positive-only coverage meaning

A1 adds no per-file completeness system, export coverage rows, or unresolved counter. Existing CodeGraphExtractionResult coverage is run-level and is not a queryable surface inventory; it is not used to answer export questions.

- Nonempty result means only that these supported occurrences were observed.
- Empty result means only that no supported occurrence was recorded.
- Empty does not mean the module has no exports.
- Unsupported, malformed, omitted, remote, or not-yet-supported forms may be present even when no entry is returned.
- Valid supported sibling nodes may still produce facts in a parser-recovered file; this does not make the inventory complete.
- Every Track A entry is unbound by design. “Unattributed” does not mean a declaration lookup failed.
- A truncated neighborhood is only a subset of positive observations.

No consumer may infer a complete effective export surface, valid runtime binding, physical declaration, remote target, or import binding. Do not reuse UnresolvedRelationships for exports.

## 9. Re-export and imported-local scope

Named re-exports with a from clause, star re-exports, namespace re-exports, remote binding, closure, cycles, conflicts, CommonJS and TypeScript export equals are excluded from A1. No export entry or exports edge is emitted for those forms. Existing module resolver facts such as resolves-to remain governed by resolver2.relative-v1 and do not assert an export name.

A local list after an imported binding, such as:

~~~ts
import { remote as local } from "./other";
export { local as publicName };
~~~

is a supported local-list observation: create entry publicName and its Structural occurrence with local-name local and projection unknown. Do not record the import’s remote name or specifier on the export fact. Do not claim the source is a local declaration or that the remote binding resolved. This remains distinct from export { remote as publicName } from "./other", which is excluded.

## 10. Exact-name query compatibility

Source-level verification:

- FindSymbolsByQualifiedNameAsync delegates to a store lookup matching exact QualifiedName across nodes and sorts candidates by Id. It has no node-kind or SymbolId filter.
- The accepted consumer guidance, incorporated before EC6C-A1, states that candidate results can include declaration-less graph nodes and instructs callers to check SymbolId before treating a candidate as a declared symbol. It makes no declaration-only promise.
- FindDeclarationsAsync requires SymbolId; export nodes have none.
- GetNeighborhoodAsync can traverse the file node filtered to exports and returns edges with evidence/properties and endpoint nodes. It is bounded; consumers check Truncated.
- FindReferencesAsync uses the references preset. Impact/dependency presets are closed lists and do not gain exports.
- Generic name-pattern search defaults to symbol-bearing nodes; export entries have no SymbolId and therefore do not enter that default symbol set.

A QualifiedName lookup for Item may gain a Kind=export candidate alongside existing declaration-less or declaration-bearing candidates, sorted by Id. This is the existing all-kind candidate contract as the graph grows, not a declaration result. Existing candidate IDs remain. Declaration-only callers filter SymbolId or Kind before declaration navigation. Export consumers traverse the file’s exports edges and filter Kind=export. No new query API or accepted public API change is required. Update consumer guidance and add A25 compatibility assertions before implementation.

## 11. Publication and p9 migration requirements

All entry nodes and occurrence edges are owned by their file unit. Endpoints are staged in the same replacement. Completed replacement atomically removes stale export facts; empty completion clears all old export facts for that file; file deletion removes its unit. Failure, collision and cancellation before completion do not publish partial facts. Durable reopen preserves the published facts.

Advance the shared ECMAScript profile from p8 to p9 in both JavaScript and TypeScript plugins. The existing plugin Version embeds ProfileRevision, and CodeIndexPlanner compares plugin version and source hash, so p9 forces both plugin outputs to be regenerated. Provider version for new exports is packageVersion+exports1.local-v1, matching the existing resolver pattern packageVersion+resolver2.relative-v1. The provider is structural, not semantic. Keep the module resolver provider/policy resolver2.relative-v1 unchanged. Do not migrate any existing graph identity.

Future qualification must seed durable state with the actual accepted p8 extractor, reindex unchanged sources with p9, compare node/declaration/structural/unchanged resolves-to IDs, verify both plugins ran, verify only supported export facts were added, test exact replacement/failure/cancellation, and close/reopen LatticeDb. EC6C-A1 has not run these tests.

## 12. Acceptance matrix reference

The [A01–A29 matrix](../design/ec6c-export-adversarial-cases.md#current-ec6c-a2-structural-acceptance-matrix) specifies each syntax/input, expected entry and occurrence, identity inputs, evidence, location, namespace treatment, omission behavior, publication result, required test and independent gate. The matrix includes exact-name queries, both stores, collision handling, deterministic repeat extraction, p8-to-p9 migration, no attribution and positive-only interpretation.

## 13. Reconciliation of D-EC6C-01 through D-EC6C-14

This table records the original proposal, independent-review evidence, current owner disposition, correction, accepted scope, deferred work, implementation prerequisite, and whether another owner decision is required.

| Decision | Original D0 proposal | AR1 / source evidence | A1 owner disposition | Required correction | Accepted Track A scope | Deferred scope / implementation prerequisite | Further owner approval |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D-EC6C-01 | Export entries plus exports and exported-as; reject declaration-only/hybrid. | Entry separates public name from declaration; no target is needed for unresolved/default-expression names. | ACCEPTED, structural-only. | Remove exported-as from accepted stage. | File-to-entry Structural occurrence only. | Attribution remains Track B. | No additional Track A choice. |
| D-EC6C-02 | One node per module path and name; D0 limit 256 characters. | Module/name slot is stable; type/value facets are not one binding. | ACCEPTED; A29 closed in EC6C-A2. | Exact formula; maximum 256 Unicode scalar values; strict malformed-UTF-16 rejection; ordinal, case-sensitive and unnormalized. | Entry identity independent of declaration, import and span. | A29 boundary fixtures are normative; source compatibility is COMPATIBLE WITH LOCAL VALIDATION. | No further Track A owner choice. |
| D-EC6C-03 | One span-anchored occurrence edge with form/local/type-only discriminators. | UTF-16 coordinates accepted; properties are not identity; existing TryAdd can silently drop collisions. | ACCEPTED with corrected tuple. | Include public name, form, local-name presence/value, type-only, occurrence projection and span; fail before insert on conflict. | Separate deterministic edge per accepted written site. | Re-export identity is EC-6D work. | No further Track A choice. |
| D-EC6C-04 | Direct Resolved, unique local lookup, name-converged Derived. | Same-file declarations can converge; p8 lacks a module-scope table; Derived does not identify target occurrence. | DEFERRED / NOT ACCEPTED. | No export attribution quality or edge in A1. | None. | Physical record uniqueness, scope, merging, overload and evidence need Track B design. | YES: new Track B decision. |
| D-EC6C-05 | Structural extraction provider; distinct versioned resolver for attribution. | CodeEvidence provider/version are extensible; planner separately uses plugin Version; resolver already has separate provider. | ACCEPTED for Structural surface provider only. | Use export-surface provider and packageVersion+exports1.local-v1; no resolver provider. | Structural occurrence provenance. | Any attribution provider is deferred with Track B. | No Track A choice; Track B later. |
| D-EC6C-06 | Entry-level export-space union plus occurrence type-only. | Item may occupy both namespaces; aggregate slot is not a binding. | ACCEPTED with per-occurrence projection. | No entry namespace union; occurrence values type/value/both/unknown, explicit type-only separate. | Syntax-established facets only. | EC-6D must select compatible projection. | No further Track A choice. |
| D-EC6C-07 | Entry default for all default forms; some attribution. | Default syntax is observable; EC-6A D-11 had not accepted declaration/binding edges. | ACCEPTED as a new A1 surface decision. | Name-only default entry and Structural occurrence; no declaration link. | Supported named/anonymous default forms. | Default binding is later Track B/EC-6D. | No further Track A choice. |
| D-EC6C-08 | Per-file unit and existing atomic publication. | File units/endpoint validation/atomic replacement exist; no export lifecycle has run. | ACCEPTED in principle. | Same file unit owns entry and occurrences; retain transaction behavior. | Per-file local facts. | Both stores, failure, cancellation and durable reopen qualify implementation. | No new choice; qualification gate remains. |
| D-EC6C-09 | Structural entries for re-exports and imported locals. | Named re-export model loses remote name; EC-6A D-06 had not accepted imported binding. | REVISED. | Exclude named/star/namespace re-exports. Local export list can be structural-only even for imported local value. | Local-list name observation; no source binding. | Remote names, module resolution-to-export relation, closure/cycles to EC-6D. | No local-list choice; new decision needed for future remote forms. |
| D-EC6C-10 | Existing traversal and exact-name lookup; no new method. | Lookup returns all exact-QN nodes sorted by ID; consumer guidance already permits declaration-less candidates; traversal is bounded. | ACCEPTED with explicit candidate behavior. | Kind=export candidate; SymbolId-based declaration navigation; inspect Truncated. | Existing file traversal and exact name. | No completeness API; no new method. | No further choice; update consumer doc done. |
| D-EC6C-11 | p9 both plugins; exports1.local-v1; resolver unchanged; no migration. | Profile revision is embedded in plugin Version; planner compares it; migration not run. | ACCEPTED as future contract. | p9 both; Structural provider version packageVersion+exports1.local-v1; resolver2.relative-v1 unchanged. | Additive re-extraction, no identity migration. | Authentic durable p8-to-p9 qualification. | No new choice; evidence gate remains. |
| D-EC6C-12 | Per-kind counters and possible deferred counter. | Counters are run-level and dropped from indexing diagnostics; no per-file query result. | REJECTED for A1. | Do not add completeness status/counter. | Positive-only inventory only. | Effective surface needs later design. | No further choice. |
| D-EC6C-13 | Keep readable names without attribution; absence means not established. | No per-file omission reason can be queried; Track A does no attribution. | ACCEPTED with positive-only wording. | Empty means no supported occurrence recorded; all A1 facts are unbound by design. | Positive syntax facts. | Unsupported and effective forms are not enumerated. | No further choice. |
| D-EC6C-14 | Entry as future EC-6D endpoint; defer binding/closure; no Derived promotion. | Shared slot may have several projections; Derived is not verified binding proof. | ACCEPTED. | EC-6D independently establishes binding and namespace match. | Stable entry endpoint only. | Remote closure, cycles, conflicts, cross-file ownership. | Separate EC-6D owner decision. |

## 14. Track B remediation requirements

Track B remains **DESIGN REMEDIATION REQUIRED**. The original D-EC6C-04/05 attribution recommendations are not accepted for implementation. Defer every exported-as edge and all Resolved or Derived export-attribution claims.

A separate future design must specify:

- A conservative syntax-level module-scope table and import-local classification.
- Physical declaration-record uniqueness, not graph-node count.
- Direct source-span ownership and exact declaration-record joins.
- Same-file merging, overloads, duplicate declarations, ambient and destructured forms.
- Type/value compatibility for local bindings.
- What Resolved proves when node identity can still merge physical occurrences.
- Whether name-converged relations are omitted or defined only as occurrence associations joined by path/span, never binding proof.
- No identity migration merely to broaden EC-6C.
- An independent review and owner decision before any Track B implementation.

## 15. Deferred EC-6D decisions

EC-6D or later design owns named/star/namespace re-export source names, remote target resolution, imported-binding edges, star closure, cycles, conflicts, type/value import matching, cross-file dependency tracking, effective-surface completeness and any cross-file export unit. A Track A entry is a possible future endpoint, not a verified binding.

Derived associations cannot be promoted to Resolved import bindings automatically. No EC-6D work is authorized here.

## 16. EC6C-A2 A29 decision closure

The owner selected 256 Unicode scalar values as the maximum exported-name length. A scalar is a Unicode code point outside U+D800–U+DFFF. A valid UTF-16 surrogate pair counts once; each combining mark counts separately. Strict validation must report and reject unpaired high or low surrogates before hashing, serialization, or publication. Empty, malformed, all-whitespace, control-containing, and over-bound names produce no export fact and are never replaced, normalized, or truncated.

256 UTF-16 code units is rejected as the name-counting unit. UTF-16 remains the existing source-coordinate convention. No graph-core naming or coordinate contract changes.

Source inspection found no name-field cap in CodeGraphNode.Name, QualifiedName, graph identifiers, exact-name query, in-memory storage, or the pinned LatticeDb provider API. The compatibility result is COMPATIBLE WITH LOCAL VALIDATION: future extraction-local strict validation is required. This is not durable runtime qualification; both-store round-trip and migration gates remain future EC-6C.3 work.

The [A29 closure addendum](ec6c-a29-closure.md) records the boundary fixtures, identity implications, risks, evidence, and readiness assessment. D-EC6C-01 through D-EC6C-14 retain their A1 dispositions except that D-02’s A29 alternative is now resolved. Track B remains DESIGN REMEDIATION REQUIRED; no exported-as relationship is authorized.

## 17. Implementation authorization boundary

EC6C-A1 accepted the structural-only direction; EC6C-A2 closes A29 and establishes Track A implementation readiness. Neither milestone authorizes production implementation, tests, merge, release, EC-6D, Track B remediation, or identity migration. A separate authorization is required before EC-6C.1 begins. No production code or tests were changed in EC6C-A1/A2.
