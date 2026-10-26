# EC-6D symbol-binding contract (R1 proposal)

**Status: remediated planning contract for owner review.** This document
specifies the two missing fact capabilities identified by Q1 finding F-02 and
the binding rule that joins them. It authorizes no implementation. Any
implementation requires separate owner approval of this contract and of the
representation ADR it defines.

## 0. Problem statement (from Q1 evidence)

At the accepted EC-6C p9 baseline, the graph contains relative module-file
resolution (`resolves-to`, `Resolved` evidence under `relative-v1`),
file-scoped heritage placeholders (`implements`/`inherits` to
declaration-less `heritage-target` nodes), and positive-only export-name
slots (`export` entries + per-occurrence `exports` edges). It contains no
imported-name binding evidence and no export-to-declaration attribution
(Q1 scenarios A, D, G1, G2, G5). Name-only matching is unsafe: it produced a
wrong-binding case (alias collision) and a spurious duplicate binding in Q1
fixtures, and one false positive plus one false negative on the committed
corpus.

## 1. Fact contract A — import occurrences

### 1.1 Required content

For **every import occurrence** of a supported form in a TypeScript source
file, extraction must record:

| Field | Meaning | Notes |
| --- | --- | --- |
| `sourceFile` | importing file node identity | existing file node |
| `importSpan` | UTF-16 span of the import statement | existing coordinate conventions |
| `specifierSpan` | UTF-16 span of the module specifier token | distinct from statement span |
| `moduleSpecifier` | written specifier text | unchanged from source |
| `importedName` | name imported from the module | `null` for default/namespace forms |
| `localName` | binding introduced into the file scope | equals `importedName` when unaliased |
| `isTypeOnly` | type-only status | statement-level `import type`, inline `type` modifier, or `import type` statement |
| `form` | `named`, `named-aliased`, `default`, `namespace`, `import-equals`, `dynamic`, `unsupported` | enumerated, never guessed |
| `resolutionStatus` | `resolved`, `unresolved`, `external`, `unsupported` | inherited from the relative-v1 resolver outcome |
| `resolvedTarget` | resolved target file node identity | present only when `resolutionStatus = resolved` |
| provenance | evidence quality, provider, provider version, location | same model as p9 occurrence edges |

### 1.2 Required properties

- Occurrences are **per-specifier**: `import { A, B as C } from "./m"` is two
  occurrences, independently distinguishable, each with its own span.
- Occurrences remain distinguishable when imported names or local aliases
  collide across or within files: identity must include the importing file,
  the statement span, and the specifier span. Two identical specifiers in one
  file stay distinct.
- Unsupported forms (`default`, `namespace`, `import-equals`, dynamic,
  anything outside the allowlist) are **recorded, never silently dropped**,
  and are binding-ineligible.
- Type-only status is recorded but does **not** by itself make an occurrence
  binding-ineligible: `implements` is a type position, so type-only named
  imports are eligible for the interface-impact binding.

### 1.3 Proposed representation (ADR decision required)

The recommended representation follows the accepted p9 occurrence-edge
pattern: one span-anchored, discriminator-hashed **`imports-name` edge per
supported specifier occurrence**, from the importing file node to the
existing module node for the specifier, carrying the properties above
(plus `resolvedTarget` when resolved). Additive; no change to existing
`imports`, `resolves-to`, or export facts. Provider:
`penghou.hetu.tree-sitter.ecmascript-import-occurrences`, version
`imports1.local-v1`, evidence quality `Structural` (syntax) with resolution
outcomes quoted from the resolver.

Alternative representations (binding node per occurrence; properties on
`resolves-to` edges) must be compared in the ADR. The contract requires the
per-occurrence content of §1.1 regardless of representation.

### 1.4 Lifecycle and invalidation

Occurrence facts are single-file facts owned by the importing file's index
unit: editing, deleting, or re-extracting a file replaces its occurrences
atomically under the existing unit-replacement lifecycle. No cross-file unit
is required for fact A alone.

## 2. Fact contract B — export-to-declaration attribution

### 2.1 Required content

For each supported direct named export in a TypeScript source file,
extraction must record the relationship between the existing export entry
(slot `(file, exportedName)`) and the local declaration that the export
names:

| Field | Meaning |
| --- | --- |
| `exportEntry` | existing export entry node identity |
| `declaration` | local declaration node identity (must carry a `SymbolId`) |
| `exportedName` | written public name (post-alias) |
| `localDeclarationName` | declared local name (pre-alias) |
| `exportSpan` | span of the export keyword/name occurrence |
| provenance | evidence quality, provider, provider version, location |

### 2.2 Uniqueness rule (fail closed)

Attribution is emitted **only when the evidence is sufficient to be unique**:

1. The export is a direct named export of a supported declaration form
   (`interface`, `class`, `type alias`, `enum`, named function, named
   file-level variable). Re-exports, star/namespace exports, default exports
   and `export =` are **out of scope** in this milestone and remain
   unattributed.
2. After alias resolution (`export { Local as Exported }` → local name
   `Local`), **exactly one** declaration node in the same file carries that
   local name. Multiple same-named declarations in one file (for example
   merged `interface` + `class`, or overload sets that emit separate
   declaration nodes) make attribution **ambiguous → not emitted**.
3. If no declaration with the local name exists in the file, attribution is
   **not emitted** (missing-declaration case), and the fact set must remain
   positive-only about what exists.

The rule is deliberately narrower than TypeScript semantics (it does not
model overloads, merging, or default exports). A missing attribution is an
explicit unknown downstream, never a false unique claim.

### 2.3 Proposed representation

One additive **`export-declares` edge per unique attribution**, from the
export entry node to the declaration node, with the properties above.
Provider: `penghou.hetu.tree-sitter.ecmascript-export-attribution`, version
`exports2.local-v1`, evidence quality `Structural`. Existing p9 export
entries and `exports` occurrence edges are preserved byte-identically; the
attribution is additive. Collision behavior matches the p9 publisher:
duplicate identity with different facts fails closed.

### 2.4 Lifecycle and invalidation

Attribution facts are single-file facts owned by the exporting file's index
unit, like fact A.

## 3. Binding rule — import occurrence → exported declaration

The consumer binding joins fact A, the accepted relative-v1 resolution, the
accepted p9 export entries, and fact B:

1. **Heritage clause.** A class `C` in file `F` has an explicit
   `implements`/`extends-type` clause with written name `H` at span `S`
   (existing structural heritage fact).
2. **Local binding resolution, precedence order:**
   a. **Import-first.** If `F` has an import occurrence with
      `localName = H` and `form ∈ {named, named-aliased}`:
      - `resolutionStatus` must be `resolved` with target `T` (unique),
        otherwise → *unresolved-module / external / unsupported* unknown;
      - let `N = importedName`.
   b. **Same-file fallback.** Else, if `F` declares exactly one declaration
      named `H`, bind to it (same-file route).
   c. Otherwise → *unresolved-heritage-name* unknown.
   Import-first precedence is deliberate: where an imported binding and a
   local declaration share a name, TypeScript's file-scope binding resolves
   the clause through the import (Q1 fixture G2); the fallback must not
   override a supported import.
3. **Export lookup.** The export entry `(T, N)` must exist. If absent:
   if `T` has no export entry named `N` but has outgoing `resolves-to` edges
   and `F`'s specifier resolved to `T`, classify *re-export-barrier*;
   otherwise *missing-export*. (Distinguishing rule: a module that re-exports
   produces `resolves-to` edges for `export-from` references but publishes no
   export entries for them at p9 — observed on the committed corpus for
   `src/api/index.ts`.)
4. **Attribution.** The entry `(T, N)` must carry exactly one attribution to
   declaration `D` (fact B). Zero attributions → *unattributed-export*
   unknown; multiple → *ambiguous-export* unknown.
5. **Kind check (query-side).** For the interface-impact query, `D` must be
   an interface declaration node matching the query target identity.

A confirmed binding is emitted **only when every step succeeded**. The
binding asserts: "`C`'s heritage clause at `S` is bound, through the import
occurrence at span `I` (specifier resolved under `relative-v1`), to export
entry `(T,N)`, which is attributed to declaration `D`." It does **not**
assert compiler-level type checking, runtime behavior, or that other
implementations do not exist.

### Evidence classification

Confirmed bindings produced by this rule carry a new evidence identity —
provider `penghou.hetu.tree-sitter.ecmascript-symbol-binding`, version
`binding1.local-v1`, quality **`Resolved`** — whose documented analysis
context is exactly the five steps above. Per the accepted evidence model,
`Resolved` means "linked under the documented policy", never compiler or
runtime equivalence. Every binding must expose its full step chain as
provenance (Section 5).

## 4. Declaration identity

No new identity scheme is introduced. Declaration identities remain the
existing path-scoped, owner-derived identities (`docs/ecmascript-symbol-identity.md`).
The heritage-placeholder namespace rule — written heritage names use
declaration-less identities in a separate namespace from declared symbols,
so same-spelling convergence never happens implicitly — remains in force and
is load-bearing for this contract: binding is always an explicit,
evidence-carrying step, never a name collision.

Query input is the existing `CodeNodeId` of an interface declaration node.
Because class/interface declarations are file-scoped, duplicate interface
names in different modules never converge (verified in Q1 scenario G1), and
the query's kind check plus identity comparison prevents name-only
promotion.

## 5. Required provenance for a confirmed binding

Every confirmed implementation result must expose, at minimum:

1. heritage fact: edge id, class node, heritage-target node, span, provider
   `…abi14.p9`, quality `Structural`;
2. import occurrence: edge id, specifier, imported name, local name,
   type-only, statement and specifier spans, `imports1` provider version;
3. module resolution: `resolves-to` edge id, resolution rule
   (`exact`/`extension-substitution`/`directory-index`), policy
   `relative-v1`, `resolver2` provider version, quality `Resolved`;
4. export entry + occurrence: entry node id, occurrence edge id, span,
   `exports1` provider version, quality `Structural`;
5. attribution: `export-declares` edge id, span, `exports2` provider version,
   quality `Structural`;
6. the binding's own edge/fact identity, `binding1` provider version,
   quality `Resolved`, with the five-step chain as its witness path.

Each step must be independently verifiable against the published graph
facts; no step may cite a value that is not itself a stored fact.

## 6. Implementation impact inventory (not authorized by this document)

| Area | Required change | Authorization |
| --- | --- | --- |
| Extraction profile | capture per-specifier import occurrences; attribution; profile revision bump (p9 → p10 or additive providers) | owner approval of this contract + ADR |
| Graph vocabulary | `imports-name`, `export-declares`, binding representation; new property keys | owner approval; additive |
| Persistence | additive edge kinds/properties; no store-schema change expected (p9 added kinds the same way) — must be confirmed against both stores during qualification | owner approval; qualification gate |
| Index identity | plugin/provider version changes invalidate prior indexes; repeat of the accepted p8→p9 migration pattern for p9→new | owner approval; migration rehearsal gate |
| Query service | new bounded consumer query (see consumer-query-contract.md); existing queries untouched | owner approval |
| Parser/grammar | none (pinned grammars unchanged) | — |

## 7. What this contract does not do

- It does not implement anything; it specifies what implementation must be
  approved to build.
- It does not claim compiler, bundler, or runtime equivalence for `Resolved`
  binding evidence.
- It does not resolve re-exports, default exports, namespace imports, bare
  package specifiers, `tsconfig` path aliases, cross-plugin or cross-language
  symbols, or declaration merging across files.
- It does not promise completeness: absence of a binding is not evidence of
  absence of impact; unknowns and coverage statements carry that burden
  explicitly (see consumer-query-contract.md).
