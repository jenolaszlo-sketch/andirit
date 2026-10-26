export interface Renderable {
  render(context: RenderContext): string;
}

export interface RenderContext {
  theme: string;
}
