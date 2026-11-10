# S-01 canonical-form contract v1 — clarification addendum 01 (dated errata and normative clarification)

**Authority:** [ADR-CI1-01-CI1A-S2.5-WP-S25-3-P1-CONTRACT-CLARIFY-OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md)
(**`S-01 P1 CONTRACT CLARIFICATION APPROVED WITH QUALIFICATIONS`**).
**Recorded:** 2026-11-11. **Status:** ADOPTED (prospective). **Effective version:**
`canonical-forms v1 (clarified by this addendum)`.

> This addendum clarifies the adopted [S-01 canonical-form contract v1](s01-canonical-form-contract.md)
> in response to findings F-P1-01, F-P1-02, and F-P1-03 of the P1 integrated proof. It applies
> **prospectively**; the original contract document, the P0 adoption decision, and all P1 records
> remain **byte-exact historical records** and are not rewritten. Identifiers E-1/E-4 are
> **editorial errata** (no semantic effect); E-2/E-3 are a **normative clarification** of adopted
> expectation text — the primary canonical forms (identifier/declaration/shape definitions),
> normalization, byte encoding, and all produced identities are **unchanged**. No new
> canonical-forms version, no semantic-digest version change, no production remediation, and no
> re-ingestion are required. The failed R13a/R13b/R13c subcases of P1 remain recorded failures
> against the original wording and are re-established only by the proposed P1 delta proof.

## 1. E-1 (F-P1-01) — display-format name (editorial erratum)

**Original (v1 §5):** `TypeIdentity(t) = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedGlobalNamespaceFormat)`

**Corrected:** `TypeIdentity(t) = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`

**Classification:** `EDITORIAL CONTRACT ERROR`. **Reason:** the original name does not exist in the
accepted Roslyn generation; `FullyQualifiedFormat` is the documented format and is exactly what the
production producer uses (`CSharpCodeGraphPlugin.Symbols.cs` — `TypeIdentity`). Observed outputs
(`int`/`string` special types; `global::` qualification for non-special types) were verified by P1
(forms 17/17; the reserved as-written run `Q1-ORACLE-v1-text-literal-run.json` documents the
defect's practical consequence). **Effect:** none on semantics, expectations, or identities.

## 2. E-2 (F-P1-02) — shape commentary sentence (normative clarification)

**Original (v1 §5):** "Parameter order, count, and arity participate. `ref`/`out`/`in` modifiers
appear in the display strings and therefore in the shape."

**Corrected (replaces the quoted sentences; the remainder of the bullet is unchanged):**
"Parameter order, count, and arity participate. Parameter mode (`ref`/`out`/`in`) does not
participate in the shape form: `TypeIdentity` displays the parameter type only, and Roslyn type
displays never include the parameter's reference mode. Where documentation IDs are used, Roslyn
renders `ref`/`out`/`in` uniformly with the documentation-ID `@` suffix (value parameters carry no
suffix); mode-vs-value distinctness is carried by the declaration form, and mode-family
distinctness across source revisions by the defining-compilation context component of `asmctx3`
(`compctx1`) — never by the shape form."

**Classification:** `NORMATIVE CLARIFICATION REQUIRED` (approved). **Reason (evidence):** P1
R13a/R13b/R13c — the shape form is mode-blind (`method|0|int` for value/`ref`/`out`/`in`); the
doc-ID renders the family uniformly as `@` (`System.Int32` vs `System.Int32@` for value vs mode);
such forms are what the primary §3–§5 rules compute. **Effect:** the corrected text describes the
unchanged operative semantics; no produced identity changes.

## 3. E-3 (F-P1-02) — §7 row 13 (normative clarification)

**Original (v1 §7):**

| 13 | `ref` vs `out` (vs value) | = | ≠ | ≠ | declaration/shape ≠ | doc-ID `@`/`%` modifiers; display modifiers |

**Corrected (replaces row 13; §7 remains a 15-row matrix):**

| 13 | Parameter mode — `ref`/`out`/`in` vs value; and within the mode family | = | ≠ (mode vs value) · = (within `ref`/`out`/`in`) | = | mode vs value: declaration digest ≠ and composed target ≠ (such overloads are declarable simultaneously, so the declaration form must distinguish them). Within `ref`/`out`/`in`: all component digests =; no within-compilation collision exists (modifier-only overloads are not declarable — CS0663 family); across source revisions the composed target is distinct via the defining-compilation context (`compctx1`, which binds the changed source bytes) | doc-ID `@` suffix (uniform across `ref`/`out`/`in`); the mode is not part of the type display. Recorded v1 design property: mode-family distinctness is carried by non-coexistence + defining-context binding |

**Classification:** `NORMATIVE CLARIFICATION REQUIRED` (approved). **Reason:** the original row's
declaration/shape-expectations are not derivable from the adopted forms (§2–§5) and are not
required by any higher-level authority (identity contract, S-01 disposition, G-ACCEPT, entry
decisions, Stage-2 boundary/acceptance matrix — the matrix's binding requirements are "changed
source bytes → new `compctx1`/`asmctx3`/`usage4`" and "different defining context/component →
different target"). The corrected row is the exact consequence of the unchanged forms. The
original basis cell's "`@`/`%` modifiers" does not correspond to the observed Roslyn doc-ID
rendering (uniform `@`; no `%` mode token). **Effect:** future qualification assesses row 13
against this corrected text; the P1 R13 failures against the original text stand as recorded.

## 4. E-4 (F-P1-03) — documentation-ID example (editorial erratum)

**Original (v1 §4):** "the Roslyn documentation-comment ID, e.g. `M:Namespace.OwnerType.MethodName(global::System.String)`"

**Corrected:** "the Roslyn documentation-comment ID, e.g. `M:Namespace.OwnerType.MethodName(System.String)`"

**Classification:** `EDITORIAL CONTRACT ERROR`. **Reason:** documentation-comment IDs use
doc-ID notation only; `global::` is a symbol-display device and never appears in doc IDs (P1
verified actual doc-ID forms at the digest level, e.g.
`M:Corpus.Core.Methods.Add(System.Int32,System.Int32)`). The §4 **fallback** example
(`Method:global::Ns.OwnerType.Method(...)`) is display-based and remains correct — unchanged.
**Effect:** none on semantics, expectations, or identities. (The same illustrative text is quoted
in the P0 decision; that document remains a historical record.)

## 5. Effect on evidence and remaining obligations

- P1 evidence (`Q1-ORACLE.json` 10/10 · 17/17 · 15/18 with R13a/b/c failing the original wording;
  `Q1-ORACLE-v1-text-literal-run.json`; `Q2`–`Q4`; manifest 23/0) is **unchanged and not resealed**.
- **R13a/R13b/R13c are not converted into historical passes.** The proposed
  `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01` must re-execute them against this corrected text
  (including the recommended direct `compctx1` sensitivity receipt for a modifier-only pair)
  before independent P2 qualification, which must assess against
  `canonical-forms v1 (clarified by this addendum)`.
- S-01 remains unsatisfied; P2 and P3 remain separate, unauthorized gates.

## 6. Provenance and versioning

- Effective: 2026-11-11, prospectively, by owner decision
  `adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`.
- No canonical-forms version bump: no form, normalization, encoding, or produced-identity change.
- Occurrence semantics remain **version 4** (`usage4`) with the frozen `asmctx3`/`compctx1`
  encoding; no migration, dual-write, or re-ingestion is triggered by this addendum.
- Any **future** change to the forms still requires a new canonical-forms version, an explicit
  owner decision, and re-ingestion per D5 (v1 §9, unchanged).
- Reading instruction: wherever the original contract or the P0 decision restates the passages
  corrected above, this addendum governs prospectively; the originals remain historical records.
