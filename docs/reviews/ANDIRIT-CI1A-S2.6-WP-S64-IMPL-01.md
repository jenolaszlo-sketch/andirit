# Proposed commission — ANDIRIT-CI1A-S2.6-WP-S64-IMPL-01

**Status: draft for owner decision. Scope limited to the remaining S6.4 D3
corpus/oracle implementation and evidence.**

Authorized re-entry baseline: accepted implementation `ec984bf…`, tree
`8ec0eb7a…`; producer seal `870b600…`; independent qualification seal
`b3951c7…`; re-entry evidence on `decisions/s64-entry-01-r1`.

## 1. Remaining requirements

1. **C4 occurrence contributor ledger.** Derive the exact `usage4` and target
   identities for the `Caller`/`Caller2` two-unit union from the accepted
   authority with the production plugin/repository identities. Freeze it as
   `expected/c4-occurrences.json`.
2. **D3-01…08 campaign.** Implement the cases in
   `d3-case-matrix.md` using existing `Andirit.CSharp.Tests` and
   `Andirit.LatticeDb.Tests` infrastructure. Reuse the C# lifecycle setup,
   conformance suite, query presets, fault hook, worker and receipt patterns.
3. **Raw mirror and traversal receipts.** Directly census mirror nodes, edges,
   manifests, endpoints and payloads; exercise incoming, outgoing and both
   routes with ordering/truncation; repeat on enabled reopen and the
   enabled→disabled→enabled transition.
4. **Lifecycle sequences.** Initial/staged invisibility, source and symbol
   replacement, C1/C2/C3 unit deletion, known-failure rollback, retry and fresh
   process reopen.
5. **H1 copy checks.** Use the manifest-bound historical stores only as
   disposable copies under `PROVENANCE.md`; retain prior codec exclusions.
6. **Named receipts.** Exact-candidate Windows x64 and genuine hosted Linux x64
   named-case results; macOS remains a regression gate. Preserve all inherited
   CI gates and coverage floors.
7. **Sealed producer evidence.** Self-excluding manifest, source/oracle
   manifests, derivation/comparison commands, failure/retry receipts and exact
   tested identities.

## 2. Scope and prohibitions

- May add tests, test utilities, fixture staging and scoped evidence paths.
- May add additive S6.4 receipt capture/verification under `.github/scripts` and
  the existing CI test job if attribution requires it. Justify each changed
  path against a D3 row.
- No production behavior/API changes, dependency upgrades, new LatticeDbSharp
  graph API adoption, Bolt integration, architectural expansion, P-03
  optimization, release changes, NuGet publication, or main merge.
- Stop and return an owner decision request if a target-qualified route cannot
  exercise a required case or a new contract delta appears.

## 3. Acceptance inputs

- Re-entry corpus/oracle seal and `corpus-manifest.json`.
- C1/C2/C3/C4 expected ledgers and `validation.json`.
- D3 case matrix and controls.
- DF-1 reconciliation.
- Existing S6.1–S6.3 acceptances and the P-03 erratum.

## 4. Subsequent qualification

After producer handoff, commission
**`ANDIRIT-CI1A-S2.6-WP-S64-QUAL-01`** for independent re-derivation, raw native
replay and D3 adjudication. Only a later owner decision may discharge DF-1 and
close S6.4.

## 5. Entry condition

The owner must authorize this commission separately. The remediation acceptance
does not authorize it automatically.
