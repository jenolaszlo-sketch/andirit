# Release verification — PUBLICATION-EXEC-01 (0.2.0-preview.7)

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-PUBLICATION-EXEC-01.md`.
Final package verification at the release version, on a byte-exact export of the release commit.
No production source, dependency, formatter, coverage or CI-policy change is included in the release.

## 1. Release commit identity

| Item | Value |
| --- | --- |
| Release commit | `68a141f67023a9a66a5d2b77c8d93cf3cecb9993` (amended from `530aacec` to add the test re-baseline; not pushed before the amendment) |
| Tree | `97993a729b5eb8c4323621d152eabbbc2e5eaa20` |
| Parent | `2aaab9ab3babd493b4a84e954ee16c623f558e73` (PUBLICATION-01 seal) |
| Contents | `Directory.Build.props` (version), `CHANGELOG.md` (new `## 0.2.0-preview.7` section; `Unreleased` history moved verbatim), `tests/Andirit.TreeSitter.Tests/TreeSitterEcmaExportShapeTests.cs` (two golden values) |
| Protected scope | `git diff eafd632 68a141f6 -- src .github` **empty** — no production or CI change since the qualified revision |
| Byte-exact export | `D:\m54b-release\production`, 4,722/4,722 blobs, 0 missing / 0 extra / 0 byte mismatches (`export-verification.txt`) |

## 2. Local gate results at 0.2.0-preview.7 (Windows x64, SDK 10.0.401)

| Gate | Command | Exit | Evidence |
| --- | --- | --- | --- |
| Restore | `dotnet restore Andirit.slnx` | 0 | `gate-exits.txt` |
| Exact-byte formatting | `dotnet format Andirit.slnx --verify-no-changes --no-restore` | 0 | `gate-exits.txt` |
| Whole-solution tests | `dotnet test Andirit.slnx -c Release --no-restore` | 0 | `gate-exits.txt`, `suite-summary.txt` |
| Pack (six packages + symbols) | `dotnet pack Andirit.slnx -c Release --no-restore` | 0 | `pack.log`, `feed-listing.txt` |
| Pack-consume-load smoke (win-x64) | smoker with `SMOKE_FEED` at `0.2.0-preview.7` | 0 | `smoke-final.log` |

Per-project test counts (`suite-summary.txt`): Andirit.Tests 345/345/0; Andirit.CSharp.Tests 372/372/0;
Andirit.TreeSitter.Tests 157/157/0; Andirit.LatticeDb.Tests 75/75/0 — **total 949 passed / 0 failed /
0 skipped**, equal to the accepted-suite baseline count. TRX receipts: `trx-*.trx`.

The initial gate run at `530aacec` failed exactly one test
(`TreeSitterEcmaExportShapeTests.PublishedGraphAddsExportsWithoutChangingPreChangeP8Facts`): its
golden pre-export fingerprints fold the provider/assembly version, which the authorized version bump
changes. Owner-authorized release-preparation amendment: two literal values updated in
`tests/Andirit.TreeSitter.Tests/TreeSitterEcmaExportShapeTests.cs` (typescript
`84c13587…` → `7d067f6c70c20b2cc7db310d1d61b49c87f90eb432eebdee6135b2433b52a664`; javascript
`3b2d3761…` → `5e6c797366ef1443dc4c687e481d59537ac8c9378486aebbcf2fa22d7604ac37`). The new values
were computed by a scratch-only probe over the verbatim test inputs (never committed) and confirmed
against the failing assertion's actual value; the released values are deterministic on re-run. This
is a version-folded expectation update, not a production behavior change, a semantic claim change,
or an accepted-evidence modification — historical receipts (including the EC-6C and M54-B packages)
are preserved at their recorded versions.

## 3. Final package verification at 0.2.0-preview.7

- **Versions**: every nuspec and dependency at `0.2.0-preview.7` (`package-verification.txt`); the
  six nupkg + six snupkg hashed in `feed-listing.txt` (12 files, 1,158,401 bytes total).
- **Native assets**: `Andirit.TreeSitter` ships `runtimes/win-x64/native/tree-sitter-elixir.dll`
  (1,504,768 B), `runtimes/linux-x64/native/libtree-sitter-elixir.so` (1,426,696 B),
  `runtimes/osx-arm64/native/libtree-sitter-elixir.dylib` (1,467,336 B); README packed.
- **Dependencies**: `Andirit.LatticeDb` → `Andirit`/`Andirit.Abstractions` at `0.2.0-preview.7` +
  `LatticeDbSharp` 0.2.0; no retired identities (smoker check `ok: no old Penghou.Andirit or Hetu
  assembly dependencies`).
- **Smoke**: all checks `ok` — all six packed packages, managed providers, durable store, testing
  conformance and Tree-sitter natives resolved; provider versions
  `0.2.0-preview.7+v0.3.5.abi14.p2` (elixir), `+v0.23.2.abi14.rem1.p10` (typescript),
  `+v0.25.0.abi15.p10` (javascript) (`smoke-final.log`). Genuine-Ubuntu pack-consume-load evidence
  is covered by the push-triggered CI `pack` job (see `publication-receipt.md`).
- **osx-arm64 smoke limitation preserved**: pack-consume-load runtime evidence covers win-x64
  (this commission) and linux-x64 (CI); osx-arm64 remains package-content evidence only — not
  represented as runtime qualification.