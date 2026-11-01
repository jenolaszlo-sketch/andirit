# Q1B Finding Disposition — REM-01

**Baseline:** Q1B `REMEDIATION REQUIRED`; 6 blocking, 8 significant, 2 minor.
**Status:** Production/contract remediation complete for the foundation scope;
independent acceptance remains **REM-01-Q1**. No finding is claimed independently
accepted by this producer record.

| Q1B ID | Severity | Disposition | Evidence |
|---|---|---|---|
| F01 identity substitution | Blocking | **Corrected** | `identity-contract.md`; R2 frozen vector; `OccurrenceIdentityRemediationTests` |
| F02 external fact | Blocking | **Corrected in contract; production path refuses non-canonical targets** | `contract-reconciliation.md` §5; target constructor tests; Stage 2 matrix corrected |
| F03 coverage conflation | Blocking | **Corrected** | `coverage-and-availability.md`; `IncompleteAnalysis`; coverage result/persistence |
| F04 zero-fact | Blocking | **Corrected** | Native `CompleteEmptyAnalysisSurvivesReplacementAndReopen`; sink test |
| F05 unknown outcome | Blocking | **Corrected for ordinary path** | `publication-uncertainty.md`; ack-loss native test |
| F06 gates | Blocking | **Corrected** | `stage2-gate-reconciliation.md`; Stage 2 docs |
| F07 identity enforcement | Significant | **Corrected** | canonical IDs; recomputation; no caller payload hash |
| F08 coverage persistence | Significant | **Corrected** | unit/meta coverage fields; reopen test |
| F09 conditional inactive | Significant | **Contract fixed; extractor deferred** | coverage vocabulary; Stage 2 deliverable |
| F10 query contract | Significant | **Partially corrected** | coverage + `IncompleteAnalysis`; continuation deferred to S2.4 |
| F11 provenance | Significant | **Partially corrected** | identity accepts canonical compctx; full S2.1 chain deferred |
| F12 fixture | Significant | **Open qualification** | existing fixture below minima; not claimed qualifying |
| F13 evidence metadata | Significant | **Erratum issued** | `evidence-integrity-erratum.md` |
| F14 documentation | Minor | **Corrected in affected docs** | Stage 2 corrections |
| F15 historical/Linux timing | Significant | **Reconciled** | `stage2-gate-reconciliation.md` §7 |
| F16 matrix traceability/union oracle | Minor | **Corrected oracle; full matrix remains S2 prep** | union semantics; Stage 2 matrix |

---

## Blocking closure notes

### F01

The production helpers now implement the exact R2 `usage4`/`asmctx3` tuples with
big-endian lengths and canonical formats. The frozen R2 vector
`bd0487b7…` is reproduced. Different defining contexts yield distinct targets and
occurrences. Pre-REM identities are refused, not reinterpreted.

### F02

The Stage 2 matrix no longer requires an `External` fact. A target outside the
verified defining-compilation authority has no canonical `asmctx3`; the
production target constructor rejects `asmctx3:unavailable` and fabricated
values. Stage 2 extraction must record refusal/coverage, not a confirmed fact.

### F03/F08

Coverage carries analysis state, scope/universe identities, counters and refusal
reasons, and is persisted with the unit/meta. The reader returns
`IncompleteAnalysis` when completeness is unproven; it cannot silently convert an
incomplete zero result into `Available`.

### F04

A complete-empty unit is persisted with `Produced` coverage even when its fact
list and page set are empty. Reopen restores `Available` with zero facts.
Deleting the unit removes coverage and returns `AnalysisUnavailable`.

### F05

Post-commit/ack-loss is an unknown outcome; the store quarantines the instance
and refuses current-state claims until reopen. Known pre-commit failure remains
recoverable. No blind retry or false rollback is implemented.

### F06

The Stage 2 ledger now separates preauth contract, implementation slice and
acceptance evidence. S-01/IF-03 are contract prerequisites; their production
integration/tests are Stage 2 work. Fixture timing and historical/Linux timing are
explicit.

---

## Remaining qualifications

The following are intentionally **not** claimed as independently accepted:

- Full Stage 2 Roslyn extraction, `compctx1` construction, live universe,
  semantic binding and canonical `usage4` emission.
- Conditional-inactive scanning.
- Reader continuation/cross-target surface.
- A Stage 2-qualifying real-project fixture and independent oracle.
- Authentic historical fixtures for formats with no prior writer.
- Linux native qualification and crash/power-loss/multi-process behavior.
- Independent requalification of this remediation.

These are not used to defer an original blocking correctness defect that this
commission claims to have fixed; they are explicitly out-of-scope Stage 2/later
obligations.
