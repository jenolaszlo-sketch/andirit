# Regression and CI qualification

Local Windows Release restore/build PASS, zero warnings/errors; all **391 tests pass**, none skipped: core 160, CSharp 47, TreeSitter 157, LatticeDb 27. Baseline 388 plus three new tests freezing historical storage/native labels, provider/profile identifiers and loose C# assembly evidence. Existing public API assertions are mapped, not weakened; no test removed. Existing cancellation, resolver failure, store failure and native grammar regressions pass.

Formatting verification passes for solution and standalone packed consumer. Active documentation local links and all project references resolve; Git whitespace check passes. Exact commands/transcripts are under raw/. Four unchanged line+branch coverage thresholds pass (55/75/60/70 respectively).

| Gate | Line | Branch |
|---|---|---|
| core | 80.59% | 63.05% |
| csharp | 93.63% | 80.95% |
| treesitter | 89.32% | 75.36% |
| latticedb | 90.49% | 77.68% |

Full durable and packed-consumer comparisons pass. Hosted [source CI run 40805404393](https://github.com/jenolaszlo-sketch/penghou-andirit/actions/runs/40805404393) PASSED on exact rename commit `d2a57c9ea28d3b74b8b1a8e46f12cc9d23debba4`: windows-latest, ubuntu-latest, macos-latest test/coverage and Ubuntu pack/all-package native consumer. This was verified both before and after repository rename. The final evidence-only commit also triggers this same full matrix; its exact result is bound in the external final Git identity report at handoff. No reduction to matrix, thresholds or triggers. Publication remains restricted to configured v-tag/manual triggers; none will be invoked.

Post-relocation restore/build, all 391 tests, final pack and fresh-cache consumer also pass from D:/projects/Andirit; raw final-* evidence and canonical SourceLink capture are included. Hosted runner deprecation/capacity notices are informational; no dependency upgrades were introduced.
