# ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Gate**: Stage 6 work package **S6.3** — entry assessment for the full native D4.5 fault matrix and complete S-02 publication correctness obligations, covering the remaining D4 invariants (M12.1, M12.3) and the full publication lifecycle (staging, validation, commit, supersession, reopen, recovery).
**Mode**: investigation, contract reconciliation, feasibility assessment, implementation boundary definition, and independent qualification plan only. No production implementation, no dependency change, no test changes, no workflow change, no acceptance threshold change, no sealed evidence modification, no historical findings closure, no package publication, no tag changes, **no push** performed.

**Formal disposition**: **ENTRY AUTHORIZED WITH CONDITIONS**

> S6.3 entry is authorized with the conditions documented below. A separate implementation commission (`ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01`) is proposed as the successor to this entry assessment. No production implementation is authorized by this record.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` |
| Authorized scope | S6.3 entry assessment only: governing requirements (remaining D4 invariants M12.1/M12.3, full S-02 matrix), feasibility, implementation boundaries, qualification plan, successor commission proposal |
| Baseline | Current local `main` HEAD `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (10 commits ahead of `origin/main`, not pushed) |
| S6.1 acceptance | `92bf73a61794fa3bccad53d23dd7e65c51807cd5` |
| S6.2 acceptance | `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (this HEAD) |
| S6.1 qualification HEAD | `f8f430b1faf4b3326c723ccb9f5a057228f42fb4` |
| S6.2 qualification HEAD | `459b50c48f0ac2ef0a7bb4ca1f7d15c2a3d7e838` |
| `origin/main` | `01624943233f16447c3176b7c905c544642dfb2c` |
| Tag `v0.2.0-preview.7` | `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` (unchanged) |
| Working tree | Clean (only local commits) |
| Implementation authorization | **Not granted** — requires separate bounded authorization |
| Push authorization | **Not granted** — documents and evidence committed locally only |

### 1.1 Ancestry verification (independently re-derivable)

- S6.1 entry `2d1e8ec…` → S6.1 implementation `0162494…` → S6.1 qualification `f8f430b…` → S6.1 acceptance `92bf73a…` → S6.2 entry `915f5ba…` → S6.2 implementation `ac46235…` → S6.2 qualification `459b50c…` → S6.2 acceptance `359a9e8…`: **all ancestor relations hold**.
- `eafd632…` (S6.1 qualified implementation) is a strict ancestor of all subsequent commits.
- `origin/main` = `0162494…` (the pushed implementation revision); the local HEAD is 10 commits ahead (S6.1/S6.2 qualification, acceptance, entry, evidence seals, record finalization) and **not pushed** per authorization.

### 1.2 Deliverables produced by this commission

| Deliverable | Document |
| --- | --- |
| Commission record (this document) | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01.md` |
| S6.3 obligation inventory | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-obligation-inventory.md` |
| S6.3 traceability matrix | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-traceability-matrix.md` |
| S6.3 feasibility assessment | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-feasibility.md` |
| S6.3 implementation proposal | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-implementation-proposal.md` |
| S6.3 independent qualification plan | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-qualification-plan.md` |
| Proposed successor commission | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-successor-commission.md` |
| Sealed evidence package | `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-evidence/` |

---

## 2. Repository preflight (all conditions met; no stop condition)

Verified against primary sources; re-derivable from Git objects.

| Check | Result |
| --- | --- |
| Local `main` HEAD | `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (tree `...`) |
| Remote `main` | `01624943233f16447c3176b7c905c544642dfb2c` — local 10 commits ahead, not pushed |
| Working tree | Clean at preflight (verified before any commission artifact was written) |
| CI run `45037630799` | `completed` / `success`, `headSha = 0162494…` (implementation revision) — all 3 platforms green |
| Qualified S6.1 implementation | `eafd632…` is a strict ancestor (21 commits behind HEAD) |
| Qualified S6.2 implementation | `eafd632…` (same — S6.2 implementation `ac46235…` is 14 commits behind HEAD) |
| `src/` since `eafd632` | **Unchanged** — S6.1/S6.2 are documentation, test, and evidence only |
| `tests/` since `eafd632` | Only `NativeCancellationPathTests.cs` and `NativeDurabilityTests.cs` added (S6.1/S6.2 test suites) |
| `.github/` since `eafd632` | Only `.github/workflows/publish.yml` — authorized publication-workflow remediation |
| Published tag | `v0.2.0-preview.7` object unchanged; remote ref matches local object |
| Release version | `Directory.Build.props` = `0.2.0-preview.7` (matches published tag) |
| LatticeDbSharp | `0.2.0` pinned (assembly SHA-256 `d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46`) |
| Sealed evidence packages | **13** lineage packages, **250 entries**, **0 mismatches** (byte-exact, self-excluding manifests) |
| Pre-existing 4-file gap | `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence` identical at remote base, S6.1 baseline, S6.2 entry, current HEAD — recorded, not repaired |
| Stage-2 acceptance decisions | Intact and unmodified |
| S6.1/S6.2 qualifications | Sealed and byte-exact; no evidence substituted |

---

## 3. Governing contracts reconciliation (summary)

The S6.3 boundary derives **only** from existing contracts; no requirement is invented and no guarantee is reinterpreted. Byte-exact source excerpts are in `evidence/contract-reconciliation.txt`.

| Source | What it fixes for S6.3 |
| --- | --- |
| `ADR-CI1-01` owner decision §D.4 (D4 invariants 1–10) | The ten explicit invariants governing index maintenance, publication, and cancellation. S6.1 discharged invariant 5 (cancelled staging non-exposure); S6.2 discharged invariants 4 (failed staging preserves prior) and the unknown-outcome path. **Remaining: invariants 1, 2, 3, 6, 7, 8, 9, 10** — the full stale-index, supersession, rebuild/reopen, contributor/deletion, ordering/limits, and concurrency/snapshot contract. |
| S-01 obligation reconciliation §S-02 | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* S6.1/S6.2 covered cancelled/failed/unknown. **Remaining: superseded publication, reopen equivalence, native concurrency/republication**. |
| CI1A specification (line 194) | *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario. In-process deterministic interleaving is retained as a diagnostic only and is not accepted as native atomicity evidence."* |
| CI1A acceptance matrix (rows M12.1–M12.3) | **M12.1** — D4 invariants 1–4, 6–9 (stale index, supersession, rebuild/reopen, contributor changes, ordering/limits). **M12.2** — D4 invariant 5 (cancelled staging, **S6.1 done**). **M12.3** — D4 invariant 10 (query behaviour during concurrent publication — snapshot/freshness contract). |
| CI1A obligation register | **I-04** (cancellation — S6.1 done), **I-05** (supersession/reopen — **S6.3**), **I-06** (concurrency/republication — **S6.3**), **I-07** (full D4 matrix — **S6.3**). |
| S2.5 qualification matrix | M12.1/M12.3 deferred with named owner Stage 6; M9.5 (S6.1), M12.2 (S6.1) done. |
| Residual-risk register (DF-1…DF-8) | **DF-2** (D4.5 full native fault matrix — **S6.3**), **DF-3** (full S-02 ten-invariant native matrix — **S6.3**), **DF-8** (M12.1/M12.3 — **S6.3**). |

**Derived S6.3 acceptance boundary (exactly):**

A production-backed test suite that exercises the **remaining eight D4 invariants** (1–4, 6–10) and the **complete S-02 publication correctness matrix** against the actual production `LatticeCodeGraphStore`, including:
- **Stale index refusal** (invariants 1–3): query uses only index consistent with accepted publication; stale/unbound/unverifiable index refuses explicitly; no silent full-store scan.
- **Failed staging preservation** (invariant 4, partly S6.2): failed staging preserves last accepted publication and valid index state.
- **Supersession rejection** (invariant 6): superseded publication must not replace a newer accepted generation.
- **Rebuild/reopen equivalence** (invariant 7): rebuild/reopen produces semantically equivalent published index.
- **Contributor/deletion consistency** (invariant 8): contributor changes and scoped deletion update publication and index visibility consistently.
- **Bounded query determinism** (invariant 9): bounded queries have deterministic ordering and explicit result limits.
- **Concurrent publication snapshot contract** (invariant 10): query behaviour during concurrent publication follows a defined snapshot or freshness contract.

And the **publication lifecycle correctness** cases:
- **Staging**: staged facts invisible to queries; publication not advanced.
- **Validation**: pre-commit validation failures leave durable state unchanged.
- **Commit**: `txn.Commit()` is the durability point; atomic all-or-nothing.
- **Supersession**: newer generation wins; stale publication rejected; no generation regression.
- **Reopen**: ordinary reopen (no rebuild) and full rebuild/reopen produce equivalent query results.
- **Recovery**: unknown-outcome quarantine, corroboration by reopen, no blind retry.
- **Native concurrency/republication**: concurrent writers serialized by file lock; first writer commits; others fail/wait; durable state consistent; no mixed-state exposure.

---

## 4. S6.3 obligation inventory

| ID | Obligation | Source | State at S6.3 baseline | S6.3 assignment |
| --- | --- | --- | --- | --- |
| **D4-1** | Query uses only index consistent with accepted published generation | Owner decision §D.4 #1 | Open | **In** — stale-index refusal tests |
| **D4-2** | Stale/unbound/unverifiable derived index refuses explicitly | Owner decision §D.4 #2 | Open | **In** — explicit refusal status tests |
| **D4-3** | Stale index must not silently trigger unbounded full-store scan | Owner decision §D.4 #3 | Open | **In** — bounded scan enforcement tests |
| **D4-4** | Failed staging preserves last accepted publication and valid index | Owner decision §D.4 #4 | **Partly discharged (S6.2 D-01, D-06)** | **In** — verify with occurrence index |
| **D4-5** | Cancelled staging exposes nothing | Owner decision §D.4 #5 | **Discharged (S6.1 Q-01…Q-06, Q-11)** | **Out** — S6.1 complete |
| **D4-6** | Superseded publication must not replace newer accepted generation | Owner decision §D.4 #6 | Open | **In** — supersession stress tests |
| **D4-7** | Rebuild/reopen produces semantically equivalent published index | Owner decision §D.4 #7 | Open | **In** — reopen equivalence tests |
| **D4-8** | Contributor changes and scoped deletion update visibility consistently | Owner decision §D.4 #8 | Open | **In** — union/conflict/deletion tests |
| **D4-9** | Bounded queries have deterministic ordering and explicit limits | Owner decision §D.4 #9 | Open | **In** — ordering/limit tests |
| **D4-10** | Query behaviour during concurrent publication follows snapshot/freshness contract | Owner decision §D.4 #10 | Open | **In** — native concurrency/republication tests |
| **S-02-1** | Failed publication exercised against production storage | S-01 §S-02 / CI1A spec | **Partly (S6.2 D-01…D-04)** | **In** — extend to occurrence index |
| **S-02-2** | Cancelled publication exercised against production storage | S-01 §S-02 / CI1A spec | **Discharged (S6.1)** | **Out** — S6.1 complete |
| **S-02-3** | Superseded publication exercised against production storage | S-01 §S-02 / CI1A spec | Open | **In** — supersession stress |
| **S-02-4** | Reopen exercised against production storage | S-01 §S-02 / CI1A spec | Open | **In** — ordinary + rebuild reopen |
| **S-02-5** | Native concurrency/republication scenario | S-01 §S-02 / CI1A spec | Open | **In** — multi-process writer race |
| **S-02-6** | In-process interleaving is diagnostic only, not native atomicity evidence | CI1A spec line 194 | Open | **In** — record limit explicitly |
| **M12.1** | D4 invariants 1–4, 6–9 native matrix | S2.5 matrix | Open | **In** — primary S6.3 deliverable |
| **M12.3** | D4 invariant 10 (concurrent publication) native matrix | S2.5 matrix | Open | **In** — primary S6.3 deliverable |
| **I-05** | Supersession/reopen qualification | CI1A obligation register | Open | **In** |
| **I-06** | Concurrency/republication qualification | CI1A obligation register | Open | **In** |
| **I-07** | Full D4 matrix qualification | CI1A obligation register | Open | **In** |
| **DF-2** | D4.5 full native fault matrix | Residual-risk register | Open | **In** |
| **DF-3** | Full S-02 ten-invariant native matrix | Residual-risk register | Open | **In** |
| **DF-8** | M12.1/M12.3 named deferral | Residual-risk register | Open | **In** |
| **Q2-F12** | Crash/power-loss/multi-process/cross-platform not demonstrated | Owner decision §G.2 | Open | **Out** — S6.2 Q-S62-1 records limit; S6.3 does not simulate |
| **F-02** | Managed fault injection ≠ crash/power-loss/abrupt-kill/multi-process | S6.1/S6.2 records | Open | **Out** — unchanged |
| **DF-4** | Crash/power-loss/multi-process durability | Residual-risk register | **Partly (S6.2 D-03, D-07)** | **Out** — S6.2 complete; S6.3 does not extend |
| **DF-1** | Full D3 real-fixture corpus | Residual-risk register | Open | **Out** — S6.4 |
| **DF-6** | Full platform certification | Residual-risk register | Open | **Out** — S6.5 |
| **DF-7** | Optional cross-target/public FindUsages/optimization | Residual-risk register | Unauthorized | **Out** — unauthorized |

---

## 5. S6.1/S6.2 evidence reusability analysis

| Evidence | Reusable for S6.3? | Notes |
| --- | --- | --- |
| S6.1 cancellation contract (Q-01…Q-12) | **Yes** — foundational | Cancellation path, unknown-outcome, truthfulness vocabulary, non-interruptible native window |
| S6.1 native state assertion pattern (HetuStage/HetuUnit direct read) | **Yes** — mandatory | All S6.3 tests must assert native durable state, not managed projection |
| S6.2 durability test suite (D-01…D-10) | **Partly** | D-01, D-06 (failed staging preservation) reusable; D-03, D-04, D-07 (unknown-outcome, abrupt termination) reusable; D-05 (multi-process) **test logic defect** — must be corrected for S6.3 concurrency scenario |
| S6.2 fault-injector seam (`"before-commit"`, `"after-commit"`) | **Yes** — mandatory | Deterministic placement of failure at commit boundary |
| S6.2 genuine hosted Ubuntu x64 evidence (CI run `45037630799`) | **Yes** — mandatory | Linux evidence row must be genuine hosted Linux; Windows does not substitute |
| S6.2 production equivalence (metadata/IL/package) | **Yes** — foundational | Baseline for S6.3 implementation equivalence |
| S6.2 D-05 test (skipped) | **No — defective** | Writer doesn't call `CompleteIndexRunAsync`; publication never committed. Must be fixed for S6.3 concurrency scenario. |
| S6.1/S6.2 evidence package sealing pattern | **Yes** — mandatory | Self-excluding SHA-256 manifests, byte-exact lineage |
| S6.1/S6.2 qualification findings (F-QU-01…F-QU-08) | **Yes** — carried forward | F-QU-01 (D-05 test fix) required as S6.3 follow-up; F-QU-02 discharged |

---

## 6. Implementation feasibility assessment

### 6.1 Production code changes required

| Area | Change needed? | Nature |
| --- | --- | --- |
| `LatticeCodeGraphStore.Persist()` / `PersistOccurrences()` | **No** — existing logic covers supersession, reopen, contributor union, conflict rejection, scoped deletion | The `PublishStagedRun`, `PersistOccurrences`, `CompleteIndexRunAsync` paths already implement the publication lifecycle. S6.3 is **qualification of existing behaviour**, not new implementation. |
| `LatticeCodeGraphStore` read paths (`FindOccurrencesAsync`, `GetLatestPublicationAsync`, etc.) | **No** — existing snapshot isolation via `_writeGate` and `_outcomeUnknown` quarantine | Queries already wait for write gate, refuse uncertain state. Deterministic ordering via `OrderBy` in `PersistedOccurrencePage` construction. |
| Occurrence index rebuild/reopen (`RestoreOccurrences`, `RebuildInnerFromDurableStateAsync`) | **No** — existing logic rebuilds from authoritative native tables | `RestoreOccurrences` reads `OccurrenceMeta`, `OccurrenceUnit`, `OccurrencePage` — semantically equivalent to accepted publication. |
| New public API surface | **No** | S6.3 adds no new public API; qualification uses existing interfaces. |
| Dependency upgrade (LatticeDbSharp) | **No** — pinned at 0.2.0 | No upgrade authorized; all evidence against pinned dependency. |

**Conclusion**: S6.3 is **documentation + test infrastructure only** — no production code changes, no dependency changes, no behavioural changes. The implementation already satisfies the D4 invariants and S-02 publication correctness; S6.3 provides the **independent production-backed qualification evidence**.

### 6.2 Test infrastructure required

| Component | Status | Notes |
| --- | --- | --- |
| S6.3 dedicated test suite (`NativePublicationCorrectnessTests.cs`) | **New** — primary deliverable | ~15–20 test cases covering D4-1…D4-10 (except D4-5) and S-02-1,3,4,5,6 |
| Multi-process worker process (for D4-10 / S-02-5) | **Extend existing** | Reuse S6.2 `Andirit.LatticeDb.Tests.Worker.exe` pattern; fix D-05 logic (call `CompleteIndexRunAsync`) |
| Fault-injector for supersession/reopen scenarios | **Extend existing** | Reuse `"before-commit"`/`"after-commit"` seams; add `"before-publication"`/`"after-publication"` if needed |
| Genuine hosted Linux execution | **Mandatory** | CI `ubuntu-latest` runner; Windows evidence does not substitute (Q-S61-5, Q-S62-5) |
| Native state assertion helpers | **Reuse** | `NativePayloads(path, label)` pattern from S6.1/S6.2 — read `HetuStage`, `HetuUnit`, `AndiritOccurrenceUnit`, `AndiritOccurrencePage`, `AndiritOccurrenceMeta` directly |
| Evidence package sealing | **Reuse** | Self-excluding SHA-256 manifest pattern established |

### 6.3 Platform evidence requirements

| Platform | Evidence class | Required for S6.3 |
| --- | --- | --- |
| Windows x64 | Local execution | **Yes** — all test cases |
| Linux x64 (genuine hosted) | GitHub Actions `ubuntu-latest` | **Yes** — all test cases (Q-S61-5, Q-S62-5) |
| macOS arm64 | Content only (no runtime) | **No** — remains unqualified (Q-S61-4, Q-S62-5); record limitation |

---

## 7. Implementation boundary (what S6.3 does NOT include)

| Excluded | Reason |
| --- | --- |
| Production code changes | Not needed — existing implementation covers D4/S-02 |
| Dependency upgrades (LatticeDbSharp) | Pinned at 0.2.0; no upgrade authorized |
| Crash/power-loss/abrupt-kill simulation | Not qualified (Q-S62-1); managed fault injector is not crash |
| Multi-process durability beyond file-locking serialization | S6.2 D-07 covers genuine abrupt termination; S6.3 tests concurrency/republication only |
| D3 full real-fixture corpus | S6.4 |
| Platform certification matrix (full Windows/Linux/macOS) | S6.5 |
| Optional cross-target scope / public FindUsages / optimization | Unauthorized (DF-7) |
| Package publication, tag changes, push | Not authorized by this commission |
| Historical findings closure | No acceptance threshold changes; no sealed evidence modification |
| New public API surface | No API changes required for qualification |

---

## 8. Independent qualification plan (summary)

Full plan: `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-qualification-plan.md`.

### 8.1 Gate structure (5 execution gates)

| Gate | Requirement | S6.3 application |
| --- | --- | --- |
| **G-1 Implementation** | Correct implementation against contracts; docs-only; no dependency/workflow/threshold change; whole solution builds/tests clean | **S6.3 = test suite + documentation only**; 0 production changes |
| **G-2 Qualification** | All test cases executed; genuine Linux on genuine hosted runner bound to sealed commit; no stop condition | **~15–20 cases** on Windows x64 + hosted `ubuntu-latest`; D-05 logic fixed |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70), CI gates unchanged and green on all 3 platforms | All existing suites + S6.1/S6.2 suites must pass; thresholds met |
| **G-4 Evidence** | Evidence packages sealed with self-excluding SHA-256 manifests; production delta = test + doc paths only | New evidence package; lineage extended; byte-exact |
| **G-5 Independence** | Reviewer session ≠ producer session; independent contract re-derivation; wider superset enumeration | Separate qualification commission after implementation |

### 8.2 Test case matrix (preliminary)

| Case | D4 invariant / S-02 | Description | Evidence class |
| --- | --- | --- | --- |
| P-01 | D4-1 | Query refuses stale occurrence index (generation mismatch) | Constructed (fault-injector) |
| P-02 | D4-2 | Query refuses unbound/unverifiable index (missing meta) | Constructed |
| P-03 | D4-3 | Query refuses to scan full store when index stale; returns explicit refusal | Constructed |
| P-04 | D4-4 | Failed staging (pre-commit validation fail) preserves prior publication + index | Constructed (fault-injector) |
| P-05 | D4-6 | Superseded publication (older generation) rejected; newer generation retained | Genuine (concurrent writers) |
| P-06 | D4-6 | Generation regression attempt (re-publish older run) refused | Constructed |
| P-07 | D4-7 | Ordinary reopen (no rebuild) produces equivalent query results | Genuine |
| P-08 | D4-7 | Full rebuild/reopen produces semantically equivalent index | Genuine |
| P-09 | D4-8 | Contributor union: identical facts merged; conflicting payload rejected | Constructed |
| P-10 | D4-8 | Scoped deletion removes only affected target context; index clean | Constructed |
| P-11 | D4-9 | Bounded occurrence query: deterministic site order, explicit limit honored | Constructed |
| P-12 | D4-10 | Concurrent publication: queries see snapshot of accepted generation | Genuine (multi-process) |
| P-13 | S-02-3 | Supersession stress: rapid sequential publications; only latest accepted | Genuine |
| P-14 | S-02-4 | Unknown-outcome quarantine + reopen corroboration (occurrence index) | Constructed (fault-injector) |
| P-15 | S-02-5 | Native concurrency: first writer commits; others fail/wait; state consistent | Genuine (multi-process, fixed D-05 logic) |
| P-16 | S-02-6 | In-process interleaving hook recorded as diagnostic only | Documentation |

### 8.3 Constructed vs. genuine evidence classification

| Class | Cases | Rationale |
| --- | --- | --- |
| **Constructed (deterministic)** | P-01, P-02, P-03, P-04, P-06, P-09, P-10, P-11, P-14, P-16 | Fault-injector places failure at precise checkpoint; not timing-dependent |
| **Genuine (arbitrary arrival)** | P-05, P-07, P-08, P-12, P-13, P-15 | No seam control; OS/file-locking serialization; genuine concurrent writers |

### 8.4 Stop conditions

Qualification halts if:
1. Any test case fails on Windows x64 or hosted `ubuntu-latest` (no platform substitution).
2. Evidence package cannot be sealed byte-exact with self-excluding manifest.
3. Production equivalence (metadata/IL/package) shows non-provenance differences.
4. Historical suite regression or coverage threshold breach.
5. New dependency, workflow, or threshold change introduced.

---

## 9. Formal disposition and qualifications

**`ENTRY AUTHORIZED WITH CONDITIONS`**

S6.3 entry is authorized with the following conditions:

| ID | Condition |
| --- | --- |
| **C-S63-1** | **No production code changes.** S6.3 is qualification of existing behaviour only. Any production change requires a separate, explicitly authorized implementation commission. |
| **C-S63-2** | **LatticeDbSharp 0.2.0 remains pinned.** No dependency upgrade authorized. All evidence against the pinned assembly and native assets. |
| **C-S63-3** | **D-05 test logic must be fixed for S6.3 concurrency scenario.** The S6.2 D-05 skip is a test defect (writer doesn't call `CompleteIndexRunAsync`); the corrected test is required for P-15. |
| **C-S63-4** | **Genuine hosted Linux evidence is mandatory.** `ubuntu-latest` CI runner required for all test cases; Windows evidence does not substitute for Linux (Q-S61-5, Q-S62-5). |
| **C-S63-5** | **Crash/power-loss/abrupt-kill/multi-process durability is not simulated.** The managed fault injector is not crash (Q-S62-1, F-02, DF-4). S6.3 tests concurrency/republication via file-locking serialization only. |
| **C-S63-6** | **osx-arm64 remains unqualified (content evidence only).** No runtime qualification on macOS arm64; limitation recorded explicitly. |
| **C-S63-7** | **In-process deterministic interleaving is diagnostic only.** Not accepted as native atomicity evidence (CI1A spec line 194). |
| **C-S63-8** | **No package publication, tag changes, or push.** Documents and evidence committed locally only. |
| **C-S63-9** | **All 13 existing lineage packages remain byte-exact.** New evidence package extends lineage; 4-file pre-existing gap recorded, not repaired. |
| **C-S63-10** | **S6.1/S6.2 qualifications and findings carried forward.** F-QU-01 (D-05 test fix) tracked as S6.3 follow-up; F-QU-02 discharged. |

**Supported qualifications carried forward**: Q-S61-1…Q-S61-8, Q-S62-1…Q-S62-9, Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8, and F-QU-01…F-QU-08.

---

## 10. Evidence package and integrity

Package: `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-evidence/` — self-excluding SHA-256 manifest (`manifest.json`), `.gitattributes` (`* -text` / `**/* -text`), `.gitignore` (`!*` / `!**/`), reproducible probe harness (if any), and raw receipts.

| Artifact | Content |
| --- | --- |
| `contract-reconciliation.txt` | Byte-exact governing-contract excerpts backing every citation in the traceability matrix and obligation inventory |
| `preflight.txt` | Preflight receipt (Git objects, CI run, tag, working tree, evidence lineage) |
| `obligation-inventory.md` | This commission's obligation inventory (standalone copy) |
| `traceability-matrix.md` | This commission's traceability matrix (standalone copy) |
| `feasibility.md` | This commission's feasibility assessment (standalone copy) |
| `implementation-proposal.md` | This commission's implementation proposal (standalone copy) |
| `qualification-plan.md` | This commission's qualification plan (standalone copy) |
| `successor-commission.md` | Proposed successor commission (standalone copy) |

Integrity: the manifest lists every file with its byte length and SHA-256 and excludes itself; the package is bound to the repository commit that carries it (`359a9e8a8768d4cc9204e898ef76f0b4eba38d47`).

---

## 11. Repository state and protected scope

- Baseline before this commission: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (HEAD); the commission adds **documentation and evidence only** and is committed locally on `main`.
- **No push** was performed or authorized.
- No production code, test code, dependency, or workflow was modified. Any probe harness is a standalone, non-packed project under the evidence directory, excluded from `Andirit.slnx`; it references the production projects and never modifies them.
- No package was published; no release tag was deleted, moved or recreated; the version was not incremented; no force-push or history rewrite; no historical Hetu package was deleted, unlisted or deprecated; no qualification branch was deleted.
- S6.4/S6.5 not begun.

---

## 12. Successor commission

**`ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01`** is proposed as the next implementation commission (`ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-successor-commission.md`). It is owner-gated and bounded to:

1. Add the S6.3 dedicated native publication correctness test suite (`NativePublicationCorrectnessTests.cs`) covering the test case matrix in §8.2.
2. Fix the D-05 test logic (call `CompleteIndexRunAsync` before testing concurrent readers).
3. Execute the suite on `win-x64` (local) and genuine hosted `linux-x64` (GitHub Actions `ubuntu-latest`).
4. Seal the native publication correctness evidence package with self-excluding SHA-256 manifest.
5. Submit the result for independent review and owner acceptance.

It does **not** include: dependency upgrades, production code changes, new public API, crash/power-loss simulation, S6.4/S6.5, publication or tag changes, push.

---

**Recorded**: 2026-11-24
**Canonical branch**: `main`
**HEAD**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47` (local, 10 commits ahead of `origin/main`, not pushed)
**Next commission**: `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` (this) → `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01` (pending owner approval)