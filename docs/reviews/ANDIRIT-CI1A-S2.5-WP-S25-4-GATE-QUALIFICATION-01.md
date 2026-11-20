# ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

**Date**: 2026-11-20
**Subject**: independent, bounded qualification of `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01` (QUAL-04 exact-byte formatting + F-G2-01 LatticeDb branch coverage)
**Sealed remediation revision**: `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` (tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d`, parent `f669e107` / `5df749d24bdf1973f7da3c66e75d83f07affc139`)
**Overall disposition**: **SUPPORTED WITH QUALIFICATIONS** — exact-byte formatting gate **SUPPORTED**; F-G2-01 coverage gate **SUPPORTED**.
**Commission class**: qualification only. No remediation implemented, no tests changed, no production code modified, no push, no owner acceptance.

---

## 1. Preflight and binding

| Item | Value |
| --- | --- |
| Repository | `D:\projects\andirit`, branch `main` |
| Remediation revision (expected) | `eafd632` — **confirmed**: commit `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` |
| Tree | `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` |
| Parent | `f669e107e499986c141f4bf1724b8aa93cf13fc3` (QUAL-04 review commit; tree `5df749d24bdf1973f7da3c66e75d83f07affc139`) |
| Worktree | clean (`git status --porcelain` empty), one worktree, zero stashes |
| Remote divergence | `HEAD...origin/main` = **ahead 13, behind 0**; recorded `origin/main` `48370711a6247b972dfc83628618924e864be374` (unchanged since QUAL-04) |
| Governing contracts | `.github/workflows/ci.yml` (format gate line 24; LatticeDb coverage gate lines 57–64); `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-qualification-matrix.md`; `wp-s25-4-execution-plan.md`; `s01-canonical-form-contract.md`; `wp-s25-4-obligation-inventory.md` |
| Prior qualification record | `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04.md` + `-evidence/` (R3: format exit 2 with 7 WHITESPACE requests; 305/466 = 65.45% branches, exit 1; suite then 52 LatticeDb / 926 total) |
| Producer evidence | `C:\Windows\Temp\opencode\gate-remediation\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01\` — imported verbatim into `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/` |

The producer report's own identity block records `f669e107`/`5df749d2` as "this HEAD" and as the
commit "Seal"; those are the parent revision's identities — the sealed revision is `eafd632`/
`823e3c6`. This qualification binds `eafd632` (finding F-05; the producer's "4,553 tracked blobs"
is likewise the parent's count — F-03).

Independence: the reviewer session had no role in the remediation implementation, producer
verification, or producer-evidence construction. Expectations are derived from the CI contract
and the QUAL-04 record, not from the producer's conclusions. Organizational independence is
not established (no employment/reporting-line evidence), the same boundary recorded by QUAL-04.

## 2. GQ-01 — Change scope: PASS

`git diff eafd632^ eafd632` touches exactly six files:

| File | Δ | Class |
| --- | --- | --- |
| `src/Andirit.CSharp/CSharpProjectDiscovery.cs` | +1/−1 | production — comment-only relocation (comment moved above the `Loose` factory) |
| `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` | +4/−4 | production — comment-only relocation (A-01 comment moved above the parse-options validation block) |
| `tests/Andirit.LatticeDb.Tests/LatticeCodeGraphStoreTests.cs` | +7/−1 | test — `Store_AcceptsTuningOptions` widened to set `PageSize`/`EnableAdjacencyCache` in addition to `CacheSizeMb`/`EnableWal`; assertion unchanged |
| `tests/Andirit.LatticeDb.Tests/LatticeDbDurableStateRegressionTests.cs` | +500 | test — new; 13 cases |
| `tests/Andirit.LatticeDb.Tests/NativeGraphMirrorTests.cs` | +332 | test — additive; +10 cases, zero deleted lines |
| `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01.md` | +192 | documentation |

- **Production changes are limited to the two reported comment relocations.** Both types are
  `internal`; no identifier, literal, operand, or branch is added, removed, or reordered; the
  moved bytes are the two comments' text.
- **Test changes are within the authorized coverage remediation scope** (23 added cases in
  `tests/Andirit.LatticeDb.Tests`, using public store and `LatticeDbSharp` APIs; no production
  visibility expansion, no private reflection).
- **No contract, formatter-setting, exclusion, instrumentation, threshold, or CI-policy
  changes**: the diff contains no `.editorconfig`, no `Directory.Build.props`/`.targets`, no
  project file, no `.github/workflows/ci.yml`, no coverlet filters, no thresholds.
- **Existing assertions are not weakened**: test deltas are strictly additive (`numstat`
  500/0 and 332/0 for the two test files); the one modified test widens inputs only.

## 3. GQ-02 — Exact-byte formatting: SUPPORTED

Method (independent, no producer inputs): fresh export of the pinned blobs with
`git -c core.autocrlf=false archive` + Python `tarfile` extraction — bypassing the worktree's
`core.autocrlf=true` — then the independent blob verifier over every tracked entry.

| Item | Value |
| --- | --- |
| Export identity | tar 169,031,680 bytes, SHA-256 `DBF8CAA1CE84DD1AD9609A0ABE03707A8FAB33E8823BD7FBB14625E61CC5A231` |
| Blob verification | `git entries=4555 exported=4555 missing=0 extra=0 mismatched=0` (exit 0) |
| Binding command (CI line 24) | `dotnet format Andirit.slnx --no-restore --verify-no-changes` |
| Environment | SDK 10.0.401 (Commit `e34a38d2ae`), Windows 10.0.26200 x64, PowerShell 5.1.26100.9818, Python 3.14.8 |
| **Exit code** | **0** — PASS |

The exit-0 root cause is the seven comment-adjacent whitespace normalization requests that
QUAL-04 recorded at R3: with the comments relocated out of the argument-list flow, Roslyn no
longer regenerates blank-line trivia endings, and `--verify-no-changes` finds nothing to
rewrite. No formatter correction, Git normalization, or transformation of any kind was applied
by the reviewer.

## 4. GQ-03 — F-G2-01 coverage: SUPPORTED

The binding CI LatticeDb gate was re-executed verbatim on the same pinned export:
`dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Release
--no-restore /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
'/p:Include=[Andirit.LatticeDb]*' /p:Threshold=70 /p:ThresholdType=line%2cbranch`.

| Metric | R3 baseline (QUAL-04 package) | `eafd632` reviewer run | Binding threshold |
| --- | --- | --- | --- |
| Branches covered / valid | 305 / 466 (65.45%) | **368 / 466 (78.96%)** | ≥ 70 branch — **PASS** |
| Lines covered / valid | 843 / 969 (86.99%) | **900 / 969 (92.87%)** | ≥ 70 line — **PASS** |
| Gate exit code | 1 | **0** | 0 — **PASS** |

Independent derivation (`coverage-derivation.json`, `parse-coverage.py`): the generated
artifact declares `branches-covered=368 branches-valid=466 line-rate=0.9287
branch-rate=0.7896`; the re-derivation from `<line condition-coverage>` sums gives condition
totals 736/928 whose halved covered sum (368) matches the numerator exactly.

- **Numerator/denominator**: covered 368 = 736/2 (exact); valid 466 vs line-derived half 464
  — a constant +2 coverlet granularity offset that is **identical in the R3 baseline artifact**
  (305/466 root vs 305/464 half), i.e. pre-existing and not introduced by remediation.
- **No manipulation / no denominator reduction**: denominator unchanged (466 → 466); class set
  identical (24 classes both revisions); no filter, exclusion, instrumentation, or threshold
  change in the diff. Covered-branch delta +63, valid delta 0; line-covered delta +57.
- **Differences from the reported 368/466**: none — the reviewer-generated artifact and the
  producer artifact declare identical root attributes and rates. Per-class condition deltas
  concentrate in `<CheckOccurrenceAuthorityAsync>` (10→74 covered conditions),
  `NativeGraphMirror` (120→154), and `<FindOccurrencesAsync>` (6→8) — exactly the areas the
  report names, confirming the previously uncovered branches correspond to the new tests.

**New-tests review** (meaningfulness, determinism, correspondence): assertions are behavioral
outcomes through public API — availability states (`AnalysisUnavailable`, `StalePublication`,
`CorruptedIndex`, `IncompleteAnalysis`), corrected coverage state (`NotProduced` → `Partial`),
refusal-reason substrings, exception types (`CodeOccurrenceIndexException`,
`InvalidDataException`, `ArgumentNullException`), and aggregate single/empty assertions. The
durable tampering is genuine (native `LatticeDbSharp` write transactions on real stores), with
no production visibility expansion. The tests are deterministic (fixed clock, GUID-keyed temp
databases, no time/network dependence). Suite census: 52 cases at the parent → **75 at
`eafd632`** (13 + 10 = 23 added; the producer's "24" is off by one — F-04).

## 5. GQ-04 — Regression and preservation: PASS

| Gate | Result |
| --- | --- |
| Release build (`dotnet build Andirit.slnx -c Release --no-restore`) | exit 0, **0 warnings, 0 errors** |
| Whole solution (`dotnet test Andirit.slnx -c Release --no-restore --no-build`) | exit 0; **949 passed, 0 failed, 0 skipped** = 75 LatticeDb + 157 TreeSitter + 345 core + 372 CSharp |
| Focused preservation (13 cases: `CompleteScopeMembership_*` ×11, `MissingCurrentAuthority_IsUnavailable_NotEmptySuccess`, `L04_IdenticalRealFactIdentity…UnionToOneFactTwoContributors`) | exit 0, 13/13 passed |
| Focused preservation (7 cases: `N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`, `HistoricalStorageIdentityTests.Schema2_…`, `OccurrenceRemediationNativeTests` ×5) | exit 0, 7/7 passed |
| Frozen R2 semantic oracle | **185/185, exit 0, byte-identical**: 146,851 bytes, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` (matches the frozen output sealed by QUAL-03/QUAL-04) |

- **R1/R2/R3 qualified behavior**: R1 noninterference (`N8_D3GraphNoninterference…`), R2
  legacy identity (`HistoricalStorageIdentityTests.Schema2_LabelsRemainHistoricalHetuProtocolIdentities`),
  R3 complete-empty/incomplete/availability mapping (five `OccurrenceRemediationNativeTests`),
  Q03-F-01 occurrence availability (`MissingCurrentAuthority_IsUnavailable_NotEmptySuccess`) —
  all pass on the sealed revision.
- **Merge and identity semantics**: contributor union and conflict/refusal (L04/L05),
  `IndexingLifecycle_RemovesProjectUnitWhenSolutionMembershipChanges`, and the 11
  complete-scope membership cases (1/2/3 complete, complete-empty, mixed, missing members,
  legacy reopen, replacement restore) pass; the oracle byte-identity preserves the
  `usage4`/`asmctx3` identity encoding surface bit-for-bit.
- **Public contracts and historical refusal behavior**: the diff cannot alter a public
  surface (internal comment relocations); the schema/namespace/page-binding/stale-publication
  refusal suite (`S24SurfaceRefusalMapping`, native refusal paths) passes; the identity
  encoding is oracle-pinned.

## 6. GQ-05 — Evidence integrity: PASS WITH QUALIFICATIONS

- **Producer manifest**: `evidence/MANIFEST.txt` is a directory tree listing only — it carries
  **no SHA-256 digests**, so it does not meet the repository's established self-excluding
  SHA-256 manifest convention (QUAL-04/R3 style). The reviewer rehashed the imported artifacts
  (digests in `PRODUCER-EVIDENCE.md`); nothing was reconstructed or edited. (F-01)
- **Reconciliation against raw artifacts**: the producer's own two artifacts support its
  coverage (368/466) and test (75/75) claims fully. Its format-exit-0, whole-solution-949 and
  build-0-warnings claims have **no supporting raw artifacts in the package** (no format log,
  no whole-solution TRX, no build log, no SDK record, no export-verification record); those
  claims are unverifiable from producer evidence and were independently reproduced instead
  (all three hold). (F-02)
- **Producer vs reviewer counts**: "4,553 tracked blobs" vs actual **4,555** entries at `eafd632` (4,553 is
  the parent's count — F-03); "24 focused regression tests" vs 23 actual, and "(14 tests)" vs
  13 for the new file (F-04); report identity block records the parent as "this HEAD"/"Seal"
  (F-05); the "18/32" authority-remainder note is not derivable from published artifacts
  (F-06, minor).
- **Preservation with separation**: producer artifacts imported byte-verbatim into
  `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01-evidence/` (with
  `PRODUCER-EVIDENCE.md` provenance); reviewer evidence is the sibling
  `…-GATE-QUALIFICATION-01-evidence/` package sealed by a self-excluding SHA-256 manifest.
- **Historical evidence**: QUAL-04/R1–R3/QUAL-01…03 and planning records are tracked files,
  untouched by the remediation diff and by this commission.

## 7. Disposition

| Scope | Disposition |
| --- | --- |
| **QUAL-04 exact-byte formatting gate** | **SUPPORTED** — independently reproduced exit 0 on a byte-exact export of `eafd632` (SDK 10.0.401) |
| **F-G2-01 LatticeDb branch coverage gate** | **SUPPORTED** — independently reproduced 368/466 = 78.96% ≥ 70% under the unchanged CI policy; numerator/denominator independently derived; denominator unchanged |
| **Overall commission** | **SUPPORTED WITH QUALIFICATIONS** — qualifications F-01…F-06 (producer manifest convention, missing producer artifacts for exit-code claims, three producer count/identity inaccuracies, one undocumented derivation note) |

Passing this commission does **not** constitute WP-S25-4 owner acceptance or Stage 2.5 closure,
and it does not refresh the Linux rows: M5.4 remains BLOCKED (unchanged), and M11.4/M14.1
remain BLOCKED and now scope-stale, because the LatticeDb suite grew from 52 to 75 cases and
the genuine-Ubuntu receipts cover the old suite. See `obligations.md` for the full
reconciliation and the recommended next bounded commission (owner decision on F-G2-01/format
disposition; corrected-revision genuine-Ubuntu qualification; narrow M5.4 route; then WP-S25-4
closure).

## 8. Evidence seal

This report and `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence/` (self-excluding
SHA-256 `manifest.json`; 40 entries, 1,927,786 bytes; package `.gitattributes` `* -text` and
package `.gitignore` `!*` per the established evidence-package convention, so committed blobs,
working bytes and manifest digests are byte-exact) are committed locally on `main`; the sealed
commit identity is supplied in the commit record rather than embedded here, per the
established anti-self-reference convention. No fetch, push, remote execution, or historical
evidence modification was performed.