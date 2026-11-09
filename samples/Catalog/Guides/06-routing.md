# Routing

Choose how links find their way between ports: straight through, at right angles, or at right angles around every node. Tune the obstacle router's costs, and re-route at the right moments.

## Three routers

A router decides the points a link passes through between its two ends; the [path generator](guides/links) then draws a line through them. Set one for every link with `Options.Links.DefaultRouter`, or per link with `Router`.

{{source routers Routers.razor router}}

{{example routers}}

| Router | Route | Avoids | Uses vertices |
|---|---|---|---|
| [NormalRouter](xref:Flow.Diagrams.Core.Routers.NormalRouter) (default) | Through the link's vertices, or direct when it has none. | Nothing | Yes |
| [OrthogonalRouter](xref:Flow.Diagrams.Core.Routers.OrthogonalRouter) | Horizontal and vertical segments, leaving each port along its side, on a grid built around the two end nodes. | Its own two nodes | No |
| [ObstacleRouter](xref:Flow.Diagrams.Core.Routers.ObstacleRouter) | Horizontal and vertical segments around every visible node, choosing the cheapest of length, bends, crossings and overlaps. | Every node | No |

`OrthogonalRouter(shapeMargin, globalMargin)` keeps `shapeMargin` (10) clear around the two end nodes and stays within `globalMargin` (50) of them. `ObstacleRouter` is new in Flow.Diagrams.

Both right-angle routers need each end on a port, with a [SinglePortAnchor](xref:Flow.Diagrams.Core.Anchors.SinglePortAnchor), on a node that has a size. When that is not the case they hand the link to their fallback router (`NormalRouter` unless you pass another): a link between two nodes without ports, the loose end of a link being dragged, and an HTML node that has not been measured yet. HTML nodes get their size after their first render, so their links are drawn straight for a moment and routed when the size arrives.

## Which path generator

Use [StraightPathGenerator](xref:Flow.Diagrams.Core.PathGenerators.StraightPathGenerator) with both right-angle routers, with a `radius` if you want rounded corners. `SmoothPathGenerator` draws a spline through the route points, which turns a right-angled route into a wobble. `ObstacleRouter` also measures crossings and overlaps on the route's straight segments, so with a smooth line it weighs a line you do not see, and crossing halos are not drawn for smooth links at all. `NormalRouter` works with either generator.

## Routing around nodes

{{source obstacle-routing ObstacleRouting.razor defaults}}

{{example obstacle-routing}}

`ObstacleRouter` lays a grid of lines along the node edges (inflated by the margin), through the middle of every channel between them and around everything, then searches it for the route with the lowest total cost. Costs are in diagram units, so raising one makes the router pay more length to avoid that thing. They are set in [ObstacleRouterOptions](xref:Flow.Diagrams.Core.Routers.ObstacleRouterOptions):

| Option | Default | Effect |
|---|---|---|
| `Margin` | 10 | Clearance kept around every node, and the shortest straight run out of a port. |
| `BendCost` | 30 | What one bend costs. |
| `CrossCost` | 200 | What crossing a link of another net costs ([crossing cost](examples/crossing-cost)). |
| `OverlapCostPerUnit` | 40 | Per unit of length run on top of a link of another net. |
| `HugCostPerUnit` | 20 | Per unit of length run alongside a link of another net, closer than the margin. |
| `MinStub` | 20 | Straight run kept before each end that carries a marker (`SourceMarker` or `TargetMarker`), so the arrowhead sits on a line ([arrow stub](examples/arrow-stub)). |
| `OuterRing` | 30 | Distance of the outer ring of grid lines from everything, so a route can always go around. |

The options are fixed when the router is created. To change them, create a new router, make it the default (or the links' router), and route again:

{{source obstacle-routing ObstacleRouting.razor options}}

Two ports that face each other on one line, with nothing in between, get a straight line with no bends. When no grid path exists at all the router draws a plain dogleg, which is visibly wrong rather than missing.

## When links are re-routed

A link is routed when it refreshes: when it is added, when its own nodes or ports move or resize, and when you call `link.Refresh()`. Two consequences:

- Moving a node into another link's way does not re-route that link. It is routed again only when one of its own ends moves.
- Each link is routed against the routes the other links have at that moment, so the result depends on the order. Links routed one by one as they are added often box each other in.

`RouteAll(diagram, reroutePasses = 2)` fixes both: it refreshes every link whose router is this instance (its own `Router`, or the default router) once, then again for each pass, in one batch. Call it once the nodes have their sizes, and after the user moves something; a node raises `Moved` once when a drag ends (see [MovableModel](xref:Flow.Diagrams.Core.Models.Base.MovableModel)). While a node is being dragged, its own links are re-routed on every pointer move.

## Nets

Links that share a port are one net, like the branches of a tee. They may share a line and are not charged for crossing or running on top of each other; links of other nets are kept apart.

{{source shared-nets SharedNets.razor net}}

{{example shared-nets}}

To define nets differently (for example, every link carrying the same signal), derive from `ObstacleRouter` and override `SameNet`. Override `GetObstacles` to change what routes keep clear of; by default it is every visible node with a size, and a group's frame is not an obstacle while its children are. `LinkHalosBehavior` has its own `SameNet` with the same default, so override both if you change one.

## Corner ports

Ports can sit on a node's corners (`PortAlignment.TopLeft`, `TopRight`, `BottomLeft`, `BottomRight`). Both right-angle routers leave a corner port horizontally, to the right for the right corners and to the left for the left ones, and arrive the same way. See [corner ports](examples/corner-ports).
