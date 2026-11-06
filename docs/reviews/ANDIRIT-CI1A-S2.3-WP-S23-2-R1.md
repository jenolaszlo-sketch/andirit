# ANDIRIT-CI1A-S2.3-WP-S23-2-R1

## Producer remediation: stale occurrence invalidation after graph-owner retirement

**Commission:** bounded WP-S23-2 remediation and additive evidence for the Q1 F-01 finding.
**Repository:** `D:/projects/Andirit`. **Branch:** `main`. **Date:** 2026-11-06 (Asia/Manila).
**Disposition:** **REMEDIATED — READY FOR INDEPENDENT Q2**.
**Owner acceptance:** not granted by this report. The next decision belongs to the independent Q2 reviewer and, subsequently, the owner.

## 1. Verified baseline and source binding

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Accepted implementation parent | `b576acb004747c89d257d8e61721c17babe6c5a6` | `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` |
| WP-S23-2 implementation | `51369b3b463b6a2f137ee3475b78c9cd43f4db85` | `2deec87bca15b24133be51263a7c9ea137b5b978` |
| Q1 reviewed HEAD / tree | `d94052d2fa8dd14ecbfcc6337b4a262e8954ba85` | `53966a01b69c876bfff29a60c576c595bdb9e84b` |
| **R1 remediation commit** | **`1bce70ee2f52ed5100a2e68bc004462f9824d2f2`** | **`a6a908ab69740e4ee114e845cdde9374975dc15a`** |

Preflight matched the reported baseline exactly: canonical `D:/projects/Andirit`, branch `main`, clean working tree, one worktree, HEAD and tree equal to the Q1 reviewer evidence commit/tree. The additive reviewer commit was not mistaken for a production modification. No concurrent modifying agent was observed. No source, historical test/evidence, decision, or frozen input was changed by this remediation commit. The Q1 report and all Q1 evidence remain byte-identical.

Evidence: [`ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence/`](ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence/).
Reused independent probe driver: [`CI1A-S2.3-WP-S23-2-R1-tests/`](CI1A-S2.3-WP-S23-2-R1-tests/) (uses the unchanged Q1 probe executable; it does not edit the Q1 probe or its evidence).

## 2. Root cause (F-01)

The occurrence gate validated occurrence-internal evidence but never cross-checked the occurrence projection against the accepted published graph owner set.

- `RestoreOccurrences` (`src/Andirit.LatticeDb/LatticeCodeGraphStore.cs:518–645`) reconstructs the occurrence projection from the durable `AndiritOccurrenceUnit` / `AndiritOccurrencePage` / `AndiritOccurrenceMeta` rows only. It never consults the published `HetuUnit` graph owner set.
- `CheckOccurrenceAuthorityAsync` (`src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs`) then validated publication identity, occurrence accounting, the mandatory per-unit carrier/version, and each unit's recorded defining context against the current verified live compilation — but it iterated only `stored.Units` (derived from the occurrence tables). It neither required each occurrence storage owner to exist in the published graph units, nor required every published authority-requiring graph unit to have an occurrence association.
- Because the graph owner set and the occurrence owner set were reconstructed independently and never reconciled, deleting a published `HetuUnit` row (leaving every occurrence row intact) produced an orphaned authority-bearing occurrence unit that still passed semantic verification against a matching live caller. The reopened query returned `Available` / 1 hit / `IsComplete = true` even though the graph API listed only the remaining unit.

This is a **reopen cross-projection integrity** gap, not a transaction-atomicity failure and not a defect in the accepted staged-deletion path (which retires the graph owner and its occurrence contribution together; P17–P19 already passed). F-01 is also distinct from stale-context refusal: a current verified compilation context proves semantic freshness, not graph ownership.

Failure class: **graph and occurrence publication state diverge, and the query gate has no authoritative ownership correspondence check.** In-memory state, durable state, and durable projection reconstruction are each individually correct for the data they read; the missing element is the cross-projection reconciliation rule.

## 3. Original failure reproduction

The independent Q1 reviewer reproduced the defect with three separate processes against the previously qualified implementation (`d94052d`), recorded in `docs/reviews/CI1A-S2.3-WP-S23-2-Q1-evidence/P36-graph-orphan.json` and `orphan-confirmation.json`:

1. Publish a source-owned unit (with occurrence facts and a verified carrier) plus a complete-empty unit.
2. Delete exactly the durable graph-owner row `HetuUnit|review:q1-native-authority\nreview.q1.csharp\nClient.csproj`; no occurrence fact/receipt/meta/page/run/baseline/index-state row changes.
3. A separate fresh reader returned `Available`, one occurrence hit, `IsComplete = true`, with no refusal.

The R1 fresh-process reproduction (`r1-orphan-summary.json`) observes the identical sequence against the corrected implementation and now refuses. "Before" and "after" are captured inside the same three-process run.

## 4. Required behavioral correction and production change

The smallest coherent correction establishes a single authoritative lifecycle rule at the existing query gate: **the accepted occurrence projection must correspond to the accepted published graph owner set in both directions.**

Changed production file (only one): `src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs`.

After the existing accounting check and before returning positive availability, the gate now reads the accepted published graph units from the same materialized store and builds the graph owner set keyed by `(plugin, storage unit)`. It then:

- refuses `CorruptedIndex` if any stored occurrence unit's storage owner is absent from the published graph units (orphaned/retired occurrence), and
- refuses `CorruptedIndex` if any published graph unit that carries the `RequiresOccurrenceAuthority` marker has no occurrence association (missing mandatory association).

Both directions use the existing `CorruptedIndex` result vocabulary (the Q1 probe matrix already recorded `CorruptedIndex` as the expected result for P36; ownership inconsistency is already `CorruptedIndex` elsewhere, e.g. P33). No status vocabulary, public API, identity, schema, namespace, or persisted representation was changed. The check is a durable correspondence rule, not a query-only filter: the occurrence projection is still reconstructed independently at open, and the decision is derived from the accepted published graph owner set, so it survives process restart.

Rationale for the chosen location: the gate is the single authoritative positive-availability decision point for the verified path, is already under `_writeGate`, already has the materialized `_inner` store, and already owns refusal mapping. A separate cleanup/rewrite of durable rows was rejected because it would mutate persisted authority from a read path and would not address externally removed owners. Broadening the representation was rejected as unnecessary: the existing `RequiresOccurrenceAuthority` marker and `(plugin, unit)` ownership key already carry the association, so no new fields are required.

## 5. Regression tests

New file: `tests/Andirit.CSharp.Tests/S23OccurrenceOwnershipRegressionTests.cs` (6 tests). Supporting additive fixture helpers: `S23NativeAuthorityFixture.RemoveGraphUnit`, `RemoveOccurrenceUnit`, `MutateMetaRow`, `RemoveRow`, `WriteRow` (the existing `MutateUnit` now delegates to `WriteRow`). These use the real LatticeDB file boundary; the critical retirement/reopen cases reopen a fresh store on the same file.

| Test | Scenario verified |
| --- | --- |
| `GraphOwnerRemoved_OrphanedOccurrence_RefusesAsCorruptedIndex_AcrossReopen` | Positive control Available/1; remove the durable `HetuUnit` owner; fresh reopen → `CorruptedIndex`, 0 hits, 0 examined, not complete, refusal reason; durable rows unchanged by the read. |
| `PublishedAuthorityUnitWithoutOccurrenceAssociation_Refuses` | Remove the occurrence unit while keeping the authority-requiring graph unit (manifest kept internally consistent) → `CorruptedIndex` (missing mandatory association). |
| `StagedDeletionOfOneUnit_LeavesUnaffectedUnitsQueryable_AcrossReopen` | Accepted staged deletion of one of several units retires graph + occurrence together; the unaffected unit remains `Available`/complete after reopen. |
| `RetainedOwnerBesideGraphOnlyUnit_IsNotMistakenForOrphan` | A later graph-only unit (no marker) does not invalidate retained authority-bearing units (negative control for over-refusal). |
| `CompleteEmptyPublication_RemainsCompleteZero_NotRefused` | Complete-empty stays `Available`/0/complete; omitted current authority still refuses `AnalysisUnavailable` (empty ≠ unavailable). |
| `Replacement_ReportsNewValidState_AndRefusesOldContext_AcrossReopen` | Replacement reports fresh valid state and refuses the old context as `StalePublication` across reopen. |

The original defect is reproduced by the unchanged independent Q1 probe against the corrected implementation; the Q1 probe and its evidence were not weakened or edited.

## 6. Verification results

Windows x64 10.0.26200, SDK 10.0.401, runtime 10.0.12, Roslyn 5.9.0.0. Full command/outcome detail in `verification-results.json` and the corresponding logs.

| Verification | Observed result |
| --- | --- |
| New R1 regression tests | **6 / 0 / 0** (`r1-ownership-tests.log`) |
| Existing WP-S23-2 tests | **32 / 0 / 0** (`r1-wp-s23-2-tests.log`) |
| NEW-01 independent probes | **17 / 0 / 0** (`r1-new01-tests.log`) |
| Full solution | **834 / 0 / 0** = LatticeDb 50 + TreeSitter 157 + core 345 + CSharp 282 (`r1-solution-tests.log`); Q1 baseline 828 + 6 new |
| Frozen R2 oracle | **185/185**, 0 failed; 146851 bytes; output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged) |
| Format | `dotnet format Andirit.slnx --verify-no-changes` exit 0 |
| Release build | 0 warnings, 0 errors |
| Debug build | 0 warnings, 0 errors |
| **R1 fresh-process F-01 reproduction** | publish `Available`/1/complete → remove owner → fresh reader `CorruptedIndex`/0/0/not complete, refusal reason; 3 distinct PIDs; exactly the graph-owner row changed; mutation and reader native rows equal (`r1-orphan-summary.json`) |
| Preserved-behavior controls (independent probe) | **35/35** representative expectations met (`r1-control-summary.json`) |

The preserved-behavior controls independently re-exercise, with fresh processes: matching/stale authority, accepted retirement, graph-only vs complete-empty, staged resumption, retained-owner resumption, per-record mutation refusals (missing/malformed/unsupported/forged-context/wrong-owner/wrong-payload/wrong-publication/meta-snapshot), and the accepted O-01 page-ID observation. All met their previously accepted statuses.

## 7. Preserved guarantees and remaining qualifications

Retained unchanged: known-failure recovery and NEW-01 (17/17), stale-context refusal, conflict detection/refusal, complete-empty semantics distinct from unavailable coverage, unknown-outcome quarantine, coverage/refusal accounting, published graph/occurrence consistency for the accepted deletion/replacement paths, native mirrors, and D3/D4/D5. Q1-Q1 (segment resumption) and Q1-Q2 (recovery-rebuild atomicity) remain explicitly qualified and unclosed. S-01 component-digest qualification remains S2.5 scope.

The remediation commit is limited to WP-S23-2 F-01. It does not grant owner acceptance, does not expand the accepted qualification scope, and starts no successor package (WP-S23-3 remains separately commissioned).

Remaining limitations: no abrupt-kill/power-loss, arbitrary-crash, unrestricted-concurrency, cross-platform, or multiple-writer guarantee is claimed; the change was verified on Windows only. The full 56-invocation Q1 probe matrix was not re-run to avoid rewriting Q1-owned evidence; 35 representative preserved-behavior probes plus the 6 new tests cover the affected semantics. No performance or autonomy claim is added.

## 8. Reproducibility

From `D:/projects/Andirit` after `dotnet restore`:

```text
dotnet test Andirit.slnx -c Release --no-restore
dotnet build docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/ReviewerProbe.csproj -c Release
python docs/reviews/CI1A-S2.3-WP-S23-2-R1-tests/run-r1-orphan.py
python docs/reviews/CI1A-S2.3-WP-S23-2-R1-tests/run-r1-controls.py
python docs/reviews/CI1A-S2.3-WP-S23-2-R1-tests/seal-r1-evidence.py
```

`run-r1-orphan.py` builds the unchanged Q1 probe against the corrected production source, creates a temporary corpus/database, runs the three-process counterexample, and asserts the corrected refusal. `seal-r1-evidence.py` regenerates the deterministic R1 evidence manifest. No worktree, branch, reset/restore, or push is performed.

## 9. Evidence manifest

`manifest.json` binds all R1 evidence files by sorted relative path, exact byte length, and SHA-256 (excluding itself). It covers the production/test diffs, the fresh-process orphan reports/logs, the 35 preserved-behavior control reports/logs, the oracle output, environment, and verification results. The original Q1 report and evidence are unchanged.
