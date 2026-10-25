# EC6C-GLM-AR1 — Independent export representation architecture review

**Project:** Penghou.Hetu (future Andirit)
**Review ID:** EC6C-GLM-AR1 (retained as supplied; it is not a reviewer attribution)
**Actual reviewer:** GPT-6, Codex runtime. The exact serving build/version is not exposed in this session. This review is not attributed to GLM or Luna.
**Review type:** Independent adversarial architecture and contract review
**Design reviewed:** EC6C-D0, export representation architecture
**Production implementation / design remediation:** Not authorized or performed
**Review disposition:** READY WITH QUALIFICATIONS overall; Track A is conditionally ready, Track B requires design remediation

## A. Git binding

| Binding | Verified value |
| --- | --- |
| Repository | jenolaszlo-sketch/penghou-hetu |
| Qualified canonical baseline | 558f8ac82ac1b5e07da68f4a71a70240eaf38409 |
| Accepted EC-6B commit | 038295f26ce76a8747e923f9e0ef971e773c166e |
| Accepted EC-6B tree | 8779e00d57bd0b40a3ff89d16ca771e79cc950bf |
| EC6C-D0 architecture | 09035f673df042c78683d5f01aac193ae657ec21 |
| EC6C-D0 adversarial cases | 6f7fa6379533c0f29caa67ccc931f7ab8fcd65de |
| EC6C-D0 roadmap | 21e3cd77385bbf513751034878b6ef1f60f69fff |
| EC6C-D0 decision register / reviewed tip | d12fe1882c814a0863f872e3913ddff921d31bee |
| Reviewed design tree | fed4ff117b28899ccbb9032d0fded2084dc59523 |
| Review parent | d12fe1882c814a0863f872e3913ddff921d31bee |
| Review branch / worktree | review/ec6c-glm-ar1 · C:/Users/Laszlos/source/repos/Penghou.Hetu.review-ec6c-glm-ar1 |

The four design commits resolve to the full identities above and form the linear tip ancestry. The expected final design tree matches. Both the qualified canonical baseline and accepted EC-6B commit are ancestors of the review tip. The design worktree and canonical main were clean at inspection. No pull, merge, rebase, reset, or push was performed. The review branch was created directly from the specified design tip.

## B. Evidence categories

This review distinguishes verified existing behavior, the EC6C-D0 proposal, and unverified future behavior. No export implementation exists at this revision.


## C. Existing graph contract verification

| Area | Existing contract verified at the accepted EC-6B baseline | Consequence for EC-6C |
| --- | --- | --- |
| Node/edge identity | IDs are separate from properties. Edge equality includes properties, but properties do not participate in edge identity. | Occurrence discriminators must include each property whose change distinguishes the fact. |
| Evidence | Structural is directly observed syntax; Resolved is resolved by a named provider in its analysis context; Derived is computed from other evidence and must never present as Resolved. Qualities are not ordered confidence levels. | Derived cannot repair an incorrect target identity. Each relationship needs an exact claim. |
| Declarations | CodeGraphDeclaration records one physical declaration, but graph edges target CodeGraphNode, not declaration records. A symbol node can have many records. | Edge location can identify an occurrence, but the endpoint may remain merged or converged. |
| ECMAScript identities | Top-level variables, classes, interfaces are path-scoped; functions, type aliases, enums, namespaces are name-scoped and may converge across files. | Path-scoped does not mean physical-occurrence-specific if same-file declarations merge. |
| Callable variable projection | A function-valued variable emits a path-scoped field declaration and a separate name-scoped callable projection from the same declarator. | The field is the declared variable. The projection is not a second declaration. |
| Scope state | Scope tracks container, owner node, file node and owner kind. The walker has an isFileLevel Boolean. There is no module-scope binding table. Imports create module placeholders, not local binding declaration records. | Proposed local lookup is new behavior, not an existing verified capability. |
| Coordinates | Accepted EC-6B-R1 coordinates are one-based UTF-16 code-unit columns, end-exclusive. LF/CRLF identity stability is accepted. | Preserve this contract for occurrence identities. |
| Publication | Tree-sitter emits per-file units and a plugin-owned module-resolution unit. Completed units stage invisibly; publication is atomic at run completion. Endpoint validation sees the prospective graph. Failed/cancelled runs retain the previous publication. | Per-file export facts fit existing replacement if all their endpoints share the owning file unit. This is feasibility evidence, not an export lifecycle test. |
| Stores | In-memory and LatticeDb treat graph kinds/properties as opaque. LatticeDb persists normalized graph facts and a generic native mirror. | No store schema change is apparent; export durable round-trip is untested. |
| Query behavior | Exact QualifiedName lookup returns every matching node, ordered by node ID, with no kind filter. Traversals return edge evidence/properties; edge kind, direction and evidence can be filtered; properties are client-side. | Export nodes sharing a name with declarations become additional exact-name candidates. |
| Presets/references | Traversal presets are closed sets. FindReferencesAsync uses references; impact/dependency presets exclude arbitrary new kinds. | New exported-as edges do not affect these APIs unless the sets are changed. |
| Versioning | ECMAScript profile is p8 for both plugins. Effective plugin version includes profile revision; planner compares plugin version and source hash. | p9 on both plugins should force re-extraction. No p8→p9 execution exists at this revision. |
| Coverage | CodeGraphExtractionResult has run coverage. Tree-sitter currently emits run-level coverage only. CodeIndexingLifecycle diagnostics do not carry RelationshipCoverage; publication/query models do not persist it. | D-EC6C-12 counters are not queryable from the published graph or ordinary indexing result. |

### Source and execution evidence

Inspected source includes:

- src/Penghou.Hetu.Abstractions/CodeGraphFacts.cs and CodeEvidence.cs.
- src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs.
- src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs, TreeSitterExtraction.cs and TreeSitterExtractionSession.cs.
- src/Penghou.Hetu/CodeIndexPlanner.cs, CodeIndexingLifecycle.cs, CodeGraphQueryService.cs and InMemoryCodeGraphStore.cs.
- src/Penghou.Hetu.LatticeDb/NativeGraphMirror.cs and LatticeCodeGraphStore.cs.
- docs/ecmascript-symbol-identity.md, docs/ecmascript-module-resolution.md, docs/design/ec6a-cross-file-resolution-architecture.md, and the EC-6A/EC-6B owner records.

No new probes were run. Current p8 behavior is supported by source inspection plus preserved Q1/Q2 evidence. Q1 recorded 28 executed probes; Q2 recorded 45 closure/compatibility probes and an authentic p7→p8 migration. Those establish the accepted p8 runtime/publication contracts, not EC-6C export behavior. EC6C-D0’s temporary syntax probes are reported in its architecture document but were not re-executed here and do not test a design implementation. No test suite was run during this review.

## D. Export-entry representation assessment

| Alternative | Assessment |
| --- | --- |
| File → declaration-only exports | Reject. There is no honest declaration endpoint for anonymous/default expressions, unresolved names, imported bindings or named re-exports. The exported name and multiplicity become properties rather than identities, and converged declaration nodes remain ambiguous. |
| One entry per module and exported name, with occurrence edges | Recommend with qualifications. It separates the observed public name from declaration attribution and permits entries without local declaration endpoints. |
| Hybrid of declaration edges for attributable names and entries otherwise | Reject. Representation would depend on attribution success, and EC-6D would need two models. |

Track A’s entry should mean a **module-scoped exported-name slot with zero or more namespace projections**, not a unique declaration binding. The entry can truthfully record a written name and occurrence independently of its supplier. It becomes misleading if advertised as a complete effective module export surface while star exports and unsupported syntax are invisible to queries.

The graph shape file → entry via Structural exports occurrences, with no declaration associations in the first slice, is viable.



## E. Identity and namespace analysis

### Module and name identity

The proposed language + hash of repository-relative module path and exported name is a sound stable key for a name slot:

- File path is already the ECMAScript file-node identity scope and is normalized to repository-relative POSIX form.
- A file move changes the key, consistent with existing source-node identities.
- Names are ordinal and case-sensitive; no Unicode normalization is proposed.
- Hashing the exact UTF-8 name spelling keeps supported quoted/unescaped names distinct.
- A 256-character rejection bound is reasonable if “character” is defined as UTF-16 code units or Unicode scalar values. Overlong or escaped names must be unsupported, never truncated.
- The source path contract rejects control characters, so the LF separator between path and exported name is not ambiguous for indexed paths.

Entry identity should be independent of source occurrence and declaration identity. Renaming an alias changes the entry ID; moving an occurrence changes only the occurrence-edge ID; two modules exporting the same name retain separate entries.

### Namespace conclusion

**Recommendation: accept the current module+name identity with a normative namespace refinement. Do not add namespace to Track A identity.**

The entry means **one written exported name with separate type/value projections**. The property export-space = both is an aggregate, not proof of one declaration or permission to bind an arbitrary import use to either declaration.

~~~ts
export interface Item { id: string; }
export const Item = { id: "one" };
~~~

A single Item entry with export-space = both is a useful surface slot. It is not enough to establish which declaration supplies a particular type-only or value use. EC-6D must preserve and apply namespace on binding/use relationships. Type-only imports can bind only to type projections; value uses can bind only to value-capable projections; a normal TypeScript import may expose both facets. The entry ID may remain stable as facets change, but future binding facts must be recomputed.

Declaration-kind and type-only metadata could support this refinement, but the normative contract does not yet specify whether a future bind means the whole slot or a selected projection. The shared entry must not be described as one semantic binding.

### Occurrence identity and multiplicity

The proposed occurrence identity uses source node, entry target, export kind/via, local name, explicit type-only form, raw module specifier, and written-name span. Properties do not participate in edge identity, so each property that distinguishes an occurrence must remain in the discriminator. The span correctly distinguishes same-name duplicates and stable aliases; UTF-16 line/column behavior agrees with the accepted contract. Removing one of two occurrences should replace only that edge while retaining the entry and sibling occurrence.

Two gates remain:

1. Re-export source name is missing from the current property/discriminator model. A named alias re-export has a remote source name distinct from its entry name; see finding F06.
2. The extraction result stores edges in an ID-keyed dictionary and the existing profile helper uses TryAdd. A same-ID/different-fact collision could be silently discarded before batch/store validation sees it. A new export builder must compare full facts and fail; it cannot rely on downstream validators alone.

Default exports can use the name default regardless of whether a local named declaration exists. Named default declarations, anonymous defaults and expressions can have Structural occurrences without fabricated declaration nodes.

## F. Declaration attribution analysis

### Direct syntax and physical spans

Direct syntax can establish that an export occurrence syntactically contains a declaration. The p8 extractor emits per-declaration records, but those records are not graph endpoints. TryEmitNamed records the full definition span; the proposed attribution edge uses a declaration occurrence span, while the export occurrence may use only the written-name span. The new pass must match full AST declaration span and declaration kind to exactly one same-file declaration record, then separately document the edge location. Name equality is insufficient.

Direct syntax does not make a graph-node identity occurrence-specific. Current class/interface/variable identities are source path + name. Two physical same-name declarations in one file can still share one node:

~~~ts
export interface Item { a: string; }
export interface Item { b: number; }
~~~

The two interface records have different declaration IDs and spans but share a path+name interface node. Two Resolved exported-as edges can therefore point to the same node. D0’s Resolved definition says its target is occurrence-specific; that claim fails here. Either restrict Resolved to a target with exactly one physical declaration record under the supported model, or revise it to a symbol-level association and stop calling the node occurrence-specific.

### Local module-scope lookup

The proposed “exactly one eligible declaration and no import binding” rule is a plan, not an existing extractor contract. The p8 Scope does not resolve lexical names; imports do not emit local binding records; only supported simple file-level variables emit field declarations. The current code cannot establish the proposed lookup from graph nodes or exact-name lookup.

Minimum conditions for later verified lookup:

1. Build a syntax-level module-scope table from the complete parse tree, not graph names.
2. Count physical declarations, not distinct graph node IDs.
3. Distinguish module scope from nested block/function scopes, and treat var, let, const, function declarations and imports under a documented conservative subset.
4. Include import local bindings and refuse attribution when the name denotes an import.
5. Exclude destructured/ambient declarations, relevant parser-recovered syntax, overload/merge groups, duplicate declarations, and namespace-space collisions unless modeled.
6. Apply type/value compatibility before emitting an edge.
7. Resolve independent of source order by building the complete table before local-list/default-identifier attribution.
8. Join direct syntax by AST ownership and declaration record span, never name match.

This does not require a full TypeScript checker. It does require a narrower supported subset than “unique eligible declaration” alone.

### Name-converged attribution and the required example

~~~ts
// a.ts
export function run() {}

// b.ts
export function run() {}
~~~

p8 emits one shared callable(run) node with two declaration records. D0 would add two Derived edges from that shared node to module-specific run entries, with separate evidence locations and spans.

The query API returns edge evidence and properties. A client can identify the a.ts occurrence by filtering the Derived edge’s Evidence.Location.Path and span, then join that location to declaration records returned for the shared symbol. This is sufficient to reconstruct an **occurrence association** if every consumer performs that join. It is not enough for the shared node ID alone to identify a.ts’s declaration, and Derived is not itself a mitigation.

Recommendation: keep stage 4 optional and exclude name-converged exported-as edges from the first attribution release. Reopen only with a normative contract that calls the relation an occurrence association, not a declaration binding; requires the edge-location-to-declaration-record join; and forbids promoting it to Resolved or using it as EC-6D binding proof. An occurrence-specific target is an alternative if the owner needs physical declaration nodes and accepts the extra identity surface. Do not migrate declaration identities merely to complete EC-6C.

## G. Evidence analysis

| Fact | Minimum evidence | Recommended quality | Safe to publish? |
| --- | --- | --- | --- |
| Written exported name | AST observation of supported module-level export name | Structural | Yes, as a positive syntax fact only. |
| Default export occurrence | AST observation of default syntax | Structural | Yes; entry named default, no fabricated declaration for anonymous/expression forms. |
| Alias of local declaration | Exact module-scope binding proof plus identity/namespace eligibility | Resolved only for supported unique targets; otherwise no attribution | Later only. Name equality is not proof. |
| Direct declaration attribution | Direct AST ownership, exact declaration-record match, unique eligible identity, namespace compatibility | Resolved only under a narrower occurrence/merge rule | Not Track A; later under a separate gate. |
| Name-converged declaration | Exact declaration occurrence plus declaring file path and explicit occurrence-association semantics | Derived only if owner accepts this narrower meaning; never a verified binding | Defer from initial implementation. |
| Imported local alias | Import binding observed; remote binding unavailable | Structural occurrence only; no exported-as | Yes as surface only if D-06 is newly accepted and metadata is sufficient. |
| Re-exported name | Public name, source-side imported name, written module specifier | Structural occurrence only; no exported-as | Yes after owner decision and F06 correction. |
| Anonymous/default expression | Default AST occurrence, no declaration endpoint | Structural | Yes; do not fabricate a declaration. |

The extraction plugin is a suitable provider for Structural occurrence edges. A separate versioned export-resolver provider for attribution is reasonable, but its evidence claims remain owner decisions. EC-6A accepted Resolved only for resolves-to; it did not accept a general export attribution policy. Preserve global evidence meanings and the accepted policy-scoped resolves-to sentence.

## H. Syntax coverage and absence semantics

The syntax inventory is conservative in useful ways: only module-level export statements; no CommonJS surface; no nested namespace exports as module exports; no guessed declaration for anonymous defaults; no remote declaration claim for re-exports; grammar-aware handling of contextual type. Reuse the accepted EC-6B-R1 correction for type as an identifier.

D0 still confuses a set of observed supported names with a complete export inventory:

- export star from another module has no written exported name and therefore no entry.
- Unsupported names, ambient forms, destructuring, CommonJS, and parser-recovered subtrees can also produce no entry.
- A genuinely empty ESM module produces no entry.
- D0’s coverage rows would distinguish these in counters, but current CodeGraphExtractionResult coverage is not carried through ordinary indexing diagnostics and is not queryable from graph publication.
- Even if a caller retained the run result, current coverage is aggregate, not a per-occurrence reason for every missing attribution. The public diagnostics projection drops the coverage object.

A consumer-safe contract needs these states:

| State | Required consumer interpretation |
| --- | --- |
| Complete supported inventory | All recognized export forms for this module were classified; zero entries can mean a genuinely empty supported surface. |
| Partial inventory | Some constructs or source regions were deferred, unsupported, undecodable or parser-recovered. Entries are positive observations; absence is not negative evidence. |
| Unattributed named export | Entry and Structural occurrence exist, but no declaration association was established. It does not mean there is no declaration. |
| Unsupported construct | A recognized construct was deliberately omitted, and the graph exposes that omission or a queryable reason/count. |
| Genuine empty surface | Complete supported inventory with zero entries and no partial/unsupported marker. |

Minimum correction: publish queryable per-file export-surface status and omission reasons/counts in the same publication as the export facts. A run-only counter dropped by the indexing lifecycle does not satisfy the graph-query contract. If the owner rejects a graph coverage marker, narrow the contract to positive-only observations and explicitly deny completeness. That fallback cannot answer complete/partial/empty questions.



## I. Publication and compatibility

### Per-file ownership

Per-file ownership is the right EC-6C location for local export-name entries and occurrences. They depend on the owning file’s syntax and module-local facts. Existing replacement of a completed file unit removes stale nodes and edges; an empty completed file unit clears old export facts; deleting the file removes that unit. Same-unit endpoints satisfy the existing endpoint validator.

The existing transaction model makes P1–P10 feasible, but no export-fact lifecycle has been executed. Q1/Q2 establish atomic publication for the existing resolver unit, not the proposed export pass.

| Scenario | Existing support | EC-6C qualification still required |
| --- | --- | --- |
| P1 add export | File content change causes re-extraction | Assert new entry/occurrence appears in completed p9 publication. |
| P2 rename alias | Whole file unit is replaced | Assert old entry/edge removed, new entry/edge added, declaration identity retained. |
| P3 remove one duplicate occurrence | Per-file replacement | Assert sibling occurrence and shared entry remain. |
| P4 delete origin declaration | Per-file replacement | Structural name can remain; stale attribution must disappear. |
| P5 remove final export | Empty export subgraph is possible in completed file unit | Assert no stale export facts remain while other file facts survive. |
| P6 delete source file | Obsolete per-file unit is deleted | Assert no export facts or dangling endpoints in both stores. |
| P7 source edit, same export surface | Planner re-extracts changed content | Entry identity remains; occurrence ID changes only if discriminating fields/span change. |
| P8 export pass fails after staging | Failed run retains prior publication | Inject failure after staging; assert exact previous publication remains. |
| P9 cancellation | Cancellation before completion does not publish partial run | Assert prior export IDs remain in both stores. |
| P10 p8→p9 | Version comparison should invalidate both plugins | Execute authentic p8 durable seed/reopen/reindex; none exists yet. |

### p8→p9 and compatibility

p9 is the correct profile bump for new emitted facts. Because TypeScript and JavaScript share the profile, both effective plugin versions must advance. The module resolver remains resolver2.relative-v1. No existing identity migration is needed for export entries. The profile bump should re-extract unchanged files and replace existing file and modules units; no mixed revisions should be visible after atomic completion.

Required migration assertions:

- Seed LatticeDb using the real p8 source from 038295f, not a p9 simulation.
- Reopen with the real p9 implementation and index unchanged fixture sources.
- Both ECMAScript plugins execute due version change.
- Existing file, declaration, structural edge, import, and resolves-to IDs remain byte-identical; compare properties/evidence separately and explain any intentional additions to existing file nodes.
- Only export-model nodes/edges/coverage facts are added.
- Module-resolution edges retain resolver2.relative-v1 and their IDs.
- No p8 and p9 plugin origins are mixed in the final publication.
- Export facts survive durable close/reopen.
- Removal, alias rename, duplicate-occurrence removal, source deletion, failure, cancellation, and empty export replacement remove exactly the stale facts.

This review did not execute or claim a p8→p9 migration.

## J. Consumer-query analysis

Existing APIs can expose the proposed graph objects:

- Outgoing file traversal filtered to exports yields occurrence edges and export nodes.
- Incoming exports traversal yields module nodes.
- Incoming/outgoing exported-as traversal yields declaration nodes, edges, evidence locations, and properties.
- Exact-name lookup returns all matching QualifiedName candidates; it does not select a declaration.
- Declaration records can be queried by SymbolId and joined to an edge Evidence.Location by path/span.

Limits the design must document:

1. Property filters such as export-space, local-name, type-only, and attribution-basis are client-side. A bounded traversal can truncate before the client’s property filter; callers must inspect truncation.
2. Exact-name lookup has no node-kind parameter. An export entry named run and declaration named run both match the same QualifiedName. Existing callers relying on SingleOrDefault can become ambiguous.
3. A converged declaration node is not a physical declaration. Edge location and declaration records enable a client-side join, but the query API does not return a CodeDeclarationId on the edge.
4. No query can distinguish a complete empty surface from unsupported/deferred/partial coverage.
5. No query can select a unique EC-6D import binding from export-space = both without an explicit namespace-projection rule.
6. Structural import queries, FindReferencesAsync, impact presets, declaration lookup by SymbolId, and resolves-to semantics remain unchanged if new kinds stay out of existing closed presets. Generic unfiltered neighborhood results will include new graph facts by design.

## K. Adversarial cases

### K1. Existing C01–C35 review

| Case | Independent assessment | Track / correction |
| --- | --- | --- |
| C01 aliases of one declaration | Entry per alias and distinct spans are sound. Local attribution depends on the future scope table. | B; defer attribution. |
| C02 duplicate exported name from distinct declarations | Shared entry plus two occurrences is sound; aggregate ambiguity is not queryable. It incorrectly cites D-EC6C-08 as duplicate policy; D-08 is unit ownership. | A coverage; B attribution. Fix cross-reference. |
| C03 name-converged functions | Two module entries and one shared callable source are correct. Derived edges are safe only as occurrence associations with mandatory path/span join. | B; stage 4 deferred. |
| C04 direct exports | Positive occurrences are sound. Resolved for path-scoped kinds fails when same-file declarations merge to one node. | B; restrict Resolved. |
| C05 named local export | Surface fact is sound; no existing binding table proves lookup. | B; stage 3 gated. |
| C06 unresolved local name | Entry without attribution is truthful. Graph does not distinguish unresolved from ambiguous lookup. | A coverage; B reason. |
| C07 anonymous default function | Default entry is structurally truthful; D-11 was not owner-accepted, so this is a new owner decision. | A owner decision. |
| C08 default expression/identifier | Expression entry is sound; identifier attribution requires scoped table and value-space proof. | A surface; B gated. |
| C09 multi-declarator variable | One entry/occurrence per declarator is appropriate if each name span is exact. | A test gate. |
| C10 type identifier | Correct; EC-6B-R1 type/as rule and Q2 matrix support it. | A regression gate. |
| C11 type-only list | Correct as syntax; namespace compatibility is later semantics. | A test; B gated. |
| C12 class in both spaces | One name slot with both projections is useful, not one declaration binding. | A namespace refinement; B/EC-6D gate. |
| C13 overloads | Surface occurrences are valid; implementation-only attribution is a deliberate under-model. Signature records remain absent. | B limitation; no fabricated overload nodes. |
| C14 declaration merging | Multiple occurrences can be preserved. Interface merges expose the false “occurrence-specific path-scoped node” claim; local-list ambiguity must not use distinct node count. | B blocker. |
| C15 quoted/Unicode names | Unescaped names are plausible; define 256-character unit and test supplementary identifiers. Escaped names remain unsupported. | A test and coverage marker. |
| C16 parser-recovered syntax | Keeping valid siblings is consistent with parser policy; Partial is not queryable from graph. | A coverage blocker. |
| C17 alias removal | Per-file replacement should remove old entry/edge. Not executed for p9 facts. | A lifecycle gate. |
| C18 declaration deletion with export left | Entry should remain and attribution be withdrawn. Depends on unimplemented attribution. | B lifecycle gate. |
| C19 unchanged publication | Deterministic IDs and planner no-op are plausible; no export implementation exists to qualify. | A test gate. |
| C20 failure after staged facts | Existing atomic completion supports old facts; export-specific failure was not tested. | A lifecycle gate. |
| C21 named re-export | Structural entry can represent the public name, but source-side name is missing and D-06 is not owner-accepted. | A; decide D09 and fix F06. |
| C22 cross-plugin source | No remote declaration attribution is correct. A structural name can still be recorded independently. | A; D09 and coverage. |
| C23 LF/CRLF and supplementary Unicode | UTF-16 coordinates and LF/CRLF stability agree with accepted contract. | A identity fixture. |
| C24 same names across modules | Distinct entries are sound; shared callable source repeats C03’s Derived ambiguity. | B; stage 4 deferred. |
| C25 empty export inventory | Replacement can clear old facts; no marker distinguishes truly empty from omitted syntax. | A coverage blocker. |
| C26 type alias vs type-only list | Correct distinction; p8 grammar-aware type handling must remain. | A regression gate. |
| C27 imported local binding re-export | No local declaration association is right. D-06 was deferred, not accepted; structural entry is a new decision. | A owner decision. |
| C28 destructuring | Omission is conservative only if consumers can see partial/unsupported coverage. | A coverage blocker. |
| C29 ambient declaration | Omission is conservative only with visible coverage. | A coverage blocker. |
| C30 function-valued variable | Field is the declared path-scoped variable; callable is a projection and must not be attributed as a second declaration. | B restriction. |
| C31 escape/overlong/empty name | No truncation is right; without surface status a missing name is indistinguishable from empty. | A coverage blocker. |
| C32 type and value same name | One slot with union facets is sound if namespace projections are explicit. | A namespace refinement. |
| C33 type-only and value occurrences | Separate occurrence flags plus union space retain syntax; future binding needs namespace-aware matching. | A refinement; B/EC-6D gate. |
| C34 CommonJS assignment | Deferral preserves ESM boundary; graph consumer needs an unsupported marker. | A coverage blocker. |
| C35 export inside namespace | Excluding nested members from module surface is correct; exported namespace itself can remain an entry. | A scope test. |



## K2. Independent AR1 cases

These are review predictions from source/design inspection, not executed export-runtime tests. Identity/evidence describes the proposed facts and the existing graph rules that would govern them.

| ID | Minimal case and invariant challenged | Predicted facts, identity, evidence, and query reading | Assessment, correction, and track |
| --- | --- | --- | --- |
| AR1-01 | Same-name type and value: export interface Item {}; export const Item = {};. | One module+name entry, two Structural occurrences, aggregate space both. Entry ID has no namespace component. Exact-name lookup returns the entry plus any same-named declarations. | Truthful as a name slot, not one binding. Require normative type/value projections and EC-6D namespace matching. A refinement; B/EC-6D gate. |
| AR1-02 | Two files each declare and export function run. | One name-converged callable node, two physical declaration records, two entries, and (if D0 stage 4 runs) two Derived edges with source spans. | Consumers must join each edge location to its declaration record. Derived alone does not identify the physical occurrence or justify import binding. Defer stage 4. B. |
| AR1-03 | export { x as result }; export { y as result }; | One result entry and two occurrence edges if written spans/discriminators differ. Structural edges record the written name, not which declaration supplies it. | Surface aggregation is sound; expose duplicate/conflict status and never select one attribution arbitrarily. A coverage and B ambiguity. |
| AR1-04 | const value = 1; export { value as first }; export { value as second }; | Two entries keyed by first and second; two source-span occurrence edges; no declaration edge in Track A. | Sound. Later attribution needs exact module binding proof and distinct attribution edges. A ready subject to coverage. |
| AR1-05 | export { missing as publicName }; | publicName entry and Structural occurrence; no declaration target. The graph can query positive syntax, but absent attribution alone cannot distinguish unresolved, unsupported, or deferred. | Keep the surface fact; publish queryable per-file coverage and, for later attribution, reason classification. A coverage; B unresolved handling. |
| AR1-06 | import { remote as local } from "./dep"; export { local as publicName }; | Potential publicName Structural entry with local-name local and import/local-import classification; no local declaration attribution. Existing import model has no binding record. | D-06 makes this a new owner decision. Do not emit declaration attribution; either exclude in first slice or accept a precisely classified surface entry. A owner decision; B deferred. |
| AR1-07 | export { remote as publicName } from "./dep"; | publicName entry with raw via-module and Structural evidence. D0 stores the public name but not remote. | Inadequate for EC-6D to identify the remote export without reparsing. Add source-side name and discriminator, or suppress until EC-6D. A blocker if included. |
| AR1-08 | export * from "./dep"; | No written name, therefore no entry; normal export traversal is empty. Existing run-level coverage is not available per file through the query contract. | Must produce queryable partial/deferred coverage or empty result resembles an empty module. A coverage blocker. |
| AR1-09 | export default function () {} | Entry named default, Structural occurrence, no declaration node. This conflicts with broad readings of EC-6A D-11 if misread as forbidding any export fact. | Useful name-slot representation, but D-11 did not approve it. Owner must explicitly approve a name-only default exception. A owner decision. |
| AR1-10 | export const calculate = () => 42; | Surface entry calculate; p8 has a path-scoped field declaration and a separate name-scoped callable projection. | Any future attribution targets the field record/node only. Treating callable projection as another declaration is false. B eligibility test. |
| AR1-11 | Function overload signatures or merged interface declarations share one name. | Surface occurrences may be one entry; declaration records may be multiple and endpoints can converge. | Distinct graph-node count is not a uniqueness test. Keep Track A; omit attribution until physical records, merge groups, and overload policy are modeled. B blocker. |
| AR1-12 | A module has a top-level const item and a nested same-name declaration; export { item } occurs at module scope. | Existing p8 Scope does not bind names; graph name matches do not resolve the lexical module binding. | Build a complete syntax-level module table, count records, track imports/scopes, and reject malformed or ambiguous cases. B blocker. |
| AR1-13 | const type = 1; export { type }; export type { Item }; | First occurrence is a value identifier; second is a type-only occurrence. A text-only parser can misclassify the contextual word. | Preserve grammar-aware classification from EC-6B-R1; test both forms and retain occurrence-level type-only metadata. A regression gate. |
| AR1-14 | Type-only occurrence and value occurrence aggregate under one public name. | One entry with space both; occurrence edges carry different type-only flags. | Sound only as aggregate facets. Do not let entry-level both establish which occurrence/declaration satisfies a future import. A namespace refinement; B/EC-6D gate. |
| AR1-15 | Remove one of two duplicate occurrences while the other remains. | Entry ID remains because module+name is unchanged; removed span edge disappears; sibling edge remains if its own inputs do. | Per-file atomic replacement supports this shape, but export facts have no executed lifecycle evidence. Require both-store replacement test. A test gate. |
| AR1-16 | Unsupported syntax or parser recovery versus a genuinely empty module. | Both can yield zero export entries. D0 counters are run-level and are not in query results. | Cannot claim completeness or negative absence. Publish per-file coverage state/reasons or restrict the contract to positive observations. A blocker. |
| AR1-17 | Export name contains a supplementary Unicode scalar before the written-name span. | Edge coordinates are UTF-16 code units, end-exclusive; entry hashing uses exact UTF-8 name spelling. | Existing contract supports this if preserved. Define the 256 bound and add a fixture proving coordinates and stable ID. A qualification. |
| AR1-18 | Exact-name query for run after an export node and converged declaration node both exist. | Query returns all nodes with matching QualifiedName and no kind filter. It does not return a unique declaration. | Exact-name lookup is candidate search only. Document additive ambiguity; callers must filter node kind or a future API may be needed. A compatibility qualification. |
| AR1-19 | Export extraction fails after staging or is cancelled. | Existing transaction contract retains the prior completed publication; proposed export facts are invisible until completion. | Feasible by same-unit ownership, not proven for a new export pass. Require failure/cancellation assertions in InMemory and durable LatticeDb. A test gate. |
| AR1-20 | EC-6D import use binds against Item whose entry aggregates type and value facets. | One entry ID exists but represents a union slot; no unique declaration endpoint follows. | EC-6D must match use namespace against projections and source occurrences. Entry identity alone cannot choose a binding; Derived cannot be promoted. B/EC-6D gate. |
| AR1-21 | Two edge facts produce the same proposed ID but differ in properties. | Current profile helper inserts into an ID dictionary with TryAdd; the second fact may be dropped before downstream validation. | New export emission must compare complete facts at insertion and throw on same-ID/different-fact collisions. A implementation blocker until guarded. |




### K3. Roadmap feasibility

| Stage | Independent assessment |
| --- | --- |
| 6C.1 syntax inventory | Correct prerequisite. It needs an explicit inventory of supported versus omitted forms and a per-file coverage contract, not only counters. |
| 6C.2 entries, identities, publication, queries | Feasible without attribution. Split the stage from any local binding work; include collision guard, re-export source-name decision, type/value projections, exact-name compatibility, and visible completeness semantics. |
| 6C.3 local declaration attribution | Not ready as currently specified. Add a syntax-level module binding table, physical-record uniqueness, import/scope handling and namespace checks before emitting any Resolved edge. Keep this separate from Track A. |
| 6C.4 name-converged attribution | Keep optional and defer. The current endpoint is shared; evidence location can support a client-side occurrence association only if consumers perform the normative join. Do not present it as a binding or Resolved fact. |
| 6C.5 qualification and migration | Correct final gate. It must execute the authentic p8-to-p9 durable migration, verify old identity stability, and test new-fact replacement/failure/cancellation in both stores. No such export implementation evidence exists in this review. |

The proposed order is directionally sound, but stage 2’s stated completeness/query promises depend on adding coverage visibility, and stages 3–4 cannot be treated as straightforward follow-on implementation. The smallest independently reviewable Track A is 6C.1 + 6C.2 with no exported-as edges. No EC-6D closure, dependency tracking or import binding belongs in that slice.



## L. Findings

Severity order is highest first. Source references name accepted EC-6B contracts; section references point to EC6C-D0 unless stated otherwise.

### EC6C-AR1-F02 — Shared namespace slot is not a semantic binding

**Disposition:** DESIGN AMBIGUITY. **Severity:** P1.
**Design location:** architecture §§5.1, 5.3, 7.3, 13; decision D-EC6C-06.
**Existing contract:** CodeGraphFacts.cs separates identity from properties; exact-name query returns nodes, not binding projections.
**Counterexample:** interface Item plus const Item.
**Expected:** one public name slot may aggregate type and value projections.
**Proposed:** one entry with export-space both, but D0 does not define future import matching against the aggregate.
**Consequence:** EC-6D can bind a type use to a value-only declaration or treat the slot as one binding.
**Minimum remediation:** Normatively define one entry as a name slot, retain occurrence and declaration namespace facets, and require EC-6D namespace matching; otherwise split identity.
**Track A:** qualification, not a blocker if narrowed. **Track B:** block binding until resolved.
**Owner decision:** accept the shared-slot interpretation and EC-6D projection rule.

### EC6C-AR1-F03 — Resolved target can still merge physical declarations

**Disposition:** DESIGN DEFECT. **Severity:** P1.
**Design location:** architecture §§2.2, 7.1–7.2, 14.4; decision D-EC6C-04; roadmap 6C.3.
**Existing contract:** declaration records are occurrence-specific, graph edges target symbol nodes; class/interface/variable nodes are path+name scoped.
**Counterexample:** two same-file exported interface Item declarations; each has a distinct declaration record but the same path+name interface node.
**Expected:** Resolved declaration attribution identifies the actual declaration occurrence.
**Proposed:** two Resolved edges may terminate at the same merged node.
**Consequence:** the edge target is not occurrence-specific as D0 claims, even though edge locations differ.
**Minimum remediation:** Restrict Resolved to cases where the target node maps to exactly one eligible physical declaration record under the supported subset, or redefine the claim and use an occurrence-specific attribution representation.
**Track A:** none. **Track B:** blocks Resolved as written.
**Owner decision:** accept restriction or authorize a separate occurrence target model.

### EC6C-AR1-F04 — Derived does not cure name convergence

**Disposition:** DESIGN DEFECT. **Severity:** P1.
**Design location:** architecture §§7–8, 14.3; decisions D-EC6C-04/05; roadmap 6C.4.
**Existing contract:** callable/function/enum/namespace nodes can converge across files; query returns the shared node plus edge evidence and properties, not a declaration ID.
**Counterexample:** a.ts and b.ts each export function run.
**Expected:** a client can identify the a.ts physical occurrence without reading the shared node as its supplier.
**Proposed:** Derived edge from the shared callable node to each file’s export entry with occurrence properties.
**Consequence:** consumers who read endpoint identity alone see a cross-module association; Derived quality is a computation label, not an occurrence-specific target.
**Minimum remediation:** Defer stage 4. If retained later, define the edge strictly as an occurrence association, require joining Evidence.Location path/span to the declaration record, and prohibit using it as import-binding proof.
**Track A:** none. **Track B:** stage 4 blocked.
**Owner decision:** explicitly accept the narrower association or omit converged attribution.

### EC6C-AR1-F05 — Local lookup is future syntax analysis

**Disposition:** DESIGN AMBIGUITY. **Severity:** P1.
**Design location:** architecture §§7.1, 7.3; decision D-EC6C-04; roadmap 6C.3.
**Existing contract:** p8 Scope records containers/owner/file only; no lexical binding table; imports are module placeholders, not local declaration records.
**Counterexample:** a module with a top-level const and same-name nested declaration, imported local alias, overload, or two merged interface records.
**Expected:** local-list export binds only to one eligible module-scope declaration in the correct namespace.
**Proposed:** exactly one eligible declaration and no import binding, but eligibility/counting rules and table do not exist in current runtime.
**Consequence:** graph name equality can select a nested, imported, merged, or wrong-space node.
**Minimum remediation:** Build a complete syntax-level table before attribution; count declaration records, model imports/scopes, exclude recovered/ambient/destructured/merge/overload cases unless supported, and apply namespace checks.
**Track A:** none. **Track B:** blocks stage 3 until the subset is specified and implemented.
**Owner decision:** accept a conservative module-scope subset.

### EC6C-AR1-F01 — Coverage cannot answer per-file completeness

**Disposition:** DESIGN DEFECT. **Severity:** P2.
**Design location:** architecture §§6, 9.1, 10, 15; decisions D-EC6C-12/13; adversarial C16/C25/C28/C29/C34.
**Existing contract:** coverage is run-level in extraction results; indexing diagnostics discard it; query/published facts contain no per-file coverage.
**Counterexample:** export star from another module or unsupported CommonJS versus a genuinely empty module; both may return zero entries.
**Expected:** consumers distinguish complete-empty, partial, deferred, and unsupported surfaces.
**Proposed:** counters distinguish them only in design; no normal query sees those counters.
**Consequence:** absence can be misread as no exports or full coverage.
**Minimum remediation:** Add atomically published, queryable per-file surface status and omission reasons/counts; alternatively narrow the first-stage contract to positive-only facts and explicitly make absence non-evidence.
**Track A:** blocks completeness claims; positive-only Track A remains viable. **Track B:** coverage of attribution outcomes also needs a visible contract.
**Owner decision:** choose published per-file coverage or positive-only semantics.

### EC6C-AR1-F06 — Named re-export loses its remote source name

**Disposition:** DESIGN DEFECT. **Severity:** P2.
**Design location:** architecture §§5.1, 5.3, 6, 13; decisions D-EC6C-03/09; adversarial C21.
**Existing contract:** properties are not identity inputs; edge query exposes only recorded properties and span.
**Counterexample:** export { remote as publicName } from "./dep".
**Expected:** preserve publicName, remote, specifier, and written occurrence independently.
**Proposed:** publicName plus via-module, without remote.
**Consequence:** EC-6D cannot identify the remote export requested by the syntax without reparsing.
**Minimum remediation:** Add a source-side imported/local name property to the fact and discriminator; otherwise exclude named re-exports from the accepted initial slice.
**Track A:** blocks D09 named re-export support as written; no impact if excluded. **Track B:** no remote declaration attribution.
**Owner decision:** accept expanded D09 metadata or defer the form.

### EC6C-AR1-F07 — Prior owner-decision status is overstated

**Disposition:** DESIGN DEFECT. **Severity:** P2.
**Design location:** decision register §2 and D-EC6C-07/09; roadmap §13.
**Existing contract:** EC-6A owner record accepts D-05/name-based targets only under the F01 gate; D-06/imported-binding and D-11/anonymous defaults were deferred. It did not accept the new export-entry model or these exceptions.
**Counterexample:** D0 says imported-binding exports and anonymous default surface entries reconcile already accepted boundaries.
**Expected:** a new representation or scope exception is labeled as a new recommendation requiring owner acceptance.
**Proposed:** decision register §2 treats D-06 and D-11 as accepted boundaries reconciled by D0; D-05 acceptance is also broader than its accepted F01 gate.
**Consequence:** owner may believe the new policy was previously approved and skip a required decision.
**Minimum remediation:** Correct the historical status and list D-05 representation refinement, imported/local re-export surface entries, and default entries as new decisions.
**Track A:** owner gate for re-exports/default policy and entry model. **Track B:** owner gate for any D-05 attribution refinement.
**Owner decision:** affirm or reject each new scope explicitly.

### EC6C-AR1-F08 — Exact-name lookup is not compatibility-neutral

**Disposition:** QUALIFIED LIMITATION. **Severity:** P2.
**Design location:** architecture §§5.1, 10, 11; decision D-EC6C-10.
**Existing contract:** CodeGraphQueryService exact QualifiedName lookup has no node-kind filter and InMemoryCodeGraphStore returns every matching node.
**Counterexample:** before adding an export entry, exact lookup of run returns a declaration; afterward it returns the declaration and a new export entry.
**Expected:** existing declaration lookup keeps its documented result behavior or consumers receive an explicit additive-candidate contract.
**Proposed:** use existing exact-name API for entry lookup and claim existing query compatibility.
**Consequence:** SingleOrDefault callers may throw or stop returning a result.
**Minimum remediation:** Document the candidate-set change and require kind filtering; add a kind-filtered query only if existing consumers cannot tolerate it.
**Track A:** owner-facing compatibility qualification. **Track B:** no independent blocker.
**Owner decision:** accept additive exact-name candidates or require a filtered API.

### EC6C-AR1-F09 — ID-dictionary insertion can hide collisions

**Disposition:** CONFIRMED CONTRACT CONFLICT. **Severity:** P2.
**Design location:** architecture §5.3 and decision D-EC6C-03; roadmap 6C.2 failure cases.
**Existing contract:** EcmaScriptExtractionProfile.AddEdge inserts with _result.Edges.TryAdd; later validators cannot inspect a fact that was never inserted.
**Counterexample:** two proposed occurrence facts compute the same edge ID but differ in properties/span semantics.
**Expected:** same-ID/different-fact collision fails loudly as D0 promises.
**Proposed:** D0 relies on downstream collision handling, while current insertion can silently keep the first fact.
**Consequence:** a written occurrence can disappear according to traversal order.
**Minimum remediation:** On insertion, compare the existing edge’s full fact value; allow exact duplicate only if explicitly intended, otherwise throw on same ID with different fact. Add a reversed-enumeration collision test.
**Track A:** implementation gate. **Track B:** applies to later attribution edge emission too.
**Owner decision:** approve the fail-loud invariant and apply it before dictionary insertion.




## M. Decision reconciliation

A compact owner-ready table is also provided in ec6c-glm-ar1-decision-reconciliation.md. “Runtime evidence” distinguishes inspected p8 behavior from design-only proposals.

| Decision | D0 proposal and independent disposition | Runtime evidence / counterexample | Owner action and future impact |
| --- | --- | --- | --- |
| D-EC6C-01 Representation | Recommend export-entry nodes and separate file-to-entry occurrences; accept with qualifications. | Declaration-only edges lack endpoints for expressions, unresolved names, imports and re-exports; graph nodes and edges have distinct roles. | Accept only if positive facts and coverage semantics are explicit. A; enables later binding endpoint. |
| D-EC6C-02 Name identity | One node per module path plus exported name; accept with namespace refinement. | File path scopes other symbols; names are exact and ordinal; Item can have type and value facets. | Accept entry as name slot, not binding. A; EC-6D namespace matching. |
| D-EC6C-03 Occurrence identity | One edge per syntax occurrence; accept with corrections. | Properties do not key edge ID; written spans are UTF-16; current TryAdd may swallow collisions; re-export source name is absent. | Add source-side remote name to properties and identity; guard collisions. A implementation gate. |
| D-EC6C-04 Attribution eligibility | Direct span or module-scope lookup; reject as written. | Records are physical, nodes may merge; no scope table. Same-file merged interfaces contradict “occurrence-specific”. | Limit Resolved to unique target records; build conservative binding table; defer converged stage. B stages 6C.3/6C.4. |
| D-EC6C-05 Evidence/provider | Structural extraction provider plus versioned export resolver for attribution; provider split is sound, attribution semantics need revision. | Global evidence meanings are fixed; Derived never becomes Resolved; same-file merging is a counterexample. | Accept provider separation; reword Resolved/Derived claim and preserve D-05 gate. B; affects EC-6D proof. |
| D-EC6C-06 Namespaces | Metadata, not identity; accept only as explicit projections. | Interface Item plus const Item gives aggregate both, not one binding. | Define projection and future match rule. B/EC-6D gate. |
| D-EC6C-07 Defaults | Always emit default entry, sometimes no attribution; plausible representation, owner decision required. | Anonymous/default expressions have no declaration; EC-6A D-11 deferred no edge/binding. | Explicitly approve name-only default surface or omit it in first slice. A owner decision. |
| D-EC6C-08 Ownership | Per-file unit; accept in principle. | Existing units atomically replace and validate same-unit endpoints; export lifecycle is untested. | Keep local export facts in file unit; add both-store lifecycle evidence. A qualification. |
| D-EC6C-09 Remote/import surface | Emit structural entries for named/namespace re-exports and imported local bindings; owner decision, not accepted. | D-06 prior boundary deferred; remote source name currently lost. | Add source-name discriminator and accept entries, or suppress until EC-6D with queryable coverage. A owner gate; remote binding remains EC-6D. |
| D-EC6C-10 Queries | Existing traversal and exact-name APIs; qualified acceptance only. | Traversals can expose facts; properties are client-side; exact name returns all kinds; coverage is absent. | State kind filtering/truncation and add coverage query contract. A qualification. |
| D-EC6C-11 Versioning | p9 both plugins, resolver revision unchanged, no migration; accept as plan. | Planner compares profile-bearing plugin version and source hash; no p9 implementation exists. | Preserve identity expectations and prove authentic p8-to-p9 durable migration. A gate. |
| D-EC6C-12 Coverage | New per-kind counters and optional deferred counter; reject as sufficient query contract. | Run-level coverage is not persisted/queryable and lifecycle diagnostics drop it. | Define counter arithmetic and publish per-file completeness/reasons; settle deferred policy. A blocker for completeness; B outcome reporting. |
| D-EC6C-13 Absence | Surface remains without attribution; absence means “not established”; accept with published status. | Positive graph facts can omit endpoints, but no coverage is queryable. | Preserve this language and expose omitted/deferred reasons. A/B. |
| D-EC6C-14 EC-6D boundary | Entry as future import-binding endpoint; accept with namespace and no-promotion constraints. | Shared entry can be a union slot; Derived is not verified binding. | Keep EC-6D separate; match facets, design closure/dependency ownership, and do not promote Derived. Future gate. |

## N. Separate readiness verdicts

### Track A — Structural export-surface representation

**READY WITH QUALIFICATIONS.**

The graph model can truthfully record positive, readable export-name occurrences without declaration attribution. Keep the module+name entry ID, define it as a namespace-faceted name slot, and use Structural file-to-entry occurrence edges. The first accepted slice must either publish queryable per-file complete/partial/deferred status or explicitly promise positive-only observations and say that missing entries convey no absence. Do not include named re-exports until source-side names and D-09 ownership are settled. Require a collision guard and kind-aware query guidance. Existing per-file publication and p9 invalidation are plausible but still need implementation evidence.

### Track B — Local declaration attribution

**DESIGN REMEDIATION REQUIRED.**

D0’s unique-local-binding rule is not supported by the existing extractor, and Resolved eligibility fails when multiple physical declarations share one path-scoped node. Name-converged Derived edges only become occurrence associations through a required location-to-declaration-record join; they are not safe binding evidence. Re-specify stage 3 around a conservative syntax-level module-scope table and unique physical declaration records. Defer stage 4 until the owner accepts the narrower association contract, or remove it.

### Overall review

**READY WITH QUALIFICATIONS.**

The central export-entry model is viable, and there is a defensible Track A slice. D0 as a whole should not be treated as approved for implementation: revise the coverage/absence contract, namespace meaning, re-export metadata, query compatibility, collision enforcement, and owner-decision history. Track B needs a separate design gate. These are independent review verdicts; no verdict authorizes implementation.

## O. Owner decisions and smallest safe milestone

Before accepting Track A, the owner should explicitly decide:

1. Whether the export node is a module+name slot with independent type/value projections and what “both” means.
2. Whether surface completeness is queryable per file; if not, whether EC-6C is explicitly positive-only.
3. Whether anonymous/default expressions are represented as name-only default entries despite EC-6A D-11’s deferred status.
4. Whether named re-export and imported-local surface entries are included; if yes, preserve both public and source-side name plus specifier and classify them Structural only.
5. Whether exact-name lookup may return export nodes alongside declarations and what callers do with the expanded candidate set.
6. Whether identity collisions fail before ID-dictionary insertion.
7. Whether D-05 attribution, D-06 imported-binding and D-11 anonymous-default history is corrected and new scope exceptions are recorded as fresh owner decisions.
8. Whether stage 4 name-converged association is omitted or accepted only under the strict location-join rule.

Smallest defensible milestone: revised EC-6C.1 plus EC-6C.2 for a deliberately bounded set of directly observed, locally represented ESM names; entries and Structural occurrences only; no exported-as edges; queryable per-file coverage or an explicit positive-only contract; type/value facets defined; p9 on both plugins; atomic per-file replacement; collision checks; named re-exports/imported-local exports excluded unless D-09 and its metadata are corrected. After owner acceptance, qualify those facts on both stores and against authentic p8-to-p9 migration. Keep stages 3 and 4 as separately reviewed work. This review itself did not run tests, probes, p9 implementation or migration.
