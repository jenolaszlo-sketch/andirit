# CI1A S2.3 — Native publication entry package

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. **Mode:** documentation and
assessment only. **Recorded:** 2026-11-06. **Canonical branch:** `main`.

**Disposition:** `ENTRY ACCEPTED WITH EXPLICIT QUALIFICATIONS`.
**First authorized implementation package:** `NEW-01` targeted recovery remediation.
**Native publication / durability implementation:** **NOT authorized** until the
`NEW-01` gate has passed its independent qualification and owner decision.

This package reconstructs the accepted S2.3 scope, the `NEW-01` recovery
prerequisite, the Option A native-reopen stale-refusal gate, the Stage 1 native
capability reuse boundary, the native lifecycle test matrix, the S-01
canonicalization disposition, and a dependency-ordered work-package and
independent-qualification plan. It changes no production code, test, schema,
frozen expectation, or accepted decision.

## Documents

| # | Document | Purpose |
| --- | --- | --- |
| 1 | [scope-and-authority-inventory.md](scope-and-authority-inventory.md) | Authoritative S2.3 requirements and boundaries |
| 2 | [new-01-recovery-remediation.md](new-01-recovery-remediation.md) | `NEW-01` targeted-remediation entry contract |
| 3 | [native-reopen-test-8b-gate.md](native-reopen-test-8b-gate.md) | Option A §5.2 test 8b native reopen stale-refusal gate |
| 4 | [stage1-native-reuse-assessment.md](stage1-native-reuse-assessment.md) | Reuse vs integration assessment of existing native foundation |
| 5 | [native-lifecycle-test-matrix.md](native-lifecycle-test-matrix.md) | N1–N8-equivalent real-fact lifecycle test matrix |
| 6 | [s01-canonicalization-disposition.md](s01-canonicalization-disposition.md) | Outstanding digest-canonicalization obligation disposition |
| 7 | [work-package-plan.md](work-package-plan.md) | Dependency-ordered S2.3 work packages |
| 8 | [independent-qualification-plan.md](independent-qualification-plan.md) | Independent qualification and evidence plan |

The formal owner decision is
[`docs/decisions/adr-ci1-01-ci1a-s2-3-entry-owner-01.md`](../../decisions/adr-ci1-01-ci1a-s2-3-entry-owner-01.md).

## Controlling authority (precedence order)

1. Accepted Candidate C owner decision (`adr-ci1-01-owner-decision.md`) §§D/F —
   **D3 ACCEPTED**, **D4 ACCEPTED WITH EXPLICIT INVARIANTS**, **D5 ACCEPTED — NO
   MIGRATION**.
2. Frozen R2 `PROVENANCE-CONTRACT.md` §§2–5 and Q3 `TARGET-IDENTITY-REVIEW.md`
   (identity; `asmctx3`/`usage4`/`compctx1`/`compctx2`).
3. Option A decision (`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`) §5,
   especially §5.1(5) durability and §5.2 test 8.
4. S2.2 `G-ACCEPT` decision (`adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md`) — the
   accepted S2.2 capabilities and the recorded S2.3 entry obligations.
5. S2.1 acceptance (`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`) and the S2.1
   authorization boundary (`adr-ci1-01-ci1a-s2-1-authorization-boundary.md`) §4.
6. Consolidated obligation register (`adr-ci1-01-ci1a-s2-obligation-register.md`)
   §§3–8 — `NEW-01`, `S-01`, `F05`, `F12`, `SQ-02`, `SQ-03`.
7. REM-01 contract reconciliation and the Stage 1 owner acceptance (`S1A`).
8. Stage 2 boundary §8 / handoff / acceptance matrix (corrected by REM-01).
9. S2.2 WP5–WP8 owner decisions.

**Precedence rule applied.** Older stage documents describe publication as part of
S2.2. The later accepted boundary (S2.1 authorization boundary; S2.2 entry
acceptance §3.5; S2.2 `G-ACCEPT` §17) assigns **native source-owned publication to
S2.3**. This decision follows the later accepted boundary. Historical planning
drafts are not current authorization.
