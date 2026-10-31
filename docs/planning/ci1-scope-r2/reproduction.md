# Reproduction — failing-before and passing-after

All commands run from the canonical checkout `D:\projects\Andirit`. The checker
projects are planning-only and reference the NuGet `Microsoft.CodeAnalysis.CSharp`
package (Roslyn 5.9.0), **not** any production project.

## 0. Environment

- .NET SDK `10.0.401`; Roslyn `Microsoft.CodeAnalysis.CSharp` `5.9.0` (cached at
  `C:\Users\Laszlos\.nuget\packages`); Python 3.14.
- Offline restore source: `C:\Users\Laszlos\.nuget\packages`.
- Canonical fixtures: `docs/planning/ci1-scope-01/fixtures/`.

## 1. Failing-before (reproduce the four Q2 failures)

```powershell
Set-Location D:\projects\Andirit\docs\planning\ci1-scope-r2\analysis\before
dotnet restore Before.csproj --ignore-failed-sources -p:NuGetAudit=false --source C:\Users\Laszlos\.nuget\packages
dotnet run --no-restore -c Release --project Before.csproj -- `
  D:\projects\Andirit `
  D:\projects\Andirit\docs\planning\ci1-scope-r2\evidence\before\before-run.json *>&1 |
  Tee-Object -FilePath D:\projects\Andirit\docs\planning\ci1-scope-r2\evidence\before\before-run.txt
```

Expected summary:

```json
{"Sites":32,"Failures":0,"Mutations":13,"UndetectedMutations":4}
```

The four undetected mutations are `outcome-confirmed-to-external`,
`outcome-confirmed-to-deferred`, `outcome-invalid-enum` and
`outcome-external-to-confirmed`. The preserved output is
[`evidence/before/before-run.json`](evidence/before/before-run.json); the run log
is [`evidence/before/before-run.txt`](evidence/before/before-run.txt).

> The before-harness is a faithful pure-Roslyn transcription of the cited
> checker's `Audit`, helpers, target table and mutation set. Only the
> plugin-dependent *observation* sections of the Q2 challenge (same-context
> positives, declaration conflicts, signature gaps, public-extraction
> adversaries) are omitted; none participates in `Audit` or changes the F04
> result. Provenance: Q1 `oracle/Program.cs` (`a39d087e…`), Q2
> `challenge/Program.cs` (`2cad757e…`).

## 2. Passing-after (corrected semantic oracle)

```powershell
Set-Location D:\projects\Andirit\docs\planning\ci1-scope-r2\analysis\oracle
dotnet restore OracleR2.csproj --ignore-failed-sources -p:NuGetAudit=false --source C:\Users\Laszlos\.nuget\packages
dotnet run --no-restore -c Release --project OracleR2.csproj -- `
  D:\projects\Andirit `
  D:\projects\Andirit\docs\planning\ci1-scope-r2\evidence\after\after-run.json *>&1 |
  Tee-Object -FilePath D:\projects\Andirit\docs\planning\ci1-scope-r2\evidence\after\after-run.txt
```

Expected summary:

```json
{"Sites":32,"Failures":0,"Mutations":12,"AcceptedControls":1,"PositiveControlAccepted":true,"Collision":true}
```

Interpreting the output:

- `Sites=32`, `Failures=0`: the unchanged manifest passes with zero errors.
- `Mutations=12`, `AcceptedControls=1`: eleven adversarial mutations are rejected
  and the single positive control (`P01`) is accepted.
- Inspect `mutations[].rejectedForIntendedReason` — all must be `true`; and
  `mutations[].unrelatedValidationFailedFirst` — all must be `false`.
- `adversarialScenarios[0].requirementSatisfied = true`: the F01 collision stays
  external.

Preserved output: [`evidence/after/after-run.json`](evidence/after/after-run.json);
run log: [`evidence/after/after-run.txt`](evidence/after/after-run.txt).

## 3. Build the side-by-side comparison and hashes

```powershell
python D:\projects\Andirit\docs\planning\ci1-scope-r2\analysis\compare.py
python D:\projects\Andirit\docs\planning\ci1-scope-r2\analysis\hash_evidence.py
```

- `compare.py` writes [`evidence/mutation-comparison.json`](evidence/mutation-comparison.json).
- `hash_evidence.py` writes [`evidence/evidence-hashes.json`](evidence/evidence-hashes.json).

## 4. What to verify independently (Q3)

1. The four named Q2 regressions show `before=false, after=true` in
   `mutation-comparison.json`.
2. Each corrected rejection's `correctedOraclePrimaryReason` matches the intended
   code in [`mutation-matrix.md`](mutation-matrix.md) §2.
3. `P01` is the only accepted mutation.
4. `baseline.failures = 0` and `semanticIdentity.allRowsDerivedMatchesDeclared = true`.
5. `adversarialScenarios[0].documentedIdCoincides = true` **and**
   `boundTargetIsEligibleLocal = false`.
6. The source-input SHA-256 values in `after-run.json` match the canonical
   fixtures (`git hash-object`/raw bytes) independently.
7. Re-derive a sample row by hand from source and Roslyn (for example C10 and
   C11) and confirm the derived classes.

## 5. Rebuild-from-scratch note

The two JSON outputs are deterministic for the pinned fixtures, Roslyn 5.9.0 and
SDK 10.0.401. `bin/`/`obj/` are build artifacts and are not part of the
deliverable evidence. Re-running overwrites the two JSON/log names above; the
preserved copies can be regenerated at any time from the checker sources.
