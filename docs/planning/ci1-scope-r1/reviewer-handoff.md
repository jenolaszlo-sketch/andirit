# Reviewer handoff (independent Q2)

This packet is ready for an independent Q2 review of the **planning
remediation**. It does not authorize CI-1 implementation, schema changes,
package publication or release.

## What Q2 must verify

1. **Findings fully reconstructed.** Every Q1 finding (F01–F04) and every
   reviewer conclusion recorded outside the numbered findings (S01–S05) is
   represented with severity, reproduction, expected/actual behaviour, root
   cause, predating status and required correction (`findings-matrix.md`).
2. **F01 remediation is sound.** Confirm the exact-identity gate replaces
   canonical-name coincidence, that the collision is refused/qualified, and that
   no legacy aggregate ID semantics change. Confirm P-DEF-01 is a genuine,
   bounded prerequisite rather than a hidden CI-1 implementation.
3. **F02 remediation is sound.** Confirm the trigger is correctly identified,
   the two-layer structure is respected (cast fix alone is insufficient), and
   the qualification/prerequisite decision is honest. Confirm P-DEF-02 is
   separately authorized.
4. **Evidence provenance (F03).** Confirm raw-byte and Git identity are
   distinguished and that no normalized fixture is claimed equivalent to its
   original source.
5. **Oracle strength (F04).** Confirm the checker rejects target/overload/
   coordinate/duplicate/outcome corruption and that lexical validation is
   distinguished from candidate-result comparison.
6. **ADR-CI1-01 is genuinely open.** Confirm no hidden preference for separate
   facts survives and that the minimum comparative experiment is sufficient to
   enable an owner decision.
7. **Bounded CI-1A preserved.** Confirm no scope expansion, that future-impact
   `changeKind`/path fields are removed from mandatory CI-1A metadata, and that
   the CI-1A objective is unchanged.
8. **Measurement claims corrected.** Confirm context separation and that no
   speedup/token/total-cost claim is made.
9. **No acceptance weakening.** Confirm the C01–C20 revisions only reflect
   independently established evidence and that the existing 32-row audit is
   preserved.

## How to reproduce

The Q1 reviewer workspace is preserved intact at
`C:/Users/Laszlos/source/repos/Solo/ci1-scope-q1` (do not modify it). R1's exact
commands and results are in `reproduction.md`. Q2 should:

1. Verify canonical Git state and tree (see `final-handoff.md`). The reviewed
   packet is published as `e7dc274b2819e2657c9312f711cbf2b47f94bd42` on
   `origin/main`; CI run `41048530036` for that SHA passed all jobs. Confirm any
   later commits are docs-only delivery-evidence updates and pin Q2 to the
   packet content at `e7dc274`.
2. Read `REPORT.md` and `REPRODUCE.md` in the Q1 workspace first.
3. Run the reviewer oracle and the wider probe, writing output **outside** the
   Q1 evidence directory, and compare against the Q1 evidence.
4. Re-derive F03 byte arithmetic and the F04 mutation results independently.
5. Inspect production source at the cited lines; do not modify it.

## Producer prior evidence vs R1 reproduced evidence

| Kind | Location | Status |
| --- | --- | --- |
| Q1 review report / reproduction | `ci1-scope-q1/REPORT.md`, `REPRODUCE.md` | Preserved, unmodified |
| Q1 reviewer oracle evidence | `ci1-scope-q1/evidence/independent-oracle.json` | Preserved; R1 reproduction is byte-identical (`5e0140b1…`) |
| Q1 reviewer byte/mutation evidence | `ci1-scope-q1/evidence/source-byte-check.json`, `author-check-mutations.json` | Preserved; R1 re-derived independently |
| Producer original packet | `docs/planning/ci1-scope-01/**` | Preserved; only marked `R1 amendment` blocks added |
| Producer baseline evidence | `ci1-scope-01/evidence/*.json`, `*.txt` | Preserved raw observations |
| R1 reproductions | `docs/planning/ci1-scope-r1/reproduction.md` | Distinct, independently executed by R1 |

Q2 must treat the producer's and reviewer's evidence as inputs, not as ground
truth, and must keep its own reproductions distinguishable.

## Explicit non-claims

- No candidate `FindUsagesV1` exists.
- No candidate storage representation is chosen.
- No performance, token or total-cost benefit is established.
- No CI-1 implementation, schema, codec, API or package change is authorized.
- No owner acceptance is issued.
- Two existing C# defects (F01/F02) require separate production remediation.

## Remaining questions for the owner

1. **Representation.** C or B2 after the minimum comparative experiment? (No
   default winner.)
2. **F01.** New-query strict resolver (P-DEF-01a) or bounded provider fix
   (P-DEF-01b)?
3. **F02.** Authorize P-DEF-02 or explicitly narrow qualification with honest
   unavailable/failure retention?
4. **Persistence/history.** Approve the version/codec route, named authentic
   historical fixtures, snapshot limits and old-reader refusal policy.
5. **Query limits.** Approve exact statuses/DTO/budgets, proof truncation,
   completeness universe and no-continuation V1.
6. **Utility threshold.** Approve the paired task protocol and the deliberately
   narrow real-repository qualification and its expansion conditions.
