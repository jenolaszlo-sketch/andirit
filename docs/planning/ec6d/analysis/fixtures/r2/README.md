# EC6D-PLAN-R2 fixture supplements (review-only, non-production)

Purpose: concrete, reproducible inputs for the R2 acceptance rows that the
committed Q2 corpora
(`docs/reviews/ec6d-scope-q2-evidence/cases.json`) do not already cover.
All sources live in [cases.json](cases.json) (same shape as the Q2 corpus
manifest: case → { file → source }). The TypeScript 5.9.3 semantic oracle
was run over every case; the recorded output is
[../../r2-typescript-oracle-run.txt](../../r2-typescript-oracle-run.txt).
None of this is production code, and no EC-6D fact, binding or query
behavior exists to test yet: the expected dispositions below are **future
contract criteria**.

## Case map

| Case | Acceptance row | Shape | Expected future disposition (contract level) |
| --- | --- | --- | --- |
| `absent-export` | A04b | import of the target name from a module that does not export it (heritage clause + bare import) | clause-level and occurrence-level `missing-export`; no confirmation |
| `barrel-named` | A10 | named re-export barrel, heritage consumer | clause-level `re-export-barrier` with the named record as witness |
| `barrel-star` | A10 | star re-export barrel, heritage consumer | clause-level `re-export-barrier` (star record; target-star-barrel association) |
| `barrel-missing` | A10 | unresolved re-export source | `re-export-barrier` carrying the unresolved record (never invisible) |
| `barrel-multi` | A10 | two export sources (resolved + unresolved) | `re-export-barrier` listing both records separately, never first-wins |
| `barrel-no-declaration` | A10 | barrel re-exports a name the resolved source does not declare | barrier, never confirmation (re-exports publish no entries) |
| `barrel-ordinary-consumer` | A10b | ordinary importing module (import-side references only) | `missing-export`, **not** a barrier |
| `barrel-missing-consumer` | A10b | bare import through an unresolved barrel (no heritage clause) | occurrence-level `re-export-barrier` |
| `forms-import-equals` | A12 | `import W = require(...)` heritage (binds the module namespace; the recorded TS2709 diagnostic confirms using it as a type is invalid — refusal precedes semantics) | `unsupported-import-form` |
| `forms-inline-type` | A12 | inline `{ type Widget }` heritage | **confirmed** (per-specifier type-only is eligible) |
| `forms-dynamic` | A12 | dynamic `import()` occurrence | recorded (form `dynamic`), coverage only, never a candidate |
| `expression-heritage` | A25 | `extends factory(Widget)` | `unsupported-heritage-form` (expression, terminal identifier target-associated) |
| `interface-extends` | A25 | `interface Extended extends Widget` | `heritage-kind-not-supported` (transitive impact excluded) |
| `same-file-merged` | A22 | interface + class merging in one file, same-file implementation | `ambiguous-local-declaration` (two module-scope physical records) |
| `ambient-export` | A26 | `export declare interface` target | attribution `ineligible-declaration-context` → `ineligible-declaration` |
| `ambient-script` | A26 | non-module (script) file heritage | `ambient-or-global-context` |
| `recovered-clause` | A26 | clause/owner in recovered error syntax | `recovered-heritage-syntax` |
| `ghost` | A26 | export clause naming a declaration that does not exist | entry `missing-declaration` → `unattributed-export` |
| `external-package` | A28 | bare package specifier | `external-module`; no possible consumer |
| `barrier-scope` | A27 | target-associated barrier + unrelated barrel + direct consumer | barrier only for the related chain; unrelated never appears; direct stays confirmed |
| `nested-unrelated` | A23 (oracle) | nested same-name declaration that must not block a module-scope import binding | import route confirmed (TS-valid; module-scope candidates only) |
| `default-implement` | A12 (oracle) | default-import heritage of a default-exported interface | `unsupported-import-form` (conservative exclusion of a TS-valid shape) |
| `qualified-member` | A25 (oracle) | `implements NS.Widget` | `qualified-heritage-unresolved` with stored fact-C evidence |

## Oracle notes

The oracle (`docs/reviews/ec6d-scope-q2-evidence/tools/q2-typescript-oracle.cjs`,
TypeScript 5.9.3, node v24.21.0) reports diagnostics and the declaration each
heritage clause resolves to. Expected semantic highlights, to be read from
the recorded output:

- `nested-unrelated`, `interface-extends`, `same-file-merged`,
  `forms-inline-type`, `barrel-named/star`, `barrier-scope` direct consumer,
  `qualified-member`, `default-implement` are valid TypeScript and bind as
  the fixtures intend.
- `absent-export`, `barrel-missing`, `barrel-multi`,
  `barrel-no-declaration`, `expression-heritage`, `recovered-clause`,
  `ghost` produce diagnostics (invalid or recovered source); the contract
  deliberately fails closed on them instead of guessing.
- `ambient-script` is valid TypeScript in a global-script context; the
  contract still refuses binding there because cross-file global merging is
  not structurally verifiable.

Diagnostics are recorded for completeness, never as an excuse to promote a
relationship: unsupported or invalid shapes remain unknown.

## Limitations

- These are planning fixtures; p9 does not yet emit facts A/C/E, so no
  observation of the future extraction exists. The R2 oracle run verifies
  only TypeScript semantics.
- No cross-platform or performance claims; no production behavior is
  implied.
