# L1–L9 execution receipts — UBUNTU-QUALIFICATION-01 G2 (run 44746950426)

Receipts are derived from the preserved raw log (`job-ubuntu-latest.log`, SHA-256
`922f058a…`, 98,464 bytes), the pack job log (`job-pack.log`, `4c157ea4…`), the downloaded
coverage artifacts (`artifacts/coverage-results-{ubuntu,windows,macos}-latest/`), and the
run/job API records captured in `publication-record.json` / `results.json`.

| Item | Receipt | Status |
| --- | --- | --- |
| **L1 — Native environment** | Ubuntu 26.04.1 LTS x64; hosted image `ubuntu-26.04` / `20261111.196.1` (test job), provisioner `20261103.629`; runner 2.338.2; region northcentralus (Azure); .NET SDK **10.0.404** / runtime **10.0.15** (LTS) via `setup-dotnet@v5` (`10.0.x`). Genuine hosted Ubuntu (fixture L). Image generation differs from SQ-05 (24.04.5 / SDK 10.0.403) because `ubuntu-latest` rolls forward — recorded as the demonstrated environment, not a substitution | SUPPORTED |
| **L2 — Expanded test suite** | `Andirit.LatticeDb.Tests`: **Passed 75 / Failed 0 / Skipped 0**, `-c Release`, **no test filter**, full current suite (log line 293; matches the 75-case census in the preparation package). Zero skips recorded | SUPPORTED |
| **L3 — Persistence / durable reopen** | The suite's durable-reopen/reconstruction cases (incl. the 13 durable-state regressions added at `eafd632`: legacy restore projection, coverage-state correction, complete-empty survival) pass on Linux | SUPPORTED |
| **L4 — Schema / corruption refusal** | The suite's schema/namespace/page-binding/stale-publication refusal cases (incl. `S24SurfaceRefusalMapping`, `S23Wp5NamespaceRefusal`, page/publication binding, accounting-mismatch and orphan refusals) pass on Linux | SUPPORTED |
| **L5 — Publication authority** | The suite's authority-envelope cases (missing/stale/mismatched `CurrentAuthority`, publication accounting, orphan-owner correspondence) pass on Linux | SUPPORTED |
| **L6 — Recovery / lifecycle** | NEW-01 managed-recovery and lifecycle cases pass on Linux; **managed fault injection does not prove crash/power-loss durability — F-02 preserved unchanged** | SUPPORTED WITH QUALIFICATION |
| **L7 — Platform behavior** | Suite-level Linux pass at the same counters as Windows (75/0/0; coverage 368/466 and 900/969 identical) for the first time with a green repository conclusion. Internal locking/WAL/mmap mechanics are **not directly instrumented** — observation limit carried from the SQ-05 record | SUPPORTED WITH QUALIFICATION |
| **L8 — Evidence** | Raw run log, both job logs, all four suites' coverage artifacts per OS, run id/URL, `head_sha`, merge-ref tree, artifact IDs/digests, and exit codes preserved under a self-excluding SHA-256 manifest (`manifest.json`); no reconstruction | SUPPORTED |
| **L9 — Boundary** | Scope confined to the authorized operation (one branch push, one PR, the unmodified workflow, evidence retrieval). No production/test/workflow change; the SQ-05 G2/G3 packages and PR #6 remain untouched; prior evidence and decisions preserved | SUPPORTED |

## Supplementary results (same run, recorded as supplementary evidence only)

core **345/0/0** (line 82.42% / branch 70.74%, threshold 55), CSharp **372/0/0** (95.11% / 87.85%,
threshold 75), TreeSitter **157/0/0** (89.89% / 75.95%, threshold 60); `pack` job success on hosted
Ubuntu (pack + smoke-test steps). The CSharp count (372) reflects the membership-remediation
additions since the SQ-05 revision (353).

## Native execution evidence

`LatticeDbSharp 0.2.0` is pinned at the executed tree (`src/Andirit.LatticeDb.csproj`, blob
verified at `823e3c65`); the restore of `src/Andirit.LatticeDb` succeeded on the runner; and all
75 cases — which open, publish, and reopen native LatticeDb stores — passed on Ubuntu, with the
coverage artifact instrumenting `Andirit.LatticeDb` production code (368/466 branches). As with
the SQ-05 qualification, the passing store-opening/publication/reopen behavior is the operative
proof that the linux-x64 `liblattice.so` native dependency resolved and executed.