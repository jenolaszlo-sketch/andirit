# S2.0-Q1 independent reviewer handoff

**Commission to create:** `ADR-CI1-01-CI1A-IMPL-01-S2.0-CORPUS-01-Q1`.
**Reviewer rule:** do not treat the producer's conclusions as ground truth. Reconstruct the compilations, recompute the expected semantic relationships and identities independently, and add cases the producer did not anticipate.

## 1. Source binding

- Repository `D:\projects\Andirit`, branch `main`.
- Starting commit `b7fea0d219409b279b78317e32bdc3c8b5ae61ee`, tree `b4a764637498ed8e8bddac28fb0b0b3716b7ffee`.
- Reviewed S2.1 implementation `0fa63ed2a0f7f1b5ffe21f3791e93e81eb4cb329`, tree `f713cb6e4a1dbfb4f531f6971a37893902b41435`.
- Final S2.0 commit/tree recorded in `../../evidence/adr-ci1-01-ci1a-s2-0/source-evidence-manifest.json` and in the final handoff. No push occurred.

## 2. Reproduce the package

```powershell
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj `
  -- --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json `
     --output "$env:TEMP/q1-oracle-run.json"
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0/verify-manifest.py
```

Expected: `114/114` checks pass and the manifest verifies. Environment: .NET SDK 10.0.401, runtime 10.0.12, Roslyn 5.9.0.

## 3. Independent probes

The reviewer must be able to, without the producer oracle generator as authority:

1. **Reconstruct compilations** from `corpus/repo/` and the declared options/references; confirm error-free baseline compilation of `Leaf`, `Core`, `App`, `Solo`.
2. **Recompute semantic relationships** for every `Sxx`/`Nnn`/`Enn` marker with raw `GetSymbolInfo` and compare to `case-matrix.md`.
3. **Challenge target selection** for S04 (constructed vs defining generic), S06 (reduced vs unreduced extension), S09/S11 (compile-time vs dispatch), S12 (interface vs implementation).
4. **Challenge source spans** against the syntax tree using the identifier-token convention.
5. **Challenge defining-compilation attribution** for S26 and the cross-project invocations.
6. **Challenge identity collision assumptions**: recompute `asmctx3` for two distinct declarations with identical declared component forms (A05) and confirm distinct `compctx1` separates them.
7. **Challenge stale `compctx2`**: rebuild `symbols-leaf-changed` and confirm `asmctx3(Core)` is stable while `compctx2` changes at `Leaf`, `Core`, `App`.
8. **Challenge incomplete-universe handling**: omit a declared unit and confirm refusal, not complete-empty.
9. **Challenge complete-empty classification**: confirm `Solo` produces zero occurrences under a complete scope.
10. **Challenge external-symbol refusal**: confirm `E01` resolves in Roslyn but is not a publishable source-defined target.
11. **Recompute selected identities** with an independent implementation of the R2 tuple and compare.
12. **Add unanticipated cases** (for example: same-name type in two projects; anonymous/λ; static constructor; nested generic; ref/out parameters; same `asmctx3` collision attempt).

## 4. Acceptance requirements

Independent Q1 acceptance requires:

- Reproduced `114/114` and manifest verification, or documented discrepancies.
- At least one added adversarial case beyond the producer corpus.
- Confirmation that no production extraction code changed.
- An explicit owner disposition closing or bounding F12.

F12 is not closed by producer tests alone. The four permitted dispositions are
`READY FOR INDEPENDENT Q1`, `READY FOR Q1 WITH QUALIFICATIONS`, `REMEDIATION REQUIRED`,
and `BLOCKED`.

## 5. Files of interest

- Fixtures: `docs/planning/adr-ci1-01-ci1a-s2-0/corpus/`
- Oracle: `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/`
- Frozen oracle: `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json`
- Evidence: `docs/evidence/adr-ci1-01-ci1a-s2-0/`
