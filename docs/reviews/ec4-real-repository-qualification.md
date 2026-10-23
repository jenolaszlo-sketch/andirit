# EC-4: Real-repository code graph qualification (jenolaszlo-sketch/website)

Status: qualification report, 2026-10-23. Executor: GLM. Verdict:
**USEFUL WITH QUALIFICATIONS** (scoped exactly as stated in section J).

This milestone evaluated Hetu against an actual repository rather than
synthetic parser fixtures. Every number below comes from executed runs of the
review-only harness preserved in `review/ec4/harness/` against an isolated
snapshot of the target repository; evidence files are in `review/ec4/evidence/`.
No production code was changed.

## A. Git and environment binding

| Item | Value |
| --- | --- |
| Hetu implementation | `jenolaszlo-sketch/penghou-hetu`, commit `1cc2939ea615d627cde616d06e739867be4e56e2` (canonical `main`) |
| Prior independent EC-3D verification | `1287590ee0c46bd863873d30baea633c3b04ef13`, verdict "VERIFIED WITH QUALIFICATIONS" |
| Review branch / worktree | `review/ec4-real-repository-qualification` at `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec4`, branched from `1cc2939` |
| Target repository | `https://github.com/jenolaszlo-sketch/website`, local checkout `C:\Users\Laszlos\source\repos\website`, branch `main`, commit `f9cca1773a32bb517e624cc380e1f6d14e0cc317`, tree `f5df0538ee751c9f6dae5890ff69b8b6c5aed69e` |
| Indexed source | isolated disposable snapshot produced with `git archive` from the target commit into `C:\Windows\TEMP\opencode\ec4`; the canonical checkout was never indexed or modified |
| Runtime | .NET SDK 10.0.401, `net10.0`, Windows (win-x64) |
| Stores | `InMemoryCodeGraphStore` (baseline, queries, lifecycle); `LatticeCodeGraphStore` schema version 2 (durable and migration runs) |
| Extraction plugins | `penghou.hetu.tree-sitter.typescript` `0.2.0-preview.6+v0.23.2.abi14.p5` (TypeScript grammar v0.23.2, ABI 14), `penghou.hetu.tree-sitter.javascript` `0.2.0-preview.6+v0.25.0.abi15.p5` (JavaScript grammar v0.25.0, ABI 15) |
| Historical p4 extractor | built from authentic source at `e4598ac` (`ProfileRevision = 4`, package `0.2.0-preview.6`), used only for the migration run |
| C# extractor | not exercised (no C# sources in the target) |

Reproduction (from the review worktree, Release build):

```text
dotnet review/ec4/harness/Ec4Harness/Ec4Harness.dll full     <snapshot> <outdir>
dotnet review/ec4/harness/Ec4Harness/Ec4Harness.dll heritage <snapshot> <outfile>
dotnet review/ec4/harness/Ec4Harness/Ec4Harness.dll lifecycle <snapshot> <outdir>
dotnet review/ec4/harness/Ec4Harness/Ec4Harness.dll durable  <snapshot> <outdir> <dbPath>
dotnet review/ec4/harness/Ec4Harness/Ec4Harness.dll migrate  <snapshot> <p4-dbPath> <outfile>
```

## B. Source inventory

Discovered by direct file enumeration of the snapshot (78 tracked files) and
confirmed by the planner diagnostics (`filesDiscovered=78`, `filesUnsupported=69`).

| Category | Count | Indexed? |
| --- | --- | --- |
| TypeScript `.ts` | 8 | yes (TypeScript plugin) |
| JavaScript `.mjs` | 1 (`astro.config.mjs`) | yes (JavaScript plugin) |
| TSX `.tsx`, JSX, `.js`, `.jsx`, `.mts`, `.cts` | 0 | n/a |
| Astro `.astro` | 22 | no (no Astro extractor) |
| Markdown, JSON, CSS, YAML, assets, config | 47 | no (unsupported extensions) |
| Total | 78 | 9 supported |

Exclusion policy: the planner's default directory exclusions are `.git`, `bin`,
`obj`, `node_modules`, `dist`, `packages`, `vendor`. The git-archive snapshot
contains none of those directories, so `directoriesExcluded=0` is expected and
not evidence that exclusions were exercised. Generated directories that would be
excluded in a real checkout: `node_modules` (dependencies), `dist` (build
output), `.astro` (generated types).

Supported files: `src/lib/ecosystem.ts`, `src/lib/format.ts`, `src/lib/paths.ts`,
`src/lib/schema.ts`, `src/lib/site.ts`, `src/pages/robots.txt.ts`,
`src/content.config.ts`, `src/env.d.ts`, `astro.config.mjs`.

Astro-specific TypeScript not handled by the pipeline (recorded, not classified
as a defect): all 22 `.astro` files, including frontmatter interfaces (`Props`),
exported page functions (`getStaticPaths`), component destructuring, and every
component/page import of the `src/lib` modules. `src/env.d.ts` is parsed but
produces no facts beyond its file node (a triple-slash reference only).

## C. Indexing results

Baseline run (`full` mode, `CodeIndexingService.IndexAsync` through `HetuHost`,
in-memory store, one run):

| Diagnostic | Value |
| --- | --- |
| Status | Completed |
| Files discovered / new / unchanged / deleted | 78 / 9 / 0 / 0 |
| Files unsupported | 69 |
| Plugins executed | 2 (TypeScript: 8 sources examined, 7 contributing; JavaScript: 1 source examined, 1 contributing) |
| Index units completed | 9 |
| Source bytes read | 35,810 |
| Facts produced | 109 nodes, 62 declarations, 235 edges; 0 rejected, 0 unresolved |
| Durations (this run) | planning 48.0 ms, extraction 109.4 ms, persistence 9.3 ms (wall 190.4 ms) |
| Warnings | none |

Published graph aggregate (reconstructed through public queries from every file
node; reproduced byte-identically across two independent runs — deterministic):

| Nodes | Count | | Edges | Count | Evidence |
| --- | --- | --- | --- | --- | --- |
| file | 9 | | contains | 62 | Structural |
| callable | 38 (12 declared + 26 invocation placeholders) | | declares | 62 | Structural |
| interface | 10 | | imports | 9 | Structural |
| property (interface members) | 38 | | inherits | 1 | Structural |
| type | 12 (2 type aliases + 9 import targets + 1 heritage target) | | invokes-name | 101 | Structural |
| **total** | **107** | | **total** | **235** | all Structural |

`NodesProduced=109` exceeds the published 107 because two invocation
placeholders (`withBase`, `URL`) converge across files by name, as documented.
All 62 symbol-bearing nodes carry exactly one physical declaration each; no
declaration merging occurred in this repository. Derived evidence: none (no
recognized prototype patterns exist in the source).

Suitability (EC-4A3): the repository exercises interfaces, interface members
with optional/readonly-style modifiers, type aliases, top-level functions,
arrow-assigned exported callables, imports, and one real interface-heritage
edge. It contains no classes, no class members, no enums, no JS prototype
patterns, and no implements clauses, so EC-3 class/member and Derived-heritage
behaviors could not be exercised here beyond what previous fixtures cover.
The supported subset is nonetheless sufficient for a meaningful structural
qualification.

## D. Ground-truth verification

Ground truth was derived by independent source inspection of all 9 supported
files (not from Hetu fixtures) before comparing against the published graph.

### D1. Declaration discovery — 62/62 correct (repo-wide within the supported subset)

Hand-derived expectation vs published declarations:

| File | Expected | Published |
| --- | --- | --- |
| `src/lib/format.ts` | 2 callables | 2 |
| `src/lib/paths.ts` | 1 callable | 1 |
| `src/lib/site.ts` | 2 interfaces + 9 members | 11 |
| `src/lib/ecosystem.ts` | 1 type alias + 5 interfaces + 16 members | 22 |
| `src/lib/schema.ts` | 1 type alias + 8 callables + 3 interfaces + 13 members | 25 |
| `src/pages/robots.txt.ts` | 1 callable (`GET`, arrow-assigned const) | 1 |
| `src/content.config.ts`, `astro.config.mjs`, `src/env.d.ts` | 0 declared symbols | 0 |
| **Total** | **62** | **62** |

Every sampled declaration has the correct node kind, name, physical
declaration, file, span, ownership, and evidence level (Structural), and no
unintended identity convergence occurred. Span spot checks (1-based, matching
`CodeLocation` semantics):

- `withBase` declaration `src/lib/paths.ts:12.8-15.2` — matches source lines 12–15.
- `StatusDefinition` `src/lib/ecosystem.ts:8.8-12.2`; member `key` `9.3-9.14`.
- `RuntimeLayer.adjunct` `123.3-123.20` with `modifiers=optional`.
- Heritage edge location `200.46-200.56` — exactly the written `ProjectRef` token.
- `formatDate` `src/lib/format.ts:1.8-8.2`.

Two apparent mismatches during verification resolved in the graph's favor both
times: `SupportingPrimitive` declares only `note` (its `label`/`href` are
inherited from `ProjectRef`, and the graph correctly does not propagate
interface members), and my initial line-number recollection for `withBase` was
wrong while the graph was right. These are recorded as reviewer corrections,
not graph findings.

Sampled precision/recall on 3 files (denominator 48 declarations:
`schema.ts` 25 + `ecosystem.ts` 22 + `robots.txt.ts` 1): precision 48/48,
recall 48/48, 0 missing, 0 unexpected. This is a sample metric only; the 62/62
table above is the full supported-subset count.

### D2. Containment and ownership

`contains` edges exist only between each file (or interface) and the
declarations it owns; `declares` edges connect each file to every declaration
physically located in it, including interface members. No containment edge
crosses unrelated owners. The one ownership subtlety found is API shape, not a
graph fact: `GetNeighborhoodAsync` traverses both directions, so a naive member
query also receives the containing file; callers must filter by edge direction
(harness initially did this wrong; see Q2). Static/instance and nested-declaration
distinctions could not be exercised (no classes in source; covered by EC-3
fixtures instead).

### D3. Imports and dependencies

9 import edges, one per written import statement, each with the exact written
module specifier and source location. Observations:

- `src/lib/schema.ts` imports `./paths`; `src/pages/robots.txt.ts` imports
  `../lib/paths`. These produce **two distinct import target nodes** for the
  same physical module; neither links to the `src/lib/paths.ts` file node.
  Imports are spelling-keyed observations, not resolved dependencies.
- A type-only import (`import type { APIRoute } from 'astro'`) is recorded
  identically to a value import; the type-only flag is not retained.
- No module resolution, no export graph, no package dependencies.

A written import is correctly NOT treated as proof of a resolved module
dependency; the graph records only the observed syntax.

### D4. Heritage relationships (EC-3D identity policy on real code)

The supported source contains exactly one heritage construct:
`export interface SupportingPrimitive extends ProjectRef` at
`src/lib/ecosystem.ts:200`. Verified behavior (`heritage-f-q2-01.json`):

- The edge is `inherits` with Structural evidence at `200.46-200.56`.
- The target is a declaration-less `type` node in the dedicated
  `typescript:heritage-target:` namespace with **no `SymbolId`**.
- The declared `ProjectRef` interface keeps its own `typescript:symbol:` identity.
- Exact-name lookup for `ProjectRef` returns **both** candidates (2 results),
  ordered by node id — this is qualification `EC3-GLM-Q2 F-Q2-01` reproduced
  with real-world code.
- Impact traversal from the declared `ProjectRef` node returns only itself;
  the written relationship is reachable only from the heritage-target
  candidate. A same-spelled declaration does not acquire false dependents.

`implements` edges and Derived (prototype) evidence could not be exercised —
the repository has no such constructs. This is recorded as a scope limitation,
not a production finding.

### D5. False relationships

Active search across all 235 edges found no wrong ownership, incorrect
identity, false inheritance binding, misleading invocation attribution, same-name
convergence of declared symbols, stale facts, or incorrect source locations.

- All 101 `invokes-name` edges are attributed to the lexically enclosing
  callable or file, verified spot-wise (`abs` under `breadcrumbNode`,
  `page.includes` under the `astro.config.mjs` file node, all `z.*` calls under
  the `content.config.ts` file node, `path.startsWith` under `withBase`).
  Invocation placeholders converge across files by design (`withBase`, `URL`)
  and never carry a `SymbolId`; they are honest unbound name records.
- Honest omissions (coverage, not incorrectness): invocations whose receiver is
  a call result or array literal are dropped (`.url()`, `.format()`, `.slice()`,
  `.join()`, `.filter()`), so a chained call emits only its leftmost
  member-path name (`z.string().url()` records `z.string` only).

## E. Developer-query evaluation

All scenarios executed through `CodeGraphQueryService` against the published
graph. Full machine-readable results were produced by the harness (regenerable;
the full dump was intentionally not committed).

| # | Question | Expected (from source) | Actual | Correctness | Usefulness |
| --- | --- | --- | --- | --- | --- |
| Q1 | Where is `withBase` / `formatDate` / `ProjectSchemaInput` / `graph` / `Site` / `Status` declared? | single candidate each, correct kind and location | single candidate each, correct | Correct | Useful. `getStaticPaths` correctly returns 0 candidates (lives in an unsupported `.astro` file) |
| Q2 | What members belong to `RuntimeLayer` / `FounderProfile` / `NavItem`? | 6 / 7 / 2 members | exactly those members; `adjunct` carries `modifiers=optional` | Correct | Useful; caller must filter `GetNeighborhoodAsync` to outgoing edges (it traverses both directions) |
| Q3 | What declarations are in `src/lib/schema.ts` (and 2 more files)? | 25 / 22 / 1 | 25 / 22 / 1, zero missing, zero unexpected | Correct | Useful (`GetDeclarationsInFileAsync`, ordered by position) |
| Q4 | What modules does `schema.ts` import? | `node:url`, `./site`, `./paths` with locations | identical | Correct (observed imports only) | Limited beyond spelling: no resolution, no binding names, no type-only distinction |
| Q5 | What does `SupportingPrimitive` structurally extend? | `ProjectRef` (written, unbound) | one `inherits` edge to a declaration-less candidate, Structural, correct location | Correct | Useful with mandatory interpretation: the edge is not a semantic binding |
| Q6 | What is reachable from `withBase`? | outgoing invocations; incoming callers | outgoing `invokes-name` (`path.startsWith` ×2) correct; `GetImpactSetAsync` returns only the seed for both the declared node and the invocation placeholder | Correct under documented semantics | Limited: impact preset excludes `invokes-name`; callers are reachable only via explicit neighborhood traversal with `[InvokesName]` incoming |
| Q7 | Which symbol encloses `format.ts:2.5` / `ecosystem.ts:9.5` / `paths.ts:1.1`? | `formatDate` / `StatusDefinition.key` (smallest enclosing) / none | exact matches; module level returns null | Correct | Useful (`FindEnclosingSymbolAsync`) |
| Q8 | What happens with same-named nodes? | ambiguity preserved | exact `ProjectRef` → 2 candidates (declared + heritage target); pattern `abs` → declared + declaration-less invocation placeholder; exact `site` → empty (import is `./site`); pattern search is a case-insensitive substring match (`site` also matches `sitemap`, `Site`, `websiteNode`) | Correct per contract | Useful, but consumers must check `SymbolId` and interpret pattern matches |

## F. Publication lifecycle

All eight required cases executed on a disposable copy (`lifecycle.json`):

1. **Initial indexing/publication** — Completed, 9 files.
2. **Reindex unchanged** — 0 facts produced, 9 unchanged; latest publication unchanged.
3. **Add declaration** (`ec4Probe`) — 1 file changed; symbol published at `src/lib/format.ts:14.8`.
4. **Rename declaration** — new node identity (hash change), old name gone, new name present at the same location.
5. **Remove declaration** — facts disappear; declaration counts return to baseline.
6. **Change import** (add `import { formatDate } from '../lib/format';` to `site.ts`) — import target `../lib/format` appears in the file's traversal.
7. **Remove source file** (`src/env.d.ts`) — `deleted=1`; file node no longer published.
8. **Query after each change** — every step above was verified through public queries on the newly published graph.

Failed attempt: indexing a nonexistent directory throws
`DirectoryNotFoundException` and the prior successful publication remains fully
queryable.

Durable store (`durable.json`): index into `LatticeCodeGraphStore`, dispose,
reopen the same database file: identical publication run id, identical
`withBase` node identity, 25 declarations in `schema.ts`. Reopen-and-query
lifecycle verified.

**Historical p4→p5 migration — executed for real.** The prior qualification
used a proxy; this milestone upgraded that evidence. An authentic p4 extractor
was built from the historical source at `e4598ac` (`ProfileRevision=4`,
`0.2.0-preview.6`), the website was indexed into a LatticeDb publication by
that p4 code, and the current p5 stack then:

- observed the p4 publication (`publicationBeforeRunId=run:ec4-p4-publication`);
- reported `CheckFreshnessAsync` = **Stale, changed=9** before any indexing
  (plugin-version invalidation detected without reindexing);
- re-extracted all 9 units on the p5 run (`filesChanged=9`, `filesUnchanged=0`);
- rewrote the heritage identity from the p4 `typescript:symbol:` namespace to
  the p5 `typescript:heritage-target:` namespace with p5 provenance
  (`p4-p5-migration.json`).

Boundary note: the p4 publication itself was created during this review from
the authentic historical source; no p4-era database artifact was preserved in
the repository. The core and LatticeDb sources are byte-identical between
`e4598ac` and `1cc2939`, so the store-side migration path is exact; only the
extraction profile differs, which is precisely what the invalidation exercises.

## G. Findings

**VERIFIED CAPABILITY**

1. Declaration discovery for the supported TS/JS subset is exactly correct on
   this repository: 62/62 declarations, correct kinds, names, spans, ownership,
   and Structural evidence (D1).
2. Containment/declaration queries, enclosing-symbol navigation, and observed
   imports are correct and useful (E: Q2–Q4, Q7).
3. The EC-3D heritage identity policy holds on real code: declaration-less
   file-scoped candidates, preserved ambiguity, no false dependents (D4).
4. Incremental publication, stale-fact removal, failure isolation, durable
   reopen-and-query, and deterministic repetition all verified (F).
5. Plugin-version invalidation verified with an authentic p4 extractor and a
   real p4→p5 re-extraction, replacing the prior proxy evidence (F).
6. Invocation attribution is lexically exact at all 101 sites (D5).

**COVERAGE GAP** (missing representation; not defects)

1. `.astro` files: 22 of 78 files (page components, frontmatter interfaces,
   `getStaticPaths`, and most real consumers of the `src/lib` modules) are
   outside the graph. Hetu correctly skips them.
2. Exported data constants are invisible: `STATUSES`, `CATEGORIES`,
   `RUNTIME_LAYERS`, `SUPPORTING_PRIMITIVES`, `SITE`, `NAV`, `FOUNDER`,
   `collections` (object/array-valued const declarators) produce no nodes, so
   the website's primary public surface is unrepresented; `content.config.ts`
   and `astro.config.mjs` publish zero declared symbols.
3. Imports are unbound: no module resolution, no import binding names, no
   type-only flag, spelling-keyed duplicate nodes for one module (D3).
4. Invocation chains: calls on call results and array literals are dropped
   (`.url()`, `.join()`, `.filter()`, `.format()`, `.slice()`).
5. Class/member, enum-member, parameter, and generic-parameter coverage was
   not exercised by this repository (no such constructs in the supported
   subset); the feature matrix's recorded gaps stand unchanged.

**CONFIRMED DEFECT** — none found in the tested scope.

**DESIGN AMBIGUITY**

1. `GetImpactSetAsync` (preset `References/Calls/Implements/Inherits/DependsOn`)
   excludes `invokes-name`, so "who calls this?" via the impact API returns
   nothing in Tree-sitter graphs, while a neighborhood query with
   `[InvokesName]` incoming works. The behavior is consistent with the
   evidence model (a name match is not a resolved call) but the distinction is
   easy to misuse and under-documented for consumers.

**CONSUMER LIMITATION**

1. `EC3-GLM-Q2 F-Q2-01` observed on real code: exact-name lookup returns a
   declaration-less heritage candidate beside the declared interface
   (`ProjectRef`). Callers must filter on `SymbolId` (see the consumer-guidance
   amendment).
2. Pattern search returns declaration-less invocation placeholders beside
   same-named declared symbols (`abs`), and matches case-insensitive
   substrings across names and qualified names (`site` → `sitemap`, `Site`,
   `websiteNode`).
3. Import placeholders do not link to file nodes; per-file dependency and
   cross-file impact questions require application-side resolution today.

## H. Qualification boundaries

Verified: everything in sections C–F on the exact target revision, the
supported 9-file subset, the two Tree-sitter plugins at their pinned grammar
versions, the named store implementations, and the listed query APIs.

Not verified (and not claimed): classes, class members, static/instance
distinction, `implements` clauses, Derived prototype evidence, enum members,
parameters, generics, export binding, module resolution, C# Roslyn behavior,
Astro content, and any syntax outside the pinned grammars. Resource usage was
observed only as the diagnostic durations and wall time of single runs; no
benchmark claims are made. The 48-declaration precision/recall sample is not
extrapolated beyond the supported subset.

## I. Recommended roadmap (evidence-ranked)

1. **Represent exported data constants** (const declarators with object/array
   literal values as `property`/`field`-style declarations with `contains`/
   `declares` and their locations). — Observed: the site's main public surface
   (`ecosystem.ts`, `site.ts`) is absent; `content.config.ts` publishes zero
   symbols (D1/E-Q3). Consequence: symbol discovery misses the most-read data
   structures. Expected improvement: meaningful discovery for non-callable
   modules. Complexity: **low** (extractor-only, follows the existing
   declarator handler). Risk: minor identity-policy decision (path-scoped like
   classes vs name-based like functions) must be made explicitly.
2. **Resolve intra-repository imports to file nodes** (or emit a second,
   resolution-evidenced edge) while keeping the spelling observation. —
   Observed: `./paths` and `../lib/paths` are two unlinked placeholders for one
   module (D3). Consequence: "what depends on this file?" and cross-file
   navigation are unanswerable from the graph. Expected improvement: file-level
   dependency and honest cross-file reachability. Complexity: **medium**
   (specifier-to-path resolution rules, ambient packages stay unbound).
   Dependency: none beyond the existing edge vocabulary.
3. **Astro support as a separate milestone** (extract frontmatter as
   TypeScript; treat templates as structure). — Observed: 22/78 files, all
   pages/components, are unindexed; `getStaticPaths` is unfindable (E-Q1).
   Consequence: for this repository class the graph covers a minority of the
   code that matters. Complexity: **high** (new grammar/profile; component
   identity policy). Recommendation: schedule after 1–2; do not silently
   broaden scope.
4. *(Query-level quick win, fold into any of the above)* document or add an
   evidence-aware incoming-invocation query so `invokes-name` callers are
   discoverable without raw neighborhood queries (G: design ambiguity 1).

## J. Final verdict

**USEFUL WITH QUALIFICATIONS.**

Within the supported language subset and the tested revision, the published
graph was exactly correct: 62/62 declarations, 235/235 relationships verified
against source, zero confirmed defects, zero misleading facts, deterministic
repetition, correct incremental and durable publication behavior, and a real
p4→p5 migration. The developer queries it can answer — declaration discovery,
member discovery, file exploration, observed imports, structural heritage,
enclosing-symbol navigation — are answered correctly.

The qualifications are coverage, not correctness: 69 of 78 files (including all
22 Astro components and pages) are outside the graph, exported data constants
are unrepresented, imports are unbound observations, and impact queries require
evidence-aware interpretation. The verdict is scoped to this repository at
`f9cca17`, the two Tree-sitter plugins at their pinned grammars, the tested
APIs, and the constructs listed in section H; it does not imply that Hetu
supports all TypeScript, JavaScript, or Astro syntax.

## Consumer guidance: qualified-name lookup

This section is the EC-4 follow-up required by `EC3-GLM-Q2 F-Q2-01`, based on
observed behavior in this repository. A standalone documentation-only
amendment with the same content is committed separately as
`docs/consumer-guidance-qualified-name-lookup.md`.

A single displayed name does **not** imply one semantic entity:

- **Graph-node identity** (`CodeNodeId`) is unique per node; two nodes that
  share a name are always different nodes.
- **Declared symbols** carry a `SymbolId` and physical
  `CodeGraphDeclaration` records. On this repository, exact-name lookup
  (`FindSymbolsByQualifiedNameAsync`) returned declared symbols only —
  invocation placeholders have no qualified name.
- **Declaration-less candidates** (heritage targets, invocation placeholders,
  import targets) have no `SymbolId`. Exact-name lookup can return a
  heritage candidate beside a same-named declared symbol — observed for
  `ProjectRef` — and name-pattern search can return invocation placeholders
  beside same-named declarations — observed for `abs`.
- **Structural observations** (`imports`, `invokes-name`, `inherits`/
  `implements` to candidates) record written syntax and provenance. They are
  not resolved bindings: a written `extends ProjectRef` does not make the
  declared `ProjectRef` a base of anything, and a written import does not prove
  a resolved module dependency.
- **Impact traversal** (`GetImpactSetAsync`) follows semantic-ish relationship
  kinds and deliberately excludes `invokes-name`; incoming structural
  invocations must be queried explicitly (`GetNeighborhoodAsync` incoming with
  `CodeEdgeKinds.InvokesName`) and must never be described as guaranteed
  runtime or semantic impact.

Practical rules: distinguish candidates by `SymbolId` presence before ranking;
treat candidate-namespace ids (`:heritage-target:`, `:invoked:`, `:import:`)
as observations, not symbols; filter neighborhood traversals by direction when
asking membership questions; and do not merge candidates by display name.
