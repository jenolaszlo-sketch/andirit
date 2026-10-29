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

## Explicit project mapping

| Before | After |
|---|---|
| Penghou.Hetu.slnx | Penghou.Andirit.slnx |
| Penghou.Hetu (directory/project/assembly) | Penghou.Andirit |
| Penghou.Hetu.Abstractions (directory/project/assembly) | Penghou.Andirit.Abstractions |
| Penghou.Hetu.CSharp (directory/project/assembly) | Penghou.Andirit.CSharp |
| Penghou.Hetu.TreeSitter (directory/project/assembly) | Penghou.Andirit.TreeSitter |
| Penghou.Hetu.LatticeDb (directory/project/assembly) | Penghou.Andirit.LatticeDb |
| Penghou.Hetu.Testing (directory/project/assembly) | Penghou.Andirit.Testing |
| Penghou.Hetu.Tests (directory/project/assembly) | Penghou.Andirit.Tests |
| Penghou.Hetu.CSharp.Tests (directory/project/assembly) | Penghou.Andirit.CSharp.Tests |
| Penghou.Hetu.TreeSitter.Tests (directory/project/assembly) | Penghou.Andirit.TreeSitter.Tests |
| Penghou.Hetu.LatticeDb.Tests (directory/project/assembly) | Penghou.Andirit.LatticeDb.Tests |
| Penghou.Hetu.Benchmarks (directory/project/assembly) | Penghou.Andirit.Benchmarks |

Six source project PackageIds map with their same suffixes; no new packable project was introduced. All nested CSharp, TreeSitter, LatticeDb, Testing and test namespaces map to Penghou.Andirit. The standalone smoke project retains its existing path and now validates all six packages. Canonical checkout is D:/projects/Andirit by explicit owner instruction.
