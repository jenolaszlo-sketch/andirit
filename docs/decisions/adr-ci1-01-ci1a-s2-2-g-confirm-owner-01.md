# ADR-CI1-01-CI1A-S2.2-G-CONFIRM-OWNER-01 — Formal G-CONFIRM gate acceptance

**Commission:** `ANDIRIT-CI1A-S2.2-G-CONFIRM-OWNER-01`.
**Project:** Andirit. **Date:** 2026-11-04.
**Recorded gate:** `G-CONFIRM`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate status:** **`G-CONFIRM PASSED`**.

**Accepted foundation:** `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`
(tree `ba9338d74bf59c584bef340dbda49ed46eb1c5d9`).
**G-CONFIRM-Q1 content commit:** `391abbee8c26d957a5776bd893403dc2916ecfef`
(tree `3f06ecf81c9dff33eb6d5532705092fe292d2457`).
**Evidence addendum:** `1f51db33103ed79523371e3d2a9957a02fa0f821`
(tree `e469719f13a2b9be40fb06f613e8a96ac42ca6e3`).
**External handoff correction:** `073659fbb44832abf3654c1f2b6c7318097c8aee`
(tree `3459e54a7cb39d2942ac7099c0c54802ca4f4964`), parent `1f51db3`.
**Independent verdict accepted:** `PASS WITH EXPLICIT QUALIFICATIONS`.

This is the owner decision recording formal acceptance of the independently
confirmed G-CONFIRM semantic-authority and expected-value gate, **with explicit
qualifications**. It records a gate disposition only. It authorizes no production
implementation and does not claim any runtime capability.

---

## 1. Verified baseline and evidence integrity

Canonical `main`; clean working tree; one worktree; no stashes; no in-progress
operations. `git fetch` (no prune) changed no ref; `origin/main` remains
`e0d9110`. `main` is 3 commits ahead of `origin/main`; nothing has been pushed.

| Role | Commit | Tree |
| --- | --- | --- |
| Accepted WP1–WP3 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM-Q1 content | `391abbee8c26d957a5776bd893403dc2916ecfef` | `3f06ecf81c9dff33eb6d5532705092fe292d2457` |
| Evidence addendum (index + encoding + log ref) | `1f51db33103ed79523371e3d2a9957a02fa0f821` | `e469719f13a2b9be40fb06f613e8a96ac42ca6e3` |
| External handoff §7 correction (amended from `2f5a3b1`) | `073659fbb44832abf3654c1f2b6c7318097c8aee` | `3459e54a7cb39d2942ac7099c0c54802ca4f4964` |

Verified:

- `e0d9110 → 391abbee → 1f51db3 → 073659f` is a consecutive parent chain; every
  tree reproduced exactly. `e0d9110`, `526d2c0`, `d5a785f`, `391abbee` and
  `1f51db3` retain their original object identities — **no historical commit was
  rewritten**.
- `391abbee` contains exactly the reported **11** review-package files (all under
  `docs/reviews/CI1A-S2.2-G-CONFIRM-Q1/`; it omits `README.md`).
- `1f51db3` contains only: `README.md` (add), `evidence/oracle-run.txt` (add),
  two evidence-encoding normalizations, and the `verification-log.md` log
  reference.
- `073659f` changes only `docs/reviews/CI1A-S2.2-G-CONFIRM-Q1/owner-handoff.md`
  (+20 lines, a §7 state section); its pre-amend reflog entry `2f5a3b1` added
  19 lines to the same file.
- `git diff --name-only e0d9110..HEAD -- src tests benchmarks smoke` is **empty**:
  no production or test file was modified.
- The two evidence files normalized from UTF-16 to UTF-8 retain their exact
  intended text; only encoding, BOM and line endings changed. Comparison of the
  `391abbee` (UTF-16) and `1f51db3` (UTF-8) blobs is text-identical.

## 2. Concurrent-commit anomaly disposition

Three commits were created by an external writer during and after the G-CONFIRM
review; they are **not** authored by this recorder:

| Commit | Content | Note |
| --- | --- | --- |
| `391abbee` | 11 package files (no index) | external; on-topic subject |
| `2f5a3b1` → amended to `073659f` | `owner-handoff.md` §7 | external self-amend of its own unpushed commit |
| `1f51db3` | index + encoding normalization + log ref | **this recorder** (evidence addendum) |

Disposition:

- The anomaly is **accounted for and non-blocking**. Every external change is
  additive documentation confined to the G-CONFIRM review package; none touches
  production, tests, frozen evidence, historical reports or accepted decisions.
- **No content was lost.** The only rewrite is the external writer's own unpushed
  `2f5a3b1 → 073659f` amend of the §7 wording in `owner-handoff.md`; the pre-amend
  content is preserved in the reflog and object database.
- **No pushed history was rewritten** (`origin/main` unchanged). No force-push,
  rebase, reset or branch deletion occurred.
- Git author/committer fields are the repository Git identity
  (`jenol <jenolaszlo-sketch@users.noreply.github.com>`); **process identity is
  not inferred from them**. The author/committer timestamp gap on `073659f`
  (`18:40:02` vs `18:45:55`) is consistent with a local amend and no more.

## 3. G-CONFIRM authentication (owner acceptance basis)

The independent report
[`docs/reviews/CI1A-S2.2-G-CONFIRM-Q1/REPORT.md`](../reviews/CI1A-S2.2-G-CONFIRM-Q1/REPORT.md)
and all 14 nine-part rows
([`fourteen-row-confirmation-matrix.md`](../reviews/CI1A-S2.2-G-CONFIRM-Q1/fourteen-row-confirmation-matrix.md))
were read. Independently re-verified at this recording:

- **All 14 authority rows confirmed** — 4 `CONFIRMED` (rows 1 Q-F2, 3 A-01,
  4 A-02, 6 `compctx2` isolation), 10 `CONFIRMED WITH QUALIFICATIONS`
  (rows 2, 5, 7–14); none `INSUFFICIENT EVIDENCE` or `CONTRADICTED`. The **10
  qualified dispositions are retained**.
- **Independent NEW-02 `usage4` anchor reconstruction** — both frozen vectors
  (`bd0487b7…`, `b874bdc5…`) reproduced by a standalone script with no Andirit
  imports.
- **Independent `compctx1` anchor reconstruction** —
  `compctx1:sha256:71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333`
  reproduced.
- **Frozen R2 oracle** — `185/185` passed; output SHA-256
  `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`, identical
  to the committed evidence and the accepted R2 output.
- **Nine frozen expected sections** — `9/9` match.
- **R2 / R1 / Q2 evidence integrity** — `100 / 50 / 93+3` files verified.
- **No circular expected-value derivation** — every expected value is grounded in
  accepted authority; rows 5 and 11 forbid implementation-originated oracles, and
  no future test was credited as passing.

**Confirmed semantic authority is not implemented production functionality.** No
mapper, resolver, association enforcement, minting, persistence or reopen is
claimed.

## 4. Owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`.** The owner accepts the independently
confirmed semantic authority, expected-value derivation, provenance and
consistency for all 14 F16/NEW-02 rows, carrying the corrections and
qualifications below. **`G-CONFIRM` is recorded as `PASSED`.**

## 5. Explicitly acknowledged corrections

| ID | Correction |
| --- | --- |
| **C1** | Bounded Q-F3 metadata-validation wording: the achieved guarantee is the bounded name/signature/relationship surface (with documented still-capturing excluded surfaces), not universal PE/metadata validity. The pre-remediation handoff §D row-2 wording is superseded. |
| **C2** | Declared comparison profiles for the `compctx1` mapping: production actual-path profile vs frozen fixture profile, with the test-only comparison-label alignment disclosed and never entering production. Handoff §D row 5 is superseded by the declared-profile wording. |
| **C3** | Nine-part schema field-naming consistency: the ninth field is **closure authority**; the handoff §D header "Authority" is a naming defect only. |
| **C4** | Semantic authority is confirmed now for all 14 rows; **implementation verification remains deferred** to WP4–WP9 (rows 7–13, plus the live-mapper/extraction parts of 4/5/6/13). A blanket "rows 5–14" deferral is narrower in practice. |

## 6. Retained Q2 foundation limitations (unchanged)

All qualifications accepted with the WP1–WP3 foundation remain in force and are
**not** closed by this gate:

- Bounded Q-F2 encoding support and its documented NUL-heuristic limitation.
- Bounded Q-F3 structural metadata validation; **no** complete PE/IL correctness
  guarantee; **no** semantic compilation-usability guarantee; documented
  remaining identity-capture surfaces and exclusions.
- A-01 supported hardening boundary.
- Corrected production-vs-frozen reference profiles and authority boundaries.
- Shape-versus-authority distinction.
- Option A identity and provenance separation, including the pre-mint association
  **specification without implemented enforcement**.
- **No native persistence or reopen enforcement.**
- Q1 historical observation failures remain preserved and classified as failures.
- Carried accepted limitations (S2.1 packaging, Q-F4/Q-F5, A-02, S-01, F09,
  F10/IF-04, F11, NEW-01, NEW-04, SQ-01…SQ-07, D3/D4/D5, I-05/MD-03, bounded
  F12/toolchain reference).

## 7. Explicit non-claims

This decision does **not** claim, and must not be read as:

- a production mapper/resolver implemented;
- extraction-time association enforced;
- association or target minting implemented;
- complete verified-universe integration;
- native persistence or reopen enforcement;
- complete S2.2 acceptance.

## 8. Next gate boundaries

| Item | Status |
| --- | --- |
| Dependent test authoring against the independently confirmed rows | **AUTHORIZED**, subject to accepted authority and the correct comparison profiles (C1/C2) |
| WP3 production mapper/resolver implementation | **REQUIRES SEPARATE COMMISSION** |
| `G-MINT` | **OPEN** |
| WP4–WP9 | **NOT AUTHORIZED** |
| S2.3+ | **NOT AUTHORIZED** |

**Preserved ordering:** `G-CONFIRM → WP3 mapper → G-MINT → WP4 → WP5`.

**WP3 pre-mint validation vs WP4 runtime integration.** The WP3 mapping and the
pre-mint association invariant (Option A §5.2 in-memory checks: candidate/current/
universe mismatch, stale/absent/substituted/fabricated/ambiguous refusal,
`compctx2` isolation, determinism, no-churn) are **independently testable without
WP4 runtime extraction**, because they operate on the implemented mapper and
in-memory verified contexts. `G-MINT` reviews that implemented mapper plus the
pre-mint association validation. WP4 runtime extraction integration then
consumes the cleared mapper. This distinction avoids any need to weaken
`G-MINT`.

**Escalation.** If implementing WP3 finds that a `G-MINT` requirement genuinely
cannot be satisfied without WP4 runtime output, that is a genuine ordering
conflict to **escalate to the owner** — it must **not** be resolved by weakening
or deferring `G-MINT`.

## 9. Recording and boundaries

- Additive documentation only. No production code, test, frozen manifest, oracle,
  accepted decision or historical report is modified.
- **No amend, rebase, force-push, branch deletion or worktree creation. No push.**
- Frozen R2/Q3 evidence and the Q1/R1/Q2 reports remain preserved; the concurrent
  external commits are documented above and left intact.
- Stop after formal gate acceptance; WP3 implementation is not started.

---

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-G-CONFIRM-OWNER-01 ACCEPTED WITH
EXPLICIT QUALIFICATIONS`** — **`G-CONFIRM PASSED`** for the 14 F16/NEW-02 rows'
semantic authority and expected-value plan, with C1–C4 acknowledged and all
foundation limitations retained; `G-MINT` remains **OPEN** and WP4–WP9/S2.3+
remain **NOT AUTHORIZED**.
