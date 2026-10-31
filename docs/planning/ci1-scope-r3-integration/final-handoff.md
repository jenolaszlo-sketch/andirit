# Final handoff — CI-1-SCOPE-OWNER-01

**Disposition: `CI-1-SCOPE-OWNER-01 ACCEPTED AND INTEGRATED LOCALLY`.**

Owner acceptance recorded; the exact Q4-qualified R3 artifacts integrated;
acceptance evidence reproduced byte-identically from the canonical repository.
**Not pushed** — publication requires separate owner authorization.

## A. Git identities

| Role | Commit | Tree |
| --- | --- | --- |
| Starting published HEAD | `d02baaa8e7e8704f2f5dac9ab549779f45fa5cb6` | `406282797948c035b3dda4f70b9ec5c5b536a6d7` |
| Integration / acceptance commit | recorded by the follow-up bookkeeping commit (this file cannot contain its own commit's hash) | — |
| Bookkeeping commit | — | — |
| Final local `main` | — | — |

R3 frozen qualification baseline: `5c89ac0b3d0e87c0112880cf985826ded42b21b2`
(tree `e61994289e5f96203ded37be000b0ed3bb3b79a2`).

## B. Owner decision

- `CI-1-SCOPE-R3: ACCEPT WITH QUALIFICATIONS` — record:
  [`docs/decisions/ci1-scope-owner-01-acceptance.md`](../../decisions/ci1-scope-owner-01-acceptance.md).
- `CI-1-SCOPE-Q4: QUALIFICATION COMPLETE` (independent reviewer; disposition
  `SUPPORTED WITH QUALIFICATIONS`).
- `CI-1-SCOPE-OWNER-01: OWNER ACCEPTANCE RECORDED`.

## C. Integration inventory

### Added (7)

| Path | SHA-256 |
| --- | --- |
| `docs/planning/ci1-scope-r2/analysis/oracle-r3/OracleR3.csproj` | `0859b07a…` |
| `docs/planning/ci1-scope-r2/analysis/oracle-r3/Contracts.cs` | `478770d7…` |
| `docs/planning/ci1-scope-r2/analysis/oracle-r3/Sem.cs` | `2cf259b8…` |
| `docs/planning/ci1-scope-r2/analysis/oracle-r3/Audit.cs` | `686ca1d7…` |
| `docs/planning/ci1-scope-r2/analysis/oracle-r3/Program.cs` | `323b410b…` |
| `docs/planning/ci1-scope-r2/analysis/identity-probe/IdentityProbeR3.csproj` | `35e4b333…` |
| `docs/planning/ci1-scope-r2/analysis/identity-probe/Program.cs` | `c3242fba…` |

Full hashes: [`integration-manifest.md`](integration-manifest.md).

### Modified (2, append-only)

- `docs/planning/ci1-scope-r2/oracle-contract.md` (+5,168 bytes; block
  `9f202c24…`; pristine prefix preserved byte-for-byte).
- `docs/planning/ci1-scope-r2/mutation-matrix.md` (+2,814 bytes; block
  `39d2350f…`; pristine prefix preserved byte-for-byte).

### Acceptance documentation (7)

- `docs/decisions/ci1-scope-owner-01-acceptance.md`
- `docs/planning/ci1-scope-r3-integration/README.md`
- `docs/planning/ci1-scope-r3-integration/integration-manifest.md`
- `docs/planning/ci1-scope-r3-integration/qualification-reconciliation.md`
- `docs/planning/ci1-scope-r3-integration/test-results.md`
- `docs/planning/ci1-scope-r3-integration/final-handoff.md`
- (this bookkeeping commit, when used)

## D. Reproduced acceptance evidence

| Check | Result |
| --- | --- |
| Oracle gate | `Pass=true`, exit 0, `0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f` (byte-identical to Q4 and R3) |
| Mutations | 198 executed; 196 intended rejections; 2 positive controls; 0 crashes; 0 unrelated-only; 0 reason mismatches |
| Confirmed-family sweep | 126/126 rejected |
| Lifecycle | exactly 5 `LIFECYCLE_NOT_QUALIFIED` (C14×2, C15×3) |
| Identity probe | `allRequirementsSatisfied=true`, exit 0 |
| Repository tests | 565/565 passed (27 + 157 + 312 + 69) |

## E. Change boundary

No changes to `src/**`, `tests/**`, `.github/**`, public query APIs, graph
schemas or codecs, package versions, build-wide configuration, existing
accepted historical contracts, C# provider-level target resolution or
partial-type documentation merging. The integrated files are planning/
qualification tooling and documentation only.

## F. Local vs remote

Local `main` contains the integration and acceptance commits on top of the
published baseline `d02baaa`; the remote remains at `d02baaa`. **Nothing was
pushed, tagged, released or published.**

## G. Publication approval required

Publication of the integration/acceptance commits (together with this
bookkeeping commit) requires explicit owner authorization. The next separately
authorized engineering milestone remains ADR-CI1-01 (minimal B2-versus-C
implementation experiment for occurrence storage), preserving existing
aggregate graph relationships.

## H. Preserved qualifications

Q4-F01 (equal assembly identity — bounded limitation/graduation condition),
Q4-F02 (unreadable fixture at the library boundary — documentation correction,
deferred robustness), Q4-F03 (two null hashes in R3's export ledger — recorded,
export independently verified 675/675), Q4-F04 (cross-project generic
expectations outside the frozen table — explicitly refused), Q4-F05 (provenance
recorded, not pinned), plus lifecycle, human-reviewed basis, single-Roslyn-family
independence, bounded corpus scope, unsupported capabilities and unqualified
candidate-result comparison. See
[`qualification-reconciliation.md`](qualification-reconciliation.md).
