# ANDIRIT-CI1A-S2.3-WP-S23-3A-R1

## Durable cross-unit graph reconstruction remediation — implementation and reviewer handoff

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-R1` (bounded remediation of `B-WP-S23-3A-01`).
**Repository:** `D:/projects/Andirit`. **Canonical branch:** `main`. **Date:** 2026-11-07.
**Authorization:** owner remediation authorization for **B-WP-S23-3A-01** only (WP-S23-3A scope; no WP-S23-3B/4/5, no S2.4/S2.5, no Stage 6, no public API, no schema/format change, no push).
**Disposition:** **`R1 IMPLEMENTED — READY FOR INDEPENDENT S23-PUB-Q1`**.

## 1. Starting state and preflight

| Object | Value |
| --- | --- |
| Starting HEAD | `f8b2b08bbd2db0a551412816705a7971973658bd` |
| Starting tree | `0cc5ad3a9c045e4250d35a9d666b17f52e2b5879` |
| Working tree / worktrees | clean tracked/index state; one canonical worktree (`D:/projects/Andirit`); no stashes |
| Environment | Windows x64 10.0.26200; SDK `10.0.401`; runtime `10.0.12` |
| Producer baseline (WP-S23-3A IMPL-01) | whole solution **840 / 0 / 0**; NEW-01 **17/0/0**; frozen R2 oracle **185/185** (`97F6C5E5…39C9`); Release build and format clean |

Preflight verified: exact HEAD/tree/branch/clean state, single worktree, recent-commit ancestry (`e2b833a` WP-S23-3A implementation → `f8b2b08` its review record), the producer report and its blocker reproduction, and the accepted S2.3 durability, corruption-refusal and publication contracts (WP-S23-2 IMPL/Q1/R1/Q2/OWNER records, WP-S23-3 entry owner decision). No concurrent Andirit-modifying agent was observed at any point (working tree remained exclusively owned by this commission; status re-checked before each commit).

## 2. Original failure and root cause

**Defect `B-WP-S23-3A-01` (pre-existing).** Durable reopen of any multi-unit publication whose units contain legitimate cross-unit graph edges failed with:

```
CodeGraphBatchRejectedException: The restored graph is invalid.
graph.edge.endpoint-missing=An edge endpoint does not exist in the materialized graph. (x4 in the producer case)
```

**Root cause chain (all locations verified before editing):**

1. `LatticeCodeGraphStore`'s constructor reads the persisted `HetuUnit` table in **ordinal key order** (`ReadTable`, `LatticeCodeGraphStore.cs:108–114`) and folds it via `DurableCommandLog.Reconstruct`.
2. `DurableCommandLog.ReplayAsync` restored each `replace` command **one at a time** (`DurableCommandLog.cs:131–135` pre-fix) in that enumeration order.
3. `InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync` (`InMemoryCodeGraphStore.cs:387–406` pre-fix) ran the **complete** `ValidateMaterializedGraph` — including cross-unit endpoint checks (`graph.edge.endpoint-missing`), declaration→symbol-node checks (`graph.declaration.symbol-missing`), symbol-uniqueness and fact-conflict checks — against **only the units restored so far**, and committed each unit incrementally.

The C# plugin persists unit ids `csharp:project:<sha256(project path)>` (`CSharpProjectDiscovery.IndexUnitId`); for the producer fixture `App/App.csproj` (hash `94c6…`) ordinally precedes `Lib/Lib.csproj` (hash `b89b…`), so the referencing unit was validated before its endpoint unit existed. Publication itself succeeds because staging follows dependency order (endpoint validation at staging time); the defect was observable only on durable reconstruction, exactly as the accepted design frame anticipated (`docs/design/ec6a-cross-file-resolution-architecture.md:162–175`).

**Independent reproduction (before any production change):** see `ANDIRIT-CI1A-S2.3-WP-S23-3A-R1-evidence/reproduction-before-fix.txt`. Both required controls were reproduced: (A) a graph-only two-unit publication with zero occurrence rows failed reopen identically, and (B) the real cross-project occurrence publication failed identically (producer's pinned blocker test passed pre-fix). Single-unit reopen was never affected.

## 3. Exact production change

Bounded, +73/−10 across two files (full diff: `ANDIRIT-CI1A-S2.3-WP-S23-3A-R1-evidence/production-diff.txt`):

| File | Change |
| --- | --- |
| `src/Andirit/DurableCommandLog.cs` | `ReplayAsync` no longer restores `replace` commands one-by-one. It collects all persisted unit replacements (preserving per-item cancellation checks) and issues **one** bulk restore: `RestorePublishedUnitsAsync(replacements)`. No other replay step changed. |
| `src/Andirit/InMemoryCodeGraphStore.cs` | `RestorePublishedIndexUnitAsync` (per-unit restore + per-step full-graph validation) was replaced by `RestorePublishedUnitsAsync(IReadOnlyList<CodeIndexUnitReplacement>)`, which restores the complete persisted unit set as one reconstruction. |

No other production file changed. No public API, batch contract, storage path, persisted format, or occurrence-authority surface changed.

### Before/after graph reconstruction sequence

**Before (order-dependent, per-unit validation):**

```
for each replace command in ReadTable key order:
    prospective = _units-so-far + { this unit }        // partial graph
    errors = ValidateMaterializedGraph(repo, prospective)   // FULL graph validation incl. cross-unit endpoints
    if errors: throw (reopen refused — ORDER-DEPENDENT)
    _units = prospective                                // incremental publication, N intermediate states
```

**After (complete reconstruction, validate once, publish once):**

```
replacements = [ all replace commands ]
RestorePublishedUnitsAsync(replacements):
    lock (_gate):
        1. per-unit isolated structural validation      // duplicate node/declaration/edge ids within each unit
        2. prospective = _units + all units (command order; later row per owner wins, matching prior semantics)
        3. for each affected repository:
               ValidateMaterializedGraph(repo, prospective)   // COMPLETE graph: endpoints, symbol nodes,
                                                              // symbol uniqueness, cross-unit fact conflicts
        4. if errors: throw CodeGraphBatchRejectedException("The restored graph is invalid.", errors[:100])
           — nothing committed
        5. _units = prospective; _materialized invalidated for affected repositories   // single atomic commit
```

### What validation moved and what stayed (not a disabling of intermediate validation)

| Check | Requires complete graph? | Timing after fix |
| --- | --- | --- |
| Duplicate fact ids **within** one unit (`replacement.{node,declaration,edge}.duplicate`) | No — unit-local | Per unit, in isolation, **plus** again over the final set (`ValidateMaterializedGraph` re-runs them) — cannot regress |
| Cross-unit fact conflicts (`graph.{node,declaration,edge}.conflict`), symbol uniqueness (`graph.symbol.duplicate`) | Yes | Once, after all units restored |
| Edge endpoints (`graph.edge.endpoint-missing`) | Yes | Once, after all units restored |
| Declaration symbol nodes (`graph.declaration.symbol-missing`) | Yes | Once, after all units restored |
| Staged-command validation (`StageIndexUnitAsync`/`StageIndexUnitDeletionAsync` in replay step 4) | No change | Still validates against the **complete** published set (all replaces are committed before step 4) |
| Terminal-history validation (`RestoreCompletedRunAsync` in replay step 5) | No change | Still validates the complete set; unchanged |

### Validation timing and atomicity proof

- `RestorePublishedUnitsAsync` performs every read, validation, and mutation **inside one `lock (_gate)` critical section**; `_units` is replaced by a single dictionary assignment only after validation succeeds. There is no intermediate observable state: readers cannot observe a partially reconstructed publication (no store is returned to any caller unless `ReplayAsync` completes, and the open-time store is unreachable until its constructor finishes).
- On failure the exception is thrown **before** `_units` or `_materialized` are touched; the test `FailedReplay_RefusesReopen_WithoutPublishingPartialState` proves the durable snapshot is byte-identical before/after the failed open (no write-back, no partial exposure) and that reopen is refused.
- Transaction semantics, publication-generation state, baseline anchoring (`RestoreBaselines`/`RebaseRunningRuns`), replay sequencing, and occurrence restoration from the authoritative native tables are all untouched: units from incompatible durable states cannot be merged because the reconstruction set is exactly the one publication's persisted rows, validated per repository exactly as staging validates.

### Order-independence proof

- `Replay_IsIndependentOfPersistedUnitEnumerationOrder` replays the same persisted command set with the referencing unit enumerated first (the previously failing order) and reversed; both must produce identical published unit sets and identical traversal edges. Pre-fix the forward order threw at `RestorePublishedIndexUnitAsync`; post-fix both orders are identical.
- Durable-level order variation: `S23DurableGraphReopenTests` publishes the same logical cross-unit graph under both HetuUnit key orders (persisted order asserted from the durable rows), and `CrossProjectRealFact_DefiningUnitPersistedFirst_ReopensWithIdenticalOutcome` does the same with a real occurrence-bearing cross-project publication (unit ids computed from the plugin's public identity convention; the defining unit's hash `01c2…` precedes the referencing unit's `f5f3…`).

## 4. Fresh-process cross-project reopen evidence

`CrossProjectRealFact_SurvivesDurableReopen_WithCorrectUnitsAndCoverage` doubles as a two-phase harness (`S23_REOPEN_FRESH_PROCESS=publish|verify`, `S23_REOPEN_FRESH_PROCESS_PATH`). Two separate `dotnet test` invocations ran in two separate OS testhost processes (`ANDIRIT-CI1A-S2.3-WP-S23-3A-R1-evidence/fresh-process-reopen.log`):

| Phase | Testhost PID | Exit | Action |
| --- | --- | --- | --- |
| publish | `31128` | 0 | Real C# plugin (Lib ← App) → sink → LatticeDB; durable file written; **no reopen in this process** |
| verify | `22644` | 0 | **Only** reopened the durable file left by the publishing process; recomputed expected target from the accepted frozen identity helpers and the live verified compilation; asserted: 2 published units; cross-project graph edge resolves to the Lib project node; occurrence query `Available`, exactly one hit, `IsComplete = true`, correct `TargetId`, `SourceUnitId` = App's `ProjectUnitId`; durable receipt `RequiresAuthority = true` and `Authority.Facts[0].DefiningProjectUnitId` = Lib's `ProjectUnitId` |

The verify process never publishes; the reopened state comes exclusively from durable reconstruction. Without the environment variables the same test runs the full in-process durable round trip, so the suite remains self-contained.

## 5. Negative corruption controls (invariants preserved)

| Control | Test | Result |
| --- | --- | --- |
| Genuine missing endpoint **after** complete reconstruction | `GenuinelyMissingEndpoint_AfterCompleteReconstruction_IsStillRefused` (removes the Lib `HetuUnit` owner row from a real durable file) | reopen refused, `graph.edge.endpoint-missing` |
| Inconsistent persisted publication (edge to a node no persisted unit defines) | `InconsistentPersistedUnit_RemainsRefused` | reopen refused, `graph.edge.endpoint-missing` |
| Malformed persisted row (shape-corrupted `HetuUnit` payload) | `MalformedPersistedUnitRow_RemainsRefused` | reopen refused, `JsonException` (malformed base64 refusal remains covered by `LatticeCodeGraphStoreTests.Store_RejectsCorruptedDurablePayloadOnReopen`, still passing) |
| Failed replay exposes nothing | `FailedReplay_RefusesReopen_WithoutPublishingPartialState` | refusal thrown; durable snapshot byte-identical before/after |
| Duplicate/conflicting identity refusal | unit-level duplicates validated per unit in isolation plus over the final set; cross-unit conflicts and symbol-uniqueness validated on the complete set (strictly stronger or equal to pre-fix) | preserved |
| Stale refusal after reopen | `ReopenedStore_RefusesStaleRealFact_AfterTransitiveDependencyChange` | `StalePublication`, 0 hits, 0 examined, not complete |
| Missing authority after reopen | `ReopenedStore_MissingAuthority_IsUnavailable_NotEmptySuccess` | `AnalysisUnavailable`, never a successful empty |
| Single-unit reopen | `SingleUnitGraphOnlyPublication_SurvivesDurableReopen`; producer's `SingleUnitRealFact_SurvivesDurableReopen`; `LatticeCodeGraphStoreTests` reopen suite | all pass |
| Complete-empty vs unavailable/corrupted, orphaned-owner refusal (F-01), unknown-outcome quarantine, NEW-01 recovery, retirement, mirrors | existing suites (`S23NativeAuthorityTests`, `S23OccurrenceOwnershipRegressionTests`, `New01RecoveryProjectionTests`, `OccurrenceStage1NativeTests`, `OccurrenceRemediationNativeTests`, `HistoricalStorageIdentityTests`) | all pass unchanged |

No previously rejected genuinely corrupted graph is now accepted: every pre-fix rejection cause other than enumeration order still rejects, now after the complete reconstruction.

## 6. Occurrence-authority regression results

The WP-S23-3A implementation is intact and untouched by this remediation:

- `ICodeOccurrenceAuthoritySink` + `PluginScopedSink` forwarding, accepted `CodeOccurrenceAuthority` envelope and marker, atomic staging of facts+coverage+authority, graph-only output when authority cannot be carried, Option A separation, and segment `[JsonIgnore]` authority behavior — no source change (`git diff` touches only the two graph-reconstruction files).
- Q1-Q1 fail-closed resumption: `S23NativeAuthorityTests.SegmentResumption_MarkerSurvives_ButMissingFactsAndCarrierNeverBecomeVerifiedEmpty` passes — the staged segment persists the marker without facts/authority, and a resumed store completing the run serves `AnalysisUnavailable` (never verified-empty) after final reopen.
- Durable authority receipts after reopen: `VerifyCrossProjectReopenAsync` asserts `DefiningProjectUnitId` (defining unit) and the query result `SourceUnitId` (source unit) with real cross-project facts (`CrossProjectRealFact_DefiningUnitPersistedFirst_ReopensWithIdenticalOutcome` repeats this under the reversed persisted order).
- No transient authority envelope is persisted in segment records (`Authority` on the carrier remains `[JsonIgnore]`; the additive occurrence-unit carrier is unchanged).

## 7. Full test receipts

| Gate | Command | Result |
| --- | --- | --- |
| Focused replay tests | `dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~S23DurableGraphReopenTests\|FullyQualifiedName~S23PublicationIntegrationTests\|FullyQualifiedName~S23NativeAuthorityTests\|FullyQualifiedName~S23OccurrenceOwnership"` | **55 / 0 / 0** |
| Full solution | `dotnet test Andirit.slnx -c Release --no-restore` | **851 / 0 / 0** (LatticeDb 50, TreeSitter 157, core 345, CSharp 299) |
| NEW-01 regression | `dotnet test docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj -c Release --no-restore` | **17 / 0 / 0** |
| Frozen R2 oracle | `dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -c Release --no-restore -- --corpus … --expected …/oracle-expected-r2.json --output docs/reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-R1-evidence/oracle-r2.json` | **185/185**, 0 failed; **146,851 bytes**; SHA-256 **`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`** (full hash computed and verified from the committed source, matching the recorded `97F6C5E5…39C9`) |
| Fresh-process reopen | two-phase harness (publish pid `31128`, verify pid `22644`) | both exit 0 |
| Release build | `dotnet build Andirit.slnx -c Release --no-restore` | Build succeeded, **0 warnings, 0 errors** |
| Format | `dotnet format Andirit.slnx --verify-no-changes --no-restore` | exit 0 |

Solution delta vs producer baseline: **840 → 851** (+11: 8 new `S23DurableGraphReopenTests` controls; 4 new publication reopen tests; −1 superseded blocker-pinning test, below). No existing test was weakened; the one superseded test pinned the *existence* of the defect this commission was authorized to fix.

**Superseded test (documented, not silently weakened):** `CrossProjectPublication_DurableReopen_IsBlockedByPreExistingGraphOrderingDefect` (producer, `e2b833a`) asserted that cross-project durable reopen *throws* `graph.edge.endpoint-missing`. Its fixture and identity computations are preserved and inverted into the authorized positive test `CrossProjectRealFact_SurvivesDurableReopen_WithCorrectUnitsAndCoverage` (with strictly stronger assertions: graph integrity, availability, hit count, coverage, source unit, defining unit). The supersession is the remediation itself; the producer's historical evidence directory is untouched.

## 8. Commits and trees

| Object | Value |
| --- | --- |
| Starting HEAD/tree | `f8b2b08bbd2db0a551412816705a7971973658bd` / `0cc5ad3a9c045e4250d35a9d666b17f52e2b5879` |
| Implementation commit (production + tests + evidence) | **`e0cef1c94f68f2a24a529e1182c0de5862e383c8`** (tree `f6284492bdc08e786e78ddac4b47e429bacff4be`; parent `f8b2b08`) |
| Reviewer-handoff commit (this report) | recorded in the external handoff; this file's commit is the final verification point |
| Working tree at handoff | clean of tracked modifications after the handoff commit; single worktree; nothing pushed |

## 9. Independent S23-PUB-Q1 reviewer questions

The blocked WP-S23-3A package is now unblocked for independent qualification. Reviewer (≠ implementer) should independently establish:

1. **Fresh-process reopen (positive).** Publish a real cross-project fact publication, then in a separate process reopen and confirm: `Available`, exactly one expected hit, `IsComplete`, correct `SourceUnitId` (App) and durable `DefiningProjectUnitId` (Lib); cross-project graph edges resolve.
2. **Order invariance.** Verify reconstruction is invariant to persisted unit enumeration order — both durable fixtures (referencing-unit-first and defining-unit-first) and a direct `DurableCommandLog.ReplayAsync` permutation; confirm the plugin's unit-id hash convention (`CSharpProjectDiscovery.IndexUnitId`) when reproducing persisted order.
3. **Corruption refusal after complete reconstruction.** Remove a graph owner row / retarget an edge / corrupt a payload on a real durable file; confirm refusal (`graph.edge.endpoint-missing`, `JsonException`/`FormatException`) and that refused reads write nothing.
4. **Stale/missing/incomplete after reopen.** Direct, transitive-only, and options-only defining-context changes refuse (`StalePublication`); missing authority refuses `AnalysisUnavailable`; incomplete universes publish graph-only with no occurrence rows.
5. **Q1-Q1 containment.** Segment resumption without authority still fails closed across store lifetimes; no durable authority envelope in segment records.
6. **Conservation checks.** F-01 orphan-owner refusal across separate reader processes; NEW-01 recovery projection; unknown-outcome quarantine; complete-empty distinction; counters; native mirrors; D3/D4/D5; frozen identities; frozen R2 oracle 185/185 with full-hash verification.
7. **Scope discipline.** Confirm the production diff is exactly `DurableCommandLog.ReplayAsync` + `InMemoryCodeGraphStore.RestorePublishedUnitsAsync`; no public API, schema, persisted-format, occurrence-authority, or batch-contract change; no test weakened (the single superseded blocker test is documented in §7).

## 10. Residual qualifications (unchanged, carried)

Q1-Q2 (no stronger recovery-rebuild atomicity/quarantine claim), S-01 (caller-supplied digests), O-Q2-01 (separate hardening candidate), SQ-03/SQ-04 and real-fact N1–N8 (WP-S23-4/S2.5), Windows-x64-only verification scope (no crash/power-loss/cross-platform/concurrency claim). No WP-S23-3B/4/5, S2.4/S2.5, Stage 6, public Find Usages, or push occurred.
