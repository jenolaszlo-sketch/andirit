# TypeScript consumer baseline

## Selection and binding

The exercise uses a committed, purpose-built Apache-2.0-compatible fixture
because the available pinned website smoke contains only nine supported
TypeScript/JavaScript files and did not provide the controlled same-name
negative, imported alias, re-export, unresolved module and inheritance cases
needed for a manually reviewed impact test. The fixture is intentionally
small and is not a global accuracy or performance benchmark.

| Artifact | Binding |
| --- | --- |
| Source baseline | EC-6C owner acceptance 2c3090cba6d8a56102a5c47ceb026c5b5fea3234, tree ece772f1f2b2497891f4019d0d85f4d852b197e1 |
| Harness and fixture commit | 1c9f4c9f12e82fe35261096ff3c96343336bdb63 |
| Fixture tree | 014da71f0855cdc957fa9b6a0667bca565145092 |
| Corpus path | docs/planning/ec6d/corpus/ts-interface-impact |
| Plugin | TypeScript Tree-sitter 0.2.0-preview.6+v0.23.2.abi14.p9 |
| Store | InMemoryCodeGraphStore |
| Environment | Windows x64, .NET SDK 10.0.401 |

The corpus has thirteen TypeScript source files and one Markdown README. It
contains two same-named Renderable interfaces, four intended direct
implementations (one using an imported alias), one inherited renderer, direct
and barrel importers, direct exports and a re-export barrel, a missing import,
and type-annotated consumers. The manual expected set is recorded in the
fixture README. It is a source-reviewed syntax expectation, not a compiler
type-check result.

## Primary question

For the declaration src/contracts/Renderable.ts / Renderable, which direct
implementations and consumers may be affected by a change, and what evidence
supports each result?

## Commands

Run from the repository root. The local NuGet vulnerability-data lookup is
unavailable in this environment and the repository treats NU1900 as an error,
so restore disables only the online audit lookup. Package versions remain
specified by project files.

~~~powershell
dotnet restore docs/planning/ec6d/analysis/ConsumerBaseline.csproj --ignore-failed-sources -m:1 -p:RestoreUseStaticGraphEvaluation=true -p:NuGetAudit=false
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
dotnet run --no-build --project docs/planning/ec6d/analysis/ConsumerBaseline.csproj -- docs/planning/ec6d/corpus/ts-interface-impact
~~~

The harness indexes source with the production indexing and query APIs, exports
the latest publication, queries the selected declaration and exact-name
candidates, then records facts, provenance, locations, evidence and bounded
module/export neighborhoods. The output is
[observed-results.json](analysis/observed-results.json); reproduction details
and regression-suite logs are in [analysis/README.md](analysis/README.md).

The file-system-backed publication reports PublicationConsistent=false: this
provider does not guarantee an immutable source snapshot during enumeration.
The fixture was not edited during the runs, but the run does not claim a
repository snapshot isolation guarantee. This is a limitation of the
experiment, not a failed graph publication.

## Observed extraction

| Measure | Result |
| --- | ---: |
| Discovered files | 14 |
| TypeScript files examined | 13 |
| Unsupported Markdown files | 1 |
| Completed index units | 14 |
| Node contributions / distinct nodes | 73 / 64 |
| Declarations | 23 |
| Distinct edges | 99 |
| Aggregate unresolved relationships | 1 |
| Warnings | 0 |

Distinct relationship counts:

| Relationship | Count |
| --- | ---: |
| contains | 23 |
| declares | 23 |
| exports | 13 |
| implements | 5 |
| imports | 17 |
| inherits | 1 |
| invokes-name | 1 |
| resolves-to | 16 |

No calls or references edges were produced. The single unresolved count is
aggregate diagnostic evidence; the graph does not expose a detailed
queryable unresolved-reference node for MissingImport.

## Query outcome and reference comparison

Exact qualified-name lookup for Renderable returns eight candidates: two
interface declarations, two export entries and four declaration-less
heritage-target nodes. The selected declaration is unambiguous only after the
consumer supplies its file/declaration identity.

| Method | Expected / observed | Outcome |
| --- | --- | --- |
| FindImplementations on the selected interface declaration | 4 / 0 | No relationship points at the selected declaration. Recall is 0/4; precision is undefined because there are no positive candidates. |
| Name-match the four Renderable heritage placeholders | 4 expected / 4 observed | Three true positives (SvgRenderer, HtmlRenderer, AbstractRenderer), one false positive (OtherRenderer, which implements another same-named interface), and one missed expected implementation (AliasRenderer, whose placeholder is named Drawable). Precision 3/4 = 0.75; recall 3/4 = 0.75 for this narrow, manually reviewed direct-implements class only. |
| Direct resolved module dependencies to contracts/Renderable.ts | 6 expected / 6 observed | AbstractRenderer, api/index.ts, HtmlRenderer, SvgRenderer, Preview and AliasRenderer. These are file-to-file facts; they do not prove the imported symbol or type use. |
| Direct export entries on contracts/Renderable.ts | 2 / 2 | Renderable and RenderContext, with Structural occurrence edges and exact public-name token spans. |
| Re-export entries on api/index.ts | 3 written / 0 export edges | Module-file resolution is present; EC-6C does not publish those re-export names as export entries. |
| Type-annotated consumers | 2 intended / 0 symbol-linked results | Preview.preview and BarrelPreview.previewFromBarrel are visible in source, but imports, annotations and declaration identity are not joined by this graph. |

The built-in implementation and impact query against the selected declaration
returns only the seed node. Querying the four same-spelling heritage
placeholders yields three correct classes and the unrelated OtherRenderer.
The imported alias is absent from that name-matched set. These scores describe
only the explicit direct-implements slice of this fixture; they are not a
global precision or recall estimate.

CanvasRenderer's written inheritance of the BaseRenderer placeholder is
present. It does not establish a transitive implementation relationship to
Renderable. MissingImport has no resolved module edge. The module resolver's
Resolved evidence links files according to relative-v1; it does not establish
symbol binding or compiler/bundler/runtime equivalence.

## Explainability and determinism

Export and resolves-to facts carry source locations, evidence quality,
provider/profile and per-unit origin. The queries return provenance envelopes.
This is enough to inspect the syntax or resolver policy behind a returned
edge, but not enough to explain the intended interface impact: the critical
binding edge is missing. The query API also supplies no source excerpt.

Two equivalent fixture runs produced the same IndexIdentity
667ec2d4df6b821e63b2f7d1adad6c63492aaf453b9af639f1aa6e9b0fa77ec8 and
CanonicalFactsSha256
05bf7c7de0e5e1a552c77759cc3bba39aff8d72e093405379e5acfd2d99b0d3c. The
snapshot integrity hash includes publication metadata and is not the
cross-run fact digest.

## Conclusion

The pipeline succeeds at extracting structural declarations, explicit
heritage syntax, relative module-file edges and positive export-name
occurrences with provenance. It cannot answer the primary consumer question
from an interface declaration: direct implementation lookup returns none,
name matching creates one false positive and misses an imported alias, and
the two visible type-use consumers cannot be linked. The main missing step is
symbol binding across declaration, export, import and heritage facts, followed
by a query that reports a bounded, source-backed impact result.

## R1 reproduction note (EC6D-PLAN-R1)

The independent scope review (Q1, commit `5ba43e1`) and the R1 remediation
run (`analysis/r1-reproduction-run.txt`, this branch) both re-executed this
harness against the committed corpus. Structural results reproduce exactly
(all 64 node identities, all 99 edge identities, identical relationship
counts, `FindImplementations` = 0, `PublicationConsistent=false`). The
`IndexIdentity` and `CanonicalFactsSha256` values are
checkout-environment-dependent (file-node `content-hash` properties hash
working-tree bytes; this environment checks out CRLF via `core.autocrlf=true`
while the original preserved LF). The committed content is byte-identical —
`git hash-object` of the R1 working file equals the committed blob hash.
Digest comparisons are therefore valid only within byte-identical checkouts;
graph-identity comparisons are valid across checkouts. Full analysis:
[recovery-and-provenance.md](recovery-and-provenance.md) §6.
