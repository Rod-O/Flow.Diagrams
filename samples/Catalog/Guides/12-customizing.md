# Customizing the look

Draw nodes, links and groups with your own Blazor components, define your own markers, and style what the library emits with CSS.

{{example pid}}

## Models and widgets

Every model is drawn by a component, its widget. To give a kind of node its own look, subclass a model, write a component for it and map one to the other with `RegisterComponent`:

{{source org-chart OrgChartNode.cs}}

{{source org-chart OrgChartWidget.razor}}

{{source org-chart OrgChart.razor register}}

The widget receives the model in a parameter named after its role: `Node`, `Group`, `Link`, `Label` or `Vertex` (controls get `Control` and `Model`). Type the parameter as your subclass. It draws in the model's own coordinates: (0, 0) is the node's top-left corner, because the renderer wraps the widget in an element already moved to the node's position. The wrapper also handles pointer events, so the widget does not wire up selection or dragging; it reads `Node.Selected` to draw the selected state.

How the mapping resolves ([BlazorDiagram](xref:Flow.Diagrams.BlazorDiagram)):

- An exact registration wins; otherwise a registration for a base class is used. Registering a widget for `NodeModel` therefore draws every node subclass that has no registration of its own, `SvgNodeModel` subclasses included.
- Registering the same model type twice throws unless you pass `replace: true`.
- With no registration, nodes fall back to `NodeWidget` (HTML) or `SvgNodeWidget` (a placeholder blue square), links to `LinkWidget`, groups to `DefaultGroupWidget`.

## SVG or HTML

Subclass [NodeModel](xref:Flow.Diagrams.Core.Models.NodeModel) for an HTML node: the widget emits HTML, and the renderer measures it, so the content decides the size. Subclass [SvgNodeModel](xref:Flow.Diagrams.Models.SvgNodeModel) for an SVG node: the widget emits SVG elements, which share a layer with the links, can be drawn over by links in a higher layer, and are what the [SVG exporter](guides/export) exports. The showcases all use SVG nodes.

For SVG nodes:

- Set `Size` in the model and `ControlledSize = true`, as `OrgChartNode` does. A measured SVG node grows on every render.
- In Razor, an SVG `<text>` directly inside `@if` or `@switch` fails to compile (RZ1023). Wrap it in `<g>`, as every showcase widget does.
- Style with attributes (`fill`, `stroke`) rather than CSS classes if the node will be exported.

The [HTML nodes example](examples/html-nodes) shows the HTML side: a card with form fields that stop pointer propagation so typing does not drag the node.

## Ports in a widget

Ports are not drawn for you. Put one [PortRenderer](xref:Flow.Diagrams.Components.Renderers.PortRenderer) per port in the widget; after it renders, it measures its element and the links attach to that spot. In an SVG widget, place a small shape where the link should meet the symbol; in an HTML widget, position the port element with CSS. The port's alignment says which way the link leaves, not where the port is: [the P&ID showcase](examples/pid) stores a point per port so pipes attach to the symbol's nozzles, and [the flowchart](examples/flowchart) moves the side ports of its slanted data shape inwards.

## Custom links

{{example custom-links}}

A link widget works the same way. Read the routed path from `Link.PathGeneratorResult` (`null` until the link has been routed), draw it, and draw the markers at the positions and angles the result gives:

{{source custom-links CustomLinksModel.cs model}}

{{source custom-links CustomLinksWidget.razor widget}}

{{source custom-links CustomLinks.razor register}}

What the default `LinkWidget` does that yours must do itself, if you need it: a wide transparent stroke to make the link easy to click, markers, vertices (draw a `LinkVertexRenderer` per vertex), and labels when `LabelsOnTop` is off. `LinkWidget` takes a `LinkModel`; a link type derived from `BaseLinkModel` directly needs its own widget.

Links the user draws by dragging from a port are created by `Options.Links.Factory`, which makes a plain `LinkModel`. To have them be your type, set the factory to create it.

## Custom groups

A group widget draws the frame and must also draw the children, with [GroupNodes](xref:Flow.Diagrams.Components.GroupNodes). Leave it out and the group's nodes disappear.

{{source svg-groups SvgGroupsWidget.razor}}

`SvgGroupModel` draws the group in the SVG layer, and padding per side is set with `GroupPadding`. See the [SVG groups](examples/svg-groups) and [group padding](examples/group-padding) examples.

## Custom markers

A [LinkMarker](xref:Flow.Diagrams.Core.Models.LinkMarker) is SVG path data in a frame where the link's end is the origin and the link arrives along the positive x axis, plus its width along the link. The link's line stops that far before the end so it does not show through the marker. It is filled with the link's colour.

{{source markers Markers.razor custom}}

`LinkMarker.NewArrow`, `NewCircle`, `NewRectangle` and `NewSquare` build sized versions of the built-in shapes.

## CSS

The library ships two stylesheets in `_content/Flow.Diagrams/` (minified as `.min.css`):

- `style.css` is required. It positions the layers, turns pointer events on for nodes, groups, links and controls, and sets the cursors. Without it the canvas does not work.
- `default.styles.css` is the look of the default widgets: `default-node`, its ports, the default group, the HTML link label. Drop it if every widget is yours, or override its rules.

```html
<link href="_content/Flow.Diagrams/style.min.css" rel="stylesheet" />
<link href="_content/Flow.Diagrams/default.styles.min.css" rel="stylesheet" />
```

Classes the renderers put on their wrapper elements, for your selectors:

| Element | Classes |
|---|---|
| Canvas | `diagram-canvas`; layers `diagram-svg-layer`, `diagram-html-layer` |
| Node | `diagram-node`, plus `selected`, `locked`, `grouped`, and the node's `CssClass` |
| Group | `diagram-group`, plus `selected`, `locked`, `default` when drawn by `DefaultGroupWidget`, and the group's `CssClass` |
| Link | `diagram-link`, plus `attached` once both ends are attached, and the link's `CssClass`; `diagram-link-halo` on halos, `diagram-link-flow` on a flowing stroke |
| Port | `diagram-port`, its alignment in lower case (`top`, `bottomright`), `has-links`, plus the `Class` you pass |
| Label | `diagram-link-label`; `diagram-link-labels-layer` when `LabelsOnTop` |
| Vertex | `diagram-link-vertex` |
| Control | `diagram-control`, the control's type name, and `executable` for executable controls |

## Showcases to read

- [Cooling loop P&ID](examples/pid): symbols with ports where the pipes attach, a custom flow marker, process lines in a higher layer than signals, halos.
- [Network topology](examples/network-topology): one node class drawing many device kinds with `@switch`, names placed beside or under the symbol.
- [Flowchart](examples/flowchart): shapes per kind, ports moved to fit a slanted shape.
- [State machine](examples/state-machine): round pseudo-states, corner ports (`TopLeft`, `TopRight`).
- [Org chart](examples/org-chart): the smallest complete node and widget pair.

Controls have widgets too; see [Behaviors and interaction](guides/behaviors-and-interaction).
