# Accepted capability and qualification summary — Stage 2 (STAGE2-ACCEPTANCE-OWNER-01)

**Recorded:** 2026-11-22 with `adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`.
Summarizes the capabilities accepted at Stage-2 closure and the qualifications under which they
are accepted. Nothing here broadens a claim or discharges a Stage 6 obligation.

## 1. Accepted implementation baseline

| Item | Value |
| --- | --- |
| Qualified implementation | `eafd632b...` / tree `823e3c65…` (branch point after the R3/QUAL-04 review chain; the gate-remediation revision) |
| Production delta vs. the pre-remediation baseline | two comment-only relocations (exact-byte formatting) and 23 focused LatticeDb regression tests (F-G2-01 coverage) — no semantic, contract, dependency, formatter or CI-policy change |
| Corrected-revision CI | run 44746950426 — green full matrix (windows/ubuntu/macos test jobs + pack) on the unmodified workflow |
| Scope boundary | C# method-invocation occurrence extraction with canonical identity (`usage4`/`asmctx3`), verified-compilation binding, native LatticeDb persistence, authority-gated publication, bounded reader/continuation, coverage and conservative refusal |

## 2. Accepted capabilities (within the qualified boundary)

- **S-01 source-byte provenance and semantic identity**: descriptor hashes the actual indexed
  bytes; `compctx1` binds options/references; Roslyn binding uses the exact verified
  compilation; frozen R2 oracle vectors byte-identical (185/185).
- **Occurrence extraction and coverage**: supported invocation sites extract as facts;
  unsupported categories counted (never facts); exact invocation spans; conservative refusal
  states (External, Ambiguous, Unsupported, ConditionalInactive, Incomplete, Unresolved)
  including the amended M5.4 basis.
- **Authority-gated minting and publication**: `asmctx3` minting requires a non-null recorded
  defining context; every non-Complete resolution refuses; the unresolved classification never
  reaches publication without a verified authority envelope (M54-B-QUAL-01).
- **Native persistence and reopen**: atomic publication; durable reopen reconstructs; schema /
  namespace / page-binding / stale-publication refusals; authority-masking refusals.
- **Reader surface**: bounded target-index queries with continuation, `Examined` accounting and
  truthful availability mapping (S2.4).
- **Cross-platform**: genuine hosted Ubuntu x64 execution of the 75-case native suite with the
  demonstrated-environment scope (M14.2).
- **Historical compatibility**: the two authenticated prior-writer fixture sets (pre-REM
  occurrence store + WAL; R2 baseline graph-only store) per the amended SQ-04 contract.

## 3. Qualifications carried (Q-L1…Q-L8)

See the acceptance record §5: Linux scope restricted to the demonstrated hosted environment
(Ubuntu 26.04.1 x64, SDK 10.0.404, hosted runner); only 75-case-suite behavior; managed fault
injection only (F-02, no crash/power-loss); locking/WAL/mmap internals uninstrumented;
hosted-run independence receipt-based; Windows-x64/serialized-single-writer limits the
non-Linux scope; restricted real-authority cross-unit union (F-01); reviewer-independence
limitations (same-harness; organizational independence unestablished).

## 4. M5.4 amended-contract limitations (binding on citation)

MR-1 conservative false-negative behavior; MR-2 constructed-control evidence does not establish
natural reachability; MR-3 the exhaustive Roslyn symbol-association proof remains unproven and
unclaimed; MR-4 the structural universe-membership bound narrows but does not eliminate
identity-substitution residual risk.

## 5. Explicitly not accepted

Stage 6 obligations DF-1…DF-8 (full D3, D4.5, full S-02, F-02 crash/power-loss, cancellation
paths, full platform certification); WP-S24-3 optional cross-target scope; public Find Usages;
optimization; migration/dual-write; package publication; any platform or distribution beyond the
demonstrated Linux scope.