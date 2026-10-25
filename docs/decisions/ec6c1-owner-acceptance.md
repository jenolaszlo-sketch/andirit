# EC-6C.1 Owner Acceptance and EC-6C.2 Authorization

**Project:** Penghou.Hetu (future Andirit)  
**Milestone:** EC-6C.1 owner acceptance  
**Date:** 2026-10-25  
**Disposition:** **EC-6C.1 OWNER ACCEPTED WITH QUALIFICATIONS — EC-6C.2 IMPLEMENTATION AUTHORIZED**

## 1. Decision

The owner accepts the EC-6C.1 Structural Export Shape Inventory as the
implementation foundation for EC-6C.2. This acceptance relies on the local
implementation report and preserved evidence; it is not an independent
implementation review or qualification verdict.

Independent verification of EC-6C.1 remains mandatory before integration or
release. No reviewer verdict is asserted or implied by this record. The
acceptance authorizes only the bounded EC-6C.2 structural export graph
publication stage described by the accepted A1/A2 contract. It does not
authorize Track B declaration attribution, EC-6D binding, integration, or
release.

## 2. Exact source and evidence binding

- EC-6C.1 branch: `implementation/ec6c-1-export-shape-inventory`.
- Implementation commit: `b1998d64bb7d2811be5ac1a14bda24df4b1cbef6`.
- Final EC-6C.1 handoff commit: `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340`.
- Accepted EC-6C.1 tree: `8a3cc154486bf9796b532ee4db96714fa63763b2`.
- The final handoff commit is a child of the implementation commit; the
  implementation commit is based directly on EC6C-A2
  `bf4ffcc25c22a6b8f98fb2b3f513dd95e5f8f49f` (tree
  `8abb3f12b4344a17b8a530c42c10c01130daabed`).
- Implementation evidence: `docs/evidence/ec6c-1-export-shape-inventory-handoff.md`
  at the accepted handoff commit above.
- The EC-6C.1 source remains on its own branch and is not modified by this
  decision or the EC-6C.2 worktree.

## 3. Accepted EC-6C.1 contract

EC-6C.1 adds immutable, internal observations from the shared TypeScript and
JavaScript Tree-sitter parse/traversal. An observation preserves the module
path and language, exact public name, written local name when present, syntax
form, named/default kind, explicit grammar-derived type-only state,
syntax-grounded type/value/both/unknown projection, exact public-name token
span, and enclosing export construct span. Observations are deterministically
ordered and retain duplicate written occurrences. No declaration lookup,
identity, or binding is claimed.

The local report states support for direct allowlisted declarations, simple
local export lists and aliases, imported-local export syntax, defaults,
explicit type-only forms, and the narrowly recognized pinned-grammar
contextual `type` recovery shapes. Source-bearing re-exports, CommonJS,
`export =`, unsupported ambient declarations, destructuring, nested namespace
exports, escaped public names, malformed or unrecognized forms, and invalid or
over-bound names remain excluded.

The accepted source-location convention is one-based, end-exclusive UTF-16
code-unit columns. Names are validated as 1–256 Unicode scalar values using
strict UTF-16 decoding; valid text is preserved ordinally without normalization,
trimming, replacement, or truncation.

## 4. A29 qualification verified against the accepted contract

The implementation's rejection of empty, all-whitespace, and control-containing
public names was checked against the normative EC6C-A2 language. The current
architecture §3, A29 closure §3, and A01–A29 matrix explicitly classify
all-whitespace and control-containing names as unsupported, in addition to
empty, malformed-surrogate, and over-bound names. These restrictions are
therefore part of the accepted Track A name allowlist, not an implementation
convenience that needs removal. The EC-6C.1 validator matches this boundary;
no corrective change is required for this item.

## 5. Locally reported preservation and tests

The EC-6C.1 handoff reports the following local results; this owner acceptance
does not independently reproduce or qualify them:

- Focused export inventory: 14 passed, 0 failed, 0 skipped.
- Full Tree-sitter project: 145 passed, 0 failed, 0 skipped.
- Dedicated module-resolution suite: 29 passed, 0 failed, 0 skipped.
- Full solution: 377 passed, 0 failed, 0 skipped.
- Formatter verification and `git diff --check` passed.
- The TypeScript and JavaScript published-graph fixtures retained their
  recorded p8 fingerprints, counts, existing `resolver2.relative-v1` facts,
  and no export nodes or edges.

The source handoff also records failed test-authoring and cleanup attempts and
their corrections. They are preserved as local implementation evidence, not
review findings.

## 6. Known limitations and outstanding qualification

The pinned TypeScript grammar reports recovery for valid contextual forms
including `export { type }` and `export { type as kind }`. EC-6C.1 handles only
the exact inspected tree shapes; broader recovered syntax remains omitted and
parse-error state remains visible. Escaped public names remain unsupported.
The observation list is positive-only and does not establish export
completeness, declaration attribution, imported binding, or effective
re-export surfaces.

A separate independent qualification must reproduce EC-6C.1 syntax
recognition, contextual `type` and recovery behavior, Unicode boundaries,
source spans, deterministic ordering, duplicate occurrences, and unchanged
p8 graph facts before integration or release. No independent EC-6C.1 review
has yet been completed.

## 7. EC-6C.2 authorization boundary

EC-6C.2 implementation is authorized to publish the accepted module/name
`export` entry nodes and per-occurrence Structural `exports` edges in each
existing file unit, with deterministic accepted identities, explicit
pre-insertion collision detection, accepted provenance and namespace
properties, p8-to-p9 profile invalidation, and in-memory/LatticeDb qualification.
It must preserve positive-only semantics, existing queries and graph
identities, and `resolver2.relative-v1`.

This authorization explicitly excludes `exported-as`, declaration attribution,
module-scope lookup, import-to-export binding, remote re-export binding,
re-export publication, completeness claims, graph-core identity redesign, new
query APIs absent a demonstrated contract conflict, Track B, EC-6D, integration,
and release. No export observation may be linked to a declaration under this
acceptance.

## 8. Authority

The EC6C-A1/A2 architecture and decisions remain normative. This record accepts
the locally reported EC-6C.1 implementation with the qualification above and
authorizes EC-6C.2 development only. It does not amend accepted historical
review evidence.