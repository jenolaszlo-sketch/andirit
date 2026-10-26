# EC6D-SCOPE-Q2 — Independent Remediation Verification

- **Milestone under review:** Penghou.Hetu EC-6D: Consumer Intelligence, after
  the EC6D-PLAN-R1 remediation of the EC6D-PLAN-01 recovery.
- **Reviewer:** independent technical reviewer (review-only assignment).
- **Review branch:** `review/hetu-ec6d-scope-q2`.
- **Review worktree:** `C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-scope-q2`.
- **Date:** 2026-10-27.
- **Prior review:** EC6D-SCOPE-Q1 (`review/hetu-ec6d-scope-q1`, content commit
  `5ba43e1cd23871c55e01d9201a492a32dff09ae6`, binding-record HEAD
  `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc`) — verdict: **REJECT** of the
  submission (missing planning artifact), with findings F-01..F-08.

## A. Git binding

### A.1 Reviewed source (R1 remediation tip)

| Item | Value |
| --- | --- |
| Reviewed commit | `0e1b213737d42c74df20aef4744ab5718058e266` (`planning/hetu-ec6d-plan-r1`) |
| Reviewed tree | `ddf777aa155852daca40b6ed8748c028c67d5822` |
| Parent (direct) | `91a3ed53a256e89ed71d7d11c204252c417400a6` (recovered planning tip) |
| Recovery base commit/tree | `91a3ed5…` / `d79e5814485040a36c77bcde332ebc937f312476` |
| Recovery base parent | `3b5e8d91e89e082fd236452656c1d66a4afd3662` |
| Planning ancestry | `2c3090cb` → `1c9f4c9` → `3b5e8d9` → `91a3ed5` (linear, 3 planning commits) |
| EC-6C owner acceptance | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` (tree `ece772f1…`) |
| Canonical main | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (live `origin/main` verified 2026-10-27) |
| Review worktree HEAD at start | `0e1b213…`, branch `review/hetu-ec6d-scope-q2` (clean) |

The review worktree/branch existed at exactly the reviewed source commit when
this session began (prepared during environment setup), and
`docs/reviews/ec6d-scope-q2-evidence/` already contained a pre-existing,
setup-generated untracked evidence toolkit (preserved untouched; see §N). No
additional worktree was created; the R1 worktree, original planning worktree,
Q1 worktree, canonical clone, and canonical main were used read-only.

### A.2 Final review binding

| Item | Value |
| --- | --- |
| Review-content commit (final review commit) | `575712074cae22ea98312864b78c950ecc56e411` (`docs(review): add EC6D-SCOPE-Q2 independent verification`) |
| Review-content tree | `209d0df6c3131b1e814cff0e05cba46953e5568a` |
| Final review parent | `0e1b213737d42c74df20aef4744ab5718058e266` (reviewed R1 source commit) |
| Binding-record commit (branch HEAD) | created immediately after the content commit (`docs(review): record final review binding and validation log`); does not alter review content; hash reported in the Q2 completion record |

*The content commit carries the complete review; the binding-record commit
fills these Git-binding rows and the validation log only (the Q1 pattern).
The filled values are also recorded in
[ec6d-scope-q2-evidence/README.md](ec6d-scope-q2-evidence/README.md).*

## B. Verdict

**REMEDIATION REQUIRED** — bounded, targeted remediation of specific
contract and acceptance defects (Q2-N01..Q2-N08) before implementation can be
authorized. This is **not** a rejection of the recovery, the architecture, or
the scope, and **not** a demand for redesign:

- The recovery and provenance claims are **verified exactly**, including a
  bit-exact causal reproduction of both checkout environments (§C).
- F-01 is **RESOLVED**; F-02..F-08 are resolved at the planning-contract level
  with qualifications (§D).
- The proposed fact contracts, five-step binding rule, query contract, and
  publication policy form a coherent and implementable direction (§E–G).
- However, the contract as written contains **one false-confirmation path in
  the core binding rule for valid TypeScript (P1)** and several P2 contract /
  acceptance ambiguities which would force implementers to invent
  correctness-critical semantics or implement inconsistent test expectations
  (§E, §F, §H, §K). The decision rules therefore exclude both ACCEPT (a new
  P1 remains) and ACCEPT WITH QUALIFICATIONS (the contracts are not yet
  technically sound at the identified points; targeted amendments — not just
  owner attention — are required).

The required remediation is small, text-level, and fully specified in §K/§L.
No production implementation is authorized by this review.

## C. Recovery verification (Q2-A)

All checks below were executed by the reviewer during this session. The
planning mirror is `C:/Users/Laszlos/source/repos/Solo/.hetu-ec6d-planning-mirror.git`
(bare, own object database; `remote.origin.url` = a local bundle path; `main`
= `558f8ac8…`).

### C.1 Authenticated identities

| Check | Result |
| --- | --- |
| Planning commit exists | `git cat-file -t 91a3ed5…` → `commit` (mirror) |
| Planning tree matches | `91a3ed5^{tree}` = `d79e5814485040a36c77bcde332ebc937f312476` (exact) |
| Planning parent matches | parent = `3b5e8d91e89e082fd236452656c1d66a4afd3662` |
| Artifact completeness | `git diff --name-status 2c3090cb..91a3ed5` → **33 added paths, all under `docs/planning/ec6d/`**, 0 deletions; 9,852 insertions; empty diff under `src`, `tests` |
| Fixture corpus | tree `014da71f0855cdc957fa9b6a0667bca565145092`, **identical at `1c9f4c9`, `3b5e8d9`, `91a3ed5`, and `0e1b213`** |
| Harness + observations | `analysis/Program.cs`, `ConsumerBaseline.csproj`, `observed-results.json`, four test logs present at the original commit |
| Ancestry | `merge-base --is-ancestor`: `2c3090cb` → `91a3ed5` ✓; `558f8ac8` → `2c3090cb` ✓; `2c3090cb` → `0e1b213` ✓; `558f8ac8` → `0e1b213` ✓ (all exit 0) |
| R1 parent | direct parent of `0e1b213` is exactly `91a3ed5` |
| R1 change surface | `git diff --name-status 91a3ed5..0e1b213` = 8 modified + 6 added files, **all under `docs/planning/ec6d/`**; `src/tests/benchmarks/smoke/.github` diff empty; `git diff --check` clean for both commits |
| Rewrite check | R1 diff is pure insertion except `README.md` (32+/11−, navigation/status lines only); no existing evidence or measurement was rewritten |
| Worktree isolation | Original planning worktree and R1 worktree both **clean** at their recorded commits; Q1 worktree clean at `d2bb7b1`; canonical clone `Penghou.Hetu` clean at `558f8ac`, contains the Q1 branch but **no** planning/R1 objects |

### C.2 The Q1-miss explanation — verified

- The canonical clone (`Penghou.Hetu`, shared object DB with its own
  worktrees) does not contain `91a3ed5`, `0e1b213`, or `2c3090cb` (all
  `cat-file` fail) — a genuinely separate object database.
- The planning worktree's `.git` file resolves into the mirror's
  `worktrees/` administrative area (`hetu-ec6d-consumer-intelligence`).
- The mirror's `origin` is a local bundle path (never GitHub).
- **Live origin check (2026-10-27):** `git ls-remote --heads origin`
  (`https://github.com/jenolaszlo-sketch/penghou-hetu.git`) returns exactly
  9 heads; **no `ec6d`, `planning`, `plan-r1`, or `hetu-*` ref exists**, and
  `refs/heads/main` = `558f8ac8…`. The planning branch was never pushed.
- The Q1 review branch was fetched into the mirror **additively**
  (`d2bb7b1…` exactly matches the Q1 worktree HEAD; reflog shows a single
  branch-creation entry; Q1 branch contents match Q1's committed evidence).

### C.3 Cross-environment digest reconciliation — verified causally

The reviewer re-ran the committed harness three ways (full outputs in
[evidence/runs/](ec6d-scope-q2-evidence/runs/)):

| Run | Input bytes | IndexIdentity | CanonicalFactsSha256 |
| --- | --- | --- | --- |
| Original observation (committed) | original worktree (LF + final-line CRLF) | `667ec2d4…77ec8` | `05bf7c7d…b0d3c` |
| R1 fresh run (committed) | R1 worktree (all-CRLF) | `5c866e16…3f5eb` | `631c0ab3…ecb29` |
| **Q2 run 1** — fresh checkout (CRLF) in review worktree | all-CRLF | **`5c866e16…3f5eb` (exact)** | **`631c0ab3…ecb29` (exact)** |
| **Q2 run 2** — reconstructed original bytes (14/14 byte-identical to the original worktree) | LF + final-line CRLF | **`667ec2d4…77ec8` (exact)** | **`05bf7c7d…b0d3c` (exact)** |

Additional verified facts:

- Committed blobs are byte-identical across all commits
  (`git hash-object` of every corpus file equals the commit blob; e.g.
  `src/other/Renderable.ts` → `79fddb0a7de07f9245671c981e540bd26de0576a`).
- Q2 run 1 vs R1's committed run: **only the `SnapshotIntegrityHash`
  line differs** (run metadata); all 64 node identities and all 99 edge
  identities, every query result, and every diagnostic are byte-identical.
- Q2 run 1 vs the original observation: node identity sets and edge identity
  sets **equal**; counts equal (14 units / 73 node contributions / 23
  declarations / 99 edges; mix 23/23/13/5/17/1/1/16); `FindImplementations`
  seed-only (1 node, 0 edges) in both; `PublicationConsistent=false` in both.
- Byte-level cause: original working files are LF except the final line
  break (CRLF); fresh checkouts are all-CRLF; `git hash-object` (filtered)
  is identical; `--no-filters` hashes differ. The digest divergence is fully
  explained by working-tree normalization of the file-node `content-hash`
  property; graph identities are environment-invariant.

**F-01 verdict: RESOLVED.** The "RECOVERED EXACTLY" claim is authentic, the
Q1-miss explanation is independently confirmed, and both sides of the digest
divergence are reproduced bit-exactly. Minor wording imprecisions noted in
§K (Q2-N08).

## D. Q1 findings reconciliation (F-01..F-08)

Disposition categories: RESOLVED / RESOLVED WITH QUALIFICATIONS / OPEN /
NOT VERIFIED. "Planning-level" dispositions do not claim any implementation.

| ID | Q1 sev. | Original observation | R1 remediation | Independent verification | Remaining deficiency | Disposition |
| --- | --- | --- | --- | --- | --- | --- |
| F-01 | P0 | Planning artifact absent everywhere Q1 could see; no reviewable identity | Recovery: exact commit/tree/worktree recovered from the planning mirror; R1 branches from it; Q1 branch fetched additively | Fully reproduced by Q2 (§C): identities, ancestry, 33-path surface, fixture tree, clean fsck (no missing objects), bit-exact dual-environment reproduction, live-origin absence confirmed | None affecting the finding; wording imprecisions in two R1 tables (Q2-N08) | **RESOLVED** |
| F-02 | P1 | Accepted facts cannot bind imports to declarations without prohibited name matching | New contracts: per-specifier import occurrences (fact A), fail-closed export attribution (fact B), five-step binding rule, provenance chain | Contracts exist and align with actual p9 facts (module-reference values, export local-name, per-occurrence identities verified); **but** a scope-shadowing false-confirmation path (Q2-N01), a multi-occurrence rule gap (Q2-N02), and qualified-heritage silence (Q2-N05) remain | Q2-N01/N02/N05 amendments required before implementation | **RESOLVED WITH QUALIFICATIONS** (planning-contract level) |
| F-03 | P2 | Slice as proposed would still answer 0-of-4; consumer value unproven | Scope revision pins success at the query boundary: four canonical confirmed implementations (A07); fixture-scoped claim | Contract-level only (no EC-6D code exists); corpus expectation matches the human-reviewed fixture and the rule | Implementation not verified (expected); R16 demand question remains open | **RESOLVED WITH QUALIFICATIONS** |
| F-04 | P2 | Legacy query cannot express classified answers; unknowns must be explicit | `GetInterfaceImpactV1` contract: classified categories, closed reasons, ordering, dedup, budgets, cancellation, provenance | Contract is precise and implementable; two reason-mapping/predicate defects and acceptance gaps found (Q2-N03/N04/N07) | Amendments required; A04/A10/A12/A18 corrections | **RESOLVED WITH QUALIFICATIONS** |
| F-05 | P2 | Inconsistent publications are structural; results must not overclaim | Qualified-results policy; mandatory qualification in primary result type; fail-closed boundaries; separation of source consistency vs fact integrity | Design accepted as safe with explicit safeguards (§G); derivation nit (Q2-N09) and A18 provider-failure test gap | Q2-N09 correction; A18 assertion fix | **RESOLVED WITH QUALIFICATIONS** |
| F-06 | P2 | Barrel consumers must not be silently missed | Barrier detection from stored facts; `re-export-barrier` unknown; mandatory coverage statement | Barrier detection is feasible for both `export-from` and `export-star` (probes #4/#5); the predicate text is imprecise (Q2-N03) and A10 is unsatisfiable with the committed corpus as specified (Q2-N04) | Predicate + fixture corrections | **RESOLVED WITH QUALIFICATIONS** |
| F-07 | P3 | Heritage-placeholder namespace rule should be cited as load-bearing | Cited in symbol-binding-contract §4 (profile revision 5; identity doc) | Citation verified against source: revision-5 comment
(`EcmaScriptExtractionProfile.cs` lines 39-41) and the file-scoped `heritage-target` identity construction | None | **RESOLVED** |
| F-08 | P3 | Legacy preset must not change implicitly | Explicit deferral with reasons/limits; A20 pins preset behavior | Preset verified unchanged: `Implementations = [Implements, Inherits]`; additive/opt-in requirements recorded | None for this milestone | **RESOLVED** |

No Q1 finding is OPEN or NOT VERIFIED.

## E. Symbol-binding assessment (Q2-B)

### E.1 Fact contract A — import occurrences

Assessed against the actual p9 fact model (source + three independently
executed harness runs; see §I):

| Required property | Contract treatment | Assessment |
| --- | --- | --- |
| Identity stability | Per-specifier occurrence; identity includes file + statement span + specifier span; "discriminator-hashed edge" following the p9 occurrence-edge pattern; ADR defines exact identity inputs | Sufficient at contract level. Precedent verified: two identical specifiers in one file produced two distinct p9 resolution edges (`484a7821…` / `bd4b1103…`). |
| Span interpretation | "UTF-16 span … existing coordinate conventions"; acceptance contract states one-based, end-exclusive UTF-16 | Verified against source spans (`SameFileC` heritage `5:35-5:41`, `TypeParamC` `3:48-3:58` etc.). Note: line-ending changes move byte offsets but not line/column positions — consistent with the observed environment-invariant identities. |
| Source version/revision binding | Provenance same model as p9 occurrence edges (origin carries index unit + source hash) | Sufficient; matches p9 `OriginView` evidence. |
| Multiple specifiers / aliases / type-only | Explicit: per-specifier; importedName/localName; `isTypeOnly` eligible for `implements` | Sufficient. |
| Missing modules / unsupported forms | `resolutionStatus` enum; unsupported forms recorded, never dropped, binding-ineligible | Sufficient **as a contract**; p9 currently records only aggregate counters for e.g. `import x = require`, so this is new obligation (correctly identified as such). |
| Duplicate local names | Occurrences stay distinguishable; query reason `duplicate-local-name` exists | Distinguishability verified; **but the binding rule does not say to refuse confirmation** when multiple occurrences introduce the same local name (Q2-N02). |
| Path normalization | `moduleSpecifier` written text unchanged; `resolvedTarget` = existing file node identity | Sufficient (normalization owned by the accepted resolver). |
| Persistence / repeat-index determinism | Single-file unit ownership; additive edges; A15 pins equivalence within byte-identical checkouts | Sufficient; consistent with verified p9 lifecycle and the F-01 reconciliation. |

### E.2 Fact contract B — export attribution

Adversarial probes (independently executed; artifact
`ec6d-scope-q2-evidence/fixtures/adversarial/`, run output committed):

- **Merged interfaces (`interface Merge` ×2, one file):** the identity model
  converges same-namespace same-name declarations into **one** declaration
  node and one `(path, name)` export entry (two occurrence edges). The
  "exactly one declaration" rule therefore attributes correctly. The contract
  wording ("merged interface + class, or overload sets that emit separate
  declaration nodes") does not describe this actual shape.
- **Type/value merging (`interface Mix` + `class Mix`):** **two** declaration
  nodes, one export entry. The rule correctly refuses (ambiguous); this is
  fail-closed and detectable. No acceptance row tests it (Q2-N04).
- **Aliased local export (`export { Q as QRename }`):** entry carries
  `export-via=local`, `local-name=Q` — the local-name side the rule needs
  exists at p9. Implementable.
- **Multiple occurrence edges per entry** (`Merge` ×2): the contract's
  singular `exportSpan` is underdefined for multi-occurrence entries
  (Q2-N07).
- **Candidate-set definition:** the rule must count **all** declaration node
  identities carrying the local name (any namespace), or interface+class
  merging would be wrongly attributed to the interface alone. This definition
  is currently implicit (part of Q2-N04/N07 remediation).

No case was found where the uniqueness rule produces an *incorrect*
attribution given a correctly defined candidate set; failures are
conservative. Declaration merging does not break the rule; the rule text is
imprecise about the model it must consume.

### E.3 Import-first precedence — challenged (key finding)

The direction of import-first is **correct for module-scope references in
valid TypeScript**, and the G2 fixture validates that direction. However, the
rule as written only checks "file F has an import occurrence with
`localName = H`" — it has **no scope condition**, while the extraction
demonstrably emits heritage facts for nested declarations:

- **Q2 adversarial probe `NsShadow.ts` (valid TypeScript):**
  `import type { Renderable as Drawable } from "../contracts/Renderable"`
  plus `namespace NS { interface Drawable {…}; class NsC implements Drawable }`.
  Observed p9 facts: nested namespace/members and the heritage fact
  `NS.NsC → heritage-target Drawable (span 8:31-8:39)` are all emitted; the
  future import occurrence would carry `localName=Drawable`; the target's
  export entry and attribution are unique. Under the literal rule, the query
  would **confirm `NsC implements Renderable`**, while TypeScript resolves
  `Drawable` to `NS.Drawable` (different declaration; the class does not even
  structurally conform). This is a **false confirmed result for legal
  syntax** — the exact failure class the milestone exists to prevent.
- **Secondary probe `TypeParam.ts`** (`class TypeParamC<Renderable> implements
  Renderable`): heritage fact is emitted despite class type-parameter
  shadowing. This is semantically invalid TypeScript; the contract never
  states how semantically invalid sources are treated.
- **Compiler verification (Q2-authored oracle, TypeScript 5.9.3):**
  `ns-shadow` produces **zero diagnostics** and the heritage name binds to the
  **namespace-local `Drawable`** — the counterexample is valid TypeScript and
  the correct target is the local declaration, not the import.
  `collision` (the G2/A05 shape) reports TS2440 ("Import declaration conflicts
  with local declaration") — it is invalid TypeScript — and TypeScript's
  resolver still associates the clause with the imported symbol when forced.
  `type-param` reports TS2422 and binds to the class's `TypeParameter`.
  Artifacts: `ec6d-scope-q2-evidence/runs/typescript-oracle-q2.json`
  (script, cases and outputs committed).
- **Multiple conflicting imports:** covered by Q2-N02 (must fail closed).
- **Heritage expressions (`implements NS.Member`):** the profile emits **no**
  heritage fact for qualified `implements` names (verified by probe and by
  `HeritageName` source: identifier/type-identifier/member-expression only;
  qualified type grammar shapes are not handled). They silently disappear —
  see Q2-N05.

**Stricter alternative (recommended):** require, for the confirmed category,
that (i) the heritage clause's source declaration is a declaration contained
**directly by the file node** (file-scope), and (ii) no declaration carrying
the written name is contained by any scope enclosing the heritage clause
(excluding the import itself); otherwise the clause is binding-ineligible →
unknown (new reason, e.g. `nested-scope-shadowing`). Condition (i) alone
removes the legal-TypeScript counterexample class; document residual
type-parameter cases as out of the supported profile (semantically invalid
sources) or add an extraction signal. `contains` edges verified in the probes
provide the needed scope facts (e.g. `NS → NS.Drawable`, `NS → NS.NsC`).

### E.4 Five-step binding chain — identity constraints

The chain is sound and every step can cite stored facts; the following
constraints are missing or imprecise and must be added (Q2-N01..N03/N05):

1. Step 1 lacks the **scope condition** (E.3).
2. Step 2a lacks the **multi-occurrence uniqueness rule** (E.1).
3. Step 3's barrier predicate ("outgoing `resolves-to` edges") is satisfied
   by ordinary importing modules; it must be
   `module-reference ∈ {export-from, export-type-from, export-star,
   export-namespace-from}` — the complete re-export set from the documented
   seven-value enumeration
   (`docs/ecmascript-module-resolution.md:66`; `export-from`/`export-star`
   verified present in p9 facts by probe and corpus; the R1 text's
   `export-from`-only framing is incomplete).
4. Steps 3–4 need the **attribution candidate-set definition** and the
   multi-occurrence `exportSpan` definition (E.2).
5. Step 4's `ambiguous-export` reason is unreachable as defined (attribution
   is unique-only); the merging case surfaces as `unattributed-export`,
   losing the ambiguity signal (Q2-N07).

### E.5 Representation decision (ADR)

R1 recommends query-time joins over per-file occurrence/attribution facts
(alternative: extraction-time binding edges). Independent assessment:

- **Correctness:** equivalent if the query joins facts from exactly one
  publication and the fail-on-change view pins it; single-file fact ownership
  keeps invalidation local to units. Extraction-time binding edges would be
  cross-file facts requiring cross-unit invalidation (a correctness hazard
  under incremental replacement) — the recommendation is the safer design.
- **Determinism:** join order and dedup are fixed by the query contract §4;
  facts are deterministic (verified at p9); bounded candidate gathering must
  also be declared (minor addition).
- **Persistence:** additive facts; no schema change expected; A16 pins
  reopen; store-level duplicate-identity fail-closed behavior verified
  (`EcmaScriptExportGraphPublisher` throws on collisions with different
  facts).
- **Traceability:** each step is a stored fact + evidence identity (§5),
  with the provider-version citation fixed per Q2-N06.
- **Incremental behavior / ambiguity propagation:** the required test is
  "no result references facts from superseded units" (contract §3.4) plus a
  bounded-gathering statement; both are testable.

**Conclusion:** the ADR may reasonably be **finalized at owner acceptance**
— no further design investigation is needed — provided the owner decides it
explicitly (adopt the recommended query-time join, or justify an equivalent
that preserves single-file ownership and one-publication joins) and the
acceptance contract pins the superseded-units and bounded-gathering
properties. It must not be deferred into implementation.

## F. Query contract assessment (Q2-C)

`GetInterfaceImpactV1` is correctly specified as a new, versioned contract
distinct from `FindImplementationsAsync`, on the fail-on-change publication
view. The four semantic areas are structurally separated and mechanically
enforceable, with these findings applied:

| Q2-C question | Assessment |
| --- | --- |
| 1. Direct import of an unattributable declaration as possible consumer? | Yes by design ("module-level potential dependency only"); definition must bind to `module-reference ∈ {import, import-type}` facts (Q2-N03) so re-exporting modules are not mislabeled as importers. |
| 2. Ambiguous heritage ever confirmed? | No — confirmed requires the full chain; ambiguity conditions are enumerated. Verified sound with the E.3/E.4 constraints added. |
| 3. Missing evidence read as no impact? | Mitigated by mandatory coverage statement with candidate/unbound counts and explicit unknowns; A07/A10/A11 pin negatives. Sound once A10 is corrected (Q2-N04). |
| 4. Unsupported syntax disappearing? | **Partially defective.** Default/namespace/import-equals forms are recorded (future fact A) and reportable; **qualified/member heritage names are not extracted at all** and cannot be reported (Q2-N05). |
| 5. Incomplete coverage vs complete empty result? | Yes — coverage statement + candidate counts distinguish; empty-well-formed result is defined. |
| 6. Sufficient provenance? | Yes — five-step chains with edge ids/spans/providers; per-item provenance required. |
| 7. Ordering and dedup? | Yes — deterministic total order; per-class/per-file dedup with deterministic merges. |
| 8. Budgets and truncation observable? | Yes — budgets declared, truncation state + omitted frontier count; add bounded candidate-gathering wording (E.5). |
| 9. Cancellation/provider failure partial results? | Yes — cancellation throws; query never invokes providers; fail-on-change on movement. A18's test does not cover provider-failure semantics (Q2-N04). |
| 10. Durable reopen preserves identities? | Yes — facts persist (Q1 scenario H), query is stateless; A16 pins. |

**A04/A10/A12/A18 defects** are detailed in §H and §K (Q2-N04). The contract
is otherwise sufficiently precise to implement without additional semantic
decisions, once Q2-N01..N05/N07 are applied.

## G. Publication consistency assessment (Q2-D)

**The qualified-results design is accepted, with explicit safeguards, as
safe and unambiguous at the contract level.** Rationale and boundaries:

- `qualified` vs `consistent` is mechanically derived from the provider
  declaration and is a first-class field of the primary result type; the
  coverage statement embeds it; A17 asserts presence and the "relative to
  publication P" framing. A consumer can still ignore a field it is handed —
  that is a consumer obligation stated normatively (§5); the contract cannot
  prevent malice.
- Relationship uncertainty (confirmed/possible/unknown) and publication
  consistency are never conflated; neither dimension is allowed to absorb
  the other.
- Fail-closed boundaries verified as implementable: publication movement
  (`CodeGraphPublicationChangedException` via `EnsurePublication` — source
  verified), invalid input, internal fact inconsistency (store/publisher
  collision behavior verified), cancellation (throw, no partial results).
- **Critical requirement answered:** a structurally inconsistent publication
  can never produce an *unqualified* confirmed result — the only available
  qualification value with the shipped provider is `qualified`, and no
  digest/reopen claim may substitute for consistency (§1, A17 negative).
- **Correction (Q2-N09, P3):** the derivation
  `qualification = isConsistentSnapshot ? "consistent" : "qualified"`
  ignores `SnapshotIdentity`; the provider interface documents the identity
  as "when available" and does not guarantee non-null for consistent
  providers. Derive `consistent` only when `IsConsistentSnapshot == true &&
  SnapshotIdentity is non-null` (or validate/fail closed on the inconsistent
  combination).
- **Implications for acceptance:** A17 is sound subject to N09. A18 is
  defective: its title covers provider failures, but the assertion text only
  covers cancellation and publication movement; add the re-index-failure
  semantics (failed/cancelled re-index retains the last successful
  publication; a bound query still answers with unchanged qualification).

## H. Acceptance matrix audit (A01–A20)

Legend — basis: **[E]** verified existing behavior/defect at p9 (executed by
Q2), **[C]** contract-level criterion (no implementation yet, assertions
internally consistent), **[D]** defective as written, **[M]** missing
coverage. Status: ✅ sound / ⚠️ sound with corrections / ❌ defective.

| ID | Preconditions & fixture | Assertions (pos/neg) | Boundary | Basis | Status |
| --- | --- | --- | --- | --- | --- |
| A01 | Supported publication; facts A+B | +Svg/Html confirmed; −other/Renderable never target | binding+query | C (corpus facts [E]) | ✅ |
| A02 | Alias occurrence | +AliasRenderer via Drawable→Renderable; −literal Drawable never an entry | binding+query | C | ✅ |
| A03 | Both files indexed | +two distinct identities; −no name-only convergence | extraction+binding | E (G1 + corpus) | ✅ |
| A04 | "Q1 G3 (imports Gadget, implements Widget)" | −no confirmed; expected reason **`missing-export`** | binding | E (G3 shape verified) | ❌ reason mismatch: the rule yields `unresolved-heritage-name` for the named shape; split into (i) heritage not bound by any import and (ii) bound import whose target lacks the export name |
| A05 | "Q1 G2" (import alias + local same-name interface) | +binds imported target; −local interface never target | binding | E (G2) | ⚠️ fixture is semantically invalid TS; keep as deterministic-policy case but add the **valid** shadowing case (NsShadow) asserting unknown per Q2-N01 |
| A06 | Facts A+B | +chain to exact declaration id; −placeholder never reported as target | binding | C | ✅ |
| A07 | Full corpus | +exactly 4 confirmed; −OtherRenderer/CanvasRenderer absent; fixture-scoped | query | E (source semantics) + C | ✅ |
| A08 | resolves-to facts | +Preview once as possible consumer; −not confirmed | query | E (facts) + C | ✅ (predicate precision per Q2-N03) |
| A09 | Idle import (G4) | +possible consumer; −never claims type use | query | E + C | ✅ |
| A10 | "BarrelPreview via src/api/index.ts" | +`re-export-barrier` unknown; −not confirmed/omitted | query | E | ❌ unsatisfiable: BarrelPreview has **no heritage clause**, and its import resolves to the barrel, not the target file, so no candidate triggers the barrier under the contract's candidate rules; use a class-through-barrel fixture (Q1-F style) and cover the star case |
| A11 | Unresolved specifier | +`unresolved-module` unknown; −no binding cites unresolved/external | binding+query | E (corpus) + C | ✅ |
| A12 | "Q1 G5 (default, namespace, inline type-only)" | unsupported recorded; heritage naming such bindings unknown `unsupported-import-form`; −never confirmed | extraction+query | E + C | ❌ G5 contains **no heritage clause** (cannot exercise the negative), and "inline type-only" is *eligible* under the contract, not unsupported; split the row |
| A13 | A07 result | five-step provenance; steps verifiable; −no missing facts | query | C | ✅ |
| A14 | Same publication repeated | identical ordered results; total order | query | C | ✅ |
| A15 | Two runs, unchanged sources | identical identity sets + equivalent results; digest comparisons only within byte-identical checkouts | persistence | E (bit-exact reproductions) + C | ⚠️ "equivalent ordered query results" → "identical"; otherwise sound |
| A16 | LatticeDB committed | reopen preserves everything incl. qualification; −fact loss/identity change | persistence | Q1 [E] + C | ✅ |
| A17 | Filesystem provider | `qualified` + `isConsistentSnapshot=false` in primary type; −unqualified claim; −digest/reopen as consistency evidence | query+persistence | C + E (flag) | ⚠️ with Q2-N09 derivation fix |
| A18 | Pre-cancelled token; simulated movement | throws; no partial results | query | C | ❌ title/assertion mismatch on provider failures; assert failed/cancelled re-index retains last publication and bound queries still answer; add explicit provider-failure semantics |
| A19 | p9 vs new profile | p9 facts preserved byte-identically for unchanged sources; −no identity/evidence/property change | extraction+persistence | E (p9 pattern + provider-version comment) + C | ✅ (provider-version citation fix Q2-N06) |
| A20 | Full suite | all tests pass unmodified; presets/defaults unchanged; additive/opt-in; −preset/default change | query | E (preset source) | ✅ |

**Missing rows (Q2-N04):** declaration-merging attribution ambiguity
(`interface`+`class` same name → no attribution, fail-closed, ideally reason
granularity); duplicate local names (`duplicate-local-name` unknown, no
confirmed result); same-file binding route (positive: local interface
implemented in the same file; negative: multiple same-name declarations); a
coverage row for qualified/member heritage silence (Q2-N05); star-barrel
barrier coverage (A10 fix).

## I. Counterexample evidence (independently executed)

All artifacts are committed under
[ec6d-scope-q2-evidence/](ec6d-scope-q2-evidence/README.md); commands were
run with the **unmodified** committed planning harness built from the review
worktree (`dotnet build … --no-restore`; `ConsumerBaseline.dll`).

1. **Environment reproduction (F-01):** runs `crlf-environment-reproduction`
   and `original-bytes-reconstruction` — bit-exact on both sides (§C.3).
2. **Adversarial corpus (13 files, 99 edges):** run
   `adversarial-corpus-observation`; probes and observed p9 facts are
   tabulated in
   [fixtures/adversarial/README.md](ec6d-scope-q2-evidence/fixtures/adversarial/README.md).
   Key results:
   - `NsShadow.ts` — nested-scope shadowing; heritage + scope facts emitted
     → Q2-N01 false-confirmation path (P1).
   - `Merged.ts` / `ClassInterfaceMerge.ts` — one vs two declaration
     identities for merging; unique vs ambiguous attribution → Q2-N04 rows.
   - `StarBarrel.ts` / `NamedBarrel.ts` — `export-star` / `export-from`
     resolution facts exist → Q2-N03 predicate is implementable precisely.
   - `MemberConsumer.ts` — qualified heritage produces **no fact** →
     Q2-N05.
   - `DupLocal.ts` — duplicate occurrences have distinct identities; binding
     must fail closed → Q2-N02.
   - `SameFile.ts` — same-file route implementable → Q2-N04 missing row.
   - `TypeParam.ts` — type-parameter shadowing extracted → Q2-N01 boundary
     note.
3. **Existing-behavior confirmations:** Q1 harness results re-read and
   cross-checked against its committed code; Q1's committed evidence remains
   unmodified; the four p9 test logs (388/388) were not re-run (no
   additional uncertainty to resolve; Q1 performed that run).
4. **TypeScript semantic oracle (Q2-authored):**
   `runs/typescript-oracle-q2.json` — compiler 5.9.3, node v24.21.0; script
   `tools/q2-typescript-oracle.cjs`, inputs `runs/typescript-oracle-cases.json`
   (ns-shadow / collision / type-param). Results: ns-shadow 0 diagnostics and
   binds the namespace-local interface; collision TS2440; type-param TS2422
   binding the class type parameter. The compiler library was taken from an
   editor installation on this machine (path in the evidence README).
5. **Pre-existing environment artifacts:** the evidence directory contained a
   setup-generated, pre-dating toolkit (harness copy, TypeScript oracle,
   baseline/digest JSONs, Python audit scripts; timestamps in the evidence
   README). It is preserved untouched, **not committed by this review**, and
   was **not used as evidence anywhere in this report**; the checks it
   overlaps (digest reconciliation, identity equality, preservation audit)
   were independently reproduced by Q2's own runs above.

Contract tests vs implementation tests: every A-row above is marked [E]
(verified existing behavior/defect) or [C] (contract criterion); **no
proposed test is reported as passed implementation evidence** — EC-6D
behavior does not exist yet.

## J. Compatibility and integration assessment (Q2-G)

- **Implementation gate:** owner acceptance of scope, symbol-binding facts,
  query contract, representation ADR, and publication policy — correctly
  stated as prerequisites (integration-readiness §"Prerequisites for EC-6D
  implementation") and reinforced by this review's §L.
- **Integration gate:** EC-6C/EC-6B integration into canonical main remains
  separately unauthorized; p9→new migration rehearsal, store/schema
  confirmation, and A19/A20 compatibility are required before merge. The
  65-file / 14,519-insertion / 81-deletion delta manifest was independently
  counted and matches exactly (12 src / 12 tests / 38 docs / 3 root).
- **Release gate:** durability/incremental qualification (EC-6E-class),
  cross-platform qualification (Windows-only evidence preserved),
  representative performance baseline, documentation synchronization — all
  preserved as qualifications. The unreproducible historical migration hash
  (QN-1/FQ1-EV-1) remains recorded as a limitation.
- **No silent authorization found.** R1 implements nothing, merges nothing,
  publishes nothing, activates no schema, performs no migration rehearsal
  and no integration; canonical main (`558f8ac8`) is unchanged locally and
  on the live origin; EC-6C acceptance is nowhere interpreted as merge or
  release authorization.
- **Review-side preservation:** Q2 read-only usage; the only ref advanced is
  the Q2 review branch itself; the original planning worktree, R1 worktree,
  Q1 worktree/branch, and canonical clone remain untouched and clean.

## K. New findings

| ID | Severity | Finding | Required remediation | Blocks |
| --- | --- | --- | --- | --- |
| Q2-N01 | **P1** | **False-confirmation path in the core binding rule.** Import-first precedence (symbol-binding-contract §3.2a) has no scope condition; the extraction emits nested-scope heritage facts (verified: `NsShadow.ts`), so a legal-TypeScript file with a namespace-scoped declaration shadowing an imported alias would be **confirmed** as implementing the imported interface — wrong per TypeScript resolution (**compiler-verified**: TS 5.9.3 oracle, zero diagnostics, binds the namespace-local declaration). Class type-parameter shadowing has the same shape (semantically invalid TS, TS2422; boundary unstated). | Add the file-scope/shadowing guard (exact recommendation in §E.3) to the confirmed category; define the semantically-invalid-sources boundary; add acceptance rows (valid shadowing → unknown; same-file → confirmed; type-param → documented). | Implementation authorization |
| Q2-N02 | P2 | **Multi-occurrence local-name rule absent.** §3.2a does not state what happens when >1 import occurrence introduces `localName = H` (the query's `duplicate-local-name` reason exists, but the binding rule does not require refusal; determinism unspecified). | State: multiple matching occurrences ⇒ binding-ineligible ⇒ `duplicate-local-name` unknown; never confirmed, never nondeterministically selected. Add A-row (DupLocal shape). | Implementation authorization |
| Q2-N03 | P2 | **Barrier / possible-consumer predicates imprecise.** "Outgoing resolves-to edges" is true for any module with relative imports; possible consumers are defined as "import relationships" but derived from all resolves-to edges. Both are precisely implementable from the verified `module-reference` property (documented seven values: `import`, `import-type`, `require`, `export-from`, `export-type-from`, `export-star`, `export-namespace-from`). | Barrier: `module-reference ∈ {export-from, export-type-from, export-star, export-namespace-from}`. Possible consumers: `module-reference ∈ {import, import-type}`. Update both contracts and A08/A10 wording. | Implementation authorization |
| Q2-N04 | P2 | **Acceptance-matrix defects:** (i) A04 expected reason contradicts the rule for its named fixture; (ii) A10 unsatisfiable with the committed corpus (BarrelPreview lacks any heritage candidate); (iii) A12 fixture has no heritage clause and conflates eligible inline type-only with unsupported forms; (iv) A18 title/assertion mismatch on provider failures; (v) missing rows: declaration-merging ambiguity, duplicate local names, same-file route; (vi) A15 "equivalent" → "identical". | Correct/split the four rows; add the missing rows; align wording. | Implementation authorization (acceptance baseline) |
| Q2-N05 | P2 | **Qualified heritage names silently disappear.** `implements NS.Member` emits no heritage fact (empirical probe; `HeritageName` handles only `identifier`/`type_identifier`/`member_expression`, and no `nested_type_identifier` handling exists anywhere in the profile), so it cannot appear as an unknown; the coverage statement does not disclose it; no acceptance row covers it. Q1 explicitly required "reported, not silent". | Extend the coverage statement (mandatory text) to name qualified/member-expression heritage as not evaluated; preferably add an extraction-side skipped-heritage counter for the coverage line; add an acceptance row. | Implementation authorization |
| Q2-N06 | P3 | **Provenance citation off by one revision.** symbol-binding-contract §5 cites heritage facts as provider `…abi14.p9`; at the p9 baseline they carry `…abi14.p8` **by design** (source comment: existing facts keep p8 so p8→p9 re-index is byte-identical; origin uses p9). | Cite the actual provider/version of the fact, or distinguish "profile revision" from "fact provider version". | No (doc fix) |
| Q2-N07 | P3 | **Reason reachability / span definition.** `ambiguous-export` cannot occur under unique-only attribution emission; merging ambiguity surfaces as `unattributed-export`. `exportSpan` is singular while entries can carry multiple occurrence edges. | Either emit an extraction-time attribution status (unique/missing/ambiguous) so both reasons are reportable, or reword; define `exportSpan` for multi-occurrence entries as the occurrence-edge set (or canonical span). | No (doc fix) |
| Q2-N08 | P3 | **Documentation-accuracy items:** (a) `consumer-baseline.md` R1 note says Q1 "re-executed this harness against the committed corpus" — Q1 never had the planning harness/corpus (it used its own harness on equivalent fixtures); (b) recovery §6 table says `FindImplementations` fresh run "0 nodes, 0 edges" — the committed traversal contains 1 seed node, 0 edges; (c) the §6 comparison table omits `SnapshotIntegrityHash`, which also differs (run metadata — expected). | Correct (a) and (b); optionally note (c). | No (doc fix) |
| Q2-N09 | P3 | **Qualification derivation ignores `SnapshotIdentity`; the provider interface documents the identity as "when available"** — `true` + null identity would derive `consistent`. | Derive `consistent` only from (`IsConsistentSnapshot && SnapshotIdentity is non-null`), or validate/fail closed on the combination; adjust A17 wording if needed. | No (minor fix) |

Benign observations (no action): `git fsck --full` on the mirror reports no
missing objects and exactly one dangling blob (`33d1e319…`), a near-final R1
draft of `gap-analysis.md` (1 line from the committed version) — a staging
leftover, not corruption; the "clean connectivity" claim in R1 means "no
missing objects" and is substantially correct.

## L. Owner decision requirements (before implementation)

1. **N01 (blocking):** adopt the scope restriction/shadowing guard for the
   confirmed category (or commission an equivalent guard) and the
   semantically-invalid-sources boundary statement. Owner: milestone owner;
   gate: before implementation authorization; consequence if unresolved:
   false confirmed implementations remain reachable in legal TypeScript.
2. **N02 (blocking):** adopt the duplicate-local-name fail-closed rule.
   Gate: before implementation authorization; consequence: nondeterministic
   or over-confident bindings.
3. **N03 (blocking):** adopt the `module-reference`-based predicates for
   barriers and possible consumers. Gate: before implementation
   authorization; consequence: wrong reason categories / mislabeled
   consumers.
4. **N04/N05 (blocking):** correct the acceptance matrix rows and add the
   missing rows/coverage for merging, duplicate local names, same-file route,
   qualified heritage, star barrels, provider-failure semantics. Gate: the
   acceptance baseline re-issued with the amendments; consequence: tests
   cannot distinguish intended behavior.
5. **Representation ADR:** decide the recommended query-time join (with
   single-file facts; superseded-units and bounded-gathering pins) or an
   equivalent preserving those properties. Gate: before implementation
   authorization.
6. **Publication policy:** accept the qualified-results policy with the N09
   derivation fix and the A17/A18 corrections. Gate: before implementation
   authorization.
7. **N06/N07/N08 (documentation):** apply the citation/reason/accuracy fixes.
   Gate: with the R2 amendment pass.
8. **R16 (retained from the original risk register):** intended-consumer
   confirmation of the bounded workflow before authorization; no telemetry/
   interviews exist yet. Gate: at or before implementation authorization.
9. **Everything in integration-readiness remains in force:** EC-6C
   integration, migration rehearsals, A19/A20 compatibility, platform and
   performance qualifications, release authorization — all separately
   required, none granted.

No decision listed here permits developers to invent correctness-critical
semantics during implementation; each is a concrete, testable amendment.

## M. Recommended next action

**Limited remediation (an EC6D-PLAN-R2 amendment pass), then owner
acceptance.** Specifically: apply Q2-N01..N09 to
`symbol-binding-contract.md`, `consumer-query-contract.md`,
`publication-consistency-contract.md`, `acceptance-contract.md`, and the
documentation-accuracy items; re-issue the packet and obtain a **bounded
verification of the amendments** (QP2, focused on N01–N05) before
implementation authorization. Redesign is not warranted: the recovery is
authenticated, the architecture and scope are sound, and every required
change is text-level and precisely specified above. Do **not** start EC-6D
implementation, integration, or release work in the interim.

## N. Evidence limits

- No EC-6D implementation exists; all binding/query statements are
  contract-level or existing-p9-behavior statements, clearly distinguished
  (A-row basis markers; §I).
- TypeScript semantic checks were executed only for the three committed
  oracle cases (ns-shadow, collision, type-param) with compiler 5.9.3; other
  shapes remain reasoned, not compiler-verified.
- The review worktree contained **pre-existing, setup-generated verification
  artifacts** in the evidence directory (created before this review began;
  timestamps and inventory in the evidence README): a copied harness project,
  a TypeScript oracle script and outputs, baseline/digest reconciliation
  JSONs, and Python audit scripts. They are preserved untouched and are not
  committed as part of this review's evidence; the checks they overlap were
  independently reproduced by Q2's own runs. The TypeScript compiler library
  used for Q2's oracle was located in an editor installation on this machine
  (an external tool, like the .NET SDK used for the harness runs).
- The four existing test suites (388/388) were not re-run by Q2; Q1 ran them
  at the p9 baseline and the harness reproductions extended that evidence.
  No broad regression run was duplicated.
- Runner: Windows x64, .NET SDK 10.0.401; no cross-platform execution.
- The mirror's ref history before R1 cannot be fully reconstructed (bare
  repo, limited reflogs); the current state, origins, reflogs, and
  reproduction evidence are mutually consistent and verified.
- The Q1 harness was not re-executed (its results were read and compared to
  its committed code); Q1 evidence was consumed read-only and remains
  byte-identical.
- Live-remote checks cover `origin` heads only (`ls-remote`), read-only, on
  2026-10-27.

## Validation log (final pass)

| Check | Result |
| --- | --- |
| `git merge-base --is-ancestor` chains (`558f8ac8`→`2c3090cb`→`91a3ed5`→`0e1b213`) | exit 0 ×4 |
| `git diff --name-status 2c3090cb..91a3ed5` | 33 added, all `docs/planning/ec6d/`; no src/tests |
| `git diff --name-status 91a3ed5..0e1b213` | 8 M + 6 A, all `docs/planning/ec6d/`; no src/tests/smoke/.github |
| `git diff --check` (both commits) | exit 0 |
| Harness rebuild + runs (CRLF / reconstructed / adversarial) | exit 0; digests bit-exact as tabulated |
| `git hash-object` corpus vs committed blobs | identical (14/14) |
| `git fsck --full` (mirror) | no missing objects; 1 benign dangling blob |
| `git ls-remote --heads origin` | 9 heads; no EC-6D refs; `main` = `558f8ac8…` |
| Canonical clone / Q1 worktree / planning worktrees / R1 worktree status | clean at recorded commits; untouched |
| Production `src/` and `tests/` in the R1 diff | unchanged (empty) |
| TypeScript oracle (Q2-authored, TS 5.9.3) | ns-shadow: 0 diagnostics, binds namespace-local interface; collision: TS2440; type-param: TS2422 |
| Pre-existing environment evidence toolkit | preserved untouched; excluded from the review commit |
| Local Markdown link validation (R1 packet + this review) | all local links resolve |
| Review worktree status before commit | this review's artifacts staged; pre-existing environment toolkit untracked and preserved |

---

*EC6D-SCOPE-Q2 is a review-only artifact. It creates no production code,
authorizes no implementation, performs no merge/rebase/reset/push/tag, and
does not perform owner acceptance. Evidence: this document plus
[ec6d-scope-q2-evidence/](ec6d-scope-q2-evidence/README.md).*
