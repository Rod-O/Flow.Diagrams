using Flow.Diagrams.Core.Geometry;

namespace Flow.Diagrams.Core.Models.Base;

public interface IHasShape
{
    public IShape GetShape();
}