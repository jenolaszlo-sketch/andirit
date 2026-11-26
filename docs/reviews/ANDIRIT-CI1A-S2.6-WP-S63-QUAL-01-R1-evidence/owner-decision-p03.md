# P-03 owner decision assessment

**F-S63Q-07 · Low · Open · Prepared for owner decision**

## Governing text

- Owner architecture decision §D.4 item 3 says “A stale index must not silently trigger an unbounded full-store scan.” [Owner decision, §D.4](../../decisions/adr-ci1-01-owner-decision.md)
- Item 9 says “Bounded queries must have deterministic ordering and explicit result limits.” The CI1A acceptance matrix §2 row 3 specifies “bounded page read” and “stale query against a large store confirms bounded work.” The S6.3 entry expected observation is stale refusal without full scan. [CI1A acceptance matrix](../../planning/adr-ci1-01-ci1a-impl-01/acceptance-matrix.md) · [S6.3 entry assessment, §8.2](../ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01.md)
- The entry qualification plan §4.1 names P-03 `StaleIndex_NoUnboundedScan` and specifies “Large fact set + stale index + bounded query” with `Examined` ≤ `Limit + 2`. The entry traceability matrix illustrates this as 102 with a limit of 100. Section B of the owner architecture record describes Candidate C's observed 100 returned / 102 examined, alongside much larger scan observations for other candidates. [Entry qualification plan](../ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-qualification-plan.md) · [Entry traceability](../ANDIRIT-CI1A-S2.6-WP-S63-ENTRY-01-traceability-matrix.md)

The public `Examined` diagnostic counts target index entries examined. It differs from returned cardinality and physical I/O counts. Current source looks up the target index, copies and sorts its candidate entries and subsequently limits the returned page. In this path work and allocation grow with target cardinality even for a one-entry page. A stale authority is rejected before reading the target entries. [Diagnostic contract](../../../src/Andirit/CodeOccurrenceReader.cs#L118) · [Query implementation](../../../src/Andirit/InMemoryCodeGraphStore.cs#L945)

## Observed on R2

P-03's existing fixture returned 100 and examined 200 target entries among 400 facts. The independent scale controls below separated stale authority from current target work. The six stale probes returned `StalePublication`, zero results and zero examined.

| Target entries | Unrelated entries | Limit | Current examined | Current returned | Numerically within limit + 2 |
| ---: | ---: | ---: | ---: | ---: | --- |
| 31 | 169 | 1 | 31 | 1 | No |
| 31 | 169 | 100 | 31 | 31 | Yes |
| 200 | 200 | 1 | 200 | 1 | No |
| 200 | 200 | 100 | 200 | 100 | No |
| 400 | 300 | 1 | 400 | 1 | No |
| 400 | 300 | 100 | 400 | 100 | No |

The divergence is externally observable in the reported diagnostic. Current-target copying and sorting also consume memory and execution time. There is no demonstrated stale fallback or breach of the returned-result bound. The observations establish a real work-bound shortfall if the numeric wording is applied to current authority.

## Interpretations and gate effects

1. **A stale-refusal verification diagnostic.** The plan case name and entry P-03 expected observation concern stale authority and unbounded fallback. The governing D4-3 text is conditional on stale state. D4-9 sets a deterministic result limit. The owner decision describes 102 as an experiment result, and the normative acceptance row establishes a bounded retrieval observation without that number. Under this reading the six zero-examined stale observations satisfy the underlying requirement. The healthy query proves target locality and output bounds. The plan should say which metric and observation apply to each request state.
2. **A current-target work criterion.** “Bounded query,” the separately reported `Examined` metric, the explicit `≤` in the plan, the 102-example and the choice of an indexed candidate support a small page-proportional work expectation for current authority. A stale-refusal diagnostic always reports zero here and cannot exercise a 102-entry cutoff. Under this reading the committed P-03 is green by testing a different criterion, and the independent current-authority measurements directly falsify the intended cap. If the owner confirms this as an applicable binding requirement, the current record supplies no qualifying pass for that added criterion. A focused, separately authorized remediation and requalification would be needed before accepting that criterion at G-6, with the corresponding G-5 scope revisited.

**Recommendation.** Retain the qualified G-5 disposition under the binding wording in force. Record an explicit owner interpretation before G-6 and align the qualification plan and traceability rows with it. If the intent is a work guarantee, specify its authority, applicable request path and measurement and resolve the observed limit overruns before G-6. This assessment records the distinction and leaves the plan finding open.
