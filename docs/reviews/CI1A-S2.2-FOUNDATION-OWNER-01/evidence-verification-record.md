# Evidence verification record

**Commission:** `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-01`. **Date:** 2026-11-04.
**Environment:** Windows x64; .NET SDK 10.0.401; runtime 10.0.12; Roslyn 5.9.0
(as recorded by Q1/R1/Q2). Repository `D:\projects\Andirit`, canonical `main`.

This records the independent re-verification performed while preparing the
acceptance package. It re-confirms the Q2 report; it is **not** a new review
verdict and does not re-qualify production.

---

## 1. Git state and ancestry

```
git status --short --branch        -> ## main...origin/main [ahead 38] (clean)
git rev-parse HEAD 'HEAD^{tree}'   -> d5a785f898ce7877caef10be53074f2c2ae3a1cd
                                      5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c
git worktree list --porcelain      -> one worktree (D:/projects/Andirit)
git stash list                     -> (empty)
git rev-list --count origin/main..main  -> 38
git rev-list --count main..origin/main  -> 0
git merge-base --is-ancestor origin/main main -> exit 0
```

Role commit trees reproduced exactly (see the draft decision §2 table).
Consecutive parent chain verified:
`38d4e51 → d5644d8 → b4eff01 → d5a785f`.

Bounded diffs:

```
git diff --name-only b4eff01^ b4eff01 -- src tests  -> (none)
git diff --name-only d5a785f^ d5a785f -- src tests  -> (none)
git show --stat d5644d8 ->
  src/Andirit.CSharp/MetadataReferenceValidation.cs   | 200 ++ (add)
  src/Andirit.CSharp/VerifiedCSharpCompilation.cs     |  10 +-
  tests/Andirit.CSharp.Tests/S2_2MetadataRemediationTests.cs | 194 ++ (add)
```

## 2. Evidence verifiers (read-only)

```
python -X utf8 docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py
  -> PASS: 100 files verified; Git branch and pinned ancestry verified. (exit 0)

python -X utf8 docs/reviews/CI1A-S2.2-Q2-evidence/verify-q2.py D:/projects/Andirit
  -> PASS: 93 Q2 evidence files and 3 reviewer source/report files verified (exit 0)

python -X utf8 docs/reviews/CI1A-S2.2-R1-evidence/verify-evidence.py
  -> PASS: 50 R1 evidence files verified (exit 0)
```

## 3. Frozen R2 oracle replay

```
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj \
  -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus \
  --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json \
  --output <temp>/oracle-r2.json
  -> S2.0 oracle: 185/185 checks passed, 0 failed. (exit 0)
```

The accepted output SHA-256 is
`97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`
(byte-identical to the accepted Q1/Q3 output, per the Q2 report §1).

## 4. Test accounting reconciled (Q2 report §6)

| Run | Result |
| --- | --- |
| Full unfiltered solution | 692 / 0 / 0 |
| Producer metadata remediation subset | 40 / 0 / 0 (subset of 692) |
| Producer WP1/WP2/A-01 foundation subset | 7 / 0 / 0 (subset of 692) |
| Existing frozen identity subset | 9 / 0 / 0 (subset of 692) |
| Independent Q2 reviewer suite | 40 / 0 / 0 (separate process; also 37/1 intermediate, 39/0 intermediate) |
| Historical Q1 reviewer probes | 45 passed / **3 failed** / 0 skipped |

The three Q1 failures are `ObserveMalformedMetadataStillExposesIdentity` for
`first-type-name`, `later-type-name` and `method-signature`: expected historical
observation mismatches invalidated by the remediation. **They are not counted as
passing tests** and are not added to any green total. Counts are deliberately not
summed into a single green number (Q2 §6).

## 5. Q1 finding closure (Q2 §2)

Q1-F01, Q1-F02, Q1-F03, Q1-F04 and Q1-F05 are **CLOSED** by Q2 at the bounded
correction/specification scope. No finding is OPEN or INSUFFICIENT EVIDENCE within
that scope; no new demonstrated production defect was assigned.

## 6. Git fetch and branch audit summary

```
git fetch origin                -> no new/changed refs; origin/main still 31c33a9…
git fsck --no-progress          -> no errors/warnings; no dangling/unreachable commits
```

- 26 local branches; 9 with unique commits, each backed by an identical-SHA
  `origin` ref.
- 21 remote-tracking refs; the unique-commit refs are all already on `origin`.
- `origin/main..main` diff: 487 paths (450 docs, 20 src, 16 tests, 1 ROADMAP.md);
  no build-artifact/secret patterns.

Details in the [git branch audit](git-branch-audit-and-push-readiness.md).

## 7. What this record does not do

- It does not re-run the full 692-test solution (the Q2 report and raw TRX remain
  the authority; this preparation re-ran the frozen oracle and the evidence
  verifiers only).
- It does not re-qualify production, does not issue a `G-CONFIRM`/`G-MINT`
  verdict, and does not authorize anything.
- It introduces no production, test, frozen-evidence or accepted-decision change.
