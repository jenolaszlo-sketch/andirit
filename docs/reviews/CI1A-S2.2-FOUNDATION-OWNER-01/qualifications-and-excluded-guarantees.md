# S2.2 WP1–WP3 — Qualifications and excluded guarantees

**Commission:** `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-01`. **Date:** 2026-11-04.
**Candidate:** `d5a785f898ce7877caef10be53074f2c2ae3a1cd`
(tree `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c`).
**Authority:** independent Q2 report
[`CI1A-S2.2-Q2-foundation-requalification.md`](../CI1A-S2.2-Q2-foundation-requalification.md)
§§3–8, reconciled against the R1 report, the accepted entry decision and the
Option A decision. This document does not amend or extend Q2.

Every qualification below is taken from the authoritative Q2 report; the Q2
section is cited so the owner can check it directly. This is the exact set the
[draft owner decision](../../decisions/adr-ci1-01-ci1a-s2-2-foundation-owner-01.md)
would carry forward.

---

## 1. Q-F2 — supported encodings and the bounded NUL heuristic

**Source:** Q2 §3 (WP1 scope and §3 table); entry decision §3.1.

**Supported (accepted) contract:**

- UTF-8 **with or without BOM**, decoded strictly.
- UTF-16 LE/BE **with** a recognized BOM, decoded strictly.
- UTF-32 LE/BE **with** a recognized BOM, decoded strictly.
- BOM detection order: UTF-32 before its UTF-16 prefix, then UTF-16, then UTF-8.

**Bounded refusal (the “NUL heuristic”):**

- When no recognized BOM is consumed, the input is decoded as strict UTF-8.
- If the decoded text contains U+0000 (NUL) — the characteristic signature of
  BOM-less UTF-16/UTF-32 misread as UTF-8 — the source is refused with the
  bounded token `AmbiguousBomlessEncoding:<path>` and **no verified identity is
  exposed**.
- Strict decoder failure yields `InvalidSourceEncoding:<path>`.
- The raw byte SHA-256 remains honest on refusal.

**Known limitation (not a defect):**

- A legitimate BOM-less UTF-8 source containing a literal NUL is refused under
  this bounded rule.
- BOM-less bytes `41 41` can mean BOM-less UTF-16 U+4141 or ordinary UTF-8 `AA`;
  they decode/verify as `AA`. No detector can recover the intended encoding from
  those bytes alone.

**Explicitly not guaranteed (Q2 §3, entry §3.1):**

- Universal UTF-16/UTF-32 detection or arbitrary BOM-less non-NUL UTF-16
  interpretation.
- Other encodings; exhaustive Unicode sequence qualification; cross-platform
  decoding equivalence.
- Any claim that a successfully decoded source is valid C# syntax.

**Boundary statement:** Q-F2 acceptance is for the strict recognized-encoding set
plus the bounded BOM-less NUL refusal — not universal encoding detection.

---

## 2. Q-F3 — bounded structural metadata validation

**Source:** Q2 §3 (validator review; challenge table; “Documented excluded
surfaces”); Q2 §5; R1 report §3.

**Achieved (accepted) contract:**

- `VerifiedMetadataReference.Capture` remains the **sole** constructor of
  verified reference evidence; its constructor is private; no descriptor-only or
  lazy successful fallback exists.
- Identity is returned only after: immutable acquisition (bounded at 128 MiB),
  SHA-256 of the exact image, Roslyn assembly materialization, an
  `IsAssembly` manifest guard, then bounded structural validation, then a final
  cancellation check.
- Validation dereferences **every** TypeRef and TypeDef name/namespace and
  requires nonempty required names (heap access + nonemptiness).
- Validation dereferences **every** MethodDef name and consumes the signature blob
  fully: headers, instance/static agreement, counts, placements, local type-token
  row bounds, local generic indices/arity, arrays, nested function-pointer
  traversal, and rejects trailing bytes.
- Custom-modifier TypeSpec rows/blobs are checked for accessibility (bounds), not
  recursively certified syntax.
- Type/method GenericParam collections check local owner/index order; method owner
  must be non-nil; Param sequence numbers are unique and within signature count
  (legal return sequence zero included); missing Param rows remain legal.
- The iterative parser avoids recursive stack exhaustion and count-sized arrays.
- The three original Q1-F01 malformed images (first TypeDef name, later TypeDef
  name, MethodDef signature heap index; exact hashes preserved) **refuse
  deterministically, twice, with no returned capture**.
- Ten new heap/relationship mutations and fifteen compound signature adversaries
  refuse; non-language identifier controls and legal minimal/complex positives
  capture.

**Documented excluded surfaces still observed to capture (Q2 §3):**

- Malformed Field signature `FF`.
- Invalid TypeRef ResolutionScope.
- Malformed modifier TypeSpec syntax `FF`.

These are observed false-acceptance under a **hypothetical complete-metadata
validity claim**; that claim is expressly **not** made. Their exact bytes are
preserved.

**Explicitly not guaranteed (Q2 §3, R1 §3 “Exact exclusions”):**

- **No guarantee of complete PE/IL correctness.**
- No guarantee of complete metadata validity, binding or C# identifier
  grammar/UTF-8/name canonicalization.
- Field/property/MemberRef/MethodSpec signatures, resolution scopes,
  inheritance/constraints, full TypeSpec resolution, other tables, custom
  attributes, IL bodies, resources, PE trailing sections and cross-module
  relationships are not exhaustively validated.
- Multi-module/mixed/native images, arbitrary platforms and resource-exhaustion
  attacks are not newly qualified.
- The existing 128 MiB image bound is not a comprehensive CPU/memory-exhaustion
  qualification.
- Capture success is not successful binding or compilation.
- Retaining complete metadata with missing trailing PE bytes still captures — a
  universal truncation-refusal claim is **not** made.

**Semantic compilation negative (Q2 §3):** a captured structurally supported
synthetic assembly and verified compilation provenance can coexist with compiler
errors from missing core semantic references. **No guarantee of semantic
compilation usability.**

**Boundary statement:** Q-F3 acceptance is for the bounded name/signature/
relationship surface actually implemented and adversarially exercised — not
“usable assembly metadata” in the entry §3.2 sense, not full PE validity, and not
a general reference-metadata validator.

---

## 3. A-01 — symbol hardening

**Source:** Q2 §2 (Q1-F04 row), Q1 §5, entry decision §3.3.

- Preprocessor symbols containing `,` or `|` are refused as
  `UnsupportedParseConfiguration`; comma/pipe/mixed injections and bare
  separators all deterministically refuse.
- Reversing `TRACE`/`FEATURE` preserves the descriptor digest and yields
  `FEATURE,TRACE`; symbol ordering is deterministic.
- Composed `é` versus decomposed `e\u0301` remains ordinally distinct; no Unicode
  normalization silently collapses them.
- The changed API is internal; signatures and frozen source canonical form are
  unchanged; rejection concerns decoded symbol characters.

**Explicitly not guaranteed:** arbitrary invalid C# identifier grammar or
exhaustive Unicode parsing. S2.2 introduces no public or caller-supplied
parse-option reuse; the hardening is a bounded correction to the internal
construction surface.

---

## 4. Corrected mapping reference profiles and authority boundaries

**Source:** Q2 §4; R1 §4; entry decision §3.4; mapping contract §2.

**Two distinct comparison profiles (accepted):**

1. **Production capture profile** — immutable captured
   `file|<actual normalized path>|<lowercase byte hash>` inventory; the host
   `TRUSTED_PLATFORM_ASSEMBLIES` paths, distinct and sorted case-insensitively,
   subject to the existing bound (193 entries in the Q2 host, which is **not** a
   universal inventory size). Exact byte/path identity; actual paths preserved.
2. **Frozen corpus fixture profile** — exactly three framework items labelled
   `file|runtime/System.Runtime.dll|…`,
   `file|runtime/System.Private.CoreLib.dll|…`,
   `file|runtime/Microsoft.CSharp.dll|…`, over the accepted runtime 10.0.12
   bytes; frozen anchors `Leaf = compctx1:sha256:404c5e48…` and
   `Core = compctx1:sha256:942a8e58…` apply **only** under this complete profile.

**Acceptance-critical qualifications:**

- Algorithm fidelity (R2 `Describe` vs the mapping over the **same** complete
  inputs) is separate from exact frozen-fixture-anchor reproduction.
- The actual-path captures on this host are `Leaf = …b816dcb5…` and
  `Core = …df8d2932…`; the frozen anchors are recovered only by aligning **the
  comparison’s** three PE labels to the authenticated fixture identities, with
  all bytes and other inputs fixed. This is **test-only** and must never enter
  production reference identities or frozen expectations.
- The production profile is **not** promised to equal a different fixture
  preimage. The frozen corpus’s three-reference construction is **not** evidence
  that the live production inventory has three references.
- The R2 project-reference item is a **faithful legacy projection**, **not** a
  strong verified project-reference claim; the strong claim is carried by
  `compctx2`. The item omits the child’s options, preprocessor-symbol identity,
  references and transitive dependencies.
- Trees sort by **raw** ordinal `FilePath` before separator normalization and are
  hashed as UTF-8 decoded text; own-source hashes use exact captured raw bytes
  (including BOM). Projected reference items are not deduplicated.
- `analysisscope1` is a declared unit/path identity, **not** a source-inventory
  digest (A-02); membership/completeness come only from the verified universe and
  descriptors.

**Authority boundary:** accepted authority is the Option A §5, entry §§3.2/3.5/
3.6, accepted identity contract, frozen R2 `Describe`/`ReferenceIdentity` and
corpus construction — not a green production helper or a producer summary.

---

## 5. Option A identity distinctions and association invariants

**Source:** Q2 §5; Option A decision §5; entry decision §3.5; mapping contract
§§1, 3, 4.

**Accepted distinctions (must be preserved):**

- `asmctx3` semantic **target** identity is minted from the defining R2-faithful
  `compctx1`; `usage4` is frozen and uses the target’s `asmctx3`; `compctx2` is
  **provenance validity only** and never enters `asmctx3`/`usage4`.
- No new identity version; no identity churn on a dependency-context-only change.
- Analysis generation, publication freshness and storage-schema identity remain
  separate from both target identity and provenance validity.
- Equal complete accepted inputs reproduce equal identities across distinct
  object instances; different accepted defining preimages discriminate; same
  bytes at different paths remain distinct under the path-bound reference
  contract. Object identity is not an identity field.
- `compctx1`, `compctx2`, `asmctx3` and frozen `usage4` stay distinct; the
  consuming context is never substituted for the defining context.

**Pre-mint association invariant (specified, not implemented):**

- For every candidate occurrence, before any fact is minted: exactly one verified
  live defining compilation instance; a complete `compilation-universe1` for the
  declared `analysisscope1`; the defining compilation’s current verified
  `compctx2` equal to the context under which the occurrence is produced; the
  association recorded **separately** from the identity tuple.
- If any check fails, no fact is emitted and the case is classified with the
  correct coverage state, never as successful empty.

**Explicitly not guaranteed (Q2 §5):**

- No current production mapped-target resolver or per-occurrence carrier exists;
  unsupported/missing authority has **not** passed a production minting rejection
  test.
- Shape-only helpers still accept canonical fabrications by design; shape is not
  authority. A fabricated canonical `compctx1` still yields a helper target, and a
  direct `compctx2` passed as a `compctx1` is refused — both are shape/interface
  facts, not live authority.
- Equal semantic identity does **not** prove current association or freshness.

---

## 6. No native reopen / durability enforcement under S2.2

**Source:** Q2 §§5, 8; Option A decision §§5.1(5), 7; entry decision §3.5;
mapping contract §4.

- S2.2 scope is **extraction-time in-memory enforcement** of the association, plus
  a durable-carrier **requirement/design direction**.
- The concrete per-fact carrier, keys, serialization, aggregation rules and
  reopen checks are **unresolved and unimplemented**. A scope/universe digest
  alone or a caller boolean is insufficient.
- Native persistence/reopen enforcement is the **first separately authorized S2.3
  gate**, which must prove that a stale/changed defining `compctx2` refuses prior
  facts after reopen.
- S2.2 acceptance must **not** claim “provenance enforceable after reopen”; that
  claim is deferred to S2.3.
- If S2.3 cannot persist and enforce the association after reopen, stop and return
  to the owner with the bounded **Option B** alternative before any production
  reliance.

---

## 7. Carried accepted limitations (unchanged by this acceptance)

Q2 §8 and the risk register retain, at their existing owner/gate assignments:

- S2.1 preserved packaging limitation; Q-F4 diagnostic wording; Q-F5 historical
  branches.
- A-02 scope composition; S-01 full live source/target chain; F09 inactive
  scanner; F10/IF-04 reader continuation; F11 semantic correspondence;
  F16/NEW-02 gating; NEW-01 recovery; NEW-04 merge; SQ-01…SQ-07; D3/D4/D5;
  I-05/MD-03.
- Bounded F12/toolchain reference: accepted Windows/runtime bytes, purpose-built
  corpus, fixture-declared target components and modeled lifecycle/coverage/
  provenance. Oracle green must not be generalized to production correctness,
  full canonicalization, other runtimes/platforms or integrated S2.5.

None of these is closed by implication of the WP1–WP3 acceptance.

---

## 8. Untested boundaries (no capability claimed)

Q2 §8:

- Exhaustive names/UTF-8/table validity; external generic/constraint resolution;
  multi-module/native images; IL/resources; resource exhaustion; arbitrary
  platforms.
- Per-fact production stale association; persistence/reopen/recovery.
- Malformed UTF-8/name canonicalization; field/property/MemberRef/MethodSpec
  syntax; resolution scopes; inheritance/constraints; full TypeSpec resolution.
- Complete PE/IL correctness and semantic compilation usability.

No hypothetical defect is invented from an untested boundary; equally, no
capability is claimed for one.

---

## 9. One-line acceptance boundary

**Acceptance would cover the bounded WP1–WP3 encoding, structural metadata,
symbol-hardening and corrected mapping-contract foundation only — with the
excluded surfaces above, both pre-minting gates still open, and no production
mapping, association, minting, persistence or reopen capability.**
