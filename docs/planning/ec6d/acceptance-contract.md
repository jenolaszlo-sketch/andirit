# Proposed EC-6D acceptance contract (R2 revision)

**Status: proposal for owner review (EC6D-PLAN-R2).** This document
authorizes no implementation, integration or release. It supersedes the R1
matrix at commit `0e1b213`; the R1 text remains in Git history. Change log
and new fixtures are recorded below the matrix. Every row is a **future
contract criterion**, not evidence that EC-6D behavior exists.

The proposed outcome is a bounded TypeScript query for one uniquely
selected interface declaration, its uniquely bound direct explicit
implementers, and its direct named-importing modules as possible consumers.
It does not claim whole-program impact.

## Functional requirements

### Supported profile

1. Run against one TypeScript Tree-sitter plugin inventory and one
   successful publication carrying facts A–E (symbol-binding-contract §1–2).
2. Resolve an interface target only when its declaration identity is unique
   and its unique physical declaration record is known.
3. Support a direct named declaration export from the target module and a
   direct named relative import under the accepted relative-v1 module
   policy, plus module-scope simple-identifier class `implements` clauses.
4. Support a named imported alias and explicit type-only import form when
   the syntax is unambiguous; type-only status is per specifier.
5. Associate a class's explicit `implements` name with exactly one eligible
   visible binding at module scope: one import occurrence (§3.4) or one
   module-scope physical declaration (§3.5) of the symbol contract.
6. Return direct named-importing modules as possible consumers. A module
   import is not proof of a use site.
7. Return the target declaration, direct explicit implementers, possible
   importers, witnesses, explicit unknowns, and coverage/qualification/
   truncation status from an additive bounded consumer query.

Merge-converged declarations, nested declarations, ambient/global
contexts, recovered syntax, qualified/expression/extends heritage, and
renamed or multi-hop re-export paths are outside the confirmed profile and
must be classified explicitly (unknown or documented not-evaluated), never
silently omitted.

### Result shape

The result must contain: selected declaration node and physical record ids,
kind, path, span; zero or more direct implementers with class identity,
binding route, target declaration record, and one or more complete
witnesses; zero or more possible-consumer modules with occurrence detail;
explicit unresolved, ambiguous, unsupported, shadowed and conflicting
cases with stored evidence; full bound-view publication identity, provider/
profile versions, applied query, charged-work budgets, truncation state and
omitted frontier count; structured coverage and a fixed coverage statement
embedding publication qualification. No consumer must infer which node is
the interface from a same-spelling name.

## Correctness requirements

- A positive direct implementation must resolve to the selected declaration
  identity through a complete witness; node counts alone never establish
  uniqueness (physical declaration records are counted before symbol-node
  deduplication).
- Same-name declarations in different modules remain distinct; merged,
  nested, ambient and recovered declarations never produce a positive
  binding. The fixture's OtherRenderer must not bind to
  contracts/Renderable.ts.
- The imported alias case must bind to the original imported interface only
  when the import path and imported/exported name are uniquely supported;
  export-alias discovery must reach declarations exported under a public
  alias.
- A missing import target, absent exported name, duplicate/ambiguous
  binding, unsupported form, conflicting binding, insufficient lexical
  context, unsupported re-export path or conflicting fact must not produce
  a positive binding; each must produce its specified unknown reason.
- AliasRenderer is included only through its direct aliased import;
  BarrelPreview is reported as an occurrence-level re-export-barrier
  unknown, never confirmed.
- Direct named importers are possible consumers, not confirmed type uses;
  export-side references, default/namespace forms and `require` calls are
  never possible consumers.
- CanvasRenderer is not a direct implementer; transitive impact stays out.
- Do not infer absence of impact from missing graph facts. Report
  evaluated, unsupported, ambiguous, not-evaluated and truncated states
  within the declared profile.

## Provenance and evidence

Each claimed relation is traceable to its source occurrence, source
location, provider and provider version (heritage evidence cites its own
`.abi14.p8` version, not the profile revision), extraction/binding policy,
evidence quality, source content hash and index-unit origin, and every
witness step. Module-file resolution and symbol binding are shown as
separate steps; the witness identity is a versioned computed derivation
(`binding1.local-v1`), never an invented stored edge. The result states
that the bounded rule is not a full TypeScript compiler, bundler or
runtime proof.

## Determinism

For identical source bytes, plugin versions, repository inventory and
equivalent query bounds: repeated indexing produces the same canonical
facts and IDs; equivalent input enumeration order does not change canonical
results; repeated queries return identical ordered results, witnesses,
coverage and truncation state; evidence spans retain documented UTF-16,
one-based, end-exclusive coordinates. Run metadata and snapshot integrity
hashes may differ when they include publication-specific values; compare
canonical facts and query results. Semantic graph equivalence must never
substitute for byte-identical indexing.

## Persistence and lifecycle

Run the contract against InMemory and LatticeDB. For LatticeDB, commit the
publication, close the store/process, reopen, then verify the selected
declaration, each positive and negative binding, witnesses (identity
recomputed identically), provenance, unknowns, coverage and qualification.
Repeated indexing of unchanged sources must not duplicate facts. Editing or
deleting an import, export, declaration or implementing class must remove
or replace all owned stale facts atomically, including resolution outcomes
that depend on target inventory. Empty replacement, failed run and
cancellation retain the last successful publication; conflicting IDs with
different facts fail closed.

## Compatibility

- Preserve EC-6C p9 export entries, occurrence IDs, evidence and properties,
  and EC-6B relative-v1 resolver results for unchanged source.
- Keep Structural imports and existing query defaults/preset membership
  unchanged; expose new behavior additively and opt-in.
- Qualify the p9-to-new-profile migration, including durable close/reopen
  and comparison of all pre-existing facts.
- Verify snapshot import/export and public query compatibility where
  affected.

## Failure behavior

- Invalid repository/declaration IDs, bounds and malformed inputs follow
  existing argument-validation conventions.
- Unsupported or ambiguous syntax yields an explicit non-positive status;
  it never becomes a guessed binding.
- Cancellation, extraction failure and query/store failure publish or
  return no partial binding facts or classified results; the last
  successful publication is retained.
- A duplicate identity with different facts, a missing fact inside an
  asserted witness, contradictory endpoints or mismatched unit versions are
  deterministic integrity failures, not first-wins behavior.
- Stale publication, source conflict and traversal truncation remain
  visible; partial results are never presented as complete.

## Independent consumer qualification

A reviewer unfamiliar with implementation internals must be able to check
out the exact accepted source and corpus revisions; follow documented
restore/build/run steps; reproduce the selected declaration, direct
implementers, possible importers, negative cases, provenance, canonical
digests and test results; and verify outputs against a human-reviewed
reference set without using implementation-generated counts as ground
truth. Qualification must include direct import, alias, export-alias
discovery, same-name negative, missing module, absent export, duplicate
declaration, merged declaration, nested scope, shadowing, qualified
heritage, unsupported forms, re-export barriers (relevant and unrelated),
repeated index, source change/delete, cancellation and durable reopen. It
must record any platform or store not evaluated.

## Performance and operations

No numeric target is justified by the small fixtures. Before thresholds,
measure indexing wall time, peak memory, query latency by result size,
durable reopen time and repeated-index cost on a representative repository.
Do not make a scale claim from the fixture.

## Out of scope

No compile/runtime completeness, all type uses, calls/references, implicit
implementations, dynamic dispatch, transitive inheritance, re-export
closure (one bounded alias-association hop only), package/config
resolution, cross-plugin symbols, CommonJS, graph diff, ranking or AI
explanations.

---

## R2 requirements matrix (revision of the R1 matrix)

Fixtures named `Q2 <case>` refer to the committed reconciled Q2 corpora
(`docs/reviews/ec6d-scope-q2-evidence/cases.json`); `Q2 adv <case>` refers
to the committed adversarial fixtures on the Q2 branch
(`docs/reviews/ec6d-scope-q2-evidence/fixtures/adversarial/`); `r2 <case>`
refers to this plan's supplements under
`docs/planning/ec6d/analysis/fixtures/r2/` (see fixture inventory below).
Layer: extraction / binding / query / persistence. Every positive
assertion is a future criterion unless marked as verified existing
behavior.

| ID | Required behavior | Preconditions | Fixture / input | Expected observation (assertion) | Negative assertions | Layer | Q2 link |
| --- | --- | --- | --- | --- | --- | --- | --- |
| A01 | Direct named import resolves to the correct declaration with scope and uniqueness guards | Supported publication; facts A–E | Corpus `SvgRenderer.ts`, `HtmlRenderer.ts` | Confirmed via import route with a complete witness (context, occurrence, resolver join, entry, attribution, physical declaration record = target) | other/Renderable.ts never the resolved target; a synthetic publication missing any witness step fails closed, not confirmed | extraction + binding + query | Q2-01/04 |
| A02 | Imported alias binds; discovery reaches public export aliases | Alias occurrences recorded | Corpus `AliasRenderer.ts` (`Renderable as Drawable`); Q2 `export-alias` (`Internal as Public`, imported as `Alias`) | Both confirmed; export-alias reached through the attributed `Public` entry; witness shows localName → importedName → entry | Literal alias spelling is never matched against entry names; no confirmation by name equality | binding + query | Q2-03 |
| A03 | Duplicate interface names stay distinct across files; merging and nesting never yield unique-physical confirmation | Both files indexed | Corpus contracts/other `Renderable`; Q2 `merged`; Q2 `nested-export` | Two distinct identities across files; query on one never returns the other's implementers; `merged` (one node, two physical) → `ambiguous-export` carrying both candidate records; `nested-export` → `unattributed-export` (only a nested declaration exists) | No name-only convergence; no confirmation derived from symbol-node count; hidden nested declarations never attributed | extraction + binding | Q2-02 |
| A04 | Heritage name not bound by any candidate | Occurrences emitted | Q2 `wrong-export` | Unknown `unresolved-heritage-name`; file remains a possible consumer (its `Gadget` import resolves to the target file) | No confirmation; reason is exactly `unresolved-heritage-name` (not missing-export/unsupported-form) | binding + query | Q2-07 |
| A04b | True absent export | Absent-export fixture | r2 `absent-export` (import of the target name from a module that does not export it; one via heritage clause, one via bare import) | Clause-level unknown `missing-export`; occurrence-level `missing-export` for the bare import | No confirmation; not `re-export-barrier` (no export-side records); no possible consumer via that import | binding + query | Q2-07 |
| A05 | Import/local conflict fails closed | Conflict fixture | Q2 `collision` | Unknown `conflicting-local-binding`; no target selected; invalid-source note (TS2440) | Imported declaration NOT confirmed; local interface NOT confirmed; import-first never applied; valid shadowing exclusion cross-checked in A21 | binding | Q2-01 |
| A06 | Explicit implements binds to the intended interface; placeholder never target | Facts A–E | Corpus `SvgRenderer.ts` chain | Witness terminates at the exact declaration record + node = target; heritage evidence cites `…abi14.p8` (fact's own version) | Heritage placeholder node never reported as implementation target; no witness step cites a non-existent fact | binding + query | Q2-04 |
| A07 | Four canonical direct implementations identified (positive-capability guard) | Full corpus, supported profile | Contracts `Renderable` target | Exactly SvgRenderer, HtmlRenderer, AbstractRenderer, AliasRenderer confirmed (fixture-scoped); all four arrive through supported routes | OtherRenderer absent (different declaration → omitted); CanvasRenderer absent (transitive); BarrelPreview not confirmed; MissingImport not confirmed; no additional confirmed; the row fails if the fix achieves negatives by refusing all bindings | query | Q2-01/02/03 |
| A08 | Possible consumers from named import occurrences | Facts A–E | Corpus `Preview.ts` | Preview listed once as possible consumer with occurrence detail (spans, type-only, resolution rule) | Not confirmed; `api/index.ts` (export-side only) never a possible consumer; default/namespace forms never possible consumers | query | Q2-06 |
| A09 | Idle import is possible consumer only | Import without use | Q2 `idle` | Possible consumer; confirmed list unchanged | Never claims type use; never confirmed | query | — |
| A10 | Re-export barrier detection over the real vocabulary | Facts E emitted | Corpus `BarrelPreview.ts` + `api/index.ts`; r2 `barrel-named`, `barrel-star`, `barrel-missing`, `barrel-multi`, `barrel-no-declaration` consumers | BarrelPreview → occurrence-level `re-export-barrier` (associated alias `Drawable` via the barrel's resolved re-export record; no heritage clause required); heritage consumers through named/star/missing/multi/no-declaration barrels → clause-level `re-export-barrier` listing all matching fact-E records as separate witnesses (never first-wins) | None of these confirmed or silently omitted; barrier never inferred from import-side references; separate sources remain separate witnesses | extraction + binding + query | Q2-03/06 |
| A10b | Ordinary imports are not barriers; unresolved export sources stay visible; unrelated paths do not contaminate | Facts E emitted | r2 `barrel-ordinary-consumer`, r2 `barrel-missing-consumer`; corpus `barrel/direct.ts` composed | Ordinary consumer → `missing-export` (module has only import-side references); missing-consumer → barrier carrying the unresolved export record; `direct.ts` Direct remains confirmed | No false barrier from ordinary imports; unresolved sources never invisible; unrelated barrel/missing paths never invalidate the direct confirmation | query | Q2-06 |
| A11 | Missing modules and unresolved aliases | Unresolved specifier | Corpus `MissingImport.ts`; Q2 `unsupported` (`MissingAlias`) | `unresolved-module` with occurrence id and spans; no confirmed; no possible consumer via that import | No binding cites an unresolved module; alias case recorded with target-association coverage | binding + query | Q2-07 |
| A12 | Import forms: supported vs unsupported, with heritage clauses | Forms recorded | Corpus `SvgRenderer.ts` (statement type-only); Q2 `unsupported` (`DefaultWidget`); r2 `forms-import-equals`, `forms-inline-type`, `forms-dynamic` | Default/import-equals heritage → `unsupported-import-form`; inline `{ type Widget }` heritage → CONFIRMED (eligible, per-specifier type-only); dynamic occurrence recorded (form `dynamic`), counted in coverage, never a binding candidate; namespace-qualified heritage → `qualified-heritage-unresolved` (A25) | Unsupported forms never in confirmed chains; inline type-only not classified unsupported; occurrence inventory equality at extraction (nothing silently dropped) | extraction + query | Q2-07 |
| A13 | Provenance completeness, same-file variant, computed witness | A07 + same-file results | Corpus + Q2 `same-file` + Q2 `repeated-export` | Every confirmed entry exposes ordered witnesses: heritage fact (p8 evidence), context record, occurrence spans (import route), resolver join key + rule, entry + occurrence edges, attribution record, declaration record + node, witness id (`binding1.local-v1` canonical hash); source hashes/unit origins included; same-file witness uses only heritage + context + declaration steps; repeated-export preserves separate alias witnesses; multi-chain classes preserve distinct witness ids | No confirmed entry without a complete witness; no invented stored edge ids; every step verifiable as a stored fact | query | Q2-04 |
| A14 | Deterministic ordering with nested ties | A07 + Q2 `multi-specifier` + `repeated-export` | Same publication repeated; reversed enumeration | Identical ordered results across repeats including witnesses (by id), steps (ordinal), occurrences (span), unknowns (subject); reverse enumeration does not change results; truncation ordering stable for equal bounds | No ordering instability; deterministic dedup | query | Q2-07 |
| A15 | Repeat indexing identical; lifecycle transitions | Fresh stores | Full corpus twice; edit/delete/target-resolution/ambiguity transitions; byte-identical checkout for digests | Identical identity sets and identical ordered results; transitions change outcomes deterministically (edit consumer import; delete target; change resolution target; introduce a merge → categorized per A03/A24) | No drift or fact duplication; digest comparisons only within byte-identical checkouts | extraction + persistence + query | Q2-04/07 |
| A16 | Durable reopen | LatticeDB publication committed | A07 publication; process-level reopen | Same declaration, confirmed entries, witnesses (identity recomputed identically), unknowns, coverage, qualification; source/version fidelity intact | No fact loss; no identity change; qualification preserved | persistence | — |
| A17 | Publication qualification truth table and consumption | Filesystem + synthetic publications | `false`/null; `true`/identified; `true`/null | `false`/null → `qualified`; `true`/identified → `consistent`; `true`/null → `qualified` (never `consistent`); full identity block present; serialization round-trip preserves qualification; checked authority path refuses qualified results; navigation labels qualified | No unqualified confirmed claim; digest/reopen never cited as consistency; structural integrity errors fail closed | query + persistence | Q2-05 |
| A18 | Query-side cancellation, movement, failure | Bound publication view | A07; pre-/mid-query cancellation; movement between joins incl. same-content/new-run; simulated query/store failure | Cancellation throws with no partial classified results; movement raises the existing fail-on-change failure; failures return no classified output | No partial results; no silent fallback to a stale or partial view | query | Q2-05 |
| A18b | Index failure retention | Indexing path | Failed/cancelled extraction/index run | No partial binding facts published; last successful publication retained with unchanged identity/qualification; bound query still answers from it | No freshness claim; later live edits do not move or upgrade it | persistence + query | Q2-05 |
| A19 | p9 fact preservation | Unchanged sources | Full corpus; p9 vs new profile | p9 export entries, occurrence edges, resolver edges and structural heritage facts preserve ids/evidence/properties; intentionally changed unit origins/index identity distinguished from fact changes; evidence-version policy documented (`.abi14.p8` retained facts vs new providers) | Any change to pre-existing fact identity, evidence or properties | extraction + persistence | Q2-04 |
| A20 | Legacy query compatibility | Full existing suite | Repository test suite | Existing tests pass unmodified; `FindImplementations` and presets unchanged; new query additive/opt-in | Any preset membership or default change | query | — |
| A21 | Lexical shadowing, type parameters and missing context fail closed | Facts C emitted | Q2 `nested-shadow` (valid TS binds function-scope local); Q2 adv `TypeParam`; synthetic publication lacking one fact-C record | Nested clause → `nested-scope-heritage`; type-parameter clause → `type-parameter-shadowing`; missing context → `binding-context-unavailable`; coverage counts nested and unavailable contexts | No imported confirmation in any case; no local-first guessing; the nested local declaration is not confirmed as the target either | binding + query | Q2-01 |
| A22 | Same-file binding route and same-file ambiguity | Facts C emitted | Q2 `same-file`; r2 `same-file-merged` | Positive: confirmed with `bindingRoute = same-file` and the short witness (no import/resolution/export steps; interface need not be exported). Negative: `same-file-merged` (interface + class) → `ambiguous-local-declaration` with all candidate records | No import/export steps appear in same-file witnesses; merged same-file names never confirmed; module-scope count counts physical records first | binding | Q2-01/02 |
| A23 | Duplicate local names fail closed; unrelated nested names do not conflate | Facts A/C emitted | Q2 `duplicate-imports`; r2 oracle `nested-unrelated` (valid TS: nested same-name does not block the module-scope import binding) | Duplicates → `duplicate-local-name`, both occurrences distinct identities with binding spans, no first-wins; contrast case → import route confirmed because only module-scope candidates participate | No ordering-based selection; nested declaration never selected nor made ambiguous | extraction + binding | Q2-01/02 |
| A24 | Merging, nesting and type/value cardinality | Physical records emitted | Q2 `merged`, `type-value`, `nested-export` | Extraction: physical declaration counts and candidate record ids recorded. Binding: `merged` and `type-value` → `ambiguous-export`; `nested-export` → `unattributed-export` | No unique-physical claim from node count; no guessed type/value namespace; invisible nested declarations never attributed | extraction + binding | Q2-02 |
| A25 | Qualified, expression and extends heritage stay observable | Facts C emitted (including clauses p9 skips) | Q2 `unsupported` (`NS.Widget`); r2 `expression-heritage`; r2 `interface-extends` | Each clause appears as an unknown with stored evidence (fact-C record id, span, written text): `qualified-heritage-unresolved`, `unsupported-heritage-form`, `heritage-kind-not-supported`; coverage counters include them | None confirmed; terminal-identifier relevance never promotes to binding; no silent omission for clauses inside the declared boundary | extraction + query | Q2-03 |
| A26 | Declaration ineligibility and reserved reasons | Facts B/C emitted | r2 `ghost`, r2 `ambient-export`, r2 `ambient-script`, r2 `recovered-clause` | `ghost` → `unattributed-export`; ambient export declaration → attribution status `ineligible-declaration-context` → `ineligible-declaration`; script/ambient clause → `ambient-or-global-context`; recovered clause → `recovered-heritage-syntax`; reserved reasons (`unsupported-export-form`, `unsupported-declaration-form`) are never emitted | No confirmation for any ineligible case; no reserved reason in a conforming publication | extraction + binding + query | Q2-02/07 |
| A27 | Relevant vs unrelated re-export paths | Composed fixture | r2 `barrier-scope` (target-associated barrier + unrelated barrel with its own consumers + direct confirmed consumer) | Barrier unknown only for chains connected to the target; unrelated barrel never appears in the target's unknowns or possible consumers; direct consumer remains confirmed | Unrelated unsupported paths never invalidate established relationships; no global contamination; structural publication failure remains the separate global error boundary | query | Q2-03/06 |
| A28 | External resolution status | Bare specifier | r2 `external-package` | `external-module` unknown with occurrence evidence; no resolution edge; no possible consumer via that import | No binding cites external modules; no invented module node | binding + query | Q2-07 |

### Fixture inventory

- **Committed Q2 corpora (authoritative, on branch `review/hetu-ec6d-scope-q2`
  at `docs/reviews/ec6d-scope-q2-evidence/`; referenced, not duplicated):**
  the fourteen cases in `cases.json` — `collision`, `nested-shadow`,
  `merged`, `wrong-export`, `nested-export`, `barrel`, `idle`,
  `duplicate-imports`, `multi-specifier`, `export-alias`, `unsupported`,
  `type-value`, `same-file`, `repeated-export` — with TypeScript 5.9.3
  oracle results (`typescript-results.json`) and p9 observations
  (`p9-adversarial-results.json`).
- **Committed Q2 adversarial fixtures:** `NsShadow`/`TypeParam` shapes at
  `docs/reviews/ec6d-scope-q2-evidence/fixtures/adversarial/`.
- **R2 supplements (this plan):**
  [analysis/fixtures/r2/cases.json](analysis/fixtures/r2/cases.json) with
  [analysis/fixtures/r2/README.md](analysis/fixtures/r2/README.md) — cases
  `absent-export`, `barrel-named`, `barrel-star`, `barrel-missing`,
  `barrel-multi`, `barrel-no-declaration`, `barrel-ordinary-consumer`,
  `barrel-missing-consumer`, `forms-import-equals`, `forms-inline-type`,
  `forms-dynamic`, `expression-heritage`, `interface-extends`,
  `same-file-merged`, `ambient-export`, `ambient-script`,
  `recovered-clause`, `ghost`, `external-package`, `barrier-scope`,
  `nested-unrelated`, `default-implement`, `qualified-member`. The R2
  TypeScript 5.9.3 oracle run over every case is recorded at
  [analysis/r2-typescript-oracle-run.txt](analysis/r2-typescript-oracle-run.txt).

### Coverage and measurement rules

- Precision/recall against the manually reviewed reference sets is reported
  per relationship class (direct implementers; direct possible importers)
  and never extrapolated beyond the fixtures.
- No numerical performance target appears in this contract; performance
  remains NOT EVALUATED until a representative baseline exists.
- Independent verification requires reproducing A07's classification from
  stored facts alone, without trusting implementation-generated counts.
- Every unknown reason in the matrix must appear in exactly the closed
  enumeration of the consumer-query contract §3.4; reserved reasons are
  non-emittable until a later version defines their reachable condition.

### R2 change log (vs the R1 matrix)

| R1 row | R2 change | Reason |
| --- | --- | --- |
| A01 | Guards added; witness completeness asserted | Q2-01/04 |
| A02 | Export-alias discovery case added | Q2-03 |
| A03 | Merging/nesting rejection added | Q2-02 |
| A04 | Expected reason corrected to `unresolved-heritage-name`; absent-export split out | Q2-07 |
| A04b | New row | Q2-07 |
| A05 | Unconditional confirmation replaced by conflict-unknown | Q2-01 |
| A06 | Physical declaration + p8 evidence assertions | Q2-04 |
| A07 | Positive-capability guard made explicit | Q2-01/02/03 |
| A08 | Predicates pinned; export-side exclusion added | Q2-06 |
| A10 | Corrected to realizable fixtures incl. BarrelPreview obligation | Q2-03/06 |
| A10b | New row | Q2-06 |
| A11 | Unresolved alias/target-association coverage | Q2-07 |
| A12 | Fixtures given heritage clauses; inline type-only positive | Q2-07 |
| A13 | Witness/physical-id/hash/same-file/multi-witness assertions | Q2-04 |
| A14 | Nested ordering and tie rules | Q2-07 |
| A15 | "identical" wording; lifecycle transitions | Q2-04/07 |
| A17 | Truth table, consumption safeguards | Q2-05 |
| A18 | Mid-query cancellation, movement, failure | Q2-05 |
| A18b | New row | Q2-05 |
| A19 | Evidence-version policy assertions | Q2-04 |
| A21–A28 | New rows | Q2-01/02/03/06/07 and task R2-A/B/D/E |
