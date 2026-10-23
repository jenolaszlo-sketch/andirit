# Penghou.Hetu EC-3D — GLM Q2 Preservation Checkpoint

Status: **EC-3D CLOSED, VERIFIED WITH QUALIFICATIONS.** The first qualified real-repository reindex remains outstanding.

## 1. Repository and Git binding

Repository: jenolaszlo-sketch/penghou-hetu
Canonical path: C:\Users\Laszlos\source\repos\Penghou.Hetu
Origin: https://github.com/jenolaszlo-sketch/penghou-hetu.git

| Role | Commit | Parent | Tree |
| --- | --- | --- | --- |
| EC-3C baseline | e4598ac9b517654b8346cdcaf96c594fc50bc53b | 29c0e45967acc3943cda7b8f215f4971da18a336 | 7158725553a4f45170ce5f90b39b575f4d6ed56a |
| EC-3D implementation | b1d4827489c0aea114952049d11f082650514c02 | e4598ac9b517654b8346cdcaf96c594fc50bc53b | 43a2a2ae5b1ddd8f13e51195133c2aff8f6e88f5 |
| EC-3D documentation | 1cc2939ea615d627cde616d06e739867be4e56e2 | b1d4827489c0aea114952049d11f082650514c02 | d97170219fe930fe2e1921853be479e39675bb0d |
| Independent GLM Q2 review | 1287590ee0c46bd863873d30baea633c3b04ef13 | 1cc2939ea615d627cde616d06e739867be4e56e2 | 6b71ffee62af3634d0e41336492f44ca4d19a1f9 |
| Prior GLM Q1 review, unchanged | ba2ca83ace500e148a2bfa7de9c0c01d8ef6f6ff | e4598ac9b517654b8346cdcaf96c594fc50bc53b | Preserved on review/ec3-glm-q1 |

Verified implementation ancestry is e4598ac → b1d4827 → 1cc2939. The Q2 review is a direct child of 1cc2939. Its report, docs/reviews/ec3-glm-q2-heritage-remediation-verification.md, has blob SHA 24e418bc98ee0b46dcb706d4076d83d5e758294e.

Required topology:

    e4598ac
       |
    b1d4827
       |
    1cc2939
       |\
       | +-- 1287590  review/ec3-glm-q2
       |
       +---- NEW    preservation/ec3-glm-q2-heritage-remediation

The review and preservation commits are siblings. Neither is an ancestor of the other. Review-only files remain on review/ec3-glm-q2: tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmQ2ReviewAdversarialTests.cs, tests/Penghou.Hetu.TreeSitter.Tests/EcmaGlmQ2ReviewPublicationTests.cs, and the REVIEW-ONLY ProjectReference in tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj.

At checkpoint preparation, local main pointed at 1cc2939 and origin/main pointed at 0d05c6aac6933bea549ac5a8458f25ae653d82a6. No distinct local implementation branch name at 1cc2939 was present. The requested implementation publication ref therefore requires resolution without changing canonical main; no branch name is inferred here.

## 2. F01 correction

The original EC3-GLM-Q1 F01 counterexample was a nested class named Base shadowing an unrelated top-level Base. Under the former identity, the written heritage target could converge with a symbol-bearing declaration and incorrectly make create.Derived appear dependent on the outer Base.

EC-3D gives written heritage targets a declaration-less identity:

typescript:heritage-target:{sha256(path + "\n" + writtenName)}

The candidate has no SymbolId and occupies a namespace separate from declared symbol identities. The declaration create.Derived keeps its declared class SymbolId. Its inherits edge to the unresolved Base candidate remains Structural. Published impact from the unrelated top-level Base excludes create.Derived; impact from the candidate includes create.Derived under the documented traversal preset.

The independent review verified the original case at extraction level and after publication through CodeIndexingService, CodeGraphQueryService public lookup, neighborhood and impact APIs, and store reads. F01 is corrected.

## 3. Independent review and test evidence

GLM's exact disposition is **VERIFIED WITH QUALIFICATIONS**. It found no confirmed or plausible production defects within the EC-3D scope.

The 17 review probes were nine adversarial probes and eight publication probes:

| Probe | Coverage and outcome |
| --- | --- |
| Q2-CE1 | Original shadowing counterexample at extraction level; all eight identity and evidence invariants held. |
| Q2-CE2 | Same written name in different files; path-scoped candidates stayed distinct. |
| Q2-CE3 | Same written name in different lexical scopes in one file; shared candidate retained distinct source edges and locations. |
| Q2-CE4 | Qualified heritage names and expression targets; dotted names were preserved and unsupported mixin expressions produced no candidate. |
| Q2-CE5 | implements and interface inheritance beside a declared interface; edge kinds and Structural evidence were correct. |
| Q2-CE6 | Prototype inheritance and dynamic patterns; exactly three supported Derived edges were emitted and unsupported dynamic cases fabricated no facts. |
| Q2-CE7 | Declared class, interface and member identities remained equal to the pre-EC-3D formulas. |
| Q2-CE8 | Extraction serialization was deterministic across input orderings. |
| Q2-CE9 | Unsupported forms and generic heritage; Base<string> produced the documented name-subset candidate Base. |
| Q2-P1 | F01 through published queries and impact in both directions; exact-name lookup observation recorded as F-Q2-01. |
| Q2-P2 | Incremental removal of one shared-candidate occurrence retained the surviving edge; final removal retired the candidate. |
| Q2-P3 | Renaming the written target replaced the old candidate relationship. |
| Q2-P4 | Deleting the owning source file removed its heritage facts. |
| Q2-P5 | Unchanged reindex reported zero changed files and preserved fact identities. |
| Q2-P6 | Failed indexing preserved the prior publication and candidate impact result. |
| Q2-P7 | p4-to-p5 shape transition invalidated the unchanged source and replaced old placeholder facts with candidate facts. |
| Q2-P8 | Durable LatticeCodeGraphStore publication, dispose, reopen and verification succeeded. |

Reported test breakdown:

- TreeSitter focused baseline before review probes: 82 passed.
- Review filter FullyQualifiedName~EcmaGlmQ2Review: 17 probes, comprising 9 adversarial and 8 publication probes.
- Full Penghou.Hetu.slnx: 331 passed, 0 failed — Hetu 160, TreeSitter 99 (82 baseline plus 17 review), LatticeDb 26, CSharp 46.

Published graph probes used the real indexing service and public query APIs. InMemoryCodeGraphStore was exercised by P1–P7. LatticeCodeGraphStore was exercised with dispose and reopen by P8. A normal durable reopen is not crash-recovery evidence; process-kill interruption was not simulated.

## 4. Review-probe development history

Four early failures were defects in the review probes, were corrected in the review-only tests, and were retained in the GLM report:

1. CE2 compared an unsorted expected-ID array.
2. CE6 used a line-range predicate that matched all three prototype edges.
3. P7 used fake version 9.9.9+p4, which did not satisfy EndsWith(".p4") and emitted the p5 shape during its p4 phase.
4. P6 used a fake-plugin class name that collided with a real fixture name.

These failures remain in the historical record. The probes were fixed, and the implementation was not modified to address them.

## 5. Remaining P3 qualifications

### F-Q2-01 — Exact-name lookup ambiguity

**Classification: DESIGN AMBIGUITY / P3.** An exact-name query can return a declared symbol and a same-named declaration-less heritage candidate. The candidate has no SymbolId, so callers that assume one result or use SingleOrDefault may receive null or encounter an ambiguous result. Consumer guidance is recorded in docs/ecmascript-symbol-identity.md: declaration consumers should filter for SymbolId or use a declared-symbol query, and should inspect ambiguity.

### F-Q2-02 — Shared candidate identity

**Classification: DOCUMENTED / P3.** Same-file unresolved heritage references with the same written name may share one candidate node; edge source, location and evidence distinguish occurrences. GLM verified incremental removal behavior. No implementation change is authorized by this checkpoint.

### F-Q2-03 — Generic heritage written name

**Classification: DOCUMENTED / P3.** extends Base<string> produces a name-subset candidate for Base. Generic arguments are not modeled and semantic type resolution is outside this structural extraction behavior. This is recorded in docs/ecmascript-symbol-identity.md and the feature matrix.

### F-Q2-04 — p4-to-p5 migration verification

**Classification: METHOD LIMITATION / P3.** GLM verified profile invalidation with a faithful plugin-shape proxy, planner source checks and actual .p5 version assertions. The old p4 binary was unavailable, so the evidence does not prove a real historical p4-binary-to-p5-binary transition.

On the first qualified real-repository reindex, verify profile invalidation and regeneration from any available p4 state. If no real p4 artifact exists, record that limitation and verify available profile-change behavior without inventing historical migration evidence. Do not start that qualification as part of this preservation task.

## 6. Scope and closure

This checkpoint and its accompanying documentation updates change documentation only. No production source, test, project file, generated graph store, language plugin, extractor version, dependency or CI configuration is included. Review-only tests and their project reference remain exclusive to review/ec3-glm-q2. Original implementation, documentation, Q1 and Q2 producer commits are not amended, rebased, merged or cherry-picked.

The report's review scope was Windows-only. Cross-platform determinism was not claimed. Process-kill interruption was not simulated. The review covered EC-3D only, not the full ECMAScript extractor or all Hetu graph behavior.

**EC-3D: CLOSED, VERIFIED WITH QUALIFICATIONS.** The four P3 qualifications remain traceable above. Real-repository qualification remains open.

## 7. Publication and fresh-clone verification instructions

Publish only these three refs after the implementation branch identity is resolved: the existing implementation branch at 1cc2939, review/ec3-glm-q2 at 1287590ee0c46bd863873d30baea633c3b04ef13, and preservation/ec3-glm-q2-heritage-remediation at its preservation commit. Use ordinary non-force refspec pushes. First inspect each remote ref; if it exists at a different SHA, stop. Do not update main, tags, packages, or unrelated branches.

After publication, clone origin into a temporary clean directory and verify all three branch tips against their intended full SHAs. Verify the e4598ac → b1d4827 → 1cc2939 ancestry; verify that review and preservation have immediate parent 1cc2939 and are siblings; verify neither sibling is an ancestor of the other; and verify the report blob SHA above. Confirm the two review-only test files and REVIEW-ONLY project reference exist only on review/ec3-glm-q2. Compare preservation with 1cc2939 and confirm its changed files are documentation only. Confirm F01 is corrected, the verdict and four qualifications remain, and real-repository qualification remains outstanding. Check that main, tags and unrelated branch tips match the pre-publication state. Run git fsck --full and record its exit status. Remove the temporary clone after verification.

At checkpoint preparation, live origin reported main at 0d05c6aac6933bea549ac5a8458f25ae653d82a6 and no review/ec3-glm-q2 or preservation branch. Since the only local branch at 1cc2939 was main and the task prohibits updating canonical main, publication must stop until the intended existing implementation branch is identified. This limitation does not alter the verified local ancestry or sibling separation.
