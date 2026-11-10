# ANDIRIT-CI1A-S2.5-S25-SQ05-Q1 — independent G3 qualification of the WP-S25-2 native Linux execution

**Commission:** `ANDIRIT-CI1A-S2.5-S25-SQ05-Q1` (`S25-SQ05-Q1 INDEPENDENT QUALIFICATION AUTHORIZED WITH RESTRICTIONS`).
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Reviewed local source:** `df57a2a52bd89ff54e56faaf384ff98d7aa0a9b2` (tree `9d66bb2fb73cfc86faea99acceda4c42a3239876`) — the G2 evidence commit.
**Role:** independent G3 reviewer (distinct session from the G2 executor; same-harness limitation disclosed in §11).
**Final disposition:** **`SUPPORTED WITH QUALIFICATIONS`**.

> The G2 Linux execution evidence is authentic, applies to the exact owner-authorized
> revision, and demonstrates the SQ-05 native behavior requirements. **F-G2-01 is classified
> as a non-blocking SQ-05 qualification accompanied by an unresolved repository CI
> quality-gate issue** — the 70% branch-coverage threshold is a repository CI policy
> (`.github/workflows/ci.yml`), **not** part of the normative SQ-05 acceptance contract, but
> it remains binding for the workflow's own conclusion and must be resolved by a separate
> owner decision. This review grants no owner acceptance and performs no remediation.

---

## 1. Verified preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `df57a2a52bd89ff54e56faaf384ff98d7aa0a9b2` / `9d66bb2fb73cfc86faea99acceda4c42a3239876` — both expected (the G2 evidence commit) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote (recorded) | `origin/main` = `443c1927…`; local `main` **74 ahead** (the G2 evidence commit is local-only, as required) |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

PR #6 and the remote qualification branch were **not** modified; only read-only GitHub inspection was performed. No reset/clean/restore/rebase/prune/worktree/push occurred.

## 2. Independent five-identity Git verification (Q1)

Reviewer probe `verify-identities.ps1` (primary sources: Git objects, `git ls-remote`, GitHub PR/run API — **not** the G2 report's summaries). Receipt: `Q1-IDENTITIES.json` — **14 checks, 0 failed**.

| # | Identity | Independently verified value |
| --- | --- | --- |
| 1 | Pinned qualification commit / tree | `7457284451dff8ddb0417f83ecc19097103316fc` / `fba379453ceb3088cab97141ebae5c4c4978e3d3` (Git objects; strict ancestor of the reviewed HEAD; `src/`, `tests/`, `.github/` identical from pinned to HEAD) |
| 2 | Remote qualification branch HEAD (live `ls-remote`) | `refs/heads/qualification/ci1a-s25-sq05-linux` = `7457284451dff8ddb0417f83ecc19097103316fc` |
| 3 | PR #6 head SHA (GitHub API) | `7457284451dff8ddb0417f83ecc19097103316fc`; base `main` at `443c1927…` (the unchanged remote main); state **OPEN** |
| 4 | Workflow run `head_sha` (GitHub API, run 42744298084, event `pull_request`, workflow `ci`) | `7457284451dff8ddb0417f83ecc19097103316fc`; conclusion `failure` (F-G2-01) |
| 5 | GitHub synthetic merge commit (`refs/pull/6/merge`, fetched read-only) | `331ad6062908458665a40ca0c777b79507c52709`; **tree `fba379453ceb3088cab97141ebae5c4c4978e3d3`** — equals the authorized qualification tree; the merge contains the pinned commit |
| 6 | Workflow file identity | blob `be48426f…` identical at the pinned revision, HEAD, and index (exact-byte SHA-256 `2c650722…`) |

**G1 §7 binding-condition analysis (no waiver).** Identities 1–4 are the **same commit** (`7457284…`) — the pinned-commit identity conditions hold exactly. Identity 5 (the PR checkout) is GitHub's synthetic merge commit — a different commit **by design**, for which G1 §7 imposed precisely one condition: *"the executed tree (5) must equal the pinned qualification tree."* That condition is **independently evidenced** (merge tree = `fba37945…`, byte-identical contents; the merge contains the pinned commit; the base is the unchanged remote main). The tree-equality "argument" flagged by G1 was **converted into evidence**: the merge ref was fetched and its tree hash recorded. **No commit-identity condition is waived and no conflict exists.**

## 3. Genuine native Linux execution (Q2 — verified from primary records)

The reviewer **freshly re-downloaded** the hosted Ubuntu job log from GitHub and verified it **byte-identical** to the committed G2 copy (SHA-256 match), then extracted the results from actual test output:

| Field | Independently verified value |
| --- | --- |
| Environment | **Ubuntu 24.04.5 LTS** (hosted image `ubuntu-24.04`, version `20261101.360.1`), **x64**, .NET **SDK 10.0.403** / runtime 10.0.14 |
| Native engine | `LatticeDbSharp 0.2.0` pinned at the qualification revision (ships `runtimes/linux-x64/native/liblattice.so`); resolution and exercise **proven by the passing store-opening/publication/reopen tests** on Linux |
| Configuration | Release (`-c Release`); **no test filter** in the CI LatticeDb step (complete suite) |
| **LatticeDb result** | **`Passed! - Failed: 0, Passed: 52, Skipped: 0, Total: 52`** — from the run's own console output |
| Other suites (same Linux run) | core **345/0**, CSharp **353/0**, TreeSitter **157/0** — all green on Linux |
| Skips/exclusions | **Zero skipped**; no `--filter`; no environment-driven exclusions |

Genuine GitHub-hosted Linux execution — not Windows, WSL2, or Docker. Receipt: `Q2-Q4-EXECUTION-COVERAGE.json` (17 checks, 0 failed); fresh log copy committed (`gh-job-129592604459-reviewer-copy.log`).

## 4. L1–L9 qualification matrix (independent assessment)

| # | Requirement | Classification | Basis |
| --- | --- | --- | --- |
| L1 | Native environment | **Independently supported** | Ubuntu 24.04.5 LTS x64; SDK 10.0.403; hosted runner identity; verified from the freshly downloaded log |
| L2 | Expanded suite (52 tests) | **Independently supported** | 52/0/0 from actual test output; no filters/skips; suite count matches the pinned revision's attributed tests |
| L3 | Persistence / durable reopen | **Independently supported** | `LatticeCodeGraphStoreTests` (20), `OccurrenceStage1NativeTests` (10), `S23LifecycleQualificationTests` (CSharp suite) — all green on Linux |
| L4 | Schema / corruption refusal | **Independently supported** | `OccurrenceStage1NativeTests` (schema/version), `S23Wp5NamespaceRefusalTests`, `OccurrenceRemediationNativeTests`, `S24SurfaceRefusalMappingTests`, `HistoricalStorageIdentityTests`, `LatticeDbPublicApiContractTests` — green on Linux |
| L5 | Publication authority | **Independently supported** | `S23NativeAuthorityTests` + `S23OccurrenceOwnershipRegressionTests` (CSharp suite, 353/0 green on Linux) |
| L6 | Recovery / lifecycle | **Supported with qualifications** | `New01RecoveryProjectionTests` (7) green on Linux; **managed fault injection only — F-02 preserved (no crash/power-loss durability)** |
| L7 | Platform behavior | **Supported with qualifications** | Suite-level Linux/Windows result parity observed; native mirror exercised (`NativeGraphMirrorTests`, 5). Internal locking/WAL/mmap mechanics are **not directly instrumented** — recorded as an observation limit, not a claim |
| L8 | Evidence / provenance | **Independently supported** | Fresh log byte-identical to the committed copy; run/PR/commit identities from primary sources; G2 manifest 18/0; artifact hashes re-verified |
| L9 | Architecture boundary | **Independently supported** | `git diff` empty for `src/`, `tests/`, `.github/` from the pinned revision to HEAD; no production/test/CI/schema/public-contract modification |

**Not exercised / not claimed:** multi-process writers, crash/power-loss durability, macOS, full platform certification, performance contracts, internal locking/WAL/mmap mechanics beyond suite-observed behavior.

## 5. Independent F-G2-01 assessment (Q4)

**Independent recomputation from the raw committed artifact** (`coverage-latticedb/coverage.cobertura.xml`, downloaded from run 42744298084 and byte-verified): root **line-rate 87.01%** (838/963), **branch-rate 65.77%** (296/450) — an independent per-line recomputation (every `condition-coverage` and `hits` attribute re-aggregated) agrees within 0.3pp (66.07%/87.02%; the raw document duplicates the main partial class `Andirit.LatticeCodeGraphStore` across its source documents, and the root aggregates the deduplicated set). Reaching the configured 70% branch gate requires **19 more covered branches** (315 of 450). The gate itself is configured in `ci.yml` on the LatticeDb step: `/p:Threshold=70 /p:ThresholdType=line%2cbranch` with `Include=[Andirit.LatticeDb]*`. The identical threshold error was re-verified in the committed **Windows** and **macOS** log extracts — **platform-independent, not a Linux behavior issue**.

### Required classification

| Question | Determination |
| --- | --- |
| **A. Native Linux functional qualification** | **Satisfied.** The required Linux behavior executed successfully (52/0/0 on the hosted Ubuntu runner; native engine exercised; identities bound to the authorized revision). |
| **B. Repository-wide CI quality gate** | **Not satisfied — and pre-existing.** The workflow's configured quality policy (70% branch coverage on the LatticeDb step) fails on **all three OSes**; the last green hosted run predates the current suite. This is a genuine, binding CI quality-gate failure of the repository — it must **not** be silently waived, and the PR's red conclusion must stand until the owner resolves it. |
| **C. SQ-05 normative acceptance** | The SQ-05 contract (acceptance matrix §14: *"Linux CI executes native tests … CI pipeline logs, before S2.5 acceptance"*; register SQ-05: expanded native suite with logs + platform behavior matrix) requires **successful native test execution with independently verifiable logs and a platform-behavior record**. It does **not** contain a coverage-percentage requirement — the 70% branch gate is repository CI policy, not SQ-05 acceptance criteria. |

**Classification of F-G2-01: a non-blocking SQ-05 qualification accompanied by an unresolved repository CI quality-gate issue.** Explicitly **not** classified as: a blocking SQ-05 qualification violation; and **not** a mere observation to be silently waived — the gate remains binding for the workflow conclusion and is carried as a restriction until the owner resolves it. No threshold reduction, new exclusion, test skipping, workflow change, or unauthorized rerun was performed or authorized.

## 6. Coverage-policy and SQ-05 contract analysis

The 70% line+branch threshold lives in `ci.yml` (repository CI policy), introduced for the Stage-1-era suite and unchanged since; the suite has since grown (38 → 52 tests) and the measured branch coverage at the qualified revision is 65.77% (296/450; 19 short). Nothing in the SQ-05 normative text (matrix §14, register SQ-05, boundary §8 S2.5) references coverage percentages. **Contractual conclusion:** SQ-05's acceptance criterion is the demonstrated native execution of the complete suite with reviewable logs — which is met — while the coverage gate is a **separate repository quality policy** whose adjustment belongs to the repository owner. This is not an ambiguity requiring owner *interpretation*; it is a policy decision requiring owner *action*.

**Remediation analysis (recommendations only — nothing implemented):**

1. **Narrowest:** a separately authorized coverage-gap investigation plus additional branch tests in the LatticeDb suite (the largest contributor is `Andirit.LatticeCodeGraphStore` at 72% branch; ~19 additional covered branches reach the gate).
2. **Alternative:** a justified coverage-policy clarification by the owner (e.g., explicit per-suite line/branch thresholds set to owner-approved values, or a step-scoped policy for platform-qualification runs) — recorded as an explicit decision, never a silent threshold edit.
3. **Not acceptable:** assertion weakening, test skipping, `[ExcludeFromCodeCoverage]` exclusions, or workflow edits without an owner decision.

## 7. Evidence provenance and manifest integrity (Q5)

Receipt: `Q5-INTEGRITY.json` (**13 checks, 0 failed**). Verified: **G2 manifest 18/0**; **SQ04-Q1 manifest 20/0**; **BIND-01 manifest 24/0** — all byte-verified, with `git status` confirming each package's committed state is pristine. The four downloaded coverage artifacts re-hash to their G2-recorded values. The freshly downloaded Ubuntu log is byte-identical to the committed G2 copy. **The disclosed G2 accidental-seal incident is verified fully reverted**: the SQ04-Q1 `reviewed-head.txt` still records its own sealing state (`bf1af23…`/`f2195c26…`) and the package manifest re-verifies 20/0 — no historical evidence was rewritten. The reviewer's GitHub inspection was read-only; PR #6 and the qualification branch were not modified.

## 8. Evidence provenance classification

Direct GitHub Actions evidence (run 42744298084 records, freshly re-downloaded log); independently verified Git identities (Git objects + `ls-remote` + GitHub API); fresh reviewer analysis (three reviewer-owned probes: `verify-identities.ps1`, `verify-execution.ps1`, `verify-integrity.ps1`; independent Cobertura recomputation); producer-generated evidence (G2 package — verified, not trusted as qualification); reused historical evidence (P13b/H records, prior manifests — integrity-checked); static code/configuration analysis (`ci.yml`, csproj, package assets); unsupported claims (full platform certification, crash/power-loss durability, multi-process, macOS, performance contracts — none made).

## 9. Binding qualifications

Carried unchanged: F-01, F-02, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer-independence limitation; Windows-x64 baseline; serialized single-writer; managed-fault durability limits; restricted real-authority cross-unit union; F-OBS-Q1-01/Q1-03; Q2-OBS-01; S1/S3/S4 observations; **SQ-04 satisfied with qualifications under the amended contract**; S-01/D3-full/D4/D5/WP-S25-4 open; D4.5 and full S-02 (Stage 6); WP-S24-3 (deferred). **New/extended qualifications from this review:** the Linux qualification is restricted to the demonstrated environment (Ubuntu 24.04.5 x64, SDK 10.0.403, hosted runner) and the exercised behavior; hosted-run independence is receipt-based; F-G2-01's CI quality-gate issue remains unresolved and binding for the workflow conclusion.

## 10. Findings and blocking classification

| ID | Requirement | Severity | Expected | Observed | Evidence | Blocking |
| --- | --- | --- | --- | --- | --- | --- |
| **F-G2-01** (classified) | Repository CI quality policy (ci.yml LatticeDb coverage gate) | Significant (CI gate) | Branch coverage ≥ 70% per the configured policy | 65.77% (296/450) on Linux; identical error on Windows/macOS | Raw Cobertura (independently recomputed); Windows/macOS extracts | **Not an SQ-05 blocker; blocking for any "CI workflow green" claim** — separate owner decision required |
| Q4-OBS-01 | Reviewer independence | Info | — | Same agent harness as the G2 executor (distinct session; reviewer-owned probes and primary-source verification) | This report | No — carried limitation |
| Q4-OBS-02 | L7 platform mechanics | Info | — | Locking/WAL/mmap internals not directly instrumented | §4 L7 | No — recorded limit |
| Q4-OBS-03 | Cobertura duplication | Info | — | `Andirit.LatticeCodeGraphStore` class element duplicated across partial-class documents; root aggregation deduplicates | `Q2-Q4-EXECUTION-COVERAGE.json` | No |

No SQ-05 normative violation was found; no blocking SQ-05 finding exists.

## 11. Final G3 qualification disposition

**`SUPPORTED WITH QUALIFICATIONS`.**

1. The exact qualified-revision identity is independently established under G1 §7 (five identities from primary sources; the synthetic-merge tree condition evidenced, not assumed). 2. Genuine native Linux execution is independently demonstrated (freshly downloaded hosted log, byte-identical to the committed copy). 3. The 52-test suite passes with **no skipped tests**. 4. The applicable L1–L9 behavioral claims are adequately supported (L6/L7 with recorded qualifications). 5. Evidence integrity is established (manifests 18/0, 20/0, 24/0; artifact hashes; the accidental-seal incident verified reverted). 6. **No unresolved normative SQ-05 blocker is found** — the SQ-05 contract requires successful native execution with reviewable logs, which is met. 7. **F-G2-01 is explicitly classified** (non-blocking for SQ-05; unresolved repository CI quality-gate issue requiring a separate owner decision) and is **not silently waived**.

## 12. Recommendation for G4 owner acceptance and the separate coverage-gate decision

1. **Proceed to the separate G4 `WP-S25-2-OWNER` decision**, which may accept SQ-05 as **satisfied with qualifications on this evidence**: the expanded 52-test suite passed on genuine hosted Ubuntu Linux at the exact authorized revision, with the platform-behavior record, the hosted-independence and environment-scope qualifications, and the F-G2-01 restriction carried.
2. **Address F-G2-01 in a separate owner decision** (never silently): either authorize the coverage-gap investigation and additional LatticeDb branch tests (~19 covered branches reach the gate), or approve an explicit coverage-policy clarification. Until then, the PR #6 CI conclusion remains `failure` and any "CI workflow green" claim is blocked.
3. **Decide the disposition of the qualification PR #6 and remote branch** at G4 (retain as the SQ-05 evidence anchor until Stage-2 acceptance; merging or deletion requires its own authorization).
4. **WP-S25-3 (S-01) and WP-S25-4 (final integration) remain planned and unauthorized**; D4.5/full S-02 remain Stage 6.

---

*Additive reviewer evidence only: three reviewer-owned probes, the fresh log copy, four JSON receipts, and this report. Read-only GitHub inspection; no CI execution, no PR/branch modification, no production/test/CI/fixture changes, no push. The final commit/tree and working-tree state are reported in the handoff.*