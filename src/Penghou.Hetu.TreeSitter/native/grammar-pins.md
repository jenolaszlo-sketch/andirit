# Tree-sitter Grammar Pins

Grammars are pinned native dependencies of `Penghou.Hetu.TreeSitter`.
Each entry below identifies exactly which parser produced a graph fact, so a
grammar upgrade participates in Hetu's normal plugin-version freshness
behavior. Never upgrade a pin without rebuilding and committing all
per-RID natives in `runtimes/` first: a version without its binaries cannot
index on that runtime, and the plugin reports the gap as
`ProviderUnavailable` rather than guessing.

## Pinned grammars

| Language | Grammar | Source | Version | Commit | ABI | Capabilities |
| --- | --- | --- | --- | --- | --- | --- |
| elixir | tree-sitter-elixir | https://github.com/elixir-lang/tree-sitter-elixir | v0.3.5 | `e2d9e6e0e76b0c436fa48a0b8c32a031d0cbdf49` | 14 | Syntax, Declarations, Imports, Diagnostics |

The grammar is Apache-2.0, matching this repository's license; the managed
`TreeSitter.DotNet` runtime dependency is MIT.

The managed parsing runtime comes from the `TreeSitter.DotNet` NuGet
dependency (exact version in the project file). The expected grammar ABI is
validated at language load: anything other than the pinned ABI fails with
`VersionMismatch` before any source is parsed.

## Why these natives are checked in

No trustworthy prebuilt .NET grammar distribution exists that satisfies
Hetu's constraints: the community language pack downloads grammars on first
use (network at runtime, unpinned revisions), and the binding package itself
ships no Elixir grammar. Building the two generated C files per runtime is
cheap and reproducible; checking in the result keeps indexing offline and
deterministic, exactly like any other pinned native dependency. The recipe
below is the upgrade path, not a build-time step.

## Rebuild procedure

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
