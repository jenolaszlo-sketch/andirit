# ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1

**Date**: 2026-11-25
**Gate**: S6.3 integration, publication and hosted-evidence closure (remediation cycle after the first hosted run failed).
**Mode**: test-infrastructure remediation, documentation and evidence only. **No production-code change**,
no dependency upgrade, no coverage-threshold change, no workflow change, no test weakening, no force
push, no tag or package publication.
**Predecessor record**: `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01.md` (implementation). This record supersedes
nothing and rewrites nothing; it records the integration and the two defects the hosted run exposed.

---

## 1. Formal disposition

**`A — IMPLEMENTED AND READY FOR INDEPENDENT QUALIFICATION`**

Every mandatory implementation and evidence gate is satisfied. The success is bound to the remediated
revision `d3c9359d1abd8c89a7214e37f074802e5d2fa238` (not merely to a generic green status), and the
hosted evidence — runner identity, per-project counts, coverage and the multi-process execution
chain — is sealed in the extension package `docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R1-evidence/`
(self-excluding SHA-256 manifest, `manifest_sha256` `b17a726d…`, 11 entries, 0 mismatches). Next
commission: `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` (not executed here).

---

## 2. Integration summary (owner-authorized)

| Item | Value |
| --- | --- |
| Verified remote revision before integration | `4ef34b51bdcc60bb77450270564d0e2e0dff9330` |
| Remote commits integrated | `8715ac3` "docs: refresh Andirit README for the published preview", `4ef34b5` "docs: clarify package imports in integration examples" |
| Conflicts | **none** — incoming touched only `README.md` and `docs/runtime-guide.md`; the S6.3 commits touch only new `docs/…` files and `tests/…` files |
| Content fidelity | every S6.3 blob identical by Git hash before/after; sealed manifest byte-identical |
| Recovery reference | `refs/backups/s63-impl-original` → `d9461c06b9ba7a42b20a3ce100d28f39d1935a40`, **unpublished** |
| Commit mapping (original → integrated) | `1cbce3b → 94a8c19` (entry assessment), `33b5867 → ece6002` (implementation), `31673f5 → 89ae722` (erratum), `d9461c0 → 7a64ab2` (formatting), plus a new reconciliation commit `ed216e8` |
| Publication | fast-forward `4ef34b5..ed216e8` (no force) |

### 2.1 Git attribution

`user.name = jenol`, `user.email = jenolaszlo-sketch@users.noreply.github.com`. Author and committer
identity is `jenol <jenolaszlo-sketch@users.noreply.github.com>` for every commit in the published
range **and** for the whole previously published S6.1/S6.2 lineage. **No** `Co-authored-by:` trailers
and no AI/agent identities are present anywhere in the range. Previously published history was not
rewritten.

---

## 3. Root cause of the first hosted failure (run 45513525415)

**Historical failure evidence — preserved, not deleted.** Run
[45513525415](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45513525415), workflow `ci`,
event `push`, `headSha = ed216e87a2222e974ff0e477d1b04d87f151acc4`, conclusion **failure**:

| Job | Result |
| --- | --- |
| `test (ubuntu-latest)` | failure — LatticeDb 112/114 |
| `test (windows-latest)` | failure — same two cases |
| `test (macos-latest)` | failure — same two cases |
| `pack` | success |

All four per-project test steps passed except LatticeDb; the two failures were:

1. `NativeDurabilityTests.ConcurrentMultiProcessAccess_FileLockingAndConsistency` (D-05)
2. `NativePublicationCorrectnessTests.P15_MultiProcessPublication_FileLockSerializesAndReopenSeesCommittedPublication` (P-15)

Both failed in 1–6 ms with `InvalidOperationException` naming the unbuilt worker test-harness assembly.

**Cause.** Hosted `ci.yml` runs `dotnet test <project>` **per project**, which builds only that project
and its dependencies. The standalone Worker project is a separate solution project, so it was restored
but never built, and its assembly was not in the test project's output. My earlier local verification
had built the whole solution first, which masked the defect: the local runs passed while the hosted
per-project runs failed. `src/` was never involved — the production delta remained zero.

**Fix (test infrastructure only, 4 files).**

| File | Change |
| --- | --- |
| LatticeDb test project (`.csproj`) | added a `<ProjectReference>` to the Worker project, so a per-project `dotnet test` builds and copies the worker assembly |
| Worker project (`.csproj`) | removed its `ProjectReference` back to the test project (unnecessary — the worker exercises only the production store; keeping it would have made a dependency cycle) |
| both multi-process test files | `LocateWorkerAssembly()` now first probes the worker assembly copied into the test output, then falls back to the repository-root walk; a missing worker now fails loudly |

<!-- PART2 -->

## 4. Finding F-S63-01 — a second, pre-existing defect exposed by the fix

Recording the previously hidden skip as a formal qualification finding, as required.

| Item | Detail |
| --- | --- |
| Finding | `NativeDurabilityTests.AbruptProcessTermination_DurableStateConsistent` (D-07) opened by probing for the worker **apphost executable** in the test output and, when that probe failed, executed `return;` |
| Effect | whenever the host ran the test project per project — as hosted `ci.yml` does — the probe failed and the method **returned**: the case reported a pass without executing any part of its abrupt-termination scenario |
| Evidence | the case was executed 0 times on hosted runs; once the worker assembly was actually copied into the test output it began to run and immediately failed on a native lock race (F-S63-02) |
| Severity | **High as a qualification-integrity defect** (a "pass" that ran nothing), though the underlying durability behaviour was independently exercised by D-01…D-04 and by the S6.2 reviewer probes |
| Not closed by | the project-reference fix alone — that only removed the *reason* for the skip |
| Remediation | the silent skip and the apphost-only discovery are removed. The case now launches the worker through the dependency, **requires** its READY signal and that it has not exited before termination, terminates it with `Kill(true)`, and fails loudly if the worker cannot be located |

**Adjacent-test scan (performed after the fix).** All 14 `*.cs` files of the LatticeDb test project were
scanned: **0** remaining `[Fact(Skip = …)]`, **0** early-return guards conditioned on `File.Exists` /
`worker` / `HasExited`, and every remaining `File.Exists` occurrence sits inside a `Delete…`/cleanup
helper. No other case can pass by not running.

---

## 5. Finding F-S63-02 — post-termination native lock race (MITIGATED)

**Cause.** After `Process.Kill(true)` the OS releases the file lock with no ordering guarantee, so an
immediate native open can fail with `LatticeException` native error code `-16` ("LatticeDB operation
`database/open` failed with `-16`: Database is open in another process"). This was observed locally on
Windows in the first *executed* D-07 run (reproduced while reading the native tables), and it is the
same condition the hidden skip would have masked indefinitely.

**Remediation.** D-07 keeps its original abrupt-termination scenario. What changed: the native tables
are now read **before** a store is opened (so no `LatticeCodeGraphStore` holds the lock while the native
rows are read), and both the raw native open and the store open are wrapped in a **bounded** retry.

| Property | Value |
| --- | --- |
| Retry budget | 30 s deadline, 100 ms polling — finite by construction |
| Retries only on | `LatticeException.NativeErrorCode == -16`, the pinned dependency's "open in another process" condition |
| All other failures | propagate unchanged — the `catch … when` filter excludes them by construction, so the retry can never suppress an unexpected exception |
| Persistent failure | `TimeoutException` at the deadline carrying the last observed native message as context, so a real resource-release defect can never be concealed |
| Effect on assertions | none — the durability invariants (staged row count 0 or 1, healthy reopen) are unchanged; no threshold was lowered |

**Retry narrowing.** `LatticeException.NativeErrorCode` is an `int` (verified by reflection over the
pinned assembly), so the retry compares against the literal `-16` rather than a message match; unrelated
native errors (corrupt page, different dependency condition) are neither retried nor delayed.
`CodeGraphStoreException` opens retry only when their `InnerException` is that same `-16` condition.

**Repetition evidence.** D-07 was executed 5 consecutive times on the remediated revision: **5/5
passed** (544–587 ms each), in addition to the full-suite executions. Not a single lucky run.

**Explicit qualification (recorded as required).** The retry path itself was **not exercised** during
those successful repetitions: producer probe I, which terminates a hold worker and then attempts a raw
native open in a tight loop under the same `-16`-only filter and the same finite 30 s budget, recorded
`attempts=1, lockRaceObserved=False` on all three of its runs on this Windows host — after
`WaitForExit(30000)` the lock was already released. The race is therefore **real but timing-dependent**;
its observed evidence is the single earlier failing D-07 execution, and the bounded retry is a robustness
measure for a genuinely observed condition, **not** a claim that the retry loop ran on every pass.

**Complementary deterministic test (added, without altering D-07).** Because deterministic verification
of the *filter* is desirable, a separate committed test now establishes, deterministically and in-process,
that the native "open in another process" condition is exactly what the retry filter selects: while one
`LatticeCodeGraphStore` holds the file, a second raw native open fails with `-16`; after the first is
disposed the same open succeeds. D-07's original abrupt-termination semantics are untouched.

---

## 6. Verification of the remediated revision

| Gate | Result |
| --- | --- |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| Release build (whole solution) | 0 warnings, 0 errors |
| Per-project LatticeDb suite with the CI coverage gate (`Threshold=70 line,branch`) | **115 passed / 0 failed / 0 skipped**, gate exit 0 |
| Full solution regression | **989 passed / 0 failed / 0 skipped** (TreeSitter 157, LatticeDb 115, Core 345, CSharp 372) |
| **Difference from the pre-remediation result** | LatticeDb 114 → 115 and the solution total 988 → 989. The only additions are the complementary deterministic `-16`-filter test described in §5 (one test); nothing was removed, weakened or re-baselined. Every previously executed case still executes with the same identifier and the same invariants |
| LatticeDb module coverage | 92.87 % line / 78.96 % branch (unchanged; threshold 70 %) |
| Reviewer probes A–I | **27/27 PASS** (3 repetitions × 9 probes), exit 0 |
| Production delta vs integrated parent and vs original baseline | **zero** (`git diff --name-only … -- src/` empty) |
| LatticeDbSharp pin | `0.2.0` unchanged |
| Original sealed evidence | 17 entries, 0 mismatches, manifest hash `fcab38f1…` unchanged |

### 6.1 Clean-checkout verification (mandatory check 1)

| Step | Evidence |
| --- | --- |
| Isolated checkout | `git worktree add` to a fresh path checked out at the remediated revision |
| Command executed | the **exact** per-project command from `ci.yml` (single project, no prior whole-solution build), with `/p:CollectCoverage=true`, `Include=[Andirit.LatticeDb]*`, `Threshold=70`, `ThresholdType=line%2cbranch` |
| Worker built by the dependency | the worker assembly and its apphost are produced in the Worker project's output during that per-project build **and** copied into the test project's output |
| Locator path used | the local-first probe succeeds — evidenced by the copied assembly in the test output and the multi-process cases passing |
| No residual artifacts | the worktree began with no `bin`/`obj`; after building only the test project, the Worker's own `bin` was **deleted** and the suite re-run with `--no-build`, which still passed — proving the copied assembly, not a stale build, is used |

---

## 7. Conditions and findings carried forward

| ID | Status |
| --- | --- |
| C-S63-1 … C-S63-10 | carried; C-S63-4 (hosted Ubuntu) to be closed by the hosted run on the remediated revision |
| F-QU-01 (D-05 logic) | closed by the S6.3 remediation; D-05 now executes |
| F-QU-02 (no genuine multi-process evidence in the suite) | discharged by D-05 + P-15 |
| **F-S63-01** (D-07 hidden skip) | **new, closed by this remediation** — reviewer to re-verify |
| **F-S63-02** (post-termination lock race) | **new, mitigated** — bounded `-16`-only retries; durability assertions unchanged; retry path not exercised on every pass (explicit qualification above) |
| Q-S61-1…8 / Q-S62-1…9 / Q-L1…8 / MR-1…4 / GH-1…4 / DF-1…8 | carried unchanged |
| Hosted run 45513525415 | **preserved as historical failure evidence** (not deleted, not re-attributed) |

---

## 8. Hosted evidence closure (all mandatory gates)

| Gate | Evidence |
| --- | --- |
| Hosted run bound to the remediated revision | **[45639342986](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45639342986)**, `push` on `main`, `headSha = d3c9359d1abd8c89a7214e37f074802e5d2fa238`, conclusion **success** |
| Jobs | `test (ubuntu-latest)` **success** (job 139293320941), `test (windows-latest)` **success**, `test (macos-latest)` **success**, `pack` **success** |
| Runner identity (genuine hosted Linux x64) | GitHub **Hosted Compute Agent** (not self-hosted), Azure / centralus, runner 2.338.2, **Ubuntu 26.04.1 LTS**, workspace `/home/runner/work/andirit/andirit`, shell `/usr/bin/bash`, `DOTNET_ROOT=/usr/share/dotnet`, SDK 10.0.404, runtime 10.0.15 — zero Windows/WSL/Docker indicators |
| Hosted `dotnet format --verify-no-changes` | passed |
| Hosted counts | Core 345, CSharp 372, TreeSitter 157 — all 0 failed / 0 skipped |
| Hosted LatticeDb | **115 passed / 0 failed / 0 skipped** |
| Hosted LatticeDb coverage | **92.87 % line / 78.96 % branch** (900/969 lines, 368/466 branches), identical to Windows; threshold 70 met (artifact retained) |
| Multi-process cases on hosted Ubuntu | executed — the CI step reports 0 skipped; the committed suite contains no `[Fact(Skip)]` and no early-return guard; the Ubuntu coverage is identical to Windows, which is only possible if the same case set ran |
| D-05 / P-15 / D-07 on Windows | executed; D-07 5/5 consecutive passes; clean-checkout per-project run exit 0 |
| Historical failure run | [45513525415](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45513525415) preserved verbatim as historical failure evidence |
| Publication | fast-forward `ed216e8..d3c9359`, no force; the evidence-only commit carrying the extension package is recorded in `commit-mapping.md` |
| Attribution | `jenol <jenolaszlo-sketch@users.noreply.github.com>` for author and committer on every published commit in `4ef34b5..d3c9359`; no co-author trailers, no AI/agent identities |
| Evidence extension | `…-IMPL-01-R1-evidence/` sealed, self-excluding SHA-256 manifest verified byte-exact (7 entries) |

---

## 9. Sealed-evidence preservation and one recorded breach

**Requirement.** Earlier sealed evidence must not be modified; additional evidence must go into a
separately identified extension.

**What happened (recorded, not hidden).** While preparing probe I I edited the producer-prepared
probe harness **inside** the earlier sealed package
`docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/probe/`. That package's own manifest detects
it: two of its seventeen entries (`probe/probe-receipt.txt` and `probe/Program.cs`) no longer matched
their recorded SHA-256 hashes. The manifest itself was never touched, so the mismatch was visible
rather than masked.

**Restoration.** Both files were restored from the sealed commit and the original package now verifies
**17 entries, 0 mismatches, manifest_sha256 `fcab38f1…` unchanged**. The extended A–I harness now
lives in the extension package under `probes/`, and the original package is byte-identical to what it
was when sealed.

**Net effect.** The historical sealed evidence is intact; nothing previously recorded was re-attributed,
reinterpreted or rewritten. The breach is disclosed here so the independent reviewer can verify the
restoration itself (`verify` of the original manifest) rather than take it on trust.

---

## 10. Remaining work and next commission

**None for this commission.** All mandatory gates are satisfied and the disposition is selected in §1.

Hand off to **`ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01`** — independent qualification (gates G-1…G-5).
G-5 and G-6 are **not** granted here, and no independent qualification or owner acceptance was
performed by this producer commission.

---

**Recorded**: 2026-11-25 (completed; hosted evidence collected same day)
**Canonical branch**: `main`
**Tested and published remediation revision**: `d3c9359d1abd8c89a7214e37f074802e5d2fa238`