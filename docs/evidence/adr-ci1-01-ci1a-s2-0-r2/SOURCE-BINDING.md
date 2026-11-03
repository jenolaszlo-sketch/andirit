# R2 source binding

- Repository: `D:\projects\Andirit`; branch: `main`.
- Starting commit/tree: `9fc2115622c93fa54f5f7882b117185a017703bb` / `547af9c3c7d37132b93944cf8b3dbf284872450f` (the R1 final commit recorded in the R1 post-commit handoff).
- Ancestry verified: `82f101c24e21828a41a30d4342ba73db9376e24e` (Q1 pin) is an ancestor of the R1 pin; the owner baseline `b7fea0d219409b279b78317e32bdc3c8b5ae61ee` is an ancestor of `main` HEAD.
- Q2 review evidence: `adr-ci1-01-ci1a-s2-0-corpus-01-q2` package (108 files), manifest SHA-256 `2858F9FD20473CC7973DA006235D2566E30DF7C5E86EC8A646EA4E226979B1F4`, verified with its `verify-manifest.py` before any edit. Findings source: `NEW-FINDINGS.md` (Q2-F01 Blocking, Q2-F02 Significant) and `evidence/Q2-P19-stale-E02.patch`.
- R2 source changes are limited to `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/` and `docs/evidence/adr-ci1-01-ci1a-s2-0-r2/`. Changed/new oracle files: `Program.cs`, `R2Identity.cs`, `SemanticInspector.cs` (modified); `CanonicalTarget.cs`, `FrozenExpectations.cs`, `ProvenanceGate.cs` (new); `expected/oracle-expected-r2.json` (new frozen artifact).
- The original `oracle-expected.json` (SHA-256 `D5F507DDF2220DF556702982E3BE1F86CDE698195AEA66872F488E96EDE5C0D7`) and `oracle-expected-r1.json` (SHA-256 `1A53633EEF9517128EDFE90E8E715C513056488E430860462AB76BF0DB3AD085`) are byte-identical to their pinned sources; both were byte-compared against clean `git archive` extractions of their pins during this commission (see `HISTORICAL-PRESERVATION.md`).
- Accepted R2 contract preserved: `asmctx3` derives from the defining `compctx1`; `compctx2` carries dependency-sensitive validity; `usage4` is frozen; no `asmctx4`; no S2.2 production extraction; no production code change.
- Final commit/tree and clean Git state are recorded in the post-commit handoff outside committed content.
- No fetch or push is part of this commission.
