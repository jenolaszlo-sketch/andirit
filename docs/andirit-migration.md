# Migrating from Hetu to Andirit

Andirit is the new public identity of the existing Penghou code graph project. Solution, six source projects, assemblies and local package IDs use `Penghou.Andirit`; namespaces use `Penghou.Andirit` with the existing nested suffixes. The active solution is `Penghou.Andirit.slnx`.

Replace package references `Penghou.Hetu[.*]` with corresponding `Penghou.Andirit[.*]`, update using directives and replace `HetuBuilder`, `HetuHost`, `HetuHostBuilder`, `HetuHostHealth`, and `HetuHostBuilderExtensions` with their Andirit forms. Rebuild your application and all direct consumers: old binaries do not bind automatically to the renamed assemblies. There are no legacy compatibility wrappers. Other general-purpose type names and graph behavior stay the same.

New packages retain development version `0.2.0-preview.6` and are validated through a local feed. **No Andirit package publication is part of this milestone.** Existing published Hetu packages/releases remain untouched; existing users can continue using them until they deliberately migrate.

Durable format remains schema 2. `HetuMetadata` and related historical labels, `hetu_json`, `penghou.hetu.*` plugin/provider IDs, loose-source `Hetu.Loose.CSharp` evidence, graph identities and TS/JS p9 semantics are intentional protocol history. Do not replace them with Andirit strings or rewrite databases. See the [cross-version qualification](rename/andirit-rename-01/durable-compatibility.md) for tested profiles and environment; always work on backups when qualifying your own data.

The canonical GitHub URL becomes [penghou-andirit](https://github.com/jenolaszlo-sketch/penghou-andirit) after the gated in-place rename. The old GitHub repository URL redirects; update local remotes to the new URL after that operation. Repository ID, Git history, tags, reviews, issues and archival refs remain the same. Local checkout becomes `C:\Users\Laszlos\source\repos\Penghou.Andirit`.

Historical review/acceptance/integration documents retain Hetu names, original paths and Git bindings. The [accepted EC-6D owner contract](decisions/ec6d-owner-01-contract-acceptance.md) and [planning index](planning/ec6d/README.md) remain authentic historical contracts; use the namespace mapping above for future consumers. R16 intended-consumer confirmation remains required before EC-6D implementation. No new extraction or consumer intelligence is introduced.

Qualifications remain: Windows-only durable rename rehearsal, historical unreproducible migration hash, narrow positive-only extraction, source snapshot consistency limits and no performance qualification. Read the [milestone evidence](rename/andirit-rename-01/README.md) before relying on a completion claim.
