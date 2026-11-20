# Requirement-to-evidence disposition matrix — UBUNTU-QUALIFICATION-01-G3-QUAL-01

Reviewer-derived criteria from the commissioning instruction and the governing contracts
(`wp-s25-4-qualification-matrix.md` §11/§14 at HEAD; the SQ-05 acceptance chain). Every verdict
below rests on independently re-derived receipts (`g3-verification-receipts.json`, 33 checks,
all PASS) from primary sources: Git objects, the sealed G2 package blobs, and read-only GitHub
records re-queried in this session. Producer summaries were not trusted as inputs.

Dispositions: PASS · PASS-QUAL (pass with recorded qualification) · QUAL (recorded finding,
not failing a binding row).

## A. G1 authorization and stop-condition compliance

| # | Requirement | Result | Independent evidence |
| --- | --- | --- | --- |
| A1 | G1 publication/execution authorization existed and bounded the operation | PASS | The owner G1/G2 commission (2026-11-20) authorized exactly: push `eafd632` to `qualification/wp-s25-4-sq05-linux-r2`, one PR, the unmodified workflow, evidence retrieval. Remote state today matches exactly that operation (§D) |
| A2 | Stop 1 — remote `main` unchanged / still a strict ancestor of the pinned revision | PASS | `ls-remote origin refs/heads/main` = `48370711…` before and after; `git merge-base --is-ancestor 48370711 eafd632` → true; the synthetic merge's parents are exactly those two commits |
| A3 | Stop 2 — destination branch did not pre-exist | PASS | `refs/heads/qualification/wp-s25-4-sq05-linux-r2` absent pre-push (reviewer re-queried: present now, exactly at `eafd632`; the SQ-05 branch remains at `7457284…`, untouched) |
| A4 | Stop 3 — workflow identity | PASS | `ci.yml` blob `be48426f…` identical at the pinned commit, the merge tree, and the base; the run's `path` = `.github/workflows/ci.yml`; no `workflow_dispatch`, no re-run, no edit |
| A5 | Stop 4 — credentials | PASS | Push and PR creation completed with the authorized identity (run/PR records exist) |
| A6 | Stop 5 — ancestry | PASS | Merge parents = base + pinned head only; no surprise refs |
| A7 | Stop 6 — executed tree equality | PASS | Merge commit `e753e991…` tree = `823e3c65…` = pinned tree; the raw checkout log independently shows fetching/checking out that merge commit |
| A8 | Prohibited operations not performed | PASS | Remote holds only the authorized branch and PR; `main` untouched; SQ-05 branch untouched; no force, no merge, no workflow change, no deletion |

## B. Five-identity commit/tree equivalence

| # | Requirement | Result | Independent evidence |
| --- | --- | --- | --- |
| B1 | Pinned branch commit = PR head = run `head_sha` = `eafd632` | PASS | Git object exists; PR #7 `headRefOid`; run 44746950426 `head_sha` (run API) |
| B2 | Synthetic merge SHA recorded; executed source tree = pinned tree | PASS | `e753e991eb255446cbd1ce2be72ec49fb3f4c733`; fetched read-only via `refs/pull/7/merge`; tree `823e3c65…` = `eafd632^{tree}` |
| B3 | Checkout evidence from the raw log | PASS | Log line 93 fetch `+e753e991…:refs/remotes/pull/7/merge`; line 134 `HEAD is now at e753e99` |
| B4 | Production/test/CI content equals the pinned revision | PASS | `git diff eafd632 HEAD -- src tests .github` empty; workflow blob equal at all three refs |

## C. Environment and genuine execution

| # | Requirement | Result | Independent evidence |
| --- | --- | --- | --- |
| C1 | Genuine hosted Ubuntu x64 (fixture L — no WSL2/Windows substitution) | PASS | Raw log: hosted image `ubuntu-26.04` / `20261111.196.1`, Ubuntu 26.04.1 LTS, Azure northcentralus, runner 2.338.2; `pull_request` event on GitHub-hosted runners |
| C2 | Native LatticeDbSharp use | PASS | `LatticeDbSharp 0.2.0` at the executed tree; `src/Andirit.LatticeDb.csproj` restored on the runner; all 75 store-opening/publication/reopen cases pass on linux-x64; `Andirit.LatticeDb` production code instrumented at 368/466 in the run's coverage artifact |
| C3 | Toolchain recorded | PASS | SDK 10.0.404 / runtime 10.0.15 (workflow pin `10.0.x`), from `setup-dotnet@v5` log |

## D. Binding test results

| # | Requirement | Result | Independent evidence |
| --- | --- | --- | --- |
| D1 | Entire current expanded LatticeDb suite executed, **no filter** | PASS | Raw-log step is the whole-project `dotnet test` with no `--filter`/`test-filter`; 75 unique cases match the preparation census |
| D2 | 75 passed / 0 failed / 0 skipped | PASS | Raw-log summary `Passed! - Failed: 0, Passed: 75, Skipped: 0, Total: 75`; regex and failed-total scan over the whole log |
| D3 | M11.4 counter-check — coverage 368/466 branches from raw Cobertura | PASS | Ubuntu artifact root `branches-covered=368 branches-valid=466 branch-rate=0.7896`; independent condition-sum derivation 736/928 → 368/464 (the +2 coverlet granularity offset) |
| D4 | 900/969 lines from raw Cobertura | PASS | Root `lines-covered=900 lines-valid=969 line-rate=0.9287`; condition-sum derivation agrees |
| D5 | Thresholds (70, line+branch) | PASS-QUAL | Gate exit 0 on the run (job success); counters ≥ 70 both — the platform-independent threshold check passes on Ubuntu for the first time |
| D6 | Supplementary suites | PASS | Raw log: core 345/0/0, CSharp 372/0/0, TreeSitter 157/0/0 (supplementary evidence only, per the matrix qualification "supplemental runs distinct") |
| D7 | Packaging | PASS | `pack` job success on hosted Ubuntu (log preserved) |
| D8 | Complete CI matrix disposition | PASS | Run conclusion `success`: ubuntu/windows/macos test jobs + pack all success (run/job API + rollup) |

## E. Evidence integrity and preservation

| # | Requirement | Result | Independent evidence |
| --- | --- | --- | --- |
| E1 | All 20 G2 manifest entries verified | PASS | Rehash from Git blobs at `29bdc5c2…`: 20 entries, 0 mismatches, complete coverage (manifest self-excludes) |
| E2 | L1–L9 receipts validated against raw evidence | PASS | Every L-row fact re-derived: L1 environment, L2 75/0/0 no filter, L3/L4/L5 suite behaviors (suite contents at the pinned tree), L6 managed-recovery scope, L7 parity counters, L8 preserved artifacts, L9 operation boundary |
| E3 | Prior SQ-05 G3 package preserved | PASS | `CI1A-S2.5-S25-SQ05-Q1-evidence` 6/6 entries verify at HEAD; unchanged since `eb1abef` |
| E4 | Prior SQ-05 G2 package preserved | **QUAL** | `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence` unchanged since `df57a2a`, but the commit contains **14 of the manifest's 18 entries** — the four `coverage-results-ubuntu-latest/*.cobertura.xml` artifacts were never committed (manifest describes them; Git does not carry them). Historical finding; this commission's own coverage counters are independently derived from the current run's artifacts |
| E5 | PR #7 remains unmerged | PASS | PR #7 state OPEN, `mergeCommit` null (re-queried this session) |
| E6 | PR #6 / SQ-05 remote anchor | **QUAL** | PR #6 is now **MERGED** (fast-forward; merge commit = head SHA `7457284…`; merged 2026-11-10T23:05:29Z, i.e., after the SQ-05 G3 assessment recorded it as open/unmerged, and contrary to the WP-S25-2 §12 cleanup determination that its disposition required a separate bounded remote-resource disposition). Its content is an ancestor of remote `main` (`48370711…` contains `7457284…`), so no evidence was lost; the SQ-05 G2/G3 packages are committed repository evidence and are preserved (E3/E4). Recorded for owner reconciliation; does not block M11.4/M14.1 |
| E7 | Production/tests/CI unchanged from the pinned revision | PASS | `git diff eafd632 HEAD -- src tests .github` empty; workflow blob equal at pinned/base/merge |

## F. Qualifications assessment (commission §4)

| # | Qualification | Assessment | Blocks M11.4/M14.1? |
| --- | --- | --- | --- |
| F1 | Managed-fault-only durability | The executed suite exercises managed recovery/lifecycle; crash/power-loss durability (F-02) remains unproven — carried unchanged, outside M11.4/M14.1 scope ("cross-platform native tests pass" / "suite passes on Linux") | **No** |
| F2 | Uninstrumented locking/WAL/mmap internals | Suite-observed platform parity (identical counters 75/0/0, 368/466, 900/969 on Linux and Windows); internals not instrumented. M14.2 requires documentation, which the run receipts now provide at suite level; internal mechanics remain a Stage-6-adjacent limitation | **No** (for M11.4/M14.1 as written; qualifies M14.2 documentation depth) |
| F3 | Ubuntu 26.04 demonstrated vs the SQ-05 24.04 environment | The binding text binds to genuine Ubuntu x64 (fixture L) without pinning a point release. The demonstrated environment (Ubuntu 26.04.1 LTS hosted, SDK 10.0.404) is genuine hosted Ubuntu x64; the difference from 24.04.5 is recorded as the demonstrated scope and limits any claim about other generations | **No**; qualification recorded |
| F4 | Independence of hosted-run receipts | G2 was executed by the operator session that also ran this G3 review's commission turn; independence here is method-level (primary-source re-derivation), matching the amendment of the SQ-05 G3 record ("hosted-run independence is receipt-based") | **No**; qualification recorded |

## G. Dispositions

| Scope | Disposition |
| --- | --- |
| **M11.4** (cross-platform native tests pass; class A, fixture L) | **SUPPORTED WITH QUALIFICATIONS** — expanded native suite coverage verified on genuine hosted Ubuntu x64 at the corrected revision, with linked Linux source/run/log identities (F1–F4 recorded) |
| **M14.1** (Linux CI executes native tests — entire current expanded suite) | **SUPPORTED WITH QUALIFICATIONS** — 75/0/0 executed on genuine hosted Ubuntu at the pinned revision with five-identity checkout/tree verification; F1–F4 recorded; E4/E6 historical findings do not affect the current evidence |
| **M14.2** (platform behavior documented) | **SUPPORTED WITH QUALIFICATION** — suite-level behavior matrix now covered at Linux/Windows counter parity (F2 records the instrumentation limit) |
| **Overall G3** | **SUPPORTED WITH QUALIFICATIONS** — G3 support is not G4 acceptance |