# Findings matrix

Every actionable finding from `CI-1-SCOPE-Q1` is reconstructed below. Findings
the reviewer numbered are `F01`–`F04`; reviewer conclusions that were recorded
in ADR/decision prose rather than as numbered findings are reconstructed as
`S01`–`S05` so that no actionable item is dropped.

Legend for "Predates CI-1?" — the defect exists in the production baseline
`cd2b7fb` independent of any CI-1 work. "Separate production correction?" — a
change to `src/**` or existing tests, which this packet does not authorize.

---

## F01 — P1: A unique documentation ID does not prove the indexed target

| Field | Content |
| --- | --- |
| Reviewer ID / severity | F01 / P1 (potentially blocking correctness defect) |
| Contract or assumption challenged | CI-1 confirmation "requires ... unique canonical target in its project context" (query-contract-proposal.md selection rule); ADR-CI1-04 "keep existing target normalization ... tightens only its new confirmation gate". Assumption that `CanonicalSymbolKey` (Roslyn doc-comment ID) identifies an assembly-qualified target. |
| Reproduction | Two unrelated projects. `A/Shadow.cs` declares `System.Console.WriteLine(string)`; `Z/App.cs` has no reference to `A/Shadow` and calls the platform `System.Console.WriteLine("outside")`. R1 reproduced the reviewer probe byte-for-byte (`reproduction.md` §F01). |
| Independently supported expected behavior | The call in `Z/App.cs` binds to assembly `System.Console, Version=10.0.0.0, PublicKeyToken=b03f5f7f11d50a3a`, which has **no source location** and is external to the index. It must **not** be emitted as a resolved call to the `A/Shadow.cs` declaration. |
| Actual behavior | The plugin emits one `Resolved` call edge from `App.Run()` (`Z/App.cs` 1:40–75) to the local declaration `A/Shadow.cs` 1:49–94, and counts it as `CrossProjectTargets`, not external. There is only one indexed match for the documentation ID, so the run-wide ambiguity guard does not fire. |
| Root cause | `CanonicalSymbolKey` (`CSharpCodeGraphPlugin.Symbols.cs:63`) returns the Roslyn doc-comment ID (`M:System.Console.WriteLine(System.String)`), which omits assembly/compilation identity. `RunSymbols.GlobalNodes` is keyed by that ID (`Session.cs:230`). `TryResolveTarget` (`GraphBuilder.cs:460`) resolves a current-project scoped key first, then the global canonical map, without proving that the bound symbol's **containing assembly/compilation and definition** correspond to the indexed declaration. |
| Predates CI-1? | **Yes.** `Symbols.cs`, `Session.cs` and `GraphBuilder.cs` are unchanged from baseline `cd2b7fb`. No CI-1 code exists. |
| Required planning correction | Define an **exact eligible target identity** gate: confirmation requires a proven mapping from the bound symbol's defining compilation/assembly and definition to the indexed declaration in the applicable dependency context. A canonical documentation ID is a lookup aid, not proof. Specify conservative refusal or explicit external qualification when correspondence cannot be proven. Add the single-indexed-local-versus-external collision to mandatory acceptance (C10 variant). |
| Separate production correction? | **Yes, either** a strictly new-query resolver boundary that never copies current global-key resolution into positive facts, **or** a separately authorized bounded provider fix. The legacy aggregate edge/ID semantics must not be rewritten. See `prerequisite-defects.md` P-DEF-01. |
| Evidence required for independent re-review | The two-project collision input; the emitted call edge and its target declaration location; the independently bound assembly identity of the true target; a passing strict gate that refuses/qualifies the collision while still confirming C03/C19; a legacy regression proving unchanged aggregate IDs. |

---

## F02 — P1: Wider failure is a normal documented-partial-type prerequisite decision

| Field | Content |
| --- | --- |
| Reviewer ID / severity | F02 / P1 |
| Contract or assumption challenged | The packet treats the wider-source failure as an "unidentified exceptional input" and C12 ("simple undocumented partial") as partial-type coverage. Assumption that ad-hoc diagnostic formatting of an inconsistent symbol is safe. |
| Reproduction | Minimal two-file probe `A.cs="public sealed partial class P {}"`, `B.cs` with the same type plus a `/// <summary>`; and the real wider run over `Andirit.Abstractions,Andirit,Andirit.CSharp`. R1 reproduced both (`reproduction.md` §F02). |
| Independently supported expected behavior | Two partial declarations of one type produce one logical node. Declaration-specific documentation must either merge deterministically or be excluded from the node's identity-agreement set, and an inconsistency must be reported, not crash. |
| Actual behavior | `GraphBuilder.AddSymbol` (`GraphBuilder.cs:314`) throws `InvalidCastException: Unable to cast object of type 'Andirit.CodeTextProperty' to type 'Andirit.CodeTextListProperty'` when formatting the inconsistency. The wider real run fails at the same site (`GraphBuilder.cs:314`, reached via `AddDeclarations:155`, `Session.cs:92`). Trigger symbol: `T:Andirit.CSharpCodeGraphPlugin` in `CSharpCodeGraphPlugin.Compilation.cs` (11,1–74,2) and `CSharpCodeGraphPlugin.cs` (12,1–68,2); the only differing property key is `doc-summary`; both carry `modifiers` stored as `CodeTextProperty("sealed")`. |
| Root cause | Two distinct layers: (1) the diagnostic `string.Join` casts `existing.Properties["modifiers"]` and `node.Properties["modifiers"]` to `CodeTextListProperty`, but `CSharpSymbolPropertyFactory.BuildProperties` (`PropertyFactory.cs:59`) stores `modifiers` as `CodeTextProperty`; (2) `NodesEquivalent` (`GraphBuilder.cs:366`) requires `doc-summary` to agree across partial parts, and one part has no documentation comment, so the conflict branch is genuinely reached. Fixing the cast alone exposes the `InvalidOperationException` throw and does not resolve the merge conflict. |
| Predates CI-1? | **Yes.** Both layers are in baseline `cd2b7fb`; no CI-1 code exists. |
| Required planning correction | Name the trigger instead of leaving it unidentified; extend C12 with a documented/sealed partial type; state the honest disposition: the input fails before relationship extraction, so it is a prerequisite for supporting this documented partial input through the existing pipeline, and real-repository qualification must be narrowed until resolved. Do not silently include either fix in CI-1A. |
| Separate production correction? | **Yes.** A two-layer bounded prerequisite proposal (diagnostic cast correctness plus partial-part property agreement/merge policy) with its own authorization gate. See `prerequisite-defects.md` P-DEF-02. |
| Evidence required for independent re-review | The minimal documented-partial probe and the real three-project run both reaching the same site; the declaration-conflict record for `T:Andirit.CSharpCodeGraphPlugin`; a passing test that the documented partial extracts with one logical node; a test that the conflict diagnostic formats correctly when a real inconsistency is present. |

---

## F03 — P2: Raw-byte qualification is overstated

| Field | Content |
| --- | --- |
| Reviewer ID / severity | F03 / P2 |
| Contract or assumption challenged | README claim "Their raw bytes are checked against pinned Git blobs" and `final-handoff.md` "Source-binding evidence verifies all 47 real inputs against the starting Git blobs". |
| Reproduction | `core.autocrlf=true` in the canonical checkout. Four of the 47 canonical inputs are CRLF in the working tree while their committed blobs are LF: `CodeGraphPublicationQuery.cs` (201 CRLF), `CodeGraphQueryService.cs` (1094), `InterfaceImpactQueryV1.cs` (561), `InterfaceImpactResultV1.cs` (106). R1 reran the check (`reproduction.md` §F03). |
| Independently supported expected behavior | A byte-binding check must distinguish the **exact canonical input bytes** (raw SHA-256 of the file as read) from **normalized Git identity** (`git hash-object`, which applies clean filters). Both must be recorded, with the checkout/filter policy. |
| Actual behavior | All 47 recorded `Sha256` values equal the raw working-tree hashes and are reproducible, but for the four files `git hash-object` returns the pinned **blob** hash, not the raw-byte hash. The recorded value is the CRLF raw bytes; the claim that raw bytes equal the blob is false. LF-only Git inputs total 380,982 bytes; the measured canonical mix totals 382,944 bytes; the 1,962-byte delta is exactly the four files' extra CR bytes (201+1094+561+106). |
| Root cause | `measure-baseline.ps1:36–42` records `Sha256` of the working-tree file and separately verifies `git hash-object`, but the packet prose conflates the two. `check-packet.ps1:46–50` only verifies `git hash-object` (clean-filtered), so it never checks raw-byte identity. `.gitattributes` pins exact bytes only for packet `fixtures/**` and `evidence/**`, not for `src/**`. |
| Predates CI-1? | **No** — it is a defect in the CI-1 planning evidence method, not in production. The underlying source files are unmodified. |
| Required planning correction | Require exact source-byte provenance: encoding, line endings, path, raw SHA-256, Git blob, and checkout/filter policy; check both; never normalize fixture bytes and then claim equivalence. Matters directly to proposed source-hash occurrence IDs and serialized source qualifications. |
| Separate production correction? | **No.** Planning-evidence correction only. |
| Evidence required for independent re-review | A binding table that records raw SHA-256 and blob hash separately for all 47 inputs, states `core.autocrlf`, and reconstructs the measured 382,944 bytes; the four CRLF files explicitly identified. |

---

## F04 — P2: The producer's manifest check is not an acceptance oracle

| Field | Content |
| --- | --- |
| Reviewer ID / severity | F04 / P2 |
| Contract or assumption challenged | `check-packet.ps1` "source-selected-site-spans-valid:32" reads as manifest validation, but it validates only start line and column width. |
| Reproduction | The exact producer loop (`check-packet.ps1:29–35`) was mutated with wrong target, wrong overload, duplicate row, wrong start column and wrong end line. R1 reran the mutations (`reproduction.md` §F04). |
| Independently supported expected behavior | A manifest oracle must validate full UTF-16 start/end coordinates, intended semantic target, intended outcome, and row uniqueness; target/owner evidence must be independently recorded. |
| Actual behavior | The producer loop rejects only the wrong **start column**. It accepts wrong target, wrong overload, duplicate row and wrong end line (end line 99). It performs no semantic, outcome or uniqueness check. |
| Root cause | `check-packet.ps1:33` computes `$lines[$s[0]-1].Substring($s[1]-1, $s[3]-$s[1])` — it reads only the start line and the start/end **column width**, ignoring `$s[2]` (end line) and all semantic fields. |
| Predates CI-1? | **No** — CI-1 planning evidence method. The 32 authored rows all pass an independent semantic audit, so this is a checker-strength defect, not a wrong-manifest defect. |
| Required planning correction | Preserve the independently reviewed manifest and its target/owner evidence as an oracle input; require full start/end coordinate and semantic/uniqueness checks for future acceptance. Distinguish lexical manifest validation from candidate-result comparison. Keep the packet's own "author consistency checks; not independent qualification" label. |
| Separate production correction? | **No.** Planning-evidence correction only. No absent candidate implementation has passed any acceptance test. |
| Evidence required for independent re-review | A checker that rejects all five corruptions (R1's independent oracle already does); mutation records; an explicit statement that candidate-result comparison is still unexecuted because no candidate exists. |

---

## S01 — P2: ADR-CI1-01 preference for separate facts is unproven

| Field | Content |
| --- | --- |
| Reviewer source | §4 ADR table + "C versus B2" paragraph + Decision 4. |
| Challenged assumption | ADR-CI1-01 "Recommend C with B2 as a serious simpler alternative" and `occurrence-representation-analysis.md` "Recommend separate occurrence facts". |
| Reviewer-supported position | Separate facts make typed validation/ownership/target queries clearer and avoid a vertex per site, but a new occurrence **edge** also avoids a vertex, can use source-owner/target adjacency, preserves old presets, and already has contributor/replacement infrastructure. Neither demonstrates a cheaper ordered target range or byte-bound behaviour. C adds batch/accumulator/replacement/fact-kind/provenance/store/query/codec/snapshot contracts; B2 adds kind/property validation and risks exposure through unfiltered/native traversal and total graph counts. |
| Required planning correction | Do not claim a new fact family is more correct because it is more explicit. Retain both as qualified alternatives and specify the minimum comparative experiment for the owner. Do not make the storage choice irreversible during remediation. |
| Separate production correction? | No — pending owner decision. |
| Evidence for re-review | `storage-alternatives.md` (requirement-by-requirement comparison, no winner) and the minimum experiment definition. |

---

## S02 — P2: Authentic historical compatibility is not qualified for the new feature

| Field | Content |
| --- | --- |
| Reviewer source | §7 + ADR-CI1-05. |
| Challenged assumption | ADR-CI1-05 treats old-query preservation and reindex behaviour as recommendable without naming authentic prior-writer fixtures. |
| Reviewer-supported position | A fresh schema-2 roundtrip or `HistoricalStorageIdentityTests` label assertion is not an authentic historical-writer fixture. The packet supplies no occurrence codec, candidate reader, historical occurrence fixture or cross-version output (expected for planning). Old-reader refusal must be explicit: adding JSON fields under the same schema can let old readers silently drop data. |
| Required planning correction | Before schema work, name and hash real prior-writer databases/snapshots, writer package/commit and consumer executable; demonstrate old-query preservation and `AnalysisUnavailable`/reindex-required for new queries; preserve Hetu labels and accepted EC6D contracts; define old-reader refusal; never fabricate occurrences from old aggregate edges. |
| Separate production correction? | No — future schema gate; owner decision required. |
| Evidence for re-review | Named/hashed historical fixtures or an explicit statement that they must be produced before codec work. |

---

## S03 — P2: Measurement and economics claims must be separated by context

| Field | Content |
| --- | --- |
| Reviewer source | §2 note, §8, performance-evaluation-plan audit. |
| Challenged assumption | Any reading that pairs producer "stronger oracle" invocation counts with stored call edges as a same-context precision/recall score, or that converts serialized bytes into token/total-cost savings. |
| Reviewer-supported position | The oracle adds seven SDK implicit global usings and is a stronger context; the raw plugin context omits them and ignores accessibility. EnsurePublication 11-vs-1 combines binding-context omissions with aggregate collapse. Timings are single observations, not distributions. Serialized-byte accounting (174,927 bytes sum) is not model context or tokens. No speedup/savings/break-even is established. |
| Required planning correction | Present extractor behaviour, Roslyn oracle behaviour, compilation-context differences, successful 47-file measurements, failed wider extraction, query/source-retrieval costs and unmeasured candidate overhead as separate quantities. Do not claim same-context accuracy where contexts differ. Do not infer token savings from serialized bytes. |
| Separate production correction? | No. |
| Evidence for re-review | Revised performance plan with explicit context separation and a frozen paired protocol; the R1 corrected claims in `corrections.md`. |

---

## S04 — P2: Coverage counters cannot stand in for selected-target unknown sites or persistence

| Field | Content |
| --- | --- |
| Reviewer source | §5, §7, existing-capability-inventory. |
| Challenged assumption | That existing calls/coverage counters can define the new operation's denominator or completeness. |
| Reviewer-supported position | Existing calls counters also include constructors and omit candidate-only attempts; `EdgesEmitted` increments even when `TryAdd` rejects a duplicate. Unit-level unknown counts are not selected-target usages. Extraction coverage is not already an authoritative persisted occurrence-coverage contract. A publication receipt proves read consistency, not source freshness, clean Git, build success, all configurations or runtime coverage. |
| Required planning correction | Persist disjoint semantic outcomes including candidate-only ambiguity and intrinsic/unsupported invocations; define "complete within supported scope" over a bounded explicit unit/context universe; retain the prohibition on `SymbolUnused`/complete-usage claims; make coverage a publication-owned artifact, not a live reconstruction. |
| Separate production correction? | No — new CI-1 contract, not an existing defect. |
| Evidence for re-review | The revised coverage/completeness contract clauses in `corrections.md` and the acceptance reconciliation. |

---

## S05 — P3: Additional prose-only adversaries need executable comparisons once authorized

| Field | Content |
| --- | --- |
| Reviewer source | §5, §6 ("largely prose obligations"). |
| Challenged assumption | That the C01–C20 matrix plus listed adversaries constitute executable acceptance coverage. |
| Reviewer-supported position | Nested/same-line calls, linked projects, canonical collisions, explicit interface implementations, inaccessible/error signatures, shadowed local owners, partial methods, contributor conflicts and proof truncation need executable result comparisons once a candidate exists. The 32 selected lexemes are not suite completeness. Constructor bodies owning ordinary calls is supported; lambda ownership is lexical, not a caller relation; indexer/event/accessor/initializer owner contexts are not qualified. |
| Required planning correction | Add the concrete F01/F02 inputs now as planning amendments; require a frozen supported lexical-owner table with explicit refusal rules and a fixture per accepted context; keep these as candidate-execution obligations. |
| Separate production correction? | No — future test/acceptance obligation. |
| Evidence for re-review | `acceptance-reconciliation.md` and the R1 amendment to C12/C10. |

---

## Remediation summary

| ID | Severity | Planning correction applied | Production prerequisite |
| --- | --- | --- | --- |
| F01 | P1 | Exact eligible target identity gate + collision corpus | P-DEF-01 (resolver boundary or provider fix) |
| F02 | P1 | Documented/sealed partial trigger named; C12 extended; qualification narrowed | P-DEF-02 (two-layer fix) |
| F03 | P2 | Raw-byte provenance policy | none |
| F04 | P2 | Oracle input + full-coordinate/semantic/uniqueness checks | none |
| S01 | P2 | ADR-CI1-01 reopened; C vs B2 retained | owner decision |
| S02 | P2 | Historical-fixture + old-reader-refusal requirements | owner decision before codec |
| S03 | P2 | Context-separated measurements | none |
| S04 | P2 | Persisted disjoint coverage contract | none (new CI-1 contract) |
| S05 | P3 | Executable adversary obligations + owner-context table | none |

## R2 amendment (CI-1-SCOPE-R2) — F04 disposition correction

> **Status marker.** Appended by CI-1-SCOPE-R2. The F04 table above is preserved
> unchanged; this note corrects its "Evidence required for independent re-review"
> claim after independent Q2 evidence.

The F04 row above states that "R1's independent oracle already does" reject a
wrong outcome. **Independent Q2 disproved the breadth of that claim.** The cited
R1 oracle accepted four outcome mutations (`Q2-F04-01 / P2`), including promoting
the external C10 `System.Console.WriteLine` call to `confirmed`.

- **What R1 required:** full start/end coordinates, semantic target, intended
  outcome and row uniqueness.
- **What R1 claimed:** its independent oracle already rejected all of these,
  including a wrong outcome.
- **What Q2 disproved:** outcome validation was limited to a
  `confirmed`-prefix-gated positive check plus two narrow negative strings; the
  oracle never derived an outcome from bound target eligibility or rejected
  unrecognised outcome strings.
- **R2 executable correction:**
  `docs/planning/ci1-scope-r2/analysis/oracle/` derives the outcome class from
  fixture source and the recorded compilation/dependency context and enforces an
  exact declared-outcome vocabulary.
- **New evidence:** all four Q2 failures are rejected for the intended semantic
  reasons; the unchanged 32-row manifest still passes; the F01 collision stays
  `external`; a no-op positive control is accepted
  (`docs/planning/ci1-scope-r2/evidence/`).
- **Untested until CI-1A:** candidate-result comparison. No candidate exists. R2
  does not claim independent qualification of the corrected checker; that is
  Q3's responsibility. F01–F03, S01–S05, P-DEF-01 and P-DEF-02 are unchanged and
  remain open.
