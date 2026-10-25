# EC6-A1: Owner acceptance and design reconciliation for EC-6B

**Status:** acceptance record. This document preserves the owner's decisions,
the independent review verdict, and the reconciled EC-6B contract. It
authorizes nothing by itself: implementation remains a separate owner
decision (§10).
**Date:** 2026-10-25.
**Task type:** architecture reconciliation, contract finalization and
owner-decision preservation. Documentation only; no production or test code
was modified.

---

## 1. Git and evidence binding

| Item | Identity |
| --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` |
| Canonical checkout | `C:\Users\Laszlos\source\repos\Penghou.Hetu` |
| Qualified production baseline | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` (tree `08a01a1b7d5d3109b991bcd05d57272fd3f5f018`, canonical `main`) — verified unchanged |
| Original EC-6A design | branch `design/ec6a-cross-file-resolution`, tip `aa4031cd14b6ed8b7089c9471648a8d9d677f782` (tree `fa5efdef1b3694b12f40de0c4d6b81447fcd5857`) |
| Design commits | `99da4d4` (architecture) → `b46334d` (adversarial cases) → `d87ab92` (roadmap) → `aa4031c` (decision register); linear from the baseline |
| Independent GLM review | branch `review/ec6-glm-ar1`, tip `c66ed605b00d1bff7645f3cd52ce47b275aa5817`; evidence `docs/reviews/ec6-glm-ar1-architecture-review.md`, `docs/reviews/ec6-glm-ar1-decision-reconciliation.md`, probe file `ReviewEc6GlmAr1Probes.cs` |
| Acceptance branch | `decisions/ec6a-owner-acceptance`, created from exactly `aa4031c` in an isolated worktree |
| Acceptance commits | 1. architecture contract corrections; 2. adversarial fixtures (AC-41–AC-53); 3. roadmap and decision-register reconciliation; 4. this record |
| Isolation | No pull, rebase, merge, or push. Canonical `main`, the design branch, the review branch, and all existing review evidence are unchanged. The review was read through its existing Git reference; review-only probe code was not merged |

Ancestry was verified before any document was modified: `558f8ac` →
`99da4d4` → `b46334d` → `d87ab92` → `aa4031c` → (`66102e8` → `c66ed60`).

## 2. Independent review verdict

**EC6-GLM-AR1: READY WITH QUALIFICATIONS.** No P0/P1 findings; no contract
conflicts or design defects. The plugin-local resolution architecture is
accepted, subject to four documentation-level corrections, all of which are
reconciled in §6:

- **F02** — normative staging and replacement ordering.
- **F04** — exact path normalization and specifier classification.
- **F05** — cancellation, deterministic behavior, and failure handling.
- **F09** — stable edge identity, occurrence discrimination, and multiplicity.

The review executed six probes (P1–P6: cross-unit staging, whole-unit
replacement including empty replacement, deletion ordering, edge-identity
coexistence and conflict, symbol-identity convergence, staged-invisibility
and atomic completion); all passed. Those probes remain the executed runtime
evidence for the verified store behaviors cited below.

**Source inspection performed during this reconciliation** (facts re-verified
against the baseline tree before documenting them): the edge-identity formula
(`EcmaScriptExtractionProfile.cs:1095-1096`), one-based byte-column locations
(`TreeSitterSyntax.cs:39-50`, `CodeLocation.cs:3-35`), staged-vs-prospective
validation and endpoint rejection (`InMemoryCodeGraphStore.cs:287-299, 358-390,
661-671, 913-929, 985-1011`), atomic completion and supersession
(`InMemoryCodeGraphStore.cs:150-215, 752-765`), the lifecycle order
execution → obsolete deletions → ownership hygiene → completion
(`CodeIndexingLifecycle.cs:681-696, 716-731, 732`), per-file loop ordering and
cancellation points (`TreeSitterExtractionSession.cs:74-161`), coverage
arithmetic (`CodeGraphExtraction.cs:204-209`), evidence rules
(`CodeEvidence.cs`), batch/validator limits (`CodeGraphBatch.cs:95-141`,
`CodeGraphBatchValidator.cs`), path normalization
(`ContractValue.cs:31-44`), reparse-point refusal
(`CodeRepositorySource.cs:287-292, 310-312, 358-368`), and the planner
version/hash invalidation (`CodeIndexPlanner.cs:204-208`).

No new runtime probes were executed for EC6-A1; none were required because
production code is unchanged and every documented behavior is either already
probe-verified (P1–P6, conformance suite) or source-inspected as above.

## 3. Accepted architectural decisions

Recorded as **owner-accepted** with exact scope. Original decision IDs are
preserved; the acceptance record maps task labels A1–A7 onto them.

| Acceptance | Decision ID | Accepted scope |
| --- | --- | --- |
| A1 | D-01 | A resolution pass inside the existing Tree-sitter extraction session, executed after the relevant per-file extraction units and exposed through the internal resolver seam `ITreeSitterModuleResolver`, with an ECMAScript-specific implementation. The resolver emits its relationships into the stable plugin-owned resolution unit `{language}:modules`. No new core graph-resolution phase. A future parser-independent or cross-plugin resolution layer remains an option, not a requirement |
| A2 | D-02 | Module targets reuse the existing file nodes (`{language}:file:{sha256(path)}`). No new module node kinds and no changes to existing file identities. EC-6B supports only the conservative subset of repository-local relative module resolution |
| A3 | D-03/O-01 | `Resolved` evidence classification for `resolves-to`, per §4 below |
| A4 | D-04/D-19 | Graph vocabulary for EC-6B is exactly the `resolves-to` kind plus the property keys `specifier`, `module-reference`, `resolution-policy`, `resolution-rule`, `type-only`. Direction: importing file → resolved target file. No new public node kinds; no `exports` or `binds` relationships during EC-6B; the later vocabulary remains proposed and subject to separate acceptance |
| A5 | D-12/D-13 | The plugin-owned resolution unit is rebuilt when relevant indexed inputs or the resolution policy change; no dependency-level incremental optimization in EC-6B; correctness and deterministic replacement take precedence over the cost of a single-file change |
| A6 | D-15 | Existing impact presets remain unchanged; adding `resolves-to` must not silently expand impact-analysis interpretation; the structural `imports` relationships remain available |
| A7 | D-18 | EC-6B operates within the source inventory of the owning Tree-sitter plugin. Cross-plugin TypeScript/JavaScript resolution is not claimed. Missing targets outside that inventory must not be represented as verified source dependencies; the limitation is documented explicitly (architecture §6.2.3) and measured in EC-6F |
| — | D-10 (boundary) | Literal `require('./m')` yields a `resolves-to` module edge; CommonJS export/binding modeling stays deferred |
| — | D-14 | The §12.2 counting table is normative; resolver kinds join the Tree-sitter coverage list; no new coverage fields |
| — | D-16 | A single emitted-fact revision governs resolver-fact changes; provider version `{packageVersion}+resolver{ResolverRevision}.{policy}` changes in lockstep; no second invalidation axis |
| — | D-20 | Absence of a resolver edge means "not verified under the active policy", never "no dependency exists"; the sentence is required in consumer documentation |

Explicitly **not** accepted (remain proposals at their gates): `exports`/
`binds` kinds and semantics, export tables, D-05 name-based export targets,
D-06/D-11/D-08/D-09, D-17 numbers, query conveniences, coverage persistence,
cross-plugin resolution.

## 4. Accepted evidence semantics (A3/D-03/O-01)

`Resolved` on a `resolves-to` edge carries exactly this normative meaning,
which must appear verbatim in consumer documentation and the identity doc:

> The target was verified under the declared resolution policy and the
> indexed source inventory of this publication. This does not establish
> equivalence with TypeScript compiler, bundler, or runtime resolution.

Boundaries of the claim:

- A matching filename alone never justifies `Resolved`; the ordered policy
  must yield the candidate and the policy's conditions must hold. When they
  do not, no `resolves-to` edge is emitted.
- The emitted relationship carries sufficient provenance to identify the
  resolver, the policy, and the applicable version: edge properties
  (`specifier`, `module-reference`, `resolution-policy`, `resolution-rule`,
  `type-only` where applicable), the `Resolved` quality with the distinct
  provider identity `penghou.hetu.tree-sitter.ecmascript-module-resolver`,
  the provider version `{packageVersion}+resolver{ResolverRevision}.{policy}`,
  the written statement span, and the unit/run origins of the
  `{language}:modules` unit.
- `imports` and every pre-existing fact keep `Structural` evidence and their
  identities; nothing existing changes quality.
- The `exports`/`binds` evidence classification is **not** decided here; it
  is settled at the EC-6C/EC-6D gates under F12's preconditions
  (direct-capture may be `Resolved`; scope resolution requires a documented
  module-scope model; name-match-only binding is forbidden in either
  quality). The rejected `Derived` alternative remains recorded in D-03.

## 5. Normative EC-6B contract

The contract below is stated normatively in the reconciled design documents;
this section indexes it.

**Resolution behavior** — architecture §6.2: relative-v1 qualification
(`./`/`../` prefixes only), POSIX segment normalization, root-escape refusal,
the ordered matching algorithm, and the input classification table giving a
deterministic outcome for every input category (resolved / unresolved /
external / unsupported), including repeated separators, backslashes, query
and fragment suffixes, absolute paths, bare dots, bare packages and aliases,
overlong text, out-of-policy extensions, case-only differences, missing
files, multiple physical candidates, and symlinked entries.

**Identity** — architecture §7.1.1: `resolves-to` edges are identified by
`{language}:{sha256(kind \n source \n target \n discriminator \n span)}` with
discriminator `resolves-to:{module-reference}:{raw-specifier-text}` and the
written statement span (import/export-from statement node; `require` call
expression node). Locations are one-based lines and one-based byte columns,
end-exclusive. Each written module-reference site yields at most one edge;
occurrences are distinguished by span; identical specifiers at distinct
locations, different specifiers to one target, and shared targets across
files are distinct edges. Identity is occurrence-anchored: location-shifting
edits change ids, and identity is stable only under byte-identical
re-extraction. LF/CRLF variants produce identical ids. Duplicate identities
are rejected loudly, never silently merged. Edge properties are not identity
inputs.

**Provenance and evidence** — §4 above and architecture §8: `Resolved`
quality, distinct resolver provider identity, policy-bearing provider
version, written-span locations, and unit/run contributor origins.

**Publication** — architecture §9: one stable `{language}:modules` unit per
plugin, edges only, always re-emitted when the plugin executes (an empty
completing batch clears previous edges); the normative emission order
(per-file units → in-memory shapes → resolution pass → resolution-unit
staging → lifecycle deletions → atomic completion); staged facts are
invisible until atomic completion; ordered staging is not an atomicity
guarantee; failures, cancellations, and interruptions retain the previous
publication; wrong-order staging is rejected loudly as the backstop.

**Coverage** — architecture §12.2: `resolves-to` joins the coverage list with
the accepted counting table (`EdgesEmitted == InternalTargets +
CrossProjectTargets`; self-references count internal; all other resolved
targets count cross-project; unresolved/external/unsupported counted per
§6.2.3; `Partial` when any source parsed with errors or was undecodable).

## 6. F02/F04/F05/F09 reconciliation

| Finding | Disposition | Exact normative locations |
| --- | --- | --- |
| F02 staging/replacement ordering | Reconciled. Emission order stated as an implementation contract (steps 1–5), verified against the existing session/lifecycle/store code; staged-vs-published distinction made explicit ("ordered staging is not an atomicity guarantee"); lifecycle scenarios specified for target deletion, importer deletion, last-relationship removal, previously unresolved modules, extraction-success-with-resolution-failure, and interrupted publication. Required lifecycle fixtures AC-41–AC-45 with expected published-graph results on both stores | `docs/design/ec6a-cross-file-resolution-architecture.md` §9.1.1, §9.1.2, §9.1.3; `docs/design/ec6a-adversarial-cases.md` §11 (AC-41–AC-45); roadmap EC-6B acceptance criteria and matrix |
| F04 normalization/classification | Reconciled. `relative-v1` finalized: normative normalization, the ordered matching algorithm, and the input classification table with deterministic outcomes and rules. Policy constraints preserved: no general TS resolution, no bundler inference, no code execution, no network, no repository-boundary escape, no arbitrary fallbacks, no silent first-match (declared precedence is recorded per edge as `resolution-rule`). One reconciliation choice is flagged below | architecture §6.2.1–§6.2.3 (and §6.3 outcome table, §12.1 overlong bound, §12.2 counters); roadmap EC-6B fixtures 1–14; AC-17–AC-23 remain valid |
| F05 cancellation/determinism/failure | Reconciled. Determinism inputs and stable orderings; token observation between files/modules and before batch emission; cancellation never publishes partial results and has no fallback; failure taxonomy separating ordinary per-specifier non-resolution from fatal resolution-pass failure (which preserves the previous publication); resource limits reused from the existing runtime with no invented thresholds; numeric limits deferred to EC-6E with behavior never undefined | architecture §9.4.1–§9.4.4 (with §9.3); AC-45, AC-51, AC-52; roadmap EC-6B acceptance criteria (F05) |
| F09 identity/multiplicity | Reconciled for `resolves-to`: exact identity inputs, discriminator and source-span specification with coordinate conventions and LF/CRLF behavior, occurrence semantics, stale-occurrence removal via replacement, property/identity consistency, and collision rejection. The full per-kind contract for `exports`/`binds` remains gated at EC-6C/D. Required identity fixtures AC-46–AC-53 | architecture §7.1.1; `docs/design/ec6a-adversarial-cases.md` §12 (AC-46–AC-53); roadmap EC-6B acceptance criteria (F09) |

**Flagged reconciliation choice (recorded, reversible).** The original §6.2
draft listed only `.ts`/`.tsx`/`.mts`/`.cts` while the reviewed adversarial
case AC-40 requires `import './types'` to reach `types.d.ts` — an internal
contradiction that F04's mandate (finalize the policy, leave no
contradictory classification rules) required resolving. The reconciled
ordered list includes `.d.ts` for TypeScript (`.d.mts`/`.d.cts` remain
unreachable by substitution). The alternative — keeping the original list
and demoting AC-40 to an unresolved case — is equally deterministic; if the
owner prefers it, the change is a one-row policy edit before EC-6B fixtures
are written. No other new material design choice was introduced: every other
addition (overlong handling, extension classification, `require` span
convention, coverage unit semantics, failure-class mapping) implements the
accepted decisions and the F02/F04/F05/F09 mandates using verified existing
runtime behavior.

## 7. Deferred decisions and their milestones

Preserved from the independent review; none is resolved by this task. The
full table with reasons, blocking conditions, and acceptance evidence lives
in the [decision register](../design/ec6a-decision-register.md) ("Deferred
decisions with future gates"). Summary:

| Item | Gate | Blocking condition |
| --- | --- | --- |
| F01 attribution of converged name-based identities; export binding attribution and eligibility (D-05/O-02) | EC-6C design (exports), EC-6D (binds) | Any `exports`/`binds` edge targeting a converged kind |
| Full F09 discriminator contract for export/import bindings | EC-6C/D | Any `exports`/`binds` edge |
| F07 star/re-export type-only propagation; O-04 star-edge representation | EC-6D design | Star/namespace re-export emission |
| Re-export cycle closure completeness; order-independence fixtures | EC-6D design | Star/re-export closure ships |
| D-06 export of imported bindings; D-11 anonymous defaults; D-10 CommonJS modeling | post-6F | Milestone authorized |
| D-08 `.js`→`.ts` substitution (`relative-v2`) | post-6F | Policy extension authorized |
| D-09 `tsconfig` paths/baseUrl/modes | post-6F | Explicit opt-in design |
| D-17/O-07 degradation numbers and preference order; measured performance limits | EC-6E / EC-6F | Large-repository qualification |
| F10 all-files-deleted lifecycle fixture | EC-6E | EC-6E |
| F06 persisted coverage diagnostics | post-6F platform work | Consumer need |
| Cross-plugin resolution (D-18 evolution) | post-6F, core work | Owner authorization |
| Query conveniences, preset/impact changes, optimized dependency tracking | post-6F | Explicit decision |
| Identity migration for name-based symbols | post-6F | F01 option 3 chosen |

Standing rule preserved from the review: **name equality alone is never
sufficient evidence** for future declaration binding; where an existing
symbol identity is insufficiently specific, binding must be restricted or
separately justified. Basic lifecycle behavior (deletion, replacement,
failure, cancellation) is required for EC-6B and is **not** deferred to the
EC-6E qualification.

## 8. Compatibility and migration implications

- **No identity changes.** Every existing symbol, declaration, file, node,
  and edge identity is preserved; no graph migration is required
  (architecture §13).
- **No node kinds added.** EC-6B adds one edge kind and five property keys —
  open, validated vocabularies; stores and snapshots need no schema change.
- **Old publications remain valid.** Pre-EC-6 publications simply lack
  resolver facts; the plugin version bump in the emitting milestone
  re-extracts all sources, so no mixed-version units can exist in one
  publication.
- **Existing evidence guarantees untouched.** `imports` stays `Structural`
  and its placeholders unchanged; the `calls`-requires-`Resolved` validator
  rule is unaffected; presets stay closed sets, so impact semantics are
  unchanged (A6).
- **Test expectations, not behavior, must be rescoped** in the emitting
  milestone (the roadmap lists the exact blanket assertions); this is a
  test-update obligation, not a regression.
- **Consumer documentation duty** ships with the facts: the mandated
  `Resolved` sentence (§4) and the "absence is not evidence of absence" rule
  (D-20).

## 9. Residual risks

| Risk | Standing mitigation |
| --- | --- |
| Per-plugin scope depresses real-world recall (mixed TS/JS repositories) | Documented boundary (A7); measured in EC-6F; cross-plugin is a designed next step |
| `relative-v1` diverges from TypeScript's resolver in documented cases (`.js`→`.ts`, tsconfig, dynamic forms) | Every deviation enumerated with fixtures; each future extension gets its own policy level |
| Name-based symbol convergence (F01) limits future binding attribution | Deferred with a mandatory correction at EC-6C/D; no binding ships before it is resolved |
| Cost of full resolution-unit rebuilds on very large repositories | Correctness-first by accepted decision (A5); bounds reused from the runtime; D-17 numbers measured and owner-gated at EC-6E |
| Consumer over-interpretation of missing edges | D-20 documentation duty in every shipping milestone |
| Resolution-unit vocabulary growth | D-19 discipline; accepted key set is closed for EC-6B |

## 10. EC-6B authorization boundary

- **This record authorizes:** treating the reconciled contract as the
  implementable specification for EC-6B — conservative relative module
  resolution with `resolves-to`, `relative-v1`, stable identity, provenance,
  deterministic rebuild, and the lifecycle/failure/cancellation behavior of
  architecture §6.2, §7.1.1, §9, §11.1, §12.
- **This record does not authorize:** starting EC-6B implementation.
  Implementation authorization remains a separate owner decision, per the
  review's closing statement and the task's completion criteria.
- **EC-6B's approved scope** (included: internal ECMAScript resolver, file
  node identities, `relative-v1`, `resolves-to`, policy-scoped `Resolved`
  evidence, stable identity and provenance, deterministic rebuild,
  replacement/deletion behavior, failure/cancellation/publication
  preservation, focused tests against both stores; excluded: export binding,
  import-to-declaration binding, new symbol identities, re-export traversal,
  type inference, runtime dependency analysis, general Node/TS compiler
  resolution, arbitrary package resolution, cross-plugin resolution,
  impact-preset changes, core graph redesign) is normative in the
  [roadmap](../design/ec6a-implementation-roadmap.md), whose acceptance
  matrix is the EC-6B review checklist.
- **Next milestone:** EC-6B, "Conservative Relative Module Resolution",
  gated by the recorded EC6-A1 acceptance (satisfied) and by a separate
  implementation authorization.
