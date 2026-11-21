# ANDIRIT-CI1A-S2.5-PUBLICATION-WORKFLOW-01

**Date**: 2026-11-22
**Gate**: PUBLICATION-WORKFLOW-01 — correction and qualification of the release publication
workflow (`publish.yml`) for the `0.2.0-preview.7` release tag.
**Formal disposition**: **QUALIFIED** — the workflow now publishes the complete six-package
inventory with tag/version consistency validation and a pre-publish inventory gate; authentication,
permissions and triggers are unchanged; packaging steps were reproduced locally without publishing
and reconciled against the qualified `0.2.0-preview.7` inventory. **No tag is created and no NuGet
publication is triggered by this commission.**

---

## 1. Preflight (all matched)

| Object | Value |
| --- | --- |
| Local main / tree | `3c48187ca1ec66eb1c62c1cfb9c1a4134875bb57` / `00053387dce602cd8239b26d7f8cae5a2721fdfc`, `main`, clean |
| Remote main | `68a141f67023a9a66a5d2b77c8d93cf3cecb9993` (the release commit) |
| Version availability | `0.2.0-preview.7` absent on nuget.org for all six IDs; tag `v0.2.0-preview.7` not present remotely |
| Publication evidence | PUBLICATION-EXEC-01 evidence package intact (20 entries, byte-exact at the entry commit) |
| PR #7 | CLOSED (not merged) |

## 2. Workflow changes (`.github/workflows/publish.yml`, +34/−1)

| Change | Purpose |
| --- | --- |
| Added `src/Andirit.TreeSitter/Andirit.TreeSitter.csproj` to the pack list | **Defect fixed**: the September workflow omitted the Tree-sitter package from the tag-triggered release (five of six packages published). The release inventory is now the complete six-package set. |
| New step **Validate tag matches release version** (before Pack) | Reads `<Version>` from `Directory.Build.props`; on a tag run, fails with `::error::` unless `GITHUB_REF_NAME` == `v$VERSION`; non-tag (dispatch) runs log the repository version. Prevents publishing a tag whose version differs from the tree. |
| New step **Verify complete package inventory before publishing** (after Pack, before OIDC login) | Fails unless exactly 6 `.nupkg` + 6 `.snupkg` exist and all six expected package IDs are present. Guarantees the publish steps can never run on an incomplete inventory. |
| Quoting normalization `args+=("--property:PackageVersion=…")` | Robustness; semantics unchanged. |

**Unchanged (verified by diff and YAML parse)**: `on: push tags ['v*']` + `workflow_dispatch` only
(**no branch trigger — ordinary `main` pushes cannot publish**); top-level `permissions: contents:
read`; job permissions `id-token: write, contents: read`; the `NuGet/login@v1` OIDC step with
`secrets.NUGET_USER`; the push steps (`dotnet nuget push --skip-duplicate` for nupkg then snupkg).
No credential, token or secret is added, logged or exposed; no production behavior is touched.

## 3. Local verification (no publishing)

| Check | Result |
| --- | --- |
| YAML parse (pyyaml) | valid; steps: Checkout, Setup .NET, Validate tag matches release version, Pack, Verify complete package inventory, NuGet login, Push nupkg, Push snupkg |
| Tag/version logic (bash, mirror run) | `v0.2.0-preview.7` → **ok-tag-matches**; `v0.2.0-preview.6` and `v0.2.0-preview.8` → **error-tag-mismatch** (fails before Pack); dispatch on main → **ok-non-tag** (`workflow-logic-test.sh`, `workflow-logic-test-output.txt`) |
| Packaging reproduction | all six per-project packs exit 0 at `PackageVersion=0.2.0-preview.7` on a fresh byte-exact export of `68a141f6` (4,722/4,722 blobs) (`workflow-pack-exits.txt`) |
| Inventory gate | **ok-inventory (6 nupkg, 6 snupkg)** against the reproduced artifacts |
| Reconciliation vs qualified preview.7 feed | 12/12 files, identical names; nuspec versions all `0.2.0-preview.7`; dependency graph consistent (`LatticeDb → LatticeDbSharp 0.2.0`; `TreeSitter → TreeSitter.DotNet 1.3.0`; `CSharp → Roslyn`; internal refs at `0.2.0-preview.7`); `README.md`, `.nuspec`, `nuget.psmdcp` and all three RID natives **byte-identical** to the qualified feed; assembly DLLs same size, differing only in build-determinism metadata (PE timestamp, module MVID, PDB checksum) — the same class of difference present between independent builds across machines (`package-reconciliation.txt`, `package-diff-detail.txt`) |
| Historical trigger audit | `Publish to NuGet` runs exist only for tag pushes (v0.2.0-preview.5/.6) and `workflow_dispatch`; the 2026-11-21 `main` publication produced only the `ci` run (`publish-workflow-run-history.json`) |

## 4. Failure visibility and safe retry

Inventory/version gates fail the job with `::error::` annotations **before** any authentication or
push, so a mis-tagged or incomplete release never reaches the feed and the failure is visible in the
run summary and the tag event. The pushes keep `--skip-duplicate`, so re-running the workflow after
a partial publication skips already-published packages and only pushes the remainder; a fresh
failure during push still fails the job visibly (no `continue-on-error`, no masked steps).

## 5. Stage 6 and boundary

No Stage 6 implementation; no accepted qualification broadened; the Q-L/MR/DF qualifications and
Stage 6 deferrals stand. The only change is the narrow workflow remediation on `main`, plus this
record and its evidence package.

## 6. Intended release behavior (for the separately gated tagging commission)

- **Tag**: `v0.2.0-preview.7` (annotated) at release commit `68a141f67023a9a66a5d2b77c8d93cf3cecb9993`
  (not at `3c48187`, the post-publication evidence commit — both trees carry identical package
  content, but the release commit is the release identity).
- **On tag push**: publish.yml validates tag/version consistency, packs the six packages + six
  symbol packages at `0.2.0-preview.7`, enforces the 6/6 inventory, logs in via OIDC, and pushes
  twelve `.nupkg`/`.snupkg` files to `https://api.nuget.org/v3/index.json` with `--skip-duplicate`.
- **Readiness**: **READY** for the final tag-triggered NuGet release under a separate owner decision,
  with the carried osx-arm64 smoker gap (package-content evidence only; win-x64 and linux-x64
  pack-consume-load smoke evidenced).