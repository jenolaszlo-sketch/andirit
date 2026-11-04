# G-CONFIRM — adversarial findings and reproductions

**Commission:** `ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1`. **Candidate:** `e0d9110`
(tree `ba9338d`). **Date:** 2026-11-04.

The task's adversarial list, challenged systematically. Each item records the
challenge, the independent observation, and the outcome. Corrections to
authoritative rows are recorded here rather than editing the authoritative
evidence.

---

## A1 — Wrong defining context

**Challenge:** does `asmctx3` use the *defining* compilation's `compctx1`, or the
consuming caller's?
**Observation:** the tuple (`asmctx3-context` over `"compctx1"`, defining
`compctx1`, `metadataIdentity`, `moduleName`, identifier/declaration/shape
digests) and the corpus (`RunIdentities`) build each target from its defining
unit (`Core`, `Leaf`); the cross-view section derives the same targets for a
caller in `App`. Option A A03/A05 show a defining-API change altering the target
while the caller source is unchanged, and identical declared forms in different
defining compilations yielding distinct `asmctx3`. **Outcome: CONFIRMED — the
target is the defining compilation.**

## A2 — Consuming vs defining context confusion

**Challenge:** is the consuming context substituted for the defining context?
**Observation:** `usage4` hashes the caller's semantic unit for ownership, while
`asmctx3` hashes the defining `compctx1`; `compctx2` of the **defining** unit
governs validity. The E02 section resolves a project symbol in Roslyn but refuses
publication when the defining `compctx2`/universe membership is absent, and the
positive control permits it when the verified defining context is added. **Outcome:
CONFIRMED — consuming and defining contexts are distinct and not substituted.**

## A3 — Incorrect or missing provenance

**Challenge:** is provenance (provenance `compctx2`, universe membership) actually
required, or does `asmctx3` equality suffice?
**Observation:** `ProvenanceGate` requires recorded `compctx2` == independently
derived current `compctx2` **and** matching universe membership. A06 (missing
current `compctx2`), A07 (missing universe membership), A08 (stale) all refuse;
A01-gate permits. **Outcome: CONFIRMED at the contract/predicate level.**
**Qualification:** this is a *modeled* predicate; no production association
enforcement exists. Must not be read as implemented (row 13/WP8).

## A4 — Incorrect or missing live/frozen reference-profile selection

**Challenge:** are production and frozen reference profiles correctly separated,
or is a fixture digest silently compared to a production capture?
**Observation:** the frozen anchors are required only under the complete fixture
profile; the actual-path capture values (`…b816dcb5…`, `…df8d2932…`) differ as
they must. Recovery of the anchors requires a disclosed comparison-only label
alignment. The oracle's `--expected` comparison covers `identities` (including
`compctx1`) and matched this run. **Outcome: CONFIRMED with the disclosed
test-only alignment.** **Correction C2** recorded for handoff §D row 5.

## A5 — Equivalent vs distinct semantic identities

**Challenge:** are equal complete inputs over-conflated (object identity) or
under-conflated (distinct targets merged)?
**Observation:** A01 re-derives identical `compctx1`/`compctx2` from a second
build; A05 yields distinct `asmctx3` for identical declared forms in different
defining compilations. The accepted wording (Q1-F04 correction) uses *distinct
accepted defining contexts/declarations*, not object identity. **Outcome:
CONFIRMED — equal complete inputs ⇒ equal identities; distinct preimages ⇒
distinct identities; object identity is not an identity field.**

## A6 — Dependency reconstruction errors

**Challenge:** is the legacy R2 project-reference item reconstructed correctly
(raw ordinal sort before normalization, no dedup, text/tree hash vs source-byte
hash, no transitive child context)?
**Observation:** the mapping contract §2 and the oracle's
`R2ProjectReferenceItem` implement raw-ordinal sort before `NormalizePath`, comma
join, no dedup; the item omits child options/preprocessor/transitive references.
Q2 §4 records a frozen-only raw-backslash adversary proving sort-before-
normalization and duplicate preservation, and notes that current
`CodeGraphSource` normalizes earlier (so the raw-path case is a frozen-rule test,
not a claim about current production). **Outcome: CONFIRMED with that disclosed
boundary.**

## A7 — Scope and verified-universe conflation

**Challenge:** is `analysisscope1` treated as a source inventory, or completeness
inferred from it?
**Observation:** `analysisscope1` binds only repository + unit/path; membership is
enforced by the universe and descriptors (`ScopeSourceMembershipMismatch`,
A-02). The oracle models completeness via `ProvenanceGate.Universe`. **Outcome:
CONFIRMED.** **Qualification:** production completeness is scope-relative and
must be proven from the universe, not the scope digest.

## A8 — Expected values derived from untrusted producer behavior

**Challenge:** is any expected value generated by the implementation under test?
**Observation:** the frozen anchors (compctx1/compctx2/asmctx3/usage4) are
literals transcribed from accepted contract documents; the reviewer independently
reconstructed the NEW-02 `usage4` anchors and the `compctx1` self-test anchor from
the tuple text alone. Rows 5 and 11 explicitly forbid self-originated
expectations (production mapper; defective `Merge`). **Outcome: CONFIRMED — no
expected value is producer-generated.**

## A9 — Hidden assumptions inherited from the original F16 matrix

**Challenge:** does the current row set still carry stale/overbroad wording from
the pre-correction matrix?
**Observation:** the current corrected rows (package §2 / mapping contract)
supersede two stale handoff §D rows. **Corrections recorded (not silently
edited):**

- **C1** — handoff §D row 2 retains the pre-remediation "Eager PE/assembly
  validation" wording; the bounded-surface wording in package §2 and Q2 §3 is
  authoritative.
- **C2** — handoff §D row 5 omits the declared comparison profile; package §2 and
  mapping contract §2 are authoritative.
- **C3** — handoff §D's final column header is "Authority" where the nine-part
  schema requires "closure authority"; package §2 uses "Closure". Non-semantic.
- **C4** — package §6 says evidence "rows 5–14 lands during WP3–WP9"; the precise
  reading is that authority/expected-value for all 14 rows is confirmable now and
  only rows 7–13 (plus the live-mapper/extraction parts of 4/5/6/13) retain
  implementation evidence. Rows 1–4, 6, 14 have authority/contract/review
  evidence.

**Outcome: CONFIRMED WITH QUALIFICATIONS** — the corrections are documentation
hygiene, not authority contradictions; the corrected rows are used.

## A10 — Circular confirmation using future implementation output

**Challenge:** does any row's expected value presuppose the implementation's own
output, or credit a planned/future test as passing?
**Observation:** no test is counted as passing except those actually executed
here (frozen oracle, identity tests, verifiers). Rows 7–13 record *planned*
implementation verification, explicitly not credited. The G-MINT row and the
NEW-04 row forbid implementation-originated oracles. **Outcome: CONFIRMED — no
circularity.**

---

## Signal from the frozen oracle's expected comparison

The committed `oracle-expected-r2.json` contains **176** checks; this run emits
**185**. The difference is exactly the **9** `expected.<section>` checks added by
`CompareToExpected` (`semantics`, `identities`, `variantIdentities`, `variants`,
`crossView`, `e02`, `coverage`, `counts`, `lifecycle`). All **9** sections match
the frozen document byte-for-byte in canonical JSON, and 185/185 pass
([oracle-section-diff.txt](evidence/oracle-section-diff.txt)). This is expected
and not a defect; it is recorded to avoid mis-citing a count mismatch.

## Adversarial conclusions

- No authority contradiction was found.
- No expected value was derived from untrusted producer behavior.
- No circular confirmation was used.
- Four documentation corrections (C1–C4) are recorded for the owner; they do not
  block dependent test authoring once the corrected rows are referenced.
- All bounded qualifications in the Q2/foundation acceptance remain in force and
  are carried into the rows.
