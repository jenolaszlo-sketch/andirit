# ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01 — bounded P1 delta proof under the approved canonical-forms v1 clarification

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01` (`WP-S25-3 P1 DELTA PROOF EXECUTION AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-11. **Canonical branch:** `main`.
**Starting HEAD:** `87097242d1610689c388d6814be06fef8cc89d89` (tree `d73a1f857494e7b063291f5132c08fced6670264`) — the contract-clarification commit. The delta harness, evidence package, and this report are committed on top as one local commit (no push).
**Normative basis:** canonical-forms **v1**, interpreted through the approved addendum [`s01-canonical-form-v1-clarification-01.md`](../planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-v1-clarification-01.md) (authority: [`adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`](../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md)); the original contract and all P1 evidence remain historical records.
**Final disposition:** **`S-01 P1 DELTA PROOF COMPLETE — READY FOR INDEPENDENT P2 QUALIFICATION`**.

> The R13a/R13b/R13c subcases recorded as failed in P1 against the original wording are **not claimed to have passed then**. This delta proof **freshly executes** the corrected E-3 expectations against the same canonical producer and adds the mandatory direct `compctx1` and composed-target receipts (D2/D3). All 55 delta checks pass; all regression and preservation gates are green. This is **producer evidence**: independent P2 qualification and P3 owner acceptance remain separate gates, and **S-01 is not marked satisfied**.

---

## 1. Repository preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `87097242d1610689c388d6814be06fef8cc89d89` / `d73a1f857494e7b063291f5132c08fced6670264` — exactly as commissioned |
| Branch / worktrees / stashes | `main`; exactly one (`D:/projects/Andirit`); none |
| Tracked/index state | Clean on tracked files; the only entries are the permitted new delta artifacts (recorded in `preflight.log`: 8 untracked at run time, committed with this package) |
| Local vs remote | Local `origin/main` tracking ref = `8709724…` (the previously authorized user push of the P1/clarification commits); **no fetch/push/remote operation performed by this commission**; PR #6 and the qualification branch untouched |
| Forbidden operations | None performed (no reset/restore/clean/rebase/prune/branch/worktree) |

## 2. Clarified-contract authority

Re-verified against the committed decision (SHA-256 `341a4d1a39430319799182b335d14e05e3db8d2bf488e1b1dcb7b0e0569293b2`) and addendum (SHA-256 `1c3fd5e5e8db3011d422d8ab5c6d0753bf2698470bb2bce3e849686a53754c09`):

| Condition | Verification |
| --- | --- |
| E-1 `SymbolDisplayFormat.FullyQualifiedFormat` | Approved (addendum §1); corroborated by execution (§9) |
| E-2 mode not in shape; doc-IDs uniform `@` | Approved (addendum §2); corroborated by all fresh executions (§5–§7) |
| E-3 corrected R13 (value-vs-mode vs within-family) | Approved (addendum §3); executed exactly (§5) |
| E-4 doc-IDs carry no `global::` | Approved (addendum §4); corroborated by execution (§9) |
| (1) No production change authorized | Confirmed — decision §13/§15; none made (`src/` byte-unchanged) |
| (2) No canonical-form version bump | Confirmed — decision §9; `canonical-forms v1 (clarified)` remains operative |
| (3) No digest encoding change | Confirmed — decision §9 (semantics v4, `asmctx3`/`compctx1` encodings untouched) |
| (4) No persisted identity change intended | Confirmed — the clarification aligns text to the unchanged producer |
| (5) No retroactive pass | Confirmed — P1 R13 failures remain recorded failures against the original wording; this proof is fresh evidence |
| (6) Fresh R13 delta proof required before P2 | Confirmed — decision §14; this commission executes it |

## 3. Original P1 evidence baseline (preserved)

Re-verified byte-exactly before and during this proof (all recorded in `preservation.log`):

- P1 package unchanged since `90c5fbe` (`git diff --quiet` exit 0) — evidence, harness, and report intact; **no resealing**.
- P1 evidence manifest re-hashed: **23 entries, 0 mismatches**.
- Canonical-forms v1 contract unchanged since `349136d` (`5958dd6b…`, 12,636 bytes); clarification addendum and decision unchanged since `8709724`.
- Historical results as recorded: encoding vectors 10/10; forms 17/17; matrix 15/18 with R13a/R13b/R13c **failing against the original wording**; integrated S1–S8 proof complete with qualifications.

## 4. Independent expected-form derivation

Delta harness: `CI1A-S2.5-WP-S25-3-P1-DELTA-tests/` (isolated; `AssemblyName=Andirit.CSharp.Tests`; not in `Andirit.slnx`; never touches the P1 package). Expectations derive from the clarified contract only:

- **Independent encoder (RefEnc)** re-implemented (§6 encoding: domain + 4-byte big-endian length-prefixed UTF-8 parts, SHA-256, lowercase hex) — never calls production canonicalization for expectations.
- **Encoding anchors** (hand-verified digests, cross-checked against production primitives only as comparison): `asmctx3-identifier("Write")` = `4a468c68…f61af88` with hand-written message hex `0000001261736d637478332d6964656e746966696572000000055772697465`; `asmctx3-declarations("M:Fixture.Core.Mods.Change13(System.Int32@)")` = `df577e8f…39f73bad0`; `asmctx3-target("method|0|int")` = `928f040e…7dcfef8`. All three pass in the delta run.
- **Literal forms per case** (identifier / declaration / shape) with independent digests, and **composed targets** recomputed independently from the frozen R2 primitives (`ProvenanceHashing` + the frozen descriptor) as well as via the production `ComputeTargetId` — equality between the two is itself a check.

## 5. Fresh R13a/R13b/R13c results (D1)

Executed with the **corrected E-3 relations**, through the real producer path (session → batch/sink → occurrence authority) on fresh, separately valid revisions. Each side compiled with **0 errors** (2 warnings, recorded) and produced a verified compilation and occurrence facts. Full values in `Q1-DELTA.json`; digest-verified against the independent expectations.

| Case | Pair | identifier | declaration | shape | Fresh outcome |
| --- | --- | --- | --- | --- | --- |
| **R13a** | value `Change(int)` → `Change(ref int)` | = (`cf655206…`) | **≠** `c0ba1a0a…` (`(System.Int32)`) vs `778e90ed…` (`(System.Int32@)`) | = (`928f040e…`) | **PASS** |
| **R13b** | `ref` → `out` | = (`2ac84b6a…`) | = (`b2dc1473…`, uniform `@`) | = (`928f040e…`) | **PASS** |
| **R13c** | `ref` → `in` | = (`981da0ca…`) | = (`06f27302…`, uniform `@`) | = (`928f040e…`) | **PASS** |
| (family) | `out` → `in` | = (`081b8661…`) | = (`cc548ff4…`) | = (`928f040e…`) | **PASS** |

All expected forms were declared as literals before execution (e.g. `M:Fixture.Core.Mods.Change(System.Int32@)`), and every actual component digest equals the independent RefEnc digest; every extracted fact target equals the independently composed target (checks `{pair}:declaration-relations`, `{pair}:shape-equal-and-expected`, `{pair}:fact-target-matches-composed`).

### Reconcile the original matrix (no overwrite)

| Original case | Original v1 outcome | Clarified v1 expectation (E-3) | Fresh delta outcome |
| --- | --- | --- | --- |
| R13a | FAIL (shape assertion) | `=` / `≠` (mode vs value) / `=` | **PASS** |
| R13b | FAIL (declaration + shape assertions) | `=` / `=` (within family) / `=`; context distinguishes | **PASS** |
| R13c | FAIL (declaration + shape assertions) | `=` / `=` / `=`; context distinguishes | **PASS** |

`R13a/R13b/R13c — SUPPORTED BY NEW DELTA EVIDENCE UNDER CANONICAL-FORMS V1 (CLARIFIED)`. The original 15/18 remains an accurate historical outcome; the updated contract-level evidence = 15 inherited passing rows + 3 newly demonstrated rows (inherited and fresh evidence are distinguished throughout).

## 6. Defining-compilation context comparisons (D2 — mandatory direct `compctx1` proof)

For each revision pair: same logical source layout and project identity, same compiler configuration, same references (see isolation below); the changed source bytes are restricted to the defining project's `Core/Targets.cs` (the consumer call-site edit is mechanically necessary for validity and is disclosed per pair in `changedInputFiles`).

| Pair | `compctx1` A | `compctx1` B | Differ |
| --- | --- | --- | --- |
| R13a | `compctx1:sha256:58aa6f32…` | `compctx1:sha256:2da03571…` | ✓ |
| R13b | `compctx1:sha256:a274cbb0…` | `compctx1:sha256:4cd1f6c2…` | ✓ |
| R13c | `compctx1:sha256:95c8bc7d…` | `compctx1:sha256:37588d32…` | ✓ |
| out→in | `compctx1:sha256:b936f85d…` | `compctx1:sha256:f1e91bc8…` | ✓ |

**Isolation (per pair, all equal A↔B):** `projectUnitId` (`csharp:project:bffa11e9…`), `assemblyName` (`Core`), `metadataIdentity` (`Core|0.0.0.0||`), `moduleName` (`Core.dll`), `optionsDigest` (`8428dcbc…`), references (179 identity entries, digest `b8cdb1ce…` — the host build closure, identical within this process; recorded per side). **Only the source identities differ**, and exactly one file changes in the defining compilation: `Core/Targets.cs` (per-side content SHA-256 recorded, e.g. R13b: `8192db21…` vs `56f84140…` in the receipt). Language version `CSharp14` recorded per side in the canonical source form. This isolates the modifier change as the cause: no reference-closure, options, or configuration difference contributes (O-1 discipline honored — the intended driver is the modifier-driven source change alone).

**Frozen cross-check:** on both sides of all pairs, the production `compctx1` mapping equals the frozen R2 recomputation field-for-field (`descriptorAgrees`, `sourcesAgree`, `referencesAgree`, `optionsAgree`, `metadataAgrees` all true), and the `CompilationDescriptor` is recomputable from its own recorded fields (P2 can re-verify).

## 7. Composed target identity comparisons (D3)

Composition per v1 §6 (domain `asmctx3-context` over `["compctx1", compctx1, metadataIdentity, moduleName, idDigest, declDigest, shapeDigest]`), computed three ways: production `ComputeTargetId`, frozen-primitive hash, and the delta RefEnc — all three agree per side (checks `{pair}:composed-agrees`).

| Case | Component digests A/B | Context A/B | Composed target A | Composed target B | Differs |
| --- | --- | --- | --- | --- | --- |
| **A — declaration distinction (R13a)** | decl ≠ (others =) | differ | `asmctx3:sha256:0a7d99ac…` | `asmctx3:sha256:c1a369c5…` | ✓ (declaration + context) |
| **B — component equality (R13b)** | all = | differ | `asmctx3:sha256:af61f132…` | `asmctx3:sha256:391eb048…` | ✓ (**context alone**) |
| B (R13c) | all = | differ | `asmctx3:sha256:0e2bce04…` | `asmctx3:sha256:b4795e08…` | ✓ (context alone) |
| B (out→in) | all = | differ | `asmctx3:sha256:b72d1e0d…` | `asmctx3:sha256:c2bc061e…` | ✓ (context alone) |

The exact encoded target message bytes are preserved (hex, 698 chars for the R13b sides — 7 length-prefixed parts). Every extracted occurrence fact's target equals the composed target both sides. **Case B demonstrates precisely the clarified guarantee:** identical identifier/declaration/shape digests with different defining contexts and **different composed targets** — the result follows from the actual accepted composition, digest-verified, not assumed. O-1 preserved: the composed target is not claimed invariant when components are invariant; the pair's target difference is proven to come from the modifier-driven context change (§6 isolation).

## 8. Valid-language and collision assessment

- **Language restriction (executed):** a modifier-only overload set does not compile — `ref`+`out`: error **CS0663** ("'BadMods' cannot define an overloaded method that differs only on parameter modifiers 'out' and 'ref'"); `ref`+`in`: error **CS0663** ("…'in' and 'ref'"). Recorded with messages in `languageBoundaries`.
- **Coexistence challenge (executed):** the only legal mode-bearing overload pair — value vs `ref` in ONE compilation (`Both.Ping(int)` + `Both.Ping(ref int)`) — compiles with 0 errors; both occurrences are facts; within the **same** context (`compctx1:sha256:0d1a7868…`): identifier digests equal (`bf8cb43b…`), declaration digests differ (`71d85f79…` vs `06d649bf…`), shape digests equal (`928f040e…`), and the composed targets differ (`e4157977…` vs `9f156d6b…`). No supported in-compilation relationship bypasses the clarifying mechanism.
- **Limits (recorded):** this is a finite, bounded case set plus the demonstrated language restriction; **no universal collision-freedom or universal C# symbol-uniqueness claim is made**. No concrete supported counterexample was found.

## 9. Editorial correction verification (E-1 / E-4)

- **E-4:** the previously accepted explicit-interface case re-checked on the frozen corpus: `Corpus.Core.IGreeter.Greet(string)` doc ID = `M:Corpus.Core.IGreeter.Greet(System.String)` (exact literal match; **no `global::`**); the explicit implementation doc ID is `M:Corpus.Core.ExplicitGreeter.Corpus#Core#IGreeter#Greet(System.String)` — consistent with P1 FC-14. No documentation ID contains `global::`.
- **Display-vs-doc-ID distinction:** type display under `SymbolDisplayFormat.FullyQualifiedFormat` = `global::Corpus.Core.IGreeter` (carries `global::`); parameter displays = `string` (special-type keyword) and `global::System.Collections.Generic.Dictionary<string, int>` — both literal-verified, corroborating E-1 and the fallback path remaining display-based (§4 example fix therefore editorial).
- **Observation D-1 (recorded, non-blocking):** `FullyQualifiedFormat` renders **member** symbols with minimal member options (a method displays as `Greet`); the §4 fallback formula (`Kind:display`) is followed exactly by production, but no supported occurrence target reaches the fallback in this Roslyn generation — local functions and explicit-interface implementations both receive documentation IDs (local function observed: `M:Fixture.App.Consumer.Local(System.Collections.Generic.Dictionary{System.String,System.Int32})`). No identity consequence; noted for future fallback-sensitive review.

## 10. Regression receipts

All gates executed by `run-delta.ps1`; exit codes in the logs:

| Gate | Result |
| --- | --- |
| Delta harness build | exit 0 |
| **Delta checks** | **55/55, `allPass=true`** (4 revision pairs + coexistence + boundaries + editorial + encoding anchors) |
| Release build (`Andirit.slnx`) | exit 0 |
| **Full solution tests** | **907 passed, 0 failed** (LatticeDb 52 · TreeSitter 157 · core 345 · CSharp 353) — unchanged baseline |
| Focused historical/refusal tests | 8 passed, 0 failed |
| **Frozen R2 oracle** | **185/185**; artifact byte-identical: 146,851 bytes, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` |
| Formatting (`dotnet format --verify-no-changes`) | exit 0 |
| Preservation | P1 package unchanged; v1 unchanged; addendum/decision unchanged; P1 manifest 23/0 |

Delta case counts are **separate** from the inherited 907 tests. No CI changes; no coverage remediation; no earlier package resealed.

## 11. Evidence integrity

- Package: `docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-evidence/` — `preflight.log`, `normative-inputs.log`, `preservation.log`, `build.log`, `Q1-DELTA.json` + `Q1-DELTA.log`, `toolchain.log`, `release-build.log`, `full-solution-tests.log`, `focused-historical-tests.log`, `oracle.log` + `oracle-r2.json`, `format.log`, `receipt-summary.log`, `reviewed-head.txt`, `manifest.json`, `.gitattributes`/`.gitignore`.
- **Manifest: 17 entries, 0 mismatches**, complete coverage (no unlisted/extra files), self-excluding SHA-256 over exact bytes; `reviewed-head`/tree = starting `8709724…`/`d73a1f85…`.
- Provenance categories (per commission §14): original normative contract (v1, historical); approved prospective clarification (addendum); historical P1 evidence (unchanged); **fresh P1 delta execution** (this package); independent expected values (delta RefEnc, frozen primitives, literal forms); production actual values (extraction digests, mapped descriptor, `ComputeTargetId`); no unverified claims asserted. A producer-created delta oracle **is not** independent P2 qualification.

## 12. Findings and qualifications

- **No blocking findings; no mandatory failure.** All corrected R13 cases pass, context and composed-target receipts pass, editorial corrections corroborated, regressions green.
- **Observation D-1** (§9): member-symbol display nuance — non-blocking, no reachable supported fallback case, recorded only.
- **Qualifications:** (a) producer-created evidence — independent P2 must re-derive expectations and re-verify descriptor recomputability; (b) finite bounded case set (no universal collision-freedom claim); (c) composed-target values are host-build-closure-scoped (O-1 preserved; the pair proofs use a single process and record the identical 179-entry reference closure); (d) F-02 boundary and all carried qualifications remain unchanged; **F-G2-01 remains OPEN** (untouched).
- **R13 delta fixtures are fresh** (`Change…`/`Both…` fixtures distinct from P1's `Change13…` fixtures) — this evidence stands alone rather than depending on P1 fixture reuse.

## 13. Formal delta disposition

**`S-01 P1 DELTA PROOF COMPLETE — READY FOR INDEPENDENT P2 QUALIFICATION`.**

Conditions: (1) all corrected mandatory R13 cases pass ✓ (incl. the family pair); (2) independent expected values agree with production ✓; (3) direct defining-context distinction demonstrated ✓; (4) relevant composed targets differ as required ✓ (declaration and/or context, digest-verified); (5) no mandatory supported semantic collision identified ✓ (coexistence separated by declaration; mode-family non-coexistence demonstrated by CS0663); (6) editorial corrections corroborated ✓; (7) existing P1 evidence intact ✓; (8) regression gates pass ✓; (9) evidence sealed and reproducible ✓.

**S-01 is not satisfied.** P2 independent qualification (against `canonical-forms v1 (clarified)`, including this delta evidence) and P3 owner acceptance remain separate, unauthorized gates.

## 14. P2 readiness recommendation

- **Reproduce:** `powershell -NoProfile -ExecutionPolicy Bypass -File docs/reviews/CI1A-S2.5-WP-S25-3-P1-DELTA-tests/run-delta.ps1` (Windows x64, .NET SDK 10.0.401, Roslyn 5.9.0, NuGet restore). Then re-verify the manifest and `oracle-r2.json`.
- **Independently re-derive:** the literal form table and digests with the reviewer's own encoder (the three anchors are the starting point); **recompute `compctx1` from the recorded descriptor fields** (contract version, repository, project unit, assembly, metadata identity, module, options digest, source canonical forms, references) and check it against the recorded digest — the descriptor is recomputable by construction; re-run the composed-target composition with the reviewer's own implementation and compare to the recorded targets within a fresh run.
- **Known limitations to disclose at P2:** producer-created oracle (not independent by construction); host-closure-scoped composed targets (O-1); finite case set; observation D-1; the R13 reconciliation must reference the clarified addendum, never the original wording.
- **Not authorized here:** P2 itself, P3, WP-S25-4, Stage-2 acceptance, any production/contract/CI change.
