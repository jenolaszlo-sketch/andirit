# Lexical binding and acceptance trace

Binding follows the accepted ordered gates: missing context, recovery,
ambient/global, nested scope, unsupported heritage kind with associated written
name, qualified form, expression form, owner type-parameter shadowing,
ineligible imports, duplicate eligible imports, import/local conflicts, unique
eligible import route, then same-file physical cardinality/eligibility.
No name equality, import-first choice or first match proves a binding.

Import proofs contain actual legacy heritage, C, A, resolver edge/policy,
export entry, every export occurrence, B and the physical declaration. Same-file
proofs contain only heritage, C and the physical declaration/node. SHA256 over
the ordered stored identity tuple plus policy yields `binding1.local-v1`.
Repeated clauses and repeated exports preserve all complete witness paths.
Chains completing to another physical declaration are omitted for this target.

All 14 Q2 and 23 R2 cases execute against both stores with independently
recorded, manually selected expectations in `InterfaceImpactTests`.
The original cases.json files are read as sources, not rewritten or used as
implementation-generated expected output. TypeScript 5.9.3 was rerun over
14/14 Q2 and 23/23 R2 cases as supporting lexical evidence, without claiming
compiler equivalence. The original recorded oracle observations remain intact.

## Acceptance matrix

| Rows | Reproducible evidence |
| --- | --- |
| A01, A02, A06, A07 | Controlled exact four results/five modules/full stored witnesses; Q2 multi-specifier/export-alias; discovery origins retained |
| A03, A24 | Q2 merged/type-value/nested-export; null ambiguous target record/span; all physical contributor IDs; no wrong-target confirmation |
| A04, A04b | Q2 wrong-export; R2 absent-export clause and bare occurrence |
| A05, A21 | Q2 collision/nested-shadow; type-parameter fixture; synthetic missing-C publication |
| A08, A09 | Controlled possible-module predicate and separate occurrence evidence; idle named imports; unsupported forms excluded from possible consumers |
| A10, A10b, A27 | All named/star/missing/multi/no-declaration barrels; ordinary import is not a barrier; related/unrelated scope; each E and its resolver outcome exposed |
| A11 | Primary MissingImport; target-associated MissingAlias supplement and original Ghost unresolved coverage, per explicit owner clarification |
| A12 | Inline type positive; import-equals/default/namespace/dynamic refusals; recognized unsupported resolver counterexample and coverage-only occurrence path |
| A13 | All stored witness IDs retrievable; p8 heritage version; source hashes/units; same-file short proof; multiple complete witnesses and export occurrences |
| A14 | Repeat JSON equality, reversed reader enumeration, all five work budgets, all three output caps, total ordering and partial/lower-bound flags |
| A15 | Merge, rename, target deletion, import removal, unchanged indexing and stale-unit cleanup; migration repeat/reopen |
| A16 | Both-store corpus tests, exact snapshot replay and real durable close/reopen; controlled and website migration harness |
| A17 | False/null, false/identified, true/null, true/blank, true/identified; JSON roundtrip; checked authority guard and coverage labels |
| A18 | Query cancellation, same-content/new-run movement, injected store failure, missing resolver and contradictory evidence return no classified result |
| A18b | Both-store failed extraction retention; durable authentic-migration cancelled/failed follow-ups preserve last publication; subsequent deletion/reopen |
| A19 | Authentic p9 preservation on both corpora; unchanged p8 fingerprints; historical providers/grammar/ABI |
| A20 | Original 391 tests retained with explicitly approved snapshot/inventory adjustments; unchanged traversal presets and legacy seed-only result |
| A22, A23 | Same-file short proof, same-file merged physical ambiguity, different-declaration omission, duplicate imports, nested-unrelated contrast |
| A25 | Qualified-member, expression and interface-extends facts remain observed and refused with their exact gate reason |
| A26, A28 | Ghost, approved ambient slot, ambient script, recovered clause, external package; reserved reasons never emitted |

Synthetic tests additionally reject wrong attribution/owner identities,
stale source hashes, bad provider versions, missing export evidence and missing
declaration contexts. Foreign-language heritage cannot contaminate the query.
This is producer evidence; independent review remains a separate gate.
