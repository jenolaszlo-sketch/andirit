# Storage alternatives: reopened ADR-CI1-01

**Status: reopened. No winner. OWNER DECISION REQUIRED.**
This document replaces the `architecture-decisions.md` preference for separate
facts. It does not authorize schema, codec or storage code.

The Q1 reviewer did not find that separate facts are wrong; the reviewer found
that separate facts had **not earned preference** over a versioned occurrence
edge on measured cost or correctness grounds. R1 therefore evaluates both
serious candidates against the same mandatory V1 guarantees and specifies the
minimum experiment needed for an owner decision. The storage choice must not be
made irreversible during remediation.

## Candidates

**C — separate source-owned occurrence facts.** A provider-neutral
`UsageOccurrenceV1` value per distinct supported site, stored alongside the
existing aggregate edges, owned by the semantic project unit, with its own
validated payload, target index, codec and snapshot handling.

**B2 — new versioned occurrence edge kind alongside existing aggregate edges.**
A new edge kind (for example `invokes`) that carries typed site semantics as
validated edge properties, reusing the existing edge/store/contributor/
replacement/adjacency infrastructure, excluded from legacy presets.

Rejected or subsumed: **A** (aggregate + on-demand rebinding) cannot answer
fact-only offline queries and repeats parse/bind cost. **B** (occurrence-aware
*calls* IDs) silently changes legacy IDs, cardinalities and budgets. **D** (typed
witness collections) effectively becomes a side table at scale with contributor/
paging/conflict rules and is not clearly cheaper than C. The two serious
candidates are C and B2.

## Requirement-by-requirement assessment

Status key: **OK** = can meet the guarantee; **RISK** = can meet it only with
explicit new validation/work; **OPEN** = not yet demonstrated; **SHARED** = both
candidates carry the same obligation and neither is advantaged.

| # | Mandatory V1 guarantee | C (separate facts) | B2 (occurrence edge) | Assessment |
| --- | --- | --- | --- | --- |
| 1 | Physical occurrence identity | One value per site; identity tuple explicit | One edge per site; identity tuple explicit | SHARED |
| 2 | Exact target and source evidence | Typed payload, exact span/hash/target | Typed validated edge properties | RISK for B2: edge property bags must be *validated*, not free-form; C must still validate |
| 3 | No change to legacy aggregate-edge semantics | New fact family; legacy untouched | New kind excluded from legacy presets; legacy edges untouched | RISK for B2: exposure via unfiltered/native traversal and total graph counts must be controlled; presets alone are insufficient |
| 4 | Target-indexed bounded queries | Needs a target index | Can reuse source-owner/target adjacency, but existing adjacency APIs materialize and sort work | OPEN: neither demonstrates a cheaper ordered target range; B2 must not inherit unbounded adjacency enumeration |
| 5 | Source ownership | Owned by semantic project unit | Owned by semantic project unit | SHARED |
| 6 | Reindex, deletion, replacement | Project-unit atomic replacement + target-index update | Existing replacement infrastructure already stages edges | Slight B2 reuse advantage; both must prove atomic replacement/deletion of exactly this unit's sites |
| 7 | Failure/cancellation retention | Retain last successful facts and coverage | Retain last successful edges and coverage | SHARED |
| 8 | Deterministic ordering | Canonical ID/tuple order | Canonical edge ID/order + validated properties | SHARED |
| 9 | Memory and LatticeDB parity | New projection must be built in both | Edges already replayed by both | RISK for C: extra provider work; RISK for B2: new kind must be validated/ordered identically in both |
| 10 | Existing durable stores | New codec/fact-kind route | New kind/property validation route | SHARED |
| 11 | Snapshot codecs | New occurrence section | New edge kind in existing unit payload | SHARED |
| 12 | Validation and contributor conflicts | Typed occurrence validator; conflicting payload rejects publication | Typed edge-property validator; conflicting payload rejects publication | RISK for B2: generic property bags are not automatically a safe typed contract |
| 13 | Processing and storage overhead | O(U) occurrence payload + index | O(U) edges + property payload | OPEN: no measured winner |
| 14 | Query performance and agent utility | Answer-oriented range read | Answer-oriented range read over edge kind | OPEN: depends on index design, not representation |

Reviewer-provided facts behind the risk ratings:

- C adds batch/accumulator/replacement/fact-kind/provenance/store/query/codec/
  snapshot contracts.
- B2 adds kind/property validation and risks exposure through unfiltered/native
  traversal and total graph counts; preserving named presets alone is
  insufficient.
- Neither option currently demonstrates a cheaper ordered target range or a
  proof/byte-bound behaviour.
- B2's existing adjacency APIs materialize and sort work and cannot be assumed
  to satisfy hard processing quotas unchanged.
- C's "cleaner" shape is a maintainability argument, not measured lifecycle or
  correctness superiority.

## Shared obligations (neither representation removes these)

Independent of C or B2, V1 still requires:

- A length-prefixed, domain-separated occurrence ID that keeps unit/project
  context and the exact byte hash; source edits intentionally retire all old
  file IDs; declaration moves change physical declaration IDs; linked files in
  two projects stay distinct even when targets coincide.
- Immutable occurrence payload separated from sorted proof provenance;
  conflicting payloads reject publication and retain prior state.
- Context/producer changes invalidate reusable analysis even when source bytes
  and the normalized target are unchanged.
- A handle is meaningful only with its exact publication; continuity across
  edits is not promised.
- Exact source-byte provenance per `corrections.md` C3.
- Honest, persisted, disjoint coverage (`corrections.md` C8).

## Minimum experiment for an owner decision

Do not choose C or B2 from this document. The owner should require the following
comparative experiment before durable authorization. Both arms must use the same
payload, provenance, validation, ordered lookup, replacement, replay and
snapshot workload.

1. **Payload and index.** Encode the same occurrence set as C facts and as B2
   edges; measure added records, index entries, encoded bytes and durable/log
   growth at U/P ratios 3, 8, 100 and 10,000 sites per pair, high fan-in and
   linked contexts.
2. **Ordered target query.** Measure a deterministic target-indexed prefix read
   with explicit processing/byte quotas on both representations; record examined
   duplicates/proofs/joins. Confirm neither performs an unbounded pre-sort or
   full-repository materialization.
3. **Lifecycle.** Successful replacement, obsolete-unit deletion, shifted/
   deleted bytes, linked/dependency-context change, plugin/semantic version
   change, unchanged reuse, idempotent retry, contributor survival, conflict
   rejection, failed/rejected/cancelled staging retention, superseded completion,
   and no staged leakage.
4. **Parity.** Byte-identical normalized results and coverage on memory, Lattice
   projection, separate-process reopen, and snapshot export/import; old query
   equality and explicitly decided unfiltered/native behaviour.
5. **Cost.** Replay/reopen time, managed peak allocation, and amortization over
   plausible query frequencies; exclude nothing silently.
6. **Correctness first.** Zero wrong confirmations, zero occurrence collapse and
   zero stale facts on the mandatory controlled corpus, with honest unsupported
   coverage and bounded work/output. A representation that cannot pass the F01
   identity gate is disqualified regardless of cost.

Freeze success criteria before the comparative run. The owner may narrow or
reject the feature if no useful total-cost benefit appears. Until then, ADR-CI1-01
remains OPEN and both representations remain qualified alternatives.
