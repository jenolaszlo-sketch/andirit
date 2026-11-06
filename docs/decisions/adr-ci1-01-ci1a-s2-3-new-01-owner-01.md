# ADR-CI1-01-CI1A-S2.3-NEW-01-OWNER-01 — Qualified owner acceptance of NEW-01 recovery remediation

**Commission:** `ANDIRIT-CI1A-S2.3-NEW-01-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`NEW-01 ACCEPTED WITH QUALIFICATIONS`**.
**Gate:** **WP-S23-1 / NEW-01 PASSED within the qualified execution scope below.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
>
> The original NEW-01 projection-loss and conflict-bypass defect is closed for
> the qualified recovery paths. Qualifications Q1-Q1 and Q1-Q2 remain binding.
> WP-S23-2 is eligible for a separately authorized commission; this decision
> does not commission or begin it.

This is an additive owner decision. It changes no production source, tests,
reviewer evidence, frozen R2 oracle/corpus, prior decision, or planning contract.

## 1. Authority, exact baseline, and ancestry

Controlling authority: the [S2.3 entry owner decision](adr-ci1-01-ci1a-s2-3-entry-owner-01.md),
[NEW-01 entry contract](../planning/adr-ci1-01-ci1a-s2-3/new-01-recovery-remediation.md),
[work-package plan](../planning/adr-ci1-01-ci1a-s2-3/work-package-plan.md), and
[independent qualification](../reviews/ANDIRIT-CI1A-S2.3-NEW-01-Q1.md).

| Role | Exact commit | Exact tree |
| --- | --- | --- |
| Entry owner / pre-fix | `311e97ffa716ff725f70dcb57ca71dca891903f6` | `72666340399bdc26012d85566decab9a840c32bb` |
| Accepted implementation | `c75c41f39f86744292de6df42410a465a237f7be` | `b7f2f1740728ccf3e902d1af06b41849cdbbfe80` |
| Producer report | `9a9c50238ecdcd75a8e89e4ed76d8696b5141df4` | `1f4275dd0f4a34a8776d573eb16240c804fed125` |
| Producer evidence / review baseline | `cecb9a88f9a95462272d4498d8600e9ef03eb5f6` | `0d2ded15935a71ee7c45975a5d0bdfa89e1ae525` |
| Independent reviewer / owner baseline | `ef097aa595bfeade70952d274cb38728f1c71346` | `42dfa50d8553913d1a84bb117cc6b3b5e165d8ef` |

These form an exact consecutive parent chain, verified from Git objects, not
just abbreviated names. Git commit/tree identifiers use this repository's
SHA-1 object format; they are distinct from the SHA-256 file digests in §4.

The owner began on clean canonical `main`, at the reviewer commit/tree above,
with one worktree (`D:/projects/Andirit`), no stashes, and 23 ahead / 0 behind
local `origin/main` (`443c192733a53d2f1da415743debb84e8c81acb5`). No fetch,
branch, additional worktree, push, or publication was performed.

## 2. Change scope and evidence integrity

The implementation changes only `src/Andirit.LatticeDb/LatticeCodeGraphStore.cs`
and adds `tests/Andirit.LatticeDb.Tests/New01RecoveryProjectionTests.cs` (509 lines).
The production delta is **23 insertions / 4 deletions**. Both known-failure
catch sites call `RebuildInnerFromDurableStateAsync`, which executes graph
`ReplayAsync(_commands, CancellationToken.None)`, `RestoreBaselines(_inner)`,
and `RestoreOccurrences(CancellationToken.None)`, matching initial-open recovery
of the accepted durable occurrence publication. The unknown-outcome catch is
unchanged: it sets `_outcomeUnknown`, performs no replay, and rethrows.

The reviewer commit adds exactly **16 files**, all confined to its report,
`docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/`, and
`docs/reviews/CI1A-S2.3-NEW-01-Q1-evidence/`. It changes no producer test,
production source, decision, planning contract, frozen corpus, or frozen oracle.
The producer report/evidence commits contain only their review documentation.

All **12** entries in the reviewer's self-excluding `manifest.json` were
rehashed using SHA-256 over exact bytes and checked against recorded byte
lengths; every entry passed. Its inventory accounts for every evidence file
except the manifest itself. Manifest SHA-256:
`7E6B19310553EFDE9A4B6C5F856847854645EF9EAEECBFE4B9099CC696D37B0B`.
The report and probe source are bound by the verified reviewer Git tree.

Two non-blocking documentation corrections are recorded here without rewriting
reviewer evidence: the report's `+27/-4` production count confuses 27 total
changed lines with additions; the correct count is `+23/-4`. The saved
`production-diff.txt` has a commit/stat header and two UTF-8 em dashes rendered
through CP437 as `ΓÇö`, both in added comments. Removing that header and
correcting those two comment renderings makes its complete production patch
text equal the actual Git diff. Executable lines match exactly; the preserved
file's byte digest still matches its manifest. The Git source diff controls.

## 3. Verification and closure of the original defect

Owner verification used Windows x64, .NET SDK `10.0.401`, runtime `10.0.12`.
The committed reviewer logs were inspected and the following gates rerun at
`ef097aa`, before adding this decision:

| Gate / command | Owner result |
| --- | --- |
| `dotnet test docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj -c Release --no-restore --logger 'console;verbosity=normal'` | 17 passed, 0 failed, 0 skipped; P01–P17 individually reported |
| `dotnet test Andirit.slnx -c Release --no-restore` | 796 passed, 0 failed, 0 skipped: native 50 + TreeSitter 157 + core 345 + CSharp 244 |
| `dotnet build Andirit.slnx -c Release --no-restore` | 0 warnings, 0 errors |
| `dotnet format Andirit.slnx --verify-no-changes --no-restore` | Exit 0 |
| `dotnet format docs/reviews/CI1A-S2.3-NEW-01-Q1-tests/ReviewerTests.csproj --verify-no-changes --no-restore` | Exit 0 |
| Frozen R2 oracle replay with explicit `--expected` (see §4) | 185/185, 0 failed; exact observed output digest reproduced |

The 17 independent probe implementations were inspected, including their
reviewer-owned native row decoding and expected facts/coverage. They use the
accepted friend assembly and existing fault injector without altering production
visibility. They do not derive expectations from producer observations. Native
row reads occur while the store is closed; failed-publication comparisons
protect the published graph/occurrence labels separately from successful staging.

| Probe | Verified obligation |
| --- | --- |
| P01 | Before-commit completion failure: recovered reader equals prior state and durable reopen; independent occurrence fact/coverage/manifest/page/baseline decoding and D3 checks |
| P02 | Apply-time payload conflict refused; recovery retains occurrences and subsequent conflict validation |
| P03 | Apply-time missing graph endpoint refused; graph traversal and durable rows conserved |
| P04 | Failed run absent durably; ordering baseline and same-owner fact retirement preserved |
| P05 | Failed staging preserves existing stage row; re-staged contributions publish within the publishing lifetime |
| P06 | Failed deletion invisible; retry retires target with complete `Available` / zero-hit semantics |
| P07 | Recovery followed by publication preserves distinct facts and sorted contributor union |
| P08 | Two consecutive before-commit failures preserve the reader; subsequent retry publishes once |
| P09 | Complete-empty, Partial, graph-only availability, and all refusal counters preserved across recovery |
| P10 | Chained apply/persist/apply recovery leaves conflict refusal effective and ends in reopen parity |
| P11 | Native mirror and filtered/unfiltered traversal conserved on failure; retry and native/non-native reopen agree |
| P12 | Native edge rejection and staged deletion conserve mirror/projection on failure; retry updates both |
| P13 | Corrupt occurrence payload and namespace fail closed at open without changing tampered rows |
| P14 | Running baseline and accepted index state retained on failed empty completion; advance only on success |
| P15 | After-commit unknown outcome quarantines reads/mutations; fresh reopen reconciles the durable commit |
| P16 | Graph/occurrence schema constants preserved; unsupported graph metadata fails closed |
| P17 | No-fault reader and durable-state correspondence across successive opens |

The hash-verified historical `prefix-311e97f-probes.log` records two expected
defect failures at the entry-owner source: recovered `AnalysisUnavailable`
versus durable `Available`, and a conflict accepted instead of refused. Its
unknown-outcome control passes. This owner inspected that recorded historical
reproduction; it did not recreate or modify the historical probe source.
The corrected source and fresh P01/P02/P10 results close both original symptoms.

**Accepted findings:** both recovery paths rebuild graph and occurrence
projections; recovered published state corresponds to durable reopen in the
qualified scope; publication identity/baselines remain continuous; the conflict
bypass is closed; coverage, refusal counters, and complete-empty semantics remain
intact; unknown-outcome quarantine is unchanged; native mirror consistency and
D3/D4/D5 are preserved. No merge, identity formula, schema, migration, or
publication-authority change is accepted or introduced by NEW-01.

**Retired targets under complete repository coverage correctly return
`Available` with zero hits.** This is the accepted repository-scoped availability
mapping, not `AnalysisUnavailable` and not fabricated analysis.

## 4. Frozen-oracle identity reconciliation

These two identities are **SHA-256 hashes of the exact UTF-8 file bytes**, not
hashes of one shared canonical payload:

| Artifact | Bytes | Exact SHA-256 |
| --- | --- | --- |
| Observed verification report: `docs/reviews/CI1A-S2.3-NEW-01-Q1-evidence/oracle-r2.json` | 146851 | `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` |
| Frozen expected input: `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json` | 145620 | `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` |

`oracle/Program.cs` `CompareToExpected` compares the compact JSON serializations
of nine sections: `semantics`, `identities`, `variantIdentities`, `variants`,
`crossView`, `e02`, `coverage`, `counts`, and `lifecycle`. It appends one passing
`expected.<section>` check per section. `Finish` then writes the checks and the
resulting summary into the observed report.

The frozen file has 176 passing checks and summary `176/176`; the observed file
retains those same 176 checks and appends the nine passing comparisons, yielding
`185/185`. Direct owner comparison verified all nine compared sections, all
shared checks, and every other top-level value equal. Removing exactly those
nine appended check objects and changing only summary `total` and `passed` from
185 to 176 makes the observed file **byte-identical** to the frozen expected
file, including whitespace and encoding. There are no other byte differences.
Thus identical comparison-section content was expected and obtained; identical
whole-report content was never the comparison contract. The distinct file
hashes are fully explained and are not an unresolved canonical-content mismatch.

The owner replayed:

```text
dotnet run --project docs/planning/adr-ci1-01-ci1a-s2-0/oracle/SemanticCorpusOracle.csproj -c Release --no-restore -- --corpus docs/planning/adr-ci1-01-ci1a-s2-0/corpus --expected docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json --output <temporary-owner-oracle-r2.json>
```

Result: `185/185`, zero failures, 146851 output bytes, exact SHA-256
`97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9`.
The frozen expected Git blob and working file both hash to `274A7AAE…`; the
same exact blob digest was verified at `311e97f`, `c75c41f`, `cecb9a8`, and
`ef097aa`. The frozen R2 oracle and corpus were not modified.

## 5. Binding qualifications and exact accepted execution scope

**Q1-Q1 — D3 durable boundary.** Occurrence facts are not stored in segment
records. A resumed run after a full process restart can therefore publish
graph-only results. Do not claim occurrence reconstruction across that boundary.
The inspected `[JsonIgnore]` fields and durable segment checks support this
boundary. Acceptance covers reconstruction of already published occurrences
from authoritative native occurrence tables after the demonstrated known
failures. Occurrence-bearing staging and publication/retry in the qualified
probes occur within the publishing store lifetime (or contributions are
explicitly re-staged there). This is not acceptance of staged-occurrence
persistence across store lifetimes or a full process restart.

**Q1-Q2 — Reconstruction-failure boundary.** A hypothetical failure inside the
recovery rebuild could leave partially restored internal state and mask the
original exception. This is not reachable through the currently accepted
surface. Do not claim stronger failure atomicity or quarantine behavior than
has been demonstrated. Source ordering assigns `_inner` before baseline and
occurrence restoration; there is no accepted injector inside that rebuild.
Tamper-driven open failures demonstrate fail-closed open behavior, not recovery
rebuild failure atomicity. No new quarantine policy is inferred or authorized.

The accepted scope is the serialized native store on Windows x64, the two
existing known-failure recovery paths, accepted published graph/occurrence
projection and publication/coverage correspondence, the demonstrated
contributor/conflict and native-mirror behavior, and unchanged unknown-outcome
quarantine. It excludes broad crash/power-loss/multi-process recovery, MVCC,
cross-platform certification, real-fact publication integration, and any
stronger cross-restart or reconstruction-failure claim.

Existing stage qualifications remain: defining-`compctx2` durability and test
8b are outstanding; real Roslyn fact publication/lifecycle qualification is
outstanding; S-01 component-digest canonicalization remains an S2.5 obligation;
native schedule/platform and other retained obligations are not closed here.

## 6. Owner disposition, next assignment, and stop

**`NEW-01 ACCEPTED WITH QUALIFICATIONS`.** WP-S23-1's bounded recovery
prerequisite is now satisfied by implementation, independent qualification,
and this owner decision, subject to Q1-Q1 and Q1-Q2. This additively records
the disposition of NEW-01 without rewriting the historical obligation register
or earlier acceptance/non-reliance records.

**WP-S23-2 is eligible for the next separately authorized assignment:** the
additive defining-`compctx2` native carrier and reopen enforcement, including
Option A test 8b and its independent `S23-DUR-Q1`/owner gates. It has not been
started or authorized for implementation by this commission. Its missing,
corrupt, unsupported, and changed defining-provenance refusal requirements,
frozen identities, D3/D4/D5 constraints, and Option B escalation remain binding.
No successor package is automatically authorized; S2.3 as a whole is not accepted.

Only this decision is added to canonical `main`. No production/test change,
WP-S23-2 work, branch, additional worktree, package publication, or push is
performed. The decision commit/tree identities and final repository cleanliness
are reported in the handoff, outside this self-containing Git record.
