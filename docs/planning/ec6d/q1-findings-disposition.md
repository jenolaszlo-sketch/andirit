# EC6D-PLAN-R1 — Q1 findings disposition

Every finding from
`docs/reviews/ec6d-scope-q1-independent-review.md` (branch
`review/hetu-ec6d-scope-q1`, content commit `5ba43e1cd23871c55e01d9201a492a32dff09ae6`,
binding-record HEAD `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc`) is dispositioned
below. Q1 evidence is referenced, not modified. Q1's severity ratings are
preserved; dispositions state what changed in this packet and what remains
for Q2 or later gates.

## Remediation matrix

### F-01 — Missing planning artifact (P0, process/integrity)

- **Disposition:** RESOLVED BY RECOVERY for Q2 readiness. The original
  planning artifact was recovered exactly (commit `91a3ed5`, tree
  `d79e5814`, worktree and mirror intact; `fsck` clean; 33 added paths, all
  under `docs/planning/ec6d/`; zero production changes). The remediation
  branch `planning/hetu-ec6d-plan-r1` branches **from the recovered commit**,
  preserving authentic ancestry: every Q2 check can bind to the original
  identifiers and to this branch's new, truthful identifiers. The Q1 review
  branch was fetched into the planning mirror additively so the review
  evidence is verifiable from one repository.
- **Updated artifact:** [recovery-and-provenance.md](recovery-and-provenance.md);
  this branch itself.
- **Evidence:** `git cat-file`, `git rev-parse^{tree}`, `git fsck
  --connectivity-only`, `git diff --name-status 2c3090c..91a3ed5`,
  `git merge-base --is-ancestor` (all recorded in recovery doc §3).
- **Remaining qualification:** Q2 must independently re-verify the bindings
  and the change surface. F-01 is not "closed" by the existence of new
  files; it is resolved only if Q2 confirms identity, ancestry, and
  completeness of the packet.

### F-02 — Missing symbol-binding facts (P1, missing contract definition)

- **Disposition:** ADDRESSED IN CONTRACT. New contracts specify the two
  missing capabilities without implementing them: (A) per-specifier import
  occurrences with form, names, spans, type-only status, resolution status
  and target; (B) conservative fail-closed export-to-declaration attribution
  (unique same-file, same-local-name rule; re-exports/defaults excluded).
  The five-step binding rule (heritage → import occurrence → relative-v1
  resolution → export entry → attribution) replaces name-only matching;
  import-first precedence resolves the alias-collision case in the direction
  TypeScript semantics require.
- **Updated artifact:** [symbol-binding-contract.md](symbol-binding-contract.md).
- **Evidence:** Q1 scenarios A, D, G1, G2, G5; committed corpus observations
  (5 `implements` edges to file-scoped placeholders; 13 export entries; 0
  attribution; 17 import edges with no binding names).
- **Remaining qualification:** owner approval of the fact contracts and of
  the representation ADR; then Q2 review of the contracts. No implementation
  has occurred or is authorized.

### F-03 — Insufficient consumer value as proposed (P2, unproven assumption)

- **Disposition:** ADDRESSED IN SCOPE. The revised scope requires the
  complete vertical slice (source → attribution → identity → binding →
  query) and pins success to the query boundary: the canonical corpus's four
  intended direct implementations (SvgRenderer, HtmlRenderer,
  AbstractRenderer, AliasRenderer-by-alias) must be returned as confirmed
  results for the supported fixture, with wrong-target, duplicate-name,
  alias, and unknown requirements in the acceptance matrix. Internal fact
  growth alone cannot satisfy acceptance.
- **Updated artifact:** [scope-proposal.md](scope-proposal.md) (R1 revision
  section); [acceptance-contract.md](acceptance-contract.md) (A01–A20).
- **Evidence:** Q1 scenario A (0 of 4); committed corpus expected set;
  committed baseline (0.75/0.75 name-match precision/recall).
- **Remaining qualification:** the four-case expectation is fixture-scoped
  and must not be generalized into an accuracy claim (enforced by contract
  text); real-consumer value confirmation remains an owner decision (R16 of
  the original risk register).

### F-04 — Inadequate existing query semantics (P2, missing contract definition)

- **Disposition:** ADDRESSED IN CONTRACT. New versioned query
  `GetInterfaceImpactV1` with classified result categories (confirmed /
  possible / unknown), closed unknown-reason enumeration, per-relationship
  provenance chains, deterministic total ordering, dedup rules, declared
  budgets with visible truncation, cancellation-throwing semantics,
  fail-on-change publication binding, and reopen determinism. Existing
  queries, presets, and defaults untouched.
- **Updated artifact:** [consumer-query-contract.md](consumer-query-contract.md).
- **Evidence:** Q1 scenario A; Q1 query-contract assessment; existing
  `CodeGraphTraversalResult` composition limits.
- **Remaining qualification:** owner approval; Q2 review; implementation-time
  conformance tests.

### F-05 — Publication inconsistency structural for shipped provider (P2)

- **Disposition:** ADDRESSED IN CONTRACT. Adopted the conservative policy:
  an inconsistent publication must not produce unqualified confirmed impact
  results. Evaluated fail-closed vs qualified-results; recommended
  **qualified results with structurally mandatory qualification**
  (`qualification` field in the primary result type, mechanically derived
  from `IsConsistentSnapshot`), with hard fail-closed boundaries (publication
  movement, invalid input, inconsistent facts, cancellation). Source
  consistency is explicitly separated from durable fact integrity; digest
  stability and reopen survival are documented as non-evidence for snapshot
  consistency.
- **Updated artifact:** [publication-consistency-contract.md](publication-consistency-contract.md).
- **Evidence:** Q1 scenario H; `CodeRepositorySource.cs:254-255`; committed
  baseline `PublicationConsistent=false`.
- **Remaining qualification:** owner approval; Q2 review; A17/A18 acceptance
  tests at implementation time.

### F-06 — Barrel re-export limitation (P2)

- **Disposition:** ADDRESSED IN CONTRACT WITHOUT SCOPE EXPANSION. Re-export
  closure stays deferred. The query detects the barrier from stored facts
  (resolved target has no matching export entry but has outgoing
  `resolves-to` edges — observed for `src/api/index.ts` on the committed
  corpus) and reports `re-export-barrier` unknowns; the mandatory coverage
  statement forbids false completeness. No invented relationships.
- **Updated artifact:** [consumer-query-contract.md](consumer-query-contract.md)
  §3.3, §6; [symbol-binding-contract.md](symbol-binding-contract.md) §3 step 3.
- **Evidence:** Q1 scenario F; committed corpus barrel observations (3
  written re-export names, 0 export entries).
- **Remaining qualification:** owner approval; A10 acceptance test.

### F-07 — Heritage-placeholder namespace rule should be cited in binding evidence (P3, documentation)

- **Disposition:** RESOLVED IN DOCUMENTATION. The binding contract cites the
  rule explicitly (profile revision 5; `docs/ecmascript-symbol-identity.md`)
  and marks it load-bearing: written heritage names live in a
  declaration-less namespace, so binding is always an explicit
  evidence-carrying step and same-spelling convergence never happens
  implicitly.
- **Updated artifact:** [symbol-binding-contract.md](symbol-binding-contract.md) §4.
- **Evidence:** Q1 finding F-07; profile revision history.
- **Remaining qualification:** none for planning; Q2 verifies the citation
  is accurate against the source.

### F-08 — Deferred compatibility: legacy implementations preset (P3, future enhancement)

- **Disposition:** DEFERRED, RECORDED. Reason for deferral: changing preset
  membership or reinterpreting legacy traversal results would alter
  documented query semantics that existing tests pin; it is unnecessary for
  the vertical slice because the new query is the consumer surface.
  Consumer-visible limitation: legacy `FindImplementations` continues to
  return seed-only results for TypeScript interfaces until a separately
  authorized change. Future dependency: any preset integration requires its
  own authorization and compatibility qualification after EC-6D's query is
  accepted. Blocking status: blocks neither implementation nor integration
  of EC-6D; a release claim that the legacy preset now returns
  implementations would be false and is prohibited by this disposition.
- **Updated artifact:** [consumer-query-contract.md](consumer-query-contract.md) §5.
- **Evidence:** Q1 finding F-08; `CodeGraphTraversalPresets`.
- **Remaining qualification:** none for this milestone.

## Cross-cutting disposition notes

- Q1's Section K minimum contract is superseded in one respect: R1 recommends
  persisting per-file occurrence/attribution facts and computing the binding
  join in the versioned query (cleaner single-file invalidation) rather than
  emitting extraction-time binding edges; both options are presented in
  [symbol-binding-contract.md](symbol-binding-contract.md) §1.3/§6 and the
  choice is an explicit ADR gate. Everything else in Q1 §K is preserved.
- Q1's review of the EC-6C baseline (388/388; scenario evidence) is treated
  as evidence about the platform, never as a review of the planning proposal;
  the authentic proposal is now recovered and Q2 reviews it with these
  remediations applied.
