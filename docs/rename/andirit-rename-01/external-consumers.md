# External-consumer impact

Read-only scan of locally accessible sibling Penghou repositories covered C#/project/build/config/workflow/Markdown references (`Penghou.Hetu`, `penghou-hetu`, `penghou.hetu`). No source import, package dependency or runtime configuration consumer was found in the scanned siblings; no directly dependent build is blocked. No unrelated repository was edited.

| Location | Finding | Disposition |
|---|---|---|
| Penghou.Hongxian/README.md:78 | historical GitHub ecosystem link | Old URL continues through redirect; follow-up branding update in its own milestone. |
| Penghou.Hongxian/docs/roadmap.md:708 | exclusion of transitive Penghou.Hetu* dependencies | Architectural text; follow-up should mention both historical and new package prefixes. No active dependency. |
| Penghou.Guihua/docs/evidence-driven-workflow-evolution-v2.md:443 | relative ../.. sibling Penghou.Hetu/ROADMAP.md link | Local checkout rename makes the relative link stale; follow-up in Guihua should point to Penghou.Andirit/ROADMAP.md or canonical web URL. Does not affect build/runtime. |

Full matches: [scan](raw/external-consumers-scan.txt). Historical review/planning mirrors and arbitrary repositories outside the accessible Penghou sibling scan are not claimed exhaustively validated. Existing published Hetu package consumers can retain old dependencies temporarily. Consumers adopting new packages must update IDs, namespaces, product types and rebuild; package publication is a separate authorized milestone. Future EC-6D consumers use the migration mapping while original accepted contracts and R16 remain unchanged.
