# ANDIRIT-CI1A-S2.2-WP8-IMPL-01 — Frozen R2 corpus integration, comparison, and F16 evidence

**Commission:** `ANDIRIT-CI1A-S2.2-WP8-IMPL-01` (bounded integration implementation and evidence).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-06.
**Baseline:** `7cef15aaf45e7c203b28541ed1b24bf27a3bb3b9` (tree `f43d25af2be04d1e439acab6e7b28d0b6fd8d94a`), clean tree, one worktree, **11 ahead / 0 behind**, nothing pushed — verified before any change.

**Disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.**

The real S2.2 production chain was exercised over the accepted frozen S2.0/R2 corpus; all 50 frozen invocation expectations match, the independently enumerated supported fact set matches exactly, negatives refuse, and determinism holds. No production code changed (test harness + one test-project compile include only).

---

## 1. Baseline and commit/tree integrity

Full identities re-resolved: `e0d9110`/`ba9338d7…`, `812584d`/`71675570…`, `4fdc23c`/`55b5e990…`, `d3da2e0`/`029299bf…`, `97c88e5`/`27cda088…`, `f1e7f27`/`794d3a3d…`, `2b35cd6`/`c3706eb9…`, `cdd416d`/`abe0f514…`, `0d4277b`/`aef210b3…`, `42ef163`/`2136a81e…`, `7cef15a`/`f43d25af…`. Parent chain consecutive; no superseding WP8 decision; no concurrent writer.

## 2. Authoritative corpus inventory (authenticated)

- **11 C# source files** verified byte-for-byte against the frozen `fixtureManifest` (SHA-256); **5 projects** (`Corpus.App`, `Corpus.Core`, `Corpus.Leaf`, `Corpus.Solo`, `Corpus.Negatives`); **5 namespaces** independently counted from source.
- Project graph: `App → {Core, Leaf}`, `Core → Leaf`, `Negatives → Core + external image`, `Solo` standalone; `External/ExternalLib.cs` emitted to a captured metadata image (production requires captured PE references).
- **87 frozen semantic entries**: 50 invocation expectations (49 mandatory + 1 qualification explicit-interface), 21 method-group (20 bulk qualification + 1 deferred), 1 constructor, 1 property, 1 field, 1 event-member, 1 delegate, 1 local-function, 1 type-reference, 1 event-handler, 1 resolved-with-missing-provenance negative, and 7 negative categories (ambiguous, genuine-ambiguity, unresolved, dynamic, invalid-invocation, external, inactive).
- Frozen identity values exist under the **frozen fixture profile** (three labelled framework references); production runs under the **production capture profile** (actual captured paths/bytes). Exact cross-profile identity equality is therefore **not** claimed (WP7-OWNER).

## 3. §6.6 quantitative assessment

| Measure | §6.6 minimum | Frozen corpus (verified) |
| --- | ---: | ---: |
| C# source files | 10 | **11** (manifest-authenticated) |
| Invocations | 50 | **53** (frozen counts; 51 supported production facts) |
| Method references | 20 | **21** |
| Overloaded methods | 5 | **5** |
| Namespaces | 3 | **5** (independently counted) |
| Projects | 2 | **5** (independently counted) |
| Cross-project invocations | 5 | **52** |

No minimum is lowered or excluded. Corpus inventory counts are reported separately from supported-extraction counts (below).

## 4. Frozen expected-result schema and comparison method

Sections inspected: `semantics`, `identities`, `variantIdentities`, `variants`, `crossView`, `e02`, `coverage`, `counts`, `lifecycle`, `checks`.

- **`semantics`** (87 entries): profile-independent. Compared directly per site (resolution class, span, target documentation id, assembly).
- **`variants`** (17 relations): compared as **relations** (stable/changed/invalidated), not exact values.
- **`identities`** (`compctx1`/`compctx2`/`asmctx3`/`usage4`): **fixture-profile** values. Not compared cross-profile; the production-profile identities are independently reconstructed with the frozen `CompilationProvenance.Describe` formula over the actual production inputs.
- **`checks`/`counts`**: reproduced by the frozen oracle regression (185/185 below).

Independence: the frozen expected file is never regenerated; production output is never copied into expectations; expectations are computed with the frozen R2 authority compiled into the test assembly, whose `Describe` is an independent implementation (not production mapping code).

## 5. Comparison-profile distinctions

| Profile | Identity basis | Used for |
| --- | --- | --- |
| Production capture | `file|<normalized actual path>|<sha256 bytes>`; actual TPA/external paths preserved | Production `compctx1`/`asmctx3`/`usage4` reconstruction (observed Core `compctx1 = afcb56d0…`) |
| Frozen corpus fixture | three labelled framework items over the accepted runtime bytes | Frozen `identities` values (Core `compctx1 = 942a8e58…`) |

The two digests differ as expected; no normalization or fixture-label substitution is performed. Cross-profile equality is not claimed.

## 6. Actual production-chain integration

The harness (`tests/Andirit.CSharp.Tests/S2_2Wp8CorpusIntegrationTests.cs`) invokes the real internal production APIs: `VerifiedCSharpSource.ReadAsync`, `VerifiedMetadataReference.Capture`, `VerifiedCSharpCompilation.Create`, `VerifiedCSharpUniverse.Create`/`ResolveDefining`/`MintTarget`/`MintUsage`, `VerifiedCSharpOccurrenceExtractor.Extract`, `CodeOccurrenceFact`/`CodeOccurrenceCoverage`. No parallel production implementation; no recorded-context validation bypass.

## 7. Case-by-case comparison results

- **Frozen invocation expectations:** 50 / 50 matched (fact exists at the exact site span; target documentation id equals the frozen `symbolKey` documentation id; explicit-interface and reduced-extension definition ids normalized correctly).
- **Independent fact-set enumeration:** production **51** facts = independently derived **51** supported invocation spans (50 frozen-marked + the unmarked `PartialType.Right()` site, correctly extracted; documented as an accounted extra).
- **Deferred/unsupported categories** (constructor, property, field, event-member, method-group ×21, delegate, local-function, type-reference): **no facts emitted**; the two invocation-shaped unsupported kinds (delegate, local function) are counted as `Unsupported`.
- **Negatives:** no facts; `Ambiguous=1`, `Unresolved=4` (missing type, dynamic, invalid static access, external), `ConditionalInactive=1`, `External=0` (WP5 F4 construction-stage refusal), `facts=0`. `m.Static(1)` (CS0176 invalid invocation) was independently observed to be **refused** (`observed-fact=False`) — consistent with the frozen `InvalidInvocation` negative.
- **Dynamic** is classified `Unresolved` in production (the frozen oracle names the category `Dynamic`); this is the accepted WP5 category mapping (no `Dynamic` counter), recorded as a bounded label difference, not a fact mismatch.
- **External** resolves at Roslyn but has no verified defining compilation → refused (production counter `Unresolved`; `External` unreachable per F4).

## 8. `asmctx3` / `usage4` provenance validation

- S01 (`methods.Direct(1)`, exact span): production `asmctx3 = asmctx3:sha256:ece8e803…`, `usage4 = 1027f062…`; both equal the values independently computed from the frozen formulas over the production inputs (`CompilationProvenance.Describe` → `ComputeTargetId`; frozen occurrence tuple).
- The frozen exact values (fixture profile) differ and are not compared.
- Cross-project targets (Core/Leaf) mint only with their live verified defining compilations; no structural-id elevation.

## 9. Refusal and coverage findings

| Observation | Result |
| --- | --- |
| App baseline | `Produced`, 51 facts, `Ambiguous=0`, `Unresolved=0`, `Unsupported≥2` (delegate + local function) |
| Negatives baseline | `Produced`, 0 facts, `Ambiguous=1`, `Unresolved=4`, `ConditionalInactive=1`, `External=0` |
| Solo | `Produced`, 0 facts, `IsComplete` (verified complete-empty) |
| Mixed-scope merge | `Partial`, unattributed (no false `Produced`) |
| Differing non-null universes | refused (`ArgumentException`) |
| Same-scope complete merge | `Produced` preserved |
| F09 inactive site | counted, never a fact |

No false complete-empty claim and no fabricated fact observed.

## 10. Adversarial integration tests

- Stale baseline `compctx2` against the changed (symbols-leaf) universe → `StaleContext`, no target.
- Substituted metadata-equivalent live instance → refused.
- Declared scope claiming a source that was not compiled → `ScopeSourceMembershipMismatch` refusal.
- Reference-missing variant (`App` without `Core`): S01 unresolved, no fact.
- Source-changed variant: App identity/usage changes; Core target stable.
- Symbols-leaf variant: Leaf identity changes; Core/App `compctx1` stable; `compctx2` changes at Leaf/Core/App; Core target stable.
- Symbols-core variant: Core identity and `ConditionalApi.Value` target change; App source hash unchanged.
- Options variant: material option change alters identities; restore reproduces the baseline.

## 11. F16 nine-part traceability (accepted rows)

The accepted 14 rows (execution-readiness handoff §D) are traced to the production surface and WP8/earlier evidence. **Closure authority remains IR+O (independent review + owner); producer execution does not close any row.**

| # | Requirement | Authority | Production surface | Observable behavior | Positive test | Adversary | Env | Evidence artifact | Closure authority |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Supported encodings + BOM-less refusal (Q-F2) | S21A §F; REG §4A | `VerifiedCSharpSource` | refusal, no identity | corpus sources captured (UTF-8) in WP8 harness | WP1 encoding suite (BOM-less UTF-16/32) | native | `wp8-details-run1.log`, WP1 TRX | IR+O |
| 2 | Eager PE/assembly validation (Q-F3) | S21A §F | `VerifiedMetadataReference.Capture` | throw before identity | 3 framework PEs + external image captured | WP2 truncated/garbage/netmodule tests | native | `wp8-details-run1.log` | IR+O |
| 3 | Symbol canonicalization (A-01) | S21A §F | `VerifiedCSharpCompilation.Create` | `UnsupportedParseConfiguration` | corpus parse options accepted | WP3 symbol-hardening tests | native | WP3 TRX | IR+O |
| 4 | Scope ≠ source inventory (A-02) | S21A §F; S2.1 §3 | `VerifiedCSharpScope`/`Universe` | membership from universe | corpus universe complete | WP8 scope-mismatch refusal | native | `wp8-details-run1.log` | IR+O |
| 5 | R2-faithful `compctx1` mapping | S22ID §5.1(1) | `CompilationProvenanceDescriptor`/mapping | deterministic mapped digest | frozen `Describe` equals production mapping (WP8 corpus) | WP7-Q1 mapping probes | native | `wp8-details-run1.log` | IR+O (G-MINT) |
| 6 | `compctx2` isolation | S22ID §5.1(3)(7) | minting/validation | absent from `asmctx3`/`usage4` | WP8 frozen formula reconstruction | WP4/WP5 negative suites | native | WP4/WP5 TRX | IR+O (G-MINT) |
| 7 | Canonical minting under live authority | S22ID §5.1(2) | extraction + mint | `asmctx3` from verified defining `compctx1` | 51 corpus facts incl. cross-project | stale/substituted/incomplete probes | native | `wp8-details-run1.log` | IR+O |
| 8 | Invocation extraction fidelity | stage2-boundary §3.6–3.7 | extractor | token span, source hash, category | 50/50 frozen sites + span set equality | deferred/unsupported no-fact probes | native | `wp8-details-run1.log` | IR+O |
| 9 | Coverage/refusal classification | stage2-boundary §3.9/§5 | extractor + coverage | correct state; no empty success | App/Negatives/Solo coverage | negative site matrix | native | `wp8-details-run1.log` | IR+O |
| 10 | F09 inactive scanner | REG §3 | extractor | `ConditionalInactive`, no fact | I01 observed inactive=1 | inactive site no-fact | native | `wp8-details-run1.log` | IR+O |
| 11 | Coverage-merge authority (NEW-04) | REG §4; S2.1 §4 | `CodeOccurrenceCoverage` | no mixed-scope `Produced` | same-scope merge Produced | differing-scope/universe probes | native | WP6 matrix + `wp8-details-run1.log` | IR+O |
| 12 | Live defining/source/target correspondence (S-01) | REG §7; S2.1 §6 | verified analysis + extraction | attributable facts | corpus facts match frozen semantics | substituted/stale/missing probes | native | `wp8-details-run1.log` | IR+O |
| 13 | Option A durability association | S22ID §5.1(5) | extraction/coverage model | defining `compctx2` carried; stale refused | corpus stale-context probe | symbols-leaf invalidation | native | `wp8-details-run1.log` | IR+O (S2.3 reopen) |
| 14 | F16/NEW-02 confirmation itself | REG §3; F12A §8 | docs/tests | rows present; matrix confirmed | this matrix + G-CONFIRM record | missing-row gate | — | this report | IR+O |

## 12. Reproducibility and canonical output hashes

Canonical output (deterministic, corpus case/identity/coverage/relation content, production-profile digests, no timestamps/absolute paths beyond the declared profile):

- SHA-256 **`3f08abc616858224b78a9b08a125e167374cbeb40c84c145db2b2c8ab4817ff7`**
- Canonical bytes: **12 108**
- Repeated in one process: identical. Separate processes: **DIGEST MATCH** (`wp8-determinism-run2.log`).
- Same-host only: production-profile digests include the host's actual reference paths/bytes; cross-machine byte equality is not claimed (§6.6 requires repeated/separate-process determinism, satisfied here).

## 13. Full regression and oracle results

Windows x64, SDK `10.0.401`, runtime `10.0.12`.

| Suite | Result |
| --- | --- |
| WP8 corpus integration (new) | **9 / 0 / 0** |
| WP7 producer tests | 12 / 0 / 0 |
| WP7-Q1 probes | 19 / 0 / 0 |
| WP6 matrix / WP6-Q1 probes | 20 / 0 / 0, 30 / 0 / 0 |
| WP5-Q2 / WP5-Q1 probes | 22 / 0 / 0, 16 / 0 / 0 |
| WP4-Q1 / G-MINT-Q1 probes | 13 / 0 / 0, 12 / 0 / 0 |
| Whole solution | **789 / 0 / 0** (43 + 157 + 345 + 244; baseline 780 + 9) |
| Frozen R2 oracle | **185/185**; output `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted; expected `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged |
| `dotnet format Andirit.slnx --verify-no-changes` | exit 0 |
| `dotnet build -c Release` | 0 warnings, 0 errors |

## 14. Frozen-file integrity

No modification to the frozen corpus, `oracle-expected-r2.json`, the oracle program, or any accepted decision. `git status` shows only the new test file, the test-project compile include, and the evidence directory. The oracle regression reproduces the accepted output hash.

## 15. Platform limitations

Windows x64 only. No WP8-specific Linux/macOS qualification is claimed; prior hosted CI success is not WP8 cross-platform evidence.

## 16. Remaining qualifications

- Production-profile identities are host-toolchain-bound; exact values are recorded for this environment, with semantics/relations compared independently.
- Frozen fixture-profile identities are not compared cross-profile (WP7-OWNER).
- Dynamic invocations are reported as `Unresolved` (no `Dynamic` counter in accepted WP5).
- `External` counter remains unreachable (WP5 F4); external evidence refuses via construction/provenance gates.
- F09 counts remain heuristic (F2); `Unsupported`/`UnsupportedCategories` may overlap (F3).
- S-01 caller-supplied component digests remain unverified by design.
- Native persistence/reopen (S2.3) and the Option A reopen half of §5.2 test 8 remain outstanding.
- Unmarked corpus invocation `PartialType.Right()` is extracted (51st fact) and accounted; the 50 frozen invocation rows all match.

## 17. Mandatory gaps

None identified for the bounded WP8 scope. All §6.6 minima are met and authenticated; the real production chain was exercised; frozen semantic expectations compare cleanly; negatives refuse; determinism holds. No production remediation is requested.

## 18. Recommended independent WP8-Q1 review targets

1. Frozen oracle independence (no production-derived expectations; frozen files untouched).
2. Real corpus authenticity (manifest hashes, project graph, external image construction).
3. All §6.6 quantitative minima and the 50/51 fact accounting.
4. Attempts to promote unsupported/deferred occurrences into facts.
5. Cross-project target-identity correctness and `usage4` formula checks.
6. False in-profile equality/inequality and any attempted cross-profile comparison.
7. Stale recorded contexts, substituted instances, and scope-mismatch refusals.
8. Coverage completeness claims (App/Negatives/Solo; mixed-scope/universe).
9. F16 row correctness and evidence completeness (no manufactured closure).
10. Separate-process byte-identical determinism and canonical digest reproduction.

---

**Final disposition: `IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION`.** No WP8-Q1, no owner acceptance, no S2.3/WP9 work, no publication, nothing pushed.
