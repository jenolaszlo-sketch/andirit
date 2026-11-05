# ADR-CI1-01-CI1A-S2.2-WP6-OWNER-01 — Qualified owner acceptance of the NEW-04 coverage-merge correction

**Commission:** `ANDIRIT-CI1A-S2.2-WP6-OWNER-01` (formal owner decision).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate/scope:** **WP6 ACCEPTED — NEW-04 coverage-merge correction and bounded coverage integrity.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the corrected `CodeOccurrenceCoverage.Merge` and closes the NEW-04
> obligation under Stage-2 scope §6.9 item 4 for its bounded scope only, after
> independent qualification (WP6-Q1: SUPPORTED WITH QUALIFICATIONS). It does **not**
> implement or accept WP7, WP8, WP9, S2.3 persistence/reopen, cross-platform reviewer
> qualification, package publication/release, or complete S2.2, and does **not** close
> Option A §5.2 test 6 (allocated to WP7).

This decision is additive documentation. It does not amend any prior accepted decision,
reviewer artifact, producer test, frozen oracle or production source, and it preserves
the WP6-Q1 findings with their original severities.

---

## 1. Owner decision, gate, and scope fields

| Field | Value |
| --- | --- |
| Owner decision | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` |
| Gate/scope | WP6 — NEW-04 coverage-merge correction and bounded coverage integrity (closed) |
| Accepted capability | Corrected `CodeOccurrenceCoverage` construction and `Merge` within the declared disjoint-contribution contract, with conservative mixed-scope handling, universe refusal, and P06 false-`Produced` prevention |
| Excluded | WP7/WP8/WP9; S2.3 persistence/reopen; cross-platform reviewer qualification; arbitrary overlapping contributions; cross-universe reconciliation; publication; complete S2.2; Option A §5.2 test 6 |
| Next stage | WP7 — requires a separate commission (entry contract in §8) |

## 2. Verified authority chain

| Role | Commit | Tree |
| --- | --- | --- |
| R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 owner | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 owner | `f1e7f273750b41413621db869542e2415ca24345` | `794d3a3d2f498856cba18ed5633fb41407ee92ca` |
| WP5 evidence repair | `25b5b315ef1afa031f8309ea0215d9082499371a` | `7c4b870d85471b764dac38bce72af90841f837a7` |
| WP6 implementation | `6b45bdecf9d9e49a24b3b001b20bcfca72a10afd` | `bc8b35863eb4b66de35365739f355e8afe807d2a` |
| WP6 implementation evidence | `4fb20878114d8109f8c7ee1839b48f65d522881b` | `5a32bc25e7d47637df9bb7565b65c901c526c8d1` |
| WP6 independent qualification | `109b200028ffe4ee1f3a41be435e1701e58c7147` | `2e0f3a611f139ef8e0c9b7728e002b67aee84685` |

Verified at recording: consecutive parent chain `e0d9110 → … → 6b45bde → 4fb2087 → 109b200`;
`109b200` tree `2e0f3a61…` as commissioned; working tree clean; one worktree; no stashes;
`origin/main` = `64ee765`; `main` **6 ahead, 0 behind**; nothing pushed. **No equivalent or
superseding WP6 owner decision exists** (`docs/decisions/` contains no `wp6-owner`
record); this is not a duplicate.

## 3. WP6 implementation identity

WP6 implementation `6b45bde` changed two files (+375/−8; production +22/−8):

| File | Change |
| --- | --- |
| `src/Andirit.Abstractions/CodeOccurrence.cs` | Constructor invariant unconditional; `Merge` corrected (universe refusal, mixed-scope containment, unit-count sums, doc comment) |
| `tests/Andirit.Tests/S2_2Wp6CoverageMergeTests.cs` | new: 20-test NEW-04 matrix |

Source integrity (distinguishing source verification from regression testing): the diff
`25b5b31..6b45bde -- src/Andirit.Abstractions/CodeOccurrence.cs` contains no occurrence of
`ComputeTargetId`, `ComputeOccurrenceId`, `ComputePayloadHash`, the `Hash` helper,
`asmctx3`, `usage4`, or `compctx2`; the `.editorconfig` scoped `utf-8-bom` rule is intact;
`docs/planning/adr-ci1-01-ci1a-s2-0/oracle` is unchanged. The file SHA-256 necessarily
changed (`78FBD358…` → `A8EF3403A1190CC7C2773C87A8F571A718A8E02CDD0582A096DE11F88B1F2085`);
the frozen **formulas** are semantically unchanged, corroborated by the frozen oracle.

## 4. Owner verification of the qualification evidence

Re-executed by the owner at recording HEAD (logs in
`docs/reviews/CI1A-S2.2-WP6-OWNER-01-evidence/`):

| Verification | Result |
| --- | --- |
| WP6-Q1 independent probes | **30 / 0 / 0** |
| NEW-04 producer matrix | **20 / 0 / 0** |
| Whole solution | **768 / 0 / 0** (43 + 345 + 157 + 223) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| Production unchanged since WP6 implementation | `git diff 6b45bde..HEAD -- src tests` empty |

WP6-Q1's reported WP5-Q2/WP5-Q1/WP4-Q1/G-MINT-Q1 probe results (22/16/13/12) and the
separate-process merge determinism MATCH are accepted as committed reviewer evidence;
the owner re-executed the WP6-specific gates above.

## 5. Formal NEW-04 closure — `NEW-04 CLOSED WITH EXPLICIT QUALIFICATIONS`

| Defect | Evidence | Disposition |
| --- | --- | --- |
| **NEW-04-A Count aggregation** | Unit counts now sum across disjoint contributions; occurrence/refusal counters and `UnavailableReasons` sum; the 20-case matrix and Q1-B use distinct unequal values so max cannot masquerade as sum | **Closed** — for the declared **disjoint-contribution** contract only |
| **NEW-04-B Scope attribution** | Mixed non-null scopes → `ScopeId=null` and state capped at `Partial`; `IsComplete=false`; P06 probes through record, store, and sink paths all refuse completeness | **Closed** |
| **NEW-04-C Universe compatibility** | Differing non-null universes refused in both orders and in multi-step combinations | **Closed** (see G-Q1E1 for the store-level surfacing) |
| **NEW-04-D Declared/verified invariant** | `verified>declared` rejected unconditionally (including `declared=0/verified>0`); `0/0`, `1/0`, `1/1` remain valid; empty-complete preserved | **Closed** |
| **NEW-04-E P06 false completeness** | No permitted combination produced a false `Produced`; positive controls preserve legitimate same-scope complete merges | **Closed** |

The register's "native persistence/reopen if published" clause is **not** satisfied by
in-memory/store-level paths and remains open for S2.3 (publication has not occurred).

## 6. Qualification dispositions

| Finding | Owner disposition |
| --- | --- |
| **G-Q1A2** — mixed-scope merges also discard a **matching** universe attribution | **ACCEPTED QUALIFICATION.** Behavior implemented: `mixedScope ? null : UniverseId ?? other.UniverseId` nulls the universe whenever scopes differ. Behavior described by the producer report §4: only `ScopeId` nulled and state capped. Behavior required by accepted authority (register NEW-04; entry-acceptance probe list; coverage-and-availability): **silent** on the universe under mixed scope. The implemented behavior is strictly conservative — no false claim, no arbitrary first-selection, completeness containment intact — and loses only informational attribution. **Owner does not claim preservation of agreeing universe attribution.** Any future change (preserving an agreeing universe) must be separately commissioned and qualified; it must not silently alter accepted merge behavior. |
| **G-Q1E1** — cross-universe units in one repository make occurrence queries fail closed | **ACCEPTED QUALIFICATION.** The mandated merge refusal surfaces at `RepositoryCoverage`, so `FindOccurrencesAsync` throws rather than fabricating cross-universe coverage. Publication itself succeeds. This is consistent with the contract's "differing-universe merge refused"; the operational limitation (incompatible universe evidence can prevent a query result) is preserved. Any store-side mapping or reconciliation is separately specified/qualified work and is **not** authorized here. |
| **G-Q1A7** — unattributed `Produced` contribution can acquire attributed universe identity | **ACCEPTED QUALIFICATION (reachability verified).** Owner-verified production inventory: the extractor always attributes (`universe.Scope.Identity`/`universe.Identity`); every store/sink construction is `Partial` or `NotProduced` with null or persisted-meta identities; `WithState(Produced)` exists only behind the Lattice `CompleteCoverage` flag, never set true in repository history. No production producer can construct `Produced`-with-null-universe. Reachable only from test fixtures (not production producers). The behavior is therefore a bounded, currently-unreached input domain; recorded and carried forward, not dismissed as globally valid. |
| **G-Q1FLAG** — legacy Lattice `CompleteCoverage` restore branch | **BOUNDED COMPATIBILITY QUALIFICATION.** The flag is only read (default `false`) and `git log -S "CompleteCoverage = true"` is empty across history; the `WithState(Produced)` branch is unreachable for records current code can write. **Not** claimed: exhaustively validated historical/external databases. Persistence/reopen remains an S2.3 obligation. |
| **G-Q1OVF** — overflow conditions fail closed | **NON-BLOCKING.** Tested boundary: `int.MaxValue` unit-count self-merge yields a negative sum rejected by the constructor (`ArgumentOutOfRangeException`). No wraparound acceptance. No unlimited count range is claimed. |

## 7. Accepted WP6 capabilities (bounded)

1. Corrected `CodeOccurrenceCoverage.Merge` within its declared disjoint-contribution contract.
2. Sum-based unit and occurrence/refusal/`UnavailableReasons` aggregation.
3. Scope compatibility and conservative mixed-scope handling (`ScopeId=null`, `Partial` cap).
4. Incompatible non-null universe refusal.
5. Correct `declared=0` behavior and unconditional `verified<=declared` enforcement.
6. Prevention of P06-class false `Produced` outcomes.
7. Preservation of refusal and unavailable-reason evidence.
8. Preservation of legitimate complete merges for compatible disjoint contributions.
9. Bounded F09 heuristic-count aggregation without fact creation.
10. Tested repository coverage, sink, and Lattice restoration behavior.
11. Preserved WP3/WP4/WP5 authority and frozen identity formulas.
12. Deterministic merging within the accepted input profile.

## 8. Explicit non-acceptance

This decision does **not** claim: arbitrary overlapping contributions merge safely (the
contract is disjoint-contribution only); complete cross-universe coverage reconciliation;
preservation of matching universe attribution for mixed-scope inputs; that all
historically possible Lattice records are qualified; exact F09 disabled-invocation counts;
that coverage aggregation creates trusted `usage4` facts; complete C# extraction; closure
of Option A §5.2 test 6; WP7/WP8/WP9 implementation; S2.3 persistence/native reopen
qualification; WP6-specific Linux/macOS independent qualification; complete S2.2
acceptance; or package publication/release. All prior G-CONFIRM/Q2/G-MINT/WP3/WP4/WP5
qualifications, including S-01 caller-supplied component digests and the WP5 F2/F3/F5/F7
limitations, remain in force.

## 9. WP7 entry contract

Authoritative scope (`scope-and-entry-criteria.md` §6.2): *"Option A invalidation/refusal:
transitive-only `compctx2` change invalidates facts; stale publication refused; no churn;
durable association per P10"* — internal gate **"Option A §5.2 tests 2, 6, 8"**. Test
definitions are in `adr-ci1-01-ci1a-s2-2-target-identity-decision.md` §5.2; the
comparison-profile rule is in `mapping-contract.md` §7.

| ID | Requirement | Authoritative source | Prerequisite accepted | Remaining work | Completion condition | Exclusions |
| --- | --- | --- | --- | --- | --- | --- |
| **WP7-1** | Accepted authority continuity (G-CONFIRM, WP3, G-MINT, WP4, WP5, WP6) | this record §7; prior owner decisions | All accepted | Preserve semantics; no formula or merge changes | Authority unchanged; frozen oracle 185/185 | Reopening accepted findings |
| **WP7-2** | Option A §5.2 **test 2**: transitive-only dependency change → `asmctx3` stable, `compctx2` changes at all levels, **stale facts refused** | target-identity decision §5.2(2); scope §6.2 | WP4/WP5 bounded behavior + WP6 identity preservation | WP7 does **not** implement invalidation/refusal; it independently executes the assertions for accepted behavior | No identity churn + stale refusal demonstrated for the accepted profile | Inventing new identity semantics |
| **WP7-2b** | Option A §5.2 **test 6**: equivalent reference artifact → identity equality **per the accepted reference contract** (T07) — the outstanding cross-profile comparison | target-identity decision §5.2(6); mapping-contract §7 | WP4-Q1/WP5-Q2 noted the obligation as open | Independently compare only within a declared profile; never compare different reference profiles as equal inputs | Documented comparison under the named profile with explicit equivalence boundary | Cross-profile equality invention; normalization beyond the reference contract |
| **WP7-2c** | Option A §5.2 **test 8**: stale publication refused under a changed context, including after reopen (T09) | target-identity decision §5.2(8); mapping-contract §7 (S2.2/S2.3 split) | S2.2 pre-mint stale-association refusal exists (W5-6, WP5) | In-memory portion within WP7; **native reopen remains S2.3** | In-memory stale refusal reproduced; reopen obligation recorded for S2.3 | S2.3 persistence/reopen implementation |
| **WP7-3** | Comparison profiles: respect declared profiles and equivalence boundaries | mapping-contract §2/§7 | Profile-scoped comparisons only | Document the exact profile used | Explicit profile identity in evidence | Cross-profile equality |
| **WP7-4** | Coverage provenance: preserve WP6 scope/universe/completeness qualifications in any WP7 consumer | this record §6; coverage-and-availability | WP6 accepted | Do not re-derive or strengthen coverage claims | Merged coverage qualifications honored | Treating coverage as fact authority |
| **WP7-5** | Qualified attribution: carry G-Q1A2 and G-Q1A7 wherever WP7 depends on attributed merged evidence | this record §6 | Bounded behaviors accepted | Treat agreeing-universe attribution as unavailable; do not assume sole-attribution validity for new inputs | No reliance on unaccepted attribution | Silent behavior changes |
| **WP7-6** | Corpus/integration requirements actually established by WP7's specification | scope §6.2; mapping-contract §7 | None invented | Identify only specified assertions (tests 2/6/8) | Requirements traceable to source | Invented corpus/thresholds |
| **WP7-7** | Platform limitations: distinguish hosted CI from independent qualification | prior platform records | Windows x64 reviewer qualification | State WP7-specific reviewer platform | Explicit limitation recorded | Claiming Linux/macOS reviewer qualification |
| **WP7-8** | Independent qualification: separate implementation, independent qualification, owner acceptance | W5-10 pattern; §6.9 | Gate ordering | WP7-Q1 then WP7-OWNER | Closure only after owner decision | Auto-starting WP7 |

## 10. Outstanding cross-profile comparison obligation (Option A §5.2 test 6)

Test 6 (T07, equivalent reference artifact → identity equality per the accepted reference
contract) remains **open** and is now the explicit WP7-2b entry requirement. It is not
closed by WP6, and no cross-profile equality is claimed. Its completion gate is the WP7
independent qualification, within the declared comparison profile only.

## 11. Platform and persistence limitations

- WP6 reviewer qualification is Windows x64 (SDK `10.0.401`, runtime `10.0.12`); hosted CI runs the solution suites on three OSes after the next owner-controlled push, but reviewer probes are Windows-local. No WP6-specific Linux/macOS independent qualification is claimed.
- Native persistence/reopen is **not** qualified. The register's "if published" merge/reopen tests activate only when publication occurs (S2.3); the first S2.3 gate remains a reopen test proving stale defining `compctx2` refuses prior facts.

## 12. Authorization boundaries

- Publication/release remains separately controlled; this decision does not push (main is 6 ahead of `origin/main` at recording).
- WP7, WP8, WP9, S2.3+, and any store-side cross-universe reconciliation or mixed-scope attribution change each require their own commissions.
- The frozen R2 oracle, frozen identity formulas, all accepted decisions, and all reviewer artifacts remain untouched by this record.

## 13. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`** — WP6 is formally closed for its bounded
NEW-04 coverage-merge correction and coverage-integrity scope. **NEW-04 is
`CLOSED WITH EXPLICIT QUALIFICATIONS`** under Stage-2 scope §6.9 item 4. **WP7 becomes
eligible for a separately commissioned implementation** under the entry contract of §9.

---

Recorded at HEAD `109b200028ffe4ee1f3a41be435e1701e58c7147` (tree
`2e0f3a611f139ef8e0c9b7728e002b67aee84685`). This record and its evidence are the only
changes in their commit; the decision commit/tree identities are recorded in the handoff.
