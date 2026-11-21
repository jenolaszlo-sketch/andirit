# S2.5 work-package plan — independent Stage-2 qualification (S-01 proof, authentic historical refusals, Linux minimum, final integration)

**Commission:** `ANDIRIT-CI1A-S2.5-ENTRY-OWNER-01`. Documentation/plan only.
No package authorizes its successor. Each package requires its own commission,
independent qualification, and owner decision.

**Controlling authority:** the [S2.5 entry owner decision](../../decisions/adr-ci1-01-ci1a-s2-5-entry-owner-01.md)
(fixes this decomposition and the S-01 proof contract), the
[Stage-2 boundary](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8 (S2.5)/§9/§10/§11,
the [Stage-2 acceptance matrix](../adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §13–§16,
the [S2.5 entry assessment](../../reviews/ANDIRIT-CI1A-S2.5-ENTRY-ASSESS-01.md),
the [S1 obligation reconciliation](../../decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md) SQ-04/SQ-05/S-01/S-02,
the [obligation register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md) §6–§8,
the [S-01 canonicalization disposition](../adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md),
the [S2.3 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-3-accept-owner-01.md), and the
[S2.4 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-4-accept-owner-01.md).

**Global boundaries (all packages):** Windows x64 and the serialized single-writer model
(plus the mandatory genuine-Linux qualification evidence of WP-S25-2); no public Find Usages
(MD-03; ADR-CI1-02 absent); no cross-target (WP-S24-3 remains optional, deferred,
unauthorized); no persisted continuation sessions; no storage/schema migration; no
dual-write; no pagination/index optimization; no Stage 6 development; preserve all S2.3/S2.4
binding qualifications (F-01, F-02, F-03, Q1-Q1, Q1-Q2, S-01 restriction, Q2-Q01, Q2-Q02,
O-Q2-01, Q-I01, Q-I08, F-OBS-Q1-01, F-OBS-Q1-03, Q2-OBS-01, S1/S3/S4 observations, the
same-harness independence limitation, and the managed-fault durability limits). Self-excluding
SHA-256 evidence manifests for every package; zero unauthorized production change.

---

## WP-S25-1 — SQ-04 authentic historical fixture binding and refusal evidence

| Field | Content |
| --- | --- |
| Obligation ownership | **SQ-04** (available formats); M-01 handoff records; Q2-F11/F12 records; F-03 status; D5 refusal preservation. **No-writer codec register** (below) defers the remaining formats to an explicit owner amendment |
| Scope | Authenticate the two existing prior-writer masters; establish repository-side evidence bindings; verify original bytes, complete hashes, provenance and expected refusal behavior; define reproducible native refusal qualification; separate authentic historical evidence from newly constructed controls |
| Authoritative inputs | Pre-REM master + WAL + README + writer sources (REM-01-Q1 package); R2 baseline graph-only master + `provenance.json` + historical-matrix evidence (R2 package); acceptance matrix §13; REM §7; F15; M-01 |
| Recorded complete hashes (re-verified 2026-11-09; **re-verify at package entry**) | `prerem-authentic.ltdb` = `7646b98288a9803cff6f9930c78a3e7df5c676f6fc4e0cd2df2af5e79bc68885` (86,016 bytes); `prerem-authentic.ltdb-wal` = `8ca170cdc55a4428a5506bcb447a0667ab10c92c77ef149ae36460a5603aeab4`; `baseline-graph-only.master.db` = `f21a72bee2c2e378e16766ca7b84c29010ab4d50360bf3111a82706f947781e0` (163,840 bytes). **Abbreviated hashes are not verification values** |
| Entry prerequisites | S2.5 entry decision recorded; separate owner commission of WP-S25-1; clean canonical `main` at the reviewed SHA; external masters located and re-hashed against the complete recorded values |
| Required fixtures and environments | The two external masters, **copied** into the evidence package and **never opened in place**; Windows x64; fresh processes; per-instance disposable copies |
| Permitted change surfaces | Additive evidence package under `docs/reviews/` + additive tests/scripts only; no production change; no master regeneration or alteration; no store opened in place |
| Required evidence | Master/copy hashes before and after execution; open-mutation disclosure (for the graph-only store); refusal receipts (pre-REM → store-wide `CodeOccurrenceSchemaException`; graph-only → legacy graph readable + occurrence `AnalysisUnavailable`, not successful empty); format-availability matrix; M-01 writer/commit/command records; classification of authentic vs constructed controls; self-excluding manifest |
| Independence requirements | Reviewer ≠ producer; fresh-process refusal receipts verified against the hash-bound copies; expected behaviors derived from the accepted P13b / H-01…H-03 evidence, not from the new runs alone |
| Blocking failures | Hash mismatch against recorded values; any reinterpretation or migration path; tampering substituted for authenticity; a master opened in place; a synthetic fixture represented as authentic historical evidence |
| Qualification disposition | Report → `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED` |
| Owner-acceptance gate | Separate `WP-S25-1-OWNER` decision; records the no-writer amendment requirement (bounded format list) or the acceptance blocker if no exception is permitted |
| Explicit exclusions | Migration/dual-write; store repair; historical reinterpretation; claims about formats without authentic writers; production change; any “discharge” of SQ-04 beyond the available formats |

---

## WP-S25-2 — SQ-05 expanded Linux native qualification

| Field | Content |
| --- | --- |
| Obligation ownership | **SQ-05**; Q2-F12 (Linux minimum); acceptance matrix §14; register §6. Full Windows/Linux/macOS certification remains Stage 6 |
| Scope | Execute the current expanded `Andirit.LatticeDb.Tests` suite on **genuine Linux**; produce native execution receipts; behavior and refusal matrix; regression and environment evidence; independent review. Verify the authoritative suite and requirements at package entry (assessment observed 52 tests in `Andirit.LatticeDb.Tests`; the original 38 plus additions must be retained) |
| Execution routes | **A — owner-authorized CI publication and execution**: requires a **separate authorization** identifying the remote publication mechanism, scope, and affected commits **before any push or PR operation**; **entry authorization is not push authorization**. **B — owner-provided genuine Linux runner**: requires reproducible runner metadata and trustworthy native receipts. **WSL2/Docker do not substitute for genuine native Linux evidence unless an explicit normative amendment permits it** |
| Entry prerequisites | Separate owner commission; an approved route (A or B); reviewed commit fixed; CI workflow (`ci.yml` `ubuntu-latest` job) verified present at that commit for route A |
| Required fixtures and environments | The current suite (verify count at entry; retain original 38 + additions); ubuntu-latest runner or native Linux host; **no Windows result counts as Linux evidence** |
| Permitted change surfaces | CI logs/evidence only; no production change. A `workflow_dispatch` affordance, if chosen, is a separate, explicitly authorized minimal CI change |
| Required evidence | CI run identity (or runner metadata) bound to the reviewed SHA; per-suite logs with pass/fail counts; behavior matrix (file locking, WAL, memory mapping, paths, persistence, reopen, publication, recovery, authority as covered by the suite); environment record; self-excluding manifest |
| Independence requirements | Reviewer verifies SHA binding, suite completeness, and log integrity; hosted logs are **not** independent execution — the reviewer’s verification is the independence step; a reviewer-controlled native run is preferred where available |
| Blocking failures | Suite failure on Linux (→ bounded remediation decision, separate authorization); log↔SHA mismatch; a compatibility environment presented as native Linux; incomplete suite; execution without the separate publication authorization (route A) |
| Qualification disposition | Report → `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED` |
| Owner-acceptance gate | Separate `WP-S25-2-OWNER` decision |
| Explicit exclusions | macOS; full platform certification (Stage 6); crash/power-loss; MVCC/multi-process; performance claims; push/PR without separate authorization |

---

## WP-S25-3 — S-01 integrated proof (central technical qualification package)

| Field | Content |
| --- | --- |
| Obligation ownership | **S-01** (integrated chain incl. general component-digest canonicalization); boundary §8 S2.5 “verify the S-01 proof chain”; G-ACCEPT §18; S-01 canonicalization disposition §5/§7 |
| Scope | Independent integrated S-01 proof: acquired source bytes → verified compilation (`compctx1`/`compctx2`) → live binding → canonical component digests → `asmctx3`/`usage4`; general component-digest canonicalization qualification; independent semantic reproduction; real-project end-to-end behavior; publication, authority, persistence, and native reopen; applicable D3/D4 and D5; the remaining native-mirror observation; relevant refusal and recovery evidence. **Proof methodology defined before implementation or testing** (entry decision §8) |
| Methodology distinctions (explicit) | semantic equivalence · byte-level canonical identity · deterministic digest reproduction · publisher-authority binding · durable reopen · managed recovery · crash/power-loss durability claims (the last remains unclaimed/unqualified) |
| Entry prerequisites | S2.5 entry decision recorded; separate owner commission; frozen reviewed SHA; production path unchanged; **no dependence on unaccepted or unavailable evidence** |
| Required fixtures and environments | Frozen R2 corpus + generated-real workloads; Windows x64 with the serialized store; fresh processes; Linux coverage only as demonstrated by WP-S25-2 within its covered behaviors (uncovered behaviors must be listed explicitly as not Linux-verified) |
| Permitted change surfaces | Reviewer probes/tests/evidence only; **production corrections are not pre-authorized** — a demonstrated defect → stop → separate bounded remediation commission |
| Required evidence | Digest expectations derived independently (frozen R2 vectors + an independent canonicalization oracle — never implementation-generated values as the sole oracle); byte-level canonical identity receipts; deterministic reproduction records; publication/authority binding receipts; durable reopen and managed-recovery receipts; D3 before/after snapshots incl. the native mirror; refusal receipts; provenance classification (fresh / reused / authentic / generated / constructed); self-excluding manifest |
| Independence requirements | Reviewer ≠ implementer; independent oracle; expected values from accepted contracts; the independent qualification is separate from the proof construction |
| Blocking failures | Any frozen-vector/identity mismatch; demonstrated non-canonical digest behavior; authority/refusal violation; false-available; evidence-integrity failure |
| Qualification disposition | Report → `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED` |
| Owner-acceptance gate | Separate `WP-S25-3-OWNER` decision; any remediation is separately authorized |
| Explicit exclusions | Identity-scheme change; public API; cross-target; optimization; crash/power-loss claims; silent expansion of canonicalization semantics beyond the accepted R2/identity contracts |

---

## WP-S25-4 — Final integrated Stage-2 qualification

| Field | Content |
| --- | --- |
| Obligation ownership | The S2.5 gate as a whole (boundary §8/§12); complete obligation reconciliation (register §8); SQ-06 remainder; applicable SQ-02/SQ-03; D3-full real-fixture part; applicable D4/D5; IF-02/IF-05/SQ-07 reconciliation; preparation of the evidence for a separate Stage-2 owner-acceptance decision |
| Scope | Reconcile accepted Stage-2 evidence; independently qualify remaining mandatory interactions; confirm the S-01 proof; verify the SQ-04 and SQ-05 dispositions; reconcile applicable D3/D4/D5 obligations; classify every remaining qualification and deferred requirement; prepare the evidence set for the separate Stage-2 acceptance. **Earlier owner decisions are not substitutes for final integration evidence** |
| Entry prerequisites | WP-S25-1, WP-S25-2, WP-S25-3 completed and independently reviewed; a frozen reviewed SHA; regression floors green (whole solution, frozen R2 oracle byte-identical, Release 0/0, format exit 0); **exact prerequisites defined at commission time from the then-current accepted states** |
| Required fixtures and environments | Frozen R2 corpus; the authentic fixture evidence from WP-S25-1; the Linux evidence from WP-S25-2; Windows x64; fresh processes |
| Permitted change surfaces | Reviewer tests/probes/evidence/report only; no production change |
| Required evidence | Integrated matrix with fresh/reused classification (no aggregation of overlapping counts); gate logs; historical-evidence byte check; complete reconciliation matrix classifying each obligation as discharged / qualified / open-S2.5 / open-Stage6 / blocked / amendment-subject |
| Independence requirements | Reviewer ≠ implementer; fresh processes; expected values from accepted contracts; earlier decisions and producer tests are not integration evidence |
| Blocking failures | Any authority/ordering/coverage/refusal/identity violation; false-available or false-complete; an unreconciled obligation; evidence-integrity failure |
| Qualification disposition | Report → `SUPPORTED` / `SUPPORTED WITH QUALIFICATIONS` / `REMEDIATION REQUIRED` |
| Owner-acceptance gate | The final report feeds a **separate Stage-2 owner-acceptance decision** — not a work package and not authorized here |
| Explicit exclusions | New functionality; thresholds; optimization; Stage 6 work; public Find Usages; cross-target; the Stage-2 acceptance decision itself |

---

## SQ-04 no-writer codec register (amendment subject)

No authentic prior-writer artifact for the following named experimental codecs exists in the
repository or environment; R2 records “no prior C fact-store writer exists”, and Andirit never
shipped these schemas (D5: no code reads them; no migration). **No synthetic fixture may be
represented as authentic historical evidence**, and these cases are **not silently waived**.
Each requires an explicit owner amendment identifying:

| Field | Requirement |
| --- | --- |
| Affected format | Exact codec/format name (e.g., `occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`) |
| Unsupported historical claim | The specific SQ-04 refusal/availability claim that cannot be demonstrated |
| Alternative evidence | Any bounded substitute (clearly labeled non-authentic), or none |
| Residual risk | What remains unverified and its consequence |
| Future obligation | Where authentic evidence would be required if ever claimed (re-encountered historical writer, Stage 6 as applicable) |
| Effect on S2.5 / Stage-2 acceptance | Recorded limitation at acceptance; blocker identification if no permitted exception exists |

If the contract required unavailable authentic evidence **with no permitted exception**, the
affected Stage-2 acceptance claim is **blocked** until an authentic source is found or the
contract is amended by the owner.

---

## Ordering and non-authorization

- Intended order: **WP-S25-0 (this entry decision) → (WP-S25-1 ∥ WP-S25-2 ∥ WP-S25-3) → WP-S25-4 → separate Stage-2 owner acceptance**.
- WP-S25-1 (fixture binding) and WP-S25-2 (Linux infrastructure) may be prepared and
  executed independently where their prerequisites permit; WP-S25-3 must not silently
  depend on unaccepted or unavailable evidence; WP-S25-4 consolidates the qualified results.
- **This plan authorizes nothing.** WP-S25-1, WP-S25-2, WP-S25-3, and WP-S25-4 each require
  their own owner commission, independent qualification, and owner decision. Stage-2 final
  qualification is not complete, and Stage-2 owner acceptance remains unauthorized.
  WP-S24-3 remains optional, deferred, and unauthorized; S2.5/Stage 6, public Find Usages,
  and optimization remain unauthorized.

---

## Authorization status (additive; current state)

This note is additive status only; it changes no package definition, scope,
dependency, or gate above. Package definitions remain the controlling record.

| Package | Status |
| --- | --- |
| WP-S25-1 (SQ-04 authentic historical-fixture binding) | **accepted with qualifications by owner (`adr-ci1-01-ci1a-s2-5-wp-s25-1-owner-01.md`)** — BIND-01 `bf1af23` (`FIXTURE BINDING COMPLETE — READY FOR INDEPENDENT QUALIFICATION`), independent `S25-SQ04-Q1` `3d3980a` (`SUPPORTED WITH QUALIFICATIONS`; 26/0 authentication checks, 4/4 fresh-process reproductions, staging isolation verified). SQ-04 is **satisfied for the two available formats** (pre-REM occurrence store + WAL; R2 baseline graph-only store) with the F-03 refinement, the H-03 inapplicability record, and the experimental-codec gaps preserved. WP-S25-1 is **closed within its bounded qualified scope** |
| SQ-04 (stage-level obligation) | **`SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`** — bounded amendment approved by owner (`adr-ci1-01-ci1a-s2-5-sq04-amend-owner-01.md`): the two authenticated prior-writer fixture sets (WP-S25-1/SQ04-Q1) remain binding; the eight experimental codecs (`occurrence-facts/1..3`, `usage1..3`, `asmctx1/2`) are **excluded from positive historical-compatibility claims** (no authentic retained artifact; writer existence ≠ artifact availability; D5 refusal remains their operative guarantee); future authentic-artifact discovery reopens the obligation. Full historical compatibility is **not** claimed. Decision B of `adr-ci1-01-ci1a-s2-5-wp-s25-1-owner-01.md` is resolved by this amendment |
| WP-S25-2 (SQ-05 expanded Linux native qualification) | **accepted with qualifications by owner (`adr-ci1-01-ci1a-s2-5-wp-s25-2-owner-01.md`)** — G2 authorized execution `df57a2a` (push of pinned `7457284…` to `qualification/ci1a-s25-sq05-linux`, PR #6, run 42744298084) and independent G3 `eb1abef` (`SUPPORTED WITH QUALIFICATIONS`; five-identity chain verified; 52/0/0 LatticeDb on genuine Ubuntu 24.04.5 x64; 345/353/157 supplementary). **`SQ-05 SATISFIED WITH QUALIFICATIONS`** within the qualified boundary. **`F-G2-01 OPEN — REPOSITORY CI COVERAGE-GATE FAILURE`** (branch 65.77% < 70 on all OSes; platform-independent, pre-existing; non-blocking for SQ-05, binding for the CI conclusion; separate owner decision required). WP-S25-2 **closed within its bounded qualified scope** |
| WP-S25-3 (S-01 integrated proof) | **CLOSED WITH QUALIFICATIONS** ([closure record](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md)). **P0 contract decision recorded — `S-01 CANONICAL-FORM CONTRACT ADOPTED WITH QUALIFICATIONS`** (`adr-ci1-01-ci1a-s2-5-wp-s25-3-p0-contract-owner-01.md`; normative contract: [`s01-canonical-form-contract.md`](s01-canonical-form-contract.md) v1 — the Case-B finding is resolved by adopting the production canonical-form semantics). **P1 executed** (`90c5fbee…`; report: [`ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md`](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01.md)) — `S-01 P1 INTEGRATED PROOF COMPLETE WITH QUALIFICATIONS`; the R13a/R13b/R13c subcases remain recorded failures against v1-as-written. **Contract-clarification decision recorded** ([`adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md); dated addendum: [`s01-canonical-form-v1-clarification-01.md`](s01-canonical-form-v1-clarification-01.md)) — `S-01 P1 CONTRACT CLARIFICATION APPROVED WITH QUALIFICATIONS`; F-P1-01/F-P1-03 editorial; F-P1-02 normatively clarified (effective `canonical-forms v1 (clarified)`; no version bump, no remediation, no re-ingestion). **P1 delta executed** (`976fe52…`; report: [`ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md`](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P1-DELTA-01.md)) — `S-01 P1 DELTA PROOF COMPLETE — READY FOR INDEPENDENT P2 QUALIFICATION`; 55/55 delta checks, 4/4 pairs, direct `compctx1`/composed-target receipts; the P1 historical 15/3 result remains preserved. **P2 independent qualification complete** (`0c0bab0…`; report: [`ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01.md`](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01.md)) — `S-01 P2 INDEPENDENT QUALIFICATION COMPLETE — SUPPORTED WITH QUALIFICATIONS`; 70/70 Part A, 190/190 artifact recomputations, fresh-process integration/reopen/recovery, 907/0/0, frozen oracle byte-identical, 26/0 manifest. **P3 owner decision recorded** ([`ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md`](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md)) — **`S-01 — SATISFIED WITH QUALIFICATIONS`**; **`WP-S25-3 — READY FOR CLOSURE WITH QUALIFICATIONS`**; formal WP-S25-3 closure is **not** authorized by P3; Q-P2-01…Q-P2-06 and P3-F-01 are carried with explicit dispositions and must be reconciled in WP-S25-4/Stage-2 |
| WP-S25-4 (final integrated Stage-2 qualification) | **WP-S25-4 — READY FOR INDEPENDENT QUALIFICATION WITH QUALIFICATIONS** — [entry owner record](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-ENTRY-OWNER-01.md). Separate independent execution authorization remains pending. |

**S2.5 stage disposition (additive).** WP-S25-1, WP-S25-2 and WP-S25-3 are closed with qualifications; SQ-04 is **`SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`**; SQ-05 is **`SATISFIED WITH QUALIFICATIONS`** (bounded hosted-Linux scope); **F-G2-01 remains OPEN** (repository CI coverage-gate failure; separate owner decision required; not waived by any acceptance). WP-S25-4 is eligible for a separately commissioned entry assessment. Stage-2 final integration is incomplete and Stage-2 owner acceptance remains unauthorized. WP-S24-3 remains optional, deferred, and unauthorized; S2.5/Stage 6, public Find Usages, and optimization remain unauthorized.

**Canonical-forms v1 clarification (additive).** The dated addendum [`s01-canonical-form-v1-clarification-01.md`](s01-canonical-form-v1-clarification-01.md) is effective 2026-11-11 (authority: `adr-ci1-01-ci1a-s2-5-wp-s25-3-p1-contract-clarify-owner-01.md`): the P1 findings are disposed as editorial errata (F-P1-01, F-P1-03) and a normative clarification (F-P1-02) with no identity change, no canonical-forms version bump, no semantic-digest version change, no production remediation, and no re-ingestion; the R13a/R13b/R13c subcases require the proposed P1 delta proof and must be re-established before independent P2 qualification, which assesses `canonical-forms v1 (clarified)`.

**P3 owner acceptance (historical status at P3).** [`ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md`](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P3-OWNER-01.md) records **`S-01 — SATISFIED WITH QUALIFICATIONS`** and **`WP-S25-3 — READY FOR CLOSURE WITH QUALIFICATIONS`**. The six P2 qualifications Q-P2-01…Q-P2-06 are individually dispositioned; **P3-F-01** records the P2 report narrative/evidence discrepancy (sealed evidence governs; an additive erratum is required before downstream citation). Formal WP-S25-3 closure, WP-S25-4, Stage-2 acceptance, F-G2-01 closure, S-02/D4.5, and crash/power-loss qualification remain unauthorized or unsatisfied. No historical report or evidence is revised.

**WP-S25-3 formal closure (2026-11-15).** [CLOSURE-OWNER-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-CLOSURE-OWNER-01.md) records **WP-S25-3 — CLOSED WITH QUALIFICATIONS** and retains **S-01 — SATISFIED WITH QUALIFICATIONS**. [P2 erratum](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-3-P2-QUAL-01-ERRATUM.md) records **P3-F-01 — CLOSED BY VERIFIED ADDITIVE ERRATUM**. Q-P2-01 through Q-P2-06 continue per the closure record. B3 cross-project binding, B6 contributor union and B7 payload conflict are carried historical evidence and remain outstanding for WP-S25-4/Stage-2 reconciliation. The same route owns the applicable D3/D4/D5 and Linux scope reconciliation. F-G2-01 stays **OPEN** (296/450 = 65.77%, 70% threshold), with a separate owner decision required. Full D3, D4.5, full S-02 and F-02 crash/power-loss qualification remain Stage 6 obligations. **WP-S25-4 is eligible for entry assessment, subject to separate owner authorization.** S2.5 and Stage-2 acceptance remain pending.

**WP-S25-4 entry assessment (2026-11-15).** The owner recorded qualified readiness at frozen reviewed revision
48370711a6247b972dfc83628618924e864be374. The [qualification matrix](wp-s25-4-qualification-matrix.md),
[evidence inventory](wp-s25-4-evidence-inventory.md), [obligation inventory](wp-s25-4-obligation-inventory.md)
and [execution design](wp-s25-4-execution-plan.md) define the route. Independent execution and Stage-2
acceptance each require separate authority. F-G2-01 remains open.

**WP-S25-4 gate remediation and owner acceptance (2026-11-20).** The QUAL-04 exact-byte formatting failure and the F-G2-01 LatticeDb coverage failure were remediated at `eafd632` (two comment-only production relocations; 23 focused LatticeDb regression tests; no formatter, threshold, exclusion, instrumentation or CI-policy change) and independently qualified by [GATE-QUALIFICATION-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01.md) (`SUPPORTED WITH QUALIFICATIONS`; formatting gate and F-G2-01 individually **SUPPORTED**; independently re-executed: format exit 0 on a byte-exact 4,555-blob export; LatticeDb branches **368/466 = 78.96%**, lines **900/969 = 92.87%**, gate exit 0; whole solution 949/0/0; frozen R2 oracle byte-identical). The owner decision [`adr-ci1-01-ci1a-s2-5-wp-s25-4-gate-owner-01.md`](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-gate-owner-01.md) records **exact-byte formatting gate CLOSED** and **F-G2-01 CLOSED** at the repository level, preserves qualifications F-01…F-06 as historical producer-evidence deficiencies, and leaves M5.4 BLOCKED, M11.4/M14.1 open (SQ-05 satisfaction is scope-stale: the LatticeDb suite grew 52 → 75), and WP-S25-4/Stage-2 acceptance pending. A corrected-revision genuine Ubuntu qualification is **prepared** ([ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01.md)); its publication and execution require a separate G1-style owner authorization. This note is additive status only; no package definition, scope, dependency, or gate above is changed.

**Ubuntu qualification and G4 acceptance (2026-11-21).** The corrected-revision genuine Ubuntu qualification executed under the owner G1/G2 authorization: push of `eafd632` to `qualification/wp-s25-4-sq05-linux-r2`, PR #7 (open, unmerged), run **44746950426** (conclusion **success** — all four jobs). Independently qualified by G3 ([G3-QUAL-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-UBUNTU-QUALIFICATION-01-G3-QUAL-01.md), 33/33 primary-source checks PASS): genuine hosted **Ubuntu 26.04.1 x64** (SDK 10.0.404), LatticeDb **75/0/0** with no filter, branches **368/466 = 78.96%**, lines **900/969 = 92.87%** re-derived from sealed artifacts, native `LatticeDbSharp 0.2.0` exercised, five-identity checkout/tree equivalence (`823e3c65…`). The owner decision [`adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-owner-01.md`](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-owner-01.md) records **M11.4 / M14.1 / M14.2 ACCEPTED WITH QUALIFICATIONS** for the current scope (52-test SQ-05 acceptance preserved as historical and superseded for current scope only; PR #7 remains open and unmerged). The two G3 material findings are dispositioned in [`adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-findings-disposition-01.md`](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-ubuntu-findings-disposition-01.md) (PR #6 merge reconciliation; SQ-05 G2 package completeness — neither blocks acceptance). **M5.4 remains BLOCKED** with the resolution proposal ([M54-RESOLUTION-PROPOSAL-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01.md); recommended: owner clarification + integrated qualification, compiler proof as escalation; the recommendation was adopted by the 2026-11-21 amendment below).

**M5.4 contract clarification approved (2026-11-21).** Following the Option-B resolution route ([M54-RESOLUTION-PROPOSAL-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-RESOLUTION-PROPOSAL-01.md)) and the SQ-04 amendment pattern, the owner approved the bounded M5.4 contract clarification ([M54-AMEND-OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-5-m54-amend-owner-01.md)): **M5.4 — AMENDMENT-APPROVED, PENDING INDEPENDENT QUALIFICATION**. The acceptance basis is the conservative-refusal guarantee — isolated refusal demonstration (constructed control, labeled) + integrated accounting (unresolved classification through publication, native persistence, query, distinct-process reopen) + pre-mint defining-evidence validation — with the prohibited outcomes (false-available, false-complete, wrong reason, mint from unsupported channel) preserved verbatim and strengthened by qualified pre-mint validation. Input reachability and the exhaustive Roslyn symbol-association proof remain unproven and unclaimed; the four inconclusive probes are preserved as inconclusive; escalation to the Option A compiler-proof route is preserved. The independent qualification commission [M54-B-QUAL-01](../../reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-M54-B-QUAL-01.md) is prepared and **not authorized to execute**; the closure checklist is consolidated in [wp-s25-4-closure-checklist.md](wp-s25-4-closure-checklist.md). WP-S25-4 closure, Stage-2 acceptance, and Stage 6 remain separate owner decisions. This note is additive status only; no package definition, scope, dependency, or gate above is changed.

**M5.4 satisfaction and WP-S25-4 final closure (2026-11-22).** [M54-OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-m54-owner-01.md) records **M5.4 — SATISFIED UNDER AMENDED CONTRACT** on the independent M54-B-QUAL-01 qualification (**SUPPORTED**; 11/11 exercises, 11/11 targeted refusal branches hit, no prohibited outcome, no Option A escalation trigger; pinned `eafd632`). [FINAL-OWNER-01](../../decisions/adr-ci1-01-ci1a-s2-5-wp-s25-4-final-owner-01.md) records **WP-S25-4 — CLOSED WITH QUALIFICATIONS**: all matrix blocking obligations discharged or dispositioned (M5.4 satisfied under amended contract; M11.4/M14.1/M14.2 accepted with qualifications; F-G2-01 and the exact-byte formatting gate CLOSED), SQ-06 satisfied on existing evidence (M15.1 PASS), F06/F16/I-01 qualified, the required-evidence blocker not triggered, Stage 6 deferrals restated and not discharged. Stage-2 acceptance readiness: READY WITH QUALIFICATIONS.

**Stage-2 integrated acceptance (2026-11-22).** [STAGE2-ACCEPTANCE-OWNER-01](../../decisions/adr-ci1-01-ci1a-stage2-acceptance-owner-01.md) records **Stage 2 — ACCEPTED WITH QUALIFICATIONS** and **CLOSED** within the qualified boundary: S2.0/F12, S2.1, S2.2, S2.3, S2.4 and S2.5 scopes accepted as recorded; every Stage-2 blocking obligation closed; the corrected revision `eafd632` produced a green full-matrix CI run (**44746950426**) on genuine hosted Ubuntu with the demonstrated Ubuntu 26.04.1 x64 / SDK 10.0.404 scope. Carried: Q-L1…Q-L8 qualifications, the M5.4 amended-contract limitations (MR-1…MR-4), the GH historical errata, and the DF-1…DF-8 Stage 6 deferrals (not discharged). **Stage 6 implementation, WP-S24-3 and publication remain unauthorized**; the Stage 6 entry proposal recommends `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` as the next implementation commission and `ANDIRIT-CI1A-S2.5-PUBLICATION-01` for the package-release checkpoint.
