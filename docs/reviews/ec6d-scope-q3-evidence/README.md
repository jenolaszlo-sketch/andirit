# EC6D-SCOPE-Q3 independent verification evidence

Review-only artifacts for
[../ec6d-scope-q3-independent-verification.md](../ec6d-scope-q3-independent-verification.md)
(branch `review/hetu-ec6d-scope-q3`, worktree
`C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-scope-q3`).

Nothing here is production code, an EC-6D implementation, or a claim that any
proposed fact (A/C/E), binding rule or `GetInterfaceImpactV1` behavior exists.
The TypeScript compiler is used diagnostically as a semantic reference only.

## Files

| File | Content |
| --- | --- |
| `git-verification.txt` | Exact Git commands and outputs: R2/R1/recovered/EC-6C/main identities, Q2 supersession chain, all ancestry checks, the 12+6 change surface, empty `src`/`tests`/`docs/reviews` diff, `git diff --check`, branch and protected refs. |
| `q3-oracle-supplement-cases.json` | Six Q3-authored supplemental TypeScript cases (task-brief adversarial shapes not already covered by the committed corpora). |
| `q3-oracle-input-cases.json` | The exact 43-case oracle input manifest = Q2 corpora (14, from `review/hetu-ec6d-scope-q2` `cases.json` at `1bfcc84`) + R2 fixtures (23, from `planning/hetu-ec6d-plan-r2` `analysis/fixtures/r2/cases.json` at `3ce7dd9`) + the six supplements. |
| `q3-oracle-run.json` | Independent Q3 run of the unchanged Q2 oracle tool over the manifest (`typescript` 5.9.3, node `v24.21.0`). |
| `q3-oracle-recorded-comparison.json` | Mechanical comparison of the Q3 run with the recorded R2 run (23 cases, 0 diffs) and the recorded Q2 run (14 cases, normalized, 0 diffs). |
| `q3-link-check.json` | Relative Markdown-link existence check over all 22 planning documents under `docs/planning/ec6d` (67 links, 0 missing). |
| `validation.json` | Consolidated Q3 validation record: link check, whitespace, change surface, protected refs/worktrees, fsck/disk, artifact SHA-256 values, and the R2 `evidence-index.md` change-surface discrepancy found by Q3. |
| `tools/merge-cases.cjs` | Builds the 43-case manifest from the two committed corpora plus the supplement. |
| `tools/q2-typescript-oracle.cjs` | Byte-identical copy of the Q2 oracle tool (blob `c657eaf17e2bca6a94ae0ae6ef5e1a9e5dea0686` at `1bfcc84`), unmodified. |
| `tools/compare-oracle-runs.cjs` | Produces `q3-oracle-recorded-comparison.json`. |
| `tools/git-verification.cjs` | Produces `git-verification.txt`. |

## Reproduction

Prerequisites: node (the run used `v24.21.0`) and TypeScript 5.9.3 available
as a requireable module. Any 5.9.3 installation works; the run recorded here
resolved it from a local pnpm store path on the reviewer's machine.

```text
node tools/merge-cases.cjs <q2-cases.json> \
  docs/planning/ec6d/analysis/fixtures/r2/cases.json \
  q3-oracle-supplement-cases.json q3-oracle-input-cases.json
node tools/q2-typescript-oracle.cjs <typescript.js> q3-oracle-input-cases.json > q3-oracle-run.json
node tools/compare-oracle-runs.cjs q3-oracle-run.json \
  docs/planning/ec6d/analysis/r2-typescript-oracle-run.txt \
  <q2-typescript-results.json> q3-oracle-recorded-comparison.json
```

`<q2-cases.json>` and `<q2-typescript-results.json>` are the committed Q2
evidence files (`docs/reviews/ec6d-scope-q2-evidence/`) at `1bfcc84`; they
are referenced, not duplicated.

## Limits

- The oracle proves TypeScript 5.9.3 semantics for the fixture sources only;
  it is not evidence about any Hetu extraction, binding or query behavior.
- p9 observations were spot-checked read-only against the committed Q2
  evidence and production source; the Q2 C# harness was not re-executed.
- The link check verifies relative-target existence, not anchors.
- Windows x64 only; no performance or cross-platform claims.
