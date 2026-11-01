# Independent Review Handoff — ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01-Q1

**Reviewer commission:** `ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01-Q1`
**Producer:** REM-01 implementation agent (not the independent reviewer).
**Stage 2:** remains unauthorized.

---

## 1. Exact source

| Item | Value |
|---|---|
| Repository | `D:\projects\Andirit`, `main` |
| Starting HEAD | `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` |
| Starting tree | `320e965f219d0b615da29665d7fa3e85f8bbfc15` |
| Contract commit | `c13137d54e0ee48c0f7c7383b03bf43bed74a936` |
| Contract tree | `380104122e96e5a164a2a8375ffdd7f37c513e1f` |
| Implementation commit | `fc515ffbc20adc286fb57339130fa5ccbf466bb8` |
| Implementation tree | `8c2d7d08be0f7294332d48ec8cb3b2c98476a9fa` |
| Evidence/handoff commit | `e7b4258a3e4c00d25ad609c13ce90eb2d6f65298` (tree `04af74d1134d98567a771674c46db77787d8ed47`) |
| Branch/worktree | `main`, single canonical worktree; no push |

Expected linear ancestry:
`4c6d26a -> c13137d -> fc515ff -> <evidence commit>`.

---

## 2. Read first

1. `docs/planning/adr-ci1-01-ci1a-s2-rem-01/README.md`
2. `contract-reconciliation.md`
3. `identity-contract.md`
4. `coverage-and-availability.md`
5. `publication-uncertainty.md`
6. `stage2-gate-reconciliation.md`
7. `evidence-integrity-erratum.md`
8. `finding-disposition.md`
9. `qualification-matrix.md`
10. `docs/evidence/adr-ci1-01-ci1a-s2-boundary-rem-01.md`

---

## 3. Verify

### 3.1 Source and preservation

- Confirm the commits/trees above and linear ancestry.
- Confirm only the listed production, test and documentation paths changed.
- Confirm no accepted owner decision, frozen review package, tag, push or
  branch/worktree was altered.
- Confirm the canonical worktree is clean after the evidence commit.

### 3.2 Identity

- Independently recompute the frozen R2 `usage4` vector
  `bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af`
  from the inputs in `identity-contract.md` §3.1. Do not use the production
  helper as the oracle.
- Verify the same site with
  `asmctx3:sha256:fb3e0561...` yields `b874bdc5...`.
- Verify `asmctx3` composition, canonical format, unavailable/fabricated
  rejection, payload recomputation, and non-canonical old-record refusal.
- Verify different semantic units yield different `usage4`; same semantic fact
  from different storage units unions contributors.

### 3.3 Coverage and zero-fact

- Run `CompleteEmptyAnalysisSurvivesReplacementAndReopen`.
- Verify a complete-empty unit persists unit/meta, has no target page, reopens as
  `Available`/zero with complete coverage.
- Verify partial coverage returns `IncompleteAnalysis`, not a complete negative.
- Verify no-analysis returns `AnalysisUnavailable`.
- Verify deletion removes coverage.
- Inspect the native labels directly if desired: `AndiritOccurrenceUnit`,
  `AndiritOccurrencePage`, `AndiritOccurrenceMeta`.

### 3.4 Unknown outcome

- Run `AfterCommitAcknowledgementLossRefusesCurrentStateAndReopenSeesDurableState`.
- Verify a post-commit fault yields
  `CodeGraphCommitOutcomeUnknownException`, current-state queries are refused,
  and reopen sees the committed state.
- Verify known pre-commit failure (`N6`) preserves the prior publication and does
  not poison the instance.
- Do not treat this as crash/power-loss or multi-process evidence.

### 3.5 Regression

Run:

```
dotnet build Andirit.slnx
dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --no-build
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj --no-build
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj --no-build
dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj --no-build
```

Expected producer counts: 325 / 43 / 69 / 157 = 594 passed; 0 failed/skipped.
N1–N8 are in the native suite. Re-run independently and record the actual
environment.

### 3.6 D3/D4/D5

- D3: `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`; confirm no graph
  identity/count/traversal change.
- D4: applicable stale/superseded/failed/reopen/zero-fact/unknown tests; full
  S-02 remains later.
- D5: schema/namespace refusal; additive coverage JSON; no migration or
  reinterpretation of pre-REM identities.

### 3.7 Stage 2 documents

- Confirm the external-target row is refusal, not a fact.
- Confirm S-01/IF-03 preauth vs implementation vs acceptance separation.
- Confirm fixture timing and the existing fixture's below-minima status.
- Confirm historical/Linux timing and the no-substitution rule.
- Confirm the "five vs six" count, D4 count, duplicate §15 and specification path
  corrections.

---

## 4. Adversarial focus

Challenge at least:

1. equal descriptors in distinct compilations;
2. external target without verified defining compilation;
3. confirmed fact within incomplete coverage;
4. complete analysis with zero facts;
5. no analysis with zero facts;
6. incomplete analysis with zero facts;
7. nonempty-to-empty replacement;
8. coverage persistence/reopen;
9. coverage invalidation after deletion;
10. source-unit addition invalidating completeness;
11. failed publication preserving prior facts/coverage;
12. superseded publication;
13. commit success with acknowledgment loss;
14. retry with same identity;
15. incompatible payload;
16. projection reconstruction after uncertainty;
17. graph-only historical store;
18. unsupported occurrence schema;
19. target-page/publication mismatch;
20. D3 native graph enumeration after occurrence publication.

---

## 5. Unresolved qualifications (not acceptance)

- Full Stage 2 Roslyn extraction, `compctx1` construction, live universe and
  semantic binding.
- Conditional-inactive scanning.
- Reader continuation/cross-target surface.
- Qualifying real-project fixture and independent oracle.
- Authentic historical fixtures for available writers.
- Linux native suite.
- Crash/power-loss/multi-process recovery.
- Independent acceptance of this remediation.

The reviewer must not silently remediate the implementation. Produce a separate
disposition.

## 6. Producer statement

No independent qualification is claimed. Stage 2 is not authorized by this
handoff. The producer has not edited accepted owner decisions or frozen review
evidence.
