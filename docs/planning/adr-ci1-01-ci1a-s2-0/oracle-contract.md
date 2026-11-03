# Oracle contract

**Artifact:** `oracle/SemanticCorpusOracle.csproj` — a standalone console project. **Independence rule:** it references only `Microsoft.CodeAnalysis.CSharp` 5.9.0. It does **not** reference `Andirit`, `Andirit.Abstractions`, `Andirit.CSharp` or any production assembly, so it cannot call the production identity helpers or the future S2.2 occurrence extractor it is meant to test.

## 1. Three separated questions

Following the commission, every case distinguishes:

1. **Semantic truth** — what symbol the real Roslyn compilation resolves. Obtained with `CSharpCompilation.GetSemanticModel` + `GetSymbolInfo`/`GetOperation`, and recorded as `<assembly>!<documentation-comment-id>` of the defining declaration.
2. **Target identity** — the R2 `asmctx3` for a source-defined target, computed from the defining compilation's R2-faithful `compctx1`.
3. **Provenance validity** — whether the analysis is still valid under the current `compctx2` and `compilation-universe1` membership. A valid-looking identity is not sufficient.

## 2. Independent identity reconstruction

`oracle/R2Identity.cs` re-implements the accepted R2 tuple algebra from the
contract documents — the big-endian 32-bit length-prefixed SHA-256 encoding, the
`compctx1`/`compctx2` descriptor tuple, `asmctx3`, `usage4`, and the options
digest. It is **not** a thin wrapper over production code.

Independence is anchored, not asserted. On startup the oracle reproduces frozen
accepted vectors using fixed inputs:

| Anchor | Frozen value |
|---|---|
| `compctx1` synthetic R2 tuple | `compctx1:sha256:71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333` |
| `compctx2` versioned domain + canonical project-ref item | `compctx2:sha256:9e55d2dbb920ed6a03935e115cf65d48fbd61f9185a7cd3030e03cd6fa6dd924` |
| `usage4` frozen R2 vector | `bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af` |

These vectors exist in the accepted test suite
(`tests/Andirit.CSharp.Tests/VerifiedCompilationProvenanceTests.cs`,
`tests/Andirit.Tests/OccurrenceIdentityRemediationTests.cs`) but the oracle
reproduces them with its own implementation. Two independent implementations of
the same frozen contract agreeing on the frozen vectors is meaningful evidence;
the oracle never calls the helper under test.

The R2 project-reference item for the legacy `compctx1` is the frozen text/tree
form `compilation|<assembly>|<metadata-identity>|<path=sha256,...>` (recovered
from the R2 authority copy `../../evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs`).
The verified `compctx2` uses the S2.1 item `compilation-context|<dependency compctx2>`.
Both are implemented independently.

## 3. Avoiding circular validation

- Expected **semantic** results are hard-coded in the oracle for every named case
  (`Program.cs` case table) from reading the frozen source, then compared to the
  compiler's actual binding. A mismatch fails the run; the expectation is fixed
  before the comparison.
- Expected **identity** relations (equality/inequality across variants) are
  asserted directly (Option A cases A01–A10).
- The frozen document `oracle/expected/oracle-expected.json` is the comparison
  authority for the `semantics`, `identities`, `variantIdentities`, `coverage`
  and `counts` sections (`--expected`). It was produced once from the independent
  inspector, reviewed, and frozen; it is never regenerated silently, and it is
  listed in the evidence manifest so drift is detectable.
- R2 (`oracle/expected/oracle-expected-r2.json`) extends the compared sections
  to `semantics`, `identities`, `variantIdentities`, `variants`, `crossView`,
  `e02`, `coverage`, `counts`, `lifecycle`. The `crossView` section enforces
  Q2-F01 (one canonical target-description pathway: reduced symbols normalize
  `ReducedFrom` then `OriginalDefinition`; shape is the fixture-declared
  `method|parameterCount`, never Roslyn generic arity) against independently
  transcribed anchors in `oracle/FrozenExpectations.cs`, and the `e02` section
  enforces Q2-F02 (defining-provenance eligibility through the single
  `ProvenanceGate` predicate: recorded == current verified == universe-member
  context). `checks`, `summary`, `environment`, `fixtureManifest` and
  `corpusRoot` are not compared.
- No test regenerates expected results from the same code path it checks.

## 4. Record shape

Each semantic record contains: case id, source file, category, scope
classification, actual resolution (`Resolved`/`Ambiguous`/`Unresolved`/`Dynamic`/
`Inactive`), symbol key, defining assembly, stable span, and detail. Identity
records contain `compctx1`, `compctx2`, `asmctx3` and `usage4` values.

## 5. Frozen oracle immutability

The completed corpus freezes source files, project/reference configuration,
expected outputs, toolchain information, a reproducible command, and a
self-excluded SHA-256 manifest. A later correction must preserve the previous
accepted version, describe the error, record the replacement and requalify the
affected cases. Frozen R2 vectors are never rewritten.

## 6. Reproduction

```powershell
# Verify the frozen oracle (fails on any drift):
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj `
  -- --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json `
     --output "$env:TEMP/oracle-run.json"

# Integrity of the frozen package:
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/verify-manifest.py
```
