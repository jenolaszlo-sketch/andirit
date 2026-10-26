import type { Renderable } from "../contracts/Renderable";
import { SvgRenderer as Renderer } from "../renderers/SvgRenderer";

export function preview(value: Renderable): string {
  return new Renderer().render(value as unknown as never);
}
