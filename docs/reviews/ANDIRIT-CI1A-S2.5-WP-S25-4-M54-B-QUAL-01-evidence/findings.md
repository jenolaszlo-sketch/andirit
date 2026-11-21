# Findings, residual limitations and recommended disposition — M54-B-QUAL-01

Independence: the reviewer session computed every expectation from the amended contract
(`adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6) before observing production output; the harness
(`harness/`) is reviewer-owned, built against the reserved `Andirit.CSharp.Tests` assembly
identity, and references the pinned production projects without modifying them. The harness and
its expected outcomes were derived from the contract text, not from prior execution results.
Organizational independence is not established (no employment/reporting-line evidence); the
same standard recorded by prior commissions applies.

## Findings

| ID | Severity | Finding |
| --- | --- | --- |
| M54B-F01 | INFORMATIONAL | Constructed controls are used for the isolated refusal, the status screen, the partial-coverage store leg, and the authority-masking tamper. Each is explicitly labeled in its receipt; none supports a natural-reachability claim. The amended contract anticipated exactly this labeling. |
| M54B-F02 | INFORMATIONAL | Refusal-path line coverage was collected only for the in-process legs (coverlet does not instrument the distinct reopen process). The native authority gate and availability-mapping paths are receipt-evidenced instead (`e3b`/`e3b2`/`e3c` reopenedRaw); they are **not** claimed as line coverage. |
| M54B-F03 | INFORMATIONAL | The `Ambiguous` classification was demonstrated with two identity-equal compiled instances in one universe (a constructed ambiguity). A *natural* ambiguous production case was not constructed and is not claimed. |
| M54B-F04 | INFORMATIONAL | Structural observation (not a defect finding): through `VerifiedCSharpCompilation.Create` and `VerifiedCSharpUniverse.Create`, an admissible complete universe cannot carry a member depending on an unverified compilation (guard receipts in `e1b`). This bounds, but does not eliminate, the identity-substitution residual risk the amendment §8 records. |
| M54B-F05 | INFORMATIONAL | Same-session sequencing: this commission was executed by the same assistant session that prepared the commission document (a consequence of the amendment commission's authorization to prepare and the subsequent execution commission). Independence is method-level: fresh harness, contract-derived expectations, independent target-id computation, primary-source receipts. Hosted/producer independence is receipt-based, consistent with the SQ-05 G3 amendment precedent. |

## Prohibited-outcome screen (C4) — PASS

| Prohibited outcome | Result | Evidence |
| --- | --- | --- |
| Minted fact without a Complete defining resolution | None observed | `e2` mintedAny=false across all six non-Complete statuses; `e3a` facts limited to the in-universe call |
| False-available / false-complete | None observed | `e3b` Available only with complete coverage and a validated authority; `e3b2` partial → `IncompleteAnalysis` with the truthful reason "a zero-result answer is not a complete negative" |
| Incorrect refusal reason | None observed | Statuses/detail match the constructions (e.g. Ambiguous reports "2 verified compilations bind the same target symbol", not a missing-defining reason) |
| Mint from an unsupported channel | None observed | PE-only → `UnsupportedExternalEvidence`; masked authority → `AnalysisUnavailable`; missing authority envelope → `MissingVerifiedOccurrenceAuthority` |

## Option A escalation conditions — none triggered

| # | Condition | Triggered? | Basis |
| --- | --- | --- | --- |
| 1 | Prohibited-direction outcome in any adversarial case | **No** | Screen above |
| 2 | Isolated refusal not demonstrable even on constructed input | **No** | `e1a` |
| 3 | Integrated accounting fails to carry the unresolved classification | **No** | `e3a`/`e3b`/`e3b2`/`e3c` |
| 4 | Amended basis insufficient; original reachability basis required | **No** | All amended criteria satisfied with labeling preserved |

## Residual limitations (carried, per the amendment §8)

- Input reachability of a true missing-defining case by an admissible production input remains
  **unproven and unclaimed**; the exhaustive Roslyn symbol-association proof remains **unproven
  and unclaimed**.
- The demonstrated guarantee is **conservative (fail-closed)**: a hypothetical false refusal
  would suppress a legitimate fact (false negative) and cannot produce a prohibited outcome.
- The qualification is Windows-x64 (SDK 10.0.401); no cross-platform claim is made.
- Managed-fault durability (F-02) and full native matrix items are outside this commission.

## Recommended owner disposition

**Recommend M5.4 `SATISFIED UNDER AMENDED CONTRACT`** (owner acceptance record separate from this
commission), on the basis that all binding amended criteria are supported:
C1 isolated refusal behavior (C1.1–C1.5), C2 integrated accounting (C2.1–C2.6), C3 pre-mint
validation (C3.1–C3.4), and the C4 prohibited-outcome screen — each independently exercised and
receipt-evidenced, with the refusal-path coverage receipt covering 11 in-process refusal branches.
The restatement obligations (conservative false-negative limitation; unproven reachability and
association proof; constructed-control labeling) must accompany any downstream citation.