# EC6C-D0: Export representation architecture

**Status:** design artifact for EC6C-D0. It proposes an export representation for
EC-6C; it does not authorize or contain production code. The previously
proposed `exports` edge model was **not** accepted for EC-6C, and this design
re-evaluates it against the implemented graph. Owner decisions required before
implementation are listed in the decision register.
**Baseline:** owner-accepted EC-6B tree
`038295f26ce76a8747e923f9e0ef971e773c166e` (tree
`8779e00d57bd0b40a3ff89d16ca771e79cc950bf`), from canonical `main`
`558f8ac82ac1b5e07da68f4a71a70240eaf38409`.
**Date:** 25 October 2026.
**Design disposition:** READY FOR INDEPENDENT DESIGN REVIEW.
**Related documents:**
[adversarial cases](ec6c-export-adversarial-cases.md),
[implementation roadmap](ec6c-export-implementation-roadmap.md),
[decision register](ec6c-export-decision-register.md),
[EC-6A architecture](ec6a-cross-file-resolution-architecture.md),
[EC-6B owner acceptance](../decisions/ec6b-owner-acceptance.md).

**Primary question.** How can Hetu represent the names a module exports, and
establish which local declarations those names refer to, without claiming
bindings that have not been proven?

**Recommended answer in one paragraph.** Give every exported name its own
module-scoped **export entry node** (`export` kind), independent of declaration
identity. Connect the module file to each written occurrence with an
**`exports`** edge carrying the occurrence span, written form, local name and
explicit type-only syntax. Connect eligible local declarations to the entry with
an **`exported-as`** edge; use `Resolved` evidence only when the declaration
node identity is occurrence-specific (file-scoped kinds) and the attribution is
direct or uniquely scoped, and `Derived` evidence when the association is
deterministic but the target symbol identity is name-converged. Do not emit an
attribution edge when the local origin is unresolved, ambiguous, deferred or
unsupported. Emit export facts in the owning per-file unit, under a p8→p9
profile bump and a distinct, versioned export-resolver provider.

---

## 1. Scope, inputs, and method

Read in full: the accepted EC-6A architecture, adversarial cases, roadmap,
decision register, and owner acceptance; the EC-6B owner acceptance; the
EC-6B consumer documentation and handoff; the Q1/Q2 review reports at their
preserved commits (Q1 `6669058`, Q2 `b79bfce`, verdict VERIFIED WITH
QUALIFICATIONS). Inspected in source: the shared ECMAScript profile, symbol
and declaration identity, evidence, node/edge/property contracts, extraction
session and unit staging, `CodeIndexingService`, in-memory and LatticeDb
stores, query service and presets, coverage contracts, plugin versioning, and
the identity documentation.

Two disposable probes were executed at the baseline (recorded in §16):
one dumping the current extraction result for an export-form matrix, and one
dumping the relevant Tree-sitter export AST shapes. They establish what the
current p8 implementation does; they do not test a design implementation,
because none exists.

This document follows the EC-6A constraints: no new language parser, no
TypeScript type checker, no runtime/Node/bundler semantics, no cross-plugin
resolution, no identity migration without a separate owner decision, and no
core graph redesign without demonstrated necessity.

## 2. Current graph and extraction contracts

### 2.1 Nodes, edges, and properties

- Node kinds are open normalized strings: `repository`, `project`, `package`,
  `file`, `namespace`, `type`, `interface`, `callable`, `property`, `field`,
  `parameter` (`CodeGraphKinds.cs`). Adding a kind is additive.
- Edge kinds are open normalized strings: `contains`, `declares`, `references`,
  `calls`, `invokes-name`, `implements`, `inherits`, `imports`, `resolves-to`,
  `depends-on`, `returns`, `accepts`, `exercised-by`.
- Edge identity is the id string alone; **properties are not identity inputs**.
  Edge equality, including properties, decides whether same-id contributions
  merge or conflict. Any semantic multiplicity must live in identity inputs
  (EC-6A §7.1.1; `CodeGraphFacts.cs`).
- Node identity is `CodeNodeId`; nodes may carry an optional `CodeSymbolId`.
  Export entries are not declarations, so the design gives them no `SymbolId`.
- Properties are bounded (64 per fact; 32,768 text characters) and are not
  server-side query filters. Names must therefore be node names/qualities, not
  properties, when consumers must query them.

### 2.2 Declaration and symbol identity (verified)

The shared profile (`EcmaScriptExtractionProfile.cs`) emits declarations with:

- **File-scoped (path in identity):** class (`class\n{path}\n{name}`),
  interface (`interface\n{path}\n{name}`), file-level variable declarator
  (`variable\n{path}\n{name}`, kind `field`).
- **Name-scoped (no path, converge across files):** function
  (`callable\n{name}`), type alias (`type\n{name}`), enum (`enum\n{name}`),
  namespace (`namespace\n{name}`).
- Declaration records (`CodeGraphDeclaration`) are keyed by symbol identity
  plus source path and exact span; one symbol may carry many declaration
  records. Edges target **nodes**, never declaration records.
- A function-valued file variable produces a path-scoped `field` and a
  name-scoped `callable` projection from the same declarator (dual projection;
  the callable is a projection, not a second declaration of the name).

The disposable probe confirmed the convergence matrix: two files exporting
`function run` share one `callable` node id with two declaration records; two
files exporting `class C` or `interface I` have distinct node ids; `type T`,
`enum E`, and `namespace N` converge across files.

Consequence: an `exports` edge from module A to a name-scoped declaration node
does not identify A's physical declaration. Recording the module and
occurrence in edge properties does not change the target identity, and
properties are not identity inputs. This is the F01 hazard the EC-6C design
must resolve.

### 2.3 Evidence

`Structural` follows directly from syntax and is not semantically resolved;
`Resolved` means resolved by the named provider in its analysis context;
`Derived` is deterministically computed from other evidence and "must never
present itself as resolved"; deterministic qualities cannot carry confidence
(`CodeEvidence.cs`). `resolves-to` keeps its accepted policy-scoped
`Resolved` meaning and is unchanged.

### 2.4 Units, publication, and stores

- Plugins own units. Per-file units carry nodes, declarations, and edges; the
  unit origin names repository, plugin, plugin version, run, and unit; an
  optional source path/hash binds the unit to the file.
- The session stages per-file units in ordinal path order and completes each.
  The plugin-owned `{language}:modules` unit is emitted after the files and
  carries only `resolves-to` edges.
- The store validates endpoints against the **prospective** graph (previous
  publication − staged deletions + staged replacements), stages only completed
  units, and publishes atomically at run completion. Failed or cancelled runs
  retain the previous publication. Staging order is not an atomicity
  guarantee; visibility comes only from completion.
- Kinds and properties are opaque strings to the in-memory and LatticeDb
  stores and the native mirror. New kinds need no schema change.
- Query traversals filter by kind, direction, and evidence; exact-name lookup
  returns all candidates ordered by node id and never picks one. Presets are
  closed sets and do not include new kinds.

### 2.5 Profile versioning

The effective plugin version is
`{packageVersion}+{grammarVersion}.abi{abi}.p{ProfileRevision}` and is the
single planner invalidation axis. Any change to emitted facts must bump the
profile (and any provider version in lockstep). EC-6B ends at p8 /
`resolver2.relative-v1`.

## 3. Export terminology and the five fact layers

EC-6C must keep five different facts distinct:

1. **Written export occurrence** — a syntactic site that exposes a name
   (`export const a`, `export { x as y }`, `export default …`, `export … from`).
2. **Exported name** — a name on a module's export surface, independent of where
   it was written or what supplies it.
3. **Declaration association** — a deterministic link between the name and a
   specific declaration occurrence, without necessarily verifying a unique
   graph symbol.
4. **Verified declaration binding** — an association whose target declaration
   node identity is occurrence-specific and whose local binding was established
   by direct syntax or the documented module-scope model.
5. **Unresolved/ambiguous name** — a name visible in source whose originating
   declaration EC-6C does not establish.

The proposed vocabulary maps them as follows:

| Layer | Representation | Evidence |
| --- | --- | --- |
| Written occurrence | `exports` edge (file → export entry) with the written span and form | `Structural` |
| Exported name | `export` node, one per module + name, with `export-space` metadata | n/a (existence supported by occurrences) |
| Declaration association | `exported-as` edge (declaration node → export entry) with the declaration span | `Derived` when the target symbol identity is name-converged |
| Verified declaration binding | `exported-as` edge (declaration node → export entry) | `Resolved` when the target is occurrence-specific and attribution is direct or uniquely scoped |
| Unresolved/ambiguous/deferred | Entry exists; no `exported-as` edge; coverage records the reason | n/a |

The terms "entry", "occurrence edge", and "attribution edge" are used below
with exactly these meanings.

## 4. Alternatives

### 4.1 Alternative A — file-to-declaration `exports` edges (the previously proposed model)

`module file --exports--> declaration node`, with `exported-name`,
`local-name`, `export-kind`, `export-via`, `type-only`, and occurrence
properties.

**Assessment.** It reuses existing nodes and is minimal for the simplest case,
but it fails the accepted truth requirements:

1. **No declaration endpoint for unattributable names.** `export default 42`,
   anonymous defaults, unresolved local names, imported-binding exports and
   re-exports have no local declaration node. A self-edge to the file or a
   placeholder target would conflate "the module itself" with "no declaration"
   and cannot carry a proper export-name identity.
2. **The exported name has no identity.** It lives only in `exported-name`
   properties/discriminators. Properties are not identity inputs, are not
   server-side queryable, and duplicate names/aliases require ad-hoc
   discriminator construction. Exact-name lookup cannot find an export name as
   a graph entity.
3. **The F01 convergence hazard remains at the endpoint.** For
   `function`/`type`/`enum`/`namespace`, two modules' export edges target one
   shared node. Recording the module/occurrence as edge properties does not
   make the target occurrence-specific; consumers cannot rely on the target
   identity for the physical declaration, and EC-6D would have no
   export-name endpoint to bind to.
4. **Multiplicity is property-dependent.** Multiple aliases, duplicate names,
   multiple occurrences, and type/value distinctions must all be encoded in
   discriminators; the model cannot distinguish "one name written twice"
   from "two names" without parsing properties.
5. **Default and type-only semantics are squeezed into properties** while the
   graph entity remains the declaration, so a consumer asking "what names does
   this module export?" must scan edges and decode properties.

Alternative A is therefore **rejected** for EC-6C. It could represent the
subset of direct, uniquely identified declarations, but not the contract's
five layers.

### 4.2 Alternative B — explicit export-entry nodes (recommended)

`module file --exports--> export entry` for each written occurrence, plus
`declaration node --exported-as--> export entry` for eligible attribution.

**Assessment.** It gives the exported name an independent, module-scoped
identity; occurrences are edges; aliases and duplicate names are explicit; the
entry exists even when no declaration is attributable; EC-6D has a stable
bind target; and the F01 hazard is bounded by evidence and eligibility rather
than by target identity alone. The cost is one new node kind, two new edge
kinds, and new property vocabulary, with no schema change.

### 4.3 Alternative C — hybrid

Use A's declaration edges for safely attributable exports and B's entries for
unresolved/ambiguous cases.

**Assessment.** Rejected. The representation of the same exported name would
depend on whether attribution succeeded or on the declaration kind; a name
gaining or losing a declaration would change its representation; consumers and
EC-6D would have to support two models; and the F01 convergence cases still need
entries, so the complexity buys nothing. A single entry model with differential
attribution edges is simpler and more truthful.

### 4.4 Required comparison

| Criterion | A: file→declaration edges | B: export entries (recommended) | C: hybrid |
| --- | --- | --- | --- |
| 1. Identity correctness | Exported name not an identity; convergence leaks into the target | Name has module-scoped identity; occurrence and declaration are separate identities | Correct only in the entry branch; two identity regimes |
| 2. Alias multiplicity | Multiple edges to one declaration, distinguishers ad hoc | One entry per exported name; one occurrence edge per written occurrence | Split across models |
| 3. Declaration attribution | Target may be shared; attribution relies on properties | Attribution edge with declared evidence and occurrence span | Two representations |
| 4. Default-export support | No declaration target for anonymous/expression defaults | Entry `default` exists; no attribution edge | Entries needed for defaults |
| 5. Type/value namespaces | Only edge properties; no name entity | `export-space` metadata + `type-only` occurrences + declaration kind | Mixed |
| 6. Unresolved exports | No valid target; self-edge hack | Entry with no attribution edge; coverage | Entry branch only |
| 7. Queryability | Property scanning; no exact-name export lookup | Entry name is exact-name queryable; traversals work | Two query models |
| 8. Publication/replacement | Per-file or resolution unit; no entry lifecycle | Per-file unit; entry/edge lifecycle follows file replacement | Mixed ownership |
| 9. Durable stores | No schema change | No schema change; new kinds opaque | No schema change |
| 10. Consumer compatibility | New edge kind; names not queryable | New kinds only; existing queries/presets unchanged | New kinds + two models |
| 11. EC-6D requirements | No export-name endpoint; binding would target shared symbols or edges | Entry is the natural bind endpoint; aliases/defaults/type spaces explicit | Inconsistent bind targets |
| 12. Implementation complexity | Low for happy path; high for F01/aliases/defaults | Medium: one node kind, two edge kinds, shape capture + pass | Highest |

The smallest model that satisfies the truth requirements is **B**, not A: A is
smaller only by omitting the capabilities the contract requires.

## 5. Recommended representation

### 5.1 Vocabulary

Additive constants:

| Kind | Value | Direction/meaning |
| --- | --- | --- |
| Node kind `Export` | `export` | One exported name on one module's surface |
| Edge kind `Exports` | `exports` | Module file → export entry; a written export occurrence exposes the name |
| Edge kind `ExportedAs` | `exported-as` | Local declaration node → export entry; the declaration occurrence supplies the name |

Property keys (all bounded, ordinal value sets):

| Key | Applies to | Values |
| --- | --- | --- |
| `export-kind` | `exports` occurrence edge | `named`, `default` |
| `export-via` | `exports` occurrence edge | `direct`, `local`, `local-import`, `expression`, `re-export`, `namespace-re-export` |
| `local-name` | `exports` occurrence edge (when a local binding name is written) | Written local name |
| `type-only` | `exports` occurrence edge (existing key) | Present as `true` only for explicit type-only syntax |
| `via-module` | `exports` occurrence edge (re-export forms) | Raw written specifier, bounded; omitted when unavailable |
| `export-space` | export entry node | `type`, `value`, `both`, `unknown` |
| `attribution-basis` | `exported-as` edge | `direct-syntax`, `module-scope-lookup` |
| `declaration-kind` | `exported-as` edge | `variable`, `function`, `class`, `interface`, `type`, `enum`, `namespace` |

The exported name is the entry node's `Name` (and `QualifiedName`), not a
property. This deliberately replaces the EC-6A proposal's `exported-name` key so
that names are queryable graph entities rather than edge properties.

The entry node has no `SymbolId`; consumers can distinguish it from
declarations by kind and by `SymbolId` presence. Node properties: `export-space`
(and the existing language property). Occurrence- and attribution-specific
facts live on their edges.

### 5.2 Graph shape

```text
file node ──exports──▶ export entry ◀──exported-as── declaration node
   (occurrence span,     (module+name,        (declaration span,
    written form,         export-space)        evidence Resolved/Derived,
    local/type-only)                           basis + declaration-kind)
```

- One **entry node per (module path, exported name)**. The module is the owning
  file; the entry is emitted in that file's unit.
- One **`exports` edge per written occurrence** of the name.
- Zero or more **`exported-as` edges**, one per declaration occurrence that
  supplies the name; multiple edges represent overloads, declaration merging,
  or duplicate-name conflicts rather than forcing a one-to-one choice.
- No edge from the entry to a remote module and no fabricated declaration node.

### 5.3 Identity

**Export entry node:**

```text
{language}:export:{sha256(modulePath + "\n" + exportedName)}
```

- `modulePath` is the repository-relative POSIX source path (the file node's
  `QualifiedName`).
- `exportedName` is the written name without quotes; `default` for default
  exports. Names are ordinal, case-sensitive, not Unicode-normalized, and
  bounded at 256 characters.
- The entry is occurrence-independent and therefore stable when an export
  statement moves or when the same name is written multiple times. It changes
  when the exported name changes (alias rename) or the module path changes.
- Identity is module-scoped; two modules exporting `run` have distinct entries
  even though their function declarations share a node.
- `export-space` is recomputed for each publication from that publication's
  occurrences; the entry id remains the name. This is not a within-publication
  identity collision: one publication has one entry with one union value, and a
  file-unit replacement makes the new value a fact of the new publication.
  Consumers must read the current publication's properties rather than assume a
  space across edits.

**`exports` occurrence edge:**

```text
{language}:sha256("exports\n" + fileNodeId + "\n" + entryNodeId + "\n" +
                  "exports:" + exportKind + ":" + exportVia + ":" +
                  (localName ?? "") + ":" + (explicitTypeOnly ? "type-only" : "normal") + ":" +
                  (viaModule ?? "") + "\n" +
                  startLine:startColumn:endLine:endColumn)
```

- Span is the written occurrence of the exported name: the declaration name for
  direct exports, the specifier name/alias for export lists, the `default`
  keyword for default exports.
- One-based lines and one-based UTF-16 code-unit columns, end-exclusive. The
  EC-6B coordinate correction is preserved; no byte-column assumption returns.
- Properties are duplicated into the discriminator where they distinguish
  written forms (kind, via, local name, explicit type-only, and the raw
  `via-module` specifier when present) so a property cannot change under an
  unchanged identity. The raw specifier is a written fact and is therefore an
  identity input for re-export occurrences; if it is unavailable
  (overlong/escaped specifier), the occurrence is still identifiable but the
  module property is omitted and the binding is deferred.

**`exported-as` attribution edge:**

```text
{language}:sha256("exported-as\n" + declarationNodeId + "\n" + entryNodeId + "\n" +
                  "exported-as:" + localName + ":" + attributionBasis + ":" +
                  declarationKind + "\n" +
                  startLine:startColumn:endLine:endColumn)
```

- Span is the attributed **declaration occurrence**: the declaration name span
  captured while walking the file (for direct exports) or the unique eligible
  declaration's name span (for local lookup). The evidence location path is
  the declaring module, so the F01 declaring-module requirement is explicit
  even when the declaration node is name-converged.
- Source is the declaration node; target is the entry. The edge is
  occurrence-specific through its span even when the node is shared.
- Evidence: `Resolved` for occurrence-specific declarations, `Derived` for
  name-converged declarations (see §7). The provider is the distinct export
  resolver (see §8).

**Property and identity consistency.** Every semantic property that
distinguishes a written fact (export kind, export via, local name, explicit
type-only, raw `via-module`) is part of the occurrence-edge discriminator; the
attribution discriminator includes the attribution basis and declaration kind.
The only metadata outside identity is the entry-level `export-space` union,
which is exactly one value per entry within a publication; it is recomputed on
each file-unit replacement and is never contradicted by another contribution in
the same publication.

**Multiplicity and collisions:**

- Two aliases of one declaration → two entries (different names), two
  occurrence edges, two attribution edges from the same declaration.
- Two occurrences of one name → one entry, two occurrence edges. If both are
  local bindings with different declarations, the entry gets two attribution
  edges; the surface is visibly ambiguous rather than silently collapsed.
- Duplicate exported names (invalid TypeScript but parseable) are preserved as
  multiple occurrence edges; when their local declarations differ, the
  attribution coverage records the ambiguity and the surface edges remain
  emitted. No arbitrary first/last choice is made.
- Identity collisions (same id with differing facts) must fail the export pass
  loudly and be rejected by the existing batch/store validators; duplicates are
  never silently merged at emit time.
- Removing an export occurrence removes its occurrence edge; the entry remains
  only if another occurrence of the same name remains. Removing a declaration
  removes its attribution edge on re-extraction; the entry remains if the name
  is still written. There is no tombstone logic; the file unit is replaced.
- Moving an export occurrence changes the occurrence edge id but not the entry
  id. Moving a declaration changes the attribution edge id. This is
  occurrence-anchored identity, consistent with the existing graph and EC-6B.

## 6. Syntax coverage and classification

Only **module-level** export statements (`isFileLevel`) are captured. Export
statements nested in namespaces/functions are outside the module surface and
are deferred.

| Form | Classification | Facts |
| --- | --- | --- |
| `export const/let/var a = …` (each simple declarator) | Supported | Entry per declarator; occurrence `direct`; attribution to the field (Resolved, path-scoped); multi-declarators per declarator |
| `export function f() {}` / generator | Supported | Entry; occurrence `direct`; attribution Derived (name-converged callable) |
| `export class C {}` / abstract class | Supported | Entry; occurrence `direct`; attribution Resolved (path-scoped); space both |
| `export interface I {}` | Supported | Entry; occurrence `direct`; attribution Resolved; space type |
| `export type T = …` | Supported | Entry; occurrence `direct`; attribution Derived (name-converged); space type; this is a **direct type alias**, not a type-only export list |
| `export enum E {}` | Supported | Entry; occurrence `direct`; attribution Derived; space both |
| `export namespace N {}` | Supported | Entry; occurrence `direct`; attribution Derived; space both; nested exports not captured |
| `const x = …; export { x }` | Supported | Entry; occurrence `local`; attribution via unique module-scope lookup |
| `export { x as y }` | Supported | Entry `y`; occurrence `local`, `local-name: x`; attribution to x's declaration |
| `export { x as "quoted-name" }` | Supported when the quoted name has no escape sequence and is non-empty/bounded | Entry with the string content; local name x; attribution |
| `export { x as first, x as second }` | Supported | Two entries/occurrences; both attribute to the same declaration |
| `const type = …; export { type }` / `{ type as kind }` | Supported | Grammar-aware: `type` is a binding name, not a type modifier; no `type-only` |
| `export { type T }` (inline modifier) | Supported | Entry T; occurrence `type-only: true`; type-space attribution |
| `export type { T }` (type-only list) | Supported | Entries; occurrence `type-only: true`; type-space attribution |
| `export default function f() {}` / `class C {}` | Supported | Entry `default`; occurrence `direct`, local-name f/C; attribution to the declaration (function Derived / class Resolved) |
| `export default function () {}` / `class {}` | Supported as an expression form | Entry `default`; occurrence `expression`; no attribution; no fabricated declaration; space value (function) / both (class) |
| `export default current` (identifier) | Supported with local lookup | Entry `default`; occurrence `local`, local-name current; attribution only if exactly one value-space declaration |
| `export default 42` / arbitrary expression | Supported as an expression form | Entry `default`; occurrence `expression`; no attribution; space value |
| `export type { T as default }` | Supported | Entry `default`; occurrence `type-only: true`; type-space attribution |
| `export { x } from './m'` / `{ x as y } from` | Structural surface only (recommended; owner decision) | Entry with written name; occurrence `re-export`, `via-module`; no attribution; EC-6D binds |
| `export * as ns from './m'` | Structural surface only (recommended) | Entry `ns`; occurrence `namespace-re-export`, `via-module`; no attribution |
| `export * from './m'` | Deferred (no written exported names) | No entry; coverage deferred; existing `resolves-to` unchanged |
| `import { x } from './m'; export { x }` | Structural surface only, binding deferred (recommended; D-06 reconciliation) | Entry; occurrence `local-import`, local-name x; no attribution; EC-6D may bind |
| `module.exports = …`, `exports.name = …`, `export = …` | Unsupported | No export entry; not treated as ESM; existing `resolves-to`/`imports` behavior unchanged; coverage unsupported |
| `export declare …` (ambient) | Unsupported/deferred | No entry in EC-6C; ambient declarations are not modeled (EC-5 limitation); coverage unsupported |
| `export const { a, b } = …` / array patterns | Deferred | No entries in EC-6C; destructured bindings are not modeled; coverage deferred/unsupported |
| Overload signatures (`export function f(…): …;`) | Structurally observed; attribution only through an emitted implementation | Occurrence edges for written signatures; if the implementation is exported and the function node exists, the entry has one Derived attribution; signatures without an implementation are unattributed |
| Declaration merging (class + namespace, interface + const, etc.) | Supported as multiple attributions | One entry per name; multiple occurrence/attribution edges; space is the union; no one-to-one claim |
| Duplicate exported names | Represented; ambiguous surface | One entry; multiple occurrence edges; coverage records the duplicate; attribution edges reflect each declaration |
| Unquoted/Unicode identifier names | Supported | Ordinal, unchanged; UTF-16 columns |
| Empty, whitespace-only, overlong, or escape-bearing exported names | Unsupported | No entry or no `via-module`; coverage unsupported; no truncated text |
| Parser-recovered malformed export syntax | Partial | Error/missing subtrees skipped; valid sibling facts kept; file partial; no fabricated facts |

Re-export and imported-binding entries are **surface facts, not remote
bindings**. The owner must confirm this interpretation (decision register
D-EC6C-09) or choose to suppress those entries entirely.

## 7. Declaration attribution eligibility and evidence

### 7.1 Attribution bases

1. **Direct syntax (`direct-syntax`).** The declaration is syntactically inside
   the export statement. The export shape captures the declaration AST span;
   the pass matches it to the declaration record emitted in the same file. The
   match is by exact source span and declaration kind, not by name equality.
   The callable projection of a function-valued variable shares the declarator
   span and is excluded from the eligible set; the field is the written
   declaration. This is occurrence-specific even when the resulting node is
   name-converged.
2. **Module-scope lookup (`module-scope-lookup`).** A local list or default
   identifier references a module-scope name. The profile builds a per-file
   module-scope table during the walk:
   - file-level declarations (excluding class/interface members and
     function-local declarations);
   - import bindings (named/default/namespace, with their written local names);
   - the callable projection of a function-valued variable is marked as a
     projection, not an independent declaration.
   A lookup succeeds only when the name has **exactly one eligible declaration**
   and no import binding. Zero → unresolved; more than one → ambiguous;
   import → deferred (`local-import`). This is a documented, file-local,
   deterministic module-scope model, not name equality across the repository.

### 7.2 Evidence levels

| Target declaration kind | Node identity | Attribution evidence | Rationale |
| --- | --- | --- | --- |
| File-level variable (`field`) | Path-scoped, per file+name | `Resolved` | The node is occurrence-specific; direct or unique lookup |
| Class | Path-scoped | `Resolved` | Same |
| Interface | Path-scoped | `Resolved` | Same |
| Function (`callable`) | Name-converged | `Derived` | The node is shared across files; the edge records the declaration occurrence and declaring module, but the graph symbol is not occurrence-specific |
| Type alias | Name-converged | `Derived` | Same |
| Enum | Name-converged | `Derived` | Same |
| Namespace | Name-converged | `Derived` | Same |
| Function-valued variable | Path-scoped `field`; callable is a projection | `Resolved` to the field; the projection is not attributed | The written declaration is the variable; the callable is a projection |
| Imported binding | No local declaration | No attribution (`local-import`) | Deferred to EC-6D; D-06 boundary |
| Anonymous/expression default | No declaration | No attribution | No fabricated declaration |
| Ambient/destructured declaration | No modeled declaration | No attribution; form unsupported/deferred | EC-5 limitations |

**Resolved claim:** the export entry's local binding is the declaration
occurrence recorded in the edge evidence location; the declaration node identity
is file-scoped and occurrence-specific; the association was established by
direct capture or by the documented module-scope model. This is a Hetu graph
binding, not compiler/bundler/runtime equivalence.

Within-file duplicate declarations (for example `var` redeclarations or
interface merging) may share a path-scoped node. The attribution edge remains
occurrence-specific through its span and direct-syntax basis; the node's
declaration-record list remains available through the existing declaration API.
A local-list lookup with multiple eligible declarations is ambiguous and emits
no attribution; only direct syntax can choose one of several same-name
declaration occurrences, and it records that choice on the edge.

**Derived claim:** the export entry's local binding is deterministically
associated with the declaration occurrence recorded in the edge evidence
location under the documented model, but the target symbol identity is
name-converged and is **not** proof of a unique physical declaration.
`Derived` must never be presented or consumed as `Resolved`. The edge evidence
location path names the declaring module; the occurrence span names the
declaration record.

**No attribution edge:** the exported name is visible but EC-6C did not
establish a local declaration association. Coverage distinguishes unresolved,
ambiguous, deferred and unsupported cases.

The F01 gate is satisfied without identity migration:

- the exported name has its own module-scoped identity;
- the attribution edge carries the declaring module (evidence location path)
  and the exact declaration occurrence, not merely the shared node identity;
- `Resolved` is restricted to occurrence-specific declaration identities;
- `Derived` explicitly withholds a verified-binding claim for name-converged
  kinds;
- no `exports`/`binds` edge targets an unattributed name-based symbol.

### 7.3 Type/value compatibility

- `type-only` is recorded only for explicit syntax (`export type …`,
  inline `type` specifier), reusing the EC-6B grammar-aware rule. A binding
  named `type` never triggers it.
- An explicit type-only occurrence can attribute only to a declaration with
  type space (interface, type alias, class, enum, namespace). A value-only
  declaration makes the type-only attribution ineligible; the entry remains
  structural and coverage records the mismatch.
- A non-type-only list/default identifier can attribute to any declaration;
  the entry's space is derived from the declaration (a type-only declaration
  in a value list exposes only type space; this is not a value claim).
- `export default` expressions are value-space; a bare identifier must resolve
  to a value-space declaration to be attributed. If the identifier resolves to
  an imported binding, the occurrence is `local-import` and its binding is
  deferred; if it resolves to a type-only declaration or is otherwise not
  value-compatible, the entry remains structural with `export-space=unknown`
  and no attribution (TS would reject it).
- The entry's `export-space` is the union of occurrence contributions:
  type-only → type; attributed declaration → its space (variable/function =
  value; class/enum/namespace = both; interface/type = type); expression →
  value (anonymous class → both); re-export/imported/unresolved → unknown
  unless the occurrence is explicitly type-only (then type). The union is
  `both` when type and value are both present.

## 8. Evidence and provenance

| Fact | Quality | Provider | Version |
| --- | --- | --- | --- |
| `exports` occurrence edge | `Structural` | Extraction plugin (`penghou.hetu.tree-sitter.typescript` / `.javascript`) | Plugin version p9 |
| `exported-as` attribution | `Resolved` or `Derived` per §7.2 | Export resolver `penghou.hetu.tree-sitter.ecmascript-export-resolver` | `{packageVersion}+exports1.local-v1` |
| `resolves-to` (unchanged) | `Resolved` | Module resolver `…ecmascript-module-resolver` | `{packageVersion}+resolver2.relative-v1` |
| Existing facts (unchanged) | Existing | Existing | Existing |

- The export resolver is a distinct provider, like the EC-6B module resolver,
  so consumers can separate "who parsed" from "who attributed the export".
  It introduces no second invalidation axis: the export resolver revision and
  the profile revision move together on the same planner axis.
- `exports1.local-v1` names the documented module-scope attribution model.
  Any change to eligibility, evidence mapping, namespace derivation, or
  attribution rules must bump the revision and the profile.
- Deterministic qualities carry no confidence.
- Consumer documentation must state the exact Resolved and Derived scopes and
  must state that absence of an attribution edge means "not established under
  EC-6C", never "no declaration exists". It must also preserve the mandated
  EC-6B `resolves-to` sentence unchanged.

## 9. Publication and invalidation

**Recommendation: per-file units (Option A).** All EC-6C facts are file-local:
the entry depends only on the file's written names; attribution depends only on
the file's module-scope declarations and imports. The profile emits them as
part of the owning file's unit, after the walk (`Extract` returns after a
post-walk export pass). No new unit, no cross-file dependency, no core change.

| Event | Behavior |
| --- | --- |
| Export added/removed/renamed | File content hash changes → plugin executes → file unit replaced with new entries/occurrences/attributions; stale facts removed |
| Declaration deleted, export remains | File content changes → re-lookup finds no declaration → attribution edge not emitted; entry remains; old edge removed with replacement |
| Declaration kind/alias changes | File unit replaced; identity changes as defined; no tombstones |
| File deleted | Existing lifecycle deletes the file unit; entries/edges must not dangle |
| Empty export inventory | File unit completes without export nodes/edges; previous export facts are removed by replacement |
| Unchanged file / no-op reindex | Planner returns previous publication; entry/edge ids stable |
| Plugin/profile version change (p8→p9) | All plugin sources re-extract; both plugins bump; module resolution unit rebuilds; no mixed revisions |
| Extraction/resolver failure after file staging | Run fails; staged footprint discarded; previous publication retained (EC-6B proved) |
| Cancellation | Run reaches Cancelled; previous publication retained; no partial export facts |
| Endpoint ordering | Entries, declarations and edges are in the same unit; the replacement is validated as a whole; the existing endpoint backstop remains |
| Durable store reopen | Export facts are ordinary file-unit facts; LatticeDb persists them; reopen fixture required |

Why not `{language}:exports` in EC-6C: the facts are not cross-file; a separate
unit would add ownership/ordering/endpoint complexity for no benefit. EC-6D
re-export/star closure and import binding may need a cross-file export unit or
additional dependency tracking; that is an EC-6D decision, not an EC-6C
requirement. A hybrid is unnecessary for the same reason.

### 9.1 Coverage accounting

Add `exports` and `exported-as` to the Tree-sitter coverage list and the plugin
produced-kind sets. The existing `CodeRelationshipCoverage` fields are reused
with these exact meanings:

| Kind | Candidates | Emitted / Internal | Cross | Unresolved | Ambiguous | Unsupported | External |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `exports` | Every recognized written export occurrence (including unsupported forms) | Occurrence edges; all file-local | 0 | 0 | 0 at the surface (duplicate names are visible as multiple occurrence edges; attribution-kind ambiguity is counted there) | Forms with no entry: `export *`, CJS/`export =`, ambient/destructuring per policy, unsupported/overlong/escape-bearing names, malformed sites | 0 |
| `exported-as` | Local-binding occurrences that EC-6C expects to attribute (direct declarations, local lists, default identifiers/named) | Attribution edges; all file-local | 0 | No eligible local declaration found | Multiple eligible declarations or duplicate-name conflict | Ineligible declaration kind, type/space mismatch, projection-only match, unsupported identity | 0 |

- `EdgesEmitted == InternalTargets + CrossProjectTargets` holds: every export
  entry/attribution is within one file unit.
- Imported-binding and re-export occurrences are surface-emitted but are not
  `exported-as` candidates; their binding is deferred. If the owner accepts an
  additive `deferredTargets` counter, those deferrals are reported there;
  otherwise they are documented as deferred and excluded from the attribution
  candidates (not misreported as unresolved). The design recommends the
  additive counter as the clearer option (decision D-EC6C-12); the fallback is
  an explicit documentation mapping.
- Coverage state is `Partial` when any owning-plugin source had a parse error
  or could not be decoded, following the existing session rule.
- `CodeGraphExtractionResult.UnresolvedRelationships` keeps its accepted EC-6B
  meaning (module-resolver unresolved only) unless the owner explicitly extends
  it; export attribution outcomes are reported per kind. This avoids a silent
  change to EC-6B consumer diagnostics.

Invariants required by the design (all already supported by the accepted
publication model): no missing endpoints; removed exports remove their facts;
renames replace identities; unchanged exports keep deterministic identities;
failure/cancellation publishes nothing partial; deleted declarations leave no
stale resolved attribution; module-resolution facts remain correct; existing
structural facts remain unchanged; no mixed plugin/profile revisions.

## 10. Queries and consumer usability

Existing APIs are sufficient; no new query method is required for EC-6C.
Illustrative public-API patterns:

| Question | Query |
| --- | --- |
| What does module M export? | Outgoing `GetNeighborhoodAsync(M_file, [exports])` → `exports` edges + `export` nodes |
| What names refer to declaration D? | Outgoing traversal from D over `[exported-as]` → entries |
| Which declaration supplies entry E? | Incoming traversal to E over `[exported-as]` → declaration nodes + edge evidence |
| Which exports are aliases? | Compare `exports` edge `local-name` with the target entry `Name`; multiple names per declaration are separate entries |
| Which exports are type-only? | `exports` edge `type-only: true`; entry `export-space: type`/`both` |
| Which have verified attribution? | `exported-as` edges filtered by `EvidenceQualities=[Resolved]` (Derived is association, not verified binding) |
| Which are visible but unattributed? | Entries with no `exported-as` edge (or only evidence not Resolved) |
| Where was it exported/declared? | `exports` edge location (written occurrence); `exported-as` edge location (declaration occurrence) |
| What evidence supports it? | Edge `Evidence` (quality, provider, version, location) |
| Which modules export name X? | Exact-name lookup on X (entries) then incoming `exports` to their file nodes |

- The entry's `Name`/`QualifiedName` is the exported name; exact-name lookup
  returns entries from all modules as candidates (never a single pick), matching
  existing lookup semantics. Module ownership comes from the `exports` edge.
- `export-space` and `type-only` are properties; the traversal API filters by
  kind/direction/evidence only, so property filtering is client-side, as with
  existing properties. Optional convenience APIs are deferred; the task warns
  against assuming properties are efficiently queryable, and the design does
  not rely on them for name identity.
- Existing exact-name queries, declaration lookup, source navigation,
  structural import queries, `resolves-to` semantics and impact presets are
  unchanged. `exported-as` is a new kind and is not in any preset; the design
  deliberately does **not** use the existing `references` kind, so
  `FindReferencesAsync` behavior is unchanged.
- Consumers can distinguish declarations from entries via node kind and the
  absence of `SymbolId`; entry nodes are not returned by declaration lookup.

## 11. Compatibility and profile evolution

- Additive vocabulary: `export` node kind, `exports`/`exported-as` edge kinds,
  and the property keys in §5.1. No existing kind, identity, or property
  changes.
- Profile revision **p8 → p9** for both ECMAScript plugins, because new facts
  are emitted. The export resolver provider reports `exports1.local-v1`; the
  module resolver stays `resolver2.relative-v1`.
- Existing file, declaration, structural edge and `resolves-to` identities must
  be byte-identical across p8→p9 for unchanged sources; the only additions are
  export entries/edges and their coverage.
- Old publications remain valid (they lack export facts). The version bump
  re-extracts; no mixed revisions. No identity migration is proposed.
- Stores and snapshots need no schema change; kinds/properties are opaque.
  Coverage gains the new kinds; if a deferred counter is added it is an
  additive, owner-gated API change (decision register D-EC6C-12).
- The planner needs no new change detection: file content hash + plugin version
  already cover file-local export facts. EC-6D cross-file binding may change
  that; deferred.
- Capability flags are coarse; no new flag is required. Coverage kinds are the
  per-kind contract.
- Run diagnostics (`NodesProduced`, `EdgesProduced`, per-kind coverage) grow
  additively with export entries/edges; no existing count is redefined.
  `CodeGraphExtractionResult.UnresolvedRelationships` keeps its module-resolver
  meaning unless the owner explicitly extends it (D-EC6C-12).

## 12. Performance and bounded behavior

- Per file: build the module-scope declaration/import table in O(declarations),
  capture export shapes in source order, and group entries by name with
  deterministic sorted maps. Attribution lookup is O(1) average per local name;
  direct attribution matches captured AST spans to declaration records.
- Facts per file are bounded by declarations + export occurrences + distinct
  exported names; the unit/batch caps fail loudly rather than truncating.
- No cross-file work, no filesystem/network access, no type inference, no
  recursion beyond the existing AST walk. Cancellation remains between files;
  the post-walk pass is short and deterministic.
- Pathological names are bounded at 256 characters; overlong/escape-bearing
  names are rejected without retaining text; duplicate names/large export lists
  are linear. No benchmark numbers are claimed.

## 13. Deferred semantics and EC-6D compatibility

EC-6C does not implement import-to-export binding, re-export closure, star/
namespace closure, cycles, or remote declaration-origin resolution. The
representation is designed to accept them later:

- EC-6D binds an importing file's written local name to an export entry
  (aliases/default/type-only/namespace supported by entries and their metadata).
- Re-export entries (if the owner accepts structural emission) name the
  exported binding without claiming a remote declaration; EC-6D can bind them
  to the target module's entries.
- Star exports without written names remain an EC-6D table computation; the
  entry model does not fabricate names.
- `Derived` attribution is available to EC-6D but should not be promoted to a
  verified import binding; EC-6D's contract decides whether it is usable.
- Entry name identity is stable; no identity migration is expected for EC-6D.
- EC-6D may need a plugin-owned cross-file unit or dependency tracking for
  re-export/star closure; that is explicitly outside this design.

## 14. Concrete graph examples

### 14.1 Direct, local, default

```ts
// src/model.ts
export const count = 1;          // 1:1
export function calculate() {}   // 2:1
const current = {};              // 3:1
export { current as activeProject }; // 4:1
export default function () {}    // 5:1
```

Illustrative facts (ids abbreviated; entry hash = sha256(`src/model.ts\nname`)):

```text
NODE typescript:export:46c3f3d3… kind=export name=count qualified=count
  property export-space=value
EDGE exports file(src/model.ts) -> entry(count) Structural plugin p9
  location 1:14-1:19; export-kind=named; export-via=direct; local-name=count
EDGE exported-as field(count) -> entry(count) Resolved export-resolver exports1.local-v1
  location 1:14-1:19 (declaration name); attribution-basis=direct-syntax; declaration-kind=variable

NODE typescript:export:42cc362d… kind=export name=calculate qualified=calculate
  export-space=value
EDGE exports file -> entry(calculate) Structural; direct; local-name=calculate
EDGE exported-as callable(calculate) -> entry(calculate) Derived
  declaring module src/model.ts; declaration occurrence; declaration-kind=function
  (the callable node is name-converged across files; Derived is not a verified binding)

NODE typescript:export:34df1e83… kind=export name=activeProject qualified=activeProject
  export-space=value
EDGE exports file -> entry(activeProject) Structural; export-via=local; local-name=current
EDGE exported-as field(current) -> entry(activeProject) Resolved
  attribution-basis=module-scope-lookup; declaration-kind=variable

NODE typescript:export:29eab297… kind=export name=default qualified=default
  export-space=value
EDGE exports file -> entry(default) Structural; export-kind=default; export-via=expression
(no exported-as: anonymous default function has no declaration node)
```

### 14.2 Unresolved and deferred

```ts
export { missing };                 // unresolved local
export { x } from './other';        // re-export surface only
import { imported } from './m';
export { imported };                // imported binding, deferred
export * from './other';            // no written name; deferred
```

- `missing`: entry exists (`export-via=local`, no local-name attribution);
  no `exported-as`; coverage unresolved.
- `x`: entry exists (`export-via=re-export`, `via-module=./other`); the EC-6B
  `resolves-to` edge remains; no `exported-as`; EC-6D binds.
- `imported`: entry exists (`export-via=local-import`, `local-name=imported`);
  no `exported-as`; deferred (D-06 reconciliation).
- `export *`: no entry; coverage deferred; the `resolves-to` edge remains.

### 14.3 F01 convergence

```ts
// src/a.ts: export function run() {}
// src/b.ts: export function run() {}
```

Two entries: `typescript:export:<hash(src/a.ts\nrun)>` and
`<hash(src/b.ts\nrun)>`. Each has its own `exports` edge and each
`exported-as` edge targets the shared `callable(run)` node with `Derived`
evidence, its own declaration occurrence, and its own declaring-module path.
No consumer can mistake the shared node for a unique physical declaration; the
entry and edge provenance remain module- and occurrence-specific.

### 14.4 Type/value union and merging

```ts
export interface Model { id: string; }
export const Model = 1;              // type + value name (structural)
export class C {}
export namespace C { export const v = 1; }
```

- Entries `Model` and `C`, each `export-space=both`; `Model` has two
  occurrence and two attribution edges (interface Resolved, variable Resolved);
  `C` has two attribution edges (class Resolved, namespace Derived). The entry
  does not claim a single declaration.
- `export type { Model }` alone would produce `export-space=type`; the
  occurrence `type-only: true`.

## 15. Risks and limitations

- Name-converged declaration kinds (function/type/enum/namespace) cannot
  produce a `Resolved` declaration binding without identity migration; the
  design records `Derived` association instead. This is deliberate and
  owner-visible.
- The module-scope model is file-local and syntax-level; it does not implement
  TypeScript merging/overload semantics, destructuring, ambient declarations,
  or type checking. Those forms are deferred/unsupported and documented.
- Duplicate exported names and overload signatures are represented
  syntactically; consumers may see multiple occurrence/attribution edges.
- Re-export/imported-binding entries are surface facts only; remote binding is
  EC-6D.
- No real-repository export-ground-truth qualification is performed in this
  design; EC-6C/EC-6F must establish it.
- The recommended representation supersedes the previously proposed
  file-to-declaration `exports` model; owner acceptance is required before
  implementation.

## 16. Disposable probe record

**Source revision:** `038295f26ce76a8747e923f9e0ef971e773c166e` (owner-accepted
EC-6B tree). TypeScript grammar `tree-sitter-typescript` v0.23.2 (pinned);
JavaScript grammar v0.25.0. Windows 10.0.26200; .NET SDK 10.0.401; `net10.0`.

**Probe source.** Two temporary xunit tests in the design worktree, not
committed:

1. `TempEc6cExportProbe`: for 20 fixtures (direct variables; local list/alias;
   export-before-declaration; function convergence across two files; default
   named; anonymous/expression defaults; default identifier;
   class/interface convergence; type/enum/namespace convergence; `type`
   identifier; type-only forms; quoted name; imported-binding export;
   re-exports; CJS; ambient; destructuring; overloads; merging; duplicate
   names), it ran `TreeSitterTestSupport.ExtractAsync` with the pinned
   TypeScript plugin and dumped distinct nodes, declaration records with
   locations, and non-contains/declares/imports/invocation edges.
2. `TempEc6cAstProbe`: parsed 15 export statement forms with the pinned parser
   and dumped node types/fields (`declaration`, `source`, `value`, `name`).

**Commands.**

```text
dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj \
  --nologo --filter "FullyQualifiedName~TempEc6cExportProbe"
dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj \
  --nologo --filter "FullyQualifiedName~TempEc6cAstProbe"
```

Both passed. The temporary files were deleted; the observed results are
summarized here and reproduced in the design analysis.

**Actual results (summarized).**

- The current p8 extractor emits **no export-specific facts** for any fixture.
- `export const a = 1, b = 2` emits two `field` nodes with distinct path-scoped
  identities and declarations at the declarator spans.
- `export function run` in two files emits one shared `callable` node id with
  two declaration records (name convergence confirmed).
- `class C` and `interface I` in two files emit distinct node ids (path-scoped).
- `type T`, `enum E`, and `namespace N` in two files emit shared node ids
  across files (name convergence confirmed; enum kind is `type`, namespace kind
  `namespace`).
- `export default function f` / `class C` emit ordinary function/class
  declarations with local names; anonymous/expression defaults emit no
  declarations; `export default current` emits the `current` field.
- `export { x }` (local, alias, type-only, quoted, imported binding) emits no
  export facts; imported-binding export emits the `resolves-to` edge only.
- All four re-export forms emit only `resolves-to` edges; CJS assignment,
  ambient declarations and destructuring emit no declarations; overload
  signatures emit no declarations while the implementation emits one callable;
  class+namespace merging emits one path-scoped class node and one name-scoped
  namespace node.
- AST shapes: `export type T = …` has `declaration=type_alias_declaration` (no
  statement-level `type` token); `export type { T }` has a statement-level
  anonymous `type` plus an export clause; `export default function f` and
  `export default class C` use the `declaration` field; anonymous/expression
  defaults use the `value` field; `export declare …` wraps
  `ambient_declaration`; destructuring uses `object_pattern`; overload
  signatures are `function_signature`.

**Supported conclusions.** (1) The current graph has no export representation;
all EC-6C facts are additive. (2) The F01 convergence categories and the
path-scoped categories are exactly as documented. (3) The AST provides the
fields needed to separate declaration exports, type-only lists, default
declaration/value forms, re-exports, ambient declarations and overload
signatures without text heuristics.

**Limitations.** The probe covers the pinned TypeScript grammar and fixtures
only; it does not execute a design implementation, a store publication, or a
JavaScript export matrix; and it does not establish real-repository export
ground truth.
