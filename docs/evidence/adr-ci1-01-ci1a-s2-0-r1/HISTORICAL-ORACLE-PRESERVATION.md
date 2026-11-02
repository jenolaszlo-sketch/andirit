# Historical oracle preservation

The original Q1 expected result remains at `docs/planning/adr-ci1-01-ci1a-s2-0/oracle/expected/oracle-expected.json` and is unchanged from the pinned Q1 source. R1 uses the separate `oracle-expected-r1.json` and explicitly identifies itself as `CI1A-S2.0-CORPUS-R1`.

Q1's source-binding package records the historical baseline as commit `82f101c24e21828a41a30d4342ba73db9376e24e`, tree `75faf3eb0fcdab8ad78142617d1d7ee3dfce95c0`; the historical oracle was independently reproduced there in the review archive. R1 reproduction commands and actual new runs are in `REGRESSION-RESULTS.md`. Historical evidence is not a runtime compatibility promise.
