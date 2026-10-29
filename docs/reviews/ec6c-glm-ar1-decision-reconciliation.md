# EC6C-GLM-AR1 — Decision reconciliation

**Project:** Penghou.Hetu (future Andirit)
**Actual reviewer:** GPT-6, Codex runtime; exact serving build/version is not exposed.
**Reviewed design tip:** d12fe1882c814a0863f872e3913ddff921d31bee
**Reviewed tree:** fed4ff117b28899ccbb9032d0fded2084dc59523
**Review branch:** review/ec6c-glm-ar1
**Baseline:** accepted EC-6B commit 038295f26ce76a8747e923f9e0ef971e773c166e, tree 8779e00d57bd0b40a3ff89d16ca771e79cc950bf
**Scope:** independent architecture review only. No production implementation, design remediation, tests, probes, migration, push or merge were performed.

This table is the compact owner-decision record; the main report contains the detailed source contracts, counterexamples and analysis. A “proposed” status in EC6C-D0 is not owner acceptance.

| Decision | D0 proposed choice | Independent assessment and runtime evidence | Recommended owner disposition | Track / future implication |
| --- | --- | --- | --- | --- |
| D-EC6C-01 Representation | Export-entry node plus file-to-entry Structural occurrence and declaration-to-entry attribution; reject declaration-only and hybrid models. | Entry nodes separate public name from declaration facts and support names without a local declaration endpoint. Existing graph model distinguishes nodes, edges, declaration records and properties. | Accept the entry model for structural positive observations, with completeness/absence qualifications. | A; provides future EC-6D endpoint. |
| D-EC6C-02 Name identity | One entry per module path and exported name; no namespace in identity; aggregate duplicates. | Repository-relative module path is a stable scope; exact name is ordinal. A shared Item can represent a name slot but not one type/value binding. | Accept module+name identity only with normative statement that the entry is a name slot with separate namespace projections. | A; EC-6D must match namespace. |
| D-EC6C-03 Occurrence identity | One exports edge per written occurrence; include kind, local/via details and UTF-16 span; collision fails loudly. | Span anchoring and UTF-16 match accepted contract. Properties are not identity. Existing AddEdge TryAdd can suppress a colliding edge before validators. Named re-export source name remote is omitted. | Accept with source-side name in re-export facts/discriminator and pre-insertion same-ID collision validation. | A implementation gate; same rule for later attribution. |
| D-EC6C-04 Attribution eligibility | Direct span or unique module-scope lookup; Resolved for path-scoped kinds, Derived for name-converged kinds. | p8 has no module-scope binding table. Physical declaration records can share one path+name node, e.g. merged same-file interfaces, so “path-scoped” does not always mean occurrence-specific. | Reject as written. Restrict Resolved to a target with one eligible physical record; specify conservative lookup. Defer name-converged attribution. | B stages 6C.3/6C.4 blocked pending remediation. |
| D-EC6C-05 Evidence and provider | Structural extraction provider; separate versioned export resolver; Resolved/Derived by D-04. | Provider split is coherent. Global evidence rules require Derived never be presented as Resolved. Evidence.Location plus declaration-record join can identify an occurrence, not a declaration node by itself. | Accept provider separation; revise the claim and restrict Derived to an explicitly defined occurrence association if retained. | B and EC-6D proof rules. |
| D-EC6C-06 Type/value namespaces | Metadata rather than identity; type/value/both/unknown; type-only on occurrence. | Sound for a name slot. Aggregate both does not say which declaration or occurrence satisfies an import. | Accept only with explicit projection semantics, occurrence flags, and EC-6D namespace-match requirement. | A refinement; B/EC-6D gate. |
| D-EC6C-07 Default representation | Every default syntax produces entry named default; anonymous/expression forms have no attribution. | Structurally truthful, but anonymous/default surface entries are a new decision. EC-6A D-11 deferred anonymous default exports with no export edge/binding target; it is not prior acceptance of a name-only entry. | Owner must explicitly accept the name-only default entry exception, or omit it from first slice. | A owner decision. |
| D-EC6C-08 Ownership/publication | All EC-6C facts are owned by per-file source unit. | Existing publication atomically stages and replaces source units and validates same-unit endpoints. No export-fact lifecycle has run. | Accept in principle. Require add/remove/empty/delete/failure/cancel/durable-reopen qualification on both stores. | A lifecycle evidence gate. |
| D-EC6C-09 Remote/import surface | Emit Structural name entries for re-exports and imported local exports; no declaration target; preserve via-module. | EC-6A D-06 was deferred as an imported-binding boundary, not accepted for new surface entries. The named form export { remote as publicName } from "./dep" loses remote in D0. | New owner decision. Either defer forms in EC-6C or accept Structural entries with public name, source-side name, specifier and clear classification. | A scope decision; declaration/remote binding remains EC-6D. |
| D-EC6C-10 Query visibility | Existing outgoing/incoming traversals and exact-name query; no new API. | Traversals expose edge kind/evidence/properties, but property filters are client-side and may follow truncation. Exact QualifiedName lookup returns all node kinds; adding entry nodes changes candidate sets. Coverage is not queryable. | Accept only with kind-filtering/truncation guidance, additive exact-name compatibility note, and coverage query contract. | A qualification; B clients must not infer binding from endpoint alone. |
| D-EC6C-11 Version/migration | Bump shared ECMAScript profile p8 to p9 and both plugins; keep resolver2.relative-v1; no identity migration; authentic p8-to-p9 proof. | Planner version comparison supports invalidation in principle. No export implementation or p9 migration has been executed. | Accept as a plan; require authentic durable p8 seed/reopen/reindex and byte-stability assertions for all existing IDs. | A qualification/test gate. |
| D-EC6C-12 Coverage | Add per-kind counters; optional deferredTargets counter; leave unresolved resolver count unchanged. | Current extraction coverage is run-level; ordinary indexing diagnostics drop it; it is not persisted in graph query results. Thus proposed counters cannot distinguish empty from partial through the advertised query path. | Reject as sufficient as written. Define the counter policy and publish per-file queryable complete/partial/deferred status and omission reasons; or narrow to positive-only semantics. | A blocker for completeness; B needs visible attribution outcomes. |
| D-EC6C-13 Unattributable/absence | Keep readable surface entries; no fabricated declaration; absent attribution means not established; classify reasons. | This is the right semantic rule, but consumers cannot currently query omitted/unsupported reasons. | Accept after D-12 coverage visibility; state absence is non-evidence. | A and B qualification. |
| D-EC6C-14 EC-6D boundary | Export entry is future import-binding endpoint; defer closure/remote origins; do not promote Derived automatically. | Good separation of milestones, but shared namespace slots and name-converged endpoints need binding constraints. | Accept boundary with mandatory namespace projection matching and no Derived-to-Resolved promotion. | EC-6D design gate; possible cross-file ownership/dependency tracking. |

## Owner actions before Track A acceptance

1. Ratify the shared module+name slot and namespace projection rule.
2. Choose per-file queryable completeness or explicitly positive-only semantics.
3. Record default name-only entries as a new decision or exclude them.
4. Decide whether re-export/imported-local surface entries are included; if included, add the remote source name to the model and identity discriminator.
5. Document that exact-name lookup returns all node kinds and may become ambiguous.
6. Require a collision guard at ID-dictionary insertion.
7. Correct EC-6A decision-history wording: D-05 was gated, while D-06 and D-11 were deferred; the D0 exceptions need fresh approval.
8. Keep declaration attribution out of the structural slice. Reopen D-04/05 only after module-scope and physical-record uniqueness are specified.

## Separate verdicts

- **Track A — Structural export-surface representation:** READY WITH QUALIFICATIONS.
- **Track B — Local declaration attribution:** DESIGN REMEDIATION REQUIRED.
- **Overall review:** READY WITH QUALIFICATIONS.

**Smallest safe milestone:** revised EC-6C.1 plus EC-6C.2; positive Structural name occurrences and module+name entries only; no exported-as; a queryable per-file coverage marker or explicitly positive-only contract; type/value projections defined; p9 invalidation; per-file atomic replacement; collision checks. Exclude named re-exports and imported-local exports until D-09 and metadata are corrected. Owner acceptance is required before implementation.