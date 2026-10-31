# P-DEF-02 analysis — documented partial-type extraction

## Known failure (reproduced)

Two declarations of one `sealed partial` type:

```csharp
// A.cs
namespace Example; public sealed partial class P { }
// B.cs
/// <summary>A documented type.</summary>
namespace Example; public sealed partial class P { }
```

produce one logical node identity (same scoped node id) but different
declaration-specific `doc-summary` properties. Baseline `NodesEquivalent`
(`GraphBuilder.cs:366`) requires every property to agree across parts, so the
inconsistency branch fired, and its diagnostic cast `modifiers` to
`CodeTextListProperty` (`GraphBuilder.cs:317–318`) although
`CSharpSymbolPropertyFactory.BuildProperties` (`PropertyFactory.cs:59`) stores
`modifiers` as `CodeTextProperty`:

```
System.InvalidCastException: Unable to cast object of type 'Andirit.CodeTextProperty' to type 'Andirit.CodeTextListProperty'.
   at Andirit.CSharpCodeGraphPlugin.GraphBuilder.AddSymbol(...) GraphBuilder.cs:line 314
```

The real `T:Andirit.CSharpCodeGraphPlugin` two-part symbol triggers the same
failure before any relationship extraction
([`evidence/comparison/partial.baseline.json`](evidence/comparison/partial.baseline.json)).

## Root cause classification (matches R1/Q1, now verified by execution)

| Layer | Classification | Status after remediation |
| --- | --- | --- |
| 1. Unsafe diagnostic property cast | Confirmed defect | Fixed (type-safe formatting) |
| 2. Declaration-specific `doc-summary` disagreement activating the inconsistency branch | Confirmed defect | Fixed (deterministic aggregation policy) |
| 3. Genuine partial-type identity/extraction defect | Not established | Re-checked after the fix: one logical node id, distinct declaration ids, correct containment/call relationships — no additional identity defect found |

## Correction, layer 1 — type-safe diagnostics

`FormatProperty` (`GraphBuilder.cs`) renders any `CodePropertyValue` variant by
its actual type: `CodeTextProperty` → value, `CodeTextListProperty` →
`a/b/c`, `CodeIntegerProperty`/`CodeNumberProperty` → invariant-culture number,
`CodeBooleanProperty` → `true/false`, unknown future variant → CLR type name.
Missing keys render `none`. The inconsistency message now lists every differing
key with both rendered values, deterministically ordered:

```
Roslyn produced inconsistent facts for symbol '<scoped key>';
differing: [modifiers=[existing:static|new:sealed]].
```

No cast of property payloads occurs anywhere in diagnostic formatting, so the
diagnostic cannot itself throw. A genuine conflicting invariant still raises
`InvalidOperationException` with meaningful, deterministic content
(regression `ExtractAsync_GenuineInvariantConflictFailsWithTypeSafeDiagnostic`).

## Correction, layer 2 — deterministic declaration-specific property policy

Explicit property classification for one logical symbol (same scoped node id,
i.e. same project + canonical definition):

| Class | Properties | Policy |
| --- | --- | --- |
| Declaration-specific documentation | `doc-summary`, `doc-remarks` | **Aggregated deterministically.** Removed from the per-declaration property set before comparison; every physical part contributes its value to an ordinal-sorted distinct set; the logical node receives values joined by `"\n\n"`, bounded to 2048 characters. Source-enumeration-order independent (merge is commutative/associative over the set), never "last declaration wins", never silently discards a part's documentation. |
| Logical-symbol invariants | everything else (`language`, `canonical-key`, `symbol-kind`, `access`, `modifiers`, `attributes`, `obsolete`, `test-method`, `http-endpoint`, `route-template`, `constant-value`, …) | Must agree exactly across all physical parts, and must be present/absent identically. Any disagreement throws `InvalidOperationException` with the type-safe diagnostic. |

Why the invariant set is safe for legitimate partial declarations: Roslyn
models all parts of a partial type as **one** `INamedTypeSymbol` (verified by
probe: `ReferenceEquals` across parts is true), so symbol-derived properties
(`modifiers`, `access`, `attributes`) are identical for every part, and
declaration syntax differences that Roslyn accepts (e.g. `sealed` written on
one part only) resolve to the same symbol flags. Only the syntax-derived
documentation differs between parts — exactly the class placed under the
deterministic merge policy.

`NodesEquivalent` is **not** globally weakened: it remains a strict full-node
comparison; documentation keys are removed *before* the comparison precisely
so genuine invariant conflicts (e.g. two distinct symbols sharing a canonical
key with different modifiers) are still rejected. Physical declarations remain
independently preserved: each part keeps its own `CodeGraphDeclaration`
(id/location hash includes path + span) and its own `declares` edge; only the
logical node's property payload is aggregated.

Aggregated properties are materialized in `WriteAsync` (after relationship
extraction, which never reads documentation), so per-declaration processing
sees pure invariant state and the final bounded text is computed once from the
complete declaration set.

## Deferred, explicitly out of scope

Partial-method definition/implementation unification (role modelling,
`PartialDefinitionPart`/`PartialImplementationPart`) remains **not qualified
and not claimed**. The documentation aggregation policy is generic over
physical declarations of one logical symbol; it does not add, remove, or
re-model partial-method roles, and no test claims partial-method coverage.
If a documented partial method reaches extraction, its parts' documentation
merges under the same declared rule; its method-role semantics are unchanged
from baseline.

## Bounded-output rule

Merged documentation is capped at 2048 characters with deterministic ordinal
truncation. With 512-character per-part summaries (the parser cap), ~4 parts
fill the budget; the regression
`ExtractAsync_PartialDocumentationMergeIsBounded` pins the cap and prefix
behaviour.
