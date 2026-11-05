# ANDIRIT-CI1A-S2.2-WP5-IMPL-01 — Production `usage4` construction, extraction and emission report

**Commission:** `ANDIRIT-CI1A-S2.2-WP5-IMPL-01` (bounded production implementation).
**Project:** Andirit. **Date:** 2026-11-05. **Branch:** `main`.
**Target disposition:** `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.

This report records the WP5 production `usage4`, bounded extraction, coverage/refusal
and F09 disabled-text scanning only. It does **not** independently qualify WP5, does
**not** grant owner acceptance, and does **not** implement WP6 NEW-04, persistence,
native reopen or any later stage. Producer tests are not independent qualification.

> **Commission note.** The supplied commission text is truncated at its §9. Sections
> 1–8 and the accepted repository contracts were used; where the commission is silent
> the accepted contracts govern. Any divergence is noted in §13.

---

## 1. Baseline and final Git identities

| Role | Commit | Tree |
| --- | --- | --- |
| Authoritative baseline (WP4 owner acceptance) | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP4 implementation | `eac302295b5bbaf8d71181ddcc70a62089a9a28e` | `9a4ccefc182d5b80123def1dce313350c37e23d6` |
| WP4 independent qualification | `6ecf9962216b3c7a3c7330da856383c5c5b0dde0` | `c0ce3df6dafe5383cb12c683597c71728ded35e7` |
| **WP5 implementation (this commission)** | `a5f04fda495a5f54d95fd3f6a313620334a61155` | `c58781497560cae4bd8fbd420d83b0b41350fb58` |

Starting state verified: `main` at `97c88e5` (tree `27cda088…`), clean working tree,
one worktree, no stashes, `origin/main` = `e0d9110`, 15 commits ahead, nothing
pushed, no concurrent writer. `git diff --stat 97c88e5 -- src tests` before
implementation was empty.

---

## 2. Authoritative contracts consulted

- **WP5 scope** — `docs/planning/adr-ci1-01-ci1a-s2-2/scope-and-entry-criteria.md`
  §6.2 (WP5 = "Coverage/refusal emission and unconditional-inactive scanner (F09)
  with no confirmed fact for inactive/external/ambiguous/unresolved/unsupported/
  incomplete"), §6.3 (frozen `usage4` invariants), §6.4 (required tests).
- **WP4 owner decision §9 (binding WP5 entry table)** —
  `docs/decisions/adr-ci1-01-ci1a-s2-2-wp4-owner-01.md` W5-1…W5-10.
- **Stage 2 boundary** — `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md`
  §3.6 (extraction from the same live compilation; supported category `Invocation`),
  §3.7 (`usage4` fields), §3.9 (coverage/refusal vocabulary), §5 (scope-relative
  completeness), §10 (coverage boundary).
- **Stage 2 acceptance matrix** — `stage2-acceptance-matrix.md` §2 (`usage4`/`asmctx3`
  tuple), §4 (extraction: identifier-token span, declaration span, source hash),
  §5 (refusal/coverage including `ConditionalInactive` and `Incomplete`).
- **Coverage and availability** —
  `docs/planning/adr-ci1-01-ci1a-s2-rem-01/coverage-and-availability.md` (three
  dimensions; `Produced`/`Partial`/`NotProduced`; a zero-fact `Produced` is a valid
  complete-empty, a zero-fact `Partial` is not).
- **F09** — `docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md` §F09 ("Require
  actual disabled-text scanning bound to language/preprocessor configuration; contract
  closure is not scanner completion").
- **`usage4` identity** — `docs/planning/adr-ci1-01-ci1a-s2-0/identity-and-provenance-mapping.md`
  §1; the frozen helper `CodeOccurrenceIdentity.ComputeOccurrenceId`.
- **WP4 `asmctx3`** — WP4 report and WP4-Q1 qualification; `VerifiedCSharpUniverse.MintTarget`.
- **NEW-04 boundary** — G-CONFIRM owner §8; register §4; `CodeOccurrenceCoverage.Merge`
  remains deferred to WP6.

---

## 3. WP5 implementation architecture

WP5 consumes the accepted WP4 `asmctx3` authority and the accepted WP3 mapper/resolver.
The trust chain is preserved end to end:

```
live CSharpCompilation (same instance as graph facts)
  → InvocationExpressionSyntax
  → bound IMethodSymbol (semantic model)
  → VerifiedCSharpUniverse.ResolveDefining  (exact live defining instance, complete universe)
  → current verified compctx2 recorded
  → VerifiedCSharpUniverse.MintTarget        (WP4: mandatory recorded compctx2, fail-closed)
  → VerifiedCSharpUniverse.MintUsage         (WP5: only a minted asmctx3 becomes usage4)
  → CodeOccurrenceFact (usage4)              (frozen ComputeOccurrenceId inside Create)
```

New/changed production surface (all `internal`, no public API change):

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedUsageMint.cs` | new: `VerifiedUsageInput`, `VerifiedUsageMint`, `VerifiedOccurrenceExtraction` |
| `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs` | new: `VerifiedOccurrenceExtractionRequest`, `VerifiedCSharpOccurrenceExtractor.Extract`, F09 scanner |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +`VerifiedCSharpUniverse.MintUsage` (additive, +30) |
| `tests/Andirit.CSharp.Tests/S2_2Wp5UsageTests.cs` | new: 14 tests |

No public API, no persistence, no scanner outside WP5, no NEW-04/`Merge` use.

---

## 4. Contract-to-code traceability (WP5-A)

| Contract clause | Implementation | Test |
| --- | --- | --- |
| `usage4` field order/domain/encoding (identity mapping §1) | `CodeOccurrenceFact.Create` → frozen `ComputeOccurrenceId` | `Usage_ValidDirectInvocation_ProducesCanonicalUsage4` vs independent `ExpectedUsage4` |
| Supported category `Invocation`; others coverage (matrix §4.1) | `IsSupportedInvocation` (`Ordinary`/`ReducedExtension`/`ExplicitInterfaceImplementation`) | `Usage_UnsupportedCategories_AreCoverageNotFacts` |
| Identifier-token span + declaration span (matrix §4.3) | `SiteSpan`, `DeclarationSpan` | `Usage_ValidDirectInvocation…` |
| SourceHash = actual indexed bytes (matrix §4.4) | `VerifiedCSharpSource.ContentHash` of the calling unit | `Usage_ValidDirectInvocation…` |
| `asmctx3` from the defining compilation (Option A §5.1) | `ResolveDefining` + `MintTarget` | `Usage_CrossProjectInvocation…` |
| Mandatory recorded `compctx2` (W5-2) | `MintUsage` delegates to `MintTarget` (required parameter) | `Usage_MintUsage_NullRecordedContext…`, `Usage_MintUsage_StaleRecordedContext…` |
| Structural identifier is not authority (W5-1) | `VerifiedUsageInput` has no target-id input; id derived from the mint | `Usage_StructuralIdentifierAloneIsNotAuthority` |
| `compctx2` isolation (mapping §6) | `usage4` uses only `asmctx3` | `Usage_ValidDirectInvocation…` |
| Coverage states (boundary §3.9/§5) | `Extract` counters + `CodeOccurrenceCoverage` | `Usage_IncompleteUniverse…`, `Usage_UnresolvedExternalTarget…`, `Usage_AmbiguousDefining…` |
| `ConditionalInactive` (matrix §5.6; F09) | `CountConditionalInactiveSites` (disabled-text trivia bound to declared configuration) | `Usage_F09_ConditionalInactive_IsNotAConfirmedFact` |
| No `Merge`/NEW-04 (G-CONFIRM §8) | coverage constructed directly | `Usage_Coverage_IsConstructedWithoutMerge` |
| S-01 caller-supplied digests | request `Func`s; not verified | `Usage_ComponentDigests_AreCallerSupplied` |

---

## 5. `usage4` construction semantics (WP5-C)

```
usage4 = SHA256("usage4",
  repository, plugin, "usage4-b2c-exp-r2-20261101", semanticSourceUnit, path,
  sourceHash, startLine, startColumn, endLine, endColumn,
  targetDocumentId, asmctx3, "method-invocation")
```

- 32-bit big-endian length-prefixed UTF-8 parts; lowercase hex (no prefix).
- `asmctx3` is the WP4-minted target of the **defining** compilation.
- `identifierDigest`/`declarationDigest`/`shapeDigest`/`targetDocumentId` are
  caller-supplied (S-01) and are **not** independently verified.
- Construction is the frozen `CodeOccurrenceFact.Create`; the identity cannot be
  bypassed. Determinism follows from deterministic inputs.

---

## 6. Mandatory recorded-`compctx2` enforcement (WP5-B)

`MintUsage` has no target-identifier input; it calls `MintTarget` (WP4), which
requires a supplied non-null recorded `compctx2`, and only on `Minted` builds the
`CodeOccurrenceFact`. A non-minted status yields `Fact = null`. The extractor
resolves the defining compilation and records its **current verified** `compctx2`,
which the mint re-validates. `Usage_MintUsage_NullRecordedContext_ProducesNoFact`
and `Usage_StructuralIdentifierAloneIsNotAuthority` demonstrate that a structural
identifier or a null/incorrect recording produces no trusted `usage4`.

---

## 7. G1–G4 and W5-1…W5-10 compliance

| Obligation | WP5 treatment |
| --- | --- |
| **G1 / W5-3** metadata-vs-instance | The `asmctx3`/`usage4` is built from the resolved **defining** live instance; metadata equivalence is not upgraded (WP4 unchanged). |
| **G2 / W5-5** fail-closed | Every non-`Complete` resolution and every non-`Minted` mint produces coverage, never a fact. |
| **G3 / W5-2** recorded `compctx2` | Required and re-validated through `MintTarget`. |
| **G4 / W5-6** Option A §5.2 | WP5-applicable runtime forms covered below; test 3 `usage4` now exercised at WP5 (see §8). |
| **W5-1** minted-consumption | `VerifiedUsageInput` exposes no target id; authority derives from `MintTarget`. |
| **W5-4** component provenance | S-01 caller-supplied digests retained; no invented verification. |
| **W5-7** emission/extraction | Implemented: invocation extraction, `usage4`, coverage/refusal, F09 scanner. |
| **W5-8** NEW-04 | `Merge` not used; coverage constructed directly. |
| **W5-9** platform | Windows x64 only (see §12). |
| **W5-10** independent qualification | Required; not performed here. |

C1–C4 are untouched. NEW-04 remains deferred to WP6.

---

## 8. Option A §5.2 runtime-test coverage

| §5.2 | WP5 status |
| --- | --- |
| 1 conditional defining API change → target changes | ✅ (WP4-Q1) and preserved through `usage4` |
| 2 transitive-only change → identity stable | ✅ (WP4-Q1) |
| 3 caller unchanged; target follows defining API → `usage4` | ✅ **WP5** `usage4` form exercised (`Usage_ValidDirectInvocation…`, `Usage_CrossProjectInvocation…`); no claim of a full E3 scenario harness |
| 4 restore → original identity | ✅ (WP4-Q1) |
| 5 distinct defining compilations → distinct targets | ✅ (WP4-Q1) |
| 6 equivalent reference artifact | ✅ same-input profile; cross-profile is a G-MINT comparison item |
| 7 re-ingestion determinism | ✅ `Usage_DeterministicAcrossRepeatedExtraction` (in-process) |
| 8 stale publication refused (in-memory) | ✅ `Usage_MintUsage_StaleRecordedContext…`; native reopen = S2.3 |
| 9 deterministic documented mapping | ✅ |
| 10 no minting from unverified/incomplete; isolation | ✅ `Usage_IncompleteUniverse…`, `Usage_UnresolvedExternalTarget…`, `Usage_StructuralIdentifierAloneIsNotAuthority` |

Complete Option A §5.2 acceptance is **not** claimed.

---

## 9. Test commands and actual outcomes

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.

| Suite | Command | Actual |
| --- | --- | --- |
| New WP5 tests | `dotnet test tests/Andirit.CSharp.Tests --filter FullyQualifiedName~S2_2Wp5UsageTests` | **14 / 0 / 0** |
| `Andirit.CSharp.Tests` | `dotnet test tests/Andirit.CSharp.Tests` | **217 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **742 / 0 / 0** (325+217+157+43) |
| WP4-Q1 probes (reference) | `dotnet test docs/reviews/CI1A-S2.2-WP4-Q1-tests` | **13 / 0 / 0** |
| WP3-Q1 probes (reference) | `dotnet test docs/reviews/CI1A-S2.2-WP3-Q1-tests` | **19 / 0 / 0** |
| G-MINT-Q1 probes (reference) | `dotnet test docs/reviews/CI1A-S2.2-G-MINT-Q1-tests` | **12 / 0 / 0** |

Reference totals (C# 203 / solution 728) increased by exactly the 14 new WP5 tests
(217 / 742). Expected `usage4` values are reconstructed independently
(`ExpectedUsage4`/`ExpectedAsmctx3`), never by the extractor under test.

---

## 10. Frozen oracle integrity

- Frozen expected `oracle-expected-r2.json`:
  `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` (unchanged).
- Oracle replay: **185 / 185**, exit 0; output SHA-256
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged).
- WP5 does not consume or alter the oracle. Evidence captured in
  `docs/reviews/CI1A-S2.2-WP5-IMPL-01-evidence/`.

---

## 11. Remaining obligations

- **Persistence/publication/native reopen (S2.3):** not implemented; `usage4` facts
  are produced in memory only.
- **NEW-04 (WP6):** coverage-merge correction remains deferred; `Merge` unused.
- **Component-digest canonicalization (S-01):** still caller-supplied; WP5 does not
  canonicalize them.
- **Option A §5.2 test 6 cross-profile** equality remains a G-MINT comparison item.
- **Full F09 scanner precision** and the **E3/durable-association** runtime harnesses
  are WP5-Q1/independent subjects.

---

## 12. Platform limitations

- Executed on Windows x64 (`net10.0`) only; no cross-platform qualification.
- Separate-process determinism for the extractor is not independently run here
  (the identity inputs are deterministic; WP4-Q1 already demonstrated separate-process
  mint determinism). Independent WP5-Q1 should re-confirm.
- S-01 component-digest provenance is not verified.

---

## 13. Known restrictions and unresolved issues

- **F09 scanner is a bounded heuristic.** It detects disabled-text trivia (bound to
  the declared preprocessor configuration) and counts invocation-like sites with a
  documented regex excluding control keywords. It never emits a fact. Exact scanner
  precision (e.g., distinguishing method-group references from invocations inside
  disabled text) is explicitly a WP5-Q1 subject; the register's F09 obligation warns
  that "contract closure is not scanner completion".
- **Extraction scope.** Only `InvocationExpressionSyntax` is scanned; `ObjectCreation`,
  member access (property/field/event), method groups, delegates and local functions
  are classified `Unsupported`. This matches matrix §4.1/§5.5 but is bounded.
- **`targetDocumentId`** uses `GetDocumentationCommentId() ?? Name` as a bounded
  canonicalization; the corpus supplies fixture-declared forms. No stronger rule is
  invented.
- **Commission truncation.** The commission text ends mid-§9; §9 (identity/context
  continuity) and later sections were reconstructed from the accepted WP4 owner §9
  table (W5-1…W5-10), Option A §5.1/§5.2, and the stage-2 contracts.

---

## 14. Recommendation for independent qualification

**`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** An independent WP5-Q1 reviewer
should challenge: (1) that no path can produce a trusted `usage4` without a WP4 mint;
(2) null/stale/fabricated recorded-context refusal at the emission boundary; (3) the
frozen `usage4` tuple and `compctx2` isolation with independently derived values;
(4) coverage/refusal classification and the F09 disabled-text scanner, including the
heuristic's false positives/negatives; (5) determinism, including separate processes;
(6) the WP5/WP6 boundary (no `Merge` reliance); and (7) S-01 retained limitations.

### Changed-file inventory (production + tests)

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedUsageMint.cs` | new |
| `src/Andirit.CSharp/VerifiedCSharpOccurrenceExtractor.cs` | new |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +30 (additive) |
| `tests/Andirit.CSharp.Tests/S2_2Wp5UsageTests.cs` | new, 14 tests |

No production file outside `src/Andirit.CSharp` was modified; no accepted decision,
reviewer test, frozen oracle or report was altered.

### Git identities

- Starting commit/tree: `97c88e5` / `27cda088…`.
- Production implementation commit/tree: `a5f04fda495a5f54d95fd3f6a313620334a61155` /
  `c58781497560cae4bd8fbd420d83b0b41350fb58`.
- Evidence commit (this report + evidence): the direct child of `a5f04fd`; exact hash
  and final HEAD are recorded in the commission handoff.

---

**Final disposition:** **`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** WP5
`usage4` construction, bounded extraction, coverage/refusal emission and the F09
disabled-text scanner exist behind the accepted WP4 authority boundary. It does not
authorize independent acceptance, WP6, persistence or publication. Stop after evidence
preservation.
