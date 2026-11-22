# Release identity and integrity record — RELEASE-RECOVERY-01

**Recorded:** 2026-11-22 with `ANDIRIT-CI1A-S2.5-RELEASE-RECOVERY-01.md`.

## Release identity

| Item | Value |
| --- | --- |
| Package version | `0.2.0-preview.7` |
| Release tag | `v0.2.0-preview.7` (annotated, object `6659647527a6e0eb822a3d7a05b8283c75fd6a3e`) |
| Tag target | `6ed05964d2fcc758648b654a01ec706647e52183` (unchanged; not recreated, not moved) |
| Publication workflow | `.github/workflows/publish.yml` on the tagged commit (six csproj pack + tag/version gate + inventory gate + OIDC) |
| Failed attempt | run **44965409440 attempt 1** (2026-11-22T00:11:55Z): OIDC login HTTP 401 — no matching trust policy; zero packages published (sealed separately in `ANDIRIT-CI1A-S2.5-RELEASE-01-evidence/`) |
| Recovered attempt | run **44965409440 attempt 2** (2026-11-22T05:17:16Z–05:17:47Z): **success** — all steps green, 12 packages published |
| Published artifacts | 6 nupkg + 6 snupkg (`publish-attempt2.log`, `publish-attempt2-extract.txt`) |
| Published at | `https://api.nuget.org/v3/index.json` (PUTs to `www.nuget.org/api/v2/package/` and `/symbolpackage/`) |

## Public package URLs (nuget.org)

| Package | Package page |
| --- | --- |
| Andirit | https://www.nuget.org/packages/Andirit/0.2.0-preview.7 |
| Andirit.Abstractions | https://www.nuget.org/packages/Andirit.Abstractions/0.2.0-preview.7 |
| Andirit.CSharp | https://www.nuget.org/packages/Andirit.CSharp/0.2.0-preview.7 |
| Andirit.LatticeDb | https://www.nuget.org/packages/Andirit.LatticeDb/0.2.0-preview.7 |
| Andirit.Testing | https://www.nuget.org/packages/Andirit.Testing/0.2.0-preview.7 |
| Andirit.TreeSitter | https://www.nuget.org/packages/Andirit.TreeSitter/0.2.0-preview.7 |

Registration-index download endpoints:
`https://api.nuget.org/v3-flatcontainer/{id}/0.2.0-preview.7/{id}.0.2.0-preview.7.nupkg`
(all six verified HTTP 200 and downloaded — `public-feed-availability.txt`).

## Symbol publication

Six `PUT …/api/v2/symbolpackage/` calls each answered `Your package was pushed.` in attempt 2,
and the explicit symbol step's duplicate check returned `Package '…snupkg' already exists at feed`
for all six — the feed confirms symbol-package presence. The `.snupkg` 404 on the flatcontainer is
the post-symbol-server-migration behavior (controls: `penghou.hetu 0.2.0-preview.6` and
`newtonsoft.json 13.0.3` `.snupkg` also 404). Symbol packages are consumed by debuggers from
symbols.nuget.org via PDB checksums.

## Published content integrity

Downloaded feed copies hashed (`public-feed-verification.txt`); nuspec metadata verified at
`0.2.0-preview.7` with Apache-2.0, "Andirit contributors", README packed, and the correct
dependency graph; `Andirit.TreeSitter` carries the three RID natives at the expected sizes
(`public-feed-content.txt`). Consumer-restore integrity: the isolated cache copies are
byte-identical (SHA-256) to the feed downloads (`cache-integrity.txt`).

## Repository state

Local and remote `main` at the evidence commit (`ffd6a04` at preflight; this record's commit is
reported in the handoff). Tag unchanged; `origin/main` fast-forward only; no force-push; no
tag deletion/recreation; no version increment; historical Hetu packages untouched; qualification
branches preserved. All prior sealed evidence packages remain manifest-exact.