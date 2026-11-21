# ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01

**Date**: 2026-11-21
**Gate**: M54-B-QUAL-01 — independent qualification of M5.4 under the amended contract
(`docs/decisions/adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6), executed per the prepared
commission (`docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01.md`).
**Pinned implementation**: `eafd632b...` / tree `823e3c65…` (the gate-remediation revision).
**Formal disposition**: **SUPPORTED** — every binding amended criterion is independently
supported; no Option A escalation condition is triggered; no owner acceptance is granted here.

---

## 1. Role, independence and method

The reviewer derived all acceptance criteria from the amended contract before observing any
production output and built a reviewer-owned harness (`harness/`) that references the pinned
production projects under the reserved `Andirit.CSharp.Tests` assembly identity **without
modifying production code, canonical tests, dependencies, or CI policy**. Expected values were
computed independently (e.g. the `asmctx3` target id recomputed through
`CodeOccurrenceIdentity.ComputeTargetId` over the mapped R2-faithful `compctx1`). The distinct-
process reopen leg runs through the public store API plus the live authority boundary, so the
reopen observability is the production contract, not a test seam.

Independence is method-level (fresh harness, contract-derived expectations, independent
derivations, primary-source receipts). Organizational independence is not established
(no employment/reporting-line evidence) — the same recorded boundary as prior commissions.
Same-session sequencing with the commission's preparation is disclosed; the SQ-05 G3 amendment
precedent permits this pattern when the limitation is recorded.

## 2. Execution summary

| Item | Value |
| --- | --- |
| Exercises | **11 passed / 0 failed / 0 skipped** (`qualification-run.log`, `trx/m54b-results.trx`) |
| Refusal-path coverage | **11/11 in-process refusal branches hit** (`receipts/refusal-path-coverage.json`) |
| Native gate / mapping paths | receipt-evidenced in the distinct reopen process (`e3b`/`e3b2`/`e3c`) |
| Prohibited-direction outcomes | **none** |
| Option A escalation conditions | **none triggered** |
| Environment | Windows x64, SDK 10.0.401, pinned exact-byte export (4,555 blobs, 0/0/0 verified) |

## 3. Criterion-level results (full detail in `matrix.md`)

**C1 Isolated refusal behavior — supported.** E1a: a deliberately absent defining compilation
yields `MissingDefiningCompilation` at resolution, minting, and usage; no fact is emitted;
`RequireCompilation` refuses. E1b: both construction guards (unmatched project reference;
non-member universe dependency) refuse before minting — a complete admissible universe cannot
carry a member depending on an unverified compilation. E1c: `Incomplete` (missing scope unit /
empty universe) is correctly distinguished from `MissingDefiningCompilation`.

**C2 Integrated accounting guarantees — supported.** E3a: unresolved/external/unsupported sites
are counted as coverage, never minted (1 fact for the in-universe call only). A4/A4b: an
incomplete universe records no authority envelope and `PrepareNativeUnit` refuses publication
without one. E3b: authority-bound publication survives native persistence and a distinct-process
reopen with the fact id and counters preserved (`Available`). E3b2: a partial coverage record
reopens as `IncompleteAnalysis` with the truthful reason "a zero-result answer is not a complete
negative" — never false-available. E3c: an authority-masking tamper (envelope stripped while the
unit claims required authority) is refused `AnalysisUnavailable` with zero occurrences served.

**C3 Pre-mint validation — supported.** The mint gate refuses without a non-null recorded
defining `compctx2` (E2); every non-Complete status (MissingRecordedContext, StaleContext,
MissingDefiningCompilation, Incomplete, UnsupportedExternalEvidence, Ambiguous) refuses with its
own mapped status and `mintedAny: false` (E2). The natural positive control mints with a target
id equal to the reviewer's independent computation (E2p).

**C4 Prohibited-outcome screen — supported.** No executed case produced a minted fact without a
Complete defining resolution, a false-available/false-complete result, an incorrect refusal
reason, or a mint from an unsupported channel.

## 4. Constructed vs natural behavior (preserved distinction)

Constructed controls (labeled in every receipt): the isolated missing-defining case; the
incomplete/empty universes; the six-status screen (incl. the ambiguous two-instance universe and
the runtime-forwarded PE case); the partial-coverage store leg; the authority-masking tamper.
Naturally reproduced behavior: the in-universe mint and its authority envelope; the complete-
universe extraction accounting; publication → native persistence → distinct-process reopen →
query; and the production construction gates exercised with constructed inputs. **No constructed
control supports a natural-reachability claim**, and the four inconclusive compiler probes from
the R3 record are not treated as positive evidence — consistent with the amendment §6/§8.

## 5. Qualifications and residual limitations

- Input reachability of a true missing-defining case by an admissible production input remains
  **unproven and unclaimed**; the exhaustive Roslyn symbol-association proof remains **unproven
  and unclaimed** (amendment §8, restated).
- The demonstrated guarantee is **conservative (fail-closed)**: a hypothetical false refusal
  would suppress a legitimate fact and cannot produce a prohibited outcome.
- Qualification is Windows-x64 (SDK 10.0.401); no cross-platform claim.
- Managed-fault durability (F-02) and full native matrix items are outside this commission.
- Refusal-path line coverage was collected for in-process legs only; native gate lines are
  receipt-evidenced (coverlet does not instrument the reopen child).

## 6. Option A escalation assessment

All four conditions of the prepared commission §7 were evaluated and **none is triggered**:
(1) no prohibited-direction outcome; (2) the isolated refusal is demonstrable (E1a, line 119
hit ×4); (3) the integrated accounting chain carries the unresolved classification through every
stage; (4) the amended basis is sufficient as written. Details in `findings.md`.

## 7. Disposition and recommendation

**`SUPPORTED`** — M5.4 is supported under the amended contract on the independently exercised
criteria C1–C4.

**Recommendation for the owner (separate decision, not granted here):** record M5.4
`SATISFIED UNDER AMENDED CONTRACT`, restating the conservative false-negative limitation, the
unproven reachability/association items, and the constructed-control labeling at every downstream
citation.

## 8. Seal

This report and `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01-evidence/` (self-excluding SHA-256
`manifest.json`) are committed locally on `main`; the sealed commit identity is supplied in the
commit record rather than embedded here, per the established anti-self-reference convention. No
production, test, dependency, formatter, coverage or CI-policy change; no push; no owner
acceptance; WP-S25-4 closure and Stage-2 acceptance remain separate owner decisions.