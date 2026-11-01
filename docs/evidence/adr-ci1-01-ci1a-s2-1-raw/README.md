# S2.1 raw evidence and reproduction

Producer packet, 2026-11-02 (Asia/Manila). Independent Q1 remains pending.
Implementation commit c02d5eae157653ba7f123b98aceccb01a0b542a0, tree
a53a076dbb482062973f70fd11bf4261cc5368f3. No fetch or push performed.

Run from D:/projects/Andirit with .NET SDK 10.0.401, runtime 10.0.12,
Roslyn 5.9.0 and Python 3.14.8. Restore the existing solution dependencies if
not already present. Probe projects use the canonical checkout's built DLLs;
the console uses the existing test friend assembly name, adding no public hook.

```powershell
dotnet test Andirit.slnx --no-restore --logger 'trx;LogFilePrefix=reproduction' --results-directory docs/evidence/adr-ci1-01-ci1a-s2-1-raw/reproduction
dotnet run --project docs/evidence/adr-ci1-01-ci1a-s2-1-raw/reference-probe/ReferenceProbe.csproj
dotnet run --project docs/evidence/adr-ci1-01-ci1a-s2-1-raw/production-probe/ProductionProbe.csproj -- run:one
dotnet run --no-build --project docs/evidence/adr-ci1-01-ci1a-s2-1-raw/production-probe/ProductionProbe.csproj -- run:two
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-1-raw/verify-probe.py
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-1-raw/verify-production.py
```

The Python scripts independently check the frozen captured JSON logs. To check
a new execution, save its stdout under a different name and compare to the
captured logs; preserve originals. Identity stability is bounded to equivalent
inputs including R2's absolute PE paths/content. Different runtime files/paths
can legitimately change identities. Timings are in stderr and excluded from
manifest comparison. The two final production stdout logs are byte-identical
despite separate processes and different run IDs.

The original R2 source copy is unchanged and SHA-bound. Its diagnostic exposes
an identity representation collision, not a cryptographic SHA-256 collision.
The new producer fixture confirms int/string conditional API propagation under
the explicitly amended compctx2 contract.

Raw TRXs include baseline, intermediate failures, focused fixes, the first
643-case full run and final 645-case full run. Use the final_* files for final
status; do not aggregate repeated executions as distinct tests. The 594-case
baseline and final645 have no failures/skips. Earlier failure records are
retained and explained in the regression report.

source-evidence-manifest.json binds canonical Git blob bytes for code and staged
documentation/evidence, identifies the exact baseline/contract/implementation
trees, and excludes itself. Raw files have -text attributes to preserve original
bytes across checkouts. Working-file hashes also record the source bytes on this
host; Git newline normalization may give code files different working hashes on
another checkout. The final evidence-only commit must not contain its own SHA.
