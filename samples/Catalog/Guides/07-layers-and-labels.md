# Layers and labels

Decide what is drawn over what: put links and nodes in layers, reorder them within a layer, break crossings with halos, and keep labels readable. The same settings decide what an SVG export looks like.

## Layer and Order

Every node, link and group has a `Layer` and an `Order` ([SelectableModel](xref:Flow.Diagrams.Core.Models.Base.SelectableModel)). The diagram draws them back to front by layer, then by order within the layer; `Diagram.OrderedSelectables` is that list. `Layer` is 0 unless you set it. A model added with `Order` 0 gets the highest order so far plus one, so by default whatever was added last is in front. Changing either property re-sorts the drawing at once. `Layer` is new in Flow.Diagrams.

{{source layers Layers.razor layer}}

{{example layers}}

Use `Layer` for kinds of things (signal lines under process lines, annotations over everything) and `Order` for one item against its neighbours.

The canvas has two drawing surfaces, and the sort applies within each. The SVG layer holds links, [SvgNodeModel](xref:Flow.Diagrams.Models.SvgNodeModel) nodes and [SvgGroupModel](xref:Flow.Diagrams.Models.SvgGroupModel) groups; the HTML layer holds every other node and group and is drawn above the SVG layer. So an HTML node is in front of every link whatever their layers: in the example above, layer 2 puts the signals over the process lines, not over the nodes. [BlazorDiagramOptions](xref:Flow.Diagrams.Options.BlazorDiagramOptions) has `LinksLayerOrder` and `NodesLayerOrder`, the CSS `z-index` of the two surfaces, which can only put all links above all HTML nodes. To mix links and nodes freely, use SVG nodes.

## Bring to front, send to back

`Diagram.SendToFront(model)` gives the model an order above every other; `Diagram.SendToBack(model)` puts it behind every other and renumbers all orders. Neither changes `Layer`, so a model never leaves its layer this way. Both do nothing for a model that is not in the diagram.

{{source draw-order DrawOrder.razor order}}

{{example draw-order}}

## Crossing halos

Where two lines cross, a reader cannot tell whether they join. A halo answers it: where a link crosses a link drawn below it, a short stroke in the background colour under the upper link breaks the lower one, so the crossing reads as a bridge. Which link bridges follows the drawing order.

{{source crossing-halos CrossingHalos.razor halos}}

{{example crossing-halos}}

[LinkHalosBehavior](xref:Flow.Diagrams.Core.Behaviors.LinkHalosBehavior) is not registered by default; register it as above. It is new in Flow.Diagrams. What to know:

- Only links drawn with `StraightPathGenerator` get halos, and only where a horizontal segment crosses a vertical one. In practice that means `OrthogonalRouter` or `ObstacleRouter`; diagonal links get none.
- `Options.Links.HaloColor` is `"white"`. Set it to your canvas background, or every break shows as a white patch.
- Links that share a port are one net and never break each other. Override the behaviour's `SameNet` to change that, and `Clearance` (2) to widen the break.
- Unregistering the behaviour leaves the halos it set. The example's checkbox clears them with `link.SetHalos(Array.Empty<LinkHalo>())`.

## Labels on top

A link's labels are drawn with the link, so a link drawn later can run over them. `Options.Links.LabelsOnTop` draws every label after every link and SVG node, so no line covers one; HTML nodes are still above them, since they are on the other surface. `Options.Links.SvgLabels` draws labels as SVG text over a halo in `HaloColor` instead of HTML.

{{example labels-on-top}}

Both options are read when the canvas and each link draw. Links redraw only when they change, so after changing either option at runtime refresh the diagram and every link:

{{source labels-on-top LabelsOnTop.razor options}}

## Why this matters for export and dense drawings

[DiagramSvgExporter](xref:Flow.Diagrams.Export.DiagramSvgExporter) draws in the same order as the canvas: `Layer` and `Order` are the export's stacking order, halos are part of each link and are exported with it, and `LabelsOnTop` is honoured. It exports only what is drawn as SVG. HTML nodes and groups are left out and listed in the result's `HtmlModels`; HTML labels stay in the file as `foreignObject`, which most SVG-to-PDF converters drop.

For a dense drawing that has to print well, that adds up to: SVG nodes, a right-angle router with `StraightPathGenerator`, halos with `HaloColor` set to the page colour, `SvgLabels` and `LabelsOnTop`, and layers to separate kinds of lines. The [P&ID showcase](examples/pid) puts these together.
