using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Models;

/// <summary>
/// A node drawn in the diagram's SVG layer instead of the HTML layer: its widget renders SVG elements, not HTML, so the diagram
/// can export as vectors. The default widget is a blue 50 by 50 square; register another with
/// <see cref="BlazorDiagram.RegisterComponent{TModel, TComponent}(bool)"/>.
/// </summary>
public class SvgNodeModel : NodeModel
{
    /// <summary>Creates a node at <paramref name="position"/>; <c>null</c> means the origin.</summary>
    public SvgNodeModel(Point? position = null) : base(position)
    {
    }

    /// <summary>Creates a node with the given <paramref name="id"/> at <paramref name="position"/>; <c>null</c> means the origin.</summary>
    public SvgNodeModel(string id, Point? position = null) : base(id, position)
    {
    }
}