# ADR-CI1-01-CI1A-S2.2-WP7-OWNER-01 — Qualified owner acceptance of WP7 identity stability, comparison profiles, and in-memory stale-publication refusal

**Commission:** `ANDIRIT-CI1A-S2.2-WP7-OWNER-01` (formal owner decision).
**Project:** Andirit. **Recorded:** 2026-11-06. **Canonical branch:** `main`.
**Owner decision:** **`ACCEPTED WITH EXPLICIT QUALIFICATIONS`**.
**Gate/scope:** **WP7 ACCEPTED — Option A §5.2 tests 2/6/8, bounded identity stability, declared-profile comparisons, and in-memory stale-publication refusal.**

> **Status: RECORDED — OWNER-AUTHORIZED.**
> **Owner decision: `ACCEPTED WITH EXPLICIT QUALIFICATIONS`.**
>
> This accepts the WP7 integration evidence for its bounded scope. WP7 added tests and
> evidence only — **no production code changed** — and that is a valid completion
> outcome because the existing WP3–WP6 production capabilities satisfy the accepted
> requirements. This decision does **not** implement or accept WP8, WP9, S2.3
> persistence/reopen, cross-platform reviewer qualification, package publication, or
> complete S2.2, and does not close the deferred native-reopen half of test 8.

This decision is additive documentation. It does not amend any prior accepted decision,
reviewer artifact, producer test, frozen oracle or production source, and it preserves
the WP7-Q1 findings with their original severities.

---

## 1. Owner decision, gate, and scope fields

| Field | Value |
| --- | --- |
| Owner decision | `ACCEPTED WITH EXPLICIT QUALIFICATIONS` |
| Gate/scope | WP7 — Option A §5.2 tests 2, 6, 8 (in-memory), bounded identity stability and profile comparisons (closed) |
| Accepted capability | Existing production identity/minting/publication behavior demonstrated by permanent regressions and independent probes; no new production functionality claimed |
| Excluded | WP8/WP9; S2.3 native persistence/reopen; cross-profile equality; cross-platform reviewer qualification; publication; complete S2.2 |
| Next stage | WP8 — requires a separate commission (entry contract in §8) |

## 2. Verified authority chain

| Role | Commit | Tree |
| --- | --- | --- |
| Frozen R2 foundation | `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22` | `ba9338d74bf59c584bef340dbda49ed46eb1c5d9` |
| G-CONFIRM owner | `812584dc56e11983c3ea7de8977fd1cd74ff0297` | `7167557024e1602c81b5e6b169455c0ad10a5864` |
| WP3 owner | `4fdc23cfd3d760639dd653ad92bb204376a08c05` | `55b5e990cfa16606d7ba09752d18d360bf01db08` |
| G-MINT owner | `d3da2e009154402a92275874ac4bb94426b97df3` | `029299bf277bc99212ea2ded311c0ec54f70fa09` |
| WP4 owner | `97c88e58851820468cd21ee66d7a8ba460baf23a` | `27cda08808b4b56ccd6524b82b8315666636bd8d` |
| WP5 owner | `f1e7f273750b41413621db869542e2415ca24345` | `794d3a3d2f498856cba18ed5633fb41407ee92ca` |
| WP6 owner | `2b35cd6c7577508b04e75b5bf34d23cbc261f308` | `c3706eb919d1455bd12746b22336d956e3e0c8ae` |
| WP7 regression tests | `cdd416d0e92bdb4a56860529b86e7e82fd657b96` | `abe0f5144855e5b1a539259888ac4d39bd84dd9d` |
| WP7 implementation evidence | `0d4277b62fbf7e786771d2d46a736b069bfbf8b3` | `aef210b34cae2fc52e3cb414f7f718cd86ca1b00` |
| WP7-Q1 independent qualification | `42ef16378e86cec757cec5917847906b21f33cd5` | `2136a81e60a1057d08af6998eca1160d55e9f7be` |

Verified at recording: consecutive parent chain; `42ef163` parent = `0d4277b`, tree
`2136a81e…` as commissioned; working tree clean; one worktree; no stashes; `origin/main`
= `443c192`; `main` **10 ahead, 0 behind**; nothing pushed. **No equivalent or
superseding WP7 owner decision exists** (`docs/decisions/` contains no `wp7-owner`
record); this is not a duplicate.

## 3. Owner verification of the qualification evidence

Re-executed at recording HEAD (logs in
`docs/reviews/CI1A-S2.2-WP7-OWNER-01-evidence/`):

| Verification | Result |
| --- | --- |
| WP7-Q1 independent probes | **19 / 0 / 0** |
| WP7 producer tests | **12 / 0 / 0** |
| WP6-Q1 probes | **30 / 0 / 0** |
| WP6 producer matrix | **20 / 0 / 0** |
| WP5-Q2 / WP5-Q1 probes | **22 / 0 / 0**, **16 / 0 / 0** |
| WP4-Q1 / G-MINT-Q1 probes | **13 / 0 / 0**, **12 / 0 / 0** |
| Whole solution | **780 / 0 / 0** (43 + 157 + 345 + 235) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

**Production-free scope verified:** `git diff 2b35cd6..HEAD -- src docs/decisions docs/planning`
is empty; `cdd416d` adds one test file (594 insertions) and modifies no existing producer
test; `42ef163` adds only reviewer probes, report, and evidence. No blocking or
decision-required finding was demonstrated.

## 4. Option A §5.2 dispositions

### Test 2 — transitive-only change: **ACCEPTED**

Independently established (frozen R2 `Describe` as the expected-value oracle, not the
production mapping): a transitive-only **source** change and a transitive-only
**compilation-options** change both preserve `asmctx3`; `compctx2` changes at all four
levels; Graph A's recorded context refuses against Graph B (`StaleContext`); a fresh
context mints the same accepted structural identity; the direct-dependency negative
control changes the dependent identity; cross-project bound-symbol behavior is
consistent; Graph A remains reproducible.

**Recorded explicitly: stable structural identity does not imply current verified
authority.** No claim is made that every dependency change preserves `asmctx3`, or that
`compctx2` changes necessarily leave identity unchanged.

### Test 6 — declared comparison profiles: **ACCEPTED WITH EXPLICIT PROFILE QUALIFICATIONS**

Supported guarantees: within the **production capture profile**, independently captured
equivalent artifacts (same normalized actual path and same PE bytes) produce equal
identities; the capture identity was independently reconstructed as
`file|<normalized actual path>|<lowercase sha256 of bytes>` and cross-checked against the
frozen R2 reference identity; different paths (same bytes), different PE artifacts, and
relevant compilation-option changes produce the required distinctions. The
**frozen-corpus fixture profile** remains a distinct declared profile whose labelled
anchors are profile-specific.

**Not claimed:** universal cross-profile equality; interchangeability of fixture labels
with actual PE identities; cross-machine path normalization; general identity
equivalence outside the accepted profiles. No cross-profile comparison was performed or
claimed.

### Test 8 — stale publication (in-memory): **ACCEPTED for the authorized in-memory portion**

Exercised through the real `InMemoryCodeGraphStore` stage/complete/query path: a stale
caller-held `ExpectedPublication` returns `StalePublication` with no hits (even when the
structural target id is unchanged); current publication evidence is accepted; stale,
missing, fabricated, and substituted recorded contexts refuse minting/usage; incomplete
universes refuse; graph-only repositories return `AnalysisUnavailable` rather than a
false complete-empty result. **Native persistence/reopen remains deferred to S2.3**; the
in-memory result is not proof that stored or restored publication evidence is safe.

## 5. Qualification dispositions

| Finding / obligation | Disposition |
| --- | --- |
| **G-WP7Q1-1** — caller-held publication evidence is the effective in-memory stale boundary; the index/publication-mismatch branch is defensive/restoration-oriented | **ACCEPTED QUALIFICATION.** Demonstrated boundary recorded. Not claimed: every restoration/mismatch path independently exercised. Carried into the S2.3 requirements wherever the specification requires restore-path enforcement; any WP8 publication-consuming path must preserve this boundary. |
| **G-WP7Q1-2** — production and frozen-corpus identities are distinct declared profiles | **ACCEPTED QUALIFICATION.** Actual production PE identities, profile-specific fixture anchors, explicit comparison boundaries, and the no-normalization/no-label-substitution rule are preserved. Any stronger cross-profile guarantee requires separately accepted authority. |
| **G-WP7Q1-3** (additive options-only probe) | **NON-BLOCKING**, accepted as added evidence. |
| **G-WP7Q1-4** (oracle re-reads the PE during derivation) | **NON-BLOCKING**, accepted with the stated assumption (file stable during the probe; production uses the captured identity). |
| **S2.3 native persistence/reopen** (test 8 second half) | **DEFERRED TO S2.3 UNDER ACCEPTED SCOPE** — an uncompleted requirement, not a WP7 defect. |
| **Platform** | **ACCEPTED PLATFORM QUALIFICATION** — WP7-Q1 evidence is Windows x64; no Linux/macOS WP7-specific independent qualification is inferred from hosted CI. |
| **Inherited** | Preserved unchanged: S-01 caller-supplied component digests; G1/F1 cross-project metadata identity; mandatory recorded-`compctx2` validation; WP5 F09 heuristic limits; WP6 G-Q1A2 mixed-scope universe-attribution behavior; WP6 G-Q1A7 bounded sole-attribution reachability; WP6 incompatible-universe refusal. |

## 6. Accepted WP7 capabilities (bounded)

1. Option A §5.2 test 2 transitive-only identity stability (source and options-only).
2. `compctx2` change detection at every level of the verified graph.
3. Refusal of stale recorded contexts (mint and trusted `usage4`).
4. Correct valid-context reminting of the same accepted structural identity.
5. Direct-dependency negative controls (identity changes where required).
6. Cross-project identity behavior within accepted semantics.
7. Option A §5.2 test 6 declared-profile identity equality and required non-equivalences.
8. Profile-specific comparison limitations (no cross-profile equality).
9. Option A §5.2 test 8 in-memory stale-publication refusal.
10. Correct `AnalysisUnavailable` versus complete-empty distinction.
11. Continued WP3–WP6 authority and coverage integrity.
12. Independently reconstructed identity expectations (frozen R2 oracle).
13. Demonstrated separate-process determinism.
14. Permanent integration regressions and independent qualification evidence.

Acceptance means existing production capabilities satisfy the bounded WP7 requirements;
it does not imply new production functionality was added in WP7.

## 7. Explicit non-acceptance

This decision does **not** claim: universal equality across comparison profiles; full
native persistence/reopen validation; complete stale-publication protection for untested
restore paths; cross-platform independent WP7 qualification; universal PE path
normalization; stronger exact-instance identity than previously accepted; independent
verification of S-01 component digests; complete C# occurrence extraction; resolution of
all inherited coverage qualifications; WP8 or WP9 implementation; complete S2.2
acceptance; or package publication/release. The frozen identity contracts and previous
owner decisions remain unchanged.

## 8. WP8 entry contract

**Authoritative scope** (`scope-and-entry-criteria.md` §6.2): *"Integration/comparison
over the accepted S2.0/R2 corpus with frozen expected results; F16 rows; reproducibility
evidence"* — internal gate **"Independent Q1 review"**. Quantitative minima and
reproducibility rules are in §6.6; evidence requirements (actual inputs, self-excluded
manifests, raw logs/TRX/environment, F16 nine-part rows) are in §6.7.

**Numbering discrepancy (recorded):** the execution-readiness handoff uses an older draft
numbering in which the integration/comparison package is "WP9" (its WP6=NEW-04,
WP7=coverage/F09, WP8=Option A). The **owner-accepted §6.2 scope takes precedence**: the
integration/comparison package is **WP8**, and there is no separate WP9 row in the
accepted scope. WP8 work must follow §6.2, not the handoff's numbering.

| ID | Requirement | Authoritative source | Accepted prerequisite | Existing evidence | Remaining work | Acceptance condition | Exclusions |
| --- | --- | --- | --- | --- | --- | --- | --- |
| **WP8-1** | Authority continuity across G-CONFIRM, WP3–WP7 | §6.3 invariants; prior decisions | All accepted | WP3–WP7 suites/probes green | Re-assert in the integration harness | No new authority path from structural ids | Reconstructing trusted facts from identifiers |
| **WP8-2** | Integration/comparison over the accepted S2.0/R2 corpus with frozen expected results | §6.2; §6.6; §6.7; F12 corpus decision (`oracle-expected-r2.json`, 185 checks) | G-CONFIRM (F16/NEW-02 closed) and the frozen corpus/oracle | Frozen corpus fixtures (`docs/planning/adr-ci1-01-ci1a-s2-0/corpus/repo`: App/Core/Leaf/Solo/Negatives/External) and the 185-check oracle; **no test currently consumes the corpus** | Build the WP8 integration/comparison harness over the corpus using the accepted S2.2 production chain (verified compilations → compctx1/compctx2 → asmctx3 → usage4 → coverage), comparing to the frozen expected results as authorized | Byte-identical deterministic output across repeated and separate processes; reviewer reproduction | Inventing corpus size, language coverage, or thresholds beyond §6.6 |
| **WP8-3** | Comparison-profile integrity | §6.2; WP7-OWNER §4/§5 | WP7 profile qualification | WP7-Q1 evidence | Keep production-capture vs frozen-fixture profiles separate in any comparison | No implicit normalization or label substitution | Cross-profile equality |
| **WP8-4** | Identity/authority continuity (`compctx1`/`compctx2`/`asmctx3`/`usage4`, recorded-context validation, stale refusal) | Option A §5; WP4/WP5/WP7 decisions | WP7 | WP7-Q1 | Integration-level assertions only; no formula changes | Stale evidence refused; identities exact | Broadening accepted authority without decision |
| **WP8-5** | Coverage/refusal integrity if coverage is consumed | WP6-OWNER §6–§8; G-Q1A2/G-Q1A7 | WP6 | WP6 matrix + Q1 probes | Preserve mixed-scope/incompatible-universe refusal; no false `Produced` | Coverage claims remain bounded | Reopening NEW-04 |
| **WP8-6** | Evidence and determinism | §6.6/§6.7 | F16/NEW-02 confirmation | Producer regressions; frozen oracle; independent probes | F16 nine-part rows per WP8 case; separate-process byte-identical evidence; independent oracle | Reviewer-reproducible, no self-generated oracle | Treating related production functions as an independent oracle |
| **WP8-7** | Platform/durability boundaries | §6.2 EXCL row; §6.6; WP7-OWNER §5 | WP7 | Windows x64 local; hosted CI solution suites | State WP8-specific platform scope; **do not implement S2.3** | Explicit limitation recorded | Native persistence/reopen; publishing |
| **WP8-8** | Independent qualification | §6.2 gate; W5-10 pattern | — | — | Separate WP8 implementation/evidence commission → independent Q1 review → owner acceptance | Closure only after owner decision | Auto-starting WP8 |

**Explicit exclusions for WP8:** native publication/reopen/lifecycle (S2.3), reader
continuation (S2.4), integrated S2.2 acceptance (`G-ACCEPT`), public API, background/
incremental indexing, Stage 6, and package publication. WP8 does not authorize S2.3 and
does not require it.

## 9. Remaining later-stage obligations

- **S2.3:** native persistence/reopen; first gate = reopen stale-refusal (test 8 second half).
- **WP9 / later:** no separate WP9 package exists in the accepted §6.2 scope; later work is governed by the S2.2 acceptance gate (`G-ACCEPT`: Q-F2, Q-F3, NEW-04, S-01, mapping review) and the S2.3/S2.4 boundary.
- **Cross-platform:** release-stage platform evidence.
- **Publication:** separately controlled; nothing pushed.

## 10. Authorization boundaries

- Publication/release remains separately controlled; this decision does not push (`main` is 10 ahead of `origin/main` at recording).
- WP8, S2.3+, and any cross-profile identity guarantee each require their own commissions.
- The frozen R2 oracle, frozen identity formulas, accepted decisions, and all reviewer artifacts remain untouched by this record.

## 11. Final owner decision

**`ACCEPTED WITH EXPLICIT QUALIFICATIONS`** — WP7 is formally closed for its bounded
identity-stability, declared-profile comparison, and in-memory stale-publication scope.
**WP8 becomes eligible for a separately commissioned implementation/integration-evidence
package** under the entry contract of §8.

---

Recorded at HEAD `42ef16378e86cec757cec5917847906b21f33cd5` (tree
`2136a81e60a1057d08af6998eca1160d55e9f7be`). This record and its evidence are the only
changes in their commit; the decision commit/tree identities are recorded in the handoff.
