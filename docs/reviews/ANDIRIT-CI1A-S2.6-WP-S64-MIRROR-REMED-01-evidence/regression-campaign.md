# Regression campaign

All cases use the production `LatticeCodeGraphStore` and the real native
database. Physical inventories are read from `HetuGraphNode`, mirror edges and
`HetuMirror` rows after all store handles are closed. Results below are the local
Windows x64 candidate run; hosted reruns are in `hosted-ci.md`.

| # | Required scenario | Named test | Result |
| --- | --- | --- | --- |
| 1 | Original real linked-file fixture | `NativeGraphMirrorLinkedFixtureTests.LinkedFixture_HasOnePhysicalNodePerLogicalIdentity_AcrossLifecycle` | Pass |
| 2 | Shared logical node identities across contributors | `SharedLogicalNodeIdentity_AcrossContributors_HasOnePhysicalNode` | Pass |
| 3 | Shared logical edge identities across contributors | `SharedLogicalEdgeIdentity_AcrossContributors_HasOnePhysicalEdge` | Pass |
| 4 | Established conflicting-contributor semantics | `ConflictingContributors_AreRejected_AndNeverReachTheMirror` | Pass |
| 5 | Initial and repeated publication | `RepeatedEnabledReopen_KeepsPhysicalCardinalityStable` | Pass |
| 6 | Source-unit replacement and deletion | `SourceUnitReplacementAndDeletion_RemoveOldPhysicalFacts` | Pass |
| 7 | Repeated enabled reopen | `RepeatedEnabledReopen_KeepsPhysicalCardinalityStable` (four cycles) | Pass |
| 8 | Enabled → disabled → enabled transitions | `DisabledPublication_RemovesPhysicalMirrorObjects_AndReEnableRebuilds` | Pass |
| 9 | Repeated disabled publications | `RepeatedDisabledPublications_LeaveNoMirrorState` | Pass |
| 10 | Failed publication and transactional rollback | `FailedEnabledPublication_RollsBackWithoutMirrorDrift` | Pass |
| 11 | Cancellation | `CancelledCompletion_LeavesAcceptedMirrorIntact` | Pass |
| 12 | Recovery of previously affected stores | `Recovery_RemovesUntrackedDuplicateNodesAndEdges`; `Recovery_RemovesOrphansLeftByManifestOnlyDeletion`; `DisabledPublication_CleansLegacyOrphansWithoutManifest`; affected-store receipts | Pass |
| 13 | Recovery failure and subsequent reopen | `RecoveryFailure_RollsBackCleanup_AndSubsequentReopenSucceeds` | Pass |
| 14 | Multi-repository isolation | `MultipleRepositories_WithSharedLogicalIdentity_StayIsolated` | Pass |
| 15 | With and without occurrence publication | `OccurrencePublication_DoesNotChangeGraphMirrorCardinality`; integration fixture; recovery receipts for both lanes | Pass |
| 16 | Public traversal and graph query parity | Linked fixture native/projection equality; recovery validator; existing `NativeTraversalMatchesProjection` | Pass |
| 17 | Direct physical node and edge inventory reconciliation | `AssertGlobalCensus` in every test; `AssertCorrespondence` in the linked fixture; recovery validator | Pass |
| — | Malformed and duplicate-valued manifests | `Recovery_RebuildsMalformedManifest_FromAuthoritativeFacts`; `Recovery_RebuildsManifestWithDuplicateNativeIdentities`; `MalformedManifest_DoesNotAffectDisabledAuthoritativeOpen` | Pass |
| — | Negative controls | `InventoryOracle_DetectsInjectedDuplicateAndOrphan`; `InventoryOracle_DetectsManifestOnlyDeletionResidue`; baseline-worktree failures | Pass |
| — | Manifest referencing unrelated node | `ManifestReferencingUnrelatedNode_DoesNotDeleteIt` | Pass |
| — | Manifest referencing authoritative command-log record | `ManifestReferencingAuthoritativeCommandLogRecord_DoesNotDeleteIt` | Pass |
| — | Manifest referencing unrelated edge | `ManifestReferencingUnrelatedEdge_DoesNotDeleteIt` | Pass |
| — | Unrelated edge incident to mirror node | `UnrelatedEdgeIncidentToMirrorNode_IsRefusedAndPreserved` | Pass |
| — | Cross-repository native reference | `CrossRepositoryNativeReference_DoesNotDeleteOtherRepository` | Pass |
| — | Multiple malformed/incomplete manifests | `MalformedOrIncompleteManifests_RebuildWithoutAuthoritativeLoss` | Pass |
| — | Inconsistent retained-edge endpoints | `InconsistentManifestEdgeWithoutEndpointNodes_DoesNotLeaveDanglingEdge` | Pass |
| — | Sentinel identity, properties and relationships | `UnrelatedSentinelObjects_KeepIdentityPropertiesAndRelationships` | Pass |

## Totals

- New tests: 28 LatticeDb + 1 CSharp.
- LatticeDb project: 159 passed, 0 failed.
- CSharp project: 373 passed, 0 failed.
- Core: 345 passed. TreeSitter: 157 passed.
- Full solution: 1034 passed, 0 failed.
- Formatting: `dotnet format Andirit.slnx --verify-no-changes` clean.

## Reconciliation with the preceding accepted inventory

Published main `99b04c0…` Windows CI run `45773556513` receipts:

| Project | Accepted total |
| --- | ---: |
| core | 345 |
| CSharp | 372 |
| LatticeDb | 131 |
| TreeSitter | 157 |
| **Total** | **1005** |

Candidate:

| Project | Candidate total | Delta |
| --- | ---: | ---: |
| core | 345 | 0 |
| CSharp | 373 | +1 (linked fixture) |
| LatticeDb | 159 | +28 (20 remediation + 8 ownership) |
| TreeSitter | 157 | 0 |
| **Total** | **1034** | **+29** |

No existing test was removed or skipped. Reconciliation of the earlier producer
summary:

- At the first remediation commit `579fd6a`, LatticeDb was 150 and CSharp 373,
  i.e. **19 remediation tests + 1 linked-fixture test = 20 new tests** and
  **1,025 total**. The earlier report said "21 added tests" and "1,024 total";
  both were arithmetic slips (19 + 1 = 20; 345 + 373 + 150 + 157 = 1,025).
- The 19-test baseline negative control corresponded to the 19 remediation tests
  that existed at that point.
- After the mandated ownership campaign, the candidate adds 8 ownership tests
  and one additional remediation test (legacy orphan cleanup): **+9**.
- Final candidate: **29 new tests** (28 LatticeDb + 1 CSharp) and **1,034
  total**. The final baseline control covers all 28 LatticeDb tests.

## Baseline negative control

The same two test files copied unmodified onto the entry seal produce:

- 10 of 19 LatticeDb remediation tests failing, including the shared-node,
  shared-edge, repeated-reopen, multi-repository, disabled-transition and
  recovery cases;
- the linked-fixture test failing with `Expected: 20 / Actual: 21`.

Exact text is in `baseline-reproduction/negative-controls.txt`.
