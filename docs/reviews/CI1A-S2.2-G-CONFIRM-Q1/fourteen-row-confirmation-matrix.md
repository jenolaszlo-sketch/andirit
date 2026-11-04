# G-CONFIRM — fourteen-row independent confirmation matrix

**Commission:** `ANDIRIT-CI1A-S2.2-G-CONFIRM-Q1`. **Candidate:** `e0d911055f4cc9bdbf9aa4cc338f5cc007737a22`
(tree `ba9338d74bf59c584bef340dbda49ed46eb1c5d9`). **Date:** 2026-11-04.

Authority for the rows is the corrected
[G-CONFIRM package §2](../../planning/adr-ci1-01-ci1a-s2-2/g-confirm-package.md#2-nine-part-rows),
read together with the
[execution-readiness handoff §D](../../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md#d-f16-nine-part-traceability-rows-for-s2-2).
The nine-part schema is preserved exactly:
**requirement, authority, production surface, observable behavior, positive test,
adversary, environment, evidence artifact, closure authority.**

Disposition vocabulary: **CONFIRMED**, **CONFIRMED WITH QUALIFICATIONS**,
**INSUFFICIENT EVIDENCE**, **CONTRADICTED**.

Summary: **CONFIRMED 4** (rows 1, 3, 4, 6); **CONFIRMED WITH QUALIFICATIONS 10**
(rows 2, 5, 7–14); **INSUFFICIENT EVIDENCE 0**; **CONTRADICTED 0**.
Overall gate verdict: **PASS WITH EXPLICIT QUALIFICATIONS** (see
[REPORT.md](REPORT.md)).

---

## Row 1 — Q-F2 supported-encoding set + ambiguous BOM-less refusal

| Nine-part field | Value |
| --- | --- |
| Requirement | Supported-encoding set + ambiguous BOM-less refusal (Q-F2) |
| Authority | S21A §F; register §4A; entry acceptance §3.1; foundation owner acceptance |
| Production surface | `VerifiedCSharpSource` (`ReadAsync`) |
| Observable behavior | Refusal, no verified identity |
| Positive test | BOM UTF-8/16/32 accepted |
| Adversary | BOM-less UTF-16/32 NUL decode → `AmbiguousBomlessEncoding` |
| Environment | native (Roslyn) |
| Evidence artifact | TRX + log |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED.**
The claim is fully determined by the accepted contract, not by producer output.
`VerifiedCSharpSource.cs` implements strict UTF-8 ±BOM, strict UTF-16/32 with a
recognized BOM (UTF-32 checked before its UTF-16 prefix), and the bounded rule:
a BOM-less decode that yields U+0000 refuses as `AmbiguousBomlessEncoding:<path>`
with no verified identity; strict decoder failure is `InvalidSourceEncoding`.
The bounded limitation is part of the accepted contract (a legitimate BOM-less
UTF-8 file containing NUL is refused; `41 41` is indistinguishable). Q1 §3
independently exercised 28 encoding cases; Q2 §3/§6 and R1 preserve them.
Expected values are explicit input bytes and fixed refusal tokens, independent of
future implementation. No unresolved authority question.

## Row 2 — Q-F3 bounded assembly/metadata capture boundary

| Nine-part field | Value |
| --- | --- |
| Requirement | Bounded assembly/metadata capture boundary (Q-F3) |
| Authority | S21A §F; register §4A; Q1-F01 qualification; Q2 bounded surface; foundation owner acceptance |
| Production surface | `VerifiedMetadataReference.Capture` |
| Observable behavior | Valid supported metadata captured; malformed assembly-name, type-name and method-signature heap handles refuse before identity; no universal PE/metadata validity guarantee |
| Positive test | Valid assembly and supported metadata extent |
| Adversary | Corrupt assembly-name/type-name/method-signature heap indices, truncation, garbage, netmodule inputs |
| Environment | native |
| Evidence artifact | TRX + exact malformed image details + disposition |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
`Capture` is the sole identity constructor (private); it acquires immutable bytes
(bounded 128 MiB), hashes, forces Roslyn assembly materialization, applies the
`IsAssembly` manifest guard, runs `MetadataReferenceValidation.Validate`, then a
final cancellation check. The validator dereferences all TypeRef/TypeDef
name/namespace, all MethodDef names/signatures (full blob traversal, headers,
instance/static, arity, counts, placements, local token bounds, arrays, nested
function pointers, trailing bytes), GenericParam owner/index order, non-nil
method owner and unique Param sequences. The three Q1-F01 images refuse
deterministically; the exact byte hashes are frozen.
Qualification carried: **exactly** the Q2 §3 bounded surface. Documented
excluded surfaces (malformed Field signature `FF`, invalid TypeRef
ResolutionScope, malformed modifier TypeSpec syntax `FF`) still capture; no
universal PE/metadata/IL validity or semantic-usability claim is made.
**Correction C1:** handoff §D row 2 ("Eager PE/assembly validation ... Every
syntax-tree/`GetMetadata()` ...") is superseded by this bounded wording; the
corrected row must be used.

## Row 3 — A-01 symbol canonicalization hardening

| Nine-part field | Value |
| --- | --- |
| Requirement | Symbol canonicalization hardening (A-01) |
| Authority | S21A §F; entry acceptance §3.3 |
| Production surface | `VerifiedCSharpCompilation.Create` |
| Observable behavior | `UnsupportedParseConfiguration` |
| Positive test | Ordinary symbols deterministic |
| Adversary | `,` / `\|` symbol refused |
| Environment | native |
| Evidence artifact | TRX |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED.**
Preprocessor symbols containing `,` or `|` refuse; symbol reordering preserves
the descriptor digest; composed/decomposed `é` remain ordinally distinct. The
changed surface is internal (no public/caller parse-option reuse in S2.2). Q1 §5
and R1 independently reproduce comma/pipe/mixed injections. Expected values are
fixed separator inputs and digest comparison; not derived from future output.

## Row 4 — A-02 scope is not source inventory

| Nine-part field | Value |
| --- | --- |
| Requirement | Scope is not source inventory (A-02) |
| Authority | S21A §F; S2.1 contract §3; entry acceptance §3.4 |
| Production surface | `VerifiedCSharpScope` / `VerifiedCSharpUniverse` |
| Observable behavior | Membership from universe |
| Positive test | Matching scope/universe accepted |
| Adversary | Supply-source mismatch refused |
| Environment | native |
| Evidence artifact | test + statement |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED.**
`analysisscope1` hashes only the declared repository + project-unit/path
boundary; per-unit source membership is enforced by `VerifiedCSharpUniverse`
(`ScopeSourceMembershipMismatch`) and each `compctx2` descriptor. Q1 §5 and Q2 §5
independently observe the same-scope/different-source-membership falsification.
Consumers must not treat the scope digest as a source-inventory digest.
Extraction-time consumption is deferred (row 12), which is not an authority gap.

## Row 5 — R2-faithful `compctx1` derivation under a declared comparison profile

| Nine-part field | Value |
| --- | --- |
| Requirement | R2-faithful `compctx1` derivation under a declared comparison profile |
| Authority | S22ID §5.1(1); mapping contract §2 and its comparison-profile table |
| Production surface | `CompilationProvenanceDescriptor` plus the frozen R2 oracle |
| Observable behavior | Equal complete inputs yield equal mapped digests; the frozen corpus digest is required only under its complete fixture profile |
| Positive test | Independent same-input vector; separately, fixture-aligned Leaf/Core anchors |
| Adversary | Different labels/inventory are not treated as equal inputs; omitted dependency or nondeterminism refused |
| Environment | native |
| Evidence artifact | full profile/inventory + vectors + oracle log |
| Closure authority | IR+O (G-MINT) |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Two distinct profiles are accepted and disclosed: (1) algorithm fidelity over
the same complete inputs with actual verified reference identities; (2) exact
frozen-fixture reproduction using the corpus's three `file|runtime/...` labels
over accepted runtime 10.0.12 bytes. Frozen anchors
`Leaf = compctx1:sha256:404c5e48…` and `Core = compctx1:sha256:942a8e58…` apply
only under the complete fixture profile; the actual-path values on this host are
`…b816dcb5…` / `…df8d2932…`, and recovery of the anchors requires a **test-only**
comparison-label alignment that must never enter production. The R2 project item
is a faithful legacy projection (`compilation|<assembly>|<metadata>|<trees>`,
raw-ordinal sort before normalization, no dedup), not a strong reference claim —
`compctx2` carries that.
This review independently reproduced the oracle `compctx1` self-test anchor
`71d89657b28013dc7e6fcf6f755980a4412451802437a85aea8e67a3fe4f1333` and the full
frozen oracle (185/185; see [verification log](verification-log.md)). The
production mapper is not implemented, which is a row-implementation item
(G-MINT), not an authority gap. **Correction C2:** handoff §D row 5 is superseded
by this declared-profile wording.

## Row 6 — `compctx2` isolation from identity

| Nine-part field | Value |
| --- | --- |
| Requirement | `compctx2` isolation from identity |
| Authority | S22ID §5.1(3)(7) |
| Production surface | minting/validation |
| Observable behavior | `compctx2` absent from `asmctx3`/`usage4` |
| Positive test | Frozen vectors unchanged |
| Adversary | Inject `compctx2` into identity → impossible/refused |
| Environment | native |
| Evidence artifact | negative test |
| Closure authority | IR+O (G-MINT) |

**Independent confirmation — CONFIRMED.**
The frozen `usage4` tuple (13 parts, listed in
[authority-and-expected-value-provenance.md](authority-and-expected-value-provenance.md))
contains no `compctx2`; `ComputeTargetId` requires a canonical
`compctx1:sha256:<64 hex>` and structurally rejects a `compctx2` prefix. This
review independently recomputed both NEW-02 `usage4` anchors
(`bd0487b7…`, `b874bdc5…`) from the tuple alone and the frozen oracle
reproduced all tuple/component discrimination. Live-mapper negatives are a
G-MINT item, not an authority gap.

## Row 7 — Canonical target minting under live authority

| Nine-part field | Value |
| --- | --- |
| Requirement | Canonical target minting under live authority |
| Authority | S22ID §5.1(2); stage2-boundary §3.5 |
| Production surface | extraction + `CodeOccurrence.Identity` |
| Observable behavior | `asmctx3` from verified defining `compctx1` |
| Positive test | Cross-project positive |
| Adversary | Missing/ambiguous/external/unbound refused |
| Environment | native |
| Evidence artifact | probes + log |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: `asmctx3` is the domain-separated SHA-256 over
`asmctx3-context`, `"compctx1"`, the **defining** canonical `compctx1`,
`metadataIdentity`, `moduleName`, `identifierDigest`, `declarationDigest`,
`shapeDigest`; a target is not minted from a caller boolean, documentation ID or
path; external/out-of-scope/unbound is an explicit refusal. Expected values at
authoring time are derived from the defining `compctx1`, never from the mapper.
Qualification: production live-instance/unique-binding correspondence is not
implemented (WP5); this is implementation verification (C), legitimately later.

## Row 8 — Invocation extraction fidelity

| Nine-part field | Value |
| --- | --- |
| Requirement | Invocation extraction fidelity |
| Authority | stage2-boundary §3.6–3.7; acceptance matrix §4 |
| Production surface | `CSharpCodeGraphPlugin*` |
| Observable behavior | Token span, declaration span, source hash, category |
| Positive test | ≥ fixture invocations |
| Adversary | Wrong span/hash → fail |
| Environment | native |
| Evidence artifact | TRX |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: extraction uses the **same live Roslyn compilation** as graph
facts; the supported category is `Invocation` → `method-invocation`; the
identifier-token span is the site, the declaration span is perpendicular
evidence, and the source hash is the actual indexed bytes. Expected spans/source
bytes are established from accepted inputs and independent Roslyn inspection, not
from extractor output. Qualification: production extraction evidence is WP4/WP9.

## Row 9 — Coverage/refusal classification

| Nine-part field | Value |
| --- | --- |
| Requirement | Coverage/refusal classification |
| Authority | stage2-boundary §3.9/§5; coverage-and-availability |
| Production surface | extraction + `CodeOccurrenceCoverage` |
| Observable behavior | Correct state; no empty success |
| Positive test | Confirmed/complete-empty only when complete |
| Adversary | External/ambiguous/unresolved/unsupported refused |
| Environment | native |
| Evidence artifact | matrix + TRX |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: three separate dimensions (semantic resolution, analysis
coverage, publication availability); `Produced` with zero facts is a valid
complete-empty, `Partial` zero facts is not; no state collapses to successful
empty. Expected values are the declared coverage states, not extractor output.
Qualification: production coverage emission and the complete-empty/refusal matrix
are WP7/WP9.

## Row 10 — Unconditional-inactive scanner (F09)

| Nine-part field | Value |
| --- | --- |
| Requirement | Unconditional-inactive scanner (F09) |
| Authority | register §3 (F09); stage2-boundary §10; acceptance matrix §5.6 |
| Production surface | extraction |
| Observable behavior | `ConditionalInactive`, no fact |
| Positive test | Active branch → fact |
| Adversary | `#if`-inactive site → coverage only |
| Environment | native |
| Evidence artifact | TRX |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: `ConditionalInactive` is a coverage classification and never
a confirmed fact; the contract was fixed under F09 while the scanner itself was
deferred. No inactive site becomes a fact because a model labels it.
Qualification: real disabled-text scanner evidence is WP7/WP9.

## Row 11 — Coverage-merge authority (NEW-04)

| Nine-part field | Value |
| --- | --- |
| Requirement | Coverage-merge authority (NEW-04) |
| Authority | register §4; S2.1 boundary §4; entry acceptance §3.8 |
| Production surface | `CodeOccurrenceCoverage.Merge` / constructor |
| Observable behavior | No mixed-scope `Produced` |
| Positive test | Same scope merges |
| Adversary | Differing scope/universe/`declared=0` refused |
| Environment | native |
| Evidence artifact | falsification probes |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves to the exact accepted correction invariants: no adoption of a
conflicting `ScopeId`/`UniverseId`; unconditional `verified <= declared`; no
`Math.Max` scope mixing; mismatched-scope contributions never combine to
`Produced`. Expected values come from entry §3.8, **not** from the current
defective `Merge` (the register and R2 explicitly forbid using the defective
merge as its own oracle). Qualification: correction and independent
falsification are WP6, before any coverage reliance.

## Row 12 — Live defining/source/target correspondence (S-01)

| Nine-part field | Value |
| --- | --- |
| Requirement | Live defining/source/target correspondence (S-01) |
| Authority | register §7; S2.1 boundary §6 |
| Production surface | verified analysis + extraction |
| Observable behavior | Fact attributable to compiled input/live instance |
| Positive test | Cross-project live binding |
| Adversary | Substituted/stale/missing refused |
| Environment | native |
| Evidence artifact | chain evidence |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: every fact must be attributable to the exact compiled input
and the exact live defining compilation; missing/conflicting/substituted evidence
refuses; the consuming unit owns the occurrence and the defining unit owns the
target. Expected values require the same exact compiled bytes/live instance.
Qualification: per-fact production chain is WP4/WP5/WP9; integrated S2.5.

## Row 13 — Option A durability association

| Nine-part field | Value |
| --- | --- |
| Requirement | Option A durability association |
| Authority | S22ID §5.1(5); entry acceptance §3.5; mapping contract §4 |
| Production surface | extraction/coverage model (native excluded) |
| Observable behavior | Defining `compctx2` carried separately; stale refused |
| Positive test | Same context valid |
| Adversary | Transitive-only change invalidates |
| Environment | native |
| Evidence artifact | test + boundary statement |
| Closure authority | IR+O (S2.3 reopen) |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
Authority resolves: the defining `compctx2` association is **separate** from the
identity tuple; a transitive-only change leaves `asmctx3` stable while
invalidating prior facts; extreme pre-mint invariant is in-memory. Qualification:
the concrete carrier/keys/serialization/aggregation are unimplemented; native
persistence/reopen enforcement is the **first separately authorized S2.3 gate**
and is explicitly **not** an S2.2 capability. The row must not be read as a
durability claim.

## Row 14 — F16/NEW-02 confirmation itself

| Nine-part field | Value |
| --- | --- |
| Requirement | F16/NEW-02 confirmation itself |
| Authority | register §3; F12 acceptance §8 |
| Production surface | docs/tests |
| Observable behavior | Rows present; matrix confirmed |
| Positive test | Reviewer confirms rows |
| Adversary | Missing row blocks authoring |
| Environment | — |
| Evidence artifact | review record |
| Closure authority | IR+O |

**Independent confirmation — CONFIRMED WITH QUALIFICATIONS.**
All 14 rows are present with all nine fields in both
[handoff §D](../../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md#d-f16-nine-part-traceability-rows-for-s2-2)
and package §2. The NEW-02 corrected matrix
([stage2-acceptance-matrix §2](../../planning/adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md#2-semantic-identity-usage4--asmctx3))
`usage4` order/domain/components and the `asmctx3` component domains are
independently reproduced. Qualifications/corrections recorded separately (see
[adversarial-findings.md](adversarial-findings.md)): **C1/C2** (stale handoff §D
rows 2/5 superseded by the corrected package), **C3** (handoff §D final column
header "Authority" vs the nine-part "closure authority"), **C4** (implementation
deferral applies to rows 7–13 plus partial 4/5/6, not a blanket "5–14"). No row
is missing; no field is invented.
