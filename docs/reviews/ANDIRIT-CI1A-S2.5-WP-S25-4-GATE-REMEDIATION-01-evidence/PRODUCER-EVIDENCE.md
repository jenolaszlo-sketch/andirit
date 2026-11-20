# Producer evidence preservation record — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01

**Class**: PRODUCER evidence (preserved verbatim; reviewer analysis lives in the separate
`ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01-evidence/` package).

## Provenance and import

| Item | Value |
| --- | --- |
| Producer package (source of import) | `C:\Windows\Temp\opencode\gate-remediation\ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01\` |
| Imported by | ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01 (independent reviewer), 2026-11-20 |
| Producer report (bound by Git) | `eafd632` → `docs/reviews/ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-REMEDIATION-01.md` (blob `e1789255c1328d730c69016f7e352693b7368cf6`) |
| Temp-package report copy | byte-identical to the Git blob (9,864 bytes, SHA-256 `94b12f28c0dba5cc0a2342f786a351cb89879dd38e70472a54f04a0479e64a6e`) |

## Reviewer rehash of the producer artifacts (exact bytes, SHA-256)

| File | Bytes | SHA-256 |
| --- | --- | --- |
| `coverage-latticedb.cobertura.xml` | 222,719 | `88f2f353d35f6b4612fb3ae396d512b3d1ec3b6e07c0e7d8e8850a0e324d45e0` |
| `latticedb-testresults.trx` | 106,348 | `78ecfafb781a3af8710dc50ae3fe3fedcfe7ae325dd7f9c88568de25b6c35a57` |
| `MANIFEST.txt` (producer original) | 198 | `83d1de196825b783b24c50f08e571b4a7675dd2a0065aff7f76fc50c86093f71` |

The producer `MANIFEST.txt` is a directory tree listing only; it carries **no SHA-256 digests**,
so it cannot satisfy the repository's established self-excluding SHA-256 manifest convention
(compare the QUAL-04 `manifest.json`). The digests above are the reviewer's independent rehash
after the byte-verbatim import. No producer artifact was reconstructed or edited.

## Reconciliation of producer claims against these raw artifacts

| Producer claim | Raw artifact available in package | Reviewer reconciliation |
| --- | --- | --- |
| LatticeDb branch coverage 368/466 = 78.96%, lines 900/969 = 92.87% | `coverage-latticedb.cobertura.xml` | **VERIFIED** — root attributes `branches-covered=368 branches-valid=466 line-rate=0.9287 branch-rate=0.7896`; TRX-independent derivation in the reviewer package |
| LatticeDb tests 75 passed | `latticedb-testresults.trx` | **VERIFIED** — counters `total=75 executed=75 passed=75 failed=0` |
| Exact-byte formatting exit 0 (4,553 tracked blobs) | none (no format log, no export record) | **UNVERIFIABLE FROM PRODUCER PACKAGE** — independently reproduced by the reviewer (exit 0 on a fresh 4,555-blob export at `eafd632`). Note: 4,553 is the parent `f669e107` blob count; `eafd632` has **4,555** |
| Whole-solution 949 passed | none (no whole-solution TRX) | **UNVERIFIABLE FROM PRODUCER PACKAGE** — independently reproduced (949 passed, 0 failed, 0 skipped) |
| Release build succeeded, 0 warnings | none (no build log) | **UNVERIFIABLE FROM PRODUCER PACKAGE** — independently reproduced (exit 0, 0 warnings, 0 errors) |
| "24 focused regression tests" added | test sources and TRX | **COUNT DISCREPANCY** — actual added test cases = **23** (13 in `LatticeDbDurableStateRegressionTests` + 10 in `NativeGraphMirrorTests`); the new file's table row also reads "(14 tests)" where 13 is actual |
| Report HEAD/Tree/Seal `f669e107` / `5df749d2` ("this HEAD") | — | **IDENTITY DISCREPANCY** — those are the parent revision's identities; the sealed remediation commit is `eafd632` / tree `823e3c655f8fa8aea64b68e0935b1048bc9b6f7d` |

All gaps above are recorded as qualifications in the reviewer report. No missing producer
evidence was reconstructed.