namespace Flow.Diagrams.Core.Options;

/// <summary>
/// The options of a <see cref="Diagram"/>, grouped by area. Subclasses can override the area properties to supply
/// their own option types.
/// </summary>
public class DiagramOptions
{
    /// <summary>
    /// Grid spacing in diagram units that dragged movable models snap to; <c>null</c> (the default) disables
    /// snapping.
    /// </summary>
    public int? GridSize { get; set; }
    /// <summary>Whether grid snapping applies to a node's center instead of its top-left corner.</summary>
    public bool GridSnapToCenter { get; set; }
    /// <summary>
    /// Whether holding Ctrl while clicking adds to the selection; when <c>false</c> every click replaces it.
    /// </summary>
    public bool AllowMultiSelection { get; set; } = true;
    /// <summary>Whether dragging the empty canvas (without Shift) pans the diagram.</summary>
    public bool AllowPanning { get; set; } = true;
    
    /// <summary>Zoom limits and wheel behavior.</summary>
    public virtual DiagramZoomOptions Zoom { get; } = new();
    /// <summary>Link routing, drawing and creation options.</summary>
    public virtual DiagramLinkOptions Links { get; } = new();
    /// <summary>Group options.</summary>
    public virtual DiagramGroupOptions Groups { get; } = new();
    /// <summary>Deletion veto hooks.</summary>
    public virtual DiagramConstraintsOptions Constraints { get; } = new();
    /// <summary>Options for hiding models that are outside the viewport.</summary>
    public virtual DiagramVirtualizationOptions Virtualization { get; } = new();
}