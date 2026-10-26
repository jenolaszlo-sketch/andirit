# Proposed EC-6D acceptance contract

**Status: proposal for owner review.** This document authorizes no
implementation, integration or release.

The proposed outcome is a bounded TypeScript query for one uniquely selected
interface declaration, its uniquely bound direct explicit implementers, and
its direct named-importing modules as possible consumers. It does not claim
whole-program impact. The exact graph representation must be approved by an
ADR before implementation.

## Functional requirements

### Supported profile

1. Run against one TypeScript Tree-sitter plugin inventory and one successful
   publication.
2. Resolve an interface target only when its declaration identity is unique.
3. Support a direct named declaration export from the target module and a
   direct named relative import under the accepted relative-v1 module policy.
4. Support a named imported alias and explicit type-only import form when the
   syntax is unambiguous.
5. Associate a class's explicit implements name with the unique imported or
   same-file interface binding.
6. Return direct importing modules as possible consumers. A module import is
   not proof of a use site.
7. Return the target declaration, direct explicit implementers, possible
   importers, evidence paths, and coverage/ambiguity/truncation status from an
   additive bounded consumer query.

Re-exports, star/namespace exports, bare packages, config aliases,
cross-plugin resolution, CommonJS, dynamic modules, implicit satisfaction,
general references/calls and ordinary type-use locations are excluded.

### Result shape

The result must contain:

- selected declaration ID, kind, path and physical declaration span;
- zero or more direct implementers, each with class ID, source location,
  binding route and complete provenance;
- zero or more direct possible-consumer modules, each with import occurrence,
  imported name, local alias (if present), source span and target path;
- explicit unresolved, ambiguous, unsupported and conflicting cases;
- bound-view publication identity, plugin/profile versions, applied query,
  depth/node/edge limits, truncation state and omitted frontier count;
- a coverage statement that names the supported profile and does not imply
  completeness outside it.

No consumer must infer which node is the interface from a same-spelling name.

## Correctness requirements

- A positive direct implementation must resolve to the selected declaration
  identity, not a name-only placeholder.
- Same-name declarations in different modules remain distinct. The fixture's
  OtherRenderer must not bind to contracts/Renderable.ts.
- The imported alias case must bind to the original imported interface only
  when the import path and imported/exported name are uniquely supported.
- A missing import target, absent exported name, duplicate/ambiguous target,
  unsupported re-export or conflicting fact must not produce a positive
  binding.
- AliasRenderer is included only when the profile supports its direct
  aliased import. BarrelPreview remains unsupported until re-export origin
  tracing is separately accepted.
- Direct importers are possible consumers, not confirmed type uses.
- CanvasRenderer is not a direct implementer of Renderable. Do not report
  inherited/transitive impact unless a separate qualified rule is included.
- Do not infer absence of impact from missing graph facts. Include unknown
  coverage and aggregate unresolved status.
- Report relationship-class precision and recall against a separately
  manually reviewed reference set. Never extrapolate fixture values globally.

## Provenance and evidence

Each claimed relation must be traceable to its source occurrence, source
location, provider and profile version, extraction/binding policy, evidence
quality and all graph path steps. Module-file resolution and symbol binding
must be shown as separate steps. The result must state that its bounded
binding rule is not a full TypeScript compiler, bundler or runtime proof.

Before implementation, an ADR must decide the additive graph representation,
evidence classification and provider-version contract. If the current evidence
vocabulary cannot express a unique static binding without implying a stronger
semantic guarantee, extend or qualify the contract before emitting facts.

## Determinism

For identical source bytes, plugin versions, repository inventory and
equivalent query bounds:

- repeated indexing produces the same canonical node, declaration and edge
  facts and IDs;
- equivalent input enumeration order does not change canonical results;
- repeated queries over the same publication return equivalent ordered
  result sets, provenance and truncation state;
- evidence spans retain documented UTF-16, one-based, end-exclusive
  coordinates.

Run metadata and snapshot integrity hashes may differ when they include
publication-specific values; compare canonical graph facts and query results.

## Persistence and lifecycle

Run the contract against InMemory and LatticeDB. For LatticeDB, commit the
publication, close the store/process, reopen, then verify the selected
declaration, each positive and negative binding, provenance and query output.

Repeated indexing of unchanged sources must not duplicate facts. Editing or
deleting an import, export, declaration or implementing class must remove or
replace all owned stale binding facts atomically. Empty replacement, failed
run and cancellation must retain the last successful publication. Conflicting
IDs with different facts fail closed.

## Compatibility

- Preserve EC-6C p9 export entries, occurrence IDs, evidence and properties
  for unchanged source.
- Preserve EC-6B relative-v1 resolver results and meanings.
- Keep Structural imports and existing query defaults/preset membership
  unchanged.
- Expose new binding behavior additively and opt-in; do not reinterpret
  existing structural placeholders as bound symbols.
- Qualify the p9-to-new-profile migration, including durable close/reopen and
  comparison of all pre-existing facts.
- Verify snapshot import/export and public query compatibility where affected.

## Failure behavior

- Invalid repository IDs, declaration IDs, bounds and malformed query inputs
  follow existing argument-validation conventions.
- Unsupported or ambiguous syntax yields an explicit non-positive status;
  it never becomes a guessed binding.
- Cancellation and extraction failure publish no partial binding facts and
  preserve the last successful graph.
- A duplicate identity with different facts is a deterministic failure, not
  first-wins behavior.
- Stale publication, source conflict and traversal truncation remain visible
  to the caller. Do not silently return partial results as complete.

## Independent consumer qualification

A reviewer unfamiliar with implementation internals must be able to:

1. check out the exact accepted source and corpus revisions;
2. follow documented restore/build/run steps;
3. reproduce the selected declaration, direct implementers, possible
   importers, negative cases, provenance, canonical digests and test results;
4. verify the outputs against a human-reviewed reference set without using
   implementation-generated counts as ground truth.

Qualification must include direct import, alias, same-name negative, missing
module, missing export, duplicate declaration, unsupported re-export,
inheritance, repeated index, source change/delete, cancellation and durable
reopen. It must record any platform or store not evaluated.

## Performance and operations

No numeric target is justified by the thirteen-source baseline. Before
setting thresholds, measure at least indexing wall time, peak memory, query
latency by result size, durable reopen time and repeated-index cost on a
representative repository. Record hardware, runtime, warm/cold state, source
count, query bounds and repetition method. Do not make a scale claim from the
small fixture.

## Out of scope

This contract does not promise complete compile/runtime impact, all type uses,
calls/references, implicit implementations, duck typing, dynamic dispatch,
transitive inheritance, re-export closure, package/config resolution,
cross-plugin symbols, graph diff/history, JavaScript/CommonJS symbol binding,
new languages, ranking or AI-written explanations.

---

## R1 revision (EC6D-PLAN-R1): executable acceptance requirements matrix

The following matrix operationalizes this contract against the remediated
design ([symbol-binding-contract.md](symbol-binding-contract.md),
[consumer-query-contract.md](consumer-query-contract.md),
[publication-consistency-contract.md](publication-consistency-contract.md)).
Each row separates the **assertion** (what must hold regardless of
technique) from the **technique** (how it is tested); a technique may change
during implementation only if the assertion still distinguishes. Fixtures
reference the committed corpus
(`corpus/ts-interface-impact/`, tree `014da71f0855cdc957fa9b6a0667bca565145092`)
unless a row defines an additional non-production fixture inside the
planning evidence area. Extraction, binding, query, and persistence layers
are tested at their own boundaries: a passing graph-storage test never
substitutes for a semantic assertion.

| ID | Required behavior | Preconditions | Fixture / input | Expected observation (assertion) | Negative assertions | Layer |
| --- | --- | --- | --- | --- | --- | --- |
| A01 | Direct named import resolves to the correct declaration | Supported publication; facts A+B emitted | `SvgRenderer.ts` / `HtmlRenderer.ts` (`import type { Renderable } from "../contracts/Renderable"`) | Query target = contracts/Renderable.ts `Renderable` node returns those classes as confirmed, binding chain names that import occurrence | The other same-named interface (other/Renderable.ts) never appears as the resolved target | binding + query |
| A02 | Imported alias binds correctly | Alias occurrence recorded with imported + local names | `AliasRenderer.ts` (`import type { Renderable as Drawable }`; `implements Drawable`) | AliasRenderer confirmed via chain localName=Drawable → importedName=Renderable → entry (target, Renderable) → declaration | The literal spelling `Drawable` never matches an export entry named Drawable; the alias is visible in provenance | binding + query |
| A03 | Duplicate interface names remain distinct | Both files indexed | contracts/Renderable.ts vs other/Renderable.ts (both export `Renderable`) | Two distinct declaration identities; query on one never returns implementers of the other; export entries distinct per file | No name-only convergence of declarations or entries (Q1 G1) | extraction + binding |
| A04 | Wrong export never produces a confirmed binding | Occurrence facts emitted | Q1 scenario G3 fixture (imports `Gadget`, implements `Widget`) | No confirmed implementation of `Widget` via that file; unknown `missing-export` (or `unsupported-import-form` per form) recorded where applicable | No confirmed relationship may cite an export entry whose name differs from the imported name | binding |
| A05 | Local name collision does not cause false identity | Import-first precedence in force | Q1 scenario G2 fixture (`import { Widget as Local }` + local `interface Local` + `implements Local`) | Binds to the imported Widget declaration, never the local `Local` interface | The local same-name interface is never the binding target while a supported import occurrence exists | binding |
| A06 | Explicit `implements` binds to the intended interface | Facts A+B present | `SvgRenderer.ts` | Confirmed entry's chain terminates at the exact target declaration node id | Heritage placeholder node is never reported as the implementation target | binding |
| A07 | Four canonical direct implementations are identified | Full corpus, supported profile | Selected declaration contracts/Renderable.ts `Renderable` | Exactly SvgRenderer, HtmlRenderer, AbstractRenderer, AliasRenderer as confirmed | OtherRenderer absent (bound elsewhere); CanvasRenderer absent (transitive); no additional confirmed entries; fixture-scoped, not an accuracy claim | query |
| A08 | Direct importing module is labeled possible consumer | `resolves-to` facts present | `Preview.ts` | Preview listed once as possible consumer with its import occurrences | Preview is not in the confirmed category | query |
| A09 | Idle import is not reported as confirmed semantic use | Import without use | Q1 scenario G4 fixture (`import { Widget } from "./types";` only) | File is possible consumer; confirmed list unchanged | Possible-consumer entry never claims type use | query |
| A10 | Unsupported barrel path produces explicit unknown | Barrel has no export entries, has outgoing resolves-to | `BarrelPreview.ts` via `src/api/index.ts` | Unknown with reason `re-export-barrier`, evidence spans recorded | BarrelPreview neither confirmed nor silently omitted; coverage statement present | query |
| A11 | Missing module does not produce false binding | Unresolved specifier | `MissingImport.ts` (`../contracts/missing`) | Unknown with reason `unresolved-module`; no confirmed or possible-consumer entry via that import | No binding may cite an unresolved or external module | binding + query |
| A12 | Unsupported import form does not silently misbind | Unsupported forms recorded | Q1 scenario G5 fixture (default, namespace, inline type-only) | Each unsupported occurrence recorded with its form; any heritage clause naming such a binding is unknown `unsupported-import-form` | Unsupported forms never appear in confirmed chains | extraction + query |
| A13 | Every confirmed relationship has provenance | — | A07 result | Each confirmed entry exposes all five steps with edge ids, spans, providers, versions, qualities; each step verifiable as a stored fact | No step may cite a non-existent fact; no confirmed entry without a complete chain | query |
| A14 | Canonical query ordering is deterministic | Same publication | A07 repeated | Identical ordered results across repeated calls; total order as specified | Any nondeterministic ordering or set instability | query |
| A15 | Repeated indexing preserves equivalent canonical results | Fresh stores, unchanged sources | Full corpus, two independent index runs | Identical node/edge identity sets and equivalent ordered query results; content-hash-dependent digests compared only within byte-identical checkouts (recovery doc §6) | Facts duplicated or identities drifting between runs | persistence |
| A16 | Durable reopen preserves supported facts and identity | LatticeDB publication committed | A07 publication | After close/reopen: same declaration, confirmed entries, unknowns, provenance, qualification | Any fact loss or identity change on reopen | persistence |
| A17 | Inconsistent publication cannot imply unqualified confirmation | Filesystem provider publication | Full corpus | `qualification = "qualified"` present in the primary result type with `isConsistentSnapshot=false`; confirmed entries assert "relative to publication P" only | Any unqualified confirmed result from an inconsistent publication; digest/reopen claims used as consistency evidence | query + persistence |
| A18 | Cancellation and provider failures have defined behavior | Bound publication view | A07 with pre-cancelled token; simulated publication movement | Cancellation throws with no partial classified results; publication movement raises the existing fail-on-change failure | No partial results on failure; no silent fallback to stale publication | query |
| A19 | Existing EC-6C p9 facts remain compatible | Unchanged sources | Full corpus, p9 vs new profile | p9 export entries, occurrence edges, resolver edges, structural heritage facts preserved byte-identically for unchanged sources (pattern of the accepted p8→p9 migration) | Any change to pre-existing fact identity, evidence, or properties | extraction + persistence |
| A20 | Existing query defaults and behavior are preserved | Full existing suite | Repository test suite | All existing tests pass unmodified; `FindImplementationsAsync` and presets unchanged; new query is additive and opt-in | Any preset membership change or default change | query |

### Coverage and measurement rules

- Precision/recall against the manually reviewed corpus expected set is
  reported **per relationship class** (direct implementers; direct possible
  importers) and **never extrapolated** beyond the fixture.
- No numerical performance target appears in this contract; G7 remains
  NOT EVALUATED until a representative repository baseline exists.
- Independent verification requires the reviewer to reproduce A07's
  classification from stored facts alone, without trusting
  implementation-generated counts as ground truth (the original contract's
  independent-consumer-qualification section, unchanged).
