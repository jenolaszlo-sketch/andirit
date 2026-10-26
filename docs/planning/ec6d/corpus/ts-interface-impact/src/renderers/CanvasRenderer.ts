import { SvgRenderer as BaseRenderer } from "./SvgRenderer";

export class CanvasRenderer extends BaseRenderer {
  renderToCanvas(): string {
    return "canvas";
  }
}
