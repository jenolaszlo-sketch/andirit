# Proposed successor commission — `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01`

**Proposed**: 2026-11-22, by `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` (disposition: **READY WITH
QUALIFICATIONS**).
**Status**: **proposed — owner-gated. Not authorized. Not started.**

---

## 1. Title and objective

**`ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01` — S6.1 native cancellation path: implementation and native
qualification.**

Implement and qualify the Stage 6 work package **S6.1**: publish the cancellation contract for the
production LatticeDB store, add the dedicated native cancel-path test suite, execute it against
production storage on the supported platforms, seal the evidence, and submit the result for
independent review and owner acceptance.

## 2. Preconditions (all satisfied at the time of proposal)

| Precondition | State |
| --- | --- |
| Stage 6 entry completed with a disposition | **READY WITH QUALIFICATIONS** (`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md`) |
| Baseline pinned | `b5a81f81fade9807f706b4586329efe416cf1558` (production `src/` unchanged since the qualified implementation `eafd632…`) |
| Published baseline preserved | `v0.2.0-preview.7` → `6ed05964d2fcc758648b654a01ec706647e52183`; `0.2.0-preview.7` published on nuget.org for all six IDs |
| Native dependency pinned | `LatticeDbSharp 0.2.0` (no upgrade authorized) |
| Feasibility established | active native execution is not interruptible; the cancellation surface is managed-only and bounded to the pre-native window (feasibility assessment §3–§4) |
| Qualification plan prepared | 12 deterministic cases with observation mechanisms, platform requirements, evidence formats, acceptance criteria and stop conditions |
| Implementation proposal prepared | documentation-only production change + one new test file (no dependency, workflow or API change) |

## 3. Authorized scope (proposed)

1. Apply the approved change set **C-1…C-4** of the implementation proposal:
   - the cancellation contract on `LatticeCodeGraphStore` and on `ICodeGraphStore`;
   - the dedicated native cancel-path test suite `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs`;
   - the qualification record and the owner-visible disposition.
2. Execute the qualification plan Q-01…Q-12 against the sealed commit:
   - `win-x64` execution (local or hosted);
   - **genuine hosted `linux-x64`** execution bound to the sealed commit;
   - the managed-suite regression row Q-10 on the existing three-OS CI matrix;
   - the native API/ABI re-census Q-12.
3. Seal the evidence with a self-excluding SHA-256 manifest and a production-delta receipt.
4. Produce the independent review and owner acceptance records; close S6.1 against M9.5, M12.2,
   D4 invariant 5 and the cancellation part of I-04/Q2-F09 — **and nothing else**.

## 4. Explicitly not authorized in this successor commission

- Any dependency upgrade (including `LatticeDbSharp`), any new native capability, any attempt to
  interrupt active native execution, or any claim that a managed timeout is a native cancellation.
- Any production code change beyond the approved documentation paragraphs; any change to the write
  gate, the commit boundary, atomicity, validation, refusal vocabulary, identity, merge or
  accounting semantics; any change to the storage format or protocol identities.
- S6.2 (durability), S6.3 (full native matrices), S6.4 (real-fixture D3), S6.5 (platform
  certification) or DF-7 (optional cross-target scope).
- Publication, version increment, tag change, force-push, history rewrite, deletion of qualification
  branches, or deletion/unlisting/deprecation of historical Hetu packages.
- Any waiver of Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8 or the osx-arm64 runtime limitation.

## 5. Gates

| Gate | Requirement |
| --- | --- |
| G-1 Implementation | C-1…C-4 applied; `src/` differs from the baseline only in documentation; no dependency, workflow, test-expectation or threshold change; the whole solution builds, formats and tests clean |
| G-2 Qualification | Q-01…Q-12 executed; the genuine Linux row is executed on a genuine hosted Linux runner bound to the sealed commit; Q-12 unchanged; no stop condition triggered |
| G-3 Regression | the historical suites, coverage thresholds (55 / 75 / 60 / 70) and CI gates are unchanged and green on all three platforms |
| G-4 Evidence | the evidence package is sealed with a self-excluding SHA-256 manifest; the production-delta receipt shows only the approved changes |
| G-5 Independence | reviewer session ≠ producer session (or an independent review against the sealed evidence); the reviewer re-derives the expectations from the contract excerpts |
| G-6 Owner acceptance | an owner acceptance record names exactly what S6.1 discharges (M9.5, M12.2, D4 invariant 5 native case, I-04/Q2-F09 cancellation aspect) and what remains deferred (M12.1, M12.3, the complete S-02/D4.5 matrices, DF-4, DF-6) |

## 6. Expected disposition options

- **S6.1 — QUALIFIED** (all gates pass, with the Q-S61-1…Q-S61-8 qualifications carried); or
- **S6.1 — QUALIFIED WITH QUALIFICATIONS** (a case is QUAL with a recorded reason, e.g. the macOS
  native row remains unavailable); or
- **S6.1 — NOT QUALIFIED** (a stop condition was triggered; the evidence is preserved and the
  owner decides the next step).

No disposition may claim native interruption, S-02 completeness, crash/power-loss durability or
platform certification beyond `win-x64` and genuine hosted `linux-x64`.

## 7. Dependencies and sequencing

S6.1 remains the first Stage 6 package: S6.2 (durability) and S6.3 (the complete native matrices)
consume its cancel-path receipts. If S6.1 is not qualified, the downstream packages are not started.

## 8. Evidence lineage

The successor commission must preserve and reference: the Stage 6 entry record and its sealed
evidence package (this commission), the nine prior sealed evidence packages, the published
`0.2.0-preview.7` baseline and its tag, and the accepted Stage-2 decision records.
