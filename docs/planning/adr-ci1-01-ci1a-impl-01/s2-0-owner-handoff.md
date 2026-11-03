# S2.0/F12 OWNER-01 current status and S2.2 readiness handoff

**Recorded:** 2026-11-03 (Asia/Manila). **Work type:** documentation only.
**Authority:** [OWNER-01 decision](../../decisions/adr-ci1-01-ci1a-s2-0-corpus-01-owner-acceptance.md).
**Current S2.0/F12:** `ACCEPTED WITH QUALIFICATIONS`;
`F12 CLOSED — ACCEPTED WITH QUALIFICATIONS` at the frozen reference boundary.
**Q3:** `CI1A-S2.0-CORPUS-01-Q3 SUPPORTED WITH QUALIFICATIONS` (unchanged).
**S2.2:** `S2.2 SEMANTIC EXTRACTION — NOT AUTHORIZED BY OWNER-01`.
**S2.3 and later:** gated. Stage 2 is incomplete.

## Accepted reference and history

Qualified R2 is `7f8d035d7fa6bd5da49f71ee86ff54d7350318c5`, tree
`29150eb6ef2b0fe65942f3c8930037f8c0120174`. Producer manifest
`docs/evidence/adr-ci1-01-ci1a-s2-0-r2/r2-source-evidence-manifest.json`:
SHA-256 `BCE7DE2A98602E60C7631757B30EBD05B7B388D65FED69E50A43AE0CEC848B13`,
100 files verified. Q3 manifest at
`C:\Users\Laszlos\source\repos\Solo\adr-ci1-01-ci1a-s2-0-corpus-01-q3/reviewer-manifest.json`:
SHA-256 `9EFC098528340178D6A239A9C0137F8F40B8C62ACE2B1D5546561C1B56CCF52A`,
338 files verified. Both are self-excluded.

Initial producer→Q1 remediation required→R1→Q2 remediation required→R2→Q3
supported with qualifications→OWNER-01 accepted with qualifications is retained.
Both Q2 findings independently close; no material Q3 defect remains. Q3's
114/114 original, 130/130 R1, 185/185 R2 twice and 645/0/0 regression are
reviewer executions, not new OWNER-01 runs. Base corpus counts remain
11 files, 53 invocations, 21 method references, 5 overloads, 5 namespaces,
5 projects and 52 cross-project calls without variant double counting.

The active expected reference is
`../adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected-r2.json`.
Use its explicit version-specific command in the frozen
[R2 handoff](../../evidence/adr-ci1-01-ci1a-s2-0-r2/Q3-HANDOFF.md), not an
older default expected file. The frozen
[S2.0 README](../adr-ci1-01-ci1a-s2-0/README.md) and its pending-owner wording
describe producer history. This companion supplies current status without
rewriting the 100-file package. The decision records the complete closure
matrix, exact source identities, evidence interpretation and qualifications.

## Remaining work queue and gate order

| Order | Work | Boundary |
| --- | --- | --- |
| Complete | Qualified S2.1; Option A direction; qualified S2.0/F12 reference | Accepted evidence only; production semantic extraction remains unimplemented |
| Next eligible commission | Separately commission S2.2 scope/entry-criteria preparation | No implementation authorized; pin source/surfaces, F16 nine-part rows, independent confirmation of identity-matrix corrections and exact gate dependencies |
| Before target minting | Production R2-faithful defining compctx1 mapping and independent review; real defining compilation authority; enforceable Option A provenance association | Option A §5.1/§5.2/§7; Q-F2/Q-F3 corrected or explicitly decided as required by the minting gate; NEW-04 disposition before coverage reliance |
| S2.2 acceptance | Source-derived Invocation extraction and canonical outputs against accepted independent R2 corpus; unsupported/refusal/inactive/completeness behavior | Q-F2 before acceptance; Q-F3 before semantic acceptance; NEW-04 corrected/independently qualified; live S-01 binding; independent review and owner decision |
| Before S2.3 or earlier recovery reliance | Separately commissioned NEW-01 foundation recovery correction | Independent source-bound qualification and owner acceptance; preserve D3/D4/D5 |
| S2.3–S2.5 | Native publication, lifecycle/deletion/replacement, complete-empty, reopen, bounded internal lookup, real-project D3/applicable D4 | F12's original end-to-end path remains mandatory, not satisfied by reference freeze |
| Before Stage 2 acceptance | Authentic available historical/native fixtures, expanded Linux native suite and integrated S-01/obligation reconciliation | Unavailable writers need owner amendment; full platform/full D3/S-02 and broader fault qualification remain assigned later gates |

Earlier dependent reliance moves the relevant correction/evidence gate earlier.
A-01 precedes public/caller-supplied parse-option reuse; A-02 forbids treating
scope digest alone as source inventory. All other register obligations remain.

## S2.2 scope for separate authorization

Extract supported Invocation occurrences from actual verified Roslyn bindings.
Deferred categories remain explicit coverage cases. Derive canonical defining
targets using Option A: `asmctx3` uses defining R2-faithful `compctx1`,
`compctx2` supplies dependency-sensitive validity/invalidation and required
universe membership, and `usage4` remains frozen. Resolution alone cannot
establish verified authority. If after-reopen provenance cannot be enforced,
return to the owner before minting. No target identity is invented for
unsupported, unresolved, ambiguous, stale or unverified evidence.

Compare production source-derived output to independently frozen expectations;
do not generate expected results from the extractor. Plan positive, negative,
restore/determinism and Option A mapping tests. This scope does not authorize
S2.3 native publication or waive NEW-01/NEW-04.

Fixture-declared canonical components, pinned runtime references, modeled
coverage/lifecycle/provenance, purpose-built corpus and Windows-only evidence
remain explicit limitations. Preserve no-migration/no-legacy-reader/no-dual-write
policy, versioned identity, incompatible-schema refusal, accepted re-ingestion,
historical evidence and D3 graph compatibility. No database deletion is authorized.
