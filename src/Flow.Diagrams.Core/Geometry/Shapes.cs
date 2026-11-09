using Flow.Diagrams.Core.Models;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>
/// Factories that turn a node's or port's box into an <see cref="IShape"/> for anchors to intersect. The box is
/// <c>Position</c> as top-left corner and <c>Size</c>; a node that is not measured yet (null <c>Size</c>) gives a shape of zero size at its position.
/// </summary>
public static class Shapes
{
    /// <summary>The node's box as a rectangle.</summary>
    public static IShape Rectangle(NodeModel node) => Rectangle(node.Position, node.Size ?? Size.Zero);

    /// <summary>A circle inscribed in the node's box, centered on it; the diameter is the shorter side of the box.</summary>
    public static IShape Circle(NodeModel node) => Circle(node.Position, node.Size ?? Size.Zero);

    /// <summary>The ellipse inscribed in the node's box.</summary>
    public static IShape Ellipse(NodeModel node) => Ellipse(node.Position, node.Size ?? Size.Zero);

    /// <summary>The port's box as a rectangle.</summary>
    public static IShape Rectangle(PortModel port) => Rectangle(port.Position, port.Size);

    /// <summary>A circle inscribed in the port's box, centered on it; the diameter is the shorter side of the box.</summary>
    public static IShape Circle(PortModel port) => Circle(port.Position, port.Size);

    /// <summary>The ellipse inscribed in the port's box.</summary>
    public static IShape Ellipse(PortModel port) => Ellipse(port.Position, port.Size);
    
    private static IShape Rectangle(Point position, Size size) => new Rectangle(position, size);

    private static IShape Circle(Point position, Size size)
    {
        var radius = Math.Min(size.Width, size.Height) / 2;
        var centerX = position.X + size.Width / 2;
        var centerY = position.Y + size.Height / 2;
        return new Ellipse(centerX, centerY, radius, radius);
    }

    private static IShape Ellipse(Point position, Size size)
    {
        var halfWidth = size.Width / 2;
        var halfHeight = size.Height / 2;
        var centerX = position.X + halfWidth;
        var centerY = position.Y + halfHeight;
        return new Ellipse(centerX, centerY, halfWidth, halfHeight);
    }
}
