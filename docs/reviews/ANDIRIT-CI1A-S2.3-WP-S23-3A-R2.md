# ANDIRIT-CI1A-S2.3-WP-S23-3A-R2

## Staged-run durable reconstruction ordering remediation

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-R2` (bounded remediation of S23-PUB-Q1 finding **F-Q1-01** only).
**Repository:** `D:/projects/Andirit`. **Canonical branch:** `main`. **Date:** 2026-11-07.
**Starting HEAD/tree:** `b76dc4c825ac73da832265338d56a223ffbc5b6f` / `6116523b445a52043efcda010dd2a95ed6ea06c3` (the S23-PUB-Q1 review commit).
**Disposition:** **`R2 IMPLEMENTED — READY FOR INDEPENDENT S23-PUB-Q2`**.

## 1. Preflight

Exact HEAD/tree verified (the Q1 review commit), clean tracked/index state, canonical `main`, one worktree, no stashes; no prior R2 existed; no concurrent writer (state re-checked before each commit). Read: the S23-PUB-Q1 report (F-Q1-01 and probes P10–P13/P17/P18), the R1 report, the current `DurableCommandLog.ReplayAsync`, and the accepted S23-DUR / Q1-Q1 / D3-D4-D5 / publication contracts (`wp-s23-3-publication-integration.md`, WP-S23-2 records, WP-S23-3 entry owner decision).

## 2. Baseline reproduction (before any production change)

The committed, reviewer-owned S23-PUB-Q1 probe harness was rerun into fresh R2 databases with R2-prefixed outputs (the original Q1 evidence untouched). Results (evidence `R2PRE-*`):

| Scenario | Stage-key order | Pre-fix result |
| --- | --- | --- |
| R2PRE-P10 → P11 | referencing unit first (`definingUnitFirst=False`) | segments stage correctly; **fresh-process reopen refuses at open** — `CodeGraphBatchRejectedException` with 4× `graph.edge.endpoint-missing` |
| R2PRE-P12 → P13 | referencing unit first | **re-stage branch unreachable** (same refusal at open) |
| R2PRE-P17 → P18 | defining unit first (`definingUnitFirst=True`) | resumption completes; publishes 2 units with markers; authority gate fails closed `AnalysisUnavailable` |

Both orders carry the **same logically valid staged publication** (identical segments, produced by the same real plugin/sink flow; only the project names — and therefore the persisted `HetuStage` key order — differ). This confirms Q1's F-Q1-01 exactly.

**Exact replay sequence and validation call sites (pre-fix):**
1. `LatticeCodeGraphStore` constructor reads `HetuStage` rows via `ReadTable` (ordinal key order = `StageKey` = repository\nrun\nplugin\nunit) → `DurableCommandLog.Reconstruct`.
2. `ReplayAsync` step 4 applied stage rows **one at a time** (`ApplyCommandAsync` → `InMemoryCodeGraphStore.StageIndexUnitAsync` / `StageIndexUnitDeletionAsync`).
3. `StageIndexUnitAsync` computes the prospective set as `_units` + **staged-so-far** and runs the full `ValidateMaterializedGraph` (cross-unit endpoints included) against that partial set → order-dependent refusal.
4. With `QApp`'s unit hash (`2810…`) < `QLib`'s (`2e8a…`), the referencing unit's staged row replays first and its cross-unit edges target nodes in a segment not yet applied.

## 3. Exact production change

+151/−2 across two files (evidence `production-diff.txt`). `ApplyCommandAsync` was deliberately **retained** — it is the live mutation applier used by `MutateAsync`; only the replay path changed.

| File | Change |
| --- | --- |
| `src/Andirit/DurableCommandLog.cs` | `ReplayAsync` step 4 now groups stage commands **per (repository, run)** — the group key derived exactly as `StageKey` derives it (replacement-origin fields for `stage-replace`, command fields for `stage-delete`) — and reconstructs each group with one `RestoreStagedRunAsync` call. |
| `src/Andirit/InMemoryCodeGraphStore.cs` | New internal `RestoreStagedRunAsync(repositoryId, indexRunId, commands)`: per-command ownership checks and intrinsic unit-structure checks in durable command order; graph and occurrence invariants validated **once** against the complete applicable staged set; staged bookkeeping committed only after validation succeeds. |

### Reconstruction and validation sequence (after)

For each (repository, run) group, inside one `_gate` critical section:

1. **Per command, in durable command order:** `stage-replace` → `EnsureKnownOwnership` (repository registered, run registered and running, plugin in run) + intrinsic duplicate-id checks for the unit; `stage-delete` → `EnsureRunningRun`. A later command for one stage identity wins (mirroring live bookkeeping; the durable table stores one row per stage identity, so this only defends a hand-crafted or future log).
2. **Reconstruct the complete applicable staged set:** `_units` − this run's staged deletions + this run's staged replacements (exactly `ApplyStagedChanges` semantics). Staged state of other runs is not part of this boundary and is never merged.
3. **Validate once:** `ValidateMaterializedGraph(repositoryId, completeStagedSet)` — cross-unit endpoints, declaration symbol nodes, symbol uniqueness, fact conflicts, and each unit's intrinsic duplicate checks all run against the complete set — plus `ValidateOccurrences` over the same boundary (mirroring live per-command occurrence validation, including the `TryGetCoverage` gating).
4. **Commit only after validation:** `GetOrCreateStagedRun` bookkeeping is written exactly as live staging would leave it (deletions then replacements, per owner). On any error, `CodeGraphBatchRejectedException("The restored staged graph is invalid.", errors[:100])` is thrown **before** any staged mutation; the open-time store is unreachable until `ReplayAsync` completes, so no partial staged or published state is exposed (proven by test 12 and probes).

### Run/stage grouping justification (§4)

The reconstruction boundary is the **run** (`RunKey` = repository + run):
- The stage key is `repository\nrun\nplugin\nunit` — the persisted table and its ordinal enumeration therefore scope every stage row to exactly one run.
- Live staging bookkeeping (`_staged[RunKey]`), staged-state computation (`ApplyStagedChanges(key)`), occurrence staging (`ApplyStagedOccurrences(key)`), and publication (`CompleteIndexRunAsync` folds exactly one run's staged set) are all **per RunKey**.
- Groups are formed per (repository, run); rows of different runs, repositories or publication generations are never merged. Within a group the durable command order is preserved. No safe-reconstruction boundary ambiguity exists in the accepted replay format, so no **DESIGN DECISION REQUIRED** arises.
- Cross-group independence: run B's staged prospective never includes run A's staged state (per-RunKey semantics), so group order carries no validation meaning (test 11).

## 4. Focused regression tests (§6)

New suite `tests/Andirit.CSharp.Tests/S23StagedReopenTests.cs` — **14 tests, all passing**:

| # | Requirement | Test |
| --- | --- | --- |
| 1 | Staged cross-unit graph, referencing unit first | `StagedCrossUnitGraph_ReferencingUnitFirst_SurvivesReopenAndCompletes` |
| 2 | Same graph, defining unit first | `StagedCrossUnitGraph_DefiningUnitFirst_SurvivesReopenAndCompletes` |
| 3 | Real cross-project occurrence-bearing staged publication | `RealCrossProjectInterruptedRun_Reopens_AuthorityFailsClosed` (real plugin → sink chain) |
| 4 | Interruption between `sink.CompleteAsync()` and `CompleteIndexRunAsync()` | setup of tests 3/6 (no completion before dispose) |
| 5 | Fresh-process reopen of the interrupted run | `new LatticeCodeGraphStore` over the durable file; run still running, 0 published units |
| 6 | Existing authority-unavailable outcome after successful reopen | completion of the resumed run publishes markers without authority; query refuses `AnalysisUnavailable`, 0 hits, 0 examined, not complete; plus `…_ReStagedResumption_BecomesAvailable` for the now-reachable Q1-Q1 re-stage branch (fresh verified authority → `Available`/1 hit/complete) |
| 7 | Genuine missing endpoint after complete reconstruction | `StagedMissingEndpoint_AfterCompleteReconstruction_IsStillRefused` (defining unit's staged row removed → refuse) |
| 8 | Inconsistent and malformed staged records | `InconsistentStagedRecord_IsRefused` (`graph.edge.endpoint-missing`); `MalformedStagedRecord_IsRefused` (`JsonException`) |
| 9 | Duplicate or conflicting staged identities | `DuplicateStagedIdentity_IsRefused` (`replacement.node.duplicate`); `ConflictingStagedIdentity_IsRefused` (`graph.node.conflict`) |
| 10 | Repeated stage replacement and command-order semantics | `RepeatedStagedCommands_LastCommandWins` (replace-replace last-wins; unknown-unit deletion no-op; staged-unit deletion removes it from the completed publication) |
| 11 | Isolation of different runs and repositories | `StagedGroups_AreIsolatedPerRunAndRepository` (per-run publication scoping; invalid unit in another repository refuses the whole reconstruction; durable completion coexists) |
| 12 | Failure without partial exposure | `FailedStagedReconstruction_ExposesNothing_AndWritesNothing` (durable snapshot byte-identical across the failed open) |
| 13 | R1 completed-publication replay behavior | `PublishedAndStagedReconstruction_Coexist` (published + staged reconstruction in one replay; the accepted stale-context refusal also exercised: a staged run planned before a newer completion refuses live completion) |
| 14 | Single-unit staged reopen | `SingleUnitStagedReopen_RemainsCorrect` |

All tests use real durable round trips through `LatticeCodeGraphStore` or the real replay API; the two-process fresh-process evidence for the original finding is carried by the producer probes below.

## 5. Independent producer probes (§7)

The committed Q1 probe harness was rerun post-fix with R2 output names (one OS process per phase; durable snapshot hashes recorded per phase in `R2POST-probe-matrix.json`). **24/24 phases pass** — including the full non-segment matrix (positive, graph-only, corruption, negative-authority, stale-after-reopen, replay permutation), confirming no regression elsewhere. The F-Q1-01 scenarios:

| Probe | Process ids | Post-fix result |
| --- | --- | --- |
| R2POST-P10 → P11 (formerly failing) | 28964 → 13096 | staged run **reopens**; completion publishes 2 valid units with markers; occurrence query fails closed `AnalysisUnavailable` ("Missing or malformed recorded occurrence authority; re-ingestion required.") — 0 hits, 0 examined, not complete |
| R2POST-P12 → P13 (formerly failing) | 23536 → 30572 | re-stage branch now reachable: fresh verified authority re-staged into the resumed run; completion → `Available`, 1 hit, complete, correct source unit |
| R2POST-P17 → P18 (formerly passing) | 43600 → 28512 | unchanged: resumption completes; fail-closed `AnalysisUnavailable` preserved |

No unqualified occurrence becomes `Available` in any probe; genuine corruption still refuses (R2POST-P07–P09 trio unchanged, durable snapshots byte-identical across refused opens).

## 6. Occurrence-authority containment (§5)

- No occurrence-authority source was touched: the production diff is limited to `DurableCommandLog.cs` and `InMemoryCodeGraphStore.cs` (`git diff` verified).
- Segment records still persist the `RequiresOccurrenceAuthority` marker without the transient envelope (R2POST-P10/P12/P17 assert `hasAuthorityProperty=false` on the real `HetuStage` rows).
- Reopen success is not permission to publish incomplete facts: the reconstructed staged set completes to a marker-bearing publication whose occurrence gate refuses `AnalysisUnavailable` until fresh verified authority is re-staged (R2POST-P11 vs P13).
- Stale-publication, missing-authority, corruption refusals, Option A separation, and source/defining-unit relationships are unchanged (focused suites 69/69, full solution green, frozen oracle unchanged).

## 7. Regression gates (§8)

| Gate | Result |
| --- | --- |
| New staged-replay suite | **14 / 0 / 0** |
| Focused S23 suites (14 new + 55 existing) | **69 / 0 / 0** |
| NEW-01 reviewer suite | **17 / 0 / 0** |
| Full solution | **865 / 0 / 0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 313; prior 851 + 14 new) |
| Frozen R2 oracle | **185/185**, 146,851 bytes, SHA-256 recomputed `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` — unchanged |
| Release build / format | 0 warnings, 0 errors / exit 0 |

No existing test was removed, disabled, or weakened. D3/D4/D5, stale-context behavior (explicitly exercised in test 13), unknown-outcome quarantine (NEW-01 suite), and native-mirror consistency (LatticeDb suite) all remain green.

## 8. Commits

| Object | Value |
| --- | --- |
| Starting HEAD/tree | `b76dc4c825ac73da832265338d56a223ffbc5b6f` / `6116523b445a52043efcda010dd2a95ed6ea06c3` |
| Implementation commit (production + tests + evidence + probe runner) | **`a4d85011b8b753d4b058ed3a2a02fe2ebc73294c`** (tree `cfbf67eb4d38999c3b95e0186655915461ff732b`; parent `b76dc4c`) |
| Reviewer-handoff commit (this report) | recorded in the external handoff |
| Working tree | clean after the handoff commit; single worktree; nothing pushed |

## 9. Independent S23-PUB-Q2 handoff questions

1. **Order invariance at the staged boundary.** Independently rerun the segment probes (P10–P13, P17–P18) in separate processes and confirm both stage-key orders reopen, complete, and reach identical fail-closed/available branches per the accepted Q1-Q1 semantics.
2. **Grouping boundary audit.** Verify the group key is derived exactly as `StageKey` (replacement-origin fields for `stage-replace`, command fields for `stage-delete`) and that no grouping merges rows across runs, repositories, or publication generations — including the `Repeat`/deletion semantics in `S23StagedReopenTests.RepeatedStagedCommands_LastCommandWins`.
3. **Atomicity and exposure.** Confirm staged bookkeeping is committed only after complete-set validation (source inspection of `RestoreStagedRunAsync`) and that a refused reopen leaves the durable snapshot byte-identical (tests 12, probes).
4. **Authority containment after reopen.** Confirm the reconstructed staged publication still fails closed (`AnalysisUnavailable`) without fresh verified authority, that the transient envelope is absent from segment records, and that the re-stage branch reaches `Available` only with fresh verified authority (R2POST-P11 vs P13; test 3/6).
5. **Corruption refusal.** Confirm missing-endpoint, inconsistent, malformed, duplicate and conflicting staged records are still refused after complete reconstruction (tests 7–9; probes), and that R1's completed-publication behavior is unchanged (test 13; R1 suite).
6. **Regression integrity.** Reproduce the gates in §7, including the frozen-oracle full digest.
7. **Scope discipline.** Confirm the production diff is exactly the two files' bounded changes, with no public API, persisted-format, segment-authority, or occurrence-authority modifications.

## 10. Remaining qualifications

Q1-Q2 (no stronger recovery-rebuild atomicity/quarantine claim), S-01 (caller-supplied digests), O-Q2-01 (separate hardening candidate), SQ-03/SQ-04 and real-fact N1–N8 (WP-S23-4/S2.5) remain outstanding and untouched. Verification remains Windows-x64, single-writer; no crash/power-loss/cross-platform claims are made. This remediation does not authorize WP-S23-3B/4/5, S2.4/S2.5, or Stage 6, and claims no owner acceptance.
