using System.Collections.Generic;

namespace Flow.Diagrams.Core.Geometry;

public interface IShape
{
    public IEnumerable<Point> GetIntersectionsWithLine(Line line);
    public Point? GetPointAtAngle(double a);
}
