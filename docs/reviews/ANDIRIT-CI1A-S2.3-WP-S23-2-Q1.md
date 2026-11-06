# ANDIRIT-CI1A-S2.3-WP-S23-2-Q1

## Independent qualification: native authority persistence and reopen context gate

**Date:** 2026-11-06 (Asia/Manila). **Role:** independent qualification reviewer.
**Disposition: REMEDIATION REQUIRED.** No owner acceptance is issued.

The independently verified current-context gate passes matching A, direct-change,
transitive-source/options stale refusal with stable `asmctx3`, and fresh B controls.
The completed-publication carrier survives process boundaries. However, a reopened
store accepts an occurrence as complete after its owning published graph unit has
been removed. This violates the commissioned authority/projection mismatch and
fail-closed correspondence requirement. Passing implementation and regression tests
does not resolve this counterexample. No production remediation was performed.

## 1. Reviewed baseline, provenance, and final source state

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Previously accepted baseline / implementation parent | `b576acb004747c89d257d8e61721c17babe6c5a6` | `3d7c152eb2d15fe87f940ba165f2b5c2d44cfc2a` |
| Implementation | `51369b3b463b6a2f137ee3475b78c9cd43f4db85` | `2deec87bca15b24133be51263a7c9ea137b5b978` |
| Received and reviewed HEAD / final source HEAD before additive qualification commit | `0f124eae8ba70bab3a6eaf4c2cd1e049f3435a51` | `3f96dc428b63dee617615d7af7cd5bf647b11338` |

The canonical repository is `D:/projects/Andirit`, on `main`, initially clean,
with exactly one worktree at that path. Both consecutive parent relationships were
inspected in Git history. The implementation changes 17 files, +928/-16; its handoff
is `ANDIRIT-CI1A-S2.3-WP-S23-2-IMPL-01.md`. The following documentation commit adds
the implementation report/evidence. The actual Git diff, rather than a saved patch
rendering, controlled source review. The exact production diff is preserved anew in
the reviewer evidence directory.

The final qualification commit contains only this report, additive reviewer probe
source, and additive reviewer evidence. Its exact final HEAD/tree are reported in
the external completion handoff; a commit cannot embed its own eventual identifier
and tree in a tracked report without changing those identities. The pre-commit
repository state and source-integrity checks are saved in `final-state-before-commit.json`.
No source, historical test/evidence, decision, or frozen input is changed.

## 2. Scope and independent methodology

Evidence: [`CI1A-S2.3-WP-S23-2-Q1-evidence/`](CI1A-S2.3-WP-S23-2-Q1-evidence/).
Reviewer-owned harness: [`CI1A-S2.3-WP-S23-2-Q1-tests/`](CI1A-S2.3-WP-S23-2-Q1-tests/).

Review covered the received committed implementation against the accepted baseline,
the work-package row, Option A §5.1/§5.2, mapping contract, native test-8b gate,
S2.2 acceptance requirements, NEW-01 owner qualifications, and D3/D4/D5. All 18
producer-listed controlling inputs were independently byte-checked; the key
contracts were read against production behavior. Frozen authority and oracle
integrity were established from Git, not inferred from passing tests.

The new executable does **not** link the implementation's test fixture or process
program. It uses the existing friend-assembly capability, without changing production
visibility. It independently reads five files from disk in each process, constructs
real Roslyn compilations, captures platform metadata, creates verified sources and
the verified dependency universe, invokes the production extractor/preparation/gate,
and decodes native records with reviewer-owned code. The graph is
`Seed -> Bridge -> Api -> Client`, plus `Empty`. Each published source-owned unit
also has an ordinary graph node, so graph loss is observably distinguishable from
occurrence results. The graph nodes are reviewer fixtures, not full plugin wiring.

Expected target identities come from the unchanged frozen R2 `Describe` implementation
and frozen tuple hashing, applied to the same real input profile; expected `usage4`
is separately assembled with the frozen tuple. These expectations do not use the new
persisted carrier or production mapper. Component digests remain explicit S-01 caller
inputs. The fixture is bounded and does not certify the full production TPA inventory,
arbitrary repositories, or end-to-end plugin/batch/sink publication.

There are 56 final matrix invocations with 56 distinct PIDs, plus a separate three-process
confirmation of the graph-orphan defect and three independent conflict-control processes. Normal process exit/disposal separates
publisher and reader. No live compilation or verifier crosses that boundary.
Native decoded snapshots include graph units/stages/index state, occurrence
units/pages/meta, run state, and ordering baselines. Read operations are checked for
exact snapshot conservation. The temporary databases are not committed; their decoded
records, exact source text/hashes, options/context identities, commands, and raw outputs
are preserved. This is process-boundary evidence, not abrupt-kill or power-loss evidence.

## 3. Production authority-path analysis and entry-point inventory

The authority chain is:

`source bytes -> verified source -> Roslyn compilation/binding -> exact defining entry -> compctx2 -> R2 compctx1/asmctx3 -> usage4/fact -> native carrier -> query current-context verifier`.

| Production entry or helper | Observed responsibility and boundary |
| --- | --- |
| `VerifiedCSharpSource.ReadAsync` | Acquires bytes, verifies the expected byte hash, decodes supported encodings, retains an owned snapshot. It does not reload later filesystem changes. |
| `VerifiedMetadataReference.Capture`, `VerifiedCSharpCompilation.Create` | Capture immutable metadata; verify source/tree correspondence, options, exact project-reference instances, dependency closure and conflicts; derive dependency-sensitive `compctx2`. These authority constructors are unchanged. |
| `VerifiedCSharpScope.Declare`, `VerifiedCSharpUniverse.Create/RequireCompilation/ResolveDefining` | Verify scope/source membership, complete dependency universe, exact live caller/defining binding, and recorded-context equality. Identity-equal substitute compilations do not satisfy exact membership. |
| `VerifiedCSharpUniverse.MintTarget/MintUsage` | Enforce supplied recorded defining context before frozen target/usage construction. The new receipt is emitted after successful mint, using the resolved defining entry, not automatically the caller. |
| `VerifiedCSharpOccurrenceExtractor.Extract` | Collects per-fact receipts, source ownership, coverage and ordered verified membership. Substituted sources cannot acquire an envelope. |
| `VerifiedOccurrenceExtraction.PrepareNativeUnit` | Internal repository/plugin ownership check and storage-unit binding; attaches authority and mandatory marker. Full plugin/sink handoff remains WP-S23-3. |
| `LatticeCodeGraphStore.StoreIndexRunAsync/StageIndexUnitAsync/StageIndexUnitDeletionAsync/CompleteIndexRunAsync` | All public mutation entry points route to serialized `MutateAsync`. Failed/cancelled terminal runs drop staging. Staging does not publish facts. |
| `MutateAsync`, `Persist`, `PublishStagedRun`, `PersistOccurrences` | Validate in-memory prospective state, then write graph units, accepted index state, occurrence units/pages/meta and authority in one native transaction on completion. Replacement/deletion update the occurrence ownership map. Retained authority is rebound to the completion run. |
| `PersistedOccurrenceUnit`, `PersistedOccurrenceMeta` | Additive JSON envelope, mandatory flag, and publication association; native graph schema remains 2, occurrence schema/namespace remain 1 / `andirit-occurrence-facts/1`. |
| Constructor / both `OpenAsync` overloads; `DurableCommandLog.Reconstruct/ReplayAsync`; `RestoreOccurrences` | Reconstruct graph, baselines and occurrence projection from durable records. Carrier JSON remains opaque during reconstruction. Successful construction alone does not authorize occurrence queries. Internal replay `replace/delete` cases are reconstruction helpers, not additional public publication APIs. |
| `RebuildInnerFromDurableStateAsync` | Used by both known apply/persist failure branches; rebuilds graph, baselines and occurrences/authority snapshot. Assignment-before-full-restore retains Q1-Q2. Unknown commit outcome quarantines instead of replaying. |
| `VerifiedCSharpOccurrenceAuthority.Bind/Validate` | Bind requires exact current caller in a complete verified universe. Validate compares live scope/universe/members and defining context, payload/target ownership, current source hashes, and recomputed frozen target mapping. Persisted identifiers cannot construct this verifier. |
| `LatticeCodeGraphStore.FindOccurrencesAsync/CheckOccurrenceAuthorityAsync` | Under `_writeGate`, checks quarantine, accepted publication, row counts, missing/versioned carrier, durable ownership key and per-unit verifier before delegating to the occurrence reader. **It does not compare occurrence owners with the actual published graph owner set** (F-01). |
| `InMemoryCodeGraphStore.FindOccurrencesAsync`, restore helpers | Restores the target index from occurrence unit facts and checks publication/coverage before returning ordered bounded hits. Native target-page IDs are not the source of reopened hit membership (P38). |

`CodeOccurrenceQuery.CurrentAuthority` and the verifier/envelope types are internal.
The public query constructor cannot accept a claimed digest as a capability. Omitted
current authority is refused for authority-required data. Legacy raw/synthetic queries
retain their accepted storage behavior; supplying the verified gate never promotes
legacy missing provenance. Current context must be supplied from newly verified live
inputs by an internal caller. Reusing an old immutable universe after filesystem changes
is not autonomous current-context detection, and is outside the claimed mechanism.

The verified universe, Roslyn compilations, source text and descriptor arrays are
immutable snapshots in the reviewed path. The native authority cache is built from
the same committed occurrence rows and installed only following a known successful
commit, or reconstructed on reopen/recovery. There is no independent durable cache
write. These observations support the demonstrated behavior; they do not prove
every hostile internal mutation or arbitrary re-use of stale caller snapshots safe.

## 4. Independent probe matrix

`probe-matrix.json` preserves the original expected/actual predicates, with raw
per-process `P*.json/log` and paired corruption controls `C27`–`C38`. Numeric enum
values are 0 Available, 1 AnalysisUnavailable, 2 UnsupportedSchema, 3 StalePublication,
4 CorruptedIndex, 5 IncompleteAnalysis. All successful negatives below return no
hits, no successful completeness, and zero examined entries. All query-only snapshots
remain exactly unchanged.

| Probe(s) | Actual input / expected outcome | Actual outcome and distinguishing control |
| --- | --- | --- |
| P01–P02 | Publish A, exit, independently acquire matching inputs in a fresh process; Available / 1 hit / complete | Pass; exact published graph/occurrence rows and authority preserved. Rules out refusing all reopened data. |
| P03 | Same persisted A, omit current verifier | AnalysisUnavailable. Matching P02 proves absence of authority, rather than lack of facts, causes refusal. |
| P04 | Supply descriptor-equal caller from another freshly built live universe | Bind throws `MissingDefiningCompilation`; recorded in notes. A subsequent valid query succeeds. Rules out descriptor-only caller proof. |
| P05 | Only `Seed.cs`: return value `11` -> `29`; Api/Bridge/Client source and options unchanged | StalePublication. Api `asmctx3` stays identical; defining `compctx2` changes. P02 and fresh B P12/P13 discriminate stale detection from blanket refusal. |
| P06 | Only Seed compilation overflow checking false -> true; all source bytes unchanged | StalePublication with the same target and changed defining `compctx2`. Fresh options publication/reopen P15/P16 succeeds. |
| P07 | Only Api source body adds `+ 1` | StalePublication; direct defining change refused before target lookup. |
| P08–P09 | Stage B, inject before-commit IOException at completion, exit/reopen | Original injected error retained; B query stale; independent A reader Available / 1. Published graph/index-state/occurrence rows exactly A. |
| P10–P11 | Stage B, mark run Failed, reopen | B stale; A remains Available / 1. No aborted B graph or authority becomes published. |
| P12–P14 | Replace A with legitimately changed source-B, exit/reopen, then query old A | Fresh B Available / 1; old A StalePublication. B receipts persist B rather than reusing A context. |
| P15–P16 | Publish options variant, inject after-commit failure | Original instance quarantines reads and writes; fresh reopen finds committed options graph/facts/authority Available / 1. No blind retry or rollback assertion. |
| P17–P19 | Publish, then delete Client through accepted staged deletion while retaining Empty | Client graph unit and occurrence/receipt retire; fresh reader Available / zero / complete under remaining valid coverage. No resurrection. |
| P20 | Legitimate graph-only publication | AnalysisUnavailable / null coverage, distinct from complete-empty. |
| P21–P23 | Publish only verified Empty; matching, missing, and changed current context | Matching Available / zero / complete; omitted current AnalysisUnavailable; changed current StalePublication. |
| P24–P26 | Stage in one process, terminate, complete in another without re-staging | Graph publication survives; verified and raw occurrence queries AnalysisUnavailable. Q1-Q1 is retained. |
| P27–P28 | Remove or null the envelope inside an existing occurrence unit | AnalysisUnavailable. Each has a separate intact-publication positive control. |
| P29 | Replace envelope with string garbage | CorruptedIndex; baseline control succeeds. |
| P30 | Envelope version 99 | UnsupportedSchema; baseline version-1 control succeeds. |
| P31 | Replace receipt context with correctly formatted `compctx2:sha256:` plus 64 `c` characters | StalePublication. Distinguishes actual verified-context equality from string-format checking. |
| P32 | Null per-fact receipt list | CorruptedIndex, with no exception/data leakage. |
| P33 | Envelope storage owner `other.csproj` | CorruptedIndex. |
| P34 | Replace receipt payload hash with 64 zero characters | CorruptedIndex. |
| P35 | Change authority publication run to `review:foreign` | StalePublication. |
| **P36** | **Delete only the published Client `HetuUnit` row; expected explicit refusal** | **FAIL: Available / 1 hit / complete while graph API lists only Empty. F-01.** All occurrence records and accepted state are untouched. |
| P37 | Change occurrence meta snapshot identity/consistency while run/index identity stays equal | StalePublication. The downstream full publication comparison rejects what the outer gate's run/index checks alone would miss. |
| P38 | Empty native page occurrence-ID array; exploratory expectation CorruptedIndex | Available / 1 / complete, correctly rebuilt from verified unit facts. No wrong facts or false empty demonstrated; observation O-01. Original unmet stronger-refusal predicate retained. |
| P39 | Explicit matching Partial contribution | IncompleteAnalysis / 1 / not complete. |
| P40 | Set occurrence meta CompleteCoverage true over persisted Partial units; exploratory expectation CorruptedIndex | Still IncompleteAnalysis / 1 / not complete. No escalation of Partial authority; observation O-02. Original predicate retained. |
| P41–P44 | Publish A+Empty; stage only Client, terminate, resume with Empty retained; independently reopen again | Prior A stays usable while staging; resumed/fresh queries AnalysisUnavailable due to missing Client carrier. The retained valid Empty unit cannot mask the missing authority. |
| Confirmation | Separate publisher -> graph-owner deletion process -> separate reader process | Same P36 defect; exactly one native key removed; last two snapshots identical. See `orphan-confirmation.json`. |
| Conflict control | Publish A+Empty, then attempt only B Client with retained A Empty under the identical target | Mixed-universe completion throws the original ArgumentException; B query stale, fresh A query Available / 1. Exact published graph/index-state/occurrence rows conserved; `conflict-checks.json` passes all seven checks. |

Raw matrix accounting is **53/56 original predicates met**. Of the three unmet
predicates, P36 is a contract-impacting failure. P38/P40 are stronger exploratory
refusal hypotheses whose actual results preserve the observable accepted semantics;
they have not been rewritten as passing tests. All 62 final process invocations
completed normally; process exit zero is not a claim that every qualification
predicate passed. The initial reviewer decoder error and commands are separately
preserved and excluded from final behavioral counts.

The exact unchanged target for A/source-B/options is
`asmctx3:sha256:694070afd3d577b746033576ff70d7775e04d8a92271b98b6bd3c0793ba81fd5`.
Api defining contexts are:

- A: `compctx2:sha256:1d77ffb8fdefe881332c4f9803efd9326618e601b8695b15b7013eacbe1a8406`.
- Source-B: `compctx2:sha256:f5ad6108d0abc025eee5ea1daa34cfdc9b1dd88add3e609393616056fe408c6a`.
- Options: `compctx2:sha256:38b3cb1af7d0daf9bab4e251d4f9862e2f49237e795257337ebd1014aa0e54df`.

`independent-analysis.json` independently confirms that exactly Seed source changed
for B, no source changed for options, and only Seed's options digest changed.
`relationships.json` establishes stable targets, changed defining contexts,
unchanged stale-query rows, before-commit conservation, and legitimate B replacement.

## 5. Atomic persistence and refusal/availability conclusions

Known-success completion writes graph units/index state, occurrences, pages/meta
and the attached authority inside the same native transaction. `_writeGate`
serializes mutation with graph and occurrence reads. The in-memory speculative apply
is not observable through public readers until commit handling finishes. Independent
before-commit failure, abort, publication/replacement/deletion and fresh-process
snapshots support transaction consistency on those exercised paths. Unknown outcome
quarantines the instance; independent reopen reconciles the actually committed state.

No partial successful publication was observed in the exercised known-failure paths.
That is **not** a tested guarantee for torn files, arbitrary crash points, power loss,
recovery-rebuild failures, multiple writers, or platform-independent database atomicity.
F-01 is a missing reopen cross-projection integrity check, not evidence that the
native transaction partially committed.

For intact recorded units, independent live authority is required before returning
occurrences; digest comparison is not solely between two persisted values. Direct,
transitive and option stale facts were refused without being transformed into
successful empty results. Fresh changed authority was accepted. The preserved empty,
Partial, unavailable, retirement and resumption distinctions passed. The exception
is the ownership mismatch in F-01: current verified compilation context proves
semantic freshness but does not prove that the unit still belongs to the graph
publication returned by the same store.

## 6. Build, test, oracle, and provenance verification

All commands ran from `D:/projects/Andirit`. Exact argv, exit values, raw stdout/stderr,
and per-process output paths are in `commands.json` and corresponding `.log` files.
The final independent execution environment is Windows x64 10.0.26200, SDK 10.0.401,
runtime 10.0.12. No network documentation was required for this local committed-source
qualification; the controlling contracts are repository artifacts.

| Exact command | Independently observed result |
| --- | --- |
| `python docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/run-review.py integrity` | 38/38 exact Git-byte manifest entries, complete inventory, 17/17 implementation source bindings; all 18 authorities unchanged from baseline; two Git-vs-working authority hash discrepancies retained (F-02). |
| `dotnet build docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/ReviewerProbe.csproj -c Release` | Exit 0; 0 warnings/errors. Only reviewer project/dependency restore/build outputs added outside tracked source. |
| `python docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/run-review.py processes` | Final 56 processes; 53 original predicates met, F-01 reproduced; two non-blocking exploratory deviations described above. |
| `python docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/run-review.py confirm-orphan` | Three separate processes; P36 reproduced in an additional fresh reader. |
| `python docs/reviews/CI1A-S2.3-WP-S23-2-Q1-tests/run-review.py conflict` | Three separate processes; conflicting contexts refused, original A publication conserved. Builds reviewer project with `dotnet build <reviewer-project> -c Release --no-restore` first; exit 0, no warnings/errors. |
| `dotnet --info` | Recorded environment above. |
| `dotnet test Andirit.slnx -c Release --no-restore` | 828 passed, 0 failed/skipped: native 50, TreeSitter 157, core 345, CSharp 276. Includes the 32 implementation WP-S23-2 tests. |
| `dotnet test docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj -c Release --no-restore` | 17 passed, 0 failed/skipped, including known-failure projection recovery, conflict refusal, mirrors and unknown-outcome quarantine. |
| `dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -c Release --no-restore -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output docs/reviews/CI1A-S2.3-WP-S23-2-Q1-evidence/oracle-r2.json` | 185/185, 0 failed; 146851 output bytes, exact expected observed-output SHA-256. |
| `dotnet format Andirit.slnx --verify-no-changes --no-restore` | Exit 0; no tracked changes. |
| `dotnet build Andirit.slnx -c Release --no-restore` | Exit 0; 0 warnings/errors. |

The regression commands are also reproduced by the reviewer driver's `regressions`
mode. Producer `run-verification.py` was **not** run because it writes into historical
implementation evidence. All rerun output is reviewer-owned.

The 38 producer manifest entries were read with `git show <received HEAD>:<path>`,
hashed as raw bytes, and compared for byte length and SHA-256. `git ls-tree` confirmed
there are no omitted/unlisted producer evidence files except the self-excluded manifest.
All 17 source-binding blob IDs, lengths and SHA-256 values match implementation Git
objects. `docs/decisions`, `docs/planning`, `docs/evidence`, frozen abstractions,
verified compilation/descriptor/source constructors, and accepted NEW-01 reviewer
source/evidence show no changes from the accepted baseline to received HEAD.
Final checks also protect them against qualification-time mutation.

Frozen expected input SHA-256:
`274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF`.
Independent observed output SHA-256:
`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`.
The accepted distinction remains: frozen expected contains 176 checks; verification
appends nine frozen-section comparisons and reports 185. No frozen content was
rewritten or normalized.

## 7. Findings, reproduction, and required correction

### F-01 — High / blocking: occurrence authority does not verify published graph ownership

**Contract impact:** Q1-B authority/projection mismatch, Q1-D graph/occurrence
correspondence, Q1-E prevention of orphan/retired records being served as authoritative,
and the fail-closed reopen obligation. This is a persisted corruption/mismatch
counterexample, not a demonstrated ordinary staged-deletion or transaction failure.
The stale-context portion of Option A test 8b passes independently.

**Production locations:** `LatticeCodeGraphStore.OccurrenceAuthority.cs:18–36`
obtains the accepted publication but validates only the occurrence manifest's
self-reported unit/fact counts. Lines 35–58 iterate the occurrence-unit set, without
checking that each storage owner exists in published graph units or that all graph
units requiring authority have an occurrence association. `RestoreOccurrences`
(`LatticeCodeGraphStore.cs:554–637`) reconstructs occurrences independently of the
graph-owner set. The current C# verifier sees real current compilations and validates
their contexts, but it is not supplied the actual graph publication membership.

**Minimal reproduction:** use the five source files recorded in
`C36-control-graph-orphan.json`, with `overflow.txt` set to false. After building the
reviewer executable:

```text
dotnet <ReviewerProbe.dll> publish <corpus> <db> <control.json> a
dotnet <ReviewerProbe.dll> mutate <corpus> <db> <tampered.json> graph-orphan
dotnet <ReviewerProbe.dll> query <corpus> <db> <fresh.json>
```

The automated `confirm-orphan` mode creates these files and runs that sequence.
The mutation deletes exactly:

```text
HetuUnit|review:q1-native-authority\nreview.q1.csharp\nClient.csproj
```

No source/options, occurrence facts/receipts/meta/pages, run, baseline or index-state
record changes. Initially graph API lists Client and Empty and the occurrence query
returns one complete current hit. Afterwards graph API lists only Empty; the
independently reopened reader still returns Client's occurrence, Available / 1 /
IsComplete=true, with no refusal reason. The separate reader consumes exactly the
mutated durable snapshot. `independent-analysis.json` and `orphan-confirmation.json`
prove the one-record difference and reader conservation.

**Shared assumption:** production gate and implementation mutation tests treat the
occurrence table as the complete ownership inventory. Existing malformed-authority
tests mutate envelopes inside an existing unit and retain graph ownership. They
therefore do not expose a missing graph-owner association. The independent counterpart
changes that external ownership fact while preserving all identifiers checked by the gate.

**Required correction, separately authorized:** before positive availability, establish
the association between the actual accepted graph owner set and authority-bearing
occurrence units, with explicit refusal for orphan/missing mandatory associations.
Cover both directions with negative controls and preserve legitimate graph-only,
complete-empty, retained-owner, replacement/deletion and Q1-Q1 semantics. An additive
internal ownership check should be assessed before considering broader representation
changes. No API, identity or semantic-contract expansion is recommended or performed.

### F-02 — Low / non-blocking provenance precision: two authority hashes identify working bytes

The implementation evidence manifest itself passes 38/38 exact Git-byte checks.
Two entries inside its `authority-inputs.json` identify working-file bytes rather
than Git bytes. The exact discrepancy is:

| Authority path | Git bytes / SHA-256 | Recorded working bytes / SHA-256 |
| --- | --- | --- |
| `docs/decisions/adr-ci1-01-ci1a-s2-3-new-01-owner-01.md` | 15018 / `1B1B1F57DC0D8155CF139CEEAF959793DD59923AC8AA8DFA8F773D4EAB7E1BFC` | 15019 / `208C9AEDFCC5A3C9FD51A06AE3D29DE32181C3677BAF25CE8B1C0AFA2E8C60C1` |
| `docs/planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md` | 15602 / `78247A46BC0137158A022B000DD8DBF053DD5B17EB3105B68B985167CFB3D69B` | 15610 / `1D6569D7B0CBBCDF2FA59E0F8098518BEEDF2430199A2CFAED0905DCC6736DD2` |

Comparing a temporary in-memory CRLF-to-LF transform establishes that line endings
fully explain both differences. No file or historical evidence was transformed.
Both Git blobs are unchanged from the accepted baseline, and recorded hashes match
the actual working bytes. This is evidence-label precision, not altered controlling
authority. `authority-byte-discrepancy.json` preserves the raw values. Any clarification
must be additive; the received manifest/evidence remains untouched.

### O-01 / O-02 — Informational exploratory results, no demonstrated contract defect

P38's damaged native page ID list does not govern reconstructed hits; unit facts
rebuild the actual in-memory target index. It yields the correct hit, not a false
empty. No stronger native-page integrity certification is granted. P40's changed
aggregate complete flag does not override per-unit Partial coverage in the observable
reader. These results challenge the reviewer's initial stronger refusal hypotheses,
not the accepted correct-data/coverage behavior. Original expected and observed
results remain recorded, rather than silently adjusting tests to improve the count.

## 8. Preserved qualifications and unverified claims

- **Q1-Q1 retained:** segment records omit occurrence facts and the full authority
  envelope. Cross-process resumed runs can publish graph-only results. Both initially
  empty and retained-other-unit process controls refuse missing authority; occurrence
  reconstruction across a staging restart is not accepted. WP-S23-3 still requires
  explicit re-staging/re-verification or separately authorized durability work.
- **Q1-Q2 retained:** a hypothetical failure inside rebuild can partially assign
  internal state and mask the original exception. Existing known-failure paths and
  original error propagation passed; there is no accepted rebuild-internal injector
  or proof of stronger recovery-failure atomicity/quarantine.
- **S-01 retained:** integrated component-digest canonicalization/proof belongs to
  S2.5. Neither the frozen tuple comparison nor the real Roslyn fixture proves
  arbitrary caller-supplied digests canonical.
- **D3 retained within tested scope:** graph/segment JSON contains no occurrence
  facts/full carrier and no occurrence graph nodes/edges. The mandatory marker is
  additive. Reviewer graph nodes are independent ordinary nodes.
- **D4 partially supported, challenged by F-01:** serialized visibility, known-failure
  rollback, stale/current publication checks, ordering/limits and normal retirement
  remain supported by source and rerun tests. Do not claim complete cross-projection
  integrity on corrupted reopen while the missing owner check remains.
- **D5 retained:** schemas/namespaces and frozen identities are unchanged; additive
  carrier fields only, no migration/dual-write/normalization or evidence repair.
- Windows x64/toolchain-scoped verification only. No general cross-platform,
  abrupt-crash/power-loss, unrestricted concurrency or performance guarantee.
- No reliance on unaccepted NEW-01 behavior. No public Find Usages or automatic
  filesystem freshness capability. Full plugin/batch/sink integration and broad
  real-fact lifecycle qualification remain separately commissioned work.

The first reviewer run incorrectly attempted to base64-decode `HetuBaseline`, whose
payload is a raw publication baseline string. The decoder was corrected only in the
new reviewer harness; the initial failure log/command records are preserved as
`initial-harness-decoder-failure.log` and `initial-commands.json`. Initial provenance
assertions also exposed F-02; `initial-provenance.json` remains preserved. No producer
test or expectation was repaired. Final process results use the corrected evidence
decoder; production behavior was not changed.

## 9. Qualification disposition and stop

**REMEDIATION REQUIRED** for F-01. Independently demonstrated test-8b context
validity, successful transactional completion, and fresh authority acceptance are
retained as positive evidence. They do not compensate for the false positive on a
missing graph owner. Require a separate bounded remediation assignment and independent
requalification of ownership correspondence and its negative/positive controls before
owner acceptance of the durability gate.

Only additive qualification material is committed. No push, publication, extra branch,
worktree, reset/clean/restore, owner acceptance, or WP-S23-3 implementation occurred.
The next decision belongs to the owner.
