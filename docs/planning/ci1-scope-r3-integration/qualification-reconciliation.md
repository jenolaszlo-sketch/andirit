# Qualification reconciliation — Q4 findings and preserved limitations

This document records how the independent CI-1-SCOPE-Q4 findings are carried
into the owner acceptance and the integrated oracle. It references the
independent evidence by identity; it does not rewrite or duplicate it.

- Q4 workspace: `C:\Users\Laszlos\source\repos\Solo\ci1-scope-q4`
  (`REPORT.md`, `FINDINGS-MATRIX.md`, `QUALIFICATIONS.md`,
  `evidence/artifact-hashes.json` — 39 entries).
- Q4 reproduced oracle evidence: `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f`.
- Q4 independent probe evidence: `ab48e8325c3b6076cc27af10bf659d4babef6e088f6436fb8fa361d37c15d36c`.

## Finding dispositions

| ID | Q4 classification | Owner disposition | Where it is enforced/bounded |
| --- | --- | --- | --- |
| **Q4-F01** | Same assembly identity across distinct compilations (latent identity-model boundary) | **Accepted bounded limitation; future graduation condition** | The frozen audited-expectation resolver requires the expected declaration to resolve uniquely *inside* the expected compilation, so unsupported cross-compilation identity cases are refused (`TARGET_IDENTITY_UNVERIFIED` / `TARGET_DEFINING_PROJECT_MISMATCH`), never guessed. Acceptance does not authorize confirmation in unproven contexts. Expanding support requires additional compilation/definition provenance evidence. |
| **Q4-F02** | Unreadable fixture throws `FileNotFoundException` at the library boundary | **Documentation correction; deferred robustness improvement** | The qualified CLI does not expose the path (it audits only the frozen manifest + built-in mutations). The integrated documentation does not claim that every malformed/unavailable input produces a structured diagnostic; the "never throw" statement is scoped to the manifest structure. No implementation change was made for this nonblocking qualification. |
| **Q4-F03** | Two `sha256: null` entries in R3's frozen-export ledger | **Recorded provenance limitation** | The original evidence is preserved unchanged; the nulls were not rewritten. The export itself was independently verified 675/675 against Q4's own Git archive, and that distinction is recorded in the owner decision and `test-results.md`. |
| **Q4-F04** | Cross-project generic/reduced-extension expectations outside the frozen target table | **Bounded unsupported case** | Such rows are explicitly refused (`TARGET_IDENTITY_UNVERIFIED`), not fabricated. The integration does not expand the oracle's target vocabulary; future support requires explicit acceptance cases and independent evidence. |
| **Q4-F05** | Provenance recorded, not pinned | **Preserved provenance limitation** | The oracle records fixture SHA-256/byte counts and the trusted-platform-assembly reference count, but does not pin every dependency input. No universal reproducibility across arbitrary SDK/runtime environments is claimed. Stronger enforcement may be required before broadening beyond the frozen corpus. |

## Additional preserved limitations

1. **Lifecycle.** Exactly five rows carry `LIFECYCLE_NOT_QUALIFIED` (C14×2,
   C15×3). The R3 gate requires exactly five such qualifications, so the
   limitation cannot silently become success. Static binding does not establish
   publication replacement, stale-fact retirement or lifecycle correctness.
2. **Basis statements.** Some are human-reviewed; only the coordinate claim is
   mechanically corroborated. They are recorded as
   `BASIS_NOT_MECHANICALLY_QUALIFIED`.
3. **Compiler independence.** The oracle uses Roslyn 5.9.0. It is independent
   of the Andirit production implementation but is not a second compiler.
4. **Scope.** The oracle validates the frozen 32-row CI-1 acceptance corpus,
   not every language construct or project topology.
5. **Unsupported capabilities.** Runtime dispatch expansion, constructor
   discovery, method-group targets, local-function targets and indirect impact
   remain outside the qualified scope.
6. **Candidate-result comparison.** No CI-1A implementation exists; comparison
   with production `FindUsagesV1` results remains unqualified.
7. **Platform.** The integrated oracle was executed on Windows for this
   milestone; it is not part of the CI workflow, so no multi-platform execution
   of the planning oracle is claimed.

## What the qualification does establish

For the frozen corpus and the supported compilation model: typed outcome
assertions are derived from Roslyn evidence; exact target-definition
correspondence is resolved from the expected declaring compilation and compared
to the bound definition; owners, repository paths and corpus identity are
derived/validated, not table-copied; conditional activity is label-independent;
malformed manifest structure is controlled; and the full 198-mutation suite
(196 intended rejections, 2 positive controls, 0 crashes) reproduces
byte-identically.

No Q4 finding was reclassified as resolved, and no qualification was converted
into an unconditional guarantee.
