# EC6D-SCOPE-Q2 — Independent remediation verification

Final assessment: 2026-10-27 (Asia/Manila). Repository: `jenolaszlo-sketch/penghou-hetu`. **Review only; no production implementation.**

## A. Git binding, isolation and review history

| Role | Commit | Tree | Direct parent |
| --- | --- | --- | --- |
| Canonical main | `558f8ac82ac1b5e07da68f4a71a70240eaf38409` | `08a01a1b7d5d3109b991bcd05d57272fd3f5f018` | `0da1f56d3afc4448fb04e33a6aa84ebff3513599` |
| EC-6C acceptance | `2c3090cba6d8a56102a5c47ceb026c5b5fea3234` | `ece772f1f2b2497891f4019d0d85f4d852b197e1` | `5e79418f8b3864e8b829a4be10ab62c6b5744d89` |
| Recovered planning | `91a3ed53a256e89ed71d7d11c204252c417400a6` | `d79e5814485040a36c77bcde332ebc937f312476` | `3b5e8d91e89e082fd236452656c1d66a4afd3662` |
| **Reviewed R1** | `0e1b213737d42c74df20aef4744ab5718058e266` | `ddf777aa155852daca40b6ed8748c028c67d5822` | `91a3ed53a256e89ed71d7d11c204252c417400a6` |
| Q1 binding record | `d2bb7b1c6d2c760ed044ad31994a90d8665fd0dc` | `aa65551d7a93082a195715b8aa8c86a3a348a0d8` | `5ba43e1cd23871c55e01d9201a492a32dff09ae6` |

Q2 branch: `review/hetu-ec6d-scope-q2`; worktree: `C:/Users/Laszlos/source/repos/Solo/hetu-ec6d-scope-q2`. This chat created it directly at R1 in the existing planning mirror. Its checkout/index/branch are isolated; its objects are shared with that mirror, not canonical. No repositories were consolidated.

The review was interrupted by a tool-approval usage limit after producing evidence. On resume, two additional review commits existed: `575712074cae22ea98312864b78c950ecc56e411` (tree `209d0df6c3131b1e814cff0e05cba46953e5568a`, parent R1), and `6e34452647fb28ee12be14798142921838f997bd` (tree `55ea07415c97792710151392c6b1ae9eef74f776`, parent `5757120`). They and their fixtures/outputs are preserved. This report reconciles them with this chat's original evidence, which was review-authored, not “setup-generated.” The prior report's claims that every Q1 issue was resolved and that merging cannot break attribution are superseded here. The verdict remains REMEDIATION REQUIRED.

Final content binding is recorded in O. A commit cannot embed its own literal hash without changing it; the completion handoff supplies the exact final binding-record commit/tree/parent.

## B. Verdict

**REMEDIATION REQUIRED.** Recovery is authentic, but three P1 contract defects block implementation authorization:

1. **Q2-01:** import-first selects an imported declaration under valid local shadowing, and A05 requires confirmation of conflicting source.
2. **Q2-02:** graph-node uniqueness does not establish unique physical declaration or visible module binding; merging and nested declarations defeat the rule.
3. **Q2-03:** candidate discovery can miss supported export aliases and the existing A10 barrel; unsupported heritage disappears before querying.

No P0 remains. Query-time joins and qualified results are reasonable directions, subject to concrete repairs. Correctness-critical decisions cannot be left to implementers under ACCEPT WITH QUALIFICATIONS. Targeted planning remediation is sufficient; full language resolution is not required.

## C. Recovery and cross-environment identities

### C.1 Recovery authentication

[Git evidence](ec6d-scope-q2-evidence/git-before.json) records commands, exit codes, identities and protected tracked-file SHA-256 hashes.

- Original commit/tree/parent and R1's direct parent match exactly. Both descend from EC-6C acceptance and canonical main; all four ancestry checks exit 0.
- `git fsck --full` exits 0 without missing/corrupt objects. One dangling blob (`33d1e31902c15a6a09554f7f30c4bec845e6c207`) is not a connectivity failure.
- Original planning adds **33 paths**, all under `docs/planning/ec6d/`: eleven planning documents, thirteen TS sources plus corpus README, harness/project/analysis README, observations and four test logs. Corpus tree: `014da71f0855cdc957fa9b6a0667bca565145092`.
- R1 modifies **eight files and adds six**, all under that planning directory. Production/tests and original corpus/harness/observations/test logs are unchanged. Original document versions remain in Git.
- Original commit/tree remain absent from canonical's object database. Neither database has an `objects/info/alternates` bridge. Mirror origin is a local bundle; canonical origin is GitHub. This independently explains Q1's search failure.
- Canonical main and origin/main match the assigned SHA. A read-only remote query finds no EC-6D planning heads. That corroborates current unpublished status, not the historical universal claim “never pushed.”
- Q1 report/harness/results were read at `5ba43e1`/`d2bb7b1`; blob identities were recorded. Q1 evidence was not rewritten.

The [resume audit](ec6d-scope-q2-evidence/git-resumed.json) confirms original-planning, R1, Q1 and canonical tracked files, HEADs, branches and statuses exactly match the initial audit. It does not claim knowledge of pre-audit activity or ignored files not enumerated by the audit.

### C.2 Six different guarantees

The unchanged harness was built in Q2 and run against copied original working bytes, exact Git-blob LF bytes, Q2 CRLF bytes and repeated CRLF bytes. Protected worktrees were only read. See [digest reconciliation](ec6d-scope-q2-evidence/digest-reconciliation.json).

| Bytes | IndexIdentity | CanonicalFactsSha256 |
| --- | --- | --- |
| Original mixed LF/CRLF; original values reproduced | `667ec2d4df6b821e63b2f7d1adad6c63492aaf453b9af639f1aa6e9b0fa77ec8` | `05bf7c7de0e5e1a552c77759cc3bba39aff8d72e093405379e5acfd2d99b0d3c` |
| R1/Q2 CRLF; R1 values reproduced twice | `5c866e16796b6784327a5366b93ba5926e44abdad10841b2e04533ee6a63f5eb` | `631c0ab3960790d714f824259cede7e7da89c3fc930f410b109c8cc4c44ecb29` |
| Exact committed blobs, pure LF | `7d77066510bf469e86dc5405a7c55b33d287aeea862778cc5b3637b809a4cfc9` | `547fca187c8678fcbbb4fd609efe82547dda4b51cb27d12aec709af7767dabf6` |

1. **Git blobs:** filtered `git hash-object` agrees with committed blobs for every corpus file. It does not establish raw-byte equality; `--no-filters` and SHA-256 distinguish that.
2. **Working bytes:** differences are entirely line endings. R1's pure-LF explanation is inaccurate: original files include CRLF at EOF. Original `src/other/Renderable.ts` SHA-256 is `c8ccb2c6033a49afda1499e778f52764cc2cee11d4baccd0f5eb8a4b5092e47c`; the LF blob's is `8f06fbb95584007fb055e54bc76ac72652723eb11edd4545fed53d22f94517e7`.
3. **Graph identities:** all **64 node IDs and 99 edge IDs** match original, R1 and Q2. Counts: 14 units, 73 node contributions, 23 declarations; edge mix 23/23/13/5/17/1/1/16. This is not equality of every fact property.
4. **IndexIdentity:** includes repository, plugin versions, paths, source hashes and consistency metadata; see [CodeIndexIdentity](../../src/Penghou.Hetu/CodeIndexIdentity.cs). Raw-byte changes correctly change it.
5. **Canonical-facts digest:** includes file-node content hashes, so it also changes. Both historical pairs reproduce from their actual bytes. No nondeterminism was demonstrated.
6. **Publication consistency:** false throughout. Stable IDs/digests and durable reopen do not establish an immutable source snapshot.

Legacy FindImplementations returns **one seed node, zero edges**, not R1's “0 nodes, 0 edges.” R1's consumer-baseline appendix incorrectly says Q1 ran the authentic corpus; Q1 used equivalent fixtures because it could not locate it. These are Q2-08 documentation corrections.

**F-01 verdict:** authenticated recovery; RESOLVED WITH QUALIFICATIONS for evidence wording. Determinism is compatible with identical source bytes. Semantic graph equivalence must never substitute for byte-identical indexing.

## D. Q1 findings reconciliation

These dispositions concern planning defects, not implemented EC-6D features.

| Q1 / severity | Original observation | R1 remediation | Independent verification | Remaining deficiency | Disposition |
| --- | --- | --- | --- | --- | --- |
| F-01 / P0 | Target absent from searched stores | Exact mirror recovery | Commit/tree/parent/ancestry, change surfaces, both digest pairs reproduced | Mixed-line-ending, seed-node and Q1-reproduction wording; historical claim bounded | **RESOLVED WITH QUALIFICATIONS**, Q2-08; no identity blocker |
| F-02 / P1 | Missing import/declaration facts | Fact A/B and five-step join | Necessary facts correctly identified | Scope, physical uniqueness, span identity, same-file/provenance/lifecycle contradictions | **OPEN**, Q2-01/02/04 |
| F-03 / P2 | Slice lacks implementation-query utility | Four-class query-level success | Zero legacy relationships reproduced; expected four source-grounded | Depends on repaired binding/query contracts; demand unvalidated | **RESOLVED WITH QUALIFICATIONS** at scope/value-contract level; L |
| F-04 / P2 | Legacy traversal cannot express unknown impact | Additive V1 classifications | API boundary appropriate | Candidate loss, unsupported loss, category overlap, ordering/bounds | **OPEN**, Q2-03/04/07 |
| F-05 / P2 | Source consistency not guaranteed | Mandatory qualified policy | Provider false/null; hashes separated from consistency | Publication identity/truth table, integrity and consumption safeguards | **OPEN**, Q2-05 |
| F-06 / P2 | Barrels must not disappear | Barrier detection/coverage | p9 retains kinds for resolved export sources | Ordinary-import false barrier, unresolved invisibility, A10 omission | **OPEN**, Q2-03/06 |
| F-07 / P3 | Cite heritage namespace | Explicit binding-contract citation | Path/name heritage namespace distinct from declared symbols | None for that citation; p8/p9 correction separately Q2-04 | **RESOLVED** |
| F-08 / P3 | Legacy preset integration deferred | Explicit opt-in boundary | Presets unchanged; seed-only behavior reproduced | Any future preset change separately authorized/qualified | **RESOLVED WITH QUALIFICATIONS**, intentional deferral; L |

Q1 report and scenarios A/B/C/D/F/G1–G5/H were examined. G2 “ground truth” was prose, not a compiler test; Q1 section E itself calls the duplicate-local shape unknown/unsupported. Q2 does not inherit the contradictory import-precedence assertion unchecked.

## E. Symbol-binding and representation assessment

### E.1 Import-occurrence facts

Fact A's names, aliases and type-only status are necessary. Named type-only imports are eligible in type-position implements clauses. Remaining requirements (Q2-04):

- `specifierSpan` means the **module string token** in §1.1, but §1.2 demands a separate span for each member of `import { A, B as C } from './m'`. Members share statement/module spans. Stated identity inputs cannot distinguish them or identical repeated specifiers. Define distinct binding-specifier and module-specifier spans plus exact discriminator inputs.
- Adopt one-based, end-exclusive UTF-16 coordinates, quoting/raw text boundaries, ordinal comparison and existing normalized repository-relative path/relative-v1 policy explicitly. Define null fields for dynamic/side-effect forms with no local binding.
- Persist source-hash/index-unit/publication binding for spans. Provider/version/location alone does not identify source revision; stable occurrence ID does not prove unchanged text.
- Reject duplicate local names before selecting a target, including mixed eligible/ineligible forms. The query enum names this failure; binding step 2 lacks the guard.
- Syntax ownership is per-file; resolutionStatus/resolvedTarget also depend on target inventory. Target creation/deletion or a higher-priority resolver candidate can change them without an importer edit. Recompute or join current resolver-unit facts. Importer replacement alone is insufficient.

Unsupported forms and missing modules must remain observable. Repeat-index/durable criteria are appropriate but future Fact A behavior has not been executed.

### E.2 Export attribution and declaration identity

The [identity contract](../ecmascript-symbol-identity.md) says same-file same-name interfaces converge to one node with multiple physical declarations. `merged` measured **one Widget node, two physical declarations, two exports**. Fact B's “exactly one declaration node” test passes the merging case the text excludes. A target CodeNodeId cannot select one physical declaration. Valid merging is not intrinsically wrong TypeScript; treating it as unique physical declaration contradicts the milestone's exclusion. The earlier Q2 conclusion that node convergence therefore attributes correctly did not establish that exclusion or handle invalid merges.

Same-file is not same scope. `nested-export` has only `Hidden.Widget`, while module-scope `export { Widget }` cannot see it (TS2304). Same-file/local-name cardinality can wrongly attribute it to the nested interface. Unknown namespace projection is not scope proof.

Require a unique **module-scope binding and physical declaration occurrence**, containment/scope eligibility, and declaration record ID alongside node ID. Count physical declarations before deduplicating symbols. Reject merged, ambient/global, recovered or unmodeled scope cases observably.

The valid `type-value` fixture has interface and const Widget. Refusing the ambiguous name is conservative and safe; selecting the interface while discarding namespace/export meaning is unjustified. Inspect p9 export-kind, export-via, local-name, namespace-projection and type-only fields. TypeScript's separate type/value/namespace meanings are documented in the [official handbook](https://www.typescriptlang.org/docs/handbook/declaration-merging.html).

Local export aliases must resolve visible unique bindings. Several public aliases of one unique declaration are legitimate separate entries. Multiple occurrences per entry need occurrence-specific witnesses and an all-contributors ambiguity rule; one good occurrence cannot hide a conflicting contributor. Default/re-export/star/namespace/export-equals/missing/unsupported forms remain unattributed with explicit downstream reasons. Detectable conservative exclusion suffices; full namespace semantics are unnecessary.

### E.3 Import-first and five-step proof

TypeScript 5.9.3 reports TS2440 for the import/local collision. Recovery may retain an imported alias, but invalid-source recovery cannot justify confirmation. More decisively, `nested-shadow` has no diagnostics and its class implements local `make.Local`, not imported Widget. p9 emits its heritage. R1's file-wide import-first rule falsely selects the imported target in valid syntax.

Use explicit lexical-scope eligibility and exactly one eligible visible binding. A narrow first slice can restrict confirmation to module-scope simple-identifier class implements clauses, rejecting relevant shadowing, duplicate bindings, type parameters, ambient/global and parser-recovery hazards observably. Nested support requires scope-aware lookup facts. Do not replace import-first with local-first guessing.

Pin heritage syntax and owner kinds: class value extends, interface type extends and class implements differ. p9 inherits covers several forms and drops expressions. “extends-type” needs an exact definition or removal from this class-implementation slice. Transitive impact stays excluded.

The direct-import chain is sound only with those guards: heritage occurrence → visible import binding → exact relative-v1 import-resolution occurrence → target-file exported-name occurrence → unique physical declaration of the correct kind/identity. Resolver joins require source file, statement span, raw specifier, reference form and publication, not endpoint files alone. Type-only status is per specifier, not an aggregate mixed-statement flag. No step uses global name matching.

Same-file binding needs a short proof variant: no import/resolution/export path exists, and its interface need not be exported. Mandatory five stored steps in A13/query §3.1 contradict this supported route.

### E.4 Representation ADR

**Recommend query-time joins** over persisted syntax/attribution/resolver facts. They avoid persistent cross-file binding-edge invalidation, support reopen and propagate ambiguity. Adjacency/lookups suit the fixed short chain; any inventory fallback must charge scanned candidates/witnesses to observable budgets. No benchmark is necessary here.

R1 nevertheless requires a binding edge/fact identity and stored verification of every step while recommending an unstored join. Define a versioned query-derived witness with canonical identity over ordered source fact IDs/policy, or persist it with dependency ownership. Never invent a stored edge ID. Deduplicate classes while preserving separate witness paths, not a flattened mixture.

Representation spelling can be finalized at owner acceptance **after** scope, identity, ambiguity, source-version, resolver recomputation, same-file proof, computed/stored witness and bounded enumeration are fixed. Those correctness obligations must precede planning acceptance, not become implementation discretion. Both stores/new-profile migration still need qualification.

## F. Consumer-query assessment

GetInterfaceImpactV1 must remain additive and distinct from legacy traversal. The present contract requires further semantic decisions.

**Discovery defect:** target Internal is exported as Public, imported as Alias and implemented as Alias in `export-alias`. Target-name matching against heritage/imported names finds neither. Start from target-attributed export entries and incoming occurrences, then scope-matched heritage. Actual A10 BarrelPreview imports Drawable, not Renderable, and has no heritage clause: prescribed discovery misses that required unknown. Replacing A10 with an easier fixture would not repair the original obligation.

p9 emits no heritage edge for `implements NS.Widget`. Fact A alone cannot recover it. Published-graph-only querying needs stored unsupported-site/coverage facts or an explicit not-evaluated frontier; a prose disclaimer is not analysis evidence.

| Challenge | Finding / required semantics |
| --- | --- |
| Possible consumer without attribution | Yes as a **module** dependency, not declaration use. Decide whether missing-export occurrence also has a distinct binding unknown; §§3.2/3.3 conflict. |
| Ambiguous heritage confirmed | Never; A05 and absent duplicate-binding guards violate this. |
| Missing evidence becomes no impact | Prose forbids it, but candidate loss produces empty lists/zero counts. Define evaluated universe and incomplete coverage. |
| Unsupported syntax disappears | Measured for qualified heritage. Store observability or explicit not-evaluated coverage. |
| Complete empty versus incomplete | Three counts/fixed text are insufficient; report evaluated, unsupported, ambiguous, not-evaluated and truncated states/counts within profile. |
| Provenance | Needs source hash/unit origin, physical declaration, complete publication identity, same-file variant and computed witness identity. |
| Ordering/dedup | Specify ordinal/path/null rules, span/ID tie-breaks, nested occurrence/path lists and unknown subjects without IDs. |
| Budgets/truncation | Define charged scan/join work, stage order, pre/post-dedup charging, defaults/invalid bounds, exact versus lower-bound counts. Return-size bounds are not execution bounds. |
| Cancellation/failure | Errors without partial classified results for cancellation/store failure/movement. Failed re-index retains old publication; expose its identity, not freshness. |
| Durable reopen | Published-fact derivation is sound; new facts/V1 reopen unimplemented. Q1 old-fact durability is not V1 evidence. |

Confirmed class relationships, possible file dependencies and unknown occurrences have different subjects. One class's file may also be possible; a file may have supported and unknown occurrences. Apply mutual exclusion to the same evaluated relationship, not the file. Filter module-reference and Fact A forms so require/export-from/namespace/default forms do not become supported named imports merely because files resolve.

## G. Publication-consistency safety verdict

**Retain qualified results with explicit safeguards; R1 is not yet acceptable.** Source consistency, relationship certainty and coverage are distinct. A structurally valid non-snapshot publication supports qualified navigation; contradiction must error, never be excused by qualified status.

Filesystem false/null, per-file hash checking and fail-on-change source were inspected; fresh runs reproduce false. Incremental idle re-emits all units in the second TypeScript run: no mixed-origin defect observed in that path. This is not simultaneous-read proof.

Required safeguards (Q2-05):

1. Include full publication identity: repositoryId, indexRunId, indexIdentity, snapshotIdentity, isConsistentSnapshot. R1 omits repository/run IDs despite promising P/run R. IndexIdentity omits run ID deliberately. Existing [models](../../src/Penghou.Hetu/CodeGraphStoreModels.cs) and [EnsurePublication](../../src/Penghou.Hetu/CodeGraphQueryService.cs) compare the full publication; every join must too.
2. Define a consistency truth table. R1 describes true plus non-null snapshot, yet derives consistent from the flag alone. The constructor permits true/null. Reject it or explicitly define its weaker meaning; never silently claim snapshot authority.
3. Error on contradictory endpoints, mismatched unit versions, superseded units, missing facts in an asserted witness, identity conflicts and movement. Ordinary missing attribution is unknown; a broken purported chain is integrity failure.
4. Preserve qualification in official display/serialization/export. Define navigation opt-in and a checked consistent-only authority path. A required sibling field cannot prevent copying just confirmedImplementations. A wrapper/discriminated result and tested consumption boundaries are reasonable; arbitrary downstream misuse cannot be made impossible.
5. Invalid input, cancellation, query/store failure, movement and internal inconsistency return no classified output. Index provider failure/source drift preserves the last successful publication. Later live edits need not move it or upgrade consistency.

Publication prose says coverage embeds qualification; query's fixed text does not. Correct both. Consistent output still requires language/coverage caveats.

**A17:** test false/null, true/identified, true/null policy, structural errors and qualification-preserving consumption. **A18:** test mid-query cancellation, movement between joins including same-content/new-run, failed indexing retaining old publication, and query/store failures without partial output. Pre-cancellation alone is insufficient.

## H. Independent A01–A20 audit

All rows are **future contract criteria**, not passed EC-6D implementation tests. P/F/+/-/B/R/U = preconditions, fixture, positive assertion, negative assertion, observation boundary, reproducible expectation, uncertainty handling. Y = adequate ordinary case; ~ = partial; N = absent/contradictory. A positive assertion can require an unknown/error for negative syntax.

| ID | P/F/+/-/B/R/U | Audit / required correction |
| --- | --- | --- |
| A01 | Y/Y/Y/Y/Y/Y/~ | Direct type-import/other-file negative good; add scope/uniqueness/incomplete-chain guards. |
| A02 | Y/Y/Y/Y/Y/Y/~ | Imported alias good; add exported+imported alias and mixed type-only members; discovery must reach them. |
| A03 | Y/Y/Y/Y/Y/Y/~ | Across-file identity good; add same-file one-node/two-declaration rejection. |
| A04 | Y/Y/~/Y/Y/N/~ | G3 exports Widget and Gadget, imports Gadget, references unbound Widget. Reason is unresolved-heritage-name, not missing-export/unsupported form. Add true absent-export fixture. |
| A05 | N/Y/N/N/Y/N/N | Unsafe positive; require conflict unknown/no target and valid-shadowing negative or observable exclusion. |
| A06 | Y/Y/Y/Y/Y/Y/~ | Exact target/placeholder negative useful; add same-file proof and actual provider versions. |
| A07 | Y/Y/Y/Y/Y/Y/~ | Exact four source-grounded; future query must meet it. Legacy remains zero edges. |
| A08 | Y/Y/Y/Y/Y/~/~ | Preview possible valid; pin forms/category overlap. |
| A09 | Y/Y/Y/Y/Y/Y/Y | Idle boundary sound; p9 import/no-implements observed, future classification unimplemented. |
| A10 | Y/Y/Y/Y/Y/N/N | Original Drawable missed; add ordinary-import false-barrier, unresolved export, missing declaration, unrelated direct-positive controls. |
| A11 | Y/Y/Y/Y/Y/Y/~ | Missing module unknown appropriate; add unresolved aliases and target-association coverage. |
| A12 | ~/~/~/Y/Y/N/N | G5 has no relevant heritage, includes eligible inline type-only. Add default/namespace/import-equals/dynamic/qualified heritage plus supported inline-type control. |
| A13 | ~/Y/Y/Y/Y/N/~ | Five stored steps contradict same-file/computed witness; add hashes/origins/physical ID/p8 evidence/distinct paths. |
| A14 | Y/Y/Y/Y/Y/~/~ | Define nested ties/dedup; reverse enumeration and multi-witness truncation tests. |
| A15 | Y/Y/Y/Y/Y/Y/~ | Identical-byte condition good; add edit/delete/target-resolution/ambiguity transitions beyond prose. |
| A16 | Y/Y/Y/Y/Y/Y/~ | Reopen criterion sound; pin source/version fidelity/process reopen; future behavior not executed. |
| A17 | Y/Y/Y/Y/Y/~/~ | Expand identity truth table/integrity errors/consumption safeguards. |
| A18 | ~/~/Y/Y/Y/~/~ | Title promises provider failures; add failed index/mid-query cancel/query failure/same-content new run. |
| A19 | Y/Y/Y/Y/Y/~/~ | Preserve old fact IDs/evidence/properties; distinguish intentionally changed unit origins/index identity; exact comparison and migration gate. |
| A20 | Y/Y/Y/Y/Y/Y/Y | Existing suite/preset boundary appropriate; explicit legacy assertions; broad rerun unnecessary for review-only changes. |

## I. Independent counterexample evidence

[cases.json](ec6d-scope-q2-evidence/cases.json) contains fourteen independent corpora. The [p9 harness](ec6d-scope-q2-evidence/harness/Program.cs) uses unchanged production APIs. The [TypeScript oracle](ec6d-scope-q2-evidence/ts-oracle.cjs) uses TS 5.9.3 diagnostically, not as a proposed product dependency. Outputs: [p9](ec6d-scope-q2-evidence/p9-adversarial-results.json), [TS](ec6d-scope-q2-evidence/typescript-results.json), [18 evidence assertions](ec6d-scope-q2-evidence/audit-results.json).

| Fixture | Verified existing observation | Required future contract result, not executed |
| --- | --- | --- |
| collision | Local heritage; TS2440/TS2420 | Conflicting-binding unknown, no guessed target |
| nested-shadow | Valid TS binds local; p9 retains make.Nested/make.Local | Correct scoped route or observable exclusion, never imported Widget |
| merged | One node, two physical declarations/exports | Unsupported/ambiguous merge, no unique-physical-declaration confirmation |
| wrong-export | Gadget import, unbound Widget TS2304 | Unresolved-heritage-name, no Widget confirmation |
| nested-export | Only Hidden.Widget; module Widget export TS2304 | No attribution to invisible nested declaration |
| barrel | Named/star kinds persist; missing source no edge; ordinary import also resolves | Distinguish barriers/coverage; independent Direct remains valid |
| idle | Resolved import-type, no implements; valid TS | Possible module only, no semantic-use claim |
| duplicate-imports | Two resolution targets; TS2300 | Duplicate-local-name, no first-wins |
| multi-specifier | One resolution statement, separate W/G heritage; valid TS | Distinct binding-token spans; inline type-only eligible |
| export-alias | Public exports Internal, Alias heritage binds Internal | Discover through attributed Public entry |
| unsupported | NS.Widget heritage absent; default/missing-alias heritage present | Stored unsupported-site or not-evaluated coverage; default ineligible/missing unknown |
| type-value | Interface/const distinct nodes; valid TS | Conservative ambiguity, no guessed namespace |
| same-file | Heritage without import/resolution | Short same-file witness or explicitly remove route |
| repeated-export | One/Two slots name Widget; valid TS | Separate alias witnesses, deterministic dedup |

Previously committed fixtures/runs/tools remain preserved as additional evidence. Their namespace-shadow/type-parameter observations complement these corpora. No hypothetical binding output is labeled production behavior. All proposed import facts, attribution and V1 query remain unimplemented.

## J. Re-export coverage and compatibility/integration boundaries

R1's missing-entry plus **any outgoing resolves-to** rule falsely treats an ordinary importing module as a barrel. Filter stored module-reference values `export-from`, `export-type-from`, `export-star`, `export-namespace-from`. They prove an export-source barrier, not a symbol origin. Multiple sources remain separate witnesses, never first-wins.

The resolver skips unresolved/external/unsupported targets after aggregate counting; no per-occurrence resolution edge survives. `barrel/missing.ts` proves no outgoing edge does not mean no re-export. A resolved source with missing exported declaration (`no-declaration.ts`) remains a barrier, not confirmation. p9 suffices for **some resolved barriers**, not complete detection.

Add minimal stored unsupported-export/reference observations or explicit not-evaluated coverage. Specify A10 discovery without full closure, for example bounded file-level candidate reachability with target association unknown. Distinguish analyzed, known unsupported, known ambiguous and not evaluated. An unrelated barrier must not invalidate independently established Direct; structural publication failure is the separate global error boundary.

[Integration readiness](../planning/ec6d/integration-readiness.md) correctly separates gates. Git diffs show R1 has no production implementation/schema activation. The 65-path main-to-owner delta is the EC-6B/EC-6C dependency base, not evidence p9 is in main. Implementation requires accepted repaired contracts and owner scope/fact/query/ADR/publication-policy approval. Integration separately requires EC-6C authorization, compatible persistence changes, migration rehearsals and A19/A20. Release requires applicable platform/durability/resource/performance qualification.

Preserve Windows-only evidence, unreproducible historical migration hash, staged p6→p8→p9 versus unrehearsed direct p6→p9, and small-fixture limits. Narrow attribution requires explicit authority across the prior Track B deferral; it does not authorize general Track B. This chat performed no merge, migration, push, tag, package publication, release or production implementation.

## K. New findings and required remediation

This register supersedes prior Q2-N numbering. Crosswalk: N01/N02 → Q2-01; N03 → Q2-06/07; N04 → Q2-02/03/07; N05 → Q2-03; N06/N07 → Q2-04; N08 → Q2-08; N09 → Q2-05. Q2-02 and export-alias discovery strengthen the earlier assessment using additional evidence.

| ID / severity | Evidence and impact | Required repair / gate |
| --- | --- | --- |
| **Q2-01 / P1** | Symbol §3.2/A05; collision/nested-shadow: wrong imported confirmation or invalid-source positive | Binding owner: exact supported scope/heritage, unique visible binding, duplicate/shadow rejection; replace A05 **before planning acceptance** |
| **Q2-02 / P1** | Symbol §2.2/§4; merged/nested-export: node count hides physical multiplicity/visibility | Binding owner: physical cardinality, module-scope/namespace/export eligibility, all-contributor ambiguity and exact declaration IDs before acceptance |
| **Q2-03 / P1** | Query §3.3/§6; export-alias/A10/qualified heritage: required results disappear | Query/extraction owner: entry-driven discovery, stored unsupported-site/coverage facts, candidate universe/not-evaluated semantics before acceptance |
| Q2-04 / P2 | Fact A spans/lifecycle, symbol §5/query shape: identity/proof contradictions | Architecture owner: separate spans, hashes/origins, resolver join/recomputation, same-file proof, computed witness identity/order; cite preserved `.abi14.p8` heritage evidence, not p9. Before acceptance/ADR |
| Q2-05 / P2 | Publication §§2–4/response: incomplete identity, true/null conflict, qualification escape | Publication/API owner: state table/full identity/integrity errors/checked official consumption and A17/A18 safeguards before acceptance |
| Q2-06 / P2 | Barrier trigger: ordinary-import false barriers and unresolved invisibility | Query/extraction owner: reference-kind filtering and observable unresolved/not-evaluated frontier without closure before acceptance |
| Q2-07 / P2 | Categories/order/budgets; A04/A12/A18 contradictions | Planning/test owner: exact reasons, independent relationship categories, nested total order, charged work/truncation and missing fixtures before authorization |
| Q2-08 / P3 | Recovery/consumer-baseline: inaccurate pure-LF/Q1-original-run/zero-node wording | Evidence owner: correct text at remediation handoff, preserve historical outputs/commits |

All eight remain open. **Three P1, no P0.** P2 repairs are correctness-contract requirements, not permission for implementation-time invention.

## L. Owner decisions and retained qualifications

| Decision / qualification | Owner | Resolution / deadline | Consequence if unresolved |
| --- | --- | --- | --- |
| Q2-01–07 correctness | Technical/planning/API/extraction owners | Commit repairs and targeted independent re-review before planning acceptance | No implementation authority; not waivable as qualification |
| F-03/R16 utility | Product/consumer owner | Confirm bounded workflow value at owner acceptance | Do not authorize on unvalidated demand |
| Representation/evidence ADR | Architecture owner | Approve exact facts/physical attribution/witness/provider/profile/lifecycle before production work | No extraction/query implementation |
| Publication policy | Publication/API owner | Approve state machine and navigation/authority boundaries before production work | No authoritative unqualified output/query authorization |
| F-01/Q2-08 wording | Evidence owner | Correct at remediation handoff, preserve original records | F-01 remains qualified; inaccurate claims must not recur |
| F-08 legacy deferral | API owner | Preserve presets throughout EC-6D; separate future approval before modification | Legacy stays seed-only here; no contrary release claim |
| Integration/schema/migration | Integration owner | Exact candidate/method approval, rehearsals, A19/A20 before integration | No merge/schema activation/deployment |
| Windows/history/scale limits | Release/qualification owner | Retain caveats; qualify intended platforms/durability/performance before corresponding release claims | No broader platform/migration/scale assurance |

Owner acceptance must bind to a repaired commit/tree and explicitly authorize narrow attribution. Q2 performs no owner acceptance.

## M. Recommended next action

Commission limited planning remediation Q2-01–08, preserving prior history. Independently re-review the binding algorithm, candidate/coverage rules, publication truth table and corrected acceptance rows. Full TypeScript compiler integration, re-export closure, legacy preset redesign and performance work are not prerequisites to this planning repair. Only after satisfactory review may the owner separately approve implementation.

## N. Evidence limits and validation

Windows x64/.NET SDK 10.0.401; TS oracle 5.9.3. No cross-platform, representative-repository, performance or release qualification. This chat built/ran the unchanged baseline, fourteen p9 corpora, one incremental-origin check and eighteen evidence assertions. Historical 388-test logs/Q1 report were examined; broad suites were not repeated for review-only edits.

No new EC-6D query/facts, LatticeDB reopen, migration, cancellation/failure injection or consistent-provider execution was performed. A01–A20 are audited criteria, not passed future-feature tests. Original whole-machine histories, historical push absence and pre-audit worktree activity cannot be proved from present state. Source/compiler observations remain fixture-bounded.

Final validation checks local Markdown links, evidence hashes, `git diff --check`, exact source/review bindings, unchanged production/planning/Q1 evidence, protected tracked-file/status preservation, artifact-only change surface and clean review status. Ignored build output exists only in Q2. See [evidence README](ec6d-scope-q2-evidence/README.md) for commands and [validation](ec6d-scope-q2-evidence/validation.json) for results.

## O. Review commit attestation

The final content binding is recorded in a binding-only follow-up after committing all review artifacts and validation evidence. The containing commit/tree/parent are emitted in the completion handoff and can be resolved without a self-referential hash:

```text
git show -s --format="%H%n%T%n%P" review/hetu-ec6d-scope-q2
```

**EC6D-SCOPE-Q2 COMPLETE: REMEDIATION REQUIRED**
