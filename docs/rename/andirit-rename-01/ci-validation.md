# Regression and CI qualification

Local Windows Release restore/build PASS, zero warnings/errors; all **391 tests pass**, none skipped: core 160, CSharp 47, TreeSitter 157, LatticeDb 27. Baseline 388 plus three new tests freezing historical storage/native labels, provider/profile identifiers and loose C# assembly evidence. Existing public API assertions are mapped, not weakened; no test removed. Existing cancellation, resolver failure, store failure and native grammar regressions pass.

Formatting verification passes for solution and standalone packed consumer. Active documentation local links and all project references resolve; Git whitespace check passes. Exact commands/transcripts are under raw/. Four unchanged line+branch coverage thresholds pass (55/75/60/70 respectively).

| Gate | Line | Branch |
|---|---|---|
| core | 80.59% | 63.05% |
| csharp | 93.63% | 80.95% |
| treesitter | 89.32% | 75.36% |
| latticedb | 90.49% | 77.68% |

Full durable and packed-consumer comparisons pass. Hosted CI is pending this source commit: required windows-latest, ubuntu-latest, macos-latest test/coverage matrix and Ubuntu pack/all-package native consumer job. No reduction to matrix, thresholds or triggers. Publication remains restricted to configured v-tag/manual triggers; none will be invoked.
