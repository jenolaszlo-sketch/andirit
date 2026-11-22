# Reproducing the Stage 6 entry probes

**Commission**: `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01`
**Baseline**: `b5a81f81fade9807f706b4586329efe416cf1558` (production `src/` unchanged since the
qualified implementation `eafd632…`).

The harness in this directory is a standalone, non-packed project. It is **not** part of
`Andirit.slnx`; it references the production projects
(`src/Andirit.Abstractions`, `src/Andirit`, `src/Andirit.LatticeDb`) and never modifies them.
`Directory.Build.props` in this directory is intentionally empty so that the harness does not
inherit the repository-root build policy (the referenced production projects still import it
normally).

## 1. Prerequisites

- .NET SDK 10.0.x (the receipts in this package were produced with runtime 10.0.12 on Windows
  x64, 10.0.26200).
- Network access for the initial restore of `LatticeDbSharp 0.2.0` (or a warm NuGet cache).
- A writable scratch directory for the scratch databases and receipts.

## 2. Build

```powershell
cd docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/probe
dotnet build Stage6Probe.csproj -c Release
```

## 3. Run every scenario (one process per scenario)

```powershell
powershell -ExecutionPolicy Bypass -File run-probes.ps1 -Work <scratch/work-dir> -Out <receipt-dir>
```

(`pwsh -File run-probes.ps1 …` works identically where PowerShell 7 is installed; the receipts in
this package were produced with Windows PowerShell 5.1.)

The driver writes one receipt per scenario plus `probe-summary.txt` and `environment.txt`, and
records each scenario's exit code and check counts. A scenario exits `0` only when every check in
it passed.

| Scenario | What it establishes |
| --- | --- |
| `api-surface` | the managed cancellation surface of `LatticeDbSharp 0.2.0` (public member census, transaction surface, native asset hashes) |
| `pre-invocation` | cancellation before native invocation: no native transaction, no durable change |
| `managed-wait` | cancellation of a managed wait while a native write is in flight |
| `native-window` | cancellation requested inside the open native transaction: not observed; the commit proceeds |
| `post-commit` | cancellation requested after the irreversible commit: not reported as cancellation |
| `commit-unknown` | the accepted unknown-commit-outcome semantics (report, quarantine, corroborate by reopen) |
| `dispose-race` | concurrent cancellation and disposal: disposal waits for the in-flight native write |
| `cleanup` | the native handle is released after disposal |
| `control` | the successful non-cancelled control path |
| `reopen` | reopen and durable-state consistency (publication identity, snapshot identity, consistency flag, unit count) |

A single scenario can also be run directly:

```powershell
dotnet run --project Stage6Probe.csproj -c Release --no-build -- native-window <scratch/work-dir>
```

## 4. Native ABI and capability scan (read-only, static)

```powershell
python native-abi-scan.py <path-containing-the-native-assets> `
    <path-to>/build/LatticeDbSharp/capabilities.json `
    <output-file>
```

The native assets are the `runtimes/{win-x64,linux-x64,osx-arm64}/native/` files copied next to the
harness by the build (the `LatticeDbSharp` native assets are copied into the output directory). The
scan parses the PE export table and the ELF dynamic symbol table, performs an identifier scan for
the Mach-O binary, enumerates the declared capability features, and reports the pinned upstream
identity.

## 5. Git-derived receipts

```powershell
python preflight-receipt.py <repo> <baseline-commit> <output-file>
python contract-receipt.py <repo> <baseline-commit> <output-file>
```

Both read only Git objects, so their outputs can be re-derived independently of any working tree.

## 6. Notes

- The harness reaches the production store's internal fault-injector constructor **by reflection**
  (`GetConstructor(NonPublic | Instance, …)`), which is the same seam the repository's own test
  project uses through `InternalsVisibleTo("Andirit.LatticeDb.Tests")`. If that constructor ever
  changes, the harness fails loudly instead of silently degrading.
- Every scenario writes only into the caller-supplied scratch directory. The repository is never
  modified by a probe run.
- The `probe/bin` and `probe/obj` build outputs are not part of the sealed package.
