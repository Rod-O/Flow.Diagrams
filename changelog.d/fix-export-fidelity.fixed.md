- `DiagramSvgExporter`: text drawn by widgets without a `font-family` (node names, tags) came out in the viewer's
  default face, usually a serif, because the document has no page CSS. The root now sets `SvgExportOptions.FontFamily`.
- `DefaultSvgLinkLabelWidget` no longer hard-codes `Helvetica, Arial, sans-serif`: labels inherit the font like node
  text, from the page CSS on screen and from `SvgExportOptions.FontFamily` in an export, so on screen they match the
  nodes. Apps whose page font is not sans-serif (or that set none) now see SVG labels in that font. Breaking for
  appearance; style `.diagram-link-label text` to keep the old face.
