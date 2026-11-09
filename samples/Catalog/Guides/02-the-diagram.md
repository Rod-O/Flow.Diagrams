# The diagram

Configure a diagram, move its viewport from code, convert between screen and diagram coordinates, react to what the user does, and keep parts of it from being changed.

## What a diagram holds

A [`BlazorDiagram`](xref:Flow.Diagrams.BlazorDiagram) holds `Nodes`, `Links` and `Groups`: read-only lists you change with `Add` (which returns the item), `Remove` and `Clear`. A group is in `Groups` only, not in `Nodes`. Removing a node also removes the links attached to it and its ports. `OrderedSelectables` lists all three in drawing order, back to front (by `Layer`, then `Order`); `SendToFront` and `SendToBack` change it.

## Options

A [`BlazorDiagramOptions`](xref:Flow.Diagrams.Options.BlazorDiagramOptions) (namespace `Flow.Diagrams.Options`) goes to the constructor. Its groups (`Zoom`, `Links`, `Groups`, `Constraints`, `Virtualization`) are fixed instances: set their properties with nested initializers, or later through `Diagram.Options`. Behaviors read the options each time they act, so a change applies to the next gesture.

{{source diagram-options DiagramOptions.razor options}}

{{example diagram-options}}

| Option | Default | Effect |
|---|---|---|
| `GridSize` | `null` | Dragged nodes snap to multiples of this many diagram units. |
| `GridSnapToCenter` | `false` | Snap a node's centre instead of its top-left corner. |
| `AllowMultiSelection` | `true` | Ctrl-click adds to the selection. |
| `AllowPanning` | `true` | Dragging the empty background pans. |
| `LinksLayerOrder`, `NodesLayerOrder` | `0`, `0` | CSS `z-index` of the SVG layer and the HTML layer. When equal, HTML nodes draw above links. |
| `Zoom.Enabled` | `true` | The wheel zooms. `SetZoom` works either way. |
| `Zoom.Minimum`, `Zoom.Maximum` | `0.1`, `2` | Limits for the wheel and for `SetZoom`. |
| `Zoom.ScaleFactor` | `1.05` | Zoom change per wheel step; 1.01 to 2, otherwise it throws. |
| `Zoom.Inverse` | `false` | Reverses the wheel direction. |
| `Links.DefaultRouter`, `Links.DefaultPathGenerator` | `NormalRouter`, `SmoothPathGenerator` | Used by links that do not set their own. |
| `Links.RequireTarget` | `true` | A dragged link dropped on nothing is removed. |
| `Groups.Enabled` | `false` | Turns on the Ctrl+Alt+G shortcut that groups the selected nodes. |
| `Constraints.ShouldDeleteNode`, `...Link`, `...Group` | allow all | Veto a deletion started by the user. |
| `Virtualization.Enabled` | `false` | Stops drawing models outside the viewport (nodes by default; `OnGroups`, `OnLinks` opt in). |

Grid snapping applies only while dragging: a node placed in code, like those added by double-click above, keeps its exact position. The constraints see only deletions made with the Delete key or the remove control; `Diagram.Nodes.Remove` is never vetoed. Link creation options are in [Ports and anchors](guides/ports-and-anchors).

## Coordinates, zoom and pan

Model positions and sizes are in **diagram units**. A diagram point appears at `point × Zoom + Pan` from the canvas's top-left corner; `Pan` is in screen pixels, `Zoom` a plain scale. Pointer events report **client pixels** (`ClientX`, `ClientY`).

| Method | Converts |
|---|---|
| `GetRelativeMousePoint(clientX, clientY)` | Client pixels to diagram units. Use it to place something where the user clicked or dropped. |
| `GetScreenPoint(x, y)` | Diagram units to client pixels; the inverse. |
| `GetRelativePoint(clientX, clientY)` | Client pixels to pixels inside the canvas, ignoring pan and zoom. |

All three need `Container`, the canvas rectangle measured after the first render, and throw before it.

The user zooms with the wheel, around the pointer, and pans by dragging the background (Shift-drag is the selection box). From code:

- `SetZoom(z)` clamps to the zoom limits and keeps `Pan`, so the view scales about the canvas's top-left corner, not its centre.
- `SetPan(x, y)` and `UpdatePan(dx, dy)` set or shift the pan in screen pixels.
- `ZoomToFit(margin)` fits the selected nodes, or all nodes when none is selected, with `margin` diagram units around them. Links and groups are not counted, and the result is clamped to the zoom limits. It does nothing while the canvas has no width or height (for example in a hidden tab).

{{example zoom-and-pan}}

## Events

Events are plain .NET events, raised outside your component's render cycle: when a handler changes what your component shows, call `InvokeAsync(StateHasChanged)`.

| Event | Raised |
|---|---|
| `PointerDown`, `PointerUp` | With the model under the pointer, or `null` on the background. |
| `PointerMove` | On every move; from the canvas the model is always `null`. |
| `PointerEnter`, `PointerLeave` | When the pointer enters or leaves a node, group or link. |
| `PointerClick` | Down and up on the same model without moving. |
| `PointerDoubleClick` | A click within 500 ms of the previous click on the same model (or on the background twice). |
| `SelectionChanged` | Once per model that becomes selected or unselected. |
| `KeyDown`, `Wheel` | Keyboard (while the canvas has focus) and wheel input. |
| `PanChanged`, `ZoomChanged`, `ContainerChanged` | After the viewport or the canvas size changes. |
| `Nodes.Added`, `Nodes.Removed` (and `Links`, `Groups`) | For each item added or removed. |
| `Changed` | On every refresh; the canvas listens to it to redraw. |

Two node events are easy to confuse: `Moved` fires once when a drag that moved the node ends, and never for `SetPosition`; `Moving` fires on every `SetPosition`, including each step of a drag.

{{source diagram-options DiagramOptions.razor double-click}}

{{example events}}

**Selection.** `Selected` is read-only; select with `SelectModel(model, unselectOthers)`, `UnselectModel` and `UnselectAll`, and read the selection with `GetSelectedModels()`. A click replaces the selection, Ctrl-click toggles a model, and a click on the background clears it. The rubber band of `SelectionBoxWidget` selects nodes only, not groups or links.

## Refresh and Batch

Every change calls `Refresh`, which raises `Changed`, and the canvas redraws. `Batch` runs an action with refreshes suspended and refreshes once at the end, also when the action throws; an inner batch leaves the refresh to the outer one.

{{source diagram-options DiagramOptions.razor batch}}

Changing a model's property (a node's `Title`, a link's `Color`) does not redraw it. Call `Refresh()` on that model: nodes and links redraw only on their own `Changed`, so `Diagram.Refresh()` is not enough.

## Locking

Set `Locked` on a node, group, link or port:

- a locked node or group is not moved by dragging;
- a locked node, group or link is not deleted by the Delete key or the remove control;
- a locked port, or any port of a locked node, can neither start a link nor receive one;
- locked nodes and groups get a `locked` CSS class.

{{example locked}}

A locked node can still be selected. Setting `Locked` does not refresh the model, and does not stop code: `SetPosition` and `Remove` still work. For a read-only diagram, unregister the editing behaviors: `Diagram.UnregisterBehavior<DragMovablesBehavior>()`, and likewise `DragNewLinkBehavior` and `KeyboardShortcutsBehavior`.
