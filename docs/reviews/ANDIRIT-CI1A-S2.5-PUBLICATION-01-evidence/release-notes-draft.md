# Release notes draft — Andirit Stage-2 baseline (v0.2.0-preview.7, recommended)

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-PUBLICATION-01.md`. Draft for the
publication decision; not yet released. Add to `CHANGELOG.md` as a new
`## 0.2.0-preview.7` section above the existing `## 0.2.0-preview.6` (September release) section
at publication time, leaving `## Unreleased` empty — see publication-plan §2: the version
`0.2.0-preview.6` is already a released (tagged) version.

## Summary

The Stage 2 baseline of the Andirit code knowledge graph: C# method-invocation occurrence
extraction with canonical identity (`usage4`/`asmctx3`), verified-compilation binding,
native LatticeDb persistence, authority-gated publication, bounded reader queries, coverage
and conservative refusal. Stage 2 is **accepted with qualifications** and closed within its
qualified boundary (2026-11-22).

## Highlights

- **Occurrence extraction and identity**: supported invocation sites extract as facts with
  exact spans; canonical identities recompute independently from the frozen R2 oracle
  vectors (185/185).
- **Authority-gated minting**: `asmctx3` minting requires a non-null recorded defining
  context; every non-Complete resolution refuses; the unresolved classification never reaches
  publication without a verified authority envelope.
- **Native persistence**: atomic publication with durable reopen; schema/namespace/page-binding
  and stale-publication refusals; authority-masking refusals.
- **Reader surface**: bounded target-index queries with continuation and `Examined`
  accounting; truthful availability mapping.
- **Cross-platform**: genuine hosted Ubuntu x64 execution of the 75-case native suite
  (Ubuntu 26.04.1, SDK 10.0.404); the corrected revision produced a green full-matrix CI run
  (format step + four coverage-gated suites + pack).
- **Packages**: six packages (`Andirit`, `Andirit.Abstractions`, `Andirit.CSharp`,
  `Andirit.LatticeDb`, `Andirit.Testing`, `Andirit.TreeSitter`); Tree-sitter natives for
  win-x64/linux-x64/osx-arm64; pack-consume-load smoke evidence for win-x64 and linux-x64.

## Qualified boundary (restate at release)

- Linux scope restricted to the demonstrated hosted environment (Ubuntu 26.04.1 x64, SDK
  10.0.404, hosted runner) and 75-case-suite behavior; other generations/distributions and
  full platform certification are not claimed.
- Managed fault injection only; no crash/power-loss/multi-process durability claim.
- M5.4 is satisfied under an amended contract with a conservative false-negative direction;
  constructed-control evidence does not establish natural reachability; an exhaustive Roslyn
  symbol-association proof remains unproven.
- Deferred Stage 6 scope (full D3, D4.5, full S-02, cancellation paths, full platform
  certification) is unchanged.
- Preview release: public contracts may be revised.

## Fixes since the last preview

- Exact-byte formatting gate: two comment-only relocations resolve the CI formatter failure
  (exit 0) without configuration changes.
- LatticeDb branch coverage raised above the repository 70% policy via 23 focused regression
  tests (368/466 = 78.96%; 900/969 lines = 92.87%); the collection continues to expand with
  the expanded durable-state and native-mirror regression coverage.