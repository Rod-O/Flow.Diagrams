using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System;
using System.Collections.Generic;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>
/// Where one end of a link attaches: the link asks its source and target anchors for their position every time it is
/// drawn. Use <see cref="SinglePortAnchor"/> for a port, <see cref="ShapeIntersectionAnchor"/> or <see cref="DynamicAnchor"/>
/// for a node, <see cref="LinkAnchor"/> for a point on another link and <see cref="PositionAnchor"/> for a free point.
/// </summary>
public abstract class Anchor
{
    /// <summary>Creates an anchor attached to <paramref name="model"/>, or to no model when null.</summary>
    protected Anchor(ILinkable? model = null)
    {
        Model = model;
    }

    /// <summary>The model this anchor attaches to; null for a free point such as <see cref="PositionAnchor"/>.</summary>
    public ILinkable? Model { get; }

    /// <summary>
    /// Where <paramref name="link"/> touches this anchor, in diagram units; null when it cannot be computed yet (for example
    /// the node has no size). Anchors that depend on the other end aim at the first (or last) point of <paramref name="route"/>,
    /// or at the other anchor's <see cref="GetPlainPosition"/> when the route is empty.
    /// </summary>
    /// <param name="link">The link being drawn.</param>
    /// <param name="route">The link's intermediate points, from source to target; empty when it has none.</param>
    public abstract Point? GetPosition(BaseLinkModel link, Point[] route);

    /// <summary>The anchor's reference point, independent of any link (the node's center, the port's middle); null when unknown. Used to aim the other end of a link at this one.</summary>
    public abstract Point? GetPlainPosition();

    /// <summary><see cref="GetPosition(BaseLinkModel, Point[])"/> for a link with no intermediate points.</summary>
    public Point? GetPosition(BaseLinkModel link) => GetPosition(link, Array.Empty<Point>());

    /// <summary>The <see cref="GetPlainPosition"/> of the end of <paramref name="link"/> opposite this anchor; <paramref name="isTarget"/> says whether this anchor is the target.</summary>
    protected static Point? GetOtherPosition(BaseLinkModel link, bool isTarget)
    {
        var anchor = isTarget ? link.Source : link.Target!;
        return anchor.GetPlainPosition();
    }

    /// <summary>The member of <paramref name="points"/> nearest to <paramref name="point"/>, ignoring nulls; null when none is left.</summary>
    protected static Point? GetClosestPointTo(IEnumerable<Point?> points, Point point)
    {
        var minDist = double.MaxValue;
        Point? minPoint = null;

        foreach (var pt in points)
        {
            if (pt == null)
                continue;
            
            var dist = pt.DistanceTo(point);
            if (dist < minDist)
            {
                minDist = dist;
                minPoint = pt;
            }
        }

        return minPoint;
    }
}
