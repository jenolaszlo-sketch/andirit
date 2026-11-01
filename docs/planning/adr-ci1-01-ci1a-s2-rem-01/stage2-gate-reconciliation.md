# Stage 2 Gate Reconciliation — REM-B06

**Authority:** S1A, RECON, S2B, S2M, S2H; Q1B F06/F15/F16.
**Status:** Corrected authorization ledger. Stage 2 remains **unauthorized**.

---

## 1. Classification

| Class | Meaning |
|---|---|
| Preauthorization contract | Semantics/types/policy must be fixed before implementation authorization |
| Existing implementation prerequisite | Must already work before a dependent slice |
| Authorized Stage 2 deliverable | Work the Stage 2 commission may implement |
| Stage 2 acceptance evidence | Independent evidence required before Stage 2 acceptance |
| Later CI-1A obligation | Explicitly deferred to a named later gate |

---

## 2. Requirement ledger

| Requirement | Q1B disposition | Authority | Current state | Preauth contract | Implementation slice | Acceptance evidence | Blocking stage |
|---|---|---|---|---|---|---|---|
| S-01 source-byte provenance | F11; significant | OWNER §F; S2B §3.2 | Byte capture/verify exists; `compctx1`/scope/universe/live binding not implemented | Complete trust-chain contract | S2.1 | Independent positive/negative provenance tests | S2.1/S2.5; not preauth as completed code |
| IF-03 resolution/coverage/availability | F03/F08/F10 | RECON IF-03; owner D4/D5 | Coverage record, persistence, `IncompleteAnalysis` implemented in REM-01 | Fixed dimensions/state mapping (this commission) | Stage 2 production reader/coverage integration | Production query/coverage/reopen evidence | Contract satisfied; Stage 2 acceptance still required |
| SQ-02 unknown outcome | F05 | RECON SQ-02; D4 | Conservative quarantine/reopen policy implemented; ack-loss test | Policy recorded | S2.3/S2.4 reuse | Native fault/reopen evidence; crash/multi-process later | Contract satisfied; later crash qualification remains |
| SQ-03 concurrency | F05 (different scope) | RECON SQ-03; D4.10 | Serialized `_writeGate`; no MVCC | Serialized contract accepted | S2.3/S2.5 native schedule test | Native schedule test | S2 acceptance; no MVCC requirement |
| Real-project fixture | F12 | S2B §9; S2H §6 | Existing fixture below minima, oracle proposed | Timing decision | S2.0/S2.1 preparation or qualifying corpus | Frozen corpus + independent oracle | Before S2.2 qualification |
| Full internal reader | F10 | S2B §3.10; IF-04 | Coverage/result added; continuation/cross-target not implemented | Internal contract fixed | S2.4 | Bounded/continuation tests | S2.4 acceptance |
| Historical fixture (SQ-04) | F15 | RECON SQ-04; D5 | Not generated; authentic writer availability limited | Timing/availability decision | S2.2/S2.5 (available formats) | Immutable master/copy/open refusal | S2 acceptance for available formats |
| Cross-platform (SQ-05) | F15 | RECON SQ-05 | Windows only | Linux minimum fixed | S2.5 | Expanded native suite on Linux | S2 acceptance |
| D3 real-project | Q1B F16 | OWNER D3 | Stage 1 synthetic; REM-01 preserves D3 | No change | S2.3/S2.5 | Native graph noninterference with real facts | S2 acceptance |
| D4 applicable invariants | Q1B F16 | OWNER D4 | REM-01 preserves publication/serialization; zero-fact/coverage corrected | No change | S2.3/S2.4 | Native invariant evidence | S2 acceptance |
| D5 no migration/refusal | F13/F15 | OWNER D5 | REM-01 additive JSON only; old non-canonical IDs refused | No change | S2.2/S2.5 | Schema/refusal evidence | S2 acceptance |

---

## 3. S-01 distinction

**Before Stage 2 authorization:** the complete source/compilation trust-chain
contract must be fixed, including `compctx1`, `analysisscope1`,
`compilation-universe1`, defining-compilation correspondence, source/option/
reference identities and canonical minting. This is a contract, not an
implementation claim.

**During Stage 2:** implement actual capture-to-`compctx1`, live universe,
semantic binding and occurrence extraction.

**Before Stage 2 acceptance:** independent positive/adversarial provenance tests
must pass. The existing byte-materialization and SHA-256 verification are reusable
inputs, not proof of the whole chain.

---

## 4. IF-03 distinction

REM-01 fixes the contract and implements the minimal production coverage/zero-fact
persistence needed for Stage 1 foundation correctness. Stage 2 still owns:

- the semantic extractor's resolution counters;
- inactive configuration scanning;
- production `analysisscope1`/`compilation-universe1` population;
- the full internal reader continuation/coverage surface.

The fact that the type exists is not evidence that Stage 2 extraction is
implemented.

---

## 5. SQ-02 / SQ-03 statement

- SQ-02: known pre-commit failure is recoverable; a post-commit/ack-loss outcome
  is unknown and quarantines the instance until reopen. No blind retry. Crash,
  power-loss and multi-process recovery remain unqualified.
- SQ-03: reads and writes serialize behind `_writeGate`; this is not snapshot
  isolation or MVCC. A native schedule test remains an S2 acceptance obligation.

---

## 6. Fixture timing

The existing fixture at `docs/planning/ci1-scope-01/fixtures/` has 9 C# files and
a 32-row proposed oracle. It is below the Stage 2 minima and is not referenced by
Stage 2. The owner must either:

1. freeze a qualifying fixture and independent oracle before S2 authorization; or
2. authorize a bounded S2.0 preparation slice to create it, with S2.2 depending
   on the frozen oracle.

No qualifying fixture is claimed by REM-01.

---

## 7. Historical and Linux timing

- Linux: minimum native suite is a Stage 2 acceptance obligation; full
  cross-platform certification remains Stage 6.
- Historical fixtures: available authentic prior-writer formats are Stage 2
  acceptance evidence; formats with no authentic writer are recorded as an
  explicit limitation, not replaced by fresh-store tampering. Infeasibility
  returns to the owner for an amendment.

---

## 8. Matrix corrections

- S-01 tests are no longer assigned to "S2 spec review"; they are S2.1/S2.5
  evidence.
- IF-03 is a preauth contract, with implementation/acceptance split.
- The External row is refusal, not a fact.
- The distinct-site union oracle is corrected: different semantic units produce
  different `usage4` facts; only identical semantic facts union contributors.
- D4 has ten invariants; invariant 5 (cancelled staging) remains explicitly
  Stage 6, not silently counted as exercised.
- Duplicate section numbering and the stale "five prerequisites" count are
  corrected in the Stage 2 documents.

---

## 9. Authorization status

This ledger does **not** authorize Stage 2. It makes the prerequisites and
evidence obligations separable. Any implementation commission must still be
issued separately after the corrected boundary is independently reviewed.
