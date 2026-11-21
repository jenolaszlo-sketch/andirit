# Requirement-to-evidence disposition matrix — M54-B-QUAL-01

Criteria derive from the amended M5.4 contract
(`docs/decisions/adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md` §6) and the prepared commission
(`docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01.md`). Every verdict rests on a harness
receipt or the refusal-path coverage receipt in `receipts/`; the harness was built and executed
against the pinned exact-byte export of `eafd632` (4,555 blobs, 0/0/0 verified).

Dispositions: PASS · PASS-QUAL (pass with recorded qualification) · QUAL (recorded finding).

## C1 — Isolated refusal behavior

| # | Requirement | Result | Evidence |
| --- | --- | --- | --- |
| C1.1 | A defining compilation absent from the declared universe yields the `MissingDefiningCompilation` refusal (isolated, constructed control) | PASS | `receipts/e1a.json` — resolvedStatus `MissingDefiningCompilation`, detail "no verified compilation in the declared universe is the symbol's defining assembly"; line-119 coverage hit ×4 (`receipts/refusal-path-coverage.json`) |
| C1.2 | The refusal returns no minted target and no emitted fact (mint gate + usage) | PASS | `e1a.json` — mintStatus `MissingDefiningCompilation`, minted=false, usageMinted=false, usageFactEmitted=false; mint-gate line 167 hit ×13; MintUsage refusal line 203 hit ×1 |
| C1.3 | The natural construction guards refuse before minting: unmatched project reference; non-member dependency | PASS | `e1b.json` — `MissingDefiningCompilation` at compilation creation (unmatched project reference) and universe creation (dependency not a member); `PrepareNativeUnit` line 60 hit ×1 (also exercised in A4b) |
| C1.4 | `MissingDefiningCompilation` is distinguished from `Incomplete` (declared-universe variant) and from `UnsupportedExternalEvidence` (PE-only) | PASS | `e1c.json` (Incomplete with missing-unit detail), `e2.json` (`UnsupportedExternalEvidence` with the PE-only detail), `e3c`/`e3b2` (native correctness) |
| C1.5 | `RequireCompilation` refuses a non-member compilation and an incomplete universe | PASS | `e1a.json` (`MissingDefiningCompilation`), `e1c.json` (`IncompleteCompilationUniverse`) |

## C2 — Integrated accounting guarantees

| # | Requirement | Result | Evidence |
| --- | --- | --- | --- |
| C2.1 | Extraction reports unresolved/external/unsupported sites as counters, never as facts | PASS | `e3a.json` — facts=1 (the in-universe call only), external=1, unresolved=1, unsupportedCategories=1 |
| C2.2 | A complete universe records an authority envelope; an incomplete universe never does | PASS | `e3a.json` (authorityRecorded=true, authorityFacts=1) and `a4.json` (authorityRecorded=false, coverageState=Partial) |
| C2.3 | The production bridge refuses to publish without a verified authority | PASS | `a4b.json` — `MissingVerifiedOccurrenceAuthority`; `PrepareNativeUnit` line 60 hit |
| C2.4 | Published coverage and facts survive publication, native persistence, close, and distinct-process reopen, authority-bound | PASS | `e3b.json` — reopen `Available`, 1 occurrence, fact id identical to the pre-publish fact, coverage counters preserved (unresolved=1, external=1) |
| C2.5 | A partial (incomplete) coverage record reopens as `IncompleteAnalysis` with a truthful refusal reason — never false-available | PASS | `e3b2.json` — `IncompleteAnalysis`, reason "The declared analysis scope is not proven complete; a zero-result answer is not a complete negative." |
| C2.6 | An authority-masked store refuses with `AnalysisUnavailable` and a truthful reason | PASS | `e3c.json` — constructed tamper (envelope stripped while the unit claims required authority): "Current verified compilation authority is required."; 0 occurrences |

## C3 — Pre-mint validation

| # | Requirement | Result | Evidence |
| --- | --- | --- | --- |
| C3.1 | A mint without a non-null recorded defining `compctx2` never succeeds (gate) | PASS | `e2.json` — MissingRecordedContext for null and blank; line 163 hit ×2 |
| C3.2 | Every non-Complete defining status refuses the mint (status screen) | PASS | `e2.json` — MissingDefiningCompilation, Incomplete, UnsupportedExternalEvidence, Ambiguous, StaleContext all non-minted; `mintedAny: false` |
| C3.3 | A Complete resolution mints with an independently computed target identity (positive control) | PASS | `e2p.json` — `Minted`, targetId equals the reviewer's independent `ComputeTargetId` over the mapped R2-faithful compctx1 |
| C3.4 | Reference-capture validation rejects unmatched project references before minting | PASS | `e1b.json` (guard 1) |

## C4 — Prohibited-outcome screen

| # | Requirement | Result | Evidence |
| --- | --- | --- | --- |
| C4.1 | No executed case mints a fact without a Complete defining resolution | PASS | `e2.json` mintedAny=false across all non-Complete statuses; `e3a.json` facts limited to the in-universe call |
| C4.2 | No executed case reports false-available or false-complete | PASS | Every refusal carries a truthful reason; `e3b` Available only with complete coverage and a validated authority; `e3b2` partial → IncompleteAnalysis |
| C4.3 | No executed case reports an incorrect refusal reason (statuses match the constructions) | PASS | Receipts record the exact status/detail per construction; the Ambiguous case reports "2 verified compilations bind the same target symbol" rather than any missing-defining reason |
| C4.4 | No executed case mints from an unsupported channel (PE-only, ambiguous, stale, masked) | PASS | `e2.json` (PE → UnsupportedExternalEvidence), `e3c.json` (masked authority refused), `a4b.json` (publication refused) |

## Refusal-path coverage evidence

| Path | Evidence |
| --- | --- |
| In-process refusal branches (11 production lines across `VerifiedCSharpAnalysis.cs` and `VerifiedUsageMint.cs`) | `receipts/refusal-path-coverage.json` — allTargetsHit=true (hits 13/6/6/4/3/2/2/1/5/1/1) |
| Native authority gate and availability mapping (distinct reopen process) | Receipt-evidenced (`e3b`/`e3b2`/`e3c` reopenedRaw); coverlet does not instrument the child process — recorded, not claimed as line coverage |

## Labeling: constructed vs natural

| Exercise | Nature |
| --- | --- |
| E1a, E1c, E2(d) | Constructed controls (deliberately absent defining compilation; incomplete/empty universes) |
| E1b, A4, A4b | Natural production guards exercised with constructed inputs |
| E2(a–c, e, f) | Constructed controls for the status screen; E2(f) Ambiguous via two identity-equal instances in one universe (constructed ambiguity) |
| E2p, P | Naturally reproduced positive control (complete admissible universe) |
| E3a, E3b | Natural production behavior on a constructed admissible universe (extraction → bridge → store → distinct-process reopen) |
| E3b2, E3c | Constructed controls through the real publication/authority paths |
| Natural reachability | **Not claimed** — no constructed control is presented as evidence that an admissible production input naturally reaches `MissingDefiningCompilation` |