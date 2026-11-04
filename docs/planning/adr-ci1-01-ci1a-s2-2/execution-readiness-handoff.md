# CI1A S2.2 execution-readiness handoff

**Commission:** `ADR-CI1-01-CI1A-S2.2-OWNER-01`. **Date:** 2026-11-04.
**Decision:** [`ADR-CI1-01-CI1A-S2.2-OWNER-01`](../../decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md).
**Disposition:** `ACCEPTED WITH QUALIFICATIONS`; bounded S2.2 implementation issued.
**S2.2 implementation state:** architectural decision checkpoint reached —
**implementation not yet begun**. S2.3/S2.4/S2.5/Stage 6/publication: not authorized.

This handoff is the first decision checkpoint required by the commission. It
records the accepted entry baseline and explicit qualifications, the Q-F2/Q-F3
architectural dispositions, the Option A durability boundary, A-01/A-02/S-01/
NEW-01/NEW-04/F16-NEW-02 dispositions, the ordered work packages with dependency
gates, exact remaining blockers and the evidence/qualification plan. Dependent
production implementation must not begin until `G-CONFIRM` is satisfied and the
governing decision is recorded.

---

## A. Accepted entry baseline and qualifications

Baseline is the entry-preparation commit `56877aa728236b440c90dc1876d1c21b2dfab064`
(tree `363e937a9685718c422be946e055f32dfa9b9b1e`); F12 acceptance parent
`a1cf470…`; qualified R2 `7f8d035…`; S2.1 baseline `b7fea0d…` and reviewed
production source `c02d5eae…`. Clean tree, one worktree, no stashes, no push.

Accepted qualifications (carried, none closed): Q-F2, Q-F3, A-01, A-02, S2.2-ID
mapping/durability, S-01, NEW-01, NEW-02/F16, NEW-04, F09, F10/IF-04, F11,
SQ-01…SQ-07, D3/D4/D5 and the S2.0/F12 bounds (fixture-declared digest forms,
host-toolchain-bound identities, purpose-built corpus, modeled coverage/
provenance, Windows-only). See the
[risk and qualification register](risk-and-qualification-register.md) and the
[decision §4](../../decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md#4-explicit-qualifications-and-nonclaims-accepted-with-entry).

## B. Disposition summary

| Obligation | Disposition | Kind |
| --- | --- | --- |
| Q-F2 | Explicit supported-encoding set (BOM UTF-8/16/32 + BOM-less UTF-8) enforced by a bounded `AmbiguousBomlessEncoding` refusal for NUL-containing BOM-less decodes | Contract + bounded correction |
| Q-F3 | Eager `GetMetadata()` validation authoritative (already forced in `Capture`); add adversarial tests; bounded `UnsupportedMetadataReference` fallback if a non-assembly image still captures an identity | Contract + tests |
| A-01 | Bounded hardening: reject preprocessor symbols containing `,`/`\|` (`UnsupportedParseConfiguration`) | Bounded correction |
| A-02 | Recorded: `analysisscope1` is not source inventory; membership from the verified universe | Statement |
| Option A durability | Option (a): extraction-time enforcement + designed durable carrier; reopen enforcement is the first S2.3 gate; Option B escalation if S2.3 cannot persist/enforce | Boundary decision |
| F16 / NEW-02 | Nine-part rows defined below; independent NEW-02 matrix confirmation before dependent test authoring (`G-CONFIRM`) | Gate |
| NEW-01 | Confirmed non-reliance; NEW-01 stays before S2.3 | Gate |
| NEW-04 | Scope/universe-safe merge, unconditional `verified<=declared`, no `Math.Max` scope mixing, no `Produced` from mismatched scopes; independent falsification | Bounded correction |
| S-01 | Preserve full byte→binding chain per fact; integrated proof S2.5 | Invariant |

Full rationale and inspected locations are in the
[decision §3](../../decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md#3-architectural-dispositions).

## C. Ordered implementation packages and dependency gates

Gates:

| Gate | Meaning |
| --- | --- |
| `G-CONFIRM` | F16 rows recorded and the NEW-02-corrected identity matrix independently confirmed (before dependent qualification-test authoring) |
| `G-MINT` | WP3 mapping/compatibility contract independently reviewed (before any target minting) |
| `G-DUR` | Option A durability boundary recorded (resolved as (a)); S2.3 reopen enforcement is a downstream gate |
| `G-ACCEPT` | S2.2 acceptance (Q-F2, Q-F3, NEW-04, S-01, mapping review satisfied) |

| WP | Work | Authority / files | Prerequisite | Completion evidence |
| --- | --- | --- | --- | --- |
| **WP0** | Decisions, F16 rows, statements (this checkpoint) | `docs/decisions/adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md`, this handoff | — | Recorded owner decision |
| **WP1** | Q-F2 bounded refusal correction + exact supported-encoding contract | `src/Andirit.CSharp/VerifiedCSharpSource.cs`; `tests/Andirit.CSharp.Tests/VerifiedCompilationProvenanceTests.cs` | WP0 | BOM-less UTF-16/32 refusal tests; BOM-present acceptance; no verified identity on refusal |
| **WP2** | Q-F3 eager-validation adversarial tests (+ bounded unsupported-reference guard if needed) | `src/Andirit.CSharp/VerifiedCSharpCompilation.cs`; C# tests | WP0 | Truncated/garbage/netmodule refusal before identity; valid assembly captured |
| **WP3** | A-01 symbol hardening + Option A mapping contract: R2-faithful `compctx1` derivation for dependency-bearing defining compilations; `compctx2` validity consumption with `compctx2`-isolation guard rails | `src/Andirit.CSharp/CompilationProvenanceDescriptor.cs`, `VerifiedCSharpCompilation.cs`, `VerifiedCSharpAnalysis.cs`; C# tests | WP0, WP1, WP2, `G-CONFIRM` | Deterministic independent vectors; negative proof `compctx2` cannot enter `asmctx3`/`usage4`; **`G-MINT` independent review** |
| **WP4** | `Invocation` extraction from the same live compilation used for graph facts: identifier-token span, full declaration span, source hash, `method-invocation` | `src/Andirit.CSharp/CSharpCodeGraphPlugin*.cs` | WP3 | Focused positive/negative semantic tests |
| **WP5** | Canonical target minting with exact live-instance / defining `compctx2` / `compilation-universe1` gating | `src/Andirit.Abstractions/CodeOccurrence.cs` (consume only); C# extraction | WP4 + `G-MINT` | Adversarial target-binding tests; distinct defining compilations → distinct `asmctx3` |
| **WP6** | NEW-04 coverage-merge correction | `src/Andirit.Abstractions/CodeOccurrence.cs`; `src/Andirit/CodeGraphIngestionSink.cs`; tests | WP0 | Independent falsification probes; no mixed-scope `Produced` |
| **WP7** | Coverage/refusal emission (`Confirmed`/`External`/`Ambiguous`/`Unresolved`/`Unsupported`/`ConditionalInactive`/`Incomplete`) + unconditional-inactive scanner (F09) | `src/Andirit.CSharp/*`; `src/Andirit.Abstractions/CodeGraphExtraction.cs` | WP5, WP6 | Refusal matrix; inactive sites never facts |
| **WP8** | Option A invalidation + durable association carrier (option (a)) | extraction/coverage model; native persistence **excluded** | WP5, `G-DUR` | Transitive-only change invalidates; no churn; association designed durable |
| **WP9** | Integration/comparison over the accepted R2 corpus with frozen expectations; F16 evidence; reproducibility | tests + evidence under `docs/evidence/` | WP4–WP8 | Byte-identical determinism; independent Q1 review; `G-ACCEPT` |
| **EXCL** | Native publication/reopen/lifecycle (S2.3), reader continuation (S2.4), integrated acceptance (S2.5), public API, background/incremental, Stage 6 | — | — | Not authorized |

**Gate ordering:** WP0 → {WP1, WP2, WP6} → `G-CONFIRM` → WP3 → `G-MINT` → WP4 →
WP5 → {WP7, WP8} → WP9 → `G-ACCEPT` → (separate) S2.3 with NEW-01 closed and
reopen-enforced durability.

No package begins while a prerequisite decision is unresolved. `G-CONFIRM` must
clear before dependent qualification tests are authored; `G-MINT` must clear
before any canonical target is minted.

## D. F16 nine-part traceability rows for S2.2

Each row is: **requirement, authority, production surface, observable behavior,
positive test, adversary, environment, evidence artifact, closure authority**.
Rows must not credit future tests as passing; all rows are required before S2.2
acceptance.

| # | Requirement | Authority | Production surface | Observable behavior | Positive test | Adversary | Env | Evidence artifact | Authority |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Supported-encoding set + ambiguous BOM-less refusal (Q-F2) | S21A §F; REG §4A | `VerifiedCSharpSource` | Refusal, no verified identity | BOM UTF-8/16/32 accepted | BOM-less UTF-16/32 NUL decode → `AmbiguousBomlessEncoding` | native (Roslyn) | test TRX + log | IR+O |
| 2 | Eager PE/assembly validation (Q-F3) | S21A §F; REG §4A | `VerifiedMetadataReference.Capture` | Throw before identity | Valid assembly captured | Truncated/garbage/netmodule refused | native | test TRX + log | IR+O |
| 3 | Symbol canonicalization hardening (A-01) | S21A §F | `VerifiedCSharpCompilation.Create` | `UnsupportedParseConfiguration` | Ordinary symbols deterministic | `,`/`\|` symbol refused | native | test TRX | IR+O |
| 4 | Scope is not source inventory (A-02) | S21A §F; S2.1 contract §3 | `VerifiedCSharpScope`/`Universe` | Membership from universe | Matching scope/universe accepted | Supply-source mismatch refused | native | test + statement | IR+O |
| 5 | R2-faithful `compctx1` derivation for dependency-bearing defining compilations | S22ID §5.1(1); identity mapping §2 | `CompilationProvenanceDescriptor` | Deterministic mapped digest | Independent vector reproduces | Nondeterminism/omitted dependency refused | native | vectors + oracle log | IR+O (G-MINT) |
| 6 | `compctx2` isolation from identity | S22ID §5.1(3)(7) | minting/validation | `compctx2` absent from `asmctx3`/`usage4` | Frozen vectors unchanged | Inject `compctx2` into identity → impossible/refused | native | negative test | IR+O (G-MINT) |
| 7 | Canonical target minting under live authority | S22ID §5.1(2); stage2-boundary §3.5 | extraction + `CodeOccurrence.Identity` | `asmctx3` from verified defining `compctx1` | Cross-project positive | Missing/ambiguous/external/unbound refused | native | probes + log | IR+O |
| 8 | Invocation extraction fidelity | stage2-boundary §3.6–3.7; acceptance matrix §4 | `CSharpCodeGraphPlugin*` | Token span, declaration span, source hash, category | ≥ fixture invocations | Wrong span/hash → fail | native | test TRX | IR+O |
| 9 | Coverage/refusal classification | stage2-boundary §3.9/§5 | extraction + `CodeOccurrenceCoverage` | Correct state; no empty success | Confirmed/complete-empty only when complete | External/ambiguous/unresolved/unsupported refused | native | matrix + TRX | IR+O |
| 10 | Unconditional-inactive scanner (F09) | REG §3 | extraction | `ConditionalInactive`, no fact | Active branch → fact | `#if`-inactive site → coverage only | native | test TRX | IR+O |
| 11 | Coverage-merge authority (NEW-04) | REG §4; S2.1 boundary §4 | `CodeOccurrenceCoverage.Merge`/ctor | No mixed-scope `Produced` | Same scope merges | Differing scope/universe/`declared=0` refused | native | falsification probes | IR+O |
| 12 | Live defining/source/target correspondence (S-01) | REG §7; S2.1 boundary §6 | verified analysis + extraction | Fact attributable to compiled input/live instance | Cross-project live binding | Substituted/stale/missing refused | native | chain evidence | IR+O |
| 13 | Option A durability association | S22ID §5.1(5) | extraction/coverage model (native excluded) | Defining `compctx2` carried separately; stale refused | Same context valid | Transitive-only change invalidates | native | test + boundary statement | IR+O (S2.3 reopen) |
| 14 | F16/NEW-02 confirmation itself | REG §3; F12A §8 | docs/tests | Rows present; matrix confirmed | Reviewer confirms rows | Missing row blocks authoring | — | review record | IR+O |

## E. Exact remaining blockers

| # | Blocker | Gate | Owner/reviewer action |
| --- | --- | --- | --- |
| B1 | Q-F2/Q-F3/A-01 corrections and adversarial tests not yet implemented/run | WP1/WP2/WP3, `G-ACCEPT` | Implement + test; independent review |
| B2 | NEW-04 coverage-merge correction not implemented or independently qualified | WP6, `G-ACCEPT` | Correct + falsification; independent review |
| B3 | F16 rows not yet independently confirmed; NEW-02 matrix confirmation pending | `G-CONFIRM` | Reviewer confirmation before dependent test authoring |
| B4 | Option A mapping/compatibility contract not implemented or independently reviewed | WP3, `G-MINT` | Implement + independent review before minting |
| B5 | S2.3 reopen enforcement of the durability association outstanding | S2.3 (downstream) | S2.3 first gate; Option B escalation if unenforceable |
| B6 | Independent Q1 review of S2.2 not yet performed | `G-ACCEPT` | Separate reviewer, not the implementer |
| B7 | NEW-01 recovery closure outstanding | S2.3 | Separate commission before S2.3/recovery reliance |

## F. Evidence and qualification plan

- **Producer evidence** (implementer) and **independent review evidence**
  (separate reviewer) are recorded separately; producer runs are never
  qualification.
- Expected values come only from the frozen R2 oracle/case matrix, never from
  the extractor under test. The R2 oracle is a bounded reference, not a
  production correctness guarantee.
- Each case records actual inputs (bytes/hash/path/encoding, options,
  references, live instances), observed identities, refusal reasons,
  reproduction command, environment and self-excluded manifest.
- Determinism: byte-identical repeated and separate-process runs.
- Independent Q1 review reproduces positives, negatives, determinism and the
  mapping contract, then the owner records acceptance (`G-ACCEPT`).
- No evidence credits a future test as passing; all F16 rows are required before
  S2.2 acceptance.

## G. Stop conditions

Stop immediately and return to the owner if: source/ancestry cannot be
authenticated; a change would invalidate the accepted semantic reference; the
Option A durability choice cannot be enforced (present bounded Option B before
minting); required F16/NEW-02 evidence is missing before dependent test
authoring; a change would require unapproved S2.3+ functionality; the
implementation introduces unqualified semantic drift; `compctx2` would enter
`usage4`/`asmctx3`; NEW-01 recovery would be relied upon; D3/D4/D5 would be
weakened; or a public API becomes mandatory.

## H. Readiness statement

The architectural decision checkpoint is complete. Entry is
`ACCEPTED WITH QUALIFICATIONS`; all required dispositions are recorded; the
bounded work packages and gates are ordered. Dependent production
implementation is held behind `G-CONFIRM` (WP1/WP2/WP6 first) and `G-MINT`.
This handoff reports **readiness for bounded S2.2 implementation** and stops at
the architectural decision checkpoint. S2.3 and later remain unauthorized.
