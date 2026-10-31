# CI-1-SCOPE-OWNER-01: R3 Owner Acceptance and Integration

**Project:** Andirit

**Milestone:** CI-1-SCOPE-OWNER-01 — owner acceptance of the CI-1-SCOPE-R3
semantic acceptance oracle following the independent CI-1-SCOPE-Q4
qualification, and integration of the exact qualified artifacts

**Recorded:** 2026-10-31

**Decision authority:** explicit owner instruction in the CI-1-SCOPE-OWNER-01
request ("Owner decision: ACCEPT WITH QUALIFICATIONS")

**Executor:** delegated acceptance recorder; the Git author label records
automation provenance, not a human signature or an independent review event.
The recorder did not conduct the Q4 review and does not issue a second review
verdict.

**Disposition: ACCEPT WITH QUALIFICATIONS.**

This record documents an already-made owner decision. It does not reopen the
independent review, does not redesign the oracle, and does not authorize CI-1A
implementation, storage-architecture selection, package publication or release.

## A. Authority

The owner accepts the R3 semantic acceptance oracle within the scope
independently qualified by CI-1-SCOPE-Q4. The owner instruction:

- accepts **CI-1-SCOPE-R3** as the planning-oracle successor to the historical
  R2 oracle, with every Q4 qualification preserved;
- authorizes integration of **only** the seven Q4-qualified R3 artifacts and
  the two append-only documentation amendments listed in section G;
- requires the integrated oracle's acceptance evidence to be reproduced from
  the canonical repository before the integration is reported complete;
- preserves R1 acceptance-reconciliation item 5 (proven definition/assembly
  correspondence) as the requirement R3 fulfils — this is an existing
  acceptance requirement, not a new architectural contract.

## B. Exact source binding

| Role | Identity |
| --- | --- |
| Repository / canonical checkout | `jenolaszlo-sketch/andirit` / `D:\projects\Andirit` |
| R3 frozen qualification baseline | `5c89ac0b3d0e87c0112880cf985826ded42b21b2` / tree `e61994289e5f96203ded37be000b0ed3bb3b79a2` |
| Published C# correctness baseline (starting HEAD) | `d02baaa8e7e8704f2f5dac9ab549779f45fa5cb6` / tree `406282797948c035b3dda4f70b9ec5c5b536a6d7` |
| R3 producer workspace | `C:\Users\Laszlos\source\repos\Solo\ci1-scope-r3` |
| Q4 independent reviewer workspace | `C:\Users\Laszlos\source\repos\Solo\ci1-scope-q4` |
| Q4 disposition | `CI-1-SCOPE-Q4 SUPPORTED WITH QUALIFICATIONS` |
| Q4 reproduced oracle evidence | `r3-run.json` SHA-256 `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` |
| Q4 independent probe evidence | `q4-probes.json` SHA-256 `ab48e8325c3b6076cc27af10bf659d4babef6e088f6436fb8fa361d37c15d36c` |
| Integrated-repository reproduction | `r3-run-integrated.json` SHA-256 `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` (byte-identical) |

The seven R3 integration artifacts were verified byte-for-byte against
`INTEGRATION-MANIFEST.md` and against the Q4 candidate copies before copying;
the two amendment blocks match their recorded sizes and hashes
(5,168 B `9f202c24…`; 2,814 B `39d2350f…`).

## C. Decision

**`CI-1-SCOPE-R3: ACCEPT WITH QUALIFICATIONS`**

The owner accepts the independently qualified R3 oracle for its frozen CI-1
acceptance scope and records the integration of the exact qualified artifacts.

## D. Accepted contract

Within the qualified compilation context, the accepted implementation
establishes:

- versioned typed semantic assertions (`ci1-scope-r3/semantic-assertions/1`);
- semantic outcomes derived from Roslyn binding evidence, not from the
  declared label;
- exact target-definition correspondence (defining compilation, assembly
  identity, documentation ID, declaration path and span), with the expected
  definition resolved uniquely inside the expected declaring compilation;
- explicit defining-compilation and assembly correspondence, including the
  compilation-reference and metadata negative rules;
- source declaration identity verification (path + span + lexeme coordinates);
- outcome-facet validation beyond broad classification (dispatch
  characteristic, cross-project, after-replacement, conditional activity);
- conditional-activity checks for every row, independent of the expected label;
- lexical-owner validation from Roslyn syntax ancestry;
- repository-path validation against the authorized fixture mapping;
- frozen-corpus completeness validation (missing/unexpected/contradiction/
  duplicate occurrence identity);
- controlled malformed-manifest diagnostics for the qualified CLI surface
  (see Q4-F02 for the library-boundary exception);
- explicit reporting of unqualified lifecycle assertions;
- reproducible adversarial mutation evidence.

This fulfils R1 acceptance-reconciliation item 5: positive expectations require
proven definition/assembly correspondence, not canonical-name equality.

## E. Independent review basis

Q4 independently established (reproduced, not accepted from the producer):

- the original Q3 failures are authentic (byte-identical tier-0 evidence);
- the R3 oracle rebuilds successfully and its evidence reproduces
  byte-identically (`0b301850…`);
- **198 mutation cases executed**; **196 invalid mutations rejected for their
  intended reasons** (reason-code checked per mutation); **two positive
  controls accepted**; **zero crashes** in the qualified mutation suite; zero
  unrelated-only rejections;
- the 126/126 confirmed-family sweep passes (no recognized-label substitution
  accepted);
- **17 additional independent Q4 adversarial probes** were executed across
  exact identity, assertion semantics, dependency context, ownership, corpus
  integrity and provenance;
- no false semantic confirmation was discovered; no incorrect outcome
  assertion was accepted; no valid supported positive case was incorrectly
  refused;
- the integration patch was applied and executed in a disposable export of the
  published tree with byte-identical output.

These are observed qualification results for the frozen corpus and supported
compilation model — not general guarantees of semantic correctness for every
C# project.

## F. Preserved qualifications

The owner preserves every Q4 finding exactly as recorded:

1. **Q4-F01 — same assembly identity across distinct compilations
   (bounded limitation; future graduation condition).** The descriptor's
   "defining compilation" component is an assembly-name/identity string; two
   distinct compilations sharing one assembly identity are not distinguishable
   by the descriptor. The frozen audited-expectation resolver refuses
   unsupported cross-compilation identity cases rather than guessing.
   Acceptance does not authorize confirmation in those unproven contexts;
   expanding support requires additional compilation/definition provenance
   evidence.
2. **Q4-F02 — unreadable fixture at the library boundary (documentation
   correction; deferred robustness improvement).** An unreadable fixture path
   can throw `FileNotFoundException` from the library boundary. The qualified
   CLI does not expose that path in its accepted execution flow. Overly broad
   "all malformed or unavailable inputs produce structured diagnostics" claims
   are corrected in the integration documentation; the implementation was not
   changed to eliminate this nonblocking qualification.
3. **Q4-F03 — incomplete R3 export ledger (recorded provenance limitation).**
   Two entries in R3's `frozen-export-hashes.json` contain `sha256: null`.
   Q4 independently verified all 675 frozen exported files against its own Git
   archive. The incomplete producer ledger is distinguished from the verified
   integrity of the export; the original evidence remains unchanged and the
   nulls were not rewritten.
4. **Q4-F04 — cross-project generic expectations (bounded unsupported case).**
   Cross-project generic/reduced-extension identities outside the frozen
   expected-target table are explicitly refused rather than fabricated. The
   oracle's supported target vocabulary is not expanded by this integration;
   future support requires explicit acceptance cases and independent evidence.
5. **Q4-F05 — provenance recorded rather than pinned (preserved provenance
   limitation).** Fixture and trusted-platform-assembly provenance is recorded,
   not universally pinned through an independently enforced mechanism. Q4
   verified the relevant source identities for its execution. No universal
   reproducibility across arbitrary SDK/runtime environments is claimed;
   stronger provenance enforcement may be required before broadening the oracle
   beyond the frozen corpus.

Additional preserved limitations:

- **Lifecycle:** exactly five rows report `LIFECYCLE_NOT_QUALIFIED`; the R3
  gate requires those qualifications to remain present. Static binding does not
  establish publication replacement or stale-fact retirement.
- **Basis statements:** some are human-reviewed rather than mechanically
  proven.
- **Compiler independence:** the oracle uses Roslyn; it is independent from the
  Andirit production implementation but is not a second compiler
  implementation.
- **Scope:** the oracle validates the frozen CI-1 acceptance corpus, not every
  language construct or project topology.
- **Unsupported capabilities:** runtime dispatch expansion, constructor
  discovery, method-group targets, local-function targets and indirect impact
  remain outside the qualified scope.
- **Candidate-result comparison:** no CI-1A implementation exists; comparison
  with production `FindUsagesV1` results remains unqualified.
- **Platform:** the integrated oracle was executed on Windows in this
  milestone; the planning project is not part of the CI workflow, so no
  multi-platform execution of the oracle is claimed.

## G. Integration inventory

Starting from published HEAD `d02baaa…` (tree `406282…`), the canonical
repository received exactly the following, and nothing else:

### Added files (7)

| # | Path | Bytes | SHA-256 (verified in repository) |
| --- | --- | --- | --- |
| 1 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/OracleR3.csproj` | 450 | `0859b07a8b7e3a079a6a379a1bc96a3646c31dfcfbef90721bc0c62d8404ddb8` |
| 2 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Contracts.cs` | 17,949 | `478770d78c46912bff2b1ffa962aa3b5b89477c12ec885d996b137d2f6ee1d01` |
| 3 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Sem.cs` | 8,385 | `2cf259b8259f95a5ce2610442c31169df2736f2c3f16c79deab096cb8dcddd53` |
| 4 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Audit.cs` | 27,880 | `686ca1d76d4dfaa085715ffdf3d26b0d2d8d6a3f55093497d2da484e178780ef` |
| 5 | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Program.cs` | 47,077 | `323b410bf4909e80b9536adb1a3da6818563c24f372db55cfdb70bf5f68c130c` |
| 6 | `docs/planning/ci1-scope-r2/analysis/identity-probe/IdentityProbeR3.csproj` | 464 | `35e4b3335cae8c9ad67180e672224e9e2b6c817e5e56d0595e5f70ef93088b6a` |
| 7 | `docs/planning/ci1-scope-r2/analysis/identity-probe/Program.cs` | 10,830 | `c3242fbad8fce9d135c16880c653f3c0211dfcd53aa861d2d8c5b805b8d00afc` |

### Append-only amendments (2)

| # | Destination | Appended bytes | Block SHA-256 |
| --- | --- | --- | --- |
| 8 | `docs/planning/ci1-scope-r2/oracle-contract.md` | +5,168 | `9f202c24cc45e4cc1263c7e7c535306b16c42528d07bc874c58acc346c58eb1c` |
| 9 | `docs/planning/ci1-scope-r2/mutation-matrix.md` | +2,814 | `39d2350fb7562747f3710c52fc0f9c5a5bfa40c28ad964ea5f6cecb55977d9fb` |

For both amendments the pre-existing file prefix was preserved byte-for-byte
(verified before/after); only the block was appended. The canonical working
tree uses LF endings (8,807 → 13,975 bytes; 5,414 → 8,228 bytes), so the
integrated files differ from Q4's CRLF `git archive`-based patched copies only
by line endings (content equal modulo EOL, verified).

The original R2 oracle, before-harness, fixtures, frozen 32-row manifest, the
15 declared outcome strings and all R1/R2/Q1/Q2/Q3 evidence remain unchanged.
R2 remains preserved as historical evidence; R3 is the accepted planning-oracle
successor.

## H. Explicit non-claims

This acceptance does **not**:

1. Implement or accept CI-1A; no `FindUsagesV1` candidate, occurrence storage,
   persistence schema or query API is added.
2. Select between ADR-CI1-01 options B2 and C; that decision remains open.
3. Establish general correctness of C# Find Usages for every project,
   compilation topology or language construct.
4. Qualify publication replacement/stale-fact retirement (lifecycle) or
   candidate-result comparison.
5. Authorize package publication, tagging, release or deployment.
6. Claim cross-platform execution of the planning oracle (it is not part of the
   CI workflow) or universal reproducibility across arbitrary SDK/runtime
   environments.
7. Convert any human-reviewed basis statement, recorded-only provenance or
   unqualified lifecycle assertion into a mechanical guarantee.

## I. Closure

- **CI-1-SCOPE-R3: ACCEPTED AND CLOSED.**
- **CI-1-SCOPE-Q4: QUALIFICATION COMPLETE.**
- **CI-1-SCOPE-OWNER-01: OWNER ACCEPTANCE RECORDED.**

The integration commit and its tree, plus the bookkeeping commit and final
local HEAD, are recorded in
[`docs/planning/ci1-scope-r3-integration/final-handoff.md`](../planning/ci1-scope-r3-integration/final-handoff.md)
and in the milestone handoff. The integration is local only; publication
requires separate owner authorization.

Any future CI-1A implementation still requires its own engineering and
independent qualification gates. The next separately authorized engineering
milestone is ADR-CI1-01: a minimal B2-versus-C implementation experiment to
select the storage representation for individual C# invocation occurrences
while preserving existing aggregate graph relationships.
