# ADR-CI1-01-CI1A-S2.5-WP-S25-4-UBUNTU-FINDINGS-DISPOSITION-01 — disposition of the G3 material findings

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-OWNER-01` §6.
**Recorded:** 2026-11-21. **Canonical branch:** `main`.
**Disposition:** **RECORDED — NO BLOCKING ISSUE; NO REMEDIATION REQUIRED BY THIS DECISION.**

This is an additive owner record. It changes no historical artifact and grants no retroactive
authorization. The findings are those of the independent G3 qualification
(`ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G3-QUAL-01`).

## G3-F01 — Historical PR #6 merge (recorded discrepancy)

**Facts.** PR #6 (`qualification/ci1a-s25-sq05-linux`, head `7457284…`) is **MERGED**:
`gh pr view 6` reports `state=MERGED`, `mergeCommit.oid = 7457284451dff8ddb0417f83ecc19097103316fc`
(a fast-forward merge commit = head SHA), `mergedAt 2026-11-10T23:05:29Z`. The SQ-05 G3
assessment (recorded 2026-11-10) read PR #6 as open and unmerged, and the WP-S25-2 owner
acceptance §12 recorded the PR/branch as the retained evidence anchor whose disposition
(merge/close/delete) required a **separate bounded remote-resource decision**.

**Determinations.**

1. **Effect on historical authorization**: the merge post-dates the recorded assessment and no
   decision in the repository authorizes it. This record **does not retroactively authorize**
   the merge; the divergence is recorded for the owner's remote-resource reconciliation (or an
   acknowledgment that a later authorized action merged it — no such record exists in the
   repository at writing time).
2. **Effect on ancestry**: none adverse. `7457284…` is a strict ancestor of remote `main`
   (`48370711…` contains it; verified locally), and remote `main` has not moved since the
   recorded value. The merge was a fast-forward (no force, no history rewriting): its parent is
   `487f17a3…` (the then-base, WP-S25-1 owner acceptance). Both historical and current evidence
   chains remain intact.
3. **Effect on prior qualification claims**: none. SQ-05's satisfaction was bounded to the
   52-test suite at `7457284…` with committed sealed packages (preserved: G2 14/18 committed —
   see G3-F02; G3 6/6 verified); the merge neither added nor removed repository evidence. The
   PR #7 evidence chain (current scope) is independent of PR #6's state; PR #7 remains open and
   unmerged.
4. **Actions**: none on the PR (not merged-again, not modified, not closed, not deleted). The
   branch remains at `7457284…`. Recommended: an owner remote-resource reconciliation record
   acknowledging the merge event (informational), at the owner's convenience; not a blocker.

## G3-F02 — Incomplete historical G2 evidence (recorded deficiency)

**Facts.** The SQ-05 G2 evidence package (`CI1A-S2.5-WP-S25-2-G2-EXEC-evidence`, sealed at
`df57a2a`) has a manifest listing **18 entries** but the commit carries **14**: the four
`coverage-results-ubuntu-latest/*.cobertura.xml` artifacts were never committed (verified absent
via `git cat-file`; the package has exactly one commit and has not changed since the seal). The
WP-S25-2 owner record "G2 manifest 18/0" was evidently verified against the producer working
tree, not the committed package.

**Determinations.**

1. **Historical package is preserved unchanged** — nothing is reconstructed, and the four
   absent artifacts are **not** re-imported from the current run: the current run's artifacts are
   different-bytes, different-revision evidence and must not be represented as the historical
   ones. The deficient manifest remains as sealed, so the deficiency stays visible.
2. **Historical vs current evidence distinction (explicit)**: the SQ-05 coverage counters
   (296/450 branches, 838/963 lines) survive in the independently verified SQ-05 **G3** package
   (`CI1A-S2.5-S25-SQ05-Q1-evidence`, 6/6 verified) and the owner decision; the current-scope
   counters (368/466, 900/969) derive exclusively from the corrected-revision run's artifacts,
   independently re-derived in the G3 package. No historical/counterfactual claim is transferred.
3. **Remediation need**: none for this commission. The deficiency is historical-packaging, not
   evidence-loss for any live obligation (SQ-05's satisfaction was already re-based to the
   current scope by the corrected-revision acceptance). Recommended: an additive historical
   errata note (one paragraph appended to the SQ-05 G2 package's provenance or the register) so
   downstream citations do not assume the four artifacts exist in Git; recorded as a checklist
   item, not a blocker.

## Overall determination

**Neither material finding requires additional remediation, and neither affects the M11.4/M14.1/
M14.2 acceptance**, which rests on the corrected-revision G2/G3 packages verified intact at
their seals. No unresolved blocking issue is escalated. Both findings are carried into the Stage-2
acceptance restatement.