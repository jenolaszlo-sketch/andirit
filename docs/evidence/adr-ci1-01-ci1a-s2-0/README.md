# S2.0 corpus evidence package

Producer packet for commission `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01`.
Independent Q1 remains pending; producer results do not constitute acceptance.

## Contents

| File | Meaning |
|---|---|
| `oracle-run.json` | Full oracle report: environment, fixture manifest, semantics, identities, variant identities, Option A results, coverage, counts, checks. |
| `oracle-run.log` | Producer run summary (`114/114 checks passed, 0 failed`). |
| `environment-dotnet-info.txt` | `dotnet --info` capture (SDK 10.0.401, runtime 10.0.12). |
| `corpus-baseline_*.trx` | Four regression TRX files (645/645) captured after adding the corpus. |
| `source-evidence-manifest.json` | Self-excluded SHA-256 manifest over the corpus, oracle and evidence files. |
| `build-manifest.py`, `verify-manifest.py` | Manifest builder / verifier. |
| `.gitattributes` | `* -text -diff` to preserve exact bytes across checkouts. |

The frozen oracle expectation lives with the oracle at
`docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json` and is
hashed by the manifest.

## Reproduction

```powershell
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj `
  -- --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json `
     --output "$env:TEMP/oracle-run.json"
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/verify-manifest.py
```

## Integrity

- Identity digests are content-bound to the pinned runtime reference files and
  reproducible on the same runtime; the semantic and relational expectations are
  portable. See the bounded qualifications in
  `docs/planning/adr-ci1-01-ci1a-s2-0/qualification-plan.md` §6.
- The manifest is self-excluded and records the source binding. Do not edit frozen
  files; corrections require a preserved prior version, a described error and a
  requalification.

## Non-claims

No native upload, reopen or reader continuation; no crash/power-loss/multi-process
or Linux/cross-platform qualification; no production extraction implementation.
The oracle is a test oracle, not an alternative production indexing implementation.
