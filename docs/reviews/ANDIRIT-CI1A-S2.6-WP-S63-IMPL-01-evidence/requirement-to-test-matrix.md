# S6.3 requirement-to-test matrix (standalone copy)

Commission: ANDIRIT-CI1A-S2.6-WP-S63-IMPL-01 · Date: 2026-11-24
Case assignments preserve the entry traceability matrix exactly; no invariant was redefined.

| Obligation | Discharging evidence | Class | Status |
| --- | --- | --- | --- |
| D4-1 query uses only the accepted published generation | P-01, P-02 | constructed | qualified (Windows) |
| D4-2 stale/unbound/unverifiable index refuses explicitly | P-01, P-02 | constructed | qualified (Windows) |
| D4-3 no unbounded full-store scan | P-03 | constructed | qualified (Windows) |
| D4-4 failed staging preserves prior publication + valid index | P-04 (+ S6.2 D-01/D-06 carried) | constructed | qualified (Windows) |
| D4-5 cancelled staging exposes nothing | S6.1 Q-01…Q-06, Q-11 (carried, sealed) | — | qualified (S6.1) |
| D4-6 superseded publication cannot replace a newer generation | P-05, P-06 | genuine + constructed | qualified (Windows) |
| D4-7 rebuild/reopen produces an equivalent published index | P-07, P-08 | genuine | qualified (Windows) |
| D4-8 contributor changes and scoped deletion update visibility consistently | P-09, P-10 | constructed | qualified (Windows) |
| D4-9 bounded queries: deterministic ordering and explicit limits | P-11 | constructed | qualified (Windows) |
| D4-10 query behaviour during concurrent publication: snapshot contract | P-12, P-15 | genuine | qualified (Windows; hosted Linux open) |
| S-02-1 all ten invariants against production storage | D4-1…D4-10 | mixed | qualified (Windows; hosted Linux open) |
| S-02-2 failed publication | P-04 (+ carried S6.2 D-01…D-04) | constructed | qualified (Windows) |
| S-02-3 cancelled publication | S6.1 (carried, sealed) | — | qualified (S6.1) |
| S-02-4 superseded publication | P-05, P-06, P-13 | genuine + constructed | qualified (Windows) |
| S-02-5 reopen | P-07, P-08, P-14 | genuine + constructed | qualified (Windows) |
| S-02-6 native concurrency/republication | P-12, P-15 | genuine | qualified (Windows; hosted Linux open) |
| S-02-7 interleaving is diagnostic only | P-16 | documentation | qualified (Windows) |
| M12.1 (D4 1–4, 6–9) | P-01…P-06, P-08…P-11 | mixed | qualified (Windows; hosted Linux open) |
| M12.3 (D4 10) | P-12, P-15 | genuine | qualified (Windows; hosted Linux open) |
| I-05 supersession/reopen | P-05, P-06, P-07, P-08, P-13, P-14 | mixed | qualified (Windows) |
| I-06 concurrency/republication | P-12, P-15 | genuine | qualified (Windows; hosted Linux open) |
| I-07 full D4 matrix | P-01…P-16 (less D4-5) | mixed | qualified (Windows; hosted Linux open) |
| DF-2 / DF-3 / DF-8 | as above | mixed | qualified (Windows; hosted Linux open) |

No invariant lacks evidence. The only open item is the platform leg (hosted Ubuntu), which affects
the execution-evidence class of every case equally and is recorded once (see `environment.txt`).
