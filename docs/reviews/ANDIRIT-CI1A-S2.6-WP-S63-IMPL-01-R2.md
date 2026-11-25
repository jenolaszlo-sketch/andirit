# ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2

**Targeted producer remediation · 25 November 2026**

**A — IMPLEMENTED AND READY FOR INDEPENDENT QUALIFICATION**

**G-5 withheld. G-6 owner acceptance has not been granted. S6.3 remains open.**

This record responds to all eight findings in independent Q1. It distinguishes the producer's verified remediation from future independent requalification. The findings matrix records each finding's severity, frozen reproduction, root cause, implementation, regression controls, local and hosted receipts, and remaining limitation.

## Subject and publication

| Subject | Commit | Git tree |
| --- | --- | --- |
| Frozen R1 implementation | `d3c9359d1abd8c89a7214e37f074802e5d2fa238` | `6eb68de12467e95efa51c8b1a71b1c17713d9492` |
| Published R1 producer evidence and R2 baseline | `dd484af2d32ff1f5b7951a85f4f3104c4b4b5402` | `505511dc6404e3d38fb9f80457f909b6e2aac5ba` |
| Independent Q1 | `a69c13ce447839330793be2e4cbc7a1dd732d1dd` | `a57df9a83cb692acef2c449b0c7eba657887f6d0` |
| R2 implementation and exact local/hosted tested revision | `a96c19a844f8ea1719508198b47df23c4e203571` | `f05ded592bc82c4e0b02044c97fb1f509fdb6e8e` |
| R2 evidence-only payload | `f01465cf39677536794cc30db2b2875c76912a97` | `f4c3b6e249e90e2d65bc18212345cec7740695f4` |
| R2 evidence-only seal | `ab2bd168ab2436ffb2a07de8466b33cecd79720b` | `b65c73eb5e8ceb3cc31c83028ccc7a03c4c1baee` |

R2 was produced from a separate clean clone based on the verified published baseline. The implementation fast-forwarded directly from `dd484af` to `a96c19a`. Its seven paths are five C# test or test-worker files, the CI workflow, and its TRX verifier. The production source delta is empty and LatticeDbSharp remains pinned to 0.2.0. The existing coverage thresholds, Windows/Ubuntu/macOS matrix, package build and smoke validation remain in place. The tag object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` for `v0.2.0-preview.7` still points to `6ed05964d2fcc758648b654a01ec706647e52183`. No package or release publication occurred.

Q1 remains on its original branch and checkout. Its 24 committed evidence blobs verify under manifest package ID `09cedb476ef53d64672151204e4f4b04bb00e4ed53fb2d055e95c046adb50e2d`. The original producer 17-entry package and R1 extension retain their committed byte provenance. Neither Q1 nor historical evidence is part of the R2 implementation delta. The authoritative recovery ref points to `d9461c06b9ba7a42b20a3ce100d28f39d1935a40`. Every R2 commit uses the repository's configured author and committer `jenol <jenolaszlo-sketch@users.noreply.github.com>` with shared hooks active. The repository publication receipt records both pre-push and post-push implementation identities.

## Retry, recovery, concurrency and cancellation

- The exact Q1 reproducer accepted 5/5 late opens at 118–119 ms using a 25 ms deadline on the frozen helper. R2 rejected all five, with a timeout receipt at 25 ms in each trial. The original Q1 executable's late-success assertion therefore fails as expected on R2. Its other five probes pass. Persistent native -16 retained its diagnostic. The control suite separately verified immediate success, timely retry, persistent failure, propagation of a different native error and a managed exception, late and exact-boundary rejection, clamped delay and disposal.
- D-07 observes a durable accepted occurrence publication before the worker starts, then verifies live lock ownership, the exact READY worker and genuine process termination. After recovery it checks the exact staged worker row, accepted unit bytes, publication and occurrence state, and non-publication of the held run. A separate worker republishes the same accepted repository and the old handle is explicitly stale. Five synthetic controls show the oracle rejects missing or wrong stage, missing publication or fact, and leaked abrupt-run publication. The external worker controls exercised the real D-07 entry point. The first removed the copied worker, and the second exited 19 before READY after forwarding the real baseline setup.
- P-12 observes the old exact fact tuple after staging, queues a reader at the native before-commit checkpoint, and observes the new exact ordered tuples and identities after release. Four deliberately faulty observations are rejected. P-15 separately observes an exclusive cross-process lock and same-repository republication after reopen. That native result is the cross-process part of S-02. P-12's in-process synchronization is diagnostic.
- D-05 retains separate holder and completing publisher processes. Three fresh processes corroborate the same accepted generation, including a pre-cancelled stage request on the completed run. The parent compares exact publication, occurrence and native stage/unit state. Added controls reject a leaked stage and a lost publication. The predecessor's skipped D-05 source did call completion; its after-completion staged-row expectation was the mismatch.
- Prepared A–I stayed unchanged and passed 27/27. Probe I opened on the first attempt in all three trials. P-04/P-08 and the exact Q1 C/F replay provide the stronger failure, durable reopen and projection-rebuild observations. The timed hold replay uses real native lock errors with controlled release. It does not establish a natural post-kill race. The deadline controls govern retry start and result acceptance around a synchronous native open. They do not interrupt a running native call.

## Local and hosted verification

The Windows clean detached checkout ran the LatticeDb test project before any solution build. DLL, runtime configuration and dependency file hashes matched between original and copied worker outputs. D-05, D-07 and P-15 passed against the copied output while the original output was parked. Formatting, clean Release build, solution and all four CI-equivalent coverage commands passed. The 1,005 solution tests break down as 345 core, 372 CSharp, 157 TreeSitter and 131 LatticeDb. The increase from 989 to 1,005 is nine deadline tests, five D-07 recovery-oracle data cases, one early-exit regression and one D-05 cancellation-oracle regression. All 16 named publication cases passed. All 20 dedicated D-07 repetitions passed and both expected-failure controls failed. There were no failed or skipped tests in the positive gates.

GitHub run 45691273312, attempt 1, completed all four jobs at `a96c19a844f8ea1719508198b47df23c4e203571`. Each Ubuntu x64, Windows x64 and macOS arm64 leg passed all 1,005 tests with zero failed or skipped and passed all coverage gates. The pack and native smoke job also passed. The downloaded Ubuntu TRX verifies all 16 mapped P-cases and the supplemental D-05, D-07 and early-exit cases, including the required process and checkpoint outputs. Ubuntu used runner GitHub Actions 1000004851 (job 139477057628) and reported SDK 11.0.100. The selected Windows and macOS SDKs were 11.0.100 and 11.0.100. The unchanged setup step requests .NET 10. The receipts record the SDK the `dotnet` commands actually selected. [Hosted run](https://github.com/jenolaszlo-sketch/andirit/actions/runs/45691273312)

| Runtime evidence | Core lines · branches | CSharp lines · branches | TreeSitter lines · branches | LatticeDb lines · branches |
| --- | --- | --- | --- | --- |
| Windows local | 5029/6101 · 2105/2969 | 2199/2311 · 1181/1342 | 2240/2493 · 1442/1911 | 900/969 · 368/466 |
| Ubuntu hosted | 5029/6101 · 2082/2943 | 2198/2311 · 1172/1334 | 2241/2493 · 1406/1851 | 900/969 · 368/466 |
| Windows hosted | 5029/6101 · 2082/2943 | 2199/2311 · 1173/1334 | 2240/2493 · 1404/1851 | 900/969 · 368/466 |
| macOS hosted | 5029/6101 · 2082/2943 | 2198/2311 · 1172/1334 | 2242/2493 · 1408/1851 | 900/969 · 368/466 |

The unchanged 55/75/60/70 line and branch gates passed on every environment. LatticeDb coverage was 92.87% lines and 78.96% branches.

The local verifier accepted the bound actual receipts and rejected each of eight controls: a missing case, a skipped case, a failed case, a missing worker observation, invalid XML, a missing TRX, a broken content hash and the wrong revision. The hosted verification independently ingested the downloaded artifact files and checked the exact commit and run metadata against GitHub. Source-level evidence summaries preserve the run, job and worker IDs, the runner platform and SDK, the actual exact names and durations, and the coverage numerators and denominators.

## Evidence and inherited obligations

The R1 erratum reconstructs the two CRLF byte sequences exactly from the historical LF Git blobs and recovers the missing Cobertura XML from the verified Q1 archive, bound to the original hosted run. Its three replacement payloads match the original expected hashes. The historical R1 manifest and original faulty blobs remain preserved. The erratum and R2 producer evidence each use a separate self-excluding manifest derived from the committed payload Git tree, then verified from the sealing commit.

| Package | Payload tree | Manifest Git blob | Manifest package SHA-256 | Verification |
| --- | --- | --- | --- | --- |
| R2 producer evidence | `3f8022b74b1429d8bb0f4453aef6ea02ab4f5dc2` | `2c72f72e52fad436e609a146c00b754b3bae1e55` | `c81430e4b135f7c4b23bcc686eecd16517bb45530ed1a1692b000850b00c60ea` | 12 paths, zero missing or mismatched |
| R1 extension erratum | `d9a97aff5c8676983e16b4df042b4d0be27c07f5` | `4dedc37e5d9928fae07c784e80f5e60f22ff9afb` | `4052bf04b4878ea349a268cbf9dd13c3316102d5bbabffa63a190ddbb9c5f933` | 7 paths, zero missing or mismatched |

Earlier S6.1, S6.2 and Stage 2 accepted limitations retain their Q1 reconciliation, including the native cancellation await obligation, unknown-outcome quarantine, scope and platform qualification boundaries, and the inherited prohibition on interpreting managed injection as crash or power-loss evidence. The macOS hosted job remains supporting evidence within the existing candidate-RID boundary. R2 carries the P-03 Limit + 2 plan wording discrepancy to the owner. The governing D4-3 stale fallback prohibition is exercised directly, and the current-target numeric bound remains open for clarification.

The next commission is the separate independent `ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01-R1`. This producer record leaves the independent Q1 disposition intact.

[Findings matrix](ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2-evidence/findings-matrix.md) · [Execution summary](ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2-evidence/results-summary.txt) · [R2 evidence manifest](ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2-evidence/manifest.json) · [R1 evidence erratum](ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01-R2-R1-ERRATUM/README.md)
