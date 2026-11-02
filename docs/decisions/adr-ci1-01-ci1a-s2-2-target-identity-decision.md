# ADR-CI1-01-CI1A — S2.2 Semantic Target Identity Decision

**Recorded:** 2026-11-02. **Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01`.
**Status:** `S2.2-ID OPTION A ACCEPTED` — retain `asmctx3` as semantic target identity with mandatory `compctx2` provenance validity and invalidation.
**S2.2 implementation:** NOT AUTHORIZED. **Migration:** not required for experimental occurrence data (pre-release policy).

---

## 1. The question

**Should changes to a defining compilation's dependency context change semantic target identity, or only invalidate the provenance supporting a semantic binding?**

These concepts are deliberately separate and must not be conflated:

| Concept | Meaning | Current contract |
|---|---|---|
| Semantic target identity | Identity of the defining declaration a usage resolves to | `asmctx3` (frozen R2) |
| Source occurrence identity | Identity of one usage site bound to a target | `usage4` (frozen R2; includes `asmctx3`) |
| Compilation identity | Identity of one defined compilation context (legacy material) | `compctx1` (frozen R2; unchanged) |
| Dependency provenance | Verified context including effective dependency contexts | `compctx2` (authorized amendment) |
| Analysis generation | One indexing run's declared scope and verified members | `analysisscope1` / `compilation-universe1` |
| Publication freshness | Whether a published generation is current for a query | D4 authority |
| Storage schema identity | Persisted occurrence format version | D5 / pre-release policy |

## 2. Existing accepted arrangement

- `compctx1`: frozen R2 compilation-context identity. It already includes the defining compilation's actual sources (path, exact byte SHA-256, language version, preprocessor symbols), material options digest, PE references and R2-form project-reference items.
- `compctx2`: additive, versioned dependency-sensitive provenance; project-reference items are `compilation-context|<dependency compctx2>`, binding the exact live `CompilationReference.Compilation`.
- `asmctx3`: frozen R2 target identity = domain-separated SHA-256 over `"compctx1"` + the defining `compctx1` + metadata identity + module + identifier digest + declaration digest + shape digest.
- `usage4`: frozen R2 occurrence identity using the target's `asmctx3`.
- Candidate C: source-owned occurrence facts. D5: no experimental occurrence-data migration (reinforced by `adr-ci1-01-pre-release-compatibility-policy.md`).

## 3. The two options

**Option A — preserve `asmctx3` with separate `compctx2` provenance.** `asmctx3` remains the canonical semantic target identity; `compctx2` governs validity, reuse and invalidation; a changed dependency context invalidates stale semantic evidence but does not by itself re-identify an unchanged declaration.

**Option B — introduce an explicitly versioned target identity** (e.g., an `asmctx4`-class contract) embedding dependency context so that dependency changes alter target identity directly.

The preferred design principle is to keep semantic target identity separate from provenance validity **unless evidence demonstrates that the existing target identity conflates semantically distinct targets**.

## 4. Evidence

### 4.1 Structural reading of the frozen tuple

`asmctx3` already binds the defining compilation's R2-visible identity: any change to the defining project's source bytes, preprocessor symbols, language version, material options, assembly/metadata identity, module or PE references changes `compctx1` and therefore `asmctx3`. The only changes not reflected are **effective transitive dependency contexts that do not alter the defining project's own R2-visible fields** — precisely the gap `compctx2` was authorized to close. Such changes do not alter the declaration that the target identity names; they alter which referenced symbols the compilation binds, i.e., semantic validity of occurrences, not declaration identity.

### 4.2 Bounded reviewer-owned experiment

Executed outside the canonical repository against the pinned S2.1 production seams (`work/probe-run/tests/Andirit.CSharp.Tests/S22IdentityExperiment.cs`; raw output `evidence/s22-identity-experiment.json` in the Q1 reviewer workspace; 3/3 tests passed). No canonical source was modified.

| Experiment | Setup | Observed |
|---|---|---|
| **E1** | Dependency-free defining project with `#if FEATURE` around the public member; same source text, `TRACE` vs `FEATURE` | `compctx1` `ff933d5d…` → `d8a22e2a…`; **`asmctx3` `5ac32e15…` → `4f9d5031…`** — conditional declaration changes are already discriminated by `asmctx3` |
| **E2** | Three levels `App → Lib → Leaf`; only **Leaf's** preprocessor symbol changes; `Lib` and `App` sources untouched | `compctx2` changes at **every** level (Leaf `ecb5815a…`→`f9921ead…`; Lib `ad98ee4d…`→`741545dd…`; App `c15ec417…`→`232964d3…`), while Lib's own R2 material (sources, options digest, metadata identity, module, assembly name) is unchanged and only its dependency reference items differ. Dependency-bearing compilations expose no legacy `compctx1` twin (`LegacyDescriptor == null`) |
| **E3** | `App` references `Lib`; Lib's conditional API changes; **App source bytes identical** | The target `Lib.Value`'s `asmctx3` changes (`5ac32e15…`→`4f9d5031…`); occurrences targeting it re-identify |

Interpretation: `asmctx3` already distinguishes semantically different **declarations**; the residual transitive-only case does not change the declaration, so identity stability is correct and `compctx2` is the invalidation signal. No collision of genuinely distinct declarations was demonstrated.

### 4.3 Scenario analysis

| Scenario | Relevant fields | Expected semantic equality | Required provenance validity | Target identity behavior | Occurrence invalidation | A sufficient? | B necessary? |
|---|---|---|---|---|---|---|---|
| **T01** same declaration, changed implementation | defining source bytes → `compctx1`/`compctx2` | Declaration unchanged; compilation context changed | New `compctx2` | Changes (frozen R2 binds the defining context) | Yes — old facts invalid | Yes | No (B would also change; no gain) |
| **T02** same declaration, changed direct dependency | dependency `compctx2` item | Declaration unchanged | New `compctx2` | **Stable** | Yes | Yes | No |
| **T03** different semantic target, same descriptor | `compctx1` + identifier/declaration/shape digests | Distinct declarations ⇒ distinct `asmctx3` | Context must verify | Distinct for distinct defining compilations; same declaration in same R2-identified compilation is the same target | N/A | Yes* | No |
| **T04** conditional API change | source items' preprocessor symbols → `compctx1` | Target set changes | New `compctx2` | **Changes** (E1/E3) | Yes | Yes | No |
| **T05** referenced type changes | dependency `compctx2`; target's own defining fields | Declaration unchanged unless its own fields change | New `compctx2` | Changes iff the defining compilation's R2 fields change; otherwise stable | Yes | Yes | No |
| **T06** cross-project usage, B changes | B `compctx2` → A `compctx2` | Relationship re-resolved | New `compctx2` at A and B | B's targets change iff B's R2 fields change | Yes, for A and B facts | Yes | No |
| **T07** equivalent reference artifact | accepted reference identity | Equal iff the accepted contract says equal | Valid under same identity | Stable | No | Yes | No |
| **T08** source re-ingestion | all | Deterministic | Recomputed | Deterministic (separate-process stability verified) | N/A (fresh store) | Yes | No |
| **T09** stale occurrence publication | stored `compctx2` vs current | Invalid | Must be current | Stable | **Refuse/re-ingest** | Yes, **iff** the stored context is enforceable | No |
| **T10** distinct target, identical textual descriptor | `compctx1` (source hashes) + declaration digests | Distinct | Verified | Distinct | N/A | Yes | No |

\* T03 is the only theoretically sensitive row; no counterexample was demonstrated, and any residual case (identical R2-visible fields, different effective transitive binding) does not change the declaration identity. R2/Q3 already qualified `asmctx3` as a defining-context identity.

## 5. Decision

**`S2.2-ID OPTION A ACCEPTED`.**

The existing `asmctx3` target identity is sufficient when combined with mandatory `compctx2` provenance validity and invalidation. No new target identity version is introduced. Introducing one merely because `compctx2` exists is rejected.

### 5.1 Exact mapping contract (binding on S2.2)

1. **Target identity.** `asmctx3` is computed with the frozen R2 algorithm over the R2-faithful `compctx1` of the **defining compilation**, derived deterministically from S2.1-verified material. For dependency-bearing defining compilations (where S2.1 intentionally exposes no legacy twin), the R2-faithful `compctx1` is derived using the frozen R2 project-reference representation; this is a mapping, not a redefinition, and it is **not** a strong verified project-reference claim — the strong claim is carried by `compctx2`.
2. **Provenance validity.** `compctx2` of the defining compilation (including its transitive dependency contexts) must be current and equal to the context under which the occurrence was produced. `compilation-universe1` membership must hold. A bare `asmctx3` is not provenance.
3. **Occurrence identity.** `usage4` remains frozen and uses the target's `asmctx3`.
4. **Invalidation.** Any change to the defining compilation's `compctx2` — including a transitive dependency-only change — invalidates previously produced occurrence facts for that context. Stale facts must be refused or the experimental store re-ingested (POL-02/POL-06). There is no silent reuse under a new context.
5. **Durability.** S2.2 must record the defining context's `compctx2` (or an equivalent unambiguous provenance association) in publication/coverage evidence so the validity gate is enforceable after reopen. If S2.2 cannot enforce this association, the mapping is incomplete and the owner must revisit the decision (potential Option B) before target minting.
6. **No identity churn.** Target identity changes only when the accepted target-identity fields change; dependency-context changes alone do not re-identify an unchanged declaration.
7. **No conflation.** Analysis generation, publication freshness and storage schema identity remain separate from both target identity and provenance validity.

### 5.2 Required tests before S2.2 acceptance

1. Replicate E1: conditional defining-project API change → `asmctx3` changes; old facts invalid.
2. Replicate E2: transitive-only dependency change → `asmctx3` stable, `compctx2` changes at all levels, stale facts refused.
3. Replicate E3: caller source unchanged; target identity follows the defining conditional API.
4. Restore original inputs → exact original identities (deterministic).
5. Distinct defining compilations → distinct targets (T03/T10).
6. Equivalent reference artifact → identity equality per the accepted reference contract (T07).
7. Re-ingestion determinism (T08).
8. Stale publication under a changed context is refused, including after reopen (T09).
9. Mapping derivation for dependency-bearing defining compilations is deterministic and documented.
10. Negative: no target or occurrence is minted from an unverified/incomplete context; `compctx2` cannot enter `usage4`/`asmctx3` directly.

## 6. Compatibility consequence

No experimental `asmctx3`/`usage4` occurrence data requires runtime compatibility. Frozen R2 evidence remains preserved. If a later, separately authorized change selects Option B, the identity change must be explicit, historical evidence preserved, and current experimental data refused or re-ingested without migration, dual-write or legacy readers.

## 7. Gate

S2.2 target minting remains blocked until: S2.1 owner acceptance is recorded (this commission); the S2.0/F12 semantic oracle is independently accepted; NEW-04 has an accepted disposition for any coverage reliance; Q-F2/Q-F3 are corrected or explicitly decided; and the mapping contract above is implemented and independently reviewed.
