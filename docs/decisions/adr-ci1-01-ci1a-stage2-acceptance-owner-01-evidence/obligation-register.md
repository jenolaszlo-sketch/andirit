# Consolidated Stage-2 obligation register — STAGE2-ACCEPTANCE-OWNER-01

**Recorded:** 2026-11-22 with `adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`.
Consolidates the Stage-2 obligation register dispositions at acceptance time. The register rows
themselves remain preserved verbatim in `docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md`
and the S2.5 inventory; this is the acceptance-time summary.

## 1. Stage-2 blocking obligations — all closed

| Blocker (as of QUAL-04, 2026-11-19) | Closure |
| --- | --- |
| M5.4 (`Unresolved` refusal reachability) | **CLOSED** — satisfied under amended contract (M54-AMEND/M54-OWNER-01); limitations MR-1…MR-4 preserved |
| M11.4 (corrected-revision genuine Ubuntu) | **CLOSED** — accepted with qualifications (UBUNTU-OWNER-01); run 44746950426; Ubuntu 26.04.1 x64 demonstrated scope |
| M14.1 (current expanded suite on Linux) | **CLOSED** — accepted with qualifications (75/0/0, five-identity equivalence) |
| F-G2-01 (LatticeDb coverage gate) | **CLOSED** — 368/466 = 78.96%, 900/969 = 92.87% at `eafd632`; unchanged policy |
| QUAL-04 exact-byte formatting gate | **CLOSED** — exit 0 on byte-exact export; green in run 44746950426 |
| F06 / F16 / I-01 (QUAL-04 BLOCKED entries) | **CLOSED** — causal blockers discharged; qualified on existing evidence (FINAL-OWNER-01 §3) |
| Required-evidence conditional blocker | **NOT TRIGGERED** — no mandatory entry evidence failure at the pinned revision |

## 2. Stage-2 acceptance register state

| Class | Count | Items |
| --- | --- | --- |
| Matrix rows (64) | 46 PASS / 10 PASS-WITH-QUALIFICATION / 0 FAIL / 1 REUSED QUALIFIED / 4 DEFERRED | S2.5 qualification matrix |
| Obligations (78-entry register, QUAL-04 baseline: 53 QUALIFIED, 4 BLOCKED, 11 DISCHARGED, 7 OPEN-STAGE6, 1 OPEN-S2.5, 2 AMENDMENT-SUBJECT) | the 4 BLOCKED entries closed (§1); the F-G2-01 OPEN-S2.5 entry closed; AMENDMENT-SUBJECT entries remain under their amendments (SQ-04 satisfied under amended contract; Q2-F11 on discovery) | Register rows preserved verbatim; dispositions in the owner records |
| New acceptances since QUAL-04 | M5.4 satisfaction; M11.4/M14.1/M14.2 acceptance; two gate closures; WP-S25-4 closure; this Stage-2 acceptance | §1 and the acceptance record |

## 3. Carried reconciliations (scheduled, non-blocking)

| ID | Item | Schedule |
| --- | --- | --- |
| CR-1 | SQ-06 row-flip (done in FINAL-OWNER-01; substantive test qualified M15.1) | recorded at inventory |
| CR-2 | F06/F16/I-01/required-evidence row-flips (done in FINAL-OWNER-01) | recorded at inventory |
| CR-3 | B3/B6/B7 carried-evidence reconciliation (L04/L05 on `eafd632`; QUAL-01…04) | restated at Stage-2 acceptance (this record) |
| CR-4 | Consolidated-register addenda alignment (S2.5 register addendum of 2026-11-22 + this record) | this document |

## 4. Deferred (Stage 6) — not discharged by acceptance

DF-1 full D3 real-fixture part; DF-2 D4.5 full native fault matrix; DF-3 full S-02 ten-invariant
matrix (applicable subset qualified); DF-4 F-02 crash/power-loss/multi-process durability;
DF-5 Q2-F09/Q2-F12/I-04 cancellation; DF-6 full platform certification; DF-7 WP-S24-3 optional
cross-target scope, public Find Usages, optimization; DF-8 M9.5 cancellation path, M12.1–M12.3
full native matrix. Entry proposal: `stage6-entry-proposal.md`.

## 5. Historical findings (errata, non-blocking)

GH-1 SQ-05 PR #6 merge reconciliation (owner remote-resource disposition scheduled); GH-2 SQ-05
G2 package completeness errata; GH-3 gate producer findings F-01…F-06; GH-4 M54-B handoff
byte-total prose erratum (sealed package intact).