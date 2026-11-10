# ADR-CI1-01-CI1A-S2.5-WP-S25-2-ROUTE-OWNER-01 — SQ-05 native Linux qualification route decision

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-2-ROUTE-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision:** **`SQ-05 ROUTE READY — OWNER EXECUTION AUTHORIZATION REQUIRED`** (recommended route: **A — GitHub-hosted Ubuntu**, using the existing workflow without modification).
**Gate:** the SQ-05 execution route is fixed and the qualification matrix is defined; **no remote publication, CI execution, Linux qualification, or implementation is authorized by this decision.**

> **Status: RECORDED — OWNER-AUTHORIZED (planning boundary only).**
>
> The existing `.github/workflows/ci.yml` already satisfies the authoritative SQ-05
> requirements (genuine hosted Linux runner; the full expanded `Andirit.LatticeDb.Tests`
> suite; .NET 10.0.x; Release configuration) and requires **no CI modification**. What it
> requires instead is a **separate, explicit owner execution authorization** that names the
> exact remote publication operation (destination ref, exact commits) and the evidence
> retrieval. A successful CI job is supporting evidence — never automatic owner acceptance.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `7457284451dff8ddb0417f83ecc19097103316fc` / `fba379453ceb3088cab97141ebae5c4c4978e3d3` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` is **0 behind / 71 ahead** (no fetch performed; remote not contacted); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Authority chain: S2.5 entry owner (`9b158b8`) → WP-S25-1 owner (`487f17a`, closed) → SQ-04 amendment owner (`7457284`, `SQ-04 SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`) → this route decision. No unexpected state, authority conflict, or evidence-inconsistency.

## 2. SQ-05 normative scope (verified against the current tree)

| Requirement | Authoritative source | Verified current state |
| --- | --- | --- |
| Operating system / architecture | acceptance matrix §14: *"Linux CI executes native tests"*; full Windows/Linux/macOS certification remains Stage 6 | **Linux x64** (genuine native host; WSL2/Docker excluded absent amendment); no specific distribution is mandated by the contract — the hosted `ubuntu-latest` image qualifies as a genuine Linux host |
| .NET SDK / runtime | S2.3/S2.4 accepted environment; no `global.json` present (any 10.0.x SDK resolves) | suite targets `net10.0`; CI installs `10.0.x` |
| Native libraries | `LatticeDbSharp 0.2.0` (Andirit.LatticeDb.csproj) | package ships **`runtimes/linux-x64/native/liblattice.so`** (and win-x64 `lattice.dll`) — the native engine resolves via the standard NuGet asset graph on Linux; no RID work needed |
| Filesystem / path behavior | matrix §14 platform-matrix row | covered by suite execution + explicit L7 observations (locking, WAL, memory mapping, path/case semantics) |
| Persistence / durable reopen | matrix §10 rows; S2.3 acceptance §5 | covered by the suite's durable reopen/lifecycle tests (L3) |
| Publication / authority behavior | matrix §9 rows; S2.3 §5 | covered by the suite's authority tests (L5) |
| Schema and corruption refusal | matrix §10 rows 3–5; register SQ-06/D5 | covered by `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`, `Store_RejectsOccurrenceUnitsWithoutMeta`, `S23Wp5NamespaceRefusalTests` (L4) |
| Recovery / serialized single-writer | register SQ-02/SQ-03; NEW-01 acceptance | covered by the NEW-01 recovery-projection and schedule tests (L6); **managed fault injection ≠ crash/power-loss durability** (F-02 stands; no crash testing) |
| Regression / behavioral matrix | matrix §16 (all Stage-2 requirements pass independent Stage-2 review) | L1–L9 below (§7) |

**Suite verification (current tree, no execution):** `tests/Andirit.LatticeDb.Tests` = 10 source files, **52 attributed xUnit tests** (`[Fact]`/`[Theory]`) — exactly the count recorded by the accepted receipts (FINAL-Q1/SQ04-Q1: 52 passed on Windows). The original 38 Stage-1 tests are retained within it, satisfying the "original 38 plus additions" retention requirement. **Windows evidence covers behavior, not platform**: nothing in the Windows receipts qualifies Linux; WSL2/Docker are excluded absent an explicit amendment (per the S2.5 entry decision §7 and work-package plan WP-S25-2).

## 3. Current Linux CI capability assessment (Route A inspection)

Inspection of `.github/workflows/ci.yml` at the reviewed commit:

| Route-A requirement | Assessment |
| --- | --- |
| Genuine Linux runner | **Yes** — `runs-on: ${{ matrix.os }}` with `ubuntu-latest` in the `[windows-latest, ubuntu-latest, macos-latest]` matrix (hosted Ubuntu VM; `fail-fast: false`) |
| Executes the current expanded suite | **Yes** — dedicated step: `dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Release --no-restore` (whole suite, coverage gates `Threshold=70` line+branch) |
| Required .NET version | **Yes** — `actions/setup-dotnet@v5` with `dotnet-version: 10.0.x` |
| Release configuration | **Yes** — `-c Release` on every test step |
| Logs and failure output preserved | **Yes** — GitHub retains run logs; test console summaries distinguish `Passed! / Failed! / Skipped` per suite; failures fail the job with full output; `upload-artifact@v4` (`if: always()`) preserves coverage artifacts |
| Environment metadata captured | **Adequate** — hosted-runner job logs include the runner image/OS and setup-dotnet output; the workflow does not print an explicit `uname`/SDK dump (optional enhancement, **not required**, see below) |
| Reproducible evidence artifacts | **Yes** — run id + `head_sha` identify the execution; logs and coverage artifacts are downloadable and can be hash-sealed into the qualification evidence package |
| Skipped vs passing distinction | **Yes** — VSTest console summary reports skipped counts; `fail-fast: false` keeps all suites reporting |
| Independent review of receipts | **Yes** — the reviewer verifies run id ↔ `head_sha` ↔ tree binding, suite counts, and artifact hashes without access to the producer's machine |

**Necessary CI configuration changes: none.** Optional, explicitly-not-required enhancements (would need their own authorization if ever wanted): a `workflow_dispatch` trigger, an explicit environment-dump step, and a TRX logger. The selected route proceeds **without them**.

**Trigger limitation (the only gap).** The workflow triggers on `push` to `main` and on `pull_request` only. Since nothing has ever been pushed in this work stream (0/71), **no run exists for the current expanded suite** — an owner-authorized remote publication is the prerequisite for Route A execution (§6).

## 4. Execution-route comparison

| Criterion | Route A — GitHub-hosted Ubuntu (existing workflow) | Route B — owner-provided native Linux runner |
| --- | --- | --- |
| Contract compliance | Meets matrix §14 verbatim ("Linux CI executes native tests … CI pipeline logs") with **zero modification** | Meets the *register's* "expanded native suite with logs" wording; the matrix's "CI pipeline logs" phrasing fits less directly (would be native-runner logs) |
| Genuine Linux execution | Yes — hosted Ubuntu VM | Yes — physical/VM Linux |
| Reproducibility | High: pinned commit via `actions/checkout@v4`; hosted image versions recorded in run logs; deterministic test invocation | High if disciplined: bundle/checkout pinned by hash; environment must be captured manually |
| Evidence credibility | Hosted, tamper-resistant run records bound to `head_sha`; artifacts retained by GitHub; reviewer-verifiable | Credible only with rigorous runner metadata + hash discipline; no third-party attestation |
| Setup complexity / cost | **Low** — workflow exists; publication is the only new step | **Medium/high** — provision a Linux host/VM, install SDK, transfer source, script evidence capture |
| Review independence | Receipt-based: reviewer verifies run binding + counts + hashes; can request a re-run; hosted records are producer-independent | Can be **strongest**: reviewer may execute directly on the provided runner |
| Operational risk | Publication exposure (§5); run retention limits (mitigate by promptly downloading + sealing evidence); hosted-image drift over time | Environment drift/undeclared dependencies; single-machine evidence |
| Required authorization | Separate explicit publication + execution authorization (§5) | Separate execution authorization naming the runner and transfer method |

**Recommendation: Route A.** The existing GitHub-hosted Ubuntu workflow meets every authoritative requirement without modification, matches the acceptance-matrix evidence class ("CI pipeline logs") exactly, minimizes setup and cost, and produces independently reviewable, commit-bound records. Route B is retained as a fallback (e.g., if branch protections or publication constraints block Route A) and as an independence-strengthening supplement, not a replacement.

## 5. Remote publication authorization boundary (Route A)

The local `main` is **71 commits ahead** of the recorded `origin/main` (`0 behind`); nothing has ever been pushed in this work stream. Any remote contact is **forbidden in this assessment**; the following boundary defines what a future execution authorization must specify.

| Question | Determination |
| --- | --- |
| Dedicated qualification branch appropriate? | **Yes — preferred**: push the exact qualification commit to a dedicated ref (e.g., `refs/heads/qualification/s25-2-linux` at `7457284`) and open a **pull request** to `main`. The `pull_request` trigger then runs the matrix on the PR merge ref. Avoids writing to canonical `main` and allows review before merge. |
| Which commits must be published? | The qualification commit `7457284…` and its full ancestry (a push publishes all 71 commits — all are committed, accepted documentation/evidence/test work; **no restricted or unfinished material is exposed**: the lineage contains only recorded decisions, reviews, evidence, and the bound fixtures). |
| Branch protections | **Cannot be verified without contacting the remote (not authorized here).** The execution commission must confirm at execution time; if `main` is protected against direct pushes, the PR route is unaffected. |
| Can CI run against the exact qualification commit? | **Yes.** For the PR route, the run's `head_sha` is the branch head `7457284…`; because `origin/main` is a **strict ancestor** (0 behind), the PR merge ref's tree is exactly `fba37945…` — the tested tree is the qualification tree. This tree-equality argument must be recorded in the evidence. Optionally, merging the PR afterwards yields a `push`-triggered run on the exact commit (strongest binding). |
| How the run is tied to commit/tree | Record run id + `head_sha` + `head_repository` from the GitHub API/UI, plus the locally computed tree `fba37945…`; the reviewer re-derives the tree from the commit and confirms the tested-suite counts. |
| How logs/artifacts are retrieved and preserved | Download the run logs and coverage artifacts promptly (retention is limited), hash them, and seal them into the qualification evidence package (self-excluding manifest) alongside the L1–L9 receipts. |
| How accidental publication is avoided | Explicit refspec only (`git push origin 7457284…:refs/heads/qualification/s25-2-linux`), never force-push, never wildcard push, no other branches touched; the authorization names the destination ref, the exact commit, and the operation; direct `main` push is the **fallback** (A2) only if the PR route is impracticable. |

**Boundary rule (binding):** remote publication and CI execution are **separate, explicit owner authorizations**. This decision neither pushes, nor opens a PR, nor triggers CI, nor fetches.

## 6. SQ-05 native Linux qualification matrix

| # | Matrix item | Verification content | Evidence |
| --- | --- | --- | --- |
| **L1** | Native environment | Runner image/OS, kernel, architecture (x64), .NET SDK/runtime versions (`10.0.x`), filesystem (ext4), temp-path behavior | Run "Set up job" log section + explicit environment capture from the run logs |
| **L2** | Expanded test suite | Execute the **entire current `Andirit.LatticeDb.Tests` suite (52 tests)**; record exact passed / failed / **skipped** counts per suite; zero failed expected; any skip is recorded and assessed (not silently accepted) | Run test-step log; console `Passed!/Failed!` summary; coverage artifact |
| **L3** | Persistence and reopen | Durable-reopen and reconstruction tests within the suite (N-equivalents; S23 lifecycle reopen) pass on Linux | Suite log + named test classes |
| **L4** | Refusal and corruption | `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`, `Store_RejectsOccurrenceUnitsWithoutMeta`, `S23Wp5NamespaceRefusalTests` and the suite's corruption/stale-refusal tests pass on Linux | Suite log + named test classes |
| **L5** | Publication authority | S23 native authority tests (authority envelope, orphan-owner correspondence, stale/foreign-handle refusal) pass on Linux | Suite log + named test classes |
| **L6** | Recovery and lifecycle | NEW-01 recovery-projection and managed-recovery tests pass on Linux; **managed injection ≠ crash/power-loss** (F-02; no crash testing; no durability claim) | Suite log + named test classes |
| **L7** | Platform behavior | Compare Linux vs Windows: file locking, WAL behavior, memory mapping, path separators/case semantics; record a platform-behavior matrix; **no production changes** to reconcile differences — differences are recorded, and any behavioral divergence is a finding for a separate decision | Behavior-matrix section of the qualification report |
| **L8** | Evidence and provenance | Exact commit `7457284…` / tree `fba37945…`, run id + `head_sha`, commands, environment, exit codes, artifact hashes; self-excluding SHA-256 evidence manifest; prompt download of logs/artifacts | Evidence package + manifest |
| **L9** | Regression and boundary | Confirm no production/test changes are needed for Linux execution (`git diff` empty for `src/`); identify any test that cannot execute on Linux and record the acceptance consequence; **environmental incompatibility is investigated before being classified as a production defect** | Qualification report + `git diff` receipt |

## 7. Independent qualification and owner gates (four separate gates)

| Gate | Owner/commission | Required evidence |
| --- | --- | --- |
| **G1 — WP-S25-2 execution authorization** | Owner authorization naming: the selected route (A), the exact remote publication operation (destination ref, exact commit `7457284…`, explicit refspec, no force), the CI execution, and the evidence retrieval/preservation plan; confirms the tree-binding argument (§5) and branch-protection handling | Authorization record (documentation) |
| **G2 — Genuine Linux qualification execution** | Publication + PR + CI run execution strictly within G1; prompt download of logs/artifacts; L1–L9 receipts assembled into a self-excluding evidence package | Run id + `head_sha` + tree binding; full run logs; per-suite `Passed!/Failed!/Skipped` counts (52 expected, 0 failed); coverage artifact hashes; L7 behavior matrix; L9 `git diff` receipt; manifest |
| **G3 — Independent evidence assessment** | Reviewer ≠ producer: verify run binding (head_sha/tree), suite counts and skip accounting, L1–L9 coverage, manifest integrity, historical-evidence preservation; classify findings (blocking: suite failure, binding mismatch, unrecorded divergence, unauthorized changes) | Independent assessment report + manifest re-verification |
| **G4 — WP-S25-2 owner acceptance** | Owner decision on the qualified SQ-05 result (bounded accepted scope, carried qualifications, platform-matrix limitations) | Owner decision document |

**A successful CI job is supporting evidence, never automatic owner acceptance.** If a normative Linux behavior fails: preserve the failure verbatim, stop the affected claim, and require a **separate remediation decision** — production code is not silently altered and assertions are not relaxed. Environmental incompatibilities are investigated (L9) before any defect classification.

## 8. Binding qualifications

**Carried forward unchanged:** the Windows-x64 accepted baseline (all S2.3/S2.4 behaviors); serialized single-writer model (no MVCC); managed recovery ≠ crash/power-loss durability (F-02); restricted real-authority cross-unit union (F-01); same-harness review limitations; Q1-Q1/Q1-Q2, S-01, Q2-Q01/Q2-Q02, O-Q2-01, Q-I01, Q-I08, F-OBS-Q1-01/Q1-03, Q2-OBS-01, S1/S3/S4 observations; **SQ-04 satisfied with qualifications under the amended contract** (`adr-ci1-01-ci1a-s2-5-sq04-amend-owner-01.md`; eight experimental codecs excluded; D5 refusal operative); S-01 independent integrated proof (WP-S25-3, open); D3-full and applicable D4/D5 (open); WP-S25-4 final-integration prerequisites (open). **D4.5 and full S-02 remain Stage 6. WP-S24-3 remains optional, deferred, unauthorized.** No public Find Usages, cross-target implementation, schema migration, dual-write, or pagination optimization.

**Linux-specific qualifications to record at G4 (anticipated, not yet earned):** hosted-runner independence is receipt-based (reviewer verifies binding; hosted records are producer-independent but not reviewer-executed); hosted-image versions drift over time (recorded per run); results apply to the tested runner image and SDK, not to a full platform certification (Stage 6).

## 9. Formal route disposition

**`SQ-05 ROUTE READY — OWNER EXECUTION AUTHORIZATION REQUIRED`.**

- **Recommended execution route:** **Route A — GitHub-hosted Ubuntu**, using the existing `.github/workflows/ci.yml` **without modification** (genuine Linux runner; full 52-test expanded suite; .NET 10.0.x; Release; retained logs/artifacts; commit-bound records).
- **Required changes:** none to CI, production, or tests.
- **Required publication authorization:** a separate, explicit owner authorization naming the destination ref (`refs/heads/qualification/s25-2-linux`, PR to `main` — preferred; direct `main` push as fallback A2), the exact commit `7457284…`, the explicit refspec, and the evidence-retrieval plan. Publication exposes only committed, accepted material.
- **Qualification matrix:** L1–L9 (§6).
- **Independent-review requirements:** G3 (§7) — reviewer ≠ producer, binding + counts + manifest verification.
- **Blocking prerequisites:** only the G1 execution/publication authorization; no technical blocker exists.
- **Proposed next commission:** `ANDIRIT-CI1A-S2.5-WP-S25-2-EXEC-OWNER-01` — the G1 execution authorization (documentation-only), followed by G2 execution, G3 independent assessment, and G4 owner acceptance as separate gates.

**The actual Linux qualification is NOT authorized by this decision.**

## 10. Proposed next commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.5-WP-S25-2-EXEC-OWNER-01` — a bounded owner execution authorization that: (1) authorizes the exact remote publication (destination ref + commit + refspec per §5) and the CI execution on the hosted Ubuntu runner; (2) requires the L1–L9 evidence receipts and prompt log/artifact preservation; (3) requires the G3 independent assessment; and (4) leaves G4 owner acceptance to a further decision. **Not authorized by this decision:** the publication itself, any PR, any CI trigger, any Linux execution, any WP-S25-2 owner acceptance, S-01 proof, Stage-2 acceptance, Stage 6, public Find Usages.

## 11. Explicit successor authorization status

- **WP-S25-1: closed. SQ-04: satisfied with qualifications under the amended contract.**
- **WP-S25-2: route fixed (`SQ-05 ROUTE READY — OWNER EXECUTION AUTHORIZATION REQUIRED`); execution, publication, and owner acceptance NOT authorized.**
- **WP-S25-3: planned, NOT authorized. WP-S25-4: planned, NOT authorized.**
- **Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Pagination/index optimization: not authorized. Migration/dual-write: forbidden (D5).**
- No production or test change, CI change, remote contact, or new public API is authorized by this decision.

## 12. Repository discipline

Only this decision document and the additive WP-S25-2 status-note update to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No branch, additional worktree, fetch, push, PR, or CI trigger is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.