# Binding contract derivation — M11.4 and M14.1 for ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01

Preparation-time derivation from the governing contracts (quotations verbatim; nothing inferred).
Authority chain: `adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md` → the S2.5 qualification matrix
(`docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`) → the obligation register →
the WP-S25-2 SQ-05 owner acceptance.

## Fixture legend (binding for both rows)

`docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`, preamble: **"L = genuine
Ubuntu x64 via accepted sealed receipts"**. The matrix further requires per-row independent checks
and receipts; a "Pass / blocker" column requires that the named observations agree and blocks on
missing measurements or an unsupported Linux requirement.

## M11.4 — re-derived

| Element | Binding content |
| --- | --- |
| Row | §11 "Performance and Operational", row 4 |
| Requirement | "Cross-platform native tests pass" |
| Class / fresh / fixture | **A / no / L** — evidence class A (accepted sealed receipts), fixture L (genuine Ubuntu x64) |
| Independent check | "Verify accepted G1/G2/G3 commitment and expanded native suite coverage" |
| Receipt | "Link Linux source/run/log identities" |
| Pass condition | "All named observations and controls agree; block on Missing measurements, unbounded lookup or Linux requirement unsupported" |
| Qualifications carried | "MD-02, SQ-07, O-Q2-01, no new latency threshold" |

**Derived obligation:** a G1/G2/G3 commitment (publication-and-execution authorization → genuine
hosted Ubuntu execution → independent assessment) with linked Linux source/run/log identities,
covering the **expanded** native suite. "Expanded" is current-suite-relative: at the qualified
SQ-05 revision the suite was 52 cases; at the pinned remediation revision `eafd632` it is **75**
cases, so the accepted SQ-05 receipts do not satisfy M11.4 until re-established at the current
suite. Route: `adr-ci1-01-ci1a-s2-5-wp-s25-2-route-owner-01.md` Route A (GitHub-hosted Ubuntu via
the existing unmodified workflow) is the established authorized route; Route B (owner-provided
native runner) is the documented fallback. WSL2 or Windows results are **not** fixture L.

## M14.1 — re-derived

| Element | Binding content |
| --- | --- |
| Row | §14 "Cross-Platform (SQ-05)", row 1 |
| Requirement | "Linux CI executes native tests — Entire current expanded `Andirit.LatticeDb.Tests` suite passes on Linux; retain the original 38 Stage 1 tests plus additions (REM-01 reviewed 43 on Windows)" |
| Class / fresh / fixture | **A / no / L** |
| Independent check | "Confirm genuine hosted run identity, pinned tree, expanded suite and observed behaviors from owner-bound evidence" |
| Receipt | "G1/G2/G3 chain, Ubuntu runner and per-suite counts, scope matrix" |
| Pass condition | "All named observations and controls agree; block on Revision or behavior mismatch or insufficient native coverage" |
| Qualifications carried | "SQ-05, F-G2-01; supplemental runs distinct" |

**Derived obligation:** the **entire current** LatticeDb suite (75 cases at `eafd632`, which
includes the original Stage 1 tests and every addition) must pass on genuine hosted Ubuntu, with
the five-identity checkout/tree verification (G1 §7 pattern: pinned branch commit = PR head =
run `head_sha`, and the synthetic merge tree equal to the pinned tree). A partial-suite or
filtered run does not satisfy the row ("no test filter" was part of the SQ-05 G2 procedure).

## M14.2 (rides the same run)

"Platform-specific behavior documented — File locking, WAL, memory mapping differences noted";
receipt "G1/G2/G3 chain, Ubuntu runner and per-suite counts, scope matrix". The prior run's L7
limitation applies: suite-level parity is observed; internal locking/WAL/mmap mechanics are not
directly instrumented.

## SQ-05 register row (context, preserved)

"Linux CI/native suite expanded to current tests with logs; describe locking/WAL/mmap
differences." The WP-S25-2 owner acceptance bounded the satisfaction to the 52-test suite at
`7457284…`; the current 75-test suite requires the corrected-revision run. F-G2-01 is closed at
the repository level by the GATE-OWNER-01 decision (368/466 = 78.96% at `eafd632`), so the Linux
coverage steps are expected to pass on their own recorded actuals.