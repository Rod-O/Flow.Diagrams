# Flow.Diagrams: architecture review before 1.0

Reviewed at commit `76c2470` (1.0.0-alpha.0). Scope: `src/` (Core, Blazor package, Algorithms, `script.js`, CSS),
the tests, the catalog guides, `benchmarks/README.md`, and NodeCAD's client as a real consumer
(`NodeCad.Client`, Flow.Diagrams submodule). File references are to this commit; NodeCAD references are to
`/Volumes/Samsung990/NodeCAD/src/NodeCad/NodeCad.Client` unless stated.

New measurements in this document were taken with throwaway code outside the repository (not committed): a console
program over the Core models, and a minimal `Microsoft.AspNetCore.Components.RenderTree.Renderer` subclass that mounts
the real `DiagramCanvas` with a counting `IJSRuntime` and counts render batches, rendered components and DOM edits.
It dispatches pointer moves through the canvas's real `onpointermove` handler, so batching matches the browser. Machine:
Intel Xeon W-3245, macOS, .NET 10.0.11, Release, JIT. They measure the .NET side only; browser layout, paint, the
WebAssembly runtime and Blazor Server's network are not in them (see "What I could not determine").

## Summary

The library is a model-view design with a clean Core (models, behaviors, routers, geometry, no Blazor dependency)
and a thin Blazor layer of one renderer component per model. That split, the geometry and routing code, and the test
and benchmark infrastructure are worth keeping. Five decisions shape the public contract and should be made before
1.0, because every one of them changes what users subclass, call or rely on:

| # | Decision | Why before 1.0 |
|---|---|---|
| 1 | One drawing surface (SVG) with HTML as an opt-in island, instead of two layers chosen by model subclass | `SvgNodeModel`/`SvgGroupModel` are the API for it today; z-order, export and labels depend on it |
| 2 | Geometry computed in the model (sizes, port positions), browser measurement as an opt-in | `PortModel.Position/Size/Initialized`, `ControlledSize` and `ReinitializePorts` are the contract today |
| 3 | Change tracking: observable properties, dirty flags and one flush per turn, instead of opt-in `Refresh()` per model | Every user model and widget is written against "call `Refresh()` yourself" |
| 4 | Behaviors: explicit ordering/phases and a handled flag, instead of multicast registration order | Custom behaviors (NodeCAD's) depend on the current ordering rules |
| 5 | Routing as a diagram-level pass with a shared context, not a side effect of each link's `Refresh()` | `Router.GetRoute(Diagram, BaseLinkModel)` is the extension point |

Most other findings (exception safety, the JS observer, virtualization's flag, the transient link, smaller API
inconsistencies) are cheaper and several can be fixed without breaking anything.

---

## 1. How it works

### 1.1 Packages

| Package | Contents | Depends on |
|---|---|---|
| `Flow.Diagrams.Core` | `Diagram`, layers, models, anchors, behaviors, controls (model side), routers, path generators, geometry, options | `SvgPathProperties` (its `SvgPath` appears in the public `PathGeneratorResult`) |
| `Flow.Diagrams` | `BlazorDiagram`, `DiagramCanvas`, renderers, default widgets, controls widgets, navigator/grid/selection-box widgets, `DiagramSvgExporter`, `script.js`, CSS | Core, ASP.NET Core Components |
| `Flow.Diagrams.Algorithms` | `LinksReconnectionAlgorithms` | Core |

### 1.2 The model

`Diagram` (abstract, `Diagram.cs:24`) owns three layers, `Nodes`, `Links`, `Groups` (`BaseLayer<T>`,
`Layers/BaseLayer.cs:13`), a `ControlsLayer`, the viewport (`Pan` in screen pixels, `Zoom` a scale, `Container` the
canvas rectangle in client pixels), the registered behaviors, and the input events (`PointerDown/Move/Up/Enter/Leave`,
`KeyDown`, `Wheel`, plus `PointerClick`/`PointerDoubleClick` raised by `EventsBehavior`). It keeps
`OrderedSelectables`, every node, link and group sorted by `Layer` then `Order` (`Diagram.cs:159`, `:539`).

The model classes:

```
Model (Id, Locked, Visible, Changed, VisibilityChanged)
├── SelectableModel (Selected, Order, Layer, OrderChanged)
│   ├── MovableModel (Position, SetPosition, Moved)
│   │   ├── NodeModel (Size, ControlledSize, Title, Ports, Links, Group, SizeChanged, Moving)
│   │   │   └── GroupModel (Children, padding; is a NodeModel but lives in Diagram.Groups, not Nodes)
│   │   └── LinkVertexModel (Parent link)
│   └── BaseLinkModel (Source/Target anchors, Router, PathGenerator, markers, Vertices, Labels, Route,
│       │              PathGeneratorResult, Halos, Diagram)
│       └── LinkModel (Color, SelectedColor, Width)
├── PortModel (Parent node, Alignment, Position, Size, Initialized, Links)
└── LinkLabelModel (Parent link, Content, Distance, Offset)
```

The Blazor package adds `SvgNodeModel` and `SvgGroupModel` (`Models/SvgNodeModel.cs:11`), empty subclasses whose
only meaning is "draw me in the SVG layer".

Positions are absolute diagram coordinates for nodes, groups, ports and vertices. A port's `Position` is not
relative to its node: it is a cached absolute rectangle that `NodeModel.SetPosition` shifts by the node's delta
(`NodeModel.cs:150-160`, `:212-220`).

A link joins two **anchors**. `SinglePortAnchor` ends at a port (and returns `null` until the port is
`Initialized`, `SinglePortAnchor.cs:26`); `ShapeIntersectionAnchor` ends where the line to the other end crosses the
node's shape; `DynamicAnchor` picks the closest of several position providers; `LinkAnchor` ends on another link;
`PositionAnchor` is a free point (used while dragging a new link). Adding a link to `Diagram.Links` registers it with
the anchors' models (`ILinkable.AddLink`, `LinkLayer.cs:17-26`).

A **group** is a `NodeModel` whose box follows its children (`GroupModel.UpdateDimensions`, `GroupModel.cs:171-191`).
Children stay in `Diagram.Nodes`; the group is in `Diagram.Groups`.

### 1.3 From a model change to pixels

```
 input (pointer/key/wheel on a renderer or the canvas)
        │  Blazor event handler → Diagram.TriggerPointerX(model, args)
        ▼
 Diagram events ──► behaviors (multicast, registration order) ──► model mutations
        │                                                         │
        │                                     model.Refresh() ── raises Model.Changed
        │                                     (BaseLinkModel.Refresh first recomputes route + path)
        ▼                                                         ▼
 Diagram.Refresh() ── Diagram.Changed ──► DiagramCanvas     NodeRenderer / LinkRenderer / GroupRenderer /
        (adds, removes, pan, zoom, order)    re-renders its   PortRenderer set _shouldRender, StateHasChanged
                                             model lists      ──► that component's widget re-renders
        ▼
 Blazor render batch → DOM edits
        ▼
 JS: ResizeObserver per measured node → NodeRenderer.OnResize → Node.Size, Refresh, ReinitializePorts
     PortRenderer.OnAfterRender → getBoundingClientRect per uninitialized port → Port.Position/Size
```

`DiagramCanvas` (`Components/DiagramCanvas.razor`) renders two sibling layers, each moved by the same CSS transform
`translate(pan) scale(zoom)` (`DiagramCanvas.razor.cs:65-69`):

- an `<svg class="diagram-svg-layer">` with, in drawing order, every top-level `SvgNodeModel`, `SvgGroupModel` and
  link, then (optionally) all labels, then the SVG controls (`DiagramCanvas.razor:12-44`);
- a `<div class="diagram-html-layer">` with every top-level HTML node and group, then the HTML controls
  (`:46-66`). It comes later in the DOM, so with the default `z-index: 0` on both it is drawn on top.

Each model is drawn by a renderer component (`NodeRenderer`, `GroupRenderer`, `LinkRenderer`, `PortRenderer`,
`LinkLabelRenderer`, `LinkVertexRenderer`) that wraps the user's widget, found through
`BlazorDiagram.RegisterComponent<TModel, TComponent>` (`BlazorDiagram.cs:37-92`). Renderers gate rendering with a
`_shouldRender` flag set only by their model's `Changed`/`VisibilityChanged` (`NodeRenderer.cs:100-107`, `:176-191`),
so a canvas re-render does not redraw nodes. Group children are rendered inside the group's element
(`GroupNodes.razor`), not by the canvas.

**Coordinate systems.** Client pixels (pointer events) → canvas pixels (minus `Container.Left/Top`) → diagram units
(minus `Pan`, divided by `Zoom`): `GetRelativeMousePoint`, `GetRelativePoint`, `GetScreenPoint`
(`Diagram.cs:418-451`). `Container` is measured by JS after the first render and kept current by a `ResizeObserver`,
a document-wide `MutationObserver` and a `window` scroll listener (`script.js:7-27`, `:51-57`). Until then every
conversion throws.

**Measurement.** An HTML node without `ControlledSize` is observed by a `ResizeObserver` (one interop call to observe,
`NodeRenderer.cs:163-166`); each resize calls back `OnResize`, which divides by zoom, sets `Size`, refreshes the node
and its links and marks every port uninitialized (`NodeRenderer.cs:61-79`). A `PortRenderer` whose port is not
`Initialized` calls `getBoundingClientRect` on its element and converts it to diagram units
(`PortRenderer.cs:149-181`). SVG ports are measured the same way.

### 1.4 Behaviors

A `Behavior` (`Behavior.cs:10`) is an `IDisposable` that subscribes to diagram, layer or model events in its
constructor. `RegisterBehavior` stores it in a `Dictionary<Type, Behavior>` keyed by exact type
(`Diagram.cs:293-300`); `GetBehavior<T>` falls back to the first subclass (`:306-312`). The default set, in order:
selection, drag movables, drag new link, pan, zoom, events (click/double click), keyboard shortcuts, controls,
virtualization (`Diagram.cs:109-117`). Opt-in: `LinkHalosBehavior`, `DebugEventsBehavior`.

There is no dispatcher: a C# multicast event calls subscribers in subscription order, so the order behaviors run in
is the order they were constructed. Behaviors coordinate implicitly:

- `DragMovablesBehavior` captures `GetSelectedModels()` on pointer down (`DragMovablesBehavior.cs:36-53`), so it must
  run after `SelectionBehavior` has selected the clicked node.
- `PanBehavior` starts only when the press is on the empty canvas without Shift (`PanBehavior.cs:36`);
  `SelectionBoxWidget` (a component, not a behavior) starts only on the empty canvas with Shift
  (`SelectionBoxWidget.razor.cs:49`). The split is a convention between two unrelated classes.
- `DragNewLinkBehavior.OngoingLink` is read by `LinkWidget` (`LinkWidget.razor:9`) and by controls through
  `GetBehavior` (`DragNewLinkControl.cs:37`, `ArrowHeadControl`).
- `PointerClick`/`PointerDoubleClick` are events on `Diagram` that exist only while `EventsBehavior` is registered
  (`Diagram.cs:55-63`).

### 1.5 Controls

`ControlsLayer` maps a model to a `ControlsContainer` of `Control`s shown on hover or selection
(`Controls/ControlsLayer.cs`, `ControlsBehavior.cs`). A control computes its position from the model; an
`ExecutableControl` handles pointer down (remove, drag a new link, arrow head). `ControlsLayerRenderer` is rendered
once in each layer and draws the controls of models of its kind (`ControlsLayerRenderer.razor`).

### 1.6 Routing and path generation

`BaseLinkModel.Refresh()` recomputes the link's geometry every time, before raising `Changed`
(`BaseLinkModel.cs:103-107`, `:231-250`): the router (`Link.Router ?? Options.Links.DefaultRouter`) returns the
intermediate points, the anchors resolve the end points against that route, and the path generator (`Smooth` or
`Straight`) turns them into an `SvgPath` plus marker positions. Routers: `NormalRouter` (the vertices),
`OrthogonalRouter` (A* on a grid of node edges), `ObstacleRouter` (Dijkstra on a Hanan-style grid with penalties for
bends, crossings and hugging other links' current routes, `ObstacleRouter.cs:57-82`). A link is therefore re-routed
whenever it refreshes: when it is added, when one of its ends moves or resizes, or when user code calls `Refresh()`.
`ObstacleRouter.RouteAll` refreshes every link it owns several times in one batch (`ObstacleRouter.cs:40-49`).

`LinkHalosBehavior` listens to every link's `Changed` and computes crossing halos against the links drawn below.

### 1.7 Export

`DiagramSvgExporter.ExportAsync` (`Export/DiagramSvgExporter.cs:35-73`) temporarily un-hides models hidden by
virtualization, renders `SvgExportCanvas` with a static `HtmlRenderer` using the same renderers and widgets as the live
canvas, strips Blazor's internal attributes, and returns the SVG plus the list of models it could not export (HTML
nodes and groups, HTML labels).

---

## 2. What is sound and should be kept

- **Core has no Blazor dependency.** Models, behaviors, routers and geometry are testable without a renderer; the
  benchmarks run on Core alone. This is the asset that makes the redesigns below affordable.
- **Anchors as a strategy.** Port, shape-intersection, dynamic, link and position anchors cover the cases users ask
  for and compose with any router. Keep the abstraction; only its timing (when it is evaluated) needs change.
- **Router / path generator split.** Route (topology) and path (curve) are separate and swappable per link or per
  diagram. Keep it; change how routing is scheduled, not this split.
- **Renderer + registered widget.** Users write a plain component that receives its model; the library owns events,
  selection classes and positioning. It is the right extension point.
- **`Layer` + `Order` drawing bands** and the ordered list are a good model of z-order; the problem is only that the
  HTML/SVG split defeats it.
- **Geometry code** (`Rectangle`, `Ellipse`, `Shapes`, `LinkGeometry`, `BezierSpline`) is small, tested
  (`tests/Flow.Diagrams.Core.Tests/Geometry/*`) and was hardened in this fork (CHANGELOG "Fixed").
- **`ObstacleRouter`'s routing quality** (nets, crossing and hugging costs, stubs before markers) matches what NodeCAD
  needs for engineering drawings; its cost is a scheduling and indexing problem, not an algorithm problem.
- **Export through the real widgets.** One widget draws both screen and file, so they cannot drift. Keep it.
- **Test infrastructure:** bUnit component tests, leak tests (`tests/*/Memory`), BenchmarkDotNet suite, and a
  catalog whose examples double as manual tests. The benchmarks already found the two worst cliffs.
- **Per-model `ShouldRender` gating** is the right idea; it is why a single-node drag renders 14 components out of
  17 000 (section 4). The redesign should keep component-level gating and change what feeds it.

---

## 3. Problems and options

Severity: **Contract** = changes public API or a core rule; decide before 1.0. **High** = frequent bugs or
workarounds, fixable later without breaking. **Medium/Low** as named.

### P1. Two rendering layers chosen by model subclass

**Design.** HTML nodes and groups are drawn in a `div` layer, SVG nodes, groups and all links in an `svg` layer
(`DiagramCanvas.razor:12-66`). Which layer is decided by the model's type: `SvgNodeModel`/`SvgGroupModel`
(`ModelExtensions.IsSvg`, `Extensions/ModelExtensions.cs:13-16`).

**Consequences.**
- No link, halo or label can be drawn above an HTML node; `Layer`/`Order` only sort HTML nodes among themselves
  (guide `03-nodes.md:26-33`). `LabelsOnTop` cannot lift labels above HTML nodes.
- HTML nodes are not exported; the exporter reports them (`DiagramSvgExporter.cs:97-105`). `foreignObject` labels
  are reported too unless `SvgLabels` is on.
- The render mode is a type, so a node cannot switch mode at runtime; NodeCAD plans to re-base its node, note and
  group models on `SvgNodeModel`/`SvgGroupModel` and re-implement every widget
  (`NodeCAD/docs/plans/DIAGRAM_SVG_MIGRATION.md`, "Design").
- Ports of HTML nodes are HTML elements measured through JS; ports of SVG nodes are measured the same way
  (`PortRenderer.cs:149-181`) even though their geometry is known.
- Two `ControlsLayerRenderer`s, two code paths in every renderer (`_isSvg` in `NodeRenderer.cs:122-135`,
  `GroupRenderer.cs:114-123`, `PortRenderer.cs:81`, `GroupNodes.razor`).

**Severity.** Contract. It decides the meaning of `SvgNodeModel`, the z-order guarantee and what export can promise.

**Options.**

| Option | Gains | Costs |
|---|---|---|
| A. SVG is the drawing surface; an HTML node is an SVG `<g>` hosting a `foreignObject` island (opt-in per widget) | One z-order for everything; export of all non-island content; one code path; the type distinction disappears (render mode becomes a renderer/widget property) | Breaking: `SvgNodeModel`/`SvgGroupModel` deprecated; HTML-in-`foreignObject` has browser quirks (Safari transforms, focus), and islands still cannot be exported as vectors; L effort |
| B. Keep two layers, add more layers (HTML layer per band, interleaved `svg`/`div` sheets per `Layer` value) | Links can go above nodes of a lower band; non-breaking | Interleaving only at band granularity; more DOM; export still loses HTML; M effort |
| C. Keep as is, document | Zero cost | Every consumer that exports or layers (NodeCAD) rewrites its widgets anyway |

**Recommendation.** A, decided before 1.0: make the render surface SVG and HTML an explicit island
(`<NodeHtml>` wrapper or a widget flag), with the island's size controlled by the model (P2). Keep `SvgNodeModel` as
an obsolete alias for one release if the break must be softened.

### P2. Geometry measured in the browser

**Design.** Node sizes (HTML, unless `ControlledSize`) come from a `ResizeObserver` per node; every port's position
and size comes from `getBoundingClientRect` per port (`NodeRenderer.cs:152-174`, `PortRenderer.cs:97-111`,
`:149-181`). The result is stored as absolute coordinates on `PortModel` and then shifted by deltas when the node
moves (`NodeModel.cs:212-220`). `SinglePortAnchor` returns `null` until `Initialized` (`SinglePortAnchor.cs:26`), so
a link is invisible until its ports are measured.

**Consequences.**
- A diagram cannot be laid out, routed or exported before it has been on screen. The benchmarks have to fake it
  (`GridLayout.AddMeasuredPort` sets `Initialized = true` by hand). Static server rendering draws no link (guide
  `01-getting-started.md:74`).
- Load assembles in jumps: NodeCAD covers the canvas until every node has a size
  (`Models/DiagramReadiness.cs:7-43`, used at `Components/Diagrams/DiagramEditor.razor.cs:218`).
- NodeCAD replaced port measurement entirely with `PortLayout`, computing ports from node size in C#, because
  measurement cost "16·N JS calls, each one forcing the browser to lay out the whole canvas, each one followed by a
  re-render of the port's links" and because an expanded container that draws no ports never got its links drawn
  (`Models/PortLayout.cs:10-24`; `DiagramRenderCostTests`).
- `ControlledSize` is needed for SVG nodes or their measured `g` bounding box feeds back into `Size` (observed bug).
  After changing a controlled size the user must call `Refresh()` and `ReinitializePorts()` by hand (guide
  `03-nodes.md:54`).
- A port's position is cached state with several writers: the JS measurement, `UpdatePortPositions` on move, the
  group fix in `UpdateDimensions` (`GroupModel.cs:184-186`, upstream #581). `MovableModel.Position` has a public
  setter (`MovableModel.cs:36`) that moves a node without moving its ports. A port that moves inside its node without
  the node resizing (CSS change, content reflow) is never re-measured.
- The `PortRenderer` race (gave up when the canvas was not yet measured) was a direct product of this design; the fix
  subscribes to `ContainerChanged` (`PortRenderer.cs:151-158`).
- Every measured element costs interop on add, resize and dispose (section 4).

**Severity.** Contract (`PortModel.Position/Size/Initialized`, `ControlledSize`, `ReinitializePorts` are public and
documented), and the root of most first-load and export bugs.

**Options.**

| Option | Gains | Costs |
|---|---|---|
| A. Model owns geometry: a port is placed by a pure function of its node's box (alignment or fractional X/Y, as NodeCAD's `PortLayout` and its `Port X/Y` already do); `Size` is required or computed by the widget's layout in C#; JS measurement becomes an opt-in `MeasuredSize` mode for HTML islands | Layout, routing, export and tests without a browser; no first-load jumps; removes per-port interop; port position is derived, not cached, so it cannot drift | Breaking for code that sets `Port.Position`/`Initialized`; widgets that rely on CSS layout for size need a C# size or the opt-in measurement; text needs a measurer (NodeCAD plans SkiaSharp) — the library can stay agnostic with an `ITextMeasurer` hook; M–L |
| B. Keep measurement, store port offsets relative to the node, re-measure on a single batched JS call per frame | Fewer bugs from drift; fewer calls | Still no headless geometry; still first-load jumps; M |
| C. Keep as is | — | NodeCAD-style workarounds in every serious consumer |

**Recommendation.** A. Make relative port placement the default and measured size the exception; derive
`Port.Position` from node position + relative offset on read. This is the change that unlocks headless export,
server-side layout and the performance work in section 4.

### P3. Change propagation is opt-in, per model, and conflates recompute with repaint

**Design.** A model redraws only when something calls its `Refresh()`, which raises `Changed`
(`Model.cs:57`). Most properties are plain auto-properties with no notification: `NodeModel.Title`
(`NodeModel.cs:72`), `Model.Locked` (`Model.cs:36`), `LinkModel.Color/SelectedColor/Width` (`LinkModel.cs:37-43`),
`LinkLabelModel.Content` (`LinkLabelModel.cs:36`). `NodeModel.Size` raises `SizeChanged` but not `Changed`
(`NodeModel.cs:48-59`). `SetSource`/`SetTarget`/`AddLabel`/`AddVertex` do not refresh (guide `05-links.md:13`).
`Diagram.Refresh()` re-renders the canvas only; every renderer below it is gated on its own model's `Changed`.
For links, `Refresh()` also means "recompute route and path" (`BaseLinkModel.cs:103-107`), so repaint and geometry
are one signal. `Diagram.Batch` suspends only `Diagram.Changed`; model `Changed` events still fire, and each one
outside a Blazor event handler produces its own render batch.

**Consequences.**
- The documented rule "after changing a property, call `Refresh()` on that model; `Diagram.Refresh()` is not enough"
  (guide `02-the-diagram.md:87`, `11-performance.md:19`) is a trap users hit; NodeCAD's diagram service calls
  `Refresh`/`RefreshLinks`/`RefreshAll` 21 times by hand (`Services/DiagramStateService.cs`), plus 4 in the undo
  service.
- Over-refresh is as easy as under-refresh: `GroupModel.SetPosition` calls `base.SetPosition` (which refreshes the
  group and its links) and then refreshes both again (`GroupModel.cs:98-112`); `SetHalos` raises `Changed` again
  (`BaseLinkModel.cs:214-221`), which re-enters `LinkHalosBehavior.OnLinkChanged`.
- Behaviors that need geometry subscribe to `Changed` and get repaint-only changes too; `LinkHalosBehavior` has to
  compare polylines to skip them (`LinkHalosBehavior.cs:63-73`).
- Measured: removing 500 of 1000 nodes inside `Diagram.Batch` produces **4 363 render batches** and 500 JS calls;
  retitling and refreshing 500 nodes inside `Batch` produces 501 batches (section 4.2). On Blazor Server each batch is
  a SignalR message.

**Severity.** Contract. The "call `Refresh()` yourself" rule is in every widget and model users write.

**Options.**

| Option | Gains | Costs |
|---|---|---|
| A. Observable properties + dirty tracking + one flush: setters mark the model dirty with a reason (geometry, appearance, structure); the diagram collects dirty models and, once per dispatcher turn (or per animation frame), recomputes geometry for geometry-dirty models in dependency order (ports → links → halos/labels), then calls `StateHasChanged` once per dirty renderer inside one render batch | No manual `Refresh()`; `Batch` really batches; geometry computed once per turn instead of once per event; the natural hook for a frame loop (P13) and for incremental halos/routing | Breaking: `Refresh()` becomes an escape hatch, custom properties need `SetField`-style setters or an explicit `MarkDirty()`; ordering bugs move into one scheduler (testable in Core); L |
| B. Keep per-model events, add notifications to existing properties, make `Batch` defer model `Changed` and replay de-duplicated at the end | Fixes the trap for built-in properties and the batch storm; mostly non-breaking | Still two signals mixed; custom properties still need `Refresh()`; M |
| C. Keep, document better | — | Trap stays |

**Recommendation.** A before 1.0, at least its contract: properties notify, `Batch` defers, and `Changed` carries a
reason. The scheduler itself can start simple (flush at end of batch/event) and gain frame alignment later without
breaking.

### P4. Behaviors coordinate through registration order and each other's internals

**Design.** See 1.4. Order = constructor order; no phases, no "handled" flag, no pointer capture; behaviors look each
other up by type.

**Consequences.**
- NodeCAD replaces `SelectionBehavior` and must unregister and re-register `DragMovablesBehavior` to restore the
  order, "otherwise clicking a new node drags the *previously* selected node"
  (`Services/DiagramStateService.cs:234-242`).
- Observed bugs from ordering: halos computed before the diagram re-sorted on `SendToFront` (now worked around by
  sorting inside the behavior, `LinkHalosBehavior.cs:85-87`); exact-type lookup missing subclasses (fixed,
  `Diagram.cs:306-312`); unsubscription leaks in `VirtualizationBehavior` and `EventsBehavior` (fixed, leak tests in
  `tests/*/Memory`).
- `UnregisterBehavior<T>` is exact-type only (`Diagram.cs:317-325`) while `GetBehavior<T>` is not: unregistering the
  base type of a registered subclass silently does nothing.
- `KeyboardShortcutsBehavior.OnDiagramKeyDown` is `async void` and catches only when `ActionFailed` has a subscriber
  (`KeyboardShortcutsBehavior.cs:76-97`); without one, an exception in a shortcut escapes onto the renderer's
  synchronization context (on Server this normally terminates the circuit; not verified here).
- Behaviors scale with the diagram because the event bus offers no spatial or incremental queries (P9, section 4).

**Severity.** Contract for the `Behavior` base and registration API; High for the bugs.

**Options.**

| Option | Gains | Costs |
|---|---|---|
| A. An input pipeline: behaviors declare a phase/priority (`Selection` < `Manipulation` < `Navigation` < `Default`) and handle a pointer event by returning `Handled` or capturing the gesture; the diagram dispatches in order and stops when handled; services (`IDiagramServices`: selection, ongoing link, spatial index) replace `GetBehavior` lookups | Order is explicit and stable across replace/re-register; gestures cannot conflict (pan vs selection box vs drag); custom behaviors insert at a phase | Breaking for custom behaviors (new base methods instead of subscribing to events); M |
| B. Keep events, add `RegisterBehavior(behavior, before: typeof(X))` and `ReplaceBehavior<TOld>(new)` keeping position | Fixes NodeCAD's case; non-breaking | Still implicit coupling; M-small |
| C. Keep | — | Re-register dance in every customizing consumer |

**Recommendation.** A for pointer input (the source of the ordering bugs), with B's `Replace` as the migration helper.
Model-change subscriptions (halos, virtualization) move to the dirty-flush of P3 rather than raw `Changed`.

### P5. A link exists in the diagram while it is being dragged

**Design.** `DragNewLinkBehavior` adds the link to `Diagram.Links` on pointer down with a `PositionAnchor` target
(`DragNewLinkBehavior.cs:85-97`, `StartFrom` at `:39-51`) and removes it on release over nothing when
`RequireTarget` (`:140-143`, test `Behavior_ShouldRemoveLink_WhenMouseUpOnCanvasAndRequireTargetIsTrue`).

**Consequences.** Every `Links.Added` subscriber sees half-made links: NodeCAD's `OnLinkAdded` distinguishes "user
started dragging" from "link is complete" by inspecting anchors and subscribing to `TargetChanged`
(`Services/DiagramStateService.cs:326-354`), and locks links on arrival when read-only (`:275`). Virtualization,
halos, `ObstacleRouter.PlacedRoutes` (`ObstacleRouter.cs:144-153`) and undo stacks all process the transient link.

**Severity.** Contract (event semantics of `Links.Added`), Medium effort.

**Options.** A. Keep the ongoing link out of `Links`: the behavior owns it and a `DragNewLinkPreview` renderer draws
it; on attach, the completed link is added once (and `Links.Added` means "a link exists"). Breaking for code that
relies on the current events; S–M. B. Keep, add `BaseLinkModel.IsPending` and `LinkCompleted` events. Non-breaking,
but every subscriber must still filter. **Recommendation:** A.

### P6. Groups: a node that is not in `Nodes`, with children nested in its element

**Design.** `GroupModel : NodeModel` lives in `Diagram.Groups`; its children live in `Diagram.Nodes` but are rendered
inside the group's element by `GroupNodes.razor` (canvas skips nodes with `Group != null`,
`DiagramCanvas.razor:16`, `:57`). Moving a group moves each child "silently" and then refreshes everything
(`GroupModel.cs:98-123`).

**Consequences.** Children cannot interleave with models outside the group: a link can never be drawn between a
group's child and its sibling outside the group's element, and HTML groups sit in the HTML layer while links to their
children are in the SVG layer. Moving a group re-renders every descendant because children are positioned in
absolute coordinates inside a counter-translated `.children` element (`GroupNodes.razor:12`, `:28`). Code iterating
`Diagram.Nodes` misses groups (`ObstacleRouter.GetObstacles` intentionally, `DiagramSvgExporter.Bounds` handles both;
user code must remember). Grouping by shortcut cannot nest (guide `08-groups.md:17`). NodeCAD wrote its own
`DiagramGroupModel` (159 lines) with auto-layout and resize handles.

**Severity.** Contract for the membership rule; Medium for rendering.

**Options.** A. Flat rendering: every node/group is drawn by the canvas in drawing order; a group's `Order`/`Layer`
just places it below its children by default; group membership is a model relation only. Breaking for custom group
widgets that render `GroupNodes`; M. B. Keep nesting, add an option to render flat. **Recommendation:** A together
with P1 (a single SVG surface makes it natural). Also decide whether groups appear in `Nodes` (simpler iteration) or
stay separate (document it as the rule).

### P7. `Visible` has two owners, and virtualization does not reduce component work

**Design.** `VirtualizationBehavior` hides off-screen models by setting `Model.Visible = false` and remembers them in
`_hidden` (`VirtualizationBehavior.cs:126-151`). A hidden model's renderer still exists and renders nothing.

**Consequences.**
- A model the user hid while virtualization had already hidden it is shown again when it scrolls into view
  (`_hidden.Remove(model)` → `Visible = true`, `:143-144`). The tests cover a model the user hid while it was
  in view (`Virtualization_ShouldNotShowAModelTheUserHid`), not one hidden after virtualization had hidden it.
- Export toggles live models' `Visible` to un-hide them (`DiagramSvgExporter.cs:38-49`), re-rendering them twice on
  the live canvas.
- The first render cannot virtualize (`Container` is null until after it), so every model renders, then the hidden
  ones render again to nothing: at 1 000 nodes the first render rendered 18 350 components with virtualization on and
  17 374 with it off. A pan still walks every component (section 4.3).
- Options are read at event time; switching an option does nothing until `CheckVisibility()` (P8).

**Severity.** Medium; the flag split is an API change.

**Options.** A. Separate `Visible` (user) from `IsInViewport` (computed); the canvas renders components only for
models in the viewport (component-level virtualization), driven by a spatial index (section 4). Breaking only for code
that reads `Visible` to detect virtualization; M. B. Keep the flag, add a `HiddenBy` reason. S. **Recommendation:** A;
it is also the main lever on pan cost at large sizes.

### P8. Options are plain mutable objects

**Design.** `DiagramOptions` and its sub-options are POCOs read at use time (`Options/*.cs`); routers and path
generators are instances inside them.

**Consequences.** No component or behavior can react to an option change: virtualization (above), `LabelsOnTop`,
`SvgLabels`, `HaloColor`, `DefaultRouter` (changing the router re-routes nothing until each link refreshes).
NodeCAD's diagram-type options (planned phase 8 of its SVG plan) will need to refresh everything manually.

**Severity.** Low–Medium; can be fixed later without breaking by adding change events.

**Options.** A. Raise `Changed` from options (or make them records replaced as a whole via `Diagram.SetOptions`) and
let the diagram mark affected models dirty. B. Document "call X after changing options". **Recommendation:** A,
non-breaking, after P3.

### P9. Routing is a side effect of `Refresh()`, one link at a time

**Design.** See 1.6. `Router.GetRoute(Diagram, BaseLinkModel)` is called inside each link's `Refresh()`. Routers read
the whole diagram each time: `ObstacleRouter` collects obstacles from every node, the current polylines of every other
link, builds the axes, then searches (`ObstacleRouter.cs:62-81`, `:88-89`, `:144-153`).

**Consequences.**
- Moving a node into another link's way does not re-route that link (guide `06-routing.md:55`); results depend on
  the order links were routed; `RouteAll` with re-route passes exists to compensate (`ObstacleRouter.cs:40-49`).
- Per-route setup is linear in the diagram, and every link refresh repeats it; a drag re-routes the dragged node's
  links on every pointer move (section 4.4: 129 ms per drag move at 1 000 nodes).
- `LinkGeometry.Polyline` re-resolves both anchors for every link each time halos or the obstacle router need it
  (`LinkGeometry.cs:13-20`), duplicating work `GeneratePath` already did.

**Severity.** Contract for the router signature if routing becomes a pass; High for cost.

**Options.**

| Option | Gains | Costs |
|---|---|---|
| A. Routing pass: the flush (P3) collects geometry-dirty links, plus links whose route intersects a moved/resized node (spatial index), and calls `Router.Route(RoutingContext, IReadOnlyList<BaseLinkModel>)`; the context holds the obstacle index and placed routes built once per pass | Correct invalidation (links re-route when an obstacle moves into them); setup shared across links; `RouteAll` becomes "mark all dirty"; deterministic order | New router API (keep `GetRoute` as a per-link adapter for simple routers); M |
| B. Keep per-link routing, cache obstacles/placed routes in the router, invalidated by diagram events | Faster per route; non-breaking | Still order-dependent, still no re-route of bystander links; cache invalidation via events is the bug class of P4; M |
| C. Keep | — | Obstacle routing stays a batch-only feature |

**Recommendation.** A; define `RoutingContext` before 1.0 even if the first implementation is naive.

### P10. Public mutable surfaces bypass invariants

- `BaseLinkModel.Vertices` and `Labels` are public `List<T>` (`BaseLinkModel.cs:91`, `:96`): `RemoveVertex` and
  `VertexRemoved` exist, yet `LinkWidget` inserts with `Link.Vertices.Insert` (`LinkWidget.razor.cs:69`) and NodeCAD
  removes with `Vertices.Remove`/`RemoveAt` (`Services/DiagramStateService.cs:252`, `:2362`), so `VertexRemoved` does
  not fire for them.
- `MovableModel.Position` public setter (`MovableModel.cs:36`); `PortModel.Position/Size/Initialized` public
  setters (`PortModel.cs:49-59`).
- `Diagram.SuspendRefresh` and `SuspendSorting` are public settable flags (`Diagram.cs:148`, `:154`); NodeCAD and the
  library's own `DeleteSelection` toggle them by hand.

**Severity.** Contract (these are API shapes). S effort each: expose `IReadOnlyList` with `Add/Insert/Remove` methods
that raise events and mark dirty; make `Position` setters go through `SetPosition` or be internal; make the suspend
flags read-only and keep `Batch`.

### P11. Exception safety of refresh suspension

`Diagram.Batch` was fixed (CHANGELOG, `Diagram.cs:178-198`), but two other places set `SuspendRefresh` without
`finally`:
- `ZoomToFit` (`Diagram.cs:347-358`): with a zero-width container (canvas in a hidden tab) `xf` is 0 and
  `SetZoom(0)` throws (`:390-391`) with refresh left suspended; the diagram never redraws again.
- `KeyboardShortcutsDefaults.DeleteSelection` (`KeyboardShortcutsDefaults.cs:14-40`): awaits user-supplied
  constraints (which may show a dialog) with refresh suspended for the whole wait, and leaves it suspended if one
  throws.

Docs are stale in the other direction: guide `11-performance.md` ("Trap: `Batch` is not exception-safe") and
NodeCAD's `InOneRefresh` comment (`Services/DiagramStateService.cs:1680-1683`) describe the fixed bug.

**Severity.** High (silent dead canvas), S effort, non-breaking. Fix now.

### P12. The JavaScript observer

`script.js` observes `document.body` with a subtree `MutationObserver` that calls `getBoundingClientRect` on every
canvas after any DOM mutation in the page (`script.js:7-18`), i.e. a forced layout per render batch of any component
on the page. Scroll is tracked only on `window` (`:51-57`); a canvas inside a scrolled container keeps a stale
`Container` offset (wrong pointer coordinates) until some DOM mutation happens. The element id is recovered from
Blazor's private `_bl_` attribute (`:21`), an undocumented Blazor detail. `for (id in ...)` assigns an implicit global
(`:8`, `:52`). Each node is a separate `observe`/`unobserve` interop call.

**Severity.** Medium, non-breaking. Options: observe ancestors' scroll with a capturing `scroll` listener on
`document`, drop the body `MutationObserver` for an `IntersectionObserver`/`ResizeObserver` on the canvas only, pass
ids explicitly, and batch observe/unobserve (one call per render batch). S–M.

### P13. No time model: animation

**Design.** Every visual change is a model mutation, applied instantly, rendered by Blazor. There is no clock, no
transition state and no hook to add classes to the renderer wrappers beyond `selected`/`locked`/`grouped`
(`NodeRenderer.cs:117-120`).

**What the owner wants and what each needs:**

| Effect | Can be CSS-only? | Needs from the architecture |
|---|---|---|
| Animated link flow (marching dashes) | Yes: `stroke-dasharray` + `@keyframes` on `stroke-dashoffset` | A way to put a class on a link's path (a `LinkModel.CssClass` or widget parameter); no per-frame .NET work |
| Highlight pulse | Yes: keyframes on a class | Same: model-driven classes on renderer wrappers |
| Animated zoom/pan (zoom-to-fit, focus node) | Partly: a CSS `transition` on the layers' `transform`, toggled on for programmatic moves and off for user drags | `Pan`/`Zoom` jump at once, so pointer conversion during the transition uses the final values; acceptable for 200–300 ms, or interpolate in .NET (see below) |
| Node position transitions (layout changes) | No: links are computed in .NET at the final positions and would jump while nodes glide | Interpolated model positions per frame (a frame loop) so links follow, or link paths computed client-side |

**Severity.** Contract only for the hooks: classes on renderers (S, additive) and a frame-aligned flush (part of P3).
A tween API (`diagram.Animate(node, to, duration)`) can come later without breaking if the flush exists. Performance
implications are in section 4.6.

### P14. Smaller API issues worth fixing before 1.0

- `LinkWidget.Link` is typed `LinkModel` (`LinkWidget.razor.cs:21`) while `LinkRenderer` falls back to `LinkWidget`
  for any `BaseLinkModel` (`LinkRenderer.cs:59`): a custom `BaseLinkModel` without a registered widget fails at
  runtime.
- The component registry lives on the model object (`BlazorDiagram.RegisterComponent`), so a diagram model cannot be
  rendered by two differently configured canvases and Core cannot be shared between renderers. Consider moving it to
  a canvas parameter or DI.
- `SvgPathProperties.SvgPath` is part of Core's public API (`PathGeneratorResult`), tying the 1.0 contract to a
  third-party package's versioning. Wrap it or own the type.
- `BoundaryControl.GetPosition` mutates `Bounds` as a side effect (`BoundaryControl.cs:22`), the same pattern that was
  removed from `ArrowHeadControl.Angle` (CHANGELOG "Changed").
- `Renderer._shouldRender = true` initial values in `LinkRenderer.cs:19`, `GroupRenderer.cs:24`, `PortRenderer.cs:25`,
  `LinkVertexRenderer.cs:18` are never consumed by the first render (Blazor does not call `ShouldRender` for it), so
  the first canvas re-render after mount re-renders every link once (measured: 5 685 extra component renders at 1 000
  nodes, section 4.2). Non-breaking, S.

---

## 4. Performance architecture

### 4.1 Cost model in one table

Measured at commit `76c2470` with the harness described at the top (grid diagram identical to the benchmarks:
100×60 nodes, 4 measured ports each, about 2.8 links per node). Times are .NET JIT on a desktop CPU; on WebAssembly
expect several times more (not measured).

| Operation | 50 nodes | 200 | 1 000 | 5 000 | Scaling | Dominant cost |
|---|---:|---:|---:|---:|---|---|
| Hover move, no gesture | 0 batches | 0 | 0 | 0 | O(1) | one event round trip into .NET |
| Pan move (renders) | 0.27 ms | 1.5 ms | 2.4 ms | 7.3–8.4 ms | O(N+L) | canvas re-render walks every child component |
| Pan move, virtualization on (nodes) | — | — | 5.7 ms, 64 KB | 10.0 ms, 314 KB | O(N+L) | the walk + bounds of every model |
| Pan, model only, virtualization on nodes+links | 0.10 ms | 0.52 ms | 4.1 ms, 1.2 MB | 9.0 ms, 6.2 MB | O(N+L) | `link.GetBounds()` via `SvgPath.GetBBox` allocates |
| Drag move, 1 node with 6 links (renders) | 0.22 ms | 0.20 ms | 0.07 ms | 0.07 ms | O(links of the node) | 14 components, 19 DOM edits, 31 KB |
| Drag move, 20 selected nodes | 2.1 ms | 2.8 ms | 1.1 ms | 1.1 ms | O(selected + their links) | about 170 components |
| Drag move, `OrthogonalRouter` (model only) | 1.2 ms | 1.0 ms | 0.45 ms | 0.29 ms | per link, bounded grid | 242 KB per move |
| Drag move, `ObstacleRouter` | 45 ms | 23 ms | 129 ms | 336 ms (2 000) | O(links × diagram) | per-route setup + search |
| One route, `ObstacleRouter` | 0.05 ms | 13 ms | 48 ms | 109 ms (2 000) | ~O(N) per route | obstacles, placed routes, grid |
| Drag end (`Moved`), virtualization on links | 0.09 ms | 0.34 ms | 2.9 ms, 1.1 MB | 6.5 ms, 5.9 MB | O(L) | rechecks every link (`VirtualizationBehavior.cs:98-107`) |
| Select (unselect others), model | 18 µs | 235 µs | 677 µs | 883 µs | O(N+L) | `GetSelectedModels` scans all (`Diagram.cs:206-231`) |
| `SendToFront`, model | 43 µs | 261 µs | 669 µs | 2.2 ms | O(N log N) | `Max` + full sort (`Diagram.cs:489-505`) |
| Add + remove one node, model | 18 µs | 68 µs | 81 µs | 430 µs | O(N) | `GetMaxOrder` (`Diagram.cs:547`), `List.Remove` |
| Add one node with 4 unmeasured ports, renders | 1 batch, 5 JS calls at every size | | | | O(N+L) | canvas walk; 1 observe + 1 `getBoundingClientRect` per port |
| First render, HTML nodes | — | 3 326 components | 17 374, 70 MB alloc, 1 001 JS calls | 88 594, 336 MB, 5 001 JS calls | O(N+L) | one observe per node |
| First render, SVG controlled nodes | — | — | 13 374, 50 MB, 1 JS call | — | O(N+L) | no per-node interop |
| Remove half the nodes in `Batch` | | 844 batches, 100 JS calls (100 removed) | 4 363 batches, 500 JS calls (500 removed) | | O(removed × their links) batches | per-model `Changed` outside event dispatch |
| Build (benchmarks/README) | 0.74 ms | 4.1 ms | 50.7 ms, 13.7 MB | 1.5 s, 69 MB (one run, this harness) | ~O(N log N) to O(N²) | order bookkeeping, link refresh |

Not repeated here because a PR in flight addresses them (per the owner): `LinkHalosBehavior` moving one node
(4.23 s, 6.8 GB at 1 000 nodes in the README → about 0.1 ms) and `ObstacleRouter.RouteAll` (about 4.3 min → 8.7 s at
1 000 nodes). What remains after that PR is discussed in 4.4.

### 4.2 Render pipeline

**Components per element.** HTML node with the default widget: `NodeRenderer` + widget + one `PortRenderer` per port
(6 for a 4-port node). Link: `LinkRenderer` + `LinkWidget`, plus one `LinkLabelRenderer` per label and one
`LinkVertexRenderer` per vertex, plus a `LinkLabelsRenderer` per link when `LabelsOnTop`. At 1 000 nodes / 2 842 links
that is about 11 700 live components; the first render rendered 17 374 because of P14's unconsumed `_shouldRender`
(every link renders twice).

**What re-renders.**
- *Pointer move with no gesture:* the canvas's `@onpointermove` handler runs, behaviors return early, `ShouldRender`
  of the canvas is false: 0 render batches. On Blazor Server it is still one SignalR message per mouse move over the
  canvas (`DiagramCanvas.razor:5`), and `mouseenter`/`mouseleave` on every node and link selection path add more.
- *Pan or zoom step:* `UpdatePan` → `Diagram.Refresh()` → the canvas re-renders. It walks the ordered list of every model
  twice (SVG loop and HTML loop, `DiagramCanvas.razor:14-28`, `:48-61`), and because each child takes a model
  object as a parameter, Blazor calls `SetParametersAsync` on every child renderer, which then declines in
  `ShouldRender`. 1 component rendered, 4 DOM edits (the two `transform` styles), but O(N+L) work: 0.27 ms at 50,
  2.4 ms at 1 000, 7.3–8.4 ms at 5 000 nodes. The only thing that changed is two CSS transforms.
- *Drag move:* node + its links: 14 components, 19 edits for one node with 6 links; about 170 for 20 nodes. Grid
  size does not matter. This is the design working as intended.
- *Selection click:* every selected model is unselected and refreshed individually (`Diagram.cs:238-283`), plus an
  O(N+L) scan.
- *Node add:* `Nodes.Add` refreshes the diagram (canvas walk, O(N+L)), the new renderers render, then
  `OnAfterRenderAsync` makes 1 observe call and one `getBoundingClientRect` per port.

**Render batches.** Inside a Blazor event handler all `StateHasChanged` calls coalesce into one batch. Outside one
(JS callbacks such as `OnResize`, `async` continuations in `PortRenderer`, timers, code run from a service), each
model `Changed` triggers its own batch. Measured: one measured node resizing via the `OnResize` path costs 7 batches
and 4 JS calls; removing 500 nodes inside `Batch` costs 4 363 batches. On WebAssembly each batch is an interop call
that applies DOM edits; on Server each is a message with its own acknowledgement.

**Blazor Server vs WebAssembly.** The interaction loop is in .NET by design: every pointer move of a drag, pan or new
link goes to .NET and back. On Server that makes drag latency at least one network round trip per frame and puts
pointer-move traffic on the circuit even while hovering; on WebAssembly it is local but runs on the (interpreted
unless AOT) .NET runtime. Each JS interop call (`observe`, `unobserve`, `getBoundingClientRect` per port) is a round
trip on Server. The architecture choices below that remove interop and batch renders matter more on Server.

### 4.3 Model-side costs

- **Event fan-out per drag move:** 1 node `Changed`, 6 link `Changed` (each runs router + anchors + path generator),
  0 port `Changed`, 0 diagram `Changed`; 6 routes, 19.5 KB allocated in the model (measured). Each link `Changed`
  also reaches every subscriber: halos, controls (`ControlsLayer.cs:33`, per model with controls), the navigator.
- **Geometry is recomputed on every refresh, never cached:** `BaseLinkModel.Refresh` always routes
  (`BaseLinkModel.cs:231-250`), including repaint-only refreshes (selection, halo updates via `SetHalos`). Anchors are
  resolved again by `LinkGeometry.Polyline` for halos and obstacle routing.
- **O(N) on single-item operations:** `OnSelectableAdded` takes `GetMaxOrder()` with LINQ `Max` over all models
  (`Diagram.cs:547`, `:521-524`), so building N models is O(N²) in that bookkeeping; `SendToFront` does `Max` plus a
  full sort; `BaseLayer.Remove` and `_orderedSelectables.Remove` are `List.Remove`.
- **`NavigatorWidget`** subscribes to `Diagram.Changed` and every node's `Changed`, recomputing the union of all node
  bounds and re-rendering all node rectangles on each (`NavigatorWidget.razor.cs:69-130`): a 20-node drag move costs
  20 × O(N) recomputes and re-renders. NodeCAD wrote its own `MiniMapNavigator` that coalesces refreshes with a delay
  (`Components/Diagrams/Widgets/MiniMapNavigator.razor:100-120`).
- **Virtualization** checks every model on each pan/zoom (`VirtualizationBehavior.cs:39-71`) and every link on each
  drag end (`:98-107`); link bounds come from `SvgPath.GetBBox()` and allocate (1.2 MB per pan step at 1 000 nodes
  with `OnLinks`). It hides DOM but not components, so it does not reduce the pan walk (5.7 ms with it vs 2.3 ms without
  at 1 000 nodes) and does not reduce the first render (all models render, then hidden ones render again).

### 4.4 Routing after the in-flight fixes

Per route, `ObstacleRouter.GetRoute` (`ObstacleRouter.cs:57-82`):
1. collects every visible node's bounds (O(N), `:88-89`);
2. collects the current polyline of every other link (O(L), re-resolving anchors, `:144-153`);
3. builds axes from all inflated obstacle edges (O(N log N), `:132-141`);
4. searches a grid of up to (2N+k)² cells; for each edge it tests every block (`blocks.Any`, `:200`) and every
   segment of every placed route (`Penalty`, `:227-254`).

Steps 1–3 are identical for every link in a `RouteAll` pass and for the 6 links of a dragged node; step 4's inner
tests are linear scans. Measured per route: 13 ms at 200, 48 ms at 1 000, 109 ms at 2 000 nodes; a drag move of a node
with 6 links: 129 ms at 1 000 nodes. Whatever the in-flight PR changes, an interactive drag with `ObstacleRouter` at
these sizes needs:
- a spatial index (R-tree or uniform grid) of obstacles and placed segments, built once per pass and updated
  incrementally, so steps 1–2 become local queries and step 4's tests touch only nearby blocks/segments;
- a corridor-limited grid (axes from obstacles within the bounding box of the two ends plus a margin, widened only
  if no path is found);
- routing during a drag as preview (cheap router or straight stubs) and the full obstacle route on drag end
  (`Moved`), which is what `RouteAll`'s guidance already implies.

All of these need P9's routing context; with the current per-link signature each router must rebuild or cache
diagram-wide state behind events.

### 4.5 Memory

- Retained per model: one renderer component (plus widget, plus one per port/label/vertex), its event subscriptions,
  and for measured nodes a `DotNetObjectReference` and a JS-side `tracked[id]` entry (`script.js:30-33`). For 1 000
  nodes and 2 842 links: about 11 700 components.
- Allocations per frame: drag move 31 KB (1 node), about 400 KB (20 nodes); pan 1 KB without virtualization, 64–314 KB
  with it (node bounds), 1.2–6.2 MB with `OnLinks`.
- First render allocates 70 MB at 1 000 nodes and 336 MB at 5 000 (render tree frames and diffs, not retained), which
  on WebAssembly means GC pauses during load.
- Leaks are covered by tests (`tests/Flow.Diagrams.Core.Tests/Memory/DiagramLeakTests.cs`,
  `tests/Flow.Diagrams.Tests/Memory/CanvasLeakTests.cs`).

### 4.6 Animation and render cost

- CSS-only effects (dash flow, pulses, a CSS transition on the layer transform) cost nothing in .NET after the class
  is set; the browser composites them. They need only class hooks (P13). Many simultaneous dash animations on large
  diagrams cost paint time in the browser (not measured).
- Geometry animation (node positions, link reflow) needs a .NET frame loop: per frame, the cost is a drag move for
  each animated node (route + path per attached link, renderer per node and link). At the measured 0.07 ms per
  one-node move that is cheap on WebAssembly for tens of nodes, but on Server a 60 Hz loop is 60 render batches per
  second per circuit. A frame-aligned flush (P3) makes the loop a scheduler concern rather than ad-hoc timers, and lets
  animations and drags share one "geometry dirty → recompute → one batch" path.
- Animated pan/zoom through `SetPan`/`SetZoom` per frame would cost a full canvas walk per frame (7–8 ms at 5 000 nodes
  in JIT) unless 4.7's option C lands first.

### 4.7 Options that change the performance envelope

| Option | Gains | Costs / breaking surface | Before 1.0? |
|---|---|---|---|
| A. Dirty tracking + one flush per turn/frame (P3) | Bulk edits become one batch (4 363 → 1 measured case); geometry computed once per flush; halos/routing incremental by construction | Breaking `Refresh()` semantics; M–L | Contract yes |
| B. Viewport component: move the layers' transform into a small child component that alone renders on pan/zoom; keep model lists in components that render only on add/remove/order changes | Pan/zoom becomes O(1) in .NET (from 7–8 ms per step at 5 000 nodes) | None public; S | No, but cheap: do it first |
| C. Component-level virtualization from a spatial index (P7) | Live components and the first render scale with what is on screen | `Visible` semantics; M | Flag split yes |
| D. Model-computed geometry, SVG surface (P1, P2) | No per-node/port interop (1 001 → 1 call at first render measured with SVG controlled nodes), headless export | Breaking (P1, P2); L | Yes |
| E. Shared spatial index service (nodes, link segments) used by routing, halos, virtualization, snapping (`FindNearPortToAttachTo` scans all ports, `DragNewLinkBehavior.cs:167-192`), selection box, hit testing | Turns several O(N)/O(L) scans into local queries | New internal service, exposed to custom behaviors; M | API shape yes if behaviors get it |
| F. Links rendered in chunks (one component per K links, or one for all non-selected links) | Fewer components (2 per link today), cheaper first render and canvas walk | Per-link widgets become per-chunk; custom link widgets need a different contract; M | Only if adopted |
| G. Drag frame scheduler: coalesce pointer moves to one model update per animation frame; on Server optionally drag a client-side preview in JS and commit on release | Bounded work per frame, no backlog on slow links/Server | JS preview duplicates drawing logic; M | No |
| H. Batch JS calls: one `observeMany`/`unobserveMany` per render batch; drop the body `MutationObserver` (P12) | Fewer interop calls on load/remove (500 per 500 removed nodes today) | None; S | No |

### 4.8 Where the cliffs are

1. `ObstacleRouter` interactively above about 200 nodes (per route linear in the diagram; P9, 4.4).
2. Pan/zoom above a few thousand models (O(N+L) canvas walk per step; option B).
3. Bulk changes outside event handlers (one render batch per model `Changed`; P3) — worst on Server.
4. First load of measured HTML nodes (one interop per node, one per port, then resize callbacks each producing
   several batches; P2).
5. Virtualization with `OnLinks` (allocating link bounds per pan step).
6. Navigator during multi-node drags.

---

## 5. Proposed order of work before 1.0

Sizes: S ≈ days, M ≈ 1–2 weeks, L ≈ several weeks, for one developer familiar with the code.

| Step | Work | Size | Breaking | Depends on |
|---|---|---|---|---|
| 1 | Exception safety (`ZoomToFit`, `DeleteSelection`, `async void` shortcut), fix stale docs, `_shouldRender` initial values, `LinkWidget` parameter type | S | No | — |
| 2 | Viewport component for pan/zoom (4.7 B); batch JS observe/unobserve and replace the body `MutationObserver` (P12) | S–M | No | — |
| 3 | Decide and implement change tracking (P3): notifying properties, `Batch` defers model `Changed`, `Changed` carries a reason, one flush per turn | L | Yes | — |
| 4 | Model-owned geometry (P2): relative port placement as default, `Port.Position` derived, measured size opt-in | M–L | Yes | 3 helps |
| 5 | Single SVG surface with HTML islands, flat group rendering (P1, P6) | L | Yes | 4 |
| 6 | Input pipeline for behaviors with phases and handled/capture; `ReplaceBehavior` (P4); transient link out of `Links` (P5) | M | Yes | — (parallel with 3) |
| 7 | Routing context and routing as a pass in the flush; spatial index shared with halos (P9, 4.4, 4.7 E) | M | Yes (router API) | 3 |
| 8 | API hygiene: read-only collections with methods, `Position` setters, suspend flags, `SvgPath` in Core, `Visible` vs in-viewport flag (P7, P10, P14) | S–M | Yes | — |
| 9 | Class hooks on renderers for CSS animation (P13) | S | No | — |

Can wait until after 1.0 without breaking anything, provided steps 3, 4 and 7 define the contracts: component-level
virtualization (4.7 C, once the flag split is in), observable options (P8), frame-aligned flush and a tween API
(P13), chunked link rendering (4.7 F), drag preview on Server (4.7 G), corridor-limited obstacle routing.

If only part of this fits before 1.0, the minimum that avoids an expensive 2.0 is: step 3's contract (properties
notify, `Changed` with reason), step 4's contract (port geometry relative and derived), P1's decision on
`SvgNodeModel` (even if implementation follows), and the router signature of step 7.

---

## What I could not determine

- Browser-side costs: layout and paint per render batch, the forced layouts from the body `MutationObserver`, and
  CSS animation paint cost. All timings above are the .NET side under JIT.
- WebAssembly and Blazor Server numbers: no app was run. The Server statements (one message per pointer move and per
  render batch, one round trip per interop call) follow from how Blazor Server works, not from measurement here.
- Whether an unhandled exception from `KeyboardShortcutsBehavior`'s `async void` handler terminates a Server circuit
  in practice; inferred, not reproduced.
- The in-flight halos/`RouteAll` PR's code: its effect on per-route cost and drag-time obstacle routing is unknown;
  the `ObstacleRouter` numbers in 4.1 and 4.4 are from this commit.
- NodeCAD's own perf telemetry (`perf.db`) was not consulted; NodeCAD evidence here is from its code, comments, tests
  and plan.
