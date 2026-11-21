# Publication receipt — PUBLICATION-EXEC-01

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-PUBLICATION-EXEC-01.md`.
The repository publication (fast-forward of canonical `main`) is the only remote write this
commission performs. NuGet publication and release tagging are **not** performed and remain
separately gated.

## 1. Push receipt

| Item | Value |
| --- | --- |
| Command | `git push origin main` (normal push; no `--force`, no history rewrite) |
| Result | `4837071..68a141f  main -> main` (fast-forward; exit 0) |
| Pre-push `origin/main` | `48370711a6247b972dfc83628618924e864be374` (strict ancestor of the release commit) |
| Post-push `origin/main` | `68a141f67023a9a66a5d2b77c8d93cf3cecb9993` — **equals the intended release commit** ✓ |
| PR #7 disposition | executed before the push (`pr-disposition.md`) — CLOSED, not merged |

## 2. Push-triggered CI receipt (run 44953429653)

| Item | Value |
| --- | --- |
| Run | `ci`, event **push**, branch `main`, head `68a141f6` (the release commit) |
| Conclusion | **success** — status `completed`, `updatedAt 2026-11-21T21:52:18Z` (`ci-run.json`) |
| Jobs | `test (windows-latest)` success · `test (ubuntu-latest)` success · `test (macos-latest)` success · `pack` success |
| Packages packed in CI | all six at **`0.2.0-preview.7`** + six `.snupkg` (`ci-job-pack.log`, artifacts/packages) |
| Genuine-Ubuntu pack-consume-load smoke | PASS on this run (`ci-job-pack.log`): all checks `ok`, `PASS: all six packed Andirit packages, managed providers, durable store, testing conformance and Tree-sitter natives resolved.` — repacked at the unchanged ci.yml `SMOKE_VERSION` override `0.2.0-preview.6-smoke.44953429653` (an explicit per-run smoker override, not the release version) |
| Workflow | `.github/workflows/ci.yml` unchanged from the accepted configuration (no workflow change in the release commit) |
| Gate verdict | all required platform jobs and packaging gates pass — tagging/NuGet may proceed only under a separate owner decision |

## 3. Not performed (separately gated)

- NuGet publication to any feed (including nuget.org — none of the six IDs exists there as of
  2026-11-22, and the `publish.yml` tag-triggered workflow is not armed: no tag is created).
- Release tag creation/push (`v0.2.0-preview.7` remains available and uncreated).
- Qualification branch deletion (`qualification/wp-s25-4-sq05-linux-r2` and
  `qualification/ci1a-s25-sq05-linux` preserved at their prior commits).
- Stage 6 implementation.