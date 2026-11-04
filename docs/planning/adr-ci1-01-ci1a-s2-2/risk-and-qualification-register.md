# CI1A S2.2 risk and qualification register

**Commission:** `ADR-CI1-01-CI1A-S2.2-ENTRY-01`. **Mode:** documentation only.
**Baseline:** canonical `main` `a1cf47083747bae90293ec074b35fe8078bd0d6e`.
This register carries forward every retained qualification and names the
implementation/acceptance risks for the not-yet-authorized S2.2 slice. No
qualification is closed here.

---

## 1. Retained qualifications that bind S2.2

| ID | Qualification | Source | Effect on S2.2 |
| --- | --- | --- | --- |
| Q-F2 | BOM-less UTF-16/32 decoding boundary; not forgery | S21A §F; REG §4A | Decide before S2.2 acceptance |
| Q-F3 | Lazy PE/assembly validation; valid content identity ≠ valid semantic reference | S21A §F; REG §4A | Qualify before S2.2 semantic acceptance |
| Q-F4 | Tree path binding no declared source reported as `RoslynTextMismatch` | S21A §F | Diagnostic clarification only; optional later slice |
| Q-F5 | ~30 pre-existing historical local branches | S21A §F | Housekeeping; out of commission |
| A-01 | Comma-joined preprocessor-symbol collision on the internal construction surface | S21A §F; REG §4A | Before public/caller-supplied parse-option reuse |
| A-02 | `analysisscope1` is not a source-inventory digest | S21A §F; REG §4A | Standing constraint; prove membership from the universe |
| S2.2-ID | Option A mapping/durability required before minting | S22ID §5.1/§7 | Internal G-MINT gate |
| S-01 | Full production provenance chain not yet established | REG §7 | Core S2.2 deliverable; S2.5 integrated proof |
| F16 / NEW-02 | Nine-part rows; corrected matrix confirmation before authoring | REG §3; F12A §8 | Pre-authoring gate |
| F09 | Inactive scanner contract only | REG §3 | S2.2 implements the scanner |
| F10 / IF-04 | Continuation/cross-target reader surface partial | REG §3/§6 | S2.4 |
| F11 | Semantic correspondence partial | REG §3 | S2.2/S2.5 |
| NEW-01 | Occurrence-projection recovery defect | REG §4; S21B §4 | Before S2.3 / earlier recovery reliance |
| NEW-04 | Coverage merge defect | REG §4; S21B §4 | Before coverage reliance / S2.2 acceptance |
| SQ-01…SQ-07 | Native pages/outcome/concurrency/fixtures/platform/namespace/rebuild | REG §6 | S2.3/S2.5 and later |
| D3/D4/D5 | Graph noninterference, publication authority, no-migration | OWNER §D; REG | Preserved through S2.2 |
| I-05 / MD-03 | No experimental hook; public reader boundary | REG §§6–7 | Standing; public API is ADR-CI1-02 |

## 2. S2.0/F12 reference qualifications (bounded oracle)

Accepted with qualifications by F12A §7 and the S2.0
[qualification plan](../adr-ci1-01-ci1a-s2-0/qualification-plan.md) §6:

- **Q-A fixture components** — `identifierDigest`/`declarationDigest`/
  `shapeDigest` are fixture-declared; general S2.2 source canonicalization is
  still to implement and qualify.
- **Q-B runtime references** — some identities depend on recorded .NET 10.0.12
  artifacts; another runtime needs explicit fixture qualification, not silent
  normalization.
- **Q-C model authority** — coverage/acquisition/lifecycle/provenance include
  declared models; no native/durable publication, complete-empty replacement or
  production recovery is implied.
- **Q-D purpose-built corpus** — controlled Roslyn project graph, not exhaustive
  arbitrary-repository qualification.
- **Q-E platform** — Windows reproducibility only.

These are scope boundaries, not corrected defects, and are not production
correctness guarantees.

## 3. S2.2 implementation risks

| Risk | Likelihood | Impact | Mitigation / gate |
| --- | --- | --- | --- |
| Roslyn binding yields a different `asmctx3` than the frozen expectation | Medium | High | WP1/WP2 independent vectors and G-MINT before minting; compare against the frozen R2 oracle |
| R2-faithful `compctx1` mapping for dependency-bearing compilations is ambiguous or nondeterministic | Medium | High | Deterministic documented mapping; separate vectors; independent review; stop if not deterministic |
| `compctx2` leaks into `asmctx3`/`usage4` or a frozen identity is redefined | Low | High | Structural guard rails; negative test that `compctx2` cannot enter either identity; frozen helper tests |
| Option A durable association cannot be enforced before minting | Medium | High | Owner §6.5 decision; stop and return to owner (possible Option B) per S22ID §5.1(5)/§7 |
| Cross-project binding fails for complex dependencies | Medium | High | Real multiproject corpus; adversarial target-binding tests; live-instance gating |
| Coverage merge (NEW-04) silently overstates completeness | Medium | High | Correct/qualify before any dependent claim; independent qualification before acceptance |
| Inactive/ambiguous/external sites become facts or complete-empty | Medium | High | Refusal matrix; no-empty-success tests; `ConditionalInactive` never a fact |
| Q-F2/Q-F3 accepted limits are silently exceeded | Medium | High | Explicit decisions (P5/P6); discriminating tests; bound dependent claims |
| Scope creep into S2.3 native publication or S2.4 reader | Medium | Medium | Commission exclusions; stop conditions; separate authorization |
| Reliance on NEW-01 recovery | Low | High | Non-reliance confirmation (P11); focused tests independent of recovery |
| Self-generated oracle or circular validation | Low | High | Expected values only from the frozen R2 oracle; independent Q1 review |
| Producer evidence mistaken for independent qualification | Medium | Medium | IR+O closure rule |

## 4. Non-production limitations that remain

- No production occurrence extraction, publication, reopen or lifecycle is
  established by this preparation.
- No public Find Usages API or consumer contract.
- No migration, dual-write, legacy readers or adapters; superseded data is
  refused or re-ingested (no data deletion authorized).
- No full D3/D4/D5 qualification, no Linux native qualification, no
  crash/power-loss/multi-process guarantee, no MVCC/snapshot isolation.
- Frozen R2 evidence and historical failed reviews remain preserved and
  qualified; no historical defect is retrospectively closed.

## 5. Register discipline

Every risk maps to an existing accepted gate. This register invents no new
correctness condition and waives none. If an S2.2 work package reaches a
later-gated dependency earlier, that gate moves before the reliance.
