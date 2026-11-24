# Andirit

[![CI](https://github.com/jenolaszlo-sketch/andirit/actions/workflows/ci.yml/badge.svg)](https://github.com/jenolaszlo-sketch/andirit/actions/workflows/ci.yml)

Andirit is an embedded code knowledge graph for .NET 10. Index a repository and query its symbols, declarations, and relationships to trace dependencies, assess the impact of a change, or select source context for another tool. Language plugins map parser output into a shared graph vocabulary, so consumers can use the same query API across languages and stores.

## Install and try it

The current NuGet release is **0.2.0-preview.7**, a preview of the Stage 2 baseline. In a .NET 10 console project, install the runtime and the C# plugin:

```sh
dotnet add package Andirit --version 0.2.0-preview.7
dotnet add package Andirit.CSharp --version 0.2.0-preview.7
```

This example indexes the local directory passed as the first argument and takes an exact qualified symbol name as the second argument, such as `Example.OrderService`.

```csharp
using Andirit;

await using var host = new AndiritHostBuilder()
    .AddPlugin(new CSharpCodeGraphPlugin())
    .Build();

var repository = new CodeRepositoryDescriptor(
    new CodeRepositoryId("repo:my-app"), Path.GetFullPath(args[0]));
await host.IndexRepositoryAsync(repository, new CodeIndexRunId($"run:{Guid.NewGuid():N}"));

var publication = await host.OpenLatestPublicationAsync(repository.Id)
    ?? throw new InvalidOperationException("No successful index publication.");
var lookup = await publication.FindSymbolAsync(args[1]);
Console.WriteLine($"Matching symbols: {lookup.Result.Candidates.Count}");
```

The default store is in memory. For persistence across processes, install `Andirit.LatticeDb` at the same version, import `Andirit.LatticeDb`, and add `.UseLatticeStore("my-app.ltdb")` before `.Build()`. Reuse the logical repository ID across runs and supply a fresh run ID each time.

## What the graph can tell you

| Language | Extraction in preview.7 |
| --- | --- |
| C# | Project-aware Roslyn extraction of declarations and selected resolved calls, references, inheritance, and project dependencies. SDK-style project discovery uses a lightweight model with host runtime references and leaves MSBuild conditions and custom targets unevaluated. |
| TypeScript and JavaScript | Tree-sitter extraction of supported declarations, imports, invocation names, and a positive-only inventory of supported written export entries. Bounded relative-module resolution can identify an indexed target file. Import-to-export symbol binding and compiler-equivalent type resolution are outside this coverage. |
| Elixir | Tree-sitter extraction of modules, functions, imports, containment, and invocation names. Invocation targets are syntax observations. |

Semantic facts rely on bindings the provider actually resolves. Structural facts describe supported syntax, such as the name written at an invocation site. Some relationships are derived from recognized patterns. Evidence records the distinction. See the [language feature matrix](docs/language-feature-matrix.md) and [evidence guide](docs/evidence-quality-and-provider-strategy.md).

- **Incremental indexing.** Source hashes and plugin versions identify what changed. An unchanged plugin can skip extraction, while each plugin chooses its replacement unit, such as a file or C# project. A successful run publishes the graph and source state atomically.
- **Deterministic, bounded queries.** Use exact symbol and declaration lookup, references, callers, dependencies, or impact traversals where the plugin supplies the facts. Exact lookup returns every candidate. Traversals have node, edge, and depth limits and report truncation.
- **Publication provenance.** Publication-bound queries identify the successful index and applied query and retain contributing plugin versions and index units. A bound view detects a newer publication between calls. For exact name lookup, see the [consumer guide](docs/consumer-guidance-qualified-name-lookup.md).
- **Embedded persistence.** The LatticeDB provider uses an embedded database file and restores committed state on reopen. Its native file lock limits the database to one owner at a time.

## Packages and platforms

All six packages target `net10.0` and are published at `0.2.0-preview.7`.

| Package | Purpose |
| --- | --- |
| [Andirit](https://www.nuget.org/packages/Andirit/0.2.0-preview.7) | Host, incremental indexer, query services, and in-memory store |
| [Andirit.Abstractions](https://www.nuget.org/packages/Andirit.Abstractions/0.2.0-preview.7) | Graph contracts for consumers, plugins, and providers |
| [Andirit.CSharp](https://www.nuget.org/packages/Andirit.CSharp/0.2.0-preview.7) | Roslyn C# plugin |
| [Andirit.TreeSitter](https://www.nuget.org/packages/Andirit.TreeSitter/0.2.0-preview.7) | TypeScript, JavaScript, and Elixir plugins |
| [Andirit.LatticeDb](https://www.nuget.org/packages/Andirit.LatticeDb/0.2.0-preview.7) | Durable embedded graph store |
| [Andirit.Testing](https://www.nuget.org/packages/Andirit.Testing/0.2.0-preview.7) | Reusable provider conformance tests |

Native packages carry Windows x64, Linux x64, and macOS ARM64 assets. The published packages were exercised from NuGet on Windows x64. Linux x64 has native source qualification. The release record lists macOS ARM64 at the asset-evidence level. See the [public-feed qualification](docs/reviews/ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01.md) and [grammar pins](src/Andirit.TreeSitter/native/grammar-pins.md).

## Unreleased and planned

The source on `main` carries the S6.1 cancellation contract and S6.2 native durability tests and evidence, accepted with qualifications. A cancellation request can be honored before native execution. Once the native write begins, callers must await its outcome. Full crash and power-loss guarantees, the remaining native publication-correctness cases, and broader platform certification remain open. See [S6.1 acceptance](docs/reviews/ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01.md) and [S6.2 acceptance](docs/reviews/ANDIRIT-CI1A-S2.6-WP-S62-ACCEPT-01.md).

The [project roadmap](ROADMAP.md) tracks operational hardening and semantic expansion. Python, Java, Go, Rust, and Kotlin are planned in the [language roadmap](docs/language-support-roadmap.md).

## More documentation

[Runtime and integration details](docs/runtime-guide.md) · [Development](docs/DEVELOPMENT.md) · [Benchmarks](BENCHMARKS.md) · [Hetu migration](docs/andirit-migration.md) · [Apache-2.0 license](LICENSE)
