# ANDIRIT-CI1A-S2.5-PUBLICATION-01

**Date**: 2026-11-22
**Gate**: PUBLICATION-01 — publication qualification of the accepted Stage-2 baseline
(`docs/decisions/adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`), executed per the prepared
commission.
**Pinned implementation**: `eafd632b...` / tree `823e3c65…` (the gate-remediation revision);
local `main` head `a5e861b86ef4487c46be6724e7ac7054248263eb` (Stage-2 acceptance seal), tree
`56b6f931da28769b214d7e826ba6b89e560cb6b2`.
**Formal disposition**: **READY FOR A PUBLICATION COMMISSION** — inventory, metadata, smoke,
native-asset and credential checks pass; the repository publication plan is prepared with exact
commands. **No publication is authorized by this record.**

---

## 1. Role, independence and method

The reviewer derived the publication criteria from the commission and the Stage-2 acceptance
record before executing anything. All verification ran on a pinned exact-byte export of `eafd632`
(`D:\m54b-qual\production`, 4,555 blobs) on Windows x64 (SDK 10.0.401); the genuine-Ubuntu leg
reuses the preserved CI evidence (run 44746950426) rather than re-executing hosted runs.
Independence is method-level (fresh smoke harness invocation, contract-derived expectations,
primary-source receipts). Organizational independence is not established (the same recorded
boundary as prior commissions); same-session sequencing with the commission's preparation is
disclosed.

Method boundary honored: no production source, test, fixture, dependency, formatter, coverage or
CI-policy change; no sealed historical evidence modified; no accepted qualification execution
repeated; no fetch, push, merge, tag or feed operation.

## 2. Execution summary

| Item | Value |
| --- | --- |
| Package inventory | 6 packages at `0.2.0-preview.6` + symbol packages (`package-inventory.md`) |
| Local pack-consume-load smoke (win-x64) | **PASS** — pack exit 0, smoker exit 0, all checks `ok`, `PASS: all six packed Andirit packages, managed providers, durable store, testing conformance and Tree-sitter natives resolved.` (`smoke.log`) |
| Genuine-Ubuntu smoke (linux-x64) | **PASS** — preserved CI `pack` job, run 44746950426; provider versions `+v0.3.5.abi14.p2` / `+v0.23.2.abi14.rem` / `+v0.25.0.abi15.p10` (`job-pack.log`) |
| osx-arm64 smoker | **NOT COVERED** — package-content evidence only; recorded, mitigated in the plan |
| Native RID assets | win-x64 / linux-x64 / osx-arm64 Elixir natives present in `Andirit.TreeSitter` (`native-rid-verification.txt`) |
| Dependencies | `LatticeDbSharp 0.2.0`, `TreeSitter.DotNet 1.3.0`, Roslyn; no retired identities (`smoke.log`) |
| Release metadata | Apache-2.0, repository URL, README packed in each package; deterministic build (`Directory.Build.props`) |
| Credential scan (4,708 blobs) | **CLEAN** — no tokens/keys/passwords; one config-like file, sources only (`sensitive-scan.txt`) |
| Version identity | `0.2.0-preview.6` is **already a released version** (tag `v0.2.0-preview.6` → `f20b77a9`, 2026-09-26, 293 commits behind); none of the six IDs on nuget.org |
| Remote state | `origin/main` `48370711…`; PR #7 OPEN head `eafd632` (ancestor of local main); PR #6 MERGED (GH-1) |

## 3. Findings (None blocking)

- **F-PUB-01 (version identity, resolved in plan)**: the accepted baseline packs at
  `0.2.0-preview.6`, a version already tagged and released in September; Stage-2 work sits under
  `## Unreleased`. The plan requires the owner version decision and recommends
  **`0.2.0-preview.7`** (release commit + tag + CHANGELOG section), with the final pack and smoke
  re-run at the decided version as a required gate (`publication-plan.md` §2, §4 step 5). Feed
  collision checked: none of the six IDs exists on nuget.org.
- **F-PUB-02 (PR #7 auto-merge risk, resolved in plan)**: PR #7's head `eafd632` is an ancestor of
  local `main`; publishing `main` marks PR #7 merged automatically — the GH-1 failure mode. The
  plan dispositions PR #7 explicitly (closing comment + close, before or atomically with the
  push) and records the disposition (`publication-plan.md` §3).
- **F-PUB-03 (osx-arm64 smoker gap, recorded)**: pack-consume-load evidence covers win-x64
  (this commission) and linux-x64 (run 44746950426); the osx-arm64 asset is content-verified but
  not executed. Recommendation: run the smoker on a macOS runner in the publication gate set.
- **F-PUB-04 (feed scope, recorded)**: publication target feed is a decision input; if a
  non-public feed is chosen, verify the decided version is absent there before pushing.

## 4. Deliverables

| Deliverable | Artifact |
| --- | --- |
| Publication qualification report | this document |
| Package and version inventory | `ANDIRIT-CI1A-S2.5-PUBLICATION-01-evidence/package-inventory.md` |
| Smoke-test evidence and manifest | `…-evidence/smoke.log`, `pack.log`, `feed-listing.txt`, `manifest.json` |
| Native dependency resolution | `…-evidence/native-rid-verification.txt` |
| Credential suitability | `…-evidence/sensitive-scan.txt` |
| Release notes | `…-evidence/release-notes-draft.md` |
| Exact publication commands and prerequisites | `…-evidence/publication-plan.md` |
| Stage 6 entry readiness confirmation | `…-evidence/stage6-entry-readiness.md` |

## 5. Qualifications restated (carried, not discharged)

Q-L1 Linux scope restricted to the demonstrated hosted environment (Ubuntu 26.04.1 x64, SDK
10.0.404, hosted runner) — no other generations, distributions or architectures. Q-L2 only
75-case-suite behavior is qualified. Q-L3 managed fault injection only; no crash/power-loss
durability (F-02). Q-L4 locking/WAL/mmap internals uninstrumented. Q-L5 hosted-run independence
receipt-based. Q-L6 Windows-x64, serialized single-writer baseline for non-Linux scope. Q-L7
restricted real-authority cross-unit union (F-01). Q-L8 reviewer-independence limitations
(same-harness; organizational independence unestablished). M5.4 amended-contract limitations
MR-1…MR-4: conservative false-negative behavior; constructed-control evidence does not establish
natural reachability; the exhaustive Roslyn symbol-association proof remains unproven and
unclaimed; the structural universe-membership bound narrows but does not eliminate
identity-substitution residual risk. Stage 6 deferrals DF-1…DF-8 and WP-S24-3 are unchanged.

## 6. Repository publication plan (summary)

Fast-forward publication of local `main` (23 commits ahead of `origin/main`, by construction),
with: (0) pre-state verification; (0b) the owner version decision — `0.2.0-preview.7` release
commit recommended; (1) `git push origin main`; (2) explicit PR #7 disposition (comment + close)
to avoid the GH-1 repeat; (3) optional branch cleanup for
`qualification/wp-s25-4-sq05-linux-r2`; (4) annotated tag `v0.2.0-preview.7` + push (the existing
`v0.2.0-preview.6` tag is not moved); (5) final pack + smoke re-run at the decided version;
(6) green `push`-triggered CI gate; (7) `dotnet nuget push` of the six packages from the
publisher's secret store. Full commands: `publication-plan.md` §4. Required owner authorizations:
version decision; PR #7 disposition; optional branch cleanup; main publication; release tagging;
NuGet publication; post-publication gate. **None is granted by this record.**

## 7. Stage 6 entry readiness

**READY** — preparation complete; `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` remains the recommended
next implementation commission (owner-gated, S6.1 scope). Detail:
`stage6-entry-readiness.md`.

## 8. Disposition and boundary

**READY FOR A PUBLICATION COMMISSION.** The accepted Stage-2 baseline is packable, consumable and
loadable on win-x64 and linux-x64 with correct metadata, native assets and clean evidence; the
repository publication plan is prepared with exact commands and the PR #7/version risks
dispositioned. This record grants no authorization: no push, merge, tag, version bump or NuGet
publication is performed or authorized; no Stage 6 implementation is begun; no accepted
qualification is broadened or discharged.