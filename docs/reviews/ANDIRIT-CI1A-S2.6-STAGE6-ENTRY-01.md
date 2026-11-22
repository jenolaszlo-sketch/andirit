# ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01

**Date**: 2026-11-22
**Gate**: Stage 6 entry, bounded to work package **S6.1** — native cancellation, the applicable
D4.5/S-02 native qualification requirements, and implementation preparation.
**Mode**: investigation, technical probes, contract reconciliation and implementation planning only.
No production implementation, no dependency change, no publication, no tag change, no push.

**Formal disposition**: **READY WITH QUALIFICATIONS**

> The S6.1 acceptance boundary — the dedicated native cancel-path test (M9.5), the native
> cancelled-staging case of D4 invariant 5 (M12.2), and the cancellation part of the S-02 native
> matrix — is implementable and qualifies on the pinned baseline with no dependency change and no
> production behaviour change. One capability is **not** implementable and is therefore recorded as a
> bounded limitation rather than simulated: **active native execution cannot be interrupted**
> (LatticeDbSharp 0.2.0 exposes no cancellation or interruption entry point at the managed or native
> ABI level). The qualification therefore proves a *bounded* cancellation contract — cancellation
> before native invocation and cancellation of a managed wait — plus atomicity and truthfulness for
> the non-cancellable native window. No evidence in this commission represents a managed timeout or
> an abandoned task as native cancellation.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01` |
| Authorized scope | S6.1 only: native cancellation; applicable D4.5/S-02 native qualification requirements; technical feasibility assessment; implementation preparation |
| Baseline | current published repository lineage (`b5a81f81fade9807f706b4586329efe416cf1558`) |
| Qualified implementation | `eafd632b19703b3f93dfdf5a6eff5ae34d06a0a6` (strict ancestor of the baseline) |
| Implementation authorization | **not granted** — production implementation requires a separate bounded authorization |
| Push authorization | **not granted** — documents and evidence are committed locally on `main` only |

Deliverables produced by this commission:

| Deliverable | Document |
| --- | --- |
| Commission record (this document) | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01.md` |
| Stage 6 obligation inventory | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-obligation-inventory.md` |
| S6.1 traceability matrix | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-traceability-matrix.md` |
| Native cancellation feasibility assessment | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md` |
| S6.1 implementation proposal | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md` |
| Independent qualification plan | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-qualification-plan.md` |
| Proposed successor commission | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-successor-commission.md` |
| Sealed evidence package | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/` |

## 2. Repository preflight (all conditions met; no stop condition)

Verified against primary sources; re-derivable from Git objects by
`evidence/probe/preflight-receipt.py` (`evidence/preflight.txt`).

| Check | Result |
| --- | --- |
| Local `main` | `b5a81f81fade9807f706b4586329efe416cf1558` (tree `e3c28384abe1870eb5b44dbbc693b69f371542af`) |
| Remote `main` | identical (`b5a81f8…`) — local and remote in agreement |
| Working tree | clean at preflight (verified before any commission artifact was written) |
| CI run `44993021436` | `completed` / `success`, `headSha = b5a81f8…` |
| Qualified implementation | `eafd632…` is a strict ancestor (16 commits behind the baseline) |
| `src/` since `eafd632` | **unchanged** |
| `tests/` since `eafd632` | only `tests/Andirit.TreeSitter.Tests/TreeSitterEcmaExportShapeTests.cs` — the owner-authorized version-folded golden re-baselining recorded in the release-preparation record |
| `.github/` since `eafd632` | only `.github/workflows/publish.yml` — the authorized publication-workflow remediation |
| Published tag | `v0.2.0-preview.7` object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e` → `6ed05964d2fcc758648b654a01ec706647e52183`; remote ref matches the local object |
| Release version | `Directory.Build.props` = `0.2.0-preview.7` (matches the published tag) |
| Sealed evidence packages | **9** prior packages, **113** entries, **0** mismatches at the baseline (manifest-exact) |
| Stage-2 acceptance decisions and qualifications | intact and unmodified |

## 3. Stage 6 contract reconciliation (summary)

The S6.1 boundary was derived only from existing contracts; no requirement was invented and no
guarantee was reinterpreted. The byte-exact source excerpts are in `evidence/contract-reconciliation.txt`.

| Source | What it fixes for S6.1 |
| --- | --- |
| `ADR-CI1-01` owner decision §D.4 | D4 invariant 5: *"Cancelled staging must not expose partial facts or index entries."* D4 qualification: CI-1A must reproduce the applicable cancellation tests *"using actual production storage and APIs"*. The requirement is **non-exposure**, not interruption. |
| S-01 obligation reconciliation §S-02 | The Stage-6 native matrix must exercise all ten D4 invariants against the actual production `LatticeCodeGraphStore`, including a cancellation path and an unknown-outcome case; Stage 2 inherits but does not close it. |
| CI1A specification | S-02: *"All ten invariants must be exercised against actual production `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a native concurrency/republication scenario."* |
| CI1A acceptance matrix | Row 5: *"cancelled staging exposes nothing / accepted state intact / cancel path (`complete:false`) / memory + native"*. |
| S2.5 qualification matrix | **M9.5** — Stage-1 cancellation behavior preserved; *"Dedicated native cancel-path test"*; evidence class `E / no`; owner Stage 6. **M12.2** — D4 invariant 5 *"Explicitly deferred to Stage 6"*; *"Cancel path test native"*. **M12.1 / M12.3** — the remaining full-native-matrix rows, belonging to later work packages. |
| CI1A obligation register | **I-04** — cancellation remains unqualified, owner Stage 6. **Q2-F09** — rebuild blocks queries / no cancellation; addressed by D4. **Q2-F12** — crash/power-loss/multi-process/cross-platform not demonstrated. |
| Residual-risk register / Stage 6 entry proposal | **DF-1…DF-8** deferred obligations; every DF item except DF-7 is *"qualification of existing behavior"*; proposed order S6.1 cancellation → S6.2 durability → S6.3 full native matrices → S6.4 real-fixture D3 → S6.5 platform certification. |
| S2.5 entry assessment §5 | Native-versus-compatibility rule: `ubuntu-latest` is genuine Linux evidence; WSL2/Docker-on-Windows are not; no Windows result counts as Linux evidence; full Windows/Linux/macOS certification remains Stage 6 (DF-6). |

**Derived S6.1 acceptance boundary** (exactly): a *dedicated native cancel-path test* against the
production `LatticeCodeGraphStore` that (a) demonstrates that a cancelled staging exposes no partial
facts and leaves the accepted publication intact, (b) demonstrates the truthful outcome contract of
the cancellation path, and (c) records the cancellation policy in the public contract — **not** a
capability to interrupt active native execution, which no governing contract requires and the pinned
dependency cannot provide.

## 4. Native cancellation feasibility (summary)

Full assessment: `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-feasibility.md`.

**Managed/native boundary.** Every mutation is a single `LatticeDatabase.BeginWriteTransaction()`
followed by LatticeDB node/edge writes and one `txn.Commit()`
(`src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`, `Persist`, lines 883–982). The managed layer
holds a single-writer `SemaphoreSlim` write gate across the whole mutation, and the projection is
advanced only after the native write returns.

**Cancellation mechanisms that exist today** (three distinct classes, all empirically verified):

| Class | Mechanism | Verified outcome |
| --- | --- | --- |
| 1. Before native invocation | `cancellationToken.ThrowIfCancellationRequested()` immediately before `BeginWriteTransaction` (line 888) and the managed write-gate wait (line 339) | Cancellation observed; **no native transaction starts**; the durable state is byte-for-byte unchanged (`pre-invocation` probe: `HetuStage` rows = 0, `HetuUnit` rows = 0, no publication, reopen healthy) |
| 2. Cancellation of a managed wait | `await _writeGate.WaitAsync(cancellationToken)` (line 339) | Cancellation observed (`TaskCanceledException`); the in-flight native write is unaffected and completes; only the non-cancelled write is durable (`managed-wait` probe: `HetuStage` rows = 1) |
| 3. Cancellation of active native execution | **none exists** | A cancellation requested inside the open native write transaction is **not observed**: the operation returns success and the native transaction commits (`native-window` probe: `HetuStage` rows = 1, no publication, reopen healthy). The same holds after the irreversible commit (`post-commit` probe) |

**Dependency capability.** `LatticeDbSharp 0.2.0` (assembly `0.2.0+40bc81ce…`, SHA-256
`d803ca19fad1c0628558b0c8f6b5387471ef72d69db67a543e5b3e7c4555cf46`) exposes **23 public types /
270 public members**, of which **0** have a cancellation-like name and **0** carry a
`CancellationToken`; `LatticeTransaction` offers only `Commit`, `Rollback`, `Dispose`,
`IsCompleted`. The native ABI scan of all three shipped native assets found **0**
`lattice_*`-namespace cancellation entry points (PE `win-x64`: 105 exports; ELF `linux-x64`: 191
dynamic symbols; Mach-O `osx-arm64`: identifier scan), and the declared capability set contains
**0** cancellation-like features among 30. The pinned upstream is `latticedb v0.15.0`
(`9800159e22e200f2b6888c7c6be1810adb695506`), whose C API exposes only
`lattice_begin / lattice_commit / lattice_rollback / lattice_close`.

**Implication.** Native cancellation of active native work is **unavailable** under the pinned
dependency. Any requirement to interrupt it would need an upstream LatticeDbSharp/LatticeDB change
(a new transaction-interrupt entry point) and a separately authorized, qualified dependency upgrade.
Neither is authorized by this commission, and no such capability is simulated.

**Truthfulness.** The store already owns the correct pattern for an indeterminate native outcome:
a post-commit fault produces `CodeGraphCommitOutcomeUnknownException`, the instance is quarantined
and refuses current-state claims, and the durable state is left untouched for corroboration by
reopening (`commit-unknown` probe: outcome reported as unknown, instance quarantined, and the write
had in fact landed — `HetuStage` rows = 1). The S6.1 contract reuses this vocabulary rather than
inventing a cancellation-shaped answer for an unobservable native outcome.

**Transaction, durability, lifetime, concurrency.** One native write transaction per command
(atomic all-or-nothing commit); the command log advances only after the native write returns, so a
failed or cancelled pre-native mutation leaves the previous publication and index state intact;
`Dispose()` takes the same write gate and therefore **waits** for an in-flight native write instead
of aborting it (`dispose-race` probe: disposal blocked until the native write finished, durable
state intact, reopen healthy); after disposal no native handle is retained (the database file is
deletable on Windows — `cleanup` probe).

**Platform differences.** The pinned dependency declares `linux-x64` and `win-x64` as supported
RIDs and `linux-arm64`, `osx-x64`, `osx-arm64` as candidate RIDs; the shipped `osx-arm64` binary
carries no supported-capability declaration. The cancellation surface is identical across all three
native assets (no interruption entry point anywhere), so the *contract* is platform-invariant while
the *runtime evidence* is available only on `win-x64` and `linux-x64`.

## 5. S6.1 implementation proposal (summary)

Full proposal: `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-implementation-proposal.md`.

The smallest technically sound S6.1 change set is **documentation + the dedicated native cancel-path
test suite** — no behavioural change, no dependency change, no new public API:

| # | Change | File(s) | Nature |
| --- | --- | --- | --- |
| 1 | State the cancellation contract precisely on the production store: cancellation is honoured (a) while waiting for the write gate and (b) before the native write transaction begins; it is **not** honoured inside the native window; the operation must be awaited to learn its outcome; the only truthful outcomes are *cancelled before native invocation*, *completed*, or *commit outcome unknown*. | `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs` (XML documentation), `src/Andirit/ICodeGraphStore.cs` (interface documentation) | documentation only |
| 2 | Add the dedicated native cancel-path test suite (M9.5 + D4 invariant 5 / M12.2), built on the existing internal fault-injector seam (`InternalsVisibleTo("Andirit.LatticeDb.Tests")`) and on concurrent cancellation during a large native batch write. | new `tests/Andirit.LatticeDb.Tests/NativeCancellationPathTests.cs` | test addition (the qualification instrument) |
| 3 | Record the cancellation policy and the non-cancellable native window in the contract record; name the upstream dependency change that would be required if interruption is ever demanded (a separate owner commission). | `docs/decisions/…` (successor commission record) | documentation |
| — | **Not** included: dependency upgrade, new native seam in production code, new public API surface, changes to commit/atomicity semantics, changes to the write gate, or any change to the published packages. | — | — |

The existing production seam is sufficient: `Persist` invokes the internal fault injector at
`"before-commit"` (after every native write, inside the open transaction) and `"after-commit"`, so
a test can place a cancellation deterministically at the commit boundary without any production
change, and the genuinely-arbitrary case is covered by cancelling from a concurrent thread while a
large native batch write is in flight.

## 6. Qualification design (summary)

Full plan: `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-qualification-plan.md` — ten deterministic cases
(cancellation before invocation; during active native work; before the commit boundary; racing with
successful completion; after an irreversible commit; reopen and durable-state consistency;
concurrent cancellation and disposal; resource cleanup; successful non-cancelled control;
preservation of the historical Stage-2 regressions), each with an expected outcome, an observation
mechanism, a platform requirement, an evidence format and an acceptance criterion.

Constructed controls (fault-injector-placed cancellation) are recorded separately from genuinely
exercised native behaviour (cancellation arriving at an arbitrary moment during an in-flight native
batch write). Genuine hosted Linux execution is required for the Linux row; Windows evidence does
not substitute for it; the osx-arm64 row remains unavailable and is preserved as a limitation.

## 7. Formal disposition and qualifications

**`READY WITH QUALIFICATIONS`** — one of the four dispositions authorized by the commission.
S6.1 is not declared implemented or qualified by this entry commission.

| ID | Qualification |
| --- | --- |
| **Q-S61-1** | **Active native execution is not interruptible.** `LatticeDbSharp 0.2.0` exposes no cancellation/interruption API at the managed or native ABI level (0 cancellation-like public members, 0 `CancellationToken` members, 0 `lattice_*` cancellation exports, 0 cancellation-like capability features). A cancellation requested inside the open native write transaction is not observed; the transaction commits atomically. The S6.1 cancellation guarantee is bounded to the pre-native window and to managed waits. |
| **Q-S61-2** | **The native window is non-cancellable and must be awaited.** A managed timeout, an abandoned task, or a `Task.WhenAny` race must never be represented as a native cancellation. The truthful outcomes are exactly three: *cancelled before native invocation*, *completed*, or *commit outcome unknown* (existing quarantine path). Callers must await completion to learn the outcome. |
| **Q-S61-3** | **D4 invariant 5 is discharged by atomicity, not by interruption.** A cancellation observed before the native transaction leaves the durable state unchanged (no staged rows, no published units, prior publication intact); a cancellation arriving during the native window yields a completed, fully committed operation — never a partially applied one. The qualified claim is "cancelled staging exposes nothing", not "staging can be interrupted". |
| **Q-S61-4** | **osx-arm64 remains unqualified.** The pinned native dependency declares `linux-x64` and `win-x64` as supported RIDs; `osx-arm64` is a candidate RID whose shipped binary carries no supported-capability declaration. Runtime qualification on osx-arm64 remains unavailable; content evidence only. |
| **Q-S61-5** | **Linux evidence must be genuine hosted Linux.** `ubuntu-latest` is the evidence class; WSL2/Docker-on-Windows are not native Linux evidence; no Windows result counts as Linux evidence. |
| **Q-S61-6** | **The cancellation surface is platform-invariant but the runtime evidence is not.** All three native assets lack an interruption entry point, so the contract does not vary by platform; the native cancel-path evidence exists only for `win-x64` and `linux-x64` unless a macOS-arm64 native store becomes available. |
| **Q-S61-7** | **Scope remains qualification of existing behavior.** No new native capability, no dependency upgrade, and no interruption semantics are introduced; the only upstream change that could create interruption is a separately authorized, qualified LatticeDbSharp/LatticeDB commission. |
| **Q-S61-8** | **The F-02 durability boundary is untouched.** Managed fault injection is not crash, power-loss, abrupt-kill or multi-process durability (DF-4 remains open). The cancel-path evidence does not narrow that boundary. |

Carried unchanged and still binding: **Q-L1…Q-L8**, **MR-1…MR-4**, **GH-1…GH-4**, **DF-1…DF-8**
(including DF-4, DF-6 and the osx-arm64 runtime limitation), and every accepted Stage-2 limitation.
No Stage-2 acceptance decision was reopened.

## 8. Evidence package and integrity

Package: `docs/reviews/ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-evidence/` — self-excluding SHA-256
manifest (`manifest.json`), `.gitattributes` (`* -text` / `**/* -text`), `.gitignore`
(`!*` / `!**/`), the reproducible probe harness, and the raw receipts.

| Artifact | Content |
| --- | --- |
| `probe/Program.cs`, `probe/Stage6Probe.csproj`, `probe/Directory.Build.props` | the reproducible probe harness (10 scenarios, 41 checks; each scenario in its own process) |
| `probe/run-probes.ps1` | the driver that runs every scenario and writes the receipts |
| `probe/native-abi-scan.py` | the read-only native export/capability scanner |
| `probe/preflight-receipt.py`, `probe/contract-receipt.py` | Git-object-derived preflight and contract-excerpt generators |
| `probe-summary.txt`, `environment.txt` | scenario exit codes, check counts and the execution environment |
| `api-surface.log`, `pre-invocation.log`, `managed-wait.log`, `native-window.log`, `post-commit.log`, `commit-unknown.log`, `dispose-race.log`, `cleanup.log`, `control.log`, `reopen.log` | raw scenario receipts (all exit 0) |
| `native-abi-scan.txt` | the native ABI and capability scan |
| `preflight.txt` | the preflight receipt |
| `contract-reconciliation.txt` | the byte-exact governing-contract excerpts backing every citation in the traceability matrix |
| `probe-reproduction.md` | how to reproduce the receipts |

Integrity: the manifest lists every file with its byte length and SHA-256 and excludes itself; the
package is bound to the repository commit that carries it.

## 9. Repository state and protected scope

- Baseline before this commission: `b5a81f81fade9807f706b4586329efe416cf1558`; the commission
  adds **documentation and evidence only** and is committed locally on `main`.
- **No push** was performed or authorized.
- No production code, test code, dependency, or workflow was modified. The probe harness is a
  standalone, non-packed project under the evidence directory, excluded from `Andirit.slnx`; it
  references the production projects and never modifies them.
- No package was published; no release tag was deleted, moved or recreated; the version was not
  incremented; no force-push or history rewrite; no historical Hetu package was deleted, unlisted
  or deprecated; no qualification branch was deleted; no Stage 6 implementation (S6.2–S6.5) was
  begun.

## 10. Successor commission

**`ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01`** is proposed as the next implementation commission
(`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-successor-commission.md`). It is owner-gated and bounded to:
publish the cancellation contract, add the dedicated native cancel-path tests, execute them on
`win-x64` and on genuine hosted `linux-x64`, seal the native cancel-path evidence, and submit the
result for independent review and owner acceptance. It does **not** include dependency upgrades,
native interruption, S6.2–S6.5, publication or tag changes.
