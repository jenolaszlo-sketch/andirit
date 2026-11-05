# ANDIRIT-CI1A-S2.2-G-MINT-Q1 — Independent pre-mint gate qualification

**Commission:** `ANDIRIT-CI1A-S2.2-G-MINT-Q1` (independent gate qualification only).
**Project:** Andirit. **Date:** 2026-11-05. **Canonical branch:** `main`.
**Gate under assessment:** `G-MINT` (status **OPEN**).
**Accepted baseline:** `4fdc23cfd3d760639dd653ad92bb204376a08c05` (tree `55b5e990cfa16606d7ba09752d18d360bf01db08`).
**Reviewer role:** independent reviewer; not the implementer and not the owner.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS FOR OWNER DECISION`.**

The pre-mint gate requirements (GM-1…GM-7) are supported by reproducible,
independently constructed evidence. No mandatory pre-mint gate condition fails and no
unresolved blocking violation was demonstrated. The remaining items are explicitly
bounded qualifications that require the owner's explicit treatment at the `G-MINT`
decision. This report does **not** grant gate acceptance, does **not** mint, and does
**not** authorize WP4/WP5/WP6 or publication.

The central question — *does the existing implementation establish sufficient
authority and refusal guarantees for subsequent minting work, without relying on
unimplemented WP4 functionality or unverified associations?* — is answered **yes,
with explicit qualifications**.

---

## 1. Baseline verification and Git ancestry

| Role | Commit | Tree | Verified |
| --- | --- | --- | --- |
| Accepted R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` | ✅ |
| G-CONFIRM independent authority | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` | ✅ |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` | ✅ |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` | ✅ |
| WP3 implementation evidence | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` | ✅ |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` | ✅ |
| WP3 owner acceptance | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` | ✅ |

- `e0d9110 → 391abbe → 812584d → ff58a92 → e673775 → 2cdcd10 → 4fdc23c` is a
  consecutive parent chain (`git merge-base --is-ancestor`, exit 0 each); every tree
  reproduced exactly.
- Working tree **clean**; one worktree (`D:/projects/Andirit`); no stashes;
  `origin/main` = `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`; `main` is **9 ahead, 0
  behind**; **nothing pushed**.
- No concurrent writer was observed moving `HEAD`, the branch or `origin/main` during
  this qualification.
- **No pre-existing G-MINT qualification or owner decision exists.** Only
  `docs/planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md` (planning) and a foundation
  owner draft exist; no `ANDIRIT-CI1A-S2.2-G-MINT-*` review or `…-g-mint-owner-*`
  decision record is present. This qualification is not a duplicate and is not
  self-certification.
- Accepted implementation and evidence are unaltered; `git diff --stat 4fdc23c HEAD -- src tests` is empty (changes here are reviewer-only).

---

## 2. Authoritative G-MINT requirements

Reconstructed from accepted documents (not from the commission summary):

- **Gate purpose and scope** — `g-mint-package.md` §1–§2, §4, §6: confirm the mapping
  preserves frozen R2 identities, keeps `compctx2` strictly separate, is
  deterministic, and enforces the association invariant **before** any canonical
  target is minted; the gate precedes WP5 minting and its verdict is reviewer-owned.
- **Pre-mint vs runtime boundary** — G-CONFIRM owner decision §8: `G-MINT` reviews the
  implemented mapper plus the pre-mint association validation; WP4 runtime extraction
  then consumes the cleared mapper. Native reopen is S2.3 and is **not** a G-MINT
  prerequisite (`g-mint-package.md` §2.7, §4).
- **Option A identity/validity separation** — target-identity decision §5.1:
  `asmctx3` from the defining R2-faithful `compctx1`; `compctx2` is mandatory
  provenance validity; `usage4` frozen; no identity churn; `compctx2` never enters
  `asmctx3`/`usage4`; durability association required for later stages.
- **R2-faithful mapping** — mapping contract §2: reuse all verified fields; the single
  substitution `compilation-context|<dep compctx2>` → frozen R2 legacy item
  `compilation|<assembly>|<metadata>|<trees>`; actual PE identities preserved;
  declared comparison profiles.
- **Association invariant** — mapping contract §3: (1) exactly one verified **live**
  defining instance; (2) complete declared universe; (3) current verified `compctx2`
  equals the recorded context; (4) `asmctx3` from the mapped `compctx1`; (5)
  association recorded separately.
- **Required falsification set** — mapping contract §7 and `g-mint-package.md` §4:
  Option A §5.2 tests 1–7 and 9–10 against the mapper and live authority/association
  checks (test 8 split by the S2.2/S2.3 boundary); dependency-bearing mapped
  `compctx1`; `compctx2` isolation; repeated/separate-process determinism; no-churn /
  identity-change pair; fabricated, incomplete, out-of-universe, duplicate, ambiguous,
  stale and substituted negatives.
- **Carried-forward obligations** — WP3 owner decision §7: GM-1…GM-7.

Expected behavior for each obligation was derived from these texts and independently
exercised; no new requirement was invented where authority is silent (notably
refusal-status precedence, which is unspecified).

---

## 3. GM-1 … GM-7 qualification matrix

Probes: `docs/reviews/CI1A-S2.2-G-MINT-Q1-tests/GMintProbes.cs` (12 tests).

| ID | Requirement (authority) | Relevant implementation | Independent evidence | Actual result | Qualification / gap | G-MINT impact | Required next action |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **GM-1** | Exactly-one verified **live** defining instance; metadata equivalence not exact-instance authority (mapping §3.1; S2.1 §5; Option A §5.1(1)–(2)) | `VerifiedCSharpUniverse.BindsDefining`, `ResolveDefining` | `Gm1_ExactLiveInstance_…`, `Gm1_ValidCallerDefiningRelationship`, `Gm1_ForeignIdentityEqual…`, `Gm1_DifferentInstanceEqualDigest…`, `Gm1_MultipleCandidates_Ambiguous`, `Gm1_SubstitutedCallerInstance_Missing` | Defining instance bound by exact live instance; foreign identity-equal target binds via metadata; unequal identity ⇒ `MissingDefiningCompilation`; two candidates ⇒ `Ambiguous`; substituted caller ⇒ `Missing` | **QUALIFICATION**: foreign identity-equal target instance is accepted (F1), matching frozen R2 `CompilationUniverse.Verify`; production direct branch is stricter than frozen (`ReferenceEquals` vs identity equality) | No block: binding authority is the live instance; metadata is not the sole authority for the defining instance | Owner acknowledges F1; optional stricter enforcement would be a new owner decision, not silently applied |
| **GM-2** | Refusal behavior / any needed precedence (stage2-boundary §5; mapping §3; Option A §5.1) | `ResolveDefining` ordering | `Gm2_RefusalConditions_Cannot_Yield_Complete_Under_Any_Overlap` (10 overlaps) | All overlapping failures refuse; only `complete+single+null` and `complete+single+valid` reach `Complete` | **QUALIFICATION**: which refusal label wins depends on evaluation order (unspecified), but mint-eligibility is order-invariant | No block: no failure combination yields a mint; labels are not authority | Consumers must treat every non-`Complete` status as fail-closed; a precedence contract is optional, not required for safety |
| **GM-3** | Mandatory non-null recorded `compctx2` equal to the current verified context (Option A §5.1(2)/(5); mapping §3.3) | `ResolveDefining(recordedCompctx2, …)` | `Gm3_RecordedContext_Matrix` (7 cases), `Gm3_Complete_Alone_Does_Not_Expose_A_Validated_Association`, `Gm3_ValidRecordedIncorrectLiveBinding_Missing` | When supplied, empty/malformed/wrong/stale/fabricated ⇒ `StaleContext`; valid+correct live binding ⇒ `Complete`; `null` ⇒ `Complete`; no result member records validation | **QUALIFICATION**: pre-mint API permits the `null` omission; enforcement capability is demonstrated. The non-null requirement is **already accepted authority** and the owner decision assigned the fix to WP4 | No block at the pre-mint gate: it enforces when a context is supplied; minting must always supply it | WP4 must always supply a non-null recorded `compctx2`; verify at the minting path |
| **GM-4** | Option A §5.2 association negatives tied to accepted outcomes (Option A §5.2; mapping §7) | mapper + resolver | `Gm4_OptionAS52_PreMint_Negatives_And_Controls` (stale/fabricated/substituted/incomplete/ambiguous/missing/external/isolation + valid control); WP3-Q1 mapper probes | All pre-mint negatives refuse; valid control accepts; no minting | **QUALIFICATION**: pre-mint falsification set (mapping §7) demonstrated; full `asmctx3`/`usage4` forms (tests 1, 3, 5) and cross-profile equality (test 6) are WP4/WP5 | No block for a gate explicitly preceding WP4 | Owner acknowledges the pre-mint scope; run the minting-path forms at WP4/WP5 |
| **GM-5** | Construction-stage external-evidence refusal (mapping §1.4, N2) | `VerifiedCSharpUniverse.Create` | `Gm5_ConstructionStage_Refusal_Boundary` | Out-of-scope unit, repository mismatch, dependency outside universe all refused at construction; recorded refusal ⇒ universe incomplete; resolver never reaches `UnsupportedExternalEvidence` | **NON-BLOCKING** (F4 preserved) | No block | Preserve; re-verify at G-MINT |
| **GM-6** | NEW-04 deferred to WP6; no reliance (G-CONFIRM owner §8; register §4) | none (not consulted) | `git grep CodeOccurrenceCoverage` in `src/Andirit.CSharp` and reviewer/producer test trees = no matches; `Merge` last changed `fc515ff`; `Gm4`/`Gm1` probes never invoke coverage | `Merge` unchanged and unused by WP3/G-MINT | **NON-BLOCKING** | No block: no unaccepted NEW-04 behavior is relied upon | Preserve deferral; correct/falsify NEW-04 at WP6 |
| **GM-7** | No authority from producer oracle / fabricated context / unvalidated substitution; `compctx1` R2-faithful; PE identities intact (Option A §5.2(10); mapping §6; `g-mint-package.md` §4) | `MapR2FaithfulCompctx1`, `ResolveDefining` | `Gm7_MappedCompctx1_EqualsFrozenAuthority…`, `Gm7_StableDigest_ForCrossProcessComparison` (two processes), `Mapper_*` (WP3-Q1) | Mapper digest equals frozen R2 authority; actual PE identities present; no `compilation-context|` remains; `compctx2` isolated; identical digest across separate processes | **QUALIFICATION**: fixture-profile anchors not re-derived here (see §9); algorithm fidelity is independently proven against the frozen authority | No block | Preserve; carry PE-profile reproduction as accepted evidence |

---

## 4. Independent negative-test evidence

Full in-memory negative/positive matrix (all before minting, no WP4 output):

| Negative / control | Input | Expected (authority) | Actual |
| --- | --- | --- | --- |
| Stale recorded context | `compctx2:sha256:000…` | refuse | `StaleContext` |
| Fabricated context (unrelated compilation) | unrelated `DescriptorDigest` | refuse | `StaleContext` |
| Empty/malformed recorded context | `""`, `"not-a-context"` | refuse | `StaleContext` |
| Wrong defining context | caller's own `compctx2` | refuse | `StaleContext` |
| Substituted live instance | caller references a distinct instance | refuse | `MissingDefiningCompilation` |
| Incomplete universe | declared-but-unresolved unit or recorded refusal | refuse | `Incomplete` |
| Ambiguous binding | two live bindings | refuse | `Ambiguous` |
| Missing defining compilation | unrelated target | refuse | `MissingDefiningCompilation` |
| External/out-of-scope unit | unit not in scope | refuse at construction | `InvalidDataException` (`ScopeSourceMembershipMismatch`) |
| Repository mismatch | universe built for another repo | refuse at construction | `InvalidDataException` |
| Dependency outside universe | app without its dependency entry | refuse at construction | `InvalidDataException` (`MissingDefiningCompilation`) |
| `compctx2` isolation | mapped `compctx1` | no `compctx2` material | none present |
| Valid association control | exact instance + correct recorded context | accept | `Complete` |
| Valid association control (no recording) | exact instance + `null` | accept (pre-mint scope) | `Complete` |

No negative was bypassed through a weaker path except the accepted F1 metadata
cross-binding (which still requires a live exact-instance caller reference) and the
accepted GM-3 `null` omission. Both are recorded as qualifications, not defects.

---

## 5. Identity and recorded-context analysis

**Which identity is required at each boundary (derived from mapping §3.1 and S2.1 §5):**

| Boundary | Required identity | Implementation |
| --- | --- | --- |
| Universe membership | exact live `CSharpCompilation` instance (`VerifiedCSharpUniverse.Create` rejects duplicate live instances) | `ConflictingLiveCompilation` |
| Direct defining binding | exact live assembly instance (`ReferenceEquals`) | `BindsDefining` direct branch |
| Cross-project defining binding | exact live `CompilationReference` instance to the entry **plus** matching metadata identity of the target's containing assembly | `BindsDefining` cross branch |
| Recorded provenance | string equality with `entry.Descriptor.DescriptorDigest` | `ResolveDefining` |
| Dependency substitution (mapping) | exact live dependency matched by verified `compctx2` digest | `MapReferenceForR2` |

**Conclusion.** The defining compilation is bound by exact live instance; metadata
identity is a consistency check on the **target's containing assembly** in the cross
branch, not the sole authority for the defining instance. Therefore a future minting
operation that consumes `ResolveDefining` cannot obtain a stronger association than
the contract authorizes from metadata equivalence alone: the defining instance must
be the caller's live reference (or the target's exact instance). The residual F1
behavior — a foreign identity-equal target symbol binds the universe member — is
accepted-authority-consistent (frozen R2 reaches the same verdict) and is preserved
as a qualification.

**Recorded-context is a precondition distinct from resolver success.** `Complete`
means "exactly one live binding resolved"; it does not mean "a recorded context was
validated". The implementation correctly refuses any supplied context that differs,
but treats `null` as "no recording to compare". The mandatory non-null recording for
minting is established by Option A §5.1(2)/(5) and mapping §3.3 — **not** invented
here — and the WP3 owner decision assigned the implementation of "always supply" to
WP4. This is a pre-mint capability plus a WP4 integration obligation, not a pre-mint
gate failure.

---

## 6. Refusal-status analysis

Observed (independently reproduced) results:

```
incomplete+stale          => Incomplete
incomplete+valid-recorded => Incomplete
incomplete+missing        => Incomplete
incomplete+ambiguous      => Incomplete
ambiguous+stale           => Ambiguous
ambiguous+null-recorded   => Ambiguous
complete+stale            => StaleContext
complete+missing          => MissingDefiningCompilation
complete+single+null      => Complete
complete+single+valid     => Complete
```

**Mint-eligibility invariant (order-independent).** `Complete` is produced **iff**
the universe is complete **and** exactly one binding exists **and** (no recorded
context **or** the recorded context equals the current verified context). Every other
combination returns a non-`Complete` status. Therefore **no refusal-status pair can
yield a mint under any evaluation order**, satisfying GM-2's alternative acceptance
condition. The *label* for a multi-failure input depends on the evaluation order,
which accepted authority does not specify; consumers must not rely on it and must
treat every non-`Complete` status as fail-closed. No precedence decision is required
for gate safety; if the owner wants labels to be contractual, that is a new decision.

---

## 7. NEW-04 deferral verification

- The deferral is explicitly authorized: G-CONFIRM owner decision §8 (NEW-04 at WP6),
  obligation register §4, S2.1 boundary §4, entry acceptance §3.8; WP3 owner decision
  GM-6.
- `CodeOccurrenceCoverage.Merge` is unchanged (last change `fc515ff`, the
  pre-correction commit) and is **not** referenced anywhere in
  `src/Andirit.CSharp` or in the WP3/G-MINT reviewer or producer test trees
  (`git grep` → no matches).
- Neither WP3 nor this gate depends on any NEW-04 behavior; no coverage-merge result is
  used to establish an association.
- The deferral is preserved and **not** claimed closed. G-MINT has no conflicting
  dependency on NEW-04.

---

## 8. Regression results and oracle integrity

Environment: Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`
(`CI1A-S2.2-G-MINT-Q1-evidence/environment.txt`).

| Suite | Command | Independent result |
| --- | --- | --- |
| G-MINT reviewer probes | `dotnet test docs/reviews/CI1A-S2.2-G-MINT-Q1-tests/ReviewerTests.csproj` | **12 / 0 / 0** |
| WP3-Q1 reviewer probes (re-run) | `dotnet test docs/reviews/CI1A-S2.2-WP3-Q1-tests/ReviewerTests.csproj` | **19 / 0 / 0** |
| Producer WP3 targeted | `dotnet test tests/Andirit.CSharp.Tests --filter FullyQualifiedName~S2_2Wp3MapperResolverTests` | **16 / 0 / 0** |
| `Andirit.CSharp.Tests` | `dotnet test tests/Andirit.CSharp.Tests` | **183 / 0 / 0** |
| Whole solution | `dotnet test Andirit.slnx` | **708 / 0 / 0** (`Andirit.Tests` 325, `Andirit.CSharp.Tests` 183, `Andirit.TreeSitter.Tests` 157, `Andirit.LatticeDb.Tests` 43) |
| Frozen R2 oracle | `dotnet run … SemanticCorpusOracle --expected oracle-expected-r2.json` | **185 / 185**, exit 0 |

Oracle integrity (independently verified):

- Frozen expected `oracle-expected-r2.json`:
  `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` ✅
- As-produced output:
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` ✅
- Separate-process mapper determinism: `GM7-MAPPER-DIGEST` =
  `compctx1:sha256:709181c6ec9eee58f2be88b8984690d7fc1991149b5f7f327e55a41a091ae7e2`
  and `GM7-LIB-DIGEST` =
  `compctx2:sha256:982e642d3146a87be43021b360bb1a356b90168c8849c0ade457eb10746ef54f`
  identical across two independent `dotnet test` processes.

A passing suite is not treated as authority; each requirement above is grounded in
contract text and an independently derived expected result.

---

## 9. Platform and evidence limitations

- **Platform.** All execution was Windows x64 (`net10.0`); no cross-platform
  qualification was performed and none is inferred.
- **Fixture-profile anchors.** The frozen fixture `compctx1` anchors
  (`Leaf = 404c5e48…`, `Core = 942a8e58…`) were independently reconstructed in the
  accepted G-CONFIRM-Q1 evidence and are compared as a frozen oracle section on
  replay. This commission independently proved **algorithm fidelity** of the
  production mapper against the frozen R2 authority (`CompilationProvenance.Describe`)
  on arbitrary inputs and confirmed actual PE identities are preserved; it did not
  re-run the test-only fixture-label alignment. This is a bounded evidence limitation,
  not a contract gap.
- **`asmctx3`/`usage4` and native persistence** are WP4/WP5 and S2.3 respectively and
  are outside the pre-mint gate.
- **Caller-supplied component digests** (`identifierDigest`/`declarationDigest`/
  `shapeDigest`) remain the accepted S2.2/S-01 obligation.
- Windows-only execution; no crash/power-loss/multi-process qualification.

---

## 10. Findings

No **BLOCKING** finding. No **UNSUPPORTED HYPOTHESIS** is promoted. Sources of
reproducible evidence are the probe file and the evidence directory.

| ID | Severity | Finding | Smallest reproducible case | Authoritative basis |
| --- | --- | --- | --- | --- |
| **G1** | **QUALIFICATION** | A foreign, identity-equal target instance binds the universe member the caller live-references; the frozen R2 authority does the same, and the production direct branch is stricter. | `Gm1_ForeignIdentityEqualTargetInstance_BindsViaMetadataIdentity`; `Gm1_DifferentInstanceEqualDigest_And_UnequalIdentity` | mapping §3.1; S2.1 §5; frozen R2 `Verify`/`Binds` |
| **G2** | **QUALIFICATION** | Refusal-status labels depend on an unspecified evaluation order, but mint-eligibility is order-invariant: no multi-failure combination yields `Complete`. | `Gm2_RefusalConditions_Cannot_Yield_Complete_Under_Any_Overlap` | Option A §5.1; stage2-boundary §5 |
| **G3** | **QUALIFICATION** | `ResolveDefining(..., recordedCompctx2: null, ...)` returns `Complete`; `Complete` is not proof of a validated recorded context. Enforcement works when a context is supplied. The non-null requirement is already accepted authority; the fix is WP4 integration. | `Gm3_RecordedContext_Matrix`; `Gm3_Complete_Alone_Does_Not_Expose_A_Validated_Association` | Option A §5.1(2)/(5); mapping §3.3 |
| **G4** | **QUALIFICATION** | The pre-mint Option A §5.2 falsification set is demonstrated; the full `asmctx3`/`usage4` forms (tests 1, 3, 5) and cross-profile equality (test 6) legitimately belong to WP4/WP5. | `Gm4_OptionAS52_PreMint_Negatives_And_Controls` | mapping §7; G-CONFIRM owner §8; WP3 owner GM-4 |
| **G5** | **NON-BLOCKING** | External/out-of-scope evidence is refused at construction; `UnsupportedExternalEvidence` is unreachable through `ResolveDefining`. | `Gm5_ConstructionStage_Refusal_Boundary` | mapping §1.4, N2 |
| **G6** | **NON-BLOCKING** | NEW-04 is correctly deferred; `Merge` unchanged and unused by WP3/G-MINT. | `git grep CodeOccurrenceCoverage` (no matches) | G-CONFIRM owner §8; register §4 |
| **G7** | **NON-BLOCKING** | No authority from producer oracle/fabricated context; mapper R2-faithful; PE identities intact; separate-process determinism. | `Gm7_MappedCompctx1_EqualsFrozenAuthority…`; `Gm7_StableDigest_ForCrossProcessComparison` | Option A §5.2(10); mapping §6 |

---

## 11. Gate readiness assessment

The `G-MINT` gate exists to confirm, **before** production minting, that the S2.2
mapping preserves frozen R2 identities, keeps `compctx2` strictly separate, is
deterministic, and enforces the pre-mint association invariant. On the independent
evidence above:

- **Ready:** R2-faithful mapper (digest-equal to the frozen authority, actual PE
  identities preserved, `compctx2` isolated, deterministic across processes);
  association invariant enforced (complete universe, exact live binding, recorded
  equality); fail-closed refusal under every overlapping negative; no reliance on
  WP4 output or unaccepted NEW-04 behavior.
- **Bounded qualifications requiring owner treatment:** G1 (F1 inheritance), G2
  (label ordering), G3 (non-null recording is a WP4 integration obligation), G4
  (minting-path §5.2 forms are WP4/WP5).

No mandatory pre-mint condition fails. The gate is ready for a formal owner decision.

---

## 12. Recommended owner disposition

**`SUPPORTED WITH QUALIFICATIONS FOR OWNER DECISION`.**

The owner decision should: (a) accept that the pre-mint gate conditions are met;
(b) explicitly preserve G1–G4 as carried qualifications; (c) require WP4 to always
supply a non-null recorded `compctx2` and to run the minting-path Option A §5.2 forms;
and (d) keep `G-MINT`'s downstream gates closed until then. This report does not grant
`G-MINT` acceptance and does not mint.

Recommended progression remains:
`G-CONFIRM → WP3 → WP3-Q1 → WP3-OWNER → G-MINT-Q1 → G-MINT-OWNER → WP4 → WP5`.

---

## 13. Commit identities and final repository status

| Object | Commit | Tree |
| --- | --- | --- |
| Accepted R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM independent authority | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` |
| G-CONFIRM owner acceptance | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 implementation | `ff58a9290a7583d4493d177e5de5aa6315904a85` | `85cdcedeb261c656893cdd7c2e3f21b741a25dc6` |
| WP3 implementation evidence | `e67377555bde8b2958292a2baf45e05908af917f` | `0a6a91e95541e28e985269879c2c2e824123fdb7` |
| WP3 independent qualification | `2cdcd10e4b225debe0d081280d8a18a6304d0475` | `a3d6dcc92b08c8080f2d2402a9d3a714b01b7409` |
| WP3 owner acceptance / G-MINT-Q1 baseline | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |

Final repository status at recording: review additions only
(`docs/reviews/ANDIRIT-CI1A-S2.2-G-MINT-Q1.md`,
`docs/reviews/CI1A-S2.2-G-MINT-Q1-tests/`,
`docs/reviews/CI1A-S2.2-G-MINT-Q1-evidence/`); production source, accepted
decisions, reviewer tests and the frozen oracle unchanged; `main` 9 ahead of
`origin/main`; **nothing pushed**.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS FOR OWNER DECISION`.** All
mandatory pre-mint conditions have sufficient independent evidence; the remaining
qualifications (G1–G4) are explicitly bounded and require owner treatment before
`G-MINT` acceptance. No gate acceptance is granted and no minting is authorized. Stop
after independent qualification and evidence preservation.
