# ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01

**Occurrence identity reconciliation · 28 November 2026**

## B — OCCURRENCE IDENTITY RECONCILIATION SUPPORTED WITH OUTSTANDING REQUIREMENTS

The divergence is reproduced and classified. `compctx2` is not the defect and is
not reverted. The root cause is the reference-resolution/identity boundary:
production uses the process trust-platform assembly set and absolute-path
reference identities, while the frozen F12 contract requires host-independent,
content-bound references. Correcting this changes persisted occurrence and
published identities. The commission stops before that incompatible transition,
as required, and requests an owner decision.

## 1. Baseline and seals

| Subject | Identity |
| --- | --- |
| Accepted implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Qualification seal | `b3951c790768ad257fb3418e5abe41cc5ecc5322` |
| Re-entry seal | `1776ea8ca12ba54dc85e817484711663480569a2` |
| Blocked implementation seal | `d79617727dcdcdbc0ed659c1c69dc0e175787806` |
| Blocked implementation SHA-256 | `08f26f380a08ddb419d37da994001406bb14a2648084da0a380a48fa6c2b611f` |

The blocked implementation branch and its 23-file seal verify. Predecessor
evidence is preserved and unmodified.

## 2. Reproduced divergence

Same C4 source, same production plugin, same 51 facts and two contributors:

| Observation | Target |
| --- | --- |
| xUnit host, production | `asmctx3:sha256:1844c0…e69e` |
| Console host, production | `asmctx3:sha256:f9728a…0f85` |
| Earlier test-host run | `asmctx3:sha256:b3cb5e…0ddb` |
| Independent oracle, runtime refs, Release | `asmctx3:sha256:4f4afa…eb29` |
| Independent oracle, runtime refs, Debug | `asmctx3:sha256:4454f0…91e7` |

The production descriptor is `compctx2` and lists absolute framework paths plus
testhost/xunit/consumer assemblies. The oracle uses the frozen runtime reference
set. Cardinality and contributor union are stable; identities diverge.

## 3. Root cause and contract history

- `compctx2` was introduced by the authorized S2.1 amendment for
  dependency-sensitive validity. It is not part of `asmctx3`/`usage4`.
- `asmctx3` is minted from the defining R2-faithful `compctx1`; the
  `LegacyDescriptor` mapping only substitutes the contract version and retains
  the reference items.
- `VerifiedMetadataReference.Capture` uses `file|<absolute path>|sha256`.
- `BuildPlatformReferenceEvidence` consumes all process TPA.
- The frozen S2.0/F12 contract uses host-independent, content-bound runtime
  references.

Classification: ambient host assembly discovery plus path-bound reference
identity, under a documented inconsistency between the S2.1 production identity
and the S2.0 oracle contract. It is not a `compctx2` version defect, not a target
generation defect, and not merely a test fixture error.

## 4. Required invariants

A correction must make equivalent semantic inputs produce equivalent identities,
exclude incidental host assemblies and absolute paths, preserve genuinely
different semantic references/aliases/properties, and keep contributor union and
distinct contexts correct. It must not indiscriminately exclude references.

## 5. Bounded remediation boundary

The smallest coherent correction is a new identity contract that:

1. resolves a deterministic target-framework reference closure instead of
   process TPA;
2. excludes ambient application/test assemblies not referenced by the project;
3. canonicalizes reference identity to content/logical framework identity
   without absolute paths, preserving assembly/binding semantics;
4. introduces a new identity version rather than silently mutating existing
   domains;
5. defines re-ingestion/rebuild and package compatibility.

This is an incompatible persisted-identity transition. **Not implemented** here;
owner authorization is required.

## 6. Cross-host and compatibility

Executed: console and xUnit hosts; partial. Not executed: isolated consumer host,
alternate installation paths, Linux/macOS identity runs, production-host semantic
negative controls. No compatibility is claimed for existing stores or published
packages; changing reference identity changes `compctx2`, `asmctx3`, `usage4` and
persisted occurrence authority, requiring re-ingestion or migration.

## 7. C4 oracle

The independent C4 ledger is derived (one target, 51 facts, 50/1 distribution,
two contributors, removal and caller-change variants) but is **not frozen**
against a reconciled contract. The C4 graph oracle remains frozen and valid.
Files are under `evidence/c4-oracle-unvalidated.json` and variants.

## 8. R1-06 owner exception

The owner exception is recorded: the mandatory self-excluding delta rule was
violated by the two prior seals; content integrity remains independently
supported; the seals are not compliant, not modified, and the exception is not a
general relaxation. Successor seals add only `manifest.json`. Previously closed
production findings are not reopened.

## 9. Regression and deliverables

No production or test code changed; inventory remains **1,034** tests. No new
hosted candidate run exists. The full identity correction, cross-host campaign,
C4 freeze, compatibility transition and D3 campaign remain outstanding.

## 10. Findings

| Finding | Status |
| --- | --- |
| OCC-ID-01 contract inconsistency | Open; owner transition decision |
| OCC-ID-02 ambient host references | Confirmed |
| OCC-ID-03 absolute path identity | Confirmed |
| OCC-ID-04 cross-host campaign | Partial |
| OCC-ID-05 compatibility transition | Owner decision required |
| R1-01 | Open; C4 ledger not frozen |
| R1-04, R1-05 | Not executed |
| R1-06 | Owner exception recorded |
| F-S64E-03/04/05 | Open |

## 11. Required owner decision and successor

1. Authorize a new identity contract (deterministic framework reference closure,
   content-bound reference identity), including re-ingestion/rebuild and package
   compatibility boundary; or
2. Retain the S2.1 path-bound contract and accept host-bound occurrence
   identities, which the owner previously rejected; or
3. Redefine the binding contract.

After the transition is implemented and qualified, commission
**`ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01`** for independent cross-host identity
verification, then resume the D3 implementation. DF-1 and S6.4 remain open.

No dependency, release, merge, historical seal or production change was made.
