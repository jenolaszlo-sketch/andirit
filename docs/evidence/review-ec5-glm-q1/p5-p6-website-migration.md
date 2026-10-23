# EC5-GLM-Q1 executed publication evidence

REVIEW-ONLY artifact produced by the EC5-GLM-Q1 independent review. The numbers
below are verbatim console output of a review harness that ran the authentic
p5 extractor (built from commit `1cc2939ea615d627cde616d06e739867be4e56e2`) and
the p6 extractor (review branch `review/ec5-glm-q1` at `5a7c59c4b0f783980ef7301069edf1a8fba7ecc2`)
against the same isolated `LatticeCodeGraphStore` and the website snapshot
`jenolaszlo-sketch/website@f9cca1773a32bb517e624cc380e1f6d14e0cc317`
(git-archive snapshot, 78 files, 9 supported sources).

Environment: Windows 10.0.26100 x64, .NET SDK 10.0.401. Run date: 2026-10-23.

## 1. Authentic p5 publication (fresh store, p5 plugin build)

```text
PUBLISH status=Completed discovered=78 new=9 changed=0 unchanged=0 deleted=0 unsupported=69 nodes=109 declarations=62 edges=235 units-completed=9 units-deleted=0 warnings=[]
PUBLICATION-VERSION run:ec5-p5
```

Verified by reopening the store in a second process (p5 harness `verify`):

```text
PUBLICATION run:ec5-p5-only
RAW unit-count=9 nodes=109 unique-nodes=107 declarations=62 unique-declarations=62 edges=235 unique-edges=235
EDGE-KINDS contains=62 declares=62 imports=9 inherits=1 invokes-name=101
NODE-KINDS callable=40 file=9 interface=10 property=38 type=12
EVIDENCE Structural=235
FIELDS 0
DUPLICATE-SYMBOL-NODES 0
DECLARATION-COLLISIONS False
```

This independently reproduces the EC-4 baseline (107 unique nodes, 62
declarations, 235 edges) and confirms p5 emitted zero `field` nodes.

## 2. Authentic p5 -> p6 invalidation (same store, p6 plugin build)

```text
PUBLISH status=Completed discovered=78 new=0 changed=9 unchanged=0 deleted=0 unsupported=69 nodes=130 declarations=83 edges=277 units-completed=9 units-deleted=0 warnings=[]
PUBLICATION-VERSION run:ec5-p6
```

All 9 previously published units were re-planned as `changed` (stale p5
manifest versions) and re-extracted despite unchanged content hashes.

Verified by reopening the store in a second process (p6 harness `verify`):

```text
PUBLICATION run:ec5-p6
RAW unit-count=9 nodes=130 unique-nodes=128 declarations=83 unique-declarations=83 edges=277 unique-edges=277
EDGE-KINDS contains=83 declares=83 imports=9 inherits=1 invokes-name=101
NODE-KINDS callable=40 field=21 file=9 interface=10 property=38 type=12
EVIDENCE Structural=277
FIELDS 21
DUPLICATE-SYMBOL-NODES 0
DECLARATION-COLLISIONS False
```

Delta over p5: +21 unique `field` nodes, +21 declarations, +42 edges
(`contains` 62 -> 83, `declares` 62 -> 83; `imports`, `inherits`, and
`invokes-name` counts unchanged). The two raw-vs-unique node collapses
(130 -> 128) pre-exist in p5 (109 -> 107) and come from name-based
import/invocation placeholder identities shared across files.

## 3. Unchanged reindexing after p6 (same store, third run)

```text
PUBLISH status=Completed discovered=78 new=0 changed=0 unchanged=9 deleted=0 unsupported=69 nodes=0 declarations=0 edges=0 units-completed=0 units-deleted=0 warnings=[]
PUBLICATION-VERSION run:ec5-p6
```

Zero facts re-extracted and the prior publication is retained unchanged.

## 4. Declaration-level query evidence (p6 store, 21 fields)

Exact-name lookups: each of `STATUSES`, `STATUS_BY_KEY`, `CATEGORIES`,
`CATEGORY_BY_KEY`, `RUNTIME_LAYERS`, `SUPPORTING_PRIMITIVES`, `evidenceLink`,
`projects`, `journal`, `milestones`, `philosophy`, `evidence`, `collections`,
`rawBase`, `SITE`, `NAV`, `FOUNDER`, `site` returns exactly one `field`
candidate; `base` returns two (javascript `astro.config.mjs` + typescript
`src/lib/paths.ts`, distinct identities); `GET` returns the documented
`field` + `callable` pair.

`FindEnclosingSymbolAsync("src/lib/site.ts", 17, 14)` -> `field:NAV`.
Neighborhood of interface `StatusDefinition` contains only the interface and
its three member properties; no file-level variable is contained by any class
or interface. Name-pattern query `GET` returns `callable:GET` and `field:GET`.

All 21 declaration spans matched the independently read sources, and all 11
directly exported declarations carry node `modifiers` `const export`.
