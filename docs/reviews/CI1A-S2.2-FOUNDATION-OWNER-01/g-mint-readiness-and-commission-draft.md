# G-MINT — readiness assessment and independent commission draft

**Commission ID (proposed):** `ANDIRIT-CI1A-S2.2-G-MINT-01`.
**Gate:** `G-MINT`. **Status:** `DRAFT — NOT ISSUED`. **Gate remains OPEN.**
**Candidate:** `d5a785f898ce7877caef10be53074f2c2ae3a1cd`
(tree `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c`).
**Authority:** Option A decision §§5.1/5.2/7; entry decision §§3.4/3.5;
[mapping contract](../../planning/adr-ci1-01-ci1a-s2-2/mapping-contract.md);
[G-MINT package](../../planning/adr-ci1-01-ci1a-s2-2/g-mint-package.md);
independent Q2 report §§4–5, 7.

This reassesses the corrected mapping contract and defines what would be required
to pass `G-MINT` before WP4–WP5 minting. It issues no verdict. It does **not**
claim that association minting, persistence or reopen behavior is implemented.

---

## 1. What the corrected mapping contract now provides (review-ready input)

- A precise, deterministic R2-faithful `compctx1` derivation for dependency-
  bearing defining compilations, expressed as a **mapping, not a redefinition**.
- Two explicitly declared comparison profiles with distinct expected results:
  1. **Algorithm fidelity** over the **same** complete input profile (including
     unchanged verified reference identities and actual production paths).
  2. **Frozen fixture profile** reproduction, using the corpus’s three
     `file|runtime/...` labels over accepted runtime bytes, yielding
     `Leaf = compctx1:sha256:404c5e48c3106b47786f14cad594139477c651fd78d0e050f8fd5bcab18fcb5f`
     and `Core = compctx1:sha256:942a8e588a8db15afafb30d0292e0b867c767c5f72392b6f458783d961a71db3`.
- Disclosure of the **test-only** label alignment that recovers those anchors and
  of the actual-path values on this host
  (`Leaf = …b816dcb5…`, `Core = …df8d2932…`).
- The exact legacy R2 project-reference representation
  `compilation|<assembly>|<metadata>|<trees>`, including raw-ordinal sort before
  separator normalization, UTF-8 decoded-tree hashes, no deduplication, and the
  source-byte-vs-tree-text distinction.
- The extraction-time association invariant (`mapping-contract §3`) and the
  S2.2/S2.3 durability partition (`mapping-contract §4`).
- Positive/negative/collision examples (§5–§6) and the required falsification
  probe list (§7).

Independently verifiable invariants the reviewer can check now:

- `asmctx3` is computed from the defining R2-faithful `compctx1`; `usage4` frozen;
  no new identity version.
- `compctx2` never enters `asmctx3`/`usage4`; passing a `compctx2` directly as a
  `compctx1` to the frozen helper refuses.
- No identity churn on a transitive-only change; identity change when the
  defining R2 preimage changes.
- Equal complete inputs → equal mapped identities across distinct instances;
  different accepted preimages discriminate; same bytes at different paths remain
  distinct under the path-bound contract.
- Frozen corpus anchors are required only under the complete fixture profile.

## 2. What is still missing (gate cannot pass)

Per Q2 §7 and G-MINT package §7:

- **No production mapper/resolver** implementing the R2-faithful `compctx1`
  derivation.
- **No complete verified universe / current defining `compctx2` association**
  enforced at extraction time.
- **No concrete per-fact association model** (carrier, keys, serialization,
  aggregation, refusal classifications) — only the requirement.
- **No independent pre-WP5 falsification** of the implemented surfaces.

Repeated reviewer reconstruction is not production re-ingestion; frozen modeled
gates do not prove production refusal. A documented map, a reviewer demonstrator
and passing algebra tests cannot substitute for implemented, independently
qualified production gates.

## 3. Evidence and review requirements to pass `G-MINT` before WP5

The independent reviewer must verify, against an **implemented** mapper and live
authority/association checks (not only reviewer reconstruction):

1. **Identity invariants** — `asmctx3` from defining `compctx1`; `usage4` frozen;
   no new version.
2. **Provenance separation** — `compctx2` never enters `asmctx3`/`usage4`;
   `asmctx3` never substitutes for validity.
3. **Mapping fidelity by profile** — algorithm fidelity over equal complete
   inputs; frozen anchors only under the complete fixture profile; both actual-path
   inventory and fixture identities recorded; no production label substitution.
4. **Determinism** — byte-identical repeated and separate-process runs.
5. **No-churn / identity-change pair** — transitive-only change leaves `asmctx3`
   stable; declaration-changing input changes it.
6. **Association invariant** — in-memory live-instance, universe-membership and
   current-`compctx2` equality checks, with refusal classification.
7. **Durability boundary** — no S2.3 persistence claimed; reopen deferred; Option B
   escalation recorded.

Required falsification probes (Option A §5.2 tests 1–7 and 9–10 against the
implemented mapper; test 8 split by the accepted boundary):

- E1 conditional defining-API change → `asmctx3` changes; old facts invalid.
- E2 transitive-only dependency change → `asmctx3` stable, `compctx2` changes at
  all levels, stale facts refused.
- E3 caller unchanged; target follows the defining conditional API.
- Restore original inputs → exact original identities.
- Distinct defining compilations → distinct targets (T03/T10).
- Equivalent reference artifact → equality per the accepted contract (T07).
- Re-ingestion determinism (T08).
- **Pre-mint (in-memory) portion of test 8** — candidate/current/universe mismatch
  refuses before fact emission.
- Negative — no target/occurrence from an unverified/incomplete context;
  `compctx2` cannot enter `usage4`/`asmctx3`.
- Fabricated, incomplete, out-of-universe, duplicate, ambiguous, stale and
  substituted live-context negatives; matching shape/name/descriptor is not
  authority.
- NEW-04 disposition accepted before any coverage reliance.

## 4. Boundary: what `G-MINT` does NOT require

- **Native persistence and reopen enforcement are not required to pass `G-MINT`
  in S2.2.** The first S2.3 gate must prove stale defining `compctx2` refuses prior
  facts after reopen; it must not be pulled forward as S2.2 work.
- No production publication, no public API, no migration.

## 5. Reviewer deliverables and verdict

- Exact source/tree reviewed; exact commands; raw outputs.
- Mapped `compctx1`/`asmctx3` values against frozen R2 anchors under the stated
  complete profile, with actual verified inventory and any comparison-only
  alignment stated explicitly.
- Falsification probes with pass/fail and the failing check identity.
- Residual limitations/disagreements.
- Verdict: `G-MINT PASS`, `G-MINT PASS WITH QUALIFICATIONS`, or `G-MINT FAIL` —
  recorded by the reviewer, never by the producer. Until a pass (or
  accepted-with-qualifications) verdict is recorded, WP5 target minting must not
  begin.

## 6. Current state

`G-MINT` is **OPEN** and **not ready to pass a production minting qualification**:
the mapping contract is documented and review-ready, but the mapper, live
association and refusal behaviors are **not implemented**, **no targets are
minted**, and **no independent verdict exists**. Association minting, persistence
and reopen are **not** implemented by this foundation.
