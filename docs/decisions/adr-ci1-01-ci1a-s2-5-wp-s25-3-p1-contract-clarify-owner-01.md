# ADR-CI1-01-CI1A-S2.5-WP-S25-3-P1-CONTRACT-CLARIFY-OWNER-01 — owner disposition of the P1 canonical-forms v1 findings

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-CONTRACT-CLARIFY-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-11. **Canonical branch:** `main`.
**Owner disposition:** **`S-01 P1 CONTRACT CLARIFICATION APPROVED WITH QUALIFICATIONS`**.

> This decision disposes of the three findings recorded by the P1 proof
> (`S-01 P1 INTEGRATED PROOF COMPLETE WITH QUALIFICATIONS`, commit `90c5fbee…`):
> **F-P1-01** and **F-P1-03** are **`EDITORIAL CONTRACT ERROR`** (name and example only; no semantic
> effect). **F-P1-02** is a **`NORMATIVE CLARIFICATION REQUIRED`** and is hereby clarified: the
> corrected expectation text describes exactly what the adopted forms compute and what every
> produced identity already is. It is **not** a substantive change to the required identity
> semantics, **not** an implementation defect, and requires **no new canonical-forms version, no
> semantic-digest version, no production remediation, and no re-ingestion**. The clarification is
> recorded in the dated addendum
> [`s01-canonical-form-v1-clarification-01.md`](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md).
> The previously failed R13a/R13b/R13c subcases are **not** converted into historical passes: a
> bounded P1 delta proof (proposed, not authorized) is required before independent P2
> qualification. No production, test, CI, contract-file, or evidence modification is authorized.

## 1. Verified repository preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `90c5fbee03f7826ba81b3b1eef77a4b24f22700f` / `3b2f7959c21804b160de45b372bf9072022be00f` — exactly as commissioned (the P1 evidence commit) |
| Branch | `main` |
| Worktrees / stashes | Exactly one (`D:/projects/Andirit`); no stashes; no concurrent writer |
| Tracked/index state | Clean before modification; only the permitted documentation artifacts of this commission are added |
| Local vs remote | Recorded `origin/main` = `443c1927…`; local `main` **79 ahead**; no fetch/push/remote contact; PR #6 untouched |
| Forbidden operations | None performed (no reset/restore/clean/rebase/prune/branch/worktree/remote) |

## 2. Authority clause mapping

| Authority | Governing role for this decision |
| --- | --- |
| [P0 contract decision](adr-ci1-01-ci1a-s2-5-wp-s25-3-p0-contract-owner-01.md) §§4–10, 12–13 | Adopted v1 §§2–9 "exactly as written"; restates §5/§7 (including the defective passages — historical record, not rewritten) |
| [Canonical-forms v1](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md) §§2–7, §8, §9, §10, §11 | The primary forms (§3–§5), normalization (§2), encoding (§6), the derivative §7 table, context binding (§8), versioning (§9), oracle duties (§10), exclusions (§11) |
| [WP-S25-3 entry decision](adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md) §§3–5 | The eleven-component S-01 chain; the Case-B determination; the adoption checklist and oracle strategy (**neither lists any parameter-modifier item**) |
| [S2.5 entry decision](adr-ci1-01-ci1a-s2-5-entry-owner-01.md) §8 | The S-01 proof contract; component canonical forms are the qualification target, "not a new semantics" |
| [S-01 canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 | The S2.5 completion gate (canonical derivation + full-chain proof); forbids reopening S2.2 |
| [Identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md) | Normative `usage4`/`asmctx3` **encoding**; the C#-level "canonical … forms" are deliberately opaque |
| [Stage-2 acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §1 row 1; §2 row 2 | The binding composed-identity requirements: *"Changed source bytes → new `compctx1`/`asmctx3`/`usage4`"* and *"Different defining context/component → different target; unavailable/external/unbound context → refusal, no fact"* |
| [Stage-2 boundary](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §3.2/§4 | Source/context acquisition and refusal contract (no modifier-specific requirement) |
| P1 report + evidence package | Producer evidence for F-P1-01/02/03, the R13 subcases, and observation O-1 |
| Production sources (`CSharpCodeGraphPlugin.Symbols.cs`, `CSharpCodeGraphPlugin.Session.cs`) | The actual canonical producer inspected for the F-P1-02 assessment |

## 3. Original P0/P1 contract identity and evidence baseline

- **Contract of record:** `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md` (v1) —
  12,636 bytes, SHA-256 `5958dd6bb40da2d44c6845a65d7aee0e8e5c9c06966a4a29653c8dbb6c7f4852`
  (adopted by P0 in commit `349136d…`; **byte-unchanged** — this decision clarifies via addendum,
  it does not rewrite).
- **P1 revision:** `90c5fbee…` / tree `3b2f7959…`; P1 report
  `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md`; evidence package
  `docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/`.
- **P1 baseline re-verified by this decision** (independent re-hash / receipt reads):

| Reported item | Re-verified value |
| --- | --- |
| Known-answer vectors | `10/10` |
| Canonical-form cases | `17/17` |
| §7 matrix | `15/18` pair assertions pass; R13a/R13b/R13c fail against v1-as-written |
| Full solution | 907 passed / 0 failed (52 + 157 + 345 + 353) |
| Frozen R2 oracle | `185/185`; produced artifact 146,851 bytes, SHA-256 `97f6c5e5…39c9` (byte-identical) |
| Integrated proof | targets present in native rows (Add/Greet/Hit all true); reopen `Available` (8/2/1 facts); `StalePublication`/`AnalysisUnavailable` refusals; known-failure preserved; quarantine + reconcile; tamper refusal |
| Evidence manifest | 23 entries, 0 mismatches |

Discipline preserved: passing evidence and contract mismatches remain distinct; the R13 failures
remain recorded failures against the original wording and are not silently converted.

## 4. F-P1-01 assessment (display-format identifier)

| Determination | Result |
| --- | --- |
| 1. Is the v1 name invalid? | **Yes.** v1 §5 requires `SymbolDisplayFormat.FullyQualifiedGlobalNamespaceFormat`. No such member exists in `Microsoft.CodeAnalysis.CSharp` 5.9.0; P1 could not compile the literal and preserved the as-written run (`Q1-ORACLE-v1-text-literal-run.json`, forms 3/9). The documented member is `SymbolDisplayFormat.FullyQualifiedFormat` |
| 2. Exact format used by production | `TypeIdentity(t) = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` (`CSharpCodeGraphPlugin.Symbols.cs`) — confirmed by observed outputs (`method|0|string`; `global::System.Collections.Generic.Dictionary<string, int>`) |
| 3. Relevant display properties | Global-namespace style (`global::` for non-special types) + special-type keywords (`int`, `string`); all other §5 display properties (arrays, tuples, generics as displayed) unchanged |
| 4. Purely editorial? | **Yes** — a name substitution; no semantic behavior changes, no clause meaning changes |
| 5. Oracle expectations change? | **No.** The P1 final oracle already uses the documented format (17/17); the preserved as-written run remains historical evidence of the defect's practical consequence |

**Classification: `EDITORIAL CONTRACT ERROR`.** Exact correction: addendum E-1.

## 5. F-P1-02 — modifier identity semantics (mandatory substantive assessment)

### A. The original guarantee, clause by clause

| Clause | Exact adopted text (quoted) | What it actually requires |
| --- | --- | --- |
| v1 §3 | identifier = "the normalized definition's simple name"; collisions permitted | Modifier-insensitive identifier — **no modifier requirement** |
| v1 §4 | declaration = `docID(N(M))`; "so **overloads are distinguished by the declaration form alone**"; fallback `"<SymbolKind>:<fully-qualified display>"` | For every *declarable* overload set, the declaration form alone distinguishes members. (Value-vs-mode overloads are declarable and `@` distinguishes them; mode-family overloads are not declarable — see §5B) |
| v1 §5 formula | shape = `method|arity|join(",", TypeIdentity(p.Type))` | Types only — the formula itself contains **no** parameter-mode input |
| v1 §5 commentary | "`ref`/`out`/`in` modifiers appear in the display strings and therefore in the shape" | **False** as adopted text: `TypeIdentity` receives `p.Type`; Roslyn type displays never contain the parameter's reference mode |
| v1 §7 row 13 | "`ref` vs `out` (vs value) | = | ≠ | ≠ | declaration/shape ≠ | doc-ID `@`/`%` modifiers; display modifiers" | Asserts declaration- and shape-level distinctness for mode pairs — **not derivable** from §2–§5 |
| P0 decision §9 | "`ref`/`out` → declaration+shape distinct" | Restatement of row 13 (historical record) |
| Acceptance matrix §1 row 1 | "Changed source bytes → new `compctx1`/`asmctx3`/`usage4`" | **Binding:** any modifier-only source revision must produce a different composed target (source bytes changed) |
| Acceptance matrix §2 row 2 | "Different defining context/component → different target; … refusal, no fact" | **Binding:** distinct contexts/components must not share a target |
| WP-S25-3 entry §4 checklist / §5 oracle strategy | forms checklist; perturbation matrix | **No modifier item anywhere** — the modifier row entered only with the v1 §7 table itself |
| Identity contract | encoding only ("canonical identifier-site/declaration-site forms") | **No modifier requirement** |

**Finding (A):** the only clauses requiring modifier-sensitive component forms are v1 §5's
commentary sentence and §7 row 13 (plus P0's restatement). No higher-level authority — identity
contract, S-01 disposition, G-ACCEPT, entry decisions, Stage-2 boundary/acceptance matrix —
requires it. The binding composed-target requirements (matrix §1 row 1, §2 row 2) are satisfied by
the *context* binding, not by modifier encoding — demonstrated next.

### B. Actual behavior (R13a/R13b/R13c, from the P1 receipts)

All three pairs executed on the real extractor; both revisions of each pair compiled into verified
compilations and produced occurrence facts (binding succeeded). Normalization
(`ReducedFrom ?? OriginalDefinition`) has no mode interaction. Documentation IDs (confirmed by
independent RefEnc reconstruction of the exact form strings against the recorded digests):

| Pair | Source before → after | Symbols (normalized) | Doc IDs | Identifier form (digest) | Declaration form (digest) | Shape form (digest) |
| --- | --- | --- | --- | --- | --- | --- |
| R13a value → `ref` | `Change13(int)` → `Change13(ref int)` | `Fixture.Core.Mods.Change13` | `…Change13(System.Int32)` → `…Change13(System.Int32@)` | `Change13` (`230ed9ab…`) in both | **≠** `7001080d…` vs `df577e8f…` | `method\|0\|int` (`928f040e…`) in both |
| R13b `ref` → `out` | `Change13B(ref int)` → `Change13B(out int)` | `…Change13B` | `…Change13B(System.Int32@)` in both | `Change13B` (`2a751972…`) in both | **=** `224f8100…` in both | **=** `928f040e…` in both |
| R13c `ref` → `in` | `Change13C(ref int)` → `Change13C(in int)` | `…Change13C` | `…Change13C(System.Int32@)` in both | `Change13C` (`6be36109…`) in both | **=** `9c3fcb17…` in both | **=** `928f040e…` in both |

Each digest above was independently re-derived in this commission from the literal form string via
the §6 encoding (byte-exact match to the P1 receipts). Facts proven:
`GetDocumentationCommentId()` renders a mode parameter uniformly as `@` for `ref`, `out`, and
`in`; the plain type form differs from the `@` form (value vs mode); and the shape form is mode-blind
in all cases.

Within one compilation vs across revisions: value-vs-mode overloads are **declarable
simultaneously** (the C# rule permits overloading when one method takes `ref`/`in`/`out` and the
other takes none of those modifiers — official compiler-message guidance); mode-vs-mode
declarations **cannot coexist** (modifier-only overloads are rejected; CS0767 confirms the
language treats `ref`/`out` as distinct signatures with no boundary disambiguation). Across
separate valid revisions every one of these changes is a changed-source-bytes event.

### C. Semantic consequences of a modifier-only change between revisions

Determined independently (the commission's instruction not to infer significance from CS0663 is
honored — CS0663 is used only for non-coexistence):

1. **Public signature/metadata changes:** `ref`/`out` become `T&` with `OutAttribute`; `in` adds
   `modreq(InAttribute)`/`readonly` semantics — callee contract and ABI surface change.
2. **Call-site semantics change:** `ref` (must be initialized, may be written), `out` (need not be
   initialized, must be assigned), `in` (read-only reference; call sites may omit `in` under
   compatibility rules) — definite-assignment and argument-passing behavior change.
3. **Overload resolution/binding changes:** adding or removing a mode overload alters candidate
   sets for existing call sites (and can break binary/source compatibility).
4. **Persisted occurrence intent:** a fact whose intended target is "the `out` declaration" must
   not resolve to "the `ref` declaration," and vice versa.

**Conclusion:** modifier changes are semantically significant across revisions. Identity must
therefore distinguish them *where both versions are reachable as targets* — and it does, through
the defining-compilation context component (§5D) and, within a compilation, through the
declaration form for the only declarable mode-bearing overload pairs (§5A/§5B). The original row 13
never needed to — and the adopted forms never could — carry that distinction at the component
level.

### D. Composed identity — which component distinguishes each reachable pair

Composed target = SHA-256 over `["compctx1", compctx1, metadataIdentity, moduleName, identifierDigest, declarationDigest, shapeDigest]` (v1 §6; domain `asmctx3-context`).

| Pair (same name/type/arity/containing type) | Coexistence in one compilation | Distinguishing vector | Basis |
| --- | --- | --- | --- |
| value vs `ref`/`out`/`in` | **Declarable simultaneously** | **declaration form** (`Int32` vs `Int32@`) → declaration digest ≠ → composed target ≠ | P1 R13a digests; independent RefEnc reconstruction; v1 §4 |
| `ref` vs `out` vs `in` (within family) | **Not declarable** (modifier-only overloads rejected; CS0767) | Within one compilation: none needed (only one exists). Across revisions: **defining-compilation context** — the changed source bytes change `compctx1`, hence `asmctx3` | Acceptance matrix §1 row 1 (binding); v1 §8 ("the `compctx1`/… components … still bind the defining compilation"); descriptor construction includes per-source content hashes (`VerifiedCSharpCompilation`); accepted FINAL-Q1 source-change semantics; P1 O-1 receipts (context binding demonstrated) |

No claim of composed injectivity is made merely because several digest components are combined;
the table enumerates every reachable supported pair class and names the exact distinguishing
vector for each. The limits of the "context" vector are explicit: it distinguishes *revisions*
(because revisions change source bytes); it is not part of the three component forms
(direct `compctx1` sensitivity for a modifier pair is recommended as a delta-proof receipt).

### E. Collision and compatibility implications

1. **Do distinct supported inputs map to identical canonical component representations?** Yes —
   precisely the mode-family pairs (`ref`/`out`/`in`) share identifier + declaration + shape forms.
   Value-vs-mode pairs do not (declaration `@`).
2. **Is that permitted by the accepted design?** Yes, under the corrected reading: the adopted
   forms define identity *as* (name, Roslyn doc-ID, type-display shape) bound to a defining
   compilation context; §3 already permits identifier collisions by design, and the declaration
   form's mode encoding is whatever Roslyn's doc-ID grammar provides (uniform `@`). The design
   never promised the three forms to be injective over parameter modes.
3. **Does it violate a mandatory S-01 invariant?** No. The binding requirements (matrix §1 row 1,
   §2 row 2) hold for every reachable pair (§5D): same-context mode-family collisions are
   unreachable (non-coexistence), and cross-revision changes are distinct targets by context.
4. **Persisted source identity:** no within-store ambiguity arises; a persisted target always
   refers to the one declaration that existed in its publication context.
5. **D5:** nothing is relocated or mitigated — no identity values change, so no re-ingestion is
   triggered by this clarification. (Had the distinction required new encoding, D5 would mandate
   re-ingestion under a new version; that path is not taken.)
6. **A hash cannot recover discarded distinctions** — acknowledged and recorded: mode-family
   distinctness is *not* recoverable from the component forms alone; it is carried by
   (i) non-coexistence and (ii) defining-context binding, now an explicit v1 design property.

## 6. F-P1-03 assessment (documentation-ID example)

| Determination | Result |
| --- | --- |
| 1. Correct doc-ID representation | Roslyn documentation-comment IDs use doc-ID notation only: `M:Namespace.OwnerType.MethodName(System.String)` (generic `{…}`, `@` for modes, `#` escaping inside explicit-interface member names — P1 FC-14) |
| 2. Is `global::` appropriate? | **No** — `global::` is a symbol-*display* device (`FullyQualifiedFormat`), not part of the documentation-ID grammar. It remains correct where the §4 **fallback** uses a display string (`Method:global::Ns.OwnerType.Method(...)`) |
| 3. Does production conform? | Yes — actual produced doc IDs (`M:Corpus.Core.Methods.Add(System.Int32,System.Int32)`, `M:Corpus.Core.IGreeter.Greet(System.String)`) verified in P1 at the digest level |
| 4. Is the affected text illustrative? | Yes — one example inside §4; the normative rule is `docID(N(M)) = GetDocumentationCommentId()`, untouched |
| 5. Normative identity expectations change? | **No** |

**Classification: `EDITORIAL CONTRACT ERROR`.** Exact correction: addendum E-4 (note: the same
example text was restated in the P0 decision; that document remains a historical record and is not
rewritten).

## 7. R13a/R13b/R13c evidence matrix (original vs clarified)

| Subcase | v1-as-written required (id/decl/shape) | Observed (P1) | Against original wording | Against clarified wording (addendum E-3) |
| --- | --- | --- | --- | --- |
| R13a (value vs `ref`) | = / ≠ / ≠ | = / ≠ / = | **FAIL** (shape assertion only) | PASS (id =; decl ≠ via `@`; shape =) |
| R13b (`ref` vs `out`) | = / ≠ / ≠ | = / = / = | **FAIL** (declaration + shape assertions) | PASS (all component forms =; pairing excluded from the declaration/shape requirement; composed-target distinctness across revisions via `compctx1`) |
| R13c (`ref` vs `in`) | = / ≠ / ≠ | = / = / = | **FAIL** (declaration + shape assertions) | PASS (as R13b) |

These subcases must be **re-established by execution against the clarified text** (delta proof)
before any future qualification treats them as passing; this decision alone changes nothing about
their historical status.

## 8. Composed-identity and collision assessment (summary)

- **C1 (binding, holds):** every reachable pair of distinct supported declarations is separated by
  at least one of: coexistence-impossibility (mode family), declaration form (`@`; value vs mode),
  or defining-compilation context (cross-revision; context binds source bytes per matrix §1 row 1
  and v1 §8).
- **C2 (recorded v1 design property):** mode-family component forms are intentionally equal;
  identity separation rests on C1's non-coexistence + context vectors. Documented alongside the
  existing unescaped-comma-join property.
- **C3 (no violation):** no mandatory S-01 invariant (identity contract, disposition, entry
  decisions, acceptance matrix §1/§2) requires modifier-sensitive declaration or shape forms.
- **Limits (recorded):** the direct `compctx1` sensitivity receipt for a modifier-only pair is
  design/code-inspection + O-1 evidence today; the delta proof should capture it directly. Finite
  test cases do not constitute a universal proof; the C1 enumeration is clause-level reasoning
  plus executed evidence for the exercised classes.

## 9. Contract versioning and provenance consequences

| Question | Determination |
| --- | --- |
| Editorial erratum? | **Yes** for F-P1-01 and F-P1-03 (addendum E-1/E-4) |
| Authoritative v1 clarification addendum? | **Yes** — [s01-canonical-form-v1-clarification-01.md](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md), dated, effective 2026-11-11, applying prospectively; the original contract file and the P0/P1 records remain byte-exact historical records |
| New canonical-forms version? | **No** — no form, normalization, encoding, or produced identity changes; v1 remains operative with the addendum |
| New semantic digest version? | **No** — occurrence semantics v4 and the `asmctx3`/`compctx1` encodings are untouched |
| Production remediation / re-ingestion? | **None required** — the clarification aligns the derivative expectation text with the primary forms the producer already implements |

## 10. Exact approved clarification

Approved (full replacement text in the addendum):

- **E-1 (F-P1-01, editorial):** §5 format name → `SymbolDisplayFormat.FullyQualifiedFormat`.
- **E-2 (F-P1-02, clarification):** §5 commentary sentence → parameter mode does not participate
  in the shape; doc-IDs render `ref`/`out`/`in` uniformly with `@`; mode-vs-value distinctness is
  carried by the declaration form and mode-family/cross-revision distinctness by the defining
  context, not by the shape form.
- **E-3 (F-P1-02, clarification):** §7 row 13 → corrected relations: identifier `=`; declaration
  `≠` for mode-vs-value and `=` within `ref`/`out`/`in`; shape `=`; digest column as in the
  addendum; basis column corrected (uniform `@`; no `%`; non-coexistence + context binding).
- **E-4 (F-P1-03, editorial):** §4 example → `M:Namespace.OwnerType.MethodName(System.String)`;
  fallback example unchanged (display-based).

**Note:** P1 originally failed the old wording (R13a shape; R13b/R13c declaration + shape). That
record stands; the clarified wording is the basis for future qualification only.

## 11. Effect on existing P1 evidence

| Artifact | Effect |
| --- | --- |
| `Q1-ORACLE.json` (10/10, 17/17, 15/18) | Unchanged; R13 subcase failures remain recorded against the original wording |
| `Q1-ORACLE-v1-text-literal-run.json` | Unchanged (historical as-written run) |
| `Q2/Q3/Q4` receipts, logs, `oracle-r2.json`, manifest (23/0) | Unchanged; no resealing |
| P1 report | Unchanged historical record; the addendum supersedes its finding text prospectively |
| Frozen corpus/oracle, SQ-04/SQ-05 packages, all sealed evidence | Untouched |

## 12. Remaining S-01 readiness

Evidence already available (from P1, all re-verified in §3): canonical encoding (KATs);
identifier/declaration/shape derivation (17/17); explicit-interface behavior (FC-8 + FC-14
observation); independent reference values (RefEnc, cross-checked); real C# extraction;
ingestion/native publication; durable fresh-process reopen; authority/refusal; managed recovery;
applicable D3/D4/D5 with the recorded exclusions.

**Observation O-1 determination:** a comment-only rebuild changing the composed target while
component digests stay equal is **expected under the accepted target contract** — the composed
target binds the defining compilation including its reference identities (v1 §8; matrix §1 row 2
"references in context"; P0 §10). Component-level invariance and full-target invariance **must not
be conflated**: component digests are the stable comparison surface; composed targets are
build/closure- and path-scoped and self-consistent within a build (all P1 processes agree).

Remaining work classification:

- **Narrow P1 delta proof** — **required** (R13a/b/c re-executed against the clarified wording;
  recommended: the direct `compctx1` modifier-pair sensitivity receipt; new self-excluding delta
  package; the P1 package is never modified or resealed).
- **Production remediation** — none (§5).
- **Additional contract approval** — the addendum (this decision); nothing further required.
- **Independent P2 qualification** — after the delta proof, against v1-as-clarified; **not
  authorized here**.
- **P3 owner acceptance** — separate gate.

**S-01 remains unsatisfied.**

## 13. Formal owner disposition

**`S-01 P1 CONTRACT CLARIFICATION APPROVED WITH QUALIFICATIONS`.**

Approval requirements (§11 of the commission):

1. F-P1-01/F-P1-03 accurately classified — yes (§4, §6: editorial, no semantic effect).
2. F-P1-02 fully analyzed against normative requirements — yes (§5A–E; all authority layers
   mapped in §2; no clause requiring modifier-sensitive component forms exists above the defective
   text).
3. No mandatory implementation contradiction hidden — the implementation matches the primary
   forms exactly; the correction aligns derivative text to reality and removes no binding
   obligation (§5D/§8 C1–C3); nothing is reclassified to enable P2.
4. Substantive-change authorization — not needed: the operative semantics (forms, normalization,
   encoding, produced identities) are unchanged; if a future review were to classify E-3
   differently, this decision records that the owner has exercised the explicit authorization the
   P0 versioning rule requires for any canonical-forms text change, with no version bump because
   no identity consequence exists.
5. Correct versioning and provenance — §9; addendum dated, prospective; historical records
   preserved byte-exact.
6. Exact effect on R13a/b/c documented — §7; delta proof required before they may be treated as
   passing.
7. Remaining evidence and qualification requirements — §12.

**Qualifications:** (a) delta proof required before P2; (b) mode-family component non-injectivity
is a recorded v1 design property (C2); (c) context-vector direct receipt recommended in the delta
proof; (d) P2 must assess against v1-as-clarified, never against the original row-13 wording;
(e) all prior qualifications carried below remain binding.

## 14. Proposed next bounded commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01` — tests/probes/evidence only; no production
change; a new self-excluding delta evidence package (the P1 package is immutable): (a) re-execute
R13a/R13b/R13c against the clarified expectations, with expected doc-ID forms
(`System.Int32` vs `System.Int32@`) and the corrected relations, recording expected/actual digests;
(b) capture the direct receipt that a modifier-only source revision changes the composed target
via `compctx1` while component digests change exactly as §5B records; (c) re-affirm the regression
floor (907/0/0; frozen oracle byte-identical; format exit 0). Only after the delta proof may an
independent **P2** qualification commission (reviewer ≠ producer) be proposed against
v1-as-clarified. **Neither is authorized by this decision.**

## 15. Successor authorization status, preserved qualifications, repository discipline

- **WP-S25-3:** P0 recorded; P1 executed (`90c5fbee…`, `COMPLETE WITH QUALIFICATIONS`); this
  contract-clarification decision recorded; **P1-DELTA: proposed, NOT authorized. P2: NOT
  authorized. P3: NOT authorized. S-01: not satisfied.**
- **Preserved without change:** all accepted S2.3/S2.4 limitations; WP-S25-1 acceptance; SQ-04
  amended historical boundary (`SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`); WP-S25-2
  acceptance; SQ-05 qualified Linux scope (`SATISFIED WITH QUALIFICATIONS`); **F-G2-01 OPEN**
  (repository CI coverage-gate failure; separate owner decision; not waived); F-02 managed-fault
  boundary; D3-full and applicable D4/D5 obligations and their recorded gaps; **D4.5 and full
  S-02 remain Stage 6**; WP-S25-4 planned and unauthorized; Stage-2 acceptance unauthorized;
  no public Find Usages; no cross-target implementation; no schema migration or dual-write; no
  optimization; WP-S24-3 deferred/unauthorized.
- **Repository discipline:** only this decision, the addendum, and the additive work-package-plan
  status update are added, on canonical `main`, single worktree. No production, test, CI,
  contract-file, fixture, oracle/corpus, prior decision, or historical evidence is modified; no
  remote operation. Commit/tree identities and final status are reported in the handoff.
