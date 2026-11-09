namespace Flow.Diagrams.Export;

public sealed record SvgExportOptions
{
    /// <summary>Space around the drawing, in diagram units.</summary>
    public double Margin { get; init; } = 10;

    /// <summary>
    /// Unit of the document's width and height, one diagram unit each ("mm" for a diagram drawn in millimetres);
    /// empty for unitless, which SVG reads as pixels.
    /// </summary>
    public string Unit { get; init; } = "";

    /// <summary>Fill of a rectangle behind the drawing; null for a transparent background.</summary>
    public string? Background { get; init; } = "white";

    /// <summary>Stylesheet embedded in the document, for widgets that draw with CSS classes.</summary>
    public string? Css { get; init; }
}

/// <param name="Svg">A standalone SVG document.</param>
/// <param name="HtmlModels">
/// Models drawn as HTML. Nodes and groups are left out of the document; labels stay in it as <c>foreignObject</c>,
/// which most SVG-to-PDF converters drop (set <c>Options.Links.SvgLabels</c>).
/// </param>
public sealed record SvgExport(string Svg, IReadOnlyList<Core.Models.Base.Model> HtmlModels);
