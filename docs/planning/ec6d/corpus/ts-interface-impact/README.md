# TypeScript interface impact corpus

This purpose-built Apache-2.0-compatible corpus exercises a narrowly reviewed
interface-impact scenario. It is committed with the EC6D planning evidence and
is not production code.

Selected declaration: src/contracts/Renderable.ts / Renderable.

Manually reviewed expected relationships:

- Direct explicit implementations of that declaration:
  SvgRenderer, HtmlRenderer, AbstractRenderer, and AliasRenderer.
- Type-annotated consumers:
  Preview.preview, BarrelPreview.previewFromBarrel (via the Drawable
  re-export alias), and any direct API consumer that imports Renderable.
- CanvasRenderer extends SvgRenderer, so a propagated change may affect it
  through an implementation, even though it does not name Renderable.
- OtherRenderer implements a different, same-named Renderable declared at
  src/other/Renderable.ts; it is a negative case for name-only matching.
- MissingImport.unavailable references a missing relative module and is not
  a valid resolved consumer.
- The barrel exports rename symbols but do not redefine their origin.

This is a diagnostic fixture, not a global language-accuracy benchmark. The
expected set records syntax and type relationships visible to a human reviewer;
it does not ask Hetu to type-check the program.
