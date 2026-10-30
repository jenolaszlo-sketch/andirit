# Reproduction and root-cause evidence

Before production edits: verified local/remote main at `514cd7938afff14af20fc5e258577ac2ece6641b`, exact tree, clean tracked status and one checkout. A recoverable main bundle was created and verified at `C:/Users/Laszlos/source/repos/Solo/ec6d-rem-01/evidence/baseline.bundle`. No reset, force push, worktree or routine branch was used. An initial PowerShell Git expression needed quoted `HEAD^{tree}`; the corrected binding is retained.

The mandatory report, complete finding descriptions, all probe source/inputs/expectations, README, saved checks/logs, source and assembly bindings, historical seed evidence, accepted contracts, R16 record and producer handoffs were inspected. SHA-256 verification of the entire original manifest establishes that none of that evidence was overwritten. Project source and csproj files were copied byte-identically into this remediation's external `preserved/` directories so running dotnet would not overwrite even the original bundle's bin/obj outputs.

| Run | Before correction | After approved final corrections |
| --- | --- | --- |
| Original solution suite | 505 passed, 0 failed/skipped | All 505 retained; 38 new cases also pass (543 total) |
| Preserved main Probe | 633 checks, 24 failures | 640 checks, 2 failures, both original F08 route-evidence assertions, explicitly owner-superseded; separate supplement 15/15 |
| Preserved Authority | 40 checks, 2 failures (F10) | 40 checks, 0 failures |
| Preserved Lifecycle | 12 checks, 2 failures (F09) | 12 checks, 0 failures |

The seven extra post-Probe checks were previously unreachable after the F03 exception. Of the original 24 failing assertions, 22 pass under their original expectations and exactly two F08 expectations are explicitly superseded by the approved supplement; these are store repetitions of F08, not new independent bugs. No original probe source or expectation was changed or removed. The two original F08 assertions are reported as failing under the old summary interpretation, never reinterpreted as original passes; their owner-approved replacement checks pass separately. The original snapshots fed to Authority/Lifecycle are replaced as input by freshly migrated authentic p9-to-corrected-p10 publications: uncorrected p10 is deliberately refused until reindexed. The harness code/expectations remain identical. Actual old p10 publications are separately upgraded in P10Upgrade.

Environment: Windows 10.0.26200 x64; .NET SDK 10.0.401/runtime 10.0.12, Python 3.14.8, Node 24.21.0, TypeScript 5.9.3. A restricted-token `dotnet --info` attempt could not access the Windows Service Control Manager; rerunning the read-only environment query with authorized filesystem/runtime access succeeded. This was not a test failure. The new compiler oracle reran over the unchanged 18 extra source cases and retains F01's invalid-source diagnostics and F03/F04/F08's valid-source observations.

Commands (PowerShell; use fresh output directories):

```powershell
$rem = 'C:/Users/Laszlos/source/repos/Solo/ec6d-rem-01'
$qual = 'C:/Users/Laszlos/source/repos/Solo/ec6d-qual-01'
dotnet test D:/projects/Andirit/Andirit.slnx -c Release --no-restore
dotnet run --project "$rem/preserved/Probe/Probe.csproj" -c Release -- D:/projects/Andirit "$qual/cases.json" "$rem/evidence/FRESH-probe"
dotnet run --project "$rem/preserved/Authority/Authority.csproj" -c Release -- "$rem/evidence/controlled-migration/p10.json" "$rem/evidence/FRESH-authority"
dotnet run --project "$rem/preserved/Lifecycle/Lifecycle.csproj" -c Release -- "$rem/evidence/controlled-migration/p10.json" D:/projects/Andirit/docs/planning/ec6d/corpus/ts-interface-impact "$rem/evidence/FRESH-lifecycle"
```

Saved baseline/post commands and outcomes are in baseline-tests.log, baseline-probe.log, baseline-authority.log, baseline-lifecycle.log, post-probe-final.log, post-authority-final.log and post-lifecycle-final.log. Each output directory has checks.json. query.json in alias-extends records the original complete stack trace. Contradictory B inventories/results and malformed transport inputs are preserved per case/store. Early development attempts (script parse error, nonexistent Class constant, initial nested-discovery test failure) are retained separately; corrected runs support the reported success counts.
