# ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01 — P-03 owner decision and plan erratum

**26 November 2026 · F-S63Q-07 · Low · Accepted with erratum**

The owner resolves the ambiguity recorded by independent requalification. This additive decision governs the P-03 interpretation in qualification plan §4.1 and its corresponding traceability row. The historical plan, its example and the Q1 and R2 observations remain in the record. [Independent decision assessment](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/owner-decision-p03.md)

| Authority | Reconciliation |
| --- | --- |
| Owner decision §D.4 items 1–3 | Requests may use only corroborated accepted index authority. A stale, unbound or unverifiable derived index must refuse explicitly. Stale authority must never silently trigger an unbounded full-store scan. |
| Owner decision §D.4 items 7, 9 and 10 | Preserve equivalent accepted results after supported rebuild or reopen, deterministic site order, explicit result limits and the defined publication snapshot/freshness contract. |
| CI1A specification §7 and acceptance matrix §2 | Exercise all ten invariants against actual production storage. Row 3 specifies a bounded page read and a large-store stale query demonstrating bounded work. Row 9 sets a deterministic result limit. S-02 adds failed, cancelled and superseded publication, reopen and a genuine native concurrency/republication scenario. |
| S6.3 entry §8.2 and qualification plan §4.1 | Entry P-03 seeks explicit stale refusal without a full scan. The plan's `Examined ≤ Limit + 2` combines two distinct observations in one row. The traceability example of 102 at limit 100 reflects the Candidate C experiment in owner decision §B. The governing contracts specify no such numeric ceiling for current-index query work. |

[Owner architecture decision](../decisions/adr-ci1-01-owner-decision.md) · [CI1A specification](../planning/adr-ci1-01-ci1a-impl-01/specification.md) · [CI1A acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/acceptance-matrix.md) · [S6.3 entry](ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01.md) · [Historical P-03 plan](ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-qualification-plan.md)

## Effective interpretation of P-03

1. Exercise stale or unverifiable index authority against a large store. Require explicit refusal and absence of a full-store fallback. Record the `Examined` diagnostic for the stale path. The six independent stale controls returned `StalePublication`, no results and zero entries examined.
2. With current verified authority, demonstrate target-scoped access, publication consistency and deterministic results within the explicit result limit. Record current `Examined` separately as a work observation. P-03's committed fixture returned 100 after examining 200 entries for its target. An independent current request with a limit of one returned one after examining all 400 entries for that target among 700 total entries.
3. Treat the historical `Limit + 2` expression as a verification-plan diagnostic, with the 102-entry example retained as a performance observation. A current-index page limit constrains result count. A computational cap for current requests would require an explicit governing contract and qualification of that cap. The accepted evidence establishes the actual target-scoped behavior and exposes amplification as target cardinality grows.

**Disposition.** F-S63Q-07 is resolved as **accepted with erratum**, with the original observed behavior retained. This interpretation preserves D4-3, D4-9 and S-02 and leaves the G-5 qualification intact. It satisfies the pending owner-decision condition for G-6.

**Open engineering concern.** Current lookups copy and sort the selected target's candidate entries before limiting the page. That observable work can grow with the target despite a small page limit. Track possible reductions in examined entries, allocations and latency while preserving deterministic order and publication consistency. No current-query computational bound is qualified. Any optimization or future threshold requires separate authorization and evidence. [Accepted scope and follow-up](ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01.md)
