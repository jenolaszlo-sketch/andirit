# ADR-CI1-01-CI1A-S2.3-WP-S23-2-OWNER-01 — Qualified owner acceptance of the WP-S23-2 durability gate

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-2-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`WP-S23-2 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`S23-DUR` / WP-S23-2 (defining-`compctx2` native carrier + reopen enforcement) ACCEPTED within the qualified execution scope below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The WP-S23-2-Q1 F-01 publication-divergence defect is closed by the R1
> remediation and independently requalified by Q2. Qualifications Q1-Q1,
> Q1-Q2, S-01, and the non-blocking O-Q2-01 observation remain binding.
> WP-S23-3 is **not** authorized by this decision and further work requires a
> separate owner authorization.

This is an additive owner decision. It changes no production source, tests, reviewer evidence, R1/R2 oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Authority, exact baseline, and ancestry

Controlling authority: the [S2.3 entry owner decision](adr-ci1-01-ci1a-s2-3-entry-owner-01.md), the [NEW-01 owner decision](adr-ci1-01-ci1a-s2-3-new-01-owner-01.md), the [WP-S23-2 work-package row](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md), the [native reopen test-8b gate](../planning/adr-ci1-01-ci1a-s2-3/native-reopen-test-8b-gate.md), the [S-01 canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md), and the independent [Q1](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q1.md), [R1](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-R1.md), and [Q2](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-2-Q2.md) reports.

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Accepted NEW-01 owner baseline / implementation parent | `b576acb004747c89d257d8e61721c17babe6c5a6` | `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` |
| WP-S23-2 implementation (IMPL-01) | `51369b3b463b6a2f137ee3475b78c9cd43f4db85` | `2deec87bca15b24133be51263a7c9ea137b5b978` |
| Q1 reviewed HEAD (F-01 defect baseline) | `d94052d2fa8dd14ecbfcc6337b4a262e8954ba85` | `53966a01b69c876bfff29a60c576c595bdb9e84b` |
| **R1 production remediation (accepted)** | **`1bce70ee2f52ed5100a2e68bc004462f9824d2f2`** | **`a6a908ab69740e4ee114e845cdde9374975dc15a`** |
| R1 handoff docs commit | `0bc509164363013899fa5ddbf825dd7086c9c5df` | `d3840933d6c19f46a20889a9b454b0d06c2c1c14` |
| **Independent Q2 requalification (accepted)** | **`0888be98f0fd50241f8f2bf22ee23a87b5c428f9`** | **`2d3068283c39bc6f9b091f52a5a8504f3cd6f19c`** |

The reviewed chain is consecutive and exact, verified from Git objects: `d94052d` → `1bce70e` → `0bc5091` → `0888be9`. Git SHA-1 commit/tree identifiers are distinct from the SHA-256 file digests used below.

The owner began on clean canonical `main`, at `0888be9`, one worktree (`D:/projects/Andirit`), no stashes, 30 ahead / 0 behind local `origin/main`. No fetch, branch, additional worktree, reset/restore, push, or publication was performed.

## 2. Change scope and evidence integrity

The accepted R1 production commit changes exactly one file, `src/Andirit.LatticeDb/LatticeCodeGraphStore.OccurrenceAuthority.cs` (+32 lines), and adds two test files (`tests/Andirit.CSharp.Tests/S23OccurrenceOwnershipRegressionTests.cs` and additive fixture helpers). There is **no** `src` change between `1bce70e` and the Q2 HEAD `0888be9` (`git diff --name-only 1bce70e 0888be9 -- src tests` is empty). The Q2 review itself is documentation-and-evidence only and modified no production source or test.

Evidence integrity was independently re-verified by the owner over exact Git bytes:

| Evidence | Files | Result |
| --- | --- | --- |
| R1 evidence manifest (`0bc5091`) | 94 | all bytes/lengths/SHA-256 match; 0 mismatches. Manifest SHA-256 `5B17D4A1BB9F5F297FA552999E6637F5833B1E6406A28B27C57D4AED63B9BBCA` |
| Q2 evidence manifest (`0888be9`) | 196 | all bytes/lengths/SHA-256 match; 0 mismatches. Manifest SHA-256 `A4514D4613473578BFA8FBD698602118487E64A151C5A02D5FB6594B61972650` |

The Q1 report and evidence, the R1 report and evidence, and the Q2-owned probes were treated as historical artifacts and were not altered. The producer handoff that previously occupied the Q2 report path is preserved additively as `CI1A-S2.3-WP-S23-2-Q2-evidence/producer-q2-handoff.md`.

## 3. Owner verification and closure of the F-01 defect

Owner verification used Windows x64 10.0.26200, .NET SDK `10.0.401`, runtime `10.0.12`. Re-run gates at `0888be9` before adding this decision:

| Gate / command | Owner result |
| --- | --- |
| `dotnet test Andirit.slnx -c Release --no-restore` | 834 passed, 0 failed, 0 skipped: native 50 + TreeSitter 157 + core 345 + CSharp 282 |
| `dotnet test docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj -c Release --no-restore` | 17 passed, 0 failed, 0 skipped |
| Frozen R2 oracle replay with explicit `--expected` | 185/185, 0 failed; 146851 output bytes; SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| `dotnet build Andirit.slnx -c Release --no-restore` | 0 warnings, 0 errors |
| `dotnet format Andirit.slnx --verify-no-changes --no-restore` | Exit 0 |
| Owner fresh-process F-01 reproduction (Q2 probe, temporary database) | publish `Available`/2/complete → remove only the `HetuUnit|…Client.csproj` row → separate reader process `CorruptedIndex`/0 hits/0 examined/not complete with meaningful reason; 3 distinct PIDs; exactly the one graph-owner row changed |

The owner reproduced the original Q1 counterexample independently rather than relying on producer or Q2 claims, and confirmed the corrected refusal. Q2 additionally re-executed the unchanged Q1 56-invocation matrix with Q2-owned outputs: 54/56 predicates met, 0 strict failures, with the F-01 probe `P36` now passing.

## 4. Accepted behavioral guarantees

Within the qualified execution scope, the owner accepts the following, evidenced by the independent Q2 results and the owner re-check:

1. **Orphan refusal.** Authority-bearing occurrence units whose accepted published graph owner is absent are refused, not served as current occurrences.
2. **Mandatory association.** A published graph unit requiring occurrence authority cannot silently pass without its occurrence association; a missing or internally inconsistent association is refused.
3. **F-01 closed across durable reopen.** The refusal is produced by reconstruction from persisted state across separate process lifetimes, not by a process-local or query-only filter; refused reads do not modify durable rows.
4. **Retirement correctness.** Accepted staged deletion retires the graph owner and its occurrence contribution together; unaffected owners remain queryable, and a retired target under complete repository coverage correctly returns `Available` with zero hits.
5. **Complete-empty vs unavailable/corrupted.** A genuinely complete result with zero occurrences remains `Available`/complete and distinguishable from `AnalysisUnavailable` and from corrupted or orphaned state.
6. **Preservation.** Unaffected published graph and occurrence facts are conserved, and the accepted recovery, stale-context, conflict, coverage/refusal, native-mirror, D3/D4/D5, and frozen-identity guarantees remain intact within the demonstrated scope.

## 5. Explicit acceptance boundary

Acceptance is limited to the serialized native store on **Windows x64** with the accepted SDK/runtime, the demonstrated durable-reopen lifecycle, occurrence/graph owner correspondence, coverage and refusal accounting, and the existing known-failure recovery paths. It does **not** extend to crash consistency, abrupt-kill or power-loss recovery, arbitrary-crash points, cross-platform correctness, unrestricted concurrency, or multiple writers.

## 6. Remaining qualifications and deferred findings

- **Q1-Q1 (binding).** Occurrence facts are not stored in segment records; cross-process segment-resumption occurrence reconstruction remains unqualified. WP-S23-3 must re-stage/re-verify or obtain separately authorized durability work; it must not rely on graph-only segment resumption for occurrence correctness.
- **Q1-Q2 (binding).** Recovery-rebuild internal-failure atomicity remains unqualified; no stronger failure-atomicity or quarantine claim is accepted.
- **S-01 (deferred).** Caller-supplied component-digest canonicalization/proof remains an S2.5 obligation.
- **O-Q2-01 (deferred, non-blocking).** See §7.
- **P38 / P40.** The Q2 exploratory hypotheses for a stronger native-page refusal (P38) and aggregate-complete-flag refusal (P40) remain unmet; they were not strict acceptance failures and are preserved as the recorded O-01/O-02 observations.

## 7. Non-blocking status and rationale for O-Q2-01

**Observation.** Clearing the additive `RequiresOccurrenceAuthority` marker on a **present** graph owner (owner not retired) while retaining its authority-bearing occurrence unit does not currently cause refusal; the query returns `Available`.

**Rationale for non-blocking status.** This is not an F-01-class defect. Unlike an orphaned or retired owner, no stale, orphaned, or semantically invalid occurrence is served: the occurrence unit's recorded defining authority is still independently validated against the current verified live compilation before any hit is returned, and its durable ownership key is validated. The condition additionally requires externally corrupting an existing graph record's additive marker rather than removing a graph owner, and it weakens only the graph-marker↔authority bijection claim, not the no-stale/no-orphan guarantee. It is recorded as a separate candidate hardening issue (a symmetric "authority-bearing occurrence ⇒ marker-true owner" clause) and is **not** authorized for remediation by this decision.

## 8. Excluded or unqualified claims

This decision does not accept: cross-process segment-resumption occurrence reconstruction (Q1-Q1); recovery-rebuild failure atomicity (Q1-Q2); digest canonicalization (S-01); any new public API, schema, namespace, migration, or persisted representation; crash/power-loss/multi-process recovery; cross-platform certification; unrestricted concurrency or MVCC; real-fact plugin/batch/sink publication integration (WP-S23-3); broad real-fact lifecycle qualification (WP-S23-4); native schedule/platform guarantees; or any stronger refusal hypotheses beyond those demonstrated (P38/P40).

## 9. Subsequent work authorization status

**WP-S23-2 is closed by this decision.** This acceptance is final for WP-S23-2's bounded scope only.

- **WP-S23-3 (real-fact publication integration) is NOT authorized** and is not begun. It remains gated behind separate owner authorization, and Q1-Q1 remains a binding prerequisite.
- **O-Q2-01 remediation is NOT authorized** and is not begun.
- **S2.3 as a whole is not accepted**; WP-S23-4 and WP-S23-5 remain unauthorized.

Any further work requires a separate owner authorization.

## 10. Repository discipline

Only this decision document is added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, planning contract, oracle/corpus, or prior decision is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff, outside this self-containing Git record.
