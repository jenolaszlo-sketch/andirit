# External mutation-control results

Each control is an external, one-purpose mutation applied to an isolated copy
of the frozen R2 oracle (`mutation-base` = the R2 oracle + corpus + frozen
expected, verified 185/185 before mutation; copies are ordinary directories,
not worktrees). The mutated copy is built and run against the R2 frozen
expected artifact. A correct oracle must **fail** (exit 1) on every control;
`mutation.diff` records the exact patch, `out.json` the full failing report,
`run.log` the console output including failing check IDs.

All runs: .NET SDK 10.0.401, Roslyn 5.9.0.0, Windows x64. Every control
produced exit code 1 with 0 unexpected failures elsewhere.

| # | Case | Mutated file | Defect modeled | Failed | Failing check IDs |
|---|---|---|---|---:|---|
| 1 | `q2-f01-lifecycle-regression` | `Program.cs` | Q2-F01 reintroduced verbatim (`OriginalDefinition` + generic `method.Arity` in `AppFacts`) | 12 | crossView.S01, S02, S03, S06, S21, S26, INV-01, INV-11, INV-31, crossView.all-sites-agree, expected.crossView, expected.lifecycle |
| 2 | `q2-f02-presence-only-gate` | `ProvenanceGate.cs` | Q2-F02 reintroduced verbatim (eligibility = key presence + universe list membership) | 12 | optionA.A08, e02.E02-R2-03-stale, -04-all-zero, -05-wrong-compilation, -07-incompatible-universe, e02.PROV-1, PROV-2, PROV-3, PROV-5, PROV-7, expected.variants, expected.e02 |
| 3 | `e02-zero-supplied-context` | `Program.cs` | Q2 reviewer's zero-digest supply patched into the R2 positive case | 2 | e02.E02-R2-01-valid, expected.e02 |
| 4 | `frozen-expected-e02-authority-zeroed` | `expected/oracle-expected-r2.json` | frozen provenance authority field tampered | 1 | expected.e02 |
| 5 | `frozen-expected-s01-usage4-altered` | `expected/oracle-expected-r2.json` | frozen S01 usage4 vector altered (all 9 occurrences) | 3 | expected.identities, expected.crossView, expected.lifecycle |
| 6 | `universe-membership-removed` | `ProvenanceGate.cs` | gate ignores universe membership entirely | 7 | optionA.A07, e02.E02-R2-06-missing-universe, -07-incompatible-universe, e02.PROV-4, PROV-5, expected.variants, expected.e02 |
| 7 | `universe-context-equality-removed` | `ProvenanceGate.cs` | membership reduced to key presence (incompatible context accepted) | 3 | e02.E02-R2-07-incompatible-universe, e02.PROV-5, expected.e02 |
| 8 | `deleted-contribution-reinjected` | `Program.cs` | deleted `App/Sites.cs` contributions reinjected into Snapshot B | 4 | lifecycle.DEL-N1, lifecycle.DEL-counts, lifecycle.deterministic, expected.lifecycle |
| 9 | `overload-substitution-s01` | `Program.cs` | S01 main target swapped to `Methods.Static(System.Int32)` (same shape) | 7 | crossView.S01, crossView.S01.frozen, crossView.ID-2-target-vs-occurrence, crossView.ID-N5-overload-substitution, crossView.all-sites-agree, crossView.all-frozen-anchors, expected.crossView |
| 10 | `reduced-normalization-disabled` | `CanonicalTarget.cs` | `ReducedFrom` normalization removed (R1 lifecycle behavior) | 9 | semantic.S06, semantic.INV-31, semantic.INV-32, crossView.S06, crossView.INV-31, crossView.all-sites-agree, expected.semantics, expected.crossView, expected.lifecycle |

## Interpretation

- Both Q2 findings are detected by the corrected oracle in their verbatim R1
  form (cases 1 and 2), not only through the reviewer's minimal one-line
  patches (cases 3, and case 1's field-level probe `q2-f01-preflight.json`).
- The frozen expected artifact is an enforced authority: tampering with the
  provenance field (case 4) or a frozen usage4 vector (case 5) is rejected by
  the compared `expected.*` sections.
- Each component of the new gate is individually load-bearing: removing
  universe membership (case 6) or membership-context equality (case 7) is
  caught by dedicated E02 cases and PROV invariants.
- The lifecycle stale-contribution rejection and the overload/shape
  distinctions are enforced against frozen bytes, not self-comparisons
  (cases 8, 9, 10).

Baseline control: the unmutated copy passes 185/185 (exit 0) — see
`REGRESSION-RESULTS.md`.
