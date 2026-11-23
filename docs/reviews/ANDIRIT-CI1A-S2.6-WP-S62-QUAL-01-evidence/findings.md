# ANDIRIT-CI1A-S2.6-WP-S62-QUAL-01 — findings

| ID | Finding | Severity | Disposition |
|------|---------|----------|-------------|
| F-QU-01 | D-05 skipped due to test logic bug (writer doesn't complete publication) | Medium | Accept with required regression follow-up. The second Q-06 scenario missing from committed suite. Require permanent inclusion in committed suite. |
| F-QU-02 | D-05 covers only first plan scenario; second scenario (completed-but-cancelled) missing from committed suite | Medium | Accept with required regression follow-up. Reviewer executed second scenario and it passes. Require permanent inclusion in committed suite. |
| F-QU-03 | Q-12 covers only managed census; native-ABI half not in committed test | Low | Accept — discharged by reviewer re-census. Optional test extension in future. |
| F-QU-04 | Q-02 expected outcome unachievable for untimed canceller; invariant framing is correct | Low | Accept — plan deviation justified. Record deviation. |
| F-QU-05 | Q-10 mapping divergence (traceability label only; substance discharged by unchanged suite counts) | Low | Accept — traceability labelling only. |
| F-QU-06 | Producer's §6/§7 quote Windows coverage for three suites under hosted heading | Low | Accept with erratum. Bounded erratum recommended. |
| F-QU-07 | Producer's package/API receipts produced at baseline HEAD (provenance stamps match) | Low | Accept — evidence note. Rebuilt correctly; 2/6 entries differ, all provenance metadata. |
| F-QU-08 | Q-08 omits "no temporary artifact remains" assertion | Low | Accept — handle-release is load-bearing. Optional completeness. |

All findings non-blocking. None remediated here.