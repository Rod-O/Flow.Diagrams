- `SelectableModel.CssClass`: extra CSS classes the renderers add to a node's, group's or link's wrapper element, so an
  app can style and animate models with plain CSS. Setting it redraws the model. `style.css` ships one: `diagram-pulse`,
  a highlight glow (colour from `--diagram-pulse-color`). If you serve your own copy of `style.css`, copy the new rules.
- `LinkModel.Flow` and `LinkFlow(Speed, Dash, Gap, Reverse)`: marching dashes along a link, in its direction, to show
  what flows through it. Pure CSS (one keyframes rule in `style.css`), no work in .NET or JavaScript per frame; still
  under `prefers-reduced-motion: reduce`; halos, markers and labels stay solid; the SVG export draws the link solid. A
  link without flow renders exactly as before.
