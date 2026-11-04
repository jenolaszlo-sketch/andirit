# ADR-CI1-01-CI1A-S2.2-OWNER-01 — Entry acceptance and architectural dispositions

**Project:** Andirit. **Date:** 2026-11-04.
**Commission:** `ADR-CI1-01-CI1A-S2.2-OWNER-01`.
**Entry package accepted:** `docs/planning/adr-ci1-01-ci1a-s2-2/` at entry
preparation commit `56877aa728236b440c90dc1876d1c21b2dfab064`
(tree `363e937a9685718c422be946e055f32dfa9b9b1e`).
**Disposition:** `ADR-CI1-01-CI1A-S2.2-OWNER-01 ACCEPTED WITH QUALIFICATIONS`.
**S2.2 implementation:** bounded commission issued for the work packages in the
[execution-readiness handoff](../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md).
**S2.3/S2.4/S2.5/Stage 6/publication:** **NOT AUTHORIZED**.

This is the owner decision recording acceptance of the S2.2 entry package, the
architectural dispositions that must be settled before dependent coding, and the
bounded implementation authority. The executor records the owner's issued
decision; it is not a new independent review or a human signature.

---

## 1. Verified entry baseline

Canonical repository `D:\projects\Andirit`, branch `main`. Verified at this
decision: clean tracked/untracked tree, one canonical worktree, no stashes,
`origin/main` `31c33a934aa13e373d9a3a6ffbc54d789eef092c` is an ancestor, no push.

| Role | Commit | Tree |
| --- | --- | --- |
| Entry-preparation commit | `56877aa728236b440c90dc1876d1c21b2dfab064` | `363e937a9685718c422be946e055f32dfa9b9b1e` |
| F12 owner acceptance (parent) | `a1cf47083747bae90293ec074b35fe8078bd0d6e` | `ebba0daf2977315a5c09551df0de698afeb6696f` |
| Qualified source-bound R2 reference | `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5` | `29150eb6ef2b0fe65942f3c8930037f8c0120174` |
| S2.1 owner baseline | `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` | `b4a764637498ed8e8bddac28fb0b0b3716b7ffee` |
| S2.1 reviewed production source | `c02d5eae157653ba7f123b98aceccb01a0b542a0` | `a53a076dbb482062973f70fd11bf4261cc5368f3` |

All six entry documents under `docs/planning/adr-ci1-01-ci1a-s2-2/` were read:
`README.md`, `scope-and-entry-criteria.md`, `obligation-dependency-matrix.md`,
`implementation-commission-draft.md`, `risk-and-qualification-register.md`,
`owner-handoff.md`. The `56877aa` diff is documentation-only (6 added files,
+964). The `a1cf470` diff against R2 is documentation-only. No `src/`, `tests/`,
`benchmarks/`, `smoke/`, schema or evidence path changed.

## 2. Entry acceptance

The owner **accepts** the S2.2 entry package with the explicit qualifications in
§4, and confirms:

- The F12 frozen R2 semantic reference remains `ACCEPTED WITH QUALIFICATIONS`
  as an independent reference and a **bounded oracle, not a production
  correctness guarantee**.
- S2.1 remains `ACCEPTED WITH QUALIFICATIONS`.
- Frozen R2/Q3 evidence, historical failed reviews, manifests, oracles and prior
  qualifications are preserved and not rewritten.
- Option A distinctions are preserved: `asmctx3` semantic target identity;
  defining `compctx1`; provenance `compctx2`; frozen `usage4`.

## 3. Architectural dispositions

Each disposition names its authority, the inspected implementation, the chosen
contract, rationale/limitations, and required tests. Where a choice is genuinely
bounded, the escalation trigger is recorded.

### 3.1 Q-F2 — encoding boundary

- **Authority:** S21A §F; REG §4A. **Inspected:** `src/Andirit.CSharp/VerifiedCSharpSource.cs` `ReadAsync` (strict `UTF8Encoding(false, true)`; BOM detection for UTF-32 LE/BE before UTF-16 LE/BE before UTF-8; `DecoderFallbackException` → `InvalidSourceEncoding` refusal with historical graph fallback text).
- **Chosen contract (supported-encoding restriction enforced by a bounded refusal correction):**
  - Supported: UTF-8 with or without BOM; UTF-16 LE/BE **with** BOM; UTF-32 LE/BE **with** BOM; all strict.
  - Unsupported/ambiguous: **BOM-less UTF-16/UTF-32.** A BOM-less input is decoded as strict UTF-8; if the decoded text contains U+0000 (NUL) — the BOM-less UTF-16/32 signature — the source is **refused** with a new bounded token `AmbiguousBomlessEncoding:<path>` and no verified identity is exposed.
- **Rationale/limitations:** the raw byte SHA-256 is still honest; this is a refusal of an ambiguous decode, not a forgery claim. A legitimate BOM-less source containing a literal NUL is refused under this bounded rule (documented limitation). No change to `usage4`/`asmctx3`/`compctx1`.
- **Required tests:** existing UTF-8/BOM/Unicode/empty/CRLF/LF and invalid-UTF-8 tests remain; add BOM-less UTF-16LE/BE and UTF-32LE/BE ASCII-range → refusal; BOM-present UTF-16/32 → accepted (already covered); assert refusal yields no verified source and `VerifiedCSharpCompilation.Create` rejects it (`UnverifiedSource`).

### 3.2 Q-F3 — PE/assembly metadata validation boundary

- **Authority:** S21A §F; REG §4A. **Inspected:** `src/Andirit.CSharp/VerifiedCSharpCompilation.cs` `VerifiedMetadataReference.Capture` — the digest is computed, then `reference.GetMetadata()` is **already forced before the identity is returned** ("Force PE validity before exposing proof"); `Create` also rejects non-assembly image kinds, aliases, embed-interop and duplicate references.
- **Chosen contract (eager validation, with an explicit bounded unsupported-reference fallback):**
  - Eager validation is authoritative: any image that is not a valid PE or does not expose a usable assembly metadata image must fail inside `Capture` **before** any identity string is produced (inherited `GetMetadata()` throw semantics preserved).
  - If adversarial testing shows a non-assembly artifact (e.g., a netmodule) can still produce a captured identity, add a bounded guard that refuses it as an unsupported reference (`UnsupportedMetadataReference`) rather than capturing an identity.
  - No lazy deferral to a later Roslyn bind is acceptable.
- **Rationale/limitations:** a valid content identity still does not prove semantic usability; the contract only guarantees that usable assembly metadata was materialized before capture. This is the bounded unsupported-reference boundary. No frozen identity change.
- **Required tests (adversarial):** truncated PE, random/garbage image, and netmodule/no-assembly-manifest image → `Capture` throws before returning an identity; a valid assembly → captured identity bound to exact bytes; missing file already covered.

### 3.3 A-01 — comma-symbol canonicalization

- **Authority:** S21A §F. **Inspected:** `CompilationProvenanceDescriptor.CanonicalForm` joins path/contentHash/languageVersion/preprocessorSymbols with `|`; preprocessor symbols are joined with `,` in `VerifiedCSharpCompilation.Create`. `SpecificDiagnosticOptions` keys already reject `,`/`=`; tree paths already reject `|`.
- **Chosen contract (bounded hardening):** reject verified construction when any preprocessor symbol name contains `,` or `|` (extend the existing `UnsupportedParseConfiguration` boundary). Valid C# preprocessor symbols are identifiers and cannot contain these characters, so accepted project inputs and frozen vectors are unaffected.
- **Rationale/limitations:** removes the caller-supplied collision without changing the frozen canonical form. S2.2 introduces no public or caller-supplied parse-option reuse.
- **Required tests:** symbol containing `,` → `UnsupportedParseConfiguration`; symbol containing `|` → refusal; ordinary multi-symbol ordering remains deterministic.

### 3.4 A-02 — scope-digest boundary

- **Authority:** S21A §F; S2.1 provenance contract §3. **Inspected:** `VerifiedCSharpScope.Declare` hashes only `analysisscope1-scope`, repositoryId and `projectUnitId|projectPath`; per-unit source membership is enforced separately by `VerifiedCSharpUniverse` (`ScopeSourceMembershipMismatch`) and each `compctx2` descriptor.
- **Disposition (recorded explicitly, no production change):** `analysisscope1` is **not** a source-inventory digest. S2.2 must derive declared/verified membership and completeness only from the verified universe and descriptors, never from the scope identity alone.

### 3.5 Option A durability — semantic identity, defining context and provenance

- **Authority:** S22ID §5.1(5)/§7; REG §4A; entry package §6.5. **Inspected:** `CodeOccurrenceCoverage` carries `ScopeId`/`UniverseId` but no per-defining-context association; native publication is S2.3.
- **Decision (option (a)):**
  - **No conflation:** `asmctx3` continues to be minted from the defining R2-faithful `compctx1` only; `compctx2` never enters `asmctx3` or `usage4`.
  - **S2.2 scope:** implement the defining-context association as an explicit, separate provenance-validity record on the occurrence-evidence model (the defining compilation's verified `compctx2` plus `compilation-universe1` membership), and enforce it at extraction time: no fact is emitted when the defining `compctx2` is absent/unverified/not a universe member; a changed defining `compctx2` (including transitive-only) invalidates prior facts; stale evidence is refused.
  - **S2.2/S2.3 boundary:** S2.2 does **not** implement native persistence or claim reopen enforcement. The association carrier is designed to be durable, and **S2.3's first gate** is a reopen test proving a stale/changed defining `compctx2` refuses prior facts after publication.
- **Rationale/limitations:** native publication is S2.3 by the accepted register; requiring reopen enforcement inside S2.2 would be scope creep into S2.3. S2.2 **can** enforce the association at extraction time and **can** define the durable carrier, so no Option B escalation is required now.
- **Escalation trigger:** if S2.3 cannot persist and enforce the association after reopen, stop and return to the owner with the bounded **Option B** alternative before any production reliance.
- **Qualification:** S2.2 acceptance must **not** claim "provenance enforceable after reopen"; that claim is deferred to S2.3.
- **Required tests:** defining `compctx2` absent/unverified/not-in-universe → no fact; transitive-only change → `asmctx3` stable, `compctx2` changed, prior facts invalid; `compctx2` cannot appear in `asmctx3`/`usage4` (negative); restore → exact identities.

### 3.6 F16 / NEW-02 — traceability and independent confirmation

- **Authority:** REG §3 F16; F12A §8. **Disposition:** the nine-part traceability rows for the S2.2 slice are defined in the [execution-readiness handoff §D](../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md#d-f16-nine-part-traceability-rows-for-s2-2). An independent reviewer must confirm the NEW-02-corrected R2 identity matrix before any dependent qualification test is authored (gate `G-CONFIRM`). Rows must not credit future tests as passing.

### 3.7 NEW-01 — non-reliance

- **Authority:** REG §4; S2.1 boundary §4. **Disposition:** S2.2 extraction and qualification operate on live compilations and in-memory/test occurrence facts; they must **not** invoke `DurableCommandLog.ReplayAsync`, native recovery, or occurrence-projection replay. NEW-01 remains a mandatory independently-qualified gate before **S2.3** and any earlier production recovery reliance. Any S2.2 work package that would exercise recovery must stop and return to the owner.

### 3.8 NEW-04 — coverage-merge correction

- **Authority:** REG §4; S2.1 boundary §4; coverage-and-availability §2. **Inspected:** `CodeOccurrenceCoverage.Merge` in `src/Andirit.Abstractions/CodeOccurrence.cs` (lines 475–496) and the constructor (lines 400–446).
- **Required correction (bounded, in a dedicated package):**
  1. **Scope/universe compatibility.** `Merge` must not adopt the first non-null `ScopeId`/`UniverseId` when the other contribution carries a non-null and different value. On mismatch it must refuse (`InvalidDataException`/conflict) or degrade to `Unavailable`/`Partial` with a conflict reason; mismatched-scope contributions must never combine to `Produced`.
  2. **Unconditional `verified <= declared`.** The constructor must reject `verifiedUnitCount > declaredUnitCount` without the current `declaredUnitCount != 0` exemption, so `declared=0, verified>0` cannot claim verified membership.
  3. **Count reconciliation.** Declared/verified counts for differing scopes must not be silently `Math.Max`-combined; same-scope merges must reconcile consistently or refuse.
  4. **State combination.** `Worse` must not yield `Produced` for mismatched-scope/ universe contributions.
- **Invariants:** same scope+universe merges permitted; differing scope or universe refused/degraded; partial+complete → partial; any unavailable → unavailable; complete+complete over the same scope → complete; a fact alone never asserts completeness.
- **Independent falsification probes:** P06-style mixed-scope merge cannot claim `Produced`; `declared=0/verified>0` rejected; matching-scope merge; differing-universe merge refused; verified-empty vs missing-evidence/no-analysis; partial+complete; changed membership; native persistence/reopen only if published (S2.3).
- **Qualification:** corrected merge independently qualified before any coverage-dependent claim and before S2.2 acceptance. The S2.0 `NEW-04` case fixture remains a model-level assertion and is not an oracle for the defect.

### 3.9 S-01 — source-byte provenance through S2.2

- **Authority:** REG §7; S2.1 boundary §3/§6. **Disposition:** preserve the full chain `source bytes → verified buffer → Roslyn text/tree → verified compilation → live semantic binding → minted target/occurrence` for every S2.2 fact. Each fact must be attributable to the exact compiled input and the exact live defining compilation; missing/conflicting/substituted evidence refuses. The integrated independent chain remains an S2.5 obligation.

## 4. Explicit qualifications and nonclaims accepted with entry

- Q-F2 and Q-F3 are addressed by bounded contracts (§3.1/§3.2); their correction
  and adversarial tests are **implementation work**, not yet performed.
- NEW-04 correction and independent qualification are **pending** (§3.8).
- Option A durability reopen-enforcement is **deferred to S2.3** by decision
  (§3.5); S2.2 acceptance must not claim it.
- F16 rows and NEW-02 independent confirmation are **pending** as the
  `G-CONFIRM` pre-authoring gate (§3.6).
- The frozen R2 oracle remains a bounded reference; fixture-declared digest
  forms, host-toolchain-bound identities, purpose-built corpus, modeled
  coverage/provenance and Windows-only reproducibility are retained bounds.
- No claim is made of production semantic correctness, native publication,
  public Find Usages, complete-empty replacement, crash/power-loss/multi-process
  recovery, D3/D4 full qualification, Linux qualification or full platform
  certification.
- Every historical disposition, failed review, frozen vector and evidence
  package remains preserved; nothing is retrospectively closed.

## 5. Authorization and boundaries

- The owner authorizes **only** the bounded S2.2 work packages and gates in the
  [execution-readiness handoff](../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md).
- No authorization is granted for S2.3, S2.4, S2.5, Stage 6, publication or
  unrelated implementation. Native publication remains S2.3 and requires a
  separate commission plus NEW-01 closure.
- Work proceeds only in canonical `main`; no additional worktrees, no
  unnecessary branches, no push or publication without separate authorization.
- Established production contracts are preserved; the minimum bounded change is
  required; producer evidence and independent review evidence are kept separate.
- Stop conditions in the handoff §G are binding.

## 6. Recording

This decision and the companion execution-readiness handoff are additive
documentation. Frozen manifests, oracles, historical reports and all prior owner
decisions are untouched. Current work queue and gate status are recorded in the
[§8 progression rows of the obligation register](adr-ci1-01-ci1a-s2-obligation-register.md#8-progression-and-closure)
and the [S2.2 execution-readiness handoff](../planning/adr-ci1-01-ci1a-s2-2/execution-readiness-handoff.md).

**Final disposition:** **`ADR-CI1-01-CI1A-S2.2-OWNER-01 ACCEPTED WITH QUALIFICATIONS`** — entry accepted, architectural dispositions recorded, bounded S2.2 implementation commission issued. S2.3 and later remain unauthorized.
