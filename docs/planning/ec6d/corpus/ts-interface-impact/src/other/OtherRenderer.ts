import type { Renderable } from "./Renderable";

export class OtherRenderer implements Renderable {
  snapshot(): string {
    return "other";
  }
}
