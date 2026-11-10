# Nodes

Draw nodes your own way: choose between HTML and SVG, pair a node model with a Blazor component, decide who owns the size, and add nodes while the diagram runs or from a palette.

## Model and widget

A node is two things: a model that holds its data and a component, the widget, that draws it. A plain [`NodeModel`](xref:Flow.Diagrams.Core.Models.NodeModel) is drawn by the default widget, a grey box that shows `Title` (the text "Title" when it is `null`) and the node's ports. `Title` means nothing to any other widget.

For a node of your own, subclass the model, write a component with a parameter named `Node`, and pair them once per diagram with `Diagram.RegisterComponent<TModel, TWidget>()`. A subclass without its own registration uses the registration of its nearest registered base class. The renderer passes the model in the `Node` parameter, so the name matters; a component without it fails to render.

{{source html-nodes HtmlNodesNode.cs}}

{{source html-nodes HtmlNodesCard.razor markup}}

{{example html-nodes}}

Three things in that widget are not decoration:

- **One `PortRenderer` per port.** [`PortRenderer`](xref:Flow.Diagrams.Components.Renderers.PortRenderer) measures where the port is drawn, and links attach to that measurement. A port your widget does not render is never measured, and links to it are never drawn.
- **`@onpointerdown:stopPropagation` on inputs.** Without it, a click in a text box selects the node and starts a drag.
- **`Node.Selected`.** The widget draws the selection itself; the library only adds a `selected` class to its wrapper.

## HTML or SVG

A `NodeModel` is drawn in the canvas's HTML layer, inside a `div`. A [`SvgNodeModel`](xref:Flow.Diagrams.Models.SvgNodeModel) is drawn in the SVG layer, inside a `g` translated to the node's position, and its widget must output SVG elements. The choice decides more than the markup:

| | HTML node (`NodeModel`) | SVG node (`SvgNodeModel`) |
|---|---|---|
| Content | Any HTML: inputs, text that wraps, CSS layout. | SVG shapes and `text`. |
| SVG export | Left out; `DiagramSvgExporter` lists it in `HtmlModels`. | Exported as vectors. |
| Drawing order | The whole HTML layer is above (or below) all links; `Layer` and `Order` sort HTML nodes among themselves only. | Shares the SVG layer with links, so `Layer` and `Order` put links above or below nodes one by one. |
| Labels on top | `Links.LabelsOnTop` cannot lift labels above HTML nodes. | Labels are drawn above SVG nodes. |
| Size | Measured from the rendered `div` by default. | Set `ControlledSize = true` and give it a `Size`. |

Pick SVG when the diagram is exported or printed, or when links and nodes must interleave (see [the layers example](examples/layers)). Pick HTML for nodes that are forms or rich text.

{{source svg-shapes SvgShapesNode.cs}}

{{source svg-shapes SvgShapesWidget.razor}}

{{example svg-shapes}}

The widget draws in the node's own coordinates: `(0, 0)` is the node's top-left corner and `Node.Size` its extent. Ports are placed by drawing them where you want them; [Ports and anchors](guides/ports-and-anchors) covers that.

## Size: measured or controlled

`Size` is `null` until it is known. Until then the node has no bounds: links that attach to its shape are not drawn, and `ZoomToFit` ignores it.

- **Measured** (the default). After the first render the renderer watches the element's size and stores it, converted to diagram units. When the content changes size, `Size` follows, and the ports are measured again.
- **Controlled.** With `ControlledSize = true` you set `Size` and the renderer never measures. `ControlledSize` is init-only: set it in the object initializer or the constructor.

SVG nodes need a controlled size. A measured SVG node's box includes everything drawn, ports sticking out of the shape included; a widget that draws to `Node.Size` then grows, is measured bigger, and grows again on every render.

To resize a controlled node, call `SetSize(size)`: it sets `Size`, which redraws the node and re-routes the links attached to it, and calls `ReinitializePorts()` so the ports are measured at their new places, as the renderer does when it measures a node. Setting `Size` alone does not re-measure the ports.

## Adding and removing at run time

The diagram redraws from the model: `Diagram.Nodes.Add` and `Remove` work at any time, and removing a node removes its links too. Setting a node's properties redraws it; call `node.Refresh()` only for state the diagram cannot see, such as a property you added.

A node, port and group mark their changes as follows. A mark is `Appearance` (redraw) or `Geometry` (redraw, and recompute what depends on the shape: links, group bounds).

| Member | Marks |
|---|---|
| `Model.Locked` | Appearance |
| `SelectableModel.Selected` | Appearance |
| `MovableModel.Position` (setter) | Geometry |
| `NodeModel.Title` | Appearance |
| `NodeModel.Size` (still raises `SizeChanged`) | Geometry |
| `PortModel.Position`, `PortModel.Size` | Geometry |

`NodeModel.RefreshAll` and `RefreshLinks` are obsolete: the marks above replace them.

{{example dynamic-nodes}}

## Drag and drop from a palette

The palette is plain HTML with the browser's drag events: `draggable="true"` and `@ondragstart` on each item, and on the element around the canvas `@ondragover:preventDefault` (without it the browser refuses the drop) and `@ondrop`.

{{source drag-and-drop DragAndDrop.razor markup}}

The drop handler turns the pointer's client position into diagram units with `GetRelativeMousePoint`, which accounts for where the canvas is on the page, the pan and the zoom.

{{source drag-and-drop DragAndDrop.razor drop}}

{{example drag-and-drop}}

The point becomes the node's top-left corner. To centre the node under the pointer, subtract half its size; for a measured node that size is unknown at drop time, so use the size you expect.

## Traps

- **SVG `text` in `@if` or `@switch`.** Razor fails with RZ1023 on a `<text>` element directly inside a code block, because `<text>` is its own keyword there. Wrap it in `<g>`, as the [process symbols example](examples/svg-symbols) does.
