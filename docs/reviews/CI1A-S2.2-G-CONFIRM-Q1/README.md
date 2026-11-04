# ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1 — Independent F16/NEW-02 semantic authority confirmation

**Commission:** `ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1` (independent qualification only).
**Date:** 2026-11-04. **Repository:** canonical `D:\projects\Andirit`, `main`.
**Reviewed commit/tree:** `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` /
`ba9338d74bf59c584bef340dbda49ed46eb1c5d9`.
**Verdict:** **`G-CONFIRM PASS WITH EXPLICIT QUALIFICATIONS`**.

This is an independent reviewer package. It confirms the **semantic authority,
expected-value derivation, provenance and consistency** of the 14 nine-part
F16/NEW-02 rows before dependent qualification-test authoring. It issues no
implementation authorization and does not claim production mapping, minting,
association, persistence or reopen.

## Result

- 14/14 rows authority-confirmed: **CONFIRMED 4**, **CONFIRMED WITH
  QUALIFICATIONS 10**, **INSUFFICIENT EVIDENCE 0**, **CONTRADICTED 0**.
- The NEW-02 corrected identity matrix is independently reproduced from the
  contract text; the frozen R2 oracle replays `185/185` with the accepted output
  hash `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`.
- Four documentation corrections (C1–C4) recorded; no authority contradiction.

## Documents

| Document | Content |
| --- | --- |
| [`REPORT.md`](REPORT.md) | Independent qualification report, verdict, scope, method, boundaries |
| [`fourteen-row-confirmation-matrix.md`](fourteen-row-confirmation-matrix.md) | Row-by-row nine-part confirmation and dispositions |
| [`authority-and-expected-value-provenance.md`](authority-and-expected-value-provenance.md) | Authority chain, frozen tuples, profiles, A/B/C status |
| [`adversarial-findings.md`](adversarial-findings.md) | Adversarial challenges, outcomes and corrections C1–C4 |
| [`verification-log.md`](verification-log.md) | Exact commands, environment, hashes, results |
| [`owner-handoff.md`](owner-handoff.md) | Decision-ready handoff and outstanding prerequisites |
| [`evidence/`](evidence) | Standalone reconstruction script/output, frozen oracle output, section diff, hashes |

## Boundaries

- Independent qualification of F16/NEW-02 **authority only**; no WP4–WP9,
  `G-MINT`, minting, persistence/reopen, S2.3+ or publication.
- `G-CONFIRM` and `G-MINT` were **OPEN** at review time; this package records a
  `G-CONFIRM` verdict and leaves `G-MINT` **OPEN**.
- Frozen evidence, historical reports and accepted decisions are unmodified;
  producer tests are cited only as corroboration.

## Authority

- Corrected G-CONFIRM package:
  [`docs/planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md`](../../planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md)
- Execution handoff §D:
  [`docs/planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md`](../../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md)
- Stage 2 acceptance matrix §2:
  [`docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md`](../../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md)
- Option A decision, entry + foundation owner decisions, S2.1/S2.0 acceptances,
  identity contract (see [REPORT.md](REPORT.md) §1).
