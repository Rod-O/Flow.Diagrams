using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>A fixed point in diagram coordinates attached to no model. A link being dragged ends on one until it snaps to a target.</summary>
public sealed class PositionAnchor : Anchor
{
    private Point _position;

    /// <summary>Creates an anchor at <paramref name="position"/>, in diagram units.</summary>
    public PositionAnchor(Point position) : base(null)
    {
        _position = position;
    }

    /// <summary>Moves the anchor; links using it are not refreshed, the caller does that.</summary>
    public void SetPosition(Point position) => _position = position;

    /// <inheritdoc />
    public override Point? GetPlainPosition() => _position;

    /// <inheritdoc />
    public override Point? GetPosition(BaseLinkModel link, Point[] route) => _position;
}
