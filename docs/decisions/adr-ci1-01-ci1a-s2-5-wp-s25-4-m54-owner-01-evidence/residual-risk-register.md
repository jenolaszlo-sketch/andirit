# Consolidated qualification and residual-risk register — WP-S25-4 (M54-OWNER-01)

**Recorded:** 2026-11-21. Consolidates every live qualification, accepted limitation, historical
evidence finding, and deferred obligation for the WP-S25-4 closure record and the Stage-2
acceptance restatement. Nothing here reopens accepted work or grants closure.

## 1. Live qualifications to restate at Stage-2 acceptance

| ID | Qualification | Source |
| --- | --- | --- |
| Q-L1 | Linux qualification restricted to the demonstrated hosted environment (Ubuntu 26.04.1 LTS x64, image `ubuntu-26.04`, SDK 10.0.404, hosted runner) | UBUNTU-OWNER-01 §4 |
| Q-L2 | Only behavior exercised by the 75-case suite is qualified | UBUNTU-OWNER-01 §4 |
| Q-L3 | Managed fault injection only — no crash/power-loss durability (F-02) | All prior acceptance records |
| Q-L4 | Locking/WAL/mmap internals not directly instrumented; platform claims are suite-observed counter parity | UBUNTU-OWNER-01 §4 |
| Q-L5 | Hosted-run independence is receipt-based; G2/G3 same-session sequencing limitation recorded | M54-OWNER-01 §1; SQ-05 G3 amendment precedent |
| Q-L6 | Windows-x64, serialized single-writer baseline for non-Linux scope | All prior acceptance records |
| Q-L7 | Restricted real-authority cross-unit union (F-01); constructed cross-contributor controls only | QUAL-02/03/04 |
| Q-L8 | Reviewer-independence limitations (same-harness; organizational independence unestablished) | QUAL-04 and successors |

## 2. M5.4 amended-contract limitations (binding on every citation)

| ID | Limitation |
| --- | --- |
| MR-1 | Conservative false-negative behavior: a hypothetical false `MissingDefiningCompilation` suppresses a legitimate fact and cannot produce prohibited outcomes |
| MR-2 | Constructed-control evidence does not establish natural reachability |
| MR-3 | Exhaustive Roslyn symbol-association proof remains unproven and unclaimed; the four R3 probes remain inconclusive |
| MR-4 | Structural bound (M54-B-F04): an admissible complete universe cannot carry a member depending on an unverified compilation — bounds but does not eliminate identity-substitution residual risk |

## 3. Historical evidence findings (errata to carry, not blockers)

| ID | Finding | Disposition |
| --- | --- | --- |
| GH-1 | G3-F01: SQ-05 PR #6 merged (fast-forward, 2026-11-10T23:05:29Z) after the SQ-05 G3 assessment recorded it open; no decision authorizes it | Owner remote-resource reconciliation recommended at final closure; no retroactive authorization; content is an ancestor of remote main |
| GH-2 | G3-F02: SQ-05 G2 evidence package committed 14 of 18 manifest entries (four Ubuntu coverage artifacts never committed) | Additive historical errata recommended; current-run artifacts are not represented as replacements; counters survive in the G3 package |
| GH-3 | Gate producer evidence findings F-01…F-06 (manifest convention; missing exit-code raw artifacts; 4,4555→4,555 blob count; 24→23 test count; parent-identity in the report seal; undocumented authority-remainder note) | Preserved as historical producer-evidence deficiencies; technical results were independently reproduced |
| GH-4 | M54-B handoff prose byte-total discrepancy (1,927,786 vs 2,314,011) | Reporting-only; erratum recorded in M54-OWNER-01 §1; sealed package verified intact (32/32 entries byte-exact) |
| GH-5 | A-03/Q-F4, New-03-style notes and the SQ-04 amended-contract exclusions | Carry as recorded |

## 4. Deferred obligations (named owners; not discharged)

| ID | Item | Owner |
| --- | --- | --- |
| DF-1 | Full D3 real-fixture part (native-mirror full corpus) | Stage 6 |
| DF-2 | D4.5 full native fault matrix | Stage 6 |
| DF-3 | Full S-02 ten-invariant native matrix (applicable subset qualified) | Stage 6 |
| DF-4 | F-02 crash/power-loss/multi-process durability | Stage 6 |
| DF-5 | Q2-F09 (rebuild blocks queries/no cancellation), Q2-F12, I-04 cancellation | Stage 6 |
| DF-6 | Full platform certification (other distributions/architectures; internal locking/WAL/mmap instrumentation) | Stage 6 |
| DF-7 | WP-S24-3 optional cross-target scope; public Find Usages; optimization | Separate owner commissions (unauthorized) |
| DF-8 | M9.5 cancellation path; M12.1–M12.3 full native matrix | Named deferral (Stage 6) |

## 5. Carried reconciliations for the final-closure decision

| ID | Item |
| --- | --- |
| CR-1 | SQ-06 inventory row-flip (substantive test qualified as M15.1 PASS; entry-status wording is entry-time historical text) |
| CR-2 | F06 / F16 / I-01 / required-evidence entry row-flips (causal blockers discharged) |
| CR-3 | B3 / B6 / B7 carried-evidence reconciliation (L04/L05 lifecycle tests qualified on `eafd632`; QUAL-01…04 evidence) |
| CR-4 | Consolidated-register addenda alignment (this register vs. the S2.5 register addendum of 2026-11-21) |

## 6. Residual-risk statement

No unresolved blocking obligation remains for the WP-S25-4 Stage-2 qualification scope. The
residual risks are: (a) the M5.4 conservative-false-negative direction (a legitimate fact could in
principle be suppressed — unquantified, fail-safe); (b) the Linux scope being a single hosted
image generation; (c) the four historical evidence findings, which are documentation-level and
reconciled. All are accepted with the qualifications of §1–§2 and restated at Stage-2 acceptance.