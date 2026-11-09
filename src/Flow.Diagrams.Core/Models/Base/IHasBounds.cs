using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models.Base;

/// <summary>A model that occupies a rectangle in the diagram.</summary>
public interface IHasBounds
{
    /// <summary>
    /// The bounding rectangle in diagram units, or <c>null</c> when it is not known yet (a node not measured, a link
    /// not yet routed).
    /// </summary>
    public Rectangle? GetBounds();
}