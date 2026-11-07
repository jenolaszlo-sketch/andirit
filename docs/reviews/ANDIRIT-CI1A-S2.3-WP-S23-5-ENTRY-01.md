# ANDIRIT-CI1A-S2.3-WP-S23-5-ENTRY-01 — WP-S23-5 entry assessment (S2.3 final integration qualification and owner acceptance)

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-5-ENTRY-01`.
**Project:** Andirit. **Recorded:** 2026-11-07. **Canonical branch:** `main`.
**Role:** independent engineering reviewer, entry assessment only.
**Disposition:** **`ENTRY READY WITH QUALIFICATIONS`**.

> This is an entry assessment. It authorizes **no** WP-S23-5 execution, **no**
> production/test change, **no** S2.3 owner-acceptance decision, and **no**
> successor package. The proposed `WP-S23-5-QUAL-01` commission in §7 is a draft
> for separate owner authorization only.

---

## 1. Verified repository preflight

| Check | Result |
| --- | --- |
| HEAD | `078011b1c4f5a0d82c9a477b646dde965f3049b9` (expected) |
| Tree | `b91cf9249f6c61c5771c58dedef23388c1a8d1c4` (expected) |
| Branch | `main` |
| Worktrees | exactly one (`D:/projects/Andirit`) |
| Tracked/index state | clean; no stashes; nothing pushed |
| Ancestry of interest | `f0d960a` → `0dca67b` → `e4b64eb` → `b32cb32` → `5f1ae63` → `078011b` (WP-S23-4 owner acceptance) |
| Existing WP-S23-5 decision | none (`docs/decisions/` has no `wp-s23-5*`) |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Preflight matches every expected condition; no unexpected change and no evidence inconsistency was found. No reset/clean/restore/rebase/prune/push/worktree was performed.

---

## 2. WP-S23-1 through WP-S23-4 acceptance matrix

Reconstructed from the committed owner decisions and review reports (exact commits/trees as recorded there). Owner dispositions are all **accepted with qualifications**; **no package authorizes its successor.**

| Package | Owner decision | Accepted baseline (commit / tree) | Owner disposition | Accepted functionality | Explicitly excluded / unqualified |
| --- | --- | --- | --- | --- | --- |
| **WP-S23-1** (`NEW-01`) | `adr-ci1-01-ci1a-s2-3-new-01-owner-01.md` | impl `c75c41f` / `b7f2f174…`; owner `ef097aa` / `42dfa50d…` | **ACCEPTED WITH QUALIFICATIONS** (Q1-Q1, Q1-Q2) | Closed NEW-01 projection-loss/conflict-bypass on the two known-failure recovery paths; recovered projection equals durable reopen; coverage/refusal/complete-empty preserved; unknown-outcome quarantine unchanged; D3/D4/D5 preserved | No merge/identity/schema/migration/authority change; no crash/power-loss/multi-process recovery; **cross-process segment-resumption occurrence reconstruction (Q1-Q1)** and **recovery-rebuild failure atomicity (Q1-Q2)** excluded |
| **WP-S23-2** (`S23-DUR`) | `adr-ci1-01-ci1a-s2-3-wp-s23-2-owner-01.md` | R1 `1bce70e` / `a6a908ab…`; Q2 `0888be9` / `2d306828…` | **ACCEPTED WITH QUALIFICATIONS** (Q1-Q1, Q1-Q2, S-01, O-Q2-01) | Additive defining-`compctx2` carrier + reopen stale refusal; F-01 owner-correspondence defect closed across durable reopen; retirement correctness; complete-empty vs unavailable/corrupted | No new public API/schema/namespace/migration; no crash/power-loss/unrestricted-concurrency/MVCC; **O-Q2-01** (present-owner marker-clear) non-blocking, remediation not authorized; P38/P40 hypotheses unmet |
| **WP-S23-3A** (`S23-PUB`) | `adr-ci1-01-ci1a-s2-3-wp-s23-3a-owner-01.md` | impl `e2b833a` / `a196cb2c…`; R1 `e0cef1c` / `f6284492…`; R2 `a4d8501` / `cfbf67eb…`; owner `4a775c8` | **ACCEPTED WITH QUALIFICATIONS** (Q2-Q01, Q2-Q02) | Real-fact publication through the accepted chain; internal authority handoff; association correctness; no falsely-available facts; completed cross-unit durable reopen (R1); order-independent staged reopen (R2); fail-closed after reopen; corruption refusal; atomic reconstruction | Q2-Q01 concurrent interrupted-run stale baseline binding; Q2-Q02 message text; no WP-S23-4/5; no schema-format/dual-write/legacy; no crash/power-loss |
| **WP-S23-4** (`S23-LIFE`) | `adr-ci1-01-ci1a-s2-3-wp-s23-4-owner-01.md` | tests `0dca67b` / `690a6ae5…`; report+evidence `e4b64eb` / `8d8f134a…`; reviewed source `f0d960a` / `a72f05c6…` | **ACCEPTED WITH QUALIFICATIONS** (F-01, F-02, F-03 + carried) | L1–L20 real-fact lifecycle, N1–N8-equivalent, SQ-03 native schedule, D3/D4/D5; 18 new native tests; 8 native probe phases | **F-01** restricted real-authority cross-unit union; **F-02** managed fault injection only; **F-03** no authentic SQ-04 fixtures; carried Q1-Q1/Q1-Q2/S-01/Q2-Q01/Q2-Q02/O-Q2-01; Windows x64 serialized single-writer only |

**RQ1 — Acceptance-state consistency.** Each package's exclusions align with its successor's preconditions, and no earlier package's exclusion is contradicted by a later acceptance. The S2.2/Stage-2 capabilities WP-S23-5 must integrate (real-fact publication, native reopen, replacement/deletion, contributor behavior) are exactly what WP-S23-2/3A/4 accepted. **Do not infer closure from a later package's acceptance:** WP-S23-3A acceptance does not close SQ-03/SQ-04; WP-S23-4 acceptance does not close S-01, SQ-04, SQ-05 (Linux), or SQ-06.

---

## 3. Integrated evidence inventory

Provenance classification (avoiding aggregate-count inflation):

**A. Genuinely native, independently qualified (Windows x64):**
- WP-S23-2 Q2 F-01 fresh-process reproduction (3 PIDs; publish → owner-row removal → separate reader `CorruptedIndex`), plus the Q2 56-invocation matrix (54/56 predicates; 0 strict failures).
- WP-S23-3A S23-PUB-Q1/Q2 fresh-process publish/reopen, staged resumption, corruption/adversarial probes.
- WP-S23-4 S23-LIFE-Q1: 18 native lifecycle tests + `P01`–`P08` probes (real plugin/ingestion, real Roslyn, durable reopen, row inspection; each probe a distinct OS process).

**B. Same-harness reviewer results (disclosed):** S23-PUB-Q2 and S23-LIFE-Q1 were executed in the same agent harness as prior commissions, as distinct commissioned review tasks with reviewer-owned committed probes and frozen-R2-derived expected values. No separate-session/separate-model receipt exists. NEW-01-Q1, WP-S23-2-Q1/Q2, S23-PUB-Q1 were likewise produced in this harness history.

**C. Managed fault injection (not crash evidence):** NEW-01 recovery probes; WP-S23-2 recovery/stale probes; S23-LIFE-Q1 `L09`/`L10` (`before-commit`) and `L20` (`after-commit`).

**D. Synthetic / constructed cases (must not be presented as authentic production evidence):** the original Stage-1 `N1`–`N8` synthetic native tests; the `L04_*` two-storage-unit union construction; corruption/tamper controls (schema version, meta-row mutation, owner-row removal, malformed rows). These validate refusal/merge algebra, not naturally occurring production facts.

**E. Authentic production-path evidence:** real C# source bytes + SHA-256, live Roslyn compilations, real cross-project `asmctx3` targets, real `compctx2` defining bindings, and publication through the accepted `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync` path.

**F. Claims not demonstrated by available evidence:** crash/power-loss/abrupt-kill/arbitrary-crash-point recovery; Linux/cross-platform native behavior (SQ-05); a naturally occurring fully real-authority cross-unit identical-fact union (F-01); authentic SQ-04 historical-writer fixtures; MVCC/snapshot isolation/general linearizability; multi-process writers; digest canonicalization (S-01); SQ-06 namespace-mismatch test (see §4); O-Q2-01 remediation.

**Overlap caution.** The `883 passed` whole-solution figure includes the 18 new lifecycle tests and re-runs the already-accepted S23 suites; the `189` focused figure and the probe matrices overlap each other and the whole-solution run. The frozen oracle (`185/185`, SHA-256 `97F6C5E5…39C9`) is a single regression-integrity signal reused by every gate, not four independent confirmations. Counts are reported as regression floors, not as independent assurance.

---

## 4. Open qualification and obligation register

Carried forward **binding** (must not be silently resolved, broadened, or waived):

| ID | Obligation (as accepted) | Status for WP-S23-5 |
| --- | --- | --- |
| **F-01** | Restricted real-authority cross-unit union; no unrestricted claim | Preserve; do not claim unrestricted union |
| **F-02** | Managed fault injection ≠ crash/power-loss durability | Preserve; no crash inference from process restart |
| **F-03** | Authentic SQ-04 historical fixtures unavailable; none fabricated | Preserve; S2.5/Stage-2 obligation remains open |
| **Q1-Q1** | Cross-process segment-resumption occurrence reconstruction unqualified (facts not in segment records) | Preserve |
| **Q1-Q2** | Recovery-rebuild internal-failure atomicity unqualified | Preserve |
| **S-01** | Caller-supplied component digests; canonicalization proof | Preserve; **S2.5 integrated-proof gate** |
| **Q2-Q01** | Concurrent interrupted-run stale baseline (binding) | Preserve; S23-LIFE `P07` does not supersede it |
| **Q2-Q02** | Staged-graph failure-message text not a contract | Preserve |
| **O-Q2-01** | Present-owner marker-clear hardening candidate; remediation not authorized | Preserve as candidate; do not remediate |
| Same-harness reviewer independence | No separate-session/model receipt | Preserve; independent re-run optional |
| Platform / concurrency | Windows x64; serialized single-writer; no MVCC/linearizability | Preserve |

**Additional obligation surfaced by this assessment (entry finding RQ2):**
- **SQ-06 — occurrence namespace-mismatch refusal test is missing.** The register requires `Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` *or equivalent* (isolated `meta.Namespace` tamper → `CodeOccurrenceSchemaException`), gated **S2.3/S2.5 before Stage 2 acceptance**. Production code **does** enforce it (`LatticeCodeGraphStore.cs` §"Occurrence records exist…" → `throw new CodeOccurrenceSchemaException` on `meta.Namespace != OccurrenceNamespace`), but no test exercises the namespace branch: the only schema-open test (`OccurrenceStage1NativeTests.Store_RejectsUnsupportedOccurrenceSchemaOnOpen`) tampers the **version** to 99, and `Store_RejectsOccurrenceUnitsWithoutMeta` deletes the meta row. **Impact:** WP-S23-5 must add the isolated namespace-mismatch test (or cite a genuine equivalent) in its qualification, and must not record SQ-06 as closed by the existing version/units-without-meta tests.

**S2.5 / Stage-2 obligations preserved (not discharged by S2.3):** expanded **Linux** native suite (SQ-05); authentic **SQ-04** historical fixtures; **S-01** integrated proof; **SQ-06** namespace test; D4.5 (cancelled staging) and full S-02 Stage-6 items. A later `S2.3-ACCEPT-OWNER` decision must record these as open Stage-2 obligations and must not treat S2.3 acceptance as cross-platform certification or SQ-04 closure.

---

## 5. Proposed final integration qualification matrix

Minimum necessary checks across the accepted S2.3 surface, with reuse vs fresh-execution split. "Fresh" = not established by any committed evidence; "reuse" = committed evidence exists and may be re-cited (subject to independence and platform limits).

| # | Integration check | Covers | Reuse | Fresh |
| --- | --- | --- | --- | --- |
| I-01 | Real-project end-to-end fixture (Stage-2 §9 minima: ≥10 files, ≥50 invocations, ≥2 projects, cross-project target) through plugin → sink → store → reopen → reader; expected values independently derived | Publication, identity, persistence, cross-unit | Partial (small fixtures) | **Fresh** |
| I-02 | Known-failure recovery of an **authority-bearing real-fact** publication: recovered in-process projection and authority dictionary equal a fresh reopen; graph-owner ↔ occurrence correspondence holds | WP-S23-1 × WP-S23-2 × WP-S23-3A interaction | S23-LIFE `L10` (fixture) | **Fresh** (real plugin path, cross-package) |
| I-03 | Completed cross-unit and interrupted staged-run reopen with the defining-`compctx2` carrier + authority envelopes; order independence; stale refusal after reopen | WP-S23-2 × WP-S23-3A | S23-PUB-Q1/Q2, S23-LIFE `L08` | Reuse + **fresh** combined |
| I-04 | **SQ-06** isolated occurrence namespace-mismatch tamper → `CodeOccurrenceSchemaException` (and confirm version/units-without-meta unchanged) | D5 refusal, SQ-06 | version/units tests | **Fresh** |
| I-05 | SQ-02 unknown-outcome quarantine/reopen across the accepted paths; no blind retry | SQ-02, F-02 | NEW-01 P15, S23-LIFE `L20` | Reuse |
| I-06 | SQ-03 native schedule: old-or-new complete visibility, truthful attribution, no partial blend | SQ-03, D4.10 | S23-LIFE `P06`/`P07`, `L12*` | Reuse |
| I-07 | Real-source replacement/deletion/reindex with the carrier: new identities, supersession (D4.6), no unbounded scan (D4.3), complete-empty vs unavailable | L2/L3/L11/L13/L14 | S23-LIFE `L02*`, `L13*`, `L14*`, `P05` | Reuse + **fresh** carrier-stale variant |
| I-08 | Real-project D3 graph noninterference across the full occurrence lifecycle | D3, N8 | S23-LIFE `L16` | Reuse + **fresh** multi-project graph |
| I-09 | Availability/coverage/refusal accounting integrated across real + authority surfaces (no false-available / no false-empty) | D4.1/D4.2/D4.4, IF-03 | S23-PUB/S23-LIFE | Reuse + **fresh** integrated |
| I-10 | Regression floor at the qualification HEAD: whole solution green, frozen oracle `185/185` byte-identical (`97F6C5E5…39C9`), format exit 0, Release 0/0 | Integrity | All | **Fresh re-run** |
| I-11 | Evidence provenance + manifest (self-excluding SHA-256), independence disclosure, production-diff = 0 | Provenance | Pattern | **Fresh** |

**Interaction risks not covered by any single package (require I-02/I-03/I-09):** (a) the WP-S23-1 recovery rebuild over the WP-S23-2 carrier + WP-S23-3A authority envelope (recovery of an authority-bearing real-fact publication); (b) completed-publication reconstruction (R1) and staged-run reconstruction (R2) combined with real-fact authority envelopes and the defining-`compctx2` carrier in one durable file; (c) orphan/ownership correspondence (WP-S23-2 R1) evaluated against real-fact authority units (WP-S23-3A).

---

## 6. Entry disposition and rationale

**`ENTRY READY WITH QUALIFICATIONS`.**

**Rationale.** All four predecessor packages are owner-accepted with their bounded scopes; the planned WP-S23-5 scope (S2.3 owner acceptance; documentation/decision only, on a separate final integration qualification with combined independent reviews and the `S2.3-ACCEPT-OWNER` decision) remains consistent with the accepted S2.3 architecture. No accepted package's exclusion is contradicted, no production-scope change is pending, and the repository is clean at the expected HEAD. Entry is therefore supportable.

**Qualifications attached to entry (why not unconditional `ENTRY READY`):**
1. **No S2.5/Stage-2 closure.** S-01, SQ-04, SQ-05 (Linux), SQ-06, D4.5 and full S-02 remain open; WP-S23-5 must record them, not close them.
2. **SQ-06 gap (RQ2).** The namespace-mismatch refusal test is absent; WP-S23-5's qualification must add it or cite a genuine equivalent.
3. **Cross-package integration is unproven.** I-02/I-03/I-09 must be executed; individually accepted packages do not jointly establish the interactions.
4. **F-01/F-02/F-03 and Q1-Q1/Q1-Q2/S-01/Q2-Q01/Q2-Q02/O-Q2-01 preserved.**
5. **Independence limitation.** Same-harness reviewer results; an independent re-run of the final qualification may be commissioned if strict separation is required.
6. **Platform/concurrency boundary.** Windows x64, serialized single-writer; no MVCC/linearizability/crash claims.

No condition rises to `ENTRY BLOCKED`: the missing SQ-06 test is a bounded qualification gap (production already refuses correctly), not a demonstrated defect, and it is addressable within a tests/evidence-only WP-S23-5 qualification.

---

## 7. Draft next commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.3-WP-S23-5-QUAL-01` — S2.3 final integration qualification.

- **Scope:** tests, bounded real-input fixtures, probes and evidence only; no production change. Execute the §5 matrix (I-01 … I-11), with the fresh items called out; add the SQ-06 namespace-mismatch test; produce an independent report and a self-excluding SHA-256 evidence manifest; state a single disposition (`SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED`).
- **Required independent-review inputs:** the four owner decisions; the L1–L20 / N1–N8 matrices; the WP-S23-4 entry/owner decisions; the S23-LIFE-Q1 report and evidence; the S23-PUB-Q1/Q2 and WP-S23-2 reports; the obligation register (SQ-02…SQ-06, S-01, S-02, D4).
- **Evidence/provenance:** real S2.2 inputs; independently derived expected values (frozen R2 + identity primitives); raw receipts distinguishing native, managed-fault, synthetic and same-harness evidence; self-excluding manifest.
- **Regression expectations:** whole solution green; frozen oracle `185/185` byte-identical; `dotnet format --verify-no-changes` exit 0; Release 0 warnings/0 errors; production-diff 0.
- **Blocking failure conditions:** any D3/D4/D5 weakening; any frozen-identity/oracle or schema-format change; any false-available/stale/partial served; any accepted regression failing; any demonstrated production defect (→ `REMEDIATION REQUIRED`, stop, do not fix in-commission).
- **Disposition conditions:** `SUPPORTED` (all mandatory checks pass, no new limitation); `SUPPORTED WITH QUALIFICATIONS` (mandatory checks pass with explicitly bounded nonblocking limitations); `REMEDIATION REQUIRED` (demonstrated defect).
- **Then:** a separate `S2.3-ACCEPT-OWNER` decision (`S2.3-ACCEPT-OWNER` gate) recording accepted scope, all preserved qualifications, the open S2.5/Stage-2 obligations, and the no-public-Find-Usages / no-successor boundary.
- **Not authorized by this assessment:** WP-S23-5 execution, any production/test change, the S2.3 acceptance decision, WP-S23-5 entry authorization, S2.4/S2.5, Stage 6, public Find Usages.

---

### Assessment stop

This report is additive documentation only. It changes no production code, test, evidence, oracle/corpus, planning contract, or prior decision; it executes no qualification and records no acceptance. Final HEAD/tree and working-tree status are reported in the handoff.
