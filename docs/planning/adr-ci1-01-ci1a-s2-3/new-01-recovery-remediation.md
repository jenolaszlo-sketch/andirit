# NEW-01 targeted recovery remediation — entry contract

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Task ID:** `ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01` (accepted follow-up).
**Authority:** obligation register `NEW-01 / Significant` (§4); S2.1 authorization
boundary §4; S2.2 entry acceptance §3.7; S2.2 `G-ACCEPT` §12; REM-B05.

---

## 1. The documented defect (verified in source)

The obligation register states: *"`DurableCommandLog.ReplayAsync` restores graph
units through `InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync`, not
occurrence availability/coverage/publication binding; native `MutateAsync` recovery
paths replay. … deliberate cross-unit conflict after replay may bypass lossy
in-memory validation."*

Verified at the expected HEAD in `src/Andirit/DurableCommandLog.cs` and
`src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`:

- **Replay restores graph only.** `DurableCommandLog.ReplayAsync` restores
  repositories, runs, `"replace"` commands via
  `InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync`, staged commands via
  `ApplyCommandAsync`, and terminal runs via `RestoreCompletedRunAsync`.
  `RestorePublishedIndexUnitAsync` (InMemoryCodeGraphStore) updates `_units` and
  invalidates the materialized graph — it **never** touches `_occurrences`,
  `_occurrenceCoverage`, occurrence availability, or the occurrence index.
- **Initial open is unaffected.** `LatticeCodeGraphStore` construction calls
  `ReplayAsync` and then separately `RestoreOccurrences` (which calls
  `RestorePublishedOccurrencesAsync` / `RestoreOccurrencePublication`). So a normal
  close/reopen rebuilds the occurrence projection from the authoritative
  `AndiritOccurrenceUnit`/`Meta` rows.
- **The recovery path omits it.** `LatticeCodeGraphStore.MutateAsync` has two
  recovery branches (pre-apply validation failure and pre-commit persist failure)
  that rebuild `_inner = ReplayAsync(_commands)` + `RestoreBaselines(_inner)` and
  then rethrow. **Neither branch restores occurrences.** After either recovery, the
  in-memory `_inner` has graph units but **no occurrence projection** — even though
  the durable occurrence tables are intact.

This is the exact NEW-01 failure: recovered in-memory state disagrees with durable
authority for the occurrence projection.

---

## 2. Reconstructed failure paths

### 2.1 Pre-commit failure path (validation)
`MutateAsync` applies the command to the in-memory projection first
(`ApplyCommandAsync(_inner, …)`). If that throws (ordinary validation), the
catch rebuilds `_inner` from `_commands` and rethrows. Occurrence projection is
lost. (Durable state unchanged.)

### 2.2 In-memory apply failure path
Same as 2.1 — the pre-apply validation failure branch.

### 2.3 Known pre-commit persist failure path
`Persist(command, …)` throws a known exception before commit; the second catch
rebuilds `_inner` from `_commands` and rethrows. Occurrence projection is lost.
(Durable state unchanged; prior publication retained.)

### 2.4 Post-commit / ack-loss path (already correct)
A `CodeGraphCommitOutcomeUnknownException` sets `_outcomeUnknown = true` and
quarantines the instance; **no replay** occurs. Reopen reconciles durable state.
This path is **not** part of NEW-01 and must not be changed.

---

## 3. Required remediation properties

1. **Graph and occurrence projection restoration.** After any replay-based
   recovery, the in-memory occurrence projection (`_occurrences`,
   `_occurrenceCoverage`, availability, occurrence target index) must correspond to
   durable authority exactly as initial open does.
2. **Publication identity continuity.** Recovered availability/publication binding
   must equal the durable accepted publication.
3. **Coverage and refusal restoration.** `Produced`/`Partial`/`NotProduced`/
   `Unavailable` and refusal counters (including complete-empty) must survive
   recovery.
4. **Cross-unit contributor and conflict validation.** The recovered occurrence
   projection must be subject to the same cross-unit validation as initial open so
   that a deliberate conflict cannot bypass lossy in-memory validation.
5. **Correct successful recovery availability.** A recovered instance must answer
   occurrence queries consistently with a freshly reopened instance (no false
   `AnalysisUnavailable`).
6. **Unknown-outcome quarantine preserved.** The post-commit quarantine path and
   `ThrowIfUncertain` behaviour are unchanged.
7. **Native close/reopen correspondence.** Recovery-time projection equals
   subsequent reopen-state projection.
8. **D3/D4/D5 preserved.** No occurrence facts as legacy graph nodes/edges; no
   publication-authority change; no migration/dual-write (additive only).
   `CodeOccurrenceCoverage.Merge` and frozen identity formulas unchanged.

---

## 4. Smallest authorized implementation scope

**In scope (bounded):**

- Make the `ReplayAsync`-based recovery paths reconstruct the occurrence
  projection from the same durable authority used by initial open — i.e. ensure
  recovery restores occurrences/coverage/availability/publication binding, not
  graph only. Either:
  - (a) extend `ReplayAsync` to restore occurrence state from the replayed
    `"replace"`/`"complete"` commands (which carry `Occurrences` and
    `OccurrenceCoverage`) via the existing
    `InMemoryCodeGraphStore.RestorePublishedOccurrencesAsync` /
    `RestoreCompletedRunAsync` paths; **or**
  - (b) after `ReplayAsync` in the recovery branches, re-run the existing
    `RestoreOccurrences` / baseline / mirror restoration exactly as the constructor
    does.
- Add focused regression tests that reproduce the defect before the fix and assert
  graph **and** occurrence correspondence after recovery.

**Out of scope (not authorized here):**

- Any change to the unknown-outcome/quarantine policy (already accepted).
- Any native schema change (that belongs to the durability work package, document 3).
- General crash / power-loss / multi-process recovery.
- Any `CodeOccurrenceCoverage.Merge` or frozen-identity change.
- Any S2.3 publication wiring.

---

## 5. Required qualification before acceptance

- **Reproduce the known defect before correction** (failing test that shows the
  occurrence projection is lost after a pre-commit/apply failure).
- **Permanent focused regression tests** asserting: graph correspondence;
  occurrence projection correspondence; coverage/state correspondence; the
  deliberate cross-unit conflict is still refused after recovery; successful
  recovery availability; unknown-outcome quarantine distinction; close/reopen
  correspondence.
- **Producer evidence** (logs/TRX) and **independent adversarial qualification**
  (separate reviewer, not the implementer).
- **Formal owner acceptance.**
- Full solution suite + frozen R2 oracle (185/185, `97F6C5E5…`) + formatting gate.

---

## 6. Prohibited S2.3 operations until NEW-01 passes its gate

Until `NEW-01` is remediated, independently qualified, and owner-accepted:

- **No S2.3 native publication** of real occurrence facts (R15).
- **No reuse of the recovery path** for occurrence publication or recovery
  reliance.
- **No durability assertion** that "publication survives recovery" (the projection
  is the recovery output under test).
- Durability/persistence work that would assert recovery-safe state must wait.

Reading code, writing documentation, and the NEW-01 remediation package itself are
permitted.

**Do not mark `NEW-01` closed based on S2.2 non-reliance.** S2.2 did not rely on
recovery; that establishes non-reliance, not correctness.

---

## 7. Ordering

`NEW-01 remediation → NEW-01-Q1 independent qualification → NEW-01-OWNER decision`
must complete **before** the durability gate (document 3) may begin. This matches
the accepted rule: *"Before S2.3 and any production recovery reliance."*
