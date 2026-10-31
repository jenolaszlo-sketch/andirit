# Integration manifest — exact inventory and verification

Starting point: published `main` at
`d02baaa8e7e8704f2f5dac9ab549779f45fa5cb6` (tree
`406282797948c035b3dda4f70b9ec5c5b536a6d7`).

Authorized sources: `ci1-scope-r3/integration/**`. Every source artifact was
verified against R3's `INTEGRATION-MANIFEST.md`, and against the Q4 candidate
copies (`ci1-scope-q4/probes/oracle-r3-under-test/**`,
`probes/identity-probe-r3/**`, and Q4's disposable patched export) before
copying. All hashes matched; no copy was selected by file age.

## A. Added files (7)

| # | Source (R3) | Destination (canonical) | Bytes | SHA-256 |
| --- | --- | --- | --- | --- |
| 1 | `integration/oracle-r3/OracleR3.csproj` | `docs/planning/ci1-scope-r2/analysis/oracle-r3/OracleR3.csproj` | 450 | `0859b07a8b7e3a079a6a379a1bc96a3646c31dfcfbef90721bc0c62d8404ddb8` |
| 2 | `integration/oracle-r3/Contracts.cs` | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Contracts.cs` | 17,949 | `478770d78c46912bff2b1ffa962aa3b5b89477c12ec885d996b137d2f6ee1d01` |
| 3 | `integration/oracle-r3/Sem.cs` | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Sem.cs` | 8,385 | `2cf259b8259f95a5ce2610442c31169df2736f2c3f16c79deab096cb8dcddd53` |
| 4 | `integration/oracle-r3/Audit.cs` | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Audit.cs` | 27,880 | `686ca1d76d4dfaa085715ffdf3d26b0d2d8d6a3f55093497d2da484e178780ef` |
| 5 | `integration/oracle-r3/Program.cs` | `docs/planning/ci1-scope-r2/analysis/oracle-r3/Program.cs` | 47,077 | `323b410bf4909e80b9536adb1a3da6818563c24f372db55cfdb70bf5f68c130c` |
| 6 | `integration/identity-probe/IdentityProbeR3.csproj` | `docs/planning/ci1-scope-r2/analysis/identity-probe/IdentityProbeR3.csproj` | 464 | `35e4b3335cae8c9ad67180e672224e9e2b6c817e5e56d0595e5f70ef93088b6a` |
| 7 | `integration/identity-probe/Program.cs` | `docs/planning/ci1-scope-r2/analysis/identity-probe/Program.cs` | 10,830 | `c3242fbad8fce9d135c16880c653f3c0211dfcd53aa861d2d8c5b805b8d00afc` |

Post-copy verification in the canonical tree: all seven byte counts and SHA-256
values match the table above.

## B. Append-only amendments (2)

| # | Destination | Block source | Appended bytes | Block SHA-256 |
| --- | --- | --- | --- | --- |
| 8 | `docs/planning/ci1-scope-r2/oracle-contract.md` | `integration/oracle-contract-r3-amendment.md` | +5,168 | `9f202c24cc45e4cc1263c7e7c535306b16c42528d07bc874c58acc346c58eb1c` |
| 9 | `docs/planning/ci1-scope-r2/mutation-matrix.md` | `integration/mutation-matrix-r3-amendment.md` | +2,814 | `39d2350fb7562747f3710c52fc0f9c5a5bfa40c28ad964ea5f6cecb55977d9fb` |

Verification performed on the canonical files:

- the pre-existing canonical bytes are a byte-for-byte prefix of the integrated
  file (checked before/after);
- the appended byte counts are exactly 5,168 and 2,814;
- the appended blocks hash to the recorded values;
- content is identical, modulo line endings, to Q4's independently patched
  disposable export (canonical working tree is LF: 8,807 → 13,975 bytes and
  5,414 → 8,228 bytes; Q4's `git archive` export was CRLF: 8,967 → 14,135 and
  5,506 → 8,320 — the differences are exactly the prefix CRLF bytes).

Historical R1/R2 text is otherwise untouched: the R2 contract and mutation
matrix retain all original sections; the amendments are appended at end of
file.

## C. Verified absent before integration

`docs/planning/ci1-scope-r2/analysis/oracle-r3/` and
`.../analysis/identity-probe/` did not exist in the published tree, and none of
the seven files existed anywhere else. No destination collision occurred.

## D. Deliberately not changed

| Path | State |
| --- | --- |
| `docs/planning/ci1-scope-r2/analysis/oracle/**` (R2 oracle) | byte-identical (historical evidence) |
| `docs/planning/ci1-scope-r2/analysis/before/**` | byte-identical |
| `docs/planning/ci1-scope-r2/analysis/fixtures/**` | byte-identical |
| `docs/planning/ci1-scope-01/fixtures/expected-sites.json` (32 rows) | byte-identical |
| 15 declared outcome strings | unchanged |
| Q1/Q2/Q3 evidence | unchanged |
| `src/**`, `tests/**`, `.github/**`, public APIs, schemas, codecs, package versions, build config | unchanged |

## E. Reproduction (from the integrated repository)

```powershell
dotnet build -c Release --artifacts-path <external> docs/planning/ci1-scope-r2/analysis/oracle-r3/OracleR3.csproj
dotnet run --no-build -c Release --artifacts-path <external> `
  --project docs/planning/ci1-scope-r2/analysis/oracle-r3/OracleR3.csproj `
  -- <repo-root> <output.json>
dotnet run -c Release --artifacts-path <external> `
  --project docs/planning/ci1-scope-r2/analysis/identity-probe/IdentityProbeR3.csproj `
  -- <repo-root>/docs/planning/ci1-scope-r2/analysis/fixtures
```

Expected: the R3 gate line with `Pass=true`, exit 0, and output SHA-256
`0b3018507679b022f3638615ba9b43b7e11315f9ee01a4fe347c8cdfbbbc979f`
(byte-identical; observed). Build artifacts were directed outside the
repository; no generated binaries are committed.
