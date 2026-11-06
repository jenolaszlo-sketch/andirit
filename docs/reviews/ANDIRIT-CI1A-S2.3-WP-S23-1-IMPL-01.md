# ANDIRIT-CI1A-S2.3-WP-S23-1-IMPL-01 — NEW-01 targeted recovery projection remediation

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-1-IMPL-01` (bounded implementation and regression preparation).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Baseline:** `311e97ffa716ff725f70dcb57ca71dca891903f6` (tree `72666340399bdc26012d85566decab9a840c32bb`), clean, one worktree, **19 ahead / 0 behind**, nothing pushed — verified before any change.
**Authorized package:** `WP-S23-1` only.
**Disposition:** **`REMEDIATED — READY FOR INDEPENDENT QUALIFICATION`**.

The known-failure recovery branches of `LatticeCodeGraphStore.MutateAsync` reconstructed
the graph projection but not the occurrence projection; recovered in-memory state
disagreed with the authoritative durable occurrence tables, producing false
`AnalysisUnavailable` and bypassing occurrence validation. The correction makes recovery
rebuild the full accepted projection (graph + occurrences + coverage + availability +
publication binding) from durable authority, exactly as initial open does. No native
publication functionality, semantic extraction, or schema format was changed.

This report and its evidence (`docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-1-IMPL-01-evidence/`)
are additive; no historical evidence was edited.

---

## 1. Verified baseline and authority

| Object | Value |
| --- | --- |
| Starting/final HEAD | `311e97ffa716ff725f70dcb57ca71dca891903f6` |
| Starting tree | `72666340399bdc26012d85566decab9a840c32bb` |
| Working tree | clean; one worktree (`D:/projects/Andirit`); no stashes |
| `origin/main` | `443c192…` (ancestor); 19 ahead / 0 behind; nothing pushed |
| Environment | Windows x64; SDK `10.0.401`; runtime `10.0.12` |

Authorities read directly: S2.3 entry decision
(`adr-ci1-01-ci1a-s2-3-entry-owner-01.md`) and its NEW-01 contract
(`docs/planning/adr-ci1-01-ci1a-s2-3/new-01-recovery-remediation.md`); S2.3 scope
inventory / work-package plan / qualification plan; obligation register §§3–8
(`NEW-01`, `F05`, `SQ-02`, `SQ-03`); REM-01 `contract-reconciliation.md`,
`publication-uncertainty.md`, `coverage-and-availability.md`; Stage 1 owner acceptance;
Candidate C owner decision §§D.3/D.4/D.5; S2.2 `G-ACCEPT` decision.

## 2. Requirement-to-code-and-test matrix

| # | Requirement (authority) | Production surface | Test |
| --- | --- | --- | --- |
| 1 | Known pre-commit failure recovery restores occurrence projection (NEW-01; O-§4) | `MutateAsync` catch (persist) → `RebuildInnerFromDurableStateAsync` | `Recovery_AfterPreCommitFailure_MatchesDurableOccurrenceProjection` |
| 2 | In-memory apply failure recovery restores occurrence projection (NEW-01) | `MutateAsync` catch (apply) → same | `Recovery_AfterInMemoryApplyFailure_PreservesCommittedOccurrenceProjection` |
| 3 | Complete-empty preserved (REM-01 §6) | `RestoreOccurrences` → `RestoreOccurrencePublication` | `Recovery_PreservesCompleteEmptyEvidence` |
| 4 | Partial not upgraded (REM-01 §6; D4) | same | `Recovery_PreservesPartialCoverage` |
| 5 | Cross-unit conflict still refused after recovery (NEW-01) | inner `StageIndexUnitAsync` validation | `Recovery_ConflictRefusalStillEffective` |
| 6 | D3 graph noninterference (O-§D.3) | replay unchanged | `Recovery_PreservesPublishedGraphUnits` |
| 7 | Unknown-outcome quarantine unchanged (SQ-02) | `_outcomeUnknown`; no replay | `UnknownOutcome_StillQuarantinesAndReopenSeesDurableState` |
| 8 | Fresh open / reopen non-regression (S1A §3.5) | constructor path unchanged | existing N1–N8 + reopen in every test |

## 3. Exact defect and affected branches

`LatticeCodeGraphStore.MutateAsync` has two **known-failure** recovery branches, both of
which rebuilt `_inner` with `ReplayAsync(_commands)` + `RestoreBaselines(_inner)` and
**omitted** the occurrence restoration that initial open performs:

- **Branch A — in-memory apply failure** (before: `ApplyCommandAsync(_inner, command)`
  throws).
- **Branch B — known pre-commit persist failure** (before: `Persist(...)` throws a
  non-unknown exception; e.g. the accepted `before-commit` fault point).

Initial open is unaffected: the constructor calls `ReplayAsync` then
`RestoreBaselines` then **`RestoreOccurrences`** then (if native traversal)
`RestoreMirrors`. `RestoreOccurrences` is the routine that reads the authoritative
`AndiritOccurrenceUnit/Meta/Page` rows and calls
`InMemoryCodeGraphStore.RestorePublishedOccurrencesAsync` /
`RestoreOccurrencePublication`. Because `ReplayAsync` restores graph units only
(`RestorePublishedIndexUnitAsync` updates `_units`, never `_occurrences` /
`_occurrenceCoverage`), the recovery branches produced a graph-only store.

The **unknown-outcome** path is separate and correct: a `CodeGraphCommitOutcomeUnknownException`
sets `_outcomeUnknown` and does **not** replay. It is not modified.

## 4. Before-fix reproduction

`tests/Andirit.LatticeDb.Tests/New01RecoveryProjectionTests.cs` (7 tests) reproduces the
defect on the unmodified production code. Raw log:
`new01-before-fix.log`.

```
Failed!  - Failed: 5, Passed: 2, Skipped: 0, Total: 7
```

| Test | Before-fix result |
| --- | --- |
| Pre-commit failure → occurrence projection | **FAIL** — Expected `Available`, Actual `AnalysisUnavailable` |
| In-memory apply failure → occurrence projection | **FAIL** — Expected `Available`, Actual `AnalysisUnavailable` |
| Complete-empty preserved | **FAIL** — Expected `Available`, Actual `AnalysisUnavailable` |
| Partial coverage preserved | **FAIL** — Expected `IncompleteAnalysis`, Actual `AnalysisUnavailable` |
| Conflict refused after recovery | **FAIL** — `Assert.Throws` failure: **no exception was thrown** (validation bypass) |
| Published graph units preserved | PASS (graph is restored) |
| Unknown-outcome quarantine + reopen | PASS (path is separate) |

**Durable-state vs in-memory-state comparison (before the fix):** a fresh reopen of the
same database returned the committed occurrence facts (`Available`, correct coverage),
while the recovered in-process store returned `AnalysisUnavailable` with no facts — the
recovered state disagreed with durable authority. The graph test passing confirms the
discrepancy was occurrence-only, exactly as the accepted defect description states.

The conflict-test failure is the second documented NEW-01 consequence: with the
occurrence projection missing, a deliberate cross-unit conflict **bypassed** in-memory
validation (no `CodeGraphBatchRejectedException`).

## 5. Correction design

One private helper replaces the graph-only rebuild in both known-failure branches:

```csharp
private async ValueTask RebuildInnerFromDurableStateAsync()
{
    _inner = await ReplayAsync(_commands, CancellationToken.None).ConfigureAwait(false);
    RestoreBaselines(_inner);
    RestoreOccurrences(CancellationToken.None);
}
```

Reconstruction order matches initial open: **command-log replay → baseline restoration →
occurrence restoration**; `_inner` becomes active at assignment. `RestoreOccurrences`
**reads only** and never mutates durable authority. The existing routine is reused (no
second restoration implementation, no duplicated decoding/publication validation).

Failure handling is preserved: each catch still rethrows the original exception; if
occurrence restoration itself failed it would propagate (fail-closed), never exposing a
graph-only store as healthy or fabricating current authority. The unknown-outcome
quarantine contract is untouched.

## 6. Production files changed

`src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` — **+23 / −4** (two call-site lines
replaced; one 21-line private helper added). No public API change; no schema-format
change; no other production file changed. Full diff: `production-diff.txt`.

## 7. Full recovery-state matrix (after fix)

| State | Requirement | Result |
| --- | --- | --- |
| Repository / index-run | replay | preserved |
| Published graph units | D3 | `Recovery_PreservesPublishedGraphUnits` PASS |
| Published occurrence units/facts | NEW-01 | pre-commit/apply tests PASS |
| Occurrence target index | query | tests PASS |
| Coverage (`Produced`/`Partial`) | REM-01 §6 | complete-empty + partial tests PASS |
| Availability | coverage-and-availability §3 | `Available`/`IncompleteAnalysis` correct |
| Publication identity | D4.1 | `durable.Publication == recovered.Publication` |
| Source-unit ownership | REM-01 §4.3 | unchanged (facts restored verbatim) |
| Contributor records | REM-01 §4.3 | conflict test PASS |
| Conflict validation | NEW-01 | `Recovery_ConflictRefusalStillEffective` PASS |
| Baselines | replay | `RestoreBaselines` preserved |

## 8. Known versus unknown outcome boundary

- **Known outcome** is not misclassified as unknown: the `before-commit` fault and the
  in-memory apply failure both rethrow their specific exception
  (`InjectedPersistenceException` / `CodeGraphBatchRejectedException`), not
  `CodeGraphCommitOutcomeUnknownException`.
- **Unknown outcome** is not declared committed/rolled back: the ack-loss test asserts
  `CodeGraphCommitOutcomeUnknownException` on the mutation and on a subsequent
  current-state query, and that reopen reconciles durable state.
- **Quarantine** remains conservative; replay does not manufacture publication
  authority; recovery does not clear safety flags. No new command-log protocol or
  broader crash recovery was introduced.

## 9. Coverage and availability evidence

`Recovery_PreservesCompleteEmptyEvidence`: an authoritative complete-empty unit keeps
`Available` + `IsComplete` + zero facts after recovery (never `AnalysisUnavailable`).
`Recovery_PreservesPartialCoverage`: a `Partial` unit keeps `IncompleteAnalysis`,
`IsComplete == false`, and the merged state. Coverage counters/`UnavailableReasons` are
restored by the reused `RestoreOccurrences` path; restored coverage is not treated as
independent proof of trusted semantic authority (WP6 semantics unchanged).

## 10. Contributor and conflict evidence

`Recovery_AfterInMemoryApplyFailure_PreservesCommittedOccurrenceProjection` and
`Recovery_ConflictRefusalStillEffective` exercise actual mutation validation (not merely
a read): a conflicting immutable payload for a committed occurrence identity is
**refused** after recovery, and the rejected contribution does not change durable state
(`GetPublishedUnitsAsync` unchanged).

## 11. Initial-open and reopen non-regression

Path A (fresh open) and Path B (in-process recovery) are compared against the same
durable data in each recovery test via a fresh `new LatticeCodeGraphStore(path)`. They
agree on availability, ordered facts, coverage state, and publication identity. The
existing Stage 1 `N1`–`N8`, complete-empty/incomplete/delete, and ack-loss tests remain
green (native suite 50/50).

## 12. D3 / D4 / D5 preservation

- **D3:** recovery neither creates, removes, nor reinterprets graph facts
  (`Recovery_PreservesPublishedGraphUnits`); occurrences are not legacy graph elements.
- **D4:** the existing persistence/transaction/replay/projection architecture is reused;
  no replacement storage engine, background framework, or repair process was added.
- **D5:** native schema/format semantics unchanged; no migration, dual-write, adapter,
  relaxed namespace refusal, or structural reinterpretation. The change is source-bound
  to the recovery discrepancy.

## 13. Regression results

| Verification | Command | Result |
| --- | --- | --- |
| NEW-01 regressions (after) | `dotnet test … --filter New01RecoveryProjectionTests` | **7 / 0 / 0** |
| Native suite | `dotnet test tests/Andirit.LatticeDb.Tests` | **50 / 0 / 0** (43 baseline + 7 new) |
| Whole solution | `dotnet test Andirit.slnx -c Release` | **796 / 0 / 0** (50 + 157 + 345 + 244) |
| `dotnet format` | `--verify-no-changes` | **exit 0** |
| Release build | `dotnet build -c Release` | **0 warnings, 0 errors** |

Baseline was 789; +7 new tests = 796. S2.2 baselines WP8 9, WP8-Q1 10, WP7-Q1 19,
WP6-Q1 30 are included in the solution/corpus suites and remain green.

## 14. Frozen oracle integrity

- Frozen R2 oracle: **185/185**, output SHA-256
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged).
- Frozen expected `oracle-expected-r2.json` SHA-256
  `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` (unchanged).
- Frozen identity formulas, `CodeOccurrence.cs`, and the frozen corpus are untouched.

## 15. Platform limitations

Windows x64 only (SDK `10.0.401`, runtime `10.0.12`). Nothing was pushed, so no hosted CI
ran for this change; **no Linux/macOS qualification is claimed**. The Linux native
minimum remains an S2.5/Stage-2 acceptance obligation.

## 16. Remaining qualifications

- The unknown-outcome/ack-loss path is **not** re-verified as new functionality; it is
  unchanged and asserted only for non-regression.
- `RestoreMirrors` is not re-run on recovery; mirrors are derived from the unchanged
  durable graph and the failed mutation rolled back, so `_mirrors` remains consistent.
  (Independent qualification should exercise native traversal + recovery explicitly.)
- Broader crash/power-loss/multi-process recovery, MVCC, and durability across process
  restarts remain out of scope (SQ-02/SQ-03/Stage 6).
- The defining-`compctx2` carrier (WP-S23-2) and real-fact publication (WP-S23-3) are not
  implemented.
- S-01 component-digest canonicalization remains a retained S2.5 obligation.

## 17. Independent NEW-01-Q1 reviewer entry requirements

The reviewer must independently challenge the correction. Recommended probes: reproduce
both original failure branches and the conflict bypass; compare recovery against a fresh
durable reopen (Path A vs Path B); verify exact graph and occurrence projection
correspondence; complete-empty vs unavailable; partial/refusal coverage; publication
identity consistency; multiple units/contributors; conflict rejection after recovery;
known vs unknown outcome; failure during reconstruction; repeated recovery; D3/D4/D5
non-regression; no native schema migration; frozen S2.2 authority preservation; and
native traversal combined with recovery. The reviewer should derive expected states from
durable authority, not from the producer's before/after output.

---

**Final disposition: `REMEDIATED — READY FOR INDEPENDENT QUALIFICATION`.** No `NEW-01-Q1`,
no owner acceptance, no WP-S23-2 work, no publication, nothing pushed.
