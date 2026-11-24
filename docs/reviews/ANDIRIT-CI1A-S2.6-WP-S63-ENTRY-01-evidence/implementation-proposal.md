# S6.3 Implementation Proposal — ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Scope**: Minimal technically sound S6.3 change set — documentation + dedicated native publication correctness test suite. No behavioural change, no dependency change, no new public API.
**Mode**: Proposal only — does not authorize implementation. Implementation requires separate bounded authorization (`ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01`).

---

## 1. Change set summary

| # | Change | File(s) | Nature |
| --- | --- | --- | --- |
| 1 | Add S6.3 dedicated native publication correctness test suite covering D4 invariants 1–4, 6–10 and full S-02 matrix | New `tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs` | Test addition (the qualification instrument) |
| 2 | Fix D-05 test logic (call `CompleteIndexRunAsync` before testing concurrent readers) | `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` (D-05) | Test fix (required for P-15 concurrency scenario) |
| 3 | Extend native assertion helpers for occurrence tables (`AndiritOccurrenceMeta`, `AndiritOccurrenceUnit`, `AndiritOccurrencePage`) | `tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs` (helpers) / shared helper | Test infrastructure |
| 4 | Document the publication correctness contract and non-qualified limits in the contract record | `docs/decisions/...` (successor commission record) | Documentation |
| 5 | Record the upstream dependency change that would be required if crash/power-loss interruption is ever demanded | `docs/decisions/...` (successor commission record) | Documentation |
| — | **Not included**: production code changes, dependency upgrade, new public API, new native seam, crash/power-loss simulation, S6.4/S6.5, package publication, tag changes, push | — | — |

---

## 2. Test suite design: `NativePublicationCorrectnessTests.cs`

### 2.1 Structure (mirrors S6.1/S6.2 patterns)

```csharp
namespace Andirit.LatticeDb.Tests;

/// <summary>
/// S6.3 dedicated native publication-correctness regressions (M12.1, M12.3, D4 invariants 1–4, 6–10, S-02).
///
/// The published publication-correctness contract for <see cref="LatticeCodeGraphStore"/> is:
/// - Stale/unbound/unverifiable occurrence index refuses explicitly (D4-1, D4-2)
/// - No unbounded scan on stale index (D4-3)
/// - Failed staging preserves prior publication and index (D4-4)
/// - Superseded publication cannot replace newer generation (D4-6)
/// - Rebuild/reopen produces semantically equivalent index (D4-7)
/// - Contributor union/conflict/deletion updates visibility consistently (D4-8)
/// - Bounded queries have deterministic ordering and explicit limits (D4-9)
/// - Concurrent publication follows snapshot/freshness contract (D4-10)
/// - All exercised against production storage; in-process interleaving is diagnostic only (S-02-7).
///
/// Every case runs against the production store over a real LatticeDB file and asserts the
/// native durable state directly (AndiritOccurrenceMeta/Unit/Page), so a case cannot pass
/// on the managed projection alone. Constructed cases use the production fault-injector seam;
/// genuine cases use concurrent writers/readers and OS-level file-locking serialization.
/// </summary>
public sealed class NativePublicationCorrectnessTests
{
    // Constants, helpers, test cases P-01 through P-16
}
```

### 2.2 Test case matrix (from traceability matrix)

| Case | D4/S-02 | Description | Evidence Class | Key Assertions |
| --- | --- | --- | --- | --- |
| **P-01** | D4-1 | Query refuses stale occurrence index (generation mismatch) | Constructed | `OccurrenceIndexStale` refusal; no results |
| **P-02** | D4-2 | Query refuses unbound/unverifiable index (missing meta) | Constructed | `OccurrenceIndexUnavailable`/`Unverifiable` refusal |
| **P-03** | D4-3 | Stale index does not trigger unbounded scan | Constructed | `Examined` ≤ `Limit + tolerance` (102 for limit 100) |
| **P-04** | D4-4 | Failed staging preserves prior publication + index | Constructed | Prior publication + occurrence index intact after validation failure |
| **P-05** | D4-6 | Superseded publication rejected; newer generation retained | Genuine | Two writers race; only newer generation visible |
| **P-06** | D4-6 | Generation regression (re-publish older run) refused | Constructed | `CompleteIndexRunAsync` for older run refused/no-op |
| **P-07** | D4-7 | Ordinary reopen produces equivalent query results | Genuine | Identical occurrence facts/coverage/publication before/after reopen |
| **P-08** | D4-7 | Full rebuild/reopen produces equivalent index | Genuine | `RebuildInnerFromDurableStateAsync` path; identical results |
| **P-09** | D4-8 | Contributor union: identical facts merged; conflict rejected | Constructed | Union merges; `OccurrenceConflictException` retains first |
| **P-10** | D4-8 | Scoped deletion removes only affected target context | Constructed | Deleted target's facts/pages gone; others unaffected |
| **P-11** | D4-9 | Bounded query: deterministic site order, explicit limit | Constructed | Order: Path → Line → Column → OccurrenceId; count ≤ limit |
| **P-12** | D4-10 | Concurrent publication: queries see atomic snapshot | Genuine | Reader sees either old or new publication; no mixed state |
| **P-13** | S-02-4 | Supersession stress: rapid sequential publications | Genuine | Only latest accepted; generation regression refused |
| **P-14** | S-02-5 | Unknown-outcome quarantine + reopen corroboration (occurrence) | Constructed | `CodeGraphCommitOutcomeUnknownException` → reopen verifies |
| **P-15** | S-02-6 | Native concurrency: multi-process writer race (fixed D-05) | Genuine | First writer commits; others fail/wait; state consistent |
| **P-16** | S-02-7 | In-process interleaving recorded as diagnostic only | Documentation | `QueryInterleavingHook` test-only; not native evidence |

### 2.3 Helper methods (reused/extended from S6.1/S6.2)

```csharp
// Native occurrence state assertion
private static IReadOnlyList<string> NativeOccurrenceMeta(string path) { ... }
private static IReadOnlyList<string> NativeOccurrenceUnits(string path) { ... }
private static IReadOnlyList<string> NativeOccurrencePages(string path) { ... }

// Publication state
private static async Task<CodeGraphPublication?> GetLatestPublicationNative(string path, string repositoryId) { ... }

// Occurrence index query with explicit limit
private static async Task<CodeOccurrenceQueryResult> QueryOccurrencesNative(string path, CodeOccurrenceQuery query) { ... }

// Fault-injector for supersession/reopen
Action<string> injector = point => { ... }; // "before-commit", "after-commit", optionally "before-publication", "after-publication"

// Multi-process worker (fixed D-05 logic)
static async Task<int> WorkerMain(string[] args) {
    // ... seed ...
    await store.StageIndexUnitAsync(unit);
    await store.CompleteIndexRunAsync(completedRun, state); // FIX: was missing
    // ... wait for kill ...
}
```

### 2.4 Constructed vs. genuine classification

| Constructed (deterministic, fault-injector) | Genuine (arbitrary arrival, no seam control) |
| --- | --- |
| P-01, P-02, P-03, P-04, P-06, P-09, P-10, P-11, P-14, P-16 | P-05, P-07, P-08, P-12, P-13, P-15 |

---

## 3. D-05 fix (required for P-15)

**Current defect** (S6.2 `NativeDurabilityTests.cs:262–308`):
```csharp
await writer.StageIndexUnitAsync(Unit(...));
// MISSING: await writer.CompleteIndexRunAsync(...);
```

**Fix**: Add the missing `CompleteIndexRunAsync` call before readers verify:
```csharp
await writer.StageIndexUnitAsync(Unit(repository, run, "unit:writer", "node:writer"));
await writer.CompleteIndexRunAsync(
    new(repository, run, Started, CodeIndexRunStatus.Completed, Started.AddSeconds(1), new[] { Plugin }),
    new(repository, run, new[] { new CodeSourceManifest(Plugin, "1.0.0", "src/Writer.cs", "sha256:writer") }));
```

This publishes the staged unit, making it visible to subsequent readers. The independent verification in S6.2 §4.3 already confirmed this works.

---

## 4. Documentation changes

| Document | Change |
| --- | --- |
| `LatticeCodeGraphStore.cs` (class remarks) | No change needed — contract already covers D4 invariants, publication lifecycle, concurrency, unknown-outcome, limitations |
| `ICodeGraphStore.cs` (interface remarks) | No change needed — contract published at API boundary |
| Successor commission record (`ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` or acceptance record) | Record: publication correctness contract, non-qualified limits (crash/power-loss, osx-arm64), upstream dependency change required for interruption |
| Evidence package documentation | Self-excluding SHA-256 manifest, reproduction instructions |

---

## 5. Production equivalence verification (pre/post implementation)

| Check | Baseline (S6.2 acceptance `359a9e8`) | Post-S6.3 Implementation | Requirement |
| --- | --- | --- | --- |
| `Andirit.Abstractions.dll` metadata/IL | Identical | Identical | Byte-exact except provenance |
| `Andirit.dll` metadata/IL | Identical | Identical | Byte-exact except provenance |
| `Andirit.LatticeDb.dll` metadata/IL | Identical | Identical | Byte-exact except provenance |
| Public API surface | 2,629 lines | 2,629 lines | 0 differences |
| Package content (except nuspec commit stamp, PE debug) | Identical | Identical | Byte-region analysis: only MVID/PDB/timestamp |
| Historical test suites | All pass | All pass | No regression |
| Coverage thresholds | 55/75/60/70 | 55/75/60/70 | Unchanged |
| CI gates (3 platforms) | Green | Green | Unchanged |

---

## 6. Evidence package structure

```
docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/
├── manifest.json                 # Self-excluding SHA-256 manifest
├── .gitattributes                # * -text / **/* -text
├── .gitignore                    # !* / !**/
├── contract-reconciliation.txt   # Byte-exact contract excerpts
├── preflight.txt                 # Preflight receipt (Git, CI, tag, lineage)
├── test-receipts/                # Raw test output per case (Windows + Linux)
│   ├── P-01-win.txt
│   ├── P-01-linux.txt
│   └── ...
├── native-abi-scan.txt           # Native ABI scan (unchanged from S6.1/S6.2)
├── api-surface.log               # Public API surface dump
├── package-receipt.txt           # Package content analysis
├── probe-reproduction.md         # How to reproduce
└── evidence-summary.md           # Summary table
```

---

## 7. Implementation boundary (what is explicitly excluded)

| Excluded | Reason |
| --- | --- |
| Any production code change in `LatticeCodeGraphStore` | Not needed — existing implementation satisfies all D4/S-02 |
| `LatticeDbSharp` dependency upgrade | Pinned at 0.2.0; no upgrade authorized |
| New public API surface | Qualification uses existing interfaces |
| New native seam in production code | Fault-injector already sufficient |
| Crash/power-loss/abrupt-kill simulation | Not qualified (Q-S62-1); managed fault injector ≠ crash |
| Multi-process durability beyond file-locking serialization | S6.2 D-07 covers genuine abrupt termination |
| D3 full real-fixture corpus | S6.4 |
| Platform certification matrix | S6.5 |
| Optional cross-target / public FindUsages / optimization | Unauthorized (DF-7) |
| Package publication, tag changes, push | Not authorized |

---

## 8. Success criteria for implementation commission

The implementation commission `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` succeeds iff:

1. **All 16 test cases (P-01…P-16) pass** on Windows x64 (local) and genuine hosted `ubuntu-latest` (GitHub Actions).
2. **D-05 fixed** — multi-process concurrency test passes with `CompleteIndexRunAsync` called.
3. **Zero production code changes** — `git diff --name-only` shows only test files and documentation.
4. **Whole solution builds and tests clean** — `dotnet build` + `dotnet test` on all 3 platforms.
5. **Historical suites unchanged** — Core 345, CSharp 372, TreeSitter 157, LatticeDb 88+ all pass.
6. **Coverage thresholds met** — 55/75/60/70 unchanged.
7. **Evidence package sealed** — self-excluding SHA-256 manifest, byte-exact lineage extended.
8. **Production equivalence verified** — metadata/IL/package identical except provenance stamps.
9. **No push performed** — commits local only.

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`
**Next commission**: `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` (pending owner approval)