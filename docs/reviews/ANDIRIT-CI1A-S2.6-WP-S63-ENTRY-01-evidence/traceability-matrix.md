# S6.3 Traceability Matrix — ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01

**Date**: 2026-11-24
**Scope**: Traceability from governing requirements (D4 invariants 1–10, S-02, CI1A spec, S2.5 matrix) to S6.3 test cases, evidence classes, and qualification gates.
**Sources**: Byte-exact excerpts in `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-evidence/contract-reconciliation.txt`.

---

## 1. D4 Invariants → Test Cases → Evidence

| D4 Invariant | Exact Requirement | S6.3 Test Case(s) | Evidence Class | Observation Mechanism | Acceptance Criterion |
| --- | --- | --- | --- | --- | --- |
| **D4-1** | Query uses only index consistent with accepted published generation | **P-01** | Constructed (fault-injector) | Stage stale index (older generation meta); query occurrence index; assert explicit refusal status | Query returns `OccurrenceIndexStale` or equivalent refusal; no results returned from stale index |
| **D4-2** | Stale/unbound/unverifiable index refuses explicitly | **P-02** | Constructed | Remove/corrupt `AndiritOccurrenceMeta`; query; assert explicit refusal | Query returns `OccurrenceIndexUnavailable` / `OccurrenceIndexUnverifiable`; never silent empty success |
| **D4-3** | Stale index must not silently trigger unbounded full-store scan | **P-03** | Constructed | Stage stale index with large fact set; query with bounded limit; measure examined count | `Examined` ≤ `Limit + tolerance` (e.g., 102 examined for limit 100); no full-store scan |
| **D4-4** | Failed staging preserves last accepted publication and valid index | **P-04** | Constructed (fault-injector) | Establish accepted publication; inject validation failure pre-commit; verify prior publication + index intact | `GetLatestPublicationAsync` returns prior run; occurrence index queries return prior facts; no partial exposure |
| **D4-5** | Cancelled staging exposes nothing | — | — | **S6.1 complete (Q-01…Q-06, Q-11)** | — |
| **D4-6** | Superseded publication must not replace newer accepted generation | **P-05**, **P-06** | **P-05**: Genuine (concurrent writers)<br>**P-06**: Constructed | **P-05**: Two writers race to publish; first commits; second's older generation rejected<br>**P-06**: Attempt to re-publish older run after newer accepted | **P-05**: Only newer generation visible; durable state consistent<br>**P-06**: `CompleteIndexRunAsync` for older run refused or no-op; newer generation retained |
| **D4-7** | Rebuild/reopen produces semantically equivalent published index | **P-07**, **P-08** | Genuine | **P-07**: Ordinary reopen (no rebuild); query equivalence<br>**P-08**: Force full rebuild (`RebuildInnerFromDurableStateAsync` path); query equivalence | Identical query results (occurrence facts, coverage, publication binding) before/after reopen and rebuild |
| **D4-8** | Contributor changes and scoped deletion update visibility consistently | **P-09**, **P-10** | Constructed | **P-09**: Stage identical facts from two contributors → union; stage conflicting facts → rejection retains first<br>**P-10**: Stage deletion for one target context; verify only that context removed | **P-09**: Union merges identical; conflict throws `OccurrenceConflictException`; accepted state queryable<br>**P-10**: Deleted target's facts/pages gone; other targets unaffected; index clean |
| **D4-9** | Bounded queries have deterministic ordering and explicit limits | **P-11** | Constructed | Query occurrence index with limit N; verify ordering (path, line, column, occurrence ID) and count ≤ N | Results ordered by `Span.Path` → `StartLine` → `StartColumn` → `EndLine` → `EndColumn` → `OccurrenceId`; count ≤ limit |
| **D4-10** | Query behaviour during concurrent publication follows snapshot/freshness contract | **P-12**, **P-15** | **P-12**: Genuine (concurrent writer + reader)<br>**P-15**: Genuine (multi-process, fixed D-05) | **P-12**: Writer publishes; concurrent reader queries; verify snapshot isolation<br>**P-15**: Multi-process writer commits; sequential readers see consistent publication | **P-12**: Reader sees either old or new publication atomically; no mixed state<br>**P-15**: All readers see same committed publication; file locking serializes writers |

---

## 2. S-02 / CI1A Specification → Test Cases → Evidence

| S-02 Obligation | Exact Requirement | S6.3 Test Case(s) | Evidence Class | Acceptance Criterion |
| --- | --- | --- | --- | --- |
| **S-02-1** | All ten invariants exercised against production `LatticeCodeGraphStore` | **P-01…P-12, P-15** | Mixed | All 10 D4 invariants have at least one passing test case against production storage |
| **S-02-2** | Failed publication exercised | **P-04** (extends S6.2 D-01…D-04) | Constructed | Failed staging (validation, pre-commit) preserves prior occurrence publication and index |
| **S-02-3** | Cancelled publication exercised | — | — | **S6.1 complete** |
| **S-02-4** | Superseded publication exercised | **P-05, P-06, P-13** | Genuine / Constructed | Supersession stress: rapid sequential publications; only latest accepted; generation regression refused |
| **S-02-5** | Reopen exercised | **P-07, P-08, P-14** | Genuine / Constructed | Ordinary reopen, full rebuild/reopen, unknown-outcome quarantine + reopen corroboration (occurrence index) |
| **S-02-6** | Native concurrency/republication scenario | **P-12, P-15** | Genuine | Multi-process writer race; file locking serializes; first writer commits; state consistent |
| **S-02-7** | In-process interleaving diagnostic only | **P-16** | Documentation | `QueryInterleavingHook` / in-process interleaving recorded as diagnostic; not native atomicity evidence |

---

## 3. S2.5 Matrix Rows → Test Cases

| Matrix Row | Scope | S6.3 Test Cases | Status |
| --- | --- | --- | --- |
| **M12.1** | D4 invariants 1–4, 6–9 (stale index, supersession, rebuild/reopen, contributor changes, ordering/limits) | P-01, P-02, P-03, P-04, P-05, P-06, P-07, P-08, P-09, P-10, P-11 | **In** — primary deliverable |
| **M12.2** | D4 invariant 5 (cancelled staging) | — | **Done (S6.1)** |
| **M12.3** | D4 invariant 10 (concurrent publication snapshot/freshness) | P-12, P-15 | **In** — primary deliverable |

---

## 4. CI1A Obligation Register → Test Cases

| Register ID | Obligation | S6.3 Test Cases | Status |
| --- | --- | --- | --- |
| **I-05** | Supersession/reopen qualification | P-05, P-06, P-07, P-08, P-13, P-14 | **In** |
| **I-06** | Concurrency/republication qualification | P-12, P-15 | **In** |
| **I-07** | Full D4 matrix qualification | P-01…P-12, P-15 | **In** |

---

## 5. Implementation Surface Mapping

| S6.3 Test Case | Production Code Exercised | Key Methods |
| --- | --- | --- |
| P-01, P-02, P-03 | Occurrence query path + index authority check | `FindOccurrencesAsync` → `CheckOccurrenceAuthorityAsync` → `_inner.FindOccurrencesAsync` |
| P-04 | Staging + validation failure + rollback | `StageIndexUnitAsync` → `MutateAsync` → `ApplyCommandAsync` (fails) → `RebuildInnerFromDurableStateAsync` |
| P-05, P-06, P-13 | Publication + supersession logic | `CompleteIndexRunAsync` → `MutateAsync` → `Persist` → `PublishStagedRun` → `PersistOccurrences` |
| P-07, P-08 | Reopen + rebuild paths | `LatticeCodeGraphStore` constructor → `RestoreOccurrences` / `RebuildInnerFromDurableStateAsync` |
| P-09, P-10 | Contributor union / conflict / deletion | `StageIndexUnitAsync` (union path) / `StageIndexUnitDeletionAsync` → `PersistOccurrences` (conflict detection, deletion) |
| P-11 | Bounded query + deterministic ordering | `FindOccurrencesAsync` → `_inner.FindOccurrencesAsync` (occurrence index pages already ordered) |
| P-12, P-15 | Concurrent publication (multi-process) | File locking via `LatticeDatabase.Open`; `CompleteIndexRunAsync` serialization |
| P-14 | Unknown-outcome + quarantine + reopen | Fault-injector at `"after-commit"` → `CodeGraphCommitOutcomeUnknownException` → quarantine → reopen corroboration |
| P-16 | Diagnostic interleaving | `QueryInterleavingHook` (test-only surface; recorded as diagnostic) |

---

## 6. Platform Evidence Matrix

| Test Case | Windows x64 (local) | Linux x64 (hosted `ubuntu-latest`) | macOS arm64 |
| --- | --- | --- | --- |
| P-01…P-11, P-13, P-14, P-16 | Required | Required | Not qualified (content only) |
| P-12, P-15 | Required | Required | Not qualified (content only) |

**Rule**: Windows evidence does not substitute for Linux (Q-S61-5, Q-S62-5). All test cases must pass on both Windows x64 and genuine hosted Linux x64.

---

## 7. Qualification Gate Traceability

| Gate | Requirement | S6.3 Traceability |
| --- | --- | --- |
| **G-1 Implementation** | Correct implementation; docs-only; no dependency/workflow/threshold change; whole solution builds/tests clean | 0 production changes; test suite + documentation only; `dotnet build` + `dotnet test` clean on all 3 platforms |
| **G-2 Qualification** | All test cases executed; genuine Linux on genuine hosted runner bound to sealed commit; no stop condition | P-01…P-16 executed on Windows x64 + hosted `ubuntu-latest`; CI run bound to implementation commit; no stop condition triggered |
| **G-3 Regression** | Historical suites, coverage thresholds (55/75/60/70), CI gates unchanged and green on all 3 platforms | All existing suites (Core 345, CSharp 372, TreeSitter 157, LatticeDb 88+) pass; thresholds met; all 3 CI legs green |
| **G-4 Evidence** | Evidence packages sealed with self-excluding SHA-256 manifests; production delta = test + doc paths only | New evidence package `ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-evidence/` + implementation evidence `ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-evidence/`; self-excluding manifests; byte-exact lineage extended |
| **G-5 Independence** | Reviewer session ≠ producer session; independent contract re-derivation; wider superset enumeration | Separate qualification commission after implementation; independent re-derivation of all contracts; wider regex superset for contract gate |

---

**Recorded**: 2026-11-24
**Baseline**: `359a9e8a8768d4cc9204e898ef76f0b4eba38d47`
**Canonical branch**: `main`