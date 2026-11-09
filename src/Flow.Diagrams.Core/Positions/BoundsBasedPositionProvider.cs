using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;

namespace Flow.Diagrams.Core.Positions;

/// <summary>A point given as a fraction of a model's bounding box, plus a fixed offset: <c>(0, 0)</c> is the top-left corner, <c>(0.5, 0.5)</c> the center, <c>(1, 1)</c> the bottom-right. Requires an <see cref="IHasBounds"/> model.</summary>
public class BoundsBasedPositionProvider : IPositionProvider
{
    /// <summary>Creates a provider for the point at fractions <paramref name="x"/> and <paramref name="y"/> of the bounds, shifted by the offsets.</summary>
    public BoundsBasedPositionProvider(double x, double y, double offsetX = 0, double offsetY = 0)
    {
        X = x;
        Y = y;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>Horizontal position as a fraction of the bounds' width, from the left edge.</summary>
    public double X { get; }
    /// <summary>Vertical position as a fraction of the bounds' height, from the top edge.</summary>
    public double Y { get; }
    /// <summary>Horizontal shift added after the fraction is applied, in diagram units; not scaled with the size.</summary>
    public double OffsetX { get; }
    /// <summary>Vertical shift added after the fraction is applied, in diagram units; not scaled with the size.</summary>
    public double OffsetY { get; }

    /// <summary>The point in the model's bounds; null while the model has no bounds. Throws <see cref="DiagramsException"/> when the model is not an <see cref="IHasBounds"/>.</summary>
    public Point? GetPosition(Model model)
    {
        if (model is not IHasBounds ihb)
            throw new DiagramsException("BoundsBasedPositionProvider requires an IHasBounds model");
        
        var bounds = ihb.GetBounds();
        if (bounds == null)
            return null;
        
        return new Point(bounds.Left + X * bounds.Width + OffsetX, bounds.Top + Y * bounds.Height + OffsetY);
    }
}