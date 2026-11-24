# S6.3 worker-process contract (standalone copy)

Commission: ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01 · Date: 2026-11-24

`tests/Andirit.LatticeDb.Tests.Worker` is a standalone console application (already part of
`Andirit.slnx`). S6.3 adds a `publish-complete` mode and keeps the original `hold` behaviour
unchanged so S6.2's D-07 continues to work as qualified.

## Launch

```
dotnet exec tests/Andirit.LatticeDb.Tests.Worker/bin/<config>/net10.0/Andirit.LatticeDb.Tests.Worker.dll \
  --db <database path> \
  --repo <repository id> --run <index run id> --unit <unit id> --node <node id> \
  --mode hold|publish-complete \
  [--signal <signal base path>] [--target <target name>] [--occurrences <n>]
```

## Modes

### `hold` (S6.2 D-07 behaviour, preserved)

1. Opens the store on `--db`.
2. Upserts the repository, stores a Running index run, stages one graph unit.
3. If `--signal` is supplied, writes `<signal>.ready` and then blocks until `<signal>.go` appears
   (60 s deadline) — this lets a test deterministically observe that the native file lock is held.
4. Returns; the store is disposed and the lock released only then.

The staged run is never completed, so the staged row legitimately remains in `HetuStage` and is
invisible to queries (D4-5 territory).

### `publish-complete` (S6.3 / F-QU-01 fix)

1. Opens the store on `--db`.
2. Upserts the repository, stores a Running index run.
3. Stages one index unit that carries `--occurrences` invocation occurrence facts for the target
   derived from `--repo` + `--target`, with `Produced` coverage (declared = verified = 1 unit).
4. **Completes the run** (`CompleteIndexRunAsync`) so the publication actually commits — this is
   the correction F-QU-01 named (the previous worker only ever staged).
5. Reads back the latest publication; if absent, exits 3.
6. Prints `PUBLISHED|<indexRunId>|<snapshotIdentity>` and exits 0.

## Test-side usage

| Case | Sequence |
| --- | --- |
| D-05 | hold (+ signal) → second open from the test **fails** (negative control) → GO → publisher completes → reopen: publication committed, healthy; `HetuStage` = the hold run's staged row only; `HetuUnit` = the published unit |
| P-15 | hold (+ signal) → second open fails → GO → publisher completes → fresh reopen: publication + 3 occurrences queryable; hold repository reports `AnalysisUnavailable` |

## Locating the worker

Tests resolve the repository root by walking up from `AppContext.BaseDirectory` until
`Andirit.slnx` is found, then probe `tests/Andirit.LatticeDb.Tests.Worker/bin/{Release,Debug}/net10.0/`
for the worker assembly. If it is not built, the tests **fail loudly** with an instruction to build
the solution — there are no silent skips.

## Honesty boundaries

- The worker performs real native operations through the production `LatticeCodeGraphStore`; it is
  not a mock, and it does not modify production code.
- File-lock exclusivity is exercised against the real OS lock held by a real separate process.
- The worker never reports a managed timeout or an abandoned task as a native cancellation, and it
  never represents an unknown commit outcome as a confirmed rollback.
