# Frozen persisted meaning

Schema version remains 2. Storage labels: `HetuMetadata`, `HetuRepository`, `HetuRun`, `HetuIndexState`, `HetuUnit`, `HetuStage`, `HetuBaseline`; native labels `HetuGraphNode`, `HetuMirror`; native property `hetu_json`.

Plugins: `penghou.hetu.csharp`, `penghou.hetu.tree-sitter.typescript`, `penghou.hetu.tree-sitter.javascript`, `penghou.hetu.tree-sitter.elixir`. Resolver: `penghou.hetu.tree-sitter.ecmascript-module-resolver`; export publisher: `penghou.hetu.tree-sitter.ecmascript-export-surface`. Preserve `Hetu.Loose.CSharp`: it is an emitted assembly-name graph property for loose C# input, not the library assembly identity.

TS/JS extraction p9, resolver revision 2 / relative-v1, exports1.local-v1, grammar pins and ABI versions unchanged. Provider versions derive from informational/package version excluding Git build metadata; keeping `0.2.0-preview.6` avoids planner invalidation. Assembly names never enter these provider IDs. Snapshot discriminators use explicit JSON names; no serialized assembly-qualified CLR type identity is introduced.

Graph, declaration, occurrence, edge, repository, unit, export, resolver, source normalization, coordinate, publication hashing, query and command-log algorithms are unchanged. New regression tests freeze these historical identifiers. Baseline full facts, units, queries, provenance and integrity-checked snapshots are retained in raw reports; qualification results appear in durable-compatibility.md.
