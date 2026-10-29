# ANDIRIT-RENAME-01 readiness

**READY for the separately scoped ANDIRIT-RENAME-01 milestone.** Canonical main is integrated, synchronized and verified; all 30 extra checkouts are retired without blockers. Migration, compatibility and regression gates pass; no unresolved integration defect was observed. No rename work was performed.

Accepted implementation: `2c3090cba6d8a56102a5c47ceb026c5b5fea3234`; owner planning: `2dd78104ed0a4ea3b3b95a7d8d5319ef452f5f84`; tested merged history: `5548bf4a870733341a1471eb26a9727192dc9f48` / tree `2cd2ca748fad48c0057ddbe5a2864320d4dacf57`. The final evidence commit will supply the immutable canonical rename baseline in the handoff; a document cannot contain its own Git object hash.

Rename-sensitive paths: [path inventory](raw/rename-path-inventory.txt). Identity-bearing occurrences: [identity inventory](raw/rename-identity-inventory.txt). These cover repository/solution/project names, namespaces, package IDs, workflow/project paths, persisted schema metadata, publication hashes, plugin/provider versions and graph IDs. Query witness/publication contracts must be reviewed in [accepted contracts](../../planning/ec6d/consumer-query-contract.md) and [publication consistency](../../planning/ec6d/publication-consistency-contract.md).

Carry forward: graph node/edge IDs, symbols, persisted schema 2 identifiers (`HetuMetadata`, other command-log labels), migration/profile identities, publication identities, provider/source attribution and query witness identities must not change silently as a consequence of renaming repository or assemblies. Public APIs, assembly/NuGet names and namespace compatibility need an explicit controlled rename plan. Historical review documents may retain their original Hetu names and filesystem paths.

Preserved qualifications: narrow positive-only structural export coverage; Windows-focused independent qualification; historical unreproducible migration hash; no representative performance claim; publication consistency/snapshot limitation; R16 intended-consumer validation remains open. No EC-6D implementation is authorized by integration or rename readiness.

The rename itself has not begun. Its starting point must be clean synchronized canonical main with this evidence and final retirement outcomes committed.

Use the final documentation-only commit on canonical main identified in the final handoff as the exact immutable rename starting baseline. It descends from CI-green integration `4aadf0e659e3e536e12c4eb64214a9796f4b323b`; its production source is identical to accepted EC-6C. This handoff binding is external to its own commit to avoid a circular self-hash.
