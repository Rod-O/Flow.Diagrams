using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models.Base;

public interface IHasBounds
{
    public Rectangle? GetBounds();
}