# FQ1-DOC-1 Correction Record

**Finding:** FQ1-DOC-1 (LOW, documentation accuracy), recorded in the GLM-Q1
qualification report at `54ca715`
(`docs/reviews/ec6c2-glm-q1-qualification-report.md` §6).
**Correction performed under:** EC6C-OWNER-01 (documentation only).
**Corrected file:** `docs/evidence/ec6c-1-export-shape-inventory-handoff.md`
(implementation evidence; the claim is corrected in place with a marker, and
this record preserves the original wording verbatim).

## Original wording (line 100, accepted-shape table)

> Inline and statement-level TypeScript type-only exports | Type-only state
> follows pinned grammar tokens and existing EC-6B grammar helpers; projection
> is type. **JavaScript type-only-like recovery is excluded.**

## What independent qualification demonstrated

For JavaScript source `const Model = 1;\nexport { type Model as Shape };\n`:

- The statement-level form `export type { value }` produces no export facts
  (the whole clause is excluded by the collector's JavaScript type-only-like
  prefix check) — the original sentence is accurate for this form.
- The inline form `export { type Model as Shape }` is invalid JavaScript; the
  pinned JavaScript grammar recovers an error-free `export_specifier`
  (`Model as Shape`) beside an `ERROR` token for `type`. The accepted
  positive-only rule A17 ("valid siblings may still emit" and "no fact for a
  recovered occurrence whose name/span cannot be established") permits the
  observed behavior: one structural export entry `Shape` is published with
  `local-name=Model`, **no** `type-only` property, and
  `namespace-projection=unknown`, while `javascript.parse-error` remains
  visible in the extraction diagnostics.
- Therefore the exclusion claimed in the accepted-shape table holds at
  statement level only; it does not describe the inline recovery path.

## Corrected wording (in place, line 100)

> Inline and statement-level TypeScript type-only exports | Type-only state
> follows pinned grammar tokens and existing EC-6B grammar helpers; projection
> is type. JavaScript statement-level type-only-like clauses
> (`export type { … }`) are excluded. Inline JavaScript recovery such as
> `export { type Model as Shape }` may still publish a conservative structural
> occurrence for the error-free specifier while the parser error stays
> visible: no `type-only` flag, `unknown` projection, and no assertion of
> JavaScript runtime export validity or complete TypeScript semantic
> interpretation. (Corrected in EC6C-OWNER-01; see
> `docs/evidence/ec6c-owner-acceptance/fq1-doc-1-correction.md`.)

The document's known-limitations list gained a matching bullet so no
remaining sentence in the file states the broader exclusion.

## Justification and boundaries

- The correction distinguishes statement-level exclusions, inline
  type-only-like constructs, conservative recovery behavior, parser
  diagnostics, and the limits of the semantic interpretation, and it states
  that a recovered entry does not prove JavaScript runtime export validity or
  complete TypeScript semantic interpretation.
- **No production semantics changed:** no parser, collector, publisher,
  identity, or resolver code was touched; the collector's behavior was not
  modified and the parse diagnostic is not suppressed.
- **No new exclusion rule was introduced.** Whether to exclude the inline
  recovered form is left as an owner decision for a future milestone if
  desired; the accepted positive-only A17 behavior remains as qualified.
- **No coverage expansion:** the qualification's claims (positive-only,
  narrow allowlist, no completeness) are unchanged and remain valid.

## Evidence

- Behavioral proof: GLM-Q1 test
  `Q1_D_JavaScriptStatementLevelTypeOnlyExcluded_InlineRecoveryBehaviorRecorded`
  (`tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportQualificationQ1Tests.cs`
  at `609af02`), independently reproduced during qualification.
- Finding record: qualification report §6, FQ1-DOC-1, at `75d23e9`.
