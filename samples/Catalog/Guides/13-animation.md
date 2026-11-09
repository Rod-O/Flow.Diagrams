# Animation

Animate with CSS: give a model a class and style it, or set a link's flow and its dashes march along it. Nothing runs in .NET or JavaScript per frame.

{{example link-flow}}

## Classes on models

`CssClass` on a node, group or link ([SelectableModel.CssClass](xref:Flow.Diagrams.Core.Models.Base.SelectableModel.CssClass), new in Flow.Diagrams) adds your classes, space-separated, to the model's wrapper element, next to the ones the library sets (`selected`, `locked`, ...). Setting it redraws the model, so a class can follow state: `tripped` on a breaker, `offline` on a server.

```css
.diagram-node.tripped { outline: 2px solid #c0392b; }
.diagram-link.offline path { stroke-opacity: 0.3; }
```

The class is on the wrapper: a `div` for an HTML node or group, a `g` for an SVG one and for every link. Inside a link's `g` are its halos, its stroke, the transparent selection helper, its markers and its labels, so a selector that styles only the stroke has to name your widget's elements, or use a widget of your own.

## A pulse to highlight

`diagram-pulse` is built in: a glow that swells and fades every 1.2 seconds for as long as the class is there. Set `--diagram-pulse-color` to change its colour (amber by default).

{{source link-flow FlowingLinks.razor pulse}}

It animates a CSS `drop-shadow` filter, which follows what the node draws, a card or a symbol.

## Flow along a link

[LinkModel.Flow](xref:Flow.Diagrams.Core.Models.LinkModel.Flow) takes a [LinkFlow](xref:Flow.Diagrams.Core.Models.LinkFlow): `Speed` in diagram units per second (40 by default), `Dash` and `Gap` lengths in diagram units (8 and 6), and `Reverse` to run from target to source. `null`, the default, is a plain stroke. Setting it redraws the link.

{{source link-flow FlowingLinks.razor flow}}

The default link widget dashes the stroke with `stroke-dasharray` and gives it the class `diagram-link-flow` and two custom properties inline, the dash period and the time to move one period at that speed. One `@keyframes` rule in `style.css` moves `stroke-dashoffset` by one period, so every link shares it. Halos, markers, the selection helper and labels are separate elements and stay solid. `Speed = 0` draws still dashes, with neither the class nor the properties. Dashes scale with zoom, like `Width`.

A link without flow gets no extra attribute or element. A flowing link costs the browser a repaint of its stroke every frame; hundreds of them on screen at once are noticeable on a slow machine.

A link type of your own, drawn by your own widget, does not get the flow; copy the three attributes from `LinkWidget` if you want it.

## Reduced motion

Under `prefers-reduced-motion: reduce` the stylesheet stops both animations: flowing links keep their dashes, still, and `diagram-pulse` becomes a steady glow. The information stays; only the motion goes.

## Export

[DiagramSvgExporter](xref:Flow.Diagrams.Export.DiagramSvgExporter) produces a still document, so a flowing link is exported as a solid stroke, as if `Flow` were `null`. `CssClass` is kept on the exported wrappers; the document carries none of the library's styles, so a class does nothing there unless you pass rules for it in `SvgExportOptions.Css`.

## Not supported yet

- Animated zoom and pan (`ZoomToFit` and `SetPan` jump).
- Transitions when a node moves or a link reroutes: positions change in one step.
