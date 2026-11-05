# ANDIRIT-CI1A-S2.2-WP7-IMPL-01 — Identity stability, comparison profiles, and stale-evidence refusal

**Commission:** `ANDIRIT-CI1A-S2.2-WP7-IMPL-01` (bounded WP7 implementation and integration validation).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Baseline:** `2b35cd6c7577508b04e75b5bf34d23cbc261f308` (tree `c3706eb919d1455bd12746b22336d956e3e0c8ae`), clean tree, one worktree, **7 ahead / 0 behind**, nothing pushed — verified before any change.

**Disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`** (no production change required; WP7 obligations are satisfied by existing WP3–WP6 production code and are now independently grounded by WP7-specific producer tests).

---

## 1. Baseline commit and tree

As above. Full authority chain re-resolved from Git: `e0d9110`, `812584d`, `4fdc23c`, `d3da2e0`, `97c88e5`, `f1e7f27`, `6b45bde`, `109b200`, `2b35cd6`. No concurrent writer; no superseding WP7 decision exists.

## 2. Authoritative WP7 contract reconstruction

| Requirement | Authoritative source | Required behavior |
| --- | --- | --- |
| WP7 scope | `scope-and-entry-criteria.md` §6.2 | Option A invalidation/refusal: transitive-only `compctx2` change invalidates facts; stale publication refused; no churn; durable association per P10; gate = §5.2 tests 2, 6, 8 |
| Test 2 (E2) | `adr-ci1-01-ci1a-s2-2-target-identity-decision.md` §5.2(2); mapping-contract §7 | Transitive-only change → `asmctx3` stable, `compctx2` changes at all levels, stale facts refused |
| Test 6 (T07) | decision §5.2(6); foundation-owner record §4 | Equivalent reference artifact → identity equality **per the accepted reference contract**, within a declared profile |
| Test 8 (T09) | decision §5.2(8); mapping-contract §7 | Stale publication under a changed context refused (in-memory portion); native reopen → S2.3 |
| Identity vs authority | decision §5.1(1)–(7) | `asmctx3` over the defining `compctx1`; `compctx2` (incl. transitive contexts) is provenance validity; a bare `asmctx3` is not provenance; dependency-context changes alone do not re-identify an unchanged declaration |
| Comparison profiles | foundation-owner record §4; mapping-contract §2 | Two declared profiles: (1) production capture — `file|<actual normalized path>|<lowercase byte hash>`, exact path/byte identity, actual paths preserved; (2) frozen corpus fixture — three labelled items over runtime 10.0.12 bytes, anchors apply only under the complete fixture profile; cross-profile equality is **not** supported |
| Durability (P10) | entry-acceptance owner §3.5 | S2.2 enforces the association at extraction/publication time; native persistence/reopen enforcement is deferred to S2.3 (first gate: reopen stale refusal) |
| Authority continuity | G-CONFIRM/WP3/G-MINT/WP4/WP5/WP6 decisions | Exact live-instance binding, recorded `compctx2` validation, fail-closed minting, trusted `usage4`, ambiguity refusal, coverage integrity, frozen formulas |

## 3. Implemented versus already-existing behavior

**No production change was necessary or made.** The traceability classification:

| Requirement | Classification | Existing implementation |
| --- | --- | --- |
| Test 2 identity stability | Already implemented, needs qualification | `VerifiedCSharpCompilation.Create` (compctx2 with `compilation-context|<dep compctx2>` items) + `MapR2FaithfulCompctx1` (substitutes the direct dependency's R2 legacy item, omitting the child's references/transitive deps) + `ResolveDefining` equality check |
| Test 2 stale refusal | Already implemented, needs qualification | `ResolveDefining` → `StaleContext`; `MintTarget` requires non-null recorded context |
| Test 6 profile equality | Already implemented, needs qualification | `VerifiedMetadataReference.Capture` produces `file|<normalized actual path>|<sha256 bytes>`; descriptor carries it verbatim |
| Test 6 cross-profile boundary | Contractual exclusion (no implementation) | Profiles are distinct; no normalization exists |
| Test 8 in-memory publication staleness | Already implemented, needs qualification | `InMemoryCodeGraphStore.FindOccurrencesAsync` → `StalePublication` for `ExpectedPublication` mismatch and occurrence-index/publication mismatch |
| Test 8 stale context / substitution | Already implemented, needs qualification | `MintTarget`/`MintUsage` statuses (`StaleContext`, `MissingRecordedContext`, `Incomplete`, `MissingDefiningCompilation`) |
| WP7 coverage provenance | Already implemented, needs regression | WP6 behavior unchanged; WP7 exercises no `Merge` |
| Native reopen (test 8 second half) | **Outside WP7** | S2.3, per the accepted S2.2/S2.3 split |
| WP8/WP9 corpus/integration | **Outside WP7** | Later WPs |

## 4. Contract-to-code traceability

| Rule | Code |
| --- | --- |
| Defining R2 fields + direct-dependency legacy projection → `compctx1` | `VerifiedCSharpCompilation.MapR2FaithfulCompctx1` (`R2LegacyProjectReference`) |
| Verified context incl. transitive dependency contexts → `compctx2` | `VerifiedCSharpCompilation.Create` (`referenceIdentities`: `compilation-context|<dep compctx2>`) |
| Recorded context equality gate | `VerifiedCSharpUniverse.ResolveDefining` (`StaleContext`) |
| Non-null recorded context precondition | `VerifiedCSharpUniverse.MintTarget` (`MissingRecordedContext`) |
| Complete declared universe precondition | `ResolveDefining` (`Incomplete`) |
| Frozen `asmctx3` from mapped `compctx1` | `CodeOccurrenceIdentity.ComputeTargetId` |
| Trusted `usage4` only after mint | `VerifiedCSharpUniverse.MintUsage` |
| Captured reference identity | `VerifiedMetadataReference.Capture` |
| Publication-bound occurrence answers | `InMemoryCodeGraphStore.FindOccurrencesAsync` |

## 5. Test 2 — transitive-only change results

Four-level graph `App → Lib → Mid → Leaf`; Graph B changes **only** Leaf's source (a transitive dependency of `Lib`).

| Assertion | Result |
| --- | --- |
| `Lib` defining R2 fields unchanged (assembly, metadata, module, options, source canonical forms) | ✅ |
| Mapped `compctx1` (independent implementation of the accepted formula, incl. the legacy project-reference substitution) unchanged | ✅ |
| `asmctx3` unchanged and equal to the independently computed value | ✅ |
| `compctx2` changed at **all** levels (Leaf, Mid, Lib, App) | ✅ |
| The differing Lib item is the `compilation-context|…` project-context; the mapped legacy item is unchanged | ✅ |
| Graph A mint reproducible under Graph A (same id) | ✅ |
| Graph A recorded context refused against Graph B (`StaleContext`, no target) | ✅ |
| Graph B recorded context accepted (`Minted`), same `asmctx3` | ✅ |
| `usage4`: stale context → no fact; current context → fact with independently computed value and the minted target | ✅ |
| Cross-project association (symbol bound through `App`'s live `CompilationReference`, caller-supplied) | ✅ stale refused, fresh minted, A replayed |
| Negative control: `Mid`'s identity **changes** when its direct dependency (Leaf) changes | ✅ (the stability is specific to the transitive dimension, not a blanket rule) |

## 6. Test 6 — comparison-profile matrix

| # | Case | Declared profile | Expected relation | Result |
| --- | --- | --- | --- | --- |
| 1 | Identical artifact captured twice | Production | Equal identity (`file|<path>|<sha256 bytes>`, independently recomputed) | ✅ |
| 2 | Independently constructed equivalent artifacts (separate capture objects) | Production | Equal `compctx1`/`asmctx3` | ✅ |
| 3 | Alternative representation within a profile | — | **Not permitted by the accepted production profile** (exact path+bytes required); no alternative-representation rule invented | recorded |
| 4 | Same bytes, different path | Production | **Not equivalent** (path is identity-bearing); different `compctx1`/`asmctx3` | ✅ |
| 5 | Different PE/metadata identity (platform vs Andirit.Abstractions) | Production | Not equivalent | ✅ |
| 6 | Different dependency context | — | Covered by test 2: stable identity, changed `compctx2` | ✅ |
| 7 | Same source, changed compilation options (Release vs Debug) | Production | Different `OptionsDigest` → different `compctx1`/`asmctx3` | ✅ |
| 8 | Same artifact under distinct declared profiles | Cross-profile | **No equality is claimed.** Production identity contains the actual path/byte hash and structurally differs from the frozen fixture labels (`file|runtime/System.Runtime.dll|…`); production never substitutes fixture labels | ✅ boundary |
| 9 | Equivalent graphs reconstructed independently | Production | Equal identities (see also determinism §12) | ✅ |
| 10 | Deliberately non-equivalent graph | Production | Not equal | ✅ |

Frozen fixture profile: the accepted anchors (`Leaf = compctx1:sha256:404c5e48…`, `Core = compctx1:sha256:942a8e58…`, as recorded with their source-document ellipses) apply only under the complete three-item fixture profile over runtime 10.0.12 bytes; the recorded production-capture anchors on the qualification host (`…b816dcb5…`, `…df8d2932…`) differ. **No cross-profile equality or normalization is claimed.**

## 7. Test 8 — stale publication and stale evidence results

| Case | Result |
| --- | --- |
| Current publication binding (`ExpectedPublication` = latest) | `Available`, complete, hits returned |
| Previous publication after a new publication | **`StalePublication`**, no hits |
| Stale recorded `compctx2` against the current universe | `StaleContext`, no target, no fact |
| Fabricated canonical-shaped `compctx2` | refused (not minted) |
| Missing recorded context | `MissingRecordedContext`, no target |
| Metadata-equivalent substituted live instance (same descriptor digest, different object) | refused; the exact instance in its own universe mints |
| Incomplete declared scope (one uncovered unit) | `Incomplete`, no target |
| Reuse of a previously minted structural identity with stale evidence | no mint — a stable `asmctx3` is not current authority |

## 8. Identity and recorded-context continuity

The accepted distinction is demonstrated: `asmctx3` remains stable across a transitive-only change while the recorded `compctx2` changes and every stale association is refused. A previously minted identifier or a duplicated metadata-equivalent instance does not bypass the current-context equality gate. Exact live-instance binding, `compctx2` validation, fail-closed minting, and `usage4` gating are unchanged (WP3–WP6 suites green).

## 9. WP6 coverage provenance

WP7 exercises no coverage merge. The WP6 suite (20/20) and WP6-Q1 reviewer probes (30/30) are green; G-Q1A2 (mixed-scope universe attribution discarded) and G-Q1A7 (sole-attribution boundary) remain accepted, unchanged behaviors. No new WP7 input shape makes the previously unreachable `Produced`-with-null-universe shape reachable — WP7 does not construct coverage.

## 10. Integration corpus

Real production paths exercised: `VerifiedCSharpCompilation.Create` over four independently built compilations, `VerifiedMetadataReference.Capture` (real PE files), `VerifiedCSharpUniverse.Create`/`ResolveDefining`/`MintTarget`/`MintUsage`, `CodeOccurrenceFact.Create` via minting, and `InMemoryCodeGraphStore` stage/complete/query with publication binding. No mocks, no persistence simulation.

## 11. Negative tests and valid controls

Negative: ambiguous/stale/fabricated/missing contexts, substituted instance, incomplete scope, stale publication, different path/bytes/options. Valid controls: same-profile equivalence, same-scope complete merging (WP6), fresh-context minting, exact-instance minting, current publication query. The suite deliberately avoids "always refuse" (positive controls assert minting and availability).

## 12. Separate-process determinism

Fresh equivalent graphs mint the same identity in separate processes: `WP7-DIGEST|asmctx3=asmctx3:sha256:8af871e8541b106e3f35f8bad3ad5003b4d4b27ca248d647e56e34e20f9140aa|compctx2=compctx2:sha256:4cf4273c716804f67f0a78ac7b4225d747a5b8007f03dd75dcbcfbf7ebd7079a` — **MATCH** across two processes, and equal to the independently computed `asmctx3`.

## 13. Regression and oracle integrity

Windows x64, SDK `10.0.401`, runtime `10.0.12`.

| Suite | Result |
| --- | --- |
| WP7 tests (new) | **12 / 0 / 0** |
| WP6 producer matrix | 20 / 0 / 0 |
| WP6-Q1 probes | 30 / 0 / 0 |
| WP5-Q2 / WP5-Q1 probes | 22 / 0 / 0, 16 / 0 / 0 |
| WP4-Q1 / G-MINT-Q1 probes | 13 / 0 / 0, 12 / 0 / 0 |
| WP5 / WP4 / WP3 targeted | 14, 20, 16 — green |
| WP5-R1 regressions | 6 / 0 / 0 |
| Whole solution | **780 / 0 / 0** (43 + 157 + 345 + 235; baseline 768 + 12) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

## 14. Platform limitations

All WP7 qualification is Windows x64. Hosted CI runs the solution suites on three OSes only after the next owner-controlled push; reviewer/probe execution for WP7 is local Windows. No Linux/macOS WP7 independent qualification is claimed.

## 15. Remaining obligations

- **WP7-Q1:** independent qualification of tests 2/6/8 and the profile boundary.
- **S2.3:** native persistence/reopen (test 8 second half; first gate = reopen stale-refusal).
- **WP8:** integration/comparison corpus over the accepted S2.0/R2 evidence.
- **WP9 / later:** per scope; not authorized.
- **Cross-platform:** release-stage platform evidence.
- **Publication:** separately controlled; nothing pushed.

## 16. Recommended WP7-Q1 probes

1. Independently derive and confirm stable `asmctx3` under transitive-only changes (including the negative control where a direct dependency changes identity).
2. Confirm changed `compctx2` at every level and rejection of stale recordings, including cross-project bound symbols.
3. Attempt false cross-profile equality (production inventory vs frozen fixture labels) — must fail / be unsupported.
4. Attempt false inequality within the production profile for independently captured equivalent artifacts — must not occur.
5. Reconstruct equivalent reference artifacts independently and compare identities under the accepted profile.
6. Reuse stale facts/contexts at in-memory publication boundaries (expected-publication and recorded-context).
7. Cross-project identity substitution and metadata-equivalent instance substitution.
8. WP6 scope/universe attribution preservation (G-Q1A2/G-Q1A7 unchanged).
9. Separate-process determinism.
10. Frozen authority/oracle integrity (`97F6C5E5…`) and no formula changes.

---

**Final disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** No production change, no WP7-Q1, no owner acceptance, no S2.3/WP8 work, no push.
