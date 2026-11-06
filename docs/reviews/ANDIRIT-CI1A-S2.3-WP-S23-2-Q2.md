# ANDIRIT-CI1A-S2.3-WP-S23-2-Q2

## Independent requalification handoff: occurrence/graph owner correspondence after remediation

**Handoff role:** producer → independent reviewer.
**Repository:** `D:/projects/Andirit`. **Branch:** `main`.
**Commissioned disposition to determine:** owner acceptance of the WP-S23-2 durability gate, or further remediation.

## 1. Pin

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Q1 reviewed HEAD (defect baseline) | `d94052d2fa8dd14ecbfcc6337b4a262e8954ba85` | `53966a01b69c876bfff29a60c576c595bdb9e84b` |
| **R1 remediation commit (requires requalification)** | **`1bce70ee2f52ed5100a2e68bc004462f9824d2f2`** | **`a6a908ab69740e4ee114e845cdde9374975dc15a`** |
| Q1 finding | **F-01 — High / blocking: occurrence authority does not verify published graph ownership** | — |
| Remediation report | [`ANDIRIT-CI1A-S2.3-WP-S23-2-R1.md`](ANDIRIT-CI1A-S2.3-WP-S23-2-R1.md) | — |
| R1 evidence | [`ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence/`](ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence/) | — |
| Reused independent probe driver | [`CI1A-S2.3-WP-S23-2-R1-tests/`](CI1A-S2.3-WP-S23-2-R1-tests/) | — |
| Preserved Q1 report | [`ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md`](ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md) | — |

The Q2 reviewer must qualify the remediation commit (or a later additive documentation commit whose production tree is unchanged), not the Q1 baseline. The Q1 report and evidence are preserved and must not be rewritten.

## 2. Production change under review

One production file changed: `src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs`. The query gate now reconciles the accepted occurrence projection against the accepted published graph owner set in both directions and refuses `CorruptedIndex` for (a) an orphaned/retired authority-bearing occurrence unit with no published graph owner, and (b) a published authority-requiring graph unit with no occurrence association. No public API, schema, namespace, identity, or persisted representation changed. Exact diff: `ANDIRIT-CI1A-S2.3-WP-S23-2-R1-evidence/production-diff.txt`.

## 3. Required Q2 challenge scope

The Q2 reviewer must independently challenge, with separate evidence and without modifying production:

1. **Retirement correctness** — removing a published graph owner must make its exclusively-owned occurrences not report as valid current occurrences. Reproduce F-01 with the unchanged Q1 probe and confirm the corrected refusal. Attempt to bypass the ownership correspondence through other removal/replacement/staging paths.
2. **Persisted occurrence validity** — verify the correction holds at a durable reopen, including that the occurrence projection is genuinely reconstructed (not filtered at query time) and that no durable rows are mutated by refusal.
3. **Completeness semantics** — verify a genuinely complete result with zero occurrences (`Available`/0/`IsComplete = true`) remains distinct from unavailable/invalid coverage, and that an orphaned occurrence is never reported as complete. Challenge the accounting and `RequiresAuthority` gates.
4. **Unaffected publication preservation** — verify retiring one unit does not invalidate occurrences supported by other valid published units; verify retained-owner, replacement, graph-only, complete-empty, staged-resumption (Q1-Q1) and unknown-outcome-quarantine semantics are preserved.
5. **Fresh-process reopen** — verify the behavior with distinct PIDs and a real persistence boundary, independent of the producer harness.
6. **Both-direction negative controls** — verify orphan occurrence refusal and missing mandatory association refusal, and confirm no over-refusal for legitimate marker-false graph-only units.
7. **No regression to accepted guarantees** — NEW-01 recovery, stale-context refusal, conflict refusal, coverage/refusal accounting, native mirrors, and D3/D4/D5.

Rejected/invalid remediation shapes to check for: suppressing the condition by changing assertions or filtering test data; a query-only workaround while persisted state is inconsistent; weakened publication/retirement validation; changed externally observable API contracts.

## 4. Producer-declared results (for challenge, not acceptance)

- R1 regression tests 6/0/0; existing WP-S23-2 32/0/0; NEW-01 17/0/0; full solution 834/0/0; R2 oracle 185/185 (output SHA-256 unchanged); format/Release/Debug clean.
- Independent-probe fresh-process F-01 reproduction: before `Available`/1/complete → after `CorruptedIndex`/0/0/not complete.
- 35/35 representative preserved-behavior independent-probe expectations met.

## 5. Boundary

This handoff starts no implementation work and grants no acceptance. WP-S23-3 and the Q1-Q1/Q1-Q2 qualifications remain separately commissioned. The Q2 reviewer must not modify production during qualification and must use separate evidence.
