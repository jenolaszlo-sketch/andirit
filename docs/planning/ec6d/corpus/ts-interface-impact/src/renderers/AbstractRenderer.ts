import type { Renderable } from "../contracts/Renderable";
import type { RenderContext } from "../contracts/RenderContext";

export abstract class AbstractRenderer implements Renderable {
  abstract render(context: RenderContext): string;
}
