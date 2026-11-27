# Cross-host identity campaign

## Executed observations

Same C4 source, same production plugin, same 51 facts and two contributors:

| Host / recipe | Target | Cardinality |
| --- | --- | ---: |
| xUnit test host, production plugin | `asmctx3:sha256:1844c0…e69e` | 51 |
| Console host, production plugin | `asmctx3:sha256:f9728a…0f85` | 51 |
| Earlier test-host run | `asmctx3:sha256:b3cb5e…0ddb` | 51 |
| Independent oracle, runtime refs, Release | `asmctx3:sha256:4f4afa…eb29` | 51 |
| Independent oracle, runtime refs, Debug | `asmctx3:sha256:4454f0…91e7` | 51 |

The console and xUnit runs use different process reference sets. The production
descriptor is `compctx2` and lists absolute framework paths plus host assemblies;
the oracle uses the frozen runtime reference set. Cardinality and contributor
union are stable; only identities diverge.

## Not executed

- Isolated consumer host with a separately curated reference set.
- Two working directories with identical source and references.
- Two reference installation paths with byte-identical assemblies.
- Genuine Linux x64 and macOS host comparison.
- Semantic-reference negative controls in the production host.

These are deferred because the required correction is an incompatible identity
transition and the commission stops before implementing it.

## Code-level negative control

`VerifiedMetadataReference.Capture` identity is
`file|<absolute path>|<sha256>`. Two byte-identical references installed at
different absolute paths therefore produce different descriptors and target
identities by construction. This is the control the proposed correction must
remove without losing semantic reference distinctions.

## Conclusion

The campaign demonstrates host dependence but does not establish a compliant
cross-host identity contract. Cross-host completion remains an outstanding
requirement.
