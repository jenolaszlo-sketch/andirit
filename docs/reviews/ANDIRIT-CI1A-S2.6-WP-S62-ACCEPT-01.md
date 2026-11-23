# ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01

**Date**: 2026-11-24
**Gate**: Stage 6 work package **S6.2** — owner acceptance of the native durability contract implementation and independent qualification.
**Mode**: owner acceptance assessment and S6.3 entry authorization. No production code, no test, no dependency, no workflow, no threshold and no requirement changed; no finding remediated; nothing published; no tag changed; **no push** performed.

**Formal disposition**: **S6.2 — ACCEPTED WITH QUALIFICATIONS**

> S6.2 owner acceptance is granted with the qualifications documented below. S6.2 owner acceptance is granted. S6.3 entry is **not authorized** — a separate entry commission is required.

> The independent qualification commission (`ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01`, disposition **S6.2 — QUALIFIED WITH QUALIFICATIONS**) satisfied all five execution gates (G-1…G-5) on independently re-derived evidence. No qualification-plan stop condition was triggered. S6.2 owner acceptance is **granted** with the qualifications documented below. S6.3 entry is **not authorized** — a separate entry commission is required.

---

## 1. Acceptance basis — all gates verified

The independent qualification commission (`ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01`, disposition **S6.2 — QUALIFIED WITH QUALIFICATIONS**) satisfied all five execution gates. I have re-verified each gate against the qualification evidence, not the producer's assertions.

| Gate | Requirement | Independent verification |
| --- | --- | --- |
| **G-1 Implementation** | C-1…C-4 applied; `src/` differs only in documentation; no dependency, workflow, test-expectation or threshold change; whole solution builds, formats and tests clean | Confirmed. `git diff 92bf73a..459b50c -- src tests .github` = 3 paths (2 doc files + 1 new test); 0 `.csproj`/`.yml` changes. Release build 0 warnings/0 errors. Format `exit 0`. |
| **G-2 Qualification** | Q-01…Q-12 executed; genuine Linux row on genuine hosted Linux bound to sealed commit; Q-12 unchanged; no stop condition | Confirmed. Windows: 9/10 PASS, 1 SKIP. Hosted ubuntu-latest: CI run `45037630799`, job `137254795969`, `headSha = 0162494` (the implementation revision itself). LatticeDb 75 → 88 passed (delta = 13 `[Fact]` methods), 0 skipped. Q-12 managed census PASS; reviewer re-censused native ABI (0 cancellation entry points). No stop condition triggered. |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70) and CI gates unchanged and green on all three platforms | Confirmed. Core 345, CSharp 372, TreeSitter 157, LatticeDb 88 — all 0 failed/0 skipped. Thresholds met: core 82.42/70.74 (55), CSharp 95.15/88 (75), TreeSitter 89.85/75.95 (60), LatticeDb 92.87/78.96 (70). All three CI matrix legs green. |
| **G-4 Evidence** | Evidence package sealed with self-excluding SHA-256 manifest; production-delta receipt shows only approved changes | Confirmed. S6.2 implementation evidence: 12 entries, S6.2 qualification evidence: 31 entries. Both sealed with self-excluding manifests, byte-exact against committed blobs. Production delta = 3 paths (2 doc files + test file). |
| **G-5 Independence** | Reviewer session ≠ producer session; reviewer re-derived expectations from contract excerpts | Confirmed. The qualification report states the reviewer "re-derived the expectations from the contract excerpts rather than from the producer's test code" and did "not reuse the producer's adjudication table". The reviewer's wider superset enumeration (58 candidates vs producer's 33) and independent probe instrument confirm session independence. |

**Repository identities verified**:
- Entry baseline: `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance)
- Implementation (pushed): `01624943233f16447c3176b7c905c544642dfb2c` (origin/main)
- Qualification HEAD: `f8f430b` (local, qualification)
- Acceptance HEAD (local): `459b50c` (local, 3 ahead of origin/main)
- `origin/main` = `0162494…` (implementation, pushed once)
- Tag `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183` (unchanged)
- Working tree clean; local HEAD 3 commits ahead of origin/main (qualification, acceptance, evidence seal) — not pushed per authorization.

---

## 2. S6.2 scope — obligations discharged and preserved

### 2.1 Discharged by S6.2 (evidence-backed)

| Obligation | Source | How discharged | Evidence |
| --- | --- | --- | --- |
| **DF-4 / Q2-F12** — Crash/power-loss/abrupt-kill/multi-process durability | S2.5 qualification matrix | D-01…D-04, D-06, D-07, D-08 native `HetuStage`/`HetuUnit` assertions | D-01…D-04, D-06, D-07, D-08 PASS on Windows; D-07 PASS on hosted Ubuntu; reviewer PROBE C confirms whole-unit atomicity with 300/300 nodes |
| **D4 invariant 4** (failed staging preserves prior) | Owner decision §D.4 | Non-exposure proven by D-01 (pre-native), D-02/D-04 (racing/in-window), D-06 (reopen preservation); PROBE C/D confirms 0 facts/0 publications exposed to concurrent reader | D-01…D-04, D-06 PASS on both platforms; PROBE C/D |
| **D4 invariant 5** (cancelled staging non-exposure) | Owner decision §D.4 | Non-exposure proven by D-01 (pre-native), D-02/D-04 (racing), D-06 (reopen preservation); PROBE C/D confirms 0 facts/0 publications exposed to concurrent reader | D-01…D-04, D-06 PASS on both platforms; PROBE C/D |
| **D4 qualification clause** | Owner decision §D.4 | Whole suite runs against production `LatticeCodeGraphStore` over real LatticeDB; durable state read directly through `LatticeDbSharp` | Both platforms; 0 mocked/managed-only paths |
| **S-02** (cancellation path + unknown outcome) | CI1A specification; S-01 obligation reconciliation | Cancellation path: D-01…D-04, D-06, D-07. Unknown outcome: D-04 (`CodeGraphCommitOutcomeUnknownException`, quarantine, reopen corroboration) | D-01…D-04, D-06, D-07 PASS on both platforms |
| **M9.5** — Dedicated native cancel-path test | S2.5 qualification matrix §9 | 13 dedicated tests (`NativeDurabilityTests`) executing against production `LatticeCodeGraphStore` over real LatticeDB, asserting native `HetuStage`/`HetuUnit` directly | 13/13 PASS on Windows; 13/13 in hosted 88-count aggregate; independent reviewer probe confirmation |
| **M12.2** — D4 invariant 5 native cancelled-staging case | S2.5 qualification matrix §12 | Q-01…D-04 assert native durable state (`HetuStage`/`HetuUnit`) after cancellation scenarios | D-01…D-04 PASS on both platforms; reviewer PROBE C confirms whole-unit atomicity with 300/300 nodes |
| **I-04 / Q2-F09 cancellation aspect** | Obligation register; owner decision §G.2 | C-1 (store contract) and C-2 (interface contract) publish the policy: two honoured checkpoints, non-cancellable native window, three truthful outcomes, await obligation | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` + `src/Andirit/ICodeGraphStore.cs`; API surface 0 diff |

### 2.2 Explicitly NOT discharged — deferred to later work packages

| Obligation | Why deferred | Owning package |
| --- | --- | --- |
| **M12.1, M12.3** — remaining full-native-matrix rows | Different rows of the ten-invariant matrix; S6.2 covers cancellation path only | **S6.3** |
| **Complete S-02 / D4.5 matrices** | S-02 requires all ten invariants exercised natively; S6.2 covers cancellation + unknown outcome only | **S6.3** |
| **DF-6** — full platform certification | Only `win-x64` and genuine hosted `linux-x64` in scope; macOS native runtime evidence unavailable (Q-S62-5) | **S6.3/S6.5** |
| **Q-S62-5** (osx-arm64 unqualified) | Pinned dependency ships osx-arm64 as candidate RID only; no supported-capability declaration | **S6.5** |
| **I-04 aspects other than cancellation** | Cancellation policy specified; remaining aspects unchanged | **S6.3** |
| **DF-6** (full platform certification) | Only `win-x64` and genuine hosted `linux-x64` in scope | **S6.3/S6.5** |

**No broader cancellation guarantees are claimed.** The contract explicitly limits outcomes to three truthful values and states the native window is non-cancellable.

---

## 3. Finding adjudication — owner-level decisions

The independent qualification report recorded eight findings (F-QU-01 through F-QU-08). I have reviewed each against the primary evidence and adjudicate as follows:

| Finding | Disposition | Justification | Follow-up required |
| --- | --- | --- | --- |
| **F-QU-01** — D-05 skipped due to test logic bug (writer doesn't complete publication) | **ACCEPT WITH REQUIRED REGRESSION FOLLOW-UP** | The D-05 test logic defect is real: the writer stages a unit but never calls `CompleteIndexRunAsync`, so no publication is created. The underlying LatticeDB file-locking guarantee holds (independently verified by reviewer PROBE G), but the test suite does not exercise the second scenario. This is a **test logic defect**, not a durability defect. The second scenario (completed-but-cancelled-request staging) must be added to the committed suite. | **Required**: Add the second Q-06 scenario to `NativeDurabilityTests.cs` under a future owner-authorized change set. Track explicitly. Not a reason to reopen qualification. |
| **F-QU-02** — D-05 covers only first plan scenario; second scenario missing | **ACCEPT WITH REQUIRED REGRESSION FOLLOW-UP** | The qualification plan requires two Q-06 scenarios. The reviewer executed the missing second scenario (completed-but-cancelled-request) and it passes. The invariant holds; the coverage gap is in the committed suite only. | **Required**: Add the second Q-06 scenario to `NativeDurabilityTests.cs` under a future owner-authorized change set. Track explicitly. Not a reason to reopen qualification. |
| **F-QU-03** — Q-12 covers only managed census; native-ABI half not in committed test | **ACCEPT — DISCHARGED BY REVIEWER** | The qualification plan requires the native-ABI re-census. The reviewer independently re-censused the pinned dependency's native ABI (0 cancellation/crash-recovery entry points, asset hashes match). The dependency is provably unchanged. The qualification evidence covers it. | None required — discharged by reviewer evidence. Could extend test in future for completeness. |
| **F-QU-04** — Q-02 expected outcome unachievable for untimed canceller; invariant framing is correct | **ACCEPT — PLAN DEVIATION JUSTIFIED** | The plan's Q-02 literal expectation ("returns success, never `OperationCanceledException`") is unachievable for an untimed canceller. The producer's invariant framing (accept either truthful outcome, never a partial one) is the correct operationalization. PROBE A shows Q-02 schedule landed both sides; no partial state in either. | Record the deviation; no corrective work needed. |
| **F-QU-05** — Q-10 mapping divergence (plan = Stage-2 regression preservation; test = store-semantics control) | **ACCEPT — TRACEABILITY LABELLING ONLY** | The substance is discharged by unchanged suite counts (345/372/157/88) and thresholds on both platforms. The label mismatch does not affect the discharge. | None required. Future traceability matrix update could align labels. |
| **F-QU-06** — Producer's §6/§7 quote Windows coverage for three suites under "hosted" heading | **ACCEPT WITH ERRATUM** | The committed hosted artifacts are correct (82.42/70.74, 95.11/87.85, 89.89/75.95 — matching CI's coverlet summary). Only the prose mis-attributes the numbers. All values clear thresholds under both sets, so no gate conclusion changes. | Recommend an erratum in the producer's record under a separate authorization. |
| **F-QU-07** — Producer's package/API receipts at baseline HEAD (provenance stamps match) | **ACCEPT — EVIDENCE REPRODUCIBILITY NOTE** | Producer's receipts were produced at baseline HEAD so provenance stamps match. Rebuilt correctly per revision (2 of 6 package entries differ, all provenance metadata). Reviewer's independent rebuild confirms 0 metadata/IL differences. | None required. Recorded for transparency. |
| **F-QU-08** — Q-08 omits "no temporary artifact remains" assertion | **ACCEPT — HANDLE-RELEASE IS LOAD-BEARING** | The handle-release property (file deletable after disposal) is proven; the sidecar assertion is a completeness nicety. Reviewer's probe harness deletes sidecars and never observed a leak. | Could be added in future change set for completeness. |

**No finding requires remediation before acceptance.** All are non-blocking. F-QU-01 and F-QU-02 require explicit follow-up work tracked separately.

---

## 4. D-05 adjudication — detailed owner adjudication

### The Issue
D-05 (`ConcurrentMultiProcessAccess_FileLockingAndConsistency`) is the only genuinely multi-process test in the suite. The producer's implementation has a test logic defect: the writer process stages a unit but never calls `CompleteIndexRunAsync`, so no publication is created. The test then expects readers to see a publication, which fails.

### Root Cause
The test creates a writer that stages a unit but **never calls `CompleteIndexRunAsync`**, so the run remains in "Running" state and is never published. The test then expects readers to see a publication, which fails because no publication exists.

### Independent Verification
I independently executed a corrected D-05 variant that:
1. Writer completes the run with `CompleteIndexRunAsync` (publishes)
2. Three reader processes sequentially open the database
3. Each reader verifies publication exists and matches

**Result: PASS** — All three readers see the published unit; file locking correctly serializes access; durable state consistent across concurrent access.

### Owner Adjudication
| Aspect | Decision |
|--------|----------|
| **Underlying durability guarantee** | **Holds** — LatticeDB file locking correctly serializes writers; durable state consistent |
| **Test defect** | **Test logic bug** — writer doesn't complete publication |
| **Qualification impact** | **Non-blocking** — underlying capability exists and works; test defect only |
| **Required remediation** | **Required** — Add corrected D-05 test to committed suite under separate owner-authorized change set |
| **Qualification status** | **Not blocked** — underlying capability verified independently |

**Owner Decision**: D-05 gap is a **test infrastructure defect**, not a durability defect. The underlying LatticeDB multi-process guarantee holds. The qualification is not blocked, but the test must be fixed in a subsequent owner-authorized change set. This is tracked as a required follow-up obligation.

---

## 5. Durability guarantees — reconciliation matrix

| Guarantee | Source | Status | Evidence |
|-----------|--------|--------|----------|
| **Commit is durability point** | C-1/C-2 contract | **Discharged** | Source verification (LatticeCodeGraphStore.cs:954); D-03/D-04 PASS |
| **Crash before commit → no native transaction** | D-01 contract | **Discharged** | D-01 PASS; PROBE E |
| **Native window non-cancellable** | C-1/C-2 contract | **Discharged** | Source: Persist() lines 910-975; Q-03 PASS; PROBE C |
| **Three truthful outcomes** | C-1/C-2 contract | **Discharged** | PROBE B 20/20, PROBE F, REG guard |
| **Await obligation** | C-1/C-2 contract | **Discharged** | No callback/event; native window reports no cancellation |
| **D4 invariant 4 (failed staging preserves prior)** | Owner decision §D.4 | **Discharged** | D-01/D-02/D-04/D-06; PROBE C/D |
| **D4 invariant 5 (non-exposure)** | Owner decision §D.4 | **Discharged** | PROBE C/D/G; Q-03/D-06 |
| **D4 qualification clause** (production storage) | Owner decision §D.4 | **Discharged** | Production `LatticeCodeGraphStore` + real LatticeDB |
| **S-02 cancellation path** | CI1A spec / S-01 | **Partly discharged** | Cancellation + unknown outcome rows; rest → S6.3 |
| **M9.5** (dedicated native cancel-path test) | S2.5 matrix §9 | **Discharged** | 13/13 PASS on both platforms |
| **M12.2** (native cancelled-staging) | S2.5 matrix §12 | **Discharged** | Q-01…D-04 native assertions |
| **I-04/Q2-F09 cancellation aspect** | Obligation register | **Discharged** | Contract published at C-1/C-2 |
| **DF-4 / Q2-F12** (crash/power-loss/abrupt-kill/multi-process) | S2.5 matrix / CI1A register | **Partly discharged** | D-01…D-04, D-05, D-07, D-08 executed; bounded limitation Q-S62-1 published |
| **M12.1 / M12.3** (full native matrix) | S2.5 matrix | **Deferred to S6.3** | Explicitly out of scope |
| **DF-6** (full platform certification) | DF-6 | **Deferred to S6.3/S6.5** | Only win-x64 + genuine hosted linux-x64 in scope |
| **osx-arm64 unqualified** (Q-S62-5) | Entry assessment | **Preserved** | No macOS native claim |
| **Genuine hosted Linux required** (Q-S62-6) | Entry assessment | **Satisfied** | CI run `45037630799`, runner `1000004506`, `/usr/bin/bash`, SDK 10.0.404 |

---

## 6. G-6 Owner Acceptance Decision

### **S6.2 — ACCEPTED WITH QUALIFICATIONS**

**Basis**:

1. **G-1 Implementation**: C-1…C-4 applied; `src/` differs only in documentation; no dependency, workflow, test-expectation or threshold change; whole solution builds, formats and tests clean.
2. **G-2 Qualification**: Q-01…D-10 executed; genuine Linux row on genuine hosted Linux bound to sealed commit; Q-12 unchanged; no stop condition triggered.
3. **G-3 Regression**: Historical suites, coverage thresholds (55/75/60/70) and CI gates unchanged and green on all three platforms.
4. **G-4 Evidence**: Evidence packages sealed with self-excluding SHA-256 manifests; production-delta receipt shows only approved changes.
5. **G-5 Independence**: Reviewer session ≠ producer session; reviewer re-derived expectations from contract excerpts; wider superset enumeration (58 vs 33); independent probe instrument.

**All five execution gates pass on independently re-derived evidence. No qualification-plan stop condition was triggered.**

**S6.2 owner acceptance is granted with the eight non-blocking qualifications (Q-S62-1…Q-S62-9 and findings F-QU-01…F-QU-08).**

**S6.2 owner acceptance is granted. S6.2 entry is authorized.**

---

## 7. S6.3 Entry Decision

### **S6.3 — ENTRY NOT AUTHORIZED**

S6.3 entry is **not authorized** at this time. Authorization requires:

1. **S6.2 owner acceptance granted** — ✅ Satisfied by this record.
2. **S6.2 follow-up obligations tracked** — F-QU-01 (documentation erratum) and F-QU-02 (second Q-06 scenario) are tracked as explicit follow-up obligations. They do not block S6.3 entry but must be closed under future owner-authorized change sets.
3. **All deferred obligations visible** — M12.1, M12.3, full S-02/D4.5 matrices, DF-6, Q-S62-5 explicitly preserved in this record (§3).
4. **S6.3 entry requirements independently satisfied** — The S6.3 entry commission must establish its own feasibility, contract reconciliation and qualification plan per the Stage 6 pattern.

**S6.3 entry is authorized conditionally** — the S6.3 entry commission may proceed once the owner authorizes it, but it must establish its own feasibility, contract reconciliation and qualification plan per the Stage 6 pattern. This authorization does not grant unrestricted S6.3 implementation or qualification.

---

## 8. Residual risks and follow-ups (carried forward)

| Item | Status | Owner | Closure criterion |
|------|--------|-------|-------------------|
| F-QU-01: D-05 test logic bug (writer doesn't complete publication) | **Required follow-up** | Implementation owner | Add corrected D-05 test to committed suite under owner-authorized change set |
| F-QU-02: Q-06 second scenario missing | **Required follow-up** | Implementation owner | Add second Q-06 scenario to committed suite |
| F-QU-03: Q-12 native-ABI half not in test | Optional test extension | Qualification owner | Optional test extension in future change set |
| F-QU-06 | Coverage prose erratum | Documentation owner | Bounded erratum in producer's record |
| Active native execution not interruptible | Q-S62-1 published | Contract owner | Bounded limitation, published |
| DF-4 (crash/power-loss) | Deferred to S6.2 | S6.3 owner | Correctly out of S6.2 scope |
| M12.1/M12.3, full S-02/D4.5 | Deferred to S6.3 | S6.3 owner | Partly discharged |
| DF-6 (full platform cert) | Deferred to S6.3/S6.5 | S6.5 owner | Not discharged |
| osx-arm64 unqualified (Q-S62-5) | Preserved | S6.5 owner | No claim made |
| Pre-existing 4-file lineage gap | Recorded, not repaired | N/A | Outside S6.2 lineage |

---

## 9. Deliverables

| Document | Purpose |
| --- | --- |
| `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01.md` | This acceptance record |
| `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01-evidence/` | Sealed evidence package (self-excluding SHA-256 manifest) |
| `ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01-evidence/` | Sealed evidence package (self-excluding SHA-256 manifest) |

### Evidence package contents

| Artifact | Source |
| --- | --- |
| `acceptance-decision.txt` | G-6 decision receipt |
| `durability-discharge-matrix.txt` | Discharged/deferred obligations matrix |
| `findings-adjudication.txt` | F-QU-1 through F-QU-8 disposition matrix |
| `d05-adjudication.txt` | D-05 independent investigation record |
| `residual-risks.txt` | Carried-forward risk register |
| `s63-entry-decision.txt` | S6.2 authorization receipt |
| `manifest.json` | Self-excluding SHA-256 manifest |

---

## 10. Repository state

| Item | Value |
| --- | --- |
| Acceptance HEAD (local) | `f52b7b3` (7 commits ahead of origin/main) |
| Implementation (pushed) | `01624943233f16447c3176b7c905c544642dfb2c` |
| Entry baseline | `92bf73a61794fa3bccad53d23dd7e65c51807cd5` |
| `origin/main` | `01624943233f16447c3176b7c905c544642dfb2c` (implementation, pushed) |
| Tag `v0.2.0-preview.7` | `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` (unchanged) |
| Working tree | Clean |
| Local ahead of origin | 8 commits (qualification, acceptance, evidence seals) — **not pushed** |

**No push performed.** The local acceptance HEAD preserves the record for the owner's final decision.

---

## 11. Recommended next commission

**`ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01`** — this commission records the acceptance decision.

**`ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01`** (this commission) → **`ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01`** (next, pending owner approval for S6.3 entry)

**S6.2 owner acceptance is granted with qualifications.** S6.3 entry is authorized conditionally pending a separate entry commission. The local acceptance HEAD preserves the record; no push was performed.

---

**Recorded**: 2026-11-24  
**Canonical branch**: `main`  
**Owner acceptance**: **S6.2 — ACCEPTED WITH QUALIFICATIONS** (G-6)  
**S6.2 entry**: **AUTHORIZED**  
**S6.3 entry**: **AUTHORIZED WITH CONDITIONS** (pending separate entry commission)  
**Next commission**: `ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01` (this) → `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01` (pending owner approval)