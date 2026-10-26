# EC-6C.2 GLM-Q1 Evidence — Compatibility and Documentation Audit

**Qualification:** EC-6C.2 independent qualification GLM-Q1
**Date:** 2026-10-26

## 1. Audit of the 18 modified TreeSitter assertions (`b61b45e`)

Commit `b61b45e6988b0bf2bf161f02055d2536893dd654` ("test(treesitter): preserve
compatibility with export entries") modified 18 assertion hunks across seven
legacy test files. Each hunk was classified:

### Category 1 — declaration selection now filters `SymbolId` (11 hunks)

`TreeSitterEcmaClassLifecycleTests.cs` (4), `TreeSitterEcmaClassMemberTests.cs`
(1), `TreeSitterTypeScriptPluginTests.cs` (3: `Widget`, `Shape`, and
`Shape.perimeter` lookups), `TreeSitterEcmaVariableLifecycleTests.cs` /
`TreeSitterEcmaVariableTests.cs` (3: helpers now select `Kind == Field` or
`SymbolId is not null`).

Assessment: **correctly scoped.** `FindSymbolsByQualifiedNameAsync` is
documented (A2 arch §9) as an exact-name candidate query that does not filter
by kind or SymbolId; p9 legitimately adds declaration-less export entries as
additional candidates. The modifications preserve the original intent —
exactly one declaration candidate — while acknowledging the additive export
entry. The export entries themselves are still exercised by the new export
tests, and the class-lifecycle "two Service declarations" test was extended
(rather than weakened) to also assert the two module export entries. No
previously supported relationship or query behavior lost coverage.

### Category 2 — plugin version p8→p9 (3 hunks)

`TreeSitterJavaScriptPluginTests.cs` (1), `TreeSitterTypeScriptPluginTests.cs`
(2: plugin version, planner re-extraction assertion).

Assessment: **required by the authorized profile revision bump** (A2 arch §11).
The planner hunk still asserts that an older-profile publication is re-planned,
preserving the invalidation regression.

### Category 3 — `exports`-edge absence assertions narrowed (2 hunks)

- `TreeSitterModuleResolutionTests.cs` resolver fixture: previously asserted no
  `exports` edges anywhere; now only `binds`. **Necessary:** the fixture's
  `src/projects.ts`, `types.d.ts`, `src/folder/index.ts`, and
  `src/folder2/index.ts` legitimately carry 4 export edges at p9.
- `TreeSitterModuleResolutionTests.cs` `export = require(...)` fixture:
  previously no `exports` edges anywhere; now scoped to
  `SourceId == file(src/legacy.ts)`. **Necessary:** `src/item.ts` legitimately
  produces one export edge at p9. The `binds` assertion is retained.

### Category 4 — all-edge provider assertion filtered (1 hunk)

`TreeSitterTypeScriptPluginTests.ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`
now asserts the TypeScript provider on edges except `Exports` and `ResolvesTo`.

Assessment: the `Exports` exclusion is **necessary** (the fixture produces 7
export edges with the export-surface provider; verified by direct extraction of
the fixture: 7 export edges, no resolves-to edges, all other edges carry
provider `penghou.hetu.tree-sitter.typescript` version `.p8`). The
`ResolvesTo` exclusion is **unnecessary for this fixture** — it contains no
resolves-to edges (its `node:fs`/`node:path` imports are external and
unresolved, `UnresolvedRelationships = 0`). The exclusion therefore narrows
future regression coverage marginally (a resolves-to edge with a wrong
provider introduced into this fixture would no longer be caught here), but it
masks no current behavior; resolver provider/version remain explicitly asserted
in the module-resolution tests, and the shape fingerprint test asserts
resolver2 evidence on fixtures that do contain resolves-to edges.

**H1 conclusion:** no previously supported relationship, identity, or query
behavior lost regression coverage; 17 of 18 hunks are correctly scoped and
necessary; 1 hunk contains an unnecessary (but currently harmless) exclusion
recorded as qualification note QN-1.

## 2. Regression and consumer compatibility checks (H2–H5)

- Full suites pass on the qualification worktree (TreeSitter 156, Hetu 160,
  LatticeDb 26, CSharp 46) with no skipped tests; the qualification adds 27
  focused tests (all passing).
- The p8 pre-export fingerprint is preserved byte-identically in the p9
  shape test, and the persisted migration comparison (see migration evidence)
  demonstrated byte-identical existing facts.
- Independent consumer-facing query exercise (qualification test
  `Q1_H_ConsumerQueriesDistinguishExportEntriesFromDeclarations`, both
  stores): exact-name lookup returns the export entry and the declaration as
  distinguishable candidates; `FindDeclarationsAsync` navigates only the
  symbol-bearing node; containment-filtered neighborhood returns no export
  edges; `FindReferencesAsync` on an export entry is empty; explicit
  `exports`-filtered neighborhood returns the expected two edges.
- No public query API was added or changed (production diff touches only
  `CodeGraphKinds`, `CodePropertyKeys`, the publisher, and the profile).
- Traversal presets (`References`, `Containment`, `ImpactSet`, `Dependencies`)
  do not include `CodeEdgeKinds.Exports`, and `exports` is absent from the
  session's `CoverageKinds`, so export facts cannot leak into
  `RelationshipCoverage` or impact/reference consumers.

## 3. Documentation audit (H6)

- The implementation handoff's §G migration table, §H test counts, §I
  compatibility statements, and §J website smoke (nine files, 33 occurrences,
  35,810 CRLF-UTF-16 source bytes) were all independently reproduced; see the
  migration and smoke evidence files.
- One claim is broader than observed behavior: the EC-6C.1 handoff states
  "JavaScript type-only-like recovery is excluded"
  (`docs/evidence/ec6c-1-export-shape-inventory-handoff.md` line 100). The
  exclusion holds for the JavaScript **statement-level** form
  (`export type { … }`, no facts) but not for the **inline** recovered form
  (`export { type Model as Shape }` in JavaScript, which publishes an entry
  while the parse error remains visible). Recorded as finding FQ1-DOC-1; the
  published entry carries no `type-only` flag and an `unknown` projection, so
  no false type/value attribution occurs.
- Positive-only semantics are consistently documented (handoff §K, A2 arch §7);
  no completeness, attribution, binding, or coverage claims were found in the
  changed documentation.
