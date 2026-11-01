# CI-1A Stage 2 consolidated obligation register

**Recorded:** 2026-11-02. **Authority:** OWNER-01, documentation only.
**Foundation:** [REM-01 accepted with qualifications](adr-ci1-01-ci1a-s2-boundary-rem-01-owner-acceptance.md)
at reviewed `aaea1ca7f32a666bc9c6480e91c75c7dd14779d4` /
`4012bf14e680a742da7a3785831fb578d9f92eb4`.
No Stage 2 production implementation is authorized by this register.

## 1. Sources and closure rules

Authority keys (paths relative to the canonical repository unless external):

| Key | Exact source |
| --- | --- |
| OWNER | `docs/decisions/adr-ci1-01-owner-decision.md` §§D/F/G |
| OQ1 | `docs/decisions/adr-ci1-01-owner-01-q1-acceptance.md` §§B–D |
| RECON | `docs/decisions/adr-ci1-01-ci1a-s1-obligation-reconciliation.md`, individual ID sections |
| S1A | `docs/decisions/adr-ci1-01-ci1a-s1-owner-acceptance.md` |
| S2B/M/H | `docs/planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md`, `stage2-acceptance-matrix.md`, `stage2-handoff.md` |
| REM | `docs/planning/adr-ci1-01-ci1a-s2-rem-01/contract-reconciliation.md`, `finding-disposition.md`, `stage2-gate-reconciliation.md`, `qualification-matrix.md` |
| B | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-q1b\NEW-FINDINGS.md`, §Q1B-Fnn; manifest 29/29 verified |
| Q | `C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-boundary-rem-01-q1`; manifest 33/33 verified, SHA bound in acceptance |
| R2/Q3 | `...\Solo\adr-ci1-01-exp-02-r2\PROVENANCE-CONTRACT.md` and `...\Solo\adr-ci1-01-exp-02-q3\TARGET-IDENTITY-REVIEW.md`, incorporated by OWNER §F |

Every F row cites its actual original definition in B, producer disposition
in REM, and independent final disposition in Q/`FINDING-REQUALIFICATION.md`
§Fnn. Probe references refer to Q/`ADVERSARIAL-PROBES.md` and hashed evidence
logs, not executions during OWNER-01. No finding is renamed away. All open
implementation obligations require **separate independent source-bound review
and owner acceptance** (closure authority **IR+O** below). Documentary corrections
are closed by **owner verified diff (O-doc)**; future independent review must
confirm dependent acceptance evidence. Closed means only the stated foundation
claim; deferred capabilities are not implemented or independently qualified.

## 2. Original six blockers: individual acceptance matrix

| ID / original severity / source | Defect and exact remediation / production location | Independent evidence; accepted claim | Status, qualification and remaining work | Blocking gate / evidence to close / authority |
| --- | --- | --- | --- | --- |
| F01 / Blocking / B §F01 | Weak occurrence/target tuples replaced with full R2 tuples in `src/Andirit.Abstractions/CodeOccurrence.cs`; validator recomputes in `src/Andirit/CodeGraphBatchValidator.cs`; `OccurrenceIdentityRemediationTests.cs`. Semantic source unit separated from storage contributor. | Q/`IDENTITY-REVIEW.md` §§1–4; P01/P01b/P02/P03/P04b frozen vectors, discriminatory cases and P13b authentic refusal. Accept canonical encoding/identity enforcement. | Closed, independently verified. Canonical-shaped unverified context still structural; pre-REM store refused wholesale. S2.2 must establish actual minting authority, not another tuple. | No remaining F01 foundation blocker. Real source/target correspondence before S2.2 acceptance; independent frozen-vector and live-definition evidence; IR+O. |
| F02 / Blocking / B §F02 | Required External fact contradicted refusal. S2B §5/§10, S2M §5.2, S2H §3 now require no fact/no canonical target; `CodeOccurrenceTargetId` and validator reject unavailable/noncanonical targets. | Q/`EXTERNAL-TARGET-REVIEW.md` §§1–4, P04. Accept corrected refusal contract and structural enforcement. | Closed with qualifications. A fabricated canonical-shaped target is structurally accepted; full live defining-compilation proof remains S2.2. | Before S2.2 acceptance: actual out-of-scope/ambiguous/missing defining compilation refusal before minting, plus genuine cross-project positive; IR+O. |
| F03 / Blocking / B §F03 | Resolution, analysis completeness and availability conflated. `CodeOccurrenceCoverage` fields, `CodeOccurrenceReader.cs` incomplete status/coverage, both stores and `PersistedOccurrenceUnit/Meta` now separate and persist them. | Q/`COVERAGE-AVAILABILITY-REVIEW.md`, P05/P06/P07/P07b/P16. Accept dimension separation and native reopen state mapping. | Closed with qualifications. Counts/Produced are structural claims, not membership proof. Scope/universe authority S2.1/S2.2; merge defect NEW-04 remains. | S2.1 provenance claims need actual membership proof; S2.2 coverage authority correction and S2.4 surface; S2.5 integrated evidence; IR+O. |
| F04 / Blocking / B §F04 | Zero-fact analysis dropped by sink/native predicates. `CodeGraphIngestionSink.cs`, `CodeGraphStoreModels.cs`, both stores preserve analysis flag/coverage; `PersistOccurrences`/`RestoreOccurrences` retain empty units/meta. | Q/`ZERO-FACT-NATIVE-REVIEW.md`, P05/P08 raw native unit=1/pages=0/complete meta; P07/P07b/P14/P18c negatives. Accept durable complete-empty lifecycle. | Closed, independently verified. Completeness authority remains scope-relative; no extractor completeness is inferred. | No F04 residual; real semantic empty/non-analysis negative pair at S2.3/S2.4/S2.5; IR+O for new implementation. |
| F05 / Blocking / B §F05 | Ack-loss local/durable divergence/retry undefined. `LatticeCodeGraphStore.cs` adds unknown exception, `_outcomeUnknown`, quarantine, conservative commit classification and reopen reconciliation; `publication-uncertainty.md`. | Q/`COMMIT-UNCERTAINTY-REVIEW.md`, P09/P10/P12/P17, N6, P11 code audit. Accept ordinary quarantine/reopen/retry safety. | Closed with qualifications. NEW-01 known-failure replay loses occurrence projection; durable state intact. No crash/power-loss/multi-process claim. | NEW-01 before S2.3 and any earlier production recovery reliance. Native schedule/reopen evidence S2.3/S2.5; broader fault qualification Stage 6 only if separately commissioned; IR+O. |
| F06 / Blocking / B §F06 | Circular preauth code/tests, future tests credited, fixture/timing/count contradictions. S2B/M/H and REM gate ledger separate contract, code and acceptance; S-01 tests assigned S2.1/S2.5. | Q/`STAGE2-GATE-REVIEW.md` §§2–4; Q finding requalification §F06. Accept authorization-safe corrected contract. | Closed with qualifications. NEW-02 fixed now; F15 wording fixed now. F16 nine-part row preparation remains, with no future tests credited. | Before dependent slice test authoring: complete F16 row traceability; all rows before S2 acceptance. Correct prerequisites never authorize implementation; O-doc then IR+O. |

## 3. Remaining original findings

| ID / original severity / exact sources | Current disposition and accepted correction/evidence | Missing work / owner disposition | Named blocking stage / required closure evidence / authority |
| --- | --- | --- | --- |
| F07 / Significant / B §F07; REM finding-disposition F07; Q requalification F07, IDENTITY review | Closed, independently verified: canonical constructor/validator recomputation, computed-only payload hash, P03/P03b conflict rejection. | Accept foundation enforcement; retain source provenance obligation under F11. | No F07 residual; future minting qualification S2.2 IR+O. |
| F08 / Significant / B §F08; REM F08; Q requalification F08, COVERAGE review | Closed, independently verified: persisted coverage/unit/meta and conservative Partial for legacy absent coverage, P05/P14/P16. | Accept persistence; actual producer scope/universe/counters still S2.2. | New producer/coverage and reopen proof S2.2/S2.3/S2.5; IR+O. |
| F09 / Significant / B §F09; REM F09; Q requalification F09 | Contract closed; extractor correctly deferred. ConditionalInactive vocabulary and declared configuration rule fixed in S2B §10/S2M §5.6; no inactive confirmed-fact path. | Require actual disabled-text scanning bound to language/preprocessor configuration; contract closure is not scanner completion. | S2.2 implementation/acceptance, independently reproduced positives/negatives by S2.5; IR+O. |
| F10 / Significant / B §F10; REM F10; Q requalification F10, COVERAGE review | Partially corrected, independently verified: coverage attached, IncompleteAnalysis and missing-target semantics, Examined=target-index entries (P05/P07). | Require bounded pagination/continuation and applicable cross-target internal contract; no public API. | S2.4 acceptance and S2.5 review: continuation across publication changes, ordering/limits, completeness/availability assertions; IR+O. |
| F11 / Significant / B §F11; REM F11; Q requalification F11, S2-1-READINESS | Partially corrected: existing byte materialization/SHA verify and same-buffer Roslyn text inspected; canonical context input contract fixed. | Require actual per-document hashes/options/references, compctx1, scope/universe/live membership S2.1; full semantic correspondence S2.2. Existing discovery is bounded, not general MSBuild equivalence. | S2.1 independent provenance tests before S2.2; integrated S-01 proof S2.5; IR+O. |
| F12 / Significant / B §F12 + REAL-PROJECT-FIXTURE-AUDIT §§1–5; REM gate reconciliation §6; Q requalification F12 + OWNER-HANDOFF §2 | Open qualification: nine C# files/32 proposed rows below minima; current fixture explicitly not qualifying. OWNER-01 recounted files/rows, not a full semantic oracle. | Accepted deferred qualification; owner chooses existing bounded S2.0 preparation route for separate commissioning. See §5. No corpus acceptance or execution authorization. | Decision recorded now; corpus/oracle frozen and independently accepted before S2.2 qualification; stricter S2.2 test-authoring gate adopted here; IR+O. |
| F13 / Significant / B §F13; REM evidence-integrity-erratum; Q EVIDENCE-INTEGRITY-REVIEW/requalification F13 | Erratum verified with two corrections NEW-03. Historical technical evidence traceable/nonblocking with provenance qualifications. | Accept bounded technical evidence, qualify old full-integrity claim; append exact measured identities. Frozen packages remain untouched. | NEW-03 correction completed OWNER-01, O-doc. Historical aggregate tree/report remain unverified; no closure without authentic evidence. |
| F14 / Minor / B §F14; REM F14; Q requalification F14 | Corrected where claimed: duplicate §15, stale path and five/six count. NEW-02 residual fixed OWNER-01. | Accept corrected docs; no broader identity implementation defect inferred. | O-doc; later reviewer verifies corrected matrix before S2.2 test authoring. |
| F15 / Significant / B §F15; REM gate reconciliation §7; Q requalification F15 + STAGE2-GATE-REVIEW §4 | Reconciled independently: available authentic writers and Linux minimum at S2 acceptance; full certification Stage 6. Ambiguous S2B §11 wording aligned now. | Require immutable authentic fixture/copies; no-writer format requires explicit limitation and owner amendment, not tampered substitute. | Wording corrected before any S2 authorization (O-doc); available formats and Linux minimum S2.5 acceptance (IR+O); full certification Stage 6. |
| F16 / Minor / B §F16; REM F16; Q requalification F16 + STAGE2-GATE-REVIEW §4 | Union oracle corrected independently in S2M §6.4: identical semantic facts union contributors; distinct semantic units remain distinct. Full matrix row form deferred. | Require nine-part rows: requirement, authority, production surface, observable behavior, positive test, adversary, environment, evidence artifact, closure authority. | S2 preparation before each dependent slice's test authoring, all rows before S2 acceptance; S2.1 boundary §6 supplies this slice's test contract; IR+O. |

## 4. New REM-01-Q1 findings

Sources for each: Q/`NEW-FINDINGS.md` matching NEW-nn section, plus
Q/`OWNER-HANDOFF.md` §2 and specialist reviews named below.

| ID / independent class | Behavior, source and accepted evidence | Owner disposition / missing work | Blocking gate / evidence to close / authority |
| --- | --- | --- | --- |
| NEW-01 / Significant | `DurableCommandLog.ReplayAsync` restores graph units through `InMemoryCodeGraphStore.RestorePublishedIndexUnitAsync`, not occurrence availability/coverage/publication binding; native MutateAsync recovery paths replay. Q P09/P17 reports conservative AnalysisUnavailable despite complete durable publication; deliberate cross-unit conflict after replay may bypass lossy in-memory validation. D4.4/REM recovery contract require correspondence. | **Targeted foundation correctness remediation required.** Assigned required follow-up task `ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01` (separate authorization pending), mandatory before S2.3 starts depending on recovery. Do not implement under OWNER-01. | **Before S2.3 and any production recovery reliance**, including earlier use. Reproduce known pre-commit and in-memory apply failures; assert graph AND occurrence projections against durable authority, publication identity/coverage/facts/conflict refusal, successful recovery availability (no false empty/unavailable), distinguish unknown quarantine, close/reopen, preserve D3/D4/D5; IR+O. |
| NEW-02 / Minor documentation | S2M §2.1 stale pre-REM tuple; §2.2 components insufficiently explicit; Q IDENTITY/STAGE2 gate reviews. Production helper was correct. | Corrected now from frozen R2 §§4–5/Q3: exact usage4 order, source hash/unit/target doc, asmctx3 domains/components/encoding; frozen vectors and discrimination requirements; nearby contributor row clarified. | OWNER-01 O-doc; independent confirmation before S2.2 test authoring. No production identity change. |
| NEW-03 / Minor evidence | Erratum falsely said manifest records observed actual hash; Codex declared SHA/size actually match pinned blob, materialized file differs by EOL. Q EVIDENCE-INTEGRITY-REVIEW §§1–4. | Additive §4 correction records measured hashes/sizes/CRLF conditions and preserves old text/historical uncertainty. Owner reproduced Stage 1 21/22 and blob-vs-working byte conditions. | Completed OWNER-01 O-doc; frozen manifests/report not rewritten; aggregate tree remains unverified. |
| NEW-04 / Implementation follow-up | `CodeOccurrenceCoverage.Merge` takes first scope/universe and max counts; constructor exempts declared=0 from verified<=declared. P06 shows mixed scopes can claim Produced. | Required coverage-authority correction during S2.2. [S2.1 dependency analysis](adr-ci1-01-ci1a-s2-1-authorization-boundary.md#4-new-04-and-new-01-dependency-analysis) prohibits using the defective merge as provenance oracle. Real universe can be prepared without occurrence publication. | Before any S2.1 claim relying on this merge; otherwise correct during S2.2 and independently qualify before S2.2 acceptance. Test matching/different scope, matching/different universe, verified empty vs missing evidence/no analysis/invalid zero declarations, partial+complete, changed membership, native persistence/reopen if published; IR+O. |

## 5. F12 explicit owner decision

**Selected disposition: Accepted deferred qualification.** The actual issue is
qualification evidence and timing, not contract identity or an implementation
defect. Accepted Candidate C/R2 authority already defines semantics; no new
architecture decision is necessary to acquire/compile verified source in S2.1.
REM-01 producer reconciliation §6 explicitly offers preauthorization freeze or
bounded S2.0 preparation; Q requalification F12 and owner handoff place the
decision before S2.2 qualification. OWNER-01 selects the **existing S2.0 bounded
preparation route** for a separate commission, with no new stage number and no
permission to execute preparation here.

Smallest remaining action: separately commission corpus preparation, name/pin
the reproducible actual project sources and variants, establish expected
positions/targets/refusals independently of the extractor, then freeze hashes
and obtain independent acceptance. The existing 9-file/32-row proposed oracle
is only a seed. S2.1's focused small projects cannot be represented as closing F12.

Minimum retained S2B §9/S2H §6 criteria: at least **10 C# files, 50 invocations,
20 method references, 5 overloaded methods, 3 namespaces, 2 projects, and 5
cross-project invocations** (retain S2H's stricter count). Include source edit,
options and reference changes, replacement/deletion, valid zero-usage,
unsupported/external/incomplete and ambiguous/missing binding cases. References
and unsupported categories are oracle/coverage cases, not expanded confirmed
fact scope. End-to-end path must use actual source, actual Roslyn compilation,
real binding, native C publication, reopen, internal lookup, verified positions
and target identities. Corpus/oracle freeze and independent acceptance block
S2.2 qualification; this decision additionally requires readiness before S2.2
qualification-test authoring. Any infeasible criterion returns for owner action;
an implementer cannot silently lower minima or self-generate its oracle.

## 6. Inherited Stage 1 obligations (all retained)

Each ID's exact source is RECON's matching section and Stage 1 Q1 `FINDINGS.md`
under `...\Solo\adr-ci1-01-ci1a-impl-01-s1-q1`; current evidence/disposition is
qualified by Q and REM gate reconciliation. Mandatory work closes by IR+O unless
explicitly classified O-doc or a standing accepted limitation.

| ID | Behavior / accepted evidence / current status | Missing work and owner disposition | Exact blocking point / closure evidence |
| --- | --- | --- | --- |
| SQ-01 | Pages are derived from authoritative unit facts; accepted limitation, no independent full page-integrity verification on every open. | Preserve/document boundary; corrupted/stale index must refuse, not silently scan. | No new S2 blocker; S2.3/S2.4 must preserve refusal; redesign requires separate authority. |
| SQ-02 | Unknown commit outcome contract and ordinary quarantine implemented/reviewed F05. Qualified. | Reuse conservatively, fix NEW-01; no blind retry, false rollback or crash claim. | NEW-01 gate above; S2.3/S2.5 native faults/reopen evidence. Broader crash/power-loss/multi-process only separately scoped Stage 6 qualification. |
| SQ-03 | Serialized `_writeGate`, no MVCC; contract accepted, native schedule qualification partial. | Native publication/query schedule test must demonstrate old/new complete results and truthful attribution. | S2.3/S2.5 before Stage 2 acceptance; IR+O. |
| SQ-04 | Authentic historical writer coverage partial; P13b pre-REM fixture now independently verified. | Available older experimental formats still need immutable master/per-instance hashes/disposable copies/refusal; no-writer formats explicitly limited with owner amendment. | S2.2/S2.5 available-format evidence before Stage 2 acceptance. Tampering current stores is not a substitute; full matrix Stage 6 as applicable. |
| SQ-05 | Windows native results accepted only in tested scope; Linux open. | Linux CI/native suite expanded to current tests with logs; describe locking/WAL/mmap differences. | S2.5 before Stage 2 acceptance; full cross-platform certification Stage 6. Retain all original 38 native tests plus additions, not a frozen 38-test total. |
| SQ-06 | Namespace mismatch separate from version mismatch; test evidence still an obligation. | `Store_RejectsUnsupportedOccurrenceNamespaceOnOpen` or equivalent isolated meta.Namespace tamper -> CodeOccurrenceSchemaException. | S2.3/S2.5 before Stage 2 acceptance; IR+O. Do not confuse SQ-04's historical label with this ID. |
| SQ-07 | Whole-repository publication/reopen work accepted; bounded retrieval does not imply bounded rebuild. | Measure real-project publication/reopen/query costs; do not preauthorize incremental maintenance/native-node-ID optimization. | S2.3 measurement and S2.5 evidence. Optimization optional only under a later separate commission. |
| IF-01 | Contributor identity/union strengthened F01/F07; R2 semantic unit differs from storage contributor. Partial integrated qualification. | Same semantic fact/two storage units unions; different semantic units/plugins remain distinct; payload conflict refuses; ordinal contributor order. | S2.2 identity and S2.3 lifecycle evidence before S2.5 acceptance; IR+O. |
| IF-02 | Current target-page encoding/order/limits qualified synthetically. | Verify real target sizes and site order including end coordinates/ID; planner bounds and pagination. | S2.3 storage/S2.4 bounded reader, S2.5 independent evidence. |
| IF-03 | Resolution/coverage/availability contract and minimal persistence fixed REM-01 F03/F08. Qualified. | Actual semantic counters, scope/universe membership, inactive configuration scanner, NEW-04 and full reader coverage. | S2.1 membership proof; S2.2 producer authority; S2.3 reopen; S2.4 surface; S2.5 acceptance. No completed extractor prerequisite before S2.1 authorization. |
| IF-04 | Reader remains internal; coverage partially corrected F10. | Continuation/pagination, applicable cross-target request and coverage breakdown; publication change handling. | S2.4 acceptance/S2.5 independent review. |
| IF-05 | Refusal vocabulary exists; exhaustive operational mapping partial. | Document/test actual open exceptions vs query statuses, graph-only/no-analysis, schema/namespace, stale/unbound page, incomplete coverage. P18c zero-page mismatch refuses at query. | S2.3 schema/reopen plus S2.4 contract; all before S2.5 acceptance. Do not falsely promise every mismatch is detected at open. |
| MD-01 | Standard self-excluded manifest accepted, O-doc. | Retain explicit scheme/path/coverage and self-exclusion. OWNER-01 binds actual Q manifest path. | Evidence packaging condition for every later handoff; no production blocker. |
| MD-02 | Single-run Debug performance explicitly non-benchmark/non-threshold; accepted O-doc boundary. | Keep qualification; new measurements state repetitions/environment. | S2.3 measurement/S2.5 evidence; no numerical threshold invented. |
| MD-03 | Public type visibility with internal reader contract accepted design. | Preserve boundary; public Find Usages requires separate ADR-CI1-02 owner decision. | Standing constraint at S2.4 and any public API authorization. |

## 7. Earlier owner obligations and standing qualifications

| Stable ID / exact source | Current evidence/status and owner disposition | Missing work / blocking point / evidence and closure authority |
| --- | --- | --- |
| S-01 / OQ1 §B.1; OWNER §F; RECON §5 | Contract fixed; capture exists; full production chain partial. | S2.1 byte/tree/context/options/reference/scope proof, S2.2 semantic correspondence, S2.5 integrated independent proof; IR+O. |
| S-02 / OQ1 §B.1; OWNER §D.4; RECON §5 | Applicable native subset qualified, NEW-01 limits in-memory D4.4; full ten-invariant native matrix open. | S2.3/S2.4 applicable tests and native schedule S2.5; all ten including cancellation/stress in Stage 6, IR+O. Earlier RECON's “6/10” wording enumerates seven numbers (1–4,6,7,9); use explicit invariant evidence, not this inconsistent aggregate count. |
| D3-full / OWNER §D.3; RECON §5 | Synthetic noninterference supported; native-mirror path not enabled in reviewer probe. | Real fixture graph identity/cardinality/callers/references/traversal/native-mirror observations S2.3/S2.5; full canonical real-project corpus Stage 6; IR+O. |
| D4.1–D4.10 / OWNER §D.4; Q D3-D4-D5 review §2 | 1 accepted generation, 2 refusal, 3 no unbounded scan, 4 failure retention, 6 supersession, 7 reopen, 8 contributor/deletion, 9 order/limits supported in tested scope; 4 has NEW-01; 10 serialized only; 5 not counted as exercised. | NEW-01 strict gate; real native lifecycle S2.3, bounded reader S2.4, native schedule S2.5; invariant 5 and full S-02 Stage 6. IR+O; no weakening or MVCC inference. |
| D5 / OWNER §D.5; RECON §5 | Accepted/preserved: no migration, dual-write, adapters, silent reinterpretation or automatic destructive rebuild; pre-REM refused in full. | Standing throughout S2.1–S2.5; schema/refusal and available authentic writers S2.5. Re-ingest available source into fresh store; aggregate/Hetu historical contracts untouched. |
| I-01 / OQ1 §B.2 | Integration surfaces pinned by specification/source map and Stage 2 boundary; partial ongoing application. | Re-pin concrete files/APIs at each slice's authorization and reviewer source binding; IR+O. |
| I-02 / OQ1 §B.2 | Canonical D3 corpus only synthetic so far; full corpus open. | Name/freeze real corpus under F12 for S2.3/S2.5 and full D3 corpus Stage 6; IR+O. |
| I-03 / OQ1 §B.2 | Unsupported schema/unavailable vocabulary fixed; full mapping partial IF-05. | S2.3/S2.4 exact exception/status and refusal tests, reviewed S2.5; IR+O. |
| I-04 / OQ1 §B.2 | Simple publication-bound maintenance and serialized policy fixed; cancellation remains unqualified. | Preserve policy S2.3/S2.4; full cancellation/no partial visibility Stage 6; IR+O. |
| I-05 / OQ1 §B.2; OWNER Q3-QUAL-05 | Experiment hook not accepted as a production API. | Verify no experimental public interleaving hook ships at every production slice acceptance, latest S2.5; use internal/test-only fault controls; IR+O. |
| M-01 / OQ1 §B.3 | Different generated native instances need per-instance identity; immutable P13b master is an example. | Every historical-fixture handoff S2.2/S2.5 records writer commit/commands, hashes, bytes and disposable-copy use; IR+O. |
| Q3-QUAL-01 / OWNER §G.1.1 | Declared-scope completeness accepted, not global. | S2.1/S2.2 prove every declared member and refuse omissions; standing boundary through S2.5. |
| Q3-QUAL-02 / OWNER §G.1.2 | Experimental descriptor hashes supplied bytes; no independent disk check established. | S-01 actual accepted buffer-to-tree proof S2.1; later S2.2 live binding; IR+O. |
| Q3-QUAL-03 / OWNER §G.1.3–4 | In-process interleaving and verification-to-return TOCTOU limits retained; results attributed to checked publication. | Native serialized schedule S2.3/S2.5; stronger isolation/crash/multi-process claims separately authorized and qualified Stage 6. |
| Q3-QUAL-04 / OWNER §G.1.5 | B2 deduplicated publication summary limitation retained historically; B2 not selected. | No C implementation credit; reevaluate only if B2 or multi-content-per-run scope is separately authorized, before that claim. |
| Q3-QUAL-05 / OWNER §G.1.6 | Public test hook limitation retained. | I-05 gate; no unchanged experimental hook in production. |
| Q2-F06 / OWNER §G.2 | Historical self-referential manifest trees unreproducible; preserved. | No claim of historical aggregate integrity without authentic reproducible evidence; every later citation retains qualification. |
| Q2-F07 / OWNER §G.2 | Historical fixture mutates on open, per-instance identity accepted boundary. | Immutable master/disposable copy and M-01 procedure before S2.5 reliance. |
| Q2-F08 / OWNER §G.2 | B2 graph exposure resolved for C by D3, historical B2 finding preserved. | No B2 parallel representation; any new proposal requires separate architecture amendment before implementation. |
| Q2-F09 / OWNER §G.2 | C rebuild blocks queries/no cancellation, addressed by D4 policy but not full qualification. | S2.3/S2.4 maintenance behavior, Stage 6 cancellation/full matrix; IR+O. |
| Q2-F10 / OWNER §G.2 | Higher C write/delete/import cost and larger fresh DB accepted tradeoff. | Real-project measurements S2.3/S2.5; no optimization credit without authorized measurement. |
| Q2-F11 / OWNER §G.2 | Older experimental migration/reader compatibility not demonstrated, D5 intentionally excludes migration. | Available authentic refusal evidence S2.5; aggregate/Hetu history unchanged; no-writer limit requires owner amendment. |
| Q2-F12 / OWNER §G.2 | Crash/power-loss/multi-process/cross-platform unproved. | Linux minimum S2.5; full platform and separately commissioned broader recovery Stage 6. Until then explicit nonclaims. |
| Q2-F13 / OWNER §G.2 | Benchmark spread/order instability retained. | Historical figures bounded to host/corpus/repeats; S2.3 new measurement reports variance/environment, no universal performance claim. |
| Q3-F-NEW-01 / OWNER §J + `adr-ci1-01-q3-f-new-01-evidence-erratum.md` | Existing freshness locator erratum retained. | No frozen file rewriting or claim of new index qualification; preserve locator correction in later evidence citations. |

## 8. Progression and closure

| Stage | Implementation boundary and mandatory incoming/acceptance gates |
| --- | --- |
| S2.0 | Existing contracts/authority and bounded corpus preparation route; separate commission required for preparation; no new milestone invented. |
| S2.1 | Verified source acquisition/compilation provenance under [the separate boundary](adr-ci1-01-ci1a-s2-1-authorization-boundary.md). Requires accepted contracts/foundation, not already-completed S-01 code. Independently accepted positive/negative evidence before S2.2. |
| S2.2 | Real Roslyn extraction, canonical minting/external refusal, inactive scanning; F12 frozen independent oracle, NEW-04 correction and accepted S2.1 evidence. |
| S2.3 | Native source-owned publication integration; NEW-01 fixed and independently qualified before reliance, real N1–N8 equivalents, replacement/deletion/reopen/conflict, applicable D4 and real-project D3. |
| S2.4 | Internal bounded reader/coverage, continuation and publication-change contract; preserve exact target and complete/incomplete/unavailable semantics. |
| S2.5 | Independent integrated source/compilation/semantic proof, real-project end-to-end/native reopen, D3/applicable D4, authentic available historical refusals, expanded Linux suite and complete obligation reconciliation before owner Stage 2 acceptance. |
| Stage 6 / later CI-1A | Full D3 corpus, all ten native D4/S-02 including cancellation, full platform certification, separately scoped crash/power-loss/multi-process qualification if claimed. These guarantees remain unproved until that evidence is accepted. |

No “future work” entry waives a correctness condition. If a dependency is reached
earlier, its correction/evidence gate moves before that reliance. Producer tests
do not constitute independent closure, and successful acceptance of one slice
does not authorize the next.
