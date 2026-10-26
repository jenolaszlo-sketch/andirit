# QN-1 Non-Blocking Test-Maintenance Note

**Observation:** QN-1 (NOTE), recorded in the GLM-Q1 qualification report at
`54ca715` (`docs/reviews/ec6c2-glm-q1-qualification-report.md` §6).
**Classification:** test-maintenance observation. **Not** a demonstrated
production defect.

## What was observed

`TreeSitterTypeScriptPluginTests.ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`
asserts the TypeScript evidence provider on all extracted edges except
`Exports` and `ResolvesTo`. The independent audit confirmed that:

- the `Exports` exclusion is necessary (the fixture produces export edges with
  the export-surface provider at p9); and
- the `ResolvesTo` exclusion is unnecessary for that fixture — it contains no
  resolves-to edges (its `node:fs`/`node:path` imports are external and
  unresolved), so the exclusion only narrows future regression coverage. No
  current behavior is masked; resolver provider and version remain explicitly
  asserted in the module-resolution tests.

## Disposition

- Preserved as a non-blocking note by EC6C-OWNER-01; the assertion was not
  changed under this assignment (no acceptance requirement depends on it).
- Cleanup may occur during a future test-maintenance milestone with
  appropriate regression coverage (for example, dropping the `ResolvesTo`
  exclusion or adding a resolves-to-bearing fixture to the provider
  assertion).
- QN-1 must not be represented as a production defect in any closure record.

## Evidence

- Qualification report §6 (QN-1) at `75d23e9`.
- Compatibility audit §1 at `54ca715`
  (`docs/reviews/ec6c2-glm-q1-compatibility-audit.md`).
