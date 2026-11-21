# ADR-CI1-01-CI1A-S2.5-WP-S25-4-M54-OWNER-01 — owner acceptance of M5.4 satisfaction under the amended contract

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-21. **Canonical branch:** `main`.
**Owner decisions:**
**M5.4 — SATISFIED UNDER AMENDED CONTRACT** (subject to final evidence integrity verification,
completed in §1 below).

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The owner accepts the M54-B-QUAL-01 independent **SUPPORTED** disposition on the pinned
> implementation `eafd632` and records M5.4 satisfaction under the amended contract
> (`adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6). The three owner-preserved limitations are
> binding on every downstream citation and are restated at Stage-2 acceptance. This decision grants
> no WP-S25-4 final closure and no Stage-2 acceptance; those remain separate owner decisions.

This is an additive owner decision. It changes no production source, test, fixture, CI
configuration, reviewer evidence, oracle or corpus, prior decision, historical review artifact, or
existing planning contract definition. The sealed M54-B-QUAL-01 evidence package is **not
modified**; the erratum recorded in §1 lives only in this document.

## 1. Preflight and final evidence integrity verification

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `3e6a0690df55d8e61b71e1f0813732b839141c9f` / `a111150c258568e17dadbba3f93c3145976811ec` (as expected) |
| Branch / worktree | `main`; exactly one; clean tracked/index; no stashes |
| Qualified implementation | `eafd632b...` / tree `823e3c65…`, strict ancestor of HEAD (verified via the published qualification branch ref) |
| Protected scope | `git diff eafd632 HEAD -- src tests .github` **empty** — no production/test/CI change since the qualified revision |
| Sealed package | `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01-evidence/` — 32 manifest entries, all byte-exact against the committed blobs (0 mismatches) |

**Byte-total reconciliation (commission §2).** The previous reviewer handoff prose reported two
different totals: **1,927,786** and **2,314,011** bytes. Independent reconciliation at this
commission:

1. The sealed manifest's `totalBytes` field is **2,314,011**; the independently recomputed sum of
   the 32 listed entry byte lengths is also **2,314,011** — they agree exactly.
2. The figure **1,927,786** matches no entry size, no tested subset sum, the manifest's own size
   (5,851 bytes), or the all-files sum (2,319,862 bytes).
3. Full-text search of every sealed text artifact finds **no occurrence** of `1,927,786` /
   `1927786` anywhere in the package.

**Conclusion: a reporting-only discrepancy in the prior handoff prose (a misrecollected total),
not an evidence-integrity defect.** The sealed package is intact and self-consistent. Per the
commission, this **additive erratum** is recorded here; no sealed evidence is modified. **No
blocker.**

## 2. Confirmed basis of acceptance

Confirmed against the sealed M54-B-QUAL-01 package before recording acceptance:

| Commission requirement | Confirmed value | Evidence |
| --- | --- | --- |
| All five exercise families supported | E1 (isolated refusal), E2 (mint-gate screen) + E2p (positive control), E3 (integrated accounting), E4/E5 (adversarial + guards) | `matrix.md` C1–C4 rows all PASS |
| 11 qualification tests passed | **11 passed / 0 failed / 0 skipped** | `qualification-run.log`, `trx/m54b-results.trx` |
| All targeted refusal branches exercised | **11/11 in-process refusal branches hit**; native gate and availability-mapping paths receipt-evidenced in the distinct reopen process | `receipts/refusal-path-coverage.json` (`allTargetsHit: true`), `e3b`/`e3b2`/`e3c` |
| No prohibited outcomes observed | no mint without a Complete defining resolution; no false-available/false-complete; no incorrect refusal reason; no unsupported-channel mint | `findings.md` prohibited-outcome screen |
| No Option A escalation condition triggered | all four conditions evaluated, none triggered | `findings.md` §Option A table |

## 3. Owner-preserved limitations (binding)

1. **Conservative false-negative behavior.** The demonstrated guarantee is fail-closed: a
   hypothetical false `MissingDefiningCompilation` would suppress a legitimate fact (false
   negative) and can never produce the prohibited outcomes. Any downstream citation must carry
   this direction statement.
2. **Constructed-control evidence does not establish natural reachability.** The isolated
   refusal, the status screen, the partial-coverage leg, and the authority-masking tamper are
   constructed controls, labeled as such in every receipt; no constructed control supports a claim
   that an admissible production input naturally reaches `MissingDefiningCompilation`.
3. **Exhaustive Roslyn symbol-association proof remains unproven and unclaimed.** The four
   inconclusive R3 compiler probes are preserved as inconclusive; the exhaustive association proof
   is neither claimed nor implied.

## 4. Obligation inventory / register / checklist updates

- `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md` — the M5.4 row records
  **SATISFIED UNDER AMENDED CONTRACT — 2026-11-21** with the qualification and limitations
  summary. The historical disposition D text in QUAL-02/03/04 and R3 remains preserved verbatim.
- `docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md` — dated additive note
  (2026-11-21) records the satisfaction and the preserved limitations.
- `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-closure-checklist.md` — M5.4 flipped to satisfied;
  the remaining WP-S25-4 final-closure item is the final integrated closure decision itself.

## 5. WP-S25-4 integrated closure assessment (summary)

The full row-by-row reconciliation is in
[`closure-assessment.md`](ANDIRIT-CI1A-S2.5-WP-S25-4-M54-OWNER-01-evidence/closure-assessment.md)
with the consolidated register in
[`residual-risk-register.md`](ANDIRIT-CI1A-S2.5-WP-S25-4-M54-OWNER-01-evidence/residual-risk-register.md).
Summary:

- **All three QUAL-04 matrix BLOCKED rows are now dispositioned**: M5.4 (satisfied under amended
  contract, this decision), M11.4 and M14.1 (accepted with qualifications, UBUNTU-OWNER-01;
  M14.2 likewise).
- **The two repository quality gates are closed**: F-G2-01 (368/466 = 78.96% branches,
  900/969 = 92.87% lines on the corrected revision, genuine-Ubuntu-verified) and the QUAL-04
  exact-byte formatting gate (exit 0 on a byte-exact export, genuine-Ubuntu-verified) —
  GATE-OWNER-01.
- **R1/R2/R3 and Q03-F-01** remain independently qualified and preserved (QUAL-01…04);
  preservation re-confirmed on `eafd632` in GATE-QUALIFICATION-01.
- **The four QUAL-04 obligation-BLOCKED entries** (F06, F16, I-01, required-evidence) had exactly
  these four causal blockers; all four are now discharged, so their final row-flips carry to the
  WP-S25-4 final-closure decision.
- **Genuinely outstanding**: no blocking obligation. Carried items: the SQ-06 inventory row-flip
  (its substantive test `Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` is qualified — M15.1
  PASS in QUAL-04, passing on `eafd632`); the B3/B6/B7 carried-evidence reconciliation; and the
  historical-evidence errata (G3-F01 PR #6 merge reconciliation; G3-F02 SQ-05 G2 package
  completeness; gate producer findings F-01…F-06).
- **Explicitly deferred (Stage 6 / named-deferral)**: full D3 real-fixture part, D4.5, full S-02
  native matrix, F-02 crash/power-loss durability, Q2-F09/Q2-F12, I-04 cancellation, full platform
  certification; WP-S24-3 optional/deferred.

## 6. Recommended final WP-S25-4 owner disposition

**Recommend the owner commission a WP-S25-4 final-closure decision** that records:
`WP-S25-4 — CLOSED WITH QUALIFICATIONS` for the Stage-2 qualification scope, restating:
(a) the M11.4/M14.1/M14.2 accepted qualifications and the Ubuntu 26.04.1 demonstrated environment
scope; (b) the M5.4 amended-contract basis with the three preserved limitations; (c) the closed
F-G2-01 and formatting gates; (d) the carried qualifications and historical-evidence errata; and
(e) the explicitly deferred Stage 6 obligations. **Stage-2 final integration and Stage-2 owner
acceptance remain separate decisions** and must not be inferred from that closure.

## 7. Repository discipline

Only this decision document, its evidence package (closure assessment + residual-risk register +
manifest), and the additive updates to the obligation inventory, the consolidated register, and
the closure checklist are added, locally on `main`, under the single-worktree policy. No
production, test, dependency, formatter, coverage or CI-policy change; no sealed historical
evidence is modified; no fetch, push, or remote operation. The commit/tree identities are reported
in the handoff.