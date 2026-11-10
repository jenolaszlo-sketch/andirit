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
| WP-S25-3 (S-01 integrated proof) | **entry decision recorded — `WP-S25-3 ENTRY READY WITH QUALIFICATIONS`** (`adr-ci1-01-ci1a-s2-5-wp-s25-3-entry-owner-01.md`): the integrated S-01 proof design, independent oracle strategy, D3/D4/D5 matrix, and evidence plan are fixed. **Case B qualification:** the production canonical derivation exists (plugin-side, deterministic), but the C#-level canonical component-form semantics must first be **formally adopted** by a bounded contract-resolution decision (**P0**) before P1 proof execution. **P0/P1/P2/P3: all NOT authorized** |
| WP-S25-4 (final integrated Stage-2 qualification) | **planned; NOT authorized** — requires WP-S25-1/2/3 completed and independently reviewed |

**S2.5 stage disposition (additive).** WP-S25-1 and WP-S25-2 are closed; SQ-04 is **`SATISFIED WITH QUALIFICATIONS UNDER AMENDED CONTRACT`**; SQ-05 is **`SATISFIED WITH QUALIFICATIONS`** (bounded hosted-Linux scope); **F-G2-01 remains OPEN** (repository CI coverage-gate failure; separate owner decision required; not waived by any acceptance). WP-S25-3/4 require their own commissions. Stage-2 final integration is incomplete and Stage-2 owner acceptance remains unauthorized. WP-S24-3 remains optional, deferred, and unauthorized; S2.5/Stage 6, public Find Usages, and optimization remain unauthorized.
