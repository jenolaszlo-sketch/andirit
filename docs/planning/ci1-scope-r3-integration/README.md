# CI-1-SCOPE-R3 integration — accepted planning oracle

**Status: integrated locally on `main`; not pushed.** This directory records the
owner acceptance and integration of the CI-1-SCOPE-R3 semantic acceptance
oracle following the independent CI-1-SCOPE-Q4 qualification.

- Owner decision: [`docs/decisions/ci1-scope-owner-01-acceptance.md`](../../decisions/ci1-scope-owner-01-acceptance.md)
  — `CI-1-SCOPE-R3: ACCEPT WITH QUALIFICATIONS`.
- Independent review: `CI-1-SCOPE-Q4 SUPPORTED WITH QUALIFICATIONS`
  (workspace `C:\Users\Laszlos\source\repos\Solo\ci1-scope-q4`; not duplicated
  here — referenced by identity).
- Producer evidence: `C:\Users\Laszlos\source\repos\Solo\ci1-scope-r3`
  (preserved unchanged; not duplicated here).

## What was integrated

Exactly the seven Q4-qualified artifacts and two append-only amendments listed
in [`integration-manifest.md`](integration-manifest.md):

| Kind | Count | Destination |
| --- | --- | --- |
| Oracle sources | 5 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/` |
| Identity probe | 2 | `docs/planning/ci1-scope-r2/analysis/identity-probe/` |
| Contract amendment | 1 | appended to `docs/planning/ci1-scope-r2/oracle-contract.md` |
| Mutation-matrix amendment | 1 | appended to `docs/planning/ci1-scope-r2/mutation-matrix.md` |

The R2 oracle, before-harness, fixtures, the frozen 32-row manifest and all
historical evidence are untouched. R2 remains historical evidence; R3 is the
accepted planning-oracle successor.

## Scope of the acceptance

The oracle validates the frozen CI-1 acceptance corpus
(`docs/planning/ci1-scope-01/fixtures/expected-sites.json`): typed semantic
assertions, exact target-definition correspondence inside the expected
declaring compilation, outcome-facet provenance, label-independent conditional
activity, lexical owner, repository path, corpus completeness and controlled
malformed-manifest diagnostics. It is **planning/qualification tooling**, not a
production query implementation.

## Binding identities

| Item | Value |
| --- | --- |
| R3 frozen qualification baseline | `5c89ac0b3d0e87c0112880cf985826ded42b21b2` |
| Published starting HEAD / tree | `d02baaa8e7e8704f2f5dac9ab549779f45fa5cb6` / `406282797948c035b3dda4f70b9ec5c5b536a6d7` |
| Q4 oracle evidence hash | `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` |
| Integrated reproduction hash | same as above (byte-identical) |
| Integration / bookkeeping commits | recorded in [`final-handoff.md`](final-handoff.md) |

## Documents

| Document | Content |
| --- | --- |
| [`integration-manifest.md`](integration-manifest.md) | Exact source→destination inventory, hashes, verification results |
| [`qualification-reconciliation.md`](qualification-reconciliation.md) | Q4 findings Q4-F01…F05 and preserved limitations |
| [`test-results.md`](test-results.md) | Reproduced oracle gate, mutation counts, identity probe, repository regression |
| [`final-handoff.md`](final-handoff.md) | Git commits, changed-file inventory, publication state |

## What this integration does **not** do

No CI-1A implementation, no occurrence storage or persistence schema, no query
API change, no ADR-CI1-01 (B2 vs C) selection, no package publication, release
or push. The next separately authorized engineering milestone is the ADR-CI1-01
B2-versus-C experiment.
