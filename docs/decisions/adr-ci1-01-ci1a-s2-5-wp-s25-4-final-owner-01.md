# ADR-CI1-01-CI1A-S2.5-WP-S25-4-FINAL-OWNER-01 — WP-S25-4 final closure with qualifications

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-4-FINAL-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-22. **Canonical branch:** `main`.
**Owner decision:** **`WP-S25-4 — CLOSED WITH QUALIFICATIONS`** (Stage-2 qualification scope).

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> WP-S25-4 is closed for the Stage-2 qualification scope. Every WP-S25-4 matrix blocking
> obligation has been discharged or dispositioned on accepted evidence; no blocking obligation
> remains. The closure carries the qualifications, amended-contract limitations, historical
> evidence findings and Stage 6 deferrals restated in §4–§6. **Stage-2 final integration,
> Stage-2 owner acceptance and Stage 6 implementation remain separate owner decisions** and
> are not granted by this record.

This is an additive owner decision. It changes no production source, test, fixture, CI
configuration, reviewer evidence, oracle or corpus, prior decision, historical review artifact,
or existing planning contract definition. M54-OWNER-01 and the sealed M54-B-QUAL-01 evidence
package are preserved unchanged.

## 1. Preflight and verification basis

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `a6161a0f187c3d1755e91ef19546efa23e1b5a6a` / `814b35f0825ed4d637f6358f60e2918653ba0c27` (as expected) |
| Branch / worktree | `main`; exactly one; clean tracked/index; no stashes |
| Qualified implementation | `eafd632b...` / tree `823e3c65…`, strict ancestor of HEAD |
| Protected scope | `git diff eafd632 HEAD -- src tests .github` **empty** |

**Verified before recording:** the M54-OWNER-01 integrated closure assessment
(`…-m54-owner-01-evidence/closure-assessment.md`) and the consolidated qualification and
residual-risk register (`residual-risk-register.md`) were re-read at this commission; both are
complete, internally consistent, and match the invoked evidence. The sealed M54-B-QUAL-01
package remains manifest-exact (32/32 entries; `totalBytes` 2,314,011; the GH-4 erratum stands
as reporting-only). All stated evidence identities were re-confirmed.

## 2. Blocking-obligation discharge confirmation

| Former blocker | Disposition | Decision record |
| --- | --- | --- |
| M5.4 (matrix §5) | **SATISFIED UNDER AMENDED CONTRACT** (three preserved limitations binding) | `m54-amend-owner-01.md`, `m54-owner-01.md`, M54-B-QUAL-01 |
| M11.4 (matrix §11 row 4) | **ACCEPTED WITH QUALIFICATIONS** (Ubuntu 26.04.1 x64 demonstrated scope) | `…-ubuntu-owner-01.md` |
| M14.1 (matrix §14 row 1) | **ACCEPTED WITH QUALIFICATIONS** (75/0/0 on genuine hosted Ubuntu at `eafd632`; 52-test SQ-05 acceptance re-based for current scope) | `…-ubuntu-owner-01.md` |
| M14.2 (matrix §14 row 2) | **ACCEPTED WITH QUALIFICATION** (suite-level parity documented; internals uninstrumented) | `…-ubuntu-owner-01.md` |
| F-G2-01 (LatticeDb coverage gate) | **CLOSED (repository level)** — 368/466 = 78.96% branches, 900/969 = 92.87% lines, exit 0, genuine-Ubuntu-verified | `…-gate-owner-01.md` |
| QUAL-04 exact-byte formatting gate | **CLOSED (repository level)** — exit 0 on a byte-exact 4,555-blob export, genuine-Ubuntu-verified | `…-gate-owner-01.md` |

**Result: no WP-S25-4 blocking obligation remains.**

## 3. SQ-06 formal disposition (existing accepted evidence)

`Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` (matrix M15.1): an isolated
`meta.Namespace` tamper on a disposable valid store yields `CodeOccurrenceSchemaException` at
open. The test exists (`S23Wp5NamespaceRefusalTests`, WP-S23-5), was qualified **PASS** in
QUAL-04 (M15.1), and passes on the pinned `eafd632` in the qualified whole-solution and
genuine-Ubuntu runs. **SQ-06 is dispositioned SATISFIED — QUALIFIED ON EXISTING EVIDENCE**, with
no new execution (no technical qualification was repeated) and no reopening of accepted work.
The F06/F16/I-01 rows — recorded BLOCKED at QUAL-04 solely on the now-discharged causal
blockers — are dispositioned **QUALIFIED** on existing evidence, and the conditional
required-evidence blocker is recorded **NOT TRIGGERED** (no mandatory entry evidence failure at
the pinned revision). The ledger addendum of 2026-11-22 carries these flips; B3/B6/B7 are
QUALIFIED per the QUAL-04 reconciliation (entry-status wording retained as entry-time history).

## 4. Qualifications and limitations preserved by this closure

**Restated at Stage-2 acceptance (M54-OWNER-01 residual-risk register §1):** Linux scope
restricted to the demonstrated hosted environment (Ubuntu 26.04.1 x64, SDK 10.0.404); only
75-case-suite behavior qualified; managed fault injection only (F-02, no crash/power-loss);
locking/WAL/mmap internals uninstrumented; hosted-run independence receipt-based; Windows-x64
serialized single-writer baseline; restricted real-authority cross-unit union (F-01);
reviewer-independence limitations.

**M5.4 amended-contract limitations (binding on every citation):** conservative false-negative
behavior; constructed-control evidence does not establish natural reachability; the exhaustive
Roslyn symbol-association proof remains unproven and unclaimed.

**Historical evidence findings (carried as errata, not blockers):** G3-F01 (SQ-05 PR #6 merged
after the SQ-05 G3 assessment recorded it open — owner remote-resource reconciliation
recommended; no retroactive authorization; content is an ancestor of remote `main`); G3-F02
(SQ-05 G2 package committed 14 of 18 entries — additive historical errata recommended);
gate producer findings F-01…F-06 (preserved; technical results independently reproduced); GH-4
(M54-B handoff byte-total prose error — erratum recorded, sealed package intact).

## 5. Deferred scope (unchanged; not discharged)

Stage 6: full D3 real-fixture part; D4.5 full native fault matrix; full S-02 ten-invariant
matrix (applicable subset qualified); F-02 crash/power-loss/multi-process durability; Q2-F09 /
Q2-F12 / I-04 cancellation; full platform certification. WP-S24-3 optional cross-target scope
(public Find Usages, optimization) remains optional, deferred and unauthorized. Stage 6
implementation is **not authorized** by this record.

## 6. Stage-2 acceptance readiness

The readiness assessment is delivered in
[`stage2-acceptance-readiness.md`](adr-ci1-01-ci1a-s2-5-wp-s25-4-final-owner-01-evidence/stage2-acceptance-readiness.md).
Summary: **READY WITH QUALIFICATIONS** — every Stage-2 acceptance prerequisite within the
WP-S25-4 scope is dispositioned; the remaining Stage-2 work is the integration record that
restates the qualifications, limitations and deferrals above. Recommended successor commission:
`ANDIRIT-CI1A-S2.5-STAGE2-ACCEPTANCE-01` (owner-gated; not granted here).

## 7. Ledger, register and checklist updates

- `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md` — dated addendum
  (2026-11-22) plus in-place flips: SQ-06 → SATISFIED; F06/F16/I-01 → QUALIFIED;
  required-evidence entry → NOT TRIGGERED. Entry-status wording elsewhere retained as
  entry-time history per the inventory header rule.
- `docs/decisions/adr-ci1-01-ci1a-s2-obligation-register.md` — dated additive note (2026-11-22)
  records the closure and the restatement list.
- `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-closure-checklist.md` — the final integrated
  qualification item is marked done and the closure is recorded.

## 8. Repository discipline

Only this decision document, its evidence package (readiness assessment + manifest), and the
additive updates to the obligation inventory, the consolidated register and the closure
checklist are added, locally on `main`, under the single-worktree policy. No production, test,
dependency, formatter, coverage or CI-policy change; no sealed historical evidence is
modified; no technical qualification is repeated; no fetch, push, or remote operation. The
commit/tree identities are reported in the handoff.