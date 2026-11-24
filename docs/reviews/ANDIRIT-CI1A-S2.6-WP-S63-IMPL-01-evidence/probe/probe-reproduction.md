# Reviewer probe reproduction notes

Commission: ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01 · Date: 2026-11-24

## What this is

`Program.cs` implements the eight reviewer diagnostic probes (A–H) named in the S6.3 entry
qualification plan §4.4. They were prepared by the **producer** of the S6.3 implementation and are
included so the independent qualification commission has a concrete starting point.

**They are not independent evidence.** The reviewer must re-derive the expectations from the
governing contracts and re-execute (or rewrite) these probes in its own session — that is
qualification gate G-5.

## How to run

```powershell
dotnet build docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/probe/ReviewerProbes.csproj -c Release
& docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/probe/bin/Release/net10.0/Andirit.S63.ReviewerProbes.exe
```

Each probe runs 3 times; every repetition prints `PASS`/`FAIL` with the observed values. The
process exits 0 only when all 24 repetitions pass. `probe-receipt.txt` is the producer's own run
output (Windows x64, 2026-11-24) and is provided for provenance only.

## Probe → obligation map

| Probe | Obligation | Mechanism (public API only) |
| --- | --- | --- |
| A | D4-1/D4-2 | query pinned to a superseded generation → `StalePublication`, zero occurrences |
| B | D4-2 | durable meta row deleted → explicit refusal at open (`CodeOccurrenceSchemaException`, identified by name because the type is internal) |
| C | D4-4 | rejected conflicting staging → prior publication and index intact |
| D | D4-6 | older generation refused; newest retained; native meta bound to the newest run |
| E | D4-7 | ordinary reopen reproduces identical occurrence query results |
| F | D4-7 | rejected staging routes through the durable-authority rebuild; index reproduced exactly |
| G | D4-10 / file-lock exclusivity | a second open fails while a writer holds the native lock |
| H | S-02-7 | pinned native surface (270 public members) exposes no interleaving/coordination/hook seam |

## Design constraints (deliberate)

- **Public API only.** The production fault-injector seam is `internal` to `Andirit.LatticeDb`
  (`InternalsVisibleTo` is restricted to the test assembly). Widening it would have been a
  production change, which condition C-S63-1 forbids. Probes C and F therefore trigger failure and
  recovery through the public contract (a rejected conflicting staging), which routes through the
  same store recovery path.
- **No solution membership.** The harness is excluded from `Andirit.slnx` (evidence tooling, same
  pattern as the Stage-6 entry probe harness) and carries its own `Directory.Build.props`.
- The harness references the production projects read-only and never modifies them.
