# ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G3-QUAL-01

**Date**: 2026-11-21
**Gate**: G3 — independent qualification of the sealed G2 corrected-revision Ubuntu execution (run 44746950426, PR #7)
**Overall disposition**: **SUPPORTED WITH QUALIFICATIONS** — **M11.4: SUPPORTED WITH QUALIFICATIONS; M14.1: SUPPORTED WITH QUALIFICATIONS; M14.2: SUPPORTED WITH QUALIFICATION**. G3 support is **not** G4 owner acceptance.

---

## 1. Role, independence and method

The reviewer derived all acceptance criteria independently from the governing contracts and
re-derived every result from primary sources: Git objects (`git rev-parse`/`merge-base`/
`cat-file`/`ls-tree` on the sealed commit and the pinned revision), the sealed G2 package blobs
(raw log, both job logs, all downloaded coverage artifacts), and read-only GitHub records
re-queried in this session (`ls-remote`, PR/run/job API — no push, no re-run, no PR modification).
No producer summary was trusted as an input; the 33 receipt checks in
`g3-verification-receipts.json` (produced by `verify-g2-evidence.py`, all PASS) are the binding
record. Honest independence boundary: the G2 execution and this G3 review were performed by the
same operator session in sequence, so independence is **method-level** (primary-source
re-derivation), not session/organizational-level — recorded as qualification F4, matching the
SQ-05 G3 amendment ("hosted-run independence is receipt-based").

## 2. Preflight

| Object | Value |
| --- | --- |
| Sealed HEAD / tree | `29bdc5c2d2d2bdccf0834ab4540770a787eb42b3` / `4ba58e5f…` (as expected), branch `main`, clean, one worktree, zero stashes |
| Execution revision / tree | `eafd632b...` / `823e3c65…`, strict ancestor of HEAD (verified via `merge-base --is-ancestor`) |
| Remote main | `48370711a6247b972dfc83628618924e864be374` (unchanged; strict ancestor of the pinned revision) |
| Protected scope | `git diff eafd632 HEAD -- src tests .github` **empty** — production, tests and workflow content identical to the pinned revision |

## 3. G1 authorization and stop-condition compliance — PASS

All six stop conditions independently verified against primary remote state (A1–A8 in the
matrix): remote main unchanged and an ancestor; destination branch absent pre-push (it now
exists exactly at `eafd632`); workflow blob `be48426f…` identical at the pinned commit, the
merge tree and the base; push/PR completed; merge parents exactly base+pinned head; executed
merge tree `823e3c65…` equals the pinned tree. Prohibited operations confirmed not performed:
`main` untouched, SQ-05 branch `7457284…` untouched, no force, no merge, no workflow change, no
re-run, no deletion.

## 4. Five-identity commit/tree equivalence — PASS

| # | Identity | Verified value |
| --- | --- | --- |
| 1 | Pinned branch commit | `eafd632b...` |
| 2 | PR #7 head SHA | `eafd632b...` (state OPEN, `mergeCommit` null — **not merged**, re-queried) |
| 3 | Synthetic merge SHA | `e753e991eb255446cbd1ce2be72ec49fb3f4c733` |
| 4 | Run 44746950426 `head_sha` | `eafd632b...` (event `pull_request`, workflow `.github/workflows/ci.yml`) |
| 5 | Executed source tree | `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` = **pinned tree** (merge ref tree, read-only fetch) |

The raw checkout log independently corroborates: fetch of `+e753e991…:refs/remotes/pull/7/merge`
and `HEAD is now at e753e99`. Workflow identity holds at all three refs.

## 5. Genuine hosted Ubuntu execution and native dependency — PASS

Ubuntu **26.04.1 LTS x64**, hosted image `ubuntu-26.04`/`20261111.196.1`, runner 2.338.2 (Azure,
northcentralus) — genuine hosted Ubuntu (fixture L); no WSL2/Windows substitution. .NET SDK
10.0.404 / runtime 10.0.15 (workflow pin `10.0.x`). `LatticeDbSharp 0.2.0` is pinned at the
executed tree; `src/Andirit.LatticeDb` restored on the runner; all 75 suite cases — which open,
publish and reopen native LatticeDb stores — passed on linux-x64, and the run's coverage artifact
instruments `Andirit.LatticeDb` production code (368/466), independently re-read by this review.

## 6. Binding results — all re-derived from raw evidence

| Metric | Verified value (primary sources) |
| --- | --- |
| LatticeDb suite | **75 passed / 0 failed / 0 skipped**, Release, **no test filter** (raw-log summary; whole-log failed-total scan; no `--filter` token) |
| LatticeDb branches | **368/466 = 78.96%** (root attributes; independent condition-sum derivation 736/928 → 368/464; +2 coverlet granularity offset) |
| LatticeDb lines | **900/969 = 92.87%** (root attributes; derivation agrees) |
| Thresholds | 70 (line+branch) met; gate exit 0; the ubuntu job and run concluded success |
| Supplementary (distinct) | core 345/0/0 (82.42%/70.74% vs 55), CSharp 372/0/0 (95.11%/87.85% vs 75), TreeSitter 157/0/0 (89.89%/75.95% vs 60) |
| Formatting | `dotnet format --verify-no-changes --no-restore` executed verbatim, exit 0 within the successful job |
| Packaging | `pack` job success on hosted Ubuntu (log preserved) |
| CI matrix | run conclusion **success** — ubuntu/windows/macos test jobs and pack all green (first green full-matrix conclusion at the qualified implementation) |

## 7. Evidence integrity and preservation

- **G2 package 20/20** rehashed byte-exact from Git blobs (complete coverage, manifest
  self-excluding correctly).
- **L1–L9 receipts validated**: each L-row fact re-derived from raw evidence (matrix E2);
  L6/L7's standing qualifications are correctly carried, not widened.
- **SQ-05 G3 package preserved**: `CI1A-S2.5-S25-SQ05-Q1-evidence` 6/6 verify; unchanged since
  `eb1abef`.
- **SQ-05 G2 package — MATERIAL finding G3-F02**: unchanged since `df57a2a`, but the commit
  carries **14 of the manifest's 18 entries** (four Ubuntu coverage artifacts never committed);
  historical record preserved, not reconstructed; no impact on this commission's counters.
- **PR #6 — MATERIAL finding G3-F01**: now **MERGED** (fast-forward, 2026-11-10T23:05:29Z),
  after the SQ-05 G3 assessment read it open and contrary to the WP-S25-2 §12 cleanup
  determination; content is an ancestor of remote main and the SQ-05 packages are preserved.
  Recorded for owner reconciliation; does not block M11.4/M14.1.
- **PR #7 remains unmerged**; production/tests/CI content unchanged from the pinned revision.

## 8. Qualifications (commission §4)

1. Managed-fault-only durability (F-02) — outside M11.4/M14.1 scope; carried.
2. Locking/WAL/mmap internals uninstrumented — platform claims are suite-observed counter parity;
   qualifies M14.2 documentation depth.
3. Demonstrated environment is Ubuntu 26.04.1/SDK 10.0.404; the binding text requires genuine
   Ubuntu x64 without pinning a point release, so this is recorded as scope, not failure; no
   claim about other generations.
4. Hosted-run independence is receipt-based (primary-source re-derivation; method-level reviewer
   independence).

No remaining limitation prevents satisfaction of M11.4 or M14.1 as written; nothing is claimed
beyond the demonstrated evidence.

## 9. Dispositions

| Row | Disposition |
| --- | --- |
| **M11.4** | **SUPPORTED WITH QUALIFICATIONS** — expanded native suite coverage on genuine hosted Ubuntu x64 at the corrected revision with linked source/run/log identities |
| **M14.1** | **SUPPORTED WITH QUALIFICATIONS** — entire current 75-case suite passed on Linux at the pinned revision, five-identity checkout/tree evidenced, supplementary runs recorded distinctly |
| **M14.2** | **SUPPORTED WITH QUALIFICATION** — suite-level platform-behavior parity documented; internal mechanics uninstrumented (F2) |
| **Overall (G3)** | **SUPPORTED WITH QUALIFICATIONS** — findings G3-F01/G3-F02 (MATERIAL, historical) and G3-F03…F05 (INFORMATIONAL) restated for G4; G3 support is not acceptance |

## 10. G4 recommendation

**Recommend `ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-OWNER-01` (G4)** accepting M11.4
and M14.1 **with qualifications**, limited to: the demonstrated hosted environment (Ubuntu 26.04.1
LTS x64, SDK 10.0.404); the 75-case suite's demonstrated behavior; the unchanged F-02/managed-fault
and platform-instrumentation limits; and receipt-based hosted independence. G4 must restate
qualifications F1–F4 and should disposition findings G3-F01/G3-F02 in an additive record. WP-S25-4
closure, Stage-2 acceptance, Stage 6 and M5.4 remain separate decisions and are not advanced by
this qualification.

## 11. Repository discipline

Only this report and the `-G3-QUAL-01-evidence/` package are added, locally on `main`
(`29bdc5c2…` parent), under the single-worktree policy. No production/test/workflow/CI change, no
fetch/push/PR/remote mutation except the read-only remote verification described above, and no
historical artifact is modified. Seal identity is reported in the handoff, not embedded here.