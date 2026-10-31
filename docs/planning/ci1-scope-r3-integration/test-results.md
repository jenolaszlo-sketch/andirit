# Test results — integrated oracle and repository regression

Environment: Windows (win32), .NET SDK `10.0.401`, Roslyn
`Microsoft.CodeAnalysis.CSharp` 5.9.0, `CSharp14`, offline NuGet.

## 1. Integrated oracle — mandatory R3 gate

Built **from the canonical integrated repository**
(`docs/planning/ci1-scope-r2/analysis/oracle-r3/`) with build artifacts directed
outside the checkout, then executed against the canonical repository root:

```
{"Sites":32,"Failures":0,"GlobalStructureErrors":0,"Mutations":198,"Rejected":196,"AcceptedControls":2,"PositiveControlAccepted":true,"Q2NamedRejected":true,"Collision":true,"ABIdentityRejected":true,"LifecycleQualifications":5,"Pass":true}
```

Exit code 0. Output SHA-256:
`0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` —
**byte-identical** to Q4's independent reproduction and to R3's recorded
evidence. No environmental metadata difference occurred.

Independent inspection of the integrated run's mutation array:

| Check | Result |
| --- | --- |
| Mutation cases executed | 198 (tier A 12, tier B 38, tier C 148) |
| Rejected for intended reason | 196 (every mutation's reason codes contain its declared expected reason; 0 mismatches) |
| Positive controls accepted | 2 (`P01`, `R14`) |
| Crashes | 0 |
| Unrelated-only rejections | 0 |
| Confirmed-family sweep | 126/126 rejected |
| Baseline corpus | 32 sites, 0 failures, 0 global structure errors |
| Class distribution | confirmed 21 / external 2 / ambiguous 1 / unresolved 1 / unsupported 7 |
| A/B and collision scenarios | `requirementSatisfied=true` both |
| Owner channel | `ownerChannelDistinguishes=true` (5 distinct lexical owners over the 6 same-doc-ID rows) |
| Lifecycle qualifications | exactly 5 (C14×2, C15×3), gate requires the count |

## 2. Integrated identity probe

```
allRequirementsSatisfied = true
```

Scenarios: S1 two-source-built-assembly exact identity (Q3-F02), S2 F01
metadata collision, S3 lexical-owner distinction, S4 cross-project reference
edge. Exit 0. Output SHA-256:
`01a0464c7ea2fe1ce16da6adbb06994c08ce9443b4ba41ba6e6fa7f584c62ce8`.

## 3. Repository regression

Full solution suite run from the integrated tree:

| Assembly | Passed | Failed | Skipped |
| --- | --- | --- | --- |
| `Andirit.LatticeDb.Tests` | 27 | 0 | 0 |
| `Andirit.TreeSitter.Tests` | 157 | 0 | 0 |
| `Andirit.Tests` | 312 | 0 | 0 |
| `Andirit.CSharp.Tests` | 69 | 0 | 0 |
| **Total** | **565** | **0** | **0** |

This matches the published C# correctness baseline (565/565) and confirms that
the integration introduces no production or test regression.

## 4. Cross-platform and CI status (explicit)

- The R3 oracle and identity probe are **planning/qualification projects** and
  are **not part of `Andirit.slnx`** (11 solution projects) and **not
  referenced by `.github/workflows/ci.yml`**. The existing CI workflow does not
  build or execute them.
- Therefore **no multi-platform (Windows/Ubuntu/macOS) execution of the
  planning oracle is claimed** by this milestone. The hosted CI results for the
  published production baseline (565 tests, pack + three OS jobs) remain the
  cross-platform evidence for production code, not for the planning oracle.
- The integrated oracle was executed on Windows only in this milestone, as
  recorded.

## 5. Reproduction integrity

The byte-identical output means the mandatory comparison (full SHA-256
`0b301850…`) passed exactly. No oracle change was made to force hash equality;
the integrated sources are byte-identical to the Q4-qualified artifacts
(`integration-manifest.md` §A).

## 6. Evidence locations

| Artifact | Path | SHA-256 |
| --- | --- | --- |
| Integrated oracle run | `C:\Users\Laszlos\source\repos\Solo\ci1-scope-owner-01\evidence\r3-run-integrated.json` | `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` |
| Integrated identity probe | `C:\Users\Laszlos\source\repos\Solo\ci1-scope-owner-01\evidence\identity-probe-integrated.json` | `01a0464c7ea2fe1ce16da6adbb06994c08ce9443b4ba41ba6e6fa7f584c62ce8` |

These are owner-milestone reproductions from the canonical tree; the original
R3 and Q4 evidence remains preserved unchanged in their own workspaces.
