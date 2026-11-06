# WP-S23-3 — Real-fact publication integration (authorized first bounded package)

**Commission:** `ANDIRIT-CI1A-S2.3-WP-S23-3-ENTRY-OWNER-01`. Documentation/specification only.
**Authority:** Stage 2 boundary §3.8/§8 (S2.3); S2.2 identity handoff §3(4); S2.3 scope inventory R15 and §4; WP-S23-2 work-package row; WP-S23-2 owner acceptance §6.
**Status:** **authorized by [the WP-S23-3 entry owner decision](../../decisions/adr-ci1-01-ci1a-s2-3-wp-s23-3-entry-owner-01.md)**, first bounded package only.

---

## 1. Contractual purpose

WP-S23-3 publishes **real Roslyn-derived occurrence facts** through the already-accepted
publication lifecycle, reusing the accepted S2.2 extractor and the accepted WP-S23-2
defining-`compctx2` authority carrier. Per Stage 2 boundary §3.8:

> Occurrence facts flow through existing ingestion: `CodeGraphBatch` → `CodeGraphIngestionSink`
> → `StageIndexUnitAsync` → `CompleteIndexRunAsync`. Native persistence unchanged:
> `AndiritOccurrenceUnit/Page/Meta` in the same transaction as graph. Replacement/deletion/
> reopen/contributor/conflict behavior inherited from Stage 1.

WP-S23-3 must **not** invent a replacement storage architecture, a new publication path,
a new public API, or a new semantic identity. It consumes the accepted S2.2 capability and
the accepted S23-DUR carrier.

## 2. Verified starting surface (at `80f6177`)

- `CodeGraphBatch` (`src/Andirit.Abstractions/CodeGraphBatch.cs`) already carries public
  `Occurrences` and `OccurrenceCoverage`, but **not** the internal
  `CodeOccurrenceAuthority` envelope or the `RequiresOccurrenceAuthority` marker.
- `CodeGraphIngestionSink.Accumulator.ToReplacement()` (`src/Andirit/CodeGraphIngestionSink.cs`)
  builds a `CodeIndexUnitReplacement` from the batches but does **not** set the internal
  `OccurrenceAuthority` / `RequiresOccurrenceAuthority` members.
- `CodeIndexUnitReplacement` (`src/Andirit/CodeGraphStoreModels.cs`) already exposes the
  internal `[JsonIgnore] OccurrenceAuthority` and `[JsonInclude] RequiresOccurrenceAuthority`
  members, and `VerifiedOccurrenceExtraction.PrepareNativeUnit`
  (`src/Andirit.CSharp/VerifiedUsageMint.cs`) is the existing internal handoff that sets them.
- `CSharpCodeGraphPlugin.GraphBuilder` publishes nodes/declarations/edges only; the S2.2
  extractor is not wired to the publication path (R15).
- Layering constraint: `Andirit` references `Andirit.Abstractions`; `Andirit.Abstractions`
  references nothing. The internal `CodeOccurrenceAuthority` (defined in `Andirit`) therefore
  **cannot** be a member of the public `CodeGraphBatch` type in `Andirit.Abstractions`.

Consequence: the authority handoff must use an internal, non-public, non-persisted channel
(or an equivalent internal mechanism) that does not add a public API and does not change the
persisted schema/format. This is the principal design tension of the package.

## 3. First bounded package (`WP-S23-3A`) — deliverables

1. **Real-fact publication wiring.** A verified C# publication path emits real extracted
   occurrence facts and coverage into the existing chain
   `CodeGraphBatch → CodeGraphIngestionSink → StageIndexUnitAsync → CompleteIndexRunAsync`,
   persisting under `AndiritOccurrenceUnit/Page/Meta` in the same transaction as graph, with
   correct real cross-project `asmctx3` targets.
2. **Authority + mandatory-marker propagation.** Thread the accepted internal
   `CodeOccurrenceAuthority` envelope and the `RequiresOccurrenceAuthority` marker from the
   verified extraction to the staged `CodeIndexUnitReplacement` (equivalently to
   `PrepareNativeUnit`), so `PersistOccurrences` persists them and the query gate accepts
   matching current authority after reopen. Internal/additive only.
3. **Segment-resumption containment (Q1-Q1).** An authority-bearing real-fact unit staged
   across a store-lifetime interruption must not silently serve stale, partial, or fabricated
   authority. The package must demonstrate either explicit re-stage/re-verification after the
   interruption or an explicit fail-closed refusal, with fresh-process evidence. It must not
   rely on graph-only segment resumption for occurrence/provenance correctness.
4. **Negative controls.** Unsupported/ambiguous/unresolved/missing-defining targets refuse;
   no trusted fact from a structural id alone; stale defining context (direct, transitive-only,
   options-only) refuses after reopen even with a stable `asmctx3`; missing/corrupt/unsupported
   persisted authority refuses; no occurrence is exposed as a legacy graph node/edge (D3).
5. **Evidence.** Real source bytes + hashes, actual Roslyn compilations/references, recorded
   `compctx1`/`compctx2`/`asmctx3`/`usage4` identities, native before/after records, reopen and
   fresh-process observations, refusal/coverage classifications, raw logs/TRX/environment, and a
   self-excluding manifest.

## 4. Invariants that must remain exactly preserved

- **S23-DUR (WP-S23-2)**: orphan-owner refusal; mandatory association; F-01 durable-reopen
  closure; retirement correctness; complete-empty vs unavailable/corrupted distinction; the
  accepted carrier, its version, and the occurrence schema/namespace are unchanged.
- **D3** graph noninterference; **D4** publication invariants 1–10; **D5** no migration
  (additive JSON only; explicit unsupported-schema refusal).
- Serialized write/concurrency model; SQ-02 unknown-outcome quarantine/reopen (no blind retry).
- Frozen identity formulas (`compctx1`/`compctx2`/`asmctx3`/`usage4`); `compctx2` never enters
  `asmctx3`/`usage4`.
- S2.2 refusal/coverage semantics; WP6 merge semantics; WP7 stale boundary.
- Internal-only reader; no public Find Usages.

## 5. Acceptance criteria (executable)

1. Real Roslyn facts published through the accepted chain are `Available` with matching
   current authority after durable reopen, with correct cross-project targets (fresh process).
2. Direct, transitive-only, and options-only defining-context changes refuse after reopen
   despite a stable `asmctx3`.
3. Missing/corrupt/unsupported persisted provenance refuses conservatively (never a successful
   empty or fabricated current authority).
4. Complete-empty remains `Available`/complete; `AnalysisUnavailable` and `CorruptedIndex`
   remain distinct.
5. Q1-Q1: cross-store segment resumption cannot silently serve stale/incomplete authority.
6. Existing NEW-01 recovery, retirement, conflict refusal, unknown-outcome quarantine,
   coverage/refusal counters, native mirrors, and D3/D4/D5 remain correct; no S23-DUR guarantee
   is weakened.
7. Whole solution green; frozen R2 oracle `185/185` with output
   `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`; `dotnet format
   Andirit.slnx --verify-no-changes` exit 0; Release build 0 warnings / 0 errors.
8. Producer evidence plus independent `S23-PUB-Q1` qualification (reviewer ≠ implementer) and a
   separate `S23-PUB-OWNER` decision.

## 6. Stop conditions (return to owner)

- A new **public API** or a new **storage/publication path** is required.
- A schema **format** migration or any non-additive persisted change is required (D5).
- Satisfying Q1-Q1 requires durably persisting the full authority envelope in segment records
  (a durability change beyond re-stage/re-verify) — present Option B / a separately authorized
  segment-durability package.
- Enforcing the association would weaken `asmctx3`/`usage4` or the Option A separation.
- D3/D4/D5 is weakened, a frozen identity formula changes, or an S23-DUR guarantee regresses.
- Segment resumption cannot be made fail-closed.

## 7. Explicitly excluded

WP-S23-4 (lifecycle L1–L20, SQ-03 native schedule test, real-fact N1–N8 adaptation); WP-S23-5
(S2.3 owner acceptance); S2.5; Stage 6; S2.4 reader continuation/pagination; public Find
Usages; schema-format migration, dual-write, legacy readers/adapters; general background/
distributed indexing or automatic repair; MVCC/snapshot isolation; crash/power-loss/
multi-process recovery; O-Q2-01 remediation; package publication. No successor package is
authorized by this specification.

## 8. Qualification and ordering

`WP-S23-3A implementation → S23-PUB-Q1 independent qualification → S23-PUB-OWNER decision`.
Further WP-S23-3 packages (if any) and WP-S23-4/WP-S23-5 each require a separate owner
commission. Q1-Q1 and Q1-Q2 remain binding qualifications of WP-S23-2 and are not silently
closed by this specification.
