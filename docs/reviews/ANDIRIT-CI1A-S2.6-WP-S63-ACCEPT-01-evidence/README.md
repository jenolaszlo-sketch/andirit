# S6.3 owner acceptance evidence

This package is independent of the earlier producer and qualification seals. [Owner acceptance](../ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01.md) · [P-03 decision](../ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01-P03.md)

- `prepublication.txt` records the pre-push baseline, tag, exact qualified run, and committed-object verification of the original, producer and qualification seal lineage.
- `historical-q1.bundle` preserves the original Q1 commit and evidence from its divergent historical branch. The bundle requires the existing `dd484af` ancestor. A full clone can recover the exact Q1 object by running `git fetch docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01-evidence/historical-q1.bundle refs/qualification-baselines/s63-q1:refs/qualification-baselines/s63-q1` before verifying the independent requalification seal.
- `seal.py` builds or verifies `manifest.json` using committed blob bytes. The seal covers the owner decisions, project-state updates and new acceptance evidence. It includes the exact implementation and earlier report/seal identities. Its single-file seal commit follows the payload.
- A separate operational receipt records the actual fast-forward and subsequent hosted CI against the published acceptance SHA. Keep it separate from the published seal to avoid a circular digest and another documentation-only CI revision.

Run `python docs/reviews/ANDIRIT-CI1A-S2.6-WP-S63-ACCEPT-01-evidence/seal.py verify <acceptance-seal>` from the repository root. A normal release tag and package version remain outside the acceptance delta.
