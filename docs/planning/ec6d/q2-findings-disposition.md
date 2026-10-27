# EC6D-PLAN-R2 — Q2 findings disposition

**Status: R2 remediation record.** This document dispositions every finding
of the independent Q2 review and records the exact contract corrections.
It authorizes nothing. It preserves Q2 findings exactly as recorded and does
not modify the Q2 report or its evidence.

**Authoritative sources.** The reconciled Q2 review lives at
`review/hetu-ec6d-scope-q2` (content commit
`6630624f976d53161e2657b638c7a67c41f9c0ed`, tree
`5c4447b43c19133fc912ad1402f34405541a74fe`; branch HEAD `1bfcc84`), with
evidence under `docs/reviews/ec6d-scope-q2-evidence/`. The report's §K
register (Q2-01..Q2-08) supersedes the earlier Q2-N numbering; its crosswalk
maps the assignment's N-identifiers:

| Assignment identifier | Authoritative register |
| --- | --- |
| N01, N02 | Q2-01 (binding scope/shadowing; physical-declaration cardinality) |
| N03 | Q2-06 (barrier vocabulary), Q2-07 (reasons/acceptance) |
| N04 | Q2-02 (merging/nesting), Q2-03 (discovery), Q2-07 (acceptance rows) |
| N05 | Q2-03 (qualified heritage observability) |
| N06, N07 | Q2-04 (spans/identity/witness/provider versions) |
| N08 | Q2-08 (documentation accuracy) |
| N09 | Q2-05 (publication truth table/consumption) |

The R2 task brief's sections R2-A..R2-F are satisfied through the register
rows below because the register is a strict superset; where the brief and
the register differ, the register governed.

---

## A. Disposition matrix — authoritative register (Q2-01..Q2-08)

### Q2-01 — P1 — Lexical shadowing and invalid-source confirmation

- **Original defect:** R1 §3.2a applied unconditional import-first
  precedence; a heritage reference could be confirmed to an imported
  declaration although a nearer lexical binding exists. A05 required
  confirmation of a conflicting (invalid) source.
- **Evidence:** Q2 report §E.3, §K; fixtures `nested-shadow` (TypeScript
  5.9.3: zero diagnostics; binds function-scope `Local`) and `collision`
  (TS2440); p9 retains the nested heritage.
- **Remediation:** import-first is removed. The lexical-binding rule
  (symbol-binding-contract §3) confirms only module-scope simple-identifier
  class `implements` clauses with an explicit heritage context record and
  exactly one eligible visible binding; deterministic precedence emits
  `nested-scope-heritage`, `type-parameter-shadowing`,
  `ambient-or-global-context`, `recovered-heritage-syntax`,
  `binding-context-unavailable`, `duplicate-local-name`,
  `conflicting-local-binding`, or `ambiguous-local-declaration`; no
  local-first fallback, no ordering-based selection.
- **Documents:** symbol-binding-contract §1.5, §3 (rewritten);
  consumer-query-contract §3.1–§3.4; acceptance-contract A05, A21, A22,
  A23.
- **Acceptance:** A05 (conflict unknown, no target), A21 (shadowing,
  type parameter, missing context), A22 (same-file route), A23 (duplicate
  local names + unrelated nested contrast).
- **Remaining qualification:** none beyond Q3 verification; the supported
  shape is deliberately narrower than TypeScript (`extends`, nested scopes
  and qualification forms stay unknown/observable).
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-02 — P1 — Physical declaration cardinality, merging and visibility

- **Original defect:** R1 §2.2 based uniqueness on declaration *nodes*;
  same-namespace merging converges one node with multiple physical
  declarations (`merged`), same-file cardinality could select invisible
  nested declarations (`nested-export`), and type/value pairs
  (`type-value`) were unaddressed.
- **Evidence:** Q2 report §E.2, §K; fixtures `merged` (one node, two
  physical, two exports), `nested-export` (only `Hidden.Widget`; TS2304),
  `type-value` (interface + const).
- **Remediation:** attribution counts **physical declaration records at
  module scope before symbol-node deduplication** (symbol §2.2); statuses
  `attributed` / `missing-declaration` / `ambiguous-declarations` /
  `ineligible-declaration-context` carry all candidate and ineligible
  record ids; the all-contributors rule rejects conflicting occurrences;
  nested, ambient, recovered and unmodeled records are never candidates and
  are observable; declaration record id is recorded alongside node id and
  `SymbolId`.
- **Documents:** symbol-binding-contract §2, §4; consumer-query-contract
  §3.4 (`unattributed-export`, `ambiguous-export`,
  `ineligible-declaration`); acceptance-contract A03, A22, A24, A26.
- **Acceptance:** A24 (merged/type-value/nested-export), A03 (same-file
  one-node/two-declaration rejection), A22, A26.
- **Remaining qualification:** merged declarations are classified unknown
  (fail closed), not resolved; several public aliases of one unique
  declaration remain legitimate separate attributed entries.
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-03 — P1 — Candidate discovery and observability of unevaluated relationships

- **Original defect:** R1 discovery matched the target's written name
  against heritage/imported names; it missed declarations exported under a
  public alias (`export-alias`), missed the committed `BarrelPreview`
  obligation, and could not report qualified heritage
  (`implements NS.Widget`) because p9 emits no fact for it.
- **Evidence:** Q2 report §F, §I, §K; fixtures `export-alias`,
  `unsupported`, `barrel`; corpus `BarrelPreview.ts`.
- **Remediation:** entry-driven discovery — associated names from the
  target's attributed export entries plus one bounded hop of resolved
  re-export alias association (facts E); candidate occurrences and clauses
  gathered from associated names; occurrence-level unknowns for
  target-associated imports without heritage clauses (the `BarrelPreview`
  obligation → `re-export-barrier`); **fact C stores heritage context
  records for every explicit heritage clause including qualified/
  expression/extends shapes p9 drops**, reported as
  `qualified-heritage-unresolved`, `unsupported-heritage-form`,
  `heritage-kind-not-supported`; structured coverage with a not-evaluated
  frontier; renamed/multi-hop paths disclosed as not evaluated.
- **Documents:** symbol-binding-contract §1.5, §1.6, §3.3;
  consumer-query-contract §3.3, §3.4, §6; scope-proposal R2 revision;
  acceptance-contract A02, A10, A10b, A25, A27.
- **Acceptance:** A02 (export-alias), A10/A10b (barrier family incl.
  BarrelPreview), A25 (qualified/expression/extends), A27 (relevant vs
  unrelated).
- **Remaining qualification:** one-hop alias association only; no re-export
  closure; relevance may over-report unknowns (never confirms).
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-04 — P2 — Spans, identity, lifecycle, witness and provider versions

- **Original defect:** §1.1 `specifierSpan` (module token) could not
  distinguish per-specifier members demanded by §1.2; identity inputs were
  underspecified; spans were not bound to source revisions; resolver
  outcomes freeze per-importer while depending on target inventory; §5
  demanded stored step verification while recommending an unstored join;
  same-file binding contradicted the mandatory five-step proof; heritage
  evidence cited `…abi14.p9` instead of the fact's `…abi14.p8`.
- **Evidence:** Q2 report §E.1, §E.3, §E.4, §K; fixture `multi-specifier`;
  source comment on retained evidence versions; Q1/Q2 provider-version
  findings.
- **Remediation:** distinct `statementSpan` / `moduleSpecifierSpan`
  (token includes quotes) / `bindingSpan` fields with stated coordinate and
  quoting conventions; exact identity inputs
  `(sourceFile, statementSpan, bindingSpan, form, discriminator)`; source
  content hash + index unit + publication binding; resolver join or
  recomputation keyed by `(sourceFile, statementSpan, raw specifier,
  reference form, publication)` with contradiction = integrity failure;
  **computed, versioned witness identity** (`binding1.local-v1`, canonical
  hash over ordered fact ids + policy) — never an invented stored edge;
  same-file short proof (no import/export steps; interface need not be
  exported); heritage evidence cited as `…abi14.p8` with the retention
  rationale.
- **Documents:** symbol-binding-contract §1.1–§1.6, §3.6, §5;
  consumer-query-contract §4; acceptance-contract A01, A06, A13, A14,
  A15, A19.
- **Acceptance:** A13 (witness/physical ids/hashes/same-file/multi-witness),
  A14, A15, A01, A06, A19.
- **Remaining qualification:** representation spelling (computed witness
  vs persisted with dependency ownership) is the ADR-1 decision; the
  correctness obligations are fixed in contract either way.
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-05 — P2 — Publication identity, truth table, integrity and consumption

- **Original defect:** R1 omitted repository/run ids from the result
  publication block; derived `consistent` from the flag alone although the
  constructor permits `true`/null; did not distinguish integrity failures
  from unknowns; left qualification escapable in serialization; A17/A18
  under-specified.
- **Evidence:** Q2 report §G, §K; `CodeGraphStoreModels` constructor
  semantics; `EnsurePublication` full-identity comparison; filesystem
  provider `false`/null.
- **Remediation:** full identity block; mechanical truth table
  (`false`/any → `qualified`; `true`/identified → `consistent`;
  `true`/null → `qualified`, never promoted); explicit integrity failures
  (contradictory endpoints, mismatched unit versions, superseded units,
  missing witness facts, identity conflicts, movement incl. same-content/
  new-run); failed/cancelled re-index retains the last successful
  publication; qualification preserved through serialization with a checked
  consistent-only authority path and navigation labeling.
- **Documents:** publication-consistency-contract §3–§6;
  consumer-query-contract §3.5, §5; acceptance-contract A17, A18, A18b.
- **Acceptance:** A17 (truth table + consumption), A18 (mid-query
  cancellation/movement/failure), A18b (index retention).
- **Remaining qualification:** design choice between qualified results and
  the recorded fail-closed alternative is ADR-2; the truth table and
  integrity rules are fixed either way.
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-06 — P2 — Barrier vocabulary and unresolved export visibility

- **Original defect:** R1 classified any resolved target with outgoing
  `resolves-to` edges as a re-export barrier, mislabeling ordinary
  importing modules; unresolved export sources produced no per-occurrence
  facts and were invisible.
- **Evidence:** Q2 report §J, §K; fixtures `barrel/ordinary.ts` (import-side
  reference only), `barrel/missing.ts` (no edge), `barrel/no-declaration.ts`
  (resolved source, missing declaration).
- **Remediation:** barrier predicate over **fact E export-side records**
  with `form ∈ {export-from, export-type-from, export-star,
  export-namespace-from}` (any resolution status), all matching records
  carried as separate witnesses; ordinary imports classify `missing-export`;
  unresolved/unsupported export references are stored and counted in
  coverage; possible consumers derive only from named/named-aliased fact A
  occurrences; no closure, no invented reference kinds.
- **Documents:** symbol-binding-contract §1.6, §3.4; consumer-query-contract
  §3.2–§3.4, §6; acceptance-contract A08, A10, A10b, A27.
- **Acceptance:** A10/A10b/A27.
- **Remaining qualification:** one hop of alias association for relevance;
  multi-hop and unresolved-origin barriers beyond the frontier are
  disclosed, not resolved.
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-07 — P2 — Categories, ordering, budgets and acceptance rows

- **Original defect:** §3.2/§3.3 category conflict (possible consumer with
  binding failure), candidate/unsupported loss yielding empty results,
  unspecified nested ordering/tie-breaks, return-size bounds conflated with
  execution bounds, A04/A12/A18 contradictions and missing fixtures.
- **Evidence:** Q2 report §F table, §H rows A04/A12/A18, §K.
- **Remediation:** independent relationship categories with mutual
  exclusion per **evaluated relationship** (a class's file may also be a
  possible consumer; a file may hold supported and unknown occurrences);
  occurrence-level unknowns for target-associated imports; closed reason
  table with reachable conditions and fixtures, reserved reasons labeled
  non-emittable; nested total ordering with ordinal/null rules and stable
  tie-breaks; charged-work budgets (candidate/heritage scans, resolver
  joins, attribution lookups, witness steps) separate from
  result-materialization caps, charging before deduplication, exact vs
  lower-bound counts; A04 corrected to `unresolved-heritage-name` + A04b,
  A12 rebuilt with heritage clauses and the inline-type-only positive,
  A18/A18b rebuilt; A21–A28 added; fixture supplements under
  `analysis/fixtures/r2/`.
- **Documents:** consumer-query-contract §3.4, §4, §5;
  acceptance-contract (whole R2 matrix + change log); this document §C.
- **Acceptance:** A04/A04b/A12/A18/A18b and A21–A28.
- **Remaining qualification:** none beyond Q3 verification; reserved
  reasons may become emittable only in a later version that defines their
  reachable condition.
- **Disposition: RESOLVED IN CONTRACT (pending Q3 verification).**

### Q2-08 — P3 — Documentation accuracy

- **Original defect:** recovery §6 said the original environment
  "preserved LF" (it is LF except a final-line CRLF; a pure-LF copy does
  not reproduce the original digests) and reported `FindImplementations` as
  "0 nodes, 0 edges" (it is one seed node, zero edges); the
  `consumer-baseline.md` R1 note misattributed a planning-harness run to
  Q1; `SnapshotIntegrityHash` divergence was not noted.
- **Evidence:** Q2 report §C.2, §K; digest reconciliation (pure-LF digests
  differ); committed observations.
- **Remediation:** narrow in-place corrections marked "(R2 correction)" in
  `recovery-and-provenance.md` §6 and `consumer-baseline.md`; historical
  outputs, commits and raw evidence are untouched.
- **Documents:** recovery-and-provenance.md §6; consumer-baseline.md.
- **Acceptance:** documentation-only; no behavioral assertion.
- **Remaining qualification:** F-01 remains resolved with its evidence-
  wording qualification; original records preserved.
- **Disposition: RESOLVED IN CONTRACT.**

---

## B. Assignment-identifier closure (N01–N09)

Preserving the Q2 report's crosswalk; severities are the authoritative
register's.

| ID | Maps to | R2 closure |
| --- | --- | --- |
| N01 (lexical shadowing / import-first) | Q2-01 | RESOLVED IN CONTRACT — lexical-binding rule; A05/A21/A22/A23 |
| N02 (duplicate local names) | Q2-01 (+ Q2-02 cardinality) | RESOLVED IN CONTRACT — precedence §3.2 steps 9–13; A23 |
| N03 (module-reference vocabulary) | Q2-06, Q2-07 | RESOLVED IN CONTRACT — fact E predicates; A10/A10b/A27 |
| N04 (acceptance matrix) | Q2-02, Q2-03, Q2-07 | RESOLVED IN CONTRACT — R2 matrix + change log + fixtures |
| N05 (qualified heritage coverage) | Q2-03 | RESOLVED IN CONTRACT — fact C + reasons + coverage; A25 |
| N06 (provider-version citation) | Q2-04 | RESOLVED — symbol §5 cites `…abi14.p8` with rationale |
| N07 (reason reachability / `exportSpan`) | Q2-04, Q2-07 | RESOLVED — closed reason table with fixtures/reachability; `exportSpans[]` list with occurrence witnesses; reserved reasons labeled |
| N08 (documentation accuracy) | Q2-08 | RESOLVED — marked corrections, records preserved |
| N09 (qualification derivation) | Q2-05 | RESOLVED — truth table §3.2; A17 |

## C. Reason reachability confirmation (Q2-07)

The closed enumeration in consumer-query-contract §3.4 records, for every
reason, its reachable condition and a fixture; every non-reserved reason is
reachable under the committed Q2 corpora or the R2 supplements
(`analysis/fixtures/r2/`), and `binding-context-unavailable` is reachable
via the declared synthetic publication assertion (A21). `unsupported-export-
form` and `unsupported-declaration-form` are explicitly **reserved and
non-emittable in this milestone**, satisfying the "reachable or labeled
reserved" rule. Reasons are never emitted from invented reference kinds.

## D. Cross-contract consistency audit (R2-G)

| Term | Reconciliation result |
| --- | --- |
| Declaration identity | Module-scope physical declaration record + symbol node + `SymbolId`; counted before deduplication (symbol §2/§4; query §3.1/§3.3; acceptance A03/A24) |
| Import occurrence identity | `(sourceFile, statementSpan, bindingSpan, form, discriminator)`; per specifier; statement/module/binding spans distinct (symbol §1.1–1.2; query §4) |
| Export attribution | Status record with physical cardinality, record ids, occurrence spans; all-contributors rule (symbol §2; query §3.4) |
| Heritage binding | Gate sequence + exactly one eligible route + witness (symbol §3; query §3.1) |
| Lexical scope | `ownerScope`, module/script context, type parameters, ambient, recovered — all recorded and gating (symbol §1.5; query §3.4) |
| Same-file binding | Short proof, no import/export steps, interface need not be exported (symbol §3.5; query §3.1; acceptance A13/A22) |
| Confirmed implementation | Complete witness; different-declaration completions omitted; positive capability pinned (query §3.1; acceptance A07) |
| Possible consumer | Named/named-aliased resolved occurrences into the target file only; module dependency only (query §3.2; acceptance A08) |
| Unknown relationship | Closed reasons with reachability; subjects: heritage, occurrence; never silent (query §3.3–3.4; acceptance A25–A28) |
| Re-export barrier | Fact E export-side predicate; separate witnesses; no closure (query §3.4; acceptance A10/A10b/A27) |
| Coverage completeness | Structured counters + truncated/not-evaluated/complete-within-profile qualification; truncation marks partial (query §6; acceptance A07/A25) |
| Publication qualification | Full identity + truth table + hypothetical true/null → qualified (publication §3; acceptance A17) |
| Source snapshot consistency | Provider-declared; never equated with determinism/reopen/digest (publication §1, §3; acceptance A17 negative) |
| Durable fact integrity | Witness identity recomputed identically on reopen; integrity failures distinct from unknowns (publication §4–§5; acceptance A16/A18b) |
| Truncation and cancellation | Charged-work budgets, exact/lower-bound counts, no partial classified results (query §5; acceptance A18) |

No remaining contradiction was found at the planning-contract level; any
future contradiction is a blocker, not implementation discretion.

## E. Verification log

Recorded in the R2 handoff and `evidence-index.md` (R2 section): Git
identities, change surface, link validation, `git diff --check`, the R2
TypeScript oracle run, and preservation checks. This record is
planning-only; no production behavior is claimed.
