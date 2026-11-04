# G-CONFIRM review package (F16 / NEW-02 traceability)

**Commission:** `ADR-CI1-01-CI1A-IMPL-01-S2.2`. **Gate:** `G-CONFIRM`.
**Status:** prepared for an **independent reviewer**. **Not self-certified.**
`G-CONFIRM` remains **open** until a separate reviewer records a verdict and
independently confirms the NEW-02-corrected identity matrix.
**Authority:** obligation register §3 F16; F12 acceptance §8; entry acceptance §3.6.

---

## 1. Purpose

Provide the nine-part S2.2 traceability rows and the independent-confirmation
requirements so a reviewer can clear `G-CONFIRM`, which gates dependent
qualification-test authoring. Rows must **not** credit future tests as passing.

## 2. Nine-part rows

Each row: **requirement, authority, production surface, observable behavior,
positive test, adversary, environment, evidence artifact, closure authority**.

| # | Requirement | Authority | Production surface | Observable behavior | Positive test | Adversary | Env | Evidence artifact | Closure |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Supported-encoding set + ambiguous BOM-less refusal (Q-F2) | S21A §F; register §4A | `VerifiedCSharpSource` | Refusal, no verified identity | BOM UTF-8/16/32 accepted | BOM-less UTF-16/32 NUL decode → `AmbiguousBomlessEncoding` | native | TRX + log | IR+O |
| 2 | Bounded assembly/metadata capture boundary (Q-F3) | S21A §F; register §4A; Q1-F01 qualification | `VerifiedMetadataReference.Capture` | Valid supported metadata is captured; malformed assembly-name, type-name, and method-signature heap handles refuse before identity; no universal PE or metadata validity guarantee | Valid assembly and supported metadata extent | Corrupt assembly-name/type-name/method-signature heap indices, truncation, garbage, and netmodule inputs | native | TRX + exact malformed image details + disposition | IR+O |
| 3 | Symbol canonicalization hardening (A-01) | S21A §F | `VerifiedCSharpCompilation.Create` | `UnsupportedParseConfiguration` | Ordinary symbols deterministic | `,`/`\|` symbol refused | native | TRX | IR+O |
| 4 | Scope is not source inventory (A-02) | S21A §F; S2.1 contract §3 | `VerifiedCSharpScope`/`Universe` | Membership from universe | Matching scope/universe accepted | Supply-source mismatch refused | native | test + statement | IR+O |
| 5 | R2-faithful `compctx1` derivation under a declared comparison profile | S22ID §5.1(1); mapping contract §2 and its comparison-profile table | `CompilationProvenanceDescriptor` plus the frozen R2 oracle | Equal complete inputs yield equal mapped digests; the frozen corpus digest is required only under its complete fixture profile | Independent same-input vector; separately, fixture-aligned Leaf/Core anchors | Different labels/inventory are not treated as equal inputs; omitted dependency or nondeterminism is refused | native | full profile/inventory + vectors + oracle log | IR+O (G-MINT) |
| 6 | `compctx2` isolation from identity | S22ID §5.1(3)(7) | minting/validation | `compctx2` absent from `asmctx3`/`usage4` | Frozen vectors unchanged | Inject `compctx2` into identity → impossible/refused | native | negative test | IR+O (G-MINT) |
| 7 | Canonical target minting under live authority | S22ID §5.1(2); stage2-boundary §3.5 | extraction + `CodeOccurrence.Identity` | `asmctx3` from verified defining `compctx1` | Cross-project positive | Missing/ambiguous/external/unbound refused | native | probes + log | IR+O |
| 8 | Invocation extraction fidelity | stage2-boundary §3.6–3.7 | `CSharpCodeGraphPlugin*` | Token span, declaration span, source hash, category | ≥ fixture invocations | Wrong span/hash → fail | native | TRX | IR+O |
| 9 | Coverage/refusal classification | stage2-boundary §3.9/§5 | extraction + `CodeOccurrenceCoverage` | Correct state; no empty success | Confirmed/complete-empty only when complete | External/ambiguous/unresolved/unsupported refused | native | matrix + TRX | IR+O |
| 10 | Unconditional-inactive scanner (F09) | register §3 | extraction | `ConditionalInactive`, no fact | Active branch → fact | `#if`-inactive site → coverage only | native | TRX | IR+O |
| 11 | Coverage-merge authority (NEW-04) | register §4; S2.1 boundary §4 | `CodeOccurrenceCoverage.Merge`/ctor | No mixed-scope `Produced` | Same scope merges | Differing scope/universe/`declared=0` refused | native | falsification probes | IR+O |
| 12 | Live defining/source/target correspondence (S-01) | register §7; S2.1 boundary §6 | verified analysis + extraction | Fact attributable to compiled input/live instance | Cross-project live binding | Substituted/stale/missing refused | native | chain evidence | IR+O |
| 13 | Option A durability association | S22ID §5.1(5) | extraction/coverage model (native excluded) | Defining `compctx2` carried separately; stale refused | Same context valid | Transitive-only change invalidates | native | test + boundary statement | IR+O (S2.3 reopen) |
| 14 | F16/NEW-02 confirmation itself | register §3; F12A §8 | docs/tests | Rows present; matrix confirmed | Reviewer confirms rows | Missing row blocks authoring | — | review record | IR+O |

## 3. Independent reviewer must additionally confirm

1. The NEW-02-corrected identity matrix (R2 §§4–5; Q3 `TARGET-IDENTITY-REVIEW`)
   is consistent with the S2.1/S2.0 accepted evidence — independent of the
   producer.
2. Every row's authority resolves to an accepted decision, and no row credits a
   future test as passing.
3. No dependent qualification test may be authored until this verdict is
   recorded.

## 4. Evidence the reviewer records

- Exact matrix document/version read and the frozen evidence hashes relied on.
- Row-by-row confirmation or the named defect.
- Any row that cannot yet be evidenced at `G-CONFIRM` time, with the stage at
  which it will be (this is expected: rows 5–14 evidence lands during WP3–WP9).

## 5. Verdict (reviewer-owned)

One of: `G-CONFIRM PASS`, `G-CONFIRM PASS WITH QUALIFICATIONS`,
`G-CONFIRM FAIL`. The S2.2 producer does **not** issue this verdict.

## 6. Current state

Rows are recorded, but the independent matrix confirmation and a
`G-CONFIRM` verdict are **outstanding**. The historical Q1 qualification report
records the Q-F3 deferred malformed type-name and method-signature reads; the
bounded WP2 remediation and its malformed-name/blob adversaries are tracked as
separate evidence and do not establish universal PE/metadata validity or erase
those historical observations. The independent Q2 review remains pending.
Dependent qualification work gated by G-CONFIRM and extraction remain blocked.

## 7. Row 5 comparison-profile confirmation

Row 5 has two independent expected-value checks. First, algorithm fidelity
compares the independent R2 reconstruction and mapped derivation over the same
complete inputs, including unchanged verified reference identities. Second,
exact frozen-corpus reproduction uses the accepted S2.0 fixture's three
content-bound `file|runtime/...` framework labels and all other frozen inputs.
The production capture profile retains the host's actual verified reference
inventory and normalized paths. In the reviewer environment those live labels
were under `C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/`;
the fixture labels omit that host path. Therefore the reviewer-only comparison
may align just the PE preimage labels to the authenticated fixture identities
to verify the two frozen Leaf/Core anchors. This test-only adaptation is disclosed,
limited to expected-value comparison, and must never be copied into production
reference identities or frozen authority.

The NEW-02 `usage4` tuple correction remains governed by the frozen matrix. Its
source identity retains the source-bytes-versus-tree-text distinction, and
`analysisscope1` remains a declared scope identity rather than a source
inventory. These are independent of the row-5 reference-profile adaptation.
