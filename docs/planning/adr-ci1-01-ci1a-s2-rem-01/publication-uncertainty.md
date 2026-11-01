# Publication Uncertainty and Retry — REM-B05

**Authority:** RECON SQ-02; owner decision D4.1, D4.4, D4.6–D4.7; Q1B F05.
**Production:** `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`.
**Tests:** `OccurrenceRemediationNativeTests.AfterCommitAcknowledgementLoss...`
and existing N6/N8.

---

## 1. Outcome model

| Outcome | Detectable? | Required behavior |
|---|---|---|
| Failure before native write/commit | Yes (exception before commit) | Report failure; restore projection from unchanged command log; prior publication retained |
| Confirmed transaction rejection | Yes (validation or native rejection before commit) | Report rejection; no success receipt; prior publication retained |
| Confirmed durable commit | Yes (commit returns) | Report success only after the write returns; advance command log/projection |
| Commit outcome unknown | **No** | Refuse current-state claims and mutations on this instance; do not claim rollback or success; no blind retry |
| Commit succeeded, acknowledgment lost | Indistinguishable from unknown | Same as unknown; reopen reconstructs durable state |
| Retry after uncertainty | Not automatic | Reopen/corroborate first; then normal baseline rules |

The implementation does not claim crash/power-loss or multi-process recovery.

---

## 2. Mutation ordering

`MutateAsync` now:

1. computes `next = Apply(_commands, command)`;
2. applies the command to the in-memory projection first, so ordinary validation
   errors happen before native I/O;
3. persists the command in one native transaction;
4. advances `_commands` only after `Persist` returns;
5. updates baseline/mirror bookkeeping.

If step 2 fails, the projection is replayed from the unchanged `_commands`; no
native write occurred.

If step 3 throws a known pre-commit failure, the projection is replayed from the
unchanged `_commands`.

If step 3 throws `CodeGraphCommitOutcomeUnknownException`, the instance sets
`_outcomeUnknown = true` and refuses further reads/mutations. It does not replay
old commands, because that could assert a false rollback.

`Persist` wraps any exception from `txn.Commit()` or the post-commit fault point as
`CodeGraphCommitOutcomeUnknownException`. A pre-commit fault remains the original
exception.

---

## 3. Quarantine and reconciliation

`ThrowIfUncertain` is checked at the start of `MutateAsync`, `ReadAsync`,
`FindOccurrencesAsync` and `TraverseAsync`. The exception is
`CodeGraphCommitOutcomeUnknownException`.

Reopening constructs a new store from durable records; the uncertainty flag is
not persisted. The reopened view is therefore the durable accepted state. The
reviewer must not call this a retry: it is a fresh reconciliation from native
state.

---

## 4. Retry rules

A retry is allowed only after reconciliation:

- same immutable operation/publication identity where the lifecycle supports it;
- no duplicate occurrence facts or contributors;
- no stale generation overwrite;
- correct handling when the earlier commit actually succeeded (reopen sees it and
  the retry must not overwrite it);
- correct handling when it failed (prior publication remains).

A new run ID alone is not proof of safety. No automatic retry is implemented.

---

## 5. Tests

`AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState`:

1. publishes prior facts;
2. injects a fault **after** `txn.Commit()` on the next completion;
3. asserts `CodeGraphCommitOutcomeUnknownException`;
4. asserts current-state queries are refused;
5. reopens and observes the committed `src/New.cs` fact.

Existing N6 (`before-commit`) still verifies that a known failure preserves the
prior publication. N8 reopens after its pre-commit failure for graph observation.
The fault injection is deterministic and is not a crash/power-loss test.

---

## 6. Limitations

- No native transaction-isolation, multi-process writer or power-loss evidence.
- The unknown state is process-local; a reopen is the supported reconciliation.
- A caller that ignores the exception and constructs a new store intentionally
  accepts the durable state; this is the documented recovery path.
- No general recovery coordinator or background repair is introduced.
