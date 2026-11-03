# CI1A S2.0 corpus R2 evidence

This is the source-bound remediation and qualification package for commission
`ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-R2`. It closes the two independent Q2
findings against the R1 oracle:

- **Q2-F01 (Blocking):** the source-lifecycle projection (`AppFacts`) used
  `method.OriginalDefinition` plus Roslyn generic `method.Arity`, while the main
  identity view used the accepted fixture-declared parameter-count shapes and
  reduced-extension normalization. The same occurrence therefore produced two
  different `usage4` identities in two sections of the same oracle.
- **Q2-F02 (Significant):** the E02 positive authority gate accepted any
  canonical-shaped defining `compctx2` (key presence + universe list membership)
  without comparing it to the current verified context. A one-line zero-digest
  mutation still passed 130/130 with byte-identical output.

R2 freezes a new expected artifact `oracle-expected-r2.json` (185 runtime
checks). The original Q1 and R1 expected files are preserved byte-identically,
and both historical oracles were re-reproduced from pinned isolated archives.
No production code, R2 formula, or accepted R2 vector changed.

## Package contents

| Record | Purpose |
|---|---|
| `SOURCE-BINDING.md` | pinned source revision and scope of change |
| `Q2-REMEDIATION-MATRIX.md` | finding-to-fix-to-evidence mapping |
| `CANONICAL-IDENTITY-REVIEW.md` | Q2-F01 correction, cross-view results, frozen anchors |
| `LIFECYCLE-CONSISTENCY.md` | DEL01 projection consistency, counts, Snapshot A/B |
| `REDUCED-EXTENSION-REVIEW.md` | reduced-extension normalization evidence |
| `DEFINING-PROVENANCE-REVIEW.md` | Q2-F02 correction, E02 case matrix, PROV-1..8 |
| `PROVENANCE-MUTATION-RESULTS.md` | all external mutation controls (exit codes, failing checks) |
| `HISTORICAL-PRESERVATION.md` | historical oracle reproductions and byte preservation |
| `REGRESSION-RESULTS.md` | oracle runs and full-solution regression (TRX) |
| `Q3-HANDOFF.md` | independent Q3 reproduction instructions |
| `mutations/<case>/` | one directory per external mutation: diff, run log, full output |
| `oracle-r2-run-{1,2}.json/.log` | two-process qualified R2 oracle runs |
| `q2-f01-preflight.json/.log` | field-level Q2-F01 reproduction |
| `q2-f02-{clean,mutated}-r1.json/.log` | Q2-F02 reproduction against R1 |
| `historical-oracle-*.json/.log` | historical reproductions from pinned archives |
| `trx/` | per-project TRX results for the 645-test regression |
| `r2-source-evidence-manifest.json` | frozen package manifest |

Verify package bytes with `verify-manifest.py`; archive mode requires the
manifest SHA-256 published in the final source-bound handoff.
