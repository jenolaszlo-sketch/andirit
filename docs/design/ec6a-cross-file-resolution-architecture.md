# EC-6A: Cross-file resolution architecture (TypeScript / JavaScript)

**Status:** architecture design for EC-6, reconciled under EC6-A1. The
independent review EC6-GLM-AR1 returned **READY WITH QUALIFICATIONS** (no
P0/P1 findings) and the owner accepted the EC-6B direction with the F02/F04/
F05/F09 corrections folded in; the record is
[ec6a-owner-acceptance.md](../decisions/ec6a-owner-acceptance.md). Sections
keep per-milestone labels: vocabulary and semantics not accepted for EC-6B
(`exports`, `binds`, later policy levels) remain **proposals**, and none of
this document is implemented. EC-6B implementation authorization is a
separate owner decision.
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
- [EC6-A1 owner acceptance](../decisions/ec6a-owner-acceptance.md)

## 0. EC6-A1 reconciliation status

This section records the post-review state as of 2026-10-25. It does not
replace the sections below; it marks which of their proposals the owner
accepted and where the EC6-A1 corrections became normative. The binding
evidence is the independent review at `review/ec6-glm-ar1` (tip `c66ed60`):
`docs/reviews/ec6-glm-ar1-architecture-review.md` and
`docs/reviews/ec6-glm-ar1-decision-reconciliation.md`, with executed probes
P1–P6.

**Owner-accepted decisions (scope bound per row; details in the acceptance
record):**

| Acceptance | Decision | Accepted scope | Normative location |
| --- | --- | --- | --- |
| A1 (D-01) | Resolution placement | Pass inside the existing Tree-sitter session behind internal `ITreeSitterModuleResolver`, emitting into `{language}:modules`; no core resolution phase | §4, §5, §9.1 |
| A2 (D-02) | Module identity | Existing file nodes; no module node kinds or identity changes | §6.1 |
| A3 (D-03/O-01) | Evidence | `Resolved` for `resolves-to` under the declared policy, with mandated sentence and provenance | §8 |
| A4 (D-04) | Graph vocabulary | EC-6B adds only `resolves-to` plus its five property keys; `exports`/`binds` remain proposed | §7.1, §7.2 |
| A5 (D-13) | Rebuild policy | Full resolution-unit rebuild on relevant input or policy change; no incremental optimization in EC-6B | §9.2 |
| A6 (D-15) | Query behavior | Presets unchanged; `imports` remains the structural observation | §10 |
| A7 (D-18) | Cross-plugin scope | Per-plugin inventory only; limitation documented | §6.2.3 |

**Corrections folded in:** F02 → §9.1.1–§9.1.3 (ordering, staged vs
published facts, lifecycle scenarios) plus lifecycle fixtures AC-41–AC-45;
F04 → §6.2 (normalization, matching algorithm, input classification table);
F05 → §9.4 (determinism, cancellation, failure taxonomy, resource limits)
and §11.1 ordering; F09 → §7.1.1 (normative `resolves-to` identity) with the
full per-kind discriminator contract scheduled at EC-6C/D.

**Still proposed (not accepted for EC-6B):** `exports` and `binds` kinds and
properties; export tables; binding and type-only propagation semantics;
name-based symbol attribution (F01); later policy levels (`.js`→`.ts`
substitution, tsconfig); CommonJS surfaces; degradation numbers (D-17);
query conveniences; coverage persistence. See the deferred-decisions section
of the owner-acceptance record.

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
(`ITreeSitterModuleResolver`, internal; owner-accepted for EC-6B, not yet
implemented) plus an ECMAScript implementation
(`EcmaScriptModuleResolver`, EC-6B scope). A future language
implements the same internal seam; a future core phase (alternative B) can
lift the same resolver core without changing graph identities. See D-01 in
the [decision register](ec6a-decision-register.md).

## 5. Architecture overview (owner-accepted for EC-6B; not implemented)

```text
CodeIndexingService
  plan (per plugin/path manifest; version + hash invalidation)
    └─ plugin executes? ──no──► previous publication reused unchanged
         │ yes
         ▼
  TreeSitterExtractionSession (per plugin, e.g. typescript)
    1. per-file loop (existing, unchanged output):
         parse → facts → file unit (nodes/declarations/edges) → staged
          + CAPTURE module shape per file (EC-6B, in-memory only)
    2. resolution pass (EC-6B, after the loop):
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

Nothing in the diagram changes core code. EC-6B's only new graph vocabulary
is the owner-accepted `resolves-to` kind and its five property keys; the
further kinds of §7 remain proposals. No new node kinds, no new declarations,
no identity changes.

### 5.1 Module shape capture (EC-6B scope, in-memory)

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

## 6. Module resolution policy (EC-6B: `relative-v1` accepted; later levels proposed)

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

### 6.2 Policy `relative-v1` (owner-accepted; normative for EC-6B)

Only **relative specifiers** are resolved. A captured specifier qualifies as
relative when its raw text starts exactly with `./` or `../`. Nothing is
trimmed and nothing is decoded; the exact written text is preserved in the
`specifier` property and is never normalized away. The resolver never reads
the filesystem, environment, or network; only the session's decodable source
inventory is consulted.

#### 6.2.1 Normalization (normative)

`NormalizeJoin(importerPath, specifier)` uses POSIX segment arithmetic over
the already repository-relative, `/`-normalized source paths:

1. `base` = the importer path split on `/` with its last segment removed.
2. Split the specifier on `/` and process segments left to right:
   - `.` contributes nothing.
   - an empty segment (from `//`, a trailing `/`, or a bare `./`) rejects
     the specifier as `unsupported` syntax; repeated separators are never
     silently collapsed.
   - `..` pops one `base` segment; popping past the repository root rejects
     the specifier as root-escaping (`unsupported`).
   - any other segment is appended verbatim.
3. The result is the repository-relative candidate path.

This mirrors `ContractValue.RelativePath` (`ContractValue.cs:31-44`): source
paths are `/`-separated, non-rooted, and contain no `..` segments; candidate
paths constructed here satisfy the same invariants by construction. Path
comparison is **ordinal** (case-sensitive), matching every store lookup and
`ContractValue` handling. Case-only differences are therefore deterministic
mismatches even on case-insensitive file systems, because no filesystem probe
occurs. Symlinked entries never enter the inventory: repository discovery
skips reparse points and refuses to follow them
(`CodeRepositorySource.cs:287-292, 310-312, 358-368`), so a specifier naming
one is simply unresolved.

#### 6.2.2 Matching algorithm (normative, ordered; first match wins)

```text
ResolveModule(importerPath, rawSpecifier):
  if rawSpecifier does not start with "./" or "../"     → classification table
  if rawSpecifier contains "\", "?", or "#"             → Unsupported
  if rawSpecifier length > 256                          → Unsupported (no text retained)
  joined = NormalizeJoin(importerPath, rawSpecifier)    → Unsupported on escape/empty segment
  if inventory contains joined                          → (joined, exact)
  if final segment of joined has no ".":
      for suffix in policySuffixes(language):           # ordered list below
          if inventory contains joined + suffix         → (joined+suffix, extension-substitution)
      for suffix in policySuffixes(language):
          if inventory contains joined + "/index" + suffix
                                                        → (joined+"/index"+suffix, directory-index)
  else:
      if last extension ∈ ecmaScriptExtensions           → Unresolved (module-not-found)
      else                                               → Unsupported (out-of-policy extension)
  → Unresolved(module-not-found)
```

- `policySuffixes("typescript")` = `.ts`, `.tsx`, `.d.ts`, `.mts`, `.cts`;
  `policySuffixes("javascript")` = `.js`, `.jsx`, `.mjs`, `.cjs`.
- `ecmaScriptExtensions` is the union of every suffix's last extension across
  both policy lists: `.ts`, `.tsx`, `.mts`, `.cts`, `.js`, `.jsx`, `.mjs`,
  `.cjs`.
- Reconciliation note: `.d.ts` is in the TypeScript ordered list because
  AC-40 requires `import './types'` to reach `types.d.ts`; the original draft
  listed only `.ts`/`.tsx`/`.mts`/`.cts` while AC-40 already assumed the
  declaration suffix. Explicit `.d.mts`/`.d.cts` targets are not reached by
  extension substitution.
- `inventory` is the session's decodable source set (the only files that
  emitted a file node in this run). A specifier that names a file outside the
  plugin's inventory is `module-not-found` for that resolver, even when the
  file exists in the repository; this is honest within scope and is the
  documented cost of per-plugin resolution (D-18).
- Known deliberate deviations from TypeScript's own resolver are listed in
  §6.4 and qualified per milestone.

#### 6.2.3 Input classification table (normative, F04)

Every captured site is classified exactly once by the first applicable rule.
"Captured" means the per-file walk recorded the site; shapes the walk never
captures cannot reach the resolver (F04 requires the outcomes below to be
deterministic, not that every syntactic form becomes a candidate).

| Input category | Classification | Deterministic rule |
| --- | --- | --- |
| `./module`, `../module` | Resolved / Unresolved | NormalizeJoin then the ordered matching algorithm |
| `./module.ts` (explicit policy extension) | Resolved (exact) / Unresolved | Exact inventory match only; no suffix is appended to a specifier whose final segment already has an extension |
| `./module.js` | Resolved only if that exact `.js` path is in *this plugin's* inventory; otherwise Unresolved | No `.js`→`.ts` substitution (D-08) |
| `./module.json`, `./widget.astro` | Unsupported | Last extension is not an ECMAScript source extension; never searched |
| `./dir` (extensionless, not a file) | Resolved via directory-index / Unresolved | All suffixes first, then `<dir>/index<suffix>` in the declared order; `package.json` is never read |
| `./` | Unsupported | Trailing separator produces an empty segment |
| bare `.` / `..` | External | Does not start with `./` or `../`; bare specifiers are out of scope |
| `./a//b` | Unsupported | Repeated separator produces an empty segment |
| `./a/./b` | Resolved / Unresolved | Single-dot segments are no-ops; the remainder is normalized deterministically |
| `.\a`, `./a\b` | Unsupported | Any backslash in the raw text; source *paths* are POSIX-normalized, specifier text is never treated as a path |
| `./x?raw`, `./x#frag` | Unsupported | Query/fragment suffixes are never stripped or interpreted |
| `/abs/x` | Unsupported | Absolute paths are never resolved |
| `../../../x` escaping the root | Unsupported | Segment arithmetic pops past the repository root |
| `zod`, `node:fs`, `https://…`, `@/alias`, `#internal` | External | Raw text does not start with `./`/`../` and is not an absolute or backslash form; configuration aliases and package resolution are out of policy (D-09) |
| dynamic `import(expr)`, template `require` | Unsupported | Not a literal site; counted by shape capture where observed |
| literal over 256 chars | Unsupported | The site is captured without retaining text; no property value and no edge |
| missing file | Unresolved | No inventory match (`unresolved-targets`) |
| multiple physical candidates (`x.ts` + `x.tsx`) | Resolved to the declared first match | The ordered suffix list **is** the policy; `resolution-rule` records the matched rule per edge, so the choice is declared and auditable, never an arbitrary fallback |
| case-only difference | Unresolved | Ordinal comparison; no filesystem probe |
| symlink target | Unresolved | Reparse points never enter the inventory; resolution cannot leave the indexed boundary |

Outcome semantics and ownership are exactly §6.3; coverage counters per
outcome are §12.2.

**Multiplicity and ambiguity.** Under `relative-v1`, candidate precedence is
explicit and versioned (suffix order, then file-before-index), so several
physical candidates are not ambiguity: the policy declares the single
candidate and the matched rule is recorded per edge. The design refuses
*heuristic* choice (similar names, recursive directory search,
configuration-dependent candidates). Equal-precedence ambiguity is not
reachable for `resolves-to`; it remains reserved for export-name resolution
at EC-6C/D, where no edge is emitted and `ambiguous-targets` is counted.

**Scope boundary (D-18, owner-accepted).** The inventory is the owning
plugin's decodable sources only. A relative specifier naming a file that
exists in the repository but belongs to another plugin's language is
Unresolved (`module-not-found`) for this resolver; no edge is fabricated.
Cross-plugin TypeScript/JavaScript resolution is not claimed, and missing
targets outside the inventory are never represented as verified source
dependencies. The recall cost is measured in EC-6F.

### 6.3 Resolution outcomes

| Outcome | Meaning | Graph effect | Coverage |
| --- | --- | --- | --- |
| Resolved | The ordered policy produced exactly one candidate | `resolves-to` edge | emitted |
| Unresolved | Relative specifier with no inventory match (including explicit extensions that exist only as a substitute, e.g. `./x.js` with only `x.ts`) | none | `unresolved-targets` |
| External | Not a relative specifier: bare packages, `node:`/URL schemes, aliases | none | `external-targets` |
| Unsupported | Rejected or out-of-policy syntax: dynamic `import()`, template `require`, `import x = require()`, `export =`, escape/absolute paths, repeated or trailing separators, backslashes, query/fragment suffixes, overlong text, out-of-policy extensions (e.g. `.astro`, `.json`) | none | `unsupported-targets` |

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

## 7. Cross-file facts (EC-6B: `resolves-to` accepted; further kinds proposed)

### 7.1 Edge kinds

Additive constants on `CodeEdgeKinds` (`CodeGraphKinds.cs`). Only
`resolves-to` is owner-accepted for EC-6B (A4/D-04); the `exports`/`binds`
rows remain proposals gated at EC-6C/EC-6D:

| Constant | Value | Meaning | Source | Target | Evidence | EC-6B |
| --- | --- | --- | --- | --- | --- | --- |
| `ResolvesTo` | `resolves-to` | A written module specifier was resolved to an indexed module under a declared policy | importing/re-exporting **file node** | imported **file node** | `Resolved` | accepted |
| `Exports` | `exports` | Module exports a name backed by a declared entity | exporting **file node** | declared **symbol node** (named/default) or **file node** (namespace re-export) | per EC-6C gate (F12) | proposed |
| `Binds` | `binds` | A written import binding denotes a specific export | importing **file node** | exported **symbol node** (named/default) or **file node** (namespace import) | per EC-6D gate (F12) | proposed |

Edge ids follow the existing formula
(`EcmaScriptExtractionProfile.cs:1095-1096`):

```text
{language}:{sha256(kind \n source \n target \n discriminator \n
                   startLine:startColumn:endLine:endColumn)}
```

The discriminator encodes the written name and form (for example
`resolves-to:import`, `exports:named:default`, `binds:named:a->b`) so distinct
written facts never collide while repeated extraction of identical input
reproduces identical ids.

#### 7.1.1 Edge identity and multiplicity (normative; F09)

Properties are **not** identity inputs: edge identity is the id string alone
(`CodeGraphFacts.cs:79-101`), while equality — including properties — decides
whether two contributions of one id merge or conflict
(`GraphFactEquality.cs:25-31`). Logical multiplicity therefore has to live in
the identity inputs. For EC-6B's only new kind the normative inputs are:

| Input | Value for `resolves-to` |
| --- | --- |
| kind | `resolves-to` |
| source | importing/re-exporting file node id `{language}:file:{sha256(importerPath)}` |
| target | resolved file node id `{language}:file:{sha256(targetPath)}` |
| discriminator | `resolves-to:{module-reference}:{raw-specifier-text}` |
| location | the written statement span: the import/export-from statement node, or the `require('./m')` call expression node |

Location conventions are the existing profile's: one-based lines, one-based
**UTF-16 code-unit** columns (the pinned `TreeSitter.DotNet` binding parses
the decoded .NET string as UTF-16LE, so Tree-sitter row columns count UTF-16
code units rather than UTF-8 bytes; `TreeSitterSyntax.cs:39-50`),
end-exclusive (`CodeLocation.cs:3-35`). EC-6B-R1 corrected the earlier "byte
columns" wording; the emitted coordinate behavior is unchanged and
remains deterministic. Consequences, which the EC-6B fixtures must assert:

1. Each written module-reference site emits at most one `resolves-to` edge;
   two identical specifiers at distinct source locations differ in the
   location input and are two distinct edges.
2. Two different written specifiers resolving to the same target (`./a` and
   `./a.ts`) differ in the discriminator and are distinct edges.
3. The same target imported from different files differs in the source input.
4. Identity is occurrence-anchored, exactly like every existing profile edge:
   an edit that shifts a statement's span changes that edge id (and the ids
   of later statements on shifted lines). Identity is stable only under
   byte-identical re-extraction; this is documented behavior, not a defect.
5. Line endings do not change identity: CRLF changes neither the row nor the
   column of any token, because `\r` belongs to the preceding row; LF and
   CRLF variants of the same text produce identical ids.
6. Stale occurrences disappear only through whole-unit replacement; there is
   no tombstone logic. Removing one of two same-target imports removes exactly
   the removed occurrence's edge and leaves the other edge id byte-identical.
7. Duplicate identities are rejected, never merged silently: within a batch
   the validator reports `batch.edge.duplicate` (`CodeGraphBatchValidator.cs:28,
   64-80`); within a unit the store reports `replacement.edge.duplicate`
   (`InMemoryCodeGraphStore.cs:784-826`); across units a same-id different-
   properties contribution is a loud `graph.edge.conflict`
   (`InMemoryCodeGraphStore.cs:985-1011`); a collision with identical facts
   merges into one edge with multiple origins. The resolver must construct
   ids deterministically and fail fatally on a differing-fact collision rather
   than suppressing duplicates at emit time.
8. Edge properties and identity stay consistent by never changing a property
   without changing one of the identity inputs that produced the fact: the
   raw written specifier and the module-reference form are both identity
   inputs, the policy/rule are version-stamped provenance, and a policy
   change re-extracts everything (D-16), so one published graph never contains
   two policies' edges.

The full per-kind discriminator contract for `exports` and `binds` (aliases,
re-export forms, and their fixtures) is required at the EC-6C/D gates; it is
**not** claimed for EC-6B.

Why separate kinds instead of upgrading `imports`:
`imports` is an existing, documented structural observation
(`docs/language-feature-matrix.md:117`) and its identity/edges are already
published; changing its meaning or target would be a fact migration. The
EC-4 recommendation ("emit a second, resolution-evidenced edge while keeping
the spelling observation") is adopted verbatim. `depends-on` is not reused:
its documented scope is project/package dependencies
(`docs/language-feature-matrix.md:113`).

### 7.2 Property keys

Additive constants on `CodePropertyKeys`. The owner accepted the EC-6B
subset — `specifier`, `module-reference`, `resolution-policy`,
`resolution-rule`, `type-only` — as public vocabulary (A4/D-19); the
`exports`/`binds` keys remain proposals:

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

## 8. Evidence and provenance (owner-accepted for `resolves-to`)

- **Quality (A3/D-03/O-01, owner-accepted):** `resolves-to` edges carry
  `Resolved` with the following normative meaning, which must appear verbatim
  in consumer documentation and the identity doc:

  > The target was verified under the declared resolution policy and the
  > indexed source inventory of this publication. This does not establish
  > equivalence with TypeScript compiler, bundler, or runtime resolution.

  Rationale: the resolver is a named, versioned provider that applies the
  language's module-resolution concept under a declared policy to inputs it
  fully possesses; `Resolved` means "resolved by the named provider in its
  analysis context, not universally exact" (`CodeEvidence.cs:15-20`), and the
  claim is bounded to policy verification, never to compiler or runtime
  equivalence. A matching filename alone never justifies `Resolved`; when the
  policy's conditions are not met, no edge is emitted. The rejected
  alternative (`Derived`) and its tradeoffs remain recorded in D-03. The
  evidence classification of `exports`/`binds` is **not** settled here: it is
  scheduled at the EC-6C/EC-6D gates under F12's preconditions (direct-capture
  may be `Resolved`; scope-resolution requires a documented module-scope
  model; name-match-only binding is forbidden in either quality).
  `Imports` and every pre-existing fact stay `Structural` and unchanged.
- **Provider (accepted as A3's concrete provenance):** resolver identity
  `penghou.hetu.tree-sitter.ecmascript-module-resolver`, distinct from the
  extraction plugin id, so consumers can separate "who parsed" from "who
  resolved" (`CodeEvidence.Provider`/`ProviderVersion`). This implements the
  review's accepted recommendation (EC6-A1 A3; GLM reconciliation D-19); it
  does not introduce a second invalidation axis (D-16).
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

## 9. Ownership, invalidation, and publication (owner-accepted for EC-6B)

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

#### 9.1.1 Emission order (normative; F02)

Within one Tree-sitter session execution the following order is required:

1. **Per-file extraction units.** The existing loop over `context.Sources`
   (ordinal path order, `TreeSitterExtractionSession.cs:74-161`) stages each
   decodable file's unit — nodes, declarations, edges — and completes it
   before the next file starts. Undecodable files stage an empty completing
   batch and contribute no file node
   (`TreeSitterExtractionSession.cs:96-107`). Deleted paths are not staged
   here; the session reports them as obsolete unit ids after the loop
   (`TreeSitterExtractionSession.cs:163-169`).
2. **Module-shape capture (in memory).** No graph fact is emitted in this
   step; shapes exist only for the resolver pass (§5.1).
3. **Resolution pass.** Runs after the loop over the session's decodable
   inventory and the captured shapes (§11.1).
4. **Resolution-unit staging.** The resolver's batches are written to the
   same sink after all file units; the completing batch stages
   `{language}:modules` as one whole-unit replacement. An empty completing
   batch is the valid representation of "no resolvable facts" and clears the
   previous unit's edges (probe P2).
5. **Lifecycle deletions and completion.** Only after the session returns
   does the lifecycle stage obsolete file-unit deletions
   (`CodeIndexingLifecycle.cs:681-696`), then cross-run ownership hygiene
   (`CodeIndexingLifecycle.cs:716-731`), then complete the run atomically
   (`CodeIndexingLifecycle.cs:732`).

**Endpoint availability (normative).** Every `resolves-to` target is the
file node of a source that is either decodable in this run (staged in step 1)
or still present from the plugin's previous publication. The resolver never
targets deleted, undecodable, or absent files, so
`graph.edge.endpoint-missing` cannot occur for a correct resolver in a
successful run. The store's rejection
(`InMemoryCodeGraphStore.cs:913-929`) remains the invariant backstop, not the
specification; the ordering above is what prevents the condition GLM observed
in probe P1.

#### 9.1.2 Staged facts vs published facts (normative)

- **Staged facts** are replacements recorded for the running run by
  `StageIndexUnitAsync`; they are validated against the *prospective graph*
  (previous publication − staged deletions + staged replacements + the
  replacement itself; `InMemoryCodeGraphStore.cs:287-299, 661-671`) and are
  invisible to every query.
- **Published facts** are the unit set swapped in atomically by
  `CompleteIndexRunAsync` (`InMemoryCodeGraphStore.cs:150-215`).
- Ordered staging (9.1.1) exists to satisfy staging-time endpoint validation.
  It is **not** an atomicity guarantee; visibility comes only from
  completion. The two properties are independent and were verified
  separately (GLM probes P1/P2/P3/P6).

#### 9.1.3 Lifecycle scenarios (normative; F02)

| Scenario | Staging behavior | Published result |
| --- | --- | --- |
| Target file disappears | Plugin executes; the target's file unit is reported obsolete; the rebuilt resolution unit excludes it; the obsolete deletion is staged after the resolution replacement (step 5) | No `resolves-to` edge targets the deleted node; every previous edge for it is gone with the unit replacement. The importer's structural `imports` observation is untouched |
| Importer file disappears | The importer contributes no unit; the rebuilt resolution unit excludes it; the importer's file unit is deleted in step 5 | No outgoing resolver edge from the deleted importer; no stale edge survives |
| Last resolved relationship disappears | The resolution unit is staged with a zero-edge completing batch | Previous edges are removed; the unit remains published, empty (probe P2) |
| Previously unresolved module becomes available | The new source is staged in step 1; the rebuilt resolution unit emits the edge against the new file node | The edge appears in the same atomic publication as the new file node |
| Extraction succeeds, resolution fails | Resolver failure fails the plugin execution; the run fails; the staged footprint is discarded | The previous publication — including the previous resolution unit — remains queryable and consistent (`failed-run-discards-staged-graph`) |
| Publication interrupted (crash, rejection, supersession) | Staged facts never become visible; superseded runs fail loudly (`RequireCurrentBaseline`) | Prior publication retained; no partial resolution edge set is ever queryable |

**All-files-deleted:** when every source of the plugin is deleted, the empty
resolution unit is staged and then displaced by cross-run ownership hygiene
because the plugin owns no paths in the new state
(`CodeIndexingLifecycle.cs:716-731`; `InMemoryCodeGraphStore.cs:389-390`).
The outcome is correct; EC-6E pins it with a fixture (GLM F10).

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
  The full failure taxonomy and cancellation contract are normative in §9.4.
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

### 9.4 Determinism, cancellation, and failure contract (normative; F05)

#### 9.4.1 Determinism

Given identical source inventory (paths and content hashes), identical source
contents, identical plugin versions, resolver version, policy value, and
applicable settings, the resolver pass produces byte-identical resolution
facts: identical edge ids, properties, batching, and coverage counts.

Stable orderings, all normative:

- Files are processed in ordinal path order (the existing session order).
- Sites within a file are processed in source order (the walk order of the
  profile).
- The emitted edge list is sorted by `(kind, source, target, edge id)`
  (ordinal) before batching; batches are formed deterministically from that
  order.
- No decision may depend on filesystem enumeration order, dictionary
  insertion order, culture-sensitive comparison, or hash-map iteration order.
  Lookup structures are explicitly ordered wherever their iteration can
  influence emitted facts or batches.

#### 9.4.2 Cancellation

- The resolver observes the session's `CancellationToken` at least between
  files/modules and before emitting each resolution batch, exactly as the
  existing per-file loop does (`TreeSitterExtractionSession.cs:76`).
- Cancellation surfaces as `OperationCanceledException`; the run reaches the
  `Cancelled` terminal state, staged changes are discarded, and the previous
  publication (including the previous resolution unit) is retained
  (conformance `cancelled-run-discards-staged-graph`).
- Cancellation must never be converted into a successful empty or partial
  resolution unit; publishing partially completed resolution edges is
  forbidden. No fallback path may swallow the token.

#### 9.4.3 Failure taxonomy

| Situation | Class | Behavior |
| --- | --- | --- |
| Specifier missing from inventory | Ordinary non-resolution | No edge; `unresolved-targets`; run succeeds |
| Specifier external, unsupported, or rejected syntax | Ordinary non-resolution | No edge; matching counter (§6.2.3); run succeeds |
| Parse-error source | Diagnostic | Existing `Partial` handling; resolvable shapes still emit; resolver kinds report `Partial` |
| Undecodable source | Diagnostic | Absent from inventory; specifiers naming it are unresolved; resolver kinds report `Partial` |
| Invalid/escaping input paths | Ordinary (by classification) | Deterministic `Unsupported`; never normalized outside the repository |
| Ambiguous match | Not reachable for `resolves-to` (§6.2.3); reserved for EC-6C/D | No edge; `ambiguous-targets` |
| Resolver exception (invariant violation, duplicate identity with differing facts, unexpected state) | Fatal | Propagates out of the session; plugin execution fails; run fails with the existing plugin-failure classification (`CodeIndexingLifecycle.cs:471-492`); previous publication retained |
| Resource exhaustion (batch/unit caps, source byte budgets) | Fatal | Sink poisons the unit; run fails loudly (`ingestion.index-unit.limit`); never silent truncation |
| Indexing cancellation | Cancellation | §9.4.2 |
| Publication failure | Fatal | Staged facts discarded; previous publication retained (`failed-run-discards-staged-graph`) |

Ordinary per-specifier non-resolution is counted, never thrown. A fatal
resolution-pass failure must preserve the previous successful publication
under the existing indexing contract and must never downgrade into a warning
that publishes partial facts. Diagnostics record the failure class; they
never fabricate resolver facts.

#### 9.4.4 Resource limits

EC-6B reuses the existing runtime limits and invents no benchmark-derived
numbers: source materialization stays under
`MaxSourceBytes`/`MaxTotalSourceBytes` (`CodeIndexingLifecycle.cs:15-16,
1105-1166`); the resolution unit obeys the batch and unit caps
(`CodeGraphBatch.cs:95-141`) and fails the run loudly when exceeded. A
guidance limit with deterministic degradation (D-17) remains owner-gated and
is qualified with fixtures in EC-6E (O-07); no numeric threshold is fixed by
this document.

## 10. Query semantics (A6/D-15 owner-accepted; no API changes required)

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

## 11. Algorithms and termination (EC-6B executes the module path only)

### 11.1 Resolution pass

```text
Resolve(inventory, shapes, policy):
  modules = inventory.SortedByPath
  tables  = memoized ExportTable per module               # §11.2 (EC-6C/D)
  edges   = []
  counts  = per-kind counters                              # §12.2
  foreach file F in modules (ordinal order):
    cancellationToken.ThrowIfCancellationRequested()       # §9.4.2
    foreach module site in shape(F).ModuleReferences (source order):
      outcome = ResolveModule(F.path, site)
      record counts for outcome
      if outcome resolved:
        edges += resolves-to(F.fileNodeId → outcome.fileNodeId, properties…)
        # EC-6B stops here: no binding step, no export-table consultation
  return edges (deterministically ordered), counts
```

EC-6B executes only the module-resolution path: no export table is built, no
binding step runs, and no `exports`/`binds` fact exists (A4). The
table/binding lines remain the EC-6C/D plan and are inactive until the
`.p8`-next milestones.

All iteration is over ordinal-sorted paths and source-ordered shapes; the
final edge list is sorted by (kind, source, target, edge id) before emission.
Identical inputs therefore produce byte-identical batches and identical edge
ids across runs; ordering and cancellation are normative in §9.4.

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
- Pathological names: the raw specifier is bounded at 256 characters like
  existing identifiers (`EcmaScriptExtractionProfile.cs:1017-1021,
  1034-1039`). An overlong site is captured without retaining text, counted
  `unsupported-targets`, and emits no edge; as today, the profile also emits
  no `imports` placeholder for it. The bound is pinned by §6.2.3.

### 12.2 Coverage accounting (mapping onto the existing contract)

EC-6B reports only `resolves-to`; the `exports`/`binds` rows below remain
proposals for their milestones. Every kind joins the Tree-sitter coverage list
(`TreeSitterExtractionSession.cs:24-34`) as a produced kind when it ships.
Counting rules (must satisfy
`EdgesEmitted == InternalTargets + CrossProjectTargets`,
`CodeGraphExtraction.cs:204-209`); for `resolves-to` the unit of "same unit"
is the file unit owning the source file node, so self-references count
internal and every other resolved target counts cross-project:

| Kind | Candidates | Internal | CrossProject | Unresolved | External | Ambiguous | Unsupported |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `resolves-to` | every captured specifier site | source and target file nodes are the same file unit (self-reference) | target file node belongs to another file unit | relative specifier not in inventory, including explicit out-of-plugin extensions (`.js` from TypeScript) | bare/URL/`node:`/aliases | 0 (ordered policy; §6.2.3) | escape/absolute, repeated/trailing separators, backslashes, query/fragment, overlong, dynamic/template/`export =`, out-of-policy extensions (`.json`, `.astro`) |
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

1. **Evidence-quality claim (closed for EC-6B).** The independent review
   recommended `Resolved` for `resolves-to` under the mandated policy
   sentence, and the owner accepted it (A3/D-03/O-01). The `Derived` fallback
   remains recorded as the rejected alternative; `exports`/`binds`
   classification is still open at their gates (F12).
2. **Shared name-based symbol identities (open; F01).** Top-level functions,
   enums, namespaces, and type aliases converge across files
   (`docs/ecmascript-symbol-identity.md:223-232`); export edges may point at a
   node exported by several modules. The issue does not affect EC-6B (no
   export or binding facts) and must be resolved or attributed at EC-6C/D
   through F01's options; identity migration is a separate milestone.
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
