# Findings and remediation matrix

| Invariant | Implementation | Regression evidence |
| --- | --- | --- |
| One physical node per canonical logical node identity within a repository | `NativeGraphMirror.Rebuild` consumes `GetMaterializedFacts` and creates one node per identity | `SharedLogicalNodeIdentity_AcrossContributors_HasOnePhysicalNode`; `LinkedFixture_HasOnePhysicalNodePerLogicalIdentity_AcrossLifecycle` |
| Physical edge representation consistent with canonical legacy graph semantics | One edge per materialized visible edge, same source/target/kind, payload preserved | `SharedLogicalEdgeIdentity_AcrossContributors_HasOnePhysicalEdge`; linked fixture edge/manifest correspondence |
| Correct contributor union and aggregation | Materialized projection performs equivalence merge and contributor ordering | `ConflictingContributors_AreRejected_AndNeverReachTheMirror`; `OccurrencePublication_DoesNotChangeGraphMirrorCardinality` |
| No arbitrary first/last-writer deduplication | Mirror never deduplicates; the authoritative projection rejects conflicts | Conflict test; baseline negative control |
| Complete manifest ↔ owned-object correspondence | Rebuild writes a full manifest in the same transaction; oracle checks both directions | `AssertGlobalCensus` in every remediation test; `AssertCorrespondence` in the linked fixture; recovery validator |
| No duplicate or orphaned mirror objects in scope | `DeleteOwned` + `SweepOrphans` on rebuild and disable | `RepeatedEnabledReopen_KeepsPhysicalCardinalityStable`; `DisabledPublication_RemovesPhysicalMirrorObjects_AndReEnableRebuilds` |
| Stable physical cardinality across repeated rebuilds and reopen | Full delete/recreate from canonical facts; sweep removes residue | Repeated reopen test (four cycles); second recovery receipt |
| Correct enable/disable transitions | Disabled completion deletes physical rows, manifest, then sweeps | Transition test; repeated disabled publications; legacy orphan cleanup |
| Repository isolation | Sweep retains every native id referenced by another manifest | `MultipleRepositories_WithSharedLogicalIdentity_StayIsolated` |
| Transactional consistency | Publication, mirror objects and manifest share one native transaction | `FailedEnabledPublication_RollsBackWithoutMirrorDrift`; recovery failure test |
| No staged/cancelled/failed publication exposure | Mirror work only on completion; rollback/quarantine unchanged | Failure test; `CancelledCompletion_LeavesAcceptedMirrorIntact`; `StagedFactsStayOutOfNativeTraversal` |
| Unchanged public graph query/traversal semantics | No public API change; materialized facts are the projection's own facts | Existing parity suite; linked fixture native/projection equality |
| Occurrence invisibility in legacy graph surfaces | Occurrence labels untouched; mirror never contains occurrence rows | Occurrence parity test; recovery occurrence preservation |
| Safe recovery of previously affected stores | Detect/refuse malformed manifests; sweep only unreferenced protocol rows | Four recovery tests; affected-store receipts |
| Manifest references are ownership evidence only for protocol-marked objects | `ReadOwnedManifest` requires every node/edge to be a mirror object and every edge to connect manifest nodes | `NativeGraphMirrorOwnershipTests` cases 1, 2, 3, 7 |
| No deletion of unrelated or authoritative objects | Untrusted manifests own nothing; cleanup checks markers before deletion | Cases 1–3, 6, 9 with content-and-identity comparison |
| Ambiguous relationships are refused, not silently removed | Non-mirror incident edge throws `InvalidDataException`; transaction rolls back | Case 4 and case 8 |
| Retained edges protect endpoint nodes | Sweep protects retained native ids plus endpoints of retained edges | Case 5 and case 7 |

## Findings

| ID | Status | Notes |
| --- | --- | --- |
| F-S64E-01 | Remediated in producer scope | Physical multiplicity now follows canonical identity; residue removed on rebuild |
| F-S64E-02 | Remediated in producer scope | Disable removes physical rows; missing/malformed manifests reconstruct safely |
| F-S64E-03 | Unchanged | DF-1 evidence gap remains outside this commission |
| F-S64E-04 | Unchanged | Corpus/oracle preparation remains with S6.4 |
| F-S64E-05 | Partially discharged here | Local Windows campaign complete; hosted Linux record in `hosted-ci.md`; independent replay still required |
| F-S64R-01 | Remediated in producer scope | A valid-looking manifest could name an authoritative or unrelated node; now untrusted and never deleted |
| F-S64R-02 | Remediated in producer scope | Unrelated incident edges were deleted; now explicit ownership refusal before any mutation |
