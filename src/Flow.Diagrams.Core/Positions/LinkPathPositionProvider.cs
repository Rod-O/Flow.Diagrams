using Flow.Diagrams.Core.Geometry;
using Flow.Diagrams.Core.Models.Base;
using System;

namespace Flow.Diagrams.Core.Positions;

/// <summary>A point along a link's drawn path. Requires a <see cref="BaseLinkModel"/>.</summary>
public class LinkPathPositionProvider : IPositionProvider
{
    /// <summary>Creates a provider for the point at <paramref name="distance"/> along the path, shifted by the offsets.</summary>
    public LinkPathPositionProvider(double distance, double offsetX = 0, double offsetY = 0)
    {
        Distance = distance;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>
    /// Where along the path, measured from the source. From 0 to 1 inclusive it is a fraction of the total length (so exactly
    /// <c>1</c> is the end, not one unit); above 1 it is a length in diagram units; below 0 it is a length counted back from the
    /// target end.
    /// </summary>
    public double Distance { get; }
    /// <summary>Horizontal shift added to the point, in diagram units.</summary>
    public double OffsetX { get; }
    /// <summary>Vertical shift added to the point, in diagram units.</summary>
    public double OffsetY { get; }

    /// <summary>The point on the link's path; null until the link has been drawn once (no path yet). Throws <see cref="DiagramsException"/> when the model is not a link.</summary>
    public Point? GetPosition(Model model)
    {
        if (model is not BaseLinkModel link)
            throw new DiagramsException("LinkPathPositionProvider requires a link model");
        
        if (link.PathGeneratorResult == null)
            return null;
            
        var totalLength = link.PathGeneratorResult.FullPath.Length;
        var length = Distance switch
        {
            >= 0 and <= 1 => Distance * totalLength,
            > 1 => Distance,
            < 0 => totalLength + Distance,
            _ => throw new NotImplementedException()
        };

        var pt = link.PathGeneratorResult.FullPath.GetPointAtLength(length);
        return new Point(pt.X + OffsetX, pt.Y + OffsetY);
    }
}