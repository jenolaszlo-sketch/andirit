# G-MINT review package (mapping contract)

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.2`. **Gate:** `G-MINT`.
**Status:** prepared for an **independent reviewer**. **Not self-certified.**
`G-MINT` remains **open** until a separate reviewer records a verdict.
**Subject:** [mapping contract](mapping-contract.md) (Option A), which must be
implemented and independently reviewed before any canonical target is minted
(Option A §5.1/§7).

---

## 1. Purpose

Confirm that the S2.2 mapping preserves the frozen R2 identities, keeps
`compctx2` strictly separate, and is deterministic, before minting is allowed.

## 2. Reviewer must independently verify

1. **Identity invariants** — `asmctx3` from the defining R2-faithful `compctx1`;
   `usage4` frozen; no new identity version.
2. **Defining-context/provenance separation** — `compctx2` never enters
   `asmctx3`/`usage4`; `asmctx3` never substitutes for validity.
3. **Mapping fidelity** — the §2 derivation reproduces the frozen R2 `compctx1`
   for standalone and dependency-bearing defining compilations, using the frozen
   R2 project-reference item `compilation|<assembly>|<metadata>|<trees>` and
   exact verified byte source hashes.
4. **Determinism** — repeated and separate-process runs produce identical
   mapped identities.
5. **No identity churn** on transitive-only change; identity change on
   declaration-changing input.
6. **Association invariant** — extraction-time checks (live instance, universe
   membership, current `compctx2` equality) and refusal classification.
7. **Durability boundary** — no S2.3 persistence is claimed; reopen enforcement
   is explicitly deferred and the Option B escalation trigger is recorded.

## 3. Positive / negative / collision examples

Use [mapping-contract §5–§6](mapping-contract.md) verbatim as the case list
(P1–P5, N1–N5, collision/conflation adversaries).

## 4. Required falsification probes

- Option A §5.2 tests 1–10.
- Independent re-derivation of a dependency-bearing defining `compctx1`.
- `compctx2`-isolation negative test.
- Determinism across processes.
- No-churn / identity-change pair.

## 5. Evidence the reviewer records

- Exact source/tree reviewed; exact command lines; raw outputs.
- Mapped `compctx1`/`asmctx3` values compared with the frozen R2 oracle anchors
  ([identity mapping §4](../adr-ci1-01-ci1a-s2-0/identity-and-provenance-mapping.md)).
- Falsification probes with pass/fail and the failing check identity.
- Any residual limitation or disagreement.

## 6. Verdict (reviewer-owned)

The reviewer records one of: `G-MINT PASS`, `G-MINT PASS WITH QUALIFICATIONS`,
`G-MINT FAIL`. The S2.2 producer does **not** issue this verdict. Until a
`G-MINT PASS` (or accepted-with-qualifications) verdict is recorded, WP5 target
minting must not begin.

## 7. Current state

The mapping contract is documented; the derivation is **not implemented**, no
targets are minted, and no independent verdict exists. `G-MINT` is **open**.
