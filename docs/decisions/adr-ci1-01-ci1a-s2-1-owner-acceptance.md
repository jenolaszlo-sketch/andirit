# ADR-CI1-01-CI1A-IMPL-01-S2.1 — Owner Acceptance with Qualifications

**Commission:** ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01
**Date:** 2026-11-02
**Status:** `ADR-CI1-01-CI1A-IMPL-01-S2.1 ACCEPTED WITH QUALIFICATIONS`
**Controlling independent review:** `ADR-CI1-01-CI1A-IMPL-01-S2.1-Q1` — **SUPPORTED WITH QUALIFICATIONS**
**S2.2 implementation:** NOT AUTHORIZED

---

## A. Source binding

| Role | Commit | Tree |
|---|---|---|
| S2.1 starting baseline | `acd7ecd22516851ac57169fe19d184f6b10ac639` | `75ebd29380d6331accdc9e5838440d921a4280f9` |
| S2.1 contract and authorized amendment | `bd8e6331da9023cebb211f764d81b978a96650dc` | `d6a8fbd36d1c5a20a228ab6d86710025b81d6ba4` |
| S2.1 production implementation | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |
| S2.1 reviewed final source (evidence-only tip) | `0fa63ed2a0f7f1b5ffe21f3791e93e81eb4cb329` | `f713cb6e4a1dbfb4f531f6971a37893902b41435` |

Ancestry is linear `acd7ecd → bd8e6331 → c02d5eae → 0fa63ed2`; the reviewed production source is exactly `c02d5eae` (the evidence commit changes no source or test file). Canonical `main`, one worktree, no stashes, clean tracked tree, no push (`origin/main` = `31c33a9…`, an ancestor).

## B. Independent review binding

The controlling Q1 package is `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s2-1-q1`.

**Package integrity (verified 2026-11-02):**

- `evidence/REVIEWER-MANIFEST.sha256`: **1,635 recorded entries**, 0 missing. **1,615 entries verify byte-identically in place**; **6 additional recorded documents verify byte-identically** in the preserved copies (`evidence/prior-review-preserved/`). 13 root documents were superseded in the same workspace by a **second, complete independent Q1 run** (this recorder's run; its own self-excluded manifest of 36 entries verifies 36/36), and one log (`evidence/reviewer-final-verify.log`) was rewritten by the earlier run after its manifest was built. The 13 superseded documents are not recoverable; the preserved six and all 1,615 in-place entries are authentic. This is an **evidence-packaging limitation recorded transparently**, not a source or correctness defect.
- The second independent run reproduced the same material results (645/0/0; 22/22 reviewer probes; frozen-vector oracle 793/793) and independently reproduced the two bounded edges Q-F2 and Q-F3 before reading the earlier package's preserved findings.

**Independent evidence accepted with this decision (executed by the independent reviewers; NOT rerun by OWNER-01):**

| Evidence | Result |
|---|---|
| Full regression | **645 passed / 0 failed / 0 skipped** (325 core, 43 native, 157 TreeSitter, 120 C#), both independent runs; producer final TRX counters and 0-warning/0-error build log verified |
| Reviewer probes | **27/27** (controlling package: P01–P22, X01–X05) plus **22/22** in the corroborating run |
| Frozen vectors | Frozen `usage4` vectors, `compctx1` synthetic vector, `compctx2` tuple independently recomputed; `asmctx3`/`usage4` helpers byte-unchanged |
| Project-reference propagation | Conditional public API (`int`→`string`) detected through the actual live `CompilationReference`; transitive propagation; deterministic restore |
| D3/D4/D5 | Preserved in tested scope; zero graph/native/publication/schema changes; 51 added tests, 0 removed/disabled |

No source-identity, manifest, or frozen-vector defect was found by either run.

## C. Accepted source provenance

Accepted only as demonstrated:

- Lifecycle-owned immutable source buffers; single acquisition per source; no re-read after acquisition.
- Raw-byte SHA-256 identity; planned hash verification; non-SHA plans and mismatches refuse.
- Strict BOM-aware UTF-8/UTF-16/UTF-32 decoding; characters and line endings preserved; invalid sequences refuse.
- Roslyn `SourceText` derived from the verified snapshot; syntax trees bound to that text; tree-text substitution, extra/missing membership and same-count-different-membership refused.
- Source substitution after acquisition retains the immutable snapshot and does not alter the compiled input.

## D. Accepted compilation provenance

- Actual syntax-tree membership verified by identity+text; parse options read from the actual trees.
- Material compilation options (all ten R2 fields) bound to the identity; unsupported option variations refused.
- Actual PE metadata references captured into immutable images; identity `file|path|sha256` over the captured bytes; the Roslyn reference is created from that image; uncaptured/substituted/aliased/embed-interop/duplicate references refuse.
- Project references bound to the actual live `CompilationReference.Compilation`; dependency identity is the verified dependency `compctx2`; missing, cyclic, conflicting and mismatched dependencies refuse with no text-only fallback.
- Computed deterministically (canonical ordinal order; no clock/run/process/culture dependence); stale or forged descriptors refuse; cancellation and partial preparation never yield a verified result.

## E. Accepted `compctx2`

- Amendment authority verified: explicit, additive, owner-recorded (`bd8e6331`); canonical tuple/domain/version/encoding specified and implemented exactly.
- `compctx1` unchanged; frozen `usage4` and `asmctx3` preserved; legacy consumers reject `compctx2`.
- Verified project-reference items are `compilation-context|<verified dependency compctx2>`, bound to the exact live instance; dependency DAG is deterministic bottom-up.
- `compilation-universe1` uses contract version `compctx2` and ordered `compctx2` digests only; mixed-version universes are structurally refused.
- `analysisscope1` formula preserved.

## F. Qualifications accepted with this decision

### Q-F2 — BOM-less UTF-16/32 decoding boundary

**Owner disposition: accepted S2.1 limitation; correction or an explicit supported-encoding decision is required before S2.2 acceptance.** BOM-less ASCII-range UTF-16/32 content can decode as NUL-interleaved UTF-8 while retaining an honest byte-hash identity. This is **not** source-byte forgery: the bytes are correctly hashed and the decoded text is truthfully attributed. Universal encoding correctness is not claimed. Later evidence must distinguish supported from unsupported/ambiguous encodings.

### Q-F3 — Lazy PE validation

**Owner disposition: accepted S2.1 limitation; metadata validation/refusal behavior must be qualified before S2.2 semantic acceptance.** Some invalid metadata artifacts (e.g., netmodule, truncated PE) can receive an honest captured identity before Roslyn fully validates usability. A valid content identity does not imply a valid semantic reference.

### Q-F4 — refusal wording

Minor diagnostic-quality issue: a tree path that binds no declared source is reported as `RoslynTextMismatch` together with genuine text mismatches. The refusal outcome is correct and fail-closed. **Disposition:** record; optional clarification in a later implementation slice (not required for acceptance).

### Q-F5 — branch housekeeping

The canonical repository carries ~30 pre-existing historical local branches (none created by S2.1; one worktree). No correctness impact. **Disposition:** record; housekeeping outside this commission.

### Additional qualification A-01 — canonical symbol separator (from the corroborating independent run)

The source-item canonical form joins preprocessor symbols with `,`; a caller-supplied single symbol containing `,` can produce the same identity string as two ordinary symbols. **Not reachable** through accepted project inputs (`DefineSettings` are split on `;`/`,` by discovery) or through any public API; bounded to the internal construction surface used by tests/plugins. **Disposition:** bounded hardening before public exposure or caller-supplied parse-option reuse; does not block S2.1 acceptance.

### Additional qualification A-02 — scope digest composition

`analysisscope1` binds repository + project-unit/path pairs; per-unit declared source lists are enforced by universe construction and by each `compctx2` descriptor but are not part of the scope digest. Consistent with "declared unit set"; consumers must not treat the scope identity alone as a source-inventory digest.

## G. Explicit nonclaims

- OWNER-01 did **not** rerun the tests, probes or oracle; it verified identities, manifests, TRX counters and the recorded evidence.
- No claim of universal encoding correctness, semantic reference validity (Q-F3), Linux/cross-platform behavior, crash/power-loss, multi-process, native fault behavior, or production readiness.
- S2.1 acceptance does **not** establish semantic target identity, occurrence minting, `usage4` production, public Find Usages, or the relationship between `compctx2` and target identity (separately decided in `adr-ci1-01-ci1a-s2-2-target-identity-decision.md`).
- S-01, F12/S2.0, NEW-01, NEW-04, D3/D4 full qualification, historical native fixtures, required Linux qualification and the crash/power-loss/multi-process limits remain open with unchanged dispositions.

## H. Obligations preserved by this acceptance

| Obligation | Gate |
|---|---|
| Q-F2 encoding boundary | Before S2.2 acceptance |
| Q-F3 metadata validity | Before S2.2 semantic acceptance |
| A-01 symbol separator | Before public exposure / caller-supplied parse options |
| S2.2 target-identity mapping | Before S2.2 target minting (decided: Option A, see the S2.2 decision) |
| S2.0/F12 semantic oracle | Before S2.2 qualification-test authoring |
| S-01 defining-target binding | During S2.2, independently qualified |
| NEW-01 projection recovery | Before S2.3 or earlier recovery reliance |
| NEW-04 coverage merge | Before any dependent coverage claim, otherwise before S2.2 acceptance |
| Reader continuation | S2.4 |
| Real-project end-to-end | S2.5 |
| Full D3/D4, historical fixtures, Linux | Assigned later CI-1A gates |
| Crash/power-loss/multi-process | Outside current guarantees unless separately authorized |

## I. Evidence record and recording

This acceptance is bound to the Q1 package state recorded in `docs/evidence/adr-ci1-01-ci1a-s2-1-owner-01.md`. The recorder did not perform an independent security review and issues no second review verdict; the Q1 disposition is cited from its own package.

## J. Formal disposition

**`ADR-CI1-01-CI1A-IMPL-01-S2.1 ACCEPTED WITH QUALIFICATIONS`**

S2.1 is accepted within the demonstrated source and compilation provenance scope, with Q-F2/Q-F3 (and bounded A-01/A-02) preserved as explicit later obligations. S2.2 remains separately authorized.
