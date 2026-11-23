# ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01

**Date**: 2026-11-22
**Gate**: Stage 6 work package **S6.1** — independent qualification of the native cancellation
contract implementation.
**Mode**: independent qualification only. The reviewer is **not** the implementation producer; no
producer conclusion was accepted without independent re-derivation. No production code, no
committed implementation test, no dependency, no workflow, no threshold and no requirement was
changed; no finding was remediated during this commission; nothing was published; no tag changed;
**no push** was performed.

**Formal disposition**: **S6.1 — QUALIFIED WITH QUALIFICATIONS**

> All five execution gates (G-1…G-5) pass on independently re-derived evidence. No qualification-plan
> stop condition was triggered. S6.1 owner acceptance is **not** granted by this commission, and
> S6.2 entry is **not** authorized. Eight non-blocking findings are recorded (§7); none weakens a
> binding requirement, and none requires remediation before the owner's acceptance decision.

---

## 1. Authorization and scope

| Item | Value |
| --- | --- |
| Commission | `ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01` |
| Reviewer role | independent qualification; session distinct from the producer session |
| Object qualified | the S6.1 implementation of `ANDIRIT-CI1A-S2.6-WP-S61-IMPL-01` at `0162494…`, sealed at `3eb1339…` |
| Qualification plan | `ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01-qualification-plan.md` (Q-01…Q-12, §6 stop conditions) |
| Disposition vocabulary | `S6.1 — QUALIFIED` / `QUALIFIED WITH QUALIFICATIONS` / `NOT QUALIFIED` (successor-commission §6) |
| Owner acceptance | **not granted** here — it is gate G-6, a separate owner record |

Deliverable: this record plus the sealed evidence package
`ANDIRIT-CI1A-S2.6-WP-S61-QUAL-01-evidence/` (self-excluding SHA-256 manifest).

## 2. Entry and provenance — PASS

Independently verified from Git objects (`evidence/entry-provenance.txt`,
generator `evidence/entry-provenance.py`); the script's own entry checks all pass.

| Check | Independent result |
| --- | --- |
| Pinned revisions resolve | entry `2d1e8ec…` (tree `82ce2b87…`), implementation `0162494…` (tree `f8a6876f…`), evidence `3eb1339…` (tree `be0d1273…`) — all commits |
| Ancestry | entry → implementation → evidence; `eafd632…` and remote base `b5a81f8…` are ancestors of the evidence revision; the implementation is **not** an ancestor of the entry baseline |
| Working tree | clean; branch `main`; `HEAD = 3eb1339…` |
| Local-only evidence revision preserved | `origin/main = 0162494…`, local `main = 3eb1339…` → `1 0` ahead; the evidence commit is **not pushed**, as required |
| Published state untouched | tag `v0.2.0-preview.7` object `6659647527…` peels to `6ed05964…`; `Directory.Build.props` version `0.2.0-preview.7` identical at entry and evidence |
| Production change set | `git diff entry..implementation -- src tests .github` = exactly 3 paths: 2 documentation files and the new test file; **0** `.csproj`/`.props`/`.yml` changes |
| Structural check of the two `src/` changes | +21/−0 and +21/−0; **zero** non-comment, non-XML added or removed lines — documentation only |
| Evidence packages | **47** packages with a manifest, **2 971** manifest entries verified against committed Git blobs; **46** packages byte-exact |
| Pre-existing gap | the only mismatch anywhere is the known 4-file gap in `CI1A-S2.5-WP-S25-2-G2-EXEC-evidence`, **identical at the remote base, at the entry baseline and at the evidence revision** — pre-existing, outside the S6.1 lineage, recorded not repaired |
| S6.1 evidence package | 61 entries, manifest complete over the committed tree and self-excluding |

## 3. Contract qualification — PASS

The reviewer re-derived the contract from the production source of the pinned implementation
revision, not from the producer's test code.

### 3.1 The contract's five claims, verified from source

| Contract claim | Independent verification |
| --- | --- |
| Checkpoint 1 — honoured while waiting for the managed write gate | `LatticeCodeGraphStore.MutateAsync` line 360: `await _writeGate.WaitAsync(cancellationToken)`; the same pattern at lines 231, 280, 319, 445. **Verified.** |
| Checkpoint 2 — honoured immediately before the native transaction opens | `LatticeCodeGraphStore.Persist` line 909: `cancellationToken.ThrowIfCancellationRequested()` immediately before `BeginWriteTransaction()` (line 910). **Verified.** |
| Not honoured inside the native transaction | The native window is lines 910–975 (`BeginWriteTransaction` → `Commit`). `PersistOccurrences(txn, run, state)` (line 668) and `NativeGraphMirror.Rebuild(txn, units, existing)` (line 52) take **no** `CancellationToken`; the only `ThrowIfCancellationRequested` calls in `NativeGraphMirror` (line 165) and `LatticeCodeGraphStore.OccurrenceAuthority` (line 70) are in the **read/traverse** path, not the write path. **Verified — the token is not consulted anywhere in the native write window.** |
| Three truthful outcomes | cancelled-before-native (`OperationCanceledException`), completed, or `CodeGraphCommitOutcomeUnknownException` (Persist lines 978–989 → quarantine at MutateAsync line 402). **Verified.** |
| Caller's await obligation | The outcome is only knowable by awaiting: no callback, no event, and the native window cannot report cancellation. **Verified.** |
| D4 invariant-5 consequence | "A cancelled operation therefore never exposes partial facts or index entries, and a durable mutation is never reported as cancelled." Verified behaviourally by PROBE C/D/F (§4). |

**Interface/implementation consistency**: C-1 (`LatticeCodeGraphStore` class `<remarks>`) and C-2
(`ICodeGraphIndexStore` `<remarks>`) state the same two checkpoints, the same non-cancellable
window, the same three outcomes and the same await obligation. No divergence between the two
documents and no divergence from the implemented behaviour. **Consistent.**

**C-2 placement is appropriate.** The remarks were added to `ICodeGraphIndexStore` — the mutation
interface in `src/Andirit/ICodeGraphStore.cs` whose members carry the `CancellationToken` parameters
and whose implementations are the ones whose behaviour the contract describes. Publishing it on the
mutation interface (rather than on the read-oriented `ICodeGraphStore` facade in the same file)
puts the contract where its token parameters live. The commission's plan named the file, not the
interface; the producer's choice is within the authorization and is the better of the two options.

**Absence of packaged XML documentation is neither a violation nor a qualification.** No governing
requirement names an artifact format for the contract. The entry commission's implementation
proposal *proposed* XML documentation "embedded in the assemblies"; that proposal was not a
requirement, and the projects do not enable `GenerateDocumentationFile`, so no XML documentation
file exists to embed. The contract is published in source-level API documentation, which is the
only documentation artifact these projects produce. The erratum the producer recorded in the
decision record (ADR §4) is accurate. **Neither violation nor qualification — no requirement is
unmet.**

### 3.2 Independent contract gate — PASS (58/58, 0 interruption mandates)

The reviewer did **not** reuse the producer's adjudication table. `evidence/reviewer-contract-gate.py`
enumerates the tracked Markdown corpus with a **deliberately wider** superset regex (adding
`halt*`, `discontinu*`, `should`, `may not`, `cannot`, `must not`, `stage`/`staging` to the
native term family) and finds **58 candidate lines** in **522 documents** — 25 more than the
producer's 33. Every candidate is adjudicated against the reviewer's own reading of the governing
requirements, with document context captured in
`evidence/contract-gate-candidate-context.txt`.

| Reviewer category | Count | Meaning |
| --- | --- | --- |
| `REQUIRES-EVIDENCE` | 25 | requires native **evidence** for the cancellation path (D4 invariant 5 non-exposure, the D4 qualification clause, S-02, M9.5, M12.2, I-04/Q2-F09) |
| `RECORDS-LIMITATION` | 13 | records the non-interruptible window or a conditional future dependency change |
| `FORBIDS-INTERRUPTION` | 8 | explicitly forbids claiming/simulating interruption or negating a durable outcome |
| `NON-NATIVE-INTERRUPTION` | 6 | "interruption" denotes a process/store-lifetime/run-lifecycle event |
| `OUT-OF-SCOPE` | 6 | other tracks (EC-6C, EC-6D, rename, F-02/DF-4 durability) |
| **`MANDATE-INTERRUPTION`** | **0** | **no line requires that active native execution be interruptible** |
| unclassified | 0 | gate does not fail closed |

The 25 candidates the producer's narrower net missed are, without exception, non-native uses of
"interruption", out-of-scope track prose, D4 invariant-5 verbatim restatements, or the producer's
own gate-summary rows. **The reviewer independently reaches the same conclusion as the producer:
no binding requirement mandates interruption of active native work; the bounded
contract-resolution route is not triggered.**

## 4. Test qualification — 13/13 genuinely demonstrate the claimed behaviour

The reviewer executed the suite independently on Windows x64
(`evidence/reviewer-latticedb-suite.log`): **88 total, 88 passed, 0 failed, 0 skipped**, with all
13 `NativeCancellationPathTests` individually reported `Passed`. The producer's per-case receipts
(`case-summary.json`, `Q-01.log`…`REG.log`) were spot-checked and are genuine `dotnet test`
transcripts.

### 4.1 Reviewer-owned diagnostic probes (8 probes, all PASS, stable across 3 runs)

The producer's tests do not record *which* branch a racing case took, so the reviewer built an
isolated instrument (`evidence/probe/`, outside the repository, not part of `Andirit.slnx`,
assembly-named to reach the internal fault-injector seam) and ran it three times
(`evidence/probe-output.txt`, `-run2`, `-run3`) — identical results every time.

| Probe | Question | Independent observation |
| --- | --- | --- |
| **A** | Does the producer's Q-02 schedule actually reach the native window? | All 3 attempts: the operation entered the native transaction, the cancellation was observed **during/after** the native window, and the staged row contained **300/300** nodes. Q-02 is **not vacuous**. |
| **B** | Does the producer's Q-04 schedule reach the window, and is a false cancellation ever reported? | **20/20** repetitions reached the native window; **20/20** completed; **0** outcome/state disagreements; **0** false cancellations. |
| **C** | With the window provably open, is cancellation not observed and the unit committed whole? | Cancellation requested = `True`, observed = `False`, 1 staged row with **300/300** nodes. |
| **D** | Does a pending cancellation ever expose a staged fact to a concurrent reader? | 0 facts, 0 publications exposed; the reader was neither served partial state nor failed. D4 invariant 5 holds under concurrency. |
| **E** | Is a pre-cancelled request refused before any native transaction opens? | Outcome `cancelled`; the operation **never** opened the native transaction; 0 staged rows. |
| **F** | Is "cancelled before native invocation" ever reported when a native transaction actually opened? | A native write landed (1 staged row) and **no** cancellation was reported. The outcome taxonomy is **truthful**. |
| **G** | The plan's Q-06 second scenario, which the producer's suite does not cover | The reviewer executed it: a staging that **completes** after a cancellation arrived inside the native window preserves the accepted publication identity, snapshot identity, consistency flag and published-unit count across reopen. **It passes** (finding F-2). |
| **H** | Does a third cancellation checkpoint exist between the write gate and the native transaction? | **Yes** — deterministically demonstrated: `InMemoryCodeGraphStore.StageIndexUnitAsync` throws `OperationCanceledException` at `src/Andirit/InMemoryCodeGraphStore.cs:311`, and `MutateAsync` line 382 drives it **before** `Persist` opens the native transaction (finding F-1). |

### 4.2 Challenge results

| Challenge | Result |
| --- | --- |
| **False-positive assertions** | None found. Every case asserts the native durable state directly (`HetuStage`/`HetuUnit` read through `LatticeDbSharp` after the store released the file), so no case can pass on the managed projection alone. Q-02's `MarkerCount` check is a real atomicity check (300 exact occurrences of the unit's qualified-name prefix). |
| **Unexercised cancellation paths** | Q-06's second plan scenario is not covered by the committed suite (F-2); Q-12's native-ABI half is not in the committed test (F-3); Q-08 omits the plan's "no temporary artifact remains" assertion (F-8). All three were discharged by the reviewer's own additional qualification; none is a defect in the contract or the implementation. |
| **Timing dependencies and races** | Q-02/Q-04 are correctly written as **invariant** tests (accept either truthful outcome, never a partial one) rather than timing assumptions — the correct operationalization, because an untimed canceller cannot be guaranteed to land in the window. PROBE B measured 20/20 in-window for Q-04's schedule; PROBE A showed the Q-02 schedule can land either side across runs. No assertion depends on a wall-clock bound except Q-07's bounded `Assert.False(disposal.IsCompleted)` after 200 ms, which is safe because the write gate is provably held. |
| **Incorrect assumptions about native execution** | None. The tests read the native file only after disposal, matching the engine's single-process open policy — the reviewer's first probe draft violated this and failed with `LatticeException -16`, confirming the constraint is real and that the committed tests respect it. |
| **Tests that pass without establishing their invariant** | Two found and closed by the reviewer: Q-06's second scenario (PROBE G — passes) and Q-04's "never reports cancellation after a commit" branch, which PROVE B shows is exercised (20/20 in-window, 0 false cancels). Q-02's completed branch is exercised by PROBE A (3/3 in-window in the final run) and by PROBE C deterministically. |

## 5. Production equivalence — independently confirmed

The reviewer rebuilt **both pinned revisions from scratch** in separate worktrees and compared them
with a reviewer-written `System.Reflection.Metadata` dumper (`evidence/revdump-Program.cs`),
excluding only the MVID and the PE debug directory, which a deterministic build derives from the
source hash (`evidence/equivalence/equivalence.txt`).

| Assembly | Metadata+IL dump lines | Baseline SHA-256 | Implementation SHA-256 | Identical |
| --- | --- | --- | --- | --- |
| `Andirit.Abstractions.dll` | 1 239 | `F5682749…` | `F5682749…` | **True** |
| `Andirit.dll` | 5 522 | `2B4D10F6…` | `2B4D10F6…` | **True** |
| `Andirit.LatticeDb.dll` | 888 | `9920C467…` | `9920C467…` | **True** |

7 649 metadata+IL lines across the three production assemblies, **0 differences** — the producer's
line-count claim is exactly reproduced. The **public API surface** of all three assemblies is also
identical (0 diff lines). **The change is provably documentation-only; production runtime behaviour
is unchanged.**

**Package content, rebuilt per revision** (`evidence/package/package-comparison.txt`): 6 entries,
**4 identical, 2 different** — `lib/net10.0/Andirit.LatticeDb.dll` and `Andirit.LatticeDb.nuspec`.
A byte-region analysis of the two DLLs (`evidence/package/dll-pe-regions.txt`) attributes every
differing byte to provenance: the MVID (16 bytes in `.text`), the embedded PDB/CodeView payload,
the PE header timestamp, and the `.rsrc` version resource — and the nuspec difference is **only**
line 15, `<repository commit="…">`. The version resources confirm it:
`ProductVersion = 0.2.0-preview.7+<commit>` on each side. **No content or behaviour difference.**

**Boundary** (`evidence/boundary.txt`): no project-file, `Directory.Build.props`, workflow or
threshold change between the two revisions; the workflow's thresholds are exactly `55 / 75 / 60 /
70`; no coverage exclusion; the LatticeDb step runs with no `--filter` and no OS condition; the CI
matrix includes `ubuntu-latest`. `LatticeDbSharp` remains 0.2.0 — the reviewer measured the pinned
managed assembly SHA-256 `d803ca19…` and all three native asset hashes, and they match the entry
commission's receipt byte for byte.

## 6. Platform and execution evidence — PASS

**Windows x64.** Independently re-executed: 88/88 (see §4). The producer's local receipts were
spot-checked and agree.

**Genuine hosted Ubuntu x64.** Re-fetched from the GitHub Actions API by the reviewer
(`evidence/hosted/reviewer-hosted-verification.txt`, generator `evidence/hosted/hosted-verify.py`;
fresh raw job logs and coverage artifacts in `evidence/hosted/`):

| Item | Independent result |
| --- | --- |
| CI run `45037630799` | `completed` / `success`, event `push`, workflow `ci` |
| `headSha` | `01624943233f16447c3176b7c905c544642dfb2c` — **the pinned implementation revision itself** |
| Job `137254795969` | `test (ubuntu-latest)`, `completed` / `success`, runner `GitHub Actions 1000004506`, runner group `GitHub Actions`, labels `['ubuntu-latest']` |
| Genuine-Linux proof | workspace `/home/runner/work/andirit/andirit`, shell `/usr/bin/bash`, `DOTNET_ROOT=/usr/share/dotnet`, .NET SDK **10.0.404**, runtime **10.0.15**; **zero** Windows/WSL/Docker indicators in the freshly fetched log |
| Suite counts | core 345, CSharp 372, TreeSitter 157, **LatticeDb 88** — all `failed=0`, all `skipped=0` |
| Baseline comparison | run `44993021436` on `b5a81f8…`: LatticeDb **75 → 88**, delta **exactly 13** = the number of `[Fact]` methods in the committed new test file; the other three suites unchanged |
| Coverage gates (freshly downloaded raw Cobertura artifacts) | core 82.42 / 70.74 (≥55), CSharp 95.11 / 87.85 (≥75), TreeSitter 89.89 / 75.95 (≥60), LatticeDb 92.87 / 78.96 (≥70) — **all met**; the reviewer's figures reproduce the CI's own coverlet summary lines byte for byte |

**The 13 additional passing hosted tests are exactly the S6.1 tests**, established by the
count-delta argument rather than assumed, and **no evidence substitution** occurred: the runner
layout, shell, runner group and log contents are inconsistent with WSL2, Docker-on-Windows or any
Windows-hosted compatibility environment.

**Native ABI census (Q-12's native half).** Re-run by the reviewer against the pinned package's
three native assets (`evidence/abi/reviewer-native-abi-scan.txt`): linux-x64 191 dynamic symbols,
win-x64 105 exports, osx-arm64 identifier scan — **0 `lattice_*`-namespace cancellation-capable
entry points**, matching the entry commission's receipt exactly. The committed Q-12 test covers
only the managed census; the native half is discharged here by independent re-census (finding F-3).

## 7. Findings

Every finding is independent, evidence-backed and **non-blocking**. No finding requires remediation
before the owner's acceptance decision, and none was remediated during this commission.

| ID | Finding | Governing requirement | Independent evidence | Severity / blocking |
| --- | --- | --- | --- | --- |
| **F-1** | The published contract enumerates **two** honoured checkpoints, but a **third** exists: `InMemoryCodeGraphStore.StageIndexUnitAsync` observes the token (line 311, and again after validation at line 347), and `MutateAsync` line 382 drives it before the native transaction opens. | Contract precision; D4 invariant 5 (outcome) | PROBE H (deterministic, stack trace to `InMemoryCodeGraphStore.cs:311`) + source-path citation | **LOW / non-blocking** — the outcome taxonomy and the non-exposure guarantee are unaffected: it is still "cancelled before native invocation". A future documentation erratum should say "honoured while waiting for the write gate, during the managed projection apply, and immediately before the native transaction opens". |
| **F-2** | The producer's Q-06 covers only the first of the **two** scenarios the plan requires (pre-native cancelled staging); the completed-but-cancelled-request staging with an existing publication is not covered by the committed suite. | Plan §2 Q-06 | PROBE G — the reviewer executed the second scenario and it **passes** (publication identity, snapshot identity, consistency flag and unit count preserved across reopen) | **MEDIUM / non-blocking** — the invariant holds; the coverage gap is closed by reviewer evidence, not by the committed suite. Recommend adding the second scenario to the committed suite under a future owner-authorized change set. |
| **F-3** | The committed Q-12 test performs only the **managed** census; the plan also requires the **native-ABI** re-census at the sealed revision. | Plan §2 Q-12 | `evidence/abi/reviewer-native-abi-scan.txt` — 0 lattice-namespace cancellation entry points, asset hashes matching the entry receipt | **LOW / non-blocking** — discharged independently by the reviewer; the dependency is provably unchanged. |
| **F-4** | The plan's Q-02 expected outcome ("returns success, never `OperationCanceledException`") is unachievable for an untimed canceller; the producer's invariant framing (accept either truthful outcome, never a partial one) is the correct operationalization. | Plan §2 Q-02 | PROBE A — the Q-02 schedule landed in-window in one run and pre-native in another; **no partial state in either** | **LOW / non-blocking** — a justified deviation from the plan's literal expectation; recorded so the successor does not read it as a gap. |
| **F-5** | Q-10 mapping divergence: the plan's Q-10 is "preservation of the historical Stage-2 regressions", while the producer's Q-10 test is a store-semantics control. The obligation's substance is discharged by the unchanged whole-suite counts, which the producer evidences separately. | Plan §2 Q-10; M-matrix | Reviewer's own 88/88 run; hosted 345/372/157/88 unchanged; thresholds unchanged | **LOW / non-blocking** — traceability-labelling only; the discharge is real but is evidenced by the suite receipts, not by the test named Q-10. |
| **F-6** | The producer's commission record §6 and ADR §7 quote the **Windows** coverage figures for three of the four suites under the heading "Coverage gates on the hosted runner" (core 82.42/70.89, CSharp 95.15/88.00, TreeSitter 89.85/75.45). The committed hosted artifacts actually contain 82.42/**70.74**, 95.11/**87.85**, 89.89/**75.95** — which the CI's own coverlet summary confirms. LatticeDb coincidentally matches (92.87/78.96). | Evidence accuracy (G-4) | `evidence/coverage-comparison.txt` — producer's committed local vs hosted artifacts compared directly | **LOW / non-blocking** — the committed artifacts are correct; only the prose mis-attributes the numbers. **All values clear their thresholds under both sets, so no gate conclusion changes.** Recommend an erratum in the producer's record under a separate authorization. |
| **F-7** | The producer's package and API-surface receipts were produced while the working tree's `HEAD` was still the baseline commit, so the `<repository commit>` nuspec stamp and the version-resource stamp match between the two packages. Rebuilt correctly (one package per revision), **2** of 6 package entries differ — the DLL and the nuspec commit stamp. | Evidence reproducibility (G-4) | `evidence/package/package-comparison.txt`, `dll-pe-regions.txt`, `show-nuspec.py`; `api-surface/api-diff.txt` records "implementation-dump: D:\projects\andirit (working tree of the implementation revision)" | **LOW / non-blocking** — every differing byte is provenance metadata (MVID, PDB checksum, PE timestamp, version-resource commit, nuspec commit stamp); the reviewer's independent rebuild from the pinned revisions confirms the substantive conclusion (documentation-only) with **0** metadata/IL differences. |
| **F-8** | Q-08 does not assert the plan's "no native handle **or temporary artifact** remains" — it deletes the main database file only, not the `-wal`/`-journal` sidecars. | Plan §2 Q-08 | Source review of `NativeCancellationPathTests.DisposalReleasesTheNativeHandle` | **LOW / non-blocking** — the handle-release property (the load-bearing one) is proven; the sidecar assertion is a completeness nicety. The reviewer's probe harness deletes sidecars and never observed a leak. |

### 7.1 Stop conditions — none triggered

| Plan §6 stop condition | Independent result |
| --- | --- |
| 1. The pinned dependency exposes a cancellation mechanism | **No** — reviewer's ABI census: 0 entry points; managed census: 0 members; `LatticeTransaction` = Commit/Rollback/Dispose/IsCompleted only |
| 2. Any case shows a *partial* native application | **No** — PROBE A/C: 300/300 nodes committed whole; 0 partial states in 20+3 repetitions |
| 3. A cancellation observed **after** a successful commit | **No** — PROBE B 20/20 completed with 0 disagreements; PROBE F; the committed REG guard |
| 4. A staged/published fact visible while a cancellation is pending | **No** — PROBE D: 0 facts, 0 publications exposed to a concurrent reader |
| 5. Disposal interrupts an in-flight native transaction, or the handle is not released | **No** — committed Q-07 (disposal waits; the write completes) and Q-08 (file deletable after disposal) |
| 6. Historical suites change count, expectation, threshold or gate | **No** — reviewer's own 88/88; hosted 345/372/157/88; workflow identical between the two revisions; thresholds 55/75/60/70 unchanged |
| 7. A non-Linux result proposed as Linux evidence | **No** — only `ubuntu-latest` hosted evidence is cited as Linux evidence; no WSL2/Docker claim anywhere |

## 8. Residual risks and lineage

| Item | Reviewer assessment | Blocking for S6.1? |
| --- | --- | --- |
| Active native execution is not interruptible | **Re-verified independently** (managed census + native ABI census + PROBE C). The limitation is published in the API contract and carried as Q-S61-1. No binding requirement mandates interruption (§3.2). | **No** — bounded limitation, correctly published |
| Q-S61-2 (await obligation), Q-S61-3 (atomicity, not interruption) | Verified behaviourally (PROBE C/F and the committed Q-05/REG guard) | No |
| Q-S61-4 (osx-arm64 unqualified) | Preserved. The pinned dependency ships an osx-arm64 asset but declares it a candidate RID with no supported-capability declaration. The reviewer makes **no** macOS native claim; the CI `macos-latest` job covers managed suites only. | No |
| Q-S61-5/Q-S61-6 (genuine hosted Linux; platform-invariant contract) | Satisfied for `linux-x64` by the hosted run; the contract text is platform-invariant and the ABI census shows the same absence of interruption on all three assets | No |
| Q-S61-7 / Q-S61-8 (scope; F-02 boundary) | Preserved. Managed fault injection is **not** crash/power-loss/abrupt-kill/multi-process durability | No |
| **DF-4** (managed fault injection ≠ crash/power-loss durability) | **Open, and correctly out of S6.1 scope.** The plan's stop conditions and the entry record assign durability to S6.2. Nothing in S6.1 claims durability. | **No** — deferred, not blocking |
| **M12.1 / M12.3** and the complete S-02/D4.5 ten-invariant native matrix | **Open, and correctly out of S6.1 scope.** S-02 requires all ten invariants exercised natively; S6.1 discharges only the cancellation and unknown-outcome rows. The producer's traceability matrix states this as "partly discharged", which the reviewer confirms as the correct scoping. | **No** — deferred to S6.3 |
| I-04 / Q2-F09 | The cancellation aspect is addressed by the published contract; the rebuild-blocking aspect remains a D4/S6.3 matter | No |
| **Pre-existing four-file lineage gap** (`CI1A-S2.5-WP-S25-2-G2-EXEC-evidence`) | The reviewer verified the same 4 coverage XML files are absent at the remote base, at the entry baseline and at the evidence revision — pre-existing, outside the S6.1 lineage. **Not repaired**, as instructed. | No |
| Deferred obligations that might be blocking? | **None.** The S6.1 acceptance boundary derived at the entry commission is exactly: a dedicated native cancel-path test, non-exposure of partial facts, the truthful outcome contract, and the recorded cancellation policy. All four are established. M12.1/M12.3, DF-4, DF-6 and platform certification are named deferrals with owners, not S6.1 acceptance criteria. | **No** |

## 9. Disposition

**`S6.1 — QUALIFIED WITH QUALIFICATIONS`**

Verified behaviour (independently re-derived, not accepted from the producer):

- the cancellation contract is published at the store and at the interface, consistently, and
  matches the implemented behaviour;
- cancellation is honoured at the write gate and before the native transaction; a pre-cancelled
  request never opens a native transaction (PROBE E);
- the native write window is **not** cancellable — verified from source and behaviourally
  (PROBE C, committed Q-03);
- the three truthful outcomes are exclusive and truthful — a native write was never reported as
  cancelled and a cancelled operation never reported as committed (PROBE B 20/20, PROBE F, REG);
- a cancelled or completed-but-cancelled-requested operation never exposes partial facts or index
  entries, including to a concurrent reader (PROBE C/D/G);
- disposal waits for an in-flight native write and releases the native handle (committed Q-07/Q-08);
- production runtime behaviour is unchanged — 7 649 metadata+IL lines identical, public API
  identical, package differences provenance-only;
- the qualification matrix executed on Windows x64 and on genuine hosted Ubuntu x64 with 0 skips;
- the pinned dependency still exposes no cancellation/interruption capability at the managed or
  native ABI level.

Supported qualifications (carried): Q-S61-1…Q-S61-8, Q-L1…Q-L8, MR-1…MR-4, GH-1…GH-4, DF-1…DF-8,
and the eight non-blocking findings F-1…F-8 (§7).

Unsupported claims found: **none.** The producer's substantive claims all reproduced. Two
*receipts* were found to be working-tree-derived rather than pinned-revision-derived (F-7) and one
prose passage mis-attributes coverage figures (F-6); neither changes any conclusion.

Untested behaviour: the plan's Q-06 second scenario and Q-12's native-ABI half are not covered by
the committed suite; both were discharged by the reviewer's own qualification (PROBE G, ABI census)
and are recorded as findings F-2/F-3 rather than as verified properties of the committed suite.

**S6.1 owner acceptance is not granted by this commission**, and **S6.2 entry is not authorized**.

## 10. Recommended next commission

**`ANDIRIT-CI1A-S2.6-WP-S61-ACCEPT-01`** — the owner acceptance record required by gate G-6 of the
successor commission: it must name exactly what S6.1 discharges (M9.5, M12.2, the D4 invariant-5
native case, the I-04/Q2-F09 cancellation aspect) and what remains deferred (M12.1, M12.3, the
complete S-02/D4.5 native matrices, DF-4, DF-6), decide on findings F-1 (contract-text erratum) and
F-2 (add the second Q-06 scenario to the committed suite), and only then authorize S6.2 entry.
