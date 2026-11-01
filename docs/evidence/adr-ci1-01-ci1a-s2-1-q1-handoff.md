# ADR-CI1-01-CI1A-IMPL-01-S2.1-Q1 handoff

**Current state:** implementation is committed and all 645 producer regression tests passed. Independent Q1 qualification and final owner acceptance are still open. This is a handoff for review, not an S2.1 acceptance claim.

## Reviewer packet

- Original authorization and narrow owner-approved project-reference amendment: canonical Andirit records `docs/decisions/adr-ci1-01-ci1a-s2-1-authorization-boundary.md` and `docs/decisions/adr-ci1-01-ci1a-s2-1-project-reference-amendment.md`.
- Contract, design and exact commission traceability: `docs/planning/adr-ci1-01-ci1a-s2-1/` including `acceptance-matrix.md`.
- Implementation commit `c02d5eae157653ba7f123b98aceccb01a0b542a0`, tree `a53a076dbb482062973f70fd11bf4261cc5368f3`; owner amendment predecessor `bd8e6331da9023cebb211f764d81b978a96650dc`, tree `d6a8fbd36d1c5a20a228ab6d86710025b81d6ba4`.
- Source and test changes are in `src/Andirit.CSharp/`, `tests/Andirit.CSharp.Tests/`, and the minimal internal source-handoff changes in `Andirit.Abstractions` and core `CodeIndexingLifecycle`. Raw commands, final and intermediate TRX files, and captured probe/build stdout/stderr are under `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/`.
- Producer evidence: [implementation](adr-ci1-01-ci1a-s2-1-implementation.md), [provenance](adr-ci1-01-ci1a-s2-1-provenance.md), and [regression history](adr-ci1-01-ci1a-s2-1-regression.md).

## Independent Q1 work still required

1. Reproduce the 645-test final result and verify exact command, environment, commit/tree, stdout/stderr, and all four TRX counters from the raw packet.
2. Independently recompute frozen R2 compctx1 vectors and compctx2 digests from accepted authority; verify exact bytes-to-tree association, actual options, captured PE image/reference association, and verified project dependency DAG.
3. Check `analysisscope1` completeness relative only to declared scope and independently recalculate `compilation-universe1` with compctx2-only members.
4. Independently execute the commission negatives: changed bytes, hash disagreement, Roslyn text mismatch, option/reference changes, missing defining compilation, incomplete scope, conflicting project identities, stale reuse, and forged descriptors. Confirm failure never becomes successful verified-empty analysis.
5. Review both preserved intermediate failure/correction paths, verify no production changes followed the final passing run, and record reviewer findings.
6. Obtain separately required owner acceptance; do not infer it from the amendment or passing producer tests.

## Boundaries

The amendment preserves compctx1 and authorizes verified compctx2 dependency contexts. It does not authorize occurrence extraction or a compatibility rewrite of `asmctx3`/`usage4`; S2.2 must separately reconcile consumer compatibility. Unsupported project inputs may refuse strong provenance while the existing graph continues without a verified-completeness claim. The evidence does not qualify Linux behavior, power-loss behavior, a comprehensive semantic corpus, complete MSBuild/NuGet/generator integration, or repository-scale performance.

S-01 remains open through later semantic provenance qualification. NEW-01 remains due before S2.3 and before any earlier recovery reliance. NEW-04 remains due during S2.2/before S2.2 acceptance, and before any earlier use of the defective merge as authority; this implementation does not use it. F12 requires the separately commissioned S2.0 frozen independent semantic oracle before S2.2 qualification-test authoring.

**Disposition:** ready for independent Q1 review; not ready for an S2.1 acceptance claim until that review and owner disposition are recorded.
