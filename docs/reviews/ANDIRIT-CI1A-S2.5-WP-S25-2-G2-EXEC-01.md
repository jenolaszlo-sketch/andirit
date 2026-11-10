# ANDIRIT-CI1A-S2.5-WP-S25-2-G2-EXEC-01 — WP-S25-2 G2 Linux qualification publication, execution, and evidence capture

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-2-G2-EXEC-01` — executing G1 decision `adr-ci1-01-ci1a-s2-5-wp-s25-2-exec-owner-01.md` (`SQ-05 PUBLICATION AND EXECUTION AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Reviewed local source:** `8ac64e9e90dba5d57e8ba829229e47ee07de1966` (tree `7eaca047cead036d697138c3122e4d9b272de140`).
**Executed qualification revision:** commit **`7457284451dff8ddb0417f83ecc19097103316fc`**, tree **`fba379453ceb3088cab97141ebae5c4c4978e3d3`** (pinned by G1; verified unchanged).
**Producer disposition:** **`LINUX EXECUTION COMPLETE WITH RESTRICTIONS`**.

> The authorized publication, PR, and GitHub-hosted Ubuntu execution were performed exactly
> within G1 §5 authority. **The entire expanded 52-test `Andirit.LatticeDb.Tests` suite passed
> on genuine Linux** (`Passed! - Failed: 0, Passed: 52, Skipped: 0`), as did every other suite
> in the run (345 + 353 + 157). The CI job conclusion is nevertheless **`failure`** — caused
> solely by the repository's pre-existing coverlet **branch-coverage threshold** (65.77% < 70),
> which fails identically on Windows and macOS (platform-independent, predates this work) and
> is **not** a Linux behavior issue, a test failure, or an implementation defect. No rerun was
> performed (the failure is deterministic; rerunning to obtain green is prohibited by this
> commission and would not succeed). The result is producer evidence only — it does **not**
> discharge SQ-05.

---

## 1. Verified local preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `8ac64e9e90dba5d57e8ba829229e47ee07de1966` / `7eaca047cead036d697138c3122e4d9b272de140` — both expected (the G1 decision commit) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote (recorded, pre-contact) | `origin/main` = `443c1927…`, **0 behind / 73 ahead**; nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12`; `gh` 2.97.0 authenticated as `jenolaszlo-sketch` (scopes `repo`, `workflow`); `git credential.helper=manager` |

## 2. Live remote prerequisites (§4 of G1 — all passed before publication)

| Prerequisite | Verified result |
| --- | --- |
| Remote identity | `origin` = `https://github.com/jenolaszlo-sketch/andirit.git` (matches G1); `git ls-remote` succeeded |
| Remote `main` state | `refs/heads/main` = `443c192733a53d2f1da415743debb84e8c81acb5` — **identical to the recorded `origin/main`** (the remote has **not** advanced; the stale local ref is current); commit dated 2026-11-05 "docs(review): record S2.2 WP5-R1 F1 remediation evidence" |
| Ancestry compatibility | remote `main` is a **strict ancestor** of the pinned `7457284…` (`git merge-base --is-ancestor` exit 0) — the synthetic PR merge tree will equal the pinned tree |
| Qualification branch availability | `refs/heads/qualification/ci1a-s25-sq05-linux` **absent** pre-publication (no `qualification/*` refs existed) |
| Workflow identity | `ci.yml` blob `be48426f9be77eacf5dc3de925566126eedad03b` — identical at `7457284`, at HEAD, and in the index; exact-byte SHA-256 `2c650722bcca19f8175051ca30445824729f44548f9b492d51a35d45b6d472de` |
| Credentials/permissions | push and PR creation succeeded with the authenticated account (no elevation, no workaround) |
| Published material | `443c1927..7457284` = **71 commits**, all committed accepted work (decisions, reviews, evidence, bound fixtures); no restricted or unfinished material |

Receipts: `remote-preflight.txt`, `push-receipt.txt`, `pr-create-receipt.txt`, `pr-view.json`.

## 3. Exact push receipt (§5 — the single authorized publication)

| Field | Value |
| --- | --- |
| Command | `git push origin 7457284451dff8ddb0417f83ecc19097103316fc:refs/heads/qualification/ci1a-s25-sq05-linux` |
| Exit code | **0** |
| Result | `* [new branch]  7457284451dff8ddb0417f83ecc19097103316fc -> qualification/ci1a-s25-sq05-linux` |
| Post-push verification | `git ls-remote origin refs/heads/qualification/ci1a-s25-sq05-linux` → `7457284451dff8ddb0417f83ecc19097103316fc` — **exactly the pinned commit** |
| Force/tags/other refs | none (single ref, no force) |

## 4. PR creation and state

| Field | Value |
| --- | --- |
| PR | **#6** — `https://github.com/jenolaszlo-sketch/andirit/pull/6` |
| Head / Base | `qualification/ci1a-s25-sq05-linux` (`7457284…`) / `main` (`443c1927…`) |
| State | **OPEN, unmerged**; `mergeable: MERGEABLE`; described as qualification-only ("do not merge") |
| PR receipt | `pr-view.json`, `pr-create-receipt.txt` |

## 5. Five-identity commit/tree verification (G1 §7 — binding gate: PASSED)

| # | Identity | Value |
| --- | --- | --- |
| 1 | Pinned qualification commit | `7457284451dff8ddb0417f83ecc19097103316fc` |
| 2 | Remote qualification branch HEAD (post-push `ls-remote`) | `7457284451dff8ddb0417f83ecc19097103316fc` |
| 3 | PR head SHA (`headRefOid`) | `7457284451dff8ddb0417f83ecc19097103316fc` |
| 4 | Workflow run `head_sha` (run **42744298084**, event `pull_request`) | `7457284451dff8ddb0417f83ecc19097103316fc` |
| 5 | GitHub synthetic merge commit | `331ad6062908458665a40ca0c777b79507c52709` (`refs/pull/6/merge`), retrieved post-run |
| — | **Executed tree** | **`fba379453ceb3088cab97141ebae5c4c4978e3d3`** — byte-identical to the pinned qualification tree (`git diff 7457284..331ad606` empty; the merge contains the pinned commit; base `443c1927` is the strict-ancestor remote main) |

**Executed tree == authorized qualification tree: YES.** The CI demonstrably executed the pinned qualification contents. Receipt: `identity-verification.txt`.

## 6. Genuine Linux environment (L1)

| Field | Value (from run 42744298084, `test (ubuntu-latest)` job log) |
| --- | --- |
| Runner / image | GitHub-hosted `ubuntu-latest` → image **ubuntu-24.04**, version `20261101.360.1` (Hosted Compute Agent 20261028.624, Azure westus, runner 2.338.2) |
| Operating system | **Ubuntu 24.04.5 LTS** (Linux) |
| Architecture | **x64** |
| .NET SDK / runtime | **SDK 10.0.403**, runtime **10.0.14** (installed by `setup-dotnet@v5`, `dotnet-version: 10.0.x`) |
| Native engine | **`liblattice.so` (linux-x64)** resolved and exercised — the suite's store-opening/publication/reopen tests all succeeded, which is only possible with a working native LatticeDB engine |
| Filesystem | ext4 (Ubuntu default), case-sensitive; per-test disposable work directories (suite-standard) |
| Workflow revision | `.github/workflows/ci.yml` blob `be48426f…` (exact-byte SHA-256 `2c650722…`) — the G1-inspected, unmodified workflow |

## 7. L1–L9 qualification matrix (producer evidence)

| # | Item | Result |
| --- | --- | --- |
| **L1** | Native environment | Captured (§6): Ubuntu 24.04.5 LTS x64; SDK 10.0.403/runtime 10.0.14; native `liblattice.so` exercised; hosted runner identity recorded |
| **L2** | Expanded test suite | **`Andirit.LatticeDb.Tests`: Passed 52 / Failed 0 / Skipped 0 / Total 52** on genuine Linux, Release configuration — the complete current suite, matching the Windows baseline exactly |
| **L3** | Persistence and reopen | Green on Linux: `LatticeCodeGraphStoreTests` (20), `OccurrenceStage1NativeTests` (10; durable complete-empty, reopen), `S23LifecycleQualificationTests` (in the CSharp suite, also green on Linux) |
| **L4** | Refusal and corruption | Green on Linux: `OccurrenceStage1NativeTests` (schema/version tamper), `S23Wp5NamespaceRefusalTests` (namespace), `OccurrenceRemediationNativeTests` (noncanonical payloads), `S24SurfaceRefusalMappingTests` (IF-05 mapping), `HistoricalStorageIdentityTests`, `LatticeDbPublicApiContractTests` |
| **L5** | Publication authority | Green on Linux via the CSharp suite (353/0), which carries `S23NativeAuthorityTests` and `S23OccurrenceOwnershipRegressionTests` (authority envelope, orphan-owner correspondence, stale/foreign handles) |
| **L6** | Recovery and lifecycle | Green on Linux: `New01RecoveryProjectionTests` (7; known-failure recovery projection, unknown-outcome quarantine). **Managed fault injection only — no crash/power-loss durability claim (F-02)** |
| **L7** | Platform behavior | Native mirror exercised on Linux (`NativeGraphMirrorTests`, 5) and the Stage-1 performance probe ran; environment facts recorded (§6); Linux results are **identical to the Windows baseline** (52/52, same counts); no behavioral divergence observed at the suite level. Internal locking/WAL/mmap mechanics are not directly instrumented by the suite — recorded as an observation, not a claim |
| **L8** | Evidence and provenance | Executed commit/tree verified (§5); run URL/ID, PR URL/number, workflow revision, runner metadata, complete Ubuntu job log, per-suite counts, exit codes, coverage artifacts (downloaded + hashed), sealed manifest (18 entries / 0 mismatches) |
| **L9** | Regression and boundary | `git diff` for `src/`, `tests/`, `.github/` between the pinned revision and HEAD is **empty** — zero production/test/CI changes for Linux execution; every suite test executed (nothing skipped, nothing excluded); no unauthorized modification |

**Honesty note (per commission §8):** the green suite demonstrates the behaviors its tests exercise (L3–L6 as mapped). It does **not** establish: multi-process writers, crash/power-loss durability, macOS qualification, full platform certification, or any performance contract. None of those is claimed.

## 8. Complete regression receipts (run 42744298084, `pull_request`, head `7457284…`)

| Suite (Linux, Release) | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Andirit.Tests (core) | 345 | 0 | 0 |
| Andirit.CSharp.Tests (incl. S23 authority/lifecycle/ownership) | 353 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| **Andirit.LatticeDb.Tests (SQ-05 target suite)** | **52** | **0** | **0** |
| **Total** | **907** | **0** | **0** |

`pack` job: **success** (package + smoke consume, Ubuntu).

**Job conclusions:** `pack` = success; `test (ubuntu-latest)`, `test (macos-latest)`, `test (windows-latest)` = **failure** — each failing **only** at the step *"Test and coverage (LatticeDb)"* **after** all 52 tests passed, with the identical error on every OS:

```
coverlet.msbuild.targets(72,5): error : The minimum branch coverage is below the specified 70
| Total   | 87.01% (line) | 65.77% (branch) | 97.14% (method) |
```

**Finding F-G2-01 (non-Linux; CI quality gate).** The repository's own coverage gate (`/p:Threshold=70 /p:ThresholdType=line,branch` on the LatticeDb step) fails because **branch coverage is 65.77% < 70%**. This is **platform-independent** — the identical error occurs on Windows (`windows-latticedb-coverage-error.log`) and macOS (`macos-latticedb-coverage-error.log`) — and **pre-existing**: the suite and codebase evolved since the last green hosted run (the historical green run 40991194763 predates the current suite). It is **not** a Linux behavior issue, **not** a test failure (all 52 tests passed), and **not** an implementation defect. Resolution (threshold adjustment, additional branch tests, or a step-scoped exemption) requires a **separate owner decision — not authorized by G1 or this commission**. No rerun was performed: the failure is deterministic, and rerunning to obtain a green result is prohibited by this commission (§9) and would not succeed.

## 9. Evidence provenance and manifest

| Class | Items |
| --- | --- |
| Live remote verification | `remote-preflight.txt` (ls-remote, ancestry, branch absence, workflow blob identity, credentials) |
| Authorized publication receipt | `push-receipt.txt` (command, exit 0, new-branch result) + post-push `ls-remote` = pinned commit |
| PR receipts | `pr-create-receipt.txt`, `pr-view.json` (PR #6, head/base OIDs, MERGEABLE, OPEN), `merge-commit.json` (pre-run null state) |
| CI execution receipts | `run-list.json`, `run-final.json` (run 42744298084, conclusion failure), `run-jobs.json` (per-job/step conclusions) |
| Linux execution evidence | `gh-run-42744298084-ubuntu-job.log` (complete Ubuntu job log: environment, all four suite summaries, coverage table, threshold error) |
| Coverage artifacts (downloaded, exact bytes) | `coverage-results-ubuntu-latest/` — 4 cobertura files (core 1,420,071 B `3B801E30…`; CSharp 531,903 B `43B62D8D…`; LatticeDb 216,358 B `7D73E0E6…`; TreeSitter 620,106 B `5B777694…`) |
| Platform-independence extracts | `windows-latticedb-coverage-error.log`, `macos-latticedb-coverage-error.log` (identical threshold error) |
| Identity verification | `identity-verification.txt` (five identities; executed tree == pinned tree) |
| Manifest | `manifest.json` — **18 entries, 0 mismatches** (self-excluding SHA-256; sealed by `CI1A-S2.5-WP-S25-2-G2-EXEC-tests/seal-evidence.ps1`); `reviewed-head.txt` |

**Provenance discipline.** Previously accepted evidence was never resealed or modified (an accidental invocation of another commission's seal script was caught by `git status` and immediately reverted via `git checkout --` before any commit; the committed SQ04-Q1 package remains byte-identical — manifest 20/0 re-verified). Historical artifacts, fixtures, and the frozen oracle were not touched.

## 10. Findings and limitations

| ID | Finding | Severity | Blocking? |
| --- | --- | --- | --- |
| F-G2-01 | LatticeDb **branch coverage 65.77% < 70** fails the repository's own CI coverage gate on **all three OSes** (identical error; platform-independent; pre-existing since the suite evolved; all 52 tests pass) | Significant (CI gate) | **Not a G2/Linux blocker** — the Linux execution and its evidence are complete. It is a **repo CI configuration matter requiring a separate owner decision** (threshold vs branch tests vs step-scoped exemption). Until resolved, the PR's CI conclusion remains `failure` and **must not be "fixed" by assertion weakening, test skipping, or unauthorized CI edits** |
| Q4-OBS (carried) | Hosted-run independence is receipt-based (reviewer verifies binding + counts; hosted records are producer-independent but not reviewer-executed) | Info | No |
| Q4-OBS (carried) | Run/artifact retention is limited — logs and artifacts were downloaded and hash-sealed promptly | Info | No |
| — | The qualification PR (#6) remains **open and unmerged**, and the remote branch `qualification/ci1a-s25-sq05-linux` remains in place — both left exactly as the G1 operation left them (no deletion/merge authorized) | Info | No |
| — | ubuntu-latest image migration notice (Ubuntu 26 planned) and Node 20 deprecation warnings — recorded; no impact on this run | Info | No |

## 11. G2 execution disposition

**`LINUX EXECUTION COMPLETE WITH RESTRICTIONS`.**

Publication, PR, and execution were strictly within G1 §5 authority; the executed identity satisfies G1 (five-identity verification, executed tree == pinned tree); genuine Linux execution is demonstrated (Ubuntu 24.04.5 LTS x64, native `liblattice.so` exercised); **the required expanded 52-test suite completed successfully with zero failures and zero skips**; sufficient reviewable evidence is captured and sealed. The restrictions: (1) the CI job conclusion is `failure` due to the platform-independent pre-existing branch-coverage gate (F-G2-01) — recorded, not remediated, not rerun; (2) hosted-run independence is receipt-based; (3) internal locking/WAL/mmap mechanics are not directly instrumented (observation only). The execution result is **producer evidence only** — it does **not** discharge SQ-05; independent G3 qualification and G4 owner acceptance remain separate.

## 12. Recommended G3 independent qualification scope

The independent `S25-SQ05-Q1` reviewer (≠ producer) should:

1. **Verify the five-identity binding** (§5) from GitHub records: PR #6 `head_sha`, the synthetic merge commit `331ad606…`, and its tree `fba37945…` against the pinned revision — including the merge-ref ancestry argument.
2. **Re-verify the Linux results from the hosted records**: run 42744298084's Ubuntu job — 52/52 LatticeDb (0 failed, 0 skipped), plus 345/353/157 in the other suites; confirm the failure is exclusively the coverage threshold (F-G2-01) and platform-independent.
3. **Assess L1/L7 environment receipts** (Ubuntu 24.04.5, SDK 10.0.403, native engine exercised) and the platform-behavior matrix's stated observation limits.
4. **Verify evidence integrity**: the G2 manifest (18/0), the downloaded coverage artifacts' hashes, and the unchanged historical/accepted evidence (including the SQ04-Q1 and BIND-01 packages).
5. **Classify F-G2-01** as a CI quality-gate matter (platform-independent, pre-existing) distinct from SQ-05 Linux behavior, and carry it as a restriction — the coverage-gate resolution belongs to a separate owner decision.
6. **Confirm scope limits**: no crash/power-loss, multi-process, macOS, or full-platform claims; managed-fault vs real-crash distinction preserved; Windows-x64 baseline and serialized single-writer qualifications untouched.

**Then** the separate G4 `WP-S25-2-OWNER` acceptance decision — which must also address F-G2-01 (the coverage gate) as its own owner matter.

---

*Additive execution evidence only. The authorized remote operations were exactly: one branch push (pinned commit), one PR, one workflow run (observed), one merge-ref fetch, one artifact download. No source/test/workflow/fixture changes; no PR merge; no remote deletion; no push of `main`; no push of the new evidence commit. The final local commit/tree and full operation log are reported in the handoff.*