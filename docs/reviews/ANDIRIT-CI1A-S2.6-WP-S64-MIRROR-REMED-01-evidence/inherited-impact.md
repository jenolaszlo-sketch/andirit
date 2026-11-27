# Inherited acceptance impact

## Changed production surface

- `src/Andirit/InMemoryCodeGraphStore.cs`: one internal accessor returning the
  already-materialized node and edge dictionaries under the existing lock.
- `src/Andirit.LatticeDb/NativeGraphMirror.cs`: canonical-fact rebuild, owned-row
  deletion, orphan sweep, disabled drop.
- `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`: mirror lifecycle wiring and
  manifest validation.

No public API signature, persisted manifest shape, label, property name,
command-log record, occurrence record, publication record or query result type
changed.

## S6.1 cancellation

The mirror mutation remains inside the existing completion transaction. No new
cancellation checkpoint or swallowed token was added. The full
`NativeCancellationPathTests` suite passes. `CancelledCompletion_…` confirms a
cancelled completion leaves the accepted mirror intact.

## S6.2 durability and recovery

No durability or quarantine control flow changed. Mirror mutation occurs before
the existing commit hook and before the unknown-outcome classification, so
rollback and quarantine semantics are unchanged. The full
`NativeDurabilityTests` and `New01RecoveryProjectionTests` suites pass.

## S6.3 publication correctness

`NativePublicationCorrectnessTests` P-01…P-16 and the S6.3 named-case receipt
verifier pass locally in the coverage job. The D-07 worker and multi-process
cases are unaffected because the mirror is written inside the same native
transaction and the file lock still serializes writers. The P-03 erratum remains
binding; no current-query optimization was attempted.

## Coverage thresholds

| Job | Required | Observed |
| --- | --- | --- |
| Core | 55% line / branch | 82.36% line / 70.89% branch |
| CSharp | 75% | 95.15% / 88% |
| TreeSitter | 60% | 89.85% / 75.45% |
| LatticeDb | 70% | 93.32% / 79.13% |

## Retained qualifications

- DF-1 remains open.
- DF-2, DF-3, applicable I-05…I-07 and DF-8 retain the S6.3 disposition.
- DF-5 and applicable M9.5/M12.2 retain the S6.1/S6.2 cancellation disposition.
- DF-4 retains tested native contention and supported recovery boundaries.
- DF-6 belongs to S6.5; DF-7 and WP-S24-3 remain deferred.
- macOS remains bounded candidate execution, not platform certification.
