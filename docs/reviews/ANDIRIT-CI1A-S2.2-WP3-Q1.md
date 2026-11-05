# ANDIRIT-CI1A-S2.2-WP3-Q1 — Independent mapper/resolver qualification

**Commission:** `ANDIRIT-CI1A-S2.2-WP3-Q1` (independent qualification only).
**Project:** Andirit. **Date:** 2026-11-05. **Branch:** `main`.
**Reviewer role:** independent reviewer; not the implementer.
**Implementation under review:** `ff58a9290a7583d4493d177e5de5aa6315904a85` (WP3).
**Producer evidence under review:** `e67377555bde8b2958292a2baf45e05908af917f`.
**Starting HEAD (verified):** `e67377555bde8b2958292a2baf45e05908af917f` (tree `0a6a91e95541e28e985269879c2c2e824123fdb7`).

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

Independent qualification supports proceeding to **WP3 owner acceptance**. It does
**not** grant owner acceptance, does **not** pass or advance `G-MINT`, and does
**not** authorize WP4/WP5/S2.3+. `G-MINT` remains **OPEN**.

This report is an independent review, not a producer self-certification: the
mapper was checked against the **accepted frozen R2 authority** rather than
against the production mapper, the resolver was challenged with independently
constructed cases, and the frozen oracle was replayed independently.

---

## 1. Verified baseline and ancestry

Canonical `main`; one worktree (`D:/projects/Andirit`); no stashes; clean working
tree before this review except the untracked review additions listed in §12. `HEAD`
was `e673775` throughout; no linked worktree was used; no concurrent writer moved
`HEAD`, the branch, or `origin/main` during the review. `origin/main` remains
`e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`.

| Role | Commit | Tree | Verified |
| --- | --- | --- | --- |
| Accepted S2.2 WP1–WP3 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` | ✅ |
| G-CONFIRM-Q1 content | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` | ✅ |
| Owner G-CONFIRM acceptance (decision + gate/scope fields) | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` | ✅ |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` | ✅ |
| WP3 producer report | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` | ✅ |

**Ancestry** (`git merge-base --is-ancestor`, exit 0 each):
`e0d9110 → 391abbe → 812584d → ff58a92 → e673775`, a consecutive parent chain.
`ff58a92^ = 812584d`; `e673775^ = ff58a92`. Every named tree was reproduced
exactly; no historical commit was rewritten.

**Scope of change.** `git diff --stat 812584d..e673775 -- src tests benchmarks smoke`
is exactly the four WP3 files (+649): `VerifiedCSharpCompilation.cs` (+50),
`VerifiedCSharpAnalysis.cs` (+72), `VerifiedDefiningResolution.cs` (new, +46),
`tests/Andirit.CSharp.Tests/S2_2Wp3MapperResolverTests.cs` (new, +481). The diff of
the two modified production files is **purely additive** (two `using` directives
plus new internal members); no existing method body, no frozen evidence, no oracle,
no accepted decision and no test was altered. `e673775` changes only
`docs/reviews/ANDIRIT-CI1A-S2.2-WP3-IMPL-01.md`.

---

## 2. Independent authority reconstruction

Authority was reconstructed from accepted sources **before** reading the producer's
rationale, and the producer's implementation report was treated as a claim to
verify.

| Authority | Location | What it fixes |
| --- | --- | --- |
| Option A decision (`S2.2-ID OPTION A ACCEPTED`) | `docs/decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md` §5.1/§5.2 | `asmctx3`/`usage4`/`compctx1`/`compctx2` roles; required tests 1–10 |
| Mapping contract (binding) | `docs/planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md` §1–§3, §5–§7 | R2-faithful `compctx1` derivation; substitution; comparison profiles; association invariant; negatives |
| G-CONFIRM package / owner acceptance | `docs/planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md`; `docs/decisions/adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md` §8 | nine-part rows 5/6/13; `G-CONFIRM PASSED`; WP3-pre-mint vs WP4-runtime boundary; C1–C4 |
| G-CONFIRM-Q1 independent confirmation | `docs/reviews/CI1A-S2.2-G-CONFIRM-Q1/REPORT.md`, `fourteen-row-confirmation-matrix.md` (rows 5, 6, 12, 13) | confirmed authority and retained qualifications |
| G-MINT review package | `docs/planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md` | reviewer scope, pre-WP5 sequencing, verdict ownership |
| S2.1 provenance contract | `docs/planning/adr-ci1-01-ci1a-s2-1/provenance-contract.md` §1–§5 | exact live-instance project-reference binding; `compctx2` isolation |
| Stage 2 boundary | `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md` §3.4–3.9, §5 | exact-live defining compilation, refusal vocabulary |
| Frozen R2 authority | `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/authority/CompilationProvenance.cs` (`AdrCi1Exp.Shared`) | the accepted R2 `Describe`/`CompilationReferenceIdentity`/`OptionsDigest`/`CompilationUniverse.Verify` |
| Frozen R2 expected | `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json` | 185-check frozen oracle anchor |

**Reconstructed R2 `compctx1` (frozen):**

```
compctx1 = "compctx1:sha256:" + SHA256("compctx1-context",
  "compctx1", repositoryId, projectUnitId, assemblyName, metadataIdentity,
  moduleName, optionsDigest, sources..., references...)
source  = normalizedPath | "sha256:"+hex | languageVersion | symbols   (ordinal by path, then hash)
refitem = "file|"+normalizedPath+"|"+sha256(bytes)              (PE)
        | "compilation|"+asm+"|"+metadataIdentity+"|"+trees     (project; R2 legacy)
trees   = join(",", trees.OrderBy(FilePath, Ordinal).Select(p => normalize(p)+"="+sha256Hex(utf8(text))))
```

All parts are 32-bit big-endian length-prefixed UTF-8; reference items are sorted
ordinal with **no dedup**. This was implemented independently in the reviewer probe
project only through the **accepted frozen R2 authority** (linked source), never
through the production mapper.

**Expected semantics fixed by authority:**

- `compctx2` is the sole strong project-reference/provenance signal; it must never
  enter `compctx1`/`asmctx3`/`usage4` (Option A §5.1(3)(7)).
- The S2.2 `compctx1` of a dependency-bearing defining compilation is a **mapping**,
  not a new identity: every field is reused and each
  `compilation-context|<dep compctx2>` reference item is replaced by the frozen R2
  legacy item from the **verified live dependency** (mapping contract §2).
- Association requires (1) exactly one verified live defining instance, (2) a
  complete declared universe, (3) the current verified `compctx2` equal to the
  recorded context (mapping contract §3).
- Actual verified PE identities are preserved; fixture labels are a **test-only**
  comparison alignment that must never enter production (C2 / mapping contract §2).
- Precedence among refusal statuses is **not specified** by any accepted document.

---

## 3. Mapper findings — `VerifiedCSharpCompilation.MapR2FaithfulCompctx1()`

**Result: no demonstrated mapper defect.** Every mapper probe passed, including
byte-exact digest equality with the accepted frozen R2 authority.

| # | Requirement (authority) | Independent probe | Actual | Expected (independent derivation) |
| --- | --- | --- | --- | --- |
| 1 | Exact R2 formula reproduction (mapping §2) | `Mapper_Standalone_EqualsFrozenR2Authority` | equal digest | frozen `Describe` digest |
| 2 | Single substitution of dependency `compctx2` descriptors (mapping §2) | `Mapper_DependencyBearing_EqualsFrozenR2Authority` | equal digest; no `compilation-context\|` remains; one `compilation\|Lib\|` | frozen `Describe` with a live `CompilationReference` |
| 3 | Actual PE identities preserved (C2) | `Mapper_PreservesActualPeIdentity_AndIsolatesCompctx2` | `file\|<actual path>\|…` present; no `runtime/System.` label | actual captured identity |
| 4 | Assembly/metadata/syntax-tree identity (mapping §2) | probes 1–3 + `Mapper_TransitiveLegacyItem...` (producer) | equal digest | frozen `assemblyName`/`MetadataIdentity`/tree-text SHA-256 |
| 5 | Multiple dependencies + ordering | `Mapper_TwoDistinctDependencies_EqualsFrozenR2Authority` | equal digest; both items present | frozen `Describe` ordinal, no dedup |
| 6 | No field omission/duplication | `Mapper_IdenticalLegacyItems_NoDedup_EqualsFrozenR2Authority` | two identical legacy items retained | R2 does not dedup |
| 7 | Determinism | `Mapper_DeterministicAcrossFreshConstructedInstances` (3 fresh graphs × 2 calls) | single distinct digest | identical inputs ⇒ identical digest |
| 8 | Legacy S2.1 / S2.2 separation | `Mapper_LegacyTwinSeparatedFromS22Mapper` | dependency-free: `LegacyDescriptor` present and equal to mapper; dependency-bearing: `LegacyDescriptor == null`, mapper still derivable | accepted `LegacyDescriptor` contract |
| 9 | Declared comparison profiles (C2) | `Mapper_PreservesActualPeIdentity…`, `Mapper_DifferentReferenceInventory_DoesNotCollapse` | different inventories ⇒ different digests | profiles not conflated |
| 10 | No unsupported equivalence/normalization | `Mapper_DifferentReferenceInventory_DoesNotCollapse` | distinct digests | no collapse/normalization |

Notes on fidelity boundaries (all inherited, not WP3-introduced):

- `ProvenanceHash.Options` uses `WarningLevel.ToString(InvariantCulture)` where the
  frozen authority uses `WarningLevel.ToString()`; equal for non-negative integers
  under all practical cultures. Digest equality was confirmed empirically against
  the frozen authority.
- PE reference identity is bound to the **captured image** (`file|path|sha256(captured)`),
  not a re-read of the path; this is the accepted S2.1 behavior and is stricter than
  R2's `File.ReadAllBytes(path)`. Probes used the live host DLL, so the two agree.
- Defining compilations with a null assembly name are refused by S2.1
  (`MissingAssemblyName`); the R2 `"(anonymous)"` fallback is therefore unreachable
  in production. No divergence on reachable inputs.

---

## 4. Resolver findings — `VerifiedCSharpUniverse.ResolveDefining(...)`

**Result: no demonstrated blocking defect; three explicit qualifications and one
unreachable defensive status.**

Accepted status vocabulary (`VerifiedDefiningStatus`,
`src/Andirit.CSharp/VerifiedDefiningResolution.cs`) and observed behavior:

| Status | Independent probe | Actual | Authority / expected |
| --- | --- | --- | --- |
| `Complete` | `Resolver_Complete_ExactLiveInstance_ReturnsMappedCompctx1` | `Complete`; `Defining` is the exact live instance; `Compctx1` = mapped `compctx1` | mapping §3 |
| `Incomplete` | `Resolver_RefusesIncompleteUniverse_BeforeBinding` | `Incomplete` before binding | completeness precedes binding |
| `Ambiguous` | `Resolver_RefusesAmbiguousTwoLiveBindings` | `Ambiguous` for two live bindings | mapping N3 |
| `MissingDefiningCompilation` | `Resolver_RefusesMissingDefiningCompilation`; `Resolver_RefusesSubstitutedLiveInstanceWithSameDigest` | `MissingDefiningCompilation` | mapping N1 |
| `UnsupportedExternalEvidence` | `Resolver_RefusesExternalUnitAtConstruction` | refused at `Universe.Create` (`ScopeSourceMembershipMismatch`) | mapping N2; **status unreachable via `ResolveDefining`** |
| `StaleContext` | `Resolver_RefusesStaleAndFabricatedRecordedContext` | `StaleContext` for zero/`a`/non-canonical recordings | mapping §3.3, N5 |

Challenge coverage:

- **Valid defining compilation + caller association** — `Complete` via the direct
  branch; `Resolver_BindsCrossProjectViaSharedAssemblySymbol` demonstrates that the
  ordinary cross-project positive is bound by `ReferenceEquals(target.ContainingAssembly,
  entry.Compilation.Assembly)` (Roslyn shares the referenced compilation's assembly
  symbol through a `CompilationReference`; `sameAssemblyInstance=True`).
- **Exact live-instance binding** — direct branch uses `ReferenceEquals`; substituted
  same-digest instance + same-identity caller reference ⇒ `MissingDefiningCompilation`.
- **Equivalent-looking but different instances** — see qualification **F1**.
- **Fabricated recorded contexts** — `StaleContext`.
- **Previously valid but stale contexts** — `StaleContext`.
- **Missing defining compilation** — `MissingDefiningCompilation`.
- **Multiple candidate defining compilations** — `Ambiguous`.
- **Incomplete compilation evidence** — `Incomplete` (missing declared unit or recorded refusal).
- **Unsupported external dependencies** — refused at construction (see F4).
- **Mismatched caller and defining contexts** — substituted instance refused.
- **Boundary combinations** — precedence probed (see F2).
- **No structural-only success where exact instance is required** — see F1.
- **No silent minting / evidence extraction / persistence** —
  `Resolver_DoesNotMintAsmctxOrPersistAssociation`: the result exposes only a mapped
  `compctx1`; no `asmctx3`/`usage4` is produced; no persistable association state;
  no `CodeOccurrenceCoverage.Merge` involvement.

### Findings

**F1 — QUALIFICATION (accepted-authority-consistent). Cross-project binding validates
the target's containing assembly by metadata identity, not exact live instance.**
`BindsDefining` binds a universe entry when the caller holds a live
`CompilationReference` to that exact entry **and** the target's containing assembly
has the same `metadataIdentity` as the referenced compilation. Reproduced
(`Resolver_RefusesForeignIdentityEqualTargetInstance`): a target symbol taken from a
**different, identity-equal** compilation `libB` binds to the universe's `libA`
(`status=Complete`, `bound to libA=True`) when the caller references `libA`.

This is **not** a WP3-introduced weakening: the accepted frozen R2 authority
(`AdrCi1Exp.Shared.CompilationUniverse.Verify`/`Binds`) reaches the same `Complete`
verdict for the same input (its cross branch is identical, and its direct branch uses
`SymbolEqualityComparer.Default` identity equality, which is *weaker* than the
production `ReferenceEquals`). The probe corroborates this: `frozen-r2
foreign-instance status=Complete`. Under the accepted contract the target is obtained
from the same live compilation graph (the consuming project's semantic model / the
universe's live compilations), so a foreign identity-equal instance is not a
supported input. The exact-instance requirement is enforced on the **defining**
compilation (the caller's live reference) and hardened in the direct branch.
Flagged for `G-MINT` review; not classified as a defect.

**F2 — QUALIFICATION (contract-unspecified precedence, flagged not invented).**
No accepted document specifies precedence among
`Incomplete`/`Ambiguous`/`MissingDefiningCompilation`/`UnsupportedExternalEvidence`/`StaleContext`.
The implementation's deterministic order is:
`Incomplete` → (`Ambiguous` | `MissingDefiningCompilation`) →
`UnsupportedExternalEvidence` → `StaleContext`. Probes confirm `Incomplete` beats
`Missing` and `Ambiguous` beats `Stale`. The ordering is deterministic and
conservative (fail-closed); it is recorded here as an **ambiguity to be settled at
`G-MINT`**, not silently ratified.

**F3 — QUALIFICATION. A `null` recorded context returns `Complete`.**
`ResolveDefining(target, recordedCompctx2: null, …)` treats "no recorded association"
as no comparison to fail and returns `Complete` with the mapped `compctx1`. This is
acceptable pre-mint (no fact is emitted, no authority is minted), but it means
`Complete` alone does **not** prove that a recorded defining `compctx2` was validated.
`G-MINT` must confirm the extraction path always supplies the defining context.

**F4 — NON-BLOCKING. `UnsupportedExternalEvidence` is defensive/unreachable.**
`VerifiedCSharpUniverse.Create` refuses out-of-scope entries at construction
(`ScopeSourceMembershipMismatch`), so `ResolveDefining` cannot observe an out-of-scope
entry in a validly constructed universe. The status is declared and documented; the
external negative is asserted at construction. This is consistent with "the scope is
the authority boundary" (mapping contract §1.4).

**F5 — NON-BLOCKING. Producer evidence report reproduced.** All producer-claimed
totals and hashes reproduced independently (§7). No overclaim found.

---

## 5. Option A §5.2 negative-test results (pre-mint, in-memory)

Independent reconstruction of the Option A §5.2 in-memory association negatives,
distinguishing **implemented WP3 checks** from **requirements open for `G-MINT`**.

| Option A §5.2 test | WP3 independent evidence | Status |
| --- | --- | --- |
| 1 conditional defining API change changes target | `Mapper_DefiningSourceChange_ChangesMappedIdentity…` (mapped `compctx1` changes) | Covered at `compctx1`/association; `asmctx3` minting = WP4 (open) |
| 2 transitive-only: identity stable, `compctx2` changes | `Mapper_DefiningSourceChange…TransitiveOnlyDoesNot` | Covered |
| 3 caller unchanged; target follows defining API | `Mapper_DefiningSourceChange…` + existing change tests | Covered at `compctx1`; `usage4` = WP5 (open) |
| 4 restore inputs → original identity | `Mapper_DeterministicAcrossFreshConstructedInstances` | Covered |
| 5 distinct defining compilations → distinct targets | `Mapper_TwoDistinctDependencies…`, `Mapper_IdenticalLegacyItems_NoDedup…`, `Resolver_RefusesAmbiguousTwoLiveBindings` | Covered at `compctx1` |
| 6 equivalent reference artifact → equality per contract | `Mapper_PreservesActualPeIdentity…` | Partial: production preserves actual identity; cross-profile equality is a `G-MINT` comparison item |
| 7 re-ingestion determinism | `Mapper_DeterministicAcrossFreshConstructedInstances` (×3) | Covered |
| 8 stale publication refused (in-memory) | `Resolver_RefusesStaleAndFabricatedRecordedContext`, `Resolver_RefusesSubstitutedLiveInstanceWithSameDigest` | Covered (in-memory); native reopen = S2.3 |
| 9 deterministic, documented mapping for dependency-bearing compilations | `Mapper_DependencyBearing…`, `Mapper_TwoDistinctDependencies…` | Covered |
| 10 no minting from unverified/incomplete; `compctx2` cannot enter `usage4`/`asmctx3` | `Mapper_PreservesActualPeIdentity_AndIsolatesCompctx2`, `Resolver_DoesNotMintAsmctxOrPersistAssociation` | Covered |

Required in-memory association negatives:

| Required negative | Result |
| --- | --- |
| Invalid association rejected | ✅ `StaleContext` / `MissingDefiningCompilation` / `Ambiguous` / `Incomplete` |
| Valid association preserved | ✅ `Complete` positive |
| Stale / substituted identity cannot establish authority | ✅ stale + substituted-same-digest probes |
| Tests do not require WP4 runtime output | ✅ all probes are in-memory |
| No minting performed | ✅ no `asmctx3`/`usage4`, no persistence |
| Negatives not bypassable by a weaker comparison path | ✅ for stale/complete gates; the only weaker path (cross metadata binding) is accepted-authority-consistent (F1) |

**Explicitly open for `G-MINT` (not implemented by WP3):** `asmctx3`/`usage4`
construction and extraction-time emission (WP4/WP5); cross-profile equivalent-reference
equality (test 6); the precedence and null-recorded-context questions (F2/F3).

---

## 6. C1–C4 and NEW-04 compliance

| Item | Accepted requirement | Independent verification | Verdict |
| --- | --- | --- | --- |
| **C1** | `Q-F3` remains the bounded metadata surface | WP3 adds no metadata validation; the only `Capture` path is untouched; diff is additive | ✅ Bounded, intact |
| **C2** | Comparison profiles explicit; actual PE identities preserved | Mapping contract §2 profiles; mapper preserves `file\|<actual>` verbatim; `Mapper_PreservesActualPeIdentity…`, `Mapper_DifferentReferenceInventory…` | ✅ Intact |
| **C3** | Ninth field terminology is `closure authority` | WP3 changed no documentation; accepted docs retain "closure authority"/"Closure" | ✅ Intact |
| **C4** | No broader runtime authority claimed | New members are `internal`; no minting/persistence/public API; report reproduces | ✅ Intact |
| **NEW-04** | Coverage-merge authority deferred to WP6 | `CodeOccurrenceCoverage.Merge` last changed at `fc515ff` (pre-correction) and untouched by WP3; resolver does not use `Merge` | ✅ Correctly deferred |

No pre-correction `Merge` behavior is treated as authoritative closure: `ResolveDefining`
and the mapper never consult `CodeOccurrenceCoverage`. The accepted boundary
(`G-CONFIRM` owner §8: `G-CONFIRM → WP3 mapper → G-MINT → WP4 → WP5`, NEW-04 at WP6)
is preserved.

---

## 7. Reproduced test totals and oracle hash

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`
(`docs/reviews/CI1A-S2.2-WP3-Q1-evidence/environment.txt`).

| Suite | Command | Result (independently reproduced) |
| --- | --- | --- |
| Independent WP3 reviewer probes | `dotnet test docs/reviews/CI1A-S2.2-WP3-Q1-tests/ReviewerTests.csproj` | **19 / 0 / 0** (twice) |
| Producer WP3 targeted | `dotnet test tests/Andirit.CSharp.Tests --filter FullyQualifiedName~S2_2Wp3MapperResolverTests` | **16 / 0 / 0** |
| `Andirit.CSharp.Tests` | `dotnet test tests/Andirit.CSharp.Tests` | **183 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **708 / 0 / 0** (`Andirit.Tests` 325, `Andirit.CSharp.Tests` 183, `Andirit.TreeSitter.Tests` 157, `Andirit.LatticeDb.Tests` 43) |
| Frozen R2 oracle | `dotnet run … SemanticCorpusOracle --expected oracle-expected-r2.json` | **185 / 185**, exit 0 (two processes) |

Frozen oracle hashes (independently validated):

- Frozen expected `oracle-expected-r2.json`:
  `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` ✅
- As-produced output:
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` ✅ (identical across two runs)
- LF-normalized output is **byte-identical** to the committed
  `docs/evidence/adr-ci1-01-ci1a-s2-0-r2/oracle-r2-run-1.json`:
  `577603295884242155FACF737066D019C60198DE31E6A01FBED644C9CE40891D` ✅

**Regression assessment.** The WP3 diff is additive and resides only in
`src/Andirit.CSharp` and the new test file. The full solution regression is unchanged
from the pre-WP3 total, and no existing behavior outside the new members was modified.
Cross-platform support is **not inferred**: all execution was Windows x64 only.

---

## 8. Independent probe inventory

Project: `docs/reviews/CI1A-S2.2-WP3-Q1-tests/ReviewerTests.csproj`
(assembly name `Andirit.CSharp.Tests` to reuse the existing internal seam; links the
frozen R2 authority `…/authority/CompilationProvenance.cs`).

Mapper probes (9): `Mapper_Standalone_EqualsFrozenR2Authority`,
`Mapper_DependencyBearing_EqualsFrozenR2Authority`,
`Mapper_TwoDistinctDependencies_EqualsFrozenR2Authority`,
`Mapper_IdenticalLegacyItems_NoDedup_EqualsFrozenR2Authority`,
`Mapper_DeterministicAcrossFreshConstructedInstances`,
`Mapper_PreservesActualPeIdentity_AndIsolatesCompctx2`,
`Mapper_DefiningSourceChange_ChangesMappedIdentity_TransitiveOnlyDoesNot`,
`Mapper_LegacyTwinSeparatedFromS22Mapper`,
`Mapper_DifferentReferenceInventory_DoesNotCollapse`.

Resolver probes (10): `Resolver_Complete_ExactLiveInstance_ReturnsMappedCompctx1`,
`Resolver_BindsCrossProjectViaSharedAssemblySymbol`,
`Resolver_RefusesStaleAndFabricatedRecordedContext`,
`Resolver_RefusesSubstitutedLiveInstanceWithSameDigest`,
`Resolver_RefusesForeignIdentityEqualTargetInstance`,
`Resolver_RefusesAmbiguousTwoLiveBindings`,
`Resolver_RefusesIncompleteUniverse_BeforeBinding`,
`Resolver_RefusesMissingDefiningCompilation`,
`Resolver_RefusesExternalUnitAtConstruction`,
`Resolver_DoesNotMintAsmctxOrPersistAssociation`.

Expected values are derived only from the accepted frozen R2 authority and known
inputs; the production mapper is never used to compute its own expectation. Evidence
artifacts: `docs/reviews/CI1A-S2.2-WP3-Q1-evidence/`
(`reviewer-wp3-probes.log` + `.trx`, `reviewer-wp3-probes-repeat.log`,
`producer-wp3-targeted.log`, `csharp-suite.log`, `solution-suite.log`,
`oracle-r2.json` + `oracle-r2.log`, `environment.txt`).

---

## 9. Findings with evidence and severity

| ID | Severity | Finding | Smallest reproducible case | Authority |
| --- | --- | --- | --- | --- |
| F1 | **QUALIFICATION** | Cross-project binding identifies the target's containing assembly by metadata identity; a foreign identity-equal target instance binds to a universe member. Matches accepted frozen R2 `Verify`/`Binds`; production's direct branch is stricter. | `Resolver_RefusesForeignIdentityEqualTargetInstance`: `libB` symbol + caller referencing `libA` ⇒ `Complete` bound to `libA`; frozen R2 authority also `Complete` | mapping §3.1; S2.1 §5; frozen R2 `Binds` |
| F2 | **QUALIFICATION** | Refusal-status precedence is unspecified by accepted authority; implementation order is `Incomplete` → `Ambiguous`/`Missing` → `External` → `Stale`. | `Resolver_RefusesIncompleteUniverse_BeforeBinding` (`Incomplete` beats `Missing`); `Resolver_RefusesAmbiguousTwoLiveBindings` (`Ambiguous` beats `Stale`) | none (ambiguity flagged) |
| F3 | **QUALIFICATION** | `recordedCompctx2 == null` returns `Complete`; `Complete` does not by itself prove a recorded association was validated. | `Resolver_Complete_ExactLiveInstance_ReturnsMappedCompctx1` with `null` recorded context | mapping §3.3; Option A §5.1(2) |
| F4 | **NON-BLOCKING** | `UnsupportedExternalEvidence` is unreachable via `ResolveDefining` (refused earlier at universe construction). | `Resolver_RefusesExternalUnitAtConstruction` | mapping §1.4, N2 |
| F5 | **NON-BLOCKING** | Producer evidence report totals/hashes reproduced; no overclaim. | §7 | — |

No **BLOCKING** finding was demonstrated. No **UNSUPPORTED HYPOTHESIS** is promoted to
a defect; F1 is reproduced and explicitly grounded in accepted authority, so it is
recorded as a qualification rather than a defect.

---

## 10. Remaining limitations

- **No minting.** `asmctx3`/`usage4` construction, extraction emission and WP4 runtime
  integration are not implemented; the first S2.3 gate (native reopen enforcement) is
  not implemented.
- **Caller-supplied component digests.** `identifierDigest`/`declarationDigest`/
  `shapeDigest` remain the accepted S2.2/S-01 obligation; WP3 does not canonicalize them.
- **`G-MINT` remains open.** Cross-profile equivalent-reference equality (Option A
  §5.2 test 6), the F1 binding boundary, the F2 precedence question, and the F3
  null-context boundary are `G-MINT` review items.
- **Option A §5.2 tests 1–3 and 5** are covered only at `compctx1`/association; their
  full `asmctx3`/`usage4` forms are WP4/WP5.
- **Platform.** Windows x64 only; no cross-platform result is inferred.
- **No production extraction path** is wired to the resolver yet (WP4).

---

## 11. Recommended owner disposition

**`SUPPORTED WITH QUALIFICATIONS`.** The evidence supports proceeding to **WP3 owner
acceptance**. The mapper reproduces the accepted frozen R2 `compctx1` exactly,
including single-substitution, PE-identity preservation and multi-dependency/no-dedup
behavior; the resolver implements the accepted pre-mint association vocabulary and
correctly refuses stale, fabricated and substituted contexts; C1–C4 and NEW-04 are
intact; the frozen oracle and all regressions reproduce.

**Do not advance `G-MINT`.** `G-MINT` is the next gate and remains reviewer-owned and
**OPEN**; F1–F3 should be carried into its review. No owner acceptance is granted by
this report; no production code, frozen oracle, accepted decision or producer test was
modified.

If remediation is later required, the smallest bounded scope would be a single
targeted change in `VerifiedCSharpUniverse.BindsDefining` (the cross-project branch);
requalification would then require re-running the reviewer resolver probes plus the
WP3 targeted and full C# suites. **No remediation is required for WP3 acceptance.**

---

## 12. Exact commit and tree identities

| Object | Commit | Tree |
| --- | --- | --- |
| Accepted foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM-Q1 | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` |
| Owner G-CONFIRM acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| Starting/expected HEAD | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` |

Reviewed working tree: `main` at `e673775`, `origin/main` = `e0d9110`, one worktree,
no stashes. Review additions (this commission) are limited to
`docs/reviews/ANDIRIT-CI1A-S2.2-WP3-Q1.md`,
`docs/reviews/CI1A-S2.2-WP3-Q1-tests/`, and
`docs/reviews/CI1A-S2.2-WP3-Q1-evidence/`. Production source is unchanged.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS`.** Evidence supports WP3 owner
acceptance consideration; `G-MINT` remains **OPEN** and is not advanced. Stop after
independent qualification and evidence preservation.
