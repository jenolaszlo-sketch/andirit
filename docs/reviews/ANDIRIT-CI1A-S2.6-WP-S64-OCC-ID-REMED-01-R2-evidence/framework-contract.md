# Target-framework reference-resolution contract

## 1. Supported matrix

| Target framework | Pack family | Deterministic selection |
| --- | --- | --- |
| `net8.0` | `Microsoft.NETCore.App.Ref` | highest installed `8.0.x` with `ref/net8.0` |
| `net9.0` | `Microsoft.NETCore.App.Ref` | highest installed `9.0.x` with `ref/net9.0` |
| `net10.0` | `Microsoft.NETCore.App.Ref` | highest installed `10.0.x` with `ref/net10.0` |
| `netstandard2.1` | `NETStandard.Library.Ref` | highest installed `2.1.x` with `ref/netstandard2.1` |

Selection is by declared major/minor, never by the globally newest pack. A pack
version is selected only when its `ref/<tfm>` directory exists. Ties are resolved
by ordinal directory name. Equivalent pack contents at different physical paths
produce equal `refctx1` identities.

## 2. Explicit refusal

The following are refused with actionable diagnostics and no authoritative
complete occurrence publication:

- `netstandard2.0` — no installed 2.0 reference pack; not silently mapped to 2.1;
- `net6.0`, `net7.0` — no installed reference pack;
- platform-specific TFMs such as `net10.0-windows` — outside the supported scope;
- missing/empty `TargetFramework` without explicit caller configuration;
- ambiguous/invalid pack metadata or missing listed reference assemblies;
- incompatible project-reference target frameworks.

Loose/synthetic sources may use explicit caller configuration
`csharp.targetFramework`; there is no ambient runtime fallback.

## 3. Reference identity and content

Identities remain `refctx1|assembly|version|culture|public-key-token|sha256:<image>`.
No installation path participates. The actual Roslyn compilation uses exactly the
selected pack files. `data/FrameworkList.xml` managed entries are checked for
existence, and each selected PE is validated and content-hashed.

## 4. Compilation and identity consequences

The declared target framework is represented by the selected reference closure
inside the canonical `compctx3` descriptor. Distinct supported TFMs produce
distinct descriptors and targets even when source text is identical. The
`asmctx4`/`usage5` recipes are unchanged; only their reference inputs are
corrected.

A framework-reference error (`CS0234`, `CS0246`, `CS0400`, `CS0012`, `CS0518`,
`CS1069` naming a `System`/`Microsoft` surface) refuses the project's complete
semantic publication. Other source errors remain coverage/unresolved findings,
preserving the accepted semantic extraction contract.

## 5. Project references

Same-TFM project references are supported. `netstandard2.1` libraries may be
consumed by `net8.0`, `net9.0` and `net10.0` projects. Incompatible caller/
dependency TFMs refuse `UnsupportedProjectReferenceTargetFramework`.

## 6. Persisted compatibility

Corrected stores use occurrence schema **3**, namespace
`andirit-occurrence-facts/3`, metadata `IdentityGeneration=3`, authority version
**3** and the existing `asmctx4`/`usage5` domains. R1 generation-2 stores are
experimental and incompatible; they are refused before any writable operation.
