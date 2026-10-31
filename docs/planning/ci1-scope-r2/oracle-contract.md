# Oracle contract — R2 semantic validation rules and limitations

This is the authoritative description of what
[`analysis/oracle/Program.cs`](analysis/oracle/Program.cs) validates, how it
derives evidence, and where its authority ends. It is planning-only validation.

## 1. Independence boundary

The oracle is a **pure Roslyn compiler oracle**. It:

1. does **not** reference the `Andirit` plugin or any production project;
2. does **not** read the output of any CI-1A implementation (none exists);
3. derives expected semantics from **fixture source + the recorded compilation/
   dependency context**, never by comparing one editable manifest field to
   another (for example, `expectedOutcome == storedOutcome` is never used).

The four distinct things are kept separate:

| # | Thing | Role |
| --- | --- | --- |
| 1 | Frozen expected manifest `docs/planning/ci1-scope-01/fixtures/expected-sites.json` | Subject under test; 32 rows, unmodified |
| 2 | Source/compiler-derived semantic evidence | Produced by this oracle from raw bytes + Roslyn |
| 3 | Verification of declared outcomes against that evidence | This oracle's verdict |
| 4 | Future candidate-result comparison | **Not qualified; no candidate exists** |

Step 4 is explicitly unqualified. Nothing here shows that any candidate passes.

## 2. Recognised classification vocabulary

The oracle derives one of these classes for every occurrence from compiler
evidence:

| Class | Meaning |
| --- | --- |
| `confirmed` | A uniquely resolved **eligible indexed local definition** with verified semantic identity. |
| `external` | A resolved target **outside** the eligible indexed definitions in the recorded context. |
| `ambiguous` | No unique eligible target: two or more candidate symbols, no bound symbol. |
| `unresolved` | Binding established no target and no candidates. |
| `unsupported` | The occurrence/category falls outside the accepted analysis boundary (constructors, method groups, `typeof`, `nameof`). |
| `notEvaluated` | The occurrence is inactive in the recorded configuration (C20 default). |

The declared-outcome vocabulary is **exhaustive and exact** (no prefix matching).
Every value below is recognised; any other string is rejected as
`OUTCOME_UNRECOGNIZED`:

| Declared string | Class | Subtype |
| --- | --- | --- |
| `confirmed` | `confirmed` | — |
| `confirmed-interface-static-target` | `confirmed` | — |
| `confirmed-virtual-static-target` | `confirmed` | — |
| `confirmed-base-static-target` | `confirmed` | — |
| `confirmed-cross-project` | `confirmed` | — |
| `confirmed-after-replacement` | `confirmed` | — |
| `inactive-default; confirmed only with CI1_EXTRA` | `confirmed` + default-inactive required | — |
| `external-no-confirmed-local-target` | `external` | — |
| `external-delegate-invoke-not-Target.Work` | `external` | — |
| `ambiguous-no-confirmation` | `ambiguous` | — |
| `unresolved-no-confirmation` | `unresolved` | — |
| `deferred-constructor` | `unsupported` | `constructor` |
| `deferred-method-group` | `unsupported` | `method-group` |
| `deferred-type-reference` | `unsupported` | `type-reference` |
| `deferred-nameof-reference` | `unsupported` | `nameof-reference` |

**Vocabulary note.** These exact strings are the ones already adopted by the
CI-1 planning contracts and the frozen manifest; no new declared vocabulary is
introduced. The class layer (`confirmed`/`external`/`ambiguous`/`unresolved`/
`unsupported`) matches the disjoint-outcome vocabulary already required by the
S04 amendment ("confirmed, unresolved, ambiguous, external, unsupported
owner/category, candidate-only, intrinsic/unsupported invocation"). The only
addition is the explicit R2 rank `unsupported` for the deferred categories that
the existing strings already describe.

## 3. Derivation rules (source/compiler evidence only)

For each manifest row:

1. **Coordinates.** Reconstruct the exact UTF-16 start/end span from the raw file
   text and require the substring to equal the declared lexeme. A coordinate
   failure is reported alone (`COORDINATE_LEXEME_MISMATCH`) and short-circuits;
   it never cascades into semantic noise.
2. **Uniqueness.** Reject a repeated `(fixture path, span)` (`DUPLICATE_OCCURRENCE`).
3. **Context.** Select the compilation from the recorded mapping
   (`Repeated.cs`→`repeated`, `AfterDelete`→`deleted`, `AfterShift`→`shifted`,
   `CrossProject`→`app`, `C20`→`enabled` with `CI1_EXTRA`, else `acceptance`).
   Compilation name, assembly identity, parse options, preprocessor symbols,
   source files and reference counts are recorded.
4. **Occurrence category.** From the syntax node kind: constructor initialiser/
   object creation → `constructor`; `typeof` → `type-reference`; `nameof` →
   `nameof-reference`; a method-group reference that is not the callee of an
   invocation → `method-group`; otherwise ordinary invocation.
5. **Binding.** `GetSymbolInfo` yields the target symbol and candidate symbols.
6. **Derived class.**
   - unsupported categories → `unsupported` with subtype;
   - ordinary invocation: bound `IMethodSymbol` → `confirmed` if eligible local,
     else `external`; no symbol with candidates → `ambiguous`; no symbol and no
     candidates → `unresolved`.
7. **Eligibility (the local/confirmed test).** A bound target is an **eligible
   indexed local definition** only when **both**:
   - its containing assembly identity equals the identity of a compilation built
     from source in the recorded context, **and**
   - its original definition has a source declaration location inside a fixture
     tree.
   A documentation ID or display name is never sufficient. This is what keeps
   the F01 `System.Console.WriteLine` collision external.
8. **Target identity.** The bound documentation ID is compared to the frozen
   target table entry for the declared signature; for eligible-local entries the
   bound target must also be eligible local; constructor/type-token entries must
   bind the corresponding symbol kind.
9. **Outcome agreement.** The declared string's class must equal the derived
   class; for `unsupported` the subtype must also match. The C20 row additionally
   requires the default context to be inactive.

## 4. Reason codes

| Code | Meaning |
| --- | --- |
| `COORDINATE_LEXEME_MISMATCH` | Span/lexeme inconsistent, invalid, or reversed. |
| `DUPLICATE_OCCURRENCE` | Same fixture path and span audited twice. |
| `SYNTAX_MISSING` | No syntax node matches the span. |
| `SYNTAX_CATEGORY_UNKNOWN` | Node kind outside the recognised category set. |
| `TARGET_UNRECOGNIZED` | Declared signature absent from the frozen target table. |
| `TARGET_IDENTITY_MISMATCH` | Bound documentation ID differs from the declared expectation. |
| `TARGET_ELIGIBILITY_MISMATCH` | Declared eligible-local but bound target is not eligible local. |
| `TARGET_KIND_MISMATCH` | Bound symbol kind differs (constructor/type token). |
| `OUTCOME_UNRECOGNIZED` | Declared outcome outside the exact vocabulary. |
| `OUTCOME_CLASS_MISMATCH` | Derived class differs from the declared class. |
| `OUTCOME_SUBTYPE_MISMATCH` | Unsupported subtype differs from declared subtype. |
| `DEFAULT_CONTEXT_ACTIVE` | A context-dependent site is active in the default context. |

## 5. Recorded provenance

`evidence/after/after-run.json` records, for independent re-review:

- the Roslyn assembly versions and language version;
- every fixture source input with raw **SHA-256** and byte count;
- every compilation context (assembly identity, parse options, defines, source
  files, reference counts);
- the frozen target table and the declared-outcome vocabulary;
- per-site evidence: category, bound documentation ID, bound containing assembly,
  bound source declarations, eligibility, candidates, derived/declared classes,
  and error codes;
- the adversarial collision fixture hashes and result.

## 6. Limitations

The following are **not** established and must not be read into a passing run:

- No candidate `FindUsagesV1` output is compared (step 4 unqualified).
- The oracle is a compiler **mechanism** oracle for the manifest, not a
  production resolver; it does not exercise linked files, producer-version
  changes or legacy aggregate behaviour.
- It uses the same Roslyn compiler family as the product; it is not a second,
  independent compiler.
- It does not qualify runtime dispatch, method-group invocation, constructor
  discovery, local-function targets or indirect impact.
- It does not fix P-DEF-01 or P-DEF-02; the F01 collision remains a production
  defect outside this task.
- Independent qualification of this oracle is the responsibility of Q3, not of
  R2. R2 claims only that the four Q2 failures are now rejected for the intended
  semantic reasons and that positive controls pass.
# Integration amendment — oracle contract (CI-1-SCOPE-R3)

> **APPEND-ONLY AMENDMENT BLOCK** — proposed by CI-1-SCOPE-R3 for
> `docs/planning/ci1-scope-r2/oracle-contract.md`. Append verbatim after the
> existing content. Historical R2 text above remains unchanged and remains the
> record of what R2 claimed.

---

## R3 amendment (CI-1-SCOPE-R3) — typed semantic assertion model

Status: proposed by R3, pending independent review (Q4). The corrected oracle
is `analysis/oracle-r3/` (assertion-model version
`ci1-scope-r3/semantic-assertions/1`). It supersedes `analysis/oracle/` as the
acceptance oracle for the frozen CI-1A corpus.

### A. What changed and why

Q3 demonstrated (Q3-F01, Q3-F02) that class-level validation plus
source-built-set membership accepts recognized-but-incorrect labels and cannot
establish exact target identity. R3 replaces the classification model with a
typed assertion model: each declared label claims specific facts (binding
outcome, unsupported subtype, dispatch characteristic, cross-project,
after-replacement, conditional activity, lifecycle), each fact is derived
independently from the fixture source and compilation context, and every claim
is compared. A label alone can never satisfy a check.

### B. Re-scoped rules

1. **Exact target identity (replaces R2 §3 rule 7's set-membership test).**
   Each target-table entry names an expected declaring project
   (`audited` / `lib` / `metadata` / `none`). The expected definition is
   resolved inside that compilation by documentation ID and must be unique;
   the bound definition — normalized through `ReducedFrom`/`OriginalDefinition`
   — must equal it in defining compilation, defining assembly identity,
   documentation ID and declaration location. New codes:
   `TARGET_DEFINING_PROJECT_MISMATCH`, `TARGET_IDENTITY_UNVERIFIED`
   (bounded explicit result; never guessed). Metadata targets additionally
   require that no source-built compilation declares the documentation ID.
2. **Outcome provenance (new).** Dispatch characteristic (InterfaceMember /
   VirtualMember / BaseReceiver / DelegateInvoke / Plain — mutually exclusive,
   documented precedence), cross-project (defining compilation ≠ audited ∧
   recorded project-reference edge) and after-replacement (replacement-snapshot
   context) are derived per row and compared to the label's claims
   (`OUTCOME_PROVENANCE_MISMATCH`).
3. **Conditional activity (new, label-independent).** Every row is re-parsed
   with the empty define set and with `CI1_EXTRA`; conditional-only sites must
   match a label that asserts conditionality and vice versa
   (`CONDITIONAL_ACTIVITY_MISMATCH`; `DEFAULT_CONTEXT_ACTIVE` retained).
4. **Corpus completeness (new, closes the R2 §6 out-of-scope item).** A frozen
   32-key occurrence universe is enforced:
   `CORPUS_ROW_MISSING` / `CORPUS_ROW_UNEXPECTED` / `CORPUS_CONTRADICTION`;
   duplicates remain `DUPLICATE_OCCURRENCE`. Row count alone is not accepted as
   completeness.
5. **Input robustness (new, closes the R2 crash boundary).** Malformed
   structure yields `MANIFEST_STRUCTURE_INVALID` (per row or global); the run
   fails with structured diagnostics and a non-zero exit; unreadable input is
   never treated as an empty success.
6. **Owner identity (new).** The lexical owner (nearest enclosing method-like
   declaration; lexical containment, not runtime attribution) is compared to a
   frozen per-occurrence expectation table (`OWNER_MISMATCH`).
7. **RepositoryPath / Basis (new).** `RepositoryPath` is validated against the
   authorized fixture→repository mapping (`REPOSITORY_PATH_MISMATCH`). `Basis`
   must be one of the two recognized provenance statements
   (`BASIS_UNRECOGNIZED`); its claims are classified — the coordinate claim is
   corroborated by the span check, the "hand-selected" and "no extractor
   output" claims are human-reviewed and recorded as
   `BASIS_NOT_MECHANICALLY_QUALIFIED`.
8. **Case label (closes Q3-F07).** `Case` is part of the frozen occurrence
   identity key.

### C. Explicit non-qualifications (unchanged in spirit, now structured)

- The after-replacement **lifecycle** assertion (publication replacement,
  stale-fact retirement, lifecycle correctness) is not mechanically
  qualifiable from single-compilation evidence. Rows asserting it are accepted
  only on their confirmable facets and carry an explicit
  `LIFECYCLE_NOT_QUALIFIED` qualification. The original expectations are
  preserved unchanged.
- Candidate-result comparison remains unqualified; no CI-1A implementation
  exists.
- The oracle is a mechanism oracle on the frozen corpus; it does not qualify
  runtime dispatch, constructor discovery, method-group invocation,
  local-function targets, indirect impact, P-DEF-01 or P-DEF-02.

### D. Acceptance gate

The R3 oracle embeds its own gate: baseline 32/0 with full typed agreement,
all Q2-named, R2-required, Q3-adversarial and R3-regression mutations rejected
for their intended reasons, both positive controls accepted, the A/B identity
and collision requirements satisfied, and zero crashes — `Pass=true`, exit 0.
Any regression of these properties fails the run.
