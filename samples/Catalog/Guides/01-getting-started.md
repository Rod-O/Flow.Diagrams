# Getting started

Add Flow.Diagrams to a Blazor app, load its CSS and JavaScript, and draw a first diagram with two nodes and a link.

## The packages

| Package | What it contains |
|---|---|
| `Flow.Diagrams.Core` | The model, with no UI dependency: [`Diagram`](xref:Flow.Diagrams.Core.Diagram), nodes, ports, links, groups, anchors, routers, path generators, behaviors and options. |
| `Flow.Diagrams` | The Blazor side: [`BlazorDiagram`](xref:Flow.Diagrams.BlazorDiagram), [`DiagramCanvas`](xref:Flow.Diagrams.Components.DiagramCanvas) and the renderers, the default widgets, canvas widgets (grid, navigator, selection box), SVG export, and the CSS and JavaScript they need. Depends on `Flow.Diagrams.Core`. |
| `Flow.Diagrams.Algorithms` | Helpers that rewrite a diagram. Today it holds one: [`LinksReconnectionAlgorithms`](xref:Flow.Diagrams.Algorithms.LinksReconnectionAlgorithms), which moves links to the closest ports. |

All three target .NET 8, 9 and 10. A Blazor app needs only `Flow.Diagrams`; `Flow.Diagrams.Core` comes with it.

```
dotnet add package Flow.Diagrams
dotnet add package Flow.Diagrams.Algorithms   # optional
```

## Static assets

`Flow.Diagrams` ships three files under `_content/Flow.Diagrams/`. Reference them from the host page: `wwwroot/index.html` in a standalone WebAssembly app, `Components/App.razor` in a Blazor Web App.

```html
<head>
    <link href="_content/Flow.Diagrams/style.min.css" rel="stylesheet" />
    <link href="_content/Flow.Diagrams/default.styles.min.css" rel="stylesheet" />
</head>
<body>
    ...
    <script src="_content/Flow.Diagrams/script.min.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
```

In a Blazor Web App the last line is your existing `_framework/blazor.web.js`; keep `script.min.js` before it.

| File | Needed? | What breaks without it |
|---|---|---|
| `style.min.css` | Always | The canvas, its SVG and HTML layers and the nodes lose their positioning and pointer rules; nothing lines up. |
| `default.styles.min.css` | While you use the default widgets | The default node, its ports, the default group and the navigator lose their look. The default ports are placed on the node's sides by this file; without it they are not. |
| `script.min.js` | Always | The canvas, nodes and ports are measured through it. Without it every measurement call fails and links are never drawn. |

Each file also has an unminified version (`style.css`, `default.styles.css`, `script.js`).

## Namespaces

Add these to `_Imports.razor`, or to the component: `Flow.Diagrams` (`BlazorDiagram`), `Flow.Diagrams.Components` (`DiagramCanvas`), `Flow.Diagrams.Core.Models` (nodes, ports, links), `Flow.Diagrams.Core.Geometry` (`Point`, `Size`). Others you meet soon: `Flow.Diagrams.Core.Anchors`, `Flow.Diagrams.Core.Routers`, `Flow.Diagrams.Core.PathGenerators`, `Flow.Diagrams.Components.Widgets`, `Flow.Diagrams.Options` (`BlazorDiagramOptions`) and `Flow.Diagrams.Models` (`SvgNodeModel`).

## A first diagram

A diagram is two objects: a [`BlazorDiagram`](xref:Flow.Diagrams.BlazorDiagram) that holds the model, and a [`DiagramCanvas`](xref:Flow.Diagrams.Components.DiagramCanvas) that draws it. The canvas receives the diagram as a cascading value, not as a parameter, and fills its parent, so the parent needs a height.

{{source minimal-setup MinimalSetup.razor markup}}

The diagram is built in code. Here `Diagram` is a property of the catalog's example base class; in your own component it is a field or property you create the same way.

{{source minimal-setup MinimalSetup.razor model}}

{{example minimal-setup}}

What each part does:

- `new BlazorDiagram()` creates the model with the default behaviors already registered: selecting, dragging, drawing links from ports, panning, wheel zoom, the Delete key, click events and controls. Pass a [`BlazorDiagramOptions`](xref:Flow.Diagrams.Options.BlazorDiagramOptions) to configure it; see [The diagram](guides/the-diagram).
- `Diagram.Nodes.Add(...)` adds a node and returns it. A plain [`NodeModel`](xref:Flow.Diagrams.Core.Models.NodeModel) is drawn by the default widget, which shows its `Title`. Positions are in diagram units, top-left corner first.
- `AddPort(PortAlignment.Right)` adds a port on the right side and returns it. A link built from two ports attaches to them and follows them when the nodes move.
- `Diagram.Links.Add(...)` adds the link. You can add nodes and links in any order, before or after the canvas renders.

From here, [the first diagram example](examples/first-diagram) adds arrows and more ports, and [Nodes](guides/nodes) shows how to draw your own.

## Traps

- **No height, no diagram.** The canvas is `width: 100%; height: 100%`. Inside a parent with no height it is zero pixels tall and draws nothing, with no error.
- **Interactive rendering only.** The canvas measures itself, its nodes and its ports from the browser after the first render. Under static server rendering that never happens: no node is measured, no link is drawn, and input does nothing. In a Blazor Web App give the hosting component an interactive render mode.
- **The diagram is passed by cascade.** Without the `CascadingValue`, the canvas has no diagram and throws as soon as it initializes. `IsFixed="true"` is right as long as you keep the same `BlazorDiagram` instance; the canvas redraws when the diagram raises `Changed`, not when its parent re-renders.
- **Coordinates need a measured canvas.** `GetRelativeMousePoint` and `ZoomToFit` need `Diagram.Container`, which is set after the canvas's first render. Calling them in `OnInitialized` throws or does nothing.
