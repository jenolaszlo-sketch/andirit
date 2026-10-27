# EC-6D planning: evidence index

**R2 revision.** This index supersedes the R1 version at commit `0e1b213`
(additions marked; historical entries preserved). It lists the evidence for
the recovered original packet, the R1 remediation and the R2 repairs. No
production behavior is claimed anywhere: every EC-6D fact, binding and query
is a proposal.

## Original packet (recovered exactly, `91a3ed5`)

- Fixture corpus tree `014da71f0855cdc957fa9b6a0667bca565145092`
  (`corpus/ts-interface-impact/`), analysis harness
  (`analysis/ConsumerBaseline.csproj`, `analysis/Program.cs`), recorded
  observation (`analysis/observed-results.json`) and four test-suite logs.
- Environment: Windows x64, .NET SDK 10.0.401, TypeScript Tree-sitter
  plugin `0.2.0-preview.6+v0.23.2.abi14.p9` (existing structural facts keep
  the `.p8` evidence version by design).
- Reproduction commands as recorded in R1 §6; digests:
  `IndexIdentity 667ec2d4…`, `CanonicalFactsSha256 05bf7c7d…`.

## R1 evidence (EC6D-PLAN-R1, `0e1b213`)

- Recovery verification table (mirror identities, ancestry, 33-path change
  surface), `analysis/r1-reproduction-run.txt`, and the Q1 fetch record.
- R1 fresh-run digests `IndexIdentity 5c866e16…`,
  `CanonicalFactsSha256 631c0ab3…`; both digest pairs remain reproducible
  from their actual bytes (Q2 evidence, below).

## Q2 evidence (EC6D-SCOPE-Q2, `review/hetu-ec6d-scope-q2`)

- Reconciled review: `docs/reviews/ec6d-scope-q2-independent-verification.md`
  (content commit `6630624…`, tree `5c4447b4…`); register Q2-01..Q2-08.
- Evidence directory `docs/reviews/ec6d-scope-q2-evidence/`: `cases.json`
  (14 corpora), `harness/` (p9 observation harness), `ts-oracle.cjs` +
  `typescript-results.json` (TypeScript 5.9.3), `p9-adversarial-results.json`,
  `digest-reconciliation.json` (original mixed-LF, R1/Q2 CRLF and pure-LF
  digest pairs), `git-before/after/resumed.json` (protected-tree SHA-256
  audits), `audit-results.json` (18 assertions) and `validation.json`.

## R2 evidence (EC6D-PLAN-R2, this branch)

### Git bindings and change surface

- Base: R1 `0e1b213737d42c74df20aef4744ab5718058e266`
  (tree `ddf777aa…`), direct parent `91a3ed5…`; ancestry to EC-6C
  acceptance (`2c3090cb`) and canonical main (`558f8ac8`) unchanged.
- R2 changes exactly ten modified and five added paths, all under
  `docs/planning/ec6d/`; the diff under `src`, `tests` and `docs/reviews`
  is empty (verified with `git diff --name-status 0e1b213`).
- Modified: `symbol-binding-contract.md`, `consumer-query-contract.md`,
  `publication-consistency-contract.md`, `acceptance-contract.md`,
  `scope-proposal.md`, `integration-readiness.md`, `risk-register.md`,
  `q1-findings-disposition.md`, `consumer-baseline.md`,
  `recovery-and-provenance.md`. Added: `q2-findings-disposition.md`,
  `representation-decision-proposal.md`,
  `publication-policy-decision-proposal.md`,
  `analysis/fixtures/r2/` (`cases.json`, `README.md`),
  `analysis/r2-typescript-oracle-run.txt`.

### R2 TypeScript oracle run

- Compiler TypeScript `5.9.3`, node `v24.21.0`; 23 fixture cases from
  `analysis/fixtures/r2/cases.json`; recorded verbatim at
  `analysis/r2-typescript-oracle-run.txt` (script reused read-only from the
  Q2 branch, not modified).
- Highlights: `nested-unrelated`, `barrel-named`, `barrel-star`,
  `barrier-scope`, `interface-extends`, `same-file-merged` (zero
  diagnostics; heritage binds as intended); `qualified-member` and
  `default-implement` are valid TypeScript that the R2 profile
  conservatively excludes with explicit reasons; `absent-export`,
  `barrel-missing`, `barrel-multi`, `barrel-no-declaration`,
  `barrel-ordinary-consumer`, `expression-heritage`, `recovered-clause`,
  `ghost`, `external-package`, `forms-import-equals` record expected
  diagnostics and fail closed by contract.

### Validation

- Local Markdown links: 22 planning documents checked, 0 missing.
- `git diff --check`: clean; staged whitespace check clean.
- Hypothesis checks: no active import-first rule remains (only removal
  notes); reserved reasons labeled non-emittable; module-reference
  predicates use the verified vocabulary
  (`import`/`import-type` for possible consumers; `export-from`/
  `export-type-from`/`export-star`/`export-namespace-from` for barriers);
  every emitted reason has a reachable condition and fixture
  (`q2-findings-disposition.md` §C).
- Protected worktrees, Q1/Q2 evidence and the reconstructed original
  digests were re-verified read-only during R2 preparation; nothing outside
  this branch was modified.

## Limits

Fixtures are small; p9 does not yet emit facts A/C/E, so their behavior is
a future criterion. No cross-platform, representative-repository,
performance, migration or release qualification exists. The historical
migration-hash limitation (QN-1/FQ1-EV-1) and the Windows-only evidence are
preserved. Owner decisions (ADR-1, ADR-2, narrow attribution) and Q3
verification are open prerequisites; no implementation is authorized.
