# Mutation matrix — failing before, passing after

All figures are reproduced by the two preserved harnesses:

- **Before:** [`analysis/before/Program.cs`](analysis/before/Program.cs) — faithful
  pure-Roslyn transcription of the cited prior checker; output
  [`evidence/before/before-run.json`](evidence/before/before-run.json).
- **After:** [`analysis/oracle/Program.cs`](analysis/oracle/Program.cs) — corrected
  semantic oracle; output
  [`evidence/after/after-run.json`](evidence/after/after-run.json).

Machine-readable side-by-side:
[`evidence/mutation-comparison.json`](evidence/mutation-comparison.json).

## 1. The four mandatory Q2 regressions

| Q2 mutation (named regression) | Original → mutated value | Prior checker | Corrected oracle | Primary reason code |
| --- | --- | --- | --- | --- |
| `outcome-external-to-confirmed` (R2 `M01`) | C10 `ExpectedOutcome` `external-no-confirmed-local-target` → `confirmed` | **Accepted** (`Detected=false`) | **Rejected** | `OUTCOME_CLASS_MISMATCH` |
| `outcome-confirmed-to-external` (R2 `M02`) | C01 row 0 `ExpectedOutcome` `confirmed` → `external-no-confirmed-local-target` | **Accepted** | **Rejected** | `OUTCOME_CLASS_MISMATCH` |
| `outcome-confirmed-to-deferred` (R2 `M03`) | C01 row 0 `ExpectedOutcome` `confirmed` → `deferred-method-group` | **Accepted** | **Rejected** | `OUTCOME_CLASS_MISMATCH` |
| `outcome-invalid-enum` (R2 `M04`) | C01 row 0 `ExpectedOutcome` `confirmed` → `invalid-q2-outcome` | **Accepted** | **Rejected** | `OUTCOME_UNRECOGNIZED` |

The corrected-oracle rejection is not incidental: for `M01` the derived class is
`external` because the bound target is
`System.Console, Version=10.0.0.0, PublicKeyToken=b03f5f7f11d50a3a` with **zero**
source locations; for `M02`/`M03` the bound target is the eligible local
`M:Logger.Write(System.String)` in assembly `repeated` with a source declaration
in `Repeated.cs`.

## 2. Full required mutation set

| Required result | R2 mutation | Original → mutated value | Rejected | Primary reason code | Unrelated failure first |
| --- | --- | --- | --- | --- | --- |
| External → confirmed | `M01` | C10 outcome → `confirmed` | Yes | `OUTCOME_CLASS_MISMATCH` | No |
| Confirmed → external | `M02` | C01 outcome → `external-no-confirmed-local-target` | Yes | `OUTCOME_CLASS_MISMATCH` | No |
| Ambiguous → confirmed | `M05` | C11 outcome → `confirmed` | Yes | `OUTCOME_CLASS_MISMATCH` | No |
| Unresolved → confirmed | `M06` | C11 outcome → `confirmed` | Yes | `OUTCOME_CLASS_MISMATCH` | No |
| Confirmed → wrong overload | `M07` | C02 `Hit(int)` signature → `CI1Corpus.Overloads.Hit(string)` | Yes | `TARGET_IDENTITY_MISMATCH` | No |
| Confirmed → wrong assembly | `M08` | C03 signature → `System.Console.WriteLine(string)` | Yes | `TARGET_IDENTITY_MISMATCH` | No |
| Confirmed → unrelated indexed method | `M09` | C01 signature → `CI1Corpus.Target.Work()` | Yes | `TARGET_IDENTITY_MISMATCH` | No |
| Correct target → incorrect span | `M10` | C01 `Span[1]` 9 → 10 | Yes | `COORDINATE_LEXEME_MISMATCH` | No |
| Correct entry → duplicate entry | `M11` | C01 row duplicated (32 → 33 rows) | Yes | `DUPLICATE_OCCURRENCE` | No |
| Correct expected outcome | `P01` | unchanged | **Accepted** (control) | — | No |

Every rejection is produced by the intended semantic reason, and no mutation is
rejected by an unrelated coordinate/duplicate/syntax failure first. A mutation
counts only when the primary reason code equals the intended code, so a crash or
a missing fixture would not count.

## 3. Positive controls

1. **Unchanged manifest** → 32 sites, **0 failures**. The corrected checker does
   not reject the frozen expectations.
2. **`P01` no-op mutation** → accepted (`Detected=false`), proving the checker is
   not rejecting every input.
3. **All 32 rows retain intended semantics** → every row's derived class equals
   its declared class (`semanticIdentity.allRowsDerivedMatchesDeclared = true`).

Class distribution over the 32 rows (independently derived):
`confirmed` 21, `external` 2, `ambiguous` 1, `unresolved` 1, `unsupported` 7.

## 4. Prior-checker regression coverage preserved

The before-harness replays all 13 Q2 mutations. The five Q1 mutations
(`wrong-target`, `wrong-coordinate`, `duplicate-row`, `wrong-overload`,
`wrong-outcome`) and the added coordinate mutations (`wrong-start-line`,
`wrong-end-line`, `wrong-end-column`) are still detected, exactly as recorded by
Q2. R2 neither weakens nor removes them.

## 5. Adversarial collision (F01)

[`evidence/after/after-run.json`](evidence/after/after-run.json) →
`adversarialScenarios[0]`:

- documentation ID of the indexed local `Shadow` declaration and of the bound
  platform call **coincide** (`M:System.Console.WriteLine(System.String)`);
- the indexed local declaration **is** eligible
  (`Shadow, …`, 1 source location);
- the bound target is **not** eligible local
  (`System.Console, Version=10.0.0.0, …`, 0 source locations);
- derived class for the call = **`external`**.

So an unrelated indexed method with the same documentation ID cannot absorb the
external call. Fixtures:
[`analysis/fixtures/f01-collision/`](analysis/fixtures/f01-collision/).

## 6. Reproduction

See [`reproduction.md`](reproduction.md). The raw hashes of every checker source
and evidence file are in
[`evidence/evidence-hashes.json`](evidence/evidence-hashes.json).
# Integration amendment — mutation matrix (CI-1-SCOPE-R3)

> **APPEND-ONLY AMENDMENT BLOCK** — proposed by CI-1-SCOPE-R3 for
> `docs/planning/ci1-scope-r2/mutation-matrix.md`. Append verbatim. Historical
> R2 content above remains unchanged.

---

## R3 mutation matrix (CI-1-SCOPE-R3)

The corrected oracle is `analysis/oracle-r3/`. Full raw results are recorded in
the R3 evidence packet (`evidence/tier1-r3/r3-run.json` in the R3 review
workspace; regenerate with the command in the R3 handoff).

Totals: **198 mutations** (tier A: Q2 named + R2 required set, 12; tier B:
complete Q3 adversarial set replayed, 38; tier C: R3 regressions, 148).
**196 rejected for the intended reason; 2 positive controls accepted
(P01, R14); 0 crashes; no unrelated-only rejections.** Baseline: 32 sites,
0 failures, universe complete, class distribution confirmed 21 / external 2 /
ambiguous 1 / unresolved 1 / unsupported 7.

| Group | Result |
| --- | --- |
| Q2 named (M01–M04) | rejected, same intended reason codes as R2 |
| R2 required set (M05–M11) | rejected, same intended reason codes as R2 |
| Q3 OUT-01…OUT-10 (accepted by R2: 9) | all rejected — `OUTCOME_PROVENANCE_MISMATCH` / `CONDITIONAL_ACTIVITY_MISMATCH` |
| Q3 VOCAB-01…10 | all rejected — `OUTCOME_UNRECOGNIZED` |
| Q3 TARGET-01…07 (accepted by R2: TARGET-02/03/07) | all rejected — `CORPUS_ROW_*` / `REPOSITORY_PATH_MISMATCH` / `BASIS_UNRECOGNIZED` / `TARGET_IDENTITY_MISMATCH` |
| Q3 CORPUS-01…04 (accepted by R2) | all rejected — `CORPUS_ROW_MISSING` / `CORPUS_ROW_UNEXPECTED` |
| Q3 MAL-01…07 (crashed by R2: MAL-02/03/04) | all rejected without crash — `COORDINATE_LEXEME_MISMATCH` / `MANIFEST_STRUCTURE_INVALID` |
| R01 confirmed-family substitution sweep (21 rows × 6 labels = 126) | 126/126 rejected — 100 × `OUTCOME_PROVENANCE_MISMATCH`, 26 × `CONDITIONAL_ACTIVITY_MISMATCH` (with documented co-fires) |
| R02–R13 named regressions (external subtypes, conditional bypass, wrong definition/overload/owner/path/basis, missing/extra rows, malformed structure, unverifiable identity) | all rejected for their intended reasons |
| A/B two-assembly identity scenario | wrong-assembly expectation rejected (`TARGET_DEFINING_PROJECT_MISMATCH`); correct expectation accepted |
| F01 collision scenario | platform call stays external; metadata rule now structurally rejects a source-built same-doc-ID shadow |
| Positive controls (P01, R14) | accepted |

Replay notes: Q3's OUT-08 probe selected the method-group row via
`SiteByCase("C08")`; the R3 replay implements the intended delegate-row pair
(by outcome value) and rejects both directions. M10 additionally trips the
occurrence-universe check because the span is part of the frozen occurrence
identity — the intended `COORDINATE_LEXEME_MISMATCH` still fires first.
