# Tree-sitter Grammar Pins

Grammars are pinned native dependencies of `Penghou.Andirit.TreeSitter`.
Each entry below identifies exactly which parser produced a graph fact, so a
grammar upgrade participates in Andirit's normal plugin-version freshness
behavior. Never upgrade a pin without rebuilding and committing all
per-RID natives in `runtimes/` first: a version without its binaries cannot
index on that runtime, and the plugin reports the gap as
`ProviderUnavailable` rather than guessing.

## Pinned grammars

| Language | Grammar | Source | Version | Commit | ABI | Natives | Capabilities |
| --- | --- | --- | --- | --- | --- | --- | --- |
| elixir | tree-sitter-elixir | https://github.com/elixir-lang/tree-sitter-elixir | v0.3.5 | `e2d9e6e0e76b0c436fa48a0b8c32a031d0cbdf49` | 14 | checked in | Syntax, Declarations, Imports, Diagnostics |
| typescript (`.ts`/`.mts`/`.cts`) | tree-sitter-typescript | https://github.com/tree-sitter/tree-sitter-typescript | v0.23.2 | `75b3874edb2dc714fb1fd77a32013d0f8699989f` | 14 | binding | Syntax, Declarations, Imports, Diagnostics |
| typescript (`.tsx`) | tree-sitter-tsx | https://github.com/tree-sitter/tree-sitter-typescript | v0.23.2 | `75b3874edb2dc714fb1fd77a32013d0f8699989f` | 14 | binding | Syntax, Declarations, Imports, Diagnostics |
| javascript (`.js`/`.mjs`/`.cjs`/`.jsx`) | tree-sitter-javascript | https://github.com/tree-sitter/tree-sitter-javascript | v0.25.0 | `58404d8cf191d69f2674a8fd507bd5776f46cb11` | 15 | binding | Syntax, Declarations, Imports, Diagnostics |

All grammars are permissively licensed (Elixir Apache-2.0, TypeScript and
JavaScript MIT), matching this repository's license; the managed
`TreeSitter.DotNet` runtime dependency is MIT.

The TypeScript and JavaScript grammars come from the same grammar repositories
and commits that the pinned `TreeSitter.DotNet` package vendors, so their
natives are resolved from that package's `runtimes/<rid>/native` folder rather
than checked in here. A `TreeSitter.DotNet` upgrade can therefore change those
grammar revisions behind an unchanged `Penghou.Andirit.TreeSitter` version: the
load-time ABI check still fails closed on a mismatch, but the recorded version
and commit above must be re-verified against the binding's
`tree-sitter-native/` submodule SHAs (and updated together) whenever the
binding is upgraded.

The managed parsing runtime comes from the `TreeSitter.DotNet` NuGet
dependency (exact version in the project file). The expected grammar ABI is
validated at language load: anything other than the pinned ABI fails with
`VersionMismatch` before any source is parsed.

## Why these natives are checked in

No trustworthy prebuilt .NET grammar distribution exists that satisfies
Andirit's constraints: the community language pack downloads grammars on first
use (network at runtime, unpinned revisions), and the binding package itself
ships no Elixir grammar. Building the two generated C files per runtime is
cheap and reproducible; checking in the result keeps indexing offline and
deterministic, exactly like any other pinned native dependency. The recipe
below is the upgrade path, not a build-time step.

## Rebuild procedure

This procedure applies to the checked-in Elixir native. The TypeScript and
JavaScript natives are supplied by the `TreeSitter.DotNet` binding and have no
separate rebuild step in this repository.

Check out the pinned commit (tags move less than branches; prefer a tag):

```powershell
git clone --depth 1 --branch v0.3.5 https://github.com/elixir-lang/tree-sitter-elixir.git grammar
```

The grammar ships generated `src/parser.c` plus a handwritten
`src/scanner.c` (external scanner for quoted content). Both compile with a
plain C99 toolchain; no tree-sitter CLI or Node.js is required because the
parser is already generated. Headers live in `src/tree_sitter`.

Windows x64 (MSVC, from a Developer Command Prompt):

```cmd
cl /nologo /LD /O2 /Isrc\tree_sitter src\parser.c src\scanner.c /Fetree-sitter-elixir.dll
```

Copy the result to `runtimes/win-x64/native/tree-sitter-elixir.dll`.

Linux x64 (gcc or clang):

```sh
cc -shared -fPIC -O2 -Isrc/tree_sitter src/parser.c src/scanner.c -o libtree-sitter-elixir.so
```

Copy the result to `runtimes/linux-x64/native/libtree-sitter-elixir.so`.

macOS ARM64 (zig cross-compiles without an Apple toolchain; any recent
zig works):

```sh
zig cc -target aarch64-macos -dynamiclib -O2 -Isrc/tree_sitter src/parser.c src/scanner.c -o libtree-sitter-elixir.dylib
```

Copy the result to `runtimes/osx-arm64/native/libtree-sitter-elixir.dylib`.

## Verification checklist for a new pin

1. `LANGUAGE_VERSION` in the new `parser.c` still equals the ABI recorded
   above; if it changed, update the registry descriptor (which rejects
   anything else) and this file together.
2. Each binary exports its `tree_sitter_<language>` factory
   (`dumpbin /exports`, `nm -D`, or equivalent).
3. The plugin test suite passes on Windows, Linux, and macOS CI runners,
   proving each native loads and parses through the real resolution path.
4. The `TreeSitterGrammarDescriptor` values (version, commit, ABI,
   capabilities) match the new pin.
5. `CHANGELOG.md` records the grammar change as an index-invalidation event.
