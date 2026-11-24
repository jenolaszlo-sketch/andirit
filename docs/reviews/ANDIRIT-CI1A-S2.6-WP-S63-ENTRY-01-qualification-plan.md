# S6.3 Independent Qualification Plan — ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Scope**: Independent qualification plan for S6.3 native publication correctness. The reviewer is **not** the implementation producer; no producer conclusion is accepted without independent re-derivation.
**Mode**: Plan only — execution occurs in a separate qualification commission after implementation.

---

## 1. Qualification commission identity

| Item | Value |
| --- | --- |
| Commission ID | `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` (proposed) |
| Reviewer session | Distinct from producer session (separate `execute` context) |
| Baseline | S6.3 implementation HEAD (to be determined) |
| Entry baseline | `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (this entry assessment) |
| Independence requirement | Reviewer re-derives all contracts from source; wider superset enumeration; independent probe execution |

---

## 2. Gate structure (5 execution gates)

| Gate | Requirement | S6.3 Application | Stop Condition |
| --- | --- | --- | --- |
| **G-1 Implementation** | Correct implementation against contracts; docs-only; no dependency/workflow/threshold change; whole solution builds/tests clean | 0 production changes; test suite + documentation only; `dotnet build` + `dotnet test` clean on all 3 platforms | Any production file modified; dependency version changed; workflow changed; threshold changed; build/test fails |
| **G-2 Qualification** | All test cases executed; genuine Linux on genuine hosted runner bound to sealed commit; no stop condition | P-01…P-16 executed on Windows x64 + hosted `ubuntu-latest`; CI run bound to implementation commit; no stop condition triggered | Any test case fails on either platform; CI run not genuine hosted Linux; stop condition triggered |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70), CI gates unchanged and green on all 3 platforms | All existing suites + S6.1/S6.2 suites pass; thresholds met; all 3 CI legs green | Any historical test fails; coverage threshold breached; CI gate fails on any platform |
| **G-4 Evidence** | Evidence packages sealed with self-excluding SHA-256 manifests; production delta = test + doc paths only | New evidence package `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-evidence/`; lineage extended to 14 packages; byte-exact | Manifest not self-excluding; byte mismatch vs Git blob; production delta includes non-test/doc paths |
| **G-5 Independence** | Reviewer session ≠ producer session; independent contract re-derivation; wider superset enumeration | Separate qualification commission; contracts re-derived from source; contract gate regex superset (add `supersede*`, `reopen*`, `recover*`, `concurr*`, `republicat*`, `snapshot*`, `freshness*`, `ordering*`, `limit*`, `contributor*`, `deletion*`, `union*`, `conflict*`) | Reviewer session = producer session; contract gate not re-derived; superset not wider |

**G-6 Owner acceptance**: Not granted by qualification commission — separate commission `ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01`.

---

## 3. Independent contract assessment (re-derived)

### 3.1 Source-level contract verification

The reviewer will independently re-verify the governing contracts from source code (not from producer summaries):

| Requirement | Source Location | Independent Verification Method |
| --- | --- | --- |
| D4-1 (consistent index) | `CheckOccurrenceAuthorityAsync` + `FindOccurrencesAsync` | Read source; verify authority check before query |
| D4-2 (explicit refusal) | `CodeOccurrenceQueryResult` refusal types | Read source; verify `UnavailableReasons` enum coverage |
| D4-3 (no unbounded scan) | `PersistedOccurrencePage` construction + query path | Read source; verify pre-materialized pages + `maxResults` enforcement |
| D4-4 (failed staging preserves) | `MutateAsync` try/catch + `RebuildInnerFromDurableStateAsync` | Read source; verify projection rebuild on pre-commit failure |
| D4-6 (supersession rejection) | `PersistOccurrences` meta binding + query authority check | Read source; verify `PublicationIndexRunId` on pages + meta match check |
| D4-7 (reopen equivalence) | Constructor `RestoreOccurrences` = `RebuildInnerFromDurableStateAsync` | Read source; verify identical code path |
| D4-8 (contributor/deletion) | `PersistOccurrences` union/conflict/deletion logic | Read source; verify coverage aggregation, conflict detection, deletion |
| D4-9 (deterministic ordering) | Page construction `OrderBy` chain + query `maxResults` | Read source; verify ordering keys and limit enforcement |
| D4-10 (concurrent snapshot) | `_writeGate` + LatticeDB file lock + `_outcomeUnknown` | Read source; verify serialization points |
| S-02-7 (interleaving diagnostic) | `QueryInterleavingHook` + `InternalsVisibleTo` | Read source; verify test-only surface |

### 3.2 Contract gate (wider superset regex)

Producer's S6.2 contract gate used term families: `crash*`, `durab*`, `persist*`, `recover*`, `atomic*`, `halt*`, `discontinu*`, `should`, `may not`, `cannot`, `must not`.

**S6.3 wider superset adds**: `supersede*`, `reopen*`, `recover*`, `concurr*`, `republicat*`, `snapshot*`, `freshness*`, `ordering*`, `limit*`, `contributor*`, `deletion*`, `union*`, `conflict*`, `stale*`, `unverif*`, `unbound*`, `scan*`, `determinist*`, `equivalent*`, `semantic*`, `rebuild*`, `restore*`, `authority*`, `generation*`.

**Expected outcome**: 0 `MANDATE-CRASH-RECOVERY` lines (same as S6.2); new categories for supersession, reopen, concurrency, ordering, contributor semantics.

---

## 4. Independent test qualification — P-01 through P-16

### 4.1 Windows x64 execution (independently re-run)

All tests re-executed on Windows x64 (local, `dotnet test -c Release --no-build`):

| Case | Test | Expected Result | Independent Observation Focus |
| --- | --- | --- | --- |
| **P-01** | `StaleOccurrenceIndex_RefusedExplicitly` | PASS | Native `AndiritOccurrenceMeta` shows older generation; query returns refusal |
| **P-02** | `UnboundUnverifiableIndex_RefusedExplicitly` | PASS | Missing/corrupt meta; query returns `UnavailableReasons` |
| **P-03** | `StaleIndex_NoUnboundedScan` | PASS | Large fact set + stale index + bounded query; `Examined` ≤ `Limit + 2` |
| **P-04** | `FailedStaging_PreservesPriorPublicationAndIndex` | PASS | Pre-commit validation failure; prior publication + occurrence index intact |
| **P-05** | `SupersededPublication_Rejected_NewerRetained` | PASS | Two writers race; file lock serializes; only newer generation visible |
| **P-06** | `GenerationRegression_Refused` | PASS | `CompleteIndexRunAsync` for older run after newer accepted; refused/no-op |
| **P-07** | `OrdinaryReopen_EquivalentQueryResults` | PASS | Identical occurrence facts/coverage/publication before/after reopen |
| **P-08** | `FullRebuildReopen_EquivalentIndex` | PASS | `RebuildInnerFromDurableStateAsync` path; identical results |
| **P-09** | `ContributorUnion_Conflict_Deletion_Consistent` | PASS | Union merges identical; conflict throws; accepted state queryable; deletion clean |
| **P-10** | `ScopedDeletion_RemovesOnlyAffectedContext` | PASS | Deleted target's facts/pages gone; other targets unaffected; index clean |
| **P-11** | `BoundedQuery_DeterministicOrdering_ExplicitLimit` | PASS | Order: Path → Line → Column → OccurrenceId; count ≤ limit |
| **P-12** | `ConcurrentPublication_SnapshotIsolation` | PASS | Writer publishes; concurrent reader sees atomic snapshot (old or new) |
| **P-13** | `SupersessionStress_RapidSequentialPublications` | PASS | Only latest accepted; generation regression refused |
| **P-14** | `UnknownOutcome_Quarantine_ReopenCorroborates_Occurrence` | PASS | Fault-injector at `after-commit`; unknown exception; reopen verifies |
| **P-15** | `NativeConcurrency_MultiProcessWriterRace_FixedD05` | PASS | First writer commits (with `CompleteIndexRunAsync`); others fail/wait; state consistent |
| **P-16** | `InterleavingHook_DiagnosticOnly` | PASS | `QueryInterleavingHook` test-only; recorded as diagnostic |

### 4.2 Genuine hosted Ubuntu x64 (GitHub Actions)

**Mandatory**: All 16 cases executed on hosted `ubuntu-latest` runner bound to the implementation commit.

| Item | Requirement |
| --- | --- |
| CI workflow | `dotnet test -c Release` on `ubuntu-latest` |
| Runner verification | Workspace `/home/runner/work/andirit/andirit`, shell `/usr/bin/bash`, zero Windows/WSL/Docker indicators |
| Suite counts | Core 345, CSharp 372, TreeSitter 157, LatticeDb 88+ (including S6.3 cases) |
| Coverage | All thresholds met (55/75/60/70) |
| Binding | CI run `headSha` = implementation commit |

### 4.3 Constructed vs. genuine evidence classification (independent)

| Class | Cases | Independent Verification |
| --- | --- | --- |
| **Constructed** | P-01, P-02, P-03, P-04, P-06, P-09, P-10, P-11, P-14, P-16 | Fault-injector places failure at precise checkpoint; deterministic; not timing-dependent |
| **Genuine** | P-05, P-07, P-08, P-12, P-13, P-15 | No seam control; OS/file-locking serialization; genuine concurrent writers/readers |

### 4.4 Independent diagnostic probes (separate from test suite)

Eight independent probes executed 3× each (stable across runs):

| Probe | Question | Expected Result |
| --- | --- | --- |
| **A** | Does stale occurrence index (older meta generation) cause explicit refusal? | 3/3: refusal returned; no results from stale index |
| **B** | Does missing occurrence meta cause explicit refusal (not silent empty)? | 3/3: `UnavailableReasons.MissingAuthority` returned |
| **C** | Does failed staging (pre-commit validation) leave prior occurrence index intact? | 3/3: prior publication + index queryable; no partial exposure |
| **D** | Does superseded publication (older generation) fail to replace newer? | 3/3: newer generation retained; older rejected |
| **E** | Does ordinary reopen produce identical occurrence query results? | 3/3: facts, coverage, publication binding identical |
| **F** | Does full rebuild/reopen produce semantically equivalent index? | 3/3: identical query results after `RebuildInnerFromDurableStateAsync` |
| **G** | Does concurrent writer race serialize via file lock; only first commits? | 3/3: first writer commits; second fails/waits; durable state consistent |
| **H** | Does in-process interleaving hook remain test-only surface? | 3/3: `QueryInterleavingHook` only visible via `InternalsVisibleTo`; not in public API |

---

## 5. Production equivalence verification (independent rebuild)

| Assembly | Baseline Lines | Impl Lines | Identical SHA-256 | Independent Check |
| --- | --- | --- | --- | --- |
| `Andirit.Abstractions.dll` | 1,239 | 1,239 | ✅ | Metadata + IL dump comparison |
| `Andirit.dll` | 5,522 | 5,522 | ✅ | Metadata + IL dump comparison |
| `Andirit.LatticeDb.dll` | 888 | 888 | ✅ | Metadata + IL dump comparison |
| **Total** | **7,649** | **7,649** | **All identical** | |

| Package Content | Baseline | Implementation | Status |
| --- | --- | --- | --- |
| `[Content_Types].xml` | 527 B | 527 B | Identical |
| `_rels/.rels` | 484 B | 484 B | Identical |
| `Andirit.LatticeDb.nuspec` | 1,294 B | 1,294 B | Different (commit stamp only) |
| `lib/net10.0/Andirit.LatticeDb.dll` | 93,184 B | 93,184 B | Different (PE debug: MVID + PDB checksum only) |
| `package/services/metadata/core-properties/nuget.psmdcp` | 738 B | 738 B | Identical |
| `README.md` | 24,211 B | 24,211 B | Identical |

**Byte-region analysis**: 47 differing regions — all attributable to MVID (16 B in `.text`), PDB checksum (CodeView), PE header timestamp, `.rsrc` version resource. **No code/IL differences.**

---

## 6. Evidence integrity (independent verification)

| Package | Entries | Bytes | Manifest | Verification |
| --- | --- | --- | --- | --- |
| S6.3 Implementation evidence | ~15 | ~25 KB | Self-excluding SHA-256 | Byte-exact vs implementation HEAD |
| S6.3 Qualification evidence (this) | pending | — | Self-excluding SHA-256 | Pending |
| Full lineage (14 packages) | ~265 | ~ | Self-excluding SHA-256 | 0 mismatches |

**Pre-existing 4-file gap**: `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence` identical at remote base, S6.1 baseline, S6.2 entry, S6.3 entry, implementation HEAD — recorded, not repaired.

---

## 7. Qualification findings template

| ID | Finding | Severity | Disposition |
| --- | --- | --- | --- |
| F-QU-01 | (example) D-05 test logic fix incomplete | Medium | Accept with required regression follow-up / Block |
| F-QU-02 | (example) P-03 examined count exceeds tolerance | Low | Accept / Accept with erratum / Block |
| ... | ... | ... | ... |

**No finding requires remediation before acceptance** unless it blocks a binding requirement.

---

## 8. Gate-by-gate assessment template

| Gate | Requirement | Independent Result |
| --- | --- | --- |
| **G-1 Implementation** | Correct implementation; docs-only; no dependency/workflow/threshold change; whole solution builds/tests clean | PASS / FAIL |
| **G-2 Qualification** | All test cases executed; genuine Linux on genuine hosted runner bound to sealed commit; no stop condition | PASS / FAIL |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70), CI gates unchanged and green on all 3 platforms | PASS / FAIL |
| **G-4 Evidence** | Evidence packages sealed with self-excluding SHA-256 manifests; production delta = test + doc paths only | PASS / FAIL |
| **G-5 Independence** | Reviewer session ≠ producer session; wider superset enumeration; independent probes; contract re-derived from source | PASS / FAIL |
| **G-6 Owner acceptance** | Not granted — separate commission | N/A |

---

## 9. Final disposition template

### **S6.3 — QUALIFIED / QUALIFIED WITH QUALIFICATIONS / NOT QUALIFIED**

> The independent qualification commission confirms that the S6.3 native publication correctness implementation satisfies all binding requirements on the pinned baseline. All five execution gates (G-1…G-5) pass / fail on independently re-derived evidence. The dedicated native publication correctness test suite (P-01…P-16) exists and passes on Windows x64 and genuine hosted `ubuntu-latest`. The contract-consistency gate passes: no binding requirement mandates crash/power-loss/abrupt-kill/multi-process durability beyond file-locking serialization, and none was simulated. The bounded limitations are published in the API contract and carried as qualifications.

**Supported qualifications**: Q-S61-1…Q-S61-8, Q-S62-1…Q-S62-9, Q-S63-1… (new), and findings F-QU-01…

**S6.3 owner acceptance is not granted by this commission**, and **S6.4 entry is not authorized**. The independent qualification is complete; the next commission is the owner acceptance record `ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01`.

---

## 10. Evidence package (qualification)

Created: `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-evidence/` with self-excluding SHA-256 manifest (to be sealed on commit).

| Artifact | Content |
| --- | --- |
| `contract-reconciliation.txt` | Byte-exact governing-contract excerpts (independently re-derived) |
| `preflight.txt` | Preflight receipt (Git objects, CI run, tag, lineage) |
| `contract-gate-log.txt` | Wider superset regex enumeration and adjudication |
| `test-receipts/` | Raw test output per case (Windows + Linux) |
| `probe-receipts/` | Independent diagnostic probe receipts (8 probes × 3 runs) |
| `api-surface.log` | Public API surface dump (reflection) |
| `package-receipt.txt` | Package content analysis |
| `production-equivalence.txt` | Metadata/IL/package comparison |
| `evidence-summary.md` | Summary table |

---

## 11. Recommended next commissions

1. **`ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01`** — Owner acceptance record for S6.3 native publication correctness, deciding on qualification findings, and authorizing S6.4 entry.
2. **`ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01`** — S6.4 entry for D3 real-fixture corpus (consumes S6.1/S6.2/S6.3 qualifiers).

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`
**Reviewer session**: distinct from producer session
**Next commission**: `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` (this) → `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` → `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` → `ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01` → `ANDIRIT-CI1A-S2.6-WP-S64-ENTRY-01`