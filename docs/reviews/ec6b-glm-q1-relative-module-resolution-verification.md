# EC6B-GLM-Q1: Independent verification of EC-6B relative module resolution

- **Review ID:** `EC6B-GLM-Q1`
- **Reviewer:** GLM
- **Review type:** independent adversarial implementation and qualification review
- **Milestone:** EC-6B, Conservative Relative Module Resolution
- **Date:** 2026-10-25
- **Production remediation:** not performed by this review
- **Result:** **REMEDIATION REQUIRED** (three confirmed production defects,
  one contract ambiguity; see §J)

Everything below was established independently in an isolated review
worktree. No production source, implementation test, handoff evidence,
decision document, or canonical branch was modified. Temporary execution
artifacts (website snapshot, p6 throwaway clone, databases, snapshots) live
outside the repository and are not committed.

---

## A. Git binding

| Item | Verified identity |
| --- | --- |
| Repository remote | `https://github.com/jenolaszlo-sketch/penghou-hetu.git` (both worktrees; directory names were not trusted) |
| Qualified production baseline | `558f8ac82ac1b5e07da68f4a71a70240eaf38409`, canonical `main`; confirmed an ancestor of the accepted decision tip |
| Accepted architecture branch | `decisions/ec6a-owner-acceptance` = `1d11b7330e1d8755d5c3e3b4a830167bcf8b5bf6` (tree `649b3111a273478547d8542770220e043fd5cbd5`), "docs: record the review-brief decision-identifier reconciliation" |
| EC-6B implementation commit | `8aa083ce36e52f666578f5ed6b539ec194178561` (tree `52f877acb0daf2cdf41fedf2d1d1fb0ff6fda034`), "Implement EC-6B relative module resolution"; parent is exactly `1d11b73` |
| Documentation/handoff commit | `691a8327ab7446416de22fe26e023662399b6cf2` (tree `f2bb8c5faa301fdfa8542fc1d39aaeec68040356`), "Document EC-6B resolution implementation"; direct child of `8aa083c`; **modifies documentation only** (CHANGELOG.md, README.md, ROADMAP.md, `docs/ecmascript-module-resolution.md`, `docs/ecmascript-symbol-identity.md`, `docs/evidence/ec6b-relative-module-resolution-handoff.md`, `docs/language-feature-matrix.md`, `docs/language-support-roadmap.md`) |
| Implementation branch | `implementation/ec6b-relative-module-resolution` at `691a832`; the reported worktree `C:\Users\Laszlos\source\repos\Solo\ec6b-relative-module-resolution` was clean at review start |
| Reviewed source revision | exactly `691a8327ab7446416de22fe26e023662399b6cf2` |
| Review worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6b-glm-q1` |
| Review branch | `review/ec6b-glm-q1`, created from `691a832` with `git worktree add … -b` |
| Review preservation commit | this commit; parent `691a8327ab7446416de22fe26e023662399b6cf2`. Its full hash and tree are recorded in the review completion message because this report is part of that commit |

Ancestry verified: `558f8ac` → `1d11b73` → `8aa083c` → `691a832` (linear; parent
links and `git merge-base --is-ancestor` checked). The expected final tree
`f2bb8c5…` matches. No pull, rebase, merge, reset, or push was performed.

## B. Contract compliance

Sources read in full: the owner-accepted architecture
(`docs/design/ec6a-cross-file-resolution-architecture.md`), adversarial
cases AC-40–AC-53, the EC-6B roadmap section and acceptance matrix, the
decision register, `docs/decisions/ec6a-owner-acceptance.md`, the new
consumer doc `docs/ecmascript-module-resolution.md`, the updated identity doc,
and the handoff. Implementation inspected: `TreeSitterModuleResolution.cs`,
`EcmaScriptExtractionProfile.cs`, `TreeSitterExtractionSession.cs`, both
ECMAScript plugins, `CodeGraphKinds.cs`, `CodePropertyKeys.cs`,
`TreeSitterSyntax.cs`, `CodeGraphIngestionSink.cs`, `InMemoryCodeGraphStore.cs`
staging/completion/validation paths, `LatticeCodeGraphStore.cs` replay and
persistence, `CodeIndexingLifecycle.cs`, `CodeGraphTraversalPresets.cs`,
planner and coverage contracts.

| Contract area | Result |
| --- | --- |
| §6.2 relative qualification, normalization, ordered matching, root escape, classification table | Compliant in all prose rows **except** escaped specifiers (F02) |
| §7.1.1 identity formula, discriminator, span, occurrence anchoring, LF/CRLF, multiplicity, collision rejection | Compliant; independent recomputation matched every emitted id (probes 03, 04, 16, 19) |
| §7.1.1 coordinate convention ("one-based **byte** columns") | **Not met**; columns are UTF-16 code units (F01) |
| §7.2 property semantics (`specifier`, `module-reference`, `resolution-policy`, `resolution-rule`, `type-only`) | Compliant except `type-only` false positives for bindings named `type` (F03) |
| §8 evidence: `Resolved`, distinct provider id, policy-bearing provider version, justification properties, unit origin, no confidence | Compliant (probes 05, 15, 16) |
| §9.1 unit `{language}:modules`, edges only, always re-emitted, empty completion clears | Compliant (probes 05–09) |
| §9.1.1 ordered staging; §9.1.2 staged invisibility; §9.1.3 lifecycle | Compliant (probes 05–09; wrong-order rejection reproduced) |
| §9.4.1 determinism | Compliant (probes 01, 10, 19) |
| §9.4.2 cancellation | Compliant (probe 08; source-level token checks between files, before each batch, before completion) |
| §9.4.3 failure taxonomy and fatal handling | Compliant (probes 08, 09, 21 evidence of capture defect) |
| §12.2 coverage accounting and counters | Compliant (`EdgesEmitted == Internal + CrossProject` enforced by construction; probes 16, 17) |
| §13 compatibility: no identity changes, no node kinds, presets unchanged | Compliant (probes 13, 14) |
| D-20 absence semantics and the mandated `Resolved` sentence verbatim | Present in both consumer docs and the handoff |

Reported implementation claims reproduced exactly: 344 solution tests
(Tree-sitter 112, core 160, C# 46, LatticeDb 26), 22 focused module-resolution
cases, formatter verification exit 0, clean `git diff --check`, authentic
p6→p7 preservation, and three website resolver edges. The qualification
claims are accurate **except** the "byte columns" statement (F01).

## C. Resolution-policy correctness

Established by execution (`ReviewEc6bGlmQ1Probes`, `…CaptureProbe`,
`…ImportFormProbe`) and source inspection:

- **Precedence is declared policy, not enumeration order.** `./mod` chooses
  `mod.ts` over `mod.tsx` (ordered suffix list); `./both` chooses `both.ts`
  over `both/index.ts` (file before index); reversing source enumeration
  produces byte-identical normalized facts (probe 01, 10).
- **Boundaries.** `../../../escape` → Unsupported; `../../root-file` → root
  file; `./case` vs `Case.ts` → Unresolved (ordinal); `./café` → `café.ts`
  (non-ASCII ordinal match); every resolved target is a file node already in
  the plugin inventory (probe 02). The resolver consults only the in-memory
  inventory; it never probes the filesystem (source inspection).
- **Rejected forms.** Query/fragment, repeated separators, absolute paths,
  trailing separators, bare `./` → Unsupported with counts (probe 18).
  Backslash forms are **not** correctly rejected (F02).
- **Extension rules.** `.js` specifier with only `.ts` present → Unresolved
  (D-08); `.json` and `.astro` targets → Unsupported; `./types` → `types.d.ts`;
  `./dir` → `dir/index.ts`; explicit `.d.ts` → `exact`; explicit `.ts` with no
  such file → Unresolved (probe 16). Present-but-unindexed files never enter
  the inventory and never receive file nodes (probe 20).
- **Legacy forms.** `import x = require(…)`, `import type X = require(…)`,
  dynamic `import('…')`, template `require` → Unsupported, no edge; literal
  `require('…')` → edge with `module-reference=require`; inline and whole
  statement type-only forms produce `type-only` correctly except F03
  (probes 17, 17b).
- **Ambiguity.** The policy's ordered precedence means no equal-precedence
  ambiguity is reachable; `AmbiguousTargets` is always 0 (probes assert this).
- **Self-reference** counts `InternalTargets` (probe 16).

**F02 is the material exception.** The raw specifier is taken from the first
`string_fragment` child of the string literal. Escape sequences split the
literal, so the text after an escape is silently dropped and the backslash
never reaches the policy:

```typescript
import './a\b';        // captured as "./a"  → false resolves-to a.ts
import './caf\u00e9';  // captured as "./caf" → false resolves-to caf.ts
```

This contradicts §6.2 ("Nothing is trimmed and nothing is decoded; the exact
written text is preserved…"), the backslash/query Unsupported rules of
§6.2.2/§6.2.3, the discriminator contract of §7.1.1, and the `Resolved`
evidence claim of §8. The same truncation made `./a\b` count as *Unresolved*
instead of *Unsupported* in a fixture without `a.ts`. Because TypeScript
requires a literal backslash to be written as `\\`, every backslash-bearing
specifier is affected, and the documented "any backslash" rejection row is
effectively unreachable through fragments.

## D. Evidence and identities

- Every emitted `resolves-to` edge observed in this review was `Resolved`,
  carried provider `penghou.hetu.tree-sitter.ecmascript-module-resolver` and
  version `{packageVersion}+resolver1.relative-v1`, and recorded the raw
  specifier, module-reference, policy, rule, and (only when written
  explicitly) `type-only` (probes 05, 15, 16, 17).
- Both endpoints were always existing file nodes; no fabricated nodes, no
  `exports`, no `binds`, no symbol or call resolution, no semantic
  import-to-declaration claim anywhere in the tested graphs (probes 15, 16,
  17, 17b).
- Structural `imports` edges remained `Structural`, with their placeholders
  and identities unchanged (probes 06, 12; migration).
- Edge ids recomputed independently from `(kind, source, target,
  discriminator, span)` matched every emitted id (probes 03, 04, 16).
- Multiplicity held: identical specifiers on one line are two edges with
  distinct byte/character columns (probe 03); two specifiers to one target,
  shared targets across files, and occurrence removal behaved per AC-46–AC-49
  (implementation fixtures reproduced; probe 15 shows one target reached from
  two importers).
- LF and CRLF variants produced identical ids and evidence locations
  (probe 19).
- **F01:** the coordinates embedded in spans and identities are UTF-16 code
  units, not the "byte columns" the accepted contract and shipped docs claim.
  For `const s = "中文 café"; import './a';` the reported one-based column is
  22 (UTF-16/char) while the UTF-8 byte column is 27. The pinned
  `TreeSitter.DotNet 1.3.0` binding parses .NET strings as UTF-16LE
  (`ts_parser_parse_string_encoding(..., InputEncoding.UTF16LE)`), so
  tree-sitter's columns count UTF-16 code units. Identity remains
  deterministic, occurrence-anchored, and line-ending-stable; the defect is
  the documented convention and the consumer-facing "byte" slices.
- **F03:** specifiers whose *binding name* is `type`
  (`import { type } …`, `import { type as t } …`, `export { type as u } …`)
  are marked `type-only: true` although no explicit type-only modifier is
  written (probe 22). The property is justification metadata, not an identity
  input; edges and targets remain correct, but the flag is false.

## E. Publication verification

Executed through `CodeIndexingService` on both `InMemoryCodeGraphStore` and
`LatticeCodeGraphStore` (durable runs also reopened):

| Scenario | Result |
| --- | --- |
| P1/target added while importer unchanged | Edge and target file node appear in one completed publication; unresolved count returns to 0; module unit holds the new edge (probe 05) |
| P2/target removed | No resolver edge survives; target file node absent; structural `imports` observation remains; unresolved count 1 (probe 06) |
| P3/importer removed | No outgoing resolver edge; importer file node removed (implementation lifecycle fixture reproduced on both stores) |
| P4/specifier changed | Old edge replaced by the new target's edge with a different id (implementation fixture reproduced) |
| P5/empty resolution | The stable `{language}:modules` unit is published empty and no stale resolver edge remains anywhere (probe 07) |
| P6/resolver failure after file staging | Run `Failed`, staged footprint discarded, previous publication and previous resolution-unit contents retained (implementation fixture reproduced; probe 08 proves unit contents) |
| P6b/ingestion-limit failure mid module unit | Module batches are accumulated in the sink and staged only at unit completion; a unit that exceeds `MaxFactsPerIndexUnit` is poisoned; the run `Failed`; previous publication and all five prior edges retained (probe 09) |
| P7/cancellation before publication | Run `Cancelled`; previous publication including its exact resolver-edge ids retained; token observed between files, before each batch write, and before the completing batch (probe 08; source) |
| P8/unchanged reindex | Plugin does not execute; previous publication reused (implementation fixture: `PluginsExecuted == 0`) |
| P9/policy or version change | The effective plugin version (`.p6` → `.p7`) forces re-extraction of unchanged sources; both plugins executed in the authentic migration (probe 13) |
| P10/process interruption | Source-level guarantee only: staged units and staged runs are durable logs but are never published until `CompleteIndexRunAsync`; the native mirror is rebuilt only on completion; a reopened store publishes only completed state. Not executed with a killed process (evidence limitation) |
| Wrong staging order | Directly staging `typescript:modules` before file units is rejected with `graph.edge.endpoint-missing` (implementation fixture reproduced on both stores) |

No partial resolver edge set was visible in any observed failure or
cancellation. Endpoint validation was never weakened.

## F. Determinism

Repeated extraction of identical input produced byte-identical normalized
facts (ids, endpoints, properties, evidence locations), including with the
source enumeration reversed at the extraction and resolver seams (probes 01,
10). LF/CRLF input produced identical ids (probe 19). The resolver sorts
files ordinally, orders sites by location, sorts emitted edges by
`(kind, source, target, id)`, batches from that order, and uses no unordered
iteration for decisions (source inspection). Independent runs of the website
probe produced identical counts and edges. Failures, unsupported, external,
unresolved, and ambiguous outcomes are distinguished by counters exactly as
§12.2 requires; a fatal failure never degrades into an empty successful
publication (probes 08, 09).

## G. Compatibility (p6 → p7)

An authentic historical migration was executed: a throwaway local clone
checked out at the accepted decision commit `1d11b73` (real p6 extractor,
plugin version `0.2.0-preview.6+v0.23.2.abi14.p6`) seeded a durable
`LatticeCodeGraphStore` from the isolated website snapshot at
`jenolaszlo-sketch/website@f9cca177…`; the review worktree (`691a832`, p7)
then reopened that database and reindexed the unchanged sources.

| Measure | p6 | p7 |
| --- | --- | --- |
| Units | 9 | 11 (adds `typescript:modules`, `javascript:modules`) |
| Distinct node ids | 128 | 128 |
| Declarations | 83 | 83 |
| Edges | 277 | 280 |
| imports / inherits / invokes-name / contains / declares | 9 / 1 / 101 / 83 / 83 | identical |

Every p6 `NODE`, `DECL`, and `EDGE` line survived byte-identically
(`missing p6 lines: 0`). The only additions were exactly three `resolves-to`
edges. Both plugins executed during the transition although content was
unchanged, proving version-driven invalidation. No mixed-version publication
was observed. The store roundtrip of the p6 snapshot matched the
p6-extractor-written snapshot exactly.

## H. Real-repository qualification

Snapshot: an isolated checkout of `jenolaszlo-sketch/website` at exactly
`f9cca1773a32bb517e624cc380e1f6d14e0cc317` (78 tracked files). All nine
indexed blobs matched the handoff's SHA list via `git rev-parse HEAD:<path>`:
`astro.config.mjs`, `src/content.config.ts`, `src/env.d.ts`,
`src/lib/ecosystem.ts`, `src/lib/format.ts`, `src/lib/paths.ts`,
`src/lib/schema.ts`, `src/lib/site.ts`, `src/pages/robots.txt.ts`.

Independent ground truth: the nine files contain exactly three supported
relative references — `schema.ts` lines 2–3 (`./site`, `./paths`) and
`robots.txt.ts` line 2 (`../lib/paths`) — and six external references
(`node:url`, `astro`, `astro:content`, `astro/loaders`, `astro/config`,
`@astrojs/sitemap`).

Executed p7 indexing produced exactly the expected graph:

- 128 distinct nodes, 83 declarations, 280 edges
  (contains 83, declares 83, imports 9, inherits 1, invokes-name 101,
  resolves-to 3).
- The three resolver edges match ground truth one-for-one:
  `src/lib/schema.ts → src/lib/site.ts` (`./site`, extension-substitution),
  `src/lib/schema.ts → src/lib/paths.ts` (`./paths`),
  `src/pages/robots.txt.ts → src/lib/paths.ts` (`../lib/paths`), all
  `Resolved` under `relative-v1`, at statement spans.
- No unsupported reference fabricated an edge; no `exports`/`binds` fact
  exists. A consumer query with an explicit `[resolves-to]` filter from the
  schema file returns exactly its two dependencies. "Which exported
  declaration does this imported name bind to?" remains unanswerable by
  design.
- The EC-5 baseline (128 nodes, 83 declarations, 277 structural edges) is
  preserved. The handoff's three-edge claim is accurate.

## I. Adversarial probes

All probes are preserved in the review branch
(`tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc6bGlmQ1*.cs`) and marked
review-only. 28 executed cases; all green (the two defect-pinning probes
assert the observed defective behavior, not the violated expectation).

| Probe | Challenge | Result |
| --- | --- | --- |
| 01 | `.ts` vs `.tsx`, file vs index, reversed enumeration | Held: declared precedence, identical facts |
| 02 | root escape, case mismatch, non-ASCII ordinal, inventory containment | Held |
| 03 | two identical specifiers on one line | Held: two distinct ids, byte-column ordering |
| 04 | multibyte UTF-8 before the statement | **Failed contract**: columns are UTF-16 units (F01) |
| 05 (×2 stores) | target addition without importer edit | Held |
| 06 (×2) | target deletion, no dangling endpoints | Held |
| 07 (×2) | empty resolution clears publication | Held |
| 08 (×2) | cancellation retains previous unit contents | Held |
| 09 (×2) | mid-module-unit ingestion failure | Held: nothing partial published |
| 10 | source/inventory enumeration order | Held: identical facts |
| 12 | TS↔JS cross-plugin references | Held: declined both directions |
| 13 | authentic p6→p7 migration | Held: 0 missing p6 lines, +3 edges |
| 14 (×2) | impact presets exclude `resolves-to` | Held |
| 15 | website ground truth | Held: exact three targets |
| 16 | extension/index/`.d.ts`/self-import/classification | Held |
| 17 | import-equals, dynamic/template forms, type forms | Held |
| 17b | `export = require('./m')` | Observed nested `require` edge (F04 ambiguity) |
| 18 | query/fragment/repeated/absolute/trailing forms | Held (escape case moved to 21) |
| 19 | LF vs CRLF identity | Held |
| 20 | physically present but unindexed targets | Held |
| 21 | escaped specifiers | **Failed contract**: truncation and false edges (F02) |
| 22 | binding literally named `type` | **Failed contract**: false `type-only` (F03) |

Reproduction notes: probe 21 shows `specifier='./a' → src/a.ts` and
`specifier='./caf' → src/caf.ts` for the written texts `./a\b` and
`./caf\u00e9`; probe 22 shows `typeOnly=True` for all three `{ type … }`
forms.

## J. Findings

### EC6B-GLM-F01 — Published columns are UTF-16 code units, not the contract's byte columns

- **Disposition:** CONFIRMED DEFECT (P3) and CONFIRMED QUALIFICATION DEFECT
  (same root; handoff §D and consumer docs state "byte columns").
- **Contract:** architecture §7.1.1 ("one-based **byte** columns
  (Tree-sitter columns are byte offsets within a `\n`-delimited row;
  `TreeSitterSyntax.cs:39-50`)"); owner acceptance §5; new
  `docs/ecmascript-module-resolution.md`; `docs/ecmascript-symbol-identity.md`;
  handoff §D.
- **Implementation source:** `src/Penghou.Hetu.TreeSitter/TreeSitterSyntax.cs:39-50`
  (claim), `EcmaScriptExtractionProfile.cs:1223-1229` (uses reported
  positions), `TreeSitter.DotNet` 1.3.0 `Parser.Parse(string)`
  (`ts_parser_parse_string_encoding(..., InputEncoding.UTF16LE)`).
- **Counterexample:** `const s = "中文 café"; import './a';` → StartColumn 22
  (UTF-16 code units) versus 27 (UTF-8 bytes).
- **Expected:** the accepted contract's byte columns. **Observed:** UTF-16
  code-unit columns (equal to .NET string indices for BMP text).
- **Graph/query consequences:** identities remain deterministic and stable;
  consumers converting locations to UTF-8 byte offsets mislocate statements
  in non-ASCII files; the handoff's evidence claim is materially inaccurate.
  The behavior predates EC-6B; EC-6B re-documented it.
- **Suggested remediation:** correct the convention in the source comment and
  all four documents to "UTF-16 code-unit columns" (no code change, no
  identity migration). A true byte-column change would alter every existing
  edge id and belongs to a separate identity-migration milestone.
- **Uncertainty:** none about behavior; impact depends on consumer use.

### EC6B-GLM-F02 — Escaped specifiers are truncated, bypassing the backslash rejection and emitting false `Resolved` edges

- **Disposition:** CONFIRMED DEFECT (P2).
- **Contract:** architecture §6.2 ("Nothing is trimmed and nothing is
  decoded; the exact written text is preserved in the `specifier` property
  and is never normalized away"; any backslash → Unsupported), §6.2.2,
  §6.2.3 (`.\\a`, `./a\b` → Unsupported), §7.1.1 (raw-specifier-text
  discriminator), §8 (`Resolved` claim).
- **Implementation source:** `EcmaScriptExtractionProfile.cs:409-427`
  (`RawModuleSpecifier` takes only the first `string_fragment`).
- **Counterexample:** `import './a\b';` with `src/a.ts` present →
  `resolves-to` edge with `specifier=./a` to `src/a.ts`;
  `import './caf\u00e9';` with `src/caf.ts` present → `specifier=./caf`.
  Expected: no edge, `unsupported-targets` (probe 21).
- **Execution evidence:** probe 21 (2 false edges, 0 unsupported); probe 18
  first run (`./a\b` counted Unresolved instead of Unsupported).
- **Graph/query consequences:** false verified module dependencies whose
  written specifier does not exist; wrong `specifier` property and
  discriminator; consumers cannot distinguish these from genuine matches.
  The documented backslash row is effectively unreachable through fragments.
- **Suggested remediation:** capture the full string-literal source text
  (strip only the quotes; never decode, never take a single fragment), then
  apply the existing policy checks. Add escaped-specifier fixtures to the
  EC-6B identity/classification suite.
- **Uncertainty:** prevalence in real repositories (legal but uncommon).

### EC6B-GLM-F03 — `type-only: true` false positive for bindings named `type`

- **Disposition:** CONFIRMED DEFECT (P3).
- **Contract:** architecture §7.2 (`type-only` present only when the written
  syntax is explicitly type-only: `import type`, `export type … from`,
  `type` specifier modifier).
- **Implementation source:** `EcmaScriptExtractionProfile.cs:429-461`
  (`AllSpecifiersAreTypeOnly` + `HasPrefixTokens` treat a bare `type`
  identifier as a modifier).
- **Counterexample:** `import { type } from './m';`,
  `import { type as t } from './m';`, `export { type as u } from './m';`
  all carry `type-only: true` (probe 22). These are value bindings whose
  name is `type`.
- **Expected:** property absent. **Observed:** property present, `true`.
- **Graph/query consequences:** edge justification metadata is inaccurate;
  no identity, target, or edge-existence change.
- **Suggested remediation:** detect the modifier at the syntax level (a
  `type` token followed by an imported/exported name; exclude `as`, `,`,
  `}`), rather than by text prefix.
- **Uncertainty:** none.

### EC6B-GLM-F04 — Legacy module forms are classified asymmetrically

- **Disposition:** DESIGN OR CONTRACT AMBIGUITY (P3).
- **Contract:** §6.3/§7.4 list `import x = require('./m')` and `export =` as
  unsupported; §7.4 also supports literal `require` at any nesting depth.
  The documents do not say whether a literal `require` nested inside an
  explicitly unsupported form is a separate supported site.
- **Observed:** `import x = require('./item')` and
  `import type X = require('./item')` yield no edge (one unsupported site
  each), while `export = require('./item')` yields one unsupported site plus
  one `resolves-to` edge attributed to the nested call span with
  `module-reference=require` (probe 17b).
- **Assessment:** no false fact is fabricated — the edge is attributed to a
  real literal `require` call and no export/bind fact is produced — but the
  asymmetry is undocumented and could surprise consumers.
- **Suggested remediation:** state the rule explicitly in the policy
  (either "unsupported statement forms suppress nested captures" or "literal
  `require` is captured wherever written") and align the implementation.

### Reproduced without finding

- AC-41–AC-45 lifecycle, AC-46–AC-53 identity/multiplicity, wrong-order
  rejection, durable reopen, cross-plugin decline, impact-preset exclusion,
  and the three website edges all reproduced correctly.
- No stale resolver edge, no dangling endpoint, no partial publication, no
  duplicate-identity suppression, and no identity drift were observed in any
  probe or in the authentic migration.

## K. Qualification boundaries

Established: `relative-v1` correctness for the probed input classes on
Windows with .NET SDK 10.0.401 / `net10.0`; both stores; the exact website
revision; an authentic p6→p7 migration; cancellation and failure retention;
determinism; preset compatibility.

Not established / not claimed:

- General TypeScript compiler, bundler, Node.js, or runtime equivalence.
- Cross-plugin TypeScript↔JavaScript resolution (documented limitation) or
  non-ECMAScript formats.
- Symlink handling was verified only by source inspection (repository
  discovery refuses reparse points); no OS-level symlink fixture was executed.
- Process-kill interruption (P10) was verified at source level only.
- Large-repository resource behavior and degradation numbers remain EC-6E/6F
  work; no benchmark claims are made here.
- Behavior of escaped specifiers and `type`-named bindings is established as
  defective (F02/F03), not as a limitation.
- The p6 seed used a throwaway local clone with an uncommitted review-only
  probe and a review-only LatticeDb project reference added to the p6 test
  project; no branch or published evidence was modified.

## L. Final verdict

**REMEDIATION REQUIRED.**

EC-6B is correct across the broad tested surface — policy classification,
deterministic identity, evidence, unit ownership, atomic publication,
failure/cancellation retention, p6→p7 compatibility, and real-repository
targets — but two confirmed production defects violate the accepted contract
in a way that affects resolution correctness and evidence: escaped specifiers
produce false `Resolved` relationships (F02), and bindings named `type` carry
a false `type-only` property (F03). The coordinate convention documented in
the accepted architecture, consumer docs, and handoff is also factually wrong
(F01). Because a false verified dependency is exactly the failure mode the
conservative policy exists to prevent, EC-6B cannot close as verified until
F02 (and preferably F03/F01 wording) is remediated and re-reviewed. The
defects are localized to specifier capture and property derivation; identity,
publication, and lifecycle machinery held under every probe.

## M. Owner decision requirements

1. **EC-6B closure:** withhold closure. Require a minimal remediation of F02
   (full raw string text) and F03 (syntax-aware type modifier), plus a
   documentation correction for F01, followed by re-execution of the EC-6B
   qualification (this review's probes 04, 18, 21, 22 are ready-made
   regression tests).
2. **F04:** choose and document the intended legacy-form rule so the
   asymmetry is contractual rather than accidental.
3. **EC-6C:** EC-6C design may start only after the owner accepts the
   remediation plan and the EC-6B re-review passes. EC-6C's export/binding
   work depends on the same shape capture that F02/F03 touch; starting it
   against the current capture would propagate the defects.
4. **No merge to `main`** and no EC-6B authorization beyond qualification were
   performed or implied by this review.

---

## Appendix: execution record

- OS: Windows 10.0.26200 (win-x64); .NET SDK 10.0.401; target `net10.0`.
- Review worktree baseline: `691a832`; implementation tests before review
  probes: `dotnet test Penghou.Hetu.slnx -m:1 --nologo` → **344 passed**
  (C# 46, LatticeDb 26, core 160, Tree-sitter 112); focused
  `FullyQualifiedName~TreeSitterModuleResolution` → **22 passed**.
- Formatter: `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore`
  → exit 0 on the pristine implementation tree and with review probes present.
- Review probes: **28 passed** (including env-gated website and migration
  probes executed with `EC6B_WEBSITE_SNAPSHOT`, `EC6B_P6_DB`,
  `EC6B_EVIDENCE_PATH`); full solution with probes → **372 passed**.
- Website snapshot: `git clone` + detached checkout of
  `f9cca1773a32bb517e624cc380e1f6d14e0cc317`; all nine blobs verified.
- p6 seed: throwaway clone at `1d11b73`, uncommitted review probe and
  LatticeDb reference; durable database; p6 snapshot 128/83/277.
- p7 migration: reopened the same database, reindexed, 0 lost p6 lines,
  exactly 3 added `resolves-to` edges; both plugins executed.
- `git diff --check` clean; worktree contained only the five review probe
  files and this report before the preservation commit.
