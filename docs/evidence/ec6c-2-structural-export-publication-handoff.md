# EC-6C.2 Structural Export Publication Handoff

**Project:** Penghou.Hetu (future Andirit)
**Date:** 2026-10-25
**Branch:** `implementation/ec6c-2-structural-export-publication`
**Disposition:** **EC-6C.2 IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION**

This record binds local implementation and verification evidence. It is not an
independent review or owner closure. EC-6C.1 and EC-6C.2 still require explicit
independent findings and owner disposition before integration or release.
Track B, EC-6D, integration, merge, push, tagging, and release are outside this
work.

## A. Git binding

Repository: `jenolaszlo-sketch/penghou-hetu`; canonical checkout:
`C:\Users\Laszlos\source\repos\Penghou.Hetu`. All EC-6C.2 changes are in
the separate worktree
`C:\Users\Laszlos\source\repos\Penghou.Hetu.ec6c-2-structural-export-publication`.
The canonical checkout and EC-6C.1 branch were not modified.

| Binding | Commit | Tree / relationship |
| --- | --- | --- |
| Accepted EC6C-A2 design | `bf4ffcc25c22a6b8f98fb2b3f513dd95e5f8f49f` | Tree `8abb3f12b4344a17b8a530c42c10c01130daabed` |
| EC-6C.1 implementation | `b1998d64bb7d2811be5ac1a14bda24df4b1cbef6` | Based directly on EC6C-A2 |
| EC-6C.1 final handoff | `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` | Tree `8a3cc154486bf9796b532ee4db96714fa63763b2`; child of the implementation commit |
| EC-6C.1 owner acceptance | `d81108dd51d61fe79767e15c4adb4c10659836de` | First EC-6C.2 branch commit; direct child of `8c1d5e3` |
| Acceptance documentation cleanup | `f73d3e457399766fabcf6094a7d4a2f33414b938` | Child of the acceptance commit |
| EC-6C.2 production implementation | `3bf418d372b125107058f5985a5a311ce825b156` | Child of the acceptance cleanup |
| Export qualification tests | `b54eb3be258656feefedd939f38f398b4fe1dd86` | Tree `8e064823d3d31d680e93f9bcdb1a090d30faf885`; child of production implementation |
| Legacy query compatibility tests | `b61b45e6988b0bf2bf161f02055d2536893dd654` | Tree `3ad7f924b9c84caa28823b67d742258e0b47ef65`; child of export qualification tests |

This handoff and the final documentation updates are committed after
`b61b45e`. The final evidence commit and resulting tree are reported alongside
this file in the task completion record. No merge, push, or release was made.

## B. EC-6C.1 owner acceptance

Disposition: **EC-6C.1 OWNER ACCEPTED WITH QUALIFICATIONS — EC-6C.2
IMPLEMENTATION AUTHORIZED**. The decision is recorded in
[`../decisions/ec6c1-owner-acceptance.md`](../decisions/ec6c1-owner-acceptance.md)
at commit `d81108dd51d61fe79767e15c4adb4c10659836de`.

The owner accepted the EC-6C.1 implementation as the foundation for this
bounded publication step based on its local report. No independent EC-6C.1
implementation verification has been completed, and this acceptance does not
claim a reviewer verdict. The record accepts the allowlisted rejection of
empty, all-whitespace, control-containing, malformed-surrogate, and
over-bound public names as consistent with the A29 closure. Independent
verification of the shape collector, contextual `type` recovery, spans,
Unicode, multiplicity, and unchanged p8 graph facts remains required.

## C. Graph contract and identities

For accepted TypeScript and JavaScript observations, publication adds:

- One `export` node per distinct exported name in a module.
- One Structural `exports` edge from that module's file node to the entry per
  supported written occurrence.
- No declaration `SymbolId`, physical declaration record, or edge to a
  declaration.

An entry has `Name` and `QualifiedName` equal to the exact exported name,
`SymbolId = null`, and only the existing `language` node property. Its
module-scoped identity is:

```text
{language}:export:{lowercase-hex-SHA256(UTF8(modulePath + LF + exportedName))}
```

`modulePath` is the canonical repository-relative path supplied by the
existing file extraction unit. Names and paths are ordinal and case-sensitive;
Unicode is not normalized. The entry ID excludes source occurrence,
declaration identity, and type/value projection. Repeated occurrences of the
same name therefore share an entry.

Every occurrence edge points `file → export entry`, has kind `exports`, and
uses `Structural` evidence from provider
`penghou.hetu.tree-sitter.ecmascript-export-surface`. Its provider version is
`{packageVersion}+exports1.local-v1`. Properties are:

| Property | Value |
| --- | --- |
| `export-kind` | `named` or `default` |
| `export-via` | `direct`, `local`, or `expression` |
| `local-name` | Present only when the written syntax supplies a local name |
| `type-only` | Present with value `true` only for an explicit type-only form |
| `namespace-projection` | `type`, `value`, `both`, or `unknown` |

`Evidence.Location` is the exact public-name token span, with one-based,
end-exclusive UTF-16 code-unit columns. The whole export-construct span stays
in the immutable internal observation and is not published as a graph
property.

The occurrence ID is `{language}:` plus lowercase SHA-256 of strict UTF-8 over
the following newline-delimited tuple, in order:

```text
exports
fileNodeId
exportEntryNodeId
exportedName
export-kind
export-via
local-name-presence (0 or 1)
local-name (empty when absent)
type-only (true or false)
namespace-projection
startLine:startColumn:endLine:endColumn
```

The separate local-name-presence bit distinguishes an absent local name from
an explicitly empty value. Source span participates in occurrence identity;
construct span does not. Moving an occurrence changes its edge ID, while the
module/name entry ID stays fixed.

## D. Collision handling

The publisher checks candidates in the current extraction result before
adding them to its node and edge dictionaries. A repeated ID with an exactly
equivalent complete fact is idempotent. A repeated ID with a different fact
throws `InvalidOperationException` before the conflicting fact is inserted.
The diagnostic includes the ID and serialized existing and candidate facts,
including endpoints, kind, evidence provider/version/location, and properties.
The graph core's general insertion semantics were not changed.

Permanent tests exercise exact duplicate idempotence and deliberately
constructed same-ID/different-evidence edge and different-name node
collisions. Assertions inspect the actual identities, properties, endpoints,
and complete-fact diagnostics. The extraction operation fails; the existing
source-unit publication remains authoritative, as also checked through
failure and cancellation lifecycle tests.

## E. Syntax, projection, and Unicode

Publication consumes the existing EC-6C.1 observation list from the original
Tree-sitter parse; it does not parse source again or perform name-based
declaration lookup. The allowlist includes supported direct declarations,
simple direct variable declarators, local export lists and aliases,
imported-local spellings without remote attribution, default declarations
and expressions, multiple declarators, and grammar-recognized explicit
type-only forms. The collector retains only its documented exact contextual
`type` recovery shapes.

Projection remains syntax-grounded: direct variables and functions are
`value`; interfaces and type aliases are `type`; classes and enums are `both`;
nonambient namespaces and ordinary local export lists are `unknown`; explicit
type-only forms are `type`; default expressions/functions are `value` and a
default class is `both`. No lookup changes a projection, including for
imported locals or same-name declarations. Direct observations write
`export-via=direct`; local lists use `local`; default expressions use
`expression`.

Excluded forms include named, star, and namespace re-exports; CommonJS export
forms; TypeScript `export =`; unsupported ambient or destructured forms;
nested namespace exports; escaped public names; malformed or unsupported
grammar shapes. An unsupported form does not create an entry. Parse recovery
remains visible through the existing parse diagnostics.

The A29 public-name limit is 1–256 Unicode scalar values, strict rejection of
invalid UTF-16 surrogate sequences, and no normalization, trimming,
replacement, or truncation. Empty, all-whitespace, control-containing, invalid,
and over-bound names are excluded. Valid supplementary characters are
preserved exactly. Tests cover normalization distinction, source UTF-16
columns, the 256 supplementary-scalar boundary, rejection at 257, malformed
surrogate validation, and durable LatticeDb close/reopen of the 256-scalar
name. The durable test queries the exact preserved name and identity after
reopening the database.

## F. Per-file publication and stores

Export nodes and edges are appended to the existing per-file extraction result
after the owning file node is available and before the existing unit is
validated and staged. No separate export unit, resolution pass, or graph-core
publication path was introduced. The existing atomic publication lifecycle
provides replacement and deletion behavior.

The indexing tests exercise both `InMemoryCodeGraphStore` and LatticeDb for
initial facts, repeated occurrences, alias rename, removal of one duplicate,
final export removal, empty replacement, file deletion, and failure/cancellation
retention. LatticeDb tests close and reopen the store and verify exact-name
lookup, entry identity, Unicode text, edge target, Structural evidence, and
persisted occurrence data. No test was skipped.

## G. p8→p9 versioning and authentic persisted migration

Both TypeScript and JavaScript extraction profiles advance from p8 to p9.
Actual source-unit/plugin metadata uses p9 so the planner invalidates unchanged
p8 sources. Existing file, declaration, symbol, structural-edge, and
`resolves-to` facts preserve their prior p8 evidence provider versions because
their facts are unchanged. New export relationships use
`{packageVersion}+exports1.local-v1`. The resolver remains
`{packageVersion}+resolver2.relative-v1`; export edges are not added to
relationship coverage or `UnresolvedRelationships`.

An authentic p8 seed was produced by the pre-publication EC-6C.1 implementation
at `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` in a disposable detached
qualification checkout, persisted in LatticeDb, then reindexed from the same
source under p9 in this worktree. The seed was not a fabricated p8-versioned
p9 graph. Full p8 facts were compared against the p9 graph filtered to the
existing fact set:

| Measurement | p8 | p9 |
| --- | ---: | ---: |
| Nodes | 7 | 12 |
| Physical declarations | 4 | 4 |
| Edges | 12 | 17 |
| Added export entries | — | 5 |
| Added Structural `exports` edges | — | 5 |

The existing-fact snapshots were byte-identical (`true`); their SHA-256 was
`ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f`. The p9
LatticeDb publication was closed and reopened; exact-name query found the
expected entry, and a file neighborhood returned its three expected export
occurrences. The temporary migration report recorded the plugin versions
`0.2.0-preview.6+v0.23.2.abi14.p8` and
`0.2.0-preview.6+v0.23.2.abi14.p9`. Existing identities were compared as
complete graph facts, not just counts.

The pinned website was smoke-tested under p9 separately; it was not used as
the p8 seed for the migration above. The prior EC-6B website migration record
documents its own verified baseline and must not be conflated with this
synthetic-source p8→p9 comparison.

## H. Tests and local verification

Environment: Windows; .NET SDK `10.0.401`; Microsoft.NETCore.App
`10.0.12`; tests target `net10.0`.

Final focused export qualification, on the committed production and
compatibility-test source:

```text
dotnet test tests\Penghou.Hetu.TreeSitter.Tests\Penghou.Hetu.TreeSitter.Tests.csproj --no-restore --filter FullyQualifiedName~TreeSitterEcmaExport --verbosity minimal
Passed: 25, Failed: 0, Skipped: 0
```

Final full solution run:

```text
dotnet test Penghou.Hetu.slnx --no-restore --verbosity minimal
Penghou.Hetu.Tests: 160 passed, 0 failed, 0 skipped
Penghou.Hetu.TreeSitter.Tests: 156 passed, 0 failed, 0 skipped
Penghou.Hetu.LatticeDb.Tests: 26 passed, 0 failed, 0 skipped
Penghou.Hetu.CSharp.Tests: 46 passed, 0 failed, 0 skipped
Total: 388 passed, 0 failed, 0 skipped
```

A Tree-sitter-only run passed 156/156 with no skips after the main
compatibility fixes. The committed full solution run, after the final explicit
query-candidate assertion was added, also passed all 156 Tree-sitter tests.
Formatting verification used `dotnet format Penghou.Hetu.slnx
--verify-no-changes --no-restore`; `git diff --check` passed.

Failed attempts are recorded for context. The first solution-wide run after
adding graph entries had 18 Tree-sitter failures (138 passed, 18 failed); the
other projects passed 232 tests. Those failures were legacy expectations that
looked up declarations without filtering node kind/SymbolId, rejected any
`exports` edge, or expected p8 plugin versions. The compatibility tests were
updated to distinguish export entries from declarations and preserve the
resolver-only assertions. The first rerun of those edits caught three
test-code compile errors from two edit mistakes (using the nonexistent
`CodeEdgeKinds.Binds` member twice and treating an array's LINQ `Count` method
as a property), which were corrected. The next Tree-sitter run had one remaining
exact lookup for `NewWidget`; it was changed to select the declaration node.
The focused 25-test run and final full 388-test run passed on the committed
source. No skipped test masked an unresolved failure.

## I. Compatibility

Exact `QualifiedName` lookup can now return an `export` entry alongside a
declaration with the same name. Consumers identify entries by `Kind=export`
and their lack of `SymbolId`; declaration navigation continues to select
symbol-bearing nodes and use `CodeGraphDeclaration`/`SymbolId`. The generic
lookup API was not changed. No new query API was added. Existing
`FindReferencesAsync` behavior, impact presets, and traversal defaults remain
unchanged; consumers can request `exports` explicitly.

The module resolver and `resolves-to` meaning are unchanged. Export edges do
not add module coverage claims. Regression tests cover existing p8 TypeScript
and JavaScript graph fingerprints, resolver provider versions, exact queries,
declaration navigation, and module-resolution tests. The tests with broad
all-edge assumptions were narrowed to the pre-existing relationship kinds.

## J. Pinned website smoke

Source binding: local clone of `jenolaszlo-sketch/website` at
`f9cca1773a32bb517e624cc380e1f6d14e0cc317`; clone status was clean on `main`.
The prior EC-6B evidence records 128 nodes, 83 declarations, and 280 edges for
its qualified website graph. The EC-6C.2 smoke inspected the pinned source
snapshot and indexed its nine supported files (eight TypeScript files plus
`astro.config.mjs`, 35,810 source bytes). It manually enumerated the
allowlisted written exports and compared them with p9 output: 33 expected and
33 observed occurrences/entry slots, with no parser warnings. The per-file
inventory was:

| File | Observed exported names |
| --- | --- |
| `astro.config.mjs` | `default` |
| `src/content.config.ts` | `collections` |
| `src/env.d.ts` | none |
| `src/lib/ecosystem.ts` | `Status`, `StatusDefinition`, `STATUSES`, `STATUS_BY_KEY`, `Category`, `CATEGORIES`, `CATEGORY_BY_KEY`, `ProjectRef`, `RuntimeLayer`, `RUNTIME_LAYERS`, `SupportingPrimitive`, `SUPPORTING_PRIMITIVES` |
| `src/lib/format.ts` | `formatDate`, `formatDateISO` |
| `src/lib/paths.ts` | `withBase` |
| `src/lib/schema.ts` | `personNode`, `websiteNode`, `profilePageNode`, `ProjectSchemaInput`, `softwareSourceCodeNode`, `BlogPostSchemaInput`, `blogPostingNode`, `BreadcrumbItem`, `breadcrumbNode`, `graph` |
| `src/lib/site.ts` | `SITE`, `NavItem`, `NAV`, `FounderProfile`, `FOUNDER` |
| `src/pages/robots.txt.ts` | `GET` |

The 22 `.astro` files were not indexed. The smoke asserts only the enumerated
positive allowlist and absence of parser warnings; it does not establish
complete exports for any module or support for Astro syntax. It found no
`exported-as`, `binds`, or export-coverage claims. This is local smoke evidence,
not independent real-repository qualification.

## K. Known limitations

- The inventory is positive-only: an absent entry does not establish that a
  module has no exports or that all syntax was understood.
- Named/star/namespace re-exports, CommonJS, `export =`, unsupported ambient,
  nested namespace, destructured, escaped, malformed, or otherwise unaccepted
  syntax remains excluded.
- Export-name nodes are not declarations, resolved symbols, verified local
  bindings, runtime values, or proof of a complete export surface.
- Import-to-export binding, declaration attribution, re-export closure,
  Track B, and EC-6D are unauthorized here.
- `.astro` files were outside the pinned website smoke and remain unsupported.
- EC-6C.1 independent verification and EC-6C.2 independent qualification are
  outstanding. No integration or release is authorized.

## L. Independent qualification instructions

The independent reviewer should bind conclusions to the implementation and
test commits listed in §A and issue separate findings/dispositions for
EC-6C.1 and EC-6C.2. Highest-risk checks and counterexamples:

1. Compare `export { project as currentProject };` and a duplicate occurrence:
   both share one module/name node, but each source occurrence has its own
   Structural edge and exact public-name token span.
2. Move the statement, rename the alias, remove only one duplicate, then remove
   the final occurrence. Verify the slot identity stays stable for movement,
   alias identity changes, stale facts disappear, and the surviving duplicate
   remains.
3. Force a same-ID/different-fact node and edge collision. Require complete
   existing/candidate facts in the error, no conflicting graph insertion, and
   unchanged previous publication.
4. Compare identical names in two module paths, case-distinct names, and
   canonically equivalent but differently encoded Unicode names. Verify
   module scoping, ordinal identity, and no normalization.
5. Recheck explicit `export type`, inline `type Model`, ordinary local
   `export { type }`, and imported locals. Confirm only grammar-grounded
   projections are emitted and no same-name declaration lookup sharpens
   `unknown`.
6. Exercise file replacement, deletion, empty replacement, failure, and
   cancellation in both stores. Reopen LatticeDb after publishing the maximum
   256-supplementary-scalar name and query the exact name and edge.
7. Reproduce a durable authentic p8→p9 migration. Compare complete old facts,
   ensure planner invalidation, check five additions on the recorded fixture,
   and confirm resolver facts remain under `resolver2.relative-v1`.
8. Query an exported declaration name and verify generic lookup returns
   distinguishable export and declaration nodes; declaration navigation must
   continue through the symbol-bearing node only.
9. Challenge unsupported source-bearing re-exports, star/namespace exports,
   CommonJS, `export =`, malformed/escaped names, and `.astro` files. None may
   produce fabricated entries or completeness claims.

The existing EC-6C.1 handoff at commit
`8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`, this owner record, the accepted
A1/A2 architecture, and the A29 closure remain the review inputs. Passing
local tests is not the independent qualification or owner closure.
