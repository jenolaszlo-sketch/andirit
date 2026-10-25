# EC6C-D0: Export representation adversarial cases

## Current EC6C-A2 structural acceptance matrix

**Status:** This matrix is the normative Track A syntax/test contract, including the closed A29 scalar boundary. It supersedes conflicting expectations in historical C01–C35; those cases remain as design history and are not evidence that a p9 implementation exists. A29 tests are specified here but were not executed in EC6C-A2.

The shared contract in the architecture addendum defines E(path,name) for entry identity and O(file,entry,exportedName,form,localName-presence/localName,typeOnly,projection,UTF-16 span) for occurrence-edge identity. Every emitted relationship is kind exports with Structural evidence, provider penghou.hetu.tree-sitter.ecmascript-export-surface, provider version packageVersion+exports1.local-v1, and Evidence.Location at the token span of the public exported name. It is owned and replaced in the file unit. No row emits exported-as. For every omitted syntax form, positive-only semantics apply.

### Syntax allowlist

| Category | A1 disposition | Expected fact and projection | Fixture and gate |
| --- | --- | --- | --- |
| Direct variable declaration | Supported for simple identifier declarators. | Entry per public name; direct named occurrence; value projection. | A01 and multi-declarator variant; exact name span. |
| Direct class declaration | Supported. | Entry and direct occurrence; both projection. | A01b; one class source occurrence. |
| Direct function declaration | Supported, including each direct overload signature that carries an unambiguous exported name. | One entry; one Structural occurrence per written declaration site; value projection. No overload grouping. | A01c and duplicate-site case; no declaration records joined. |
| Interface and type alias | Supported as direct declarations. | Type projection per occurrence. | A05/A07; no binding claim. |
| Enum | Supported as a direct TypeScript declaration. | Both projection per occurrence; no merge resolution. | A01d; no compiler analysis. |
| Namespace/module declaration | Direct non-ambient syntax may produce a name occurrence; projection is unknown. | Name entry and Structural occurrence only. | A01e; nested namespace exports excluded. |
| Local named export list | Supported when exported-name token is exact. | Entry and local-list occurrence; explicit local-name; projection unknown unless explicit type-only. | A02/A03/A06/A10. |
| Local alias | Supported. | Entry is keyed by public alias; local-name is written local spelling; no binding. | A02/A03/A14. |
| Multiple declarators | Supported only for simple identifier declarators with exact name nodes. | One occurrence per public identifier token. | A01 multi-name fixture; destructuring is excluded. |
| Named and anonymous default declarations | Supported when parser provides exact default token. | Entry name default; function value, class both; optional declared name remains syntax metadata only. | A08 and named default variant. |
| Default expression | Supported as a default value occurrence. | Entry default; expression projection value; no declaration identity. | A09. |
| Explicit type-only export | Supported when grammar exposes the modifier. | type-only=true and projection type on that occurrence. | A05/A06. |
| Identifier named type | Supported as an identifier; not a modifier. | No type-only flag; local-list projection unknown absent an explicit modifier. | A06; grammar-shape regression. |
| Simple quoted and Unicode name | Supported when literal needs no escapes and meets the approved bound. | Canonical public string value is entry name; exact source token is occurrence span. | A19 and string-name fixture. |
| Repeated, overloaded, or merged direct declaration sites | Supported only as separate syntactic observations when each site independently has an exact export name. | One shared module/name entry; separate occurrence edges. No semantic merge/overload classification or attribution. | A04/A07; distinct spans and IDs. |
| Imported local alias exported by a local list | Supported Structural-only. | Same local-list observation as any export list; no remote origin, import binding or declaration edge. | A10; distinguish no from-clause from named re-export. |
| Named re-export | Excluded. | No export entry or exports edge for the re-export form. Existing module dependency fact may remain. | A11. |
| Star re-export | Excluded. | No name entry or exports edge. | A12. |
| Namespace re-export | Excluded. | No name entry or exports edge. | A12. |
| Ambient declaration | Unsupported in A1. | No entry or occurrence, even if a name is visible. | A17; absence is not negative evidence. |
| Destructuring export | Unsupported in A1. | No entry for names discovered only by pattern analysis. | A17. |
| CommonJS forms | Unsupported in A1. | No export facts for module.exports or exports.name. | A17. |
| TypeScript export equals | Unsupported in A1. | No export facts. Existing module-specifier observations remain governed by their own contract. | A17. |
| Nested namespace export | Unsupported as module-level export. | No module export entry for nested member. The outer exported namespace may be observed by its direct declaration. | A17. |
| Parser-recovered malformed syntax | Error/missing subtrees are unsupported; valid sibling nodes may still emit positive facts. | No fact for a recovered occurrence whose name/span cannot be established; no completeness claim. | A17. |
| Escaped, empty, control, malformed UTF-16, or over-bound name | Unsupported; never truncate, normalize, or replace. | No entry and no occurrence. | A29; escaped-name and malformed-surrogate validation fixtures. |

### A01–A29 implementation-ready cases

| ID | Fixture or operation | Expected entry and occurrences | Projection, provenance, publication and required assertion |
| --- | --- | --- | --- |
| A01 | export const count = 1; | E(path,count); one direct named O at the count token. | value; Structural; file unit. Assert ID, Kind export, Name/QN count, null SymbolId, edge provider/version/location/properties. |
| A02 | const value = 1; export { value as current }; | E(path,current); one local occurrence at current with local-name value. | unknown; Structural; no declaration link. Assert alias is the entry name and source spelling remains occurrence metadata. |
| A03 | export { value as first }; export { value as second }; | E(path,first), E(path,second); two occurrence IDs with local-name value. | unknown; each span Structural. Assert distinct entries, no edge collapse, no exported-as. |
| A04 | export { left as result }; export { right as result }; | One E(path,result); two occurrence edges at separate result tokens. | unknown for each; Structural. Assert duplicate slot has two edges, neither chosen as binding. |
| A05 | export type { Item as Shape }; | E(path,Shape); one local occurrence at Shape. | type-only=true; projection type; Structural. Assert explicit modifier differs from normal local export. |
| A06 | const type = 1; export { type }; export type { Item }; | Entries type and Item; one occurrence each. | type is not a modifier: first has no type-only and projection unknown; Item is type-only/type. Assert grammar distinguishes identifier from modifier. |
| A07 | export interface Item {}; export const Item = {}; | One E(path,Item); two direct occurrences at distinct spans. | interface contributes type; const contributes value. No aggregate on entry. Assert one slot, two projections, no claim either occurrence binds imports. |
| A08 | export default function () {}; | E(path,default); one occurrence at default token. | value; Structural; no local-name or declaration association. Assert no synthetic declaration. |
| A09 | export default makeValue(); | E(path,default); one occurrence at default token. | value; Structural; no declaration association. Assert expression does not create a declaration node. |
| A10 | import { remote as local } from "./other"; export { local as publicName }; | E(path,publicName); one local-list occurrence with local-name local. | unknown, or type for explicit type-only local list; Structural. No via-module or remote target on export edge. Assert no declaration binding and no re-export classification; existing import/resolves-to facts remain independent. |
| A11 | export { original as alias } from "./other"; | No export entry and no exports edge for this syntax. | The existing resolver may still emit resolves-to for the specifier. Assert public alias and remote source name are absent from Track A graph and no resolver identity changes. |
| A12 | export * from "./other"; export * as ns from "./other"; | No export entry or exports edge for either form. | Existing module dependency observations remain separate. Assert no claim of export completeness. |
| A13 | Remove the only supported export occurrence from an indexed file. | Completed replacement removes its old occurrence and now-unreferenced entry. | No export coverage marker is created; unrelated file facts remain. Assert exact unit replacement in both stores. |
| A14 | Rename export { value as before } to export { value as after }. | E(path,before) is removed if unreferenced; E(path,after) is added; occurrence ID changes. | Same local spelling; Structural. Assert old entry is absent and any declaration identity remains unchanged. |
| A15 | Remove one of two duplicate result occurrences. | E(path,result) remains; removed edge disappears; unchanged sibling edge ID remains. | Structural. Assert multiplicity is occurrence-based and exact surviving fact is stable. |
| A16 | Index a valid file with no supported exports. | No export entry or exports edge. | Meaning is “no supported occurrence recorded”; no complete-empty status. Assert old export facts clear on completed replacement. |
| A17 | Mix supported syntax with re-export, ambient, destructuring, nested export, or malformed subtrees. | Emit only error-free allowlisted occurrences with exact names/spans; emit none for unsupported forms. | Every emitted fact Structural. Assert valid siblings survive parse recovery where available and omitted forms imply no absence. |
| A18 | Generate the same complete occurrence fact twice; then inject same ID with a different property or evidence location. | Exact duplicate is idempotent; conflicting same-ID fact fails before insertion/completion. | No conflict is resolved by traversal order. Assert prior published file unit survives failure in both stores. |
| A19 | /*😀*/ export const 名 = 1; | E(path,名) hashes the exact name; O location begins after the supplementary scalar using UTF-16 columns. | value; Structural. Assert UTF-16, one-based, end-exclusive span; entry hash unaffected by preceding text. |
| A20 | Re-extract the same source once with LF and once with CRLF. | Entry and occurrence IDs remain equal when path and logical line/column source are otherwise identical. | Structural. Assert raw source-byte differences do not enter occurrence identity. |
| A21 | Extract identical path and bytes repeatedly and with reversed internal enumeration. | Same entry/edge IDs and complete facts. | Structural. Assert graph fact set deterministic and exact duplicates coalesce only as specified. |
| A22 | Fail after staging some exports or cancel before file-unit completion. | No partial new publication; prior complete unit remains visible. | Assert atomic retention in both InMemory and LatticeDb; this is a future qualification, not an A1 runtime result. |
| A23 | Close and reopen durable LatticeDb after successful p9 publication. | Same export nodes, edges, IDs, properties and evidence locations reappear. | Assert round-trip equality and valid endpoints; no schema migration. |
| A24 | Seed a durable publication with the accepted p8 extractor, then index unchanged sources using p9. | Both ECMAScript plugins invalidate; only supported export facts are added. | Existing node, declaration, structural-edge and unchanged resolves-to IDs remain byte-identical; resolver2.relative-v1 remains unchanged; reopen and compare. |
| A25 | Query an existing declared symbol named Item after an export entry Item is added. | Exact QualifiedName lookup may return both declaration and export entry, sorted by node ID. Module traversal filtered to exports retrieves entries/occurrences. | Existing candidate API already preserves declaration-less nodes and has no node-kind filter. Assert old candidate IDs remain, added Kind=export is explicit, SymbolId filtering keeps declaration navigation stable, Truncated is checked, FindReferences and presets stay unchanged. |
| A26 | Run every Track A syntax case. | No exported-as edge exists in any unit. | All export relationships are Structural; none is Resolved, Derived or Inferred. Assert provider identity/version and no declaration attribution. |
| A27 | Index from-clause re-export forms alongside ordinary imports. | No Track A export facts for re-export statements; resolver may publish its existing file-level resolves-to fact. | Assert resolver2.relative-v1 evidence, edge IDs and properties remain unchanged when inputs are unchanged. |
| A28 | Explain a nonempty, empty, omitted, unsupported or truncated result to a consumer. | Nonempty is a set of observed supported occurrences; empty is no supported occurrence recorded; omitted forms and additional effective exports remain unknown. | Update consumer guidance. Assert docs reject complete-surface, valid-binding and declaration-origin inference; truncated traversal is explicitly partial. |
| A29 | Apply the accepted strict 256 Unicode scalar-value rule to syntax and validation-level boundary fixtures. | Valid, allowlisted names of 1–256 scalars receive E(path,name); empty, malformed, and over-bound inputs receive no entry or edge. Never normalize, substitute, or truncate. | A29 is closed by EC6C-A2. UTF-16 remains the source-coordinate unit; see the normative fixture table below. |

### A29 — normative 256 Unicode scalar-value fixtures

A scalar is a code point outside U+D800–U+DFFF. Strict validation walks UTF-16, accepts a high surrogate only when immediately followed by a low surrogate, counts that pair once, and rejects either unpaired half. Combining marks count separately. .NET string.Length is not the scalar counter. The future extractor validates only after the existing syntax allowlist has established a canonical name; escaped-name syntax remains unsupported even if its decoded text would be short enough.

The fixture file below is a future test location and is not created by EC6C-A2. For syntax fixtures, use the existing JavaScript/TypeScript forms where their current grammars expose the accepted node; if a constructed string cannot occur in an allowlisted form, run it only at the name-validator boundary. Every emitted fact remains E(modulePath,name) plus one Structural occurrence per accepted written occurrence. A failed name produces no entry, no edge, and no identity.

| Fixture / input construction | Scalar count / UTF-16 units | Support classification | Expected export fact behavior | Expected identity behavior | Validation boundary | Future test location | Independent qualification requirement |
| --- | ---: | --- | --- | --- | --- | --- | --- |
| Empty candidate string "" (validator-level input) | 0 / 0 | Unsupported: empty | No entry or occurrence. | No identity is constructed. | Reject before hashing, node construction, serialization, or publication. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer checks rejection and absence through both store paths; no durable fact may appear. |
| export const A = 0; | 1 / 1 | Supported | One entry named and qualified-name A; one Structural occurrence. | Existing module/name formula hashes exact A; occurrence uses its accepted span. | Strict validation after syntax classification and before identity construction. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 independently verifies exact node/edge in InMemory and LatticeDb, including reopen. |
| Identifier made from U+0041 repeated 256 times | 256 / 256 | Supported if exposed as the existing direct-identifier form | One entry and one Structural occurrence for the exact 256-scalar name. | Exact 256-scalar name feeds existing UTF-8/SHA-256 formula; no truncation. | Strict scalar count is exactly 256; UTF-16 Length happens to match here but is not the general rule. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 independently verifies identity and exact stored name in both stores and LatticeDb reopen. |
| Identifier made from U+0041 repeated 257 times | 257 / 257 | Unsupported: exceeds bound | No entry or occurrence. | No identity; never hash a shortened prefix. | Reject at scalar-count boundary before graph fact creation. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer confirms absence in both store paths and no truncation. |
| Existing simple quoted-name form containing U+1F600 repeated 256 times, with no escapes | 256 / 512 | Supported by the name rule; emit only if the existing quoted-name syntax node is allowlisted | One exact-name entry and one Structural occurrence when syntax is allowlisted; otherwise validator-level only. | Hash exact 256-scalar sequence; surrogate pairs encode as their scalar UTF-8 bytes. | Strictly validate pairs and count each pair once; do not use UTF-16 length as the bound. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer verifies validator result and, when emitted, both-store exact round-trip including reopen. |
| Existing simple quoted-name form containing U+1F600 repeated 257 times, with no escapes | 257 / 514 | Unsupported: exceeds bound | No entry or occurrence, even if the syntax node is otherwise valid. | No identity and no truncation. | Reject after syntax classification and before hash or serialization. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer verifies absence in both store paths. |
| Simple quoted-name candidate with U+1F600 repeated 128 times followed by U+0041 repeated 128 times | 256 / 384 | Supported by the name rule; syntax eligibility remains governed by the existing quoted-name allowlist | One exact-name entry and one Structural occurrence when allowlisted; otherwise validator-level only. | Exact mixed sequence receives its own entry ID. | Count 128 valid pairs as 128 scalars, plus 128 BMP scalars. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer checks exact result and both-store persistence when emitted. |
| Simple quoted-name candidate with U+1F600 repeated 129 times followed by U+0041 repeated 128 times | 257 / 386 | Unsupported: exceeds bound | No entry or occurrence. | No identity and no truncation. | Reject at 257 scalars, despite fewer than 512 UTF-16 units. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer confirms no graph fact in either store. |
| Direct IdentifierName built from U+0065 followed by U+0301 | 2 / 2 | Supported when the current grammar exposes the combining identifier | One entry for the exact two-scalar name and one Structural occurrence. | Hash the exact decomposed sequence; combining mark is not folded into the base letter. | Count each valid scalar separately. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 independently verifies identity and round-trip in both stores. |
| Same module exports U+00E9 and U+0065 followed by U+0301 as two names | 1 vs. 2 / 1 vs. 2 | Both supported if the existing grammar exposes both IdentifierNames | Two entries and two occurrences; both are positive observations. | IDs remain distinct; no Unicode normalization or convergence. Exact-name queries distinguish both spellings. | Validate each exact sequence independently before hashing. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer checks distinct IDs and exact query results in both stores; LatticeDb reopen preserves both. |
| Candidate containing one unpaired high surrogate U+D800 | Invalid / 1 | Unsupported: malformed UTF-16 | No entry or occurrence. | No hash; do not let replacement fallback converge it with U+FFFD. | Validation-level fixture; strict decoder must report non-Done before encoding or JSON. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | Independent review checks rejection before both store APIs; no replacement character or persisted fact. |
| Candidate containing one unpaired low surrogate U+DC00 | Invalid / 1 | Unsupported: malformed UTF-16 | No entry or occurrence. | No hash and no replacement fallback. | Validation-level fixture; reject before encoding, JSON, or publication. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | Independent review checks rejection before both store APIs; no replacement character or persisted fact. |
| export const Name = 1; export const name = 2; | 4 each / 4 each | Both supported | Two entries and two Structural occurrences. | IDs and exact-name query results remain distinct by ordinal case-sensitive spelling. | Validate each exact spelling; no case folding. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 reviewer verifies both exact values and stored names in both stores. |
| Escaped alias source with backslash followed by u0041; decoded candidate would be A | Candidate 1 / 1; source escape contains additional code units | Unsupported by existing syntax allowlist | No entry or occurrence. | Do not decode-to-identity or hash A; no identity is formed. | Syntax classification rejects escaped names before canonical-name validation; no escape support is added. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | Independent review verifies the syntax exclusion in both plugin grammars and no export fact in either store. |
| Construct source as comment delimiter, U+1F600, comment close, then export { value as Alias }; | Exported name: 5 / 5 | Supported local-list occurrence | One entry for Alias, one Structural edge. Alias span is one-based UTF-16 columns 26–31 (end-exclusive); the preceding supplementary scalar occupies two source columns. | Entry ID depends on module/name; occurrence ID also uses the exact UTF-16 span. | Name count and source-coordinate count are separate; do not change the established UTF-16 coordinate conversion. | tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportNameValidationTests.cs (future) | EC-6C.3 independently asserts exact one-based span and IDs in both extraction/store paths; LatticeDb reopen preserves the evidence location. |

All rows require extraction tests for JavaScript and TypeScript where the existing allowlist permits the syntax. Validation-level rows do not authorize new syntax support. The independent EC-6C.3 review must inspect the complete fixtures, and durable qualification must verify accepted names, identities, query distinctions, and evidence survive LatticeDb close/reopen.
### Independent-review disposition

- F01 (coverage visibility): accepted positive-only scope; no completeness counters or marker.
- F02 (namespace-slot ambiguity): accepted correction; facets are per occurrence and unknown remains unknown.
- F03 (same-file merged path-scoped target): Track B only; no Track A attribution.
- F04 (name-converged Derived target): Track B only; no Track A attribution.
- F05 (module-scope lookup): deferred to Track B.
- F06 (missing remote source name): bounded by excluding all re-export entries.
- F07 (EC-6A decision history): corrected in the owner record and decision register.
- F08 (exact-name candidates): accepted additive candidate behavior under existing all-node lookup and consumer guidance; declaration navigation remains SymbolId based.
- F09 (TryAdd collision suppression): accepted fail-before-insert requirement for future export emission.

No A01–A29 implementation tests ran during EC6C-A1. The cases specify future tests and are not runtime qualification.

---

## Historical EC6C-D0 adversarial cases

C01–C35 below are retained as the original design review matrix. They are historical D0 predictions, not current Track A acceptance tests where they conflict with the A1 matrix above. Their original wording is preserved for traceability.

---

> Historical EC6C-D0 content follows. Preserve it for rationale and traceability; the EC6C-A1 contract above controls wherever the text conflicts.

**Status:** design-only counterexample matrix for the recommended export model
in [ec6c-export-representation-architecture.md](ec6c-export-representation-architecture.md).
It does not implement or execute the model. Cases marked **executable** are
planned deterministic fixtures for EC-6C; cases marked **design-only** are
reasoned publication/evidence cases that require the implementation to execute.

**Baseline:** `038295f` (owner-accepted EC-6B tree). All entry ids below use
`{lang}:export:{sha256(modulePath\nname)}`; occurrence/attribution ids follow
the architecture §5.3. Hash values are abbreviated (`…`) in this document.

## Summary

| Case | Scenario | Disposition | Executable? | Blocks EC-6C? |
| --- | --- | --- | --- | --- |
| C01 | Two aliases of one declaration | Supported | Yes | No |
| C02 | Same exported name from distinct occurrences | Supported with ambiguity | Yes | No |
| C03 | Name-scoped declaration convergence | Supported (Derived) | Yes | No |
| C04 | Direct export declaration | Supported (Resolved for path-scoped) | Yes | No |
| C05 | Named local export | Supported (Resolved/Derived by kind) | Yes | No |
| C06 | Unresolved local name | Supported (no attribution) | Yes | No |
| C07 | Anonymous default function | Supported (entry only) | Yes | No |
| C08 | Default expression | Supported (entry only) | Yes | No |
| C09 | Multi-declarator variable | Supported per declarator | Yes | No |
| C10 | `type` used as identifier | Supported | Yes | No |
| C11 | Genuine type-only export | Supported | Yes | No |
| C12 | Class in both namespaces | Supported | Yes | No |
| C13 | Function overloads | Supported conservatively | Yes | No |
| C14 | Declaration merging | Supported (multiple attributions) | Yes | No |
| C15 | Quoted/Unicode export names | Supported (no escapes) | Yes | No |
| C16 | Parser-recovered malformed syntax | Partial, no fabrication | Yes | No |
| C17 | Export alias removed | Supported (replacement) | Yes | No |
| C18 | Declaration deleted, export remains | Supported (attribution withdrawn) | Yes | No |
| C19 | Unchanged publication | Deterministic ids | Yes | No |
| C20 | Failure after staged export facts | Prior publication retained | Design-only | No |
| C21 | Re-export statement | Structural entry; binding deferred | Yes | Owner decision D-EC6C-09 |
| C22 | Cross-plugin source | Local surface only; no false remote binding | Yes | No |
| C23 | LF/CRLF and supplementary Unicode | Stable ids | Yes | No |
| C24 | Same-name declarations across modules | Distinct entries; Derived target | Yes | No |
| C25 | Empty export inventory | Stale facts removed | Yes | No |
| C26 | Type alias vs type-only list | Correct classification | Yes | No |
| C27 | Imported binding exported | Entry only; binding deferred | Yes | Owner decision D-EC6C-09 |
| C28 | Destructuring export | Deferred/unsupported | Yes | No |
| C29 | Ambient export | Unsupported/deferred | Yes | No |
| C30 | Function-valued variable (dual projection) | Field attributed; projection excluded | Yes | No |
| C31 | Escape/overlong/empty exported name | Unsupported, no text retained | Yes | No |
| C32 | Type + value same name across statements | One entry, space both | Yes | No |
| C33 | Type-only and value occurrences of one name | One entry, both spaces | Yes | No |
| C34 | CommonJS assignment | Unsupported; not ESM | Yes | No |
| C35 | Export inside a namespace | Not module surface; deferred | Yes | No |

## C01 — Two aliases of one declaration

**Fixture**
```ts
const value = 1;
export { value as first };
export { value as second };
```
**Expected interpretation.** One local declaration; two exported names; two
occurrences; two independent entries.
**Facts.** Entries `first` and `second` (`export-space=value`); two `exports`
edges (`local-name=value`, `export-via=local`); two `exported-as` edges from the
`field(value)` node to each entry, both `Resolved`,
`attribution-basis=module-scope-lookup`, `declaration-kind=variable`.
**Evidence.** Surface edges Structural; attributions Resolved (path-scoped
field, unique module-scope declaration).
**Identity.** Entry ids differ by name; occurrence edge ids differ by span;
attribution edge ids differ by target entry. The shared declaration does not
collapse the names.
**Query.** Outgoing `exports` from the file returns both entries; outgoing
`exported-as` from the field returns both entries; exact-name lookup for
`first`/`second` returns one entry each.
**Publication.** File unit replacement removes each entry/edge independently
when its occurrence is removed.
**Disposition.** Supported. **Test form:** executable extraction + publication
fixture. **Blocks EC-6C:** no.

## C02 — Same exported name from distinct occurrences

**Fixture**
```ts
const a = 1, b = 2;
export { a as same };
export { b as same };
```
**Expected interpretation.** One exported name `same` with two written
occurrences and two different local declarations. The surface is ambiguous by
construction (TypeScript would reject the duplicate export).
**Facts.** One entry `same`; two `exports` edges (same name/kind; distinct
spans; `local-name=a` and `local-name=b`); two `exported-as` edges from
`field(a)` and `field(b)`, each Resolved; the surface has two occurrence edges
(both emitted); `exported-as` coverage records the ambiguous name (two
declaration sources).
**Evidence.** Each attribution is individually verified for its declaration;
the entry does not claim a single origin.
**Identity.** One entry id; occurrences distinguished by span; attributions by
source+span.
**Query.** Incoming `exported-as` to the entry returns two declarations;
consumers detect ambiguity. No arbitrary first/last pick.
**Publication.** File unit replacement removes both occurrences/attributions
when the duplicate statements are removed.
**Disposition.** Supported with explicit ambiguity. **Test form:** executable.
**Blocks EC-6C:** no (decision D-EC6C-08 records duplicate policy).

## C03 — Name-scoped declaration identity convergence

**Fixture**
```ts
// a.ts
export function run() {}
// b.ts
export function run() {}
```
**Expected interpretation.** Two modules export the same name; their
declarations share a name-scoped `callable` node.
**Facts.** Two entries (`a.ts#run`, `b.ts#run`); two `exports` edges; each entry
has an `exported-as` edge from the shared callable with `Derived` evidence,
the declaring module in the evidence location path, and the declaration
occurrence span. No `Resolved` edge.
**Evidence.** Derived explicitly withholds a verified declaration-binding
claim. The edge carries the declaring module and occurrence, so the F01
convergence is not presented as a unique physical declaration.
**Identity.** Entry ids differ by module; occurrence edges by span; attribution
edges by target entry and declaration occurrence.
**Query.** A consumer asking "which declaration supplies b.ts#run?" gets the
shared callable plus the Derived edge located in b.ts; it must not treat the
node identity as proof. Filtering `Resolved` excludes it.
**Publication.** File units own their entries; reindexing one module does not
alter the other entry; the shared callable node remains a multi-origin node.
**Disposition.** Supported as Derived. **Test form:** executable (AC-01b
modernized). **Blocks EC-6C:** no, subject to owner acceptance of Derived
attribution (D-EC6C-05).

## C04 — Direct export declaration

**Fixture**
```ts
export const count = 1;
export class Project {}
export interface Model {}
export function calculate() {}
```
**Expected interpretation.** Each declaration is directly inside its export
statement; attribution is by exact declaration AST span, not by name.
**Facts.** Four entries (count/Project/Model/calculate), four `exports` edges
(`export-via=direct`), four `exported-as` edges:
variable Resolved, class Resolved, interface Resolved, function Derived.
Spaces: value, both, type, value.
**Evidence.** Direct-syntax for all; Resolved only where the target node is
path-scoped (variable/class/interface).
**Identity.** Entry ids by module+name; occurrence spans = declaration name
spans; attribution spans = declaration name spans.
**Query.** Outgoing `exports`; outgoing `exported-as` from each declaration
node; evidence filter separates verified from Derived.
**Publication.** Entries/attributions in the same file unit; atomic replacement.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C05 — Named local export

**Fixture**
```ts
const count = 1;
export { count };
```
**Expected interpretation.** The export references a module-scope binding; the
binding is established by the documented module-scope table, not by text match
alone.
**Facts.** Entry `count`; occurrence `local`; `exported-as` from the unique
`field(count)` declaration, Resolved, basis `module-scope-lookup`.
**Evidence.** Resolved: unique module-scope declaration, path-scoped node.
**Identity.** As architecture §5.3.
**Query.** Outgoing `exported-as` from the field returns the entry; incoming to
the entry returns the field.
**Publication.** Removing the declaration while keeping `export { count }`
withdraws the attribution (C18).
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C06 — Unresolved local name

**Fixture**
```ts
export { missing };
```
**Expected interpretation.** The name is visibly exported but no local
declaration or import supplies it in this file.
**Facts.** Entry `missing`; `exports` edge (`export-via=local`); no
`exported-as`; coverage `unresolvedTargets=1` on the attribution kind.
**Evidence.** No binding claim; the surface edge is Structural.
**Identity.** Entry/occurrence normal; no attribution edge.
**Query.** Entry is visible; incoming `exported-as` returns nothing.
**Publication.** The file unit publishes the entry; no fabricated declaration.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C07 — Anonymous default function

**Fixture**
```ts
export default function () {}
```
**Expected interpretation.** The module exports the name `default`; no local
declaration name exists.
**Facts.** Entry `default`, `export-space=value`; `exports` edge
(`export-kind=default`, `export-via=expression`); no `exported-as`; no
declaration node is fabricated.
**Evidence.** Structural surface only.
**Identity.** Entry hash of `default`; occurrence span = `default` keyword.
**Query.** Outgoing `exports` returns the entry; no declaration association.
**Publication.** File unit replacement; no placeholder declaration.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C08 — Default expression

**Fixture**
```ts
export default 42;
const current = 1;
export default current; // separate fixture, one default per file
```
**Expected interpretation.** A default expression exposes `default`; no
declaration is fabricated. A bare identifier may be attributed only through the
unique module-scope lookup and only when the declaration has value space.
**Facts.** Entry `default`; occurrence `expression` (literal) with no
attribution; for `current`, occurrence `local`, `local-name=current`, and
`exported-as` to the value field if unique.
**Evidence.** Structural for the expression; Resolved/Derived per the local
declaration for the identifier.
**Identity.** As architecture §5.3; the two fixtures are separate files.
**Query.** Expression: entry only. Identifier: attribution traversal.
**Publication.** No partial facts on failure; replacement on edit.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C09 — Multi-declarator variable

**Fixture**
```ts
export const first = 1, second = 2;
```
**Expected interpretation.** Two independently declared names are exported by
one statement; attribution is per declarator.
**Facts.** Entries `first`/`second`; two `exports` edges at each declarator
name; two `exported-as` edges to the respective `field` nodes, Resolved.
**Evidence.** Direct-syntax per declarator.
**Identity.** Per-declarator spans and fields; no whole-statement lumping.
**Query.** Outgoing `exports` returns both; declarations are distinct.
**Publication.** Each entry/edge replaced with the file unit.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C10 — `type` used as identifier

**Fixture**
```ts
const type = 1;
export { type };
export { type as kind };
```
**Expected interpretation.** `type` is a binding name, not a type-only
modifier; the EC-6B-R1 grammar-aware correction must be reused.
**Facts.** Entries `type` and `kind`; occurrences `local`; no `type-only`
property; `exported-as` to the `field(type)`, Resolved.
**Evidence.** Structural + Resolved; no namespace type claim.
**Identity.** `kind` is a different entry from `type`; `local-name=type` on the
`kind` occurrence.
**Query.** Entry `kind` is a normal alias; exact-name lookup `type` returns the
value entry.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable (regression against
EC-6B F03). **Blocks EC-6C:** no.

## C11 — Genuine type-only export

**Fixture**
```ts
interface Model { id: string; }
export type { Model };
export { type Model as Other };
```
**Expected interpretation.** Explicit type-only syntax exports only type space.
**Facts.** Entries `Model` (`export-space=type`) and `Other`
(`export-space=type`); occurrences `type-only: true`; `exported-as` to the
interface, Resolved; `declaration-kind=interface`.
**Evidence.** Structural type-only flag; Resolved attribution (path-scoped
interface).
**Identity.** `Other` entry distinct by name; type-only flag in the occurrence
discriminator.
**Query.** Outgoing `exports` with `type-only`; `export-space=type`;
`exported-as` Resolved.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C12 — Class in type and value namespaces

**Fixture**
```ts
export class Project {}
```
**Expected interpretation.** A class is both a value and a type; one exported
name with space both.
**Facts.** Entry `Project`, `export-space=both`; one occurrence `direct`;
`exported-as` to the path-scoped class, Resolved, `declaration-kind=class`.
**Evidence.** Resolved: class node is file-scoped and unique per name in the
file; direct syntax.
**Identity.** One entry; no split into two nodes.
**Query.** Consumers see `export-space=both`; a type-only or value consumer can
use the same entry; EC-6D matches with the declaration kind.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C13 — Function overloads

**Fixture**
```ts
export function parse(value: string): string;
export function parse(value: number): number;
export function parse(value: unknown): unknown { return value; }
```
**Expected interpretation.** One exported name; three written export
occurrences; the graph currently emits only the implementation declaration.
**Facts.** One entry `parse` (`export-space=value`); three `exports` edges
(`direct`); if the implementation is exported and the callable node exists, one
`exported-as` edge from the callable with `Derived` evidence and the
implementation declaration span; signature occurrences have no separate
declaration node and therefore no separate attribution edge.
**Evidence.** Derived (name-converged callable). Signature declarations are
unmodeled (documented); no fabricated signature nodes.
**Identity.** Entry name-level; occurrences by span; one declaration-level
attribution edge.
**Query.** Surface shows three written occurrences; declaration association
points at the implementation. Consumers must not assume one attribution per
occurrence.
**Publication.** Normal; removing the implementation withdraws attribution
while signatures remain surface occurrences.
**Disposition.** Supported conservatively. **Test form:** executable.
**Blocks EC-6C:** no (overload declaration identity is deferred, not
fabricated).

## C14 — Declaration merging

**Fixture**
```ts
export class Service {}
export namespace Service { export const value = 1; }
export { Service }; // duplicate name, ambiguous local lookup
```
**Expected interpretation.** The exported name has two independently written
declarations; the local list cannot pick one.
**Facts.** One entry `Service`, `export-space=both`; two direct `exports` edges;
two `exported-as` edges (class Resolved, namespace Derived); the local-list
occurrence adds a third `exports` edge but no attribution (ambiguous lookup:
two eligible declarations). Coverage records the ambiguous local lookup.
**Evidence.** Each direct attribution is evidence-scoped; no one-to-one claim.
**Identity.** Entry-level; occurrences by span; attributions by source+span.
**Query.** Incoming `exported-as` returns both declarations; the ambiguous
local list is visible as an occurrence without attribution.
**Publication.** Normal; removing one declaration removes its attribution edge
while the other remains.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C15 — Quoted or Unicode export name

**Fixture**
```ts
const value = 1;
export { value as "café-label" };
const café = 2;
export { café };
```
**Expected interpretation.** Quoted names are the string content when the
literal has no escape sequence and is non-empty/bounded; Unicode identifiers
are preserved ordinally.
**Facts.** Entries `café-label` and `café`; occurrences with the written spans;
local names `value`/`café`; attribution to the fields; `export-space=value`.
**Evidence.** Structural surface; Resolved attribution (path-scoped fields).
**Identity.** Entry ids hash the name text; UTF-16 columns locate spans; the
quoted name is not truncated or normalized.
**Query.** Exact-name lookup by the literal name; the quoted name is a node
Name (not an edge property).
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C16 — Parser-recovered malformed syntax

**Fixture**
```ts
export { good, , bad as };
export const value = 1;
```
**Expected interpretation.** Error/missing subtrees are skipped; the file is
partial; valid sibling exports remain.
**Facts.** Entry/occurrence for `value`; no fabricated entry for malformed
specifiers; partial coverage state; warning `<language>.parse-error`.
**Evidence.** Structural for valid facts; no attribution for error nodes.
**Identity.** No facts from error nodes; valid ids normal.
**Query.** Only valid entries are queryable.
**Publication.** Failed/partial extraction follows existing behavior: valid
facts are staged with partial coverage; a fatal failure retains the previous
publication.
**Disposition.** Supported (partial). **Test form:** executable.
**Blocks EC-6C:** no.

## C17 — Export alias removed

**Fixture**
```ts
const value = 1;
export { value as first };
export { value as second };
```
Then remove the second export.
**Expected interpretation.** The `second` entry and its occurrence/attribution
edges disappear; `first` remains with stable ids.
**Facts.** After reindex: entry `first` with its edges; no `second` entry;
coverage recomputed.
**Evidence.** File unit replacement.
**Identity.** `first` entry id stable; `second` entry id gone; occurrence ids
for `first` unchanged.
**Query.** Outgoing `exports` returns only `first`.
**Publication.** Atomic completion; no stale `second`; no tombstones.
**Disposition.** Supported. **Test form:** executable (both stores).
**Blocks EC-6C:** no.

## C18 — Declaration deleted but export statement remains

**Fixture**
```ts
const value = 1;
export { value };
```
Then delete the declaration.
**Expected interpretation.** The exported name is still written; the local
declaration association must be withdrawn.
**Facts.** Entry `value` remains (`export-via=local`); no `exported-as`;
coverage unresolved; stale attribution edge removed with the file unit.
**Evidence.** No fabricated declaration.
**Identity.** Entry/occurrence stable; attribution edge gone.
**Query.** Entry visible; no declaration association.
**Publication.** File unit replacement; failed run retains the prior
publication.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C19 — Unchanged publication

**Fixture** Any supported export fixture, indexed twice unchanged.
**Expected interpretation.** Deterministic identities and no unnecessary new
facts.
**Facts.** Same entries/occurrences/attributions with byte-identical ids,
properties and evidence; no-op reindex returns the previous publication
(`PluginsExecuted = 0`).
**Evidence.** Deterministic provider.
**Identity.** Stable under byte-identical re-extraction.
**Query.** Same results.
**Publication.** No new publication.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C20 — Failure after staged export facts

**Fixture** A fixture with a valid export; a store/test seam injects a failure
after the file unit (including export facts) is staged but before completion.
**Expected interpretation.** No partial export facts become visible.
**Facts.** Run `Failed`; previous publication retained; staged export
nodes/edges discarded.
**Evidence.** Existing `failed-run-discards-staged-graph` behavior; the
architecture places export facts in the file unit, so the existing guarantee
applies.
**Identity.** Previous ids retained.
**Query.** Previous graph.
**Publication.** Atomic; no mixed facts.
**Disposition.** Supported by the accepted publication model. **Test form:**
design-only until the EC-6C implementation adds the failure seam; then
executable on both stores. **Blocks EC-6C:** no.

## C21 — Re-export statement

**Fixture**
```ts
export { value } from './other';
export { value as alias } from './other';
export * as namespace from './other';
export * from './other';
```
**Expected interpretation.** The written exported names are surface facts; the
remote binding is deferred to EC-6D. `export *` has no written names.
**Facts.** Entries `value`, `alias`, `namespace`; occurrence edges
`export-via=re-export`/`namespace-re-export` with `via-module` raw specifier;
no `exported-as`; existing `resolves-to` edges unchanged; `export *` produces
no entry; coverage deferred. If the owner chooses strict suppression
(D-EC6C-09 alternative), no entries are emitted and the deferral is coverage-
only.
**Evidence.** Structural only; no remote declaration claim.
**Identity.** Entry ids by written name; occurrence spans at the specifier
names; `via-module` is a written fact and participates in the occurrence
edge discriminator; the entry identity does not.
**Query.** Surface names visible; no declaration association; the module
target is available via the existing `resolves-to` edge (same source file,
statement span).
**Publication.** File unit; no cross-file dependency in EC-6C.
**Disposition.** Structural surface only, binding deferred. **Test form:**
executable. **Blocks EC-6C:** owner decision D-EC6C-09; not a correctness
blocker.

## C22 — Cross-plugin source

**Fixture**
```ts
// page.ts
import { helper } from './helper.js';
export { helper };
// helper.js (JavaScript plugin)
export const helper = 1;
```
**Expected interpretation.** The TypeScript plugin's export surface contains
the written name `helper` (as an imported binding export). It must not claim a
declaration or resolve the JavaScript-owned declaration. The JavaScript
plugin's own exports are separate.
**Facts.** TS entry `helper` with `export-via=local-import`; no `exported-as`;
the EC-6B TS resolver has no `resolves-to` to the JS file (per-plugin
inventory); JS plugin exports its own `helper` entry/attribution in its own
file unit.
**Evidence.** Structural; no cross-plugin binding.
**Identity.** Entries are plugin-language-prefixed; no collision.
**Query.** Consumers see both plugins' surfaces without a false cross-plugin
association.
**Publication.** Plugin-local file units; no cross-plugin resolution.
**Disposition.** Supported as documented boundary. **Test form:** executable.
**Blocks EC-6C:** no.

## C23 — LF/CRLF and supplementary Unicode

**Fixture** An export line preceded by BMP and supplementary characters, in LF
and CRLF copies; quoted Unicode export names.
**Expected interpretation.** Same logical source yields identical entry ids and
occurrence/attribution ids; UTF-16 code-unit columns; `\r` does not shift
columns because it belongs to the preceding row.
**Facts.** Equal ids/properties/locations for LF vs CRLF; supplement columns
count UTF-16 units, not UTF-8 bytes; names preserve Unicode.
**Evidence.** Deterministic.
**Identity.** Stable under line-ending change; occurrence-anchored otherwise.
**Query.** Same.
**Publication.** Same.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C24 — Same-name declarations across modules

**Fixture** Two modules each `export class Project {}` or
`export function run() {}`.
**Expected interpretation.** Module ownership is preserved; shared name-scoped
nodes do not merge entries.
**Facts.** Distinct entries per module; class attributions Resolved to
distinct path-scoped nodes; function attributions Derived to the shared
callable with per-module occurrences.
**Evidence.** Per architecture §7.
**Identity.** Module path in entry ids.
**Query.** Exact-name lookup returns both entries; incoming `exports` identifies
the module.
**Publication.** Each file unit owns its entry.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C25 — Empty export inventory

**Fixture** A file previously exporting names is edited to have no exports
(the file remains).
**Expected interpretation.** The previous entries/occurrences/attributions are
removed; the file unit still publishes declarations.
**Facts.** No export nodes/edges; coverage recomputed; prior facts gone.
**Evidence.** File unit replacement.
**Identity.** Removed ids are not retained.
**Query.** No exports returned.
**Publication.** Atomic completion; no stale facts; if all files of the plugin
are deleted, the existing ownership/lifecycle behavior applies.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C26 — Type alias vs type-only export list

**Fixture**
```ts
export type Alias = string;
export type { Model };
```
**Expected interpretation.** `export type Alias = string` is a direct type
alias declaration; `export type { Model }` is a type-only export list. Neither
may be confused with the other; only the latter carries the written type-only
occurrence flag.
**Facts.** Entry `Alias` (`export-via=direct`, no `type-only`, space type);
entry `Model` (occurrence `type-only: true`, space type); the AST separates
`declaration` from the anonymous statement-level `type` + export clause (probe
§16).
**Evidence.** Direct type alias attribution Derived (name-converged); type-only
list attribution per eligibility.
**Identity.** Different entry names/forms; occurrence discriminators include
kind/via/type-only.
**Query.** Consumers see `Alias` as a direct type export and `Model` as a
type-only list export.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C27 — Imported binding exported

**Fixture**
```ts
import { value } from './other';
export { value };
```
**Expected interpretation.** The name is written as an export but its origin is
an import; EC-6C must not claim a local declaration. This reconciles D-06
(no `exports` edge) with the new surface model.
**Facts.** Entry `value` (`export-via=local-import`, `local-name=value`); no
`exported-as`; coverage deferred. If the owner suppresses such entries
(D-EC6C-09 alternative), no entry and coverage deferred.
**Evidence.** Structural surface only; no local or remote binding.
**Identity.** Entry name-level; occurrence normal.
**Query.** Entry visible; no declaration association; EC-6D may bind.
**Publication.** File unit; no cross-file binding.
**Disposition.** Structural surface only, deferred binding. **Test form:**
executable. **Blocks EC-6C:** owner decision D-EC6C-09.

## C28 — Destructuring export

**Fixture**
```ts
export const { a, b } = source;
export const [c, d] = list;
```
**Expected interpretation.** The exported names are real, but the existing
extractor does not model destructured bindings. EC-6C must not fabricate
declarations.
**Facts.** No entries in the recommended first implementation; coverage
deferred/unsupported; no `exports`/`exported-as` facts. A future slice may
capture the pattern names as surface-only entries; that requires a separate
decision.
**Evidence.** None claimed.
**Identity.** n/a.
**Query.** No export facts.
**Publication.** No facts; documented limitation.
**Disposition.** Deferred/unsupported. **Test form:** executable (negative
assertion). **Blocks EC-6C:** no (documented limitation).

## C29 — Ambient export

**Fixture**
```ts
export declare const ambient: string;
export declare function ambientFn(): void;
```
**Expected interpretation.** Ambient declarations are not modeled by the
extractor (EC-5 limitation); the export surface must not fabricate a
declaration or a resolved association.
**Facts.** No entries in the recommended first implementation; coverage
unsupported/deferred; no `exported-as`. A future slice could emit surface-only
entries after the ambient declaration model is designed.
**Evidence.** None claimed.
**Identity.** n/a.
**Query.** No facts; documented.
**Publication.** No facts.
**Disposition.** Deferred/unsupported. **Test form:** executable (negative
assertion). **Blocks EC-6C:** no.

## C30 — Function-valued variable (dual projection)

**Fixture**
```ts
export const handler = () => {};
const local = () => {};
export { local };
```
**Expected interpretation.** The written declaration is the variable; the
callable projection is not a second declaration of the exported name.
**Facts.** Entry `handler` attributed to the path-scoped `field(handler)`,
Resolved, `declaration-kind=variable`; the callable projection is excluded from
eligibility. For `local`, the module-scope table has the field and the
projection; the projection is marked non-eligible, so the unique field is
attributed Resolved.
**Evidence.** Resolved (field path-scoped, unique eligible declaration).
**Identity.** Field node id; projection not targeted.
**Query.** Outgoing `exported-as` returns the field, not the callable
projection; the callable remains queryable as a callable.
**Publication.** Normal.
**Disposition.** Supported; refines D-05 for the dual-projection case.
**Test form:** executable. **Blocks EC-6C:** owner acceptance of the D-05
refinement (D-EC6C-05).

## C31 — Escape/overlong/empty exported name

**Fixture**
```ts
const v = 1;
export { v as "a\u0062" };   // escaped quoted name
export { v as ">256 chars…" };
export { v as "" };
```
**Expected interpretation.** Escapes are never decoded; overlong/empty names
are unsupported (the graph requires a non-empty bounded Name and no truncated
text).
**Facts.** No entry for the escaped/overlong/empty occurrences; coverage
unsupported; no text retained beyond the bound; other valid exports unaffected.
**Evidence.** Structural refusal.
**Identity.** No ids for unsupported names.
**Query.** No entry.
**Publication.** No fabricated fact.
**Disposition.** Unsupported. **Test form:** executable. **Blocks EC-6C:** no.

## C32 — Type + value same name across statements

**Fixture**
```ts
export interface Model { id: string; }
export const Model = 1;
```
**Expected interpretation.** One exported name in both spaces; two directly
declared declarations; no collapse into one declaration.
**Facts.** One entry `Model`, `export-space=both`; two `exports` occurrences;
two `exported-as` edges (interface Resolved, variable Resolved);
`declaration-kind` distinguishes them.
**Evidence.** Each attribution individually Resolved (path-scoped).
**Identity.** One entry; occurrences by spans; attributions by sources.
**Query.** Incoming `exported-as` returns two declarations; space both.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C33 — Type-only and value occurrences of one name

**Fixture**
```ts
class Project {}
export type { Project };
export { Project };
```
**Expected interpretation.** The same name is exported once as type-only and
once as a value; the surface has both spaces.
**Facts.** One entry `Project`, `export-space=both`; two occurrence edges, one
with `type-only: true`; two `exported-as` edges from the class (both Resolved;
the occurrence flag is on the surface edge).
**Evidence.** Both attributions Resolved (class path-scoped); the type-only
flag scopes the occurrence, not the declaration.
**Identity.** Occurrence discriminators include the type-only token.
**Query.** Consumers see the type-only occurrence and the value occurrence;
entry space both.
**Publication.** Normal.
**Disposition.** Supported. **Test form:** executable. **Blocks EC-6C:** no.

## C34 — CommonJS assignment

**Fixture**
```js
const value = 1;
module.exports = value;
exports.name = value;
```
**Expected interpretation.** These are not ESM named exports; EC-6C must not
silently treat them as such.
**Facts.** No `export` entries; existing `imports`/`resolves-to` facts (if any)
unchanged; coverage unsupported/not-EC-6C; consumer docs state the boundary.
**Evidence.** None claimed for ESM exports.
**Identity.** n/a.
**Query.** No ESM export entries.
**Publication.** No facts.
**Disposition.** Unsupported. **Test form:** executable (negative assertion).
**Blocks EC-6C:** no (D-10 boundary).

## C35 — Export inside a namespace

**Fixture**
```ts
export namespace N {
  export const value = 1;
}
```
**Expected interpretation.** The inner export is a namespace member, not a
module-level export; EC-6C captures only the module surface. The module exports
`N` directly.
**Facts.** Entry `N` (direct, namespace space both, Derived attribution to the
namespace); no module entry for `value`; the inner `export` is deferred and not
captured. Consumers can inspect `N`'s members only through existing declaration
facts, not as module exports.
**Evidence.** Structural for `N`; Derived for the namespace (name-converged).
**Identity.** Entry `N`; no `value` entry.
**Query.** Module exports returns `N`; `value` is not a module export.
**Publication.** Normal.
**Disposition.** Supported with documented nested-export limitation.
**Test form:** executable. **Blocks EC-6C:** no.

## Additional observations

- Case C02's duplicate-name policy and C14's multiple attributions require the
  coverage mapping in decision D-EC6C-08; no arbitrary tie-breaking is allowed.
- Cases C21/C27 are the two owner-visible representation choices (structural
  entries for deferred bindings versus suppression). The correctness of the rest
  of the design does not depend on the choice.
- Cases C28/C29 are explicit coverage limitations, not false claims. A consumer
  must not read their absence as "no export".
- Cases C13/C14 show why `Derived` evidence and multiple attribution edges are
  necessary; collapsing them into one `Resolved` edge would violate the F01
  and merging constraints.
