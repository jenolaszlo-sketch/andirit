# Transactional consistency

## Single native transaction

Every mutation path that changes mirror state performs all of the following in
one `LatticeTransaction`:

- authoritative command-log, unit and index-state writes;
- occurrence authority writes;
- deletion of the previous mirror-owned rows;
- orphan sweep;
- creation of the canonical physical nodes and edges;
- manifest upsert (enabled) or manifest deletion (disabled).

Enabled completion and disabled completion therefore cannot expose a manifest
that disagrees with physical rows, or physical rows with no manifest, after a
successful commit.

## Enable path

`Persist("complete")` applies the authoritative publication first, then rebuilds
from `_inner.GetMaterializedFacts`. If any write, sweep, create or manifest
upsert fails, the transaction rolls back and the command log is not advanced.

## Disable path

`Persist("complete")` with `UseNativeTraversal=false` reads the persisted
manifest, deletes its rows, deletes the manifest row, and sweeps all remaining
unreferenced mirror rows before commit. A failure leaves the previous manifest
and rows intact.

## Evidence

| Case | Test | Observation |
| --- | --- | --- |
| Failed enabled publication | `FailedEnabledPublication_RollsBackWithoutMirrorDrift` | Injected `before-commit` fault; prior node and mirror census unchanged; proposed node absent |
| Failed disabled cleanup | `RecoveryFailure_RollsBackCleanup_AndSubsequentReopenSucceeds` | Fault during disable; prior manifest/rows preserved; later reopen succeeds |
| Cancellation before persistence | `CancelledCompletion_LeavesAcceptedMirrorIntact` | Cancelled token; no publication or mirror change |
| Staged publication invisible | Existing `StagedFactsStayOutOfNativeTraversal` | Staged node absent before completion, present after |
| Unknown commit outcome | Existing S6.2/S6.3 quarantine suites | Unchanged; mirror mutations are inside the same native commit |
| Occurrence preservation | `OccurrencePublication_DoesNotChangeGraphMirrorCardinality` | Occurrence fact count preserved; mirror graph cardinality canonical |
| Recovery rollback | `recover-integrated.json` | Occurrence fact survives recovery |

## Cancellation and quarantine impact

No cancellation or quarantine branch was changed. Mirror work remains inside the
same atomic window as before; the sweep lengthens that window but does not alter
which outcomes are observable. The existing S6.1 cancellation and S6.2/S6.3
durability suites pass unchanged.
