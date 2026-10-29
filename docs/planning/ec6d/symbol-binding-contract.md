# EC-6D symbol-binding contract (R2 revision)

**Status: remediated planning contract for owner review (EC6D-PLAN-R2).**
This document supersedes the R1 proposal at commit `0e1b213` in the sections
listed under "R2 changes". It authorizes no implementation. Any
implementation requires separate owner approval of this contract, of the
representation decision proposal, and of the publication policy proposal.

**R2 changes (summary):**

1. **Lexical-binding rule replaces import-first precedence.** Confirmed
   import binding now requires positive lexical-context evidence; nested
   scope, shadowing, type parameters, ambient/global contexts, recovered
   syntax, duplicate bindings and conflicting bindings fail closed with
   explicit reasons. (Q2 report Q2-01; Q2-04.)
2. **Physical-declaration cardinality.** Export attribution counts physical
   declaration records at module scope before symbol-node deduplication;
   merged, nested and ambient declarations never yield confirmed identity;
   attribution records its unique candidate or its ambiguity/ineligibility
   with all contributors. (Q2 report Q2-02.)
3. **New fact contracts C and E** make heritage contexts and export-side
   module references observable, enabling entry-driven discovery,
   re-export-barrier detection over the real vocabulary, and honest
   not-evaluated coverage. (Q2 report Q2-03, Q2-06.)
4. **Exact identity and provenance**: separate statement / module-specifier /
   binding spans with stated discriminator inputs; source-hash, index-unit
   and publication binding for spans; resolver join or recomputation rules;
   a versioned query-derived witness identity; same-file proof variant;
   corrected provider-version citations (`…abi14.p8` heritage evidence).
   (Q2 report Q2-04, Q2-07.)
5. **Reason reachability** is specified per binding outcome (symbol §3.7);
   reserved reasons are labeled non-emittable in this milestone.

## 0. Problem statement (from Q1/Q2 evidence)

At the accepted EC-6C p9 baseline the graph contains relative module-file
resolution (`resolves-to`), file-scoped heritage placeholders
(`implements`/`inherits` to declaration-less `heritage-target` nodes),
positive-only export entries, and no imported-name binding or
export-to-declaration attribution. Independent review established:

- Name-only matching produces wrong confirmations (alias collision,
  `collision` fixture) and misses aliases (`export-alias`).
- The R1 import-first rule confirmed an imported declaration for
  `nested-shadow`, a valid TypeScript program whose compiler binds the
  heritage name to a function-scope local interface (TypeScript 5.9.3,
  zero diagnostics).
- Symbol-node uniqueness does not establish a unique *physical* declaration
  (`merged`: one node, two physical declarations), and same-file name
  cardinality does not establish visibility (`nested-export`).
- Discovery driven by the target's written name misses target declarations
  exported under aliases and misses barrel consumers of re-export aliases
  (`export-alias`, `barrel`, the committed `BarrelPreview` corpus case).
- Qualified heritage (`implements NS.Widget`) is not emitted by p9 at all,
  so it can neither be confirmed nor reported.

The rule below binds only where every listed condition holds, and otherwise
refuses confirmation with an explicit, machine-readable unknown.

## 1. Fact contracts A, C and E — occurrence and context records

### 1.1 Fact contract A — import occurrences

For **every import occurrence** of a supported or recognized form in a
TypeScript source file, extraction records:

| Field | Meaning | Notes |
| --- | --- | --- |
| `sourceFile` | importing file node identity | existing file node |
| `statementSpan` | UTF-16 span of the whole import statement | one-based, end-exclusive |
| `moduleSpecifierSpan` | UTF-16 span of the module string token | covers the literal token **including its quote characters** |
| `bindingSpan` | UTF-16 span of the written binding token for this occurrence | the imported name token for named forms; the alias token for aliased forms; the local binding token for default/namespace forms; `null` when the form introduces no local binding |
| `moduleSpecifier` | written specifier text | quotes excluded; escape sequences preserved verbatim |
| `importedName` | name imported from the module | `null` for default/namespace/import-equals/dynamic/side-effect forms |
| `localName` | binding introduced into file scope | `null` when the form introduces no local binding |
| `isTypeOnly` | type-only status **of this specifier** | statement-level `import type`, inline `type` modifier, or `import type` statement; never an aggregate flag on a mixed statement |
| `form` | `named`, `named-aliased`, `default`, `namespace`, `import-equals`, `dynamic`, `side-effect`, `unsupported` | enumerated, never guessed |
| `resolutionStatus` | `resolved`, `unresolved`, `external`, `unsupported` | quoted from the resolver records of the same publication (see §1.4) |
| `resolvedTarget` | resolved target file node identity | present only when `resolutionStatus = resolved` |
| provenance | evidence quality, provider, provider version, location, source content hash, index-unit identity | same model as p9 occurrence edges |

### 1.2 Required properties

- Occurrences are **per specifier**: `import { A, B as C } from "./m"` is
  two occurrences; identity inputs are
  `(sourceFile, statementSpan, bindingSpan, form, discriminator)` — the
  binding span distinguishes members that share statement and module
  spans, and repeated identical specifiers on different lines. Two
  occurrences with identical specifier text remain distinct.
- Coordinates are one-based, end-exclusive UTF-16 code units under the
  existing location contract; string spans include the quote characters and
  the text field excludes them (stated once, applied everywhere).
- Occurrences are distinguishable when imported names or local aliases
  collide across or within files; unsupported forms are **recorded, never
  silently dropped**; `localName`/`importedName`/`bindingSpan` are null for
  forms with no local binding.
- Type-only status does **not** by itself make an occurrence
  binding-ineligible: `implements` is a type position, so type-only named
  imports are eligible for the supported binding route.
- Duplicate local names are recorded as written; the binding rule (§3)
  refuses them before selecting any target (`duplicate-local-name`).
- Occurrence facts and their resolution fields are bound to the emitting
  source content hash and index unit; provider/version/location alone does
  not identify a source revision.

### 1.3 Proposed representation (ADR decision)

Recommended: one span-anchored, discriminator-hashed **`imports-name` edge
per supported specifier occurrence**, from the importing file node to the
existing module node for the specifier, carrying the properties above.
Additive; no change to existing `imports`, `resolves-to`, or export facts.
Provider: `penghou.hetu.tree-sitter.ecmascript-import-occurrences`, version
`imports1.local-v1`, evidence quality `Structural` with resolution outcomes
quoted from the resolver. Alternative representations must be compared in
the representation decision proposal; the per-occurrence content of §1.1 is
required regardless of representation.

### 1.4 Lifecycle, resolver join and repeat-index determinism

- Fact A records are single-file facts owned by the importing file's index
  unit; editing or re-extracting a file replaces its occurrences atomically
  under the existing unit-replacement lifecycle.
- `resolutionStatus`/`resolvedTarget` depend on both the importer and the
  **target inventory**. They are recomputed or joined from the resolver
  records of the **same publication**, using the key
  `(sourceFile, statementSpan, raw moduleSpecifier, reference form,
  publication)` — never endpoint files alone. Importer-unit replacement
  alone is insufficient; the query reads resolver facts from the bound
  publication and treats a missing or contradictory resolver counterpart
  for an import-side occurrence as an internal inconsistency (§3.6,
  fail-closed).
- Repeat indexing of unchanged sources must reproduce identical occurrence
  identities; acceptance A15 pins this together with the byte-identical
  checkout condition.

### 1.5 Fact contract C — heritage context records (new)

p9 emits heritage edges only for some clause shapes and drops others before
they become facts. To make every explicit heritage occurrence observable and
to carry the lexical context the binding rule needs, extraction records one
**heritage context record per explicit `implements`/`extends` clause written
in parsed TypeScript syntax** (including clauses p9 currently skips):

| Field | Meaning | Notes |
| --- | --- | --- |
| `sourceFile` | declaring file node identity | |
| `heritageSpan` | span of the written clause expression | one-based, end-exclusive UTF-16; identity input with `sourceFile` and `ownerNode` |
| `ownerNode` | declaration carrying the clause | class or interface node identity |
| `ownerKind` | `class` \| `interface` \| `other` | |
| `heritageKind` | `class-implements` \| `class-extends` \| `interface-extends` \| `other` | distinguishes the supported kind from the rest |
| `writtenForm` | `simple` \| `qualified` \| `expression` | simple = single identifier/type-identifier |
| `writtenText` | verbatim written text | preserved for qualified/expression forms |
| `terminalIdentifier` | final identifier token when unambiguous | used only to decide candidate relevance; never to confirm |
| `ownerScope` | `module` \| `nested` | module = owner contained directly by the file node |
| `ownerTypeParameterNames` | written type-parameter names of the owner | empty list when none |
| `ownerAmbient` | owner declared `declare`/ambient | |
| `fileModuleContext` | `module` \| `script` \| `unknown` | a file is a module when it has top-level import/export syntax |
| `clauseSyntaxStatus` | `well-formed` \| `recovered` | recovered = clause or owner touches error/missing syntax |
| provenance | provider, version, location, source hash, unit | provider `penghou.hetu.tree-sitter.ecmascript-heritage-context`, version `heritage1.local-v1`, quality `Structural` |

Properties: context records are additive single-file facts; existing p9
heritage edges and their evidence versions are untouched (A19). A heritage
occurrence without a context record is binding-ineligible
(`binding-context-unavailable`) — absence of context never falls back to
name matching. Qualified/expression/extends records are emitted even though
p9 emits no relationship edge for them; they are the stored
unsupported-site evidence the query reports.

### 1.6 Fact contract E — export-reference occurrence records (new)

To detect re-export barriers over the real reference vocabulary, to expose
unresolved export sources, and to associate one level of re-export aliases
with a target, extraction records one **export-reference occurrence per
export-side module reference and re-export clause**:

| Field | Meaning | Notes |
| --- | --- | --- |
| `sourceFile` | declaring file node identity | |
| `statementSpan` / `moduleSpecifierSpan` / `moduleSpecifier` | as §1.1 conventions | |
| `form` | `export-from`, `export-type-from`, `export-star`, `export-namespace-from`, `export-equals`, `unsupported` | the export-side forms; never invented |
| `resolutionStatus` | `resolved`, `unresolved`, `external`, `unsupported` | quoted from the resolver records of the same publication |
| `resolvedTarget` | resolved source file node identity | present only when resolved |
| `clauses` | per written name pair: `{ importedName (source-side written name; null for star), exportedName (public written name; null for bare star), clauseSpan }`, canonically ordered | multiple clauses and multiple sources are separate records/witnesses, never first-wins |
| provenance | as §1.1 | provider `penghou.hetu.tree-sitter.ecmascript-module-occurrences`, version `module1.local-v1`, quality `Structural` |

Properties: additive single-file facts; they publish **no symbol origin**
and support **no confirmation** — they exist so that barriers, unresolved
sources and one-hop alias association are observable instead of invisible.
Re-export closure, origin chains beyond one hop, and CommonJS graph
resolution remain out of scope.

## 2. Fact contract B — export-to-declaration attribution (revised)

### 2.1 Required content

For each supported direct named export entry `(file, exportedName)`,
extraction records an attribution record:

| Field | Meaning |
| --- | --- |
| `entryNode` | existing export entry node identity |
| `status` | `attributed` \| `missing-declaration` \| `ambiguous-declarations` \| `ineligible-declaration-context` |
| `localName` | written local name after alias resolution (`export { Local as Exported }` → `Local`) |
| `eligiblePhysicalDeclarationCount` | count of eligible candidate physical declaration records at module scope |
| `candidateDeclarationRecordIds` | all candidate physical declaration record identities contributing to the outcome (canonical order); empty for `missing-declaration` |
| `ineligibleDeclarationRecordIds` | ambient/recovered/unmodeled records excluded from candidacy |
| `declarationRecordId`, `declarationNodeId`, `symbolId` | present only when `status = attributed` |
| `exportSpans[]` | **all** occurrence-edge spans for this entry (one entry may have several written occurrences), canonically ordered, each with its occurrence edge identity |
| provenance | evidence quality, provider, provider version, location, source hash, index unit |

Provider `penghou.hetu.tree-sitter.ecmascript-export-attribution`, version
`exports2.local-v1`, quality `Structural`. Existing p9 export entries and
`exports` occurrence edges are preserved byte-identically; attribution is
additive.

### 2.2 Candidate definition (fail closed)

A **candidate physical declaration** for local name `L` in file `T` is a
physical declaration record in `T` whose written name is `L` and whose
symbol node is **contained directly by the file node** (module scope).
Establishing uniqueness:

1. Count **physical declaration records before symbol-node deduplication**.
   Same-namespace interface merging converges to one node but remains two
   physical declarations → `ambiguous-declarations`. `interface` +
   `class` merging, type/value namespace pairs (`interface Widget` +
   `const Widget`) and overload sets likewise produce multiple records →
   `ambiguous-declarations` with all contributor record ids.
2. Nested declarations (`Hidden.Widget`, classes/interfaces inside
   namespaces or functions) are **never candidates**: module-scope exports
   cannot see them; using them would attribute invisible declarations
   (`nested-export`).
3. Ambient (`declare`), recovered-syntax and unmodeled records are excluded
   from candidacy; if they are the only same-name records the status is
   `ineligible-declaration-context` with their ids recorded.
4. Zero candidates → `missing-declaration`; exactly one eligible candidate
   → `attributed`.
5. The export form allowlist is unchanged from R1: direct named exports of
   supported declaration forms. Re-exports, star/namespace exports, default
   exports and `export =` remain out of scope and produce no attribution
   record beyond their fact E presence.
6. **All-contributors rule:** every written occurrence of the entry must
   contribute; if occurrences conflict (different resolved local names or
   candidate sets), the status is `ambiguous-declarations`, never a
   first-occurrence win. Several public aliases of one unique declaration
   are legitimate separate entries, each attributed to the same declaration
   record with its own occurrence witnesses.

### 2.3 Downstream mapping

`missing-declaration` → unknown `unattributed-export`;
`ambiguous-declarations` → unknown `ambiguous-export`;
`ineligible-declaration-context` → unknown `ineligible-declaration`;
`attributed` → eligible for the binding rule. A missing attribution is an
explicit unknown downstream, never a false unique claim.

## 3. Lexical-binding rule (replaces import-first precedence)

The consumer binding joins fact C (context), fact A (occurrences), the
accepted relative-v1 resolution, the p9 export entries, fact E
(export-side records) and fact B (attribution). No step uses global name
equality, file-level name matching, import-first precedence, first-match
selection, or stable ordering as a substitute for semantic identity. A
deterministic false answer is still incorrect.

### 3.1 Supported confirmed shape

The first milestone confirms only:

- a heritage occurrence whose context record shows
  `heritageKind = class-implements`, `writtenForm = simple`,
  `ownerScope = module`, `ownerAmbient = false`,
  `fileModuleContext = module`, `clauseSyntaxStatus = well-formed`, and no
  owner type parameter named `H`; and
- exactly one eligible visible binding for the written name `H` at module
  scope: either one import occurrence (§3.4) or one module-scope physical
  declaration (§3.5).

`class-extends`, `interface-extends`, qualified and expression heritage are
not confirmable in this milestone; they receive explicit unknowns (§3.7).

### 3.2 Gate outcomes (deterministic precedence)

Evaluate in order; the first matching outcome is the clause's disposition:

1. No context record for the heritage occurrence →
   `binding-context-unavailable`.
2. `clauseSyntaxStatus = recovered` → `recovered-heritage-syntax`.
3. `ownerAmbient = true` or `fileModuleContext ≠ module` →
   `ambient-or-global-context`.
4. `ownerScope = nested` → `nested-scope-heritage`.
5. `heritageKind ∈ {class-extends, interface-extends, other}` and the
   written name is target-associated (§ query contract §3) →
   `heritage-kind-not-supported`.
6. `writtenForm = qualified` and terminal identifier target-associated →
   `qualified-heritage-unresolved`.
7. `writtenForm = expression` and terminal identifier target-associated →
   `unsupported-heritage-form`.
8. `H ∈ ownerTypeParameterNames` → `type-parameter-shadowing`.
9. Any binding-ineligible import form (§1.1 `form` ∈ {default, namespace,
   import-equals, dynamic}) introduces `localName = H` →
   `unsupported-import-form` (the local binding is that import; no other
   candidate may be selected).
10. More than one eligible import occurrence with `localName = H` →
    `duplicate-local-name`.
11. Exactly one eligible import occurrence with `localName = H` **and** at
    least one module-scope physical declaration record named `H` in the
    same file → `conflicting-local-binding`.
12. Exactly one eligible import occurrence with `localName = H` and no
    module-scope declaration named `H` → continue on the import route
    (§3.4).
13. No import occurrence introduces `H`: count all module-scope physical
    declaration records named `H`:
    - exactly one, eligible → continue on the same-file route (§3.5);
    - exactly one, ineligible (ambient/recovered/unmodeled) →
      `ineligible-declaration`;
    - more than one → `ambiguous-local-declaration`;
    - zero → `unresolved-heritage-name`.

### 3.3 Candidate visibility

Only module-scope declarations participate: a heritage reference at module
scope cannot see nested declarations, and nested declarations never shadow
module scopes. An unrelated nested same-name declaration must not create
ambiguity for a module-scope reference, and must not be selected as a
target. The binding is established by the enumerated gates only; there is
no local-first fallback.

### 3.4 Import route

1. The single eligible occurrence's `resolutionStatus` must be `resolved`
   with a unique target `T`; `unresolved` → `unresolved-module`;
   `external` → `external-module`; `unsupported` →
   `unsupported-import-form`.
2. Let `N = importedName`. The target `T` must publish an export entry for
   `N`:
   - no entry, and `T` has at least one fact E record with `form ∈
     {export-from, export-type-from, export-star, export-namespace-from}`
     (any resolution status) → `re-export-barrier`, carrying all matching
     records as separate witnesses;
   - no entry, and no such record (unsupported export forms alone are not a
     barrier) → `missing-export`;
   - entry present → continue.
3. The entry's attribution record (§2) must have `status = attributed`;
   `missing-declaration` → `unattributed-export`;
   `ambiguous-declarations` → `ambiguous-export`;
   `ineligible-declaration-context` → `ineligible-declaration`.
4. Let `D` be the attributed physical declaration record and `DN` its
   symbol node. For the interface-impact query, `DN` must be the queried
   interface declaration node and `D` its unique physical declaration. If
   the chain completes to a **different declaration** (another module's
   same-name interface, another same-file declaration), the candidate is
   **omitted** from the target's result: it concerns a different
   declaration and is neither confirmed nor an unknown for this target.
5. Every step above must cite stored facts; a missing fact in a purported
   completed chain is an internal inconsistency (fail closed), not a
   reason to guess.

### 3.5 Same-file route (short proof)

When §3.2 step 13 selects the same-file route, confirmation requires only:
the context record, the unique module-scope physical declaration record `D`
named `H`, and `D`'s symbol node equal to the queried interface node. The
same-file proof has **no import, resolution, export or attribution steps**,
and the referenced interface **need not be exported**. The result records
`bindingRoute = same-file` with exactly those witness steps.

If the unique eligible module-scope record `D` instead belongs to a
**different declaration** (including a const/field/alias or another kind),
omit this candidate from the queried target's result: neither confirmed nor
unknown for this target, as on the import route (§3.4 step 4). Mere same-file
presence never proves identity. This disposition applies only after the
§3.2 gates: multiple physical records still yield
`ambiguous-local-declaration`; a same-name declaration in another scope is
not a module-scope candidate (§3.3); nested or otherwise unsupported lexical
contexts retain their gate reason. The positive short proof is unchanged.
(EC6D-OWNER-01 / Q3-F03.)

### 3.6 Evidence classification and witness identity

- Confirmed import bindings carry a **versioned query-derived witness**
  with evidence identity `binding1.local-v1` and quality `Resolved`:
  `WitnessId = "binding1.local-v1:" + SHA-256 over the canonically ordered
  tuple of (heritage fact id, heritage context record id, import occurrence
  id, resolver edge id, export entry id, export occurrence edge id(s),
  attribution record id, declaration record id, declaration node id) +
  policy string`. Same-file witnesses use the ordered subset (heritage,
  context, declaration record, declaration node).
- The witness is computed at query time from stored fact identities, or
  persisted with explicit dependency ownership per the representation
  decision; it is never an invented stored edge ID. A missing constituent
  fact in an asserted witness is a fail-closed integrity error.
- `Resolved` keeps the accepted meaning: resolved by the named provider
  under the documented policy, never compiler or runtime equivalence.
  Class implementations may be confirmed through several distinct witness
  paths between the same class and target; paths are preserved separately,
  never flattened into one mixture.

### 3.7 Reason reachability

Every emitted unknown uses the closed reason list in the query contract
§3.4, which records for each reason its reachable condition and a fixture.
`unsupported-export-form` and `unsupported-declaration-form` are **reserved
and non-emittable in this milestone**: the name-based channels cannot
match default/namespace entries, and non-interface query targets are input
errors. They must not be emitted, and their absence is not a coverage gap.

## 4. Declaration identity

No new identity scheme. Declaration identities remain the existing
path-scoped, owner-derived identities; the heritage-placeholder namespace
rule (profile revision 5) remains load-bearing: written heritage names use
declaration-less identities, so same-spelling convergence never happens
implicitly. R2 adds only the explicit **physical declaration record** as
the unit of candidacy and confirmation, alongside its symbol node and
`SymbolId`, with module-scope containment as the visibility condition.
Duplicate interface names in different modules never converge; same-name
merging inside one module converges at node level but remains ambiguous at
record level and is therefore never confirmed.

## 5. Required provenance for a confirmed binding

Every confirmed result exposes, at minimum, the ordered witness steps:

1. heritage fact: edge id, owner node, heritage-target node, span,
   provider `penghou.hetu.tree-sitter.typescript`, quality `Structural`,
   evidence version `0.2.0-preview.6+v0.23.2.abi14.p8` — existing
   structural facts **intentionally retain the p8 evidence version at the
   p9 profile** so a p8→p9 re-index is byte-identical; the plugin/session
   origin reports `p9`. Cite the fact's own evidence version, never the
   profile revision, for heritage evidence;
2. heritage context: record id, heritageKind, written form, ownerScope,
   module context, span, provider `…ecmascript-heritage-context`,
   `heritage1.local-v1`, quality `Structural`;
3. import occurrence (import route): record id, statement/module/binding
   spans, imported and local names, type-only status, `imports1.local-v1`;
4. module resolution: resolver edge id, rule
   (`exact`/`extension-substitution`/`directory-index`), policy
   `relative-v1`, `resolver2` provider version, quality `Resolved`, joined
   by `(sourceFile, statementSpan, raw specifier, reference form,
   publication)`;
5. export entry and occurrences: entry id, occurrence edge ids and spans,
   `exports1` provider version, quality `Structural`;
6. attribution: record id, status, declaration record id and node id,
   `exports2` provider version, quality `Structural`;
7. same-file route: steps 1, 2 and the declaration record only (no
   import/export/resolution steps);
8. the witness identity itself (§3.6), with its five (or three) step chain
   and policy string.

No step may cite a value that is not itself a stored fact; no confirmed
entry may omit a step.

## 6. Implementation impact inventory (not authorized)

| Area | Required change | Authorization |
| --- | --- | --- |
| Extraction profile | facts A, C and E; attribution statuses; profile revision bump | owner approval + representation decision |
| Graph vocabulary | `imports-name`, `heritage-context`, `export-reference`, `export-declares`/attribution records; additive properties | owner approval; additive |
| Persistence | additive records; confirm both stores during qualification | qualification gate |
| Index identity | plugin/provider versions invalidate prior indexes; repeat the accepted migration pattern | migration rehearsal gate |
| Query service | lexical-binding rule, entry-driven discovery, reasons, coverage, budgets, witness | owner approval |
| Parser/grammar | none (pinned grammars unchanged) | — |

## 7. What this contract does not do

- No implementation; specification only.
- No compiler, bundler or runtime equivalence claim.
- No re-export closure (one-hop alias association only, for candidate
  relevance and barrier reporting — never confirmation); no default,
  namespace or `export =` binding; no bare-package or config-alias
  resolution; no cross-plugin or cross-language symbols; no declaration
  merging as confirmation; no nested-scope binding; no transitive impact.
- No completeness promise: absence of a binding is not evidence of absence
  of impact; unknowns, coverage counters and the not-evaluated frontier
  carry that burden explicitly (see consumer-query contract).
