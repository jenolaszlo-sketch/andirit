# EC6D-SCOPE-Q3 — Independent focused contract verification

Final assessment: 2026-10-28 (Asia/Manila). Repository:
`jenolaszlo-sketch/penghou-hetu`. **Review only; no production
implementation, no integration, no rename.** This is the focused amendment
verification of EC6D-PLAN-R2 against the reconciled Q2 register
(Q2-01..Q2-08) and its N01–N09 crosswalk. Evidence:
[ec6d-scope-q3-evidence/](ec6d-scope-q3-evidence/README.md).

## A. Git binding

| Role | Commit | Tree | Direct parent |
| --- | --- | --- | --- |
| **Reviewed R2** | `3ce7dd93c105e7e7375a04edb083be3a5d97f672` | `a10b91d10523515de9c038b9fa2e8cc51000c19d` | `0e1b213737d42c74df20aef4744ab5718058e266` |
| R1 planning | `0e1b213737d42c74df20aef4744ab5718058e266` | `ddf777aa155852daca40b6ed8748c028c67d5822` | `91a3ed53a256e89ed71d7d11c204252c417400a6` |
| Recovered original planning | `91a3ed53a256e89ed71d7d11c204252c417400a6` | `d79e5814485040a36c77bcde332ebc937f312476` | `3b5e8d91e89e082fd236452656c1d66a4afd3662` |
| EC-6C owner acceptance | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | `ece772f1f2b2497891f4019d0d85f4d852b197e1` | `5e79418f8b3864e8b829a4be10ab62c6b5744d89` |
| Canonical main | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | `08a01a1b7d5d3109b991bcd05d57272fd3f5f018` | `0da1f56d3afc4448fb04e33a6aa84ebff3513599` |

Ancestry (`git merge-base --is-ancestor`, all exit 0): R1→R2, recovered→R1,
EC-6C→recovered, EC-6C→R2, canonical main→R2. The R2 object exists in the
planning mirror where the planning worktrees live; the canonical clone's
separate object database does not imply these planning commits.

**Isolation and environment.** Review branch
`review/hetu-ec6d-scope-q3`, worktree
`C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-scope-q3`, created directly at
the R2 commit; clean starting state; `git fsck --connectivity-only` exit 0
(dangling blobs only, the same class of residue recorded by Q2); 71.7 GB disk
free. All existing worktrees and refs were left untouched: refs
`planning/hetu-ec6d-plan-r2` = `3ce7dd9…`, `planning/hetu-ec6d-plan-r1` =
`0e1b213…`, `planning/hetu-ec6d-consumer-intelligence` = `91a3ed5…`,
`review/hetu-ec6d-scope-q1` = `d2bb7b1…`,
`review/hetu-ec6d-scope-q2` = `1bfcc84…`, `main` = `558f8ac…`; the four
planning worktrees were clean at those commits before and after this review.

**R2 change surface (verified, matches the reported claim):** 12 modified +
6 added paths, all under `docs/planning/ec6d/`; `git diff --name-only
0e1b213 3ce7dd9 -- src tests docs/reviews` is empty; `git diff --check`
clean. Full commands and outputs: `ec6d-scope-q3-evidence/git-verification.txt`.
One in-packet discrepancy found: `evidence-index.md` reports "ten modified
and five added" and omits `README.md`/`evidence-index.md` from its list —
see new finding Q3-F01.

**Q2 supersession (verified, see §C).** Original handoff
`6e34452(55ea074, parent 5757120)` → reconciled content
`6630624(5c4447b4…, parent 6e34452)` → binding attestation HEAD
`1bfcc84(a34154a…, parent 6630624)`. The reconciled report blob
(`ff2d18826a350c809785a8d24c5355abbfb36f38`) was extracted byte-identical
from Git; the attestation commit's recorded binding matches exactly.

**Final review binding.** This report and its evidence are the Q3 content
commit; a binding-only follow-up commit records the exact content
commit/tree/parent and the resulting HEAD. Resolve the final review
commit/tree/parent with:

```text
git show -s --format="%H%n%T%n%P" review/hetu-ec6d-scope-q3
```

## B. Final verdict

**ACCEPT WITH QUALIFICATIONS.**

All Q2 blockers (Q2-01..Q2-07 correctness; Q2-08 documentation) are resolved
in contract by R2, independently verified below; no new P0/P1 finding
remains; the supported binding rules prevent the known false confirmations
while preserving the four canonical positive implementations; query,
unknown, coverage and publication semantics are coherent; every audited
acceptance row is satisfiable; and the three owner decisions are prepared.
The qualifications are five bounded, non-blocking items (Q3-F01..Q3-F05,
all P3) plus the inherited limitations in §L. None conceals an unresolved
correctness semantic. Verdict is for **owner disposition only** — not
implementation, integration or release authorization.

## C. Q2 provenance reconciliation

**Supersession authenticated.** The reconciled Q2 review is a real,
committed, self-attesting supersede of the original Q2 handoff:

| Item | Identity | Verification |
| --- | --- | --- |
| Original Q2 handoff (referenced by the original review) | `6e34452` / tree `55ea0741…` / parent `5757120` | object present; ancestry to reconciled review exit 0 |
| Reconciled review content | `6630624` / tree `5c4447b43c19133fc912ad1402f34405541a74fe` / parent `6e34452` | matches R2's quoted prefix `5c4447b4…`; report blob hash `ff2d188…` reproduced byte-for-byte |
| Binding attestation HEAD | `1bfcc84` / tree `a34154a…` / parent `6630624` | attestation diff read: records content commit/tree/parent exactly; ancestry exit 0 |

The reconciled register appendix (report §K) states its own crosswalk
("N01/N02 → Q2-01; N03 → Q2-06/07; N04 → Q2-02/03/07; N05 → Q2-03; N06/N07 →
Q2-04; N08 → Q2-08; N09 → Q2-05") and the R2 disposition document reproduces
the same mapping. I verified both against the original §K register (Q2-N01..N09) as committed
at `6e34452` and against the R2 remediation text — not merely identifier to
identifier.

**Original-to-reconciled survival check.**

| Original | Original requirement (abridged) | Surfaced in | Survival evidence |
| --- | --- | --- | --- |
| N01 (P1) | Scope/shadowing guard; semantically-invalid boundary; acceptance rows | Q2-01 | Lexical-binding rule §3.1/§3.2; A05/A21/A23; oracle re-run (nested-shadow valid, binds local) |
| N02 (P2) | Duplicate local names fail closed; no ordering selection; Duplicate row | Q2-01 (+cardinality), Q2-02 | §3.2 gate 10; A23; duplicate-imports TS2300 re-run |
| N03 (P2) | Barrier = export-form vocabulary; consumers = import/import-type; A08/A10 | Q2-06, Q2-07 | §3.4 fact-E predicate; §3.2 possible-consumer form filter; A10/A10b/A27; vocabulary verified in `docs/ecmascript-module-resolution.md:66` |
| N04 (P2) | (i) A04 reason; (ii) A10 unsatisfiable; (iii) A12 fixture; (iv) A18 provider-failure gap; (v) missing rows; (vi) A15 "identical" | Q2-02/03/07 | A04 corrected to `unresolved-heritage-name` + A04b; A10 rebuilt on realizable fixtures incl. BarrelPreview; A12 with heritage clauses + inline-type positive; A18/A18b; A21–A28 added; A15 says "identical" |
| N05 (P2) | Qualified heritage not evaluated; counter; acceptance row | Q2-03 | Fact C stores qualified/expression/extends contexts; reasons + coverage counters; A25; p9 `HeritageName` source confirms the drop |
| N06 (P3) | p8/p9 evidence citation | Q2-04 | §5 cites `0.2.0-preview.6+v0.23.2.abi14.p8` with retention rationale; source comment verified in `EcmaScriptExtractionProfile.cs` |
| N07 (P3) | `ambiguous-export` reachability; multi-occurrence span identity | Q2-04, Q2-07 | Attribution statuses make `ambiguous-export` reachable (§2.2/§2.3, A24); `exportSpans[]` list with occurrence witnesses (§2.1); reserved reasons labeled |
| N08 (P3) | Mixed-line-ending wording; seed-node count; Q1-harness attribution; (optionally SnapshotIntegrityHash) | Q2-08 | All four corrected in place with "(R2 correction)" markers; historical outputs untouched; verified in diff |
| N09 (P3) | `true`+null must not derive consistent | Q2-05 | Mechanical truth table; source confirms the constructor permits true/null; A17 |

No original finding was omitted, weakened, or silently closed: severities
were escalated where the reconciliation found stronger evidence (N02/N04/N05
folded into P1 rows Q2-01/02/03), and the N07 specifics — which the
reconciled register folds into broader Q2-04/Q2-07 wording — demonstrably
survive in the R2 contract text (`exportSpans[]`, reachable
`ambiguous-export`, reserved-reason labels). The reconciled register's own
status ("All eight remain open; three P1, no P0") is consistent with what
R2 then addressed.

## D. Q2-01 through Q2-08 dispositions

| Finding | Sev | Original defect | R2 correction | Independent evidence | Remaining limitation | Disposition |
| --- | --- | --- | --- | --- | --- | --- |
| Q2-01 | P1 | Import-first precedence confirmed imported declarations under valid shadowing; A05 demanded a conflicting positive | Import-first removed; §3.1/§3.2 gate sequence (context, recovered, ambient/script, nested, kind, form, type-parameter, ineligible import forms, duplicates, conflicts) with explicit reasons; A05/A21/A22/A23 | §E below: oracle re-run reproduces TS 5.9.3 semantics for nested-shadow, collision, type-param, duplicate-imports; contract gate trace confirms refusal; positives (unshadowed alias, nested-unrelated, same-file) remain achievable | Mixed eligible/ineligible duplicate precedence deterministically yields `unsupported-import-form` rather than `duplicate-local-name` (non-confirming; observation only) | **RESOLVED** |
| Q2-02 | P1 | Node-count uniqueness hid physical multiplicity/visibility (merged, nested-export, type-value) | Physical declaration records counted at module scope before dedup (§2.2); statuses + all contributor ids; merged/type-value → `ambiguous-*`; nested never candidates; same-file short proof | p9 evidence re-read: merged = one node + two occurrence edges + two physical declarations; type-value = distinct interface/field nodes; audit check passes; A03/A22/A24 traces | Merged/type-value refuse confirmation (documented conservative exclusion, not general merge support) | **RESOLVED** |
| Q2-03 | P1 | Discovery missed export aliases, the A10 barrel and qualified heritage; unsupported sites disappeared | Entry-driven A0→A1→A2 discovery; facts C/E; occurrence-level unknowns; BarrelPreview → `re-export-barrier`; not-evaluated frontier; A02/A10/A10b/A25/A27 | Corpus trace: api/index.ts re-export record reaches `Drawable` into A2; p9 barrel evidence shows no entry for re-exports; oracle re-run validates barrier fixtures while binding correctly in TS | One-hop alias association may over-report relevance (adds unknowns only — verified: confirmation still requires the full chain to the exact declaration) | **RESOLVED** |
| Q2-04 | P2 | Span/identity/proof/lifecycle contradictions; wrong `.p9` citation; unstored join vs stored-step requirement | Statement/module/binding spans; exact identity inputs; resolver join/recompute key; computed `binding1.local-v1` witness; same-file short proof; `…abi14.p8` citation; exportSpans[] | §F: source-grounded checks of heritage extraction, p8 retention comment, resolver vocabulary; witness tuple contents; A13 (see Q3-F03/F05 for forward precision) | Computed-vs-persisted is ADR-1 (correctness obligations fixed either way); witness is a proposed feature, not implemented | **RESOLVED** |
| Q2-05 | P2 | Incomplete publication identity; true/null derivable as consistent; qualification escapable; integrity unknown | Full identity block; mechanical truth table; integrity-failure list; checked consistent-only path; index-failure retention; A17/A18/A18b | §G: truth table read line-by-line; `CodeGraphPublication` constructor permits true/null (source); `EnsurePublication` full-record equality (source); serialization/authority-path tests planned | Design choice qualified vs fail-closed is ADR-2 (rules fixed either way) | **RESOLVED** |
| Q2-06 | P2 | "Outgoing resolves-to" mislabeled ordinary importers as barriers; unresolved export sources invisible | Fact-E predicate over the four export forms; ordinary imports → `missing-export`; unresolved sources stored/counted; no closure; separate witnesses | p9 evidence: ordinary.ts has only import-side references; named/star/missing/multi records retained; vocabulary verified against source and docs | Multi-hop/unresolved-origin barriers beyond the frontier are disclosed, not resolved (declared) | **RESOLVED** |
| Q2-07 | P2 | Category conflict; candidate/unsupported loss; ordering/budget gaps; A04/A12/A18 contradictions; missing fixtures | Closed 20-reason table + 2 reserved; independent categories with per-relationship exclusivity; nested total order; charged-work budgets vs caps; corrected A04/A10/A12/A18; A04b/A10b/A18b/A21–A28; R2 fixtures | §H audits every corrected/new row; oracle re-run validates fixture semantics; reason reachability traced condition-by-condition | Two reason-table text precisions (Q3-F02) and merged-target/A24 pinning (Q3-F04), both P3 | **RESOLVED WITH QUALIFICATIONS** |
| Q2-08 | P3 | Pure-LF wording; 0-nodes/0-edges; Q1-harness attribution; SnapshotIntegrityHash not noted | In-place marked corrections in `recovery-and-provenance.md` §6 and `consumer-baseline.md`; historical outputs/commits preserved | Diff read: all four corrections present with "(R2 correction)"/"(R2 completeness note)" markers; original raw evidence untouched | None; the evidence-index count error is a new R2-era item (Q3-F01), not a Q2-08 regression | **RESOLVED** |

Crosswalk closure: N01→Q2-01; N02→Q2-01+Q2-02; N03→Q2-06+Q2-07;
N04→Q2-02/03/07; N05→Q2-03; N06→Q2-04; N07→Q2-04+Q2-07; N08→Q2-08;
N09→Q2-05. Every original requirement is dispositioned above; none remains
open.

## E. Lexical-binding assessment

**Gate necessity/sufficiency (declared profile).** For the supported shape —
a heritage occurrence with `heritageKind = class-implements`,
`writtenForm = simple`, `ownerScope = module`, `ownerAmbient = false`,
`fileModuleContext = module`, `clauseSyntaxStatus = well-formed`, no owner
type parameter named `H`, and exactly one eligible visible binding — the
gates are *necessary* (each condition excludes a concrete wrong-binding or
unverifiable shape) and *sufficient* (after the gates, the import route
requires a complete stored chain terminating in the queried interface's
unique physical declaration record and node; the same-file route requires the
unique module-scope declaration to be that node). No step uses global or
file-level name matching; the only name-based joins are discovery relevance
(A2, over-reporting allowed) and the module-local imported-name → export-name
lookup inside a unique resolved target, which is a structural
export-surface lookup, not identity inference.

**Adversarial cases (independent TypeScript 5.9.3 oracle re-run; full
results in `q3-oracle-run.json`, recorded-run comparison 23/23 and 14/14 with
0 diffs):**

| Case | Oracle (independent) | Contract (traced) | Result |
| --- | --- | --- | --- |
| nested-shadow (Q2 shape and Q3 variant) | valid; binds function/namespace-scope local interface | gate 4 `nested-scope-heritage`; never the import, never the local as target | no false confirmation |
| nested-unrelated | valid; binds imported target | import route confirmed; nested same-name excluded from candidacy (§3.3) | positive preserved |
| type-value | valid; binds interface **and** value declaration | attribution counts two physical records → `ambiguous-declarations` → `ambiguous-export`; same-file → `ambiguous-local-declaration` | conservative refusal, no namespace conflation |
| merged | valid; binds two interface declarations | two physical records → `ambiguous-declarations` | conservative refusal |
| same-file | valid; binds same-file interface | §3.5 short proof; interface need not be exported | positive preserved |
| unshadowed alias (export-alias shape; also Q3 variant) | valid; binds the aliased target | import route via attributed public entry | positive preserved |
| type-param boundary | TS2422; binds the type parameter | gate 8 `type-parameter-shadowing` | declaration invalid in TS → refused |
| collision | TS2440/2420; TS still associates the import when forced | gate 11 `conflicting-local-binding` | invalid source → refused, never confirmed either way |
| duplicate-imports | TS2300; first import associated when forced | gate 10 `duplicate-local-name` | invalid source → refused, no first-wins |
| block-scope clause (Q3) | TS1184 fixture; binds the import | ownerScope=nested → refused | conservative false negative, declared boundary |
| local-namespace-qualified (Q3) | valid; binds the local namespace member | `qualified-heritage-unresolved` (terminal identifier target-associated) | unrelated local never confirmed; stays observable |

**Positive-capability guard (A07, contract-level feasibility).** The
canonical corpus's four implementations trace through the supported profile:
`SvgRenderer`/`HtmlRenderer` (statement type-only named import →
resolved → entry → attributed → node = target), `AbstractRenderer`
(same, abstract class is still a class clause), `AliasRenderer`
(named-aliased import; candidate linked through the occurrence's local name
per §3.3, and in this corpus also via the barrel alias in A2). Negatives:
`OtherRenderer` completes to a different declaration → omitted;
`CanvasRenderer` (extends) is not discovered; `BarrelPreview` is an
occurrence-level `re-export-barrier`; `MissingImport` is `unresolved-module`.
No false-confirmation path was found that reaches the confirmed category
without a complete witness chain to the queried declaration.

**Observation (non-blocking).** For a mixed eligible+ineligible duplicate
local name, the deterministic gate order emits `unsupported-import-form`
(gate 9) rather than `duplicate-local-name` (gate 10); both are truthful,
non-confirming, and consistent with the documented precedence.

## F. Fact attribution and witness assessment

- **Spans and identity.** Statement / module-specifier (quote-inclusive) /
  binding spans are distinct; occurrence identity inputs
  `(sourceFile, statementSpan, bindingSpan, form, discriminator)` separate
  specifiers that share statement/module spans and repeated identical
  specifiers; coordinates are one-based end-exclusive UTF-16 with quoting
  conventions stated once. Resolver join key `(sourceFile, statementSpan,
  raw specifier, reference form, publication)` matches the accepted
  relative-v1 records and prevents endpoint-only joins. `exportSpans[]`
  restores multi-occurrence span identity per export entry (source p9
  evidence: one entry with two occurrence edges for `merged`; separate alias
  entries for `repeated-export`).
- **Witness.** `binding1.local-v1` is a versioned computed derivation over
  the canonical ordered fact-id tuple + policy; it includes source
  occurrence identities, keeps same-file (3-step) and import (9-step) proofs
  distinct, is deterministic across reopen, and a changed input changes its
  constituent fact ids so a stale witness cannot validate against a new
  publication (missing fact → integrity failure). It is never an invented
  stored edge; persistence, if ever chosen, must carry dependency ownership
  (ADR-1).
- **Vocabulary against actual source.** `module-reference` values
  (`import`, `import-type`, `require`, `export-from`, `export-type-from`,
  `export-star`, `export-namespace-from`) verified in
  `docs/ecmascript-module-resolution.md:66` and the extraction profile;
  resolver outcomes `Resolved/Unresolved/External/Unsupported` verified in
  `TreeSitterModuleResolution.cs`; `export-via` is `direct/local/expression`
  only and re-export clauses publish **no** export entries
  (`EcmaScriptExportGraphPublisher.cs`, `EcmaScriptExportObservation.cs`),
  which is what makes the §3.4 barrier branch correct for `barrel-named`;
  heritage extraction drops qualified TS types (`HeritageName` = identifier
  /type_identifier/member_expression) — verified — which is exactly why fact
  C is required; the `heritage-target` namespace is file-scoped and
  declaration-less (profile revision 5, `docs/ecmascript-symbol-identity.md`).
  The `.abi14.p8` citation matches the source comment ("existing facts keep
  p8 so a p8→p9 re-index is byte-identical; the origin uses p9").
- **Forward precision items** (P3): §3.5 does not restate the
  different-declaration disposition for the same-file route that §3.4 step 4
  and §3.7 already apply (Q3-F03); §3.3's heritage-candidate selection reads
  as bullet 1 alone unless bullet 2's local-name linking is read as
  set-extending — it is required for A02's export-alias confirmation
  (Q3-F05).

## G. Query and publication assessment

- **Result dimensions remain separate.** Confirmed (complete witness,
  identity-grounded), possible consumer (module-level named import resolved
  to the target file; never a type-use claim), unknown (closed reasons, per
  heritage/occurrence subject), publication qualification, and coverage
  (evaluated/unsupported/ambiguous/not-evaluated/truncated) are independent;
  a class may be confirmed while its file is a possible consumer, and a file
  may hold supported and unknown occurrences. Integrity failures are never
  unknowns; ordinary missing attribution is never an error.
- **Publication truth table (read exactly; mechanically derivable):**
  `false`/any → `qualified`; `true`/non-blank identity → `consistent`;
  `true`/null-or-blank → `qualified`, never promoted. Verified against
  `CodeGraphPublication` source (constructor normalizes blank to null and
  does not forbid true/null) and `EnsurePublication` (full-record equality →
  movement of any field, including same-content/new-run, fails on change).
- **Failure behavior.** Contradictory endpoints, mismatched unit versions,
  superseded units in a chain, missing witness facts, duplicate identities,
  invalid input, cancellation mid-query, and query/store failure produce no
  partial classified output; failed/cancelled re-index preserves the last
  successful publication with unchanged identity/qualification; live edits
  never move or upgrade it. Truncation is visible (stage, charged work,
  applied limits, omitted frontier, lower-bound flags) and coverage states
  truncated / incomplete-frontier / complete-within-profile; a result cap is
  never presented as proof that no further relationships exist.
- **Consumer safety.** Qualification is a discriminated field of the primary
  result, embedded in the coverage text and preserved by serialization, with
  a checked consistent-only authority path that refuses qualified results;
  the contract explicitly notes a sibling field cannot mechanically prevent
  copying a bare list — the wrapper plus tested boundaries is the stated
  reasonable safeguard. Integrity failure cannot be laundered into a
  qualified success.
- **Pinning item** (P3): the observable outcome for a *target* whose own
  physical declaration record is not unique (merged target) is not stated
  explicitly — functional requirement 2 and A24 are jointly satisfiable only
  if "resolve the target" is read as constraining confirmation rather than
  rejecting the query (Q3-F04).

## H. Acceptance contract assessment

Every audited row is satisfiable under the proposed facts and query
semantics; no row requires a relationship the contract cannot extract or
construct. Fixture semantics re-verified with the independent oracle run.

| Row | Fixture(s) | Key assertion | Independent check |
| --- | --- | --- | --- |
| A01 | SvgRenderer/HtmlRenderer | complete witness; other/Renderable never the target | trace + oracle (valid import binding) |
| A02 | AliasRenderer; Q2 export-alias | both confirmed; no literal-alias name matching | oracle valid; needs §3.3 bullet-2 linking (Q3-F05) |
| A03 | other/Renderable; merged; nested-export | distinct identities; ambiguous-export; unattributed-export | p9 evidence (merged 1 node/2 edges/2 physical); oracle valid |
| A04 | Q2 wrong-export | exactly `unresolved-heritage-name`; file still a possible consumer | trace: zero candidates; Gadget import resolves to target file |
| A04b | r2 absent-export | clause + occurrence `missing-export`; not barrier | oracle TS2305; no export-side records |
| A05 | Q2 collision | `conflicting-local-binding`; neither declaration confirmed | oracle TS2440/2420 |
| A06 | SvgRenderer chain | witness terminates at exact record/node; p8 evidence | source-verified citation |
| A07 | full corpus | exactly the four positives; all negatives stay negative | full trace in §E; contract-level feasibility, not a production claim |
| A08 | Preview | possible consumer once with occurrence detail; export-side excluded | trace |
| A10 | BarrelPreview + api; barrel-named/star/missing/multi/no-declaration | barrier family, separate witnesses; BarrelPreview obligation met | p9 evidence: re-exports publish no entries → barrier branch; oracle valid |
| A10b | barrel-ordinary-consumer; barrel-missing-consumer; direct | ordinary → missing-export; unresolved visible; direct stays confirmed | p9 evidence ordinary.ts has no export-side records |
| A12 | SvgRenderer; unsupported; forms-import-equals/inline-type/dynamic | default/import-equals refused; inline type-only confirmed; dynamic recorded only | oracle: TS2709 / valid / no heritage |
| A13 | corpus; same-file; repeated-export | witness completeness; short proof; separate alias witnesses | trace + p9 repeated-export evidence |
| A17 | filesystem + synthetic | full truth table; serialization; authority path | source-level constructor/EnsurePublication checks |
| A18/A18b | bound view; indexing path | mid-query cancellation/movement/failure; retention | contract text; movement semantics source-verified |
| A21 | nested-shadow; TypeParam; synthetic | three fail-closed reasons; no local-first | oracle: valid-binds-local; TS2422 |
| A22 | same-file; same-file-merged | same-file positive with short witness; merged refused | oracle valid; two declarations bound |
| A23 | duplicate-imports; nested-unrelated | duplicates refused with distinct identities; unrelated nested accepted | oracle TS2300 / valid import binding |
| A24 | merged, type-value, nested-export | ambiguous-export / unattributed-export; record ids | p9 evidence; Q3-F04 pinning item for merged-target observability |
| A25 | unsupported; expression-heritage; interface-extends | three observable heritage reasons with stored evidence | oracle: valid/T2693/valid |
| A26 | ghost, ambient-export, ambient-script, recovered-clause; reserved reasons | four ineligibility reasons; reserved never emitted | oracle diagnostics; reserved labels verified |
| A27 | barrier-scope | related barrier only; unrelated never appears; direct confirmed | oracle: three valid bindings |
| A28 | external-package | external-module; no consumer | oracle TS2307; resolver External outcome exists |

Reason table: 20 emittable reasons + 2 reserved, each with trigger and
fixture; all emittable reasons reachable under committed corpora or the
declared synthetic publication condition; deterministically ordered
precedence; no reserved reason appears in any expected result. Budgets:
charged work (candidate/heritage scans, resolver joins, attribution lookups,
witness steps) is separate from result caps; work is charged pre-dedup in a
fixed stage order; truncation is visible with lower-bound flags.

## I. Independent adversarial evidence

- A fresh run of the **unchanged Q2 oracle tool** (blob `c657eaf1…`,
  copied byte-identically) under TypeScript 5.9.3 / node v24.21.0 over a
  43-case manifest (14 committed Q2 corpora + 23 R2 fixtures + 6 Q3-authored
  supplements) reproduced the recorded R2 run for all 23 R2 cases with 0
  differences and the recorded Q2 run for all 14 corpora with 0 differences
  (normalized schema). Artifacts: `q3-oracle-run.json`,
  `q3-oracle-recorded-comparison.json`, inputs in `q3-oracle-input-cases.json`.
- The six supplements target the task-brief shapes not already covered
  (nested shadowing in the Q2 shape, type-parameter boundary, block-scope
  clause, unshadowed alias, import-vs-nested-unrelated, local-namespace
  qualified heritage); results tabulated in §E.
- Committed p9 observations (Q2 evidence, read-only) were spot-checked
  against the contract's claims: barrels (`export-from` reference, no
  entry), merged (one node, two occurrence edges, two physical
  declarations), repeated-export (separate alias entries), type-value
  (distinct interface/field nodes; one ambiguous local-via entry), and the
  18-assertion audit file (all true). The Q2 C# harness was not re-executed
  (no Q3 uncertainty required it).
- One in-packet inconsistency was found and is reported as Q3-F01: the R2
  evidence index understates its own change surface.

## J. Owner decision recommendations

### ADR-1 — binding representation

- **Recommended option: A (query-time joins with computed witnesses).**
  Inputs are single-file facts (A/C per-file; B/E on exporter/barrel sides);
  the corrected rule reads scope, physical cardinality, attribution status
  and resolver joins from stored records, so a persisted binding edge would
  either duplicate authority (divergence risk) or move cross-file
  invalidation into the indexing lifecycle, which the accepted contracts do
  not model. Option A keeps ambiguity visible as reasons instead of missing
  edges, is additive over p9, and its witness identity is deterministic.
- **Evidence:** §E/§F traces; symbol §3.6; ADR-1 comparison table;
  Q2 E.5 assessment retained.
- **Remaining risks:** query-time recomputation cost (bounded by charged
  budgets; no benchmark claimed); any future persistence must carry explicit
  dependency ownership.
- **Required authority:** explicit owner approval recorded against the
  repaired commit/tree; silence is not approval.
- **Alternative consequence:** Option B is correctness-relevant (fact set,
  invalidation ownership, retraction tests) and requires re-review of the
  amended contracts before implementation authorization.
- **Blocking:** implementation remains blocked until this decision is
  recorded.

### ADR-2 — publication policy

- **Recommended option: design 2 (qualified results with mandatory
  safeguards).** The only shipped provider declares `false`/null, so
  fail-closed would reject every production publication and make the
  vertical slice untestable end-to-end; the mechanical truth table, full
  identity block, integrity-failure list, checked consistent-only authority
  path and retention semantics keep inconsistency visible and prevent
  unqualified authority use.
- **Evidence:** §G; publication contract §3–§5; source-level constructor and
  `EnsurePublication` checks.
- **Remaining risks:** downstream copying of bare confirmed lists cannot be
  mechanized away (stated); automation must use the checked path.
- **Required authority:** explicit owner approval of design 2 and the
  authority-path requirement.
- **Alternative consequence:** design 1 requires an explicit
  `consistent-publication-required` error state and re-aimed A07/A17 rows —
  a correctness-relevant re-review.
- **Blocking:** implementation remains blocked until recorded.

### Narrow attribution authority

- **Scope (verified precise):** physical declaration records, export aliases
  and barrier reporting with one-hop relevance — exactly the fact-B/E subset
  the symbol contract §2–§3 and query contract §3.3–§3.4 require.
- **Does not authorize general Track B:** stated in both the disposition and
  integration-readiness documents; the contracts additionally exclude
  re-export closure, default/namespace binding, `export =`, name-converged
  Derived attribution and general lexical-scope machinery.
- **Dependency real:** the prior Track B deferral (`D-EC6C-04`, "new Track B
  owner decision" required; EC6C-OWNER-01 withholds Track B) explicitly
  deferred exactly this attribution work; EC-6D cannot confirm any import
  binding without it.
- **Implementability without unrelated Track B work:** yes — the subset is
  self-contained (module-scope records, fail-closed exclusions, witness
  chain); the remaining Track B design (full scope tables, general merge
  semantics, name-converged attribution) is not required.
- **Required authority:** explicit written owner authorization recorded
  against the prior deferral; Q3 does not grant it.

## K. New findings

All are P3 (non-blocking documentation/precision items). No new P0/P1/P2
findings were found.

| ID | Sev | Evidence | Impact | Required action |
| --- | --- | --- | --- | --- |
| Q3-F01 | P3 | `evidence-index.md` R2 section states "ten modified and five added" and omits `README.md` and `evidence-index.md` from the modified list; Git shows 12 modified + 6 added (commit message is correct) | A verifier relying on the index could under-scope the R2 surface; no correctness impact | Correct the two counts and the modified list (smallest correction), preferably before the owner acceptance binds to a commit/tree |
| Q3-F02 | P3 | Reason table: (a) `ambient-or-global-context` lists r2 `ambient-export` as a fixture although that case exercises `ineligible-declaration` (A26 and the fixture README agree); (b) the `unsupported-import-form` trigger text omits the §3.4 route-step-1 emission path (eligible form with `resolutionStatus = unsupported`) | Fixture mapping could mislead implementers; both conditions remain reachable and non-confirming | Align the fixture references and state the second trigger path |
| Q3-F03 | P3 | Symbol §3.5 states confirmation requires the declaration node to equal the queried interface but does not state the outcome for one eligible module-scope declaration whose node is a different declaration (const/field/alias/other kind); §3.4 step 4 and §3.7 already apply "omitted" for the import route and chains to other declarations | Implementer discretion on a rare shape; both readings refuse confirmation | State that such a same-file candidate is omitted from this target's result (consistency with §3.4/§3.7) |
| Q3-F04 | P3 | Acceptance functional requirement 2 ("resolve an interface target only when … its unique physical declaration record is known") does not state the observable outcome for a *merged/ambiguous target*, while A24 expects `ambiguous-export` for chains into merged entries | Under a strict input-error reading, A24's merged observability could be unverifiable; no false-confirmation risk under either reading | Pin: the query runs, reports no confirmation for such chains as `ambiguous-export`, and the target's record remains unresolved (or explicitly record an equivalent safe reading) |
| Q3-F05 | P3 | Consumer query §3.3 bullet 1 selects heritage candidates by `written name ∈ A2`; A02's export-alias confirmation requires the bullet-2 local-name linking to extend that set (the alias local name is not itself in A2) | A literal bullet-1-only implementation would fail A02's export-alias positive | State the heritage-candidate selection as the explicit union of (written name ∈ A2) and (written name = local name of an associated import occurrence) |

## L. Remaining qualifications

**Verdict qualifications (bounded; owner action; gate; consequence):**

1. **Q3-F01** — evidence-index correction. Owner action: direct the small
   documentation correction (or record the corrected surface) before owner
   acceptance binds. Consequence if unaddressed: the packet's own index
   understates its surface; no correctness impact.
2. **Q3-F02** — reason-table precision. Owner action: correct the two items
   at or before implementation authorization. Consequence: fixture-to-reason
   mapping ambiguity for implementers; no reachability or safety impact.
3. **Q3-F03** — same-file different-declaration disposition. Owner action:
   add the one-line disposition before implementation authorization.
   Consequence: implementer discretion on a rare, safe shape.
4. **Q3-F04** — merged-target outcome pinning. Owner action: state the safe
   observable behavior (A24-compatible) before implementation authorization.
   Consequence: A24's merged observability could be read as unsatisfiable
   under a strict reading.
5. **Q3-F05** — §3.3 union wording. Owner action: state the union explicitly
   before implementation authorization. Consequence: literal implementation
   could fail A02's export-alias case.

**Inherited limitations (retained, unchanged):** Windows-only EC-6C
qualification; unreproducible historical migration hash (QN-1/FQ1-EV-1);
staged p6→p8→p9 evidence does not prove a direct p6→p9 procedure; small
fixtures; performance NOT EVALUATED; p9 not in canonical main (integration
authorized separately); legacy `FindImplementations` stays seed-only (F-08);
intended-consumer value confirmation outstanding (R16); Track B remains
outside authorization; F-01 remains qualified on evidence wording with the
R2 corrections now in place.

## M. Next action

Recommend **EC6D-OWNER-01: Contract and Architecture Decision Acceptance**:
record owner acceptance of the R2 contracts at the repaired commit/tree,
adopt ADR-1 (Option A) and ADR-2 (design 2) — or explicitly record the
analyzed alternatives with their re-review consequences — and grant the
narrow attribution authority explicitly, addressing the five P3 corrections
above at or before the acceptance record. This is followed by a separately
authorized integration/consolidation milestone and the Hetu → Andirit rename
per the intended sequence (owner disposition → separately authorized
integration → controlled worktree consolidation → rename → implementation):
R2 introduces no dependency that makes that sequence unsafe. Persistent identities that any rename must either preserve
or migrate by re-indexing are: provider ids/versions
(`penghou.hetu.tree-sitter.*`, `imports1.local-v1`, `heritage1.local-v1`,
`module1.local-v1`, `exports2.local-v1`, `resolver2`, `binding1.local-v1`),
graph id prefixes (`typescript:file:`, `typescript:export:`,
`typescript:heritage-target:` and their JavaScript counterparts), and public
API/type names; changing any of these must not produce mixed publications —
re-index under the renamed provider set with the accepted migration pattern.
Git provenance and review evidence are content-addressed and unaffected.
Implementation remains blocked until the owner decisions are recorded.

## N. Evidence limits

Q3 verifies planning contracts, not behavior: no EC-6D fact (A/C/E),
attribution record, reason emission, budget enforcement, witness
recomputation, reopen of new facts, migration or query implementation
exists, and nothing here is evidence that it does. The TypeScript oracle is
a diagnostic semantic reference over small fixtures, not a product
dependency or a precision/recall measurement. p9 observations were
spot-checked read-only from committed Q2 evidence and production source; the
Q2 C# harness was not re-executed. The link check verifies relative-target
existence only. No cross-platform, representative-repository, performance,
migration or release qualification was performed. Q3 does not re-qualify the
authenticated recovery beyond the targeted identity, ancestry, evidence and
change-surface checks recorded in §A/§C.

## Validation log

1. Git identities and ancestry — verified (§A; `git-verification.txt`).
2. Q2 supersession and findings crosswalk — verified (§C).
3. R2 change surface — verified 12+6; one index discrepancy recorded (Q3-F01).
4. TypeScript oracle cases — re-run; 23/23 and 14/14 with 0 diffs (§I).
5. Lexical binding and same-file proof challenged — §E (oracle + trace).
6. Fact vocabulary against production source — verified (§F).
7. Closed reason reachability — verified (§H; 20 + 2 reserved).
8. Acceptance fixture satisfiability — audited (§H).
9. ADR proposals and owner gates — verified decision-ready (§J).
10. Evidence references and Markdown links — 22 documents / 67 links /
    0 missing (§I).
11. `git diff --check` — clean (§A).
12. No production implementation or test changes — R2 diff empty outside
    `docs/planning/ec6d/`; Q3 commit adds review documents only.
13. Protected worktrees unchanged — verified before/after (§A).
14. Only Q3 review documents and non-production evidence committed —
    enforced by the review commit (§A).
15. Final review worktree clean — verified at completion.

**EC6D-SCOPE-Q3 COMPLETE: ACCEPT WITH QUALIFICATIONS**

- Final review commit/tree/parent: recorded by the binding attestation
  commit; resolve with `git show -s --format="%H%n%T%n%P" review/hetu-ec6d-scope-q3`.
- Open P0/P1 findings: none.
- Remaining qualifications: Q3-F01..Q3-F05 (all P3) plus the inherited
  limitations in §L.
- Owner decisions required: ADR-1, ADR-2, narrow attribution authority
  (EC6D-OWNER-01).
- Recommended next action: EC6D-OWNER-01 acceptance, then separately
  authorized integration/consolidation, then the Hetu → Andirit rename, then
  EC-6D implementation on the renamed baseline. No implementation,
  integration, rename, publication or release is authorized by this review.
