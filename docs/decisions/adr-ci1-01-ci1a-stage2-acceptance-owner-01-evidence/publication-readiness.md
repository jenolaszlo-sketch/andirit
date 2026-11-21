# Publication readiness recommendation — Stage-2 baseline

**Recorded:** 2026-11-22 with `adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`.
Recommendation only. **Publication is not authorized** by the Stage-2 acceptance record.

## 1. Readiness assessment

**READY FOR A PUBLICATION COMMISSION** — the accepted Stage-2 baseline (`eafd632` lineage)
packs cleanly in the existing repository CI (`pack` job green on run 44746950426) and the
whole-matrix run is green. No package-publication-specific defect is known.

## 2. What the publication commission must cover

| Item | Content |
| --- | --- |
| Baseline pin | `eafd632`/tree `823e3c65…` (or its accepted descendant), verified by exact-byte export |
| Package identity/version | `0.2.0-preview.6` lineage per `Directory.Build.props`; explicit version decision in the commission |
| Native assets | Tree-sitter/LatticeDb RID assets resolve on the pack–consume–load smoke test per RID |
| Release notes | Restate the Stage-2 qualifications (Q-L1…Q-L8), the M5.4 amended-contract limitations (MR-1…MR-4) and the deferred Stage 6 scope (DF-1…DF-8) |
| Repository CI | The unmodified `ci.yml` (blob `be48426f…`); no workflow change as part of publication |
| Governance | Separate owner authorization; no Stage-6 claim; no Stage-2 restatement |

## 3. Recommended commission

`ANDIRIT-CI1A-S2.5-PUBLICATION-01` (owner-gated) — publish the accepted Stage-2 baseline to the
package feed with the smoke test and release notes above, after the Stage-2 acceptance record is
sealed. Until then, packages remain unpublished and the repository CI `pack` job is the only
pack evidence.