# REM-01 Contract Reconciliation

**Commission:** `ADR-CI1-01-CI1A-S2-BOUNDARY-REM-01`
**Status:** Contract and production-foundation remediation; Stage 2 remains
unauthorized.
**Starting source:** `4c6d26a1d103a62fffe2e2743467eedb5f21afc2` (tree
`320e965f219d0b615da29665d7fa3e85f8bbfc15`).
**Starting review:** `ADR-CI1-01-CI1A-S2-BOUNDARY-Q1B`, disposition
`REMEDIATION REQUIRED`.

This document reconciles the Q1B findings against accepted architecture. It is
the contract gate for the production changes in this commission.

---

## 1. Controlling authority

| Area | Authority |
|---|---|
| Occurrence representation | ADR-CI1-01 owner decision, Candidate C |
| Identity | R2 `PROVENANCE-CONTRACT.md` §§2, 4, 5; Q3 `PROVENANCE-REVIEW.md`, `TARGET-IDENTITY-REVIEW.md`; owner decision §F |
| Publication/index | Owner decision D4 |
| Legacy graph | Owner decision D3 |
| Historical/schema | Owner decision D5 |
| Stage 1 | `adr-ci1-01-ci1a-s1-owner-acceptance.md` and `adr-ci1-01-ci1a-s1-obligation-reconciliation.md` |

The Q1B review is evidence of defects, not authority to replace these decisions.

---

## 2. Q1B blocking findings

| Q1B ID | Accepted requirement | Defect found | Corrected contract | Production components |
|---|---|---|---|---|
| F01 | Exact R2 `usage4`/`asmctx3` tuple; no substitution | Production helper hashed repository/target/category/span/content hash and a four-field target; ignored plugin, semantic unit, source hash, target document, metadata identity, identifier digest, declaration digest and shape digest | Implement R2 `usage4` and `asmctx3` with big-endian part lengths, R2 domain strings and canonical formats; separate semantic source unit from storage contributor; validate/recompute | `CodeOccurrence.cs`, `CodeGraphBatchValidator.cs`, tests |
| F02 | External/unverified target is `UnsupportedExternalEvidence` refusal, never a confirmed fact | Stage 2 matrix required an `External` fact and mapped it to `Available` | External targets cannot obtain canonical `asmctx3`; no fact, explicit coverage/refusal; Stage 2 matrix corrected | Stage 2 docs; `CodeOccurrenceTargetId` canonical validation |
| F03 | Resolution, coverage/completeness and availability are distinct | Six labels mixed three dimensions; `Incomplete` absent; coverage not persisted; `External`/`ConditionalInactive` -> `Available` | Composite coverage record with analysis state, counters, scope/universe and refusal reasons; distinct query availability `IncompleteAnalysis`; persisted with publication | `CodeOccurrenceCoverage`, reader, in-memory/native stores |
| F04 | Complete-empty analysis is `Available` with zero facts | Sink set analysis flag from occurrence count; zero-fact unit removed; reopen returned `AnalysisUnavailable` | Analysis-performed evidence independent of fact count; zero-fact unit persists; reopen restores complete-empty | Sink, replacement, native unit/meta, in-memory view, reader |
| F05 | Unknown commit outcome is conservative; no false rollback/success or blind retry | `MutateAsync` could replay old commands after a successful commit with lost acknowledgment | Distinguish unknown outcome; quarantine the store instance; reopen reconstructs durable state; known pre-commit failure remains recoverable; no blind retry | `LatticeCodeGraphStore` |
| F06 | Preauth contracts, implementation slices and acceptance evidence are distinct | Gates required unbuilt abstractions and future tests; fixture timing and counts contradicted | Contract-first gate; S-01/IF-03/SQ-02/SQ-03 classified; fixture timing; corrected matrix | Stage 2 boundary/matrix/handoff |

---

## 3. Significant and minor findings

| Q1B ID | Disposition in REM-01 |
|---|---|
| F07 identity enforcement | Implemented: canonical ID formats, tuple recomputation, payload-hash recomputation, no caller-supplied payload hash. |
| F08 coverage persistence | Implemented as additive fields on unit/meta records; old records restore as `Partial`, never complete. |
| F09 conditional inactive | Contract now requires a declared configuration and explicit exclusion; production extractor remains Stage 2 work. No false fact path. |
| F10 query contract | Reader now returns coverage and `IncompleteAnalysis`; missing-target complete-empty is distinguishable. Continuation remains a Stage 2 internal-reader deliverable. |
| F11 provenance | Byte capture remains as reviewed; `compctx1`/scope/universe/live binding remain Stage 2 work. Production identity helpers now accept canonical `compctx1`/digests. |
| F12 fixture | Not made Stage-2-qualifying by this commission. The existing fixture is documented as below minima and unreferenced. |
| F13 evidence metadata | Erratum issued in `evidence-integrity-erratum.md`; originals preserved. |
| F14 documentation | Corrected in the Stage 2 documents where it affects this remediation. |
| F15 historical/Linux timing | Reconciled in `stage2-gate-reconciliation.md`; authentic writer availability is explicit. |
| F16 matrix traceability | Corrected gates; full nine-part matrix remains a Stage 2 preparation obligation. |

---

## 4. Identity decision detail

### 4.1 `usage4`

Accepted R2 tuple:

```
Hash("usage4",
  repository, plugin, "usage4-b2c-exp-r2-20261101", semanticUnit,
  path, sourceHash, startLine, startColumn, endLine, endColumn,
  targetDocumentId, asmctx3, "method-invocation")
```

Each part is length-prefixed with a **big-endian** 32-bit byte length. The
result is 64 lowercase hex characters. The storage contributor is not part of
the tuple; the semantic calling/source unit is.

### 4.2 `asmctx3`

```
identifierDigest  = Hash("asmctx3-identifier", identifier-site forms)
declarationDigest = Hash("asmctx3-declarations", full canonical declaration forms)
shapeDigest       = Hash("asmctx3-target", shape)
asmctx3 = "asmctx3:sha256:" + Hash("asmctx3-context",
  "compctx1", compctx1, metadataIdentity, moduleName,
  identifierDigest, declarationDigest, shapeDigest)
```

The production helper now requires a canonical `compctx1:sha256:<64 hex>` value
and canonical component digests. It does not accept an opaque context or an
`asmctx3:unavailable` target as a confirmed identity.

### 4.3 Contributor union

R2 distinguishes the semantic source unit from the storage unit that contributed
a fact. Stage 1 conflated them. Under the corrected contract:

- `CodeOccurrenceFact.SourceUnitId` is the semantic calling unit and participates
  in `usage4`.
- `CodeOccurrenceContributor.IndexUnitId` is the storage contributor and does not
  participate in `usage4`.
- Identical facts from different storage units of the **same semantic unit** union
  contributors.
- Different semantic source units produce different `usage4` identities and
  therefore distinct facts, even at the same span/target.

This preserves the accepted R2 union semantics and removes the Q1B F16
"two projects -> one fact" oracle error.

### 4.4 Compatibility

- Existing persisted `CodeOccurrenceId` values produced by the pre-REM helper are
  **not** canonical R2 `usage4` values. They are not silently reinterpreted.
- Existing persisted `CodeOccurrenceTargetId` values had no canonical prefix and
  were not canonical `asmctx3`. They are refused by the new constructor on
  restore/validation rather than promoted.
- The durable occurrence schema remains format `andirit-occurrence-facts/1` with
  additive JSON fields for coverage and analysis state. This is not a migration:
  old records lacking coverage restore as `Partial` (analysis ran but
  completeness unproven), never as complete.
- A database containing pre-REM identity values must be rebuilt by re-ingestion
  under the corrected semantics. The store refuses non-canonical identities; it
  does not rewrite them.

**Compatibility consequence:** Stage 1 databases produced by the pre-REM helper
must be treated as historical/non-canonical. This is a qualification, not an
experimental-schema migration. D5's no-migration policy is preserved.

---

## 5. External-target decision

The accepted R2 authority refuses a target whose defining compilation is outside
the declared universe (`UnsupportedExternalEvidence`). The corrected Stage 2
contract is:

| Case | Result |
|---|---|
| External target, no verified defining compilation | No fact; coverage/refusal with `External` or `UnsupportedExternalEvidence`; query never reports confirmed |
| Valid cross-project target, defining compilation verified and in scope | Confirmed fact owned by the calling unit, target from the defining context |
| Equal descriptors in two verified compilations | Distinct targets; each binds only to its own context |
| Descriptor-only or missing defining compilation | Refusal; no canonical target |

Production enforcement in this commission: `CodeOccurrenceTargetId` accepts only
`asmctx3:sha256:<64 hex>`; `asmctx3:unavailable` and fabricated values are
rejected. Stage 2 extraction remains responsible for refusing before minting.

---

## 6. Coverage and zero-fact decision

`CodeOccurrenceCoverage` now carries:

- analysis state (`Produced` = complete declared scope; `Partial` = analysis ran
  but completeness unproven; `NotProduced` = no analysis; `Unavailable` =
  unavailable);
- invocation/unsupported counters and refusal reasons;
- `ScopeId` (`analysisscope1`) and `UniverseId` (`compilation-universe1`);
- declared/verified unit counts.

`Produced` with zero facts is a valid complete-empty analysis. `Partial` with
zero facts is not a complete negative and returns
`CodeOccurrenceAvailability.IncompleteAnalysis`. `NotProduced` returns
`AnalysisUnavailable`.

Persistence:

- `PersistedOccurrenceUnit.Coverage` stores per-unit evidence.
- `PersistedOccurrenceMeta.CoverageState`, `ScopeId`, `UniverseId` and
  `CompleteCoverage` store publication-level evidence.
- Legacy records without coverage restore as `Partial` if they contain facts or
  claim analysis, otherwise unavailable.
- No completeness is inferred from an empty fact collection.

---

## 7. Unknown-outcome decision

`MutateAsync` applies the command to the local projection first (so ordinary
validation is known before any native write), persists, and only then advances
`_commands`. A post-commit fault is wrapped as
`CodeGraphCommitOutcomeUnknownException`; the store sets `_outcomeUnknown` and
refuses further reads/mutations until reopened. Reopen reconstructs durable state.

Known pre-commit failure remains recoverable: the projection is replayed from the
unchanged command log. No rollback is claimed for an unknown outcome; no blind
retry is permitted. This is a deterministic ack-loss simulation, not a
crash/power-loss qualification.

---

## 8. Gate decision

The corrected Stage 2 matrix classifies each requirement as one of:

- preauthorization contract;
- existing implementation prerequisite;
- authorized Stage 2 deliverable;
- Stage 2 acceptance evidence;
- later CI-1A obligation.

S-01 and IF-03 are **contract prerequisites** before authorization; their
production types/tests are implementation/acceptance work. SQ-02 policy is
recorded; SQ-03's serialized model is accepted, with a native schedule test as a
Stage 2 acceptance obligation.

---

## 9. Stop-condition review

No stop condition was engaged:

- No new identity scheme was introduced; the accepted R2 tuple was implemented.
- No storage architecture or D3/D4/D5 guarantee was replaced.
- No experimental-schema migration was introduced.
- No public Find Usages API or Stage 2 extractor was implemented.
- The durable schema change is additive JSON, not a format migration; old
  non-canonical identities are refused rather than reinterpreted.

The pre-REM database compatibility qualification remains explicit.
