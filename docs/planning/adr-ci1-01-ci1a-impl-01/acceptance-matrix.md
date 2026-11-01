# Acceptance matrix — ADR-CI1-01-CI1A-IMPL-01

Authority: `docs/decisions/adr-ci1-01-owner-decision.md`. This matrix binds each
accepted invariant to a required observable behavior, a positive test, a
negative/adversarial test, an environment, and the required independent review
stage. It does not authorize implementation.

## 1. D4 — the ten accepted invariants (transcribed verbatim)

Transcribed exactly from `docs/decisions/adr-ci1-01-owner-decision.md` §D.4:

1. A query may use only an index consistent with the accepted published
   occurrence-fact generation.
2. A stale, unbound or unverifiable derived index must refuse with an explicit
   status.
3. A stale index must not silently trigger an unbounded full-store scan.
4. Failed staging must preserve the last accepted publication and corresponding
   valid index state.
5. Cancelled staging must not expose partial facts or index entries.
6. Superseded publication must not replace a newer accepted generation.
7. Rebuild/reopen must produce a semantically equivalent published index.
8. Contributor changes and scoped deletion must update publication and index
   visibility consistently.
9. Bounded queries must have deterministic ordering and explicit result limits.
10. Query behaviour during concurrent publication must follow a defined snapshot
    or freshness contract.

## 2. D4 acceptance matrix

| # | Observable behavior | Positive test | Negative/adversarial test | Env | Independent stage |
| --- | --- | --- | --- | --- | --- |
| 1 | query uses only the accepted published occurrence generation | publish → query returns current | query with an index bound to a prior publication | memory + native | post-impl review |
| 2 | unverifiable/stale index refused with explicit status | binding fields populated; query succeeds | tamper/absent binding → refusal | native | post-impl review |
| 3 | no unbounded fallback scan | bounded page read | stale query against a large store confirms bounded work | native | post-impl review |
| 4 | failed staging retains prior publication + index | prior store answers after failure | inject `CompleteIndexRunAsync` failure | native (real write tx) | post-impl review |
| 5 | cancelled staging exposes nothing | accepted state intact | cancel path (`complete:false`) | memory + native | post-impl review |
| 6 | monotonic publication; older run cannot overwrite | current run survives | superseded completion attempt | native | post-impl review |
| 7 | reopen/rebuild yields equivalent ordered result | reopen → equal result | reopen after replace/delete | native durable file | post-impl review |
| 8 | contributor set + scoped deletion consistent | union for identical facts; delete removes only target | conflicting payload; contributor removal | memory + native | post-impl review |
| 9 | deterministic order + explicit limit | stable ordering at limit boundary | limit boundary | memory + native | post-impl review |
| 10 | defined concurrency/snapshot contract | publication change → explicit result | in-process interleaving (production where possible) | native | post-impl review |

**S-02 requirement:** every native row above must be exercised against actual
`LatticeCodeGraphStore`; in-process interleaving is not accepted as native
atomicity evidence.

## 3. D3 — legacy graph noninterference matrix

| Surface | Positive check | Negative/adversarial check | Env |
| --- | --- | --- | --- |
| legacy node/edge identity sets | byte-equal ids before/after occurrence publication | occurrence must not create graph nodes/edges | native |
| `FindCallers` | same ids/order/cardinality | occurrence must not be returned | memory + native |
| `FindReferences` | same ids/order/cardinality | occurrence must not be returned | memory + native |
| unfiltered traversal | same results from representative roots | occurrence kinds must not appear | native |
| counts/enumerations | unchanged totals | occurrence ingestion must not change totals | native |
| native mirror / consumer | unchanged | occurrence must not enter the mirror | native |
| replace/delete | graph untouched by occurrence unit replace/delete | — | native |
| reopen | unchanged graph after reopen | — | native |
| failed occurrence publication | graph unchanged | — | native |

D3 corpus: see `specification.md` §6. Required events: initial publication,
replacement, deletion, reopen, failed occurrence publication.

## 4. D5 — historical compatibility matrix

| State | Required behavior | Positive/negative test | Env |
| --- | --- | --- | --- |
| supported graph-only historical store (schema 2) | legacy graph readable as before | open + legacy query | native |
| occurrence absent on schema 2 | `AnalysisUnavailable`/reindex-required (never empty-success) | occurrence query on schema-2 store | native |
| unsupported experimental occurrence format (`occurrence-facts/1…3`, `usage1…3`, `asmctx1/asmctx2`) | explicit refusal, no reinterpretation, no auto-delete | open/import/query old format | native |
| stale publication | explicit stale/`PublicationChanged` status | query with stale handle | native |
| corrupted/malformed storage | explicit failure | malformed store | native |
| fresh compatible store after re-ingestion | complete valid publication | re-ingest → publish → query | native |

## 5. Identity/provenance acceptance (S-01)

| Check | Required behavior | Test |
| --- | --- | --- |
| descriptor source bytes | descriptor hashes the actual indexed source bytes used for binding | S-01 probe: supplied-vs-indexed bytes mismatch must fail/refuse |
| equal-descriptor compilations | distinct `asmctx3`; each binds uniquely | two-compilation adversary |
| omitted/unresolved/unbound | refuse with recorded reason | omission/unbound adversary |
| linked contexts | distinct definitions | linked-file adversary |
| cross-project positive | confirms; calling unit owns occurrence; defining unit owns target | positive control |
| options/reference/source change | context changes; reindex required | invalidation adversary |

## 6. Required independent review

After implementation, an independent review must reproduce the D3/D4/D5 matrices
against production storage and verify that principal query results are produced
by production code, not test oracles. Producer-passing suites are evidence only,
not independent qualification.
