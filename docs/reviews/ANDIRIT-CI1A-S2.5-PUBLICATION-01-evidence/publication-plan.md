# Repository publication plan — Stage-2 baseline

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-PUBLICATION-01.md`.
Preparation only. **No push, merge, tag or NuGet publication is authorized by this commission.**

## 1. Starting remote state (verified at preparation, 2026-11-22)

| Ref | Current value |
| --- | --- |
| `origin/main` | `48370711a6247b972dfc83628618924e864be374` (strict ancestor of local `main`) |
| local `main` | `a5e861b86ef4487c46be6724e7ac7054248263eb` (23 commits ahead of `origin/main`) |
| `origin/qualification/ci1a-s25-sq05-linux` | `7457284451dff8ddb0417f83ecc19097103316fc` (SQ-05 evidence anchor; PR #6 head — **merged**, GH-1) |
| `origin/qualification/wp-s25-4-sq05-linux-r2` | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` (corrected-revision qualification anchor; PR #7 head) |
| PR #6 | MERGED (fast-forward, GH-1; reconciliation scheduled) |
| PR #7 | **OPEN**, head `eafd632` (**an ancestor of local `main`**) |
| Existing remote tags | `v0.2.0-preview.5` → `3d59e461…` (2026-09-24); **`v0.2.0-preview.6` → `f20b77a9b9637abf825045b3f2a307d1c0253a5d` (2026-09-26, "chore: release 0.2.0-preview.6 with test-to-production mapping")** |

## 2. Version-identity decision (required before publication)

The `Version` in `Directory.Build.props` is **`0.2.0-preview.6`** at the accepted head — the **same
version string as the 2026-09-26 release** (`f20b77a9`, 293 commits behind the accepted head,
tagged `v0.2.0-preview.6`). All Stage-2 work sits under `## Unreleased` in `CHANGELOG.md`; the
`## 0.2.0-preview.6` changelog section still holds the September content. Publishing the accepted
baseline **as** `0.2.0-preview.6` would therefore either reuse a tag that points at a different
commit or force-move the existing tag.

Feed-collision check (2026-11-22): none of the six package IDs
(`Andirit`, `Andirit.Abstractions`, `Andirit.CSharp`, `Andirit.LatticeDb`, `Andirit.Testing`,
`Andirit.TreeSitter`) exists on nuget.org — no duplicate-version collision on the public feed;
the September release was a repository release (tag + changelog), not a verified feed
publication.

**Recommended decision (owner authorization required): publish the accepted Stage-2 baseline as
`0.2.0-preview.7`.** Consistent with the repository release convention (version bumped in
`Directory.Build.props` in a `chore: release …` commit at release time; matching tag; dedicated
CHANGELOG section), preserves the existing tag semantics, and gives the accepted baseline a
distinct identity. Commands (executed only under the authorization):

```powershell
# version bump (build metadata only; no production behavior change)
(Get-Content Directory.Build.props) -replace '<Version>0\.2\.0-preview\.6</Version>','<Version>0.2.0-preview.7</Version>' | Set-Content Directory.Build.props
# CHANGELOG: add a `## 0.2.0-preview.7` section above `## 0.2.0-preview.6` with the release-notes-draft.md content, leaving `Unreleased` empty
git commit -m "chore: release 0.2.0-preview.7 (Stage-2 baseline)"
```

Alternative (not recommended): publish at `0.2.0-preview.6` with a non-conventional tag name —
leaves the tag/package-version identity inconsistent and risks collision on any non-public feed
where the September packages were pushed.

**Consequence:** the final pack must be re-run at the decided version before the NuGet push; the
smoker in this commission is version-parameterized and its re-run is a required publication gate
(§4 step 5). The smoke evidence in this package was produced at the distinct smoke version
`0.2.0-preview.6-smoke.publication01` and remains valid as pack-consume-load evidence for the
accepted tree.

## 3. PR #7 impact assessment (explicit — avoiding the GH-1 repeat)

PR #7's head `eafd632` is an ancestor of local `main`. Once `main` is published, GitHub evaluates
PR #7's head as contained in the base branch and marks it **merged automatically** — exactly the
GH-1 failure mode (PR #6 became `MERGED` without a disposition decision). **Therefore the
publication sequence must disposition PR #7 explicitly, before or atomically with the `main`
push, and record it.** The required disposition:

1. Post a closing comment on PR #7 stating: the qualification evidence it anchored is sealed in
   the repository (the G2/G3 evidence packages); the accepted Stage-2 baseline descends from its
   head; the PR is closed by owner disposition in this plan, not left to auto-merge.
2. Close PR #7 (state: closed, not merged).
3. Record the disposition in the publication decision record.

Branch cleanup for `qualification/wp-s25-4-sq05-linux-r2` may then be considered (delete the
remote branch) in the same authorized operation set; the SQ-05 branch
`qualification/ci1a-s25-sq05-linux` follows the GH-1 reconciliation decision and is not touched
here.

## 4. Fast-forward publication steps and exact commands

Prerequisites (all require separate owner authorization): remote write access; the operation set
below approved; no concurrent publisher.

| Step | Command (illustrative, executed only under the authorization) |
| --- | --- |
| 0. Verify pre-state | `git fetch origin` + `git rev-parse origin/main` (must equal `48370711…`; if not, stop and re-plan) |
| 0b. Version decision | §2 — `chore: release 0.2.0-preview.7` commit on `main` (recommended) |
| 1. Publish main (fast-forward) | `git push origin main` (local `main` = accepted Stage-2 head + release commit; fast-forward by construction) |
| 2. PR #7 disposition | comment + close PR #7 (§3) — executed **before step 1** if the authorization orders it that way, or immediately after with the comment referencing this plan |
| 3. Optional branch cleanup | `git push origin --delete qualification/wp-s25-4-sq05-linux-r2` (after step 2) |
| 4. Release tag | `git tag -a v0.2.0-preview.7 -m "Andirit Stage 2 baseline (accepted with qualifications)"` + `git push origin v0.2.0-preview.7` — **not** `v0.2.0-preview.6` (tag exists; see §2) |
| 5. Final pack + smoke | `dotnet pack Andirit.slnx -c Release --output <feed>` then `dotnet run -p smoke/TreeSitterPackage/TreeSitterPackage.csproj` with `SMOKE_FEED` pointed at the feed (re-run at the decided version; exit 0 both) |
| 6. Post-push CI gate | `push`-triggered workflow run on the published head must be green (all four jobs) before the NuGet step |
| 7. NuGet publication | `dotnet nuget push *.nupkg --source <feed> --api-key <key from secret store>` for the six packages (and `.snupkg` if the feed accepts symbols) — **key material never enters the repository** |

## 5. Required owner authorizations (separate decisions)

1. **Version decision** — `0.2.0-preview.7` recommended (§2), including the release commit.
2. **PR #7 disposition** (comment + close) — remote write.
3. **Branch cleanup** for `qualification/wp-s25-4-sq05-linux-r2` — remote delete (optional).
4. **Main publication** — push `origin main`.
5. **Release tagging** — `v0.2.0-preview.7` creation + push.
6. **NuGet publication** — package push to the chosen feed (feed + key handling).
7. **Post-publication gate** — green `push`-triggered CI run and re-run pack-consume-load smoke at
   the decided version.

## 6. Risks and controls

- **GH-1 repeat**: mitigated by the explicit PR #7 disposition (§3) — the plan does not rely on
  GitHub's auto-merge behavior.
- **Version identity**: `0.2.0-preview.6` is already a released (tagged) version; the bump to
  `0.2.0-preview.7` keeps tag, package version and changelog consistent (§2). No production
  behavior changes from the bump.
- **Feed collision**: none of the six IDs exists on nuget.org (checked 2026-11-22); if a
  non-public feed is chosen, verify the version is absent there before the push (NuGet rejects
  duplicate versions).
- **Key hygiene**: no NuGet API key, token, or credential in the repository (scanned clean,
  `sensitive-scan.txt`); keys stay in the publisher's secret store.
- **osx-arm64 smoker gap**: pack-consume-load evidence exists for win-x64 (this commission) and
  linux-x64 (CI run 44746950426); osx-arm64 has only package-content evidence
  (`native-rid-verification.txt`). Recommended mitigation: run the smoker on a macOS runner in
  the publication gate set (recommendation, not a Stage-2 blocker).
- **Preview-version churn**: the accepted head contains work accumulated under `Unreleased` since
  the September release; the release notes restate the qualified boundary so consumers see the
  Stage-2 qualifications, not a claim of general readiness.