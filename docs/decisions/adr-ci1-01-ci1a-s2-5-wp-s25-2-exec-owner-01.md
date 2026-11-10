# ADR-CI1-01-CI1A-S2.5-WP-S25-2-EXEC-OWNER-01 — G1 publication and execution authorization for WP-S25-2 native Linux qualification

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-2-EXEC-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision (G1):** **`SQ-05 PUBLICATION AND EXECUTION AUTHORIZED WITH QUALIFICATIONS`** — confined **exactly** to the future operation specified in §5. Nothing is executed in this commission.

> **Status: RECORDED — OWNER-AUTHORIZED (G1 only).**
>
> This decision authorizes one future operation: publish the pinned qualification commit
> `7457284…` to the dedicated remote branch
> `refs/heads/qualification/ci1a-s25-sq05-linux` on `origin`
> (`https://github.com/jenolaszlo-sketch/andirit.git`) with an explicit, non-force refspec,
> open a **pull request** to `main`, and let the **existing, unmodified**
> `.github/workflows/ci.yml` (as of `7457284`) execute on the hosted Ubuntu runner —
> followed by evidence retrieval. It authorizes **no** PR merge, no force-push, no history
> rewriting, no remote deletion, no workflow change, no Linux work outside the described
> run, and no owner acceptance. The executor must verify the actual remote state before
> publication and **stop** on any of the §5 stop conditions.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `cd3e5166b5c7b4491c150b8e6fbb39dff6499a74` / `59a07552a4ddc63e18163b95f2bd67d3ed0503f8` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` is **0 behind / 72 ahead** (no fetch performed; remote not contacted); nothing pushed |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Authority chain: route decision `SQ-05 ROUTE READY — OWNER EXECUTION AUTHORIZATION REQUIRED` (this commission's immediate predecessor) → this G1 authorization. No unexpected repository state or conflicting authority. No reset/clean/restore/rebase/prune/fetch/push/branch/worktree creation was performed, and the remote was not contacted.

## 2. Selected Linux route (reaffirmed)

**Route A — GitHub-hosted Ubuntu using the existing workflow**, per `adr-ci1-01-ci1a-s2-5-wp-s25-2-route-owner-01.md`: genuine hosted Linux runner; the **entire current `Andirit.LatticeDb.Tests` suite**; .NET `10.0.x`; `-c Release`; retained logs and coverage artifacts; commit-bound (`head_sha`) records. No CI modification is authorized or needed. Route B (owner-provided native runner) remains a documented fallback only.

## 3. Pinned qualification revision (verified — remains correct)

**Pinned target: commit `7457284451dff8ddb0417f83ecc19097103316fc`, tree `fba379453ceb3088cab97141ebae5c4c4978e3d3`** (the SQ-04 amendment decision commit, as proposed by the route decision).

| Verification | Result |
| --- | --- |
| 1. Git objects exist | commit exists; `^{tree}` = `fba37945…` exactly |
| 2. Belongs to the accepted local history | strict ancestor of HEAD (`git merge-base --is-ancestor` → yes) |
| 3. Source/test/CI contents match the accepted qualification scope | `git diff 7457284 HEAD -- src/ tests/ .github/` is **empty** — identical implementation |
| 4. Later commits do not change the qualified implementation | the only commits after `7457284` are the route decision document and this decision's predecessors' plan note — documentation only (158 insertions under `docs/`) |
| 5. The exact 52-test suite exists at the revision | `git grep -E '\[(Fact|Theory)\]' 7457284 -- tests/Andirit.LatticeDb.Tests` → **52** attributed tests |
| 6. Linux native dependency available | `src/Andirit.LatticeDb/Andirit.LatticeDb.csproj` at `7457284` pins `LatticeDbSharp 0.2.0`, which ships `runtimes/linux-x64/native/liblattice.so` |
| 7. No unrelated/unauthorized changes included | the commit's diff (the SQ-04 amendment) is documentation-only; no production/test/CI content |

**Conclusion: the proposed revision REMAINS the correct SQ-05 qualification target. Current HEAD is NOT substituted** — the commits after `7457284` (through `cd3e516` and including this G1 decision) are documentation-only, so the qualified implementation is identical at HEAD; the G2 publication uses the **pinned `7457284`**, exactly as proposed, with no silent substitution. The qualification covers every revision whose `src/`, `tests/`, and `.github/` contents are identical to `7457284` (verified true through the current HEAD at recording time).

## 4. Publication route assessment (dedicated branch + PR)

| Question | Assessment |
| --- | --- |
| Remote repository identity | `origin` = `https://github.com/jenolaszlo-sketch/andirit.git` (recorded locally; **not contacted** in this commission) |
| Source ref | local commit `7457284451dff8ddb0417f83ecc19097103316fc` |
| Destination ref | `refs/heads/qualification/ci1a-s25-sq05-linux` (the commission's proposed name, adopted) |
| Branch name available? | **Execution-time prerequisite** — cannot be verified without remote contact. If the branch already exists with different content → **stop** and request a new decision (§5). |
| Branch protection effects | **Execution-time prerequisite** — cannot be verified locally. The PR route is unaffected by `main`-push protection; PR *merge* restrictions do not block the run (the branch is not merged merely to run qualification). |
| PR trigger supported by the workflow? | **Yes** — `on: pull_request` (unfiltered) is present in `ci.yml` at `7457284`; the matrix (`ubuntu-latest`) runs on PRs. |
| Synthetic merge commit | **Yes** — `pull_request` runs check out `refs/pull/<N>/merge` (a synthetic merge of the PR head into the base). With the recorded `origin/main` a **strict ancestor** of `7457284`, the synthetic merge tree is exactly `fba37945…`; this must be **evidenced at execution time**, not assumed (§7). |
| Executed-tree comparison | Record the PR number, `head_sha` (= `7457284…`), and `merge_commit_sha`; retrieve the merge ref's tree (GitHub API or `git fetch origin refs/pull/<N>/merge`) and assert tree == `fba37945…`. If equality cannot be established, the Linux result **must not** be used as acceptance evidence for the pinned revision (§7). |
| Commits becoming visible | `7457284…` and its 71 ancestors — all committed, accepted documentation/evidence/test work and the bound fixtures; **no restricted or unfinished material** (assessed in the route decision §5). |
| Material unsuitable for publication? | **None identified.** No credentials, secrets, or restricted records exist in the lineage. |
| PR without merging to `main`? | **Yes** — the `pull_request` trigger runs without merging. The branch **must not be merged merely to run qualification**; any merge is a separate, later owner decision. |

**Prohibited (binding):** force-pushing, history rewriting, PR merging, deletion of remote branches, pushing any other ref, and any workflow modification.

## 5. Exact future publication authority (the ONLY permitted operation)

| Element | Authorized value |
| --- | --- |
| Remote repository | `origin` — `https://github.com/jenolaszlo-sketch/andirit.git` |
| Qualification commit | `7457284451dff8ddb0417f83ecc19097103316fc` |
| Qualification tree | `fba379453ceb3088cab97141ebae5c4c4978e3d3` |
| Qualification branch | `refs/heads/qualification/ci1a-s25-sq05-linux` (new; must not pre-exist with different content) |
| Permitted push operation | `git push origin 7457284451dff8ddb0417f83ecc19097103316fc:refs/heads/qualification/ci1a-s25-sq05-linux` — explicit refspec, **no force**, single ref, nothing else pushed |
| Permitted PR | head `qualification/ci1a-s25-sq05-linux`, **base `main`**, title/body referencing this decision; **never merged** under this authorization |
| Permitted CI workflow | `.github/workflows/ci.yml` **exactly as it exists at `7457284`** (verified in §3) — no workflow edit, no re-run with modified workflows, no manual workflow_dispatch (not present in this workflow) |
| Evidence retrieval | Download the run logs and coverage artifacts; hash and seal them into the qualification evidence package |
| **Prohibited operations** | Force-push; history rewriting; deleting or overwriting any remote ref; merging the PR; pushing `main` or any other ref; modifying `.github/workflows/*` or any `src/`/`tests/` file; triggering additional runs; contacting the remote for anything beyond the above |

**Execution-time prerequisites (verify before publication; stop and request a new decision on failure):**

1. **Remote state**: fetch `origin` (authorized at execution time) and verify the recorded `origin/main` is unchanged **or still a strict ancestor** of `7457284…`; if the remote has advanced so that the synthetic merge tree would differ from `fba37945…`, **stop**.
2. **Branch availability**: `refs/heads/qualification/ci1a-s25-sq05-linux` must not already exist; if it exists with any content, **stop**.
3. **Workflow identity**: the workflow at `7457284` must equal the version inspected by the route decision and this decision; any difference → **stop**.
4. **Credentials/permissions**: push and PR creation must succeed with the executor's credentials; authorization failures → **stop** (no workaround, no alternate destination).
5. **No unrelated changes**: the published ref must contain exactly the pinned ancestry; any surprise → **stop**.

A successful G1 permits **only** the explicitly described operations. It does **not** authorize arbitrary synchronization of local `main` with remote `main`.

## 6. G2 native Linux execution procedure (L1–L9)

| Item | Procedure |
| --- | --- |
| **L1 — Native environment** | Capture from the run's "Set up job" section and logs: Ubuntu version, kernel, CPU architecture, .NET SDK/runtime installed by `setup-dotnet`, native `liblattice.so` resolution (the suite's own store opens prove resolution), filesystem characteristics, CI runner identity (image + runner name) |
| **L2 — Expanded test suite** | The CI step runs the **complete current 52-test `Andirit.LatticeDb.Tests` suite** in Release; record exact **passed / failed / skipped / total** counts from the console summary; zero failed expected; any skip is recorded and assessed, never silently accepted |
| **L3 — Persistence/durable reopen** | Covered by the suite's durable-reopen/reconstruction tests; pass/fail recorded per suite |
| **L4 — Schema/corruption refusal** | Covered by `Store_RejectsUnsupportedOccurrenceSchemaOnOpen`, `Store_RejectsOccurrenceUnitsWithoutMeta`, `S23Wp5NamespaceRefusalTests` and stale/corruption-refusal tests |
| **L5 — Publication authority** | Covered by the S23 native-authority tests (authority envelope, orphan-owner correspondence, stale/foreign handles) |
| **L6 — Recovery/lifecycle** | Covered by NEW-01 recovery-projection and managed-recovery tests; **managed fault injection does not prove crash/power-loss durability** (F-02) |
| **L7 — Platform behavior** | Record Linux filesystem/path/locking/WAL observations and differences vs Windows as a behavior matrix; **no production modification** to reconcile differences — divergence is a finding for a separate decision |
| **L8 — Evidence** | Preserve: executed commit `7457284…` / tree `fba37945…`; CI run URL + identifier; workflow revision (ci.yml at `7457284`); runner metadata; **complete test logs**; exit codes; test results; artifact checksums; the behavioral matrix; skips/exclusions; sealed by a self-excluding SHA-256 manifest |
| **L9 — Boundary** | All existing qualifications preserved; **no production modification to accommodate a Linux failure without a separate remediation commission**; environmental incompatibility is investigated before any defect classification |

**Honesty rule:** successful execution of the test suite does **not** automatically prove requirements the suite does not exercise (e.g., multi-process writers, crash/power-loss durability, macOS, full platform certification) — none of those is claimed.

## 7. PR checkout and tree verification (binding)

Five distinct identities must be evidenced and reconciled:

| # | Identity | How evidenced |
| --- | --- | --- |
| 1 | Pinned qualification branch commit | `7457284451dff8ddb0417f83ecc19097103316fc` (this decision §3) |
| 2 | GitHub PR head SHA | PR metadata — must equal `7457284…` |
| 3 | GitHub synthetic merge SHA | PR `merge_commit_sha` (API/UI) |
| 4 | Actual workflow checkout SHA | Run record (`head_sha` for `pull_request` events; the checkout step's ref) |
| 5 | Actual executed source/test tree | The synthetic merge commit's **tree** — retrieved via the GitHub API or `git fetch origin refs/pull/<N>/merge`, then `git rev-parse <merge>^{tree}` |

**Requirement:** the executed tree (5) must equal the pinned qualification tree `fba37945…`, with (1) = (2) = (4). The tree-equality basis (recorded `origin/main` is a strict ancestor of `7457284…`) is an **argument, not evidence**: the executor must verify the actual remote `main` state at execution time (§5 prerequisite 1) and record the merge ref's tree hash. **If equivalence cannot be established, the Linux result must not be used as acceptance evidence for the pinned revision** — a new decision is required.

## 8. Independent review and owner gates (unchanged, with handoffs)

| Gate | Status | Handoff to the next gate |
| --- | --- | --- |
| **G1 — Publication and execution authorization** | **THIS DECISION — `AUTHORIZED WITH QUALIFICATIONS`** | The executor receives: this decision, the pinned commit/tree, the exact operation (§5), the L1–L9 procedure (§6), and the checkout/tree verification requirements (§7) |
| **G2 — Actual Linux execution and evidence capture** | **Pending** (not authorized to start before this decision; now eligible) | The executor delivers the run records (run id/URL, `head_sha`, merge ref tree), complete logs, artifact checksums, L1–L9 receipts, and the sealed evidence package — plus the §7 tree-verification record. **G2 success is not G3 qualification.** |
| **G3 — Independent `S25-SQ05-Q1` assessment** | **Pending** | The reviewer (≠ producer) verifies: run binding (§7 identities), suite counts/skip accounting, L1–L9 coverage, manifest integrity, historical-evidence preservation; classifies findings. **G3 support is not G4 acceptance.** |
| **G4 — `WP-S25-2-OWNER` acceptance** | **Pending** | The owner decision records the accepted SQ-05 scope, the platform-behavior matrix limitations, and all carried qualifications |

**Failure handling:** any normative Linux failure at G2 is preserved verbatim and requires a separate remediation decision — no silent production modification, no relaxed assertions. An execution-time stop condition (§5) returns to the owner for a new decision.

## 9. Formal G1 disposition

**`SQ-05 PUBLICATION AND EXECUTION AUTHORIZED WITH QUALIFICATIONS`.**

The authorization is **confined to the exact future operation specified in §5**: publish `7457284…` to `refs/heads/qualification/ci1a-s25-sq05-linux` on `origin` (explicit refspec, no force), open the PR to `main`, let the unmodified `ci.yml` (at `7457284`) run on the hosted Ubuntu runner, and retrieve/preserve the evidence. Qualifications: (1) the five §5 execution-time prerequisites must all pass before publication, with stop-and-return on any failure; (2) the §7 executed-tree equivalence must be evidenced or the result is unusable for the pinned revision; (3) hosted-runner independence is receipt-based; (4) a successful run is supporting evidence only — G3 and G4 remain separate. The qualification is **not executed** in this commission.

## 10. Binding qualifications

**Carried forward unchanged:** the Windows-x64 accepted baseline; serialized single-writer (no MVCC); managed recovery ≠ crash/power-loss durability (F-02); restricted real-authority cross-unit union (F-01); same-harness review limitations; Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08, F-OBS-Q1-01/Q1-03, Q2-OBS-01, S1/S3/S4 observations; **SQ-04 satisfied with qualifications under the amended contract** (eight experimental codecs excluded; D5 refusal operative); S-01 integrated proof (WP-S25-3, open); D3-full and applicable D4/D5 (open); WP-S25-4 final-integration prerequisites (open). **D4.5 and full S-02 remain Stage 6. WP-S24-3 remains optional, deferred, unauthorized.** No public Find Usages, cross-target implementation, schema migration, dual-write, or pagination optimization.

## 11. Explicit G2/G3/G4 status

- **G2 (execution + evidence capture): AUTHORIZED-ELIGIBLE — not yet executed.** May proceed only under §5's exact operation and prerequisites.
- **G3 (`S25-SQ05-Q1` independent assessment): NOT authorized** — becomes eligible only after G2 delivers its sealed evidence package.
- **G4 (`WP-S25-2-OWNER` acceptance): NOT authorized** — becomes eligible only after G3.
- **WP-S25-3 (S-01): planned, NOT authorized. WP-S25-4 (final integration): planned, NOT authorized.**
- **Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Optimization: not authorized.**

## 12. Repository discipline

Only this decision document and the additive WP-S25-2 status-note update to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No branch, additional worktree, fetch, push, PR, or CI trigger is performed in this commission. The decision commit/tree identities and final working-tree status are reported in the handoff.