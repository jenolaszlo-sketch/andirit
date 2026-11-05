# ANDIRIT-CI-FORMAT-R1 — Restore hosted CI formatting gate

**Commission:** `ANDIRIT-CI-FORMAT-R1` (bounded CI remediation; formatting only).
**Repository:** Andirit. **Canonical branch:** `main`. **Date:** 2026-11-05.
**Baseline:** `958d98ac0fc782e659734f8e56889784df9fdbfb` (tree `9496cf2633ff77fb17af0d5462c0d4fb78cd3431`), synchronized with `origin/main` (0/0), clean working tree, one worktree, no stashes — all verified before any change.

**Disposition: `CI FORMAT RESTORED`** (locally verified; hosted run outcome recorded in the handoff).

---

## 1. Original CI failures

Hosted runs `42027019732` (this baseline) and `41805099074` (previous published base `e0d9110`) both failed identically on all three OS jobs at:

```
dotnet format Andirit.slnx --verify-no-changes --no-restore
```

All four "Test and coverage" steps were skipped on every OS; the `pack` job passed. Exact diagnostics (evidence: `ci-run-42027019732-format-errors.log`, `ci-run-41805099074-format-errors.log`):

| # | Diagnostic | Location |
| --- | --- | --- |
| 1 | `WHITESPACE` — replace 1 char with newline + 8 spaces | `src/Andirit.Abstractions/CodeGraphSource.cs(30,11)` |
| 2 | `WHITESPACE` — replace 1 char with newline + 16 spaces | `src/Andirit.CSharp/MetadataReferenceValidation.cs(115,27)` |
| 3 | `WHITESPACE` — replace 1 char with newline + 16 spaces | `src/Andirit.CSharp/MetadataReferenceValidation.cs(115,38)` |
| 4 | `WHITESPACE` — replace 1 char with newline + 16 spaces | `src/Andirit.CSharp/MetadataReferenceValidation.cs(122,27)` |
| 5 | `CHARSET` — fix file encoding (BOM) | `src/Andirit.Abstractions/CodeOccurrence.cs(1,1)` |
| 6 | `CHARSET` — fix file encoding (BOM) | `tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs(1,1)` |

The failure predates the S2.2 work: the offending lines were introduced by ancestors of `e0d9110` (`c02d5ea`, `d5644d8`, `fc515ff`, `0c989e2`), so neither WP5 nor the consolidation caused it.

## 2. Toolchain and configuration

- CI: `actions/setup-dotnet@v5`, `dotnet-version: 10.0.x`; no `global.json`. Local SDK `10.0.401` (runtime `10.0.12`, Windows x64) — same major line as CI.
- `.editorconfig` (root, 7 lines): `root = true`; `[*] charset = utf-8`, `insert_final_newline`, `trim_trailing_whitespace`, `indent_style = space`, `indent_size = 4`.
- Reproduction: local `dotnet format Andirit.slnx --verify-no-changes --no-restore` → **exit 2** with exactly the six diagnostics above (`format-verify-before.log`). No additional formatting issues were present.

## 3. Protected-file classification (per commission §3)

| File | Classification | Evidence |
| --- | --- | --- |
| `src/Andirit.Abstractions/CodeOccurrence.cs` | **Frozen R2 authority** (S2-REM-01, `fc515ff`; referenced by `adr-ci1-01-ci1a-s2-obligation-register.md`, `adr-ci1-01-ci1a-s2-1-authorization-boundary.md`, `adr-ci1-01-ci1a-s2-2-entry-acceptance-owner-01.md`) | bytes preserved |
| `tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs` | Producer test fixture (WP1-WP3, `0c989e2`); referenced by no accepted decision | BOM stripped |

A repo-wide scan found exactly two `.cs` files with a UTF-8 BOM under `src/`/`tests/` — the two reported ones.

### Byte-level integrity assessment

- **`CodeOccurrence.cs` — unchanged.** SHA-256 before and after: `78FBD358BE875735E3E29A68A7FE8FF22DCE511BA8FC72D1FE9795A6C785B27D` (`protected-file-integrity-before.txt` / `-after.txt`). Instead of rewriting accepted authority, a **single-file scoped `.editorconfig` section** now requires `charset = utf-8-bom` for this one path. The gate stays strict: the rule *enforces* the historical encoding rather than disabling verification; every other file remains `charset = utf-8` (no BOM). Accepted commits, trees, and hashes are untouched.
- **`NonAssemblyMetadataFixture.cs` — 3-byte BOM strip, nothing else.** Length 5455 → 5452 (delta exactly 3); first bytes now `75 73 69 6E …` (`using …`). No test logic, identifiers, or literals changed; the mixed line-ending state of this file is original and untouched.

## 4. Corrections applied (formatting-only)

| File | Change |
| --- | --- |
| `src/Andirit.Abstractions/CodeGraphSource.cs` | Object-initializer brace moved to its own line (1 line → 2); no tokens changed |
| `src/Andirit.CSharp/MetadataReferenceValidation.cs` | Multi-label `case` lines split, one label per line (0x18/0x19/0x1c and 0x11/0x12); comments and statements unchanged |
| `tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs` | UTF-8 BOM removed (3 bytes) |
| `.editorconfig` | +5 lines: scoped `[src/Andirit.Abstractions/CodeOccurrence.cs]` → `charset = utf-8-bom` with rationale comment |

Full diff: `change-diff.txt`. No semantic edits, no unrelated formatting, no behavior change.

## 5. Verification results

| Check | Command | Result |
| --- | --- | --- |
| Formatter gate | `dotnet format Andirit.slnx --verify-no-changes --no-restore` | **exit 0**, no diagnostics (`format-verify-after.log`) — proves no remaining issues, incl. none beyond the reported six |
| Build | `dotnet build Andirit.slnx -c Release` | **0 warnings, 0 errors** (`build-release.log`) |
| Solution tests | `dotnet test Andirit.slnx -c Release --no-build` | **742 / 0 / 0** = 325 + 217 + 157 + 43 — matches the accepted baseline exactly (`solution-tests-release.log`) |
| Frozen R2 oracle | `SemanticCorpusOracle` against `oracle-expected-r2.json` | **185/185, 0 failed**; output SHA-256 `97F6C5E5A4CC77E089481C7643732B7A07A31D483C2643BD7A95FDAD7CED39C9` == accepted evidence; expected file `274A7AAE9EB6724879D619416E449C52759B44085D7B718DAD3434B15387CBBF` unchanged (`oracle-r2.log`, `oracle-r2.json`) |
| WP5-Q1 F1 probe (must still fail) | `Wp5_Ambig_Invocation_MustNotEmitTrustedFact` | **FAIL exactly as documented**: `WP5Q1 ambig facts=1 ambiguous=0 unresolved=0` (`wp5-q1-f1-probe.log`) — the known blocking defect is neither altered nor suppressed |
| Semantic drift vs baseline | `git diff --stat 958d98a -- src tests docs/decisions docs/planning` | only the three formatting files above; **decisions and planning untouched** |

## 6. Changed-file inventory

- `src/Andirit.Abstractions/CodeGraphSource.cs` (formatting only)
- `src/Andirit.CSharp/MetadataReferenceValidation.cs` (formatting only)
- `tests/Andirit.CSharp.Tests/NonAssemblyMetadataFixture.cs` (encoding only)
- `.editorconfig` (scoped charset rule for the frozen file)
- `docs/reviews/ANDIRIT-CI-FORMAT-R1.md` (this report)
- `docs/reviews/ANDIRIT-CI-FORMAT-R1-evidence/` (logs, hashes, diffs)

## 7. Qualifications

- Local verification is Windows x64 only; the hosted matrix (windows/ubuntu/macos) executes the identical formatter command and re-verifies the gate on the published commit (outcome recorded in the handoff).
- The scoped `.editorconfig` exception is intentionally permanent for the frozen file: `CodeOccurrence.cs` keeps its historical BOM bytes until the owner separately decides to normalize frozen authority (not authorized here).
- WP5-Q1 F1 remains open and is untouched; its remediation belongs to the next development commission, identified as `ANDIRIT-CI1A-S2.2-WP5-R1`.

## 8. Exclusions honored

No WP5 F1 remediation; no change to the ambiguous-invocation fallback, WP3/WP4/WP5 semantic contracts, the frozen R2 oracle, or any accepted decision; no failing correctness test suppressed; no WP6 work; no unrelated formatting; no package publication; no force-push, rebase, reset, or linked worktree.
