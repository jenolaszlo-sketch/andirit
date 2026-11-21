# Package and version inventory — Stage-2 baseline publication qualification

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-PUBLICATION-01.md`.
Inventories the packages produced from the accepted Stage-2 baseline (`eafd632` lineage,
packed from the pinned exact-byte export) and the smoke-test verification result.

## 1. Package inventory

Version: **`0.2.0-preview.6`** (from `Directory.Build.props`), packed locally at the distinct
smoke version **`0.2.0-preview.6-smoke.publication01`** (`pack.log`, `feed-listing.txt`).
Symbol packages (`.snupkg`, `IncludeSymbols=true`, `SymbolPackageFormat=snupkg`) accompany each.

### 1a. Version identity (publication-plan §2)

`0.2.0-preview.6` is **already a released version**: the lightweight remote tag
`v0.2.0-preview.6` points at `f20b77a9b9637abf825045b3f2a307d1c0253a5d` (2026-09-26, "chore: release
0.2.0-preview.6 with test-to-production mapping"), 293 commits behind the accepted head; the
CHANGELOG's `## 0.2.0-preview.6` section still holds the September content, with all Stage-2 work
under `## Unreleased`. The accepted baseline packs at that same version string. The publication
plan therefore requires an owner version decision and recommends **`0.2.0-preview.7`** for the
accepted Stage-2 baseline (release commit + matching tag + dedicated CHANGELOG section).
Feed-collision check: none of the six package IDs exists on nuget.org (2026-11-22) — no public-feed
duplicate-version collision; verify absence on any chosen non-public feed before pushing.

| Package | nupkg bytes | Native assets | Dependencies | License / README |
| --- | --- | --- | --- | --- |
| `Andirit` | 191,919 | — | `Andirit.Abstractions` | Apache-2.0; README packed |
| `Andirit.Abstractions` | 47,030 | — | — | Apache-2.0; README packed |
| `Andirit.CSharp` | 88,157 | — | `Andirit`, `Andirit.Abstractions`, Roslyn | Apache-2.0; README packed |
| `Andirit.LatticeDb` | 51,155 | — | `Andirit`, `Andirit.Abstractions`, **`LatticeDbSharp` 0.2.0** | Apache-2.0; README packed |
| `Andirit.Testing` | 29,877 | — | `Andirit`, `Andirit.Abstractions` | Apache-2.0; README packed |
| `Andirit.TreeSitter` | 613,743 | `runtimes/win-x64/native/tree-sitter-elixir.dll`, `runtimes/linux-x64/native/libtree-sitter-elixir.so`, `runtimes/osx-arm64/native/libtree-sitter-elixir.dylib` | `Andirit`, `Andirit.Abstractions`, `TreeSitter.DotNet` 1.3.0 | Apache-2.0; README packed |

No reverse dependencies on retired identities: the smoker's "no old Penghou.Andirit or Hetu
assembly dependencies" check passes.

## 2. Smoke-test evidence (pack-consume-load)

| RID / platform | Evidence | Result |
| --- | --- | --- |
| **win-x64** (this commission, Windows 10.0.26200, SDK 10.0.401) | `smoke.log` — all checks `ok`, `PASS: all six packed Andirit packages, managed providers, durable store, testing conformance and Tree-sitter natives resolved.`, exit 0 | **PASS** |
| **linux-x64** (genuine Ubuntu 26.04.1, SDK 10.0.404) | CI `pack` job of run **44746950426** (preserved `job-pack.log`): pack of all six packages at `0.2.0-preview.6-smoke.44746950426` + smoker PASS with provider versions `+v0.3.5.abi14.p2` (elixir), `+v0.23.2.abi14.rem` (typescript), `+v0.25.0.abi15.p10` (javascript) | **PASS** |
| **osx-arm64** | The repository CI runs the `pack` job only on `ubuntu-latest`; the packed osx-arm64 asset is verified present (`native-rid-verification.txt`) but no pack-consume-load execution exists | **NOT COVERED** — recorded; recommendation: extend the publication gate set to a macOS smoker run in the publication commission (not a Stage-2 blocker) |

## 3. Native dependency resolution

- `Andirit.TreeSitter` ships the checked-in Elixir grammar natives per RID inside the package
  (`grammar-pins.md` records the rebuild procedure); TypeScript/JavaScript natives resolve from
  the `TreeSitter.DotNet` dependency.
- `Andirit.LatticeDb` depends on `LatticeDbSharp 0.2.0`, which ships
  `runtimes/linux-x64/native/liblattice.so` (resolution evidenced by the 75/0/0 genuine-Ubuntu
  suite and the win-x64 smoker's "packed durable reopen and native mirror" check).

## 4. Committed-evidence suitability

Credential/sensitive scan of all 4,708 tracked blobs at `a5e861b8` (`sensitive-scan.txt`):
no GitHub tokens, no AWS keys, no private keys, no password assignments, no NuGet API keys
(524 raw-pattern hits are base64 store payloads inside evidence JSON; 8 "secret" hits are the
deliberate `"invalid-token"` continuation-token test values). The only config-like tracked
file, `smoke/TreeSitterPackage/nuget.config`, contains package sources only (no credentials).
**Committed evidence is suitable for publication.**