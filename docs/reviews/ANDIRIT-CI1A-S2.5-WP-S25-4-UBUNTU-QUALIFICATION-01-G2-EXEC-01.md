# ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G2-EXEC-01

**Date**: 2026-11-20
**Gate**: G2 — authorized native Linux execution and evidence capture (executed under the owner G1/G2 commission for UBUNTU-QUALIFICATION-01)
**Outcome**: **`LINUX EXECUTION COMPLETE WITH QUALIFICATIONS`** — genuine hosted Ubuntu execution of the entire current LatticeDb suite succeeded at the pinned revision; **G2 success is not G3 qualification**.
**Status**: COMPLETE — evidence sealed; ready for independent G3 assessment.

---

## 1. Authorization and scope compliance

Executed exactly the authorized operation: one explicit-refspec push of `eafd632` to the new
remote branch `qualification/wp-s25-4-sq05-linux-r2`, one PR (#7) against `main` (**never
merged**), the existing unmodified `ci.yml` run on the hosted runners, and evidence retrieval.
**Not performed:** no `main` push, no force-push, no modification of the SQ-05 branch
(`qualification/ci1a-s25-sq05-linux` remains at `7457284…`), no workflow edit, no additional or
repeated runs, no PR merge, no deletion, no production/test/CI change of any kind.

## 2. Publication and execution identities

| Identity | Value |
| --- | --- |
| Pinned branch commit (1) | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` |
| PR head SHA (2) | `eafd632b...` (PR #7, head `qualification/wp-s25-4-sq05-linux-r2`, base `main`, state OPEN, `mergeCommit` null) |
| Synthetic merge SHA (3) | `e753e991eb255446cbd1ce2be72ec49fb3f4c733` (parents `48370711…` base + `eafd632…` head) |
| Run `head_sha` (4) | `eafd632b...` (run 44746950426, event `pull_request`) |
| Executed source tree (5) | `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` = **the pinned tree** (retrieved merge-ref tree via read-only `git fetch origin refs/pull/7/merge`; the checkout log line 93/134 independently fetched and checked out the same merge commit) |
| Workflow identity | `ci.yml` blob `be48426f9be77eacf5dc3de925566126eedad03b` — identical at the pinned commit, the merge tree, and the base |
| Remote main | `48370711a6247b972dfc83628618924e864be374` before and after (no advancement) |

All six stop conditions were verified **before** publication and re-confirmed from the run
records; none triggered. Full record: `publication-record.json`.

## 3. Workflow and job dispositions

Run **44746950426** — workflow `ci` — conclusion **success** (all four jobs):

| Job | Conclusion | Duration |
| --- | --- | --- |
| **test (ubuntu-latest)** | **success** | 13:44:25Z → 13:46:50Z |
| test (windows-latest) | success | 13:44:26Z → 13:48:32Z |
| test (macos-latest) | success | 13:44:27Z → 13:46:52Z |
| pack | success | 13:44:25Z → 13:45:14Z |

The ubuntu job executed, in order: restore → `dotnet format --verify-no-changes --no-restore`
(exit 0) → coverage-gated test steps for core (345/0/0), CSharp (372/0/0), TreeSitter (157/0/0),
LatticeDb (75/0/0) → artifact upload. Step-by-step evidence is in the raw log
(`job-ubuntu-latest.log`).

## 4. Binding results for M11.4 / M14.1

**Ubuntu 26.04.1 LTS x64 (hosted, image `ubuntu-26.04`, SDK 10.0.404, runtime 10.0.15):**

| Metric | Result | Binding requirement |
| --- | --- | --- |
| `Andirit.LatticeDb.Tests` | **75 passed / 0 failed / 0 skipped**, Release, no filter | M14.1 — entire current expanded suite passes on Linux |
| LatticeDb branch coverage | **368/466 = 78.96%** (threshold 70) | M11.4/M14.1 support; gate exit 0 |
| LatticeDb line coverage | **900/969 = 92.87%** (threshold 70) | same |
| Native `liblattice.so` | resolved and exercised (all 75 store cases pass; `Andirit.LatticeDb` instrumented; `LatticeDbSharp 0.2.0` at the executed tree) | M11.4 — cross-platform native tests pass |
| Supplementary suites | core 345/0/0, CSharp 372/0/0, TreeSitter 157/0/0 | G1 §6 procedure (supplementary, distinct from the M11.4/M14.1 core claim) |
| pack | success on hosted Ubuntu | G1 §6 procedure |

Reviewer-side independent derivation from the downloaded artifacts (G3 will re-derive):
LatticeDb condition sums 736/928 → halved 368/464; root 368/466 — the same +2 coverlet
granularity offset as the Windows artifact; counters identical to the Windows reference
(75/0/0; 368/466; 900/969). This is the **first green full-matrix repository CI conclusion**
at the qualified implementation.

## 5. Qualifications carried into the evidence (not resolved here)

- **L6**: managed fault injection only — crash/power-loss durability (F-02) remains unproven.
- **L7**: internal locking/WAL/mmap mechanics not directly instrumented; platform claims are
  suite-observed and counter-identical, not instrumented.
- **Environment generation**: the demonstrated environment is Ubuntu **26.04.1** / SDK 10.0.404
  (`ubuntu-latest` rolled forward since SQ-05's 24.04.5 / 10.0.403); the qualification
  boundary should name this generation, and reproducibility on other generations is not claimed.
- **Independence**: this is producer-side execution evidence (the executor ran the authorized
  operation). G3 must be performed by a reviewer independent of the executor; **G2 success is
  not G3 qualification**.
- Windows and macOS jobs also came back green, but their evidence is recorded as run-level
  dispositions here (logs not downloaded); the commission's binding target is the Ubuntu job.

## 6. Evidence package and integrity

Sealed under this commission's `-G2-EXEC-01-evidence/` package with a self-excluding SHA-256
manifest (`manifest.json`, scheme per the QUAL-04/R3 convention): raw logs, downloaded
artifacts (all three OS artifact sets preserved for completeness), publication record,
machine-readable results, and L1–L9 receipts. Nothing was reconstructed; every value traces to
a preserved raw artifact or a run/job API record. Hashes are listed in the manifest.

## 7. Readiness and handoff

- **M11.4 / M14.1 evidence: READY for independent G3 assessment** (execution complete; package
  sealed; identities verified). Disposition of the obligations themselves remains G4.
- **Recommendation: commission `ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G3-QUAL-01`**
  (independent assessment): re-derive the five identities from primary sources (Git objects,
  PR/run API, merge-ref tree), re-derive the 75/0/0 counts and the 368/466 + 900/969 coverage
  from the sealed artifacts, verify the manifest, confirm historical evidence preservation
  (SQ-05 G2/G3 packages, PR #6), and classify findings. G4 owner acceptance remains separate
  after G3.
- WP-S25-4 closure, Stage-2 acceptance, Stage 6, and M5.4 remain unauthorized/unaffected.

## 8. Repository discipline

Only this report and the evidence package were added, locally on `main`, under the
single-worktree policy; the remote holds exactly the authorized branch and PR. No historical
review, evidence, oracle/corpus, or prior decision was modified. The commit/tree identities are
reported in the handoff.