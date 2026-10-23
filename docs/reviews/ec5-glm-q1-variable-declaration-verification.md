# EC5-GLM-Q1 — EC-5 Variable Declaration Verification

**Project:** Penghou.Hetu (future Andirit)
**Reviewer:** GLM (independent)
**Review type:** Independent adversarial implementation and qualification review
**Milestone:** EC5-GLM-Q1
**Scope:** EC-5 named file-level TypeScript/JavaScript variable extraction and its integration with the existing graph contracts
**Review date:** 2026-10-23
**Environment:** Windows 10.0.26100 x64, .NET SDK 10.0.401, `dotnet test` / `dotnet run` on the pinned grammars shipped in the repository.

---

## A. Git binding

All identities were re-verified at review start; nothing below reviews a different revision.

| Item | Identity |
| --- | --- |
| Implementation repository | `jenolaszlo-sketch/penghou-hetu`, canonical checkout `C:\Users\Laszlos\source\repos\Penghou.Hetu` |
| Canonical `main` at review start/end | `1cc2939ea615d627cde616d06e739867be4e56e2` (clean working tree; verified unchanged after review) |
| Implementation branch | `implementation/ec5-variable-declarations` = `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2`, ancestry verified linear: `1cc2939` → `c7657f8` (extractor) → `e77deea` (tests) → `5a7c59c` (docs) |
| Implementation tip tree | `153a426ac8925d52b6f37cd7089872a86b444c97` (verified `git show -s --format=%T`) |
| Review worktree | `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec5` |
| Review branch | `review/ec5-glm-q1`, created from exactly `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2` |
| Real-repository target | `jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317` (tip of that repository, clean); reviewed through a `git archive` snapshot (78 files) extracted to `C:\Windows\Temp\opencode\ec5-website-snapshot`; the target checkout was never modified |

Isolation: no pull, rebase, reset, push, or merge was performed on any branch; production extractor code was not modified; the p5 baseline extractor was built in a detached temporary worktree (`C:\Windows\Temp\opencode\ec5-p5-baseline` at `1cc2939`) used only for evidence.

## B. Independent source inspection

The review read the implementation before the handoff's test assertions and before relying on its conclusions.

- Production change is exactly one file: `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs` (+140/−10, commit `c7657f8`). Core changes: `Extract` seeds `isFileLevel: true`; `Walk` handles `lexical_declaration`/`variable_declaration` and threads `isFileLevel` through `Walk`/`WalkChildren`; `HandleExport` routes file-level variable declarations to `HandleVariableDeclaration(..., isExported: true)`; new `HandleVariableDeclaration` (lines 292–392) emits per-declarator `field` nodes with `AddDeclaration` receiving the declarator span and optional properties.
- Identity formula verified by executed test (probe 9): node ID `typescript:symbol:{hex}` and symbol ID `typescript:{hex}` with `hex = SHA256(UTF8("variable\n" + path + "\n" + name))` rendered **lowercase** hex, matching `Convert.ToHexStringLower` (EcmaScriptExtractionProfile.cs:1110). The handoff (§B) says “uppercase-hex”; that wording is wrong. Finding EC5-GLM-F1 (P3, documentation only).
- Guard analysis: `HandleVariableDeclaration` requires `isFileLevel && scope.Owner == ScopeOwner.File`. `isFileLevel` is `true` only for children of the root and for declarations of a root-level `export_statement`; every nested `WalkChildren` call resets it to `false`, so for-header, block, function, class, and namespace declarations cannot qualify even though `scope.Owner` remains `File` inside some wrappers. A top-level `declare const x;` is wrapped in `ambient_declaration`, whose children are walked without `isFileLevel`, so ambient variables are never emitted (probe 5).
- Per-declarator skips: non-`variable_declarator` children, `HasError` declarators, non-`identifier` names (destructuring and patterns), error/missing values, `=`-without-value, and `const` without value (probe 5 showed tree-sitter accepts `const x;` grammatically — the keyword guard, not parse error, is what excludes it).
- Duplicate same-file names: one shared node whose `modifiers` is the ordinal intersection of occurrences (lines 347–374); each physical declarator keeps its own `CodeGraphDeclaration` with exact written modifiers (lines 386). Probe 12 verified both directions, including `export var shared` + `var shared` → node `modifiers` exactly `"var"`, per-declaration `["export var", "var"]`.
- `AddDeclaration` gained an optional properties parameter; the declaration ID remains `sha256(symbolId\npath\nspan)` — offset-sensitive, so re-extraction after a move replaces the old declaration (verified in lifecycle tests and probe 14 for the stable case).
- Path input: `CodeGraphSource.Path` is normalized by `ContractValue.RelativePath` (backslash→slash, relative, no `..`, case preserved), and the session passes `source.Path` to the profile (`TreeSitterExtractionSession.cs:121`), so the identity path component is normalized independently of OS. Verified by probe 10 (`src\sub\ids.ts` ≡ `src/sub/ids.ts`).
- Both plugins fold the profile revision into their version strings (`.p6`): `TreeSitterTypeScriptPlugin.cs:23–27`, `TreeSitterJavaScriptPlugin.cs:20–24`.

Contract assumptions relied on: `CodeNodeKinds.Field` is an existing public node kind; `declares`/`contains` are file-anchored; `invokes-name` never implies resolution; `calls`/`references` remain not produced.

## C. Adversarial counterexamples

18 review-only probes were executed in the review worktree
(`tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc5GlmQ1VariableProbes.cs`, marked REVIEW-ONLY;
all 18 pass; `dotnet test --filter FullyQualifiedName~ReviewEc5GlmQ1`).

| # | Probe (invariant challenged) | Result |
| --- | --- | --- |
| P1 | Local/block/for/if/while/method variables never emit file-level fields; unrelated top-level facts in the same file survive | Held |
| P2 | Destructuring (object, nested, rest, defaults, array holes) emits nothing; their initializer invocations remain file-attributed with exact spans | Held |
| P3 | `export default defineConfig(...)` and anonymous `export default function () {}` fabricate no variable | Held |
| P4 (C5) | Function-local shadow of a file-level name emits only the file-level field with its own span | Held |
| P5 | `var x;`/`let y;` emitted; `declare const` (ambient) never emitted; `const x;` (grammatically clean, semantically invalid) excluded by the keyword guard | Held |
| P6 | Namespace members (`namespace N { export const x }`) are not file variables | Held |
| P7 | Malformed `const broken = ;` does not suppress sibling `const healthy = 1;` or the following class | Held |
| P8 | Unicode names (`const π`) emitted; >256-char identifier skipped (bounded text) | Held |
| P9 (C-formula) | Node/symbol IDs equal the documented lowercase SHA-256 formula exactly | Held |
| P10 (C1) | Backslash and slash path spellings collapse to one identity (normalization) | Held |
| P11 (C2/B) | File variable, instance field, and static field with the same written name keep three distinct identities; containment edges separate file vs class ownership | Held |
| P12 (C3) | `var item = 1; var item = 2;` → one node, two physical declarations, honest modifier intersection; export/non-export mix verified | Held |
| P13 (C4) | `const first = 1, second = 2;` → distinct identities and exact spans (7 and 18) | Held |
| P14 (C6) | Identities (node, declaration, edge IDs) stable under unrelated edits | Held |
| P15 (D) | Only directly written `export` recorded; `export { local }`, re-export, `export { direct as alias }` fabricate nothing; type annotation `: Missing` creates no node or edge | Held |
| P16 (D) | `export const a = 1, b = 2;` → each declarator emitted exactly once (no export-wrapper duplication) | Held |
| P17 (E) | Initializer invocations remain lexical (file for plain values, callable for arrow-valued variables) with exact spans; chained `factory().configure()` outer call excluded per documented unnameable-target coverage (pre-existing, unchanged by EC-5 — see §E) | Held |
| P18 (C) | LF vs CRLF source content produces byte-identical node, declaration, and edge identities and spans | Held |

Six initial probe failures occurred; all six were probe assertion defects (reviewer arithmetic or
wrong assumptions), not implementation defects. Each was corrected against the documented
contract; the implementation behaved correctly in every inspected case. Probe-only defects are
not production defects. No production code was changed to make any probe pass.

## D. Variable and export correctness (Review A / D)

**Supported subset verified by executed extraction:** top-level `const`/`let`/`var` (TypeScript and JavaScript, `.ts/.mts/.cts/.tsx/.js/.mjs`), direct `export` of each, TypeScript annotations without type relationships, multiple declarators per statement, call initializers, function/arrow-valued variables (dual projection retained), existing `.mjs` pipeline behavior. EC-5 adds no extension support, matching the handoff.

**Negative cases verified (no invented variables):** function/block/for/if/while/method locals, namespace members, ambient `declare`, object/array/nested/rest/default destructuring, anonymous default exports, `export { x }` lists, re-exports, malformed declarators, overlong identifiers. Negative cases do not suppress unrelated valid facts in the same file (P1, P7).

**Export metadata (semantic honesty):** `modifiers` records only the written keyword plus `export` for directly written `export const|let|var`. Probes confirm: `export` never implies a successful external binding (no export edge exists anywhere in the vocabulary emitted); re-exports create only the pre-existing import-target node; no new resolved references, calls, or types appear (all edges are `Structural`; `calls`/`references` absent in every probe and in the website publication). Values/initializers are not claimed as evaluated constants. The export-wrapper path was checked for accidental double-walking (`HandleExport` returns after one pass) — probe 16 and the website publication show exactly one declaration per declarator.

## E. Identity and query contracts (Review B / C)

**C1 same name, different files** — distinct identities (path component); website `base` returns exactly two candidates (javascript + typescript) with distinct IDs; query ambiguity is preserved, not merged.
**C2 variable vs class field** — distinct discriminators (`variable` vs owner-derived `field\n{classNodeId}\n{ownership}`); probe 11 and the website neighborhood check (`StatusDefinition` contains only its own members; no class contains a file variable).
**C3 duplicate declarations** — one symbol, two physical declarations, per-occurrence metadata, node modifiers = intersection. Honest representation; identity policy documented in `docs/ecmascript-symbol-identity.md` (§EC-5). Not a defect under the documented symbol model.
**C4 multiple declarators** — separate identities and exact spans (probe 13; website `const first = 1, second = 2` analogues verified per-file in declaration listings).
**C5 nested declarations** — only the file-level variable is emitted (probe 4; P1).
**C6 rename/delete** — implemented lifecycle tests (passing) verify rename produces a new identity and removes the old declaration, sibling removal works, file deletion clears all declarations; export add/remove updates metadata while preserving identity.
**Additional scrutiny** — path normalization (P10), case-preserving ordinal handling (identity hashes the exact name; no case folding anywhere in the pipeline), Unicode (P8), identifier bounds (P8), duplicates (P12), module-vs-script (module only in the grammar's sense; `namespace` members excluded, P6), malformed syntax (P7, error/missing nodes skipped), unrelated-edit stability (P14), source line endings (P18). Cross-platform determinism is **not claimed**: all executed evidence is Windows x64; the path normalization logic is platform-independent by construction but only Windows execution was observed.

**`field` node-kind reuse (Review B):** extraction-level and query-level checks found no material consumer-visible failure. Concretely: (1) a file-level variable and a class field never share an identity; (2) `contains` edges distinguish file ownership from class ownership; (3) member discovery (class/interface neighborhood) returns only class/interface members — the website neighborhood of `StatusDefinition` exposes no file variable; (4) generic declaration queries (`GetDeclarationsInFileAsync`) return both when both exist in a file; (5) filtering by node kind `field` returns exactly the file-level variables in the qualified set (class fields are node kind `property` with member-kind discriminator `field`), so today's `field` kind is effectively "module-level variable" for the ECMAScript providers. The vocabulary is mildly misleading by name but is internally consistent, documented (handoff §B, identity doc §EC-5), and consistent with the generic graph contract. **Disposition: DOCUMENTED LIMITATION (P3)** — a future differentiation (e.g., a dedicated kind) would be a public-contract change and is not justified by any observed failure.

## F. Publication verification (Review F)

Executed on a real durable store, not just batch extraction. Evidence: `docs/evidence/review-ec5-glm-q1/p5-p6-website-migration.md` (verbatim harness output).

1. **Both plugins advertise p6** — verified in source and in emitted edge evidence (`ProviderVersion = 0.2.0-preview.6+v0.23.2.abi14.p6`).
2. **Authentic p5 → p6 invalidation** — the genuine p5 extractor (built from baseline commit `1cc2939`) published 9 units to a fresh `LatticeCodeGraphStore`; the p6 build re-planned all 9 units as `changed` (new=0, changed=9) and re-extracted despite identical content hashes. No fake plugin proxy was needed.
3. **Re-extraction publishes variables** — p5 publication had `FIELDS 0`; p6 publication has `FIELDS 21`.
4. **No duplication** — `DUPLICATE-SYMBOL-NODES 0`, `DECLARATION-COLLISIONS False`; per-unit raw counts 130 collapse to 128 unique exactly as in p5 (109→107), because name-based import/invocation placeholders legitimately share identities across files (documented, pre-existing).
5. **Old facts not duplicated** — contains/declares went 62→83 (+21 each); imports/inherits/invokes-name unchanged (9/1/101).
6. **Deletion/rename** — implementation lifecycle tests (passing in the reviewed suite) cover rename, removal, sibling change, export add/remove, and file deletion against `CodeIndexingService` + public queries.
7. **Unchanged reindexing** — third p6 run: `new=0 changed=0 unchanged=9 nodes=0 declarations=0 edges=0`, prior publication retained.
8. **Failed indexing** — the existing core regression `IndexAsync_FailedRunRetainsLastSuccessfulSourceState` passes in the suite; Lattice store conformance (including publication round-trips and superseded-run conflicts) passes in the LatticeDb project (26 tests).
9. **Durable store dispose/reopen** — every `verify` step ran in a separate process against the same database file, so publication state, all 21 variables, declarations, and edges survived close/reopen twice.

Historical migration note: an authentic p5 implementation was available (baseline commit), so the real p5→p6 migration was used as instructed. No historical evidence was fabricated.

## G. Website ground truth (Review G)

Target: `jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`, git-archive snapshot, 78 files: 8 `.ts` + 1 `.mjs` supported, 22 `.astro` + others unsupported.

**G1. Independent ground truth** — assembled by reading the sources before running any extractor (not derived from emitted nodes):

| File | Eligible variables (written keyword, line:col) | Directly exported |
| --- | --- | --- |
| `astro.config.mjs` | `site` (const, 9:7), `base` (const, 10:7) | — |
| `src/content.config.ts` | `evidenceLink` (4:7), `projects` (9:7), `journal` (42:7), `milestones` (56:7), `philosophy` (69:7), `evidence` (79:7), `collections` (93:14) | `collections` |
| `src/lib/ecosystem.ts` | `STATUSES` (19:14), `STATUS_BY_KEY` (50:14), `CATEGORIES` (62:14), `CATEGORY_BY_KEY` (107:14), `RUNTIME_LAYERS` (130:14), `SUPPORTING_PRIMITIVES` (208:14) | all six |
| `src/lib/paths.ts` | `rawBase` (1:7), `base` (2:7) | — |
| `src/lib/site.ts` | `SITE` (1:14), `NAV` (17:14), `FOUNDER` (38:14) | all three |
| `src/pages/robots.txt.ts` | `GET` (4:14) | `GET` |
| `src/lib/format.ts`, `src/lib/schema.ts`, `src/env.d.ts` | none (functions/interfaces/reference only) | — |

**Total: 21 eligible declarations, 11 directly exported.**

**G2. Publication comparison** — indexing the snapshot through `CodeIndexingService` into `LatticeCodeGraphStore` and reading back via public queries produced exactly these 21 `field` symbols, each with exactly one physical declaration whose span matches the source (spot-verified against the read files, including `SITE` 1:14–10:11 ending before `as const`, `STATUS_BY_KEY` 50:14–52:38 ending before the `as` cast, `GET` 4:14–19:2). All 11 exported declarations carry `const export`; the other 10 carry `const`. No unsupported variable appears; no identity collisions. Aggregate published state:

| Metric | p5 (= EC-4, re-measured) | p6 | Δ |
| --- | ---: | ---: | ---: |
| Unique nodes | 107 | 128 | +21 |
| Declarations | 62 | 83 | +21 |
| Edges | 235 | 277 | +42 |
| contains / declares | 62 / 62 | 83 / 83 | +21 / +21 |
| imports / inherits / invokes-name | 9 / 1 / 101 | 9 / 1 / 101 | 0 |
| `field` nodes | 0 | 21 | +21 |

The implementation's reported 21/11 and 128/83/277 are independently reproduced.

**G3. Developer queries** — exported data constant: `FindSymbolsByQualifiedNameAsync("STATUSES")` → single `field` with `const export`. Variables per file: `GetDeclarationsInFileAsync` returns exact per-file declaration lists (2, 7, 28, 3, 14, 2, 2, 25, 0). Direct-export status read from `modifiers`. Variable vs interface member: `StatusDefinition` neighborhood contains only the interface and its three members. Exact-name `base` → two honest candidates; `GET` → `field` + documented `callable` projection. Enclosing-symbol at `src/lib/site.ts` 17:14 → `field:NAV`. No file-level variable appears as a member of an unrelated class/interface. No misleading results observed.

**G4. Metrics over the explicitly defined eligible subset** — true positives 21, false positives 0, false negatives 0; precision 21/21 (100%), recall 21/21 (100%); directly exported 11/11. These metrics apply only to this snapshot's 9 supported files and must not be extrapolated to `.astro` files, other extensions, or general language coverage.

## H. Findings

| ID | Disposition / severity | Description | Evidence |
| --- | --- | --- | --- |
| EC5-GLM-F1 | CONFIRMED DEFECT (documentation only) — **P3** | Handoff §B states the identity hash is “uppercase-hex”; the implementation and all published IDs use lowercase hex (`Convert.ToHexStringLower`, EcmaScriptExtractionProfile.cs:1110–1111). The handoff's own published-node tables show lowercase IDs. Fix the sentence. | Probe P9; source inspection |
| EC5-GLM-F2 | CONFIRMED DEFECT (test robustness, qualification reproducibility) — **P3** | `TreeSitterEcmaVariableTests.ExtractAsync_ModelsTypeScriptFileVariablesWithWrittenModifiersAndLocations` line 52 asserts `EndColumn == source.Split('\n')[3].Length`, which fails on any checkout where `core.autocrlf=true` converts the LF-committed test file to CRLF (expected 39 vs actual 38). The committed blob is pure LF; the suite passes 322/322 on an LF checkout (verified in a separate `-c core.autocrlf=false` worktree). A fresh clone on a default Windows git config cannot reproduce the reported green suite. Suggest deriving the expectation from the declarator text or normalizing line endings in the fixture. | Executed test runs; blob `a0975fd` byte check |
| EC5-GLM-F3 | DOCUMENTED LIMITATION — **P3** | Node kind `field` now means “file-level variable” for the ECMAScript providers, while class fields are node kind `property` (member-kind discriminator `field`). The name suggests the opposite. No consumer-visible failure was found (identity, ownership, member discovery, and exact-name queries all disambiguate correctly); differentiation would be a public-contract redesign not justified by evidence. | Probe P11; website neighborhood check |
| EC5-GLM-F4 | DOCUMENTED LIMITATION — **P3** | Ambient declarations (`declare const x: number;`) are not emitted. The handoff claims “top-level `const`” without carving out ambient declarations; the behavior is deliberate (ambient wrapper is not walked at file level) and consistent with “no ambient/global-scope modeling”, but the boundary is only implicit in the docs. | Probe P5; source inspection |
| EC5-GLM-F5 | NO ISSUE FOUND | Initializer invocation attribution, callable projection, chained-call exclusion, import/invocation placeholder behavior: p5→p6 extraction diff is byte-identical for every non-variable fact across 5 mixed fixtures; no invocation lost or reattributed. | p5/p6 dump diff (evidence files) |

No P0/P1/P2 findings. No confirmed defect affects identity, ownership, extraction correctness, or publication integrity.

## I. Final verdict

**VERIFIED WITH QUALIFICATIONS.**

Within the examined subset (named, simple file-level `const`/`let`/`var` declarations in the pinned tree-sitter TypeScript/JavaScript grammars, on Windows x64, against the specified website revision and the current generic graph contracts), EC-5 adds correct, discoverable file-level variable declarations without corrupting existing symbol identities, member queries, evidence semantics, or published graph behavior. All headline claims (21/21 website variables, 11 exports, profile p5→p6 invalidation, unchanged pre-existing facts, stable identities) were independently reproduced with executed evidence. The qualifications are the P3 items above plus these boundaries: ambient `declare` variables are not modeled; `.astro` files and extensions outside the current set are not qualified; export lists, default exports, re-export binding, and module resolution remain unmodeled by design; cross-platform determinism was not executed on non-Windows hosts; and the p5→p6 migration evidence covers the shared-profile change only (no simultaneous grammar change).

## J. Next actions

1. (EC-5 closure, optional, non-blocking) Correct the “uppercase-hex” sentence in `docs/evidence/ec5-variable-declarations-handoff.md` (F1) and make the EndColumn assertion line-ending-agnostic (F2). Neither affects published graph facts; a profile revision bump is **not** required for either.
2. Record the ambient-`declare` boundary explicitly in the identity doc or feature matrix (F4).
3. EC-6 design may begin now. Suggested qualification boundaries for EC-6: keep the variable identity formula stable (any change is another p-revision and re-extraction); if export lists or default-export variables are modeled, define their identity relationship to the direct-export subset before implementation; if `.astro` support lands, qualify it as a separate milestone with its own snapshot; preserve the invariant that class/interface member discovery never returns file-level variables.

## K. Review preservation

- Review branch: `review/ec5-glm-q1` from `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2`.
- Review-only additions committed on the branch: this report; `docs/evidence/review-ec5-glm-q1/p5-extraction-fixture-dump.txt`; `docs/evidence/review-ec5-glm-q1/p6-extraction-fixture-dump.txt`; `docs/evidence/review-ec5-glm-q1/p5-p6-website-migration.md`; `tests/Penghou.Hetu.TreeSitter.Tests/ReviewEc5GlmQ1VariableProbes.cs` (marked REVIEW-ONLY).
- Not committed: website snapshot, temporary Lattice databases, p5 baseline worktree, harness projects (all under `C:\Windows\Temp\opencode`), build outputs.
- Canonical `main` and the implementation branch were left unchanged; nothing was pushed or merged.

### Test commands and results (this environment)

| Command | Result |
| --- | --- |
| `dotnet test Penghou.Hetu.slnx` (review worktree, CRLF checkout, with probes) | 338 passed / 339 (1 failure = F2, CRLF artifact; probes 18/18 pass) |
| `dotnet test Penghou.Hetu.slnx` (LF checkout of `5a7c59c4`) | 322 passed / 322, 0 failed |
| `dotnet test ... --filter FullyQualifiedName~TreeSitterEcmaVariable` (CRLF) | 7/8 (only F2 fails) |
| `dotnet test ... --filter FullyQualifiedName~ReviewEc5GlmQ1` | 18/18 pass |
| p5 baseline extractor build (`1cc2939` temp worktree) | built clean; used only for the migration and extraction-diff harnesses (its test suite was not run in this review) |
| `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore` | passed, no changes |
| Authentic p5→p6 migration + verification harness | see `docs/evidence/review-ec5-glm-q1/p5-p6-website-migration.md` |
