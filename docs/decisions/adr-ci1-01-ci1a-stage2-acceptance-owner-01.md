# ADR-CI1-01-CI1A-STAGE2-ACCEPTANCE-OWNER-01 — integrated Stage-2 acceptance with qualifications

**Commission:** `ANDIRIT-CI1A-S2.5-STAGE2-ACCEPTANCE-01`.
**Project:** Andirit. **Recorded:** 2026-11-22. **Canonical branch:** `main`.
**Owner decision:** **`Stage 2 — ACCEPTED WITH QUALIFICATIONS`** and **`CLOSED`** (within the
qualified boundary below).

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> Stage 2 is accepted and closed within its qualified boundary: every binding Stage-2
> acceptance prerequisite is dispositioned on accepted evidence, no Stage-2 blocking
> obligation remains, and the corrected revision `eafd632` produced a green full-matrix CI
> run (44746950426) on genuine hosted Ubuntu. The acceptance carries the qualifications,
> amended-contract limitations, historical findings and deferrals restated in §5–§8.
> **This acceptance grants no Stage 6 implementation and no package publication.**
> **Stage 6 implementation, WP-S24-3 and public Find Usages remain unauthorized.**

This is an additive owner decision. It changes no production source, test, fixture, CI
configuration, reviewer evidence, oracle or corpus, prior decision, historical review artifact,
or existing planning contract definition. WP-S25-4 FINAL-OWNER-01 and every earlier
acceptance are preserved unchanged and are not repeated by this record.

## 1. Preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `6250ad153191cb60c779136dd08e0ce2548bbce6` / `7e42295c2379fedb7df6716588af60d1c884046b` (as expected) |
| Branch / worktree | `main`; exactly one; clean tracked/index; no stashes |
| Qualified implementation | `eafd632b...` / tree `823e3c65…`, strict ancestor of HEAD |
| Protected scope | `git diff eafd632 HEAD -- src tests .github` **empty** — no production/test/CI change since the qualified revision |
| Prior closure | WP-S25-4 FINAL-OWNER-01 (`6250ad15…`) verified in place; M54-OWNER-01 (`a6161a0f…`) and the sealed M54-B-QUAL-01 package (32/32 byte-exact) verified unchanged |

## 2. Accepted Stage-2 scope (S2.1–S2.5, as accepted)

| Stage | Scope | Accepted record | State |
| --- | --- | --- | --- |
| S2.1 | SQ-04 authentic historical fixtures | `wp-s25-1-owner-01.md` + `sq04-amend-owner-01.md` | **Accepted with qualifications** (two authenticated prior-writer sets; eight experimental codecs excluded; D5 refusal operative) |
| S2.0/F12 | Source-bound R2 semantic reference corpus | `s2-0-corpus-01-owner-acceptance.md` | **Accepted with qualifications** |
| S2.2 | SQ-05 Linux native scope | `wp-s25-2-owner-01.md` + `…-ubuntu-owner-01.md` | **Accepted with qualifications** (bounded hosted-Linux scope; current-scope re-based to the 75-case suite) |
| S2.3 | S-01 integrated proof | `wp-s25-3-closure-owner-01.md` | **Closed with qualifications** (S-01 satisfied with qualifications; P1 delta) |
| S2.4 | Internal reader / continuation / coverage surface | `s2-4-accept-owner-01.md` | **Accepted with qualifications** (WP-S24-3 explicitly deferred) |
| S2.5 | SQ-06 remainder; WP-S25-1…4 integration | `final-owner-01.md` + this record | **Accepted with qualifications** (WP-S25-4 closed with qualifications 2026-11-22) |

Stage-2 acceptance prerequisites within the WP-S25-4 scope: **R1/R2/R3 and Q03-F-01** qualified
and re-confirmed on `eafd632`; **M5.4** satisfied under amended contract; **SQ-06** satisfied on
existing evidence (M15.1 PASS); **F06/F16/I-01** qualified; required-evidence blocker not
triggered; B3/B6/B7 qualified per QUAL-04.

## 3. Stage-2 acceptance matrix reconciliation

The operative reconciliation is the S2.5 qualification matrix (64 rows), dispositioned by
QUAL-01…04 and the 2026-11-20/22 owner decisions: **46 PASS, 10 PASS-WITH-QUALIFICATION,
0 FAIL, 1 REUSED QUALIFIED, 4 DEFERRED**, with the three former BLOCKED rows (M5.4, M11.4,
M14.1) now dispositioned and M14.2 accepted with qualification. The full row map with
evidence anchors is in
[`acceptance-matrix.md`](adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/acceptance-matrix.md);
the closure of all Stage-2 blocking obligations is in
[`obligation-register.md`](adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/obligation-register.md).

## 4. Corrected-revision CI matrix and genuine-Ubuntu scope

- **Corrected revision `eafd632` produced the first green full-matrix CI conclusion**: run
  **44746950426** (PR #7), all four jobs success — test (windows-latest), test
  (ubuntu-latest), test (macos-latest), pack — on the unmodified workflow (`ci.yml` blob
  `be48426f…`). The LatticeDb coverage gate passed at **368/466 branches = 78.96%** and
  **900/969 lines = 92.87%** with the unchanged 70% policy; the exact-byte formatting step
  passed. Cited as the corrected-revision repository CI consequence — **not** as a general
  CI property.
- **Genuine Ubuntu demonstrated environment** (fixture L): Ubuntu 26.04.1 LTS **x64**, hosted
  image `ubuntu-26.04` / `20261111.196.1`, runner 2.338.2, .NET SDK 10.0.404 / runtime
  10.0.15, with the entire current 75-case `Andirit.LatticeDb.Tests` suite passing
  (75/0/0, no filter) and the five-identity checkout/tree equivalence evidenced.
- **M5.4 amended-contract acceptance**: satisfied under the amended contract
  (`m54-amend-owner-01.md` §6) on the independent M54-B-QUAL-01 **SUPPORTED** disposition,
  with the three binding limitations of §5.

## 5. Qualifications and limitations carried by this acceptance

Q-L1 Linux scope restricted to the demonstrated hosted environment (Ubuntu 26.04.1 x64, SDK
10.0.404, hosted runner) — no other generations, distributions or architectures. Q-L2 only
75-case-suite behavior is qualified. Q-L3 managed fault injection only; no crash/power-loss
durability (F-02). Q-L4 locking/WAL/mmap internals uninstrumented; platform claims are
suite-observed counter parity. Q-L5 hosted-run independence receipt-based; the G2/G3
same-session sequencing limitation is recorded. Q-L6 Windows-x64, serialized single-writer
baseline for non-Linux scope. Q-L7 restricted real-authority cross-unit union (F-01).
Q-L8 reviewer-independence limitations (same-harness; organizational independence
unestablished).

M5.4 limitations (binding on every citation): MR-1 conservative false-negative behavior;
MR-2 constructed-control evidence does not establish natural reachability; MR-3 the exhaustive
Roslyn symbol-association proof remains unproven and unclaimed; MR-4 the structural
universe-membership bound narrows but does not eliminate identity-substitution residual risk.

## 6. Historical findings and errata (carried, not blocking)

GH-1 SQ-05 PR #6 merged (fast-forward, 2026-11-10T23:05:29Z) after the SQ-05 G3 assessment
recorded it open; no decision authorizes it — owner remote-resource reconciliation remains a
scheduled item; no retroactive authorization; content is an ancestor of remote `main`. GH-2
SQ-05 G2 evidence package committed 14 of 18 manifest entries — additive historical errata
recommended. GH-3 gate producer evidence findings F-01…F-06 preserved as historical
deficiencies (technical results independently reproduced). GH-4 M54-B handoff byte-total prose
error — erratum recorded, sealed package intact. Full text: residual-risk register §3.

## 7. Explicitly deferred scope (not discharged)

DF-1 full D3 real-fixture part (native-mirror full corpus); DF-2 D4.5 full native fault matrix;
DF-3 full S-02 ten-invariant native matrix (applicable subset qualified); DF-4 F-02
crash/power-loss/multi-process durability; DF-5 Q2-F09 / Q2-F12 / I-04 cancellation; DF-6 full
platform certification; DF-7 WP-S24-3 optional cross-target scope, public Find Usages,
optimization; DF-8 M9.5 cancellation path and M12.1–M12.3 full native matrix. **Stage 6
implementation is not authorized by this acceptance**; the entry proposal is in
[`stage6-entry-proposal.md`](adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/stage6-entry-proposal.md).

## 8. Disposition and successor status

**`Stage 2 — ACCEPTED WITH QUALIFICATIONS`** and **`CLOSED`** within the qualified boundary of
§2–§7. The accepted capability and qualification summary is in
[`capability-summary.md`](adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/capability-summary.md);
publication readiness is in
[`publication-readiness.md`](adr-ci1-01-ci1a-stage2-acceptance-owner-01-evidence/publication-readiness.md).

Successor status: Stage 6 entry **prepared** (implementation unauthorized); WP-S24-3 optional,
deferred, unauthorized; the recommended next implementation commission is named in the Stage 6
entry proposal. No package publication is authorized.

## 9. Repository discipline

Only this decision document, its evidence package, and the additive updates to the S2.5
obligation register/plan status notes are added, locally on `main`, under the single-worktree
policy. No production, test, dependency, formatter, coverage or CI-policy change; no sealed
historical evidence is modified; no already-accepted qualification execution is repeated; no
fetch, push, or remote operation. The commit/tree identities are reported in the handoff.