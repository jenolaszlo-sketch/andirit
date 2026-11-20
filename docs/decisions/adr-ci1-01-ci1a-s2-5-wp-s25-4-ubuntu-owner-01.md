# ADR-CI1-01-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-OWNER-01 — G4 owner acceptance (M11.4 / M14.1 / M14.2 corrected-revision Ubuntu qualification)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-OWNER-01` (G4).
**Project:** Andirit. **Recorded:** 2026-11-21. **Canonical branch:** `main`.
**Owner decisions:**
**M11.4 — ACCEPTED WITH QUALIFICATIONS**;
**M14.1 — ACCEPTED WITH QUALIFICATIONS**;
**M14.2 — ACCEPTED WITH QUALIFICATIONS** —
bounded to the demonstrated corrected-revision execution (`eafd632`; genuine hosted Ubuntu run
44746950426; PR #7) and the G3 qualification findings.

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> Acceptance is bounded to the demonstrated corrected-revision execution and the G3 findings,
> and supersedes the scope-stale 52-test SQ-05 acceptance **for the current scope** (the
> LatticeDb suite is now 75 tests). The 52-test SQ-05 acceptance remains preserved as
> historical evidence for its own scope. PR #7 remains **open and unmerged**. This decision
> grants no WP-S25-4 closure, no Stage-2 acceptance, and no Stage-6 authorization.

This is an additive owner decision. It changes no production source, test, CI configuration,
fixture, dependency, reviewer evidence, oracle or corpus, prior decision, historical review
artifact, or existing planning contract definition.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `c38eba1e79bd7485d4b3239d0f70e20fe734c291` / `34a891794fb3e4778a99914d0ec798f9a3b00dd8` (as expected) |
| Execution revision / tree | `eafd632b...` / `823e3c65…`, strict ancestor of HEAD (verified via `merge-base --is-ancestor` on the published branch ref) |
| Branch / worktrees | `main`; exactly one; clean tracked/index; no stashes |
| Remote | `origin/main` `48370711…` unchanged (strict ancestor of HEAD); `qualification/wp-s25-4-sq05-linux-r2` at `eafd632`; PR #7 **OPEN, `mergeCommit` null** — read-only `ls-remote`/API verification, no remote contact beyond reads |
| Environment | Windows `10.0.26200` x64; .NET SDK `10.0.401` |

**Evidence manifests re-verified before recording**: G2 package 20/20 byte-exact at its seal
(`29bdc5c2…`); G3 package 9/9 at HEAD; preparation package 7/7; the G3 receipts record
overall PASS with zero failures. Protected scope: `git diff eafd632 HEAD -- src tests .github`
**empty** — production, tests, and workflow content unchanged from the pinned revision.

## 2. Independence-amendment confirmation (commission §1)

The applicable amendment pattern permits this qualification method; the review is valid for
acceptance:

- **SQ-05 G3 amendment** (recorded in `ANDIRIT-CI1A-S2.5-S25-SQ05-Q1.md` and carried by
  `adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md` §9): "hosted-run independence is **receipt-based**".
  The G3 review is receipt-based: every binding value was re-derived from primary sources
  (Git objects, sealed blobs, read-only GitHub records) by a **freshly written reviewer verifier**
  (`verify-g2-evidence.py`, 33 checks, all PASS) that consumed no producer summary as input.
- **WP-S25-3 P2-QUAL amendment** (`CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/Q1-P2A.json`, accepted by
  the owner in `adr-ci1-01-ci1a-s2-5-wp-s25-3-closure-owner-01.md`): a same-assistant-session
  reviewer is an accepted pattern **when the limitation is recorded and independence is derived
  from fresh harness/fixtures and cross-source derivation**.
- The current G3 limitation — G2 execution and G3 review occurred in the same operator session
  in sequence — is therefore **recorded as a qualification, not a blocker**, exactly as the
  precedent requires. **No additional independent review is required** for this commission;
  the same-session limitation is restated in §4.

## 3. Accepted technical basis (from the independent G3 qualification)

| Fact | Verified value |
| --- | --- |
| Five identities | pinned commit `eafd632` = PR #7 head = run `head_sha`; executed synthetic-merge tree `823e3c65…` = pinned tree; checkout evidence in the raw log |
| Environment | genuine hosted **Ubuntu 26.04.1 LTS x64** (image `ubuntu-26.04`/`20261111.196.1`), SDK 10.0.404, runtime 10.0.15 |
| Native dependency | `LatticeDbSharp 0.2.0` linux-x64 native resolved and exercised (all 75 store cases pass; production code instrumented) |
| LatticeDb suite | **75 passed / 0 failed / 0 skipped**, Release, **no filter** — the entire current expanded suite |
| Coverage | branches **368/466 = 78.96%**, lines **900/969 = 92.87%** (independently re-derived from the sealed Cobertura artifacts; all four coverage gates above thresholds on Ubuntu) |
| Supplementary / packaging | core 345/0/0, CSharp 372/0/0, TreeSitter 157/0/0; `pack` success; full run conclusion **success** |
| G1 compliance | all six stop conditions verified; no prohibited remote operation |

## 4. Binding qualifications (carried, restated)

1. **Environment scope**: the qualification is restricted to the demonstrated hosted environment
   (Ubuntu 26.04.1 LTS x64, SDK 10.0.404, hosted runner); no claim about other Ubuntu
   generations, distributions, or architectures.
2. **Behavior scope**: only the behavior exercised by the 75-case suite is qualified.
3. **Durability scope**: managed fault injection only — no crash/power-loss durability (F-02).
4. **Platform internals**: locking/WAL/mmap mechanics are not directly instrumented; platform
   claims are suite-observed parity (identical counters 75/0/0, 368/466, 900/969 on Linux and
   Windows).
5. **Independence**: hosted-run independence is receipt-based; the G2/G3 same-session sequencing
   limitation is recorded (§2).
6. Supplementary suites (core/CSharp/TreeSitter/pack) are recorded as **supplementary** evidence
   only, distinct from the M11.4/M14.1 core claim.

## 5. Scope supersession (for the current scope)

**The 52-test SQ-05 acceptance (`adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md`) is superseded FOR
CURRENT SCOPE** by this corrected-revision 75-test qualification, because the LatticeDb suite
changed after the F-G2-01 remediation. The historical acceptance and its evidence (G2/G3
packages, PR #6 records) are **preserved unchanged** as evidence for their own scope and
generation; nothing historical is rewritten. The current-scope Linux obligation rests on
`eafd632` + run 44746950426 + the G3 qualification.

## 6. Material findings — summary and effects

Dispositioned in detail in the additive record
[`adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-findings-disposition-01.md`](adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-findings-disposition-01.md).
Summary: **G3-F01** (PR #6 merged after the SQ-05 G3 assessment recorded it open) — a governance
divergence with no evidence loss (content is an ancestor of remote `main`; packages preserved);
recorded for reconciliation, no retroactive authorization. **G3-F02** (the SQ-05 G2 package
committed 14 of 18 manifest entries) — a historical packaging deficiency; the four absent
coverage artifacts are not reconstructed and the current-run artifacts are not represented as
replacements; this commission's counters derive from the current run. **Neither finding blocks
M11.4/M14.1 acceptance**; neither requires remediation by this decision (G3-F01 goes to the
owner's remote-resource reconciliation record; G3-F02 is dispositioned historically with the
recommended additive errata).

## 7. Formal G4 disposition and remaining checklist

**`M11.4 ACCEPTED WITH QUALIFICATIONS`; `M14.1 ACCEPTED WITH QUALIFICATIONS`;
`M14.2 ACCEPTED WITH QUALIFICATIONS`** — accepted on the independent G3 qualification
(`SUPPORTED WITH QUALIFICATIONS`), bounded to §3/§4, with the §5 supersession and the §6
findings recorded.

**Remaining WP-S25-4 / Stage-2 closure checklist (not closed by this decision):**

- [ ] M5.4 — BLOCKED; proposal prepared in
      [`ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01.md`](../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01.md); separate bounded commission required.
- [ ] WP-S25-4 final integrated qualification closure — separate owner decision.
- [ ] Stage-2 final integration and Stage-2 owner acceptance — separate decisions; must restate
      the M11.4/M14.1/M14.2 qualifications, the F-G2-01 closure, the format-gate closure, and the
      demonstrated Linux environment scope.
- [ ] Stage-6 items (full D3, D4.5, full S-02, crash/power-loss) — unauthorized and unaffected.
- [ ] G3-F01 owner reconciliation record (PR #6 merge authorization) — recommended.
- [ ] G3-F02 historical errata note (SQ-05 G2 package completeness) — recommended.

## 8. Repository discipline

Only this decision document, the additive findings-disposition record, the additive
[M5.4 resolution proposal](../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01.md),
and the additive ledger/plan updates are added, locally on `main`, under the single-worktree
policy. No production code, test, CI configuration, fixture, dependency, formatter, coverage
setting, historical review, evidence, oracle/corpus, or prior decision is modified. No fetch,
push, PR, branch change, worktree creation, or remote mutation is performed. PR #7 remains open
and unmerged. The commit/tree identities are reported in the handoff.