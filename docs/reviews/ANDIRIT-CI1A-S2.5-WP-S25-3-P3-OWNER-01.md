# ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01 — S-01 owner acceptance and WP-S25-3 closure-readiness decision

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-15. **Canonical branch:** `main`.
**Owner dispositions:**
- **`S-01 — SATISFIED WITH QUALIFICATIONS`** (Disposition B).
- **`WP-S25-3 — READY FOR CLOSURE WITH QUALIFICATIONS`** (Disposition B).

> **Status: RECORDED — OWNER-AUTHORIZED (acceptance and closure-readiness decision only).**
>
> P3 accepts the integrated S-01 proof under `canonical-forms v1 (clarified)` with the
> qualifications recorded below. It does **not** formally close WP-S25-3, does **not**
> authorize WP-S25-4 or Stage-2 acceptance, does **not** close F-G2-01, and does **not**
> satisfy S-02 or D4.5. No production, test, CI, contract, or historical-evidence change
> was made. Nothing was pushed.

---

## 1. Commission and authorization

| Item | Content |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01` — formal P3 owner acceptance of the S-01 integrated proof and WP-S25-3 closure-readiness assessment |
| Authority | Owner commission; P3 gate of the WP-S25-3 entry decision (§9); S-01 closure condition of the S-01 canonicalization disposition (§5/§7); Stage-2 acceptance matrix §1–§2/§16; G-ACCEPT §18; S2.5 entry decision §8 |
| Starting point | HEAD `0c0bab00586ad65bcc290d304234d39055a0ec6a`, tree `66e112b77b08679d26aa6b30b32dc718626d8c55` — the P2 qualification commit |
| Permitted | Read-only repository/evidence examination; targeted verification; additive owner-decision documentation; the authorized additive work-package-plan status update |
| Prohibited | Production/test/harness/project/package/CI/contract changes; historical-evidence modification; F-G2-01 closure; S-02/D4.5 satisfaction; formal WP-S25-3 closure; new work-package implementation; branches/worktrees; fetch/push; remediation |
| Independence of this decision | This is an owner decision based on the sealed P1/delta/P2 evidence. Authorization to conduct P3 is **not** automatic acceptance; every disposition below is evidence-based |

**Authorization boundary.** The owner authorized P3 after P2 because P2 provides sufficient
evidence to proceed to owner acceptance. That authorization does not convert P2's
`SUPPORTED WITH QUALIFICATIONS` into an unconditional pass, does not waive any qualification,
and does not authorize any successor work. This decision issues exactly one S-01 disposition
and exactly one WP-S25-3 closure-readiness disposition; neither is a formal closure.

---

## 2. Repository preflight and provenance

Read-only preflight was performed before any change. No fetch, GitHub query, remote contact,
branch creation, worktree creation, reset, clean, rebase, or push occurred.

| Check | Expected | Observed | Result |
| --- | --- | --- | --- |
| HEAD | `0c0bab00586ad65bcc290d304234d39055a0ec6a` | `0c0bab00586ad65bcc290d304234d39055a0ec6a` | exact |
| Tree | `66e112b77b08679d26aa6b30b32dc718626d8c55` | `66e112b77b08679d26aa6b30b32dc718626d8c55` | exact |
| Branch | `main` | `main` | exact |
| Working tree | clean | clean | exact |
| Worktrees | one | one (`D:/projects/Andirit`) | exact |
| Stashes | none | none | exact |
| Recorded local `origin/main` ref | `87097242d1610689c388d6814be06fef8cc89d89` | `87097242d1610689c388d6814be06fef8cc89d89` | exact |
| Commits ahead of that ref | two | two (delta `976fe52`, P2 `0c0bab0`) | exact |

**Commit ancestry (all verified ancestors of HEAD):**

| Commit | Identity | Content |
| --- | --- | --- |
| P0 contract | `349136db53c811c4e648e7986865a60991c2e232` | canonical-forms v1 adoption |
| P1 proof | `90c5fbee…` | integrated S-01 P1 proof |
| Clarification | `87097242d1610689c388d6814be06fef8cc89d89` | P1 finding disposition + addendum |
| P1 delta | `976fe52a1fef1a69e3d68a7a4227a9337524274c` | bounded delta proof |
| P2 qualification | `0c0bab00586ad65bcc290d304234d39055a0ec6a` | independent P2 qualification |

**Protected-area verification (all unchanged):**

| Area | Baseline | Result |
| --- | --- | --- |
| `src`, `tests`, `.github` | since P0 `349136d` | unchanged |
| Canonical-form contract v1 | since P0 `349136d` | byte-unchanged |
| Clarification addendum + clarification owner decision | since `8709724` | byte-unchanged |
| P1 report/tests/evidence package | since P1 `90c5fbe` | byte-unchanged |
| Delta report/tests/evidence package | since delta `976fe52` | byte-unchanged |
| P2 report/tests/evidence package | since P2 `0c0bab0` | byte-unchanged (P2 manifest 26/0) |

**Provenance statement.** P3 reviewed the committed P1 proof, the approved clarification, the
P1 delta proof, the P2 qualification report and harness, and the sealed P1/delta/P2 evidence
manifests. The P2 manifest records the reviewed code state, not the P2 commit:
`reviewedHead=976fe52a1fef1a69e3d68a7a4227a9337524274c`,
`reviewedTree=c0089a29ee56bbb89936a4cf77963d46c49222c2`. This is intentional and is the
correct provenance binding for a qualification whose additive artifacts were committed after
the reviewed code state.

---

## 3. Governing requirements

### 3.1 Authority set

| Authority | What it binds for P3 |
| --- | --- |
| [`s01-canonicalization-disposition.md`](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 | S-01 closure condition: implement and independently qualify canonical source derivation of `identifierDigest`/`declarationDigest`/`shapeDigest` from the verified live compilation, and prove the full S-01 chain end to end before Stage-2 owner acceptance |
| [`stage2-acceptance-matrix.md`](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §1–§2, §16 | Binding S-01 source-byte provenance and semantic-identity rows; S-01 Provenance and Semantic Identity are blocking Stage-2 acceptance gates |
| [`adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md) §18 | S2.5 independent integrated source/compilation/semantic proof, including general source canonicalization of the component digests and the integrated S-01 chain |
| [`adr-ci1-01-ci1a-s2-5-entry-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-5-entry-owner-01.md) §8 | Minimum integrated-proof contract: canonical component-digest inputs/serialization; independently derived expectations; semantic reproduction; publication/authority; recovery/reopen; native platform coverage; D3 noninterference; applicable D4/D5; failure/refusal; evidence provenance |
| [`work-package-plan.md`](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) WP-S25-3 | Package scope, independence requirements, required evidence, blocking failures, owner-acceptance gate, exclusions |
| [`s01-canonical-form-contract.md`](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md) v1 + [`s01-canonical-form-v1-clarification-01.md`](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md) | Normative canonical forms; E-1…E-4 clarification effective 2026-11-11; R13 corrected relations |
| [`adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md) | F-P1-01/F-P1-03 editorial; F-P1-02 normatively clarified; no version bump/remediation/re-ingestion; delta required before P2 |
| [`adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md) §7/§10 | Applicable D3/D4/D5 scope; D3-full/D4.5/full S-02 remain Stage 6; F-02 managed-fault boundary; F-G2-01 open but independent |

### 3.2 The eleven-component S-01 chain

The entry decision §3 decomposes S-01 into eleven verifiable components. P3 uses that
decomposition as the acceptance structure; each component is mapped to evidence and a
disposition in §5.

| # | Component | Governing requirement | Required proof |
| --- | --- | --- | --- |
| 1 | Canonical source inputs | S-01 §F; S2.1; matrix §1.1 | Adversarial invariance/difference cases bound to the adopted forms |
| 2 | Canonical serialization | Identity contract; matrix §2.1 | Known-answer vectors recomputed by an independent implementation |
| 3 | `identifierDigest` derivation | Identity contract; v1 §3 | General-source independent reproduction |
| 4 | `declarationDigest` derivation | Identity contract; v1 §4 | General-source independent reproduction |
| 5 | `shapeDigest` derivation | Identity contract; v1 §5 clarified | General-source independent reproduction |
| 6 | Independent semantic reproduction | G-ACCEPT §18; entry §8 | Independent oracle; reviewer ≠ implementer |
| 7 | Real-source extraction/ingestion | S2.2/S2.3 accepted path | Chain re-execution with digest-level assertions |
| 8 | Publication/authority binding | D4; S2.3 §5; matrix §1.2/§1.3/§2.2 | Digest-level persisted-row verification |
| 9 | Durable reopen/recovery | D4.4/D4.7; NEW-01 | Post-reopen identity equality; managed recovery |
| 10 | D3/D4/D5 behaviors | entry §7 | Applicable matrix; carried items explicit |
| 11 | Evidence provenance/oracle | MD-01; entry §8/§10 | Self-excluding manifest; fresh/reused classification |

### 3.3 Binding Stage-2 matrix rows

| Row | Requirement |
| --- | --- |
| §1.1 | Descriptor hashes actual indexed source bytes; changed source bytes → new `compctx1`/`asmctx3`/`usage4`; supplied bytes ≠ indexed bytes → refusal |
| §1.2 | Compilation options/references in context; changed options → new `compctx1`; missing reference → refusal |
| §1.3 | Semantic binding against the verified compilation; cross-project target binds to the dependency's verified compilation; target in missing compilation → refusal |
| §2.1 | Exact `usage4` tuple/encoding; frozen vectors independently recompute; same semantic fact via two storage contributors retains ID; forged ID refused |
| §2.2 | Exact `asmctx3` composition; frozen components and valid cross-project defining context reproduce target identity; different context/component → different target; unavailable/external/unbound → refusal, no fact |
| §2.3 | Storage contributor unit excluded; plugin and semantic source unit included; two storage units → one fact, two contributors |
| §2.4 | Payload hash detects conflict; same ID with different immutable payload → rejection |

### 3.4 Clarified R13 expectation

Under the approved addendum E-3, the R13 relations are: identifier `=` for all mode pairs;
declaration `≠` for value-vs-mode and `=` within `ref`/`out`/`in`; shape `=` for all mode
pairs; composed-target distinctness across revisions is carried by the defining-compilation
context (`compctx1`), and value-vs-mode distinctness is additionally carried by the
declaration form (`@`). The P1 historical failures against v1-as-written remain recorded and
are not converted into passes.

---

## 4. Evidence integrity verification

### 4.1 Integrity checks

| Check | Result |
| --- | --- |
| P0/P1/clarification/delta/P2 ancestry | all true |
| P2 reviewed HEAD/tree | `976fe52a…` / `c0089a29…` — the reviewed code state |
| P2 manifest | 26 entries, 0 mismatches, self-excluding SHA-256 over exact bytes; no unlisted/extra files |
| P1 manifest | 23 entries, 0 mismatches; `reviewedHead=349136d…` |
| Delta manifest | 17 entries, 0 mismatches; `reviewedHead=8709724…` |
| P2 evidence bytes vs manifest | exact; re-verified after the P2 commit |
| Historical P1 outcomes preserved | 10/10 KATs; 17/17 forms; matrix 15 pass / 3 fail (R13a/b/c) preserved; no resealing |
| Delta pairs recomputed | 4/4 from recorded primitives; no reconstruction of missing historical reference identities |
| Unauthorized changes | none: `src`, `tests`, `.github`, contract, clarification, P1 package, delta package, P2 package all unchanged |

### 4.2 Reconciliation of the reported qualification results

| Reported result | Evidence | Verified value | Status |
| --- | --- | --- | --- |
| Part A 70/70 | `Q1-P2A.json` `checkSummary`, `allPass` | `70/70`, `allPass=true`, PID 25400 | reconciled |
| Artifact audit 190/190 | `Q5-ARTIFACT-AUDIT.json` | `190/190`, `allPass=true` | reconciled |
| Historical P1 10/10 KATs | `Q5` `p1.knownAnswers` | 10 | reconciled |
| Historical P1 17/17 forms | `Q5` `p1.formCases` | 17 | reconciled |
| Historical matrix 15/3 preserved | `Q5` `p1.matrix`, `matrixPass`, `matrixFail` | 18 rows: 15 pass / 3 fail (R13a/b/c) | reconciled |
| Four delta pairs recomputed | `Q5` `delta.pairs` | 4/4; 55/55 producer checks recorded, not used as a P2 pass condition | reconciled |
| Part B integration | `Q2-INT.json` | PID 23112; 20 batches; 4 authority envelopes; 1 meta / 20 pages / 4 units / 4 graph units; both targets present | reconciled |
| Part B reopening | `Q3-REOPEN.json` | PID 23884; `Available/8`; `Available/2`; `StalePublication/0`; `AnalysisUnavailable/0`; invalid target refused | reconciled |
| Part B recovery | `Q4-RECOVERY.json` | PID 12368; baseline `Available/8`; known fault preserves `Available/8`; unknown outcome quarantines; reconcile `run:review3`; tamper refused `CodeOccurrenceIndexException` | reconciled |
| Regression tests 907/0/0 | `full-solution-tests.log` | 52 + 157 + 345 + 353 = 907 passed, 0 failed, 0 skipped | reconciled |
| Focused refusal tests 8/0 | `focused-historical-tests.log` | 8 passed, 0 failed | reconciled |
| Frozen oracle byte-identical | `oracle-r2.json`, `oracle.log` | 146,851 bytes; SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` | reconciled |
| Release build 0/0 | `release-build.log` | 0 warnings, 0 errors, exit 0 | reconciled |
| Formatting | `format.log` | exit 0 | reconciled |

### 4.3 P3-F-01 — P2 report narrative values are not traceable to the sealed receipts

This is a **documentation/provenance finding**, not an S-01 requirement failure and not an
evidence-integrity failure. The sealed P2 evidence is internally consistent and the manifest
is intact. The P2 report's narrative absolute `compctx1`/`asmctx3` values and its
reference-closure digest are not the sealed run's values:

| Location | P2 report narrative | Sealed receipt (`Q1-P2A.json` / `Q2-INT.json`) |
| --- | --- | --- |
| Reference closure (181 entries) | digest `4b9637df…` | digest `a5bc0419e461dc434278a6618a1359a9962122cd6152e51173ead670d68a5941`; `referencesOrdered=true`, `matchesProduction=true`, `matchesFrozen=true` on every R13 side |
| R13 value-vs-ref `compctx1` | `6fefb305…` → `e42ff5f4…` | `74137c5c19e6b829…` → `35cb902358f9c8a0…` |
| R13 value-vs-ref composed target | `2e4cef53…` → `d09d744c…` | `1f0b883a938976b8…` → `97612ad461c3c62d…` |
| R13 ref-vs-out composed target | `f56aa218…` → `0ca5278f…` | `238bb137f9e3d79e…` → `360475434f4cdc10…` |
| R13 ref-vs-in composed target | `0d0ae0a5…` → `8159ff46…` | `2cc6c3b73786a539…` → `3ad651e487103b84…` |
| R13 out-vs-in composed target | `442d749d…` → `a25fdad8…` | `b1a3375940be3ef2…` → `523d1f4dc88ccdfb…` |
| Coexistence context / targets | `25901398…` / `f8998daf…`, `bb6c05ed…` | `99f2372a3ce727da…` / `dcb0cbfcd4b32c03…`, `ddce4e094374c568…` |
| Explicit-interface target | `0ab72b5c834227d3…` | `664e7ffe6a2d3b9b…` |
| Part B `Add` / `Greet` targets | `f51f0fbd…` / `0d92fc2d…` | `f6bd5d282c050f70…` / `1577c900e2ef79bf…` |

All **component digests**, all **pass/fail results**, all **counts**, and all **Part B
availability/recovery outcomes** in the report match the sealed evidence. The pattern —
stable component digests with different composed targets/context — is exactly the O-1
host-closure effect: composed targets bind the defining compilation including reference
identities/order, so absolute targets are not invariant across differently built hosts. The
report's narrative values appear to have been taken from a different build closure than the
sealed run; they are not the sealed run's values and must not be cited as such.

**Disposition of P3-F-01:** the sealed evidence governs. The historical P2 report is not
revised in this commission (prohibited). The report's absolute target/context values and its
`4b9637df…` closure digest are **not used** for acceptance. An additive erratum is required
before the P2 report is used downstream as the authoritative narrative for those values. This
finding does not change any S-01 requirement result; it is carried as a qualification on the
P2 report's narrative accuracy and on WP-S25-3 closure readiness.

---

## 5. Acceptance matrix

The matrix maps each governing S-01 requirement to evidence, the relevant P1/P2 result, the
applicable qualification, and the owner disposition. It is the owner's acceptance map; it is
not a new qualification campaign.

### 5.1 S-01 chain components

| # | Governing requirement | Supporting evidence | P1/P2 result | Qualification/limitation | Owner disposition |
| --- | --- | --- | --- | --- | --- |
| A1 | Canonical source inputs: bytes hashed as indexed; changed bytes → new context/target | P1 S1 receipts; P2 `Q1-P2A` R13 sides; P2 `Q2-INT` | Fresh reviewer reconstructs `compctx1` from recorded descriptor fields and matches production/frozen; changed source bytes change `compctx1`; targets differ | O-1 host scope; finite cases | **SATISFIED WITH QUALIFICATION** (Q-P2-03, Q-P2-04) |
| A2 | Canonical serialization: domain-separated, length-prefixed big-endian UTF-8, SHA-256, lowercase hex | P1 10/10 KATs; P2 5/5 KATs; `Q5` 190/190 | Domain separation, ordering, UTF-8, empty-component encoding/form-layer separation verified; KAT-3/KAT-4 encoding-only | Finite vectors; encoding-only anchors are explicit observations | **SATISFIED** |
| A3 | `identifierDigest` from the normalized definition's simple name | P1 17/17 forms; P2 8/8 forms; R13 | Overloads share identifier; mode-insensitive; normalization `ReducedFrom ?? OriginalDefinition` | Finite cases; no universal injectivity claim | **SATISFIED WITH QUALIFICATION** (Q-P2-04) |
| A4 | `declarationDigest` from Roslyn doc-ID, `Kind:FQDN` fallback | P1 17/17; P2 8/8; R13 | Doc-ID first; `@` for modes; explicit-interface `#` escaping; fallback distinction preserved | Finite cases; fallback not the primary exercised path | **SATISFIED WITH QUALIFICATION** (Q-P2-04) |
| A5 | `shapeDigest` = `method\|arity\|fully-qualified parameter types` | P1 17/17; P2 8/8; R13 | Special types, generics, nested generics, tuples, arrays; mode-blind shape as clarified | Finite cases; unescaped-comma-join property recorded | **SATISFIED WITH QUALIFICATION** (Q-P2-04) |
| A6 | Independent semantic reproduction; reviewer ≠ implementer; expected values from accepted contracts | P1 RefEnc; P2 reviewer encoder, PowerShell KATs, fresh fixtures, R13 | 70/70 fresh reviewer checks; producer `allPass` not used as a pass condition; independent encoder agrees with production | Q-P2-01 same-session; Q-P2-04 finite | **SATISFIED WITH QUALIFICATION** (Q-P2-01, Q-P2-04) |
| A7 | Real-source extraction/ingestion through the accepted path | P1 S1–S5; P2 `Q2-INT` | 20 batches, 4 authority envelopes, native rows populated; both expected targets present | Windows x64; frozen R2 corpus; no production change | **SATISFIED WITH QUALIFICATION** (environment scope) |
| A8 | Publication/authority binding; `asmctx3` from the verified defining compilation; no false-available | P1 S5/S7; P2 `Q2-INT`/`Q3-REOPEN` | Both targets present; `Available` only with current publication; stale/missing authority refused | O-1; cross-project binding not separately re-established | **SATISFIED WITH QUALIFICATION** (Q-P2-03; cross-project carried) |
| A9 | Durable reopen and managed recovery; identities preserved | P1 S6/S8; P2 `Q3`/`Q4` | Fresh-process reopen `Available/8` and `Available/2`; known fault preserves prior publication; unknown outcome quarantines; reconcile `run:review3`; tamper refused | F-02 managed-fault boundary; D4.5 Stage 6 | **SATISFIED WITH QUALIFICATION** (Q-P2-05) |
| A10 | Applicable D3/D4/D5 behaviors | P1 applicable matrix; P2 §15 | Publication/authority, stale, bounded `Examined`, failed-staging preservation, page-binding refusal, no migration/dual-write | Legacy D3 graph noninterference and some D5 variants carried; D4.5/full S-02 Stage 6 | **SATISFIED WITH QUALIFICATION** (Q-P2-05, Q-P2-06) |
| A11 | Evidence provenance/oracle: fresh/reused/authentic/generated/constructed; self-excluding manifest | P1 23/0; delta 17/0; P2 26/0 | Manifests intact; provenance categories recorded; no producer value presented as independent | Q-P2-01; P3-F-01 narrative discrepancy | **SATISFIED WITH QUALIFICATION** (Q-P2-01, P3-F-01) |

### 5.2 Binding Stage-2 matrix rows and explicit P3 assessment items

| # | Governing requirement | Supporting evidence | P1/P2 result | Qualification/limitation | Owner disposition |
| --- | --- | --- | --- | --- | --- |
| B1 | §1.1 source bytes → `compctx1`; changed bytes → new `compctx1`/`asmctx3`/`usage4` | P1 S1; P2 R13, `Q2` | Reviewer reconstruction matches production/frozen; changed source bytes change context; targets differ | O-1; finite | **SATISFIED WITH QUALIFICATION** |
| B2 | §1.2 options/references in context | P2 R13 sides: 181 ordered references, digest `a5bc0419…`, matches production/frozen | Options/reference closure included and ordered; context reconstruction agrees | Q-P2-02 historical 179 vs fresh 181; Q-P2-03 | **SATISFIED WITH QUALIFICATION** (Q-P2-02, Q-P2-03) |
| B3 | §1.3 semantic binding against the verified compilation; cross-project target | P1 S1–S4; P2 `Q2` | Real extraction/binding on the frozen corpus; both targets persisted | Cross-project binding not separately re-established in P2 | **CARRIED — NOT RE-ESTABLISHED IN P2** (no new claim) |
| B4 | §2.1 exact `usage4` tuple/encoding; frozen vectors; forged ID refused | P1 KATs/frozen vectors; P2 KATs | Exact R2 encoding and frozen anchors verified | Contributor-union row not re-run in P2 | **SATISFIED** (encoding); contributor union **CARRIED** |
| B5 | §2.2 exact `asmctx3` composition; different context/component → different target; unavailable/external/unbound → refusal | P1 S5/S7; P2 R13 32/32; `Q2` | Composed target matches production and independent composition; context alone distinguishes mode-family revisions; value-vs-mode also distinguishes declaration | O-1; finite | **SATISFIED WITH QUALIFICATION** (Q-P2-03, Q-P2-04) |
| B6 | §2.3 storage contributor exclusion; two storage units → one fact, two contributors | Not re-run in P2; historical S2.2/S2.3 evidence | Carried; no new evidence claimed | Q-P2-06 | **CARRIED — NOT RE-ESTABLISHED IN P2** |
| B7 | §2.4 payload hash detects conflict | Not re-run in P2; historical S2 evidence | Carried; no new evidence claimed | Q-P2-06 | **CARRIED — NOT RE-ESTABLISHED IN P2** |
| B8 | Clarified R13 behavior (value/ref/out/in) | P2 R13 32/32; P1 15/3 historical preserved | Value vs mode: id `=`, decl `≠`, shape `=`; mode family: id `=`, decl `=`, shape `=`; context distinguishes revisions; composed targets differ | Q-P2-03; Q-P2-04; P1 historical failures preserved | **SATISFIED WITH QUALIFICATION** (Q-P2-03, Q-P2-04) |
| B9 | Coexistence of composed targets | P2 coexistence 4/4; language boundaries 2/2 | Plain + `ref` coexist in one compilation; identifier/shape equal, declaration/targets differ; `ref`+`out`/`ref`+`in` not declarable (CS0663) | Finite cases | **SATISFIED WITH QUALIFICATION** (Q-P2-04) |
| B10 | Native-store persistence and reopening | P2 `Q2`/`Q3`; P1 S5/S6 | 1 meta, 20 pages, 4 units, 4 graph units; reopen `Available/8` and `Available/2`; same publication/spans | Windows x64; O-1 | **SATISFIED WITH QUALIFICATION** (environment/O-1) |
| B11 | Stale and unavailable analysis behavior | P2 `Q3` | `StalePublication/0`; `AnalysisUnavailable/0` with reason; invalid target refused `ArgumentException` | None material | **SATISFIED** |
| B12 | Known-fault recovery | P2 `Q4` | `ReviewFault`; prior `Available/8` preserved | F-02 managed fault | **SATISFIED WITH QUALIFICATION** (Q-P2-05) |
| B13 | Unknown-outcome quarantine and reconciliation | P2 `Q4` | `CodeGraphCommitOutcomeUnknownException`; reads/mutations throw; fresh open reconciles `run:review3` | F-02 managed fault | **SATISFIED WITH QUALIFICATION** (Q-P2-05) |
| B14 | Tamper refusal | P2 `Q4` | Fresh open refuses tampered page binding with `CodeOccurrenceIndexException` | None material | **SATISFIED** |
| B15 | Regression and frozen-oracle preservation | P2 §16 logs | 907/0/0; focused 8/0; oracle 146,851 B / `97f6c5e5…`; Release 0/0; format 0 | Inherited regression evidence; no CI change | **SATISFIED** |

**Carried rows are not part of the S-01 acceptance disposition.** The `CARRIED — NOT
RE-ESTABLISHED IN P2` rows in §5.2 (B3, B6, B7) record Stage-2 semantic-identity obligations
that P2 did not freshly re-establish; they remain WP-S25-4/Stage-2 obligations and no new
evidence is claimed for them. They do not block S-01 acceptance because the S-01 integrated
proof's demonstrated scope does not depend on re-qualifying those rows, but they must be
reconciled before Stage-2 acceptance.

### 5.3 Carried, deferred, and out-of-scope items

| # | Item | Evidence/status | Owner disposition |
| --- | --- | --- | --- |
| C1 | Legacy D3 graph noninterference (before/after node/edge/count/traversal) | Not re-run in P2; historical accepted evidence; LatticeDb 52 green | **CARRIED — NOT RE-ESTABLISHED IN P2** (Q-P2-06) |
| C2 | D4.5 cancelled staging and full S-02 ten-invariant matrix | Not exercised; Stage 6 | **STAGE 6 — NOT CLAIMED** (Q-P2-05) |
| C3 | D5 schema-version/namespace refusal; graph-only isolation | Not re-run in P2; page-binding tamper exercised; historical tests green | **CARRIED — NOT RE-ESTABLISHED IN P2** (Q-P2-06) |
| C4 | F-02 crash/power-loss durability | Managed fault only | **NOT CLAIMED** (Q-P2-05) |
| C5 | F-G2-01 repository CI branch-coverage gate | `OPEN` — 296/450 = 65.77% < 70% | **OPEN — SEPARATE OWNER DECISION** (Q-P2-05) |
| C6 | Linux coverage | WP-S25-2 genuine-Linux evidence only; P2 Windows x64 | **CARRIED — WP-S25-2 SCOPE** |
| C7 | Universal collision freedom / universal C# symbol uniqueness | Finite executed cases | **NOT CLAIMED** (Q-P2-04) |
| C8 | Public Find Usages, cross-target, optimization, migration/dual-write, Stage 6 | Unauthorized/deferred by governing boundaries | **NOT AUTHORIZED / DEFERRED** |

**Matrix conclusion.** Every mandatory S-01 requirement within the demonstrated scope is
supported by P1, delta, and/or P2 evidence. The matrix contains no unconditional pass for a
requirement whose evidence is carried, deferred, or out of scope; those rows are marked
explicitly. The qualifications in §7 are the reasons the overall S-01 disposition is B rather
than A.

---

## 6. P1/P2 evidence reconciliation

### 6.1 P1 and delta preservation

| Item | Recorded outcome | P3 verification |
| --- | --- | --- |
| P1 KATs | 10/10 | `Q5` recomputed all 10 from recorded primitives; no conversion of historical failures |
| P1 canonical forms | 17/17 | `Q5` recomputed all 17; FC-13/FC-14/FC-DELIM preserved |
| P1 matrix | 15 pass / 3 fail (R13a/b/c) | `Q5` preserves 15/3; R13 failures remain failures against v1-as-written |
| P1 integrated proof | S1–S8 complete with qualifications | P1 package byte-unchanged; P2 fresh processes re-execute the applicable path |
| Delta R13a/b/c + out/in | 4/4 pairs, 55/55 checks | `Q5` recomputes all four pairs, descriptor/source isolation, coexistence, boundaries, editorial |
| Delta reference closure | 179 entries, digest `b8cdb1ce…` | Recorded count/digest preserved; identity strings absent; **not** reconstructed |
| Historical reports | P1/delta reports | Byte-unchanged; no revision |

### 6.2 P2 fresh evidence

| Item | P2 result | P3 assessment |
| --- | --- | --- |
| Part A | 70/70, `allPass=true`, PID 25400 | 5 encoder + 8 forms + 15 matrix + 32 R13 + 4 coexistence + 2 boundaries + 4 editorial; reconciled |
| Artifact audit | 190/190, `allPass=true` | Recomputation over producer-recorded primitives, explicitly not fresh execution; reconciled |
| R13 | 4 pairs × 8 checks = 32/32 | Sealed `compctx1` values: value-ref `74137c5c…`→`35cb9023…`, ref-out `3fa86dc3…`→`b120cd0e…`, ref-in `f1a4f104…`→`0639de89…`, out-in `c0f0df2a…`→`a3e517bb…`; composed targets all differ; component relations match E-3 |
| Reference closure | 181 ordered entries, digest `a5bc0419…` | `matchesProduction=true`, `matchesFrozen=true`, `referencesOrdered=true` on all eight R13 sides; the report's `4b9637df…` is not the sealed value (P3-F-01) |
| Coexistence | 4/4 | Identifier `bf8cb43b…` equal; declarations `40136edc…` vs `c8d02386…`; shapes `928f040e…` equal; targets `dcb0cbfc…` vs `ddce4e09…` |
| Part B integration | PID 23112 | 20 batches; 4 authority envelopes; 1 meta / 20 pages / 4 units / 4 graph units; `Add` target `f6bd5d28…`, `Greet` target `1577c900…`; both `agree=true` |
| Part B reopen | PID 23884 | `Available/8`; `Available/2`; `StalePublication/0`; `AnalysisUnavailable/0`; invalid target refused |
| Part B recovery | PID 12368 | Baseline `Available/8`; `ReviewFault` preserves prior publication; unknown outcome quarantines reads/mutations; reconcile `run:review3`; tamper refused |
| Regression | 907/0/0; 8/0 focused | 52 + 157 + 345 + 353; no P2 checks hidden in the inherited count |
| Frozen oracle | 146,851 B, `97f6c5e5…` | byte-identical |
| Release/format | 0 warnings/0 errors; exit 0 | reconciled |

### 6.3 P2 report narrative discrepancy

P3-F-01 is recorded in §4.3. The sealed receipts, not the report's absolute target/context
values or its `4b9637df…` closure digest, are the basis for the acceptance matrix. The
component digests and all pass/fail outcomes in the report match the sealed evidence. This
finding is a qualification on the report's narrative accuracy; it does not change the S-01
requirement results.

---

## 7. Individual qualification dispositions

Each P2 qualification is dispositioned individually. Acceptance of a qualification does not
mean the underlying limitation has been eliminated.

### Q-P2-01 — Independence

| Field | Determination |
| --- | --- |
| Owner disposition | **ACCEPTED AS METHODOLOGICAL INDEPENDENCE; ORGANIZATIONAL INDEPENDENCE NOT CLAIMED** |
| Rationale | The governing contracts require reviewer ≠ implementer, an independent oracle, and expected values from accepted contracts. The reviewer is the **same assistant session** as the P1/delta producer, so organizational independence is absent. Methodological independence is supported by a reviewer-owned harness, fresh fixtures, PowerShell-derived KATs computed before the harness, an independent encoder, producer `allPass` never used as a pass condition, four fresh processes, and an artifact audit explicitly classified as recomputation rather than execution. The owner accepts this methodological independence as sufficient for P2, while recording that it does not satisfy organizational independence |
| Evidence references | P2 §3, §5, §6; `Q1-P2A.json` `reviewerProvenance`; `Q2`/`Q3`/`Q4` PIDs 23112/23884/12368; `Q5` classification |
| Residual limitation | Same-session producer and reviewer; no independent human/model/organizational review; no external replication |
| Continuing obligation | If a governing contract or later stage requires organizational independence, a separate reviewer commission is required. This decision does not create or imply organizational independence |
| Effect on S-01 acceptance | Does not block; S-01 is accepted with this qualification |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B |

### Q-P2-02 — Artifact reproducibility

| Field | Determination |
| --- | --- |
| Owner disposition | **ACCEPTED; HISTORICAL LIMITATION PRESERVED; NOT REMEDIATED** |
| Rationale | The historical delta artifact records `referencesCount=179` and `referencesDigest=b8cdb1ce…` per side but not the 179 reference identity strings, so its `compctx1` cannot be recomputed from the artifact alone. No governing S-01 acceptance contract explicitly requires artifact-only reproducibility of the historical delta `compctx1`: the S-01 closure condition requires an S2.5 integrated independent proof including canonical component-digest derivation; G-ACCEPT §18 and the entry decision §8 require independently derived expectations and fresh-process reproduction. The fresh P2 run records its own **181**-entry ordered closure, digest `a5bc0419…`, and matches production and frozen descriptors on every R13 side. The fresh evidence therefore independently supports the governing S-01 requirement. The 179/181 difference is a host-closure difference and the two closures must not be conflated. The P2 report's narrative digest `4b9637df…` is not the sealed value; P3-F-01 records the discrepancy |
| Evidence references | `Q1-DELTA.json` `referencesCount=179`/`referencesDigest=b8cdb1ce…`; `Q1-P2A.json` R13 sides: 181 entries, `a5bc0419…`, `matchesProduction=true`, `matchesFrozen=true`, `referencesOrdered=true`; `Q5` `referenceIdentityLimitation`; P2 §7/§10; §4.3 herein |
| Residual limitation | Historical delta `compctx1` is not artifact-only recomputable; the 179 historical identity strings are absent; no reconstruction was performed and none is claimed |
| Continuing obligation | Preserve the 179/181 distinction in all downstream records; do not claim remediation. If a future contract requires artifact-only reproducibility, a separate commission must capture the identity lists prospectively |
| Effect on S-01 acceptance | Does not block; accepted with the historical limitation |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B |

### Q-P2-03 — Host-scoped composed targets

| Field | Determination |
| --- | --- |
| Owner disposition | **ACCEPTED; O-1 PRESERVED** |
| Rationale | Composed targets bind the host build closure because `compctx1` includes reference identities/order. P2 compares component digests and within-run relational claims, not absolute target IDs across differently built hosts. The report's narrative absolute values differing from the sealed receipts is consistent with O-1 and is recorded as P3-F-01. The proof does not generalize beyond its demonstrated identity and composition boundaries |
| Evidence references | P2 §10, §12; `Q1-P2A.json` R13 sides; `Q2-INT.json`; clarification decision §12 O-1 determination |
| Residual limitation | Composed targets are host-scoped; absolute target equality across hosts is not claimed; component digests are the stable comparison surface |
| Continuing obligation | Downstream packages must compare component digests/relational claims, not absolute targets across hosts; any cross-host claim requires a separate design/evidence decision |
| Effect on S-01 acceptance | Does not block; accepted with the O-1 boundary |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B |

### Q-P2-04 — Finite proof coverage

| Field | Determination |
| --- | --- |
| Owner disposition | **ACCEPTED; NO UNIVERSAL COLLISION-FREEDOM CLAIM** |
| Rationale | The executed forms/matrix/R13/coexistence set is finite. The governing contract requires the demonstrated cases and the clarified relations, not a mathematical proof of universal injectivity; v1 §3 already permits identifier collisions by design, and the clarification decision §8 records the clause-level plus executed-evidence limits. The finite set covers the contract's perturbation matrix and the clarified R13 relations; no concrete supported counterexample was found |
| Evidence references | `Q1-P2A.json` 70/70; P1 17/17; `Q5` 190/190; P2 §9–§11 |
| Residual limitation | Finite cases; no universal C# symbol-uniqueness or collision-freedom proof; unexercised symbol categories may exist |
| Continuing obligation | Any new symbol category or edge case requires a separate bounded proof; no universal claim may be inferred from this decision |
| Effect on S-01 acceptance | Does not block; accepted with the finite-coverage qualification |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B |

### Q-P2-05 — Stage 6 obligations

| Field | Determination |
| --- | --- |
| Owner disposition | **PRESERVED; NOT SATISFIED OR WAIVED** |
| Rationale | D4.5 and the full S-02 matrix remain Stage 6 obligations. F-02 crash/power-loss evidence requirements remain unclaimed; managed fault injection is **not** crash or power-loss durability evidence. F-G2-01 remains `OPEN — REPOSITORY CI BRANCH-COVERAGE GATE FAILURE` at **296/450 = 65.77%**, below the 70% threshold; P2 did not change coverage policy or CI. These items are outside the S-01 acceptance scope and are not waived by this decision |
| Evidence references | P2 §14–§16, §18; `Q4-RECOVERY.json` `f02Note`; entry decision §7/§10; work-package-plan WP-S25-2/F-G2-01 rows |
| Residual limitation | D4.5/full S-02 not exercised; crash/power-loss not qualified; F-G2-01 open |
| Continuing obligation | Stage 6 commissions for D4.5/full S-02 and crash/power-loss; a separate F-G2-01 owner decision; no managed-fault equivalence claim |
| Effect on S-01 acceptance | Does not block; S-01 scope excludes these |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B. F-G2-01 is a separate open finding, non-blocking for WP-S25-3 but binding for the CI conclusion and Stage-2 acceptance |

### Q-P2-06 — Carried historical scope

| Field | Determination |
| --- | --- |
| Owner disposition | **ACCEPTED AS CARRIED; NO NEW EVIDENCE CLAIMED** |
| Rationale | Legacy D3 graph noninterference, schema-version/namespace refusal, graph-only isolation, and the non-applicable D4 invariants were not re-run in P2. The governing S-01 scope requires applicable D3/D4/D5 behavior, not re-qualification of every Stage-2 obligation; the entry decision §7 explicitly leaves D3-full, D4.5, and full S-02 to Stage 6, and the WP-S25-3 definition requires the applicable S-01 subset. The carried items remain covered by earlier accepted evidence or later gates; no new evidence is claimed. P2 §15 marks them `Carried (not claimed)` |
| Evidence references | P2 §15, §18; P1 D3/D4/D5 matrix; entry decision §7; work-package-plan WP-S25-3 scope/exclusions |
| Residual limitation | Carried items not freshly re-established in P2; some D5 variants and legacy D3 graph noninterference are not re-run |
| Continuing obligation | WP-S25-4/Stage-2 must reconcile the carried items; Stage 6 owns D3-full/D4.5/full S-02; no claim of new evidence |
| Effect on S-01 acceptance | Does not block; accepted as carried within the S-01 scope |
| Effect on WP-S25-3 closure readiness | Carried; closure readiness is B |

---

## 8. Residual limitations and outstanding obligations

| Item | Status | Owner/route | Effect on S-01 | Effect on WP-S25-3 closure readiness |
| --- | --- | --- | --- | --- |
| Q-P2-01 same-session independence | Accepted methodological only; organizational independence absent | Future reviewer commission if required | Qualification | Carried |
| Q-P2-02 historical delta artifact reproducibility | Limitation preserved; 179 vs 181 distinct; not remediated | Future commission if artifact-only reproducibility is required | Qualification | Carried |
| Q-P2-03 O-1 host-scoped targets | Preserved | Downstream comparison discipline | Qualification | Carried |
| Q-P2-04 finite coverage | Preserved; no universal claim | Future bounded proof for new cases | Qualification | Carried |
| Q-P2-05 Stage 6/F-02/F-G2-01 | Preserved/open | Stage 6; separate F-G2-01 owner decision | Outside S-01 | Carried; F-G2-01 separate |
| Q-P2-06 carried D3/D5 variants | Carried; not re-run | WP-S25-4/Stage 6 | Qualification | Carried |
| P3-F-01 P2 report narrative discrepancy | Recorded; sealed evidence governs | Additive erratum before downstream citation | Qualification | Carried |
| D4.5 / full S-02 | Stage 6 | Stage 6 | Not claimed | Deferred |
| F-02 crash/power-loss | Not claimed | Stage 6/separate scope | Not claimed | Deferred |
| F-G2-01 CI coverage gate | `OPEN` — 296/450 = 65.77% < 70% | Separate owner decision | Outside S-01 | Open finding; non-blocking for WP-S25-3, binding for CI/Stage-2 |
| Linux coverage | WP-S25-2 bounded scope | WP-S25-2 accepted evidence | Not re-run in P2 | Carried |
| Windows x64 / serialized single-writer | Environment preserved | Governing boundary | Qualification | Carried |
| Public Find Usages, cross-target, optimization, migration/dual-write | Unauthorized/deferred | Future owner decisions | Not claimed | Not authorized |
| Formal WP-S25-3 closure | Not performed | Next owner commission | — | Requires separate decision |
| WP-S25-4 / Stage-2 acceptance | Not authorized | After WP-S25-3 closure | — | Deferred |

**No finding is closed by this decision merely because a test passed.** P3 closes no
production, CI, contract, or Stage-2 finding. F-G2-01 remains open. S-02 and D4.5 remain
unsatisfied. The only status changes are the S-01 disposition and the WP-S25-3
closure-readiness disposition recorded in §§9–10.

---

## 9. Formal S-01 owner decision

**`S-01 — SATISFIED WITH QUALIFICATIONS` (Disposition B).**

**Basis.** The integrated P1 proof, the approved clarification, the bounded delta proof, and
the independent P2 qualification together support every mandatory S-01 requirement within
the demonstrated scope: governed canonical forms and encoding; identifier/declaration/shape
derivation; independent semantic reproduction; real-source extraction/ingestion;
publication/authority binding; native persistence and fresh-process reopen; stale/unavailable
refusal; known-fault recovery; unknown-outcome quarantine and reconciliation; tamper refusal;
and regression/frozen-oracle preservation. The clarified R13 relations are satisfied by
execution, while the P1 historical 15/3 result remains preserved. No blocking finding, no
production contradiction, no mandatory P2 check failure, and no evidence-integrity failure
was found.

**Why B rather than A.** Material qualifications remain and are individually dispositioned
in §7: same-session methodological independence (Q-P2-01); the historical delta artifact
reproducibility limitation with distinct 179/181 closures (Q-P2-02); O-1 host-scoped targets
(Q-P2-03); finite coverage with no universal collision-freedom claim (Q-P2-04); Stage 6,
F-02, and F-G2-01 obligations (Q-P2-05); carried legacy D3/D5 variants (Q-P2-06); and the
P2 report narrative discrepancy (P3-F-01). Unconditional acceptance is therefore not issued.

**Why not C.** No acceptance blocker exists. The sealed evidence independently supports the
governing requirements; the qualifications are limitations of scope, provenance, or
independence that the governing contract permits to be carried, not failures of a mandatory
requirement. No remediation is required for S-01.

**What this decision does not mean.** It does not mean Stage-2 acceptance, S-02 satisfaction,
D4.5 satisfaction, F-G2-01 closure, crash/power-loss qualification, organizational
independence, universal collision freedom, artifact-only reproducibility of the historical
delta, cross-host target invariance, Linux re-qualification, or formal WP-S25-3 closure.

---

## 10. WP-S25-3 closure-readiness decision

**`WP-S25-3 — READY FOR CLOSURE WITH QUALIFICATIONS` (Disposition B).**

### 10.1 Gate status

| Gate | Status | Basis |
| --- | --- | --- |
| P0 contract adoption | satisfied | `349136d`; canonical-forms v1 adopted |
| P1 integrated proof | complete with qualifications | `90c5fbe`; P1 report; 10/10 KATs, 17/17 forms, 15/3 matrix preserved |
| Contract clarification | approved | `8709724`; addendum E-1…E-4; no version bump/remediation |
| P1 delta proof | complete, ready for P2 | `976fe52`; 55/55 delta checks; 4/4 pairs; direct context receipts |
| P2 independent qualification | supported with qualifications | `0c0bab0`; 70/70 Part A; 190/190 audit; Part B fresh processes; regressions green; 26/0 manifest |
| P3 owner acceptance | S-01 satisfied with qualifications | this decision |
| Evidence integrity | intact | P1 23/0; delta 17/0; P2 26/0; protected areas unchanged |
| Regression floors | green | 907/0/0; focused 8/0; oracle byte-identical; Release 0/0; format exit 0 |

### 10.2 Qualifications and conditions

| Qualification | Disposition | Closure-readiness effect |
| --- | --- | --- |
| Q-P2-01…Q-P2-06 | individually dispositioned in §7 | carried into WP-S25-4/Stage-2; none blocks WP-S25-3 closure |
| P3-F-01 P2 report narrative discrepancy | recorded; sealed evidence governs | additive erratum required before downstream citation of the report's absolute target/context values |
| F-G2-01 | `OPEN` — 296/450 = 65.77% < 70% | separate owner decision; non-blocking for WP-S25-3 closure, binding for CI conclusion/Stage-2 |
| D4.5 / full S-02 / F-02 | Stage 6 / not claimed | deferred; no closure claim |
| Carried legacy D3/D5 variants | carried | WP-S25-4/Stage-2 reconciliation; no new evidence claim |
| O-1, finite coverage, same-session independence | preserved | no generalization beyond demonstrated boundaries |

### 10.3 Additional closure activity required

1. A **separate formal WP-S25-3 closure decision** by the owner. This commission does not
   authorize formal closure.
2. An **additive P2 report erratum** (or an equivalent additive correction record) for
   P3-F-01 before the report's absolute target/context values or `4b9637df…` closure digest
   are cited downstream. The historical P2 report is not revised in this commission.
3. Carrying Q-P2-01…Q-P2-06 and P3-F-01 into the WP-S25-4/Stage-2 reconciliation with their
   owners and routes intact.
4. No S-01 remediation is required; no production, test, CI, contract, or historical-evidence
   change is a condition of closure readiness.

**Why not A.** Material qualifications remain, and P3-F-01 requires an additive erratum.
**Why not C.** Every mandatory WP-S25-3 gate is satisfied; the qualifications have explicit
dispositions, owners, and routes; no acceptance blocker or evidence-integrity failure exists.

---

## 11. Scope and non-claims

**Changes made by this commission.** Only this new owner-decision record and the authorized
additive WP-S25-3 status update in
[`work-package-plan.md`](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) are changed.
No production source, test, harness, project, package, CI, contract, fixture, oracle/corpus,
prior decision, historical review, or historical evidence is modified. No branch or worktree
is created. No fetch, remote query, or push occurs.

**Explicit non-claims.**

- No formal WP-S25-3 closure; this decision only records closure readiness.
- No WP-S25-4, Stage-2 acceptance, or Stage 6 authorization.
- No S-02 or D4.5 satisfaction.
- No F-G2-01 closure or coverage remediation.
- No crash/power-loss durability claim; managed fault injection is not equivalent.
- No organizational independence; P2 independence is methodological only.
- No universal collision-freedom or universal C# symbol-uniqueness claim.
- No artifact-only reproducibility of the historical delta `compctx1`; the 179 historical
  reference identities are absent and were not reconstructed.
- No cross-host invariance of composed targets; O-1 host scope is preserved.
- No Linux re-qualification by P3; WP-S25-2's bounded Linux evidence is unchanged.
- No public Find Usages, cross-target, optimization, migration, or dual-write.
- No remediation; no new work-package implementation.
- No revision of the P2 report or any historical evidence; P3-F-01 is recorded, not corrected
  in place.

**Provenance.** This decision is based on the committed P1 proof (`90c5fbe`), clarification
(`8709724`), delta (`976fe52`), P2 qualification (`0c0bab0`), and the sealed P1/delta/P2
evidence. The P3 commit is additive on `main`; its commit/tree identities and the final
working-tree status are reported in the handoff.

---

## 12. Recommended next commission

**Proposed (explicitly NOT authorized by this decision):**
`ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01` — a documentation-only formal WP-S25-3
closure decision that:

1. records the formal closure of WP-S25-3 within its bounded qualified scope;
2. carries Q-P2-01…Q-P2-06 and P3-F-01 into the WP-S25-4/Stage-2 reconciliation;
3. authorizes and records the additive P2 report erratum for P3-F-01;
4. preserves F-G2-01 `OPEN`, D4.5/full S-02 as Stage 6, F-02 as unclaimed, O-1, finite
   coverage, and the historical 179/181 distinction;
5. does not authorize WP-S25-4, Stage-2 acceptance, or any production/CI/contract change.

After that closure decision, WP-S25-4 (final integrated Stage-2 qualification) becomes the
next eligible package, subject to its own commission and entry prerequisites. Stage-2 owner
acceptance remains a separate decision after WP-S25-4. Stage 6 remains separate.

**Stop point.** This decision and its handoff complete the P3 commission. No further
authorization is implied; the owner's next authorization is required.
