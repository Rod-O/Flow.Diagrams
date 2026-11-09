# Links

Connect nodes with links, let users draw new ones by dragging, and set how a link looks: markers, labels, vertices, colour, width and the shape of the line.

## A link between two ports

A [LinkModel](xref:Flow.Diagrams.Core.Models.LinkModel) joins two anchors. The common case is port to port: the constructor that takes two ports wraps each in a [SinglePortAnchor](xref:Flow.Diagrams.Core.Anchors.SinglePortAnchor). Add the link to `Diagram.Links`; until it is in a diagram it has no route and draws nothing.

{{source link-styles LinkStyles.razor create}}

The other constructors take two nodes, and the link then ends where it crosses each node's outline ([links without ports](examples/portless-links)), or any two anchors ([dynamic anchors](examples/dynamic-anchors)).

`AddLabel`, `AddVertex`, `InsertVertex`, `SetSource` and `SetTarget` do not redraw the link. When you change a link that is already in the diagram, call `link.Refresh()` afterwards.

## Drawing links by dragging

[DragNewLinkBehavior](xref:Flow.Diagrams.Core.Behaviors.DragNewLinkBehavior) is registered by default: pressing the left button on a port starts a link whose loose end follows the pointer. A link that starts on a port attaches only to another port, on a different node, where neither the port nor its node is locked; releasing over a node or the empty canvas counts as releasing on nothing. Locked ports, and the ports of locked nodes, start no link.

What happens next is set in `Diagram.Options.Links` ([DiagramLinkOptions](xref:Flow.Diagrams.Core.Options.DiagramLinkOptions)):

| Option | Default | Effect |
|---|---|---|
| `RequireTarget` | `true` | A link released on nothing is removed. With `false` it stays, its end on a free point, and `IsAttached` is `false`. |
| `EnableSnapping` | `false` | While dragging, the end jumps to the nearest port within `SnappingRadius`, so the user does not have to release exactly on the port. |
| `SnappingRadius` | `50` | In diagram units. Zero or less throws. |
| `Factory` | a `LinkModel` | Creates the link when a drag starts. Return `null` to refuse a link from that port; return your own link type to give new links your defaults. |
| `TargetAnchorFactory` | port or node anchor | Creates the anchor for the end that was dropped. |

{{source snapping Snapping.razor snapping}}

When a dragged link attaches, the link raises `TargetAttached` (`SourceAttached` when an `ArrowHeadControl` moved its source end). That is the event to listen to if you store user-drawn links somewhere; `Diagram.Links.Added` fires earlier, when the drag starts and the link has no target yet.

## Markers

A [LinkMarker](xref:Flow.Diagrams.Core.Models.LinkMarker) is an SVG path drawn at an end of the link, filled in the link's colour. `LinkMarker.Arrow`, `Circle` and `Square` are 10 units long; `NewArrow`, `NewCircle`, `NewRectangle` and `NewSquare` make other sizes; the constructor takes any path.

{{source markers Markers.razor markers}}

A custom path is drawn in a frame where x runs along the link towards its end and y across it. The line stops `Width` units before the anchor so it does not show through the marker: x = 0 is where the line stops, and x = `Width` is the anchor itself, where an arrowhead's tip belongs.

## Labels

`AddLabel(content, distance, offset)` puts text on the link. The distance says where along the path:

| Distance | Position |
|---|---|
| `null` | The link's labels are spread evenly along it. |
| 0 to 1 | A fraction of the path's length; exactly 1 is the end, not one unit. |
| above 1 | That many diagram units from the start. |
| below 0 | That many diagram units back from the end. |

The offset is then added as a plain x/y shift in diagram units, not across the link: `new Point(0, -20)` moves the label up whatever the link's direction. A label is centred on the resulting point.

{{source labels Labels.razor labels}}

{{example labels}}

By default a label is HTML in a `foreignObject` inside the link's SVG, styled by the `default-link-label` CSS class. With `Options.Links.SvgLabels = true` it is drawn as SVG text over a halo in `Options.Links.HaloColor`. Choose SVG labels if you export: [DiagramSvgExporter](xref:Flow.Diagrams.Export.DiagramSvgExporter) keeps HTML labels as `foreignObject`, which most SVG-to-PDF converters drop, and lists them in the export's `HtmlModels`. A widget you register for your own `LinkLabelModel` subclass is used in both modes, so it must produce HTML or SVG to match the setting.

A label is drawn with its link, so a link drawn later can cover it. [Layers and labels](guides/layers-and-labels) explains `LabelsOnTop`.

## Vertices

A vertex is a waypoint the link passes through. Add them in code, or set `Segmentable = true` so that pressing on the link creates a vertex where the pointer is and starts dragging it (the default is `false`).

{{source vertices Vertices.razor vertices}}

{{example vertices}}

Two traps:

- Double-clicking a vertex removes it through `RemoveVertex`, which raises the link's `VertexRemoved`. Pressing on a segmentable link creates a vertex through `InsertVertex`, which raises the link's `VertexAdded`. `AddVertex` raises it too; editing `Vertices` directly raises neither event.
- Only [NormalRouter](xref:Flow.Diagrams.Core.Routers.NormalRouter) passes through vertices. `OrthogonalRouter` and `ObstacleRouter` ignore them, so with those routers the default link widget draws no vertex handles and a segmentable link creates none. A custom router says which it does with `Router.FollowsVertices`.

## Colour and width

{{source link-styles LinkStyles.razor styles}}

`Color` and `SelectedColor` take any SVG colour; when `null` they fall back to `DefaultColor` (black) and `DefaultSelectedColor` (`rgb(110, 159, 212)`) in [BlazorDiagramLinkOptions](xref:Flow.Diagrams.Options.BlazorDiagramLinkOptions). Markers and vertex handles use the same colours. `Width` is in diagram units (2 by default) and scales with zoom. These three are properties of `LinkModel`; a link type that derives from `BaseLinkModel` directly needs its own widget, and without one its rendering throws a `DiagramsException` that names the type. `Flow` marches dashes along the stroke to show what flows; see [Animation](guides/animation).

## The shape of the line

A path generator turns the route into the SVG path. Set one for all links with `Options.Links.DefaultPathGenerator`, or per link with `PathGenerator`.

{{source path-generators PathGenerators.razor generators}}

{{example path-generators}}

- [SmoothPathGenerator](xref:Flow.Diagrams.Core.PathGenerators.SmoothPathGenerator) is the default. A link with no route points is one curve that leaves each port along the port's side; with route points it is a spline through all of them, which wobbles on a right-angled route.
- [StraightPathGenerator](xref:Flow.Diagrams.Core.PathGenerators.StraightPathGenerator) draws straight segments. `radius` rounds the corners, limited to half of each segment next to the corner.

Which generator goes with which router is covered in [Routing](guides/routing).
