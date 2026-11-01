# S2.1 regression record

**Evidence class:** producer regression results. Final tests passed; independent Q1 qualification remains outstanding.

## Baseline and final results

The clean pre-change baseline was Andirit commit `acd7ecd22516851ac57169fe19d184f6b10ac639`, tree `75ebd29380d6331accdc9e5838440d921a4280f9`: 594 passed, 0 failed, 0 skipped. Assembly counts were core 325, native 43, CSharp 69, TreeSitter 157. Baseline and final raw TRX files are retained under `docs/evidence/adr-ci1-01-ci1a-s2-1-raw/`.

The first post-change full run passed 643. Final regression passed **645 passed, 0 failed, 0 skipped**, split across four TRX results:

| Assembly | Tests | Result file |
| --- | ---: | --- |
| TreeSitter | 157 | [`final_net10.0_20261102010728.trx`](adr-ci1-01-ci1a-s2-1-raw/final_net10.0_20261102010728.trx) |
| Native | 43 | [`final_net10.0_20261102010729.trx`](adr-ci1-01-ci1a-s2-1-raw/final_net10.0_20261102010729.trx) |
| Core | 325 | [`final_net10.0_20261102010733.trx`](adr-ci1-01-ci1a-s2-1-raw/final_net10.0_20261102010733.trx) |
| CSharp | 120 | [`final_net10.0_20261102010739.trx`](adr-ci1-01-ci1a-s2-1-raw/final_net10.0_20261102010739.trx) |

The 51-test increase from baseline includes S2.1 coverage and boundary tests. Environment: .NET SDK 10.0.401, .NET 10.0.12, Roslyn 5.9.0, win-x64. The producer recorded a build with 0 warnings and 0 errors. No production change followed final regression; only trailing blank-line cleanup in a test file occurred before implementation commit `c02d5eae157653ba7f123b98aceccb01a0b542a0` (tree `a53a076dbb482062973f70fd11bf4261cc5368f3`).

## Preserved intermediate failures and corrections

1. The first plugin integration run failed `CSharpCodeGraphPluginTests.ExtractAsync_RealAndiritCSharpPluginPartialDeclarationsExtract`: expected 6, observed 7 after provenance preparation changed which source set entered compilation. The producer moved preparation into the established `Compilation.cs` lifecycle; it did not change the test expectation. Preserve the original failing TRX in the raw evidence packet.
2. The PE-caller extension initially failed with `ReferenceMembershipMismatch` in runs ending `010125`, `010221`, and `010234`. Investigation found the test declared an incomplete metadata inventory while its actual Roslyn compilation also contained the platform reference. The test inventory was corrected to reflect the compilation's actual references. The focused 48-case run ending `010253` passed. Keep all failed and corrected TRX results to show the diagnostic path.

Legacy identity regression coverage remained in the full run, including `Andirit.Tests.OccurrenceIdentityRemediationTests.Target_UsesTheAcceptedAsmctx3ComponentsAndCanonicalFormat`, `Usage4_MatchesTheFrozenR2Vector`, `Usage4_IncludesSemanticUnitAndSourceHash`, and `Usage4_DistinguishesDefiningCompilationContexts`. These checks do not authorize compctx2 use by `asmctx3`/`usage4`; S2.2 still needs separate compatibility review.

## Scope and qualification limits

All 645 producer regression tests passed, but this does not replace independent Q1. No Linux-host run, power-loss/filesystem mutation campaign, comprehensive semantic corpus, full MSBuild/NuGet/generator integration qualification, or repository-scale performance study is claimed. Unsupported project inputs can refuse strong provenance while legacy graph execution continues without a verified-completeness claim. The precise changed-file list and exact commands are retained in the canonical raw evidence package; no unstored command is reconstructed here.
