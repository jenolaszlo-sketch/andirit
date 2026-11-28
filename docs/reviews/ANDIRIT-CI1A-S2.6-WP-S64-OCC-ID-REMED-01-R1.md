# ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1

**Occurrence identity transition · 28 November 2026**

## A — VERSIONED IDENTITY TRANSITION IMPLEMENTED — READY FOR INDEPENDENT QUALIFICATION

The owner-authorized incompatible identity transition is implemented. Production
compilation references now come from a deterministic managed runtime closure and
carry content-bound `refctx1` identities. Target and occurrence identities use
explicit successor domains (`asmctx4`/`usage5`) minted from the successor
defining context (`compctx4`). Persisted predecessor records are detected and
refused with re-ingestion guidance; historical stores are preserved and
reconstruction happens only by re-ingestion into a new store. The independently
derived C4 successor ledger is frozen and the local, Windows console/xUnit,
cross-path and hosted Windows/Linux/macOS campaigns are completed.

DF-1 and S6.4 remain open. No independent qualification, merge, release or
publication is claimed.

## 1. Baseline and authorization

| Subject | Identity |
| --- | --- |
| Investigation payload | `b03edfeb13073d2806f085ce3c9f5a1a92be2a53` |
| Investigation seal | `81102d21b4d6f6ed510b0555bb0d034aa1705c44` |
| Seal tree | `81bdedc2936b1bc62f57476038807bff116f102a` |
| Seal SHA-256 | `b7a93c56809a15f5c3d39a3d3c6680bcbb1eecbe0c6ddef2c5a2a9cd9149f0dd` |

The owner selected option 1 and authorized the breaking persisted-identity
transition. The investigation seal is the accepted baseline and is unmodified.
Predecessor seals (`870b600…`, `b3951c7…`, `1776ea8…`, `d796177…`) are unchanged.
The R1-06 exception remains limited; this successor seal adds only
`manifest.json`.

| Candidate | Identity |
| --- | --- |
| Branch | `remediation/s64-occ-id-01-r1` |
| Implementation commit | `77ec887368c62b404e0d9a8034d34b7f8cac8007` |
| Draft pull request | #9 (do not merge) |

## 2. Successor contract

| Domain | Successor | Predecessor |
| --- | --- | --- |
| Reference identity | `refctx1\|assembly\|version\|culture\|pkt\|sha256:<image>` | `file\|absolute path\|sha256:<image>` |
| Production descriptor | `compctx3` | `compctx2` |
| Defining context | `compctx4` | `compctx1` |
| Target | `asmctx4:sha256:<hash>` | `asmctx3:sha256:<hash>` |
| Occurrence | `usage5` | `usage4` |
| Persisted generation | 2 | 1 |

Framework references are the managed assemblies of the running
`Microsoft.NETCore.App` shared framework directory, ordered ordinally. Process
TPA, testhost/xunit/consumer/application assemblies and absolute installation
paths no longer participate in identity. Project references remain live
compilation references; package/external references remain refused as before.

Exact digests are bound to the referenced runtime file contents (S2.0 §5). The
semantic contract, domains, cardinality and contributor union are portable.

Full detail: `transition-contract.md`.

## 3. Implementation

- Content-bound capture in `VerifiedMetadataReference`.
- `compctx3` production descriptor and `compctx4` R2-faithful mapping.
- `asmctx4` target and `usage5` occurrence construction.
- Occurrence schema 2 / namespace 2 / meta generation 2.
- Authority version 2 / identity generation 2.
- Predecessor generation and predecessor context refusal with explicit
  re-ingestion guidance.
- No migration code path; reconstruction is ordinary ingestion into a new store.
- Successor descriptors reject relabeled predecessor path-bound reference items.

Full detail: `implementation.md`.

## 4. C4 successor oracle

The independent oracle is a separate tool that consumes live Roslyn inputs and
implements `refctx1`/`compctx4`/`asmctx4`/`usage5` from the documented contract.
It never calls production identity helpers.

| Field | Value |
| --- | --- |
| Target | `asmctx4:sha256:8258e165d53336f87abf3b1d1ae3f220190dfaf848eaae2906222bf49940555f` |
| Defining context | `compctx4:sha256:476de9e8c407f13756a931f8ca285ce7d254c19309be433cdf84d022165e31be` |
| Production descriptor | `compctx3:sha256:f85c46768aa2a3c1c8dbda35a3e2e8f699ceea04ddf7a17d7ec590865ce313ee` |
| Cardinality | 51 (50 Caller, 1 Caller2) |
| Contributors | 2 |
| Variants | 51 / 1 / 50 / 51 |

Artifacts: `expected/` and `c4-oracle.md`.

## 5. Cross-host and cross-path

| Observation | Target | Cardinality |
| --- | --- | ---: |
| Windows x64 xUnit host | `asmctx4:sha256:8258e1…555f` | 51 |
| Windows x64 console host | `asmctx4:sha256:8258e1…555f` | 51 |
| Two physical corpus roots | identical | 51 |
| Framework closure | 172 managed runtime refs, no host/test/app/path items | — |

The predecessor console/xUnit divergence is eliminated. Hosted run: `https://github.com/jenolaszlo-sketch/andirit/actions/runs/46132247172` on candidate `77ec887368c62b404e0d9a8034d34b7f8cac8007`; Windows, Linux, macOS and pack jobs all succeeded. Full detail:
`cross-host.md` and `evidence/host-probe-console.json`.

## 6. Persisted generation and reconstruction

A predecessor schema/namespace/generation or authority envelope is refused with
an explicit re-ingestion requirement. The refusal does not rewrite the logical
predecessor rows. Re-ingestion into a new store reproduces the successor target
and all 51 facts. Historical stores and packages are not claimed compatible.

Full detail: `compatibility.md`.

## 7. Regression, coverage and packaging

| Campaign | Result |
| --- | --- |
| Full local Release suite | 1,044 passed, 0 failed |
| Andirit + Abstractions coverage | 82.29% line / 70.75% branch (threshold 55%) |
| Andirit.CSharp coverage | 94.90% line / 87.66% branch (threshold 75%) |
| Andirit.LatticeDb coverage | 93.51% line / 80.85% branch (threshold 70%) |
| Package smoke | Pass: six packages, packed extraction, durable store, Testing laws |
| Hosted Windows x64 | Success (run 46132247172, job 140939754018) |
| Hosted Linux x64 | Success (run 46132247172, job 140939753793) |
| Hosted macOS x64 (regression only) | Success (run 46132247172, job 140939754002) |

Artifacts: `evidence/local-release-tests.log`,
`evidence/local-regression-summary.txt`,
`evidence/local-csharp-coverage.log`,
`evidence/local-package-smoke.log`.

## 8. Findings and gates

OCC-ID-01…05 and R1-01 are resolved. R1-06 remains the recorded owner exception.
F-S64E-03/04/05, DF-1 and S6.4 remain open. See `findings-gates.md`.

## 9. Delimitation

- No dependency upgrade.
- No LatticeDbSharp graph API adoption or Bolt integration.
- No current-query optimization or architectural expansion.
- No historical seal modification.
- No in-place migration or destructive rebuild.
- No main merge, release tag or package publication.
- No DF-1 or S6.4 closure.
- No independent qualification granted.

## 10. Required successor

Commission **`ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-QUAL-01`** for independent
qualification of the versioned identity transition, including independent
reproduction of the C4 ledger, cross-host/cross-path verification and the
compatibility/re-ingestion boundary.

