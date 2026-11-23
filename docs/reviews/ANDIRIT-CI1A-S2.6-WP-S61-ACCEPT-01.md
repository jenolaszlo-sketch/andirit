# ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01

**Date**: 2026-11-22
**Gate**: Stage 6 work package **S6.1** — owner acceptance of the native cancellation contract
implementation and independent qualification, plus controlled S6.2 entry authorization.
**Mode**: documentation and decision only. No production code, no test, no dependency, no workflow,
no threshold and no requirement changed; no finding remediated; nothing published; no tag changed;
**no push** performed.

**Formal dispositions**:
- **S6.1 — ACCEPTED WITH QUALIFICATIONS**
- **S6.2 — ENTRY AUTHORIZED WITH CONDITIONS**

> Owner acceptance is recorded as a deliberate decision, not inferred from test execution. S6.2
> entry is authorized only with the conditions stated in §6. The next commission is
> `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01`.

---

## 1. Acceptance basis — all gates verified

The independent qualification commission (`ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01`, disposition
**S6.1 — QUALIFIED WITH QUALIFICATIONS**) satisfied all five execution gates. I have re-verified
each gate against the qualification evidence, not the producer's assertions.

| Gate | Requirement | Independent verification |
| --- | --- | --- |
| **G-1 Implementation** | C-1…C-4 applied; `src/` differs only in documentation; no dependency, workflow, test-expectation or threshold change; whole solution builds, formats and tests clean | Confirmed. `git diff 2d1e8ec..0162494 -- src tests .github` = 3 paths (2 doc files + 1 new test); 0 `.csproj`/`.yml` changes. Release build 0 warnings/0 errors. Format `exit 0`. |
| **G-2 Qualification** | Q-01…Q-12 executed; genuine Linux row on genuine hosted Linux bound to sealed commit; Q-12 unchanged; no stop condition | Confirmed. Windows: 88/88 (13 new tests). Hosted ubuntu-latest: CI run `45037630799`, job `137254795969`, `headSha = 0162494` (the implementation revision itself). LatticeDb 75 → 88 passed (delta = 13 `[Fact]` methods), 0 skipped. Q-12 managed census PASS; reviewer re-censused native ABI (0 cancellation entry points). No stop condition triggered. |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70) and CI gates unchanged and green on all three platforms | Confirmed. Core 345, CSharp 372, TreeSitter 157, LatticeDb 88 — all 0 failed/0 skipped. Thresholds met: core 82.42/70.74 (55), CSharp 95.15/88 (75), TreeSitter 89.85/75.45 (60), LatticeDb 92.87/78.96 (70). All three CI matrix legs green. |
| **G-4 Evidence** | Evidence package sealed with self-excluding SHA-256 manifest; production-delta receipt shows only approved changes | Confirmed. S6.1 implementation evidence: 61 entries, S6.1 qualification evidence: 40 entries. Both sealed with self-excluding manifests, byte-exact against committed Git blobs. Production delta = 3 paths (2 doc files + test file). |
| **G-5 Independence** | Reviewer session ≠ producer session; reviewer re-derived expectations from contract excerpts | Confirmed. The qualification report states the reviewer "re-derived the expectations from the contract excerpts rather than from the producer's test code" and did "not reuse the producer's adjudication table". The reviewer's wider superset enumeration (58 candidates vs producer's 33) and independent probe instrument confirm session independence. |

**Repository identities verified**:
- Entry baseline: `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157`
- Implementation (pushed): `01624943233f16447c3176b7c905c544642dfb2c`
- Qualification HEAD (local): `f8f430b1faf4b3326c723ccb9f5a057228f42fb4`
- `origin/main` = implementation revision; tag `v0.2.0-preview.7` unchanged (object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183`)
- Working tree clean; qualification evidence HEAD is 2 ahead of `origin/main`, not pushed

---

## 2. S6.1 scope — obligations discharged and preserved

### 2.1 Discharged by S6.1 (evidence-backed)

| Obligation | Source | How discharged | Evidence |
| --- | --- | --- | --- |
| **M9.5** — Dedicated native cancel-path test | S2.5 qualification matrix §9; Stage 1 cancellation behaviour preserved | 13 dedicated tests (`NativeCancellationPathTests`) executing against production `LatticeCodeGraphStore` over real LatticeDB, asserting native `HetuStage`/`HetuUnit` directly | 13/13 PASS on Windows; 13/13 in hosted 88-count aggregate; independent reviewer probe confirmation |
| **M12.2** — D4 invariant 5 native cancelled-staging case | S2.5 qualification matrix §12; explicitly deferred to Stage 6 | Q-01…Q-04 assert native durable state (`HetuStage`/`HetuUnit`) after cancellation scenarios | Q-01…Q-04 PASS on both platforms; reviewer PROBE C confirms whole-unit atomicity with 300/300 nodes |
| **D4 invariant 5** (cancellation path) — cancelled staging exposes no partial facts/index entries | Owner decision §D.4 | Non-exposure proven by Q-01 (pre-native), Q-02/Q-04 (racing/in-window), Q-06 (reopen preservation); PROBE D confirms 0 facts/0 publications exposed to concurrent reader during pending cancellation | Windows + hosted receipts; reviewer PROBE C/D/G |
| **D4 qualification clause** — reproduce applicable cancellation tests using actual production storage and APIs | Owner decision §D.4 | Whole suite runs against production `LatticeCodeGraphStore` over real LatticeDB; durable state read directly through `LatticeDbSharp` | Both platforms; 0 mocked/managed-only paths |
| **S-02** — native matrix includes a cancellation path and an unknown-outcome case | CI1A specification; S-01 obligation reconciliation | Cancellation path: Q-01…Q-04, Q-06, Q-07. Unknown outcome: Q-11 (`CodeGraphCommitOutcomeUnknownException`, quarantine, reopen corroboration) | Q-01…Q-04, Q-06, Q-07, Q-11 PASS on both platforms |
| **I-04 / Q2-F09 cancellation aspect** — cancellation policy specified | Obligation register; owner decision §G.2 | C-1 (store contract) and C-2 (interface contract) publish the policy: two honoured checkpoints, non-cancellable native window, three truthful outcomes, await obligation | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` + `src/Andirit/ICodeGraphStore.cs`; API surface 0 diff |

### 2.2 Explicitly NOT discharged — deferred to later work packages

| Obligation | Why deferred | Owning package |
| --- | --- | --- |
| **M12.1, M12.3** — remaining full-native-matrix rows | Different rows of the ten-invariant matrix; S6.1 covers cancellation path only | **S6.3** |
| **Complete S-02 / D4.5 matrices** | S-02 requires all ten invariants exercised natively; S6.1 discharges only cancellation + unknown outcome | **S6.3** |
| **DF-4** — crash, power-loss, multi-process durability | Managed fault injection is not crash/power-loss/abrupt-kill durability | **S6.2** |
| **DF-6** — full platform certification | Only `win-x64` and genuine hosted `linux-x64` in scope; macOS native runtime evidence unavailable (Q-S61-4) | **S6.3/S6.5** |
| **I-04 aspects other than cancellation** | Cancellation policy is specified; remaining obligation aspects unchanged | **S6.3** |
| **Q-S61-4** — osx-arm64 unqualified | Pinned dependency ships osx-arm64 asset as candidate RID only; no supported-capability declaration | **S6.5** (if macOS native runtime becomes available) |

**No broader cancellation guarantees are claimed.** The contract explicitly limits outcomes to three truthful values and states the native window is non-cancellable.

---

## 3. Finding adjudication — owner-level decisions

| Finding | Disposition | Justification | Follow-up required |
| --- | --- | --- | --- |
| **F-1** — Third managed checkpoint (`InMemoryCodeGraphStore.StageIndexUnitAsync`) not enumerated in contract | **ACCEPT WITH DOCUMENTATION FOLLOW-UP** | The outcome taxonomy and D4 invariant-5 guarantee are unaffected: it is still "cancelled before native invocation". A bounded documentation erratum should enumerate the third checkpoint. | Bounded erratum to C-1/C-2 contract text under a separate owner-authorized change set. No production change. |
| **F-2** — Q-06 covers only first plan scenario (pre-native cancelled staging); completed-but-cancelled-request scenario missing from committed suite | **ACCEPT WITH REQUIRED REGRESSION FOLLOW-UP** | The independent reviewer executed the second scenario and it passes (publication identity, snapshot identity, consistency flag and unit count preserved across reopen). The invariant holds; the coverage gap is real and must be closed in the committed suite. | **Required**: add the second Q-06 scenario to `NativeCancellationPathTests.cs` under a future owner-authorized change set. Track explicitly. Not a reason to reopen the qualification. |
| **F-3** — Q-12 covers only managed census; native-ABI half not in committed test | **ACCEPT — DISCHARGED BY REVIEWER** | The reviewer independently re-censused the native ABI (0 lattice-namespace cancellation entry points, asset hashes match entry receipt). The dependency is provably unchanged. | None required — the qualification evidence covers it. The committed test could be extended in a future change set for completeness. |
| **F-4** — Q-02 expected outcome ("returns success, never OCE") unachievable for untimed canceller; invariant framing is correct | **ACCEPT — PLAN DEVIATION JUSTIFIED** | The producer's invariant framing (accept either truthful outcome, never a partial one) is the correct operationalization. PROBE A shows Q-02 schedule landed both sides; no partial state in either. | Record the deviation; no corrective work needed. |
| **F-5** — Q-10 mapping divergence (plan = Stage-2 regression preservation; test = store-semantics control) | **ACCEPT — TRACEABILITY LABELLING ONLY** | The substance is discharged by unchanged suite counts (345/372/157/88) and thresholds on both platforms. The label mismatch does not affect the discharge. | None required. Future traceability matrix update could align labels. |
| **F-6** — Producer's §6/ADR §7 quote Windows coverage for three suites under "hosted" heading | **ACCEPT WITH ERRATUM** | Committed hosted artifacts are correct (82.42/70.74, 95.11/87.85, 89.89/75.95 — matching CI's coverlet summary). All values clear thresholds under both sets; no gate conclusion changes. | Bounded erratum in producer's record under separate authorization. |
| **F-7** — Producer's package/API receipts produced at baseline HEAD (provenance stamps match) | **ACCEPT — EVIDENCE REPRODUCIBILITY NOTE** | Rebuilt correctly per revision (2 of 6 package entries differ, all provenance metadata). Independent rebuild confirms 0 metadata/IL differences. | None required. The qualification report records the finding for transparency. |
| **F-8** — Q-08 omits "no temporary artifact remains" assertion | **ACCEPT — HANDLE-RELEASE IS LOAD-BEARING** | The handle-release property (file deletable after disposal) is proven. The sidecar assertion is a completeness nicety; reviewer's probe harness never observed a leak. | Could be added in a future change set for completeness. |

**No finding requires remediation before acceptance.** All are non-blocking and evidence-backed.

---

## 4. Owner acceptance decision

### G-6 Owner Decision: **S6.1 — ACCEPTED WITH QUALIFICATIONS**

**Basis**:

1. **Independent qualification evidence**: The reviewer re-derived the contract from the production source, did not reuse the producer's adjudications, built an independent probe instrument (8 probes, stable across 3 runs), and independently verified Windows and hosted Ubuntu execution.

2. **Contract compliance**: Both honoured checkpoints verified from source; the native write window (lines 910–975) consults no `CancellationToken`; three truthful outcomes exclusive and truthful; await obligation enforced; D4 invariant-5 consequence (non-exposure) behaviourally confirmed.

3. **Native cancellation behaviour**: Cancellation honoured at write gate and pre-native; native window non-cancellable (PROBE C, Q-03); three truthful outcomes truthful (PROBE B 20/20, PROBE F, REG guard); no partial exposure including to concurrent readers (PROBE D/G); disposal waits and releases (Q-07/Q-08).

4. **Platform qualification**: Windows x64 (88/88) and genuine hosted Ubuntu x64 (CI run `45037630799`, job `137254795969`, runner `GitHub Actions 1000004506`, `/usr/bin/bash`, SDK 10.0.404, zero Windows/WSL/Docker indicators) — LatticeDb 75 → 88 (delta = 13 `[Fact]`), 0 skipped.

5. **Production equivalence**: 7 649 metadata+IL lines identical across three assemblies; public API identical; package differences are provenance-only (MVID, PDB checksum, PE timestamp, version-resource commit, nuspec commit stamp).

6. **Explicitly bounded residual risks**: Active native execution not interruptible (Q-S61-1, published, independently re-verified); DF-4 deferred to S6.2; M12.1/M12.3 and full S-02/D4.5 deferred to S6.3; osx-arm64 unqualified (Q-S61-4).

**S6.1 owner acceptance is granted with the eight non-blocking qualifications (Q-S61-1…Q-S61-8 and findings F-1…F-8).**

---

## 5. S6.2 entry authorization

### Decision: **S6.2 — ENTRY AUTHORIZED WITH CONDITIONS**

**Conditions** (all must hold before S6.2 implementation commission is authorized):

1. **G-6 acceptance granted** — satisfied by this record.
2. **S6.1 follow-up obligations tracked** — F-1 (documentation erratum) and F-2 (second Q-06 scenario in committed suite) are recorded as explicit tracked obligations. They do not block S6.2 entry but must be closed under future owner-authorized change sets.
3. **All deferred obligations remain visible** — M12.1, M12.3, full S-02/D4.5, DF-4, DF-6, Q-S61-4 are explicitly preserved in this record (§2.2) and in the qualification report (§8).
4. **S6.2 entry requirements independently satisfied** — the S6.2 entry commission must establish its own feasibility, contract reconciliation and qualification plan per the Stage 6 pattern. This authorization does not grant unrestricted S6.2 implementation or qualification.

**S6.2 scope guidance** (per the Stage 6 entry assessment and residual-risk register):

| S6.2 obligation | Owner expectation |
| --- | --- |
| **DF-4** — crash, power-loss, abrupt-kill, multi-process durability | Real durability evidence (not managed fault injection). Requires new instrumentation and safety scoping. |
| **Managed fault injector limitations** | The injectors remain internal/test-only (I-05). S6.2 must not claim they discharge crash/power-loss durability. |
| **Platform evidence** | `win-x64` and genuine hosted `linux-x64` required; macOS native remains unavailable unless runtime evidence appears. |
| **Dependency boundary** | `LatticeDbSharp` stays at 0.2.0 unless a separately authorized, qualified upgrade commission is approved. |

**S6.2 entry is authorized only for the bounded scope above. No implementation, qualification, publication or tag change is authorized by this record.** The next commission is `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01`.

---

## 6. Residual-risk register (carried forward)

| Risk | Status | S6.1 discharge |
| --- | --- | --- |
| Active native execution not interruptible | Bounded limitation, published (Q-S61-1) | Contract text + Q-03 + PROBE C |
| Cancellation outcome taxonomy incomplete (F-1) | Documentation erratum tracked | Contract text to be updated |
| Q-06 second scenario missing from committed suite (F-2) | Required follow-up tracked | PROBE G passes; suite addition needed |
| Q-12 native-ABI half not in committed test (F-3) | Discharged by reviewer re-census | Could extend test in future |
| DF-4 (managed fault ≠ crash/power-loss) | Deferred to S6.2 | Correctly out of S6.1 scope |
| M12.1/M12.3, full S-02/D4.5 matrices | Deferred to S6.3 | Partly discharged (cancellation + unknown outcome) |
| osx-arm64 native unqualified (Q-S61-4) | Preserved | No claim made |
| Pre-existing 4-file lineage gap | Recorded, not repaired | Outside S6.1 lineage |

---

## 7. Evidence and repository control

### Deliverables created by this commission

| Document | Purpose |
| --- | --- |
| `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md` | This acceptance record |
| `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01-evidence/` | Sealed evidence package (self-excluding SHA-256 manifest) |

### Evidence package contents

| Artifact | Source |
| --- | --- |
| `entry-provenance.txt` | Git-object-derived identity/ancestry verification |
| `gate-verification.txt` | G-1 through G-5 re-verification receipt |
| `scope-matrix.txt` | Discharged/deferred obligations matrix |
| `findings-adjudication.txt` | F-1 through F-8 disposition matrix |
| `acceptance-decision.txt` | G-6 decision receipt |
| `s62-entry-decision.txt` | S6.2 authorization receipt |
| `residual-risks.txt` | Carried-forward risk register |
| `manifest.json` | Self-excluding SHA-256 manifest |

### Repository state

| Item | Value |
| --- | --- |
| Acceptance HEAD (local) | `f8f430b1faf4b3326c723ccb9f5a057228f42fb4` (this commit) |
| Implementation (pushed) | `01624943233f16447c3176b7c905c544642dfb2c` |
| Entry baseline | `2d1e8ec88e397d0c36b7f258eb1a1c0840e56157` |
| `origin/main` | `01624943233f16447c3176b7c905c544642dfb2c` (unchanged) |
| Tag `v0.2.0-preview.7` | object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` (unchanged) |
| Working tree | clean |
| Local-only acceptance HEAD | 2 ahead of `origin/main` (not pushed) |

---

## 8. Next commission

**`ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01`** — Stage 6 work package S6.2 entry assessment:

- Feasibility and contract reconciliation for crash/power-loss/multi-process durability (DF-4)
- Native durability qualification plan (real evidence, not managed fault injection)
- Platform evidence requirements (win-x64 + genuine hosted linux-x64)
- Dependency boundary (LatticeDbSharp 0.2.0 unless upgraded)
- Explicit S6.2 scope bounding (durability only; not S6.3 matrices)

**Owner approval for this commission is pending.** No push is performed. The local acceptance HEAD preserves the record for the owner's final decision.

---

**Recorded**: 2026-11-22
**Canonical branch**: `main`
**Owner acceptance**: **S6.1 — ACCEPTED WITH QUALIFICATIONS** (G-6)
**S6.2 entry**: **AUTHORIZED WITH CONDITIONS** (see §5)
**Next commission**: `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01` (pending owner approval)