# ANDIRIT-CI1A-S2.2-WP3-IMPL-01 — Production mapper/resolver implementation report

**Commission:** `ANDIRIT-CI1A-S2.2-WP3-IMPL-01` (bounded production implementation).
**Project:** Andirit. **Date:** 2026-11-04. **Branch:** `main`.
**Target disposition:** `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.

This report records the WP3 production mapper/resolver and pre-mint validation
only. It does **not** independently qualify WP3, pass `G-MINT`, or authorize WP4
or later. Accepted semantic authority is not claimed as implemented downstream
capability: no target or occurrence is minted here.

---

## 1. Baseline

| Item | Value |
| --- | --- |
| Baseline commit (WP3 start) | `812584dc56e11983c3ea7de8977fd1cd74ff0297` |
| Baseline tree | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| Accepted foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` |
| G-CONFIRM-Q1 | `391abbee8c26d957a5776bd893403dc2916ecfef` |
| Accepted owner decision | `docs/decisions/adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md` (`ACCEPTED WITH EXPLICIT QUALIFICATIONS`; `G-CONFIRM PASSED`) |
| Implementation commit | `ff58a9290a7583d4493d177e5de5aa6315904a85` |
| Starting state | clean working tree; `main` 5 ahead of `origin/main` (`origin/main` = `e0d9110`); one worktree; no stashes |

The accepted decision was present and verified before any change. No concurrent
writer moved `HEAD` during implementation.

## 2. Authoritative documents consulted

- `docs/planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md` (§1 invariants, §2
  R2-faithful `compctx1` derivation and comparison profiles, §3 extraction-time
  association invariant, §5–§6 positive/negative/collision cases, §7 probes).
- `docs/decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md` (Option A §5
  decision, §5.1 mapping contract, §5.2 required tests 1–10).
- `docs/planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md` (G-MINT reviewer scope and
  pre-WP5 falsification sequencing).
- `docs/planning/adr-ci1-01-ci1a-s2-0/identity-and-provenance-mapping.md` (frozen
  tuples, fixture profiles, Option A cases A01–A10).
- `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs`
  (frozen R2 `Describe`, `CompilationReferenceIdentity`, `OptionsDigest`).
- `docs/reviews/CI1A-S2.2-G-CONFIRM-Q1/*` (14 nine-part rows, C1–C4).
- `docs/decisions/adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md` §8 (next-gate
  boundaries; NEW-04 at WP6).

Existing production seams read (not modified beyond WP3 additions):
`VerifiedCSharpCompilation`, `VerifiedCSharpAnalysis`, `CompilationProvenanceDescriptor`,
`CodeOccurrenceIdentity`.

**No authoritative document was insufficient for a required WP3 behavior**, so no
portion of implementation was stopped for a missing decision.

## 3. Contract-to-implementation traceability

| Contract clause | Implementation | Test |
| --- | --- | --- |
| Mapping §2 `compctx1 = "compctx1:sha256:" + SHA256("compctx1-context", …)` over the verified fields | `VerifiedCSharpCompilation.MapR2FaithfulCompctx1` | `Mapper_StandaloneEqualsLegacyTwinAndIndependentR2Tuple`, `Mapper_DependencyBearingReplacesContextItemWithR2LegacyItem` |
| Mapping §2 substitution of `compilation-context\|<dep compctx2>` with the frozen R2 item `compilation\|<assembly>\|<metadata>\|<trees>` | `MapReferenceForR2`, `R2LegacyProjectReference` | `Mapper_DependencyBearingReplacesContextItemWithR2LegacyItem`, `Mapper_TransitiveLegacyItemEqualsFrozenR2Formula` |
| Mapping §2 actual PE identities preserved; fixture labels never substituted (C2) | `MapReferenceForR2` leaves `file\|` items verbatim | `Mapper_PreservesActualPeIdentityAndDoesNotSubstituteFixtureLabels` |
| Mapping §1.5 no conflation; `compctx2` never enters `compctx1` | `MapR2FaithfulCompctx1` version/domain swap only | `Mapper_PreservesCompctx2Isolation` |
| Mapping §3 association invariant (live instance, universe completeness, current `compctx2`, external refusal) | `VerifiedCSharpUniverse.ResolveDefining`, `BindsDefining` | resolver tests below |
| Option A §5.1(6) no identity churn on dependency-context-only change | composition of the above | `Mapper_TransitiveOnlyDependencyContextChangeDoesNotChurnIdentity` |
| Option A §5.2 test 9/10 | mapper + resolver | `Mapper_IsDeterministicAcrossRepeatedAndEquivalentInstances`, resolver negatives |

## 4. `compctx1` derivation coverage (WP3-B)

- Standalone compilation: mapped `compctx1` equals `LegacyDescriptor` and the
  independently computed R2 tuple.
- Dependency-bearing compilation (previously `LegacyDescriptor == null`): mapped
  `compctx1` replaces every `compilation-context|…` item with the R2 legacy
  project-reference item reconstructed from the verified live dependency; the
  result equals the independently computed R2 tuple.
- The legacy item is the frozen formula `compilation|<assemblyName>|<metadataIdentity>|<trees>`,
  `trees` = ordinal-by-original-`FilePath` join of `normalizedPath + "=" + sha256hex(utf8(text))`.
- Actual verified PE (`file|…`) identities are preserved; references are
  re-sorted ordinal, matching R2; no dedup is introduced.
- Determinism: repeated calls and equivalent separate instances produce identical
  digests.

## 5. `compctx2` consumption coverage (WP3-B / WP3-D)

- `compctx2` remains the sole dependency-sensitive validity signal and the strong
  project-reference claim; it is never placed into `compctx1`, `asmctx3` or
  `usage4`.
- The resolver accepts a recorded defining context only when it equals the current
  verified `compctx2`; `asmctx3` equality, descriptor shape, or a caller boolean
  never establish authority.
- Binding is proven by the exact live instance: the direct defining assembly must
  be reference-identical, and cross-project binding requires a live
  `CompilationReference` to the exact universe compilation plus a matching
  verified metadata identity.

## 6. C1–C4 compliance

- **C1 (bounded Q-F3):** untouched. WP3 consumes already-verified metadata; it adds
  no validity guarantee and does not broaden the bounded surface.
- **C2 (declared comparison profiles):** the production mapper preserves actual
  captured PE identities and never rewrites to frozen fixture labels; no relaxed
  matching, implicit equivalence, or undocumented normalization is introduced.
- **C3 (ninth field):** untouched; the accepted terminology `closure authority`
  is preserved.
- **C4 (authority vs implementation):** WP3 implements the accepted authority and
  claims no runtime capability beyond the mapper/resolver; no minting, no
  `G-MINT`.

All Q2 foundation limitations remain in force.

## 7. Test matrix and actual results

Environment: Windows x64; .NET SDK `10.0.401`; runtime `10.0.12`; xUnit `2.9.3`.

| Suite | Command | Result |
| --- | --- | --- |
| New WP3 tests | `dotnet test tests/Andirit.CSharp.Tests --filter FullyQualifiedName~S2_2Wp3MapperResolverTests` | **16 passed / 0 failed / 0 skipped** |
| WP3 tests (repeat, no build) | same filter, second run | **16 / 0 / 0** (deterministic) |
| C# regression | `dotnet test tests/Andirit.CSharp.Tests` | **183 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **708 / 0 / 0** (`Andirit.Tests` 325, `Andirit.TreeSitter.Tests` 157, `Andirit.CSharp.Tests` 183, `Andirit.LatticeDb.Tests` 43) |
| Frozen R2 oracle regression | `dotnet run … SemanticCorpusOracle` | `185/185`, output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged) |

WP3 test classes: mapper (9) and pre-mint resolver (7). Expected values are
reconstructed independently from the accepted R2 formulas and known inputs; they
never read the implementation under test.

## 8. Option A §5.2 pre-mint evidence (in-memory portion)

WP3 implements and exercises the in-memory, non-minting portions that can be
validated without WP4 runtime output. `asmctx3`/`usage4` construction and
extraction-time emission remain WP4/WP5; the WP3 checks operate on the mapped
`compctx1` and the association resolver.

| §5.2 test | WP3 evidence | Status |
| --- | --- | --- |
| 1 conditional defining API change changes the defining context | `Mapper_DefiningSourceChangeChangesMappedIdentity` (+ existing source/parse/options change tests) | Covered at `compctx1`/association; `asmctx3` minting is WP4 |
| 2 transitive-only change: mapped identity stable, `compctx2` changes | `Mapper_TransitiveOnlyDependencyContextChangeDoesNotChurnIdentity` | Covered |
| 3 caller unchanged; target follows defining API | `Mapper_TransitiveDependencySourceChangePropagatesToCaller` | Covered at `compctx1`; `usage4` minting is WP5 |
| 4 restore inputs → original identity | `Mapper_IsDeterministicAcrossRepeatedAndEquivalentInstances` | Covered |
| 5 distinct defining compilations → distinct targets | `Mapper_DependencyBearingReplacesContextItemWithR2LegacyItem`, existing unit/source change tests | Covered at `compctx1` |
| 6 equivalent reference artifact → equality per contract | `Mapper_PreservesActualPeIdentityAndDoesNotSubstituteFixtureLabels` | Partial: preserves accepted identity; cross-profile equality is a G-MINT comparison item |
| 7 re-ingestion determinism | `Mapper_IsDeterministicAcrossRepeatedAndEquivalentInstances` | Covered |
| 8 stale publication refused (in-memory) | `Resolver_RefusesStaleOrFabricatedRecordedContext`, `Resolver_RefusesSubstitutedLiveInstanceWithSameDigest` | Covered (in-memory); native reopen deferred to S2.3 |
| 9 deterministic, documented mapping for dependency-bearing compilations | mapper tests + this report | Covered |
| 10 no minting from unverified/incomplete; `compctx2` cannot enter `usage4`/`asmctx3` | `Mapper_PreservesCompctx2Isolation`, resolver negatives | Covered |

Resolver negatives also cover the g-mint required list: **incomplete** universe,
**ambiguous** multi-live binding, **missing** defining compilation, **out-of-scope
(external)** defining unit (asserted at scope-membership construction),
**stale/fabricated** recorded context, and **substituted** live instance with an
identical digest.

## 9. NEW-04 disposition

**Deferred under an explicitly authorized boundary (WP6).** NEW-04 concerns
coverage-merge authority (`CodeOccurrenceCoverage.Merge` scope/universe adoption
and `verified <= declared`), which the accepted record places at **WP6**, before
any coverage reliance, and which is explicitly outside the WP3 mapper/resolver.
WP3 neither reinterprets NEW-04 nor closes it because tests pass. The present
`Merge` implementation remains the pre-correction behavior and must not be relied
upon; an independent falsification is a WP6/G-MINT item.

## 10. Known limitations and deferred requirements

- **No minting.** `asmctx3`/`usage4` construction, extraction emission and the
  WP4 runtime integration are not implemented here.
- **Caller-supplied component digests.** `identifierDigest`/`declarationDigest`/
  `shapeDigest` remain the accepted S2.2/S-01 obligation and are not canonicalized
  by WP3.
- **Resolver binding is exact-instance.** Ambiguity is detected when the caller
  holds multiple live `CompilationReference`s to distinct verified compilations
  presenting the same defining assembly.
- **Native persistence/reopen** (durable `compctx2` association after reopen) is
  the first separately authorized **S2.3** gate; not required for `G-MINT`.
- **Platform.** Executed on Windows only (`net10.0`); no cross-platform run was
  performed and none is inferred from the Windows result.
- **No production extraction path** is wired to the resolver yet (WP4).

## 11. Changed-file inventory

Implementation commit `ff58a92` (4 files, +649):

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` | +`MapR2FaithfulCompctx1`, `MapReferenceForR2`, `R2LegacyProjectReference`; `using System.Text` |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +`VerifiedCSharpUniverse.ResolveDefining`, `BindsDefining`; `using Microsoft.CodeAnalysis` |
| `src/Andirit.CSharp/VerifiedDefiningResolution.cs` | new: `VerifiedDefiningStatus`, `VerifiedDefiningResolution` |
| `tests/Andirit.CSharp.Tests/S2_2Wp3MapperResolverTests.cs` | new: 16 authority-grounded mapper/resolver tests |

No production file outside `src/Andirit.CSharp` was modified; no test outside the
new file; no frozen evidence, oracle, historical report or accepted decision was
altered.

## 12. Boundary

WP3 is **implemented and ready for independent qualification**. This report is
producer evidence, not an independent qualification. It does not pass `G-MINT`,
does not authorize WP4–WP9, minting, persistence/reopen, S2.3+ or publication, and
makes no claim of complete S2.2 acceptance.
