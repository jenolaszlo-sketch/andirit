# Compatibility qualification

The new API is additive and opt-in. No legacy traversal preset or default was
modified. `FindImplementationsAsync` retains its existing seed-only TypeScript
interface behavior (F-08); controlled-corpus tests assert this explicitly.
Calls, references, neighborhoods, declaration/node lookups, impact presets and
publication views continue through their existing implementations.

The 391 baseline tests remain present. Owner-approved changes are limited to
p10/API inventories and scoping two p9 inventory assertions to their original
facts. Their original expected counts, provider IDs and both original p8 fact
fingerprints are unchanged. No behavioral expectation, test or coverage gate
was removed or weakened. New tests exercise both positive capability and
negative refusal.

Authentic p9 durable comparison preserved every old node, declaration and edge
as serialized facts in the controlled corpus and pinned website. This compares
identity, properties and recorded evidence, separately from intentionally
changed unit origins, plugin revisions, index identities and snapshot digests.
The controlled corpus retains 64 distinct nodes, 23 declarations and all 99 old
edges. The website retains 161 nodes, 83 declarations and all 313 old edges.

All historical `penghou.hetu.*` provider IDs, grammar pins, ABIs, exports1,
resolver2/relative-v1 semantics and schema-2 labels remain intact. Source facts
are stored using the existing typed property/edge serialization and atomic unit
replacement. There is no storage migration or second binding authority.

Public result types are provider-neutral graph/value types; no parser/native
handle escapes into the API. `InterfaceImpactResultV1.ToJson/FromJson` preserve
qualification, coverage, occurrence data, supporting facts and witness order.
Both-store replay and close/reopen reproduce exact results for one publication.

The new public-query suite resides in the runtime test project so its existing
coverage gate instruments the new runtime code. It adds test-only references
to the existing TreeSitter and LatticeDB projects; no production dependency or
package version was added.
