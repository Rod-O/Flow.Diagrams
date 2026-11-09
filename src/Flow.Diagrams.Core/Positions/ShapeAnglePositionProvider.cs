using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Positions;

/// <summary>The point where a ray from the center of a model's shape, at a given angle, leaves the shape. Requires an <see cref="IHasShape"/> model.</summary>
public class ShapeAnglePositionProvider : IPositionProvider
{
    /// <summary>Creates a provider for the shape point at <paramref name="angle"/> degrees, shifted by the offsets.</summary>
    public ShapeAnglePositionProvider(double angle, double offsetX = 0, double offsetY = 0)
    {
        Angle = angle;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>Direction of the ray in degrees, clockwise on screen: 0 is right, 90 is down, 180 is left, 270 is up.</summary>
    public double Angle { get; }
    /// <summary>Horizontal shift added to the point, in diagram units.</summary>
    public double OffsetX { get; }
    /// <summary>Vertical shift added to the point, in diagram units.</summary>
    public double OffsetY { get; }
    
    /// <summary>The point on the model's shape outline; null when the shape yields none. Throws <see cref="DiagramsException"/> when the model is not an <see cref="IHasShape"/>.</summary>
    public Point? GetPosition(Model model)
    {
        if (model is not IHasShape ihs)
            throw new DiagramsException("ShapeAnglePositionProvider requires an IHasShape model");
        
        var shape = ihs.GetShape();
        return shape.GetPointAtAngle(Angle)?.Add(OffsetX, OffsetY);
    }
}