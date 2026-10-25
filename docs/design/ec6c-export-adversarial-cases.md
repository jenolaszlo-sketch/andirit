# EC6C-D0: Export representation adversarial cases

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
