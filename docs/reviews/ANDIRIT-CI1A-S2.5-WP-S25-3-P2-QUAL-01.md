# ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01 — independent P2 qualification of the S-01 integrated proof under canonical-forms v1 (clarified)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01`.
**Project:** Andirit. **Recorded:** 2026-11-14. **Canonical branch:** `main`.
**Starting HEAD:** `976fe52a1fef1a69e3d68a7a4227a9337524274c` (tree `c0089a29ee56bbb89936a4cf77963d46c49222c2`) — the P1 delta commit. The P2 harness, evidence package, and this report are committed on top as one additive local commit (no push).
**Normative basis:** canonical-forms **v1**, interpreted through the approved addendum [`s01-canonical-form-v1-clarification-01.md`](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md) (authority: [`adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md)); the original contract and all P1 records remain byte-exact historical records.
**Final disposition:** **`S-01 P2 INDEPENDENT QUALIFICATION COMPLETE — SUPPORTED WITH QUALIFICATIONS`**.

> P2 independently re-derived the canonical expectations, re-executed the canonical-form and
> integrated paths through a reviewer-owned harness in four fresh processes, recomputed every
> derivable claim in the producer artifacts, and re-ran all regression gates. No blocking failure
> and no production contradiction was found. The disposition is **WITH QUALIFICATIONS**, not clean,
> because reviewer independence is limited by the same-session construction, the delta artifact
> does not record the reference identity list needed to recompute `compctx1` from the artifact
> alone, composed targets remain host-closure-scoped (O-1), the case set is finite, and several
> non-S-01 D3/D4/D5 variants remain carried. **S-01 is not marked satisfied**; P3 owner acceptance
> remains a separate, unauthorized gate.

---

## 1. Commission, authority, and scope

| Item | Content |
| --- | --- |
| Object qualified | The combined P1 proof, P1 clarification, and P1 delta evidence against `canonical-forms v1 (clarified)` |
| Permitted change surface | Additive reviewer artifacts only: `docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-tests/`, `docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/`, and this report |
| Forbidden changes | Production, tests, CI, contracts, owner decisions, prior evidence; push/fetch/reset/clean/rebase/branch/worktree operations; GitHub contact |
| Independence requirement | Re-derive expectations rather than trust producer `allPass`; producer oracle outputs may be compared only after independent derivation |
| Preserved qualifications | F-G2-01 `OPEN`; F-02 managed-fault boundary; D4.5/full S-02 as Stage 6; O-1 host-scoped targets; finite case set; all carried P1/delta qualifications |
| Not authorized | P3 owner acceptance, S-01 satisfaction, WP-S25-4, Stage-2 acceptance, remediation |

P2 assesses the S-01 proof chain only. It does not re-open the contract, does not reclassify the
recorded P1 R13 failures, and does not convert producer evidence into independent evidence by
repetition.

## 2. Verified repository preflight

Receipt: `preflight.log` (captured at run start).

| Check | Result |
| --- | --- |
| HEAD / tree | `976fe52a1fef1a69e3d68a7a4227a9337524274c` / `c0089a29ee56bbb89936a4cf77963d46c49222c2` — exactly the commissioned delta commit |
| Branch | `main` |
| Worktrees | Exactly one (`D:/projects/Andirit`); none created |
| Stashes | None |
| Tracked/index state | Clean apart from the 32 P2 additive paths present as untracked/modified at run start; no tracked production/test/CI/contract/prior-evidence file was modified |
| Local vs remote (recorded) | Local `origin/main` ref = `87097242d1610689c388d6814be06fef8cc89d89`; local `main` one commit ahead at start. No fetch, push, prune, remote query, or GitHub contact occurred |
| Reset/clean/restore/rebase | None performed |

The producer audit (below) proves that `src`, `tests`, and `.github` are unchanged from the P0
commit through the reviewed HEAD, so the P2 run exercised exactly the code state already covered
by P1 and the delta.

## 3. Independence model and reviewer limitations

**Recorded limitation (honest):** this reviewer is the **same assistant session** that produced the
P1 proof and the P1 delta. That is a genuine limitation on organizational independence and is not
argued away. P2's independence is therefore **methodological**, not organizational:

| Independence mechanism | Implementation |
| --- | --- |
| Fresh harness | Reviewer-owned project `P2Qual.csproj`; no P1/delta harness source is compiled or invoked |
| Fresh encoder | `Enc` implements its own 4-byte big-endian length prefixing, UTF-8 encoding, SHA-256, and lowercase hex; production digest APIs are called only for comparison |
| Fresh fixtures | Distinct fixture identities (`Confirm`, `Probe.Hit`, `Spec.Add`, `Generic.Pick`, `Ext.Twice`, `Util.Merge`, `Util.Pair`, `Util.Sum`, `IThing.Go`) not reused from P1/delta |
| Cross-toolchain anchors | The five known-answer digests were computed in Windows PowerShell **before** the C# harness was written and embedded as literals (`kat-powershell.log`) |
| Producer values not trusted | Producer `allPass` is recorded but never used as a pass condition; every check recomputes its own expected value |
| Artifact audit | `audit-artifacts.ps1` independently recomputes all derivable P1/delta claims from the recorded primitives; classified as an artifact audit, not fresh execution |
| Fresh processes | Part A, integrate, reopen, and recover ran in four distinct OS processes (PIDs 25400 / 23112 / 23884 / 12368) over one disposable native store |
| No production edits | Additive artifacts only; the full solution and frozen oracle are re-run as gates |

**What P2 does not claim:** organizational independence from the producer, independence of the
host reference closure, or universal coverage beyond the executed finite case set.

## 4. Normative inputs and byte-immutability audit

Receipt: `normative-inputs.log` and `producer-audit.log`.

| Input | Bytes | SHA-256 |
| --- | ---: | --- |
| `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md` (v1) | 12,636 | `5958dd6bb40da2d44c6845a65d7aee0e8e5c9c06966a4a29653c8dbb6c7f4852` |
| `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md` (addendum) | 7,982 | `1c3fd5e5e8db3011d422d8ab5c6d0753bf2698470bb2bce3e849686a53754c09` |
| `docs/decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md` | 28,823 | `341a4d1a39430319799182b335d14e05e3db8d2bf488e1b1dcb7b0e0569293b2` |
| `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md` | 37,212 | `1981a10a4f50f98b863c85a00fe6641413a7ebaa67b8943c6af15a70e6a457ab` |
| `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md` | 19,585 | `99e6531a25336d7d079e92085d29964002516742919680233f9d855ca3e06585` |

Preservation checks, all `True`:

- P1 evidence manifest 23 entries, 0 mismatches (`reviewedHead=349136db…`); P1 package unchanged since `90c5fbe`.
- Delta evidence manifest 17 entries, 0 mismatches (`reviewedHead=87097242…`); delta package unchanged since `976fe52`.
- v1 contract unchanged since `349136d`; clarification and owner decision unchanged since `8709724`.
- `8709724` is an ancestor of HEAD; `349136d` is an ancestor of `8709724`.
- `src`, `tests`, and `.github` unchanged since the P0 commit `349136d`.

## 5. Reviewer harness architecture

`docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-tests/` (not part of `Andirit.slnx`; `AssemblyName=Andirit.CSharp.Tests` for the established internals-access pattern, so P2 checks can never hide inside the inherited 907-test count):

| Artifact | Role |
| --- | --- |
| `Reviewer.cs` | Reviewer encoder `Enc`, five PowerShell-anchored KATs, eight fresh form cases, fifteen matrix rows, four R13 mode pairs with `compctx1`/composed-target reconstruction, coexistence, language boundaries, editorial checks |
| `ReviewIntegration.cs` | Part B verbs: `integrate` (S1–S5), `reopen` (S6–S7), `recover` (S8) over the real `LatticeCodeGraphStore` |
| `Program.cs` | Verb dispatch and JSON receipt writing |
| `run-p2.ps1` | Build → preflight → normative inputs → PowerShell anchors → producer audit → artifact audit → Part A → Part B fresh processes → toolchain → regressions → oracle → summaries |
| `audit-artifacts.ps1` | Recomputes all derivable P1/delta recorded primitives (encoding vectors, form digests, matrix relations, composed targets, descriptor/source isolation, language/editorial receipts) |
| `seal-evidence.ps1` | Self-excluding SHA-256 manifest for the P2 evidence package |

The disposable native store for Part B is `C:\Users\Laszlos\AppData\Local\Temp\p2-qual-90a3fe18.ltdb`, outside the repository and retained for inspection; it is left tamper-refused by the final D5 case.

## 6. Independent known-answer derivation

Receipts: `kat-powershell.log` (pre-harness anchors) and `Q1-P2A.json` (`encoder`).

| Anchor | Domain / field | PowerShell-derived digest | Reviewer | Production |
| --- | --- | --- | --- | --- |
| KAT-1 | `asmctx3-identifier` / `ReviewerPing` | `2184752575bbc9b65b8b85e52134904216bc39aeb9d60d9f7f19bd3c97bdebb9` | equal | equal |
| KAT-2 | `asmctx3-declarations` / `M:Review.Core.Spec.Confirm(System.Int32@)` | `a9df5dcd8f6c83641d7b24ec981d8974872f7915d56e2821b67bf2c485e72bbb` | equal | equal |
| KAT-3 | `asmctx3-target` / `method|1|T` | `af2c463835c11a3396c179709087b2b644584875af3955869f7713cb337d109d` | equal | encoding-only anchor (not invoked) |
| KAT-4 | `asmctx3-identifier` / `""` (empty) | `bfc94e29357152d9d7759cf169c04e8cfd5efb5f749b3e1c09fe76590f3952eb` | equal | encoding-only anchor; the production form layer rejects empty forms (P1 KAT-9 `ArgumentException`) |
| KAT-5 | `asmctx3-identifier` / Unicode `ИдентификаторΩ` | `45b29ca1c4e3a6efaf29ef99ad42a4214aa85f722abd2324ecd0b0ac12e3a1af` | equal | equal |

All five exact length-prefixed message byte strings are recorded in `Q1-P2A.json`. The anchors
establish domain separation, ordering/length semantics, UTF-8 handling, lowercase hex, and the
empty-component encoding/form-layer separation. KAT-3/KAT-4 are deliberately encoding-only: the
production shape/identifier form APIs are not invoked for synthetic or empty forms, so the
receipt does not overstate production agreement. The production cross-check for real shapes is
carried by the form and matrix receipts below.

## 7. Producer-artifact recomputation audit (P1 and delta)

Receipt: `Q5-ARTIFACT-AUDIT.json` — **190/190 checks, `allPass=true`**, exit 0. Classification:
producer-recorded primitives recomputed by the reviewer; this complements fresh execution and is
**not** a substitute for it.

**P1 historical artifact** (`Q1-ORACLE.json`):

| Group | Result |
| --- | --- |
| Known answers | 10/10 recomputed from recorded domain + fields (special shapes handled: two-domain KAT-7, ordered KAT-8, empty KAT-9, predicate-only KAT-10) |
| Form cases | 17/17 recomputed from recorded literal forms; FC-13 return-type invariance, FC-14 doc-ID observation, FC-DELIM collision challenge verified |
| Matrix | 18 recorded pairs: **15 pass / 3 fail** preserved; R13a/R13b/R13c remain historical failures against the original wording |

The audit does not convert the three R13 failures into passes. It confirms that the recorded
before/after digests imply exactly the recorded relations and that the failures are the original
shape-assertion failures.

**Delta artifact** (`Q1-DELTA.json`):

| Group | Result |
| --- | --- |
| Encoding vectors | 3/3 recomputed (declaration/ref and shape/int vectors record "not constrained" message hex; digest recomputation is authoritative) |
| Revision pairs | 4/4: expected digests recomputed from literal forms; declaration distinction checked; composed targets recomputed from recorded `compctx1` + metadata + module + component digests; exact message bytes recomputed; extracted fact target/digests checked; diagnostics clean |
| Descriptor/source isolation | 4/4: unchanged configuration fields equal A↔B, descriptor digests differ, one defining source each, same path/language, changed bytes, source hash consistent with file hash, changed-file set exactly `App/Consumer.cs` + `Core/Targets.cs`, targets differ |
| Coexistence | same-context identifier/shape equal, declaration distinct, targets distinct |
| Language boundaries | CS0663 for `ref`+`out` and `ref`+`in` |
| Editorial | doc-IDs exact and free of `global::`; `FullyQualifiedFormat` type display carries `global::`; special-type parameter display `string` |
| Recorded producer checks | 55/55 recorded, `allPass=true` — recorded only, not used as a P2 pass condition |

**Reference-identity limitation (recorded):** `Q1-DELTA.json` records `referencesCount=179` and
`referencesDigest=b8cdb1ce…` per side but **not** the 179 reference identity strings. Therefore the
delta `compctx1` cannot be recomputed from the artifact alone. Cross-side closure equality is
supported by identical count + digest (collision-resistant), and identity/order verification is
performed in the fresh reviewer run, whose own closure has **181** entries and digest
`4b9637df…`. The 179/181 difference is a host-closure difference, not a contradiction; the two
must not be conflated.

## 8. Canonical-form qualification (fresh reviewer fixtures)

Receipt: `Q1-P2A.json` (`forms`), 8/8 pass.

| Case | Expected identifier / declaration / shape | Result |
| --- | --- | --- |
| simple | `Confirm` / `M:Review.Core.Spec.Confirm(System.String)` / `method|0|string` | ✓ |
| overloads | `Add` / `M:Review.Core.Spec.Add(System.Int32)` / `method|0|int` | ✓ |
| generic | `Pick` / `` M:Review.Core.Generic.Pick``1(``0) `` / `method|1|T` | ✓ |
| extension | `Twice` / `M:Review.Core.Ext.Twice(System.Int32)` / `method|0|int` | ✓ |
| nested-generic | `Merge` / `M:Review.Core.Util.Merge(System.Collections.Generic.Dictionary{System.String,System.Int32})` / `method|0|global::System.Collections.Generic.Dictionary<string, int>` | ✓ |
| tuple | `Pair` / `M:Review.Core.Util.Pair(System.ValueTuple{System.Int32,System.String})` / `method|0|(int, string)` | ✓ |
| array | `Sum` / `M:Review.Core.Util.Sum(System.Int32[])` / `method|0|int[]` | ✓ |
| explicit-interface | implicit and explicit dispatch both target `asmctx3:sha256:0ab72b5c834227d3658d8ef6ba29f771a08040d7b7bc257e7bb54f434dcadf34`; explicit implementation doc-ID `M:Review.Core.ExplicitThing.Review#Core#IThing#Go(System.String)` | ✓ |

Each entry records the reviewer-computed digests, the extracted fact digests, and the actual
target. The explicit-interface case confirms that both dispatch styles bind the interface member
and that the implementation symbol is not an occurrence target (P0 observation preserved).

## 9. Normative matrix under v1 (clarified)

Receipt: `Q1-P2A.json` (`matrix`), 15/15 pass. R13 is assessed separately in §10 (four mode pairs,
32 checks) because the clarified row 13 carries a multi-part expectation.

| Row | Transformation | Expected id/decl/shape | Observed | Result |
| --- | --- | --- | --- | --- |
| R1 | whitespace/formatting | = / = / = | = / = / = | ✓ |
| R2 | comments/trivia | = / = / = | = / = / = | ✓ |
| R3 | declaration relocation (`Core/Targets.cs` → `Core/Deep/Targets.cs`) | = / = / = | = / = / = | ✓ |
| R4 | namespace-qualification style | = / = / = | = / = / = | ✓ |
| R5 | type alias | = / = / = | = / = / = | ✓ |
| R6a | overload addition (existing target) | = / = / = | = / = / = | ✓ |
| R6b | retargeted overload | = / ≠ / ≠ | = / ≠ / ≠ | ✓ |
| R7 | rename | ≠ / ≠ / = | ≠ / ≠ / = | ✓ |
| R8 | parameter-type change | = / ≠ / ≠ | = / ≠ / ≠ | ✓ |
| R9 | parameter-order change | = / ≠ / ≠ | = / ≠ / ≠ | ✓ |
| R10 | generic arity change | = / ≠ / ≠ | = / ≠ / ≠ | ✓ |
| R11 | generic substitution | = / = / = | = / = / = | ✓ |
| R12 | extension reduction (both call forms) | = / = / = | = / = / = | ✓ |
| R14 | same simple name, different owner | = / ≠ / = | = / ≠ / = | ✓ |
| R15 | unresolved/external refusal | refusal | refusal (control fact present) | ✓ |

R3 genuinely relocates the defining file to a different directory while preserving the logical
source; R11 uses a constructed generic substitution pair; R5 uses a real namespace alias to a
type (`using F = Review.Core.Fmt;`) rather than an invalid member alias. R15 emits no fact for
`System.Console.WriteLine` while the in-corpus control fact is present.

## 10. R13 mode pairs, defining context, and composed targets

Receipt: `Q1-P2A.json` (`r13`), 4 pairs × 8 checks = 32/32 pass. Each pair re-extracts fresh
fixtures in two sides, computes expected identifier/declaration/shape digests with `Enc`, and
reconstructs `compctx1` and the composed target from the recorded descriptor fields.

| Pair | Declaration distinguishes | Component relations (id/decl/shape) | `compctx1` A → B | Composed target A → B | Result |
| --- | --- | --- | --- | --- | --- |
| value vs ref | yes | = / ≠ / = | `6fefb305…` → `e42ff5f4…` | `2e4cef53…` → `d09d744c…` | ✓ |
| ref vs out | no | = / = / = | `a21530e4…` → `83614969…` | `f56aa218…` → `0ca5278f…` | ✓ |
| ref vs in | no | = / = / = | `4c4a97c5…` → `edf4a61a…` | `0d0ae0a5…` → `8159ff46…` | ✓ |
| out vs in | no | = / = / = | `9be41029…` → `a51f12e3…` | `442d749d…` → `a25fdad8…` | ✓ |

For every side: the reviewer `compctx1` reconstruction matches production and the frozen R2
recomputation; the reference closure is 181 ordered entries, digest `4b9637df…`; the composed
target matches both production and the reviewer recomputation; the exact 7-part length-prefixed
message bytes are recorded; and every extracted occurrence fact's target equals the composed
target. The mode-family pairs demonstrate the clarified guarantee precisely: identical component
digests with different defining contexts still produce different composed targets (context alone),
while the value-vs-ref pair additionally distinguishes the declaration. No target is claimed
invariant across differently-built hosts (O-1).

## 11. Coexistence, language boundaries, and editorial observations

Receipt: `Q1-P2A.json` (`coexistence`, `languageBoundaries`, `editorial`).

- **Coexistence (4/4):** one compilation declares `Probe.Ping(int)` and `Probe.Ping(ref int)`.
  Both calls are facts in the same context `compctx1:sha256:25901398…`; identifier digests are equal
  (`bf8cb43b…`), declaration digests differ (`40136edc…` vs `c8d02386…`), shape digests are equal
  (`928f040e…`), and the composed targets differ (`f8998daf…` vs `bb6c05ed…`).
- **Language boundaries (2/2):** modifier-only overload sets are not declarable; `ref`+`out` and
  `ref`+`in` both produce exactly one `CS0663` with the expected message. This is the language-level
  reason the mode family cannot collide within one compilation.
- **Editorial (4/4):** `IThing.Go(string)` doc-ID is `M:Review.Core.IThing.Go(System.String)` and
  contains no `global::`; the type display under `FullyQualifiedFormat` is
  `global::Review.Core.IThing`; the parameter display is the special-type keyword `string`. These
  corroborate E-1/E-4 and the doc-ID-first/display-fallback distinction.

No universal collision-freedom claim is made. The executed set is finite; no concrete supported
counterexample was found.

## 12. Integrated source-to-store receipts (S1–S5)

Receipt: `Q2-INT.json` (fresh process PID 23112; exit 0). The verb publishes the accepted frozen
R2 publishable subset through the real extraction → batch/sink → stage/complete path into the
disposable native store.

| Observation | Result |
| --- | --- |
| Publication run | `run:review` |
| Batches | 20 |
| Authority envelopes | 4 |
| Native rows | `AndiritOccurrenceMeta` 1, `AndiritOccurrencePage` 20, `AndiritOccurrenceUnit` 4, `HetuUnit` 4 |
| Expected targets present in unit payloads | `Add` true, `Greet` true |
| `Add` expected forms | `Add` / `M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` / `method|0|int,int` |
| `Add` digests | id `cf6ce097…`, decl `af62216e…`, shape `4bb442d7…` |
| `Add` target | production `asmctx3:sha256:f51f0fbd…` = independent `asmctx3:sha256:f51f0fbd…` (`agree=true`) |
| `Greet` target | production `asmctx3:sha256:0d92fc2d…` = independent `asmctx3:sha256:0d92fc2d…` (`agree=true`) |

This establishes that the reviewer's independent composition agrees with the real store's persisted
target for both a two-parameter method and the explicit-interface dispatch scenario, and that the
native occurrence tables and graph mirror rows are populated.

## 13. Fresh-process reopen and availability receipts (S6–S7)

Receipt: `Q3-REOPEN.json` (fresh process PID 23884; exit 0) reopening the store written by PID 23112.

| Query | Observed |
| --- | --- |
| `Add` | `Available`, complete, 8 facts, totalMatches 8, examined 8, all targets match, publication `run:review`; spans `App/Bulk.cs:20:30` … `:24:30` |
| `Greet` | `Available`, complete, 2 facts, totalMatches 2, examined 2, all targets match, publication `run:review`; spans `App/Sites.cs:26:30`, `App/Sites.cs:30:38` |
| Stale publication handle | `StalePublication`, 0 facts |
| Missing verified-compilation authority | `AnalysisUnavailable`, 0 facts, reason "Current verified compilation authority is required." |
| Invalid canonical target construction | refused (`ArgumentException`) |

The reopened process returns the same publication and the same ordered spans as the integrating
process, with no false-available or false-complete result. The `examined` counts equal the fact
counts for these target pages, consistent with the no-unbounded-scan requirement at this scope.

## 14. Managed recovery and refusal receipts (S8)

Receipt: `Q4-RECOVERY.json` (fresh process PID 12368; exit 0). **F-02 boundary: managed fault
injection is not crash or power-loss durability evidence.**

| Phase | Observed |
| --- | --- |
| Baseline | `Available`, 8 facts, publication `run:review` |
| Known failure (before-commit fault) | fault observed (`ReviewFault`); prior publication preserved and still `Available`, 8 facts, `run:review` |
| Unknown outcome (after-commit fault) | `CodeGraphCommitOutcomeUnknownException`; quarantined reads and mutations both throw the same exception |
| Fresh-open reconciliation | `Available`, 8 facts, publication `run:review3`; ordered spans preserved |
| Tampered occurrence page (publication binding altered) | fresh open refused with `CodeOccurrenceIndexException` |

This confirms the known-failure/unknown-outcome distinction, quarantine behavior, reconcile-on-open,
and fail-closed D5 page-binding refusal. It does not claim crash consistency.

## 15. D3/D4/D5 qualification matrix and requirement traceability

The S-01 commission requires applicable D3/D4/D5 behavior, not a re-qualification of every Stage-2
obligation. The matrix below distinguishes what P2 freshly exercised from what remains carried.

| Area | P2 evidence | Status |
| --- | --- | --- |
| **D3** publication/authority binding | 4 authority envelopes; 20 batches; unit/meta/page rows; both targets persisted; queries `Available` only with the current publication; stale handle → `StalePublication`; missing authority → `AnalysisUnavailable` | Exercised (applicable S-01 subset) |
| **D3** legacy graph noninterference (node/edge/count/traversal before-after) | Not re-run in P2; inherited accepted evidence and the 52 green LatticeDb tests; no production change | Carried (not claimed) |
| **D4.1** accepted generation only | stale handle → `StalePublication` | Exercised |
| **D4.2** stale index refused | stale → `StalePublication`; missing authority → `AnalysisUnavailable` | Exercised |
| **D4.3** no unbounded scan | `examined` equals target fact count (8/8, 2/2); all targets match | Exercised at this scope |
| **D4.4** failed staging preserves prior publication | known fault preserves `Available/8`; unknown outcome quarantines; fresh open reconciles `run:review3` | Exercised (managed-fault scope, F-02) |
| **D4.5** cancelled staging | Deferred to Stage 6 | **Not exercised** |
| **D4.6–D4.10** superseded, contributor/deletion, order/limit, concurrency | Not re-run in P2 | Carried (not claimed) |
| **D5.3** unsupported schema refused | Not re-run in P2 (tamper targeted page binding, not schema version/namespace); inherited tests green | Carried (not claimed) |
| **D5.4** page/publication mismatch | tampered page → `CodeOccurrenceIndexException` on open | Exercised |
| **D5.5** stale query refused | `StalePublication` with explicit availability | Exercised |
| **D5.1/D5.2** graph-only open/query | Not isolated in P2; `AnalysisUnavailable` path exercised via missing authority | Partially exercised / carried |
| **D5.6** no migration/dual-write | No production/test/CI change (producer audit); no new codec, migration, or fallback introduced | Preserved by construction |
| **Full S-02 / D4 native matrix** | Stage 6 | **Not claimed** |

**Requirement traceability (commission → evidence):**

| Requirement | Evidence |
| --- | --- |
| Independently re-derive expectations | §6 PowerShell anchors; §8–§11 reviewer encoder; §10 reconstructed `compctx1`/targets |
| Do not trust producer `allPass` | §7 records producer 55/55 but every P2 check recomputes; producer values compared only after derivation |
| Re-verify P1/delta artifact claims | §7, 190/190 recomputations; P1 15/3 historical result preserved |
| Exercise the real source-to-store path | §12–§14, four fresh processes over one native store |
| Preserve prior evidence and normative text | §4 byte-immutability audit, all `True` |
| Regression floors | §16: 907/0/0, oracle byte-identical, format clean, Release 0/0 |
| Evidence integrity | §17: 26-entry self-excluding manifest, 0 mismatches |
| Preserve qualifications | §18: F-G2-01, F-02, D4.5/S-02, O-1, finite set, reference-identity limitation |

## 16. Regression gates and toolchain provenance

Receipts: `release-build.log`, `full-solution-tests.log`, `focused-historical-tests.log`,
`oracle.log`, `format.log`, `toolchain.log`.

| Gate | Result |
| --- | --- |
| Reviewer harness build | exit 0 |
| Release build (`dotnet build Andirit.slnx -c Release`) | 0 warnings, 0 errors, exit 0 |
| Full solution tests (`dotnet test Andirit.slnx -c Release --no-build`) | **907 passed, 0 failed, 0 skipped** — LatticeDb 52, TreeSitter 157, Andirit.Tests 345, Andirit.CSharp 353 |
| Focused historical/refusal tests (`Store_Rejects|Refusal`) | 8 passed, 0 failed, exit 0 |
| Frozen R2 oracle | **146,851 bytes**, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`, byte-identical to the recorded expectation |
| Formatting (`dotnet format Andirit.slnx --verify-no-changes`) | exit 0 |
| Toolchain | .NET SDK 10.0.401, host 10.0.12, Windows 10.0.26200 x64; Roslyn `Microsoft.CodeAnalysis.CSharp` 5.9.0 via the unchanged production project reference |

P2 case counts are separate from the inherited 907 tests. No CI change, coverage remediation, or
prior-package reseal occurred. F-G2-01 remains `OPEN — REPOSITORY CI BRANCH-COVERAGE GATE FAILURE`
(296/450 = 65.77% < 70%) and is untouched by P2.

## 17. Evidence integrity and provenance classification

- **Package:** `docs/reviews/CI1A-S2.5-WP-S25-3-P2-QUAL-evidence/` — preflight, normative inputs, PowerShell anchors, producer audit, artifact audit JSON/log, Part A JSON/log, integrate/reopen/recover JSON/logs, toolchain, release build, full/focused tests, oracle log + artifact, format, receipt summary, reviewed-head, `.gitattributes`, `.gitignore`.
- **Manifest:** **26 entries, 0 mismatches**; self-excluding SHA-256 over exact bytes; complete coverage of the evidence directory (no unlisted/extra files); `reviewedHead=976fe52a…`, `reviewedTree=c0089a29…`, branch `main`. Re-verified independently after sealing.
- **Exact-byte policy:** evidence `.gitattributes` uses `* -text`; raw logs remain unnormalized; JSON receipts are written without BOM.
- **Provenance categories:**

| Category | Artifacts |
| --- | --- |
| Original normative contract (historical) | v1 contract, byte-unchanged |
| Approved prospective clarification | addendum + owner decision, byte-unchanged |
| Historical producer evidence (unchanged) | P1 package (23/0), delta package (17/0) |
| Fresh P2 reviewer execution | Part A, integrate, reopen, recover, PowerShell anchors, artifact audit |
| Independent expected values | PowerShell KAT literals, reviewer `Enc`, contract formulas, frozen R2 authority primitives |
| Production actual values | extraction digests, mapped descriptors, `ComputeTargetId`, native store rows |
| Inherited regression evidence | full solution tests, focused tests, frozen oracle artifact |

No producer-created value is presented as independent evidence. The artifact audit is explicitly
classified as recomputation over producer-recorded primitives, not independent execution.

**Local vs remote / push status:** no fetch, push, prune, remote query, or GitHub contact occurred.
At start, local `main` was one commit ahead of the recorded local `origin/main` ref (`8709724…`).
This additive P2 commit makes local `main` two commits ahead of that ref; it remains **unpushed**.

## 18. Findings, qualifications, formal disposition, and P3 recommendation

**Findings:** no blocking finding; no production contradiction; no mandatory P2 check failed; no
evidence-integrity failure; nothing was excluded to obtain agreement.

**Qualifications (all recorded, none waived):**

- **Q-P2-01 — reviewer independence is methodological, not organizational.** The reviewer is the
  same assistant session as the P1/delta producer. Fresh harness, fresh fixtures, PowerShell-derived
  KATs, and independent recomputation mitigate but do not eliminate this limitation.
- **Q-P2-02 — delta reference-identity limitation.** The delta artifact records reference count and
  digest but not the 179 identity strings, so its `compctx1` cannot be recomputed from the artifact
  alone. Cross-side equality rests on count + digest; the fresh reviewer run reconstructs its own
  181-entry closure and matches production/frozen. The 179/181 difference is host-scoped and must
  not be conflated.
- **Q-P2-03 — O-1 preserved.** Composed targets bind the host build closure; P2 compares component
  digests and relational claims within a run, not absolute target ids across differently-built hosts.
- **Q-P2-04 — finite case set.** The executed forms/matrix/coexistence set is bounded; no universal
  collision-freedom or universal C# symbol-uniqueness claim is made.
- **Q-P2-05 — carried boundaries.** D4.5 and the full S-02 matrix remain Stage 6; managed fault
  injection is not crash/power-loss evidence (F-02); F-G2-01 remains `OPEN` (296/450 = 65.77% < 70%).
- **Q-P2-06 — carried D3/D5 variants.** Legacy graph noninterference, schema-version/namespace
  refusal, graph-only isolation, and the non-applicable D4 invariants were not re-run in P2 and are
  not claimed; they remain covered by earlier accepted evidence or later gates as recorded in §15.
- **Observation — encoding-only anchors.** KAT-3/KAT-4 do not invoke production; real-shape
  production agreement is established by the form and matrix receipts. The empty form-layer
  rejection remains the P1 KAT-9 record.
- **Observation — PowerShell Unicode rendering.** The anchor log's console rendering of the Unicode
  field may be visually degraded; the recorded digest anchor is authoritative and was computed from
  explicit code points before the harness was written.

**Formal disposition:** **`S-01 P2 INDEPENDENT QUALIFICATION COMPLETE — SUPPORTED WITH QUALIFICATIONS`.**

Rationale: (1) all 70 fresh reviewer checks pass; (2) all 190 artifact recomputations pass,
including preservation of the P1 15/3 historical matrix and full delta pair recomputation; (3) the
four R13 mode pairs satisfy the clarified E-3 expectations, including context-alone target
distinction; (4) the real source-to-native-store path, fresh-process reopen, and managed recovery
all produce the expected availability, authority, and refusal behavior; (5) applicable D3/D4/D5
obligations pass at the recorded scope with carried items explicitly marked; (6) all regression
floors are intact; (7) the evidence package is sealed and reproducible. The qualifications above
prevent a clean `SUPPORTED` disposition.

**P3 recommendation:** proceed to a **separate P3 owner-acceptance decision** for S-01 against
`canonical-forms v1 (clarified)`, presenting this P2 report, the sealed P2 evidence, the unchanged
P1/delta packages, and the qualifications Q-P2-01…Q-P2-06. No remediation is identified. If the
owner requires organizational independence or artifact-only `compctx1` recomputation, those are
separate follow-up commissions, not P2 findings of nonconformance.

**S-01 is not marked satisfied by P2.** P3, WP-S25-4, Stage-2 acceptance, any production/contract/CI
change, and any push remain unauthorized and unperformed.
