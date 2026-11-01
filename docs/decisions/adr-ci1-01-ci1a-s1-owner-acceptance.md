# ADR-CI1-01-CI1A-S1-OWNER-ACCEPTANCE

**Project:** Andirit  
**Milestone:** ADR-CI1-01-CI1A-S1-OWNER-01 — Owner acceptance of the Stage 1 independent implementation review  
**Recorded:** 2026-11-01  
**Executor:** delegated acceptance recorder. The Git author label records automation provenance, not a human signature. The recorder did not conduct the independent review and issues no second review verdict.

**Disposition: `ADR-CI1-01-CI1A-IMPL-01-S1 ACCEPTED WITH QUALIFICATIONS`**

---

## 1. Verified Source Identity

| Item | Value |
|------|-------|
| Canonical repository | `D:\projects\Andirit` (`https://github.com/jenolaszlo-sketch/andirit.git`) |
| Branch | `main` |
| Reviewed implementation HEAD | `76ebaae84d741a38f92d2422c63742197c36bc82` |
| Reviewed implementation tree | `361293dbf94c0836fe9666bb07d596ea7ae4a7f4` |
| Implementation commit A (contracts + in-memory) | `01b74782751aede7274a1e4841b8a676a79fae7b` (tree `b98fb150a4a91884dea6f5bb2521995f7a1d4d19`) |
| Implementation commit B (native persistence) | `921900d258d8811a693141abb0dcb4137bcd901a` (tree `125c5e2c5a3c41451124c7fe4d7f467c2fe79d75`) |
| Implementation commit C (evidence) | `76ebaae84d741a38f92d2422c63742197c36bc82` (tree `361293dbf94c0836fe9666bb07d596ea7ae4a7f4`) |
| Working tree | Clean (no tracked or untracked changes) |
| Worktrees | 1 canonical (`D:/projects/Andirit 76ebaae [main]`) |
| Local vs origin | `main` ahead 6 commits, nothing pushed |
| Independent Q1 review package | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s1-q1` |
| Q1 reviewer manifest (self-excluded) | `evidence/q1-reviewer-manifest.txt`, scheme `q1-review-v1`, 16 entries, tree `c8d4f2a1e3b7f9e5d2c4a8b1f3e6d9c2a5f8e1b4c7d0a3f6e9b2c5d8a1f4e7b0` |

The source binding was independently verified by the Q1 reviewer (see `SOURCE-BINDING.md` in the review package). No production changes occurred after the reviewed implementation.

---

## 2. Independent Review Disposition

| Review | Package | Disposition |
|--------|---------|-------------|
| `ADR-CI1-01-CI1A-IMPL-01-S1-Q1` | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-impl-01-s1-q1` | `SUPPORTED WITH QUALIFICATIONS` |

**Finding counts (from `FINDINGS.md`):**
- Blocking correctness defects: **0**
- Significant qualifications: **7**
- Implementation follow-ups: **5**
- Minor documentation/evidence issues: **3**

---

## 3. Accepted Capabilities

The owner accepts that the following capabilities have been **independently verified** within the tested Stage 1 scope:

### 3.1 Native Persistence
Separate Candidate C occurrence state persisted in distinct LatticeDB labels:
- `AndiritOccurrenceUnit` — source-unit-owned occurrence facts
- `AndiritOccurrencePage` — target-index pages with ordered occurrence IDs
- `AndiritOccurrenceMeta` — schema version, namespace, publication binding

Namespace: `andirit-occurrence-facts/1`  
Native occurrence schema version: `1`  
Occurrence semantics version: `4` (accepted `usage4` shape)

### 3.2 Atomic Publication
Graph completion and occurrence persistence occur in the **same LatticeDB write transaction** (`CompleteIndexRunAsync` → `Persist` → `PersistOccurrences`). A `before-commit` fault injection test (N6) independently verified that a failed commit leaves the prior accepted publication fully intact in durable storage.

### 3.3 Source-Owned Lifecycle
- **Replacement:** Unit replacement retires obsolete facts from native rows and query results; unrelated units preserved (N3).
- **Deletion:** Deleted unit contributions removed; remaining valid contributors persist; target pages rebuilt (N4).
- **Contributor union:** Identical facts from multiple units share one identity; contributors unioned at query time (N4).
- **Conflict rejection:** Same identity with different immutable payload rejected at staging; prior state preserved (N4).
- **Idempotence:** Repeat publication of equivalent unit state is a no-op; no duplicate facts or contributors.

### 3.4 Bounded Deterministic Retrieval
- Target-scoped queries read only the relevant target page (`Examined` = page size).
- Deterministic ordering: path → line → column → ID (ordinal).
- Explicit `MaxResults` limit with `Truncated` flag.
- Publication consistency enforced: stale query handle → `StalePublication`; graph-only store → `AnalysisUnavailable` (N2, N7).

### 3.5 Durable Reopen
Close/reopen reconstructs the occurrence projection from authoritative `AndiritOccurrenceUnit` rows. N5 verified byte-equivalent ordered query results after reopen.

### 3.6 Schema and Refusal
- Unsupported occurrence schema version/namespace → explicit refusal on open (`CodeOccurrenceSchemaException`).
- Occurrence units/pages without meta manifest → refusal on open.
- Page/publication run ID mismatch → refusal on open (`CodeOccurrenceIndexException`).
- Graph-only schema-2 store → opens normally; occurrence query → `AnalysisUnavailable` (never empty success).

### 3.7 D3 Legacy Graph Noninterference
Synthetic graph corpus (2 graph units + 1 occurrence-only unit) verified identical graph snapshots across:
1. Baseline (graph only)
2. Initial occurrence publication
3. Occurrence replacement
4. Occurrence-bearing unit deletion
5. Failed occurrence publication
6. Close/reopen

All 579 regression tests (315 + 69 + 38 + 157) pass unchanged. No occurrence facts appear as legacy graph nodes/edges.

### 3.8 Test Evidence
- 8 mandatory native tests (N1–N8) independently reproduced against real `LatticeCodeGraphStore`.
- 2 schema refusal tests independently reproduced.
- 38/38 `Andirit.LatticeDb.Tests` pass.
- 579 total regression tests pass (all four suites).
- 3/3 in-memory parity tests pass.

---

## 4. Explicit Claim Boundaries

**This acceptance does NOT assert:**

| Excluded Claim | Reason |
|----------------|--------|
| Full production Roslyn occurrence extraction | Not implemented in Stage 1; controlled records used |
| Verified source-byte provenance (S-01) | Descriptor hashes not proven to be actual indexed source bytes |
| Global semantic completeness | Synthetic corpus; real-project ingestion not tested |
| True concurrent native publication stress qualification | Only serialized `_writeGate` demonstrated; no MVCC |
| MVCC/snapshot isolation | Queries serialize behind publication; no non-blocking reads |
| Crash or power-loss recovery qualification | Acknowledged Stage 1 boundary (Q3-QUAL-03) |
| Unknown-acknowledgment outcome reconciliation | Not distinguished in current provider model |
| Universal graph noninterference | Synthetic corpus only; full D3 regression deferred to Stage 6 |
| Full historical fixture compatibility | No prior-writer fixture generated (SQ-06) |
| Cross-platform native qualification | Windows only; Linux/macOS unverified |
| Constant-time or incremental target-page maintenance | Full repository page rebuild per completion (explicit Stage 1 design) |
| Public Find Usages readiness | Internal `ICodeOccurrenceReader` only; no public API |

---

## 5. Owner Disposition

**`S1 ACCEPTED WITH QUALIFICATIONS — NATIVE STORAGE FOUNDATION ESTABLISHED`**

This acceptance closes Stage 1 at its declared qualification boundary. It does not close remaining CI-1A obligations and does not authorize Stage 2 implementation. Stage 2 remains separately authorized.

---

## 6. Commitment to Finding Reconciliation

All seven significant qualifications, five implementation follow-ups, three minor findings, and earlier S-01/S-02 obligations are carried forward into the authoritative obligation reconciliation document: `docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md`.

No finding is dismissed; each is classified with an explicit owner disposition and assigned to a specific stage gate.

---

## 7. Next Authorized Action

**Documentation only:** Record the obligation reconciliation and define the Stage 2 boundary (`ADR-CI1-01-CI1A-IMPL-01-S2`). No implementation is authorized by this acceptance.

**Next commission (proposed, not authorized):**
`ADR-CI1-01-CI1A-IMPL-01-S2` — Roslyn Semantic Occurrence Extraction and Verified Provenance