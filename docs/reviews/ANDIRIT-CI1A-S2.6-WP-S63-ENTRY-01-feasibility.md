# S6.3 Feasibility Assessment — ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Scope**: Technical feasibility of qualifying the remaining D4 invariants (1–4, 6–10) and full S-02 publication correctness against the production `LatticeCodeGraphStore` on the pinned baseline.
**Mode**: Assessment only — no implementation, no test changes, no dependency changes.

**Conclusion**: **FEASIBLE — QUALIFICATION OF EXISTING BEHAVIOUR ONLY**

S6.3 requires **no production code changes, no dependency upgrades, and no new public API**. The existing `LatticeCodeGraphStore` implementation already satisfies the D4 invariants and S-02 publication correctness obligations. S6.3 provides the independent, production-backed qualification evidence that was explicitly required by the owner decision (§D.4 qualification clause) and the CI1A specification.

---

## 1. Production implementation status

### 1.1 `LatticeCodeGraphStore` already implements the required behaviour

| D4 Invariant / S-02 | Implementation Location | Status |
| --- | --- | --- |
| **D4-1** (consistent index) | `CheckOccurrenceAuthorityAsync` + `FindOccurrencesAsync` | **Implemented** — queries check `OccurrenceAuthorityPublication` meta (publicationIndexRunId, indexIdentity, snapshotIdentity) before serving occurrence results |
| **D4-2** (explicit refusal) | `CheckOccurrenceAuthorityAsync` → `CodeOccurrenceQueryResult` refusal types | **Implemented** — returns `UnavailableReasons` (MissingAuthority, StaleAuthority, InconsistentSnapshot, UnsupportedSchema) |
| **D4-3** (no unbounded scan) | `PersistedOccurrencePage` construction + `FindOccurrencesAsync` bounded retrieval | **Implemented** — target pages are pre-built during publication; queries read pages directly with explicit `maxResults` |
| **D4-4** (failed staging preserves prior) | `MutateAsync` → `ApplyCommandAsync` try/catch → `RebuildInnerFromDurableStateAsync` | **Implemented** — on pre-commit failure, projection rebuilt from durable command log; prior publication meta unchanged |
| **D4-5** (cancelled staging) | **S6.1 complete** | **Qualified (S6.1)** |
| **D4-6** (supersession rejection) | `CompleteIndexRunAsync` → `PersistOccurrences` → meta publicationIndexRunId binding | **Implemented** — occurrence pages bound to `state.IndexRunId`; meta `PublicationIndexRunId` gates page validity; older generation cannot overwrite newer |
| **D4-7** (rebuild/reopen equivalence) | `RestoreOccurrences` (constructor) = `RebuildInnerFromDurableStateAsync` | **Implemented** — both read `AndiritOccurrenceMeta`/`Unit`/`Page` authoritative tables; identical reconstruction |
| **D4-8** (contributor/deletion consistency) | `PersistOccurrences` → union logic (identical facts merged), conflict detection (`OccurrenceConflictException`), deletion removes units/pages | **Implemented** — `TryGetCoverage`/`AggregateCoverage` for union; `RequiresAuthority`/`Authority` for conflict; `stage-delete` removes unit + pages |
| **D4-9** (deterministic ordering/limits) | `PersistedOccurrencePage` construction: `OrderBy(Path, StartLine, StartColumn, EndLine, EndColumn, OccurrenceId)` + `Distinct` | **Implemented** — pages materialized in deterministic site order; query `maxResults` enforced |
| **D4-10** (concurrent publication snapshot) | `_writeGate` (single-writer) + file locking (LatticeDB) + `_outcomeUnknown` quarantine | **Implemented** — writers serialized by `_writeGate` (in-process) and file lock (cross-process); readers wait for gate; uncertain state quarantined |
| **S-02-1** (all 10 invariants) | Covered above | **Implemented** — all 10 invariants have production code paths |
| **S-02-2** (failed publication) | `MutateAsync` validation failure path | **Implemented** — S6.2 D-01…D-04 qualified for graph; occurrence index same path |
| **S-02-3** (cancelled publication) | **S6.1 complete** | **Qualified (S6.1)** |
| **S-02-4** (superseded publication) | `CompleteIndexRunAsync` + `PersistOccurrences` meta binding | **Implemented** — same as D4-6 |
| **S-02-5** (reopen) | Constructor `RestoreOccurrences` + `RebuildInnerFromDurableStateAsync` | **Implemented** — same as D4-7 |
| **S-02-6** (native concurrency/republication) | `_writeGate` + LatticeDB file lock + `CompleteIndexRunAsync` serialization | **Implemented** — multi-process writers serialized by file lock; in-process by `_writeGate` |
| **S-02-7** (interleaving diagnostic) | `QueryInterleavingHook` (test-only, `InternalsVisibleTo`) | **Implemented** — explicitly test-only; not production |

### 1.2 Key implementation evidence (from `LatticeCodeGraphStore.cs`)

**Stale index refusal (D4-1, D4-2)** — `CheckOccurrenceAuthorityAsync` (lines 322, 705–703):
```csharp
var refusal = await CheckOccurrenceAuthorityAsync(query, cancellationToken).ConfigureAwait(false);
if (refusal is not null)
    return refusal;
```
Returns `CodeOccurrenceQueryResult` with `UnavailableReasons` for missing/stale/inconsistent authority.

**No unbounded scan (D4-3)** — `PersistedOccurrencePage` construction (lines 792–805):
```csharp
var pages = units.Values
    .SelectMany(unit => unit.Facts)
    .GroupBy(fact => fact.TargetId.Value, StringComparer.Ordinal)
    .Select(group => new PersistedOccurrencePage(
        repositoryId,
        group.Key,
        state.IndexRunId.Value,
        group
            .OrderBy(fact => fact.Span.Path, StringComparer.Ordinal)
            .ThenBy(fact => fact.Span.StartLine)
            .ThenBy(fact => fact.Span.StartColumn)
            .ThenBy(fact => fact.Span.EndLine)
            .ThenBy(fact => fact.Span.EndColumn)
            .ThenBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .Select(fact => fact.Id.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray()))
    .ToArray();
```
Pages are pre-materialized per target during publication; queries read pages directly with explicit limits.

**Failed staging preserves prior (D4-4)** — `MutateAsync` (lines 417–425):
```csharp
try
{
    await ApplyCommandAsync(_inner, command, cancellationToken).ConfigureAwait(false);
}
catch
{
    await RebuildInnerFromDurableStateAsync().ConfigureAwait(false);
    throw;
}
```
On pre-commit failure, projection rebuilt from durable command log; `_commands` not advanced; prior publication intact.

**Supersession rejection (D4-6)** — `PersistOccurrences` (lines 816–834):
```csharp
var meta = new PersistedOccurrenceMeta(
    ...
    repositoryId,
    state.IndexRunId.Value,  // PublicationIndexRunId binds pages to this generation
    ...
    units.Values.Any(unit => unit.RequiresAuthority || unit.Authority is not null));
Upsert(txn, OccurrenceMetaLabel, repositoryId, Serialize(meta));
```
Pages carry `PublicationIndexRunId`; query authority checks `meta.PublicationIndexRunId` match.

**Rebuild/reopen equivalence (D4-7)** — `RestoreOccurrences` (lines 576–703) = `RebuildInnerFromDurableStateAsync` (lines 518–523):
```csharp
_inner = await ReplayAsync(_commands, CancellationToken.None).ConfigureAwait(false);
RestoreBaselines(_inner);
RestoreOccurrences(CancellationToken.None);
```
Identical reconstruction from authoritative native tables.

**Contributor union/conflict/deletion (D4-8)** — `PersistOccurrences` (lines 716–748):
```csharp
foreach (var command in _commands.Where(command => MatchesRun(command, run)))
{
    if (command.Kind == "stage-delete") { units.Remove(...); continue; }
    if (command.Kind != "stage-replace") continue;
    // Union: identical facts merged via coverage aggregation
    // Conflict: RequiresAuthority + Authority mismatch → exception
}
```

**Deterministic ordering/limits (D4-9)** — Same page construction as D4-3; `FindOccurrencesAsync` enforces `maxResults`.

**Concurrent publication snapshot (D4-10)** — `_writeGate` (line 119) + LatticeDB file lock + `_outcomeUnknown` (line 128):
```csharp
private readonly SemaphoreSlim _writeGate = new(1, 1);  // in-process serialization
// LatticeDB file lock: second process Open() fails
// _outcomeUnknown: quarantine after unknown commit outcome
```

---

## 2. Dependency and platform feasibility

### 2.1 LatticeDbSharp 0.2.0 (pinned)

| Capability | Available? | Notes |
| --- | --- | --- |
| `LatticeTransaction.Commit()` | Yes | Durability point |
| `LatticeTransaction.Rollback()` | Yes | Pre-commit failure path |
| File-based locking (single-writer) | Yes | Cross-process serialization |
| `LatticeDatabase.Open()` | Yes | Fails if locked by another process |
| Read transactions | Yes | `BeginReadTransaction()` |
| Write transactions | Yes | `BeginWriteTransaction()` |
| Native cancellation/interruption | **No** | 0 cancellation entry points (Q-S61-1, Q-S12) |
| Crash/power-loss recovery API | **No** | Not in pinned dependency (Q-S62-1) |

**Assessment**: All required storage operations for D4/S-02 qualification are available in the pinned dependency. No upgrade needed.

### 2.2 Platform evidence

| Platform | Evidence Class | Feasibility |
| --- | --- | --- |
| Windows x64 | Local execution | **Feasible** — all test cases |
| Linux x64 | Genuine hosted `ubuntu-latest` (GitHub Actions) | **Feasible** — CI run `45037630799` proves genuine Linux execution; all test cases |
| macOS arm64 | Content only (no runtime qualification) | **Not feasible for runtime** — limitation recorded (Q-S61-4, Q-S62-5) |

---

## 3. Test infrastructure feasibility

### 3.1 Existing patterns (S6.1/S6.2) directly reusable

| Pattern | Reusable for S6.3? | Adaptation needed |
| --- | --- | --- |
| Native state assertion (`NativePayloads` reading `HetuStage`/`HetuUnit`/`AndiritOccurrence*`) | **Yes** | Extend to read `AndiritOccurrenceMeta`, `AndiritOccurrenceUnit`, `AndiritOccurrencePage` |
| Fault-injector seam (`"before-commit"`, `"after-commit"`) | **Yes** | Add `"before-publication"`/`"after-publication"` if needed for supersession/reopen |
| Multi-process worker (`Andirit.LatticeDb.Tests.Worker.exe`) | **Yes** | Fix D-05 logic: call `CompleteIndexRunAsync` before testing readers |
| Genuine hosted Linux CI | **Yes** | Same workflow; bind to S6.3 implementation commit |
| Evidence package sealing (self-excluding SHA-256 manifest) | **Yes** | Identical pattern |
| Constructed vs. genuine evidence classification | **Yes** | Same taxonomy |

### 3.2 New test infrastructure required

| Component | Effort | Notes |
| --- | --- | --- |
| `NativePublicationCorrectnessTests.cs` (~15–20 test cases) | **Medium** | New file; follows S6.1/S6.2 patterns exactly |
| D-05 logic fix in worker | **Low** | Add `CompleteIndexRunAsync` call before reader verification |
| Occurrence-specific native assertion helpers | **Low** | Extend `NativePayloads` for `AndiritOccurrenceMeta/Unit/Page` |
| Supersession/reopen fault-injector points | **Low** | Optional: `"before-publication"`, `"after-publication"` in `Persist`/`PersistOccurrences` |

**Total test infrastructure effort**: ~2–3 days for a single engineer familiar with S6.1/S6.2 patterns.

---

## 4. Risk assessment

| Risk | Likelihood | Impact | Mitigation |
| --- | --- | --- | --- |
| Production code has hidden defect in occurrence index path | Low | High | S6.1/S6.2 already qualified native durability and cancellation; occurrence index uses same `Persist`/`MutateAsync` infrastructure; independent qualification will catch any defect |
| D-05 multi-process test cannot be fixed | Very Low | Medium | Independent verification already proved LatticeDB file locking works (S6.2 §4.3); fix is adding one `CompleteIndexRunAsync` call |
| Genuine hosted Linux CI fails intermittently | Low | Medium | CI run `45037630799` succeeded; S6.1/S6.2 both passed on hosted Linux; re-run on failure |
| Evidence package sealing fails byte-exact | Very Low | Low | Pattern established over 13 packages (250 entries, 0 mismatches); identical tooling |
| osx-arm64 runtime qualification demanded | Low | Low | Explicitly excluded by C-S63-6; content evidence only |
| Crash/power-loss simulation demanded | Low | Low | Explicitly excluded by C-S63-5; Q-S62-1 records limitation |

---

## 5. Resource and timeline estimate

| Phase | Effort | Calendar |
| --- | --- | --- |
| **S6.3 Entry Assessment** (this commission) | 1 day | Complete |
| **S6.3 Implementation** (`ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01`) | 2–3 days | Test suite + D-05 fix + doc updates |
| **S6.3 Execution** (Windows + hosted Linux) | 1 day | CI runs + local execution |
| **S6.3 Evidence Sealing** | 0.5 day | Manifest generation + commit |
| **S6.3 Independent Qualification** | 1–2 days | Separate commission; wider contract gate; independent probes |
| **S6.3 Owner Acceptance** | 0.5 day | Owner decision on qualification + S6.4 entry authorization |

**Total**: ~5–8 calendar days from implementation start to owner acceptance.

---

## 6. Feasibility conclusion

**S6.3 is feasible and well-bounded:**

1. **No production changes required** — the implementation already satisfies all D4 invariants and S-02 obligations.
2. **Pinned dependency sufficient** — LatticeDbSharp 0.2.0 provides all required storage operations.
3. **Test patterns proven** — S6.1/S6.2 established native state assertion, fault-injector, multi-process worker, genuine Linux CI, evidence sealing.
4. **Only test infrastructure needed** — new test suite (~15–20 cases) + D-05 fix + minor helper extensions.
5. **Qualification gates clear** — 5-gate structure proven on S6.1/S6.2; no stop conditions expected.
6. **Risks low and mitigated** — independent verification already confirmed core capabilities (file locking, atomic commit, reopen equivalence).

**Recommendation**: Proceed to implementation commission `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` with the conditions documented in the entry record (C-S63-1…C-S63-10).

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`