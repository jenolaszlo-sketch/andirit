# Proposed successor commission — `ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01`

**Proposed**: 2026-11-22, by `ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01` (disposition: **READY WITH
QUALIFICATIONS**).
**Status**: **proposed — owner-gated. Not authorized. Not started.**

---

## 1. Title and objective

**`ANDIRIT-CI1A-S2.6-WP-S62-IMPL-01` — S6.2 native durability: implementation and native
qualification.**

Implement and qualify the Stage 6 work package **S6.2**: publish the durability contract for the
production LatticeDB store, add the dedicated native durability test suite, execute it against
production storage on the supported platforms, seal the evidence, and submit the result for
independent review and owner acceptance.

---

## 2. Preconditions (all satisfied at the time of proposal)

| Precondition | State |
| --- | --- |
| Stage 6 entry completed with a disposition | **READY WITH QUALIFICATIONS** (`ANDIRIT-CI1A-S2.6-WP-S62-ENTRY-01.md`) |
| Baseline pinned | `92bf73a61794fa3bccad53d23dd7e65c51807cd5` (S6.1 acceptance revision; production `src/` unchanged since the qualified implementation `eafd632…`) |
| S6.1 qualification | `ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01` — **QUALIFIED WITH QUALIFICATIONS** |
| S6.1 owner acceptance | `ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01` — **ACCEPTED WITH QUALIFICATIONS** |
| Published baseline preserved | `v0.2.0-preview.7` → `6ed05964d2fcc758648b654a01ec706647e52183`; `0.2.0-preview.7` published on nuget.org for all six IDs |
| Native dependency pinned | `LatticeDbSharp 0.2.0` (no upgrade authorized) |
| Feasibility established | crash/power-loss/abrupt-kill/multi-process durability is not yet qualified; the durability surface is managed-only and bounded to the commit point (feasibility assessment §3–§4) |
| Qualification plan prepared | 10 deterministic cases with observation mechanisms, platform requirements, evidence formats, acceptance criteria and stop conditions |
| Implementation proposal prepared | documentation-only production change + one new test file + new infrastructure (no dependency, workflow or API change) |

---

## 3. Authorized scope (proposed)

1. **Apply the approved change set C-1…C-4** of the implementation proposal:
   - the durability contract on `LatticeCodeGraphStore` and on `ICodeGraphStore`;
   - the dedicated native durability test suite `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs`;
   - the new infrastructure for genuine crash/abrupt-kill/multi-process cases (worker process harness, multi-process harness, safety scoping);
   - the qualification record and the owner-visible disposition.
2. **Execute the qualification plan D-01…D-10** against the sealed commit:
   - `win-x64` execution (local);
   - **genuine hosted `linux-x64`** execution bound to the sealed commit;
   - the managed-suite regression row D-10 on the existing three-OS CI matrix;
   - the native API/ABI re-census.
3. **Seal the evidence** with a self-excluding SHA-256 manifest and a production-delta receipt.
4. **Produce the independent review and owner acceptance records**; close S6.2 against DF-4, Q2-F12,
   D4 invariant 4, and the crash-recovery aspect of S-02 — **and nothing else**.

---

## 4. Explicitly not authorized in this successor commission

- Any dependency upgrade (including `LatticeDbSharp`), any new native capability, any claim of
  crash-recovery simulation, or any claim that a managed fault is crash durability.
- Any production code change beyond the approved documentation paragraphs; any change to the write
  gate, the commit boundary, atomicity, validation, refusal vocabulary, identity, merge or
  accounting semantics; any change to the storage format or protocol identities.
- S6.3 (full native matrices), S6.4 (real-fixture D3), S6.5 (platform certification) or DF-7
  (optional cross-target scope).
- Publication, version increment, tag change, force-push, history rewrite, deletion of qualification
  branches, or deletion/unlisting/deprecation of historical Hetu packages.
- Any waiver of Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8, Q-S61-1…Q-S61-8, or the osx-arm64
  runtime limitation.

---

## 5. Gates

| Gate | Requirement |
| --- | --- |
| **G-1 Implementation** | C-1…C-4 applied; `src/` differs from the baseline only in documentation; no dependency, workflow, test-expectation or threshold change; the whole solution builds, formats and tests clean |
| **G-2 Qualification** | D-01…D-10 executed; the genuine Linux row is executed on a genuine hosted Linux runner bound to the sealed commit; the native API/ABI census unchanged; no stop condition triggered |
| **G-3 Regression** | the historical suites, coverage thresholds (55 / 75 / 60 / 70) and CI gates are unchanged and green on all three platforms; S6.1 qualifications intact |
| **G-4 Evidence** | the evidence package is sealed with a self-excluding SHA-256 manifest; the production-delta receipt shows only the approved changes |
| **G-5 Independence** | reviewer session ≠ producer session (or an independent review against the sealed evidence); the reviewer re-derives the expectations from the contract excerpts |
| **G-6 Owner acceptance** | an owner acceptance record names exactly what S6.2 discharges (DF-4, Q2-F12, D4 invariant 4, the crash-recovery aspect of S-02) and what remains deferred (M12.1, M12.3, the complete S-02/D4.5 matrices, DF-6, Q-S62-5) |

---

## 6. Expected disposition options

- **S6.2 — QUALIFIED** (all gates pass, with the Q-S62-1…Q-S62-9 qualifications carried); or
- **S6.2 — QUALIFIED WITH QUALIFICATIONS** (a case is QUAL with a recorded reason, e.g. the macOS
  native row remains unavailable, or genuine power-loss testing infrastructure is infeasible); or
- **S6.2 — NOT QUALIFIED** (a stop condition was triggered; the evidence is preserved and the
  owner decides the next step).

No disposition may claim native crash-recovery capability, S-02 completeness, or platform
certification beyond `win-x64` and genuine hosted `linux-x64`.

---

## 7. Dependencies and sequencing

S6.2 consumes S6.1's cancel-path receipts (the Q-01…Q-12 qualification evidence and the published
cancellation contract). S6.3 (full native matrices) consumes S6.1 and S6.2. If S6.2 is not
qualified, the downstream packages are not started.

The S6.1 follow-up obligations are tracked but do not block S6.2:

| S6.1 finding | Disposition | S6.2 action |
| --- | --- | --- |
| F-1: Third managed checkpoint undocumented | Bounded erratum to C-1/C-2 | Tracked; no S6.2 block |
| F-2: Q-06 2nd scenario missing from suite | Required suite addition | Tracked; no S6.2 block |
| F-3…F-8 | Recorded, non-blocking | No S6.2 action required |

---

## 8. Evidence lineage

The successor commission must preserve and reference: the Stage 6 S6.2 entry record and its sealed
evidence package (this commission), the S6.1 acceptance record and its sealed evidence package,
the S6.1 qualification record and its sealed evidence package, the S6.1 implementation record and
its sealed evidence package, the Stage 6 entry record and its sealed evidence package, the nine
prior sealed evidence packages, the published `0.2.0-preview.7` baseline and its tag, and the
accepted Stage-2 decision records.

---

## 8. Next commission after S6.2

If S6.2 is qualified, the next commission is **`ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01`** — Stage 6
work package S6.3 entry assessment for the full native matrices (M12.1, M12.3, complete S-02/D4.5
matrices, the remaining D4 invariants 1–3, 6–10).