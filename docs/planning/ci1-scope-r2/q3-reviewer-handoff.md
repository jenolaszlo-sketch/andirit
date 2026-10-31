# Q3 reviewer handoff — independent F04 qualification

This handoff asks an **independent** reviewer (Q3) to qualify the R2 correction
of F04. R2 does **not** claim independent qualification; it claims only that the
four Q2 failures are now rejected for the intended semantic reasons and that
positive controls pass.

## 1. What Q3 must qualify

1. **Faithful before-state.** Confirm the before-harness reproduces the four Q2
   failures against the cited prior checker, and that its transcription of the
   audit/mutation logic is faithful (not weakened).
2. **Genuine after-rejection.** Confirm each of the four mandatory regressions is
   rejected **because of** the intended semantic reason (`OUTCOME_CLASS_MISMATCH`
   for three, `OUTCOME_UNRECOGNIZED` for one), not because of a crash, a missing
   fixture, or an unrelated coordinate/duplicate failure.
3. **Independent evidence direction.** Confirm the corrected oracle derives
   outcomes from source + compiler context and does **not** compare one editable
   manifest field to another. The key test: change the compiler context (for
   example, make the C10 target an eligible local definition) and confirm the
   derived class changes accordingly without editing `ExpectedOutcome`.
4. **Positive controls.** Confirm the unchanged manifest passes 32/32 and that
   the no-op control is accepted.
5. **Eligibility strength.** Confirm the F01 collision is `external` even though
   the documentation IDs coincide, and that a documentation ID alone never
   proves local identity.
6. **Bounded vocabulary.** Confirm the declared-outcome vocabulary is exact and
   that an unknown string is rejected.
7. **Manifest integrity.** Confirm all 32 original rows retain their intended
   semantics and that no row was weakened or removed.
8. **Preserved qualifications.** Confirm F01–F03, S01–S05, P-DEF-01, P-DEF-02 and
   ADR-CI1-01 are untouched and still open.
9. **No production change.** Confirm no `src/**`, test, schema, package or CI
   file changed.

## 2. How to reproduce

Follow [`reproduction.md`](reproduction.md). Then re-derive independently:

- Re-run the before and after harnesses and diff against the preserved JSON.
- Re-hash the checker sources and evidence against
  [`evidence/evidence-hashes.json`](evidence/evidence-hashes.json).
- Re-compute the source-input SHA-256 values in `after-run.json` from the
  canonical fixtures and compare to `git hash-object`.
- Pick at least one confirmed row and one external row and re-derive the bound
  symbol, containing assembly and eligibility by hand from Roslyn.

## 3. Adversarial checks Q3 should add

R2 expects Q3 to try to break the correction. Suggested independent attempts:

- **Context change test.** Move the C10 `System.Console.WriteLine` call into a
  compilation that *defines* its own `System.Console.WriteLine(string)` (the
  collision fixture via a real project reference) and confirm the class stays
  `external` unless the definition genuinely becomes the eligible local target.
- **Vocabulary test.** Replace a declared outcome with a near-miss such as
  `confirmed-bogus` and confirm it is rejected (the vocabulary is exact, not
  prefix-based).
- **Subtype test.** Declare a constructor occurrence as `deferred-method-group`
  and confirm `OUTCOME_SUBTYPE_MISMATCH`.
- **Candidate test.** Declare the ambiguous `Pick(null)` as a confirmed local and
  confirm `OUTCOME_CLASS_MISMATCH` with the two candidate documentation IDs.
- **Unaffected-mutation test.** Confirm a mutation that only changes a choice
  R2 does not claim to validate (for example, an owner field) does not produce a
  false rejection.

## 4. Required Q3 evidence

For each check: the command, the observed reason code, and a screen- or
file-level comparison against the R2 evidence. Explicitly record any mutation
that R2 accepts but Q3 believes should be rejected, and any rejection that fires
for the wrong reason.

## 5. Explicit non-claims (unchanged)

- No candidate `FindUsagesV1` exists; candidate-result comparison is unexecuted.
- No storage representation (C or B2) is selected.
- No production defect is fixed; P-DEF-01 and P-DEF-02 remain open.
- No CI-1A implementation, schema, codec, package or release is authorized.
- R2's oracle shares the Roslyn compiler family with the product; it is not a
  second independent compiler.
- R2 does not grant owner acceptance.
