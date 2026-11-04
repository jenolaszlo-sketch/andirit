# CI1A S2.2 — Scope and Entry-Criteria Preparation

**Commission:** `ADR-CI1-01-CI1A-S2.2-ENTRY-01`.
**Stage:** S2.2 (Roslyn semantic occurrence extraction).
**Status:** `S2.2 ENTRY PREPARATION COMPLETE — READY WITH EXPLICIT QUALIFICATIONS`.
**S2.2 implementation:** **NOT AUTHORIZED.** This directory prepares a separate owner
authorization decision; it authors no production code, tests, schemas or fixtures.

This package recovers the actual S2.2 scope from the accepted architecture and
records the entry contract, the obligation/dependency matrix, a draft
implementation commission, a risk/qualification register and a decision-ready
owner handoff. It was produced from the canonical `main` checkout at
`a1cf47083747bae90293ec074b35fe8078bd0d6e`
(tree `ebba0daf2977315a5c09551df0de698afeb6696f`), which is the recorded F12
owner-acceptance commit and one direct documentation-only descendant of the
qualified source-bound R2 reference.

## Documents

| Document | Content |
| --- | --- |
| [`scope-and-entry-criteria.md`](scope-and-entry-criteria.md) | Recovered S2.2 objective/deliverables, dependency contracts, boundaries, authoritative specifications, exclusions, entry preconditions, bounded work packages, invariants, verification, acceptance criteria, evidence, stop conditions and required owner decisions |
| [`obligation-dependency-matrix.md`](obligation-dependency-matrix.md) | Per-obligation source, requirement, current evidence, blocking stage, required probe/remediation, dependencies and owner decisions for Q-F2, Q-F3, A-01, A-02, S-01, NEW-01, NEW-04 (plus F16/NEW-02 and standing constraints) |
| [`implementation-commission-draft.md`](implementation-commission-draft.md) | Draft, not-yet-issued S2.2 implementation commission with bounded work packages and evidence obligations |
| [`risk-and-qualification-register.md`](risk-and-qualification-register.md) | Risks, retained qualifications and non-production limitations that any authorization must accept |
| [`owner-handoff.md`](owner-handoff.md) | Decision-ready handoff, final disposition and the exact owner decisions required |

## Verified baseline (summary)

Canonical repository `D:\projects\Andirit`, branch `main`, clean tracked/untracked
tree, one canonical worktree, no stashes, no push (`origin/main` =
`31c33a934aa13e373d9a3a6ffbc54d789eef092c`, an ancestor; local `main` is 30
commits ahead).

| Role | Commit | Tree |
| --- | --- | --- |
| Canonical `main` / F12 owner acceptance | `a1cf47083747bae90293ec074b35fe8078bd0d6e` | `ebba0daf2977315a5c09551df0de698afeb6696f` |
| Qualified source-bound R2 reference (F12 parent) | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |
| S2.1 reviewed production source | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |
| S2.1 evidence-only tip | `0fa63ed2a0f7f1b5ffe21f3791e93e81eb4cb329` | `f713cb6e4a1dbfb4f531f6971a37893902b41435` |

The `7f8d035…` (R2) and `b7fea0d…` (S2.1 owner baseline) commits are both
ancestors of `a1cf470…`. The `a1cf470…` diff against R2 is documentation-only
(8 files, +452/−5; `ROADMAP.md`, `docs/agent-code-intelligence-roadmap.md`, the
S2.0 owner-acceptance decision, the obligation register, and four planning
documents). No `src/`,
`tests/`, `benchmarks/`, `smoke/` or schema path changed.

## Authority

- [`adr-ci1-01-owner-decision.md`](../../decisions/adr-ci1-01-owner-decision.md) —
  Candidate C selected; D3/D4/D5 binding.
- [`adr-ci1-01-ci1a-s2-obligation-register.md`](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md)
  — consolidated obligation register and progression (§§4A, 5, 7, 8).
- [`adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md)
  — F12 `ACCEPTED WITH QUALIFICATIONS`; S2.2 `NOT AUTHORIZED`.
- [`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md)
  — S2.1 `ACCEPTED WITH QUALIFICATIONS`; Q-F2/Q-F3/A-01/A-02 preserved.
- [`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md)
  — `S2.2-ID OPTION A ACCEPTED`; mapping contract and minting gate.
- [`adr-ci1-01-pre-release-compatibility-policy.md`](../../decisions/adr-ci1-01-pre-release-compatibility-policy.md)
  — no migration/dual-write/legacy readers; refuse or re-ingest.
- [`stage2-boundary.md`](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md),
  [`stage2-acceptance-matrix.md`](../adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md)
  — Stage 2 S2.2 definition and per-requirement matrix.
- [`identity-contract.md`](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md),
  [`provenance-contract.md`](../adr-ci1-01-ci1a-s2-1/provenance-contract.md)
  — frozen `usage4`/`asmctx3`/`compctx1` and the `compctx2` amendment.
- [`s2-0-owner-handoff.md`](../adr-ci1-01-ci1a-impl-01/s2-0-owner-handoff.md),
  [`s2-2-identity-handoff.md`](../adr-ci1-01-ci1a-impl-01/s2-2-identity-handoff.md)
  — current gate order and S2.2 mapping/slice handoff.

## Boundary of this package

This package is additive documentation. It preserves every historical finding,
failed review, frozen vector, manifest, qualification and non-production
limitation. It does not modify production code, tests, frozen manifests,
oracles, evidence or historical reports, and it authorizes no S2.2, S2.3 or
downstream work.
