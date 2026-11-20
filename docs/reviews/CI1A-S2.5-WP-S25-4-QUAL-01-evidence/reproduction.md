# Reproduce the qualification

Use the Windows x64 native toolchain and package versions in the sealed receipts. Build a disposable copy of probe/Qual.csproj with dotnet build -c Release -p:RepoRoot=D:/projects/Andirit. The resulting DLL is Andirit.CSharp.Tests.dll. Run dotnet with the DLL and four positional arguments in this order, using the phase, a disposable native database, the output JSON path, and the fixture or receipts directory.

1. Use the fixture directory for the b3-publish, b3-reopen, b3-missing and b3-ambiguous phases. Reopen the publish database in a separate process. Use separate disposable paths for negative controls. The publish and reopen invocations return 1 for the M5.2 external-classification finding.
2. Use the new receipts directory as the fourth argument for b6-publish, b7-conflict, b7-reopen, b6-delete-a, b6-reopen-a, b6-delete-b and b6-reopen-b. Each phase runs in a separate process on the same contributor store. Name the first output b6-publish.json for the conflict reopen comparison.
3. Run d3 and d4-schedule against their own isolated fresh stores, passing the fixture directory. Copy the d3 result and its WAL, if present, to three independent destinations before running d5-namespace, d5-schema and d5-page against those respective copies.
4. The historical and managed-recovery PowerShell scripts record the executed replay procedures. Set the output path to a new receipts folder on reuse. Keep the historical store and WAL masters read-only and stage only exact byte copies.
5. Gate commands and exits are recorded in the logs. Verify the nine historical seals with verify-history.ps1. Run python verify-manifest.py before adding any generated files to the sealed package.
