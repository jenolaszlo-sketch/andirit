# Implementation record

## Framework resolver

New `src/Andirit.CSharp/FrameworkReferenceResolver.cs`:

- supported matrix and explicit refusal;
- pack root from the running dotnet installation's `packs` directory;
- highest matching major/minor patch selection with `ref/<tfm>` presence;
- `FrameworkList.xml` managed-file existence checks;
- managed PE validation and `refctx1` content-bound identities;
- deterministic ordinal ordering;
- project-reference TFM compatibility.

`PrepareAnalysisAsync` now resolves a closure per project from its declared
`TargetFramework` (or explicit `csharp.targetFramework` for loose sources), uses
that exact closure for Roslyn, and refuses missing/unsupported/incompatible
closures. Framework-reference compiler errors prevent `Verified` creation.

## Compilation and identity

- `compctx3`/`compctx4` recipes are unchanged.
- `asmctx4`/`usage5` recipes are unchanged.
- The corrected reference inputs change descriptor/target/usage values.
- Actual Roslyn references equal the descriptor inventory.
- No runtime-directory implementation assemblies or process TPA are used.

## Persisted generation

- `CodeOccurrenceIdentity.Generation = 3`.
- `CodeOccurrenceIdentity.AuthorityVersion = 3`.
- Occurrence schema 3 / namespace `/3`.
- Authority/fact payloads retain `asmctx4`/`usage5`/semantics 5.

This marker distinguishes corrected stores from R1 generation-2 experimental
stores without adding a new identity domain.

## Compatibility preflight

`LatticeCodeGraphStore` opens a read-only native connection
(`Create=false, ReadOnly=true, EnableWal=false`) before the writable store,
schema initialization or command-log replay. It checks occurrence schema,
namespace and identity generation. Incompatible stores throw
`CodeOccurrenceSchemaException` before any Andirit persistent mutation. The pinned
LatticeDbSharp 0.2.0 read-only open with WAL disabled is physically non-mutating
for the tested store; no dependency upgrade or ad hoc parser was introduced.

## Test additions

`FrameworkReferenceResolutionTests` covers the matrix, distinct descriptors,
API availability, unsupported TFMs, loose/configured sources, project-reference
compatibility and path-independent reference content. Generation-1/2 refusal
tests assert main-file and WAL hashes are unchanged.
