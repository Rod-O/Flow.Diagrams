# Performance

Keep large diagrams responsive: know what redraws when, batch your changes, let virtualization skip what is off screen, and choose node and link setups that avoid measuring in the browser.

{{example many-nodes}}

## What redraws when

Every drawn piece is a component that redraws only when its own model says so. Their `ShouldRender` returns `false` for any other cause.

| Component | Redraws when |
|---|---|
| [DiagramCanvas](xref:Flow.Diagrams.Components.DiagramCanvas) | The diagram raises `Changed`, that is when `Diagram.Refresh()` runs: models added or removed, pan, zoom, drawing order. |
| `NodeRenderer`, `GroupRenderer`, `LinkRenderer`, `PortRenderer` | Their model raises `Changed` (`model.Refresh()`) or its `Visible` changes. |
| `ControlsLayerRenderer` | The controls of a model in its layer are shown, hidden, changed, or their model changes while they are shown. |

So a pan redraws the canvas, which moves the two layers with a CSS transform and walks the list of models, but no node or link widget runs. Dragging a node refreshes that node and its links, and each link refresh recomputes its route and path.

The consequence for your code: **after changing a property of your own model, call `Refresh()` on that model.** `Diagram.Refresh()` is not enough; it redraws the canvas, and the canvas skips every node whose model did not raise `Changed`. `SetPosition` and selecting or unselecting refresh the model for you; setting `Size`, `Title` or a property you added does not.

## Batch your changes

Each `Add`, `Remove` and `Clear` on `Diagram.Nodes`, `Links` and `Groups` already refreshes the diagram once for its whole call, and `Add(IEnumerable<T>)` adds many items under one refresh. For a sequence of different changes, wrap them in `Diagram.Batch(...)`: refreshes are suspended while the action runs and the diagram refreshes once at the end. Batches nest; only the outermost one refreshes.

Trap: `Batch` is not exception-safe. If the action throws, `SuspendRefresh` stays `true` and the diagram never redraws again. Catch inside the action, or reset `SuspendRefresh` yourself when you handle the exception.

## Virtualization

Virtualization hides models outside the viewport by setting their `Visible` to `false`; a hidden model renders nothing. It is off by default:

| Option (`Options.Virtualization`) | Default | Effect |
|---|---|---|
| `Enabled` | `false` | Master switch. |
| `OnNodes` | `true` | Hide nodes outside the view. |
| `OnGroups` | `false` | Hide groups outside the view. |
| `OnLinks` | `false` | Hide links outside the view. |

[VirtualizationBehavior](xref:Flow.Diagrams.Core.Behaviors.VirtualizationBehavior) rechecks only when the zoom, the pan or the canvas size changes. Adding or moving a model does not recheck, and neither does switching the option. The example handles both directions when you toggle it:

{{source many-nodes ManyNodes.razor virtualization}}

- Turning it **on** does nothing until the view moves, so the example nudges the pan by a pixel.
- Turning it **off** leaves the hidden models hidden, so the example sets `Visible = true` on every node.
- A model you add outside the view stays drawn until the next pan or zoom.
- Virtualization overwrites `Visible` for the kinds it covers, so you cannot also use `Visible` to hide nodes yourself while it is on.
- The SVG exporter skips invisible models: see [Export](guides/export).

Virtualization reduces what is drawn, not what is computed: models keep their positions and routes, and with `OnLinks` off every link is still drawn.

## Many links

{{example many-links}}

Hundreds of links over a few dozen nodes stay light here because nothing has to be measured in the browser:

{{source many-links ManyLinks.razor links}}

{{source many-links ManyLinksNode.cs}}

- **No ports.** The links join nodes directly (`new LinkModel(node, node)`) and attach where they cross the node's shape. Every rendered port is measured once with a JavaScript call, and again whenever its node is resized.
- **Controlled size.** The node sets its `Size` and `ControlledSize = true`, so its renderer does not observe its element for resizes. Without it, each node is measured through JavaScript after it renders.
- **A cheap path.** `StraightPathGenerator` with the default `NormalRouter` costs almost nothing per refresh. `ObstacleRouter` routes each link against every node and every other link, so dragging a node that many links touch costs more with it.

## Checklist

- Build large diagrams inside `Batch`, or with the `IEnumerable` overloads of `Add`.
- Give nodes a size and `ControlledSize = true` when you know their size.
- Prefer node-to-node links when the attachment point does not matter.
- Turn on virtualization for diagrams larger than the screen, and recheck by nudging the pan after bulk changes.
- Call `Refresh()` on the one model you changed, not on the diagram.

The two examples are on the [Many nodes](examples/many-nodes) and [Many links](examples/many-links) pages.
