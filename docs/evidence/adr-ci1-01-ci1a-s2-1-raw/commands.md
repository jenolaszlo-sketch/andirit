# Executed commands and source binding

Working directory for solution/test/build/Git commands: D:\projects\Andirit.
Probe commands ran from C:\Users\Laszlos\source\repos\Solo.
Client date: 2026-11-02, Asia/Manila. TRX filenames retain their original host timestamps.

Baseline (before production or plan edits):
```powershell
dotnet test Andirit.slnx --no-restore --logger 'trx;LogFilePrefix=baseline' --results-directory C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-1\baseline
```

Final complete test execution, immediately before implementation checkpoint:
```powershell
dotnet test Andirit.slnx --no-restore --logger 'trx;LogFilePrefix=final' --results-directory C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-1\final-regression
```

Final build and runtime capture:
```powershell
dotnet build Andirit.slnx --no-restore -clp:Summary
dotnet --info
```

Final fixture execution (each dotnet run starts a separate process):
```powershell
dotnet run --project adr-ci1-01-ci1a-s2-1\production-probe\ProductionProbe.csproj -- run:one
dotnet run --no-build --project adr-ci1-01-ci1a-s2-1\production-probe\ProductionProbe.csproj -- run:two
python -X utf8 adr-ci1-01-ci1a-s2-1\verify-production.py
```

Original R2 differential diagnosis:
```powershell
dotnet run --project adr-ci1-01-ci1a-s2-1\reference-probe\ReferenceProbe.csproj
dotnet run --no-build --project adr-ci1-01-ci1a-s2-1\reference-probe\ReferenceProbe.csproj
python adr-ci1-01-ci1a-s2-1\verify-probe.py
```

Probe stdout/stderr is retained as *.log. Test runner console streams were not
separately redirected; original complete test result records and failure stack
traces are retained in raw TRX files. Build stdout/stderr are captured. All
645 final results are classified Passed, with no failed/skipped/not-executed
results. Baseline 594/594; intermediate records remain historical diagnostics.
The final whitespace-only EOF cleanup does not change test behavior; the final
commit source is the reproduction authority. No native or occurrence identity
consumer code changed between the accepted baseline and implementation.
