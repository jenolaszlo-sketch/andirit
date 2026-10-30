# EC6D-QUAL-02 — Independent Re-qualification Report

**Milestone:** EC6D-QUAL-02 (independent adversarial re-qualification of EC6D-REM-01)
**Repository:** `jenolaszlo-sketch/andirit` — canonical checkout `D:\projects\Andirit`
**Reviewer / model:** DeepSeek V4.1 Flash, fresh session (no prior involvement in EC-6D)
**Disposition:** **EC6D-QUAL-02 SUPPORTED WITH QUALIFICATIONS**

> This report issues no owner acceptance and no release authorization.

---

## A. Git binding

Verified in the single canonical checkout, one worktree, `main` clean before and after all work.

| Role | Exact identity | Verified |
| --- | --- | --- |
| Original reviewed implementation | `514cd7938afff14af20fc5e258577ac2ece6641b` (tree `c9a2016d…`) | present |
| Final remediation production source | `701257f4e6f0dbf71ab57072f8204cfa4cd6cad2` | present |
| Production tree | `80d284029f5a82184bb04fd3d0d11fef7b223dc0` | **equals** tree of `701257f4` |
| Final evidence/main commit (HEAD = origin/main) | `503b58152f7a3112c153214c44dd592663722150` | present, `## main...origin/main`, clean |
| Hosted CI for evidence head | run `40991194763`, attempt 1, `head_sha = 503b581`, conclusion **success** | verified via GitHub API |

`git diff --name-only 701257f…503b581` returns **only** `docs/remediation/ec6d-rem-01/*.md` — commits after the production source are evidence-only. No `src`, `tests`, `.github`, `Directory.Build.*`, `.editorconfig` change anywhere in `514cd79…503b581`. No reset, no force-push, no linked worktree.

---

## B. Independence statement

- **Reviewer:** DeepSeek V4.1 Flash in a fresh session. I did not author the implementation, EC6D-IMPL-01, EC6D-QUAL-01 or EC6D-REM-01. The producer record states all of those were authored by the same assistant; this review is a different model in a fresh session and is author-disjoint from the code under review.
- **Evidence provenance (what I executed myself):** baseline suite; the unchanged original qual-01 Probe bundle; the F08 supplement; the committed EC6D-IMPL-01 Qualification harness over authentic p9 seeds (controlled + website); the released Authority and Lifecycle suites against my freshly migrated corrected p10; the P10Upgrade harness against an authentic pre-remediation p10; separate-process reopen; website blob hashing; and a **new, independently authored 66-check adversarial probe** (`indep/`) with its own contract-derived expectations.
- **Limitations:** I executed only on Windows (win-x64, .NET SDK 10.0.401). Linux/macOS coverage is **inspected hosted-CI record only**, not independently exercised. Some harnesses (Probe/Authority/Lifecycle/Qualification/P10Upgrade) are producer-authored; I ran them unmodified and weighed their results against the accepted contracts and my own probes rather than treating green output as ground truth. The two residual probes (R1/R3) are recorded as observations, not as producer-verified facts.
- All review artifacts are isolated under `C:\Windows\Temp\opencode\ec6d-qual-02`. Production source, tests, accepted contracts and all original qualification/producer evidence were not modified. `git diff 514cd79…HEAD -- docs/decisions docs/planning/ec6d docs/validation/ec6d-r16 docs/implementation/ec6d-impl-01` = **0 files**. All five accepted contract blobs match the qual-01 recorded hashes exactly (symbol-binding `cb2b59b0…`, consumer-query `8e0bb0f1…`, publication-consistency `91e4f806…`, acceptance-contract `73b9c6fe…`, scope-proposal `85afde73…`). qual-01 bundle manifest integrity was not re-derived here (see K).

---

## C. Findings matrix (F01–F11)

My disposition is against the accepted contracts, using independent reproductions, not the producer tests.

| F | Disposition | Independent evidence |
| --- | --- | --- |
| **F01** | **Corrected** | `indep` F01-nested-only: nested-only `import` in a namespace → **0 confirmed**, unknown `unresolved-heritage-name`, 0 possible consumers. F01-nested-vs-visible-collision: a top-level **and** a nested same-name import → confirmed only `Outside` via the **visible top-level** import (8-step import witness); the nested owner is `nested-scope-heritage`. Source: `Visible()` requires stored `bindingScope == "module"` (`InterfaceImpactQueryV1.cs:546`), applied in consumer selection (216), bare-unknown (254) and `Bind` (271). Original probe: nested-import confirmed/unknown now pass on both stores. |
| **F02** | **Corrected** | `indep` F02: rehashed B pointing at a non-existent candidate, and a forced B list omitting a retained contributor, both raise `EC-6D integrity failure`. Source (`InterfaceImpactQueryV1.cs:456-469`) recomputes the complete module-scope physical universe from `declaration-context` facts + all export-occurrence local names and compares candidate/ineligible sets, `localName`, count and status. Original `type-value-merge/rehashed-contradictory-attribution` now passes both stores. |
| **F03** | **Corrected** | `indep` F03-aliased-extends: valid `import type { Widget as Local }` + `interface Child extends Local` → normal result, unknown `heritage-kind-not-supported`, no crash; `Correct implements Local` still confirms. Source: kind gate now `heritageKind != class-implements && writtenForm == simple` (`:279`) instead of `_associatedNames.Contains(name)`. Damaged-implements facts still fail closed (remediation test). Original `alias-extends/query` now passes both stores (and the previously aborting +7 checks are reached). |
| **F04** | **Corrected** | `indep` F04-generic: `implements Widget<string>` → `unsupported-heritage-form`, with an expression context carrying `terminalIdentifier = "Widget"`. F04-anonymous: `export const I = class implements Widget {}` now emits an `ownerKind/ heritageKind = other` source-grounded context (no physical declaration / no binding authority) and yields `nested-scope-heritage`; no confirmation. Source: unmodeled-owner anchor (`EcmaScriptInterfaceFacts.cs:74-83`) and generic leaf terminal (`:205`). Original generic-heritage + anonymous-owner observable checks pass both stores. |
| **F05** | **Corrected** | `indep` F05 via preserved probe: `import type Local = require('./types')` records `isTypeOnly = true` (was false) and stays unsupported. Source: `wholeType` computed before the import-equals branch (`:101-107`). Original `type-only-equals/type-only` passes both stores. |
| **F06** | **Corrected** | `indep` F06: removing the profile edge **and** mutating the revision marker are `ArgumentException` at both `maxCandidateScans = 100000` and `= 1`; a valid tiny budget still returns a partial result with full target metadata. Source: `ValidateInput` runs before `Run` (`InterfaceImpactQueryV1.cs:51,70-88`). Original `alias-positive/unsupported-publication/budget-1` passes both stores. |
| **F07** | **Corrected** | `indep` F07: `FromJson`/`RequireConsistentSnapshot` reject a consistent-looking envelope when `RepositoryId`, `IndexRunId`, `IndexIdentity`, `Target` or `ChargedWork` is absent (no NRE — `||` short-circuits before `Publication.Qualification`); valid round-trip preserved. Source: `ValidateEnvelope` (`InterfaceImpactResultV1.cs:63-73`) called from `FromJson`, `ToJson`, `RequireConsistentSnapshot`. Original `transport-identity` passes both stores. |
| **F08** | **Corrected under approved owner supplement** | `indep` F08-triple (`implements Widget, Alias, Widget`): one entity `Multi`, 3 witnesses with routes `import, same-file, same-file`, exact step chains, per-witness `HeritageSpan == Steps[0].Evidence.Location`, entry summary = canonical first proof (by span→route→witnessId), accepted `binding1` hash tuple unchanged, JSON round-trip + repeat stable. Preserved supplement `f08_supplement.py` = **15/15** on my fresh output. The **two** original all-witness route-evidence assertions remain failing (memory+lattice) by explicit owner supersession — not silently re-passed. |
| **F09** | **Corrected** | Bare occurrences now emit only `unresolved-module/external-module/missing-export/re-export-barrier`; other attribution outcomes are counted in the existing `notEvaluated` frontier. Source: `:260-262` + frontier text (`:533-534`). Confirmed independently: Lifecycle `introduce-merge` yields `ambiguous-export` as **heritage** subject (×4) plus occurrence `re-export-barrier`/`unresolved-module`; original `occurrence-ambiguous-export/reason-subject` passes both stores. |
| **F10** | **Corrected** | Expected resolver version derives from the bound publication's unit origin + accepted `resolver2.relative-v1`, not the evidence's own value (`ResolverVersion`, `:555`); status policy and resolves-to provider/version/policy/rule are independently checked (`:395-399, 424-428`). `indep`: policy mutation → `Mismatched resolver policy`; resolves-to version mutation → `Resolver proof has mismatched provider/version or policy`. Independent Authority run = **40/0** (was 40/2). |
| **F11** | **Corrected** | `indep` F11: with 20 repeated export occurrences, `maxWitnessSteps = 1..12` each report `ChargedWork.WitnessSteps == limit` exactly and **no** complete proof beyond it; limit 1 reports 1 charged step (original code reported 0 after full construction); deterministic repeat. Source: `ChargedStep` and pre-materialization `Charge(4,…)` (`:324, 334, 338, 342, 344`); `UnknownView` charges contributions before dedup (`:495-497`). |

No finding was dismissed as a probe defect. All eleven have a genuine, reviewable correction.

---

## D. Original probes (unchanged bundle, fresh output)

Executed the preserved qual-01 Probe against the current canonical source into a fresh directory.

| Test set | Before (preserved) | After (my run) |
| --- | --- | --- |
| Main adversarial probe | 633 checks / **24** failures | **640 checks / 2 failures** |
| F08 approved supplement | n/a | **15 / 15 passing** |
| Authority | 40 / 2 | **40 / 0** |
| Lifecycle | 12 / 2 | **12 / 0** |

Exact assertion delta (computed from the preserved baseline `checks.json` vs my fresh one): **+7 newly reached checks, all `adversarial/alias-extends/{memory,lattice}/*` (previously unreachable after the F03 exception); 22 of 24 baseline failures now pass; 0 baseline checks disappeared; 0 new failures.** The 2 remaining failures are exactly:

- `adversarial/mixed-proof-routes/memory/route-evidence`
- `adversarial/mixed-proof-routes/lattice/route-evidence`

Both are the entry-level "route agrees with every witness" summary assertion, retained and still executed, and explicitly owner-superseded. Mapping of the fixed assertions to findings: F01 (nested-import ×4), F02 (type-value-merge ×2), F03 (alias-extends ×2), F04 (generic-heritage ×4, anonymous-owner ×2), F05 (type-only-equals ×2), F06 (unsupported-publication/budget-1 ×2), F07 (transport-identity ×2), F09 (occurrence-ambiguous-export ×2). F10 is reproduced in Authority; F11 is control-flow/accounting and is covered by independent budget probes.

F08 supplement — all six required properties independently re-checked: (1) per-witness route and heritage span from stored steps; (2) legacy entry fields summarize the canonical first proof; (3) every complete proof present; (4) accepted witness hash inputs unchanged; (5) deterministic ordering independent of enumeration; (6) serialization and both stores agree. Verified both by the supplement and by my independent triple-clause case.

---

## E. New adversarial tests (independently authored)

Project `C:\Windows\Temp\opencode\ec6d-qual-02\indep` (console app referencing the canonical `Andirit.*` projects), output `indep-output/checks.json`. **66 checks, 0 failures.**

| Probe | Expected (from contracts) | Observed |
| --- | --- | --- |
| F01 nested-vs-visible collision (same local name) | bind only the visible top-level import; nested owner = nested-scope | confirmed `Outside` (import route); `nested-scope-heritage` |
| F01 nested-only | no confirmation, `unresolved-heritage-name`, no possible consumer | as expected |
| A03 alias collision (two files define `Widget`) | only same-file `ViaSelf`; no unknowns; no possible consumers | as expected |
| Boundary two interfaces, no contamination | `Widget`→only `A`; `Other` not in result | as expected |
| F04 generic named implements | `unsupported-heritage-form`, terminal `Widget` observable | as expected |
| F04 anonymous class expression | `other`-kind context, closed reason, no confirmation | `nested-scope-heritage` |
| F03 aliased extends + direct implements | unsupported kind + `Correct` confirmed | as expected |
| Valid `extends Base implements Widget` | `Mixed` confirmed, no unrelated unknown | as expected |
| F08 triple mixed clauses | 1 entity, routes {import, same-file×2}, hashes, summary, round-trip | as expected |
| F11 budgets 1..12 with 20 export occurrences | charged ≤ bound, no proof beyond bound | charged == bound each time; 0 confirmed; deterministic |
| F10 policy mutation / edge version mutation | integrity refusal | `Mismatched resolver policy` / `Resolver proof has mismatched provider/version or policy` |
| F06 profile removal / marker mutation at tiny+default budget | `ArgumentException` | as expected |
| F02 extra candidate / dropped declaration-context | integrity refusal | `Attribution candidate context is missing or contradictory` |
| F06 valid tiny budget | partial result still names target | as expected |
| F07 each missing identity field / Target / ChargedWork | rejected before authority accessor | `ArgumentException` |
| Controlled Renderable corpus | exact 4 confirmed, exact 5 possible, expected unknowns, negatives absent | as expected |

Reproduce:

```powershell
dotnet run --project "C:\Windows\Temp\opencode\ec6d-qual-02\indep\Indep.csproj" -c Release -- "C:\Windows\Temp\opencode\ec6d-qual-02\indep-output"
```

---

## F. Contract acceptance A01–A28

Evidence keys: **P** = unchanged qual-01 adversarial probe (all Q2/R2 fixtures, both stores), **I** = my independent probe, **C** = my controlled-corpus run, **W** = my website migration, **A** = Authority, **L** = Lifecycle, **U** = P10Upgrade.

| Row | Disposition | Basis |
| --- | --- | --- |
| A01 | Supported | P (SvgRenderer/HtmlRenderer), I (F01 contrast) |
| A02 | Supported | P (alias-positive, export-alias), I (F03) |
| A03 | Supported | P (merged/nested-export), I (alias collision, boundary) |
| A04 / A04b | Supported | P (wrong-export, absent-export) |
| A05 | Supported | P (collision, unsupported-before-conflict) |
| A06 | Supported | C + witness p8 evidence checks |
| A07 | Supported | C (exactly 4; OtherRenderer/CanvasRenderer absent) |
| A08 / A09 | Supported | C (5 modules; idle import only possible); nested import supplies no possible proof (I) |
| A10 / A10b | Supported | P (barrel-* including multi-witness barriers; barrier-scope) |
| A11 | Supported | P (unresolved alias/orphan coverage) |
| A12 | Supported | P (type-only named/inline positive; import-equals/unsupported), I (F06/F10 integrity) |
| A13 | Supported **with owner F08 exception** | C/P witness membership+hash+provenance; I triple-mixed; entry-level single-route summary superseded only for mixed routes |
| A14 | Supported | P repeats; I repeat + reversed-enumeration (via producer test); W reopen |
| A15 | Supported | U (old p10 → refuse → reindex → correct), L (target edit/merge/delete), C/W migration |
| A16 | Supported | separate-process reopen byte-equal (controlled + website); C/W migration both-store |
| A17 | Supported | P truth table + I F07 malformed guards |
| A18 / A18b | Supported | A (movement/cancel/store-failure/stale view), L (retention) |
| A19 | Supported | authentic p9 seeds verified; 64/23/99 and 161/83/313 old facts **0 changed**; p10 188/451 |
| A20 | Supported | 543 tests; filter `Remediation` = 38; no preset/default/workflow change |
| A21 | Supported | P (nested-shadow/type-param/missing context), I (nested-only) |
| A22 | Supported **with owner F08 exception** | P same-file/same-file-merged; I triple mixed route association |
| A23 | Supported | P duplicate-imports/nested-unrelated |
| A24 | Supported | P merged/type-value/nested-export; I F02 full-universe |
| A25 | Supported | P qualified/expression/extends; I generic value + aliased extends |
| A26 | Supported | P ambient/recovered/ghost; reserved reasons never emitted |
| A27 / A28 | Supported | P barrier-scope/external-package; I boundary isolation |
| Work budgets supplement | Supported | I F11 (charged == bound, no uncharged proof), A bounded counters |
| Closed reason subjects supplement | Supported | I + L (`ambiguous-export` heritage-only) |
| Input/provider integrity supplement | Supported | I F06/F10, A, U |
| Result authority supplement | Supported | I F07, P truth table |
| Coverage/zero-result supplement | Supported | C/W frontier text + disclaimer present in every result |

Authorized exception: A13/A22 entry-level route summary for the mixed case only, per `docs/remediation/ec6d-rem-01/owner-decision-f08.md` ("Approve the bounded F08 supplement"). No other contract text is amended.

---

## G. Durability

- **Authentic p9:** assembly SHA-256 match (`e2e01424…`, `c523dd02…`, `4e5111d5…`); seed plugins end `.p9`, `queryV1Present=false`. No p10 data relabeled.
- **Migration (my run):** controlled preserved 64 nodes / 23 declarations / 99 edges (0 changed), p10 total 188; website preserved 161 / 83 / 313 (0 changed), p10 total 451. Repeat indexing, in-memory replay, Lattice close/reopen, failed/cancelled retention and deletion all passed on the committed harness for 4 and 10 interfaces.
- **Pre-remediation p10:** authentic old p10 (`lifecycle-controlled-02/p10.json`) is **refused** (`ArgumentException`) until normal reindex; after reindex the exact four implementations return and durable reopen matches — memory and lattice (U).
- **Separate-process reopen:** `reopen-controlled-{1,2}.json` and `reopen-website-{1,2}.json` are **byte-identical** per corpus.
- **Witness reconstruction:** controlled witness ids reproduced and each step's `FactId` verified present in the stored snapshot; heritage steps cite their own `.abi14.p8`.
- **No persisted binding authority:** no `binding1.local-v1` / `binds-to` / `implements-resolved` edge in either p10 publication.

---

## H. Website and controlled corpus

- **Controlled (`src/contracts/Renderable.ts`):** exactly `AbstractRenderer, AliasRenderer, HtmlRenderer, SvgRenderer`; exactly the five accepted possible modules (`Preview`, and the four renderer files); unknowns `re-export-barrier` + `unresolved-module`; `OtherRenderer`, `CanvasRenderer`, `BarrelPreview`, `MissingImport` not confirmed. Witness/p8 checks pass. Reproduced by the preserved probe, my independent probe, and my fresh migration.
- **Website @ `f9cca177…`:** 78 files hashed via `git ls-tree` + SHA-1 blob hashing — **0 mismatches, 22 `.astro`**. My fresh p9→rem1.p10 migration queried all **10** interfaces on both stores: **0 confirmations** everywhere; `ProjectRef` = `heritage-kind-not-supported` (incoming extension); `NavItem` and `FounderProfile` each have the single module-level possible consumer `src/lib/schema.ts` (module-level predicate, not name-specific import use); all others 0 possible. Module-level possible-consumer classification was challenged separately from name-specific import usage and behaves per the accepted §3.2 predicate. No positive interface implementation exists in the pinned website, so the website does not independently validate positive binding — the controlled corpus and adversarial contrasts do.

---

## I. Regression and CI

Independently executed (Windows):

- `dotnet test Andirit.slnx -c Release` → **543 passed, 0 failed/skipped**: runtime 312, CSharp 47, TreeSitter 157, LatticeDB 27.
- `--filter FullyQualifiedName~Remediation` → **38 passed**.
- Coverage thresholds in `.github/workflows/ci.yml` are unchanged and untouched by the remediation: core 55, CSharp 75, TreeSitter 60, LatticeDB 70 (line+branch).

Reviewed (not independently re-run) hosted record:

- Run **40991194763**, attempt 1, `head_sha = 503b581`, **success** — jobs `pack` (ubuntu), `test (windows-latest)`, `test (ubuntu-latest)`, `test (macos-latest)`; each test job runs `dotnet format --verify-no-changes` then core/CSharp/TreeSitter/LatticeDb coverage steps. This is the exact evidence head and is self-consistent with the docs-only descendants of `701257f4`.

I did **not** independently execute Linux, macOS or the package/native-consumer smoke job; only their hosted records were inspected.

---

## J. Remaining findings (non-blocking)

Both residuals are **publication-forgery trust boundaries**, not regressions of the corrected ordinary behaviour. They require an actor who can author a publication with correctly recomputed content hashes. The accepted compatibility contract explicitly bounds integrity to internal consistency and states the result boundary does not cryptographically authenticate supplied proof data (`compatibility.md` §"FromJson validates the mandatory envelope… does not cryptographically authenticate").

1. **R1 — `bindingScope` payload flip (severity: Low / informational).** A self-consistent rehash of a nested `imports-name` fact setting `bindingScope="module"` makes the nested import visible and **confirms** an outside class via the import route (observed: `confirmed:["Outside"], route:["import"]`, baseline 0). Repro: `indep` `R1-scope-flag-flip`. There is no source-independent cross-check for lexical scope (unlike F10's package-origin derivation), so the query trusts the stored flag. Ordinary extraction correctly emits `nested`, so the F01 defect is genuinely fixed; this only matters if the publication itself is untrusted.

2. **R2/R3 — wholesale contributor removal (severity: Low / informational).** Removing a retained same-name physical declaration **and** its `declaration-context` alone is caught (`Attribution candidate context is missing or contradictory`). Removing both **and** rehashing B to a self-consistent attributed record confirms (observed `["Ambiguous"]`). This is the same "publication is the database of record" boundary and does not contradict F02, which only required that a *retained* contributor cannot be hidden by a rehashed B (verified satisfied).

No P1/P2 semantic defect was found in ordinary valid-source behaviour. No valid-source crash was found near F03/F04 (aliased extends, generic, anonymous, extends+implements all classified without throwing).

---

## K. Qualifications

- Independent execution was **Windows x64 only**. Linux/macOS/package behavior is hosted-CI record inspection, not independent execution.
- The original qual-01 bundle manifest (2,130 entries) was **not** re-hashed by me; I verified the original probe/bundle outputs are internally consistent with the producer's recorded baseline and reproduced the after-state independently. Contract blobs and protected authority paths were verified unchanged against Git.
- No representative scale/performance, load/concurrency soak, crash/power-loss durability, remote-provider freshness, or production deployment qualification was performed or is established by this milestone.
- The F08 exception is a real, still-failing original assertion, retained and explicitly owner-superseded; a consumer that reads only `Implementation.BindingRoute`/`HeritageSpan` for a mixed-route class gets the canonical first proof, not every route (per-witness route lives on `Witness.BindingRoute`).
- Hosted CI was verified, not re-run. The package-consumer job is smoke evidence, not release authorization.
- This is model-independent but **not** organizationally independent human review. Owner acceptance and release authorization remain separate.

---

## Final disposition

**EC6D-QUAL-02 SUPPORTED WITH QUALIFICATIONS**

The original eleven findings are genuinely corrected: every one of the 24 baseline probe failures attributable to F01–F09 is fixed (22 now pass; 2 remain only as the explicitly owner-superseded F08 entry-summary assertions), F10 and F11 are verified independently (Authority 40/0; charged-work bound probes), the accepted contracts are untouched, the controlled and pinned-website corpora reproduce exactly, authentic p9 facts are preserved, old p10 is refused until reindex, and 66 fresh adversarial checks reveal no blocking correctness defect. The qualifications are the two inherent publication-forgery trust boundaries (J), the Windows-only independent execution, and the recorded operational/performance gaps. Control is returned to the owner; no acceptance or release is granted.
