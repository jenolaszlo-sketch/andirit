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
