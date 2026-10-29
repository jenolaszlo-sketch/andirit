# Package identities and publisher qualification

Six intended local IDs: Andirit, Andirit.Abstractions, Andirit.CSharp, Andirit.TreeSitter, Andirit.LatticeDb, Andirit.Testing. Public NuGet flat-container and gallery endpoints returned 404 for every ID, establishing no existing published versions/owners for those exact IDs at preflight. [Endpoint observations](data/nuget-availability.json).

Public absence does not prove reserved-prefix control or authenticated publishing permission. The owner explicitly confirmed during this milestone: **Publisher is jeno and owns the Andirit prefix**. This confirms intended publisher/reservation control as owner-supplied evidence, separately from public endpoint uniqueness. Historical Hetu gallery ownership also identifies jeno. No publication or credential-validation transaction was used as a naming test.

Existing NUGET_USER secret name/configuration remains untouched; its value is not read. A future authorized release must qualify its authenticated trusted-publishing relationship against the final repository name. No package-publication capability is asserted as exercised here. NuGet separates package ownership from ID prefix reservation; see [official policy](https://learn.microsoft.com/en-us/nuget/nuget-org/id-prefix-reservation) and [official uniqueness guidance](https://learn.microsoft.com/en-us/nuget/create-packages/package-authoring-best-practices).

Local pack/fresh-cache consumer qualification passed. Retain development version 0.2.0-preview.6. Publish workflow still covers only the original five project entries; TreeSitter is not added to publication scope. Triggers remain v-tag/manual only; neither is invoked.

## Local qualification

**PASS:** all six standalone packages and symbol packages pack at unchanged development version 0.2.0-preview.6. Nuspec IDs, canonical /andirit repository/project URLs, README, symbol names and dependency graph inspected. Tree-sitter retains exact win-x64, linux-x64 and osx-arm64 Elixir grammar assets; other grammars remain legitimate third-party runtime dependencies. No Penghou.Andirit or Penghou.Hetu package dependency exists.

An external consumer restored all six from a local-only Andirit source mapping into a fresh dedicated cache; each cache .nupkg.metadata source is verified. It compiles public APIs, checks the exact six standalone assemblies, rejects every Penghou assembly reference, extracts C# using Roslyn, parses TS/JS/Elixir natives, composes a host, indexes/queries, persists/reopens LatticeDB with native mirror, verifies snapshot integrity, and executes the Testing package's real store-conformance laws. This consumer also runs in hosted pack CI. It is a consumer verification enhancement, not a new product feature or public API.

Package hashes/dependencies and exact API counts are in data/qualification-summary.json. Pre-commit local artifacts bind the pre-rename Git hash by construction; final handoff packages and hosted packs bind their actual committed source. SourceLink is rechecked after origin becomes canonical /andirit. All outputs/raw logs are external. No packages were uploaded, published, unlisted or removed.

After origin changed, final packages/symbols are rebuilt from the exact final committed source. SDK SourceLink and nuspec commit fields are checked against that final hash and canonical /andirit URL in the external final identity report, together with fresh local-feed cache provenance and package SHA256s. No publication authentication/credentials are changed or exercised.
