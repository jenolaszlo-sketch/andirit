# Stage 6 entry proposal — STAGE2-ACCEPTANCE-OWNER-01

**Recorded:** 2026-11-22 with `adr-ci1-01-ci1a-stage2-acceptance-owner-01.md`.
Preparation only. **Stage 6 implementation is not authorized** by this proposal or by the
Stage-2 acceptance record. The proposal prefers the shortest dependency-aware path toward
completing the existing roadmap; no unrelated features and no language-coverage expansion are
introduced.

## 1. Remaining authorized roadmap scope (deferred DF items)

| ID | Scope | Nature |
| --- | --- | --- |
| DF-1 | Full D3 real-fixture part (native-mirror full corpus) | qualification of existing behavior |
| DF-2 | D4.5 full native fault matrix | qualification of existing behavior |
| DF-3 | Full S-02 ten-invariant native matrix | qualification of existing behavior |
| DF-4 | F-02 crash/power-loss/multi-process durability | qualification of existing behavior |
| DF-5 | Q2-F09 / Q2-F12 / I-04 cancellation paths | qualification of existing behavior |
| DF-6 | Full platform certification (distributions, architectures, internals) | qualification of existing behavior |
| DF-7 | WP-S24-3 optional cross-target scope; public Find Usages; optimization | optional capability — separate authorization |
| DF-8 | M9.5 cancellation path; M12.1–M12.3 full native matrix | qualification of existing behavior |

## 2. Outstanding functional capabilities and dependency order

Dependency order (shortest path first — each gate feeds the next):

1. **DF-8 / DF-5 cancellation and the applicable native subset** (cheap, no new architecture;
   unblocks the full native matrices).
2. **DF-2 D4.5 full native fault matrix + DF-4 F-02 crash/power-loss/multi-process durability**
   (fault-injection and process-level harnesses; the largest evidentiary gap).
3. **DF-3 full S-02 ten-invariant native matrix** (consumes 1–2).
4. **DF-1 full D3 real-fixture part** (real-corpus before/after observations; consumes the
   accepted corpus and the D4/D5 qualifiers).
5. **DF-6 full platform certification** (expanded OS/architecture matrix; consumes 1–4).
6. **DF-7 WP-S24-3** (optional; owner decision required before any work).

Proposed work packages: **S6.1 native cancellation + applicable D4.5/S-02 subset**;
**S6.2 durability (crash/power-loss/multi-process)**; **S6.3 full native matrices**;
**S6.4 real-fixture D3**; **S6.5 platform certification matrix**. Each package carries its own
qualification/acceptance gate; no package grants release.

## 3. Required qualification and acceptance gates

- Per-package independent qualification (reviewer session ≠ producer session; receipt-based
  independence where hosted runs are used) and owner acceptance, mirroring the S2.x pattern.
- No Stage-6 work package may waive a Stage-2 qualification; the M5.4 amended-contract
  limitations and the Q-L qualifications continue to bind until superseded by an owner record.
- Deferral boundaries remain named; a package must not claim completion of a deferred invariant.

## 4. Recommended next implementation commission

**`ANDIRIT-CI1A-S2.6-STAGE6-ENTRY-01`** (owner-gated): scope S6.1 (native cancellation path and
the applicable D4.5/S-02 native subset qualification) — the shortest dependency-aware entry
into Stage 6. Preparation-only until owner-authorized; the commission must pin the accepted
baseline (`eafd632` or its accepted descendant) and reuse the accepted harness patterns.

## 5. Recommended publication and package-release checkpoint

Publication of the accepted Stage-2 baseline is the **repository CI `pack` job** (already green
on run 44746950426 for the corrected revision) plus a separate owner-gated publication
commission: `ANDIRIT-CI1A-S2.5-PUBLICATION-01`, covering package identity/version, a
pack–consume–load smoke test on each published RID, and the release notes that restate the
Stage-2 qualifications and the deferred Stage 6 scope. Publication is **not** authorized here.

## 6. Non-goals

No new language coverage, no public Find Usages, no optimization work, no schema migration or
dual-write, and no re-opening of accepted Stage-2 scope.