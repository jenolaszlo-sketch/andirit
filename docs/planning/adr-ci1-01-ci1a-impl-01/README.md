# ADR-CI1-01-CI1A-IMPL-01 — Candidate C production integration (specification)

**Status: `CI1A-IMPL-01 SPECIFIED — READY FOR INDEPENDENT SPECIFICATION REVIEW.`**
**Production implementation: NOT AUTHORIZED.**

This directory specifies the production integration and qualification of
**Candidate C** (separate, source-owned occurrence facts with native LatticeDB
persistence), selected by
[`docs/decisions/adr-ci1-01-owner-decision.md`](../../decisions/adr-ci1-01-owner-decision.md)
and accepted after review by
[`docs/decisions/adr-ci1-01-owner-01-q1-acceptance.md`](../../decisions/adr-ci1-01-owner-01-q1-acceptance.md).

The specification is documentation only and authorizes nothing. A separate
implementation agent and an independent reviewer execute it; implementation
must not begin without separate owner authorization.

## Documents

| Document | Content |
| --- | --- |
| [`specification.md`](specification.md) | The production contract: scope, identity map, Candidate C contract, D3/D4/D5, S-01 provenance, qualification strategy, sequence, acceptance criteria |
| [`source-integration-map.md`](source-integration-map.md) | Actual production surfaces (files/types), required changes, unchanged contracts, smallest additions |
| [`acceptance-matrix.md`](acceptance-matrix.md) | Transcribed D4 invariants + D3/D4/D5 test matrices and independent-review requirement |

## Authority

- `docs/decisions/adr-ci1-01-owner-decision.md` — Candidate C selected; B2
  evaluated, not selected; D3/D4/D5 binding.
- `docs/decisions/adr-ci1-01-owner-01-q1-acceptance.md` — owner acceptance of the
  independent review (`SUPPORTED WITH QUALIFICATIONS`), carrying S-01, S-02,
  I-01…I-05 and M-01.
- `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-owner-01-q1` — independent
  review package (manifest `evidence/q1-package-hashes.json`, 20 entries, tree
  `7f8d932c…`).

## Spec base

Canonical commit `ffdb14881511258c36d3fdce01bcdab3d2463ad9`
(tree `45dddab1d38a412dd7537257fd93ffc22842347a`).

## Carried obligations

S-01 (source-byte provenance), S-02 (native D4 qualification), I-01 (pin
surfaces), I-02 (D3 corpus), I-03 (refusal vocabulary), I-04 (maintenance/
cancellation policy), I-05 (no production interleaving hook), M-01 (per-instance
historical fixture provenance).
