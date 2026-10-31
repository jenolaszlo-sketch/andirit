# P-DEF-01 analysis — false semantic target confirmation

## Known failure (reproduced)

Independent fixture (Q1 F01, reproduced by R1 and again by this packet):

- `A/Shadow.cs`: `namespace System; public static class Console { public static void WriteLine(string value) { } }`
  → indexed declaration with documentation ID `M:System.Console.WriteLine(System.String)`.
- `Z/App.cs`: `System.Console.WriteLine("outside")`, project `Z` does **not** reference `A`.

The invocation binds to the genuine platform `System.Console` in external
assembly `System.Console, Version=10.0.0.0` (0 source locations). Baseline
output nonetheless published a **resolved cross-project `calls` edge**
`App.App.Run() → System.Console.WriteLine(string)` landing on the unrelated
`A/Shadow.cs` declaration (and two `references` edges to the shadow's
`System.Console`/method nodes). See
[`evidence/comparison/mixed.baseline.json`](evidence/comparison/mixed.baseline.json).

## Root cause (exact mechanism, baseline `5c89ac0`)

1. `CanonicalSymbolKey` (`CSharpCodeGraphPlugin.Symbols.cs:63`) returns the
   Roslyn documentation comment ID whenever one exists. The ID encodes
   namespace/type/member shape but **no assembly or compilation identity**.
2. `GraphBuilder.AddSymbol` registered every symbol in the run-wide
   `RunSymbols.GlobalNodes` map keyed by that canonical ID alone
   (`Session.cs:230–253`). The map had no project, assembly or definition
   provenance; a second project registering the same key with a different node
   id only set a global `Ambiguous` flag.
3. `TryResolveTarget` (`GraphBuilder.cs:460–486`) resolved in this order:
   project-scoped node, then the global map. If exactly one project had
   registered the key, the global match was accepted as `Found` — with no
   check that the bound target's defining compilation/assembly corresponds to
   that indexed declaration. **The absence of a competing indexed declaration
   was treated as proof of identity.**

The failure was also order-dependent: when the unrelated project sorted before
the caller (F01 layout `A` → `Z`), the false edge was emitted; when the caller
sorted first, the call happened to be counted external. Project ordering is a
path-sorting artifact, not evidence.

## Correction (provider-level, not CI-1A-specific)

Implemented in `CSharpCodeGraphPlugin.Symbols.cs`, `...Session.cs`
(`RunSymbols`) and `...GraphBuilder.cs` (`TryResolveTarget`). The resolver now
proves correspondence through a chain of exact gates; any miss is fail-closed.

### 1. Definition identity (`DefinitionFingerprint`)

Every registration and every bound target carries a compilation-independent
definition fingerprint: symbol kind plus, per symbol kind, `MethodKind`,
metadata name, arity, fully-qualified parameter type identities and return
type (or `TypeKind`/metadata name/arity for types, etc.). Generics and reduced
extension methods are normalized first (`Normalize` → `OriginalDefinition` /
`ReducedFrom`), so a constructed `Store<string>.Get` or an instance-syntax
`input.Shout()` fingerprint-equals the indexed definition. A canonical
documentation ID is used for **lookup only**; the fingerprint must also match
or the resolution is refused.

### 2. Project provenance in the run-wide map

`RunSymbols.Register(canonicalKey, projectPath, nodeId, fingerprint,
declarationPath)` stores, per canonical key, per project: the node id, the
definition fingerprint, and the set of physical declaration paths (partial
parts included). A same-key/same-project registration with a different node id
or fingerprint marks that project's entry **conflicting**. There is no longer
a canonical-ID→nodeId global shortcut.

### 3. Exact compilation correspondence

`GraphBuilder` receives the current `CSharpCompilation` and the map of
eligible dependency compilations (the direct project references that were
compiled before this project, exactly those passed to Roslyn as
`CompilationReference`s). Resolution order in `TryResolveTarget`:

| Gate | Rule |
| --- | --- |
| Same compilation | `SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, compilation.Assembly)` → resolve only the project-scoped node for this project; if absent (e.g. implicit constructor, non-indexed symbol) → **External**. Never falls through to another project's canonical match, so a same-doc-ID private local declaration cannot capture a dependency-bound call. |
| Exact dependency | target's containing assembly object equals one dependency compilation's `Assembly` → resolve that project's registration (canonical + fingerprint + not conflicting). Merged namespace case: a namespace declared by several referenced assemblies has `ContainingAssembly == null`; the local node wins if the current project declares it, otherwise any indexed contributor forces **Ambiguous**. |
| Identity fallback (metadata symbols) | full `AssemblyIdentity` equality, definition fingerprint equality, and — when the target carries source locations — declaration-path provenance in exactly one candidate project. Source-location mismatch → **External**; multiple provenance matches → **Ambiguous**. |
| Nothing matches | **External** (counted, never emitted). |

Assembly-name equality alone is never sufficient: the same-assembly-name
adversarial fixture resolves only through the exact `CompilationReference`
assembly object and project provenance (see regression
`ExtractAsync_SameAssemblyNameUsesProjectProvenanceNotNameEquality`).

### Fail-closed behaviour

| Outcome | When |
| --- | --- |
| `Found` | Exact defining-compilation object or provenance-proven declaration with matching fingerprint. |
| `External` | Target belongs to a non-indexed assembly, or to an indexed assembly without an indexed declaration, or provenance mismatch. Counted in `ExternalTargets`, no edge. |
| `Ambiguous` | Multiple eligible provenance matches, merged cross-project namespace with indexed contributors, or conflicting same-project registrations. Counted in `AmbiguousTargets`, no edge. |

No external graph node is fabricated; no valid relationship is quietly
suppressed (the `ambiguous` fixture shows previously suppressed **valid**
cross-project calls/imports being restored, not lost).

## Boundary control

- No new public API; `ICodeGraphPlugin` surface unchanged
  (`CSharpPublicApiContractTests` passes).
- Node IDs (`csharp:symbol:<hash(project.Path + "\n" + canonical)>`) and edge
  IDs (`csharp:<hash(kind|source|target|discriminator)>`) are unchanged, so
  valid legacy identities are byte-stable — proven by the byte-identical
  `cross` fixture output
  (`bf8681ca…0ab72` before and after).
- Only deliberately incorrect relationships changed; see
  [`compatibility-assessment.md`](compatibility-assessment.md).
