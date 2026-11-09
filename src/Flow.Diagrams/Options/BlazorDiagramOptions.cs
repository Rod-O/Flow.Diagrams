using Flow.Diagrams.Core.Options;

namespace Flow.Diagrams.Options;

/// <summary>
/// Options of a <see cref="BlazorDiagram"/>. The nested option groups are fixed instances: configure them by setting their
/// properties, not by replacing them.
/// </summary>
public class BlazorDiagramOptions : DiagramOptions
{
    /// <summary>
    /// CSS <c>z-index</c> of the SVG layer, which holds links, SVG nodes and SVG groups. Equal to <see cref="NodesLayerOrder"/> by
    /// default, in which case the HTML layer, which comes later in the markup, draws above it.
    /// </summary>
    public int LinksLayerOrder { get; set; } = 0;
    /// <summary>CSS <c>z-index</c> of the HTML layer, which holds nodes and groups that are not SVG; see <see cref="LinksLayerOrder"/>.</summary>
    public int NodesLayerOrder { get; set; } = 0;

    /// <summary>Zoom options.</summary>
    public override BlazorDiagramZoomOptions Zoom { get; } = new();
    /// <summary>Link options, including the default link colors.</summary>
    public override BlazorDiagramLinkOptions Links { get; } = new();
    /// <summary>Group options.</summary>
    public override BlazorDiagramGroupOptions Groups { get; } = new();
    /// <summary>Constraint options.</summary>
    public override BlazorDiagramConstraintsOptions Constraints { get; } = new();
    /// <summary>Virtualization options.</summary>
    public override BlazorDiagramVirtualizationOptions Virtualization { get; } = new();
}