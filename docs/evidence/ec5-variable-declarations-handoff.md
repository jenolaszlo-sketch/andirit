# EC-5 Variable Declaration Handoff

## A. Git binding

- Repository: `jenolaszlo-sketch/penghou-hetu`.
- Canonical checkout: `C:\Users\Laszlos\source\repos\Penghou.Hetu`.
- Required starting commit: `1cc2939ea615d627cde616d06e739867be4e56e2`; verified before implementation. The canonical checkout stayed on `main` and was not modified.
- Implementation worktree: `C:\Users\Laszlos\source\repos\Penghou.Hetu.ec5-variable-declarations`.
- Branch: `implementation/ec5-variable-declarations`.
- Implementation commit: `c7657f8` — `feat: extract ECMAScript file-level variables`.
- Test commit: `e77deea` — `test: qualify EC-5 variable declarations`.
- Documentation commit: this handoff report and the documentation updates are committed together after the two commits above. The final checkout was checked at that commit; its working tree is clean. The exact final commit ID is also recorded in the task's final report because a commit cannot contain its own hash.
- No pull, merge, rebase, push, or canonical-main modification was performed.

## B. Contract and identity decisions

The existing `field` node kind represents named file-level variables. No public graph contract changed.

For language key `language`, source path `path`, and written binding name `name`, the symbol and node IDs are derived as follows:

```text
hash = lowercase-hex(SHA256(UTF8("variable\n" + path + "\n" + name)))
symbol ID = language + ":" + hash
node ID   = language + ":symbol:" + hash
```

Hash output is lowercase hexadecimal; the implementation renders it with
`Convert.ToHexStringLower` (`EcmaScriptExtractionProfile.Hash`), and every
published identity in this report is lowercase. An earlier revision of this
sentence said "uppercase-hex"; that wording was wrong (EC5-GLM-F1) and was
corrected during EC-5 closure without changing identity formatting.

The `variable` discriminator prevents collision with callable, type, interface, class-member, and heritage identities. The source path separates same-named variables in different files. The identity excludes source offsets, so unrelated edits preserve it. Rename or file move changes it.

Repeated same-file declarations of the same name converge on one candidate node, while every physical `variable_declarator` retains its own declaration span and per-occurrence metadata. This is an identity policy and does not claim language-level declaration merging. On conflicting occurrences, node-level `modifiers` contains only modifiers common to all occurrences; each physical declaration retains the written metadata.

The existing `modifiers` property records the written keyword (`const`, `let`, or `var`) and direct written `export`. No export-binding edge is introduced. `const` records syntax only; it does not mean that the referenced value is immutable.

## C. Implementation summary

The shared TypeScript/JavaScript profile handles file-level `lexical_declaration` and `variable_declaration` syntax. It emits each simple identifier declarator as a `field`, with a physical declaration at the declarator span and file-level Structural `declares` and `contains` evidence at the identifier span. Language and written modifier metadata are retained. Profile revision is 6.

Supported and exercised forms:

- Top-level `const`, `let`, and `var` in TypeScript and JavaScript.
- Direct `export const/let/var`.
- TypeScript annotations, without a type relationship.
- Multiple simple declarators, each independently discoverable.
- Call initializers, preserving the pre-existing structural invocation attribution.
- Existing supported TypeScript extensions `.ts`, `.mts`, `.cts`, and `.tsx`; JavaScript `.js` and `.mjs` pipeline behavior. EC-5 adds no extension support.
- A function/arrow-valued variable remains a variable field and also keeps the pre-existing callable projection. It is not reclassified as a function declaration.

The implementation skips function-local and block-local bindings, object/array destructuring, ambient `declare` declarations, malformed/incomplete declarators, anonymous default-export expressions, and unsupported binding forms. It does not resolve export lists, re-exports, imports to exports, variable references, types, constant values, or runtime object properties. It does not parse `.astro` files.

Relevant production changes are in `src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs`. Focused fixtures are in `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaVariableTests.cs` and `TreeSitterEcmaVariableLifecycleTests.cs`; existing TypeScript/JavaScript plugin tests assert the p6 versions.

## D. Test and verification results

Commands and results:

- `dotnet test tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj --filter FullyQualifiedName~TreeSitterEcmaVariable --verbosity minimal` — 8 passed, 0 failed.
- `dotnet test Penghou.Hetu.slnx --verbosity minimal` — all 4 projects passed, 322 total tests: core 160, Tree-sitter 90, C# 46, LatticeDb 26.
- `dotnet format Penghou.Hetu.slnx --verify-no-changes --no-restore --verbosity minimal` — passed with no formatting changes required.
- `git diff --check` — clean before documentation commit; final commit is clean.

The focused tests cover TypeScript and JavaScript extraction, source locations, direct export metadata, typed and multi-declarator syntax, call initializers, same-name separation across files and node kinds, deterministic extraction, duplicate occurrences, local/nested/destructured/malformed boundaries, TSX grammar, and `.mjs`. Published-graph tests cover initial and unchanged indexing, addition, rename, removal, one declarator changing while its sibling remains, export add/remove, file deletion, exact-name lookup, declarations, enclosing-symbol queries, and `.mjs` indexing. Existing profile-plan tests verify p5 invalidation. The existing core test `CodeIndexingServiceTests.IndexAsync_FailedRunRetainsLastSuccessfulSourceState` passed in the full suite.

## E. EC-4 website comparison

The target was the exact EC-4 website commit `f9cca1773a32bb517e624cc380e1f6d14e0cc317`. It was read from a `git archive` snapshot in a temporary directory. A temporary copy of the preserved EC-4 harness was pointed at the EC-5 worktree and extended with exact-name queries; neither the target checkout nor the EC-4 preservation branch was changed, and no harness code was added to production.

The archived target contained 78 tracked files. Hetu selected 9 supported source files (8 TypeScript and `astro.config.mjs`), totaling 35,810 source bytes; the remaining 69 files were unsupported by the current indexing pipeline. The 22 `.astro` files were excluded. No indexing warnings occurred.

| Published graph measure | EC-4 baseline | EC-5 | Change |
| --- | ---: | ---: | ---: |
| Unique published graph nodes | 107 | 128 | +21 |
| Physical declarations | 62 | 83 | +21 |
| Edges | 235 | 277 | +42 |

The entire change is accounted for by 21 new `field` nodes, 21 physical variable declarations, and 42 file ownership edges (one `contains` and one `declares` per variable). Existing declaration counts and edge counts were unchanged: `imports` 9, `inherits` 1, and `invokes-name` 101 before and after. The corresponding EC-5 counts are `contains` 83 and `declares` 83, up from 62 each. All emitted edges are Structural; no `calls` or `references` appeared. No false-positive variable was observed in the evaluated subset.

The independent source inventory found these 21 eligible top-level simple bindings, all present exactly once in the published variable results, with no missing or unexpected names:

- `astro.config.mjs`: `site`, `base`.
- `src/content.config.ts`: `evidenceLink`, `projects`, `journal`, `milestones`, `philosophy`, `evidence`, `collections`.
- `src/lib/ecosystem.ts`: `STATUSES`, `STATUS_BY_KEY`, `CATEGORIES`, `CATEGORY_BY_KEY`, `RUNTIME_LAYERS`, `SUPPORTING_PRIMITIVES`.
- `src/lib/paths.ts`: `rawBase`, `base`.
- `src/lib/site.ts`: `SITE`, `NAV`, `FOUNDER`.
- `src/pages/robots.txt.ts`: `GET`.

Thus precision and recall were both 21/21 (100%) for this recorded eligible set and supported-file subset. Eleven are directly written exports: `collections`; the six `ecosystem.ts` bindings; `SITE`, `NAV`, and `FOUNDER`; and `GET`. All 21 physical declaration locations were checked against source; all 42 ownership edges were present. Representative published exact-name queries returned one field each for `STATUSES`, `CATEGORIES`, `RUNTIME_LAYERS`, `SUPPORTING_PRIMITIVES`, `SITE`, `NAV`, `FOUNDER`, and `collections`. `GET` returns both the new field and the pre-existing callable projection, consistent with the established callable-valued variable behavior.

This comparison establishes the EC-5 subset on the specified snapshot. It does not establish coverage for unsupported extensions, `.astro` components, export lists, or semantic binding.

## F. Publication lifecycle and compatibility

The shared profile revision moved from 5 to 6. The existing TypeScript and JavaScript plugin version strings therefore end in `.p6` (for the pinned grammars, TypeScript `v0.23.2.abi14.p6`, JavaScript `v0.25.0.abi15.p6`). The plan test verifies a prior profile version is stale and schedules unchanged content for extraction.

Incremental published-graph tests confirm stable IDs on unchanged indexing and export-status changes, new identities after rename, removal of the old declaration and obsolete sibling facts after source replacement, removal of all file declarations after file deletion, and correct exact-name/enclosing-symbol queries. The existing indexing transaction regression confirms a failed run retains the last successful source state. No persistent graph migration is required.

## G. Known limitations

- Only simple identifier bindings at file level are emitted. Local scopes, destructuring, and ambient `declare` declarations remain out of scope. `declare const externalValue: string;` is not part of the supported subset (accepted limitation EC5-GLM-F4); ambient/global-scope modeling is not implemented anywhere in the profile.
- File-level variables reuse the existing generic `field` node kind (accepted limitation EC5-GLM-F3). A class field is a `property` node whose member-kind discriminator is `field`, and its identity never converges with a file-level variable. Consumers that need to distinguish a module variable from a class member must use ownership (`contains` from the file node versus the class node) and the `variable` symbol identity discriminator rather than the node kind alone. The underlying representation is unchanged.
- Same-file duplicate names share one candidate node. Each occurrence is retained physically, but consumers must not infer semantic declaration merging; conflicting modifiers are only exact at the per-declaration property level.
- Written direct `export` is observable, but export specifiers, default exports, re-exports, and module binding resolution are not modeled.
- No variable-reference, type, constant-value, or dataflow semantics are inferred.
- A function/arrow-valued variable has the pre-existing callable projection alongside its field node. Name-only lookups can therefore return both kinds, as demonstrated by `GET`.
- `.astro` parsing and files outside the current extension set were not qualified.

## H. Independent-review readiness

EC-5 is ready for independent review, but it has not been independently qualified. Reviewers should challenge these specific invariants and counterexamples:

1. File-level context survives an export wrapper but is cleared under blocks, functions, classes, interfaces, and other nested owners.
2. Only simple identifier children of `variable_declarator` produce field identities; malformed or destructured bindings must not produce partial symbols.
3. Identity uses language, the `variable` discriminator, normalized source path, and name, but no physical offset; test cross-file duplicates, type collisions, rename, and repeated same-file occurrences.
4. Each declarator retains its own physical span and declaration metadata, including conflicting repeated declarations; node-level modifiers must not claim a modifier that is not common.
5. Direct written `export` is metadata only. No export-binding, reference, resolved call, or type edge is fabricated.
6. Initializer invocation attribution and the existing callable projection remain unchanged.
7. p5 units are invalidated; source replacement removes stale declarations and edges; a failed indexing run preserves the prior publication.
8. On the exact website snapshot, the independently enumerated 21-name set and exact locations match the published query results, while all pre-existing counts remain stable.

The EC-4 baseline evidence and consumer lookup guidance remain in their preserved review history; this report records the EC-5 comparison without claiming an independent review.
