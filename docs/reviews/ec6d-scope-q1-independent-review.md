# EC6D-SCOPE-Q1 — Independent Scope and Acceptance Contract Review

- **Milestone under review:** Penghou.Hetu EC-6D: Consumer Intelligence
- **Reviewer:** independent technical reviewer (review-only assignment)
- **Review branch:** `review/hetu-ec6d-scope-q1`
- **Review worktree:** `C:/Users/Laszlos/source/repos/Penghou.Hetu.review-ec6d-scope-q1`
- **Date:** 2026-10-26

## A. Git binding

### A.1 Identifiers cited by the assignment — verification result

| Identifier | Cited as | Verified state |
| --- | --- | --- |
| Planning branch | `planning/hetu-ec6d-consumer-intelligence` | **Does not exist** — not in the canonical clone (`git branch -a`), not on `origin` (`git ls-remote` lists every ref; no `ec6d`/`planning` ref), not in either PR head (`refs/pull/1/head`, `refs/pull/2/head`, both fetched and searched). |
| Planning commit | `91a3ed53a256e89ed71d7d11c204252c417400a6` | **Object absent.** `git cat-file -t` fails; `git rev-list --all` does not contain it; not present after fetching both PR heads; `git fsck --lost-found` dangling commits are EC-6B artifacts (`f5b2f0a`, `a38ed11`), not EC-6D. |
| Planning tree | `d79e5814485040a36c77bcde332ebc937f312476` | **Object absent.** `git cat-file -t` fails. |
| EC-6C owner-acceptance baseline | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | **Exists.** Branch `decisions/hetu-ec6c-owner-acceptance`; commit `docs(owner-acceptance): record EC6C-OWNER-01 final acceptance attestation`; tree `ece772f1f2b2497891f4019d0d85f4d852b197e1`; parent `5e79418f8b3864e8b829a4be10ab62c6b5744d89`. |
| Canonical main | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | **Exists.** Matches `origin/main` HEAD and the local main worktree. |
| Ancestry | — | **Verified:** `558f8ac` is an ancestor of `2c3090c` (39 commits behind). The EC-6C p9 baseline is a strict descendant of canonical p6 main. |

### A.2 Review binding (deviation is material and recorded)

The assignment instructed branching from the planning commit. That commit does
not exist, so the review branch was created from the EC-6C owner-acceptance
baseline instead — the newest state EC-6D is authorized to build upon:

- **Reviewed commit:** `2c3090cba6d8a56102a5c47ceb026c5b5fea3234`
- **Reviewed tree:** `ece772f1f2b2497891f4019d0d85f4d852b197e1`
- **Parent:** `5e79418f8b3864e8b829a4be10ab62c6b5744d89`
- **Review branch:** `review/hetu-ec6d-scope-q1` (created at the commit above; no force updates, no pushes)
- **Review worktree:** `C:/Users/Laszlos/source/repos/Penghou.Hetu.review-ec6d-scope-q1`
- **Final review commit:** recorded after commit; see the completion note at the end of this document.

The planning worktree, canonical main, and all other worktrees remain
untouched. No merge, rebase, reset, tag, publish, or push was performed.

### A.3 Mandatory planning documents

None of the ten documents listed as mandatory reading exist anywhere in the
repository or its worktrees:

```
docs/planning/ec6d/README.md
docs/planning/ec6d/current-capability-inventory.md
docs/planning/ec6d/integration-delta.md
docs/planning/ec6d/consumer-baseline.md
docs/planning/ec6d/gap-analysis.md
docs/planning/ec6d/risk-register.md
docs/planning/ec6d/scope-proposal.md
docs/planning/ec6d/acceptance-contract.md
docs/planning/ec6d/evidence-index.md
docs/planning/ec6d/analysis/observed-results.json
```

`docs/planning/` does not exist on canonical main, on the EC-6C baseline, or
in any worktree. A recursive search of every Hetu worktree for `EC-6D`/`ec6d`
found only forward references in EC-6A/6B/6C decision documents (deferred
decisions and "next candidate milestone" notes). EC-6D exists in this
repository solely as a *named future obligation*; no planning change set was
ever committed.

## B. Independent verdict

**REJECT** — of the EC-6D scope authorization request *as submitted*.

The change set the assignment asks this review to authorize does not exist in
any verifiable form. There is no scope proposal to accept, no acceptance
contract to test, no evidence to verify, and no git identity to bind. Q1-A
failures at step 1 are not a documentation nit: an authorization that cannot
be bound to an immutable commit cannot be reviewed, re-reviewed, or enforced
later.

This verdict is strictly about the submitted request. It is **not** a claim
that EC-6D is undesirable or infeasible. On the contrary, this review
independently confirmed that the consumer gap EC-6D targets is real,
measured, and substantial (Section C), and Sections D–K provide
evidence-backed technical guidance the planning team can use when it produces
the missing change set.

## C. Baseline reproduction

Reproduced independently at the EC-6C p9 baseline (`2c3090c`), using the
committed review harness (`docs/reviews/ec6d-scope-q1-evidence/harness/`).

### C.1 Existing regression baseline — reproduced

| Suite | Result |
| --- | --- |
| `Penghou.Hetu.Tests` | 160 / 160 passed |
| `Penghou.Hetu.LatticeDb.Tests` | 26 / 26 passed |
| `Penghou.Hetu.TreeSitter.Tests` | 156 / 156 passed |
| `Penghou.Hetu.CSharp.Tests` | 46 / 46 passed |
| **Total** | **388 / 388 passed, 0 failed** |

Command: `dotnet build Penghou.Hetu.slnx && dotnet test Penghou.Hetu.slnx --no-build` on
the review branch. No production file was modified.

### C.2 Claimed observations — independently tested

| Claim (from the assignment's description of the missing planning docs) | Independent result | Method |
| --- | --- | --- |
| `FindImplementations`: 0 of 4 expected direct implementations | **CONFIRMED.** Fixture with one exported interface and four `implements` classes reached via named relative imports: the graph records 4 `implements` edges, and `FindImplementationsAsync(interfaceNode)` returns **0 edges** (1 node: the seed). | Harness scenario A |
| Six direct module-file resolution edges | **CONFIRMED (equivalent fixture).** Six relative-import occurrences produced 6 `resolves-to` edges with `EvidenceQuality.Resolved`, policy `relative-v1` (4 extension-substitution, 1 directory-index, 1 extension-substitution for a type-only import); an unresolved `./ghost` and an external `some-package` were correctly excluded. The rule mix differs from the unavailable planning fixture; the *count and semantics* reproduced. | Harness scenario C |
| Heritage-name matching: 3 TP, 1 FP, 1 FN | **REPRODUCED AS AN EQUIVALENT, NOT AS THE ORIGINAL.** A five-file matrix scored against import-binding ground truth gives a textual name matcher 3 TP / 1 FP / 1 FN — the same failure modes (same-file match, unimported name match, aliased import miss). The planning fixture is unavailable; this is an independently designed equivalent. | Harness scenario B |
| Missing declaration-identity binding | **CONFIRMED.** Five export-entry nodes exist; **zero** edges connect any export entry to a declaration node in either direction; import edges carry **no** properties (no imported name, local name, or span); heritage targets are file-scoped declaration-less placeholders in a separate identity namespace (`typescript:heritage-target:*`). No fact chain connects interface → export slot → importing file → heritage placeholder. | Harness scenarios A, D, G1, G2, G5 |
| Missing consumer relationships | **CONFIRMED.** Incoming traversal from the interface node touches only its own file's `contains`/`declares` edges; nothing from any importing file. | Harness scenario A |
| Missing re-export relationships | **CONFIRMED.** A barrel `export * from "./types"` publishes **no export entry** for the re-exported interface (2 entries total: the original and the consumer's own class); the consumer's import resolves only to the barrel file. | Harness scenario F |
| `PublicationConsistent=false` | **CONFIRMED AS A PROVIDER-DECLARED PROPERTY.** `FileSystemCodeRepositorySource` hardcodes `IsConsistentSnapshot => false` and `SnapshotIdentity => null` (`src/Penghou.Hetu/CodeRepositorySource.cs:254-255`). Publications into both the in-memory store and the pinned LatticeDb store carry `IsConsistentSnapshot=false`. The flag is structural for the only shipped repository provider, not a defect of a particular run. | Harness scenario H; source inspection |
| Repeat-index identity / digest stability | **CONFIRMED (equivalent digest).** Two fresh index runs of the same sources produce byte-identical canonical digests (`db8a28cd…` twice); changing one source changes the digest (`d1d084c2…`). The planning's own digest definition is unknown; this is my canonical digest over all published nodes and edges. | Harness scenario H |
| Durable reopen | **CONFIRMED.** LatticeDb-backed publication preserves all 67 published facts across close/reopen. | Harness scenario H |

### C.3 Directly inspected (not reproduced)

- `EcmaScriptExtractionProfile` (profile revision 9 at the p9 baseline;
  revision 6 on canonical main): heritage emission, identity namespaces,
  import emission, export capture, module-reference capture.
- `EcmaScriptModuleResolver` (`relative-v1`, resolver revision 2): exact /
  extension-substitution / directory-index rules, unresolved/external/unsupported
  classification, per-occurrence edge identity.
- `EcmaScriptExportGraphPublisher` (`exports1.local-v1`): export entry and
  occurrence publication, collision fail-closed behavior.
- `CodeGraphPublicationQuery`: fail-on-change view (`Require` →
  `EnsurePublication` → `CodeGraphPublicationChangedException`); exposes
  `Publication` (including `IsConsistentSnapshot`) but never *rejects* an
  inconsistent publication.
- `CodeIndexingLifecycle`: per-file SHA-256 re-verification at materialization
  (`CodeSourceChangedDuringIndexingException`); publication snapshot identity
  taken from the repository provider.
- Decision documents: `ec6a-owner-acceptance.md` (F01/F07/F12 rows),
  `ec6b-owner-acceptance.md` §8, `ec6c-a29-closure.md`,
  `ec6c-owner-acceptance.md` §5/§7, `ec6c-track-a-owner-acceptance.md` §15
  (deferred EC-6D decisions, D-EC6C-14).

### C.4 Not verified

- The planning change set's "no production implementation" property
  (Q1-A.2): the change set does not exist; nothing to verify.
- The planning fixture's exact observations (heritage 3/1/1, six edges,
  `observed-results.json` contents): the committed fixture does not exist.
- The planning's `PublicationConsistent` field semantics: the field name is
  only known from the assignment; the underlying property
  (`IsConsistentSnapshot`) was verified instead.

## D. Scope assessment

### D.1 Is the proposed first slice worth building?

The consumer question — *given a TypeScript interface declaration, which
explicit implementations and potentially affected importing modules can Hetu
identify?* — is independently confirmed to be **unanswered today** and
**valuable when answered**:

- Scenario A: four unambiguous `implements` relationships exist in the graph
  as structural facts, yet the interface's implementation query returns
  nothing. This is not a hypothetical gap; it is the engine's most common
  inheritance shape.
- Scenario C: module-level consumer relationships (file → file, resolved,
  span-anchored, per-occurrence) already exist and are publication-grade
  facts.
- The gap is therefore *narrow and join-shaped*: the facts needed for a
  meaningful answer exist on both sides (heritage clauses; import
  occurrences; export slots); what is missing is the binding evidence between
  them.

So the *problem* is real. But the *proposed scope as summarized in the
assignment cannot deliver it* without new extraction facts, because the
accepted EC-6C/EC-6B facts record **none** of: (a) which declaration an
export slot belongs to, (b) which names a file's imports bind and under which
local aliases, (c) which declaration a heritage clause denotes. Without at
least one of these, every binding rule degenerates to the textual name
matching that EC-6B's acceptance explicitly rejected ("name equality alone
remains insufficient evidence; separately accepted contract required") and
that scenario G2 shows to be wrong, not merely imprecise.

### D.2 Exclusion-by-exclusion ruling

| Exclusion | Ruling | Basis |
| --- | --- | --- |
| Re-export closure | **Acceptable with explicit consumer-facing limitation** | Scenario F: barrel-imported consumers will be missed or classified unknown. The limitation must appear in query results (an explicit `unknowns`/barrier reason), not only in prose. |
| Ordinary type-use references | **Acceptable with explicit limitation** | The slice answers implementations + importing modules, not all affected code. Files using the interface without importing it (same-file, global scripts, declaration merging) are out; results must say so. |
| Call-site extraction | **Acceptable** | Orthogonal to the implementation question; the profile deliberately emits no `calls`/`references` for ECMAScript. Keeping it out preserves the structural-only contract. |
| Implicit / structural implementations | **Acceptable with explicit limitation** | Duck-typed structural conformance is undecidable without type checking; must be an explicit unknown category, never a silent omission. |
| Package resolution | **Acceptable with explicit limitation** | Scenario C: bare specifiers are classified external by design (`relative-v1`). Interfaces from dependencies will never bind; limitation must be stated. |
| Cross-language resolution | **Acceptable** | `implements` is TypeScript-only (JavaScript grammar reports it not-produced); narrowing to TypeScript matches the grammar's capability surface. |

### D.3 Smallest coherent slice

The smallest slice that demonstrates measurable consumer value is **not** the
one summarized in the assignment; it is that slice **plus the minimum binding
facts** (Section K). A query that joins existing facts alone cannot produce a
single confirmed implementation without name-only matching, which would
violate the accepted EC-6B constraint and fail the alias/duplicate
counterexamples. The milestone therefore must include:

1. per-specifier import occurrence facts (name, alias, type-only, span,
   resolution target);
2. a conservative export-slot→declaration attribution;
3. the confirmed/possible/unknown query contract over those facts.

Everything else in the proposed scope (provenance, explicit unknowns, no
semantic inference) is correct and should be kept.

## E. Symbol resolution assessment

Ground rules from the accepted contracts: EC-6B §8 — "name equality alone
remains insufficient evidence"; EC6C-A2 §15 — "Derived associations cannot be
promoted to Resolved import bindings automatically"; EC-6A F01 — export
binding attribution is an EC-6D obligation.

Classification of the challenged cases (expected EC-6D behavior):

| Case | Expected classification | Current-fact support |
| --- | --- | --- |
| Duplicate interface names in different files | Uniquely bound **only** via the importer's resolved specifier + import name evidence | Path-scoped declaration identity already prevents false convergence (scenario G1: two distinct `Widget` nodes). Binding requires the import side; textual matching would bind both — prohibited. |
| Imported aliases (`import { A as B }`) | Confirmed binding to the *imported* declaration, keyed on local name = written heritage name | Impossible today (no alias facts; scenario G2). A textual matcher binds the **wrong** declaration. This case alone justifies new import-occurrence facts. |
| Local declaration-name shadowing of an import (TS would reject the duplicate) | Unknown / unsupported | Scenario G2 shape; without alias+scope facts the engine cannot tell; must not guess. |
| Multiple imports using the same local name | Unknown unless the resolution target is unique | Requires per-occurrence spans to disambiguate. |
| Named vs namespace imports | Named: bindable; namespace (`* as NS`): **Unknown/possible only** — the written heritage name `NS.Member` is a member expression, and no entry-level binding is derivable without new facts | Scenario G5: forms are recorded but names are not. |
| Default imports / default exports | **Explicitly excluded** from the confirmed category in the first slice (default-entry attribution is Track-A-adjacent and unproven); classify unknown | D-EC6C-07 records default entries exist; attribution does not. |
| Missing target modules (unresolved `./ghost`) | Unknown with reason `unresolved-import`; never confirmed | Scenario C: resolver classifies unresolved; binding must inherit that classification. |
| Missing exported declarations | Unknown with reason `missing-export` | Requires export-entry lookup on the resolved target. |
| Ambiguous export bindings (two export slots, same name, one file — e.g. declaration merging `interface` + `class`) | Unknown with reason `ambiguous-export` | Entry identity is per (path, name); two entries of the same name in one file are possible via merging forms; attribution must fail closed. |
| Declaration merging across files (same global namespace) | Explicitly excluded; unknown | Cross-file identity is not structurally verifiable (profile identity doc). |
| Type-only imports (`import type { T }`) | Bindable for `implements`/`extends` (type position) with type-only provenance | Scenario C: type-only flag is recorded per occurrence. |
| Interfaces extending imported interfaces | Same rule as class implements (`extends_type_clause` already emits `inherits` heritage edges) | Same facts required. |
| Relative imports with extension / index resolution | Supported via resolver rules; binding may proceed | Scenario C verified rules and per-occurrence provenance. |
| Unsupported syntactic forms (`import x = require(...)`, `import type` on the statement with dynamic import, computed heritage names, member-expression heritage in namespaces) | Unknown with reason `unsupported-form`; never guessed | Profile already counts `import x = require` as unsupported and skips expression heritage. |

**Core rule validated by counterexamples:** a textual name match must never
be promoted into a confirmed semantic relationship. Scenario G2 (alias
collision) shows textual matching produces a *wrong confirmed fact*, not just
a missing one; scenario G1 (duplicate names) shows it produces a *spurious
second fact*; scenario FP1 of the matching matrix shows an unimported name
would create a relationship where ground truth has none.

**Do the accepted EC-6C export facts provide enough information?** No. Export
entries carry (path, name, kind, via, projection, localName for aliased
exports, typeOnly) — sufficient to answer "what does this file export", but
with **no attribution to the declaring node** (scenario D: zero attribution
edges), and the import side records **no names at all**. Binding therefore
requires both a new import-occurrence fact and a conservative attribution
rule; both need owner approval because attribution was explicitly deferred to
Track B (EC6C-A2 §15) and this review proposes only a narrow, fail-closed
subset (Section K).

## F. Query contract assessment

The assignment's proposed query must be specified as a **new, explicit
contract**, not an extension of `FindImplementationsAsync`. Reasons:

1. `FindImplementationsAsync` is a bounded traversal with fixed presets
   (`Implements`+`Inherits`, incoming) and existing documented semantics;
   silently changing its result composition would break query regression
   compatibility (388 tests pin current behavior).
2. The EC-6D result is *not* a traversal result: it is a three-part
   classified answer (confirmed / possible / unknown) with per-item
   provenance. Forcing it into `CodeGraphTraversalResult` collapses exactly
   the distinctions (Q1-D) the contract must preserve.
3. The fail-on-change publication view (`CodeGraphPublicationQuery`) is the
   right boundary to inherit: publication identity + consistency flag on
   every envelope, failure on publication movement.

Required semantics (each observable and testable):

- **Input:** declaration identity as `CodeNodeId` (interface node) — the same
  identity consumers already obtain from symbol lookup; ambiguous input
  (multiple candidate nodes) must be rejected or returned as an input error,
  never silently narrowed (mirrors `CodeSymbolLookupResult.IsAmbiguous`).
- **Missing input:** explicit not-found result, not empty success.
- **Confirmed explicit implementations:** each item carries the implementing
  node, the import-occurrence evidence (specifier, imported name, local name,
  span), the resolved target, the export-entry evidence, and the heritage
  span. Every confirmed item must be reconstructible from stored facts.
- **Possible importing modules:** file-level classification from
  `resolves-to` edges (module-level impact), explicitly labelled as *possible
  consumers* — never rendered as implementations or type use (scenario G4
  pins the distinction).
- **Unresolved relationships:** unknown items carry a machine-readable reason
  (`unresolved-import`, `ambiguous-export`, `unattributed-export`,
  `unsupported-form`, `re-export-barrier`, `package-specifier`).
- **Unsupported syntax:** enumerated in unknowns, not dropped.
- **Evidence paths and provenance:** file path + span + provider + provider
  version + evidence quality for every item; canonical, sorted output
  (store-level ordering already sorts by kind/source/target/id — the query
  must define its own total order over the composed result and repeat it).
- **Duplicate suppression:** same heritage clause reached through multiple
  facts must appear once, with deterministic evidence selection.
- **Bounded traversal:** the query must declare and honor
  depth/node/edge budgets like existing traversals; a bound exceeded is
  reported as truncation, never silently as completeness.
- **Cancellation:** cooperative cancellation at the same boundary as existing
  traversals (`ThrowIfCancellationRequested` in loops); partial results must
  never be returned on cancellation — the operation throws.
- **Provider failures:** a plugin failure during re-index does not affect the
  query (it reads the last publication); the query itself must not invoke
  providers.
- **Persistence/reopen:** results are derived from published facts; reopen
  must return identical results for the same publication (scenario H shows
  facts survive reopen; the query adds no mutable state).
- **Category separation:** the three categories must be structurally distinct
  in the payload (separate collections), so a consumer cannot mistake a
  possible consumer for a confirmed implementation by ignoring metadata.
- **Publication qualification:** every envelope carries the publication
  (identity, run, `IsConsistentSnapshot`). See Section G for the required
  minimum behavior on inconsistent publications.

Preservation requirement: existing query defaults and the
`FindImplementationsAsync` preset stay untouched; if the EC-6D query later
wants to *feed* the legacy preset, that is a separate, separately accepted
compatibility change.

## G. Publication consistency assessment

### G.1 What the flag means today

`IsConsistentSnapshot` is a property **declared by the repository source
provider**: "enumeration and reads are guaranteed to use one snapshot"
(`ICodeRepositorySource.IsConsistentSnapshot`). The only shipped provider,
`FileSystemCodeRepositorySource`, hardcodes `false` with a null snapshot
identity — so **every publication this repository can produce today is
declared inconsistent**, on both stores (scenario H). Mitigation that does
exist: per-file content-hash re-verification at materialization; a file that
changes between planning and read aborts the run
(`CodeSourceChangedDuringIndexingException`).

### G.2 Can results combine facts from different source states?

Yes, within one run: each file's facts reflect that file's content at its own
read time; files are read sequentially, so a publication can mix state A of
file 1 (read at t0) with state B of file 2 (read at t1) even though both
hashes verified at read time. Nothing in the publication records per-fact
read state beyond content hashes.

### G.3 Does repeat-index matching provide a stronger guarantee?

It provides *determinism*, not *consistency*: scenario H shows two fresh runs
produce identical digests, and the digest moves when a source changes. That
demonstrates reproducibility of the *pipeline*, and detects post-publish
source drift at the next index — it does **not** prove that any single
publication reflected one coherent source state. Deterministic digests must
not be cited as proof of publication consistency.

### G.4 Can durable reopen preserve inconsistent results?

Yes — scenario H: all 67 facts survive LatticeDb reopen, including the
`IsConsistentSnapshot=false` state. An EC-6D query must therefore expect to
run against a stored, inconsistent publication indefinitely.

### G.5 Minimum acceptable behavior for EC-6D (recommendation)

**Return explicitly qualified results — with structural, non-ignorable
qualification.** Rejecting inconsistent publications outright would make
EC-6D unusable with the only shipped provider (every publication would be
rejected), and requiring an immutable-source snapshot provider is an EC-6E /
provider-strategy concern beyond this milestone's authority. But "qualified"
must not mean optional metadata:

1. The query is only available through the fail-on-change publication view;
   every envelope repeats the publication identity, run id, and
   `IsConsistentSnapshot`.
2. Confirmed implementations are asserted **relative to the publication**:
   "all participating facts were produced in this run from hash-verified
   file contents". The contract must state that run-level snapshot
   consistency is *not* claimed for filesystem sources.
3. Per-file facts (heritage clause, import occurrence, export entry) are
   single-file and hash-verified at read; the cross-run mixing risk
   concentrates in the *join*. The join must therefore use only facts from
   the same index run — true by construction if the query reads the latest
   publication only, and it must be pinned by a test that no query result
   references facts from superseded units.
4. `IsConsistentSnapshot=false` must be surfaced in the primary result type —
   not only in an envelope header — so that a consumer that destructures only
   results still cannot render them as "consistently published".
5. Failure and cancellation semantics at the same boundary: publication
   movement during the query fails with `CodeGraphPublicationChangedException`
   (existing behavior); cancellation throws; no partial classified results.

## H. Acceptance contract assessment

No acceptance contract exists to review (F-01). The following is the minimum
coverage the contract must include, each case stated so the **assertion** is
distinguishable from the **technique**:

| # | Required case | Assertion (must hold regardless of technique) | Counterexample / technique notes |
| --- | --- | --- | --- |
| 1 | Direct named import resolving uniquely | Interface with one importer/one implementer yields exactly 1 confirmed implementation with full provenance | Scenario A upgraded; assert node identity + evidence spans, not edge-count alone |
| 2 | Aliased named import | `import { A as B }` + `implements B` binds to A's declaration; local `B` is not bound | Scenario G2; the assertion *names the correct declaration*, which a textual matcher fails |
| 3 | Duplicate interface names across modules | Only the imported module's interface is confirmed; the other is absent from results | Scenario G1 |
| 4 | Correct explicit implementation binding | Confirmed items carry import span + heritage span + export entry + provider versions | Technique-agnostic: provenance reconstructible from store |
| 5 | False-positive prevention | No confirmed relationship for an unimported same-named interface (scenario FP1); none for wrong-export imports (G3); none for alias-collision wrong target (G2) | Negative assertions |
| 6 | Missing imports / unresolved modules | Unknown with `unresolved-import`; module still classified possible consumer at file level | Scenario C's `./ghost` |
| 7 | Unsupported syntax | `import x = require(...)`, computed/member heritage, namespace-import member use → unknown with `unsupported-form` | Profile already skips these; assert they are *reported*, not silent |
| 8 | Possible-consumer classification without actual-use claim | Idle importer (G4) is a possible consumer and is in no confirmed collection | Structural separation of collections |
| 9 | Evidence provenance for every confirmed relationship | Every confirmed item's evidence resolves to stored facts (paths, spans, provider versions) | Round-trip assertion |
| 10 | Deterministic ordering and canonical results | Two identical queries return identical payloads (byte-identical serialization) | Repeat-call determinism, not just digest |
| 11 | Repeat indexing | Fresh re-index produces identical query results (scenario H digest stability, lifted to query level) | |
| 12 | Durable-store reopen | Same results after close/reopen on the pinned store | Scenario H |
| 13 | Publication inconsistency | Query on a filesystem-provider publication succeeds with `IsConsistentSnapshot=false` surfaced in the result type; no result claims snapshot consistency | Scenario H |
| 14 | Cancellation and failure behavior | Pre-cancelled token throws; publication movement during query throws `CodeGraphPublicationChangedException`; no partial results | |
| 15 | EC-6C p9 fact preservation | All existing export-publication and module-resolution tests pass unmodified; new facts are additive with new provider versions | 156 TreeSitter tests + export/module suites |
| 16 | Existing query regression compatibility | Full existing suite (388 tests) passes; `FindImplementationsAsync` semantics unchanged | Baseline recorded in Section C.1 |

**Missing negative tests the planning must add** (beyond the assignment's
list): the alias-collision wrong-binding case (G2), the duplicate-name
binding case (G1), the wrong-export import (G3), the barrel barrier (F), and
the "digest is not consistency proof" case (G.3). No performance targets
should appear in the contract: no representative real-repository baseline has
been observed for this query, and inventing one would create unfalsifiable
acceptance criteria.

## I. Integration dependencies

### I.1 Verified dependency chain

- EC-6D builds on: EC-6B R1 relative module resolution (`relative-v1`,
  resolver revision 2, `ResolvesTo` edges, `Resolved` evidence) and EC-6C.1/.2
  structural export publication (`exports1.local-v1`, export entries +
  occurrence edges, profile revision 9).
- Both are **owner-accepted with qualifications** at `2c3090c` and **not
  integrated into canonical main** (`558f8ac`, profile revision 6).
- EC-6C's acceptance explicitly does **not** authorize EC-6D
  (`ec6c-owner-acceptance.md` §5, §7) and does **not** authorize integration,
  merging, or release (§5). Canonical main "remains at `558f8ac`; no merge…
  is authorized" (EC-6B §9).

### I.2 Prerequisites, separated

**Before EC-6D implementation:**
1. The EC-6D planning change set must actually be committed and verifiable
   (F-01) — planning remediation first.
2. Owner approval of the new extraction facts and the narrow attribution
   rule (F-02, Section K) — because attribution was explicitly deferred to
   Track B, even the narrow subset requires a recorded owner decision.
3. EC-6C p9 baseline used as the working base (implementation does not
   require EC-6C to be merged first).

**Before EC-6D merge (integration prerequisites):**
4. EC-6C (and EC-6B R1) integrated into canonical main by separate
   authorization; merge order matters because EC-6D's facts assume p9
   extraction and `exports1`/`resolver2` provider versions.
5. p6→p9 migration gates re-run at EC-6D qualification: EC-6D's new facts
   bump the extraction profile revision again (or add a new provider), which
   invalidates prior indexes; the p8→p9 byte-identical-preservation property
   must be re-demonstrated for the new revision. The migration rehearsal
   itself was already demonstrated twice for p8→p9 with committed tooling
   (`docs/evidence/ec6c-owner-acceptance/migration-reproduction/`); a new
   *rehearsal* is required only insofar as the profile revision changes — it
   is a qualification gate, not a new design task.

**Before release / production readiness:**
6. EC-6E durability, incremental invalidation, and resource-bound
   qualification (recorded deferral, EC-6B §8) — explicitly out of EC-6D's
   scope.
7. No published schema change is required if EC-6D facts are additive edges
   under new provider versions; any change to published schemas would need
   separate authorization.

### I.3 Authorization boundaries preserved

This review created one branch (`review/hetu-ec6d-scope-q1`), one worktree,
review documents, and a non-production harness. Canonical main, the planning
worktrees, and all decision branches are untouched; nothing was pushed,
tagged, merged, or released. EC-6C's acceptance is not interpreted as merge
or release authorization anywhere in this report.

## J. Findings register

| ID | Severity | Type | Affected requirement | Evidence | Finding and required action | Disposition |
| --- | --- | --- | --- | --- | --- | --- |
| F-01 | **P0** | Confirmed defect (process/integrity) | Whole assignment; git binding; acceptance contract | §A.1–A.3; `git cat-file`, `git ls-remote`, `git fsck`, remote PR fetches | The planning branch, commit, tree, and all ten planning documents do not exist anywhere (canonical clone, 23 worktrees, reflogs, dangling objects, GitHub remote, both PR heads). The review target cannot be bound to an immutable commit, so no scope proposal or acceptance contract can be reviewed, approved, or later enforced. **Required action:** planning team must commit and publish the EC-6D planning change set with verified identifiers, then resubmit. | Reject authorization; planning remediation |
| F-02 | **P1** | Missing contract definition (verified gap) | Scope sufficiency; binding rules | Harness scenarios D, G2, G5; source inspection of `EcmaScriptExportGraphPublisher`, `EmitImportTarget`, `StructuralTypeNode` | The accepted EC-6C/EC-6B facts cannot support the proposed binding: export entries carry no declaration attribution (0 attribution edges measured), import edges carry no binding names, heritage placeholders are file-scoped and name-keyed. Any binding rule over current facts is textual name matching, which EC-6B acceptance excludes and scenario G2 shows produces wrong confirmed facts. **Required action:** the planning contract must specify the new per-specifier import-occurrence facts and a conservative fail-closed export attribution; both need owner approval. | Remediate in planning before re-review |
| F-03 | **P2** | Unproven assumption | Consumer value of the first slice | Harness scenario A | As proposed, the slice ships without the binding facts, so its headline question ("which implementations?") would still answer 0 of 4 for the canonical fixture shape. Consumer value is conditional on F-02. | Qualification on the planning contract |
| F-04 | **P2** | Missing contract definition | Query semantics; explicit unknowns | Harness scenario A; `CodeGraphTraversalResult` composition | `FindImplementationsAsync` returns a seed-only result (1 node, 0 edges) — indistinguishable from "no implementations exist". The EC-6D query must surface unbound heritage references as explicit unknowns with reasons, and must be a new contract rather than an overload (Section F). | Include in revised contract |
| F-05 | **P2** | Accepted limitation requiring explicit qualification | Publication consistency | Harness scenario H; `CodeRepositorySource.cs:254-255` | `IsConsistentSnapshot=false` and null snapshot identity are structural for the only shipped provider; durable reopen preserves inconsistent publications; repeat-index digests do not prove consistency. EC-6D must adopt the qualified-results contract of Section G.5 (publication identity + consistency flag in the primary result type; same-run-only joins; fail-on-change). | Include in revised contract |
| F-06 | **P2** | Accepted limitation requiring explicit qualification | Re-export closure | Harness scenario F | Barrel `export *` publishes no export entry; consumers importing through barrels cannot be confirmed and must not be silently missed — the query must report the barrier as an unknown reason. | Consumer-facing limitation + unknown reason |
| F-07 | **P3** | Documentation issue | Identity model documentation | Profile revision 5 comment; `docs/ecmascript-symbol-identity.md` | The heritage-placeholder namespace rule ("prevents name-only convergence") is the key property EC-6D builds on; the planning docs should cite it explicitly when defining binding evidence. | Fold into planning docs |
| F-08 | **P3** | Future enhancement | Query ergonomics | `CodeGraphTraversalPresets` | Once the EC-6D query is accepted, a *separately authorized* compatibility change could feed its confirmed results into the legacy implementations preset. Out of scope here; must not happen implicitly. | Deferred |

Category notes: F-01 is a confirmed defect of the *submission* (not of code);
F-02 is a verified missing contract definition with measured evidence; F-03
is an unproven assumption made visible; F-05/F-06 are real limitations that
require explicit qualification rather than scope expansion; F-07/F-08 are
non-blocking.

## K. Revised minimum contract

The evidence supports the following implementation-ready contract. It does
not silently rewrite the original proposal: items 1–2 are **new obligations**
the proposal lacks, and item 5 narrows the proposal's "confirmed" category.
Items marked **[owner approval required]** change accepted EC-6C/Track-B
boundaries.

1. **New extraction fact — import occurrences** (TypeScript only; new
   profile revision or additive provider, never mutating p9 facts): for each
   named import specifier, emit `{ file, resolutionTarget (only for
   relative-v1-resolved specifiers), importedName, localName, isTypeOnly,
   span }`. Namespace/default/`import x = require` occurrences are recorded
   with form only (binding-ineligible). **[owner approval required]**
2. **Conservative export attribution**: an export entry is attributed to a
   declaration node only when the same file contains exactly one declaration
   node with that written name; otherwise the entry stays unattributed and
   is binding-ineligible. No cross-file attribution. **[owner approval
   required — narrow subset of deferred Track B]**
3. **Confirmed implementation** (all conditions, else downgrade): heritage
   clause with written name H and span S in file F; import occurrence in F
   with localName = H, resolutionTarget T uniquely resolved; T has an export
   entry named importedName that is attributed to exactly one interface
   declaration D. Emit a confirmed relationship D ← implementing class with
   evidence = {import span, heritage span, export entry, provider versions,
   `relative-v1` rule}. Type-only imports are eligible (type position).
   Default/namespace/re-export/package paths are **never confirmed**.
4. **Possible consumer**: file-level relationship from `resolves-to` edges to
   T, independent of any binding; explicitly labelled possible-consumer,
   never implementations or type use.
5. **Unknown**: every binding-ineligible heritage clause in a file that
   references the interface's file appears with a machine-readable reason
   (`unresolved-import`, `ambiguous-export`, `unattributed-export`,
   `unsupported-form`, `re-export-barrier`, `package-specifier`,
   `namespace-import`).
6. **Query**: new explicit contract (name it e.g.
   `FindInterfaceImplementations`) on the fail-on-change publication view,
   input `CodeNodeId`, classified three-part result with per-item provenance,
   canonical total ordering, declared budgets with truncation reporting,
   cancellation-throwing semantics, and the publication qualification of
   Section G.5 in the primary result type. Existing queries and presets are
   untouched.
7. **Acceptance**: the 16-case matrix of Section H, including the five
   added negative cases; no performance targets (no observed baseline
   exists).

Owner approval is required for items 1–2 before implementation, and for the
item-3 confirmed-category definition at the acceptance gate.

## L. Recommended next milestone

**Planning remediation.**

1. The planning team must produce and commit the actual EC-6D planning change
   set (branch, commit, ten documents, committed fixtures and
   `observed-results.json`) with verifiable identifiers, incorporating the
   findings of this review (F-01 through F-06, Section K).
2. The owner then decides on items 1–2 of Section K.
3. Resubmit the scope and acceptance contract for an independent Q1 review
   bound to the real planning commit.
4. Owner acceptance, implementation, and integration follow only after that
   re-review. EC-6C integration and any release remain separately
   authorized, and nothing in this review authorizes them.

Scope rejection and redesign is **not** recommended: the underlying gap is
real, measured, and the technical path is clear. Proceeding to owner
acceptance of the *current* proposal is **not** possible, because there is
nothing to accept.

## M. Evidence limits

This review does **not** establish:

- Any property of the actual EC-6D planning change set — it was not found.
  All assessments of "the proposal" rest on the assignment's own summary of
  it, not on the planning documents.
- The planning fixture's exact numbers (heritage 3/1/1, six edges,
  `PublicationConsistent`, canonical-facts digest) — equivalent fixtures and
  a harness-defined canonical digest were used instead; the *shape* of the
  claims reproduced, the specific artifacts did not and could not.
- Behavior on real-world repositories: all fixtures are synthetic; no
  real-repository qualification was attempted (that is EC-6F's recorded
  scope).
- TypeScript semantic correctness beyond the structural contracts reviewed
  (e.g., the G2 ground-truth statement reflects TypeScript's import-shadowing
  resolution for that exact fixture; pathological shadowing combinations were
  not exhaustively surveyed).
- Durability and performance properties beyond the single reopen and
  repeat-index checks performed (EC-6E scope).
- That no other machine or clone holds the planning commit — this review
  establishes only that it is absent from the canonical clone, its
  worktrees, and `origin`, including both PR heads.
- The harness itself was run once end-to-end on this machine; an
  environmental disk-space exhaustion occurred during the review session
  (714 MB reclaimed from review-created build artifacts; the full test-suite
  run and the final harness run completed before and after that event
  respectively). The committed `harness-results.json` is from the final
  successful run.

---

*Review-only artifacts: this document, `docs/reviews/ec6d-scope-q1-evidence/`,
and the branch `review/hetu-ec6d-scope-q1`. No production source, test, or
schema file was modified.*
