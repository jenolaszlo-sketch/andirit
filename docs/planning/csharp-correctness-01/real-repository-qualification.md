# Real-repository qualification — ANDIRIT-CSharp-CORRECTNESS-01

## Scope of this qualification

Real-source extraction was exercised through the permanent regression
`ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract`
(`tests/Andirit.CSharp.Tests/CSharpCorrectnessRegressionTests.cs`), which reads
the actual repository working tree and runs the production plugin over the
three originally-failing projects:

- `src/Andirit.Abstractions` (14 `.cs` + `.csproj`)
- `src/Andirit` (31 `.cs` + `.csproj`)
- `src/Andirit.CSharp` (8 `.cs` + `.csproj`)

Discovery assigns sources by project directory and honours the real
`ProjectReference` graph (`Andirit.CSharp → Andirit → Andirit.Abstractions`),
so dependency-ordered extraction and cross-project resolution run against real
compilations, not synthetic fixtures.

## Baseline result (exact commit `5c89ac0`)

The same three-project input **throws before relationship extraction**:

```
System.InvalidCastException: Unable to cast object of type 'Andirit.CodeTextProperty'
to type 'Andirit.CodeTextListProperty'.
   at Andirit.CSharpCodeGraphPlugin.GraphBuilder.AddSymbol(...) GraphBuilder.cs:line 314
   at Andirit.CSharpCodeGraphPlugin.GraphBuilder.AddDeclarations(...) GraphBuilder.cs:line 155
   at Andirit.CSharpCodeGraphPlugin.Session.ExtractAsync(...) Session.cs:line 92
```

This is the F02 reproduction (R1 `reproduction.md` §F02) re-executed by this
packet through the regression harness.

## Remediated result

Extraction completes; the previously fatal symbol is correct:

| Assertion | Result |
| --- | --- |
| `T:Andirit.CSharpCodeGraphPlugin` logical nodes | exactly **1** |
| Physical declarations of that symbol | **6** (`CSharpCodeGraphPlugin.cs`, `.Symbols.cs`, `.GraphBuilder.cs`, `.Session.cs`, `.PropertyFactory.cs`, `.Compilation.cs`) |
| `doc-summary` of the logical node | `Deterministic repository-aware C# extraction powered by Roslyn.` (declared only in `CSharpCodeGraphPlugin.cs`; the five undocumented parts contribute nothing and do not fail or erase it) |
| `T:Andirit.CSharpProjectDiscovery` | exactly 1 node, 1 declaration |
| Wider extraction progression | `calls` candidates > 0 — relationship extraction is reached and produced |
| Extraction failures | none |

The separate comparison harness (`partial` fixture) additionally shows, for a
documented sealed partial with a consumer:

- 10 nodes / 9 declarations / 19 edges emitted;
- one `Example.P` node with merged `doc-summary` and `modifiers=sealed`;
- two physical declarations (`src/P.One.cs` 1:20–53, `src/P.Two.cs` 2:20–53);
- the implicit-constructor call `new P()` is honestly counted `ExternalTargets=1`
  (it is not an indexed declaration) — no fabricated node, no guessed edge.

## Honest boundaries

- This qualification is **Windows-only local execution**. Cross-platform
  (Linux/macOS) verification via CI remains blocked pending separate
  publication authorization, per the assignment. No cross-platform success is
  claimed.
- "Wider extraction progresses" means the originally failing input now reaches
  and produces relationships with zero candidates lost to the previous
  exception. It does **not** claim that every subsequent wider-extraction
  problem is solved: no further failure was observed on these three projects,
  and any future failure must be classified separately with preserved
  evidence.
- Partial-method definition/implementation unification remains unqualified and
  is not exercised by real-repository qualification.
- The real-source test reads the working tree via `RepositoryRoot` (same
  pattern as `tests/Andirit.Tests/InterfaceImpactTests.cs`); it asserts exact
  symbol identities so accidental source edits fail loudly rather than
  silently passing.
