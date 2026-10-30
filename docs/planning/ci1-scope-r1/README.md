# CI-1-SCOPE-R1: planning remediation of independent findings

**Disposition: CI-1-SCOPE-R1 READY WITH QUALIFICATIONS.**
Recorded 2026-10-30 (Asia/Manila). Planning and contract remediation only.
No production implementation, schema, API, codec or package change is authorized
by this packet. No owner acceptance is issued.

This packet remediates the actionable findings of the independent review
`CI-1-SCOPE-Q1`, which returned **REMEDIATION REQUIRED** against the
CI-1-SCOPE-01 planning proposal.

## Git and authority

| Item | Verified identity |
| --- | --- |
| Repository / canonical checkout | `jenolaszlo-sketch/andirit` / `D:/projects/Andirit` |
| Branch | `main` |
| Starting commit (reviewed Q1 packet) | `7ab0aa7086861a368d91a79b802d538c8c206bb9` |
| Starting tree | `ffb6833e74836e2a318ab3c17e39a9c748310268` |
| Planning content predecessor | `69d5152cb3ac1f9699e82981e11c122f261ada16` |
| Production baseline | `cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2` |
| Q1 review workspace | `C:/Users/Laszlos/source/repos/Solo/ci1-scope-q1` |

Working tree was clean before R1. Only `docs/planning/ci1-scope-r1/**` and
clearly-marked R1 amendment blocks inside `docs/planning/ci1-scope-01/**`
are touched. No production source, existing test, schema, package, workflow
or accepted historical artifact is changed. Q1 evidence and the original scope
packet's raw observations are preserved, not rewritten.

## What the remediation does

1. Reconstructs every Q1 finding as an auditable matrix, including findings that
   the reviewer did not number (`findings-matrix.md`).
2. Independently reproduces the Q1 defects and keeps reviewer evidence distinct
   from R1 reproduction (`reproduction.md`).
3. Applies the planning corrections to the active CI-1 proposal
   (`corrections.md`, plus R1 amendment blocks in `ci1-scope-01/`).
4. Reopens ADR-CI1-01 without presuming separate facts are superior
   (`storage-alternatives.md`).
5. Separates the two existing C# defects that need their own production
   remediation milestone from the CI-1A planning work (`prerequisite-defects.md`).
6. Reconciles the acceptance matrix and its oracle requirements
   (`acceptance-reconciliation.md`).
7. Hands a bounded, reproducible packet to an independent Q2
   (`reviewer-handoff.md`) and records the final disposition (`final-handoff.md`).

## Scope that is deliberately preserved

The CI-1A objective is unchanged:

> Given an exact C# method identity, return distinct supported invocation sites,
> their locations, owners, semantic evidence, direct caller grouping, publication
> and honest coverage.

CI-1A still does **not** include indirect impact, all references, runtime dispatch
resolution, full refactoring safety, complete MSBuild/NuGet evaluation or automatic
source editing.

## Qualifications that remain open

- **F01 defect is real and unresolved.** The existing extractor can attach a
  precise, non-error, uniquely-keyed call to a *wrong* target when a local
  declaration shares a documentation ID with a platform/external method. Fixed
  only by a strict definition/assembly identity gate in the new operation and/or
  a separately authorized provider fix (`prerequisite-defects.md`).
- **F02 defect is real and unresolved.** A documented `sealed partial` type throws
  `InvalidCastException` before relationship extraction. Requires a separately
  authorized two-layer fix or an explicitly narrowed qualification.
- **ADR-CI1-01 is undecided.** Separate facts (C) and versioned occurrence edges
  (B2) remain qualified alternatives. No winner is claimed.
- **No candidate query exists.** No speedup, token or total-cost saving is
  established.

## Document index

| Document | Purpose |
| --- | --- |
| [findings-matrix.md](findings-matrix.md) | Finding-by-finding remediation matrix |
| [reproduction.md](reproduction.md) | Exact, independent reproductions and commands |
| [corrections.md](corrections.md) | Corrections applied and their rationale |
| [storage-alternatives.md](storage-alternatives.md) | Reopened ADR-CI1-01: C vs B2 |
| [prerequisite-defects.md](prerequisite-defects.md) | Separate production-remediation proposals |
| [acceptance-reconciliation.md](acceptance-reconciliation.md) | C01–C20 revisions and oracle requirements |
| [reviewer-handoff.md](reviewer-handoff.md) | What independent Q2 must challenge |
| [final-handoff.md](final-handoff.md) | Final disposition, Git, decisions and gates |
