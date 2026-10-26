import type { Renderable } from "../contracts/Renderable";
import type { RenderContext } from "../contracts/RenderContext";

export class SvgRenderer implements Renderable {
  render(context: RenderContext): string {
    return "<svg data-theme=\"" + context.theme + "\"></svg>";
  }
}
