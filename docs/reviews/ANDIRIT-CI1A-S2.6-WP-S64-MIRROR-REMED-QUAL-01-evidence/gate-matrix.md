# Qualification gate matrix

| Gate | Requirement | Result | Evidence |
| --- | --- | --- | --- |
| G-1 Seal integrity | 34 evidence files, 7 implementation inputs, binary hashes, ancestry, seal SHA-256 | **Pass** | `seal-verification.json`, `tools/verify_seal.py` |
| G-2 Checkout equivalence | Merge tree equals implementation tree; second parent is implementation; jobs/artifacts attributed; inputs equivalent | **Pass** | `identities.json`, `hosted-verification.json`, `methodology.md` |
| G-3 F-S64E-01 reproduction | Baseline shows duplicate physical nodes/edges and accumulation | **Pass** | `results/baseline/repro-shared.json`, `repro-orphan.json`; `summary.json` baseline tests |
| G-4 F-S64E-01 remediation | Candidate has one physical row per canonical identity; stable reopen | **Pass** | `results/candidate/repro-shared.json`, `repro-orphan.json` |
| G-5 F-S64E-02 reproduction | Disabled publication leaves rows/manifest inconsistency | **Pass** | `results/baseline/repro-orphan.json` |
| G-6 F-S64E-02 remediation | Disable removes physical rows; re-enable canonical | **Pass** | `results/candidate/repro-orphan.json`; recovery receipts |
| G-7 F-S64R-01 reproduction | Baseline deletes unrelated/authoritative node named by a valid-looking manifest | **Pass** | `results/baseline/repro-unrelated-node.json`, `repro-authoritative-node.json` |
| G-8 F-S64R-01 remediation | Candidate preserves unrelated/authoritative objects and rebuilds | **Pass** | `results/candidate/repro-unrelated-node.json`, `repro-authoritative-node.json` |
| G-9 F-S64R-02 reproduction | Baseline deletes foreign incident edges without refusal | **Pass** | `results/baseline/repro-incident-edge.json`, `repro-foreign-outgoing.json` |
| G-10 F-S64R-02 remediation | Candidate refuses ambiguous ownership, preserves state and reopens after repair | **Pass** | `results/candidate/repro-incident-edge.json`, `repro-foreign-outgoing.json` |
| G-11 Ownership adversarial | Protocol orphan, unmarked object, inconsistent/missing/malformed/stale manifests, cross-repo, shared endpoints, sentinel properties | **Pass** | `results/candidate/*.json` |
| G-12 Failure/cancellation | Cleanup failure and cancellation preserve mirror and accepted publication | **Pass** | `results/candidate/cleanup-failure.json`, `cleanup-cancel.json` |
| G-13 Non-cascade awareness | `DeleteNode` observed not to cascade; all mirror deletion paths audited | **Pass** | `results/probe-delete.json`; scope audit in `methodology.md` |
| G-14 Producer-store recovery | 43→20 nodes, 60→30 edges, zero unmapped, occurrence preserved, idempotent, traversal parity | **Pass** | `results/recovery/sealed-graph-only.json`, `sealed-integrated.json` |
| G-15 Independent recovery | Synthetic affected store canonical, authoritative/sentinel/occurrence preserved, idempotent | **Pass** | `results/recovery/independent-recover.json`; `results/affected-store/` |
| G-16 Inventory reconciliation | Accepted 1,005 → candidate 1,034; +29 new tests | **Pass** | `summary.json`, `local-regression.json`, `identities.json` |
| G-17 Named cases | 29 new tests and 19 inherited S6.3 named cases pass | **Pass** | `local-regression.json`, `hosted-verification.json` |
| G-18 Coverage | 55/75/60/70 line and branch floors retained | **Pass** | `summary.json`, hosted coverage |
| G-19 Package consumer | Six packed packages consume and execute from a clean cache | **Pass** | `package-smoke.txt` |
| G-20 Inherited boundaries | S6.1–S6.3 suites pass; dependency/version/P-03 retained | **Pass** | full suite, `summary.json` |
| G-21 No unauthorised changes | Qualification branch contains evidence only; production/producer evidence untouched | **Pass** | branch diff |

No mandatory gate failed.
