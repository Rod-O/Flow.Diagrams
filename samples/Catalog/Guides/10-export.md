# Export to SVG and PDF

Turn a diagram into a standalone SVG document that matches the screen, sized in real units if you need it, and convert that document to PDF.

{{example export-svg}}

## Export

[DiagramSvgExporter](xref:Flow.Diagrams.Export.DiagramSvgExporter) (new in Flow.Diagrams) renders the diagram with the same renderers and widgets the canvas uses, through Blazor's static `HtmlRenderer`, so the document draws what the canvas draws. It needs the app's services, because the renderers resolve theirs from it:

{{source export-svg ExportSvg.razor inject}}

{{source export-svg ExportSvg.razor export}}

The result is an [SvgExport](xref:Flow.Diagrams.Export.SvgExport): `Svg` is the document as a string, `HtmlModels` lists the models that could not be exported. Save the string, show it in an `<img>` as a data URL (as the example does), or send it to a server for conversion.

## What is exported

Only the SVG layer:

- [SvgNodeModel](xref:Flow.Diagrams.Models.SvgNodeModel)s and [SvgGroupModel](xref:Flow.Diagrams.Models.SvgGroupModel)s, drawn by their registered widgets, with whatever their ports draw;
- every link, with its markers, halos and labels, in the canvas's drawing order (`Layer`, then `Order`).

Not exported: HTML nodes and groups (plain `NodeModel` and `GroupModel` and their subclasses), controls, and the canvas widgets (grid, navigator, selection box). The HTML nodes and groups are listed in `HtmlModels` so you can warn the user instead of handing over a document with holes.

Link labels are HTML by default. They stay in the document as `foreignObject`, which browsers show but most SVG-to-PDF converters drop, and they are also listed in `HtmlModels`. Set `Options.Links.SvgLabels = true` to draw them as SVG text.

Two more things the document inherits from the live diagram:

- **State.** Widgets draw from the model, so a selected node exports in its selected colour if its widget draws one.
- **Visibility.** Only visible models are exported. With virtualization on, models outside the viewport are hidden and therefore missing from the export. Disable virtualization and set `Visible = true` on the hidden models before exporting.

## Size and units

The document is cropped to the exported models plus `Margin`; pan and zoom do not matter. One diagram unit is one unit of the document: the `viewBox` is in diagram units, and `width` and `height` are the same numbers followed by `Unit`. Draw a panel in millimetres, export with `Unit = "mm"`, and a converter prints it at true size.

{{example export-units}}

{{source export-units ExportUnits.razor options}}

[SvgExportOptions](xref:Flow.Diagrams.Export.SvgExportOptions):

| Option | Default | Effect |
|---|---|---|
| `Margin` | `10` | Space around the drawing, in diagram units. |
| `Unit` | `""` | Unit appended to `width` and `height`: `"mm"`, `"cm"`, `"in"`. Empty means unitless, which SVG reads as pixels. |
| `Background` | `"white"` | Fill of a rectangle behind the drawing; `null` for transparent. |
| `Css` | `null` | Stylesheet embedded in a `<style>` element. |

## CSS in the exported document

The document carries none of your page's stylesheets: not `style.css`, not your app's CSS, not Blazor scoped CSS (`.razor.css`), not `<style>` blocks inside components. Anything a widget draws through a CSS class looks different, or invisible, in the export. Two ways out:

- Draw with presentation attributes (`fill`, `stroke`, `font-size`) in the widget. This is what the examples do, and every SVG consumer reads them.
- Pass the rules the document needs in `Css`:

{{source export-units ExportUnits.razor css}}

## Designing nodes that export

An exportable node is an `SvgNodeModel` with a widget that emits SVG and styles it with attributes. Set the node's size in the model and `ControlledSize = true`. A measured SVG node grows on every render, and the exporter has no browser to measure with anyway.

{{source export-units ExportUnitsWidget.razor}}

In Razor, an SVG `<text>` directly inside `@if` or `@switch` fails to compile (RZ1023); wrap it in `<g>`, as above. For groups, an `SvgGroupModel` with its own widget exports the same way: see the [SVG groups example](examples/svg-groups).

## From SVG to PDF

The library stops at SVG. To get a PDF on .NET, one route is Svg.Skia, which loads an SVG into a SkiaSharp picture:

```
dotnet add package Svg.Skia
```

Load the exported string with `SKSvg.FromSvg`, open a PDF with SkiaSharp's `SKDocument.CreatePdf`, begin a page the size of the document in PDF points (72 per inch, so a millimetre is 72 / 25.4 points), draw `SKSvg.Picture` on it scaled to that page, and close the document. Run it on a server or in a tool; it does not need a browser.

What to expect from the conversion:

- **Fonts.** Text is drawn with fonts installed on the machine that runs the conversion, found through the operating system (fontconfig on Linux). A container image without fonts renders text in a fallback face or not at all. Install the fonts your widgets name, or name fonts the server has.
- **foreignObject** is dropped: HTML labels disappear (use `SvgLabels`).
- **paint-order** is ignored. That is why the built-in SVG label draws its halo as a separate `<text>` behind the text instead of using `paint-order`.

See also [Customizing](guides/customizing) for writing SVG widgets, and [Performance](guides/performance) for virtualization.
