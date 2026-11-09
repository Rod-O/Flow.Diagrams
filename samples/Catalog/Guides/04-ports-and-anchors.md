# Ports and anchors

Attach links where you want them: put ports on any of eight alignments, place and size them in your widget, attach links to a node's outline instead, and decide which links the user may draw.

## Ports

A [`PortModel`](xref:Flow.Diagrams.Core.Models.PortModel) is a point on a node where links attach. `node.AddPort(PortAlignment.Right)` creates one and returns it; `node.AddPort(new MyPort(node, ...))` adds a port of your own type, which must be created with that node as its parent. A node may have several ports with the same alignment, but `GetPort(alignment)` returns only the first, so keep references or use `node.Ports`.

`new LinkModel(sourcePort, targetPort)` attaches each end with a [`SinglePortAnchor`](xref:Flow.Diagrams.Core.Anchors.SinglePortAnchor). The link follows the ports when the nodes move.

## Alignment

[`PortAlignment`](xref:Flow.Diagrams.Core.Models.PortAlignment) has eight values: the four sides (`Top`, `Right`, `Bottom`, `Left`) and the four corners (`TopRight`, `BottomRight`, `BottomLeft`, `TopLeft`). The alignment is a direction, not a position:

- the anchor attaches the link at the point of the port that faces that direction;
- the orthogonal and obstacle routers and the smooth path generator leave and enter the node in that direction; from a corner port, `OrthogonalRouter` and `ObstacleRouter` leave along one of the two sides ([corner ports example](examples/corner-ports));
- the port's element gets the alignment as a CSS class (`top`, `bottomright`...), which is how the default stylesheet places ports on the default widget.

{{example ports}}

## Position and size

A port's `Position` (top-left corner) and `Size` are not set by you: [`PortRenderer`](xref:Flow.Diagrams.Components.Renderers.PortRenderer) reads them from the rendered element after its first render, converts them to diagram units and marks the port `Initialized`. Until then a link to the port has no end point and is not drawn. After that the port moves with its node by the same offset, without being measured again.

So a port goes where your widget draws it:

- **HTML widget.** `PortRenderer` renders a `div` with the classes `diagram-port`, the alignment and `has-links` when links are attached. Position it with CSS, as the card in [Nodes](guides/nodes) does (`.html-card .diagram-port.right { right: 0; top: 50%; }`). Its CSS box is its size.
- **SVG widget.** `PortRenderer` renders a `g`; draw the port inside it at the coordinates you want, as `SvgShapesWidget.razor` in [Nodes](guides/nodes) does with a small circle. The bounding box of what you draw is its size.

If your widget moves a port while the node keeps its size, call `node.ReinitializePorts()` so it is measured again.

## Anchors

An anchor is where one end of a link attaches, asked for its position every time the link is drawn. Any two can be combined with `new LinkModel(sourceAnchor, targetAnchor)`.

| Anchor | Attaches at |
|---|---|
| [`SinglePortAnchor`](xref:Flow.Diagrams.Core.Anchors.SinglePortAnchor) | The point of the port's shape facing its alignment. `UseShapeAndAlignment = false` uses the port's box instead; `MiddleIfNoMarker` uses the port's centre at an end without a marker. |
| [`ShapeIntersectionAnchor`](xref:Flow.Diagrams.Core.Anchors.ShapeIntersectionAnchor) | The point where the line from the other end to the node's centre crosses the node's outline. |
| [`DynamicAnchor`](xref:Flow.Diagrams.Core.Anchors.DynamicAnchor) | Whichever of several candidate points on the node is closest to the other end. |
| [`LinkAnchor`](xref:Flow.Diagrams.Core.Anchors.LinkAnchor) | A point along another link. |
| [`PositionAnchor`](xref:Flow.Diagrams.Core.Anchors.PositionAnchor) | A fixed point in diagram units, attached to nothing. A link being dragged ends on one. |

### Links without ports

`new LinkModel(sourceNode, targetNode)` attaches both ends with a `ShapeIntersectionAnchor`, so the link points at each node's centre and stops at its outline, wherever the nodes move. The outline is the node's `GetShape()`, a rectangle by default; a round node must override it.

{{source portless-links PortlessLinksRoundNode.cs}}

{{example portless-links}}

Without ports there is nothing to drag a new link from, so the example adds a `DragNewLinkControl` to each node: the handle on the right of a selected node.

### Dynamic anchors

A `DynamicAnchor` takes candidate positions from [`IPositionProvider`](xref:Flow.Diagrams.Core.Positions.IPositionProvider)s and uses the one closest to the other end of the link. [`BoundsBasedPositionProvider`](xref:Flow.Diagrams.Core.Positions.BoundsBasedPositionProvider) gives a point as fractions of the node's box (`(0.5, 0)` is the middle of the top edge) plus an offset; [`ShapeAnglePositionProvider`](xref:Flow.Diagrams.Core.Positions.ShapeAnglePositionProvider) gives the point of the outline at an angle (0 right, 90 down).

{{source dynamic-anchors DynamicAnchors.razor}}

{{example dynamic-anchors}}

## Which links the user may draw

When the user drags from a port, `Options.Links.Factory` creates the link; when the link is dropped on a port or node, the source's `CanAttachTo(target)` decides whether it attaches, and `Options.Links.TargetAnchorFactory` makes the target anchor. A link dropped where it may not attach is removed (with `Links.RequireTarget`, the default). While snapping is on, ports the source may not attach to are not snap candidates.

The defaults: a port links to another port that is not itself, is on a different node, and is not locked, nor is its node; a node links to another node, never to a port.

To set your own rules, override `CanAttachTo` in a port subclass, and return `null` from the factory where no drag should start at all:

{{source port-rules PortRulesPort.cs}}

{{source port-rules PortRules.razor factory}}

{{example port-rules}}

These rules guard only what the user draws. Links you add in code are not checked.

## Reconnecting to the closest ports

`ReconnectLinksToClosestPorts`, an extension method in `Flow.Diagrams.Algorithms`, moves both ends of every port-to-port link to the closest pair of ports on the same two nodes ([example](examples/reconnect-to-closest-ports)). Distances are measured between the ports' `Position`s, so call it after the diagram has rendered: unmeasured ports all sit where they were created.

## Traps

- `Links.Added` fires when the user starts dragging a link, while its target is still a `PositionAnchor`. If it is dropped on nothing it is removed again. To act on finished links, handle the link's `TargetAttached` event or check `IsAttached`.
- `ReconnectLinksToClosestPorts` does not ask `CanAttachTo` and ignores `Locked`: it can move a link onto a locked port or break your rules.
- `Shapes.Circle` uses the node's width as the diameter and ignores its height; use `Shapes.Ellipse` for an oval.
- `LinkAnchor` distances: from 0 to 1 is a fraction of the path, above 1 a length in diagram units, negative a length from the end; exactly 1 is the end of the path.
