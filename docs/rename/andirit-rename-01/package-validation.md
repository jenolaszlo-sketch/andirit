# Package validation

**PASS, local packages only.** Six existing packable projects produce `Penghou.Andirit`, `.Abstractions`, `.CSharp`, `.TreeSitter`, `.LatticeDb`, `.Testing` plus their symbol packages. Existing development version `0.2.0-preview.6` is retained; no framework/dependency upgrade. Testing now includes the same canonical PackageProjectUrl as the other packages.

All nuspec package IDs, dependencies, product titles, descriptions, project/repository URLs, README and symbols were inspected. Repository metadata points to penghou-andirit. No Hetu package dependency exists. Package SHA256s and dependency metadata are in [qualification summary](raw/qualification-summary.json). SourceLink remains the SDK's existing configuration; renamed PDBs/symbol packages follow the new assembly paths. Local packages produced before a rename commit necessarily record the baseline Git commit; hosted pack records the committed rename source.

An isolated consumer outside the checkout restored all six packages from the local feed into a fresh dedicated NuGet cache, with package-source mapping directing Andirit IDs to that feed. The verifier checks each cache entry's actual source. It loads all six assemblies, verifies no old assembly dependency, extracts C#, parses Elixir/TypeScript/JavaScript through native assets, composes an AndiritHost, persists and reopens LatticeDB with native mirror enabled, queries the class and validates snapshot integrity. The class and export entry intentionally share a qualified name; the new smoke check selects the class kind explicitly. [Consumer transcript](raw/consumer.txt).

The original TreeSitterPackage smoke project is expanded to cover all six packages and remains outside the solution; CI packs a unique smoke version and runs that consumer. Tree-sitter package includes unchanged win-x64, linux-x64 and osx-arm64 Elixir grammar binaries; other grammar runtimes remain dependency assets. No native filename was renamed.

No package was published, unlisted or deleted. The publish workflow retains its original five project entries and v-tag/manual triggers; TreeSitter was not added to its publication set.

Historical NuGet inventory confirms five old package IDs retain published previews; Penghou.Hetu.TreeSitter returned 404. See raw/historical-nuget.json. No old package operation was performed.
