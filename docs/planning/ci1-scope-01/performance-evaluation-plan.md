# Performance and utility evaluation plan

## Measured baseline, not a candidate benchmark

Environment: Windows build 10.0.26200, .NET SDK 10.0.401/runtime 10.0.12,
Roslyn package 5.9.0, Release, 18 logical processors reported.
Source commit cd2b7fb5d73f4dc462b0ef7fcadfd75a91c118a2.
Plugin reports 0.2.0-preview.6. No paid model call or external benchmark service.

| Observation | Measured value |
| --- | ---: |
| Two-source controlled fixture, decoded input | 425 UTF-8 bytes |
| Fixture extraction only | 302.5447 ms |
| Fixture validated ingestion + completion | 26.0207 ms |
| Fixture retained nodes/declarations/edges | 11 / 8 / 18 |
| Real core subset | 47 files, 382,944 UTF-8 text bytes |
| Real extraction only | 3,045.3709 ms |
| Real ingestion + completion | 112.8454 ms |
| Real retained nodes/declarations/edges | 1,981 / 1,976 / 7,935 |
| Real serialized recording batches | 6,709,636 UTF-8 bytes |

Timings are one observation per mode after Roslyn oracle setup in the same process;
they are not cold p50/p95, full indexing-service time or end-to-end task time.
Recorded-batch JSON bytes are not durable-file bytes, compressed size or managed
allocation. No candidate overhead has been measured. Wider three-project
extraction failed; that run is not performance evidence of successful indexing.

## Five actual target lookups

Each row times four public bound API calls: selection, declarations, callers,
references. Names are exact graph names as published, including unresolved
CancellationToken display names. Source discovery from the recording sink is
analysis setup, not a proposed full-symbol scan requirement.

| Target | Four-step summed ms | Serialized envelope bytes | Stored calls / stronger-oracle invocations |
| --- | ---: | ---: | ---: |
| EnsurePublication | 40.9520 | 11,588 | 1 / 11 |
| FindSymbolsByQualifiedNameAsync | 0.9506 | 17,181 | 2 / 2 |
| FindDeclarationsWithProvenanceAsync | 0.4305 | 11,662 | 1 / 1 |
| Positive | 0.4070 | 11,355 | 1 / 8 |
| ICodeGraphReader.GetLatestPublicationAsync | 0.2792 | 39,700 | 7 / 9 |

First step includes recorded-node selection plus materialization/JIT effects. No comparative speed claim.
The oracle adds explicit SDK implicit global usings and is a stronger analysis
context; these counts do not define a same-context precision/recall score.
Positive's eight source calls and the three-call fixture independently isolate
retained-site loss. No selected returned edge was found to target a wrong method
in source inspection, but a complete false-positive/negative census is not done.

## Actual source-retrieval baseline

[measure-baseline.ps1](analysis/measure-baseline.ps1) executes five rg searches,
then reads five files used in the scenarios. It reuses separately measured
query envelopes; it does not pretend to time those calls again.
[workflow-baseline.json](evidence/workflow-baseline.json):

- 20 public query calls + one shared publication-open call.
- Five search processes, five source-file reads.
- 78,895 source-text bytes, 4,546 search-output UTF-8 bytes.
- 91,486 serialized query-envelope bytes.
- 200.2101 ms retrieval-only wall time, excluding query execution and human/model
  interpretation. It is not time to an actionable engineering answer.
- Combined measured retrieval payload components: 174,927 bytes. This is an
  accounting sum, not actual transported model context or tokens.
- Model/context tokens and actionable-answer time are null/unmeasured.
- Actual chat tool invocations can batch multiple logical operations; logical
  query count must not be mislabeled model tool-call count.

Raw JSON serialization is a reproducible payload proxy. It excludes tool-wrapper
overhead, caches, compression, model reasoning and host source-read shaping.
Do not convert bytes to token savings with an assumed bytes/token ratio.
The manually investigated task was not instrumented for total active agent time.

## Future paired protocol

Freeze source revision, fixture expectations, provider/analyzer/runtime versions,
hardware/power settings and bound publication; record all context differences.
Keep source setup/indexing separate from warm queries and complete engineering
task work. Use at least scenarios 1-4 plus C01/C03/C11/C16.

Baseline arm: current exact queries + ordinary rg/read operations + joining
evidence. Candidate arm: implemented FindUsagesV1 + necessary source reads and
follow-up queries. If no candidate exists, keep candidate cells unknown; proposed
JSON is not a measured prototype. Any prototype may use only available evidence
and must label unrecoverable sites, never fabricate them from counters.

Capture a timestamped operation ledger with request/result UTF-8 bytes, API
calls, actual host tool calls, source files/bytes read, cache hits, active
interpretation time and time to independently judged actionable answer.
Capture tokens only if the runtime reports them, with tokenizer/version, input/
output/cached accounting and full engineering task boundary. No paid model calls
merely for numbers. A deterministic manual replay is acceptable but not model
token evidence.

Correctness ledger per target/context: relevant independently expected sites,
correct confirmations, wrong-target confirmations, false omissions WITHIN
supported coverage, deferred/unknown sites surfaced, and misleading completeness.
Never count a duplicate name-token reference as another found invocation.
Incorrect confirmations and hidden truncation are blockers independent of speed.

## Index/storage economics

Measure both providers, default projection plus legacy native traversal behavior:

1. Fresh process cold full index; unchanged-index reuse; one-site deletion,
   line-shift edit, dependency-context change and producer-version reindex.
2. At least 30 warm-query samples after documented warmup; report median/p95,
   distribution, timeouts and allocations, not only a best sample.
3. Count added occurrence/coverage facts, target-index entries, encoded payload
   bytes, durable file/log growth, managed peak allocation and reopen/replay time.
4. Exercise high U/P ratios (3, 8, 100, 10,000 sites per pair), high fan-in,
   linked contexts, many projects, incomplete metadata and all hard budgets.
5. Include failed/cancelled work and warm caches; exclude nothing silently.
   Ordinary local index/query must need no network/restore.

Compute amortization for Q repeated tasks:
total baseline = baseline index + Q*(queries + source retrieval + interpretation);
total candidate = candidate index + Q*(queries + source retrieval + interpretation)
plus storage/reopen operational costs. Report break-even Q only after measurement;
if denominator is nonpositive, no break-even savings is established.
New fact bytes and context reduction may pull in opposite directions.

BENCHMARKS.md has historical extraction and schema-1 storage baselines on older
runtimes/packages; they are context, not CI-1 performance acceptance values.

## Future gate and unknowns

Owner sets acceptable absolute/relative overhead AFTER paired measurements and
correctness pass. Require zero wrong confirmations/occurrence collapse/stale
facts in the mandatory controlled corpus, honest unsupported coverage and bounded
work/output. Utility must justify added indexing/storage across repeated tasks;
otherwise narrow or reject the feature.

Unknown: candidate query/index latency, allocations, durable bytes/replay,
complete task time, tokens, break-even frequency, full real-repository success,
platform-general performance and operational crash durability. No fabricated
improvement, savings percentage or total-cost claim is made.

## R1 amendment (CI-1-SCOPE-R1)

Keep these quantities strictly separate; do not combine them into a same-context
score or a savings claim (S03):

| Quantity | Status |
| --- | --- |
| Existing extractor behaviour (occurrence collapse) | independently reproduced |
| Roslyn oracle behaviour | a stronger context (adds SDK implicit global usings); independently reproduced |
| Compilation-context difference | recorded, **not** a precision/recall score |
| Successful 47-file measurements | single observations, not distributions |
| Failed wider extraction | prerequisite defect, not performance evidence |
| Query and source-retrieval costs | accounting sums (`174,927` bytes), not model context or tokens |
| Candidate overhead, tokens, total task cost, break-even | **unmeasured** |

The `EnsurePublication` 11-versus-1 comparison combines binding-context
omissions with aggregate collapse and must never be reported as ten occurrences
lost to deduplication. Serialized-byte measurements are not token savings. The
paired protocol is unchanged and must freeze success criteria and include
fresh-process cold samples, true host-call counts and real durable bytes before
any benefit is claimed.
