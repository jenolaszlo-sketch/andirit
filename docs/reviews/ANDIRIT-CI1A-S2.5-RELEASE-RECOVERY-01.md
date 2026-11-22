# ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01

**Date**: 2026-11-22
**Gate**: RELEASE-RECOVERY-01 — recovery of the previously blocked `0.2.0-preview.7` publication
under the corrected NuGet.org Trusted Publishing configuration, plus independent public-feed
qualification and release closure.
**Formal disposition**: **`0.2.0-preview.7 — PUBLISHED AND PUBLIC-FEED QUALIFIED WITH PRESERVED
QUALIFICATIONS`** (all binding gates pass; the qualifications of the Stage-2 acceptance are
carried unchanged).

---

## 1. Preflight (all matched; no stop conditions)

| Check | Result |
| --- | --- |
| Local / remote `main` | `ffd6a04a84867a602cfb577f4d9ceebc7e70bed0` (identical), clean worktree |
| Tag `v0.2.0-preview.7` | annotated, resolves to `6ed05964d2fcc758648b654a01ec706647e52183` |
| Original failure | run 44965409440 attempt 1: OIDC login HTTP 401 after passing tag/version, pack and inventory gates (zero packages published) — sealed in `ANDIRIT-CI1A-S2.5-RELEASE-01-evidence/` |
| Six IDs at `0.2.0-preview.7` | available (not published) — no collision |
| Eight sealed evidence packages | manifest-exact at `ffd6a04` |
| Stage-2 acceptance decisions and qualifications | intact (`stage2-acceptance-owner-01.md`, S1 acceptance, all obligation records) |

## 2. Publication recovery (run 44965409440, attempt 2)

Re-run of the existing workflow with the corrected Trusted Publishing configuration — no tag
recreation, no workflow change, no version change, no credential exposure (the `NuGet/login@v1`
OIDC flow and the unchanged GitHub secret are used; the API key is generated per run and never
logged — the captured logs show `***` masking).

| Gate | Result |
| --- | --- |
| Tag/version consistency | **pass** — "Tag v0.2.0-preview.7 matches release version 0.2.0-preview.7" |
| Pack (six csproj at `0.2.0-preview.7`) | **pass** — 6 nupkg + 6 snupkg |
| Inventory gate (6/6) | **pass** — "Inventory complete: 6 nupkg and 6 snupkg" |
| NuGet OIDC authentication | **pass** (attempt 2) — the corrected trust policy accepted the GitHub OIDC token; temp API key minted |
| Push packages | **pass** — 12 "Your package was pushed." confirmations (6 nupkg + their 6 snupkg) |
| Push symbols | **pass** — all six snupkg "already exists at feed" (graceful duplicate skip via `--skip-duplicate`; confirms symbol presence on the feed) |

**Published: 6 nupkg + 6 snupkg** — `Andirit`, `Andirit.Abstractions`, `Andirit.CSharp`,
`Andirit.LatticeDb`, `Andirit.Testing`, `Andirit.TreeSitter`, all at `0.2.0-preview.7`
(`publish-attempt2.log`, `publish-attempt2-receipt.json`, `publish-attempt2-extract.txt`).
No unintended package identities or versions appear in the log.

## 3. Public-feed qualification (independent)

| Gate | Result |
| --- | --- |
| Availability | all six IDs at `0.2.0-preview.7` HTTP 200 on nuget.org and present in registration indexes; downloaded and hashed (`public-feed-availability.txt`) |
| Metadata | every package: version `0.2.0-preview.7`, license Apache-2.0, authors "Andirit contributors", README packed, correct dependency graph (`public-feed-verification.txt`) |
| Native assets | `Andirit.TreeSitter` carries `win-x64` (1,504,768 B), `linux-x64` (1,426,696 B), `osx-arm64` (1,467,336 B) natives — identical sizes to the qualified inventory (`public-feed-content.txt`) |
| Symbol publication | six symbol-package PUTs confirmed; duplicate-skip confirms presence; flatcontainer snupkg 404 is the modern behavior (Newtonsoft/penghou controls 404 identically) (`public-feed-availability.txt`) |
| Clean consumer restore | isolated fresh package cache, `RestoreSources=https://api.nuget.org/v3/index.json`, no `SMOKE_FEED`, no local feed — all six packages restored from nuget.org only; cache copies byte-identical (SHA-256) to the feed downloads (`restore-public.log`, `cache-listing.txt`, `cache-integrity.txt`) |
| Pack-consume-load smoke (Windows x64) | **PASS** — 40 checks + final `PASS: all six packed Andirit packages, managed providers, durable store, testing conformance and Tree-sitter natives resolved.` (exit 0): managed providers (elixir/typescript/javascript) load and parse; C# integration (Roslyn extraction, host indexing); TreeSitter native loading; LatticeDb schema-2 durable store creation, publication, reopen, native mirror, snapshot integrity; Testing package conformance laws; exact six assembly identities; no retired Hetu dependencies — all at `0.2.0-preview.7` (`smoke-public.log`) |
| Linux x64 runtime | **not obtained in this commission's authorized scope** (no workflow modification and no Linux host available for a public-feed consumer run) — recorded honestly; the pre-publication genuine-Ubuntu qualification (75/0/0 on run 44746950426) predates the release and is not a public-feed consumer evidence |
| osx-arm64 | **limitation preserved**: package-content evidence only; no runtime qualification |

## 4. Release closure

Attempt 2 result (`publish-attempt2-receipt.json`): run 44965409440 — completed / success on
`v0.2.0-preview.7` → `6ed05964`, all steps success. Release identity and URLs:
`release-identity.md`. Qualifications carried unchanged: Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4,
DF-1…DF-8 and all accepted Stage-2 limitations. Stage 6 entry readiness: **READY**
(preparation complete; `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` remains the recommended next
implementation commission, owner-gated). **No Stage 6 implementation was begun.** No production,
test, dependency or workflow modification; no tag deletion/movement/recreation; no version
increment; no force-push; no Hetu package deletion/unlisting/deprecation; no qualification branch
deletion.

## 5. Recommendation

**`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01`** as the next implementation commission (owner-gated, S6.1
scope, pinned to the `eafd632` lineage), per the Stage 6 entry proposal in
`adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/stage6-entry-proposal.md`.