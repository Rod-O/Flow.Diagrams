using System.Collections.Generic;

namespace Flow.Diagrams.Core.Geometry;

/// <summary>The outline of a node or port, used by anchors to find where a link meets it.</summary>
public interface IShape
{
    /// <summary>The points where the segment <paramref name="line"/> crosses the outline; empty when it does not.</summary>
    public IEnumerable<Point> GetIntersectionsWithLine(Line line);
    /// <summary>The outline point in direction <paramref name="a"/> (degrees, 0 = right, 90 = down) from the center; null when there is none.</summary>
    public Point? GetPointAtAngle(double a);
}
