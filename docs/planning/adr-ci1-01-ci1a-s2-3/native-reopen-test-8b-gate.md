# First S2.3 durability gate — Option A §5.2 test 8b (native reopen stale-refusal)

**Commission:** `ANDIRIT-CI1A-S2.3-ENTRY-OWNER-01`. Documentation/assessment only.
**Authority:** Option A decision §5.1(5)/§5.2(8); mapping contract §4; S2.2 entry
acceptance §3.5; S2.2 `G-ACCEPT` §11/§17; S2.3 scope inventory §4.

---

## 1. Why this is the first gate

Option A §5.1(5) requires S2.2 to **record the defining `compctx2`** so the validity
gate is enforceable **after reopen**. The accepted disposition (entry acceptance
§3.5, option (a)) assigns **native persistence and reopen enforcement to S2.3** and
names the first S2.3 gate explicitly: a reopen test proving that a stale/changed
defining `compctx2` refuses prior facts — **test 8, second half**.

S2.2 demonstrated only the in-memory portion (WP7). S2.2 `G-ACCEPT` §11 records that
native persistence/reopen is **not** claimed. This gate closes that boundary.

**Escalation preserved:** if the association cannot be safely persisted and enforced
after reopen, the affected path stops and returns for the bounded **Option B**
alternative (Option A §5.1(5)/§7). Option A must not be silently weakened.

---

## 2. Precondition — carrier gap

Per the scope inventory §4, the current native schema carries **no defining
`compctx2` association** on `CodeOccurrenceFact`, `CodeOccurrenceCoverage`,
`CodeGraphBatch`, or `CodeIndexUnitReplacement`. `RecordedCompctx2` is a mint-time
validation input that is not persisted. Therefore this gate requires an **additive**
persisted carrier (D5-compatible) before it can pass.

**A stored structural `asmctx3` (`TargetId`) is not sufficient.** `asmctx3` binds
the defining R2-visible `compctx1` but not the dependency-sensitive `compctx2`
(Option A §5.1(2): "A bare `asmctx3` is not provenance").

Candidate carrier shapes (implementation choice, one selected by the durability
work package; all additive, no format migration):

- a per-defining-unit context record on `PersistedOccurrenceUnit`/`Meta`, keyed by
  defining unit, holding the recorded `compctx2` (and the `compilation-universe1`
  membership evidence required to validate it);
- an equivalent unambiguous per-target defining-context association.

The carrier must be independently reviewable and must not enter `asmctx3`/`usage4`.

---

## 3. Gate contract — required demonstrations

The authorized implementation must demonstrate, at minimum:

1. **Valid publication.** A valid current compilation produces trusted occurrence
   facts (real S2.2 chain), and the mandatory defining-context association is
   persisted with the appropriate native authority in the same accepted lifecycle.
2. **Persistence.** Native close/reopen preserves enough evidence (recorded
   defining `compctx2` + universe membership) to validate the association.
3. **Matching current context remains usable.** A reopened database whose current
   defining context equals the recorded context stays `Available` with the trusted
   facts (and correct coverage).
4. **Changed defining `compctx2` invalidates stale facts.** When the defining
   context has changed (directly or transitively-only), the reopened database must
   **not** report the prior facts as current: the stale facts are refused
   (`StalePublication` / equivalent explicit refusal) or the store refuses to serve
   them.
5. **Stable `asmctx3` does not bypass the stale check.** A transitive-only change
   keeps `asmctx3` stable while `compctx2` changes; the stale-context refusal must
   still fire (test 2's identity-stability property must not defeat the validity
   check).
6. **Reopened database must not report stale facts as current.**
7. **Missing/corrupted/unsupported persisted provenance refuses conservatively** —
   never a successful empty or fabricated current authority.
8. **Complete-empty and unavailable states remain distinguishable** after reopen
   (no collapse).
9. **No unsupported recovery path silently manufactures current authority.**
   (Depends on NEW-01; see ordering.)

---

## 4. Stale-detection semantics (must be specified in the work package)

The work package must specify, from accepted authority, **how** the reopened store
obtains a "current" defining context to compare against the recorded one. Options
consistent with accepted authority include:

- the caller supplies the current verified defining context / universe for the
  query (the store validates recorded == current and `compilation-universe1`
  membership); and/or
- an explicit bounded re-verification path.

The semantics must be explicit and must not assume the live compilations exist on
reopen. A whole-universe `UniverseId` digest alone is not a per-defining-unit
association.

---

## 5. Acceptance criteria

- All §3 demonstrations pass with **real Roslyn facts** (not synthetic records).
- Independent reviewer probes reproduce positives, the transitive-only stale
  refusal, missing/corrupt-provenance refusal, and complete-empty/unavailable
  distinction.
- Frozen identity formulas unchanged; frozen R2 oracle `185/185` (`97F6C5E5…`).
- D3/D4/D5 preserved; no migration; no schema-format change (additive only).
- Producer evidence + independent qualification + owner decision.

---

## 6. Stop / escalation conditions

Stop the path and return to the owner if:

- the association cannot be persisted without a schema **format** migration (D5);
- the association cannot be validated on reopen without the live compilations and
  no bounded caller-supplied-current-context path is acceptable;
- enforcing it would require weakening `asmctx3`/`usage4` or the Option A
  separation.

In any of these cases, present the bounded **Option B** alternative; do not weaken
Option A.
