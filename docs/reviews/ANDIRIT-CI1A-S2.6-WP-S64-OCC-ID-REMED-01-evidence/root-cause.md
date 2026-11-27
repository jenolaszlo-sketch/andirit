# Occurrence identity divergence — root cause

## Reproduction

Same C4 source, same production plugin, same 51 facts and two contributors:

| Host / recipe | Target identity |
| --- | --- |
| Independent oracle, `compctx1`, runtime refs, Release | `asmctx3:sha256:4f4afa…eb29` |
| Independent oracle, `compctx1`, runtime refs, Debug | `asmctx3:sha256:4454f0…91e7` |
| Production plugin, console host current | `asmctx3:sha256:f9728a…0f85` |
| Production plugin, xUnit host current | `asmctx3:sha256:1844c0…e69e` |
| Production plugin, earlier test host run | `asmctx3:sha256:b3cb5e…0ddb` |

`production-descriptor.txt` shows the verified production descriptor is
`compctx2:sha256:651e7bac…` with references such as:

```text
file|C:/Program Files/dotnet/shared/Microsoft.NETCore.App/10.0.12/System.Runtime.dll|76be…
file|C:/Windows/Temp/opencode/.../testhost.dll|92cb…
file|C:/Windows/Temp/opencode/.../xunit.core.dll|2f7f…
```

The oracle uses the frozen S2.0 runtime reference set. Production uses the
process trust-platform assembly set.

## Contract history

| Contract | Role | Status |
| --- | --- | --- |
| `compctx1` | R2-faithful compilation context; target/usage input | Accepted S2.0/S2.1 |
| `compctx2` | Dependency-sensitive provenance validity; project references as `compilation-context|<dep compctx2>` | **Explicitly authorized** by the S2.1 project-reference amendment |
| `asmctx3` | Semantic target identity; must be minted from defining `compctx1` only; `compctx2` never enters it | Accepted S2.2 Option A |
| `usage4` | Occurrence identity; must not contain `compctx2` | Accepted S2.2 |

Code:

- `VerifiedCSharpCompilation.Create` builds a `compctx2` descriptor.
- `LegacyDescriptor` converts a dependency-free `compctx2` to `compctx1` by
  changing the contract version and recomputing the digest; **reference items are
  retained unchanged**.
- `VerifiedCSharpAnalysis.MintTarget` uses `LegacyDescriptor` for target identity.
- `VerifiedMetadataReference.Capture` uses identity
  `file|<absolute full path>|<sha256>`.
- `CSharpCodeGraphPlugin.BuildPlatformReferenceEvidence` uses the process
  `TRUSTED_PLATFORM_ASSEMBLIES` (all of them).

## Classification

| Candidate cause | Finding |
| --- | --- |
| Fixture construction | The oracle builds a runtime-reference compilation per the S2.0 spec; production builds a process-TPA compilation. Both are internally consistent, but the semantic input sets differ by incidental references |
| Compilation reference resolution | **Primary cause**: production uses ambient host TPA (including testhost/xunit in tests) |
| Ambient host assembly discovery | **Primary cause**: host-only assemblies enter the descriptor |
| Descriptor construction | `compctx2` construction is correct and authorized |
| Descriptor canonicalization | **Primary cause**: `LegacyDescriptor` retains path-bound reference identities; absolute paths and host refs survive the compctx1 mapping |
| Target generation | Correct given the descriptor; `ComputeTargetId` rejects `compctx2` and uses the mapped `compctx1` |
| Documented contract inconsistency | **Present**: S2.1 authorizes `file|path|sha256` reference identity; S2.0/F12 requires host-independent, content-bound runtime identities. Both are accepted in their own scopes |

## Conclusion

This is not a `compctx2` defect and not a reason to revert to `compctx1`. It is
an inconsistency between:

1. the S2.1 production reference identity (absolute path + process TPA), and
2. the S2.0/frozen oracle contract (content-bound framework references).

Equivalent semantic inputs therefore do not produce equivalent identities across
hosts. The independent oracle is correct for its declared contract; production
is correct for S2.1. A bounded correction must change the reference-resolution /
identity boundary, which is an incompatible identity transition for persisted
occurrences and published packages. It cannot be applied without a separate owner
decision.

## Negative controls

- Same source and same compiler options but different reference installation
  paths change the production `file|path|sha256` item and therefore the identity
  by construction.
- Genuinely different semantic references must continue to change identity; the
  proposed fix must not exclude binding-relevant references or aliases.
- The 51-fact cardinality and two-contributor union are stable across hosts; only
  target/usage identities diverge. This isolates the defect to identity inputs,
  not occurrence extraction or contributor union.
