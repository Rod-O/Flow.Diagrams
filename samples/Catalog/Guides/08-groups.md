# Groups

Frame a set of nodes in a group that moves them together and sizes itself around them, with padding per side, ports of its own, an HTML or SVG look, and other groups inside it.

## Making a group

{{source grouping Grouping.razor group}}

{{example grouping}}

`Diagram.Groups.Group(nodes)` creates a group with `Options.Groups.Factory` (a plain [GroupModel](xref:Flow.Diagrams.Core.Models.GroupModel) with 30 units of padding, unless you replace it) and adds it. You can also construct a `GroupModel` yourself and pass it to `Diagram.Groups.Add`. Groups live in `Diagram.Groups`, not `Diagram.Nodes`; the children stay in `Diagram.Nodes`, and each child's `Group` points to its group. A group is a node, so a link can end on the group itself, as above, or on one of its ports.

Users can group and ungroup with Ctrl+Alt+G once you turn it on (the binding is for the key `g`, matched case-sensitively, so with Shift or Caps Lock it does nothing); `Groups.Group` works without it:

{{source grouping Grouping.razor enable}}

The shortcut groups the selected nodes when there are two or more and none is in a group yet; when any selected node is in a group, it removes those groups instead. It only looks at selected nodes, so it cannot put a group inside another group. The rubber-band selection also selects nodes only, never groups or links.

## Ungrouping and deleting

| Call | The group | Its children | Links on the group and its ports |
|---|---|---|---|
| `Diagram.Groups.Remove(group)` | removed | released, stay in the diagram | removed |
| `Diagram.Groups.Delete(group)` | removed | removed, nested groups too | removed |
| `group.Ungroup()` | stays, empty, at its last size | released | kept |

The shortcut uses `Remove`, so ungrouping deletes the links that end on the group. The Delete key uses `Delete` on a selected group, so it deletes the children as well; `Options.Constraints.ShouldDeleteGroup` can refuse. Removing a child node from `Diagram.Nodes` takes it out of its group.

## Padding and size

A group's position and size are not yours to set: they are the bounds of its children plus the padding, recomputed whenever a child moves or changes size. The group grows and shrinks to follow. [GroupPadding](xref:Flow.Diagrams.Core.Models.GroupPadding) sets the padding per side (new in Flow.Diagrams); a tall top padding leaves room for a title.

{{source group-padding PaddedGroupsGroup.cs}}

{{example group-padding}}

Use `PaddingBySide`. The older `Padding` property is a single byte: the constructor's value, or the smallest side rounded when you passed a `GroupPadding`.

The group's size waits for its children's sizes. HTML nodes are measured after their first render, so until then the group has zero size. The children's area is the union of their `Position` and `Size`; if a child draws outside that box (an SVG symbol drawn around its origin, say), override `GetChildrenBounds`.

### The AutoSize trap

The constructor's `autoSize` argument does not control sizing. Sizing follows the children either way. `AutoSize` decides whether a child can be dragged on its own: with `false`, [DragMovablesBehavior](xref:Flow.Diagrams.Core.Behaviors.DragMovablesBehavior) skips selected nodes of that group. Clicking a child selects the child, not the group, so with `false` dragging a child moves nothing; the user moves the group by its frame.

## Custom group widgets

Register a widget for your group type with `Diagram.RegisterComponent<TGroup, TWidget>()`. The canvas draws only nodes that are not in a group; a group's children are drawn by the group's widget. Your widget must include [GroupNodes](xref:Flow.Diagrams.Components.GroupNodes), or the children disappear.

{{source group-padding PaddedGroupsWidget.razor}}

The default widget also draws the group's ports. If your group has ports, draw them in your widget with `PortRenderer`. Children are drawn inside the group's element, so they are always above their group and never interleave with models outside it; among themselves they follow their own `Layer` and `Order`. The default HTML group has the CSS classes `diagram-group default` (plus `selected`) for styling, as the examples on this page do.

## Group ports

`AddPort` works on a group as on any node. The ports are placed again whenever the group's size changes, and their links follow.

{{source group-ports GroupPorts.razor ports}}

{{example group-ports}}

## SVG groups

A [SvgGroupModel](xref:Flow.Diagrams.Models.SvgGroupModel) is drawn in the SVG layer instead of the HTML layer, so it exports and stacks with links. Its children must be SVG nodes too: an HTML node inside an SVG group would put a `div` inside the SVG. As with any [SVG node](examples/svg-shapes), the children need `ControlledSize = true`.

{{source svg-groups SvgGroupsModels.cs}}

{{source svg-groups SvgGroupsWidget.razor}}

See it running in [SVG groups](examples/svg-groups).

## Nested groups

A group can contain other groups. There is no shortcut for it; build the inner groups first, pass them as children of the outer one, and add every group to `Diagram.Groups` (the canvas draws only top-level groups, but each must be in the diagram to be selected and dragged).

{{source nested-groups NestedGroups.razor nest}}

{{example nested-groups}}

Moving a node resizes its group, which resizes the outer group in turn. `ObstacleRouter` routes around the children of a group, never around its frame.
