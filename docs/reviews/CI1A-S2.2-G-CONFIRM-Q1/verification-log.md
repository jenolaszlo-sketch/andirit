# G-CONFIRM — independent verification log

**Commission:** `ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1`. **Date:** 2026-11-04.
**Repository:** `D:\projects\Andirit`, canonical `main`.
**Environment:** Windows x64; .NET SDK 10.0.401; runtime 10.0.12; Roslyn
5.9.0; PowerShell + CPython.

This records the exact independent reproduction performed. It does not rerun the
full producer solution; the frozen oracle and the evidence verifiers are the
authority-relevant reproductions. Passing producer tests are **not** treated as
independent confirmation (the identity subset is cited only as corroboration).

---

## 1. Baseline authentication

| Command | Result |
| --- | --- |
| `git status --short --branch` | `## main...origin/main` (clean, in sync) |
| `git rev-parse HEAD HEAD^{tree}` | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` / `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| `git merge-base --is-ancestor e0d9110 main` | true (acceptance commit in ancestry) |
| `git merge-base --is-ancestor 526d2c0 main` | true (prepared HEAD) |
| `git merge-base --is-ancestor d5a785f main` | true (Q2 candidate) |
| `git worktree list --porcelain` | one worktree (`D:/projects/Andirit`) |
| `git stash list` | empty |
| `git fetch origin` (no prune) | no ref change; `origin/main` = `e0d9110` |

Accepted role commits/trees (reproduced exact): Q1 `38d4e51`/`ffca6b84`; R1
implementation `d5644d8`/`bcbf6511`; R1 evidence `b4eff01`/`ca448ef7`; Q2/candidate
`d5a785f`/`5d8aa21`; preparation `526d2c0`/`d581f25b`; foundation acceptance
`e0d9110`/`ba9338d`.

## 2. Independent tuple reconstruction (standalone)

Script: [`evidence/independent-reconstruction.py`](evidence/independent-reconstruction.py)
(no Andirit imports). Output [`evidence/independent-reconstruction.out`](evidence/independent-reconstruction.out):

```
usage4-a bd0487b749795a3e6b61028c7c9cf2d663530cae901ac6d9f35bc311536dc3af match= True
usage4-b b874bdc5afa194e0e237d0b1d7d36b3fa3dd767799e0dc62f5f1399f00b90bca match= True
compctx1-a compctx1:sha256:71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333 match= True
```

Confirms the R2/`usage4` and `compctx1` tuple algebra, field order and big-endian
length-prefix encoding directly from the contract.

## 3. Frozen R2 oracle replay

```
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj \
  -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus \
  --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json \
  --output <stage>/oracle-r2.json
```

| Item | Value |
| --- | --- |
| Result | `S2.0 oracle: 185/185 checks passed, 0 failed` (exit 0) |
| Output SHA-256 | `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (== accepted R2 output) |
| Checks by kind | identity 6, semantic 87, provenance 35, cross-view 31, lifecycle 7, model-assertion 2, coverage 8, frozen 9 |
| Frozen `expected.<section>` comparisons | 9/9 MATCH (semantics, identities, variantIdentities, variants, crossView, e02, coverage, counts, lifecycle) |
| Expected doc SHA-256 | `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` (176 checks + 9 = 185) |
| Failed checks | none |

Evidence: [`evidence/oracle-run.txt`](evidence/oracle-run.txt),
[`evidence/oracle-section-diff.txt`](evidence/oracle-section-diff.txt),
[`evidence/oracle-r2.json`](evidence/oracle-r2.json).

## 4. Trust-anchor / evidence preservation

| Command | Result |
| --- | --- |
| `python docs/evidence/adr-ci1-01-ci1a-s2-0-r2/verify-manifest.py` | `PASS: 100 files verified; Git branch and pinned ancestry verified.` |
| `python docs/reviews/CI1A-S2.2-R1-evidence/verify-evidence.py` | `PASS: 50 R1 evidence files verified` |
| `python docs/reviews/CI1A-S2.2-Q2-evidence/verify-q2.py D:/projects/Andirit` | `PASS: 93 Q2 evidence files and 3 reviewer source/report files verified` |

## 5. Corroboration (not independent authority)

| Command | Result |
| --- | --- |
| `dotnet test tests/Andirit.Tests/Andirit.Tests.csproj --filter FullyQualifiedName~OccurrenceIdentityRemediationTests` | 9 / 0 / 0 |

The frozen identity tests reproduce the frozen `usage4` vector and discriminators;
this corroborates the production helper but is **not** the independent authority
(the standalone reconstruction in §2 and the oracle in §3 are).

## 6. Frozen corpus output reproduction

The replay output is byte-identical (SHA-256 `97F6C5E5…`) to the accepted R2/Q3
output, and its nine compared sections are byte-identical in canonical JSON to the
frozen `oracle-expected-r2.json`. This reproduces the accepted R2 reference
behavior without regenerating or modifying any frozen artifact.

## 7. Independent reproductions and hashes

Evidence hashes: [`evidence/evidence-hashes.txt`](evidence/evidence-hashes.txt).

- `oracle-expected-r2.json` `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`
- `oracle-r2.json` (this run) `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`
- `independent-reconstruction.py` `B646CA909F0F4B34D46CF05C76DB3DD494F137246EF92E0F0FD1A0D64192E124`

## 8. Unresolved authority questions

None. Documentation corrections **C1–C4** (see
[adversarial-findings.md](adversarial-findings.md)) are recorded for the owner;
they do not alter any authority or expected value.

## 9. Boundaries of this verification

- No production mapper, extraction, minting, association, persistence or reopen
  behavior is claimed or tested.
- No new capability is qualified; only the accepted WP1–WP3 foundation and the
  frozen R2 reference are reproduced.
- No frozen evidence, historical report or accepted decision was modified.
