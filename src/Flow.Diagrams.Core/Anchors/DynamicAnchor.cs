using System;
using System.Linq;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>
/// Attaches to the node at whichever of several positions is closest to the other end of the link, so the link leaves
/// the node from the nearest side. Positions come from <see cref="IPositionProvider"/>s, for example
/// <see cref="BoundsBasedPositionProvider"/> at the middle of each edge.
/// </summary>
public sealed class DynamicAnchor : Anchor
{
    /// <summary>Creates an anchor on <paramref name="model"/> that chooses among <paramref name="providers"/>; throws <see cref="InvalidOperationException"/> when the array is empty.</summary>
    public DynamicAnchor(NodeModel model, IPositionProvider[] providers) : base(model)
    {
        if (providers.Length == 0)
            throw new InvalidOperationException("No providers provided");

        Node = model;
        Providers = providers;
    }

    /// <summary>The node the anchor is on.</summary>
    public NodeModel Node { get; }
    /// <summary>The candidate positions on <see cref="Node"/>; the one closest to the other end wins.</summary>
    public IPositionProvider[] Providers { get; }

    /// <summary>The provider position closest to the other end of the link; null while the node has no size or the other end is unknown.</summary>
    public override Point? GetPosition(BaseLinkModel link, Point[] route)
    {
        if (Node.Size == null)
            return null;

        var isTarget = link.Target == this;
        var pt = route.Length > 0 ? route[isTarget ? ^1 : 0] : GetOtherPosition(link, isTarget);
        var positions = Providers.Select(e => e.GetPosition(Node));
        return pt is null ? null : GetClosestPointTo(positions, pt);
    }

    /// <inheritdoc />
    public override Point? GetPlainPosition() => Node.GetBounds()?.Center ?? null;
}