# EC6D-SCOPE-Q1 evidence

Review-only artifacts for the EC6D-SCOPE-Q1 independent scope and acceptance
contract review. Nothing here is production code, and none of it is built,
tested, or shipped as part of the Penghou.Hetu solution.

## Contents

- `harness/Ec6dScopeReviewHarness.csproj` — standalone .NET 10 console
  project. It references the production projects read-only; it is not part of
  `Penghou.Hetu.slnx` and is never packaged or published.
- `harness/Program.cs` — the harness scenarios (A, B, C, D, F, G1-G5, H) that
  reproduce claimed baseline observations and run adversarial identity
  fixtures against the EC-6C p9 baseline (commit `2c3090c`).
- `harness/harness-results.json` — machine-readable output of the committed
  harness run, produced by `dotnet run` on the review branch.

## Reproduction

From the review worktree root:

```
dotnet build docs/reviews/ec6d-scope-q1-evidence/harness/Ec6dScopeReviewHarness.csproj
dotnet run --project docs/reviews/ec6d-scope-q1-evidence/harness/Ec6dScopeReviewHarness.csproj --no-build
```

The harness writes `harness-results.json` next to the project file and uses
only `%TEMP%` scratch repositories that it deletes on exit.

## What the scenarios establish

- A — a TypeScript interface with four explicit `implements` classes reachable
  through named relative imports: the graph records four `implements` edges to
  four distinct file-scoped heritage-target placeholders, and
  `FindImplementationsAsync` on the interface node returns 0 edges.
- B — heritage-name matching matrix (3 textual true positives, 1 false
  positive, 1 false negative) demonstrating why name equality alone cannot
  bind heritage names.
- C — six relative-import occurrences resolved by the relative-v1 resolver
  (extension-substitution and directory-index rules), plus one unresolved and
  one external specifier correctly excluded.
- D — export-entry slots carry no attribution to declaration nodes and import
  edges carry no binding names.
- F — a barrel `export *` publishes no export entry for the re-exported
  interface.
- G1 — duplicate interface names in two files stay distinct (path-scoped
  identity) while the heritage placeholder cannot disambiguate them.
- G2 — an imported alias colliding with a local declaration would be bound to
  the wrong declaration by a textual matcher.
- G3/G4/G5 — wrong-export imports, unused imports, and import-form gaps.
- H — `IsConsistentSnapshot=false` and null snapshot identity for both stores,
  repeat-index digest stability, digest sensitivity to source change, and
  LatticeDb reopen preserving all published facts.
