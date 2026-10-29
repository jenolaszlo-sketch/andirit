# Classification and disposition

The complete baseline naming-occurrence inventory is [CSV](raw/occurrence-classification.csv). Multiple categories apply where a line mixes source and protocol identities. This allowlist was established before editing.

| Category | Disposition |
|---|---|
| A product branding | Active root README, roadmap, benchmark instructions, active docs and source comments use Andirit. CHANGELOG remains historical. |
| B build/source | All six source, four test and one benchmark projects, solution, namespaces, assemblies, package IDs, references, smoke and all three workflows use Penghou.Andirit. |
| C public API | Namespace/assembly rename and HetuBuilder, HetuHost, HetuHostBuilder, HetuHostHealth and host extension classes renamed explicitly. No other API removed. |
| D persisted/protocol | Schema 2, all Hetu labels, hetu_json, plugin/provider IDs, loose C# assembly evidence, profile p9, resolver and identity algorithms retained. Test fixture IDs/temp prefixes retained. |
| E historical | docs/evidence, reviews, decisions, integration, planning and design retained byte-for-byte, including accepted EC-6D contracts and evidence tools. Historical source reproducers still bind their original Git sources. |
| F integrations | Active repository URLs point to penghou-andirit; historical URLs retain their original spelling and use GitHub redirects. NuGet new IDs validated locally only. |

Accepted historical design/planning contracts are accessed through the current [migration guide](../../andirit-migration.md); no implementation authority is added. Third-party grammar filenames, dependency versions, framework and publication workflow package set remain unchanged. Development version remains `0.2.0-preview.6`.
