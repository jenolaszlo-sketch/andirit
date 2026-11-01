# ADR-CI1-01-CI1A-IMPL-01-S1 — Independent Review Handoff

**Reviewer commission (prepared, not performed):** `ADR-CI1-01-CI1A-IMPL-01-S1-Q1`.
This is a producer handoff. Producer test execution is **not** independent
qualification. Stage 2 is **not authorized**.

## 1. Source binding

| Item | Value |
|---|---|
| Canonical repository | `D:\projects\Andirit` |
| Starting HEAD | `fba53ceb28af0113979f512d251e73e18f211c0a` (tree `7bc88a67ce577a11f37d7259d4428cc23782553d`) |
| Commit A — contracts + in-memory parity | `01b74782751aede7274a1e4841b8a676a79fae7b` (tree `b98fb150a4a91884dea6f5bb2521995f7a1d4d19`) |
| Commit B — native persistence + lifecycle | `921900d258d8811a693141abb0dcb4137bcd901a` (tree `125c5e2c5a3c41451124c7fe4d7f467c2fe79d75`) |
| Commit C — this evidence/handoff | documentation-only child of Commit B (its own SHA is not embedded) |
| Branch | `main`; no new branch or worktree; nothing pushed |

## 2. Commands

```
dotnet build Andirit.slnx -c Debug
dotnet test tests/Andirit.Tests/Andirit.Tests.csproj -c Debug
dotnet test tests/Andirit.CSharp.Tests/Andirit.CSharp.Tests.csproj -c Debug
dotnet test tests/Andirit.LatticeDb.Tests/Andirit.LatticeDb.Tests.csproj -c Debug
dotnet test tests/Andirit.TreeSitter.Tests/Andirit.TreeSitter.Tests.csproj -c Debug
```

Producer results: Andirit 315/315, CSharp 69/69, LatticeDb 38/38 (8 mandatory native
tests), TreeSitter 157/157; 0 failed, 0 skipped.

## 3. What the reviewer must verify

1. Starting HEAD/tree and the exact changed paths of Commits A/B.
2. Candidate C representation: separate source-owned facts, not B2, not graph-visible
   occurrence edges.
3. Actual LatticeDB persistence (inspect `AndiritOccurrenceUnit/Page/Meta`).
4. Publication transaction/visibility boundary (same write transaction as completion;
   `_writeGate` serialization).
5. The eight mandatory native tests (not weakened, not merged away).
6. Unit replacement and deletion.
7. Contributor union/conflict semantics.
8. Target-index boundedness, examined-work accounting and deterministic ordering.
9. Reopen and stale-state refusal.
10. D3 graph noninterference (exact identity/order/count).
11. Schema versioning and D5 refusal.
12. Unknown-outcome and environmental limitations.
13. No unauthorized Stage 2 implementation.

## 4. Fixtures

Native tests create fresh isolated LatticeDB files under the OS temp directory and delete
them afterwards. No shared fixture state is used. No historical prior-writer fixture was
generated (see qualifications).

## 5. Evidence

- `adr-ci1-01-ci1a-impl-01-s1-implementation.md`
- `adr-ci1-01-ci1a-impl-01-s1-native-qualification.md`
- `adr-ci1-01-ci1a-impl-01-s1-d3-noninterference.md`
- `adr-ci1-01-ci1a-impl-01-s1-schema-and-refusal.md`
- `adr-ci1-01-ci1a-impl-01-s1-performance.md`
- `adr-ci1-01-ci1a-impl-01-s1-sha256-manifest.txt` (self-excluded)

## 6. Qualifications the reviewer should challenge

1. **Controlled occurrence records** — Stage 1 uses controlled facts, not Roslyn
   extraction. The reviewer must confirm this boundary was not presented as confirmed
   production occurrence evidence.
2. **Synthetic graph units** — native tests use representative synthetic units, not a
   real `.csproj` ingestion. Real-project ingestion is covered by the unchanged C# suite.
3. **D4 partials** — invariants 6 (supersession) and 10 (concurrency/snapshot) are
   partial and carried to Stage 6.
4. **Historical fixture (SQ-06)** — not generated; schema refusal is proven on fresh
   stores only.
5. **Performance** — single-run Debug measurements, not thresholds.
6. **Page maintenance** — pages are recomputed per completion in Stage 1.
7. **Public surface** — additive public types (`CodeOccurrence*`, `ICodeOccurrenceReader`)
   were added; the reviewer must confirm this is not a user-facing Find Usages API.

## 7. Explicit non-claims

No production Roslyn extraction, no public Find Usages API, no complete CI-1A query
integration, no full D3/D4 qualification, no cross-platform native qualification, no
experimental-schema migration, no dual-write, no B2 fallback, no background/distributed
maintenance, no native-node-ID optimization, no package publication.
