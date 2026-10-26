# EC-6C integration candidate delta manifest

This manifest is the complete path list from canonical main
558f8ac82ac1b5e07da68f4a71a70240eaf38409 to the EC-6C owner-accepted tip
2c3090cba6d8a56102a5c47ceb026c5b5fea3234. The diff is 65 files with 14,519
insertions and 81 deletions. This inventory describes the candidate only;
none of these files were merged by this planning task.

## Root project documents (3)

~~~text
CHANGELOG.md
README.md
ROADMAP.md
~~~

## Documentation and evidence (38)

~~~text
docs/consumer-guidance-qualified-name-lookup.md
docs/decisions/ec6a-owner-acceptance.md
docs/decisions/ec6b-owner-acceptance.md
docs/decisions/ec6c-a29-closure.md
docs/decisions/ec6c-owner-acceptance.md
docs/decisions/ec6c-track-a-owner-acceptance.md
docs/decisions/ec6c1-owner-acceptance.md
docs/design/ec6a-adversarial-cases.md
docs/design/ec6a-cross-file-resolution-architecture.md
docs/design/ec6a-decision-register.md
docs/design/ec6a-implementation-roadmap.md
docs/design/ec6c-export-adversarial-cases.md
docs/design/ec6c-export-decision-register.md
docs/design/ec6c-export-implementation-roadmap.md
docs/design/ec6c-export-representation-architecture.md
docs/ecmascript-module-resolution.md
docs/ecmascript-symbol-identity.md
docs/evidence/ec6b-owner-acceptance/README.md
docs/evidence/ec6b-owner-acceptance/manifest.md
docs/evidence/ec6b-owner-acceptance/owner-instruction.md
docs/evidence/ec6b-owner-acceptance/validation.md
docs/evidence/ec6b-r1-relative-module-resolution-remediation/README.md
docs/evidence/ec6b-relative-module-resolution-handoff.md
docs/evidence/ec6c-1-export-shape-inventory-handoff.md
docs/evidence/ec6c-2-structural-export-publication-handoff.md
docs/evidence/ec6c-owner-acceptance/final-attestation.md
docs/evidence/ec6c-owner-acceptance/fq1-doc-1-correction.md
docs/evidence/ec6c-owner-acceptance/migration-reproduction/README.md
docs/evidence/ec6c-owner-acceptance/migration-reproduction/reindex-p9/Program.cs
docs/evidence/ec6c-owner-acceptance/migration-reproduction/reindex-p9/reindex.csproj
docs/evidence/ec6c-owner-acceptance/migration-reproduction/reproduction-attempt.md
docs/evidence/ec6c-owner-acceptance/migration-reproduction/reproduction-report.json
docs/evidence/ec6c-owner-acceptance/migration-reproduction/seed-p8/Program.cs
docs/evidence/ec6c-owner-acceptance/migration-reproduction/seed-p8/seed.csproj
docs/evidence/ec6c-owner-acceptance/preflight.md
docs/evidence/ec6c-owner-acceptance/qn-1-note.md
docs/language-feature-matrix.md
docs/language-support-roadmap.md
~~~

## Production source (12)

~~~text
src/Penghou.Hetu.Abstractions/CodeGraphKinds.cs
src/Penghou.Hetu.Abstractions/CodePropertyKeys.cs
src/Penghou.Hetu.TreeSitter/AssemblyInfo.cs
src/Penghou.Hetu.TreeSitter/EcmaScriptExportGraphPublisher.cs
src/Penghou.Hetu.TreeSitter/EcmaScriptExportObservation.cs
src/Penghou.Hetu.TreeSitter/EcmaScriptExtractionProfile.cs
src/Penghou.Hetu.TreeSitter/TreeSitterExtraction.cs
src/Penghou.Hetu.TreeSitter/TreeSitterExtractionSession.cs
src/Penghou.Hetu.TreeSitter/TreeSitterJavaScriptPlugin.cs
src/Penghou.Hetu.TreeSitter/TreeSitterModuleResolution.cs
src/Penghou.Hetu.TreeSitter/TreeSitterSyntax.cs
src/Penghou.Hetu.TreeSitter/TreeSitterTypeScriptPlugin.cs
~~~

The two abstractions files extend graph vocabulary. The remaining source
changes implement Tree-sitter extraction, relative module resolution and
ECMAScript export publication. No C# Roslyn, core query-service or LatticeDB
provider source path changed.

## Tests and test project (12)

~~~text
tests/Penghou.Hetu.TreeSitter.Tests/Penghou.Hetu.TreeSitter.Tests.csproj
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaClassLifecycleTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaClassMemberTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportPublicationTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaExportShapeTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaVariableLifecycleTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterEcmaVariableTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterJavaScriptPluginTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterModuleResolutionLifecycleTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterModuleResolutionTests.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTestSupport.cs
tests/Penghou.Hetu.TreeSitter.Tests/TreeSitterTypeScriptPluginTests.cs
~~~

## Schema and migration assessment

No LatticeDB provider implementation, store schema version, or explicit
schema-migration file appears in this delta. The new shared graph kinds and
property keys are listed above and affect serialized/public graph facts.
EC-6B qualifies p6-to-p8 historical preservation; EC-6C qualifies p8-to-p9
publication. The owner-accepted candidate is p9. An integration rehearsal
must still prove the intended deployment's p6-to-p9 procedure on its durable
store, including old-fact preservation and close/reopen.
