# ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01 — S2.5 entry and remaining Stage-2 obligation assessment

**Commission:** `ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01`.
**Project:** Andirit. **Recorded:** 2026-11-09. **Canonical branch:** `main`.
**Role:** independent entry and remaining-obligation assessment; assessment and documentation only.
**Disposition:** **`ENTRY READY WITH QUALIFICATIONS`**.

> This is an entry assessment. It authorizes **no** implementation, **no** qualification
> execution (including no Linux execution), **no** S2.5 work package, and **no** Stage-2
> acceptance. The proposed packages in §9 are drafts for separate owner authorization only.

---

## 1. Verified preflight and repository state

| Check | Result |
| --- | --- |
| HEAD | `2fbf2093f5670832c81fd58a94a5a79df1143d7e` (expected) |
| Tree | `83f6710e322e4e6bd517e47349abe12f0dacec0b` (expected) |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes |
| Local vs remote | `origin` `https://github.com/jenolaszlo-sketch/andirit.git`; `main` is **65 ahead / 0 behind** `origin/main` (local ref, no fetch performed); **nothing pushed** in this work stream |
| S2.3 / S2.4 acceptance | `adr-ci1-01-ci1a-s2-3-accept-owner-01.md` (S2.3 lineage), `adr-ci1-01-ci1a-s2-4-accept-owner-01.md` (HEAD `2fbf209`) present |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

**Evidence integrity re-check (read-only).** Eight committed manifests re-verified with **0 mismatches**: QUAL-PREP 21, SURFACE-Q1 28, READER-Q1 27, READER-Q2 41, R1 19, FINAL-Q1 17, S23-LIFE-Q1 32, S23-WP-S23-5-QUAL-01 32. The two authentic external fixtures cited in §4 were re-hashed and match their recorded values exactly.

No unexpected repository state, authority conflict, or evidence-integrity failure was found. No reset/clean/restore/rebase/prune/worktree/push was performed; no qualification was executed.

---

## 2. Authoritative S2.5 definition (A1)

S2.5 has no separately committed work-package plan. Its scope is defined across the Stage-2 boundary, the acceptance matrix, the obligation register, the S2.2/S2.3/S2.4 owner decisions, and the S-01 canonicalization disposition. Reconstructed from those sources:

| Source | Statement |
| --- | --- |
| [stage2-boundary.md](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8 (S2.5) | *Independent Stage 2 Qualification*: reproduce positive and negative semantic tests independently; verify the **S-01 proof chain** (source bytes → compilation → binding → occurrence identity); verify applicable **D3/D4** (real-project graph noninterference, publication consistency); reopen the native database and independently inspect results; **record remaining S-02, historical-fixture and cross-platform obligations**. Gate: **independent review before Stage 2 owner acceptance** |
| stage2-boundary.md §9/§10/§11/§14 | Real-project fixture/oracle requirements; internal-only reader; exclusions (public Find Usages, migration/dual-write, incremental/native-node-ID optimization, full cross-platform certification); stop conditions |
| [stage2-acceptance-matrix.md](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §16 + rows | "All requirements must pass independent Stage 2 review before Stage 2 owner acceptance"; S-01 S2.5 independent acceptance; semantic identity/refusal/cross-project/native/D3/D4/D5 rows; SQ-02/SQ-03 S2.3/S2.5; §13 authentic prior-writer fixtures; §14 Linux CI before S2.5 acceptance; §15 namespace test; §11 performance rows; §12 invariant 5 (cancellation) explicitly Stage 6 |
| [stage2-handoff.md](../planning/adr-ci1-01-ci1a-impl-01/stage2-handoff.md) §8/§9 | S2.5 gate = independent review over all above + D3/D4/D5/S-02 evidence, before owner acceptance; post-Stage-2 list (full S-02, full D3 corpus, Linux minimum is a Stage-2 acceptance item, optimization Stage 6/B2, public Find Usages ADR-CI1-02) |
| [Obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) §8 (S2.5 row) | Independent integrated source/compilation/semantic proof, real-project end-to-end/native reopen, D3/applicable D4, **authentic available historical refusals**, **expanded Linux suite** and **complete obligation reconciliation before owner Stage 2 acceptance** |
| [S2.2 G-ACCEPT](adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md) §18 | S2.5 = integrated proof chain **including the general source canonicalization of the identity component digests**, integrated S-01 chain, real-project end-to-end/native reopen, applicable D3/D4, authentic available historical refusals, expanded Linux suite, complete obligation reconciliation |
| [s01-canonicalization-disposition.md](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 | S-01 canonical component-digest derivation is an **explicit S2.5 completion gate**; earliest stage for stronger digest provenance is S2.5, before Stage-2 owner acceptance; do not reopen or silently resolve |
| [S2.3 accept](adr-ci1-01-ci1a-s2-3-accept-owner-01.md) §7; [S2.4 accept](adr-ci1-01-ci1a-s2-4-accept-owner-01.md) §7 | Deferred S2.5/Stage-2 obligations carried: **SQ-04, SQ-05, SQ-06 remainder, S-01, D4.5, full S-02, D3-full, residual IF-02/IF-05/SQ-07** |
| [S2.4 entry](adr-ci1-01-ci1a-s2-4-entry-owner-01.md) §8/§12 | Cross-target (WP-S24-3) deferred; forward-compatibility constraints fixed; all qualifications preserved |

**Exact intended S2.5 outcomes:**

1. Independent integrated **S-01 proof** over the production path: acquired source bytes → verified compilation context (`compctx1`/`compctx2`) → live binding → canonical component digests → `asmctx3`/`usage4` identity, with expected values derived independently of the implementation.
2. Independent reproduction of the positive/negative **semantic extraction, refusal and coverage** behavior (reviewer ≠ implementer; fresh processes).
3. **Real-project end-to-end**: plugin → sink → native publication → durable reopen → internal reader, with direct native row inspection.
4. Applicable **D3** (real-fixture graph identity/cardinality/callers/references/traversal/native-mirror observations) and applicable **D4** (incl. recovery/reopen, serialized schedule, bounded `Examined`), and **D5** confirmation; D3-full/D4-full remain Stage 6.
5. **SQ-04**: authentic *available-format* prior-writer refusal evidence, with no-writer formats recorded as an explicit limitation + owner amendment — never fresh-store tampering.
6. **SQ-05**: expanded current native suite on **Linux** with CI logs and documented platform behavior differences.
7. **SQ-06 remainder**, IF-01…IF-05, MD-01…MD-03, I-03…I-05 reconciliation; **complete obligation reconciliation** before Stage-2 owner acceptance.
8. Recording — not discharging — the remaining S-02/D4.5/cancellation/crash/power-loss/full-platform/full-corpus obligations.

**Dependencies on accepted S2.1–S2.4.** S2.1 verified source acquisition/compilation provenance (with the bounded Q-F2/Q-F3 dispositions later recorded at S2.2); S2.2 accepted live semantic minting/refusal (with the retained S-01 qualification); S2.3 accepted native publication/recovery/reopen lifecycle; S2.4 accepted continuation/coverage/refusal surface and the IF-02/IF-05/SQ-07 evidence.

**Mandatory vs optional.** Items 1–7 are mandatory for Stage-2 acceptance. Optional/deferred and **not** part of S2.5: WP-S24-3 cross-target, pagination/index optimization, public Find Usages, and everything in item 8.

**Not solely a testing phase.** Authoritative texts assign bounded non-test work to S2.5: authentic-fixture acquisition/binding (SQ-04), Linux CI qualification infrastructure (SQ-05), and the canonical component-digest derivation completion if independent qualification demonstrates it is not yet canonical (S-01). Conversely, no new functionality, API, schema, migration, dual-write, or optimization may be introduced: where evidence qualification satisfies a requirement, no code is authorized.

**Explicit exclusions (standing).** Public Find Usages (ADR-CI1-02 absent); new traversal semantics; MVCC/snapshot isolation; migration/dual-write; schema-format change; incremental/native-node-ID optimization; distributed/background indexing; package publication; crash/power-loss/multi-process claims; full platform certification (Stage 6); WP-S24-3 cross-target.

---

## 3. Stage-2 obligation reconciliation matrix (A2)

Classification legend: **D** fully discharged · **P** partially discharged · **F** open with feasible qualification path · **N** open, lacks fixtures/infrastructure (owner input needed) · **X** properly deferred beyond S2.5 · **C** potential conflict requiring an owner decision.

| # | Obligation | Authority (exact source) | Original requirement | Accepted evidence | Open gap | Class | S2.5 owns | Blocks Stage-2 accept | Required work/evidence | Qualification gate | Owner decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | **SQ-04** authentic historical fixtures | RECON §2 SQ-04; REM §7; register §6; matrix §13; F15 | Immutable prior-writer master(s) + per-instance hashes + disposable copies + refusal tests; no-writer formats = explicit limitation + amendment | P13b pre-REM authentic refusal (REM-01-Q1, re-hashed §4); R2 H-01/H-02/H-03 baseline graph-only fixture | Repo-side fixture binding + fresh refusal receipts at the reviewed SHA; no authentic writers for the named experimental codecs | **F/N + C** | Yes (available formats) | **Yes** (available formats); remainder via amendment | §4 path; explicit limitation list for no-writer formats | WP-S25-1 → independent review + owner | Amendment for no-writer formats |
| 2 | **SQ-05** expanded Linux native qualification | RECON §2 SQ-05; REM §7; register §6; matrix §14; F15 | Current expanded `Andirit.LatticeDb.Tests` passes on Linux; CI pipeline logs; platform behavior matrix | None for the current suite; hosted CI record only for pre-S2.2-era commits (inspected, not current) | Linux execution bound to the reviewed SHA; behavior matrix (locking/WAL/mmap) | **F/N** | Yes | **Yes** | §5 path (owner-authorized push or native runner) | WP-S25-2 → independent review + owner | Push/runner authorization; WSL2 policy if proposed |
| 3 | **SQ-06** remainder | RECON SQ-06; register §6; S2.3 accept §3/§7 | Namespace-mismatch refusal test | Closed for the S2.3 surface (`S23Wp5NamespaceRefusalTests`; P05/P06 fresh probes) | Register closure step + Stage-2 placement; re-verification in the S2.5 floor | **P** (light) | Yes (light) | No | Confirm the test in the S2.5 regression floor; record closure at acceptance | WP-S25-4 reconciliation | Recorded at Stage-2 acceptance |
| 4 | **S-01** integrated proof | RECON §5; register §7; G-ACCEPT §18; S-01 disposition §5/§7 | Full chain proof incl. general source canonicalization of component digests | S2.1 byte/tree/context; S2.2 live minting/frozen vectors; S2.3/S2.4 persisted-identity cross-checks; FINAL-Q1 primitive cross-checks | General canonical derivation + integrated proof; production derivation exists but is unqualified | **F** (central) | Yes | **Yes** | §6 path; adversarial canonicalization cases; independent oracle | WP-S25-3 → independent review + owner | Bounded correction only if a gap is demonstrated |
| 5 | **D4.5** cancelled staging | register §7 (D4 row); matrix §9 row 5 / §12 | Cancelled staging exposes no partial data | Not counted as exercised (explicit) | Nothing for S2.5 | **X** (Stage 6) | Record only | No | Record; do not count as exercised | WP-S25-4 reconciliation | None for S2.5 |
| 6 | **Full S-02** (ten-invariant native matrix) | RECON §5 S-02; register §5/§7; matrix §12 | All ten invariants native incl. cancellation/stress | Applicable subset (1–4, 6–9 native; 10 serialized) | Full matrix; cancellation; stress | **X** (Stage 6; applicable subset + schedule is S2.5) | Applicable part | Only applicable subset | Applicable-invariant integrated evidence; record remainder | WP-S25-4 | None for S2.5 |
| 7 | **D3-full** | RECON §5 D3; register §5/§7; matrix §8 | Universal graph noninterference across real ingestion | Synthetic + real-fact L16 noninterference; fresh non-leak assertion (Q-I08 reuse) | Real-fixture native-mirror observations; full canonical corpus | **P** | Yes (real-fixture part) | **Yes** (real-fixture part) | §7 path; reviewer-owned before/after graph snapshots | WP-S25-4 | None for S2.5 |
| 8 | **IF-02** residual | RECON §3; register §6; WP-S24-2 accept; SURFACE-Q1; FINAL-Q1 | Real target sizes/order/end-coordinates/planner bounds/pagination | WP-S24-2 producer + SURFACE-Q1 (frozen corpus + generated-real); FINAL-Q1 F4 at 1/10/100/1000 | S3-obs-01 (10000/page-1 omitted — accepted qualification) | **D/P** | Yes (reconcile) | No (with S3-obs-01 preserved) | Reconciliation; no repeat of discharged tests | WP-S25-4 | None |
| 9 | **IF-05** residual | register §6; WP-S24-2 accept; FINAL-Q1 | Exhaustive bounded refusal mapping; no universal open-time promise | 13/13 mapping incl. authority-bearing mismatch (F3); Q2-P1-01 closed; S1-obs-01 preserved | Reconciliation only | **D/P** | Yes (reconcile) | No | Reconciliation; preserve S1-obs-01 | WP-S25-4 | None |
| 10 | **SQ-07** residual | RECON §2 SQ-07; register §6; matrix §11; WP-S24-2 accept | Real-project publication/reopen/query measurement; no thresholds; no optimization | WP-S24-2 characterization + SURFACE-Q1; S4-obs-01 variance; F-OBS-Q1-01 preserved | Reconciliation into the S2.5 report | **D/P** | Yes (reconcile) | No | Include measurements in reconciliation; keep no-threshold / no-optimization | WP-S25-4 | None |
| 11 | **SQ-02** unknown outcome | RECON §2 SQ-02; register §6; S2.3 accept §3 | Conservative unknown-outcome handling; native fault/reopen evidence | NEW-01 closed; managed-fault recovery/quarantine evidence (L9/L10/L20; I-02) | Integrated reconciliation; crash/power-loss **not** claimed | **P** | Yes | Yes (integrated part) | Reuse/reconcile; preserve managed-fault limits | WP-S25-4 | None |
| 12 | **SQ-03** concurrency | RECON §2 SQ-03; register §6 | Native schedule test; serialized model truthful | S2.3 schedule (L12/L12b/P07) bounded claim; S2.4 per-page serialization | Reviewer reconciliation within the final qualification | **P** | Yes | Yes | Re-verify schedule assertions; no MVCC | WP-S25-4 | None |
| 13 | **IF-01** contributor union | RECON §3 IF-01; register §6 | Union of identical semantic facts; distinct units distinct; conflict refusal | S2.2/S2.3 evidence; F-01 restriction (no unrestricted real-authority union) | Preserve F-01; no broader claim | **P** | Yes (reconcile) | No | Reconciliation | WP-S25-4 | None |
| 14 | **IF-03 / IF-04 / F10** | RECON §3; register §6 | Coverage vocabulary; reader continuation; publication-change handling | REM-01 contract; S2.4 WP-S24-1/WP-S24-2 + FINAL-Q1 (owner-accepted) | None material | **D** | Yes (reconcile) | No | Reconciliation | WP-S25-4 | None |
| 15 | **I-05 / M-01 / MD-01…03** | register §6–§7 | No experimental public hook in production; fixture-handoff records; manifest scheme; no public API | Standing constraints observed at each slice | S2.5 verification (I-05); apply to the SQ-04 handoff (M-01) | **P** | Yes | I-05 standing | Check + record | WP-S25-4 | None |
| 16 | **Q2-F11 / Q2-F12** | register §7 | Older-format refusal evidence; platform and crash nonclaims | Same as SQ-04/SQ-05; explicit nonclaims | Covered by rows 1–2 | **P/F** | Via rows 1–2 | Yes (via SQ-04/05) | Records retained | WP-S25-1/2 | Amendment (SQ-04) |

**Classification notes.** No obligation was marked discharged merely because related regression tests passed; rows 8–10 are marked `D/P` only because the corresponding obligations were **owner-accepted** at S2.4 (accept decision §3/§7) with the named observations preserved. No conflict beyond the anticipated SQ-04 no-writer amendment and the SQ-05 route decision is identified. D4.5 and full S-02 are Stage 6 assignments and are recorded, not counted, per the matrix §9/§12.

---

## 4. SQ-04 authentic historical fixtures (A3)

**Inventory and provenance (re-verified read-only in this assessment).**

1. **Pre-REM authentic occurrence store** — `evidence/prerem-authentic.ltdb` (+ `-wal`) in the accepted REM-01-Q1 package (`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-rem-01-q1`). Written by the **pinned Q1B baseline production code** (`4c6d26a1d103a62fffe2e2743467eedb5f21afc2`, tree `320e965f…`, extracted with `git archive`), publishing one graph unit and one occurrence fact through the pre-REM path (old four-field `ComputeTargetId`; no canonical `asmctx3:sha256:` prefix). Master **86,016 bytes**, SHA-256 `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885`; WAL SHA-256 `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4`; README + writer sources retained; immutable-master / disposable-copy policy documented. Independently exercised (**P13b**): opening a copy with the final REM-01 store throws `CodeOccurrenceSchemaException` — refusal without reinterpretation.
2. **Baseline graph-only schema-2 store** — `evidence/historical-master/baseline-graph-only.master.db` in the R2 package (`…\adr-ci1-01-exp-02-r2`; controlling reference incorporated by OWNER §F). Written by frozen baseline `CSharpCodeGraphPlugin` + `LatticeCodeGraphStore` (`31c33a934aa13e373d9a3a6ffbc54d789eef092c`, tree `a265f561…`), graph-only publication. **163,840 bytes**, SHA-256 `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0`; per-instance wall-clock caveat and **open-mutation disclosure** recorded; the master is never opened. **H-01/H-02/H-03**: authentic schema-2 graph readable by legacy callers; occurrence analysis is `AnalysisUnavailable` (not successful empty); empty/legacy store `AnalysisUnavailable`.

Both masters were **re-hashed in this assessment and match their recorded hashes**. Neither is currently committed in the Andirit repository; both are authentic prior-writer artifacts (generated by retained historical writer code), not synthesized reproductions.

**Missing evidence.** A repo-bound fixture package — immutable masters + provenance READMEs + per-instance copy procedure + hashes + M-01 writer/commit/command records + open-mutation disclosure — and fresh refusal-test receipts executed at the S2.5 reviewed SHA against disposable copies, with the masters never opened by a store.

**No-writer formats.** The matrix §13 names older experimental codecs (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`). No authentic prior-writer artifact for these exists in the repository or environment: R2 records `"Authentic prior C fact-store writer (none exists)"`, and Andirit never shipped those schemas (D5: no code reads them; no migration). Generating such stores from surviving partial code would be a **reconstruction**, not prior-writer output, and is not a legitimate substitute. Per F15/REM §7 these formats must be **recorded as an explicit limitation and return to the owner for an amendment**.

**Legitimacy.** The two available artifacts can be legitimately obtained: copy from the accepted packages, verify hashes against the recorded/verified values, and bind them in the Andirit evidence chain (committed evidence with `* -text` attributes is the established pattern; copy-then-open per instance preserves the masters). No historical authenticity is manufactured.

**Impact.** SQ-04 does not block S2.5 entry. It **blocks Stage-2 acceptance for the available formats** until the evidence is bound and re-executed; the no-writer remainder is closable only through an explicit owner amendment — it must not silently disappear.

**Bounded alternative.** WP-S25-1 (§9). Do not substitute fresh-store tampering for authentic fixtures.

---

## 5. SQ-05 Linux native qualification (A4)

**Requirement.** Matrix §14: *the entire current expanded `Andirit.LatticeDb.Tests` suite passes on Linux; retain the original 38 Stage 1 tests plus additions; CI pipeline logs before S2.5 acceptance*, plus the platform-specific behavior matrix (file locking, WAL, memory mapping). Register §6 SQ-05 and boundary §11/§14: the expanded Linux native suite is Stage-2 acceptance evidence; full Windows/Linux/macOS certification remains Stage 6. The suite today is **52 tests** in `Andirit.LatticeDb.Tests` (FINAL-Q1 receipt: 52 + 157 + 345 + 353), superseding the 43 reviewed by REM-01 and the original 38 — the "original 38 plus additions" retention is satisfied by the current suite.

**Infrastructure.** `.github/workflows/ci.yml` already runs the solution tests on a `[windows-latest, ubuntu-latest, macos-latest]` matrix, including `Andirit.LatticeDb.Tests` with coverage gates, triggered on push to `main` and on pull requests. `benchmarks.yml` is Windows-only (not usable); `publish.yml` is a release job (out of scope). No self-hosted runner or local native Linux execution environment is identified in the repository.

**Existing Linux evidence.** A hosted-CI record exists only for an **older** commit (`ec6d-qual-02`: run `40991194763`, head `503b581`, ubuntu job success) — inspected-record-only, and predating the S2.2–S2.4 expanded suite. The S2.2/S2.3 reviews state the hosted CI "covers the solution suites on three OSes only after the next owner-controlled push", and nothing in this work stream has been pushed (0 behind / 65 ahead of `origin/main`). Therefore **no Linux evidence exists for the current expanded suite**.

**Native vs compatibility environments.** `ubuntu-latest` is a genuine Linux host (hosted VM); its logs are the evidence class the matrix names ("CI pipeline logs"). WSL2, Docker-on-Windows, or other Windows-hosted compatibility environments are **not** native Linux execution; the accepted requirement does not explicitly allow them, so a WSL2 result must not be presented as SQ-05 closure without an owner amendment. The S2.2-era qualification "hosted CI is not independent cross-platform qualification" remains binding as an independence statement: hosted logs require independent reviewer verification of SHA-binding and completeness; a reviewer-controlled native Linux execution would be stronger.

**Path.** Owner-authorized publication of a reviewed commit to `origin` (push or PR) → ubuntu-latest CI run bound to that SHA → suite logs; **or** an owner-provided native Linux host/VM under reviewer control executing the suite. Either route is a bounded owner decision (push authorization or runner provision). What can be reused from Windows: test definitions, expected semantics, and the reviewed SHA only — **no Windows result counts as Linux evidence**. What must be executed independently on Linux: the actual test execution (locking/WAL/mmap/path/persistence/reopen/publication/recovery/authority behavior as covered by the suite) plus the behavior matrix.

**Impact.** SQ-05 does not block S2.5 entry; it blocks Stage-2 acceptance until passed. No Linux execution is performed by this assessment.

---

## 6. S-01 / D4.5 / S-02 integrated proof assessment (A5)

**S-01 — proof chain.** Demonstrated: S2.1 verified acquisition (source bytes ↔ Roslyn tree, options/references, scope/universe membership); S2.2 live minting, frozen-vector reproduction and refusal; S2.3/S2.4 persisted-identity cross-checks; FINAL-Q1 probes cross-checked production primitives against frozen R2 authority values before asserting. **Not established:** the *general canonical derivation* of `identifierDigest`/`declarationDigest`/`shapeDigest` from the verified live compilation, and the full integrated chain proof. This is the S2.5 completion gate per the [canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 (do not reopen S2.2; do not declare it satisfied). Since S2.3 a production derivation exists (`CSharpCodeGraphPlugin.Session`: `OccurrenceIdentifierDigest` / `OccurrenceDeclarationDigest` / `OccurrenceShapeDigest` over the normalized symbol, wired into the extractor), but its canonicality for general source is unqualified. **Approach:** independent qualification first (adversarial cases: generics, nested/explicit-interface members, ref/out/params/optional parameters, operators/constructors, nullable/tuple/generic type formatting, overloads, cross-project binding, external refusal), with expected values derived independently of the implementation; if the qualification demonstrates a genuine gap, the narrowest bounded canonicalization correction becomes a prerequisite under its **own** owner authorization — no correction is presumed or authorized here. This is a **missing proof**, not a demonstrated production defect.

**D4.5 and full S-02.** Both are assigned to **Stage 6** by the authoritative matrix (§9 row 5: "explicitly deferred; not counted as exercised"; §12) and the register ("invariant 5 and full S-02 Stage 6"). S2.5 must **record** them and deliver the *applicable* subset: recovery/reopen evidence (SQ-02), the native serialized schedule re-verification (SQ-03), bounded `Examined`, and the D4 invariants already exercised.

**Durability boundary.** F-02 stands: managed fault injection ≠ crash/power-loss/abrupt-kill/multi-process durability. Actual crash or power-loss testing is **not** required by S2.5, would need new instrumentation and explicit safety scoping, and remains a separately commissioned Stage 6 qualification (Q2-F12). No destructive crash testing is proposed. The managed injectors remain internal/test-only (I-05).

---

## 7. D3-full evidence-gap assessment (A6)

**Proven.** Synthetic D3 noninterference (Stage 1; preserved); real-fact noninterference `L16` across publish → same-unit replacement → scoped deletion → durable reopen (node/edge/count/traversal snapshots identical; occurrence facts never appear as graph nodes or edges); fresh non-leak assertion at WP-S23-5 (Q-I08 — integration-level D3 reused plus a fresh node/edge assertion).

**Gap.** The register names the **native-mirror path not enabled in the reviewer probe**; the full canonical real-project corpus remains Stage 6; a fresh multi-project-graph-only lifecycle variant was not separately executed (Q-I08). No specific demonstrated D3 gap exists — this is evidence completeness, not a defect, and **no architectural change is required**.

**S2.5 residual.** Reviewer-owned real-fixture observations over the frozen R2 corpus: graph identity/cardinality/callers/references/traversal before and after occurrence publication, plus the native-mirror consistency observations, with independently derived before/after snapshots. Evidence-only; accepted D3 behavior is not reopened.

---

## 8. Residual IF-02 / IF-05 / SQ-07 (A7)

- **IF-02** — discharged at the S2.4 surface: real target sizes/order/end-coordinates/planner bounds in WP-S24-2; independent frozen-corpus and generated-real oracles in SURFACE-Q1; integrated traversal at page sizes 1/10/100/1000 in FINAL-Q1 (all owner-accepted). Residual: **S3-obs-01** (10,000/page-size-1 omitted — accepted qualification) and reconciliation into the S2.5 report. No repeat of discharged tests.
- **IF-05** — discharged to the demonstrated boundary: 13/13 mapping including the authority-bearing page/publication mismatch (FINAL-Q1 F3), with **Q2-P1-01 closed**; **S1-obs-01** (distinct malformed occurrence vs graph payload paths) preserved. The register's "do not falsely promise every mismatch is detected at open" remains binding. Residual: reconciliation only.
- **SQ-07** — characterization delivered (producer harness + independent SURFACE-Q1 measurements; publication/reopen/query separated; **no thresholds**; **S4-obs-01** variance preserved). **F-OBS-Q1-01** remains an observation, not a contractual violation. Residual: fold the measurements into the S2.5 reconciliation; keep the characterization-vs-guarantee distinction; optimization remains unauthorized.

No retrospective performance thresholds are introduced, and no discharged obligation is repeated.

---

## 9. Proposed S2.5 work-package plan (A8) — drafts; NOT AUTHORIZED

All packages: documentation/tests/evidence only unless a field states otherwise; preserve D3/D4/D5, the S2.3/S2.4 accepted contracts, MD-03 (no public API), the Windows-x64 and serialized single-writer boundaries (plus the Linux qualification explicitly required by SQ-05); self-excluding SHA-256 manifests; zero unauthorized production change. The owner may merge packages where dependencies justify it; the split below keeps fixture acquisition, Linux infrastructure, the S-01 proof, and the final integrated qualification separately gateable.

### WP-S25-0 — S2.5 entry/design owner decision (documentation only)

| Field | Content |
| --- | --- |
| Scope / obligations | Adopt the S2.5 scope of §2; adopt this decomposition and its gates; fix the SQ-04 no-writer amendment policy and the SQ-05 evidence route; fix the S-01 completion-gate contract; confirm no architecture expansion and no successor scope |
| Entry prerequisites | This assessment committed; S2.4 accepted (HEAD `2fbf209`) |
| Environment / fixtures | None (documentation only) |
| Change surfaces | Decision document + additive S2.5 planning note only |
| Evidence | Decision record; no execution |
| Independent review | None required (owner decision over committed assessment) |
| Blocking failures | Any scope expansion or boundary conflict |
| Owner acceptance | The decision itself; explicitly authorizes no package |
| Exclusions | Implementation; qualification; Linux execution; Stage-2 acceptance |

### WP-S25-1 — Authentic historical fixture binding + refusal evidence (SQ-04)

| Field | Content |
| --- | --- |
| Scope / obligations | Bind the two authentic masters (§4) with provenance, M-01 records, disposable-copy procedure and `* -text` evidence attributes; execute refusal/availability checks against copies at the reviewed SHA (pre-REM store → store-wide `CodeOccurrenceSchemaException`; baseline graph-only → legacy graph readable + occurrence `AnalysisUnavailable`); record the no-writer format limitation list for owner amendment |
| Entry prerequisites | WP-S25-0 adoption; owner commission |
| Environment / fixtures | Windows x64; external masters copied from the accepted packages (hash-verified); no fabrication |
| Change surfaces | Additive evidence package + tests/scripts only; no production change; stores opened only as disposable copies |
| Evidence | Master/copy hashes before and after; open-mutation disclosure; test logs; manifest; explicit format-availability matrix |
| Independent review | Reviewer ≠ producer; fresh-process refusal receipts verified against the hash-bound copies |
| Blocking failures | Hash mismatch against recorded values; any reinterpretation/migration path; tampering substituted for authenticity |
| Owner acceptance | Separate owner decision; the no-writer amendment recorded here or at Stage-2 acceptance |
| Exclusions | Migration/dual-write; store repair; historical reinterpretation; claims about formats without authentic writers |

### WP-S25-2 — Expanded Linux native qualification (SQ-05)

| Field | Content |
| --- | --- |
| Scope / obligations | Execute the current expanded `Andirit.LatticeDb.Tests` suite on genuine Linux via the committed CI matrix at a reviewed SHA (or a reviewer-controlled native Linux host); produce logs + platform behavior matrix (locking/WAL/mmap/paths); document reuse boundaries |
| Entry prerequisites | WP-S25-0 adoption; owner authorization for the evidence route (push/PR or runner) |
| Environment / fixtures | ubuntu-latest GitHub Actions runner (workflow exists) or an owner-provided native Linux host; nothing pushed without an owner decision |
| Change surfaces | CI logs/evidence; no production change. A `workflow_dispatch` affordance, if desired, is a separate explicitly authorized minimal CI change |
| Evidence | CI run identity bound to the reviewed SHA; per-suite logs; behavior matrix; manifest |
| Independent review | Reviewer verifies SHA binding, suite completeness (original 38 + additions), and log integrity; hosted logs are not independent execution — the reviewer's verification is the independence step; a reviewer-controlled native run is preferred if available |
| Blocking failures | Suite failure on Linux (→ bounded remediation decision, separate authorization); log↔SHA mismatch; a compatibility environment presented as native Linux |
| Owner acceptance | Separate owner decision |
| Exclusions | macOS; full platform certification; crash/power-loss; MVCC/multi-process; performance claims |

### WP-S25-3 — S-01 integrated proof (incl. canonical digest derivation qualification)

| Field | Content |
| --- | --- |
| Scope / obligations | Independent proof: acquired bytes → verified compilation (`compctx1`/`compctx2`) → binding → canonical component digests → `asmctx3`/`usage4`, over the production plugin path; adversarial canonicalization suite; frozen R2 vectors + independently derived expected values; encoding/metadata boundary rechecks (bounded Q-F2/Q-F3) |
| Entry prerequisites | WP-S25-0 adoption; reviewed source fixed; production path unchanged (this assessment presumes no defect) |
| Environment / fixtures | Windows x64; frozen R2 corpus + generated-real; fresh processes |
| Change surfaces | Reviewer probes + evidence only; if a demonstrated gap requires correction, **stop** → narrowest remediation boundary + separate bounded owner authorization (not granted here) |
| Evidence | Probe receipts; identity cross-checks against frozen authority; oracle outputs; manifest |
| Independent review | Reviewer ≠ implementer; expected values derived from accepted contracts, not from the implementation |
| Blocking failures | Any demonstrated non-canonical identity behavior, frozen-vector mismatch, or refusal violation → blocking finding |
| Owner acceptance | Separate owner decision; remediation decisions separately authorized |
| Exclusions | Identity-scheme change; public API; cross-target; optimization |

### WP-S25-4 — Final integrated Stage-2 qualification (the S2.5 gate)

| Field | Content |
| --- | --- |
| Scope / obligations | The §2 outcomes as a whole at a frozen reviewed SHA: independent semantic positive/negative reproduction; real-project end-to-end through plugin → sink → native store → reopen → reader with direct native inspection; applicable D3 (incl. native-mirror observations, §7); applicable D4 (recovery/reopen, serialized schedule, bounded `Examined`) + D5; reconcile IF-01…IF-05, MD-01…MD-03, I-03…I-05, SQ-02/SQ-03/SQ-06 remainder and the S1/S3/S4 observations; consume WP-S25-1/2/3 outputs; record D4.5 / full S-02 / D3-full / crash-power-loss as remaining (not discharged); produce the complete obligation reconciliation required before Stage-2 owner acceptance |
| Entry prerequisites | WP-S25-1, WP-S25-2, WP-S25-3 completed and independently reviewed; reviewed SHA adopted |
| Environment / fixtures | Windows x64 (plus the Linux evidence from WP-S25-2); frozen R2 corpus; authentic fixture evidence from WP-S25-1 |
| Change surfaces | Reviewer tests/probes/evidence + report; no production change |
| Evidence | Integrated matrix with fresh/reused classification; gate logs (full solution, frozen oracle 185/185 byte-identical, Release 0/0, format 0); manifests; historical-evidence byte check |
| Independent review | Reviewer ≠ implementer; fresh processes; expected values from accepted contracts; overlapping counts not aggregated as separate assurance |
| Blocking failures | Any authority/ordering/coverage/refusal/identity violation; false-available or false-complete; evidence-integrity failure; any unreconciled obligation |
| Owner acceptance | Report → **separate Stage-2 owner acceptance decision** (§10); not automatic |
| Exclusions | New functionality; thresholds; optimization; Stage 6 work; public Find Usages; cross-target |

**Ordering.** WP-S25-0 → (WP-S25-1 ∥ WP-S25-2 ∥ WP-S25-3) → WP-S25-4 → separate Stage-2 owner acceptance decision.

---

## 10. Stage-2 closure dependency map (A9)

| Stage / milestone | Acceptance status | Binding qualifications (carried) | Remaining prerequisites | Independent qualification | Owner decision | Blocked even after S2.5? |
| --- | --- | --- | --- | --- | --- | --- |
| Stage 1 native foundation | Accepted with qualifications (S1 + REM-01/Q1B history) | SQ-01 boundary; D5; historical packaging erratum | — | Historical reviews | Recorded | No |
| S2.0 corpus (F12) | `F12 CLOSED — ACCEPTED WITH QUALIFICATIONS` | Windows scope; reference-oracle boundary; native integration at S2.3–S2.5 | — | Q3 corpus | Recorded | No |
| S2.1 provenance | Accepted with qualifications | Bounded encoding/metadata handling (Q-F2/Q-F3 dispositions recorded at S2.2); discovery not general MSBuild equivalence | — | S21Q1/Q1b | Recorded | No |
| S2.2 extraction | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` (G-ACCEPT) | **S-01 digest canonicalization**; F09 heuristics; metadata equivalence; platform; no publication credit | S-01 → S2.5 | G-* gates | Recorded | No |
| S2.3 publication | Accepted with qualifications | F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; same-harness limitation; Windows/serialized; managed-fault limits | SQ-04/05/S-01 via S2.5 | S23 gates | Recorded | No |
| S2.4 reader/coverage | Accepted with qualifications | Plus F-OBS-Q1-01/Q1-03, Q2-OBS-01, S1/S3/S4 observations; WP-S24-3 deferred | SQ-04/05/S-01/D3-full residues via S2.5 | S24 gates + FINAL-Q1 | Recorded | No |
| **S2.5** | Entry assessed here (`ENTRY READY WITH QUALIFICATIONS`) | Inherits all above; adds the SQ-04 amendment, the SQ-05 route, and the S-01 contingency | WP-S25-0…4 | Reviewer ≠ implementer per package; final integrated gate | Separate decisions per package + Stage-2 acceptance | **Yes if SQ-04/05/S-01 fail or the owner withholds the amendment/authorization** — each has a named narrowest resolution |
| Stage 6 / later CI-1A | Not authorized | — | Full D3 corpus; all-ten S-02/D4 (incl. D4.5 cancellation, stress); full platform certification; crash/power-loss/multi-process if separately scoped | Stage 6 gates | Separate Stage 6 decisions | Not part of Stage-2 closure |
| Public Find Usages | Not authorized (ADR-CI1-02 absent) | MD-03 internal-only boundary | Separate architecture/feature decision | Separate | ADR-CI1-02 owner decision | Not part of Stage-2 closure |

**Four distinct milestones (none automatic):** (1) S2.5 work-package completion — packages executed and qualified; (2) **S2.5 owner acceptance** — each package separately decided; (3) **Stage-2 acceptance** — the separate owner decision after WP-S25-4's complete reconciliation, subject to SQ-04/05/S-01 closure; (4) Stage 6 / public Find Usages — separate authorizations entirely beyond Stage 2. Stage-2 closure remains **contingent** on the SQ-04 amendment, the SQ-05 Linux pass, and the S-01 proof; all other bounded residues are reconciliation items.

---

## 11. Binding qualifications and exclusions (A10)

**Preserved and binding (no reinterpretation):** F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08; the **same-harness reviewer independence limitation**; **Windows-x64** and the **serialized single-writer** process/store model; **managed fault-injection durability limits**; **restricted real-authority cross-unit union evidence**; **F-OBS-Q1-01** (target-local `O(n log n)` per-page sorting — observation, not a violation); **F-OBS-Q1-03** (unkeyed checksum, not authentication); **Q2-OBS-01**; **S1-obs-01, S3-obs-01, S4-obs-01**. F-Q1-01 and F-OBS-Q1-02 remain resolved; Q2-P1-01 remains closed (including the authority path).

**Standing boundaries:** no public Find Usages API; **WP-S24-3 remains optional, deferred and unauthorized** (exact-target contributor aggregation is within the accepted scope; multi-target enumeration is not included); no new persisted continuation sessions; no storage/schema migration; no dual-write; no index redesign; no pagination/index optimization; no cross-target implementation; no Stage 6 development; no crash/power-loss/multi-process claims; no full platform certification (Stage 6).

**Conflict protocol.** If a mandatory S2.5 requirement cannot be satisfied within these boundaries (for example, a needed canonicalization correction, a WSL2-only Linux option, or an SQ-04 format with no authentic writer), stop the affected path and return a **separate, explicit owner decision** with the narrowest resolution — do not silently expand the accepted architecture or substitute evidence classes.

---

## 12. Entry disposition and rationale (A11)

**`ENTRY READY WITH QUALIFICATIONS`.**

**Rationale.** The S2.5 scope, obligations, gates and exclusions are authoritatively defined across the boundary, matrix, register, the S2.2/S2.3/S2.4 decisions and the canonicalization disposition; all prerequisite stages (S2.0–S2.4) are accepted with qualifications; the evidence chain is intact (eight manifests with 0 mismatches; the external authentic fixtures re-hash exactly); and every remaining obligation has a feasible bounded path with no unresolved authority conflict. Entry is **qualified**, not unconditional, because: (1) no committed S2.5 work-package plan exists yet — §9 is a draft pending WP-S25-0; (2) SQ-04's no-writer formats require an explicit owner amendment, and the available authentic masters must be bound into the repo evidence chain; (3) SQ-05 needs an owner-authorized push or native-Linux route, and hosted logs require independent verification (WSL2 does not qualify absent an amendment); (4) S-01's general canonical derivation is an explicit S2.5 completion gate that may — only if a gap is demonstrated — require a bounded correction under separate authorization; (5) all carried qualifications remain binding. No condition rises to `ENTRY BLOCKED`: there is no missing prerequisite, no evidence-integrity failure, no demonstrated defect, and no requirement that demonstrably exceeds the accepted architecture.

---

## 13. Proposed next commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.5-ENTRY-OWNER-01` — a bounded, documentation-only S2.5 entry/design owner decision that fixes the S2.5 contract and scope, adopts the work-package decomposition and gates (§9), decides the **SQ-04 no-writer amendment policy**, selects the **SQ-05 evidence route** (owner-authorized push/PR CI run, or an owner-provided native Linux runner), fixes the **S-01 completion-gate boundary** (qualification first; bounded correction only on demonstrated need), and preserves the WP-S24-3 deferral. Only after that decision do WP-S25-1/2/3 become eligible for separate commissions, each with its own independent gate and owner decision, followed by WP-S25-4 and the separate Stage-2 owner acceptance.

**Not authorized by this assessment:** any S2.5 package; any implementation or production change; any qualification execution (including Linux); any fixture import; any push/PR; S2.5 owner acceptance; Stage-2 acceptance; Stage 6; public Find Usages; optimization; WP-S24-3.

---

## 14. Missing owner decisions (recorded, not resolved)

| # | Missing decision | Blocking point | Needed authority |
| --- | --- | --- | --- |
| M-A | Adoption of the S2.5 scope, decomposition and gates | all S2.5 packages | Owner (WP-S25-0) |
| M-B | SQ-04 amendment for formats without authentic prior writers; approved fixture-binding procedure | WP-S25-1 / Stage-2 acceptance | Owner |
| M-C | SQ-05 evidence route and push/runner authorization; WSL2 policy if proposed | WP-S25-2 | Owner |
| M-D | S-01 completion-gate boundary: acceptance criteria for the canonical derivation; remediation authorization policy if a gap is found | WP-S25-3 | Owner |
| M-E | WP-S24-3 remains deferred (confirmation only; no reopen) | standing | Owner (already recorded; preserve) |
| M-F | Stage-2 acceptance criteria confirmation (what must be recorded vs discharged at the acceptance decision) | closing gate | Owner |

---

*Additive assessment only. No production code, test, qualification, fixture, oracle/corpus, historical evidence, planning contract, or prior decision was modified. No push occurred.*
