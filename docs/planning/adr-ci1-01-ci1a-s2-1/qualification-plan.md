# S2.1 qualification plan

**Status:** Producer implementation is committed and final regression passed 645/645. Independent Q1 qualification and owner acceptance remain pending.

## Canonical evidence package

The producer packet is under `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/`. It contains the final and intermediate TRX, baseline records, exact commands/environment/output, source/tree and project fixture observations, reference probes, and independent tuple-check material. Producer test results are not independent qualification.

Bind review to Andirit implementation commit `c02d5eae157653ba7f123b98aceccb01a0b542a0`, tree `a53a076dbb482062973f70fd11bf4261cc5368f3`, and its accepted authority predecessor. The final regression environment is .NET SDK 10.0.401, .NET 10.0.12, Roslyn 5.9.0, win-x64; all four TRX counters sum to 645 passed, 0 failed, 0 skipped.

## Independent reviewer sequence

1. Verify the final baseline-to-implementation source/tree identity and reproduce the full regression commands and counters from the raw packet.
2. Recompute frozen R2 compctx1 vectors and compctx2 serialization from the accepted owner contract. Check that source/options serialization, PE identity, and tuple order match authority.
3. Independently bind exact source bytes to BOM-aware decoded text and the actual Roslyn trees; recompute byte hashes and reproduce supported/refused encoding cases.
4. Inspect every R2 material option and parse setting from actual compilations. Verify all material changes alter context identity and the nonmaterial `ConcurrentBuild` switch does not.
5. Reopen captured PE bytes, verify Roslyn metadata is constructed from those bytes, and inspect exact platform/project reference instances and properties.
6. Reconstruct project dependencies bottom-up; verify dependency compctx2 propagation through direct and transitive edges, and refusal for missing dependencies, mismatched references, cycles, conflicts, and unsupported project inputs.
7. Recalculate `analysisscope1` and compctx2-only `compilation-universe1`; test omitted members, duplicates, conflicting identities, missing acquisition, and reused compilations.
8. Exercise stale/forged descriptor and legacy minting boundaries. Confirm no refusal becomes verified-empty analysis and no S2.1 path mints an occurrence.
9. Review both retained intermediate failure/correction sequences, then issue an independent report. Obtain the separate owner acceptance before representing S2.1 as accepted or moving to S2.2.

The exact commission IDs P01–P07 and N01–N10 map to named tests in the [acceptance matrix](acceptance-matrix.md). Implementation tests are in canonical `../../../tests/Andirit.CSharp.Tests/`; production seams are under `../../../src/Andirit.CSharp/`.

## Stop conditions

Stop qualification if frozen compctx1 outputs/vectors differ; compctx2 violates the authorized amendment; source/tree or PE image binding fails; a missing/cyclic/unverified dependency receives a strong context; a mixed-version universe is accepted; stale or forged proof is reused; refusal becomes complete-empty; D3/D4/D5 is weakened; or the work crosses into S2.2. No implementation change is authorized by this qualification plan.
