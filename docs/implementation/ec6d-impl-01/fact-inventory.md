# Stored fact inventory

All additions use existing graph nodes/edges, typed property values and unit
replacement. No storage schema or historical schema-2 label was changed.
No authoritative cross-file binding or persisted query result was introduced.

The additive edge's `ec6d.data` text property contains its deterministic JSON
record. Ordinary evidence and owning `CodeFactOrigin` retain source span,
provider/version, source hash, plugin/version, index unit and run provenance.
The public result exposes these properties and origins; parser types stay internal.

| Fact | Edge kind / provider suffix | Evidence version |
| --- | --- | --- |
| A | `imports-name` / `ecmascript-import-occurrences` | `imports1.local-v1` |
| B | `export-attribution` / `ecmascript-export-attribution` | `exports2.local-v1` |
| C | `heritage-context` / `ecmascript-heritage-context` | `heritage1.local-v1` |
| E | `export-reference` / `ecmascript-module-occurrences` | `module1.local-v1` |
| Physical eligibility support | `declaration-context` / `ecmascript-heritage-context` | `heritage1.local-v1` |
| Capability marker | `interface-profile` / `ecmascript-profile` | `imports1.local-v1` |

All provider IDs retain prefix `penghou.hetu.tree-sitter.`. New syntax facts
have Structural evidence. Original heritage evidence stays at its recorded
`0.2.0-preview.6+v0.23.2.abi14.p8`; original exports1 and resolver2 facts are
unchanged. TypeScript and JavaScript share profile p10; their grammars and ABIs
remain v0.23.2/ABI14 and v0.25.0/ABI15 respectively.

A/C/E/B are owned by their source file's existing unit. A spans every named
specifier separately and records default, namespace, import-equals, dynamic,
side-effect and unsupported forms. Import-alias syntax is retained even when
the p9 resolver did not recognize it. Strings retain raw escapes without quotes;
module token spans include quotes. Coordinates are UTF-16, one-based,
end-exclusive. Mixed inline type modifiers remain per-specifier.

C records every modeled owner/clause, including qualified, expression and
extends forms p9 skipped. Separate physical declaration contexts retain module
scope, ambient/recovered status and eligibility without modifying old declaration
records. Attribution counts physical records before node deduplication, retains
all eligible and ineligible IDs and every export occurrence, and fails closed
on conflicting contributors. The approved ambient additions are separately
described in [owner clarifications](owner-clarifications.md).

E stores source references and clause pairs, including namespace-export names;
it never establishes a symbol origin. `export =` remains unsupported.

## Resolver association and ownership

`resolver-status` edges in the new `typescript:interface-resolutions` unit
retain all four outcomes with the existing resolver provider and
`0.2.0-preview.6+resolver2.relative-v1`. They quote outcomes as Structural
evidence; an actual existing Resolved `resolves-to` edge is also required for
a resolved outcome. Their key hashes source file, exact statement span, raw
specifier and reference form; the publication supplies the final join boundary.
The inventory pass replaces this unit atomically even when empty. Target-file
add/delete/rename therefore recomputes every dependent outcome.

Each source occurrence is stable across unchanged indexing. IDs use lowercase
SHA256 over deterministic JSON content and source/endpoint/kind inputs.
The query checks identity/content, provider/version, physical contributors,
hash/unit ownership and endpoints before confirming. Re-export barrier evidence
includes its exact same-publication resolver-status fact as supporting evidence.

Controlled p10 additions: 14 A, 13 B, 6 C, 3 E, 23 declaration contexts,
13 capability markers and 17 resolver outcomes: **89 additive edges**.
Existing 64 nodes / 23 declarations / 99 edges remain unchanged; p10 has
188 edges and 15 units. Website additions: **138 edges**, with the original
161 nodes / 83 declarations / 313 edges preserved.
