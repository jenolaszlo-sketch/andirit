# C4 successor occurrence oracle

## 1. Independence

`tools/c4-successor-oracle` computes the expected ledger from live Roslyn
analysis inputs and the documented successor contract. It does not call
`CodeOccurrenceIdentity`, `MapR2FaithfulDefiningContext`, or any production
identity helper. Its formulas live in `SuccessorIdentityOracle`:

- `refctx1` reference identity from assembly metadata + image hash;
- `compctx4` defining context;
- `asmctx4` target;
- `usage5` occurrence.

It is linked into the test assembly only as an expected-value oracle and into the
standalone tool; production never references it.

## 2. Baseline ledger (Windows x64, .NET 10.0.12)

| Field | Value |
| --- | --- |
| Corpus | `c4-contributors` |
| Repository | `review:s64:d3-c4` |
| Target | `asmctx4:sha256:8258e165d53336f87abf3b1d1ae3f220190dfaf848eaae2906222bf49940555f` |
| Target document | `M:QualifierDependency.Api.Mark` |
| Defining context | `compctx4:sha256:476de9e8c407f13756a931f8ca285ce7d254c19309be433cdf84d022165e31be` |
| Production descriptor | `compctx3:sha256:f85c46768aa2a3c1c8dbda35a3e2e8f699ceea04ddf7a17d7ec590865ce313ee` |
| Metadata | `Dependency|0.0.0.0||` |
| Module | `Dependency.dll` |
| Cardinality | 51 |
| Distribution | 50 in Caller, 1 in Caller2 |
| Contributors | 2 |

## 3. Variants

| Variant | Cardinality | Contributors | Target |
| --- | ---: | ---: | --- |
| baseline | 51 | 2 | unchanged |
| without-caller | 1 | 1 | unchanged |
| without-caller2 | 50 | 1 | unchanged |
| caller2-changed | 51 | 2 | unchanged; caller usage identity changes |

Artifacts:

- `expected/c4-occurrences.json`
- `expected/c4-oracle-variants/c4-occurrences-without-caller.json`
- `expected/c4-oracle-variants/c4-occurrences-without-caller2.json`
- `expected/c4-oracle-variants/c4-occurrences-caller2-changed.json`

## 4. Production comparison

`S64D3C4SuccessorTests` publishes the real corpus through the production plugin,
recomputes every fact with the independent oracle, and queries the reopened
store.

On the producing runtime it asserts exact equality with the frozen ledger. On a
different runtime image it recomputes the host's expected target/identities and
asserts the same cardinality, distribution, contributor union and production
agreement. Exact digests remain bound to the referenced runtime file contents
(S2.0 §5); the semantic relations are portable.

## 5. Regeneration

```powershell
dotnet run --project docs/reviews/ANDIRIT-CI1A-S2.6-WP-S64-OCC-ID-REMED-01-R1-evidence/tools/c4-successor-oracle/C4SuccessorOracle.csproj -c Release -- <c4-corpus> baseline expected/c4-occurrences.json
```
