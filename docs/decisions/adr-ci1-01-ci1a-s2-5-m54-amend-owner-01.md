# ADR-CI1-01-CI1A-S2.5-M54-AMEND-OWNER-01 — bounded M5.4 contract clarification (conservative-refusal acceptance basis)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-AMEND-OWNER-01` (Option B per `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01`).
**Project:** Andirit. **Recorded:** 2026-11-21. **Canonical branch:** `main`.
**Owner decision:** **`M5.4 BOUNDED AMENDMENT APPROVED WITH QUALIFICATIONS`** — clarification of the *acceptance basis* for M5.4 to the conservative-refusal guarantee, preserving the intended correctness properties.
**Amended obligation status:** **`M5.4 AMENDMENT-APPROVED — PENDING INDEPENDENT QUALIFICATION`** (commission `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01`, prepared below). **This decision does not close M5.4 and grants no technical qualification.**

> **Status: RECORDED — OWNER-AUTHORIZED (bounded contract clarification).**
>
> The Stage-2 acceptance matrix §5 ("Conservative Refusal and Coverage", row 4) requires the
> `Unresolved` refusal — missing defining compilation/universe — with independent S2.5
> acceptance; the qualification records read that row as requiring a *reachable* admissible
> input reaching the specific `MissingDefiningCompilation` reason. Four preregistered
> compiler-level probes under the pinned Roslyn produced no such input. This amendment,
> following the SQ-04 amendment pattern, narrows the **acceptance basis** — not the runtime
> guarantee: M5.4 is satisfied when (a) the implemented `MissingDefiningCompilation` refusal is
> demonstrated in isolation, (b) the exhaustive pre-mint defining-evidence validation (which
> forbids minting without defining evidence) is independently qualified, and (c) the integrated
> accounting chain (authority-bound unresolved classification through publication, native
> persistence, query and distinct-process reopen) is independently qualified. No correctness
> requirement is removed: the prohibited outcomes (false-available, false-complete, wrong
> reason, mint from an unsupported channel) remain prohibited and are strengthened by the
> pre-mint validation evidence. Inconclusive probes are not presented as proof; the exhaustive
> compiler symbol-association proof remains **unproven and unclaimed**.

This is an additive owner decision. It changes no production source, test, fixture, CI
configuration, reviewer evidence, oracle or corpus, prior decision, historical review artifact,
or existing planning contract definition. The authoritative register, ledger and plan receive
dated additive updates only.

## 1. Preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `5be643dcf962c923fcf15b1d6ff245d2aa342159` / `9b75cb850908901c70933bc7d1d0ea36efb3d69a` (as expected) |
| Branch / worktrees | `main`; exactly one; clean tracked/index; no stashes |
| Protected scope | `git diff HEAD -- src tests .github` **empty**; production/test/CI content equals the sealed remediation revision `eafd632` |
| Governing inputs | `stage2-acceptance-matrix.md` §5 row 4; `wp-s25-4-qualification-matrix.md` M5.4; QUAL-02/03/04 and R3 records; `M54-RESOLUTION-PROPOSAL-01`; the `MissingDefiningCompilation` production call sites |
| Environment | Windows `10.0.26200` x64; .NET SDK `10.0.401` |

## 2. Original M5.4 obligation (independent reconstruction)

**Stage-2 acceptance matrix §5 row 4** (verbatim): under "Conservative Refusal and Coverage
(IF-03)" — "`Unresolved` refusal | Missing defining compilation/universe | Defining compilation
not in universe → refusal | — | native | S2.2 implementation; S2.5 independent acceptance".

**S2.5 qualification matrix M5.4** (verbatim): requirement "`Unresolved` refusal — Missing
defining compilation/universe"; independent check "Hold declared universe fixed, vary membership
and scope; independently tabulate coverage and distinguish incomplete, unavailable and complete
zero"; case "Defining compilation not in universe → refusal"; receipt "Per-case
membership/counters, query status and persisted coverage after reopen"; pass condition "All named
observations and controls agree; block on **False-available, false-complete, wrong reason, or
mint from unsupported channel**".

**Qualification reading (QUAL-02→QUAL-04):** the row demands the **exact continuous
`MissingDefiningCompilation` chain** — a defining compilation missing from the declared universe
must reach the specific refusal through the production resolution path, observable through query
status and persisted coverage after reopen. **Disposition D: REACHABILITY UNRESOLVED —
ADDITIONAL EVIDENCE REQUIRED** (not an unreachability claim, not a defect claim).

## 3. Claim separation — established facts vs unproven vs excluded

| # | Claim | Class | Status |
| --- | --- | --- | --- |
| 1 | The `MissingDefiningCompilation` refusal is implemented in production | Established | Source audit (QUAL-03) + current HEAD call sites: `VerifiedCSharpAnalysis.cs` (3), `VerifiedCSharpCompilation.cs` (2), `VerifiedDefiningResolution.cs`, `VerifiedTargetMint.cs` |
| 2 | No fact is minted without a defining compilation | Established (by construction and audit) | Pre-mint validation checks exact source references, captured PE references, and the full verified dependency closure before minting (traced at all three resolve-defining call sites) |
| 3 | The unresolved classification is carried through the integrated chain | Established for qualified cases | Authority-bound unresolved/`StalePublication`/`AnalysisUnavailable` behavior qualified through R1–R3, membership, and the corrected-revision suites (publication, native persistence, query, distinct-process reopen) |
| 4 | A *reachable admissible input* produces a continuous `MissingDefiningCompilation` receipt | **Unproven** | Four preregistered probes (CS0012 gate-unreached; direct `Complete`; retargeted `Complete`; runtime forwarding → `UnsupportedExternalEvidence`) — **inconclusive for reachability; not positive proof in either direction** |
| 5 | An exhaustive Roslyn symbol/reference-association proof (no admissible input yields a *false* `MissingDefiningCompilation`) | **Unproven / not attempted fully** | Static constraints do not establish it; the four probes do not refute it |
| 6 | Any false `MissingDefiningCompilation` would suppress a legitimate fact (false negative) | Established direction analysis | The prohibited outcomes are false-positive directions (false-available, false-complete, wrong reason, unsupported mint); the conservative-refusal doctrine of §5 accepts fail-closed refusals; the membership/counter evidence shows no prohibited outcome in any qualified case |

## 4. Authority and amendment-permission assessment

- **Owner-clarification route is written into the governing records**: QUAL-03 explicitly named it — "alternatively the owner may explicitly clarify whether separate defensive and integrated accounting proofs satisfy the intended guarantee"; the S2.5 matrix requires *S2.5 independent acceptance* of the row, i.e., the acceptance basis is a qualification fact within the guarantee's intent, not a compiler-proof mandate. The matrix row's acceptance cell ("S2.2 implementation; S2.5 independent acceptance") separates implementation (established) from acceptance (this route).
- **The SQ-04 amendment precedent** (`adr-ci1-01-ci1a-s2-5-sq04-amend-owner-01.md`) establishes the owner-amendment pattern: narrow the *qualification scope* with explicit exclusions, preserve the runtime guarantee verbatim, record a future-discovery/reopening clause, prohibit unsupported claims.
- **Limits observed**: the amendment cannot convert inconclusive probes into affirmative reachability evidence (none is claimed); cannot rewrite historical records (none is); cannot weaken D5/§5 refusal guarantees (untouched); cannot discharge the obligation (M5.4 stays open pending independent qualification).

## 5. Amendment justification (proportionality) — including the stop test

| Consideration | Finding |
| --- | --- |
| Does the amendment remove any correctness requirement? | **No.** The prohibited outcomes (false-available, false-complete, wrong reason, mint from unsupported channel) are preserved verbatim and are **strengthened**: claim 2 (no mint without defining evidence) is now independently qualified evidence rather than an audited assertion |
| Does it materially weaken the intended correctness guarantee? | **No (stop test not triggered).** The intended guarantee is fail-closed treatment of missing defining evidence. The amended basis preserves the refusal implementation, requires its isolated demonstration, and requires the integrated accounting chain; the only removed element is the *reachability* property of one reason code — an evidentiary property, not a correctness property. Any hypothetical false refusal is a conservative false negative, orthogonal to the guaranteed outcomes |
| Is the refused direction safe by construction? | Yes — the failure direction of an unproven association shape would be a refusal (no fact), never an unsupported mint; the minting path is gated before use |
| Could additional evidence reasonably close the reachability gap? | Uncertain — that is Option A (pinned compiler proof), preserved as escalation; its cost/outcome is unknown, which is why the owner chose B with A retained |
| Would the amendment hide a defect? | **No** — the four probes and the reachability gap are preserved verbatim in the historical record and restated here; the exhaustive symbol-association proof remains explicitly unproven and unclaimed |

**Conclusion.** The original M5.4 breadth (a demonstrable reachable admissible input for the
specific reason code, backed by an exhaustive association proof) is disproportionate to the
guarantee the row exists to protect, while the guarantee itself is preserved and strengthened
under the amended basis. **The amendment is justified and permitted; no stop is required.**
Option A remains available as the escalation route if the amended qualification fails or the
owner later requires the original basis.

## 6. Approved M5.4 contract language (exact)

The following wording **replaces, for M5.4 acceptance purposes only**, the reachability breadth
of acceptance matrix §5 row 4 / qualification matrix M5.4. The §5 row's prohibited outcomes,
the refusal implementation, and all runtime guarantees remain in force unchanged.

> **M5.4 (amended; owner decision `ANDIRIT-CI1A-S2.5-M54-AMEND-OWNER-01`).**
> Acceptance of M5.4 is established by the **conservative-refusal acceptance basis**: for a
> defining compilation missing from the declared universe, production must never mint an
> occurrence fact and must never report false-available, false-complete, wrong-reason, or
> mint-from-unsupported-channel outcomes. Satisfaction requires independent qualification of:
> (a) **isolated refusal behavior** — the `MissingDefiningCompilation` refusal
> (`VerifiedDefiningStatus`/`VerifiedTargetMintStatus`, `CodeOccurrenceSchemaException` /
> `InvalidDataException` refusal paths) demonstrated on a constructed missing-defining case,
> labeled as a constructed control; (b) **integrated accounting guarantees** — the unresolved
> classification carried through publication, native persistence, query, and distinct-process
> reopen, with correct counters and coverage states (incomplete/unavailable/complete-empty
> distinguished); and (c) **pre-mint defining-evidence validation** — the exact source-reference,
> captured PE-reference and full verified dependency-closure checks that forbid minting without
> defining evidence, qualified at their call sites. **The demonstrated guarantee is conservative
> (fail-closed)**: a false-`MissingDefiningCompilation` outcome would suppress a legitimate
> fact (false negative) and can never produce the prohibited outcomes. **Unproven and unclaimed:
> the reachability of a true missing-defining case by an admissible production input, and the
> exhaustive Roslyn symbol/reference-association proof.** If independent qualification under
> this basis fails, or the owner later requires the original reachability basis, M5.4 returns to
> the owner (escalation to the pinned compiler-proof route, Option A).
> M5.4 is **amendment-approved, pending independent qualification**; full historical/compiler
> reachability is **not** claimed.

## 7. Original-to-amended comparison

| Dimension | Original (pre-amendment) | Amended (this decision) |
| --- | --- | --- |
| Acceptance object | A reachable admissible input producing the continuous `MissingDefiningCompilation` reason | The conservative-refusal guarantee implemented and demonstrated: isolated refusal + integrated accounting + pre-mint validation |
| Compiler proof | Implicitly required (exhaustive symbol-association) to close the row | **Not required, not claimed**; reserved as Option A escalation |
| Prohibited outcomes | False-available, false-complete, wrong reason, mint from unsupported channel | **Identical, preserved verbatim** |
| Refusal implementation | Required | Required, unchanged |
| Integrated accounting | Required | Required, unchanged (authority-bound unresolved through publication/persistence/query/reopen) |
| Demonstrated-input requirement | A natural admissible input exhibiting the refusal | A **constructed** missing-defining control (explicitly labeled) for the isolated path, plus natural integrated cases |
| Failure-direction asymmetry | Unaddressed | Explicit: false refusals are conservative false negatives; the guaranteed outcomes are unaffected |
| Residual statement | "Reachability unresolved" | "Reachability and exhaustive association proof unproven and unclaimed; conservative guarantee qualified under the amended basis" |

**Semantic difference**: none in runtime semantics or prohibited outcomes; the difference is the
acceptance *basis* (evidence set) for the row. **Residual risk**: an admissible input could
theoretically yield a false refusal (suppressed legitimate fact, conservative); or an
unanticipated compiler shape could — contrary to the audited pre-mint validation — bypass the
validating gate; the former is qualified as a stated limitation, the latter is exactly what the
integrated accounting + adversarial controls of `M54-B-QUAL-01` must probe (escalation to Option
A if any probe shows a prohibited-direction outcome).

## 8. Residual risk and exclusions

| Item | Statement |
| --- | --- |
| Unproven | Reachability of a true missing-defining case by an admissible production input; the exhaustive Roslyn symbol/reference-association proof |
| Excluded claims | "M5.4 reachability demonstrated"; "compiler association exhaustively proven"; any use of the four inconclusive probes as positive proof |
| Constructed evidence scope | The isolated refusal control is constructed; it demonstrates the refusal path, **not** input reachability |
| Residual risk | Conservative false-negative risk (suppressed legitimate fact) under an unproven association shape; quantified only in the direction that no prohibited outcome can result |
| Escalation | Failure of the amended qualification, or a later owner requirement for the original basis → Option A pinned compiler proof (`35d9211b…`) |
| Not discharged by this amendment | M11.4/M14.1/M14.2 accepted obligations (already dispositioned); WP-S25-4 closure; SQ-06 remainder; D3-full; applicable D4/D5; Stage-2 acceptance; Stage 6 |

## 9. Obligation disposition

**`M5.4 — AMENDMENT-APPROVED, PENDING INDEPENDENT QUALIFICATION`** (not closed; no technical
qualification granted).

- **Amended contract approved** for acceptance purposes (§6), with §7's comparison and §8's
  residual-risk statement binding on the qualification and on every downstream citation.
- **Independent qualification required**: `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01` (prepared
  with this commission; execution is a separate, separately authorized commission). On its
  `SUPPORTED`/`SUPPORTED WITH QUALIFICATIONS` outcome, M5.4 becomes **SATISFIED UNDER AMENDED
  CONTRACT** at a subsequent owner record.
- The historical disposition **D** is preserved verbatim in QUAL-02/03/04 and the R3 record;
  this decision supersedes its breadth for future acceptance purposes only.

## 10. Stage-2 acceptance implications

- M5.4 is **no longer an open-ended blocker** once the amended qualification passes; until then
  it remains a listed prerequisite for WP-S25-4 closure and Stage-2 acceptance.
- The Stage-2 acceptance decision must restate: the amended basis, the conservative
  false-negative limitation, and the unproven-reachability statement; it must **not** record
  "reachability proven" anywhere.
- No conflict with higher-level decisions: §5/IF-03 refusal guarantees, D5, D3, D4, and the
  Candidate C architecture decisions are untouched.

## 11. Binding qualifications (preserved)

Unchanged: the Windows-x64 and serialized single-writer scopes; managed-fault durability limits
(F-02); restricted real-authority cross-unit union (F-01); all M11.4/M14.1/M14.2 accepted
qualifications (Ubuntu 26.04.1 demonstrated environment; uninstrumented platform internals;
receipt-based hosted independence); the SQ-04 amended-contract qualifications; reviewer-
independence limitations. **New/standing from this decision**: M5.4's amended basis, its
conservative false-negative limitation, and its unproven-reachability statement.

## 12. Successor authorization status

- **`M54-B-QUAL-01` (independent qualification): PREPARED, NOT AUTHORIZED to execute** — the
  owner authorizes it as a separate commission when desired.
- **Option A (pinned compiler proof): preserved as escalation**; authorized only by a future
  owner decision.
- WP-S25-4 closure, Stage-2 acceptance, Stage 6: unauthorized and unaffected.

## 13. Repository discipline

Only this decision document, the independent-qualification commission, the updated closure
checklist, and dated additive updates to the S2.5 obligation register, the WP-S25-4 obligation
inventory, and the S2.5 work-package plan are added, locally on `main`, under the
single-worktree policy. No production code, test, CI configuration, fixture, historical review,
evidence, or prior decision is modified. No fetch, push, or remote operation is performed. The
commit/tree identities are reported in the handoff.