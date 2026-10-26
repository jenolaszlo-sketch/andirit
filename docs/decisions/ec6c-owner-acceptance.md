# EC-6C Owner Acceptance and Milestone Closure (EC6C-OWNER-01)

**Project:** Penghou.Hetu (future Andirit)
**Milestone:** EC-6C owner acceptance and closure
**Date:** 2026-10-26
**Decision branch:** `decisions/hetu-ec6c-owner-acceptance`
**Disposition:** **EC-6C.1 ACCEPTED WITH QUALIFICATIONS — EC-6C.2 ACCEPTED WITH QUALIFICATIONS — EC-6C CLOSED**

## 1. Decision

The owner accepts the EC-6C.1 structural export shape inventory and the
EC-6C.2 structural export publication based on the completed independent
GLM-Q1 qualification. Acceptance is limited to the independently verified and
qualified scope recorded below and is explicitly **not** an unqualified
acceptance: the qualifications in §4 remain in force.

This decision closes the EC-6C milestone for the accepted scope. It does not
authorize integration into `main`, package publication, release, EC-6D, or
Track B. Independent verification of EC-6C.1 is now complete, and the
EC-6C.2 implementation was independently qualified with no demonstrated
production defect.

**Accepted dispositions:**

- **EC-6C.1: ACCEPTED WITH QUALIFICATIONS** — the accepted shape inventory
  was independently verified (scope, allowlist, exclusions, grammar-grounded
  type-only state, conservative projections, exact UTF-16 spans, A29
  boundaries, deterministic ordering, unchanged p8 graph facts); one LOW
  documentation finding (FQ1-DOC-1) was corrected.
- **EC-6C.2: ACCEPTED WITH QUALIFICATIONS** — the structural publication was
  independently qualified across identity, edges, conflicts, projection,
  Unicode, lifecycle, p8→p9 migration, compatibility, and repository smoke;
  two LOW findings (FQ1-EV-1, FQ1-DOC-1 behavior aspect) are resolved or
  preserved as recorded limitations.

## 2. Evidence basis

### 2.1 Exact bindings

| Item | Binding |
| --- | --- |
| EC-6C.1 handoff | `8c1d5e3faf1fbc33162a2fa87dacfbbdf84f4340` (tree `8a3cc154486bf9796b532ee4db96714fa63763b2`) |
| EC-6C.1 owner acceptance (pre-qualification) | `d81108dd51d61fe79767e15c4adb4c10659836de` |
| EC-6C.2 implementation | `3ab1f144e31155cf3cd4bac87c4739f0a6eccb7a` (tree `d8e763a69dadd957df32c872733be5015593c7d6`, parent `160c9cff0598ab07ade402e206c553e58687900e`) |
| Production / tests / compatibility | `3bf418d`, `b54eb3b`, `b61b45e` |
| Independent qualification | branch `qualification/ec6c-2-glm-q1`; HEAD `54ca7156dbc2354843899a619c05128615e3d806` (tree `3cd18371f9a4b48fd515eb9c79d582fd8ef08851`); test commit `609af02`; worktree clean |
| Qualification base correspondence | qualification branch changes are `docs/…` plus one new test file only; zero production changes — the accepted implementation is exactly the qualified one |
| This acceptance | branch `decisions/hetu-ec6c-owner-acceptance` from `3ab1f144…`; preflight record: `docs/evidence/ec6c-owner-acceptance/preflight.md` |

### 2.2 Reviewer disposition and evidence (referenced, not copied)

- Independent reviewer disposition: EC-6C.1 **VERIFIED WITH QUALIFICATIONS**;
  EC-6C.2 **QUALIFIED WITH QUALIFICATIONS**
  (`docs/reviews/ec6c2-glm-q1-qualification-report.md` at `54ca715`;
  matrix at `568ea7f`; compatibility audit at `54ca715`).
- Focused export suite reproduced: **25 passed / 0 failed / 0 skipped**;
  full solution reproduced: **388 passed / 0 failed / 0 skipped**
  (156 TreeSitter + 160 Hetu + 26 LatticeDb + 46 CSharp); format and
  whitespace checks clean (`docs/evidence/ec6c2-glm-q1/qualification-run-log.md`).
- Independent additional tests: 27-test qualification suite (commit
  `609af02`), all passing; TreeSitter project including them: 183/0/0.
- Migration demonstration: authentic p8 seed from `8c1d5e3` (7/4/12) reindexed
  under p9 (12/4/17, +5 entries +5 edges, both languages), existing facts
  byte-identical, `.p8` evidence retained, `.p9` origins, resolver unchanged,
  close/reopen queries verified (`docs/evidence/ec6c2-glm-q1/migration-p8-to-p9-report.md`).
- Compatibility audit: 18 modified legacy assertions audited (17 correctly
  scoped and necessary; QN-1 note); no previously supported relationship or
  consumer query behavior lost
  (`docs/reviews/ec6c2-glm-q1-compatibility-audit.md`).
- Website smoke: pinned `jenolaszlo-sketch/website@f9cca177…`, nine supported
  files, 33/33 enumerated occurrences matched name-by-name, zero `.astro`
  facts, no warnings
  (`docs/evidence/ec6c2-glm-q1/website-smoke-inventory.md`).
- Qualification final attestation: `docs/evidence/ec6c2-glm-q1/final-attestation.md`.

### 2.3 Findings and resolution status

| Finding | Severity | Status after EC6C-OWNER-01 |
| --- | --- | --- |
| FQ1-DOC-1 — JavaScript type-only-like exclusion claim broader than behavior | LOW | **Corrected** (documentation only) — see §4.1 |
| FQ1-EV-1 — recorded migration snapshot hash `ff6b88…` unreproducible | LOW | **Tooling committed; fresh reproduction recorded; historical hash remains unreproducible and is preserved as a recorded limitation** — see §4.2 |
| QN-1 — unnecessary `ResolvesTo` exclusion in a legacy provider assertion | NOTE | **Preserved as a non-blocking test-maintenance note** — see §4.3 |

## 3. Accepted scope

**EC-6C.1 scope:** immutable internal TypeScript/JavaScript export-shape
observations for the accepted allowlist (direct declarations, local lists and
aliases, imported-local spellings without binding, defaults, explicit
type-only forms, the exact pinned-grammar contextual `type` recovery shapes),
with exact public-name token spans in one-based end-exclusive UTF-16 columns,
A29 public-name validation (1–256 Unicode scalar values, strict malformed
rejection, no normalization/trimming/replacement/truncation), deterministic
ordering, duplicate retention, and unchanged p8 published graph facts. No
declaration lookup, identity, or binding is claimed.

**EC-6C.2 scope:** one module/name `export` entry per distinct supported
public name with the accepted identity
`{language}:export:{lowercase SHA-256(UTF8(modulePath + LF + exportedName))}`,
no `SymbolId`, only the `language` property; one Structural `exports` edge per
supported written occurrence (file → entry) with the accepted occurrence
identity tuple, `export-kind`, `export-via`, optional `local-name`,
`type-only` (true-only), `namespace-projection`, exact public-name token
location, export-surface evidence provider
(`penghou.hetu.tree-sitter.ecmascript-export-surface`,
`{packageVersion}+exports1.local-v1`), fail-closed same-ID/different-fact
collisions, per-file unit ownership with existing atomic replacement/deletion
semantics, p9 profile revision in both plugins with unchanged existing-fact
p8 evidence and unchanged `resolver2.relative-v1`, and positive-only
consumer semantics with no completeness marker.

## 4. Qualifications retained

### 4.1 FQ1-DOC-1 (corrected)

The EC-6C.1 handoff's claim that "JavaScript type-only-like recovery is
excluded" was accurate only for statement-level clauses. The corrected text
(in the handoff, plus the known-limitations list) states that the
statement-level form (`export type { … }`) is excluded while inline recovered
forms such as `export { type Model as Shape }` may publish a conservative
structural occurrence — no `type-only` flag, `unknown` projection, parser
error visible — and that such an occurrence does not establish JavaScript
runtime export validity or complete TypeScript semantic interpretation.
Correction record with original wording:
`docs/evidence/ec6c-owner-acceptance/fq1-doc-1-correction.md`. No parser or
extraction behavior was modified; no diagnostic was suppressed; no new
exclusion rule was introduced; the qualification's coverage claims are
unchanged.

### 4.2 FQ1-EV-1 (tooling committed; historical hash remains unresolved)

The migration reproduction tooling and instructions are committed under
`docs/evidence/ec6c-owner-acceptance/migration-reproduction/` (seed harness
pinned to the pre-publication checkout, reindex/comparison harness, complete
README specifying runtime, bindings, procedures, serialization and
canonicalization rules, encoding, hash algorithm and hashed byte sequence,
expected counts, comparison procedure, and cleanup). A fresh end-to-end
reproduction was executed with that tooling: seed 7/4/12 → p9 12/4/17,
five additions of each kind, existing facts **byte-identical** for both
languages (canonical snapshot SHA-256 `7f0e2bc1…` / `7b8dd5fe…`), resolver
unchanged, `.p9` origins, close/reopen queries verified
(`migration-reproduction/reproduction-attempt.md`,
`reproduction-report.json`).

The historical recorded hash `ff6b88345b744d05caaad9ae0a5a05d1ff4ecf3d462673c06986580561c3a12f`
remains **unreproducible**: 30 documented candidate serializations (live-object
default/camel, publication-snapshot `ToJson`/`IntegrityHash`, canonical
projection and variants, combined fixtures) do not match it, and the original
tooling was never committed. The value is preserved unchanged and is not
presented as reproduced. The limitation affects **only the historical hash
value**; the underlying migration-integrity demonstration is independently
supported by the fresh reproduction's byte-identical existing-fact comparison,
counts, metadata, and reopen behavior.

### 4.3 QN-1 (preserved note)

The unnecessary `ResolvesTo` exclusion in
`TreeSitterTypeScriptPluginTests.ExtractAsync_EmitsImportsAndStructuralInvocationsOnly`
is preserved as a non-blocking test-maintenance observation
(`docs/evidence/ec6c-owner-acceptance/qn-1-note.md`). It is not a production
defect; the assertion was not changed under this decision; cleanup may occur
in a future test-maintenance milestone with appropriate regression coverage.

### 4.4 Independent risk-register limitations retained

All limitations from the independent qualification's risk register remain in
force: positive-only inventory (absence proves nothing); narrow recovered-syntax
allowlist; the unreproducible historical hash; single-platform (Windows)
qualification evidence; LatticeDb qualified through its public store contract;
documentation synchronization responsibility; and small-fixture scale with no
performance claim. See `docs/reviews/ec6c2-glm-q1-qualification-report.md` §7.

## 5. Exclusions

This acceptance does not authorize or claim:

- production integration into `main`, merging, or publication;
- package release or release readiness;
- EC-6D implementation (it remains the next candidate milestone but is
  **not authorized** by this decision);
- Track B implementation;
- new language semantics (including any change to parser behavior,
  JavaScript inline recovery treatment, or the collector allowlist);
- general semantic correctness beyond the reviewed structural contracts;
- migration guarantees beyond the tested p8→p9 profile;
- unqualified behavior for unreviewed providers, stores, or profiles;
- declaration attribution, import/export binding, re-export publication,
  `exported-as`/`binds` edges, or export-completeness claims.

## 6. Decision rationale

Both findings are LOW and neither is a demonstrated production defect:

- **FQ1-DOC-1** is a documentation-accuracy defect: the implemented behavior
  is conservative (no type-only attribution, unknown projection, visible
  parse error) and consistent with the accepted positive-only rule A17. The
  misleading sentence was corrected; no production semantics changed.
- **FQ1-EV-1** is an evidence-maintenance defect: the substantive migration
  properties were independently demonstrated twice (GLM-Q1 and this fresh
  reproduction with committed tooling). Only the historical hash value is
  unreproducible because its tooling was uncommitted; that limitation is now
  explicitly documented rather than hidden.

The independently qualified production properties (identity, edge facts,
fail-closed collisions, projection conservatism, Unicode/A29 boundaries,
per-file lifecycle with both stores, byte-identical p8→p9 preservation,
resolver stability, query compatibility, and the pinned website smoke) were
demonstrated by independent tests and harnesses, not by the implementation's
self-reported results. The qualifications narrow the accepted claims; they do
not invalidate them. The acceptance is therefore traceable to the reviewer
evidence and proceeds WITH QUALIFICATIONS.

## 7. Progress tracker and next milestone

EC-6C.1 and EC-6C.2 are recorded as **ACCEPTED WITH QUALIFICATIONS** and
closed in `ROADMAP.md`, `docs/language-support-roadmap.md`,
`docs/language-feature-matrix.md`, and
`docs/design/ec6c-export-implementation-roadmap.md`. The owner-decision commit
is recorded in the final acceptance attestation
(`docs/evidence/ec6c-owner-acceptance/final-attestation.md`). Remaining
evidence qualifications and their status are listed in §4. **EC-6D is
identified as the next candidate milestone and is not authorized by this
decision**; it requires a separate planning and authorization assignment.

## 8. Authority

The EC6C-A1/A2 architecture, the A29 closure, and the EC-6C.1/EC-6C.2
handoffs remain the accepted design basis for the accepted scope. This
decision does not amend accepted historical review evidence
(the GLM-Q1 artifacts remain unchanged at `54ca715`), and it records no new
implementation authorization.
