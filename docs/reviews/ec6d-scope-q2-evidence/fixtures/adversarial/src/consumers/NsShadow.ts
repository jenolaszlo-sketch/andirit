import type { Renderable as Drawable } from "../contracts/Renderable";

export namespace NS {
  export interface Drawable {
    own: string;
  }

  export class NsC implements Drawable {
    own = "o";
  }
}
