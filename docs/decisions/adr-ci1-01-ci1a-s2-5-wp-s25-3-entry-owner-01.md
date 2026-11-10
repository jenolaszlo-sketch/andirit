# ADR-CI1-01-CI1A-S2.5-WP-S25-3-ENTRY-OWNER-01 — WP-S25-3 entry assessment and integrated S-01 proof-plan decision

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-ENTRY-OWNER-01`.
**Project:** Andirit. **Recorded:** 2026-11-10. **Canonical branch:** `main`.
**Owner decision:** **`WP-S25-3 ENTRY READY WITH QUALIFICATIONS`** — the integrated S-01 proof design, independent oracle strategy, and D3/D4/D5 matrix are established; **P1 proof execution requires a preceding bounded contract-resolution decision** (Case B, §4) that formally adopts the canonical component-form semantics. **No implementation, proof execution, or qualification is authorized by this decision.**

> **Status: RECORDED — OWNER-AUTHORIZED (planning boundary only).**
>
> The production canonical derivation **exists** (the plugin derives the three component
> digests from the verified live compilation), so this is **not** a missing-behavior case
> (Case C). But the accepted identity contract normatively defines only the **encoding**
> (domains, length-prefixed parts, SHA-256); the **C#-level canonical forms** are currently a
> deterministic, documented **caller convention** ("not an independent canonicalization
> claim", per the source itself). **Case B** therefore applies: a separately authorized
> **contract-resolution decision** must adopt those forms as the normative canonical
> semantics before P1 can establish intended semantics. This decision fixes the proof plan
> for that sequence and explicitly does **not** implement or authorize it.

This is an additive owner decision. It changes no production source, test, CI configuration, fixture, reviewer evidence, oracle or corpus, prior decision, or historical review artifact.

## 1. Verified repository preflight

| Object | Value |
| --- | --- |
| Entry HEAD / tree | `a8c60f8fa36bc055ffdf9706a69ef3977185c4d3` / `4a2a057e11fd7f7c062b4ba9c6f8d4e4455a22d4` |
| Branch / worktrees | `main`; exactly one (`D:/projects/Andirit`); clean tracked/index; no stashes; no concurrent writer |
| Local vs remote | recorded `origin/main` = `443c1927…`; local `main` **76 ahead**; PR #6 and the qualification branch untouched; **no fetch/remote contact** |
| Environment | Windows `10.0.26200` **x64**; .NET SDK `10.0.401`; runtime `10.0.12` |

Stage-2 state verified: WP-S25-1/SQ-04 (amended) and WP-S25-2/SQ-05 accepted with qualifications; **F-G2-01 OPEN**; WP-S25-3/4 planned and unauthorized; Stage-2 acceptance unauthorized.

## 2. Authority and dependency matrix

| Authority | Governs |
| --- | --- |
| [S2.5 entry owner](adr-ci1-01-ci1a-s2-5-entry-owner-01.md) §8 | S-01 proof contract: the integrated chain and the canonical component-digest derivation; proof construction separated from independent qualification |
| [S2.5 work-package plan](../planning/adr-ci1-01-ci1a-s2-5/work-package-plan.md) WP-S25-3 | The bounded proof-package scope |
| [S-01 canonicalization disposition](../planning/adr-ci1-01-ci1a-s2-3/s01-canonicalization-disposition.md) §5/§7 | The S2.5 completion gate: "implement and independently qualify the canonical source derivation … and prove the full S-01 chain end to end"; do not reopen S2.2 |
| [S2.2 G-ACCEPT](adr-ci1-01-ci1a-s2-2-g-accept-owner-01.md) §18 | S2.5 owns the general source canonicalization of the component digests |
| [Identity contract](../planning/adr-ci1-01-ci1a-s2-rem-01/identity-contract.md) | Normative `usage4`/`asmctx3` **encoding**; digests over "**canonical** identifier-site/declaration-site forms" (forms semantics not spelled out — §4 herein) |
| [Stage-2 boundary](../planning/adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8 + [acceptance matrix](../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md) §1–§2, §16 | The integrated S-01 acceptance rows (S2.5 independent acceptance) |
| [Obligation register](adr-ci1-01-ci1a-s2-obligation-register.md) §7 (S-01 row) | "S2.5 integrated independent proof; IR+O" |
| S2.3/S2.4 acceptances; SQ-04 amendment; WP-S25-1/2 owner decisions | The accepted baseline this proof builds on |

Dependency: WP-S25-1 and WP-S25-2 accepted (closed); SQ-04 amended; **F-G2-01 open but independent** (§11).

## 3. S-01 normative interpretation (the complete integrated obligation)

The integrated S-01 chain, decomposed into the eleven verifiable components (each with its contract, current status, and gap):

| # | Component | Authoritative contract | Existing implementation | Existing evidence | Unproven / required new proof |
| --- | --- | --- | --- | --- | --- |
| 1 | Canonical source inputs | S-01 (OWNER §F); S2.1 acceptance | `VerifiedCSharpSource` (immutable buffers, BOM-aware decode, raw-byte SHA-256) | S2.1 acceptance + receipts | Adversarial invariance/difference cases bound to the adopted canonical forms (P1) |
| 2 | Canonical serialization | Identity contract (length-prefixed big-endian UTF-8 parts, domain-separated SHA-256, lowercase hex) | `CodeOccurrenceIdentity.ToParts/Hash` | Frozen R2 vectors; REM-01 identity review | Known-answer vectors recomputed by an independent implementation (P1 oracle) |
| 3 | `identifierDigest` derivation | Identity contract: "canonical identifier-site forms" | Plugin: `ComputeIdentifierDigest([method.Name])` of the normalized definition | Frozen-corpus cross-checks (S2.3/S2.4/FINAL-Q1) | **General-source** independent reproduction (Case B adoption first) |
| 4 | `declarationDigest` derivation | Same | Plugin: `ComputeDeclarationDigest([docID ?? Kind:FQDN])` of the normalized definition | Same | Same |
| 5 | `shapeDigest` derivation | Same ("shape") | Plugin: `"method\|arity\|FQ-param-types"` | Same | Same |
| 6 | Independent semantic reproduction | G-ACCEPT §18; S2.5 entry §8 | — | — | The independent oracle itself (§5) |
| 7 | Real-source extraction/ingestion | S2.2/S2.3 accepted path | `CSharpCodeGraphPlugin` → batch/sink | S2.3/S2.4/FINAL-Q1 | Chain re-execution with digest-level assertions at the pinned revision (P1) |
| 8 | Publication/authority binding | D4; S2.3 §5 | `PublishOccurrencesAsync` → `SetOccurrenceAuthorityAsync` → batch | S2.3 acceptance | Digest-level persisted-row verification (P1) |
| 9 | Durable reopen/recovery | D4.4/D4.7; NEW-01 | Native restore + managed recovery | S2.3/WP-S23 evidence | Post-reopen identity equality (P1) |
| 10 | D3/D4/D5 behaviors | OWNER §D.3–D.5 | Accepted store behavior | S2.3 + prior gates | The §7 matrix (P1 scope) |
| 11 | Evidence provenance/oracle | MD-01; register | Manifest pattern | Prior packages | The S-01 evidence manifest design (§10) |

**Source-code presence is not treated as proof** — every "existing evidence" cell is inherited; the new proof must re-establish the chain at the pinned revision with independent expectations.

## 4. Canonical digest gap assessment (Case determination)

**Per-digest anatomy** (all three share the same shape):

| Aspect | Finding |
| --- | --- |
| Producers | Exactly one production derivation: `CSharpCodeGraphPlugin.Session` — `OccurrenceIdentifierDigest` (`ComputeIdentifierDigest([method.Name])`), `OccurrenceDeclarationDigest` (`ComputeDeclarationDigest([CanonicalSymbolKey(Normalize(method))])`, doc-ID-first with a `Kind:FQDN` fallback), `OccurrenceShapeDigest` (`"method\|arity\|FQ-param-types"`). Probes/fixtures supply their own functions (declared caller-supplied inputs). |
| Serialization boundary | `CodeOccurrenceIdentity` (`Andirit.Abstractions`): domain-separated SHA-256 over 32-bit big-endian length-prefixed UTF-8 parts; lowercase hex; `IsCanonicalTarget`/constructor validation on `asmctx3`. |
| Consumers | `VerifiedCSharpOccurrenceExtractor` → `CodeOccurrenceIdentity.ComputeTargetId` → `usage4` facts → native `AndiritOccurrenceUnit/Page/Meta` → authority envelope → reader results. |
| Authoritative inputs | The verified live `IMethodSymbol` (post-`Normalize`: `ReducedFrom ?? OriginalDefinition`) — definition-level symbols from the accepted compilation. |
| Canonical representation | **Encoding: fully normative** (identity contract; frozen vectors). **Semantic forms: implemented but not normatively adopted** — the plugin's own comment states the forms are "this plugin's deterministic caller convention, not an independent canonicalization claim". |
| Normalization/ordering rules | Deterministic and documented in code (`Normalize` = definition-level; `CanonicalSymbolKey` = doc-ID-first; `TypeIdentity` = fully-qualified display strings; single-part lists make ordering trivial), but not yet **normatively adopted** as the accepted canonical semantics for general source. |
| Algorithm/versioning | SHA-256; domains `asmctx3-identifier`/`-declarations`/`-target`; semantics version 4; the R2 encoding is version-frozen. |
| Independent recomputation possible? | **Yes** — the primitives are public and the forms are symbol-deterministic; an independent oracle can derive expected forms from fixture metadata by hand and hash them with the public primitives (or a separate reference implementation of the documented encoding). |
| Sufficient for S-01 today? | **Not yet** — the general-source canonical semantics must be normatively adopted first (below). |
| Missing definition/implementation | No missing *behavior*; a missing **normative adoption** of the forms (and an explicit invariance/equivalence table). |

**Case determination:**

- **Encoding layer — Case A-equivalent:** the algorithm, domains, and serialization are normative, implemented, and independently verifiable (frozen vectors). No action beyond independent reproduction.
- **Semantic-forms layer — Case B.** The implementation **exists and is complete for the supported category** (method-invocation occurrence facts), but the accepted identity contract leaves the C#-level canonical forms ("canonical identifier-site/declaration-site forms") **without a normative definition** — the plugin's convention fills that space by default. An independent oracle that encoded *different* reasonable forms would produce different digests with no contract violated — which is exactly why the historical finding (caller-supplied digests, no independent canonicalization claim) remains accurate. Per the commission: a **separately authorized contract-resolution decision is required before P1 proof can establish the intended semantics.** This entry decision does **not** silently convert Case B into Case A, and does **not** assume the native store is responsible for recomputing source-level semantic digests (the accepted architecture places derivation in the producer; the store validates canonical *shape* only).
- **Case C (missing behavior): not applicable.** No required canonicalization behavior is absent — every supported symbol category within the accepted invocation scope is deterministically derivable today.

**Adoption contents the contract-resolution decision must pin (draft checklist, from the actual code):** identifier form = the normalized definition's simple `Name` (overloads intentionally share an identifier digest and are separated by declaration/shape); declaration form = `GetDocumentationCommentId()` of the normalized definition with the `Kind:FQDN` fallback for doc-ID-less symbols; shape form = `"method|" + Arity + "|" + comma-joined fully-qualified parameter-type display strings`; normalization = `ReducedFrom ?? OriginalDefinition`; target document = doc-ID ?? CSharp-error-format qualified name; plus the explicit invariance table (formatting, comments, trivia, member order, namespace-qualification styles) and difference table (overloads, parameter-type changes, renames, generic instantiation vs definition, extension reduction, nested-type moves).

## 5. Independent canonical oracle strategy

The oracle must never derive its expectations by calling the production derivation. Consistent with the (post-adoption) contract:

1. **Encoding-layer known-answer vectors** — hand-computed SHA-256 over hand-assembled length-prefixed part sequences (computed with an independent script/implementation, cross-checked against the frozen R2 vectors), covering multi-part lists, boundary lengths, non-ASCII text, and the exact lowercase-hex output form.
2. **Semantic-form literals** — for each fixture case, the expected canonical forms are written as **literal strings in the probe** (e.g., identifier form `"Write"`, declaration form `"M:Corpus.Worker.Write(System.String)"`, shape form `"method|1|global::System.String"`) and hashed via the public primitives (or a reference implementation of the adopted encoding) — never via the plugin's derivation functions.
3. **Deterministic perturbation matrix** — fixture pairs differing in exactly one contract-relevant dimension: formatting/whitespace/comments/trivia (expect identical digests); member ordering and file layout (identical for the same declared definitions); source namespace-qualification style (identical — forms derive from symbols, not syntax); overload addition (existing target digests unchanged; the overload shares the identifier digest and differs by declaration/shape); parameter-type change (shape/declaration change; identifier unchanged); rename (identifier/declaration change); generic instantiation vs definition (normalized to the definition); extension-method reduction (reduced form); nested/moved types (doc-ID-driven declaration change); formatting-only vs genuine semantic change (the pivotal distinction, assertable only after adoption).
4. **Negative cases** — unavailable/external/unbound defining contexts produce no fact and no minted target (accepted refusal contract), never a digest.

**Reviewer verification of the oracle:** the independent reviewer receives the fixture sources, the literal expected-form table, and the adopted contract text, and re-derives the expected digests with its own tooling; any reviewer/producer disagreement is a blocking finding. The adopted contract text is the arbiter — not either implementation.

## 6. Real-source integrated proof sequence (P1 design; verified against the repository)

All named APIs exist and are the accepted boundaries (verified: `VerifiedCSharpOccurrenceExtractor.Extract`, `CodeGraphBatch`, `CodeGraphIngestionSink.WriteBatchAsync`/`CompleteAsync`, `StageIndexUnitAsync`, `CompleteIndexRunAsync`, `LatticeCodeGraphStore` open/reopen, `FindOccurrencesAsync`, `SetOccurrenceAuthorityAsync` via `ICodeOccurrenceAuthoritySink`):

| Stage | Input authority | Output authority | Canonical identity | Invariants | Evidence | Expected refusal cases |
| --- | --- | --- | --- | --- | --- | --- |
| S1. Verified source | repository bytes → `VerifiedCSharpSource`/`Compilation` | `compctx1`/`compctx2` | S-01 input hashes | bytes = Roslyn input; BOM/decode per S2.1 | per-document source SHA-256 receipt | decode/encoding refusal |
| S2. Semantic extraction | verified compilation | extraction result + authority envelope | component digests + `asmctx3`/`usage4` | digests derive from the normalized definition; envelope binds owner/plugin/run/unit | extraction receipt with a digest-vector table | external/unbound/ambiguous → no fact |
| S3. Batch/sink | extraction output | `CodeGraphBatch` (occurrences + coverage) | `usage4` per fact | contributor = origin unit; payload integrity | batch receipt | payload conflict rejection |
| S4. Stage/complete | sink state | staged unit + completed run (same transaction as graph) | persisted unit/page identity | authority association registered before completion | staging receipt | sink without authority → graph-only (no bare facts) |
| S5. Published store | completed run | native graph/occurrence rows | `AndiritOccurrenceUnit/Page/Meta` | meta binds publication; pages ordered | native-row inspection receipt | page/publication mismatch → `CodeOccurrenceIndexException` |
| S6. Durable reopen | persisted rows | reconstructed projection + authority | post-reopen digests/identities | reopen == pre-close query results | reopen receipt | schema/namespace refusal (D5) |
| S7. Authoritative query | reader + authority | `CodeOccurrenceQueryResult` | served identities = published identities | availability/refusal correctness; `Examined` bounded; no fabricated completeness | query receipt | stale/missing authority → refusal |
| S8. Recovery | managed fault (before-commit/after-commit) | recovered projection | identities preserved | recovered == durable authority; quarantine distinct (F-02 boundary) | recovery receipt | unknown outcome → quarantine |

Fixtures: the **frozen R2 corpus** (real source, accepted oracle) plus one **small adversarial canonicalization fixture** sized to the perturbation matrix — bounded, reproducible, exact expected values; no large synthetic application.

## 7. D3/D4/D5 and recovery matrix (integrated-proof requirements)

| Area | Requirement (exact normative meaning) | P1 proof scope | Remaining elsewhere |
| --- | --- | --- | --- |
| D3 | Publication identity/authority binding; published graph/occurrence consistency; legacy-graph noninterference | Before/after graph snapshots across the S-01 publication (identities/counts/traversal unchanged; occurrence facts never in the graph) | D3-full real-project corpus (Stage 6) |
| D4 | Accepted generation only; stale/foreign refusal; bounded `Examined`; failed-staging preservation (NEW-01); reopen equivalence; contributor/deletion consistency; deterministic order/limits; serialized model | Exercised through the S-01 chain: publish → reopen → query → managed recovery, with identities asserted at each stage | D4.5 (cancelled staging) and full S-02 ten-invariant matrix (Stage 6) |
| D5 | No migration, no dual-write; refusal of unsupported/incompatible persisted data; re-ingestion rebuild only | Schema/namespace/version refusal on the reopened store; no migration code path exercised or added | Aggregate/Hetu historical contracts (untouched) |

**F-02 preserved:** managed fault injection (before-commit/after-commit, unknown-outcome quarantine) is the recovery evidence; it does **not** establish process-crash or power-loss durability, and none is claimed.

## 8. Proof scope, resources, and evidence plan (P1)

- **Fixtures:** frozen R2 corpus (real source; accepted oracle) + one small adversarial canonicalization fixture (~6–10 small files) covering the perturbation matrix. No large synthetic application.
- **Independent oracle:** literal expected-form table + independent encoding implementation + known-answer vectors (§5).
- **Native-store lifecycle:** publish → durable reopen → managed recovery → refusal scenarios, per §6.
- **Environment:** Windows x64 (the accepted platform); no new platform support.
- **Evidence artifacts:** per-stage receipts (S1–S8), the digest-vector table (expected vs actual per case), source hashes, canonical bytes, publication/reopen/query receipts, native-row inspection, and a self-excluding SHA-256 manifest.
- **Regression floor:** full solution **907/0/0**; frozen R2 oracle **185/185** (146,851 bytes, `97f6c5e5…39c9`); Release 0/0; format exit 0. **Verified:** `src/` and `tests/` are unchanged since the 907 baseline (`8d2a762`) — no new relevant tests; the baseline stands. Existing evidence (SQ-04 fixtures, SQ-05 Linux receipts, frozen oracle, accepted manifests) is preserved; the eventual S-01 proof gets its own self-excluding manifest. No resealing.
- **Reviewer responsibilities (P2):** independent oracle re-derivation, semantic equivalence/difference verification, native publication/reopen/recovery verification, evidence-integrity review.

## 9. Execution and qualification sequence (separate gates)

| Gate | Content | Authorization |
| --- | --- | --- |
| **P0** | Bounded **contract-resolution decision** (Case B): formally adopt the canonical component-form semantics (§4 checklist) and the invariance/difference table; if adoption reveals an implementation gap, a separate bounded remediation commission is inserted **before** P1 | Owner decision (documentation) |
| **P1** | WP-S25-3 proof implementation/execution per §5–§8 against the adopted contract | Owner commission (tests/probes/evidence only; no production change unless P0 revealed a gap → separate remediation) |
| **P2** | Independent S-01 qualification (reviewer ≠ producer): independent oracle re-derivation, semantic equivalence/difference verification, native publication/reopen/recovery verification, evidence-integrity review | Independent qualification commission |
| **P3** | WP-S25-3 owner acceptance: whether S-01 and its associated Stage-2 obligations are adequately discharged | Owner decision |

No gate implies its successor. If P0's adoption or P1's execution reveals an implementation defect, the affected claim **stops** and a separate bounded remediation authorization is inserted before any proof-completion claim.

## 10. F-G2-01 independence

**F-G2-01 remains `OPEN — REPOSITORY CI BRANCH-COVERAGE GATE FAILURE`** (branch 296/450 = 65.77% vs 70%; platform-independent; pre-existing). This commission does not change coverage policy. **S-01 must not be expanded solely to obtain ~19 additional covered branches** — S-01 tests may incidentally improve coverage, but coverage remediation requires its own authorization and evidence. The failed overall CI conclusion (PR #6) is preserved as an independent finding.

## 11. Binding scope

**Not authorized by this decision:** production implementation changes; canonical-digest algorithm changes; serialization-format changes; new native schema or storage paths; public Find Usages; cross-target implementation; WP-S24-3; Linux reruns; CI policy changes; PR #6 modification; migration or dual-write; Stage 6; Stage-2 final acceptance. **All previously accepted S2.3/S2.4 and S2.5 qualifications are preserved** (F-01, F-02, F-03 as amended, Q1-Q1, Q1-Q2, S-01, Q2-Q01, Q2-Q02, O-Q2-01, Q-I01, Q-I08, same-harness limitation, Windows-x64/serialized single-writer, managed-fault limits, SQ-04 amended boundary, SQ-05 bounded scope, F-G2-01 open).

## 12. Formal entry disposition

**`WP-S25-3 ENTRY READY WITH QUALIFICATIONS`.**

The S-01 contract matrix (§3), canonicalization status (§4), independent oracle strategy (§5), integrated proof design (§6), D3/D4/D5 requirements (§7), and evidence/regression plan (§8) are complete and feasible with bounded resources on the accepted real path. The entry is **qualified** because the mandatory canonical-form semantics must first be **formally adopted** (Case B, §4 — the P0 contract-resolution decision): until then, the independent oracle lacks a normative basis for its expected values, and the proof must not be labeled ready for execution. No Case C implementation gap and no normative SQ-05 blocker exists.

## 13. Next proposed commission (explicitly NOT authorized)

**Proposed:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P0-CONTRACT-OWNER-01` — a documentation-only bounded decision that adopts the canonical component-form semantics (the §4 checklist: identifier/declaration/shape forms, normalization, invariance and difference tables, versioning) as the normative S-01 canonicalization contract, explicitly preserving the caller-consumer boundary and the accepted `usage4`/`asmctx3` encoding. Only after P0 does the **P1 proof-execution commission** become eligible (tests/probes/evidence against the adopted contract), followed by the independent **P2** qualification and the **P3** owner acceptance. **Not authorized by this decision:** P0 itself, P1 execution, any implementation change, any qualification, any owner acceptance.

## 14. Successor authorization status

- **WP-S25-1: accepted/closed. SQ-04: satisfied under the amended contract. WP-S25-2: accepted/closed. SQ-05: satisfied (qualified). F-G2-01: OPEN.**
- **WP-S25-3: `ENTRY READY WITH QUALIFICATIONS` — P0/P1/P2/P3 all NOT authorized.**
- **WP-S25-4: planned, NOT authorized. Stage-2 final integration: incomplete. Stage-2 owner acceptance: unauthorized.**
- **WP-S24-3: optional, deferred, unauthorized. Public Find Usages: not authorized. Stage 6: not authorized. Optimization: not authorized. Migration/dual-write: forbidden (D5).**
- No production change, canonicalization change, proof execution, or qualification is authorized by this decision.

## 15. Repository discipline

Only this decision document and the additive WP-S25-3 status-note update to the S2.5 work-package plan are added, on canonical `main`, under the single-worktree policy. No production code, test, CI configuration, fixture, historical review, evidence, oracle/corpus, prior decision, or existing planning contract is modified. No remote operation, fetch, push, or additional worktree is performed. The decision commit/tree identities and final working-tree status are reported in the handoff.