using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Anchors;

/// <summary>Attaches to one point of a port, chosen by the port's <see cref="PortModel.Alignment"/>. The default anchor for links that start or end on a port.</summary>
public sealed class SinglePortAnchor : Anchor
{
    /// <summary>Creates an anchor on <paramref name="port"/>.</summary>
    public SinglePortAnchor(PortModel port) : base(port)
    {
        Port = port;
    }
    
    /// <summary>The port the anchor is on.</summary>
    public PortModel Port { get; }
    /// <summary>Whether a link end without a marker attaches at the port's middle instead of its aligned point. Off by default.</summary>
    public bool MiddleIfNoMarker { get; set; } = false;
    /// <summary>Whether the point is found on the port's shape at the angle of its alignment (the default), or on the port's bounding box. The two differ for non-rectangular ports.</summary>
    public bool UseShapeAndAlignment { get; set; } = true;

    /// <summary>The point on the port facing its <see cref="PortModel.Alignment"/>; null until the port has been measured (<see cref="PortModel.Initialized"/>).</summary>
    public override Point? GetPosition(BaseLinkModel link, Point[] route)
    {
        if (!Port.Initialized)
            return null;

        if (MiddleIfNoMarker && ((link.Source == this && link.SourceMarker is null) || (link.Target == this && link.TargetMarker is null)))
            return Port.MiddlePosition;
        
        var pt = Port.Position;
        if (UseShapeAndAlignment)
        {
            return Port.Alignment switch
            {
                PortAlignment.Top => Port.GetShape().GetPointAtAngle(270),
                PortAlignment.TopRight => Port.GetShape().GetPointAtAngle(315),
                PortAlignment.Right => Port.GetShape().GetPointAtAngle(0),
                PortAlignment.BottomRight => Port.GetShape().GetPointAtAngle(45),
                PortAlignment.Bottom => Port.GetShape().GetPointAtAngle(90),
                PortAlignment.BottomLeft => Port.GetShape().GetPointAtAngle(135),
                PortAlignment.Left => Port.GetShape().GetPointAtAngle(180),
                PortAlignment.TopLeft => Port.GetShape().GetPointAtAngle(225),
                _ => null,
            };
        }

        return Port.Alignment switch
        {
            PortAlignment.Top => new Point(pt.X + Port.Size.Width / 2, pt.Y),
            PortAlignment.TopRight => new Point(pt.X + Port.Size.Width, pt.Y),
            PortAlignment.Right => new Point(pt.X + Port.Size.Width, pt.Y + Port.Size.Height / 2),
            PortAlignment.BottomRight => new Point(pt.X + Port.Size.Width, pt.Y + Port.Size.Height),
            PortAlignment.Bottom => new Point(pt.X + Port.Size.Width / 2, pt.Y + Port.Size.Height),
            PortAlignment.BottomLeft => new Point(pt.X, pt.Y + Port.Size.Height),
            PortAlignment.Left => new Point(pt.X, pt.Y + Port.Size.Height / 2),
            _ => pt,
        };
    }

    /// <inheritdoc />
    public override Point? GetPlainPosition() => Port.MiddlePosition;
}
