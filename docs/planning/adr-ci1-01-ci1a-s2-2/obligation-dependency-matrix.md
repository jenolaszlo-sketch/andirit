# CI1A S2.2 obligation and dependency matrix

**Commission:** `ADR-CI1-01-CI1A-S2.2-ENTRY-01`. **Mode:** documentation only.
**Baseline:** canonical `main` `a1cf47083747bae90293ec074b35fe8078bd0d6e`.
**Rule:** no obligation is marked closed without evidence. An obligation keeps
its original severity and source; this matrix reconciles, and does not rename or
waive. "Blocks" names the earliest accepted gate; earlier reliance moves the
gate earlier.

Authority keys: **S21A** = [S2.1 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md);
**REG** = [consolidated obligation register](../../decisions/adr-ci1-01-ci1a-s2-obligation-register.md);
**S22ID** = [Option A decision](../../decisions/adr-ci1-01-ci1a-s2-2-target-identity-decision.md);
**S21B** = [S2.1 authorization boundary](../../decisions/adr-ci1-01-ci1a-s2-1-authorization-boundary.md);
**F12A** = [S2.0/F12 owner acceptance](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md);
**COV** = [coverage-and-availability](../adr-ci1-01-ci1a-s2-rem-01/coverage-and-availability.md).

---

## 1. The seven named S2.2 obligations

### Q-F2 — encoding boundary

| Field | Content |
| --- | --- |
| Exact source | S21A §F "Q-F2"; REG §4A `Q-F2 / S21Q1 (bounded encoding)`; F12A §8; [`adr-ci1-01-ci1a-s2-1-owner-acceptance.md`](../../decisions/adr-ci1-01-ci1a-s2-1-owner-acceptance.md) §H |
| Requirement | BOM-less ASCII-range UTF-16/32 can decode as NUL-interleaved UTF-8 while retaining an honest byte-hash identity. S2.2 must **correct the decode/refusal or record an explicit supported-encoding decision** distinguishing supported from unsupported/ambiguous encodings, with tests. This is not source-byte forgery (bytes are correctly hashed and text truthfully attributed). |
| Current evidence / disposition | Accepted S2.1 limitation; explicitly **not closed**. Not addressed by S2.0 (`qualification-plan §5`). Existing S2.1 tests cover UTF-8/BOM, UTF-16/32 policy, invalid sequences, Unicode, empty files, CRLF/LF, but not a supported/unsupported-encoding decision. |
| Blocks | **S2.2 acceptance** (S21A §H; REG §4A). Also listed in the S22ID §7 minting gate as "corrected or explicitly decided." Earlier dependent claims must be bounded. |
| Required probe / remediation | Decide the supported encoding set; either refuse ambiguous BOM-less UTF-16/32 or explicitly document them as unsupported/ambiguous; add discriminating tests; independent evidence; IR+O. If S2.2 semantic extraction consumes decoded text, the decision must precede any semantic-correspondence claim. |
| Dependencies | S2.1 decoding/tree correspondence (accepted). Feeds S-01 semantic correspondence. |
| Owner decision | Yes — supported-encoding decision (R-AUTH P5). |

### Q-F3 — PE metadata validity

| Field | Content |
| --- | --- |
| Exact source | S21A §F "Q-F3"; REG §4A `Q-F3 / S21Q1 (bounded metadata validity)`; F12A §8 |
| Requirement | Invalid metadata artifacts (netmodule, truncated PE) can receive an honest captured identity before Roslyn fully validates usability. A valid content identity does not imply a valid semantic reference. S2.2 must **qualify metadata validation/refusal behavior** (eager assembly/PE validation or an explicit unsupported-reference decision), with tests. |
| Current evidence / disposition | Accepted S2.1 limitation; explicitly **not closed**. S2.1 captures PE images and binds identity to exact captured bytes, but lazy Roslyn validation leaves the boundary open. Not addressed by S2.0. |
| Blocks | **S2.2 semantic acceptance** (S21A §H; REG §4A). Listed in the S22ID §7 minting gate as "corrected or explicitly decided." |
| Required probe / remediation | Qualify whether S2.2 relies on lazy metadata validation; add refusal tests for netmodule/truncated/invalid PE; or record an explicit unsupported-reference decision. Independent evidence; IR+O. |
| Dependencies | S2.1 reference capture (accepted). Feeds semantic target binding. |
| Owner decision | Yes — metadata-validation decision (R-AUTH P6). |

### A-01 — comma-symbol canonicalization

| Field | Content |
| --- | --- |
| Exact source | S21A §F "Additional qualification A-01"; REG §4A `A-01 / corroborating Q1 run (bounded canonicalization)` |
| Requirement | The source-item canonical form joins preprocessor symbols with `,`; a caller-supplied single symbol containing `,` can collide with two ordinary symbols. Not reachable through accepted project inputs (DefineSettings split on `;`/`,`) or any public API; bounded to the internal construction surface. |
| Current evidence / disposition | Accepted bounded qualification; **not closed**. |
| Blocks | **Before public exposure or caller-supplied parse-option reuse** (S21A §H; REG §4A). **Not an S2.2 entry/acceptance blocker** provided S2.2 creates no public or caller-supplied parse-option reuse. |
| Required probe / remediation | Either harden the separator at the internal construction surface, or record an explicit finding that S2.2 introduces no public/caller-supplied parse-option reuse. Independent evidence if changed; IR+O. |
| Dependencies | S2.1 source-item canonicalization (accepted). |
| Owner decision | Yes — bounded hardening or explicit non-exposure acknowledgment (R-AUTH P8). |

### A-02 — scope-digest boundary

| Field | Content |
| --- | --- |
| Exact source | S21A §F "Additional qualification A-02"; REG §4A `A-02 / corroborating Q1 run (boundary)`; [`provenance-contract.md`](../adr-ci1-01-ci1a-s2-1/provenance-contract.md) §3 |
| Requirement | `analysisscope1` binds repository + project-unit/path pairs; per-unit source lists are enforced by universe construction/descriptors, not by the scope digest. Consumers must **not** treat scope identity alone as a source-inventory digest. |
| Current evidence / disposition | Documented boundary; standing constraint; **not closed** (by design). |
| Blocks | Standing constraint through S2.5; blocks any S2.2 claim that derives source inventory or completeness from the scope digest alone. |
| Required probe / remediation | No production change required. S2.2 must prove scope/universe membership from `VerifiedCSharpUniverse`/descriptors, not from `analysisscope1`. Record this in S2.2 evidence. |
| Dependencies | S2.1 `VerifiedCSharpScope`/`VerifiedCSharpUniverse` (accepted). |
| Owner decision | Yes — acknowledgment as a standing constraint (R-AUTH P9). |

### S-01 — source-byte provenance during S2.2

| Field | Content |
| --- | --- |
| Exact source | REG §7 `S-01 / OQ1 §B.1; OWNER §F; RECON §5`; REG §3 F11; F12A §8; S21B §3/§6 |
| Requirement | The complete trust chain `source bytes → verified buffer → Roslyn tree → compilation → live binding → target/occurrence identity` must hold for S2.2. S2.1 proved bytes/buffers/trees/compilation/context membership. S2.2 must establish the **actual live defining compilation / source / target correspondence** used for each minted fact. |
| Current evidence / disposition | Contract fixed; S2.1 verified acquisition/provenance accepted. **Full production chain at S2.2 is open.** Frozen R2 targets use the defining `compctx1`; full production binding is S2.2/S2.5. |
| Blocks | **S2.2 implementation and acceptance** (core deliverable). Integrated independent proof at **S2.5**. |
| Required probe / remediation | Per-fact evidence that the semantic model belongs to the exact verified live compilation; cross-project target binds to the dependency's verified live compilation; missing/conflicting → refusal. Independent evidence; IR+O. |
| Dependencies | S2.1 `IVerifiedCSharpSession.Provenance` / `VerifiedCSharpUniverse.RequireCompilation`. Feeds D3/D4 at S2.3+. |
| Owner decision | No new decision; scope confirmation within the commission. |

### NEW-01 — occurrence-projection recovery

| Field | Content |
| --- | --- |
| Exact source | REG §4 `NEW-01 / Significant`; REG §3 F05; S21B §4; S21A §H; F12A §8 |
| Requirement | `DurableCommandLog.ReplayAsync` restores graph units, not occurrence availability/coverage/publication binding; known-failure replay loses occurrence projection; deliberate cross-unit conflict after replay may bypass lossy in-memory validation. **Targeted foundation correctness remediation** required, with graph **and** occurrence projections asserted against durable authority. |
| Current evidence / disposition | **Known correctness qualification; not fixed.** Assigned follow-up `ADR-CI1-01-CI1A-S2-FOUNDATION-RECOVERY-01` (separate authorization pending). |
| Blocks | **Before S2.3 and any production recovery reliance, including earlier use.** Does **not** block S2.2 **provided S2.2 does not rely on failure/occurrence-projection recovery** (S21B §4: focused source/compilation tests verify S2.1 independently of NEW-01). |
| Required probe / remediation | Reproduce pre-commit and in-memory apply failures; assert graph **and** occurrence projections against durable authority, publication identity/coverage/facts/conflict refusal, successful recovery availability, unknown-quarantine distinction, close/reopen; preserve D3/D4/D5. IR+O. |
| Dependencies | Durable publication lifecycle (S2.3). |
| Owner decision | Yes — confirm S2.2 non-reliance and that NEW-01 remains before S2.3 (R-AUTH P11). |

### NEW-04 — coverage merge

| Field | Content |
| --- | --- |
| Exact source | REG §4 `NEW-04 / Implementation follow-up`; REG §3 F03/F08/F10; S21B §4; COV §2; [identity mapping](../adr-ci1-01-ci1a-s2-0/identity-and-provenance-mapping.md) [§5] |
| Requirement | `CodeOccurrenceCoverage.Merge` takes the first non-null scope/universe and max counts; the constructor exempts `declared=0` from `verified<=declared`, so mixed scopes can claim `Produced`. S2.2 must **correct the coverage-authority merge** (or record an accepted bounded disposition) and independently qualify it. |
| Current evidence / disposition | **Defect preserved, not closed.** S2.0 includes a scope-compatibility fixture and does **not** use the defective merge as an oracle (case matrix `NEW-04`). S21B §4 prohibits using the merge as an S2.1 provenance oracle. |
| Blocks | **Before any dependent coverage claim; otherwise before S2.2 acceptance.** If S2.2 routes real coverage through this merge, correction + independent qualification become prerequisites to that claim. |
| Required probe / remediation | Test matching/different scope, matching/different universe, verified empty vs missing evidence/no analysis/invalid zero declarations, partial+complete, changed membership; native persistence/reopen if published. IR+O. |
| Dependencies | `CodeOccurrenceCoverage`; `CodeGraphIngestionSink`; native coverage aggregation (S2.3 for durable). |
| Owner decision | Yes — correction in S2.2 vs accepted bounded disposition (R-AUTH P7). |

---

## 2. Adjacent S2.2-shaping gates (not in the named seven)

| ID | Requirement / source | Blocks | Evidence / authority |
| --- | --- | --- | --- |
| **F16 / NEW-02** | Nine-part traceability rows before each dependent slice's test authoring; full rows before S2 acceptance. Corrected R2 matrix/encoding must be independently confirmed. | **S2.2 qualification-test authoring**; all rows before S2 acceptance. | REG §3 F16; F12A §8; S21A §H. IR+O. |
| **S2.2-ID mapping** | Implement the Option A §5.1 mapping contract and required tests; independent review before target minting. | **S2.2 target minting.** | S22ID §§5.1/5.2/7; REG §4A. IR+O. |
| **Option A durability** | Record defining `compctx2` in publication/coverage evidence so the gate survives reopen; else return to owner (possible Option B). | **S2.2 target minting.** | S22ID §5.1(5)/§7. Owner decision required. |
| **F09 inactive scanner** | Actual disabled-text scanning bound to declared language/preprocessor configuration; no confirmed-fact path for inactive. | **S2.2 implementation/acceptance.** | REG §3 F09. IR+O. |
| **F11 semantic correspondence** | Full semantic correspondence for the verified chain. | **S2.2 acceptance**; integrated S-01 proof S2.5. | REG §3 F11; S21A §H. IR+O. |
| **F02 / F01 minting authority** | F02: actual out-of-scope/ambiguous/missing defining-compilation refusal before minting, plus genuine cross-project positive. F01: real source/target correspondence before S2.2 acceptance. | **S2.2 acceptance.** | REG §§2–3. IR+O. |
| **MD-03 / I-05** | Internal-only reader; no public Find Usages (ADR-CI1-02). No experimental interleaving hook in production. | Standing constraint at S2.2 and later. | REG §§6–7. |

---

## 3. Dependency and gate ordering

```
S2.1 accepted (verified buffers/trees/compilation/compctx2/scope/universe)
        │
S2.0/F12 R2 oracle accepted (bounded reference)
        │
Option A mapping decision accepted
        │
        ▼
S2.2 ENTRY PREPARATION (this package)  ──► owner decisions Q-F2/Q-F3/
        │                                      NEW-04/A-01/A-02/durability/
        │                                      NEW-01 non-reliance
        ▼
S2.2 AUTHORIZATION (separate owner decision)
        │
        ├─ WP1 WP2 (mapping/compatibility) ──► G-MINT independent review
        │                                             │
        │                                             ▼
        ├─ WP3 WP4 WP5 WP6 WP7 (extraction/minting/coverage/invalidation)
        │
        └─ WP8 integration + F16 rows + independent Q1 review
        │
        ▼
S2.2 ACCEPTANCE (Q-F2 + F11 + NEW-04 + mapping review satisfied)
        │
        ▼
S2.3 (NEW-01 closure, native publication/reopen/lifecycle)
```

**Earlier-reliance rule.** If any S2.2 work package reaches a later-gated
dependency (e.g., routes real data through the NEW-04 merge, or relies on
recovery), that dependency's correction/evidence gate moves before the reliance.
No gate is waived by scheduling.

---

## 4. Obligation closure authority

- **IR+O** — independent source-bound review **and** owner acceptance; producer
  tests are not closure.
- **O-doc** — owner-verified documentation correction (none outstanding for
  S2.2 entry; NEW-02/F14 doc fixes already recorded).
- **Standing** — accepted limitation retained without closure (A-02 and the
  non-production qualifications).

No obligation in this matrix is marked closed by this preparation.
