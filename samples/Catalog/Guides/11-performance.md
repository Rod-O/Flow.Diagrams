# Performance

Keep large diagrams responsive: know what redraws when, batch your changes, let virtualization skip what is off screen, and choose node and link setups that avoid measuring in the browser.

{{example many-nodes}}

## What redraws when

Every drawn piece is a component that redraws only when its own model says so. Their `ShouldRender` returns `false` for any other cause.

| Component | Redraws when |
|---|---|
| [DiagramCanvas](xref:Flow.Diagrams.Components.DiagramCanvas) | The diagram raises `Changed`: models added or removed, pan, zoom, drawing order. A pointer move with no gesture raises no `Diagram.Changed`, so it renders no canvas. |
| `NodeRenderer`, `GroupRenderer`, `LinkRenderer`, `PortRenderer` | Their model raises `Changed` (a property was set, or `model.Refresh()` ran) or its `Visible` changes. |
| `ControlsLayerRenderer` | The controls of a model in its layer are shown, hidden, changed, or their model changes while they are shown. |

So a pan redraws the canvas, which moves the two layers with a CSS transform and walks the list of models, but no node or link widget runs. Dragging a node redraws that node and its links, and each of those links recomputes its route and path once per pointer move.

The consequence for your code: **setting a library property redraws its model and what depends on it.** The model marks the change, the diagram collects the marks and flushes them once, and a flush recomputes geometry (link routes, group bounds) once and passes it on to dependents: a node to its links and group, a port to its links, a link to the links, labels and vertices attached to it. For state the diagram cannot see, a property you added to your own model for example, call `Refresh()` on that model. `Diagram.Refresh()` is not enough; it redraws the canvas, and the canvas skips every node whose model did not raise `Changed`.

## Batch your changes

Each `Add`, `Remove` and `Clear` on `Diagram.Nodes`, `Links` and `Groups` already refreshes the diagram once for its whole call, and `Add(IEnumerable<T>)` adds many items under one refresh. For a sequence of different changes, wrap them in `Diagram.Batch(...)`: the flush is suspended while the action runs and happens once at the end, so a model changed many times raises `Changed` once and a link is routed once. Batches nest; only the outermost one flushes.

An input event (`Diagram.Trigger*`, which the canvas calls for every pointer, key and wheel event) flushes the same way, once at its end, so a handler that changes many models needs no batch of its own. Outside a batch or an input event, a change flushes at once. A `Changed` handler runs after the flush, so it sees the final state, not each step. A handler that keeps marking models stops after 64 passes with a `DiagramsException`.

If the action throws, `Batch` still restores refreshes and flushes once (whatever the action changed before throwing is drawn), then lets the exception propagate. Setting `SuspendRefresh` by hand gives none of that: clear it in a `finally`.

## Virtualization

Virtualization hides models outside the viewport by setting their `Visible` to `false`; a hidden model renders nothing. It is off by default:

| Option (`Options.Virtualization`) | Default | Effect |
|---|---|---|
| `Enabled` | `false` | Master switch. |
| `OnNodes` | `true` | Hide nodes outside the view. |
| `OnGroups` | `false` | Hide groups outside the view. |
| `OnLinks` | `false` | Hide links outside the view. |

[VirtualizationBehavior](xref:Flow.Diagrams.Core.Behaviors.VirtualizationBehavior) rechecks when the zoom, the pan or the canvas size changes, when a node, group or link is added, and when a drag ends. Switching an option does not recheck by itself: call `CheckVisibility()` on the behavior. The example does it when you toggle virtualization:

{{source many-nodes ManyNodes.razor virtualization}}

- Turning it **on** applies at the next check, so the example calls `CheckVisibility()`.
- Turning it **off** (or turning off one kind, such as `OnNodes`) shows again the models it hid at the next check; `CheckVisibility()` does it at once.
- The behavior remembers the models it hid (`HiddenModels`) and shows only those again, so a model you hid with `Visible = false` stays hidden.
- The SVG exporter includes the models hidden by virtualization and skips the ones you hid: see [Export](guides/export).

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
- Turn on virtualization for diagrams larger than the screen, and call `CheckVisibility()` after changing the options.
- Set properties and let the diagram redraw; use `Batch` for many changes, and `Refresh()` on the one model only for state the diagram cannot see.

The two examples are on the [Many nodes](examples/many-nodes) and [Many links](examples/many-links) pages.
