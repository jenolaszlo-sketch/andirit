# S2.2 foundation implementation evidence (WP1–WP3)

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.2`.
**Evidence class:** producer observations. **Not independent qualification.**
**Baseline commit:** `febc28d92548fd7323294077c37444048741a8c8`
(tree `e1fa31ec23ef213629dd613e92073641bf2e6861`).
**Scope:** WP1 (Q-F2 encoding), WP2 (Q-F3 metadata validation), WP3 (A-01
symbol canonicalization + mapping contract). Extraction, minting, native
publication and downstream stages are **not** implemented.

---

## 1. Commands

```powershell
# Baseline (before changes)
dotnet test Andirit.slnx --nologo -v minimal

# Fails-before: new tests only (before implementation)
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --nologo -v minimal `
  --filter "FullyQualifiedName~S2_2FoundationBoundaryTests"

# After implementation
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --nologo -v minimal
dotnet test Andirit.slnx --nologo -v minimal

# Frozen semantic reference integrity
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -- `
  --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus `
  --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json `
  --output "$env:TEMP\oracle-s2-2-out.json"
```

## 2. Baseline (before changes, 2026-11-04)

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Andirit.LatticeDb.Tests | 43 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| Andirit.Tests | 325 | 0 | 0 |
| Andirit.CSharp.Tests | 120 | 0 | 0 |
| **Total** | **645** | **0** | **0** |

Targeted new-test baseline: **5 failed / 2 passed** (the 5 failures record the
original behavior/boundary; see §3).

## 3. WP dispositions, original behavior and tests

New tests live in `tests/Andirit.CSharp.Tests/S2_2FoundationBoundaryTests.cs`;
the frozen non-assembly CLI image lives in
`tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs` (a netmodule produced
by `csc /target:module`, embedded as base64; bytes frozen).

### WP1 — Q-F2 encoding boundary

- **Original behavior:** BOM-less UTF-16/UTF-32 ASCII-range bytes decoded as
  strict UTF-8 with NUL interleaving and **no refusal**; the source was
  verified. Fails-before: `SourceRead_RefusesBomlessUtf16AndUtf32NulInterleavedDecode`,
  `SourceRead_BomlessUtf8ContainingNulIsRefusedUnderBoundedContract`,
  `SourceRead_AmbiguousRefusalPreventsVerifiedCompilation` (`Assert.NotNull`
  refusal failures).
- **Correction:** `VerifiedCSharpSource.ReadAsync` refuses a BOM-less decode
  whose text contains U+0000 with `AmbiguousBomlessEncoding:<path>`; the raw
  byte SHA-256 remains honest and no verified identity is exposed.
- **Tests (5):** BOM-less UTF-16LE/BE and UTF-32LE/BE ASCII-range → refusal;
  BOM-less non-ASCII (strict UTF-8 refusal path) stays a non-ambiguous refusal;
  BOM-less UTF-8 with a literal NUL → refusal (documented bounded limitation);
  BOM presence excludes the bounded rule; an ambiguous refusal prevents verified
  compilation (`UnverifiedSource`).
- **Residual limit:** NUL detection is a **bounded heuristic**, not universal
  detection of unsupported encodings; a legitimate BOM-less UTF-8 file
  containing a NUL is refused. Supported set: UTF-8 ±BOM, UTF-16/32 **with**
  recognized BOM.

### WP2 — Q-F3 metadata validation

- **Original behavior:** `VerifiedMetadataReference.Capture` forced
  `reference.GetMetadata()` but that does **not** read the metadata tables, so a
  **truncated PE** image returned a captured identity (no exception). Fails-before:
  `MetadataCapture_RefusesTruncatedGarbageAndNonAssemblyImages`
  (`Assert.Throws` — no exception thrown at the truncated case).
- **Correction:** after capture, require `AssemblyMetadata`, require
  `MetadataReader.IsAssembly`, read the assembly definition and first type
  definition to force metadata-table reads, and classify
  `BadImageFormatException`/`InvalidOperationException` as
  `InvalidDataException("UnsupportedMetadataReference")`. No valid assembly
  identity can be exposed when validation fails.
- **Tests (1, multi-case):** valid assembly captured (positive); truncated PE,
  garbage bytes, and an embedded netmodule (no assembly manifest) each refused
  with `InvalidDataException`.
- **Residual limit:** a valid content identity still does not prove semantic
  usability; the contract guarantees usable assembly metadata was materialized
  before capture.

### WP3 — A-01 symbol canonicalization + mapping contract

- **Original behavior:** preprocessor symbols containing `,` (or `|`) were
  accepted and could collide with the canonical source-item separators.
  Fails-before: `VerifiedCompilation_RejectsPreprocessorSymbolsWithCommaOrPipe`
  (`Assert.Throws` — no exception thrown).
- **Correction:** `VerifiedCSharpCompilation.Create` rejects a preprocessor
  symbol containing `,` or `|` as `UnsupportedParseConfiguration`, alongside the
  existing path/option restrictions.
- **Tests (1, multi-case):** `"A,B"` and `"A|B"` refused; `"TRACE"`/`"FEATURE"`
  accepted and ordinally canonicalized (`FEATURE,TRACE`).
- **Mapping contract:** [mapping-contract.md](../planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md)
  — documentation only; no extraction/minting implemented. `G-MINT` review
  package: [g-mint-package.md](../planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md).

## 4. Changed files

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedCSharpSource.cs` | WP1 bounded BOM-less NUL refusal |
| `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` | WP2 stronger eager PE/assembly validation; WP3 symbol hardening |
| `tests/Andirit.CSharp.Tests/S2_2FoundationBoundaryTests.cs` | New WP1/WP2/WP3 tests |
| `tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs` | Frozen netmodule test fixture |
| `docs/planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md` | WP3 mapping contract (docs) |
| `docs/planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md` | G-CONFIRM preparation |
| `docs/planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md` | G-MINT preparation |

No public API, schema, frozen manifest, oracle or historical report changed.

## 5. After implementation (2026-11-04)

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Andirit.LatticeDb.Tests | 43 | 0 | 0 |
| Andirit.TreeSitter.Tests | 157 | 0 | 0 |
| Andirit.Tests | 325 | 0 | 0 |
| Andirit.CSharp.Tests | 127 | 0 | 0 |
| **Total** | **652** | **0** | **0** |

New tests: **7/7 passed** (baseline 120 → 127 in the C# project).

## 6. Frozen semantic reference integrity

- `docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py`: **PASS: 100 files
  verified; Git branch and pinned ancestry verified.**
- Manifest SHA-256: `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13`
  (matches the F12 acceptance record).
- Option A oracle: **185/185 checks passed, 0 failed** against the frozen
  `oracle-expected-r2.json`.

Frozen manifests, historical qualification reports and previously accepted
evidence are unmodified.

## 7. Qualification status

- Producer evidence only. `G-CONFIRM` and `G-MINT` remain **open** (independent
  reviewers; see the review packages).
- Extraction, association minting, native publication, S2.3–S2.5 and Stage 6 are
  not implemented or claimed.
- Residual limits in §3 are carried; S2.2 acceptance is not claimed.
