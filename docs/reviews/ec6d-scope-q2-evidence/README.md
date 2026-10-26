# EC6D-SCOPE-Q2 evidence

Review-only artifacts for the EC6D-SCOPE-Q2 independent remediation
verification. Nothing here is production code; nothing in this directory is
part of the Penghou.Hetu solution. The harness used for the runs is the
committed planning harness (`docs/planning/ec6d/analysis/`) at the reviewed
R1 commit, built and executed from the review worktree — unmodified.

## Git binding (this review)

- Reviewed source commit: `0e1b213737d42c74df20aef4744ab5718058e266`
  (tree `ddf777aa155852daca40b6ed8748c028c67d5822`, parent
  `91a3ed53a256e89ed71d7d11c204252c417400a6`).
- Review branch: `review/hetu-ec6d-scope-q2`.
- Review-content commit / tree: recorded in the binding-record follow-up
  commit (final section of this file).
- Final review commit / tree / parent: recorded in the binding-record
  follow-up commit.
- Review report:
  [`../ec6d-scope-q2-independent-verification.md`](../ec6d-scope-q2-independent-verification.md).

## Contents (produced by this review)

- `runs/crlf-environment-reproduction.json` — Q2 run 1: the committed corpus
  as checked out in the review worktree (all-CRLF working files). Reproduces
  the R1 fresh-run values (`IndexIdentity` `5c866e16…`, `CanonicalFactsSha256`
  `631c0ab3…`).
- `runs/original-bytes-reconstruction.json` — Q2 run 2: the corpus
  reconstructed to the original environment's bytes (LF except the final line
  break; 14/14 files byte-identical to the original planning worktree).
  Reproduces the committed original observation (`667ec2d4…`, `05bf7c7d…`).
- `runs/original-bytes-reconstruction.sha256` — SHA-256 of each reconstructed
  file (so the reconstruction can be verified without committing
  CRLF-sensitive bytes).
- `runs/adversarial-corpus-observation.json` — Q2 run 3: the adversarial
  fixture corpus, indexed through the unmodified planning harness.
- `fixtures/adversarial/` — 13-file adversarial corpus plus
  [README.md](fixtures/adversarial/README.md) tabulating each probe, its
  adversarial intent, and the observed p9 facts.
- `tools/q2-typescript-oracle.cjs`, `runs/typescript-oracle-cases.json`,
  `runs/typescript-oracle-q2.json` — Q2-authored TypeScript semantic oracle
  (see below).

## Reproduction

From the review worktree root:

```powershell
dotnet build docs/planning/ec6d/analysis/ConsumerBaseline.csproj --no-restore -m:1
dotnet docs/planning/ec6d/analysis/bin/Debug/net10.0/ConsumerBaseline.dll docs/planning/ec6d/corpus/ts-interface-impact > runs/crlf-environment-reproduction.json
dotnet docs/planning/ec6d/analysis/bin/Debug/net10.0/ConsumerBaseline.dll docs/reviews/ec6d-scope-q2-evidence/fixtures/adversarial > runs/adversarial-corpus-observation.json
```

For the original-bytes reconstruction: copy the committed corpus, normalize
line endings to LF, then set the **final** line break to CRLF; the resulting
files must match the `original-bytes-reconstruction.sha256` manifest and the
harness must produce the original digest values above. (The reconstructed
bytes cannot be committed literally because Git normalizes line endings; the
manifest plus this procedure is the reproducible form.)

## TypeScript semantic oracle (Q2-authored)

Committed with the review because the Q2-N01 correctness finding depends on
TypeScript's name-resolution semantics:

```powershell
node tools/q2-typescript-oracle.cjs "<path-to-typescript.js>" runs/typescript-oracle-cases.json > runs/typescript-oracle-q2.json
```

The compiler library used was TypeScript 5.9.3 from an editor installation on
this machine
(`C:\Users\Laszlos\AppData\Local\Programs\Antigravity IDE\resources\app\extensions\node_modules\typescript\lib\typescript.js`);
node v24.21.0. The script indexes each case with `strict` settings via the
TypeScript compiler API and reports diagnostics plus the declaration each
heritage clause actually resolves to.

Results (committed output):

| Case | Diagnostics | Heritage resolution |
| --- | --- | --- |
| `ns-shadow` (module import alias shadowed by a namespace-local interface) | **none (valid TypeScript)** | binds the **namespace-local** `Drawable` in `consumer.ts` |
| `collision` (import alias + same-scope local interface) | TS2440 "Import declaration conflicts with local declaration" | imported `Widget` (file is invalid) |
| `type-param` (`class TypeParamC<Renderable> implements Renderable`) | TS2422 "A class can only implement an object type…" | the class's `TypeParameter` |

## Pre-existing environment artifacts (preserved, NOT part of this review)

When this review began, `docs/reviews/ec6d-scope-q2-evidence/` **already
contained** an untracked, setup-generated verification toolkit, created
2026-10-26 21:40–21:45 local (before the first Q2 file write at 2026-10-27
00:45). It was not produced by this review, is **not committed as Q2
evidence**, and was not used as evidence in the review report. It is
preserved untouched for the owner:

`audit-results.json`, `audit_evidence.py`, `baseline-crlf.json`,
`baseline-crlf-repeat.json`, `baseline-lf.json`,
`baseline-original-bytes.json`, `cases.json`, `digest-reconciliation.json`,
`git-before.json`, `harness/` (a copied C# harness project; `bin/` and `obj/`
build output is Git-ignored), `p9-adversarial-results.json`, `reproduce.py`,
`ts-oracle.cjs`, `typescript-results.json`, `verify_git.py`, and the
Git-ignored working directory `bin/` (scratch fixtures).

Notes recorded during Q2 verification of that directory:

- Its digest reconciliation and identity equality conclusions were
  **independently reproduced** by Q2's own runs (above) and by the review
  report's §C checks; Q2 did not rely on its outputs.
- Its TypeScript oracle used compiler 5.9.3; Q2 re-ran an equivalent check
  with its **own** script and cases (above), producing the committed results.
- `harness/`, `bin/`, and `obj/` build products are covered by the
  repository's `.gitignore` and never appear in `git status`.

## Verified outcomes (summary)

| Measure | Original observation | R1 fresh run | Q2 run 1 (CRLF) | Q2 run 2 (reconstructed) |
| --- | --- | --- | --- | --- |
| Node identity set (64) | baseline | equal | **equal to both** | **equal to both** |
| Edge identity set (99) | baseline | equal | **equal to both** | **equal to both** |
| Counts (units/contributions/declarations/edges) | 14/73/23/99 | equal | equal | equal |
| Relationship mix | 23/23/13/5/17/1/1/16 | equal | equal | equal |
| `FindImplementations` (target) | seed-only | seed-only | seed-only | seed-only |
| `PublicationConsistent` | false | false | false | false |
| `IndexIdentity` | `667ec2d4…` | `5c866e16…` | **`5c866e16…`** | **`667ec2d4…`** |
| `CanonicalFactsSha256` | `05bf7c7d…` | `631c0ab3…` | **`631c0ab3…`** | **`05bf7c7d…`** |
| `SnapshotIntegrityHash` | `f1bcebc2…` | `5659fad9…` | `60fff76d…` (run metadata; varies) | varies |

Additional verified facts: committed corpus blobs are identical across
`1c9f4c9`/`3b5e8d9`/`91a3ed5`/`0e1b213` (corpus tree
`014da71f0855cdc957fa9b6a0667bca565145092`); `git hash-object` of each
working file equals its committed blob (`src/other/Renderable.ts` →
`79fddb0a7de07f9245671c981e540bd26de0576a`); Q2 run 1 differs from R1's
committed run only in the `SnapshotIntegrityHash` line; the mirror `main`
ref remains `558f8ac82ac1b5e07da68f4a71a70240eaf38409` and the live origin
(`https://github.com/jenolaszlo-sketch/penghou-hetu.git`) has 9 heads with
no EC-6D/planning refs.

## Final review commit (binding record)

- Review-content commit / tree: `PENDING` / `PENDING`.
- Final review commit / tree / parent: `PENDING`.

## Limits

Runs are Windows x64 / .NET SDK 10.0.401; no cross-platform execution. The
adversarial corpus exercises p9 extraction only (no EC-6D facts exist yet);
binding-level statements live in the review report. The TypeScript oracle
covers the three committed cases only. The four existing test suites were not
re-run (Q1's 388/388 run at the same baseline is referenced).
