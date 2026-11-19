# ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04

**Overall WP-S25-4 disposition: REMEDIATION REQUIRED; Q03-F-01 TECHNICALLY QUALIFIED.** Independent Windows qualification of the pinned WP-S25-4 R3 membership-accounting remediation establishes M5.1 and M5.8 on the reviewed Windows scope. The commission does not close WP-S25-4: M5.4 remains BLOCKED with disposition D, corrected-revision genuine Ubuntu M11.4/M14.1 remain BLOCKED, exact-Git-byte formatting remains unsatisfied, F-G2-01 remains OPEN, and owner/Stage-2 acceptance is not issued. Review executed 2026-11-18 and finalized 2026-11-19, Asia/Manila.

| Subject | QUAL-04 independent disposition |
| --- | --- |
| Q03-F-01 | **QUALIFIED at the technical level** on pinned R3 M5.1/M5.8: complete scopes publish disjoint 1/1 source contributions with a full-universe authority envelope; aggregate membership is N/N at publication and fresh-process reopen |
| R2 defect control | Reproduced on pinned R2: two complete members report 4/4 and three report 9/9 on publication and reopen |
| Matching-authority legacy compatibility | **PASS within the genuine historical layout**: one self-owned row per verified unit, each carrying full-universe counts, restores to disjoint 1/1 and correct aggregate N/N |
| F-S254-01 / F-S254-02 / F-S254-03 | Independent preservation remains **PASS** in their bounded Windows scopes; formal finding closure remains owner-governed |
| B3 identity and ownership | **50/50** identities match at publication and fresh-process reopen; all 50 facts are owned by Caller, Dependency owns zero, and declaration span is `Dependency/Api.cs:4:5:4:35` |
| M5.4 | **D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED**; governance **BLOCKED** |
| Linux M11.4 / M14.1 | **BLOCKED** on corrected-revision genuine Ubuntu evidence |
| F-G2-01 | **OPEN**; R3 is 305/466 branches = 65.45%, R2 is 296/450 = 65.77%, both below the unchanged 70% policy |
| Exact-byte formatting | **NOT SATISFIED / NOT WAIVED** — exact Git bytes exit 2; checkout profile exit 0 |
| Organizational independence | **Not established.** Session-level independence and independent method are established; no employment/reporting-line evidence exists |

## Identity, independence and integrity

| Binding | Commit | Tree |
| --- | --- | --- |
| Review starting HEAD | `dba6597284c655b7fd7d2645c1ecb9486fe6d693` | `fda08e42428625ea96e0deccf3a1d97eff7a99d1` |
| Reviewed R3 production/tests | `8b83e0558f4c757272cd60a1c81b8dbc6fd7873f` | `0f89a469858abd2126f3be01cd060bc534e930c0` |
| R2 negative baseline | `25bc6e3b4305c7ce088deb36cfb000138dc76648` | `2a1309d8e08f230e61a1cd1bf0f1b596179ea8b3` |
| QUAL-03 handoff | `495e73a0f24413d2dc8ce5e75e8dd3bf58833228` | `804bdf4aa7fdedc9ca7c30d6dac0caf9d174592d` |
| Recorded origin/main | `48370711a6247b972dfc83628618924e864be374` | `3483bd278c7a628383a372cb788559cbdaf5c717` |

The starting state was clean `main`, one worktree, zero stashes, eleven ahead and zero behind recorded `origin/main`. R3 is an ancestor of the review HEAD and R2 is an ancestor of R3. The reviewer session is `ses_e1257f799ffefw0spAJ9UdV67w`; it had no role in R3 implementation, producer verification, or producer-evidence construction. Independence achieved is session separation, independently derived expectations, a reviewer-owned harness, independent Python checkers, a pinned negative baseline, and bounded mutation controls. **Organizational independence is unestablished** because no employment or reporting-line evidence was supplied. The R3 package does not record a producer session ID, so none is inferred.

Plain `git archive` exports with conversion disabled were used rather than a production checkout. The R3 export has all **4,054** tracked Git blobs byte-exact, with zero missing and zero mismatched; its 1,961 extra files are exclusively `bin`/`obj` build outputs. The R2 export has all **3,331** blobs byte-exact, zero missing and zero mismatched; its 1,284 extras are likewise build outputs. The separate checkout profile differs in 790 tracked files solely by CRLF-to-LF normalization and has no other mismatch. Disposable stores lived under `C:\Windows\Temp\opencode\qual04`; no production or test source was edited. [Revision and environment binding](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/revisions.json) · [Exact archive verification](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/archive-verification.json) · [Independent export verifier](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/verify-archives.py)

## Independent method

The reviewer harness is a separate project named `Andirit.CSharp.Tests`, using the reserved `InternalsVisibleTo` assembly identity and referencing the production projects without modifying them. `expected.json` records reviewer-derived membership, preservation, compatibility and negative-control expectations. The C# harness publishes and then reopens through distinct processes; its Python checker independently enumerates declared and verified IDs, persisted owner rows, authority members, raw coverage, target hits and refusal behavior.

The runner clears a store only before publication. Verify never republishes and reopens exactly the durable file left by the publisher. Lifecycle generations retain one store across 2/2 → 3/2 → 2/2 → 2/2. Reviewer-only corrections, including the A3 fixture description and camelCase receipt schema, are retained rather than hidden. [Harness](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/harness/) · [Registered expectations](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/harness/expected.json) · [Correction log](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/corrections.md)

## Complete-scope membership qualification

R3 changes the complete-row contribution at the extraction/session boundary to the source-owning project's disjoint **1/1** contribution. It retains the full verified-universe authority envelope and checks the persisted row against that envelope. The restore projection handles matching historical rows without weakening the query-time authority gate.

| Case | R2 publish / reopen | R3 publish / reopen | QUAL-04 result |
| --- | --- | --- | --- |
| One complete member | 1/1 | 1/1 | PASS |
| Two complete members | 4/4 | **2/2** | PASS; R2 defect reproduced then corrected |
| Three complete members | 9/9 | **3/3** | PASS; R2 defect reproduced then corrected |
| Two complete-empty members | 4/4 | **2/2**, zero facts | PASS; Available and complete remain truthful |
| Mixed empty/non-empty | 4/4 | 2/2 with correct fact distribution | PASS |
| One missing member | 3/2 | 3/2 | PASS; IncompleteAnalysis remains false-complete-free |
| Two missing members | 4/2 | 4/2 | PASS; partial path preserved |
| Controlled mixed raw rows | 4/3 | 4/3 | PASS; partial path preserved |

Every R3 complete row persists declared/verified **1/1** while its authority envelope remains bound to the complete universe; the aggregate is **N/N**. Publication and fresh-process reopen agree. The replacement lifecycle is **2/2 → 3/2 → 2/2 → 2/2**, with stale-handle refusal preserved. The reviewer checker records **644 passed / 0 failed** assertions; the pinned R2 checker records **71 passed / 0 failed**. [Full R2/R3/legacy reconciliation](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/membership-reconciliation.json) · [R3 checks](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/checks/check-r3.txt) · [R2 baseline checks](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/checks/check-r2-baseline.txt) · [R3 receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-r3/) · [R2 receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-r2/)

## Legacy compatibility and its boundary

A genuine matching-authority historical layout has one self-owned row per verified unit and carries the full-universe counts on each row and envelope. R3 reads that layout at restore as disjoint **1/1** contributions and retains the correct aggregate **N/N**. The independent X2 control passes at 3/3 for publish and reopen.

The compatibility claim is deliberately limited to authentic or matching-authority history. A hand-tampered non-self-owned full-count envelope aggregates to 5/5 (X4b), and removal of an occurrence row with otherwise consistent metadata aggregates to 2/2 (X3). Neither state is constructible through an admissible production path. These observations remain an **inadmissible-history boundary**, not a new R3 defect and not a claim that arbitrary store tampering is detected.

A genuine R2-produced store reopened with R3 binaries returns **StalePublication**, zero hits, and the explicit reason that the recorded analysis scope/universe is not the current verified universe. This follows from `BuildPlatformReferenceEvidence`, whose `TRUSTED_PLATFORM_ASSEMBLIES` capture includes the changed application binaries. Reopening the same R2 store with the same R2 binaries instead exposes the historical 9/9 inflation. The compatibility result therefore does not claim cross-binary publication identity. [Legacy receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-r3/)

## Negative controls

| Control | Mutation | Result |
| --- | --- | --- |
| N1 combined | Revert both the session contribution and restore projection, exactly R2 semantics | **Detected**: 4/4, 4/4 and 9/9; historical whole-universe row shape returns |
| N1a supplemental | Revert only the session contribution and retain the restore projection | Aggregate remains correct at 2/2, 2/2 and 3/3 while durable rows retain the historical shape; the projection alone carries the corrected aggregate |
| N2 | Remove only the restore projection | **Detected**: matching-authority historical X2 changes from 3/3 to 9/9 |

N1a was added after the initial single-line attempt did not meet N1's registered aggregate expectation. It is retained as transparent supplemental diagnostic evidence, not represented as a pre-registered substitute. The combined N1 and N2 controls are the registered negative controls. The mutation checker records **19 passed / 0 failed** assertions. All mutations were applied only to disposable copies. [Mutation diffs](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/mutations/) · [Mutation checks](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/checks/check-mutations.txt) · [N1 receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-n1/) · [N1a receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-N1a/) · [N2 receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-n2/)

## R1, R2 and identity preservation

F-S254-01 retains all **50** positive occurrences, External=1, Unresolved=0 and zero external occurrence facts through publication and fresh-process reopen. The independent identity reconstruction verifies 50/50 IDs, Caller ownership, Dependency target identity, source sites and persisted declaration spans at both phases. F-S254-02 retains three unsupported and three unsupported-category counts without an unsupported fact. F-S254-03-none remains AnalysisUnavailable with an incomplete universe and zero verified entries. These controls preserve the bounded R1/R2 corrections; they do not grant formal owner closure. [B3 identity summary](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/independent-b3-identities.json) · [Preservation receipts](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/receipts-r3/)

## Regression, oracle, formatting and coverage

| Gate | Fresh independent result |
| --- | --- |
| Whole solution | **926 passed, 0 failed, 0 skipped**: 157 TreeSitter, 345 core, 372 C# and 52 native |
| Test inventory | **917 retained + 9 added, 0 removed**; all nine additions are the complete-scope membership tests |
| Release build | Exit 0, zero warnings and zero errors |
| Focused R2 / R1 / binding / publication / native | All pass |
| Frozen oracle | **185/185**, 146,851 bytes, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9`; byte-identical to the QUAL-03 frozen output |
| Formatting | Checkout profile exit 0; exact Git-byte profile **exit 2** with seven WHITESPACE requests in `CSharpProjectDiscovery.cs` and `VerifiedCSharpCompilation.cs`, both unchanged by R3; **not waived** |
| LatticeDb coverage policy | R3 **305/466 branches = 65.45%**, exit 1; R2 **296/450 = 65.77%**, exit 1; threshold remains 70 and no threshold/filter/CI change was made |

The functional LatticeDb test gate passes, while the exact CI coverage-policy gate fails on both revisions. F-G2-01 therefore remains OPEN and is not a R3 regression. [Gate summary](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gate-summary.json) · [TRX files](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gates/trx/) · [Test inventory](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/test-inventory.json) · [Oracle log](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gates/oracle.log) · [Exact-byte format log](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gates/format.log) · [Checkout format log](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gates/checkout-format.log) · [Coverage policy exits](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/gates/coverage-policy-exits.json)

## Matrix, obligations and remaining governance

The complete **64-row** reconciliation is **46 PASS, 10 PASS WITH QUALIFICATION, 0 FAIL, 3 BLOCKED, 1 REUSED QUALIFIED and 4 DEFERRED**, with 64 unique IDs. M5.1/M5.8 move from QUAL-03 FAIL to QUAL-04 PASS; M5.4, M11.4 and M14.1 remain BLOCKED. The complete **78-entry** obligation register is **53 QUALIFIED, 4 BLOCKED, 11 DISCHARGED, 7 OPEN-STAGE6, 1 OPEN-S2.5 and 2 AMENDMENT-SUBJECT**. F03, F04, NEW-04 and IF-03 move BLOCKED → QUALIFIED on the bounded membership correction. F06, F16, I-01 and the required-evidence entry remain BLOCKED; F-G2-01 remains OPEN-S2.5. [Complete matrix](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/matrix.md) · [Complete obligations](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/obligations.md)

M5.4 has no new continuous `MissingDefiningCompilation` receipt and no exhaustive compiler symbol-association proof. Its sole technical disposition remains **D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED**, not unreachability and not a defect. No amendment or waiver is made. The next actions remain: the narrow M5.4 investigation or explicit owner clarification; disposition of the exact-byte formatter profile; the separately authorized combined genuine-Ubuntu/test-only-coverage route; and separate owner acceptance. Full D3, D4.5, full S-02, F-02 crash/power-loss and full platform certification remain Stage 6.

## Evidence seal and handoff

Only this report and `ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/` are added on `main`. The evidence package is sealed by a self-excluding SHA-256 manifest; Git binds this report. The local [seal/verifier](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/seal.py) and [commit verifier](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/commit-and-verify.py) are part of that package. The final commit/tree and post-commit verification are supplied in the external handoff because embedding a commit's own identity in its contents would be self-referential. No fetch, push or remote CI execution was performed. **This commission stops at QUAL-04 handoff.** [QUAL-04 manifest](ANDIRIT-CI1A-S2.5-WP-S25-4-QUAL-04-evidence/manifest.json)
