# Public API migration

The complete exported type/member inventory before changes is [public-api-before.json](raw/public-api-before.json). After-rename comparison must equal the following explicit mapping, including members; no allowance for other removals or additions.

| Old | New |
|---|---|
| Penghou.Hetu[.Abstractions/.CSharp/.TreeSitter/.LatticeDb/.Testing] assemblies/packages | Penghou.Andirit with the same suffix |
| Penghou.Hetu and nested namespaces | Penghou.Andirit and corresponding nested namespaces |
| HetuBuilder | AndiritBuilder |
| HetuHost | AndiritHost |
| HetuHostBuilder | AndiritHostBuilder |
| HetuHostHealth | AndiritHostHealth |
| CSharp/LatticeDb/TreeSitter.HetuHostBuilderExtensions | corresponding AndiritHostBuilderExtensions |

`HetuHealth.cs` was a filename, not an exported type named HetuHealth; general CodeGraph health types retain their short names. General-purpose public types retain their short names. This is a deliberate pre-1.0 source and binary break: update packages/usings/product types and rebuild all consumers. Type forwarding cannot preserve old full names when namespaces change; preserving those names would require wrappers and a second API. No forwarding assemblies or broad wrappers added. Old packages remain separate historical packages and stores remain data-compatible independently of CLR compatibility.
