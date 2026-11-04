# G-CONFIRM — independent commission draft (prepared, not issued)

**Commission ID (proposed):** `ANDIRIT-CI1A-S2.2-G-CONFIRM-01`.
**Gate:** `G-CONFIRM`. **Status:** `DRAFT — NOT ISSUED`. **Gate remains OPEN.**
**Candidate:** `d5a785f898ce7877caef10be53074f2c2ae3a1cd`
(tree `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c`).
**Authority:** obligation register §3 F16; F12 acceptance §8; entry decision
§3.6; [G-CONFIRM package](../../planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md);
independent Q2 report §7.

This draft prepares a bounded independent `G-CONFIRM` commission. It issues no
verdict and does not claim the gate passed. No dependent qualification test may
be authored until a separate reviewer records a verdict.

---

## 1. Purpose and gate meaning

`G-CONFIRM` confirms the **authority, traceability and expected-value plan** of
the 14 nine-part F16/NEW-02 rows **before** dependent qualification tests are
authored. It does **not** require WP4–WP9 producer executions to exist; those
executions later verify implementation against already-confirmed expectations and
populate F16 evidence for `G-ACCEPT`.

Rows must never credit a future test as passing. Expected values must come from
frozen accepted authority — never from the production mapper/extractor under
test.

## 2. The 14 nine-part rows (candidate `d5a785f`)

Rows are the corrected rows of
[execution-readiness handoff §D](../../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md#d-f16-nine-part-traceability-rows-for-s2-2)
and [G-CONFIRM package §2](../../planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md#2-nine-part-rows);
each has all nine fields (requirement, authority, production surface, observable
behavior, positive test, adversary, environment, evidence artifact, closure
authority). The row numbers below match Q2 §7.

| Row | Requirement | Confirmation class |
| --- | --- | --- |
| 1 | Q-F2 supported-encoding set + ambiguous BOM-less refusal | **Authority/contract — confirm now** |
| 2 | Q-F3 bounded assembly/metadata capture boundary | **Authority/contract + bounded-surface — confirm now** (with exact Q2 qualification) |
| 3 | A-01 symbol canonicalization hardening | **Authority/contract — confirm now** |
| 4 | A-02 scope is not source inventory | **Foundation evidence — confirm now**; extraction consumption pending |
| 5 | R2-faithful `compctx1` derivation under a declared comparison profile | **Corrected profile — confirm now** (this was the Q1 blocker) |
| 6 | `compctx2` isolation from identity | **Frozen tuple authority — confirm now**; live mapper negative pending |
| 7 | Canonical target minting under live authority | Authority resolves now; **implementation evidence pending (WP5)** |
| 8 | Invocation extraction fidelity | Authority resolves now; **implementation evidence pending (WP4/WP9)** |
| 9 | Coverage/refusal classification | Authority resolves now; **implementation evidence pending (WP7/WP9)** |
| 10 | Unconditional-inactive scanner (F09) | Authority resolves now; **implementation evidence pending (WP7/WP9)** |
| 11 | Coverage-merge authority (NEW-04) | Authority resolves now; **correction + falsification pending (WP6)** |
| 12 | Live defining/source/target correspondence (S-01) | Authority resolves now; **per-fact chain pending (WP4/WP5/WP9)** |
| 13 | Option A durability association | **Contract/pre-mint vs S2.3 partition — confirm now**; enforcement pending |
| 14 | F16/NEW-02 confirmation itself | **Review procedure — confirm now** |

## 3. Authority confirmation required before dependent test authoring

The following can and must be independently confirmed **now**, from frozen
accepted authority and the Q1/R1/Q2 evidence — without using any production
implementation output as its own expected value:

- **Row 1 (Q-F2):** the strict recognized-encoding set and the bounded BOM-less
  NUL refusal against entry §3.1 and the actual `VerifiedCSharpSource.ReadAsync`
  surface; explicit input bytes/decoder contract, not extraction output.
- **Row 2 (Q-F3):** the **exact bounded** surface from Q2 §3, including the
  documented excluded surfaces and the explicit non-guarantees. The row text must
  not retain entry §3.2 “broad usable-metadata” wording.
- **Row 3 (A-01):** comma/pipe refusal, ordinal ordering and Unicode distinctness
  against fixed separator inputs and the descriptor digest.
- **Row 4 (A-02):** scope is a declared unit/path identity; membership derives
  from the verified universe; scope-digest equality cannot establish inventory.
- **Row 5 (mapped `compctx1`):** the **corrected declared comparison profiles** —
  production actual-path profile vs frozen fixture profile — with both
  independent expected-value checks (algorithm fidelity over equal inputs;
  separately, fixture-aligned Leaf/Core anchors), naming exact inventory/bytes/
  labels and disclosing the test-only label alignment. This is the row whose Q1
  inconsistency is corrected.
- **Row 6 (`compctx2` isolation):** frozen tuple authority and algebraic
  negatives; `compctx2` absent from `asmctx3`/`usage4`.
- **Row 13 (Option A association):** pre-mint in-memory invariants vs the
  separately authorized S2.3 reopen gate; the partition must be the accepted one.
- **Row 14 (procedure):** the review records that future tests are not credited
  and the corrected package (not the older terse rows) is referenced.
- **NEW-02 matrix:** the corrected `usage4`/tuple/component authority, already
  independently reproduced by Q1 and Q2.

## 4. Implementation evidence that may legitimately arrive during WP4–WP9

Not required for the `G-CONFIRM` verdict; required later for `G-ACCEPT`:

- Row 7 — real unique/missing/ambiguous/external/unbound live binding and
  per-declaration correspondence (WP5).
- Row 8 — production occurrence extraction token/declaration/source-hash evidence
  (WP4/WP9).
- Row 9 — production coverage emission and the complete-empty/refusal matrix
  (WP7/WP9).
- Row 10 — real disabled-text scanner evidence (WP7/WP9).
- Row 11 — NEW-04 correction and independent scope/universe/count falsification
  (WP6, before coverage reliance).
- Row 12 — exact live byte→binding→fact correspondence and integrated chain
  (WP4/WP5/WP9; integrated S2.5).
- Parts of rows 4, 6, 13 — extraction-time consumption and enforcement.

`G-CONFIRM` must not pull these forward as a precondition, and must not treat
their absence as a confirmation failure.

## 5. Circularity guard (binding on the reviewer)

- Expected values come only from frozen R2/Q3 authority, the accepted identity
  contract and the corpus — **never** from the mapper/extractor under test.
- An expected digest generated by the production mapper, or a comparison after an
  undocumented field rewrite, is circular and must be rejected.
- A green producer/algebra test is not authority confirmation.
- Shape validity is not live authority; canonical fabrications are not
  provenance.
- Passing observation tests are not automatically successful remediation tests;
  the three historical Q1 observation failures remain failures.

## 6. Reviewer deliverables

- Exact matrix document/version and candidate read.
- Frozen evidence hashes relied on (R2 manifest `BCE7DE2A…`, 100 entries; F12 Q3
  `9EFC…`, 338 entries; frozen oracle output `97f6c5e5…`; the Q1/R1/Q2 manifest
  hashes).
- Row-by-row confirmation or the named defect.
- Explicit list of rows whose implementation evidence is deferred, with the stage
  at which it lands.
- A verdict: `G-CONFIRM PASS`, `G-CONFIRM PASS WITH QUALIFICATIONS`, or
  `G-CONFIRM FAIL` — recorded by the reviewer, never by the producer.

## 7. Scope, exclusions and stop conditions

- Documentation/review only. **No** production change, **no** dependent test
  authoring as part of the confirmation itself, **no** minting, **no** push.
- Stop and return to the owner if a row’s authority does not resolve to an
  accepted decision, if the corrected row 5 profile cannot be confirmed without
  rewriting frozen authority, or if confirmation would require future
  implementation output.
- A `G-CONFIRM PASS` (or accepted-with-qualifications) verdict is a prerequisite
  for dependent qualification-test authoring, not for WP4–WP9 authorization,
  which remains a separate owner decision.

## 8. Current state

`G-CONFIRM` is **OPEN**. Q2 supplies updated readiness evidence and resolves the
row-5 inconsistency; no verdict has been issued. This draft does not issue one.
