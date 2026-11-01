# ADR-CI1-01-CI1A-IMPL-01-S1 — Performance Baseline

**Status:** secondary to correctness. These are single-run observations, **not**
acceptance thresholds and **not** a benchmark contract.

## 1. Workload

`OccurrenceStage1PerformanceProbe.Stage1WorkloadMeasurements`:

- One repository, one index unit, one graph node.
- 200 controlled occurrence facts across 20 targets (10 per target).
- Publication, replacement (200 new facts), bounded query (limit 50), close/reopen,
  deletion.

## 2. Environment

| Item | Value |
|---|---|
| Host | Windows 10.0.26200, X64 |
| SDK | .NET SDK 10.0.401, `net10.0` |
| Provider | `LatticeDbSharp` 0.2.0 |
| Configuration | **Debug** |
| Repetitions | 1 per operation |

## 3. Measurements

Command:

```
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Debug \
  --filter "FullyQualifiedName~Stage1WorkloadMeasurements" \
  --logger "console;verbosity=detailed"
```

Observed output:

```
perf publication_ms=112.5 facts=200 targets=20 units=1
perf replace_ms=50.1 facts=200
perf query_ms=3.8 examined=10 total=10
perf reopen_ms=50.6
perf delete_ms=35.7
```

Interpretation:

- The bounded query examined **10** page entries for a 10-match target, not the 200
  stored facts and not unrelated targets.
- Publication includes graph completion + occurrence unit/page/meta writes in one
  transaction.
- Reopen includes LatticeDB open, graph replay, and occurrence meta/unit/page restore.

## 4. Not measured / not claimed

- Persistent occurrence-storage growth (file bytes) was not separately instrumented.
- No Release-build, repeated-run, multi-process or large-repository benchmark.
- No optimization was implemented. In particular, the proposed native-node-ID page
  optimization is **out of scope** and was not added.
- These numbers must not be used as acceptance thresholds without an accepted benchmark
  contract.
