# EC-6C.2 Independent Qualification Report — GLM-Q1

**Project:** Penghou.Hetu (future Andirit)
**Review:** Independent qualification of EC-6C.1 (verification) and EC-6C.2 (contract qualification)
**Reviewer:** GLM-Q1 (independent qualification engineer)
**Date:** 2026-10-26
**Qualification branch:** `qualification/ec6c-2-glm-q1` (isolated worktree `C:\Users\Laszlos\source\repos\Penghou.Hetu.review-ec6c2-glm-q1`)

## Dispositions

| Milestone | Disposition |
| --- | --- |
| **EC-6C.1** | **VERIFIED WITH QUALIFICATIONS** |
| **EC-6C.2** | **QUALIFIED WITH QUALIFICATIONS** |

No material production defect was demonstrated in either milestone. The
qualifications are documentation and evidence limitations, enumerated in
§6 and §7. No integration, release, EC-6D, or Track B work is authorized by
this report.

## 1. Git binding and provenance

All binding was verified in the canonical repository before any test ran:

| Binding | Value | Verified |
| --- | --- | --- |
| Repository | `jenolaszlo-sketch/penghou-hetu` | yes |
| Implementation branch HEAD | `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` | yes |
| Expected tree | `d8e763a69dadd957df32c872733be5015593c7d6` | yes (matches) |
| Expected parent | `160c9cff0598ab07ade402e206c553e58687900e` | yes (matches) |
| EC-6C.1 handoff | `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` (tree `8a3cc154486bf9796b532ee4db96714fa63763b2`) | exists; ancestor of HEAD |
| EC-6C.1 owner acceptance | `d81108dd51d61fe79767e15c4adb4c10659836de` (child of the handoff) | exists; ancestor of HEAD |
| Production implementation | `3bf418d` (`feat(treesitter): publish structural export entries`) | in ancestry |
| Export tests | `b54eb3b` | in ancestry |
| Compatibility tests | `b61b45e` | in ancestry |

Full linear history from the EC-6C.1 acceptance to the review HEAD:
`d81108d` → `f73d3e4` → `3bf418d` → `b54eb3b` → `b61b45e` → `160c9cf` →
`3ab1f14`. The qualification worktree was created at the exact implementation
HEAD, on the new branch `qualification/ec6c-2-glm-q1`, clean, and isolated
from the canonical checkout and all implementation branches. Canonical `main`
was not modified; no merge, rebase, reset, push, or package publication was
performed. A disposable detached worktree at `8c1d5e3` was created for the
p8 seed and removed after evidence capture.

Changes identified in the implementation range:

- Production: `3bf418d` — `EcmaScriptExportGraphPublisher` (new, 300 lines),
  profile revision 8→9 with the p8/p9 provider-version split, new
  `CodeNodeKinds.Export`, `CodeEdgeKinds.Exports`, four property keys
  (`export-kind`, `export-via`, `local-name`, `namespace-projection`).
- Tests: `b54eb3b` (596-line publication suite + shape-test updates),
  `b61b45e` (18 modified legacy assertions — audited in the compatibility
  report).
- Documentation: `160c9cf` + `3ab1f14` (handoff, CHANGELOG, ROADMAP, matrices).

## 2. Method and evidence

The qualification matrix (`ec6c2-glm-q1-qualification-matrix.md`, committed
**before** test execution at `568ea7f`) maps every contract requirement to its
specification, expected behavior, existing coverage claim, independent test,
and evidence artifact. Results were filled only from recorded artifacts.

Independent evidence artifacts (all committed under `docs/evidence/ec6c2-glm-q1/`):

- `qualification-run-log.md` — commands, results, failed attempts, remediations.
- `migration-p8-to-p9-report.md` + `migration-report.json` + harness sources.
- `website-smoke-inventory.md` + `website-smoke-report.json` + harness source.
- `../reviews/ec6c2-glm-q1-compatibility-audit.md` — 18-hunk audit, consumer
  checks, documentation audit.

Independent tests: 27 xUnit tests in
`tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportQualificationQ1Tests.cs`
(commit `609af02`), clearly named and separate from implementation tests.
They re-derive every expectation from the accepted EC6C-A2 contract text and
recompute identities with an independent implementation of the documented
formulas. No production code was modified at any point.

## 3. Baseline reproduction

The recorded implementation baseline was reproduced exactly on the
qualification worktree before independent tests were written:

- Focused export suite: **25 passed / 0 failed / 0 skipped**.
- Full solution: **388 passed / 0 failed / 0 skipped** (156 TreeSitter,
  160 Hetu, 26 LatticeDb, 46 CSharp).
- `dotnet format --verify-no-changes`: clean; `git diff --check`: clean.
- Environment matched the recorded one (.NET SDK 10.0.401, net10.0).

These are reproductions of recorded numbers, not evidence of correctness; all
substantive conclusions below rest on the independent tests and harnesses.

## 4. EC-6C.1 independent verification

The accepted export-shape inventory was verified against the EC-6C.1 owner
acceptance and the normative EC6C-A2 architecture, using direct grammar probes
and dedicated fixtures:

- **Allowlist (supported forms).** Direct variables (incl. `var`, multiple
  declarators, Unicode identifiers such as `℘`), functions/generators,
  overload signatures (separate occurrences), classes/abstract classes,
  interfaces, type aliases, enums, nonambient namespaces/modules, local lists
  with and without aliases, quoted unescaped aliases, imported-local lists,
  named/anonymous/async/generator/expression defaults — all observed with the
  documented kind, via, and projection values.
- **Exclusions.** Source-bearing re-exports (named/star/namespace),
  CommonJS assignments, `export =`, ambient wrappers
  (`export declare const`), destructured declarators, nested namespace
  exports, escaped identifiers/quoted aliases with escapes, and all
  A29-invalid names produced **no** observations, entries, or edges. A broken
  trailing alias (`export { a as }`) produced no fact while an independent
  valid sibling in the same list survived — matching A17.
- **Contextual `type` recovery.** The pinned TypeScript recovery shapes
  (`export { type }`, `export { type as kind }`) are recognized exactly as
  documented, with conservative `type-only=false`/`projection=unknown`
  absent an explicit modifier; explicit statement-level and inline type-only
  forms are marked `type-only=true`/`projection=type`. The JavaScript
  statement-level form is excluded. One documentation/behavior mismatch was
  found (FQ1-DOC-1 below).
- **Source locations.** One-based, end-exclusive UTF-16 columns verified
  against independently computed token offsets, including quoted aliases
  (span covers the written string token), multiline statements, and
  supplementary scalars before the name (columns count UTF-16 units while A29
  counts scalars). The whole-construct span is retained internally and never
  published as a graph property.
- **Ordering and multiplicity.** Observations sort deterministically by
  path/span/syntax fields; duplicate written occurrences are retained (two
  `export { x as a }` occurrences produce two edges over one entry).
- **No binding or attribution.** The collector never resolves local names;
  same-name declarations do not sharpen `unknown` projections; no
  `exported-as`, `binds`, or declaration relationship exists anywhere in the
  produced graphs.
- **Preservation in EC-6C.2.** The p8 pre-export fingerprint is byte-identical
  in the p9 shape test, and the persisted migration comparison reproduced the
  unchanged existing fact set byte-for-byte.

**EC-6C.1 conclusion:** the acceptance record's description of implemented
behavior is independently supported, with one documentation qualification
(FQ1-DOC-1). The owner acceptance's own listed outstanding qualifications
(inventory positivity, recovered-syntax narrowness, no completeness claims)
remain accurate and are not defects.

## 5. EC-6C.2 contract qualification (per section)

**A. Export entry identity — verified.** Every distinct supported public name
in a module produced exactly one entry with ID
`{language}:export:{lowercase SHA-256(UTF8(modulePath + LF + exportedName))}`,
recomputed independently from the contract text for every entry in the
fixtures. Name/QualifiedName equal the exact written spelling; identities are
module-scoped (same name in two modules → different IDs), case-sensitive, and
normalization-free (NFC vs NFD aliases → different IDs, exact spellings
preserved). Repeated occurrences share one entry. Entries have `SymbolId =
null` and exactly one property (`language`). Movement invariance and rename
replacement were verified in both stores.

**B. Structural export edges — verified.** Every supported written occurrence
produced one `exports` edge (file node → entry) with the complete property
contract: `export-kind` ∈ {named, default}, `export-via` ∈ {direct, local,
expression} matching the six internal forms, `local-name` present only when
the syntax writes one (absent for default expressions/anonymous declarations),
`type-only` present-and-true only for explicit type-only forms,
`namespace-projection` one of {type, value, both, unknown}. Evidence is
Structural from `penghou.hetu.tree-sitter.ecmascript-export-surface` with
version `{packageVersion}+exports1.local-v1` (recomputed from the plugin
version). `Evidence.Location` is the exact public-name token span (independently
computed, including multiline, quoted, and supplementary-scalar cases); no
construct-span property exists; property sets are exactly the permitted
fields. Export facts are absent from `RelationshipCoverage` and
`UnresolvedRelationships` (also confirmed by code inspection: `Exports` is not
in the session's `CoverageKinds`).

**C. Edge identity and conflicts — verified.** The newline-delimited tuple was
reproduced independently for every occurrence in the fixtures. The presence
bit (0/1), type-only state, projection, and source span each discriminate
identities (construct span deliberately does not — contract-conformant).
Exact-duplicate republication is idempotent. Same-ID/different-fact node and
edge collisions fail closed with `InvalidOperationException` before insertion;
diagnostics include the ID and complete serialized existing and candidate
facts (endpoints, kind, provider/version/location, properties); dictionaries
were verified unchanged after the failed publish. Through the store lifecycle,
a conflicting run leaves the prior publication intact. Ambiguity analysis: the
only free tuple fields are `exportedName` (A29-forbidden control characters,
so no LF) and `local-name` (grammar strings and identifiers cannot contain
LF); even a hostile LF-bearing local name constructed at the publisher
boundary cannot alias two facts because the fixed-vocabulary tail
(`type-only`, `projection`) and digit-only span anchor the tuple from the end
(demonstrated by test).

**D. Namespace projection — verified.** Direct variables/functions → `value`;
default expressions and default functions → `value`; interfaces and type
aliases → `type`; classes, enums (incl. abstract and default classes) →
`both`; ordinary local lists and imported-local lists → `unknown`;
nonambient namespaces/modules → `unknown`; explicit type-only forms → `type`.
No semantic resolution sharpens `unknown` (verified with a same-name
interface and an imported local). JavaScript statement-level type-only-like
forms produce no facts; the inline recovered form is the subject of FQ1-DOC-1
(no false type-only flag, conservative projection).

**E. Unicode and A29 — verified.** 255 and 256 supplementary scalars accepted
with exact spelling and contract identity; 257 rejected with no entry, no
edge, and no truncation; malformed surrogate sequences rejected by the
validator before identity construction (direct validator probe: lone
surrogates, doubled pairs, embedded invalid sequences rejected; a surrogate
pair counts as one scalar); empty, all-whitespace, and control-containing
names rejected in source fixtures; escaped names rejected; no normalization,
trimming, replacement, or truncation anywhere. A distinctive
supplementary-scalar name survived LatticeDb close/reopen with exact spelling,
identity, edge target, and Structural evidence via exact-name query.

**F. Publication lifecycle — verified in both stores.** Export facts are
appended to the owning file's ordinary extraction unit (code-inspection
confirmed: publisher runs inside `profile.Extract`, before any batch for the
file is written; no separate unit, resolution pass, or graph-core change).
Verified lifecycle: initial publication; pure movement (entry ID stable, edge
ID changes with the span); alias rename (both IDs change, stale entry gone);
removal of one of two duplicate occurrences (sibling edge and entry retained);
final export removal (all export facts gone, unrelated declaration of the same
file retained); file deletion (unit and facts gone); injected resolver failure
(run Failed, complete prior publication intact, no partial mutation);
cancellation (run Cancelled, prior publication intact). LatticeDb close/reopen
preserves facts and query behavior.

**G. p8→p9 compatibility — verified (one evidence qualification).** An
authentic p8 seed was produced by the pre-publication implementation at
`8c1d5e3` in a disposable detached checkout and reindexed under p9 from the
same sources (both languages). Observed: p8 7 nodes / 4 declarations / 12
edges → p9 12 / 4 / 17, with exactly five new export entries and five new
`exports` edges per fixture and the expected names. The p9 graph filtered to
non-export facts was **byte-identical** to the complete persisted p8 fact set
for both languages; existing edges retain `.p8` provider versions and
`resolver2.relative-v1` is unchanged; unit-origin plugin metadata progressed
to `.p9` (the invalidation control); new export edges use
`exports1.local-v1`. After close/reopen, exact-name and neighborhood queries
returned the expected facts. The recorded snapshot hash
`ff6b8834…` could not be reproduced from three natural serializations because
the implementation's migration tooling was not committed (FQ1-EV-1).

**H. Regression and consumer compatibility — verified.** The 18 modified
legacy assertions were audited hunk-by-hunk: 17 correctly scoped and
necessary; one unnecessary `ResolvesTo` exclusion (QN-1) that masks no current
behavior. All previously supported relationship kinds remain produced
(structural imports, contains/declares, heritage, invokes-name, resolved
resolves-to with unchanged provider). Consumer checks: exact-name lookup
returns export entries and declarations as distinguishable additive
candidates; declaration navigation selects only symbol-bearing nodes;
containment/reference/impact presets do not leak export facts; no schema
breaking changes (additive kinds/properties only); no new query APIs.
Documentation claims were audited — see FQ1-DOC-1.

**I. Repository smoke — verified.** The pinned website clone was verified
clean at `f9cca1773a32bb517e624cc380e1f6d14e0cc317`. The tracked supported
scope is exactly the nine files (eight TypeScript + `astro.config.mjs`); the
recorded "35,810 source bytes" reconciles exactly as the UTF-16 byte count
under CRLF normalization (`(17,325 + 580) × 2`). The independently enumerated
allowlist (33 written occurrences) matched the p9 output exactly — per-file
counts 1/1/0/12/2/1/10/5/1, with every name, token span, projection, and
identity matching. No parser warnings; zero `.astro` component facts; no
`exported-as`/`binds` edges; no completeness claim asserted. A first harness
run over the raw working directory additionally indexed four gitignored
generated files under `.astro/`; the harness was corrected to mirror a
pristine checkout and the tracked-file run is the recorded evidence.

## 6. Findings register

### FQ1-DOC-1 — Documented JavaScript exclusion broader than behavior (LOW)

1. **Identifier/severity:** FQ1-DOC-1, LOW (documentation/behavior mismatch; no false attribution results).
2. **Violated requirement/invariant:** EC-6C.1 handoff states "JavaScript
   type-only-like recovery is excluded"; the accepted positive-only rule A17
   permits but does not require the observed behavior.
3. **Source location:** `docs/evidence/ec6c-1-export-shape-inventory-handoff.md`
   line 100 (claim); `EcmaScriptExportShapeCollector.CaptureLocalList` /
   `HasJavaScriptTypeOnlyPrefix` (behavior); qualification test
   `Q1_D_JavaScriptStatementLevelTypeOnlyExcluded_InlineRecoveryBehaviorRecorded`.
4. **Minimal reproducer:** index JavaScript source
   `const Model = 1;\nexport { type Model as Shape };\n`.
5. **Expected behavior (per the recorded claim):** no export entry — the
   form is invalid JavaScript and its recovery is type-only-like.
6. **Actual behavior:** `javascript.parse-error` warning is visible, and an
   entry `Shape` is published with `local-name=Model`, no `type-only`
   property, and `namespace-projection=unknown` (the grammar recovers a clean
   `export_specifier` beside an ERROR token; A17 allows valid siblings to
   emit). The statement-level form (`export type { value }`) *is* excluded.
7. **Impact:** low — the published fact is conservative (no type-only flag,
   unknown projection), the parse error stays visible, and positive-only
   semantics prevent completeness conclusions; but the documentation claim
   overstates the exclusion and the statement-level/inline treatment is
   inconsistent.
8. **Required remediation:** owner decision to either (a) narrow the
   documented claim to the statement-level exclusion, or (b) extend the
   JavaScript exclusion to ERROR siblings inside the export clause. Either is
   a documentation/collector decision for the owner; this review made no
   production change.

### FQ1-EV-1 — Recorded migration snapshot hash not reproducible (LOW, evidence)

1. **Identifier/severity:** FQ1-EV-1, LOW (unsupported numeric claim; substance verified).
2. **Violated requirement/invariant:** recorded evidence should be
   independently reproducible.
3. **Source location:** handoff §G (`ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f`).
4. **Minimal reproducer:** run `harness/reindex-p9` against the p8 seed;
   hash the filtered existing facts with the three documented serializations.
5. **Expected behavior:** a recorded hash of a well-defined snapshot should be
   recomputable from its data.
6. **Actual behavior:** none of the attempted natural serializations (projected
   snapshot, re-encoded JSON tree, compact fact stream; TypeScript and
   JavaScript) matches the recorded value; the serialization format lived only
   in uncommitted temporary tooling.
7. **Impact:** low — the substantive migration claim (byte-identical existing
   facts, counts, five additions, metadata progression, resolver stability,
   reopen behavior) was independently demonstrated in full; only the specific
   numeric hash is unverifiable.
8. **Required remediation:** commit the migration tooling (or its exact
   serialization definition) with any future evidence records, or accept the
   hash as an unverifiable recorded artifact.

### QN-1 — Unnecessary test-coverage exclusion (NOTE)

`TreeSitterTypeScriptPluginTests.ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`
excludes `ResolvesTo` edges from the TypeScript provider assertion although
the fixture contains none (its imports are external). No current behavior is
masked; resolver provider/version remain asserted elsewhere. Optional
remediation: drop the exclusion or add a resolves-to-bearing fixture.

### Explicitly not defects

- `export-via=local` is shared by the internal `LocalName` and `LocalAlias`
  forms, so syntactically different spellings with identical published facts
  and spans share one occurrence identity — this is the documented
  identity-over-published-facts design, not a collision.
- The p9 convention of keeping p8 provider versions on unchanged existing
  facts while unit metadata moves to p9 is the documented invalidation
  design and was verified end-to-end.
- Absent excluded features (re-export publication, attribution, `exported-as`,
  `binds`, completeness APIs) — none implemented, none claimed; no scope
  violation found. No unintended implementation of excluded behavior was
  detected in production code, queries, or documentation.

## 7. Risk and limitation register

1. **Positive-only inventory.** Absence of an entry proves nothing; consumers
   must not infer complete export surfaces (documented; enforced by the
   absence of any coverage counter for `exports`).
2. **Recovered-syntax narrowness.** Only the exact documented TypeScript
   recovery shapes (and the JS inline behavior of FQ1-DOC-1) are recognized;
   broader recovery remains omitted with visible parse errors. Owner-owned
   allowlist changes required for new forms.
3. **Unreproducible recorded hash.** See FQ1-EV-1; future evidence should
   commit its tooling.
4. **Single-platform qualification.** All evidence was produced on Windows
   (.NET 10.0.401); no Linux/macOS execution was performed in this
   qualification.
5. **Persistence layer depth.** LatticeDb behavior was qualified through its
   public store/contract surface (including close/reopen, replacement,
   deletion, failure, cancellation, and Unicode round-trips); the LatticeDb
   engine's own internals remain governed by its separate qualification.
6. **Documentation synchronization.** The FQ1-DOC-1 correction should be
   propagated wherever the claim is quoted; the owner acceptance record's
   outstanding-qualification text remains accurate.
7. **Scale.** Fixtures and the website smoke are small; no large-repository
   performance claim is made or implied by this milestone.

## 8. Final dispositions

### EC-6C.1 — VERIFIED WITH QUALIFICATIONS

The accepted export-shape inventory was independently reproduced against its
documented contract: allowlist, exclusions, grammar-grounded type-only state,
conservative projections, exact UTF-16 spans, A29 boundaries, deterministic
ordering, duplicate retention, and byte-identical preservation of p8 graph
facts. Qualifications: (1) FQ1-DOC-1 — the recorded JavaScript exclusion is
broader than the implemented behavior and must be corrected or the behavior
narrowed by owner decision; (2) the inventory remains positive-only with
narrow recovery handling, as its own acceptance record already states. No
production defect was demonstrated.

### EC-6C.2 — QUALIFIED WITH QUALIFICATIONS

The structural export publication was qualified independently across all
contract sections (identity, edges, conflicts, projection, Unicode, lifecycle,
p8→p9 migration, compatibility, repository smoke) with a dedicated 27-test
independent suite, two reproducible harnesses, and a full audit of the
modified legacy assertions. Qualifications: (1) FQ1-EV-1 — the recorded
migration snapshot hash is unreproducible without the uncommitted tooling
(substance independently demonstrated); (2) QN-1 — one unnecessary legacy-test
exclusion; (3) FQ1-DOC-1's behavioral aspect also affects published facts for
invalid JavaScript inline type-like syntax. No production defect was
demonstrated; no excluded behavior was implemented; no consumer-visible
regression was found.

## 9. Recommendation

**Recommend owner acceptance of both milestones with the qualifications
recorded above.** Required follow-ups before integration, none of which are
authorized by this review to perform: correct or narrow the JavaScript
type-only-like exclusion claim (FQ1-DOC-1), and commit the migration tooling
or its serialization definition (FQ1-EV-1). Optional: address QN-1.
Integration into `main`, release, EC-6D, and Track B remain out of scope and
are not started.

## 10. Final state

The qualification branch contains, under `docs/reviews/` (matrix, this
report, compatibility audit) and `docs/evidence/ec6c2-glm-q1/` (run log,
migration evidence + report JSON + harnesses, website smoke evidence + report
JSON + harness), the complete independently reproducible record. The final
commit and tree hashes and the worktree-cleanliness confirmation are recorded
in `docs/evidence/ec6c2-glm-q1/final-attestation.md`.
