# ANDIRIT-CI1A-S2.2-WP4-Q1 — Independent `asmctx3` minting qualification

**Commission:** `ANDIRIT-CI1A-S2.2-WP4-Q1` (independent production qualification only).
**Project:** Andirit. **Date:** 2026-11-05. **Canonical branch:** `main`.
**Implementation under review:** `eac302295b5bbaf8d71181ddcc70a62089a9a28e`.
**Implementation evidence under review:** `e05ce93830f9cf2a0ac28e51a9f6980805f58a73`.
**Starting HEAD (verified):** `e05ce93830f9cf2a0ac28e51a9f6980805f58a73` (tree `7531293c49e5e01586b9d4c3af4aafdf8c45fae6`).
**Reviewer role:** independent; not the implementer and not the owner.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

The evidence supports proceeding to **WP4 owner acceptance**. No unauthorized
minting path was demonstrated; valid associations reproduce the exact accepted
`asmctx3` artifact. This is **not** owner acceptance; it does not authorize WP5/WP6,
persistence or publication.

Central question — *can any unsupported, stale, fabricated, substituted, incomplete,
ambiguous or otherwise unauthorized association cause production code to mint an
`asmctx3` artifact?* — answer: **no demonstrated path**; every non-authoritative
association refuses and produces no artifact. Valid associations mint the exact
frozen tuple.

---

## 1. Verified baseline and commit/tree integrity

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT independent qualification | `1c382d6aaa1c2ceb04b70e65e048357621a73a78` | `c2f7c9ef782e90e70df9c63dfcd62e8cb888f7b9` |
| G-MINT owner acceptance | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 implementation | `eac302295b5bbaf8d71181ddcc70a62089a9a28e` | `9a4ccefc182d5b80123def1dce313350c37e23d6` |
| WP4 evidence | `e05ce93830f9cf2a0ac28e51a9f6980805f58a73` | `7531293c49e5e01586b9d4c3af4aafdf8c45fae6` |

- `e0d9110 → … → eac3022 → e05ce93` is a consecutive parent chain
  (`git merge-base --is-ancestor`, exit 0 each); every tree reproduced exactly;
  `e05ce93^ = eac3022`.
- Working tree **clean**; one worktree; no stashes; `origin/main` =
  `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`; `main` **13 ahead, 0 behind**;
  **nothing pushed**; no concurrent writer observed.
- WP4 implementation commit `eac3022` changes exactly three files
  (`VerifiedCSharpAnalysis.cs` +49 additive, `VerifiedTargetMint.cs` new,
  `S2_2Wp4MintTests.cs` +517); no removed production lines; **no `public`
  additions**; no frozen oracle, accepted decision, reviewer artifact or WP3 test
  changed. Evidence commit `e05ce93` is documentation/logs only.
- Authoritative documents read directly: Option A §5.1/§5.2; mapping contract
  §1.1/§2/§3/§5–§7; stage2-boundary §3.5/§5; stage2-acceptance-matrix §2;
  scope-and-entry-criteria §6.2–§6.6; G-CONFIRM owner §4–§6; WP3 owner §7;
  G-MINT owner §7; frozen R2 `AdrCi1Exp.Shared.CompilationProvenance` and
  `R2Identity.TargetId`.

---

## 2. Independent authority reconstruction

Accepted `asmctx3` (frozen R2; mapping contract §1.1; acceptance-matrix §2):

```
asmctx3 = "asmctx3:sha256:" + SHA256("asmctx3-context",
  "compctx1", <canonical compctx1:sha256:<64hex>>,
  metadataIdentity, moduleName, identifierDigest, declarationDigest, shapeDigest)
```

- Component domains `asmctx3-identifier`, `asmctx3-declarations`, `asmctx3-target`;
  32-bit big-endian length-prefixed UTF-8; lowercase hex.
- `compctx1` is the **defining compilation's** R2-faithful context (WP3 mapper;
  actual PE identities preserved; project items substituted).
- `metadataIdentity`/`moduleName` are the **defining compilation's**.
- `identifierDigest`/`declarationDigest`/`shapeDigest` are **caller-supplied**
  canonical site forms (S-01) — not independently verified by WP4.

WP4 authority requirement (scope §6.2; G-MINT owner §7): minting requires exactly
one verified live defining compilation in a complete universe with the defining
compilation's current verified `compctx2` equal to a **supplied, non-null recorded**
`compctx2`; every other outcome refuses; `Complete` alone is not authority.

Expected behavior was reconstructed from these texts and the frozen authority before
relying on any producer test.

---

## 3. Complete `asmctx3` minting-path inventory

Every place in production that can produce or expose an `asmctx3`-shaped value:

| # | Path | Authority? | Notes |
| --- | --- | --- | --- |
| 1 | `VerifiedCSharpUniverse.MintTarget(target, recordedCompctx2, id, decl, shape, caller)` (`VerifiedCSharpAnalysis.cs:143`) | **Yes — the only authority-minting path** | Enforces recording + association; calls the frozen constructor only on success |
| 2 | `CodeOccurrenceIdentity.ComputeTargetId(...)` (`CodeOccurrence.cs:110`, public, frozen) | No — structural constructor | Pure identifier computation; used in `src/` only by path 1; does **not** claim association authority |
| 3 | `new CodeOccurrenceTargetId(value)` (`CodeOccurrence.cs:40`, public) | No — shape-validating value object | Canonical-shape guard only |
| 4 | `CodeOccurrenceFact.TargetId`, `CodeOccurrenceReader`, `InMemoryCodeGraphStore` | No — consume/persist | Stage 1/Stage 2 carriers; not WP4 minting |

**No overload, optional argument, alternate entry point or direct production path
mints authority without the recorded-context step.** `MintTarget` has a single
signature with `recordedCompctx2` as a required parameter. The frozen
`ComputeTargetId` is an accepted identifier-computation helper, not an authority
path (per the commission's instruction). Path 1 is the sole authority-bearing output
of WP4.

---

## 4. Independent `asmctx3` formula and canonicalization verification

Expected values were derived **without** `MintTarget`: the defining `compctx1` comes
from the accepted frozen authority `AdrCi1Exp.Shared.CompilationProvenance.Describe`,
and the `asmctx3` tuple is written independently (manual metadata/module + a fresh
length-prefixed SHA-256). Structural assertions supplement the differential check.

| Probe | Structure | Result |
| --- | --- | --- |
| `Wp4_Standalone_MintEqualsIndependentR2Tuple` | standalone defining compilation | minted == independent expected; `Compctx1` == frozen authority; metadata/module == defining compilation |
| `Wp4_DependencyBearingDefining_MintEqualsIndependentR2Tuple` | defining compilation with one dependency | minted == expected; no `compilation-context\|`; one `compilation\|Leaf\|` |
| `Wp4_MultiDependencyDefining_MintEqualsIndependentR2Tuple` | two distinct dependencies | minted == expected; both `compilation\|L1\|`, `compilation\|L2\|` present |
| `Wp4_CrossProject_MintEqualsIndependentR2Tuple` | target in dependency | minted == expected |
| `Wp4_PeIdentitiesPreserved_Compctx2Isolated_NoNormalization` | actual PE + extra reference | actual `file\|…` preserved; no `compctx2` in identity/context; different inventory ⇒ different identity (no collapse/dedup) |

Component placement/ordering, defining-compilation selection, and metadata/module
identity all matched the accepted tuple. The actual PE identities are preserved
(production actual-path profile). S-01 caller-supplied component digests are passed
through and **not** independently verified by WP4 (retained).

---

## 5. Mandatory recorded-context enforcement (G3)

`Wp4_RecordedContext_Matrix` (independent inputs, actual mint path):

| Case | Recorded / association | Observed | Artifact |
| --- | --- | --- | --- |
| 1 null | no recording | `MissingRecordedContext` | none |
| 2 empty | `""` | `MissingRecordedContext` | none |
| 3 whitespace/malformed | `"   "`, `"not-a-context"` | `MissingRecordedContext` / `StaleContext` | none |
| 4 unrelated compilation | other unit's digest | `StaleContext` | none |
| 5 fabricated | `compctx2:sha256:aaa…` | `StaleContext` | none |
| 6 stale | `compctx2:sha256:111…` | `StaleContext` | none |
| 7 correct context + substituted live instance | lib digest + distinct same-digest instance | `MissingDefiningCompilation` | none |
| 8 wrong context + valid live instance | caller's own digest | `StaleContext` | none |
| 9 correct context + correct defining compilation | lib digest | **`Minted`** | `asmctx3` |
| 10 `ResolveDefining(...,null,...)` | null | `Complete`, but `MintTarget(...,null,...)` → `MissingRecordedContext` | none |

Every refusal returns `TargetId = null` and `Compctx1 = null`. `Wp4_MalformedComponentDigest…`
confirms a non-canonical component digest throws (the frozen constructor's behavior)
with no artifact. No alternate authority-bearing path bypasses the recording.

---

## 6. G1 identity-boundary findings

| Case | Authority permits | Observed |
| --- | --- | --- |
| Valid direct binding | mint | `Minted` |
| Valid cross-project binding | mint | `Minted` |
| Foreign metadata-equal target instance | accepted bounded binding (F1, inherited) | `Minted`; artifact == the **defining member's** context (`Wp4_G1_IdentityBoundaries`) |
| Substituted same-digest instance, no live caller reference | refuse | `MissingDefiningCompilation` |
| Identity-unequal target | refuse | `MissingDefiningCompilation` |
| Ambiguous defining compilation | refuse | `Ambiguous` |
| Incorrect caller association | refuse | `MissingDefiningCompilation` / `StaleContext` |
| Valid metadata + invalid recorded context | refuse | `StaleContext` |

WP4 neither strengthens nor weakens the accepted semantics: the artifact is always
built from the resolved **defining member's** `compctx1`/metadata/module; metadata
equivalence is **not** upgraded to exact-instance authority. The bounded foreign
acceptance is inherited from the accepted R2 authority and preserved unchanged.

---

## 7. G2 fail-closed refusal findings

`Wp4_G2_OverlappingFailures_NeverMint` (observed statuses; no precedence assumed):

```
incomplete+null   => MissingRecordedContext
incomplete+stale  => Incomplete
complete+stale    => StaleContext
complete+missing  => MissingDefiningCompilation
correct+susbstituted-instance => MissingDefiningCompilation
```

Every non-`Complete` resolver outcome maps to a non-`Minted` WP4 status and yields no
artifact. A failed mint produces no authority-bearing object, partial result or side
effect. No combination produced a successful mint; the implementation does not depend
on, and does not ratify, refusal-status precedence.

---

## 8. Option A §5.2 runtime coverage matrix

| §5.2 | WP4-Q1 result | Evidence |
| --- | --- | --- |
| 1 conditional defining API change → target changes | ✅ `asmctx3` changes | `Wp4_OptionAS52_RuntimeForms` |
| 2 transitive-only change → identity stable | ✅ `asmctx3` stable while defining `compctx2` changes | `Wp4_OptionAS52_RuntimeForms` |
| 3 caller unchanged; target follows defining API | ✅ `asmctx3` form | `usage4` **remains WP5** |
| 4 restore inputs → original identity | ✅ | `Wp4_SeparateProcessDigest` (fresh-instance reproduction) |
| 5 distinct defining compilations → distinct targets | ✅ | `Wp4_OptionAS52_RuntimeForms` |
| 6 equivalent reference artifact → equality | ✅ same-input profile | cross-profile equality = G-MINT comparison item |
| 7 re-ingestion determinism | ✅ separate-process identical | `Wp4_SeparateProcessDigest` (runs 1/2) |
| 8 stale publication refused (in-memory) | ✅ | `Wp4_RecordedContext_Matrix`, `Wp4_PreviouslyValid…` (producer) |
| 9 deterministic documented mapping | ✅ | WP3-Q1 probes + this review |
| 10 no mint from unverified/incomplete; isolation | ✅ | `Wp4_Case10…`, `Wp4_G2…`, `Wp4_Compctx2…` (producer) |

No WP5-only `usage4` assertion is marked complete; test 3's `usage4` assertion is
explicitly reserved to WP5.

---

## 9. Separate-process determinism results

`Wp4_SeparateProcessDigest` executed in two independent `dotnet test` processes with
fresh compilation instances and identical controlled inputs:

```
WP4Q1-ASMCTX3=asmctx3:sha256:6ac18ca5edd6fa8f35d7623a244310db70098842107cb228ceab6d020c6b0875
WP4Q1-COMPCTX1=compctx1:sha256:d0e3e7e60d1941c76f5d10c525042850870fc85a8ed046706ffc14f3879df1db
WP4Q1-EXPECTED=asmctx3:sha256:6ac18ca5edd6fa8f35d7623a244310db70098842107cb228ceab6d020c6b0875
```

Runs 1 and 2 are **byte-identical**, and the **independently derived expected equals
the minted artifact** in both. Changed authoritative inputs produce expected
differences (forms 1/5). Evidence: `probe-run-1.log`, `probe-run-2.log`,
`probe-digests-1.txt`, `probe-digests-2.txt`.

---

## 10. Regression and frozen oracle evidence

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.

| Suite | Command | Actual |
| --- | --- | --- |
| WP4-Q1 independent probes | `dotnet test docs/reviews/CI1A-S2.2-WP4-Q1-tests` | **13 / 0 / 0** |
| WP4 targeted | `--filter FullyQualifiedName~S2_2Wp4MintTests` | **20 / 0 / 0** |
| WP3 targeted | `--filter FullyQualifiedName~S2_2Wp3MapperResolverTests` | **16 / 0 / 0** |
| WP3-Q1 probes | `dotnet test docs/reviews/CI1A-S2.2-WP3-Q1-tests` | **19 / 0 / 0** |
| G-MINT-Q1 probes | `dotnet test docs/reviews/CI1A-S2.2-G-MINT-Q1-tests` | **12 / 0 / 0** |
| `Andirit.CSharp.Tests` | `dotnet test tests/Andirit.CSharp.Tests` | **203 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **728 / 0 / 0** (325+203+157+43) |
| Frozen R2 oracle | `dotnet run … SemanticCorpusOracle` | **185 / 185**, exit 0 |

Frozen oracle integrity: expected `oracle-expected-r2.json` =
`274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`; as-produced output
= `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (both unchanged).
S2.1 compatibility preserved: `ResolveDefining`/`BindsDefining` bodies unchanged; the
WP4 diff is additive. Windows-only; no cross-platform support inferred.

---

## 11. WP4/WP5 scope assessment

The implementation commit `eac3022` adds **only** the authority-gated `asmctx3` mint
and its tests. Independently verified absent from the WP4 change:

- no `usage4` construction/emission; no `CodeOccurrenceFact`;
- no scanner/F09 behavior; no coverage/refusal emission;
- no `CodeOccurrenceCoverage.Merge`/NEW-04 change;
- no persistence/native reopen; no file I/O or mutation;
- no extraction/graph-indexing change; **no `public` API addition**.

The only comment-level `usage4`/persistence mentions are documentation. WP5, WP6 and
S2.3 obligations are preserved, not treated as WP4 defects.

---

## 12. Findings and remaining limitations

| ID | Severity | Finding | Evidence |
| --- | --- | --- | --- |
| **W1** | **NON-BLOCKING** | `VerifiedTargetMint` is an internal record with a primary constructor; in-assembly code could construct a `Minted` result directly. No production path does, and external assemblies cannot (internal). The authority boundary is `MintTarget`. | code inventory §3 |
| **W2** | **NON-BLOCKING** | The frozen `CodeOccurrenceIdentity.ComputeTargetId` is a public pure constructor. It computes identifiers and claims no association authority; it is used in `src/` only by `MintTarget`. Not a bypass (accepted REM-01 helper). | §3 |
| **W3** | **QUALIFICATION (inherited)** | F1/G1: a foreign metadata-equal target instance mints the **defining member's** context. Accepted-authority-consistent; WP4 does not upgrade it to exact-instance authority. | §6 |
| **W4** | **QUALIFICATION (inherited)** | S-01: `identifierDigest`/`declarationDigest`/`shapeDigest` are caller-supplied and not independently verified. | §4 |
| **W5** | **QUALIFICATION (scope)** | Option A §5.2 test 3's `usage4` assertion and cross-profile test 6 remain WP5/G-MINT-comparison items; not claimed complete. | §8 |
| **W6** | **NON-BLOCKING** | Windows x64 only; separate-process mint determinism demonstrated on this host. | §9/§10 |

No **BLOCKING** finding; no **DECISION REQUIRED**; no unsupported hypothesis promoted.

---

## 13. Recommended owner disposition

**`SUPPORTED WITH QUALIFICATIONS`.** The evidence supports proceeding to WP4 owner
acceptance. All mandatory WP4 conditions hold: mandatory non-null recorded-`compctx2`
enforcement; fail-closed refusal for every non-`Complete` outcome; G1 identity
distinctions preserved; G2 behavior not dependent on precedence; exact frozen
`asmctx3` reproduced with independently derived expected values; separate-process
determinism; no unauthorized `usage4`/persistence/scanner/NEW-04/public API. The
qualifications (W3–W6) are inherited accepted limitations or explicit WP5/G-MINT
reservations, not new WP4 defects. No remediation is required for WP4 acceptance.

---

## 14. Final commits, trees, and working-tree state

Reviewed objects: baseline `e05ce93` (tree `7531293c…`), implementation `eac3022`
(tree `9a4ccefc…`), evidence `e05ce93`. The reviewer adds only
`docs/reviews/ANDIRIT-CI1A-S2.2-WP4-Q1.md`,
`docs/reviews/CI1A-S2.2-WP4-Q1-tests/` and
`docs/reviews/CI1A-S2.2-WP4-Q1-evidence/`. Production source, tests, frozen oracle
and accepted decisions are unchanged; the review commit/tree and final HEAD,
ahead/behind and push status are recorded in the commission handoff.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS`.** Evidence supports WP4 owner
acceptance consideration; no owner acceptance is granted, and WP5/WP6/S2.3 and
publication remain unauthorized. Stop after independent qualification and evidence
preservation.
