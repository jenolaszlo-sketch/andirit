# Occurrence representation analysis

## Source expectation and public-API reproduction

[Repeated.cs](fixtures/Repeated.cs) has three written invocations in
Worker.Execute. A separate Roslyn walk confirms all bind to
M:Logger.Write(System.String), not three same-name guesses.

| Expression | One-based, end-exclusive span |
| --- | --- |
| Logger.Write("first") | src/Repeated.cs 5:9-5:30 |
| Logger.Write("second") | src/Repeated.cs 6:9-6:31 |
| Logger.Write("third") | src/Repeated.cs 7:9-7:30 |

The observation harness uses the public plugin session, validated ingestion,
atomic completion and bound queries. [Raw result](evidence/fixture-baseline.json)
records source hashes, target/owner IDs, evidence and coverage.

Exactly ONE calls edge is streamed and retained for this pair:

~~~text
csharp:ebaa307587a57be62f77da6aab5de0297b4e837e29939067c47bf4ceb81363f8
~~~

Its only evidence is 5:9-5:30. FindCallersAsync returns this one edge, seed and
caller nodes, Truncated=false. Extraction loss is not traversal truncation.

FindReferencesAsync returns ONE overlapping method-name reference:

~~~text
csharp:e6fca5b2ecfd9f92e04efd68b7bd95ece09eb0f60e51912c9f3b7d1fee145909
~~~

It retains 5:16-5:21. Unioning calls and references gives two relationships,
not two usage sites.

Calls coverage: Candidates=6, EdgesEmitted=3, InternalTargets=3, ExternalTargets=3.
The three other attempts are implicit new Target() expressions in the second
fixture; synthesized constructors have no declaration nodes. Only ONE unique
calls edge reaches the sink. Counts cannot recreate missing spans or identify
targets per attempt. EdgesEmitted is not usable as occurrence cardinality.

[References.cs](fixtures/References.cs) writes three Target type references at
6:9-6:15, 7:9-7:15 and 8:9-8:15 in Consumer.Run. Exactly one retained edge:

~~~text
csharp:dcc054406a0b622550d6430321f58411daa12e35499bb79f47c70eb29be3ead4
~~~

It retains only 6:9-6:15. Whole-fixture reference coverage reports nine emission
attempts but three retained reference edges: Logger type, Write method and
Target type. No read/write classification exists.

## Cause and compatibility implication

GraphBuilder.AddEdge hashes kind, source ID, target ID and discriminator,
separated by newlines. Ordinary calls/references supply no discriminator.
Location is excluded. _edges.TryAdd preserves the first edge. Extraction walks
all syntax but CountEmitted increments regardless of insertion success.
WriteAsync streams the deduplicated dictionary. Storage/query layers cannot
recover discarded evidence without source rebinding.

An existing repeated-reference test intentionally pins first evidence.
The new feature must not silently reinterpret the aggregate edge contract.

## Viable representations

Let U be distinct supported sites and P distinct source/target/kind pairs.

| Representation | Semantics/compatibility | Cost/risk |
| --- | --- | --- |
| Aggregate edges plus query-time source rebinding | Correct if exact published sources are available; existing queries unchanged | O(P) retained; repeated parse/bind/read cost; cannot answer fact-only offline queries |
| Occurrence-aware calls/reference IDs | Retains sites; silent change alters legacy edge IDs, cardinalities, budgets and frontier | O(U) edges; adjacency reusable; overlap classification still required |
| New occurrence edge kind plus legacy aggregates | Versioned extraction preserves old traversals; properties must carry typed site semantics | O(P+U); avoid including new kind in legacy presets; validator/codec complexity |
| Separate occurrence facts plus legacy aggregates | Explicit source evidence, target lookup and ownership; no graph node per site | O(P+U); new validated payload/index/snapshot/durable handling |
| Aggregate edge with typed witness collection | Can preserve U if never silently capped | O(P+U) bytes; large mutable payload, conflicting contributors/paging; effectively a side table at scale |

Recommend separate occurrence facts, pending approval. Its value is independent
physical evidence and clean compatibility, not just ease of extraction. The
strongest simpler alternative is a new occurrence edge kind; cost comparison
must precede durable architecture authorization. Both leave legacy IDs/semantics
unchanged. Merely putting location into existing calls IDs is rejected without
an explicitly versioned semantic change.

## Candidate fact and identity

Provider-neutral UsageOccurrenceV1 value, not a Roslyn object or graph node:
project unit, source path/hash, expression span, selected normalized target,
owner node or file context, category, static binding route, semantic-contract
version, contributor provenance. Source revision is a publication property.

Proposed ID: SHA-256 of a domain-separated length-prefixed UTF-8 tuple:
"usage1", repository ID, plugin ID, occurrence semantics version, unit ID,
normalized relative path, source content hash, start/end coordinates,
normalized target ID and canonical category. No run ID, wall clock or enumeration
ordinal. Coordinates use existing one-based UTF-16 lines/columns, end-exclusive;
do not normalize line endings before hashing.

Stable means repeatable for unchanged source/context/semantics, not continuity
across edits. A changed source hash intentionally changes IDs throughout that
file, even when some lines do not move. Identity churn is an explicit tradeoff
for exact evidence. A source/target pair at two locations is always two facts.

Linked files in separate project contexts stay distinct. Optional physical-site
grouping can expose a contexts list but cannot merge different target bindings.
Duplicate extraction/retry tuples dedup; identical ID with different payload
rejects the unit. Multiple proofs of one site/target/category are sorted proofs,
not extra usage counts. Reordered enumeration cannot change identities.

## Classification and deduplication

The primary supported site is the whole InvocationExpression. Suppress only
the callee-name reference to the SAME normalized method within that invocation
as a second method usage. Do not suppress receiver/type references, nested
argument invocations or independent method groups. F(F()) has two distinct
expression spans. Never dedup by line, text, source/target pair or overlap alone.
Method groups outside an invocation remain a deferred category.

## Ownership, lookup and publication

Occurrences belong to the semantic project unit and explicitly name source/hash.
Replace the entire unit atomically alongside existing facts. Deleted/shifted
occurrences disappear only upon successful publication; failed/cancelled runs
retain the old graph/source state. Obsolete-unit deletion removes only that
unit's contributions. Shared contributors survive.

Index by repository/target and deterministic site key. The query must range-read
matching sites and bounded coverage summaries, not enumerate all repository
symbols or reparse source. Quotas count examined records, proofs and duplicates.
Publication checks around composite reads prevent mixed results.

Historical stores lacking occurrences stay readable by old queries. New usage
queries return analysis-unavailable/reindex-required, not complete empty output.
Plugin/context semantic changes require reindex. New readers never fabricate
occurrences from aggregate evidence. A new schema may require old-reader refusal;
historical readability does not imply downgrade writes. Approve codec/snapshot/
migration rules separately and qualify authentic historical fixtures.

## Size/cost and decisions still required

The repeated fixture would add three invocation facts, retaining one calls edge
and legacy references. Positive would add eight facts for one pair.
There is no measured candidate byte multiplier. U/P can be large. Measure
index allocation, target-index construction, incremental replacement, replay,
file size and repeated-query amortization. Fewer graph nodes/API calls is not
a sufficient cost argument.

Owner must decide separate facts versus versioned occurrence edges, exact ID
tuple/edit stability, producer versioning, historical compatibility and hard
limits. This packet implements none of them.

## R1 amendment (CI-1-SCOPE-R1)

**Candidate fact identity is exact, not canonical-name coincidence (F01).** The
candidate fact's "selected normalized target" and the confirmation gate must
prove correspondence between the bound symbol's defining compilation/assembly and
definition and the indexed declaration. A unique canonical documentation ID is a
lookup aid only. Where correspondence is unproven, refuse/qualify; never confirm.

**Source-byte provenance (F03).** The identity tuple's "source content hash" is
the hash of the exact bytes read, with encoding and line-ending policy recorded;
Git blob identity is a separate value. Normalized fixture bytes are never claimed
equivalent to the original source. See `corrections.md` C3.

**Representation preference withdrawn (S01).** The earlier "Recommend separate
occurrence facts" is superseded by
[`../ci1-scope-r1/storage-alternatives.md`](../ci1-scope-r1/storage-alternatives.md);
C and B2 remain qualified alternatives with a minimum comparative experiment.
