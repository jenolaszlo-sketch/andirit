# Query contract proposal (not an approved public API)

Working names FindUsagesV1 and GetChangeImpactV1 are provider-neutral versioned
operations compatible with CodeGraphQueryService and publication-bound views.
No method or production DTO is introduced by this packet.

## Request and validation

FindUsagesV1 request: contractVersion="find-usages/1", repositoryId,
exact publication identity (repository/run/index identity/snapshot qualification),
selector (symbol node+symbol ID OR physical declaration ID), categories
(default ["method-invocation"]), limits, optional requireFreshSources.
A name-only selector is invalid; discovery is a separate bounded operation.
Declaration selection resolves exactly one logical symbol, returning all
relevant declarations within a declared declaration bound.

Reject wrong repository, missing/mismatched selector identities, non-C# or
unsupported target kind, ambiguity, conflicting facts, malformed coordinates/
limits/version, unknown/deferred categories, and inconsistent source evidence.
Use explicit statuses InvalidRequest, SelectionAmbiguous, SelectionUnavailable,
UnsupportedCategory, AnalysisUnavailable, PublicationChanged, FreshnessConflict.
No classified output accompanies publication movement, integrity failure or
selection ambiguity. An unknown historical occurrence producer returns
AnalysisUnavailable/reindex-required, never a successful empty result.

Confirmed selection requires non-error signature identity and unique canonical
target in its project context. Existing qualified-name candidates with error
types are not silently upgraded. Cross-project ambiguity is an explicit
qualification/rejection, not a first-candidate choice.

A normal query does not access the filesystem or network. Freshness is
notChecked unless supplied by an attributable check against the SAME publication.
requireFreshSources needs a host-supplied bounded provider check; absent support
returns FreshnessUnavailable. Source revision can be null; never infer clean Git
state from a mutable directory or publication hash. Source-state identity and
source revision are distinct fields.

## Result envelope

| Section | Required semantics |
| --- | --- |
| request | Normalized request/version/selector/categories/changeKind/limits; digest for reproducibility |
| selection | Exact node/symbol/project identities, bounded declarations, selection qualification |
| publication | Exact receipt, source state/revision if known, snapshot consistency, plugin/provider/analyzer and binding-context versions, freshness and checked-publication |
| usages | Stable evidence handle, target symbol, primary category/facets, lexical source owner/context, path/hash/span, unit, relationship classification, per-proof provenance |
| directCallers | Group by owner/context; returned count vs known total, partial flag; sites never removed by grouping |
| coverage | Unit/category analyzed status, unknown/external/ambiguous/unsupported counters and scope, omitted/not-evaluated categories, context limitations |
| completeness | completeWithinSupportedScope, returned counts, totals if actually known, truncation reasons, work examined, omitted lower bounds or unknown, continuation policy |
| impact | CI-1A: notEvaluated; future operation separates direct/indirect dependents, paths/tests/frontiers |

Per-site proof includes quality=Resolved, producer ID/version/semantic version,
unit contribution(s), source hash, static binding route and normalized target.
The evidence lookup key is (exact publication, occurrence handle); a handle alone
does not authorize a latest-publication fallback.
No raw Roslyn symbols. Contributor sets cannot be reduced to a convenient first
origin. No source excerpts by default; bounded source retrieval is a separate
hash-checked host action. A missing location stays null on a diagnostic/frontier;
a confirmed V1 occurrence without a location is rejected.

Coverage summary is publication data, not a live reconstruction from missing
edges. Units must account for all scanned invocation syntax by disjoint outcome:
confirmed, unresolved, ambiguous, external, unsupported owner/category.
Unit-level unknown counts MUST NOT be labeled selected-target usages.
Unsupported categories remain visible even when not requested.
Candidate binding sites do not become confirmed by display-name resemblance.

completeWithinSupportedScope means the requested supported categories were fully
evaluated for the reported units/context, with no unresolved/ambiguous sites
capable of hiding an eligible target, no relevant skipped unit, and no result/
work/coverage truncation. This is not repository-wide semantic completeness.
A conservative unknown-unit outcome makes the flag false even if current rows
are all proven. All positive sites retain proof irrespective of incomplete
coverage. NoSupportedUsagesFound is distinct from SymbolUnused (not offered).

## Determinism, bounds and serialization

Order: normalized path ordinal, start line/column, end line/column, primary
category, target ID, unit ID, occurrence ID. Units and contributors sort by
stable identities. Physical declaration order is path/position/ID.
Use canonical JSON property order for digest generation, sorted set arrays,
UTF-8, explicit versioned enum strings, no NaN and no locale-sensitive sorting.
Properties with unknown information use explicit null/status, not invented text.

Proposed defaults/hard maxima (owner approval required):

| Dimension | Default | Hard ceiling |
| --- | ---: | ---: |
| Returned occurrences | 100 | 1,000 |
| Returned distinct owner/target nodes | 100 | 1,000 |
| Relevant declarations | 16 | 128 |
| Aggregate/path edges, future impact | 250 | 5,000 |
| Depth: FindUsages / future impact | 0 / 2 | 0 / 3 |
| Proofs per occurrence/path | 1 | 4 |
| Coverage units examined | 512 | 4,096 |
| Fact/proof/adjacency records examined | 10,000 | 100,000 |
| Serialized UTF-8 envelope | 65,536 | 1,048,576 |
| Future paths per dependent / total | 1 / 100 | 4 / 1,000 |

Every examined record, duplicate and proof counts toward processing work.
No unbounded pre-sort or full-repository materialization hidden behind output
limits. Target index range-order must support a deterministic prefix.
Coverage/status/selection metadata have reserved byte budget before rows; if
even the minimum envelope cannot fit, return BudgetTooSmall with no classified
output. Stop before adding a row/proof that would exceed a bound. Lost proofs
are proof truncation, not a false assertion of full evidence. Report applied
limits, examined counts, returned counts and separate truncation reasons.

No continuation token in first V1: continuation=null and resumable=false.
A deterministic limited prefix is reproducible for the same publication/limits.
Consumers may retry with larger approved limits; completeness remains false at
the hard ceiling. Future seek continuation must bind publication, normalized
request, semantic version and last total-order key; never latest-graph paging.
Cancellation returns cancellation, not an apparently complete partial envelope.

## Future GetChangeImpactV1 (design only)

Request additionally requires changeKind: body/signature/rename/delete/
accessibility/interface-contract. Proposed edits are descriptive, not proof.
Resolve direct occurrences first; traverse incoming calls ONLY for the narrow
CI-1B candidate, depth <=3. Other references/heritage are individually enabled
only after category contracts are accepted, never implicitly added by a preset.

Direct verified relationships stay in one section. Indirect possible dependents
carry ordered paths of verified static edges/site handles and algorithm version;
reachability is Derived. Cycles use a visited key, bounded deterministic BFS,
canonical shortest path first and bounded additional paths. Missing occurrence
witnesses remain legacy aggregate evidence, not invented call-site paths.
A truncated path is not a complete proof to its alleged destination.

Candidate tests distinguish direct exercised-by Derived evidence, resolved
test-method calls, and indirect marked-test reachability; no executed-test or
coverage guarantee. Unknown/external frontier records identify unit/source scope
and available evidence, not nonexistent targets. CI-1A returns tests/indirect
impact notEvaluated. Change-kind claims follow scope-proposal's table.
This future contract cannot assert required edits, safe/unaffected or will-break.

## Proposed JSON A: complete bounded supported invocation example

**PROPOSED, not output of a new API.** Mnemonic example IDs stand in for the
future opaque ID/receipt codecs; actual production handles must use the approved
identity contract. Source locations are independently justified by Repeated.cs.
"Complete" applies only to the example's reported invocation context.
A models Repeated.cs alone, not the two-file baseline publication.

~~~json
{
  "status": "Success",
  "request": {
    "contractVersion": "find-usages/1",
    "repositoryId": "example:fixture",
    "selector": {"symbolId": "example:Logger.Write(string)"},
    "categories": ["method-invocation"],
    "limits": {"maxOccurrences": 100, "maxNodes": 100, "maxDeclarations": 16, "maxWork": 10000, "maxBytes": 65536}
  },
  "selection": {
    "nodeId": "example:logger-write-node",
    "symbolId": "example:Logger.Write(string)",
    "projectId": "@loose/csharp",
    "declarations": [{"id": "example:declaration", "path": "src/Repeated.cs", "span": [12, 5, 12, 47]}],
    "qualification": "unique-definition"
  },
  "publication": {
    "receipt": "example:proposed-publication-1",
    "sourceStateId": "example:fixture-state",
    "sourceRevision": null,
    "consistentSnapshot": false,
    "providers": [{"id": "penghou.hetu.csharp", "version": "proposal-only", "analyzer": "Roslyn", "analyzerVersion": "5.9.0", "semantics": "usage1-proposed"}],
    "freshness": {"status": "notChecked", "checkedPublication": null},
    "coverageScope": ["@loose/csharp"]
  },
  "usages": [
    {
      "evidenceHandle": "example:usage:first",
      "targetSymbolId": "example:Logger.Write(string)",
      "category": "method-invocation",
      "facets": ["static"],
      "sourceOwner": {"symbolId": "example:Worker.Execute()", "context": "method-body"},
      "path": "src/Repeated.cs", "span": [5, 9, 5, 30],
      "sourceHash": "sha256:1f41863276dcc45d51ab42cdfd0205f5f337a31d1da5e30e4ee49fbde5c8f886",
      "unitId": "@loose/csharp",
      "classification": "verifiedRelationship",
      "proofs": [{"quality": "Resolved", "route": "static-bound-definition", "provider": "penghou.hetu.csharp", "version": "proposal-only", "semantics": "usage1-proposed", "contribution": "example:unit-contribution-1"}]
    },
    {
      "evidenceHandle": "example:usage:second",
      "targetSymbolId": "example:Logger.Write(string)",
      "category": "method-invocation",
      "facets": ["static"],
      "sourceOwner": {"symbolId": "example:Worker.Execute()", "context": "method-body"},
      "path": "src/Repeated.cs", "span": [6, 9, 6, 31],
      "sourceHash": "sha256:1f41863276dcc45d51ab42cdfd0205f5f337a31d1da5e30e4ee49fbde5c8f886",
      "unitId": "@loose/csharp",
      "classification": "verifiedRelationship",
      "proofs": [{"quality": "Resolved", "route": "static-bound-definition", "provider": "penghou.hetu.csharp", "version": "proposal-only", "semantics": "usage1-proposed", "contribution": "example:unit-contribution-1"}]
    },
    {
      "evidenceHandle": "example:usage:third",
      "targetSymbolId": "example:Logger.Write(string)",
      "category": "method-invocation",
      "facets": ["static"],
      "sourceOwner": {"symbolId": "example:Worker.Execute()", "context": "method-body"},
      "path": "src/Repeated.cs", "span": [7, 9, 7, 30],
      "sourceHash": "sha256:1f41863276dcc45d51ab42cdfd0205f5f337a31d1da5e30e4ee49fbde5c8f886",
      "unitId": "@loose/csharp",
      "classification": "verifiedRelationship",
      "proofs": [{"quality": "Resolved", "route": "static-bound-definition", "provider": "penghou.hetu.csharp", "version": "proposal-only", "semantics": "usage1-proposed", "contribution": "example:unit-contribution-1"}]
    }
  ],
  "directCallers": [{"symbolId": "example:Worker.Execute()", "returnedOccurrences": 3, "totalOccurrences": 3, "partial": false}],
  "impact": {"indirectDependents": "notEvaluated", "candidateTests": "notEvaluated"},
  "coverage": {
    "analyzed": [{"unit": "@loose/csharp", "category": "method-invocation", "status": "analyzed"}],
    "unknowns": [], "external": [], "unsupported": [],
    "notEvaluated": ["method-groups", "constructors", "runtime-dispatch", "reflection", "source-generator-execution", "outside-repository"],
    "scopeQualification": "reported source context only"
  },
  "completeness": {"completeWithinSupportedScope": true, "returnedOccurrences": 3, "knownTotal": 3, "truncated": false, "reasons": [], "workExamined": 3, "omitted": 0, "continuation": null, "resumable": false}
}
~~~

## Proposed JSON B: ambiguous/external coverage and truncation

**PROPOSED** future result for a hypothetical incomplete publication; no invented
source locations are attached to unit-level unknowns. Selected target is unique;
ambiguity concerns other scanned sites. Fields common with A are repeated or
referenced through explicit objects; no ambiguous site is a confirmed usage.

~~~json
{
  "status": "Partial",
  "request": {
    "contractVersion": "find-usages/1", "repositoryId": "example:incomplete",
    "selector": {"symbolId": "example:unique-method"},
    "categories": ["method-invocation"],
    "limits": {"maxOccurrences": 1, "maxNodes": 10, "maxDeclarations": 16, "maxWork": 100, "maxBytes": 8192}
  },
  "selection": {"nodeId": "example:target", "symbolId": "example:unique-method", "projectId": "src/App.csproj", "declarations": [], "qualification": "unique-definition", "declarationsStatus": "notEvaluated-work-budget"},
  "publication": {
    "receipt": "example:proposed-publication-2", "sourceStateId": "example:state-2",
    "sourceRevision": null, "consistentSnapshot": false,
    "providers": [{"id": "penghou.hetu.csharp", "version": "proposal-only", "analyzer": "Roslyn", "analyzerVersion": "5.9.0", "semantics": "usage1-proposed"}],
    "freshness": {"status": "notChecked", "checkedPublication": null},
    "coverageScope": ["src/App.csproj"]
  },
  "usages": [],
  "directCallers": [],
  "impact": {"indirectDependents": "notEvaluated", "candidateTests": "notEvaluated"},
  "coverage": {
    "analyzed": [{"unit": "src/App.csproj", "category": "method-invocation", "status": "partial"}],
    "unknowns": [{"scope": "unit", "unit": "src/App.csproj", "reason": "ambiguous-binding", "count": 2, "targetAssociation": "unknown", "location": null}],
    "external": [{"scope": "unit", "unit": "src/App.csproj", "count": 4, "reason": "bound-outside-index", "targetAssociation": "unknown", "location": null}],
    "unsupported": ["top-level-owner-context"],
    "notEvaluated": ["reflection", "source-generator-execution", "runtime-dispatch", "outside-repository"]
  },
  "completeness": {
    "completeWithinSupportedScope": false, "returnedOccurrences": 0, "knownTotal": null,
    "truncated": true, "reasons": ["maxWork", "declarations-not-evaluated"],
    "workExamined": 100, "omitted": null, "omittedLowerBound": 0,
    "continuation": null, "resumable": false
  }
}
~~~

B cannot claim no usages. An empty partial response is legitimate when work
ends before any target row. Its external/ambiguous counts are example coverage
data, not measurements from the real corpus. Production validation must bound
declarations independently and reserve sufficient identity metadata.
