# EC-6A: Adversarial cases for cross-file resolution

**Status:** design artifact for EC-6A, reconciled under EC6-A1. Companion to
[ec6a-cross-file-resolution-architecture.md](ec6a-cross-file-resolution-architecture.md).
Graph facts below are expected behavior of the design, not implemented;
`resolves-to` facts and AC-41–AC-53 are required EC-6B acceptance fixtures
(the F02/F05/F09 corrections), while `exports`/`binds` facts remain
proposals. **Baseline:** `558f8ac` (EC6-A0).

Conventions used in every case:

- `M(F)` is the file node `{language}:file:{sha256(F)}` for source `F`.
- `S(F, name)` is the declared symbol node the EC-5/EC-3 profile emits for a
  declaration of `name` in `F` (path-scoped for classes, interfaces,
  variables; name-based for top-level functions, types, enums, namespaces).
- `resolves-to` is the owner-accepted EC-6B edge kind; `exports` and `binds`
  are the proposed EC-6C/D kinds from §7 of the architecture document;
  `imports`/`invokes-name`/`inherits` remain the unchanged Structural
  observations.
- "No fact" always means: no edge is emitted and coverage counts record the
  reason. Absence is never proof of absence of the real dependency.
- Invalidation is described relative to the plugin-execution model of the
  architecture document §9.2; staging order and staged-vs-published
  semantics are normative in §9.1.1–§9.1.3.
- Expected **published-graph** results are stated for each lifecycle case;
  unless a case says otherwise, they hold on both graph stores and queries
  never observe staged facts.

---

## 1. Same names across modules (identity isolation)

### AC-01 — Same-named path-scoped exports in two modules

**Sketch**

```ts
// a.ts
export const value = 1;
// b.ts
export const value = 2;
// use-a.ts
import { value } from './a';
```

**Expected facts**

- `exports` M(a.ts) → S(a.ts, `value`) `{exported-name: value, export-kind:
  named, export-via: direct}`.
- `exports` M(b.ts) → S(b.ts, `value`) — a different node because variable
  identity includes the source path.
- `resolves-to` M(use-a.ts) → M(a.ts) `{specifier: ./a, …}`.
- `binds` M(use-a.ts) → S(a.ts, `value`) `{imported-name: value,
  local-name: value, import-kind: named}`.
- No edge from use-a.ts to b.ts.

**Evidence** Resolved (unique policy candidate; unique export). The existing
`imports` placeholder for `./a` remains Structural and unchanged.

**Outcome** Fully resolved; two same-named exports stay distinct.

**Invalidation** Any change to a.ts, b.ts, or use-a.ts re-runs the plugin;
the whole resolution unit is replaced.

### AC-01b — Same-named **name-based** exports (documented identity caveat)

**Sketch**

```ts
// a.ts
export function run() {}
// b.ts
export function run() {}
// use.ts
import { run } from './b';
```

**Expected facts**

- `exports` M(a.ts) → S(`run`) and `exports` M(b.ts) → S(`run`) point at the
  **same node**, because top-level function identity is discriminator +
  lexical name with no path component (`docs/ecmascript-symbol-identity.md:223-232`).
- `binds` M(use.ts) → S(`run`).

**Evidence** Resolved with a recorded caveat: the export facts are true, but
the shared symbol identity is a pre-existing limitation. Consumers cannot
tell from the symbol alone which module declared it; they can see incoming
`exports`/`binds` and the owning file of declarations.

**Outcome** Resolved; documented as a known qualification, not a defect.

**Invalidation** Unchanged model: file units own declarations; resolution
unit owns edges.

**Design response** Accepted (D-05). Any identity migration is a separate
milestone with its own re-extraction and review.

---

## 2. Aliases

### AC-02 — Renamed import

**Sketch** `import { a as b } from './m';`

**Expected facts** `binds` M(importer) → S(m, `a`) `{imported-name: a,
local-name: b, import-kind: named}`.

**Evidence** Resolved.

**Outcome** The local name is never matched against anything by text: the
target comes from the export table of the resolved module.

**Invalidation** Import rename changes the importer's content hash; plugin
executes; resolution unit replaced.

### AC-03 — Renamed export

**Sketch** `// m.ts: const a = 1; export { a as b };` and
`import { b } from './m';`

**Expected facts** `exports` M(m.ts) → S(m.ts, `a`) `{exported-name: b,
export-via: local}`; `binds` M(importer) → S(m.ts, `a`) `{imported-name: b}`.

**Evidence** Resolved (unique local declaration `a`).

### AC-04 — Export of an imported binding (initially unsupported)

**Sketch** `// m.ts: import { x } from './n'; export { x };`

**Expected facts** `resolves-to` M(m.ts) → M(n.ts) for the import;
`export { x }` produces **no** `exports` edge under EC-6C/6D because `x` is
not a local declaration; counted `unsupported` (documented).

**Evidence** n/a (no fact). The future rule ("follow the import binding
chain") is D-06.

**Outcome** Not verified; no fabrication.

---

## 3. Missing and ambiguous modules

### AC-05 — Missing module

**Sketch** `import { x } from './nope';`

**Expected facts** No `resolves-to`, no `binds`. Coverage:
`resolves-to.unresolved-targets += 1` (candidate examined, module not in
inventory). Existing `imports` placeholder is unchanged.

**Evidence** n/a. **Outcome** Unresolved. **Invalidation** If `nope.ts` is
added, the importing plugin executes (new file) and the edge appears.

### AC-06 — Extensionless specifier matches several candidates

**Sketch** Files `x.ts` and `x.tsx` both exist; `import './x';`

**Expected facts** Exactly one `resolves-to` to `x.ts` with
`{resolution-rule: extension-substitution}` because the policy order is
fixed. **Not ambiguous by construction** — the ordered list is the policy.

**Evidence** Resolved. **Outcome** Deterministic single choice; the
resolution-rule property is the justification.

### AC-07 — File versus directory collision

**Sketch** `x.ts` and `x/index.ts` both exist; `import './x';`

**Expected facts** `resolves-to` → `x.ts` (extension substitution is tried
before directory index). Documented ordering.

### AC-08 — Escaping the repository root

**Sketch** `import { z } from '../../../outside/z';` from a shallow path.

**Expected facts** No edge; `unsupported-targets` (invalid/escaping
relative path — refused, never normalized outside the repository).

**Evidence** n/a. Note: `ContractValue.RelativePath` refuses `..` segments
for source paths (`src/Penghou.Hetu.Abstractions/ContractValue.cs:31-44`);
the resolver mirrors that invariant for specifiers.

---

## 4. Type-only semantics

### AC-09 — Type-only import of a type-only export

**Sketch** `// m.ts: export type T = { a: 1 };` `import type { T } from './m';`

**Expected facts** `resolves-to` `{module-reference: import-type,
type-only: true}`; `binds` → S(m, `T`) `{type-only: true}`.

### AC-10 — Value import of a type-only export (mismatch)

**Sketch** `// m.ts: export type T = {};` `import { T } from './m';`

**Expected facts** `resolves-to` emitted (the module resolved);
**no** `binds` edge; counted `unresolved-targets` with the mismatch reason
recorded in documentation. Rationale: emitting the binding would claim a
value binding that TypeScript rejects.

### AC-11 — Type-only import of a value export

**Sketch** `// m.ts: export const n = 1;` `import type { n } from './m';`

**Expected facts** `binds` → S(m, `n`) `{type-only: true}`. Type-only
imports of value symbols are legal (erased at emit); the binding is true.

---

## 5. Re-export chains, stars, cycles

### AC-12 — Three-hop re-export chain

**Sketch**

```ts
// c.ts: export const deep = 1;
// b.ts: export { deep } from './c';
// a.ts: export { deep } from './b';
// use.ts: import { deep } from './a';
```

**Expected facts**

- `resolves-to` a→b, b→c, use→a.
- `exports` M(c) → S(c, `deep`) `{export-via: direct}`.
- `exports` M(b) → S(c, `deep`) `{exported-name: deep, export-via:
  re-export, via-module: c.ts}`.
- `exports` M(a) → S(c, `deep`) `{export-via: re-export, via-module: b.ts}`.
- `binds` M(use) → S(c, `deep`).

**Evidence** Resolved everywhere. The final declaration is the target; the
intermediate hop is recorded in `via-module`, and each hop has its own
`resolves-to` edge, so the chain is auditable.

### AC-13 — Re-export cycle

**Sketch** `// a.ts: export { x } from './b';` `// b.ts: export { x } from './a';`

**Expected facts** `resolves-to` a→b and b→a are emitted (module resolution
succeeds). No `exports` edge for `x` on either module (unresolved name;
cycle cut by the visited set). Termination guaranteed by §11.2.

**Evidence** Resolved for module edges only.

**Outcome** Conservative: no fabricated binding; coverage counts record the
unresolved export sites. **No hang, no stack overflow.**

### AC-14 — Conflicting star exports

**Sketch**

```ts
// x.ts: export const thing = 1;
// y.ts: export const thing = 2;
// m.ts: export * from './x'; export * from './y';
// use.ts: import { thing } from './m';
```

**Expected facts** `resolves-to` m→x, m→y; **no** `exports` edge for
`thing` from m (two distinct targets → ambiguous, counted
`ambiguous-targets`); no `binds` for `thing` in use.ts (counted ambiguous).
Other names from x/y would each get per-name `exports` edges
`{export-via: star, via-module: …}`.

**Evidence** Resolved for the module edges; ambiguity is never resolved by
preference order.

### AC-14b — Local shadowing beats star

**Sketch** `// m.ts: export const thing = 9; export * from './x';` (x also
exports `thing`).

**Expected facts** `exports` M(m) → S(m, `thing`) `{export-via: direct}`;
**no** star edge for `thing` (shadowed); star edges for x's other names.

### AC-15 — Star never re-exports default

**Sketch** `// m.ts: export { default } from './x'; export * from './x';`

**Expected facts** The named re-export of `default` emits an `exports` edge
targeting x's default declaration (`exported-name: default`). The star edge
set excludes `default`.

### AC-16 — Star chain convergence and duplication

**Sketch** `m.ts` star-exports both `./x` and `./y`, which themselves
star-export the same `./base` that exports `shared`.

**Expected facts** `shared` appears exactly once from m (same target via two
paths → deduplicated, not ambiguous), pointing to S(base, `shared`), with
one deterministic `via-module` value (the first resolved path in ordinal
order, documented); no duplicate edges.

---

## 6. Relative paths, extensions, configuration

### AC-17 — Relative normalization

**Sketch** `// src/pages/page.ts: import '../lib/util';` resolving to the
inventory path `src/lib/util.ts`.

**Expected facts** `resolves-to` with `{specifier: ../lib/util,
resolution-rule: extension-substitution}`.

### AC-18 — Extension substitution and index files

**Sketch** `import './a'` → `a.ts`; `import './dir'` → `dir/index.ts`;
`import './b.ts'` → exact.

**Expected facts** Three `resolves-to` edges with `resolution-rule` `exact`,
`extension-substitution`, `directory-index` respectively.

### AC-19 — Explicit `.js` specifier pointing at `.ts` source

**Sketch** `import { x } from './x.js';` where only `x.ts` exists.

**Expected facts** No edge under `relative-v1`; `unresolved-targets`
(documenting the policy gap). Deferred to a future policy level with its own
fixtures (D-08).

### AC-20 — Unsupported extension

**Sketch** `import config from './config.json';` (and `./widget.astro`).

**Expected facts** No `resolves-to`; `unsupported-targets`; existing
`imports` placeholder unchanged.

### AC-21 — `tsconfig` path alias

**Sketch** `import { x } from '@/lib/x';` with `paths` configured in
`tsconfig.json`.

**Expected facts** Non-relative specifier → no edge; counted
`external-targets` (§6.2.3). A future explicit policy level may resolve it;
it must then parse
only the declared config subset, embed a config identity in provenance, and
invalidate on config changes (D-09). No configuration is read today.

### AC-22 — Bare package specifier

**Sketch** `import { z } from 'zod';`

**Expected facts** `external-targets`; no edge; the `imports` placeholder
remains the only observation. `node_modules` is never read.

### AC-23 — Case sensitivity

**Sketch** `import './Lib/x';` while the file is `lib/x.ts`.

**Expected facts** No match (ordinal comparison); unresolved. This matches
repository-relative path handling elsewhere; it is deterministic and safe on
case-insensitive file systems even when the repository contains both
spellings.

---

## 7. Module systems

### AC-24 — ESM and CommonJS mixed in one file

**Sketch**

```js
import { join } from 'node:path';
import { a } from './a.mjs';
const b = require('./b.mjs');
```

**Expected facts**

- `./a.mjs` → `resolves-to` `{module-reference: import}` + `binds` `a`.
- `./b.mjs` → `resolves-to` `{module-reference: require}` only. **No CJS
  binding** (`require` assigns an opaque exports object; EC-6B–E do not
  model it). Counted unsupported for bindings only; module dependency is
  still verified.
- `node:path` → external, no edge.

**Evidence** Resolved for module edges.

### AC-25 — CommonJS export surface

**Sketch** `module.exports = { a };` / `exports.b = 1;`

**Expected facts** No export facts; unsupported counts. A future milestone
may model CJS surfaces; nothing is guessed now.

### AC-26 — Dynamic or computed module references

**Sketch** `await import('./' + name);` / `require(`./${x}`);` /
`import c = require('./c');`

**Expected facts** No edges (dynamic template/expression forms); no
invocation facts either (existing profile behavior for these shapes is
unchanged). `unsupported-targets` where the profile observed a candidate
site.

---

## 8. Deletion, rename, and change

### AC-27 — Deleted target module

**Sketch** Delete `m.ts`, which `use.ts` imported (`./m`) and `n.ts`
re-exported.

**Expected facts** Plugin executes (Deleted plan item). The obsolete file
unit for m.ts is reported
(`TreeSitterExtractionSession.cs:163-169` semantics, unchanged). The
resolution unit is fully rebuilt: no edges target or source M(m.ts); the
`binds` edges in use.ts/n.ts disappear (module unresolved now).

**Evidence** n/a for removed facts.

**Invariant** Unit replacement semantics guarantee no stale edge can
survive; the empty-or-smaller replacement is validated against the
prospective graph before publication.

### AC-28 — Renamed module file

**Sketch** `m.ts` → `module.ts`; importer updated to `import './module'` in
the same change, and one importer left stale containing `./m`.

**Expected facts** Old path absent everywhere; updated importer resolves to
the new file; stale importer counts unresolved. No identity relation between
old and new module is invented.

### AC-29 — Export changed, importer unchanged

**Sketch** `m.ts` changes from `export const x = 1` to `export const y = 1`;
`use.ts` still does `import { x } from './m'`.

**Expected facts** m.ts content hash changes → plugin executes → resolution
unit replaced. The `exports` edge for `x` is gone, `y` appears; `binds` for
`x` in use.ts disappears (name not found; unresolved). No stale binding.

### AC-30 — Resolver policy/version change

**Sketch** Resolver revision or `resolution-policy` value changes (for
example adding extension substitutions later).

**Expected facts** Provider version changes → plugin version changes → every
path re-extracts and re-resolves; all resolver edges carry the new
policy/version stamp. A publication can never mix two resolver versions.

**Evidence** Provenance rule of architecture §8; invalidation rule §9.2.

---

## 9. Partial and interrupted state

### AC-31 — Parse error in the target module

**Sketch** `m.ts` has a malformed region but valid `export const good = 1;`;
`use.ts` imports `good` and `broken`.

**Expected facts** Existing behavior: valid facts survive; run-wide produced
kinds become `Partial` (`TreeSitterExtractionSession.cs:122-126`). Resolver:
`binds` for `good` emitted; `broken` unresolved; resolver kinds reported
`Partial` because a source in the run parsed with errors (D-17).

### AC-32 — Undecodable target module

**Sketch** `m.ts` contains invalid UTF-8; `use.ts` imports `./m`.

**Expected facts** m.ts stages an empty completion (existing behavior), so no
file node is published for it. The resolver inventory excludes it:
`resolves-to` is **not** emitted (no fabricated endpoint), counted
unresolved; resolver kinds `Partial`.

### AC-33 — Interrupted publication

**Sketch** A run that stages some file units and a resolution unit fails
before completion (plugin crash, store rejection, cancellation).

**Expected facts** Nothing becomes visible: the store discards staged
changes and keeps the previous publication, including the previous
resolution unit (`src/Penghou.Hetu/ICodeGraphStore.cs:27-35`; conformance
"failed-run-discards-staged-graph"). No partial edge set is ever queryable.

### AC-34 — Superseded run

**Sketch** Two runs race; the first completes against a moved publication.

**Expected facts** Existing conflict behavior is unchanged
(`superseded-run-completion-conflicts`): the losing run fails loudly; the
winning publication's resolution unit is consistent with its source state.

---

## 10. Ambiguity and unsupported syntax in the source file

### AC-35 — Dual projection of a function-valued variable

**Sketch** `// m.ts: const handler = () => 1; export { handler };`

**Expected facts** `export { handler }` finds two symbol-bearing nodes named
`handler` in m.ts (the EC-5 `field` and the callable projection). Under the
unique-match rule: **no** `exports` edge; `ambiguous-targets` count. Direct
form `export const handler = () => 1` would have captured the field node id
and resolved; the list form is the ambiguous case. Documented limitation
(D-05).

### AC-36 — Duplicate same-file declarations, one exported

**Sketch** `var shared; export var shared;`

**Expected facts** One symbol node (existing convergence, each physical
declaration retained). One `exports` edge `{export-via: direct,
exported-name: shared}` from the direct form's captured target. No
ambiguity: the direct form does not use name lookup.

### AC-37 — Anonymous default export

**Sketch** `export default () => 1;`

**Expected facts** No declaration identity exists (existing profile emits no
symbol). No `exports` edge; `unsupported-targets`; `import d from './m'`
gets module resolution but no binding. Synthetic identities are a separate
future decision (D-11).

### AC-38 — Shadowed names and nested scopes

**Sketch** File-level `const x = 1;` plus a function-local `const x = 2;`
before `export { x };`

**Expected facts** File-level declaration is the unique file-level symbol;
the local variable is out of EC-5 scope (no facts), so the export edge binds
to the file-level node. This is correct for module exports but is a
documented limitation of the structural scope model (locals are invisible to
all queries).

### AC-39 — Cross-plugin import (TypeScript → JavaScript)

**Sketch** `// app.ts: import { helper } from './helper.js';` where
`helper.js` is owned by the JavaScript plugin.

**Expected facts** The TypeScript resolver's inventory contains only
TypeScript files: module-not-found → no `resolves-to`, no `binds`; counted
unresolved. The JavaScript plugin never sees the importer. Documented
cross-plugin limitation (D-18); measured in EC-6F.

**Evidence** n/a. This case must appear in consumer documentation so that
absence of an edge in mixed repositories is understood.

### AC-40 — `.d.ts` module target

**Sketch** `import type { P } from './types';` where only `types.d.ts`
exists.

**Expected facts** `.d.ts` is claimed by the TypeScript plugin (suffix
`.ts`); it publishes a file node but (per F4) no declaration facts from
ambient content. `resolves-to` emitted `{resolution-rule:
extension-substitution}`; no `binds` (export table empty); unresolved
count. Honest: the module dependency is verified, the binding is not.

---

## 11. Lifecycle acceptance fixtures (F02, EC-6B required)

These five cases pin the normative staging/replacement contract of
architecture §9.1.1–§9.1.3. "Previous publication" names a completed
publication used as the starting state; every case asserts the exact
published graph before and after, on both stores.

### AC-41 — Target deletion

**Sketch** Published state: `a.ts` contains `import { x } from './b';`,
`b.ts` exists. Delete `b.ts`; `a.ts` is unchanged.

**Expected published graph**

- File node `M(b)` is absent; `a.ts`'s structural `imports` placeholder for
  `./b` remains unchanged (Structural).
- No `resolves-to` edge from `M(a)`; any previous edge to `M(b)` is gone
  with the resolution-unit replacement.
- Coverage: `resolves-to` unresolved target count increases by one.

**Lifecycle** Plugin executes (deletion classifies the plan); the obsolete
file unit for `M(b)` is staged after the rebuilt resolution unit; validation
passes because no surviving edge references the deleted node
(architecture §9.1.3). No stale edge is queryable in the same run.

### AC-42 — Target addition

**Sketch** Starting from the AC-41 published state, add `b.ts` back with the
same content while `a.ts` is unchanged.

**Expected published graph**

- The new file node `M(b)` appears in the same atomic publication as the
  `resolves-to` edge from `M(a)` to `M(b)`.
- Edge properties include `specifier: ./b`,
  `resolution-rule: extension-substitution` (or `exact` for a specifier
  naming the full filename), `resolution-policy: relative-v1`.

**Lifecycle** New item classifies the plugin as executing; the target unit
is staged before the resolution unit (step 1 before step 4), so endpoint
validation passes.

### AC-43 — Empty resolution replacement

**Sketch** Published state: `a.ts` imports `./b` and the `resolves-to` edge
exists. Remove the import statement from `a.ts` (both files remain).

**Expected published graph**

- No `resolves-to` edge exists; the resolution unit is still published and
  may be empty.
- Structural facts of `a.ts` reflect the edit; no stale resolver edge
  survives.

**Lifecycle** The rebuilt resolution unit completes with a zero-edge batch
and replaces the previous unit (GLM probe P2). If instead *all* files of the
plugin are deleted, the empty unit is displaced by ownership hygiene
(architecture §9.1.3, F10) — pinned separately in EC-6E.

### AC-44 — Resolution failure after per-file staging

**Sketch** Inject a resolver failure (test seam) after the per-file loop has
staged `a.ts`'s and `b.ts`'s file units, before the resolution unit
completes.

**Expected published graph**

- The run reaches `Failed`; staged facts are discarded; the previous
  publication — including the previous resolution unit — is byte-identical
  to its state before the run and fully queryable.
- No partial `resolves-to` edge set is ever visible.

**Lifecycle** Failure propagates through plugin execution
(`CodeIndexingLifecycle.cs:471-492`); the store retains the prior
publication (`failed-run-discards-staged-graph`).

### AC-45 — Cancellation before publication

**Sketch** Cancel the indexing token after file units are staged and before
run completion.

**Expected published graph**

- The run reaches `Cancelled`; staged facts are discarded; the previous
  publication (including the previous resolution unit) is retained.
- A cancellation is never converted into an empty or partial resolution
  unit.

**Lifecycle** The resolver observes the token between modules and before
batch emission (architecture §9.4.2); conformance
`cancelled-run-discards-staged-graph` holds on both stores.

---

## 12. Edge identity and multiplicity fixtures (F09, EC-6B required)

These cases pin architecture §7.1.1. All ids follow the existing formula:

```text
{language}:{sha256(kind \n source \n target \n discriminator \n
                   startLine:startColumn:endLine:endColumn)}
```

### AC-46 — Two imports, same target, different specifiers

**Sketch** In `use.ts`: `import { a } from './m';` and
`import { b } from './m.ts';`.

**Expected facts** Two `resolves-to` edges with the same source `M(use)` and
target `M(m)`, distinct ids because the discriminator includes the raw
specifier; properties record `specifier: ./m` and `specifier: ./m.ts`, each
with its own `resolution-rule` (`extension-substitution` and `exact`).

### AC-47 — Two imports, identical specifiers, distinct locations

**Sketch** In `use.ts`: `import { first } from './shared';` and
`import { second } from './shared';` on different lines.

**Expected facts** Two distinct edges; the discriminator is identical, the
location input differs. Both are returned by outgoing traversal with their
own evidence locations.

### AC-48 — Same target imported from different files

**Sketch** `a.ts` and `b.ts` both contain `import './m';`.

**Expected facts** Two `resolves-to` edges with distinct sources and the same
target; incoming traversal to `M(m)` returns both.

### AC-49 — One of two same-target imports removed

**Sketch** Start from AC-47; delete the second import statement (and nothing
else).

**Expected published graph** Exactly one `resolves-to` edge remains; its id
is byte-identical to the surviving edge's id in the previous publication.
The removed occurrence's id is gone with the unit replacement.

### AC-50 — Import location shifted by source edits

**Sketch** Start from AC-47; insert a comment line above both imports.

**Expected published graph** Both edge ids change because the location input
changed; the facts (source, target, specifier, policy, rule) are unchanged.
Occurrence-anchored identity is the documented behavior
(architecture §7.1.1 rule 4); no stale edge survives the replacement.

### AC-51 — Deterministic repeated extraction

**Sketch** Extract the same sources twice in independent runs with identical
plugin/resolver versions and policy.

**Expected facts** Byte-identical edge ids, properties, batching order, and
coverage counts. Re-indexing unchanged sources is a no-op returning the
previous publication (`PluginsExecuted = 0`).

### AC-52 — LF/CRLF identity

**Sketch** The same logical file content with LF and with CRLF line endings,
in two otherwise identical runs.

**Expected facts** Identical edge ids in both runs: Tree-sitter rows and
columns are unaffected by `\r` (it belongs to the preceding row), so the
location input is identical (architecture §7.1.1 rule 5).

### AC-53 — Identity collision rejection

**Sketch** A resolver defect that produces two different facts under one
edge id (differing target or properties with identical identity inputs).

**Expected facts** The run fails loudly: `batch.edge.duplicate` at batch
validation, `replacement.edge.duplicate` within a unit, or the cross-unit
`graph.edge.conflict` when properties differ; the previous publication is
retained. Nothing is silently merged or dropped.

---

## Summary of design responses

| Challenge | Response |
| --- | --- |
| Name equality | Never sufficient. Targets come from resolved modules' export tables or captured declaration ids |
| Ambiguity | No edge; explicit coverage counts; no preference heuristics except the declared ordered resolution policy |
| Cycles | Visited-set cut; termination proven; cyclic contributions ignored |
| Aliasing | Written names and forms preserved as properties; binding target is the final declaration |
| Chains | Followed to the final declaration; every hop has its own `resolves-to`; `via-module` records the direct hop |
| Stars | Per-name edges where uniquely visible; conflicts and shadowing resolved conservatively |
| Type-only | Explicit syntax recorded; value import of type-only export refused |
| Missing/unusual modules | Unresolved/external/unsupported counts; never a fabricated node or edge |
| Deletion/rename | Existing unit replacement + plugin-execution invalidation; resolution unit fully rebuilt |
| Lifecycle (delete/add/empty/failure/cancel) | Ordered staging then atomic publication; staged facts never visible; prior publication retained on failure or cancellation; AC-41–AC-45 |
| Multiplicity and identity | Occurrence-anchored ids over (kind, source, target, discriminator, span); distinct written facts coexist; collisions rejected loudly; AC-46–AC-53 |
| Policy/config change | Version-stamped provenance and version-driven re-extraction; no mixed-version publication |
| Partial/failed runs | Nothing partial ever becomes visible; `Partial` coverage states; prior publication retained |
| Cross-plugin and non-ECMAScript modules | Explicit documented boundaries; EC-6F measures the recall cost |
