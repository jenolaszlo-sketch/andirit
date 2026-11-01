# ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01-OWNER-01: Qualified owner acceptance

**Recorded:** 2026-11-02 (Asia/Manila). **Mode:** documentation only.
**Disposition: `ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01 ACCEPTED WITH QUALIFICATIONS`.**
The owner commissioned this decision record; the executor records acceptance,
not a new independent review or human signature. This accepts the corrected
Candidate C foundation within the demonstrated scope. Complete CI-1A acceptance
and Stage 2 implementation remain unauthorized.

## 1. Verified source and authority

Canonical repository: `D:\projects\Andirit`, origin
`https://github.com/jenolaszlo-sketch/andirit.git`, branch `main`.
OWNER-01 began at reviewed HEAD `aaea1ca7f32a666bc9c6480e91c75c7dd14779d4`,
tree `4012bf14e680a742da7a3785831fb578d9f92eb4`.
Preflight found a clean working tree, one canonical worktree and no stashes.

| Remediation role | Commit | Tree |
| --- | --- | --- |
| Q1B starting boundary | `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` | `320e965f219d0b615da29665d7fa3e85f8bbfc15` |
| Contract correction | `c13137d54e0ee48c0f7c7383b03bf43bed74a936` | `380104122e96e5a164a2a8375ffdd7f37c513e1f` |
| Production remediation | `fc515ffbc20adc286fb57339130fa5ccbf466bb8` | `8c2d7d08be0f7294332d48ec8cb3b2c98476a9fa` |
| Producer evidence | `e7b4258a3e4c00d25ad609c13ce90eb2d6f65298` | `04af74d1134d98567a771674c46db77787d8ed47` |
| Final independently reviewed source | `aaea1ca7f32a666bc9c6480e91c75c7dd14779d4` | `4012bf14e680a742da7a3785831fb578d9f92eb4` |

All trees and consecutive parent identities were reverified. Four linear
descendants connect the starting boundary to the final reviewed source. The
last remediation commit changed documentation only; `src/` and `tests/` are
identical at the evidence and final reviewed commits.

Controlling authority remains the [Candidate C owner decision](adr-ci1-01-owner-decision.md)
§§D/F/G, its [Q1 acceptance](adr-ci1-01-owner-01-q1-acceptance.md), the
[production integration specification](../planning/adr-ci1-01-ci1a-impl-01/specification.md),
the Stage 1 decisions below, and R2/Q3 incorporated by owner §F. The
[REM-01 contract reconciliation](../planning/adr-ci1-01-ci1a-s2-rem-01/contract-reconciliation.md),
[identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md),
[gate reconciliation](../planning/adr-ci1-01-ci1a-s2-rem-01/stage2-gate-reconciliation.md)
and corrected [Stage 2 boundary](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md)
govern subsequent work. Accepted architectural records are preserved unchanged.

## 2. Independent review binding and package integrity

Package: `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-rem-01-q1`.
Controlling disposition in `REPORT.md` §6 and `FINAL-HANDOFF.md` §6:
**`CI1A-S2-BOUNDARY-REM-01-Q1 SUPPORTED WITH QUALIFICATIONS`**.
Its assessment **`S2.1 READY WITH PRECONDITIONS`** is a recommendation.

OWNER-01 read all 18 root review documents. Actual manifest location is
`evidence/SHA256SUMS.txt`, not the package-root location suggested by its prose.
Scheme `sha256-file-v1`; **33/33 rows match**, zero missing/mismatched rows;
the manifest excludes itself. Its measured SHA-256 is
`cbce8ffe8ebf9cb8160f6d73830c8f11c51e6fe3d98446af986d371487cf9b0a`.
Rows cover all 18 documents, eleven evidence files and four probe/writer/extract
files. This verifies the enumerated package artifacts, not an invented
aggregate hash of unlisted build/reproduction outputs.

The original Q1B package at `...\Solo\adr-ci1-01-ci1a-s2-boundary-q1b`
also verifies **29/29** rows; its `SHA256SUMS.txt` SHA-256 is
`0a94b236241633db6e12d39af4e52e4d3d255dc379bdceb6800962af323dc6af`.
Its original F01–F16 register is `NEW-FINDINGS.md`; `FINDINGS.md` is absent.
This naming correction substitutes no handoff for the actual register.

Independent results from `REGRESSION-RESULTS.md`, `ADVERSARIAL-PROBES.md`
and hashed logs (not tests rerun by OWNER-01):

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Andirit.Tests | 325 | 0 | 0 |
| Andirit.LatticeDb.Tests | 43 | 0 | 0 |
| Andirit.CSharp.Tests | 69 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| Total | 594 | 0 | 0 |

The reviewer reports **24/24 reviewer-owned probes** (9 identity, 15 native),
build zero warnings/errors, Windows Debug, SDK 10.0.401 and LatticeDbSharp 0.2.0.
Probe P11's commit-throw classification is a code audit, not an injected
commit-throw experiment. Probe P13b refused an authentic database written by
baseline `4c6d26a` code. This does not certify every historical format or Linux.

## 3. Accepted capabilities and exact limits

| Accepted claim | Demonstrated evidence and boundary |
| --- | --- |
| Canonical identity | R2 `usage4` and `asmctx3` tuple/encoding implemented; frozen vectors independently reproduced, discriminatory inputs and constructor/validator recomputation verified (`IDENTITY-REVIEW.md`, P01–P04b). Canonical string shape alone does not prove a defining compilation. |
| External-target refusal | Corrected contract requires no fact/no canonical target for unsupported external evidence; unavailable/malformed identities refused structurally (P04). Full live Roslyn defining-compilation minting authority remains S2.2. |
| Resolution, coverage, availability | Separate dimensions persist and survive reopen; incomplete/no-analysis/complete-empty are distinct (P05/P07/P07b/P16). `Produced` and counts are structural producer claims until source/universe authority is implemented. |
| Durable complete-empty | Successful zero-fact analysis persists one unit, zero pages and complete meta; replacement retires pages; reopen retains availability (P05/P08). Completeness still requires valid scope authority. |
| Unknown native commit outcome | Ordinary ack-loss quarantines graph/occurrence reads and mutations; reopen reconciles durable authority, then identical retry can be idempotent (P10/P12/P17). Known-failure occurrence replay has NEW-01; N6 checks occurrences only after reopen. |
| Corrected gates | Contract prerequisites, implementation deliverables and independent acceptance evidence are distinct (`STAGE2-GATE-REVIEW.md`). Corrected prerequisites grant no implementation authority. |
| Compatibility | Authentic pre-REM noncanonical data refused store-wide, including graph-only access to that store (P13b); rebuild by re-ingestion into a fresh store. Missing-coverage records conservatively restore Partial (P14). No migration. |
| Preservation | D3 preserved in synthetic tested scope; D5 remains supported; applicable D4 supported with NEW-01 qualification on invariant 4 and native schedule evidence still due for invariant 10. Invariant 5 remains Stage 6. |

All F01–F16 and NEW-01–NEW-04 dispositions and gates are retained in the
[consolidated obligation register](adr-ci1-01-ci1a-s2-obligation-register.md).
NEW-01 requires source-bound remediation **before S2.3 and before relying on
failure recovery in production**, including any earlier use. NEW-04 must be
corrected before a source-completeness claim depends on the affected merge,
and independently qualified before S2.2 acceptance. Neither is marked fixed.

## 4. Explicit nonclaims

This acceptance establishes none of: completed S-01 provenance; complete Roslyn
semantic occurrence extraction; trusted compilation-universe authority;
universal semantic completeness; public Find Usages readiness; crash or
power-loss recovery; general multi-process correctness; MVCC/snapshot isolation;
full D3/D4 qualification; Linux native qualification; complete historical
fixture certification; correct occurrence-projection recovery in every known
failure path; independent acceptance of future Stage 2 code. Synthetic identities
in native probes are structurally valid, not Roslyn-verified.

## 5. Stage 1 relationship and additive qualification

1. Stage 1 established the Candidate C native vertical slice at
   `76ebaae84d741a38f92d2422c63742197c36bc82`, tree
   `361293dbf94c0836fe9666bb07d596ea7ae4a7f4`.
2. Stage 1 Q1 and the [historical owner acceptance](adr-ci1-01-ci1a-s1-owner-acceptance.md)
   accepted that slice within the tested boundaries; the
   [Stage 1 reconciliation](adr-ci1-01-ci1a-s1-obligation-reconciliation.md)
   retained its obligations.
3. Later Stage 2 Q1B found six material blocking contract/foundation defects.
4. REM-01 corrected them; REM-01-Q1 independently verified closure, four with
   qualifications.
5. This decision accepts the corrected foundation, preserving those limits.

This is an additive qualification/supersession reference for subsequent use of
the foundation's identity, coverage and gate claims. It does not rewrite Stage 1
history to imply those defects were known or corrected then. Corrected REM-01
identity and coverage contracts control later implementation. The old acceptance's
full package-integrity claim is qualified by the evidence erratum; its storage
acceptance is not revoked.

## 6. Historical evidence remains qualified

OWNER-01 reproduced the Q1 evidence: Stage 1 reviewer rows **15/16**, producer
rows **6/6**, hence **21/22** relevant rows. `FINAL-HANDOFF.md` expected
`1543cd4a80933506a64ddeaf578c08922ff640d2a6d365fd567d0c9e30007f48`,
observed `283de40be767002d06ac1f05f86cea334b2898414045316aca7caf9ec10bf457`.
`REPORT.md` is absent; the claimed historical aggregate tree is unverified.
The manifest records only the expected value. Technical review files and six
producer evidence documents remain independently reproducible; they support the
bounded behavioral conclusions despite the historical packaging defects.

The Codex declared SHA/size describe pinned Git blob
`8f921e383a9320726696694546418cdad9f9cbf2`, not materialized working bytes.
Exact measurements and the additive NEW-03 correction are in
[the erratum, §4](../planning/adr-ci1-01-ci1a-s2-rem-01/evidence-integrity-erratum.md#4-owner-01-additive-correction-2026-11-02).
No frozen reviewer package, original manifest or missing report is rewritten.

## 7. Authorization boundary

Owner acceptance is not independent implementation qualification. The
[S2.1 boundary](adr-ci1-01-ci1a-s2-1-authorization-boundary.md) defines eligibility
for a separate commission. No S2.0 preparation, S2.1 implementation, S2.2
extraction, native correction or later stage begins under OWNER-01.
