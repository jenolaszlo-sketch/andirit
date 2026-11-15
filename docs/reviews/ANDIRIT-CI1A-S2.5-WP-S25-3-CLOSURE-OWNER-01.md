# ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01 — Formal WP-S25-3 closure

**Project** Andirit · **Recorded** 2026-11-15 · **Branch** `main`

- **WP-S25-3 — CLOSED WITH QUALIFICATIONS**
- **S-01 — SATISFIED WITH QUALIFICATIONS**
- **P3-F-01 — CLOSED BY VERIFIED ADDITIVE ERRATUM**
- **WP-S25-4 — ELIGIBLE FOR ENTRY ASSESSMENT, SUBJECT TO SEPARATE OWNER AUTHORIZATION**

## 1. Commission and owner authorization

The owner authorized a documentation-only closure of WP-S25-3, additive correction of the P2 narrative, disposition of continuing obligations, and an updated status for WP-S25-4. The commission authorizes a commit on `main` and normal fast-forward publication to `origin/main` after successful post-commit verification. [P3 owner decision §§9–12](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md)

## 2. Repository preflight and provenance

Read-only preflight confirmed Andirit on `main` at `D:/projects/Andirit`, with a clean working tree, one worktree, no stashes, and a recorded local `origin/main` of `87097242d1610689c388d6814be06fef8cc89d89`. The branch was three commits ahead and zero behind that ref.

| Revision | Commit | Tree |
| --- | --- | --- |
| P0 contract, reviewed by P1 | `349136db53c811c4e648e7986865a60991c2e232` | `d7d23731e05f5f87b001bcabc73ec38473db03fd` |
| P1 integrated proof | `90c5fbee03f7826ba81b3b1eef77a4b24f22700f` | `3b2f7959c21804b160de45b372bf9072022be00f` |
| Clarification, reviewed by the P1 delta | `87097242d1610689c388d6814be06fef8cc89d89` | `d73a1f857494e7b063291f5132c08fced6670264` |
| P1 delta, reviewed by P2 | `976fe52a1fef1a69e3d68a7a4227a9337524274c` | `c0089a29ee56bbb89936a4cf77963d46c49222c2` |
| P2 qualification, reviewed by P3 | `0c0bab00586ad65bcc290d304234d39055a0ec6a` | `66e112b77b08679d26aa6b30b32dc718626d8c55` |
| P3 owner decision, closure starting revision | `92e72c99425733b0e0ed4ff5f15049df78fbb0f5` | `4fd7d20ca8a203102adfc87a8fa2919c22abafbe` |

The commits form the verified ancestry in table order. Delta, P2 and P3 are the three unpublished commits at preflight. The local tracking ref records the starting baseline. The live ref is a separate publication check.

## 3. Applicable governance

- S-01 canonical derivation and composition are governed by [canonical-forms v1](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md), the [approved E-1…E-4 clarification](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md), its [owner decision](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md), and the [S-01 canonicalization disposition §§5, 7](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md).
- The package gates, D3/D4/D5 scope and downstream ownership come from the [S2.5 entry decision §§8–11](../decisions/adr-ci1-01-ci1a-s2-5-entry-owner-01.md), [WP-S25-3 entry §§7, 9–10](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md), [work-package plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md), [Stage-2 matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md), and [consolidated obligation register §§6–8](../decisions/adr-ci1-01-ci1a-s2-obligation-register.md).
- The evidence and binding decision are the [P1 integrated proof](ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md), [P1 delta proof](ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md), [P2 independent qualification](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01.md), and [P3 owner decision](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md), including its §7 qualification register and §8 outstanding obligations.

## 4. Evidence integrity verification

| Manifest | Entries | Mismatches | Reviewed revision |
| --- | ---: | ---: | --- |
| [P1](CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/manifest.json) | 23 | 0 | P0 `349136db` |
| [Delta](CI1A-S2.5-WP-S25-3-P1-DELTA-evidence/manifest.json) | 17 | 0 | Clarification `87097242` |
| [P2](CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/manifest.json) | 26 | 0 | Delta `976fe52a` |

Each manifest is self-excluding. Exact-byte lengths and SHA-256 hashes match, and each directory has complete coverage. Reviewed commit/tree pairs agree with their committed provenance records and Git objects. The P1, delta and P2 reports, harnesses and evidence match their respective introducing commits. Production, repository tests, package/project definitions and CI match the governing proof baseline. The adopted contract and clarification match their introducing revisions.

The sealed P2 evidence records 70/70 fresh Part A checks and 190/190 artifact-audit checks, and supports the P3 record of integration, fresh-process reopen, explicit availability/refusal, and managed recovery. The historical P1 matrix stays at 15 pass / 3 fail against v1 as written. The delta established the clarified R13 relations with four pairs and 55/55 producer checks. P2 corroborated the clarified relations independently in its demonstrated scope. The sealed regression floor reports 907 passed, 0 failed, 0 skipped, eight focused refusal tests passed, the frozen R2 oracle byte-identical, Release build 0 warnings/0 errors, and clean format verification. These are recorded results. This commission used targeted documentation and sealed-artifact checks.

## 5. P3-F-01 erratum and disposition

**P3-F-01 — CLOSED BY VERIFIED ADDITIVE ERRATUM.** The erratum maps each unsupported P2 absolute value to its exact checksum-bound receipt value. It also records that the fresh runtime list was checked for ordering, with count/digest and comparison outcomes sealed in the receipt. The identity strings themselves are absent from the receipt. The reported agreement supports the live run; the omitted list precludes artifact-only replay of that list. The historical delta closure contains 179 identities, and the fresh P2 closure contains 181. Q-P2-02 remains in force. The source report and its evidence remain byte-identical. [P2 erratum](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01-ERRATUM.md)

## 6. Continuing P2 qualifications

| ID | Continuing effect | Downstream scope and ownership |
| --- | --- | --- |
| Q-P2-01 | P3 accepted methodological independence only. The producer and reviewer used the same assistant session. Claims of organizational independence remain outside the evidence. | WP-S25-4 reviewer and Stage-2 owner preserve the qualification. The owner commissions a separate reviewer if a later governing gate requires organizational independence. |
| Q-P2-02 | Historical artifact-only reproducibility remains limited. The historical 179-entry closure and fresh 181-entry closure are distinct. The historical identity strings are absent and the limitation remains unremediated. | WP-S25-4 reviewer and downstream evidence consumers retain the distinction. The owner separately commissions prospective identity-list capture if artifact-only reproducibility is required. |
| Q-P2-03 | O-1 remains binding. Absolute composed targets are host-scoped. Component digests are the stable comparison surface, with relational checks taken within a demonstrated run. | WP-S25-4 reviewer, Stage-2 owner, and downstream identity consumers apply this evidence boundary. Any cross-host claim requires a separate owner design/evidence decision. |
| Q-P2-04 | Proof coverage is finite and the demonstrated cases bound claims about canonical forms and collisions. Universal collision freedom remains outside the proof. | WP-S25-4 reviewer and Stage-2 owner preserve the bounded claim. New symbol categories or edge cases require a separate bounded proof before a broader claim. |
| Q-P2-05 | D4.5, full S-02, and F-02 crash/power-loss qualification remain outstanding. The independent F-G2-01 finding remains open. | Stage 6 owner commissions own durability and the full matrix. The owner separately dispositions F-G2-01. WP-S25-4 and the Stage-2 owner carry the applicable limits. |
| Q-P2-06 | Legacy D3, applicable D5 variants and the carried D4 scope retain their historical evidence classifications. P2 adds evidence only for its exercised subset. | WP-S25-4 reviewer reconciles the applicable Stage-2 matrix and retains explicit qualifications. Stage 6 owns full D3 and full S-02 as assigned below. |

[P3 owner decision §7](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md#7-individual-qualification-dispositions)

## 7. Outstanding findings and deferred obligations

| Item | Status at closure | Governing downstream route |
| --- | --- | --- |
| F-G2-01 | **OPEN — REPOSITORY CI BRANCH-COVERAGE GATE FAILURE**. Recorded measurement **296/450 = 65.77%**. Required threshold **70%**. | Separate owner decision required. WP-S25-4 carries the finding and the Stage-2 owner reconciles its acceptance consequence. The recorded workflow conclusion remains a coverage-gate failure. |
| D4.5 and full S-02 | **Outstanding in Stage 6.** D4.5 covers cancellation during native staging. Full S-02 requires the ten-invariant native D4 matrix. | Stage 6 owner and independent reviewer. WP-S25-4 owns the applicable Stage-2 subset, including the serialized native schedule. |
| F-02 crash and power loss | **Outstanding in Stage 6.** The sealed recovery receipts qualify managed fault injection within the exercised scope. Crash and power-loss resilience require dedicated evidence. | Separately commissioned Stage 6 durability work, with independent qualification and owner decision. |
| Legacy D3 graph noninterference | Historical before/after node, edge, count and traversal coverage carried. P2 exercised publication/authority and native mirror population. | WP-S25-4 reconciles the real-fixture/native-mirror portion under matrix §§8, 16 and the entry decision §9. Stage 6 owns the full canonical real-project corpus. |
| Applicable D5 variants, including schema and namespace refusal | Carry earlier isolated unsupported-schema and namespace refusals (SQ-06 and IF-05). P2's page-binding tamper and stale-publication refusal cover separate variants. Preserve the SQ-04 amendment and the D5 refusal boundary for experimental codecs. | WP-S25-4 reconciles the D5 and namespace gates under matrix §§10, 13, 15–16, including SQ-06 placement and floor re-verification. Authentic-format scope follows the SQ-04 owner amendment. |
| Graph-only isolation | Carry WP-S25-1's authentic graph-only evidence: legacy graph readable with occurrence `AnalysisUnavailable`. P2's missing-authority result exercised a different cause of unavailability. | WP-S25-4 reconciles D5.1/D5.2, SQ-04 and IF-05 under the applicable refusal and historical-fixture gates. |
| Linux re-qualification | Outstanding for behaviors outside WP-S25-2's bounded genuine Ubuntu x64 scope. SQ-05 remains satisfied with qualifications for that demonstrated scope. P2 supplied Windows x64 evidence. | WP-S25-4 maps each Linux claim to verified WP-S25-2 receipts under matrix §14 and records unmet applicable requirements or unverified behaviors at entry/final reconciliation. Stage 6 owns full platform certification. |
| Applicable D4 invariants carried from earlier evidence | P2 §15 classifies D4.6–D4.10 as carried outside its fresh coverage of the complete invariants. The fresh reopen receipts establish the narrower exercised identity/availability behavior. | WP-S25-4 reconciles supersession, full reopen equivalence, contributor/deletion, deterministic ordering/limits and the serialized native schedule under matrix §§9, 12, 16 and SQ-02/SQ-03. Stage 6 owns the full matrix and stronger concurrency guarantees. |

[Work-package plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) · [WP-S25-2 decision §§6–10, 13](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md) · [P2 §15](ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01.md#15-d3d4d5-qualification-matrix-and-requirement-traceability)

## 8. WP-S25-4 reconciliation handoff

The three P3 matrix rows below carry historical evidence and remain outstanding for Stage-2 acceptance. WP-S25-3 closure leaves the applicable independent reconciliation gates with WP-S25-4 and the final acceptance decision with the Stage-2 owner. [P3 §§5.2–5.3](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md) · [Stage-2 acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md)

| Row and governing requirement | Existing historical evidence | P2 status and reason | Required WP-S25-4 reconciliation and gate |
| --- | --- | --- | --- |
| **B3 — Cross-project binding.** Matrix §1.3 requires binding against the exact verified compilation, with a cross-project target bound to the dependency's verified compilation and refusal where the defining compilation is missing. §2.2 governs composed identity and §3 governs cross-project binding. | [S2.3 final qualification](ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01.md) I-01/I-02 covers real cross-project `asmctx3`, defining `compctx2`, and authority binding. P3 cites P1 S1–S4 as integrated context. | **Carried.** P2 `Q2-INT` checked extraction/binding and persisted targets on the frozen corpus. A distinct assertion of the dependency's verified compilation and its missing-compilation negative was outside that receipt. | Bind positive and refusal evidence to the actual defining compilation and authority at the frozen review revision. Independently qualify any uncovered interaction. §16 **S-01 Provenance**, **Cross-Project**, and applicable **Semantic Identity** gates. |
| **B6 — Contributor union.** Matrix §2.3 and IF-01 require a stable `usage4` ID with unioned storage contributors for the same semantic fact; different semantic units or plugins remain distinct. | Historical S2.2 identity and [S2.3 lifecycle qualification](ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1.md) L4 / N4, `L04_*` and `L04b_*`. F-01 limits cross-unit union evidence to the demonstrated constructed case and related controls. | **Carried under Q-P2-06.** P2's integrated target/lifecycle run exercised its S-01 subset and omitted the two-storage-contributor union and distinct-unit controls. | Reconcile one fact/two contributors, distinct semantic unit/plugin cases, and the F-01 authority limitation. Independently qualify remaining mandatory interactions. §16 **Semantic Identity**, with IF-01 and the applicable D4.8 gate. |
| **B7 — Payload conflict.** Matrix §2.4 and IF-01 require rejection with `CodeGraphBatchRejectedException` when the same ID carries a different immutable payload. | Historical S2 native evidence, including [S2.3 lifecycle qualification](ANDIRIT-CI1A-S2.3-WP-S23-4-S23-LIFE-Q1.md) `L05_*`: real identity, conflicting payload rejected, prior state preserved. | **Carried under Q-P2-06.** P2 exercised managed recovery and a persisted page-binding mismatch. Its scenario set omitted the same-ID conflicting-payload control. | Reconcile prior native rejection and state-preservation evidence at the frozen revision and independently qualify any remaining conflict interaction. §16 **Semantic Identity**, with IF-01 refusal coverage. |

## 9. Formal closure decision

**WP-S25-3 — CLOSED WITH QUALIFICATIONS.** P0 adoption, P1 integrated proof, the owner clarification, the bounded P1 delta, P2 qualification and P3 acceptance are accounted for. The closure condition from P3 is satisfied by the verified additive erratum. The evidence checks found no unresolved mandatory WP-S25-3 gate. The six P2 qualifications and all continuing findings retain the routes recorded in §§6–8.

**S-01 — SATISFIED WITH QUALIFICATIONS.** P3's owner acceptance remains the controlling decision. This commission verifies its evidence and records closure within the accepted scope. [P3 decision §§9–10](ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md)

## 10. Scope and non-claims

The closure covers documentation, evidence verification, and status reconciliation. The accepted S-01 envelope remains finite, Windows x64 and serialized single-writer, with Linux evidence bounded to WP-S25-2 and durable reopen/recovery claims bounded to the exercised cases. Public Find Usages, WP-S24-3 cross-target functionality, optimization, migration/dual-write, and Stage 6 each remain outside this commission. S2.5 and Stage-2 acceptance remain pending. Historical reviews, contracts, test harnesses and sealed evidence retain their original bytes.

## 11. WP-S25-4 entry eligibility

**WP-S25-4 — ELIGIBLE FOR ENTRY ASSESSMENT, SUBJECT TO SEPARATE OWNER AUTHORIZATION.** WP-S25-1, WP-S25-2 and WP-S25-3 have completed independent review and owner disposition within their respective qualifications. The current regression floor is supported by the sealed P2 results and the unchanged production/test baseline. Under the plan the separately commissioned entry assessment must freeze the actual reviewed SHA, verify the then-current regression floors and evidence, fix exact prerequisites against the then-current accepted states, and identify the remaining independent integration work. WP-S25-4 implementation authority and the later Stage-2 owner-acceptance decision remain separate. [WP-S25-4 plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md#wp-s25-4--final-integrated-stage-2-qualification)

## 12. Recommended next commission

Proposed `ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01` to assess entry for final integrated Stage-2 qualification, pin the reviewed revision, confirm the regression and evidence floors, and fix the reconciliation scope including B3/B6/B7 and the routes in §§6–7. A subsequent owner authorization governs any execution.
