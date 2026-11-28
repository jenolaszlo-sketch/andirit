# Proposed commission — ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01

**Status: draft, contingent on owner authorization of the identity transition.
Do not begin without separate authorization.**

## Purpose

Independently verify the corrected occurrence identity contract across hosts and
semantic inputs, including compatibility of the transition and the completed C4
ledger.

## Entry conditions

- Owner authorizes the identity transition and defines the compatibility/
  re-ingestion boundary.
- Producer implements the bounded reference-resolution and identity correction
  with a new identity version.
- C4 occurrence ledger is frozen under the corrected contract.
- Existing-store migration/rebuild decision is recorded.
- Producer evidence is sealed and verifies from a clean checkout.

## Required work

- Cross-host campaign: console, xUnit, isolated consumer, alternate working
  directories and equivalent reference installation paths; Windows x64 and
  genuine Linux x64; macOS regression only.
- Semantic negative controls: different references, aliases, versions and
  binding properties produce distinct identities; incidental host assemblies and
  paths do not.
- Verify all 51 C4 facts, 50/1 contributor distribution, union and removal/
  caller-change variants.
- Verify graph/mirror contracts and S6.1–S6.3 regression remain intact.
- Verify persisted occurrence compatibility or the authorized rebuild contract.
- Verify R1-06 exception remains limited and successor seals comply.

## Boundaries

No dependency upgrade, P-03 optimization, S6.5 certification, release changes,
historical seal modification, main merge, DF-1 closure or S6.4 closure.
