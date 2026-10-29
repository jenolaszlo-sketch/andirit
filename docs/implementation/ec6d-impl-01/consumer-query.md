# GetInterfaceImpactV1 consumer contract

`CodeGraphPublicationQuery.GetInterfaceImpactV1Async` and
`CodeGraphQueryService.GetInterfaceImpactV1Async(publication, ...)` expose the
same fail-on-change behavior. The caller supplies a selected physical
TypeScript interface node ID, immutable bounds and cancellation token.
Unknown/non-interface targets, missing provider facts and unsupported/mixed
TypeScript profile units are rejected. Merged interface nodes remain queryable
with null physical record and null unique physical span.

```csharp
var service = new CodeGraphQueryService(store);
var view = await service.OpenLatestPublicationAsync(repositoryId, cancellationToken)
    ?? throw new InvalidOperationException("No publication");
var impact = await view.GetInterfaceImpactV1Async(interfaceNodeId,
    new InterfaceImpactOptionsV1(), cancellationToken);
Console.WriteLine(impact.CoverageStatement);
var json = impact.ToJson();
var restored = InterfaceImpactResultV1.FromJson(json);
// When consistent-snapshot authority is required:
var authoritativeSnapshotResult = restored.RequireConsistentSnapshot();
```

The response keeps full repository/run/index/snapshot identity, derived
qualification, target metadata, confirmed entities with complete ordered
witnesses, possible modules with every import occurrence, explicit unknowns,
structured coverage, fixed coverage text and truncation together.

The exact confirmed primary set is AbstractRenderer, AliasRenderer,
HtmlRenderer and SvgRenderer. The five possible modules are Preview and those
four implementing modules. BarrelPreview and MissingImport are occurrence
unknowns. A confirmed class and its possible-consumer file are distinct
relationships and may both appear. Possible consumers follow the accepted
module-level predicate; an imported name need not equal the selected interface.

## Work limits

| Bound | Default |
| --- | ---: |
| maxCandidateScans | 100000 |
| maxHeritageScans | 10000 |
| maxResolverJoins | 10000 |
| maxAttributionLookups | 10000 |
| maxWitnessSteps | 100000 |
| maxConfirmed | 1000 |
| maxOccurrences | 10000 |
| maxUnknowns | 10000 |

All limits must be positive. Input options are immutable. Execution work and
output caps are separate; fact contributions are charged before identity
deduplication. Candidate, context, resolver, attribution and witness work
remain independently visible. The current reader materializes published units;
these budgets bound query processing, not store transport/memory or a claimed
performance SLA. No numeric performance target is inferred.

A reached limit marks coverage partial and counts as lower bounds, preserves
only complete witnesses, names the first limiting stage and reports applied
limits, charged work and omitted frontier. On work exhaustion the omitted
frontier count is a conservative lower bound; it is never an exact-completeness
claim. Equal bounds and reversed enumeration produce deterministic results.

Results sort by the accepted nested class/witness/occurrence/unknown orders.
Entities deduplicate by class+physical target, files by file identity, unknowns
by subject+reason and occurrences by stored occurrence ID. Distinct witness
paths survive as separate witnesses.

## Publication guarantees

False consistency with any snapshot is qualified. True with an identified,
nonblank snapshot is consistent. True with null/blank snapshot is qualified.
Qualification appears inside the publication block, coverage, fixed statement,
JSON and checked authority path. Qualified results are usable for navigation
and triage; `RequireConsistentSnapshot` refuses them for snapshot authority.

The service checks full publication identity before reads, after inventory
reads and after computation. Same-content/new-run movement throws the existing
publication-changed exception; no restart occurs. Cancellation, store errors
and integrity failures throw and return no classified output. Ordinary missing
exports/declarations remain explicit uncertainty. Results read no source provider
and make no freshness promise about live edits after publication.

Coverage always discloses the ordinary-type-use, calls, implicit structural,
transitive, unsupported binding, other-language/.astro and re-export frontiers.
Completeness is `truncated` or `incomplete-frontier` while these categories
remain outside evaluation; zero confirmed never means zero repository impact.
