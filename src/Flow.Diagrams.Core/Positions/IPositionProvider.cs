using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Positions;

/// <summary>Computes a point on a model, in diagram units. Used by <see cref="Flow.Diagrams.Core.Anchors.DynamicAnchor"/> to pick link attachment points and by controls to place themselves.</summary>
public interface IPositionProvider
{
    /// <summary>The point on <paramref name="model"/>; null when it cannot be computed yet (no size or no path). Implementations throw <see cref="DiagramsException"/> for a model of the wrong kind.</summary>
    public Point? GetPosition(Model model);
}