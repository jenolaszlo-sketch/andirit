# ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01

## Owner acceptance and closure of S6.3

**26 November 2026 · S6.3 — ACCEPTED WITH QUALIFICATIONS · G-6 satisfied · formally closed**

The owner accepts the native publication-correctness qualification for the exact R2 implementation below. The independent disposition remains **QUALIFIED WITH QUALIFICATIONS**. This closes S6.3 within its accepted scope and leaves the other Stage 2.6 work packages subject to their own gates.

| Subject | Commit | Root tree |
| --- | --- | --- |
| Qualified R2 implementation | `a96c19a844f8ea1719508198b47df23c4e203571` | `f05ded592bc82c4e0b02044c97fb1f509fdb6e8e` |
| Producer evidence and erratum seal | `ab2bd168ab2436ffb2a07de8466b33cecd79720b` | `b65c73eb5e8ceb3cc31c83028ccc7a03c4c1baee` |
| Pre-acceptance main and clean-checkout replay seal | `f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60` | `77274fed2469e30aeef705f487442c54f0e5133d` |
| Independent report and evidence | `0f520cf61adaf1a7336fd4a360b20f6d270029be` | `85efdfd27162257f33fe8f976d5ad6ba6d9d52d2` |
| Independent requalification seal | `329e8f775ab9fba10f61510c7f80a312525d13ff` | `96e75e26dd910e18f39e92803cac3418ec5fd1ee` |

LatticeDbSharp is pinned at **0.2.0**. The release boundary remains `v0.2.0-preview.7`, tag object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e`, peeled commit `6ed05964d2fcc758648b654a01ec706647e52183`. [Independent qualification](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1.md) · [R2 hosted qualification](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45691273312)

## Gate and entry reconciliation

| Gate | Owner assessment |
| --- | --- |
| G-1 Implementation scope | **Satisfied.** R2's seven approved test and evidence-capture paths preserve production source, dependency pins, thresholds and release automation. The acceptance integration changes documentation and evidence only. |
| G-2 Native qualification | **Satisfied.** The exact R2 Windows and genuine hosted Ubuntu x64 evidence attributes all 16 named cases, with all 27 prepared repetitions independently verified. Production-backed D4 and S-02 obligations are supported in the stated scope. |
| G-3 Regression | **Satisfied.** Formatting, Release build, full suite, worker construction and four unchanged coverage thresholds passed. Local Windows and each hosted platform recorded 1,005 passed, zero failed, zero skipped. |
| G-4 Evidence | **Satisfied.** Committed objects verify the restored 17-entry original producer package, Q1's 24 entries, the three R2 producer packages (12, 7 and 5 entries), and the independent requalification's 19 entries and report. The old R1 extension and its failed integrity result remain visible. Recovered bytes are documented by the separate erratum. Owner acceptance evidence has its own seal. |
| G-5 Independence | **Satisfied.** The separate requalification re-derived the contract, extended the review inventory, replayed the original defect and adversarial cases, and independently fetched and parsed hosted results. Its scope remains fixed to R2. |
| G-6 Owner acceptance | **Satisfied.** All checks above passed. The explicit P-03 decision resolves the sole outstanding owner item, and inherited qualifications still apply. S6.3 is closed with qualifications. |

C-S63-1 and C-S63-2 preserve implementation scope, the pinned native dependency and release boundary. C-S63-3 is satisfied by the committed cancellation follow-up in D-05. C-S63-4 is satisfied by exact-SHA GitHub-hosted Ubuntu x64. C-S63-5 and C-S63-6 preserve the tested durability boundaries and macOS arm64 qualification limits. C-S63-7 distinguishes managed interleaving from genuine native process evidence. C-S63-8 is satisfied by scoped fast-forward publication. C-S63-9 and C-S63-10 preserve sealed lineage and earlier qualifications. [Independent entry and obligation matrix](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/gate-matrix.md)

## Accepted technical scope

| Obligation | Acceptance basis |
| --- | --- |
| D4-1…3 · Accepted generation and explicit refusal | P-01/P-02 exercise supersession and invalid durable authority. P-03 and the independent controls demonstrate stale refusal without fallback. Current lookups remain target-scoped and publication-consistent. |
| D4-4…6 · Failure, cancellation and supersession | P-04 preserves an exact prior publication after failure. S6.1/S6.2 cancellation cases and committed D-05 preserve the accepted facts and native bindings across a cancelled request. P-05/P-06/P-13 demonstrate accepted progression and refusal of superseded completion. |
| D4-7…9 · Reopen, contributor/deletion semantics and query limits | P-07/P-08/P-14 corroborate reopened or rebuilt accepted results, including known failure and unknown-outcome quarantine. P-09/P-10 verify union, conflict refusal and scoped deletion. P-11 verifies ordering and truncation. |
| D4-10 · Native concurrency | P-12 is an in-process observation of exact queued-reader snapshots. P-15 and D-05 use genuine file-ownership contention and distinct processes, including same-repository republication and corroborated durable state. D-07 supports the narrow post-staging termination, recovery and republication scenario. |
| S-02 and associated deferred matrix rows | The combined S6.1–S6.3 evidence covers the complete ten-invariant matrix against production storage, with failed, cancelled, superseded, reopened and genuinely multi-process scenarios. Accept DF-2, DF-3, DF-8 (including M12.1 and M12.3) and their applicable I-05/I-06/I-07 scope within these evidence boundaries. |

## Findings and retained qualifications

| Finding | Owner disposition |
| --- | --- |
| F-S63Q-01 | Independent closure confirmed. Exact-R2 Ubuntu named and process attribution is supported. |
| F-S63Q-02 | Independent closure confirmed by the separately sealed byte recovery. Historical R1 retains aggregate evidence only for its successful hosted run. |
| F-S63Q-03 | Independent closure confirmed. Monotonic acceptance deadline, late-handle disposal and native diagnostic propagation are supported. In-flight synchronous native execution or cleanup may take longer. |
| F-S63Q-04 | Independent closure confirmed. P-12's exact in-process snapshot and P-15's cross-process repository witness cover the original gaps. |
| F-S63Q-05 | Independent closure confirmed. Twenty genuine D-07 repetitions and the worker and recovery negatives support the precise recovery oracle. |
| F-S63Q-06 | Independent closure confirmed. D-05 supplies the inherited committed follow-ups for F-QU-01 and F-QU-02. |
| F-S63Q-07 | **Accepted with erratum.** Stale refusal and result-limit behavior passed. The current query with limit one examined 400 target entries. The owner resolves the plan ambiguity and retains the observed work amplification as an open engineering concern. |
| F-S63Q-08 | Independent closure confirmed. P-08 directly demonstrated projection rebuild, with independent durable corroboration. |

[Independent findings](ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1-evidence/findings.md) · [P-03 owner decision and erratum](ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01-P03.md)

The accepted scope is functional and publication correctness under the pinned dependency. Stale and unverifiable requests explicitly refuse without full-store fallback. Current-index requests satisfy result caps, deterministic ordering and publication consistency. Current-query computational bounds remain unqualified. Target-local work amplification remains open for possible optimization and measurement under separate authorization.

All S6.1 Q-S61-1…8, S6.2 Q-S62-1…9, prior Q-L, MR and GH qualifications, and retained DF dispositions still apply. The earlier owner limits on native interruption, truthful awaited outcomes, commit/quarantine, verification-to-return, declared scope, historical schema and managed diagnostics remain in force. Native correctness is supported on tested Windows x64 and genuine hosted Linux x64. The R2 macOS arm64 job supplies bounded candidate-platform evidence. Full platform certification remains under DF-6. The accepted D-07 scenario supports termination of an idle, staged holder and corroborated reopen. General OS crash, power loss, in-flight native commit interruption and broader isolation/durability guarantees remain outside this acceptance. The open historical lineage gap remains recorded.

## Publication and handoff

The integration base is published `f19587b0d0c19f5877d3ccfaefb4d6a5bc6f3c60`. Both original independent qualification commits are direct ancestors of the acceptance payload and its seal. The acceptance manifest binds the new owner decisions, state updates and prepublication receipts to committed Git bytes. Push and CI receipts bind the resulting documentation/evidence commit to its separate GitHub workflow run. The qualified implementation and its hosted run remain the R2 identities above. [Acceptance evidence](ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01-evidence/README.md) · [CI workflow history](https://github.com/jenolaszlo-sketch/andirit/actions/workflows/ci.yml)

**Next eligible milestone.** A separate owner-authorized **S6.4 entry assessment** for the real-fixture D3 corpus (DF-1) may follow. S6.5 retains broader platform certification. This acceptance does not enter either work package. [Stage 6 sequence](ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md)
