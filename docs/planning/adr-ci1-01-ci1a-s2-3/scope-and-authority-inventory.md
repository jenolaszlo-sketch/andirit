# S2.3 authority and requirements inventory

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.

S2.3 is the Stage 2 slice that publishes **real Roslyn-derived occurrence facts**
through the already-accepted Candidate C native storage foundation, with native
reopen-time validity enforcement of the defining `compctx2` association.

---

## 1. Definition

From the [Stage 2 boundary §8](../../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md),
**S2.3: Production Publication Integration**:

- Publish extracted occurrences through the existing source-unit lifecycle.
- Preserve native transactional consistency (same transaction as graph).
- Verify replacement/deletion after real source changes.
- Verify contributor behavior across units.

**Gate:** native persistence, reopen, and exact occurrence identity verified —
N1–N8-equivalent tests with real Roslyn facts; replacement/deletion/reopen cycle
with real source changes.

S2.3 consumes the accepted S2.2 capabilities; it does not re-derive semantic
authority. It is bounded by the S2.1 authorization boundary §4 ("Native
publication is S2.3 work"), the S2.2 entry acceptance §3.5 (option (a):
extraction-time enforcement; **native persistence/reopen is S2.3's first gate**),
and the S2.2 `G-ACCEPT` §17 S2.3 entry obligations.

---

## 2. Required behavior inventory

For the existing path
`CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync`:

| # | Requirement | Authority | Current state |
| --- | --- | --- | --- |
| R1 | **Source-unit ownership** — calling/semantic unit owns the occurrence; the storage contributor is separate and excluded from `usage4` | REM-01 contract §4.3; `CodeOccurrenceFact.SourceUnitId` vs `Contributor.IndexUnitId` | Implemented (Stage 1) |
| R2 | **Atomic graph + occurrence publication** in the same native transaction | S1A §3.2; `CompleteIndexRunAsync` → `Persist` → `PersistOccurrences` | Implemented |
| R3 | **Native occurrence unit/page/meta persistence** under `AndiritOccurrenceUnit/Page/Meta`, schema `andirit-occurrence-facts/1` | S1A §3.1 | Implemented |
| R4 | **Publication identity** bound to the graph `CodeRepositoryIndexState` | S1A §3.4; D4.1 | Implemented |
| R5 | **Replacement after real source changes** retires obsolete facts and preserves other units | S1A §3.3; D4.8 | Implemented (synthetic facts) |
| R6 | **Deletion** of obsolete occurrence facts and page rebuild | S1A §3.3 | Implemented |
| R7 | **Contributor union and conflict refusal** (identical semantic facts union; conflicting payloads refused; different semantic units stay distinct) | REM-01 §4.3; S1A §3.3 | Implemented |
| R8 | **Complete-empty preservation** (`Produced` + zero facts persists; reopen restores) | REM-01 §6; S1A §3.6 | Implemented |
| R9 | **Partial / unavailable coverage** distinguished and persisted | REM-01 §6; coverage-and-availability §3 | Implemented |
| R10 | **Native close and reopen** reconstructs the occurrence projection | S1A §3.5; D4.7 | Implemented for initial open |
| R11 | **Projection reconstruction** byte-equivalent ordered query results after reopen | S1A §3.5 | Implemented |
| R12 | **Stale publication refusal** (`StalePublication`) | S1A §3.4; D4.2 | Implemented |
| R13 | **Preservation of the recorded defining `compctx2`** and reopen-time validity enforcement | Option A §5.1(5); mapping contract §4; S2.2 `G-ACCEPT` §17 | **Not implemented — carrier gap (§4)** |
| R14 | **Real cross-project target identity** (`asmctx3` from the dependency's verified defining compilation) | Option A §5.1(1); S2.2 §5 | Implemented in S2.2 extraction; not wired to native publication |
| R15 | **Real Roslyn facts through the publication path** (S2.2 extractor → batch → sink → native) | Stage 2 boundary §3.8; handoff §3 | **Not wired** |

**Reuse rule.** S2.3 must reuse the accepted native foundation (R1–R12, R14) and
must **not** invent a replacement storage architecture. A replacement requires an
independently demonstrated necessity and a separate owner decision (D4 "do not
introduce a general background maintenance service, distributed coordinator or
automatic repair framework without demonstrated need").

---

## 3. Binding invariants and boundaries

- **D3 (legacy graph noninterference):** occurrence facts are never legacy
  `calls`/`references` edges or visible graph nodes; `FindCallers`/`FindReferences`
  identity/cardinality unchanged; no compatibility exceptions to hide occurrence
  graph nodes. (Owner §D.3.)
- **D4 (publication/publication-consistency invariants 1–10):** publication-bound
  index validity; explicit stale/unavailable refusal; no unbounded scan on stale;
  failed staging preserves prior publication; cancelled staging exposes no partial
  data; supersession never replaces a newer generation; reopen is semantically
  equivalent; contributor/deletion update visibility consistently; deterministic
  ordering/limits; concurrency follows the declared serialized model. (Owner §D.4.)
- **D5 (no migration):** new canonical representation; no migration/dual-write/
  adapters; unsupported/incompatible stored schemas refused explicitly; no silent
  reinterpretation; no automatic deletion/overwrite; rebuild by re-ingestion;
  preserve historical experiment databases. Additive JSON fields are permitted;
  format migration is not. (Owner §D.5.)
- **Serialized write/concurrency model** (`_writeGate`): no MVCC, no snapshot
  isolation; no unrestricted concurrent writers. A native schedule test is an S2
  acceptance obligation. (SQ-03.)
- **SQ-02 unknown-outcome model:** known pre-commit failure recoverable; post-commit
  / ack-loss outcome unknown → quarantine instance until reopen; no blind retry;
  no crash/power-loss/multi-process claim. (SQ-02; REM-B05.)
- **Internal-only reader/API:** no public Find Usages (ADR-CI1-02).
- **No public API, no schema migration, no D3/D4/D5 weakening.**

---

## 4. Identified serialization gap (must be resolved before durability authorization)

**The existing native occurrence schema does not carry the defining `compctx2`
association.**

Verified in source:

- `CodeOccurrenceFact` carries `TargetId` (`asmctx3`), `TargetDocumentId`,
  `SourceUnitId`, `Span`, `SourceHash`, `ContentHash`, `Contributor`, `Category`,
  `SemanticsVersion`, `PluginVersion`, `PayloadHash` — **no defining `compctx2`**.
- `CodeOccurrenceCoverage` carries `ScopeId` (`analysisscope1`) and `UniverseId`
  (`compilation-universe1`) plus counters/state — **no per-defining-context
  `compctx2`**.
- `CodeGraphBatch` / `CodeIndexUnitReplacement` carry `Occurrences` and
  `OccurrenceCoverage` only — **no defining-context association**.
- `VerifiedUsageInput.RecordedCompctx2` is a **mint-time validation input**; it is
  consumed by `MintTarget`/`ResolveDefining` and **is not persisted** on the
  produced fact or unit.

Consequences for the durability gate (document 3):

1. On reopen, the store cannot validate a stored fact's defining context, because
   neither the defining `compctx2` nor a per-unit defining-context record is
   persisted. `UniverseId` alone is a whole-universe digest, not a per-defining-
   unit association, and cannot be reconstructed without the live compilations.
2. A structural stored `asmctx3` (`TargetId`) is **not** sufficient: `asmctx3`
   binds the defining R2-visible `compctx1` but **not** the dependency-sensitive
   `compctx2` (Option A §5.1; that separation is the entire point of `compctx2`).

Therefore S2.3 must add an **additive** persisted carrier for the defining
`compctx2` association (e.g. a per-defining-unit / per-target context record on
the occurrence unit or meta) and enforce it on reopen. This is D5-compatible
(additive JSON fields; no format migration). It is a **schema-adjacent change**
and is explicitly **not** authorized by this entry decision; it is gated behind
the durability work package (document 7) and must not silently reuse the recorded
S2.2 `compctx2` as if it were already durable.

**No assumption is made that a stored structural target identifier is enough.**
If the required association cannot be safely persisted and enforced after reopen,
the affected path stops and returns for the accepted **Option B** escalation
(Option A §5.1(5)/§7).

---

## 5. Explicit exclusions (not part of S2.3)

Public Find Usages (ADR-CI1-02); non-C# languages; Tree-sitter occurrences; B2;
experimental-schema migration/dual-write/legacy readers; general background/
distributed indexing; automatic repair framework; incremental target-page
maintenance/native-node-ID optimization (SQ-07/Stage 6); full cross-platform
certification (Stage 6); reader continuation (S2.4); integrated Stage-2 proof
(S2.5); crash/power-loss/multi-process recovery; package publication.

---

## 6. Downstream allocation

- **S2.4** — internal bounded reader continuation/pagination and coverage (IF-04).
- **S2.5** — independent integrated source/compilation/semantic proof, real-project
  end-to-end/native reopen, applicable D3/D4, available historical refusals,
  expanded Linux suite, complete obligation reconciliation, **and the general
  source canonicalization of the identity component digests** (document 6).
- **Stage 6** — full D3/S-02/platform/cancellation/crash qualification.
