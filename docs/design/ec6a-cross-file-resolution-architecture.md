# EC-6A: Cross-file resolution architecture (TypeScript / JavaScript)

**Status:** architecture design for EC-6. **All contracts, identities, kinds, and
properties in this document are proposals. None of them are implemented.** EC-6
implementation is not authorized by this document.
**Baseline:** `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (EC6-A0, "Qualified
pre-resolution baseline"), tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018`.
Citations use repository paths at that baseline.
**Governing principle:** an observed name or syntactic relationship is not proof
of semantic binding. No resolver fact may be produced from display-name
equality alone.

Related EC-6A documents:

- [ec6a-adversarial-cases.md](ec6a-adversarial-cases.md)
- [ec6a-implementation-roadmap.md](ec6a-implementation-roadmap.md)
- [ec6a-decision-register.md](ec6a-decision-register.md)

---

## 1. Problem statement

EC-5 makes Hetu answer "what declarations exist" for TypeScript/JavaScript
files with stable identities and honest structural evidence. EC-6 must make it
answer cross-file questions without inventing semantic certainty:

- Which source file does a written import specifier refer to?
- Which declarations does a module export, under which names?
- Which declaration does an imported local name denote?
- Which facts must be rebuilt when a file, an export, a module set, or a
  resolution policy changes?
- How do consumers distinguish an observed import, a verified module
  dependency, a resolved declaration binding, and an ambiguity?

Today the graph records imports only as spelling-keyed observations
(`imports` edge to a pathless placeholder node,
`src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs:426-431`), and the
EC-4 real-repository review measured the consequence on a real repository:
`./paths` and `../lib/paths` produce two unlinked placeholder nodes for one
physical module, imports never link to file nodes, and cross-file dependency
questions "require application-side resolution today"
(`review/ec4-real-repository-qualification:docs/reviews/ec4-real-repository-qualification.md`,
finding 5 and consumer limitation 3). Its recommended roadmap item 2 is:
"Resolve intra-repository imports to file nodes (or emit a second,
resolution-evidenced edge) while keeping the spelling observation."

## 2. Scope and non-goals

**In scope for EC-6B–EC-6F (proposed milestones, see roadmap):**

- Deterministic, offline resolution of **relative** ECMAScript import
  specifiers (`./x`, `../y`) to indexed source files under an explicitly
  versioned policy.
- Export-surface representation: direct exports, local export lists and
  aliases, named/default/namespace re-exports, and `export *` closure.
- Import-to-export binding for named, default, and namespace imports,
  including explicit type-only syntax.
- Evidence and provenance for resolved facts; conservative behavior on
  ambiguity; coverage accounting.
- Deterministic incremental publication and stale-fact cleanup through the
  existing unit/publication machinery.
- Real-repository qualification.

**Explicit non-goals (this milestone series):**

- `node_modules`, package exports maps, conditional exports, or any network or
  installed-dependency resolution. Bare specifiers remain unresolved by
  policy.
- `tsconfig.json` `paths`/`baseUrl` and module-resolution-mode emulation.
- CommonJS export surfaces (`module.exports`, `exports.x`) and CommonJS
  bindings.
- `.astro`, `.vue`, `.svelte`, or any non-ECMAScript module format.
- Cross-plugin resolution between the TypeScript and JavaScript plugins
  (different plugins, concurrent sessions; see §7.4 and D-18).
- Full TypeScript type-level semantics (type relations, contextual typing,
  conditional resolution).
- Any change to existing symbol, declaration, file, or heritage identities.
- Changing `invokes-name` or emitting `calls`/`references`.

## 3. Verified baseline (what the architecture must build on)

### 3.1 Identities and kinds that remain unchanged

| Fact | Identity | Source |
| --- | --- | --- |
| File node | `{language}:file:{sha256(path)}` | `src/Penghou.Hetu.TreeSitter/TreeSitterExtractionSession.cs:110` |
| Per-file index unit | same string as the file node id | `TreeSitterExtractionSession.cs:212-220` |
| Declared symbol | `{language}:{sha256(discriminator\n[path\n]name)}` | `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs:334-336, 637-641` |
| Import placeholder | `{language}:import:{sha256(rawSpecifier)}`, kind `type`, no `SymbolId`, no properties | `EcmaScriptExtractionProfile.cs:426-431` |
| `imports` edge | file/owner → placeholder, `Structural` | `EcmaScriptExtractionProfile.cs:417-431` |

Node kinds: `repository`, `project`, `package`, `file`, `namespace`, `type`,
`interface`, `callable`, `property`, `field`, `parameter`
(`src/Penghou.Hetu.Abstractions/CodeGraphKinds.cs:15-28`). Edge kinds:
`contains`, `declares`, `references`, `calls`, `invokes-name`, `implements`,
`inherits`, `imports`, `depends-on`, `returns`, `accepts`, `exercised-by`
(`CodeGraphKinds.cs:42-61`). Both records are open, validated string
records — additive kinds are supported (`CodeGraphKinds.cs:4-12, 30-39`).

### 3.2 Lifecycle invariants the design must respect

- **Plugins own units; units are atomically replaceable.** `CodeFactOrigin`
  names (repository, plugin, plugin version, run, unit) and an optional
  source path/hash pair (`src/Penghou.Hetu.Abstractions/CodeGraphBatch.cs:4-47`).
  A completed unit is one `CodeIndexUnitReplacement`
  (`src/Penghou.Hetu/CodeGraphStoreModels.cs:142-168`).
- **The planner invalidates per (plugin, path) manifest, never per unit.**
  Unchanged means same plugin version and same source hash; a version change
  re-extracts every source of that plugin
  (`src/Penghou.Hetu/CodeIndexPlanner.cs:204-208`). A plugin executes iff at
  least one of its plan items is not `Unchanged`; when it executes it receives
  **all** its current sources, including unchanged ones
  (`src/Penghou.Hetu/CodeIndexingLifecycle.cs:359-363, 816-836`).
- **The Tree-sitter session re-extracts every source it owns per execution**
  (`TreeSitterExtractionSession.cs:74-161`), so "incremental" granularity for
  Tree-sitter today is "plugin executes or not". Deleted paths are reported as
  obsolete file units (`TreeSitterExtractionSession.cs:163-169`).
- **Endpoint integrity is validated when a unit is staged**: the materialized
  prospective graph (previous publication + staged changes) must contain every
  edge endpoint, and a deletion that would dangle other units' edges is
  rejected (`src/Penghou.Hetu/InMemoryCodeGraphStore.cs:886-929`).
  Consequently a fact producer may only target nodes in earlier-staged units
  of the same run or in the previous publication.
- **Publication is atomic and run-scoped.** Failed runs keep the prior graph
  and source state (`src/Penghou.Hetu/ICodeGraphStore.cs:27-35`); an unchanged
  reindex is a no-op that returns the previous publication
  (`CodeIndexingLifecycle.cs:364-384`).
- **Cross-unit edges already work when ordering is guaranteed.** The C#
  plugin emits per-project units in dependency order and resolves across
  projects in one session (`src/Penghou.Hetu.CSharp/CSharpCodeGraphPlugin.Session.cs:268-299`).
  That is the precedent this design reuses.
- **Coverage accounting is strict.** `CodeRelationshipCoverage` requires
  `EdgesEmitted == InternalTargets + CrossProjectTargets`, non-produced states
  cannot report counts, and each produced kind appears exactly once
  (`src/Penghou.Hetu.Abstractions/CodeGraphExtraction.cs:193-220, 308-318`).
- **Evidence semantics are fixed.** `Structural` = follows from syntax, not
  semantically resolved; `Resolved` = resolved by the named provider in its
  analysis context; deterministic qualities cannot carry confidence
  (`src/Penghou.Hetu.Abstractions/CodeEvidence.cs:7-47, 67-72`). Only `calls`
  requires `Resolved` today (`src/Penghou.Hetu/CodeGraphBatchValidator.cs:86-97`).

### 3.3 What the query layer gives consumers today

- Exact-name lookup returns all candidates ordered by node id and never picks
  one (`src/Penghou.Hetu/CodeGraphQueryService.cs:68-81`); `SymbolId` presence
  separates declarations from candidates
  (`docs/consumer-guidance-qualified-name-lookup.md:11-44, 70-77`).
- Traversals are kind- and evidence-filterable; presets are closed sets and
  exclude `imports` (`src/Penghou.Hetu/CodeGraphTraversalPresets.cs:10-23`).
- Evidence filters apply to every traversed edge, a documented limitation for
  mixed-evidence paths (`docs/evidence-quality-and-provider-strategy.md:721-756`).
- Query envelopes carry per-fact contributors (plugin/version/run/unit)
  (`CodeGraphStoreModels.cs:315-341`).

### 3.4 What does not exist

No module node kind; no export edge; no binding edge; no resolution phase; no
unit-level dependency metadata; no query convenience for modules; the
Tree-sitter session ignores `PreviousIndexUnits` and `Settings`
(`TreeSitterExtractionSession.cs` uses only `Sources` and `Changes`).

## 4. Architectural alternatives

Four placements were evaluated for the resolution work.

| | A. Extract-time (per file) | B. Core resolution phase | C. Separate resolver plugin | D. Plugin-embedded resolver pass (recommended) |
| --- | --- | --- | --- | --- |
| Where logic lives | inside per-file profile | new core stage between extraction and completion | new plugin role/hook in core registry | after per-file extraction, inside the owning plugin session |
| Cross-file visibility | none (per-file instance) | full staged graph | full repository sources | full source set of that plugin (all its files, already materialized) |
| Ownership | per-file units | needs new ownership model | new plugin identity/units | existing plugin owns a resolution unit |
| Ordering safety | n/a | guaranteed by phase ordering | needs new scheduling guarantees | guaranteed: file units are emitted before the resolution unit in one session |
| Invalidation | impossible without new dependency engine (target changes cannot invalidate the importer's unit) | core-managed dependency tracking required | same problem unless core tracks scope hashes | inherited: plugin executes on any input change; resolution unit is fully rebuilt |
| Core changes | none | new phase, new contracts, service changes | new plugin role, registry and manifest changes | none (internal session/profile extension) |
| Language future-proofing | poor | good (generic) | good (generic) | good seam: optional per-language resolver hook in the shared Tree-sitter session; resolver core is parser-independent |
| Correctness risk | high (stale/missing bindings) | medium (new engine surface) | medium | low (reuses proven unit lifecycle) |
| Verdict | rejected | deferred (D-01) | deferred (D-01) | **recommended** |

**Recommendation.** Implement EC-6B–EC-6F as an optional **resolution pass
inside the Tree-sitter extraction session**, contributed by the language
plugin, with three properties that make it compatible with every existing
guarantee:

1. It runs **after all per-file units of the same session have been emitted**,
   so every endpoint it targets is already staged in the same run.
2. It emits **edges only** — no new nodes, no declarations — into one
   **stable resolution unit** per language. All targets are existing file
   nodes and symbol nodes.
3. It is a **pure function** of the session's inputs (per-file module shapes,
   file inventory, declared symbols, policy version), so full replacement of
   the resolution unit is always correct and no dependency tracking is needed
   for the first milestones.

The pass is factored as a generic seam in the shared Tree-sitter session
(`ITreeSitterModuleResolver` — PROPOSED, internal) plus an ECMAScript
implementation (`EcmaScriptModuleResolver` — PROPOSED). A future language
implements the same internal seam; a future core phase (alternative B) can
lift the same resolver core without changing graph identities. See D-01 in
the [decision register](ec6a-decision-register.md).

## 5. Architecture overview (proposed)

```text
CodeIndexingService
  plan (per plugin/path manifest; version + hash invalidation)
    └─ plugin executes? ──no──► previous publication reused unchanged
         │ yes
         ▼
  TreeSitterExtractionSession (per plugin, e.g. typescript)
    1. per-file loop (existing, unchanged output):
         parse → facts → file unit (nodes/declarations/edges) → staged
         + CAPTURE module shape per file (PROPOSED, in-memory only)
    2. resolution pass (PROPOSED, after the loop):
         inventory  = decodable files {path → fileNodeId, hash, shapes}
         symbolsOf  = per-file symbol-bearing nodes from step 1
         resolver.Resolve(inventory, symbolsOf) → resolver edges + counts
    3. emit resolution unit "{language}:modules" (edges only) → staged
    4. obsolete deleted file units (existing) + coverage/diagnostics
         ▼
  store completion (existing): atomic publication, endpoint validation,
  failed-run retention
         ▼
  queries (existing API): kind + evidence traversals over the new edges
```

Nothing in the diagram changes core code. The only new graph vocabulary is
three proposed edge kinds and their properties (§8). No new node kinds, no
new declarations, no identity changes.

### 5.1 Module shape capture (PROPOSED, in-memory)

The per-file profile already visits every import/export syntax node. It will
additionally capture, for the session (never as graph facts):

- **Import sites:** raw specifier, statement form (`import`, `import type`,
  `require`, re-export-from forms), explicit type-only flag, binding list
  (kind named/default/namespace; imported name; local name), statement span,
  per-binding span.
- **Export sites:** form (direct declaration/declarator, local list, named
  re-export, namespace re-export, star re-export, default declaration,
  default identifier, unsupported expression/`export =`), exported name,
  local name, source specifier, **captured declaration node ids for direct
  exports** (avoids any name lookup for the common case), explicit type-only
  flag, span.

Capturing declaration node ids at walk time is what lets direct exports and
binding avoid display-name matching entirely. Alternative (re-parsing in the
resolver) is rejected: double cost, duplicated grammar knowledge (D-alt in
register).

## 6. Module resolution policy (PROPOSED)

### 6.1 Module identity

**A module is a source file in the graph.** Module identity = the existing
file node id `{language}:file:{sha256(path)}`; the module name is the
repository-relative path stored in the file node's `QualifiedName`
(`TreeSitterExtractionSession.cs:110-120`). No `module` node kind is
introduced (D-02).

Consequences:

- Module dependency edges target file nodes that already exist and are
  already queryable (`GetDeclarationsInFileAsync` et al. use them).
- External or unresolved modules have no node; they are never fabricated.
- A future need for non-file module boundaries (package roots, ambient
  namespaces) is a separate design and not pre-committed.

### 6.2 Policy `relative-v1` (PROPOSED)

Only **relative specifiers** are resolved. A specifier qualifies when it
starts with `./` or `../` after trimming nothing (exact written text is always
preserved in the `specifier` property).

Algorithm (deterministic, ordered; first match wins):

```text
ResolveModule(importerPath, specifier):
  joined = NormalizeJoin(dirname(importerPath), specifier)   # POSIX segments
  if joined escapes repository root → Unresolved(invalid)
  if specifier is "", absolute ("/…"), bare, URL, or "node:"-prefixed
      → External/Unsupported (no edge)
  if inventory contains joined                       → (joined, exact)
  if joined has no file extension:
      for ext in policyExtensions(language):         # ordered list below
          if inventory contains joined + ext         → (joined+ext, extension-substitution)
      for ext in policyExtensions(language):
          if inventory contains joined + "/index" + ext → (joined+"/index"+ext, directory-index)
  → Unresolved(module-not-found)
```

- `policyExtensions("typescript")` = `.ts`, `.tsx`, `.mts`, `.cts`;
  `policyExtensions("javascript")` = `.js`, `.mjs`, `.cjs`, `.jsx`.
- `inventory` is the session's decodable source set (the only files that
  emitted a file node in this run). A specifier that names a file outside the
  plugin's inventory is `module-not-found` for that resolver, even when the
  file exists in the repository; this is honest within scope and is the
  documented cost of per-plugin resolution (D-18).
- Path comparison is ordinal (case-sensitive), matching
  `ContractValue.RelativePath` normalization of source paths.
- Known deliberate deviations from TypeScript's own resolver are listed in
  §6.4 and qualified per milestone.

### 6.3 Resolution outcomes

| Outcome | Meaning | Graph effect | Coverage |
| --- | --- | --- | --- |
| Resolved | Exactly one candidate matched the ordered policy | `resolves-to` edge | emitted |
| Unresolved | Relative specifier, no inventory match, or path escapes root | none | `unresolved-targets` (or `unsupported` for escape/absolute) |
| External | Bare/URL/`node:` specifier | none | `external-targets` |
| Unsupported | Dynamic `import()`, template `require`, `import x = require()`, `export =`, unsupported extensions (e.g. `.astro`, `.json`) | none | `unsupported-targets` |

No resolution outcome is ever guessed. Absence of a `resolves-to` edge means
"not verified under the active policy", never "no dependency exists" — this
sentence must appear in consumer documentation (D-20).

### 6.4 Documented policy boundaries

- `.js` specifiers that would map to `.ts` sources (`import './x.js'` where
  only `x.ts` exists) are **not** resolved by `relative-v1`; deferred to a
  later policy level with its own fixtures (D-08).
- `tsconfig.json` (`paths`, `baseUrl`, `moduleResolution` modes) is out of
  policy. Resolving them later requires explicit opt-in, config parsing,
  config identity in provenance, and config-hash invalidation (D-09).
- Directory specifiers resolve only through `index` files; `package.json`
  fields are never read.
- The resolver never reads the filesystem, environment, or network: it
  operates on the session's already-materialized sources (offline, §12).

## 7. Cross-file facts (PROPOSED graph contract)

### 7.1 Proposed edge kinds

Additive constants on `CodeEdgeKinds` (`CodeGraphKinds.cs`):

| Constant | Value | Meaning | Source | Target | Evidence |
| --- | --- | --- | --- | --- | --- |
| `ResolvesTo` | `resolves-to` | A written module specifier was resolved to an indexed module under a declared policy | importing/re-exporting **file node** | imported **file node** | `Resolved` |
| `Exports` | `exports` | Module exports a name backed by a declared entity | exporting **file node** | declared **symbol node** (named/default) or **file node** (namespace re-export) | `Resolved` |
| `Binds` | `binds` | A written import binding denotes a specific export | importing **file node** | exported **symbol node** (named/default) or **file node** (namespace import) | `Resolved` |

Edge ids follow the existing formula
(`{language}:{sha256(kind\nsource\ntarget\ndiscriminator\nspan)}`,
`EcmaScriptExtractionProfile.cs:1095-1096`); the discriminator encodes the
written name and form (for example `resolves-to:import`, `exports:named:default`,
`binds:named:a->b`) so distinct written facts never collide while repeated
extraction of identical input reproduces identical ids.

Why separate kinds instead of upgrading `imports`:
`imports` is an existing, documented structural observation
(`docs/language-feature-matrix.md:117`) and its identity/edges are already
published; changing its meaning or target would be a fact migration. The
EC-4 recommendation ("emit a second, resolution-evidenced edge while keeping
the spelling observation") is adopted verbatim. `depends-on` is not reused:
its documented scope is project/package dependencies
(`docs/language-feature-matrix.md:113`).

### 7.2 Proposed property keys

Additive constants on `CodePropertyKeys`:

| Constant | Key | Applies to | Values / notes |
| --- | --- | --- | --- |
| `Specifier` | `specifier` | `resolves-to`, `binds` | Raw written specifier text, bounded (≤256 chars); never normalized away |
| `ModuleReference` | `module-reference` | `resolves-to` | `import`, `import-type`, `require`, `export-from`, `export-type-from`, `export-star`, `export-namespace-from` |
| `ResolutionPolicy` | `resolution-policy` | `resolves-to` | `relative-v1` initially |
| `ResolutionRule` | `resolution-rule` | `resolves-to` | `exact`, `extension-substitution`, `directory-index` |
| `ExportedName` | `exported-name` | `exports` | Written exported name (`default` for default exports) |
| `ExportKind` | `export-kind` | `exports` | `named`, `default`, `namespace` |
| `ExportVia` | `export-via` | `exports` | `direct`, `local`, `re-export`, `star` |
| `ViaModule` | `via-module` | `exports` | Repository-relative path of the directly written re-export/star module (omitted for direct/local) |
| `ImportKind` | `import-kind` | `binds` | `named`, `default`, `namespace` |
| `ImportedName` | `imported-name` | `binds` | Name in the target module (omitted for namespace) |
| `LocalName` | `local-name` | `binds` | Written local binding |
| `TypeOnly` | `type-only` | all three | Present as `true` only when the written syntax is explicitly type-only (`import type`, `export type … from`, `type` specifier modifier); absent otherwise |

Property values are bounded by existing limits (≤64 properties per fact,
≤32,768 text characters, `CodeGraphBatch.cs:95-141`). Unknown/extra keys are
rejected by neither validator, but new keys must be added to
`CodePropertyKeys` so consumers have a stable vocabulary.

### 7.3 Export representation rules (PROPOSED)

| Written form | `exports` edge | Notes |
| --- | --- | --- |
| `export const/let/var a` (each declarator) | file → field symbol; `export-via=direct`; `exported-name=a` | Target node id captured during the walk (no name lookup). The `modifiers` `export` token remains as-is |
| `export class C`, `export interface I`, `export type T`, `export enum E`, `export namespace N`, `export function f` | file → declared symbol; `direct` | Target captured during the walk |
| `export default function f` / `export default class C` | file → declared symbol; `export-kind=default`; `exported-name=default` | Only named forms |
| `export default <identifier>` | file → unique local declaration of that identifier; `default` | Unique-match rule; zero/one/many handled as unresolved/ambiguous |
| `export { a, b as c }` (no source) | file → unique local declaration of `a`/`b`; `export-via=local`; `exported-name=a`/`c` | Unique-match only. A local name that denotes an imported binding is `unsupported` initially (D-06) |
| `export { a as b } from './m'` | `resolves-to` file→M plus `exports` file→final symbol; `export-via=re-export`; `via-module` = resolved path of `./m` | Chain followed recursively; the target is the final declaration |
| `export * from './m'` | `resolves-to` file→M plus one `exports` edge per name visible from M (excluding `default`, excluding locally shadowed names); `export-via=star`; `via-module` | Conflicts produce no edge for the conflicting name (ambiguous) |
| `export * as ns from './m'` | `resolves-to` file→M plus `exports` file→**file node M**; `export-kind=namespace`; `exported-name=ns` | Target is the module itself |
| `export default <expression>` (anonymous) | none | No declaration identity exists; `unsupported` (D-11) |
| `export =`, `module.exports`, `exports.x` | none | `unsupported` for EC-6B–E (D-10) |

Export tables are computed per module and memoized per resolution run;
re-export and star traversal uses a visited set so cycles terminate
(§11). "Unique local declaration" means exactly one symbol-bearing node in
that file whose `Name` equals the written local name; a function-valued
variable legitimately has two projections (`field` + `callable`) and is
therefore ambiguous under this rule (conservative; D-05).

### 7.4 Import binding rules (PROPOSED)

| Written form | Facts |
| --- | --- |
| `import './m'` | `resolves-to` only (`module-reference=import`) |
| `import type { A } from './m'` | `resolves-to` with `type-only=true`; per-binding `binds` when A resolves and is not a value-only mismatch |
| `import { a, b as c } from './m'` | `resolves-to`; per binding `binds` file→final symbol with `imported-name`/`local-name` |
| `import d from './m'` | `resolves-to`; `binds` to the module's `default` export when uniquely defined |
| `import * as ns from './m'` | `resolves-to`; `binds` file→**file node M** with `import-kind=namespace`, `local-name=ns` |
| `const x = require('./m')` (top level or nested) | `resolves-to` only (`module-reference=require`); CommonJS bindings unsupported (D-10) |
| dynamic `import('./m')`, template `require(\`./${x}\`)`, `import x = require('./m')` | none; `unsupported` counts |

Binding rules:

- A binding edge is emitted **only** when the module resolved under policy and
  the target module's export table contains exactly one binding for the
  imported name (after star/re-export closure and shadowing rules).
- Type/value mismatch: a value import of an export marked `type-only=true`
  emits no `binds` edge (counted unresolved). A type-only import of a value
  export is allowed (TypeScript erases the value usage; the symbol binding is
  still true) and carries `type-only=true`.
- Duplicate local names, shadowing, and scope nesting are not modeled; each
  written binding is a separate edge with its own location. Consumers treat
  `binds` as "this written binding denotes this export", not as a full scope
  model.
- Binding never uses display-name equality with anything outside the resolved
  module's export table.

### 7.5 What consumers gain

For the first time, these questions are answerable from the graph alone
(patterns use the existing public API; no new query methods required):

| Question | Traversal (existing API) |
| --- | --- |
| What modules does file F depend on? | outgoing `[resolves-to]` from F's file node |
| Who depends on module M? | incoming `[resolves-to]` to M's file node |
| What does module M export? | outgoing `[exports]` from M's file node |
| Which module exports symbol S, under what name? | incoming `[exports]` to S |
| Where is F's imported name `b` bound? | outgoing `[binds]` from F, filter properties `local-name` client-side |
| Which files import symbol S? | incoming `[binds]` to S |
| Verified-only view | filter `EvidenceQualities = [Resolved]` on these traversals |

The four consumer buckets required by the task map cleanly onto existing
concepts:

1. **Structural observation** — `imports`, `invokes-name`, heritage edges and
   modifiers (`Structural`); written syntax only.
2. **Verified module dependency** — `resolves-to` (`Resolved`, policy-stamped).
3. **Resolved declaration** — `binds`/`exports` targets are `SymbolId`-bearing
   nodes; declarations via `FindDeclarationsAsync`.
4. **Ambiguous candidates** — no edge is emitted; extraction coverage counts
   report the sites. Absence is explicitly not a negative claim (D-20).

No presets change in EC-6B–E (`ImpactSet`, `Dependencies`, and `Calls` stay
closed sets). Whether module facts should enter impact semantics later is
D-15.

## 8. Evidence and provenance (PROPOSED)

- **Quality:** resolver facts are `Resolved`. Rationale: the resolver is a
  named, versioned provider that applies the language's module-resolution
  concept under a declared policy to inputs it fully possesses; `Resolved`
  means "resolved by the named provider in its analysis context, not
  universally exact" (`CodeEvidence.cs:15-20`). The claim is bounded: only
  when the policy yields exactly one candidate and the binding is uniquely
  determined. The alternative (`Derived`) and its tradeoffs are recorded in
  D-03 for the independent design review; whichever is chosen must hold for
  all three kinds and must be enforced by fixtures.
- **Provider:** proposed resolver identity
  `penghou.hetu.tree-sitter.ecmascript-module-resolver`, distinct from the
  extraction plugin id, so consumers can separate "who parsed" from "who
  resolved" (`CodeEvidence.Provider`/`ProviderVersion`).
- **Provider version:** `{packageVersion}+resolver{ResolverRevision}.{policy}`
  (for example `0.2.0-preview.6+resolver1.relative-v1`). Any change to
  emitted resolver facts — including policy changes — must change this
  string; because the effective plugin version already drives the planner
  and includes the profile revision, resolver-fact changes ride the same
  invalidation axis (D-16).
- **Justification:** the edge properties of §7.2 are the justification
  record: raw specifier, policy, rule, written names, forms, and type-only
  flags. Locations are the exact written spans (statement for `resolves-to`,
  binding/name for `binds`, declaration/specifier statement for `exports`).
- **Contributor provenance:** the resolution unit's `CodeFactOrigin` carries
  repository, plugin, effective plugin version, run, and unit id
  (`{language}:modules`); query envelopes therefore attribute resolver facts
  to the exact publication and unit.
- **Confidence:** forbidden (deterministic quality,
  `CodeEvidence.cs:67-72`).

## 9. Ownership, invalidation, and publication (PROPOSED)

### 9.1 Resolution unit

- One stable unit per language plugin: id `{language}:modules`
  (for example `typescript:modules`). Stable across runs so replacement
  overwrites rather than accumulates.
- Contains **edges only**. No nodes, no declarations.
- Emitted by the same session that emitted the file units, after the per-file
  loop. The sink serializes per plugin; the store stages the completing unit
  against a prospective graph that already contains all file units of the
  same run, so every endpoint exists at staging time.
- When the plugin executes, the resolution unit is **always** re-emitted,
  including an empty completing batch when there are no sources or no
  resolvable facts — the existing "empty completion clears the previous
  unit" pattern (`TreeSitterExtractionSession.cs:96-107`).
- When the plugin does not execute (every plan item unchanged), the previous
  resolution unit stays published; its inputs are unchanged, so it remains
  correct.

### 9.2 Invalidation matrix

| Change | Planner effect | Resolution effect |
| --- | --- | --- |
| Source content of any file (imports, exports, or anything else) | file `Changed` → plugin executes | full re-resolution; resolution unit replaced |
| File added | `New` → plugin executes | new file in inventory; unit replaced |
| File deleted | `Deleted` → plugin executes | obsolete file unit reported; resolution unit replaced without it; all edges touching it disappear with the replacement |
| File renamed | `Deleted` + `New` | both of the above |
| Plugin/resolver version or profile revision bump | all plugin paths `Changed` | full re-extraction + re-resolution |
| Resolution policy change | must bump provider version (D-16) | same as version bump |
| No change at all | plugin does not execute; no-op publication reused | previous unit retained |
| Another plugin changes (for example JavaScript) | TypeScript plugin does not execute | TypeScript resolution unit retained; cross-plugin facts are out of scope (D-18) |

Correctness argument: the unit's output is a pure function of (per-file
shapes, inventory, symbols, policy/version). Any input change classifies at
least one plan item of the owning plugin as not-unchanged, forcing execution
and replacement. Any plugin execution fully recomputes the unit. Therefore no
stale cross-file fact can survive, with no dependence on dependency tracking.

### 9.3 Failure, interruption, partial results, and bounds

- A failed or cancelled run stages nothing visible; the previous resolution
  unit (and everything else) is retained
  (`ICodeGraphStore.cs:27-35`; conformance "failed-run-discards-staged-graph").
- Endpoint validation failures reject the containing unit batch, failing the
  run loudly — never producing partial publication.
- Parse-error files: existing behavior marks produced kinds `Partial` and
  preserves valid sibling facts (`TreeSitterExtractionSession.cs:122-126`).
  Resolver kinds follow: `Partial` when any source had a parse error or was
  undecodable, `Produced` otherwise (D-17 notes the exact rule).
- Undecodable sources emit no file node, so they are absent from the
  inventory; specifiers targeting them are unresolved, and the resolver never
  targets a nonexistent node.
- Bounds: the resolution unit is ordinary unit content, subject to batch
  limits (2,000/2,000/5,000) and unit caps (1,024 batches, 1,000,000 facts)
  (`CodeGraphBatch.cs:95-141`). §12 defines the projected capacity and the
  degradation policy; exceeding hard caps fails the run loudly rather than
  silently truncating.

## 10. Query semantics (PROPOSED guidance; no API changes required)

- New edges appear automatically in unfiltered neighborhood traversal and in
  `TraverseWithProvenanceAsync` with explicit kinds
  (`CodeGraphQueryService.cs:193-213, 997-1020`).
- Presets stay unchanged, so semantic queries (impact, dependencies,
  callers) do not silently incorporate module facts.
- Consumers who need "verified only" filter `EvidenceQualities=[Resolved]`;
  this works for traversals that cross only resolver edges. The documented
  evidence-filter limitation still applies when paths cross structural
  containment (`docs/evidence-quality-and-provider-strategy.md:721-756`).
- Property-level filtering (for example `local-name`) is client-side; the
  traversal API filters by kind, direction, and evidence only.
- Consumer guidance must be amended in the same milestone that ships each
  fact kind: extend `docs/consumer-guidance-qualified-name-lookup.md` with
  the module section, and state explicitly that absence of a resolver edge is
  "not verified", never "no dependency" (D-20).

Optional query conveniences (new service methods or presets) are deliberately
not required for EC-6B–E; if real consumers need them, they become a
separately reviewed additive API change (D-15).

## 11. Algorithms and termination (PROPOSED)

### 11.1 Resolution pass

```text
Resolve(inventory, symbols, policy):
  modules = inventory.SortedByPath
  tables  = memoized ExportTable per module               # §11.2
  edges   = []
  counts  = per-kind counters                              # §12.2
  foreach file F in modules (ordinal order):
    foreach import site in shape(F).Imports (source order):
      outcome = ResolveModule(F.path, spec.site)
      record counts for outcome
      if outcome resolved:
        edges += resolves-to(F.fileNodeId → outcome.fileNodeId, properties…)
        if site has bindings and form supported:
          binds = Bindings(F, site, tables[outcome.module])
          edges += binds; record counts
    # export edges are produced by table construction (per module, once)
  return edges (deterministically ordered), counts
```

All iteration is over ordinal-sorted paths and source-ordered shapes; the
final edge list is sorted by (kind, source, target, edge id) before emission.
Identical inputs therefore produce byte-identical batches and identical edge
ids across runs.

### 11.2 Export table

```text
Table(M):
  state[M] = InProgress
  direct = {}
  foreach export site in shape(M).Exports (source order):
    direct-form        → target = captured node id(s)             # no lookup
    local list {l as e}→ target = UniqueLocalDeclaration(M, l)
    named re-export    → target = Table(resolve(S)).Lookup(imported)
    namespace re-export→ target = resolved module file node       # kind=namespace
    default named      → target = captured node id
    default identifier → target = UniqueLocalDeclaration(M, name)
    unsupported        → count; no entry
    direct/recorded entries: name → (target, via, via-module, type-only, span)
  stars = {}
  foreach star site export * from S:
    N = resolve(S); if unresolved → count; continue
    foreach (name, target) in Table(N).Visible where name != "default":
      if name in direct → skip (shadowed)
      collect stars[name] += target
  foreach name in stars:
      distinct targets == 1 → entries[name] = (target, via=star, via-module=S)
      distinct targets > 1  → count ambiguous; no entry
  state[M] = Done
  Visible(M) = direct ∪ star entries (conflicts excluded)
```

Termination: `state[M]` in-progress modules contribute nothing to a re-entrant
lookup (conservative cycle cut); each module's table is computed at most once
per run; star and re-export traversal therefore runs in O(modules + names)
with memoization and cannot loop.

## 12. Resource limits, counting, and offline execution

### 12.1 Bounds

- Input: the session already materializes every source under
  `MaxSourceBytes`/`MaxTotalSourceBytes` (`src/Penghou.Hetu/CodeIndexingLifecycle.cs:11-33, 1105-1166`);
  the resolver adds only in-memory indexes over the same data.
- Output: bounded by unit caps. Projected worst case for a repository of `n`
  files with `i` imports and `e` exports each and `k` star-visible names is
  `resolves-to ≤ n·i`, `binds ≤ n·i·(bindings)`, `exports ≤ n·(e + k)`.
  Capacity before hard caps: 1,000,000 facts/unit and 1,024 batches × 5,000
  edges = 5,120,000 edges. A design-time guidance limit is proposed at
  250,000 resolver edges per language run; exceeding it degrades
  deterministically (prefer `resolves-to`, then direct exports, then
  bindings) and reports `{language}.resolution-limit` plus coverage counts —
  never silent truncation, never stale retention (replacement semantics mean
  truncation removes previous edges; coverage + warning make it visible).
  Exact policy is D-17.
- Pathological names: specifiers are bounded like existing identifiers
  (≤256 chars); overlong names are dropped exactly as the current profile
  drops them (`EcmaScriptExtractionProfile.cs:1034-1039`).

### 12.2 Coverage accounting (mapping onto the existing contract)

The three kinds join the Tree-sitter coverage list
(`TreeSitterExtractionSession.cs:24-34`) as produced kinds when the resolver
ships. Proposed counting rules (must satisfy
`EdgesEmitted == InternalTargets + CrossProjectTargets`,
`CodeGraphExtraction.cs:204-209`):

| Kind | Candidates | Internal | CrossProject | Unresolved | External | Ambiguous | Unsupported |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `resolves-to` | every observed specifier site | target file in same unit (self-reference) | target file in another unit | relative specifier not in inventory; escape | bare/URL/`node:` | 0 (ordered policy) | dynamic/template/`export =`/unsupported extensions |
| `exports` | every export site/name | target symbol in same unit (normal) | star/re-export target in another unit | local name or re-exported name not found | 0 | multiple distinct local targets; star conflicts | anonymous default, `export =`, imported-binding re-export (initially) |
| `binds` | every written binding | target in same unit (self-import) | target in another unit | module not resolved; export name not found; type/value mismatch | 0 | export name ambiguous | unsupported binding forms |

`CodeGraphExtractionResult.UnresolvedRelationships` (run-wide) is proposed to
sum the UnresolvedTargets of the three kinds (D-14 discusses the alternative
of reusing it unchanged). No new coverage fields are required; if field-level
semantics prove insufficient during implementation, that is an explicit
additive API proposal, not a silent overload.

## 13. Compatibility

- **No identity changes.** Every existing symbol, declaration, file, node,
  and edge identity is preserved; no graph migration is required.
- **No node kinds added.** Resolution adds edges only; the `field` F3
  limitation and all existing vocabulary stay untouched.
- **Old publications remain valid.** Pre-EC-6 publications simply lack
  resolver facts. The plugin version bump in the first emitting milestone
  re-extracts all sources, so no mixed-version units can exist inside one
  publication.
- **Stores and snapshots need no schema change.** Kinds and properties are
  open vocabularies; snapshots (schema v2) and the Lattice native mirror
  treat edge kinds as opaque strings
  (`src/Penghou.Hetu.LatticeDb/NativeGraphMirror.cs:103-111`).
- **No query API break.** Presets and operations are unchanged; new edges are
  discoverable via existing traversal APIs.
- **Existing test suites** that assert "all Tree-sitter edges are Structural"
  or "all providers equal the plugin id" must be rescoped in the emitting
  milestone (the roadmap lists the exact tests); this is a test-expectation
  update, not a behavior regression.

## 14. Risks and unresolved questions

1. **Evidence-quality claim.** Whether policy-based module resolution belongs
   in `Resolved` or `Derived` must survive independent review (D-03).
2. **Shared name-based symbol identities.** Top-level functions, enums,
   namespaces, and type aliases converge across files
   (`docs/ecmascript-symbol-identity.md:223-232`); export edges may point at a
   node exported by several modules. Accepted with documentation (D-05);
   identity migration is out of scope.
3. **Per-plugin scope.** TypeScript↔JavaScript imports cannot resolve
   (D-18); this limits real-repository recall and must be measured in EC-6F.
4. **Policy fidelity.** `relative-v1` deliberately diverges from TypeScript's
   full resolver; all deviations must be enumerated and qualified.
5. **Cost.** Full re-resolution per executing plugin run is O(files+imports);
   pathological repositories need the degradation policy of §12.1.
6. **Consumer over-interpretation.** Absence of resolver edges must be
   documented as "not verified" (D-20).
7. **Property vocabulary growth** is now the main public-vocabulary surface
   for modules; additions require the same discipline as kinds (D-19).

Unresolved architectural questions and owner-approval items are consolidated
in [ec6a-decision-register.md](ec6a-decision-register.md).
