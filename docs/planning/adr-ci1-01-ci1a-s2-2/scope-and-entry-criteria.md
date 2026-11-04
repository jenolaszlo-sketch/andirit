# CI1A S2.2 scope and entry criteria

**Commission:** `ADR-CI1-01-CI1A-S2.2-ENTRY-01`. **Mode:** documentation only.
**Baseline:** canonical `main` `a1cf47083747bae90293ec074b35fe8078bd0d6e`
(tree `ebba0daf2977315a5c09551df0de698afeb6696f`), one documentation-only
descendant of qualified R2 `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5`.
**Disposition:** `S2.2 ENTRY PREPARATION COMPLETE — READY WITH EXPLICIT
QUALIFICATIONS`. **S2.2 implementation is NOT authorized.** This document
prepares a separate owner authorization; it does not grant one.

---

## 1. Purpose and authority

S2.2 is the Stage 2 slice that turns the accepted S2.1 verified compilation
provenance into **real Roslyn semantic occurrence extraction and canonical
target minting**. It is defined by
[stage2-boundary §8](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md#8-stage-2-staging-proposal)
("S2.2: Roslyn Occurrence Extraction") and bounded by the
[Stage 2 acceptance matrix](../adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md).

Controlling authority, in precedence order:

1. [`adr-ci1-01-owner-decision.md`](../../decisions/adr-ci1-01-owner-decision.md)
   §F and its [Q1 acceptance](../../decisions/adr-ci1-01-owner-01-q1-acceptance.md).
2. Frozen R2 `PROVENANCE-CONTRACT.md` §§2–5 and Q3 `TARGET-IDENTITY-REVIEW.md`,
   incorporated by owner §F and transcribed in the
   [identity contract](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md).
3. [`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md)
   — `S2.2-ID OPTION A ACCEPTED`; exact mapping contract (§5.1) and required
   tests (§5.2).
4. [`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md)
   §F/§H and the
   [S2.1 provenance contract](../adr-ci1-01-ci1a-s2-1/provenance-contract.md).
5. [`adr-ci1-01-ci1a-s2-obligation-register.md`](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md)
   §§4A, 5, 7, 8 and the S2.0/F12
   [owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md).
6. [`adr-ci1-01-pre-release-compatibility-policy.md`](../../decisions/adr-ci1-01-pre-release-compatibility-policy.md).

Where the older [Stage 2 boundary](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md)
and [handoff](../adr-ci1-01-ci1a-impl-01/stage2-handoff.md) assign native
publication to S2.2 via the existing ingestion path, the later accepted register
and S2.1 boundary control: **native source-owned publication is S2.3**, and S2.2
must not build the publication path. This reconciliation is stated explicitly in
§6.5 and is one of the required owner decisions in §9.

---

## 2. Recovered S2.2 objective and deliverables

From [stage2-boundary §8](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md#s22-roslyn-occurrence-extraction),
the [S2.2 slice decomposition](../adr-ci1-01-ci1a-impl-01/s2-2-identity-handoff.md#7-suggested-s22-slice-decomposition-future-authorization-only)
and the [current work queue](../adr-ci1-01-ci1a-impl-01/s2-0-owner-handoff.md#remaining-work-queue-and-gate-order):

**Objective.** Extract supported `Invocation` occurrences from actual verified
Roslyn bindings, resolve each to an exact defining declaration in a verified
live compilation, mint canonical `asmctx3`/`usage4` identities under Option A,
refuse unsupported/unresolved/ambiguous/stale/unverified cases with correct
coverage, satisfy the Option A mapping and durability gates, and demonstrate the
result against the frozen, independently accepted R2 semantic oracle used as a
**bounded oracle, not a production correctness guarantee**.

**Deliverables (recovered, not invented):**

| # | Deliverable | Source |
| --- | --- | --- |
| D1 | Deterministic R2-faithful `compctx1` derivation for verified **dependency-bearing** defining compilations, with independent vectors; this is a mapping, not a redefinition | Option A §5.1(1); identity mapping §2 |
| D2 | A reviewed S2.2 compatibility contract to **consume `compctx2` and mint** `asmctx3`/`usage4` without letting `compctx2` enter either identity and without changing frozen R2 logic | S2.1 provenance contract §4; Option A §5.1 |
| D3 | Canonical target minting (`asmctx3`) from the verified defining compilation, with live-instance/live-context and `compilation-universe1` membership gating | Option A §5.1(2); stage2-boundary §3.5 |
| D4 | Supported `Invocation` extraction from the same live Roslyn compilation used for graph facts: exact invocation-token span, full declaration span as perpendicular evidence, actual source hash, category `method-invocation` | stage2-boundary §3.6–§3.7; acceptance matrix §4 |
| D5 | `usage4` occurrence fact construction (frozen tuple) with calling/source-unit ownership distinct from the storage contributor | identity contract §2; stage2-boundary §3.7/§6 |
| D6 | Coverage/refusal emission: `Confirmed`, `External`, `Ambiguous`, `Unresolved`, `Unsupported`, `ConditionalInactive`, `Incomplete` — no state collapses to successful empty | stage2-boundary §3.9/§5; [coverage-and-availability](../adr-ci1-01-ci1a-s2-rem-01/coverage-and-availability.md) |
| D7 | Bounded unconditional-inactive scanner bound to declared language/preprocessor configuration (F09), without a confirmed-fact path for inactive sites | register §3 F09; acceptance matrix §5 |
| D8 | NEW-04 coverage-authority correction (or accepted disposition) for the occurrence-coverage merge, independently qualified before S2.2 acceptance | register §4 NEW-04; S2.1 boundary §4 |
| D9 | Option A invalidation/refusal: a defining `compctx2` change (including transitive-only) invalidates prior facts; stale publication refused/re-ingested; no silent reuse; no identity churn | Option A §5.1(4)(6)/§5.2(2,8) |
| D10 | Durable defining-`compctx2` provenance association (or an equivalent unambiguous association) recorded so the validity gate is enforceable after reopen — **subject to the S2.2/S2.3 boundary decision in §6.5** | Option A §5.1(5); Option A §7 |
| D11 | Integration and comparison tests over the accepted S2.0/R2 corpus, with expected results taken from the frozen oracle and never generated by the extractor under test | oracle contract §§2–3; [case matrix](../adr-ci1-01-ci1a-s2-0/case-matrix.md) |
| D12 | Producer evidence, F16 nine-part traceability rows, independent Q1 review and owner acceptance | register §3 F16; stage2-handoff §5 |

---

## 3. Dependencies

### 3.1 Incoming accepted baselines (satisfied)

- **S2.1** — verified immutable source buffers, strict BOM-aware decoding,
  byte/tree correspondence, actual options/references, live project-reference
  binding, `compctx2`, `analysisscope1`, `compilation-universe1`
  (`ACCEPTED WITH QUALIFICATIONS`, S21A).
- **Option A identity direction** — `S2.2-ID OPTION A ACCEPTED` (S22ID).
- **S2.0/F12 frozen R2 oracle** — `ACCEPTED WITH QUALIFICATIONS` (OWNER-01,
  2026-11-03). Base corpus independently supports 11 files / 53 invocations /
  21 method references / 5 overloads / 5 namespaces / 5 projects / 52
  cross-project calls.
- **Stage 1 Candidate C native foundation / REM-01** — accepted with
  qualifications; native publication is consumed at S2.3, not S2.2.

### 3.2 Production surfaces S2.2 may extend (pinned at `a1cf470…`)

| Surface | File | Role |
| --- | --- | --- |
| Frozen identity helpers | `src/Andirit.Abstractions/CodeOccurrence.cs` (`CodeOccurrenceIdentity.ComputeTargetId`, `ComputeOccurrenceId`, `CodeOccurrenceFact`, `CodeOccurrenceCoverage`, `CodeOccurrenceTargetId`) | minting/validation; **frozen, must not be redefined** |
| Verified analysis result | `src/Andirit.CSharp/VerifiedCSharpAnalysis.cs` (`VerifiedCSharpAnalysis`, `VerifiedCSharpScope`, `VerifiedCSharpUniverse`, `IVerifiedCSharpSession.Provenance`) | S2.1 live scope/universe/compilations for extraction |
| Provenance descriptor/live proof | `src/Andirit.CSharp/CompilationProvenanceDescriptor.cs`, `VerifiedCSharpCompilation.cs`, `VerifiedCSharpSource.cs` | R2-faithful fields for `compctx1` mapping and `compctx2` validity |
| Extraction session | `src/Andirit.CSharp/CSharpCodeGraphPlugin.cs`, `.Session.cs`, `.GraphBuilder.cs`, `.Compilation.cs`, `.Symbols.cs` | same live compilation used for graph facts |
| Discovery | `src/Andirit.CSharp/CSharpProjectDiscovery.cs` | declared project/source inventory (bounded, not MSBuild-equivalent) |
| Sink/ingestion | `src/Andirit/CodeGraphIngestionSink.cs`, `CodeGraphBatchValidator.cs`, `src/Andirit.Abstractions/CodeGraphBatch.cs` | existing coverage/fact carrier (publication lifecycle is S2.3) |
| Coverage model | `src/Andirit/CodeGraphStoreModels.cs`, `src/Andirit.Abstractions/CodeOccurrence.cs` | `CodeOccurrenceCoverage.Merge` is the NEW-04 defect location |
| Internal reader | `src/Andirit/CodeOccurrenceReader.cs`, `ICodeOccurrenceReader` | internal-only; continuation is S2.4 |

### 3.3 Downstream consumers (gated)

- **S2.3** native source-owned publication/reopen/lifecycle/replacement/deletion;
  requires NEW-01 correction first.
- **S2.4** bounded internal reader continuation/coverage.
- **S2.5** integrated independent proof and Stage 2 acceptance.
- **Stage 6 / later** full D3/S-02/platform/crash qualification.

---

## 4. Contracts S2.2 must implement or preserve

### 4.1 Mapping contract (Option A §5.1 — binding)

1. `asmctx3` is computed with the frozen R2 algorithm over the R2-faithful
   `compctx1` of the **defining** compilation, derived deterministically from
   S2.1-verified material. The dependency-bearing derivation is a **mapping**,
   not a redefinition, and is **not** a strong verified project-reference claim —
   the strong claim is carried by `compctx2`.
2. Provenance validity requires the defining compilation's current `compctx2`
   (including transitive dependency contexts) to equal the context under which
   the occurrence was produced, plus `compilation-universe1` membership. A bare
   `asmctx3` is not provenance.
3. `usage4` remains frozen and uses the target's `asmctx3`.
4. Any defining-`compctx2` change (including transitive-only) invalidates prior
   occurrence facts; stale facts are refused or re-ingested, never silently
   reused.
5. S2.2 records the defining `compctx2` (or equivalent unambiguous association)
   so the gate survives reopen; if unenforceable, return to the owner before
   minting (potential Option B).
6. No identity churn: dependency-context-only changes do not re-identify an
   unchanged declaration.
7. No conflation: analysis generation, publication freshness and storage schema
   identity remain separate from target identity and provenance validity.

### 4.2 Frozen identities (must not change)

- `usage4` = length-prefixed SHA-256 over `usage4`, repository, plugin,
  `usage4-b2c-exp-r2-20261101`, semantic source unit, path, source hash,
  start/end line/column, target document, `asmctx3`, `method-invocation`.
- `asmctx3` = `asmctx3:sha256:` + domain-separated SHA-256 over
  `asmctx3-context`, `compctx1`, canonical `compctx1` value, metadata identity,
  module, identifier digest, declaration digest, shape digest.
- `compctx1` frozen R2 descriptor algorithm and vectors.
- `compctx2` additive versioned provenance; project-reference items
  `compilation-context|<dependency compctx2>`.
- 32-bit big-endian byte-length prefixes; coordinates as invariant decimal
  strings; 64 lowercase hex, no occurrence prefix; canonical target
  `asmctx3:sha256:<64 lowercase hex>`.
- Storage contributor excluded from `usage4`; plugin and semantic source unit
  included.

### 4.3 Semantic identity vs validity separation

`asmctx3` equality never substitutes for `compctx2` validity or universe
membership. `compctx2` never enters `usage4`/`asmctx3`. The frozen legacy
helpers reject `compctx2`; S2.2 needs a separately reviewed compatibility
contract (D2) before consuming it.

---

## 5. Supported languages, platforms, categories and data boundaries

| Dimension | S2.2 boundary |
| --- | --- |
| Language | **C# only** (Roslyn). Tree-sitter, TypeScript, JavaScript, Elixir and arbitrary language support are excluded |
| Supported fact category | **`Invocation`** (ordinary method invocation → `method-invocation`) |
| Deferred categories | property, field, event member, method group, delegate invocation, local function, type reference, constructor — recorded as coverage, never as confirmed facts |
| Platform | Windows x64; .NET SDK 10.0.401; runtime 10.0.12; Roslyn 5.9.0 (recorded S2.1/S2.0 environment). Linux minimum is S2.5; full platform certification is Stage 6 |
| Data | Experimental occurrence identities/schema only. No migration, dual-write, legacy readers or adapters; superseded data refused or re-ingested (POL-02…POL-06) |
| Protected history | D3 graph semantics, D4 publication authority, D5 graph/Hetu aggregate compatibility remain untouched |
| API surface | Internal-only. Public Find Usages remains ADR-CI1-02; no public API, UI or endpoint in S2.2 |
| Completeness | Scope-relative only; no universal semantic-completeness claim |

---

## 6. S2.2 entry contract

### 6.1 Preconditions for implementation authorization (R-AUTH)

The owner may authorize S2.2 implementation only when all of the following are
recorded. Items marked ⧉ are **already satisfied**; the rest are preparation
outputs or explicit owner decisions.

| # | Precondition | State at this preparation |
| --- | --- | --- |
| P1 | S2.1 owner acceptance recorded | ⧉ Satisfied (S21A) |
| P2 | Option A identity direction decided | ⧉ Satisfied (S22ID) |
| P3 | S2.0/F12 frozen R2 oracle accepted with qualifications | ⧉ Satisfied (OWNER-01, 2026-11-03) |
| P4 | This entry-criteria package accepted by the owner | Pending owner decision |
| P5 | **Q-F2** supported-encoding decision recorded (correct decode/refusal **or** explicit supported-vs-unsupported/ambiguous encoding decision) | Pending owner decision |
| P6 | **Q-F3** metadata-validation decision recorded (eager PE/assembly validation **or** explicit unsupported-reference decision) | Pending owner decision |
| P7 | **NEW-04** disposition recorded (correct the coverage merge in S2.2 and independently qualify it before S2.2 acceptance **or** an accepted bounded disposition) | Pending owner decision |
| P8 | **A-01** disposition recorded (bounded hardening or explicit acceptance that S2.2 creates no public/caller-supplied parse-option reuse) | Pending owner decision |
| P9 | **A-02** boundary acknowledged (scope identity is not a source-inventory digest; S2.2 must not treat it as one) | Pending owner decision |
| P10 | **Option A durability / S2.2–S2.3 boundary** decision recorded: how D10 is satisfied within S2.2 without native publication, or an explicit qualification/deferral | Pending owner decision |
| P11 | **NEW-01** non-reliance confirmed: S2.2 does not depend on failure/occurrence-projection recovery; NEW-01 stays before S2.3 and any earlier recovery reliance | Pending owner decision |
| P12 | **F16** nine-part traceability rows prepared for the S2.2 slice, and the NEW-02-corrected identity matrix independently confirmed, **before dependent qualification-test authoring** | Pending (pre-authoring gate; may be a first authorized work package) |
| P13 | No open obligation reached earlier than its accepted gate | Verify at authorization |

**Important ordering.** Option A §7 and §5.1(5) place two items inside S2.2:
the **mapping contract must be implemented and independently reviewed before
target minting**, and the **durable association must be enforceable before
minting**. The entry contract therefore authorizes the *mapping/contract*
packages first and holds the *minting* packages behind an internal independent
review gate (see §6.2 G-MINT). This is not a new requirement; it is the accepted
Option A gate made explicit.

### 6.2 Bounded implementation work packages

Each package is independently reviewable and stops at the stated gate. No
package authorizes the next.

| WP | Work | Internal gate |
| --- | --- | --- |
| **WP1** | R2-faithful `compctx1` derivation for verified dependency-bearing defining compilations + independent vectors and determinism proof | Deterministic vectors reproduced by an independent implementation |
| **WP2** | S2.2 compatibility contract: consume `compctx2` for validity while minting `asmctx3`/`usage4` from `compctx1`; guard rails prove `compctx2` cannot enter either identity | **Independent review of the compatibility + mapping contract** (G-MINT) |
| **WP3** | Invocation extraction from the same live compilation used for graph facts: invocation-token span, full declaration span, source hash, `method-invocation` category | Focused positive/negative semantic tests |
| **WP4** | Target minting under the mapping contract with exact live defining-compilation instance, `compctx2` equality and `compilation-universe1` membership gating | G-MINT passed; adversarial target-binding tests |
| **WP5** | Coverage/refusal emission and unconditional-inactive scanner (F09) with no confirmed fact for inactive/external/ambiguous/unresolved/unsupported/incomplete | Refusal matrix |
| **WP6** | NEW-04 coverage-authority correction (or accepted disposition) with declaration/verification/merge semantics fixed | Independent qualification before any coverage-dependent claim |
| **WP7** | Option A invalidation/refusal: transitive-only `compctx2` change invalidates facts; stale publication refused; no churn; durable association per P10 | Option A §5.2 tests 2, 6, 8 |
| **WP8** | Integration/comparison over the accepted S2.0/R2 corpus with frozen expected results; F16 rows; reproducibility evidence | Independent Q1 review |
| **EXCL** | Native publication, reopen, lifecycle/replacement/deletion, reader continuation, public API, background/incremental indexing | **S2.3/S2.4/Stage 6 — excluded** |

### 6.3 Invariants that must not change

- `usage4`, `asmctx3`, `compctx1` formulas/domains/encoding and frozen vectors.
- `compctx2` additive amendment, `analysisscope1`, `compilation-universe1`
  contract-version `compctx2` and no mixed-version universe.
- Semantics version `usage4-b2c-exp-r2-20261101`; category mapping
  `method-invocation`; canonical string shapes; payload-hash conflict rejection.
- Storage contributor excluded from `usage4`; semantic source unit included;
  calling-unit vs defining-unit ownership distinct.
- D3 graph noninterference, D4 publication authority, D5 no-migration.
- Internal-only reader/API contract; no public Find Usages.
- No new semantic identity scheme and no shortcut via documentation IDs, paths
  or symbol descriptors.

### 6.4 Independent verification and adversarial tests

Expected results come from the **frozen R2 oracle / case matrix**, never from the
extractor under test. Required independent/adversarial coverage:

- **Option A §5.2 (all ten):** E1 conditional API change → `asmctx3` changes;
  E2 transitive-only change → `asmctx3` stable while `compctx2` changes at all
  levels and stale facts refuse; E3 caller unchanged, target follows defining
  API; restore → exact original identities; distinct defining compilations →
  distinct targets; equivalent reference artifact → equality per accepted
  contract; re-ingestion determinism; stale publication under changed context
  refused including after reopen; deterministic documented mapping for
  dependency-bearing compilations; negative: no target/occurrence from an
  unverified/incomplete context and `compctx2` cannot enter
  `usage4`/`asmctx3`.
- **Adversarial target binding:** equal-descriptor distinct compilations →
  distinct `asmctx3`; omitted/unresolved/unbound contexts → refusal; linked
  contexts → distinct definitions; cross-project positive → confirmed.
- **Fabricated authority:** canonical-shaped but unverified target/context
  refused; caller-supplied booleans/descriptors rejected.
- **Coverage:** `External`, `Ambiguous`, `Unresolved`, `Unsupported`,
  `ConditionalInactive`, `Incomplete` never become confirmed facts or
  complete-empty; complete-empty only under a verified complete scope.
- **Reader/validity guard:** `compctx2` present in `asmctx3`/`usage4` is
  structurally impossible.

### 6.5 The S2.2/S2.3 boundary and Option A durability

Option A §5.1(5) requires S2.2 to record the defining `compctx2` "in
publication/coverage evidence so the validity gate is enforceable after
reopen," while the accepted register and S2.1 boundary place **native
publication at S2.3**. This preparation does not silently resolve the tension.
The owner must decide one of:

- **(a)** S2.2 records the association in the occurrence coverage/provenance
  model and demonstrates in-memory enforcement plus reopen-enforceable design,
  with durable native persistence proven at S2.3 as the first S2.3 gate; or
- **(b)** S2.2 implements the minimal durable association inside the existing
  occurrence-evidence carrier without building the S2.3 publication path; or
- **(c)** the owner amends the Option A durability requirement (e.g., to revisit
  Option B) if neither can be enforced before minting.

**If this association cannot be enforced, S2.2 minting must stop and return to
the owner (potential Option B) as required by Option A §5.1(5) and §7.**

### 6.6 Quantitative and qualitative acceptance criteria

**Quantitative (frozen R2 oracle / corpus):**

| Requirement | Minimum | R2 supported |
| --- | ---: | ---: |
| C# source files | 10 | 11 |
| Invocations | 50 | 53 |
| Method references | 20 | 21 |
| Overloaded methods | 5 | 5 |
| Namespaces | 3 | 5 |
| Projects | 2 | 5 |
| Cross-project invocations | 5 | 52 |

- Deterministic byte-identical output across repeated and separate processes.
- Reviewers reproduce independently; producer runs are not qualification.

**Qualitative:**

- Canonical minting only from verified live defining compilations.
- Conservative refusal on every unsupported/unresolved/ambiguous/stale/
  unverified case; no empty success.
- Option A separation preserved; no identity churn.
- Complete-empty only under verified complete scope.
- No D3/D4/D5 weakening; no public API; no migration.

### 6.7 Evidence and reproducibility requirements

- Actual inputs (bytes/hash/path/encoding, options, references, live
  instances), observed identities, refusal reasons and reproduction commands.
- A hash of supplied content is insufficient if Roslyn compiled different
  content ([S2.1 boundary §6](../../decisions/adr-ci1-01-ci1a-s2-1-authorization-boundary.md)).
- Expected results frozen and independently established; no self-generated
  oracle; frozen oracle immutability and version naming preserved.
- Self-excluded manifests; raw logs; TRX; environment; no future test credited
  as passing.
- F16 nine-part rows per case: requirement, authority, production surface,
  observable behavior, positive test, adversary, environment, evidence
  artifact, closure authority.

### 6.8 Explicit stop conditions

Stop and return to the owner if:

- `compctx2` cannot be consumed without changing a frozen identity or entering
  `usage4`/`asmctx3`.
- The Option A durable association cannot be enforced before minting.
- A defining compilation cannot be bound to an exact live instance / verified
  `compctx2` / universe membership.
- S2.2 would rely on NEW-01 recovery, weaken D3/D4/D5, or require a new
  semantic identity scheme.
- A public Find Usages API becomes mandatory.
- Q-F2/Q-F3 cannot be decided or their accepted limits would be silently
  exceeded.
- Scope expands into S2.3 native publication, S2.4 reader continuation, or
  Stage 6 qualification without separate authorization.

### 6.9 Required owner decisions (see the handoff for wording)

1. Accept this entry-criteria package and issue (or decline) the separate S2.2
   implementation commission.
2. Q-F2 supported-encoding decision.
3. Q-F3 metadata-validation decision.
4. NEW-04 disposition.
5. A-01 disposition.
6. A-02 acknowledgment (standing constraint).
7. Option A durability / S2.2–S2.3 boundary (§6.5).
8. NEW-01 non-reliance confirmation.
9. Approve F16 nine-part rows and NEW-02 independent confirmation as the
   pre-authoring gate.

---

## 7. R2 oracle as a bounded oracle, not a production guarantee

The accepted R2 corpus is an independent correctness reference for S2.2
comparison only. It is **not** a production correctness guarantee. Retained
bounds (from S2.0 OWNER-01 §7 and the
[qualification plan](../adr-ci1-01-ci1a-s2-0/qualification-plan.md)):

- `identifierDigest`/`declarationDigest`/`shapeDigest` are **fixture-declared**
  canonical forms, not S2.2 source canonicalization.
- Certain `asmctx3` values are host-toolchain-bound (pinned runtime 10.0.12);
  semantic/relational expectations are portable, exact target bytes are not
  portable without fixture qualification.
- The corpus is purpose-built, not an exhaustive arbitrary-repository corpus.
- Coverage/lifecycle/provenance are modeled; no native/durable production claim.
- Windows-only reproducibility.
- No complete-empty replacement, production recovery or native publication.

S2.2 must still implement and qualify general source canonicalization (the
fixture-declared digest forms do not satisfy it).

---

## 8. Explicit exclusions and deferred capabilities

Not authorized by S2.2: public Find Usages (ADR-CI1-02); non-C# languages;
Tree-sitter extraction; new graph traversal semantics (D3); B2; experimental
schema migration; dual-write; legacy readers/adapters; background/distributed
indexing; broad LatticeDB refactoring; native-node-ID optimization; incremental
page maintenance; package publication; full cross-platform certification;
native publication/reopen/lifecycle (S2.3); reader continuation/cross-target
surface (S2.4); integrated Stage 2 acceptance (S2.5); cancelled-staging and full
D3/S-02/platform/crash qualification (Stage 6).

No "future work" entry waives a correctness condition. If a dependency is
reached earlier, its gate moves before that reliance.
