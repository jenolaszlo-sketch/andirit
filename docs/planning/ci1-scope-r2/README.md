# CI-1-SCOPE-R2 — F04 Acceptance Oracle Remediation

Planning-only remediation of the single remaining blocker from independent
**CI-1-SCOPE-Q2**: `Q2-F04-01 / P2` — the cited acceptance oracle accepted
incorrect semantic outcome classifications, including promoting an external
`System.Console.WriteLine` call to `confirmed`.

This packet corrects the executable planning oracle and its documentation,
records failing-before and passing-after evidence, and prepares a focused
independent **Q3** handoff. It does **not** implement CI-1A and does **not** fix
any production defect.

## Scope and authority

- **Authorized:** planning documents, planning-only validation tools, fixtures and
  evidence.
- **Not authorized / not done:** production implementation, existing-test change,
  schema change, package change, CI-workflow change, storage-representation
  selection (C vs B2), owner acceptance, release.
- **Untouched and still open:** F01–F03 and S01–S05 (Q2-supported at planning
  level), **P-DEF-01** (false C# semantic target confirmation), **P-DEF-02**
  (documented partial-type extraction failure), **ADR-CI1-01** (C vs B2).

## Deliverables

| Document | Purpose |
| --- | --- |
| [`README.md`](README.md) | This scope, authority and evidence index. |
| [`f04-root-cause.md`](f04-root-cause.md) | Why the previous checker accepted incorrect outcomes. |
| [`oracle-contract.md`](oracle-contract.md) | Authoritative semantic validation rules, reason codes and limitations. |
| [`mutation-matrix.md`](mutation-matrix.md) | Failing-before vs passing-after, diagnostics, positive controls. |
| [`reproduction.md`](reproduction.md) | Exact reproduction and verification commands. |
| [`q3-reviewer-handoff.md`](q3-reviewer-handoff.md) | Instructions for independent F04 qualification. |
| [`final-handoff.md`](final-handoff.md) | Git identities, changes, evidence, limitations, disposition. |

## Code and fixtures (`analysis/`)

| Path | Purpose |
| --- | --- |
| [`analysis/before/`](analysis/before/) | Faithful pure-Roslyn transcription of the cited prior checker (failing baseline). |
| [`analysis/oracle/`](analysis/oracle/) | Corrected semantic oracle. |
| [`analysis/fixtures/f01-collision/`](analysis/fixtures/f01-collision/) | Externally bound call vs indexed local with a coincident documentation ID. |
| [`analysis/compare.py`](analysis/compare.py) | Builds the before/after comparison JSON. |
| [`analysis/hash_evidence.py`](analysis/hash_evidence.py) | Records SHA-256 of checker sources and evidence. |

The oracle reference the NuGet `Microsoft.CodeAnalysis.CSharp` 5.9.0 package and
no production project.

## Evidence (`evidence/`)

| Path | Purpose |
| --- | --- |
| [`evidence/before/before-run.json`](evidence/before/before-run.json) | Prior checker: 32 sites, 0 baseline failures, 4 of 13 mutations undetected. |
| [`evidence/before/before-run.txt`](evidence/before/before-run.txt) | Raw run log. |
| [`evidence/after/after-run.json`](evidence/after/after-run.json) | Corrected oracle: full per-site and per-mutation evidence, source hashes, contexts. |
| [`evidence/after/after-run.txt`](evidence/after/after-run.txt) | Raw run log. |
| [`evidence/mutation-comparison.json`](evidence/mutation-comparison.json) | Side-by-side before/after mutation results with reason codes. |
| [`evidence/evidence-hashes.json`](evidence/evidence-hashes.json) | SHA-256 of every R2 checker source and evidence file. |
| [`evidence/git-bindings.json`](evidence/git-bindings.json) | Repository, commit, tree, branch and worktree bindings. |

## R1 documentation correction

R1's F04 claim that its cited oracle rejects wrong outcomes is corrected by
clearly-marked **R2 amendment** blocks appended to:

- [`../ci1-scope-r1/acceptance-reconciliation.md`](../ci1-scope-r1/acceptance-reconciliation.md)
- [`../ci1-scope-r1/corrections.md`](../ci1-scope-r1/corrections.md)
- [`../ci1-scope-r1/findings-matrix.md`](../ci1-scope-r1/findings-matrix.md)

The original R1 text is preserved; nothing historical is rewritten.

## Disposition

**CI-1-SCOPE-R2 READY FOR INDEPENDENT Q3.** See
[`final-handoff.md`](final-handoff.md) for the exact basis and limitations.
