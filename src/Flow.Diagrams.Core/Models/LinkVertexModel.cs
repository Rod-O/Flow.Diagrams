using Flow.Diagrams.Core.ChangeTracking;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Models;

/// <summary>A draggable waypoint a link's route passes through.</summary>
public class LinkVertexModel : MovableModel
{
    /// <summary>
    /// Creates a vertex for <paramref name="parent"/> at <paramref name="position"/> (the origin when <c>null</c>).
    /// It is not added to the link; use <see cref="BaseLinkModel.AddVertex"/> to create and add one.
    /// </summary>
    public LinkVertexModel(BaseLinkModel parent, Point? position = null) : base(position)
    {
        Parent = parent;
    }

    /// <summary>The link the vertex belongs to.</summary>
    public BaseLinkModel Parent { get; }

    internal override ChangeTracker? Tracker => Parent.Tracker;

    /// <summary>Moves the vertex and refreshes the parent link so it is routed through the new position.</summary>
    public override void SetPosition(double x, double y)
    {
        base.SetPosition(x, y);
        Refresh();
        Parent.Refresh();
    }
}
