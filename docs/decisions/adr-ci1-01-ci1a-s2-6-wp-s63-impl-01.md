# ADR-CI1-01-CI1A-S2.6-WP-S63-IMPL-01 — S6.3 native publication-correctness qualification suite

**Project:** Andirit
**Milestone:** ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01 — implementation of the S6.3 native
publication-correctness qualification suite (remaining D4 invariants and the complete S-02
publication-correctness matrix), the D-05 regression remediation, and independently verifiable
evidence.
**Recorded:** 2026-11-24
**Baseline:** `1cbce3b36325d445b4554443d4b715087a3d65c4` (S6.3 entry assessment HEAD)
**Disposition:** `IMPLEMENTED — QUALIFICATION-READY WITH ONE OPEN EVIDENCE LEG`
(hosted-Ubuntu x64 execution pending an authorized push; see §7)

---

## 1. Decision

The S6.3 publication-correctness obligations are discharged by a **test-infrastructure-only**
change set — a dedicated native publication-correctness suite, a bounded D-05 remediation, and a
worker-process harness — with **zero production-code changes**. The governing decision is that
the existing `LatticeCodeGraphStore` implementation already satisfies D4 invariants 1–4 and 6–10
and the S-02 matrix, and that S6.3's obligation is to *qualify* that behaviour against production
storage, not to re-implement it. This mirrors the entry feasibility assessment and condition
C-S63-1.

No production behaviour, dependency, workflow, threshold, public API, or sealed-evidence byte was
changed. The complete `src/` delta against the entry baseline is empty.

## 2. Governing requirements (unchanged, restated for traceability)

- ADR-CI1-01 owner decision §D.4, invariants 1–10, and the D4 qualification clause (*"CI-1A must
  reproduce the applicable stale-index, cancellation, failed-publication, supersession and reopen
  tests using actual production storage and APIs"*).
- S-01 §S-02 / CI1A specification line 194: all ten invariants exercised against actual production
  `LatticeCodeGraphStore` storage, including failed/cancelled/superseded publication, reopen and a
  native concurrency/republication scenario; in-process deterministic interleaving is diagnostic
  only and is not native atomicity evidence.
- S2.5 qualification matrix rows M12.1 / M12.3 (M12.2 was discharged by S6.1).
- CI1A obligation register I-05 / I-06 / I-07; residual-risk register DF-2 / DF-3 / DF-8.
- S6.2 finding F-QU-01 (D-05 test logic) as a required follow-up.

No requirement was invented, redefined, or weakened. Every invariant keeps the entry
traceability matrix's case assignment.

## 3. Implementation decision detail

### 3.1 Publication-correctness suite (`NativePublicationCorrectnessTests`)

Sixteen cases (P-01…P-16) implement the entry traceability matrix exactly, preserving the
identifiers, requirement mapping, constructed/genuine classification, and expected assertions:

| Case | Discharges | Class |
| --- | --- | --- |
| P-01 | D4-1, D4-2 | constructed (real supersession) |
| P-02 | D4-1, D4-2 | constructed (durable tamper) |
| P-03 | D4-3 | constructed |
| P-04 | D4-4, S-02-2 | constructed (fault-injector) |
| P-05 | D4-6, S-02-4 | genuine |
| P-06 | D4-6 | constructed |
| P-07 | D4-7, S-02-5 | genuine |
| P-08 | D4-7 | genuine (recovery rebuild path) |
| P-09 | D4-8 | constructed |
| P-10 | D4-8 | constructed |
| P-11 | D4-9 | constructed |
| P-12 | D4-10, S-02-6 | genuine (concurrent reader) |
| P-13 | S-02-4 | genuine |
| P-14 | S-02-5 | constructed (fault-injector) |
| P-15 | S-02-6, D4-10, F-QU-01 | genuine (multi-process) |
| P-16 | S-02-7 | documentation/diagnostic |

Every case runs the production `LatticeCodeGraphStore` over a real LatticeDB file. Where the
durable state is load-bearing, the case asserts the native tables directly
(`AndiritOccurrenceMeta` / `AndiritOccurrenceUnit` / `AndiritOccurrencePage`,
`HetuStage` / `HetuUnit`) after the store has released the file — never on the managed
projection alone.

Two implementation choices are recorded because they were corrected during execution:

1. **Supersession (P-06).** The clean deterministic scenario for *"a superseded publication must
   not replace a newer accepted generation"* is two runs planned against the same baseline where
   the later-completing one wins: the loser's completion is then refused with
   *"The index run planned against a superseded publication"* and the newer generation is
   retained. Re-completing an already-completed run is either an idempotent no-op or refused
   ("a completed index run cannot publish different incremental state"); it cannot regress the
   accepted generation. P-06 asserts the refusal path.
2. **Rebuild equivalence (P-08).** The projection rebuild from durable authority
   (`RebuildInnerFromDurableStateAsync`) is triggered deterministically by a *valid* mutation
   whose native commit fails before `Commit` (fault-injector at `before-commit`). Staging against
   an already-completed run would fail managed validation before the native window and would not
   exercise the rebuild path, so P-08 registers a fresh running run first.

### 3.2 D-05 remediation (F-QU-01)

The S6.2 D-05 case was skipped with a message blaming "test environment file locking". The real
defects were: (a) the case asserted that one staged row remained *after* a completed publication,
although a completion consumes its staged rows, so the case could never pass; and (b) it never
exercised separate processes. The remediation removes the skip, corrects the durable-state
assertions (the publisher's staged row is consumed; the never-completed hold run's staged row
remains and is invisible to queries), and drives the scenario through genuine worker processes:
a hold worker owns the file lock, a second open fails (negative control), an independent
publisher completes a real publication, and a fresh reopen corroborates the committed state.

The worker (`Andirit.LatticeDb.Tests.Worker`) gained a `publish-complete` mode that stages
occurrence facts and completes the run — the correction F-QU-01 named. Its original `hold`
behaviour (used by S6.2's D-07) is preserved unchanged.

### 3.3 Truthfulness boundaries preserved

- The unknown-outcome path (P-14) quarantines and is corroborated by reopen; it is never
  reported as a confirmed rollback or as a cancellation of a committed transaction.
- Managed fault injection is not crash/power-loss/abrupt-kill durability (Q-S62-1, F-02, DF-4);
  no case claims otherwise.
- In-process interleaving is diagnostic only (P-16, S-02-7).
- osx-arm64 remains content-evidence only (C-S63-6).

## 4. Consequences

- The remaining eight D4 invariants and the complete S-02 matrix now have passing,
  production-backed, native-asserted evidence on Windows x64.
- The hosted-Ubuntu x64 leg required by C-S63-4/Q-S61-5/Q-S62-5 **cannot** be produced by this
  commission because a push is required to bind a CI run to the implementation commit, and no
  push was authorized. This is the single open evidence leg; it is an execution step, not an
  implementation step, and it gates G-2 of the qualification plan.
- The qualification commission (`ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01`) receives a green Windows
  baseline, sealed evidence, and a producer-prepared probe harness that it must re-derive and
  re-execute independently (G-5).

## 5. Alternatives considered

- *Production change to expose a test seam for stale-index construction.* Rejected: condition
  C-S63-1 forbids production changes, and the stale cases are constructible through the durable
  tables and real supersession.
- *Leaving D-05 skipped.* Rejected: F-QU-01 made the fix a required follow-up, and a skipped
  multi-process case would leave S-02-6 without native evidence.
- *Simulating crash/power-loss.* Rejected: explicitly out of scope (Q-S62-1, C-S63-5).

## 6. Compliance with the S6.3 entry conditions

| Condition | Response |
| --- | --- |
| C-S63-1 no production changes | satisfied — `git diff 1cbce3b HEAD -- src/` is empty |
| C-S63-2 LatticeDbSharp 0.2.0 pinned | satisfied — `PackageReference Version="0.2.0"` unchanged |
| C-S63-3 D-05 logic fixed for the concurrency scenario | satisfied — skip removed, completion corrected, worker `publish-complete` mode added, genuine processes exercised (D-05 + P-15) |
| C-S63-4 genuine hosted Linux evidence mandatory | **open** — requires an authorized push to bind a CI run to the implementation commit; recorded as the single open leg |
| C-S63-5 crash/power-loss not simulated | satisfied — no case claims managed fault injection is crash durability |
| C-S63-6 osx-arm64 unqualified | satisfied — no macOS runtime claim; limitation carried |
| C-S63-7 interleaving diagnostic only | satisfied — P-16 records the boundary reflectively |
| C-S63-8 no publication/tag/push | satisfied — local commits only; tag `v0.2.0-preview.7` untouched |
| C-S63-9 lineage byte-exact | satisfied — no sealed package modified; new package extends the lineage |
| C-S63-10 S6.1/S6.2 findings carried | satisfied — F-QU-01 closed by remediation; F-QU-02 discharged by genuine multi-process evidence; all others carried unchanged |

## 7. Disposition

**`IMPLEMENTED — QUALIFICATION-READY WITH ONE OPEN EVIDENCE LEG`.** All sixteen P-cases and the
remediated D-05 pass on Windows x64; the full solution regression is green (988 tests, 0 failed,
0 skipped); the LatticeDb module coverage is 92.87 % line / 78.96 % branch against a 70 %
threshold; the production runtime delta is zero. The hosted-Ubuntu x64 leg is an execution step
that requires an authorized push; until that authorization, criterion 5 of the implementation
commission is only partially satisfied and full `IMPLEMENTED AND READY FOR INDEPENDENT
QUALIFICATION` is deliberately not claimed.

**Next commissions:** owner push authorization → CI run bound to the implementation commit →
`ANDIRIT-CI1A-S2.6-WP-S63-QUAL-01` (independent qualification, G-1…G-5) →
`ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01` (owner acceptance, G-6).

**Erratum (recorded 2026-11-25, after sealing).** During this commission `origin/main` moved by a
push this session did not perform: the remote now carries the full S6.1/S6.2 lineage through
`359a9e8` plus two owner documentation commits (`8715ac3`, `4ef34b5`). This commission's two commits
(`1cbce3b` entry assessment, `33b5867` implementation) remain local-only, so local `main` and
`origin/main` have **diverged** (merge base `359a9e8`). The owner's push authorization therefore
needs an integration step (merge or rebase, owner's choice) before the hosted-Ubuntu CI leg can be
produced. Recorded in full in the implementation record §15.


**Resolution (-R1, 2026-11-25).** Owner-authorized integration rebased the unpublished S6.3 commits onto origin/main (4ef34b5) with no conflicts and no content change (every S6.3 blob identical by Git hash; the original lineage is preserved at the unpublished recovery reference refs/backups/s63-impl-original). Mapping: 1cbce3b -> 94a8c19 (entry assessment), 33b5867 -> ece6002 (implementation), 31673f5 -> 89ae722 (erratum), d9461c0 -> 7a64ab2 (formatting). Publication was a clean fast-forward. The hosted-Ubuntu evidence collected against the integrated revision is recorded in the separately identified evidence extension and in implementation record §16.