# Baseline negative controls and corrected results

## Reproduced baseline defects

Against candidate `77ec887` / producer seal `880a888`:

| Control | Baseline result |
| --- | --- |
| `net10.0`, `net8.0`, `netstandard2.0`, same source | same 172-reference runtime descriptor |
| `netstandard2.0` using `System.Half`/`System.Range` | accepted as complete |
| consumer runtime | selected the host implementation directory |
| missing/unsupported target framework | no explicit refusal |
| incompatible store open | schema/replay ran before refusal; file/WAL changed |

These are preserved by the independent qualification evidence and reproduced by
the R2 framework probe before the correction.

## Corrected results

| Case | Corrected result |
| --- | --- |
| `net10.0` | `Microsoft.NETCore.App.Ref` pack, 167 refs, complete |
| `net8.0` | `Microsoft.NETCore.App.Ref` 8.0 pack, complete, distinct descriptor |
| `net9.0` | `Microsoft.NETCore.App.Ref` 9.0 pack, complete, distinct descriptor |
| `netstandard2.1` valid source | `NETStandard.Library.Ref` 2.1.0, complete |
| `netstandard2.1` with `System.Half`/`Range` | `FrameworkReferenceErrors`, not complete |
| `netstandard2.0` | `UnsupportedTargetFramework`, not complete |
| `net6.0` / `net10.0-windows` | explicit refusal |
| missing TargetFramework | `MissingTargetFramework`; loose source requires explicit `csharp.targetFramework` |
| incompatible project-reference TFMs | `UnsupportedProjectReferenceTargetFramework` |
| R1 generation-2 store | refused before schema/replay; main and WAL hashes unchanged |
| generation-1 store | refused; main and WAL hashes unchanged |

The `net8.0` and `net9.0` controls are executed on a .NET 10.0.12 consumer host,
demonstrating that the consumer runtime no longer selects the semantic closure.
