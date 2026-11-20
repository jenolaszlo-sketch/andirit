# ADR-CI1-01-CI1A-S2.5-WP-S25-4-GATE-OWNER-01 — owner acceptance of the GATE-REMEDIATION-01 independent qualification

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-20. **Canonical branch:** `main`.
**Accepted subject:** `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01` (commit `eafd632`), independently qualified by `ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01` (commit `e005aab…`).
**Owner decisions:**
**QUAL-04 exact-byte formatting gate — ACCEPTED (CLOSED)**;
**F-G2-01 LatticeDb branch coverage gate — ACCEPTED (CLOSED)**;
**overall gate remediation — ACCEPTED WITH QUALIFICATIONS**.

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The owner accepts the independent qualification's reproduced technical results: the exact-byte
> formatting gate passes (exit 0 on a fresh byte-exact export of the pinned revision) and the
> binding LatticeDb coverage gate passes (**368/466 = 78.96% branches**, **900/969 = 92.87%
> lines**, exit 0) under the unchanged CI policy. Qualifications F-01…F-06 are preserved as
> historical producer-evidence deficiencies; they do not invalidate the independently reproduced
> results. This authorization does **not** grant WP-S25-4, Stage 2, or Stage 6 acceptance, and it
> authorizes **no** production, test, dependency, formatter, coverage, CI-policy, or remote
> operation.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, historical review artifact, or existing planning contract definition.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `e005aab8b350c8a416f250fac29c5b689142c86c` / `955a388e333eec99d5f7552b6bfabc4c7512addc` |
| Qualified remediation revision | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` / tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` — strict ancestor of HEAD |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes |
| Local vs remote | recorded `origin/main` `48370711a6247b972dfc83628618924e864be374` (unchanged since WP-S25-2); local **14 ahead, 0 behind**; no fetch performed |
| Protected scope | `git diff eafd632 HEAD -- src tests .github` **empty** — no production/test/CI change since the qualified remediation |
| Environment | Windows `10.0.26200` x64; .NET SDK `10.0.401` |

The GATE-QUALIFICATION-01 self-excluding evidence manifest (40 entries) was re-verified
post-commit: manifest ↔ worktree ↔ committed blobs byte-exact (**VERIFICATION: PASS**); the
preserved producer evidence (three artifacts) is byte-identical to the original producer
package. No reset/clean/restore/rebase/prune/fetch/push/worktree creation, and no remote
contact, were performed in this commission.

## 2. Accepted technical results (independently reproduced by GATE-QUALIFICATION-01)

| Gate | Binding command / policy | Independent result at `eafd632` | Decision |
| --- | --- | --- | --- |
| QUAL-04 exact-byte formatting | `dotnet format Andirit.slnx --no-restore --verify-no-changes` (`ci.yml` line 24), on a fresh `git archive` export with conversion disabled (4,555 tracked blobs; 0 missing/extra/mismatched) | **exit 0** (SDK 10.0.401, Windows 10.0.26200) | **ACCEPTED — CLOSED** |
| F-G2-01 LatticeDb coverage | `dotnet test tests/Andirit.LatticeDb.Tests/… -c Release --no-restore /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura '/p:Include=[Andirit.LatticeDb]*' /p:Threshold=70 /p:ThresholdType=line,branch` (`ci.yml` lines 57–64) | **exit 0**; branches **368/466 = 78.96%**, lines **900/969 = 92.87%**; numerator/denominator independently derived from the Cobertura artifact; denominator unchanged from R3 (305/466 → 368/466 with valid 466 throughout) | **ACCEPTED — CLOSED** |

Supporting preservation evidence accepted with the gates: whole solution **949 passed / 0 failed / 0 skipped** (75 LatticeDb + 157 TreeSitter + 345 core + 372 CSharp); Release build **0 warnings / 0 errors**; focused preservation receipts for R1 (`N8_D3GraphNoninterferenceAcrossOccurrenceLifecycle`), R2 (`HistoricalStorageIdentityTests.Schema2_LabelsRemainHistoricalHetuProtocolIdentities`), R3 (`OccurrenceRemediationNativeTests` ×5), Q03-F-01 (`MissingCurrentAuthority_IsUnavailable_NotEmptySuccess`), merge/identity (`L04` union, `L05` conflict, 11 `CompleteScopeMembership_*` cases); frozen R2 oracle **185/185, byte-identical** (146,851 bytes, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`).

## 3. Qualification limitations and producer-evidence findings (preserved)

The overall qualification disposition is **SUPPORTED WITH QUALIFICATIONS**. The following are
preserved as historical producer-evidence deficiencies; none invalidates the reproduced results,
which were re-established independently by the reviewer:

| ID | Finding (preserved) |
| --- | --- |
| F-01 | Producer evidence manifest is a tree listing without SHA-256 digests, below the established self-excluding SHA-256 manifest convention. |
| F-02 | Producer package contains no raw artifacts for the format-exit-0, whole-solution-949, and build-0-warnings claims (independently reproduced instead). |
| F-03 | Producer "4,553 tracked blobs" is the parent revision's count; the sealed revision has **4,555**. |
| F-04 | Producer "24 focused regression tests" — actual added cases are **23** (13 + 10); the new file's table row also reads "(14 tests)" where 13 is actual. |
| F-05 | Producer report's identity block records the parent `f669e107`/`5df749d2` as "this HEAD"/"Seal"; the sealed commit is `eafd632`/`823e3c65`. |
| F-06 | The producer report's "18/32" authority-remainder note is not derivable from any published artifact (minor). |

Reviewer-side limitations carried unchanged: all gate executions are Windows-x64 (SDK 10.0.401);
session/method independence only (no organizational-independence evidence); the C:→D: work-root
relocation and the first-attempt oracle failure (NETSDK1004, non-solution restore) are recorded
reviewer-infrastructure notes, not product findings.

## 4. Preservation of previous dispositions

All prior WP-S25-4 dispositions are preserved unchanged: R1, R2, R3 membership-accounting
remediations and their QUAL-02/03/04 independent qualifications; Q03-F-01's technical
qualification; the R1/R2/R3 accepted boundaries and qualifications (F-01, F-02, F-03, F-S254-01/02/03,
SQ-02/03, amended SQ-04 boundary); the entry-owner readiness record; the M-series matrix rows as
recorded in QUAL-04, except that this decision closes the two repository gate failures
(§2). No historical qualification report or evidence package is revised. The WP-S25-4 obligation
ledger ([`wp-s25-4-obligation-inventory.md`](../planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md))
is updated additively: F-G2-01 and the exact-byte formatting gate are marked CLOSED at the
repository level (2026-11-20) with the verified values, and the SQ-05 row records that its
satisfaction is scope-stale (§6).

## 5. F-G2-01 formal closure

**`F-G2-01 — CLOSED (repository level) under this decision`.** Basis: the independently
reproduced binding-gate result at `eafd632` (§2) — 368/466 = 78.96% branches ≥ 70% and
900/969 = 92.87% lines ≥ 70% under the unchanged `ci.yml` policy, with the denominator unchanged
and no threshold reduction, exclusion, instrumentation, or workflow change anywhere in the
remediation diff. Historical values retained: 296/450 = 65.77% (WP-S25-2), 305/466 = 65.45% (R3).
The Stage-2 acceptance decision must restate this closure together with qualifications F-01…F-06
and the Windows-only execution scope.

## 6. Exact-byte formatting gate closure

**`QUAL-04 exact-byte formatting — CLOSED (repository level) under this decision`.** Basis: exit 0
on the binding command, independently reproduced from a fresh byte-exact export of `eafd632` with
no formatter correction, Git normalization, or other transformation (§2). The prior NOT
SATISFIED / NOT WAIVED finding (exit 2, seven WHITESPACE requests) is discharged at the technical
layer by two comment-only relocations; the checkout-profile vs exact-byte divergence mechanism
recorded by QUAL-04 is respected in the reproduction. Formatter settings and CI policy are
unchanged.

## 7. Remaining obligations and closure checklist

Reconciled, not resolved, by this decision:

- [ ] **M5.4 (`Unresolved` refusal reachability) — BLOCKED.** Unchanged by the remediation; awaits its separate bounded contract-resolution commission.
- [ ] **M11.4 (cross-platform native tests pass) — OPEN.** SQ-05 satisfaction is scope-stale (52 → 75 LatticeDb tests); requires corrected-revision genuine Ubuntu qualification.
- [ ] **M14.1 (Linux CI executes the entire current expanded native suite) — OPEN.** Same corrected-revision requirement; M14.2 platform-behavior record rides the same run.
- [ ] **M5.4/M11.4/M14.1 execution authorization — NOT GRANTED.** The prepared commission [`ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01`](../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01.md) requires a separate G1-style publication-and-execution decision before any remote operation.
- [ ] **WP-S25-4 final closure / Stage-2 acceptance — NOT GRANTED** by this decision.
- [ ] **Stage 6 implementation, public Find Usages, optimization, migration/dual-write — NOT AUTHORIZED.**
- [x] QUAL-04 exact-byte formatting gate — CLOSED (§6).
- [x] F-G2-01 — CLOSED (§5).
- [x] Producer-evidence deficiencies F-01…F-06 — preserved as historical findings (§3).
- [x] R1/R2/R3, Q03-F-01, merge/identity, public-contract, and historical-refusal preservation — re-verified and accepted (§2).

## 8. Successor authorization status

**Prepared, not authorized:** `ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01`
(commission + execution plan + preparation evidence package). Its G1-equivalent publication and
execution authorization is the exact remaining owner authorization for the Linux scope (§7); it
must name the pinned revision, the new remote branch (the SQ-05 branch
`qualification/ci1a-s25-sq05-linux` remains occupied at `7457284` and must not be force-pushed),
the permitted PR operation, the unmodified workflow, and the stop conditions. Nothing else —
M5.4, WP-S25-4 closure, Stage-2 or Stage-6 — is authorized here.

## 9. Repository discipline

Only this decision document, the additive ledger update
([`wp-s25-4-obligation-inventory.md`](../planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md)),
the additive work-package-plan status note, and the prepared commission with its preparation
evidence package are added, on canonical `main`, under the single-worktree policy. No production
code, test, CI configuration, fixture, dependency, formatter setting, coverage setting,
historical review, evidence, oracle/corpus, or prior decision is modified. No fetch, push, PR,
branch change, worktree creation, or remote contact is performed. The decision commit/tree
identities and final working-tree status are reported in the handoff.