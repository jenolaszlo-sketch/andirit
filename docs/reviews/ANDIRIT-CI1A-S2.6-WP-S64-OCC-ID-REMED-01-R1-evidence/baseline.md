# Baseline and authorization

## 1. Accepted investigation baseline

| Subject | Identity |
| --- | --- |
| Investigation branch | `remediation/s64-occ-id-01` |
| Investigation payload | `b03edfeb13073d2806f085ce3c9f5a1a92be2a53` |
| Investigation seal | `81102d21b4d6f6ed510b0555bb0d034aa1705c44` |
| Investigation seal tree | `81bdedc2936b1bc62f57476038807bff116f102a` |
| Investigation seal SHA-256 | `b7a93c56809a15f5c3d39a3d3c6680bcbb1eecbe0c6ddef2c5a2a9cd9149f0dd` |
| Investigation evidence | 22 files, verified from a clean checkout |

The investigation seal is the accepted baseline, not the implementation
deliverable, and is unmodified.

## 2. Predecessor seals (unmodified)

| Subject | Identity |
| --- | --- |
| Accepted implementation | `ec984bfe63e5fbe13047cee8920e00b634ab17b0` |
| Implementation tree | `8ec0eb7a5e647a171f54fa8969dadbb6c65da179` |
| Producer seal | `870b600acf1490337af9f483f8894a5e8166a90e` |
| Qualification seal | `b3951c790768ad257fb3418e5abe41cc5ecc5322` |
| Re-entry seal | `1776ea8ca12ba54dc85e817484711663480569a2` |
| Blocked implementation seal | `d79617727dcdcdbc0ed659c1c69dc0e175787806` |

The R1-06 owner exception remains recorded and limited; successor seals add only
`manifest.json`.

## 3. Owner authorization

The owner selected option 1 and expressly authorized the incompatible persisted
identity transition:

- implement deterministic semantic reference resolution;
- replace path-dependent reference identity with content-bound identity;
- introduce explicit successor versions for affected identity domains;
- implement persisted identity-generation detection and safe refusal;
- reconstruct only by re-ingestion into a new store;
- preserve original historical stores;
- complete cross-host/cross-path verification and the C4 oracle;
- run regression, coverage, package smoke and hosted campaigns.

Restrictions retained: no dependency upgrade, no in-place migration, no
destructive rebuild, no merge, no release tag, no package publication, no DF-1 or
S6.4 closure, and no independent qualification claim.

## 4. Predecessor reproduction (for contrast)

| Host / recipe | Predecessor target |
| --- | --- |
| xUnit host, production | `asmctx3:sha256:1844c0…e69e` |
| Console host, production | `asmctx3:sha256:f9728a…0f85` |
| Independent oracle, runtime refs, Release | `asmctx3:sha256:4f4afa…eb29` |
| Independent oracle, runtime refs, Debug | `asmctx3:sha256:4454f0…91e7` |

Those values are historical. They are not accepted as successor authority.
