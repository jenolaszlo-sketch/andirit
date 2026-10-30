# Reproduction

R1 independently reproduced the Q1 defects before writing any correction. This
document records the exact commands, the observed results, and the boundary
between reviewer (Q1) evidence and R1 reproduction.

## Environment

| Item | Value |
| --- | --- |
| OS | Windows (win32) |
| .NET SDK | `10.0.401` |
| Roslyn | `Microsoft.CodeAnalysis.CSharp 5.9.0` (shared by provider and oracle) |
| Canonical checkout | `D:/projects/Andirit` |
| Starting commit / tree | `7ab0aa7086861a368d91a79b802d538c8c206bb9` / `ffb6833e74836e2a318ab3c17e39a9c748310268` |
| Q1 review workspace | `C:/Users/Laszlos/source/repos/Solo/ci1-scope-q1` |

R1 ran the reviewer's oracle project (`ci1-scope-q1/oracle`) against the
canonical checkout but wrote output to `C:/Windows/Temp/opencode/`, so the Q1
evidence directory was **not** modified. The `probes` project was run in
`wider` mode, which throws before its write step, so it also left Q1 evidence
untouched.

## F01 — external/local canonical-ID false confirmation

Command:

```powershell
dotnet run --no-restore -c Release --project oracle/Oracle.csproj -- measured-source C:\Windows\Temp\opencode\r1-oracle.json
```

Console summary: `{"Sites":32,"Failures":0,"Mutations":5,"PositiveSites":8,"DeclarationConflicts":1}`.

R1 output SHA-256 `5e0140b14e5e689b0115c358ab544b9c629d5c11c480b2125a07d0b824dfa054`
is **byte-identical** to the reviewer's
`ci1-scope-q1/evidence/independent-oracle.json`, so the reviewer's F01/F02
adversary results are reproduced exactly rather than paraphrased.

Observed collision (project `unrelated-external-docid-collision`):

| Quantity | Value |
| --- | --- |
| Emitted call source | `App.Run()` in `Z/App.cs` |
| Emitted call location | `Z/App.cs` 1:40–75 |
| Emitted target node | `csharp:symbol:216216b606bcea326c25d5bd32d5cb8106d4f7879186dae64782acf9a9520998` |
| Emitted target declaration | `A/Shadow.cs` 1:49–94 |
| Coverage label | `calls` `CrossProjectTargets = 1`, `ExternalTargets = 0` |
| True bound target | `M:System.Console.WriteLine(System.String)` |
| True target assembly | `System.Console, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a` |
| True target source locations | `0` |

Expected: the call is external (no indexed implementation) and must not be
emitted as a resolved local call. Actual: it is emitted as a resolved call to the
unrelated local declaration, because `A/Shadow.cs` declares its own
`System.Console.WriteLine(string)` whose documentation ID is the only indexed
match.

Small-code confirmation (`CSharpCodeGraphPlugin.Symbols.cs`): `CanonicalSymbolKey`
returns the doc-comment ID, which carries no assembly identity. `GraphBuilder.cs:460`
(`TryResolveTarget`) then resolves the current-project scoped key or the global
`RunSymbols.GlobalNodes` entry keyed by that ID, without proving assembly or
definition correspondence. `Session.cs:230–253` shows the run-wide map is keyed
by canonical key only.

## F02 — documented partial-type `InvalidCastException`

Minimal adversary (name `documented-sealed-partial`), embedded in
`oracle/Program.cs:101–103`:

```csharp
["A.cs"] = "public sealed partial class P {}",
["B.cs"] = "/// <summary>A documented type.</summary>\npublic sealed partial class P {}"
```

Observed: `System.InvalidCastException: Unable to cast object of type
'Andirit.CodeTextProperty' to type 'Andirit.CodeTextListProperty'.`

Real wider-source failure:

```powershell
dotnet run --no-build --no-restore -c Release --project probes/Reproduce.csproj -- measured-source wider Andirit.Abstractions,Andirit,Andirit.CSharp
```

Observed (stack, canonical-equivalent paths in the exported `source/` snapshot):

```
Unhandled exception. System.InvalidCastException: Unable to cast object of type 'Andirit.CodeTextProperty' to type 'Andirit.CodeTextListProperty'.
   at Andirit.CSharpCodeGraphPlugin.GraphBuilder.AddSymbol(...) ...GraphBuilder.cs:line 314
   at Andirit.CSharpCodeGraphPlugin.GraphBuilder.AddDeclarations(...) ...GraphBuilder.cs:line 155
   at Andirit.CSharpCodeGraphPlugin.Session.ExtractAsync(...) ...Session.cs:line 92
   at Program.<Main>$(String[] args) ...probes/Program.cs:line 77
```

R1 source inspection of the trigger symbol:

| File | Declaration span | `doc-summary` |
| --- | --- | --- |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.Compilation.cs` | 11,1–74,2 | absent |
| `src/Andirit.CSharp/CSharpCodeGraphPlugin.cs` | 12,1–68,2 | `"Deterministic repository-aware C# extraction powered by Roslyn."` |

Both are `public sealed partial class CSharpCodeGraphPlugin`. The reviewer's
oracle `DeclarationConflicts` records the same symbol `T:Andirit.CSharpCodeGraphPlugin`
with `SameSymbol = true` and the only differing property key `doc-summary`;
both properties carry `modifiers` of `{"$type":"text","Value":"sealed"}`.

Two layers, both in baseline source:

1. **Diagnostic cast (unsafe formatting).** `GraphBuilder.cs:317–318` casts
   `existing.Properties["modifiers"]` / `node.Properties["modifiers"]` to
   `CodeTextListProperty`. But `CSharpCodeGraphPlugin.PropertyFactory.cs:59`
   stores `modifiers` as `CodeTextProperty`. (`CodeTextListProperty` is used for
   `DefineConstants` in `GraphBuilder.AddProject:61`.)
2. **Inconsistent symbol properties.** `GraphBuilder.cs:307` /
   `NodesEquivalent:366` require every property to agree across partial parts.
   `doc-summary` differs (present in one part, absent in the other), so the
   conflict branch is genuinely reached and the fix to layer 1 alone would
   expose the `InvalidOperationException` throw at `GraphBuilder.cs:314–318`
   instead of resolving the input.

Derived, not executed: because `NodesEquivalent` compares `Properties.Count`
and requires all keys to match, the two dictionaries (5 vs 6 properties) cannot
be equivalent, so layer 1's fix yields the throw, not a successful extraction.
R1 did not execute this derived step because production code must not be
modified under CI-1-SCOPE-R1.

## F03 — raw canonical bytes versus Git blob identity

Command (summarized):

```powershell
git config --get core.autocrlf                                   # true
# per file: raw SHA-256, CR/LF counts, git hash-object, HEAD blob
```

| File | raw SHA-256 (working tree) | CRLF lines | `git hash-object` (blob) |
| --- | --- | ---: | --- |
| `src/Andirit/CodeGraphPublicationQuery.cs` | `ae43d859…cda7d` | 201 | `4027ba24…26df` |
| `src/Andirit/CodeGraphQueryService.cs` | `f3be2fba…3b74` | 1094 | `187fb9cf…2d99` |
| `src/Andirit/InterfaceImpactQueryV1.cs` | `4bc4c364…cf37` | 561 | `cd5638c2…9dad` |
| `src/Andirit/InterfaceImpactResultV1.cs` | `2d4b25f6…31fe` | 106 | `cfd27017…f905` |

All four recorded `Sha256` values in `ci1-scope-01/evidence/source-binding.json`
equal the **raw working-tree** hash. `git hash-object` returns the pinned blob
because `core.autocrlf=true` applies clean conversion. The four files contribute
`201 + 1094 + 561 + 106 = 1,962` extra CR bytes; measured canonical mix
`382,944 − 1,962 = 380,982` LF-only Git bytes. This exactly accounts for the
reviewer's two totals. The packet's `.gitattributes` pins exact bytes only for
`fixtures/**` and `evidence/**`, not for `src/**`.

## F04 — producer manifest loop mutation blind spots

R1 reran the exact producer loop from `check-packet.ps1:29–35` against mutated
copies of `fixtures/expected-sites.json` (no files written):

| Mutation | Rejected by producer span loop? |
| --- | --- |
| unchanged | no (correct) |
| wrong target signature | **no** |
| wrong overload | **no** |
| duplicate row | **no** |
| wrong start column | yes |
| wrong end line (99) | **no** |

The loop reads only `$lines[$s[0]-1].Substring($s[1]-1, $s[3]-$s[1])`, so it
ignores the end line and every semantic field. The reviewer's independent oracle
detects all five tested corruptions (`MutationChecks` in
`independent-oracle.json`), which R1 reproduced.

## Acceptance manifest audit

R1's oracle run reports `Sites = 32`, `Failures = 0`: all 32 authored rows have
correct coordinates, intended semantic targets and expected outcomes, matching
the reviewer's `32/32 selected spans and intended semantics pass`. `C11`
carries the two intentional compile errors (`CS0121`, `CS0103`), and `C20` is
tested inactive by default and active with `CI1_EXTRA`. The 8 independent
`SameContextPositive` sites and the single `SignatureGaps` entry
(`CancellationToken` error type in
`FindDeclarationsWithProvenanceAsync`, `Andirit` project) also reproduce.

## Boundary of the reproduction

- R1 did **not** re-execute existing production regression suites. The reviewer
  independently reran C# 47/0, selected core/query/snapshot/memory 20/0 and full
  LatticeDB 27/0; R1 relies on those recorded results as Q1 evidence, not as R1
  evidence.
- R1 did **not** modify production source, tests, schemas or Q1 evidence.
- The oracle and provider share Roslyn 5.9.0, so this reproduction confirms the
  reviewer's mechanism but is not a second independent compiler.
