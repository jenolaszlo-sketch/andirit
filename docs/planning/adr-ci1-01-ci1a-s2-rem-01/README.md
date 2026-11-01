# ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01

**Status (2026-11-02):** `ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01 ACCEPTED WITH QUALIFICATIONS`.
Independent `REM-01-Q1` reports `SUPPORTED WITH QUALIFICATIONS`; the owner has
accepted the bounded foundation. See the [owner handoff](../adr-ci1-01-ci1a-s2-rem-01-owner-handoff.md)
and [obligation register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md).
S2.1 is eligible for separate commissioning. Stage 2 remains **unauthorized**.

**Starting evidence:** Q1B independent review, disposition
`REMEDIATION REQUIRED` (6 blocking, 8 significant, 2 minor; 23 probes).

## Documents

| Document | Purpose |
|---|---|
| `contract-reconciliation.md` | Contract-first reconciliation of every Q1B finding |
| `identity-contract.md` | Exact R2 `usage4`/`asmctx3` mapping, frozen vectors and compatibility |
| `coverage-and-availability.md` | Resolution/coverage/availability separation, persistence and zero-fact semantics |
| `publication-uncertainty.md` | Known/unknown commit outcomes, quarantine, reconciliation and retry rules |
| `stage2-gate-reconciliation.md` | Corrected authorization/acceptance classification |
| `evidence-integrity-erratum.md` | Stage 1 and Codex evidence discrepancies, exact hashes and materiality |
| `finding-disposition.md` | All Q1B IDs and their disposition |
| `qualification-matrix.md` | Requirements, tests, commands, results and remaining evidence obligations |

## Implementation evidence

`docs/evidence/adr-ci1-01-ci1a-s2-boundary-rem-01.md` records the source baseline,
changed paths, test commands/results and environmental limits.

## Independent review

`docs/evidence/adr-ci1-01-ci1a-s2-boundary-rem-01-q1-handoff.md` is the handoff to
`ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01-Q1`.

## Authority and limits

Candidate C, D3, D4, D5 and the accepted R2/Q3 identity semantics are preserved.
REM-01 does not implement the Stage 2 Roslyn extractor, `compctx1` production
construction, a public Find Usages API, migration, dual-write or a new
publication authority. Producer tests are not independent acceptance.
