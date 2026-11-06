# S2.3 independent-qualification and evidence plan

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Closure authority:** **IR+O** (independent source-bound review **and** owner
acceptance); producer tests are not closure (obligation register §4).

---

## 1. Gate structure

| Package | Producer evidence | Independent gate | Owner decision |
| --- | --- | --- | --- |
| WP-S23-1 NEW-01 | defect reproduction + regressions + logs/TRX | `NEW-01-Q1` | `NEW-01-OWNER` |
| WP-S23-2 durability | carrier + test 8b evidence | `S23-DUR-Q1` | `S23-DUR-OWNER` |
| WP-S23-3 publication | integration evidence | `S23-PUB-Q1` | `S23-PUB-OWNER` |
| WP-S23-4 lifecycle | L1–L20 evidence | `S23-LIFE-Q1` | `S23-LIFE-OWNER` |
| WP-S23-5 acceptance | combined evidence | collective reviews | `S2.3-ACCEPT-OWNER` |

The independent reviewer is never the implementer. Each gate's verdict is
reviewer-owned; the owner records acceptance separately.

---

## 2. Required evidence per package

- **Real inputs:** source bytes + file hashes, encoding, normalized path; actual
  Roslyn compilations; metadata and project references; live defining instances.
- **Identities/provenance:** `compctx1`/`compctx2`/`asmctx3`/`usage4`; recorded
  defining `compctx2`; universe membership.
- **Native state:** database contents before/after publication; unit/page/meta
  records; reopen observations; replacement/deletion effects.
- **Transactions/recovery:** fault-injection results; prior-publication retention;
  recovery projection correspondence; unknown-outcome quarantine.
- **Coverage/refusal:** `Produced`/`Partial`/unavailable classification; refusal
  counters/reasons.
- **Artifacts:** raw logs, TRX, environment, self-excluded manifests,
  reproduction commands.
- **Independence proof:** expected values from accepted authority/frozen R2, never
  from the implementation under test; reviewer probe projects committed.

---

## 3. Frozen integrity preserved

- Frozen R2 corpus and `oracle-expected-r2.json` unchanged (`274A7AAE…`).
- Frozen oracle replay `185/185`, output `97F6C5E5…`.
- Frozen identity formulas and `CodeOccurrenceFact` recomputation unchanged.
- Prior S2.2 evidence preserved, not rewritten.

---

## 4. Regression baseline at entry

At entry HEAD the whole solution is **789 / 0 / 0** and the frozen oracle
**185/185** (S2.2 `G-ACCEPT` evidence). Every S2.3 package must keep the whole
solution green and the oracle at `185/185` with `97F6C5E5…`, and pass
`dotnet format Andirit.slnx --verify-no-changes` and the Release build (0/0).

---

## 5. Platform

- Primary independent qualification: **Windows x64** (SDK/runtime as recorded).
- The expanded **Linux** native suite is an **S2.5/Stage-2 acceptance** obligation,
  not full certification (Stage 6).
- No fresh S2.3 platform qualification is inferred from earlier hosted CI.

---

## 6. Unresolved gaps carried into S2.3 qualification

1. Defining-`compctx2` carrier does not exist yet (document 3 / WP-S23-2).
2. `NEW-01` recovery defect not fixed (document 2 / WP-S23-1).
3. S2.2 extractor not wired to publication (WP-S23-3).
4. N1–N8 are synthetic (WP-S23-4).
5. S-01 component-digest canonicalization remains caller-supplied (document 6;
   S2.5 closure).
6. Native schedule test (SQ-03) not yet performed (WP-S23-4).
7. Historical authentic fixtures (SQ-04) allocated to available-format evidence;
   not fabricated.

---

## 7. Stop conditions

Independent qualification must stop and report if: D3/D4/D5 are weakened; a frozen
identity formula changes; a schema **format** migration is introduced; the
`compctx2` association cannot be enforced after reopen (→ Option B escalation);
recovery reliance is required before NEW-01 closes; or a public API becomes
mandatory.
