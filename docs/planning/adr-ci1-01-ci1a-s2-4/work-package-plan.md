# S2.4 work-package plan — internal bounded reader continuation and coverage

**Commission:** `ANDIRIT-CI1A-S2.4-ENTRY-OWNER-01`. Documentation/plan only.
No package authorizes its successor. Each package requires its own commission,
independent qualification, and owner decision.

**Controlling authority:** the [S2.4 entry owner decision](../../decisions/adr-ci1-01-ci1a-s2-4-entry-owner-01.md)
(fixes the continuation contract and this decomposition), the
[Stage-2 boundary](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8/§10/§11,
the [Stage-2 acceptance matrix](../adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md),
the [S2.3 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-3-accept-owner-01.md),
the [S1 obligation reconciliation](../../decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md) IF-02…IF-05/MD-03,
and the [obligation register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md)
(F10, IF-02…IF-05, SQ-01, SQ-07, MD-02, MD-03, I-03, I-04, S-02, D4, D3-full).

**Global boundaries (all packages):** internal reader/query/result contract only;
no public Find Usages (MD-03; ADR-CI1-02 absent); no persisted token/session format;
no dual-write or storage migration (D5); no MVCC; no S2.5/Stage 6; Windows x64,
serialized single-writer; preserve all S2.3 binding qualifications.

---

## WP-S24-1 — Exact-target internal continuation + coverage breakdown

| Field | Content |
| --- | --- |
| Requirement / authority | Entry decision §3–§7; stage2-boundary §8 (S2.4)/§10; IF-04; F10 |
| Scope | Extend `ICodeOccurrenceReader` (`CodeOccurrenceQuery`/`CodeOccurrenceQueryResult`) with the **stateless versioned continuation token** (§3), **publication-change refusal** (§4), **deterministic strict total ordering** with `after`-cursor continuation (§5), **scope-level coverage breakdown** (§6), and **bounded `Examined`** (§7). `CodeOccurrenceContinuationState` (`NotRequested/More/Complete/Invalid`). |
| Allowed surfaces | `src/Andirit/CodeOccurrenceReader.cs`; `src/Andirit/InMemoryCodeGraphStore.cs` (occurrence query); `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (occurrence query + authority gate — additive only); additive tests under `tests/Andirit.CSharp.Tests` and `tests/Andirit.LatticeDb.Tests`; reviewer probe under `docs/reviews/`. |
| Explicit exclusions | Cross-target mode; public API promotion; any new `AndiritOccurrence*` row/label or persisted token/session; schema/migration/dual-write; MVCC; S2.5/Stage 6. |
| Dependencies | S2.3 accepted (`3e3209f`); IF-03 coverage contract (REM-01); entry decision. |
| Entry criteria | Separate owner commission of WP-S24-1; clean canonical `main` at the accepted S2.4 entry; contract fixed (entry decision). |
| Required qualification evidence | Native tests + fresh-process probes for: stable multi-page traversal; no duplicates/omissions; equal-position ties; overload and cross-unit identity; stale-publication refusal; recovery between pages; malformed/altered/oversized tokens; per-page authority revalidation; `Examined` bounds; coverage-breakdown correctness; availability/refusal separation; deterministic replay; **absence of new persistent storage behavior**. Self-excluding SHA-256 manifest; expected values from accepted contracts. Regression floor: whole solution green; frozen R2 oracle `185/185` byte-identical (`97F6C5E5…39C9`); format exit 0; Release 0/0; production diff limited to the allowed surfaces. |
| Independent gate | **`S24-READER-Q1`** (separate qualification commission; reviewer ≠ implementer) |
| Owner gate | separate `WP-S24-1-OWNER` decision |
| Stop | Any need for persistence/public API/cross-target/schema → return to owner |

---

## WP-S24-2 — Refusal mapping, real-size mapping, and cost measurement

| Field | Content |
| --- | --- |
| Requirement / authority | IF-05, IF-02, SQ-07, MD-02, I-03 |
| Scope | Document and test the exact **refusal-mapping** (open exceptions vs query statuses: graph-only/no-analysis, schema/namespace, stale/unbound page, incomplete coverage; do not promise every mismatch is detected at open); verify **real-project target-page size/order/end-coordinates/planner bounds** (IF-02) and pagination behavior at real sizes; **measure** real-project publication/reopen/query cost with stated environment/repetitions (SQ-07/MD-02) — **no invented thresholds**. |
| Allowed surfaces | Tests/probes and documentation only; the reader surface from WP-S24-1 (read-only use). |
| Explicit exclusions | Optimization/incremental maintenance/native-node-ID redesign (separate authority); any numerical threshold contract; production behavior change beyond WP-S24-1. |
| Dependencies | WP-S24-1 accepted (its continuation/coverage surface). |
| Entry criteria | WP-S24-1 owner-accepted; separate commission. |
| Required qualification evidence | Mapping tests (positive/negative per status); real-size fixture measurements with logs; ordering/end-coordinate verification; self-excluding manifest; regression floor. |
| Independent gate | **`S24-SURFACE-Q1`** |
| Owner gate | separate `WP-S24-2-OWNER` decision |

---

## WP-S24-3 (optional) — Bounded cross-target continuation

| Field | Content |
| --- | --- |
| Requirement / authority | IF-04 "optional cross-target mode"; entry decision §8 |
| Scope | Add a **bounded** cross-target mode reusing the token versioning: `mode=targets`, a `targets` cursor set, per-target ordering unchanged (prepend `TargetId`), a bounded global page budget and `MaxTargets` fan-out, and an explicit bounded-refusal when exceeded. |
| Allowed surfaces | Reader/query/result + additive tests/probes (as WP-S24-1). |
| Explicit exclusions | Unbounded fan-out; public API; persistence; MVCC; optimization. |
| Dependencies | WP-S24-1 accepted; separate commission; forward-compat constraints from the entry decision §8. |
| Entry criteria | Separate owner decision authorizing cross-target scope. |
| Required qualification evidence | Multi-target determinism; fan-out/budget bounds; no silent truncation; refusals; cross-target ordering; manifest; regression floor. |
| Independent gate | independent gate if authorized |
| Owner gate | separate owner decision |

---

## Ordering and non-authorization

- Intended order: **WP-S24-1 → (WP-S24-2) → (optional WP-S24-3) → S2.5**.
- **WP-S24-1 must be implementable and qualifiable independently of cross-target work.**
- **This plan authorizes nothing.** WP-S24-1 requires its own owner commission.
  `S24-READER-Q1` is a separate qualification commission. S2.5, Stage 6, and public
  Find Usages remain unauthorized.
