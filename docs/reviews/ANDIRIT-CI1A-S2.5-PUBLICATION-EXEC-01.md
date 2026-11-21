# ANDIRIT-CI1A-S2.5-PUBLICATION-EXEC-01

**Date**: 2026-11-22
**Gate**: PUBLICATION-EXEC-01 — execution of the owner-authorized publication of the accepted
Stage-2 baseline (per `ANDIRIT-CI1A-S2.5-PUBLICATION-01.md` §4–§6 and the owner authorization of
this commission).
**Release version**: `0.2.0-preview.7` (owner-authorized; `v0.2.0-preview.6` remains the September
release at `f20b77a9`).
**Formal disposition**: **EXECUTED WITH QUALIFICATIONS** — release prepared and verified; PR #7
dispositioned (closed, not merged); canonical `main` fast-forward published; push-triggered CI
green. **NuGet publication, release tagging, qualification branch deletion and Stage 6
implementation were not performed and remain separately gated.**

---

## 1. Preflight (all matched)

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `2aaab9ab3babd493b4a84e954ee16c623f558e73` / `d7795780a638bce12b2cfc3c6b5666abf40b7496` (as expected), `main`, clean tracked state |
| Publication qualification | `ANDIRIT-CI1A-S2.5-PUBLICATION-01.md` + 12-entry evidence package in place |
| Remote ancestry | `origin/main` `48370711…` (strict ancestor); PR #7 OPEN head `eafd632`; qualification branches at prior commits; no unexpected advancement |
| Version availability | none of the six package IDs on nuget.org; `v0.2.0-preview.7` tag not present |
| Evidence integrity | new and prior sealed packages byte-exact at the entry commit |

## 2. Execution summary

| Step | Result |
| --- | --- |
| Release commit | `68a141f67023a9a66a5d2b77c8d93cf3cecb9993` (tree `97993a72`, parent `2aaab9ab`): version bump `0.2.0-preview.6` → `0.2.0-preview.7`, new CHANGELOG section, owner-authorized test golden re-baseline |
| Local gates at the final version | restore 0 · format 0 · **whole-solution tests 0 (949/949/0)** · pack 0 · win-x64 pack-consume-load smoke 0 (`release-verification.md`) |
| Final package verification | six nupkg + six snupkg at `0.2.0-preview.7`; nuspec versions/dependencies consistent; win-x64/linux-x64/osx-arm64 natives present; README packed |
| PR #7 disposition | comment + close **before** the push; state **CLOSED**, `mergedAt: null`, `mergeCommit: null`; head and branch preserved (`pr-disposition.md`) |
| Publication | `git push origin main` → `4837071..68a141f` fast-forward; `origin/main` now equals the release commit (`publication-receipt.md`) |
| Push-triggered CI | run **44953429653** **success** on `68a141f6`: test windows/ubuntu/macos + pack, all green; CI packs the six packages at `0.2.0-preview.7`; genuine-Ubuntu smoke PASS (`publication-receipt.md`, `ci-job-pack.log`) |

Amended once: the initial release commit `530aacec` failed exactly one test
(`TreeSitterEcmaExportShapeTests.PublishedGraphAddsExportsWithoutChangingPreChangeP8Facts`) because
its golden pre-export fingerprints fold the provider/assembly version. Per the owner's amendment
authorization, the two golden values were re-baselined
(typescript `7d067f6c70c20b2cc7db310d1d61b49c87f90eb432eebdee6135b2433b52a664`; javascript
`5e6c797366ef1443dc4c687e481d59537ac8c9378486aebbcf2fa22d7604ac37`) and the release commit amended
**before any push**. Details, the scratch-only probe that computed the values, and the scope
boundary: `release-verification.md` §2.

## 3. Qualifications carried (restated, not discharged)

Q-L1 Linux scope restricted to the demonstrated hosted environment (Ubuntu 26.04.1 x64, SDK
10.0.404, hosted runner) — no other generations, distributions or architectures. Q-L2 only
75-case-suite behavior. Q-L3 managed fault injection only; no crash/power-loss durability (F-02).
Q-L4 locking/WAL/mmap internals uninstrumented. Q-L5 hosted-run independence receipt-based. Q-L6
Windows-x64, serialized single-writer baseline for non-Linux scope. Q-L7 restricted real-authority
cross-unit union (F-01). Q-L8 reviewer-independence limitations (same-harness; organizational
independence unestablished). M5.4 amended-contract limitations MR-1…MR-4. Stage 6 deferrals
DF-1…DF-8 and WP-S24-3 unchanged. This execution broadens no claim and discharges no obligation.

## 4. Repository discipline

Only the release commit (version, CHANGELOG, two test literals) and this record + evidence package
are added, locally on `main`. `src/` and `.github/` are unchanged since the qualified revision
`eafd632` (`git diff eafd632 68a141f6 -- src .github` empty); `ci.yml` (including its `SMOKE_VERSION`
smoker override, which remains `0.2.0-preview.6-smoke.*` by design — an explicit per-run override,
not derived from the package version) is untouched. No sealed historical evidence modified; the
qualification branches are preserved; PR #7 was closed without a merge; the only remote write is the
authorized `main` push. A post-publication local evidence commit follows this report's execution
records and is not pushed.

## 5. Remaining publication gates (separate owner decisions)

1. **Release tag**: `v0.2.0-preview.7` at `68a141f6` (annotated, pushed). **Caution — the existing
   `publish.yml` triggers on any `v*` tag push and would automatically run the NuGet publish job
   (OIDC login → `dotnet nuget push *.nupkg --skip-duplicate` to nuget.org) for all six packages;
   the five `src` csproj packs (not `Andirit.TreeSitter`), without a pack-consume-load smoke gate.
   The tagging commission must decide whether to tag as-is (arming automatic publication) or to
   arm/neutralize the workflow under a separate decision.**
2. **NuGet publication**: if not via the tag trigger, the explicit `dotnet nuget push` route with the
   publisher's secret-store key, plus the pack–consume–load smoke already evidenced.
3. **Branch cleanup** for `qualification/wp-s25-4-sq05-linux-r2` (and the GH-1 reconciliation for
   `qualification/ci1a-s25-sq05-linux`) — separate owner decision.
4. **osx-arm64 smoker** — package-content evidence only; a macOS smoker run in the publication gate
   set remains the recorded recommendation (not a Stage-2 blocker).

## 6. Stage 6 entry readiness

**READY** — unchanged by this execution: the accepted baseline is published, CI is green on the
release commit, and the deferred scope stands. `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` remains the
recommended next implementation commission (owner-gated, S6.1 scope, pinned to `eafd632` lineage).
No Stage 6 implementation was begun.