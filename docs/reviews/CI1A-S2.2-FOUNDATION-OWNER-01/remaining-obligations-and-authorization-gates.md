# Remaining obligations and explicit authorization gates

**Commission:** `ANDIRIT-CI1A-S2.2-FOUNDATION-OWNER-01`. **Date:** 2026-11-04.
**Candidate:** `d5a785f898ce7877caef10be53074f2c2ae3a1cd`
(tree `5d8aa21d515dcd2d423aa0c7eb8f7e3996eb650c`).

This records what remains after the WP1–WP3 foundation and names every
authorization gate the owner must decide separately. Nothing here is authorized
by the [draft acceptance](../../decisions/adr-ci1-01-ci1a-s2-2-foundation-owner-01.md).

---

## 1. Work-package status

| WP | Scope | Status |
| --- | --- | --- |
| WP1 | Q-F2 encoding boundary + supported-encoding contract | **Foundation done** (Q2-closed) |
| WP2 | Q-F3 bounded metadata validation | **Foundation done** (Q2-closed, bounded surface) |
| WP3 | A-01 symbol hardening + Option A mapping contract | **Specification done**; mapping **not implemented** |
| WP4 | `Invocation` extraction from the live compilation | **Not begun** |
| WP5 | Canonical target minting with live/universe/`compctx2` gating | **Not begun** (blocked by `G-MINT`) |
| WP6 | NEW-04 coverage-merge correction | **Not begun** |
| WP7 | Coverage/refusal emission + inactive scanner (F09) | **Not begun** |
| WP8 | Option A invalidation + durable association carrier | **Not begun** |
| WP9 | Integration/comparison + F16 evidence + reproducibility | **Not begun** |
| EXCL | S2.3/S2.4/S2.5/Stage 6/publication | **Not authorized** |

Gate status: `G-CONFIRM` **OPEN**, `G-MINT` **OPEN**, `G-DUR` resolved option
**(a)** (first S2.3 reopen gate), `G-ACCEPT` **not reached**.

## 2. Obligations still carried (not closed by WP1–WP3 acceptance)

From the obligation register and the Q2 report §8 — each stays at its assigned
gate until independently qualified and owner-accepted:

| Obligation | Required work | Gate / stage |
| --- | --- | --- |
| Q-F2 | Accepted with the bounded NUL-heuristic qualification (owner decision now) | S2.2 acceptance |
| Q-F3 | Accepted only at the bounded structural surface (owner decision now); excluded surfaces retained | S2.2 semantic acceptance |
| A-01 | Bounded hardening accepted; no public/caller parse-option reuse | Standing |
| A-02 | Scope is not source inventory; membership from the verified universe | Standing |
| S-01 | Full byte→binding→fact correspondence per fact | WP4/WP5/WP9; integrated S2.5 |
| F09 | Real disabled-text scanner bound to declared configuration | WP7 |
| F10 / IF-04 | Bounded reader continuation/cross-target surface | S2.4 |
| F11 | Full semantic correspondence | S2.2/S2.5 |
| F16 / NEW-02 | 14 nine-part rows independently confirmed | `G-CONFIRM` |
| NEW-01 | Occurrence-projection recovery correctness | **Before S2.3** / any earlier recovery reliance |
| NEW-04 | Coverage-merge correction + independent falsification | WP6, before coverage reliance |
| SQ-01…SQ-07 | Native pages/outcome/concurrency/fixtures/platform/namespace/rebuild | S2.3/S2.5 and later |
| D3/D4/D5 | Graph noninterference, publication authority, no-migration | Preserved through S2.2 |
| I-05 / MD-03 | No experimental hook in production; public reader boundary | Standing; public API is ADR-CI1-02 |
| Q-F4 / Q-F5 | Diagnostic wording; historical local-branch housekeeping | Later slice / out of scope |
| F12 bounds | Fixture components, host-toolchain runtime, purpose-built corpus, modeled coverage/provenance, Windows-only | Retained qualification |

## 3. Explicit authorization gates (each a separate owner decision)

The following are **not** granted by this preparation or by the draft acceptance:

| # | Authorization | Prerequisite |
| --- | --- | --- |
| AUTH-1 | **Record this S2.2 foundation acceptance** (owner decision on the draft) | This package; Q1/R1/Q2 evidence |
| AUTH-2 | **Commission independent `G-CONFIRM`**; then allow dependent qualification-test authoring | Foundation acceptance (AUTH-1); [G-CONFIRM draft](g-confirm-commission-draft.md) |
| AUTH-3 | **Implement + independently review `G-MINT`** (production mapper/resolver/association) | `G-CONFIRM`; [G-MINT readiness](g-mint-readiness-and-commission-draft.md) |
| AUTH-4 | **Implement WP4–WP9** extraction, minting, coverage, NEW-04, association, integration | `G-CONFIRM` + `G-MINT` |
| AUTH-5 | **Push / publish** `main` to `origin/main` | AUTH-1 (and owner confirmation); [push plan](git-branch-audit-and-push-readiness.md) |
| AUTH-6 | **S2.3** native publication/reopen (NEW-01 closed) and later stages | S2.2 `G-ACCEPT`; separate commission |
| AUTH-7 | Public Find Usages API (ADR-CI1-02) | Separate ADR/owner decision |

## 4. Stop conditions that remain binding

Stop and return to the owner if:

- the Option A association cannot be enforced before minting (potential Option B);
- `compctx2` would enter `asmctx3`/`usage4` or a frozen identity would change;
- a defining compilation cannot be bound to an exact live instance / verified
  `compctx2` / universe membership;
- S2.2 would rely on NEW-01 recovery, weaken D3/D4/D5, or require a new identity
  scheme;
- a public Find Usages API becomes mandatory;
- Q-F2/Q-F3 accepted limits would be silently exceeded;
- scope expands into S2.3/S2.4/Stage 6 without separate authorization.

## 5. Current position

The WP1–WP3 **foundation** is Q2-qualified `SUPPORTED WITH QUALIFICATIONS` and
independent Q1-F01–F05 closure is technically recorded. Both pre-minting gates
remain open; no production mapping, association, minting, persistence or reopen
capability exists. This preparation **stops at the owner decision gate**.
