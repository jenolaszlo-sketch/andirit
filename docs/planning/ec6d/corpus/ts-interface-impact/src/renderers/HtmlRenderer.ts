import type { Renderable } from "../contracts/Renderable";
import type { RenderContext } from "../contracts/RenderContext";

export class HtmlRenderer implements Renderable {
  render(context: RenderContext): string {
    return "<main data-theme=\"" + context.theme + "\"></main>";
  }
}
