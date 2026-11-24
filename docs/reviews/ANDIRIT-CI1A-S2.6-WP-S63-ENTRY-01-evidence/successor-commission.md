# Proposed Successor Commission — ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01

**Date**: 2026-11-24
**Status**: PROPOSAL ONLY — does not authorize implementation. Requires explicit owner approval.
**Parent commission**: `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` (this entry assessment)
**Mode**: Implementation bounded to test suite + documentation + evidence sealing. No production code changes, no dependency changes, no new public API, no push.

---

## 1. Commission identity

| Item | Value |
| --- | --- |
| Commission ID | `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` |
| Parent | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` |
| Authorized scope | S6.3 implementation: add native publication correctness test suite, fix D-05 test logic, execute on Windows + genuine hosted Linux, seal evidence |
| Baseline | `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (S6.3 entry HEAD) |
| Implementation authorization | **Pending owner approval** |
| Push authorization | **Not granted** — commits local only |
| Tag `v0.2.0-preview.7` | Unchanged |
| LatticeDbSharp | 0.2.0 pinned (no upgrade) |

---

## 2. Objectives (exactly)

1. **Add `NativePublicationCorrectnessTests.cs`** — dedicated test suite covering P-01…P-16 (D4 invariants 1–4, 6–10; S-02 matrix).
2. **Fix D-05 test logic** — add missing `CompleteIndexRunAsync` call in `NativeDurabilityTests.cs` for genuine multi-process concurrency evidence.
3. **Extend native assertion helpers** — read `AndiritOccurrenceMeta`, `AndiritOccurrenceUnit`, `AndiritOccurrencePage` directly.
4. **Execute on Windows x64 (local) and genuine hosted `ubuntu-latest` (GitHub Actions)** — all 16 cases pass on both platforms.
5. **Seal evidence package** — self-excluding SHA-256 manifest, byte-exact lineage extension.
6. **Submit for independent qualification** — separate commission `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01`.

---

## 3. Deliverables

| Deliverable | Document/Artifact |
| --- | --- |
| Implementation record | `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01.md` |
| Test suite | `tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs` |
| D-05 fix | `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` (modified) |
| Native assertion helpers | Extended in `NativePublicationCorrectnessTests.cs` / shared |
| Evidence package | `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/` (self-excluding SHA-256 manifest) |
| Production equivalence receipt | Metadata/IL/package comparison vs baseline |
| Next commission proposal | `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` (qualification) |

---

## 4. Bounded scope (what this commission does NOT do)

| Excluded | Reason |
| --- | --- |
| Any production code change | Not needed — existing implementation satisfies D4/S-02 |
| `LatticeDbSharp` dependency upgrade | Pinned at 0.2.0; no upgrade authorized |
| New public API surface | Qualification uses existing interfaces |
| New native seam in production code | Fault-injector already sufficient |
| Crash/power-loss/abrupt-kill simulation | Not qualified (Q-S62-1); managed fault injector ≠ crash |
| Multi-process durability beyond file-locking serialization | S6.2 D-07 covers genuine abrupt termination |
| D3 full real-fixture corpus | S6.4 |
| Platform certification matrix | S6.5 |
| Optional cross-target / public FindUsages / optimization | Unauthorized (DF-7) |
| Package publication, tag changes, push | Not authorized |
| Historical findings closure | No acceptance threshold changes; no sealed evidence modification |

---

## 5. Acceptance criteria (this commission succeeds iff)

1. **All 16 test cases (P-01…P-16) pass** on Windows x64 (local) and genuine hosted `ubuntu-latest` (GitHub Actions).
2. **D-05 fixed** — multi-process concurrency test passes with `CompleteIndexRunAsync` called before reader verification.
3. **Zero production code changes** — `git diff --name-only` shows only:
   - `tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs` (new)
   - `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` (D-05 fix only)
   - Documentation files (commission record, evidence package)
4. **Whole solution builds and tests clean** — `dotnet build` + `dotnet test` on Windows, Linux, macOS.
5. **Historical suites unchanged** — Core 345, CSharp 372, TreeSitter 157, LatticeDb 88+ all pass.
6. **Coverage thresholds met** — 55/75/60/70 unchanged.
7. **Evidence package sealed** — self-excluding SHA-256 manifest, byte-exact lineage extended to 14 packages.
8. **Production equivalence verified** — metadata/IL/package identical except provenance stamps (nuspec commit, PE debug).
9. **No push performed** — commits local only; `origin/main` unchanged at `01624943233f16447c3176b7c905c544642dfb2c`.
10. **Tag `v0.2.0-preview.7` unchanged** — object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183`.

---

## 6. Implementation steps (procedural)

### Step 1: Create test suite
```bash
# Create new test file following S6.1/S6.2 patterns
# tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs
# - P-01 through P-16
# - Native occurrence assertion helpers (Meta, Unit, Page)
# - Fault-injector for supersession/reopen
# - Multi-process worker (reuse S6.2 pattern with D-05 fix)
```

### Step 2: Fix D-05
```bash
# Edit tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs
# In D-05 (ConcurrentMultiProcessAccess_FileLockingAndConsistency):
# Add CompleteIndexRunAsync call after StageIndexUnitAsync in writer block
# Remove [Fact(Skip = "...")] attribute
```

### Step 3: Local execution (Windows x64)
```bash
dotnet build -c Release --no-restore
dotnet test -c Release --no-build --filter "FullyQualifiedName~NativePublicationCorrectnessTests"
dotnet test -c Release --no-build --filter "FullyQualifiedName~NativeDurabilityTests.ConcurrentMultiProcessAccess"
# Verify all pass
```

### Step 4: Push to trigger CI (if authorized) OR local Linux simulation
**Note**: Push not authorized by this commission. CI execution requires separate push authorization or manual workflow dispatch.
Alternative: Document that CI execution on `ubuntu-latest` is required for qualification gate G-2.

### Step 5: Evidence package sealing
```bash
# Create docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/
# - manifest.json (self-excluding SHA-256)
# - .gitattributes, .gitignore
# - contract-reconciliation.txt
# - preflight.txt
# - test-receipts/ (Windows + Linux if available)
# - api-surface.log, package-receipt.txt
# - evidence-summary.md
# Commit locally
```

### Step 6: Production equivalence verification
```bash
# Rebuild packages at implementation HEAD
# Compare metadata/IL/package vs S6.2 acceptance baseline (359a9e8)
# Verify only provenance differences
```

### Step 7: Record implementation commission
```bash
# Write ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01.md
# Commit locally
# Propose ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01 as next commission
```

---

## 7. Risk mitigation

| Risk | Mitigation |
| --- | --- |
| D-05 fix doesn't work on Linux | Independent verification in S6.2 §4.3 already confirmed LatticeDB file locking works; fix is one line |
| Test suite has flaky genuine cases | Run each genuine case 3× (P-05, P-07, P-08, P-12, P-13, P-15); record all runs |
| CI `ubuntu-latest` unavailable | GitHub Actions `ubuntu-latest` is standard; S6.1/S6.2 both succeeded |
| Evidence package not byte-exact | Pattern established over 13 packages (250 entries, 0 mismatches); identical tooling |
| Coverage threshold breach | New tests only add coverage; historical thresholds unchanged |

---

## 8. Qualifications carried forward (unchanged)

| ID | Qualification | S6.3 handling |
| --- | --- | --- |
| Q-S61-1 | Active native execution not interruptible | Carried forward; S6.3 does not simulate interruption |
| Q-S61-2 | Native window non-cancellable, must be awaited | Carried forward |
| Q-S61-3 | D4 invariant 5 discharged by atomicity | Carried forward |
| Q-S61-4 | osx-arm64 unqualified (content only) | Carried forward; S6.3 no macOS runtime |
| Q-S61-5 | Linux evidence must be genuine hosted | **Enforced** for all S6.3 test cases |
| Q-S61-6 | Cancellation surface platform-invariant | Carried forward |
| Q-S61-7 | Scope remains qualification of existing behavior | **S6.3 = qualification only** |
| Q-S61-8 | F-02 durability boundary untouched | Carried forward |
| Q-S62-1 | Crash/power-loss/abrupt-kill/multi-process not qualified | Carried forward; S6.3 does not simulate |
| Q-S62-2 | DF-4 managed fault ≠ crash | Carried forward |
| Q-S62-3 | M12.1/M12.3, full S-02/D4.5 deferred to S6.3 | **S6.3 completes** |
| Q-S62-4 | osx-arm64 unqualified | Carried forward |
| Q-S62-5 | DF-4 managed fault ≠ crash | Carried forward |
| Q-S62-6 | Pre-existing 4-file lineage gap | Carried forward |
| F-QU-01 | D-05 test logic bug | **Fixed in this commission** |
| F-QU-02 | D-05 multi-process genuine evidence | **Provided by P-15** |
| F-QU-03…F-QU-08 | Other S6.2 findings | Carried forward |

---

## 9. Next commissions (after this one)

| Commission | Purpose | Gate |
| --- | --- | --- |
| `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` | Independent qualification of S6.3 | G-1…G-5 pass |
| `ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01` | Owner acceptance of S6.3 | G-6 granted |
| `ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01` | S6.4 entry (D3 real-fixture corpus) | Consumes S6.1/S6.2/S6.3 qualifiers |

---

## 10. Owner approval required

This commission **requires explicit owner approval** before any implementation begins. The owner must confirm:

- [ ] S6.3 entry assessment (`ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01`) reviewed and accepted
- [ ] Implementation scope (test suite + D-05 fix + documentation only) approved
- [ ] No production code changes, no dependency upgrade, no push authorized
- [ ] Conditions C-S63-1 through C-S63-10 from entry assessment acknowledged
- [ ] Next commission (`ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01`) proposed as successor

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`
**Parent**: `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01`
**Status**: PROPOSAL — pending owner approval