using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Routers;

/// <summary>Decides the intermediate points a link passes through between its two ends. Set per link with <c>BaseLinkModel.Router</c> or for all links through the diagram's default.</summary>
public abstract class Router
{
    /// <summary>The points between source and target, in order and in diagram coordinates; empty for a direct line.</summary>
    public abstract Point[] GetRoute(Diagram diagram, BaseLinkModel link);

    /// <summary>The point on the port's box that faces out in its alignment: the middle of the edge, or the corner for diagonal alignments. <see cref="PortAlignment.TopLeft"/> returns the box's top-left corner.</summary>
    protected static Point GetPortPositionBasedOnAlignment(PortModel port)
    {
        var pt = port.Position;
        switch (port.Alignment)
        {
            case PortAlignment.Top:
                return new Point(pt.X + port.Size.Width / 2, pt.Y);
            case PortAlignment.TopRight:
                return new Point(pt.X + port.Size.Width, pt.Y);
            case PortAlignment.Right:
                return new Point(pt.X + port.Size.Width, pt.Y + port.Size.Height / 2);
            case PortAlignment.BottomRight:
                return new Point(pt.X + port.Size.Width, pt.Y + port.Size.Height);
            case PortAlignment.Bottom:
                return new Point(pt.X + port.Size.Width / 2, pt.Y + port.Size.Height);
            case PortAlignment.BottomLeft:
                return new Point(pt.X, pt.Y + port.Size.Height);
            case PortAlignment.Left:
                return new Point(pt.X, pt.Y + port.Size.Height / 2);
            default:
                return pt;
        }
    }
}
