# ADR-CI1-01-CI1A-S2.3-ENTRY-OWNER-01 — S2.3 entry acceptance and bounded first-package authorization

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01` (stage-entry assessment and owner authorization).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Decision:** **`ENTRY ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate:** **`S2.3 ENTRY PASSED`**.
**First authorized implementation package:** **`WP-S23-1` — `NEW-01` targeted recovery remediation**.
**Scope:** Native publication entry, recovery prerequisite, and Option A durability boundary — assessment and bounded authorization only.

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The S2.3 stage is accepted for entry with the explicit qualifications below. Only
> the `NEW-01` targeted recovery remediation package is authorized to begin. Native
> publication, the defining-`compctx2` durability carrier, reopen enforcement, and
> lifecycle integration remain **NOT authorized** until their predecessor gates pass.
>
> This decision implements nothing, changes no schema, and begins no native
> publication. It does not authorize S2.4/S2.5, Stage 6, public Find Usages, package
> publication, or any push.

This decision is additive documentation. It changes no production code, test,
schema, frozen expectation, prior owner decision, or accepted planning contract.

---

## 1. Verified repository baseline

| Object | Value |
| --- | --- |
| HEAD | `9b420e8960df5bfa0940444309d6829a1acfac7a` |
| Tree | `5a1f2998f3d89f2907ee0af8620a02b3e8881bf7` |
| Working tree | clean; one worktree (`D:/projects/Andirit`); no stashes |
| `origin/main` | `443c192733a53d2f1da415743debb84e8c81acb5` (ancestor) |
| Ahead / behind | 17 / 0 |
| Push status | nothing pushed |
| Environment | Windows x64; SDK `10.0.401`; runtime `10.0.12` |

Verified: the S2.2 `G-ACCEPT` decision (`9b420e8`, tree `5a1f2998…`) is HEAD; the
accepted S2.2 acceptance history is intact; no concurrent writer was observed.
Supporting authority read directly: Candidate C owner decision (`6d7a0b1`) §§D/F;
Option A (`b7fea0d`); S2.2 `G-ACCEPT` (`9b420e8`); S2.1 acceptance (`96b9db8`) and
boundary (`febc28d`); obligation register (`a1cf470`) §§3–8; REM-01 reconciliation
(`c13137d`); Stage 1 acceptance (`4c6d26a` for the acceptance record; reviewed
implementation `76ebaae`); Stage 2 boundary/handoff/acceptance-matrix (`acd7ecd`).

**No equivalent S2.3 entry decision exists** (`docs/decisions/` contains no
`s2-3-entry` or `recovery` decision record).

---

## 2. Authoritative S2.3 contract (reconstructed)

Full inventory: [`docs/planning/adr-ci1-01-ci1a-s2-3/`](../planning/adr-ci1-01-ci1a-s2-3/README.md).

S2.3 is defined by the Stage-2 boundary §8 as *production publication integration*:
publish extracted occurrences through the existing source-unit lifecycle, preserve
native transactional consistency (same transaction as graph), and verify
replacement/deletion after real source changes and contributor behavior — gated by
native persistence/reopen/exact-identity verification with **real Roslyn facts**.

**Precedence applied.** Older stage documents place publication inside S2.2; the
later accepted boundary assigns native source-owned publication to **S2.3** (S2.1
boundary §4; S2.2 entry acceptance §3.5 option (a); S2.2 `G-ACCEPT` §17). This
decision follows the later accepted boundary; historical drafts are not
authorization.

Binding invariants: D3 legacy-graph noninterference; D4 publication invariants
1–10; D5 no-migration (additive JSON only; explicit unsupported-schema refusal);
serialized write/concurrency model (no MVCC); SQ-02 unknown-outcome
quarantine/reopen (no blind retry, no crash/power-loss claim); internal-only reader;
no public Find Usages.

---

## 3. Entry conditions and dispositions

| # | Condition | Disposition |
| --- | --- | --- |
| E1 | S2.2 accepted with the capabilities S2.3 consumes | **Satisfied** (`9b420e8`) |
| E2 | S2.3 scope reconstructed and bounded | **Satisfied** (scope inventory) |
| E3 | `NEW-01` recovery prerequisite defined | **Satisfied** (document 2) |
| E4 | First durability gate (test 8b) contract defined | **Satisfied** (document 3) |
| E5 | Stage 1 native reuse assessment | **Satisfied** (document 4) |
| E6 | Native lifecycle test matrix | **Satisfied** (document 5) |
| E7 | S-01 canonicalization disposition | **Satisfied** (document 6) |
| E8 | Work-package and qualification plan | **Satisfied** (documents 7/8) |
| E9 | `NEW-01` remediation complete | **Open — first authorized package** |
| E10 | Defining-`compctx2` durability carrier exists | **Open — gated behind E9** |
| E11 | Real-fact publication integration | **Open — gated behind E10** |

No entry condition is a *documentation* blocker; E9–E11 are the bounded
implementation gates.

---

## 4. `NEW-01` recovery prerequisite (binding ordering)

The verified defect (document 2): `DurableCommandLog.ReplayAsync` restores graph
units (`InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync`) but **not** the
occurrence projection/availability/coverage/publication binding. The
`LatticeCodeGraphStore.MutateAsync` **recovery** branches (pre-apply validation
failure; known pre-commit persist failure) rebuild `_inner` via `ReplayAsync` +
`RestoreBaselines` and **omit** `RestoreOccurrences`, so recovered in-memory state
disagrees with durable authority. (Initial open is unaffected — it calls
`RestoreOccurrences` after replay.)

**Ordering (binding):** `NEW-01 remediation → NEW-01-Q1 → NEW-01-OWNER` must
complete **before** any S2.3 native publication, durability carrier, or recovery
reliance (obligation register `NEW-01`; S2.1 boundary §4; S2.2 entry §3.7).

**Prohibited until `NEW-01` passes its gate:** S2.3 native publication of real
facts; any assertion that occurrence publication survives recovery; any
recovery-path production reliance. **`NEW-01` is not closed by S2.2 non-reliance.**
The unknown-outcome/quarantine policy is not part of `NEW-01` and is not changed.
`NEW-01` must not expand into general crash/power-loss/multi-process recovery.

---

## 5. First durability gate (document 3)

The first S2.3 durability gate is **Option A §5.2 test 8b — native reopen
stale-refusal**: persist the defining `compctx2` association and refuse stale facts
after native reopen when the defining context has changed (including transitive-
only), while a stable `asmctx3` does not bypass the check.

**Identified carrier gap (blocking for the durability package, not for entry):**
the current native schema carries **no** defining-`compctx2` association on
`CodeOccurrenceFact`, `CodeOccurrenceCoverage`, `CodeGraphBatch`, or
`CodeIndexUnitReplacement`; `RecordedCompctx2` is a mint-time validation input that
is **not persisted**. A stored structural `asmctx3` is **not** sufficient (Option A
§5.1(2): "a bare `asmctx3` is not provenance"). S2.3 must add an **additive**
persisted carrier and enforce it on reopen.

**Escalation preserved:** if the association cannot be safely persisted and
enforced after reopen, stop the affected path and return for the bounded **Option
B** alternative; Option A is not silently weakened.

---

## 6. Native reuse (document 4)

**Reuse unchanged:** native occurrence labels/schema; atomic graph+occurrence
publication; source-unit ownership; replacement/deletion/contributor
union/conflict refusal; bounded deterministic query; publication-consistent
availability; durable reopen/projection reconstruction; schema/namespace refusal
(D5); complete-empty/partial/unavailable persistence; unknown-outcome
quarantine/reopen; serialized model; D3 noninterference (synthetic).

**Requires integration:** real-fact publication wiring; defining-`compctx2`
carrier; real-fact N1–N8/lifecycle; real-source replacement/deletion/reindex;
real-project D3; native schedule test; `NEW-01` recovery projection.

**Not reused / not changed:** storage architecture (D4); migration/dual-write
(D5); background/repair frameworks (D4); MVCC (SQ-03); historical aggregate
reinterpretation (D5).

---

## 7. S-01 canonicalization disposition (document 6)

The S2.2 `G-ACCEPT` retained qualification stands: general canonical derivation of
`identifierDigest`/`declarationDigest`/`shapeDigest` is **not implemented or
independently qualified**; the components remain caller-supplied identity inputs.

- This discrepancy is **not** declared resolved by S2.2 acceptance.
- Native publication consumes the digests as identity inputs; it does **not**
  independently verify their source canonicalization; S2.3 need not, because the
  accepted contract treats them as caller-supplied.
- **It does not block S2.3 publication authorization.**
- The obligation is recorded as an **S2.5 integrated-proof completion gate**
  (before Stage-2 owner acceptance). A stronger guarantee would require a separate
  owner decision; none is authorized here. The recorded S2.2 decision is neither
  reopened nor rewritten.

---

## 8. Work-package plan (document 7)

| WP | Work | Status after this decision |
| --- | --- | --- |
| **WP-S23-1** | `NEW-01` targeted recovery remediation | **AUTHORIZED (first and only immediate package)** |
| WP-S23-2 | Defining-`compctx2` native carrier + reopen enforcement (test 8b) | **NOT authorized** (gated behind WP-S23-1) |
| WP-S23-3 | Real-fact publication integration | **NOT authorized** (gated behind WP-S23-2) |
| WP-S23-4 | Native lifecycle qualification with real facts | **NOT authorized** (gated behind WP-S23-3) |
| WP-S23-5 | S2.3 owner acceptance | **NOT authorized** |

Each package requires its own commission, independent qualification, and owner
decision; **no package authorizes its successor.**

**Excluded from all packages:** schema **format** migration; dual-write; legacy
readers/adapters; public Find Usages; background/distributed indexing; automatic
repair framework; incremental page maintenance/native-node-ID optimization;
MVCC/snapshot isolation; crash/power-loss/multi-process recovery; reader
continuation (S2.4); integrated Stage-2 proof (S2.5); package publication.

---

## 9. Evidence and platform plan (document 8)

Real inputs and hashes; actual Roslyn compilations; metadata/project references;
recorded identities/provenance; native state before/after publication; reopen;
replacement/deletion effects; transaction/recovery results; refusal/coverage
classifications; raw logs/TRX/environment; reviewer probes; reproducible manifests.
Frozen R2 evidence preserved unchanged; oracle replay must remain `185/185` with
output `97F6C5E5…`. Primary qualification is **Windows x64**; the expanded Linux
native suite is an S2.5/Stage-2 acceptance obligation, not full certification.
Earlier hosted CI does not constitute fresh S2.3 platform qualification.

---

## 10. Explicit qualifications and non-authorization

**Qualifications accepted with entry:**

- `NEW-01` is an unclosed prerequisite; only its remediation is authorized.
- The defining-`compctx2` carrier does not exist; native reopen enforcement is not
  yet demonstrated.
- The S2.2 extractor is not wired to the publication path.
- N1–N8 are synthetic and must be adapted to real facts.
- S-01 component digests remain caller-supplied (S2.5 closure).
- The native schedule test (SQ-03) and available-format historical fixtures
  (SQ-04) remain outstanding.

**Not authorized:** S2.3 native publication; the defining-`compctx2` carrier;
reopen enforcement; real-fact lifecycle integration; any schema **format** change;
S2.4/S2.5; Stage 6; public Find Usages; package publication; any push.

**Non-claims:** no native publication exists; no reopen-time validity is
demonstrated; no recovery correctness is claimed; no D3/D4/D5 weakening; no MVCC;
no crash/power-loss/multi-process guarantee; no universal or cross-platform
guarantee.

---

## 11. Final owner disposition

**Decision: `ENTRY ACCEPTED WITH EXPLICIT QUALIFICATIONS`. Gate: `S2.3 ENTRY PASSED`.**
**First authorized implementation package: `WP-S23-1` — `NEW-01` targeted recovery remediation.**

The S2.3 stage is accepted for entry on the evidence-supported scope and boundaries
above. A separate commission may now begin **only** `WP-S23-1`. The defining-
`compctx2` durability gate, publication integration, lifecycle qualification, and
S2.3 owner acceptance each remain behind their predecessor gates. The Option B
escalation condition is preserved.

### Next authorized action

A **bounded `WP-S23-1` (`NEW-01`) implementation commission** (implementation →
`NEW-01-Q1` independent qualification → `NEW-01-OWNER` decision), per
[`new-01-recovery-remediation.md`](../planning/adr-ci1-01-ci1a-s2-3/new-01-recovery-remediation.md).

---

**Final disposition: `ADR-CI1-01-CI1A-S2.3-ENTRY-OWNER-01 ENTRY ACCEPTED WITH
EXPLICIT QUALIFICATIONS` — `S2.3 ENTRY PASSED`; only `NEW-01` remediation
authorized.**

Recorded at HEAD `9b420e8960df5bfa0940444309d6829a1acfac7a` (tree
`5a1f2998f3d89f2907ee0af8620a02b3e8881bf7`). This record and its evidence are the
only changes in their commit(s); the decision and evidence commit/tree identities
are recorded in the handoff.
