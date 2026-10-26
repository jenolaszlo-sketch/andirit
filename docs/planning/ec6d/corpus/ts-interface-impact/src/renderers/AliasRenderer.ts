import type { Renderable as Drawable } from "../contracts/Renderable";
import type { RenderContext } from "../contracts/RenderContext";

export class AliasRenderer implements Drawable {
  render(context: RenderContext): string {
    return "<canvas data-theme=\"" + context.theme + "\"></canvas>";
  }
}
