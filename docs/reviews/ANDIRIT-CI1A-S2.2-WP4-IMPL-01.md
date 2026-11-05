# ANDIRIT-CI1A-S2.2-WP4-IMPL-01 — Production `asmctx3` minting implementation report

**Commission:** `ANDIRIT-CI1A-S2.2-WP4-IMPL-01` (bounded production implementation).
**Project:** Andirit. **Date:** 2026-11-05. **Branch:** `main`.
**Target disposition:** `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.

This report records the WP4 production `asmctx3` minting capability only. It does
**not** independently qualify WP4, does **not** grant owner acceptance, and does
**not** implement WP5 `usage4`, WP6 NEW-04, extraction, persistence/reopen or any
later stage. Producer tests are not independent qualification.

---

## 1. Baseline and final Git identities

| Role | Commit | Tree |
| --- | --- | --- |
| Authoritative baseline (G-MINT owner acceptance) | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| Frozen R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT independent qualification | `1c382d6aaa1c2ceb04b70e65e048357621a73a78` | `c2f7c9ef782e90e70df9c63dfcd62e8cb888f7b9` |
| **WP4 implementation (this commission)** | `eac302295b5bbaf8d71181ddcc70a62089a9a28e` | `9a4ccefc182d5b80123def1dce313350c37e23d6` |

Starting state verified before any change: `main` at `d3da2e0` (tree `029299bf…`),
clean working tree, one worktree, no stashes, `origin/main` = `e0d9110`, 11 commits
ahead, nothing pushed, no concurrent writer. The chain
`e0d9110 → 391abbe → 812584d → ff58a92 → e673775 → 2cdcd10 → 4fdc23c → 1c382d6 → d3da2e0`
was reproduced exactly.

Implementation and evidence commits are recorded in §14. No prior accepted decision,
independent review artifact or frozen oracle file was modified.

---

## 2. Authoritative contracts consulted

- **WP4 scope/authority** — `docs/planning/adr-ci1-01-ci1a-s2-2/scope-and-entry-criteria.md`
  §6.2 (WP4 = "Target minting under the mapping contract with exact live
  defining-compilation instance, `compctx2` equality and `compilation-universe1`
  membership gating"), §6.3 (frozen identity invariants), §6.6 (canonical minting
  only from verified live defining compilations).
- **Option A identity decision** — `docs/decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md`
  §5.1(1)–(2) (`asmctx3` from the defining R2-faithful `compctx1`; `compctx2`
  mandatory provenance validity), §5.2.
- **Mapping contract** — `docs/planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md`
  §1.1 (frozen `asmctx3` tuple), §2 (R2-faithful `compctx1` mapping), §3
  (extraction-time association invariant), §5–§6 (P/N + collision adversaries), §7.
- **Stage 2 boundary / acceptance matrix** —
  `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md` §3.5, §5;
  `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md` §2
  (`asmctx3` components and component domains).
- **G-CONFIRM authority** — `docs/decisions/adr-ci1-01-ci1a-s2-2-g-confirm-owner-01.md`
  §4–§6 (C1–C4; Q2 foundation limitations).
- **WP3 owner acceptance §7** —
  `docs/decisions/adr-ci1-01-ci1a-s2-2-wp3-owner-01.md` GM-1…GM-7.
- **G-MINT owner acceptance §7** —
  `docs/decisions/adr-ci1-01-ci1a-s2-2-g-mint-owner-01.md` (binding WP4 entry
  contract) and G-MINT-Q1 `docs/reviews/ANDIRIT-CI1A-S2.2-G-MINT-Q1.md`.
- **Frozen identity helper** — `src/Andirit.Abstractions/CodeOccurrence.cs`
  (`CodeOccurrenceIdentity.ComputeTargetId`, `CodeOccurrenceTargetId`) — **frozen,
  must not be redefined** (scope §6.3; identity-authority file list §102).
- **Frozen R2 `asmctx3` authority** —
  `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/R2Identity.cs` `TargetId`.

No required WP4 semantic was missing or contradictory; no portion of work was stopped
for a missing decision.

---

## 3. WP4 implementation architecture

The WP4 capability is the **authority-gated minting boundary** that connects the
accepted WP3 mapper/resolver to the frozen R2 `asmctx3` constructor. It deliberately
preserves the separation between **artifact construction** (the frozen structural
helper) and **authority minting** (the new gated entry point):

```
candidate target IMethodSymbol + recorded compctx2 + caller
        │
        ▼
VerifiedCSharpUniverse.MintTarget            ← WP4 authority boundary (new)
  1. require supplied, non-null recorded compctx2   → MissingRecordedContext
  2. ResolveDefining(target, recorded, caller)      ← accepted WP3 resolver (unchanged)
       Complete / Incomplete / Ambiguous / Missing / External / Stale
  3. every non-Complete status refuses              → mapped status, no artifact
  4. map defining compilation → R2-faithful compctx1 (Mapper, WP3)
  5. CodeOccurrenceIdentity.ComputeTargetId(compctx1, defining metadata/module, S-01 digests)
        │
        ▼
VerifiedTargetMint { Minted, TargetId(asmctx3), Compctx1 }   ← no usage4 / no persistence
```

New/changed production surface (all `internal`, no public API change):

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedTargetMint.cs` | **new**: `VerifiedTargetMintStatus`, `VerifiedTargetMint` |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +`VerifiedCSharpUniverse.MintTarget`, +`MapMintStatus` (additive, +49) |

`ResolveDefining` (WP3) and its `VerifiedDefiningStatus` vocabulary are **unchanged**;
`MintTarget` wraps it. The frozen `CodeOccurrenceIdentity.ComputeTargetId` is reused
verbatim. No public API, no `usage4`, no fact emission, no persistence.

---

## 4. Contract-to-code traceability (WP4-A)

| Contract clause | Implementation | Test |
| --- | --- | --- |
| Option A §5.1(1): `asmctx3` from the defining R2-faithful `compctx1` | `MintTarget` uses `resolved.Compctx1.DescriptorDigest` (mapped by WP3) as the constructor context | `Mint_ValidDirectAssociation…`, `Mint_ValidCrossProjectAssociation…` |
| Mapping §1.1 / acceptance-matrix §2: frozen `asmctx3` tuple and domains | `CodeOccurrenceIdentity.ComputeTargetId` (frozen, unchanged) | `Mint_ValidDirectAssociation…` vs independent `ExpectedTargetId` |
| Mapping §3.1: exactly one verified live defining compilation | `ResolveDefining` (WP3), reused | `Mint_AmbiguousDefining…`, `Mint_MissingDefining…` |
| Mapping §3.2: complete declared universe | `ResolveDefining` + `VerifiedCSharpUniverse.IsComplete` | `Mint_IncompleteUniverse…` |
| Mapping §3.3: current verified `compctx2` equals recorded; non-null recording | `MintTarget` null guard + `ResolveDefining` equality | `Mint_NullOrEmptyRecorded…`, `Mint_StaleOrFabricated…`, `Mint_PreviouslyValidButStale…` |
| Option A §5.1(2)/(5); G-MINT §7 GM-3 | `MintTarget` returns `MissingRecordedContext`; `Complete` cannot be reached with no recording | `Mint_NullRecordedContext_ResolveDefiningIsComplete_ButMintRefuses` |
| Mapping §6 / Option A §5.2(10): `compctx2` cannot enter `asmctx3` | frozen constructor rejects a non-`compctx1` context | `Mint_Compctx2CannotEnterAsmctx3` |
| G1 live-instance vs metadata identity | defining context comes from the resolved live entry; cross branch unchanged (WP3) | `Mint_ForeignMetadataEquivalentTarget…`, `Mint_SubstitutedLiveInstance…` |
| G2 fail-closed | every non-`Complete` status maps to a non-`Minted` status; no artifact on failure | `Mint_OverlappingFailures_NeverMint` |
| S-01 caller-supplied component digests | passed through; not verified; no claim of verification | all minting tests |
| scope §6.3 / WP5 boundary: no `usage4` | only `asmctx3` is produced | (absence asserted structurally) |

---

## 5. `asmctx3` construction semantics

```
asmctx3 = "asmctx3:sha256:" + SHA256("asmctx3-context",
  "compctx1", <canonical compctx1:sha256:<64hex>>,
  metadataIdentity, moduleName, identifierDigest, declarationDigest, shapeDigest)
```

- Component domains: `asmctx3-identifier`, `asmctx3-declarations`, `asmctx3-target`.
- Encoding: 32-bit big-endian length-prefixed UTF-8 parts; lowercase hex.
- `metadataIdentity` and `moduleName` are taken from the **defining compilation's**
  verified `compctx2` descriptor (`Compctx1`), never from caller-supplied metadata.
- `identifierDigest`, `declarationDigest`, `shapeDigest` are **caller-supplied**
  canonical site forms (the accepted S-01 obligation); WP4 does **not** canonicalize
  or independently verify them and makes no such claim.
- The constructor is the frozen `CodeOccurrenceIdentity.ComputeTargetId`; its
  canonical-`compctx1` guard structurally rejects a `compctx2` context.
- Determinism follows from deterministic inputs; the artifact is a pure function of
  the defining `compctx1` and the (caller-supplied) component digests.

---

## 6. Mandatory recorded-`compctx2` enforcement (G3)

`MintTarget` requires a supplied, non-null recorded defining `compctx2` **before**
consulting the resolver, which forbids bypass through the nullable resolver path:

- `recordedCompctx2` is a **required parameter** (no optional/default/overload emits
  authority without it).
- `null`, empty or whitespace recording → `MissingRecordedContext`, no artifact.
- A supplied recording is validated by `ResolveDefining` against the exact live
  defining entry's current verified descriptor digest; mismatch (wrong defining
  compilation, stale, fabricated, substituted) → `StaleContext`, no artifact.
- `Complete` alone is **not** authority: the WP4 test
  `Mint_NullRecordedContext_ResolveDefiningIsComplete_ButMintRefuses` demonstrates
  that `ResolveDefining(..., null, ...)` is `Complete` while `MintTarget(..., null, ...)`
  refuses. This is the load-bearing enforcement (failing-before / passing-after).

---

## 7. G1–G4 compliance

| Qualification | WP4 treatment |
| --- | --- |
| **G1** cross-project metadata identity | The defining compilation is bound by the accepted live-instance relationship (WP3, unchanged). The artifact is built from the **defining member's** mapped `compctx1`, `MetadataIdentity` and `ModuleName`; metadata equivalence is **not** upgraded to exact-instance authority. `Mint_ForeignMetadataEquivalentTarget…` mints the member's context and documents the bounded behavior. |
| **G2** fail-closed | Every non-`Complete` resolver status maps to a non-`Minted` WP4 status; no precedence is invented or relied upon; failures produce no artifact. |
| **G3** mandatory recording | Enforced as a required parameter and null guard (§6). |
| **G4** §5.2 runtime forms | WP4-applicable forms implemented and tested; WP5 `usage4` forms remain open (§8/§11). |

C1–C4 are untouched (no metadata-validation, comparison-profile, terminology or
authority-claim change). NEW-04 remains deferred (WP6) and is not consulted.

---

## 8. Option A §5.2 runtime-test coverage

| §5.2 test | WP4 form | Test | Result |
| --- | --- | --- | --- |
| 1 conditional defining API change → target changes | ✅ `asmctx3` changes | `Mint_ConditionalDefiningApiChange_ChangesAsmctx3` | pass |
| 2 transitive-only change → identity stable | ✅ `asmctx3` stable while defining `compctx2` changes | `Mint_TransitiveOnlyChange_Asmctx3StableWhileCompctx2Changes` | pass |
| 3 caller unchanged; target follows defining API | ✅ `asmctx3` form (tests 1/3 share the defining-side assertion) | `Mint_ConditionalDefiningApiChange_ChangesAsmctx3` | pass; `usage4` **WP5** |
| 4 restore inputs → original identity | ✅ | `Mint_RestoreInputs_ReproducesOriginalAsmctx3` | pass |
| 5 distinct defining compilations → distinct targets | ✅ | `Mint_DistinctDefiningCompilations_DistinctAsmctx3` | pass |
| 6 equivalent reference artifact → equality | ✅ same-input profile | `Mint_EquivalentDefiningCompilations_EqualAsmctx3` | pass; cross-profile equality is a G-MINT comparison item |
| 7 re-ingestion determinism | ✅ repeated/fresh instances | `Mint_DeterministicAcrossRepeatedAndEquivalentInstances` | pass (in-process) |
| 8 stale publication refused (in-memory) | ✅ stale/fabricated/wrong/substituted refused | `Mint_StaleOrFabricatedRecordedContext_Refuses`, `Mint_PreviouslyValidButStaleContext_Refuses` | pass; native reopen **S2.3** |
| 9 deterministic documented mapping | ✅ (WP3) + WP4 mint | `Mint_Deterministic…`, WP3-Q1 probes | pass |
| 10 no mint from unverified/incomplete; isolation | ✅ | `Mint_IncompleteUniverse…`, `Mint_NullRecordedContext…`, `Mint_Compctx2CannotEnterAsmctx3` | pass |

Valid controls guard against over-rejection: `Mint_ValidDirectAssociation…`,
`Mint_ValidCrossProjectAssociation…`, `Mint_ForeignMetadataEquivalentTarget…`.
No WP5-dependent `usage4` assertion is marked complete.

---

## 9. Test commands and actual outcomes

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.

| Suite | Command | Actual |
| --- | --- | --- |
| New WP4 tests | `dotnet test tests/Andirit.CSharp.Tests --filter FullyQualifiedName~S2_2Wp4MintTests` | **20 / 0 / 0** |
| `Andirit.CSharp.Tests` | `dotnet test tests/Andirit.CSharp.Tests` | **203 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **728 / 0 / 0** (`Andirit.Tests` 325, `Andirit.CSharp.Tests` 203, `Andirit.TreeSitter.Tests` 157, `Andirit.LatticeDb.Tests` 43) |
| WP3 targeted (reference) | `--filter FullyQualifiedName~S2_2Wp3MapperResolverTests` | **16 / 0 / 0** |
| WP3-Q1 independent probes (reference) | `dotnet test docs/reviews/CI1A-S2.2-WP3-Q1-tests` | **19 / 0 / 0** |
| G-MINT-Q1 independent probes (reference) | `dotnet test docs/reviews/CI1A-S2.2-G-MINT-Q1-tests` | **12 / 0 / 0** |

Reference totals from prior stages (C# 183 / solution 708) increased by exactly the 20
new WP4 tests (203 / 728). Expected `asmctx3` values are reconstructed independently in
the test (`ExpectedTargetId`/`ExpectedCompctx1`), never by the minting code under test.

---

## 10. Frozen oracle integrity

- Frozen expected `oracle-expected-r2.json`:
  `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` (unchanged).
- Oracle replay: **185 / 185**, exit 0; as-produced output SHA-256
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged).
- The oracle and all frozen evidence are byte-unchanged; WP4 does not consume or alter
  the oracle. Evidence captured in `docs/reviews/CI1A-S2.2-WP4-IMPL-01-evidence/`.

---

## 11. Remaining WP5 obligations

- **`usage4` construction** (`CodeOccurrenceIdentity.ComputeOccurrenceId`) and the
  caller-side occurrence emission are **not** implemented.
- Option A §5.2 test 3's `usage4` assertion and any occurrence-level forms remain WP5.
- Extraction wiring (same live compilation as graph facts), coverage/refusal emission
  (WP5) and the unconditional-inactive scanner (F09) remain WP5.
- **NEW-04** coverage-merge correction remains **WP6**; `CodeOccurrenceCoverage.Merge`
  is unchanged and unused.

---

## 12. Platform limitations

- Executed on Windows x64 (`net10.0`) only; no cross-platform qualification.
- WP4 separate-process mint determinism is not independently run here; the mint is a
  pure function of the same deterministic inputs, and WP3-Q1 already demonstrated
  separate-process mapper determinism. Independent WP4-Q1 should re-confirm.
- S-01 caller-supplied component digests remain uncanonicalized/unverified.
- Native persistence/reopen (S2.3) is out of scope.
- Fixture-profile `compctx1` anchor reproduction is accepted prior evidence, not
  re-derived here.

---

## 13. Known restrictions and unresolved issues

- The frozen public structural helper `CodeOccurrenceIdentity.ComputeTargetId` remains
  a public pure constructor. It is the accepted artifact-construction helper (REM-01),
  not an authority-minting path; WP4 does not add a second bypass and does not treat it
  as authority. Independent qualification should confirm no alternate authority path.
- F1/G1 foreign metadata-equivalent binding is preserved (accepted); WP4 mints the
  defining member's context and does not claim exact foreign-instance identity.
- Refusal-status labels remain order-dependent (accepted qualification); WP4 adds no
  precedence and refuses every non-`Minted` status.
- Malformed caller-supplied component digests surface as the frozen constructor's
  `ArgumentException` (fail-closed: no artifact); this is the accepted constructor's
  behavior, not a new refusal taxonomy.

---

## 14. Recommendation for independent qualification

**`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** An independent WP4-Q1 reviewer
should challenge: (1) identity substitution and the G1 live-instance/metadata
distinction; (2) null/empty/stale/fabricated recorded contexts and any alternate
minting path or overload that could bypass the recording; (3) overlapping refusal
conditions producing no `asmctx3`; (4) the frozen `asmctx3` formula against the
accepted authority with independently derived expected values; (5) `compctx2`
isolation; (6) the WP4/WP5 boundary; and (7) determinism, including across separate
processes.

### Changed-file inventory (production + tests)

| File | Change |
| --- | --- |
| `src/Andirit.CSharp/VerifiedTargetMint.cs` | new |
| `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` | +49 (additive) |
| `tests/Andirit.CSharp.Tests/S2_2Wp4MintTests.cs` | new, 20 tests |

No production file outside `src/Andirit.CSharp` was modified; no previously accepted
decision, reviewer test, frozen oracle or report was altered.

### Git identities

- Starting commit/tree: `d3da2e0` / `029299bf…`.
- Production implementation commit/tree: `eac302295b5bbaf8d71181ddcc70a62089a9a28e` /
  `9a4ccefc182d5b80123def1dce313350c37e23d6`.
- Evidence commit (this report + `CI1A-S2.2-WP4-IMPL-01-evidence/`): the direct child of
  `eac3022`; exact hash recorded in the commission handoff. Final HEAD, ahead/behind and
  push status are recorded there.

---

**Final disposition:** **`IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** WP4
`asmctx3` minting exists with mandatory recorded-`compctx2` enforcement, fail-closed
refusal, G1–G4 compliance and preserved frozen R2 authority. It does not authorize
independent acceptance, WP5, WP6, persistence or publication. Stop after evidence
preservation.
