# ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01 — bounded integrated S-01 proof (canonical-forms v1)

**Commission:** `ANDIRIT-CI1A-S2.5-WP-S25-3-P1-PROOF-01` (`WP-S25-3 P1 PROOF EXECUTION AUTHORIZED WITH QUALIFICATIONS`).
**Project:** Andirit. **Recorded:** 2026-11-11. **Canonical branch:** `main`.
**Starting HEAD:** `349136db53c811c4e648e7986865a60991c2e232` (tree `d7d23731e05f5f87b001bcabc73ec38473db03fd`) — the P0 contract-adoption commit. The P1 harness, evidence package, and this report are committed on top as one local commit (no push).
**Normative contract:** `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md` (canonical-forms v1), 12,636 bytes, SHA-256 `5958dd6bb40da2d44c6845a65d7aee0e8e5c9c06966a4a29653c8dbb6c7f4852` (unchanged by P1).
**Final disposition:** **`S-01 P1 INTEGRATED PROOF COMPLETE WITH QUALIFICATIONS`**.

> P1 establishes implementation/execution evidence for the adopted canonical-forms v1 contract:
> the independent oracle reproduces every mandatory encoding and identifier/declaration/shape
> semantic, and the real source-to-native-store path (extraction → batch/sink → stage/complete →
> native rows → durable fresh-process reopen → authoritative query/refusal → managed recovery)
> executes with expected identities. Three v1 §7 row-13 expectation assertions fail against
> v1-as-written; the smallest reproductions, expected bytes, and observed outputs are recorded
> as contract-text defects (F-P1-02, plus F-P1-01/F-P1-03 clarifications) — no implementation
> contradiction was identified and nothing was changed in code or contract by this commission.
> This report is **producer evidence**: independent P2 qualification and owner acceptance remain
> separate gates. **S-01 is not marked satisfied.**

---

## 1. Verified repository preflight

| Check | Result |
| --- | --- |
| HEAD / tree | `349136db53c811c4e648e7986865a60991c2e232` / `d7d23731e05f5f87b001bcabc73ec38473db03fd` — both exactly as commissioned (P0 commit) |
| Branch | `main` |
| Worktrees | Exactly one (`D:/projects/Andirit`); no extra worktrees created |
| Tracked/index state | Clean before modification; the only changes introduced are the permitted P1 artifacts (harness + evidence + this report) |
| Stashes | None |
| Concurrent writer | None (single-session execution; every mutating command recorded in the evidence logs) |
| Local vs remote (recorded) | Recorded `origin/main` = `443c1927…`; local `main` 78 commits ahead per the P0 handoff. No fetch, push, prune, or remote operation occurred; PR #6 and the qualification branch were untouched; no GitHub contact |
| Reset/clean/restore/rebase | None performed |

The proof was executed against the P0-adopted contract with no production, test, CI, contract, or
prior-evidence modification. All P1 artifacts are new and isolated under
`docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-tests/` and `docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/`.

## 2. Contract v1 identity and scope

- Identity: `docs/planning/adr-ci1-01-ci1a-s2-5/s01-canonical-form-contract.md` — 12,636 bytes,
  SHA-256 `5958dd6bb40da2d44c6845a65d7aee0e8e5c9c06966a4a29653c8dbb6c7f4852` (re-verified during the
  proof run; recorded in `receipt-summary.log`). The contract file is byte-unchanged by P1.
- Normative content exercised: §3 identifier form (normalized simple name), §4 declaration form
  (doc-ID-first `CanonicalSymbolKey`, byte-exact `Kind:FQDN` fallback), §5 shape form
  (`method|arity|FQ-param-types`), §6 byte-exact encoding (4-byte big-endian length + UTF-8 per
  field, SHA-256, lowercase hex, three domains, `asmctx3` composition), §7 the 15-row matrix,
  §10 independent-oracle requirements.
- Digest semantics version: occurrence semantics **4** (`usage4`) with the frozen
  `asmctx3`/`compctx1` encoding (v1 §9); no migration, dual-write, fallback, or silent upgrade was
  introduced (D5 posture preserved by construction).
- Out-of-scope per v1 §11 (not claimed by P1): non-method occurrence kinds, non-C# languages,
  return-type-sensitive identity, custom modifiers, cross-language identity.
- P1 did **not** amend v1. Contract-text defects found by execution are recorded as findings
  (§15) and routed to a future narrow owner clarification.

## 3. Canonical oracle architecture

Isolated proof project `docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-tests/` (`P1Proof.csproj`,
`AssemblyName=Andirit.CSharp.Tests` — the established internals-access pattern; not part of
`Andirit.slnx`, so P1 results can never hide inside the inherited test count):

| Artifact | Role |
| --- | --- |
| `Oracle.cs` — `RefEnc` | Independent reference encoder: own 4-byte big-endian length prefixing, UTF-8 part encoding, SHA-256 (standard primitive), lowercase hex. **Never calls `CodeOccurrenceIdentity`** for expected values |
| `Oracle.cs` — `CaptureSink` | Decorates the real `CodeGraphIngestionSink`, capturing batches and occurrence-authority envelopes while forwarding to the real store path |
| `Oracle.cs` — `Extract.RunAsync` | Real plugin extraction over an in-memory store through the accepted session/sink path (Part A fixtures) |
| `Cases.cs` | Known-answer entry point plus literal-fixture form cases (FC-*), the 15-row matrix (R*), the frozen-corpus loader, and the `SourceFiles` evidence helper |
| `Integration.cs` | S1–S8 verbs over the real `LatticeCodeGraphStore` (publish / reopen / recovery), expectations assembled from `RefEnc` + the adopted `asmctx3` composition (cross-checked against the frozen R2 authority primitives) |
| `Program.cs` | Verb entry (`oracle`, `publish`, `reopen`, `recovery`) writing JSON receipts |
| `run-proof.ps1` | Orchestration: build → Part A → Part B (fresh process per verb) → toolchain → regression gates → frozen-oracle artifact verification → `receipt-summary.log` |
| `seal-evidence.ps1` | Self-excluding SHA-256 manifest for the evidence package |

Expected-value path discipline: expected forms are literal strings (FC-1…FC-8, FC-10b, FC-12) or
assembled from the contract §4/§5 formulas over the contract-named Roslyn primitives
(`GetDocumentationCommentId`, `SymbolDisplayFormat.FullyQualifiedFormat`) in FC-9…FC-11; expected
digests always come from `RefEnc` (or hand-written bytes in KAT-1). The production canonicalization
(`OccurrenceIdentifierDigest`/`DeclarationDigest`/`ShapeDigest` internals) is invoked **only for
comparison**, never for expectations. KAT-1's message bytes are hand-written hex, hashed directly.

## 4. Known-answer results (encoding layer) — 10/10

Receipt: `Q1-ORACLE.json` (`knownAnswers`). All message-byte reconstructions are recorded in hex.

| # | Vector | Expected source | Result |
| --- | --- | --- | --- |
| KAT-1 | domain `asmctx3-identifier`, field `Write`; hand-written bytes `0000001261736d637478332d6964656e746966696572000000055772697465` | digest computed directly from the byte literal | `4a468c685e4033c579aff4dcbf371062dc6a88c33318a89d54dedaf60f61af88` = production digest ✓ |
| KAT-2/3 | Cyrillic `Проверка`, Japanese `日本語` | RefEnc bytes+digest | equal to production ✓ |
| KAT-4 | multi-part `["Alpha","Beta"]` | RefEnc | ✓ |
| KAT-5 | declaration `M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` | RefEnc (domain `asmctx3-declarations`) | ✓ |
| KAT-6 | shape `method|0|global::System.Int32` (encoding layer only; the form-layer literals used by Part A/B follow the documented format — see F-P1-01) | RefEnc (domain `asmctx3-target`) | ✓ |
| KAT-7 | domain separation (same fields, three domains) | RefEnc + production | distinct across domains, equal within ✓ |
| KAT-8 | multi-part ordering sensitivity | RefEnc + production | `AB ≠ BA` ✓ |
| KAT-9 | empty field: encoding computes (length 0), form layer rejects | RefEnc + expected refusal | digest computed; `ComputeIdentifierDigest([""])` throws (v1 §6 non-empty requirement) ✓ |
| KAT-10 | lowercase hex, length, canonical-target validation | contract predicates | valid accepted; uppercase/short/wrong-prefix rejected ✓ |

The v1-§6 encoding (domain separation, ordering, length prefixing, UTF-8, lowercase hex, canonical
target shape) reproduces exactly on all vectors.

## 5. Identifier/declaration/shape qualification — 17/17 form cases

Receipt: `Q1-ORACLE.json` (`formCases`); each entry preserves the raw fixture sources
(`sourceFiles`), literal expected forms, expected digests (RefEnc), actual digests, and pass state.

| Case | Fixture semantics | Expected identifier / declaration / shape form | Result |
| --- | --- | --- | --- |
| FC-1 | simple static method invocation | `Write` / `M:Fixture.Core.Logger.Write(System.String)` / `method|0|string` | ✓ |
| FC-2a/2b | same simple name in different containing types | `Ping` / `M:Fixture.Core.Alpha.Ping` resp. `M:Fixture.Core.Beta.Ping` / `method|0|` | ✓ (identifier forms identical — simple-name collisions permitted by v1 §3; distinction carried by declaration) |
| FC-3 | overload selection (`Add(int)`) | `Add` / `M:Fixture.Core.Methods.Add(System.Int32)` / `method|0|int` | ✓ |
| FC-4 | generic method definition (inferred call) | `Pick` / `` M:Fixture.Core.Generic.Pick``1(``0) `` / `method|1|T` | ✓ |
| FC-5 | reduced extension method | `DoubleIt` / `M:Fixture.Core.Ext.DoubleIt(System.Int32)` / `method|0|int` | ✓ |
| FC-6 | `ref` parameter (doc-ID `@`) | `Swap` / `M:Fixture.Core.Mods.Swap(System.Int32@)` / `method|0|int` | ✓ |
| FC-7 | frozen corpus S03 (`Methods.Add(1,2)`) | `Add` / `M:Corpus.Core.Methods.Add(System.Int32,System.Int32)` / `method|0|int,int` | ✓ |
| FC-8 | frozen corpus S12 (explicit-interface dispatch) | `Greet` / `M:Corpus.Core.IGreeter.Greet(System.String)` / `method|0|string` | ✓ (see §7) |
| FC-9 | nested generic parameter (internal comma) | `Use` / doc-ID of `Dictionary<string,int>` param / `method|0|global::System.Collections.Generic.Dictionary<string, int>` | ✓ |
| FC-10 | tuple parameter (internal comma) | `Handle` / doc-ID of `(int,string)` / `method|0|(int, string)` | ✓ |
| FC-10b | two-parameter near-collision partner | `Handle` / `M:Fixture.Core.Pairs.Handle(System.Int32,System.String)` / `method|0|int,string` | ✓ |
| FC-11 | nested-generic + int near-collision partner | `Handle` / doc-ID of (`Dictionary<string>`, `int`) / `method|0|global::System.Collections.Generic.Dictionary<string>,int` | ✓ |
| FC-12 | array parameter | `Sum` / `M:Fixture.Core.Arrays.Sum(System.Int32[])` / `method|0|int[]` | ✓ |
| FC-13 | return-type change (`int`→`string`) | identifier/declaration/shape invariant across both variants | ✓ (all three component digests equal — return type intentionally omitted, v1 §5) |
| FC-14 | explicit-interface *implementation* symbol (P0 observation) | doc ID `M:Corpus.Core.ExplicitGreeter.Corpus#Core#IGreeter#Greet(System.String)`; selected path doc-ID-first | recorded (see §7) |

Identifier/declaration/shape semantics evidenced: simple-name identifiers with permitted
collisions; containing-type and overload distinction carried by doc-ID declarations; generic
arity encoded in both member name (`` `1 ``) and shape arity; reduced-extension normalization to
the definition; unresolved bindings produce no fact (R15); byte-exact doc-ID-first selection with
the §4 fallback available (not exercised by any supported occurrence target in this fixture set).

## 6. Fifteen-row normative matrix (v1 §7) — executed; 15 rows pass, row 13 assertions fail against v1-as-written

Receipt: `Q1-ORACLE.json` (`matrix`). v1 §7 is 15 rows; rows 6 and 13 are executed as their
component pairs (R6a/R6b, R13a/R13b/R13c) — 18 fixture pairs. Each entry preserves source
before/after, the v1 expectation, the observed relation per component (identifier / declaration /
shape digest equality), and component-level pass states. A `≠` final target digest alone was never
accepted as evidence for a required component distinction.

| Row | Transformation | v1 (id/decl/shape) | Observed | Class |
| --- | --- | --- | --- | --- |
| R1 | whitespace/formatting | =/= /= | =/= /= | PASS |
| R2 | comments/trivia | =/= /= | =/= /= | PASS |
| R3 | declaration relocation | =/= /= | =/= /= | PASS |
| R4 | namespace-qualification style | =/= /= | =/= /= | PASS |
| R5 | type alias | =/= /= | =/= /= | PASS |
| R6a | unrelated overload addition (existing target) | =/= /= | =/= /= | PASS |
| R6b | retargeted overload | =/≠/≠ | =/≠/≠ | PASS |
| R7 | rename | ≠/≠/= | ≠/≠/= | PASS |
| R8 | parameter-type change | =/≠/≠ | =/≠/≠ | PASS |
| R9 | parameter-order change | =/≠/≠ | =/≠/≠ | PASS |
| R10 | generic arity change | =/≠/≠ | =/≠/≠ | PASS |
| R11 | generic substitution | =/= /= | =/= /= | PASS |
| R12 | reduced extension vs static form | =/= /= | =/= /= | PASS |
| R13a | value vs `ref` | =/≠/≠ | =/≠/**=** | **FAIL vs v1-as-written** (F-P1-02) |
| R13b | `ref` vs `out` | =/≠/≠ | =/**=**/**=** | **FAIL vs v1-as-written** (F-P1-02) |
| R13c | `ref` vs `in` | =/≠/≠ | =/**=**/**=** | **FAIL vs v1-as-written** (F-P1-02) |
| R14 | same simple name, different owner | =/≠/=(possibly =) | =/≠/= | PASS |
| R15 | unresolved/external binding | refusal/refusal/refusal | refusal/refusal/refusal (no fact; external `Console.WriteLine` produced nothing while the in-corpus control fact is present) | PASS |

Row 13 analysis (the only failing mandatory row): the adopted declaration form is
`GetDocumentationCommentId()`, which renders `ref`/`out`/`in` parameters **uniformly** with the `@`
suffix (Roslyn doc-ID grammar) — it cannot distinguish `ref` from `out` from `in`. The adopted
shape form displays parameter **types** (`FullyQualifiedFormat`); `ref`-kind lives on the
parameter symbol, not on its type, so it never appears in the display. Consequences: (a) value vs
`ref` is distinguished by declaration (`Int32` vs `Int32@`) — shape equal; (b) `ref` vs `out` vs
`in` share all three components — harmless because C# **forbids** overloads differing only by
`ref`/`out`/`in` (CS0663), so no in-compilation collision exists, while value-vs-`ref` overloads
(which C# permits) are distinguished by declaration. The observed behavior is internally
consistent and doc-ID-conformant; v1 §5's commentary ("`ref`/`out`/`in` modifiers appear in the
display strings and therefore in the shape") and row 13's expected relations are the defect.
No implementation change was made (commission §9/§14).

## 7. Explicit-interface results (P0 mandatory qualification)

- **Call-site occurrence (FC-8, frozen corpus S12)**: source
  `IGreeter explicitGreeter = new ExplicitGreeter(); _ = /*@S12*/ explicitGreeter.Greet("b");`
  — Roslyn symbol: `Corpus.Core.IGreeter.Greet(string)` (dispatch binds the interface member;
  PC-8 accepted semantics); available documentation ID
  `M:Corpus.Core.IGreeter.Greet(System.String)`; selected canonical path: doc-ID-first; expected
  declaration form == actual == literal, with independent digest comparison — PASS. No fallback
  path was needed or silently taken.
- **Implementation declaration symbol (FC-14, observation)**: in this Roslyn generation the
  explicit interface implementation *does* receive a documentation ID:
  `M:Corpus.Core.ExplicitGreeter.Corpus#Core#IGreeter#Greet(System.String)` (containing interface
  escaped with `#` per doc-ID grammar; interface member doc ID
  `M:Corpus.Core.IGreeter.Greet(System.String)`). This resolves the P0 execution-time caveat with
  concrete evidence: doc-ID-first selection is deterministic for both the interface member (the
  supported occurrence target) and the implementation symbol; the implementation symbol is not an
  occurrence target in the supported surface, so this row is recorded as an observation, not a
  production derivation claim.
- No contradiction with canonical-forms v1 was found in either record.

## 8. Shape delimiter/collision assessment (v1 §5/§8)

Concrete test evidence (real Roslyn-produced displays, all 17 form cases passing):

| Challenge | Shape string | Partner | Shape string | Distinct? |
| --- | --- | --- | --- | --- |
| single tuple parameter vs two parameters | `method|0|(int, string)` | `method|0|int,string` | ✓ |
| single nested-generic parameter (internal comma) vs generic + separate int | `method|0|global::System.Collections.Generic.Dictionary<string, int>` | `method|0|global::System.Collections.Generic.Dictionary<string>,int` | ✓ |
| single nested-generic parameter vs single tuple parameter vs plain two-parameter | all three pairwise distinct (above plus `method|1|T`-style arity cases) | | | ✓ |

`FC-DELIM` in the receipt records the challenged pairs and their distinctness
(`allChallengedPairsDistinct=true`). Every challenged string contains internal commas inside
balanced `<…>`/`(…)`; the join remains unambiguous in the exercised cases.

Grammar-based reasoning (not a test): a collision requires a balanced whole display to equal an
unbalanced comma-split fragment; valid fully-qualified type displays are balanced, so the
unescaped join is collision-free **over the accepted display grammar** — consistent with v1 §5.

**Untested assumptions / limits (explicit):** finite fixture cases do **not** constitute a
universal collision-freedom proof; pointer types, custom modifiers, very deep nesting, and
localized/alternate compilation contexts were not exercised (custom modifiers are out of scope
per v1 §11). No representational collision was discovered; no claim beyond the exercised cases
plus the balanced-display argument is made.

## 9. Roslyn and SDK/toolchain provenance

Receipt: `toolchain.log`; assumptions bound per v1 §8/§10 (no stability claimed across arbitrary
Roslyn versions). **No dependency was upgraded to make a proof pass.**

| Item | Value |
| --- | --- |
| OS / architecture | Windows `10.0.26200`, x64 (`win-x64`) |
| .NET SDK | `10.0.401` (MSBuild 18.9.11) |
| .NET runtime | `10.0.12` |
| Roslyn | `Microsoft.CodeAnalysis.CSharp 5.9.0` (project package reference) |
| Language version | `LanguageVersion.Latest` unless a project overrides it (corpus `LangVersion 14` honored by the plugin's parse options) |
| Compilation options | `DynamicallyLinkedLibrary`, `deterministic: true`, `concurrentBuild: false`, nullable per project configuration |
| Display format | `SymbolDisplayFormat.FullyQualifiedFormat` (special-type keywords; `global::` qualification for non-special types) + `GetDocumentationCommentId()` (doc-ID grammar) — the format actually used matches the *documented* Roslyn format (F-P1-01) |
| Extractor | `Andirit.CSharp` plugin, id `penghou.hetu.csharp`; extractor execution = real `CSharpCodeGraphPlugin` session through the accepted sink |
| Trusted references | The plugin captures the host process `TRUSTED_PLATFORM_ASSEMBLIES` closure as content-hashed reference identities — recorded basis of observation O-1 (§15) |
| Frozen corpus (publishable subset) | 12 files; composite identity `sha256:3576791bd7318fb9949769d5676342a1473d84b38d42f86626e8ee6553876948` (sorted `path\nsha256:<hex>\n` records; independently reproduced with a separate PowerShell computation) |
| Adversarial fixture | 4 files; composite identity `sha256:43f990c0bfead8eb7408605db8c903a5dde20d35730e8a727465c286edaced3f`; per-file hashes in `Q2-INTEGRATION.json` |
| Contract version | canonical-forms **v1** (identity in §2) |
| Digest semantics version | occurrence semantics **4** (`usage4`) with frozen `asmctx3`/`compctx1` encoding |

## 10. S1–S8 integrated proof receipts

Verbs executed as separate processes (`RESULT` lines and exact commands in the `*.log` files;
receipts carry `processId`). One native database (system temp, outside the repository) hosts
repository `p1:integration` (frozen corpus publishable subset, `run:corpus`) and repository
`p1:fixture` (adversarial fixture, `run:fixture`) — a separate repository is used for the fixture
so per-project index-unit owners can never replace corpus units of the same project key.

| Stage | Evidence (receipt fields) | Result |
| --- | --- | --- |
| S1 source/semantic extraction | `Q2-INTEGRATION.json`: scenario `files` + `fileHashes` + `compositeHash`; real plugin `PrepareAnalysisAsync`/session over the 12 corpus files and 4 fixture files | ✓ |
| S2 canonical forms/digests | `corpus.expected`/`extraExpected`/`fixture.expected`: literal forms, RefEnc digests, descriptor constituents, composed target (cross-checked production vs frozen primitives — must be equal or the verb throws) | ✓ |
| S3 batch construction/ingestion | `corpusPublish.batches` / `fixturePublish.batches`: 20 + 10 batches over 4 + 2 index units, per-batch owner (plugin id, unit id, run id), `completesIndexUnit` flags, fact counts; 4 + 2 occurrence-authority envelopes captured by the decorating sink and forwarded to the real `CodeGraphIngestionSink` | ✓ |
| S4 staging/publication | Real `StageIndexUnitAsync` (via sink) + `CompleteIndexRunAsync`; run statuses `Completed`; no partial/fabricated completion (all failure paths recorded in the recovery receipts) | ✓ |
| S5 native persistence | `nativeRows`: 2 `AndiritOccurrenceMeta` rows (one per repository, all keys present), 21 `AndiritOccurrencePage` rows (publication-bound), 6 `AndiritOccurrenceUnit` payloads, 6 `HetuUnit` native graph-unit rows; expected target strings present in the native unit payloads for Add (`corpusAdd=true`), Greet (`corpusGreet=true`), Hit (`fixtureHit=true`) | ✓ |
| S6 durable reopen | `Q3-REOPEN.json` (fresh process): `corpusPublishedUnits` (4 durable graph units with fact counts, `requiresOccurrenceAuthority=true`) and `fixturePublishedUnits` (2 units) read through the accepted store interface; queries serve the expected publication identities | ✓ |
| S7 authoritative query/refusal | `corpusAdd`: `Available`, `IsComplete=true`, 8 facts, all hits carry the expected target; `corpusGreet` (explicit-interface member): `Available`, 2 facts; `fixtureHit`: `Available`, 1 fact; stale expected-publication pointer → `StalePublication` (0 facts); plain query without verified-compilation authority → `AnalysisUnavailable` (0 facts, "Current verified compilation authority is required.") | ✓ |
| S8 managed recovery | `Q4-RECOVERY.json` (fresh process): baseline `Available`/`run:corpus`; **known failure** (managed `before-commit` fault at `CompleteIndexRunAsync` on real re-extracted content) → fault surfaced, same instance rebuilt from durable state and still serves `run:corpus` with all 8 facts; **unknown outcome** (managed `after-commit` fault) → `CodeGraphCommitOutcomeUnknownException`; live instance quarantined (reads AND mutations throw); fresh open **reconciles** to the committed truth (`Available`, publication `run:unknown`, 8 facts) — matching the accepted NEW-01/P15 semantics | ✓ |

F-02 boundary (recorded in the receipt): managed fault injection exercises the accepted recovery
contract; it is **not** evidence of actual process crash or power-loss durability.

Durable-envelope note: the composed target ids are recorded in the receipts and are consistent
within the single final harness build across all four processes. See O-1 (§15) for the recorded
scope of target identity — component digests (identifier/declaration/shape) are
host-closure-independent; the composed `asmctx3` target binds the defining compilation including
its reference identities (host build closure and checkout path).

## 11. D3/D4/D5 integrated qualification

| Obligation area | Exercised by P1 | Not exercised (recorded) |
| --- | --- | --- |
| **D3** publication/authority consistency | Batch/authority ownership (S3); authority-envelope ↔ unit binding enforced by the sink; publication binding enforced by the store (`StalePublication` refusal); owner-correspondence and accounting invariants execute on every authoritative query (store-side `CheckOccurrenceAuthorityAsync`), evidenced by `Available` only with matching current publication; cross-projection owner parity enforced at query time | Full D3 surface: superseding publication with *changed sources* (covered by earlier accepted S2.4 evidence, deliberately not re-run); authority retirement paths; multi-writer scenarios |
| **D4** native persistence/reopen/availability/lifecycle/recovery | Native rows (S5); fresh-process reopen (S6); availability + completeness semantics (`IsComplete` with coverage/continuation state); lifecycle Running→Completed; managed known-failure and unknown-outcome recovery with quarantine + reconcile (S8) | **D4.5 and full S-02 remain Stage 6** (excluded by the commission); schema-incompatibility refusal paths (covered by existing tests, see §13 focused run) |
| **D5** incompatible-store refusal / no migration | Tampered native occurrence page (publication binding altered) → fresh open refuses with `CodeOccurrenceIndexException` (fail-closed); storage-layer graph refusal for an edge with a missing endpoint (`graph.edge.endpoint-missing`); no schema migration/dual-write/fallback/silent upgrade/fabricated authority introduced anywhere in the proof | Re-ingestion after refusal (the refusal posture is demonstrated; the rebuild-by-re-ingestion obligation itself was not executed in P1) |

No inference is drawn that every Stage-2 obligation is satisfied because S1–S8 executed.

## 12. Negative and refusal cases (per layer)

| Case | Layer | Observed |
| --- | --- | --- |
| Unresolved/external source binding (`Console.WriteLine`) | producer/extraction | no fact emitted (R15); in-corpus control fact present |
| Invalid canonical target (`"not-a-canonical-target"`) | producer boundary | refused at construction: `ArgumentException` ("asmctx3:sha256: + 64 lowercase hex") |
| Incorrect/stale expected publication | store/authority | `StalePublication`, 0 facts, reason recorded |
| Missing verified-compilation authority (plain query) | store/authority | `AnalysisUnavailable`, 0 facts |
| Tampered persisted occurrence payload | store/native | fresh open refuses (`CodeOccurrenceIndexException`) |
| Edge with missing endpoint (synthetic index unit) | store/graph | `CodeGraphBatchRejectedException` with `graph.edge.endpoint-missing` |
| Quarantined instance after unknown outcome | store/recovery | reads and mutations throw `CodeGraphCommitOutcomeUnknownException`; fresh open reconciles |
| Managed known failure before commit | store/recovery | fault surfaced; durable publication preserved on the same instance |

Layer separation is preserved: the storage-layer refusals establish storage-contract behavior;
they are not presented as proof that the source-level producer is correct (that is the Part A /
S1–S2 chain), and conversely the producer correctness does not establish native authority
enforcement.

## 13. Regression results

All gates from `run-proof.ps1` (exit codes recorded in the logs):

| Gate | Result |
| --- | --- |
| Release build (`dotnet build Andirit.slnx -c Release`) | exit 0 |
| **Full solution tests** (`dotnet test Andirit.slnx -c Release --no-build`) | **907 passed, 0 failed, 0 skipped** — LatticeDb 52, TreeSitter 157, Andirit.Tests 345, Andirit.CSharp 353 (window-native execution) |
| Focused historical/refusal tests (`Store_Rejects*` + `*Refusal*`) | 8 passed, 0 failed |
| **Frozen R2 oracle** (`SemanticCorpusOracle` vs frozen expected r2) | **185/185 checks passed, 0 failed**; produced artifact `oracle-r2.json` **byte-identical**: 146,851 bytes, SHA-256 `97f6c5e5a4cc77e089481c7643732b7a07a31d483c2643bd7a95fdad7ced39c9` |
| Formatting (`dotnet format Andirit.slnx --verify-no-changes`) | exit 0 |
| Contract identity re-verification | 12,636 bytes / `5958dd6b…` (unchanged) |
| **New P1 proof harness** (separate; NOT part of the 907) | known answers **10/10**; form cases **17/17**; matrix **15/18 pairs** (R13a/b/c fail against v1-as-written — F-P1-02); Part B verbs `publish`/`reopen`/`recovery` all exit 0 with receipts |

The oracle verb intentionally exits non-zero when the R13 pair assertions fail; the orchestration
gate validates structure instead: all known-answer and form cases must pass and every matrix
failure must be one of the recorded F-P1-02 rows — otherwise the script itself fails. No
coverage-driven exclusions or skips were added; **F-G2-01 remains OPEN and untouched** (CI gate
unmodified; no exclusions; no test skipping; branch coverage not re-measured and not claimed).

## 14. Evidence integrity

- Evidence package: `docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-evidence/` —
  `Q1-ORACLE.json` (+ `Q1-ORACLE-v1-text-literal-run.json`, the preserved as-written-contract
  reading with `global::` shape literals — forms 3/9, demonstrating F-P1-01 concretely),
  `Q2-INTEGRATION.json`, `Q3-REOPEN.json`, `Q4-RECOVERY.json`, `Q5-TARGET-SCOPE-A/B.json` (the
  controlled O-1 experiment receipts), all per-gate logs (`build`, `release-build`,
  `full-solution-tests`, `focused-historical-tests`, `oracle` + `oracle-r2.json`,
  `format`, `toolchain`), `receipt-summary.log`, `reviewed-head.txt`, `manifest.json`,
  `.gitattributes`/`.gitignore` (exact-byte preservation; raw logs kept).
- `manifest.json` is self-excluding: SHA-256 over the exact bytes of every other evidence file
  (sorted relative paths); sealing reported `mismatches=0`. `reviewed-head.txt` records the
  starting HEAD/tree (`349136d…` / `d7d2373…`), matching the commission preflight.
- Historical packages were not resealed or modified: SQ-04-Q1, BIND-01, G2-EXEC, SQ-05-Q1
  packages and all prior sealed evidence retain their original manifests; no file under
  `docs/evidence/`, `docs/planning/…/corpus/`, or `docs/planning/…/oracle/` was changed (the
  frozen corpus and frozen oracle are byte-untouched; `src/`, `tests/`, and `.github/` are
  byte-untouched).
- The proof database lives outside the repository (system temp) and is retained for inspection;
  the final copy is left tamper-refused by the D5 case at the very end of the recovery verb.

## 15. Findings and qualifications

**Findings (contract-text defects; no production or contract change made in P1):**

- **F-P1-01 — v1 §5 names a nonexistent Roslyn format.** §5 (line 45) requires
  `SymbolDisplayFormat.FullyQualifiedGlobalNamespaceFormat`; no such member exists in
  `Microsoft.CodeAnalysis.CSharp` 5.9.0. The documented format matching all observed behavior
  (special-type keywords `int`/`string`; `global::` for non-special types) is
  `SymbolDisplayFormat.FullyQualifiedFormat`. The preserved `Q1-ORACLE-v1-text-literal-run.json`
  shows the as-written reading (forms 3/9) versus the documented-format reading (17/17).
  **Narrowest remediation:** correct the format name in v1 §5 (owner decision; no implementation
  change).
- **F-P1-02 — v1 §5 commentary and §7 row 13 over-specify `ref`/`out`/`in`.** The shape displays
  parameter *types* (param-kind never appears), and doc-IDs render `ref`/`out`/`in` uniformly with
  `@`. Observed: value-vs-`ref` distinguished by declaration only (shape equal); `ref`/`out`/`in`
  not distinguished by any component (harmless — CS0663 forbids such overloads). R13a/R13b/R13c
  fail against v1-as-written; the implementation is doc-ID-conformant and internally consistent.
  **Narrowest remediation:** correct the §5 sentence and replace row 13's expected relations with
  the observed, C#-sound relations (owner decision; no implementation change). Do not silently
  ignore the failed rows — they are recorded as v1-text defects, not waivers.
- **F-P1-03 (minor) — v1 §4 example uses `global::` inside a doc-ID.** Real doc-IDs never carry
  `global::` (actual: `M:Corpus.Core.Methods.Add(System.Int32,System.Int32)`); the illustration
  `M:Namespace.OwnerType.MethodName(global::System.String)` is misleading. **Remediation:**
  example-only correction.

**Observations (non-blocking, recorded for P2):**

- **O-1 — target identity binds the host build closure.** `asmctx3` composition embeds the
  defining compilation's reference identities, captured as `file|<absolute path>|sha256(bytes)`
  from the host process's `TRUSTED_PLATFORM_ASSEMBLIES` closure. Controlled experiment
  (`Q5-TARGET-SCOPE-A/B.json`): a comment-only harness change + rebuild left all component
  digests identical and changed the composed target. Within one built harness at one checkout
  path, everything is deterministic across processes and runs (all four proof processes agree).
  This is consistent with v1 §8 ("the `compctx1`/`metadataIdentity`/`moduleName` components …
  still bind the defining compilation"), but P2 must compare **component digests and relational
  claims** (equalities, availability, refusals) and re-derive its own run-scoped targets —
  absolute target ids from this package are not expected to reproduce across differently-built
  hosts or different checkout paths.
- **Scope note — corpus publish subset.** Part B publishes the accepted frozen R2 *publishable
  subset* (App, Core, Leaf, Solo; 12 files — the FINAL-Q1 precedent). `Negatives` (intentionally
  non-compiling refusal scenarios) and `External/ExternalLib.cs` (the frozen oracle's in-memory
  metadata image; explicitly "not a declared project unit of the corpus universe") remain
  oracle-side artifacts; unclaimed sources stage no index units. The frozen oracle running over
  the full corpus (185/185) is the accepted evidence for those artifacts.
- **F-G2-01** remains `OPEN — REPOSITORY CI BRANCH-COVERAGE GATE FAILURE` (296/450 = 65.77% <
  70%); untouched by P1; separate owner decision remains outstanding.
- No defect blocked the integrated proof; nothing was excluded to obtain agreement; no mandatory
  case was reclassified to enable P2.

## 16. P1 disposition

**`S-01 P1 INTEGRATED PROOF COMPLETE WITH QUALIFICATIONS`.**

Rationale against the §19 disposition criteria: (1) all applicable mandatory canonical-form cases
pass **except** the three v1 §7 row-13 assertions, which fail *against v1-as-written* and are
attributed by smallest-reproduction analysis to contract-text defects (F-P1-02) with no
implementation contradiction; (2) independent expected values are constructible from v1
(KAT/forms/matrix digests from `RefEnc`); (3) the real source-to-native-store path executes
successfully end-to-end; (4) durable reopen and applicable authority behavior are demonstrated;
(5) applicable D3/D4/D5 cases pass with the exclusions recorded in §11; (6) regression baselines
are intact (907/0/0; frozen oracle byte-identical; format clean); (7) evidence is complete,
reproducible, and sealed. Because a failed mandatory row cannot be self-reclassified as
non-blocking, the disposition is **WITH QUALIFICATIONS**, not "ready for qualification" clean.

Remaining work, precisely categorized:

- **Contract clarification (owner decision, not P1/P2):** narrow corrections for F-P1-01/02/03 in
  v1 §4/§5/§7. No code remediation is requested or identified.
- **Additional evidence:** none required for the exercised scope; the recorded untested
  assumptions (universal shape-collision freedom; D3 supersession/multi-writer; D5 re-ingestion;
  D4.5/S-02) remain future work as scoped by the commission.
- **Independent qualification:** P2 (below).
- **Owner acceptance:** pending, separate gate.

**S-01 is not marked satisfied by P1.**

## 17. P2 independent qualification recommendation

A different reviewer can reproduce this proof without trusting the P1 executor's conclusions:

1. **Checkout and build**: at the P1 commit (recorded in `reviewed-head.txt`/commit history), run
   `powershell -NoProfile -ExecutionPolicy Bypass -File docs/reviews/CI1A-S2.5-WP-S25-3-P1-PROOF-tests/run-proof.ps1`.
   Requires Windows x64, .NET SDK 10.0.401, network-restored NuGet (Roslyn 5.9.0, LatticeDbSharp
   0.2.0); the proof database is created in system temp.
2. **Re-derive expectations**: read the contract §3–§7, then re-derive the form table and digests
   independently (KAT-1's hand-written hex is the recommended starting point). Compare component
   digests (`identifierDigest`/`declarationDigest`/`shapeDigest`) — these are
   host-independent. For composed targets, re-run within your own build and check the relational
   claims (target present in native unit payloads; queries return `Available` with all hits
   matching your recomputed target; refusals as listed) rather than string equality with this
   package's run-scoped targets (O-1).
3. **Verify the failing rows yourself**: confirm R13a/b/c behavior against a minimal fixture and
   decide whether you agree that the defect is in the contract text (F-P1-02) — the failed-row
   evidence must not be waived by either party.
4. **Evidence verification**: re-hash every file in the evidence package against `manifest.json`
   (self-excluding; sorted paths; SHA-256 exact bytes) and verify `oracle-r2.json` (146,851 bytes;
   `97f6c5e5…`).
5. **Known limitations to disclose in P2**: producer-created oracle evidence (this package) is not
   independently qualified by construction; target ids are build-scoped (O-1); the publishable
   corpus subset excludes the oracle-side artifacts (scope note); F-G2-01 remains open;
   managed fault injection is not crash/power-loss durability evidence (F-02).
