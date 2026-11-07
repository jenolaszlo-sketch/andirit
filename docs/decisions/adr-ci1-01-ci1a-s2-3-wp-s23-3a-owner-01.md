# ADR-CI1-01-CI1A-S2.3-WP-S23-3A-OWNER-01 — Qualified owner acceptance of the WP-S23-3A real-fact publication package

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3A-OWNER-ACCEPTANCE-01`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Owner decision:** **`WP-S23-3A ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **`S23-PUB` / WP-S23-3A (real-fact publication integration, authority handoff, and the R1/R2 durable-reconstruction remediations) ACCEPTED within the qualified execution scope below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> Acceptance covers the authorized real-fact publication integration (`e2b833a`),
> the R1 completed-publication reconstruction remediation (`e0cef1c`), and the R2
> staged-run reconstruction remediation (`a4d8501`). S23-PUB-Q1 recorded
> `REMEDIATION REQUIRED` with the publication integration and R1 independently
> supported; S23-PUB-Q2 recorded `SUPPORTED WITH QUALIFICATIONS` and closed
> `F-Q1-01`. The Q2 qualifications Q2-Q01 and Q2-Q02 remain binding. WP-S23-3B,
> WP-S23-4, WP-S23-5, S2.4/S2.5, Stage 6 and public Find Usages are **not**
> authorized by this decision.

This is an additive owner decision. It changes no production source, test, reviewer evidence, oracle or corpus, planning contract, prior decision, or historical review artifact.

## 1. Authority, exact baseline, and ancestry

Controlling authority: the [WP-S23-3 entry owner decision](adr-ci1-01-ci1a-s2-3-wp-s23-3-entry-owner-01.md), the authorized [WP-S23-3A specification](../planning/adr-ci1-01-ci1a-s2-3/wp-s23-3-publication-integration.md), the [S2.3 work-package plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md), the [WP-S23-2 owner decision](adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md), the [NEW-01 owner decision](adr-ci1-01-ci1a-s2-3-new-01-owner-01.md), and the independent review reports [IMPL-01](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-IMPL-01.md), [R1](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-R1.md), [S23-PUB-Q1](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q1.md), [R2](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-R2.md), and [S23-PUB-Q2](../reviews/ANDIRIT-CI1A-S2.3-WP-S23-3A-S23-PUB-Q2.md).

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| WP-S23-3A implementation parent (NEW-01 baseline) | `b576acb004747c89d257d8e61721c17babe6c5a6` | `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` |
| WP-S23-2 accepted baseline | `80f61774bf7f8205ec1f8fb27d0ac1c69aee4bfe` | — |
| **Real-fact publication integration (accepted)** | **`e2b833a35e74d7fa8b1bf9eabe506af0357902b3`** | **`a196cb2ce21163c9b45c2abd03f11fd7d4754382`** |
| IMPL-01 report / blocker record | `f8b2b08bbd2db0a551412816705a7971973658bd` | `0cc5ad3a9c045e4250d35a9d666b17f52e2b5879` |
| R1 reviewed HEAD (blocker baseline) | `f8b2b08bbd2db0a551412816705a7971973658bd` | `0cc5ad3a9c045e4250d35a9d666b17f52e2b5879` |
| **R1 completed-publication reconstruction remediation (accepted)** | **`e0cef1c94f68f2a24a529e1182c0de5862e383c8`** | **`f6284492bdc08e786e78ddac4b47e429bacff4be`** |
| R1 handoff docs commit | `072f52bb3a39dbb5cb4dd7c26e13e7022a0160e6` | `3c2f2e1f1504b2e7e9300ba59eb738dae5dc0edb` |
| **Independent Q1 qualification** | **`b76dc4c825ac73da832265338d56a223ffbc5b6f`** | **`6116523b445a52043efcda010dd2a95ed6ea06c3`** |
| **R2 staged-run reconstruction remediation (accepted)** | **`a4d85011b8b753d4b058ed3a2a02fe2ebc73294c`** | **`cfbf67eb4d38999c3b95e0186655915461ff732b`** |
| R2 handoff docs commit | `a8e7b906cdfb23821245fe2900cd1c44e3bf1105` | `29fa9f1c9a3501d8ad74ac19376a701a83a24130` |
| **Independent Q2 requalification (accepted)** | **`36657172197370603aafe1adf3d8e31a29b0f03c`** | **`3998cea5f41d98cb845bbd44af465707e1b322a2`** |

The reviewed chain is consecutive and exact, verified from Git objects: `e2b833a` → `f8b2b08` → `e0cef1c` → `072f52b` → `b76dc4c` → `a4d8501` → `a8e7b90` → `3665717`. Git SHA-1 commit/tree identifiers are distinct from the SHA-256 file digests used below.

The owner began on clean canonical `main` at `3665717`, one worktree (`D:/projects/Andirit`), no stashes, nothing pushed. No fetch, branch, additional worktree, reset/restore, rebase, prune, or publication was performed.

## 2. Change scope and evidence integrity

The accepted production change set, verified from Git objects:

| Commit | Production files changed | Test files changed |
| --- | --- | --- |
| `e2b833a` (publication integration) | `src/Andirit/CodeOccurrenceAuthority.cs`, `src/Andirit/CodeGraphIngestionSink.cs`, `src/Andirit/CodeIndexingLifecycle.cs`, `src/Andirit.CSharp/CSharpCodeGraphPlugin.Session.cs` | `tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs` |
| `e0cef1c` (R1) | `src/Andirit/DurableCommandLog.cs`, `src/Andirit/InMemoryCodeGraphStore.cs` | `tests/Andirit.CSharp.Tests/S23DurableGraphReopenTests.cs`, `tests/Andirit.CSharp.Tests/S23PublicationIntegrationTests.cs` |
| `a4d8501` (R2) | `src/Andirit/DurableCommandLog.cs`, `src/Andirit/InMemoryCodeGraphStore.cs` | `tests/Andirit.CSharp.Tests/S23StagedReopenTests.cs` |
| `3665717` (Q2 review) | none | none |

`git diff --name-only f8b2b08 3665717 -- src tests` yields exactly the two R1/R2 source files and the three test files above; the publication-integration source files are unchanged after `e2b833a`. All changed/added store methods are `internal`; no public API, persisted format, schema, namespace, or Option A separation changed (independently asserted by both S23-PUB-Q1 and S23-PUB-Q2).

Evidence integrity was independently re-verified by the owner from the committed review artifacts:

| Evidence | Result |
| --- | --- |
| Q2 reviewer oracle output `CI1A-S2.3-WP-S23-3A-S23-PUB-Q2-evidence/oracle-r2-q2.json` | 146,851 bytes; SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Q2 evidence manifest | present; concrete entries exist on disk |
| Q2 review report | present |

The Q1, R1, R2 and IMPL-01 reports and their evidence directories were treated as historical artifacts and were not altered.

## 3. Accepted behavioral guarantees

Within the qualified execution scope, the owner accepts the following, evidenced by the independent S23-PUB-Q1 and S23-PUB-Q2 results and the owner re-check of the committed artifacts:

1. **Real-fact publication.** Real C# occurrence extraction publishes through the accepted chain `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync`, with correct cross-project `asmctx3` targets.
2. **Internal authority handoff preserves identity.** The internal `ICodeOccurrenceAuthoritySink` channel preserves owner, plugin, run and compilation identity; the transient envelope travels only through the non-public, non-persisted handoff.
3. **Association correctness.** Authority envelopes and the `RequiresOccurrenceAuthority` marker remain associated with their actual publication units; a mismatch is refused at the sink and cannot survive the query gate.
4. **No falsely available facts.** Missing, stale, mismatched or incomplete authority never produces falsely available occurrence facts (`AnalysisUnavailable`/`StalePublication`/`CorruptedIndex` as applicable; never a successful empty).
5. **Completed cross-unit durable reopen.** Completed cross-unit publications survive fresh-process durable reopen (R1; S23-PUB-Q1 P02/P04/P06, S23-PUB-Q2).
6. **Order-independent staged reopen.** Valid interrupted cross-unit staged runs reopen independently of persisted stage-key ordering (R2; S23-PUB-Q2 P11/P13/P17/P18 and adversarial probes).
7. **Fail-closed after reopen.** Unavailable authority continues to fail closed after reopen (S23-PUB-Q2 P11/P18: `AnalysisUnavailable`, 0 hits, 0 examined, not complete).
8. **Verified re-staging.** Fresh independently verified authority permits correct re-staging (S23-PUB-Q2 P13: `Available`, one expected hit, complete, correct source/defining units).
9. **Corruption refusal.** Genuine graph and occurrence corruption remains refused after complete reconstruction, with the accepted error-code vocabulary (missing endpoint, inconsistent edge, malformed row, duplicate/conflicting identity).
10. **Atomic reconstruction.** Reconstruction remains atomic without exposing partial publication state; a failed replay leaves the durable snapshot byte-identical.
11. **Surface preservation.** Original public APIs, persisted formats, and the Option A identity separation remain unchanged.
12. **Preservation.** Previously accepted NEW-01, S23-DUR and relevant lifecycle guarantees remain intact within their qualified scope.

## 4. Mandatory qualifications

- **Q2-Q01 — Concurrent interrupted runs (binding).** The pre-existing stale-baseline rule for two concurrent interrupted runs in one repository remains applicable: after one run completes, the other is refused by `RequireCurrentBaseline` and must be re-planned under a new run. No broader concurrent-recovery guarantee is accepted.
- **Q2-Q02 — Failure message (binding).** R2 changes the text of one staged-graph reconstruction failure message (`The restored staged graph is invalid.`), without changing the accepted error-code vocabulary. The message text is not part of any acceptance contract.
- **Disclosed same-session reviewer qualification (binding).** S23-PUB-Q2 discloses that it was executed in the same agent harness session as prior commissions, as a distinct commissioned review task, with all evidence re-derived and re-executed and probes reproducible from committed harnesses. If strict session separation is required, the owner may commission an independent re-run; this acceptance rests on the independently re-verified committed artifacts, not on session identity.
- **Existing S23-DUR / NEW-01 limitations (binding).** Q1-Q1 and Q1-Q2 from the WP-S23-2 decision remain binding; S-01 remains deferred to S2.5; O-Q2-01 remains a separate, unstarted hardening candidate.
- **Durability boundary.** No claim of arbitrary crash, power-loss, abrupt-kill, arbitrary-crash-point, cross-platform, or multi-process-writer durability beyond the accepted and demonstrated tests. Verification remains Windows x64 with the accepted SDK/runtime and the serialized single-writer store.
- **No public Find Usages.** No new public Find Usages capability is accepted or implied.
- **No successor authorization.** No authorization is granted for WP-S23-3B, WP-S23-4, WP-S23-5, S2.4, S2.5, Stage 6, or any other work.
- **Claim provenance.** Producer claims were accepted only where S23-PUB-Q1 or S23-PUB-Q2 independently established them; carried obligations (S-01, SQ-03, SQ-04, real-fact N1–N8, O-Q2-01) remain unqualified.

## 5. Verification receipts (from committed review artifacts)

| Gate | Evidence | Result |
| --- | --- | --- |
| Full solution | `…S23-PUB-Q2-evidence/q2-solution-tests.log` | 865 passed, 0 failed, 0 skipped (LatticeDb 50, TreeSitter 157, core 345, CSharp 313) |
| Focused S23 | `…S23-PUB-Q2-evidence/q2-focused-tests.log` | 69 passed, 0 failed, 0 skipped |
| NEW-01 regression | `…S23-PUB-Q2-evidence/q2-new01-tests.log` | 17 passed, 0 failed, 0 skipped |
| Frozen R2 oracle | `…S23-PUB-Q2-evidence/q2-oracle.log` + `oracle-r2-q2.json` | 185/185, 0 failed; 146,851 bytes; SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` (unchanged) |
| Fresh-process P11/P13/P17/P18 | `…S23-PUB-Q2-evidence/Q2-P1*.json` (pids 10060→39596, 25136→6664, 34596→31820) | supported |
| Adversarial staged reconstruction + corruption controls | `…S23-PUB-Q2-evidence/Q2-ADV-*.json` | supported |
| Release build | `…S23-PUB-Q2-evidence/q2-release-build.log` | 0 warnings, 0 errors |
| Formatting | `…S23-PUB-Q2-evidence/q2-format.log` | exit 0 |

The owner verified these receipts and the oracle digest from the committed artifacts rather than from abbreviated values; the full test suite was not re-run solely to record this decision.

## 6. Explicit acceptance boundary

Acceptance is limited to the serialized native store on **Windows x64** with the accepted SDK/runtime, the accepted publication lifecycle, the accepted S2.2 extraction/refusal semantics, the accepted WP-S23-2 carrier, the real-fact publication integration, and the demonstrated completed-publication and staged-run durable-reopen behavior. It does not extend to crash consistency, abrupt-kill or power-loss recovery, arbitrary crash points, cross-platform correctness, unrestricted concurrency, or multiple writers.

## 7. Excluded or unqualified claims

This decision does not accept: WP-S23-3B or any further WP-S23-3 package; WP-S23-4 or WP-S23-5; S2.4/S2.5; Stage 6; public Find Usages; cross-process segment-resumption occurrence reconstruction (Q1-Q1); recovery-rebuild failure atomicity (Q1-Q2); digest canonicalization (S-01); O-Q2-01 remediation; any schema-format migration, dual-write, or legacy reader/adapter; crash/power-loss/multi-process recovery; cross-platform certification; MVCC/snapshot isolation; background/distributed indexing or automatic repair; or package publication. No successor package is authorized by this decision.

## 8. Subsequent work authorization status and next bounded package

**WP-S23-3A is closed by this decision** (its authorized bounded scope only), on top of the already-accepted WP-S23-1 (NEW-01) and WP-S23-2 (S23-DUR) gates.

The S2.3 [work-package plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md) defines no separate `WP-S23-3B` package; the only committed occurrence of "WP-S23-3B" is stop-condition prose in earlier commissions. The next explicitly planned bounded gate is therefore **`WP-S23-4` — Native lifecycle qualification with real facts**, not another WP-S23-3 package. It is **not** authorized by this decision and requires its own commission, independent qualification (`S23-LIFE-Q1`), and owner decision.

- **Next recommended bounded package:** `WP-S23-4` — Native lifecycle qualification with real facts.
- **Independent gate:** `S23-LIFE-Q1` (separate reviewer).
- **Prerequisites:** WP-S23-1, WP-S23-2 and WP-S23-3 (now satisfied/accepted); the frozen R2 oracle, D3/D4/D5 and the carried obligations remain binding.
- **Scope (per the plan and the WP-S23-3A specification's deferred items):** tests + evidence only, no production change expected — adapt the Stage 1 native lifecycle tests to real facts over the N1–N8-equivalent matrix (document 5 L1–L20), covering real-source replacement/deletion/reindex, contributor behavior, graph/occurrence consistency, availability/coverage, native reopen, the SQ-03 native schedule test, and D3/D4. Any production change, D3/D4/D5 weakening, or new public API returns to the owner.
- **Entry criteria:** separate owner commission of WP-S23-4; clean canonical `main` with WP-S23-3A accepted; the accepted WP-S23-2 carrier and WP-S23-3A publication path as the test surface; independent qualification plan agreed before test authoring.
- **Not authorized:** implementation must not begin under this decision. WP-S23-5 remains unauthorized.

This acceptance is final for WP-S23-3A's bounded scope only. Any further work requires a separate owner authorization.

## 9. Repository discipline

Only this decision document and the additive authorization-status note to the S2.3 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, historical review, evidence, oracle/corpus, or prior decision is modified. No branch, additional worktree, push, or package publication is performed. The decision commit/tree identities and final working-tree status are reported in the handoff, outside this self-containing Git record.
