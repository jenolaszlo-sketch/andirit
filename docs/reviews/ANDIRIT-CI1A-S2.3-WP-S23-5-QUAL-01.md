# ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01 — Final S2.3 integrated qualification

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01` (owner-authorized entry: `ENTRY ACCEPTED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Role:** independent integration qualification engineer.
**Reviewed source:** `9a8d5236b45d8e7d674126d5e94abaec26b0c80e` (tree `87b0d907be4bd4313de2b781ea59065d525e89e6`).
**Disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> Tests, reviewer probes, additive evidence and qualification reporting only. No
> production change, no new public API or storage contract, no S2.3 acceptance
> decision, and no WP-S23-5 successor authorization. No push.

---

## 1. Preflight

| Check | Result |
| --- | --- |
| HEAD | `9a8d5236b45d8e7d674126d5e94abaec26b0c80e` (expected) |
| Tree | `87b0d907be4bd4313de2b781ea59065d525e89e6` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`) |
| Tracked/index, stashes, push | clean; no stashes; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` (`environment.log`, `P08`) |
| Production change since review start | **none** (`production-diff.txt`: 0 lines) |

Preflight matched every expected condition; no conflicting authority or unexpected state was found. Qualifications carry a same-harness independence limitation (see §5).

---

## 2. Integrated I-01 … I-11 results

Matrix as defined in the entry assessment; "Fresh" denotes newly executed evidence in this commission, "Reuse" denotes a committed earlier result re-cited.

| # | Check | Evidence (this commission / reused) | Result |
| --- | --- | --- | --- |
| I-01 | Real multi-file/project/namespace end-to-end | **Fresh:** `S23Wp5IntegrationTests.I01_*`; probes `P01`/`P02` | **PASS** |
| I-02 | Recovery × carrier × authority | **Fresh:** `I02_*`; probes `P03`/`P04`; reuse S23-LIFE `L10` | **PASS** |
| I-03 | Completed + staged reopen with carrier + authority; stale after reopen | **Fresh:** `I03_I07_*`; probe `P07`; reuse S23-PUB-Q1/Q2 | **PASS** |
| I-04 | **SQ-06** isolated namespace mismatch → `CodeOccurrenceSchemaException` | **Fresh:** `S23Wp5NamespaceRefusalTests.Store_RejectsUnsupportedOccurrenceNamespaceOnOpen`; probes `P05`/`P06` | **PASS** |
| I-05 | SQ-02 unknown-outcome quarantine/reopen; no blind retry | Reuse: NEW-01 `P15`, S23-LIFE `L20` | **PASS (reuse)** |
| I-06 | SQ-03 native schedule, old-or-new, no partial blend | Reuse: S23-LIFE `P06`/`P07`, `L12*` | **PASS (reuse)** |
| I-07 | Real-source replace/delete/reindex with carrier; supersession; no unbounded scan; complete-empty vs unavailable | **Fresh:** `I03_I07_*`, probe `P07`; reuse S23-LIFE `L02*`/`L13*`/`L14*` | **PASS** |
| I-08 | D3 graph noninterference | Reuse S23-LIFE `L16`, N8; **fresh light** node/edge non-leak assertion in `I01_*` | **PASS (see Q-I08)** |
| I-09 | Integrated availability/coverage/refusal (no false-available/empty) | **Fresh:** `I09_*`; reuse S23-PUB/S23-LIFE | **PASS** |
| I-10 | Regression floor + oracle + format + Release | **Fresh:** `full-solution-tests.log`, `oracle.log`, `format.log`, `release-build.log` | **PASS** |
| I-11 | Provenance/manifest; production-diff = 0 | **Fresh:** `manifest.json` (32/0), `production-diff.txt` (0) | **PASS** |

**I-01 detail.** The fixture is a genuine real project: `Core` + `Consumer` (2 projects), 11 `.cs` files, 3 namespaces (`Core`, `Consumer`, `Consumer.Sub`), 6 overloaded `Core.Api` methods, and 50 invocations. Fresh-process reopen serves the two overload targets with **30** and **20** complete occurrences, all with `SourceUnitId == Consumer` project unit; expected targets independently recomputed (frozen R2 + identity primitives agree).

**I-02 detail.** Real cross-project publication (`Lib←App`); a second run is staged with changed source and fails under a managed `before-commit` fault. The recovered in-process projection keeps the prior authority publication (`run:a`, 1 occurrence, `Available`) and the published graph-owner set (2 units, all `RequiresOccurrenceAuthority`); a **separate process** reopen yields the same result. The accepted re-stage branch then completes run:b; a fresh reopen serves run:b and refuses run:a with `StalePublication` (`Examined == 0`).

**I-03/I-07 detail.** After a real source change, the reopened publication serves the current fact and refuses the superseded publication as stale with no scan. Retiring the referencing unit leaves the defining unit's zero-fact analysis → **complete-empty** `Available`, never stale/available leftovers.

**I-09 detail.** Missing current authority → `AnalysisUnavailable` (never empty-success); a foreign publication handle → `StalePublication`/`Examined == 0`; the current verified authority → `Available`/complete.

---

## 3. SQ-06 finding disposition (entry finding RQ2)

**Status: TEST GAP CLOSED — no production defect.**

- Production already refuses an unsupported occurrence **namespace**: `LatticeCodeGraphStore` open-time `RestoreOccurrences` throws `CodeOccurrenceSchemaException` when `meta.Namespace != OccurrenceNamespace` (source unchanged; 0 production lines).
- The accepted Stage-1 suite exercised only the **version** branch (`…SchemaOnOpen` tampers version → 99) and the missing-manifest branch. No isolated namespace-mismatch test existed.
- **Added** (`tests/Andirit.LatticeDb.Tests/S23Wp5NamespaceRefusalTests.cs`): publishes a real unit, applies an **isolated** `Namespace` tamper while leaving `SchemaVersion` valid, and asserts `CodeOccurrenceSchemaException` on open, with `ExpectedVersion == ActualVersion == CurrentOccurrenceSchemaVersion` (distinguishing it from the version-mismatch control whose `ActualVersion == 99`).
- Fresh-process probe `P05`/`P06` reproduces the same result in two OS processes (`message = "The occurrence schema version or namespace is unsupported."`).
- **Disposition:** the SQ-06 **test** gap is closed for the S2.3 surface. The register item still requires the independent-review/owner step and must not be treated as closing the S2.5/Stage-2 platform matrix.

---

## 4. Cross-package interaction findings

| Interaction | Verdict | Evidence |
| --- | --- | --- |
| WP-S23-1 recovery rebuild (`RebuildInnerFromDurableStateAsync` → `RestoreOccurrences`) over the WP-S23-2 carrier + WP-S23-3A authority envelope | **No defect.** The recovery path repopulates `_authorityPublications` from durable rows; recovered projection equals a fresh reopen; graph-owner correspondence preserved; recovery never converts stale/unverified state into authoritative-available results | `I02_*`, `P03`/`P04` |
| Completed-publication reconstruction (R1) + staged-run reconstruction (R2) with authority envelopes + defining-`compctx2` carrier in one durable file | **No defect.** Reopen, order independence and stale refusal hold | `I03_I07_*`, `P07`, reuse S23-PUB-Q1/Q2 |
| Ownership correspondence (WP-S23-2 R1) against real-fact authority units (WP-S23-3A) | **No defect** (reused); O-Q2-01 remains a separate, unremediated candidate | reuse WP-S23-2 Q2, S23-LIFE |
| Superseded/retired publication state leaking stale-available occurrences | **No leak.** Superseded → `StalePublication`/0; retired → complete-empty | `I03_I07_*`, `P07`, `I09_*` |

No new production defect, evidence-integrity failure, or material architectural inconsistency was found; no remediation is required.

---

## 5. Combined independent-review synthesis

Classification of the evidence underlying WP-S23-1 … WP-S23-5 (the synthesis itself reuses earlier evidence and is **not** an independently executed re-run except where marked fresh):

**1. Fresh independently executed (this commission):** `I01`–`I04`, `I07`, `I09` native tests; probes `P01`–`P08` (each a distinct OS process); the I-10 regression/oracle/format/build re-run; the I-11 manifest. All Windows x64, real store.

**2. Previously accepted independent evidence:** WP-S23-2 Q2 fresh-process F-01 reproduction; S23-PUB-Q1/Q2 fresh-process reopen/staged/corruption probes; S23-LIFE-Q1 native tests + `P01`–`P08`.

**3. Same-harness reviewer evidence (disclosed):** all of the above review gates were produced in the same agent harness as distinct commissioned tasks; there is no separate-session/separate-model receipt. Independence rests on reviewer-owned committed probes, frozen-R2/identity-derived expected values, and the producer/reviewer commit separation.

**4. Synthetic or constructed evidence:** original Stage-1 `N1`–`N8`; the `L04_*` two-storage-unit union construction; all corruption/tamper controls (schema version, namespace, meta-row mutation, owner-row removal, malformed rows). Used for refusal/merge algebra only.

**5. Managed fault-injection evidence:** NEW-01 recovery probes; WP-S23-2 recovery/stale probes; S23-LIFE `L09`/`L10`/`L20`; this report's `I02` before-commit fault. Establishes ordinary known-failure recovery and unknown-outcome quarantine, **not** crash/power-loss durability.

**6. Authentic real-project evidence:** real Roslyn compilations over real source bytes/hashes, real cross-project `asmctx3` targets, real defining `compctx2` bindings, published through the accepted `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync` path; fresh-process reopen; direct native row inspection.

**7. Unsupported or untested claims:** crash/power-loss/abrupt-kill; Linux/cross-platform (SQ-05); a naturally occurring fully real-authority cross-unit identical-fact union (F-01); authentic SQ-04 historical fixtures; S-01 digest canonicalization; MVCC/snapshot-isolation/general linearizability; multi-process writers; O-Q2-01 remediation; Stage-2 §9 *method-reference*/unsupported-incomplete minima beyond the invocation-fact scope.

**Synthesis verdict.** Within the accepted Windows-x64, serialized single-writer, tests-and-evidence scope, the combined evidence supports **integrated S2.3 correctness** for the accepted functionality. The synthesis is a reviewer cross-reference of separately produced evidence; it is not labelled independently executed where it reuses earlier results.

---

## 6. Evidence provenance and manifest verification

- Manifest: `docs/reviews/CI1A-S2.3-WP-S23-5-QUAL-01-evidence/manifest.json` — **32 entries; 0 missing; 0 byte/hash mismatches** (self-excluding SHA-256 over exact bytes; `probe-databases/` excluded).
- Reviewer probe harness committed under `docs/reviews/CI1A-S2.3-WP-S23-5-QUAL-01-tests/` (csproj, `Program.cs`, `run-probes.ps1`, `seal-evidence.ps1`); it references only `src/` projects + the frozen R2 authority, and asserts against frozen-R2/identity-derived expected values.
- `reviewed-head.txt` binds reviewed HEAD `9a8d523`; `production-diff.txt` = 0 lines; `test-additions-diff.txt` lists the additive files with SHA-256.
- Raw receipts distinguish observed behavior (JSON `P0*`) from inferred behavior (report prose). Each critical phase is one OS process with a recorded `processId`.

---

## 7. Regression and native execution receipts

| Gate | Command | Result | Log |
| --- | --- | --- | --- |
| New qualification tests | `dotnet test … --filter S23Wp5*` | **5 passed** (4 CSharp + 1 LatticeDb) | `new-qualification-tests.log` |
| Release build | `dotnet build Andirit.slnx -c Release` | 0 warnings / 0 errors | `release-build.log` |
| Full solution | `dotnet test Andirit.slnx -c Release --no-build` | **888 passed / 0 failed / 0 skipped** (51 + 157 + 345 + 335) | `full-solution-tests.log` |
| Focused regression | `… --filter S23|New01|Occurrence|S2_2Wp` | **194 passed / 0 failed** (170 + 24) | `focused-s23-regression.log` |
| Frozen R2 oracle | `SemanticCorpusOracle …` | **185/185**, 146,851 bytes, SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` | `oracle.log`, `oracle-r2.json` |
| Formatting | `dotnet format Andirit.slnx --verify-no-changes` | exit 0 | `format.log` |
| Integrated probes | `run-probes.ps1` | 8 phases, 0 failed | `probe-matrix.txt` |

Baseline floor before this commission was 883 passed; post-change 888 = 883 + the 5 new tests. Counts are regression floors; overlapping suites are not aggregated as independent coverage.

---

## 8. Open findings and binding qualifications

Preserved without reinterpretation: **F-01** (restricted real-authority cross-unit union), **F-02** (managed fault injection ≠ crash/power-loss), **F-03** (authentic SQ-04 historical fixtures unavailable), **Q1-Q1**, **Q1-Q2**, **S-01**, **Q2-Q01**, **Q2-Q02**, **O-Q2-01**, the same-harness independence limitation, and the Windows-x64 / serialized single-writer boundaries.

Open **S2.5 / Stage-2** obligations (not discharged by S2.3): expanded **Linux** native suite (SQ-05); authentic **SQ-04** historical fixtures; **S-01** integrated proof; D4.5 and full S-02 Stage-6 items.

Report-specific qualifications:
- **Q-I08.** D3 at integration level is **reused** (S23-LIFE `L16`, Stage-1 `N8`) plus a fresh light assertion that occurrence target identities never appear as graph node/edge ids. A fresh *multi-project-graph-only* D3 lifecycle variant was not separately executed; the real plugin path couples graph and occurrence per project, so the controlled occurrence-only unit model remains the correct D3 instrument. Bounded, non-blocking.
- **Q-I01.** The real-project fixture meets the file/project/namespace/invocation/overload minima; Stage-2 §9's *method-reference* and *unsupported/incomplete-analysis* minima are coverage categories outside the accepted S2.3 invocation-fact scope and are recorded, not claimed.

No new blocking finding. **SQ-06 test gap closed** (§3).

---

## 9. Final qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.**

All mandatory I-01 … I-11 checks pass with genuine native real-fact evidence at reviewed HEAD `9a8d523`; the SQ-06 test gap is closed; cross-package interactions expose no defect; the regression floor, oracle, format and Release build are clean; the evidence manifest is intact; and zero production lines changed. The only qualifications are the explicitly bounded, non-blocking items carried from the predecessor gates plus Q-I01/Q-I08 in §8.

---

## 10. Recommendation

A **separate S2.3 owner-acceptance commission** (`S2.3-ACCEPT-OWNER`) is **recommended** and is **not** authorized by this commission. It should be bounded to documentation/decision only; accept the combined S2.3 result within the qualified Windows-x64 scope; preserve all qualifications in §8; record the open S2.5/Stage-2 obligations (S-01, SQ-04, SQ-05, D4.5, full S-02); and grant no successor authorization and no public Find Usages. WP-S23-5 execution, S2.4, S2.5, Stage 6, and public Find Usages remain unauthorized.

---

*Additive documentation only. No production code, test, oracle/corpus, planning contract, or prior decision was modified; historical artifacts are preserved unchanged.*
