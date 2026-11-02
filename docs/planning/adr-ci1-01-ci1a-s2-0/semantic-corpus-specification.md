# Semantic corpus specification

**Authority.** [ADR-CI1-01 Candidate C](../../decisions/adr-ci1-01-owner-decision.md); R2 provenance/identity contracts; [S2.1 provenance contract](../adr-ci1-01-ci1a-s2-1/provenance-contract.md); [S2.2 target identity decision](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md); [obligation register §5](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md) (F12).

## 1. Purpose and boundary

The corpus is a small, purpose-built, deterministic C# compilation set whose
semantic occurrences are established by direct Roslyn inspection and frozen as an
oracle. It is **evidence and test oracle only**: it neither implements nor invokes
S2.2 production extraction, and it introduces no production result type.

## 2. Projects and compilation inputs

The fixture is a genuine project graph rooted at `corpus/repo/`:

| Unit | Assembly | Sources | Project references | Defines |
|---|---|---|---|---|
| `Leaf/Leaf.csproj` | `Corpus.Leaf` | `Leaf/Helper.cs` | — | dependency-free target leaf |
| `Core/Core.csproj` | `Corpus.Core` | `Core/Types.cs`, `Core/Methods.cs`, `Core/Extensions.cs`, `Core/LeafUser.cs` | `Leaf` | most source-defined targets; base/derived, interface, extension, overloads, generic, property/field/event, partial type, conditional API |
| `App/App.csproj` | `Corpus.App` | `App/Sites.cs`, `App/Bulk.cs` | `Core`, `Leaf` | named and volume call sites |
| `Solo/Solo.csproj` | `Corpus.Solo` | `Solo/Declarations.cs` | — | complete-empty scope (no invocations) |
| `Negatives/Negatives.csproj` | `Corpus.Negatives` | `Negatives/NegativeSites.cs`, `Negatives/External.cs` | `Core`, external metadata image | ambiguity, unresolved, dynamic, external, inactive |
| `External/ExternalLib.cs` | `Corpus.External` | `External/ExternalLib.cs` | — | emitted in-memory by the oracle; a source-out-of-scope metadata target |

Compilation inputs are declared and frozen as follows.

- **Parse options:** `LanguageVersion.CSharp14`, `DocumentationMode.Diagnose`, `SourceCodeKind.Regular`, no parse features, preprocessor symbols from the variant (sorted ordinal).
- **Compilation options:** `OutputKind.DynamicallyLinkedLibrary`, `OptimizationLevel.Release`, `NullableContextOptions.Disable`, `Deterministic=true`, `ConcurrentBuild=false` — matching the accepted S2.1 production baseline options so provenance is comparable.
- **Metadata references:** the runtime's `System.Runtime.dll`, the core library (`System.Private.CoreLib.dll`) and `Microsoft.CSharp.dll`, identified by **content-bound, host-independent** reference identities `file|runtime/<file>|sha256:<hash>`. The absolute host path is deliberately excluded; identity is bound to the runtime file contents. The external `Corpus.External` image is identified as `file|external/ExternalLib.dll|sha256:<hash>`.
- **Project references:** represented as `CompilationReference` to the actual dependency compilation.

Every source file is decoded from its exact bytes (`SourceText.From(stream)`), so
the decoded tree is the exact input; the identity source hash is the SHA-256 of
the file bytes. Files are UTF-8 without BOM and newline-normalised in the working
tree via `docs/evidence/adr-ci1-01-ci1a-s2-0/.gitattributes` (`* -text -diff`).

## 3. Occurrence markers

Named and volume occurrences are located by a stable marker comment
immediately preceding the designated syntax node:

```csharp
_ = /*@S01*/ methods.Direct(1);
```

`/*@Sxx*/` are named scenarios; `/*@INV-nn*/` are volume invocations;
`/*@MR-nn*/` are method-group references. Markers are data: production code never
reads them, and the oracle resolves the compiler's actual binding for the node
that follows.

The recorded site span is the **identifier-token span**: 1-based start line/column
and 1-based exclusive end line/column of the invoked member's identifier token.

## 4. Variants

Each variant declares exactly which inputs differ; the oracle never mutates
fixtures in place.

| Variant | Difference | Purpose |
|---|---|---|
| `baseline` | none | all positive and negative expectations |
| `source-changed` | `App/Sites.cs` from `variants/source-changed/` (one argument `1`→`101`) | calling-source change; new `usage4`, stable target |
| `symbols-core-changed` | `Core` preprocessor symbol `CORE_FEATURE` | conditional defining API; changed target identity |
| `symbols-leaf-changed` | `Leaf` preprocessor symbol `LEAF_TRACE` | transitive dependency change; changed `compctx2`, stable target |
| `reference-missing` | `App` omits the `Core` project reference | unresolved binding refusal |

`symbols-leaf-changed` changes a **preprocessor symbol**, not source bytes. This
is deliberate and reproduces the accepted E2 experiment: the R2 project-reference
item hashes dependency source *text*, so the defining project's R2 material — and
therefore its target identity — is unchanged, while its verified `compctx2`
changes and propagates transitively.

## 5. Environment

Recorded by the oracle and in `../../evidence/adr-ci1-01-ci1a-s2-0/environment-dotnet-info.txt`:

- .NET SDK 10.0.401, runtime 10.0.12, Windows x64.
- Roslyn `Microsoft.CodeAnalysis.CSharp` 5.9.0.
- Expected `asmctx3`/`usage4`/`compctx1`/`compctx2` digests are bound to this runtime's reference file contents. A different runtime patch level may legitimately change them; the relational and semantic expectations are portable.

## 6. F12 minima

| Criterion | Required | Corpus |
|---|---|---|
| C# files | ≥ 10 | 11 |
| Invocations | ≥ 50 | 53 |
| Method references | ≥ 20 | 21 |
| Overloaded methods | ≥ 5 | 5 (`Methods.Add`) |
| Namespaces | ≥ 3 | 5 built + external |
| Projects | ≥ 2 | 5 (+ external image) |
| Cross-project invocations | ≥ 5 | 52 |

Also covered: changed source, changed compilation context (symbols/options), unit
replacement, valid zero-usage, and unsupported/incomplete/external/ambiguous/
missing-binding cases (see [case matrix](case-matrix.md)).
