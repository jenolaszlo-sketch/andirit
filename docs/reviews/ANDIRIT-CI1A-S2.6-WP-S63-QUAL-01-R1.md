# ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1

## Independent requalification following R2 remediation

**26 November 2026 · Andirit Stage 2.6, Work Package S6.3**

**Disposition · QUALIFIED WITH QUALIFICATIONS. G-5 granted.** Independent execution closed F-S63Q-01 through F-S63Q-06 and F-S63Q-08. F-S63Q-07 remains open for owner clarification of the P-03 verification plan. Its observed current-query work exceeds the plan's numeric expectation under the current-query reading. The binding stale-refusal, target-scoping, ordering and result-limit requirements are supported. G-6 has not been granted. S6.3 remains open for owner acceptance.

The qualification applies to the exact R2 commit below, with LatticeDbSharp 0.2.0, and to the tested platform and scenario boundaries in the gate matrix. No implementation remediation was necessary or performed during this commission.

## Subject and evidence provenance

| Subject | Commit | Root tree |
| --- | --- | --- |
| Frozen R1 | `d3c9359d1abd8c89a7214e37f074802e5d2fa238` | `6eb68de12467e95efa51c8b1a71b1c17713d9492` |
| Previous producer evidence | `dd484af2d32ff1f5b7951a85f4f3104c4b4b5402` | `505511dc6404e3d38fb9f80457f909b6e2aac5ba` |
| Independent Q1, `codex/andirit-s63-qual-01` | `a69c13ce447839330793be2e4cbc7a1dd732d1dd` | `a57df9a83cb692acef2c449b0c7eba657887f6d0` |
| **R2 tested** | `a96c19a844f8ea1719508198b47df23c4e203571` | `f05ded592bc82c4e0b02044c97fb1f509fdb6e8e` |
| R2 evidence and erratum seal | `ab2bd168ab2436ffb2a07de8466b33cecd79720b` | `b65c73eb5e8ceb3cc31c83028ccc7a03c4c1baee` |
| Published main and clean-checkout replay seal | `f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60` | `77274fed2469e30aeef705f487442c54f0e5133d` |

The authorized baseline-to-R2 delta contains seven paths. The verifier and workflow bind existing tests to immutable result receipts. The remaining five paths are the worker protocol and the native durability, deadline helper, deadline regression and publication test sources.

| Path | Independently verified effect |
| --- | --- |
| `.github/scripts/verify-s63-results.ps1` | New SHA/run/attempt/runner and TRX checks for per-project results and named Ubuntu cases |
| `.github/workflows/ci.yml` | Adds result capture, digest receipts, verifier execution and artifact upload to the existing test jobs |
| `tests/Andirit.LatticeDb.Tests.Worker/Program.cs` | Test-only process identity, publication and cancellation receipt protocol |
| `tests/Andirit.LatticeDb.Tests/NativeDurabilityTests.cs` | Tightens D-05 and D-07 controls and applies deadline helper |
| `tests/Andirit.LatticeDb.Tests/NativeOpenDeadline.cs` | New test-only retry helper with monotonic acceptance deadline |
| `tests/Andirit.LatticeDb.Tests/NativeOpenDeadlineTests.cs` | Nine regression cases for the actual helper |
| `tests/Andirit.LatticeDb.Tests/NativePublicationCorrectnessTests.cs` | Strengthens P-08, P-12 and P-15 observations |

The frozen-R1-to-R2 comparison adds 14 historical review/evidence path differences, all already in the previous evidence baseline. The complete path and parent inventory is in `provenance.log`. The tested commit is a direct child of the previous baseline. Five direct, evidence-only descendants reach published main. All six commits have approved owner author and committer identities and no AI trailers. Production source, build definitions, dependency pin, planned production API/ABI scope, solution membership, release workflow, test thresholds and inherited authority are unchanged. The published release tag `v0.2.0-preview.7` remains object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` peeled to `6ed05964d2fcc758648b654a01ec706647e52183`. The owner’s scoped R2 instruction authorized the minimum CI capture and producer fast-forward needed for this evidence.

## Independent execution

| Campaign | Outcome |
| --- | --- |
| Detached Windows x64 R2, SDK 10.0.401 | Restore, formatting, Release build, solution suite and four CI-equivalent project coverage campaigns passed. The first LatticeDb project run constructed and copied its worker before any solution build. Copied worker DLL, deps and runtimeconfig were inventoried and exercised. |
| Full suite, local and each hosted platform | 1,005 passed, 0 failed, 0 skipped per platform. Core 345, CSharp 372, TreeSitter 157, LatticeDb 131. Existing coverage thresholds 55%, 75%, 60% and 70% respectively all passed. LatticeDb was 900/969 lines and 368/466 branches (92.87% and 78.96%). |
| Named publication tests and prepared probes | 16/16 passed locally and independently attributed on Ubuntu, Windows and macOS. Prepared A–I passed three repetitions each (27/27). C and F are supported by a separate armed failure/rebuild probe. Probe I opened on the first attempt in all three runs. |
| D-07 coverage and diagnostic controls | 20/20 dedicated genuine Windows process-kill runs passed. All nine exact helper regressions and all seven focused recovery/cancellation/early-exit controls passed. Missing and prematurely exiting worker injections each made the actual D-07 test fail before recovery for the expected reason. |
| Original Q1 replay and independent additions | Same original harness Git blobs compiled independently against R1 and R2. Five original late acceptance trials reproduced on R1 and were rejected on R2. Independent native-handle disposal, delay overrun, throwing cleanup, P-03 scaling and failed-writer P-12 probes passed. |

The 989→1,005 increase is nine helper regressions, five D-07 oracle corruption tests, one early-exit guard test and one D-05 follow-up-oracle test. C# test inventory, per-project counts, test definitions and existing test gates support retention of the original mandatory observations. P-12's prior count-only loop was replaced by an exact old result, an actually queued reader and an exact new result. Original prepared probes were run from historical Git blobs.

### D-07 deadline, recovery and republication

The unchanged Q1 reproducer held the same native database past a 25 ms budget. R1 accepted at 120, 121, 122, 121 and 123 ms. R2 timed out at 25 ms five times, each preserving the last native `-16` error. Its Q1 harness exit 1 is the expected result of the original defect assertion, which explicitly expects the late acceptance. The remaining five Q1 probes passed. Native controls established immediate success, a timely retryable-to-success transition on the same file, persistence of `-16` diagnostics, propagation of native `-4` and propagation of a managed argument exception.

The helper checks monotonic time before starting another open and after a successful open, retries only native `-16`, and caps delay by remaining budget. A result exactly on the deadline is rejected, with disposal before timeout on that path. An independent synthetic clock made a real native handle arrive late. A raw native reopen of the same file immediately succeeded, with two attempts and the earlier `-16` retained. This uses real native ownership and deliberately controlled timing. A 25 ms requested wait observed at 40 ms caused one open attempt total. Throwing disposal at the 25 ms boundary remained observable, with one cleanup call. The deadline governs result acceptance. An in-flight synchronous native open can return after it.

The genuine D-07 scenario established the exact accepted baseline run, index and facts. A distinct PID acknowledged READY for the exact hold-only repository/run/unit, and a live contender received raw native `-16`. The host killed and reaped the ready worker. Fresh authority reads preserved the accepted publication, facts and native unit binding, found the exact held stage and no held publication/fact, and a subsequent same-repository completion changed the accepted publication and made the old handle stale. Attempts and process output are retained for every dedicated run. Corrupted recovery observations for missing stage or facts, wrong stage identity, wrong/missing publication and unintended held publication were rejected by the committed controls. The test owns worker shutdown and file cleanup in its outer cleanup path. The scenario covers abrupt termination after staging while the holder waits.

### Publication and cancellation assertions

**P-12** reads the exact old run and ordered occurrence identities/spans before a writer reaches a real native pre-commit checkpoint. A reader enters while that gate is held and is observed pending before release. The queued reader returns the exact new generation. The receipt binds run identity even where fixture content yields the same digest. The complementary probe uses disjoint paths and a writer that throws at the checkpoint, observes projection replacement and checks that the queued reader and durable reopen both return the prior run and facts. These are in-process observations of atomic reads.

**P-15** uses distinct holder and republication child PIDs, a live native lock refusal, and distinct baseline and newly accepted run/index identities for the same publication repository. The held stage uses a different repository to demonstrate database-wide ownership. Fresh native reopen confirms the new binding, the prior fact plus three worker contributions and explicit refusal of the stale handle. This supplies genuine multi-process evidence alongside D-05 and D-07.

**D-05** binds the expected requested run and cancellation token, then observes the completed publication from one fresh reader before cancellation and one after. The holder, completing publisher, two readers and requester have five distinct process IDs. Cancellation reports the intended pre-cancelled request and matches the accepted run/index. Exact occurrence results and native `HetuStage`/`HetuUnit` content preserve the durable accepted view and exclude new cancelled state. The committed oracle rejects an unexpected stage or loss of publication. This satisfies the S6.2 owner follow-up for F-QU-01 and F-QU-02 within the existing cancellation and locking limits. The S6.2 skipped test already called completion in-process and expected a surviving stage. The historical shorthand describing a writer that never completed still requires that context.

**P-08 and C/F** exercise rebuild directly. A planned failure crosses the intended checkpoint once, replaces the projection and returns the same accepted authority and ordered facts, including scope/provenance and opaque identities. The exact Q1 harness corroborates the prior view after native close/reopen and a distinct successful changed-publication control. Prepared C/F output carries supporting weight. Genuinely distinct processes and the stronger independent controls supply separate observations.

### Hosted attribution

GitHub records the exact tested SHA on main, push event, first attempt and four successful jobs (pack and three platform test jobs). Ubuntu was genuine GitHub-hosted native x64 on label `ubuntu-latest`, image `ubuntu-26.04` version `20261116.202.1`, Ubuntu 26.04.1 LTS. The three jobs selected SDK 11.0.100 and ran net10.0 assemblies. Windows was x64 and macOS was arm64. The independent local replay used SDK 10.0.401. This difference is recorded in the subject receipts and both campaigns returned the same counts.

GitHub artifact `15058443202` was fetched independently as the original ZIP. Its SHA-256 is `27bf91185de52fb10bdd6d0027f4d97f1957b5f9a3e94d666f1afd6c59584313`, matching GitHub metadata and the separately extracted member bytes. Per-project XML and GitHub job/step metadata independently bound every exact P-01–P-16 definition to one passing result. Process transcripts demonstrate D-05 fresh observers, D-07 READY and exit 137 on Ubuntu, and P-15 same-repository republication with distinct holder and publisher. P-12 includes the queued-reader receipt. The Windows and macOS evidence corroborated all named outcomes and the expected native process observations. The six result/coverage artifact identifiers, digests, exact test identifiers and counts are in `hosted-verification.log`. [R2 workflow run](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45691273312)

The workflow retains unfiltered test commands, their failure exit status, coverage thresholds and the four-job structure. Evidence capture, artifact upload and the verifier do not replace those gates. Independently generated mutations were rejected for missing, duplicate, skipped or failed mandatory case, omitted worker output, bad digest, incomplete or malformed TRX and wrong SHA or run. The duplicate, skipped and failure controls refreshed file hashes. Full suite outcomes were independently parsed across each platform, beyond the verifier's named scope.

### Evidence integrity and R1 erratum

The restored 17-entry original producer package is byte-identical to pre-breach tree `4dee055c7691351bc14fe15efbbe5db75c8d8fa4` at `33b5867b593c7c7e43108b538bdcddad6fc4fb15`. The defective R1 extension tree `b61cf46a9d28c626faa5b5c904b5ea5f8b9c4565` and its original manifest remain preserved. The erratum's two text sequences match the original expected lengths/hashes through exact CRLF reconstruction. The original missing 219,244-byte Ubuntu coverage XML was matched to expected SHA-256 `d71c16dca4783e9de9f7ebc95131a5350cdfac8ae725ea324ed395542b7bacdd` and independently redownloaded from GitHub artifact `15034992001`. This recovers aggregate coverage and restores the evidentiary support for the affected byte claims. Historical R1 had GitHub-hosted aggregate outcomes and still lacks attributed named execution receipts. The prior worker failure is preserved. [R1 successful aggregate run](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45639342986) · [Historical failed run](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45513525415)

Committed Git blob verification validated all 24 Q1 payload entries, all 12 R2 producer entries, all seven erratum entries and all five clean-checkout replay entries. Exact membership, byte count, SHA-256, path, commit/tree binding, separation of seal and payload and post-seal preservation were verified. The new qualification evidence has its own self-excluding seal tied to exact reviewed revisions and to its payload commit. The original Q1 package and protected producer packages retain their historical locations and byte provenance.

## Gate decision and owner handoff

| Gate | Independent decision |
| --- | --- |
| G-1 Scope | **Pass.** Five test files and two expressly authorized capture files. Production, pin, release and inherited scope preserved. |
| G-2 Execution and obligations | **Pass.** All 16 mandatory named cases and 27 prepared repetitions supported by local and required native hosted evidence. D4 and S-02 are reconciled in the gate matrix. F-S63Q-07 remains an open plan clarification. |
| G-3 Regression | **Pass.** Clean project worker construction, format, Release build, full suite and per-project gates verified, with no failures or skips in the full suites and required negatives failing as designed. |
| G-4 Evidence | **Pass.** Independent seal checked against committed objects, as were the three producer packages and protected Q1 baseline. The prior R1 integrity defect remains visible, with missing expected bytes recovered by a separate erratum. |
| G-5 Independent qualification | **Granted · QUALIFIED WITH QUALIFICATIONS.** Independent commission, exact detached replays and wider contract enumeration establish the binding criteria. F-S63Q-07 remains open and no numerical current-target work guarantee is granted by this report. |

The owner should resolve F-S63Q-07 before considering G-6 by recording whether the plan number is a stale-refusal diagnostic or an intended current-target performance acceptance criterion. The [owner decision record](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/owner-decision-p03.md) sets out the exact discrepancy, possible readings and consequences. This commission leaves the earlier platform, cancellation, durability and transaction-window qualifications in force. No S6.3 formal closure, package release or subsequent work entry is authorized by this decision.

- [Findings register](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/findings.md)
- [Entry, D4, S-02 and inherited qualification matrix](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/gate-matrix.md)
- [Evidence and execution inventory](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/README.md)
