# ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01

**Date**: 2026-11-20
**Class**: PREPARED COMMISSION — commission definition and execution plan only. **Nothing is executed by this document.**
**Subject**: corrected-revision genuine Ubuntu qualification of the entire current `Andirit.LatticeDb.Tests` suite (75 cases) at the sealed remediation revision `eafd632`, satisfying **M11.4** and **M14.1** from the binding contracts.
**Prepared under**: `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-OWNER-01` §8 (preparation authorized; **publication and remote execution are NOT authorized here**).
**Status**: **PREPARED — EXECUTION AUTHORIZATION REQUIRED (G1-equivalent decision).**

---

## 1. Why this commission exists (scope derivation)

The prior genuine-Ubuntu qualification (WP-S25-2 / SQ-05, G2 run 42744298084 on PR #6) covered the
**52-case** LatticeDb suite at `7457284…`. The F-G2-01 remediation sealed at `eafd632` adds **23**
focused regression cases (13 durable-state + 10 native-mirror), raising the suite to **75** cases
and changing lines 843→900 and branches 305→368 of 466. M11.4 and M14.1 bind to the **current
expanded** suite, so the SQ-05 receipts are now scope-stale. The full contract derivation is in
[`contract-binding.md`](ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-evidence/contract-binding.md);
the pinned-revision facts are in
[`pinned-revision.json`](ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-evidence/pinned-revision.json);
the suite census is in
[`suite-census.txt`](ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-evidence/suite-census.txt).

## 2. Scope and gates (G1/G2/G3 pattern, per the SQ-05 route)

| Gate | Owner of work | Status |
| --- | --- | --- |
| **G1 — publication and execution authorization** | Owner decision (this commission's required antecedent; a new decision document, since the SQ-05 G1 is exhausted by its pinned revision) | **REQUIRED — NOT YET ISSUED** |
| **G2 — genuine Ubuntu execution and evidence capture** | Executor under G1 §exact-operation | Pending G1 |
| **G3 — independent assessment** (`S25-SQ05-R2-Q1` or as named by G1) | Reviewer ≠ producer | Pending G2 |
| **G4 — owner acceptance** (M11.4/M14.1 disposition) | Owner | Pending G3 |

## 3. Pinned production and test content

| Item | Value |
| --- | --- |
| Commit | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` |
| Tree | `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` |
| Ancestry | strict ancestor of the GATE-OWNER-01 decision commit; recorded `origin/main` `48370711…` is a strict ancestor of `eafd632` |
| Workflow | `.github/workflows/ci.yml` blob `be48426f9be77eacf5dc3de925566126eedad03b` — **byte-identical to the workflow at the SQ-05 qualified revision**, so the pinned content runs the same gates unchanged |
| Native dependency | `LatticeDbSharp 0.2.0` → `runtimes/linux-x64/native/liblattice.so` (the suite's store opens prove resolution) |
| Protected scope | `git diff eafd632 <decision HEAD> -- src tests .github` **empty** at preparation |
| Publication content | exactly `eafd632` and its ancestry (all accepted documentation/evidence/test work; no restricted or unfinished material; lineage suitability is re-verified at G2 time as a stop-condition check) |

## 4. Binding requirements to satisfy (re-derived; verbatim quotes in `contract-binding.md`)

1. **M11.4** — §11 row 4, "Cross-platform native tests pass", fixture **L = genuine Ubuntu x64 via
   accepted sealed receipts**; receipt: "Link Linux source/run/log identities"; independent check:
   "Verify accepted G1/G2/G3 commitment and expanded native suite coverage".
2. **M14.1** — §14 row 1, "Entire current expanded `Andirit.LatticeDb.Tests` suite passes on Linux";
   receipt: "G1/G2/G3 chain, Ubuntu runner and per-suite counts, scope matrix"; check: "Confirm
   genuine hosted run identity, pinned tree, expanded suite and observed behaviors from
   owner-bound evidence".
3. **M14.2** — platform-specific behavior (file locking, WAL, mmap differences) documented from
   the same run.

**Suite census (expected at `eafd632`): 75/0/0** — the 75 enumerated cases in `suite-census.txt`
must all pass on the hosted run with **no test filter**, `-c Release`, `.NET 10.0.x` (the
workflow's `setup-dotnet` pin). Supplementary expectations from the same run: core 345/0, C#
372/0 (inclusive of S23 authority/lifecycle/ownership), TreeSitter 157/0; the `pack` step must
succeed. Coverage-step expectations (Linux actuals recorded, Windows values as the reference):
LatticeDb branch **78.96% (368/466)** and line **92.87% (900/969)** ≥ 70; core ≥ 55; CSharp ≥ 75;
TreeSitter ≥ 60; the exact-byte formatting step **exit 0** on the hosted checkout profile. Any
divergence is a finding, not an assumption.

## 5. Authorized genuine Ubuntu environment

**Route A (established, reaffirmed): GitHub-hosted Ubuntu via the existing unmodified workflow** —
per `adr-ci1-01-ci1a-s2-5-wp-s25-2-route-owner-01.md` and the SQ-05 G1 pattern. The matrix's
`ubuntu-latest` job (currently the Ubuntu 24.04 image series) runs `ci.yml` on the PR. This is
fixture-L genuine Ubuntu x64 with accepted sealed receipts.

- **No WSL2 substitution, no Windows-result substitution, no local Linux VM substitute** — none is fixture L. Route B (owner-provided native runner) remains the documented fallback and requires its own G1 text.
- Environment identification to be captured at G2 from the run's "Set up job" section: runner image and identity, Ubuntu version, kernel, CPU architecture, .NET SDK/runtime versions installed by `setup-dotnet`, native `liblattice.so` resolution evidence, filesystem characteristics — mirroring the L1–L9 receipt pattern of the SQ-05 G2 procedure.

## 6. Source provenance, artifacts and expected results

| Requirement | Plan |
| --- | --- |
| Checkout/tree verification (binding) | Five identities: pinned branch commit = PR head SHA = run `head_sha`; executed tree = synthetic merge ref's tree = `823e3c65…` (retrieved via API or `git fetch origin refs/pull/<N>/merge`). If the executed tree cannot be shown equal to the pinned tree, the result **must not** be used for the pinned revision |
| Remote publication | **Separate G1 authorization required.** A **new** branch name is mandatory — `qualification/ci1a-s25-sq05-linux` remains occupied at `7457284…` (retained as SQ-05 evidence until the Stage-2 acceptance decision per `adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md` §12) and must not be force-pushed. Proposed name: `qualification/wp-s25-4-sq05-linux-r2` (G1 names the final value) |
| PR | head = the new branch, base `main`, **never merged** under the authorization; the run executes on the synthetic merge ref |
| Workflow | `ci.yml` exactly as at `eafd632` — no edit, no `workflow_dispatch` (not present), no re-run with modified workflows |
| Raw artifacts | complete run log (all steps, verbatim, including any failure), per-step exit codes, console test summaries (75/0/0 and the supplementary suites), the four coverage artifacts (`coverage.cobertura.xml` per suite), TRX files if emitted, run id/URL, runner metadata; all hashed and sealed in the G2 evidence package under a self-excluding SHA-256 manifest |
| Expected results | everything green (§4); the repository CI conclusion on Ubuntu becomes `success` for the first time — a **consequence to record, not to assume** |
| Failure handling | any normative Linux failure is preserved verbatim and returns to the owner (no silent production modification, no relaxed assertions); environment incompatibility is investigated before defect classification; execution-time stop conditions (§7) return to the owner for a new decision |

## 7. Execution-time stop conditions (G1 must carry these forward)

1. Remote `origin/main` has advanced such that recorded `48370711…` is no longer a strict ancestor of `eafd632` (synthetic merge tree would differ from `823e3c65…`) → **stop**.
2. The proposed destination branch already exists with any content → **stop**.
3. The workflow blob at `eafd632` differs from `be48426f…` inspected here → **stop**.
4. Credential/permission failure for push or PR creation → **stop** (no workaround, no alternate destination).
5. The published ref contains anything other than the pinned ancestry → **stop**.
6. Executed-tree equality (§6) cannot be evidenced → the Linux result is unusable for the pinned revision; **stop** and return.

## 8. Evidence verification and package structure (G2/G3 obligations)

- G2 seals: run records (run id/URL, `head_sha`, merge ref tree), complete logs, exit codes, coverage artifacts, L1–L9 receipts (L1 environment, L2 suite counts/skip accounting, L3 persistence/reopen, L4 schema/corruption refusal, L5 publication authority, L6 recovery/lifecycle with F-02 preserved, L7 platform-behavior matrix with uninstrumented lock/WAL/mmap limits, L8 evidence, L9 boundary), by a self-excluding SHA-256 manifest with exact bytes, lengths and ordinal relative paths.
- G3 independently re-derives: the five identities from primary sources (Git objects, GitHub PR/run API), the suite counts (75/0/0 expected), the coverage actuals against the 368/466 and 900/969 reference, manifest integrity, and historical-evidence preservation (no rewriting of the SQ-05 G2/G3 packages; the retained PR #6 evidence stays untouched).
- Honesty rule (carried from SQ-05): a successful run does not prove multi-process writers, crash/power-loss durability (F-02), other distributions/architectures, or full platform certification — none of which is claimed.

## 9. Explicitly not authorized by this commission

- No push, fetch, PR creation, branch creation, or any remote contact (G2 remains behind its G1 decision).
- No production, test, dependency, formatter, coverage, instrumentation, or CI-policy change of any kind.
- No repeat of the previously qualified Windows executions (the Windows results are inherited as the reference expectation, §4).
- No WP-S25-4 closure, Stage-2 acceptance, Stage-6 initiation, M5.4 work, or owner acceptance of M11.4/M14.1 (G4 remains separate).

## 10. Remaining owner authorization needed to execute

**Exactly one**: a G1-equivalent publication-and-execution decision naming (a) the remote and the
single permitted push (`eafd632` → the new branch, explicit refspec, no force), (b) the permitted
PR (head = new branch, base `main`, never merged), (c) the unmodified `ci.yml` at `eafd632` as the
only workflow, (d) evidence retrieval, (e) the §7 stop conditions, and (f) the §8 package
requirements. After G2 and G3, M11.4/M14.1 disposition returns to the owner (G4); WP-S25-4 closure
and Stage-2 acceptance remain separate decisions.