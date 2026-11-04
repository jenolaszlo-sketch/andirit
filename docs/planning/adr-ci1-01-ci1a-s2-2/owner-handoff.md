# CI1A S2.2 entry-preparation owner handoff

**Commission:** `ADR-CI1-01-CI1A-S2.2-ENTRY-01`. **Mode:** documentation only.
**Final disposition:** **`READY WITH EXPLICIT QUALIFICATIONS`**.
**S2.2 implementation:** **NOT AUTHORIZED.** Authorization remains a separate
owner decision.
**Baseline:** canonical `main` `a1cf47083747bae90293ec074b35fe8078bd0d6e`
(tree `ebba0daf2977315a5c09551df0de698afeb6696f`).

---

## A. What was done

1. **Verified accepted baselines.** Canonical `main` is clean; one worktree; no
   stashes; `origin/main` `31c33a9…` is an ancestor; 30 local commits, no push.
   The F12 owner-acceptance commit `a1cf470…` is one documentation-only
   descendant of qualified R2 `7f8d035…` (tree `29150eb6…`); the diff changes
   only `ROADMAP.md`, `docs/agent-code-intelligence-roadmap.md`, one decision,
   the obligation register and four planning documents (8 files, +452/−5). S2.1 owner baseline
   `b7fea0d…` (tree `b4a76463…`) and reviewed production source
   `c02d5eae…`/`a53a076d…` are ancestors. Frozen R2/Q3 evidence packages
   (E2 `BCE7DE2A…`, 100 files; E3 `9EFC…`, 338 files) and all historical
   findings/failed reviews remain preserved and qualified.
2. **Recovered the actual S2.2 scope** from the accepted architecture, design
   decisions, acceptance records and stage plans — without inventing
   requirements ([`scope-and-entry-criteria.md`](scope-and-entry-criteria.md)
   §§2–5).
3. **Reconciled the outstanding obligations** Q-F2, Q-F3, A-01, A-02, S-01,
   NEW-01, NEW-04 (plus F16/NEW-02, F09, F11, F02/F01 and standing constraints)
   with source, evidence, blocking stage, required probe and owner decision
   ([`obligation-dependency-matrix.md`](obligation-dependency-matrix.md)).
4. **Produced the S2.2 entry contract** — preconditions, bounded work packages,
   invariants, independent/adversarial verification, quantitative and
   qualitative acceptance criteria, evidence/reproducibility requirements,
   explicit stop conditions and required owner decisions.
5. **Drafted** the implementation commission, the risk/qualification register
   and this handoff. No production code, tests, manifests, oracles or historical
   reports were modified.

## B. Option A distinctions preserved

| Concept | Contract | Role in S2.2 |
| --- | --- | --- |
| `asmctx3` | Frozen R2 semantic **target identity**, from the defining R2-faithful `compctx1` | Minted per target from the verified defining compilation |
| defining `compctx1` | Frozen R2 compilation-context identity | Mapped (not redefined) for dependency-bearing defining compilations |
| `compctx2` | Additive versioned dependency-sensitive **provenance** | Mandatory validity/invalidation and universe membership; never enters `asmctx3`/`usage4` |
| `usage4` | Frozen R2 occurrence identity using the target's `asmctx3` | Remains frozen |

No new target identity version is introduced. A dependency-context-only change
invalidates provenance without re-identifying an unchanged declaration.

## C. Final disposition and remaining prerequisites

**`READY WITH EXPLICIT QUALIFICATIONS`.** The preparation is complete and
evidence-bound, but S2.2 authorization requires explicit owner decisions and two
pre-authoring gates that are not yet recorded. No item below is a fundamental
blocker; each is a bounded, closable prerequisite.

| # | Remaining prerequisite | Evidence needed | Owner decision |
| --- | --- | --- | --- |
| 1 | Accept this entry package and decide whether to issue the separate S2.2 commission | This package; [`implementation-commission-draft.md`](implementation-commission-draft.md) | Yes |
| 2 | **Q-F2** supported-encoding decision | Correct decode/refusal **or** explicit supported-vs-unsupported/ambiguous encoding decision + tests (S21A §F; REG §4A) | Yes |
| 3 | **Q-F3** metadata-validation decision | Eager PE/assembly validation **or** explicit unsupported-reference decision + tests (S21A §F; REG §4A) | Yes |
| 4 | **NEW-04** disposition | Correct the coverage merge in S2.2 with independent qualification before acceptance, or an accepted bounded disposition (REG §4; S21B §4) | Yes |
| 5 | **A-01** disposition | Bounded hardening, or explicit acknowledgment that S2.2 creates no public/caller-supplied parse-option reuse (S21A §F) | Yes |
| 6 | **A-02** acknowledgment | Standing constraint: scope identity is not source inventory; membership proven from the universe | Yes |
| 7 | **Option A durability / S2.2–S2.3 boundary** | Choose §6.5 (a)/(b)/(c); if unenforceable, stop and revisit Option B before minting (S22ID §5.1(5)/§7) | Yes |
| 8 | **NEW-01** non-reliance | Confirm S2.2 does not rely on recovery; NEW-01 stays before S2.3 / earlier recovery reliance (REG §4; S21B §4) | Yes |
| 9 | **F16 / NEW-02** pre-authoring gate | Nine-part rows for the S2.2 slice + independent confirmation of the corrected identity matrix (REG §3; F12A §8) | Yes |

If prerequisite 7 resolves to "cannot be enforced before minting," the required
action is to **stop and return to the owner** before minting (potential Option B),
not to mint under a weaker association.

## D. Evidence basis for the disposition

- Clean canonical state and verified ancestry: §A.1 above; `git` receipts in the
  commit record.
- Scope: [`stage2-boundary.md`](../adr-ci1-01-ci1a-impl-01/stage2-boundary.md) §8,
  [`stage2-acceptance-matrix.md`](../adr-ci1-01-ci1a-impl-01/stage2-acceptance-matrix.md),
  [`s2-2-identity-handoff.md`](../adr-ci1-01-ci1a-impl-01/s2-2-identity-handoff.md) §7,
  [`s2-0-owner-handoff.md`](../adr-ci1-01-ci1a-impl-01/s2-0-owner-handoff.md).
- Contracts: [`identity-contract.md`](../adr-ci1-01-ci1a-s2-rem-01/identity-contract.md),
  [`provenance-contract.md`](../adr-ci1-01-ci1a-s2-1/provenance-contract.md),
  [`oracle-contract.md`](../adr-ci1-01-ci1a-s2-0/oracle-contract.md),
  [`case-matrix.md`](../adr-ci1-01-ci1a-s2-0/case-matrix.md).
- Obligations: [`adr-ci1-01-ci1a-s2-obligation-register.md`](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md)
  §§4A/5/7/8; [`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md) §F/§H;
  [`adr-ci1-01-ci1a-s2-2-target-identity-decision.md`](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md) §5/§7;
  [`adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md) §§7–9.

## E. Recording and boundaries

- This handoff and the other four documents in this directory are additive
  documentation; they may be committed to canonical `main` as one cleanly scoped
  docs-only change. **No push or publication.**
- No production code, test, frozen manifest, oracle, evidence package or
  historical report is modified.
- S2.2 implementation, S2.3 native publication, S2.4 reader work, S2.5
  integrated qualification and Stage 6 obligations remain separately authorized
  and gated.

## F. Next eligible commission

A separate **S2.2 implementation authorization** decision, using
[`implementation-commission-draft.md`](implementation-commission-draft.md) as
the draft text, after prerequisites C.1–C.9 are recorded. Preparation stops
here.
