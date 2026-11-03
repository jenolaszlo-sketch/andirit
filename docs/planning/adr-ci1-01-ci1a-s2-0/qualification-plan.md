# Qualification plan and producer evidence

## 1. Sequence (R0–R6)

| Step | Deliverable | Result |
|---|---|---|
| R0 | Source/authority inventory | Canonical `main` = `b7fea0d2…`; tree `b4a76463…`; S2.1 implementation `0fa63ed2…` / tree `f713cb6e…`; F12 requirement recovered from obligation register §5; Option A mapping recovered from the S2.2 decision; existing S2.1 provenance APIs and frozen R2 vectors inspected |
| R1 | Corpus specification | [specification](semantic-corpus-specification.md), [case matrix](case-matrix.md), [oracle contract](oracle-contract.md) |
| R2 | Fixture implementation | `corpus/repo/` (5 projects, 11 C# files) and one source variant |
| R3 | Independent semantic oracle | `oracle/SemanticInspector.cs` + `Program.cs`; expectations established by direct Roslyn inspection |
| R4 | Identity/validity oracle | `oracle/R2Identity.cs`, `oracle/IdentityOracle.cs`; Option A cases |
| R5 | Producer qualification | 114/114 oracle checks; 645/645 regression |
| R6 | Q1 handoff | [q1-handoff.md](q1-handoff.md); frozen manifest |
| R1 rev (Q2) | Producer remediation | 130/130 R1 oracle checks; 645/645 regression; R1 evidence package |
| R2 rev (Q3) | Oracle remediation | Q2-F01/Q2-F02 corrections; `oracle-expected-r2.json`; **185/185** qualified checks; 645/645 regression; [R2 evidence](../../evidence/adr-ci1-01-ci1a-s2-0-r2/README.md) |

## 2. Gate results

- **Semantic truth** does not depend on the future extractor: the oracle references no production assembly.
- **Identity vs validity kept separate**: `asmctx3` is computed from the defining `compctx1`; validity is a separate gate over current `compctx2` and universe membership.
- **Dependency-sensitive invalidation** covered (A02, A08); **cross-project** covered (S26, many App→Core/Leaf invocations); **ambiguity/unresolved** covered (N01–N02); **dynamic** (N03); **external refusal** (E01); **inactive** (I01); **complete-empty** (A10/CE01); **incomplete scope** (IN01); **deterministic re-ingestion** (A09).

## 3. Commands

```powershell
# Oracle verification against the frozen document:
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj `
  -- --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json `
     --output "$env:TEMP/oracle-run.json"

# Existing regression:
dotnet test Andirit.slnx --no-restore

# Package integrity:
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/verify-manifest.py
```

## 4. Producer results (recorded)

- Oracle: **114/114 passed, 0 failed** (`../../evidence/adr-ci1-01-ci1a-s2-0/oracle-run.log`, `oracle-run.json`).
- Regression: **645/645 passed** (4 TRX files; 43 LatticeDb + 157 TreeSitter + 325 Andirit + 120 CSharp). Identical total to the accepted S2.1 baseline; no production file changed.
- Determinism: two runs in separate processes produced byte-identical output.
- Environment: .NET SDK 10.0.401, runtime 10.0.12, Windows x64, Roslyn 5.9.0.

### R2 remediation results (recorded)

- R2 oracle: **185/185 passed, 0 failed** against the frozen
  `oracle-expected-r2.json`, two processes byte-identical; 645/645 regression
  unchanged (`../../evidence/adr-ci1-01-ci1a-s2-0-r2/REGRESSION-RESULTS.md`).
- Historical reproductions from isolated pinned archives: 114/114 (Q1 pin) and
  130/130 (R1 pin); historical expected files byte-identical to pins.
- External mutation controls: ten targeted regressions (both Q2 findings
  verbatim, gate components, frozen-expected tampering, deleted-contribution
  reinjection, overload substitution) all exit 1 with the expected failing
  checks (`../../evidence/adr-ci1-01-ci1a-s2-0-r2/PROVENANCE-MUTATION-RESULTS.md`).

## 5. Preserved qualifications (not closed by S2.0)

| Finding | S2.0 handling |
|---|---|
| Q-F2 encoding boundary | not addressed; no encoding fixture added; remains before S2.2 acceptance |
| Q-F3 metadata validity | not addressed; remains before S2.2 semantic acceptance |
| A-01 comma-symbol canonicalization | not addressed (out of accepted input scope) |
| A-02 scope-digest composition | honoured: scope identity is not treated as a source-inventory digest |
| NEW-01 projection recovery | not addressed; before S2.3 |
| NEW-04 coverage merge | a scope-compatibility fixture is included; the defective merge is **not** used as an oracle and NEW-04 is **not** closed |
| S-01 defining-target binding | target identity uses the defining `compctx1`; full production binding remains S2.2/S2.5 |
| D3/D4 full qualification, native, Linux | out of scope; assigned later gates |

## 6. Bounded qualifications for Q1

1. **Component-digest forms are fixture-declared.** `identifierDigest`/`declarationDigest`/`shapeDigest` are supplied as frozen fixture inputs, not derived by an S2.2 canonicalization. This is bounded, documented ([identity mapping §5](identity-and-provenance-mapping.md)), and does not affect semantic truth, `compctx1`/`compctx2` or `usage4`.
2. **Target `asmctx3` values are host-toolchain-bound** through the content-bound framework reference identities. They are reproducible on the pinned runtime (10.0.12); the semantic and relational expectations are portable.
3. **The transitive-only Option A case is realized via a preprocessor-symbol change** (reproducing accepted experiment E2), which is the case in which the R2 project-reference text/tree item does not change. A dependency **source-byte** change would change the defining project's R2 material by construction.
4. **Modified fixture, not a live repository.** The corpus is a purpose-built project graph; it is not a snapshot of an external real-world repository. The F12 count minima are met and exceeded.
5. **Producer evidence only.** F12 closure requires independent Q1 qualification and owner disposition.

## 7. Remaining limitations

- No native publication, reopen, or reader continuation (explicitly out of S2.0 scope).
- No cancellation / crash / cross-platform qualification.
- The oracle validates semantics and identity algebra; it does not exercise the eventual production result enums (no new production type is introduced).
