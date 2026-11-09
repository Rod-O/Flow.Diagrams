using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>Attaches to a node where the straight line from the other end of the link to the node's center crosses the node's outline, so the link points at the center and stops at the border.</summary>
public sealed class ShapeIntersectionAnchor : Anchor
{
    /// <summary>Creates an anchor on <paramref name="model"/>.</summary>
    public ShapeIntersectionAnchor(NodeModel model) : base(model)
    {
        Node = model;
    }

    /// <summary>The node the anchor is on.</summary>
    public NodeModel Node { get; }

    /// <summary>The outline point nearest to the other end of the link; null while the node has no size or the other end is unknown.</summary>
    public override Point? GetPosition(BaseLinkModel link, Point[] route)
    {
        if (Node.Size == null)
            return null;

        var isTarget = link.Target == this;
        var nodeCenter = Node.GetBounds()!.Center;
        Point? pt;
        if (route.Length > 0)
        {
            pt = route[isTarget ? ^1 : 0];
        }
        else
        {
            pt = GetOtherPosition(link, isTarget);
        }

        if (pt is null) return null;

        var line = new Line(pt, nodeCenter);
        var intersections = Node.GetShape().GetIntersectionsWithLine(line);
        return GetClosestPointTo(intersections, pt);
    }

    /// <inheritdoc />
    public override Point? GetPlainPosition() => Node.GetBounds()?.Center ?? null;
}
