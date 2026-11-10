# ADR-CI1-01-CI1A-S2.5-WP-S25-2-OWNER-01 — qualified WP-S25-2 owner acceptance (SQ-05 native Linux qualification)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-2-OWNER-01` (G4).
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decisions:**
**`WP-S25-2 ACCEPTED WITH QUALIFICATIONS`**;
**`SQ-05 SATISFIED WITH QUALIFICATIONS`**;
**`F-G2-01 OPEN — REPOSITORY CI COVERAGE-GATE FAILURE`**.

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> WP-S25-2 is accepted on the G2 authorized execution and the independent G3 assessment, for
> the bounded SQ-05 scope: the expanded 52-test `Andirit.LatticeDb.Tests` suite passed on
> genuine hosted Ubuntu Linux at the exact owner-authorized revision. These dispositions are
> compatible because the normative SQ-05 contract (acceptance matrix §14; register SQ-05)
> requires successful native test execution with reviewable logs and a platform-behavior
> record — it does **not** require a successful overall CI conclusion. **F-G2-01 remains
> OPEN** as an unresolved repository CI coverage-gate failure (platform-independent,
> pre-existing); this acceptance does **not** waive the 70% policy, reduce the threshold, add
> exclusions, or claim CI success. No production/test/CI change, coverage remediation, remote
> operation, or successor work is authorized.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `eb1abef35f22c314963ff125d740c6dcc7f1ca2c` / `b93d1ad24bff023c31ce8d2280718d0946d6fdb9` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` = `443c1927…`; local `main` **75 ahead**; last verified during G3 (read-only `ls-remote`: remote `main` unchanged; qualification branch at the pinned commit). **No fetch or remote contact in this commission** |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

No unexpected repository state, authority conflict, or evidence inconsistency. No reset/clean/restore/rebase/prune/fetch/push/worktree creation, and no remote mutation.

## 2. G1–G3 chain verification

| Gate | Commit | Record |
| --- | --- | --- |
| S2.5 entry owner | `9b158b8` | `S2.5 ENTRY AUTHORIZED WITH QUALIFICATIONS`; WP-S25-2 planned |
| Route decision | `cd3e516` | `SQ-05 ROUTE READY — OWNER EXECUTION AUTHORIZATION REQUIRED` (Route A — GitHub-hosted Ubuntu; no CI modification) |
| **G1** execution/publication authorization | `8ac64e9` | `SQ-05 PUBLICATION AND EXECUTION AUTHORIZED WITH QUALIFICATIONS` — pinned revision, exact operation, five prerequisites, §7 identity conditions |
| **G2** authorized execution | `df57a2a` | `LINUX EXECUTION COMPLETE WITH RESTRICTIONS` (push + PR #6 + run 42744298084 + sealed evidence) |
| **G3** independent assessment | `eb1abef` | `SUPPORTED WITH QUALIFICATIONS` (14/0 identity checks; 17/0 execution/coverage checks; 13/0 integrity checks) |

**Scope compliance verified.** G2's remote operations were exactly the G1 §5 set: one branch push (pinned commit), one PR (never merged), the unmodified `ci.yml` run, one merge-ref fetch, one artifact download — no force, no `main` push, no deletion, no workflow change. G3 made **no** production, test, CI, or remote modifications (read-only inspection; its Git diff vs the G2 commit is additive reviewer evidence only).

## 3. Exact qualified revision identity

**Pinned/authorized: commit `7457284451dff8ddb0417f83ecc19097103316fc`, tree `fba379453ceb3088cab97141ebae5c4c4978e3d3`** — verified at this commission as Git objects, a strict ancestor of HEAD, carrying the 52-test LatticeDb suite and `LatticeDbSharp 0.2.0`. The five-identity chain (independently established by G3 from primary sources — Git objects, `ls-remote`, GitHub PR/run API — and re-read here from the committed `Q1-IDENTITIES.json`): remote branch = PR head = run `head_sha` = `7457284…`; synthetic merge `331ad6062908458665a40ca0c777b79507c52709` with **tree `fba37945…` == the authorized tree**; base = the unchanged remote main; workflow blob `be48426f…` identical at the pinned revision. **G1 §7 is satisfied with evidence; no identity conflict exists and nothing was inferred from ancestry alone.**

## 4. Native Linux execution (owner re-verification of the G3-established facts)

From the freshly downloaded hosted log (byte-identical to the committed copy, independently re-downloaded by G3):

| Field | Value |
| --- | --- |
| Environment | **Ubuntu 24.04.5 LTS x64** (hosted image `ubuntu-24.04`, `20261101.360.1`), .NET **SDK 10.0.403** / runtime 10.0.14 |
| Native dependency | `LatticeDbSharp 0.2.0` → `runtimes/linux-x64/native/liblattice.so`, **resolved and exercised** (the passing store-opening/publication/reopen tests are impossible without it) |
| Configuration | Release; **no test filter**; complete current suite |
| **`Andirit.LatticeDb.Tests`** | **Passed 52 / Failed 0 / Skipped 0** |
| Supplementary (same Linux run) | core **345/0**, CSharp **353/0** (incl. S23 authority/lifecycle/ownership), TreeSitter **157/0**; `pack` success — recorded as supplementary evidence only, within their demonstrated scope |

## 5. L1–L9 qualification findings (adopted from the independent G3 assessment)

L1 environment: independently supported (Ubuntu 24.04.5 LTS x64; SDK 10.0.403; hosted identity). L2 suite: independently supported (52/0/0, no filters, zero skips). L3 persistence/durable reopen: independently supported. L4 schema/corruption refusal: independently supported. L5 publication authority: independently supported (S23 authority/ownership tests green on Linux). L6 recovery/lifecycle: **supported with qualifications** (managed fault injection only; **F-02 preserved** — no crash/power-loss durability). L7 platform behavior: **supported with qualifications** (suite-level Linux/Windows parity observed; internal locking/WAL/mmap mechanics **not directly instrumented** — recorded as an observation limit). L8 evidence/provenance: independently supported. L9 boundary: independently supported (zero unauthorized changes).

## 6. F-G2-01 contractual classification

**`F-G2-01 — OPEN: REPOSITORY CI BRANCH-COVERAGE GATE FAILURE`.**

Independently verified facts: LatticeDb **branch coverage 65.77% (296/450)** vs the configured **70%** gate (`ci.yml` `/p:Threshold=70 /p:ThresholdType=line,branch`); line coverage 87.01%; **the identical threshold error occurs on Linux, Windows, and macOS** (platform-independent, pre-existing — the last green hosted run predates the current suite); **all required Linux native tests passed**; the overall CI job conclusion is `failure` **solely** because of this gate.

**Contractual separation, verified against the SQ-05 contract:** acceptance matrix §14 and register SQ-05 require the expanded native suite to execute on Linux with reviewable logs and a platform-behavior record — **they contain no coverage-percentage requirement**. The 70% branch gate is repository CI policy. Therefore: **F-G2-01 is non-blocking for bounded SQ-05 acceptance**, and it is simultaneously a **real, unresolved repository quality-gate failure** that must not be silently waived. Accordingly: **no CI success is claimed; the 70% policy is neither waived nor reduced; no exclusions are added; the finding is not marked resolved.** If a Stage-2 exit requirement is later interpreted to make a green overall CI conclusion mandatory, that consequence is recorded here as a **separate Stage-2 matter** for the Stage-2 acceptance decision — not overridden by this acceptance.

**Recommended (separate commission, not authorized here):** a bounded coverage-gap investigation plus additional LatticeDb branch tests (~19 covered branches reach the 70% gate; the largest contributor is `Andirit.LatticeCodeGraphStore` at 72% branch), or an explicit owner coverage-policy clarification. Threshold reduction, exclusions, test skipping, and workflow edits remain forbidden without such a decision.

## 7. Evidence and manifest integrity

Owner re-verification (read-only, no execution): **G3 manifest 6/0**, **G2 manifest 18/0**, **SQ04-Q1 manifest 20/0**, **BIND-01 manifest 24/0**; G3 probe receipts re-read (`Q1-IDENTITIES` 14/0, `Q2-Q4-EXECUTION-COVERAGE` 17/0, `Q5-INTEGRITY` 13/0); the three bound fixture masters re-hashed byte-identical (`7646b982…`, `8ca170cd…`, `f21a72be…`); the pinned revision's 52-test suite re-counted from the Git object. **The G2 accidental-seal incident is verified fully reversed**: the SQ04-Q1 package (manifest, `reviewed-head.txt` recording its own sealing state `bf1af23…`/`f2195c26…`) is byte-identical to the accepted committed artifacts — no historical evidence was rewritten. Provenance classes remain distinguished: direct GitHub Actions records, independently verified Git identities, fresh reviewer analysis, producer execution, inherited receipts, constructed controls, and unsupported claims.

## 8. Accepted capability boundary (WP-S25-2 / SQ-05)

**Accepted (with qualifications):** genuine **hosted Linux x64** execution (Ubuntu 24.04.5 LTS, image `ubuntu-24.04`/`20261101.360.1`, .NET SDK 10.0.403); the **current expanded 52-test `Andirit.LatticeDb.Tests` suite** (0 failed, 0 skipped); exercised **persistence and durable reopen**; exercised **schema/corruption refusal**; exercised **publication and authority behavior**; exercised **managed recovery/lifecycle** (managed injection only); **verified source-tree and execution provenance** (five-identity chain; executed tree = authorized tree); the supplementary Linux green results of the core/CSharp/TreeSitter suites within their demonstrated scope.

**Not accepted / not claimed:** all possible Linux distributions; other architectures; uninstrumented platform internals (locking/WAL/mmap mechanics beyond suite-observed behavior); unrestricted concurrent writers; crash/power-loss durability; unrestricted real-authority union; **successful repository-wide CI quality gates**; any unexecuted platform behavior; a general Linux portability certification. SQ-05 is **not** broadened beyond the demonstrated environment and tested behavior.

## 9. Binding qualifications

**Carried unchanged:** F-01, F-02, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness reviewer-independence limitation; serialized single-writer model; managed-fault durability limits; restricted real-authority cross-unit union; F-OBS-Q1-01 (target-local `O(n log n)` pagination); F-OBS-Q1-03 (unkeyed continuation checksum); Q2-OBS-01; S1/S3/S4 observations; **the amended SQ-04 boundary** (`SQ-04 SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`; the eight experimental codecs excluded; D5 refusal operative). **New/extended:** the Linux qualification is restricted to the demonstrated hosted environment (Ubuntu 24.04.5 x64, SDK 10.0.403) and exercised behavior; hosted-run independence is receipt-based; **F-G2-01 remains OPEN and binding for the workflow conclusion**.

**Remaining S2.5 obligations (open, not discharged):** independent S-01 integrated proof; D3-full; applicable D4/D5; WP-S25-4 final integration; remaining SQ-06 obligations. **D4.5 and full S-02 remain Stage 6 assignments. WP-S24-3 remains optional, deferred, and unauthorized.** No qualification is silently discharged.

## 10. SQ-05 obligation disposition

**`SQ-05 SATISFIED WITH QUALIFICATIONS`** — the expanded native suite executed successfully on genuine hosted Linux at the authorized revision with reviewable, independently verified logs and a platform-behavior record, within the qualified boundary (§8) and the recorded restrictions (L6/L7; hosted-independence; **F-G2-01 open**). Full platform certification remains Stage 6. The SQ-05 satisfaction does **not** extend to unexecuted behaviors or other distributions/architectures.

## 11. Formal owner decision

**`WP-S25-2 ACCEPTED WITH QUALIFICATIONS`.**

WP-S25-2 is accepted on the G2 authorized execution and the independent G3 qualification, bounded to the SQ-05 native Linux scope above, subject to §8's boundary, §9's qualifications, and **F-G2-01 OPEN**. The acceptance does **not** waive the separate coverage policy, does **not** claim CI success, and does **not** authorize any remediation or successor work.

## 12. Remote qualification resources

**Known state (last verified by G3's read-only inspection, 2026-11-10; not re-verified here):** PR **#6** (`https://github.com/jenolaszlo-sketch/andirit/pull/6`) remains **open and unmerged**; the branch `qualification/ci1a-s25-sq05-linux` remains at the pinned commit `7457284…`; run **42744298084** and its coverage artifacts are retained as the SQ-05 evidence (GitHub retention is limited — the authoritative copies are sealed in the committed G2/G3 evidence packages).

**Cleanup determination.** The PR and branch serve as the SQ-05 evidence anchor and should be **retained until the Stage-2 acceptance decision**; their disposition (merge, close, or delete) is **not** inferred from WP-S25-2 acceptance. If cleanup is later desirable, it requires a **separate bounded remote-resource disposition** naming the exact operations (no force, no history rewriting, no collateral ref changes).

## 13. Stage-2 consequences

- SQ-05 is removed from the open-prerequisite list for the Stage-2 acceptance decision **within the qualified boundary**; the acceptance decision must restate: the demonstrated environment scope, the F-02/L7 restrictions, the hosted-independence qualification, and **F-G2-01 OPEN**.
- **The coverage-gate failure remains a repository quality-gate matter**: if the Stage-2 acceptance contract is interpreted to require a green overall CI conclusion, that consequence stands **separately** and must be resolved by the dedicated coverage-gate decision — it is **not** overridden by this acceptance.
- Remaining Stage-2 prerequisites: independent S-01 integrated proof (WP-S25-3), D3-full real-fixture part, applicable D4/D5 integration, remaining SQ-06 placement, and the WP-S25-4 final integrated qualification.

## 14. Successor authorization status

- **WP-S25-1: accepted with qualifications (closed). SQ-04: satisfied under the amended contract. WP-S25-2: accepted with qualifications (closed). SQ-05: satisfied within its qualified scope. F-G2-01: OPEN.**
- **WP-S25-3 (S-01 integrated proof): planned, NOT authorized** — recommended as the next major technical package (independent integrated source-bytes → compilation → binding → canonical-digest → identity proof), subject to its own commission.
- **WP-S25-4 (final integrated Stage-2 qualification): planned, NOT authorized.**
- **Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Optimization: not authorized. Migration/dual-write: forbidden (D5).**
- No production/test/CI change, coverage remediation, remote operation, or new public API is authorized by this decision.

## 15. Repository discipline

Only this decision document and the additive WP-S25-2 status-note update to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No remote operation, fetch, push, PR modification, branch change, or additional worktree is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.