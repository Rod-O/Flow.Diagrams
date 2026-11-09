using System;
using System.Linq;
using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;
using Flow.Diagrams.Core.Positions;

namespace Flow.Diagrams.Core.Anchors;

public sealed class DynamicAnchor : Anchor
{
    public DynamicAnchor(NodeModel model, IPositionProvider[] providers) : base(model)
    {
        if (providers.Length == 0)
            throw new InvalidOperationException("No providers provided");

        Node = model;
        Providers = providers;
    }

    public NodeModel Node { get; }
    public IPositionProvider[] Providers { get; }

    public override Point? GetPosition(BaseLinkModel link, Point[] route)
    {
        if (Node.Size == null)
            return null;

        var isTarget = link.Target == this;
        var pt = route.Length > 0 ? route[isTarget ? ^1 : 0] : GetOtherPosition(link, isTarget);
        var positions = Providers.Select(e => e.GetPosition(Node));
        return pt is null ? null : GetClosestPointTo(positions, pt);
    }

    public override Point? GetPlainPosition() => Node.GetBounds()?.Center ?? null;
}