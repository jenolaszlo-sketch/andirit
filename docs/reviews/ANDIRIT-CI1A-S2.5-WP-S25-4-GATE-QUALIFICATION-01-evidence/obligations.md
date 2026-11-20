# Outstanding obligation reconciliation — ANDIRIT-CI1A-S2.5-WP-S25-4-GATE-QUALIFICATION-01

Per the commission, these obligations are explicitly reconciled and **not** resolved, closed,
or waived. The governing record remains `docs/planning/adr-ci1-01-ci1a-s2-5/wp-s25-4-obligation-inventory.md`
and the QUAL-04 disposition.

## 1. M5.4 contract amendment

- **Status: BLOCKED — UNCHANGED BY THIS COMMISSION.**
- M5.4 (`Unresolved` refusal — missing defining compilation/universe) held disposition
  **D. REACHABILITY UNRESOLVED — ADDITIONAL EVIDENCE REQUIRED** under QUAL-04; that disposition
  stands. The remediation under review contains no production behavior change (comment-only
  relocations) and no new compiler-symbol-association evidence; the LatticeDb tests do not
  expand production visibility or the private reflection seam, so they bear on M5.4 not at all.
- No contract amendment is proposed or implied. The next actions on the QUAL-04 route (narrow
  M5.4 investigation or explicit owner clarification) remain open.

## 2. M11.4 genuine Ubuntu execution evidence

- **Status: BLOCKED — and now SCOPE-STALE (newly widened).**
- Before this remediation, the Linux row was qualified against the **52-case** LatticeDb suite
  (WP-S25-2 accepted G2/G3 chain: 52/0/0 on genuine Ubuntu 24.04.5 x64). The remediation adds
  23 test cases (suite now 75), so the existing genuine-Ubuntu receipts no longer cover the
  current expanded suite. M11.4 ("cross-platform native tests pass") therefore requires a
  **corrected-revision genuine Ubuntu execution**.
- This commission executed Windows x64 only. No Ubuntu run was performed, emulated, or claimed.

## 3. M14.1 genuine Ubuntu execution evidence

- **Status: BLOCKED — and now SCOPE-STALE (newly widened).**
- M14.1 requires the *entire current expanded* `Andirit.LatticeDb.Tests` suite to pass on
  Linux; "current" now includes the 23 added cases. Same corrected-revision genuine-Ubuntu
  requirement as M11.4. M14.2 (platform-specific behavior documentation) rides the same route.

## 4. Remaining WP-S25-4 closure requirements

- **F-G2-01**: TECHNICALLY SATISFIED at `eafd632` (independently reproduced 368/466 = 78.96%
  ≥ 70% under the unchanged CI policy). Closure remains owner-governed: the register
  (`wp-s25-4-obligation-inventory.md` F-G2-01 row) requires "a separate binding decision before
  Stage-2 acceptance". No owner decision is issued here.
- **Exact-byte formatting**: TECHNICALLY SATISFIED at `eafd632` (independently reproduced exit 0
  from a byte-exact export). The QUAL-04 NOT-SATISFIED/NOT-WAIVED finding is discharged on the
  technical layer by this qualification; the formal profile disposition remains with the owner.
- **WP-S25-4 matrix rows**: this commission supplies reviewer evidence for the two gate rows it
  covered (QUAL-04-format-gate, F-G2-01) and the preservation rows exercised under GQ-04. The
  full QUAL-matrix reconciliation (M-series, overlays) was not re-run; prior rows remain as
  recorded in QUAL-04, with M5.4/M11.4/M14.1 blocked as above.
- **Stage 2.5 / Stage-2 acceptance**: NOT GRANTED by this commission. Passing this commission
  does not constitute WP-S25-4 owner acceptance or Stage 2.5 closure.

## 5. Recommended next bounded commission

1. **Owner decision record for F-G2-01 closure + exact-byte formatting profile disposition**
   (bounds: review of this qualification package plus the owner decision register; no new
   code, no test changes).
2. **Corrected-revision genuine-Ubuntu qualification** — the combined authorized route
   (genuine Ubuntu x64 + test-only coverage reprise) pinned at `eafd632` or its linear
   descendant, covering the full 75-case LatticeDb suite plus core/CSharp/TreeSitter counts,
   to refresh M11.4/M14.1 and the repository CI conclusion.
3. **Narrow M5.4 investigation** per the QUAL-04 owner route (pinned compiler proof or a
   concrete association-gap candidate), separately authorized.
4. Only after 1–3: a WP-S25-4 closure owner-acceptance record reconciling the full matrix.

Sequence note: item 2 is the long-pole item; items 1 and 3 are analysis-only and can run in
parallel with it.