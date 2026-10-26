# EC-6C.2 Independent Qualification Matrix — GLM-Q1

**Project:** Penghou.Hetu (future Andirit)
**Qualification branch:** `qualification/ec6c-2-glm-q1`
**Qualification HEAD under review:** `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` (tree `d8e763a69dadd957df32c872733be5015593c7d6`)
**Date:** 2026-10-26
**Status:** Matrix committed before test execution; `Result` column is updated only by recorded evidence artifacts.

This matrix binds each contract requirement to its source specification, the
expected observable behavior, the existing implementation-side test coverage
(claims, not proof), the independent test or inspection performed by this
qualification, and the evidence artifact where the result is recorded.
`PENDING` entries are replaced only when the referenced artifact records a
result; unverified claims remain `PENDING`.

## 0. Binding review inputs

| Input | Binding |
| --- | --- |
| EC6C-A2 architecture (normative) | `docs/design/ec6c-export-representation-architecture.md` §1–§12 |
| A29 closure (name rule) | `docs/decisions/ec6c-a29-closure.md` |
| EC-6C.1 owner acceptance | `docs/decisions/ec6c1-owner-acceptance.md` at `d81108dd51d61fe79767e15c4adb4c10659836de` |
| EC-6C.1 evidence (claims) | `docs/evidence/ec6c-1-export-shape-inventory-handoff.md` at `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` |
| EC-6C.2 evidence (claims) | `docs/evidence/ec6c-2-structural-export-publication-handoff.md` at `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` |
| Production changes | `3bf418d` (publisher + profile p9), `b54eb3b` (export tests), `b61b45e` (compatibility tests), `160c9cf` + `3ab1f14` (docs) |

## A. Export entry identity

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A1 | One entry per distinct (module, exportedName) | A2 arch §3 | Repeated occurrences share one node | `PublishesModuleScopedSlotsAndIndependentOccurrenceEdges` (3 entries / 4 edges) | Independent fixture: duplicate occurrences of one name in one module and one file; count nodes and IDs | `TreeSitterEcmaExportQualificationQ1Tests`, evidence log §A | VERIFIED |
| A2 | Entry ID formula `{language}:export:{lowercase SHA-256(UTF8(modulePath + LF + exportedName))}` | A2 arch §3; handoff §C | Recomputed ID equals published ID | Formula asserted in publication tests | Recompute independently in tests (separate implementation of the hash from the spec text) | evidence log §A | VERIFIED |
| A3 | Exact public-name preservation; ordinal, case-sensitive | A2 arch §3 | Name and QualifiedName equal written spelling; case-distinct names distinct | `EntryIdentityIsModuleAndNameScopedWithoutUnicodeNormalization` | Case-distinct fixture (`currentProject` vs `currentproject`); canonically equivalent but distinct Unicode spellings | evidence log §A | VERIFIED |
| A4 | Stable, deterministic identity across extractions and stores | A2 arch §3 | Same input → same ID; entry ID unchanged by occurrence movement | Lifecycle tests re-query IDs | Extract twice; compare IDs; move statement in lifecycle fixture and verify entry ID invariance | evidence log §A, §F | VERIFIED |
| A5 | Module scoping: same name in two modules → distinct identities | A2 arch §3 | IDs differ; both retrievable | `EntryIdentityIsModuleAndNameScopedWithoutUnicodeNormalization` | Independent two-module fixture with identical name | evidence log §A | VERIFIED |
| A6 | Export entries have no `SymbolId` | A2 arch §2 | `SymbolId == null` on all export nodes | Publication tests assert null | Inspect every export node in all independent fixtures | evidence log §A | VERIFIED |
| A7 | Export entries carry only the permitted `language` property | A2 arch §2 | Properties == { language } | Publication tests assert exact dictionary | Inspect exact property sets in independent fixtures (both languages) | evidence log §A | VERIFIED |
| A8 | No normalization or rewriting of names | A2 arch §3 (A29 §6) | Precomposed vs decomposed names remain distinct; spelling preserved ordinally | Unicode publication test (é variants) | Independent NFC/NFD fixture asserting distinct IDs and preserved spelling | evidence log §A, §E | VERIFIED |

## B. Structural export edges

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| B1 | Every supported written occurrence produces one `exports` edge | A2 arch §1, §5 | Edge count equals occurrence count; each has distinct identity | Publication tests (4 edges / 3 entries) | Independent fixtures: aliases, defaults, repeated occurrences, multiline | evidence log §B | VERIFIED |
| B2 | `export-kind` ∈ {named, default} correct per syntax | A2 arch §2 | named for all non-default forms; default for default forms | Publication tests cover direct/local/default | Independent fixture spanning all accepted forms | evidence log §B | VERIFIED |
| B3 | `export-via` ∈ {direct, local, expression} correct per syntax | A2 arch §2; handoff §E | direct declarations→direct; local lists→local; default expressions→expression | Publication tests assert via values | Independent fixture spanning all six observation forms | evidence log §B | VERIFIED |
| B4 | `local-name` present only when written syntax supplies it | A2 arch §2 | Absent property vs value; never empty for present | Publication tests (absent for default expression) | Independent fixtures with absent local, plain local, alias | evidence log §B | VERIFIED |
| B5 | `type-only` present with `true` only for explicit type-only forms | A2 arch §2 | Absent otherwise; true exactly for grammar-grounded explicit forms | Publication tests (statement-level + inline) | Independent type-only/non-type-only fixtures | evidence log §B, §D | VERIFIED |
| B6 | `namespace-projection` present with one of {type, value, both, unknown} | A2 arch §2, §4 | Exactly one value per edge; per occurrence, not per entry | Publication tests assert projections | Independent fixture across all projection classes | evidence log §B, §D | VERIFIED |
| B7 | Edge endpoints: file node → export entry, kind `exports` | A2 arch §1 | Source = file node ID; target = entry ID; Kind = exports | Publication tests assert source and target | Inspect endpoints in independent fixtures | evidence log §B | VERIFIED |
| B8 | Evidence: Structural / `penghou.hetu.tree-sitter.ecmascript-export-surface` / `{packageVersion}+exports1.local-v1` | A2 arch §11 | Exact provider and version on every export edge | Publication tests assert provider/version | Recompute version prefix from plugin version; inspect all edges in fixtures | evidence log §B | VERIFIED |
| B9 | `Evidence.Location` is the exact public-name token span; one-based, end-exclusive UTF-16 | A2 arch §5 | Location matches the written public token, not the construct | Publication tests compute columns from source lines | Independent fixtures: direct name token, alias token, `default` keyword, multiline, supplementary scalars before name | evidence log §B | VERIFIED |
| B10 | Construct span not published as a graph property | A2 arch §C (handoff) | No construct-span property or equivalent | Publication test asserts absence of `construct-span` | Inspect full property sets of all edges in fixtures | evidence log §B | VERIFIED |
| B11 | Export edges absent from `RelationshipCoverage` and `UnresolvedRelationships` | A2 arch §7, §11 | Coverage list unchanged vs p8 | Shape fingerprint test asserts absence | Inspect coverage lists in independent fixtures | evidence log §B | VERIFIED |

## C. Edge identity and conflicts

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| C1 | Occurrence ID = `{language}:{SHA-256(UTF8(newline tuple))}` with all prescribed fields in order | A2 arch §5 | Recomputed ID equals published edge ID for every occurrence | `ExpectedOccurrenceId` in publication tests | Independent recomputation (from spec text, not from implementation code) across fixtures | evidence log §C | VERIFIED |
| C2 | Local-name presence bit distinguishes absent from empty | A2 arch §5 | Absent → `0` + empty; written → `1` + text | Presence bit asserted via expected IDs | Independent fixture with absent vs written local names; verify distinct IDs | evidence log §C | VERIFIED |
| C3 | Type-only state, projection, and source span contribute to identity | A2 arch §5 | Changing any changes the ID | Movement/rename lifecycle assertions | Independent fixture: same fact with different spans → distinct IDs | evidence log §C | VERIFIED |
| C4 | Exact duplicate facts are idempotent | A2 arch §6 (3) | Repeated publish leaves dictionaries unchanged | `PublisherIsIdempotentAndRejectsSameIdDifferentOccurrenceFacts` | Independent idempotence exercise through full extraction and second publish | evidence log §C | VERIFIED |
| C5 | Same-ID different fact fails closed before insertion | A2 arch §6 (4) | InvalidOperationException; dictionaries unchanged | Publication collision tests (edge + node) | Independent collision construction on both node and edge with byte-level dictionary comparison before/after | evidence log §C | VERIFIED |
| C6 | Conflict diagnostics preserve both complete facts | A2 arch §6 (4); handoff §D | Message contains ID, endpoints, kind, evidence, properties of existing and candidate | Publication tests assert fragments | Independent inspection of full diagnostic for both node and edge collisions | evidence log §C | VERIFIED |
| C7 | Conflicting publication leaves no partially mutated unit | A2 arch §6 (5) | Prior publication authoritative; run failed | Failure/cancellation lifecycle tests | Independent lifecycle test: collision during reindex → prior facts intact in both stores | evidence log §C, §F | VERIFIED |
| C8 | Tuple serialization unambiguous for all permitted values | A2 arch §5 | No two distinct permitted facts share a material | None found | Analytical review of value domains + fixture: local name containing LF/quote/control characters; ambiguity analysis recorded | evidence log §C | VERIFIED (publisher-boundary test; grammar-produced local names cannot contain LF, and tail-anchored fields prevent aliasing) |

## D. Namespace projection

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| D1 | Direct variable / function → `value` | A2 arch §4 | Projection value on the edge | Shape + publication tests | Independent direct-declaration fixture | evidence log §D | VERIFIED |
| D2 | Default expression (and default function) → `value` | A2 arch §4 | Projection value | Publication default test | Independent default-expression fixture | evidence log §D | VERIFIED |
| D3 | Interface / type alias → `type` | A2 arch §4 | Projection type | Shape tests | Independent interface + type-alias fixture | evidence log §D | VERIFIED |
| D4 | Class / enum → `both` | A2 arch §4 | Projection both | Shape tests | Independent class + enum fixture (TS) | evidence log §D | VERIFIED |
| D5 | Ordinary local export list → `unknown` | A2 arch §4 | Projection unknown | Publication tests (currentProject unknown) | Independent local-list fixture with mixed content | evidence log §D | VERIFIED |
| D6 | Nonambient namespace → `unknown` | A2 arch §4 | Projection unknown | Shape tests (namespace) | Independent namespace fixture (TS) | evidence log §D | VERIFIED |
| D7 | Explicit type-only form → `type` | A2 arch §4 | Projection type; type-only true | Publication + shape tests | Independent statement-level and inline type-only fixtures | evidence log §D | VERIFIED |
| D8 | No semantic resolution sharpens `unknown` (aliased, imported-local, same-name declaration) | A2 arch §4 | Projection remains unknown for local lists regardless of local declarations | Handoff §E claim; shape tests | Independent fixture with a same-name declaration and an aliased list | evidence log §D | VERIFIED |
| D9 | JavaScript type-only-like forms excluded | EC-6C.1 handoff; arch §10 | `export type { A }` and `export { type X }` in JS produce no entries (or exactly the grammar-grounded shape only) | Shape tests claim JS exclusion | Independent JS fixtures for statement-level and inline `type` forms | evidence log §D | PARTIAL — statement-level form excluded; inline recovered form (`export { type Model as Shape }`) publishes an entry while the parse error stays visible; recorded as finding FQ1-DOC-1 |

## E. Unicode and A29

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| E1 | 1–256 Unicode scalar values accepted | A29 closure §3; arch §3 | 256 supplementary scalars accepted | Publication unicode test (256) | Independent boundary fixture (255/256/257 supplementary) | evidence log §E | VERIFIED |
| E2 | 257 supplementary scalars rejected | A29 closure §3 | No entry, no edge, no truncation | Publication unicode test (257 rejected) | Independent 257 fixture asserting absence | evidence log §E | VERIFIED |
| E3 | Malformed surrogate sequences rejected | A29 closure §3 | No entry or edge; no replacement | Publisher invalid-name test (lone surrogate) | Independent malformed-surrogate source fixture and direct validator probe | evidence log §E | VERIFIED |
| E4 | Empty and all-whitespace names rejected | A29 closure §3; arch §3 | No entry or edge | Shape tests | Independent fixtures: `""` alias, whitespace-only alias | evidence log §E | VERIFIED |
| E5 | Control-containing names rejected | EC-6C.1 acceptance §4 | No entry or edge | Shape tests | Independent fixture with control characters in quoted alias | evidence log §E | VERIFIED |
| E6 | No normalization, trimming, replacement, or truncation | A29 closure §6 | Exact spelling preserved; NFC ≠ NFD | Publication unicode test (é variants) | Independent NFC/NFD + padded fixture; assert exact Name spelling and IDs | evidence log §E | VERIFIED |
| E7 | Surviving names retain exact spelling and identity across persistence, reopen, exact-name queries | A29 closure §8; handoff §F | LatticeDb close/reopen then exact-name lookup returns identical node | `LatticeDb_ReopensAndQueriesMaximumUnicodeExportName` | Independent durable fixture with a distinctive supplementary-scalar name | evidence log §E, §F | VERIFIED |

## F. Publication lifecycle

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| F1 | Export facts owned by the existing per-file publication unit; no new unit type | A2 arch §11; handoff §F | Export nodes/edges appear only in the file's unit | Lifecycle tests query through stores | Inspect published units structure in independent lifecycle test | evidence log §F | VERIFIED |
| F2 | Initial publication in both InMemoryCodeGraphStore and LatticeDb | A2 arch §11 | Facts visible via queries | Durable theory test | Independent dual-store fixture | evidence log §F | VERIFIED |
| F3 | Replacement: alias change and statement movement | A2 arch §3, §5 | Stale entry/edge removed; moved occurrence keeps entry ID, changes edge ID | `IndexAsync_ReplacesAliasesAndRemovesFinalExportFacts` | Independent lifecycle fixture with movement + rename | evidence log §F | VERIFIED |
| F4 | Removing one of two duplicate occurrences | A2 arch §5 | One edge removed; entry and sibling edge retained | Publication replace test (rename one of two) | Independent duplicate-removal fixture | evidence log §F | VERIFIED |
| F5 | Final export removal / empty replacement | A2 arch §11 | All export facts of the file removed; unrelated file facts retained | Publication replace test | Independent empty-replacement fixture checking declaration retention | evidence log §F | VERIFIED |
| F6 | File deletion removes the file unit and its export facts | A2 arch §11 | No exports edges remain; file node gone | Publication delete test | Independent file-deletion fixture | evidence log §F | VERIFIED |
| F7 | Failure before commit retains prior publication | A2 arch §6 (5) | Previous facts intact; run failed | `ResolverFailureRetainsPreviouslyPublishedExportFacts` | Independent failure fixture (both stores) | evidence log §F | VERIFIED |
| F8 | Cancellation before commit retains prior publication | A2 arch §6 (5) | Previous facts intact; run cancelled | `CancellationRetainsPreviouslyPublishedExportFacts` | Independent cancellation fixture (both stores) | evidence log §F | VERIFIED |
| F9 | Close and reopen (LatticeDb) preserves export facts and query behavior | Handoff §F | Exact-name query works after reopen | Unicode reopen test | Independent close/reopen with ordinary names + query | evidence log §F | VERIFIED |
| F10 | No stale entries/edges; unrelated facts untouched by export replacement | A2 arch §11 | Post-replacement state exactly = expected fact set | Lifecycle tests assert nulls | Independent exact-state comparison after each lifecycle step | evidence log §F | VERIFIED |

## G. p8-to-p9 compatibility

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| G1 | Authentic persisted p8 seed from pre-publication implementation reindexed under p9 | Handoff §G; A2 arch §11 | p8 seed produced at `8c1d5e3` checkout; reindexed in p9 worktree | Handoff migration report (uncommitted tooling) | Disposable detached worktree at `8c1d5e3`; LatticeDb seed; p9 reindex; full comparison report | `migration-p8-to-p9-report.md` | VERIFIED |
| G2 | Baseline counts: p8 7 nodes/4 declarations/12 edges → p9 12 nodes/4 declarations/17 edges; +5 entries, +5 edges | Handoff §G | Exact counts for the recorded fixture | Shape fingerprint test asserts p9 counts | Reproduce counts for TypeScript and JavaScript fixtures | migration report | VERIFIED |
| G3 | Existing filtered facts byte-identical; snapshot SHA-256 `ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f` | Handoff §G | Filtered p8 snapshot == filtered p9 snapshot | Handoff claim (serialization format not committed) | Reproduce comparison with deterministic serialization; attempt to reconcile the recorded hash; document the exact format used | migration report | PARTIAL — byte-identical comparison independently demonstrated for both languages; the recorded ff6b88… hash was not matched by three natural serializations (tooling uncommitted); see migration report |
| G4 | Existing identities and evidence provider/version retention | Handoff §G | Existing facts keep p8 plugin evidence; declarations/symbols unchanged | Shape test asserts `.p8` suffixes | Compare complete fact lists pre/post migration | migration report | VERIFIED |
| G5 | Planner metadata progression to p9 (invalidation) | A2 arch §11 | Plugin version p9; p8 publications re-planned | Plugin version test; planner test asserts `.p9` | Independent planner verification in migration harness (manifest versions) | migration report | VERIFIED |
| G6 | `resolver2.relative-v1` unchanged | A2 arch §8, §11 | Resolver provider/version identical before and after | Shape tests assert resolver version | Compare resolver facts pre/post migration | migration report | VERIFIED |
| G7 | Query behavior after close/reopen of the migrated store | Handoff §G | Exact-name query and neighborhood query work on reindexed p9 store | Handoff claim | Reopen migrated store; run neighborhood + exact-name queries | migration report | VERIFIED |

## H. Regression and consumer compatibility

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| H1 | The 18 modified TreeSitter assertions correctly scope old expectations to declarations and pre-existing relationship kinds | Handoff §H, §I | Each modification justified by additive export facts; no coverage lost | Commit `b61b45e` diff | Line-by-line audit of the diff; classify each hunk; check the `ResolvesTo` provider-assertion exclusion | `compatibility-audit.md` | VERIFIED WITH NOTE — all 18 hunks audited; 17 correctly scoped and necessary; 1 unnecessary `ResolvesTo` exclusion (note QN-1, no current behavior masked) |
| H2 | Previously supported relationships unchanged | A2 arch §11 | All p8 relationship kinds still produced (imports, contains, calls, references, resolves-to, heritage) | Full suite + fingerprint test | Run full suites; inspect extraction fixtures for all prior kinds | evidence log §H | VERIFIED |
| H3 | Consumer queries: exact-name lookup returns export entry alongside declaration; declaration navigation selects symbol-bearing nodes only | A2 arch §9; handoff §I | Additive candidates; Kind/SymbolId distinguishability | Modified lifecycle tests filter `SymbolId is not null` | Independent consumer-facing query test (lookup + declarations + neighborhood) | evidence log §H | VERIFIED |
| H4 | No breaking change to existing node/edge schemas | A2 arch §3 | New kinds/properties are additive only; existing serialization unchanged | Public API contract tests | Run contract test projects; inspect added constants | evidence log §H | VERIFIED |
| H5 | No export facts leaking into consumers expecting another fact category | A2 arch §7, §9 | References/impact/traversal presets unchanged; exports only on explicit request | Publication tests assert coverage absence | Independent neighborhood query without `exports` filter; references query | evidence log §H | VERIFIED |
| H6 | Documentation claims not broader than implemented behavior | A2 arch §7; handoff §K | Positive-only wording; no completeness claims | Handoff §K limitations | Documentation audit against observed behavior | `compatibility-audit.md` | VERIFIED |

## I. Repository smoke

| # | Requirement | Source specification | Expected observable behavior | Existing coverage (claim) | Independent test / inspection | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- | --- | --- |
| I1 | Pinned website `f9cca1773a32bb517e624cc380e1f6d14e0cc317`; nine supported files indexed (8 TypeScript + `astro.config.mjs`) | Handoff §J | Only those files produce facts; 22 `.astro` files absent | Handoff smoke report | Independent indexing of the pinned clone; verify file set | `website-smoke-inventory.md` | VERIFIED |
| I2 | Allowlist independently enumerated | Handoff §J | Manual enumeration matches published occurrences per file | Handoff per-file table | Independent manual reading of each supported file + expected-fact comparison (names, spans, IDs) | website smoke inventory | VERIFIED |
| I3 | 33 expected and 33 observed export occurrences/entry slots | Handoff §J | Exact per-file counts and names | Handoff table | Independent count and identity comparison | website smoke inventory | VERIFIED |
| I4 | `.astro` files outside scope; no fabricated entries | Handoff §K | No facts for `.astro` paths | Handoff claim | Verify absence of `.astro` paths in indexed units | website smoke inventory | VERIFIED |
| I5 | No repository-wide completeness claim | Handoff §J, §K | Report asserts allowlist only | Handoff wording | Wording audit | website smoke inventory | VERIFIED |

## J. Baseline reproduction and environment

| # | Item | Expected (claim) | Independent command | Evidence artifact | Result |
| --- | --- | --- | --- | --- | --- |
| J1 | Focused export suite | 25 passed, 0 failed, 0 skipped | `dotnet test tests\Penghou.Hetu.TreeSitter.Tests\... --filter FullyQualifiedName~TreeSitterEcmaExport` | run log | VERIFIED |
| J2 | Full solution | 388 passed, 0 failed, 0 skipped (160/156/26/46) | `dotnet test Penghou.Hetu.slnx` | run log | VERIFIED |
| J3 | Format verification | no changes | `dotnet format --verify-no-changes` | run log | VERIFIED |
| J4 | Whitespace check | clean | `git diff --check` | run log | VERIFIED |

## Execution notes

- Independent tests are kept in a separate, clearly named file:
  `tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportQualificationQ1Tests.cs`
  (namespace-prefixed `QualificationQ1`). They are additive and do not modify
  production code. If a defect is found, it is recorded as a finding; the
  production code is never repaired silently.
- Evidence artifacts live under `docs/evidence/ec6c2-glm-q1/`.
