# Accepted integration delta

Baseline: canonical p6 `558f8ac82ac1b5e07da68f4a71a70240eaf38409`. Accepted implementation target: `2c3090cba6d8a56102a5c47ceb026c5b5fea3234`.

65 files changed, 14,519 insertions and 81 deletions in the accepted source-and-documentation delta. [Per-file inventory](raw/accepted-delta-numstat.txt); [complete assembled path delta](raw/integration-delta-files.txt).

| Surface | Accepted changes and provenance |
|---|---|
| EC-6B production | Relative module resolution `8aa083ce36e52f666578f5ed6b539ec194178561`; escaped-specifier/type-only remediation `ffdcdbdf57e35f94a1e459a5b393649778c21f69`; rejected-path regression `b915aed2a795f38d682fcd8289282eecd8110666`; accepted by `038295f26ce76a8747e923f9e0ef971e773c166e` |
| EC-6C production | Internal export-shape inventory `b1998d64bb7d2811be5ac1a14bda24df4b1cbef6`; structural export publisher `3bf418d372b125107058f5985a5a311ce825b156`; publication/compatibility tests `b54eb3be258656feefedd939f38f398b4fe1dd86`, `b61b45e6988b0bf2bf161f02055d2536893dd654`; accepted by `2c3090c…` |
| Graph vocabulary/public API | Additive export/module relationship kinds and property keys, supported plugin/resolver composition; no existing identity removal |
| Extraction | Structural TypeScript/JavaScript module/export extraction changes only; existing C# semantic extraction and Elixir subset exercised by full regressions |
| Persistence | No persistence implementation, store schema, native provider package or migration file changed. Lattice schema remains 2; extraction profiles p6→p8→p9 require reindexing |
| Queries | No query implementation or default changed; positive export candidates are additive. Old exact-name candidates/default neighborhood identities remain readable in the durable rehearsal |
| Incremental/publication | Accepted profile invalidation and resolution/export lifecycle; full lifecycle/failure tests plus durable repeat checks |
| CI/package metadata | No workflow or package metadata change between main and accepted source. Integration adds no publication configuration drift |
| EC-6D | Planning contracts, fixtures, analysis, Q3 and owner decisions only; no production behavior |
| Q1/Q2 integration | Original evidence-only merges, five and 43 added docs files respectively; no conflict resolution |
| HETU-INT-01 | Evidence tooling, records and contributor policy only |

The assembled candidate's `src`, `tests`, `.github` and `Directory.Build.props` matched accepted EC-6C exactly (`git diff --exit-code 2c3090c HEAD -- src tests .github Directory.Build.props`). No new implementation, rename, binary or generated build output enters main. Historical reports and fixture bytes are preserved.
