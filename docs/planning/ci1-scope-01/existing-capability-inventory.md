# Existing capability inventory

All observations bind to cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2.
Resolved means resolved by this provider in its constructed analysis context.

## Authority inspected

Product: ROADMAP.md, README.md, BENCHMARKS.md,
[agent roadmap](../../agent-code-intelligence-roadmap.md), and
[EC6D acceptance](../../decisions/ec6d-owner-02-acceptance.md).
Extraction: CSharpCodeGraphPlugin.cs and its GraphBuilder, Symbols, Compilation,
Session and PropertyFactory files; CSharpProjectDiscovery.
Contracts/runtime: CodeGraphFacts, CodeGraphKinds, CodeGraphBatch, CodeEvidence,
CodeLocation, CodeGraphQueryService, CodeGraphPublicationQuery,
CodeGraphTraversalPresets, CodeQueryOperations, ICodeGraphStore,
CodeGraphStoreModels, InMemoryCodeGraphStore, CodeIndexingLifecycle,
CodePublicationSnapshot, LatticeCodeGraphStore and DurableCommandLog.
Tests: CSharpCodeGraphPluginTests, CodeGraphQueryServiceTests,
PublicationSnapshotTests, CodeGraphStoreConformanceSuite,
InMemoryCodeGraphStoreTests, LatticeCodeGraphStoreTests, NativeGraphMirrorTests.

## Symbol identity

[Symbols](../../../src/Andirit.CSharp/CSharpCodeGraphPlugin.Symbols.cs):
CanonicalSymbolKey uses Roslyn documentation-comment ID when available.
Parameters without one use owner key/ordinal/name; other fallback is symbol kind
plus FullyQualifiedFormat display. ScopedSymbolKey prefixes project path.
Symbol ID is csharp:SHA256(scoped key); node ID csharp:symbol: plus the same hash.
Repository identity scopes stored facts outside this hash.

[GraphBuilder](../../../src/Andirit.CSharp/CSharpCodeGraphPlugin.GraphBuilder.cs):
declaration ID hashes scoped key, path and one-based start/end coordinates.
A declaration spans declaration syntax, not just its identifier.
Moving a declaration changes declaration identity; moving a project can change
logical identities. Multiple physical declarations can share a logical node.

| Distinction | Preserved today | Limit |
| --- | --- | --- |
| Method overloads | Documentation IDs carry signatures | A name search must retain ambiguity |
| Generic methods | OriginalDefinition; reduced extensions use ReducedFrom | No occurrence-specific type arguments; combined reduced/generic forms need qualification |
| Partial types | One logical node, multiple declarations | Properties must agree; partial-method unification not qualified |
| Explicit interface implementation | Roslyn member identity/signature | No method-to-interface-member relationship or dispatch proof |
| Constructors | Explicit syntax callable identity | Synthesized constructors lack declaration nodes; calls may count external |
| Extension methods | ReducedFrom resolves defining method | No dedicated usage classification |
| Local functions | Indexed; nearest local owner used | Fallback display key not proven collision-free for same names in different owners |
| Properties/indexers/fields | Separate nodes/declarations and supported parameters | No accessor callable nodes or read/write labels |
| Events | GetNodeKind excludes events; field-event syntax is outside variable filter | No reliable event target nodes |
| Cross-project | Project-scoped keys, dependency compilations and global canonical map | Global lookup omits assembly identity; repeated key across projects becomes ambiguous |
| External | Roslyn may bind a target | No external target node/edge retained; aggregate counters only |
| Ambiguous/unavailable | No guessed confirmed edge | Candidate-only Roslyn binding can fall through without coverage counters |

TryResolveTarget checks a current-project node first, then the global canonical
map; global ambiguity refuses emission. Dependencies are processed first.
Normalization intentionally maps constructed generic targets to definitions.
No continuity across rename/signature changes is promised.

## Relationships

Sorted edge dictionary plus TryAdd retains one edge per hash. Ordinary calls/
references hash kind/source/target/null discriminator, excluding location.
Each edge has ONE evidence location, not a list of witnesses.

| Relation | Extraction rule | Representation and evidence |
| --- | --- | --- |
| calls | InvocationExpression, explicit/implicit new, this/base initializer; bound method plus indexed enclosing callable/property | Aggregate source-target; Resolved; first whole-expression span; no runtime dispatch expansion |
| references | SimpleName/PredefinedType outside using; aliases unwrapped; attribute constructor maps to attribute type | Aggregate source-target; Resolved; first syntax span. Callee name overlaps calls; receiver type can be another target |
| implements | Class/struct written base-list interface | Resolved type-level structural relationship at base span; no member implementation map |
| inherits | Written class base or interface extension | Resolved, direct only; no transitive closure or method override edges |
| contains | Project -> file, existing parent symbol -> member | Structural aggregate ownership |
| declares | File -> symbol with declaration-ID discriminator | Structural declaration-aware physical evidence |
| depends-on | Available explicit project refs; package syntax entries | Project/default Resolved at project 1:1; package Structural with verbatim version/condition and package-name discriminator; no restore closure |
| exercised-by | Existing calls from allowlisted test-method node; target -> test | Derived aggregate with retained call location; no inferred indirect test coverage |

Calls inside lambdas attribute to an enclosing indexed owner; lambdas are not
separate callable nodes. Field/top-level invocation ownership can be absent.
Reference owner selection additionally handles fields/types and file fallback.
Returns/accepts are deliberately NotProduced. Missing edges do not prove no use.

Origins identify repository/plugin/version/run/project unit. C# origins omit
SourcePath/SourceHash; source hashes live in publication manifests and file nodes.
An evidence path is not a source hash. Shared facts retain all unit contributors.

## Query surfaces

[Service](../../../src/Andirit/CodeGraphQueryService.cs),
[presets](../../../src/Andirit/CodeGraphTraversalPresets.cs),
[bound view](../../../src/Andirit/CodeGraphPublicationQuery.cs).

| Query | Return today |
| --- | --- |
| FindSymbolsByQualifiedNameAsync | Exact qualified-name candidates sorted by node ID, ambiguity and SingleOrDefault; no declaration join |
| FindReferencesAsync | Incoming references traversal, including seed among Nodes |
| FindCallersAsync / FindCalleesAsync | Incoming/outgoing calls; nodes and aggregate retained edges |
| GetImpactSetAsync | Incoming references/calls/implements/inherits/depends-on reachability; no change kind or required-edit claim |
| GetImpactSetsAsync | Up to 32 distinct seeds, per-seed traversals with shared publication envelope and independent bounds |
| GetAffectedTestsAsync | Marked test nodes selected from GetImpactSets traversal, with per-seed lists/truncation |

Affected-tests XML mentions calls/references, but implementation uses the broader
ImpactSet preset. It does not traverse exercised-by to prove test completeness
and does not return a minimal proof path per test entry.

WithProvenance adds exact publication, applied descriptor and contributor records
for node/declaration/edge facts. CodeQueryOperations uses low-level traversal
for caller/callee operations. Bound-view Require rejects missing/different
publication; it is fail-on-change, not historical retention.

A current Find Usages workflow needs selection, declarations, callers, references,
then joins to owners/provenance/source hashes and source inspection to recover
lost occurrences. Further impact queries cannot recreate discarded locations.
See [consumer scenarios](consumer-scenarios.md).

## Analysis context and coverage

[Session](../../../src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs) builds
one lightweight compilation per project. No MSBuild evaluation, NuGet restore,
source generators, or normal SDK-generated global-using trees.
ImplicitUsings is recorded but not materialized. Conditions/imports/custom
targets/configurations are not evaluated; multiple TFMs are not fully modeled.
References use host runtime TPA, not targeting packs/package reference assets.
Linked compile inputs and solution membership are lightweight; path equality
is platform-dependent. Loose sources use @loose/csharp.

ignoreAccessibility:true is used: binding is not accessibility/build validation.
Inactive conditional branches are not ordinary syntax occurrences in this
configuration. Compiler warning codes are capped; no per-site unknown records.

Coverage counters include candidates, emitted attempts, internal/cross-project/
external/ambiguous/unsupported/unresolved counts per run and unit.
CountEmitted increments even when TryAdd rejects a duplicate. Thus EdgesEmitted
is not a unique edge count. Produced can coexist with ExternalTargets>0; Partial
is determined by UnresolvedTargets. info.Symbol=null with CandidateSymbols>0
can bypass several paths. Coverage cannot establish target-specific completeness,
or attribute unresolved sites to the selected method.

## Storage and publication

[Store contract](../../../src/Andirit/ICodeGraphStore.cs) stages unit replacements/
deletions, atomically publishes graph and source state, and retains successful
state on failure/cancellation. Optimistic ordering rejects stale planned runs.
C# re-extracts semantic project units when inputs change; obsolete units are
removed. Plugin-version change invalidates unchanged sources.

[Conformance](../../../src/Andirit.Testing/CodeGraphStoreConformanceSuite.cs)
covers contributor sharing/deletion, atomic failure/cancellation, deterministic
traversal, provenance, terminal-run rejection and concurrent publication conflict.
In-memory materialization maintains adjacency indexes. Traversal BFS sorts
kind/source/target/ID. Declarations sort path/position/ID.

Defaults: depth 1, 100 nodes, 250 edges; nonnegative depth, positive nodes/edges,
no hard upper ceilings in CodeGraphQueryOptions. Evidence filter applies to
EVERY traversed edge; Resolved-only cannot cross Structural containment.
Truncation flags include depth/nodes/edges, examined counts and omitted frontier.
These are not exact unseen-usage totals. Output caps do not bound adjacency
enumeration/sorting or serialized bytes; cancellation is not a full work quota.

LatticeDB schema 2 replays a durable command log into the in-memory projection.
Default queries share it; experimental native unfiltered TraverseAsync has a
separate mirror, while evidence-filtered traversal falls back to projection.
Durable tests cover reopen, historical manifests/latest state, staged runs,
corruption refusal, interrupted transactions and failed mutation retention.
Historical Hetu labels/plugin IDs are intentional compatibility identities.

CodePublicationSnapshot schema 2 exports/imports integrity-checked units with
unit/node/edge/byte bounds; schema mismatches fail without automatic migration.
Latest-only graph retention differs from explicitly exported historical state.
Freshness checks compare live source to manifests; a publication ID alone does
not prove current disk freshness or coherent snapshot. Close/reopen is not
crash/power-loss qualification. No global retained graph/replay cap is established.

## Observed verification and limitations

47 C# tests, 20 selected query/snapshot/in-memory tests and six selected durable
tests passed; logs in evidence/. No full solution rerun or new-feature test claim.

Successful real subset: two projects, 47 files, 382,944 decoded UTF-8 bytes,
1,894 unresolved call attempts and 4,474 unresolved reference attempts.
Wider three-project extraction throws InvalidCastException at AddSymbol line
314 while formatting inconsistent-fact diagnostics. The underlying inconsistent
symbol is not established; no remediation is silently included.
Zero reported global ambiguity is not proof absence of C# ambiguity.
