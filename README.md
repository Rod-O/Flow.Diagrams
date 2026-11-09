<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="logo-title-dark.png" />
    <img src="logo-title.png" alt="Flow.Diagrams" width="420" />
  </picture>
</p>

# Flow.Diagrams

A customizable diagrams library for Blazor: nodes, ports, links, groups, routers and path generators, rendered as HTML or SVG.

Flow.Diagrams is a fork of [Z.Blazor.Diagrams](https://github.com/Blazor-Diagrams/Blazor.Diagrams) by zHaytam, taken at 3.0.4.1 and maintained independently. Packages, assemblies and namespaces were renamed from `Blazor.Diagrams.*` to `Flow.Diagrams.*`; see [CHANGELOG.md](CHANGELOG.md) for what changed since the fork.

## Packages

| Project | Contents |
|---|---|
| `Flow.Diagrams.Core` | The model: diagram, nodes, ports, links, groups, behaviours, routers, path generators. No UI dependency. |
| `Flow.Diagrams` | Blazor components that render the model, plus the JavaScript and CSS they need. |
| `Flow.Diagrams.Algorithms` | Layout and graph algorithms over the model. |

## Using it

Reference `Flow.Diagrams` and add its assets to the host page:

```html
<link href="_content/Flow.Diagrams/style.min.css" rel="stylesheet" />
<link href="_content/Flow.Diagrams/default.styles.min.css" rel="stylesheet" />
<script src="_content/Flow.Diagrams/script.min.js"></script>
```

## Catalog

`samples/Catalog` is a gallery of 44 live examples, from a first diagram to a P&ID, each with its source and SVG export.

```
dotnet run --project samples/Catalog
```

`tools/catalog-previews` checks every example in a headless browser and regenerates the gallery's previews.

## Building and testing

```
dotnet test tests/Flow.Diagrams.Core.Tests
dotnet test tests/Flow.Diagrams.Tests
```

## License

MIT, see [LICENSE](LICENSE). The original copyright of Z.Blazor.Diagrams is kept there.
