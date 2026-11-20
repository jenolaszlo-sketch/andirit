# ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01

**Date**: 2026-11-21
**Class**: PROPOSAL — prepared under the G4 owner commission (`ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-OWNER-01` §5). **Nothing is amended, implemented, or closed here.**
**Purpose**: give the owner a bounded, decision-ready resolution route for the M5.4 blocker, with options, consequences, a recommendation, and acceptance criteria.

---

## 1. Exact binding requirement

`docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`, §5 row **M5.4** (verbatim):

- **Requirement**: "`Unresolved` refusal — Missing defining compilation/universe".
- **Independent check**: "Hold declared universe fixed, vary membership and scope; independently
  tabulate coverage and distinguish incomplete, unavailable and complete zero."
- **Case**: "Defining compilation not in universe → refusal".
- **Receipt**: "Per-case membership/counters, query status and persisted coverage after reopen."
- **Pass condition**: "All named observations and controls agree; block on False-available,
  false-complete, wrong reason, or mint from unsupported channel."

The binding reading established by QUAL-03/QUAL-04: the row requires the **exact continuous
`MissingDefiningCompilation` chain** — a defining compilation missing from the declared universe
must reach the `Unresolved`/`MissingDefiningCompilation` refusal through the production
resolution path, with the refusal observable through query status and persisted coverage after
reopen. Mapping/coverage contracts support more general refusal guarantees but do not authorize
substituting them for this row.

## 2. Current blocker (what is established and what is not)

**Established** (prior evidence, preserved):

| Evidence | Content |
| --- | --- |
| Static source audit (QUAL-03) | All three production resolve-defining call sites inventoried; reference capture, exact dependency closure, caller membership, symbol eligibility, live/metadata matching, extraction, authority, publication and persistence traced; predicate table of established vs assumed |
| Production code (current HEAD) | The `MissingDefiningCompilation` refusal exists in production: `VerifiedCSharpAnalysis.cs` (3 sites: lines 67, 78, 119), `VerifiedCSharpCompilation.cs` (2 sites: lines 155, 230), `VerifiedDefiningResolution.cs`, `VerifiedTargetMint.cs` — the classification and its mappings are implemented, not hypothetical |
| Bounded probes (R3) | Four preregistered probes under the pinned compiler (Roslyn 5.9.0, `35d9211b…`): (i) transitive optional parameter omitted → CS0012, resolver gate unreached; (ii) direct Root reference → `Complete`; (iii) source retargeting → `Complete`; (iv) runtime type forwarding (`System.Uri` → `System.Private.Uri`) → `UnsupportedExternalEvidence`. **No probe produced a continuous `MissingDefiningCompilation` receipt**, and none contradicted the production behavior |
| Call-graph analysis (R3) | `VerifiedCSharpOccurrenceExtractor` (first pass) → `MintTarget` (recheck after `Complete`) → `MintUsage` (re-resolve after successful mint) — the re-resolve calls provide no independent missing-defining path |

**Not established**: an exhaustive Roslyn symbol/reference-association proof that *no*
admissible input can produce a false `MissingDefiningCompilation`, and a *reachable* admissible
input that produces a *true* one. Hence the standing disposition:
**D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED** (not an unreachability claim, not a
defect claim). Governance: **BLOCKED** for WP-S25-4.

## 3. Relevant prior decisions and findings

| Record | Position on M5.4 |
| --- | --- |
| QUAL-02 (`ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-02`) | M5.4 fails on the then-scope; source-audit route proposed |
| QUAL-03 (`…-QUAL-03`) | Disposition D after the exhaustive call-site/predicate audit; two named next routes: (a) the narrow investigation (transitive signature exposure, source-symbol retargeting, platform forwarding under the pinned compiler), or (b) an explicit owner clarification on whether separate defensive + integrated accounting proofs satisfy the intended guarantee; **"no amendment or waiver is made here, and no production change solely to manufacture defensive-branch reachability is recommended"** |
| R1–R3 remediations + QUAL-04 | M5.4 unchanged; R3's four probes added no continuous receipt |
| GATE records (`GATE-OWNER-01` §7) | M5.4 kept BLOCKED pending this proposal |

## 4. Classification: evidence, amendment, or implementation?

- **Implementation is not required**: the refusal path exists in production; there is no
  identified defect and no accepted basis for a production change (QUAL-03 explicitly warns
  against manufacturing reachability).
- **Additional evidence or a contract clarification is required** — the two live routes. The
  blocker is evidentiary/contractual, not functional.

## 5. Resolution options and consequences

| Option | Content | Cost / effort | Consequences |
| --- | --- | --- | --- |
| **A — Pinned compiler-level proof** | A bounded commission under the pinned compiler (`35d9211b…`): formalize the symbol/reference-association mechanism (wrapper/synthesized/forwarded/error-recovery shapes) and either prove non-production or exhibit a concrete reachable candidate with measured missing associations; the candidate must then run the full continuous chain (publication → native persistence → query → distinct-process reopen) | High; requires compiler-internals analysis tooling; outcome may still be "unreachable within admissible inputs — proof incomplete" | If it closes: M5.4 passes on evidence alone, no contract change. If it produces a candidate: a genuine refusal-chain receipt for the first time. If inconclusive: the owner must still choose A-continue or B |
| **B — Owner contract clarification (amendment)** | Explicitly decide whether the intended guarantee is met by: an isolated resolver refusal with zero minted facts **plus** the authority-bound unresolved classification demonstrated through publication, native persistence, query and distinct-process reopen (the "defensive + integrated accounting" route) | Low; one decision document modeled on the SQ-04 amendment pattern | Amends the *acceptance basis* for M5.4 (not the production contract or code); requires restating the guarantee boundary at every downstream citation; the isolated refusal must be independently qualified once (small bounded run) |
| **C — Production change** | Modify production to make `MissingDefiningCompilation` reachable | — | **Not recommended and not authorized**: no defect basis; explicitly advised against by QUAL-03; would change production semantics under an evidentiary obligation — forbidden by the current authorizations |

## 6. Recommended owner decision

**Adopt a two-step route: B first, A only if required.**

1. **Authorize Option B as a bounded decision + small qualification**: commission
   `ANDIRIT-CI1A-S2.5-WP-S25-4-M54-AMEND-OWNER-01` (contract clarification, SQ-04-amendment
   pattern) paired with `…-M54-B-QUAL-01` (independent qualification of the isolated-refusal
   + integrated-accounting route under the existing suites — no production change). This
   resolves the blocker at low cost with the guarantee boundary made explicit.
2. Hold **Option A** as the escalation if the owner requires the original continuous-chain
   evidence: a separate `…-M54-A-INVESTIGATION-01` commission under the pinned compiler,
   bounded to the three named shapes (transitive signature exposure, source-symbol
   retargeting, platform forwarding) plus any new hypothesis its pre-registered analysis
   generates.

Rationale: the row's *functional* guarantee (no false-available, no false-complete, no wrong
reason, no unsupported mint) is already qualified across R1–R3 and the membership evidence; the
unresolved part is the reachability of one refusal reason. Option B converts that into an
explicit, qualified boundary rather than leaving an open-ended compiler-proof chase; Option A
remains available and is not foreclosed.

## 7. Acceptance criteria and qualification requirements

For **Option B** (amendment + integrated qualification):

1. The clarification decision names: the exact guarantee substituted, its boundary, the
   carried qualifications, and the restatement obligation at Stage-2 acceptance.
2. Independent qualification demonstrates, on the pinned revision: (a) the isolated resolver
   refusal with zero minted facts (a constructed/authority-bound control is acceptable and must
   be labeled as such); (b) unresolved classification carried through publication, native
   persistence, query and distinct-process reopen; (c) no false-available/false-complete/
   wrong-reason/mint-from-unsupported-channel outcome anywhere in the demonstrated set.
3. Manifest and historical preservation identical to prior commissions; no production/test/CI
   change.

For **Option A** (compiler proof):

1. Pre-registered analysis plan under the pinned compiler; every probe's prediction recorded
   before observation; failed/candidate probes preserved verbatim.
2. Closure requires either an exhaustive association proof or a concrete candidate with measured
   missing associations that then runs the continuous chain end-to-end.
3. Same manifest/historical-preservation obligations; no production change.

## 8. Scope restrictions

This proposal authorizes nothing. M5.4 remains BLOCKED until the owner issues the clarification
decision (B) or the investigation commission (A). WP-S25-4 closure, Stage-2 acceptance, Stage 6,
and any production change remain separate decisions.