# ANDIRIT-CI1A-S2.5-RELEASE-01

**Date**: 2026-11-22
**Gate**: RELEASE-01 — execution of the owner-authorized `0.2.0-preview.7` release (tag creation,
tag-triggered NuGet publication, post-publication verification).
**Formal disposition**: **BLOCKED AT PUBLICATION BY AN EXTERNAL TRUST-POLICY MISMATCH** — the
release tag was created and pushed as authorized; the publication workflow's repository-side
gates all passed; the tag-triggered run failed at the OIDC login with HTTP 401 ("no matching
trust policy") and **zero packages were published**. Post-publication feed verification and the
public-feed smoke test are therefore blocked and were not performed. No retry and no version
change was made.

---

## 1. Preflight (all matched)

| Object | Value |
| --- | --- |
| Local / remote main | `6ed05964d2fcc758648b654a01ec706647e52183` (identical, clean worktree) |
| CI run 44964051143 | completed / **success** on `6ed05964` (test windows/ubuntu/macos + pack) |
| Evidence integrity | all seven sealed packages byte-exact at the entry commit (workflow 10 · exec 20 · qual 12 · acceptance 8 · final-owner 4 · m54-owner 5 · m54-b-qual 32 entries) |
| Version availability | `0.2.0-preview.7` absent on nuget.org for all six IDs; tag `v0.2.0-preview.7` absent |
| Publication workflow | `origin/main` carries the corrected `publish.yml` (six csprojs + tag/version gate + inventory gate) |

## 2. Release tag (authorized act, completed)

| Item | Value |
| --- | --- |
| Tag | `v0.2.0-preview.7` (annotated), pushed — the only remote write of this commission |
| Tag object / target | `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → **`6ed05964d2fcc758648b654a01ec706647e52183`** (per owner authorization, not `68a141f6`) |
| Remote | `refs/tags/v0.2.0-preview.7` present; `origin/main` unchanged at `6ed05964` |

## 3. Publication workflow result (run 44965409440)

| Step | Result |
| --- | --- |
| Checkout / Setup .NET | success |
| Validate tag matches release version | **success** (tag↔version gate passed: `v0.2.0-preview.7` = `v` + `0.2.0-preview.7`) |
| Pack (six csprojs at `PackageVersion=0.2.0-preview.7`) | **success** |
| Verify complete package inventory (6 nupkg + 6 snupkg) | **success** |
| NuGet login (OIDC → temp API key) | **FAILURE** — HTTP 401 token exchange: "No matching trust policy owned by user '***' was found." |
| Push packages / Push symbols | skipped |

**Exact publication state: NONE.** Successful packages: **0 of 12**. Failed: the OIDC login —
therefore all twelve intended packages (six `.nupkg` + six `.snupkg` of `Andirit`,
`Andirit.Abstractions`, `Andirit.CSharp`, `Andirit.LatticeDb`, `Andirit.Testing`,
`Andirit.TreeSitter` at `0.2.0-preview.7`) are unpublished; verified by feed probe (no
`0.2.0-preview.7` for any of the six IDs — no duplicate-version conflict, nothing to clean up).
No retry was performed and no version was changed, per the commission.

## 4. Root cause (external; not a repository defect)

The nuget.org trusted-publishing policy predates the repository rename: the last successful
tag publication (run 36234635207, 2026-09-26, `v0.2.0-preview.6`) ran under
`jenolaszlo-sketch/penghou-hetu` (runner paths `…/penghou-hetu/…`; published the pre-rename
`Penghou.Hetu.*` IDs, which still exist on nuget.org at 0.2.0-preview.3/5/6). The rename to
`jenolaszlo-sketch/andirit` was finalized 2026-10-29 (commit `63ee7a2`), so GitHub's OIDC token
now presents the post-rename repository and the existing policy no longer matches. The
repository-side release engineering is correct and is demonstrated by the failed run's passing
tag/version gate, six-package pack and inventory gate.

**Required owner action (external, not performed)**: create/update the nuget.org
trusted-publishing policy for `jenolaszlo-sketch/andirit` + `.github/workflows/publish.yml`
under the nuget.org account matching the `NUGET_USER` secret (policy creator), then re-run
workflow 44965409440 (the existing tag needs no change). Record: `release-state.txt`.

## 5. Post-publication qualification — BLOCKED, not performed

- **Feed availability**: all six IDs at `0.2.0-preview.7` — not published (verified; nothing
  to verify further until publication succeeds).
- **Public-feed clean consumer restore + supported smoke test**: cannot run for unpublished
  packages; **not performed** (no local-feed substitution was used to stand in for the public
  feed — that would misrepresent the qualification).
- **osx-arm64 runtime limitation**: preserved unchanged (pack-consume-load runtime evidence
  remains win-x64 + linux-x64; osx-arm64 content evidence only).

## 6. Qualifications carried

Q-L1…Q-L8, MR-1…MR-4 and Stage 6 deferrals DF-1…DF-8 stand unchanged; the release qualifies
nothing further and broadens no claim. Stage 6 entry readiness remains **READY** (preparation
complete; `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` remains the next implementation commission,
owner-gated); **no Stage 6 implementation was begun** and no historical qualification branch
was deleted.

## 7. Readiness recommendation for the completed release

**NOT READY — blocked on one external action.** Repository-side readiness is fully evidenced
(tag correct and pushed; gates green; inventory reconciled; local packaging qualification
complete at `0.2.0-preview.7`). Once the trust-policy fix is in place, re-running workflow
44965409440 (or re-pushing the same tag — same result) should publish all twelve packages; the
post-publication qualification (feed availability for six IDs + public-feed consumer smoke)
must then be executed and sealed before the release is declared complete.