# Binding contract re-derivation — M11.4 and M14.1 (G3 qualification time)

Re-derived independently from the governing contract at HEAD (`git grep` on
`docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`, which is the controlling
S2.5 matrix fed by the Stage-2 acceptance matrix and the obligation register). Quotations verbatim.

## Fixture legend (row preamble)

"**Fixtures** W = Windows x64 with native LatticeDb and serialized single writer, R2 = frozen
corpus plus independent generated-real controls, C = constructed authority/storage controls, H =
the two authentic historical sets on disposable copies, **L = genuine Ubuntu x64 via accepted
sealed receipts**."

## M11.4 (matrix §11, row 4)

- Requirement: "Cross-platform native tests pass".
- Evidence class: A (accepted sealed receipts); fixture **L**.
- Independent check: "Verify accepted G1/G2/G3 commitment and expanded native suite coverage".
- Receipt: "Link Linux source/run/log identities".
- Pass condition: "All named observations and controls agree; block on Missing measurements,
  unbounded lookup or Linux requirement unsupported."
- Qualifications carried: MD-02, SQ-07, O-Q2-01, no new latency threshold.
- **Derived acceptance test for this commission**: a genuine hosted-Ubuntu x64 run of the
  *expanded* native suite, at a revision whose source/run/log identities are linked and sealed.
  "Expanded" is current-suite-relative: the SQ-05 acceptance covered 52 cases; the corrected
  revision carries 75. G3 verified: genuine hosted Ubuntu 26.04.1 x64, run 44746950426 with
  linked identities (pinned commit = PR head = run `head_sha`; executed merge tree = pinned tree
  `823e3c65…`), 75/75 pass, all counters re-derived from the sealed raw artifacts.

## M14.1 (matrix §14, row 1)

- Requirement: "Linux CI executes native tests — **Entire current expanded**
  `Andirit.LatticeDb.Tests` suite passes on Linux; retain the original 38 Stage 1 tests plus
  additions (REM-01 reviewed 43 on Windows)".
- Evidence class: A; fixture **L**.
- Independent check: "Confirm genuine hosted run identity, pinned tree, expanded suite and
  observed behaviors from owner-bound evidence."
- Receipt: "G1/G2/G3 chain, Ubuntu runner and per-suite counts, scope matrix."
- Pass condition: "All named observations and controls agree; block on Revision or behavior
  mismatch or insufficient native coverage."
- Qualifications carried: SQ-05, F-G2-01; supplemental runs distinct.
- **Derived acceptance test for this commission**: the *entire* current 75-case suite passes on
  genuine hosted Ubuntu at the pinned revision — no filters, no partial subsets — with the
  G1/G2/G3 chain evidenced and the supplementary suites recorded as distinct. G3 verified: 75/0/0
  with no filter in the raw log, plus supplementary 345/372/157 and pack recorded distinctly.

## M14.2 (matrix §14, row 2)

- "Platform-specific behavior documented — File locking, WAL, memory mapping differences noted";
  same check/receipt wording as M14.1.
- **Derived**: a platform-behavior record from the run. G3 verified the run now provides a
  suite-level Linux/Windows counter-parity record; internal mechanics remain uninstrumented
  (qualification F2, carried).

## SQ-05 register row (context)

"Linux CI/native suite expanded to current tests with logs; describe locking/WAL/mmap
differences." The WP-S25-2 satisfaction was bounded to the 52-test suite; the current 75-test
suite is what this commission qualifies, at the corrected revision `eafd632`, under the owner
G1/G2 authorization of 2026-11-20.