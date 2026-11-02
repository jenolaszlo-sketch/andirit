# ADR-CI1-01 — Pre-Release Identity and Occurrence Schema Compatibility Policy

**Recorded:** 2026-11-02. **Authority:** owner direction in `ADR-CI1-01-CI1A-IMPL-01-S2.1-OWNER-01`.
**Status:** Adopted pre-release policy. **Scope:** experimental occurrence identities and occurrence-specific storage schemas only.
**Relationship to prior authority:** reinforces and extends the accepted D5 no-migration decision; does not amend D3 legacy-graph semantics, D4 publication authority, or unrelated public contracts.

---

## 1. Policy context

The owner confirms:

- Andirit currently has **no production users or production occurrence data** requiring backward compatibility.
- Its occurrence-indexing architecture is under active development; experimental identities and native occurrence schemas may change while correctness and qualification are established.
- Maintaining backward compatibility with obsolete experimental formats would create implementation and test obligations without corresponding user value.

This policy applies **only** to experimental occurrence-related formats and identities. It does not automatically authorize changes to established legacy graph semantics, `FindCallers`, `FindReferences`, graph node/edge identity, accepted D3 invariants, unrelated persisted data, or publicly released contracts with actual consumers.

## 2. Normative policy

**POL-01 — Correctness first.** Choose the semantically correct identity and storage model, even when it is incompatible with an earlier experimental implementation.

**POL-02 — No experimental data migration.** Do not implement migration from superseded experimental occurrence identities or native occurrence schemas.

**POL-03 — No legacy experimental readers.** Do not preserve compatibility readers, adapters, fallback parsers or dual-write mechanisms solely to read obsolete experimental occurrence formats.

**POL-04 — Re-ingestion.** Superseded experimental occurrence databases may be discarded and reconstructed from authoritative source inputs. Re-ingestion is the supported replacement for migration.

**POL-05 — Explicit version identifiers.** Identity algorithms and persisted occurrence formats must remain explicitly versioned or otherwise unambiguously identified. A version identifier does not imply a requirement to implement earlier versions.

**POL-06 — Refuse incompatible data.** Never silently interpret an obsolete identity or schema as the current version. Unsupported persisted occurrence formats must produce a clear refusal or re-ingestion-required outcome.

**POL-07 — No silent algorithm changes.** A materially changed canonical identity algorithm must be explicitly documented, using a new version designation or other accepted unambiguous contract identity.

**POL-08 — Historical evidence preservation.** Preserve frozen vectors, historical decisions, previous test results and reviewer artifacts as evidence. Historical evidence preservation does not require runtime compatibility.

**POL-09 — No compatibility machinery for hypothetical users.** Do not add migration frameworks, version adapters or multi-schema runtime support purely to protect nonexistent production consumers.

**POL-10 — Reconsider at release maturity.** Backward-compatibility obligations must be reconsidered when Andirit has real released consumers or persisted user data to preserve. This pre-release policy is not an indefinite assumption about future deployments.

## 3. Reconciliation with D5 and earlier compatibility language

| Dimension | Current accepted position |
|---|---|
| Migration from experimental occurrence schemas | **Not required** — D5 accepted "no migration"; this policy reinforces it |
| Conservative refusal of unsupported occurrence data | Required — POL-06; pre-REM data is refused wholesale, never reinterpreted |
| Graph-only compatibility (D3/D5 boundaries) | **Unchanged** — aggregate graph and Hetu-labelled historical compatibility contracts remain in force (ADR-CI1-05 as amended; EC6D-OWNER-02) |
| Re-ingestion | The supported path to current occurrence functionality |
| Identity versions | Explicit versioning required (POL-05/POL-07); no obligation to implement superseded versions |

Any older planning text that suggested maintaining legacy experimental occurrence readers solely for experimental compatibility is **superseded for experimental occurrence formats only** by this additive policy record. No historical decision is edited; no accepted D5 guarantee is weakened. No conflict between D5 and this policy was found: D5 covers storage-schema migration; this policy additionally covers experimental canonical identity supersession and explicitly re-states the same no-migration, refuse-incompatible posture.

## 4. Data disposal safety

This policy permits re-ingestion instead of migration. It does **not** authorize this documentation commission (or any agent acting under it) to delete any actual database, fixture or user file.

Future implementation agents must:

- identify affected experimental stores explicitly;
- provide clear refusal/re-ingestion instructions;
- preserve historical database fixtures needed for independent evidence;
- obtain separate authorization for any destructive cleanup.

## 5. Consequences for upcoming stages

- S2.2 may mint a new canonical occurrence/identity version if the semantic identity decision requires it; experimental data produced under superseded identities is refused or re-ingested, never migrated.
- S2.3/S2.4 native publication/reader work is not obligated to support superseded experimental occurrence schemas.
- Frozen R2 vectors, historical owner decisions, prior review artifacts and accepted evidence remain preserved for traceability even when their runtime contract is superseded.

## 6. Standing boundary

D3 graph semantics, D4 publication authority and D5's graph/Hetu historical compatibility scope remain in force. A future change to those guarantees requires a separate owner amendment, not this pre-release policy.
