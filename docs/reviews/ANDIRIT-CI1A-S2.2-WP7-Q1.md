# ANDIRIT-CI1A-S2.2-WP7-Q1 — Independent identity-stability, comparison-profile, and stale-publication qualification

**Commission:** `ANDIRIT-CI1A-S2.2-WP7-Q1` (independent integration qualification).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Starting HEAD (verified):** `0d4277b62fbf7e786771d2d46a736b069bfbf8b3` (tree `aef210b34cae2fc52e3cb414f7f718cd86ca1b00`), clean, one worktree, **9 ahead / 0 behind**, nothing pushed.
**Under review:** tests `cdd416d` (tree `abe0f514…`), evidence `0d4277b`; no production change since `2b35cd6`.

**Overall disposition: `SUPPORTED WITH QUALIFICATIONS`.**

Option A §5.2 tests 2, 6, and 8 (in-memory portion) are independently supported. No blocking defect was found; no production remediation is required. WP7 is ready for owner acceptance subject to the stated limitations. Owner acceptance is not granted here.

---

## 1. Baseline, ancestry, and tree integrity

Full identities re-resolved: `e0d9110`/`ba9338d7…`, `812584d`/`71675570…`, `4fdc23c`/`55b5e990…`, `d3da2e0`/`029299bf…`, `97c88e5`/`27cda088…`, `f1e7f27`/`794d3a3d…`, `6b45bde`/`bc8b3586…`, `109b200`/`2e0f3a61…`, `2b35cd6`/`c3706eb9…`, `cdd416d`/`abe0f514…`, `0d4277b`/`aef210b3…`. Parent chain consecutive; no superseding WP7 decision; no concurrent writer; no stashes.

**Source integrity (separate from runtime correctness):** `git diff 2b35cd6..0d4277b -- src docs/decisions docs/planning` is **empty** — WP7 added only tests and evidence; no accepted decision, planning contract, or frozen data was rewritten. `cdd416d` is additive (one new test file, 594 insertions, no modifications to existing producer tests).

## 2. Independent WP7 contract reconstruction

| Obligation | Contract | Positive | Negative | Production path | Independent derivation |
| --- | --- | --- | --- | --- | --- |
| Test 2 (E2) | Option A §5.1(2)(4)(6), §5.2(2); mapping-contract §2/§7 | Transitive-only change → `asmctx3` stable; `compctx2` changes at all levels; new context mints | Stale recorded context refused; no trusted `usage4` | `VerifiedCSharpCompilation.Create` / `MapR2FaithfulCompctx1` / `ResolveDefining` / `MintTarget` / `MintUsage` | Frozen R2 `CompilationProvenance.Describe` over identical accepted inputs (independent implementation) |
| Test 6 (T07) | Decision §5.2(6); foundation-owner §4; mapping-contract §2 | Equality within the declared production profile for equivalent artifacts | Inequality for different path/bytes/options; no cross-profile equality | `VerifiedMetadataReference.Capture`; descriptor reference items | Identity recomputed as `file|<normalized actual path>|<sha256 bytes>`; frozen `Describe` cross-check |
| Test 8 (T09, in-memory) | Decision §5.2(8); mapping-contract §7; WP6-OWNER §9 | Current publication accepted | Stale `ExpectedPublication` refused with no hits; stale context cannot mint/emit | `InMemoryCodeGraphStore.FindOccurrencesAsync`; `MintTarget`/`MintUsage` | Contract-derived statuses; real store path |
| Durability (P10) | Entry-acceptance §3.5 | Extraction/publication-time association | Native reopen deferred to S2.3 | — | Boundary recorded, not simulated |
| Coverage provenance | WP6-OWNER §9; G-Q1A2/G-Q1A7 | Same-scope complete merging preserved | Mixed-scope/incompatible-universe containment; F09 non-authoritative | `CodeOccurrenceCoverage` (unchanged) | Direct record-level probes |

## 3. Test 2 — transitive-change matrix (independent)

Graph `App → Lib → Mid → Leaf`; the defining unit is `Lib`.

| Probe | Expected (frozen contract) | Observed |
| --- | --- | --- |
| Leaf source change (transitive) | Frozen `Compctx1(Lib)` stable; equal to production mapping; `asmctx3` identical; `compctx2` differs at Leaf/Mid/Lib/App | ✅ all |
| Leaf **options-only** change (no source change anywhere) | Same: Leaf options change only propagates through `compctx2`; Lib identity stable | ✅ |
| Mid source change (direct dependency of Lib) | Lib's accepted project-reference preimage changes → **identity changes** | ✅ (negative control) |
| Cross-project bound symbol (App's live `CompilationReference`) | Stale refused; fresh mints the same expected identity | ✅ |
| Substituted metadata-equivalent instance | Refused; the exact instance in its own universe mints the same identity | ✅ |
| Unchanged graph, fresh objects | Same frozen `compctx1` and `asmctx3` | ✅ |
| **Stable id vs stale authority** | A and B mint the **same** `asmctx3`, yet A's recorded context must not mint in B | ✅ `StaleContext`, no target |
| Stale `usage4` | No fact emitted; fresh context → fact with the stable target id and independently computed `usage4` | ✅ |

**Acceptance-critical negative established:** a stale recorded context cannot cause a trusted mint or usage merely because the structural target identity is unchanged.

## 4. Direct-dependency and identity-bearing changes

Identity changes when: the defining unit's own accepted fields change; a direct dependency's **tree content or metadata** changes; compilation options change; the reference artifact's actual path or bytes change; assembly identity changes. Identity remains stable when: only transitive/ancestor context changes (`compctx2`), including source-free options-only changes at a transitive level. No blanket rule ("every dependency change changes identity" or "context changes never change identity") is required or observed.

## 5. Test 6 — comparison-profile matrix (independent)

Two declared profiles: **production capture** (`file|<normalized actual path>|<lowercase sha256 of bytes>`, exact path/byte identity, actual paths preserved) and the **frozen corpus fixture** (three labelled items over the accepted runtime bytes; anchors profile-specific, recorded with the source document's ellipses `Leaf = compctx1:sha256:404c5e48…`, `Core = compctx1:sha256:942a8e58…`; host production captures differ, `…b816dcb5…`/`…df8d2932…`).

| Case | Expected relation | Observed |
| --- | --- | --- |
| Capture identity formula | `file|normalize(actual path)|sha256(bytes)`; agrees with the frozen R2 reference identity | ✅ |
| Two independent captures, same path/bytes | Equal identity, equal frozen `compctx1`, equal `asmctx3` | ✅ |
| Same bytes, different path | **Not equivalent** (path is identity-bearing); different `compctx1`/`asmctx3` | ✅ |
| Different PE / assembly identity | Not equivalent | ✅ |
| Changed compilation options | Different `compctx1`/`asmctx3` | ✅ |
| Different dependency context | Covered by test 2 (stable identity, changed `compctx2`) | ✅ |
| Cross-profile | **No comparison claim**; production identity preserves the actual path and never substitutes fixture labels | ✅ boundary |
| Deliberately non-equivalent graph | Inequality | ✅ |

No false in-profile inequality and no cross-profile equality were observed. The frozen fixture profile's exact anchors were not reproduced (fixture inputs are test-only and not present in production); this is the accepted profile boundary, not a defect.

## 6. Test 8 — stale in-memory publication matrix

| Case | Result |
| --- | --- |
| Current publication (`ExpectedPublication` = latest) | `Available`, complete, hits |
| Same structural target id, previous publication | **`StalePublication`**, no hits, `IsComplete=false` |
| New current publication | accepted, updated hit |
| Graph-only repository (no occurrence analysis) | **`AnalysisUnavailable`**, no hits — not a false complete-empty |
| Stale recorded `compctx2` | `StaleContext`, no target/fact |
| Missing recorded context | `MissingRecordedContext`, no target |
| Fabricated canonical-shaped context | refused |
| Substituted metadata-equivalent instance | refused; exact instance mints |
| Incomplete declared scope | `Incomplete`, no target |
| Previously minted structural identity with stale evidence | no mint — identity is not authority |

The real production path (`InMemoryCodeGraphStore` stage/complete/query) was exercised; this is not a test-local simulation. Native persistence/reopen remains **S2.3** and was not claimed.

## 7. Authority and identity continuity

Exact live-instance defining-compilation binding, mandatory non-null recorded `compctx2`, stale-context refusal, `MintTarget`/`MintUsage` gating, and the frozen `asmctx3`/`usage4` formulas all hold; the ambiguous-invocation refusal still emits no trusted fact (WP5 behavior unchanged). Metadata-equivalent targets remain qualified (G1/F1); no stricter identity rule was invented. S-01 caller-supplied digests remain an inherited limitation.

## 8. WP6 coverage provenance

WP7 introduces no new coverage inputs. Probes confirm: mixed-scope merge → `Partial`, unattributed; differing non-null universes refused; same-scope complete merging preserved; refusal counters preserved; F09 heuristic counts remain observations (`InvocationFacts=0`, never completeness signals). G-Q1A2 (mixed-scope universe attribution discarded) and G-Q1A7 (sole-attribution boundary) remain accepted and unchanged; WP7 does not make the previously unreachable `Produced`-with-null-universe shape reachable.

## 9. Separate-process determinism

Canonical probe digest (two separate processes, **MATCH**):

```
WP7Q1-DIGEST|asmctx3=asmctx3:sha256:c981fff06c78cb45ee34db5013b49f06c2cc28d984188539ef3711c4642c3c7a
|compctx1A=compctx1:sha256:b235d1e522317de1a125d5ddddb54149127fe208890bcdc3507acd09c324f1c4
|compctx1B=compctx1:sha256:b235d1e522317de1a125d5ddddb54149127fe208890bcdc3507acd09c324f1c4
|compctx2A=compctx2:sha256:bb07685d9950a04d27ba8785108eaf715e592238224de11c0b6926f4b9ef41a7
|compctx2B=compctx2:sha256:b7a48e79fd58d7e0f948433145364064a3a354ca86abf8b6160af04ad8f4a640
|stale=StaleContext
```

`compctx1` and `asmctx3` are equal across graphs A/B; `compctx2` differs; the stale classification is stable.

## 10. Regression and frozen oracle results

Windows x64, SDK `10.0.401`, runtime `10.0.12`.

| Suite | Result |
| --- | --- |
| WP7-Q1 reviewer probes (new) | **19 / 0 / 0** |
| WP7 producer tests | 12 / 0 / 0 |
| WP6 producer matrix | 20 / 0 / 0 |
| WP6-Q1 probes | 30 / 0 / 0 |
| WP5-Q2 / WP5-Q1 probes | 22 / 0 / 0, 16 / 0 / 0 |
| WP4-Q1 / G-MINT-Q1 probes | 13 / 0 / 0, 12 / 0 / 0 |
| Whole solution | **780 / 0 / 0** (43 + 157 + 345 + 235) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

## 11. Findings and severity

| ID | Severity | Finding |
| --- | --- | --- |
| G-WP7Q1-1 | **QUALIFICATION** | The store advances `_occurrencePublications` with each completed publication whenever occurrence evidence remains, so the "occurrence index bound to a different publication" branch is defensive/restore-oriented; the caller-held `ExpectedPublication` check is the effective WP7 in-memory stale-publication boundary. Consistent with the accepted in-memory portion; reopen remains S2.3. |
| G-WP7Q1-2 | **QUALIFICATION** | Cross-profile equality is unsupported by design; the frozen fixture anchors are profile-specific and were not reproduced (fixture-only inputs). No equality claim is made. |
| G-WP7Q1-3 | **NON-BLOCKING** | Producer test 2 covered source changes; the reviewer added an options-only transitive change (no source change) confirming the context-only propagation path. Additive evidence, not a defect. |
| G-WP7Q1-4 | **NON-BLOCKING** | The independent frozen oracle re-reads the reference PE at derivation time; the probes assume the file is stable during execution. No observed instability; production uses the captured identity, not a re-read. |
| — | **UNSUPPORTED HYPOTHESIS (dismissed)** | "Stable `asmctx3` implies current authority": disproved — same id, stale context refused. |

No BLOCKING or DECISION REQUIRED findings.

## 12. Platform limitations

All reviewer qualification is Windows x64. Hosted CI (three OSes) covers solution suites after the next owner-controlled push; the reviewer probes are Windows-local. No Linux/macOS WP7 independent qualification is claimed.

## 13. Recommended WP7 owner disposition

**Accept WP7 with explicit qualifications.** Tests 2, 6, and 8 (in-memory) are independently supported; no production change is required. Carry forward: G-WP7Q1-1 (effective in-memory stale boundary), G-WP7Q1-2 (no cross-profile equality; fixture anchors profile-specific), the S2.3 native reopen obligation (test 8 second half), and the Windows-only limitation.

## 14. Remaining WP8/WP9 and S2.3 obligations

- **S2.3:** native persistence/reopen; first gate = reopen stale-refusal (test 8's deferred half).
- **WP8:** integration/comparison over the accepted S2.0/R2 corpus with frozen expected results.
- **WP9/later:** per scope; not authorized.
- **Cross-platform:** release-stage evidence.
- **Publication:** separately controlled; nothing pushed.

## 15. Final Git identities and status

Reviewed at `0d4277b` (tree `aef210b3…`). This review adds only `docs/reviews/ANDIRIT-CI1A-S2.2-WP7-Q1.md`, `docs/reviews/CI1A-S2.2-WP7-Q1-tests/`, and `docs/reviews/CI1A-S2.2-WP7-Q1-evidence/`. Production, producer tests, accepted decisions, and frozen data are unchanged. Final review commit/tree and repository status are recorded in the handoff; nothing was pushed.

---

**Final disposition: `SUPPORTED WITH QUALIFICATIONS`.**
