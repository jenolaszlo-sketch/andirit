# Regression and CI qualification

Local Windows Release solution restore/build PASS, zero warnings/errors; **391 tests PASS**, none skipped: core 160, CSharp 47, TreeSitter 157, LatticeDb 27. Test count is unchanged from the accepted baseline: no test added, removed or weakened. The three prior explicit persistence guards remain active. Existing cancellation, resolver/store failure and native grammar regressions pass.

Solution and packed-consumer formatting verification pass. The formatter corrected line endings around the existing loose-source C# comment; no tokens or behavior changed. Core/CSharp/TreeSitter/LatticeDb thresholds remain 55/75/60/70 for line and branch. Local results:

| Gate | Line | Branch |
|---|---|---|
| core | 80.59% | 63.05% |
| csharp | 93.63% | 80.95% |
| treesitter | 89.32% | 75.36% |
| latticedb | 90.49% | 77.68% |

391-test regression command: `dotnet test Andirit.slnx -c Release --no-build --no-restore`. Restore/build/format/coverage/pack commands are listed below; raw transcripts, TRX, Cobertura, databases and packages stay outside the checkout at `C:/Users/Laszlos/source/repos/Solo/andirit-rename-02/`.

```text
dotnet test Andirit.slnx -c Release --no-build --no-restore --logger trx --results-directory C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/test-results
dotnet format Andirit.slnx --verify-no-changes --no-restore
dotnet test Andirit.slnx -c Release --no-build --no-restore --logger trx --results-directory C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/test-results
dotnet format Andirit.slnx --verify-no-changes --no-restore
dotnet test tests/Andirit.Tests/Andirit.Tests.csproj -c Release --no-build --no-restore /p:CollectCoverage=true /p:CoverletOutput=C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/coverage-core/ /p:CoverletOutputFormat=cobertura /p:Include=[Andirit]*%2c[Andirit.Abstractions]* /p:Threshold=55 /p:ThresholdType=line%2cbranch
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj -c Release --no-build --no-restore /p:CollectCoverage=true /p:CoverletOutput=C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/coverage-csharp/ /p:CoverletOutputFormat=cobertura /p:Include=[Andirit.CSharp]* /p:Threshold=75 /p:ThresholdType=line%2cbranch
dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj -c Release --no-build --no-restore /p:CollectCoverage=true /p:CoverletOutput=C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/coverage-treesitter/ /p:CoverletOutputFormat=cobertura /p:Include=[Andirit.TreeSitter]* /p:Threshold=60 /p:ThresholdType=line%2cbranch
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Release --no-build --no-restore /p:CollectCoverage=true /p:CoverletOutput=C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/coverage-latticedb/ /p:CoverletOutputFormat=cobertura /p:Include=[Andirit.LatticeDb]* /p:Threshold=70 /p:ThresholdType=line%2cbranch
dotnet pack Andirit.slnx -c Release --no-restore --output C:\Users\Laszlos\source\repos\Solo\andirit-rename-02/packages
```

Strict comparisons pass for all 139 public types/members, mapped C# source/test files, unchanged build versions, historical evidence, genuine-store facts/queries/provenance/publications and isolated package source/dependencies. Active local Markdown links and all project references resolve; source whitespace is checked before commit. Native binary filenames/content remain original.

Hosted CI pending on the committed source: Windows/Ubuntu/macOS test+coverage and Ubuntu six-package/native-consumer smoke. Matrix, coverage thresholds, test assertions and trigger restrictions remain unchanged. The original five-project publish set remains exact; no tag/manual publish/release operation is invoked.
